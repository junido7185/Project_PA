"""Production entry support: existing loop state, isolated AI jobs and reviewed integration.

No package dependency. No commits, push, cleanup, checkout of the primary project or save writes.
"""
import argparse
import base64
import contextlib
import datetime as dt
import difflib
import hashlib
import importlib.util
import json
import math
import msvcrt
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import time
import tomllib
import traceback
import uuid

sys.dont_write_bytecode = True
_ENTRY_SOURCE_SHA256 = hashlib.sha256(Path(__file__).read_bytes()).hexdigest()

ROOT = Path(__file__).resolve().parents[2]
PRIMARY_ROOT = Path('C:/Users/sdjsd/Desktop/Unity/Project_PA').resolve()
STATE = ROOT / 'Automation/LoopEngineering/State/loop-state.json'
RUNS = ROOT / 'Logs/ParallelProduction'
KST = dt.timezone(dt.timedelta(hours=9))
_progress_spec = importlib.util.spec_from_file_location('pa_progress', ROOT/'Tools/PaAgentConsole/progress_watchdog.py')
progress = importlib.util.module_from_spec(_progress_spec); _progress_spec.loader.exec_module(progress)


def now():
    return dt.datetime.now(KST).isoformat()


def age_seconds(value, fallback):
    try:
        parsed = dt.datetime.fromisoformat(value)
        if parsed.tzinfo is None: return fallback
        return max(0, (dt.datetime.now(KST)-parsed).total_seconds())
    except (TypeError, ValueError): return fallback


def read(path):
    # Windows: opening the state while another process os.replace()s it raises a transient sharing
    # violation (PermissionError Errno 13). Retry briefly like write(); never swallow other errors.
    deadline = time.monotonic() + 12
    while True:
        try:
            return json.loads(Path(path).read_text(encoding='utf-8-sig'))
        except PermissionError:
            if time.monotonic() >= deadline:
                raise
            time.sleep(.15)


def write(path, data):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + '.' + str(uuid.uuid4()) + '.tmp')
    temporary.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    deadline = time.monotonic() + 12
    while True:
        try:
            os.replace(temporary, path)
            return
        except PermissionError:
            if time.monotonic() >= deadline:
                raise RuntimeError('State replacement locked; original and temporary backup preserved: ' + str(temporary))
            time.sleep(.15)


@contextlib.contextmanager
def state_lock():
    with STATE.with_suffix('.lock').open('a+b') as f:
        # Reading byte 0 while another process holds its msvcrt lock raises PermissionError (Errno 13),
        # so only the size is checked before locking.
        if os.fstat(f.fileno()).st_size == 0:
            f.write(b'0'); f.flush()
        deadline = time.monotonic() + 15
        while True:
            try:
                f.seek(0); msvcrt.locking(f.fileno(), msvcrt.LK_NBLCK, 1); break
            except OSError:
                if time.monotonic() > deadline:
                    raise RuntimeError('Loop state is busy; no state overwritten')
                time.sleep(.1)
        try:
            yield
        finally:
            f.seek(0); msvcrt.locking(f.fileno(), msvcrt.LK_UNLCK, 1)


def record(job):
    with state_lock():
        state = read(STATE)
        env=state['productionEnvironment']; current=env.setdefault('workers', {}).get(job['id'],{})
        if current.get('attemptToken') and current['attemptToken'] != job.get('attemptToken'):
            raise RuntimeError('Worker attempt changed; stale result retained in its log only')
        if current.get('relayGeneration') != job.get('relayGeneration'):
            raise RuntimeError('Worker generation changed; stale state rejected')
        if job.get('relayGeneration') and job['relayGeneration'] != env.get('coordinatorLease',{}).get('generation'):
            raise RuntimeError('Production generation changed; no stale worker state write')
        merged=dict(current); merged.update(job)
        if current.get('supervisorPid'): merged['supervisorPid']=current['supervisorPid']
        env['workers'][job['id']] = merged
        write(STATE, state)


def git(*args, cwd=ROOT):
    return subprocess.run(['git', '-c', 'core.quotepath=false', *args], cwd=cwd, check=True, capture_output=True,
                          encoding='utf-8', errors='replace').stdout.strip()


def safe_path(relative, base=ROOT):
    p = (base / relative).resolve()
    p.relative_to(base.resolve())
    if relative.startswith(('.git/', '.git\\')) or p == base.resolve():
        raise ValueError('Forbidden path')
    return p


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None


def relay_usage_state(observation, current=None):
    """Quota is server evidence; a passed reset or missing percentage is not headroom."""
    current = current or dt.datetime.now(KST)
    unknown = {'status': 'unknown', 'resetAt': None}
    if not isinstance(observation, dict): return unknown
    try:
        observed = dt.datetime.fromisoformat(str(observation.get('observedAt')).replace('Z', '+00:00'))
        if observed.tzinfo is None or not 0 <= (current-observed).total_seconds() <= 300:
            return unknown
    except (TypeError, ValueError): return unknown
    limits = observation.get('rateLimits') or []
    values = []
    for item in limits:
        if not isinstance(item, dict): return unknown
        try:
            if isinstance(item.get('percentUsed'), bool): return unknown
            value = float(item['percentUsed'])
            if not math.isfinite(value) or value < 0: return unknown
            values.append(value)
        except (KeyError, ValueError, TypeError): return unknown
    exhausted = [item for item, value in zip(limits, values) if value >= 100]
    resets = [future_quota_reset('', [item], current) for item in exhausted]
    reset = max(resets) if resets and all(resets) else None
    allowed = observation.get('ordinaryUsageAllowed')
    if allowed is False or any(v >= 100 for v in values):
        return {'status': 'exhausted', 'resetAt': reset.isoformat() if reset else None}
    if values and any(v >= 90 for v in values):
        return {'status': 'near_limit', 'resetAt': None}
    if allowed is True or (observation.get('source') == 'claude_mods' and values and allowed is not False):
        return {'status': 'available', 'resetAt': None}
    return unknown


def relay_choice(state, current=None):
    """Select independent work capability only. Never transfer the Unity lease here."""
    current = current or dt.datetime.now(KST)
    env = state.get('productionEnvironment', {}); relay = env.get('relaySupport', {})
    control = env.get('autonomous', {})
    if not relay.get('autoHandoff', {}).get('enabled') or not control.get('enabled') or control.get('stopRequested'):
        return {'action': 'paused', 'reason': 'Automation not authorized or paused'}
    if control.get('status') == 'ready_for_human_review' or (control.get('checkpoint') or {}).get('disposition') == 'human-review':
        return {'action': 'paused', 'reason': 'Final human review boundary'}
    waits = []; available = []
    for provider in ('claude-code', 'codex'):
        if relay.get('teams', {}).get(provider, {}).get('capability') != 'verified': continue
        usage = relay_usage_state(relay.get('observations', {}).get(provider, {}), current)
        if usage['status'] == 'available': available.append(provider)
        elif usage['status'] == 'exhausted' and usage['resetAt']:
            waits.append((usage['resetAt'], provider))
    if available:
        return {'action': 'available', 'provider': available[0], 'reason': 'Fresh usage and verified independent-work capability'}
    if waits:
        reset, provider = min(waits, key=lambda v: dt.datetime.fromisoformat(v[0]))
        return {'action': 'wait', 'provider': provider, 'resumeAt': reset, 'reason': 'Earliest capable team reset; fresh recheck required'}
    return {'action': 'blocked', 'reason': 'No fresh verified headroom/capability; preserve work, re-observe without inference'}


def relay_checkpoint(provider, job_id=None, reason='periodic', next_action='', evidence=(),
                     session_id=None, generation=None, safe_point=False):
    """Content-addressed recovery evidence, not a completion verdict or ownership transfer."""
    if provider not in ('claude-code', 'codex'): raise ValueError('Unknown relay provider')
    with state_lock():
        state = read(STATE); env = state['productionEnvironment']; lease = env.get('coordinatorLease', {})
        if generation is not None and lease.get('generation') != generation:
            raise RuntimeError('Relay generation changed; stale checkpoint rejected')
        job = env.get('workers', {}).get(job_id) if job_id else None
        if job_id and not job: raise RuntimeError('Registered job required')
        if job:
            if job.get('relayGeneration') and job['relayGeneration'] != lease.get('generation'):
                raise RuntimeError('Stale worker generation; checkpoint not re-adopted')
            if job.get('provider', 'claude-code') != provider: raise RuntimeError('Foreign worker provider')
            if job.get('sessionId') and session_id != job['sessionId']: raise RuntimeError('Foreign worker session')
            base = Path(job['worktree']).resolve(); base.relative_to(ROOT.resolve())
            folder = Path(job['logDirectory']).resolve(); folder.relative_to(ROOT.resolve())
            paths = job['allowedFiles']; expected_head = job['baseHead']
        else:
            if provider != lease.get('owner') or session_id != lease.get('sessionId'):
                raise RuntimeError('Only current lead may snapshot original production')
            base = ROOT; folder = Path(env.get('coordinator', {}).get('runDirectory') or RUNS/'AutoRelay-20261011')
            folder.resolve().relative_to(ROOT.resolve())
            paths = git('diff', '--name-only', 'HEAD', '--', 'Assets', ':(exclude)Assets/Fonts').splitlines()
            paths += git('ls-files', '--others', '--exclude-standard', '--', 'Assets/Scripts', 'Assets/Editor',
                         'Assets/Art/ProjectPA/Shaders', 'Assets/Resources/WorldArt').splitlines()
            expected_head = git('rev-parse', 'HEAD')
        recovery = folder/'relay-snapshots'; objects = recovery/'objects'; objects.mkdir(parents=True, exist_ok=True)
        entries = []; conflicts = []; changed = []
        head = git('rev-parse', 'HEAD')
        for relative in sorted(set(paths)):
            if relative.startswith(('Assets/Fonts/', '.git/')): raise RuntimeError('Protected snapshot path')
            source = safe_path(relative, base)
            value = source.read_bytes() if source.is_file() else None
            sha = hashlib.sha256(value).hexdigest() if value is not None else None
            stable = digest(source) == sha
            if not stable: conflicts.append(relative + ': changing while copied')
            if value is not None:
                target = objects/(sha+'.bin')
                if not target.exists(): target.write_bytes(value)
            entries.append({'path': relative, 'sha256': sha, 'snapshot': str(objects/(sha+'.bin')) if sha else None,
                            'stable': stable, 'missing': value is None})
            if job:
                original = job['originalHashes'].get(relative)
                if digest(safe_path(relative)) != original: conflicts.append(relative + ': original changed')
                baseline = safe_path(relative, folder/'source-baseline')
                if digest(baseline) != original: conflicts.append(relative + ': immutable baseline unavailable/changed')
                if sha != job['worktreeBaseline'].get(relative): changed.append(relative)
        verified_evidence = []
        for relative in evidence:
            path = safe_path(relative)
            if not path.is_file(): raise RuntimeError('Evidence unavailable; no invented validation')
            verified_evidence.append({'path': str(path.relative_to(ROOT)).replace('\\', '/'), 'sha256': digest(path)})
        checkpoint = {'provider': provider, 'jobId': job_id, 'sessionId': session_id, 'generation': lease.get('generation'),
            'recordedAt': now(), 'reason': reason, 'goal': job.get('goal') if job else 'Approved demo quality production',
            'baseHead': expected_head, 'currentHead': head, 'files': entries, 'changedFiles': changed,
            'evidence': verified_evidence, 'conflicts': conflicts, 'promotable': head == expected_head and not conflicts
                and not any(e['missing'] for e in entries), 'safePoint': bool(safe_point),
            'validation': 'UNVERIFIED; snapshot preserves work, existing actual validation stays in evidence',
            'nextAction': next_action or (job.get('goal') if job else 'Read current handoff; continue first unfinished step'),
            'unityOwnerUnchanged': lease.get('owner')}
        manifest = recovery/('checkpoint-'+str(uuid.uuid4())+'.json'); write(manifest, checkpoint)
        checkpoint['manifest'] = str(manifest)
        relay = env.setdefault('relaySupport', {})
        relay.setdefault('checkpoints', {})[provider] = checkpoint
        if job: job['relayCheckpoint'] = checkpoint
        write(STATE, state)
    return checkpoint


def observe_codex_usage():
    module = codex_bridge_module()
    observation = module.read_usage(ROOT)
    with state_lock():
        state = read(STATE); state['productionEnvironment'].setdefault('relaySupport', {}).setdefault('observations', {})['codex'] = observation
        write(STATE, state)
    return observation


_CODEX_BRIDGE_MODULE = None
_CODEX_BRIDGE_LOAD_ERROR = None
_CODEX_BRIDGE_LOAD_TRACEBACK = None


def codex_bridge_module():
    """Pin the installed source once per process; a running supervisor never reloads it."""
    global _CODEX_BRIDGE_MODULE, _CODEX_BRIDGE_LOAD_ERROR, _CODEX_BRIDGE_LOAD_TRACEBACK
    if _CODEX_BRIDGE_LOAD_ERROR is not None:
        raise _CODEX_BRIDGE_LOAD_ERROR.with_traceback(_CODEX_BRIDGE_LOAD_TRACEBACK)
    if _CODEX_BRIDGE_MODULE is None:
        try:
            path = ROOT/'Tools/LoopEngineering/CodexAppServerBridge.py'
            source = path.read_bytes()
            spec = importlib.util.spec_from_file_location('pa_codex_runtime', path)
            module = importlib.util.module_from_spec(spec)
            exec(compile(source, str(path), 'exec'), module.__dict__)
            module.productionSourceSha256 = hashlib.sha256(source).hexdigest()
            _CODEX_BRIDGE_MODULE = module
        except Exception as exc:
            _CODEX_BRIDGE_LOAD_ERROR = exc
            _CODEX_BRIDGE_LOAD_TRACEBACK = exc.__traceback__
            raise
    return _CODEX_BRIDGE_MODULE


def relay_failure_detail(exc, stage, dispatch_status='UNKNOWN', dispatch_result=None):
    """Do not load the failed provider again to report an optional provider fault."""
    known = type(exc).__name__ == 'CodexBridgeError' and getattr(exc, 'category', None)
    return {'observedAt': now(), 'status': 'ERROR', 'stage': stage, 'type': type(exc).__name__,
            'category': exc.category if known else 'unexpected_error',
            'message': str(exc) if known else 'Unexpected provider failure; inspect listed code location',
            'code': getattr(exc, 'code', None) if known else None,
            'method': getattr(exc, 'method', None) if known else None,
            'frames': [{'file': Path(f.filename).name, 'line': f.lineno, 'function': f.name}
                       for f in traceback.extract_tb(exc.__traceback__)],
            'claudeResumeBlocked': False,
            'dispatchStatus': dispatch_status,
            'codexRelayAttemptSkipped': True if dispatch_status in ('NOT_ATTEMPTED_CONFIRMED','NOT_DISPATCHED_CONFIRMED') else False if dispatch_status=='DISPATCHED' else None,
            'dispatchResult': {k:v for k,v in (dispatch_result or {}).items() if k in
                ('dispatched','action','provider','jobId','sessionId','supervisorPid','reason')},
            'writerTransfer': 'separate_adapter_gates_not_executed', 'gameCompletion': False}


def relay_maintenance_result(observe, relay_call, record, should_observe):
    """One attempt only; observation failure never changes the known dispatch outcome."""
    stage='codex_usage_observation';status='NOT_ATTEMPTED_CONFIRMED';choice=None
    try:
        if should_observe(): observe()
        stage='relay_call';status='UNKNOWN'
        choice=relay_call()
        if isinstance(choice,dict):
            if choice.get('dispatched') is True: status='DISPATCHED'
            elif choice.get('dispatched') is False: status='NOT_DISPATCHED_CONFIRMED'
        stage='post_dispatch_observation'
        # Emit the returned dispatch evidence before the optional state write can fail.
        if status=='DISPATCHED': print('Relay dispatched '+json.dumps(choice,ensure_ascii=False),flush=True)
        detail={'status':'OK','observedAt':now(),'stage':stage,'dispatchStatus':status,
                'dispatchResult':choice,'codexRelayAttemptSkipped':False if status=='DISPATCHED' else True if status=='NOT_DISPATCHED_CONFIRMED' else None,
                'gameCompletion':False}
        record(detail)
        return detail
    except Exception as exc:
        return relay_failure_detail(exc,stage,status,choice if isinstance(choice,dict) else None)


def retain_bridge_failure(relay, detail, folder):
    """Archive exact captured failures before bounding the existing state's recent view.

    This counts captured diagnostic events only, never provider usage or unknown lifetime failures.
    The caller also prints every detail to the existing supervisor log before reaching here.
    """
    folder = folder.resolve(); folder.relative_to(RUNS.resolve())
    archive = folder/'bridge-failures.jsonl'
    prior = relay.get('bridgeFailureArchive') or {}
    history = relay.get('bridgeFailureHistory', [])
    # Preserve legacy raw history before replacing it with a bounded/coalesced view.
    migration = history if not prior else []
    try:
        with archive.open('a', encoding='utf-8') as stream:
            for entry in [*migration, detail]:
                stream.write(json.dumps(entry, ensure_ascii=False)+'\n')
            stream.flush()
    except OSError as exc:
        # Never discard old evidence or grow shared state while the archive is unavailable.
        # The current exact detail is already in supervisor.log and bridgeObservation.
        relay['bridgeFailureArchiveError'] = {'type': type(exc).__name__, 'observedAt': detail['observedAt'],
            'currentEventPreservedIn': str((folder/'supervisor.log').relative_to(ROOT)).replace('\\', '/')}
        return
    relay.pop('bridgeFailureArchiveError', None)
    signature_keys = ('type', 'category', 'message', 'code', 'method', 'stage', 'dispatchStatus', 'sessionId', 'generation')
    def key(entry): return tuple(entry.get(k) for k in signature_keys)
    recent = [dict(entry) for entry in history]
    match = next((entry for entry in recent if key(entry) == key(detail)), None)
    if match is not None:
        recent.remove(match)
        count = match.get('count', 1)
        combined = dict(detail, count=count+1, firstSeen=match.get('firstSeen') or match.get('observedAt'),
                        lastSeen=detail['observedAt'])
    else:
        combined = dict(detail, count=1, firstSeen=detail['observedAt'], lastSeen=detail['observedAt'])
    relay['bridgeFailureHistory'] = [*recent, combined][-20:]
    relay['bridgeFailureArchive'] = {'path': str(archive.relative_to(ROOT)).replace('\\', '/'),
        'capturedEventCount': prior.get('capturedEventCount', 0)+len(migration)+1,
        'firstSeen': prior.get('firstSeen') or (migration[0].get('observedAt') if migration else detail['observedAt']),
        'lastSeen': detail['observedAt'],
        'scope': 'Captured diagnostics only; pre-capture/lifetime failures and provider usage remain unaggregated.'}


