# PROJECT_CODE_INDEX — Assets/Scripts 구조 인덱스

작성: 2026-07-10
작업: Task 001 - 프로젝트 구조 인덱스 작성
범위: `Assets/Scripts/**/*.cs`

## 작성 기준

- 실제 파일 목록은 `rg --files Assets/Scripts -g "*.cs"` 및 `Get-ChildItem Assets/Scripts -Recurse -Filter *.cs`로 확인했다.
- 각 파일은 `Get-Content`/`rg`로 선언부와 주석 신호를 읽고 1줄 역할로 요약했다.
- 확실하지 않은 역할은 문장 안에 `추정`으로 표시했다.
- 이번 문서는 길찾기용 인덱스이며 코드 동작 검증 문서가 아니다.

## 수량 대조

- `Assets/Scripts` 실제 C# 파일 수: 100
- 이 문서에 분류한 파일 수: 100
- `Assets/Scripts` 아래 Editor 전용 `.cs` 파일: 없음

## 플레이어

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/CameraController.cs` | 플레이어 추적 카메라와 워프 후 카메라 스냅을 담당한다. |
| `Assets/Scripts/EquipmentSystem.cs` | 플레이어가 손에 든 아이템/장비 모델 표시를 담당한다. |
| `Assets/Scripts/PlayerController.cs` | 플레이어 이동, 회전, 앉기/일어서기, UI 오픈 중 이동 차단을 담당한다. |
| `Assets/Scripts/PlayerFootIkStabilizer.cs` | 플레이어 모델의 런타임 발 위치 보정 레이어다. |
| `Assets/Scripts/PlayerInputHandler.cs` | 이동/상호작용/인벤토리/폰/저장/로드 등 입력 이벤트의 단일 진입점이다. |
| `Assets/Scripts/PlayerInteraction.cs` | 전방/근처 `IInteractable` 탐색, 프롬프트 표시, 상호작용 실행을 담당한다. |

## 인벤토리

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/DragContext.cs` | 인벤토리/핫바 UI 드래그 중인 슬롯 상태를 공유하는 정적 컨텍스트다. |
| `Assets/Scripts/Hotbar.cs` | 플레이어 핫바 슬롯 목록과 선택 인덱스를 관리한다. |
| `Assets/Scripts/HotbarUI.cs` | 핫바 슬롯 표시, 선택 표시, 입력과 UI 갱신을 담당한다. |
| `Assets/Scripts/Inventory.cs` | 플레이어 인벤토리 싱글톤, 슬롯 초기화, 아이템 추가/제거, UI 갱신을 담당한다. |
| `Assets/Scripts/InventorySlot.cs` | 인벤토리/핫바 한 칸의 `ItemInstance` 저장과 수량 변경을 담당한다. |
| `Assets/Scripts/InventorySlotUI.cs` | 인벤토리/핫바 슬롯 UI의 드래그, 드롭, 툴팁, 슬롯 교환을 담당한다. |
| `Assets/Scripts/InventoryUI.cs` | 인벤토리 패널 열기/닫기와 슬롯 UI 갱신을 담당한다. |
| `Assets/Scripts/Item.cs` | 아이템 ScriptableObject 정의, 도구 타입, 경제 카테고리, 건설/농사 연결 데이터를 담는다. |
| `Assets/Scripts/ItemInstance.cs` | 실제 스택의 수량, 품질, 현재 가격 등 동적 상태를 담는다. |
| `Assets/Scripts/ItemPickupHandler.cs` | 월드 아이템 픽업 처리로 인벤토리에 아이템을 넣는 컴포넌트다. |
| `Assets/Scripts/ItemRegistry.cs` | Item ScriptableObject 도감 역할로 저장 복원 시 id/name 기반 아이템 조회를 제공한다. |
| `Assets/Scripts/ItemTooltip.cs` | 아이템 UI 툴팁 표시를 담당한다. |
| `Assets/Scripts/PickupItem.cs` | 플레이어 충돌/트리거 기반으로 월드 아이템을 획득시키는 픽업 컴포넌트다. |

