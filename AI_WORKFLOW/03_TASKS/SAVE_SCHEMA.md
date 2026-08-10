# SAVE_SCHEMA — Project P.A. 저장 v11 월드 상태와 후속 판매 통계 설계

갱신: 2026-08-10 (Codex, WORLD-007)
근거: `Assets/Scripts/SaveData.cs`, `SaveManager.cs`, `Services/ISaveRepository.cs`, `Services/LocalJsonSaveRepository.cs`, `ShopCustomizationController.cs`

## 현재 규약

- 현재 버전: `SaveManager.CurrentSaveVersion = 11`
- v11 소유 기능: additive `WorldStateSaveData`; 기존 v10은 `LegacyFixed`로만 승격
- 다음 제안 버전: `v12` 판매 통계 추가 확장 — **Task 054 설계만 완료, 미구현**
- 키: `savegame`
- 로컬 경로: `Application.persistentDataPath/savegame.json`
- 형식: Unity `JsonUtility` JSON
- 입력: `PlayerInputHandler.OnSave` / `OnLoad`(F5/F9)
- 저장소 추상화: `ISaveRepository`; 현재 구현은 `LocalJsonSaveRepository`
- 호환 원칙: 필드를 삭제·개명하지 않고 추가하며 버전을 1 올리고 단계별 마이그레이션을 추가한다.

## SaveData 최상위 필드

| 영역 | 필드 | 저장·복원 경로 |
|---|---|---|
| 버전 | `version` | 현재 저장 직전 v11 스탬프, 로드 시 `MigrateSaveData` |
| 경제 | `money`, `cumulativeRevenue` | `EconomyService.ForceSet*` |
| 플레이어 | `playerPosition` | CharacterController를 잠시 끄고 복원 |
| Day 1 프로필 | `playerName`, `selectedMapId`, `firstDayPrototypeStage` | `PlayableDayScenarioController` |
| 티어 | `currentTier`, `reputation` | `TierService.ForceSetTier` |
| 시간 | `gameHour`, `gameDay` | `GameClock.ForceSet` |
| 장기 진행 | `longPlayLastSupplyDay`, `longPlayDayStartRevenue`, `longPlayDayStartMoney` | `LongPlayProgressionController` |
| 감사 | `lastAuditDay` | `AuditService` |
| 친밀도 | `friendshipData` | points + lastDialogueDay |
| 고용 NPC | `hiredNpcs` | ID, 에셋, Transform, 활성 FSM과 각 FSM 상태 |
| 기존 건축 | `buildings` | `BuildingRegistry`의 prefabName/position/rotation |
| 인벤토리 | `inventorySlots`, `hotbarSlots` | item ID/name/count/quality/currentPrice |
| 상점 진열 | `shopSlots` | hierarchy key, 상품·수량·품질·표시 가격 |
| 낮 채집 | `dayPrepCollectedDay`, `dayPrepCollectedActivities` | 같은 날 중복 채집 방지 |
| 마을 변화 | v9 `villageCulture*` 6필드 | pending/active 시각 변화 |
| 배치 스타터 | v10 `placementStarterGranted` | 기본 작업대 설계도 중복 지급 방지 |
| 구역 배치 | v10 `placeables` | 구역/정의/인스턴스/셀/회전/회수/기능 상태 |
| 절차 월드 | v11 `worldState` | mode/seed/generationVersion/sparse cell/building/furniture/resource/safe player |

## v10 배치 DTO

`PlaceableSaveData`

| 필드 | 의미 |
|---|---|
| `zoneId` | 배치 구역. 현재 `shop.interior` |
| `definitionId` | `shop.shelf`, `Blueprint_B05_Workbench` 등 안정 정의 ID |
| `instanceId` | 고정 가구 hierarchy 기반 ID 또는 동적 GUID |
| `gridX`, `gridY` | 구역 로컬 앵커 셀 |
| `rotationQuarterTurns` | 0~3, 90도 단위 회전 |
| `isFixed` | 씬에서 온 기존 가구 여부 |
| `recovered` | 회수되어 비활성/카탈로그 보관 중인지 여부 |
| `functionalState` | `shop-slot:*`, `workbench:*`, `storage:*` 진단용 상태 |
| `storedItems` | 배치 가능한 보관함 내부 아이템 목록 |

