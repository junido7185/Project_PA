# DEMO_FLOW — Task 002 현재 데모 플로우 문서화

작성: 2026-07-10
범위: `Assets/Scenes/Prototype_FirstDay.unity` Day 1 데모 루트
방식: 텍스트/코드 기반 조사만. Unity 씬 로드, Play Mode, 빌드, 테스트 실행 없음.

## 확인한 출처

- `README.md`의 `Demo Route` 10단계
- `AI_WORKFLOW/03_TASKS/PROJECT_CODE_INDEX.md`
- `Assets/Scenes/Prototype_FirstDay.unity` 파일 존재 여부
- `Assets/Scripts/UI/PlayableDayScenarioController.cs`
- `Assets/Scripts/PA_RuntimeSceneBinder.cs`
- `Assets/Scripts/DayNightShopLoopController.cs`
- `Assets/Scripts/ShopSlot.cs`
- `Assets/Scripts/UI/ShopPriceUI.cs`
- `Assets/Scripts/NpcController.cs`
- `Assets/Scripts/PurchaseEvaluator.cs`
- `Assets/Scripts/EconomyService.cs`
- `Assets/Scripts/SalesLogManager.cs`
- `Assets/Scripts/SaveManager.cs`
- `Assets/Scripts/UI/SmartphoneUI.cs`
- `Assets/Scripts/AuditResultUI.cs`

## 코드 기준 핵심 루트

`README.md`의 데모 루트는 발표용 10단계이고, 실제 코드의 Day 1 진행 관문은 `PlayableDayScenarioController.Stage` 기준으로 더 좁게 관리된다.

| 코드 Stage | 완료 조건 |
|---|---|
| `TalkToNpc` | `DialogueUI.IsOpen` 감지 |
| `StockShopSlot` | 하나 이상의 `ShopSlot`이 비어 있지 않음 |
| `SetPrice` | `ShopPriceUI.ConfirmCount` 증가 및 하나 이상의 진열 슬롯 가격이 0보다 큼 |
| `WaitForPurchase` | `EconomyService.CumulativeRevenue`가 기준 매출보다 증가 |
| `OpenAuditApp` | `SmartphoneUI.IsOpen`이고 `CurrentTabIndex == 0` 감지 |
| `SaveProgress` | `PlayerInputHandler.OnSave` 입력 감지 |
| `Done` | `ShowDaySummary()`로 요약 화면 표시 |

시작 전 온보딩은 같은 컨트롤러의 `StartupStep`으로 분리되어 있다: `Name`, `Briefing`, `MapSelect`, `PhoneIntro`, `Supplies`, `Arrival`.

## README Demo Route 대조

