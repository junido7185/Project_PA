# DAYTIME_ACTIVITIES — 낮 활동 결과→상점 재고 연결 현황

갱신일: 2026-07-17
대상 작업: Task 045
판정: **문서 동기화 완료 / 농사·주민 의뢰는 기능 미구현**

## 1. 이 문서의 기준

낮 활동은 단순 보상 버튼이 아니라 다음 계약을 만족해야 한다.

```text
플레이어가 마을에서 직접 행동
→ 실제 ItemInstance가 인벤토리에 들어감
→ ShopSlot에 진열
→ 가격 확정
→ 밤 영업 개점
→ NPC 구매 또는 거절
→ 돈·누적 매출·SalesLog·일일 통계 갱신
```

소스에 상호작용이 있다는 사실과 위 전 구간 왕복이 검증됐다는 사실을 구분한다. 현재 완전 왕복 증거는 낚시와 광질에 있다.

## 2. 현재 일일 재고 원천

현재 `DayNightShopLoopController.EnsureDayPrepPoints()`가 구성하는 `DaytimeStockPrepPoint` 상태 소유자는 6개다. 이 중 2개는 온보딩/NPC 지원용 보조 원천이고, 4개는 마을을 걸어가 수행하는 활동 위치다.

| activityId | 성격 | 플레이어 행동 | 지급 | 현재 구현 | 밤 판매 증거 |
|---|---|---|---|---|---|
| `garden-basket` | 보조 | 텃밭 바구니에서 즉시 수령 | Carrot ×2 | `DaytimeStockPrepPoint`, 하루 1회 | 아이템 지급 경로 존재. Carrot 전용 획득→판매 왕복은 확인 못 함 |
| `producer-dropbox` | 보조 | 생산자 납품함에서 즉시 수령 | Wheat ×2 | `DaytimeStockPrepPoint`, 하루 1회 | 아이템 지급 경로 존재. Wheat 전용 획득→판매 왕복은 확인 못 함 |
| `forest-forage` | 실제 채집 | 숲길 포인트와 상호작용 | Carrot ×2 | 분산 배치, 트리거 상호작용, 하루 1회 | 아이템 지급 경로 존재. Carrot 전용 획득→판매 왕복은 확인 못 함 |
| `shore-forage` | 실제 낚시 | 낚싯대를 드리우고 1.25초 대기 | Fish ×2 | `FishingSpot`, 찌 대기/성공 피드백, 하루 1회 | **완료**: Fish 2→1 진열→18G→Fisher_01 구매 |
| `meadow-forage` | 실제 채집 | 들판 포인트와 상호작용 | Wheat ×2 | 분산 배치, 트리거 상호작용, 하루 1회 | 아이템 지급 경로 존재. Wheat 전용 획득→판매 왕복은 확인 못 함 |
| `quarry-mining` | 실제 광질 | 공용 곡괭이로 1.1초 타격 | Ore ×2 | `MiningSpot`, 곡괭이 스윙/광석 발견/성공 피드백, 하루 1회 | **완료**: Ore 2→1 진열→15G→Miner_01 구매 |

`Logs/Codex_Task041_FishingSaleRoundTrip.log`의 `gatherPoints=5`는 광질 추가 전 낚시 검증 시점의 수치다. 현재 소스에는 이후 `quarry-mining`이 추가되어 상태 소유자 기준 6개다.

## 3. 공통 재고 계약

모든 위 원천은 병렬 인벤토리나 별도 보상 시스템을 만들지 않고 같은 경로를 사용한다.

1. `DayNightShopLoopController.CurrentPhase == DayPreparation`일 때만 수집 가능하다.
2. 각 `activityId`는 같은 날 한 번만 완료할 수 있다.
3. `Resources/Items`의 실제 `Item`을 읽어 `ItemInstance`를 만든다.
4. 현재 지급 인스턴스는 `quality=1.03`, `currentPrice=item.basePrice`다.
5. `Inventory.AddInstance`가 성공해야 완료 상태를 기록한다. 가방이 가득 차면 활동은 소비되지 않는다.
6. 완료 상태는 `dayPrepCollectedDay`와 `dayPrepCollectedActivities`에 기록된다.
7. 이 필드는 v8에서 도입됐고 현재 전체 저장 스키마 v10에서도 그대로 저장·복원된다.
8. 다음 날에는 일차 비교로 다시 활성화된다.

낚시와 광질의 전용 컴포넌트는 행동 표현만 담당한다. 아이템 지급·당일 제한·저장 상태의 소유자는 계속 부모 `DaytimeStockPrepPoint`와 `DayNightShopLoopController`다.

## 4. 검증된 낮→밤 왕복

### 4.1 낚시

증거: `Logs/Codex_Task041_FishingSaleRoundTrip.log`

