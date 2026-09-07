# Project P.A. 2026 여름방학 개발 감사

## 분석기간

- 공식 집계 기간: `2026-06-26 00:00:00 ~ 2026-08-31 23:59:59` (KST)
- 현재 감사 기준: `milestone/gameplay-beta-85@29fb98f400b5`
- Git 기준선: 6월 26일 이전 마지막 도달 커밋 `adc5d2f5eaa8` (author date 2026-05-19)
- 보조 기준: 2026-06-02 중간발표 자료, 2026-05-12 11주차 자료, 6월 21~25 기록, 6월 26일 첫 체크포인트 `09af7bc`
- 주의: 6월 26일 체크포인트에는 6월 21~25 작업도 함께 들어 있다. 따라서 커밋 경계만으로 방학 성과를 계산하지 않고 문서 작성일·발표자료·코드 존재 시점을 교차 확인했다.

## 감사 결론

6월 25일의 Project P.A.는 이동, 인벤토리, 상점 진열, 가격 설정, NPC 구매 판단, 경제, 기본 저장 등 **기능 단위 기반**이 이미 있었다. 방학 동안의 실제 변화는 이 기능을 다시 만든 것이 아니라 다음 네 방향으로 묶인다.

1. 낮 활동 → 제작 → 진열·가격 → NPC 구매 → 결산·마을 반응을 연결했다.
2. 절차 섬과 건물·가구 배치, 내비게이션, 기존 게임플레이 연결을 `WorldSandbox`에서 검증했다.
3. 고객 성향, 구매/거절 이유, 휴대폰 채용·판매 피드, 주민/마을 반응을 실제 데이터에 연결했다.
4. 저장 범위를 확장하고 검증 자동화를 강화했으나, 재시작 후 플레이어 방향 복원 실패가 최종 통합 blocker로 남았다.

현재는 설명자가 안내하면 Build Settings의 `Prototype_FirstDay`에서 Day 1 판매 데모를 진행할 수 있다. 더 완성도 높은 M85 생성 월드 루프는 `WorldSandbox`에서만 검증돼 일반 진입 경로에서는 `UNREACHABLE`이다. 1920×1080 standalone 결과도 아직 `UNOBSERVABLE`이다.

## 6/25 Baseline

방학 이전에 이미 존재했다고 판정한 항목은 다음과 같다.

- Player 이동·상호작용, HUD, Inventory, Hotbar
- Smartphone 기본 앱 흐름과 첫날 온보딩
- `ShopSlot`, 가격 조정 UI, 상품 진열
- `EconomyService`, `TierService`, `AuditService`
- `NpcController` FSM, MBTI 프로필, `PurchaseEvaluator`
- Friendship/Hiring 기본 구조
- `SaveManager`/`SaveData` 기본 저장 구조
- `CraftingService`, Workbench/Recipe 데이터 기반
- Scene Auto Builder, Dev Console
- 6월 21~25에 연결된 Day/Night 흐름, 고객 성향 프레젠테이션, 낮 채집→밤 개점 게이트, Core Slice 표시 모드

근거:

- Git `adc5d2f` 트리에 위 핵심 클래스가 이미 존재한다.
- 11주차 자료는 이동/HUD/인벤토리/핫바/스마트폰, ShopSlot/가격, Economy/Tier, NPC FSM/MBTI/구매 판단, 기본 저장/자동화를 이미 구현 상태로 기록한다.
- 6월 2일 중간발표도 같은 시스템을 “현재 구현 현황”으로 제시한다.
- 6월 21~25 기록은 Day/Night, 채집, 고객 피드백, 마을 방향 신호, Core Slice를 방학 시작 전에 이미 연결한 것으로 기록한다.

## 방학 신규 구현

- 판매 카테고리에 따라 다음 날 실제 광장 표현이 바뀌는 첫 시각 변화(VC-001A).
- 실제 낚시와 광질 상호작용 및 획득물의 밤 판매 왕복.
- Tier 1 실내 상점, 실내 고객 방문·구매, 이동 가능한 판매대/건물 배치.
- 2m cell/16×16 Chunk 기반 절차 섬, 높이·지면·길·물 편집, 월드 건물 배치, sector 내비게이션.
- WorldSandbox에서 기존 Inventory/Crafting/Shop/NPC/Economy/Save 권위를 재사용하는 playable world adapter.
- 생성 월드 온보딩, 숲·농장·광산·해변 활동, B05~B07 제작, 판매대 가독성.
- 휴대폰 채용 후보 8명과 실제 고용, 판매 Feed, 관광객 고객 계층.

