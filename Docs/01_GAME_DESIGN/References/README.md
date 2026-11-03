# 레퍼런스 설계 결정 — 작업 입구

갱신: 2026-10-18. 적용 기준: `milestone/gameplay-beta-85`의 `b121140a`.

목적은 레퍼런스의 기능을 복제하는 것이 아니라 **해결하려던 문제 → 선택 이유 → P.A.에서 구현할 행동**을 재사용하는 것이다. 기존 역분석 MD와 개발자 인터뷰를 아래 결정으로 압축했다. 일반 제작에서는 원본을 다시 수집하지 않는다.

## 권위와 범위

- 이 묶음은 **채택된 제작 판단 지침**이다. 새 Canon, 작업 큐, 구현 완료 기록이 아니다. 최신 직접 사용자 지시, 현재 Canon과 승인 범위, 기존 시스템 권위를 따른다.
- 데모 계약: [Opening Demo Canon v2](../Canon/PROJECT_PA_OPENING_DEMO_CANON_V2_2026-09-16.md). 장기 정체성: [Identity](../Canon/PROJECT_PA_IDENTITY.md), [Design Intent](../Creative/PROJECT_PA_DESIGN_INTENT.md). 상충하면 레퍼런스보다 이 계약들이 우선한다.
- 데모는 Day 1 → Pioneer Report까지다. 물건 판매가 마을 생활을 바꾼다는 완성 게임의 방향은 유지하되, 레퍼런스를 이유로 Day 2·농사·별도 Hub·새 경제/저장 권위를 추가하지 않는다.
- 이 문서 도입은 현재 D2의 중단·예산 소진, Unity 담당자, 기존 approval과 후속 티켓 상태를 변경하지 않는다. 게임 변경은 해당 기존 실행 계약에서 진행한다. 별도 AI 자율성 실험을 도입 선행 조건으로 만들지 않는다.

## 필요한 결정만 읽기

공통 시작 문서와 현재 작업 계약을 먼저 읽는다. 아래에서 **관련 결정 1~3개**를 골라 해당 절만 읽는다. 전체 게임 문서·소스 원문을 매번 로드하지 않는다. 명백한 기능 버그처럼 레퍼런스 판단이 필요 없는 작업은 생략한다.

| 현재 작업 | 먼저 읽을 결정 | 기존 구현에서 찾을 권위 |
|---|---|---|
| 이동·도구·채집·획득 피드백 | [DK-01, DK-02](DINKUM_ACTION_WORLD.md), 필요 시 [DV-01](DAVE_MILESTONES.md) | PlayerController, PlayerInteraction, EquipmentSystem, Inventory |
| 배치·이동·취소·공간 구성 | [AC-01](ACNH_SPACE_LEARNING.md), [DK-03](DINKUM_ACTION_WORLD.md) | WorldBuildingPlacementService, WorldNavigationService, WorldPersistenceService |
| 월드 드레싱·카메라·낮밤 가독성 | [DK-03](DINKUM_ACTION_WORLD.md), [AC-03](ACNH_SPACE_LEARNING.md) | CameraController, 기존 월드 presentation 및 로컬 자산 |
| 도입·행동 안내·휴대폰 기록 | [AC-02](ACNH_SPACE_LEARNING.md), 필요 시 [DV-02](DAVE_MILESTONES.md) | 기존 onboarding/presentation, 보호된 SmartphoneUI 계약 |
| 진열·가격·손님·영업 | [ML-01~03](MOONLIGHTER_COMMERCE.md) | ShopSlot, ShopPriceUI, PurchaseEvaluator, EconomyService, SalesLogManager |
| 항해·첫 판매·Report·전환 | [DV-02, DV-03](DAVE_MILESTONES.md) | 기존 opening/FirstDayWorldPresentation, GameClock, 실제 SalesLog |
| 새 기능/QoL 제안 판단 | [DK-04](DINKUM_ACTION_WORLD.md) + 해당 기능 결정 | 기존 큐·승인·Capability; 새 작업 자동 생성 금지 |

## 근거 읽는 법

각 결정은 `D` 개발자 직접 설명, `O` 기존 플레이/영상 역분석, `I` 우리의 설계 해석, `P` P.A. 적용을 구분한다. 개발자가 말하지 않은 구현 구조·수치·심리 효과를 `D`로 적지 않는다. `P`는 우리 게임의 선택이지 원작의 내부 구현이라는 주장이 아니다. 근거의 위치와 한계는 [SOURCE_REGISTER](SOURCE_REGISTER.md)에 있다.

## 제작에 쓰는 순서

