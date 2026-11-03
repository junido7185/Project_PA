"""Explicit Mod submissions in the existing relay registry; no dispatch or model calls.

Messages are peer instructions, never approval or a permission-prompt answer.
The existing production helper supplies the shared state lock and atomic writes.
"""
from __future__ import annotations

import datetime as dt
import hashlib
import json
from pathlib import Path
import re
import uuid

from production_adapter import helper as production_helper
from production_observation import inside

STATE = Path('Automation/LoopEngineering/State/loop-state.json')
MESSAGE_DIR = Path('Logs/AgentConsole/messages')
ID = re.compile(r'^[0-9]{10,17}-[A-Za-z0-9_-]{4,64}$')
MAX_TEXT = 16000
MAX_RUN_BYTES = 32_000_000
KST = dt.timezone(dt.timedelta(hours=9))


def now():
    return dt.datetime.now(KST).isoformat()


def sha(text):
    return hashlib.sha256(text.encode('utf-8')).hexdigest()


def timestamp(value):
    try:
        parsed = dt.datetime.fromisoformat(value.replace('Z', '+00:00'))
        return parsed if parsed.tzinfo is not None else None
    except (AttributeError, ValueError, TypeError):
        return None


def object_arg(value):
    value = json.loads(value) if isinstance(value, str) else value
    if not isinstance(value, dict):
        raise ValueError('Instruction argument must be a JSON object')
    return value


def instruction_id(value):
    if not isinstance(value, str) or not ID.fullmatch(value):
        raise ValueError('Instruction id must be timestamp-random using safe ASCII characters')
    return value


def message_path(root, ident):
    path = root / MESSAGE_DIR / (instruction_id(ident) + '.json')
    inside(root, str(path))  # Resolve links before containment, including a linked messages directory.
    return path


def rows(state):
    result = state.get('productionEnvironment', {}).get('relaySupport', {}).get('userInstructions', [])
    if not isinstance(result, list):
        raise ValueError('Existing relaySupport.userInstructions is not a list; preserved')
    return result


def load_message(root, row):
    expected = (MESSAGE_DIR / (instruction_id(row.get('id')) + '.json')).as_posix()
    if row.get('path') != expected:
        raise ValueError('Instruction evidence path does not match its registered id')
    path = message_path(root, row['id'])
    if path.stat().st_size > 256_000:
        raise ValueError('Instruction evidence exceeds bounded read size; preserved for review')
    data = json.loads(path.read_text('utf-8-sig'))
    for key in ('id', 'path', 'targetSessionId', 'targetOwner', 'sourceKind', 'explicitUserSubmit',
                'sourceSessionId', 'sourceVersion', 'createdAt', 'textSha256', 'envelopeSha256'):
        if data.get(key) != row.get(key):
            raise ValueError('Instruction evidence/registry mismatch: ' + key)
    if not isinstance(data.get('text'), str) or sha(data['text']) != row.get('textSha256'):
        raise ValueError('Instruction text hash mismatch; no send or receipt inferred')
    if not isinstance(data.get('envelope'), str) or sha(data['envelope']) != row.get('envelopeSha256'):
        raise ValueError('Instruction envelope hash mismatch; no send inferred')
    if row.get('sourceKind') != 'pa_user_input' or row.get('explicitUserSubmit') is not True:
        raise ValueError('Instruction is not registered as an explicit Mod submission')
    return data


def merged_delivery(row, data):
    """A saved dispatch result outranks an older state write if the second write failed."""
    candidates = [x for x in (data.get('delivery'), row.get('delivery')) if isinstance(x, dict)]
    queued = [x for x in candidates if x.get('status') == 'QUEUED']
    if queued:
        if any(x.get('status') == 'NOT_DELIVERED' for x in candidates):
            return {'status': 'UNKNOWN', 'unknown': True, 'error': 'Conflicting preserved delivery records'}
        # A confirmed return outranks the pre-send UNKNOWN when only the first write completed.
        return queued[0]
    if candidates and candidates[0].get('status') == 'NOT_DELIVERED':
        return candidates[0]
    unknown = [x for x in candidates if x.get('status') == 'UNKNOWN']
    if unknown:
        return unknown[0]
    return next((x for x in candidates if x.get('status') == 'NOT_DELIVERED'),
                {'status': 'NOT_ATTEMPTED'})


