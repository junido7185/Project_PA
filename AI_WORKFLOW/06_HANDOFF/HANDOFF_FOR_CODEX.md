# HANDOFF_FOR_CODEX — 다음 세션 인수인계

## 2026-09-09 — VS-PRESENT-001-P2 PASS / local checkpoint 후 STOP

- P1 recovery 첫 실행 PASS 및 6b283c1 local commit 후 명시 승인된 P2 자동 진행. P1 선택 ID → 기존 배/실제 NPC2 → 갑판 WASD → 12초 항해 → fade → 같은 NPC2와 WorldGrid 섬 도착 → 섬 이동 PASS. 최종 캡처 조합은 광부·농부이며 벌목꾼·농부 조합도 앞선 P2 검사에서 PASS했다.
- 기존 WorldGridService/WorldChunkTerrain/WorldPlayerTraversalGuard/PlayerController/CameraController와 ART-000/P0 자산만 재사용. 새 presentation+전용 checks, 기존 continuation setup/prefab 연결. P0/Golden/MainGame/WorldSandbox 씬 및 WorldGrid·Save·경제 권위 소스, Packages/ProjectSettings 변경 없음.
- Runtime/Editor compile 오류0(기존 CS8785/CS0414 경고), D3D11 blocking Console 오류0/native crash0, 필수 참조/모델/청크 collider PASS. 최종 PNG2장 fresh stable write/1920×1080 확인. 증거 Docs/Presentation/2026-09-08/P2-validation.json, P2-validation-excerpt.txt; 전체 Logs/VS_PRESENT_001/P2_Presentation_Editor.log.
- P0 인증부터 연결한 전체 연속 플레이·standalone·save/load는 이번에 확인 못 함. P0 validator 재실행 없음. 생산 실행/입주/고용/채집 보상/placement preview/저장 확장은 DEFER. 개발용 메뉴 Play Companion Selection (Development Entry)로 바로 확인 가능.
- 이번 P2 변경만 local checkpoint 후 STOP. 개인 .claude/settings.json 보존/미포함, push 없음. 다음은 사람의 전체 첫 플레이 연결과 화면/조작감 확인이다.


## 2026-09-09 — VS-PRESENT-001-P1-RECOVERY PASS

- 사람의 recovery ticket으로 이전 STOP 해제. 기존 P1 코드/후보/프리팹을 보존하고 공용 GameView 캡처 lifecycle과 PrototypeWorldLabel/ShopOpenSign 초기화만 최소 교정했다. OnValidate 구조 생성 제거, ShopOpenSign 초기화 Start로 이동. 새 label/캡처 framework 없음.
- Runtime → Editor 순차 build 오류0, 기존 CS8785/CS0414 경고만 유지. D3D11 실제 GameView 첫 recovery 실행 PASS. P0 미인증 보호, 후보3/선택2, 0·1명 확정/unknown ID/3번째 거부, 취소·교체·참조, 확정 ID 1회 전달·동결 모두 PASS.
- 새 03_CompanionSelection.png 1920×1080, 229720 bytes, 2026-09-09T01:35:56Z. 기존 파일 기준선과 새 쓰기/연속 크기 안정화 확인. Camera.Render 호출 없음. blocking Console/OnValidate 오류/native crash 0. 종료 시 기존 JobTempAlloc 진단은 별도 기존 Editor 종료 부채로 남긴다.
- 증거: Docs/Presentation/2026-09-08/P1-validation.json, P1-recovery-excerpt.txt. 전체 로그와 순차 compile: Logs/VS_PRESENT_001/P1_Recovery_Editor.log, Recovery_RuntimeCompile.log, Recovery_EditorCompile.log.
- 범위: 기존 미커밋 P1 구현과 이번 recovery만 local checkpoint, 개인 .claude/settings.json 제외. P0/CONTENT/Blender/씬/Save/ProjectSettings 변경 없음. 실제 P0 인증 완료부터 연결은 이번 개발용 진입 검사에서 확인 못 함.
- 다음: 사용자 §13 승인에 따라 P1 local commit 후 VS-PRESENT-001-P2 자동 진행. 배/WorldGrid 재사용, 신규 권위 없음, push 금지.


## 2026-09-08 — VS-PRESENT-001 P1 재개 / 검증 실패 · STOP

- 사용자 요청: 현재 git 변경만 최소 확인하여 중단 작업을 마무리하고 검증 후 이번 변경만 로컬 commit, push 금지. 시작 HEAD는 e02c3a0. 기존 미추적 P1 코드 3종·meta·설정 프리팹·캡처·JSON을 확인했으며 .claude/settings.json은 제외했다.
- PA_DepartureContinuationChecks만 보강: 검증 중 프리팹 재생성 제거, 재실행 Task/오류 상태 초기화, 준비 단계까지 90초 제한, 미인증 P0 유지·필수 참조·SaveManager 부재·1명 확정 거부·unknown ID 거부 검사 추가. Runtime/Setup/기존 씬·저장·경제 코드는 수정하지 않았다.
- 컴파일: dotnet build Assembly-CSharp-Editor.csproj 통과(오류 0, 기존 CS8785/CS0414 경고 3), Unity 스크립트 재컴파일 후 Console 오류 0. 증거 Logs/VS_PRESENT_001/ResumeCompile.log. 최초 --no-restore는 임시 project.assets.json 부재(NETSDK1004)로 실행되지 않았고 일반 build 복원으로 해소했다.
- D3D11 기존 Editor에서 Run Companion Selection Checks 1회 실행: 미인증 진입 차단, 0/1명 출항 차단, 2명 활성화, 3번째 차단, 선택 취소/교체, 참조 검사까지 PASS. 이후 PA_SafeGameViewCapture의 GameView capture was not written in time: 03_CompanionSelection.png 예외로 [VS-P1] FAIL. 자동으로 Edit Mode 복귀했다. 증거 Logs/VS_PRESENT_001/ResumeP1_EditorExcerpt.log 및 Docs/Presentation/2026-09-08/P1-validation.json.
- 추가 관찰: 기존 ShopOpenSign.Awake → PrototypeWorldLabel.OnValidate → TextMeshPro 생성 경로에서 SendMessage cannot be called during Awake, CheckConsistency, or OnValidate 오류가 발생했다. 이번 P1 코드 원인으로 단정하지 않으며 기존 시스템을 임의 수정하지 않았다.
- 확인 못 함: 확정 후 ID 1회 전달/동결(캡처 다음 검사여서 미도달), P0 실제 인증 완료→P1 버튼 연결, 새 캡처 가독성, 전체 Golden 회귀, standalone, 저장 재시작. 기존 P1 PASS JSON은 과거 실행 기록으로 분리했고 이번 실행은 FAIL로 기록했다. 캡처 파일이 갱신됐더라도 검증 통과 증거로 인정하지 않는다.
- AGENTS.md의 실패 시 BUG_LOG 기록 후 중단 규칙을 적용했다. 추가 수정/재실행 없이 STOP, commit/push 없음. 다음 세션은 BUG_LOG의 캡처 타임아웃과 기존 OnValidate 오류를 먼저 확인한다. 정상화 후 같은 메뉴로 P1을 재검증하고, P0 실제 인증 완료 뒤 동행 선택을 수동 확인한다. P2 신규 구현·CONTENT/ART 자동 진행 없음.

## M85 Gameplay Beta 현재 상태 — 2026-08-11

- Branch `milestone/gameplay-beta-85`, starting baseline `b176bdc`. 사람은 `BETA-001`~`BETA-010`과 ticket별 검증/로컬 커밋/자동 다음-ticket 전환을 명시적으로 선승인했다. push/rebase/reset/clean은 금지다.
- `BETA-001 Player Onboarding and World Readability` 완료: fresh WorldSandbox는 Day 1 09:00, 새 생활 시작 prompt, WASD→P.A. 잡화점→제작 작업대→낮 자원 목표 순서를 제공한다. shop/workbench arrival은 4m다.
- `BETA-002 Daytime Activity Completion` 완료: 기존 숲 채집/고정 밭/광질/낚시를 generated Forest/Meadow/Highland/Pond의 walkable cell에 배치하고 M70 플레이어의 기존 Space interaction, Inventory, Hotbar와 연결했다.
- 낮 활동 결과는 Carrot2/Wheat3/Ore2/Fish2이며 기존 Raw 판매가 합계는 120G다. 당일 중복은 차단되고 다음날 다시 열린다. player HUD가 활동 방향·완료·가방 수량을 설명한다.
- WORLD grid/placement/generator/navigation 개발 surface는 기본 숨김이며 F10으로 함께 복구한다. Scene YAML은 수정하지 않았고 existing M70 gameplay/input/save authority를 그대로 사용한다.
- 증거: `Logs/BETA002_D3D11_Validation_Final2.log`, `Logs/BETA002_Regression_BETA001.log`, `Logs/BETA002_Regression_WORLD010.log`, `Logs/BETA002_Regression_GatheringFishing.log`, `Logs/BETA002_Regression_MiningShop_Retry.log` PASS; blocking Console 0, new crash 0.
- `BETA-003 Crafting and Production Expansion` 완료: generated WorldSandbox에 기존 B05/B06/B07 Basic/Kitchen/Forge를 연결하고 카드 2/3/2, 부족 거래 원자성, 실제 5개 제작, 품질·가격, 118G→203G, B01 28G 판매를 검증했다.
- 증거: `Logs/BETA003_D3D11_Validation_ThirdApproved.log`, `Logs/BETA003_Regression_CraftingRecipeCard.log`, `Logs/BETA003_Regression_ProcessingChain.log`, `Logs/BETA003_Regression_BETA002.log` PASS; blocking Console 0, new crash 0.
- `BETA-004 Shop Readability and Merchandising` 완료: 실제 B01에 밤 영업/판매대 표지와 4개 슬롯의 빈 칸·상품·수량·가격·품질·품절 상태, HUD 요약을 추가했다. 이동/270° 회전, v11 pose, 고객 구매를 유지한다.
- 증거: `Logs/BETA004_D3D11_Validation_Final.log`, `Logs/BETA004_Regression_BETA003.log`, `Logs/BETA004_Regression_WORLD006B.log` PASS; blocking Console 0, new crash 0.
- `BETA-005 Customer Strategy and Feedback` 완료: 실제 Miner/Tailor profile, 방문 직후 current customer와 player HUD 성향·screen bounds, Miner 250G 보류와 Tailor 1G 구매를 검증했다. 재고·경제·SalesLog·feedback·demand 권위는 기존 시스템을 그대로 사용한다.
- 증거: `Logs/BETA005_D3D11_Validation_SynchronousPreference_Licensed_Correction.log`, `Logs/BETA005_Regression_BETA004_Licensed.log`, `Logs/BETA005_Regression_CustomerPresentation_Licensed.log`, `Logs/BETA005_Regression_CustomerArrival_Licensed.log`가 D3D11 PASS했다. Runtime/Editor 오류 0, blocking Console 0, 신규 crash 0이다.
- `BETA-006 Phone Hiring and Feed Completion` 완료: WorldSandbox P 휴대폰, 후보 8명과 실제 비용/상태, 역할별 C-02~C-09 wrapper 고용, 판매 전 empty Feed와 실제 판매/마을 변화 Feed, Audit/Settings를 검증했다.
- `BETA-007 Village Response and NPC Integration`은 `BETA_007_IMPLEMENTED_WITH_VALIDATION_DEBT` 체크포인트다. 판매 event exact snapshot, next-day 역할 시설 변화, B05~B08/runtime 활동 binding, NpcDialogue, WorldAlpha HUD와 전용 validator가 연결됐다.
- Runtime/Editor 정적 compile은 오류 0이다. `Logs/BETA007_D3D11_Validation.log`와 `Logs/BETA007_D3D11_Correction.log`는 D3D11/HUD/B08/bootstrap까지 진행했지만 주민 anchor readiness에서 종료되어 실제 hire·판매·Day 2·대화 stage는 미검증이다. native crash 0이다.
- 추가 Unity 실행 없이 anchor lifecycle을 정적으로 보정했다. 건물 obstacle/NavMesh 안정화 뒤 생성하고, invalid anchor 제거·navigation revision 추적·HiringService 유효 목록 동기화를 적용했다. 실제 상품은 resident prefab의 ProductionData/RecipeData로 역할과 시설을 해석한다.
- 이 상태는 PASS나 `BETA_007_COMPLETE`가 아니다. 장기 Goal의 validation-debt 지속 정책에 따른 local checkpoint이며 세 번째 BETA-007 D3D11 실행은 새 승인 전 금지다.
- `BETA-008 7-Day Progression`은 `BETA_008_IMPLEMENTED_WITH_VALIDATION_DEBT` 체크포인트다. normal-player clock, 실제 B01 간판, 고용 전 Day 1 관광객, 명시적 producer delivery, 달성 가능한 매출/고용/마을 반응 목표를 연결했다.
- 두 번째 D3D11은 Day 1 일반 관광객, Day 1~4 실제 판매/정산/날짜 증가, Day 2~5 납품과 Day 5 paid Farmer hire까지 PASS했다. fixture duplicate stocking을 서로 다른 고가 상품 선택으로 보정한 최종 source는 compile PASS지만 Day 6~7/Week 1 completion은 runtime 미검증이다.
- 승인된 두 BETA-008 실행을 모두 사용했으며 세 번째 실행은 금지다. local checkpoint 뒤 다음 단일 티켓은 선승인 `BETA-009 Persistence and Recovery Pass`다.
- Prototype_FirstDay Golden, WorldSandbox scene, MainGame, Prefab, Packages, ProjectSettings, Save schema v11은 승인 없는 변경 금지다.

## M70 및 Loop Policy 현재 상태 — 2026-08-11

- Branch `milestone/world-alpha-70`; LOOP-POLICY-002 기준 baseline은 `e0b5678`이며 WORLD-001~010 구현과 자동 회귀가 완료됐다. 최종 상태는 `M70_PLAYABLE_WORLD_ALPHA_COMPLETE`다.
- WorldSandbox Play Mode에서 기존 WASD 입력과 카메라로 provisional 128×128/64-chunk 섬을 이동하고, 수집→B05 제작→B01 진열→개점→NPC 구매→수익→save→실제 Play Mode 재시작→load를 수행할 수 있다. Terraform, B09 배치/이동과 기능 B01 판매대 이동/회전도 같은 흐름에 포함된다.
- Existing `PA_RuntimeSceneBinder`, Inventory, CraftingService, Shop/ShopSlot, NpcController, EconomyService, GameClock, DayNightShopLoopController와 SaveManager가 계속 유일한 권위다. 별도 gameplay/save/input 시스템은 없다.
- `Logs/WORLD010_M70_Validation_02.log`가 64 chunks, movement guard, resource/craft/placement/shop/customer/economy와 v11 restart restore를 `console=0`으로 증명한다. 전체 Golden matrix와 WORLD-006B/007/008/009 회귀도 PASS했다.
- Prototype_FirstDay는 변경 없는 Golden Regression Scene이다. WorldSandbox는 WORLD 기술/alpha 전용이며 MainGame 통합은 `WORLD-MAIN-001` 별도 사람 승인 전까지 금지다. 128×128은 여전히 provisional target이다.
- 자동 연속 개발은 여기서 종료한다. 다음은 사람의 M70 통합 플레이테스트/백로그 선별이며, 새 bounded ticket 없이 구현을 재개하지 않는다. push/rebase/reset/clean은 계속 금지다.
- `PREAPPROVED_MILESTONE_CONTINUATION`은 사람 승인과 loop-state 기록이 모두 있는 제한된 sequence에서만 기본 다음-ticket 게이트를 우회한다. M70의 승인 범위는 WORLD-005~010이었고 이미 완료되어 `nextTicket=null`이다. 완료된 WORLD ticket은 재실행하지 않으며 WORLD-011/MainGame에는 새 승인이 필요하다.

최종 갱신: 2026-08-04 (Task 131 Tripo 장기 정책 재확인·B06 Kitchen 보정 / PARTIAL)
규칙: **3~5개 작업마다** 이 문서의 "현재 상태"와 "우선순위"를 갱신한다. (`CODEX_HANDOFF_PROMPT` 사용)

## 0. Codex 첫 실행 상태

