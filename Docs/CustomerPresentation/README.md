# SPY-002 — 고객 타입 / 성향 프레젠테이션

작성일: 2026-06-22
대상 씬: `Assets/Scenes/Prototype_FirstDay.unity`

## 목표

NPC 손님이 익명의 군중이 아니라, 저마다 취향과 구매 기준을 가진 "주민 손님"으로 읽히게 한다.
구매 확률·경제 계산·`PurchaseEvaluator` 로직은 **전혀 바꾸지 않고**, 기존 판단 결과를 플레이어가 이해할 수 있게 보여주는 **읽기 전용 프레젠테이션 레이어**만 추가했다.

## 어떤 기존 데이터를 사용했는가

성향 힌트는 **실제로 존재하는 데이터만** 사용한다. 데이터가 없는 성격/취향은 임의로 만들지 않았다.

| 사용 데이터 | 출처 | 표시에 쓰는 방식 |
|---|---|---|
| `NpcProfile.traitSN` | 8개 프로필 모두 값이 다름 (-0.2 ~ 0.6) | 카테고리 선호: S=실용재 / N=장식·고급품. `PurchaseEvaluator` §2 categoryBonus 와 동일 축 |
| `NpcProfile.traitTF` | 값이 다름 (-0.3 ~ 0.7) | 구매 스타일: F=감성 구매형 / T=실리 판단형. `PurchaseEvaluator` §3-a, §4 와 동일 축 |
| `NpcProfile.traitEI` | 값이 다름 (-0.5 ~ 0.2) | 구매 적극성: E=충동구매형 / I=신중형 |
| `NpcProfile.priceSensitivity` | SPY-003(2026-06-22)에서 NPC별로 차등화 (0.65 ~ 1.45) | 1.25 이상=가격에 민감, 0.75 이하=가격에 관대. 기본값(1.0)에 가까우면 표시하지 않음 |
| `NpcController.currentState` | 런타임 FSM 상태 | MovingToShop / BrowsingShop 인 손님만 "관심 손님"으로 패널에 노출 |
| `PurchaseEvaluator.Result` (willBuy, probability) | 구매 평가 결과 그대로 | 구매/거절 이유 분기 |
| `ShopSlot.EffectiveDisplayPrice` / `Item.basePrice` | 진열 데이터 | 가격 비율로 "값이 착해서/가격이 부담돼" 이유 분기 |
| `Item.category` | 아이템 데이터 | 마을 변화 연결 문구 (Processed/Luxury/Utility/Raw) |

### SPY-003 — 주민별 소비 성향 데이터 (2026-06-22)

SPY-002 당시 `priceSensitivity`/`utilityConsumption`/`luxuryConsumption` 가 8개 프로필 전부 1.0 이라
"가격에 민감한 손님" 힌트를 정직하게 만들 수 없었다. SPY-003 에서 주민 직업 아키타입에 맞춰
이 세 값을 차등화해, 가격 힌트가 **데이터 기반**으로 표시되게 했다.

| 프로필 | priceSensitivity | utilityConsumption | luxuryConsumption | 표시되는 가격 힌트 |
|---|---|---|---|---|
| Blacksmith_01 | 0.9 | 1.3 | 1.1 | (없음 → 성격 폴백 "신중형") |
| Chef_01 | 1.1 | 1.5 | 0.9 | (없음 → "감성 구매형") |
| Farmer_01 | 1.35 | 1.3 | 0.7 | 가격에 민감 |
| Fisher_01 | 0.9 | 1.1 | 1.2 | (없음) |
| Lumberjack_01 | 1.15 | 1.2 | 0.9 | (없음) |
| Miner_01 | 1.45 | 1.3 | 0.6 | 가격에 민감 |
| Tailor_01 | 0.65 | 1.0 | 1.4 | 가격에 관대 |
| Carpenter_01 | 1.0 | 1.4 | 0.9 | (없음) |

