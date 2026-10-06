# VERTICAL_SLICE_STUDIO_MODE

> Project P.A.의 **플레이어가 실제로 보는 3D 경험을 빠르게 제품 수준으로 끌어올리기 위한 제작 모드**.
>
> 이 모드는 `SAFE CORE MODE`를 대체하지 않는다. 저장/경제/데이터 마이그레이션처럼 권위와 무결성이 중요한 작업은 기존의 좁은 bounded-ticket 규칙을 계속 따른다.
> 반대로 월드, 카메라, 캐릭터 프레젠테이션, 입력감, 애니메이션, VFX/SFX, UI 피드백, 레벨 드레싱, 온보딩, 컷신처럼 **여러 요소를 동시에 맞춰야만 품질이 나오는 작업**에는 이 문서가 우선한다.

## 0. 활성화 문구

사용자 또는 상위 지시가 아래 중 하나를 명시하면 이 모드를 활성화한다.

- `VERTICAL SLICE STUDIO MODE`
- `STUDIO MODE`
- `Playable Island Rebase`
- `GameView polish pass`
- `Character feel pass`
- `Presentation pass`

활성화된 동안 작업의 기본 단위는 **파일 1개/기능 1개가 아니라 플레이어 경험 1개**다.

예:
- "숲 채집이 딩컴처럼 느껴지게"
- "도착 후 90초가 완성 게임처럼 보이게"
- "도끼를 들고 나무를 베고 보상을 얻는 전 과정을 제품 수준으로"
- "첫 영업 성공이 기억에 남는 10초 연출로"

## 1. 최우선 목표

Project P.A.의 정체성은 다음과 같다.

> 낮에는 동물 마을을 만들고, 밤에는 그 마을의 유일한 잡화점을 운영하는 3D 코지 라이프 시뮬레이션.

플레이어 판타지:
- 따뜻한 로우폴리 섬을 직접 걷는다.
- 나무를 베고, 돌을 캐고, 낚시하고, 벌레를 잡는다.
- 얻은 자원을 직접 들고 배치하고 상점에 진열한다.
- 가격을 정한다.
- 주민이 실제로 판단하고 구매/거절한다.
- 판매 결과가 돈, 상점, 마을 변화로 이어진다.

**화면에서 읽히지 않는 시스템 완성도는 Studio Mode의 완료 조건이 아니다.**

## 2. 현재 졸업 데모의 제품 목표

현재 목표는 Opening Demo Canon v2의 **12~15분짜리 finished-feeling vertical slice**다. 아래 경로는 `Docs/01_GAME_DESIGN/Canon/PROJECT_PA_OPENING_DEMO_CANON_V2_2026-09-16.md`의 MUST PATH를 따른다.

권장 플레이 흐름:

1. 기존 타이틀 NEW GAME → 출항 교육/가격 인증
2. 후보 3명 중 동행 2명 확정 → Pixel Voyage
3. Demo256 도착 → Supply Box
4. Forest → Axe → Wood / Highland → Pickaxe → Stone 계열 채집
5. Pioneer Shop/Base + Resident Tent ×2 실제 배치
6. Settlement Established → License Point → Specialization Root 선택
7. 자유 행동/대표 도구 제작 → 자연 일몰 → 밤 영업 준비
8. 기존 Display Stand에 실제 상품 진열 → 플레이어 가격 확정
9. OPEN → 실제 NPC 구매/거절 → Economy/SalesLog 일치
10. CLOSE → Pioneer Report → 데모 종료

Fish/Bug와 NPC 도구 지급은 선택 행동·Rank 보너스다. 별도 Management Hub, Day 2, 실제 Agriculture 시스템은 필수 경로에 추가하지 않는다. 이 제품 목표는 기존 티켓의 승인 범위를 자동 확장하지 않는다.

목표 감각:
- Dinkum처럼 직접 걷고, 들고, 사용하고, 배치한다.
- Animal Crossing처럼 캐릭터/주민/마을에 생기가 있다.
- Moonlighter처럼 가격과 판매 판단이 읽힌다.
- Dave the Diver처럼 중요한 성공 순간은 짧고 강하게 보상한다.
- 결과는 Project P.A.의 고유한 상점/마을 성장 판타지로 귀결된다.

참조작을 **복제하지 않는다**. 플레이 리듬과 품질 기준만 참고한다.

## 3. Studio Mode의 핵심 원칙

### 3.1 Experience Unit > File Unit

작업 범위는 필요한 경우 다음을 한 번에 포함할 수 있다.

- runtime code
- scene composition
- prefab
- material
- lighting
- particles
- audio hookup
- animator
- camera
- UI feedback
- interaction prompt
- asset placement
- small editor utility

