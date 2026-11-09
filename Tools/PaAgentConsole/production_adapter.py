"""Finite production dispatcher over the existing helper/state. Disabled until a new user resume.

No model API, default activation, budget reset, or implicit writer transfer.
The request pins the current authority and identifies one operation, not a free shell command.
"""
from __future__ import annotations
import argparse
import contextlib
import datetime as dt
import hashlib
import importlib.util
import json
import math
import os
import re
from pathlib import Path
import subprocess
import sys
import uuid

ROOT = Path(__file__).resolve().parents[2]
STATE = Path('Automation/LoopEngineering/State/loop-state.json')
HANDOFF = Path('Docs/00_CURRENT/CODEX_HANDOFF.md')
COUNTERS = {'compileFix': 'compileCycles', 'targetedPlay': 'targetedPlayRuns',
            'dedicatedValidators': 'dedicatedValidatorsAdded'}
ACTIVE = {'approved_not_started', 'preapproved', 'active', 'in_progress'}
sys.dont_write_bytecode = True

class GuardError(ValueError):
    pass

def now():
    return dt.datetime.now(dt.timezone.utc).isoformat()

def digest(data):
    return hashlib.sha256(data).hexdigest()

def read(path):
    return json.loads(Path(path).read_text('utf-8-sig'))


def tooling_application_errors(state):
    ledger = state.get('productionEnvironment', {}).get('productionDispatch', {}).get('ledger', [])
    return ['TOOLING_APPLICATION_IN_FLIGHT'] if any(
        row.get('operation') == 'tooling_application' and row.get('status') == 'in_flight'
        for row in ledger if isinstance(row, dict)) else []

def write(path, value):
    path = Path(path); path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_name(path.name + '.' + uuid.uuid4().hex + '.tmp')
    tmp.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', 'utf-8')
    os.replace(tmp, path)

def helper(root=ROOT):
    spec = importlib.util.spec_from_file_location('pa_existing_production', root / 'Tools/LoopEngineering/Invoke-ProjectPAProduction.py')
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module

def budget_policy(state, root=ROOT):
    """Resolve current policy from the selected record AND its actual contract amendment."""
    key=state.get('demoProductionRouting',{}).get('selectedApproval')
    approval=state.get(key,{})
    if approval.get('executionPolicy')!='APPROVED_GOAL_ADAPTIVE_PRODUCTION':
        return {'mode':'bounded','fixedCapsApply':True,'source':approval.get('contract')}
    contract=approval.get('contract')
    try:
        path=safe_relative(root,contract)
        amendment=path.read_text('utf-8-sig').splitlines()[0]
    except (OSError,ValueError,IndexError):
        return {'mode':'unverified','fixedCapsApply':None,'source':contract}
    if all(term in amendment for term in ('2026-10-09 policy amendment:', 'adaptive repair',
                                         'per-ticket counts', 'not current execution gates')):
        return {'mode':'adaptive','fixedCapsApply':False,'source':contract,'amendment':amendment}
    return {'mode':'unverified','fixedCapsApply':None,'source':contract}

def budget_errors(state, costs=None, root=ROOT):
    """Preserve counts; enforce current caps only when the selected contract still makes them binding."""
    route = state.get('demoProductionRouting', {}); key = route.get('selectedApproval')
    approval = state.get(key, {}); ticket = approval.get('activeTicket')
    result = approval.get('results', {}).get(ticket, {})
    totals = result.get('approvedTotalBudget', approval.get('perTicketBudget', {}))
    policy=budget_policy(state,root)
    errors = ['BUDGET_POLICY_REVIEW_REQUIRED'] if policy['mode']=='unverified' else []
    for dimension, counter in COUNTERS.items():
        used, cap = result.get(counter), totals.get(dimension)
        ledger = state.get('productionEnvironment', {}).get('productionDispatch', {}).get('ledger', [])
        known=[x.get('usedAfter',{}).get(dimension) for x in ledger if x.get('approvalKey')==key and x.get('ticket')==ticket]
        high=max([0]+[n for n in known if type(n) is int and n>=0])
        if high and (type(used) is not int or used < high):
            errors.append('CUMULATIVE_BUDGET_REGRESSION:' + dimension)
        if policy['mode']=='adaptive':
            if used is not None and (type(used) is not int or used<0): errors.append('INVALID_USAGE_RECORD:'+dimension)
            continue
        if type(used) is not int or type(cap) is not int or used < 0 or cap < 0:
            errors.append('UNKNOWN_BUDGET:' + dimension); continue
        required = (costs if costs is not None else {'compileFix':1,'targetedPlay':1,'dedicatedValidators':0}).get(dimension, 0)
        if used > cap or used + required > cap:
            errors.append('BUDGET_EXHAUSTED:' + dimension)
    return errors