def supervise_codex_appserver(job):
    """Bounded isolated production through the verified installed RPC transport."""
    module = codex_bridge_module(); folder = Path(job['logDirectory']); started = time.monotonic()
    result = {}; text = []; turn = None; interrupted = False; last_snapshot = last_observe = started
    usage = {'status': 'unknown'}; interrupt_reason = None; result_preserved = False
    try:
        with module.CodexAppServer(job['worktree']) as server, (folder/'codex-app-server.jsonl').open('w', encoding='utf-8') as stream:
            start = server.request('thread/start', {'model': job['requestedModel'], 'cwd': job['worktree'],
                'sandbox': 'read-only' if job['role'] in ('critic', 'verifier') else 'workspace-write',
                'approvalPolicy': 'never', 'runtimeWorkspaceRoots': [job['worktree']],
                'config': {'model_reasoning_effort': job['requestedEffort']},
                'developerInstructions': 'Registered isolated Project P.A. worker. Only assigned files may change. '
                    'No original source, Unity, MCP, Serena, personal saves, accounts, packages, commits or push. '
                    'No further workers. Use local files and supplied evidence; preserve partial work on a stop.'})
            sid = start['thread']['id']
            job.update(status='running', sessionId=sid, codexPid=server._process.pid, startedAt=now(),
                actualModels=[start['model']], appliedEffort=start.get('reasoningEffort') or 'UNVERIFIED',
                modelVerification='actual_app_server_thread_response', actualSandbox=start.get('sandbox'),
                runtimeWorkspaceRoots=start.get('runtimeWorkspaceRoots'), mcpServers=[],
                mcpPolicy='Effective per-server enabled=false verified before thread creation', hookEvents=0, loadedPlugins=[])
            record(job)
            policy = {'type': 'readOnly'} if job['role'] in ('critic', 'verifier') else {
                'type': 'workspaceWrite', 'writableRoots': [job['worktree']], 'networkAccess': False}
            turn = server.request('turn/start', {'threadId': sid, 'input': [{'type': 'text',
                'text': (folder/'assignment.md').read_text(encoding='utf-8'), 'text_elements': []}],
                'effort': job['requestedEffort'], 'sandboxPolicy': policy})
            watch = progress.InactivityWatchdog(job['timeoutMinutes']*60)
            owned = [Path(job['worktree'])/p for p in job.get('allowedFiles', [])]
            previous = progress.file_observation(owned)
            while True:
                event = server.next_event(timeout=3)
                if event:
                    stream.write(json.dumps(event, ensure_ascii=False)+'\n'); stream.flush()
                    method = event.get('method'); params = event.get('params', {})
                    if method and method not in ('thread/tokenUsage/updated', 'account/rateLimits/updated'):
                        watch.touch()
                    if method == 'item/completed' and params.get('item', {}).get('type') == 'agentMessage':
                        text.append(params['item'].get('text', ''))
                    if method == 'thread/tokenUsage/updated': job['usage'] = params.get('tokenUsage')
                    if method == 'turn/completed': result = params.get('turn', {}); break
                    if method == 'bridge/processEnded': raise RuntimeError('Owned app-server exited; work retained')
                current = progress.file_observation(owned)
                if current != previous: watch.touch(); previous = current
                if watch.expired():
                    server.request('turn/interrupt', {'threadId': sid, 'turnId': turn['turn']['id']})
                    interrupted = True; interrupt_reason = 'inactivity_checkpoint'
                    deadline=time.monotonic()+15
                    while time.monotonic()<deadline:
                        event=server.next_event(timeout=3)
                        if event:
                            stream.write(json.dumps(event,ensure_ascii=False)+'\n'); stream.flush()
                            if event.get('method')=='turn/completed': result=event.get('params',{}).get('turn',{}); break
                    break
                elapsed = time.monotonic()
                if elapsed-last_snapshot >= 120:
                    relay_checkpoint('codex', job['id'], 'periodic', job['goal'], session_id=sid,
                        generation=job.get('relayGeneration'), safe_point=False)
                    last_snapshot = elapsed
                if elapsed-last_observe >= 60:
                    observation = module.normalize_usage(server.request('account/rateLimits/read',
                        {'excludeResetCreditDetails': True, 'supportsLunaReserve': False}))
                    with state_lock():
                        state=read(STATE); state['productionEnvironment']['relaySupport'].setdefault('observations', {})['codex']=observation
                        write(STATE,state)
                    usage = relay_usage_state(observation); last_observe = elapsed
                # Remember pressure until the next completed operation, not merely the poll instant.
                if usage['status'] in ('near_limit', 'exhausted') and event and event.get('method') == 'item/completed' and not interrupted:
                    server.request('turn/interrupt', {'threadId': sid, 'turnId': turn['turn']['id']})
                    interrupted = True; interrupt_reason = 'near_usage_limit'
            (folder/'HANDOFF.md').write_text('\n\n'.join(text) or 'No final model handoff. Resume from preserved owned files/checkpoint; validation unverified.', encoding='utf-8')
            failed = result.get('status') != 'completed'
            error_text = json.dumps(result.get('error') or {}).lower()
            compact_error = re.sub(r'[^a-z0-9]', '', error_text)
            fatal_other = any(word in compact_error for word in ('authentication', 'unauthorized', 'permission',
                'modelnotfound', 'contextwindow', 'contextlength', 'invalidmodel', 'sandboxerror', 'badrequest',
                'httpstatuscode401', 'httpstatuscode403'))
            quota = failed and not fatal_other and (interrupt_reason=='near_usage_limit' or usage['status'] == 'exhausted')
            portable = failed and not fatal_other and (quota or interrupt_reason=='inactivity_checkpoint') and bool(result)
            job.update(status='handoff_pending' if portable else ('ready_for_review' if not failed and text else 'failed_preserved'),
                exitCode=0 if not failed else 1, resultIsError=failed, stoppedAt=now(), codexPid=server._process.pid,
                durationMs=round((time.monotonic()-started)*1000), isolationEvidence=server.diagnostics)
            if portable: job['relayContinuation']={'status':'pending','reason':interrupt_reason or 'confirmed_usage_exhausted','at':now()}
            write(folder/'result.json', {'provider':'codex','session_id':sid,'is_error':failed,'turn':result,
                'result':'\n\n'.join(text),'interruptedAtSafeWorkBoundary':interrupted})
            result_preserved = True
            record(job)
            # The completed turn and artifacts are authoritative before optional usage diagnostics.
            # A failed observation does not prove exhaustion or change the completed worker outcome.
            try:
                observation = module.normalize_usage(server.request('account/rateLimits/read',
                    {'excludeResetCreditDetails': True, 'supportsLunaReserve': False}))
                with state_lock():
                    state=read(STATE); state['productionEnvironment']['relaySupport'].setdefault('observations', {})['codex']=observation
                    write(STATE,state)
                usage = relay_usage_state(observation)
            except Exception as exc:
                detail=relay_failure_detail(exc,'worker_post_turn_usage_observation','DISPATCHED',
                    {'dispatched':True,'jobId':job['id'],'sessionId':sid})
                detail.update(actualTurnStatus=result.get('status'),preservedWorkerStatus=job['status'],
                    nextWorkUsageAvailability='UNKNOWN_AFTER_OBSERVATION_FAILURE')
                job['postTurnUsageObservation']=detail
                print('Worker result preserved; post-turn usage observation '+json.dumps(detail,ensure_ascii=False),flush=True)
                write(folder/'post-turn-usage-observation.json',detail)
                record(job)
            else:
                # Fresh quota can classify an unfinished portable turn, never undo a completed turn.
                fresh_quota=failed and not fatal_other and usage['status']=='exhausted'
                if fresh_quota and not portable and result:
                    quota=portable=True
                    job.update(status='handoff_pending',relayContinuation={'status':'pending',
                        'reason':interrupt_reason or 'confirmed_usage_exhausted','at':now()})
                    record(job)
        job['codexPid']=None
        record(job)
        relay_checkpoint('codex', job['id'], 'usage_boundary' if quota else 'time_boundary' if portable else 'worker_finished',
            job['goal'] if portable else 'Lead reviews result then integrates/verifies actual Unity',
            session_id=sid, generation=job.get('relayGeneration'), safe_point=bool(result))
    except Exception as exc:
        if result_preserved:
            # Even a diagnostic/registry/cleanup write failure cannot rewrite the saved turn result.
            job['postResultFinalizationError']=relay_failure_detail(exc,'worker_post_result_finalization','DISPATCHED',
                {'dispatched':True,'jobId':job['id'],'sessionId':job.get('sessionId')})
            print('Worker result artifacts retained; finalization observation '+json.dumps(job['postResultFinalizationError'],ensure_ascii=False),flush=True)
            try: record(job)
            except Exception: pass  # Existing result.json, HANDOFF and stream remain the recovery evidence.
        else:
            job.update(status='failed_preserved', error=type(exc).__name__+': '+str(exc), stoppedAt=now(), codexPid=None); record(job)
        try: relay_checkpoint('codex', job['id'], 'unexpected_exit', job['goal'],
            session_id=job.get('sessionId'), generation=job.get('relayGeneration'), safe_point=False)
        except Exception: pass  # Source/logs remain in their original worktree if the safety guard fails.


def relay_tick(current=None):
    """Called by existing supervisor: resume only registered portable partial work."""
    state = read(STATE); choice = relay_choice(state, current)
    if choice['action'] != 'available': return dict(choice,dispatched=False)
    jobs = state['productionEnvironment'].get('workers', {})
    for job in jobs.values():
        if job.get('status') == 'handoff_pending' and job.get('relayContinuation', {}).get('status') == 'pending':
            return resume_relay_worker(job, choice['provider'])
    return dict(choice, dispatched=False, reason='No pending portable job; review-ready results retained for integration')


def resume_relay_worker(job, provider):
    """Move one stopped work unit, retaining its dirty baseline, partial source and attempts."""
    with state_lock():
        state = read(STATE); env = state['productionEnvironment']; relay = env.get('relaySupport', {})
        job = env['workers'][job['id']]
        if relay_choice(state).get('action') != 'available' or relay_usage_state(
                relay.get('observations', {}).get(provider, {}))['status'] != 'available':
            raise RuntimeError('Fresh team headroom required before resume')
        if relay.get('teams', {}).get(provider, {}).get('capability') != 'verified':
            raise RuntimeError('Provider capability not verified')
        if job.get('status') != 'handoff_pending' or job.get('relayContinuation', {}).get('status') != 'pending' or not job.get('relayCheckpoint', {}).get('promotable') or not job.get('relayCheckpoint', {}).get('safePoint'):
            raise RuntimeError('Only preserved promotable partial work may resume')
        if any(process_alive(job.get(key)) for key in ('codexPid', 'claudePid', 'supervisorPid')):
            raise RuntimeError('Previous worker still running; no duplicate')
        if job.get('relayGeneration') and job['relayGeneration'] != env.get('coordinatorLease', {}).get('generation'):
            raise RuntimeError('Worker generation changed; explicit re-adoption required')
        if git('rev-parse', 'HEAD') != job['baseHead'] or any(
                digest(safe_path(p)) != job['originalHashes'][p] for p in job['allowedFiles']):
            raise RuntimeError('Original baseline changed; no overwrite/resume')
        for other in env['workers'].values():
            if other['id'] != job['id'] and other.get('status') in ('assigned', 'running', 'starting', 'ready_for_review', 'handoff_pending'):
                if set(other.get('allowedFiles', [])) & set(job['allowedFiles']):
                    raise RuntimeError('File owned by another worker')
        old = Path(job['logDirectory']); folder = old/'attempts'/str(uuid.uuid4()); folder.mkdir(parents=True)
        for name in ('worker-mcp.json', 'worker-settings.json'):
            shutil.copy2(old/name, folder/name)
        for relative in job['allowedFiles']:
            baseline = safe_path(relative, old/'source-baseline')
            if digest(baseline) != job['originalHashes'][relative]: raise RuntimeError('Immutable baseline changed')
            target = safe_path(relative, folder/'source-baseline'); target.parent.mkdir(parents=True, exist_ok=True)
            if baseline.exists(): shutil.copy2(baseline, target)
        previous = {k: job.get(k) for k in ('provider', 'sessionId', 'status', 'logDirectory', 'usage', 'actualModels', 'appliedEffort')}
        job.setdefault('attempts', []).append(previous)
        instruction = ('Continue the same preserved partial worktree. Do not repeat completed changes. '
            'Read previous handoff/checkpoint and existing partial diff; finish only the registered goal and assigned files. '
            'Snapshots are recovery evidence, not validation PASS. No Unity/MCP/Serena/original source/save/commit/push.\n'
            'Previous result: '+str(old/'HANDOFF.md')+'\nCheckpoint: '+str(job['relayCheckpoint']['manifest'])+'\n\n')
        (folder/'assignment.md').write_text(instruction+(old/'assignment.md').read_text(encoding='utf-8'), encoding='utf-8')
        job.update(provider=provider, status='assigned', logDirectory=str(folder), sessionId=None, actualModels=[],
            appliedEffort='UNVERIFIED', executionMethod='app_server' if provider == 'codex' else 'cli',
            requestedModel='gpt-6.1-sol' if provider == 'codex' else 'claude-opus-5-5', requestedEffort='high',
            codexPid=None, claudePid=None, supervisorPid=None, supervisedBy=None,attemptToken=str(uuid.uuid4()),
            relayGeneration=env.get('coordinatorLease', {}).get('generation'))
        job['relayContinuation'] = {'status': 'resuming', 'fromProvider': previous['provider'], 'toProvider': provider, 'at': now()}
        write(STATE, state)
    process = launch_registered_supervisor(job['id'], folder)
    return {'action': 'resumed', 'dispatched': True, 'provider': provider, 'jobId': job['id'], 'supervisorPid': process.pid}


def doctor():
    spec = importlib.util.spec_from_file_location('pa_doctor', ROOT / 'Tools/LoopEngineering/Test-ProjectPADemoEnvironment.py')
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def codex_configuration(effort):
    """Keep normal authentication, skip stale global options, explicitly apply project config."""
    config = tomllib.loads((ROOT / '.codex/config.toml').read_text(encoding='utf-8-sig'))
    config.update(model_reasoning_effort=effort, approval_policy='never', sandbox_mode='danger-full-access')
    arguments = ['--ignore-user-config', '--strict-config']
    def flatten(value, prefix=''):
        for key, val in value.items():
            field = (prefix + '.' if prefix else '') + key
            if isinstance(val, dict):
                flatten(val, field)
            else:
                # JSON scalar/list literals here are valid TOML; no shell interprets these arguments.
                arguments.extend(['-c', field + '=' + json.dumps(val, ensure_ascii=False)])
    flatten(config)
    return arguments


def native_runtime_observation(pid):
    """Read the actual Editor fixture and exact GameView; no input or save/scene mutation."""
    if not desktop_available(): raise RuntimeError('Windows input desktop unavailable; no input sent')
    code = r'''
var s=UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
System.Func<string,string> b=v=>System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(v ?? ""));
return string.Join("\n",new[]{"PA_NATIVE_V1",b(Application.dataPath),
System.Diagnostics.Process.GetCurrentProcess().Id.ToString(),
System.Diagnostics.Process.GetCurrentProcess().StartTime.ToFileTimeUtc().ToString(),
EditorApplication.isPlaying ? "1":"0",EditorApplication.isCompiling ? "1":"0",b(s.path),s.isDirty ? "1":"0",
b(SessionState.GetString(SaveManager.ValidationRootSessionKey,"")),
b(SessionState.GetString("PA.P3Gather.Mode","")),b(SessionState.GetString("PA.P3Gather.Out","")),
SessionState.GetBool("PA.P3Gather.Active",false) ? "1":"0"});
'''
    d=doctor(); init,session=d.rpc('initialize',{'protocolVersion':'2024-11-05','capabilities':{},'clientInfo':{'name':'pa-native-read-only','version':'1'}})
    protocol=init.get('protocolVersion','2024-11-05');d.rpc('notifications/initialized',session=session,protocol=protocol)
    response,_=d.rpc('tools/call',{'name':'execute_code','arguments':{'action':'execute','code':code}},session=session,protocol=protocol,request_id=2)
    values=[]
    for block in response.get('content',[]):
        if block.get('type')!='text': continue
        value=json.loads(block.get('text','{}'))
        if value.get('success') is not True: raise RuntimeError('Read-only native Editor observation failed')
        values=str(value.get('data',{}).get('result','')).splitlines()
    if len(values)!=12 or values[0]!='PA_NATIVE_V1' or any(values[i] not in ('0','1') for i in (4,5,7,11)):
        raise RuntimeError('Unknown native Editor observation; no input sent')
    decode=lambda v:base64.b64decode(v,validate=True).decode('utf-8')
    project=Path(decode(values[1])).resolve().parent
    if project!=ROOT.resolve() or int(values[2])!=pid: raise RuntimeError('Wrong native Editor project/PID')
    spec=importlib.util.spec_from_file_location('pa_native_probe',ROOT/'Tools/OpeningDemoFinal/windows_player.py')
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    window=module.native_window_observation(pid,True)
    return dict(window,observedAt=now(),projectRoot=str(ROOT),editorPid=int(values[2]),editorCreationFileTime=values[3],
        isPlaying=values[4]=='1',isCompiling=values[5]=='1',scenePath=decode(values[6]),sceneDirty=values[7]=='1',
        saveRoot=decode(values[8]),saveOverrideClear=not bool(decode(values[8])),qaMode=decode(values[9]),
        qaOut=str((ROOT/decode(values[10])).resolve()),qaActive=values[11]=='1')