## 상점

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Shop.cs` | 상점 건물과 자식 `ShopSlot` 등록/조회, 일괄 판매 호환 흐름을 담당한다. |
| `Assets/Scripts/ShopOpenSign.cs` | 플레이어가 밤 영업을 시작하는 간판 상호작용을 담당한다. |
| `Assets/Scripts/ShopSlot.cs` | 진열, 가격 표시, NPC 구매, 판매 기록, 월드 진열 모델/라벨 생성을 담당한다. |
| `Assets/Scripts/UI/ShopPriceUI.cs` | 진열 상품의 가격 조정/확정/회수 UI와 NPC 반응 힌트를 담당한다. |
| `Assets/Scripts/CustomerArrivalController.cs` | 영업 시작 후 손님을 자연스럽게 상점으로 초대하는 사이드카다. |

## 경제

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/EconomyService.cs` | 공유 지갑, 돈 변경, 누적 매출 이벤트의 단일 경로다. |
| `Assets/Scripts/PurchaseEvaluator.cs` | NPC 성향, 가격, 품질, 카테고리 기반 구매 판단 순수 함수다. |
| `Assets/Scripts/SaleRecord.cs` | 단일 판매 이력 DTO다. |
| `Assets/Scripts/SalesLogManager.cs` | 판매 이력 싱글톤이며 최근 판매 기록을 제공한다. |
| `Assets/Scripts/CustomerDemandInsightController.cs` | 구매 평가 결과를 관찰해 수요 인사이트를 표시하는 읽기 전용 레이어다. |

## NPC

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/DialogueData.cs` | NPC 대사 풀 ScriptableObject와 대화 주제 enum을 정의한다. |
| `Assets/Scripts/DialogueService.cs` | NPC 프로필/주제/친밀도 기반 대사 선택 헬퍼다. |
| `Assets/Scripts/HiringService.cs` | 고용 가능한 NPC 후보, 고용 비용, 런타임 고용 NPC 목록을 관리한다. |
| `Assets/Scripts/NpcCandidateData.cs` | 고용 후보 NPC 정적 데이터 ScriptableObject다. |
| `Assets/Scripts/NpcController.cs` | 주민 소비자 FSM, 배회, 상점 방문, 구매 평가/피드백을 담당한다. |
| `Assets/Scripts/NpcDailySchedule.cs` | NPC 하루 스케줄 페이즈 데이터 ScriptableObject다. |
| `Assets/Scripts/NpcDialogue.cs` | NPC와의 상호작용 대화 진입점이며 친밀도/주제 대사를 연결한다. |
| `Assets/Scripts/NpcHumanoidProceduralAnimator.cs` | NPC 휴머노이드 모델의 절차적 애니메이션을 담당한다. |
| `Assets/Scripts/NpcPresentationNormalizer.cs` | NPC 모델 프레젠테이션/애니메이션 상태를 런타임에 정규화한다. |
| `Assets/Scripts/NpcProfile.cs` | NPC 이름, MBTI 성향, 소비 성향 등 정체성 데이터를 담는 ScriptableObject다. |
| `Assets/Scripts/NpcScheduleController.cs` | `GameClock`에 맞춰 NPC의 쇼핑/근무/휴식 스케줄을 조율한다. |
| `Assets/Scripts/NpcSpecialty.cs` | NPC 전문 분야 enum과 관련 표시/분류 헬퍼다. |
| `Assets/Scripts/ProducerNpcController.cs` | 생산형 NPC가 자원을 생산하고 납품하는 FSM을 담당한다. |
| `Assets/Scripts/ProductionData.cs` | 생산형 NPC가 만들 수 있는 품목 명세 ScriptableObject다. |
| `Assets/Scripts/SpecialistNpcController.cs` | 전문가 NPC가 작업대에서 레시피를 자동 가공하는 FSM을 담당한다. |
| `Assets/Scripts/SeasonModifier.cs` | 계절별 생산/전문가 작업 보정값을 제공한다. |

## 낮밤 루프

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/DayNightShopLoopController.cs` | DayPreparation/ShopOpen/Settlement 페이즈, 낮 재고 준비, Day 1 상점 게이트를 담당한다. |
| `Assets/Scripts/DayNightVisual.cs` | `GameClock` 시간에 맞춰 낮밤 조명/시각 변화를 반영한다. |
| `Assets/Scripts/DaytimeStockPrepPoint.cs` | 낮에 상호작용해 재고 아이템을 얻는 준비 포인트다. |
| `Assets/Scripts/LongPlayProgressionController.cs` | Day 2 이후 장기 진행용 공급/수익 목표 사이드카다. |
| `Assets/Scripts/Services/GameClock.cs` | 시간, 일차, 계절, 시간 이벤트를 제공하는 게임 시계 서비스다. |

