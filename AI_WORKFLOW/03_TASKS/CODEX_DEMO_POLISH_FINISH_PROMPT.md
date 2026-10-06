# Project P.A. — Codex 데모 완성(폴리시) 실행 계약

작성: 2026-10-06. 이 파일은 실행 지시이며 읽는 것만으로 승인·소유권이 바뀌지 않는다. 사용자의 직접 시작 메시지가 필요하다.
기존 계약 `AI_WORKFLOW/03_TASKS/CODEX_OPENING_DEMO_FINAL_EXECUTION_PROMPT.md`의 규칙(인수 절차, Studio 예산, 최종 P11 납품, HARD STOP, 기록)을 그대로 상속한다. 이 문서는 그 위에 **완성도 보완 티켓 D0~D12**를 추가한다. 충돌하면 사용자 최신 지시 → AGENTS.md → 이 문서 → 기존 계약 순이다.

## 사용자가 보낼 시작 메시지

아래 첫 문장은 실제로 후보 실행을 종료했을 때만 보낸다.

```text
후보 실행(standalone)을 종료했고 입력 소유권을 Codex에 넘긴다. Claude 자동 재개는 없다. 지금부터 Codex 한 세션이 Project_PA의 Unity 제작을 맡는다.
AI_WORKFLOW/03_TASKS/CODEX_DEMO_POLISH_FINISH_PROMPT.md를 실행 계약으로 적용해 Day 1 데모를 '완성된 게임처럼 보이고 손맛이 있는' 상태로 마감해라. VERTICAL SLICE STUDIO MODE를 활성화한다.
loop-state.json에 새 approval `demoPolishFinishApproval`을 기록하고 D0~D12를 이 순서로 선승인한다(preapprovedThrough=D12, stopAtMilestone=FINAL_WINDOWS_HUMAN_REVIEW, Unity 소유자 Codex). 기존 demoFeedbackFinishApproval의 P1~P11 결과·거절·보류 기록은 보존한다.
티켓마다 Studio 복구 예산(compile/fix 최대 3회, targeted Play 최대 3회, 전용 validator 최대 1개)을 승인한다. 대기 중인 P4 ReelHint·P6 DeviceIsolation 패치 적용도 D0 예산 안에서 승인한다.
허용: 로컬에 이미 있는 에셋(Assets, ExternalAssetSources, Blender/Library)과 Unity에 이미 설치된 패키지(URP, Shader Graph 등) 사용, Editor 도구·Animator 레이어·셰이더·UI 테마 신규 작성. 금지: 외부 패키지/에셋 다운로드, Golden 씬·SmartphoneUI.cs·Save schema·KEEP 권위 재작성, 삭제, commit/push, 경사로 추가.
HARD STOP과 FINAL_WINDOWS_HUMAN_REVIEW 외에는 티켓마다 추가 승인을 묻지 말고 계속 진행해라. 문서나 검증기만 만들고 끝내지 말고 실제 게임 화면을 바꿔라.
```

## 목표

딩컴을 '완성된 생활 게임'의 손맛·가독성 기준으로 삼아, 현재 Day 1 데모(출항 교육 → 동행 선택 → 항해 → Demo256 섬 → 채집·제작·배치 → 밤 영업 → 개척 보고서)를 다음 상태로 만든다.

- 모든 아이템이 같은 화풍의 아이콘을 가진다. 인벤토리·핫바·작업대·상점·보관함의 UI가 한 디자인으로 읽힌다.
- 흔들기·벌목·채광·포획·낚시·줍기·설치·제작에 보이는 동작과 결과 반응(모션, 입자, 소리, 짧은 정지감)이 있다.
- 섬이 테스트 맵이 아니라 바이옴이 구분되는 해안 마을로 읽힌다. 주변 생태(물고기 그림자, 나비, 작은 동물)가 있다.
- 제안서와 달라진 부분 중 Day 1 범위에서 지금 할 수 있는 것을 연결한다(D11).

범위 밖: Day 2 이후, 실제 농사·호미, 새 날씨·계절, 멀티플레이, 매입 가격 협상 같은 새 경제 규칙, 새 Save 필드, 곡면(Curved World) 셰이더 전면 적용.

## 시작 상태 (2026-10-06 확인)