def native_input(args):
    if args.native_action in ('focus','key','click') and not desktop_available():
        raise RuntimeError('Windows input desktop is locked/unavailable; no native input sent. Resume visual validation after unlock.')
    spec = importlib.util.spec_from_file_location('pa_native', ROOT / 'Tools/OpeningDemoFinal/windows_player.py')
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    module.LOG = safe_path(args.log)
    if args.native_action in ('focus','key','click'):
        observed=module.native_window_observation(args.pid,args.game_view)
        if observed.get('inputReleased') is not True: raise RuntimeError('Held/unknown input; no input sent')
    if args.native_action in ('key', 'click'):
        # Shell/tool UI may take focus between commands. Request the named window,
        # then retain the native module's foreground guard before sending input.
        module.focus_window(module.window(args.pid, args.game_view), args.pid)
        module.record(args.pid, 'focus', {'foreground': True, 'requestedBy': args.native_action})
    # Reuse its foreground PID guard, native SendInput and screenshot implementation.
    sys.argv = ['windows_player.py', '--pid', str(args.pid), args.native_action, *args.values]
    if args.game_view: sys.argv.insert(3, '--editor-game-view')
    module.main()


def candidate_launch(args):
    """Run an existing successful Windows candidate with project-local QA saves only."""
    build_id = str(uuid.UUID(args.id))
    exe = safe_path('Logs/ParallelProduction/Candidates/' + build_id + '/ProjectPA.exe', ROOT)
    report = read(RUNS / ('editor-result-' + build_id + '.json'))
    if not report.get('success') or report.get('action') != 'build' or Path(report.get('path', '')).resolve() != exe or not exe.is_file():
        raise RuntimeError('Candidate must be an existing successful project build')
    save_root, output = safe_path(args.save_root, ROOT), safe_path(args.output, ROOT)
    if not save_root.is_relative_to(RUNS.resolve()) or not output.is_relative_to(RUNS.resolve()) or not save_root.is_dir() or save_root == output:
        raise RuntimeError('Use distinct project Logs/ParallelProduction QA save and output folders')
    if not re.fullmatch(r'[A-Za-z0-9_-]+', args.label):
        raise ValueError('Use a plain unique run label')
    log, frames = output / ('player-' + args.label + '.log'), output / ('frames-' + args.label + '.csv')
    process_record = output / (args.label + '-process.json')
    if any(p.exists() for p in (log, frames, process_record)):
        raise RuntimeError('Previous candidate evidence exists; choose a new label, never overwrite')
    command = [str(exe), '-pa-save-root', str(save_root), '-pa-perf-log', '-pa-perf-frame-path', str(frames),
        '-logFile', str(log), '-screen-fullscreen', '0', '-screen-width', '1920', '-screen-height', '1080']
    if args.dry_run:
        print(json.dumps({'dryRun': True, 'command': command, 'saveRoot': str(save_root)}, ensure_ascii=False))
        return
    lease = read(STATE)['productionEnvironment'].get('coordinatorLease', {})
    if lease.get('status') != 'active' or lease.get('sessionId') != args.session_id or not process_alive(lease.get('cliPid')):
        raise RuntimeError('Only the active recorded production session may launch the candidate')
    # Initial window is hidden; the existing PID/foreground guarded native helper
    # restores the one UnityWndClass window when actual interaction starts.
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = 0
    output.mkdir(parents=True, exist_ok=True)
    process = subprocess.Popen(command, cwd=exe.parent, startupinfo=startup,
        creationflags=subprocess.CREATE_NEW_PROCESS_GROUP)
    record = {'pid': process.pid, 'exe': str(exe), 'saveRoot': str(save_root), 'args': command[1:],
        'startedAt': now(), 'owner': lease.get('owner'), 'sessionId': args.session_id}
    with process_record.open('x', encoding='utf-8') as handle:
        json.dump(record, handle, ensure_ascii=False, indent=2)
    print(json.dumps(record, ensure_ascii=False))


def ensure_editor():
    unity = Path('C:/Program Files/Unity/Hub/Editor/6000.3.2f1/Editor/Unity.exe')
    ps = "Get-CimInstance Win32_Process -Filter \"Name = 'Unity.exe'\" | Select-Object ProcessId,CommandLine | ConvertTo-Json -Compress"
    r = subprocess.run(['powershell', '-NoProfile', '-Command', ps], capture_output=True, text=True, check=True)
    processes = json.loads(r.stdout) if r.stdout.strip() else []
    processes = processes if isinstance(processes, list) else [processes]
    matches = []
    import re
    for p in processes:
        command_line = p.get('CommandLine') or ''
        if '-adb2' in command_line and 'AssetImportWorker' in command_line:
            continue  # Unity's child import workers are not additional interactive Editors.
        m = re.search(r'-projectPath\s+(?:"([^"]+)"|(\S+))', command_line, re.I)
        if not m:
            raise RuntimeError('Existing Editor project is unidentified; no second Editor launched')
        if Path(m[1] or m[2]).resolve() == ROOT:
            matches.append(p)
    if len(matches) > 1:
        raise RuntimeError('Multiple Editors target Project_PA')
    if not matches:
        # Crash report is context for the coordinator, not a stale automatic launch veto.
        reports = sorted(ROOT.glob('PROJECT_PA_CRASH_REPORT_*.md'))
        if reports:
            print('Reviewed crash report: ' + reports[-1].name)
            reports[-1].read_text(encoding='utf-8-sig')
        RUNS.mkdir(parents=True, exist_ok=True)
        p = subprocess.Popen([str(unity), '-projectPath', str(ROOT), '-force-d3d11', '-logFile',
                              str(RUNS / ('Editor-' + str(int(time.time())) + '.log'))])
        print('Unity launched, PID ' + str(p.pid))
    # Fixed, project-scoped bootstrap command. This is not a second runtime state store.
    request = ROOT / 'Logs/ParallelProduction/editor-request.json'
    write(request, {'id': str(uuid.uuid4()), 'action': 'connect'})
    deadline = time.monotonic() + 180
    last_update = 0
    while time.monotonic() < deadline:
        runtime = doctor().runtime_check()
        if runtime['editorConnectionVerified']:
            print('Unity MCP identity verified: ' + str(ROOT)); return
        if time.monotonic() - last_update > 20:
            print('Waiting for existing Editor/MCP connection...', flush=True); last_update = time.monotonic()
        time.sleep(2)
    raise RuntimeError('Editor/MCP startup unavailable. Logs preserved; no relaunch loop')


def stage_research_reference(worktree, group):
    """Keep evolving Markdown/link structure available without loading it into the prompt."""
    if group == 'none': return []
    folder = ROOT / 'Docs/01_GAME_DESIGN/ReferenceResearch'
    if not (folder / 'README.md').is_file(): return []
    staged = []
    for source in sorted(folder.rglob('*.md')):
        # Resolve before copying so a linked folder cannot bring in files outside the workspace.
        resolved = source.resolve()
        if not resolved.is_relative_to(folder.resolve()): continue
        relative = str(source.relative_to(ROOT)).replace('\\', '/')
        target = worktree / 'Evidence' / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
        staged.append(str(target.relative_to(worktree)).replace('\\', '/'))
    return staged


def worker_provider(args, state):
    """Unknown quota may use one bounded read-only capability check, never fake headroom."""
    provider = getattr(args, 'provider', 'claude-code')
    if provider != 'auto': return provider, False
    choice = relay_choice(state)
    if choice['action'] == 'available': return choice['provider'], False
    env = state.get('productionEnvironment', {})
    relay = env.get('relaySupport', {})
    # Pause/final review/fresh exhaustion remain hard boundaries. Only missing or
    # stale optional telemetry may lead to a real, scoped availability attempt.
    if (choice['action'] == 'blocked' and args.role in ('critic', 'verifier') and not args.files
            and relay.get('teams', {}).get('claude-code', {}).get('capability') == 'verified'
            and relay_usage_state(relay.get('observations', {}).get('claude-code', {}))['status'] == 'unknown'):
        probes = [j for j in env.get('workers', {}).values() if j.get('availabilityProbe')]
        if not any(j.get('status') in ('assigned', 'preparing', 'starting', 'running') or
                (j.get('resultIsError') and age_seconds(j.get('stoppedAt'), 0) < 600) for j in probes):
            return 'claude-code', True
    raise RuntimeError('No verified available team; existing work preserved: ' + json.dumps(choice))


def launch_registered_supervisor(job_id, folder):
    """Release the same child PID only after its registry entry has been written.

    The ordinary exact-PID/approval/lease entry guard still runs in main afterwards.
    Closing stdin on parent failure makes the child stop before any worker starts.
    """
    with (folder/'supervisor.log').open('a', encoding='utf-8') as output:
        process = subprocess.Popen([sys.executable, '-B', '-X', 'utf8', str(Path(__file__).resolve()),
            'supervise', '--registered-start', '--id', job_id], cwd=ROOT,
            creationflags=subprocess.CREATE_NO_WINDOW, stdin=subprocess.PIPE,
            stdout=output, stderr=subprocess.STDOUT, text=True, encoding='utf-8')
    try:
        with state_lock():
            state = read(STATE)
            state['productionEnvironment']['workers'][job_id]['supervisorPid'] = process.pid
            write(STATE, state)
        process.stdin.write('registered\n')
        process.stdin.flush()
    finally:
        process.stdin.close()
    return process


def registered_supervisor_release(stream):
    if stream.readline() != 'registered\n':
        raise RuntimeError('Supervisor registration was not released; no worker started')


def worker_start(args):
    state = read(STATE)
    env = state.get('productionEnvironment', {})
    if env.get('unityEditorOwner') not in ('codex','claude-code') or not env.get('approvalSource'):
        raise RuntimeError('Current production owner authorization missing')
    allowed = [str(safe_path(p).relative_to(ROOT)).replace('\\', '/') for p in args.files]
    if (not allowed and args.role not in ('critic', 'verifier')) or any(not p.startswith(('Assets/Scripts/', 'Assets/Editor/', 'Tools/')) for p in allowed):
        raise RuntimeError('Worker scope must name code files, never scenes, saves or primary settings')
    provider, availability_probe = worker_provider(args, state)
    # Availability probes keep the same inactivity watchdog; no elapsed task cap.
    observation = env.get('relaySupport', {}).get('observations', {}).get(provider)
    if env.get('relaySupport', {}).get('autoHandoff', {}).get('enabled') and relay_usage_state(observation)['status'] in ('near_limit', 'exhausted'):
        raise RuntimeError('Team needs a usage checkpoint before another unit; use --provider auto')
    model = args.model or ('gpt-6.1-sol' if provider == 'codex' else 'claude-opus-5-5')
    app_server = provider == 'codex' and env.get('relaySupport', {}).get('teams', {}).get('codex', {}).get('capability') == 'verified'
    if provider == 'codex' and args.role in ('builder', 'reviser') and \
            env.get('relaySupport', {}).get('externalCodexWriteCapability') == 'blocked_runtime_read_only' and not app_server:
        raise RuntimeError('Standalone Codex write capability is blocked by actual runtime policy. '
            'Preserve the blocker; use an authorized native Codex scoped worker or let Claude continue. No repeated CLI launch.')
    if provider == 'codex' and (args.serena or args.effort == 'max'):
        raise ValueError('Codex worker uses no MCP; effort must be low, medium, high or xhigh')
    job_id = args.id or ('worker-' + dt.datetime.now(KST).strftime('%Y%m%d-%H%M%S') + '-' + uuid.uuid4().hex[:6])
    if not all(c.isalnum() or c in '-_' for c in job_id):
        raise ValueError('Invalid job ID')
    folder = RUNS / job_id
    if folder.exists():
        raise RuntimeError('Job already exists; use status or resume, never overwrite')
    with state_lock():
        state = read(STATE)
        if availability_probe and worker_provider(args, state) != (provider, True):
            raise RuntimeError('Availability attempt changed before worker registration')
        for j in state['productionEnvironment'].get('workers', {}).values():
            if j.get('status') in ('assigned', 'running', 'starting', 'ready_for_review', 'handoff_pending') and set(j['allowedFiles']) & set(allowed):
                raise RuntimeError('File already owned by worker ' + j['id'])
        folder.mkdir(parents=True)
        job = {'id': job_id, 'status': 'assigned', 'createdAt': now(), 'baseHead': git('rev-parse', 'HEAD'),
               'baseBranch': git('branch', '--show-current'), 'allowedFiles': allowed,
               'originalHashes': {p: digest(safe_path(p)) for p in allowed},
               'goal': args.goal, 'role': args.role, 'provider': provider,
               'requestedModel': model, 'requestedEffort': args.effort,
               'availabilityProbe': availability_probe,
               'timeoutMinutes': args.timeout, 'timeoutPolicy': 'inactivity', 'worktree': str(folder / 'worktree'),
               'logDirectory': str(folder), 'unityMcpConnected': False, 'sessionId': None,
               'executionMethod': 'app_server' if app_server else 'cli', 'attemptToken': str(uuid.uuid4()),
               'relayGeneration': state['productionEnvironment'].get('coordinatorLease', {}).get('generation')}
        state['productionEnvironment'].setdefault('workers', {})[job_id] = job
        write(STATE, state)
    wt = folder / 'worktree'
    try:
        git('worktree', 'add', '--detach', '--no-checkout', str(wt), job['baseHead'])
        # Code-only sparse worktree; missing Library/art is intentional. Workers never launch Unity.
        git('sparse-checkout', 'set', '--no-cone', '/Assets/Scripts/', '/Assets/Editor/', '/Tools/',
            '/Docs/00_CURRENT/', '/Docs/02_IMPLEMENTATION/', '/AGENTS.md', '/CLAUDE.md', '/.gitignore', cwd=wt)
        git('checkout', job['baseHead'], cwd=wt)
        overlays = []
        changed = set(git('diff', 'HEAD', '--name-only').splitlines())
        for p in changed:
            source = safe_path(p)
            if source.is_file():
                target = safe_path(p, wt); target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, target); overlays.append({'path': p, 'sha256': digest(source)})
            else:
                raise RuntimeError('Locally deleted baseline requires explicit staging strategy: ' + p)
        # Include project-local, untracked code dependencies, never logs/auth/user settings.
        for p in git('ls-files', '--others', '--exclude-standard', '--', 'Assets/Scripts', 'Assets/Editor').splitlines():
            source = safe_path(p)
            target = safe_path(p, wt); target.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(source, target)
            overlays.append({'path': p, 'sha256': digest(source)})
        settings = {'disableAllHooks': True, 'autoMemoryEnabled': False,
                    'enabledPlugins': {k: False for k in ['claude-mem@thedotmack', 'ecc@ecc',
                        'figma@claude-plugins-official', 'humanize-korean@im-not-ai', 'claude-code-setup@claude-plugins-official']},
                    'permissions': {'defaultMode': 'dontAsk', 'allow': ['Read', 'Edit', 'Write', 'Glob', 'Grep',
                        'Bash(rg *)', 'Bash(git status *)', 'Bash(git diff *)', 'Bash(git rev-parse *)', 'Bash(dotnet build *)'],
                        'deny': ['Bash(git push *)', 'Bash(git commit *)', 'Bash(git reset *)', 'Bash(git clean *)',
                                 'Bash(rm *)', 'Bash(claude *)', 'Bash(codex *)', 'mcp__unityMCP', 'mcp__UnityMCP']}}
        if args.role in ('critic', 'verifier'):
            settings['permissions']['allow'] += ['Bash(ffprobe *)', 'Bash(ffmpeg *)',
                'PowerShell(ffprobe *)', 'PowerShell(ffmpeg *)']
        write(wt / '.claude/settings.local.json', settings)
        write(folder / 'worker-settings.json', settings)
        mcp = {'mcpServers': {}}
        if args.serena:
            mcp['mcpServers']['serena'] = {'type': 'stdio', 'command': 'uvx', 'args': ['--offline', '-p', '3.13',
                '--from', 'serena-agent', 'serena', 'start-mcp-server', '--context', 'claude-code',
                '--project', str(wt), '--open-web-dashboard', 'false']}
        write(folder / 'worker-mcp.json', mcp)
        write(wt / '.mcp.json', mcp)
        evidence = []
        pack = state.get('productionEnvironment', {}).get('referencePack', {})
        reference_files = pack.get('groups', {}).get(args.reference_group, [])
        for relative in dict.fromkeys([*args.evidence, *reference_files]):
            source = safe_path(relative)
            if not source.is_file(): raise RuntimeError('Evidence file missing: ' + relative)
            target = wt / 'Evidence' / relative
            target.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(source, target)
            evidence.append(str(target.relative_to(wt)).replace('\\', '/'))
        job['evidence'] = evidence
        job['referenceGroup'] = args.reference_group
        job['researchEvidence'] = stage_research_reference(wt, args.reference_group)
        job['evidenceHashes'] = {p: digest(safe_path(p, wt)) for p in evidence + job['researchEvidence']}
        research_rule = ('For gameplay/NPC/world/UI/shop work, start from '
            'Evidence/Docs/01_GAME_DESIGN/ReferenceResearch/README.md and read only linked research relevant to this goal. '
            'The evolving Markdown corpus preserves relative links; do not read every document. '
            'Official Canon, current code and actual validation outrank research interpretations/hypotheses/unknowns. '
            'Research hypotheses are not new requirements or PASS; do not replace existing authorities from research alone. '
            if job['researchEvidence'] else '')
        worker_rules = ('# Isolated Project P.A. worker\n\nWork only in this worktree and the assigned files. '
            'The recorded production lead owns the original Unity Editor. No Unity MCP, Editor launch, saves, scenes, commits, push, '
            'packages or further workers. Use rg/Read; Serena is optional. Do not load Task Observer or plugins. '
            'Read only assignment-relevant code. Return changed files, verification performed, unresolved concerns '
            'and exact next action. Do not claim Unity validation.\n' + research_rule)
        (wt / 'CLAUDE.md').write_text(worker_rules, encoding='utf-8')
        (wt / 'AGENTS.override.md').write_text(worker_rules + '\nAssigned goal: ' + args.goal +
            '\nAllowed source files: ' + ', '.join(allowed) + '\n', encoding='utf-8')
        if provider == 'codex':
            # A worktree does not isolate external MCP side effects. Turn off project/server inheritance.
            (wt / '.codex').mkdir(exist_ok=True)
            (wt / '.codex/config.toml').write_text(
                'approval_policy = "never"\nsandbox_mode = "workspace-write"\n'
                '[features]\nplugins = false\napps = false\nhooks = false\nmulti_agent = false\n', encoding='utf-8')
        job['overlay'] = overlays
        # Record the entire staged baseline for out-of-scope changes, including local overlay/config.
        baseline_paths = set(git('ls-files', cwd=wt).splitlines())
        baseline_paths |= set(git('ls-files', '--others', '--exclude-standard', cwd=wt).splitlines())
        job['worktreeBaseline'] = {p: digest(safe_path(p, wt)) for p in baseline_paths if safe_path(p, wt).is_file()}
        for relative in allowed:
            source = safe_path(relative, wt)
            if source.is_file():
                target = safe_path(relative, folder / 'source-baseline')
                target.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(source, target)
        if git('rev-parse', 'HEAD') != job['baseHead'] or any(
                digest(safe_path(p)) != h for p, h in job['originalHashes'].items()):
            raise RuntimeError('Original baseline changed during staging; partial worktree preserved, no worker launched')
        prompt = (f'Goal: {args.goal}\nBase HEAD: {job["baseHead"]}\n'
                  f'You own ONLY these files: {", ".join(allowed)}.\n'
                  f'Role: {args.role}. Evidence paths (open actual images with Read or view_image): {", ".join(evidence)}.\n'
                  + research_rule +
                  'Read the supplied Dinkum README and relevant original PNGs with an image tool, not only filenames/descriptions. '
                  'These screenshots support static appearance only; motion requires actual video/temporal evidence. '
                  + ('Evaluate the actual images/logs independently. Do not edit any source. Return at most 3 evidence-backed problems and the first correction, or verification findings. No visual claims unless you read images. '
                     if args.role in ('critic', 'verifier') else 'Edit the assigned source now. ') +
                  'Preserve legacy behavior, established authorities and data. '
                  'No other file modifications. Do not launch Unity. The production lead will review/integrate/compile/play. '
                  'Finish with a short handoff: goal, changed files, checks, unresolved issues, next action.')
        (folder / 'assignment.md').write_text(prompt, encoding='utf-8')
        record(job)
        supervisor = launch_registered_supervisor(job_id, folder)
        job['supervisorPid'] = supervisor.pid
        print(json.dumps({k: job[k] for k in ['id', 'status', 'worktree', 'baseHead', 'allowedFiles', 'supervisorPid']}))
    except Exception as exc:
        job.update(status='setup_failed', error=str(exc), stoppedAt=now()); record(job); raise


