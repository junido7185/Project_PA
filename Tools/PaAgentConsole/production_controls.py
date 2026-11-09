"""Explicit /pa controls over the existing owner helper; no background resume policy.

Snapshot is observational. Actions require a current UI pin and an explicit press.
Only the normal helper writes production state; this module never clears a pause,
checkpoint, approval or lease, and never sends Unity input or terminates a process.
"""
from __future__ import annotations

import ast
import datetime as dt
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import uuid

from production_adapter import authority, authorization_errors, budget_errors, helper, safe_relative

HELPER = 'Tools/LoopEngineering/Invoke-ProjectPAProduction.py'
PROMPT = 'Logs/ParallelProduction/Production-observer-codex-20261020/OWNER_R1_R4_REVIEW_REQUEST.md'
PIN_FIELDS = ('owner', 'sessionId', 'approvalKey', 'ticket', 'baselineHead', 'workspaceSha256',
              'handoffSha256', 'contractSha256', 'leaseGeneration')


def instant(value):
    try:
        parsed = dt.datetime.fromisoformat(value.replace('Z', '+00:00'))
        return parsed if parsed.tzinfo else None
    except (ValueError, TypeError, AttributeError):
        return None


def process_inventory(root):
    """CIM gives actual identity and creation time, never just a remembered PID."""
    if os.name != 'nt':
        return {'available': False, 'processes': [], 'reason': 'WINDOWS_PROCESS_IDENTITY_UNAVAILABLE'}
    script = ("$ErrorActionPreference='Stop';try {$r=@(Get-CimInstance Win32_Process | "
              "Where-Object {$_.Name -match '^(claude|python|pythonw|python312)\\.exe$'} | "
              "ForEach-Object {@{pid=$_.ProcessId;parentPid=$_.ParentProcessId;name=$_.Name;"
              "executable=$_.ExecutablePath;commandLine=$_.CommandLine;"
              "createdAt=$_.CreationDate.ToUniversalTime().ToString('o')}});"
              "@{available=$true;processes=$r}|ConvertTo-Json -Depth 4}"
              "catch {@{available=$false;processes=@();reason=$_.Exception.Message}|ConvertTo-Json}")
    try:
        result = subprocess.run([str(Path(os.environ.get('SystemRoot', 'C:/Windows')) /
                                    'System32/WindowsPowerShell/v1.0/powershell.exe'),
                                 '-NoProfile', '-NonInteractive', '-Command', script],
                                capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=15)
        data = json.loads(result.stdout)
        if result.returncode or not isinstance(data, dict) or not isinstance(data.get('processes'), list):
            raise ValueError('Process inventory did not return a complete object')
        return data
    except (OSError, ValueError, subprocess.TimeoutExpired) as error:
        return {'available': False, 'processes': [], 'reason': str(error)}


def process_status(root, lease, inventory):
    if inventory.get('available') is not True:
        return {'status': 'UNKNOWN', 'reasons': ['PROCESS_IDENTITY_UNAVAILABLE'], 'processes': []}
    sid, start = lease.get('sessionId'), instant(lease.get('startedAt'))
    stop = instant(lease.get('stoppedAt'))
    known = {lease.get('cliPid'), lease.get('launcherPid')} - {None}
    project = root.as_posix().lower()
    identified, reasons = [], []
    for process in inventory['processes']:
        if not isinstance(process, dict):
            reasons.append('MALFORMED_PROCESS_OBSERVATION'); continue
        command = str(process.get('commandLine') or '').replace('\\', '/').lower()
        name = str(process.get('name') or '').lower()
        created = instant(process.get('createdAt'))
        addresses = re.findall(r'--(?:resume-session|session-id|resume)(?:=|\s+)["\x27]?([0-9a-f-]{36})(?=["\x27\s]|$)', command)
        same = isinstance(sid, str) and sid.lower() in addresses
        lead = name == 'claude.exe' and 'projectpa claude production lead' in command
        entry = 'invoke-projectpaproduction.py' in command and bool(re.search(r'(?:^|\s)start(?:\s|$)', command))
        relevant = (same and (name == 'claude.exe' or entry)) or ((lead or entry) and project in command)
        if process.get('pid') in known:
            if (not relevant or not created or not start or created < start-dt.timedelta(seconds=5)
                    or stop is not None and created > stop+dt.timedelta(seconds=5)):
                reasons.append('REGISTERED_PID_IDENTITY_MISMATCH')
        if not relevant:
            continue  # An unrelated idle VS Code SID is not production.
        if not same or project not in command or not created or not process.get('executable'):
            reasons.append('AMBIGUOUS_OR_OTHER_PRODUCTION_PROCESS')
        else:
            identified.append({'pid': process.get('pid'), 'createdAt': process['createdAt'],
                               'kind': 'claude' if name == 'claude.exe' else 'existing_helper'})
    return {'status': 'UNKNOWN' if reasons else 'RUNNING' if identified else 'STOPPED',
            'reasons': sorted(set(reasons)), 'processes': identified}