P5 상점 테마는 새 최상위 필드 없이 특수 레코드 하나를 사용한다.

- `zoneId=shop.interior`
- `definitionId=shop.theme`
- `instanceId=fixed.shop.theme`
- `isFixed=true`, `recovered=false`
- `functionalState=default` 또는 `processed.warm`

이 레코드는 grid 점유·프리팹 재생성 대상이 아니며 `ShopCustomizationController`가 일반 가구보다 먼저 읽어 렌더러·조명·간판 테마를 복원한다.

`PlaceableStoredItemSaveData`는 `itemId`, `itemName`, `count`, `quality`, `currentPrice`를 보존한다.

## 복원 순서와 의존성

1. 경제, 티어, 시간, Day 1/장기/낮 준비/마을 변화 상태
2. 플레이어 위치, 감사, 친밀도
3. `BuildingRegistry`와 `GridService` 점유 초기화 후 기존 야외 건축 복원
4. 인벤토리와 핫바 복원
5. `ShopCustomizationController.RestoreSavedState`
   - 고정 진열대 이동·회수 상태 복원
   - 동적 Workbench 프리팹 재생성
   - zone owner 점유·clearance·보호 통로 재검사
6. `DeserializeShopSlots`
   - 이동된 진열대의 동일 hierarchy key에 상품과 가격 복원
7. 고용 NPC와 FSM 복원

가구 Transform을 ShopSlot 상품보다 먼저 복원해야 이동한 진열대가 같은 기능 타깃과 재고를 유지한다.

## 마이그레이션 체인

| 전환 | 추가·보정 |
|---|---|
| v0→v1 | 인벤토리·핫바 리스트 |
| v1→v2 | `lastAuditDay` |
| v2→v3 | 친밀도·고용 NPC |
| v3→v4 | 친밀도 일일 제한, 고용 NPC Transform/FSM |
| v4→v5 | ShopSlot 상품·가격 |
| v5→v6 | 플레이어 이름·맵·Day 1 단계 |
| v6→v7 | 장기 진행 sidecar |
| v7→v8 | 낮 채집 완료 상태 |
| v8→v9 | 마을 변화 pending/active 상태 |
| v9→v10 | 구역 배치 리스트와 스타터 지급 플래그 |
| v10→v11 | additive 절차 월드 payload; 기존 저장은 `LegacyFixed`, 절대 건물/가구 자동 변환 없음 |
| v11→v12 | 판매 통계 리스트 4종 — **Task 054 제안, Task 055 승인 전 미구현** |

v9 이하 세이브의 빈 `placeables`는 오류가 아니라 “현재 제작된 기본 상점 배치를 채택”한다는 뜻이다.

## WORLD-007 — v11 additive 월드 상태

`worldState.worldMode`는 `LegacyFixed` 또는 `Procedural`이다. v10 이하 fixture는 돈·시간·인벤토리·건물·`placeables`를 그대로 둔 채 빈 `LegacyFixed` payload만 받고 v11이 된다. 기존 절대 좌표 데이터를 seed 월드로 추정 변환하지 않는다.

`Procedural` payload는 다음만 저장한다.

- base identity: `worldSeed`, `generationVersion`, width/height/cell/chunk 정의
- `modifiedCells`: base와 다른 elevation/ground/path/water만 저장하며 occupancy는 제외
- `placedBuildings`: WORLD-007 MVP B09 stable instance/definition/anchor/quarter-turn 1개
- `shopFurniture`: 기존 `placeables`의 `shop.interior` projection과 stored item 상태
- `resourceStates`: generator stable spawn key의 consumed/respawn day
- 안전 플레이어 셀과 셀 중심 위치

로드는 payload 전체를 먼저 검증한다. generationVersion 누락/불일치, 정의 불일치, 중복·범위 밖 cell, 전체 절반을 넘는 비-sparse delta, 알 수 없는 건물/자원/가구 ID는 live world를 바꾸기 전에 거부한다. base를 seed로 재생성하고 delta를 적용한 뒤 building occupancy를 파생 재구축한다. 같은 payload를 두 번 적용해도 runtime building은 하나다.