## 방학 확장/연결

- 기본 상점 루프를 낮 활동→가공→진열→가격→개점→구매/거절→결산으로 확장.
- 구매·거절 이유, 수요, 당일 통계, 카테고리 방향, 다음 날 마을 반응을 같은 판매 데이터에 연결.
- 기본 저장을 v8~v12로 확장해 일일 활동, 마을 변화, 월드 delta, B09/가구, SalesLog/Feed, 플레이어/핫바/상점 상태를 포함.
- Day 1 중심 진행을 7일/장기 운영 체크리스트와 결산 목표로 확장.
- 직접 `Camera.Render()` 기반 검증 경로를 안전 Game View 캡처 흐름으로 교체하고 bounded ticket/validator 회귀 체계를 정리.

## Before → Summer Work → Current 변화표

| 시스템 | 6/25 이전 상태 | 6/26~8/31 변화 | 8/31 상태 | 판정 | 대표 근거 |
| --- | --- | --- | --- | --- | --- |
| Player | 이동·상호작용 구현 | 생성 월드 이동/안전 셀, 온보딩, 월드 HUD 연결 | Prototype 이동 가능, WorldSandbox 이동 검증 | CONNECTED | BETA-001/002, M70 |
| Onboarding | 첫날 기본 흐름 | 타이틀·조작·생성 월드 랜드마크 안내 | Prototype 진입 PASS, 화면 정보 경쟁 | PARTIAL | Visual Audit A~D |
| Inventory | Inventory/Hotbar 존재 | 활동·제작·보관함·판매대 왕복, 전량 수용 선검사 | 기능 연결, 인벤토리 화면 잘림 | PARTIAL | BETA-002/003, Visual Audit F |
| Smartphone | 기본 앱 흐름 | 감사 정보, 채용 8명, 판매 Feed | Prototype 감사 연결, M85 채용/Feed는 sandbox 전용 | PARTIAL | BETA-006, Visual Audit L |
| Shop | 진열·가격·구매 기반 | 실내 상점, Tier 해금, 이동 판매대, 상품/품절 표시 | Prototype 판매 가능, 최신 merchandising은 sandbox 전용 | PARTIAL | BETA-004, Golden route |
| Economy | Money/Economy/Tier/Audit 존재 | 구매/거절 통계, 결산, 시설 방향·성장 피드백 | 실제 500G→530G 판매·감사 반영 | VALIDATED | Visual Audit L, BETA-005 |
| NPC AI | FSM/MBTI/PurchaseEvaluator 존재 | 고객 유입·실내 방문·성향별 결과·관광객·작업 anchor | 구매/거절 검증, 주민 통합 후반은 검증 부채 | PARTIAL | BETA-005, BETA-007 |
| Relation | Friendship/Hiring 기반 | 휴대폰 고용, roster, 주민 대화·마을 반응 연결 | 고용 PASS, hire→sale→Day2 대화 미검증 | PARTIAL | BETA-006/007 |
| Save/Load | 기본 저장 구조 | v8~v12 확장, 월드 delta·판매/Feed·B09·플레이어 상태 | 동일 세션 복원 PASS, 재시작 facing 137° 실패 | BLOCKED | BETA-009/010 logs |
| World | 고정 Prototype scene | 절차 섬, Terraform, 물/길, 배치, Nav, 기존 시스템 adapter | M70 검증 완료, Build Settings에서 접근 불가 | UNREACHABLE | WORLD-001~010, Build Settings |
| Crafting | CraftingService/Recipe/Workbench 존재 | 3개 작업대, 7개 카드, 5개 실제 가공, 118G→203G | WorldSandbox에서 검증, 일반 빌드 진입 불가 | UNREACHABLE | BETA-003 |
| Day Cycle | 6/25까지 Day/Night·개점 게이트 연결 | 플레이어 날짜 전환, 7일 진행, Day 6~7 목표 | Day 1/다음 날은 연결, 7일 최종은 검증 부채 | PARTIAL | BETA-008, Visual Audit M |
| Audit/Settlement | 기본 감사/Tier | 일일 구매/거절·매출·마을 방향·완주 요약 | Prototype 감사/결산 연결, UI 가독성 부족 | CONNECTED | Visual Audit L/M |
| Automation | Auto Builder/Dev Console | validator registry, bounded ticket, 회귀·캡처 안전화 | 다수 D3D11 PASS와 근거 로그 존재 | VALIDATED | WORLD/BETA logs |
| Validation | 기능별 일부 확인 | Golden/M70/BETA 회귀와 8/25 시각 감사 | 현재 blocker·UNREACHABLE·16:9 미확인을 분리 기록 | PARTIAL | Visual Baseline, BETA-010 |

