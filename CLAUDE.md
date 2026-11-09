# Project P.A. Claude Code

2026-10-19 현재 운영 정책: 기존 Canon의 전체 게임 목표와 현재 실행 묶음의 FINAL_WINDOWS_HUMAN_REVIEW를 구분한다. 승인 목표 안의 구현·세부 설계·복구·검증은 자율 진행하며 단계마다 사용자 재승인을 요구하지 않는다. 파일 수·작업 총시간·compile/Play/validator 횟수·기계적 연속 실패·옛 티켓/모델/담당자 배정을 실행 상한으로 쓰지 않는다. 파일 소유 범위는 충돌 확인 후 기존 registry에서 조정하고, 순서는 최신 checkpoint와 품질 우선순위를 따른다. 시도·사용량·실패·거절·승인 이력은 누적 보존한다. 같은 행동을 진전 없이 반복하면 원인·바꾼 접근·새 근거를 checkpoint에 남긴 뒤 복구하고, 실행 상태가 모호하면 확인 전 재실행하지 않는다. 실제 출력/소유 파일 활동이 끊긴 프로세스의 inactivity timeout은 유지하며 작업 총시간 상한과 분리한다. 120초 복구 스냅샷·20분 안전 인계는 재개 가능한 복구 주기다. 실제 실행 정책은 Automation/LoopEngineering/loop-policy.json의 recoveryPolicy를 따른다. 사용자 dirty·Save 호환성·기존 권위/디자인 조건·현재 반려/보류·단일 Unity writer·미승인 결제/외부 공개 금지·최종 사람 검수는 유지한다. 과거 고정 담당자 문구보다 현재 유효한 state/lease와 검증된 인수인계가 우선하며, 이번 운영 정리는 제작 재개나 writer 이전 승인이 아니다.

2026-10-10 최신 참고 정책: `References/Dinkum/` 원본16장과 실제 웹 PC 영상 구간을 사용한다. 설치된 딩컴은 오류 때문에 실행하지 말라는 사용자 지시가 우선한다. 로컬 딩컴 실행·입력·모드 수정 금지. 동작은 실제 영상 시계열 근거와 한계를 구분하며 정지 이미지/자막만으로 검증하지 않는다.

`AGENTS.md`가 공통 제작 계약이다. 동일 지침을 다시 기록하거나 Task Observer를 자동 호출하지 않는다.

레퍼런스 연구의 공통 정책은 AGENTS.md를 따른다. 현재 작업의 연구 입구는 `Docs/01_GAME_DESIGN/ReferenceResearch/README.md`다. 제작·비평·수정·검증 작업자를 배정할 때 최신 색인과 관련 원문을 전달하고 선택적으로 읽게 한다. 연구 팀의 가설은 Canon이나 현재 구현/검증의 대체물이 아니다.

2026-10-09 최신 사용자 지시에 따라 이 원본 폴더의 Claude Code는 제작 총괄과 Unity 통합 담당이다. 기존 `loop-state.json`의 단일 소유권과 최신 `Docs/00_CURRENT/CODEX_HANDOFF.md`를 인수한다. Codex는 검토·독립 지원이다. 완료한 환경 설정과 기존 발표 가능한 플레이를 재사용하며 섬·바다·식생 품질부터 이어간다.

제작·독립 비평·수정·검증 역할을 실제 하위 세션으로 운영한다. 기존 `Invoke-ProjectPAProduction.py worker --role builder|critic|reviser|verifier`가 현재 HEAD와 dirty snapshot을 반영한 격리 worktree에서 Claude CLI를 시작하고 진행·결과·오류·세션 ID를 수집한다. 하위 작업자는 할당 파일만 수정하며 원본 Unity/세이브/씬에 쓰지 않는다. 런처의 `--strict-mcp-config`는 하위 작업자의 원본 Unity MCP를 제외한다. 총괄만 diff 검토 후 통합·실제 Unity 검증한다.

2026-10-11 최신 사용자 지시: 기한 안의 데모 품질을 우선하며 Claude·Codex 팀이 결과를 이어받는 릴레이로 운영한다. 같은 과제의 경쟁 복제나 점수판을 만들지 않는다. Claude는 총괄·원본 Unity 통합을 유지하고 Codex는 격리 코드 구현·독립 검토를 지원한다. helper의 `worker --provider codex`는 동일 registry/파일 예약/현재 HEAD+dirty worktree/검토 경로를 사용하며 빈 MCP, 플러그인·훅 제외, workspace-write를 요청한다. 실제 runtime 권한·모델·effort와 계정 한도를 확인하고 제한을 우회하지 않는다. 사용량 대기 중 다른 팀이 독립 작업을 진행하고 총괄 재개 시 먼저 실제 결과를 인수한다.

재개 시 productionEnvironment.workers의 실제 assigned/running/ready_for_review 소유 파일과 최신 Handoff/checkpoint를 대조한다. 아직 실행 중인 작업을 덮어쓰거나 중복 배정하지 않는다. 이미 완료한 후보는 status/HANDOFF/review와 원본 hash/HEAD를 확인해 인수하고 재구현하지 않는다. 실제 GameView·입력·동일 조건 비교 및 성능 증거로 검증하며 source 검토만으로 화면/FPS 개선을 주장하지 않는다. 과거 World-dune-relay/v9b 후보나 오래된 SID를 현재 작업으로 고정하지 않는다. 실패 작업의 부분 결과를 보존하고 미해결 월드→동작→소품→UI→전체 조화→Windows 후보로 이어간다.

