# Actual game-loop map

2026-09-27 새 standalone Candidate-ShopPolish-20260927-194240: 실제 타이틀 → NEW GAME → 기존 저장 확인 → PA_DepartureTutorial STEP1 화면까지 VERIFIED. 이후 OS D키 입력의 실제 이동 확인 실패; 섬 정착→Report standalone 미검증. [실행 증거/중단 경계](../../Logs/VisualQA/ReportCandidate-20260927/DELIVERY_REPORT.md). 아래 Editor 루프 근거는 standalone 완주를 대체하지 않는다.

Demo256 entrance (2026-09-25): real Hotbar Shop/Base + two tents -> Established -> child BuildingEntrance -> same-footprint cutaway -> exit -> reentry VERIFIED with InputSystem E/S/W. Same operating Shop/world placement/traversal; Golden Tier1 unchanged. 2026-09-26 site overlap cleared with pickup preservation; competing fade rejection then real door retry/roundtrip PASS. Natural16→20 sun/ambient change and NightReady/clock stop also verified. 2026-09-27: actual pickup→E stock→E price/confirm→OPEN action→autonomous tourist reject/buy→CLOSE action→Report is verified, stock1→0 and money/report8G. OPEN/CLOSE used public management UI actions; NPC purchase was autonomous, unlike the earlier direct API proof. Full continuous human route remains HUMAN-UNVERIFIED. [Evidence](../../Logs/VisualQA/T2-A/implementation-20260925/EVIDENCE.md).

현재 구현 경로 (2026-09-21): 기존 Departure 교육 → 정확한 동행 2명 → 임시 항해/전환 overlay → 실제 Demo256 항구/보급/탐험 → held placeable preview → Shop/Base + Resident Tent 2 → License Point/Root → Field Workbench/가판대 → 대표 Mining 도구 제작·선택적 NPC 지급 → GameClock 연속 태양 회전/16→20시 Sunset/Night → 1개 이상 진열 후 OPEN → 실제 판매 → CLOSE → Pioneer Report 관리 패널. 기존 Human 증거와 `Logs/FirstDayStudio/Play-20260917-134119` 84-check PASS, 새 `Logs/OpeningDemoMustPath/result.txt` D3D11 PASS를 합쳐 Pioneer Report까지 기능 경로를 확인했다. 자연 일몰과 GameView 조명 변화는 2026-09-26 검증했으며, 인간 연속 완주와 전체 시각 품질은 미검증이다.

16×16 FirstIslandSettlement/FirstProduction과 별도 Management Hub는 이 경로에서 실행하지 않는다. 전환 전 [PASS 1 기능 검사](../../Logs/FirstDayStudio/Play-20260917-134119/result.txt)는 84 PASS/오류 0이며, 새 배치·영업·Report 경로는 [MUST PATH 결과](../../Logs/OpeningDemoMustPath/result.txt) PASS/Console 오류 0이다. 시각 게이트는 미통과다.

2026-09-10 / `4374b2a`. **실행 경로 세 개를 분리한다.** 계획상 하나의 게임이라는 이유로 연결선을 추가하지 않는다. 현재 상태는 [CURRENT_STATE](CURRENT_STATE.md), 재사용 API는 [CAPABILITY_REGISTRY](CAPABILITY_REGISTRY.md).

## 제품/Golden 진입

`Prototype_FirstDay` (Build Settings 첫 활성) → 기존 타이틀 NEW GAME **CONNECTED** → `PA_DepartureTutorial` **CONNECTED** → 기존 동행/항해 → `WorldSandbox` Demo256.

제품 진입 연결 근거는 `PlayableDayScenarioController.OnPrimaryPressed`가 기존 `DepartureTutorialController.SceneName`을 Single load하는 코드와 Build Settings의 세 활성 씬이다. Runtime/Editor compile과 Unity script validation은 PASS. 갱신된 `PA_Integration01Checks`는 실제 Build 첫 씬과 NEW GAME 핸들러부터 검사하지만 이 변경 후 targeted Play는 아직 실행하지 않았다.

저장 기록이 없으면 NEW GAME 한 번, 저장 기록이 있으면 기존 확인 화면의 `새 게임 시작`까지 누르면 `PA_DepartureTutorial`로 진입한다. 타이틀 pause의 시간 배율은 로드 전에 복원한다. Continue와 기존 legacy Day 1 내부 경로는 이 티켓에서 재작성하지 않았다.

## 출항 Vertical Slice — 최근 검증된 경로

| 단계 | 상태 | 실제 연결 |
|---|---|---|
| Departure 씬 개발용 진입/스폰 | VALIDATED | builder가 배치한 기존 시스템. 일반 RuntimeBinder는 이 씬에서 반환 |
| 이동 인증 | VALIDATED | PlayerInputHandler → PlayerController → checkpoint |
| 채집/열매 3 | VALIDATED | PlayerInteraction → tree → Inventory |
| 1개 진열/가격 확정 | VALIDATED | 기존 ShopSlot → ShopPriceUI |
| NPC 평가/판매/금액/빈 진열대 | VALIDATED | NpcController → PurchaseEvaluator → ShopSlot → EconomyService/SalesLogManager |
| 인증 완료/동행 2명 확정 | VALIDATED | 실제 마우스 CTA 및 Enter; `DepartureCompanionSelection` |
| 페이드/배/선택 NPC 2/섬 도착 | VALIDATED | `DepartureVoyagePresentation`, 기존 이동·카메라·WorldGrid |
| 거점 1/거처 2/동행자 입구 이동 | VALIDATED | `FirstIslandSettlementController` → 기존 placement/NavMeshAgent |
| 정착 저장/다시 진입·복원 | VALIDATED | checkpoint v15 SaveManager/LocalJsonSaveRepository. dirty P4 v16 제외 |
| 동행 작업/첫 생산 | PARTIAL / 저장 BLOCKED | 선택 동행자 → 프로필 → WorksiteBinding → 기존 Producer/ProcurementPolicy 연결. 바인딩 정적·managed·Editor 자산 검사 PASS, Play 0회. dirty P4 저장 거절과 전체 플레이 검증 부채 유지 |
| 기존 밤 영업/다음 날 생활로 연결 | PLANNED | “첫 영업 준비” 목표 문구가 실제 다음 플레이 연결을 뜻하지 않음 |

