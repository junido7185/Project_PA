# 주민 의뢰(간단 요청) 표시 설계

작성일: 2026-07-17  
대상 작업: Task 044  
판정: **설계 완료 / 기능 미구현**

## 1. 목적

주민의 실제 생활 역할에서 나온 특정 아이템 요청을 플레이어가 대화 중 이해하게 한다. 첫 구현은 새 퀘스트 엔진이나 별도 의뢰 목록을 만들지 않고, 이미 존재하는 주민 대화·전문가 레시피·인벤토리·친밀도·낮/밤 흐름을 얇게 연결한다.

이 의뢰의 역할은 다음 한 문장으로 고정한다.

> 주민이 자기 일을 위해 필요한 재료를 부탁하고, 플레이어가 낮에 구한 재료를 건네면 그 관계와 밤 장사 준비가 함께 진전된다.

## 2. 현재 구현 감사

### 2.1 재사용 가능한 근거

| 영역 | 현재 근거 | 설계에서의 사용 |
|---|---|---|
| 대화 | `Assets/Scripts/NpcDialogue.cs`가 `IInteractable`이며 `DialogueUI`로 주민 이름과 대사를 표시한다. | 주민에게 접근해 `[Space]`로 요청을 확인하고 납품하는 유일한 진입점으로 사용한다. |
| 대사 데이터 | `DialogueData.cs`에 `Economy` 토픽과 T/F 성향별 문장 풀이 있고, `DialogueService.GetLineFor`가 이를 선택한다. | 요청의 말투 문장에 `Economy` 토픽을 재사용한다. 새 `Request` enum은 첫 구현에서 추가하지 않는다. |
| 런타임 연결 | `PA_RuntimeSceneBinder.EnsureNpcRuntimeHooks`가 `Resources/Dialogues`의 8개 역할별 대사 에셋과 주민 프로필을 연결한다. | 씬 YAML이나 메인 씬을 수정하지 않고 기존 주민 대화 구성에 붙인다. |
| 수요 표시 | `CustomerDemandInsightController`가 실제 구매 평가를 카테고리 단위로 요약한다. | 마을 전체 수요의 보조 맥락으로만 사용한다. 개인의 특정 품목 요청 원본으로 사용하지 않는다. |
| 주민 역할 | `SpecialistNpcController.assignedRecipes`가 전문가의 실제 레시피를 보유한다. | 요청 품목과 수량의 권위 있는 원본으로 사용한다. |
| 실제 데이터 | `Recipe_Bread`: Wheat 3→BreadLoaf 1, `Recipe_IronBar`: Ore 2→IronBar 1, `Recipe_Plank`: Wood 2→Plank 1, `Recipe_Clothes`: Wheat 2+Plank 1→Clothes 1. 단, 현재 `PA_SceneAutoBuilder`는 Tailor에게 Clothes가 아니라 Bread를 배정한다. | 존재하지 않는 취향이나 품목을 만들지 않고 주민의 직업과 요청을 일치시킨다. Tailor는 배정 불일치가 해결되기 전 후보에서 제외한다. |
| 인벤토리 | `Inventory.HasItems`가 핫바와 가방을 합산하고, `RemoveItems`가 핫바→가방 순으로 차감한다. | 납품 전 보유량 확인과 단일 차감 경로로 사용한다. |
| 친밀도 | `FriendshipService`가 주민 ID별 포인트 추가와 저장을 이미 담당한다. | 의뢰 완료 보상 중 관계 변화에 재사용한다. |
| 낮/밤 | `DayNightShopLoopController.CurrentPhase`가 `DayPreparation`, `ShopOpen`, `Settlement`을 구분한다. | 새 요청 확인과 납품은 낮 준비 단계에서만 허용한다. |

### 2.2 현재 없는 것

- 현재 8개 `Assets/Resources/Dialogues/*.asset`에는 `Greeting` 토픽만 있고 `Economy` 문장이 없다.
- `NpcProfile`에는 특정 요청 아이템이나 요청 수량 필드가 없다.
- `CustomerDemandInsightController`의 데이터는 카테고리 통계이며 런타임 전용이다. 특정 주민의 특정 아이템 의뢰 상태가 아니다.
- 요청의 `Available / Active / CompletedToday` 상태, 납품 처리, 요청 저장, 요청 전용 월드 마커는 구현되어 있지 않다.
- `DayNightShopLoopController`의 일일 활동 완료 사전은 저장되지만, 외부 요청이 안전하게 조회·기록할 공개 API는 없다.
- `CoreSlicePresentationMode`는 `CustomerDemandInsightCanvas`를 플레이어 화면에서 기본 숨김 처리한다. 따라서 이 패널을 요청 전달의 필수 UI로 삼을 수 없다.