def usage_status(env, sid, current):
    observed = env.get('autonomous', {}).get('modObservation') or {}
    at = instant(observed.get('observedAt'))
    fresh = observed.get('sessionId') == sid and at is not None and 0 <= (current-at).total_seconds() <= 120
    windows, blocks = [], []
    for row in observed.get('rateLimits', []) if isinstance(observed.get('rateLimits'), list) else []:
        if not isinstance(row, dict): continue
        reset = instant(row.get('resetsAt'))
        percent = row.get('percentUsed')
        valid = fresh and type(percent) in (int, float) and 0 <= percent <= 100 and reset is not None and reset > current
        known_exhausted = (observed.get('sessionId') == sid and at is not None and at <= current
                           and type(percent) in (int, float) and percent == 100 and reset is not None and reset > current)
        status = 'EXHAUSTED_UNTIL_CONFIRMED_RESET' if known_exhausted else 'OBSERVED' if valid else 'UNKNOWN'
        if status == 'EXHAUSTED_UNTIL_CONFIRMED_RESET': blocks.append('USAGE_RESET_NOT_DUE')
        windows.append({'kind': row.get('kind'), 'status': status, 'percentUsed': percent if valid or known_exhausted else None,
                        'resetAt': row.get('resetsAt') if valid or known_exhausted else None,
                        'recordedPercentUsed': percent, 'recordedResetAt': row.get('resetsAt')})
    return {'status': 'WAITING' if blocks else 'OBSERVED' if fresh and windows and all(w['status'] != 'UNKNOWN' for w in windows) else 'UNKNOWN',
            'observedAt': observed.get('observedAt') if observed.get('sessionId') == sid else None,
            'windows': windows, 'reasons': sorted(set(blocks)), 'unknownDoesNotMeanAvailable': True}


