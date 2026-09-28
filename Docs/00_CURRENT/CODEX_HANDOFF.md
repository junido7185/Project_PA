# CODEX HANDOFF — 보정 포함 Windows 후보 / standalone 입력 경계

## Current task / status
2026-09-27 승인: Report 뒤 중복 제목 숨김, 기존 상품 표시명/내부 ID 유지, 이전 후보 보존 후 별도 Windows 후보 빌드 및 NEW GAME→Report 검증.
표시 수정·Editor 관련 회귀·빌드 완료. standalone은 NEW GAME→출항 교육 STEP1까지 VERIFIED, 이후 이동 입력 확인 실패로 전체 MUST PATH 미검증. 사람 검수 부재만으로 중단한 것이 아니다.

## Baseline / approved files
milestone/gameplay-beta-85 @4374b2aa27e73c2c7fec086eac6c4130eb702306 + 기존 dirty/index.
이번 코드 수정: Assets/Scripts/Presentation/FirstDayWorldPresentation.cs의 Report-open 알림 표시 조건만. 기존 Item.itemName 표시 경로와 id 유지, 상품 번역체계 신설 없음. 필요한 종료 문서/Logs 및 별도 후보 출력 승인.
보호: 기존 후보2개,16m 카메라, 사용자 씬/배치, 경제·구매·점수·저장 권위와 .codex. index/기존377후보파일/사용자save JSON2개 해시 동일.

## Completed / validation
Report 열린 동안 뒤쪽 toast 숨김, 닫은 뒤 일반 toast 복귀; 실제1920×1080 GameView 확인. Editor 자율 NPC 판매 재고1→0/돈0→8G/Report1건8G/첫판매1회, Escape 닫기 PASS. QA 준비 pose·20시 설정·public OPEN/CLOSE 사용, standalone 증거와 구분. Compile0errors/기존warnings4, Play Console0errors.
새 후보: Builds/Windows/Candidate-ShopPolish-20260927-194240/Project_PA.exe. Windows64 성공0errors/1warning. 이전6파일 보정과 이번 알림 숨김 포함; 납품DLL=Player compiler DLL, PDB실제소스143건 hash 일치/수정7파일 포함. 빌드 전 소스2652파일/dirty patch/2051파일 archive와 빌드 후 입력hash 동일.
실제 standalone D3D11: 타이틀→마우스 새 게임→저장 기록 확인→출항 교육STEP1 확인. 정상 창 닫기/프로세스 종료 관찰(종료코드 미수집).

## Blocker / known limits
D키1.2초 OS SendInput 성공 보고 뒤 캐릭터 이동 확인 못 함. 후보 foreground 유지; Unity 키보드 수신/이동 차단 원인 미확정. 사용자 지시대로 재입력/포커스 우회 반복하지 않음. 정착→진열→가격→판매→CLOSE→Report standalone NOT VERIFIED / HUMAN-UNVERIFIED.
새 빌드의 uncompiled-code warning1건은 runtime 보정 반영을 DLL/PDB/IL로 확인했지만 경고 원인 해소는 미주장. 기존185shader경고 미출력은 캐시 빌드의 관찰값이며 해결 의미 아님. Player.log: 기존title collider4건, 출항 진입 valid NavMesh 없음1건(실제 구매 영향 미검증). FPS 미측정.

## Evidence / exact next action
Logs/VisualQA/ReportCandidate-20260927/DELIVERY_REPORT.md에 후보·Player.log·전후캡처 전체 Windows 경로, 실제 범위, 알려진 문제, 사람 실행 명령과 조작순서 기록.
보정후 Editor: final-report-clear.png. standalone: standalone-departure.png / standalone-input-boundary.png. window-state/input-events와 bubble-sale-result,build-result,preservation 참조.
다음은 새 후보를 interactive Windows에서 실행해 출항STEP1의 물리 WASD/Space 입력부터 확인하고 문서의 정착→Report 경로를 완주하는 것. 동일 자동 입력 시도를 반복하거나 Editor 결과를 standalone PASS로 바꾸지 않는다. 새로운 증거 없이 T2~T6 전체 재검증/기존 후보 덮어쓰기/무관NavMesh·Save 수정 없음.

## Commit / environment
임시 입력 설정 복원, Editor Edit/WorldSandbox, standalone 종료. 사용자 dirty/index 보존. commit/push/stage/파괴적 Git 없음.