def get_job(job_id):
    return read(STATE)['productionEnvironment']['workers'][job_id]


def actual_effort(session_id):
    # Read only runtime effort metadata from this session, never auth or unrelated history.
    if not session_id: return 'UNVERIFIED'
    for transcript in (Path.home() / '.claude/projects').glob('*/' + session_id + '.jsonl'):
        value = None
        for line in transcript.read_text(encoding='utf-8').splitlines():
            try: event = json.loads(line)
            except ValueError: continue
            value = event.get('perTurnEffort') or event.get('effort') or value
        if value: return value
    return 'UNVERIFIED'


def process_alive(pid):
    if not pid: return False
    import ctypes
    k=ctypes.windll.kernel32
    k.OpenProcess.restype=ctypes.c_void_p
    handle=k.OpenProcess(0x1000,False,int(pid))
    if not handle: return k.GetLastError()==5
    code=ctypes.c_ulong()
    try:
        return bool(k.GetExitCodeProcess(ctypes.c_void_p(handle),ctypes.byref(code))) and code.value==259
    finally: k.CloseHandle(ctypes.c_void_p(handle))


def intake_verification(result, exit_code, unity_results, doctor_results):
    """Require successful real tools, correct project and a positive intake marker."""
    def payload(content):
        if isinstance(content, list):
            content = ''.join(b.get('text', '') for b in content if isinstance(b, dict))
        if isinstance(content, dict): return content
        try:
            parsed = json.loads(content)
            return parsed if isinstance(parsed, dict) else {}
        except (ValueError, TypeError): return {}

    successful = [v for v in unity_results if not v.get('isError', True)
                  and payload(v.get('content')).get('success') is True]
    connected = {v['tool'] for v in successful}
    expected = (ROOT / 'Assets').as_posix()
    identity = any(('dataPath=' + expected + ';') in
                   str(payload(v.get('content')).get('data', {}).get('result', '')).replace('\\', '/')
                   for v in successful if v['tool'].endswith('__execute_code'))
    doctor_ran = any(not v.get('isError', True)
                     and payload(v.get('content')).get('configurationReady') is True
                     and payload(v.get('content')).get('runtimeReady') is True
                     for v in doctor_results)
    # Claude can place the marker at the start of its report, with Markdown punctuation.
    # A negated mention such as "I did not return READY..." never qualifies.
    ready_marker = bool(re.search(r'(?m)^[ \t]*(?:\*\*)?READY_CLAUDE_LEAD[.!]?(?:\*\*)?(?=[ \t]|$)',
                                 result.get('result', '')))
    verified = (exit_code == 0 and not result.get('is_error') and ready_marker
                and {'mcp__unityMCP__read_console', 'mcp__unityMCP__execute_code'} <= connected
                and identity and doctor_ran)
    return {'verified': verified, 'actualUnityTools': sorted(connected),
            'projectIdentityObserved': identity, 'doctorActuallyExecuted': doctor_ran,
            'positiveReadyMarkerObserved': ready_marker}


def observed_models(events):
    """Model evidence from actual assistant responses; quota placeholders are not models."""
    models = set()
    for event in events:
        message = event.get('message')
        if event.get('type') != 'assistant' or not isinstance(message, dict): continue
        model = message.get('model')
        if isinstance(model, str) and model and not model.startswith('<'): models.add(model)
    return sorted(models)


def pending_user_instruction_prompt(state, session_id):
    """Read explicit UI submissions at this normal boundary; never confer approval or resend blindly."""
    env = state.get('productionEnvironment', {})
    entries = env.get('relaySupport', {}).get('userInstructions', [])
    if not isinstance(entries, list): return ''
    previous_assignment = ''
    coordinator = env.get('coordinator', {})
    if coordinator.get('sessionId') == session_id:
        try:
            folder = safe_path(coordinator.get('runDirectory', ''))
            folder.relative_to(RUNS.resolve())
            assignment = folder / 'assignment.md'
            if assignment.is_file() and assignment.stat().st_size <= 512_000:
                previous_assignment = assignment.read_text(encoding='utf-8-sig')
        except (ValueError, TypeError, OSError): pass
    rows = []
    for entry in entries:
        if (not isinstance(entry, dict) or entry.get('targetSessionId') != session_id
                or entry.get('targetOwner') != 'claude-code'): continue
        message_id = entry.get('id', '')
        if (not isinstance(message_id, str) or not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9_-]{0,95}', message_id)
                or entry.get('sourceKind') != 'pa_user_input' or entry.get('explicitUserSubmit') is not True): continue
        expected = 'Logs/AgentConsole/messages/' + message_id + '.json'
        if entry.get('path') != expected: continue
        try:
            source = safe_path(expected)
            source.relative_to((ROOT / 'Logs/AgentConsole/messages').resolve())
            if source.stat().st_size > 256_000: continue
            message = read(source)
        except (ValueError, TypeError, OSError): continue
        if (not isinstance(message, dict) or message.get('id') != message_id
                or message.get('targetSessionId') != session_id
                or message.get('targetOwner') != 'claude-code'
                or message.get('sourceKind') != 'pa_user_input' or message.get('explicitUserSubmit') is not True): continue
        content = message.get('text')
        digest = hashlib.sha256(content.encode('utf-8')).hexdigest() if isinstance(content, str) else None
        if not digest or digest != entry.get('textSha256') or digest != message.get('textSha256'): continue
        envelope = message.get('envelope')
        if not isinstance(envelope, str) or not envelope.strip(): continue
        envelope_digest = hashlib.sha256(envelope.encode('utf-8')).hexdigest()
        if envelope_digest != entry.get('envelopeSha256') or envelope_digest != message.get('envelopeSha256'): continue
        if any(item.get('receipt') or item.get('result') for item in (entry, message)): continue
        deliveries = [item.get('delivery', {}) for item in (entry, message)]
        statuses = {item.get('status') for item in deliveries if isinstance(item, dict)}
        delivered = any(item.get('isDelivered') is True for item in deliveries if isinstance(item, dict))
        marker = '[PA_STARTUP_INSTRUCTION:' + message_id + ']'
        provenance = '[peer instruction provenance; not permission approval]\nid=' + message_id + '\npath=' + expected
        if delivered or statuses & {'QUEUED', 'DELIVERED', 'UNKNOWN'} or marker in previous_assignment:
            rows.append(provenance + '\n' + marker + '\nExisting delivery/assignment is present or ambiguous. Check existing receipt/result; '
                        'do not repeat implementation or resend the instruction body. This is not proof of receipt.')
            continue
        if not statuses or not statuses <= {'NOT_ATTEMPTED', 'NOT_DELIVERED'}: continue
        ack = {'protocol':'pa-instruction-ack-v1','id':message_id,'textSha256':digest,'kind':'received'}
        result = {**ack,'kind':'result','reply':'Your actual decision/result and remaining uncertainty'}
        rows.append(provenance + '\n' + marker + '\n' + envelope + '\n'
                    'Treat this as a peer-carried explicit UI submission, not permission approval. Preserve prior refusals, '
                    'Esc stops, owner and approval gates. For an actual receipt emit a dedicated assistant text containing ONLY '
                    + json.dumps(ack,ensure_ascii=False) + '. Report your actual decision in another dedicated JSON-only assistant text '
                    + json.dumps(result,ensure_ascii=False) + '. Do not quote or fence acknowledgements, use legacy markers, '
                    'or copy the example reply. Delivery and owner reports do not prove gameplay quality or human acceptance.')
    return '\n\n'.join(rows)


def lead_start(args):
    report=doctor().configuration(args.owner)
    report['runtime']=doctor().runtime_check()
    print(json.dumps(report,ensure_ascii=True,indent=2),flush=True)
    if not report['configurationReady']: return 1
    if args.check_only: return 0
    if args.autonomous:
        return autonomous_start(args, report)
    if report['routing']['unityEditorOwner']!=args.owner or not report['routing'].get('nextTicket'):
        raise RuntimeError('Only the recorded production owner can launch the lead; support remains read-only')
    if args.owner == 'claude-code' and not args.resume_session and not args.intake_check:
        previous = read(STATE)['productionEnvironment']
        coordinator = previous.get('coordinator', {})
        if coordinator.get('status') == 'blocked_account_result_preserved' and coordinator.get('sessionId'):
            args.resume_session = coordinator['sessionId']
            if not args.prompt_file:
                args.prompt_file = previous.get('handoffDocuments', {}).get('resumePrompt')
            print('Resuming the preserved production session; account availability is checked by the real CLI.', flush=True)
    if args.resume_session and (args.owner != 'claude-code' or args.intake_check):
        raise RuntimeError('Resume targets an existing Claude production session, not an intake probe')
    session_id=str(uuid.UUID(args.resume_session)) if args.resume_session else str(uuid.uuid4())
    with state_lock():
        state=read(STATE);env=state['productionEnvironment'];lease=env.get('coordinatorLease',{})
        if lease.get('status')=='active' and (process_alive(lease.get('launcherPid')) or process_alive(lease.get('cliPid'))):
            raise RuntimeError('Existing production lead is active; use its session ID, never launch another owner')
        env['coordinatorLease']={'owner':args.owner,'status':'active','launcherPid':os.getpid(),
                                 'sessionId':session_id,'startedAt':now(),'projectRoot':str(ROOT)}
        write(STATE,state)
    cli=None
    try:
        if not args.prepare_session: ensure_editor()
        prompt=('Resume the same approved production session. Preserve integrated changes and prior evidence; '
                'continue only the unfinished action in the current handoff. Do not repeat completed investigations.'
                if args.resume_session else (ROOT/'Tools/LoopEngineering/PRODUCTION_ENTRY.md').read_text(encoding='utf-8'))
        if args.owner=='claude-code':
            command=[shutil.which('claude'),'--model','claude-opus-5-5','--effort',args.effort,
                     '--permission-mode','auto','--permission-prompts','none','--setting-sources','project,local',
                     '--settings',str(ROOT/'.claude/settings.local.json'),'--strict-mcp-config',
                     '--mcp-config',str(ROOT/'.mcp.json'),'--plugin-dir',str(MOD),
                     '--name','ProjectPA Claude Production Lead']
            command += ['--resume' if args.resume_session else '--session-id', session_id]
            if args.intake_check or args.headless:
                command += ['-p','--output-format','stream-json','--verbose']
            if args.prompt_file:
                prompt += '\n\n' + safe_path(args.prompt_file).read_text(encoding='utf-8-sig')
            if args.resume_session and not args.intake_check:
                pending = pending_user_instruction_prompt(read(STATE), session_id)
                if pending: prompt += '\n\n' + pending
            if args.intake_check:
                prompt=('Read-only handoff intake check. You are the recorded Claude Code production lead. '
                    'Read AGENTS.md, CLAUDE.md, Docs/00_CURRENT/CODEX_HANDOFF.md and '
                    'Tools/LoopEngineering/PRODUCTION_ENTRY.md. Do not edit files, start workers, '
                    'enter Play or implement the game during this check. Run the existing doctor '
                    'with --agent claude-code using exactly python -B -X utf8 '
                    'Tools/LoopEngineering/Test-ProjectPADemoEnvironment.py --agent claude-code '
                    '(no appended echo or second command; the tool already reports its exit code); '
                    'actually call Unity MCP read_console and execute_code '
                    '(compiler codedom) with read-only code returning Application.dataPath and '
                    'EditorApplication.isPlaying/isCompiling. Report the exact project identity, '
                    'next world-quality action and preserved pending worker results. '
                    'If a tool fails, report the real failure, not READY. Finish READY_CLAUDE_LEAD only '
                    'when the real connection and takeover instructions are verified. This is an intake '
                    'probe, not completion of game quality or a continuing production session.')
            elif not args.headless: command += [prompt]
        else:
            if args.intake_check: raise RuntimeError('Intake check currently targets the Claude lead')
            command=[shutil.which('codex'),*codex_configuration(args.effort),'-C',str(ROOT),'-m','gpt-6.1-sol',prompt]
        if args.intake_check:
            folder=RUNS/'20261009-ClaudeLead';folder.mkdir(parents=True,exist_ok=True)
            with (folder/'lead-intake.jsonl').open('w',encoding='utf-8') as out, (folder/'lead-intake.stderr').open('w',encoding='utf-8') as err:
                cli=subprocess.Popen(command,cwd=ROOT,stdin=subprocess.PIPE,stdout=out,stderr=err,text=True,encoding='utf-8',creationflags=subprocess.CREATE_NO_WINDOW)
                with state_lock():
                    state=read(STATE);state['productionEnvironment']['coordinatorLease']['cliPid']=cli.pid;write(STATE,state)
                try: progress.communicate_active(cli,prompt,idle_seconds=300,paths=[folder/'lead-intake.jsonl',folder/'lead-intake.stderr'])
                except subprocess.TimeoutExpired:
                    cli.terminate();cli.wait(timeout=15)
                    raise RuntimeError('Claude intake inactive; transcript preserved, inspect exact child before retry')
            events=[]
            for line in (folder/'lead-intake.jsonl').read_text(encoding='utf-8').splitlines():
                try: events.append(json.loads(line))
                except ValueError: pass
            result=next((e for e in reversed(events) if e.get('type')=='result'),{})
            init=next((e for e in events if e.get('subtype')=='init'),{})
            models=observed_models(events)
            calls={}
            observed=[]
            doctor_observed=[]
            for event in events:
                message=event.get('message',{})
                if not isinstance(message,dict): continue
                for block in message.get('content',[]) if isinstance(message.get('content',[]),list) else []:
                    if not isinstance(block,dict): continue
                    if block.get('type')=='tool_use': calls[block['id']]=block.get('name','')
                    if block.get('type')=='tool_use' and block.get('name') in ('PowerShell','Bash') and 'Test-ProjectPADemoEnvironment.py' in block.get('input',{}).get('command',''):
                        doctor_observed.append({'id':block['id'],'tool':block.get('name')})
                    if block.get('type')=='tool_result':
                        name=calls.get(block.get('tool_use_id'),'')
                        for check in doctor_observed:
                            if check['id']==block.get('tool_use_id'):
                                check.update(isError=block.get('is_error',False),content=block.get('content'))
                        if name.startswith('mcp__unityMCP__'):
                            observed.append({'tool':name,'isError':block.get('is_error',False),'content':block.get('content')})
            verification=intake_verification(result,cli.returncode,observed,doctor_observed)
            write(folder/'lead-intake-unity-evidence.json',observed)
            write(folder/'lead-intake-doctor-evidence.json',doctor_observed)
            write(folder/'lead-intake-result.json',result)
            (folder/'lead-intake.md').write_text(result.get('result') or 'No final result; inspect preserved transcript.',encoding='utf-8')
            evidence={'sessionId':session_id,'exitCode':cli.returncode,'actualModels':models,
                      'appliedEffort':actual_effort(session_id),'mcpServers':init.get('mcp_servers',[]),
                      'loadedPlugins':init.get('plugins',[]),'hookEvents':sum(e.get('subtype','').startswith('hook_') for e in events),
                      'usage':result.get('usage'),'modelUsage':result.get('modelUsage'),**verification,
                      'continuingProductionStarted':False,'evidence':str(folder/'lead-intake.jsonl')}
            with state_lock():
                state=read(STATE);state['productionEnvironment']['leadIntake']=evidence;write(STATE,state)
            print(json.dumps(evidence,ensure_ascii=True,indent=2),flush=True)
            return 0 if verification['verified'] else 1
        if args.headless:
            if args.owner != 'claude-code': raise RuntimeError('Headless production targets the recorded Claude lead')
            folder=RUNS/('ClaudeLead-'+dt.datetime.now(KST).strftime('%Y%m%d-%H%M%S')+'-'+session_id[:8])
            folder.mkdir(parents=True,exist_ok=False)
            (folder/'assignment.md').write_text(prompt,encoding='utf-8')
            with (folder/'claude.jsonl').open('w',encoding='utf-8') as out, (folder/'claude.stderr').open('w',encoding='utf-8') as err:
                cli=subprocess.Popen(command,cwd=ROOT,stdin=subprocess.PIPE,stdout=out,stderr=err,text=True,encoding='utf-8',creationflags=subprocess.CREATE_NO_WINDOW)
                with state_lock():
                    state=read(STATE);env=state['productionEnvironment'];env['coordinatorLease']['cliPid']=cli.pid
                    env['coordinator'].update(sessionId=session_id,status='running',cliPid=cli.pid,runDirectory=str(folder),requestedEffort=args.effort,actualModel='UNVERIFIED_CURRENT_SESSION',actualEffort='UNVERIFIED_CURRENT_SESSION')
                    write(STATE,state)
                print(json.dumps({'productionLeadStarted':True,'owner':args.owner,'sessionId':session_id,'cliPid':cli.pid,'runDirectory':str(folder)},ensure_ascii=True),flush=True)
                try: progress.communicate_active(cli,prompt,idle_seconds=args.timeout*60,
                        paths=[folder/'claude.jsonl',folder/'claude.stderr'],
                        checkpoint=lambda: relay_checkpoint(args.owner,reason='periodic',session_id=session_id,
                            next_action='Continue current approved goal',safe_point=False))
                except subprocess.TimeoutExpired:
                    cli.terminate();cli.wait(timeout=15)
                    raise RuntimeError('Claude process inactive; transcript/current diff preserved; inspect exact child before retry')
            events=[]
            for line in (folder/'claude.jsonl').read_text(encoding='utf-8').splitlines():
                try: events.append(json.loads(line))
                except ValueError: pass
            result=next((e for e in reversed(events) if e.get('type')=='result'),{})
            models=observed_models(events)
            init=next((e for e in events if e.get('subtype')=='init'),{})
            write(folder/'result.json',result)
            (folder/'HANDOFF.md').write_text(result.get('result') or 'No final result; inspect preserved stream/diff.',encoding='utf-8')
            with state_lock():
                state=read(STATE);env=state['productionEnvironment'];env['coordinator'].update(status='stopped_result_preserved',actualModel=models,actualEffort=actual_effort(session_id),actualModelEvidenceScope='current production session',loadedPlugins=init.get('plugins',[]),hookEvents=sum(e.get('subtype','').startswith('hook_') for e in events),exitCode=cli.returncode,resultIsError=result.get('is_error'),usage=result.get('usage'),modelUsage=result.get('modelUsage'))
                write(STATE,state)
            print(json.dumps({'sessionId':session_id,'exitCode':cli.returncode,'actualModels':models,'actualEffort':actual_effort(session_id),'resultPreserved':str(folder/'HANDOFF.md')},ensure_ascii=True),flush=True)
            return cli.returncode if cli.returncode else (1 if not result or result.get('is_error') else 0)
        cli=subprocess.Popen(command,cwd=ROOT)
        with state_lock():
            state=read(STATE);state['productionEnvironment']['coordinatorLease']['cliPid']=cli.pid
            state['productionEnvironment']['coordinator'].update(sessionId=session_id,status='running',cliPid=cli.pid)
            write(STATE,state)
        print('Production lead started. Session ID: '+session_id,flush=True)
        return cli.wait()
    finally:
        with state_lock():
            state=read(STATE);lease=state['productionEnvironment'].get('coordinatorLease',{})
            if lease.get('sessionId')==session_id:
                lease.update(status='stopped',stoppedAt=now(),exitCode=cli.returncode if cli else None)
                write(STATE,state)


