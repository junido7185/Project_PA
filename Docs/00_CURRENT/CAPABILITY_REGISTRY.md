# Capability registry

2026-10-06 D1 표현 계약: `PA_DemoIconBake`는 로컬 모델로 투명 256px 아이콘을 생성하고 기존 Item31/RecipeData12의 표현 참조만 갱신한다. 가격/스택/ID 등 데이터 불변. 핫바·가방·작업대·보관함·가격 창 실제 입력/화면 PASS30, human UNVERIFIED.

2026-10-05 현재 Demo256 한정 계약: `WorldChunkTerrain`의 opening 표현에서 수면을 해변 아래로 두고 기존 dry-cell 진입 가드를 유지한다. 기존 `WorldNavigationService`의 dry one-level 링크와 `FirstDayStepTraversal`은 같은 NPC Agent/FSM의 단차 보행을 연결하며 새 경로 권위가 아니다. `FishingSpot`은 입질 뒤 `FirstDayFishingMinigame` 입력을 거쳐 `FirstDayLandedFish`를 건조한 셀에 올린다. 포획 전 재고 지급 없음, 유효한 도구 접촉 2회 뒤 기존 DayPrepStock으로 선택 종 1개를 지급하며 가방이 가득 차면 물고기를 유지한다. 참치(id8)·도미(3001)·옐로탱(3002)은 로컬 Quaternius CuteFish 모델/기존 ItemRegistry를 사용한다. Save schema/원본/활동 ID는 유지한다. 미니게임 기능 PASS25; 2026-10-06 D0 PASS25 재검수에서 안내문 겹침 해소를 1080p로 확인했다.

`PlayerInteraction`/`EquipmentSystem`/`PlayerLocomotionAnimator`가 도끼·곡괭이·잠자리채의 허공 휘두르기와 잘못된 자원 접촉의 실패 반응을 소유한다. 실패 사용은 자원과 내구도를 소모하지 않는다(PASS15, 정상/근접 시점). `FirstDayToolSurface`는 기존 배경 나무/바위의 반응만 담당한다. GatherFeedback 합성음은 기존 AudioManager SFX 풀/음량 설정을 따른다. 실제 E 타격의 볼륨1 믹스 peak .516849→볼륨0 peak0과 Windows 출력 전후 녹음을 확인했다(P11_AUDIO PASS7). 전체 도입/전환 소리와 모션/소리 사람 수락은 UNVERIFIED. [근거](../../Logs/CodexOpeningDemoFinal/P9-ToolUse-20261005-001/EVIDENCE.md).

2026-10-03 데모 D1~D4 계약: `DemoPioneerReport`는 Canon §20 배점(25/30/25/20, S90/A75/B60/C)으로 기존 SalesLog 이벤트에서만 판매/매출을 받는다. 데모 영업은 `DemoSettlementController`가 20:00 OPEN 뒤 60초/게임시간으로 진행, 21:30 예고, 22:00 `TryCloseOpeningShop` 자동 마감한다(관광객 7/동시 3/7초, 가판대 용량 5; Golden 기본값 불변). 전문 분야·영업 시작/마감은 휴대폰 상점 앱(`ShopManagementPhoneUI`), Report는 `PioneerReportCardUI`(E/Esc/Enter 닫기 → 휴대폰 상점 앱). `FirstDayWorldPresentation.Toast(message, record=true)`는 우상단 3.5초 카드이며 `record=false`(획득·지형·대화)는 휴대폰 기록 `DispatchLog`에 남기지 않는다. 데모 `HotbarUI`는 X 빈손일 때 선택 칸을 옅게 표시한다. `DispatchLog`를 그리는 `FeedUI` 탭은 현재 휴대폰 홈에서 진입 불가. [검증](../../Logs/VisualQA/DemoCompletion-20261003-003953/result.txt).

