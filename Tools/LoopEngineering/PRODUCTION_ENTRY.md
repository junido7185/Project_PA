2026-10-12 최신 대화형 운용: 원본 Unity는 Claude 소유다. 기존 총괄 SID `3f66c90c-d5eb-4a51-9c99-44d7744d2310`는 Python helper `start --owner claude-code --resume-session <SID> --prompt-file Docs/00_CURRENT/CODEX_HANDOFF.md`로 인수한다. 런처는 지원되는 auto permission mode와 production Mods를 함께 로드한다. 이전 interactive는 Mods 없이 dontAsk로 시작돼 사용량 관측/후보 실행이 차단됐으므로 idle에서 /exit 후 같은 SID를 한 번 재시작한다. 별도 총괄/Unity 소유자를 추가하지 않는다. 최신 구현/검증/다음 행동은 CODEX_HANDOFF.md가 기준이며 아래 v9b는 과거 이력이다. background supervisor는 interactive와 별개다.

레퍼런스 연구: `Docs/01_GAME_DESIGN/ReferenceResearch/README.md`에서 현재 게임플레이·NPC·월드·UI·상점 작업에 필요한 연구만 선택한다. 공식 Canon·현행 코드·실제 검증이 우선이며 연구 관찰/측정, 해석/가설/미확인을 구분한다. 연구 가설로 시스템을 임의 교체하거나 연구 검수 시나리오를 실행 PASS로 올리지 않는다. worker가 최신 Markdown/상대 링크를 Evidence에 제공하지만 전체 열람을 요구하지 않으며 새 연구는 다음 배정에 자동 반영된다.

승인된 Windows 후보 실행은 임시 scratchpad 대신 기존 helper를 쓴다:
`python -B -X utf8 Tools/LoopEngineering/Invoke-ProjectPAProduction.py candidate --id <성공 build UUID> --save-root Logs/ParallelProduction/<QA>/save --output Logs/ParallelProduction/<QA> --label <새 label> --session-id <현재 총괄 SID>`
성공한 실제 빌드/프로젝트 QA save/로그 무덮어쓰기/현재 총괄 lease만 허용한다. 후보 PID를 native helper에 전달해 Continue→실제 입력→SaveQuit→새 프로세스 Continue와 성능을 검수한다. /pa-usage는 모델 요청 없이 실제 engine usage를 조회한다. empty/stale는 미확인이지 소진/회복 확정이 아니다. worker auto는 그 경우 검증된 Claude의 읽기 전용 critic/verifier 가용성 probe(같은 inactivity watchdog 적용)로 실제 가용성을 확인하며 fresh exhaustion/중지/진짜 최종 검수는 유지한다.

2026-10-11 자동 제작 인수: 기본 런처 한 번으로 같은 Claude 총괄 세션의 /goal 제작을 감독한다. 기존 loop-state productionEnvironment.autonomous가 제어 상태이며 별도 스케줄러/상태 체계는 없다. Mods는 session.usage의 실제 plan limits와 회복 시각을 기존 helper에 전달한다. API usage_limit_reached/rejected 사건일 때만 실제 미래 reset 이후 재개하며, 시간 추정·모델/계정 전환·추가 과금으로 우회하지 않는다. 인증/도구 오류·미확인 reset·사용자 Pause·원본 HEAD/소유권 충돌·진전 없는 checkpoint·최종 사람 검수에서는 보존하고 멈춘다. /goal의 모델 판정은 게임 품질 완료가 아니다.

