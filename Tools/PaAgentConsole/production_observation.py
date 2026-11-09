"""Derived read-only view of the existing production registry and actual run events.

No model/Unity calls, writes, inferred agents, subscription balances or completion %.
Every event link points into the existing run, never a second production state.
"""
from __future__ import annotations
import ctypes
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import re

KST = dt.timezone(dt.timedelta(hours=9))
STATE = 'Automation/LoopEngineering/State/loop-state.json'
OPEN_SUFFIXES = {'.png', '.jpg', '.jpeg', '.webp', '.mp4', '.webm', '.txt', '.md', '.json', '.jsonl', '.csv', '.log', '.patch', '.diff'}


def stamp():
    return dt.datetime.now(KST).isoformat(timespec='seconds')


def text(value, limit=700):
    return re.sub(r'[\x00-\x1f\x7f-\x9f]', ' ', str(value if value is not None else '확인 못 함'))[:limit]


def local_time(value):
    try:
        return dt.datetime.fromisoformat(value.replace('Z', '+00:00')).astimezone(KST).isoformat(timespec='seconds')
    except (ValueError, TypeError, AttributeError):
        return None


def inside(root, value):
    if not isinstance(value, str) or not value:
        raise ValueError('Missing project path')
    # Shell snippets and URLs are never file paths. Resolve symlinks before containment.
    p = Path(value.replace('\\', '/'))
    p = (root / p).resolve() if not p.is_absolute() else p.resolve()
    p.relative_to(root.resolve())
    return p


def relative(root, value):
    try:
        return inside(root, value).relative_to(root.resolve()).as_posix()
    except (ValueError, OSError):
        return None


def load(path, default=None):
    try:
        return json.loads(path.read_text('utf-8-sig'))
    except (OSError, ValueError):
        return {} if default is None else default


def process_observation(pid, not_before=None):
    """Inspect only registered PIDs. Creation time protects against PID reuse."""
    result = {'pid': pid, 'status': 'UNKNOWN', 'observedAt': stamp()}
    if type(pid) is not int or pid <= 0 or os.name != 'nt':
        return result
    from ctypes import wintypes
    k = ctypes.WinDLL('kernel32', use_last_error=True)
    k.OpenProcess.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
    k.OpenProcess.restype = wintypes.HANDLE
    k.GetProcessTimes.argtypes = [wintypes.HANDLE] + [ctypes.POINTER(wintypes.FILETIME)] * 4
    k.CloseHandle.argtypes = [wintypes.HANDLE]
    k.GetExitCodeProcess.argtypes = [wintypes.HANDLE, ctypes.POINTER(wintypes.DWORD)]
    handle = k.OpenProcess(0x1000, False, pid)
    if not handle:
        result['status'] = 'EXITED' if ctypes.get_last_error() == 87 else 'UNKNOWN'
        return result
    try:
        code = wintypes.DWORD()
        if not k.GetExitCodeProcess(handle, ctypes.byref(code)):
            return result
        result['status'] = 'ALIVE' if code.value == 259 else 'EXITED'
        created, exited, kernel, user = [wintypes.FILETIME() for _ in range(4)]
        if k.GetProcessTimes(handle, *[ctypes.byref(x) for x in (created, exited, kernel, user)]):
            seconds = ((created.dwHighDateTime << 32) | created.dwLowDateTime) / 10_000_000 - 11644473600
            when = dt.datetime.fromtimestamp(seconds, KST)
            result['createdAt'] = when.isoformat(timespec='seconds')
            start = local_time(not_before)
            if start and not (-5 <= (when-dt.datetime.fromisoformat(start)).total_seconds() <= 60):
                result['status'] = 'PID_IDENTITY_MISMATCH'
    finally:
        k.CloseHandle(handle)
    return result


def run_events(root, run, max_bytes=32_000_000):
    """Read only the selected run. Partial final JSON and unknown shapes are ignored."""
    path = run / 'claude.jsonl'
    tools, results, plugins, errors = [], {}, [], []
    session_id = None
    last = None
    try:
        if path.stat().st_size > max_bytes:
            return {'tools': [], 'plugins': [], 'lastAt': None, 'warnings': ['선택 run이 읽기 한도 초과: 이벤트 확인 못 함']}
        with path.open(encoding='utf-8-sig') as stream:
            for line_no, line in enumerate(stream, 1):
                try:
                    event = json.loads(line)
                except ValueError:
                    continue
                if not isinstance(event, dict):
                    continue
                if event.get('type') == 'system' and event.get('subtype') == 'init':
                    session_id = event.get('session_id')
                    plugins = [p.get('name') for p in event.get('plugins', []) if isinstance(p, dict)]
                message = event.get('message', {})
                content = message.get('content', []) if isinstance(message, dict) else []
                if not isinstance(content, list):
                    continue
                for block in content:
                    if not isinstance(block, dict):
                        continue
                    if block.get('type') == 'tool_use':
                        at = local_time(event.get('timestamp'))
                        if at and (not last or at > last):
                            last = at
                        inp = block.get('input', {})
                        if not isinstance(inp, dict):
                            inp = {}
                        tools.append({'id': block.get('id'), 'name': block.get('name'), 'input': inp,
                                      'at': at, 'line': line_no, 'eventSha256': hashlib.sha256(line.encode()).hexdigest(),
                                      'source': relative(root, str(path))})
                    elif block.get('type') == 'tool_result':
                        results[block.get('tool_use_id')] = {'error': block.get('is_error') is True,
                                                           'body': block.get('content'), 'line': line_no}
        for tool in tools:
            answer = results.get(tool['id'])
            tool['outcome'] = 'FAILED' if answer and answer['error'] else 'RETURNED' if answer else 'UNCONFIRMED'
            tool['result'] = answer
            if answer and answer['error']:
                errors.append({'tool': tool['name'], 'at': tool['at'], 'source': tool['source'], 'line': tool['line']})
    except OSError:
        return {'tools': [], 'plugins': [], 'lastAt': None, 'warnings': ['선택 run 이벤트를 읽을 수 없음']}
    return {'tools': tools, 'plugins': plugins, 'lastAt': last, 'warnings': [], 'errors': errors, 'sessionId': session_id}