현재 `LocalJsonSaveRepository` 파일 쓰기 자체는 기존 동기 단일 파일 구현을 유지한다. WORLD-007은 손상 JSON과 손상 world payload를 적용하지 않는 fail-safe를 추가했으며, crash-safe temp/replace와 backup rotation은 별도 repository hardening 티켓이다.

## Task 054 — v12 판매 통계 추가 확장 설계

### 버전 기준과 범위

Task Queue의 원래 문구인 “v8→v9”, “v9 판매 통계”, 이후 문서의 “v11 판매 통계”는 작성 당시의 역사적 목표다. 현재 v11은 WORLD-007 additive 월드 상태가 사용하므로 판매 통계 구현은 **v12 추가 확장**으로 재기준화해야 한다.

이 설계가 보존하려는 플레이 경험은 다음 네 가지다.

1. 저장·로드 뒤에도 판매 피드와 최근 40건 카테고리 방향이 끊기지 않는다.
2. 당일 구매/보류/구매율과 다음 날 조언이 저장 전후 같은 값을 유지한다.
3. 최근 7개 완료 일차의 카테고리 판매 통계가 100건 원거래 한도를 넘어도 남는다.
4. Task 047의 fishing/furniture 명명 트렌드가 일차 스냅샷으로 남아 주간 방향과 향후 이벤트의 입력이 된다.

다음은 이번 v11에 포함하지 않는다.

- `VillageCultureVisualController`의 v9 pending/active 6필드 변경 또는 통합
- Task 052 이벤트 상태와 보상 수령 상태
- 구매 확률, 가격 수학, 돈, 누적 매출의 의미 변경
- 관광객 장기 상태, 농사 상태, 시설 해금 상태
- 과거 v10 세이브의 판매 이력을 돈이나 누적 매출에서 추정하는 소급 생성

### 제안 최상위 필드

`SaveData`에는 기존 필드를 건드리지 않고 아래 네 리스트만 추가한다. Unity `JsonUtility` 호환을 위해 `Dictionary`, 인터페이스, 다형 DTO를 저장하지 않는다.

```csharp
public List<SaleRecordSaveData> recentSales = new();
public List<DailySalesDecisionSaveData> dailyDecisionStats = new();
public List<DailyCategorySalesSaveData> dailyCategorySales = new();
public List<DailyTrendSnapshotSaveData> dailyTrendSnapshots = new();
```

| 필드 | 단일 원본 역할 | 보존 범위 |
|---|---|---|
| `recentSales` | 기존 판매 피드와 `VillageChangeSignalController` 최근 40건 입력 | `SalesLogManager.maxRecords`가 보관한 최신 원거래, 현재 기본 100건 |
| `dailyDecisionStats` | 일차별 구매·거절·구매율 원본 | 최근 7개 완료 일차 + 현재 미완료 일차 최대 1개 |
| `dailyCategorySales` | 일차·카테고리별 성공 거래 건수/매출 | 최근 7개 완료 일차 + 현재 미완료 일차 최대 1개 |
| `dailyTrendSnapshots` | Task 047 명명 트렌드의 일차별 거래/매출/점수 | 최근 7개 완료 일차 + 현재 미완료 일차 최대 1개 |

`recentSales`와 집계 스냅샷은 목적이 다르다. 전자는 기존 피드·최근 40건 의미를 정확히 복원하는 제한된 원거래 창이고, 후자는 원거래가 100건에서 밀려나도 일차/주간 결과를 보존하는 집계 원본이다. 저장할 때 매번 서로를 임의 재계산하지 않고, 성공 판매 한 건을 기록하는 같은 경로가 두 상태를 정확히 한 번 갱신해야 한다.

### 제안 DTO

`SaleRecordSaveData`

| 필드 | 형식 | 규칙 |
|---|---|---|
| `itemName` | string | 현재 `SaleRecord`의 정확한 상품명. 부분 검색 금지 |
| `category` | string | 현재 `ItemCategory` 이름. 대소문자 무시 파싱은 가능하나 불명 값은 통계에서 제외 |
| `price` | int | 실제 거래의 총 결제액. 음수는 복원 시 0으로 보정 |
| `quality` | float | 기존 피드 표시 호환용 |
| `buyerName` | string | 기존 판매 피드 표시용 NPC 이름/ID |
| `gameDay` | int | 최소 1 |
| `gameHour` | int | 0~23 |