최신 독립 대표 구역 판정은 v9b PARTIAL/YES-WITH-LIMITS다 (`Logs/ParallelProduction/World-v9-verifier-20261011/HANDOFF.md`). 온전한 나무·canopy·foam은 개선됐으므로 다시 구현하지 않는다. 남은 문제는 낮은 대비의 땅 패치, 성긴 tuft·빈 오른쪽과 먼 해변, coast kerb/glass rim과 불균일 foam이다. 같은 09:21/1920×1080 기본 카메라의 arrival/coast/north shore와 실제 보행 영상을 재촬영해 새 독립 검수를 거친 뒤 관련 공간으로 확장한다. v9b CPU 표본은 floating Game 창 조건이 달라 비교 불가하며, contact sheet 앞부분은 Serena 화면·MP4 직접 검토는 실패했다. night/수중 밀도/성능/정상 영상과 도구 베기는 미검증이며 저장·Windows 후보도 남아 있다. 기존 나무 collider 결함과 새 해안 나무의 실제 collider/NavMesh carving 영향을 보존하고 주민 경로를 검증한다.

작업자 evidence에는 비교 원본 PNG뿐 아니라 실제 MP4, ffprobe 및 정상속도 구간/시계열 프레임, native 입력 기록을 함께 --evidence로 전달한다. 이전 v8 verifier worktree에는 MP4가 빠져 독립 동작 검증은 안 됐다. 이미지 Read 횟수나 CLI0으로 모션 PASS를 만들지 않는다. 원본 나무/저장/이동 권위를 보존하고 충돌 변화의 경로 영향까지 검증한다.

주기적인 재개 가능한 안전 checkpoint 또는 실제 중단에서 최신 Handoff를 쓰고 다음을 실행한다(실제 session ID/evidence 사용):
`python -B -X utf8 Tools/LoopEngineering/Invoke-ProjectPAProduction.py autonomy checkpoint --session-id <현재ID> --disposition continue|blocked|human-review --next-action <정확한 다음 행동> --evidence <실제 검증 파일들>`
continue는 실제 코드/자산 또는 새 검증 증거의 진전이 있을 때만 다음 invocation을 허용한다. human-review는 최종 사람 확인 대기이며 완료 승인이 아니다. 사용자 Pause는 새 작업자/제작 시작 없이 현재 원자 작업만 마치고 /goal clear 후 보존한다. Windows가 잠겨 있으면 native 입력 금지, 가능한 독립 코드·검토는 계속하되 실제 화면 검증은 해제 후 한다. 상세 운용·복원: `Tools/LoopEngineering/mods/project-pa-production/README.md`.

2026-10-10 최신 사용자 지시: 설치된 딩컴을 실행하지 않습니다(반복 오류). 동작 참고는 웹의 실제 PC 플레이 영상만 사용합니다. 실제 영상 시계열 검토와 한계: `Logs/ParallelProduction/DinkumReference-20261010/WebMotion/MOTION_OBSERVATIONS.md`. local Dinkum 입력은 종료됐고 Unity native 입력 소유자는 Claude입니다. 이 지시가 아래의 로컬 딩컴 실행 안내보다 우선합니다.

Project P.A. 제작을 Claude Code 총괄·Unity 통합 담당으로 인수하고 실제 제작을 계속하세요.

2026-10-09 최신 사용자 참고 자료: `References/Dinkum/README.md`와 `images/` 원본16장을 실제 Read 이미지 도구로 확인하세요. 압축은 이미 프로젝트 루트에 풀었고 manifest 해시16개가 일치했습니다. 현재 참고는 사용자의 실제 PC 딩컴 캡처이며 world-first 제작에 우선합니다. 각 builder/critic/reviser/verifier는 helper의 `--reference-group world|motion|ui|all`로 README와 관련 원본 바이트를 Evidence에 자동 전달받습니다(기본 world). Critic/Builder가 파일명만 읽고 화면을 확인했다고 보고하지 않게 실제 Read 호출을 확인하세요. 해안·바다·식생·조명의 세 가지 큰 차이를 실제 셰이더/재질/자산·연결로 개선하고 같은 조건의 전후 GameView와 실제 보행 영상을 남기세요. 새 자산이 필요하면 승인된 표현 범위에서 제작·교체하며 코드 파일 수만으로 막지 마세요. 원본 참고 이미지는 게임 자산으로 복제하지 않습니다.

