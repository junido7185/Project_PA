# IL-001 + CDN-002 — 실제 낮 채집과 밤 영업 게이트

작성일: 2026-06-24
대상 씬: `Assets/Scenes/Prototype_FirstDay.unity`

## 목표

Milestone 1을 "표시용 낮 준비 바구니 + 항상 판매 가능한 상점"에서,
**낮에 걸어가 실제로 채집 → 진열·가격 → 밤에 가게를 열어야 손님이 구매 → 정산 → 다음날 채집 재활성화**의
실제 플레이 가능한 루프로 강화한다.

## 어떤 기존 시스템을 재사용했는가

| 기존 시스템 | 재사용 방식 |
|---|---|
| `DaytimeStockPrepPoint` (IInteractable) | 이미 DayPreparation 전용 + 하루 1회 + 유효 sellable `ItemInstance` 지급 + 시각 라벨을 제공 → 채집 포인트로 그대로 활용 |
| `DayNightShopLoopController` | 일일 리셋(`_prepCollectionDays`, `OnNewDay`), 페이즈 상태, 진열 stock-prep를 담당 → 채집 포인트 분산 생성 + 밤 영업 게이트 추가 |
| `PlayerInteraction` / `IInteractable` | `[Space]` 상호작용 + 프롬프트. 트리거 콜라이더도 감지(`QueryTriggerInteraction.Collide`)하므로 채집물/간판을 트리거로 두어 NPC 이동 비차단 |
| 기존 Raw 아이템 (`Item_Carrot/Fish/Wheat`) | 신규 ItemData 없이 채집 지급 아이템으로 사용 |
| `NpcController.EvaluateCurrentSlot` | 구매 직전에 게이트 가드 1블록만 추가(PurchaseEvaluator/ShopSlot 미변경) |
| `SaveManager` v7 LongPlay 패턴 | 동일 패턴으로 v8 최소 확장(당일 채집 상태) |
| `ShopSlot` / `ShopPriceUI` | 채집 아이템 진열·가격 설정에 그대로 사용 |

**기존 `Gatherable.cs`** 도 검토했으나, `Inventory.AddItem` 후 `Destroy(gameObject)` 하는 일회성 픽업이라
"하루 1회 + 다음날 재생성" 코지 채집과 맞지 않아, 일일 리셋이 이미 구현된 `DaytimeStockPrepPoint`를 채택했다.

## 채집 포인트 위치와 지급 아이템

`DayNightShopLoopController.EnsureDayPrepPoints()` 가 런타임에 생성한다(씬 직렬화 변경 없음 → 참조 깨짐 위험 없음).

| activityId | 라벨 | 아이템 | 카테고리/가격 | 배치(플레이어 기준) | 콜라이더 |
|---|---|---|---|---|---|
| `garden-basket` | Garden Prep Basket | Carrot | Raw / 12 | 근처(보조) | Box(기존) |
| `producer-dropbox` | Producer Drop Box | Wheat | Raw / 10 | 근처(NPC 지원) | Box(기존) |
| `forest-forage` | 숲길 채집 | Carrot | Raw / 12 | 각 40°, 9m | Trigger |
| `shore-forage` | 해변 낚시터 | Fish | Raw / 18 | 각 130°, 12m | 자식 Fishing Trigger |
| `meadow-forage` | 들판 채집 | Wheat | Raw / 10 | 각 225°, 10m | Trigger |

- 새 야생 채집 3종은 서로 다른 방향·거리에 분산 배치되어 "걸어가 탐색"하는 느낌을 준다.
- 지면 레이캐스트로 Y를 보정하고, NavMesh가 있으면 걷는 길 근처로 스냅한다.
- 트리거 콜라이더라 NPC 이동을 막지 않으면서 플레이어 상호작용은 정상 감지된다.
- 기존 Garden Prep Basket / Producer Drop Box 는 삭제하지 않고 **튜토리얼/초기 보조 재고 + NPC 지원**으로 재해석.

## 하루 리셋 방식

- 각 포인트는 `_prepCollectionDays[activityId] = CurrentDay` 로 채집일을 기록.
- `IsPrepActivityCollectedToday` 는 `저장된 날 >= CurrentDay` 로 판정 → 다음날(CurrentDay 증가)이면 자동으로 다시 채집 가능.
- 채집은 `DayPreparation` 페이즈에서만 가능(`TryCollectDayPrepStock` 가 페이즈를 검사).

## 저장/로드 처리 (Save v8, 최소 확장)

기존 v7 LongPlay 저장 패턴을 그대로 미러링한 **추가 전용** 확장이라 회귀 위험이 낮다.

