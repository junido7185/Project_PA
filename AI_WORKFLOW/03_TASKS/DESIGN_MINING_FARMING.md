# DESIGN_MINING_FARMING — 광질 우선 확장과 농사 복구 경계

작성: 2026-07-15
대상: Task 043
상태: 설계 완료, 런타임 구현은 후속 단일 작업

## 1. 목적과 결론

낚시 다음 낮 활동은 **광질을 먼저 구현**한다. 광질은 현재 `Item_Ore` → `Recipe_IronBar` → 상점 판매로 이어지는 데이터가 이미 있고, 기존 일일 활동 저장 경로를 그대로 쓸 수 있다. 반면 농사는 `Crop`/`Farmland` 코드를 버리지 않고 재사용할 수 있지만, 현재 상태로는 심기·성장·수확·저장이 한 경로로 연결되지 않는다.

이번 설계의 다음 구현 단위는 다음 한 줄로 고정한다.

> 낮에 광산 구역의 공용 곡괭이로 광석 2개를 캐고, 그중 1개를 밤 상점에 진열해 15G 판매까지 증명한다.

농사는 폐기하지 않는다. 광질 슬라이스 뒤에 데이터 참조 복구 → 고정 밭 상호작용 → 날짜 기반 성장/저장 순서로 확장한다.

## 2. 현재 코드·데이터 호환성 감사

### 2.1 바로 재사용 가능한 기반

| 기반 | 현재 증거 | 재사용 방식 |
|---|---|---|
| 입력 | `PlayerInteraction`이 `IInteractable`을 탐색하고 Space 입력을 전달 | `MiningSpot`/후속 농사 어댑터가 같은 인터페이스 사용 |
| 일일 활동 상태 | `DaytimeStockPrepPoint.activityId`, `DayNightShopLoopController`의 일차별 수집 기록 | 광질 1일 1회, 다음 날 리셋, v9 저장 복원을 새 필드 없이 재사용 |
| 광석 상품 | `Resources/Items/Item_Ore.asset`, Raw, 기본가 15G | 실제 채굴 보상·진열·판매 원본 |
| 광석 가공 | `Recipe_IronBar.asset`: Ore 2 → IronBar 1, Forge | 첫 광질 판매 뒤의 기존 가공 확장점 |
| 광산 공간 | `MineZone`, `WorkSpot_Miner` 명명과 Miner 프로필/생산 데이터 | 런타임 광질 지점의 탐색 앵커 |
| 농사 골격 | `Crop.cs`, `Farmland.cs`, `Farmland.prefab`, `Crop_Corn.prefab` | 새 농사 시스템을 만들지 않고 후속 복구에 사용 |
| 농산물 경제 | Wheat/Carrot, Bread/BakedPotato 레시피 | 후속 농사 결과가 가공·밤 판매로 연결될 목적지 |

### 2.2 현재 그대로 배치하면 끊기는 지점

| 지점 | 실제 상태 | 영향 |
|---|---|---|
| 씨앗 프리팹 | `Item_15_Seed.cropPrefab`가 null | 어떤 작물을 심을지 결정할 수 없음 |
| 농사 입력 | `PlayerInteraction`은 Hoe로 밭 생성만 시도하고 Seed 분기가 없음 | 씨앗을 들어도 `Farmland.Plant`가 호출되지 않음 |
| 밭 상호작용 | `Farmland`는 `IInteractable`이 아님 | 기존 Space 상호작용으로 심을 수 없음 |
| 작물 수확 | `Crop`도 `IInteractable`이 아니며 `Harvest()` 호출자가 없음 | 다 자라도 플레이어가 수확할 수 없음 |
| 성장 시간 | `Crop`은 단계당 `WaitForSeconds(3)` 사용 | 날짜 루프·오프라인 저장과 무관한 테스트 타이머 |
| 농사 저장 | 밭 위치, 작물, 성장 단계, 심은 날짜 저장 필드가 없음 | 종료/재실행 시 농사 진행을 보존할 수 없음 |
| 런타임 바인딩 | `PA_PlayableDayBuilder`는 `PlayerInteraction`을 보장하지만 `farmlandPrefab`을 지정하지 않음 | 빌더 경로에서는 Hoe 사용이 무효일 수 있음 |
| 작물 보상 참조 | `Crop_Corn.prefab.harvestItem` GUID가 현재 Assets의 어떤 `.meta`에도 없음 | 수확해도 Project P.A. Item을 지급하지 못할 가능성 |
| 바위 보상 참조 | `Rock_4.prefab.dropItem` GUID도 현재 Assets의 어떤 `.meta`에도 없음 | 기존 바위 프리팹을 그대로 캐면 유효 Ore가 아님 |
| 곡괭이 아이템 | `ToolType.Pickaxe`와 장착 표시 코드는 있으나 `Resources/Items`에 Project P.A. 곡괭이 Item이 없음 | 첫 광질에 소유 도구 요구를 걸면 시작 경로가 다시 끊김 |
| 바위 재생성 | `Gatherable.Harvest()`는 오브젝트를 즉시 `Destroy` | 일일 리셋·저장 복원과 호환되지 않음 |