단, 이것들은 모두 **동일한 플레이어 경험을 완성하기 위해 직접 필요해야 한다.**

"하는 김에" 기능 추가는 여전히 금지한다.

### 3.2 Existing Authority First

새 시스템을 만들기 전에 반드시 기존 권위를 찾는다.

기본 보존 권위:

- Player movement → existing `PlayerController`
- Camera → existing `CameraController`
- Interaction → `PlayerInteraction` + `IInteractable`
- Inventory → existing `Inventory`, `InventorySlot`, `ItemInstance`
- Hotbar → existing `Hotbar`
- Economy → `EconomyService`
- Shop → `ShopSlot`, `PurchaseEvaluator`, existing NPC customer flow
- Sales → `SalesLogManager`
- Placement → `WorldBuildingPlacementService`
- World grid → `WorldGridService`
- Generation → `WorldIslandGenerator`
- Navigation → `WorldNavigationService`
- Save → existing `SaveManager` / SaveData migration path
- NPC production → `ProducerNpcController` + `ProductionData`
- NPC profile/schedule → existing NPC profile/schedule stack

금지:
- `PlayerController2`
- second Inventory
- second Wallet/Economy
- second Shop purchase path
- second SaveManager
- second WorldGrid
- second Placement authority
- fake wallet/log calls
- parallel prototype system that bypasses accepted gameplay

### 3.3 Asset-First, Primitive-Last

이미 프로젝트에 있는 실제 자산을 먼저 사용한다.

우선순위:
1. Existing Project P.A. art/prefabs
2. Existing imported Quaternius / Kenney / Nature assets
3. Existing materials / animations / character models
4. Existing derived assets
5. 필요할 때만 간단한 procedural decoration
6. **Debug Cube/Capsule/flat-color primitive는 마지막 수단**

플레이어가 보는 최종 GameView에 임시 primitive가 남아 있으면 이유를 명시한다.

### 3.4 GameView Is The Product

다음은 "완료 증거"로 충분하지 않다.

- compile PASS만
- validator 100개 PASS만
- unit/integration test만
- hierarchy가 정상이라는 주장
- 코드 리뷰만

Studio Mode의 최종 증거에는 반드시 아래가 포함된다.

- 실제 GameView
- 실제 플레이어 입력
- 실제 카메라
- 실제 모델/애니메이션
- 실제 플레이 경로
- blocking runtime error 0

가능하면:
- 60~90초 플레이 캡처
- 또는 핵심 순간 3~6장의 실제 GameView 캡처

## 4. 자율 수정 권한

Studio Mode에서는 다음 실패를 **STOP 사유로 취급하지 말고 스스로 고친다.**

- compile error
- missing using/import
- serialized reference 누락
- animator parameter mismatch
- small prefab reference mismatch
- material assignment issue
- input action mismatch
- small collider/NavMesh issue
- validator fixture 문제
- 카메라 offset/clip 문제
- 단순 null-reference
- GameView에서 발견된 직접적인 layout/placement 문제

기본 자체 복구 예산:
- compile/fix cycle 최대 3회
- targeted Play 최대 3회
- validator 최대 1개
- 동일 경험을 완성하기 위한 소규모 glue 파일 추가 허용

**진단만 하고 작업을 종료하지 않는다.**

## 5. HARD STOP

다음에만 중단한다.

- destructive Git 명령이 필요함
- unrelated dirty work를 덮어써야 함
- 사용자 파일/씬/저장 데이터 손실 위험
- 기존 확립 권위를 교체해야만 구현 가능
- 호환되지 않는 SaveData migration이 필요
- 대형 아키텍처 재작성 없이는 진행 불가
- 승인되지 않은 외부 패키지/외부 에셋 다운로드 필요
- Unity/환경이 복구 예산 이후에도 실행 불가

중단 시:
1. 무엇이 막혔는지
2. 지금까지 무엇이 살아 있는지
3. 가장 작은 인간 결정 1개
만 보고한다.

## 6. Git / Dirty Workspace

로컬 작업공간이 진실의 원본이다.

- GitHub의 branch 상태를 로컬 dirty 상태로 추정하지 않는다.
- 시작 시 `git status`를 읽는다.
- unrelated dirty 파일은 그대로 보존한다.
- reset / clean / stash / broad revert 금지.
- 자동 commit 금지.
- 자동 push 금지.
- 사용자 승인 없는 파일 삭제 금지.

기존 dirty 작업을 보호하는 것이 validator 재현성보다 우선한다.

## 7. 문서 비용 제한

Studio Mode 중 매번 다음 문서를 모두 갱신하지 않는다.