따라서 Task 044 완료는 **호환 가능한 설계 문서 완료**이며, 주민 의뢰 기능 완료가 아니다.

## 3. 핵심 설계 결정

### 3.1 첫 요청자는 전문 주민으로 제한한다

첫 플레이 가능한 버전의 요청자는 다음 조건을 모두 만족해야 한다.

1. 같은 GameObject에 `NpcDialogue`와 `SpecialistNpcController`가 있다.
2. `assignedRecipes`에 유효한 레시피가 있다.
3. 레시피에 실제 `Item` 참조와 1개 이상의 재료가 있다.
4. 해당 레시피의 티어와 친밀도 잠금이 현재 해제되어 있다.
5. 현재 시간이 `DayPreparation`이다.

생산 주민(Farmer, Miner, Lumberjack, Fisher)은 첫 버전에서 제외한다. 자기가 생산하는 원재료를 다시 요청하는 모순을 피하고, 프로필 성향만으로 Utility/Luxury 품목을 임의 생성하지 않기 위해서다. 향후 생산 주민 요청은 명시적 데이터가 생겼을 때만 추가한다.

### 3.2 요청 품목은 실제 레시피 재료에서 도출한다

요청 계약의 원본은 `NpcProfile.traitSN`이나 수요 통계가 아니라 전문가의 실제 `assignedRecipes`다.

- 요청 아이템: 해금된 첫 유효 레시피의 첫 유효 재료 `ingredient.item`
- 요청 수량: 그 재료의 `ingredient.count`
- 요청 주민 ID: `NpcDialogue.friendshipId`, 없으면 연결된 `NpcProfile.name`
- 요청 식별자: `resident-request:{day}:{residentId}:{recipe.name}:{item.id}`

첫 수직 슬라이스는 **Chef_01이 Wheat 3개를 요청**하는 계약을 권장한다. 이는 `Recipe_Bread`의 실제 입력이고, Wheat는 낮의 meadow forage/납품 흐름에서 얻을 수 있으며 BreadLoaf는 이미 밤 상점 판매 품목이다.

Blacksmith_01의 Ore 2개와 Carpenter_01의 Wood 2개는 같은 구조의 두 번째·세 번째 데이터 사례다. Tailor_01은 현재 씬 빌더에서 `Recipe_Bread`가 잘못 배정되어 있으므로 첫 구현 후보에서 제외한다. `Recipe_Clothes` 배정을 별도 데이터 수정·검증 작업으로 바로잡은 뒤 Tier 2 요청 후보로 편입한다.

요청 선택에 `SpecialistNpcController`의 private `FindViableRecipe()`를 사용하면 안 된다. 그 함수는 플레이어가 재료를 이미 보유한 레시피만 반환하므로 “부족한 재료를 부탁한다”는 요청의 목적과 반대다. 후속 구현은 공개된 `assignedRecipes`를 읽고 해금 조건만 별도로 확인한다.

### 3.3 주민 말투와 수량 계약을 분리한다

말투 문장과 게임 계약을 한 문자열에 섞지 않는다.

- 말투: 역할별 `DialogueData`의 `Economy` 토픽. 예: 요리사가 재료가 부족한 이유를 말한다.
- 계약: 런타임에서 `아이템 이름 x필요 수량 · 보유 수량/필요 수량`으로 조립한다.

`Economy` 문장이 아직 없으면 계약 문장만 표시하고, 임의의 MBTI 대사를 하드코딩하지 않는다. 후속 데이터 작업에서 Chef/Blacksmith/Carpenter/Tailor 대사 에셋에 T/F 문장을 각각 추가한다.

## 4. 플레이어 경험

### 4.1 표시 흐름

1. 낮에 요청 가능한 전문 주민에게 접근한다.
2. 기존 상호작용 프롬프트가 `[Chef_01] 이야기하기`에서 `[Chef_01] 요청 확인 · 밀 0/3`처럼 상태를 함께 보여 준다.
3. 첫 대화는 기존 `DialogueUI` 안에서 말투 한 줄과 계약 한 줄을 순서대로 표시한다.
4. 플레이어가 재료가 부족하면 현재 보유량과 낮 획득 힌트를 본다. 별도 전체 화면 퀘스트 창은 열지 않는다.
5. 재료를 확보해 같은 주민과 다시 대화하면 `밀 3/3 · 건네기`가 보인다.
6. 납품 성공 시 인벤토리 차감, 친밀도 증가, 짧은 완료 대사를 한 번에 처리한다.
7. 완료한 주민은 그날 `오늘 도움 완료`로 표시되고 같은 요청을 반복 지급하지 않는다.

