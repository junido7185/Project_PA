Project P.A. — AI 에이전트 입구

Project P.A.는 **"낮에는 동물 마을을 만들고, 밤에는 그 마을의 유일한 잡화점을 운영하는 3D 코지 라이프 시뮬레이션"**이다. 핵심 차별점: "내가 판 물건이 마을의 풍경과 주민 생활을 바꾼다."

개발 목표는 단순 프로토타입이 아니라 완성 게임까지 이어지는 개발이다. 졸업 시연은 중간 마일스톤일 뿐이며, 모든 작업은 완성 게임의 지반이 된다는 전제로 수행한다.

작업 가능 경로는 C:\Users\sdjsd\Desktop\Unity\Project_PA 뿐이다. Project_D(참조 프로토타입)는 읽기 전용 — 수정·복사·병합 금지.

매번 읽어야 할 문서 (순서대로)

Docs/00_CURRENT/CURRENT_STATE.md

Docs/00_CURRENT/CAPABILITY_REGISTRY.md

Docs/00_CURRENT/GAME_LOOP_MAP.md

Docs/00_CURRENT/INTEGRATION_QUEUE.md

작업 절차는 AI_WORKFLOW/00_START_HERE/ONE_PAGE_WORKFLOW.md, 작업자·Unity·슬롭 방지 규칙은 AI_WORKFLOW/02_AGENT_RULES/를 따른다. 추가 Canon·구현·검증 문서는 AI_WORKFLOW/00_START_HERE/DOCS_INDEX.md §3에서 해당 작업에 필요한 것만 읽는다. Handoff는 재개 절차이며 상태를 중복 보관하지 않는다.

Vertical Slice Studio Mode — 플레이어 화면 제작 예외 규칙

VERTICAL SLICE STUDIO MODE, STUDIO MODE, Playable Island Rebase, GameView polish pass, Character feel pass, Presentation pass 중 하나가 사용자 지시나 승인된 작업 목표에 명시되면 player-facing 3D 제작 작업에 한해 이 섹션을 활성화한다.

활성화 시 추가로 다음 문서를 읽는다.

AI_WORKFLOW/02_AGENT_RULES/VERTICAL_SLICE_STUDIO_MODE.md

AI_WORKFLOW/08_ASSET_CONTEXT/PROJECT_PA_ASSET_MANIFEST.md

.agents/skills/project-pa-vertical-slice/SKILL.md — Codex skill 로딩이 지원되는 환경에서 사용

Docs/99_ARCHIVE/AI_STUDIO/PROJECT_PA_AI_STUDIO_PACK.md는 백업/확인용 통합본이다. 정상 작업 중 다시 읽지 않는다. 활성 문서와 내용이 중복되므로 컨텍스트에 동시에 넣지 않는다.

Studio Mode에서 한 작업의 의미

기본 규칙의 "한 번에 한 작업"은 유지한다. 단, Studio Mode에서는 작업 단위를 파일 1개/기능 1개가 아니라 플레이어 경험 1개로 정의한다.

예: 도착 → 실제 섬 → 이동/점프 → 카메라 → Hotbar → 환경 드레싱이 하나의 승인된 Playable Island Rebase 경험이라면, 그 경험을 완성하기 위해 직접 필요한 runtime code / scene / prefab / material / animator / UI / audio·VFX binding / editor utility를 함께 수정할 수 있다.

동일 경험과 직접 관계없는 기능을 "하는 김에" 추가하는 것은 금지한다.

기존 권위(PlayerController, CameraController, Inventory, Hotbar, EconomyService, ShopSlot, PurchaseEvaluator, WorldGridService, WorldBuildingPlacementService, WorldPersistenceService, SaveManager 등)는 재사용·확장한다.

두 번째 Player/Inventory/Hotbar/Economy/Shop/World/Placement/Save 권위를 만들지 않는다.

Studio Mode가 기본 규칙보다 우선하는 범위

아래 항목은 Studio Mode 문서가 기본 bounded-ticket 규칙보다 우선한다.

Player-facing scene/prefab 편집: 승인된 플레이 경험을 완성하기 위해 직접 필요하면 허용한다. 대규모 YAML/바이너리 직접 편집, 이름만 보고 삭제, serialized reference 일괄 치환은 계속 금지한다.

WORLD 작업 대상: 실제 데모가 Demo256/실제 플레이 경로를 대상으로 하는 Studio 작업이면 WorldSandbox만 수정해야 한다는 기본값을 적용하지 않는다. 다만 Prototype_FirstDay.unity Golden Regression 경로는 작업 목표가 명시적으로 그 씬을 대상으로 하지 않는 한 보호한다.

기계적 실패: 첫 compile/Play/reference/input/material/animator/collider/NavMesh/validator 실패에서 멈추지 않는다. 동일 경험 범위 안에서 Studio Mode의 self-repair budget을 사용해 수정·재검증한다.

수정 파일 수: 직접 관련된 파일 수가 늘어났다는 이유만으로 중단하지 않는다. 대신 새로운 권위/대형 리팩터/무관 범위 확장은 금지한다.

