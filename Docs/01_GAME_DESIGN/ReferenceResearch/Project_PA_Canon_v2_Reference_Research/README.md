# Project P.A. — Canon v2 게임 레퍼런스 연구

연구본: 2026-10-11. Canon v2를 바꾸지 않고, 실제 Unity 플레이의 판단·입력·상태·표현·실패를 설계하는 재사용 자료다. 고정 저장소 HEAD: `b121140ae6f5ca0f92078db5c2f75512dc8fc95a`.

**두 입구:** [상세 레퍼런스 역분석](REFERENCE_REVERSE_ANALYSIS.md) / [Canon v2 개발용 설계 지식](CANON_V2_DEVELOPMENT_KNOWLEDGE.md).

## 먼저 알아야 할 근거 범위

공식 짧은 영상 8개를 실제 프레임으로 관찰하고, 공식 조작 문헌·제작자 발언·P.A. 최신 기록 및 코드 10개를 대조했다. Dinkum·Moonlighter 1·Dave의 시각 장면은 타임스탬프로 찾을 수 있다. ACNH는 공식 문헌 중심, Moonlighter 2는 버전별 개발 발언 중심이다. 장시간 무편집 플레이·원작 음향·정확한 동작 타이밍은 미검증이며 내부 구현을 발명하지 않았다. 원본 clip은 640×360–1000×290/780×214로 제한되므로 ‘고해상도 영상 전체 분석’이라고 주장하지 않는다.

이 연구는 시스템 분석과 P.A. 적용의 정밀도를 높인 자료다. 더 좋은 영상을 확보하면 [미확인 연구 단위](03_evidence/OPEN_QUESTIONS.md)대로 해당 시스템만 깊이를 추가할 수 있다. Unity 플레이 검증·코드 변경·저장 schema 변경은 이번 연구에서 수행하지 않았다.

## 필요한 시스템을 바로 찾기

| 시스템 ID | 개발 질문 | 원작 분석 | 개발 계약 |
|---|---|---|---|
| ACT-01 | E가 무엇을 실행하며 도구·획득이 왜 믿을 만한가? | [Dinkum 행동](01_reverse_analysis/01_DINKUM_ACTION_AND_WORLD.md), [ACNH 문법](01_reverse_analysis/02_ACNH_LEARNING_OWNERSHIP.md) | [입력·손·채집](02_design_knowledge/01_ACTION_EQUIPMENT_GATHERING.md) |
| ACT-02 | 낚시·벌레의 상태와 실제 보상을 어떻게 구분하나? | [선택 활동](01_reverse_analysis/05_OPTIONAL_FISHING_AND_BUGS.md) | [행동 계약](02_design_knowledge/01_ACTION_EQUIPMENT_GATHERING.md), U-04/U-07 |
| WORLD-01 | 단차·biome·자원이 왜 읽히는가? | [Dinkum 세계](01_reverse_analysis/01_DINKUM_ACTION_AND_WORLD.md) | [섬·배치](02_design_knowledge/02_PLACEMENT_AND_SETTLEMENT.md) |
| PLC-01 | 배치 자유와 문/고객 동선을 어떻게 함께 보존하나? | [Dinkum](01_reverse_analysis/01_DINKUM_ACTION_AND_WORLD.md), [ACNH 소유](01_reverse_analysis/02_ACNH_LEARNING_OWNERSHIP.md) | [배치·정착](02_design_knowledge/02_PLACEMENT_AND_SETTLEMENT.md) |
| COM-01 | 가격 반응이 학습 자료가 되며 실제 판매와 맞는가? | [Moonlighter](01_reverse_analysis/03_MOONLIGHTER_COMMERCE.md) | [진열·가격·고객](02_design_knowledge/03_COMMERCE_PRICE_CUSTOMER.md) |
| DAY-01 | 자유 활동을 보호하면서 정착·성장·밤을 연결하나? | [ACNH](01_reverse_analysis/02_ACNH_LEARNING_OWNERSHIP.md), [Dave](01_reverse_analysis/04_DAVE_LOOP_AND_CINEMATIC.md) | [진행·시간·위임](02_design_knowledge/04_PROGRESS_TIME_DELEGATION.md) |
| PRE-01 | 모션·카메라·UI·음향·항해·보고서가 어떤 사실을 전달하나? | [Dave 연출](01_reverse_analysis/04_DAVE_LOOP_AND_CINEMATIC.md) 및 각 동작 분석 | [표현·품질](02_design_knowledge/05_PRESENTATION_AND_QUALITY.md) |

## 다른 AI를 위한 읽기 순서

1. [경험 구조](00_basis/01_EXPERIENCE_MAP.md)와 관련 Canon 조항을 읽는다.
2. 해당 시스템의 원작 분석에서 V/D와 I를 분리해 읽는다. [방법](00_basis/02_RESEARCH_METHOD.md)이 라벨을 정의한다.
3. 개발 계약에서 상태·소유자·취소/실패·품질 시나리오를 읽는다.
4. [통합 책임/작업 단위](02_design_knowledge/06_INTEGRATION_AND_WORK_PACKETS.md)에서 현재 코드 경계와 W 작업을 고른다.
5. 새 구현 후 Q 시나리오를 실제 입력으로 수행한다. fixture 기능 회귀와 자연 플레이 품질을 구분한다.

한 행동을 끝까지 따라가려면 [플레이 단위 사례 3개](02_design_knowledge/07_PLAY_UNIT_CASES.md)를 읽는다. 첫 나무 작업, 고객 browse 중 가격 조정, Workbench 이동 취소를 인식·입력·상태·표현·실패까지 연결한다.

첫 구현 판단으로는 ‘모든 거절을 과가격으로 설명하지 않기’, ‘획득 표현은 실제 지급 사실에서만 나오기’, ‘배치 preview가 실제 공간과 복원을 약속하기’를 우선한다. 이는 기존 권위를 교체하지 않고 경험의 읽기와 일관성을 개선하는 설계 가설이다.

## Canon·출처·후속 연구

- [Canon 원문 보존본](00_basis/CANON_V2_snapshot.md) / [IDENTITY 원문 보존본](00_basis/IDENTITY_snapshot.md)
- [Canon 추적표](00_basis/03_CANON_TRACEABILITY.md)
- [출처·버전 원장](03_evidence/SOURCE_LEDGER.md) / [실제 관찰 장면](03_evidence/SCENE_LEDGER.md)
- [미확인 사항과 다음 독립 연구](03_evidence/OPEN_QUESTIONS.md)
- [연구본 검증 기록](VALIDATION.md)

원작 자산/영상/프레임은 재배포하지 않는다. 출처 URL과 관찰 기록만 제공한다. 연구본의 H는 구현 제안이며 이미 적용된 변경이나 Canon의 신규 의무가 아니다.