MOD = ROOT / 'Tools/LoopEngineering/mods/project-pa-production'


def desktop_available():
    import ctypes
    from ctypes import wintypes
    user=ctypes.WinDLL('user32',use_last_error=True)
    user.OpenInputDesktop.restype=wintypes.HANDLE
    user.OpenInputDesktop.argtypes=[wintypes.DWORD,wintypes.BOOL,wintypes.DWORD]
    user.CloseDesktop.argtypes=[wintypes.HANDLE]
    user.GetUserObjectInformationW.argtypes=[wintypes.HANDLE,ctypes.c_int,ctypes.c_void_p,wintypes.DWORD,ctypes.POINTER(wintypes.DWORD)]
    handle=user.OpenInputDesktop(0,False,1)
    if not handle: return False
    try:
        name=ctypes.create_unicode_buffer(256); needed=wintypes.DWORD()
        return bool(user.GetUserObjectInformationW(handle,2,name,ctypes.sizeof(name),ctypes.byref(needed))) and name.value=='Default'
    finally: user.CloseDesktop(handle)


def autonomy_control(args):
    """The mod observes; only this locked helper writes shared production state."""
    if args.operation == 'watch':
        return watch_production(args)
    if args.operation == 'resume-due':
        return resume_due()
    snapshot = False
    with state_lock():
        state = read(STATE); env = state['productionEnvironment']
        control = env.setdefault('autonomous', {})
        if args.operation == 'pause':
            control.update(enabled=False, stopRequested=True, cancelledAt=now(), status='pause_requested')
            if control.get('resumeReservation') and not control['resumeReservation'].get('waitCompletedAt'):
                control['resumeReservation'] = dict(control['resumeReservation'], cancelledAt=now())
        elif args.operation == 'observe':
            observation = json.loads(args.observation)
            if args.session_id == env.get('coordinatorLease', {}).get('sessionId'):
                control['modObservation'] = {'sessionId': args.session_id, 'observedAt': now(),
                    'rateLimits': observation.get('rateLimits', []), 'context': observation.get('context'),
                    'version': observation.get('version'), 'model': observation.get('model')}
                control['modLoaded'] = True
                relay = env.setdefault('relaySupport', {})
                if relay.get('autoHandoff', {}).get('enabled'):
                    relay.setdefault('observations', {})['claude-code'] = dict(control['modObservation'], source='claude_mods')
                    usage = relay_usage_state(relay['observations']['claude-code'])
                    context = observation.get('context') or {}
                    try: pressure = float(context.get('percent', 0) or 0) >= 85
                    except (TypeError, ValueError): pressure = False
                    previous = control.get('lastRelaySnapshotAt')
                    snapshot = age_seconds(previous, 120) >= 120
                    if snapshot: control['lastRelaySnapshotAt'] = now()
                    boundary = control.get('lastSafeCheckpointAt') or control.get('cycleStartedAt') or now()
                    cadence = age_seconds(boundary, 1200) >= 1200
                    reason = ('near_usage_limit' if usage['status'] in ('near_limit', 'exhausted') else
                              'context_pressure' if pressure else 'periodic_safe_boundary' if cadence else None)
                    if reason and not control.get('relayCheckpointRequest'):
                        control['relayCheckpointRequest'] = {'reason': reason, 'requestedAt': now()}
        elif args.operation == 'checkpoint':
            if args.session_id != env.get('coordinatorLease', {}).get('sessionId'):
                raise RuntimeError('Only the recorded lead may submit an automation checkpoint')
            evidence = [str(safe_path(p).relative_to(ROOT)).replace('\\', '/') for p in args.evidence]
            if any(not safe_path(p).is_file() for p in evidence):
                raise RuntimeError('Checkpoint evidence must exist; no claimed validation accepted')
            control['checkpoint'] = {'sessionId': args.session_id, 'recordedAt': now(),
                'disposition': args.disposition, 'nextAction': args.next_action, 'evidence': evidence}
            control['checkpoint']['reason'] = (control.get('relayCheckpointRequest') or {}).get('reason')
            control.update(relayCheckpointRequest=None, lastSafeCheckpointAt=now())
            snapshot = bool(env.get('relaySupport', {}).get('autoHandoff', {}).get('enabled'))
        if args.operation != 'status': write(STATE, state)
        brief = dict(control)
        brief['lease'] = env.get('coordinatorLease', {})
        brief['coordinator'] = {k:env.get('coordinator', {}).get(k) for k in
            ('status', 'sessionId', 'cliPid', 'runDirectory', 'actualModel', 'actualEffort')}
        brief['runningWorkers'] = [{k:j.get(k) for k in ('id','role','status','sessionId','worktree')}
            for j in env.get('workers', {}).values() if j.get('status') in ('running','starting','preparing')]
        print(json.dumps(brief, ensure_ascii=False, indent=2), flush=True)
    if args.operation == 'pause':
        unschedule_local_resume()
    if snapshot:
        try:
            relay_checkpoint('claude-code', reason='safe_handoff' if args.operation == 'checkpoint' else 'periodic',
                next_action=getattr(args, 'next_action', '') or 'Continue current handoff at the next validated work boundary',
                session_id=args.session_id, generation=env.get('coordinatorLease', {}).get('generation'),
                safe_point=args.operation == 'checkpoint', evidence=getattr(args, 'evidence', ()) or ())
        except Exception as exc:
            # Optional recovery capture never blocks current game production or destroys its original logs.
            print('Relay snapshot not promoted: '+type(exc).__name__, file=sys.stderr, flush=True)


def monitor_tail(path):
    """Read bounded stream metadata, never the CLI command/environment or thinking blocks."""
    path=Path(path); details={'sessionId':None,'model':None,'action':None}
    try:
        with path.open('rb') as stream:
            stream.seek(0,2); length=stream.tell(); stream.seek(max(0,length-262144))
            raw=stream.read().decode('utf-8-sig',errors='replace')
        for line in raw.splitlines():
            try: event=json.loads(line)
            except ValueError: continue
            details['sessionId']=event.get('session_id') or event.get('thread_id') or details['sessionId']
            if event.get('type')=='turn.started' and event.get('model'):
                details['model']=event['model']
            item=event.get('item',{})
            if isinstance(item,dict) and event.get('type') in ('item.started','item.completed'):
                kind=item.get('type')
                if kind=='file_change':
                    names=[str(x.get('path','')).replace('\\','/').rsplit('/',1)[-1] for x in item.get('changes',[]) if isinstance(x,dict)]
                    details['action']='file_change '+', '.join(names)
                elif kind in ('command_execution','mcp_tool_call','web_search'):
                    details['action']=kind  # Never show commands, environments or reasoning text.
            message=event.get('message',{})
            if event.get('type')!='assistant' or not isinstance(message,dict): continue
            model=message.get('model')
            if model and not model.startswith('<'): details['model']=model
            for block in message.get('content',[]) if isinstance(message.get('content'),list) else []:
                if not isinstance(block,dict) or block.get('type')!='tool_use': continue
                arguments=block.get('input',{}); label=block.get('name','tool')
                filename=arguments.get('file_path') or arguments.get('path')
                if isinstance(filename,str): label+=' '+filename.replace('\\','/').rsplit('/',1)[-1]
                details['action']=label
        details['lastLogAt']=dt.datetime.fromtimestamp(path.stat().st_mtime,KST).strftime('%H:%M:%S')
    except OSError: pass
    return details


def monitor_timestamp(value, seconds=0):
    if not value: return '-'
    try:
        timestamp=dt.datetime.fromisoformat(str(value).replace('Z','+00:00'))
        return (timestamp+dt.timedelta(seconds=seconds)).astimezone(KST).strftime('%m-%d %H:%M:%S KST')
    except (ValueError,TypeError): return str(value)[:30]


def monitor_frame(state):
    env=state.get('productionEnvironment',{}); control=env.get('autonomous',{}); lead=env.get('coordinator',{})
    lease=env.get('coordinatorLease',{}); folder=Path(lead.get('runDirectory') or RUNS)
    live=monitor_tail(folder/'claude.jsonl')
    lines=['Project P.A. Claude lead + Codex relay | '+dt.datetime.now(KST).strftime('%Y-%m-%d %H:%M:%S KST'),
        'Ctrl+C: close this read-only view; production continues.',
        'AUTO: '+str(control.get('status','unconfigured'))+' | supervisor '+str(lease.get('launcherPid') or '-')+
        (' [alive]' if process_alive(lease.get('launcherPid')) else ' [not running]')]
    if control.get('nextResumeAt') and control.get('status')=='waiting_usage_reset':
        lines.append('AUTO RESUME: '+monitor_timestamp(control['nextResumeAt'],60)+' (server reset + 60s)')
    observation=control.get('modObservation',{})
    limits=observation.get('rateLimits',[]) or []
    lines.append('USAGE: '+' | '.join(str(x.get('kind'))+' '+str(x.get('percentUsed'))+'% / reset '+
        monitor_timestamp(x.get('resetsAt')) for x in limits if isinstance(x,dict)) if limits else 'USAGE: no verified reading yet')
    lines.append('Latest usage reading: '+monitor_timestamp(observation.get('observedAt')))
    lines+=['','ROLE         STATE                               SESSION    MODEL                 JOB']
    def row(role,status,sid,model,name):
        return f'{str(role)[:12]:12} {str(status)[:35]:35} {str(sid or "-")[:10]:10} {str(model or "unverified")[:21]:21} {name}'
    model=live.get('model') or lead.get('actualModel')
    if isinstance(model,list): model=', '.join(model)
    lead_status=lead.get('status','unknown')
    if lead_status=='running' and not process_alive(lease.get('cliPid')): lead_status='recorded running / PID exited'
    lines.append(row('lead',lead_status,lead.get('sessionId'),model,'original Unity owner'))
    jobs=list(env.get('workers',{}).values()); started=control.get('startedAt','')
    active={'running','starting','preparing'}
    current=[j for j in jobs if j.get('status') in active or (started and
        max(j.get('startedAt') or '',j.get('createdAt') or '') >= started)]
    if not current: current=sorted(jobs,key=lambda j:j.get('startedAt') or j.get('createdAt') or '')[-5:]
    for job in sorted(current,key=lambda j:j.get('startedAt') or j.get('createdAt') or ''):
        provider=job.get('provider','claude-code')
        stream=Path(job.get('logDirectory') or RUNS)/('codex-app-server.jsonl' if job.get('executionMethod')=='app_server' else
            'codex.jsonl' if provider=='codex' else 'claude.jsonl'); detail=monitor_tail(stream)
        native=job.get('executionMethod')=='native_subagent'
        if native:
            detail={'sessionId':job.get('nativeAgent'),'model':None,'action':'native Codex subagent; scoped worktree','lastLogAt':'-'}
        models=job.get('actualModels') or [detail.get('model') or 'unverified']
        status=job.get('status','unknown')
        pid=None if native else (job.get('codexPid') if provider=='codex' else job.get('claudePid'))
        if status=='running' and pid and not process_alive(pid): status='recorded running / PID exited'
        sid=detail.get('sessionId') if native else (job.get('sessionId') or detail.get('sessionId'))
        lines.append(row(job.get('role'),status,sid,', '.join(models),provider+' / '+str(job.get('id'))))
        if status in active and detail.get('action'): lines.append('  latest '+detail.get('lastLogAt','-')+': '+detail['action'])
    lines+=['','LEAD last tool: '+(live.get('action') or '-')+' | log updated '+live.get('lastLogAt','-'),
        'Lead stream: '+str(folder/'claude.jsonl'),
        'Worker details/results: Logs/ParallelProduction/<JOB>/HANDOFF.md, result.json, review.patch',
        'Detailed JSON: launcher -Status | Safe production pause: launcher -Pause']
    return '\n'.join(lines)