def refs(root, value):
    """Only existing, project-local evidence/manual/tool paths; no arbitrary commands."""
    value = str(value).replace('\\\\', '/').replace('\\', '/')
    found = []
    pattern = r'(?:Logs|Docs|AI_WORKFLOW|Tools|References|Assets|\.agents)/[^\s\x00-\x1f\x22\x27`<>|;,()]+?\.(?:png|jpg|webp|mp4|webm|md|txt|json|csv|py|shader|cs|patch|diff)(?=$|[\s\x22\x27`<>|;,():\]])'
    for match in re.finditer(pattern, value):
        rel = relative(root, match[0])
        if rel and (root / rel).is_file() and rel not in found:
            found.append(rel)
    return found


def citation(tool):
    return {'source': tool['source'], 'line': tool['line'], 'at': tool['at'], 'eventSha256': tool['eventSha256']}


def artifact(root, path, role='evidence', source=None):
    rel = relative(root, path)
    if not rel or not (root / rel).is_file():
        return None
    suffix = (root / rel).suffix.lower()
    return {'path': rel, 'kind': 'image' if suffix in {'.png', '.jpg', '.jpeg', '.webp'} else 'video' if suffix in {'.mp4', '.webm'} else 'document',
            'role': role, 'source': source, 'openable': suffix in OPEN_SUFFIXES}


def owner_method_use_provenance(root, env, use, adoptions, report, report_text, source, line, event_cache):
    """A matching hash is not owner attribution; require a registered run and successful report writer."""
    sid = use.get('sessionId')
    matches = [a for a in adoptions if a.get('afterSha256') == use.get('manualSha256')
               and a.get('owner') == 'claude-code' and a.get('sessionId') == sid and sid]
    if not matches: return {'ownerAttributionVerified':False,'sourceWarnings':['채택 owner SID와 보고 SID의 일치 확인 못 함']}
    coordinator = env.get('coordinator', {})
    records = [coordinator, env.get('previousCoordinator', {})]
    records += coordinator.get('priorRuns') or []
    records += list(env.get('workers', {}).values())
    registered = None
    for record in records:
        if not isinstance(record, dict) or record.get('sessionId') != sid: continue
        if (record.get('owner') or record.get('provider') or coordinator.get('owner')) != 'claude-code': continue
        try: folder = inside(root, record.get('runDirectory') or record.get('logDirectory'))
        except (ValueError, OSError): continue
        if source.name == 'claude.jsonl' and source.parent == folder:
            registered = folder; break
    if registered is None:
        return {'ownerAttributionVerified':False,'sourceWarnings':['등록된 제작 run 경로·owner·SID 확인 못 함']}
    key = str(source)
    if key not in event_cache: event_cache[key] = run_events(root, registered)
    events = event_cache[key]
    if events.get('sessionId') != sid:
        return {'ownerAttributionVerified':False,'sourceWarnings':['등록 run의 실제 init SID가 보고 SID와 일치하지 않음']}
    try: event = json.loads(line)
    except (ValueError, TypeError): event = {}
    if (not isinstance(event, dict) or event.get('type') != 'assistant'
            or event.get('session_id', sid) != sid):
        return {'ownerAttributionVerified':False,'sourceWarnings':['owner assistant의 실제 보고 생성 이벤트 확인 못 함']}
    for tool in events['tools']:
        if tool['line'] != use['line'] or tool['eventSha256'] != use['eventSha256'] or tool['outcome'] != 'RETURNED': continue
        at = tool.get('at')
        if (not at or local_time(use.get('at')) != at
                or not any(a.get('at') and at > a['at'] for a in matches)): continue
        inp = tool['input']
        if relative(root, inp.get('file_path')) != relative(root, str(report)): continue
        produced = (tool['name'] == 'Write' and inp.get('content') == report_text)
        produced |= (tool['name'] == 'Edit' and isinstance(inp.get('new_string'), str)
                     and bool(inp['new_string']) and inp['new_string'] in report_text)
        if produced:
            return {'ownerAttributionVerified':True,'sourceWarnings':[],
                    'reportWriterEvidence':{'run':relative(root, str(registered)), 'sessionId':sid,
                        'tool':tool['name'],'resultLine':tool['result']['line'],**citation(tool)}}
    return {'ownerAttributionVerified':False,'sourceWarnings':['관련 보고 생성·수정의 성공 이벤트와 채택 이후 시각 확인 못 함']}