def authority(root=ROOT):
    raw = (root / STATE).read_bytes(); state = json.loads(raw.decode('utf-8-sig'))
    route = state.get('demoProductionRouting', {}); key = route.get('selectedApproval')
    approval = state.get(key, {}); contract = approval.get('contract')
    path = safe_relative(root, contract) if isinstance(contract, str) else None
    return state, {'approvalKey': key, 'ticket': approval.get('activeTicket'),
        'owner': route.get('unityEditorOwner'), 'stateSha256': digest(raw),
        'handoffSha256': digest((root / HANDOFF).read_bytes()),
        'contractSha256': digest(path.read_bytes()) if path and path.is_file() else None,
        'workspaceSha256': workspace_digest(root),
        'baselineHead': subprocess.run(['git', 'rev-parse', 'HEAD'], cwd=root, check=True,
                       capture_output=True, text=True).stdout.strip()}

def workspace_digest(root=ROOT):
    paths = []
    for command in (['diff','--name-only','-z','HEAD','--','Assets','Packages','ProjectSettings'],
                    ['ls-files','--others','--exclude-standard','-z','--','Assets','Packages','ProjectSettings']):
        result = subprocess.run(['git',*command],cwd=root,capture_output=True,check=True)
        paths += result.stdout.decode('utf-8').split('\0')
    entries = []
    for name in sorted(set(paths)-{''}):
        path=safe_relative(root,name)
        entries.append([name,digest(path.read_bytes()) if path.is_file() else 'MISSING'])
    return digest(json.dumps(entries,ensure_ascii=False).encode())

def safe_relative(root, relative):
    if not isinstance(relative, str) or not relative or Path(relative).is_absolute():
        raise GuardError('Relative project path required')
    candidate = root / relative
    if any(p.is_symlink() or (p.exists() and getattr(p.lstat(), 'st_file_attributes', 0) & 0x400)
           for p in [candidate, *candidate.parents] if p != root.parent):
        raise GuardError('Linked path refused')
    if not candidate.resolve().is_relative_to(root.resolve()) or candidate.resolve() == root.resolve():
        raise GuardError('Path escapes project')
    return candidate

def fresh(observation, seconds=60):
    try:
        timestamp = dt.datetime.fromisoformat(observation['observedAt'].replace('Z', '+00:00'))
        return timestamp.tzinfo is not None and 0 <= (dt.datetime.now(dt.timezone.utc)-timestamp).total_seconds() <= seconds
    except (KeyError, TypeError, ValueError):
        return False

def processes():
    import csv, io
    output = subprocess.run(['tasklist', '/FO', 'CSV', '/NH'], capture_output=True,
                            text=True, check=True, timeout=15).stdout
    return {int(r[1]): r[0].lower() for r in csv.reader(io.StringIO(output)) if len(r)>1 and r[1].isdigit()}

def authorization_errors(state, pin, request, root=ROOT):
    r = state.get('demoProductionRouting', {}); a = state.get(pin['approvalKey'], {})
    errors = []
    for key in ('approvalKey', 'ticket', 'baselineHead', 'workspaceSha256', 'stateSha256', 'handoffSha256', 'contractSha256'):
        if not pin.get(key) or request.get(key) != pin[key]: errors.append('STALE_OR_MISSING:' + key)
    sequence = a.get('approvedTickets', [])
    if (a.get('status') not in ACTIVE or a.get('policyId') != 'PREAPPROVED_MILESTONE_CONTINUATION'
            or not a.get('approvalSource') or not a.get('stopAtMilestone')
            or a.get('pushAllowed') is not False or a.get('completionReached')
            or r.get('stopAtMilestoneReached') or pin['ticket'] not in sequence
            or a.get('preapprovedThrough') not in sequence):
        errors.append('APPROVAL_NOT_VALID')
    elif sequence.index(pin['ticket']) > sequence.index(a['preapprovedThrough']):
        errors.append('TICKET_OUTSIDE_APPROVAL')
    status = str(a.get('ticketStatus', {}).get(pin['ticket'], 'UNKNOWN')).lower()
    adaptive = budget_policy(state, root)['mode'] == 'adaptive'
    # Failure/budget rows are evidence, not user rejection or renewed task limits.
    # Reworking accepted work needs an actual unresolved checkpoint, not a reset to NOT_STARTED.
    if any(x in status for x in ('reject','hold','unknown')) or (not adaptive and any(
            x in status for x in ('blocked','incomplete','pass','accepted'))):
        errors.append('TICKET_NOT_RUNNABLE')
    if adaptive and any(x in status for x in ('pass','accepted')):
        evidence = request.get('checkpointEvidence')
        if not evidence or not safe_relative(root,evidence).is_file() or not request.get('unresolvedReason'):
            errors.append('UNRESOLVED_CHECKPOINT_REQUIRED')
    env = state.get('productionEnvironment', {})
    activation = env.get('productionDispatch', {}).get('resumeAuthorization', {})
    if (activation.get('enabled') is not True or not activation.get('userMessageEvidence')
            or activation.get('approvalKey') != pin['approvalKey'] or activation.get('ticket') not in sequence
            or activation.get('baselineHead') != pin['baselineHead']):
        errors.append('USER_02_PRODUCTION_RESUME_REQUIRED')
    else:
        proof = activation.get('userMessageEvidence')
        if not isinstance(proof,str) or not proof.startswith('Logs/AgentConsole/'):
            errors.append('RESUME_EVIDENCE_REQUIRED')
        else:
            proof_path=safe_relative(root,proof)
            if not proof_path.is_file() or digest(proof_path.read_bytes()) != activation.get('userMessageSha256'):
                errors.append('RESUME_EVIDENCE_REQUIRED')
    if env.get('baseHead') != pin['baselineHead']: errors.append('PRODUCTION_BASELINE_MISMATCH')
    lease = env.get('coordinatorLease', {})
    if (request.get('provider') != pin['owner'] or lease.get('owner') != pin['owner']
            or lease.get('status') not in ('running','active')
            or not request.get('sessionId') or request.get('sessionId') != lease.get('sessionId')):
        errors.append('SINGLE_WRITER_LEASE_NOT_ACTIVE')
    return errors