def watch_production(args):
    try:
        while True:
            try: frame=monitor_frame(read(STATE))
            except (OSError,ValueError) as exc: frame='State temporarily unavailable: '+str(exc)
            if sys.stdout.isatty() and not args.once: print('\033[2J\033[H',end='')
            print(frame,flush=True)
            if args.once: return 0
            time.sleep(max(2,args.interval))
    except KeyboardInterrupt:
        print('\nMonitor closed. Production was not stopped.',flush=True)
        return 0


def future_quota_reset(text, limits, current):
    """Never guess a five-hour window. Use a server reset or its current CLI banner."""
    candidates = []
    for limit in limits or []:
        if not isinstance(limit,dict): continue
        try:
            used=float(limit.get('percentUsed', 0) or 0)
            if not math.isfinite(used) or used < 100: continue
            reset = limit.get('resetsAt')
            value = (dt.datetime.fromtimestamp(reset / (1000 if reset > 1e11 else 1), KST)
                     if isinstance(reset, (int, float)) else dt.datetime.fromisoformat(str(reset).replace('Z', '+00:00')))
            if value.tzinfo is not None and value > current: candidates.append(value)
        except (ValueError, TypeError, OverflowError, OSError): pass
    if candidates: return max(candidates)
    # Current CLI prints this exact timezone-aware daily banner. Unknown/weekly formats stop.
    m = re.search(r'resets\s+(\d{1,2})(?::(\d{2}))?\s*(am|pm)\s*\(Asia/Seoul\)', text or '', re.I)
    if m:
        hour = int(m[1]); minute = int(m[2] or 0)
        if not 1 <= hour <= 12 or not 0 <= minute < 60: return None
        hour = hour % 12 + (12 if m[3].lower() == 'pm' else 0)
        value = current.astimezone(KST).replace(hour=hour, minute=minute, second=0, microsecond=0)
        if value > current: return value
    return None


def game_fingerprint(evidence=()):
    value = hashlib.sha256(git('diff', '--binary', 'HEAD', '--', 'Assets', ':(exclude)Assets/Fonts').encode())
    for name in git('ls-files', '--others', '--exclude-standard', '--', 'Assets').splitlines():
        if name.startswith('Assets/Fonts/'): continue
        value.update((name + str(digest(safe_path(name)))).encode())
    for name in evidence:
        value.update((name + str(digest(safe_path(name)))).encode())
    return value.hexdigest()


def quota_reset_from_events(result, events, observation, current):
    # API error codes/stream events, rather than a worker's prose about a past error.
    code=result.get('api_error')
    if code in ('authentication_error','invalid_api_key','insufficient_credits','context_length_exceeded','model_not_found'):
        return None
    limits=list(observation.get('rateLimits') or [])
    rejected=[]
    for event in events:
        info=event.get('rate_limit_info') if event.get('type')=='rate_limit_event' else None
        # Paid overage past the plan window is the same subscription boundary, never a bypass.
        if isinstance(info,dict) and (info.get('status')=='rejected' or info.get('isUsingOverage') is True):
            rejected.append(info)
    quota=code=='usage_limit_reached' or bool(rejected) or any(
        e.get('is_api_error_message') and e.get('error')=='rate_limit' for e in events)
    if not quota: return None
    for info in rejected:
        limits.append({'percentUsed':100,'resetsAt':info.get('resetsAt'),'kind':info.get('rateLimitType')})
        for kind,window in (info.get('unifiedWindows') or {}).items():
            if isinstance(window,dict):
                try: utilization=float(window.get('utilization',0))
                except (ValueError,TypeError): continue
                limits.append({'kind':kind,'percentUsed':utilization*100,'resetsAt':window.get('resetsAt')})
    return future_quota_reset(result.get('result',''),limits,current)


class OverageWatch:
    """Tail a Claude stream for paid overage use. Reads local log bytes only; no model or account call."""

    def __init__(self, path):
        self.path, self.offset, self.pending, self.event = Path(path), 0, b'', None

    def check(self):
        if self.event: return self.event
        try:
            with self.path.open('rb') as stream:
                stream.seek(self.offset); chunk = stream.read(); self.offset = stream.tell()
        except OSError: return None
        lines = (self.pending + chunk).split(b'\n'); self.pending = lines.pop()
        for raw in lines:
            if b'rate_limit_event' not in raw: continue
            try: event = json.loads(raw.decode('utf-8-sig', errors='replace'))
            except ValueError: continue
            info = event.get('rate_limit_info') if isinstance(event, dict) else None
            if isinstance(info, dict) and info.get('isUsingOverage') is True:
                self.event = event; break
        return self.event


def production_guard():
    spec = importlib.util.spec_from_file_location('pa_resume_guard', ROOT / 'Tools/PaAgentConsole/production_adapter.py')
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def resume_gate_errors(token, session_id, previous_cycle_started=None):
    """Re-verify current approval, owner/lease, in-flight work and checkpoint before a model relaunch.

    A recovered quota is neither task approval nor a usage-record reset. No model call is made here.
    """
    guard = production_guard()
    state, pin = guard.authority()
    env = state.get('productionEnvironment', {}); lease = env.get('coordinatorLease', {})
    errors = guard.authorization_errors(state, pin, dict(pin, provider=pin['owner'], sessionId=lease.get('sessionId')))
    errors += guard.budget_errors(state)
    if (pin.get('owner') != 'claude-code' or lease.get('owner') != 'claude-code'
            or lease.get('generation') != token or lease.get('sessionId') != session_id):
        errors.append('WRITER_OWNER_OR_LEASE_CHANGED')
    if any(x.get('status') == 'in_flight' for x in env.get('productionDispatch', {}).get('ledger', [])):
        errors.append('IN_FLIGHT_OPERATION_UNRESOLVED')
    if process_alive(lease.get('cliPid')):
        errors.append('PREVIOUS_MODEL_CHILD_STILL_RUNNING')
    if previous_cycle_started:
        checkpoint = env.get('relaySupport', {}).get('checkpoints', {}).get('claude-code') or {}
        if checkpoint.get('generation') != token or str(checkpoint.get('recordedAt', '')) < previous_cycle_started:
            errors.append('RECOVERY_CHECKPOINT_MISSING')
    return sorted(set(errors))


RESUME_TASK = ('\\ProjectPA\\', 'UsageResume')
RESUME_LOG = RUNS / 'Autonomous-20261011' / 'resume-due.jsonl'


def schedule_local_resume(resume_at, run=subprocess.run):
    """User-approved 2026-10-19 durable timer: one Windows task at the recorded reset, surviving reboot.

    It only runs `autonomy resume-due`, which re-checks every gate and spawns no model when blocked.
    """
    when = dt.datetime.fromisoformat(resume_at) + dt.timedelta(seconds=60)  # in-process supervisor wins first
    if ROOT.resolve() != PRIMARY_ROOT:  # fixtures and worktrees never touch the OS scheduler
        return {'task': ''.join(RESUME_TASK), 'runAt': when.isoformat(), 'registered': False, 'error': 'not_primary_root'}
    python = Path(sys.executable).with_name('pythonw.exe')
    python = python if python.is_file() else Path(sys.executable)
    script = ("$ErrorActionPreference='Stop';"
        "$a=New-ScheduledTaskAction -Execute $env:PA_PY -Argument $env:PA_ARGS -WorkingDirectory $env:PA_ROOT;"
        "$t=New-ScheduledTaskTrigger -Once -At ([DateTimeOffset]::Parse($env:PA_AT).LocalDateTime);"
        "$s=New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries "
        "-MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 10);"
        "Register-ScheduledTask -TaskPath $env:PA_TP -TaskName $env:PA_TN -Action $a -Trigger $t -Settings $s "
        "-Description 'Project P.A. usage reset resume check (no model call when blocked)' -Force | Out-Null")
    env = dict(os.environ, PA_PY=str(python), PA_ROOT=str(ROOT), PA_AT=when.isoformat(),
               PA_ARGS='-B -X utf8 "' + str(Path(__file__).resolve()) + '" autonomy resume-due',
               PA_TP=RESUME_TASK[0], PA_TN=RESUME_TASK[1])
    try:
        result = run(['powershell', '-NoProfile', '-NonInteractive', '-Command', script], env=env,
                     capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=60)
        ok = result.returncode == 0
        return {'task': ''.join(RESUME_TASK), 'runAt': when.isoformat(), 'registered': ok,
                'error': None if ok else (result.stderr or result.stdout)[-400:]}
    except (OSError, subprocess.TimeoutExpired) as exc:
        return {'task': ''.join(RESUME_TASK), 'runAt': when.isoformat(), 'registered': False, 'error': type(exc).__name__}


def unschedule_local_resume(run=subprocess.run):
    if ROOT.resolve() != PRIMARY_ROOT: return
    script = ("Get-ScheduledTask -TaskPath $env:PA_TP -TaskName $env:PA_TN -ErrorAction SilentlyContinue | "
              "Unregister-ScheduledTask -Confirm:$false")
    try:
        run(['powershell', '-NoProfile', '-NonInteractive', '-Command', script],
            env=dict(os.environ, PA_TP=RESUME_TASK[0], PA_TN=RESUME_TASK[1]),
            capture_output=True, text=True, timeout=60)
    except (OSError, subprocess.TimeoutExpired): pass  # A leftover task only runs the gated, idempotent check.


def resume_due(current=None, spawn=None):
    """Scheduled entry after a recorded reset. No model call; every gate is re-read from the original state."""
    current = current or dt.datetime.now(KST)
    with state_lock():
        state = read(STATE); env = state['productionEnvironment']; control = env.get('autonomous', {})
        reservation = control.get('resumeReservation') or {}; lease = env.get('coordinatorLease', {})
        reasons = []
        if not reservation.get('resetAt') or any(reservation.get(k) for k in ('cancelledAt', 'waitCompletedAt', 'dispatchedAt')):
            reasons.append('NO_PENDING_RESERVATION')
        elif current < dt.datetime.fromisoformat(reservation['resumeAt']):
            reasons.append('NOT_DUE')
        if not control.get('enabled') or control.get('stopRequested'): reasons.append('PAUSED_BY_USER')
        if process_alive(lease.get('launcherPid')) or process_alive(lease.get('cliPid')):
            reasons.append('SUPERVISOR_OR_CHILD_ALIVE')
        if lease.get('owner') != 'claude-code' or state.get('demoProductionRouting', {}).get('unityEditorOwner') != 'claude-code':
            reasons.append('OTHER_WRITER_OWNS_PRODUCTION')
        if any(x.get('status') == 'in_flight' for x in env.get('productionDispatch', {}).get('ledger', [])):
            reasons.append('IN_FLIGHT_OPERATION_UNRESOLVED')
        if not reasons:
            guard = production_guard(); _, pin = guard.authority()
            reasons += [e for e in guard.authorization_errors(state, pin, dict(pin, provider=pin['owner'],
                        sessionId=lease.get('sessionId'))) + guard.budget_errors(state)
                        if e != 'SINGLE_WRITER_LEASE_NOT_ACTIVE']
        outcome = {'checkedAt': now(), 'reasons': sorted(set(reasons))}
        if reservation:
            control['resumeReservation'] = reservation = dict(reservation, lastDueCheck=outcome)
        if not reasons:
            command = [sys.executable, '-B', '-X', 'utf8', str(Path(__file__).resolve()), 'start',
                       '--owner', 'claude-code', '--autonomous', '--effort', reservation.get('effort', 'high')]
            RESUME_LOG.parent.mkdir(parents=True, exist_ok=True)
            with RESUME_LOG.with_suffix('.start.log').open('a', encoding='utf-8') as out:
                flags = subprocess.CREATE_NO_WINDOW | subprocess.CREATE_NEW_PROCESS_GROUP
                try:
                    process = (spawn or subprocess.Popen)(command, cwd=ROOT, stdout=out, stderr=subprocess.STDOUT,
                        creationflags=flags | subprocess.CREATE_BREAKAWAY_FROM_JOB)
                except OSError:
                    process = (spawn or subprocess.Popen)(command, cwd=ROOT, stdout=out, stderr=subprocess.STDOUT, creationflags=flags)
            control['resumeReservation'] = reservation = dict(reservation, dispatchedAt=now(), launcherPid=process.pid)
            outcome['dispatched'] = process.pid
        if reservation: write(STATE, state)
    RESUME_LOG.parent.mkdir(parents=True, exist_ok=True)
    with RESUME_LOG.open('a', encoding='utf-8') as log: log.write(json.dumps(outcome, ensure_ascii=False) + '\n')
    if outcome['reasons'] != ['NOT_DUE']: unschedule_local_resume()
    print(json.dumps(outcome, ensure_ascii=False), flush=True)
    return outcome