def manual_trail(root, env, changes, manuals):
    """Join only explicit owner decisions and at most two later registered runs.

    Read return, adoption, and a worker's reported use are distinct evidence stages.
    Neither a file hash nor a Read proves understanding, game quality or acceptance.
    """
    rows={}
    def row(path):
        return rows.setdefault(path,{'path':path,'changes':[],'adoptions':[],
            'readsAfterChange':[],'laterTaskReads':[],'usageReports':[]})
    def manual(path):
        return (path and path.endswith('.md') and path.startswith(('Docs/','AI_WORKFLOW/','.agents/'))
                and not path.startswith(('Docs/00_CURRENT/','Docs/04_DEVELOPMENT_LOG/'))
                and Path(path).name not in ('HANDOFF.md','CODEX_HANDOFF.md'))
    for change in changes:
        if manual(change['path']) and change['outcome']=='RETURNED':
            row(change['path'])['changes'].append(dict(change,basis='선택 run의 Edit/Write 반환; 현재 일치는 별도'))
    reviews=[]
    for job,worker in env.get('workers',{}).items():
        review=worker.get('componentReview') or {};component=review.get('components',{}).get('manual',{})
        if component.get('decision')=='adopted_integrated':
            reviews.append((job,review,component,f'productionEnvironment.workers.{job}.componentReview.components.manual'))
        method=worker.get('methodAdoptionReview') or {}
        if isinstance(method,dict) and str(method.get('decision','')).startswith('adopted_applied'):
            reviews.append((job,method,method,f'productionEnvironment.workers.{job}.methodAdoptionReview'))
    for job,review,component,pointer in reviews:
        for path,hashes in component.get('files',{}).items():
            if isinstance(hashes,list) and len(hashes)==2: hashes=dict(zip(('before','after'),hashes))
            rel=relative(root,path)
            if not manual(rel) or not isinstance(hashes,dict): continue
            target=root/rel
            current=hashlib.sha256(target.read_bytes()).hexdigest() if target.is_file() else None
            basis={'source':STATE,'pointer':pointer,
                   'at':local_time(review.get('reviewedAt')),'owner':review.get('owner'),
                   'sessionId':review.get('sessionId'),'decision':component['decision'],
                   'beforeSha256':hashes.get('before'),'afterSha256':hashes.get('after'),
                   'currentSha256':current,'currentMatchesAdopted':bool(current) and current==hashes.get('after'),
                   'backup':relative(root,component.get('backup')),'basis':'원본 반영은 owner의 기존 채택 기록; 후속 사용은 별도'}
            row(rel)['adoptions'].append(basis)
    for item in rows.values():
        times=[r['at'] for r in item['changes']+item['adoptions'] if r.get('at')]
        item['changedAt']=max(times) if times else None
        item['readsAfterChange']=[m for m in manuals if m['path']==item['path']
            and item['changedAt'] and (m.get('at') or '')>item['changedAt']]
    # Optional support observations point to an owner's actual result, not a new
    # authority or a Read-only completion claim. Bind the report and source line.
    event_cache = {}
    for worker in env.get('workers',{}).values():
        for use in worker.get('toolAdoptionMethodUses',[]):
            if not isinstance(use,dict): continue
            item=rows.get(use.get('manualPath'))
            if (not item or not re.fullmatch('[a-f0-9]{64}',str(use.get('manualSha256','')))
                    or use.get('manualSha256') not in {a.get('afterSha256') for a in item['adoptions']}
                    or type(use.get('line')) is not int or use['line']<1): continue
            try:
                report=inside(root,use['report']);source=inside(root,use['source'])
                if report.stat().st_size>256_000 or source.stat().st_size>32_000_000: continue
                line=source.read_text('utf-8-sig').splitlines(keepends=True)[use['line']-1]
                report_bytes=report.read_bytes()
                if hashlib.sha256(report_bytes).hexdigest()!=use['reportSha256'] or hashlib.sha256(line.encode()).hexdigest()!=use['eventSha256']: continue
                report_text=report_bytes.decode('utf-8-sig')
                data=json.loads(report_text)
                if not isinstance(data,dict): continue
                claim=data.get(use['reportField'])
                if not isinstance(claim,str) or data.get('session')!=use.get('sessionId'): continue
                if not item['changedAt'] or (local_time(use.get('at')) or '')<=item['changedAt']: continue
            except (KeyError,IndexError,ValueError,OSError,TypeError): continue
            provenance=owner_method_use_provenance(root,env,use,item['adoptions'],report,
                report_text,source,line,event_cache)
            verified=provenance['ownerAttributionVerified']
            item['usageReports'].append({**use,**provenance,
                'status':'OWNER_REPORTED_WITH_RESULT' if verified else 'REGISTERED_REPORT_UNVERIFIED_SOURCE',
                'text':text(claim) if verified else '등록된 후속 보고·출처 미확인: '+text(claim),
                'basis':('등록된 owner run/init SID 및 성공한 보고 작성·수정 이벤트를 확인; 실제 이해·게임 수락은 별도'
                    if verified else '보고·파일 해시는 일치하나 owner의 실제 적용 출처는 확인 못 함; 검증된 사용 보고로 집계하지 않음')})
    earliest=min((r['changedAt'] for r in rows.values() if r['changedAt']),default=None)
    workers=sorted(env.get('workers',{}).items(),key=lambda kv:local_time(kv[1].get('startedAt')) or '',reverse=True)
    selected=[(key,w) for key,w in workers if earliest and (local_time(w.get('startedAt')) or '')>earliest
              and w.get('sessionId') and w.get('logDirectory')][:2]
    for key,worker in selected:
        try: folder=inside(root,worker['logDirectory']);wt=inside(root,worker['worktree'])
        except (ValueError,OSError,KeyError): continue
        events=run_events(root,folder)
        if events.get('sessionId')!=worker['sessionId']: continue
        handoff=folder/'HANDOFF.md'
        body=handoff.read_text('utf-8-sig',errors='replace') if handoff.is_file() and handoff.stat().st_size<256_000 else ''
        for tool in events['tools']:
            if tool['name']!='Read' or tool['outcome']!='RETURNED': continue
            try:
                actual=inside(root,tool['input'].get('file_path'))
                logical=actual.relative_to(wt).as_posix().removeprefix('Evidence/')
            except (ValueError,OSError): continue
            item=rows.get(logical)
            if not item or not item['changedAt'] or (tool.get('at') or '')<=item['changedAt']: continue
            actual_sha=hashlib.sha256(actual.read_bytes()).hexdigest() if actual.is_file() else None
            adopted={a['afterSha256'] for a in item['adoptions'] if a.get('afterSha256')}
            read={'jobId':key,'sessionId':worker['sessionId'],'path':logical,'actualPath':relative(root,str(actual)),
                  'currentCopySha256':actual_sha,'copyMatchesAdopted':actual_sha in adopted if adopted else None,
                  'basis':'다른 등록 작업의 Read 반환; 이해·채택·적용의 증명은 아님',**citation(tool)}
            item['laterTaskReads'].append(read)
            for number,line in enumerate(body.splitlines(),1):
                if ((logical in line or Path(logical).name in line)
                        and re.search(r'\b(used|applied|reused)\b|적용했|사용했|재사용했',line,re.I)
                        and not re.search(r'\b(not|never|will|should|plan|unverified)\b|미적용|미사용|사용하지|적용하지|예정|미확인',line,re.I)):
                    item['usageReports'].append({'jobId':key,'status':'REPORTED_WITH_EVIDENCE',
                        'text':text(line),'source':relative(root,str(handoff)),'line':number,
                        'eventSha256':hashlib.sha256(line.encode()).hexdigest(),
                        'readEvidence':citation(tool),'basis':'후속 작업자의 사용 보고; 게임 검증·사람 수락과 별도'})
    return list(rows.values())