`DailySalesDecisionSaveData`

| 필드 | 형식 | 규칙 |
|---|---|---|
| `day` | int | 최소 1, 일차당 레코드 하나 |
| `purchases` | int | 성공한 `RecordSale` 수, 최소 0 |
| `rejections` | int | `RecordRejection` 수, 최소 0 |
| `finalized` | bool | 권한 있는 밤 정산에서 한 번만 확정 |

`DailyCategorySalesSaveData`

| 필드 | 형식 | 규칙 |
|---|---|---|
| `day` | int | 최소 1 |
| `category` | string | 정확한 `ItemCategory` 이름 |
| `transactions` | int | 해당 일차·카테고리의 성공 거래 수 |
| `revenue` | long | `max(0, price)` 합계 |
| `latestSaleHour` | int | 해당 행의 가장 늦은 성공 판매 시각 0~23 |
| `finalized` | bool | 밤 정산 완료 여부 |

`DailyTrendSnapshotSaveData`

| 필드 | 형식 | 규칙 |
|---|---|---|
| `day` | int | 최소 1 |
| `trendId` | string | `trend.fishing`, `trend.furniture` 같은 안정 ID |
| `transactions` | int | 정확한 매핑과 일치한 성공 거래 수 |
| `revenue` | long | 일치 거래의 제한 전 결제액 합계 |
| `score` | long | v1: `transactions × 1000 + min(revenue, 999)` |
| `latestSaleHour` | int | 동점 해소용 가장 늦은 일치 판매 시각 |
| `finalized` | bool | 밤 정산 완료 여부 |

카테고리 스냅샷은 유효한 모든 `ItemCategory` 성공 판매를 보존한다. 기존 마을 방향 화면은 계속 `Tool`을 제외할 수 있지만, 저장 계층에서 Tool 거래 자체를 버리지는 않는다. 명명 트렌드는 Task 047의 정확한 `(category, itemName)` 매핑과 일치한 양수 거래만 행을 만든다. 실제 상품 계약이 없는 `trend.camping`의 0점 행은 저장하지 않는다.

### 기록·정산·보존 규칙

성공 판매 한 건의 권한 경로는 다음 순서를 보장해야 한다.

1. 기존 `SaleRecord`를 `recentSales` 대응 런타임 목록에 한 번 추가한다.
2. 같은 일차의 `purchases`를 한 번 증가시킨다.
3. 같은 일차·카테고리 행의 `transactions`, `revenue`, `latestSaleHour`를 한 번 갱신한다.
4. 공용 명명 트렌드 판정기가 정확히 일치한 행만 갱신하고 v1 점수를 다시 계산한다.
5. 구매 거절은 `rejections`만 증가시키며 판매·카테고리·명명 트렌드 행을 만들지 않는다.

밤 정산은 해당 일차의 decision/category/trend 행을 한 번 `finalized=true`로 만든다. 저장 버튼, UI 새로고침, 다음 날 로드는 정산 권한을 갖지 않는다. 최근 7개 완료 일차보다 오래된 집계 행은 제거하되, 현재 진행 중인 미완료 일차 하나는 함께 보존한다. 원거래는 기존 `SalesLogManager.maxRecords` 한도와 오래된 항목 우선 제거 규칙을 유지한다.

스택 수량은 현재 `SaleRecord`에 없으므로 거래 건수로 역산하지 않는다. `price`, 품질, 구매자 이름에서 판매 수량을 추정하지 않는다.

### 런타임 소유권과 Task 055 파일 경계