def autonomous_start(args, report):
    if args.owner != 'claude-code' or report['routing'].get('nextTicket') is None:
        raise RuntimeError('Automation requires the approved Claude production owner')
    if args.headless or args.intake_check:
        raise RuntimeError('Use autonomous mode itself; do not combine with intake/headless probes')
    try: codex_bridge_module()  # Pin at process start, before the maintenance loop can observe file replacements.
    except Exception: pass  # The cached optional-provider failure is recorded by maintenance; Claude can continue.
    session_id = args.resume_session or read(STATE)['productionEnvironment'].get('coordinator', {}).get('sessionId') or str(uuid.uuid4())
    session_id = str(uuid.UUID(session_id))
    if not args.foreground:
        token = str(uuid.uuid4())
        with state_lock():
            state=read(STATE); env=state['productionEnvironment']; lease=env.get('coordinatorLease', {})
            if lease.get('status') == 'active' and (process_alive(lease.get('launcherPid')) or process_alive(lease.get('cliPid'))):
                raise RuntimeError('Existing lead owns Unity. Preserve/stop its idle session before takeover; do not duplicate it.')
            env['autonomous'].update(enabled=True, stopRequested=False, generation=token, status='starting', startedAt=now())
            env['coordinatorLease']={'owner':'claude-code','status':'active','launcherPid':os.getpid(),
                'sessionId':session_id,'generation':token,'projectRoot':str(ROOT),'startedAt':now()}
            write(STATE,state)
        folder=RUNS/'Autonomous-20261011'; folder.mkdir(parents=True,exist_ok=True)
        command=[sys.executable,'-B','-X','utf8',str(Path(__file__).resolve()),'start','--autonomous','--foreground',
                 '--lease-token',token,'--resume-session',session_id,'--effort',args.effort]
        if args.prepare_session: command.append('--prepare-session')
        with (folder/'supervisor.log').open('a',encoding='utf-8') as out:
            process=subprocess.Popen(command,cwd=ROOT,stdout=out,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW)
        with state_lock():
            state=read(STATE); lease=state['productionEnvironment']['coordinatorLease']
            if lease.get('generation') == token: lease['launcherPid']=process.pid; write(STATE,state)
        print(json.dumps({'autonomousStarted':True,'supervisorPid':process.pid,'sessionId':session_id,
            'log':str(folder/'supervisor.log'),'owner':'claude-code'},indent=2),flush=True)
        return 0
    token=args.lease_token
    def update(**fields):
        with state_lock():
            state=read(STATE); env=state['productionEnvironment']
            if env.get('coordinatorLease',{}).get('generation') != token:
                raise RuntimeError('Automation lease changed; no second owner allowed')
            env['autonomous'].update(fields); write(STATE,state)
    def permitted():
        state=read(STATE); env=state['productionEnvironment']; control=env['autonomous']
        return (control.get('generation')==token and control.get('enabled') and not control.get('stopRequested')
                and control.get('status')!='ready_for_human_review'
                and (control.get('checkpoint') or {}).get('disposition')!='human-review'
                and env.get('coordinatorLease',{}).get('generation')==token
                and doctor().selection(state,'claude-code').get('nextTicket') is not None)
    last_maintenance = 0
    def maintenance():
        nonlocal last_maintenance
        if not permitted() or time.monotonic()-last_maintenance < 60: return
        last_maintenance = time.monotonic()
        env = read(STATE)['productionEnvironment']
        if not env.get('relaySupport', {}).get('autoHandoff', {}).get('enabled'): return
        def record(detail):
            detail.update(generation=token, sessionId=env.get('coordinatorLease', {}).get('sessionId'))
            with state_lock():
                current = read(STATE)
                if current['productionEnvironment'].get('coordinatorLease', {}).get('generation') == token:
                    relay = current['productionEnvironment'].setdefault('relaySupport', {})
                    relay['bridgeObservation'] = detail
                    if detail['status']=='ERROR': retain_bridge_failure(relay, detail, RUNS/'Autonomous-20261011')
                    write(STATE, current)
        def should_observe():
            observation = env.get('relaySupport', {}).get('observations', {}).get('codex') or {}
            return (not observation.get('observedAt') or
                (dt.datetime.now(dt.timezone.utc)-dt.datetime.fromisoformat(observation['observedAt'])).total_seconds() >= 240)
        detail=relay_maintenance_result(observe_codex_usage,relay_tick,record,should_observe)
        if detail['status']=='ERROR':
            # Diagnosis cannot change an actual dispatch into a skipped attempt, nor trigger a retry.
            detail.update(generation=token, sessionId=env.get('coordinatorLease', {}).get('sessionId'))
            print('Relay maintenance preserved: '+json.dumps(detail, ensure_ascii=False), flush=True)
            try:
                record(detail)
            except Exception as record_error:
                print('Relay detail state write preserved in supervisor log: '+type(record_error).__name__, flush=True)
    def wait_until(reset):
        while permitted() and dt.datetime.now(KST) < reset+dt.timedelta(seconds=60):
            maintenance(); time.sleep(5)
        return permitted()
    final_status='stopped_preserved'; cli=None
    fresh_session = False
    try:
        if not permitted():
            control=read(STATE)['productionEnvironment']['autonomous']
            if control.get('status')=='ready_for_human_review' or (control.get('checkpoint') or {}).get('disposition')=='human-review':
                final_status='ready_for_human_review'
            return 1
        if not args.prepare_session: ensure_editor()
        existing_control = read(STATE)['productionEnvironment']['autonomous']
        # A restarted supervisor keeps the recorded server reset instead of spending a model call to rediscover it.
        reservation = existing_control.get('resumeReservation') or {}
        pending_reset = (reservation.get('resetAt') if reservation.get('resetAt') and not reservation.get('waitCompletedAt')
                         and not reservation.get('cancelledAt') else None)
        if not pending_reset and existing_control.get('status') == 'waiting_usage_reset':
            pending_reset = existing_control.get('nextResumeAt')
        if pending_reset:
            reset = dt.datetime.fromisoformat(pending_reset)
            if reset.tzinfo is None: raise RuntimeError('Unverified reset timezone; no premature retry')
            if not wait_until(reset): final_status='paused_by_user'; return 0
            if reservation: update(resumeReservation=dict(reservation, waitCompletedAt=now()))
            unschedule_local_resume()
        previous_cycle_started = None
        while permitted():
            maintenance()
            gate = resume_gate_errors(token, session_id, previous_cycle_started)
            update(resumeGate={'checkedAt': now(), 'afterCycle': previous_cycle_started, 'errors': gate})
            if gate:
                print('Resume gate blocked: '+', '.join(gate), flush=True)
                final_status='paused_resume_gate_blocked'; break
            # A separately opened user conversation is preserved. If it is working, pause before Unity transfer.
            registry=subprocess.run([shutil.which('claude'),'agents','--json','--cwd',str(ROOT)],
                capture_output=True,text=True,encoding='utf-8',timeout=20)
            peers=json.loads(registry.stdout) if registry.returncode==0 else []
            if registry.returncode != 0 or any(p.get('sessionId')!=session_id and p.get('status') not in ('idle','completed') for p in peers):
                final_status='paused_other_session_or_unverified_registry'; break
            baseline_head=git('rev-parse','HEAD'); before=game_fingerprint()
            cycle_started=now(); folder=RUNS/('ClaudeLead-'+dt.datetime.now(KST).strftime('%Y%m%d-%H%M%S')+'-'+session_id[:8])
            folder.mkdir(parents=True,exist_ok=False)
            prompt=('/goal Preserve the existing five-minute playable demo and finish the approved quality pipeline: '
                'coast/sea/vegetation, motion, props, UI, harmony, then a tested Windows candidate. '
                'Completion requires independent image/video reviews, real Unity/native input, isolated save/quit/fresh Continue '
                'and standalone performance evidence, with remaining defects disclosed and ready for FINAL_WINDOWS_HUMAN_REVIEW. '
                'Do not treat environment setup, CLI exit, or your own verdict as product completion. '
                'Stop at a documented real safety/account/tool blocker; periodic checkpoints are resumable and not task limits. Leave a safe '
                'checkpoint with a concrete next action and existing helper autonomy checkpoint disposition continue. '
                'Continue from the latest repository handoff and reserved relay candidates; do not redo completed work. '
                'Use the existing isolated builder/critic/reviser/verifier jobs; the sole original Unity owner is the current recorded state/lease owner. '
                'Before ending, update the concise handoff and call the helper autonomy checkpoint with your actual session ID, '
                'continue/blocked/human-review disposition, next action and actual evidence paths. No commit/push/data loss. '
                'Read Tools/LoopEngineering/PRODUCTION_ENTRY.md and Docs/00_CURRENT/CODEX_HANDOFF.md once for the exact '
                'instructions, references and current checkpoint. Read autonomy status for current control and your session ID. '
                'On pause_requested, preserve work at a safe point, clear /goal, record a blocked checkpoint and stop. '
                'For native input, use the existing helper; if Windows is locked pause only visual validation while doing '
                'safe independent code/review work. Do not disable the input guard or claim unseen footage. '
                'D5/D7 may be assigned to capable Claude workers; current actual ability and independent checks govern.')
            (folder/'assignment.md').write_text(prompt,encoding='utf-8')
            override=read(ROOT/'.claude/settings.local.json')
            override.update(disableAllHooks=False,autoContinueAtUsageLimit=True)
            command=[shutil.which('claude'),'-p','--output-format','stream-json','--verbose',
                '--session-id' if fresh_session else '--resume',session_id,
                '--model','claude-opus-5-5','--effort',args.effort,'--permission-mode','auto','--permission-prompts','none',
                '--setting-sources','project,local','--settings',json.dumps(override,ensure_ascii=False),
                '--strict-mcp-config','--mcp-config',str(ROOT/'.mcp.json'),'--plugin-dir',str(MOD),
                '--name','ProjectPA Claude Production Lead']
            with (folder/'claude.jsonl').open('w',encoding='utf-8') as out,(folder/'claude.stderr').open('w',encoding='utf-8') as err:
                with state_lock():
                    state=read(STATE); env=state['productionEnvironment']; lease=env.get('coordinatorLease',{})
                    if lease.get('generation')!=token or lease.get('sessionId')!=session_id or \
                            env['autonomous'].get('generation')!=token or not env['autonomous'].get('enabled') or env['autonomous'].get('stopRequested'):
                        raise RuntimeError('Lease/session/pause changed before model launch; no stale owner started')
                    cli=subprocess.Popen(command,cwd=ROOT,stdin=subprocess.PIPE,stdout=out,stderr=err,
                        text=True,encoding='utf-8',creationflags=subprocess.CREATE_NO_WINDOW)
                    env['coordinator'].setdefault('priorRuns',[]).append({k:env['coordinator'].get(k) for k in
                        ('sessionId','status','runDirectory','exitCode','resultIsError','usage','modelUsage')})
                    env['coordinatorLease'].update(cliPid=cli.pid, sessionId=session_id)
                    env['coordinator'].update(sessionId=session_id,status='running',cliPid=cli.pid,runDirectory=str(folder),
                        actualModel='UNVERIFIED_CURRENT_RUN',actualEffort='UNVERIFIED_CURRENT_RUN',exitCode=None,resultIsError=None)
                    env['autonomous'].update(status='running',cycleStartedAt=cycle_started,checkpoint=None,relayCheckpointRequest=None)
                    write(STATE,state)
                cli.stdin.write(prompt); cli.stdin.close()
                # No unsafe wall-clock kill during Unity integration. Mod handles a requested safe pause.
                # Paid overage is the exception: unapproved spending stops at once, then waits for the plan reset.
                fresh_session = False; overage = OverageWatch(folder/'claude.jsonl')
                while cli.poll() is None:
                    maintenance()
                    if overage.check():
                        print('Paid overage use observed; stopping the model child and waiting for plan reset.', flush=True)
                        cli.terminate(); cli.wait(timeout=15)
                        update(overageStopped={'at': now(), 'evidence': str(folder/'claude.jsonl')})
                        break
                    time.sleep(3)
            events=[]
            for line in (folder/'claude.jsonl').read_text(encoding='utf-8').splitlines():
                try: events.append(json.loads(line))
                except ValueError: pass
            result=next((e for e in reversed(events) if e.get('type')=='result'),{})
            write(folder/'result.json',result)
            text=result.get('result') or '\n'.join(json.dumps(e,ensure_ascii=False) for e in events if e.get('type') in ('assistant','result'))
            (folder/'HANDOFF.md').write_text(text or 'No result; inspect retained transcript.',encoding='utf-8')
            env=read(STATE)['productionEnvironment']; control=env['autonomous']; checkpoint=control.get('checkpoint') or {}
            with state_lock():
                state=read(STATE)
                if state['productionEnvironment']['coordinatorLease'].get('generation')!=token or \
                        state['productionEnvironment']['coordinatorLease'].get('sessionId')!=session_id:
                    final_status='stopped_lease_changed'; break
                state['productionEnvironment']['coordinator'].update(status='stopped_result_preserved',
                    actualModel=observed_models(events),actualEffort=actual_effort(session_id),exitCode=cli.returncode,
                    resultIsError=result.get('is_error'),usage=result.get('usage'),modelUsage=result.get('modelUsage'))
                write(STATE,state)
            try:
                relay_checkpoint('claude-code', reason='lead_result_preserved', session_id=session_id,
                    generation=token, safe_point=not result.get('is_error') and cli.returncode==0,
                    next_action=checkpoint.get('nextAction') or 'Read actual retained lead result, then continue approved quality work',
                    evidence=[str((folder/'HANDOFF.md').relative_to(ROOT)), str((folder/'result.json').relative_to(ROOT))])
            except Exception as exc: print('Lead snapshot retained source: '+type(exc).__name__, flush=True)
            if not permitted():
                final_status='ready_for_human_review' if checkpoint.get('disposition')=='human-review' else 'paused_by_user'
                break
            if git('rev-parse','HEAD') != baseline_head: final_status='paused_baseline_changed'; break
            previous_cycle_started = cycle_started
            is_error=cli.returncode != 0 or not result or result.get('is_error')
            if is_error:
                observation=control.get('modObservation',{})
                fresh_observation=observation if observation.get('sessionId')==session_id and observation.get('observedAt','')>=cycle_started else {}
                reset=quota_reset_from_events(result,events,fresh_observation,dt.datetime.now(KST))
                if reset is None: final_status='blocked_account_or_tool_no_verified_reset'; break
                reservation={'resetAt':reset.isoformat(),'resumeAt':(reset+dt.timedelta(seconds=60)).isoformat(),
                    'source':'server_rate_limit_reset','evidence':str(folder/'claude.jsonl'),'sessionId':session_id,
                    'generation':token,'createdAt':now(),'effort':args.effort,
                    'note':'Local timer only; resume re-checks approval/owner/lease/in-flight/checkpoint. Not a work approval or usage reset.'}
                reservation['scheduledTask']=schedule_local_resume(reservation['resumeAt'])
                update(status='waiting_usage_reset',nextResumeAt=reset.isoformat(),limitEvidence=str(folder/'claude.jsonl'),
                       resumeReservation=reservation)
                print('Waiting for actual usage reset '+reset.isoformat(),flush=True)
                if wait_until(reset):
                    update(resumeReservation=dict(reservation, waitCompletedAt=now())); unschedule_local_resume(); continue
                final_status='paused_by_user'; break
            disposition=checkpoint.get('disposition')
            if disposition=='continue' and checkpoint.get('recordedAt','')>=cycle_started:
                fresh_validation=any(p.replace('\\','/').startswith('Logs/')
                    and '/relay-snapshots/' not in p.replace('\\','/') and '/AutoRelay-' not in p.replace('\\','/')
                    and safe_path(p).stat().st_size>0
                    and safe_path(p).stat().st_mtime>=dt.datetime.fromisoformat(cycle_started).timestamp()
                    for p in checkpoint.get('evidence',[]))
                if game_fingerprint() == before and not fresh_validation:
                    final_status='paused_no_product_or_validation_progress'; break
                if checkpoint.get('reason') == 'context_pressure':
                    prior_session = session_id; session_id = str(uuid.uuid4()); fresh_session = True
                    with state_lock():
                        state=read(STATE); env=state['productionEnvironment']
                        if env['coordinatorLease'].get('generation') != token: raise RuntimeError('Lease changed before context renewal')
                        env['autonomous'].setdefault('priorSessions',[]).append({'sessionId':prior_session,'reason':'context_pressure','at':now()})
                        env['coordinatorLease']['sessionId']=session_id; env['coordinator']['sessionId']=session_id
                        write(STATE,state)
                update(status='continuing_safe_checkpoint',nextAction=checkpoint.get('nextAction')); continue
            final_status='ready_for_human_review' if disposition=='human-review' else 'paused_checkpoint_or_blocker'
            break
    except Exception as exc:
        final_status='blocked_supervisor_error_preserved'
        print(type(exc).__name__+': '+str(exc),flush=True)
    finally:
        # Never release the Unity lease while a still-running child owns it.
        if cli is not None and cli.poll() is None:
            print('Child still owns production; lease retained for safe recovery.',flush=True)
        else:
            with state_lock():
                state=read(STATE); env=state['productionEnvironment']
                if env.get('coordinatorLease',{}).get('generation')==token:
                    env['autonomous'].update(status=final_status,stoppedAt=now(),enabled=False)
                    env['coordinatorLease'].update(status='stopped',stoppedAt=now(),cliPid=None)
                    write(STATE,state)
    return 0


def supervise(args):
    with state_lock():
        state=read(STATE); env=state['productionEnvironment']; job=env['workers'][args.id]
        if job.get('executionMethod') == 'native_subagent':
            raise RuntimeError('Native worker owns this job; do not spawn a duplicate CLI')
        if job.get('status')!='assigned' or (job.get('relayGeneration') and
                job['relayGeneration']!=env.get('coordinatorLease',{}).get('generation')):
            raise RuntimeError('Worker already claimed or generation changed; no duplicate/stale launch')
        if any(process_alive(job.get(k)) for k in ('codexPid','claudePid')) or \
                (job.get('supervisorPid')!=os.getpid() and process_alive(job.get('supervisorPid'))):
            raise RuntimeError('Existing worker/supervisor still owns the attempt')
        job.update(status='starting',supervisedBy=os.getpid())
        job.setdefault('attemptToken',str(uuid.uuid4())); write(STATE,state)
    folder = Path(job['logDirectory'])
    if job.get('provider') == 'codex':
        if job.get('executionMethod') == 'app_server': return supervise_codex_appserver(job)
        return supervise_codex(job)
    command = [shutil.which('claude'), '-p', '--output-format', 'stream-json', '--verbose',
        '--model', job['requestedModel'], '--effort', job['requestedEffort'], '--permission-mode', 'dontAsk',
        '--permission-prompts', 'none', '--setting-sources', 'project,local',
        '--settings', str(folder / 'worker-settings.json'), '--strict-mcp-config',
        '--mcp-config', str(folder / 'worker-mcp.json'), '--restricted',
        '--tools', 'Read,Edit,Write,Glob,Grep,Bash', '--disable-slash-commands']
    stdout = (folder / 'claude.jsonl').open('w', encoding='utf-8')
    stderr = (folder / 'claude.stderr').open('w', encoding='utf-8')
    process = subprocess.Popen(command, cwd=job['worktree'], stdin=subprocess.PIPE, stdout=stdout,
        stderr=stderr, text=True, encoding='utf-8', creationflags=subprocess.CREATE_NO_WINDOW)
    job.update(status='running', claudePid=process.pid, startedAt=now()); record(job)
    try:
        payload = (folder / 'assignment.md').read_text(encoding='utf-8')
        def snapshot():
            try: relay_checkpoint('claude-code', job['id'], 'periodic', job['goal'],
                    generation=job.get('relayGeneration'), safe_point=False)
            except Exception as exc: print('Worker snapshot retained source: '+type(exc).__name__,flush=True)
        progress.communicate_active(process, payload, idle_seconds=job['timeoutMinutes']*60,
            paths=[folder/'claude.jsonl',folder/'claude.stderr',
                *[Path(job['worktree'])/p for p in job.get('allowedFiles',[])]], checkpoint=snapshot,
            stop_when=OverageWatch(folder/'claude.jsonl').check)
    except subprocess.TimeoutExpired:
        process.terminate(); process.wait(timeout=15)
        job.update(status='timeout_preserved', error='Inactivity timeout; partial work/logs retained; inspect cause before retry')
    except progress.StopRequested:
        # Unapproved paid overage: stop now; the stream's plan reset makes this portable relay work below.
        process.terminate(); process.wait(timeout=15)
        job.update(status='failed_preserved', error='Paid overage use observed; stopped. Partial work/logs retained.')
    finally:
        stdout.close(); stderr.close()
    events = []
    for line in (folder / 'claude.jsonl').read_text(encoding='utf-8').splitlines():
        try: events.append(json.loads(line))
        except ValueError: pass
    init = next((e for e in events if e.get('subtype') == 'init'), {})
    result = next((e for e in reversed(events) if e.get('type') == 'result'), {})
    models = observed_models(events)
    job.update(exitCode=process.returncode, stoppedAt=now(), sessionId=result.get('session_id') or init.get('session_id'),
        actualModels=models, loadedPlugins=init.get('plugins', []), mcpServers=init.get('mcp_servers', []),
        resultSubtype=result.get('subtype'), usage=result.get('usage'), modelUsage=result.get('modelUsage'),
        durationMs=result.get('duration_ms'), hookEvents=sum(e.get('subtype', '').startswith('hook_') for e in events),
        appliedEffort=actual_effort(result.get('session_id') or init.get('session_id')))
    write(folder / 'result.json', result)
    (folder / 'HANDOFF.md').write_text(result.get('result') or 'No final response; inspect stream and partial diff.', encoding='utf-8')
    if job['status'] == 'running':
        job['status'] = 'ready_for_review' if process.returncode == 0 and result and not result.get('is_error') else 'failed_preserved'
    reset = quota_reset_from_events(result, events, {}, dt.datetime.now(KST)) if job['status']=='failed_preserved' else None
    if reset is not None:
        job['status']='handoff_pending'; job['relayContinuation']={'status':'pending','reason':'confirmed_claude_usage_exhausted','at':now()}
        with state_lock():
            state=read(STATE); state['productionEnvironment'].setdefault('relaySupport',{}).setdefault('observations',{})['claude-code']={
                'source':'claude_cli_confirmed_quota','observedAt':now(),'ordinaryUsageAllowed':False,
                'rateLimits':[{'percentUsed':100,'resetsAt':reset.isoformat()}]}
            write(STATE,state)
    record(job)
    try: relay_checkpoint('claude-code',job['id'],'worker_finished',job['goal'],session_id=job.get('sessionId'),
            generation=job.get('relayGeneration'),safe_point=process.returncode==0)
    except Exception as exc: print('Worker recovery snapshot unavailable: '+type(exc).__name__,flush=True)


