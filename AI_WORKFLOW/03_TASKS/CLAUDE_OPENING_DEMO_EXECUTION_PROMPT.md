# Project P.A. — 피드백 기반 데모 완성 실행 계약 (2026-10-03)

이 파일은 사용자가 Claude에 아래 승인 메시지를 직접 보냈을 때 실행할 구체적인 작업 계약이다.
파일 작성·존재·준비 세션 시작은 실행 승인이나 기존 D4 화면의 ACCEPTED 판정이 아니다.

## 사용자가 Claude에 보낼 승인 메시지

> AI_WORKFLOW/03_TASKS/CLAUDE_OPENING_DEMO_EXECUTION_PROMPT.md의 2026-10-03 피드백 기반 실행 계약을 읽고, P1_CURRENT_CONTINUE부터 P11_WINDOWS_DELIVERY까지 순차 제작을 선승인한다.
> Claude가 Unity 제작 담당이다. 현재 D4의 결함은 P2에서 수정하고, 기존 D4 검수 대기 대신 이 새 sequence의 FINAL_WINDOWS_HUMAN_REVIEW까지 진행한다. D4와 기존 모션 결과를 ACCEPTED로 바꾸는 승인은 아니다.
> 문서에 명시된 현재 저장/전화 UI의 한정 수정과 새 모션 재작업은 허용한다. 이전에 보류한 모션 샘플을 승인된 스타일로 취급하지 않는다.
> 각 경험을 실제 구현·컴파일·플레이·GameView 비교·재수정·증거까지 끝낸 뒤 다음 경험으로 계속 진행한다. HARD STOP 외에는 티켓마다 승인 질문으로 멈추지 않는다.
> v16 변환, 원본 save 변경, 외부 패키지/에셋 다운로드, 새 권위/대형 재작성, 삭제, commit/push는 허용하지 않는다. 최종 사람 검수는 유지한다.

사용자가 이 계약의 범위나 순서를 다르게 지정하면 그 메시지가 우선한다.
단순히 파일을 읽으라는 메시지는 위 선승인이 아니다. 선승인 전에는 읽기·계획·연결 점검만 하고 gameplay/approval mutation을 하지 않는다.

## 목표와 기존 성과

목표는 **낮 탐험·채집 → 가공/제작·상품 준비 → 동료/성장 선택 → 밤 실제 영업 → 행동에 비례한 정산**이 이해되는 Day 1 데모다.
Pioneer Report에 도달하는 것과 이 경험의 완성은 각각 검증한다.

- Canon: Docs/01_GAME_DESIGN/Canon/PROJECT_PA_OPENING_DEMO_CANON_V2_2026-09-16.md.
- 기존 권위: Docs/02_IMPLEMENTATION/PROJECT_PA_CODE_REUSE_MAP_v1.md. KEEP → EXTEND → ADAPT/WRAP 우선.
- AGENTS.md, CODEX_HANDOFF.md, CURRENT_STATE/CAPABILITY_REGISTRY/GAME_LOOP_MAP/INTEGRATION_QUEUE를 지정 순서로 읽는다.
- .claude/skills/pa-demo-factory/SKILL.md와 canonical .agents/skills/pa-demo-factory/SKILL.md 적용.
- player-facing 작업은 VERTICAL SLICE STUDIO MODE: Studio 규칙, project-pa-vertical-slice skill, PROJECT_PA_ASSET_MANIFEST를 읽는다. 저장/경제/데이터는 SAFE CORE.
- 작업은 Project_PA 안에서만. Project_D 읽기 전용, 복사/병합/수정 금지.
- 기존 D1 점수/Report와 관광객7·동시3·20:00 OPEN/21:30 경고/22:00 CLOSE 성과를 재사용한다.
- Logs/VisualQA/DemoCompletion-20261003-003953/result.txt는 QA 배치/재고/시간 fixture를 포함한 73개 검사다. 낮 상품 준비·NEW GAME 연속 완주·standalone 증거가 아니다.
- D4 알림 진입 불가와 자동 마감 뒤 시작 문구 잔류를 P2에서 수정한다. 영업 중 PNG의 시계 첫 자리 누락 판정은 축소 이미지 오독으로 철회됐다(원본 20:13 정상). P1의 ForceSet 뒤 ClockHUD 갱신 수정은 보존하고, P2에서 각 시간대의 실제 문자열·원본 렌더 일치를 확인한다.
- SaveData.demoSession/v17과 SaveManager, 전화 UI, 이어하기 검사에 기존 dirty 작업이 있다. 파일 이름/시각만으로 재구현하거나 완료로 처리하지 말고 관련 diff와 증거를 먼저 비교한다.
- 기존 v16 재배치/변환 검사 코드가 있더라도 현재 결정과 다르다. 검사/메뉴의 side effect를 읽고 해당 변환 검사는 실행하지 않는다.