def run_at_submit(root, state, target):
    """Capture a bounded append boundary for stream-json events without timestamps."""
    run = state.get('productionEnvironment', {}).get('coordinator', {}).get('runDirectory')
    try:
        path = inside(root, run) / 'claude.jsonl'
        if path.stat().st_size > MAX_RUN_BYTES:
            return None
        raw = path.read_bytes()
        initial = next((json.loads(line) for line in raw.splitlines()[:12]
                        if line.lstrip().startswith(b'{') and json.loads(line).get('subtype') == 'init'), {})
        if initial.get('session_id') != target:
            return None
        return {'path': path.relative_to(root.resolve()).as_posix(), 'bytes': len(raw),
                'sha256': hashlib.sha256(raw).hexdigest()}
    except (OSError, ValueError, TypeError, AttributeError):
        return None


def target_hold(state, row):
    lease = state.get('productionEnvironment', {}).get('coordinatorLease', {})
    owner = state.get('demoProductionRouting', {}).get('unityEditorOwner')
    if owner != row.get('targetOwner') or lease.get('owner') != owner or lease.get('sessionId') != row.get('targetSessionId'):
        return {'reason': 'TARGET_CHANGED', 'currentOwner': owner, 'currentSessionId': lease.get('sessionId'),
                'note': 'Saved target preserved; no send or silent retargeting'}
    return None


def response(row, data, state, *, reused=False):
    delivery = merged_delivery(row, data)
    hold = target_hold(state, row)
    return {key: data[key] for key in ('id', 'targetSessionId', 'targetOwner', 'path', 'text', 'envelope')} | {
        'saved': True, 'reused': reused, 'delivery': delivery,
        'canDeliver': hold is None and delivery.get('status') == 'NOT_ATTEMPTED', 'targetHold': hold}


def submit_instruction(root, payload):
    """Save once after a real Input submit. Never starts a CLI, model or send."""
    root = root.resolve()
    payload = object_arg(payload)
    ident = instruction_id(payload.get('id'))
    text = payload.get('text')
    if not isinstance(text, str) or not text.strip() or len(text) > MAX_TEXT or '\x00' in text:
        raise ValueError('Instruction text must be nonempty, at most 16000 characters, without NUL')
    source = payload.get('sourceSessionId')
    try:
        if str(uuid.UUID(source)) != source:
            raise ValueError()
    except (ValueError, AttributeError, TypeError):
        raise ValueError('sourceSessionId must be the actual observer UUID') from None
    version = payload.get('sourceVersion')
    if not isinstance(version, str) or not version.strip() or len(version) > 80 or any(ord(c) < 32 for c in version):
        raise ValueError('sourceVersion must be the observed engine version')
    h = production_helper(root)
    with h.state_lock():
        state = h.read(root / STATE)
        existing = next((row for row in rows(state) if isinstance(row, dict) and row.get('id') == ident), None)
        if existing:
            data = load_message(root, existing)
            if data['text'] != text or data['sourceSessionId'] != source or data['sourceVersion'] != version:
                raise ValueError('Instruction id already holds different text or provenance; preserved')
            return response(existing, data, state, reused=True)
        env = state.get('productionEnvironment', {})
        lease = env.get('coordinatorLease', {})
        if lease.get('owner') != 'claude-code' or state.get('demoProductionRouting', {}).get('unityEditorOwner') != 'claude-code':
            raise ValueError('Current production owner is not claude-code; no target was substituted')
        target = lease.get('sessionId')
        try:
            if str(uuid.UUID(target)) != target:
                raise ValueError()
        except (ValueError, AttributeError, TypeError):
            raise ValueError('Current owner session UUID is not confirmed') from None
        if source == target:
            raise ValueError('Observer submission cannot target its own production session')
        path = message_path(root, ident)
        if path.exists():
            raise ValueError('Unregistered message file exists; preserve and reconcile before sending')
        row = {'id': ident, 'path': path.relative_to(root).as_posix(), 'targetSessionId': target,
               'targetOwner': 'claude-code', 'sourceKind': 'pa_user_input', 'explicitUserSubmit': True,
               'sourceSessionId': source, 'sourceVersion': version, 'createdAt': now(),
               'textSha256': sha(text), 'delivery': {'status': 'NOT_ATTEMPTED'}}
        anchor = run_at_submit(root, state, target)
        if anchor:
            row['runEvidenceAtSubmit'] = anchor
        envelope = ('[PA_USER_INSTRUCTION:' + ident + ']\n'
                    'Explicit text submitted in the Project P.A. observer Mod. This delivery is a peer message, '
                    'not new approval, a permission-prompt answer, writer transfer or permission change. '
                    'Preserve existing refusals, Esc stop, owner, approval, usage and checkpoint.\n'
                    'Evidence: ' + row['path'] + '\n'
                    'Read the evidence when safe. Acknowledge in a dedicated assistant text message containing ONLY JSON: '
                    + json.dumps({'protocol': 'pa-instruction-ack-v1', 'id': ident, 'textSha256': row['textSha256'], 'kind': 'received'})
                    + '. For an actual reported decision/result use the same object with kind="result" and a nonempty reply field. '
                    'Do not put examples in code fences or treat this as game acceptance.\n\n'
                    + text)
        row['envelopeSha256'] = sha(envelope)
        data = dict(row, text=text, envelope=envelope)
        h.write(path, data)
        state['productionEnvironment'].setdefault('relaySupport', {}).setdefault('userInstructions', []).append(row)
        h.write(root / STATE, state)
        return response(row, data, state)