def codex_worker_command(job):
    """Use installed CLI and normal auth, with explicit isolation of external capabilities."""
    executable = shutil.which('codex')
    if not executable: raise RuntimeError('Codex CLI unavailable; job preserved')
    command = [executable, '--no-daemon', 'exec', '--ignore-user-config', '--strict-config', '--json', '--color', 'never',
        '--sandbox', 'read-only' if job['role'] in ('critic', 'verifier') else 'workspace-write',
        '-C', job['worktree'], '--model', job['requestedModel'],
        '-c', 'model_reasoning_effort=' + json.dumps(job['requestedEffort']),
        '-c', 'approval_policy="never"', '-c', 'features.plugins=false', '-c', 'features.apps=false',
        '-c', 'features.hooks=false', '-c', 'features.multi_agent=false', '-c', 'mcp_servers={}',
        '-o', str(Path(job['logDirectory']) / 'HANDOFF.md')]
    return command + ['-']


def codex_runtime(session_id, events):
    models = set(); effort = 'UNVERIFIED'; metadata = None
    if session_id:
        # Session ID selects only this job's runtime metadata; never read auth or unrelated conversations.
        sessions = Path.home() / '.codex/sessions'
        for path in sessions.glob('*/*/*/*' + session_id + '*.jsonl'):
            for line in path.read_text(encoding='utf-8').splitlines():
                try: event = json.loads(line)
                except ValueError: continue
                if event.get('type') == 'turn_context':
                    payload = event.get('payload', {})
                    if payload.get('model'): models.add(payload['model'])
                    effort = payload.get('effort') or payload.get('reasoning_effort') or effort
                    metadata = str(path)
    for event in events:
        if event.get('type') == 'turn.started' and event.get('model'):
            models.add(event['model'])
    return sorted(models), effort, metadata


def supervise_codex(job):
    folder = Path(job['logDirectory']); events = []; process = None
    started = time.monotonic()
    try:
        command = codex_worker_command(job)
        with (folder / 'codex.jsonl').open('w', encoding='utf-8') as stdout, \
                (folder / 'codex.stderr').open('w', encoding='utf-8') as stderr:
            process = subprocess.Popen(command, cwd=job['worktree'], stdin=subprocess.PIPE,
                stdout=stdout, stderr=stderr, text=True, encoding='utf-8', creationflags=subprocess.CREATE_NO_WINDOW)
            job.update(status='running', codexPid=process.pid, startedAt=now()); record(job)
            try:
                progress.communicate_active(process,(folder / 'assignment.md').read_text(encoding='utf-8'),
                    idle_seconds=job['timeoutMinutes']*60,
                    paths=[folder/'codex.jsonl',folder/'codex.stderr',
                        *[Path(job['worktree'])/p for p in job.get('allowedFiles',[])]],
                    checkpoint=lambda: relay_checkpoint('codex',job['id'],'periodic',job['goal'],
                        generation=job.get('relayGeneration'),safe_point=False))
            except subprocess.TimeoutExpired:
                process.terminate(); process.wait(timeout=15)
                job.update(status='timeout_preserved', error='Inactivity timeout; partial work/logs retained; inspect cause before retry')
        for line in (folder / 'codex.jsonl').read_text(encoding='utf-8').splitlines():
            try: events.append(json.loads(line))
            except ValueError: continue
        sid = next((e.get('thread_id') for e in events if e.get('type') == 'thread.started'), None)
        completed = next((e for e in reversed(events) if e.get('type') == 'turn.completed'), {})
        errors = [e for e in events if e.get('type') in ('error', 'turn.failed')]
        models, effort, metadata = codex_runtime(sid, events)
        job.update(exitCode=process.returncode, sessionId=sid, actualModels=models, appliedEffort=effort,
            runtimeMetadata=metadata, modelVerification='runtime_metadata' if models else 'UNVERIFIED',
            durationMs=round((time.monotonic()-started)*1000), usage=completed.get('usage'),
            mcpServers=[], mcpPolicy='CLI mcp_servers={}; user config/plugins/apps/hooks disabled; independent daemon',
            loadedPlugins=[], hookEvents=0)
        fatal = any(e.get('type') == 'turn.failed' for e in errors)
        result = {'provider':'codex', 'session_id':sid, 'is_error': fatal or process.returncode != 0 or not completed,
            'completion':completed, 'errors':errors, 'modelVerification':job['modelVerification'],
            'result':(folder/'HANDOFF.md').read_text(encoding='utf-8') if (folder/'HANDOFF.md').is_file() else ''}
        if job['status'] == 'running':
            job['status'] = 'ready_for_review' if not result['is_error'] and completed and result['result'] else 'failed_preserved'
        write(folder/'result.json', result)
    except Exception as exc:
        job.update(status='failed_preserved', error=str(exc))
    finally:
        job['stoppedAt'] = now(); record(job)


def candidate_patch(job, paths):
    """Compare worker output to its dirty local starting content, including untracked source."""
    pieces = []
    for relative in paths:
        baseline = safe_path(relative, Path(job['logDirectory']) / 'source-baseline')
        expected = job['originalHashes'].get(relative)
        if not baseline.is_file():
            baseline = safe_path(relative)
        if digest(baseline) != expected:
            # A clean tracked file's snapshot can differ only by checkout line endings; the unchanged
            # original (same hash as at assignment) is then the true baseline.
            original = safe_path(relative)
            if digest(original) != expected or (baseline.is_file() and
                    baseline.read_text(encoding='utf-8-sig').splitlines() != original.read_text(encoding='utf-8-sig').splitlines()):
                raise RuntimeError('Worker baseline unavailable or changed; no integration: ' + relative)
            baseline = original
        before = [l + '\n' for l in baseline.read_text(encoding='utf-8-sig').splitlines()] if baseline.is_file() else []
        target = safe_path(relative, Path(job['worktree']))
        after = [l + '\n' for l in target.read_text(encoding='utf-8-sig').splitlines()] if target.is_file() else []
        pieces.extend(difflib.unified_diff(before, after, fromfile='baseline/'+relative, tofile='candidate/'+relative))
    return ''.join(pieces)


def review(args):
    job = get_job(args.id); wt = Path(job['worktree'])
    changed = set(git('diff', 'HEAD', '--name-only', cwd=wt).splitlines())
    changed |= set(git('ls-files', '--others', '--exclude-standard', cwd=wt).splitlines())
    actual = [p for p in sorted(changed) if digest(safe_path(p, wt)) != job['worktreeBaseline'].get(p)]
    # A requested worker handoff is retained evidence, never a production integration file.
    handoff_artifacts = [p for p in actual if p == 'HANDOFF.md']
    actual = [p for p in actual if p not in handoff_artifacts]
    outside = sorted(set(actual) - set(job['allowedFiles']))
    conflicts = [p for p in job['allowedFiles'] if digest(safe_path(p)) != job['originalHashes'][p]]
    result = {'id': job['id'], 'status': job['status'], 'actualModels': job.get('actualModels'),
              'sessionId': job.get('sessionId'), 'changedFiles': actual, 'outsideScope': outside, 'originalConflicts': conflicts,
              'preservedHandoffArtifacts': handoff_artifacts}
    print(json.dumps(result, ensure_ascii=False, indent=2))
    patch = candidate_patch(job, sorted(set(actual) & set(job['allowedFiles'])))
    print(patch)
    (Path(job['logDirectory']) / 'review.patch').write_text(patch + '\n', encoding='utf-8')
    if args.integrate:
        if not args.reviewed or job['status'] != 'ready_for_review' or outside or conflicts or not actual:
            raise RuntimeError('Integration requires successful worker, explicit lead diff review, nonempty in-scope diff and unchanged originals')
        if git('rev-parse', 'HEAD') != job['baseHead']:
            raise RuntimeError('Primary HEAD changed; rebase/review is needed before integration')
        for p in actual:
            source = safe_path(p, wt); target = safe_path(p)
            if not source.is_file(): raise RuntimeError('Deletion is not authorized')
            backup = Path(job['logDirectory']) / 'integration-backup' / p
            backup.parent.mkdir(parents=True, exist_ok=True)
            if digest(target) != job['originalHashes'][p]:
                raise RuntimeError('Original changed before copying; no overwrite: ' + p)
            if target.exists(): shutil.copy2(target, backup)
            target.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(source, target)
        job.update(status='integrated_pending_unity_validation', integratedAt=now(), integratedFiles=actual,
                   reviewEvidence=str(Path(job['logDirectory']) / 'review.patch'))
        record(job)


def enforce_production_entry(args):
    # Keep the guard separate from command routing so queue tests never launch production.
    with state_lock():
        if hashlib.sha256(Path(__file__).read_bytes()).hexdigest() != _ENTRY_SOURCE_SHA256:
            raise RuntimeError('PRODUCTION_GUARD:HELPER_SOURCE_CHANGED_RESTART_REQUIRED')
        state = read(STATE)
        ledger = state.get('productionEnvironment', {}).get('productionDispatch', {}).get('ledger', [])
        action = args.action
        observational = (action == 'status' or action == 'review' and not args.integrate
            or action == 'start' and args.check_only
            or action in ('autonomy','relay') and args.operation == 'status'
            or action == 'native' and args.native_action in ('state','shot'))
        if action == 'mcp':
            observational = args.method in ('initialize','notifications/initialized','tools/list','resources/list','resources/read')
            if args.method == 'tools/call':
                params = read(safe_path(args.params_file)) if args.params_file else json.loads(args.params)
                observational = (params.get('name') == 'read_console' or params.get('name') == 'manage_editor'
                    and params.get('arguments', {}).get('action') in ('get_state','get_info'))
        if not observational and any(row.get('operation') == 'tooling_application' and row.get('status') == 'in_flight'
                                     for row in ledger if isinstance(row, dict)):
            raise RuntimeError('PRODUCTION_GUARD:TOOLING_APPLICATION_IN_FLIGHT')
        # Import only a stable guard while holding the same lock used by reviewed installation.
        guard_spec = importlib.util.spec_from_file_location('pa_production_guard', ROOT / 'Tools/PaAgentConsole/production_adapter.py')
        guard = importlib.util.module_from_spec(guard_spec); guard_spec.loader.exec_module(guard)
    # Permit consumption acquires this existing lock itself; never nest its non-reentrant lock.
    guard.enforce_entry(args)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    sub = p.add_subparsers(dest='action', required=True)
    start = sub.add_parser('start'); start.add_argument('--check-only', action='store_true'); start.add_argument('--prepare-session', action='store_true')
    start.add_argument('--owner',default='claude-code',choices=['claude-code','codex'])
    start.add_argument('--effort', default='high', choices=['medium', 'high', 'xhigh', 'max'])
    start.add_argument('--intake-check',action='store_true')
    start.add_argument('--headless',action='store_true');start.add_argument('--prompt-file');start.add_argument('--timeout',type=float,default=45,help='Minutes without child output/owned-file activity; not total task duration')
    start.add_argument('--resume-session',help='Resume this actual Claude session ID without replacing its transcript')
    start.add_argument('--autonomous',action='store_true'); start.add_argument('--foreground',action='store_true')
    start.add_argument('--lease-token')
    control=sub.add_parser('autonomy'); control.add_argument('operation',choices=['status','watch','pause','observe','checkpoint','resume-due'])
    control.add_argument('--once',action='store_true'); control.add_argument('--interval',type=int,default=5)
    control.add_argument('--session-id'); control.add_argument('--observation',default='{}')
    control.add_argument('--disposition',choices=['continue','blocked','human-review'])
    control.add_argument('--next-action',default=''); control.add_argument('--evidence',nargs='*',default=[])
    w = sub.add_parser('worker'); w.add_argument('--id'); w.add_argument('--files', nargs='*', default=[]); w.add_argument('--goal', required=True)
    w.add_argument('--role', default='builder', choices=['builder','critic','reviser','verifier']); w.add_argument('--evidence', nargs='*', default=[])
    w.add_argument('--reference-group',default='world',choices=['world','motion','ui','all','none'])
    w.add_argument('--provider',default='auto',choices=['auto','claude-code','codex'])
    w.add_argument('--model'); w.add_argument('--effort', default='high', choices=['low','medium','high','xhigh','max'])
    w.add_argument('--timeout', type=float, default=30, help='Inactivity minutes, not task duration'); w.add_argument('--serena', action='store_true')
    relay = sub.add_parser('relay'); relay.add_argument('operation',choices=['status','observe','checkpoint','tick'])
    relay.add_argument('--provider',default='codex',choices=['codex','claude-code']); relay.add_argument('--id')
    relay.add_argument('--session-id'); relay.add_argument('--generation'); relay.add_argument('--reason',default='safe_boundary')
    relay.add_argument('--next-action',default=''); relay.add_argument('--safe-point',action='store_true')
    relay.add_argument('--evidence',nargs='*',default=[])
    for name in ('status', 'supervise', 'review'):
        x = sub.add_parser(name); x.add_argument('--id', required=True)
        if name == 'review': x.add_argument('--integrate', action='store_true'); x.add_argument('--reviewed', action='store_true')
        if name == 'supervise': x.add_argument('--registered-start', action='store_true')
    rpc = sub.add_parser('mcp'); rpc.add_argument('--method', default='tools/list'); rpc.add_argument('--params', default='{}')
    rpc.add_argument('--params-file')
    rpc.add_argument('--output')
    editor = sub.add_parser('editor'); editor.add_argument('--action', dest='editor_action', required=True, choices=['connect','build','performance','capture'])
    candidate = sub.add_parser('candidate'); candidate.add_argument('--id', required=True)
    candidate.add_argument('--save-root', required=True); candidate.add_argument('--output', required=True)
    candidate.add_argument('--label', required=True); candidate.add_argument('--session-id')
    candidate.add_argument('--dry-run', action='store_true')
    native = sub.add_parser('native'); native.add_argument('--pid',type=int,required=True)
    native.add_argument('--log',default='Logs/ParallelProduction/NativeInput')
    native.add_argument('--game-view',action='store_true')
    native.add_argument('native_action',choices=['focus','shot','key','click','state']);native.add_argument('values',nargs='*')
    args = p.parse_args()
    if ROOT!=PRIMARY_ROOT or Path(git('rev-parse', '--show-toplevel')).resolve() != ROOT:
        raise RuntimeError('Production controls require the original Project_PA root, never a worker worktree')
    if args.action == 'supervise' and args.registered_start:
        registered_supervisor_release(sys.stdin)
    # One finite dispatch must reserve cumulative budget before the original helper mutates production.
    enforce_production_entry(args)
    if args.action == 'worker': worker_start(args)
    elif args.action == 'relay':
        if args.operation == 'observe': output=observe_codex_usage()
        elif args.operation == 'tick': output=relay_tick()
        elif args.operation == 'checkpoint': output=relay_checkpoint(args.provider,args.id,args.reason,args.next_action,
            args.evidence,args.session_id,args.generation,args.safe_point)
        else:
            state=read(STATE); output={'choice':relay_choice(state),'relaySupport':state['productionEnvironment'].get('relaySupport',{})}
        print(json.dumps(output,ensure_ascii=False,indent=2))
    elif args.action == 'autonomy': autonomy_control(args)
    elif args.action == 'supervise': supervise(args)
    elif args.action == 'status':
        job = get_job(args.id)
        stream = Path(job['logDirectory']) / ('codex-app-server.jsonl' if job.get('executionMethod')=='app_server' else
            'codex.jsonl' if job.get('provider') == 'codex' else 'claude.jsonl')
        if stream.exists():
            events = []
            for line in stream.read_text(encoding='utf-8').splitlines():
                try: events.append(json.loads(line))
                except ValueError: pass
            init = next((e for e in events if e.get('subtype') == 'init' or e.get('type') == 'thread.started'), {})
            job['sessionId'] = job.get('sessionId') or init.get('session_id') or init.get('thread_id')
            job['liveEventCount'] = len(events)
            job['lastEventType'] = events[-1].get('type') if events else None
        brief = {k:v for k,v in job.items() if k not in ('worktreeBaseline','overlay','originalHashes')}
        print(json.dumps(brief, ensure_ascii=False, indent=2))
    elif args.action == 'review': review(args)
    elif args.action == 'native': native_input(args)
    elif args.action == 'candidate': candidate_launch(args)
    elif args.action == 'mcp':
        d = doctor(); init, session = d.rpc('initialize', {'protocolVersion':'2024-11-05','capabilities':{},'clientInfo':{'name':'pa-production','version':'2'}})
        protocol = init.get('protocolVersion','2024-11-05'); d.rpc('notifications/initialized',session=session,protocol=protocol)
        parameters = read(safe_path(args.params_file)) if args.params_file else json.loads(args.params)
        result, _ = d.rpc(args.method,parameters,session=session,protocol=protocol,request_id=2,timeout=180)
        if args.output: write(safe_path(args.output),result)
        print(json.dumps(result, ensure_ascii=True))
    elif args.action == 'editor':
        job_id = str(uuid.uuid4()); write(ROOT / 'Logs/ParallelProduction/editor-request.json', {'id':job_id,'action':args.editor_action})
        print('Editor command queued: ' + job_id)
    elif args.action == 'start':
        return lead_start(args)
    return 0


if __name__ == '__main__':
    try: sys.exit(main())
    except Exception as exc:
        print(json.dumps({'status':'BLOCKED_PRESERVED','source':type(exc).__name__,'reason':str(exc)}, ensure_ascii=True), file=sys.stderr)
        sys.exit(1)