def control_snapshot(root, *, authority_reader=None, process_reader=None, clock=None):
    root = root.resolve()
    current = (clock or (lambda: dt.datetime.now(dt.timezone.utc)))()
    state, pin = (authority_reader or authority)(root)
    env = state.get('productionEnvironment', {})
    lease = env.get('coordinatorLease', {})
    auto = env.get('autonomous', {})
    sid = lease.get('sessionId')
    request_pin = dict(pin, sessionId=sid, leaseGeneration=lease.get('generation'))
    request_pin = {k: request_pin.get(k) for k in PIN_FIELDS}
    reasons = []
    try:
        if str(uuid.UUID(sid)) != sid: raise ValueError()
    except (ValueError, TypeError, AttributeError): reasons.append('ACTUAL_OWNER_SID_UNCONFIRMED')
    if pin.get('owner') != 'claude-code' or lease.get('owner') != 'claude-code': reasons.append('RECORDED_CLAUDE_OWNER_REQUIRED')
    errors = authorization_errors(state, pin, dict(pin, provider=pin.get('owner'), sessionId=sid), root)
    reasons += [e for e in errors if e != 'SINGLE_WRITER_LEASE_NOT_ACTIVE']
    reasons += budget_errors(state, costs={'compileFix': 0, 'targetedPlay': 0, 'dedicatedValidators': 0}, root=root)
    if env.get('productionDispatch', {}).get('resumeAuthorization', {}).get('identifier') != 'PA_START_CURRENT_CHECKPOINT_20261019_V3':
        reasons.append('CURRENT_V3_EVIDENCE_REQUIRED')
    if any(os.environ.get(key) for key in ('ANTHROPIC_API_KEY', 'ANTHROPIC_AUTH_TOKEN',
                                          'CLAUDE_CODE_USE_BEDROCK', 'CLAUDE_CODE_USE_VERTEX', 'CLAUDE_CODE_USE_FOUNDRY')):
        reasons.append('SUBSCRIPTION_ONLY_AUTH_REVIEW_REQUIRED')
    user_stop = (auto.get('stopRequested') is True
                 or auto.get('status') in ('pause_requested', 'paused_by_user', 'ready_for_human_review')
                 or (auto.get('checkpoint') or {}).get('disposition') == 'human-review')
    if user_stop: reasons.append('USER_STOP_OR_HUMAN_REVIEW_PRESERVED')
    if any(isinstance(row, dict) and row.get('status') == 'in_flight' for row in env.get('productionDispatch', {}).get('ledger', [])):
        reasons.append('IN_FLIGHT_OPERATION_UNRESOLVED')
    processes = process_status(root, lease, (process_reader or process_inventory)(root))
    reasons += processes['reasons']
    usage = usage_status(env, sid, current)
    reasons += usage['reasons']
    try:
        names = {n.name for n in ast.parse(safe_relative(root, HELPER).read_text('utf-8-sig')).body if isinstance(n, ast.FunctionDef)}
        supported = {'resume_due', 'schedule_local_resume'} <= names
        prompt = safe_relative(root, PROMPT)
        if not prompt.is_file(): reasons.append('EXISTING_SUPPORT_PROMPT_MISSING')
    except (OSError, ValueError, SyntaxError):
        supported = False
        reasons.append('EXISTING_HELPER_OR_PROMPT_UNAVAILABLE')
    reservation = auto.get('resumeReservation') or {}
    reservation_valid = (reservation.get('sessionId') == sid and reservation.get('generation') == lease.get('generation')
                         and instant(reservation.get('resetAt')) is not None and instant(reservation.get('resumeAt')) is not None
                         and not any(reservation.get(k) for k in ('cancelledAt', 'waitCompletedAt', 'dispatchedAt')))
    automatic_reasons = list(reasons)
    if auto.get('enabled') is not True: automatic_reasons.append('AUTONOMOUS_NOT_ENABLED_PRESERVED')
    if not reservation_valid: automatic_reasons.append('NO_VALID_PENDING_RESERVATION')
    due = bool(reservation_valid and instant(reservation['resumeAt']) <= current)
    if reservation_valid and not due: automatic_reasons.append('RESERVATION_NOT_DUE')
    if not supported: automatic_reasons.append('NORMAL_RESUME_DUE_UNAVAILABLE')
    if processes['status'] != 'STOPPED': automatic_reasons.append('PRODUCTION_NOT_CONFIRMED_STOPPED')
    return {'observedAt': current.isoformat(), 'requestPin': request_pin, 'owner': pin.get('owner'), 'sessionId': sid,
            'production': processes, 'usage': usage,
            'resume': {'allowed': not reasons and processes['status'] == 'STOPPED', 'reasons': sorted(set(reasons)),
                       'alreadyRunning': processes['status'] == 'RUNNING', 'mode': 'existing_same_sid_headless'},
            'safeStop': {'allowed': pin.get('owner') == lease.get('owner') == 'claude-code'
                                   and 'ACTUAL_OWNER_SID_UNCONFIRMED' not in reasons and processes['status'] != 'UNKNOWN',
                         'reasons': [e for e in reasons if e in ('ACTUAL_OWNER_SID_UNCONFIRMED', 'RECORDED_CLAUDE_OWNER_REQUIRED')]
                                    + processes['reasons'],
                         'alreadyRequested': auto.get('stopRequested') is True, 'mode': 'existing_autonomy_pause_only'},
            'automaticResume': {'supported': supported, 'enabled': auto.get('enabled') is True,
                                'eligible': not automatic_reasons, 'due': due, 'reasons': sorted(set(automatic_reasons)),
                                'reservation': reservation if reservation_valid else None,
                                'actualSchedule': 'UNVERIFIED_NOT_QUERIED'},
            'preservedCheckpoint': auto.get('checkpoint'), 'recordedAutonomousStatus': auto.get('status'),
            'disabledAutonomyIsNotAutomaticallyUserPause': auto.get('enabled') is False and not user_stop,
            'gameCompletion': False}


