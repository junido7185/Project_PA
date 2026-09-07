# 졸업 발표 진행 기록 — 2026-09-08

기존 기능 데모를 P.A. Company 출항 인증 코스의 첫 플레이로 연결했습니다.
이동·자원 확보·유통의 세 실습 구역을 실제로 걸어서 통과합니다.
나무에서 얻은 열매가 기존 가방과 진열대에 들어가며 직접 가격을 정합니다.
기존 NPC 구매 판단으로 7G 판매가 발생하고 실제 잔액에 반영됐습니다.
ART-000 Blender/CC0 자산으로 안내판·판매대·나무·항구 공간을 구성했습니다.
출항 인증 완료까지 검증했고 동행 선택·배 이동·섬 도착은 아직 구현하지 않았습니다.

## 이전 상태와 바뀐 경험

Prototype의 기존 기능과 WorldSandbox/CONTENT 구현은 보존했다. 새 개발용 PA_DepartureTutorial 씬은 세 Training Bay, 그림 안내판, 바닥 화살표, 실제 채집/진열/가격/판매를 하나의 코스로 제공한다. 기존 NEW GAME 메뉴에 통합한 상태는 아니다.

## 사용한 자산과 시스템

기존 Departure TrainingShelf/TrainingBoards/Checkpoint/Fruit/Boat와 ART-000 Nature·cargo wrapper를 재사용했다. 원본/파생 출처와 실제 Blender render는 ../../AssetProvenance/VS_PRESENT_001/README.md. 추가 Hero Asset 제작 없음.

Inventory, Hotbar, PlayerInputHandler, PlayerController, PlayerInteraction, DaytimeStockPrepPoint, DayNightShopLoopController, Shop/ShopSlot, ShopPriceUI, NpcController/PurchaseEvaluator, SalesLogManager, EconomyService, CameraController가 기존 권위를 유지한다. 새 두 runtime 파일은 진행 관찰과 UI 표현만 담당한다.

## 해결 및 검증

- editor builder로 새 씬·wrapper·NavMesh를 작성하고 직렬화 참조를 검사했다. Golden/MainGame YAML 변경 없음.
- 실제 InputSystem 키 입력으로 spawn→movement→tree→fruit3→display1→price7G→NPC approach→purchase→money0→7G→complete 연속 PASS.
- 첫 판매 판단 확률0.895, 구매1건. 성공 hard-code 없음. 고가 거절 재시험은 이번 턴 미실행.
- Runtime/Editor compile 오류0, 기존 warning3. P0 Play Mode validator PASS. 신규 blocking runtime 오류0. git diff --check PASS.
- 두 최종 PNG 1920×1080을 직접 열어 안내판 방향, 가격창/상품/반응 위치와 한글을 검사했다. 전체보기는 HUD를 숨긴 실제 씬 캡처다. 가격/반응 화면은 판매 직후 남은 열매를 재진열하고 실제 가격 UI를 다시 연 상태다.
- 전체 Golden 회귀, save/load, standalone 빌드는 이번 P0 마무리에서 확인 못 함. SaveManager 없이 개발용 세션으로 실행한다.

## 발표 실행

Unity 메뉴 Project PA > Presentation > Open Departure Tutorial, Play. WASD 이동, SPACE 채집/진열/가격창 열기, 표시된 실습 가격7G 확정. 기본가10G에서 -10 한 번/ +10 한 번의 UI 조작은 기존 정책 그대로다. 초기 진열가격7G를 유지해 확정하면 된다. 구매가 거절되면 가격창을 다시 열어 재확정할 수 있다.

## 미구현과 다음 단계

P1 동행 선택과 P2 항해/섬은 시작하지 않았다. 완료 화면은 동행 선정 해금 피드백까지만 제공하며 다음 버튼은 비활성이다. 첫 화면 미술·캐릭터 자세·수동 조작감은 사람 최종 확인 대상이다.

현재 checkpoint는 VS 파일만 포함한다. 기존 미커밋 CONTENT의 가격 확정/구매 판단 관찰 이벤트에 의존하므로, 현재 작업 트리에서 실행해야 한다. 해당 CONTENT 작업을 임의 포함/변경하지 않았다. Unity가 갱신한 공용 Jalnan2 font 캐시는 기존 자산 경로라 별도 dirty로 보존하고 이번 commit에서 제외한다.

## 증거

- 01_PA_Company_FirstView.png
- 02_Tutorial_PriceAndReaction.png
- 02a_Tutorial_Price.png / 02b_Certification_Complete.png
- P0-validation.json / P0-validation-excerpt.txt / P0-integrity.json
- Logs/VS_PRESENT_001/Compile_Final.log / P0_Final_Editor.log


## 2026-09-07 — 현재 작업 통합 checkpoint / push 승인

- 사용자가 현재까지 작업의 commit/push를 명시 승인했다. 기존 P0 0b4e511에 이어 보존했던 CONTENT 코드·관찰 이벤트·설계/발표 자료를 함께 checkpoint한다. CONTENT-002의 미완료 검증 상태와 BETA 저장 부채는 해소됐다고 표시하지 않는다.
- Runtime/Editor compile 오류0, 기존 경고3, diff 검사 PASS. 증거 Logs/VS_PRESENT_001/PrePushCompile_Restored.log. 이번 Git 작업에서 새 Play Mode/저장/빌드 검증은 하지 않았다.
- 기존 Word 문서 삭제 상태는 그대로 기록한다. 로컬 개인 플러그인 설정 .claude/settings.json은 commit 대상에서 제외한다. 강제 push/이력 재작성 없음.
- P0가 사용하는 ShopPriceUI/PurchaseFeedback 관찰 이벤트가 이번 checkpoint에 포함되므로, 이전 P0 보고서의 '미커밋 이벤트 의존' 제한은 이 통합 checkpoint에서 해소된다. clean checkout 실행은 별도 수행하지 않았다.
- 다음은 사람의 P0 수동 확인/명시적 후속 지시다. P1/P2 및 CONTENT 후속 구현은 자동 시작하지 않는다.