> 이 변경은 **데이터(ScriptableObject 값)만** 바꾼 것이다. `PurchaseEvaluator` 수식·코드는 그대로다.
> `priceSensitivity` 는 진열가가 이상가보다 비쌀 때(ratio>1)만 구매 확률에 영향을 주므로,
> 기본가 진열을 검사하는 FinalDemoRoute 회귀에는 영향이 없다.
> `utilityConsumption`/`luxuryConsumption` 은 Utility/Luxury 카테고리 구매에만 영향을 주며,
> 이는 의도된 게임 깊이(주민마다 소비 성향이 다름 = 역-공급망 정체성)이지 회귀가 아니다.

## 어떤 경우에 고객 힌트가 표시되는가 (A. 관심 손님 성향)

- 화면: 우상단 인사이트 스택(Money/Demand/Village) 바로 아래 `관심 손님 성향` 패널.
- NPC가 `MovingToShop` 또는 `BrowsingShop` 상태(=가게로 향하거나 둘러보는 손님)일 때 최대 3명까지 표시.
- 형식: `이름 · <카테고리 선호>[ · <구매 스타일/가격 성향>]`
  - 예) `Miner_01 · 장식·고급품 선호 · 가격에 민감`
  - 예) `Tailor_01 · 실용재(식료품·도구) 선호 · 가격에 관대`
  - 예) `Blacksmith_01 · 장식·고급품 선호 · 신중형`
- 한 줄을 짧게 유지하기 위해 가격 성향(priceSensitivity)이 두드러지면 그것을 우선 표시하고,
  그렇지 않으면 감성/실리(traitTF) → 충동/신중(traitEI) 순으로 한 가지 성격만 덧붙인다.
- 활동 중인 손님이 없으면: `밤에 가게를 열면 손님마다 취향이 표시됩니다.` (개념 학습용 안내)

## 어떤 구매/거절 이유가 표시되는가 (B. 이유 + D. 마을 연결)

- 화면: 하단 우측 `손님 반응` 패널(핫바·우상단 스택과 겹치지 않음). 최신 3건 표시.
- 구매 이유(짧고 귀여운 톤, 수치/확률 비노출):
  - 값이 쌀 때: `값이 착해서 바로 샀어요!`
  - 적정가일 때: `적당한 값이라 기분 좋게 구매!`
  - 비싸도 살 때(선호): `찾던 가공품이라 망설임 없이 구매!`
- 거절 이유:
  - 가격 부담: `가격이 조금 부담돼 다음에 올게요.`
  - 선호 낮음: `지금 찾는 물건은 아니네요.`
  - 망설임: `한참 고민하다 살며시 내려놨어요.`
- **마을 변화 연결**(최신 1건에만): 단순 판매 알림을 넘어 거래가 마을로 이어짐을 보여준다.
  - 가공품 구매: `마을 변화: 가공품이 팔리며 마을 식문화가 깨어나요.`
  - 고급품 구매: `마을 변화: 고급품 인기가 마을의 멋과 평판을 키워요.`
  - 실용품 구매: `마을 변화: 실용품 수요가 마을 살림을 단단하게 해요.`
  - 원자재 구매: `마을 변화: 원자재 거래가 생산자들을 들썩이게 해요.`
  - 거절 시: `마을 변화: 진열·가격을 손보면 손님 마음이 열려요.` (플레이어의 진열/가격 판단 → 주민 반응)

## 구현 방식 (기존 로직 보존)

- 신규 스크립트(읽기 전용 사이드카):
  - `Assets/Scripts/UI/CustomerPreferencePresentationController.cs`
  - `Assets/Scripts/UI/PurchaseFeedbackPresentationController.cs`
- `NpcController.EvaluateCurrentSlot` 에 **1줄 읽기 전용 훅** 추가
  (기존 `CustomerDemandInsightController.RecordEvaluation` 훅과 동일 패턴).
  `willBuy`/판매/돈/FSM/구매 확률에 전혀 영향 없음.