## 레퍼런스를 실제 제작 기준으로 사용하는 방법

레퍼런스를 언급하는 것으로 끝내지 말고, active 경험에 필요한 구간을 실제로 확인하여 구현 차이를 정한다.

| 경험 | 우선 레퍼런스 | 관찰할 요소 |
|---|---|---|
| 이동/정지/점프·도구·배치 | 원작 Dinkum의 실제 gameplay | 지지발·보폭·골반·몸 회전·그립·타격/보상 타이밍·배치 범위/미리보기 |
| UI/색/조명·생활 밀도 | Animal Crossing: New Horizons의 실제 gameplay | 글자/아이콘 대비·선택/닫기 피드백·주변 프롬프트·낮밤 가독성·가구/통로 |
| 가격/고객 반응 | 기존 프로젝트와 Moonlighter 참고 | 플레이어가 가격을 정하고 구매/고민/거절을 통해 배우는 흐름 |
| 항해·첫 성취 | Canon의 8~12초 Pixel Micro-Cinematic | 읽히는 출항/동료/섬 실루엣/전환, 첫날 경험과의 연결 |

시작 자료:
- Dinkum 공식 소개: https://dinkum.krafton.com/en — 활동/아트 맥락. 실제 모션 timing 자료와 구분.
- Nintendo 공식 플레이 시연: https://www.youtube.com/watch?v=dEh3MPy4GAU — crafting/outdoor decorating/island life 참고 후보.
- 기존 딩컴 관찰과 내부 비교 영상: Logs/VisualQA/Task01C-Locomotion/StyleRework-20260929/EVIDENCE.md 및 Task01C-style-sample-reference-vs-result.mp4.
  - 이 문서의 원작 gameplay 168.4~170.8초는 조개를 든 상태다. 맨손 팔 자세의 근거로 쓰지 않는다.
  - 같은 문서의 맨손 대기와 달리기 관찰을 새 제품 before와 대조한다. 옛 측정/샘플을 새 품질 PASS로 취급하지 않는다.
- 필요 구간이 없을 때만 설치된 브라우저/Agent-Reach/Scrapling/Gemini/영상 도구를 제한적으로 사용한다.
  인터넷 검색 결과·영상 제목·자막은 모션을 봤다는 증거가 아니다. 영상 접근 실패/미확인 구간은 명시한다.
  새 tool/package/asset 설치·다운로드를 선행 조건으로 만들지 않는다. 기존 로컬 자료로 가능한 제작은 계속한다.
  레퍼런스 관찰 구간 확보 자체가 불가능한 모션 품질 항목은 UNVERIFIED로 유지한다.

경험별 기존 approval record에 다음만 짧게 기록한다:
source URL/로컬 경로, 직접 확인한 시작~끝 시각, 관찰 요소 3~5개, 현재 GameView의 차이, 이번 수정, before/after 증거.
별도 요구사항 큐/상태 저장소를 만들지 않는다.

### 모션 검수