딩컴 동작·입력 참고는 웹의 실제 PC 플레이 영상으로 확인합니다. 설치된 딩컴은 실행하거나 조작하지 않습니다. 실제 도끼·채굴·보행·해안 시계열과 1x clips는 `Logs/ParallelProduction/DinkumReference-20261010/WebMotion/`에 있습니다. 정지 원본은 자세/크기만 증명하며 보행·타격 시간·입력 복귀를 증명하지 않습니다. 영상 구간 URL/시간대와 실제 관찰 범위·미검증을 구분하세요.

최신 사용자 지시: 기존 약 5분 플레이는 발표 가능한 상태입니다. 환경 설정을 재사용하고 섬·바다·식생 → 캐릭터·채집 동작 → 아이템·생활 소품 → UI → 전체 조화 순서로 품질을 높이세요. 처음에는 실제 참고 화면과 현재 GameView에서 큰 월드 품질 차이 세 가지를 골라 대표 구역을 개선하고 같은 조건의 전후 화면을 보여주세요. 기준이 잡히면 관련 공간 전체로 확장하고 다음 단계도 새 프롬프트를 기다리지 마세요. 기존 Day1과 승인된 D0~D12 범위를 재사용하며 다음 날·새 경제/저장 권위를 만들지 마세요.

AGENTS.md, CLAUDE.md, Docs/00_CURRENT/CODEX_HANDOFF.md를 한 번 읽고 현재 로컬 HEAD/dirty/실행 소유권을 확인하세요. 기존 loop-state.json의 demoProductionRouting.selectedApproval만 사용하세요. unityEditorOwner는 현재 state/lease를 따르며 비 owner는 검토·격리 지원입니다. 옛 D2 blocked_budget/횟수와 반려·보류·사용량은 이력으로 보존하고 성공으로 바꾸지 마세요. 환경 준비와 게임 품질 완성은 별개입니다.

기존 실제 작업자 경로를 사용하세요:

최신 사용자 지시는 경쟁 대신 릴레이입니다. Claude 총괄은 독립 구현·검토를 Codex에도 같은 helper의 `worker --provider codex --role builder|critic|reviser|verifier`로 배정할 수 있습니다. 먼저 active/ready 작업자의 파일 소유권을 읽고 직접 편집·중복 배정하지 마세요. Codex는 별도 HEAD+dirty worktree, 빈 MCP, 플러그인·훅 제외로 실행하고 실제 runtime 권한/모델/effort를 확인합니다. 각 팀의 결과를 이어받아 수정하고 Claude만 원본 Unity 통합·검증합니다. 사용량·권한 실패는 보존하며 작업자 CLI 종료를 게임 완료로 바꾸지 않습니다. source-baseline 대비 diff는 신규 파일과 사용자 기존 변경을 정확히 분리합니다. 매 결과 뒤 같은 일을 재구현하지 말고 미완료 부분을 이어가세요.

Codex exec CLI의 옛 read-only 실패는 보존하되, 검증된 공식 App Server 경로가 이제 격리 구현·읽기를 지원한다. `worker --provider auto`를 기본으로 사용하며 실제 사용 가능성과 capability를 모두 확인한다. App Server는 effective MCP 서버별 enabled=false를 검증한다. 원본 Unity 권한은 Claude에게만 유지한다.

`python -X utf8 Tools/LoopEngineering/Invoke-ProjectPAProduction.py worker --role builder --files <정확한 상대 코드 파일> --goal <자기 완결 작업> --effort high`