- Day 2 낮에는 고객 구매가 차단된다.
- 해변에서 실제 낚시 완료 경로로 Fish 2개를 얻는다.
- 같은 날 재낚시가 차단되고 활동 ID가 저장된다.
- Fish 1개를 `ShopSlot`에 옮겨 기본가 18G를 확정한다.
- Day 2 20:00에 플레이어가 상점을 연 뒤 Fisher_01이 구매한다.
- 돈 500→518G, 누적 매출 +18G, Raw SalesLog 1건, Day 2 구매 통계 1건, MoneyHUD 갱신이 확인됐다.
- 검증기 최종 문구: `FishSale=18G, buyer=Fisher_01, shopGate=OK`.

### 4.2 광질

증거: `Logs/Codex_MiningShopLoop_Final.log`

- Day 2 낮에 `quarry-mining`이 활성화된다.
- 실제 광질 완료 경로로 Ore 2개를 얻고 같은 날 완료 처리한다.
- 당시 v9 격리 저장 JSON에 Ore 2개와 일일 활동 상태가 기록되고 다시 복원됐다. 해당 v8 일일 활동 필드는 현재 v10 스키마에도 유지되며 이후 v10 SaveRoundTrip 회귀가 통과했다.
- 복원한 Ore 1개를 `ShopSlot`에 옮겨 기본가 15G를 확정한다.
- 밤 영업 개점 뒤 Miner_01이 구매한다.
- 돈 500→515G, 누적 매출 +15G, Raw SalesLog 1건, Day 2 구매 통계 1건, MoneyHUD 갱신이 확인됐다.
- Day 3 아침 광산이 다시 활성화된다.
- 검증기 최종 문구: `Ore=2->1 stocked, sale=15G, money=500->515G, buyer=Miner_01`.

두 왕복 뒤 각각 `PA_FinalDemoRouteValidator`의 기존 BreadLoaf 30G Day 1 경로도 회귀 통과했다.

## 5. NPC 공급과 가공의 현재 위치

- Day 2 생산자 매입 지원은 `LongPlayProgressionController`에서 Wheat ×4와 Fish ×2, 총 38G 매입 경로가 검증돼 있다. 이는 플레이어 직접 생활 활동이 아니라 NPC 지원 공급이다.
- B05 작업대의 Wood ×2→Plank ×1 가공은 실제 CraftingUI와 인벤토리로 검증돼 있다.
- 다만 현재 직접 낮 활동 원천에는 Wood가 없고, 자연 획득→B05 가공→밤 판매를 하나로 잇는 전용 왕복 증거도 없다. 따라서 가공 체인은 구현됐지만 낮 활동 전체 연결은 **부분 완료**다.

## 6. 아직 완성되지 않은 낮 활동

| 기능 | 현재 상태 | 완료에 필요한 것 |
|---|---|---|
| 농사 | `Crop`, `Farmland`, 씨앗/수확 필드는 있으나 플레이어 입력·유효 참조·날짜 성장·저장이 연결되지 않음 | Task 043의 F1→F3 순서로 기존 구조 복구. 저장 확장은 사람 승인 후 |
| 주민 의뢰 | `DESIGN_RESIDENT_REQUEST.md` 설계만 완료 | 별도 승인된 단일 코드 작업에서 Chef의 Wheat 3 요청부터 구현·저장·검증 |
| Carrot/Wheat 개별 판매 왕복 | 지급과 일반 ShopSlot 호환은 존재하나 품목별 전용 로그 없음 | 필요 시 기존 검증기를 확장하되 새 시스템은 만들지 않음 |
| Wood 자연 공급→가공→판매 | B05 제작만 검증됨 | 기존 생산자/채집 원천 중 하나와 연결한 실제 단일 왕복 |
| 채굴 비주얼 | 기능은 완료됐지만 primitive 표식 | 배치/NPC 동선을 보존하는 목적형 로우폴리 광맥 에셋으로 교체 |

상점 배치·마을 꾸미기는 현재 플레이 가능한 성장 기능이지만 재고를 생성하는 활동은 아니므로 이 표의 재고 원천 수에 포함하지 않는다.

## 7. 현재 판정

- 졸업 시연의 “두 가지 이상 낮 활동→밤 판매”는 낚시와 광질로 기능 증거가 있다.
- 완성 게임 목표의 채집·낚시·광질·농사 4계열 중 채집·낚시·광질은 플레이어 입력과 인벤토리 지급까지 연결됐다.
- 이 가운데 전 구간 밤 판매 검증은 낚시·광질 2계열이다.
- 농사와 주민 의뢰는 아직 플레이 가능한 기능이 아니다.
- 따라서 낮 생활 전체는 **부분 완료**이며, 완성 선언 조건을 충족하지 않는다.

## 8. 검증 범위

이번 Task 045는 문서 동기화 작업이다. 코드·씬·프리팹·에셋·패키지·저장 스키마를 변경하지 않았고 Unity를 새로 실행하지 않았다. 위 PASS 표기는 기존 로그의 실제 결과만 인용했다.