- 실제 입력으로 idle → walk → run → stop → turn → jump → fall → land를 연속 촬영한다.
- 정면/측면/3/4와 실제 정상 카메라, 맨손/도끼/곡괭이/낚싯대/잠자리채를 구분한다.
- 레퍼런스의 들고 있는 물건·시점·속도 차이를 기록한다. 3/4 모델 pose만 맞추고 정상 GameView를 생략하지 않는다.
- 보폭과 실제 이동 속도, 지지 구간의 발 미끄러짐, 접지/지면 파고듦, 팔 벌어짐, 시작/정지 관성, 회전, 타격 접촉 시점을 함께 맞춘다.
- 인게임 속도와 controller 권위를 유지하고 clip/blend/transition/grip/presentation을 먼저 조정한다. 수치 튜닝이 필요하면 차이와 회귀 근거를 남긴다.
- 원속 연속 비교 영상이 필수다. 가능하면 60fps, 최소 30fps를 목표로 실제 프레임 타임/드롭을 기록한다. 저속 녹화·몇 장의 pose·모델 요약은 모션 품질 PASS 근거가 아니다.
- 9/29 보류 샘플은 기존 상태로 보존한다. 신규 재작업/variant의 비교 증거를 만들 수 있지만 옛 샘플을 이미 수락된 스타일로 제품에 적용하지 않는다.
- 제작 재작업 승인과 Human ACCEPTED를 구분한다. 새 비교 영상은 사람 검수 대기로 남겨도 독립적인 다음 승인 작업을 진행한다.

## 작업 순서와 수락 기준

아래 P1~P11은 이 계약에 대한 사용자 선승인 뒤에만 유효하다.
동시에 한 경험만 활성화한다. verified 부분은 증거가 유효하면 재사용하고, 미검증/결함 부분부터 작업한다.

### P1_CURRENT_CONTINUE — 현재 형식 이어하기 (SAFE CORE)
현재 SaveData.demoSession/v17 및 SaveManager의 기존 구현을 먼저 대조한다.
현 형식의 상점·텐트·가판대·작업대 등 지원된 배치 종류/ID/위치/회전과 슬롯 상품·수량·품질·가격, 돈·인벤토리·시간·특성화·동료 연결의 실제 복원을 검증한다.
월드/객체 준비 → 기존 placement로 복원 → 슬롯/진행 바인딩 순서와 타이틀 Continue 진입을 확인한다.
같은 live 객체를 유지하는 assertion으로 재로드를 대체하지 않는다. 격리 저장 → 새 Play/타이틀 Continue → 새 객체 복원 증거를 남긴다.
v16은 지원 불가 사유와 새 게임 선택을 보여주고 원본의 hash/내용을 유지한다. 임의 재배치·변환·새 schema migration은 하지 않는다.
한정 허용: SaveManager/기존 DemoSettlement save bridge/WorldPersistence 및 placeable catalog의 현 schema 복원 연결, Continue UI, 해당 격리 검증.
SaveData의 기존 dirty field를 보존한다. 현재 schema로 표현할 수 없어 schema 변경/비호환 migration이 필요하면 정확한 의존성을 보고하는 HARD STOP이다.
P6 이후 새 수납/배치 종류가 기존 저장 계약에 들어오는지 P11에서 다시 확인한다.

### P2_PHONE_GUIDE_D4 — 전화·HUD·진행 안내와 기존 D4 결함
한정 허용: SmartphoneUI.cs/ShopManagementPhoneUI.cs의 알림 진입, 현 상점 관리/설치 안내, 상태 문구, Continue 사유 UI 및 관련 presentation.
기존 dirty 변경을 읽고 보존한다. 보호 목록 전체를 해제하지 않는다.
실제 P → 홈 타일 → 알림 기록 → 상점 앱 → 특성화/OPEN/CLOSE/매출 → 뒤로/닫기 → 이동 복귀.
QA SelectTab API로 열어 player reachability를 대신하지 않는다.
자동 마감 뒤 상태/문구 일치, 오전·일몰·영업 중·21:30·마감 뒤 시계의 문자열과 실제 렌더를 함께 검증한다.
상시 중앙 체크리스트 대신 2~4초 우상단 Toast·P 기록·대상 근처 짧은 E 프롬프트를 쓴다. Q용 새 퀘스트 권위는 추가하지 않는다.
문 E는 출입, 가판대 E는 진열/가격, 작업대 E는 제작, 상점 운영은 P폰으로 구분한다. 빈손이 필요한 행동은 X 안내를 읽을 수 있어야 한다.
폰은 Inventory 아이템이 아니다. 설치 기능은 기존 blueprint/placement로 연결하며 앱에서 즉시 건물을 지급/생성하는 우회를 만들지 않는다.