- `SalesLogManager`가 최근 원거래와 일차 구매/거절을 소유한다. 승인된 구현에서는 추가 전용 `WriteSaveFields`/`RestoreSavedState` API로 내보내고 복원해야 한다.
- 일차 카테고리 집계는 성공 판매 경로와 같은 소유자가 갱신한다. `VillageChangeSignalController._stats`는 매초 재계산되는 표현 캐시이므로 직접 저장하지 않는다.
- 명명 트렌드 매핑·점수·일차 스냅샷은 Task 047의 공용 판정기 한 곳이 소유한다. `SaveManager`나 이벤트 코드에 Fish/가구 문자열을 복제하지 않는다.
- `SaveManager`는 각 소유자에게 쓰기/복원을 요청하는 오케스트레이터다. 카테고리 파싱, 점수 계산, `Dictionary` 내부 반영을 직접 맡지 않는다.
- 복원은 기존 상태를 먼저 비우고 검증된 DTO를 **교체**한다. `RecordSale`/`RecordRejection`을 재호출해 복원하면 돈·로그·통계가 중복되므로 금지한다.

현재 Task 055의 오래된 허용 파일 목록은 `SaveData.cs`, `SaveManager.cs`만 적혀 있다. 그러나 `SalesLogManager`의 목록과 Dictionary는 private이며 안전한 복원 API가 없고, 명명 트렌드 런타임도 아직 없다. 따라서 Task 055 승인 요청 때 최소 추가 범위인 `SalesLogManager.cs`, 공용 명명 트렌드 소유자, 저장 왕복 검증기를 먼저 보고해야 한다. 이 범위를 승인받지 못하면 JSON 필드만 추가할 수 있을 뿐 실제 영속화 완료로 판정할 수 없다. reflection이나 `PlayerPrefs` 우회는 사용하지 않는다.

### v11→v12 마이그레이션

기존 단계별 체인 뒤에 다음 한 단계만 추가한다.

```text
if version < 11:
    recentSales = []
    dailyDecisionStats = []
    dailyCategorySales = []
    dailyTrendSnapshots = []
    version = 11
```

로드 직후에는 네 리스트가 null인지 한 번 더 확인하고 빈 리스트로 정규화한다. 각 DTO는 null 행, day 1 미만, 음수 수치, 알 수 없는 category/trendId, 시각 범위 밖 값을 제거하거나 안전 범위로 보정한 뒤 보존 창을 적용한다. 같은 `(day, category)` 또는 `(day, trendId)` 중복 행은 임의 순서로 합치지 말고 경고 후 결정론적으로 하나의 정규화 결과를 만든다.

v10 이하의 돈, `cumulativeRevenue`, ShopSlot 재고, v9 마을 변화 상태에서 과거 판매를 역산하지 않는다. 마이그레이션된 세이브는 “이전 통계 기록 없음”에서 시작하며 기존 경제·인벤토리·배치·문화 상태는 그대로 유지한다. 빈 신규 리스트는 오류가 아니며 UI는 0점 확정 대신 기록 부족 상태를 표시한다.

### v12 저장·복원 순서

저장 시에는 시간/일차를 먼저 캡처한 뒤 판매 상태 소유자들이 같은 일차 기준으로 네 리스트를 쓴다. 버전 스탬프는 모든 필드 작성 뒤 정확히 한 번 v11로 설정한다.

복원 순서는 다음과 같이 기존 순서에 판매 상태를 삽입한다.

1. JSON 역직렬화와 v0→v11 단계별 마이그레이션·정규화
2. 경제와 티어 복원
3. `GameClock` 시간·일차 복원
4. 최근 판매, 일일 판단, 카테고리, 명명 트렌드 상태를 기존 런타임 상태와 교체
5. Day 1/장기 진행/낮 준비/v9 마을 변화 상태 복원
6. 플레이어, 감사, 친밀도, 건물·그리드, 인벤토리, 배치, ShopSlot, 고용 NPC를 기존 순서대로 복원
7. 판매 피드·결산·마을 방향 UI를 한 번 새로고침

판매 상태는 `GameClock`보다 뒤, 이를 읽는 결산·마을 방향·이벤트 평가보다 앞에 복원해야 한다. v9 문화 상태는 자체 pending/active 저장값을 계속 사용하므로 판매 스냅샷에서 다시 예약하지 않는다.

### Task 055 승인 후 검증 계약

Task 055는 아래를 모두 통과해야 구현 완료다.