- `loop-state.json`: `demoProductionRouting.selectedApproval=demoFeedbackFinishApproval`, `unityEditorOwner=codex`, `activeTicket=P11_WINDOWS_DELIVERY`. 상태가 **BLOCKED_INPUT_OWNER**(사용자가 후보 PID 82524를 플레이 중)일 수 있다. 시작 메시지의 종료 확인 전에는 그 프로세스에 입력을 보내거나 다시 빌드하지 않는다.
- 대기 패치: `Logs/CodexOpeningDemoFinal/P4-LandFish-20261005-001/PENDING-ReelHint.patch`(낚시 안내문·성공 표시 겹침), `PENDING-DeviceIsolation.patch`(P6).
- 미해결: P9 지지발 미끄러짐·양손 그립(기존 3/3 소진), P10 단차 가독성·빈 광장·NPC 자연 루틴, 실제 오디오 청취 미확인, P11 최종 후보·새 프로세스 이어하기·성능.
- 라이선스: `Assets/Art/animal-crossing-froggy-chair`(근거 없음)가 `Resources/DemoStructure/PioneerShopBase.prefab`, `SimpleFurniture.prefab`, `Prefabs/FroggyChair.prefab`, `Item_SimpleFurniture`의 모델·아이콘에 쓰인다. Free RPG Icons(Bread/Carrot/Wheat/Ore/IronBar 아이콘)는 라이선스 문서가 없다. `Assets/Art/Character/Chop.fbx`는 출처 기록이 없다. 셋 다 데모 화면에서 빼야 한다(파일 삭제 금지, 참조만 교체).

## 티켓 (한 번에 하나, 이 순서)

각 티켓은 시작 전에 파일 목록·기존 권위·플레이어 결과·최소 검사를 짧게 보고한다. 같은 정상 카메라·해상도의 before/after GameView(동작은 정상 속도 영상)를 고유 폴더 `Logs/CodexDemoPolish/<티켓>-<시각>/`에 남긴다.

### D0 — 인수와 대기 패치
1. 기존 계약 §1 인수 절차를 수행한다. 새 approval 기록과 `unityEditorOwner` 정합 외에 별도 큐를 만들지 않는다.
2. P4 ReelHint 패치를 적용한다. `FirstDayFishingMinigame.Build`의 HowTo 텍스트·폰트·y·높이만 바꾸고, 기존 P4 Fishing Review를 1회 실행해 릴 패널과 결과 표시를 확인한다.
3. P6 DeviceIsolation 패치의 의도와 diff를 읽고 적용한다. 가방/Q2 GameView를 확인한다.

### D1 — 아이템 아이콘 통일 (Icon Bake)
- 현재 상태:
  - `InventorySlotUI`·`HotbarUI`는 `Item.icon`을 쓴다.
  - `Resources/Items` 22개 중 8개가 icon null이다. 작업대 레시피 12개의 `RecipeData.icon`은 모두 null이다.
  - 기존 아이콘은 `PA_FirstDayStudioSetup.PrepareItemIcons`의 AssetPreview(128px, 불투명 회색 배경, 어두운 조명)와 Free RPG Icons가 섞여 있다.
- 구현:
  - `Assets/Editor/`에 Icon Bake 도구를 만든다. 사양: 직교 카메라, 투명 RenderTexture, 따뜻한 3점 조명 + 림 라이트, 공통 3/4 각도, 256px, 여백 자동 맞춤.
  - 각 아이템의 로컬 모델(prefab/model 참조)로 전부 다시 굽고 `Item.icon`, `RecipeData.icon`에 연결한다.
  - Free RPG Icons와 AssetPreview 아이콘은 교체한다. 모델이 없는 아이템은 같은 화풍의 대체 모델로 만들거나 목록으로 보고한다.
- 검사:
  - 핫바·인벤토리·작업대·보관함·진열대 가격 UI에서 모든 데모 아이템이 같은 화풍으로 보이는가.
  - 아이템 ID·이름·가격·스택 규칙이 그대로인가.