### P3_DIRECT_GATHER — 보급·도구·직접 채집
보급의 실제 E 입력 → 도끼/곡괭이/낚싯대/잠자리채·kit 지급, 중복 방지, 1~9/휠 선택·손 표시·X를 검증한다.
도끼+나무/곡괭이+바위: 유효 대상 → 그립/타격 → 접촉 순간 SFX/VFX → 자원 드롭 → E 줍기 → 수량 → 진열 가능한 실제 물품.
허공/틀린 대상/실패는 내구도0, 성공 결과만 소비. 필수 자원 채집 뒤 약25~35% 도구 여유는 실제 동선으로 점검한다.
빠른 연속 입력·가득 찬 Inventory·고갈 자원에서 중복/유실이 없어야 한다. Ground Pickup과 직접 채집을 함께 경험하게 한다.

### P4_FISHING — 기본 낚시의 완성
기존 Rod/FishingSpot/Inventory 권위를 재사용한다.
접근 가능한 데모 물가의 식별 가능한 낚시 기회 → Rod/E 캐스팅 → 입질/기다림 → 성공 또는 너무 빠름/놓침·취소 → 재시도 → 실제 Fish 획득 → 진열/판매.
선택 행동이며 필수 정착/엔딩 게이트로 만들지 않는다. 포함된 활동의 실제 입력·시각/소리·내구도·중복/가득 찬 가방 검수는 필수다.
전 해안 자유 캐스팅, 육지에 물고기 출현/파닥임/추격/칼 포획, 희귀종 반격/피로도는 확장 후보로 보존하며 이 sequence에서 구현하지 않는다.

### P5_BUG_CATCH — 기본 곤충 포획의 완성
기존 BugCritter/Net/Inventory를 재사용한다.
초원 등에서 움직이는 곤충 발견 → 접근/방향 → Net/E 동작 → 명중 포획 또는 빗나감/이탈 → 실제 보상 → 진열/판매.
발견·추격·포획/실패가 화면과 소리로 읽혀야 한다. 빈손/틀린 도구/거리·가방 가득참·중복 포획을 검사한다.
현재 Day1 시간/지역 출현은 현 데이터 범위에서 확인한다. 계절·새 날씨 시스템은 만들지 않는다. 선택 행동을 MUST PATH 강제로 바꾸지 않는다.

### P6_CRAFT_PLACE_STORAGE — 낮 상품 준비·제작·배치·수납
기존 Workbench Kit/CraftingService/RecipeData/placement로 원재료 → 판재 등 가공품 → 상품/가판대·가구 → 진열/판매의 대표 경로를 완성한다.
레시피 개수 늘리기로 해결하지 않는다. 기존 레시피별 용도·재료·Root 잠금·결과를 대조하고 낮 상품 구성이 이해되는 최소 대표 경로를 선택한다.
대표 도구 upgrade1종은 선택 Root → recipe 해금 → 실제 재료 소비/제작 → 직접 사용 또는 NPC 지급까지 연결한다.
여러 작업대 종류/별도 설비 트리는 제외. 작업대 업그레이드는 실제 unlock/소비/효과 계약이 확정되지 않았다면 별도 후보로 남기고 임의 구현하지 않는다.
공통 배치의 기본 문법: 마우스로 캐릭터 주변 유효 위치 지정, E 확정, R 회전, Esc/RMB 취소.
좌클릭이 배치 확정과 선택/가격을 동시에 실행하지 않게 한다. 기존 WASD preview 이동은 호환 보조로 유지 가능.
유효/불가/범위가 보이는 preview, 재료/kit 소비, 취소 복구, Hold E 이동, 벽·가구·NPC·문 앞·고객 통로, 큰 건물 전체가 보이는 기존 camera build mode.
실내 상자는 기존 Storage 권위를 통한 실제 넣기/꺼내기·가득참/수량 보존을 제공한다. 별도 Inventory 권위를 만들지 않는다.
상품은 저장 ID를 바꾸지 않고 표시 계층에서 한국어 이름·맞는 모델·수량·가격을 읽히게 한다. Plank의 열매 모델/가격 가림도 해결한다.