2026-09-27 가격/영업 표시 계약: ShopPriceUI는 현재 가격·±1/±10·드래그·확정/회수만 표시하고 추천가/예상 구매율/유불리를 노출하지 않는다. NPC 반응은 내부 확률 없는 짧은 말풍선. Report는 기존 점수/판매 데이터의 한국어 표시만 변경했다. 구매 계산과 가격 확정 권위 유지. 후속 보정으로 Report-open 동안 뒤쪽 toast를 숨기며 닫은 뒤 일반 알림은 복귀한다. 상품 표시 Item.itemName/내부 id 계약은 그대로다. [관련 회귀·화면](../../Logs/VisualQA/ShopPolish-20260927/EVIDENCE.md).

2026-09-27 presentation contracts: secured acquisition feedback never grants inventory; Demo256 ShopSlot uses local product models and visible price/sold-out labels; held counter references a visual-only child (no second ShopSlot). P Smartphone is fully hidden while closed; 1080p P/I open/close and input restoration verified. Autonomous sale preserves PurchaseEvaluator/Economy/SalesLog authority and matches Report1sale/8G, first-sale event once. [Evidence](../../Logs/VisualQA/Continuation-20260926/EVIDENCE.md).

2026-09-25 Demo256 entry: BuildingEntrance binds the placed Shop/Base after Settlement Established (Shop + two tents), with reciprocal return and presentation-only cutaway in existing world zones. Golden Tier1 unchanged. Actual inventory-consuming placement and InputSystem entry/exit/reentry verified; site decoration clears while loose pickup identities/quantities are retained. Rejected concurrent fades release the door lock so retry works (2026-09-26). 2026-09-27 autonomous tourist interior approach/browse/purchase verified through the existing NPC FSM; final subjective visual quality HUMAN-UNVERIFIED.

현재 데모 계약 (2026-09-21, FUNCTIONAL AUTOMATED PASS / HUMAN PLAY·VISUAL UNVERIFIED): 기존 WorldBuildingPlacementService가 실제 Demo256 크기의 정착 구역·점유·예약 이동·복원을 소유한다. WorldHotbarPlacementController는 E/R/Esc/RMB preview 입력, PlayerInteraction은 tap/0.5초 Hold E를 연결한다. Inventory의 선택/아이템 권위와 EquipmentSystem의 X 손 표현 구분을 유지하며 비활성 InventoryUI/HotbarUI는 현재 플레이어 권위로 재바인딩된다.

DemoSettlementController는 단일 Shop/Base·일반 Resident Tent 2개·정확한 동행 주거·License Point 1회·Root 4개 영구 선택을 관찰/연결한다. Workbench/CraftingService, 독립 가구의 ShopSlot, ProducerNpcController/WorksiteBinding의 Mining 도구 경로를 재사용한다. 기존 DayNightShopLoopController에 데모 개점/폐점·Day 2 차단을 추가했고 DemoPioneerReport는 읽기 전용 점수/키 데이터만 생성한다. 새 상태는 세션 한정이며 persistence 확장 없음. `PA_OpeningDemoMustPathChecks` D3D11 Play가 Supply부터 Report까지 기능 연결을 PASS했다. 기존 84-check Play의 직접 채집/제어 증거와 합쳐 Canon MUST PATH의 자동 기능 근거로 사용하며 인간 입력 완주·시각 품질은 별도다.
기존 DayNightVisual은 GameClock.CurrentHour를 프레임마다 샘플해 기존 Directional Light의 태양 각도와 색·강도·주변광을 보간한다. 데모 정착 뒤 16→20시 일몰 구간을 진행하고 20시에 밤 영업 준비로 정지한다. 시간 점프도 다음 렌더 프레임에 반영된다. 2026-09-26 Demo Compose의 누락된 DayNightVisual 연결을 수정했다. 자연 16→20시 진행과 실제 GameView 그림자·밝기 변화, NightReady/20시 정지를 검증했다. 근거 `Logs/VisualQA/T5/review-20260926/EVIDENCE.md`; 최종 시각 품질은 HUMAN-UNVERIFIED다.
같은 씬에서 Opening 런타임을 다시 구성할 때 DayNightShopLoopController는 새 세션의 종료·판매 카운터를 초기화하고, 이전 DemoSettlementController의 지연된 파괴는 새 이동 게이트/커서를 바꾸지 않는다. 이 재진입 보호는 코드·컴파일/Console만 확인했으며 실제 재시작 Play는 HUMAN_UNVERIFIED다.