- Codex 첫 실행의 권장 안전 구간인 Task 001~003은 완료됐다.
- **첫날은 Bootstrap + Task 001~003(문서·조사)까지만** 권장되어 있었으므로, 기능 구현(Task 008 이후)은 사람 판단 후 진행한다.
- 7일 가동 계획: `../07_FULL_GAME_ROADMAP/CODEX_FIRST_7_DAYS_PLAN.md`.
- 사용자 미확정 항목: `../00_START_HERE/DECISION_REQUIRED_FOR_USER.md` (첫 기능 작업 전 #3 검증기 실행 방식, #5 하루 작업 수만 확정하면 충분).
- **3~5개 작업마다 HANDOFF 갱신 / 5~10개마다 사람 검수 / 실패 2회 시 사람 판단 / Demo Lock 이후 새 기능 금지 / Full Game 확장은 Vertical Slice 안정화 이후에만.**

## 1. 읽을 문서 순서

1. 루트 `AGENTS.md` — 입구 (1분)
2. `../00_START_HERE/ONE_PAGE_WORKFLOW.md` — 작업 절차
3. `../00_START_HERE/PROMPT_LIBRARY.md` — 지금 어떤 프롬프트를 쓸지 선택
4. `../01_IDENTITY/PROJECT_PA_IDENTITY.md` — 정체성 (재해석 금지)
5. `../01_IDENTITY/PROJECT_PA_SCOPE.md` — 현재 단계: **Stage 0 MVP Stabilization**
6. `../02_AGENT_RULES/` 3종 — 규칙
7. `../03_TASKS/TASK_QUEUE.md` — 다음 작업 1개 선택 → `../03_TASKS/ACTIVE_TASK.md`에 복사
8. `../04_VERIFICATION/VERIFICATION_RULES.md` — 완료 조건
9. (일정 감각 필요 시) `../07_FULL_GAME_ROADMAP/DEVELOPMENT_TIMELINE.md`

## 2. 현재 게임 정체성 (한 줄)

> "낮에는 동물 마을을 만들고, 밤에는 그 마을의 유일한 잡화점을 운영하는 3D 코지 라이프 시뮬레이션" — 핵심 차별점: **내가 판 물건이 마을을 바꾼다.**

**목표는 프로토타입이 아니라 완성 게임이다.** 졸업 시연(Stage 2)은 완성 게임의 부분집합. 일회용 코드·하드코딩·저장 미지원으로 때우지 않는다.

## 3. 현재 작업 우선순위 (2026-07-17 기준)

Persistence 스프린트(007/011/018/056) 이후 Fable 5가 **Task 057**(마을 변화 저장 v9, `71fa710` — 사용자 세션 지시로 스키마 승인)과 **Task 019**(품절 표시, `f6cbbe1`)를 완료했다. Task 054는 현재 v10 다음의 판매 통계 v11 추가 확장 계약까지 설계했고, Task 023/025는 가격 패널에 가격 파생 일반·희귀, 실제 품질, 읽기 전용 추천 기준가를 연결했다. Task 031은 현재 8명의 실제 주민 계층과 향후 관광객 폴백을 기본 말풍선에 표시했다. Task 086은 실제 `ShopSlot` placement의 4방향 코너, 장부 요약, 월드 라벨을 구현했고 전용 D3D11에서 인접/품절/이동/판매/마을 신호/v10 로드 재파생까지 PASS했다. 하지만 기존 ShopCustomization 회귀의 직접 `Camera.Render()`가 처음 ThemeCorner 충돌과 같은 네이티브 스택을 두 번째로 재현했다. 세 번째 Unity 실행은 금지하며 전체 validator 캡처 경로에 대한 사람 판단 전 Task086은 PARTIAL이다. Task 087은 Tripo 추정 에셋을 FBX/씬/GUID/Resources/코드까지 재대조했다. 현재 권위는 B06 Kitchen 2×2/Tier2, B07 Forge 3×2/Tier1, B08 Sewing 2×2/Tier3이며 B11/B12는 정적 세계 에셋이다. B07은 Task115에서 BuildingData·설계도·ToolSet 레시피/상품의 Tier1과 배치 진행을 정합했다. Quaternius Nature Pack CC0는 확인했으나 Tripo 개별 생성/상업 이용과 Froggy Chair 라이선스는 배포 게이트다. Task 088은 Chef Wheat3/Blacksmith Ore2/Carpenter Wood2 요청과 당일 완료/친밀도 보상을 연결했다. Task 089는 Seed→Crop→Wheat 참조, 씨앗 주머니, 고정 밭 2칸과 안전한 Wheat3 수확을 구현했다. Task 090은 이 실제 낮 활동·상품 2종·진열/가격·개점·판매·정산을 Day 2+ 체크리스트와 단계별 상단 목표로 연결했다. Task 091은 모든 기존 레시피에서 결과 메타와 차감 후 슬롯을 먼저 검사해 가방 가득 참 재료 소실을 차단했다. 네 기능 모두 컴파일·정적 계약은 PASS했으나 Unity 실플레이는 같은 실행 금지 경계로 대기한다. 실제 저장 스키마는 여전히 v10이며 Task 055 구현은 사용자 승인 대기다 (`SAVE_SCHEMA.md`).

Task 092~093 후속 현재값: 실제 Raw 판매를 기존 v10 마을 변화 계약에 연결해 다음 날 `원자재 수거처`를 표시하고, Fish/생선구이/목제 가구의 당일 성공 판매를 낚시·가구 생활 트렌드로 기존 결산에 함께 표시한다. Task 088~093 여섯 기능 모두 컴파일·정적 계약 PASS / Unity 실플레이 대기다. 실제 낚시→판매→당일 명명 트렌드→다음 날 Raw 변화가 코드상 이어져 Task 069는 BLOCKED에서 PARTIAL로 정합화했다. **현재 집계는 DONE 44 / PARTIAL 24 / TODO 2 / BLOCKED 5 / DECISION_REQUIRED 18(총 93)**이며 이 값이 최신이다.

Task 094는 사용자 추가 Tripo 최종화 정책을 기존 감사·배치 문서에 통합하고, 라이선스 문서가 없는 Froggy Chair의 실내·광장 런타임 생성 2곳을 제거했다. 원본/Resource는 보존했으며 최종 빌드 포함 위험은 배포 게이트로 유지한다. Runtime/Editor와 정적 계약은 PASS, GameCamera는 실행 금지 경계로 미확인이다. **현재 집계는 DONE 44 / PARTIAL 25 / TODO 2 / BLOCKED 5 / DECISION_REQUIRED 18(총 94)**이며 이 값이 최신이다.

Task 095는 기존 `Recipe_Furniture`·Inventory·B05 작업대·Tier·ShopSlot·당일 SalesLog만 읽어 Day 4+ 목표에 다음 한 단계를 표시한다. Tier 1 매출→B05 설치→Plank3 준비→Tier 2 매출→제작→진열→당일 판매→정산 확인이 한 경로로 보이며 수익/Tier/저장은 바꾸지 않았다. Runtime/Editor와 읽기 전용 정적 계약 PASS, 실제 UI/왕복은 실행 금지 경계로 대기한다. Task 070은 BLOCKED→PARTIAL이며 **현재 집계는 DONE 44 / PARTIAL 27 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 95)**가 최신이다.

Task 096은 제품 타이틀과 새 게임/이어하기, Task 097은 제품형 Pause 세션 제어, Task 098은 Day 7 첫 주 완주 요약과 저장 후 Day 8/종료 선택, Task 099는 실제 입력 기반 시작 조작 안내, Task 100은 타이틀 전용 게임 종료, Task 101은 기존 저장의 새 게임 덮어쓰기 확인을 연결했다. Task 102는 B09에 이미 있던 24칸 `StorageBox`와 v10 `storedItems`를 실제 6×4 보관/회수 UI로 연결했다. Task 103은 비어 있던 C 패널을 기존 8개 전체 도감으로 교체하고 B05~B08 작업대의 아이콘·전체 재료/출력·결과 피드백·ESC/커서 흐름을 제품화했다. 여덟 작업 모두 기존 흐름과 권위를 재사용하고 Runtime/Editor·정적 계약을 통과했으며 실제 화면/로드/빌드 종료는 실행 금지 경계로 대기한다. **현재 집계는 DONE 44 / PARTIAL 35 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 103)**가 최신이다.

Task 104는 첨부된 Grid/Tripo 지침을 기존 Placeable P1~P5 위의 장기 ADR로 고정하고, B11 원형 분수의 6×6 루트 BoxCollider 모서리를 실제 Visual mesh의 정적 비볼록 collider로 교체했다. 메시가 없으면 기존 Box를 유지하며 캡슐형 carving obstacle, 원본/프리팹/씬은 보존한다. Runtime/Editor 오류 0, 계약 12/12 PASS. 실제 분수 둘레 이동과 동일 구도 After는 안전 Unity 경로 대기다. **현재 집계는 DONE 44 / PARTIAL 36 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 104)**가 최신이다.

Task 105는 18~23시 영업과 주민 Rest 시작 19~20시의 불일치로 생기던 늦은 밤 무손님 공백을 닫았다. 시간표·실제 phase·구매 수학은 바꾸지 않고 Rest 주민에게만 한시적 방문 lease를 부여하며, Tier 0 외부와 Tier 1 실내 초대 모두 완료·실패·timeout·폐점 뒤 원래 위치·Shop·Rest를 복구한다. Runtime/Editor 오류 0, 계약 18/18 PASS. 실제 시간대 유입과 23시 회수는 안전 Unity 경로 대기다. **현재 집계는 DONE 44 / PARTIAL 37 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 105)**가 최신이다.

Task 106은 핵심 차별점의 첫 시각 변화였던 Processed 지점에 남은 원시 큐브 5개를 제거했다. 기존 B05 래퍼 전체가 아니라 이미 감사·검증된 `Visual` 메시만 180°/0.58배로 복제하고 Project P.A. 준비 키트와 `가공 준비대` 간판을 결합했다. Workbench·Collider·NavMeshObstacle·행동·추가 Light는 복제하지 않으며 Raw와 v10 카테고리 저장 계약을 보존한다. Runtime/Editor 오류 0, 계약 18/18 PASS. 실제 같은 카메라 구도는 안전 Unity 경로 대기다. **현재 집계는 DONE 44 / PARTIAL 38 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 106)**가 최신이다.

Task 107은 판매 가능한 `철제 도구`와 기존 Forge 레시피를 세 번째 마을 변화에 연결했다. Utility 판매는 기존 SalesLog→pending→다음 DayPreparation→v10 category 문자열을 그대로 사용하며, B07 래퍼 전체가 아니라 `Visual`만 0.44배로 복제해 `공구 수리대` 간판을 결합한다. Workbench·Collider·NavMeshObstacle·행동·Light는 복제하지 않고 Processed/Raw와 상호 배타적으로 활성화한다. Runtime/Editor 오류 0, 계약 23/23 PASS. 실제 같은 카메라 구도는 안전 Unity 경로 대기다. **현재 집계는 DONE 44 / PARTIAL 39 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 107)**가 최신이다.

Task 108은 기존 `목제 가구`/`의류` Luxury 판매를 네 번째 마을 변화에 연결했다. Luxury 판매는 같은 SalesLog→pending→다음 DayPreparation→v10 category 문자열을 사용하며, B08 래퍼 전체가 아니라 `Visual`만 0.48배로 복제해 `공예 전시대` 간판을 결합한다. Workbench·Collider·NavMeshObstacle·행동·Light는 복제하지 않고 Processed/Raw/Utility와 상호 배타적으로 활성화한다. Runtime/Editor 오류 0, 수정된 정적 계약 27/27 PASS. 실제 같은 카메라 구도와 v10 복원은 안전 Unity 경로 대기다. **현재 집계는 DONE 44 / PARTIAL 40 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 108)**가 최신이다.

Task 109는 스마트폰에 8명 후보가 보여도 모든 `spawnPrefab`이 비어 채용이 실패하던 성장 루프를 복구했다. 명시 프리팹은 계속 최우선이며, 없을 때만 같은 전문 분야의 기존 C-02~C-09 주민 중 `NpcController`와 실제 SkinnedMesh가 있는 원본 구성을 사용한다. 런타임 채용 clone은 원본 선택에서 배제하고 신규/복원 경로가 같은 resolver를 공유한다. 기존 Economy·v10 저장 권위는 유지했으며 profile/specialty/schedule/dialogue/후보별 친밀도 키를 주입하고 Specialist에는 작업대가 맞는 기존 레시피만 할당해 기존 Tailor의 Bread 불일치도 채용본에서 해소한다. UI는 첫 열기 카드, 소개·역할·비용·잠금·결과를 표시한다. Runtime/Editor 오류 0, 수정된 정적 계약 36/36 PASS. 실제 스마트폰 채용·역할 행동·저장 복원은 안전 Unity 경로 대기다. **현재 집계는 DONE 44 / PARTIAL 41 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 109)**가 최신이다.

Task 110은 Day 5의 추상적인 “향후 인력 필요 파악”을 실제 P.A. Phone 채용 행동으로 바꿨다. Day 5 이후 낮 목표와 기존 운영 체크리스트는 미고용이면 첫 생산자/전문가 고용을 안내하고, 고용 뒤에는 실제 후보 이름·한글 역할·인원수를 완료로 표시한다. `HiringService.OnHired`는 LongPlay UI 갱신만 요청하고 채용·비용·스폰·저장은 건드리지 않는다. Day 7 첫 주 결산에도 최대 3명의 결정론 roster와 총 인원수가 표시된다. Runtime/Editor 오류 0, 정적 계약 29/29 PASS. 실제 Day 5 채용→Day 7 결산과 1920×1080 가독성은 안전 Unity 경로 대기다. **현재 집계는 DONE 44 / PARTIAL 42 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 110)**가 최신이다.

Task 111은 채용 이후 생산 지원의 실제 소유권 이전을 막던 거래 버그를 복구했다. 기존 `Inventory.AddInstance`는 꽉 찬 가방의 메타 일치 스택 일부를 먼저 채운 뒤 false를 반환할 수 있었고, `ProducerNpcController`는 돈을 먼저 차감한 뒤 추가 실패 시 NPC 재고까지 삭제했다. 이제 전량 수용을 읽기 전용으로 선검사해 실패 시 인벤토리를 변경하지 않으며, 생산자는 공간 확인 뒤 결제한다. 가방/잔액 부족은 돈과 NPC 재고를 유지하고, 결제 후 예외 실패는 기존 Economy 권위로 전액 환불한다. 성공/보류는 기존 NPC 말풍선으로 표시한다. Runtime/Editor 오류 0, 거래 계약 30/30 PASS. 실제 가방 가득 참→공간 확보→재납품은 안전 Unity 경로 대기다. **현재 집계는 DONE 44 / PARTIAL 43 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 111)**가 최신이다.

Task 112는 Day 7 저장→Day 8 시작 뒤 일반 반복 문구만 남던 장기 루프를 기존 성장 시스템과 연결했다. Day 8~14는 B09 보관→Processed 판매→채용→2카테고리 판매→Tier 1→다음 날 마을 변화→2상품 판매를 차례로 요구하며, `PlayableDayScenarioController`가 실제 보관량·당일 판매·채용 roster·Tier·활성 문화 변화를 0.5초마다 읽는다. 자동 온보딩 보급은 Day 7에서 끝나고 경제·아이템·판매·제작·채용·Tier·저장은 기존 권위 그대로다. Runtime/Editor 오류 0, 계약 40/40 PASS. 실제 Day 7→8/대표 Day 8~14 전환과 1920×1080 가독성은 안전 Unity 경로 대기다. **현재 집계는 DONE 44 / PARTIAL 44 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 112)**가 최신이다.

Task 113은 최신 Tripo/Grid 지침을 기존 8분류·Placeable·원본 비파괴·출처 ADR에 재대조하고, B12의 오래된 10×5m 루트 Box/Obstacle이 실제 약 3.63×1.96m Visual보다 커 만드는 해안 투명 벽과 carving 공백을 축소 전용 런타임 보정으로 막았다. Visual 로컬 mesh bounds 8모서리를 사용하며 메시 실패 시 기존 물리를 유지하고 어떤 축도 키우지 않는다. B12는 정적 세계 구조로 유지해 교역/Placeable/가짜 접근점/저장을 추가하지 않았다. Runtime/Editor 오류 0, 계약 20/20 PASS. 실제 해안 이동·NPC 우회·동일 GameCamera는 안전 Unity 경로 대기다. **현재 집계는 DONE 44 / PARTIAL 45 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 113)**가 최신이다.

Task 114는 Day 14 뒤 일반 반복 문구로 남던 구간을 Day 15~30 첫 달 캠페인으로 연결했다. 기존 B09 보관, Processed/Utility/Luxury 판매, 2~3명 지원 인력, 2~3카테고리/상품, Tier 1, 활성 마을 변화, 최종 예비 재고를 날짜별 목표로 읽으며 플레이어 행동을 대신 수행하지 않는다. Day 30 Settlement에는 누적 매출·돈·Tier·평판·고용 roster·마을 변화·당일 정산을 표시하는 완주 화면이 뜨고, Day 30 저장→기존 다음 날→Day 31 재저장 또는 저장 성공 뒤 종료를 선택한다. Day 7 보급/첫 주 완료는 독립적으로 보존했다. Runtime/Editor 순차 빌드 경고 0·오류 0, 계약 56/56 PASS. 실제 대표 Day 15~30·Day 30 두 버튼·Day 31·1920×1080은 안전 Unity 경로 대기다. **현재 집계는 DONE 44 / PARTIAL 46 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 114)**가 최신이다.

Task 115는 첫 달 목표의 실제 달성 가능성을 감사해 B07 BuildingData·설계도·ToolSet 레시피/상품 Tier 1과 배치 Tier 3 불일치를 바로잡았다. Tier 1 장부는 B05와 B07 설계도를 중복 없이 지급하고, B06 Tier 2/B08 Tier 3과 TierService 10,000G/100,000G는 보존한다. Day 23은 활성 B07+정확한 당일 ToolSet+다른 상품, Day 24는 활성 B07+ToolSet+Processed 판매를 읽어 씨앗 우회와 Tier 2 전 Luxury 막힘을 제거했다. Day 15=16,000G→Day 30=31,000G→이후 단조 증가로 표시 목표 역행도 제거했다. Runtime/Editor 오류 0(기존 CS8785/CS0414만), 실행 가능 소스 계약 39/39 PASS. 실제 Tier 1 보상·진열대 회수→B07 배치/접근·IronBar/ToolSet 제작·Day 23/24 판매/UI는 안전 Unity 경로 대기다. **현재 집계는 DONE 44 / PARTIAL 47 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 115)**가 최신이다.