### P7_COMPANION_GROWTH — 동행 선택의 이유·낮 작업·성장
선택 후보3/동행2와 기존 profile/ProducerNpcController/WorksiteBinding/schedule을 보존한다.
역할·특기·Root synergy가 선택 화면과 기존 로컬 외형에서 읽혀야 한다.
동행2가 이동/자기 집/작업·채집·상황 반응을 보여주도록 기존 생산과 이동을 연결한다. 가만히 서 있는 장식 캐릭터로 끝내지 않는다.
호환 동료에게 제작한 upgrade 도구 지급 → 이전 도구 반환 → 실제 장착/작업 → 생산 효과를 before/after로 확인한다.
그릇된 Root를 고른 플레이어에게 NPC gift를 강제하지 않는다. NPC 도구 내구도/다단계 커리어/실제 농사 루프는 제외한다.
Root4는 처음부터 읽고 선택 가능하되 정착 완료 뒤 License Point로 첫 선택한다. 선택 Root의 다음1단계 preview를 보여준다. 전체 트리는 구현하지 않는다.

### P8_BUSINESS_REPORT — 낮 상품이 실제 밤 영업과 평가로 연결
P3~P6에서 직접 얻거나 만든 상품을 실제 가판대에 올리고 가격을 정해 P폰으로 개점한다. QA 재고 지급은 이 대표 제품 경로 증거에 쓰지 않는다.
기존 가판대3/1개 이상 OPEN 조건, 관광객7·동시3·영업시간 성과는 재사용한다.
기존 프로필·방문 스케줄·PurchaseEvaluator로 동행/주민과 관광객의 방문·관심·가격 반응 차이를 확인한다. 최신 검사 residentsSeen=0을 주민 검증 완료로 취급하지 않는다.
길막힘·동시 평가·평가 중 가격·품절·영업 중 이동 금지·자율 구매/거절·결제/피드백·마감 후 퇴장을 확인한다.
재고 감소=실판매, 돈 증가=SalesLog=Report 매출, CLOSE1회. 소량 판매의 과한 A/S를 되살리지 않는다.
정착/직접 탐험·채집/제작·성장/동료 지급/판매가 기존 Report 점수와 행동 댓글에 반영되는지 확인한다. 낮 동안 점수/랭크 상시 노출 금지.

### P9_CHARACTER_FEEL — 딩컴 레퍼런스 기반 새 모션 재작업
앞의 모션 검수 계약으로 맨손/도구의 이동·정지·회전·점프/착지·그립·타격을 정상 제품 카메라에서 재작업한다.
기존 PlayerController/CameraController/Animator 권위와 이미 검증된 접지·상태 전이를 재사용한다.
이번 sequence 승인은 새 재작업 범위만 허용한다. 사람에게 거절된 옛 결과는 REJECTED, 보류 샘플은 보존한다.
새 before/after 원속 비교 영상과 관련 게임플레이 회귀를 남긴다. 시각 ACCEPTED는 실제 사용자의 판정까지 UNVERIFIED로 둔다.