### D2 — UI 테마 통일
- 현재 상태:
  - `SmartphoneUI`의 Cream/Ink/Teal 토큰과 `RoundedSprite`를 작업대·인벤토리·보관함·가격·보고서·휴대폰·피드·낚시 UI가 쓴다.
  - `FirstDayHudStyle`은 섬에서만 시계·돈·프롬프트·핫바 테두리를 바꾼다.
  - `HotbarUI`, `InventorySlotUI`, `NpcBubbleUI`, `DialogueUI`, `PauseManager`/`SettingsUI`, Departure 교육·동행 선택·항해 UI는 각자 팔레트를 쓴다.
- 구현:
  - `PAUiTheme`(색, 크기, 패널·슬롯·버튼·툴팁 팩토리)를 새로 만든다. 토큰은 SmartphoneUI 값을 읽는다. 보호 파일 `SmartphoneUI.cs`는 수정하지 않는다.
  - 데모 경로 UI를 이 테마로 옮긴다.
  - 슬롯 공통 규칙: 수량 배지 위치, 선택 하이라이트, 내구도 바, 비활성 상태, 마우스 오버 툴팁(이름·설명·가격).
  - 폰트는 기존 Jalnan2_SDF + 이모지 폴백을 유지한다.
- 검사: 교육부터 보고서까지 열리는 모든 패널을 한 장씩 캡처해 색·여백·모서리·글자 크기를 비교한다. 1920×1080과 1280×720에서 겹침·잘림이 없어야 한다.

### D3 — 작업대 제작 UI
- 현재 상태:
  - `CraftingUI.cs`(729줄)는 카드 한 줄 스크롤이고, 재료는 글자로만 보이며(`BuildSlotLabel`, `BuildIngredientText`), 한 번 클릭하면 바로 제작된다.
  - 데모에 없는 잠긴 레시피까지 전부 보인다.
- 구현: `CraftingService` 권위를 유지하고 화면만 바꾼다.
  - 왼쪽에 아이콘 그리드(카테고리 탭), 오른쪽에 상세 패널을 둔다. 상세 패널: 결과 아이콘 크게, 재료 아이콘 칩과 보유/필요 수, 수량 ±, E 길게 눌러 제작.
  - 키보드(WASD/화살표/E/Esc)와 마우스를 모두 지원한다.
  - 데모 밖 레시피는 숨긴다. 재료 부족은 회색과 부족한 재료 강조로 보인다.
  - 제작 완료 시 결과 아이콘이 가방으로 날아가는 짧은 연출과 소리를 넣는다.
- 검사: 나무 → 판재(D11 연결), 개선 곡괭이, 가판대/키트 제작을 실제 입력으로 수행한다. 재료 차감, 결과 지급, 가득 찬 가방 처리가 기존과 같아야 한다.

### D4 — 나무 흔들기와 자원 반응
- 현재 상태:
  - `DaytimeStockPrepPoint.ShakeFruit()`는 자기 transform을 ±3° 돌린다. 하지만 교육 씬에서 그 transform은 트리거만 있는 `TrainingFruitTree` 앵커다. 보이는 나무(`ULTIMATENATURE_COMMONTREE_1`)와 `VisibleTrainingFruit_*`는 형제 오브젝트라서 흔들림이 보이지 않는다(`PA_DepartureTutorialBuilder.cs` 112~136).
  - 열매는 즉시 꺼진다(`DepartureTutorialController.ApplyStagePresentation`).
  - 섬에는 흔들 나무가 없다.
  - 벌목·채광은 10% 크기 펄스 뒤 렌더러가 꺼질 뿐이다(`Gatherable.RefreshDirectState`, `MiningSpot`).
- 구현:
  - 흔들기: 보이는 나무를 `shakeVisual`로 직렬화 연결한다. 뿌리 기준 감쇠 스프링 흔들림, 잎 입자를 넣는다. 열매는 매달린 위치에서 떨어지게 하고(간단한 물리/튀김) 기존 `PickupItem`/Inventory 경로로 줍는다.
  - 섬의 숲·초원에 흔들 수 있는 열매 나무를 배치한다. 보상은 기존 일일 보상/줍기 권위를 쓰고 새 보상 권위를 만들지 않는다.
  - 벌목: 맞을 때 기울기 흔들림 → 쓰러짐 트윈 → 그루터기(`TreeStump`/`TreeStump_Moss`) 교체를 넣는다.
  - 채광: 균열 단계 2~3개 → 파편 → 잔해를 넣는다. 소진·영속 키(persistence key)는 그대로 둔다.