def bridge_observation(root, env):
    """Separate legacy supervisor failure evidence from isolated successful probes."""
    lease = env.get('coordinatorLease', {})
    current = env.get('relaySupport', {}).get('bridgeObservation', {})
    if current.get('generation') == lease.get('generation') and current.get('sessionId') == lease.get('sessionId'):
        result = dict(current, source=STATE, basis='현재 generation의 supervisor 관측')
    else:
        log = root/'Logs/ParallelProduction/Autonomous-20261011/supervisor.log'
        try:
            lines = log.read_text('utf-8-sig', errors='replace').splitlines()
            errors = [(i+1, line) for i, line in enumerate(lines) if line.startswith('Relay maintenance preserved:')]
        except OSError:
            errors = []
        result = {'status': 'LEGACY_ERROR_NO_DETAIL' if errors else 'UNKNOWN',
                  'source': relative(root, str(log)), 'countInRetainedLog': len(errors),
                  'lastLine': errors[-1][0] if errors else None,
                  'message': errors[-1][1] if errors else '브리지 관측 없음',
                  'observedAt': None, 'basis': '누적 로그에 예외명만 보존. 발생 시각·동일 run의 원인 본문은 미보존.',
                  'claudeResumeBlocked': None, 'codexRelayAttemptSkipped': None, 'dispatchStatus':'UNKNOWN',
                  'writerTransfer': 'separate_adapter_gates_not_executed'}
    # Registry pointers belong to the isolated support job; never promote to production health.
    diagnostics = []
    for worker in env.get('workers', {}).values():
        for label, path in worker.get('observationEvidence', {}).items():
            rel = relative(root, path)
            if not rel:
                continue
            report = load(root/rel)
            diagnostics.append({'label': label, 'source': rel, 'status': report.get('status'),
                                'observedAt': report.get('observedAt'), 'exception': report.get('exception'),
                                'scope': 'isolated_support_probe_not_running_supervisor',
                                'productionStateWritten': report.get('productionStateWritten')})
    result['isolatedDiagnostics'] = diagnostics
    return result


