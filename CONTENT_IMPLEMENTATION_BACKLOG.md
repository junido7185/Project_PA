# Project P.A. — Authored Content Implementation Backlog

## 승인·현재 상태

- 사용자 승인: 2026-09-06 「CONTENT-000 승인 및 장기 콘텐츠 개발 재개」. Canon은 **PROVISIONAL CANON v1**이며 CONTENT-000B 자기검증 PASS 후 CONTENT-001~010을 하나씩 구현·검증·로컬 커밋한다. 중간 사람 승인 요청은 없다.
- 현재 단일 티켓: **CONTENT-002 / ACTIVE**. CONTENT-000B는 `e6f3716`, CONTENT-001은 `c49eb01`로 완료했다. 장기 목표는 새 게임~Day30의 실제 플레이 가능한 졸업작품이며 문서 작성만으로 완료하지 않는다.
- 설계 권위: [CONTENT_CANON_BIBLE](CONTENT_CANON_BIBLE.md), [CONTENT_CAMPAIGN_DAY1_30](CONTENT_CAMPAIGN_DAY1_30.md). 최소 errata는 Campaign §17에 이유·전후를 기록한다.
- 기준: `milestone/gameplay-beta-85@29fb98f400b55dcc7f7e5c580c9b476041d378f4`. 기존 사용자 변경·미추적 자료를 보존한다. 지침·로그의 기존 dirty 내용을 함께 커밋하지 않으며 추가한 작업 기록만 선택 반영한다.
- 선승인 상태 기록: `Automation/LoopEngineering/State/loop-state.json.contentContinuationApproval`.
- 기존 BETA-010의 137° facing restore 실패는 미해결 기술 이력이다. Canon 승인 대기는 해소됐지만 그 검증을 PASS로 바꾸거나 같은 실패를 세 번째 실행하지 않는다.

## 실행 순서와 공통 완료 조건

| 순서 | 티켓 | 상태 | 완료 커밋 | 다음 |
|---:|---|---|---|---|
| 0 | CONTENT-000B | COMPLETE_DESIGN_PASS | e6f3716 | CONTENT-001 |
| 1 | CONTENT-001 | COMPLETE_RUNTIME_PASS | c49eb01 | CONTENT-002 |
| 2 | CONTENT-002 | ACTIVE | — | CONTENT-003 |
| 3 | CONTENT-003 | READY_AFTER_PREDECESSOR | — | CONTENT-004 |
| 4 | CONTENT-004 | READY_AFTER_PREDECESSOR | — | CONTENT-005 |
| 5 | CONTENT-005 | READY_AFTER_PREDECESSOR | — | CONTENT-006 |
| 6 | CONTENT-006 | READY_AFTER_PREDECESSOR | — | CONTENT-007 |
| 7 | CONTENT-007 | READY_AFTER_PREDECESSOR | — | CONTENT-008 |
| 8 | CONTENT-008 | READY_AFTER_PREDECESSOR | — | CONTENT-009 |
| 9 | CONTENT-009 | READY_AFTER_PREDECESSOR | — | CONTENT-010 |
| 10 | CONTENT-010 | READY_AFTER_PREDECESSOR | — | 전체 완료 검증 |

각 구현 티켓은 관련 설계 읽기→현재 코드 조사→변경 파일 보고→데이터/최소 연결→Runtime compile→Editor compile→D3D11 validator→핵심 회귀→diff 검사→기록→local commit→다음 티켓 활성화 순서다. 컴파일에는 새 .cs가 실제 입력에 포함되었는지 확인한다. 검증기가 반환되었다는 사실 대신 성공 marker와 필수 assertion, Console/crash 결과를 확인한다. 신규 validator는 기존 레지스트리에 등록하고 production API/상태를 관찰한다. 무료 지급·강제 구매·강제 날짜/승급은 단위 검증 fixture일 수 있어도 최종 캠페인 완주 증거는 아니다.

설계 티켓 CONTENT-000B는 코드 변경이 없으므로 문서 구조·정합·경제/저장 의존성·담당 티켓 자기검증으로 판정한다. Unity 미실행을 명시하며 설계 PASS를 runtime PASS로 쓰지 않는다.