def operation(request):
    """Only explicit helper operations; compound arbitrary code is refused."""
    action = request.get('operation'); payload = request.get('params', {})
    costs = {k: 0 for k in COUNTERS}
    if not isinstance(payload, dict): raise GuardError('Object params required')
    if action in ('compile','play','stop','capture'):
        if action == 'compile':
            costs['compileFix'] = 1
            tool = {'name':'refresh_unity', 'arguments':{'mode':'force', 'scope':'scripts','compile':'request','wait_for_ready':True}}
        elif action in ('play','stop'):
            costs['targetedPlay'] = int(action == 'play')
            tool = {'name':'manage_editor','arguments':{'action':action}}
        else:
            tool = {'name':'manage_scene','arguments':{'action':'screenshot'}}
        return ['mcp','--method','tools/call'], costs, tool
    if action == 'integrate':
        if not isinstance(payload.get('workerId'), str): raise GuardError('Exact reviewed worker required')
        # Integration may add validators: reserve declared validator count before integrating.
        count = payload.get('newValidators', 0)
        if type(count) is not int or count < 0: raise GuardError('Invalid validator count')
        costs['dedicatedValidators'] = count
        return ['review','--id',payload['workerId'],'--integrate','--reviewed'], costs, None
    if action == 'build':
        return ['editor','--action','build'], costs, None
    if action == 'native':
        pid, act, values = payload.get('pid'), payload.get('action'), payload.get('values', [])
        if type(pid) is not int or pid <= 0 or payload.get('gameView') is not True:
            raise GuardError('Native permit is limited to the exact existing Editor GameView')
        if act not in ('focus','key','click') or not isinstance(values,list) or not all(isinstance(x,str) for x in values):
            raise GuardError('Native action/values invalid')
        if act == 'focus' and values: raise GuardError('Focus takes no values')
        if act == 'key':
            allowed = set('ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789') | {'SPACE','ENTER','TAB','SHIFT','ESC'}
            keys = values[0].upper().split('+') if values else []
            if (len(values) not in (1,2) or not keys or len(keys)>3 or len(set(keys))!=len(keys)
                    or any(k not in allowed for k in keys)):
                raise GuardError('Only game keys, no editor/system modifier shortcuts')
            try: duration=float(values[1]) if len(values)==2 else .12
            except ValueError: raise GuardError('Invalid key duration')
            if not math.isfinite(duration) or not 0 < duration <= 10: raise GuardError('Invalid key duration')
        if act == 'click' and (len(values)!=2 or any(not re.fullmatch(r'\d{1,5}',v) for v in values)):
            raise GuardError('Click requires client x/y')
        qa = payload.get('qaRun')
        if not isinstance(qa,str) or not re.fullmatch(r'Logs/VisualQA/\d{8}-\d{6}-NATIVE_PRODUCTION',qa):
            raise GuardError('Exact existing Native Production Input Session required')
        return ['native','--pid',str(pid),'--game-view','--log',qa+'/native-input',act,*values], costs, None
    if action == 'unity':
        # Use the existing Editor's structured tools, under the same owner/authority permit.
        # No shell, package installation, deletion or arbitrary C# execution is introduced.
        allowed = {'manage_scene','manage_gameobject','manage_components','manage_material',
                   'manage_prefabs','manage_animation','manage_ui','manage_camera','manage_profiler',
                   'run_tests','get_test_job','read_console'}
        tool = payload.get('toolCall',{})
        arguments = tool.get('arguments',{})
        if tool.get('name') not in allowed or not isinstance(arguments,dict):
            raise GuardError('Use an existing structured Unity production tool')
        action_name = str(arguments.get('action','')).lower()
        if any(word in action_name for word in ('delete','remove','import_package','install')):
            raise GuardError('Deletion/package operations are outside approval')
        serialized = json.dumps(arguments).replace('\\','/').lower()
        if any(p in serialized for p in ('prototype_firstday','smartphoneui.cs','project_d')):
            raise GuardError('Protected product authority cannot be edited')
        return ['mcp','--method','tools/call'], costs, tool
    raise GuardError('Unknown operation. Use existing isolated support runner for non-owner candidates.')