Task 116은 Editor 자동화의 실제 직접 `camera.Render()` 호출 12곳을 전수 감사하고, ThemeCorner에서 실제 사용한 일반 GameView `ScreenCapture` 흐름을 공용 `PA_SafeGameViewCapture`로 추출했다. 두 번째 충돌 지점 ShopCustomization과 현재 Tier/B07 ShopProgression의 캡처를 공용 async 경로로 옮겨 대상 직접 호출은 0이다. Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414), 계약 28/28 PASS. Character/Cottage/CustomerPanel/DemoView/FinalPresentation/GatheringShop/OutdoorPlacement/ShopEvolution/VillageCulture/Workbench의 직접 렌더 10곳이 남아 Unity는 실행하지 않았다. **현재 집계는 DONE 44 / PARTIAL 48 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 116)**가 최신이다.

Task 117은 VillageCulture·CustomerPanelLayout·FinalPresentation의 직접 렌더 3곳을 같은 공용 async GameView 경로로 옮겼다. 마을 변화 3장, 고객 패널 1장, 최종 프레젠테이션 6장은 await되어 다음 상태 변경보다 먼저 완료되며 시장 마커/FOV 46/전체 레이어/1920×1080 계약을 유지한다. Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414), 교정 계약 38/38 PASS. Character/Cottage/DemoView/GatheringShop/OutdoorPlacement/ShopEvolution/Workbench의 직접 렌더 7곳이 남아 Unity는 실행하지 않았다. **현재 집계는 DONE 44 / PARTIAL 49 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 117)**가 최신이다.

Task 118은 DemoView·GatheringShop·OutdoorPlacement의 직접 렌더 3곳을 같은 공용 async GameView 경로로 옮겼다. 실제 추적 카메라 2560×1440 한 장, 낮 채집→밤 판매 1920×1080 다섯 장, 야외 배치 전후 동일 직교 1280×720 두 장을 await하고 캡처 중 CameraController를 동결·복원한다. Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414), 계약 35/35 PASS. Character/Cottage/ShopEvolution/Workbench의 직접 렌더 4곳이 남아 Unity는 실행하지 않았다. **현재 집계는 DONE 44 / PARTIAL 50 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 118)**가 최신이다.

Task 119는 Character·Cottage·Workbench의 직접 렌더 3곳을 같은 공용 async GameView 경로로 옮겼다. 캐릭터 lineup/idle/walk, B10 전경·4방향·최종·runtime, B05 감사/최종 회전·runtime 캡처를 await하며 이동 시간·기능 판정·renderer/controller 복원을 유지한다. Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414), 계약 42/42 PASS. `PA_ShopEvolutionVisualFinalizer`의 직접 렌더 1곳만 남아 Unity는 실행하지 않았다. **현재 집계는 DONE 44 / PARTIAL 51 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 119)**가 최신이다.

Task 120은 마지막 `PA_ShopEvolutionVisualFinalizer`의 직접 렌더를 같은 공용 async GameView 경로로 옮겼다. B02~B04 4방향 12장과 runtime baseline/Tier 1~3을 await하며 1600×900, 4초/0.75초 단계 안정화, orthographic 6.6 구도, controller/clearFlags와 배치 저장 동등성 판정을 유지한다. Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414), 계약 36/36 PASS. 저장소 실제 직접 `Camera.Render()` 호출은 0이며 Unity는 실행하지 않았다. **현재 집계는 DONE 44 / PARTIAL 52 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 120)**가 최신이다.

Task 121은 Day 30 이후 일반 반복 문구만 남던 구간을 Day 31~45 두 번째 달 진입 캠페인으로 연결했다. 기존 보관·가공·채용·Forge/ToolSet·상품/카테고리 구성·마을 변화·준비 재고·누적 매출만 읽는 15개 계획/상태 목표이며 새 퀘스트·저장 상태·보상은 없다. 기존 목표 수식을 그대로 사용해 Day 31 32,500G에서 Day 45 53,500G까지 단조 증가하고, Tier 2 100,000G와 B06 Tier 2는 보존한다. Runtime/Editor 오류 0(기존 CS8785/CS0414), 계약 23/23 PASS. Unity는 반복 네이티브 충돌 경계로 실행하지 않았다. **현재 집계는 DONE 44 / PARTIAL 53 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 121)**가 최신이다.

Task 122는 Day 46~75를 보관·가공·지원 인력/4상품 준비·3카테고리·B07 ToolSet+Processed·마을 방향/4상품·매출 점검의 7단계 운영 리듬으로 생성하고, Day 76에서 기존 100,000G 자동 Tier 2 상태를 판정한다. 보관 12→20개와 가공 2→4건만 운영 목표로 완만히 상승하며 Tier 수치·보상·저장·경제 권위는 바꾸지 않는다. Runtime/Editor 오류 0(기존 CS8785/CS0414), 계약 34/34 PASS. Unity는 반복 네이티브 충돌 경계로 실행하지 않았다. **현재 집계는 DONE 44 / PARTIAL 54 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 122)**가 최신이다.

Task 123은 Day 77~90을 B06 배치→BreadLoaf/구운 감자/생선구이 조리·준비·판매→Chef→Processed 마을 변화로 연결하고 Day 90에 활성 B06+세 상품 판매+Processed 방향을 함께 판정한다. Runtime/Editor 오류 0(기존 CS8785/CS0414), 코드 공백 검사 PASS. 다만 정적 검사식이 실제 호출부 2개를 3개로 잘못 기대해 중단됐고 Unity는 실행하지 않았다. **현재 집계는 DONE 44 / PARTIAL 55 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 123)**가 최신이다.

Task 124는 호출부 기대값을 실제 2개로 바로잡아 계획/case 각 14개, 단일 판정, Day 76 경계, B06 Kitchen 프리팹, 세 Kitchen 레시피, 세 Processed 출력과 요구 리소스를 포함한 44개 계약을 확인했다. B06 최소 Tier C# 표현과 구운 감자·생선구이 `itemName` YAML 표현 2개가 검사식과 맞지 않아 44/47에서 중단했고 추가 조회·재시도·Unity 실행은 하지 않았다. **현재 집계는 DONE 44 / PARTIAL 56 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 124)**가 최신이다.

Task 125는 결합 검증을 재실행하지 않고 세 권위 행만 읽었다. B06은 실제 switch case에서 `return 2`, 두 Item은 Unity YAML Unicode escape를 해석하면 정확히 `구운 감자`와 `생선구이`다. 실제 데이터 결함 없이 검사 표현 불일치로 확정해 Task 124를 DONE으로 닫고 Task 123 정적 계약을 47/47로 확정했다. **현재 집계는 DONE 46 / PARTIAL 55 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 125)**가 최신이다.

Task 126은 Day 91~105를 전문 주민 요청→기존 일일 저장 표식→하루 1 평판→기존 Tier 3 자동 승급→B08→의류/가구→Luxury 마을 변화로 연결했다. Day 105는 Tier 3+B08+두 상품 판매+Luxury 방향을 함께 판정한다. Runtime/Editor 오류 0, 계약 24/24 PASS이며 Unity 실플레이는 안전 실행 승인 대기다. **현재 집계는 DONE 46 / PARTIAL 56 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 126)**가 최신이다.

Task 127은 B05~B08의 기존 회전 interaction 셀을 전문 주민의 NavMesh 목적지와 셀 예약에 연결했다. 장애물 원점 이동을 제거하고 완전 경로 선택, 정면 보기, 이동·회수/일정/비활성화 무효화, 복원 후 재접근을 구현했다. 경로·문서 검사 실패는 직접 권위 증거로 복구해 `BUG_LOG.md`에서 닫았고 Runtime/Editor 오류 0, 접근·예약 계약 29/29 PASS다. Unity 실제 이동·제작은 사람 판단 게이트로 대기한다. **현재 집계는 DONE 46 / PARTIAL 57 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 127)**가 최신이다.

Task 128은 완성 게임 필수 손님 2계층 중 표시 폴백만 있던 관광객을 Day 2+ 정상 영업에 실제로 연결했다. `CustomerArrivalController`가 기존 스케줄·프로필·SkinnedMesh가 유효한 주민 원본에서 외형만 복제하고 런타임 전용 관광객 프로필을 구성한다. 관광객은 주민 일정·생산·전문 역할·대화를 갖지 않으며, 완전한 NavMesh 외부 진입점에서 기존 `NpcController` 쇼핑 FSM과 `PurchaseEvaluator`를 거쳐 반응 말풍선을 보인 뒤 같은 진입점으로 퇴장한다. 영업당 2명·동시 1명 상한과 닫힘/시간초과 정리를 포함하며 저장 스키마·구매 수학·씬·프리팹은 그대로다. Runtime/Editor 오류 0, 관광객 계약 46/46 PASS이며 Unity 실제 진입·구매·퇴장과 화면은 사람 판단 게이트로 대기한다. **현재 집계는 DONE 46 / PARTIAL 58 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 128)**가 최신이다.

Task 129는 Day 105 뒤 일반 운영 폴백을 감사해 정상 플레이 평판이 Tier 2의 3점에서 끝나지만 기존 본사 감사가 평판 5를 요구해 Tier 4가 영구 차단된 단절을 복구했다. Day 106+ Tier 3 전문 주민 요청은 기존 일일 저장 표식으로 감사 요구 평판까지만 하루 1점을 지급하고, 상단 목표·체크리스트는 실제 500,000G·평판 5·고용 3명과 다음 감사일을 표시한다. 최종 승급은 계속 `AuditService → TierService.TryManualAdvance()`만 소유한다. Tier 4 통과 뒤 첫 Settlement에는 기존 완주 UI를 재사용한 전체 캠페인 기록과 저장 후 다음 날 자유 운영/저장 후 종료가 열린다. Runtime/Editor 오류 0, 40개 자동 계약+1개 직접 범위 권위 PASS이며 Unity 실제 평판·감사·Tier4·완주/저장 화면은 사람 판단 게이트로 대기한다. **현재 집계는 DONE 46 / PARTIAL 59 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 129)**가 최신이다.

Task 130은 감사 앱이 다음 감사일까지의 날짜만 표시하고 실제 실패 원인·성공 결과는 콘솔에만 남던 단절을 닫았다. `AuditService`는 현재 매출·평판·고용과 조건별 완료 여부, 다음 감사일, 현재 세션 최근 결과를 읽기 전용으로 제공하고 결과 이벤트만 발행한다. 감사 앱은 세 조건의 실제 현재값, 완료/부족, 실패 뒤 가장 가까운 행동, 통과 뒤 Tier 4 해금을 표시하며 수동 승인 진행 바도 실제 조건 진행을 반영한다. 500,000G·평판 5·고용 3명·7일·단독 수동 승급·`LastAuditDay` 저장은 그대로다. Runtime/Editor 오류 0, 계약 48/48 PASS이며 실제 앱 갱신/가독성은 사람 판단 게이트로 대기한다. **현재 집계는 DONE 46 / PARTIAL 60 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 130)**가 최신이다.

다음 단일 우선순위는 완주 루프의 구매·개점·정산 피드백이 시각/UI에만 머무는지 기존 `AudioManager`와 프로젝트 안의 출처 확인 가능 클립을 감사하는 것이다. 사용 가능한 기존 클립이 있으면 가장 영향력 큰 무음 행동 하나에만 연결하고, 외부·출처 불명·유료 음원은 추가하지 않는다. 씬·프리팹·저장·경제/구매 수학은 보존하며 Unity 실청취는 사람 판단 게이트를 유지한다.

주의: `SubmissionPackages/*.zip` 삭제 2건은 사용자 소유 변경이므로 복구/stage 금지.

추가 (2026-07-13 오후, Fable 5): 접지/충돌/NPC 정비(`f3ef51a`) + 들어갈 수 있는 상점 기초(`3bf30c9`). 파묻힘·투명 벽 해소, 실내 상점(2x3 슬롯 그리드) 검증 통과. 상세: `09_FINAL_FABLE_SPRINT/COLLISION_AND_RIG_FIX_REPORT.md`, `SHOP_EVOLUTION_PLAN_AND_IMPLEMENTATION.md`.

완료(2026-07-13 오후, Fable 5): **S2+S3 — 실내 잡화점에 손님이 온다.** `PA_ShopLocator` 앵커 안전화 + `InteriorCustomerController` 초대/복귀 + `PA_InteriorCustomerValidator` PASS(입장→구매 500→515G→퇴장). 캡처 `Logs/DemoViewShots/inside_shop_20260713_163425.png`.

추가(2026-07-13 저녁, Fable 5, **검증 생략 — 컴파일만**): S5 구현 — 진열 아이콘 빌보드(전 슬롯), 실내 인테리어(러그/선반/계산대/화분/의자), 동시 손님 2명.

완료(2026-07-14, Fable 5): **S5 검증 완료 + 정각 스케줄 버그 수복.** 1차 회귀에서 InteriorCustomer FAIL → 원인은 S5가 아니라 `NpcScheduleController`가 매 정각 `Pause()`로 진행 중 쇼핑 FSM을 초기화하던 기존 버그(같은 페이즈 재적용 생략으로 수복). InteriorCustomer(동시 2명 구매 500→530G)/FinalRoute/DayNight PASS, 실내 캡처 `Logs/DemoViewShots/s5_inside_20260714_103016.png` 시각 확인. 주의: 밤 영업(18~23시)과 NPC 일과의 겹침이 18~20시뿐 — 20시 이후 밤 손님이 구조적으로 0명(설계 이슈, 스케줄 조정은 사람 승인 필요).

완료(2026-07-15, Codex): **S4 — Tier 1 실내 잡화점 해금 연결.** Tier 0 문 잠금/조건 간판 → Tier 1 OPEN 간판·문 조명·해금 패널 → 실내 입장·진열·가격·퇴장 PASS. 별도 저장 필드 없이 v9 `currentTier`에서 상태 파생. 로그 `Logs/Codex_S4_EnterableShop.log`. `Docs/Codex/` 완성 지도 4종 추가.

완료(2026-07-15, Codex): **Task 034 — 구매/거절 일일 통계.** 성공 판매와 거절 평가를 일차별로 집계하고 정산 HUD·Day 1 결산·다음 날 가격 조언에 연결했다. D3D11 검증 구매 1/거절 1/구매율 50% PASS. 로그 `Logs/Codex_Task034_DailyDecisionStats.log`. 통계는 런타임 전용이며 저장 v9는 변경하지 않았다.

부분 완료(2026-07-15, Codex): **Task 068 — 플레이어 날짜 전환.** Day 1 결산 버튼→Day 2 06:00/목표, Day 2 정산 간판 Interact→Day 3 06:00/채집 리셋 PASS. `ForceSet` 우회가 아닌 `OnNewDay` 게임플레이 경로를 추가했다. 로그 `Logs/Codex_Task068_FinalRoute_Day2Transition.log`, `Logs/Codex_Task068_DayNight_Day3Transition.log`. 낚시 밤 판매는 Task 041로 닫혔고 사람 3일 연속 플레이·저장 재실행만 남아 PARTIAL.

완료(2026-07-15, Codex): **Task 039 — 실제 낚시 상호작용.** `shore-forage`를 전용 자식 `FishingSpot`과 캐스팅 대기 행동으로 전환하고, 기존 일일 채집/저장 상태를 그대로 재사용해 Fish 2개를 지급한다. D3D11 낚시·일일 리셋·진열·가격 PASS, FinalRoute 30G PASS. 로그 `Logs/Codex_Task039_Fishing.log`, `Logs/Codex_Task039_FinalRouteRegression.log`.

완료(2026-07-15, Codex): **Task 041 — 낚시 결과의 밤 판매 왕복.** 검증기의 합성 Fish 주입을 제거하고 실제 어획 Fish 2개 중 1개가 진열로 이동하도록 했다. Fisher_01 구매 18G, 잔액 500→518G, 누적매출·Raw SalesLog·일일 구매 통계 PASS. 로그 `Logs/Codex_Task041_FishingSaleRoundTrip.log`, 회귀 `Logs/Codex_Task041_FinalRouteRegression.log`.

완료(2026-07-15, Codex): **Task 042 — 낚시 스모크 고정.** `SMOKE_CHECKLIST.md`를 신설해 자동 D3D11 낚시→18G 판매 증거, Day 1 회귀, 사람이 확인할 이동·1.25초 대기·NPC 접근 항목을 분리했다. 런타임/씬 변경 없음.

완료(2026-07-15, Codex): **Task 043 — 광질/농사 경계 설계.** 실제 Crop/Farmland/PlayerInteraction/Item/Recipe/프리팹 감사를 바탕으로 광질을 다음 단일 구현으로 확정했다. 목표 왕복은 `quarry-mining` Ore 2개→1개 진열→15G 판매이며 기존 일일 활동/v9 저장을 재사용한다. 농사는 씨앗/수확 참조·입력·날짜 성장·승인된 저장 순으로 기존 Crop/Farmland를 복구한다. 문서 `DESIGN_MINING_FARMING.md`.

완료(2026-07-15, Codex): **quarry-mining 실제 판매 왕복.** 낮 타격으로 Ore 2개 획득 → v9 격리 저장/복원 → 1개 진열/15G 확정 → Miner 구매 → 잔액 500→515G/Raw 기록/일일 통계 → Day 3 재활성 PASS. 로그 `Logs/Codex_MiningShopLoop_Final.log`, 회귀 `Logs/Codex_Mining_FinalRouteRegression.log`. 광산 primitive는 임시 기능 표식이며 최종 아트가 아니다.

