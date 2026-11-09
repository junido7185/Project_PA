#!/usr/bin/env python3
"""P.A. observation and explicit Mod submissions; no automatic model or send calls."""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
from production_adapter import budget_policy
from production_observation import observation, open_evidence
from production_messages import submit_instruction, record_delivery, instruction_observations
from production_controls import control_snapshot, control_action

STATE = Path('Automation/LoopEngineering/State/loop-state.json')
HANDOFF = Path('Docs/00_CURRENT/CODEX_HANDOFF.md')


def read_json(path: Path) -> dict:
    value = json.loads(path.read_text(encoding='utf-8-sig'))
    if not isinstance(value, dict):
        raise ValueError(f'Expected an object: {path.name}')
    return value


def clean(value, limit=160):
    return re.sub(r'[\x00-\x1f\x7f-\x9f]', ' ', str(value))[:limit]


def snapshot(root: Path) -> dict:
    state = read_json(root / STATE)
    routing = state.get('demoProductionRouting', {})
    selected = routing.get('selectedApproval', '')
    approval = state.get(selected, {})
    ticket = approval.get('activeTicket', routing.get('activeTicket', '?'))
    result = approval.get('results', {}).get(ticket, {})
    budget = result.get('approvedTotalBudget', approval.get('perTicketBudget', {}))
    policy = budget_policy(state, root)
    status = str(approval.get('status', 'UNKNOWN'))
    ticket_status = str(approval.get('ticketStatus', {}).get(ticket, 'UNKNOWN'))
    gate = 'WORK_BLOCKED' if any(x in (status+' '+ticket_status).lower() for x in ('blocked', 'incomplete', 'hold', 'reject')) else 'CHECK_APPROVAL'
    handoff = (root / HANDOFF).read_text(encoding='utf-8-sig')
    match = re.search(r'^Exact next action:\s*(.+)$', handoff, re.M)
    next_action = match.group(1) if match else 'Read the current authoritative Handoff.'
    if not match:
        korean_next = re.search(r'^- 다음:\s*(.+)$', handoff, re.M)
        if korean_next:
            next_action = korean_next.group(1)
    missing_budget = any(result.get(key) is None for key in ('compileCycles','targetedPlayRuns','dedicatedValidatorsAdded'))
    if missing_budget and policy['mode'] == 'bounded':
        gate = 'WORK_BLOCKED_UNKNOWN_BUDGET'
    elif policy['mode'] == 'unverified':
        gate = 'WORK_BLOCKED_POLICY_REVIEW'
    dispatch = state.get('productionEnvironment', {}).get('productionDispatch', {})
    if dispatch.get('resumeAuthorization', {}).get('enabled') is not True:
        gate += ' / WAIT_USER_02'
    lease = state.get('productionEnvironment', {}).get('coordinatorLease', {})
    return {
        'project_root': str(root), 'selected_approval': selected,
        'ticket': ticket, 'ticket_label': approval.get('ticketDefinitions', {}).get(ticket, ''),
        'approval_status': status, 'ticket_status': ticket_status, 'work_gate': gate,
        'unity_owner': routing.get('unityEditorOwner', 'UNKNOWN'),
        'writer_session_status': lease.get('status', 'UNKNOWN'),
        'active_provider': 'none' if lease.get('status') == 'stopped' else lease.get('owner', 'UNKNOWN'),
        'budget_policy': policy,
        'usage_totals_aggregated': not missing_budget,
        'historical_ticket_limits': budget if policy['mode']=='adaptive' else {},
        'actual_checkpoint': state.get('productionEnvironment',{}).get('nativeResumeCheckpoint',{}),
        'ticket_status_is_legacy': policy['mode']=='adaptive',
        'historical_blocked_budgets': {key: value for key, value in approval.get('results', {}).items()
            if 'BLOCKED' in str(approval.get('ticketStatus', {}).get(key, '')).upper()},
        'budget': { 'compile': [result.get('compileCycles'), budget.get('compileFix') if policy['fixedCapsApply'] else None],
                    'play': [result.get('targetedPlayRuns'), budget.get('targetedPlay') if policy['fixedCapsApply'] else None],
                    'validator': [result.get('dedicatedValidatorsAdded'), budget.get('dedicatedValidators') if policy['fixedCapsApply'] else None] },
        'evidence': result.get('evidence', 'N/A; see actual checkpoint and production Handoff'),
        'next_action': next_action,
        'ticket_states': approval.get('ticketStatus', {}),
        'task_progress_is_not_game_completion_percentage': True,
    }