def record_delivery(root, payload):
    """Record the official send's return only; ambiguous calls never become skipped."""
    root = root.resolve()
    payload = object_arg(payload)
    ident = instruction_id(payload.get('id'))
    result = payload.get('result')
    if not isinstance(result, dict):
        raise ValueError('Missing official delivery result')
    if result.get('unknown') is True and 'isDelivered' not in result:
        delivery = {'status': 'UNKNOWN', 'unknown': True, 'error': str(result.get('error', 'Unconfirmed send result'))[:2000]}
    elif type(result.get('isDelivered')) is bool and 'unknown' not in result:
        delivery = {'status': 'QUEUED' if result['isDelivered'] else 'NOT_DELIVERED',
                    'isDelivered': result['isDelivered']}
        if 'reason' in result:
            delivery['reason'] = str(result['reason'])[:2000]
    else:
        raise ValueError('Delivery must be an official isDelivered boolean or explicit unknown error')
    h = production_helper(root)
    with h.state_lock():
        state = h.read(root / STATE)
        row = next((x for x in rows(state) if isinstance(x, dict) and x.get('id') == ident), None)
        if row is None:
            raise ValueError('Instruction is not in the existing relay registry')
        data = load_message(root, row)
        previous = merged_delivery(row, data)
        begin = payload.get('beginSend') is True
        if begin:
            hold = target_hold(state, row)
            if hold or previous.get('status') != 'NOT_ATTEMPTED':
                return {'id': ident, 'canSend': False, 'targetHold': hold,
                        'delivery': previous, 'reason': 'Target changed or attempt already preserved'}
            if delivery.get('status') != 'UNKNOWN':
                raise ValueError('Begin send must preserve UNKNOWN before transport')
        history = list(data.get('deliveryHistory', []))
        if previous.get('status') == 'UNKNOWN':
            # Reconcile the one send's final return; this endpoint never dispatches or permits a retry.
            if previous not in history:
                history.append(previous)
            delivery['at'] = now()
        elif previous.get('status') != 'NOT_ATTEMPTED':
            comparison = {k: v for k, v in previous.items() if k != 'at'}
            if delivery.get('status') == 'UNKNOWN':
                # A later diagnostic failure must not erase the already confirmed send return.
                note = dict(delivery, at=now())
                if note not in history:
                    history.append(note)
                delivery = previous
            elif comparison != delivery:
                raise ValueError('A delivery attempt is already preserved; do not resend or overwrite it')
            else:
                delivery = previous
        else:
            delivery['at'] = now()
        # Write dispatch proof first. A later state failure cannot make duplicate submit send again.
        data['delivery'] = delivery
        data['deliveryHistory'] = history
        h.write(message_path(root, ident), data)
        row['delivery'] = delivery
        row['deliveryHistory'] = history
        h.write(root / STATE, state)
        return {'id': ident, 'targetSessionId': row['targetSessionId'], 'path': row['path'],
                'status': 'DELIVERED' if delivery.get('status') == 'QUEUED' else 'SAVED',
                'delivery': delivery, 'canDeliver': False, 'canSend': begin,
                'targetSessionId': row['targetSessionId'], 'queueOnly': delivery.get('status') == 'QUEUED'}