## CONTENT-000B — Day 1–30 Campaign and Quest Architecture

- 범위: 승인 상태 기록, Campaign20개 섹션, Backlog, 정합 errata·최소 저장 계약·일반 진입점 설계, 문서 검증.
- 근거: 첫날과 World 시작의 서로 다른 starter, DayPlan 수동 공급과 실제 producer bag, Hiring의 새 객체 생성, specialist 요청 소모/가공 입력 차이, 최근 판매/마을 exact context, 실제 Tier/Audit 조건.
- 승인 선택 구체화: 보리 생활자·기존8역할 이름 유지; 첫 달 의류/가구 필수 제외; Carrot 요리 명칭 최소 교정; additive 저장; 경제 성장 지표와 저작된 캠페인 완료 분리.
- PASS 요구: 30일 단일 분류, 12~16 anchors, 첫 주 anchor별 17필드, 요청된 기능 25개 노출, 주민별 소개1/관계3/요청2, 실제 행동/실패/저장/티켓 연결, 자유 운영, Day30 완료 근거. 점수·날짜·대사만으로 거래/고용/위임을 완료 처리하는 설계 없음.
- 파일: `CONTENT_CANON_BIBLE.md`, `CONTENT_CAMPAIGN_DAY1_30.md`, 이 파일, state/active/progress 및 필수 작업 기록. 게임 코드/자산/설정 변경0.

## CONTENT-001 — Opening & Bori

- 결과: World 새 생활에서 보리가 실제 인사 대상이며, 플레이어가 오늘 가게를 여는 이유를 알고 첫 재고를 찾는다. A01.
- 재사용: `WorldAlphaPlayableController`, `WorldGameplayAdapterService`, `NpcDialogue`, `DialogueData`, `NpcProfile`, `NpcController`, `DialogueUI`, `FriendshipService`, `SaveManager/SaveData`.
- 작업: 실제 C-02/C-03 참조와 대체 가능한 기존 시각 자산을 확인해 보리 전용 생활자 연결. `bori` 유지, 기존 8직업을 새 이름으로 덮어쓰기 금지. 인사 상대 identity를 확인하고 일반 DialogueUI 열림으로 A01 완료하지 않음. 첫 HUD는 이동·이웃·재고부터, PlayerName 자유도 유지. 시작 진입 여러 번 호출/로드 시 보리와 starter 중복 방지.
- 저장: Campaign §16의 작은 additive 시작/인사 상태. 기존 playerName/firstDayPrototypeStage/WorldAlpha 및 friendship를 재사용하고 legacy save에 campaign 시작을 강제하지 않는다.
- 예상 경로: 위 기존 파일 중 필요한 최소 수정, `Assets/Resources/NPCs/Profile_Bori.asset`, `Assets/Resources/Dialogues/Dialogue_Bori.asset`, 필요한 작은 Opening 표현 컴포넌트·Editor validator. 새 Framework 없음. 실제 파일 목록은 조사 후 먼저 보고.
- 검증: 시작→보리 직접 상호작용→+2 한 번→재고 확보→다른 NPC 대화는 보리 완료 아님→save/load 유지→단일 보리. D3D11 신규 `PA_ContentOpeningValidator`; 기존 `PA_FinalDemoRouteValidator`와 World/BETA 첫날 핵심 회귀. Golden 기존 경로는 자동 캐논 치환하지 않음.
- 다음 조건: A01 실제 동작·compile·회귀·diff PASS 후 commit, CONTENT-002.
- 2026-09-07 검증 PASS: `Logs/Content/CONTENT001/OpeningValidation_Release.log`(실제 Space 인사·+2 중복 방지·실제 채집·순서 교환·v13/v12·반복 load·카메라/문구 복원), `RuntimeCompile_Release.log`/`EditorCompile_Release.log`(오류0), `GoldenRegression.log`, `WorldOnboardingRegression.log`, `WorldDaytimeRegression.log`, `SaveRoundTripRegression.log`. 모두 같은 로그 폴더.
- 외형 감사: Golden 보리는 Profile_Lumberjack 참조를 그대로 보존했다. World 보리는 C-01 기반 별도 외형과 프로필·대사, 후보 없음. CE-005 참고. `Runtime/Opening.png` 1920×1080에서 새 안내 글자 잘림/겹침 없음. 전체 지형·플레이어 외형·기존 휴대폰 패널과 최종 미술 품질은 후속 통합/사람 검토 대상.
- 저장: envelope v13 additive campaign 4필드, embedded world v11 유지. 기존 v12 recovery 의미 보존. 이번 same-session load PASS가 기존 BETA-010 restart/facing 137° 부채를 해소하지 않는다. 첫 밤 소비는 다음 티켓이므로 현재 보리는 인사 자리에서 기다린다.

