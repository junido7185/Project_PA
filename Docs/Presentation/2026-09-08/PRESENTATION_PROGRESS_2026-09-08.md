# 졸업 발표 진행 기록 — 2026-09-08

## 조작감 개선 — 2026-09-10

카메라를 캐릭터 가까이 옮겨 첫 플레이에서 표정과 행동이 더 잘 보입니다.
플레이어가 실제 움직인 속도에 맞춰 걷고 멈추며, NPC의 대기 동작도 차분하게 정돈했습니다.
물건 바로 앞에서 상호작용하고, 가격창을 여러 번 사용해도 마우스를 바로 쓸 수 있습니다.
판매가 끝나면 열매가 진열대에서 사라지고 기존 경제 시스템에 판매 금액이 반영됩니다.
출항 인증에서 동행자 선택으로 넘어가고, 선택한 두 사람과 페이드 뒤 배에 탑승합니다.
첫 정착까지 이어지는 조작과 저장은 기존 시스템으로 검증하며 P4 생산 작업은 보류했습니다.

## 2026-09-10 — OPENING-FEEL-001 PASS / local checkpoint 후 STOP

- 기존 PlayerController/CameraController 안에서 가까운 원근 구도와 실제 이동 속도 기반 절차 애니메이션을 연결했다. 카메라 거리 12.5m, pitch 40°, FOV 34°, follow 0.16s; 화면 캐릭터 높이 P0 15.70% / P3 15.68%, 중심은 위에서 약 60%다.
- 전방·인접·명시적 InteractionAnchor·가림 검사를 기존 PlayerInteraction에 적용했다. UI 후 커서 복원, 판매 직후 표시 제거, P0 완료 CTA/Enter, P1 카드 Submit 충돌, 페이드 후 안전한 배 탑승과 낙하 복귀를 교정했다. 기존 Inventory/Shop/PurchaseEvaluator/Economy/Save 권위와 기존 모델·rig를 사용하며 신규 Blender 작업은 없다.
- targeted 증거: 4.20m/s 축·대각선 일치, 2m 셀 0.476s, 정지 거리 0.237m, 충돌 정지 애니메이션, 전방/뒤/거리/장애물, 마우스 UI 10회, 1개 판매→빈 진열대→정확히 7G, P0 마우스·Enter 진입, P1 Enter 확정→P2→P3 배치/NPC 거처 도착. 모션·idle contact sheet 및 기존 실제 1920×1080 캡처를 재사용했다.
- 저장 검증 주의: 보존한 P4 미커밋 Save v16은 빈 firstProduction을 다시 읽을 때 거절한다. 이전 same-state assertion을 복원 PASS 근거로 쓰지 않는다. 이번 commit에 P4 변경을 섞지 않고 eab53b0 + feel 변경만 담은 Logs/OpeningFeel001/CheckpointValidation에서 저장·최종 연속 회귀를 통과했다. 기존 패키지의 동일 버전을 사본 안에서만 file 참조하여 registry timeout을 피한다.
- 최종 D3D11 연속 1회 PASS: P0 이동/열매3/ShopSlot1/가격 확정/PurchaseEvaluator 판매/정확한 금액 → 실제 마우스 CTA → 후보2/Enter 확정 → 페이드/배 → 섬 → 거점1/거처2/NPC 도착 → 기존 SaveManager 저장 후 +11G 변경/Load 원래 잔액 및 정착 상태 복원 → 강제 낙하/정지. runtime errors0, missing component/reference0. 별도 fresh Play에서 같은 상태/동행자2/SaveManager1 복원 PASS.
- Runtime/Editor 최종 dotnet build 오류0, P4 제외 검증 사본 Unity Runtime/Editor compile 오류0. 실제 검증 결과/캡처: Docs/Presentation/2026-09-10/GameFeel/. 전체 실행·compile 로그: Logs/OpeningFeel001/. 새 캡처는 기존 ScreenCapture Game View 경로만 사용한다.
- 사람 확인이 남는 감각: 발 접지와 방향 전환의 세부 리듬, 가까운 카메라에서 건물/돛대 가림, P3 UI의 화면 점유. 자동 검사나 정지 이미지로 사람의 만족도를 확정하지 않는다.
- 이번 staged diff --check PASS. 전체 dirty diff에는 작업 전 P4 프리팹의 공백 4줄이 남아 있으며 보존 요구에 따라 고치지 않았다. 최종 캡처 08만 갱신했고 나머지 실제 Game View와 contact sheet는 재사용했다.
- P4, CONTENT, 개인 .claude/settings.json, Assets/_Recovery와 보류 자산/프리팹은 보존한다. push 금지. 이번 변경만 local checkpoint 후 STOP. P4 보류 dirty는 현재 사용자 작업공간에 남아 있으므로 저장 시 그 별도 결함이 영향을 줄 수 있다. 검증한 상태는 이번 commit과 동일한 P3+feel 사본이다.


## 발표용 최신 요약 — 첫 정착까지 (2026-09-09)

