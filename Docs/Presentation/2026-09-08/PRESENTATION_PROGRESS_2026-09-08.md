# 졸업 발표 진행 기록 — 2026-09-08

기존 기능 데모를 P.A. Company 출항 인증 코스의 첫 플레이로 연결했습니다.
이동·자원 확보·유통의 세 실습 구역을 실제로 걸어서 통과합니다.
나무에서 얻은 열매가 기존 가방과 진열대에 들어가며 직접 가격을 정합니다.
기존 NPC 구매 판단으로 7G 판매가 발생하고 실제 잔액에 반영됐습니다.
ART-000 Blender/CC0 자산으로 안내판·판매대·나무·항구 공간을 구성했습니다.
출항 인증 뒤 생산자 세 명 중 두 명을 선택하고, 선택 결과를 다음 단계에 전달합니다.
선택한 두 사람과 12초간 배를 타고 이동한 뒤, 숲과 단차가 있는 자연 상태의 섬에 함께 도착합니다.

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

## P1 — 동행 선택 / 2026-09-09 복구 완료

벌목꾼·광부·농부 3명 비교 → 2명 선택 → 출항 확정. 기존 직업 프로필과 C-03/C-04/C-02를 참조하며 이름·성격 제안은 TEMP, MBTI 중립 축은 ? 표시다. 초기 생산 분야를 선택하는 세션 상태이며 고용·입주·Save 변경이나 실제 생산 시작은 이번 P1에 없다. 기존 P0 완료 버튼은 인증 후 선택 화면을 연다.

D3D11 개발용 선택 진입 검증 PASS, 새 03_CompanionSelection.png 1920×1080 확인. 선택 취소·교체·상한·확정 조건·정확한 2개 ID 1회 전달·동결까지 통과했다. 이전 9/8 capture 실패는 9/9 fresh stable GameView capture로 해소했다. P2는 다음 승인 단계다.

## P2 — 출항과 섬 첫 도착 / 2026-09-09

발표 한 줄: **“기존 기능을 오프닝 경험으로 연결하고, 생산자 두 명을 선택해 함께 섬으로 출발하고 도착하는 과정까지 구현했습니다.”**

- 출항 확정 → 선택된 두 NPC 모델 탑승 → 제한된 갑판에서 WASD 이동 → 12초 자동 이동 → fade → 같은 두 NPC와 섬 도착 → 기존 이동 조작으로 섬 탐색. 도착 목표는 ‘첫 거점을 세울 장소를 찾아보세요.’ 한 개다.
- `DepartureVoyagePresentation`은 P1의 확정 ID를 읽는다. `WorldGridService.TryRestoreSnapshot`의 검증된 초기화 경계로 16×16 authored cell island를 전달하고, `WorldChunkTerrain`이 메시와 충돌체를 만든다. `WorldPlayerTraversalGuard`가 도착 셀과 물가 이동을 처리한다. WorldGrid/terrain/Save/Hiring 구현 자체는 변경하지 않았다.
- 기존 `PA_DepartureBoat` (P0 Quaternius 파생), ART-000 CommonTree/BirchTree/Rock/Dock/Chest wrapper, 기존 C-03/C-04/C-02 모델을 재사용한다. Blender 신규 제작·신규 외부 자산·신규 provenance 없음. 원본 출처는 기존 `Docs/AssetProvenance/VS_PRESENT_001/README.md` 및 ART-000 intake 기록에 남아 있다. 인공물은 임시 부두와 보급 상자만 추가했다.
- 나무·바위는 첫 섬의 자원 위치를 보여 주는 표현이다. 채집/채광 보상 연결, 생산 실행, 입주/고용/산업 성장, 저장 재시작, 관리 거점 placement preview는 DEFER. 새 건축 또는 Save 체계를 만들지 않았다.

### 실행과 검증 범위

Unity `Project PA > Presentation > Play Companion Selection (Development Entry)`에서 선택 화면으로 직접 진입한다. 광부·농부를 고르고 출항하면 최종 캡처와 같은 조합이다. 처음부터는 `Open Departure Tutorial` → Play → 기존 인증 완료 후 ‘동행자 선정’ 버튼을 사용한다. 이번 자동 검증은 개발용 선택 진입부터 진행했으며 **P0 실제 인증 완료→P1 클릭을 포함한 전체 시작부터의 연속 플레이는 확인 못 함**이다. P0 검증기는 재실행하지 않았다.

Runtime/Editor 순차 compile 오류0. P2에서는 실제 InputSystem 키 입력으로 갑판 이동/난간 제한과 섬 도착 후 이동을 검사하고, 12초 경과·선택 ID 일치·같은 NPC 오브젝트 유지·WorldGrid 건조 셀·메시/콜라이더·missing script를 확인했다. 전체 Golden/CONTENT/save 회귀나 standalone 빌드는 범위 밖이다.

변경 scene 없음. `Assets/Resources/DepartureTutorial/DepartureContinuation.prefab`에 P2 자산 참조를 Editor utility로 추가했다. 기존 카메라의 추적 대상을 앞쪽으로 조정하여 섬과 배가 함께 보이도록 했으며, 별도 카메라 framework는 없다.

### 최종 화면

- `03_CompanionSelection.png` — 광부·농부를 선택한 P1 recovery 실제 화면.
- `04_Boat_To_Island.png` — 같은 광부·농부 조합으로 새로 플레이한 P2 항해 화면.
- `05_Island_FirstArrival.png` — 같은 실행의 두 NPC와 섬 도착 화면.
- 이미지 세 장은 모두 1920×1080 GameView. 03은 P1 검증 실행, 04·05는 P2 검증 실행으로 구분되며 단일 실행 녹화로 주장하지 않는다.
- 상세 증거: `P1-validation.json`, `P1-recovery-excerpt.txt`, `P2-validation.json`, `P2-validation-excerpt.txt`.

최종 P2 D3D11 PASS. 광부·농부 ID와 같은 NPC 오브젝트가 도착했다. blocking Console/native crash 0, 실제 PNG 쓰기 안정화 PASS. 약한 수면 경계선·캐릭터 자세·최종 가독성은 작은 presentation polish debt로 남긴다. P1 checkpoint `6b283c1`, P2는 이 기록을 포함한 별도 local checkpoint이며 push하지 않는다.