따라서 기존 `Rock_4/Gatherable`을 기능 원본으로 직접 쓰지 않는다. 첫 광질은 유효한 `Item_Ore`와 검증된 일일 활동 상태를 원본으로 삼고, 바위/곡괭이는 런타임 표현 계층으로만 사용한다. 농사는 위 단절을 순서대로 복구한 뒤 `Crop`/`Farmland`를 기능 원본으로 유지한다.

## 3. 후보 비교

| 기준 | 광질 우선 | 농사 우선 |
|---|---:|---:|
| 현재 유효한 결과 아이템 | Ore 있음 | Wheat/Carrot 있음 |
| 낮 행동 → 밤 판매 연결 난이도 | 낮음 | 중간 이상 |
| 기존 v9 저장 재사용 | 일일 완료 상태로 가능 | 작물/단계/밭 상태 신규 저장 필요 |
| 코어 파일 수정 위험 | 낮음, `IInteractable` 사이드카 가능 | `PlayerInteraction` 직접 수정 시 높음 |
| 기존 데이터 단절 수 | 곡괭이/바위 보상 2개 | 씨앗/심기/수확/성장/저장 등 6개 이상 |
| 기존 가공 확장 | Ore 2 → IronBar 1 | Wheat → Bread, Carrot → BakedPotato |
| 다음 한 작업에서 왕복 가능성 | 높음 | 낮음 |

판정: **광질을 다음 단일 구현 작업으로 확정**한다. 농사는 저장 승인 전까지 구현을 억지로 축소하지 않고, 완성 게임용 경계를 유지한다.

## 4. 다음 단일 구현 슬라이스 — 광질

### 4.1 플레이어 경험

1. 낮 준비 시간에 `MineZone`의 광맥 표식으로 걸어간다.
2. `[Space] 광맥 두드리기`를 누른다.
3. 공용 곡괭이 타격/먼지 피드백을 짧게 보고 Ore 2개를 얻는다.
4. 같은 날 다시 상호작용하면 `오늘 채굴 완료`를 본다.
5. 밤에 실제 획득한 Ore 1개를 진열하고 15G로 가격을 확정한다.
6. Miner 또는 기존 구매 진입점이 같은 Ore를 사며 돈·누적매출·Raw 판매 기록이 증가한다.
7. 저장 후 같은 날 복원하면 재채굴이 막히고, 다음 날에는 다시 채굴할 수 있다.

첫 패스의 곡괭이는 광산에 비치된 공용 도구다. 존재하지 않는 소유 곡괭이 Item을 가짜로 전제하지 않는다. Project P.A. 곡괭이 아이템·장착 모델·도구 티어는 광질 왕복이 닫힌 뒤 별도 작업으로 추가한다.

### 4.2 상태와 데이터 원본