- 검사: 교육 흔들기, 섬 흔들기, 벌목 3타, 채광 3타를 정상 속도 영상으로 남긴다. 저장·이어하기 후 소진 상태가 유지돼야 한다.

### D5 — 캐릭터 액션 애니메이션
- 현재 상태:
  - `Resources/PlayerLocomotion/PlayerLocomotion.controller`는 UAL1 이동 7클립만 쓴다.
  - 도구 동작은 `PlayerLocomotionAnimator`(197~237행)의 절차적 팔 키프레임이다.
  - NPC는 `NpcHumanoidProceduralAnimator`를 쓴다.
  - `Assets/Art/External/Quaternius/UniversalAnimationLibrary/UAL1_Standard.fbx`에 클립으로 가져오지 않은 동작이 약 45개 있다(Interact, PickUp_Table, Fixing_Kneeling, Idle_Talking_Loop, Sitting, Punch/Sword 계열 등).
- 구현:
  - Editor ModelImporter 스크립트로 필요한 동작을 클립으로 가져온다.
  - 상체 AvatarMask 액션 레이어와 트리거를 만든다: Shake, PickUp, Place, Interact/Craft, Talk.
  - 도구 휘두르기는 기존 그립을 유지하면서 상체 클립과 섞어, 몸통이 함께 움직이게 한다.
  - 동행 광부의 작업 동작이 실제 채굴로 보이게 한다. 대화 중에는 Idle_Talking을 쓴다.
  - 이동 수치·카메라·입력 권위는 바꾸지 않는다. 기존 사용자 반려 기록(이동 모션)을 수락으로 바꾸지 않는다. 이 티켓에서 지지발 미끄러짐을 다시 다룰 때는 새 예산 안에서 원인과 비교 영상을 남긴다.
  - `Chop.fbx`(출처 없음)는 쓰지 않는다.
- 검사:
  - 맨손과 도끼·곡괭이·낚싯대·잠자리채로 각각 흔들기·줍기·설치·제작·대화를 한다.
  - 정면/측면 정상 속도 영상으로 확인한다. 기준: 팔·도구 관통 없음, 발 접지.

### D6 — 도구 손맛
- 현재 상태: `GatherFeedback.cs`에 합성 효과음, 모델 조각 6개, 드롭 팝이 있다. 프로젝트 전체에 ParticleSystem이 없다. 잘못된 대상 반응은 `FirstDayToolSurface.React`가 맡는다.
- 구현:
  - 코드로 생성하는 입자를 넣는다: 나뭇조각, 돌 불꽃·먼지, 물 튀김, 잎, 곤충 포획 반짝임.
  - 타격 순간 40ms 정지감을 넣는다. 카메라 반동은 `CameraController` 교체 없이 가산 오프셋으로만 준다.
  - 효과음은 타격·소진·포획·낚시 성공 변주로 나눈다.
  - 내구도 감소 표시를 넣는다(D2 슬롯과 일치).
  - 허공 사용 반응(P9 계약)과 잘못된 대상 반응은 그대로 유지한다.
- 검사: 같은 각도에서 정상 속도 전후 영상을 만든다. 자원·내구도 수치가 기존과 같아야 한다.

### D7 — 지형 마감 (경사로 금지)
- 현재 상태:
  - `WorldGeneratedIslandDebugView.CreateMaterials`는 단색 URP/Lit 8종이고 텍스처가 없다.
  - 1m 단차가 갈색 줄로 보이고, 길이 도랑처럼 보이며, 넓은 광장이 비어 있다.
  - 물은 불투명 평면이다.
- 구현:
  - 사용자 지시에 따라 블록 단차와 점프는 유지하고 경사로는 넣지 않는다.
  - 절벽 면: 지층 재질과 위쪽 잔디 가장자리(lip) 스트립 메시를 넣는다.
  - 지면: 노이즈 텍스처 또는 버텍스 컬러로 변화를 준다. 길은 가장자리 처리로 도랑처럼 보이지 않게 한다.
  - 해안선: 거품 링, 모래→잔디 전이, Shader Graph 물 셰이더(얕은 곳 색, 거품, 약한 물결)를 넣는다.
  - 광장은 바이옴 드레싱(D8)으로 채운다.
  - 셀·시드·저장 ID·배치 앵커·sparse delta 계약은 바꾸지 않는다. 기존 계약 §3의 금지선을 그대로 지킨다.
