// Local observation. Explicit user actions alone request the existing guarded helper.
const MOD_VERSION = '0.3.0';
const BUILD_ID = 'pa-vscode-connection-20261023';
const ROOT = 'C:/Users/sdjsd/Desktop/Unity/Project_PA';
const PYTHON = 'C:/Users/sdjsd/AppData/Local/Programs/Python/Python312/python.exe';
const SCRIPT = ROOT + '/Tools/PaAgentConsole/pa_console.py';
const PANE = 'pa-production-state';
let state = {}, usage = {}, viewerVersion = {}, sessionId = null, agents = [], observedAt = null;
let draft = '', submitting = false, draftId = null, submittedText = '';
let error = '', notice = '', section = 'overview', media = 'all', offset = 0, opened = false, pending = null, timer = null;
let autoOpenStatus = '명시적 /pa 명령으로만 열기';
let controlling = false;
let openGeneration = 0;
const tabs = { overview: '현재 작업', team: '담당자', changes: '파일·경험', evidence: '화면·영상·검증', loop: '매뉴얼·위임', usage: '사용량', bridge: '브리지 오류', instructions: '지시·답변' };
const clean = (v, n = 700) => String(v ?? '확인 못 함').replace(/[\u0000-\u001f\u007f-\u009f]/g, ' ').slice(0, n);
const obs = () => state._console?.observation ?? {};
const evidence = () => (obs().artifacts ?? []).filter(a => media === 'all' || a.kind === media);
const cite = r => clean(r?.source) + (r?.line ? ':' + r.line : '');
function time(v) { if (!v) return '확인 못 함'; try { return new Date(v).toLocaleString('ko-KR', { timeZone: 'Asia/Seoul', hour12: false }) + ' KST'; } catch { return clean(v); } }
function limits(v) { const rows = Array.isArray(v?.rateLimits) ? v.rateLimits : []; return rows.length ? rows.map(r => clean(r.kind) + ' 사용 ' + (Number.isFinite(r.percentUsed) ? r.percentUsed.toFixed(0) + '%' : '확인 못 함') + ' / reset ' + time(r.resetsAt)).join('\n') : '사용량 확인 못 함 (보고 없음)'; }
function resume() { const p = obs().production ?? {}; return '기존 owner가 정상 경계에서 통합. 중복 제작 시작 금지.\nApproval: ' + clean(p.approval) + '; ticket: ' + clean(p.ticket) + '; owner: ' + clean(p.owner) + '\nrun: ' + clean(p.run) + '\n제작 인계: ' + clean(state._console?.productionHandoffPath) + '\nProvider switch is not a task approval. 기존 dirty·누적 시도·사용량·반려·최종 사람 검수를 보존한다.'; }
async function refresh($) {
  if (pending) return pending;
  pending = (async () => {
    try { const r = await $.process.run([PYTHON, '-B', '-X', 'utf8', SCRIPT, '--root', ROOT, '--mod-state'], { cwd: ROOT, timeoutMs: 30000 }); if (r.exitCode !== 0) throw new Error('읽기 전용 조회 실패'); state = JSON.parse(r.stdout); error = ''; } catch (e) { error = clean(e?.message); }
    try { usage = await $.session.usage(); } catch { usage = {}; }
    try { sessionId = await $.session.id(); } catch { sessionId = null; }
    try { viewerVersion = await $.session.version(); } catch { viewerVersion = {}; }
    try { agents = await $.agent.list(); } catch { agents = []; }
    observedAt = new Date().toISOString(); $.ui.invalidate('ui.render');
  })();
  try { await pending; } finally { pending = null; }
}
async function instructionCommand($, flag, payload) {
  const r = await $.process.run([PYTHON, '-B', '-X', 'utf8', SCRIPT, '--root', ROOT, flag, JSON.stringify(payload)], { cwd: ROOT, timeoutMs: flag === '--production-control' ? 30000 : 8000 });
  if (r.exitCode !== 0) throw new Error(clean(r.stderr || r.stdout || '지시 기록 실패'));
  return JSON.parse(r.stdout);
}
async function controlProduction($, action) {
  if (controlling) return;
  controlling = true;
  try {
    await refresh($);
    if (error) throw new Error('최신 조회 실패 · 제어하지 않음');
    const controls = state._console?.controls ?? {};
    const gate = action === 'resume' ? controls.resume : controls.safeStop;
    if (gate?.allowed !== true) { notice = '요청 보류 · ' + clean((gate?.reasons ?? ['검사 근거 없음']).join(' / ')); return; }
    const result = await instructionCommand($, '--production-control', {action, expected: controls.requestPin});
    notice = clean(JSON.stringify(result), 1600);
    await refresh($);
  } catch (e) { notice = '제어 요청 실패 · ' + clean(e?.message); }
  finally { controlling = false; $.ui.invalidate('ui.render'); }
}
async function openPane($) {
  const requested = ++openGeneration;
  await refresh($);
  if (requested !== openGeneration) return;
  opened = true;
  if (!timer) timer = $.clock.every(10000, async () => { if (opened) await refresh($); });
  await $.ui.open({id:PANE, title:'P.A. 제작', focus:true, closeOnEscape:true});
}
async function submitInstruction($, value) {
  if (submitting || !String(value ?? '').trim()) return;
  submitting = true; draft = String(value); $.ui.invalidate('ui.render');
  if (!draftId || submittedText !== draft) { draftId = Date.now().toString() + '-' + Math.random().toString(36).slice(2, 12); submittedText = draft; }
  const id = draftId;
  try {
    const saved = await instructionCommand($, '--instruction-submit', { id, text: draft, sourceSessionId: sessionId, sourceVersion: viewerVersion.version ?? null });
    notice = '저장됨 · ' + saved.id;
    await refresh($);
    const producer = obs().production ?? {};
    if (saved.canDeliver && !error && producer.identityMatches === true && producer.child?.status === 'ALIVE' && producer.sessionId === saved.targetSessionId) {
      // Persist uncertainty before entering transport. A crash must not cause an automatic resend.
      const gate = await instructionCommand($, '--instruction-delivery', { id: saved.id, beginSend: true, result: { unknown: true, error: '공식 session.send 호출 시작 · 결과 대기' } });
      if (gate.canSend !== true) { notice = '저장됨 · 대상 변경/기존 시도로 전송 보류 · ' + clean(gate.targetHold?.reason ?? gate.reason); await refresh($); return; }
      let result;
      try { result = await $.session.send({ to: { sessionId: gate.targetSessionId }, text: saved.envelope }); }
      catch (e) { result = { unknown: true, error: clean(e?.message) }; }
      await instructionCommand($, '--instruction-delivery', { id: saved.id, result });
      notice = result.isDelivered === true ? '전달됨(수신 큐) · 실제 읽기·답변 확인 대기' : result.unknown ? '전송 결과 미확인 · 자동 재전송 안 함' : '저장됨 · 전달 안 됨: ' + clean(result.reason);
    } else notice += ' · 미전달/재개 대기 · 대상 보존 · 같은 owner의 정상 시작 배정에서 회수';
    draft = ''; draftId = null; submittedText = ''; await refresh($);
  } catch (e) { notice = '지시 처리 실패 · 원문을 입력창에 보존: ' + clean(e?.message); }
  finally { submitting = false; $.ui.invalidate('ui.render'); }
}
function summary() {
  const o = obs(), p = o.production ?? {}, u = o.usage ?? {};
  return ['백그라운드 제작 · ' + clean(p.status) + ' / ' + clean(p.recordedStatus), '이유: ' + clean(p.reason),
    '마지막 작업: ' + clean(p.lastTask), '실제 도구 활동: ' + time(p.lastActivityAt),
    'owner ' + clean(p.owner) + ' · ' + clean(p.ticket) + ' · ' + clean(p.approval),
    'launcher ' + clean(p.launcher?.pid) + ' ' + clean(p.launcher?.status) + ' / child ' + clean(p.child?.pid) + ' ' + clean(p.child?.status),
    '사용량 대기: 서버 reset ' + time(u.resetAt) + ' / 재개 예약 ' + time(u.resumeAt),
    '별도 브리지 상태: ' + clean(o.bridge?.status) + ' · ' + clean(o.bridge?.message), 'run: ' + clean(p.run), 'session: ' + clean(p.sessionId),
    ...(p.warnings ?? []), '화면 조회: ' + time(observedAt) + ' · 실제 활동 시각과 별도', '화면 열기: ' + autoOpenStatus,
    '실행 중 Mod ' + MOD_VERSION + ' / ' + BUILD_ID + ' · 표시용 engine ' + clean(viewerVersion.version),
    'Python ' + clean(state._console?.runtime?.python) + ' · ' + clean(state._console?.runtime?.pythonVersion),
    '프로젝트 소스 버전 ' + clean(state._console?.runtime?.sourceVersion) + ' · hook SHA ' + clean(state._console?.runtime?.sourceHookSha256),
    '소스 경로 ' + clean(state._console?.runtime?.pluginSource),
    '공식 설치 레코드(재로딩 증거와 별도) ' + clean(JSON.stringify(state._console?.runtime?.registeredPlugin), 1200),
    '정상 재개 조건 ' + clean(JSON.stringify(state._console?.controls?.resume)),
    '자동 재개 조건·예약 ' + clean(JSON.stringify(state._console?.controls?.automaticResume), 1200)];
}
function rows() {
  const o = obs(), u = o.usage ?? {}, l = o.loop ?? {};
  if (section === 'instructions') return ['입력 대상은 기록된 제작 Claude SID이며 표시용 세션/서브에이전트와 별도입니다.', '저장됨 → 전달됨(큐) → 실제 수신 → 처리 결과를 구분합니다. 메시지는 권한 승인·거절 해제가 아닙니다.',
    ...((o.instructions?.items ?? o.instructions ?? []).length ? (o.instructions?.items ?? o.instructions).slice(offset, offset + 3).flatMap(m => [clean(m.id) + ' · ' + clean(m.status), time(m.createdAt) + ' · target ' + clean(m.targetSessionId), clean(m.text, 1600), '공식 전달: ' + clean(m.delivery?.status) + ' · ' + clean(m.delivery?.reason ?? m.delivery?.error), '정상 시작 배정(수신 증거와 별도): ' + clean(m.startupAssignment?.source), '파일 조회: ' + clean(m.fileLookup?.source) + ' / 본문 반환: ' + clean(m.bodyRead?.source), '명시적 owner 수신: ' + clean(m.receipt?.confirmed) + ' · ' + clean(m.receipt?.source) + ':' + clean(m.receipt?.line), 'owner가 보고한 결과: ' + clean(m.result?.text ?? m.reply, 4000)]) : ['저장된 지시 없음']), 'owner 종료 시 기록을 보존하고 다음 정상 같은 SID 경계에서 회수합니다. 미확인 전송을 자동 반복하지 않습니다.'];
  if (section === 'bridge') { const b = o.bridge ?? {}; return ['사용량 대기와 별도 · ' + clean(b.status), clean(b.message), clean(b.basis),
    '원문: ' + clean(b.source) + ':' + clean(b.lastLine), '누적 로그의 반복 ' + clean(b.countInRetainedLog) + '회 / 발생 시각 ' + time(b.observedAt),
    'Claude reset 재개 차단 관측: ' + clean(b.claudeResumeBlocked), '전송 상태: ' + clean(b.dispatchStatus) + ' / 확인된 건너뜀: ' + clean(b.codexRelayAttemptSkipped),
    '전송 근거: ' + clean(JSON.stringify(b.dispatchResult ?? {})),
    'writer 인계: 별도 adapter 검사 경로. 이 UI 세션의 writer 전환 없음.',
    ...(b.isolatedDiagnostics ?? []).flatMap(d => ['격리 진단 · ' + clean(d.label) + ' · ' + clean(d.status), time(d.observedAt),
      d.exception ? clean(d.exception.category) + ': ' + clean(d.exception.message) : '후보의 읽기 전용 계정 조회 반환; 운영 supervisor 복구는 미확인',
      ...(d.exception?.frames ?? []).map(f => clean(f.file) + ':' + clean(f.line) + ' ' + clean(f.function)), clean(d.source)]),
    '기존 로그는 본문을 버렸으므로 과거 반복 원인을 모두 확정할 수 없음. owner 통합 후 다음 정상 경계에서 상세 이벤트 수집.']; }
  if (section === 'overview') return [...summary(), '이번 run 확인 변경 ' + (o.changes?.length ?? 0) + '개 · 증거 ' + (o.artifacts?.length ?? 0) + '개', '게임 완료율·사람 수락: 확인 못 함', '연결: loop-state + 선택 run JSONL + worker registry + 기존 증거'];
  if (section === 'team') return ['현재 제작 run에 등록된 독립 작업 ' + (o.team?.length ?? 0) + '건',
    ...(o.team ?? []).slice(offset, offset + 3).flatMap(a => [clean(a.provider) + ' · ' + clean(a.id), '기록 ' + clean(a.status) + ' / process ' + clean(a.process?.status), clean(a.task, 430), '시작 ' + time(a.startedAt) + ' / 종료 ' + time(a.stoppedAt), 'session ' + clean(a.sessionId) + ' / ' + clean(a.scope)]),
    '이 화면 세션의 native subagent (제작 worker와 별도):', ...(agents.length ? agents.map(a => clean(a.id) + ' · ' + clean(a.description) + ' · ' + clean(a.status)) : ['보고된 native subagent 없음; 다른 세션의 부재를 뜻하지 않음'])];
  if (section === 'changes') return ['이번 run의 확인된 Edit/Write (기존 dirty 전체와 별도)', ...(o.changes?.length ? o.changes.flatMap(c => [clean(c.path), clean(c.outcome) + ' · 현재 텍스트 일치 ' + clean(c.currentTextMatches), clean(c.experience), time(c.at) + ' · ' + cite(c)]) : ['확인된 변경 이벤트 없음']), '이벤트 밖의 shell/외부 편집은 이 목록의 완전성을 보장하지 않음.'];
  if (section === 'evidence') return ['기존 파일 열기 · 전후 조건/검증 범위는 원문 참조', ...evidence().slice(offset, offset + 5).flatMap(a => [clean(a.kind) + ' · ' + clean(a.role), clean(a.displayPath ?? a.path), a.source ? time(a.source.at) + ' · ' + cite(a.source) : '이전 인계/배정의 보존 증거']), '이전 영상은 이번 run에서 게임을 재실행한 결과로 표시하지 않음.'];
  if (section === 'loop') return ['최근 실제 변경 기반 위임 루프 · ' + clean(l.coverage), clean(l.lesson),
    '독립 검수 → ' + clean(l.critique?.path) + ' · ' + cite(l.critique), '개선 파일 → ' + clean(l.change?.path) + ' · ' + cite(l.change),
    ...(l.checks ?? []).slice(-2).map(c => '검증 → ' + clean(c.result) + ' · ' + cite(c)), ...(l.reuseAfterFix ?? []).map(a => '편집 이후 캡처 (내용 검수 별도) → ' + clean(a.path)),
    ...(l.manualTrail ?? []).flatMap(m => [clean(m.path),
      ...(m.changes ?? []).map(c => '매뉴얼 편집 반환 · 현재 일치 ' + clean(c.currentTextMatches) + ' · ' + time(c.at) + ' · ' + cite(c)),
      ...(m.adoptions ?? []).map(a => '원본 채택 기록 · ' + clean(a.owner) + ' · 현재 해시 일치 ' + clean(a.currentMatchesAdopted) + ' · ' + time(a.at) + ' · ' + clean(a.pointer)),
      ...(m.readsAfterChange ?? []).map(r => '선택 run의 수정 후 읽기 · ' + cite(r)),
      ...(m.laterTaskReads ?? []).map(r => '후속 다른 작업 읽기 · ' + clean(r.jobId) + ' · 채택본 해시 일치 ' + clean(r.copyMatchesAdopted) + ' · ' + cite(r)),
      ...(m.usageReports ?? []).map(r => (r.ownerAttributionVerified === true ? 'owner 출처가 확인된 적용 보고 · ' : '등록된 보고·owner 출처 미확인 · ') + clean(r.text) + ' · ' + cite(r))]),
    '실제 읽기 도구가 반환한 매뉴얼/인계:', ...(l.manuals ?? []).slice(0, 9).map(m => clean(m.path) + ' · ' + cite(m)),
    '재사용 도구·fixture:', ...(l.reuse ?? []).slice(0, 8).map(r => clean(r.path) + ' · ' + cite(r)), clean(l.searchScope), ...(l.remaining ?? [])];
  const a = state[state.demoProductionRouting?.selectedApproval] ?? {};
  return ['표시용 확장/CLI 세션 · ' + clean(sessionId) + ' · engine ' + clean(viewerVersion.version), '관측 ' + time(observedAt), limits(usage), 'context ' + (Number.isFinite(usage.context?.percent) ? usage.context.percent.toFixed(0) + '%' : '확인 못 함'),
    '백그라운드 제작 세션 · ' + clean(u.sessionId), '관측 ' + time(u.observedAt), limits(u), '서버 reset ' + time(u.resetAt) + ' / 로컬 예약 ' + time(u.resumeAt),
    '이전 예약 등록 기록 ' + clean(u.scheduledTask?.registered) + ' / ' + time(u.scheduledTask?.runAt), ...(u.scheduledTaskObservations ?? []).flatMap(t=>['실제 스케줄러 관측 ' + time(t.observedAt) + ' · ' + clean(t.task) + ' · ' + clean(t.state), '마지막 실행 ' + time(t.lastRun) + ' · 결과 ' + clean(t.lastResult) + ' · 다음 ' + time(t.nextRun)]), clean(u.note), 'Codex 구독 잔량: 확인 못 함; 연결된 보고 없음',
    'POLICY: ' + (state._console?.budgetPolicy?.mode === 'adaptive' ? 'ADAPTIVE / lifetime usage not aggregated when missing' : clean(state._console?.budgetPolicy?.mode)),
    'LEGACY TICKET ROWS: ' + clean(JSON.stringify(a.ticketStatus)), 'historical D2: ' + clean(a.ticketStatus?.D2) + ' / ' + clean(JSON.stringify(a.results?.D2?.approvedTotalBudget)), '기록 누락은 0이 아니며 reset으로 누적 시도를 초기화하지 않음.'];
}
export function register(on) {
  on('session.start', async ($, e, next) => {
    for (const [name, description] of [['pa', 'P.A. 제작 화면'], ['pa-status', '모델 호출 없이 실제 상태 조회'], ['pa-resume', '현재 owner 정상 helper 재개 요청']]) await $.command.register({ name, description, immediate: true });
    await refresh($); autoOpenStatus = '명시적 /pa 명령으로만 열기'; if (timer) timer.cancel(); timer = $.clock.every(10000, async () => { if (opened) await refresh($); }); return next(e);
  });
  on('command.run', { command: 'pa' }, async ($) => { await openPane($); return {}; });
  on('command.run', { command: 'pa-status' }, async ($) => { await refresh($); return { text: [...summary(), error].filter(Boolean).join('\n') }; });
  on('command.run', { command: 'pa-resume' }, async ($) => { await controlProduction($, 'resume'); return { text: notice }; });
  on('ui.close', { id: PANE }, async ($, e, next) => { ++openGeneration; opened = false; if (timer) { timer.cancel(); timer = null; } autoOpenStatus = '사용자가 닫음 · 자동 재열기 안 함'; return next(e); });
  on('session.measure', async ($, e, next) => { if (!e.agentId) await refresh($); return next(e); });
  on('ui.render', { component: 'AbovePrompt' }, async ($, e, next) => {
    const {Box, Button} = $.ui.resolve(e);
    const previous = await next(e);
    const button = Button({key:'pa-open-production', label:'P.A. 제작 · ' + MOD_VERSION, onPress:async()=>{await openPane($);}});
    return Box({flexDirection:'column', children:[...(previous ? [previous] : []), button]});
  });
  on('ui.render', { component: 'Pane' }, async ($, e, next) => {
    if (e.requestId !== PANE) return next(e);
    const { Box, Text, Button, Input } = $.ui.resolve(e);
    const allRows = rows();
    const paged = section === 'loop' || section === 'bridge';
    const nav = Object.entries(tabs).map(([key, label]) => Button({ key: 'tab-' + key, label, onPress: async () => { section = key; offset = 0; $.ui.invalidate('ui.render'); } }));
    const children = [Text({ children: ['P.A. ' + MOD_VERSION + ' · ' + tabs[section] + (error ? '\n조회 실패 · 이전 관측 보존: ' + error : '')] }),
      Box({ flexDirection: 'row', children: nav.slice(0, 4) }), Box({ flexDirection: 'row', children: nav.slice(4) }),
      Box({flexDirection:'row', children:[Button({key:'pa-resume-production',label:controlling?'요청 중':'기존 제작 재개',onPress:async()=>{await controlProduction($,'resume');}}),Button({key:'pa-stop-production',label:'안전한 중지 요청',onPress:async()=>{await controlProduction($,'safe-stop');}})]})];
    if (section === 'instructions') children.push(Input({ key: 'pa-instruction', label: submitting ? '저장·전달 처리 중' : '제작 Claude에게 지시', placeholder: '기존 승인 범위의 다음 실제 작업을 입력', value: draft, submitLabel: '저장·전달', onInput: value => { draft = value; }, onSubmit: async value => { await submitInstruction($, value); } }));
    if (section === 'evidence') children.push(Box({ flexDirection: 'row', children: [['all','전체'],['image','전후 화면'],['video','영상'],['document','검증·문서'],['build','실행 빌드 폴더']].map(([key,label])=>Button({key:'media-'+key,label,onPress:async()=>{media=key;offset=0;$.ui.invalidate('ui.render');}})) }));
    children.push(Text({children:[(paged ? allRows.slice(offset,offset+12) : allRows).join('\n')]}));
    if (section === 'evidence') for (const [i, a] of evidence().slice(offset, offset + 5).entries()) if (a.openable) children.push(Button({ key: 'open-' + i, label: '열기 · ' + a.path.split('/').pop(), onPress: async () => { const r = await $.process.run([PYTHON, '-B', '-X', 'utf8', SCRIPT, '--root', ROOT, '--open-evidence', a.path], { cwd: ROOT, timeoutMs: 8000 }); notice = r.exitCode === 0 ? '열기 요청: ' + a.path : '열기 실패 · ' + clean(r.stderr); $.ui.invalidate('ui.render'); } }));
    const count = section === 'instructions' ? (obs().instructions?.items ?? obs().instructions ?? []).length : section === 'team' ? (obs().team?.length ?? 0) : section === 'evidence' ? evidence().length : paged ? allRows.length : 0, size = section === 'team' || section === 'instructions' ? 3 : paged ? 12 : 5;
    if (count > size) children.push(Box({ flexDirection: 'row', children: [Button({ key: 'previous', label: '이전', onPress: async () => { offset = Math.max(0, offset - size); $.ui.invalidate('ui.render'); } }), Text({ children: [(offset + 1) + '–' + Math.min(offset + size, count) + ' / ' + count] }), Button({ key: 'next', label: '다음', onPress: async () => { if (offset + size < count) offset += size; $.ui.invalidate('ui.render'); } })] }));
    if (notice) children.push(Text({ children: [notice] }));
    children.push(Button({ key: 'pa-refresh', label: '새로고침 · 모델 호출 없음', onPress: async () => { await refresh($); } }), Button({ key: 'pa-copy-resume', label: '현재 근거 복사', onPress: async () => { await $.ui.copy(resume()); } }));
    return Box({ flexDirection: 'column', children });
  });
}