def validate_native_context(state, request, actual, root=ROOT):
    """Only the actually active native fixture's isolated save override is allowed.

    Dirty scene and held/unknown input are never exempted. Candidate launch is not added.
    The consuming child collects the real Editor/window observations again.
    """
    lease=state.get('productionEnvironment',{}).get('coordinatorLease',{})
    if ('generation' not in request or request['generation']!=lease.get('generation')
            or request.get('sessionId')!=lease.get('sessionId') or request.get('provider')!=lease.get('owner')
            or lease.get('status') not in ('active','running')):
        raise GuardError('Native current owner/session/generation required (legacy null must be explicit)')
    control=state.get('productionEnvironment',{}).get('autonomous',{})
    if control.get('stopRequested') is True or control.get('status') in ('pause_requested','paused_by_user'):
        raise GuardError('User stop is active; native input is not resumed')
    if not fresh(actual) or actual.get('projectRoot')!=str(root): raise GuardError('Fresh actual native observation required')
    if actual.get('editorPid')!=request['params']['pid'] or actual.get('windowPid')!=actual.get('editorPid'):
        raise GuardError('Actual Editor and target PID differ')
    if (not actual.get('processCreationFileTime') or actual.get('processCreationFileTime')!=actual.get('editorCreationFileTime')
            or actual.get('windowTitle')!='Game' or type(actual.get('window')) is not int or actual['window']<=0):
        raise GuardError('Exact Editor identity/GameView required')
    for name,expected in [('isCompiling',False),('sceneDirty',False),('isPlaying',True),('inputReleased',True),
                          ('qaActive',True),('saveOverrideClear',False)]:
        if actual.get(name) is not expected: raise GuardError('Native state unknown/unsafe:'+name)
    if actual.get('scenePath')!='Assets/Scenes/WorldSandbox.unity' or actual.get('qaMode')!='native':
        raise GuardError('Active native WorldSandbox fixture required')
    qa=safe_relative(root,request['params']['qaRun']); save=safe_relative(root,request['params']['qaRun']+'/save')
    if (not save.is_dir() or Path(str(actual.get('qaOut',''))).resolve()!=qa.resolve()
            or Path(str(actual.get('saveRoot',''))).resolve()!=save.resolve()):
        raise GuardError('Actual isolated QA output/save do not match this execution')
    proof=qa/'result.txt'; body=proof.read_text('utf-8-sig') if proof.is_file() else ''
    if 'READY native Windows input' not in body or 'STOP native-input session' in body:
        raise GuardError('Active native fixture evidence missing/ended')
    if request['params']['action']=='click':
        width,height=actual.get('clientWidth'),actual.get('clientHeight');x,y=map(int,request['params']['values'])
        if type(width) is not int or type(height) is not int or not (0<=x<width and 0<=y<height):
            raise GuardError('Click outside observed GameView client')
    return dict(actual,owner=lease.get('owner'),sessionId=lease.get('sessionId'),generation=lease.get('generation'),
                qaProofSha256=digest(proof.read_bytes()))


def inspect(request, root=ROOT):
    state, pin = authority(root); argv, costs, tool = operation(request)
    errors = (tooling_application_errors(state) + authorization_errors(state, pin, request, root)
              + budget_errors(state, costs, root))
    # Any operation that can change/compile the product requires remaining verification budget.
    if request['operation'] in ('integrate','build'):
        errors += budget_errors(state, {'compileFix':1,'targetedPlay':1,'dedicatedValidators':costs['dedicatedValidators']},root)
    errors += recovery_errors(state, pin, request, root)
    if request['operation']=='native':
        lease=state.get('productionEnvironment',{}).get('coordinatorLease',{})
        control=state.get('productionEnvironment',{}).get('autonomous',{})
        if 'generation' not in request or request['generation']!=lease.get('generation'):
            errors.append('EXACT_NATIVE_GENERATION_REQUIRED')
        if control.get('stopRequested') is True or control.get('status') in ('pause_requested','paused_by_user'):
            errors.append('USER_STOP_PRESERVED')
    return state, pin, argv, costs, tool, sorted(set(errors))

def recovery_errors(state, pin, request, root=ROOT):
    """Diagnose an unchanged failed action before retry, without an attempt-count stop."""
    ledger = state.get('productionEnvironment',{}).get('productionDispatch',{}).get('ledger',[])
    prior = next((x for x in reversed(ledger) if x.get('approvalKey')==pin['approvalKey']
        and x.get('ticket')==pin['ticket'] and x.get('operation')==request['operation']),None)
    if not prior or prior.get('returncode') in (None,0) or prior.get('workspaceSha256') != pin.get('workspaceSha256'):
        return []
    recovery = request.get('recovery',{})
    proof = recovery.get('evidence')
    if not recovery.get('cause') or not recovery.get('changedApproach') or not isinstance(proof,str):
        return ['RECOVERY_CHECKPOINT_REQUIRED:diagnose_unchanged_failed_action']
    path = safe_relative(root,proof)
    if not path.is_file() or digest(path.read_bytes()) == prior.get('recoveryEvidenceSha256'):
        return ['RECOVERY_CHECKPOINT_REQUIRED:new_evidence_required']
    return []