- `PA_RuntimeSceneBinder` 에 두 컨트롤러 자동 부착 등록.
- `Shop`, `ShopSlot`, `ShopPriceUI`, `EconomyService`, `PurchaseEvaluator`, NPC FSM, Save 구조 **미변경**.
- 기존 `NpcController.BuildPurchaseFeedback` 말풍선(머리 위, 확률 % 포함)도 **그대로 보존**
  (FinalDemoRoute 검증기가 정확한 텍스트/길이를 검사하기 때문).

## 자동 검증 결과

| 검증기 | 로그 | 결과 |
|---|---|---|
| `PA_CustomerPresentationValidator` (신규) | `Logs/Codex_SPY002_PresentationValidation.log` | 통과 |
| `PA_FinalDemoRouteValidator` | `Logs/Codex_SPY002_FinalRouteRegression.log` | 통과 (`stocked=BreadLoaf, paid=30G`) |
| `PA_DayNightShopLoopValidator` | `Logs/Codex_SPY002_DayNightRegression.log` | 통과 |
| `PA_VillageChangeSignalValidator` | `Logs/Codex_SPY002_VillageRegression.log` | 통과 |
| `PA_LongPlayProgressionValidator` | `Logs/Codex_SPY002_LongPlayRegression.log` | 통과 |

배치 컴파일: `Logs/Codex_SPY002_Compile.log` — `error CS` 없음.

### SPY-003 데이터 차등화 후 재검증 (2026-06-22)

| 검증기 | 로그 | 결과 |
|---|---|---|
| `PA_CustomerPresentationValidator` | `Logs/Codex_SPY003_PresentationValidation.log` | 통과 — `가격에 민감`(Miner), `가격에 관대`(Tailor) 데이터 기반 표시 확인 |
| `PA_FinalDemoRouteValidator` | `Logs/Codex_SPY003_FinalRouteRegression.log` | 통과 (`stocked=BreadLoaf, paid=30G`) |
| `PA_DayNightShopLoopValidator` | `Logs/Codex_SPY003_DayNightRegression.log` | 통과 |
| `PA_VillageChangeSignalValidator` | `Logs/Codex_SPY003_VillageRegression.log` | 통과 |
| `PA_LongPlayProgressionValidator` | `Logs/Codex_SPY003_LongPlayRegression.log` | 통과 (`money=4633G` 불변 → 생산자 납품 결정론 유지) |

배치 컴파일: `Logs/Codex_SPY003_Compile.log` — `error CS` 없음.

## 패널 레이아웃 자동 검증 (2026-06-22, SPY-002-LAYOUT)

기존에는 "두 패널이 겹치지 않는가"를 사람이 눈으로 봐야 했다. 이제 좌표 기반 자동 검사로 대체했다.

- `Assets/Editor/PA_CustomerPanelLayoutValidator.cs` (신규)
  - 화면을 1920x1080 으로 두고 Play Mode 진입.
  - 두 신규 패널(`CustomerPreferencePanel`, `PurchaseFeedbackPanel`)의 화면 사각형을 `RectTransform.GetWorldCorners` 로 구한다.
  - 검사: 두 패널이 화면 경계 안에 있는가 / 우상단 스택(Money·Demand·Village)과 겹치지 않는가 /
    서로 겹치지 않는가 / 핫바(활성 `InventorySlotUI` 합집합)와 겹치지 않는가.
  - 사람 검토용 1920x1080 Game-view PNG 를 `Logs/CustomerPanelReview/<timestamp>/` 에 캡처(캡처 실패는 경고로만 처리).
  - 모든 패널이 동일한 `ScaleWithScreenSize(1920x1080, match 0.5)` 캔버스를 쓰므로, 겹침/경계 관계는 스케일 불변이라
    배치 모드 해상도와 무관하게 결과가 1920x1080 에서도 유효하다.