- 검사: 오전 9시, 노을, 밤의 정상 카메라에서 같은 지점 전후 사진을 찍는다. 채집 대상·건물·NPC 가림이 없어야 하고, 기존 이동 경로·NavMesh 연결이 유지돼야 한다.

### D8 — 바이옴 드레싱
- 현재 상태: `FirstDayWorldPresentation.ComposeDressing`(134~179행)은 3셀 격자에 나무 5·바위 4·풀 3·꽃 1·덤불 2종만 둔다. 숲과 고지는 항구 기준 방향으로 정하고 `biomeCell.Biome`을 쓰지 않는다. 가파른 셀과 해안 셀은 건너뛴다.
- 구현: 바이옴 값으로 드레싱을 고른다(로컬 Ultimate Nature 우선).

| 바이옴 | 드레싱 |
|---|---|
| 해안 | 야자, 유목, 조개·돌 |
| 연못·강 | 버드나무, 수련, 갈대 |
| 숲 | CommonTree, BushBerries, 버섯, 통나무 |
| 초원 | 꽃, Plant_1~5 |
| 고지 | 이끼 바위, 소나무 |

  - 절벽 아래 바위, 해안선 가장자리를 채운다.
  - GPU instancing을 쓰고, 채집 대상·동선·NPC 경로·배치 구역을 막지 않는다. 크기·색은 하나의 화풍으로 맞춘다.
- 검사: 섬 주요 7개 지점 사진과 실제 보행으로 주요 활동 발견 간격 10~20초를 확인한다. 기존 P10 경로 Complete가 유지돼야 한다.

### D9 — 주변 생태 엔티티
- 현재 상태: 항구 근처 고정 위치의 `BugCritter` 나비 3마리뿐이다(`WorldGameplayAdapterService` 557~565행). 물고기는 낚시 지점에만 있고, 동물은 없다. 새는 소리(`AudioManager.BirdPhrase`)뿐이다.
- 구현: 보상과 무관한 주변 생태를 넣는다.
  - 물가: 물고기 그림자(CuteFish 모델 실루엣)
  - 초원: 바이옴별 나비 무리
  - 숲·초원: 작은 동물 몇 마리. `ExternalAssetSources`의 CubeWorld 동물은 Idle/Peck/Run 클립이 있다. 화풍이 Ultimate Nature와 맞는지 먼저 비교하고, 사용 범위를 `Docs/AssetProvenance/EXTERNAL_ASSET_REGISTRY.md`에 기록한다.
  - 하늘: 원거리 새 몇 마리
  - 새 보상·인벤토리 권위를 만들지 않는다. 생태는 세션 한정, 저장 없음. 개체 수와 성능을 확인한다.
- 검사: 바이옴별 사진. 플레이어 접근 시 반응(도망·날아감) 영상. 프레임 시간 전후 비교.

### D10 — 상점 내부와 라이선스 정리
- 개구리 의자를 로컬 CC0 또는 Blender 스크립트로 만든 의자로 교체한다. 대상: `PioneerShopBase.prefab`, `SimpleFurniture.prefab`, `Item_SimpleFurniture`의 모델·아이콘. 파일은 지우지 않고 참조만 바꾼다.
- 상점 실내 조명, 진열대 가격표, 손님 동선을 D2·D5 기준으로 다듬는다.
- 교체 후 데모 경로 전체에서 `animal-crossing-froggy-chair`, Free RPG Icons, `Chop.fbx` 참조가 0인지 검색 결과로 남긴다.

### D11 — 제안서 정합 보강 (Day 1 범위, 새 권위 없이)
- 가공으로 가치 상승:
  - 데이터상 나무 8G, 판재 25G다. 섬 작업대에서 나무 → 판재 제작이 실제로 가능한지 확인하고, 안 되면 기존 `Recipe_Plank`를 데모 레시피 목록에 연결한다.
  - 판재를 진열해 관광객에게 파는 경로를 실제 입력으로 확인한다. 개척 보고서 상업 점수와의 연결은 기존 계산 그대로 둔다.