### 4.2 첫 구현의 화면 원칙

- 기존 `DialogueUI`와 `InteractPromptUI`만 사용한다.
- 숨겨지는 개발용 수요 패널, 상시 화면 구석 의뢰 목록, 머리 위 느낌표를 필수 UI로 만들지 않는다.
- 아이템명, 필요 수량, 현재 보유량을 한 줄에서 읽을 수 있게 한다.
- 밤 `ShopOpen`과 정산 단계에서는 신규 납품을 받지 않고, “내일 낮에 이야기해요”로 안내한다.
- 요청을 완료한 뒤 밤에 상품을 판매하는 행위만 `SalesLogManager`와 마을 변화에 기록한다. 주민에게 재료를 건넨 행위를 상점 매출로 위장하지 않는다.

## 5. 최소 상태 모델

새 범용 퀘스트 엔진 대신 주민 대화 옆의 작은 요청 계약만 둔다.

| 상태 | 의미 | 표시 |
|---|---|---|
| `Unavailable` | 밤, 잠긴 레시피, 유효 데이터 없음 | 기존 인사 대화만 |
| `Available` | 오늘 요청을 아직 확인하지 않음 | `요청 확인 · 밀 0/3` |
| `Active` | 요청 확인 후 아직 부족 | `밀 2/3` |
| `ReadyToDeliver` | 필요한 재료 보유 | `밀 3/3 · 건네기` |
| `CompletedToday` | 오늘 납품 완료 | `오늘 도움 완료` |

요청은 `현재 일차 + 주민 + 레시피 + 품목`으로 결정적으로 재구성한다. 무작위 재추첨이나 게임 재시작에 따른 품목 변경은 금지한다. `Active`는 표시 편의를 위한 런타임 상태이므로 저장 후 다시 열면 `Available`로 보일 수 있지만, 같은 계약과 보유량이 즉시 다시 표시되어 진행 손실은 없어야 한다.

첫 구현에서 일일 완료 상태는 기존 `dayPrepCollectedDay/dayPrepCollectedActivities`의 의미를 재사용할 수 있으나, 내부 사전에 직접 접근하면 안 된다. `DayNightShopLoopController`에 다음처럼 좁은 공개 API를 추가하는 후속 구현이 필요하다.

- `bool IsDailyActivityCompleted(string activityId)`
- `bool TryCompleteDailyActivity(string activityId)`

식별자는 `resident-request:` 접두사를 사용해 기존 `garden-basket`, `shore-forage`, `quarry-mining`과 충돌하지 않게 한다. 기존 저장 스키마의 문자열 목록을 재사용하므로 첫 구현에서 새 SaveData 필드는 만들지 않는다. `Active`를 별도 저장하지 않아도 같은 날 주민과 다시 대화할 때 계약을 결정적으로 복구할 수 있다. 완료 여부만 일일 활동 목록으로 복구한다.

## 6. 납품과 보상 계약

첫 구현의 원자적 순서는 다음과 같다.

1. 낮 단계, 유효 주민/레시피/아이템, 미완료 상태를 다시 확인한다.
2. `Inventory.HasItems(item, count)`를 확인한다.
3. 일일 완료 ID가 아직 미완료인지 확인한다.
4. `Inventory.RemoveItems(item, count)`를 정확히 한 번 호출한다.
5. 일일 완료를 기록한다.
6. `FriendshipService.AddPoints(residentId, requestFriendshipReward, reason)`로 관계 보상을 준다.
7. 완료 대사와 기존 UI 피드백을 표시한다.

첫 버전 기본 보상은 친밀도만 사용한다. `EconomyService.Deposit`은 누적 매출과 Tier 진행을 함께 올리므로 주민 심부름 보상에 사용하면 매출 의미가 오염된다. 완성품 보상(BreadLoaf 등)은 낮→밤 연결에 매력적이지만, 현재 `Inventory.AddItem/AddInstance`는 일부 스택을 채운 뒤 실패할 수 있어 선차감 롤백 없는 지급은 안전하지 않다. `CanAdd` 또는 트랜잭션 API가 마련되기 전에는 보상 아이템 지급을 넣지 않는다.

