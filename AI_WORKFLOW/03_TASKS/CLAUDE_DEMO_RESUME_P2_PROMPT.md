# Claude — P2부터 승인된 데모 제작 재개 (2026-10-04)

기존 실행 계약의 재개 지시다. 별도 승인 record나 상태 저장소를 만들지 않는다. 아래 본문을 Claude에 보내거나 이 파일을 읽고 실행하라고 지시한다.

---

Project P.A.의 승인된 제작을 재개해. 목표는 **낮 탐험·채집 → 제작/상품 준비 → 동료·성장 → 밤 실제 영업 → 행동에 비례한 정산**이 납득되는 Day 1 데모다.

## 재개 기준

기존 `demoProductionRouting.selectedApproval=demoFeedbackFinishApproval`을 사용한다. Unity 담당은 Claude, P1~P11 선승인은 이미 기록돼 있다. 작성 시점은 P1_CURRENT_CONTINUE 기능 PASS / 사람 UNVERIFIED, activeTicket=P2_PHONE_GUIDE_D4다. 실제 상태가 더 진행됐다면 최신 티켓에서 이어간다. 기존 승인과 완료 티켓을 초기화하지 않는다.

AGENTS.md, 지정 순서의 현재 문서 네 개, CODEX_HANDOFF의 Exact next action을 확인한다. `AI_WORKFLOW/03_TASKS/CLAUDE_OPENING_DEMO_EXECUTION_PROMPT.md`의 범위·레퍼런스·모션·**사용량·검증 중복 방지** 규칙을 적용한다. 같은 티켓에서 이미 읽은 불변 본문을 반복 출력하지 않는다.

P1 증거:
- `Logs/VisualQA/20261003-064739-P1_CURRENT_CONTINUE/CurrentFormatChain/result.txt`
- `Logs/VisualQA/20261003-065243-P1_CURRENT_CONTINUE/LegacyV16Notice/result.txt`

P1은 격리 저장·4 Play 세션 복원 PASS다. v16은 호환 불가 안내·원본 보존이며 변환하지 않는다. standalone 종료·재실행은 P11에 남아 있다. 새 회귀/영향 근거 없이 P1 재구현·4세션 재검사를 반복하지 않는다. 현재 문서의 옛 FAIL·전화 보호 문장만 실제 증거와 allowedLimitedScope/protectedScope에 정합화한다. 터미널 diff의 old/new 중복 줄이나 결과 감시 실패를 제품 실패로 취급하지 않는다.

재개 시 doctor `python -X utf8 Tools/LoopEngineering/Test-ProjectPADemoEnvironment.py --agent claude-code`를 실행한다. 같은 프로젝트의 열린 Editor를 재사용한다. 닫혔다면 기존 실행 경로·최신 crash report·D3D11 기준으로 한 번 시작해 MCP를 확인한다. 관련 dirty를 보호하고 수정 예정 파일·기존 권위·보이는 결과·필요한 검증과 재실행 조건을 먼저 보고한 뒤 구현한다.

## 지금 완성할 P2_PHONE_GUIDE_D4

1. SmartphoneUI 홈의 **알림 기록** 타일을 실제 FeedUI 패널에 연결한다. 탭 index를 추측하지 않는다.
2. 안내 기록의 잘림·겹침·빈 카드와 `Processed…` 디버그 줄을 정리한다. 반복 획득 토스트와 안내 기록의 구분을 유지한다.
3. 상점 앱의 영업 상태·버튼·매출·Report와 마감 문구를 실제 상태에 맞춘다. 자동 마감 뒤 `영업을 시작했어요.` 잔류를 해소한다.
4. 손에 물건을 들어 제외된 가까운 문·보급·대화 대상에는 **[X] 손 비우기**를 안내하고 X → E 행동을 확인한다.
5. 문 E=출입, 가판대 E=진열/가격, 작업대 E=제작, 상점 운영=P폰을 유지한다. 설치는 기존 blueprint/placement에 연결한다. 안내는 짧은 토스트·P 기록·대상 프롬프트로 제공한다.

## P2 검증

실제 InputSystem/uGUI로 **P → 홈 타일 클릭 → 기록 읽기 → 뒤로 → 상점 앱 → 전문 분야/OPEN → 영업 → 자동 마감 → Report/매출 → Esc 닫기 → 이동**을 검증한다. 수동 마감은 별도 초기 상태의 짧은 제품 버튼 경로로 확인한다. 폰의 E/Esc 입력 누출과 X → E도 확인한다.

관련 Runtime/Editor compile·Console·Play·1920×1080 GameView를 확인한다. 전화 홈/기록·마감 후 앱·X 안내와 오전/일몰/영업 중/21:30/마감 뒤 HUD를 같은 Play 흐름에서 가능한 만큼 묶어 캡처한다. 작은 글자는 원본 해상도로 확인한다. 옛 시계 첫 자리 누락 판정은 철회됐고 원본 20:13은 정상이다. P1의 ForceSet 갱신 수정은 유지한다.

SelectTab 직접 호출이나 영업/정산 핸들러로 제품 입력 검증을 대체하지 않는다. 준비 fixture는 명시하고 전체 NEW GAME 완주로 보고하지 않는다. 새 증거 폴더에 입력·기대/실제 결과·원본 화면을 남긴다. 가능하면 대표 경로 60~90초 영상도 남긴다. 해당 코드 변경과 관계없는 검사를 덧붙이지 않는다.

## 계속 진행할 조건

P2 검증 후 기존 계약의 P3 채집 → P4 기본 낚시 → P5 곤충 → P6 제작/배치/수납 → P7 동료/성장 → P8 영업/Report → P9 새 모션 → P10 섬/첫날 → P11_WINDOWS_DELIVERY로 한 티켓씩 계속한다. 일반 구현 선택이나 사람 검수 미실시만으로 매번 승인 질문을 하지 않는다.

딩컴은 이동·도구·타격·배치, 동물의 숲은 UI·조명·생활 공간의 실제 gameplay 구간을 참고한다. 출처/시각·관찰·게임과의 차이·수정·전후 증거를 기존 experienceReferences/evidence에 기록한다. 모션은 맨손/도구별 정상 카메라의 원속 연속 비교와 기존 최소30/권장60fps 촬영 목표를 따른다. 거절된 옛 결과·보류 샘플을 수락된 스타일로 취급하지 않는다. 상세 조건은 기존 계약을 따른다.

사용량을 아끼기 위해 관련 없는 문서/메모리·전체 검증기·중복 캡처·반복 빌드·관찰 백로그 리뷰를 추가하지 않는다. 관련 필수 검증과 실제 화면 품질 기준은 유지한다. 통과 후 새 변경/실패/미해결 근거가 없으면 다음 티켓으로 간다.

기존 core/Studio 복구 예산과 HARD STOP, 최신 사용자 hold, 사용자 save·무관 dirty·Golden·Packages 보호를 유지한다. 외부 패키지/에셋 다운로드·새 권위·대형 재작성·schema/migration·삭제·commit/push는 금지한다. 사용량/컨텍스트 종료는 Handoff에 검증·증거·남은 일·Exact next action을 한 번 기록하고 멈춘다.

P11에서 실제 NEW GAME→Report, 대표 낮 활동, 격리 저장→정상 종료→standalone 재실행→Continue를 검증해 Windows 후보와 사람 검수 경로를 전달한다. 기능·화면·사람 판정을 구분하고 **FINAL_WINDOWS_HUMAN_REVIEW에서 멈춘다. 최종 사람 판정 전 전체 PASS/RELEASE ACCEPTED를 선언하지 않는다.**