## 현재 Connected

- Build Settings 단일 진입점 `Prototype_FirstDay`: 타이틀, 새 게임, 온보딩, 이동, Inventory/Hotbar, 진열, 가격 설정, NPC 구매/거절, Money, 휴대폰 감사, Day 1 결산, F5/F9 저장/불러오기.
- `WorldSandbox`: 이동, 네 활동, B05~B07 제작, B01 진열/개점/고객 구매, Feed/채용, 절차 월드 편집·배치·내비게이션·저장.

## 현재 Validated

- Golden `FinalDemoRoute`, Core Slice, Day/Night, Customer Presentation/Arrival 계열의 기존 PASS 근거.
- M70 WORLD-001~010 통합 및 회귀.
- BETA-001~006: 생성 월드 온보딩, 네 낮 활동, 제작, merchandising, 고객 전략, 휴대폰 채용/Feed.
- 8월 25일 D3D11 감사: Prototype 타이틀→온보딩→판매/감사/결산, 동일 세션 저장/복원.

## Partial

- Prototype의 상점/가격/고객/결산은 동작하지만 카메라 가림, HUD 겹침, 작은 패널, 큰 말풍선으로 처음 보는 사용자의 이해가 불안정하다.
- BETA-007 주민·마을 반응은 구현됐지만 hire→sale→Day2→dialogue stage가 미검증이다.
- BETA-008은 Day 1~5를 통과했지만 Day 6~7/Week 1 완주가 미검증이다.
- BETA-009는 v12 저장 구현과 정적 검증을 통과했지만 restart/continue/repeated-load 전체가 끝나지 않았다.

## Blocked

- BETA-010: 저장 quaternion은 137°인데 재시작 후 runtime facing이 identity로 복원되어 `facingError=137`. 같은 원인 두 번 실패 후 규칙에 따라 중단.
- 이 blocker 때문에 M85 전체 재시작→계속 플레이→동일 저장 반복 로드와 전체 Golden/M70 회귀를 완료하지 못했다.

## Unreachable

- 최신 생성 월드 M85 전체 루프는 `WorldSandbox` 전용이다.
- Build Settings 활성 scene은 `Assets/Scenes/Prototype_FirstDay.unity` 하나뿐이다.
- `WorldSandbox`/`MainGame`은 일반 타이틀에서 진입할 수 없다.
- BETA-003 제작, BETA-004~006 최신 UX는 검증됐지만 현재 제품 진입점에서는 모두 보여 줄 수 없다.

## Planned

- 하나의 최종 제품 진입 경로와 MainGame 통합은 별도 사람 승인 필요.
- 졸업전시용 3~5분 Demo의 전용 흐름·종료점·Reset은 아직 구현 계획 단계다.
- 1920×1080 standalone safe area, 카메라 가림 방지, shop/activity 식별, 최종 빌드 검증이 필요하다.

## Demo Gap 분석

| 시스템 | 현재 상태 | Demo 필요도 | Demo Gap | 이번 학기 우선순위 |
| --- | --- | --- | --- | --- |
| 저장/복구 | 재시작 facing blocker | CRITICAL | 종료→재실행→계속 플레이 불가 | P0 |
| 제품 진입 경로 | Prototype와 M85가 분리 | CRITICAL | 핵심 최신 루프가 관람객에게 도달 불가 | P0 |
| 3~5분 핵심 사이클 | 시스템은 있으나 장기 시간/경로 | CRITICAL | 상품 선택→가격→NPC 반응→결과를 짧게 보장하지 못함 | P1 |
| 종료점 | 결산/Tier/Audit 후보 존재 | CRITICAL | Demo가 언제 끝나는지 고정되지 않음 | P1 |
| Demo Reset | 전용 기능 없음 | CRITICAL | 다음 관람객이 같은 상태에서 즉시 시작 불가 | P1 |
| 16:9 UI/카메라 | 현재 476×1297 증거만 존재 | IMPORTANT | HUD overlap, 차양 가림, 패널/말풍선 스케일 | P2 |
| 상점/활동 가독성 | 기능 연결, 시각 식별 약함 | IMPORTANT | 판매대·개점·채집 지점 발견이 어려움 | P2 |
| 행동 결과 피드백 | 구매/Feed/Audit 데이터 존재 | IMPORTANT | 재고·돈·관계/마을 변화가 한 장면에 모이지 않음 | P2 |
| Audio/Visual Polish | 일부 에셋·UI 존재 | OPTIONAL | 핵심 흐름 안정화 전 전면 폴리싱은 우선순위 낮음 | P3 |
| 대형 월드 확장/추가 앱 | 장기 Full Game 대상 | OUT OF DEMO | 3~5분 핵심 전달에 직접 불필요 | 보류 |