def owner_ack(body, row):
    """Dedicated structured declaration only; quoted/fenced/example markers are not acknowledgements."""
    try:
        value = json.loads(body.strip())
    except (ValueError, AttributeError):
        return None
    if not isinstance(value, dict) or set(value) not in (
            {'protocol', 'id', 'textSha256', 'kind'}, {'protocol', 'id', 'textSha256', 'kind', 'reply'}):
        return None
    if (value.get('protocol') != 'pa-instruction-ack-v1' or value.get('id') != row['id']
            or value.get('textSha256') != row['textSha256'] or value.get('kind') not in ('received', 'result')):
        return None
    if value['kind'] == 'result' and (not isinstance(value.get('reply'), str) or not value['reply'].strip()):
        return None
    return value


def full_body_returned(content, data):
    if isinstance(content, list):
        content = '\n'.join(x.get('text', '') for x in content if isinstance(x, dict) and x.get('type') == 'text')
    if not isinstance(content, str):
        return False
    # Read commonly prefixes JSON lines with line numbers. A partial first line cannot parse.
    body = re.sub(r'(?m)^\s*\d+→', '', content.split('<system-reminder>')[0]).strip()
    try:
        read_value = json.loads(body)
    except ValueError:
        return False
    return isinstance(read_value, dict) and all(read_value.get(k) == data.get(k)
        for k in ('id', 'targetSessionId', 'text', 'textSha256'))


def selected_events(root, state):
    env = state.get('productionEnvironment', {})
    lease = env.get('coordinatorLease', {})
    try:
        path = inside(root, env.get('coordinator', {}).get('runDirectory')) / 'claude.jsonl'
        if path.stat().st_size > MAX_RUN_BYTES:
            return [], None, None
        raw = path.read_bytes()
    except (OSError, ValueError, TypeError):
        return [], None, None
    events, cursor, session = [], 0, None
    for number, line in enumerate(raw.splitlines(keepends=True), 1):
        start = cursor
        cursor += len(line)
        try:
            event = json.loads(line)
        except (ValueError, UnicodeError):
            continue
        if not isinstance(event, dict):
            continue
        if event.get('type') == 'system' and event.get('subtype') == 'init':
            session = event.get('session_id')
        events.append((event, {'source': path.relative_to(root.resolve()).as_posix(), 'line': number,
                               'eventSha256': hashlib.sha256(line).hexdigest(), '_start': start}))
    if not session or session != lease.get('sessionId'):
        return [], None, None
    return events, raw, timestamp(lease.get('startedAt'))