1. 현재 승인 작업에 관련된 결정 ID와 Canon 절을 고른다.
2. 그 결정을 현재 실제 코드/최근 화면 증거와 연결한다. 구현이 이미 있으면 재작성하지 않는다.
3. 차이가 있으면 플레이어에게 보일 변화와 기존 권위를 한두 문장으로 정하고 직접 적용한다. 예: `ML-03 → 말풍선은 평가 결과만 표시, 판매 성공은 기존 commit 뒤에만 표시`.
4. 변경한 경로에 필요한 기존 검증을 수행한다. 문서 도입을 위해 새 벤치마크·검증 프레임워크를 만들지 않는다.
5. 작업 결과에는 `적용 ID / 변경 / 근거 / 남은 차이`만 짧게 남긴다. 기존 월별 이력·handoff 규칙을 따르고 결정 원문을 복사하지 않는다.

## 재조사와 사용량 절약

- 결정이 현재 질문에 답하고 Canon 충돌이 없으면 **웹 검색 0회**로 제작한다. 세션이 바뀌었다는 이유로 네 게임을 다시 조사하지 않는다.
- 재개 조건은 각 결정의 조건, 새 사용자 방향, 실제 플레이에서 발견된 반례, 출처 정정, 구현을 바꾸는 미해결 질문이다. 질문을 한 문장으로 먼저 적고 필요한 1차자료만 추가한다. 원문 접근 실패는 실패로 남기며 개발자 의도를 추정해 채우지 않는다.
- 원작 수치(FOV, 타격 프레임, 가격 공식, 컷신 길이)는 확인되지 않았다. P.A.의 현재 계약·실제 자산·관측으로 정한다. 몽타주나 무음 짧은 영상으로 자연 플레이·음질·모션 품질을 확정하지 않는다.
- 새 GitHub 도구·프레임워크는 현재 작업의 구체적 병목을 해결할 때만 검토한다. 기존 도구로 해결되면 추가하지 않는다. 외부 패키지/자산의 기존 승인 규칙은 유지한다.
- 공유 Unity·Assets·meta 반영은 단일 owner가 맡는다. 비중첩 후보 생산과 독립 조사는 허용된 범위에서 병렬화하되 전체 저장소·같은 자료를 여러 세션에서 중복 조사하지 않는다.
- 새 사실은 관련 결정과 출처 항목만 갱신한다. 동일 지식을 CLAUDE.md·AGENTS.md·여러 스킬에 복제하지 않고 이 입구를 가리킨다. 이는 저장소 기반 지식 재사용이며 모델 가중치를 훈련한 것은 아니다.

## 도구 후보를 여는 조건 — 2026-10-21

- **2D 아이콘/작은 효과/그림 UI가 실제 필요할 때만** [sprite-gen](https://github.com/aldegad/sprite-gen)과 [기존 이미지 우선 경로](https://github.com/aldegad/sprite-gen/blob/main/docs/user-workflow.md)를 검토한다. 현재 설치/생성은 하지 않았다. 기존 승인 이미지의 분리·투명화·색 변형으로 해결 가능한지 먼저 확인한다. 도입 시 기존 지원 job의 격리 tools 경로·버전/변경안·명시 provider·해당 기능의 현재 접근 가능 여부·생성 child와 rollout/사용 기록 보존을 확인하고, 원본 Unity import는 owner가 한다. 로그인만으로 기능/quota를 추정하거나 유료 API로 전환하지 않는다. 승인에 없는 설치/외부 에셋은 정확한 변경안을 검토하는 기존 절차를 따른다.
- **소스·개발자 공개 설명·보존된 플레이 관찰로 답하지 못한 특정 배포 파일 질문이 있을 때만** [REA](https://github.com/morluto/rea/blob/main/.agents/skills/reverse-engineer-anything/SKILL.md)를 검토한다. 현재 그런 질문/설치 과제는 없다. 필요한 조사 결과는 이 입구의 관련 기존 결정 md와 SOURCE_REGISTER에 `관측 구현 / 개발자가 밝힌 이유 / 우리 추론 / 미확인`으로 구분한다. 배포 코드에서 보았다는 이유로 개발 의도나 자연 동작을 확정하지 않는다. 설치된 Dinkum 실행 금지는 유지한다.
- 표현·진행 판단은 [현재 미술 기준](../Art/VISUAL_TARGETS.md#현재-대표-구역-검수--2026-10-21)과 현재 실제 데이터/검증부터 사용한다. 외부 조직·.claude·settings·hooks·supervisor·새 티켓/기억 체계를 복사하지 않는다.

## 오래된 문서와 충돌할 때

[VISUAL_TARGETS](../Art/VISUAL_TARGETS.md)의 상시 좌측 목표 패널·탑다운 고정·야외 부스 중심 제안과 [GAME_LOOP](../Canon/PROJECT_PA_GAME_LOOP.md)의 과거 추천가/구매확률·여러 날 시연 항목을 현재 지시로 실행하지 않는다. 현재 Canon §7·§16~17·§20~22와 실제 승인 경험을 따른다. 원본 문서의 과거 이력은 보존한다.