- `SaveData` 신규 필드: `int dayPrepCollectedDay`, `List<string> dayPrepCollectedActivities`.
- `SaveManager`: `CurrentSaveVersion` 7→8, v7→v8 마이그레이션 블록(기본값 채움), 저장/복원 훅 추가.
- `DayNightShopLoopController.WriteSaveFields(data)` / `RestoreSavedState(day, list)`:
  저장된 날과 현재 날이 같을 때만 당일 채집 완료 상태를 복원 → 저장/로드로 같은 날 채집을 재초기화하지 않는다.
- 검증: 신규 validator가 write→reset→restore 라운드트립을 확인. 기존 LongPlay save/load 회귀도 통과(마이그레이션 추가형).

## 영업 시작 방식

- `DayNightShopLoopController` 가 가게 근처에 **`ShopOpenSign`(IInteractable) 간판**을 런타임 생성(트리거 콜라이더).
- 플레이어가 `[Space]` 로 간판과 상호작용 → `TryOpenShop()`.
  - `ShopOpen` 페이즈에서만 실제로 열림. 그 전에는 "아직 영업 시간이 아니에요. 해가 지면…" 안내.
- 새 입력 키를 추가하지 않고 기존 상호작용 시스템만 사용(키 충돌 없음).
- 간판/HUD가 상태를 분명히 표시: 영업 준비 중 / 영업 시작 가능 / 영업 중.

## 낮/밤 구매 규칙 (CDN-002 게이트)

- 게이트 위치: `NpcController.EvaluateCurrentSlot` 구매 직전 1블록.
  `DayNightShopLoopController.IsShopOpenForCustomers` 가 false면 NPC는 구매하지 않고
  `EndShoppingVisit` 로 발길을 돌리며 가끔 "가게 열면 다시 올게요" 말풍선(과도 반복 방지).
- `IsShopOpenForCustomers` =
  - **Day 1 튜토리얼**: 항상 true(아래 override).
  - **Day 2+**: `phase == ShopOpen` **그리고** 플레이어가 오늘 간판으로 영업을 시작했을 때만 true.
- `PurchaseEvaluator`, `ShopSlot.TryPurchaseByNpc`, NPC FSM 은 변경하지 않았다.

## Day 1 튜토리얼 override 방식

- `IsTutorialAlwaysOpen = keepDay1TutorialShopOpen && CurrentDay <= 1`.
- Day 1 에서는 페이즈와 무관하게 `IsShopOpenForCustomers == true` → 검증된 첫 판매 루트(가격 설정·첫 구매)가 그대로 동작.
- override는 Day 1 에만 적용되고, 일반 DayPreparation 게이트 규칙(Day 2+)을 무력화하지 않는다.
- `PA_FinalDemoRouteValidator`(Day 1 루트)와 신규 validator 의 Day 1 항목으로 보존을 확인.

## 자동 검증 결과

신규 `Assets/Editor/PA_GatheringShopGateValidator.cs` — 통과 (`Logs/Codex_IL001_GateValidation.log`):

- 채집 포인트 5개(≥3) 존재, 분산 배치.
- DayPreparation 에서만 채집, 유효 sellable 아이템 추가(0→2), 같은 날 중복 차단, 다음날 재활성화.
- 채집 아이템 ShopSlot 진열 + 가격 확정(ConfirmCount 증가).
- Day 2 낮 구매 게이트 OFF → ShopOpen + 영업 시작 시 ON.
- Day 1 튜토리얼 항상 열림.
- Village Direction 컨트롤러 유지.
- 당일 채집 상태 save/load 라운드트립(v8).
- 최종: `PA Gathering ShopGate Validation passed. gatherPoints=5, gatheredInventory=2, shopGate=OK`.

기존 회귀(모두 통과):

| 검증기 | 로그 |
|---|---|
| `PA_FinalDemoRouteValidator` | `Logs/Codex_IL001_Reg_FinalRoute.log` (`paid=30G`) |
| `PA_DayNightShopLoopValidator` | `Logs/Codex_IL001_Reg_DayNight2.log` (검증기 1행 수정: 모든 포인트 채집 후 소진 확인) |
| `PA_LongPlayProgressionValidator` | `Logs/Codex_IL001_Reg_LongPlay.log` (`money=4633G`) |
| `PA_CustomerPresentationValidator` | `Logs/Codex_IL001_Reg_Presentation.log` |
| `PA_VillageChangeSignalValidator` | `Logs/Codex_IL001_Reg_Village.log` |
| `PA_CustomerPanelLayoutValidator` | `Logs/Codex_IL001_Reg_PanelLayout.log` |

배치 컴파일: `Logs/Codex_IL001_Compile2.log` — `error CS` 없음.

## 스크린샷 경로 (1920x1080)

`Logs/GatheringShopReview/20260624_141557/`

1. `01_day_forage_point.png` — 낮 채집 포인트 상호작용 전 (Day 2 09:00 Day Prep)
2. `02_after_gather.png` — 채집 후 마을 전경 (Day Prep)
3. `03_night_shop_open.png` — 밤 영업 시작 (Day 2 20:00 Night Shop Open)
4. `04_customer_reaction.png` — 손님 반응(관심 손님 성향 + 손님 반응 패널)
5. `05_settlement_next_day.png` — 다음날 (Day 3 08:00 Day Prep, 채집 재활성화)