기존 standalone Codex exec CLI read-only 실패는 보존한다. 10/11 설치 CLI의 공식 App Server 경로에서 실제 workspace-write 파일 생성·읽기가 성공했고, 격리 critic의 실제 읽기·독립 검토도 완료했다. helper `worker --provider auto`/`codex`는 검증된 App Server를 사용한다. 설정의 빈 mcp_servers 표는 상속 서버를 제거하지 않으므로 실제 config/read의 각 서버 enabled=false를 확인한 뒤 작업을 시작한다. 원본 Unity 연결은 현재 state/lease의 단일 writer에게만 유지한다.

Serena를 사용할 경우 이 작업 폴더를 가리켜야 한다. 없거나 실패하면 rg/Read로 계속한다. 결과는 목표·변경 파일·실행한 검증·미해결·다음 행동을 짧게 반환한다. 총괄의 diff 검토와 원본 Unity 검증 전 완료로 기록하지 않는다. Codex 검토는 독립 지원이며 제작의 필수 대기 조건이 아니다.

2026-11-09 도구 재설계(`Docs/00_CURRENT/TOOLING_DESIGN_2026-11-09.md`): 이 프로젝트 세션은 pa-production-console·humanize-korean(대사·문서 한국어 윤문)만 켜고 claude-mem/ECC/Figma/github/claude-code-setup/Task Observer는 끈다. 전체 도구가 필요하면 CLI `claude --settings .claude/profiles/full-tooling.settings.json`으로 일시 사용한다. Unity MCP 진입점은 프로젝트 `.mcp.json`의 `unityMCP`(HTTP) 하나다. Python은 `C:/Users/sdjsd/AppData/Local/Programs/Python/Python312/python.exe`를 쓴다(PATH의 기본 python에는 PIL 없음). /goal과 프로젝트 전용 production Mods만 제작 런처에서 사용하며 하위 작업자는 훅 비활성화를 유지한다. 기존 기억·관찰 기록은 보존한다. 문서 작성이나 관찰이 목적이면 별도 명시 호출로 사용한다. 사용량/계정 차단 시 현재 diff와 세션 ID/오류를 보존하며 계정 제한을 우회하지 않는다.

기본 진입은 `Tools/LoopEngineering/Start-ProjectPAClaudeDemo.ps1`이다. 시작 지시문은 `Tools/LoopEngineering/PRODUCTION_ENTRY.md`이며 별도 상태 문서가 아니다. 데모 제작 세부는 `.agents/skills/pa-demo-factory/SKILL.md`를 필요할 때 읽는다. 기존 중단 작업자를 재사용할 때는 성공 여부와 최신 원본 해시를 확인하고 미완료 결과를 PASS로 바꾸지 않는다.

자동 인계는 기존 supervisor/loop-state만 사용한다. near limit 90% 또는 context 85%에서 현재 원자 작업 후 인계를 요청하며, 정확한 quota가 없어도 120초 owned-source 복구 스냅샷/20분 안전 checkpoint를 남긴다. 스냅샷은 검증 PASS가 아니다. 부분 작업은 같은 worktree·dirty baseline·기존 transcript와 함께 보존하고 검증된 여유가 있는 팀이 이어받는다. 다른 팀의 ready_for_review를 다시 구현하지 않는다. 둘 다 제한이면 실제 관측으로 재확인하고 기존 supervisor가 재개한다. Claude context가 높은 안전 checkpoint는 prior SID를 보존한 새 세션으로 이어간다. Unity/Save/HEAD/소유권 충돌과 최종 사람 검수는 유지한다.

2026-10-11 인수 이력: 사용자 명시 요청으로 Codex가 중단된 Claude 제작을 인수했다. 기존 coordinatorLease/demoProductionRouting의 codex 소유권 동안 원본 Unity/원본 제작 파일을 수정하지 않는다. 이전 transcript/worker 결과는 보존돼 있으며 다시 소유권이 Claude로 넘어왔을 때 최신 Handoff에서 이어간다. 격리 작업자는 기존 strict MCP 경로를 유지한다.

2026-10-12 사용자 PowerShell 인계 이력: Codex가 실제 Unity safe point를 확인하고 원본 소유권을 Claude에 반환했다. 기존 총괄 SID `3f66c90c-d5eb-4a51-9c99-44d7744d2310`와 최신 CODEX_HANDOFF.md를 함께 인수한다. v10/v12/v14b와 windup/분수 수정·Windows 검증을 반복하지 않는다. v15 wet-band는 동일3뷰/독립 화면 검수로 유지 권고, 얇은 수면 테두리·야간은 미완료다. canopy는 원본 통합/compile/한 타격 영상만 확인했으며 전체 벌목과 새 P3/Windows는 미검증이다. 별도 idle 캡처를 모션 PASS로 쓰지 않는다. 현재 `ready_for_interactive_start`는 사용자가 CLI를 실행하기 전 대기이며 background 제작이 아니다.

환경 설치·진단·구독 릴레이·작업 패널 등 운영 도구를 변경할 때만 `Tools/PaAgentConsole/SESSION_RULES.md`를 읽는다. 환경 준비는 게임 제작 재개 승인이 아니며 현재 loop-state/Handoff가 패키지의 과거 D2 예시보다 우선한다.