완료(2026-07-15, Codex): **비주얼 툴체인 감사·Unity MCP 실사용.** Unity 6000.3.2f1/URP 17.3.0과 전체 패키지, Blender 부재, 캡처, 에셋 출처를 감사했다. 설치 전 체크포인트 `64860ff` 뒤 CoplayDev Unity MCP 9.7.0을 안정 태그/커밋에 고정하고 프로젝트 `.codex/config.toml`의 loopback 서버로 연결했다. MCP로 씬·B01 프리팹·Play Mode를 직접 확인해 중앙 원시 박스 상점을 기존 B01 목재 노점 Visual로 교체하고 겹친 임시 충돌을 런타임에서 제거했다. 문서: `Docs/Codex/VISUAL_TOOLCHAIN.md`, `ASSET_AND_TOOL_PROVENANCE.md`, `ART_DIRECTION.md`, `TRIPO_ASSET_AUDIT.md`.

장기 에셋 정책(2026-07-15 사용자 지시): Tripo 추정 에셋은 임시라는 이유만으로 삭제·전면 교체하지 않는다. 플레이어/C-01~C-09 주민은 외형 정체성을 보존하고 접지·Avatar·Animator·보행·재질·그림자·LOD·Collider·NavMeshAgent를 먼저 고친다. B09 창고와 B05 작업대는 단순 재질 확정 없이 실제 저장/준비 기능과 공간 맥락부터 분석해 필요하면 기존 모델 기반으로 재구성한다. 원본은 덮어쓰지 않으며 분류·근거·출처는 `Docs/Codex/TRIPO_ASSET_AUDIT.md`와 provenance에 계속 누적한다.

완료(2026-07-16, Codex): **그리드 기반 플레이어 커스터마이징 P1/P2.** 기존 `GridService`를 확장해 2m 5×4 `shop.interior` zone, owner footprint/clearance, 입구 보호와 BFS 통로를 구현했다. 상점 배치 장부에서 실제 청사진/회수 가구를 배치·90도 회전·이동·안전 회수한다. 기존 ShopSlot 6개와 B05 Workbench 기능·NavMesh carving을 재사용한다. 저장은 v10이며 zone/definition/instance/cell/rotation/recovered/function을 복원한다. D3D11에서 B05 2×2, 선반 `(4,0)/270°`, 이동 후 NPC 61G 구매, 저장 후 상품 2개/73G, 보호 통로와 회수 PASS. 기존 SaveRoundTrip v10과 FinalDemoRoute 30G도 회귀 PASS. 문서 `Docs/Codex/PLACEMENT_SYSTEM_ARCHITECTURE.md`, `CUSTOMIZATION_ROADMAP.md`, `PLACEABLE_ASSET_GUIDE.md`; 증거 `Logs/ShopCustomization/20260716_103307/`, `Logs/ShopCustomization_*Regression.log`.

완료(2026-07-16, Codex): **P4 ShopSlot 고객 접근 정밀화.** 배치 정의의 회전된 interaction 셀을 실제 고객 앞자리로 사용하고, `ShopCustomerApproachController`가 이동 전에 NPC owner별 예약과 `NavMesh.SamplePosition/CalculatePath=PathComplete`를 보장한다. 진열대 이동 시 낡은 예약을 폐기하며, 도착 후 기존 ShopSlot claim/PurchaseEvaluator를 그대로 실행한다. 이동 선반 `(4,0)/r3`→앞셀 `(3,0)`, 2인 별도 예약, 실내 15G 구매·퇴장, CustomerArrival cap 2, FinalDemoRoute 30G PASS. 증거 `Logs/P4_*`, 캡처 `Logs/DemoViewShots/p4_shop_approach_final_20260716_114141.png`.

완료(2026-07-16, Codex): **B10 Cottage 원본 비파괴 시각 최종화.** FBX는 정상이고 실제 정면이 로컬 `-X`인데 맵/외부 문이 `-Z`를 전제로 한 것이 원인이었다. 3채 정면을 광장으로 맞추고 레거시 Static 중복을 끄며, 원시 문 Renderer만 숨겨 기존 BuildingEntrance/Collider/Tier/spawn을 유지했다. Unity Editor API로 모따기 목재/크림 간판 에셋을 만들고 실제 Play MainCamera에서 `P.A. SHOP - Tier 1` 전체 문구를 확인했다. Tier 왕복과 FinalDemoRoute 30G PASS. 증거 `Logs/B10_CottageAudit/`, `Logs/B10_*Final*`.

완료(2026-07-16, Codex): **B05 목재 가공 작업대 기능 아트.** 원본 14,689-triangle 실루엣은 보존하고 기존 2×2 `-Z` interaction 면과 반대인 작업면을 런타임 정렬했다. 물리 collider/carving을 모델에 맞추고, 기존 Wood→Plank 제작에 원목→가이드→완성 판재→clamp가 읽히는 Project PA 파생 키트와 0.72초 성공 피드백을 연결했다. 실제 CraftingUI Wood 2→Plank 1, ShopCustomization/v10, ProcessingChain, EnterableShop, FinalDemoRoute 30G PASS. 캡처 `Logs/B05_WorkbenchAudit/b05_runtime_before.png`→`b05_runtime_after.png`; 로그 `Logs/B05_*`.

완료(2026-07-16, Codex): **Tripo C-01~C-09 캐릭터 Unity 설정/보행 최종화.** 9종 모두 valid Humanoid/정상 메시로 외형을 보존했다. 런타임 주민을 실제 bounds 기준 1.75m/+0.02m 접지, Capsule/Agent 1.8/0.4, 정지 거리 0.75로 보정하고 NPC 거리 기반 cadence와 플레이어 Walk 재생 속도 동기화를 구현했다. D3D11 최종 walking에서 플레이어 5m/s/2.25×, NPC 8명 2.5m/s/cadence 1.851~2.328, 보행 중 접지 PASS. 실제 도로 GameCamera 캡처와 InteriorCustomer/CustomerArrival/FinalDemoRoute 30G 회귀도 PASS했다. 실제 할당 Bori=C-03, Miner=C-04, Farmer/Fisher=C-05 중복, C-02 미사용은 사용자 확인 전 유지한다. 증거 `Logs/CharacterFinalization_RuntimeFinal_FinalFraming.log`, `Logs/CharacterFinalization/character_walk_after.png`, `Logs/CharacterFinalization_*Regression.log`.

완료(2026-07-16, Codex): **B02~B04 상점 진화 외관 비파괴 최종화.** 세 원본 모두 정상이고 실제 정면은 로컬 `-X`이며 코지 마을 단계형 외관으로 적합해 Unity 설정 수정 대상으로 확정했다. 래퍼의 Shop/ShopSlot 8/16/32개 중복을 피하려고 Visual-only Resource 프리팹 3개를 생성하고 모델 bounds 콜라이더·Obstacle·Entrance/Sign anchor만 구성했다. `ShopEvolutionController`는 기존 `currentTier`에서 Tier 0=B10, 1=B02, 2=B03, 3+=B04를 파생하고 기존 문·스폰·BuildingEntrance·간판을 활성 모델 문에 맞춘다. 실제 Tier 1/2/3 캡처, 단일 외관/Shop 중복 없음, 기존 배치 스냅샷 완전 보존, Tier 출입·진열·가격·퇴장, FinalDemoRoute 30G가 D3D11 PASS했다. 증거 `Logs/ShopEvolutionAudit/`, `Logs/ShopEvolution_RuntimeFinal_AnchorFix.log`, `Logs/ShopEvolution_EnterableShopRegression.log`, `Logs/ShopEvolution_FinalDemoRouteRegression.log`.

완료(2026-07-17, Codex): **P5 상점 진화·배치 해금.** 기존 `shop.interior` 원점과 v10 배치를 유지한 채 Tier 0/1=`5×4`, Tier 2=`6×5`, Tier 3+=`7×6`로 동·북 확장하고 물리 진열 한도 `6/6/8/12/20`을 적용했다. 현재 장부 보상은 Task115 정합 후 B05+B07/B06/B08이 Tier 1/2/3에서 열린다. 기존 Processed 마을 변화는 따뜻한 공방 테마를 해금하고 `shop.theme/fixed.shop.theme` v10 레코드로 복원된다. 확장 NavMeshSurface와 양방향 link의 먼 셀 완전 경로, 추가 실제 ShopSlot 3개, 저장 clear→restore, 동일 구도 UI/조명 캡처를 D3D11에서 확인했다. 회귀 ShopCustomization/EnterableShop/SaveRoundTrip/FinalDemoRoute PASS. 증거 `Logs/P5_ShopProgression_D3D11_Release.log`, `Logs/ShopProgressionUnlock/20260717_005831/`, `Logs/P5_*Regression.log`.

완료(2026-07-17, Codex): **Task 044 — 주민 의뢰 표시 설계.** `DESIGN_RESIDENT_REQUEST.md`에서 대화·수요·전문가 레시피·인벤토리·친밀도·일일 활동 저장을 대조했다. 첫 계약은 Chef_01의 실제 `Recipe_Bread` 재료인 Wheat 3개이며, 기존 `Economy` 대사 토픽과 일일 활동 문자열 저장을 재사용한다. 범용 퀘스트 엔진, 저장 스키마, 코드, 씬은 추가하지 않았다. 현재 Tailor가 `Recipe_Bread`로 잘못 배정된 데이터 불일치도 기록해 후보에서 제외했다. 기능은 미구현이다.

완료(2026-07-17, Codex): **Task 045 — 낮 활동 결과→재고 연결 동기화.** `DAYTIME_ACTIVITIES.md`를 신규 작성하고 `PROJECT_PA_GAME_LOOP.md`를 최신화했다. 현재 소스에는 보조 2+플레이어 활동 4의 일일 재고 원천 6개가 있다. 기존 D3D11 증거로 Fish 2→1 진열→18G/Fisher 구매와 Ore 2→1 진열→15G/Miner 구매를 완료 경로로 확정했다. 농사·주민 의뢰·Carrot/Wheat 개별 판매는 미구현/미검증으로 남겼다. 코드·씬은 변경하지 않았다.

완료(2026-07-17, Codex): **Task 046 — 카테고리별 판매 통계 조사.** `VILLAGE_TREND.md`에 거래 1건/총 결제액 계약, SalesLog 기본 100건, 최근 40건 `count×1000+revenue`, Tool 제외와 동점 미정, Processed 다음 날 변화, v10 문화 상태 저장과 런타임 통계 미저장 경계를 실제 코드와 기존 D3D11 로그로 확정했다. Raw Fish/Ore 실제 판매와 Processed 2건/76G·BreadLoaf·다음날 변화·pending→active 저장 증거를 분리했다. 코드·씬은 변경하지 않았다.

완료(2026-07-17, Codex): **Task 047 — 낚시/캠핑/가구 명명 트렌드 점수 설계.** Fish(Raw)/생선구이(Processed)를 fishing, 목제 가구(Luxury)를 furniture에 정확한 데이터 쌍으로 매핑하고, 캠핑은 실제 상품·레시피·활동이 없어 비활성으로 확정했다. 성공 판매만 점수를 만들며 `transactions×1000+min(revenue,999)`, 오늘 일차/향후 7일 창, 거래→매출→최신 판매→trendId 동점 규칙을 설계했다. `Shop_Tent_Kit`과 배치 가구는 판매 통계에서 제외한다. 코드·씬은 변경하지 않았다.

완료(2026-07-17, Codex): **Task 051 — 시설 해금 방향 예고 표시.** 기존 성공 판매의 선도 ItemCategory를 감사 앱에서 Raw=생산자 보관·수거, Processed=조리·가공, Utility=수리·공구, Luxury=포장·문화 진열 시설 후보로 번역한다. BreadLoaf 30G 판매 후 `가공품 / 조리·가공 작업대 / 판매 1건·30G / 실제 해금: 티어·감사 조건`을 실제 1920×1080 캡처에서 확인했다. TierService·AuditService·판매 수학·저장·씬은 변경하지 않았다. D3D11 FinalRoute와 FinalPresentation PASS.

완료(2026-07-17, Codex): **Task 052 — 이벤트 해금 후보 설계.** `DESIGN_EVENTS.md`에서 실제 Fish/생선구이 성공 판매가 다음 날 여는 `해변 풍어제`를 첫 후보로 확정했다. 행사 날 기존 낚시→가공 선택→진열·가격→밤 구매→정산을 보존하고 상태·재시도·평판 보상 후보·시각/동선·저장·Task 078 승인·검증 계약을 분리했다. 문서/소스/기존 로그 정적 검사와 `git diff --check` PASS. 이벤트 런타임은 미구현이다.

완료(2026-07-17, Codex): **Task 054 — 판매 통계 v11 추가 확장 설계.** `SAVE_SCHEMA.md`에 최근 원거래·일차 구매/거절·카테고리 판매·7일 명명 트렌드 DTO, 보존 창, 정산 권한, v10→v11 빈 기본값, 복원 순서와 격리 검증 계약을 확정했다. 문서 12·정확한 v10 소스 17·whitespace·전체 diff 검사 PASS. 런타임은 v10이며 Task 055는 승인 전 금지다.

완료(2026-07-17, Codex): **Task 023 — 희귀품/일반품 구분 데이터 표시.** rarity 원본 필드가 없어 현재 카탈로그 가격대 사이 100G를 임시 경계로 쓰고 `가격 파생값`을 명시했다. `ShopPriceUI`가 실제 `ItemInstance.quality`와 함께 일반품 연녹색/희귀품 금색을 표시한다. BreadLoaf/1.00과 의류/1.25 두 분기 D3D11 FinalPresentation assertion·1920×1080 캡처, FinalDemoRoute 30G PASS. 씬·아이템·구매·저장 무변경.

완료(2026-07-17, Codex): **Task 025 — 상품 추천 기준가 표시.** `PurchaseEvaluator`와 같은 basePrice+quality 기준을 `추천 기준가 N G · 기본가+품질`로 읽기 전용 표시한다. BreadLoaf 30G, 품질 1.25 의류는 현재가 165G/추천 186G로 비자동적용을 검증했다. 첫 캡처의 버튼 겹침을 고친 최종 1920×1080 일반/희귀 화면과 D3D11 FinalPresentation, FinalDemoRoute 30G PASS. 구매 수학·씬·저장 무변경.

완료(2026-07-17, Codex): **Task 031 — 주민/관광객 계층 표시.** 별도 관광객 필드가 없고 현재 8명 모두 실제 마을 일과표를 가진 주민임을 확인했다. 일과표 있음=`[주민]`, 없음=`[관광객]`으로 표시만 파생하며 현재 주민을 가짜 관광객으로 만들지 않는다. F10 성향 패널과 기본 `NpcBubbleUI` 별도 태그에 연결했고 기존 말풍선 본문·구매 수학은 보존했다. Runtime/Editor 오류 0, D3D11 CustomerPresentation·PanelLayout 캡처·FinalDemoRoute 30G PASS. 캡처 `Logs/CustomerPanelReview/20260717_032821/customer_panels_1920x1080.png`.

완료(2026-07-17, Codex): **Task 024 — 상품 진열 테마 코너 설계.** `DESIGN_THEME_CORNER.md`에 같은 카테고리의 활성·재고 보유 `ShopSlot` placement가 4방향으로 인접하고 2개 이상 연결될 때 코너로 파생하는 계약을 작성했다. 기존 `shop.theme/fixed.shop.theme`, v10 저장, 구매/가격/매출/트렌드 수학은 보존한다. 코너는 품절·보충·이동·회수·로드 뒤 다시 계산하며, 후속 좁은 placement 읽기 API·`MerchandisingCornerController`·배치 장부/월드 라벨·D3D11 검증 11개를 고정했다. 기능은 미구현이다.

완료(2026-07-17, Codex): **Task 087 — Tripo3D 임시 에셋 감사 정합화.** FBX 174/OBJ 150/GLB 0/Blend 0, Nature Pack 외 FBX 24개를 집계하고 Unity 6 바이너리 메인 씬·GUID→래퍼→BuildingData/청사진·Resources/코드 사용을 대조했다. B06/B07/B08을 기존 레시피·전문 주민과 연결된 2×2/3×2/2×2 기능 작업대로, B11/B12를 현재 정적 세계 에셋으로 확정했다. 당시 B07 배치 Tier3 판정은 Task115의 권위 데이터 감사로 Tier1에 정정됐고 B06 Tier2/B08 Tier3은 유지된다. Quaternius Nature Pack은 CC0 1.0이며 Tripo 개별 생성/상업 이용과 Froggy Chair 라이선스는 미확인 배포 게이트다. 코드·씬·프리팹·원본·패키지 무변경, 정적 검사 PASS.

부분 완료(2026-07-17, Codex): **Task 088 — 주민 재료 요청 플레이 루프.** 전문 주민의 실제 담당 레시피/작업대에서 Chef Wheat3, Blacksmith Ore2, Carpenter Wood2 요청을 파생하고 Tailor→Bread 불일치를 제외했다. 기존 프롬프트/DialogueUI, 인벤토리+핫바 합산, 정확 차감, `dayPrepCollectedActivities` 당일 완료, 친밀도 전용 보상을 연결했다. Runtime/Editor 오류 0과 정적 계약은 PASS했으나 직접 렌더 충돌 2회 경계 때문에 Unity 실플레이·저장 왕복·다음 날·밤 차단·UI 캡처는 확인 못 했다. 씬·프리팹·저장 스키마·경제/판매·패키지 무변경.

