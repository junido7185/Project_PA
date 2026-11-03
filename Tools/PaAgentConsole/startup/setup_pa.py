#!/usr/bin/env python3
"""Project-local bootstrap. Check is read-only. Apply never launches models or Unity."""
from __future__ import annotations
import argparse
import datetime as dt
import hashlib
import importlib.util
import json
import os
from pathlib import Path, PurePosixPath
import shutil
import subprocess
import sys
import uuid

ALLOWED_ROOT = Path('C:/Users/sdjsd/Desktop/Unity/Project_PA')
BASE_COMMIT = 'b121140ae6f5ca0f92078db5c2f75512dc8fc95a'
STATE = 'Automation/LoopEngineering/State/loop-state.json'
HANDOFF = 'Docs/00_CURRENT/CODEX_HANDOFF.md'
TOOLS = 'Tools/PaAgentConsole'


class SetupBlocked(Exception):
    pass


def sha(path: Path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None


def safe_path(root: Path, relative: str) -> Path:
    parts = PurePosixPath(relative).parts
    if not parts or PurePosixPath(relative).is_absolute() or any(p in ('..', '.', '') or ':' in p or '\\' in p for p in parts):
        raise SetupBlocked('Unsafe relative path: ' + relative)
    path = root.joinpath(*parts)
    for ancestor in path.parents:
        if ancestor.exists() and not ancestor.is_dir():
            raise SetupBlocked('Parent path is a file: ' + str(ancestor))
    # Reject symlinks and Windows junctions, including any existing ancestor.
    for item in (root, *path.parents, path):
        if item.exists() or item.is_symlink():
            attrs = getattr(item.lstat(), 'st_file_attributes', 0)
            if item.is_symlink() or attrs & 0x400 or (hasattr(item, 'is_junction') and item.is_junction()):
                raise SetupBlocked('Linked path requires manual review: ' + str(item))
    if not path.resolve().is_relative_to(root.resolve()):
        raise SetupBlocked('Path escapes root: ' + relative)
    return path


def git(root: Path, *args: str, check=True):
    try:
        result = subprocess.run(['git', '-C', str(root), *args], capture_output=True, text=True,
                                encoding='utf-8', errors='replace', timeout=30)
    except (OSError, subprocess.TimeoutExpired) as exc:
        raise SetupBlocked('Git unavailable or timed out. No branch switch was attempted.') from exc
    if check and result.returncode:
        # Do not echo remote URLs, credentials or arbitrary stderr.
        raise SetupBlocked('Git check failed: ' + args[0])
    return result


def guard_windows_root(root: Path):
    if os.name != 'nt' or root.resolve() != ALLOWED_ROOT.resolve():
        raise SetupBlocked('Use the approved Windows root: C:\\Users\\sdjsd\\Desktop\\Unity\\Project_PA')
    safe_path(root, STATE)


def project_snapshot(root: Path) -> dict:
    top = Path(git(root, 'rev-parse', '--show-toplevel').stdout.strip())
    if top.resolve() != root.resolve():
        raise SetupBlocked('ProjectRoot is not the Git worktree root.')
    remote = git(root, 'config', '--get', 'remote.origin.url').stdout.strip().lower().rstrip('/')
    if not remote.endswith(('github.com/junido7185/project_pa.git', 'github.com/junido7185/project_pa',
                            'github.com:junido7185/project_pa.git', 'github.com:junido7185/project_pa')):
        raise SetupBlocked('Origin is not junido7185/Project_PA. Let the local AI review it.')
    branch = git(root, 'branch', '--show-current').stdout.strip()
    if not branch or branch in ('master', 'main') or branch.startswith('claude/portfolio'):
        raise SetupBlocked('Wrong or detached branch. Open the gameplay worktree; do not reset/stash your files.')
    if git(root, 'merge-base', '--is-ancestor', BASE_COMMIT, 'HEAD', check=False).returncode:
        raise SetupBlocked('Known gameplay baseline not found in current history. Local AI review is required.')
    for relative in (STATE, HANDOFF, 'AGENTS.md', 'ProjectSettings/ProjectVersion.txt'):
        if not safe_path(root, relative).is_file():
            raise SetupBlocked('Required gameplay file missing: ' + relative)
    state = json.loads((root / STATE).read_text(encoding='utf-8-sig'))
    routing = state.get('demoProductionRouting', {})
    selected = routing.get('selectedApproval')
    approval = state.get(selected, {}) if selected else {}
    if not selected or not isinstance(approval, dict):
        raise SetupBlocked('Current approval state needs local review.')
    settings = safe_path(root, '.claude/settings.local.json')
    if settings.exists():
        data = json.loads(settings.read_text(encoding='utf-8-sig'))
        if not isinstance(data, dict):
            raise SetupBlocked('Claude local settings must be a JSON object.')
    return {'branch': branch, 'head': git(root, 'rev-parse', 'HEAD').stdout.strip(),
            'dirty': git(root, 'status', '--porcelain=v1', '--untracked-files=normal').stdout.splitlines(),
            'selected_approval': selected, 'ticket': approval.get('activeTicket', routing.get('activeTicket')),
            'unity_owner': routing.get('unityEditorOwner'), 'approval_status': approval.get('status'),
            'state_sha256': sha(root / STATE), 'handoff_sha256': sha(root / HANDOFF)}


def verify_package(package: Path) -> dict:
    manifest = json.loads((package / 'starter-manifest.json').read_text(encoding='utf-8'))
    hashed_paths = set()
    for item in manifest['files']:
        if item['path'] in hashed_paths:
            raise SetupBlocked('Duplicate hash entry: ' + item['path'])
        hashed_paths.add(item['path'])
        if sha(safe_path(package, item['path'])) != item['sha256']:
            raise SetupBlocked('Package file missing or changed: ' + item['path'])
    # Only code-owned destinations are accepted, regardless of the manifest contents.
    destinations = set()
    for item in manifest['install']:
        rel = item['destination']
        if not rel.startswith(TOOLS + '/'):
            raise SetupBlocked('Unexpected installation target: ' + rel)
        if rel.casefold() in destinations:
            raise SetupBlocked('Duplicate installation destination: ' + rel)
        destinations.add(rel.casefold())
        if item['source'] not in hashed_paths or not safe_path(package, item['source']).is_file():
            raise SetupBlocked('Installation source is missing or unverified: ' + item['source'])
    return manifest


def reference_status(package: Path, root: Path) -> str:
    manifest = json.loads((package / 'payload/reference-manifest.json').read_text(encoding='utf-8'))
    for item in manifest['changed_files']:
        safe_path(root, item['path'])
    patch = str(package / 'payload/reference-design-adoption.patch')
    if git(root, 'apply', '--check', patch, check=False).returncode == 0:
        return 'READY'
    if git(root, 'apply', '--reverse', '--check', patch, check=False).returncode == 0:
        return 'ALREADY_APPLIED'
    return 'LOCAL_MERGE_REQUIRED'


def build_plan(package: Path, root: Path, manifest: dict) -> list[dict]:
    plan = []
    for item in manifest['install']:
        src = safe_path(package, item['source'])
        if not src.is_file():
            raise SetupBlocked('Installation source missing: ' + item['source'])
        dest = safe_path(root, item['destination'])
        action = 'SAME' if sha(dest) == sha(src) else ('CONFLICT' if dest.exists() else 'ADD')
        if dest.exists() and not dest.is_file():
            action = 'CONFLICT'
        plan.append({**item, 'action': action})
    return plan


def atomic_write(path: Path, data: bytes):
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(path.name + '.pa-' + uuid.uuid4().hex + '.tmp')
    try:
        with tmp.open('xb') as f:
            f.write(data)
        os.replace(tmp, path)
    finally:
        if tmp.exists():
            tmp.unlink()


def apply_files(package: Path, root: Path, plan: list[dict]):
    # Preflight every conflict before writing any file.
    if any(item['action'] == 'CONFLICT' for item in plan):
        raise SetupBlocked('Existing tool files differ. The local AI must merge those files first.')
    for item in plan:
        src = safe_path(package, item['source'])
        dest = safe_path(root, item['destination'])
        current = sha(dest)
        if item['action'] == 'ADD':
            dest.parent.mkdir(parents=True, exist_ok=True)
            # Exclusive create: never replace a file another session just created.
            with dest.open('xb') as f:
                f.write(src.read_bytes())
        elif current != sha(src):
            raise SetupBlocked('A tool file changed after preflight. Stop other writers and retry.')


def install_statusline(root: Path):
    path = root / TOOLS / 'pa_console.py'
    spec = importlib.util.spec_from_file_location('pa_console_install', path)
    module = importlib.util.module_from_spec(spec)
    sys.dont_write_bytecode = True
    spec.loader.exec_module(module)
    module.install_statusline(root)


def apply_setup(package: Path, root: Path, manifest: dict, before: dict, plan: list[dict], ref_status: str):
    if ref_status not in ('READY', 'ALREADY_APPLIED'):
        raise SetupBlocked('Reference patch conflicts with local documents. Nothing was installed; use the rescue prompt.')
    if any(x['action'] == 'CONFLICT' for x in plan):
        raise SetupBlocked('Tool file conflict. Nothing was installed; use the rescue prompt.')
    if project_snapshot(root) != before:
        raise SetupBlocked('Project changed after preflight. Stop active writers and retry.')
    # Preflight all mutable output paths before creating even the local journal.
    safe_path(root, TOOLS + '/startup/installed-runtime.json')
    safe_path(root, 'Logs/AgentConsole/setup')
    safe_path(root, 'Logs/AgentConsole/settings-backups')
    safe_path(root, '.claude/settings.local.json.pa-console.tmp')
    runtime_path = root / TOOLS / 'startup/installed-runtime.json'
    if runtime_path.exists():
        old_runtime = json.loads(runtime_path.read_text(encoding='utf-8-sig'))
        if not isinstance(old_runtime, dict) or old_runtime.get('package') != 'project-pa-start-kit':
            raise SetupBlocked('Existing installed-runtime.json is not owned by this starter. Manual merge is required.')
    stamp = dt.datetime.now(dt.timezone.utc).strftime('%Y%m%dT%H%M%SZ') + '-' + uuid.uuid4().hex[:8]
    run = safe_path(root, 'Logs/AgentConsole/setup/' + stamp)
    run.mkdir(parents=True)
    atomic_write(run / 'before.json', json.dumps(before, ensure_ascii=False, indent=2).encode('utf-8'))
    journal = {'phase': 'STARTED', 'reference': ref_status, 'added_files': [], 'protected_state_preserved': False}
    try:
        apply_files(package, root, plan)
        journal['added_files'] = [x['destination'] for x in plan if x['action'] == 'ADD']
        journal['phase'] = 'TOOLS_INSTALLED'
        if ref_status == 'READY':
            ref = json.loads((package / 'payload/reference-manifest.json').read_text(encoding='utf-8'))
            for item in ref['changed_files']:
                original = safe_path(root, item['path'])
                if original.is_file():
                    backup = safe_path(run / 'reference-backup', item['path'])
                    backup.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(original, backup)
            git(root, 'apply', '--check', str(package / 'payload/reference-design-adoption.patch'))
            git(root, 'apply', str(package / 'payload/reference-design-adoption.patch'))
        journal['phase'] = 'REFERENCES_INSTALLED'
        install_statusline(root)
        journal['phase'] = 'STATUSLINE_CONFIGURED'
        after = project_snapshot(root)
        if any(after[key] != before[key] for key in ('head', 'branch', 'state_sha256', 'handoff_sha256')):
            raise SetupBlocked('Current task authority changed during setup. Review the other writer; do not reset it.')
        runtime = {'package': 'project-pa-start-kit', 'python': sys.executable, 'project_root': str(root), 'setup_run': str(run),
                   'status': 'LOCAL_TOOLS_INSTALLED_LIVE_INTEGRATION_PENDING'}
        atomic_write(root / TOOLS / 'startup/installed-runtime.json',
                     (json.dumps(runtime, ensure_ascii=False, indent=2)+'\n').encode('utf-8'))
        journal['protected_state_preserved'] = True
        journal['phase'] = 'COMPLETE_LOCAL_FILES_ONLY'
    finally:
        # Journal allows the local AI to inspect partial work; no destructive rollback.
        atomic_write(run / 'result.json', (json.dumps(journal, ensure_ascii=False, indent=2)+'\n').encode('utf-8'))
    return run


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--project-root', type=Path, default=ALLOWED_ROOT)
    p.add_argument('--apply', action='store_true')
    args = p.parse_args()
    root = args.project_root.resolve()
    package = Path(__file__).resolve().parent
    try:
        guard_windows_root(root)
        manifest = verify_package(package)
        before = project_snapshot(root)
        plan = build_plan(package, root, manifest)
        reference = reference_status(package, root)
        summary = {'mode': 'APPLY' if args.apply else 'READ_ONLY_CHECK', 'python': sys.executable,
                   'project': before, 'reference_patch': reference,
                   'tools_add': sum(x['action']=='ADD' for x in plan),
                   'tools_same': sum(x['action']=='SAME' for x in plan),
                   'conflicts': [x['destination'] for x in plan if x['action']=='CONFLICT'],
                   'not_run': ['model calls', 'CLI login', 'Unity', 'compile', 'Play', 'production handover']}
        print(json.dumps(summary, ensure_ascii=False, indent=2))
        if summary['conflicts'] or reference == 'LOCAL_MERGE_REQUIRED':
            raise SetupBlocked('LOCAL_MERGE_REQUIRED. Paste 00_RESCUE.md into the working Codex extension.')
        if not args.apply:
            print('CHECK_OK. Apply with the same command plus -Apply. No project files changed.')
            return 0
        run = apply_setup(package, root, manifest, before, plan, reference)
        print('SETUP_OK. Local tools and references installed. Task budgets and owner preserved.')
        print('Journal: ' + str(run / 'result.json'))
        print('Next: start the local AI and send 01_ENVIRONMENT_SETUP.md. Live integration is still pending.')
        return 0
    except (SetupBlocked, OSError, ValueError, KeyError) as exc:
        print('SETUP_BLOCKED: ' + str(exc), file=sys.stderr)
        print('Use prompts/00_RESCUE.md with the working local Codex extension. No reset/clean/stash is needed.', file=sys.stderr)
        return 2


if __name__ == '__main__':
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
        sys.stderr.reconfigure(encoding='utf-8', errors='replace')
    raise SystemExit(main())