| README 단계 | 데모 의미 | 실제 코드 진입점 | 대조 결과 |
|---|---|---|---|
| 1. Start the first-day scene. | 첫날 씬 진입 | `Assets/Scenes/Prototype_FirstDay.unity`, `PA_RuntimeSceneBinder.BindLoadedScene()`, `PlayableDayScenarioController.Start()` | 씬 파일 존재는 확인. 씬 로드/Play Mode는 미실행. |
| 2. Follow the objective guidance. | 목표 안내와 온보딩 진행 | `PlayableDayScenarioController.Start()`, `StartupStep`, `BuildObjectiveUI()`, `BuildFlowUI()`, `CompleteStartupFlow()` | README는 한 단계로 적지만 코드는 이름 입력, 브리핑, 지도 선택, 폰 안내, 보급품, 도착 단계로 나뉜다. |
| 3. Talk to the first guide/settler NPC. | NPC와 첫 대화 | `PlayableDayScenarioController.Stage.TalkToNpc`, `DialogueUI.IsOpen`, `NpcDialogue` | 코드는 대화 UI가 열린 사실로 완료를 감지한다. 특정 NPC Inspector 연결은 미확인. |
| 4. Stock a product into a shop slot. | 판매대에 상품 진열 | `ShopSlot.Interact()`, `ShopSlot.TryStockFromPlayer()`, `PlayableDayScenarioController.AnyShopSlotStocked()` | 빈 슬롯 상호작용이 인벤토리/핫바 아이템을 진열하고, 컨트롤러가 슬롯 상태를 감지한다. |
| 5. Open the price UI and confirm a price. | 가격 설정 확정 | `ShopSlot.Interact()`, `ShopPriceUI.Open()`, `ShopPriceUI.ConfirmCount`, `PlayableDayScenarioController.AnyShopSlotPriced()` | 찬 슬롯 상호작용이 가격 UI를 열고, 확인 시 `displayPrice`와 `ConfirmCount`가 갱신된다. |
| 6. Let an NPC customer approach and evaluate the product. | 손님 접근과 구매 판단 | `PlayableDayScenarioController.ForceFirstBuyerVisit()`, `NpcController.SetShoppingPriority()`, `NpcController.TryForceShop()`, `NpcController.EvaluateCurrentSlot()`, `PurchaseEvaluator.Evaluate()` | `WaitForPurchase` 단계 진입 시 첫 손님 방문을 강제로 유도한다. 구매 평가는 NPC 성향/가격/품질/카테고리 기반이다. |
| 7. Read the NPC buy/reject feedback bubble and confirm sale/money feedback through the HUD. | 구매/거절 피드백과 수익 확인 | `NpcController.BuildPurchaseFeedback()`, `NpcBubbleUI`, `PurchaseFeedbackPresentationController`, `ShopSlot.TryPurchaseByNpc()`, `EconomyService.Deposit()`, `SalesLogManager.RecordSale()`, `MoneyHUD` | 판매 성공 시 돈과 누적 매출이 증가하고 판매 로그가 기록된다. HUD 시각 출력은 코드상 연결만 확인, 화면 확인은 미실행. |
| 8. Open the smartphone audit app to show the next tier/growth target. | 감사 앱에서 다음 성장 목표 확인 | `SmartphoneUI.SelectTab(0)`, `SmartphoneUI.CurrentTabIndex`, `AuditResultUI.Refresh()`, `TierService`, `EconomyService` | 시나리오 컨트롤러는 폰이 열리고 0번 탭일 때 감사 앱 확인으로 처리한다. |
| 9. End the day flow and explain the summary: revenue, feedback, next action, and growth requirement. | 저장 후 Day 1 요약 | `PlayableDayScenarioController.Stage.SaveProgress`, `PlayerInputHandler.OnSave`, `SaveManager.SaveGameAsync()`, `PlayableDayScenarioController.ShowDaySummary()` | 문서와 코드 차이: README 단계에는 저장 입력이 명시되지 않지만 코드는 `SaveProgress`를 거쳐야 `Done` 요약으로 간다. |
| 10. Explain the market stall as the visible operating hub: supply, processing, price, purchase judgment, revenue, and reinvestment. | 좌판/상점 허브 설명 | `ShopSlot`, `ShopPriceUI`, `PurchaseEvaluator`, `EconomyService`, `SalesLogManager`, `ProcessingOpportunityController`, `VillageChangeSignalController` | 문서와 코드 차이: 발표 설명 단계에 가깝고, `PlayableDayScenarioController.Stage`의 별도 완료 관문은 아니다. |

## 단계별 진입점 상세

1. 씬 시작
   - `Prototype_FirstDay.unity` 파일은 `Assets/Scenes`에 존재한다.
   - `PA_RuntimeSceneBinder`는 씬 로드 뒤 `[Services]`, UI, 스마트폰 앱, NPC/플레이어 훅을 보강한다.
   - 핵심 서비스 보강 목록에는 `EconomyService`, `GameClock`, `SalesLogManager`, `DayNightShopLoopController`, `SaveManager`, `ItemRegistry` 등이 포함된다.

2. 온보딩과 목표 안내
   - `PlayableDayScenarioController.Start()`는 목표 UI/플로우 UI를 만들고 기준 매출/돈/가격확정 수를 캡처한 뒤 입력 구독을 한다.
   - `showStartupFlow`가 켜져 있고 시작 흐름이 끝나지 않았으면 `BeginStartupFlow()`로 온보딩을 시작한다.
   - 온보딩 완료 시 `EnsureRuntimeStarterSupplies()`가 판매 가능한 보급품을 런타임으로 채운다.

3. NPC 대화
   - 첫 진행 관문은 `TalkToNpc`다.
   - 컨트롤러는 특정 대화 내용이 아니라 `DialogueUI.IsOpen`을 감지해 다음 단계로 넘어간다.