부분 완료(2026-07-17, Codex): **Task 089 — 고정 밭 Wheat 재배 F1/F2.** `Item_15_Seed`→`Crop_Corn`→`Item_Wheat` 참조를 복구하고, 낮 씨앗 2개와 농부 작업 지점 인근 경작 메시 밭 2칸을 연결했다. 심기 성공 뒤 씨앗 1개 차감, 성장 단계 프롬프트/라벨, 가방 전량 수용 확인 뒤 Wheat 3 수확, 실제 Wheat 프리팹 3단계 표현을 구현했다. Runtime/Editor 오류 0과 12개 정적 계약 PASS. Unity 실플레이와 F3 날짜 성장/plot 저장은 대기한다. `PlayerInteraction`·씬·저장 스키마·경제 코어·패키지 무변경.

부분 완료(2026-07-17, Codex): **Task 090 — Day 2+ 생활–상점 운영 체크리스트.** 기존 정적 안내를 0.5초 갱신의 실제 낮 활동·판매 상품 2종·진열/가격·개점·당일 판매·정산 상태와 단계별 상단 목표로 교체했다. 가방·핫바·진열대를 합산하고 Tool/Tier 잠금은 제외하며, 새 퀘스트 상태·보상·저장을 만들지 않는다. Runtime/Editor 오류 0과 정적 계약 10개 PASS. Unity Day 2 상태 전환과 1920×1080 가독성은 대기한다. 코어·씬·저장·패키지 무변경.

부분 완료(2026-07-17, Codex): **Task 091 — 가공 결과물 수용량 선검사.** `CraftingService`가 결과 품질/메타를 먼저 만들고 핫바→가방 재료 소비 뒤의 메타 일치 스택 또는 빈 슬롯을 모의한다. 공간이 없으면 재료 차감 전에 false, 가능하면 기존 차감→결과→작업대 피드백을 유지한다. 레시피 8개와 Runtime/Editor 오류 0, 정적 계약 11개 PASS. Unity 가방 세 분기는 대기한다. `Inventory`·데이터·UI·저장·코어·씬·패키지 무변경.

부분 완료(2026-07-17, Codex): **Task 092 — Raw 다음 날 생산자 보관·수거 변화.** 실제 Raw 판매가 가장 최근 추적 판매이면 기존 pending→다음 날/v10 category 계약으로 `PA_VillageCulture_Raw`를 활성화한다. 새 primitive 없이 CC0 `Prop_WoodLog`/`Prop_Rock`과 Project P.A. 간판 메시를 사용하고 복제 물리·기능을 제거했다. 새 pending마다 힌트 1회를 다시 허용한다. Runtime/Editor 오류 0, 정적 계약 14개 PASS. 실제 GameCamera/동선은 대기한다. 저장 스키마·경제/NPC·씬·프리팹 원본·패키지 무변경.

부분 완료(2026-07-17, Codex): **Task 093 — 당일 낚시·가구 명명 트렌드.** 정확한 Fish/생선구이/목제 가구 성공 판매만 오늘의 `낚시 생활|가구 문화`로 묶고 `transactions×1000+min(revenue,999)` 및 명시적 동점 규칙을 적용했다. 기존 카테고리 결산 첫 줄과 시설 예고를 보존하며 생활 트렌드를 둘째 줄에 붙인다. Runtime/Editor 오류 0, 정적 계약 18개 PASS. 캠핑은 비활성, 저장은 런타임 전용이며 실제 결산/가구 판매 화면은 대기한다.

부분 완료(2026-07-17, Codex): **Task 094 — Tripo3D 장기 정책/미확인 소품 노출 정리.** 8분류·캐릭터 정체성·Unity/Blender 경계·Placeable/출처/동일 카메라 게이트를 기존 문서에 통합하고 `Prop_FroggyChair` 런타임 생성 2곳을 제거했다. Runtime/Editor 오류 0, `Assets/Scripts` 참조 0, 원본/Resource 보존과 B01/CC0 식생/벤치 계약 PASS. GameCamera와 최종 빌드 Resource 격리는 대기한다.

부분 완료(2026-07-17, Codex): **Task 095 — 가구 보조 루프 단계 안내.** 기존 처리 기회 책임자가 `Recipe_Furniture`, Plank 보유량, 활성 BasicWorkbench, Tier, 진열, 당일 판매를 읽어 Day 4+ 체크리스트에 다음 행동 하나를 표시한다. 아이템 지급·Tier 강제·자동 제작/진열/판매는 없다. Runtime/Editor 오류 0과 정적 계약 PASS, 실제 가독성/전체 왕복은 대기한다.

부분 완료(2026-07-17, Codex): **Task 096 — 제품형 새 게임/이어하기 진입.** 기존 첫날 Canvas에 타이틀과 저장 존재 기반 선택을 추가했다. 이어하기는 기존 v10 Load/Restore만 호출하고 새 게임은 기존 이름 등록부터 시작한다. Runtime/Editor 오류 0, 상태 전이 15개와 mutator 부재 PASS. 실제 저장 없음/있음 화면과 로드 왕복은 대기한다.

부분 완료(2026-07-17, Codex): **Task 097 — 제품형 Pause 세션 제어.** 기존 ESC 오버레이에 계속하기·저장·불러오기·저장 후 종료를 연결하고 이전 timeScale/커서를 복구한다. 저장 없음 Load 비활성, 중복 입력 잠금, 저장 실패 시 종료 취소를 구현했다. Runtime/Editor 오류 0, 계약 11/11 PASS. 실제 클릭·가독성·빌드 종료는 대기한다.

부분 완료(2026-07-17, Codex): **Task 098 — Day 7 첫 주 완주.** 기존 장기 진행 책임자가 Day 7 정산에서 성과 요약과 저장→Day 8/저장→종료 선택을 표시한다. 배경 진행 차단, timeScale/커서 복구, 실패 회복을 포함한다. Runtime/Editor 오류 0, 계약 12/12 PASS. 실제 화면·클릭·빌드 재실행은 대기한다.

부분 완료(2026-07-17, Codex): **Task 099 — 실제 입력 기반 시작 조작 안내.** 기존 첫날 `StartupStep`에서 P.A. Phone 뒤에 WASD/방향키, Space, I/P/C, 1~9/휠, 좌클릭/R/M/X, F5/F9/ESC를 실제 입력 권위와 일치하게 안내한다. 확인 뒤 보급품→도착→Day 1로 이어지며 별도 상태/저장은 없다. Runtime/Editor 오류 0, 기능 계약 11/11 PASS. 실제 1920×1080 가독성과 클릭 전환은 대기한다.

부분 완료(2026-07-18, Codex): **Task 100 — 타이틀 게임 종료.** 기존 시작 Canvas에 Title 전용 `게임 종료`를 추가해 종료/이어하기/새 게임 3버튼을 배치했다. 비Title·Day 1 결산에서는 숨기고 이어하기 로딩 중 잠근다. 빌드는 `Application.Quit`, Editor는 안내만 사용하며 저장 호출은 없다. Runtime/Editor 오류 0, 계약 14/14 PASS. 실제 1920×1080 화면과 Windows 프로세스 종료는 대기한다.

부분 완료(2026-07-18, Codex): **Task 101~103 — 저장 보호·창고·제작 제품 흐름.** 저장 있는 새 게임 확인, B09 24칸 보관/회수, C 8레시피 도감과 B05~B08 작업대 제작 카드를 연결했다. Task 103은 원격 제작을 막고 기존 `CraftingService` 권위를 보존하며 전체 화면 입력 차단·패널/커서·ESC 우선순위까지 정리했다. Runtime/Editor 오류 0, Task 103 계약 16/16 PASS. 실제 클릭·가독성·저장 왕복은 안전 Unity 경로 대기다.

1. **직접 렌더 전수 0 확인 후 사람 판단** — `PA_ThemeCornerValidator`와 `PA_ShopCustomizationValidator`에서 같은 직접 `Camera.Render()` 네이티브 충돌이 2회 발생했다. Task116~120에서 공용 GameView `ScreenCapture` 도우미와 감사 대상 전부의 전환이 끝났고 저장소 실제 직접 호출은 0이다. 세 번째 Unity 실행은 여전히 사람 판단 전 금지한다. 승인 뒤에는 격리 D3D11 GameView PNG 1회로 freshness·파일 크기·가독성·카메라/화면 복원을 먼저 확인한다.
2. **승인 후 첫 기능 검증: Task 088** — Chef의 Wheat 2 부족→3 준비→정확 3 전달→친밀도 1회→반복 차단, 저장/로드 같은 날 완료, 다음 날 재요청, 밤 전달 차단과 프롬프트/DialogueUI 가독성을 D3D11에서 확인한다.
3. **같은 승인 실행의 두 번째 기능 검증: Task 089** — 씨앗 주머니 2개→밭 2칸 심기→1/3~3/3 성장→가방 가득 참 수확 차단→Wheat 3개 수확, 밤 차단, 실제 Wheat 표현과 플레이어/NPC 동선을 확인한다.
4. **같은 Day 2 실행의 연결 검증: Task 090** — 낮 활동 완료, 판매 상품 2종, 진열/가격, 18시 개점, 당일 구매, 23시 정산이 체크리스트와 상단 목표에서 0.5초 안에 갱신되고 1920×1080에서 잘리지 않는지 확인한다.
5. **같은 가공 실행의 안전 분기: Task 091** — 가방 가득 참은 재료 무차감, 메타 일치 결과 스택은 병합, 재료 소비로 비는 슬롯은 결과 수령이 되는지 기존 Plank 레시피로 확인한다.
6. **같은 다음 날 변화 실행: Task 092** — 실제 Raw 판매 당일에는 변화 없음, 다음 날 `원자재 수거처`만 활성, v10 저장/복원 유지, 간판 방향·스케일·ShopSlot/플레이어/NPC 동선 비겹침을 같은 GameCamera로 확인한다.
7. **같은 결산 실행: Task 093/069** — Fish 18G 판매가 기존 Raw 카테고리와 `낚시 생활 1건/18G/1018점`을 함께 표시하고, 다음 날 Task 092 변화로 이어지는지 확인한다. 별도 가구 분기에서는 기존 B05/Recipe_Furniture로 목제 가구를 제작·진열·판매해 `가구 문화`를 확인한다.
8. **승인 후 첫 비주얼 검증: Task 131 B06 Kitchen** — 기존 Visual 기반 renderer-bounds 축소 전용 Box/box obstacle, 로컬 `-Z` interaction anchor, 성공 제작 모델 pulse 구현은 Runtime/Editor 오류 0·계약 30/30을 통과했다. 실제 GameCamera 전후로 2×2 footprint, 전면 2셀 통로, 플레이어/Chef 접근, 모델-물리 경계, Bread 제작 pulse를 확인한다. 이후 B07 Forge, B08 Sewing 순서이며 캡처 증거 없이 Blender/재질 재작업을 확정하지 않는다.
9. **Task 095 실제 확인 대기** — 새 안내는 기존 B05→Plank3→Recipe_Furniture→목제 가구→ShopSlot→`trend.furniture`의 다음 단계를 표시한다. 안전 Unity 경로에서 Tier 0/1/2 문구, 1920×1080 잘림, 실제 제작→진열→판매→정산 전환을 확인한다. 100,000G 장기 페이싱 변경은 별도 사용자 승인 없이는 금지한다.
10. **Task 097 실제 확인 대기** — 타이틀에서는 ESC 메뉴가 열리지 않고, 플레이 중에는 커서가 풀리며 네 버튼이 작동하는지 확인한다. 저장 없음/있음 Load 상태, Save 후 계속, Load 후 원래 시간/커서, 실제 빌드 Save & Quit→이어하기를 같은 격리 세이브로 확인한다.
11. **Task 098 실제 확인 대기** — Day 7 23시 정산에서 완주 모달이 한 번 뜨고 배경 간판이 차단되는지 확인한다. Day7 save→Day8→save와 save→quit 두 분기를 격리 세이브로 확인하고 타이틀 이어하기까지 왕복한다.
12. **Task 099 실제 확인 대기** — 타이틀→새 게임→Phone→조작 안내에서 1920×1080 다섯 줄 본문이 잘리지 않고, `조작 확인` 뒤 보급품→도착→Day 1로 진행하는지 확인한다.
13. **Task 100 실제 확인 대기** — 저장 없음/있음 타이틀에서 종료/이어하기/새 게임이 겹치지 않는지 확인하고, Windows 빌드에서 종료 클릭 뒤 프로세스가 남지 않는지 확인한다.
14. **Task 101 실제 확인 대기** — 저장 없음은 이름 등록 직행, 저장 있음은 덮어쓰기 경고→시작/타이틀 복귀로 분기하고 재조회되는지 확인한다. 1920×1080 가독성과 새 게임 후 첫 F5가 기존 단일 슬롯을 덮어쓰는 실제 동작을 격리 저장으로 확인한다.
15. **Task 102 실제 확인 대기** — B09 문 앞 `[Space]`에서 24칸 화면을 열고 선택 핫바 보관·슬롯 회수·가방 가득 참·ESC를 확인한다. 품질/가격 메타와 창고 내용물이 v10 저장→로드 뒤 동일한지 격리 저장으로 확인한다.
16. **Task 103 실제 확인 대기** — `[C]`에서 기존 8개가 작업대별로 모두 보이고 클릭되지 않는지, B05~B08 앞 `[Space]`에서는 해당 레시피만 실제 제작되는지 확인한다. 재료 부족/성공/가방 가득 참, ESC와 I/P/B09 전환, 1920×1080 카드/스크롤을 같은 실행에서 확인한다.
17. **Task 104 실제 확인 대기** — B11 분수 둘레를 네 방향에서 걸어 사각 모서리의 보이지 않는 충돌 제거, 보이는 석재 경계 정지, 벤치 접근, NPC 캡슐형 carving 우회를 확인하고 같은 GameCamera로 After를 남긴다.
18. **Task 105 실제 확인 대기** — 18:30/20:30/22:30의 외부·실내 영업에서 Rest 주민이 한시 방문하고 기존 동시 손님 상한을 지키는지, 23:00 폐점 뒤 원래 위치·Shop·Rest로 복귀하는지 확인한다.
19. **Task 106 실제 확인 대기** — 같은 GameCamera에서 Processed 판매 전/다음 날을 비교해 B05 기반 가공 준비대의 스케일·정면·간판, 상점/분수 겹침과 플레이어/NPC 동선을 확인한다.
20. **Task 107 실제 확인 대기** — 실제 철제 도구 Utility 판매 당일에는 변화가 없고 다음 DayPreparation에만 B07 기반 공구 수리대가 나타나는지, 같은 GameCamera에서 스케일·정면·간판·상점/분수 겹침과 플레이어/NPC 동선을 확인한다.
21. **Task 108 실제 확인 대기** — 실제 목제 가구 또는 의류 Luxury 판매 당일에는 변화가 없고 다음 DayPreparation에만 B08 기반 공예 전시대가 나타나는지, v10 저장/복원과 같은 GameCamera의 스케일·정면·간판·상점/분수 겹침, 플레이어/NPC 동선을 확인한다.
22. **Task 055/F3 승인 대기** — v11 판매 통계와 날짜 기반 plot 저장은 각각 저장 설계·마이그레이션·복원 API·왕복 검증 범위를 정확히 보고한 뒤 사용자 승인 전 수정하지 않는다.
23. **배포/정체성 결정 대기** — C-02~C-05 주민 역할 매핑, Tripo 생성/상업 이용 증빙, Froggy Chair 라이선스 또는 Resource 격리는 사용자 확인 조건을 유지한다. B12 교역 기능은 Task 079 선행 조건 전 추가하지 않는다.
24. **Task 109 채용 제품 흐름 실확인** — 충분/부족 잔액으로 스마트폰 채용을 시도하고 Producer/Specialist가 정확한 역할 외형·행동·레시피로 합류하는지, clone이 다음 원본으로 재사용되지 않는지, 저장 후 재실행 복원과 1920×1080 카드 가독성을 안전 Unity 경로에서 확인한다.
25. **Task 110 첫 주 채용 성장 실확인** — Day 5 미고용 목표→P.A. Phone 채용→체크리스트의 이름/역할 완료→Day 7 첫 주 결산 roster가 같은 세션에서 이어지는지와 긴 텍스트의 1920×1080 가독성을 확인한다.
26. **Task 111 생산자 납품 거래 실확인** — 플레이어 가방을 메타 불일치 스택까지 포함해 가득 채운 뒤 Producer 납품에서 돈·플레이어 재고·NPC 재고가 모두 보존되는지 확인한다. 공간을 만든 뒤 같은 생산물이 정확 수량/매입금으로 들어오고 성공/보류 말풍선이 읽히는지 확인한다.
27. **Task 112 2주차 운영 캠페인 실확인** — Day 7 저장→Day 8 시작 뒤 B09 보관 목표가 나타나는지 확인하고, Day 9 Processed 판매·Day 10 채용·Day 11 카테고리 2종·Day 12 Tier 1·Day 13 광장 변화·Day 14 상품 2종의 대표 상태가 0.5초 내 완료로 바뀌는지와 1920×1080 가독성을 확인한다.
28. **Task 113 B12 항구 충돌 실확인** — 기존 10×5m 모서리 영역을 플레이어가 통과하고 보이는 약 3.63×1.96m 부두 경계에서 정지하는지 확인한다. NPC가 축소된 box carving을 따라 해안 통로를 우회하는지와 동일 GameCamera After를 남긴다.
29. **Task 114 첫 달 캠페인·완주 실확인** — 대표 Day 15 보관, Day 17 두 번째 채용, Task115로 정합된 Day 23/24 Forge 판매, Day 30 상품 3종 상태가 체크리스트에 반영되는지 확인한다. Day 30 모달의 저장→Day 31→재저장과 저장→종료→이어하기 두 분기, 1920×1080 요약/버튼 가독성을 격리 저장으로 확인한다.
30. **Task 115 Tier 1 Forge 가치사슬 실확인** — Tier 1 장부 첫 열기에 B05/B07 설계도가 중복 없이 지급되는지 확인한다. 빈 진열대 두 칸을 비우고 회수한 뒤 B07 3×2를 배치해 전면 접근·콜라이더·NavMesh 통로를 확인하고, Plank1+Ore4→IronBar2→ToolSet1 제작 뒤 Day 23 ToolSet+다른 상품과 Day 24 ToolSet+Processed 판매가 정확히 완료되는지 확인한다.
31. **Task 116~120 안전 캡처 후속** — 저장소 실제 직접 렌더 0과 Runtime/Editor 컴파일·상태 복원 정적 확인은 완료됐다. 사람 판단 뒤 격리 D3D11 PNG 1회→ShopEvolution→ShopCustomization→ShopProgression→VillageCulture/CustomerPanel/FinalPresentation→DemoView/GatheringShop/OutdoorPlacement→Character/Cottage/Workbench 순서로만 검증한다.