### P10_ISLAND_FIRSTDAY — 섬·조명·도입·첫날의 연결
Demo256 유지. 항구/해안/숲/고지대/초원의 공간 정체성·자원/물가/곤충·랜드마크·통로를 정상 camera 탐험으로 확인한다.
주요 발견이 약10~20초 이동 간격에서 이어지는지 실제 동선으로 점검한다. 오전9시 빛/캐릭터 음영/나무, 일몰, 밤 상점 창/실내·상품/가격 가독성.
집/상점/작업/판매 공간이 생활 공간으로 읽혀야 한다. 빈 절차 평면·debug label/primitive·불규칙 단차·건물/나무 가림을 해소한다.
NEW GAME → 교육/가격 인증 → 동행2 → 8~12초 Pixel Voyage → 실제 섬 → 보급 → 정착/성장 → 일몰 → 영업/Report의 안내·전환·소리.
배/부두/섬의 겹침과 모자이크만으로 흐름이 읽히지 않는 도입을 개선하되 장편 영상·새 범용 컷신 시스템은 만들지 않는다.
Soft Clock은 정착 전 갑자기 밤으로 넘기지 않으며, 성장/상품 준비 기회를 주고 자연 일몰로 이어져야 한다.
GameView polish pass/Presentation pass는 이 경험 범위의 기존 runtime/scene/prefab/material/audio/animator를 함께 수정할 수 있다.
Golden Prototype_FirstDay.unity 덮어쓰기와 MainGame 통합, World authority 재작성은 하지 않는다.

### P11_WINDOWS_DELIVERY — 납품·실제 연속 검증
기존 빌드 경로로 고유 Windows 후보를 만들고 소스 HEAD/관련 dirty·build identity를 기록한다.
실제 NEW GAME부터 Report까지 continuous MUST PATH를 검증한다. 가능한 구간 입력은 실제 제품 메뉴/조작으로 수행하고 fixture/API 점프를 완주 증거로 대체하지 않는다.
선택 낚시·곤충·동료 지급을 포함한 낮 활동 대표 경로도 따로 검증한다.
현재 형식의 저장 → 정상 종료 → 재실행/타이틀 Continue → 배치·수납·상품·돈·진행 재검증. 사용자 save 대신 격리 경로.
이미 실패한 Windows 자동 입력 방법을 같은 방식으로 반복하지 않는다. 자동 증명이 안 되는 구간은 후보에서 바로 따라 할 정확한 사람 검수 순서로 남긴다.
실제 측정한 standalone 프레임 타임/FPS·해상도·경로와 오디오, 빌드 경고의 실제 영향, 미해결 기능/화면 항목을 보고한다.
최종 사용자 검수 전에는 RELEASE ACCEPTED/데모 전체 PASS를 선언하지 않는다.

## 사용량·검증 중복 방지

이 규칙은 제작과 검증의 중복을 줄이는 운영 지침이다. 승인 범위·AGENTS의 필수 절차·각 티켓 수락 기준·실제 Play/GameView·저장/경제 검증·최종 사람 검수는 유지한다.