- NPC 성향 표현:
  - 동행 대사와 대기 행동에 `NpcProfile` 성향을 반영한다(T·F 대사 톤, E·I 대기 위치·말 걸기 빈도).
  - 표현 계층만 바꾸고 구매·생산 계산과 FSM 권위는 그대로 둔다.
- 생산 표시:
  - 동행 광부의 생산 진행(다음 묶음까지 남은 시간, 도구 효율)을 동행 프롬프트나 휴대폰에서 읽히게 한다.
  - 데모 정책(첫 묶음 1회 수령, `STARTER_CLAIM_ONCE`)은 바꾸지 않는다.
- 하지 않음: 매입 가격 협상, 티어·감사 연결, 멀티플레이, 곡면 셰이더. 필요하면 별도 승인 후보로만 보고한다.

### D12 — 오디오 확인과 최종 납품
- 실제 출력으로 오디오를 확인한다(녹음 또는 사람 청취 기록). 코드에 AudioSource가 있다는 것만으로 PASS를 기록하지 않는다.
  - 바이옴·시간대 환경음, UI 소리를 일관되게 맞춘다.
- 기존 계약 '최종 P11' 1~5를 그대로 수행한다:
  - 새 후보에서 순간이동·지급·시간 가속 없는 완주
  - 새 프로세스 이어하기
  - 성능
  - 해시·HEAD+dirty 식별
- 최종 후보를 지정하고 FINAL_WINDOWS_HUMAN_REVIEW에서 멈춘다.

## 레퍼런스

- 기존 `Logs/CodexParallel/SteamDemoQuality/ReferenceReview-20261005-001/REFERENCE_AUDIT.md`와 로컬 딩컴 영상부터 쓴다.
- 티켓마다 관찰 요소 3~5개 → 현재 차이 → 이번 구현 → 동일 조건 전후 비교를 증거에 남긴다.
  - 딩컴 기준: 흔들기·벌목 쓰러짐·채광 파편·도구 타격 손맛·아이콘·작업대 UI·바이옴 밀도
  - 동물의 숲 기준: 마을 생활감·주민 반응·UI 정보 위계
- 캐릭터·아이콘·모델·UI 그래픽을 복제하지 않는다. 외부 영상은 관찰만 하고 내려받거나 가져오지 않는다.

## 제작·검증 규율

- 한 번에 경험 하나: 관련 수정 → compile → 영향 검사 → 실제 제품 입력 → GameView → 필요한 수정. 확인되지 않은 원인을 연달아 패치하지 않는다.
- 기존 권위를 재사용한다. Player/Inventory/Hotbar/Economy/Shop/World/Placement/Save 권위를 새로 만들지 않는다. 표현 계층(시각·입자·소리·애니메이션·UI)만 추가한다.
- 아이템·레시피 .asset의 ID·이름·가격·스택은 바꾸지 않는다(D11의 레시피 연결은 예외이며 이유를 기록한다). 아이콘·모델 참조만 바꾼다.
- 결과를 세 가지로 따로 기록하고, 숫자 PASS로 시각 결함을 덮지 않는다:
  - 기능 PASS
  - presentation STRUCTURALLY IMPROVED / FAIL / UNVERIFIED
  - human ACCEPTED / REJECTED / UNVERIFIED
- 예산 소진, 같은 blocker 2회, 저장 계약 변경 필요, 보호 파일 수정 필요, 다운로드 필요 시 HARD STOP하고 정확한 다음 행동을 한 번 기록한다.
- 티켓 완료마다 `Docs/04_DEVELOPMENT_LOG/2026-10.md`에 한 번 기록하고, 바뀐 현재 문서의 해당 부분만 갱신한다. 중단 시 `CODEX_HANDOFF.md`를 덮어써 현재 티켓·예산·다음 행동을 남긴다.
- 성능: 바이옴 드레싱·생태·입자 추가 전후로 같은 경로의 프레임 시간을 비교한다. 로딩 프레임은 따로 표시한다.
- 끝까지 실제 도달한 상태만 보고한다. 사람 검수 전에는 RELEASE ACCEPTED를 선언하지 않는다.