1. Runtime/Editor 컴파일 오류 0.
2. 사용자 저장 경로가 아닌 격리 `LocalJsonSaveRepository`에서 v11 JSON을 쓴다.
3. Day 2에 Fish 18G@20시, BreadLoaf 30G@21시, 구매 거절 1건을 기록하고 저장한다.
4. 런타임 상태를 비운 뒤 로드해 최근 판매 2건, 구매 2/거절 1, Raw 1건·18G, Processed 1건·30G를 동일하게 복원한다.
5. 기존 최근 40건 카테고리 방향은 Processed를 선도로 다시 읽고 FeedUI가 두 원거래를 표시한다.
6. Fish는 `trend.fishing` 1건·18G·1018점·20시로 복원되고 BreadLoaf는 명명 트렌드에 들어가지 않는다.
7. Day 2 정산 뒤 Day 3에서 저장·로드해 Day 2가 완료 스냅샷으로 주간 창에 한 번만 포함된다.
8. 100건 초과 원거래와 8일 이상 집계를 시드해 원거래 한도, 최근 7개 완료 일차, 현재 미완료 일차 보존을 확인한다.
9. 별도 v10 fixture를 로드해 v11 빈 통계로 마이그레이션되고 돈·누적 매출·시간·인벤토리·ShopSlot·v9 문화·v10 배치가 변하지 않음을 확인한다.
10. 저장→로드→저장을 반복해 통계가 중복 증가하지 않으며 기존 FinalDemoRoute, DayNight, SaveRoundTrip, ShopCustomization/P5 회귀가 PASS한다.

WORLD-007은 핵심 `PA_SaveRoundTripValidator`를 v11 `LegacyFixed` 계약으로 갱신한다. Mining/OutdoorPlacement/ShopCustomization의 과거 `version == 10` assertion은 각 기능 회귀를 다시 실행하는 티켓에서 v11 `LegacyFixed` 또는 Procedural fixture인지 구분해 갱신해야 하며 단순 문자열 일괄 치환은 금지한다.

### Task 054 완료 경계

이번 작업은 필드 이름, DTO, 보존 창, 소유권, 정산 시점, v10→v11 마이그레이션, 복원 순서와 검증 계약만 확정한다. `CurrentSaveVersion`, `SaveData`, `SaveManager`, `SalesLogManager`, 명명 트렌드 코드, 검증기와 실제 JSON은 변경하지 않는다. 실제 스키마 변경은 Task 055의 사용자 승인 뒤 별도 단일 작업으로 수행한다.

## 검증 증거

- 전용 D3D11 검증: `PA_ShopCustomizationValidator`
- 최종 로그: `Logs/ShopCustomizationValidator_FinalUI.log`
- 격리 저장: `Logs/ShopCustomization/20260716_103307/savegame.json`
- 확인 항목: v10 JSON, B05 2×2 셀/회전/instance ID, 이동한 고정 ShopSlot, 회수 상태, 상품 2개와 표시 가격 73G 재로드, Workbench 기능, 보호 통로
- 기존 왕복 회귀: `Logs/ShopCustomization_SaveRoundTripRegression.log` — money 1234, revenue 5678, ShopSlot 2@77G, 마을 변화 pending→active PASS
- P5 테마/확장 회귀: `Logs/P5_ShopProgression_D3D11_Release.log` — `processed.warm` 특수 레코드와 동적 선반 clear→restore PASS
- 최종 저장 왕복: `Logs/P5_SaveRoundTripRegression.log` — v10 경제·인벤토리·ShopSlot·마을 변화 pending→active PASS

## 주의

- `SaveGameAsync`의 중복 version stamp는 WORLD-007에서 하나로 정리했다.
- `ShopSlotSaveData.slotKey`는 기존 hierarchy 키다. 진열대 이동은 hierarchy를 바꾸지 않으므로 키가 안정적으로 유지된다.
- 저장된 배치가 새 구역 규칙을 위반하면 무리하게 복원하지 않고 비활성 회수 상태로 격리하며 경고한다.
- 현재 구현된 v11에서도 `SalesLogManager`의 전체 이력과 관광객 장기 상태는 저장 대상이 아니다. 위 v12 판매 통계는 승인 전 설계 상태다.