- **읽기/출력:** 매 작업의 필수 문서는 지정 순서대로 읽는다. 같은 티켓에서 이미 읽은 불변 본문은 재출력하지 않고 변경된 부분만 확인한다. 구현은 관련 파일/심볼/문서 §에 한정한다. 저장소 전체 검색·씬/오브젝트 전체 덤프·메모리 전체 조회를 기본값으로 삼지 않는다. 요약과 근거 경로를 출력하고 필요한 추가 문맥만 읽는다.
- **재사용 판단:** 기존 증거는 대상·실제 입력 경로·수락 기준이 같고, 이후 관련 code/scene/prefab/asset/설정이 변하지 않았음을 확인할 수 있을 때 재사용한다. 파일 시각이나 PASS 개수만으로 동일성을 추정하지 않는다. 관련 변화·새 실패·누락된 필수 검증·미해결 화면이 있을 때 그 영향 범위만 다시 검증한다. 기록 없는 항목을 PASS로 취급하지 않는다.
- **검증 계획:** 수정 전에 이번 티켓의 핵심 경로, 변경으로 영향을 받는 회귀, 필요한 검증기와 재실행 조건을 정한다. 작동하는 관련 검증기를 우선 사용한다. 서로 독립적이고 묶을 수 있는 읽기/상태 질의는 묶되 수정·compile·결과를 기다리는 작업은 순서대로 수행한다.
- **compile/Play:** 관련 수정을 묶고 compile → 필요한 검사 → 실제 입력 → GameView를 확인한다. 파일 하나마다 같은 전 과정을 반복하지 않는다. 실패를 고치면 실패 검사와 수정의 영향 범위를 재검증한다. Studio의 compile/fix 최대3회·targeted Play 최대3회·validator 최대1개는 상한이며 소진 목표가 아니다. 기능/화면 기준 충족 뒤 새 변경·실패·미해결 근거가 없으면 다음 티켓으로 진행한다.
- **범위 확장:** 매 티켓마다 P1의 저장4세션·D1 점수식·D2 전체 영업·전체 MUST PATH·모든 validator를 다시 실행하지 않는다. 저장/경제/입력 권위 등 공통 경로 변경이 있으면 실제 영향받는 필수 회귀를 수행한다. 새 테스트/검증기는 기존 검사가 못 보는 수락 기준이나 입증된 결함에 필요한 경우만, 해당 예산 안에서 만든다.
- **P2 묶기:** 홈/기록/상점/자동 마감/입력 복귀와 시간대별 HUD를 같은 Play 흐름에서 확인한다. 수동 마감은 Day1 재개 제한을 보존한 별도 초기 상태의 짧은 검사로 확인한다. 각 화면/시간대를 위해 전체 시나리오를 다시 시작하지 않는다. 준비 fixture와 실제 입력은 구분한다.
- **증거/레퍼런스:** 새로 바뀐 화면과 핵심 상태의 전후 증거를 남긴다. 같은 상태의 중복 촬영/영상 재인코딩은 새 결함이나 비교 조건 문제가 있을 때만 한다. 모션 원속 연속 비교는 생략하지 않는다. 기존 확인 자료의 필요한 구간을 먼저 사용하고 경험별 대표 레퍼런스1~2개로 시작한다. 추가 자료는 해결되지 않은 관찰 요소가 있을 때 찾고 그 이유를 기록한다.
- **대기/환경:** 전체 doctor는 재개 시 수행하고, 티켓 사이에는 필요한 identity·compile·Console 상태만 확인한다. 연결/프로젝트/설정 변화·crash·새 환경 실패가 있을 때 전체 점검을 반복한다. 결과는 해당 실행의 고유 폴더로 확인하고 감시는 대략10~30초 간격으로 한다. 이미 종료된 작업의 반복 폴링·겹치는 실행·같은 실패 방법의 반복은 하지 않는다.
- **빌드/전체 경로:** 전체 NEW GAME→Report와 Windows 납품 검증은 P11에서 수행한다. 앞선 티켓에서는 해당 경험과 필요한 회귀를 확인한다. Windows 후보는 P11에서 만들고 검증한 후보에 영향을 주는 소스/설정 변경이나 입증된 빌드 결함이 있을 때만 다시 빌드한다. 입력 감시 도구 실패만으로 재빌드하지 않는다.
- **보고/진행:** 중간 보고는 바뀐 점·검증 결과·다음 행동을 짧게 쓴다. 완료 검사의 긴 목록과 이전 blocker 설명을 반복하지 않는다. 월별 로그/현재 문서/Handoff는 기존 갱신 시점을 지킨다. 관찰 backlog·스킬 개선 리뷰는 제작의 추가 완료 조건으로 넣지 않는다. Opus/max 설정은 유지하며 새 병렬 Claude 세션이나 자동 재호출을 만들지 않는다.

사용량 절감률이나 완료 시간을 측정 없이 약속하지 않는다. 증거와 필요한 검증을 생략해 절감한 것으로 보고하지 않는다.

## 승인 record와 연속 실행