미확정(사용자 결정 대기, `DECISION_REQUIRED_FOR_USER.md`): Demo Lock 날짜 / 두 번째 낮 활동(임시 낚시) / 검증기 실행 방식 / 주간 작업 시간 / 하루 작업 수.

## 4. 위험 파일 (수정 전 반드시 규칙 확인)

| 파일/영역 | 위험 |
|---|---|
| `Assets/Scenes/Prototype_FirstDay.unity` | 메인 씬. 덮어쓰기·재작성 금지. Build Settings 시작 씬 |
| `SaveManager.cs`/`SaveData.cs`/`LocalJsonSaveRepository.cs` | 저장 스키마. 추가 확장만(v증가+마이그레이션), 사람 승인 |
| `Shop.cs`/`ShopSlot.cs`/`EconomyService.cs`/`PurchaseEvaluator.cs`/`NpcController.cs` | 경제·구매 코어. 넓은 수정은 사람 승인. 표시/로그만 우선 |
| `PlayerController.cs`/`PlayerInteraction.cs`/`Inventory.cs` | 조작·인벤토리 코어. 임의 재작성 금지 |
| `Docs/01~08` | 동결 문서. 코드 주석이 `Docs/§번호` 인용 |
| `PROJECT_PA_CRASH_REPORT_20260625.md`, `PROJECT_PA_CRASH_REPORT_20260717.md` | 루트 고정 (preflight glob) |
| `Assets/Jinxish/**` | 서드파티. 수정 금지 |

## 5. 동결 문서 (이동·개명·대수정 금지)

- `Docs/01_개요_및_정체성.md` ~ `Docs/08_아트_및_씬_구성_가이드.md` — 코드 § 인용. 최신 기준은 `../01_IDENTITY/`.
- `PROJECT_PA_CRASH_REPORT_20260625.md`, `PROJECT_PA_CRASH_REPORT_20260717.md` — preflight 루트 glob.
- `Assets/Jinxish/**/readme.md` — Unity 관리 영역.
- 상세: `../00_START_HERE/DOCS_INDEX.md` §5.

## 6. 금지사항 (요약)

정체성 재해석 / 감성 서사·철학·다크·순수 타이쿤 변경 / 대형 리팩터링 / 기능 삭제 / 승인·출처·정확한 버전 고정 없는 외부 패키지 / 멀티플레이 착수 / 씬·프리팹·저장 임의 변경 / 메인 씬 덮어쓰기 / Project_D 접근 / git push / 검증 없는 완료 선언 / 한 작업에 여러 기능. 전체: `../02_AGENT_RULES/AI_SLOP_PREVENTION.md`.

## 7. 다음 작업 방식

1. `PROMPT_LIBRARY.md`에서 상황에 맞는 `CODEX_*_PROMPT`를 골라 사용.
2. 사전 점검(경로/Git/Unity 프로세스/크래시 리포트) → `CODEX_WORKER_RULES.md` §4.
3. `TASK_QUEUE.md`에서 TODO 1개 → `ACTIVE_TASK.md` 복사 → 수정 계획 보고 → 최소 변경 → 검증 → 기록.
4. 실패 2회 → `BUG_LOG.md` 기록 후 정지, 사람 판단 요청.
5. **하루 권장 작업 수**: 저강도 1~2 / 집중 3~5 / 크런치 5~8(품질 위험). 상세 `DEVELOPMENT_TIMELINE.md` §7.
6. **위험 작업(사람 승인 YES) 전 반드시 사람 승인.**
7. **Demo Lock 이후 새 기능 금지** — `CODEX_DEMO_LOCK_PROMPT`만.
8. **3~5개 작업마다 이 문서(HANDOFF) 갱신**, 5~10개마다 사람 검수.

## 8. 알려진 제약

- Unity 자동 실행은 **D3D11 전용**(D3D12 크래시 이력, 미승인).
- 직접 `Camera.Render()` 기반 validator 캡처는 2026-07-17 동일 네이티브 충돌 2회로 **실행 금지**. 상세 `PROJECT_PA_CRASH_REPORT_20260717.md`.
- Unity Editor 열려 있으면 batchmode 금지.
- 캐릭터 walking의 이전 `Time.timeScale=0` 실패는 RESOLVED다. 최신 기준은 `Logs/CharacterFinalization_RuntimeFinal_FinalFraming.log`와 세 회귀 로그이며, 역할 모델 중복은 별도 사용자 확인 항목이다.
- P5의 확장 조명 null 오류는 RESOLVED다. 최종 기준은 `Logs/P5_ShopProgression_D3D11_Release.log`와 `Logs/ShopProgressionUnlock/20260717_005831/`이며, 숨은 선반 템플릿은 상점 hierarchy 밖 비활성 루트에 있어 기본 실내 ShopSlot 수 6개를 오염시키지 않는다.
- Task 044의 Tailor 소스 자동 선택자 false negative는 `BUG_LOG.md` OPEN이다. 실제 `PA_SceneAutoBuilder`의 Tailor→Bread 줄은 확인됐으며, 같은 실패 선택자는 재사용하지 말고 다음 정적 감사에서 `rg -F` 또는 직접 행 범위를 사용한다.
- Task 046의 Markdown 선택자와 추정 소스 경로 실패는 RESOLVED다. 실패 문장은 재사용하지 않았고 `rg --files Assets`로 확정한 `Assets/Scripts/` 명시 경로와 독립 표식 검증이 PASS했다.
- Task 047의 Windows 경로 와일드카드 재사용은 RESOLVED다. 후속 감사는 명시 경로와 `-g` 필터만 사용했고 캠핑 Resources 부재까지 확인했다.
- Task 023의 `C_ReactionBad` 상수 문맥 패치 실패는 RESOLVED다. 실패 선택자를 폐기하고 세 독립 문맥으로 구현했으며 일반/희귀 캡처와 상점 회귀까지 PASS했다.
- `PA_FinalDemoRouteValidator`는 2026-07-13에도 D3D11 batchmode 통과했다. `VERIFICATION_RULES.md` §2-B의 BLOCKED 표기는 오래된 문서 상태이므로 다음 문서 정리 태스크에서 동기화가 필요하다.
- 자동 루프는 dry-run 정책 + 클린 baseline 필요(`Automation/LoopEngineering/loop-policy.json`).
- PowerShell 스크립트 직접 실행은 정책에 막힐 수 있음 → `-ExecutionPolicy Bypass -File`.

## 9. DEVELOPMENT_TIMELINE.md를 읽어야 하는 시점

- 발표 날짜가 정해졌을 때(역산표 §6).
- 하루/주간 작업량을 정할 때(§5 시나리오, §7 속도 정책).
- "언제 무엇을 포기할지" 판단이 필요할 때(§8, §9).
- Full Game 확장 착수 전(§10 Fable 판단).

## 10. WORLD-000 인계 — 2026-08-04

- **현재 활성 ticket 없음:** WORLD-000 조사·설계·문서화는 종료했고 loop-state는 `needs_human_review`다. WORLD-001을 자동 시작하지 않는다.
- **선행 변경 보호:** `Assets/Scripts/AudioManager.cs`와 `Assets/Scripts/SalesLogManager.cs`에는 WORLD-000 이전의 미검증 변경이 남아 있다. 별도 bounded ticket로만 다루고 WORLD 계열에 섞지 않는다.
- **권장 아키텍처:** 2m cell, 16×16 Chunk, 1m elevation 0~6, custom chunk mesh, seed+generationVersion+sparse delta, role anchor, cell graph+per-Chunk NavMeshSurface.
- **씬 역할:** `Prototype_FirstDay` Golden Regression(신규 월드 실험 금지), 승인 후 `WorldSandbox` testbed, `MainGame`은 Gate 1~5 뒤 별도 통합 후보.
- **다음 읽기:** `PROJECT_PA_WORLD_NORTH_STAR.md` → `Docs/WorldArchitecture/WORLD_ARCHITECTURE_PLAN.md` → `WORLD_DECISIONS.md` → `WORLD_SYSTEM_IMPACT_MAP.md` → `WORLD_BOUNDED_BACKLOG.md`.
- **사람 Gate:** WorldSandbox 신규 scene, 2m/16/1m prototype baseline, WORLD-001 scope. WORLD-007 전 save schema, WORLD-008 전 D3D11 runtime nav, Gate 5 후 MainGame 통합 결정은 각각 별도 승인이다.
- **WORLD-001 조건:** 새 명시적 bounded-ticket 지시가 있어야 하며 최대 12경로, WorldSandbox/data/debug 전용이다. 실제 terrain mesh, terraforming, save, nav, shop/NPC를 동시에 넣지 않는다.
- **검증 경계:** AI Navigation 2.0.12 async update API 존재는 확인했지만 chunk seam/runtime 성능은 확인 못 했다. 직접 `Camera.Render()` 경로는 계속 금지한다.

## 11. BASELINE-CRAFTING-UI-FIX-001 인계 — 2026-08-05

- **현재 활성 ticket 없음:** Basic 제작 레시피 카드 차단 결함은 완료됐고 최종 상태는 `BASELINE_READY_FOR_WORLD_001`이다. WORLD-001은 다음 명시적 bounded ticket 전 자동 시작하지 않는다.
- **기준선:** `master@0b07d71e7dc6d259713a97d2011d181efa73b200`, Unity 6000.3.2f1, D3D11. 사람 입력 Smoke PASS와 Console `PASS_WITH_EXACT_ALLOWLIST`는 승인 상태다.
- **원인/수정:** runtime `Viewport(Image+Mask)`가 `Color.clear`라 생성된 두 카드를 전부 가렸다. `CraftingUI.cs`에서 Mask graphic을 opaque로 유지하고 `showMaskGraphic=false`로 배경을 숨겼으며 Content layout을 즉시 확정했다.
- **자동 증거:** `PA_CraftingRecipeCardValidator`가 Basic recipe/card 2/2, active 2, 각 672×96, viewport bounds, alpha, 결과/재료 보유량, 부족 상태 표시·차단, 충분 상태 Wood 2→Plank 1, B05 pulse를 D3D11 Play Mode에서 확인했다. ProcessingChain 회귀도 PASS다.
- **빌드/안전:** Runtime/Editor compile 오류 0, blocking exception/crash pattern 0, 신규 crash 0. 씬·프리팹·Save schema·Packages·ProjectSettings 변경 0.
- **캡처:** 알려진 Game View timeout을 재시도하지 않았다. 기능 계측이 통과했으므로 `CAPTURE_EVIDENCE_DEBT`; 사람에게 개별 캡처를 다시 요구하지 않는다.
- **비차단 backlog:** inventory drag ghost, developer overlay layout, Phone Hiring/Feed, shop/map readability, movable sales display, sale fallback tone, B06 pulse feel, customization/Tier capture evidence.
- **다음 우선순위:** 사용자가 `WORLD-001` bounded ticket을 명시하면 승인된 2m cell/16×16 chunk/1m level 0~6 custom mesh prototype을 `WorldSandbox` 전용으로 시작한다. `Prototype_FirstDay`는 Golden Regression Scene으로 보존하고 MainGame 통합·Save schema·Packages·ProjectSettings는 별도 승인을 유지한다.

## 12. M70 / WORLD-002 인계 — 2026-08-06

- **현재 milestone:** `M70 PLAYABLE WORLD ALPHA`, branch `milestone/world-alpha-70`. 사람 승인 baseline checkpoint는 `23ceda6`, WORLD-001 commit은 `776fd3a`다.
- **완료 상태:** WORLD-002는 `WORLD_002_COMPLETE`. 16×16 WorldSandbox의 level 0~6 terrace, 상면 256/절벽 280/정점 2,144 custom mesh, checksum `ADF9201BC8265BC5`, `MeshCollider`, 재질 2슬롯, 선택 dirty rebuild가 구현됐다.
- **증거:** `Logs/WORLD002_Validation_Final.log`와 `Logs/WORLD002_WORLD001_Regression_Final.log`. D3D11 Edit/Play, WORLD-001 회귀, blocking Console 0, Runtime/Editor 오류 0, 신규 crash 0이다.
- **복구 기록:** 첫 합성 seam 검사는 cliff cap까지 센 false negative였다. 상면 17지점 계약으로 좁힌 단일 재검증이 통과했다. WORLD-001 회귀의 첫 배치 명령은 공개 메서드 이름 오기였고 실제 `PA_WorldSandboxTools.RunValidation` 한 번으로 통과했다.
- **보호 상태:** Prototype_FirstDay/MainGame, Save schema/authority, Packages, ProjectSettings는 WORLD-002에서 변경하지 않았다. WorldSandbox는 Build Settings에 넣지 않았다.
- **다음 활성 ticket:** 기존 `WORLD_BOUNDED_BACKLOG.md` 순서의 WORLD-003 Single-Cell Raise/Lower Terraforming만 시작한다. generator, water/path, building, save, navigation, shop/NPC integration은 섞지 않는다.
- **다음 구현 기준:** level 0..6 clamp, 한 셀 raise/lower, 셀과 경계를 공유하는 Chunk만 dirty, mesh/collider 동기화, 실패 원자성, 취소/안전 경로, D3D11 검증을 최소 범위로 만든다.
- **캡처:** WORLD-002 선택 캡처는 생략해 `CAPTURE_EVIDENCE_DEBT`다. 사람 캡처는 요구하지 않는다.
- **Git:** 로컬 ticket commit만 허용되고 push/rebase/reset-hard/clean은 금지다. 현재 ticket commit 뒤 자동 진행하되 M70 완료, WORLD-010 완료, ticket commit 12개, hard blocker 또는 안전 실행 한계에서 멈춘다.

## 13. M70 / WORLD-003 인계 — 2026-08-06

- **완료 상태:** WORLD-003는 `WORLD_003_COMPLETE`. 읽기 전용 셀 collection 뒤에 원자적 one-cell elevation transaction, `(0,0)` 보호, 0..6 clamp, typed failure, 한 단계 undo가 있다.
- **입력:** WorldSandbox 좌클릭 선택, `R` raise, `F` lower, `Z` undo. overlay가 선택 level, 보호 상태, 성공/차단 이유와 revision을 표시한다.
- **dirty/rebuild:** 내부 셀은 소유 Chunk 하나, seam 셀은 cardinal 이웃까지만 dirty다. 성공/undo마다 `WorldChunkTerrain` visual/collider revision이 각각 한 번 증가한다.
- **증거:** `Logs/WORLD003_Validation.log`, `Logs/WORLD003_WORLD002_Regression.log`, `Logs/WORLD003_WORLD001_Regression.log`. D3D11, blocking Console 0, compile 오류 0, 신규 crash 0이다.
- **보호 상태:** WORLD-003에서 scene, Save schema/authority, Packages, ProjectSettings는 변경하지 않았다. water/path/building/NPC/NavMesh도 시작하지 않았다.
- **다음 활성 ticket:** 기존 backlog의 WORLD-004 Ground/Path Paint and Water Cell Prototype만 시작한다. ground/path/water transaction과 최소 prototype rendering/walkability까지만 다루며 save/building/nav는 섞지 않는다.
- **캡처/Git:** 캡처는 `CAPTURE_EVIDENCE_DEBT`. 로컬 ticket commit만 허용하며 push/rebase/reset-hard/clean은 금지다.

## 2026-08-06 WORLD-004 Ground/Path Paint and Water Cell Prototype — COMPLETE