## 마을 변화

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/VillageChangeSignalController.cs` | 최근 판매 카테고리별 통계를 읽어 마을 변화 방향 신호를 UI로 표시한다. |
| `Assets/Scripts/VillageCultureVisualController.cs` | Processed 판매 후 다음 낮 페이즈에 시장 주변 시각 변화를 생성하는 런타임 사이드카다. |

## 저장

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/SaveData.cs` | 저장 스키마 DTO이며 현재 v8 필드들을 담는다. |
| `Assets/Scripts/SaveManager.cs` | 저장/로드, v0~v8 마이그레이션, 인벤토리/상점/건물/NPC 상태 복원을 담당한다. |
| `Assets/Scripts/Services/ISaveRepository.cs` | 저장 백엔드 추상화 인터페이스다. |
| `Assets/Scripts/Services/LocalJsonSaveRepository.cs` | `Application.persistentDataPath` 기반 로컬 JSON 저장소 구현이다. |

## 활동 / 제작 / 건설 / 월드

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/BuildingData.cs` | 건설 가능한 건물 ScriptableObject 정의다. |
| `Assets/Scripts/BuildingEntrance.cs` | 같은 씬 안에서 실내/실외 워프를 처리하는 건물 입구 상호작용이다. |
| `Assets/Scripts/BuildManager.cs` | 건설 모드, 고스트 배치, 비용 지불, 그리드 점유 등록을 담당한다. |
| `Assets/Scripts/Chair.cs` | 앉을 위치와 점유 여부를 가진 월드 상호작용 보조 컴포넌트다. |
| `Assets/Scripts/CraftingService.cs` | 레시피/작업대/티어/재료 검사를 거쳐 가공 결과를 생성하는 단일 진입점이다. |
| `Assets/Scripts/CraftingUI.cs` | 작업대 또는 C키로 여는 가공 패널 UI다. |
| `Assets/Scripts/Crop.cs` | 작물 성장 단계 표시와 수확 처리를 담당한다. |
| `Assets/Scripts/Farmland.cs` | 밭 상태와 작물 배치를 관리한다. |
| `Assets/Scripts/Gatherable.cs` | 채집 가능한 월드 오브젝트 상호작용을 담당한다. |
| `Assets/Scripts/GridService.cs` | 건설 배치용 월드 좌표/그리드 좌표 변환과 점유맵을 관리한다. |
| `Assets/Scripts/HiddenBlueprintData.cs` | 친밀도 조건으로 해금되는 히든 레시피 데이터를 담는다. |
| `Assets/Scripts/ProcessingOpportunityController.cs` | 판매/레시피 데이터를 읽어 가공 기회 힌트를 제공하는 관리형 사이드카다. |
| `Assets/Scripts/RecipeData.cs` | 가공 레시피, 재료, 산출물, 작업대 요구 조건을 정의한다. |
| `Assets/Scripts/Services/BuildingRegistry.cs` | 건설된 건물 목록의 source of truth이며 저장/로드가 참조한다. |
| `Assets/Scripts/StorageBox.cs` | 월드 보관함의 아이템 목록과 상호작용을 담당한다. |
| `Assets/Scripts/StorageUI.cs` | 보관함 UI 열기/닫기, 아이템 표시/이동을 담당한다. |
| `Assets/Scripts/Workbench.cs` | 월드 작업대 상호작용 진입점이며 `CraftingUI`를 연다. |

## 티어 / 감사 / 관계 성장

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/AuditService.cs` | 주기적 감사 조건 평가와 Tier 수동 승급 시도를 담당한다. |
| `Assets/Scripts/AuditResultUI.cs` | 스마트폰 감사 앱의 매출/다음 티어/감사 카운트다운 UI다. |
| `Assets/Scripts/FriendshipService.cs` | NPC 친밀도 점수와 일일 대화 제한 상태를 저장/조회하는 서비스다. |
| `Assets/Scripts/FriendshipUI.cs` | 대화 UI에 친밀도 레벨/진행도를 표시한다. |
| `Assets/Scripts/TierDefinition.cs` | 등급별 요구 매출/평판/해금 설명을 담는 ScriptableObject다. |
| `Assets/Scripts/TierService.cs` | 누적 매출/평판 기반 티어 진행, 수동 승급, 슬롯 해금을 담당한다. |

