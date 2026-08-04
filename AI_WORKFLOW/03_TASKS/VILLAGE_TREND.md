# VILLAGE_TREND — 카테고리별 판매 집계 현황

갱신일: 2026-07-17
대상 작업: Task 046, Task 047, Task 093
판정: **카테고리 집계와 당일 낚시·가구 명명 트렌드 구현 완료 / Unity 결산 가독성·캠핑 콘텐츠·7일 저장은 미완성**

## 1. 이 문서의 기준

Project P.A.의 마을 트렌드는 단순 매출 순위가 아니다. 낮 활동으로 준비한 상품을 밤에 판매한 결과가 어떤 마을을 만들고 있는지 플레이어에게 되돌려 주는 핵심 연결부다.

현재 구현은 실제 거래를 카테고리별로 다시 모으고, 같은 당일 기록에서 Fish/생선구이/목제 가구를 낚시·가구 생활 방향으로 정확 매핑해 결산에 함께 보여 준다. 장기 누적·실제 시설 해금·NPC 행동 변화까지 수행하지는 않는다. 이 문서는 현재 코드가 하는 일과 하지 않는 일을 분리한다.

조사 기준 소스:

- `Assets/Scripts/ShopSlot.cs`
- `Assets/Scripts/SalesLogManager.cs`
- `Assets/Scripts/SaleRecord.cs`
- `Assets/Scripts/Item.cs`
- `Assets/Scripts/VillageChangeSignalController.cs`
- `Assets/Scripts/VillageCultureVisualController.cs`
- `Assets/Scripts/UI/PlayableDayScenarioController.cs`
- `Assets/Scripts/CoreSlicePresentationMode.cs`
- `Assets/Scripts/SaveData.cs`, `Assets/Scripts/SaveManager.cs`

## 2. 실제 판매 기록 경로

```text
NPC 구매 성공
→ ShopSlot.TryPurchaseByNpc
→ paidAmount = EffectiveDisplayPrice × 진열 스택 수량
→ EconomyService.Deposit
→ SalesLogManager.RecordSale
→ SaleRecord 1건 추가
→ VillageChangeSignalController가 최근 기록을 카테고리별 재집계
→ 선도 카테고리를 HUD/결산에 제공
```

`ShopSlot.TryPurchaseByNpc`는 실제 결제가 끝난 뒤 다음 값을 `RecordSale`에 넘긴다.

| SaleRecord 필드 | 실제 출처 | 의미 |
|---|---|---|
| `itemName` | `currentItem.data.itemName` | 판매 상품 이름 |
| `category` | `currentItem.data.category.ToString()` | `ItemCategory` 문자열 |
| `price` | `EffectiveDisplayPrice * currentItem.count` | 단가가 아니라 해당 거래의 총 결제액 |
| `quality` | `currentItem.quality` | 판매 스택 품질 |
| `buyerName` | `buyerTag` | 구매한 실제 NPC/검증 구매자 식별자 |
| `gameDay` | `GameClock.CurrentDay` | 거래 일차, 시계가 없으면 1 |
| `gameHour` | `GameClock.CurrentHourInt` | 거래 시각, 시계가 없으면 0 |

중요한 수량 계약은 다음과 같다.

- `SaleRecord` 1건은 **상품 1개가 아니라 성공한 ShopSlot 거래 1건**이다.
- 슬롯에 여러 개가 쌓여 있으면 총 결제액은 수량만큼 커지지만 카테고리 `count`는 1만 증가한다.
- 구매 성공 뒤 슬롯의 `currentItem` 전체가 비워진다.
- 거절은 `RecordRejection`의 일일 판단 통계에만 들어가며 `SaleRecord`나 카테고리 매출에는 들어가지 않는다.
- `Tool` 또는 `toolType != None`인 상품은 `ShopSlot.CanStock`에서 진열을 거부하므로 정상 플레이 판매 경로에 진입하지 않는다.

## 3. SalesLogManager의 보관 범위

`SalesLogManager`는 런타임 메모리에 다음 세 종류를 보관한다.