- 기준은 `milestone/world-alpha-70@de1ada54edb4db2f5719c11ffc0826f9007c73ff`이며 완료 변경은 아직 commit하지 않았다.
- `WorldCellData`는 Grass/Soil/Sand/Rock, Dirt/Stone, water surface/depth를 보유하고 `IsFarmable`/`IsWalkable`을 dry/path/ground 상태에서 파생한다.
- `WorldSurfaceEditService`는 ground/path/water 편집을 preflight 뒤 원자 commit한다. 보호 셀, 최대 초과 수면, dry drain, path-under-water는 cell hash/revision/dirty Chunk를 바꾸지 않는다. 마지막 성공 편집만 `X`로 한 단계 되돌린다.
- `WorldChunkMeshBuilder`는 8개 visual material slot을 사용하고 물 상면·shoreline을 생성한다. water vertex와 terrain collider triangle stream의 교집합은 0이며 Play Mode raycast는 물 표면이 아니라 terrain bed를 맞는다.
- WorldSandbox 조작은 기존 좌클릭 선택/R/F/Z를 보존하면서 `G` ground, `T` path, `V` water, `X` surface undo를 추가한다. surface 변경은 visual revision만 올리고 collider revision은 유지한다.
- 검증 로그: `Logs/WORLD004_Validation.log`, `Logs/WORLD004_WORLD003_Regression.log`, `Logs/WORLD004_WORLD002_Regression.log`, `Logs/WORLD004_WORLD001_Regression.log`. 모두 Unity 6000.3.2f1 D3D11에서 PASS했고 blocking Console 0, 신규 crash 0이다.
- Runtime/Editor compile 오류 0이며 기존 CS8785/CS0414 경고만 남는다. 세 Scene의 working blob은 HEAD와 동일하고 Prefab/Save/Packages/ProjectSettings diff는 없다.
- 선택 캡처는 생략해 `CAPTURE_EVIDENCE_DEBT`다. 최종 상태 `WORLD_004_COMPLETE`; Git add/commit/push 없이 정지했다. WORLD-005는 명시적 다음 지시 전 시작하지 않는다.

## 2026-08-10 WORLD-005 Relocatable Building MVP — COMPLETE

- **기준/승인:** `milestone/world-alpha-70@c23f4be`; 사용자가 M70의 WORLD-005~010 연속 bounded-ticket 실행과 로컬 ticket commit을 선승인했다. push/rebase/reset-hard/clean은 금지다.
- **배치 권위:** `WorldBuildingPlacementService`만 WorldGrid occupancy를 쓴다. 기존 `GridService`, `OutdoorPlacementController`, `BuildManager`, `BuildingRegistry`는 수정하지 않았고 WorldSandbox에 복제하지 않았다.
- **기존 에셋:** `B09_StorageShed` prefab/BuildingData를 수정 없이 사용한다. sidecar는 2m 셀 4x3 footprint와 회전하는 외부 entrance `(1,-1)`를 정의하며 실제 인스턴스는 `StorageBox`를 유지한다.
- **플레이 경험:** 좌클릭으로 anchor를 선택하고 `B` 배치, `M` 이동, `Q/E` 회전, `Enter` 확정, `Escape` 취소한다. 실제 B09 renderer ghost가 valid/invalid 색과 typed 실패 이유를 표시한다.
- **트랜잭션:** 물/길/보호/점유/범위 밖/부적합 지면/uneven footprint/blocked entrance는 무변경 실패다. 성공 move는 old/new footprint 합집합을 한 번에 commit하고 실패 move는 위치와 occupancy를 보존한다.
- **셀 계약:** occupied cell은 non-walkable/non-farmable이며 terraforming과 surface edit가 `OccupiedCell`로 차단된다. save persistence는 WORLD-007 전까지 session-only다.
- **씬:** WorldSandbox만 변경했다. authored root 3, placement service/controller 각 1, direct asset GUID 2, embedded MonoScript 0이다. Prototype_FirstDay/MainGame, prefab, Save schema, Packages, ProjectSettings는 diff 0이다.
- **증거:** `Logs/WORLD005_Validation.log` 및 `WORLD005_WORLD004_Regression.log`, `WORLD005_WORLD003_Regression.log`, `WORLD005_WORLD002_Regression_Rerun.log`, `WORLD005_WORLD001_Regression.log`. 모두 Unity 6000.3.2f1 D3D11 `FINISHED_PASS`, blocking Console 0, 신규 crash 0이다.
- **컴파일/캡처:** Runtime/Editor 오류 0, 기존 CS8785/CS0414만 남는다. 캡처는 `CAPTURE_EVIDENCE_DEBT`이며 기능 차단이 아니다.
- **다음 활성 ticket:** 로컬 WORLD-005 commit 뒤 기존 backlog `WORLD-006 World Seed + Minimal Island Generator`만 활성화한다. 128x128은 provisional target이고 Save/NavMesh/gameplay integration은 아직 섞지 않는다.

## 2026-08-10 WORLD-006 World Seed + Minimal Island Generator — COMPLETE

- **기준:** `milestone/world-alpha-70@971d23d9c5c186299e33a59f263df0ab39773414`. 사용자의 연속 승인과 로컬 commit 허용은 유지되며 push/rebase/reset-hard/clean은 금지다.
- **정의:** generationVersion 1, provisional 128x128, 2m cell, 16x16 Chunk, level 0..6. 128x128은 구성 가능한 M70 표본이지 영구 save 계약이 아니다.
- **생성:** seed 기반 integer hash/fixed-point noise로 ocean border, irregular coast, meadow, forest, highland, river와 pond를 만든다. Unity/System RNG 전역 상태와 seed별 예외는 없다.
- **Gate:** start, independent flat 4x3 shop candidate/entrance, beach, meadow/forest/highland/pond activity anchors가 존재한다. dry cardinal cell graph는 최대 1 level step으로 모두 연결된다.
- **자원:** Forage/Timber/Stone/Fish candidate는 `(generationVersion, seed, kind, coordinate)` stable key를 사용한다. 실제 prefab·respawn·save는 후속 티켓 범위다.
- **WorldSandbox:** `J` 생성, `[`/`]` seed, `K` clear. 128x128 result는 64 custom mesh chunks, 16,384 top faces, eight materials, water/shoreline을 사용하고 per-cell GameObject가 없다. 컴포넌트는 시작 시 idle이다.
- **증거:** `Logs/WORLD006_Validation.log`에서 128 seed x 2, unique checksum 128, land 44.9~58.2%, 1,599ms, anchors/connectivity/resources 및 D3D11 64-chunk 렌더 PASS. `WORLD006_WORLD005_Regression.log`~`WORLD006_WORLD001_Regression.log` 모두 `FINISHED_PASS`다.
- **안전:** Runtime/Editor 오류 0, blocking Console 0, 신규 crash 0. Prototype_FirstDay/MainGame, prefab, Save schema, Packages, ProjectSettings diff 0. 캡처는 `CAPTURE_EVIDENCE_DEBT`다.
- **다음 활성 ticket:** WORLD-006 로컬 commit 뒤 preapproved `WORLD-006B Movable Shop Furniture`만 시작한다. World persistence와 NavMesh는 아직 시작하지 않는다.

## 2026-08-10 WORLD-007 World Persistence — COMPLETE

- **기준:** `milestone/world-alpha-70@a8e760ed2d17d41b9ff8f43781aaf2370b4b9acc`. 사용자의 M70 연속 승인과 로컬 ticket commit 허용은 유지되며 push/rebase/reset-hard/clean은 금지다.
- **스키마:** 현재 v11. v10 이하는 `LegacyFixed`로만 이행하며 기존 absolute building/placeable을 procedural payload로 자동 변환하지 않는다.
- **절차 월드:** seed/generationVersion에서 base를 재생성하고 height/ground/path/water sparse delta를 적용한다. B09 stable instance와 occupancy, `shop.interior` 가구, generated-resource state, safe-player 위치를 복원한다.
- **원자성:** definition/version/범위/중복/불변식/지원 building/resource key를 전부 preflight한 뒤 live world에 적용한다. 같은 payload를 두 번 복원해도 건물과 가구가 중복되지 않는다.
- **증거:** `WORLD007_Validation_Pass.log`, `WORLD007_SaveRoundTrip_Regression.log`, `WORLD007_WORLD004_Regression.log`, `WORLD007_WORLD005_Regression.log`, `WORLD007_WORLD006_Regression.log` 모두 PASS. compile 오류·blocking Console·신규 crash는 0이다.
- **보호 상태:** Scene/Prefab/Packages/ProjectSettings diff 0. LocalJson repository의 crash-safe temp/backup write, 구 validator 세 곳의 v10 고정 assertion, 캡처는 후속 부채다.
- **다음 활성 ticket:** 승인된 WORLD-007 로컬 ticket commit과 BUG recovery 기록 분리 commit 뒤 `WORLD-008 Reachability and Navigation Prototype`만 시작한다.

## 2026-08-10 WORLD-008 Reachability and Navigation Prototype — COMPLETE

- **기준:** `milestone/world-alpha-70@42d235bcf4443ce1cb5309b87f6a317e2dd16b37`. 사용자 연속 승인과 로컬 ticket commit 허용은 유지되며 push/rebase/reset-hard/clean은 금지다.
- **권위:** `WorldCellReachability`가 logical connectivity를, `WorldNavigationService`가 runtime sector NavMesh를 담당한다. 기존 AI Navigation 2.0.12만 사용하며 새 패키지나 production NPC rewrite는 없다.
- **sector:** 2×2 Chunk=32×32 cell, 1-cell overlap, equal-elevation grouped seam links. 128×128 표본은 4×4=16 surface다.
- **갱신:** Terraform/surface/building event의 dirty chunk를 owner sector와 실제 seam-sharing neighbor로만 변환하고 `NavMeshSurface.UpdateNavMesh`를 비동기 실행한다.
- **agent:** 갱신 전 pause, 최대 2m 재투영, complete-path 확인 후 repath/resume, 도착 뒤 path clear. 전용 agent가 seam 이동과 안정 정지를 통과했다.
- **배치/저장:** B09 move와 procedural restore preflight는 critical anchors 또는 entrance를 고립시키면 live mutation 전에 `CriticalRouteBlocked`/restore reject한다. schema는 v11 그대로다.
- **증거:** `WORLD008_Validation_Final.log`(logical 8,755 cells, local 12ms, generated 57ms, 16 sectors, Console 0), WORLD-007/005/006, InteriorCustomer, FinalDemoRoute 회귀 PASS.
- **부채:** InteriorCustomer pass 뒤 late-visitor teardown NavMesh 진단, 캡처 evidence. Scene/Prefab/Packages/ProjectSettings는 무변경이고 신규 crash는 0이다.
- **다음 활성 ticket:** WORLD-008 local commit 뒤 승인된 `WORLD-009 Existing Gameplay World Adapter`만 시작한다. 기존 Inventory/Crafting/Shop/Gathering/DayNight/NPC 권위를 복제하지 말고 adapter로 연결한다.

## 2026-08-20 — BETA-005 동기 증거 수정 완료, Unity license 차단

- branch/HEAD는 `milestone/gameplay-beta-85@9d2a81e`; dirty/staged/untracked는 승인된 BETA-005 13/0/0이다. 예상 밖 변경과 Scene/Prefab/Packages/ProjectSettings/Save schema 변경은 없다.
- `WorldAlphaPlayableController`의 실제 HUD rect를 단일 read-only 계약으로 만들고, BETA-005 validator가 `BeginNewGame()` 뒤 각 `TryBeginCustomerVisit` 성공 직후 profile·visit·고객 활성·라이브 HUD text·screen bounds를 동기 검사하도록 수정했다. 숨은 개발 Canvas 값도 로그하되 가시성 권위에는 포함하지 않는다.
- Runtime/Editor compile은 오류 0, 기존 CS8785/CS0414만 유지된다. `Logs/BETA005_D3D11_Validation_SynchronousPreference.log`는 Unity license 부재로 return code 198이며 `[BETA-005]`/graphics 초기화 0건이다.
- Unity 프로세스와 새 crash는 0이다. 유효한 Unity Editor license를 복구한 뒤 현재 변경을 보존하여 동일 validator에서 재개한다. 그 전에는 commit이나 BETA-006을 시작하지 않는다.

## 2026-08-21 — BETA-006 Phone Hiring and Feed Completion — COMPLETE

- **기준:** `milestone/gameplay-beta-85@0c9131b`; WorldSandbox runtime first, Prototype Golden regression-only.
- **휴대폰:** existing SmartphoneUI가 없을 때만 runtime Phone/EventSystem을 구성한다. P 진입, 4개 탭, on-screen bounds와 닫기/홈 이동을 검증했다.
- **채용:** 8개 후보 카드가 역할·비용·잔액·잠금/고용 상태를 보인다. C-02~C-09 원본 SkinnedMesh를 보존한 역할별 wrapper와 실제 HiringService 결제·스폰·중복 차단을 사용한다.
- **피드:** clear Mask 결함을 수정하고 실제 판매 전 empty state, 판매 후 item/price/buyer/time/category/quality/village direction 실시간 갱신을 연결했다.
- **증거:** `BETA006_D3D11_Validation`, BETA-005, VillageChangeSignal, Golden FinalDemoRoute PASS; compile 오류·blocking Console·신규 crash 0.
- **보호:** Scene/Packages/ProjectSettings/Save schema/authority와 원본 C-02~C-09 FBX 무변경. 캡처는 비차단 `CAPTURE_EVIDENCE_DEBT`.
- **다음:** 승인된 BETA-006 local commit 뒤 `BETA-007 Village Response and NPC Integration`을 자동 활성화한다. push하지 않는다.

## 2026-08-21 — BETA-009 Persistence and Recovery checkpoint

- **기준:** `milestone/gameplay-beta-85@46dbea9`; BETA-009는 `IMPLEMENTED_WITH_VALIDATION_DEBT`로 checkpoint한다.
- **구현:** additive gameplay envelope v12, procedural world payload v11/LegacyFixed 보존. SalesLog/Feed, village exact context, crop, B09 storage, player/hotbar/shop-open/WorldAlpha와 load-boundary/repeated-load 보호를 기존 SaveManager에 연결했다.
- **정적 결과:** Runtime/Editor compile 오류 0, diff/JSON PASS, Scene/Prefab/Packages/ProjectSettings 변경 0, native crash 0.
- **D3D11 부채:** 첫 실행은 validator CS0165, 두 번째는 invalid B01 fixture `(1,-1)`로 save 전에 중단. `(1,0)` 교정은 compile PASS지만 실제 restart/reload는 미실행이다.
- **최우선 다음 작업:** BETA-010의 첫 통합 검증에서 교정된 BETA-009 restart→restore→continue→same-save repeat-load 경로를 실행한다. 이후 BETA-008 Day 6~7와 BETA-007 resident response 부채를 같은 full-loop 계약에 통합한다.
- **금지:** BETA-009 COMPLETE 주장, 세 번째 단독 실행, push, Scene/Prefab/Packages/ProjectSettings 변경.

## 2026-08-21 — BETA-010 player-facing restore blocker

- **기준:** `milestone/gameplay-beta-85@6168d05`; BETA-010 activation docs와 player pose 재적용/validator 진단이 dirty다.
- **통과:** D3D11 fresh fixture, 판매/Feed/village pending, v12 save, Play restart, exact world checksum, B09 storage, player cell `(64,61)`.
- **반복 실패:** 저장 quaternion은 137°지만 최초와 교정 실행 모두 runtime facing이 identity라 `facingError=137`. 교정은 모든 restore consumer 뒤 pose를 재적용했으나 해결되지 않았다.
- **중단:** 두 실행을 모두 사용했다. 세 번째 Unity 실행, assertion 완화, 추가 추측 수정 금지. `M85_GAMEPLAY_BETA_COMPLETE` 선언 금지.
- **다음 최소 조사:** load 직후 adapter `PlayerRoot`, `FindGameObjectWithTag("Player")`, Player-tag 객체 수와 `PlayerController` 내부 `_smoothMoveDir/_currentSpeed`를 frame별로 계측한다. 그 증거로 단일 authoritative `RestorePose`를 정한 뒤 별도 승인된 실행에서 continue/repeat-load까지 이어 간다.
- **미검증:** BETA-009 continue/repeated load, BETA-007 hire→sale→Day2→dialogue, BETA-008 Day6–7/Week1, Golden/M70 full regression.

## 2026-08-25 Visual Baseline Audit Handoff

- **현재 기준:** `milestone/gameplay-beta-85@29fb98f`에서 감사 수행. 장기 구현 상태는 여전히 `HARD_BLOCKER_BETA_010_PLAYER_FACING_RESTORE`이며 임의로 완료 처리하지 않았다.
- **제품 진입점:** Build Settings 활성 씬은 `Prototype_FirstDay` 하나다. 타이틀/온보딩/Day 1 판매/감사/결산/격리 저장은 실행되지만 최신 생성 월드 M85 루프는 `WorldSandbox` 전용이라 일반 플레이에서 도달할 수 없다.
- **시각 기준선:** `Docs/VisualAudit/2026-08-25-29fb98f/VISUAL_BASELINE.md`와 PNG 16장. 모든 이미지를 직접 검사했다. 캡처 크기 476×1297이므로 목표 1920×1080은 아직 별도 gate다.
- **핵심 UX blocker:** 타이틀 뒤 HUD, MoneyHUD/objective overlap assertion, 상점 차양 카메라 가림, 판매대/상점 경계 불명확, primitive 채집 노드, 거대 고객 말풍선, 작고 잘린 인벤토리/결산.
- **저장:** 감사 전용 `savegame.json`만 사용했고 사용자 save는 2026-08-05 timestamp 그대로다. Prototype player position 복원은 PASS; 생성 월드 facing 137°는 기존 blocker다.
- **다음 우선순위:** 먼저 BETA-010 persistence recovery. 그다음 사람 승인된 scene integration, 1920×1080 camera/UI gate, shop readability, diegetic activity visuals, standalone build gate 순서다.
- **감사 도구:** `Assets/Editor/PA_VisualBaselineAuditCapture.cs`는 진단 전용이다. gameplay/scene/prefab을 수정하지 않는다.
- **주의:** 첫 Unity 실행이 untracked `.claude/settings.json`을 생성했다. 출처 불명이라 보존했으며 다음 작업에서 임의 삭제하지 않는다.

## 2026-08-31 — Summer Progress Presentation Handoff