기준: 2026-09-10 / `4374b2a`와 현재 dirty를 분리. 코드·scene·Resources 연결을 정적으로 대조하고 기존 실행 증거를 인용했다. **이번 문서 작업에서 Unity 플레이를 재실행하지 않았다.**

`VALIDATED`는 명시된 경로/날짜만 검증됨, `CONNECTED`는 코드 연결 확인, `IMPLEMENTED_DISCONNECTED`는 기능이 있으나 해당 제품 경로에 미연결, `PARTIAL`은 구현/검증 일부, `STATIC_ONLY`는 데이터/코드 존재만, `UNREACHABLE`은 제품 진입 불가, `BLOCKED`는 알려진 실패, `PLANNED`는 계획, `DEPRECATED`는 대체됨을 뜻한다.

공통 근거: [F: 9월 최종 출항 검사](../90_PRESENTATION/Evidence/2026-09-10/GameFeel/FinalRoute-checks.txt), [S: 8월 관찰 snapshot](../03_VERIFICATION/StateSnapshots/2026-08-25_PROJECT_STATE.md), [H: 개발 이력](../04_DEVELOPMENT_LOG/README.md). F는 **P4를 제외한 checkpoint**이고 S/H는 당시 결과다.

## 1. 제품 진입/기존 Day 1 — CONNECTED

- Primary code / Authority: [PlayableDayScenarioController](../../Assets/Scripts/UI/PlayableDayScenarioController.cs). Supporting: [PA_RuntimeSceneBinder](../../Assets/Scripts/PA_RuntimeSceneBinder.cs), GameManager, SaveManager.
- Scene/runtime: `Prototype_FirstDay`는 기본 시작 씬이며 Departure/WorldSandbox도 Build Settings 등록. Dependencies: Canvas/TMP, Inventory, Shop, 경제·감사·저장. Entry: 씬 Play → 타이틀 primary button.
- Validation / loop: S의 기존 등록·진열·판매·결산 관찰. 신규 출항과의 연결은 미검증이 아니라 현재 미연결.
- Reuse: 기존 entry/Save를 확장. Known issue: 기본 타이틀 NEW GAME은 Departure/WorldSandbox로 연결되지 않음; 출항 도착 브리지는 INTEGRATION-01에서 별도 검증.

## 2. 이동·카메라·입력 — VALIDATED (Departure)

- Primary: [PlayerController](../../Assets/Scripts/PlayerController.cs), [CameraController](../../Assets/Scripts/CameraController.cs). Authority: CharacterController 이동/기존 camera follow. Supporting: PlayerInputHandler, NpcHumanoidProceduralAnimator, OpeningFeelPresentation.
- Usage/dependencies: Departure player·camera, HumanPose/기존 rig. API: `ApplyOpeningFeel`, `ActualPlanarSpeed`, `ResetMotionAfterTeleport`, `ConfigureOpening`, `SetOpeningBuildMode`.
- Validation/loop: F 및 기존 motion 캡처. 축/대각선 4.20m/s, 셀 0.476s, 정지 0.237m, 화면 높이 약 15.7%.
- Reuse: 기존 controller/rig 조정. Known issue: 발 접지·방향 전환·가림은 사람의 최종 체감 확인 필요.

## 3. 상호작용 — VALIDATED (출항 tree/shelf)