P.A. Company에서 이동·채집·판매를 배운 뒤 생산자 두 명과 섬으로 출발합니다.
자연 상태의 섬에서 첫 관리 거점과 동행자들의 임시 거처를 직접 배치할 수 있습니다.
건물은 격자에 맞춰 회전하거나 옮길 수 있고 나무·바위와 겹치는 설치는 막습니다.
선택했던 광부와 농부가 각자의 거처 앞으로 이동하며 첫 정착지가 만들어집니다.
기존 저장 시스템으로 건물 위치와 동행자 연결을 저장하고 새 플레이에서 복원했습니다.
Blender에서 만든 거점·거처와 실제 Unity 화면으로 무인도에서 정착지로 바뀌는 모습을 확인했습니다.
첫 정착 후에는 첫 야간 영업 준비를 안내하며, 실제 생산과 다음 밤의 플레이는 후속 개발 범위입니다.


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


## 2026-09-09 — VS-PRESENT-001-P3 First Island Settlement PASS

- 기존 WorldBuildingPlacementService에 승인된 거점/거처 정의와 명시적 장애물 검사를 연결했다. 기존 B09 창고 기본값·preview/place/90도 rotation/move를 유지한다. 자연물 자동 삭제 없이 무료 거점 1개와 선택 동행자의 임시 거처 2개를 설치한다.
- P1의 Miner_01/Farmer_01과 P2의 동일 NPC 오브젝트를 재사용한다. 거처 설치 후 기존 NavMeshAgent로 해당 입구까지 이동한다. 3번째 미선택 NPC 없음. 완료 후 현재 목표는 ‘오늘 밤 첫 영업을 준비하세요.’ 한 개이며 P4 실제 생산/First Night는 미구현이다.
- SaveData v15의 선택적 firstSettlement 필드에 기존 WorldPlacedBuildingSaveData를 사용한다. SaveManager/LocalJsonSaveRepository가 건물 3개의 좌표·90도 회전·동행자 연결·완료 상태를 저장/복원한다. Departure 씬은 departure_settlement.json, 기존 캠페인은 savegame.json으로 분리한다. 검증 파일은 Logs/VS_PRESENT_001/P3/ValidationSave/에서만 썼다.
- Blender: 기존 ART-000 라이브러리를 실제 조합한 PA_SettlementHub_Tier0와 PA_StarterShelter, 원본 라이브러리 SHA256 보존. 모델/규격/출처/reference render는 Docs/AssetProvenance/VS_PRESENT_001/P3/derived-assets.json. 신규 외부 자산 없음. 최종 거처 재질만 양면 표시로 교정했으며 모델 재생성 없음.
- 증거는 합산 판정이다. D3D11-02: 실제 P0 채집/진열/가격/판매→P1→P2 도착→P3 거점 배치/회전/이동 PASS. D3D11-06 RunRemaining: 거처2/동행입구 도착/취소/장애물/무료설치/완료/기존 창고 회귀/저장복원/새 Play 재진입 PASS. D3D11-07: 최종 재질 적용 후 저장된 동일 정착지 fresh reentry/참조/캡처 PASS. P0→P3 전체를 하나의 실행으로 통과했다고 주장하지 않는다.
- D3D11-02의 Unity missing-object ?? 처리 오류는 NavMeshAgent null 비교 후 AddComponent로 수정. D3D11-03은 가격창 미열림을 validator가 confirm으로 간주한 P0 fixture timeout. D3D11-04는 진입 전 중단, D3D11-05는 비활성 Editor 시간 진행 문제였다. 자세한 실패와 교정은 BUG_LOG 보존. 검증 중 runInBackground는 임시 true, 종료 시 원래 값 복원. 비정상 종료 복구본은 Assets/_Recovery에 보존, P3 커밋 제외.
- 최종 Runtime→Editor 순차 컴파일 오류0, 기존 CS8785/CS0414 경고. P3 blocking runtime/serialized missing reference/native crash 확인0. diff 공백 검사 PASS. Golden 전체·무관한 CONTENT·standalone·모든 과거 저장 마이그레이션 회귀는 이번 범위에서 확인 못 함.
- 실제 1920×1080 PNG: Docs/Presentation/2026-09-09/P3/01_Island_BeforeSettlement.png, 02_Settlement_PlacementPreview.png, 03_FirstSettlement_Complete.png. 03은 최종 재질의 실제 저장 복원 화면이며 동행자2/거점/거처2/다음목표/복원안내를 포함한다. 동일 구도의 04 중복 캡처는 추가하지 않았다.
- local checkpoint는 이번 P3 코드·프리팹·모델·증거·기존 상태 문서만 포함한다. 개인 .claude/settings.json과 Assets/_Recovery 복구본 제외. push 없음. P4 미시작, 이 checkpoint에서 STOP.

### P3 실행

Unity 메뉴 Project PA > Presentation > Play Companion Selection (Development Entry), 광부·농부 선택 후 출항. 도착 후 오른쪽 거점/거처 버튼 또는 B, 마우스로 위치 선택, R 90도, 클릭/Enter 확정, 취소 버튼/Esc 취소. 설치된 건물 버튼을 다시 누르면 이동한다. F5/정착 저장 버튼으로 저장, 다음 Play의 개발용 동행 선택 화면 오른쪽 위 ‘저장한 정착 이어가기’로 복원한다. 처음부터는 기존 Departure Tutorial의 인증 완료 버튼을 사용한다. 자동 검증은 전용 저장 폴더를 사용했으므로 사용자 저장 슬롯을 미리 채워 두지 않았다.