이 명령은 현재 HEAD 기반 sparse worktree에 실제 dirty snapshot을 반영하고 Claude CLI를 실행합니다. 원본 Unity MCP는 strict config에서 제외됩니다. `status --id <id>`가 진행·결과·오류·실제 모델·session ID를 반환합니다. `review --id <id>`의 diff와 HANDOFF.md를 읽고 원본 HEAD/해시/범위 충돌이 없을 때만 `review --id <id> --integrate --reviewed`로 통합하세요. 미완료·중단 작업은 성공으로 통합하지 말고 부분 결과를 읽어 필요한 작업만 새로 배정하세요. 하위 작업자는 Editor/원본 Assets/scene/.meta/세이브/공유 상태에 쓰지 않습니다. worktree는 OS 전체의 보안 샌드박스가 아닙니다. 자산·shader·prefab 작업은 총괄이 검토 가능한 Editor/API 경로로 통합하세요.

제작·독립 비평·수정·검증을 실제 다른 세션으로 운영하세요. 역할이 네 개라고 네 프로세스를 상시 켜지 마세요. 처음 두 독립 작업부터 시작하고 파일 예약과 실제 사용량에 따라 조절하세요. `--role critic|verifier --evidence <실제 PNG/로그 경로...>`는 새 worktree의 Evidence로 증거를 복사합니다. Critic에게 제작자의 해명·자기 점수는 넘기지 말고 실제 화면/정상 속도 영상/조작 기록을 읽게 하세요. 큰 결함은 최대 세 개, 먼저 고칠 문제는 하나로 좁히세요. Reviser는 그 결함만 고치고 Verifier는 별도 맥락에서 해결/미해결/미검증을 나누세요. CLI exit0만으로 Unity PASS를 선언하지 마세요.

현재 실제 Unity6000.3.2f1 D3D11 한 Editor와 Coplay HTTP MCP를 재사용하세요. 프로젝트 identity·Edit/compile/Console 상태를 먼저 확인하세요. Unity tools는 총괄에게만 연결돼 있습니다. 클라이언트 도구 목록이 아직 없으면 같은 helper의 `mcp --method tools/call --params-file <JSON 요청> --output <프로젝트 증거 파일>`을 사용하세요. 기존 scene/prefab/component/material/animation/camera/test/build/profiler 도구를 우선 사용하세요. `execute_code`는 compiler codedom이 동작하며 Roslyn은 없습니다. Play 중 직접 Camera.Render는 과거 native crash 원인이므로 실제 ScreenCapture/GameView를 사용하세요. AssetDatabase.Refresh는 비동기이므로 compile 완료·Console과 대상 DLL을 확인한 뒤 Play하세요.

실제 Windows 입력/캡처는 기존 helper를 재사용하세요:
`python -X utf8 Tools/LoopEngineering/Invoke-ProjectPAProduction.py native --pid <확인한 Editor PID> --game-view --log <증거 폴더> key W 1`
`... native ... click <Game View client x> <y>` / `shot <새 라벨>`
분리된 창 제목 Game을 대상으로 하며 원본 화면 좌표를 확인하세요. 실제 foreground를 얻지 못하면 입력은 보내지 않습니다. synthetic InputSystem 입력/teleport/time fixture는 명시하고 자연 플레이와 구분하세요. 개인 Save 대신 검증 전용 root를 유지하세요. 같은 열린 프로젝트에 batchmode/두 번째 Editor를 띄우지 마세요.

첫 월드 작업 근거는 Handoff에 있습니다. official Dinkum 이미지들은 비교용 Logs이며 Assets로 가져오지 않습니다. 우리의 시작 위치는 해안 모래이므로 비평의 '사막' 표현만으로 셀 ground type을 바꾸지 마세요. 실제 바다가 보이는 추가 기준 화면부터 확보하고 지면/절벽의 단색 면·물 경계/잔물결·식생 군락을 표현 계층에서 개선하세요. 셀·시드·저장 ID·충돌·NavMesh·배치/채집 권위를 유지하고 경사로를 추가하지 마세요. 대표 구역의 기본 카메라 전후·실제 보행/점프/채집/배치·성능을 확인한 뒤 범위를 넓히세요.