4. 상품 진열
   - `ShopSlot.Interact()`는 슬롯이 비어 있으면 `TryStockFromPlayer()`를 호출한다.
   - `AnyShopSlotStocked()`는 씬의 `ShopSlot` 중 하나라도 비어 있지 않으면 완료로 본다.

5. 가격 확정
   - 이미 찬 `ShopSlot`과 상호작용하면 `ShopPriceUI.Open(slot)`이 호출된다.
   - 가격 확인 시 `ShopPriceUI`가 슬롯의 `displayPrice`를 갱신하고 `ConfirmCount`를 증가시킨다.
   - 컨트롤러는 `ConfirmCount` 증가와 `displayPrice > 0` 슬롯을 함께 확인한다.

6. 손님 평가
   - `WaitForPurchase` 단계에서 `ForceFirstBuyerVisit()`가 첫 손님 방문을 유도한다.
   - `NpcController.EvaluateCurrentSlot()`은 `DayNightShopLoopController.IsShopOpenForCustomers`를 먼저 확인한다.
   - 영업 가능 상태면 `PurchaseEvaluator.Evaluate()`로 구매/거절 판단을 만든다.

7. 판매, 피드백, 매출
   - 구매 결정 시 `ShopSlot.TryPurchaseByNpc()`가 실제 판매 처리를 담당한다.
   - 판매 성공 경로는 `EconomyService.Deposit()`으로 돈과 누적 매출을 증가시키고, `SalesLogManager.RecordSale()`로 이력을 남긴다.
   - `PlayableDayScenarioController`는 누적 매출이 기준값보다 커지면 판매 단계를 완료한다.

8. 감사 앱
   - `SmartphoneUI`는 시작 시 자동으로 감사 탭을 열지 않고 홈으로 돌아간다.
   - 감사 앱은 `SelectTab(0)`으로 열리는 0번 탭으로 확인된다.
   - `AuditResultUI.Refresh()`는 티어, 누적 매출, 평판, 감사 정보를 표시한다.

9. 저장과 요약
   - `PlayableDayScenarioController`와 `SaveManager` 모두 `PlayerInputHandler.OnSave`를 구독한다.
   - 입력 감지 시 컨트롤러는 `SaveProgress`를 완료하고, `SaveManager.SaveGameAsync()`는 실제 저장을 수행한다.
   - 이후 `Done` 단계에서 `ShowDaySummary()`가 Day 1 요약 화면을 표시한다.

10. 상점 허브 설명
    - README의 10단계는 코드 관문이라기보다 발표자가 상점 루프를 해설하는 단계다.
    - 관련 시스템은 진열(`ShopSlot`), 가격(`ShopPriceUI`), 구매 판단(`PurchaseEvaluator`), 수익(`EconomyService`), 로그(`SalesLogManager`), 성장/변화 신호(`AuditResultUI`, `VillageChangeSignalController`)다.

## 문서와 코드 차이

- README는 10단계 발표 루트이고, 코드는 `StartupStep` 온보딩과 `Stage` 7개 상태로 진행을 판정한다.
- README 9단계는 "End the day flow"로 적혀 있지만, 코드상으로는 `SaveProgress` 단계가 있으며 저장 입력 감지 후에야 `Done` 요약으로 간다.
- README 10단계의 market stall 설명은 코드의 별도 `Stage`가 아니라 발표/해설 단계로 보인다.
- README의 과거 검증 통과 기록은 이번 작업에서 재실행하지 않았다. 이번 문서는 현재 텍스트와 코드만 기준으로 작성했다.

## 확인하지 못한 점

- Unity Editor 실행, 씬 로드, Play Mode, 빌드, 테스트를 실행하지 않았다.
- `Prototype_FirstDay.unity`의 Inspector 연결, 배치된 NPC/ShopSlot 수, 실제 UI 표시 상태는 확인하지 못했다.
- `NpcDialogue`가 README의 "first guide/settler NPC"에 정확히 어떤 오브젝트로 연결되는지는 씬을 열지 않아 확인하지 못했다.
- HUD/말풍선/스마트폰 감사 앱의 실제 화면 가독성은 확인하지 못했다.

## 다음 작업 후보

- `Task 003 - 컴파일 기준선 기록`