- Primary/Authority: [PlayerInteraction](../../Assets/Scripts/PlayerInteraction.cs). Supporting: PlayerInputHandler, IInteractable, InteractionAnchor, 물리 collider.
- Usage/dependencies/API: player의 Space → `TryInteract`; 전방·거리·anchor face·가림 검사. 게임 오브젝트별 `Interact(GameObject)` 재사용.
- Validation/loop: 앞/인접 성공, 두 칸·뒤·진열대 뒷면·장애물 거절, F 채집/진열.
- Reuse: anchor를 기존 대상에 배치. Known issue: 모든 기존 대형 건물/활동의 접근 회귀를 이 결과로 대신하지 않음.

## 4. Inventory/품질 아이템 — VALIDATED (출항)

- Primary/Authority: [Inventory](../../Assets/Scripts/Inventory.cs), ItemInstance. Supporting: Item/ItemRegistry, HotbarUI/InventoryUI.
- Usage/dependencies: 출항 및 공통 player, `Resources/Items`. API: `CountItems`, `HasItems`, `CanAddItems`, `AddItem`, `AddInstance`, `RemoveItems`, `MoveOrSwap`.
- Validation/loop: F의 열매 3 획득→1 진열. Reuse: 기존 슬롯/인스턴스 이동. Known issue: P4 농부 수령 assertion은 별도 미해결이며 Inventory 교체 사유가 아님.

## 5. 진열/가격 — VALIDATED (출항)

- Primary/Authority: [ShopSlot](../../Assets/Scripts/ShopSlot.cs), [ShopPriceUI](../../Assets/Scripts/UI/ShopPriceUI.cs). Supporting: Shop, Inventory, 품질/가격 정책.
- Usage/dependencies/API: Departure TrainingShopSlot; `Interact`, `TryClaim`, `TryPurchaseByNpc`, `RefreshDisplay`, 기존 가격창 확정 버튼.
- Validation/loop: F 재고1→구매→재고0/표시 제거. UI 마우스 닫기/열기 10회 기존 증거.
- Reuse: 기존 가격 정책 및 슬롯; 전용 튜토리얼 가격 시스템 금지. Known issue: 전체 기존 shop 배치 회귀와 구분.

## 6. NPC 구매/경제 — VALIDATED (출항)

- Primary: [PurchaseEvaluator](../../Assets/Scripts/PurchaseEvaluator.cs), [EconomyService](../../Assets/Scripts/EconomyService.cs). Authority: 구매 판단과 잔액은 각각 기존 단일 서비스. Supporting: NpcController/FSM, NpcProfile, SalesLogManager, ShopSlot.
- Usage/dependencies/API: tutorial buyer의 실제 AI; evaluator 결과→`TryPurchaseByNpc`→`Deposit(int,string)`. 성향/가격비/품질 데이터 재사용.
- Validation/loop: F 실제 7G 판매 및 1회 입금. Reuse: 데이터/권장 가격 조정, 판단 우회 금지. Known issue: 모든 가격·성향 분포/경제 밸런스가 검증됐다는 뜻은 아님.

## 7. 출항 교육 — VALIDATED

- Primary: [DepartureTutorialController](../../Assets/Scripts/Presentation/DepartureTutorialController.cs). Authority: tutorial stage만 소유. Supporting: DepartureTutorialPresentation, 기존 tree·inventory·slot·buyer.
- Usage/dependencies/API: `PA_DepartureTutorial`, Resources/DepartureTutorial, builder 생성 참조; `Stage`, `Complete`, `CompanionSelectionUnlocked`.
- Validation/loop: F 이동→채집→진열→가격→판단→인증. Reuse: 기존 stage/presentation. Known issue: 제품 타이틀에서 자동 진입하지 않음.

## 8. 동행 선택·항해 — VALIDATED