기존 loop-state.json이 유일한 실행 상태다. 사용자 선승인 메시지 수신 뒤에만 다음을 기록한다.
- record key: demoFeedbackFinishApproval.
- policyId: PREAPPROVED_MILESTONE_CONTINUATION.
- approvedOn/approvalSource: 실제 수신 날짜와 사용자 승인 메시지. 이 파일 작성 날짜를 승인 날짜로 추정하지 않는다.
- approvedTickets: P1_CURRENT_CONTINUE, P2_PHONE_GUIDE_D4, P3_DIRECT_GATHER, P4_FISHING, P5_BUG_CATCH, P6_CRAFT_PLACE_STORAGE, P7_COMPANION_GROWTH, P8_BUSINESS_REPORT, P9_CHARACTER_FEEL, P10_ISLAND_FIRSTDAY, P11_WINDOWS_DELIVERY.
- ticketDefinitions: 위 범위·수락 기준을 그대로 기록. preapprovedThrough=P11_WINDOWS_DELIVERY.
- activeTicket=P1_CURRENT_CONTINUE; status=approved_not_started; stopAtMilestone=FINAL_WINDOWS_HUMAN_REVIEW.
- localTicketCommitsAllowed=false; pushAllowed=false.
- 한정 phone/save 수정과 신규 모션 재작업 허용, v16 변환/원본 save/SaveData schema 변경/무관 dirty/Golden/Packages 보호를 기록.
- demoProductionRouting.selectedApproval만 새 record로 전환, unityEditorOwner=claude-code 유지.
  새 sequence에만 stopAtMilestoneReached=false를 기록한다. 기존 D 승인/검수 결과/보호 record는 보존한다.
- Handoff와 현재 작업 큐는 실제 새 선택을 설명하도록 해당 부분만 갱신한다.
- 옛 F1~F6, CONTENT/WORLD/BETA ACTIVE, 파일 존재는 별도 승인이 아니다.

선승인 후 첫 doctor를 --agent claude-code로 실행해 실제 프로젝트/도구/Editor 연결/Console을 확인한다.
연결 성공이 Play 검증이나 구현 PASS는 아니다. 이미 열린 같은 프로젝트 Editor를 재사용하고 새 batchmode/두 번째 Editor를 띄우지 않는다.
닫힌 Editor는 기존 실행 경로·최신 crash report·D3D11 baseline으로 한 번만 시작한다. 외부 설치/무한 재시작 금지.
각 티켓 수정 전 파일 목록·기존 authority·보이는 결과·검증 방법을 보고하고, compile/validator/diff/Play/GameView를 끝낸 뒤 다음 티켓으로 진행한다.
상태/금액/판매 성공을 API로 만들어 PASS하지 않는다. 테스트 fixture를 썼으면 범위·side effect를 구분한다.
증거는 Logs/VisualQA/<실행시각>-<ticket>/의 새 폴더, before/after 같은 조건. 메뉴 실행 뒤 완료 결과를 읽는다.
저장 등 core 기본 retry, Studio compile/fix3회·targeted Play3회·전용 validator1개 예산 유지.
기계적 실패는 범위 안에서 복구한다. native crash, save 손실 위험, 무관 dirty 충돌, KEEP 교체/대형 재작성, 비호환 migration, 파괴 작업, 예산 후 환경 불가는 HARD STOP.
한 티켓에 HARD BLOCKER가 있으면 원인/증거/의존성을 한번 기록하고 멈춘다. 다음 티켓으로 뛰어 전체 완료를 주장하지 않는다.
검수 부재만으로 독립적으로 승인된 다음 작업을 멈추지 않는다. 사용자에게 이후 명시적으로 받은 새 hold는 준수한다.
단일 Claude 제작 세션으로 진행하고 quota/context 종료는 Handoff 갱신 후 재개한다. 자동 재호출/무한 AI 실행기를 만들지 않는다.

## 납품과 기록

최종 결과에는 티켓별 기능 PASS/FAIL/NOT RUN, presentation/실제 GameView, Human ACCEPTED/REJECTED/UNVERIFIED를 각각 적는다.
후보 위치/실행/조작, reference 대비 바뀐 점, 실제 연속 영상과 모션 비교, save 재진입, 성능/소리, 남은 결함과 확인 못 한 항목을 전달한다.
검증 안 된 부분은 정확한 사람이 따라 할 경로를 적는다. 수치 PASS가 사람의 REJECTED를 지우지 않는다.
월별 개발 로그는 경험이 끝날 때 한번, 현재 문서는 바뀐 계약/연결/우선순위 해당 부분만 갱신한다. Handoff는 중단/세션 종료 때 한번 덮어쓴다.
delete/destructive Git/file, commit/push, 외부 패키지/에셋 다운로드, v16 변환, 새 Save/Economy/Inventory/Shop/Player 권위는 금지한다.