단순 작업은 medium/high, 복합 원인·모션·통합은 high/xhigh를 결과와 사용량으로 조절하세요. 실제 응답 모델/적용 effort를 기록하고 요청값만으로 최대 옵션이 적용됐다고 주장하지 마세요. 기존 full model claude-opus-5-5/high 실사용 근거를 재사용하세요. Task Observer/기억/한국어 윤문/관련 없는 플러그인과 반복 문서 작업은 제작 필수 단계가 아닙니다. Serena가 없어도 rg/Read로 계속하세요.

실제 계정/권한/저장 보호 경계에서는 결과와 정확한 다음 행동을 보존하세요. 승인된 구현·수정·검증마다 다시 확인을 기다리지 마세요. 완료 경계는 최신 Windows 후보·실제 플레이/격리 저장→정상 종료→새 프로세스 Continue·성능·남은 결함과 FINAL_WINDOWS_HUMAN_REVIEW입니다. 이관이나 부분 fixture PASS는 전체 게임 완료가 아닙니다. 자동 commit/push 없이 기존 상태·월별 로그·간결한 Handoff를 종료 시 한 번 갱신하세요.

최신 자동 인계 승인: 현재 목표·소유 파일·변경·실제 검증·미해결·정확한 다음 행동을 짧게 보존한다. 현재 helper의 relay status/observe/checkpoint/tick을 사용하고 별도 상태를 만들지 않는다. 총괄 Mods는 90% usage/85% context 및 20분 안전 checkpoint를 요청한다. 작업자/총괄 owned-source는 120초 복구 스냅샷을 남긴다. quota를 정확히 읽지 못하면 available로 추정하지 않는다. handoff_pending인 안전하고 충돌 없는 부분 결과만 실제 회복된 팀이 같은 worktree에서 이어받고, ready_for_review는 통합/검증으로 넘긴다. 과거 attempt/session/log는 유지하며 가용성만으로 Unity 소유권을 전환하지 않는다. 유효한 writerTransferAllowed와 검증된 adapter의 모든 안전 관측이 필요하다. `relay-snapshots`와 환경 증거는 게임 진행/품질 PASS를 대신하지 못한다.

## 2026-10-19 적용 운영 정책

loop-policy.json의 APPROVED_GOAL_PROGRESS_RECOVERY_20261019를 적용한다. 고정 파일 수/총시간/compile·Play·validator/실패 횟수로 중단하지 않는다. --timeout/TimeoutMinutes는 프로세스 출력 또는 소유 파일 활동이 끊긴 inactivity 검사용이며 진행 중인 작업은 전체 시간이 넘어도 계속한다. 스냅샷·quota poll 자체는 활동 근거가 아니다. 120초 복구 스냅샷/20분 안전 인계는 작업 상한이 아니며 같은 목표의 다음 세션이 기존 checkpoint에서 계속한다. 같은 실패를 변경 없이 재시도할 때는 원인·변경 접근·새 근거를 남기고, 모호한 in-flight는 확인 전 재생하지 않는다. 기존 사용자 반려·디자인 조건·Canon·dirty/Save·단일 writer·결제/공개·최종 수락 보호는 유지한다. 발송할 문안은 Tools/PaAgentConsole/startup/prompts/02_PRODUCTION_START.md이며 열람/복사는 제작 승인이 아니다.

작업 방식: 제작 전에 `.agents/skills/pa-demo-factory/SKILL.md`의 Select one experience·Toolbox·Implement, play, repair 절을 읽고 따른다(가장 앞선 미해결 경험 선택, 기존 도구 재사용과 유효 절차의 기존 위치 반영, 실제 입력·같은 조건 전후 화면·정상 속도 영상 증명, 원인 부분만 수정). 현재 `CODEX_HANDOFF.md`가 발표 빌드 기록이면 그 안의 이전 제작 Handoff 경로에서 미해결을 이어간다.