def entry_requires_permit(args):
    action = args.action
    if action == 'start': return not args.check_only
    if action in ('supervise', 'editor'): return True
    if action == 'review': return args.integrate
    if action == 'worker': return True  # independent support uses the separate read-only runner
    if action == 'candidate': return not args.dry_run
    if action == 'native': return args.native_action not in ('state','shot')
    if action == 'relay': return args.operation == 'tick'
    if action == 'autonomy': return args.operation == 'watch'
    if action == 'mcp':
        if args.method in ('initialize','notifications/initialized','tools/list','resources/list','resources/read'): return False
        params = read(safe_relative(ROOT, args.params_file)) if args.params_file else json.loads(args.params)
        return not (args.method == 'tools/call' and (params.get('name') == 'read_console'
            or (params.get('name') == 'manage_editor' and params.get('arguments',{}).get('action') in ('get_state','get_info'))))
    return False

def enforce_entry(args):
    """Called by the original helper before any production mutation, including old launchers."""
    if not entry_requires_permit(args): return
    relative = os.environ.get('PA_PRODUCTION_PERMIT')
    # Lead/isolated worker startup itself spends no Unity compile/Play. Preserve the existing
    # route behind the same explicit resume gate, exact owner and finite-run checks.
    if not relative and args.action in ('start','worker','supervise'):
        h = helper()
        with h.state_lock():
            state,pin=authority()
            if tooling_application_errors(state):
                raise GuardError('PRODUCTION_GUARD:TOOLING_APPLICATION_IN_FLIGHT')
        lease=state.get('productionEnvironment',{}).get('coordinatorLease',{})
        request=dict(pin,provider=pin['owner'],sessionId=lease.get('sessionId'))
        errors=authorization_errors(state,pin,request)
        if args.action=='start':
            errors=[e for e in errors if e!='SINGLE_WRITER_LEASE_NOT_ACTIVE']
            if args.owner!=pin['owner'] or lease.get('owner')!=pin['owner']:
                errors.append('RECORDED_OWNER_REQUIRED')
            live=processes()
            control=state.get('productionEnvironment',{}).get('autonomous',{})
            # User-approved 2026-10-19: the autonomous launcher registers its own detached supervisor
            # child under a fresh generation token first. That exact child holds the lease; it is no duplicate.
            own_child=(getattr(args,'autonomous',False) and getattr(args,'foreground',False)
                and getattr(args,'lease_token',None) and lease.get('generation')==args.lease_token
                and control.get('generation')==args.lease_token
                and lease.get('launcherPid') in (os.getpid(),os.getppid()) and lease.get('cliPid') not in live)
            if not own_child and (lease.get('cliPid') in live or lease.get('launcherPid') in live):
                errors.append('EXACT_PREVIOUS_LEAD_STILL_RUNNING')
        elif args.action=='supervise':
            worker=state.get('productionEnvironment',{}).get('workers',{}).get(args.id,{})
            if not worker or worker.get('supervisorPid')!=os.getpid():
                errors.append('EXACT_REGISTERED_SUPERVISOR_REQUIRED')
        idle = getattr(args,'timeout',45)
        if args.action!='supervise' and (not math.isfinite(idle) or idle <= 0):
            errors.append('INVALID_INACTIVITY_TIMEOUT')
        if os.environ.get('PA_AGENT_CONSOLE_CHILD'):
            errors.append('NESTED_DELEGATION_FORBIDDEN')
        if args.action!='worker' or args.role in ('builder','reviser'):
            errors+=budget_errors(state)
        if errors: raise GuardError('PRODUCTION_GUARD:'+','.join(sorted(set(errors))))
        return
    if not relative: raise GuardError('PRODUCTION_GUARD: use production_adapter; 02 resume and budget required')
    path = safe_relative(ROOT, relative)
    if not path.is_relative_to(ROOT/'Logs/AgentConsole'): raise GuardError('Invalid permit location')
    permit = read(path)
    if permit.get('argvSha256') != digest(json.dumps(sys.argv[1:], ensure_ascii=False).encode()):
        raise GuardError('Permit does not match this exact invocation')
    if getattr(args, 'params_file', None) and digest(safe_relative(ROOT,args.params_file).read_bytes()) != permit.get('toolParamsSha256'):
        raise GuardError('Tool parameters changed after budget reservation')
    h = helper()
    if args.action=='native':
        reservation_request=permit.get('nativeRequest')
        if not isinstance(reservation_request,dict): raise GuardError('Native permit context required')
        native_actual=h.native_runtime_observation(args.pid)
    with h.state_lock():
        state = read(ROOT/STATE)
        if tooling_application_errors(state):
            raise GuardError('PRODUCTION_GUARD:TOOLING_APPLICATION_IN_FLIGHT')
        dispatch = state.get('productionEnvironment',{}).get('productionDispatch',{})
        reservation = next((x for x in dispatch.get('ledger',[]) if x.get('id') == permit.get('id')), None)
        if not reservation or reservation.get('consumedAt') or not fresh(permit):
            raise GuardError('Expired, consumed or unregistered permit')
        if digest((ROOT/STATE).read_bytes()) != permit.get('reservedStateSha256'):
            raise GuardError('Authority changed after reservation; retained as uncertain, no replay')
        _, pin=authority()
        if any(pin.get(k) != permit.get('authority',{}).get(k) for k in
               ('approvalKey','ticket','owner','baselineHead','workspaceSha256','handoffSha256','contractSha256')):
            raise GuardError('Production baseline changed after reservation')
        if os.getppid() != reservation.get('adapterPid') or processes().get(os.getppid()) not in ('python.exe','python312.exe'):
            raise GuardError('Permit parent process mismatch')
        if args.action=='native':
            verified=validate_native_context(state,reservation_request,native_actual,ROOT)
            if reservation.get('nativeRequest')!=reservation_request: raise GuardError('Native request changed after reservation')
            for field in ('window','processCreationFileTime','editorPid','scenePath','saveRoot','qaOut','qaProofSha256'):
                if verified.get(field)!=reservation.get('nativeContext',{}).get(field): raise GuardError('Native runtime changed:'+field)
        reservation['consumedAt'] = now(); reservation['childPid'] = os.getpid()
        write(ROOT/STATE, state)