def instruction_observations(root, state):
    """Derive stages from this owner's selected run only; polling never writes."""
    root = root.resolve()
    events, raw, started = selected_events(root, state)
    target = state.get('productionEnvironment', {}).get('coordinatorLease', {}).get('sessionId')
    output = []
    recent = sorted((x for x in rows(state) if isinstance(x, dict)),
                    key=lambda x: str(x.get('createdAt', '')), reverse=True)[:20]
    for row in recent:
        view = {key: row.get(key) for key in ('id', 'path', 'createdAt', 'targetSessionId', 'targetOwner',
                                               'sourceSessionId', 'sourceVersion')}
        view.update(status='SAVED', startupAssignment=None, fileLookup=None, bodyRead=None, receipt=None, result=None, queueOnly=False, canDeliver=False)
        try:
            data = load_message(root, row)
        except (OSError, ValueError, TypeError, AttributeError) as error:
            view.update(evidenceError=str(error), delivery={'status': 'UNKNOWN'}, text='확인 못 함')
            output.append(view)
            continue
        delivery = merged_delivery(row, data)
        view.update(text=data['text'], delivery=delivery, queueOnly=delivery.get('status') == 'QUEUED')
        if delivery.get('status') == 'QUEUED':
            view['status'] = 'DELIVERED'
        created = timestamp(row.get('createdAt'))
        anchor = row.get('runEvidenceAtSubmit', {})
        anchor_valid = bool(raw is not None and isinstance(anchor, dict) and type(anchor.get('bytes')) is int
                            and 0 <= anchor['bytes'] <= len(raw)
                            and hashlib.sha256(raw[:anchor['bytes']]).hexdigest() == anchor.get('sha256'))
        if not events or target != row.get('targetSessionId') or created is None:
            output.append(view)
            continue
        # This is the existing helper's frozen prompt, not a second inbox or receipt.
        # Require the registered run/init SID and a later normal start, not arbitrary text.
        if started and started >= created:
            try:
                assignment = inside(root, state['productionEnvironment']['coordinator']['runDirectory']) / 'assignment.md'
                if assignment.stat().st_size <= 512_000:
                    body = assignment.read_text('utf-8-sig')
                    marker = '[PA_STARTUP_INSTRUCTION:' + row['id'] + ']'
                    if marker in body and data['envelope'] in body:
                        view['startupAssignment'] = {'source': assignment.relative_to(root).as_posix(),
                            'sha256': hashlib.sha256(assignment.read_bytes()).hexdigest(),
                            'kind': 'body_in_registered_startup_assignment', 'notReceiptProof': True}
                        view['status'] = 'STARTUP_ASSIGNED'
            except (OSError, ValueError, KeyError):
                pass
        reads = {}
        for event, cite in events:
            if event.get('session_id', target) != target:
                continue
            when = timestamp(event.get('timestamp'))
            anchor_match = anchor_valid and cite['source'] == anchor.get('path') and cite['_start'] >= anchor['bytes']
            eligible = when >= created if when else bool(anchor_match or (started and started >= created))
            if not eligible:
                continue
            evidence = {k: v for k, v in cite.items() if k != '_start'}
            evidence.update(at=when.isoformat() if when else None,
                            timeBasis='event_timestamp' if when else 'append_boundary' if anchor_match else 'run_started_after_submission')
            content = event.get('message', {}).get('content', []) if isinstance(event.get('message'), dict) else []
            if not isinstance(content, list):
                continue
            for block in content:
                if not isinstance(block, dict):
                    continue
                if event.get('type') == 'assistant' and block.get('type') == 'tool_use' and block.get('name') == 'Read':
                    inp = block.get('input', {})
                    try:
                        requested = inside(root, inp.get('file_path'))
                        if requested == message_path(root, row['id']):
                            if isinstance(block.get('id'), str) and block['id']:
                                reads[block['id']] = evidence
                    except (ValueError, OSError, TypeError, AttributeError):
                        pass
                elif block.get('type') == 'tool_result' and block.get('tool_use_id') in reads:
                    if block.get('is_error') is False or 'is_error' not in block:
                        view['fileLookup'] = dict(reads[block['tool_use_id']], kind='successful_file_lookup', result=evidence)
                        if full_body_returned(block.get('content'), data):
                            view['bodyRead'] = dict(reads[block['tool_use_id']], kind='full_body_returned', result=evidence, notUnderstandingProof=True)
                elif event.get('type') == 'assistant' and block.get('type') == 'text':
                    body = block.get('text', '')
                    if not isinstance(body, str):
                        continue
                    ack = owner_ack(body, row)
                    if ack:
                        view['receipt'] = dict(evidence, kind='explicit_owner_acknowledgement', confirmed=True)
                        if ack['kind'] == 'result':
                            view['result'] = dict(evidence, kind='owner_reported_result',
                                                  text=ack['reply'][:3000], notGameAcceptance=True)
        if view['result']:
            view['status'] = 'RESULT'
        elif view['receipt']:
            view['status'] = 'RECEIVED'
        elif view['bodyRead']:
            view['status'] = 'BODY_READ'
        elif view['fileLookup']:
            view['status'] = 'FILE_LOOKUP'
        output.append(view)
    return output