| 데이터 | 구조 | 현재 동작 |
|---|---|---|
| 성공 판매 | `List<SaleRecord> _records` | 기본 최대 100건. 초과하면 가장 오래된 기록부터 제거 |
| 일차별 구매 수 | `Dictionary<int,int> _purchasesByDay` | `RecordSale` 1회마다 해당 일차 +1 |
| 일차별 거절 수 | `Dictionary<int,int> _rejectionsByDay` | `RecordRejection` 1회마다 해당 일차 +1 |

`GetRecent(count)`는 보관 기록 중 마지막 N건을 복사한 뒤 뒤집어 **최신 거래부터** 반환한다. 일차 필터나 카테고리 필터는 없으며, 호출자가 필요한 범위를 다시 집계한다.

Task 034의 일일 구매·거절 통계와 Task 048의 카테고리 방향은 같은 매니저를 읽지만 서로 다른 데이터다.

- 일일 판단 통계: 특정 Day의 구매/거절/구매율과 다음 날 가격 조언
- 카테고리 신호: 최근 성공 판매만 대상으로 한 거래 건수/매출/선도 방향

따라서 거절이 많아도 카테고리 점수가 직접 낮아지지 않으며, 품질·구매 확률·NPC 성향도 현재 카테고리 점수에 직접 들어가지 않는다.

## 4. VillageChangeSignalController의 집계 규칙

기본 설정은 `maxRecentSales = 40`이다. 컨트롤러는 `RefreshNow()` 및 약 1초 간격 갱신 때마다 기존 `_stats`를 비우고 `SalesLogManager.GetRecent(40)` 결과를 다시 집계한다.

각 유효한 기록에 적용되는 규칙:

1. `record.category`를 대소문자 무시 방식으로 `ItemCategory`에 변환한다.
2. 변환할 수 없는 문자열은 제외한다.
3. `Tool` 카테고리는 제외한다.
4. 카테고리의 `count`를 1 증가시킨다.
5. 카테고리의 `revenue`에 `max(0, record.price)`를 더한다.

현재 선도 점수는 다음 식이다.

```text
leadingScore = transactionCount × 1000 + categoryRevenue
```

가장 큰 점수의 카테고리 하나만 선도 방향이 된다. 이 식은 거래 건수를 강하게 우선하면서 같은 건수에서는 매출이 높은 쪽을 선호한다. 다만 매출 차이가 1000G 이상이면 거래 수가 적은 카테고리도 앞설 수 있으므로, 이를 절대적인 거래 수 우선 규칙으로 해석해서는 안 된다.

동점 처리 규칙은 명시되어 있지 않다. 비교식이 `score <= bestScore`인 후보를 건너뛰므로 열거 순서에서 먼저 평가된 카테고리가 남지만, 코드에는 카테고리 우선순위나 최신 판매 우선 같은 설계 계약이 없다. 후속 설계는 이 우연한 순서에 의존하면 안 된다.

또한 명칭상 “누적”이더라도 현재 범위는 영구 누적이 아니다.

- `SalesLogManager` 자체 보관 상한: 기본 100건
- 마을 신호가 실제로 읽는 창: 기본 최근 40건
- 날짜 경계: 없음. 같은 실행 세션의 여러 날 판매가 최근 40건 안에서 섞임

## 5. 현재 카테고리 의미와 출력

| ItemCategory | 정상 판매 집계 | 현재 신호 문구 | 실제 다음 날 시각 변화 |
|---|---:|---|---|
| `Raw` | 예 | 생산자 수요 방향 | 없음 |
| `Processed` | 예 | 음식/작업장 성장 방향 | **있음** — 다음 DayPreparation에 따뜻한 준비 코너 활성 |
| `Utility` | 예 | 실용적인 마을 업그레이드 방향 | 없음 |
| `Luxury` | 예 | 문화와 평판 방향 | 없음 |
| `Tool` | 아니오 | 없음 | 없음 |

선도 결과는 두 곳에 연결되어 있다.