- 실행: `PA_CustomerPanelLayoutValidator.RunCustomerPanelLayoutValidation`
- 진행 경과:
  - 1차(`Logs/Codex_PanelLayout_Validation.log`): 패널↔HUD 겹침·화면 경계는 통과했으나
    핫바 검사는 `InventoryUI.slotParent` 미발견으로 건너뜀.
  - 핫바 검사를 활성 `InventorySlotUI` 합집합으로 보강하고 온보딩 모달을 닫도록 개선 후
    2차(`Logs/Codex_PanelLayout_Validation2.log`)에서 **실제 핫바 겹침을 발견**:
    `손님 반응` 패널이 핫바 오른쪽 끝과 겹침.
  - 수정: `PurchaseFeedbackPresentationController.BuildUI` 의 `anchoredPosition.y` 를 `24` → `170` 으로
    올려 핫바 위로 분리(다른 패널·로직은 그대로, presentation 좌표 한 값만 변경).
  - 3차(`Logs/Codex_PanelLayout_Validation3.log`): **모든 검사 통과** — 두 패널이 화면 안에 있고,
    Money/Demand/Village·핫바·서로와 겹치지 않음. (`finished successfully`)
- 최종 캡처(모달 닫힌 깨끗한 구도): `Logs/CustomerPanelReview/20260622_112554/customer_panels_1920x1080.png`
  - 육안 확인: `손님 반응` 패널이 핫바 위로 분리됨, 우상단 Money/Village 스택과 우측 패널들이 분리됨,
    한글(관심 손님 성향/손님 반응/마을 변화/장식·고급품 등)이 Jalnan 폰트로 깨짐 없이 렌더링됨.
- 패널 이동(런타임 좌표 변경) 후 5개 검증기 재실행 — 전부 통과:
  - `Logs/Codex_PanelLayout_Reg_Presentation.log`, `..._FinalRoute.log` (`paid=30G`),
    `..._DayNight.log`, `..._Village.log`, `..._LongPlay.log` (`money=4633G` 불변).

## 사람이 Unity에서 확인해야 할 UI 체크리스트

좌표 기반 겹침/경계는 위 자동 검증으로 확인됐다. 아래는 여전히 사람이 직접 봐야 하는 항목이다.

- [x] (자동) `관심 손님 성향` 패널이 우상단 Money/Demand/Village 패널과 겹치지 않는가.
- [x] (자동) 두 패널이 화면 경계 안에 들어오는가.
- [x] (자동) 두 패널이 서로 겹치지 않는가.
- [x] (자동) `손님 반응` 패널이 하단 핫바와 겹치지 않는가 — 겹침 발견 후 패널을 위로 올려 해결, 재검증 통과.
- [ ] 밤 영업 중 손님이 가게로 오면 `관심 손님 성향`에 이름·성향이 실제로 표시되는가.
- [ ] 구매/거절 시 `손님 반응`에 귀여운 이유 + 최신 1건 마을 변화 줄이 표시되는가.
- [ ] 한글 문구가 Jalnan 폰트로 깨짐 없이 렌더링되는가(스크린샷으로 1차 확인됨, 실제 모니터 재확인 권장).
- [ ] 머리 위 말풍선(% 포함)과 하단 `손님 반응` 패널이 정보적으로 충돌하지 않는가.

## 다음 추천 작업

- 손님 성향 힌트를 머리 위 작은 태그/말풍선으로도 노출(현재는 패널 중심).
- (완료, SPY-003) `priceSensitivity` / `utilityConsumption` / `luxuryConsumption` NPC별 차등화로
  "가격에 민감/관대" 힌트가 데이터 기반으로 표시됨.
- 손님 성향 → Village Direction → 시설/이벤트 잠금 해제로 이어지는 중기 연결(Milestone 2).
- 가격 성향뿐 아니라 utility/luxury 소비 강도까지 힌트에 노출할지 검토(예: "실용재를 많이 삼").