- Primary: [DepartureCompanionSelection](../../Assets/Scripts/Presentation/DepartureCompanionSelection.cs), [DepartureVoyagePresentation](../../Assets/Scripts/Presentation/DepartureVoyagePresentation.cs). Authority: 선택 세션/항해 표현만; 별도 고용/경제 없음.
- Supporting/dependencies: Resources `DepartureContinuation.prefab`, 기존 후보 profile/model, 배, WorldGrid/WorldChunkTerrain, Player/Camera.
- Usage/API: `OpenAfterCertification`, `Toggle`, `Confirm`, `ConfirmedIds`, `DepartureConfirmed`, `SkipTravelForSavedArrival`.
- Validation/loop: F 후보3→2 확정→동일 NPC2 배→섬. Reuse: 확인된 ID와 객체 전달. Known issue: 현재 원본 prefab에는 보호된 미커밋 P4 추가가 있음.

## 9. 첫 정착/배치 — VALIDATED (checkpoint)

- Primary: [FirstIslandSettlementController](../../Assets/Scripts/Presentation/FirstIslandSettlementController.cs). Authority: [WorldBuildingPlacementService](../../Assets/Scripts/World/WorldBuildingPlacementService.cs). Supporting: WorldGridService, NavMeshAgent, 기존 P3 definition/prefab.
- Usage/dependencies/API: Departure island, `Begin`, `PreviewAt`, `Rotate`, `Commit`, `Cancel`, `CaptureState`, `PrepareRestoreAsync`; placement preview/place/move 재사용.
- Validation/loop: F 거점1·거처2/NPC 입구 도착; P3 이전 회전·이동·충돌·B09 storage 회귀 이력.
- Reuse: 기존 footprint/clearance/장애물 계약. Known issue: 자연물 자동 삭제 금지. P4 공유 파일 dirty는 checkpoint와 분리.

## 10. Save/restore — VALIDATED (v15 출항), BLOCKED (dirty P4 v16)

- Primary/Authority: [SaveManager](../../Assets/Scripts/SaveManager.cs), [LocalJsonSaveRepository](../../Assets/Scripts/Services/LocalJsonSaveRepository.cs). Supporting: SaveData, FirstIslandSettlementController, WorldPersistenceService.
- Usage/dependencies/API: `SaveGameAsync`, `LoadGameAsync`; Departure=`departure_settlement.json`, 기존 캠페인=`savegame.json`. 검증은 격리 저장소 사용.
- Validation/loop: F 잔액을 바꾼 뒤 실제 복원, fresh Play P3 state/동행2/SaveManager1. Reuse: 기존 schema/migration/JSON 권위.
- Known issue: dirty v16의 빈 production payload 거절; M85 137° 방향 복원 부채. checkpoint v15와 달리 현재 dirty 전체를 PASS로 표기할 수 없음.

## 11. 생성 월드·지형 — CONNECTED / 제품 UNREACHABLE

- Primary/Authority: [WorldGridService](../../Assets/Scripts/World/WorldGridService.cs), WorldChunkTerrain. Supporting: WorldAlphaPlayableController, WorldGameplayAdapterService, WorldPlayerTraversalGuard.
- Usage/dependencies/API: WorldSandbox 전용 bootstrap, `BeginNewGame`, grid `WorldToCell`/`CellToWorld`; Departure도 기존 grid/mesh를 재사용.
- Validation/loop: WORLD/M70 기존 이력, F 작은 첫 섬. Reuse: 기존 grid/chunk/충돌/보호. Known issue: 큰 월드 엔진 재작성·MainGame 통합은 별도 승인.

## 12. 낮 활동·제작 — CONNECTED (World/기존 루프)