- `VillageChangeSignalCanvas`: `Village Direction`, 의미 문장, 거래 건수와 매출 신호를 표시한다. 현재 `CoreSlicePresentationMode`가 개발 오버레이로 분류하여 기본 플레이 화면에서는 숨기며 F10 개발 표시에서 볼 수 있다.
- `PlayableDayScenarioController` 결산: `GetLeadingSignalSummary()`의 `Category: N sale(s), XG influence`를 Day 1 결산의 `Village direction` 항목에 넣는다. 별도 개발 패널이 숨겨져도 결산 경로는 남는다.

`PurchaseFeedbackPresentationController`의 “마을 변화” 문장은 해당 거래 카테고리의 의미를 즉시 설명하는 표현 계층이다. 최근 40건 통계를 보여 주는 `VillageChangeSignalController`와는 별도이며, 그 문장 자체를 트렌드 점수로 계산하면 안 된다.

## 6. 다음 날 시각 변화와 저장 경계

`VillageCultureVisualController`는 기본적으로 `Processed` 한 종류만 추적한다.

1. 최근 최대 40건에서 새 `Processed` 판매를 찾는다.
2. 해당 판매 일차를 대기 상태로 기록한다.
3. 같은 날에는 시각을 켜지 않는다.
4. 더 늦은 일차의 `DayPreparation`에 들어가면 `PA_VillageCulture_Processed`와 한 번의 안내를 활성화한다.

현재 저장 스키마 v10에서는 Task 057이 추가한 v9 문화 상태 6필드가 유지된다.

- 대기 여부, 판매 일차, 대기 카테고리
- 활성 여부, 활성 카테고리, 안내 표시 여부

반면 다음 값은 저장하지 않는다.

- `SalesLogManager._records`
- 일차별 구매/거절 Dictionary
- `VillageChangeSignalController._stats`
- 최근 40건의 카테고리별 건수·매출·선도 점수

결론적으로 저장 후 다시 불러오면 이미 예약되거나 활성화된 `Processed` 시각 변화는 복원되지만, 그 변화를 만든 전체 판매 이력과 선도 카테고리 통계는 복원되지 않는다. `Docs/VillageCulture/VC-001A.md`의 “save schema change 없음” 설명은 2026-06-26 최초 구현 당시 기록이며, 현재는 Task 057의 v9 저장 확장이 더 최신 기준이다.

## 7. 기존 검증 증거

이번 Task 046에서는 Unity를 새로 실행하지 않았다. 아래는 현재 코드와 대조한 기존 D3D11 로그다.

| 범위 | 증거 | 확인된 사실 |
|---|---|---|
| 카테고리 선도 집계 | `Logs/Codex_VillageSignal_Validation.log` | Processed 2건, 76G가 선도 신호로 출력되어 PASS |
| 낚시 실제 판매 | `Logs/Codex_Task041_FishingSaleRoundTrip.log` | Fish 18G, Fisher_01, Raw SaleRecord와 일일 구매 통계 PASS |
| 채굴 실제 판매 | `Logs/Codex_MiningShopLoop_Final.log` | Ore 15G, Miner_01, Raw SaleRecord와 일일 구매 통계 PASS |
| Day 1 기존 판매 | `Logs/P5_FinalDemoRouteRegression.log` | BreadLoaf 30G 실제 거래 PASS |
| 다음 날 시각 변화 | `Logs/Fable_T057_VillageCulture2.log` | Processed 당일 대기→다음 DayPreparation 활성과 안전 배치 PASS |
| 문화 상태 저장 | `Logs/P5_SaveRoundTripRegression.log` | Processed `pending→active` 저장 왕복 PASS |

검증 강도도 분리해야 한다.

- `Raw`: Fish와 Ore의 실제 낮 활동→진열→NPC 구매→SalesLog 경로가 각각 검증됨
- `Processed`: 합성 판매를 이용한 선도 신호 및 다음 날 시각 변화가 검증되고, BreadLoaf 실제 30G 거래 경로도 통과함
- `Utility`, `Luxury`: 코드 분기와 문구는 있으나 품목별 실제 전체 판매 왕복과 고유 시각 변화는 확인되지 않음
- 장기 트렌드 저장: 구현되지 않았으므로 검증 증거도 없음

