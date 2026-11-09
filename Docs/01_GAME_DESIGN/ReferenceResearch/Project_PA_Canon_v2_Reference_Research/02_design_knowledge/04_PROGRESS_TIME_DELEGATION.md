# DAY-01: 정착·시간·성장·선택적 위임

Canon §0·§1–3·§13–16·§19·§21–22. [경험 구조](../00_basis/01_EXPERIENCE_MAP.md), [Dave 분석](../01_reverse_analysis/04_DAVE_LOOP_AND_CINEMATIC.md). 기존 DemoSettlementController, GameClock, DayNightShopLoopController, Crafting/Producer/Worksite를 확장 대상으로 삼는다. 해당 책임은 현재 registry R이며 전체 코드를 직접 읽은 것은 아니다.

## 필수 진행과 자유 활동을 분리한다

| 조건 C | 연결 결과 | 연결하면 안 되는 추가 조건 |
|---|---|---|
| 본사 문법 완료 | 동행 선택 | 도끼·낚시·배치·전문 분야 교육 |
| 동행 정확히 2명 | 항해/도착에 선택 유지 | 직업별 주택 구매 |
| 섬 항구 조작 시작 | Day 1 시작 | 항해 중 Day 1 시간 경과 |
| Base+텐트 2개 정착 | License Point 1회 | fish/bug/NPC 도구 지급 완료 |
| 첫 Root 선택 | 다음 노드 preview, 핵심 정착 뒤 일몰 | 모든 Root 연구·고급 업그레이드 |
| 밤 준비, stand 1개 이상 상품 | OPEN 가능 | stand 3개 모두 재고·Agriculture 실습 |
| 실제 판매→CLOSE | 영업 후 Pioneer Report | 모든 선택 활동 또는 최고 rank |

H: milestone는 실제 성공 결과에서 관찰한다. preview만으로 텐트 설치·License 지급을 인정하지 않는다. UI를 열었다고 Root 선택 완료로 인정하지 않는다. 반복 callback/재진입은 같은 보상/전환을 다시 일으키지 않는다.

## Soft Clock의 목적을 구현 상태로 옮긴다

C: 핵심 정착 전에 밤이 오지 않고, 정착·성장 판단 뒤 일몰이 진행된다. full game 하루 길이는 미확정이다. R: 현재 데모는 정착 뒤 16→20 진행/20 준비 정지, OPEN 뒤 21:30 예고/22 마감 등 특정 설정을 사용한다. 이를 Canon의 영구 시간표나 레퍼런스에서 측정한 값으로 쓰지 않는다.

H: 세 개의 시간을 구분한다. GameClock의 세계 시각은 하늘/조명/상점 gate가 읽는다. UI/gesture의 시간은 pause와 modal 정책에 맞춰 읽는다. 연출의 시간은 항해 동안 Day 1의 소모가 아니다. 같은 Time.time/unscaledTime을 모든 목적에 무조건 쓰면 pause·일몰·hold 동작이 어긋날 수 있다.

모든 상태를 한 개의 대형 stage enum으로 재작성할 필요는 없다. 기존 milestone, world hour, shop open/closed, UI owner의 관계를 검증하면 된다. 전문 분야 창을 오래 읽는 플레이어에게 낮 준비를 빼앗지 않고, OPEN 뒤에는 마감의 예고와 결과가 읽히는 정책을 유지한다.

## 직접 노동에서 위임으로 이어지는 지식

플레이어는 먼저 ‘어디서, 어떤 도구로, 어떤 결과를 얻는가’를 알아야 NPC 도구 지급이 의미를 가진다(I, Canon §0). 제작 UI에서 최고 효율 수치를 먼저 가르치는 것보다, 직접 썼던 도구의 개선품을 내가 쓰거나 동행에게 주는 선택을 보여준다(H).

C: Workbench 해금/제작 결과는 플레이어 사용 또는 compatible NPC 지급으로 이어진다. NPC 도구에는 내구도와 unequip UI가 없으며 새 도구를 주면 이전 것을 돌려준다. 지급은 선택이다. Agriculture는 설명/preview만 제공하고 Hoe·씨앗·농사 loop를 지급/실행하지 않는다.