def control_action(root, payload, *, authority_reader=None, process_reader=None, clock=None,
                   helper_loader=None, spawn=None, run=None):
    """Explicit UI call only. No auto-enable, quota probe, retry, transfer, or kill."""
    root = root.resolve()
    if isinstance(payload, str): payload = json.loads(payload)
    if not isinstance(payload, dict) or payload.get('action') not in ('resume', 'safe-stop'):
        raise ValueError('Explicit resume or safe-stop button action required')
    expected = payload.get('expected')
    if not isinstance(expected, dict) or any(k not in expected for k in PIN_FIELDS):
        raise ValueError('Current control snapshot requestPin required')
    h = (helper_loader or helper)(root)
    with h.state_lock():
        view = control_snapshot(root, authority_reader=authority_reader, process_reader=process_reader, clock=clock)
        if any(expected[k] != view['requestPin'][k] for k in PIN_FIELDS):
            return {'status': 'BLOCKED', 'reasons': ['STALE_CONTROL_PIN'], 'snapshot': view}
        action = payload['action']
        if action == 'resume' and view['resume']['alreadyRunning']:
            return {'status': 'NOOP_ALREADY_RUNNING', 'sessionId': view['sessionId'], 'snapshot': view}
        if action == 'safe-stop' and view['safeStop']['alreadyRequested']:
            return {'status': 'NOOP_SAFE_STOP_ALREADY_REQUESTED', 'snapshot': view}
        gate = view['resume'] if action == 'resume' else view['safeStop']
        if not gate['allowed']:
            return {'status': 'BLOCKED', 'reasons': gate.get('reasons', ['OWNER_OR_PROCESS_UNCONFIRMED']), 'snapshot': view}
        command = [sys.executable, '-B', '-X', 'utf8', str(safe_relative(root, HELPER))]
        if action == 'resume':
            command += ['start', '--owner', 'claude-code', '--headless',
                        '--resume-session', view['sessionId'], '--effort', 'high', '--prompt-file', PROMPT]
            # Holding the original lock through CreateProcess serializes UI presses. Once released,
            # the helper claims its normal lease; another press must see this actual helper process.
            log = safe_relative(root, 'Logs/AgentConsole/control-resume.log')
            log.parent.mkdir(parents=True, exist_ok=True)
            with log.open('a', encoding='utf-8') as stream:
                child_env = dict(os.environ)
                native_bin = Path.home() / '.local/bin'
                if not shutil.which('claude') and (native_bin / 'claude.exe').is_file():
                    child_env['PATH'] = str(native_bin) + os.pathsep + child_env.get('PATH', '')
                process = (spawn or subprocess.Popen)(command, cwd=root, stdout=stream, stderr=subprocess.STDOUT,
                    creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0), stdin=subprocess.DEVNULL, env=child_env)
            return {'status': 'START_REQUESTED_UNCONFIRMED', 'sessionId': view['sessionId'], 'pid': process.pid,
                    'log': log.relative_to(root).as_posix(), 'actualStartConfirmed': False, 'snapshot': view}
    # The helper takes the same lock itself. Safe stop requests a boundary and cancels normal resume;
    # it never terminates a child, and an accepted request is not proof that the child has stopped.
    command += ['autonomy', 'pause', '--session-id', view['sessionId']]
    try:
        result = (run or subprocess.run)(command, cwd=root, capture_output=True, text=True)
        return {'status': 'SAFE_STOP_REQUESTED' if result.returncode == 0 else 'HELPER_REJECTED',
                'sessionId': view['sessionId'], 'exitCode': result.returncode,
                'stdout': result.stdout[-12000:], 'stderr': result.stderr[-2000:], 'actualStopConfirmed': False}
    except subprocess.TimeoutExpired:
        return {'status': 'STOP_REQUEST_UNKNOWN', 'sessionId': view['sessionId'],
                'reasons': ['HELPER_RESPONSE_TIMEOUT_NO_REPLAY'], 'actualStopConfirmed': False}