근거: [최종 연속 검사](../90_PRESENTATION/Evidence/2026-09-10/GameFeel/FinalRoute-checks.txt). 검증 입력은 기존 입력/UI를 사용하며 P3 배치는 기존 preview/commit API를 사용한다. 모든 배치 조작을 사람이 마우스로 완주했다는 뜻은 아니다.

## WorldSandbox — 별도 구현 경로

개발용 `BeginNewGame` **CONNECTED** → 생성 섬/온보딩 **CONNECTED** → 채집·낚시·광질·농사 **CONNECTED** → 제작·가공 **CONNECTED** → 진열/가격/개점/고객 반응 **CONNECTED** → 정산·다음 날 마을 반응 **CONNECTED** → 7일/고용·캠페인 **PARTIAL** → 저장·fresh reentry 통합 **BLOCKED**.

근거: `WorldAlphaPlayableController`의 WorldSandbox 한정 bootstrap, `WorldGameplayAdapterService`, `CampaignOpeningController`, `CampaignFirstShopNightController`, 기존 M85 검증 이력. 기능별 과거 검증은 존재하지만 최신 dirty 전체의 한 번 연속 완주 결과는 없다. 전체 Day 1–30 **PLANNED/PARTIAL**이며 승인 Canon·시나리오 표만으로 구현 완료라고 표기하지 않는다.

WorldSandbox direct loop (2026-09-15): existing Hotbar selects Axe/Pickaxe -> PlayerInteraction proximity/Space -> generated Timber/Stone three hits -> Inventory.AddInstance -> existing resource-state depletion. Required gameplay acceptance PASS; human visuals deferred. This does not validate the separate long campaign/save loop.

WorldSandbox fishing/bug loops (2026-09-16, targeted PASS): generated Fish/shore activity -> existing PlayerInteraction/Space cast -> real wait/BITE prompt -> Space -> existing daily Fish reward/Inventory. Meadow activity region -> three drifting butterflies -> Hotbar Net/Space -> front/reach check -> Inventory.AddInstance -> session-local critter depletion. Full bags preserve each opportunity; repeated rewards are rejected.

## 설계상 목표 — 위 실행 상태와 구분

낮 직접 생활 → 재료/가공품 확보 → 저녁 진열/가격 → 밤 구매/거절 → 매출/정산 → 상점 성장·주민 생활/마을 변화 → 저장/다음 날. [승인 Canon](../01_GAME_DESIGN/Canon/CONTENT_CANON_BIBLE.md)과 [정체성](../01_GAME_DESIGN/Canon/PROJECT_PA_IDENTITY.md)을 따른다. 문서 정리로 새로운 Canon을 채택하지 않는다.

WorldSandbox direct placement (2026-09-16, targeted PASS): existing demo bootstrap -> canonical Hub/Shop kits in Hotbar -> Space/PlayerInteraction -> near-player world placement preview -> WASD/R -> Space or click/CommitPreview -> selected InventorySlot minus one on success -> normal movement. Esc cancels without consumption or pause. Existing world persistence restores Hub/B01 identities; placed B01 retains Shop and registered ShopSlots. Stocking/customer sales were not exercised in this ticket.

WorldSandbox placed-shop operation (2026-09-16, targeted PASS): Shop Kit -> existing placed B01 -> player Space on front ShopSlot -> one owned inventory item -> Space/ShopPriceUI confirm -> night ShopOpenSign/Space -> CustomerArrivalController automatic invitation -> real NpcController/PurchaseEvaluator -> rejection preserves stock or purchase clears one product -> EconomyService/SalesLogManager exactly once -> restock. This connects the previously unvalidated placed-shop sales edge; broad save/campaign regression remains outside this ticket.

INTEGRATION-01 demo entry (2026-09-16, VALIDATED): PA_DepartureTutorial certification -> existing companion choice/voyage -> arrival fade -> DemoRouteController scene bridge -> WorldSandbox Demo256 (16x16 island unloaded) -> existing tools/kits only -> Forest Wood / Highland Ore / Coast Fish / Meadow Butterfly -> Hub Kit / Shop Kit placement -> InventoryUI-to-Hotbar / ShopSlot / ShopPriceUI / night sign OPEN -> automatic real NPC purchase -> EconomyService +1G / SalesLog +1 / DemoSucceeded once -> continued movement. One continuous session, 90 checks, runtime errors 0. The validator now starts at Prototype_FirstDay and invokes the real NEW GAME entry before this previously validated route; that expanded validator has compile/static coverage but has not been rerun after the entry change. Full save/campaign loop is not promoted.