- CHANGELOG_AI
- STATUS
- TODO
- SESSION_REPORT
- 개발일지
- HANDOFF

대신 **실제 제작이 끝난 뒤 필요한 최소 상태 기록만 1회** 갱신한다.

작업 중 토큰은 다음에 우선 사용한다.

1. 실제 코드/씬/프리팹 조사
2. 자산 조사
3. Unity 편집
4. Play
5. GameView 평가
6. 수정
7. 마지막 기록

## 8. Visual Quality Gate

최종 GameView를 아래 기준으로 직접 평가한다.

### Composition
- 캐릭터가 너무 작아 보이지 않는가?
- 플레이 목표/랜드마크가 화면에서 읽히는가?
- 화면에 과도한 빈 공간이 없는가?
- 카메라 높이/거리/FOV가 코지 3인칭 플레이에 맞는가?

### World Density
- 숲은 실제 숲처럼 밀도가 있는가?
- 해안/초원/고지대가 실루엣만으로 구별되는가?
- 동일 오브젝트가 기계적으로 반복되지 않는가?
- 바닥과 환경 오브젝트 사이가 비어 보이지 않는가?

### Character
- Idle/Walk/Run/Tool action이 상태와 맞는가?
- 이동 방향과 캐릭터 facing이 일치하는가?
- 도구가 손에 보이는가?
- action 시작/impact/end가 읽히는가?

### Interaction
- 무엇을 상호작용할 수 있는지 가까이 갔을 때 명확한가?
- 성공/실패 피드백이 즉시 보이는가?
- UI보다 월드 행동이 먼저 느껴지는가?

### Feedback
- hit / gather / placement / sale에 시각 또는 청각 반응이 있는가?
- 보상이 어디서 왔는지 플레이어가 즉시 이해하는가?
- 중요한 순간이 평범한 순간보다 강한가?

### Cohesion
- Project P.A. / Quaternius / Kenney / Nature 자산 조합이 하나의 게임처럼 보이는가?
- 임시 Debug UI/label이 남아 있지 않은가?
- 색/scale/material이 서로 충돌하지 않는가?

## 9. 자동 검증과 인간 검증의 역할

자동 검증:
- 돈 중복 지급 방지
- 아이템 중복 소비 방지
- save integrity
- 실제 callback
- required reference
- compile/runtime error
- authority reuse

사람/GameView 검증:
- 재미
- 카메라
- 타격감
- 조작감
- 아트 일관성
- 가독성
- 애니메이션 리듬
- 공간 밀도

자동 검사로 인간의 감각을 대신하지 않는다.

## 10. Studio 작업 보고 형식

작업 전 장문의 문서 요약은 금지한다.

시작 보고:

```text
STUDIO MODE ACTIVE

Experience target:
- <플레이어 경험>

Reuse:
- <기존 권위/자산>

Likely edit surface:
- runtime
- scene/prefab
- art bindings
- UI/audio if required

Protected:
- Save/Economy/etc.

I will iterate through Play and GameView instead of stopping at the first mechanical failure.
```

종료 보고:

```text
RESULT: PASS / PARTIAL / HARD BLOCKED

PLAYER EXPERIENCE:
- ...

CHANGED:
- ...

REUSED:
- ...

GAMEVIEW:
- capture path / screenshots

VERIFIED:
- runtime compile
- actual Play
- critical authority checks

HUMAN VISUAL DEBT:
- ...

GIT:
- no commit
- no push
- unrelated dirty preserved
```

## 11. 현재 권장 제작 패스

### PASS A — PLAYABLE ISLAND REBASE
도착 → Demo256 → 월드 밀도 → 4개 자원 구역 → 카메라 → 랜드마크 → Debug 제거.

### PASS B — CHARACTER FEEL
Idle/Walk/Run/Jump(현재 설계에 맞으면) → Axe/Pickaxe/Net/Fishing → held tool → impact feedback.

### PASS C — SHOP PRESENTATION
실제 채집품 → placement → stock → price → OPEN → NPC purchase/reject → 명확한 반응.

### PASS D — FIRST BUSINESS DAY
실제 첫 판매를 트리거로 8~15초 성공 연출 + 다음 성장 티저 + DEMO END.

### PASS E — RELEASE
NEW GAME → 10~15분 전체 → save/reentry → Windows build → 녹화.

## 12. 완료 정의

Studio Mode에서 "완료"는 다음 문장으로 설명할 수 있어야 한다.

> "사람이 실제 GameView를 봤을 때, 구현된 시스템이 아니라 하나의 게임 장면으로 보인다."

이 기준을 못 넘으면 validator가 모두 PASS해도 작업은 아직 끝나지 않았다.