## CONTENT-002 — First Shop Night

- 결과: A02 진열·가격·자기 개점·실제 보리/고객 판단·판매/미판매 정산. 보리가 사지 않은 경우도 정상 캠페인 진행.
- 재사용: ShopSlot/ShopPriceUI/ShopOpenSign/DayNightShopLoop, NpcController/PurchaseEvaluator, SalesLog/Feed, WorldAlpha/기존 정산.
- 작업: 보리를 소비자로 기존 경로에 연결. NPC 이름과 실제 buyer/친밀도 key 일치. A02 판단 관찰/첫판매를 분리; 기본가/추천가·스택·과가격 이해. 초기 개발키/숫자 나열을 필요한 조작 안내로 정리. 제로매출 정산도 다음날 이어짐.
- 검증: 가방→진열→가격→개점→실제 평가·판매·금전/재고 원자성, 보류 때 상태 보존, +5 정확히1회, pending village 기록. save/load와 첫날 회귀. 구매 확률 강제0/1 또는 특수 고객 치트 없음.
- 예상 경로: WorldAlpha/adapter의 고객 진입, NpcDialogue·기존 피드/정산 표현, Dialogue_Bori, CONTENT-002 validator/registry. core 경제식 수정0.

## CONTENT-003 — Producer Economy

- 결과: A03 고용 전 미라가 실제 생산한 Wheat를 플레이어가 명시적으로 유료 매입한다. DayPlan 생성 공급과 개인 생산을 혼동하지 않는다.
- 재사용: ProducerNpcController의 private bag을 다루는 내부 원자 거래, ProductionData, Inventory.CanAddInstance/AddInstance, EconomyService.TrySpend/TryModifyMoney, NpcDialogue.
- 작업: producer에 읽기 전용 재고 snapshot과 명시적 수량 거래 진입을 최소 추가. 기존 자동 납품도 같은 내부 거래를 사용하고 Golden 동작을 보존. core 주민은 고용 전 생활자로 존재; 해당 주민 구매 UI는 위치/실제 bag을 검증. snapshot/restore를 명시적 API로 추가하여 미매입 재고의 반복로드 증식 방지.
- 저장: candidate stable key·고용 전 존재·producer bag/타이머·거래 evidence를 기존 SaveManager에 additive 연결. 고용 전/후 동일 bag.
- 검증: 실제 Work 산출→일부 매입/보류→잔액/양쪽 재고·metadata, 0G/가방full/동시슬롯 변동 rollback, save/restart/미고용 복원. DayPlan 버튼이 미라 생산 완료를 주지 않음. free gathering recovery 유지.
- 예상 경로: ProducerNpcController, NpcDialogue/기존 상호작용 표현, World adapter 역할 스폰 연결, SaveData/SaveManager, 해당 validator. 새 Economy/Trading Framework 없음.

## CONTENT-004 — First Hiring