## 8. 현재 판정과 Task 047 인계

Task 046의 조사 결론:

- 카테고리별 성공 판매 건수와 총 결제액 집계는 **구현됨**.
- 최근 40건에서 `count × 1000 + revenue`로 선도 카테고리 하나를 고르는 읽기 신호는 **구현됨**.
- 결산에서 선도 방향을 플레이어에게 보여 주는 경로는 **구현됨**.
- `Processed` 판매를 다음 날 시각 변화로 바꾸고 그 대기/활성 상태를 저장하는 경로는 **구현됨**.
- 카테고리별 장기 점수 저장, 명시적 동점 규칙, 품질/관계/거절 반영, Utility/Luxury 고유 시각, 시설·NPC 행동 변화는 **미구현**.

Task 047은 기존 수식을 이미 완성된 장기 트렌드로 포장하지 말고 다음을 설계해야 한다.

1. 낚시·캠핑·가구가 실제 어떤 `ItemCategory`와 플레이 행동에서 점수를 만드는지 데이터 계약을 명시한다.
2. 거래 건수와 판매 수량을 구분한다.
3. 가격 규모에 따라 1000점 가중치가 뒤집히는 현상을 의도인지 조정 대상으로 분류한다.
4. 동점 규칙과 기간 창(최근 N건/일/주)을 명시한다.
5. 장기 저장이 필요하면 Task 055의 승인·마이그레이션 경계와 Task 057 문화 상태를 분리한다.
6. 실제 월드 변화는 카테고리마다 하나의 목적형 결과부터 연결하고, 현재 임시 `Processed` 원시 오브젝트를 최종 아트로 간주하지 않는다.

## 9. Task 046 변경·검증 범위

- 신규 문서: 이 파일만 기능 산출물로 추가
- 코드·씬·프리팹·에셋·패키지·저장 스키마 변경: 없음
- Unity/Play Mode 신규 실행: 없음 — 문서 전용 작업이라 기존 로그와 실제 소스를 대조
- 사람 확인: 불필요 — 이 문서는 내부 구현 감사이며 제출용 시각 자료가 아님

## 10. Task 047 실제 데이터 감사

Task 047의 “낚시/캠핑/가구”는 세 카테고리를 새로 만드는 요청이 아니다. 현재 `ItemCategory`는 Raw/Processed/Utility/Luxury/Tool 다섯 가지뿐이다. 명명 트렌드는 이 기존 카테고리를 대체하지 않고, 성공 판매 기록 중 명시적으로 등록된 상품만 여러 카테고리에 걸쳐 다시 묶는 읽기 계층이다.

| 명명 트렌드 | 실제 상품 데이터 | 카테고리 | 획득·제작 경로 | 현재 증거 | 판정 |
|---|---|---|---|---|---|
| `trend.fishing` | id 8 `Fish`, 18G, Tier 0 | Raw | `shore-forage` 실제 낚시 또는 생산자 공급 | Fish 2→1 진열→Fisher_01 18G 실제 판매 PASS | 활성 가능 |
| `trend.fishing` | id 10 `생선구이`, 52G, Tier 0 | Processed | `Recipe_GrilledFish`: Fish 1→1, Kitchen | Item/Recipe와 ProcessingChain 후보 로드는 존재. 품목별 밤 판매는 확인 못 함 | 데이터 활성, 판매 증거 미완 |
| `trend.camping` | **없음** | 없음 | 캠핑 상품·레시피·활동 없음 | `camping/campfire/campsite/캠프/모닥불` 데이터 0건 | 비활성 |
| `trend.furniture` | id 11 `목제 가구`, 185G, Tier 2 | Luxury | `Recipe_Furniture`: Plank 3→1, BasicWorkbench | Item/Recipe와 DemoSeed 보유. 제작→밤 판매 전체 왕복은 확인 못 함 | 데이터 활성, 왕복 증거 미완 |