def percent(value):
    if isinstance(value, (int, float)) and not isinstance(value, bool) and 0 <= value <= 100:
        return f'{value:.0f}%'
    return 'N/A'


def reset_text(epoch):
    if not isinstance(epoch, (int, float)) or isinstance(epoch, bool):
        return 'N/A'
    try:
        return dt.datetime.fromtimestamp(epoch).astimezone().strftime('%m/%d %H:%M %Z')
    except (ValueError, OSError, OverflowError):
        return 'N/A'


def statusline(root: Path, data: dict) -> str:
    try:
        s = snapshot(root)
        budgets = ' '.join(f'{key} {v[0] if v[0] is not None else "?"}/{v[1] if v[1] is not None else "?"}' for key, v in s['budget'].items())
        if s['budget_policy']['mode']=='adaptive':
            budgets = 'adaptive policy | lifetime usage ' + ('recorded' if s['usage_totals_aggregated'] else 'unaggregated; not zero') + ' | no fixed ticket caps'
        project = f'P.A. | {clean(s["ticket"])} {clean(s["work_gate"])} | Unity: {clean(s["unity_owner"])} | {budgets}'
    except (OSError, ValueError, KeyError):
        project = 'P.A. | Project state unavailable; check working directory.'
    limits = data.get('rate_limits', {}) or {}
    rows = []
    for key, label in [('five_hour', '5h'), ('seven_day', '7d')]:
        window = limits.get(key, {}) or {}
        rows.append(f'{label} used {percent(window.get("used_percentage"))} reset {reset_text(window.get("resets_at"))}')
    model = clean((data.get('model') or {}).get('display_name', 'Claude'))
    context = percent((data.get('context_window') or {}).get('used_percentage'))
    return '\n'.join([project, f'{model} | context {context} | ' + ' | '.join(rows),
                      'Codex quota: use native /status; external task state is not subscription balance.'])


def configuration_summary(path: Path) -> dict:
    if not path.is_file():
        return {'exists': False}
    try:
        data = read_json(path)
    except (OSError, ValueError):
        return {'exists': True, 'parse': 'INVALID_JSON_OR_JSONC_REVIEW_REQUIRED'}
    return {'exists': True, 'model': clean(data.get('model', 'not set')),
            'effort': clean(data.get('effortLevel', 'not set')),
            'permissionMode': clean(data.get('permissions', {}).get('defaultMode', 'not set')),
            'statusLineConfigured': bool(data.get('statusLine')),
            'disableAllHooks': data.get('disableAllHooks', False),
            'mcpAllowedNames': data.get('enabledMcpjsonServers', []),
            'enabledPluginNames': [k for k,v in data.get('enabledPlugins', {}).items() if v],
            'envNamesOnly': sorted((data.get('env') or {}).keys())}


def diagnose(root: Path) -> dict:
    user_claude = Path(os.environ.get('CLAUDE_CONFIG_DIR', str(Path.home()/'.claude')))
    extensions = []
    for directory in [Path.home()/'.vscode/extensions', Path.home()/'.vscode-insiders/extensions']:
        if directory.is_dir():
            for package in directory.glob('anthropic.claude-code-*/package.json'):
                try:
                    p = read_json(package)
                    extensions.append({'path': str(package.parent), 'version': p.get('version', '?')})
                except (OSError, ValueError):
                    extensions.append({'path': str(package.parent), 'version': 'UNREADABLE'})
    commands = {}
    for name in ('claude', 'codex', 'code', 'git', 'bash', 'python', 'pwsh'):
        executable = shutil.which(name)
        item = {'path': executable, 'version': 'NOT_CHECKED'}
        if executable and Path(executable).suffix.lower() not in ('.cmd', '.bat', '.ps1'):
            try:
                r = subprocess.run([executable, '--version'], capture_output=True, text=True, timeout=8, errors='replace')
                item['version'] = clean(r.stdout.splitlines()[0] if r.stdout.splitlines() else 'NO_STDOUT')
            except (OSError, subprocess.TimeoutExpired):
                item['version'] = 'CHECK_FAILED'
        elif executable:
            item['version'] = 'SHIM_DETECTED_CHECK_NATIVE_BINARY'
        commands[name] = item
    try:
        project = snapshot(root)
    except (OSError, ValueError):
        project = {'work_gate': 'STATE_UNAVAILABLE'}
    return {
        'mode': 'READ_ONLY_NO_MODEL_CALLS_NO_UNITY_CALLS',
        'platform': sys.platform, 'python': sys.executable, 'cwd': str(root),
        'claude_config_directory': str(user_claude),
        'commands': commands, 'claude_extension_installations': extensions,
        'settings': {str(p): configuration_summary(p) for p in [user_claude/'settings.json', root/'.claude/settings.json', root/'.claude/settings.local.json']},
        'billing_environment_presence_only': {name: bool(os.environ.get(name)) for name in [
            'ANTHROPIC_API_KEY','ANTHROPIC_AUTH_TOKEN','OPENAI_API_KEY','CODEX_API_KEY',
            'ANTHROPIC_BASE_URL','OPENAI_BASE_URL','CLAUDE_CODE_USE_BEDROCK','CLAUDE_CODE_USE_VERTEX','CLAUDE_CODE_USE_FOUNDRY']},
        'mcp_project_config_exists': (root/'.mcp.json').is_file(),
        'project': project,
        'not_observed': ['VS Code active profile / remote host', 'extension effective settings and error log',
                         'subscription sign-in and actual balances', 'live Unity identity', 'Mods feature flag'],
    }