밤 장사 연결은 다음처럼 달성한다.

- 의뢰는 주민 관계와 어떤 재료가 생활에 필요한지 학습시키는 낮 활동이다.
- 같은 레시피를 플레이어/전문가 가공 흐름에서 완성품으로 만든다.
- 완성품을 상점에 진열하고 손님이 구매하면 그때 매출·수요·마을 변화가 기록된다.

## 7. 후속 구현 경계

### 7.1 예상 최소 수정 범위

- `Assets/Scripts/NpcDialogue.cs`: 요청 상태에 따른 프롬프트와 대화/납품 분기. 기존 Greeting과 일일 대화 친밀도는 보존한다.
- `Assets/Scripts/DayNightShopLoopController.cs`: 이름공간을 가진 일일 활동 완료 조회/기록 API 두 개만 공개한다.
- `Assets/Resources/Dialogues/Dialogue_Chef.asset`, `Dialogue_Blacksmith.asset`, `Dialogue_Carpenter.asset`: `Economy` 토픽 말투 추가. Tailor는 레시피 배정 수정 전 제외한다.
- `Assets/Editor/PA_ResidentRequestValidator.cs`: 전용 검증기 신규.

가능하면 별도 `ResidentRequestController`를 만들지 않고 `NpcDialogue`가 같은 오브젝트의 `SpecialistNpcController`를 읽는 얇은 확장으로 시작한다. 요청 유형, 추적 목록, 연쇄 목표, 보상 테이블, 월드 마커를 일반화하는 순간 범용 퀘스트 엔진이 되므로 금지한다.

### 7.2 건드리지 않을 것

- `PurchaseEvaluator`, `Shop`, `ShopSlot`, `EconomyService`, `SalesLogManager`의 의미와 거래 경로
- `NpcController` 구매 FSM과 고객 수요 통계
- `SaveData`/`SaveManager` 스키마
- 메인 씬 YAML과 주민 프리팹의 수동 덮어쓰기
- 프로필 성향만으로 만든 가짜 특정 품목 취향

## 8. 후속 구현 완료 조건

자동 검증은 다음을 실제 API 호출로 확인해야 한다.

1. DayPreparation에서 Chef_01이 `Recipe_Bread`의 Wheat 3개를 요청한다.
2. Wheat 2개 보유 시 납품이 거절되고 수량이 변하지 않는다.
3. Wheat 3개 보유 시 정확히 3개만 차감되고 요청 완료 보너스가 정확히 한 번 증가한다. 기존 일일 대화 포인트 경로는 제거하거나 중복 호출하지 않는다.
4. 같은 날 두 번째 납품이 차단된다.
5. 저장→불러오기 후 같은 날 완료 상태가 유지된다.
6. 다음 날 요청이 다시 가능하다.
7. ShopOpen/Settlement에서는 신규 납품이 차단된다.
8. Greeting 대화와 기존 일일 대화 친밀도가 깨지지 않는다.
9. CustomerDemandInsight, PurchaseEvaluator, SalesLog 수치가 주민 납품 때문에 변하지 않는다.
10. D3D11에서 기존 FinalDemoRoute와 SaveRoundTrip 회귀가 통과한다.

사람 확인은 요청 프롬프트 가독성, 주민 말투 자연스러움, 낮 동선에서 Wheat 3개를 구하는 부담, 완료 피드백의 체감만 남긴다.

## 9. 자기검토 결과

- 기존 Dialogue 구조와 호환: **예**. `NpcDialogue`/`DialogueUI` 진입점과 기존 `Economy` 토픽을 재사용한다.
- 기존 수요 구조와 호환: **예**. 카테고리 통계를 개인 요청 원본으로 오용하지 않는다.
- 기존 아이템/레시피와 호환: **예**. `Resources/Items`의 실제 `Item` 참조와 `assignedRecipes`의 재료를 사용한다.
- 새 퀘스트 엔진 추가: **아니오**.
- 저장 스키마 변경: **아니오**. 후속 첫 구현은 기존 일일 활동 문자열 목록을 이름공간과 공개 API로 재사용한다.
- 코드/씬 변경: **없음**. Task 044에서는 이 문서만 설계 산출물로 작성했다.
- 플레이 가능 기능 여부: **미구현**. 후속 단일 구현 작업과 D3D11 검증이 필요하다.