- 결과: A05 직접 해 본 반복 작업을 같은 주민에게 비용을 지불해 맡긴다.
- 재사용: HiringService의 CanHire/TryHire/RegisterHired/OnHired/RestoreHiredNpc, candidates, HiringUI, 기존 작업 앵커·schedule.
- 작업: 이미 존재하는 core instance를 검증한 뒤 고용 record에 연결하는 최소 경로. legacy 후보는 기존 스폰 경로 유지. stable key·friendship·producer bag·위치·소비 profile을 유지하며 중복 스폰/중복 비용 방지. 고용 전 작업지 매입에서 고용 후 상점까지 정기 운반으로 바뀌도록 기존 dropOffPoint/schedule을 연결한다. 실제 성공한 고용만 A05. UI는 고용비와 이후 매입비·잠긴 전문가 작업을 구분.
- 검증: 미라 매입 전/후 동일 instance 또는 명시된 안전 복원 identity, 비용300, +고용1, duplicate attempt 비용0, bag/친밀도 유지. 두 후보/실패스폰/로드 round-trip. 이름 기준 삭제 없음.
- 예상 경로: HiringService, HiringUI/NpcCandidateData 표시 연결, World adapter, Save restore 연결, CONTENT-004 validator. enum·candidate asset 이름/GUID 변경0.

## CONTENT-005 — Week One Authored Experience

- 결과: A04/A06/A07 및 Day1~7 전체에서 직접 준비→소비 이해→유료 매입→고용→실제 위임→회고가 이어짐.
- 재사용: LongPlayProgressionController의 목표/주간결산, WorldAlpha HUD, SalesDecision, producer 실제 Work와 purchase snapshot, DialogueData.
- 작업: A04 두 주민 실제 판단 비교; A06 고용 이후 생산분 확인; A07 사실 기반 회고. Day4/회복 경로, 놓친 시간/거절/자금부족 이월. 고용전 bag의 오래된 물량을 위임 생산으로 기록하지 않음. 현재1,700G 지표와 핵심 사건의 완료를 분리해 표시.
- 검증: 일반 조작의 D1~7 경로, 미라 외 첫생산자 선택, 첫손님보류/Day5고용지연/모달중시간/중간로드. 프로덕션 HUD에서 단계별 필요한 정보만 보임. 기존 BETA008 미검증 D6/7은 PASS로 승계하지 않고 새 증거 확보.
- 예상 경로: LongPlay, WorldAlpha, NpcDialogue, 관련 role dialogue, 주간 validator/registry.

## CONTENT-006 — Core Cast Integration

- 결과: 8직업 이름·얼굴·성향·소개와 A08 전문가 작업이 월드/Phone/Feed/소비에서 일치한다.
- 재사용: 8Profile/Candidate/Resident/Dialogue, HiringService matching recipes, SpecialistNpcController/CraftingService, existing schedule/season.
- 작업: 미라/로건/아를로/타라/노아/펠릭스/준/루카와 기존 수치 유지. 기본 greeting/Economy/구매/보류 대사 톤. 실제 카테고리 확률에 맞는 소개, Tailor Utility/낮은 가격저항·Chef SN0 유지. Carrot 음식 CE-003 참조 감사 후 표시 정합. 보리와 외형 중복 해결은 기존 시각 자산 범위, core 역할 변경 없음.
- A08: 요청한 재료의 소모와 자동 제작용 Inventory 입력을 분리. 실제 가공 성공/입력부족/시설·Tier·레시피 잠금을 명료화. 고용했다고 작업 가능한 것으로 표현하지 않음.
- 검증: 8명 identity 전수, 프로필 수치비교, schedule/controller/실제 SKU 연결, hired/unhired consumer 연속, 전문가 실제 craft와 플레이어 입력량, cooking label 구save fallback. fixed RNG로 공식 식을 확인하되 최종 플레이 소비를 강제하지 않음.
- 예상 경로: 기존 Resources/NPCs·Candidates·Dialogues의 데이터, label resolver 최소 코드, specialist 관찰 연결, cast validator. 데이터 입력을 캐논 재설계로 확장하지 않음.

## CONTENT-007 — Relationship & Personal Events