def resume_note(root: Path) -> str:
    s = snapshot(root)
    return f'''# P.A. resume pointer (generated; not task authority)

Read AGENTS.md and the four current documents, then {HANDOFF.as_posix()}.
Selected approval: {s['selected_approval']}; ticket: {s['ticket']}.
Work gate: {s['work_gate']}; Unity owner: {s['unity_owner']}.
Budget from existing state: {json.dumps(s['budget'], ensure_ascii=False)}.
Exact next action in Handoff: {s['next_action']}
Evidence pointer: {s['evidence']}

Compare actual dirty files, Editor identity and in-flight operations before acting.
Changing Claude/Codex or a subscription reset never renews work budgets.
Keep the current owner until a valid, verified transfer. Do not replay an uncertain tool call.
If this ticket is blocked, continue only independent, approved support work.
Provider session IDs and run events, when available, are under Logs/AgentConsole.
Read only the latest relevant run, not every transcript. Never infer completion from the provider's last sentence.
'''

def mod_state(root: Path):
    """Read the authoritative large state in Python; return a small view, never another authority."""
    state = read_json(root / STATE)
    routing = state.get('demoProductionRouting', {})
    key = routing.get('selectedApproval', '')
    approval = state.get(key, {})
    active = approval.get('activeTicket')
    fields = ('compileCycles','targetedPlayRuns','dedicatedValidatorsAdded','approvedTotalBudget','evidence')
    selected = {k: approval.get(k) for k in ('activeTicket','status','ticketStatus','ticketDefinitions','perTicketBudget')}
    selected['results'] = {ticket: {k: result.get(k) for k in fields}
        for ticket, result in approval.get('results', {}).items() if ticket in (active, 'D2')}
    env = state.get('productionEnvironment', {})
    handoff=(root/HANDOFF).read_text('utf-8-sig')
    pointer=re.search(r'Logs/ParallelProduction/PresentationBuild-\d{8}/backup-before/CODEX_HANDOFF\.md',handoff)
    observed = observation(root, state)
    observed['instructions'] = instruction_observations(root, state)
    plugin = Path(__file__).resolve().parent / 'pa-production-mod'
    runtime = {'python': sys.executable, 'pythonVersion': sys.version.split()[0],
               'consolePath': str(Path(__file__).resolve()), 'pluginSource': str(plugin),
               'sourceVersion': read_json(plugin / '.claude-plugin/plugin.json').get('version'),
               'sourceHookSha256': hashlib.sha256((plugin / 'hooks/register.js').read_bytes()).hexdigest()}
    try:
        registered = read_json(Path.home() / '.claude/plugins/installed_plugins.json')
        records = registered.get('plugins', {}).get('pa-production-console@project-pa-local', [])
        record = next(r for r in records if r.get('scope') == 'project'
                      and Path(r.get('projectPath', '')).resolve() == root.resolve())
        cache = Path(record['installPath'])
        runtime['registeredPlugin'] = {'version':record.get('version'), 'path':str(cache),
            'hookSha256':hashlib.sha256((cache/'hooks/register.js').read_bytes()).hexdigest(),
            'lastUpdated':record.get('lastUpdated'), 'notProofOfRuntimeReload':True}
    except (OSError, ValueError, StopIteration, KeyError, TypeError):
        runtime['registeredPlugin'] = {'status':'UNCONFIRMED'}
    return {'demoProductionRouting': routing, key: selected,
            '_console': {'budgetPolicy':budget_policy(state,root),'actualCheckpoint':env.get('nativeResumeCheckpoint',{}),
                         'observation': observed,
                         'controls': control_snapshot(root), 'runtime': runtime,
                         'handoffKind':'delivery' if handoff.startswith('# 최종발표용 Windows 실행 빌드') else 'current',
                         'productionHandoffPath':pointer[0] if handoff.startswith('# 최종발표용 Windows 실행 빌드') and pointer and (root/pointer[0]).is_file() else HANDOFF.as_posix()},
            'productionEnvironment': {'coordinatorLease': {k: env.get('coordinatorLease',{}).get(k) for k in ('owner','status')},
              'productionDispatch': {'resumeAuthorization': {'enabled': env.get('productionDispatch',{}).get('resumeAuthorization',{}).get('enabled',False)}}}}