def dispatch(request, run=False, root=ROOT):
    directory = root/'Logs/AgentConsole'/('production-' + dt.datetime.now(dt.timezone.utc).strftime('%Y%m%dT%H%M%SZ') + '-' + uuid.uuid4().hex[:8])
    directory.mkdir(parents=True)
    state, pin, argv, costs, tool, errors = inspect(request, root)
    report = {'mode':'PREPARE' if not run else 'RUN', 'observedAt':now(), 'authority':pin,
              'operation':request.get('operation'), 'provider':request.get('provider'),
              'sessionId':request.get('sessionId'), 'errors':errors,
              'status':'BLOCKED_PRESERVED' if errors else 'PREPARED_NOT_EXECUTED',
              'nextAction':'Wait for the user to send 02; reconcile current ticket/usage/lease; never reset counters.',
              'gameCompletion':False}
    write(directory/'result.json', report)
    if errors or not run: return report
    observation = request.get('runtimeObservation', {})
    if not fresh(observation) or observation.get('projectRoot') != str(root):
        raise GuardError('Fresh exact Editor observation required')
    live = processes(); lease = state['productionEnvironment']['coordinatorLease']
    native_context=None
    if request['operation']=='native':
        native_context=validate_native_context(state,request,helper(root).native_runtime_observation(request['params']['pid']),root)
        observation=dict(observation,**{k:v for k,v in native_context.items() if k!='inFlight'})
    if observation.get('isCompiling') is not False or observation.get('inFlight') is not False:
        raise GuardError('Unity command in flight/unknown; do not replay')
    if type(observation.get('sceneDirty')) is not bool:
        raise GuardError('Unknown Editor scene state; preserve and inspect before dispatch')
    owned_edit = (request['operation']=='unity' and observation.get('scenePath')
        and observation.get('sceneDirtyOwnerSessionId')==request.get('sessionId')
        and request.get('params',{}).get('preserveDirtyScene') is True)
    if observation['sceneDirty'] and request['operation'] not in ('compile','capture','stop') and not owned_edit:
        raise GuardError('Preserve unsaved scene before a Play/build/integration boundary')
    if observation.get('inputReleased') is not True or (native_context is None and observation.get('saveOverrideClear') is not True):
        raise GuardError('Input/save state unknown')
    if live.get(observation.get('editorPid')) != 'unity.exe': raise GuardError('Exact existing Editor required; adapter does not launch one')
    if live.get(lease.get('cliPid')) != ('claude.exe' if pin['owner']=='claude-code' else 'codex.exe'):
        raise GuardError('Exact writer process unavailable')
    if tool:
        write(directory/'tool-params.json', tool)
        argv += ['--params-file', (directory/'tool-params.json').relative_to(root).as_posix(),
                 '--output',(directory/'tool-result.json').relative_to(root).as_posix()]
    h = helper(root)
    with h.state_lock():
        current, current_pin, _, _, _, current_errors = inspect(request, root)
        if tooling_application_errors(current):
            raise GuardError('PRODUCTION_GUARD:TOOLING_APPLICATION_IN_FLIGHT')
        if current_errors or current_pin != pin: raise GuardError('Authority changed before reservation')
        env = current['productionEnvironment']; d = env.setdefault('productionDispatch', {})
        if any(x.get('status') == 'in_flight' for x in d.get('ledger', [])):
            raise GuardError('A prior dispatch outcome is uncertain; inspect it, never replay')
        result = current[pin['approvalKey']].get('results',{}).get(pin['ticket'],{})
        policy=budget_policy(current,root)
        # Under the amended adaptive policy an absent lifetime total remains absent, never zero.
        # Every new attempt still has an append-only cost record, including failure/timeout.
        for key, amount in costs.items():
            if type(result.get(COUNTERS[key])) is int: result[COUNTERS[key]] += amount
        reservation = {'id':uuid.uuid4().hex,'approvalKey':pin['approvalKey'],'ticket':pin['ticket'],
            'operation':request['operation'],'policy':policy['mode'],'costs':costs,'usedAfter':{k:result.get(v) for k,v in COUNTERS.items()},
            'adapterPid':os.getpid(),'sessionId':request['sessionId'],'provider':pin['owner'],
            'status':'in_flight','startedAt':now(),'resultDirectory':str(directory)}
        if native_context is not None:
            reservation['nativeContext']=native_context
            reservation['nativeRequest']={k:request[k] for k in ('operation','provider','sessionId','generation','params')}
        reservation['workspaceSha256'] = pin.get('workspaceSha256')
        reservation['recovery'] = request.get('recovery')
        proof = request.get('recovery',{}).get('evidence')
        if proof: reservation['recoveryEvidenceSha256'] = digest(safe_relative(root,proof).read_bytes())
        d.setdefault('ledger', []).append(reservation); write(root/STATE,current)
    permit = {'id':reservation['id'],'observedAt':now(),
              'authority':pin,'reservedStateSha256':digest((root/STATE).read_bytes()),
              'toolParamsSha256':digest((directory/'tool-params.json').read_bytes()) if tool else None,
              'argvSha256':digest(json.dumps(argv,ensure_ascii=False).encode())}
    if native_context is not None: permit['nativeRequest']=reservation['nativeRequest']
    write(directory/'permit.json',permit)
    child_env = dict(os.environ); child_env['PA_PRODUCTION_PERMIT']=(directory/'permit.json').relative_to(root).as_posix()
    command = [sys.executable,'-B','-X','utf8',str(root/'Tools/LoopEngineering/Invoke-ProjectPAProduction.py'),*argv]
    # Timeout is an ambiguous outcome. Keep the reservation in flight and never retry automatically.
    try:
        with (directory/'stdout.log').open('wb') as out, (directory/'stderr.log').open('wb') as err:
            process = subprocess.run(command,cwd=root,env=child_env,stdout=out,stderr=err,timeout=180)
        report.update(returncode=process.returncode,status='OBSERVED_REQUIRES_EVIDENCE_REVIEW',
                      evidence=[str(p) for p in directory.glob('tool-result.json')],
                      nextAction='Review actual Unity evidence; exit code is not gameplay or visual acceptance.')
        with h.state_lock():
            current=read(root/STATE)
            record=next(x for x in current['productionEnvironment']['productionDispatch']['ledger'] if x['id']==reservation['id'])
            record.update(status='returned_requires_review',endedAt=now(),returncode=process.returncode)
            write(root/STATE,current)
    except (subprocess.TimeoutExpired, OSError):
        report.update(status='AMBIGUOUS_IN_FLIGHT_NO_REPLAY',nextAction='Inspect exact child, Editor and prior operation before any new dispatch.')
    write(directory/'result.json',report)
    return report