문서 작업: 제작 중간에 CHANGELOG/Handoff/상태 문서를 반복 갱신하지 않는다. 실제 제작과 검증이 끝난 뒤 현재 문서와 개발 로그의 필요한 부분만 한 번 갱신한다.

완료 판정: compile/validator PASS만으로 완료 선언하지 않는다. 실제 GameView와 실제 플레이 경로가 제품 장면으로 보이는지 확인한다.

Studio Mode 자체 복구 예산

동일한 승인 경험 안에서 기본적으로 다음까지 스스로 복구한다.

compile/fix cycle 최대 3회

targeted Play 최대 3회

전용 validator 최대 1개

직접 필요한 소규모 glue/runtime/presentation 파일 추가 허용

다음은 Studio Mode에서도 즉시 HARD STOP이다.

destructive Git/file operation 필요

unrelated dirty work 덮어쓰기 위험

사용자 데이터/Save 손실 위험

확립된 KEEP 권위를 교체해야 함

incompatible SaveData migration 필요

대형 아키텍처 재작성 필요

승인되지 않은 외부 패키지/외부 에셋 다운로드 필요

복구 예산 이후에도 Unity/개발 환경 사용 불가

Studio Mode 시각 품질 게이트

Studio Mode의 제품은 GameView다.

최종 PASS에는 작업 범위에 맞는 실제 GameView 증거가 있어야 한다. 가능하면 60~90초 실제 플레이 캡처를 사용하고, 불가능하면 핵심 상태의 실제 GameView 스크린샷을 남긴다.

다음을 자동 검사 숫자로 대체하지 않는다.

카메라 구도와 가림

캐릭터 화면 점유율과 가독성

실제 모델/애니메이션 사용

Hotbar/도구 선택의 시각적 이해

숲/해안/고지대/초원의 공간 정체성

월드 밀도와 랜드마크

타격/채집/배치/판매 피드백

Debug UI/label/primitive 노출

전체 아트 스케일·색·재질 일관성

실제 GameView가 validator scene, debug sandbox, 빈 절차 생성 평면처럼 보이면 기능 테스트가 모두 통과해도 Studio Mode 작업은 아직 완료가 아니다.

Asset-first 규칙

Player-facing 자산이 필요하면 새 primitive를 만들기 전에 PROJECT_PA_ASSET_MANIFEST.md와 실제 로컬 AssetDatabase를 먼저 조사한다.

우선순위:

Assets/Art/ProjectPA

Assets/Art/Character

Assets/Art/External/Quaternius

Assets/Art/External/Kenney

Assets/Art/Ultimate Nature Pack - Jun 2019

Assets/Art/Market, Assets/Art/UI, Assets/Models

visible primitive/debug placeholder

로컬 .gitignore/미커밋 자산이 있으면 로컬이 최종 권위다. GitHub 목록만 보고 자산 부재를 단정하지 않는다.

절대 규칙

해석 우선순위: Studio Mode가 명시적으로 활성화된 player-facing 작업은 위 Vertical Slice Studio Mode — 플레이어 화면 제작 예외 규칙에 따라 아래 규칙의 작업 단위·씬 대상·retry 범위를 해석한다. 저장/경제/데이터 마이그레이션 등 core 작업에는 예외를 적용하지 않는다.

한 번에 한 작업만 수행한다.

코드 수정 전에 수정 예정 파일 목록을 먼저 보고한다.

전체 시스템 재작성 금지. 기존 작동 시스템(Shop/EconomyService/PurchaseEvaluator/NpcController/SaveManager 등)을 갈아엎지 않는다.

Unity 씬/프리팹/저장 시스템 임의 변경 금지.

메인 씬 경로(Assets/Scenes/Prototype_FirstDay.unity)를 임의로 덮어쓰지 않는다. 새 씬이 필요해도 사용자 승인 없이 기존 메인 씬을 덮어쓰지 않는다.

WORLD 씬 분리 규칙: Prototype_FirstDay.unity는 Golden Regression Scene이며 신규 WorldGrid/Chunk/Terraforming 실험 대상으로 사용하지 않는다. WORLD 티켓은 별도 명시가 없으면 승인된 Assets/Scenes/WorldSandbox.unity를 대상으로 한다. MainGame.unity 통합은 Gate 1~5와 별도 사람 승인 전 금지한다.

WORLD 씬 편집 규칙: 후속 씬 생성·변경은 editor builder/setup utility와 validator를 우선하며 대규모 YAML/바이너리 직접 편집, 이름만 보고 오브젝트 삭제, serialized reference 일괄 교체를 금지한다.

컴파일/테스트 없이 완료 선언 금지. 검증 기준: AI_WORKFLOW/04_VERIFICATION/VERIFICATION_RULES.md. 확인 못 한 항목은 반드시 "확인 못 함"으로 보고.

실패 시 AI_WORKFLOW/05_LOGS/BUG_LOG.md에 기록하고 멈춘다. 같은 원인 2회 실패 후 세 번째 시도 금지.