`Shop_Tent_Kit`은 캠핑 상품이 아니다. 과거 중앙 상점의 개발용 원시 표식이며 현재 `DemoVisualDressingController`가 숨긴다. 판매 가능한 `Item`, `RecipeData`, 낮 활동 보상, `SaleRecord` 중 어느 것과도 연결되지 않으므로 캠핑 점수를 만들면 안 된다.

또한 배치 가능한 선반·작업대·보관함과 판매 상품 `목제 가구`를 구분한다. `shop.shelf`, B05~B09 배치·회전·회수는 상점 커스터마이징이며 판매 통계가 아니다. 오직 `ShopSlot.TryPurchaseByNpc`가 `목제 가구` 거래를 성공시켜 `SaleRecord`를 남긴 경우에만 가구 트렌드가 증가한다.

## 11. 명명 트렌드 매핑 계약

### 11.1 점수가 생기는 순간

명명 트렌드 점수는 **성공 판매 뒤 생성된 `SaleRecord`에서만** 생긴다.

- 낚시 완료, 생선 조리, 가구 제작만으로는 0점이다.
- 진열·가격 확정만으로도 0점이다.
- 구매 거절, 작업대 사용, 가구 배치, 청사진 해금도 0점이다.
- 실제 결제와 `SalesLogManager.RecordSale`이 끝나야 1개 거래로 계산한다.

이 계약은 “내가 한 행동”이 아니라 **“내가 실제로 판 물건이 마을을 바꾼다”**는 정체성을 보존한다.

### 11.2 현재 소스와 호환되는 매칭 키

현재 `SaleRecord`에는 `Item.id`가 없고 `itemName`과 `category`만 있다. 따라서 다음 코드 작업 전까지 사용할 수 있는 안전한 매칭은 정확한 `(category, itemName)` 쌍이다.

| trendId | 허용 쌍 | 비고 |
|---|---|---|
| `trend.fishing` | `(Raw, "Fish")` | 실제 판매 검증됨 |
| `trend.fishing` | `(Processed, "생선구이")` | 데이터 존재, 판매 미검증 |
| `trend.camping` | 없음 | `enabled=false`; 이름 유사 검색 금지 |
| `trend.furniture` | `(Luxury, "목제 가구")` | 데이터 존재, 전체 왕복 미검증 |

카테고리가 다르거나 이름이 정확히 일치하지 않으면 점수를 주지 않는 fail-closed 규칙을 사용한다. 설명 문자열, 프리팹 이름, `Shop_Tent_Kit`, `BuildingData`, 배치 definition 이름을 부분 검색해 추론하지 않는다.

장기적으로는 `Item.id` 또는 별도 안정 `trendTag`를 판매 기록에 포함하는 편이 안전하지만, 이는 Task 047의 문서 범위를 넘는 코드/데이터 변경이다. 기존 기록을 새 의미로 소급 해석하지 않고 별도 데이터 버전에서만 전환한다.

## 12. 점수 규칙 v1

명명 트렌드는 현재 카테고리 신호를 지우지 않고 병렬 읽기 결과로 계산한다.

```text
qualifiedTransactions = 기간 안에서 매핑 쌍과 일치한 SaleRecord 수
qualifiedRevenue      = 일치 기록의 max(0, price) 합계
trendScoreV1          = qualifiedTransactions × 1000
                        + min(qualifiedRevenue, 999)
```

매출 항을 기간 전체에서 999로 제한하여 거래 1건 차이가 가격만으로 뒤집히지 않게 한다. `VillageChangeSignalController`의 레거시 카테고리 식 `count × 1000 + revenue`는 그대로 유지한다. Task 047은 설계만 확정했고 Task 093이 이 상한을 당일 명명 트렌드에 구현했다.

예시:

| 판매 결과 | 명명 트렌드 | 거래/매출 | 점수 |
|---|---|---:|---:|
| Fish 1개를 18G에 한 거래로 판매 | fishing | 1 / 18G | 1018 |
| 생선구이 1개를 52G에 판매 | fishing | 1 / 52G | 1052 |
| 목제 가구 1개를 185G에 판매 | furniture | 1 / 185G | 1185 |
| Fish를 각각 18G에 두 번 판매 | fishing | 2 / 36G | 2036 |
| Fish 2개 스택을 한 번에 총 36G로 판매 | fishing | 1 / 36G | 1036 |
| 캠핑 관련 오브젝트를 배치 | camping | 0 / 0G | 0 |

판매 수량은 현재 기록에 없으므로 거래 건수로 대체하거나 총 결제액에서 역산하지 않는다. 품질, 구매자 성향, 거절 수, Tier, 배치 수는 v1 점수에 넣지 않는다. 가격과 품질 효과는 이미 실제 결제 가능성과 결제액에 일부 반영되므로 다시 가중하면 같은 요인을 중복 계산할 수 있다.

## 13. 기간 창과 동점 규칙

### 13.1 오늘의 방향

결산에 표시할 단기 트렌드는 `record.gameDay == settlementDay`인 성공 판매만 사용한다. 이전 날 판매를 오늘 결과로 섞지 않는다.

현재 구현 호환 단계에서는 `SalesLogManager.GetRecent(40)`을 읽고 일차를 필터링한다. 하루 성공 거래가 40건을 넘으면 결과가 불완전하므로 결산은 `최근 40건 기준`이라고 표시해야 하며 영구 통계로 저장하면 안 된다.

### 13.2 주간 방향

완성 게임의 주간 트렌드는 최근 7개 완료 일차의 `DailyTrendSnapshot` 합으로 정의한다. 필요한 최소 필드는 `day`, `trendId`, `transactions`, `revenue`, `score`, `latestSaleHour`다. 현재는 이 스냅샷과 저장 필드가 없으므로 **설계만 존재**한다.

Task 054가 추가 확장 저장 구조와 마이그레이션을 설계하고, Task 055는 사용자 승인 뒤에만 구현할 수 있다. Task 057의 Processed 시각 pending/active 6필드와 판매 통계 스냅샷을 합치지 않는다.

### 13.3 명시적 동점 해소

같은 기간의 선도 명명 트렌드는 다음 순서로 정한다.

1. `qualifiedTransactions`가 많은 쪽
2. 제한 전 `qualifiedRevenue`가 높은 쪽
3. 가장 최근 일차/시각의 일치 판매가 있는 쪽
4. 그래도 같으면 `trendId` 문자열 오름차순

Dictionary 열거 순서, ItemCategory enum 순서, 마지막으로 갱신된 UI 순서에 의존하지 않는다. 캠핑처럼 활성 매핑이 0개인 트렌드는 후보 목록에도 넣지 않는다.

## 14. 카테고리 신호와 명명 트렌드의 관계

하나의 거래는 카테고리 방향과 명명 트렌드에 각각 한 번씩 읽힐 수 있다. 이는 중복 지급이 아니라 서로 다른 질문에 대한 두 읽기 결과다.

| 판매 상품 | 기존 카테고리 신호 | 명명 트렌드 |
|---|---|---|
| Fish | Raw | fishing |
| 생선구이 | Processed | fishing |
| 목제 가구 | Luxury | furniture |
| 철제 도구 | Utility | 없음 |

카테고리 신호는 “어떤 경제 분야가 강한가”를, 명명 트렌드는 “어떤 생활/문화 활동이 성장하고 있는가”를 설명한다. 명명 트렌드가 없는 Utility 상품을 억지로 camping에 넣지 않는다.

후속 시각 변화는 한 번에 목적형 결과 하나만 연결한다. fishing은 물가·생선 손질/식사 공간, furniture는 주민 집·목공 진열처럼 실제 판매 의미를 읽을 수 있어야 한다. 이 예시는 구현 승인 없이 월드 오브젝트를 만드는 지시가 아니며, 캠핑은 Task 070에서 보조 루프로 선택되고 실제 상품 계약이 생기기 전까지 시각 변화도 만들지 않는다.

## 15. Task 047 완료 경계와 후속 구현 체크

