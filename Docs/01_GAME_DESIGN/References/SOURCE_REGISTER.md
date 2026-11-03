# 레퍼런스 근거와 확인 범위

확인: 2026-10-18. 일반 제작에서는 이 표가 답하는 사실을 다시 검색하지 않는다. 새로운 사실/반례가 생기면 관련 항목만 갱신한다. 인터뷰는 개발자 의도를 확인하는 1차 발언이며 원작의 내부 코드나 P.A.의 성과를 증명하지 않는다. 원문 전문은 복제하지 않는다.

## 개발자 직접 발언

| ID | 자료·발행일 | 이번에 확인한 범위 | 연결 |
|---|---|---|---|
| S-DK1 | [What's It Like — James Bendon interview](https://www.whatsitlike.com.au/interview-with-dinkum-developer-james-bendon/), 원문 게시일 미확인 | 호주 풍경이 미술 방향에 준 영향; 메뉴보다 공간 속 실물 배치를 선호한 참고점 | DK-03 |
| S-DK2 | [Vooks — James Bendon interview](https://www.vooks.net/interview-chatting-with-dinkum-developer-james-bendon/), 2026-01-28 | 상자 이동 요구를 손수레로 푼 이유; 보이는 물고기와 실제 어획의 일치; 도전과 좌절의 구분 | DK-01, DK-04 |
| S-AC1 | [The Verge — Aya Kyogoku / Hisashi Nogami interview](https://www.theverge.com/2020/3/20/21188006/animal-crossing-new-horizons-design-interview-aya-kyogoku-hisashi-nogami), 2020-03-20 | 무인도 시작으로 관계를 바꾼 이유; Miles로 다양한 활동을 발견하게 한 의도 | AC-01, AC-02 |
| S-ML1 | [Push Square — Digital Sun interview](https://www.pushsquare.com/news/2018/05/interview_talking_shop_with_action_rpg_moonlighters_developer), 2018-05-09 | 탐험에서 영업을, 영업에서 다음 탐험을 생각하게 하는 상호 의존 | ML-01 |
| S-ML2 | [MCV/DEVELOP — When We Made Moonlighter](https://mcvuk.com/development-news/when-we-made-moonlighter/), 2018-12-18 | 금 중심 진행과 수입량의 쉬움/반복 노동 균형 문제 | ML-01 |
| S-DV1 | [Software Engineering Daily — Jaeho Hwang / Minsuk Cha](https://softwareengineeringdaily.com/articles/developing-dave-the-diver-with-jaeho-hwang-and-minsuk-cha/), 원문 게시일 미확인 | 작살 핵심 감각; 2D/3D 결합 이유; 자동 영업에서 직접 조작으로의 변경; 캐릭터를 통한 학습 | DV-01, DV-02 |

현재 의사결정은 최신 게시물 수집이 아니라 위 발언의 맥락과 현재 Canon의 적합성에 근거한다. 원작에 나중에 추가된 기능이나 지금의 도구 버전은 여기서 확인한 사실이 아니다.

## 기존 역분석을 이 저장소에 이식한 범위

아래는 이번 작업에서 읽은 기존 연구 MD의 파일명이다. 원본은 외부 연구 자료이며 이 저장소의 링크 가능한 파일로 가장하지 않는다. **제작에 필요한 결론은 결정 문서에 수록했으므로 원본 접근 없이 작업할 수 있다.** 표의 관찰/해석은 개발자 발언과 구분한다.

| ID | 기존 연구 MD | 이번에 이식한 결론/한계 |
|---|---|---|
| R-01 | `01_DINKUM_ACTION_AND_WORLD.md`, `REFERENCE_REVERSE_ANALYSIS.md` | 시도·월드 결과·획득 반영의 구분, 동일 대상, 모션/피드백 연결. 짧은 영상은 완전한 자연 플레이 증거가 아님 |
| R-02 | `02_PLACEMENT_AND_SETTLEMENT.md`, `WORLD_VISUAL_README_2026-10-12.md`, `PA_URP_WORLD_VISUAL_KNOWLEDGE_2026-10-12.md` | preview와 실제 배치의 약속; H01~H08의 공간 관찰 축. 원작 개발자가 밝힌 원칙/수치로 승격하지 않음 |
| R-03 | `WORLD_VISUAL_GAMEVIEW_VALIDATION_2026-10-12.md` | 실제 화면·연속 동작·일반 카메라의 증거 한계. 과거 캡처를 새 runtime PASS로 사용하지 않음 |
| R-04 | `03_MOONLIGHTER_COMMERCE.md` | 가격 임시/확정/취소, 고객 평가와 거래 commit의 구분. 원작 공식·RNG 구조는 미확인 |
| R-05 | `04_DAVE_LOOP_AND_CINEMATIC.md`, `05_PRESENTATION_AND_QUALITY.md`, `06_INTEGRATION_AND_WORK_PACKETS.md` | 낮밤의 의미 연결, 짧은 성취와 조작 복귀, 실제 사실 기반 Report. 과거 작업 packet은 새 승인/큐로 옮기지 않음 |

## P.A. 내부 적용 근거

- [Opening Demo Canon v2](../Canon/PROJECT_PA_OPENING_DEMO_CANON_V2_2026-09-16.md): §4~5 입력/도구, §7~12 공간/배치, §15~18 시계/영업/가격/안내, §20~22 Report/MUST PATH/HUD, §24 범위 제한.
- [Design Intent](../Creative/PROJECT_PA_DESIGN_INTENT.md), [World North Star](../Creative/PROJECT_PA_WORLD_NORTH_STAR.md), [Style Grammar](../Art/PROJECT_PA_ART_STYLE_GRAMMAR.md): 정체성·공간·자산 방향. 현재 Canon/승인과 충돌하는 과거 기본값은 실행하지 않는다.
- [Capability Registry](../../00_CURRENT/CAPABILITY_REGISTRY.md), [Game Loop Map](../../00_CURRENT/GAME_LOOP_MAP.md), [Code Reuse Map](../../02_IMPLEMENTATION/PROJECT_PA_CODE_REUSE_MAP_v1.md): 실제 구현과 재사용 입구. 결정에 적힌 권위 이름은 탐색 시작점이며 구현 완료 주장이나 정확한 현재 메서드 계약이 아니다.
- 기존 [GAME_LOOP](../Canon/PROJECT_PA_GAME_LOOP.md)·[VISUAL_TARGETS](../Art/VISUAL_TARGETS.md)는 과거 설계/상태를 포함한다. 현재 데모 계약과의 충돌 경고를 앞에 추가하고 본문 이력은 보존했다.

## 미확인으로 남기는 것

원작의 전체 개발 비용/기간 대비 P.A. 자동화 성과, Steam 평가를 보장하는 인과관계, 정확한 카메라·모션·경제 수치, 내부 소스 구조는 확인하지 않았다. 이 지식 묶음은 반복 조사와 잘못된 이식을 줄이기 위한 제작 입력이다. 게임의 실제 구현·재미·화면 품질은 현재 제작 결과로 판단한다.