def install_statusline(root: Path):
    expected = Path('C:/Users/sdjsd/Desktop/Unity/Project_PA')
    if os.name != 'nt' or root.resolve() != expected.resolve():
        raise SystemExit('Install only in the approved Windows Project_PA root. No setting was changed.')
    if not (root/STATE).is_file():
        raise SystemExit('Existing project state missing.')
    path = root/'.claude/settings.local.json'
    data = read_json(path) if path.exists() else {}
    wrapper = root/'Tools/PaAgentConsole/statusline.ps1'
    if not wrapper.is_file():
        raise SystemExit('Copy the complete Tools/PaAgentConsole folder first.')
    command = f'powershell -NoProfile -ExecutionPolicy Bypass -File "{wrapper.as_posix()}" -PythonExecutable "{Path(sys.executable).as_posix()}"'
    setting = {'type': 'command', 'command': command, 'padding': 0}
    if data.get('statusLine') == setting:
        print('Status line already configured. Other settings unchanged.'); return
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        stamp = dt.datetime.now().strftime('%Y%m%d-%H%M%S-%f')
        backup = root/'Logs/AgentConsole/settings-backups'/f'settings.local-{stamp}.json'
        backup.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, backup)
    data['statusLine'] = setting
    tmp = path.with_name(path.name+'.pa-console.tmp')
    tmp.write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    os.replace(tmp, path)
    print('Project-local statusLine configured; model, permissions, MCPs and task state preserved.')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, default=Path.cwd())
    parser.add_argument('--statusline', action='store_true')
    parser.add_argument('--diagnose', action='store_true')
    parser.add_argument('--mod-state', action='store_true')
    instructions = parser.add_mutually_exclusive_group()
    instructions.add_argument('--instruction-submit', metavar='JSON')
    instructions.add_argument('--instruction-delivery', metavar='JSON')
    instructions.add_argument('--production-control', metavar='JSON')
    parser.add_argument('--open-evidence', metavar='PROJECT_PATH')
    parser.add_argument('--resume-note', action='store_true')
    parser.add_argument('--install-statusline', action='store_true')
    args = parser.parse_args()
    root = args.root.resolve()
    if args.production_control is not None:
        try:
            print(json.dumps(control_action(root, json.loads(args.production_control)), ensure_ascii=False))
        except (ValueError, OSError, RuntimeError) as error:
            print(json.dumps({'error': str(error), 'requested': False}, ensure_ascii=False))
            raise SystemExit(2)
    elif args.instruction_submit is not None or args.instruction_delivery is not None:
        try:
            operation = submit_instruction if args.instruction_submit is not None else record_delivery
            payload = args.instruction_submit if args.instruction_submit is not None else args.instruction_delivery
            print(json.dumps(operation(root, payload), ensure_ascii=False))
        except (ValueError, OSError, RuntimeError) as error:
            print(json.dumps({'error': str(error), 'saved': False, 'canDeliver': False,
                              'preserved': 'No automatic send, restart, approval or writer change'}, ensure_ascii=False))
            raise SystemExit(2)
    elif args.open_evidence:
        print(json.dumps(open_evidence(root, args.open_evidence), ensure_ascii=False))
    elif args.install_statusline:
        install_statusline(root)
    elif args.mod_state:
        print(json.dumps(mod_state(root), ensure_ascii=False))
    elif args.statusline:
        try: data = json.load(sys.stdin)
        except (ValueError, OSError): data = {}
        print(statusline(root, data if isinstance(data, dict) else {}))
    elif args.resume_note:
        print(resume_note(root))
    else:
        print(json.dumps(diagnose(root) if args.diagnose else snapshot(root), ensure_ascii=False, indent=2))


if __name__ == '__main__':
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    main()