def transfer_errors(state, pin, request, observation, live):
    """A provider's failure is not evidence that its exact child or Unity operation ended."""
    env=state.get('productionEnvironment',{}); lease=env.get('coordinatorLease',{})
    activation=env.get('productionDispatch',{}).get('resumeAuthorization',{})
    errors=[]
    if activation.get('enabled') is not True or activation.get('writerTransferAllowed') is not True:
        errors.append('USER_02_WRITER_TRANSFER_NOT_AUTHORIZED')
    for key in ('approvalKey','ticket','baselineHead','workspaceSha256','stateSha256','handoffSha256','contractSha256'):
        if not pin.get(key) or request.get(key)!=pin[key]: errors.append('STALE_OR_MISSING:'+key)
    if request.get('targetProvider') not in ('claude-code','codex') or request.get('targetProvider')==pin['owner']:
        errors.append('DISTINCT_KNOWN_PROVIDER_REQUIRED')
    if not fresh(observation) or observation.get('projectRoot')!=str(ROOT): errors.append('FRESH_EDITOR_OBSERVATION_REQUIRED')
    previous=lease.get('cliPid')
    if (type(previous) is not int or previous<=0 or observation.get('previousChildPid')!=previous
            or request.get('previousSessionId')!=lease.get('sessionId') or previous in live):
        errors.append('EXACT_PREVIOUS_CHILD_NOT_CONFIRMED_EXITED')
    for key in ('inFlight','isCompiling','isPlaying','sceneDirty'):
        if observation.get(key) is not False: errors.append('UNITY_STATE_UNKNOWN_OR_BUSY:'+key)
    for key in ('inputReleased','saveOverrideClear'):
        if observation.get(key) is not True: errors.append('UNITY_STATE_UNKNOWN_OR_BUSY:'+key)
    if live.get(observation.get('editorPid'))!='unity.exe': errors.append('EXACT_EDITOR_REQUIRED')
    if any(x.get('status')=='in_flight' for x in env.get('productionDispatch',{}).get('ledger',[])):
        errors.append('AMBIGUOUS_OPERATION_NO_TRANSFER')
    for worker in env.get('workers',{}).values():
        if worker.get('status') in ('starting','running','dispatching'):
            pids=[worker.get(k) for k in ('pid','cliPid','supervisorPid') if type(worker.get(k)) is int]
            if not pids or any(pid in live for pid in pids): errors.append('PRIOR_WORKER_ACTIVE_OR_UNKNOWN')
    return errors + budget_errors(state, {'compileFix':1,'targetedPlay':1,'dedicatedValidators':0})