## UI / 프레젠테이션 / 오디오

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/AudioManager.cs` | BGM 크로스페이드와 SFX 풀링을 제공하는 오디오 싱글톤이다. |
| `Assets/Scripts/CoreSlicePresentationMode.cs` | Day 1~3 코어 슬라이스용 표시 필터로 검증/조언 오버레이 노출을 조절한다. |
| `Assets/Scripts/FeedUI.cs` | 스마트폰 피드 탭 내용을 표시한다. |
| `Assets/Scripts/PauseManager.cs` | ESC 일시정지, 설정 패널, 커서 상태를 관리한다. |
| `Assets/Scripts/SettingsUI.cs` | 스마트폰 설정 탭의 BGM/SFX 볼륨 슬라이더를 담당한다. |
| `Assets/Scripts/UI/ClockHUD.cs` | 화면 시각/날짜/계절 HUD를 표시한다. |
| `Assets/Scripts/UI/CustomerPreferencePresentationController.cs` | NPC 성향/선호를 플레이어에게 요약하는 읽기 전용 프레젠테이션 레이어다. |
| `Assets/Scripts/UI/DialogueUI.cs` | 하단 대화창, 타이핑 출력, 자동 숨김을 담당한다. |
| `Assets/Scripts/UI/HiringUI.cs` | 스마트폰 채용 탭의 후보 카드와 고용 버튼을 표시한다. |
| `Assets/Scripts/UI/InteractPromptUI.cs` | 상호작용 가능한 오브젝트 근처의 `[Space]` 프롬프트를 표시한다. |
| `Assets/Scripts/UI/InventoryAnchorFollower.cs` | 인벤토리 패널을 플레이어 머리 위 스크린 위치에 붙인다. |
| `Assets/Scripts/UI/MoneyHUD.cs` | 돈, 티어, 다음 목표 HUD를 표시한다. |
| `Assets/Scripts/UI/NpcBubbleUI.cs` | NPC 머리 위 대사/구매 반응 말풍선을 표시한다. |
| `Assets/Scripts/UI/PlayableDayScenarioController.cs` | 첫날 온보딩, 목표 진행, 첫 판매 루트, 저장 목표, 요약 화면을 관리한다. |
| `Assets/Scripts/UI/PrototypeWorldLabel.cs` | 월드 공간 안내 라벨을 생성/표시하는 공용 컴포넌트다. |
| `Assets/Scripts/UI/PurchaseFeedbackPresentationController.cs` | 구매/거절 이유와 마을 변화 연결 문구를 표시하는 읽기 전용 패널이다. |
| `Assets/Scripts/UI/ScreenFader.cs` | 워프 등에서 사용하는 페이드 아웃/인 시퀀스를 제공한다. |
| `Assets/Scripts/UI/SmartphoneUI.cs` | P키/호버 기반 스마트폰 UI, 홈/앱 탭/닫기 상태를 관리한다. |

## 코어 / 런타임 연결

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/GameManager.cs` | 과거 돈 관리에서 `EconomyService`로 이관된 뒤 남은 호환/초기화성 관리자다. |
| `Assets/Scripts/IInteractable.cs` | 플레이어 상호작용 대상이 구현해야 하는 공통 계약이다. |
| `Assets/Scripts/PA_RuntimeSceneBinder.cs` | 씬 로드 후 핵심 서비스, UI, NPC 훅, 플레이어 훅을 런타임에 보강하는 안전망이다. |

## 에디터

- `Assets/Scripts` 아래에는 Editor 전용 `.cs` 파일이 없다.
- `Assets/Editor`는 이번 Task 001의 `Assets/Scripts` 인덱스 범위 밖이므로 분류하지 않았다.

## 다음에 주의할 지점

- `SaveManager.cs`, `SaveData.cs`, `ShopSlot.cs`, `Shop.cs`, `EconomyService.cs`, `PurchaseEvaluator.cs`, `NpcController.cs`, `PlayerController.cs`, `PlayerInteraction.cs`, `Inventory.cs`는 프로젝트 규칙상 위험 파일이다.
- `PA_RuntimeSceneBinder.cs`는 런타임 자동 연결을 많이 담당하므로 씬/프리팹을 건드리지 않는 작업에서도 영향 범위를 먼저 확인해야 한다.
- `Resources.Load*`를 쓰는 파일이 많아 `Assets/Resources/**` 경로/이름 변경은 코드 수정 없이도 런타임 연결을 깨뜨릴 수 있다.