Task 047에서 확정한 것:

- 성공 판매만 점수를 만든다.
- 낚시는 Fish/생선구이, 가구는 목제 가구의 정확한 데이터 쌍으로 매핑한다.
- 캠핑은 실제 콘텐츠가 없어 비활성이다.
- 거래 건수와 판매 수량을 구분한다.
- 오늘은 일차 필터, 장기는 7일 스냅샷이라는 기간 경계를 둔다.
- 매출 상한과 명시적 동점 규칙으로 가격·Dictionary 순서의 우연을 제거한다.
- 기존 카테고리 신호, Task 057 문화 상태, 향후 저장 통계를 서로 다른 소유권으로 유지한다.

후속 코드 작업은 다음을 확인해야 한다.

1. 기존 `VillageChangeSignalController`의 카테고리 요약과 회귀를 보존한다.
2. 명명 트렌드 매핑은 정확한 데이터 테이블로 만들고 설명/프리팹 이름 추론을 금지한다.
3. 캠핑은 비활성 매핑 상태에서 UI 후보로 나오지 않는다.
4. Fish 실제 판매가 fishing 1018점이 되고 Raw 카테고리에도 한 번 집계된다.
5. 같은 가격의 개별 2거래와 스택 1거래가 서로 다른 거래 점수를 만든다.
6. 동점은 거래→매출→최신 판매→trendId 순서로 결정한다.
7. 저장이 필요하면 Task 054 설계와 Task 055 승인을 먼저 거친다.

Task 047은 문서 전용이다. Unity/Play Mode를 새로 실행하지 않았고 코드·씬·프리팹·에셋·패키지·저장 스키마를 변경하지 않았다. Fish 판매 외 생선구이·목제 가구의 실제 밤 판매와 모든 캠핑 경로는 확인 못 했으며, 후속 구현/콘텐츠 작업의 검증 대상으로 남긴다.

## 16. Task 093 당일 명명 트렌드 구현 상태

`VillageChangeSignalController`가 기존 최근 40건을 읽을 때 카테고리 집계는 종전처럼 모든 최근 기록에 적용하고, 명명 트렌드는 `record.gameDay == GameClock.CurrentDay`인 성공 거래에만 별도로 적용한다. 허용 쌍은 §11.2의 세 쌍뿐이며 `StringComparison.Ordinal` 정확 일치를 사용한다.

- `Fish` Raw 1건/18G → `trend.fishing`, 1018점
- `Fish` Raw 2건/36G → `trend.fishing`, 2036점
- `생선구이` Processed → `trend.fishing`
- `목제 가구` Luxury → `trend.furniture`
- 잘못된 카테고리의 `Fish`, `Shop_Tent_Kit`, 부분 이름 → 명명 트렌드 없음
- `trend.camping` → 조회 ID만 존재하며 활성 매핑은 계속 0개

선도 결과는 거래 수→제한 전 매출→최근 일차→최근 시각→`trendId` 오름차순으로 고른다. `GetLeadingSignalSummary()`는 기존 `Category: N sale(s), XG influence` 첫 줄을 보존하고 둘째 줄에 `생활 트렌드 · 낚시 생활|가구 문화`와 거래·매출·점수를 붙인다. 따라서 기존 Day 1 결산 호출부를 바꾸거나 별도 퀘스트/보상/점수 저장 시스템을 만들지 않았다.

검증은 Runtime/Editor 순차 빌드 오류 0과 정적 계약 18개까지 통과했다. 기존 Play Mode validator에도 1018/2036, 잘못된 쌍, 캠핑 비활성, 거래 수/매출 우선순위 assertions를 추가했지만 직접 `Camera.Render()` 네이티브 충돌 2회 경계 때문에 Unity는 실행하지 않았다. 실제 결산 화면 줄바꿈·가독성과 생선구이/목제 가구의 제작→밤 판매 왕복은 확인 못 했다. 명명 트렌드는 런타임 전용이며 7일 누적/복원은 Task 055 승인 전 추가하지 않는다.