def scheduled_task_observations(root, env, session_id):
    """Retained scheduler JSON is evidence of a task observation, never proof of a resumed lead."""
    rows = []
    if not session_id: return rows
    for worker_id, worker in env.get('workers', {}).items():
        request = worker.get('ownerReviewRequest', {})
        if not isinstance(request, dict) or request.get('targetSessionId') != session_id: continue
        path = request.get('scheduledTaskEvidence')
        try:
            source = inside(root, path)
            if source.suffix.lower() != '.json' or source.stat().st_size > 256_000: continue
        except (ValueError, OSError): continue
        report = load(source)
        if not isinstance(report, dict) or report.get('sameSid', session_id) != session_id: continue
        rows.append({'workerId':worker_id,'source':relative(root, str(source)),
                     'observedAt':report.get('observedAt'),'task':report.get('task'),
                     'state':report.get('state'),'lastRun':report.get('lastRun'),
                     'lastResult':report.get('lastResult'),'nextRun':report.get('nextRun'),
                     'serverResetAt':report.get('serverResetAt'),
                     'actualRestartReported':report.get('actualRebootOrRestartObserved'),
                     'basis':'저장된 스케줄러 관측. lastResult=0이나 지난 reset은 실제 제작 재개·사용량 회복의 증거가 아님.'})
    return rows