- **발표 산출물:** 루트에 7장 PPTX/PDF, 5분 대본, 발표 근거, 여름방학 개발 감사 문서를 생성했다. 디자인 마스터 원본은 읽기 전용으로 사용하고 덮어쓰지 않았다.
- **발표 기준선:** 2026-06-26 이전 이동·인벤토리·상점·NPC·경제·기본 저장은 기존 기반으로 분리했다. 방학 성과는 루프 연결, WorldSandbox, NPC/경제 피드백, 저장·검증 확장으로 요약했다.
- **정확한 현재 판정:** Prototype Day 1은 안내 포함 `PARTIAL`, 최신 WorldSandbox는 일반 진입점에서 `UNREACHABLE`, BETA-010 facing 137°는 `BLOCKED`다. 1920×1080 standalone은 미확인이다.
- **검증:** PPT 7장·PDF 7쪽·1440×810, notes `[Sources]` 7/7, 1920×1080 렌더 7장 직접 검사 완료. Unity compile/PlayMode는 발표자료 작업 범위에서 확인하지 않았다.
- **다음 우선순위:** 기존 `BETA-010-PERSISTENCE-RECOVERY-001`을 그대로 유지한다. 이 발표 작업은 새 개발 티켓을 활성화하거나 milestone 상태를 바꾸지 않는다.
- **보호:** 코드·씬·프리팹·Packages·ProjectSettings·Save schema·사용자 save 무변경. 기존 dirty/untracked 파일은 보존했다.

## 2026-09-01 — Identity & Scope Audit Handoff

- **감사 산출물:** `PROJECT_PA_IDENTITY_AUDIT_2026-08-31.md`, `PROJECT_PA_CHARACTER_AND_ECONOMY_BIBLE.md`, `PROJECT_PA_IDENTITY_IMPLEMENTATION_MATRIX.md`, `PROJECT_PA_DEMO_IDENTITY.md`.
- **복원된 축:** `개척자 지원→상점 운영→NPC 생산/소비→채용→위임/자동화→마을 경제 성장`. 현재 낮 생활·밤 잡화점·판매 후 마을 변화는 이 축의 전면이고 역공급망은 백본이다.
- **발표 권고:** “개척 주민이 만든 자원을 매입·가공해 밤의 잡화점에서 가격을 결정하고, 채용과 위임으로 주민의 생활과 마을 경제를 키우는 3D 코지 경영 시뮬레이션.” 현재 고정 Identity 파일은 수정하지 않았다.
- **캐릭터 gap:** 보리는 전용 NpcProfile/DialogueData/경제 직업이 없고, 역할 프리팹 8종은 `Farmer_01` 등 역할형 프로필과 빈 bio를 쓴다. 미라/준/아를로/타라/펠릭스 프로필은 현재 GUID 참조가 없다. 새 이름이나 역할 매핑을 임의 확정하지 않는다.
- **데모 Lock 권고:** 유료 Farmer 고용→Wheat 매입→BreadLoaf 가공→가격 거절/구매→정산→Processed 다음 날 변화. 이 경로는 문서 권고일 뿐 구현 승인이 아니다.
- **현재 우선순위 불변:** `BETA-010-PERSISTENCE-RECOVERY-001`이 여전히 다음 단일 개발 티켓이다. WorldSandbox 일반 진입 불가, facing 137° blocker, 1920×1080 standalone 미검증도 유지한다.
- **보호/검증:** PPT, 코드, 씬, 프리팹, Packages, ProjectSettings, Save schema, 사용자 save 무변경. 문서 감사라 Unity compile/PlayMode/standalone은 실행하지 않았다.

## 2026-09-06 — CONTENT-000B Campaign Architecture / SELF-AUDIT PASS

- CONTENT-000을 PROVISIONAL CANON v1으로 채택한 사람 승인과 CONTENT-000B~010 순차 진행·검증·로컬 커밋 선승인을 기록했다. 승인 대기는 해소됐다.
- CONTENT_CAMPAIGN_DAY1_30.md와 CONTENT_IMPLEMENTATION_BACKLOG.md를 작성했다. 주요 사건 14개, 첫 주 사건 7개×17항목, 기능 노출 25개, 핵심 주민 8명의 관계 사건 24개·개인 요청 16개를 기존 시스템에 배정했다.
- 실제 생산 재고 매입, 고용 전 작업지 거래→고용 후 상점 운반, 전문가 입력/요청 소모 구분, additive 저장과 동일 인물 복원, 자유·회복일 및 실제 Day30 완료 근거를 명시했다.
- 검증: Tools/LoopEngineering/Test-ContentCampaignArchitecture.ps1 PASS. 증거 Logs/Content/CONTENT000B/ArchitectureValidation.json. Unity runtime/editor compile·D3D11·실제30일·save restart는 설계 티켓에서 실행하지 않았다.
- 다음: 이 설계 체크포인트 local commit 뒤 CONTENT-001 Opening & Bori를 자동 활성화한다. 이후 CONTENT-010까지 중간 승인 요청 없이 진행한다. 기존 BETA-010 facing 137° 실패는 미해결 기술 이력으로 보존했다.
- 기존 사용자 변경 및 별도 ART-000 자료는 보존하고 이 작업 기록의 추가분만 커밋한다. 런타임 코드·Unity 씬/프리팹·ProjectSettings·Packages·Save schema 변경 없음.

## 2026-09-06 — ART-000 Asset Intake / VERIFIED

- 목표 파일의 ART-000 범위에서 공식 CC0 6팩 ZIP을 원본 보존 COPY로 입고했다. 550종/2,590파일을 분류하고 Blender 33종, Unity 28종(새 FBX20+기존 Nature8 재사용)을 선정했다.
- 산출물: Docs/AssetProvenance/EXTERNAL_ASSET_REGISTRY.md, PROJECT_PA_ART_STYLE_GRAMMAR.md, Blender/Library/ProjectPA_AssetLibrary.blend, demo 요구·missing-model·8개 제작 배치 계획, 원본 비교 렌더33장/시트3장.
- Unity 6000.3.2f1 D3D11 전용 검사: 28개 hash/mesh/크기/바닥 pivot/URP 재질/프리팹 참조 왕복 PASS. Runtime/Editor compile 오류0; 기존 CS8785/CS0414 경고는 보존. 기존 runtime/scene/settings 보호 hash 변경0.
- Blender 4.5.13 공식 portable 체크섬 검증 후 33개 library 생성·재개방·packed texture·render PASS. 물고기 원본 동작6개씩 모두 library에 보존했다. 원본 mesh/ZIP 변경 없음.
- 무결성: Docs/AssetProvenance/final-integrity-report.json PASS — 2,886 checks, GUID1,199개 중 중복0, 신규 missing meta0. 자세한 요구별 판정은 ART000_COMPLETION_AUDIT.md.
- 확인 못 함: 최종 Unity 장면의 1920x1080 시각 품질/낮밤 그림자/interaction face, 실제 gameplay 및 save/load 회귀, 최종 Vertical Slice Demo 완주. 이번은 격리 입고·제작 계획이며 ART-001 이후 적용 검증이 필요하다.
- 다음 ART 권장: ART-001 Material Palette and Unity Import Normalization. 목표 파일 §21에 따라 후속 ART 자동 구현은 시작하지 않는다. 별도 CONTENT-000B~010 승인·활성 상태와 사용자 staging은 유지한다.
- 사용자 원본 이동/Unity 수동 import 요구 없음. 파일/출처/검증이 바뀌지 않는 한 완료된 ART-000 intake를 재실행하지 않는다.

## 2026-09-07 — CONTENT-001 Opening & Bori / VERIFIED

- WorldSandbox 새 캠페인에 독립 생활자 보리, 전용 Profile/Dialogue, 기존 C-01 기반 별도 외형을 연결했다. Golden 보리(Profile_Lumberjack), 기존 8역할·후보·경제식을 보존했다.
- 실제 PlayerInteraction 인사와 실제 가방 판매 재고로 A01을 기록한다. 다른 NPC 대화로 완료되지 않으며 +2는 하루 한 번, 순서 교환·중복 시작·반복 로드에도 보상/주민/재고 중복이 없다.
- 승인된 additive envelope v13에 캠페인 4필드를 추가하고 world payload v11 및 v12 recovery 필드 의미를 유지했다. 과거 저장에 캠페인 시작을 강제하지 않는다. 로드 후 직전 채집 성공 문구를 지워 현재 재고와 일치시켰다.
- 검증 PASS: Logs/Content/CONTENT001/OpeningValidation_Release.log, RuntimeCompile_Release.log, EditorCompile_Release.log, GoldenRegression.log, WorldOnboardingRegression.log, WorldDaytimeRegression.log, SaveRoundTripRegression.log. Compile 오류0, 기존 CS8785/CS0414 경고는 남음.
- 1920×1080 Runtime/Opening.png에서 한글·새 안내 영역 잘림/겹침을 확인했다. 최종 전체 미술·기존 휴대폰 패널·플레이어 표현은 후속 통합/사람 검토 대상. BETA-010 restart 후 facing 137° 문제와 실제 첫 달 완주는 이번 검증 범위 밖이며 해결로 표시하지 않는다.
- 다음: CONTENT-001 선택 local commit 직후 CONTENT-002 First Shop Night 자동 활성화. 같은 보리를 실제 소비자로 연결하고 첫 판단과 첫 판매를 구분한다. 중간 승인 요청 없이 CONTENT-010 선승인 범위를 유지한다.
- 실패 교정 이력: editor sync 공개 API, 구체 Collider 선행 생성, batch 캡처를 일반 D3D11 GameView로 교정해 각각 해소. native crash 없음. 기존 사용자 dirty와 ART-000 기록 보존, 씬/기존 프리팹/ProjectSettings/Packages 변경0, push 없음.


## 2026-09-07 — 데모 창작 결정 상담 / 제안만 기록

- 사용자 요청에 따라 현재 개발 상태와 오프닝·인물·첫 판매 결과·첫 달 흐름의 창작 결정 가이드를 `Docs/DEMO_CREATIVE_DECISIONS.md`에 작성했다.
- 8월 25일 PROJECT_STATE와 9월 7일 CONTENT-001 완료/CONTENT-002 ACTIVE를 구분했다. CONTENT001 기존 로그의 CHECKS_PASS/VALIDATION_PASS를 읽었으며 이번 세션에서 새 runtime 검증을 수행한 것은 아니다.
- 배 도착→목수 루카의 길/가게 안내→보리의 생활 필요/첫 손님 역할은 미확정 제안이다. 현행 루카 Day4 소개와 차이를 명시했고 기존 캐논·콘텐츠 순서·선승인 범위·활성 티켓은 변경하지 않았다.
- 다음 상담 우선순위: 시작 장면, 안내자 역할, 첫 마을 변화, 체험 길이/마지막 장면에 대한 사용자 생각을 받는다. 구현 작업의 기존 우선순위 CONTENT-002는 유지한다.
- 문서 상담만 수행. 컴파일/Play Mode/빌드/재시작은 이번에 실행하지 않아 확인 못 함. 기존 사용자 미커밋 변경 보존, 코드·씬·저장 변경 및 commit/push 없음.


## 2026-09-07 — 무인도 정착·동행 선택 구상 검토 / 설계 상담

- 사용자 구상: 본사 교육→동행 NPC 선택→배로 무인도 이동→거주 겸 상점/주민 텐트 직접 배치→입주민에 따른 기술 트리→입주 후 고용. 적용 적합성을 검토하고 `Docs/DEMO_CREATIVE_DECISIONS.md`에 기존 정착지 권고의 한계를 명시했다.
- WORLD_NORTH_STAR의 절차 섬/자유 배치 방향과 부합한다. 현재 WorldGameplayAdapterService의 시설 자동 생성, HiringService의 고용 시 Instantiate, 레시피의 기존 시설/Tier 조건을 코드로 확인했다. 모든 주거/입주/해금이 이미 구현된 것으로 보고하지 않는다.
- 권고: 공통 설치 키트와 기본 채집, 입주→기술 전수와 유료 고용 분리, 주민 동일 ID·주거 앵커 저장, 기존 목수/벌목꾼·광부/대장장이 역할 구분. 선택하지 않은 분야는 후속 입주로 열고 첫 판매와 실제 마을 반응을 유지한다.
- 다음 설계 우선순위: 동행/입주/전수/고용 계약과 시작 설치 조건, 기존 날짜별 캠페인 충돌 범위 정리. 기존 활성 CONTENT-002나 선승인 sequence는 이 상담에서 변경/실행하지 않았다. 새 구상 전체의 구현 승인을 기존 승인에서 추론하지 않는다.
- 새 compile/Unity/빌드/save restart는 실행하지 않아 확인 못 함. 코드·씬·프리팹·저장·캐논 무변경, 기존 dirty 보존, commit/push 없음. 문서 diff 검사를 수행한다.


## 2026-09-07 — 1차 생산자·본사 기술 전수·섬 특화 / 아이디어 수집

- 사용자 추가 구상: 시작 동행은 1차 생산자(목수/광부/낚시꾼/곤충 채집꾼/농사꾼), 구성에 따른 초기 상품 차이, 돈을 통한 입주 확대, 직업별 자원 본사 제출로 기술 해금, 2차 생산과 관광·과학 등 섬 특화, 정기 배 방문객 연결. 상세는 Docs/DEMO_CREATIVE_DECISIONS.md 마지막 절.
- 이전 에이전트의 NPC 직접 기술 전수안과 본사 제출안을 구분했다. 목수의 1차 역할 표현은 보존하고 기존 전문가/벌목꾼 재배정, 입주비와 고용비의 관계, 가격·해금 수치, 후반 기능의 데모 필수 여부는 미확정으로 남겼다.
- 다음 대화 우선순위: 사용자가 이어서 서술할 아이디어를 누적한다. 확정 질문/구현 착수로 브레인스토밍을 중단하지 않는다. 기존 활성 티켓과 선승인 범위는 이번 기록에서 변경/실행하지 않는다.
- 문서만 수정했다. 신규 compile/Unity/빌드/밸런스 실험은 실행하지 않아 확인 못 함. 기존 사용자 변경 보존, 코드·씬·저장·캐논 무변경, commit/push 없음.


## 2026-09-07 — VS-PRESENT-001 P0 / PASS · STOP

- 별도 PA_DepartureTutorial 씬에서 실제 키보드 이동→나무 상호작용→열매3→ShopSlot 진열→ShopPriceUI 7G 확정→NPC 접근/평가→기존 Economy 0G→7G→출항 인증 완료를 연속 통과했다.
- Runtime/Editor compile 오류0(기존 CS8785/CS0414 경고 유지), D3D11 Play Mode, serialized reference 검사와 1920×1080 실캡처 PASS. 근거 Docs/Presentation/2026-09-08/P0-validation-excerpt.txt, P0-validation.json, P0-integrity.json.
- Blender 기존 Departure 자산5종과 ART-000 tree/cargo를 적용했다. 최종 화면은 01_PA_Company_FirstView.png / 02_Tutorial_PriceAndReaction.png. 전체보기는 실제 씬에서 HUD만 숨긴 캡처, 가격/반응은 실제 판매 후 남은 열매1개 재진열 상태다.
- 개발 진입: Project PA > Presentation > Open Departure Tutorial > Play. WASD/SPACE, 실습 가격7G. 튜토리얼 세션만 유지하며 SaveManager가 없어 기존 campaign save를 쓰지 않는다. NEW GAME/standalone 통합 및 저장 재개는 이번에 확인 못 함.
- 기존 dirty CONTENT/Save/World 코드를 수정하거나 이 checkpoint에 넣지 않았다. 이 작업은 현재 working tree의 기존 ShopPriceUI.OnPriceConfirmed 및 PurchaseFeedbackPresentationController.OnDecisionRecorded 관찰 seam을 사용하므로, checkpoint 단독 checkout은 기존 CONTENT 작업의 별도 checkpoint 없이는 재현 가능한 clean baseline이 아니다.
- P1/P2 NOT_STARTED. 사용자 최신 지시에 따라 고가 거절 edge case·추가 제작·다음 단계 구현은 수행하지 않고 STOP. 다음 세션은 사람의 직접 플레이 확인/명시 지시를 기다린다.


## 2026-09-07 — 현재 작업 통합 checkpoint / push 승인

- 사용자가 현재까지 작업의 commit/push를 명시 승인했다. 기존 P0 0b4e511에 이어 보존했던 CONTENT 코드·관찰 이벤트·설계/발표 자료를 함께 checkpoint한다. CONTENT-002의 미완료 검증 상태와 BETA 저장 부채는 해소됐다고 표시하지 않는다.
- Runtime/Editor compile 오류0, 기존 경고3, diff 검사 PASS. 증거 Logs/VS_PRESENT_001/PrePushCompile_Restored.log. 이번 Git 작업에서 새 Play Mode/저장/빌드 검증은 하지 않았다.
- 기존 Word 문서 삭제 상태는 그대로 기록한다. 로컬 개인 플러그인 설정 .claude/settings.json은 commit 대상에서 제외한다. 강제 push/이력 재작성 없음.
- P0가 사용하는 ShopPriceUI/PurchaseFeedback 관찰 이벤트가 이번 checkpoint에 포함되므로, 이전 P0 보고서의 '미커밋 이벤트 의존' 제한은 이 통합 checkpoint에서 해소된다. clean checkout 실행은 별도 수행하지 않았다.
- 다음은 사람의 P0 수동 확인/명시적 후속 지시다. P1/P2 및 CONTENT 후속 구현은 자동 시작하지 않는다.