- Primary/Authority: FishingSpot, MiningSpot, FarmPlot, [CraftingService](../../Assets/Scripts/CraftingService.cs). Supporting: DayNightShopLoopController, ProcessingOpportunityController, RecipeData, Inventory.
- Usage/dependencies/API: WorldGameplayAdapterService 연결; 활동 `Interact`, `TryCompleteDailyActivity`, crafting service의 기존 recipe/제작 API. Resources Items/Recipes/Buildings 확인.
- Validation/loop: BETA-002/003 및 기존 활동 이력; 이번 F에서는 열매 채집만 실행. Reuse: 기존 지급/레시피/품질/시설 요구.
- Known issue: P3 첫 정착 이후 활동/가공/밤 판매로의 연결은 별도 작업. 완전 신규 활동 권위 금지.

WorldSandbox direct-resource extension: Gatherable (Timber) and MiningSpot (Stone), canonical Axe/Pickaxe and existing Hotbar/Inventory UI are connected and gameplay-validated (2026-09-15). Three hits, full-inventory retry, persistent depletion and legacy compatibility covered. See [September log](../04_DEVELOPMENT_LOG/2026-09.md).

WorldSandbox direct fishing/bugs (2026-09-16): FishingSpot explicit demo mode waits for a bite and a second Space input, then delegates to the existing daily Inventory-first reward path; legacy automatic fishing remains the default. BugCritter requires selected ToolType.Net, proximity and facing; successful Inventory.AddInstance depletes the critter. Net (2003) and Butterfly (2004) resolve through ItemRegistry. Full-inventory retry and duplicate rejection gameplay-validated. Bug population is session-local, with no save migration.

## 13. 낮/밤·정산·마을 반응 — CONNECTED

- Primary: [DayNightShopLoopController](../../Assets/Scripts/DayNightShopLoopController.cs), VillageChangeSignalController. Authority: GameClock/기존 shop gate/SalesLogManager. Supporting: VillageCulture, MoneyHUD, LongPlayProgressionController.
- Usage/API: RuntimeBinder 공통 씬, World adapter; `TryOpenShop`, `TryStartNextDay`, `WriteSaveFields`, `RestoreSavedState`.
- Validation/loop: S 및 VC-001A/DayNight 이력. Reuse: 기존 영업 게이트·판매 카테고리 신호. Known issue: 출항 tutorial의 특수 교육 판매를 전체 밤 영업 통합으로 해석하지 않음.

## 14. 고용·생산·캠페인 — PARTIAL

- Primary/Authority: [HiringService](../../Assets/Scripts/HiringService.cs), ProducerNpcController, LongPlayProgressionController. Supporting: NpcCandidateData/ProductionData, CampaignOpeningController, CampaignFirstShopNightController, SaveManager.
- Usage/dependencies/API: 기존 Smartphone 채용과 WorldSandbox 캠페인; `TryPurchaseCurrentDaySupply`, `WriteSaveFields`, 기존 hire/producer API. Resources Candidates/Production/Dialogues/Residents 존재.
- Validation/loop: CONTENT-001 checkpoint `c49eb01`, M85 기능별 이력. 실제 전체 Day1–30 완주 검증 없음.
- Reuse: companion 선택을 기존 hire/residency와 혼동하지 않고 공급·성장 API 재사용. Known issue: 과거 CONTENT-002 ACTIVE 문구/선승인은 현재 작업 큐가 아님.

## 15. 첫 생산 P4 — BLOCKED / 보호된 미커밋