육안 검토: 단계 전환 HUD·한글 렌더링 정상, 패널 겹침 없음 확인.
한계: 채집물/간판이 단색 플레이스홀더 큐브(기능 정상, 시각 폴리시는 향후 과제).

## 사람이 Unity에서 직접 확인해야 할 항목

자동 검증은 데이터/상태 로직과 캡처 기하만 본다. 실제 플레이 감각은 사람이 확인 필요.

- [ ] 실제 입력으로 채집 포인트까지 걸어가 `[Space]` 채집이 자연스러운가(거리/위치 감).
- [ ] 채집물 큐브가 NavMesh/NPC 동선/상점 접근을 막지 않는가.
- [ ] 밤에 간판으로 영업 시작 → 손님이 실제로 구매를 시작하는가, 낮에는 구매하지 않는가.
- [ ] Day 1 튜토리얼 첫 판매가 여전히 막힘 없이 되는가.
- [ ] 채집물/간판 플레이스홀더의 시각 폴리시(메시/색) 개선 필요 여부.

## 다음 추천 작업

- 채집물/간판을 low-poly 메시(덤불·조개·표지판)로 교체하는 시각 폴리시.
- 채집 포인트 위치를 씬에 고정 배치(해변/숲/들판 실제 지형)로 이전 — 현재는 런타임 상대 배치.
- 밤 영업 종료(Settlement)에서 영업 마감 상호작용 + 일일 매출 요약 강화.
- 채집 카테고리 다양화(예: 광석/목재 채집 → 가공 체인 연결).
- 손님 도착 페이싱: 영업 시작 후 NPC 가 자연스럽게 몰려오도록 스케줄 연동.

## Follow-up — 실제 낚시와 밤 판매 왕복 (2026-07-15)

- `shore-forage`는 더 이상 즉시 Fish를 줍는 큐브가 아니다. 자식 `FishingSpot`이 `IInteractable`을 담당해 캐스팅→약 1.25초 대기→Fish 2개 획득 흐름을 제공한다.
- 일일 제한·다음 날 리셋·저장 상태는 기존 `DaytimeStockPrepPoint`를 단일 원본으로 계속 사용한다.
- 해변 표식은 물빛 원형·낚싯대·줄·찌·어획 바구니 런타임 외형으로 일반 채집 지점과 구분된다.
- `PA_GatheringShopGateValidator`는 합성 Fish 주입 없이 실제 어획물 2개 중 1개가 진열로 이동하고, 18G 가격 확정 후 Fisher_01 구매로 잔액 500→518G·누적매출·Raw 판매 기록·일일 구매 통계가 갱신되는 전체 왕복을 확인한다.
- 자동 PASS: `Logs/Codex_Task041_FishingSaleRoundTrip.log`. 사람 입력 절차: `AI_WORKFLOW/04_VERIFICATION/SMOKE_CHECKLIST.md` §3.

## Follow-up — Customer Arrival Pacing (2026-06-24)

After the gate (CDN-002), opening the shop now actively brings customers, so "opening" feels meaningful.

- Added `Assets/Scripts/CustomerArrivalController.cs` (read-only/event sidecar, registered via `PA_RuntimeSceneBinder`).
- It polls `DayNightShopLoopController.IsShopOpenForCustomers`. When the shop is open for customers AND it is not the Day 1 tutorial, it invites idle customers one at a time (`inviteInterval`) up to `maxConcurrentCustomers`, by calling the existing `NpcController.SetShoppingPriority(true)` + `TryForceShop()` (the same idempotent public API `NpcScheduleController` uses). When the shop closes it calls `SetShoppingPriority(false)` to disperse.
- Day 1 tutorial stays passive (`ShouldActivelyInvite == false`) so the existing `PlayableDayScenarioController` keeps managing Day 1 customer flow (no double-driving). Paused/already-shopping NPCs are ignored by `TryForceShop`, so sleeping/working NPCs are never woken.
- No change to `PurchaseEvaluator`, `ShopSlot`, `EconomyService`, or NPC FSM internals.

Validation: new `PA_CustomerArrivalValidator` (16 checks) — passed (`Logs/Codex_Arrival_Validation.log`): closed shop invites nobody; Day 1 tutorial stays passive; Day 2 night + player opens → 3 customers pulled toward the shop; concurrent cap respected (2). Re-ran all 7 existing validators — all passed (`Logs/Codex_Arrival_Reg_*.log`; FinalRoute `paid=30G`, LongPlay `money=4633G` unchanged).

Next: tune `inviteInterval`/`maxConcurrentCustomers` for feel; optionally bias which residents arrive first by preference/relationship; low-poly visual polish for the placeholder forage/sign cubes.