- 활동 ID: `quarry-mining`
- 보상 원본: `Resources.Load<Item>("Items/Item_Ore")`
- 보상 수량: 2
- 이용 가능 페이즈: `PADayNightPhase.DayPreparation`
- 일일 완료/저장/다음 날 리셋: 기존 `DaytimeStockPrepPoint` + `DayNightShopLoopController`
- 입력: 기존 `PlayerInteraction` + 신규 자식 `MiningSpot : IInteractable`
- 시각: 런타임 바위 군집, 공용 곡괭이, 타격 먼지/색 변화. 메인 씬과 기존 프리팹은 수정하지 않음.

### 4.3 예상 수정 파일 경계

후속 구현 작업은 아래 범위 안에서 시작한다.

- 신규 `Assets/Scripts/MiningSpot.cs`
- `Assets/Scripts/DayNightShopLoopController.cs` — `quarry-mining` 지점과 자식 상호작용 보장
- `Assets/Scripts/DemoVisualDressingController.cs` — 광산 지점 표현
- 신규 `Assets/Editor/PA_MiningShopLoopValidator.cs` — 실제 채굴 결과의 밤 판매 왕복
- 작업 상태/검증 문서

다음 파일은 수정하지 않는다.

- `Assets/Scenes/Prototype_FirstDay.unity`
- `Rock_4.prefab`, `Farmland.prefab`, `Crop_Corn.prefab`
- `SaveData.cs`, `SaveManager.cs`와 저장 스키마
- `PlayerInteraction.cs`, `Inventory.cs`
- `Shop`, `ShopSlot`, `EconomyService`, `PurchaseEvaluator`, `NpcController`

### 4.4 완료 조건

- 합성 Ore 주입 없이 광질 상호작용으로 Ore 2개 획득.
- 같은 일차 두 번째 채굴 차단, 다음 날 재활성.
- 격리 저장 왕복 후 같은 날 완료 상태 복원.
- 실제 획득 Ore 2개 중 1개가 진열로 이동.
- 15G 판매 후 잔액 +15G, 누적매출 +15G, Raw `SalesLog`, 일일 구매 수 +1.
- FinalDemoRoute의 BreadLoaf 30G 경로 유지.
- 런타임/에디터 컴파일 오류 0, D3D11 전용 검증 PASS.

## 5. 농사 후속 구현 순서

농사는 아래 순서를 건너뛰지 않는다.

### F1. 데이터 참조 복구

- `Item_15_Seed.cropPrefab`를 Project P.A. 작물 프리팹에 연결.
- `Crop_Corn.harvestItem`의 끊긴 참조를 유효한 Project P.A. 농산물 Item으로 교체.
- 첫 작물은 기존 Bread 가공 경로를 재사용할 수 있는 Wheat로 고정하고, 표시 이름/외형 불일치를 사람 확인한다.

### F2. 고정 밭 상호작용

- 자유 지형 생성부터 시작하지 않고 마을의 고정 밭 2칸으로 시작.
- `FarmPlotInteraction : IInteractable` 어댑터가 선택 씨앗 확인, 씨앗 1개 차감, `Farmland.Plant`, 성장 상태 안내, `Crop.Harvest`를 연결.
- `PlayerInteraction` 코어는 수정하지 않음.
- 가방이 가득 찼을 때 씨앗/수확물을 잃지 않는 성공·실패 순서를 먼저 보장.

### F3. 날짜 기반 성장과 저장

- `WaitForSeconds(3)`를 완성 규칙으로 사용하지 않음.
- `GameClock.OnNewDay` 기준으로 씨앗 → 새싹 → 수확 가능 단계를 진행.
- `plotId`, 작물 Item ID, 심은 날짜, 현재 단계의 추가 저장 설계를 먼저 승인받고 마이그레이션/격리 왕복을 함께 구현.
- 로드 후 동일 작물·단계·수확 가능 상태가 복원돼야 완료.

### F4. 농사 → 가공 → 판매 왕복

- Wheat 수확 → 기존 Kitchen의 `Recipe_Bread` → BreadLoaf 진열 → NPC 구매 → Processed 마을 변화 신호까지 한 경로로 검증.
- 합성 Wheat/Bread 주입 금지.