- 결과: Campaign §10의 관계24개+보리3개, §14의 개인요청16개가 실제 포인트·행동·주민의 서로 다른 필요에 연결된다.
- 재사용: NpcDialogue/DialogueData/DialogueService, FriendshipService, 기존 specialist request와 일일 활동, campaign 고정 ID 완료 기록.
- 작업: 10/25/50/80/120에서 현재 포인트와 선행행동을 읽는 작은 조건 분기. 각 인물의 대표 beat를 기존 말풍선/DialogueUI로 표시. 생활자/생산자에게 specialist 납품을 위장하지 않음. 무작위 기본풀에 하지 않은 사건 회상 금지. 한날 자동 제안1, 다시읽기 보상0.
- 저장: 사건 완료/보상 여부는 실제 저장된 고정 ID. 일일 납품과 개인 사건을 구분. 기존 fundamental recipes를 HiddenBlueprint로 다시 잠그지 않음.
- 검증: 각 인물 최소점수 미만/경계/과거증거없음/실제완료/동일날재시도/다음날/재시작, 자원소모와 +2/+4/+5가 한 번만 발생. 120점 즉시 획득해도 전story 자동완료 안 됨.
- 예상 경로: NpcDialogue, 작은 기존 진행 조건 선택, 8Dialogue·Bori, Save campaign DTO, relationship validator.

## CONTENT-008 — Week Two Economy Branch

- 결과: A09~A10에서 생활 공급/가공 집중의 관심을 선택하고 실제 이후 판매/마을반응으로 결과를 확인한다.
- 재사용: LongPlay, Phone/Feed, Inventory/Storage/Crafting, category insight, VillageChangeSignal/VillageCultureVisual.
- 작업: 관심 선택은 추천 objective/대사만 바꾸고 다른 상품/인물을 잠그지 않음. 현재 선택과 실제 지표를 구분. D11free/D13recovery, 미판매·선택변경·기존Tier 미도달 처리. Day14회고에 실제 관련 주민/상품/날짜.
- 검증: 두 방향 각각 실제 운영, 중간전환, 선택만하고판매없음, Raw/Processed결과, save/restart. display choice가 돈/티어/비주얼을 직접 변경하지 않음.
- 예상 경로: LongPlay, Phone/Feed 표현, campaign interest save, branch validator. 새 branch/quest engine 없음.

## CONTENT-009 — Month-One Campaign

- 결과: A11~A14 및 D15~30의 지원/자유/회복을 통해 producer+specialist 실제 위임과 개인관계 개입이 첫달 기록에 남음.
- 재사용: LongPlay 30일 목표/결산, 기존경제/관계/고용/작업/마을·SaveManager.
- 작업: 첫달완료 증거를 Campaign §20으로 연결. 기존매출31,000·Tier·Audit는 실제성장지표로 보존. D30열람/완료 분리, D31+계속. 모든주민120/의류가구/최종감사를 강제하지 않음. 이전저장·중간저장·완료저장 migration과 producer bag, 두인물 동일identity·모달복원을 검증.
- 검증: 서로 다른 첫고용/운영방향의 월간 경로, 고용/판매 지연복구, 실제가공·관계·마을인과, restart/repeatedload. 137° facing 결함의 기존두실패를 우회하거나완료조건에서제외하지않음. 해당실패의새실행이필요한지와사용자HARD GATE를 코드근거로 판정.
- 예상 경로: LongPlay, campaign observation/save records, 필요한 기존 pose restore의 최소 수정(근거 확보 때만), month validator.

## CONTENT-010 — Authored Experience Integration

- 결과: Windows 일반 새 게임에서 Day1~30, 저장·게임종료·재실행·계속하기까지 하나의 제품 경로가 완주 가능함.
- 진입: 기존 타이틀에 새생활/계속하기 연결. build script의 명시적 `BuildPlayerOptions.scenes`로 기존 두씬을 포함하고 ProjectSettings/Packages/Golden scene serialization을 변경하지 않음. MainGame을 별도 통합하지 않음. 기술 씬이름/개발키는 플레이어 안내에 넣지 않음.
- 통합: 중복HUD·입력·시계정지 소유권·카메라·이동·인물중복/경로·소비·저장·관계·회고·Day31 지속을 실제일반진입으로 검증.
- Runtime/Editor compile→콘텐츠 D3D11 통합→Golden 핵심회귀→Windows빌드 실행·재시작→diff/Console/crash. 1920×1080은 실제 게임 화면에서 이름·가격·목표·거절·회고의 가독성을 확인. 위험한 `Camera.Render()` 캡처 재도입 금지.
- 금지: 테스트용물량/돈/고용/강제승급으로30일 PASS, 실패assertion 삭제, 손님 구매 강제, 저장검증 생략, 한날summary를30일증거로 확대.
- 최종보고: 실제플레이증거·완료ticket/commit·남은비차단한계. 장기Goal 완료는 요청경험 전체 검증 뒤에만.