def transfer(request, run=False):
    state,pin=authority();observation=request.get('runtimeObservation',{})
    errors=transfer_errors(state,pin,request,observation,processes())
    # Reuse the same actual approval/proof checks with the old owner; do not require an exited lease to be active.
    check=dict(request,provider=pin['owner'],sessionId=state.get('productionEnvironment',{}).get('coordinatorLease',{}).get('sessionId'))
    errors += [e for e in authorization_errors(state,pin,check) if e!='SINGLE_WRITER_LEASE_NOT_ACTIVE']
    folder=ROOT/'Logs/AgentConsole'/('transfer-'+uuid.uuid4().hex)
    report={'status':'BLOCKED_PRESERVED' if errors else 'PREPARED_NOT_TRANSFERRED','errors':sorted(set(errors)),
            'observedAt':now(),'authority':pin,'nextAction':'After explicit 02, verify exact exited child and fresh Unity/input/save state.','gameCompletion':False}
    write(folder/'result.json',report)
    if errors or not run:return report
    target=request['targetProvider'];h=helper()
    with h.state_lock():
        current,current_pin=authority()
        if current_pin!=pin or transfer_errors(current,pin,request,observation,processes()):
            raise GuardError('State changed before writer transfer')
        config=ROOT/'.codex/config.toml';local=ROOT/'.claude/settings.local.json'
        original_config=config.read_text('utf-8-sig');local_data=read(local)
        # Preserve every other setting. The known Unity table must have exactly one enabled key.
        match=re.search(r'(?ms)^\[mcp_servers\.unityMCP\]\s*\n(.*?)(?=^\[|\Z)',original_config)
        if not match or len(re.findall(r'(?m)^enabled\s*=',match[1]))!=1:
            raise GuardError('Unity table needs a reviewed scoped configuration merge')
        replacement=re.sub(r'(?m)^enabled\s*=\s*(?:true|false)', 'enabled = '+str(target=='codex').lower(),match[1])
        new_config=original_config[:match.start(1)]+replacement+original_config[match.end(1):]
        names=local_data.get('enabledMcpjsonServers',[])
        local_data['enabledMcpjsonServers']=[n for n in names if n.lower()!='unitymcp']
        if target=='claude-code':local_data['enabledMcpjsonServers'].append('unityMCP')
        for path in (config,local,ROOT/STATE):
            backup=folder/'backup-before'/path.relative_to(ROOT);backup.parent.mkdir(parents=True,exist_ok=True)
            backup.write_bytes(path.read_bytes())
        temporary=config.with_name(config.name+'.'+uuid.uuid4().hex+'.tmp');temporary.write_text(new_config,'utf-8');os.replace(temporary,config)
        write(local,local_data)
        env=current['productionEnvironment'];env.setdefault('coordinatorLeaseHistory',[]).append(dict(env['coordinatorLease']))
        env['coordinatorLease']={'owner':target,'status':'ready_for_interactive_start','previousSessionId':request['previousSessionId'],
             'generation':uuid.uuid4().hex,'transferredAt':now(),'evidence':str(folder/'result.json')}
        env['unityEditorOwner']=target;current['demoProductionRouting']['unityEditorOwner']=target
        current[pin['approvalKey']]['unityEditorOwner']=target
        write(ROOT/STATE,current)
    report.update(status='TRANSFERRED_NOT_STARTED',nextAction='New owner establishes one real session/lease, rechecks effective MCP and dispatches finite operations; budgets unchanged.')
    write(folder/'result.json',report);return report

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--request',type=Path); p.add_argument('--transfer-request',type=Path); p.add_argument('--run',action='store_true'); p.add_argument('--status',action='store_true')
    args=p.parse_args()
    if args.status:
        state,pin=authority(); print(json.dumps({'authority':pin,'budgetPolicy':budget_policy(state),'budgetErrors':budget_errors(state),
            'resumeAuthorization':state.get('productionEnvironment',{}).get('productionDispatch',{}).get('resumeAuthorization',{'enabled':False}),
            'supportRunner':'provider_runner.py remains read-only; not a production failover',
            'nextAction':'User sends 02, then current owner reconciles current ticket and known cumulative usage.'},ensure_ascii=False,indent=2));return 0
    if not args.request and not args.transfer_request: p.error('--request, --transfer-request or --status required')
    if ROOT != Path('C:/Users/sdjsd/Desktop/Unity/Project_PA').resolve(): raise GuardError('Original root required')
    result=transfer(read(args.transfer_request),args.run) if args.transfer_request else dispatch(read(args.request),args.run)
    print(json.dumps(result,ensure_ascii=False,indent=2));return 2 if result['errors'] else 0

if __name__=='__main__':
    try: sys.exit(main())
    except (OSError,ValueError,subprocess.SubprocessError) as exc:
        print(json.dumps({'status':'BLOCKED_PRESERVED','reason':str(exc)[:300],'nextAction':'Inspect preserved run evidence; do not replay.'}),file=sys.stderr);sys.exit(2)