- Primary: `Assets/Scripts/Presentation/FirstProductionController.cs`, `StarterWorksiteInteraction.cs`. Authority: 기존 ProducerNpcController/Inventory/placement/Save; 새 단일 권위로 인정하지 않음.
- `ProducerNpcController.Procurement`의 `ProcurementPolicy.TryClaimSettlementSupport(Inventory)`가 0G 수령 자격·완료 상태를 담당한다. 생산·재고는 기존 Producer, 수령은 기존 Inventory API를 유지한다. `BatchReady`는 실제 stock에서 파생하며 기존 Starter API/저장 어댑터는 호환용으로 남는다. 기존 P4-R2 managed 계약 17건 PASS (`Logs/P4-R2/managed-checks.txt`); Unity lifecycle 검증은 포함하지 않는다.
- `WorksiteBinding`이 기존 선택 동행자와 작업터의 단일 연결점이다. ID·Producer·직렬화 프로필·work/procurement/home 앵커·assigned/enabled 상태를 노출한다. `FirstProductionController`는 프로필 조회/배정 요청만 하고, 상호작용은 기존 활동과 정책을 호출한다. 현재 세 프로필은 continuation prefab에 연결됐으며 기존 활동 종류를 사용하는 추가 생산자는 데이터로 표현 가능하다. Runtime·Editor compile 오류 0, 정적 40·managed 바인딩 23·Editor 자산 6건 PASS (`Logs/P4-R3C/`, `Logs/P4-R3/asset-checks.txt`), Play 0회. 바인딩 내부 Ensure/Add는 P2/P3 bootstrap 이전 전까지의 명시적 임시 호환 경로다.
- Supporting/dependencies: Worksites 자산·P4 prefab component·dirty Save v16. Entry: 정착 후 production 초기화(OpeningFeel 실행에서 보류 처리).
- Validation/loop: 기존 P4 광부/농부 활동·일부 생산·광부 수령 증거, 농부 수령 assertion 실패. 새 fresh restore에서도 dirty 빈 payload 문제 확인.
- Reuse: 다음 명시 지시 때 exact checkpoint부터 조사. Known issue: 미커밋을 문서 이동·삭제·일괄 커밋하지 않는다.

## 16. 아트·Asset pipeline — VALIDATED (기존 milestone 범위)

- Primary reference: [provenance 입구](../05_ASSET_PROVENANCE/README.md), [Style Grammar](../01_GAME_DESIGN/Art/PROJECT_PA_ART_STYLE_GRAMMAR.md). Authority: 라이선스·selected spec·원본 library/derived manifest.
- Usage/dependencies/API: `Blender/Library/ProjectPA_AssetLibrary.blend`, Blender CLI/Python, FBX/Unity wrapper, reference render. 기존 pipeline 파일의 경로를 유지.
- Validation/loop: ART-000/P0/P3 evidence, OPENING-FEEL은 기존 모델/rig만 재사용. Known issue: P4 미커밋 파생 자산은 승인된 기존 작업의 보존물이며 이번 검증 대상 아님.

WorldSandbox placeable kits (2026-09-16, targeted PASS): WorldHotbarPlacementController routes existing PlayerInteraction/Hotbar input to WorldBuildingPlacementService. Management Hub Kit (2005) references the existing settlement Hub; Shop Kit reuses B01 Blueprint (1001) and existing Shop/ShopSlots. Preview/cancel/invalid/service failure consume zero; success consumes one selected InventorySlot. WorldPersistenceService captures/restores these two stable building identities using the existing schema. No new placement, inventory or save authority.

Placed B01 shop loop (2026-09-16, targeted PASS): WorldGameplayAdapterService binds actual placed Shop/ShopSlots and ShopOpenSign; CustomerArrivalController invites existing NPCs to that Shop. ShopSlot transfers one owned ItemInstance unit, uses existing ShopPriceUI, and guards closed/reentrant payment; PurchaseEvaluator -> ShopSlot -> EconomyService/SalesLogManager remain the purchase chain. Four active slots; three stocked, real rejection and purchase, exact payment/log and restocking validated.

Demo integration (2026-09-16, targeted VALIDATED): DemoRouteController bridges existing departure arrival to Demo256 and observes SalesLogManager.OnSaleRecorded for session-local FirstDemoSaleCompleted / DemoSucceeded exactly once after Hub/B01 placement. WorldAlphaPlayableController uses existing tool/kit bootstrap and clock without campaign resource grants. 90 connected checks PASS; no new inventory/economy/save authority. See September log for evidence and human-check boundaries.