## 의존성·중복 개발 방지

| 의존성 | 발견된 현재 경계 | 소유 티켓 | 다음 사용처 |
|---|---|---|---|
| 보리정체성/첫인사 | 전용profile없음, 임의첫NPC재명명 | CONTENT-001 | 002/005/007 |
| 실제구매/거절증거 | SalesLog/decision존재, 개인표시분리 | CONTENT-002 | 005/007/008/009 |
| 생산자실제재고 거래/저장 | 자동전체매입, bag private·저장없음 | CONTENT-003 | 004/005/007/009 |
| 같은인물고용 | TryHire가새객체스폰 | CONTENT-004 | 005/006/009 |
| 위임확인 | 고용과실제output별개 | CONTENT-005/006 | 009 |
| 관계완료기록 | 포인트/일일요청만존재 | CONTENT-001에서작은DTO,007에서내용확장 | 009 |
| 직업별콘텐츠 | 기존8역할수치와5개이름분리 | CONTENT-006 | 007 |
| 판매후방향 | category통계와exactpending별개 | CONTENT-008 | 009 |
| 일반제품진입 | WorldSandbox미노출 | CONTENT-010 | 최종빌드 |
| 기존137°복원실패 | 두차례재현,미해결 | 009/010에서필수추적 | 최종완료금지조건 |

새 Quest/Dialogue/Relationship/Cutscene/Economy/Hiring Framework는 필요하지 않다. 기존 진행 컨트롤러에 고정 캠페인 기록과 조건 선택을 추가하고, 각 생산/고용/거래 API를 최소 확장한다. 이름표만 바꾸기, 생성한 원료를 NPC 생산으로 가장하기, 회고만 추가해 실제 위임을 생략하기는 이 백로그의 완료로 인정하지 않는다.

## 검증 증거 기록

| 티켓 | 정적/Runtime compile | Editor compile | D3D11/회귀 | diff/commit |
|---|---|---|---|---|
| CONTENT-000B | 문서검증 PASS, `Logs/Content/CONTENT000B/ArchitectureValidation.json`; 코드컴파일 N/A | N/A | N/A — 설계티켓 | diff/commit 순차 진행 |
| CONTENT-001~010 | NOT_RUN | NOT_RUN | NOT_RUN | 미착수 |

## 중단·복원 계약

사용자 §16의 HARD HUMAN GATE(핵심 캐논 변경/양립 불가, 파괴적 save 변경, ProjectSettings/Packages, 대규모 권위 재작성, native crash, 동일 blocking failure가 최소 수정 후 2회 반복, 예상 밖 사용자 작업 충돌)에서만 중단한다. 기존에 알려진 dirty 파일 자체나 사소한 대사 선택은 blocker가 아니다. 샌드박스가 필수 명령을 차단하면 해당 구체 명령의 실행 권한을 요청하며, 이를 콘텐츠 다음 티켓 승인과 혼동하지 않는다.

컨텍스트 한계에서는 `PAUSED_BY_CONTEXT_LIMIT`로 정확한 active ticket/마지막 완료 commit/변경 파일/남은 검증/재개 명령을 기록한다. 미완료 ticket은 commit하지 않는다. 단순히 문서가 길어졌다는 이유로 완료를 선언하거나 자동 진행 승인을 다시 묻지 않는다. push/rebase/reset/clean/history rewrite는 금지다.