## 6. 자기검토 결과

- 기존 입력은 `IInteractable` 확장으로 호환되며 `PlayerInteraction` 재작성은 필요 없다.
- 광질 일일 상태는 현재 v9 `dayPrepCollectedActivities`에 새 활동 ID를 추가하는 기존 경로이므로 저장 필드 추가가 없다.
- Ore, IronBar 레시피, Raw/Processed 경제 분류와 상점 판매 가능 조건이 이미 존재한다.
- 기존 바위/작물 프리팹의 아이템 GUID는 현재 프로젝트 데이터와 호환되지 않으므로 직접 기능 원본으로 쓰지 않는 경계를 명시했다.
- 농사는 기존 `Crop`/`Farmland`를 폐기하거나 새 대형 시스템으로 교체하지 않고, 어댑터와 날짜/저장 확장으로 복구한다.
- 이번 Task 043에서는 코드, 씬, 프리팹, Item/Recipe 에셋, 저장 스키마를 변경하지 않았다.

## 7. 2026-07-15 구현 결과 — quarry-mining

- `MiningSpot : IInteractable`을 추가해 낮 광산에서 공용 곡괭이 타격 후 Ore 2개를 실제 인벤토리에 지급한다.
- `DaytimeStockPrepPoint`와 `DayNightShopLoopController`가 `quarry-mining`의 하루 1회 제한, 다음 날 리셋, v9 저장/복원의 단일 상태 원본이다.
- 실제 채굴 Ore 2개 중 1개를 `ShopSlot`에 진열하고 기본가 15G로 확정한 뒤 Miner가 구매하는 왕복을 연결했다.
- D3D11 전용 `PA_MiningShopLoopValidator`에서 잔액 500→515G, 누적매출 +15G, Raw `SalesLog`, Day 2 구매 수 +1, Day 3 재활성화를 확인했다.
- 메인 씬, 프리팹, 저장 스키마, Shop/Economy/Purchase/NPC 코어는 변경하지 않았다.
- 현재 바위·광맥·곡괭이 런타임 primitive는 상호작용 위치를 식별하기 위한 임시 표현이다. 최종 비주얼 완료로 간주하지 않으며, 다음 비주얼 도구체인 작업에서 아트 방향에 맞는 실제 에셋으로 교체한다.

## 8. 2026-07-17 구현 결과 — 고정 밭 F1/F2

- `Item_15_Seed.cropPrefab`를 기존 `Crop_Corn`에, `Crop_Corn.harvestItem`을 실제 `Item_Wheat`에 연결하고 수확량을 3개로 명시했다.
- `FarmPlotInteraction : IInteractable`이 농부 작업 지점 인근 고정 밭 2칸에서 낮 심기, 단계 안내, 수확을 연결한다. `PlayerInteraction`은 수정하지 않았다.
- 씨앗은 `Farmland.Plant`와 Crop 구성 성공 뒤 1개 차감한다. 수확은 일반 인벤토리의 전량 수용 가능성을 먼저 확인한 뒤 Wheat 3개를 지급해 가방이 찼을 때 작물 손실을 막는다.
- 하루 1회 `farm-seed-pouch`에서 씨앗 2개를 받을 수 있어 새 플레이에서도 두 밭을 사용할 수 있다.
- 원시 Crop 단계 대신 기존 CC0 Nature Pack 기반 `PA_DemoProps/Prop_Wheat`를 3단계 군집으로 재사용하고, 밭 바닥은 경작 이랑을 가진 단일 메시로 생성한다.
- Runtime/Editor 컴파일 오류 0과 데이터/안전 순서/고정 밭/금지 코어 12개 정적 계약은 PASS했다. 직접 렌더 충돌 2회 경계 때문에 Unity 실플레이는 확인하지 않았다.
- F3의 날짜 기반 성장과 plot 저장은 새 저장 설계/승인 전 추가하지 않았다. 현재 6초×2단계 성장은 F1/F2 상호작용 연결용 런타임 규칙이다.