외부 패키지 추가 금지. git push 금지. 승인 없는 커밋 금지. 파괴적 Git/파일 명령 금지. 파일 삭제 금지.

Docs/01~06, 08 번호 문서는 코드 § 인용을 위해 경로·섹션을 유지한다. 07_개발일지.md는 월별 이력의 호환 입구다. 루트 최신 PROJECT_PA_CRASH_REPORT_*.md는 이동 금지 (preflight glob).

Bounded Ticket 연속 진행 정책

기본값은 bounded ticket 하나를 완료한 뒤 사람의 다음 명시적 지시를 기다리는 것이다.

예외 PREAPPROVED_MILESTONE_CONTINUATION: 사용자가 milestone과 ticket sequence를 명시적으로 선승인하고 그 범위가 Automation/LoopEngineering/State/loop-state.json에 기록돼 있으면, 승인된 다음 ticket은 추가 메시지 없이 자동 활성화한다.

예외 중에도 동시에 하나의 ticket만 활성화하고, ticket별 compile/validator/diff 검증과 승인된 local commit을 유지한다. HARD BLOCKER와 컨텍스트 pause 규칙도 유지한다.

승인된 sequence 밖, preapprovedThrough 다음 ticket, stopAtMilestone 도달 뒤에는 자동 진행하지 않는다. 같은 blocker 설명은 한 번만 기록하고 반복 출력하지 않는다.

noAutoCommit은 기본 안전값이다. 선승인 record에 localTicketCommitsAllowed=true가 명시된 sequence 안에서만 ticket별 local commit이 허용되며 push는 항상 별도 사람 작업이다.

작업 종료 시

작업 결과는 Docs/04_DEVELOPMENT_LOG/YYYY-MM.md에 날짜·티켓·변경·검증·미해결 사항을 한 번 기록한다. 현재 상태가 바뀌면 Docs/00_CURRENT/CURRENT_STATE.md, 기능 계약은 CAPABILITY_REGISTRY.md, 실제 연결은 GAME_LOOP_MAP.md, 다음 우선순위는 INTEGRATION_QUEUE.md의 해당 부분만 갱신한다. 완료 티켓과 긴 이력을 현재 문서에 누적하지 않는다. CHANGELOG·handoff·구 개발일지에 결과를 복제하지 않는다.

구버전 상세 가이드(2026-06-26)는 Docs/99_ARCHIVE/OldWorkflow/AGENTS_v1_20260626.md에 보존되어 있다.

Codex Resume / Handoff Protocol

Docs/00_CURRENT/CODEX_HANDOFF.md is the authoritative resume checkpoint
for the currently active bounded task.

When starting or resuming work:

Read AGENTS.md.

Read Docs/00_CURRENT/CODEX_HANDOFF.md.

Read only the architecture/implementation authority explicitly referenced
by the handoff, normally:
Docs/02_IMPLEMENTATION/PROJECT_PA_CODE_REUSE_MAP_v1.md.

Inspect the current git diff only for files relevant to the active task.

Continue from Exact Next Action.

Do not repeat completed investigation, repository-wide inspection,
validation, or reasoning unless the handoff explicitly says it is required.

Do not treat chat history as the primary project state.
The repository, current git diff, authoritative docs, and CODEX_HANDOFF.md
are the source of truth.

Handoff Update Rule

Update Docs/00_CURRENT/CODEX_HANDOFF.md once when a bounded work session stops.

Update it when:

the ticket reaches PASS;

the ticket reaches FAIL/BLOCKED/INCOMPLETE;

work must stop because of usage/time/context limits;

a discovered dependency requires a new approval before proceeding.

Do not repeatedly update it during normal implementation.

Keep the handoff concise and overwrite the previous active state.
Do not append historical work logs.

The handoff should contain only:

current task;

status;

approved baseline / branch when relevant;

completed work;

current blocker;

approved files;

forbidden files when important;

validation state;

exact next action;

commit/push state.

Before stopping because of usage or context limits, prefer updating
CODEX_HANDOFF.md over producing a long conversational summary.

Failure / Retry Policy

Studio Mode override: Vertical Slice Studio Mode가 활성화된 player-facing 작업에서는 위 Studio Mode 자체 복구 예산과 HARD STOP 규칙이 이 기본 retry 정책보다 우선한다. Studio Mode가 활성화되지 않은 작업에는 아래 기본 정책을 그대로 적용한다.

STOP immediately when:

an architecture assumption is invalid;

the required scope exceeds approved files or systems;

an authoritative KEEP system would require redesign;

destructive Git/file operations would be required;

the same blocker occurs twice;

continuing would violate the Reuse Map or current handoff.

A single local mechanical failure may be corrected once without ending
the bounded task when ALL of the following are true:

the cause is unambiguous;

the correction stays inside already approved files/symbols;

no architecture or gameplay behavior decision changes;

no destructive operation is required.

Examples of allowed one-time mechanical correction:

stale renamed variable;

missing using;

obvious compile typo;

validation script encoding/path typo;

incorrect local test command argument.

After that one correction, rerun only the failed check.

If it fails again, record the blocker in CODEX_HANDOFF.md and STOP