H: 새 도구 선물은 최소한 다음 사실을 한 번에 보존한다.

- 새 도구가 현재 가방에 있으며 해당 NPC가 받을 수 있다.
- old tool의 반환을 받을 공간/이전 경로가 있다.
- 성공이면 새 도구가 가방에서 빠지고 NPC 장비가 바뀌고 old tool이 돌아온다.
- 실패이면 새 도구·NPC 기존 도구가 모두 유지된다. 반환 공간이 없을 때 조용히 old tool 삭제 금지.

이는 새 선물 권위를 만들라는 제안이 아니다. 기존 ProducerNpcController/WorksiteBinding과 Inventory의 현재 gift/return 경로를 먼저 확인한다. NPC 도구 효과는 기존 생산 책임으로 이어지고 임의 ‘생산 성공’ VFX로 별도 재고를 지급하지 않는다.

## 영구 선택은 무엇을 선택했는지 읽힌 뒤 확정된다

C: 네 Root와 시너지 별은 강제/정답이 아닌 조언이다. 첫 선택은 데모에서 고정된다. H: 선택 전 분야명·현재 해금·다음 preview가 보이고, 확정 뒤 같은 선택이 Phone/작업대에서 읽혀야 한다. hover나 UI focus만으로 point를 소모하지 않는다. 입력을 두 번 받아 중복 point 소모/Root 변경을 일으키지 않는다.

시너지 별을 ‘반드시 고를 추천 정답’처럼 강조하면 두 동행 선택의 개성을 효율 퍼즐로 좁힌다(I). 다른 분야를 선택해도 Must Path가 완료되고 선택 도구 지급을 건너뛰어도 첫 밤이 성립해야 한다.

## 안내의 접근성과 진행의 독립성

한 번의 contextual hint는 해당 행동을 설명하며 진행 상태 자체가 아니다(H). 놓친 토스트는 P 기록에서 찾을 수 있어야 한다(C). 현재 Feed tile 부재로 DispatchLog 진입이 안 된다는 기록은 데이터 기록과 사용자 접근을 구분하는 사례(R). 알림을 더 오래 띄우는 것으로 대신하지 않는다.

First Shop mini guide의 네 단계는 지금 필요한 준비만 알리고 상시 quest list로 확대하지 않는다(C). 모든 선택 활동을 체크리스트로 만들면 Soft Clock이 지킨 자유가 텍스트 과제로 바뀐다.

## 검수

| ID | 경로 | 확인할 품질 |
|---|---|---|
| Q-D01 | 본사→동행→항해→항구 | 훈련 아이템 제거, 선택한 2명 유지, 조작 시작에서 clock 시작 |
| Q-D02 | fish/bug/추가 craft/gift 없이 Must Path | 핵심 정착·Root·일몰·개점·마감 가능 |
| Q-D03 | 느린 정착, 긴 UI 읽기, 이동/배치 실패 후 회복 | 준비 전에 밤 강제 없음, 전환 후 하늘/상점 같은 시각 |
| Q-D04 | Root 별 각각 선택·중복 입력·재진입 | 첫 선택 유지/1회 소모, Agriculture preview만 |
| Q-D05 | compatible/incompatible NPC, old tool 있음/가방 부족 | 성공의 이전/반환 일관, 실패 모두 보존, 지급 optional |
| Q-D06 | 알림 놓치기→P 기록→닫기 | 기록 접근 가능·UI 복귀, 임의 persistent quest 없음 |
| Q-D07 | NEW GAME 재시작/scene 재구성/Continue | 지원 범위를 기록, 이전 session 잠금/보상 누적 없음 |

현재 registry는 새 정착 상태의 persistence 확장이 없다고 기록한다(R). 이 연구는 Continue가 신규 Root/배정/preview를 복원한다고 가정하지 않는다. 새 저장 구현은 별도 계약이며 기존 Save schema를 임의 변경하지 않는다.