## Demo Critical

1. BETA-010 blocker 제거와 restart/repeated-load 회귀 통과.
2. 타이틀에서 도달 가능한 단일 최종 Demo 경로.
3. 30초 역할 이해 → 상품/가격 판단 → NPC 반응 → 매출/관계/마을 변화 → 결산 종료의 3~5분 사이클.
4. 고정 시작 상태와 빠른 Demo Reset.

## Demo Important

- 1920×1080 UI safe area와 카메라 가림 방지.
- 상점·판매대·개점 간판·활동 지점의 즉시 식별.
- 구매/거절 이유와 돈·재고·성장 결과를 한눈에 보이는 피드백.
- standalone build와 실패 대비 녹화본.

## Demo Optional

- 추가 장식 밀도, 미세 재질 조정, 고급 merchandising 테마, 부가 앱/통계.

## Out of Demo

- 영구 섬 크기 확정, 대규모 월드 확장, 주민 모델 전면 교체, 과도한 가구/관광객 세분화, 신규 장기 시스템.

## Screenshot 후보

- `Docs/VisualAudit/2026-08-25-29fb98f/10_market_hub_objective.png`: 현재 상점 허브와 HUD.
- `11_shop_price.png`: 실제 BreadLoaf 진열·가격 UI.
- `13_npc_feedback.png`: 실제 Tailor 구매 반응.
- `14_audit_app.png`: 실제 30G 누적 매출과 Tier/감사 방향.
- `15_day1_summary.png`: Day 1 결산과 종료점 후보.
- 모든 이미지는 실제 Unity 캡처지만 476×1297이라는 한계를 함께 표기한다.

## Git Evidence

- 집계 창 내 도달 커밋: 60개. 성과 수치가 아니라 변경 근거로만 사용한다.
- `09af7bc`: 6월 26일 안정된 Day/Night loop와 검증 harness 체크포인트. 단, 6월 21~25 작업을 포함하므로 그 부분은 Baseline으로 분리.
- `6d76a56`, `d461a96`, `003507d`, `b7cb20f`: 마을 변화 저장, 실내 상점, 실내 고객, Tier 1 상점 연결.
- `66b17ef`, `f07d3da`, `5016576`, `18def06`: 날짜 전환, 낚시, 낚시 판매 왕복, 광질.
- `776fd3a`~`e0b5678`: WORLD cell grid→terrain→terraform→surface/water→building→generator→가구→저장→navigation→기존 게임플레이→M70 통합.
- `9bf71d6`~`1539923`: BETA 온보딩→낮 활동→제작→merchandising→고객 전략→휴대폰 채용/Feed.
- `4bf89f4`, `46dbea9`, `6168d05`, `29fb98f`: 주민 반응·7일 진행·저장 통합 checkpoint와 player facing blocker 기록.
- 기준선 대비 `HEAD`: 439 files changed, 99,121 insertions, 712 deletions. 이 수치는 작업 규모 근거이며 발표 성과로 사용하지 않는다.

## 최종 Demo 방향

이번 학기의 최종 산출물은 Full Game의 모든 기능이 아니라 다음 경험을 안정적으로 전달하는 `GRADUATION_EXHIBITION_DEMO`다.

> 상품을 선택하고 가격을 결정한 내 운영 판단이 NPC의 소비와 매출·관계·마을 변화로 돌아오는 3~5분 Vertical Slice.

성공 기준은 관람객이 30초 안에 역할을 이해하고, 3분 안에 운영 판단을 하며, 5분 안에 결과와 종료점을 보고, 다음 관람객이 즉시 Reset된 상태에서 시작할 수 있는가이다.