def observation(root, state=None, probe=process_observation):
    root = root.resolve()
    state = state if state is not None else load(root / STATE)
    env = state.get('productionEnvironment', {})
    route = state.get('demoProductionRouting', {})
    auto = env.get('autonomous', {})
    coord = env.get('coordinator', {})
    lease = env.get('coordinatorLease', {})
    run_path = coord.get('runDirectory')
    try:
        run = inside(root, run_path)
    except (ValueError, OSError):
        run = root / '__unobserved_run__'
    run_rel = relative(root, str(run))
    events = run_events(root, run)
    tools = events['tools']
    result = load(run / 'result.json')
    actual_session = result.get('session_id') or events.get('sessionId')
    identity_matches = bool(actual_session) and actual_session == lease.get('sessionId')
    cycle_at = max((value for value in (local_time(auto.get('cycleStartedAt')),
                                       local_time(lease.get('startedAt'))) if value), default=None)
    if events['lastAt'] and local_time(lease.get('startedAt')) and events['lastAt']<local_time(lease['startedAt']):
        events['warnings'].append('선택 run의 마지막 도구 이벤트는 현재 lease 시작 전임; 현재 실행의 진척으로 승격하지 않음')
    launcher = probe(lease.get('launcherPid'), lease.get('startedAt'))
    child = probe(lease.get('cliPid'), cycle_at)
    state_status = auto.get('status', 'UNKNOWN')
    waiting = state_status == 'waiting_usage_reset'
    statuses = {'pause_requested':'PAUSE_REQUESTED','paused_by_user':'PAUSED',
        'paused_checkpoint_or_blocker':'BLOCKED','paused_resume_gate_blocked':'BLOCKED',
        'paused_other_session_or_unverified_registry':'BLOCKED','paused_unknown_reset':'BLOCKED',
        'waiting_usage_reset':'WAITING','starting':'STARTING','running':'RUNNING',
        'stopped':'STOPPED','stopped_preserved':'STOPPED','stopped_result_preserved':'STOPPED',
        'paused_by_limit':'BLOCKED','ready_for_human_review':'HUMAN_REVIEW','idle':'STOPPED'}
    status = statuses.get(state_status,'UNKNOWN')
    reason = result.get('result') if waiting else coord.get('currentTask')
    if status=='PAUSE_REQUESTED': reason='안전한 중지 요청이 기록됨; child 생존과 별도'
    elif status=='BLOCKED':
        checkpoint=auto.get('checkpoint') or {}
        reason='기록된 checkpoint '+str(local_time(checkpoint.get('recordedAt')) or '(시각 미확인)')+': '+str(checkpoint.get('nextAction') or '원문 확인 필요')
    if status=='RUNNING' and (not identity_matches or lease.get('status') not in ('active','running') or child['status']!='ALIVE'):
        status,reason='UNKNOWN','실행 기록과 실제 session/lease/child 정체를 함께 확인하지 못함'
    if status == 'RUNNING':
        reason = '등록된 child PID 생존; 게임 진행률은 보고하지 않음'
    checkpoint_at = local_time((auto.get('checkpoint') or {}).get('recordedAt'))
    lease_at = local_time(lease.get('startedAt'))
    if (state_status == 'paused_checkpoint_or_blocker' and auto.get('stopRequested') is not True
            and checkpoint_at and lease_at and checkpoint_at < lease_at
            and identity_matches and lease.get('status') == 'active' and child['status'] == 'ALIVE'
            and events['lastAt'] and events['lastAt'] >= lease_at):
        status = 'RUNNING_HEADLESS'
        reason = '새 동일 SID 실행의 실제 도구 활동 확인; 옛 autonomous checkpoint/비활성 상태는 보존됨'
    reservation = auto.get('resumeReservation', {})
    reservation_identity = bool(lease.get('sessionId')) and (reservation.get('sessionId') == lease.get('sessionId') and reservation.get('generation') == lease.get('generation'))
    reservation_valid = reservation_identity and not (reservation.get('waitCompletedAt') or reservation.get('cancelledAt'))
    usage = auto.get('modObservation', {})
    usage_valid = bool(lease.get('sessionId')) and usage.get('sessionId') == lease.get('sessionId')
    observed_now = stamp()
    limits = [row for row in usage.get('rateLimits', []) if isinstance(row, dict)] if usage_valid else []
    observed_at = local_time(usage.get('observedAt')) if usage_valid else None
    future_limits = [row for row in limits if local_time(row.get('resetsAt')) and local_time(row['resetsAt']) > observed_now]
    exhausted = [row for row in limits if isinstance(row.get('percentUsed'), (int, float)) and row['percentUsed'] >= 100]
    waiting_limit = [row for row in future_limits if row in exhausted] if observed_at else []
    waiting_for_reset = bool(waiting_limit)
    if (waiting_for_reset and state_status == 'paused_checkpoint_or_blocker'
            and auto.get('stopRequested') is not True and identity_matches
            and child['status'] == 'EXITED' and lease.get('status') == 'stopped'
            and result.get('api_error') == 'usage_limit_reached'):
        status = 'WAITING'
    quota_state = ('OBSERVED_EXHAUSTED_UNTIL_RESET' if waiting_for_reset else
                   'RESET_ELAPSED_UNVERIFIED' if exhausted else
                   'OBSERVED_UNEXHAUSTED' if observed_at and limits else 'UNKNOWN')
    if status == 'WAITING' and not waiting_for_reset:
        status, reason = 'UNKNOWN', '기록된 사용량 대기 상태와 현재 차단을 구분함; 최신 owner 관측이나 실제 재개 증거 확인 필요'
    elif status == 'WAITING':
        reason = '같은 SID owner의 사용량 소진 관측과 아직 지나지 않은 서버 reset을 확인한 대기 상태'
    pending_resume = (reservation_valid and local_time(reservation.get('resumeAt'))
                      and local_time(reservation['resumeAt']) > observed_now)
    scheduler = scheduled_task_observations(root, env, lease.get('sessionId'))
    latest = tools[-1] if tools else None
    artifacts = {}
    for worker in env.get('workers',{}).values():
        for path in worker.get('toolAdoptionEvidence',{}).values():
            a=artifact(root,path,'기존 지원 결과의 diff/실제 근거; owner 채택/게임 수락은 별도')
            if a: artifacts[a['path']]=a
    bridge = bridge_observation(root, env)
    for item in bridge.get('isolatedDiagnostics', []):
        a = artifact(root, item['source'], '격리 브리지 진단; 운영 supervisor 복구 확인 아님')
        if a:
            artifacts[a['path']] = a
    manuals, reused, changes, checks = [], [], [], []
    for tool in tools:
        inp = tool['input']; name = tool['name'] or ''
        source = citation(tool)
        for ref in refs(root, inp):
            reading = name == 'Read' or (name == 'Bash' and re.search(r'\b(cat|head|tail|sed|Get-Content)\b', str(inp.get('command', ''))))
            if ref.endswith('.md') and tool['outcome'] == 'RETURNED' and reading:
                manuals.append({'path': ref, 'basis': '실제 읽기 도구 반환; 이해/수락 여부는 별도', **source})
            command = str(inp.get('command', ''))
            invoked = name == 'Bash' and re.search(r'(?:python(?:\.exe)?|py)\s+(?:(?:-B|-X\s+utf8)\s+)*[\x22\x27]?' + re.escape(ref), command)
            if ref.startswith('Tools/') and ref.endswith('.py') and tool['outcome'] == 'RETURNED' and invoked:
                reused.append({'path': ref, 'basis': '실제 Python 도구 실행 반환', **source})
            if ref.startswith('Logs/'):
                item = artifact(root, ref, source=source)
                if item:
                    artifacts[ref] = item
        if name in ('Edit', 'Write'):
            path = relative(root, inp.get('file_path'))
            if path:
                change = {'path': path, 'outcome': tool['outcome'], **source}
                new = inp.get('new_string', '') if name=='Edit' else inp.get('content','')
                change['currentTextMatches'] = bool(new) and (root/path).is_file() and new in (root/path).read_text('utf-8-sig', errors='replace')
                change['experience'] = '파일 편집 이벤트; 플레이 경험·수락은 해당 검증 증거로 별도 확인'
                changes.append(change)
        if 'execute_menu_item' in name and tool['outcome'] == 'RETURNED':
            reused.append({'path': text(inp.get('menu_path')), 'basis': '기존 Editor fixture 실행 반환', **source})
        if 'screenshot' == inp.get('action'):
            filename = inp.get('screenshot_file_name')
            if filename:
                path = str(inp.get('output_folder', '')) + '/' + str(filename)
                item = artifact(root, path, 'capture', source)
                if item:
                    artifacts[item['path']] = item
        if tool['result'] and ('ShaderHasError' in str(inp) or 'GetShaderMessages' in str(inp)):
            body = str(tool['result']['body'])
            # Keep only the returned verification excerpt, never code or shell output wholesale.
            m = re.search(r'errors[=:][^\n\r\x22\x27]{0,150}', body)
            checks.append({'label': 'shader 메시지 조회', 'result': text(m[0] if m else '반환 있음; 원문 열기'), **source})
    # Actual current-cycle registered workers only. Historical and native support are labelled separately.
    team = []
    for key, worker in env.get('workers', {}).items():
        started = local_time(worker.get('startedAt') or worker.get('createdAt'))
        if not started or not cycle_at or started < cycle_at:
            continue
        log = worker.get('logDirectory', '')
        handoff = artifact(root, log + '/HANDOFF.md', 'worker handoff')
        item = {'id': key, 'provider': worker.get('provider', 'UNKNOWN'), 'sessionId': worker.get('sessionId'),
                'status': worker.get('status', 'UNKNOWN'), 'task': text(worker.get('goal')), 'startedAt': started,
                'stoppedAt': worker.get('stoppedAt'), 'scope': worker.get('executionMethod', 'isolated worker'),
                'process': probe(worker.get('cliPid'), started), 'source': STATE, 'handoff': handoff}
        team.append(item)
        if handoff:
            artifacts[handoff['path']] = handoff
        # Existing goal evidence is a pointer, never proof the worker opened it.
        for ref in refs(root, worker.get('goal', '')):
            a = artifact(root, ref, '배정에 연결된 기존 증거 (열람 여부 별도)')
            if a and a['kind'] == 'video':
                artifacts[ref] = a
        for evidence in worker.get('evidence', []):
            if not isinstance(evidence, str) or not evidence.startswith('Evidence/Logs/'):
                continue
            # The registered immutable copy is what the worker was given. Do not
            # silently substitute a subsequently changed primary-project file.
            copy = str(worker.get('worktree', '')) + '/' + evidence
            a = artifact(root, copy, 'worker에 배정된 보존 증거; 실제 열람은 handoff 참조')
            if a:
                if a['kind'] == 'video':
                    a['role'] = '레퍼런스 PC 영상 (게임 검증과 별도)' if 'DinkumReference' in evidence else '이전 실제 플레이 영상; 이번 run의 재실행 아님'
                a['displayPath'] = evidence.removeprefix('Evidence/')
                a['registeredSha256'] = worker.get('evidenceHashes', {}).get(evidence)
                artifacts[a['path']] = a
    def unique(rows, field='path'):
        seen = set(); output = []
        for row in reversed(rows):
            if row[field] not in seen:
                output.append(row); seen.add(row[field])
        return output
    changes = unique(changes)
    manuals = unique(manuals)
    reused = unique(reused)
    # Existing handoff carries video/validation evidence from the immediately preceding work.
    handoff_path = root/'Docs/00_CURRENT/CODEX_HANDOFF.md'
    if handoff_path.is_file():
        handoff_refs = refs(root, handoff_path.read_text('utf-8-sig'))
        preserved = next((p for p in handoff_refs if p.endswith('/backup-before/CODEX_HANDOFF.md')), None)
        if preserved and handoff_path.read_text('utf-8-sig').startswith('# 최종발표용 Windows 실행 빌드'):
            handoff_path = root/preserved
    if handoff_path.is_file():
        for ref in refs(root, handoff_path.read_text('utf-8-sig')):
            if Path(ref).suffix in OPEN_SUFFIXES:
                a = artifact(root, ref, '이전 제작 인계 증거; 이번 run의 재실행 아님')
                if a:
                    artifacts.setdefault(ref, a)
    # Existing packaged builds only. Open their folder, never execute a previously denied player.
    builds = root / 'Builds/Windows'
    if builds.is_dir():
        folders = sorted((p for p in builds.iterdir() if p.is_dir()), key=lambda p:p.name, reverse=True)
        for folder in folders[:30]:
            if any(folder.glob('*.exe')) and any(folder.glob('*_Data')):
                rel = relative(root, str(folder))
                if rel:
                    artifacts[rel] = {'path':rel,'kind':'build','role':'기존 빌드 폴더; 최신 소스/플레이 수락 증거와 별도',
                                      'openable':True,'openMode':'folder_only','source':None}
    last_change = changes[0] if changes else None
    upstream = next((m for m in manuals if m['path'].endswith('/HANDOFF.md')), None)
    after = [a for a in artifacts.values() if a.get('source') and last_change and (a['source'].get('at') or '') > (last_change.get('at') or '')]
    trail=manual_trail(root,env,changes,manuals)
    for item in trail:
        for path in [item['path']]+[a.get('backup') for a in item['adoptions']]+[r['source'] for r in item['usageReports']]:
            a=artifact(root,path,'매뉴얼 반영/후속 보고 근거')
            if a: artifacts.setdefault(a['path'],a)
    counts={k:sum(len(r[k]) for r in trail) for k in ('changes','adoptions','readsAfterChange','laterTaskReads','usageReports')}
    counts['unverifiedUsageReports']=sum(r.get('status')=='REGISTERED_REPORT_UNVERIFIED_SOURCE'
        for item in trail for r in item['usageReports'])
    counts['usageReports']-=counts['unverifiedUsageReports']
    missing=[label+' 확인 못 함' for key,label in [('changes','선택 run의 매뉴얼 편집'),('adoptions','owner의 원본 채택'),
        ('laterTaskReads','후속 다른 작업의 매뉴얼 읽기'),('usageReports','후속 작업의 적용 보고')] if not counts[key]]
    loop = {'change': last_change, 'manuals': manuals, 'reuse': reused, 'checks': checks,
            'critique': upstream,
            'lesson': '출처·시각이 있는 읽기/편집/검증과 owner 채택·후속 사용 보고를 각각 표시',
            'reuseAfterFix': [a for a in after if a['kind'] == 'image'],
            'manualTrail':trail,'stageCounts':counts,
            'coverage':f"편집 {counts['changes']} / 원본 채택 {counts['adoptions']} / 후속 읽기 {counts['laterTaskReads']} / 사용 보고 {counts['usageReports']} / 출처 미확인 보고 {counts['unverifiedUsageReports']}",
            'remaining':missing+['Read 반환과 적용 보고는 이해·재미·미술 품질·사람 수락을 자동 판정하지 않음.',
                '추가 이벤트 수집은 owner가 다음 정상 세션 경계에서 후보를 통합한 뒤 활성화.'],
            'searchScope':'선택 run + 기존 owner 채택 기록 + 이후 등록된 최신 두 worker run (각 32 MB 이하)'}
    def evidence_order(a):
        return (a['role'] == 'capture', a.get('source', {}).get('at') or '') if isinstance(a.get('source'), dict) else (False, '')
    ordered = sorted(artifacts.values(), key=evidence_order, reverse=True)
    return {'schemaVersion': 1, 'observedAt': stamp(), 'source': STATE, 'derivedOnly': True,
            'production': {'status': status, 'recordedStatus': state_status, 'reason': text(reason),
                           'owner': route.get('unityEditorOwner'), 'approval': route.get('selectedApproval'), 'ticket': route.get('activeTicket'),
                           'sessionId': lease.get('sessionId'), 'run': run_rel, 'identityMatches': identity_matches,
                           'lastActivityAt': events['lastAt'], 'lastActivityBasis': '선택 run의 마지막 실제 tool_use 시각; UI 갱신/heartbeat 제외',
                           'lastTask': text(latest['input'].get('description') or latest['input'].get('screenshot_file_name') or latest['name']) if latest else '확인 못 함',
                           'launcher': launcher, 'child': child, 'leaseStatus': lease.get('status'),
                           'pluginsObserved': events['plugins'], 'warnings': events['warnings']},
            'usage': {'scope': 'background production session', 'sessionId': lease.get('sessionId'),
                      'observedAt': usage.get('observedAt') if usage_valid else None,
                      'rateLimits': usage.get('rateLimits', []) if usage_valid else [],
                      'quotaState':quota_state,'waitingForReset':waiting_for_reset,
                      'resetAt':min((row['resetsAt'] for row in future_limits),key=local_time) if observed_at and future_limits else None,
                      'resumeAt': reservation.get('resumeAt') if pending_resume else None,
                      'scheduledTask': reservation.get('scheduledTask') if pending_resume else None,
                      'reservationEvidence': relative(root, reservation.get('evidence')) if reservation_identity else None,
                      'reservationRecord':dict(reservation) if reservation_identity else None,
                      'scheduledTaskObservations':scheduler,
                      'reservationValid': reservation_valid, 'codexBalance': None,
                      'note': '사용량은 같은 SID의 최신 owner modObservation 원문. 예약·지난 reset은 회복/재기동 증거가 아님. 스케줄러 관측과 실제 child/session 이벤트를 구분. 누적 modelUsage는 구독 잔량이 아님.'},
            'team': team, 'changes': changes, 'artifacts': ordered, 'loop': loop, 'bridge': bridge,
            'limits': ['게임 완료율을 계산하지 않음', '현재 dirty 전체를 이번 실행 변경으로 간주하지 않음',
                       '등록되지 않은 agent/진행률/사용량은 확인 못 함']}


def open_evidence(root, path, view=None, opener=None):
    view = view or observation(root)
    rel = relative(root, path)
    allowed = {a['path'] for a in view.get('artifacts', []) if a.get('openable')}
    if rel not in allowed:
        raise ValueError('Evidence is not in the current observed allowlist')
    target = inside(root, rel)
    item = next(a for a in view['artifacts'] if a.get('path') == rel and a.get('openable'))
    if item.get('kind') == 'build':
        target.relative_to((root / 'Builds/Windows').resolve())
        if item.get('openMode') != 'folder_only' or not target.is_dir():
            raise ValueError('Build folder only; no player execution')
        (opener or os.startfile)(str(target))
        return {'opened':rel,'mode':'folder_only','executed':False}
    if target.suffix.lower() not in OPEN_SUFFIXES or not target.is_file():
        raise ValueError('Unsupported evidence file')
    (opener or os.startfile)(str(target))
    return {'opened': rel}
