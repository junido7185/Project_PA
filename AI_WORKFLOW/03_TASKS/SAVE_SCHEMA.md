# SAVE_SCHEMA — Project P.A. 저장 v8 현황

작성: 2026-07-13 (Codex)
근거: `Assets/Scripts/SaveData.cs`, `SaveManager.cs`, `Services/ISaveRepository.cs`, `Services/LocalJsonSaveRepository.cs`

## 현재 규약

- 현재 버전: `SaveManager.CurrentSaveVersion = 8`
- 키: `savegame`
- 로컬 경로: `Application.persistentDataPath/savegame.json`
- 형식: `JsonUtility` JSON
- 입력: `PlayerInputHandler.OnSave`/`OnLoad` (현재 안내 키 F5/F9)
- 백엔드: `ISaveRepository`; 현재 구현은 `LocalJsonSaveRepository`
- 스키마 정책: 필드 제거·개명 없이 추가 확장하고 버전 증가와 단계별 마이그레이션을 함께 추가한다.

## SaveData 최상위 필드

| 영역 | 필드 | 저장/복원 경로 |
|---|---|---|
| 버전 | `version` | 저장 직전 v8 스탬프, 로드 시 `MigrateSaveData` |
| 경제 | `money`, `cumulativeRevenue` | `EconomyService` ForceSet 계열 |
| 플레이어 | `playerPosition` | CharacterController를 잠시 끄고 위치 복원 |
| Day 1 프로필 | `playerName`, `selectedMapId`, `firstDayPrototypeStage` | `PlayableDayScenarioController` |
| 티어 | `currentTier`, `reputation` | `TierService.ForceSetTier` |
| 시간 | `gameHour`, `gameDay` | `GameClock.ForceSet`; 마이그레이션 분기 밖에서 항상 복원 |
| 장기 진행 | `longPlayLastSupplyDay`, `longPlayDayStartRevenue`, `longPlayDayStartMoney` | `LongPlayProgressionController` |
| 감사 | `lastAuditDay` | `AuditService` |
| 친밀도 | `friendshipData` | points + lastDialogueDay |
| 고용 NPC | `hiredNpcs` | id/에셋명/오브젝트명/transform/활성 FSM과 3종 FSM 상태 |
| 건물 | `buildings` | BuildingRegistry의 prefabName/position/rotation |
| 인벤토리 | `inventorySlots` | Item id/name/count/quality/currentPrice |
| 핫바 | `hotbarSlots` | Item id/name/count/quality/currentPrice |
| 상점 진열 | `shopSlots` | slotIndex/key/occupied/displayPrice/item/count/quality/currentPrice |
| 낮 채집 | `dayPrepCollectedDay`, `dayPrepCollectedActivities` | 같은 날 중복 채집 방지 상태 |

## DTO

- `FriendshipRecord`: `friendshipId`, `points`, `lastDialogueDay`
- `HiredNpcRecord`: `hiredNpcId`, `candidateAssetName`, `npcObjectName`, `hasTransform`, `position`, `rotation`, legacy `spawnPosition`/`spawnRotation`, `activeFsm`, `consumerFsmState`, `producerFsmState`, `specialistFsmState`
- `SlotSaveData`: `itemId`, `itemName`, `count`, `quality`, `currentPrice`
- `ShopSlotSaveData`: 슬롯 식별자, 점유/가격, 아이템 식별자·수량·품질·현재가
- `BuildingSaveData`: `buildingName`, `position`, `rotation`

## 마이그레이션 체인

| 전환 | 추가/보정 |
|---|---|
| v0→v1 | 인벤토리·핫바 리스트 초기화 |
| v1→v2 | `lastAuditDay` |
| v2→v3 | 친밀도·고용 NPC 리스트 |
| v3→v4 | 친밀도 일일 제한, 고용 NPC transform/FSM 기본값 |
| v4→v5 | ShopSlot 진열 아이템·표시 가격 |
| v5→v6 | 플레이어 이름·맵·Day 1 단계 |
| v6→v7 | 장기 진행 sidecar 필드 |
| v7→v8 | 낮 채집 완료 일자·activity ID 목록 |

## 복원 순서와 의존성

1. 경제·티어·시간·Day 1/장기/Day Prep 상태
2. 플레이어 위치·감사·친밀도
3. 기존 등록 건물/그리드 초기화 후 건물 재생성
4. 인벤토리·핫바, ShopSlot 복원
5. 건물과 재고가 준비된 뒤 고용 NPC와 FSM 복원

아이템은 `ItemRegistry.Find(itemId, itemName)`으로 복원한다. ShopSlot은 hierarchy 기반 `slotKey`를 우선하고, 없으면 `slotIndex`를 폴백으로 쓴다.

## 현재 저장하지 않는 핵심 상태

- `SalesLogManager` 판매 이력과 구매/거절 통계
- `VillageChangeSignalController` 카테고리 누적/선도 신호
- `VillageCultureVisualController` pending/active 마을 변화
- 관광객/단골 같은 미구현 고객 계층
- 런타임 데모 드레싱(저장 대상이 아닌 프레젠테이션 사이드카)

위 세 판매/마을 변화 항목은 Task 054 설계 및 Task 055/057 사람 승인 후에만 추가한다.

## 확인된 주의점

- `SaveGameAsync`에 `data.version = CurrentSaveVersion`이 연속 두 번 있다. 기능 결과는 같아 현재 Task에서는 수정하지 않는다.
- `LocalJsonSaveRepository.SaveAsync`는 디렉터리 생성 없이 `File.WriteAllText`를 호출한다. 기본 persistentDataPath는 존재하지만 customRoot 검증에서는 디렉터리를 먼저 준비해야 한다.
- 현재 구현 증거만으로는 실제 저장소 왕복 PASS를 선언할 수 없다. Task 011에서 사용자 save를 건드리지 않는 임시 경로 왕복 검증이 필요하다.
