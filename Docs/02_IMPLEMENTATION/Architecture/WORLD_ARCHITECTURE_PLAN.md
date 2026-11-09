# WORLD-000 — World Architecture Plan

> Ticket: WORLD-000 Procedural Island + Grid Terraforming Architecture Reframe  
> 작성일: 2026-08-04  
> 상태: 조사·설계 완료, 구현 미착수  
> 구현 대상 씬: 승인 후 `Assets/Scenes/WorldSandbox.unity` (현재 존재하지 않으며 WORLD-000에서 생성하지 않음)

## 1. 결론

Project P.A.의 신규 월드는 **셀/Chunk 기반 custom mesh heightfield(Option B)** 로 설계한다. 완전 voxel은 배제하고 Unity Terrain은 권위 지형으로 사용하지 않는다. 기존 2m 배치 규칙을 유지해 `WorldGridService`가 월드 토폴로지 권위가 되고, 현재 `GridService`는 실내 및 호환 배치를 위한 façade/adapter로 단계적으로 연결한다.

기준값은 다음과 같다.

| 항목 | WORLD-000 기준 |
|---|---|
| 수평 셀 | 2.0m × 2.0m |
| 테스트 Chunk | 16 × 16셀 = 32m × 32m |
| 기본 섬 논리 크기 | 128 × 128셀 = 256m × 256m, `WorldDefinition`으로 구성 가능 |
| 높이 단계 | 1.0m, 0~6의 7단계 |
| 좌표 | X 동쪽, Z 북쪽, Y 위. `cell (0,0)`의 중심을 `worldOrigin`으로 정의 |
| 회전 | 북/동/남/서 90도 quarter turn 정수 |
| 이동 인접성 | 기본 4방향. 같은 높이 또는 명시된 ramp/bridge link만 보행 가능 |
| 저장 | seed + generationVersion + sparse modification delta |

2m는 현행 `GridService.cellSize`, `BuildManager.gridSize`, `OutdoorPlacementController`의 47×47/(-46..46m) 좌표와 일치한다. 16셀 Chunk는 한 Chunk가 32m라 실험과 부분 재생성 범위를 읽기 쉽고, 128셀 섬은 현행 약 96m 고정 맵을 보존 가능한 내부 크기로 수용하면서 8×8 Chunk로 제한된다. 1m 높이는 2m 셀 폭에서 경사로를 구현할 수 있고 캐릭터 스케일에서 단계가 보이며, 무한 수직 월드로 확장되지 않는다.

## 2. 현재 구현 감사

### 2.1 월드와 건설

- `GridService`는 2m 좌표 변환과 legacy 1셀 점유, zone별 footprint/clearance/interaction/회전/protected cell/4방향 entry-service 연결 검사를 제공한다. 높이, 지면, 물, biome, Chunk와 지속성은 없다.
- `BuildManager`는 플레이어 앞 2m 스냅, 90도 회전, 가격/인벤토리/`BuildingRegistry` 권위를 유지하고 `OutdoorPlacementController`가 준비되면 그 경로를 사용한다. fallback은 단일 셀과 Physics overlap만 본다.
- `OutdoorPlacementController`는 `village.outdoor`, 47×47셀, 원점 (-46,-46), 2m 고정 평면에서 multi-cell 배치/이동/회수, fixed 구조 보호, BoxCollider/Renderer 기반 footprint, 앞쪽 clearance, `NavMeshObstacle` carving, v10 placeable 저장을 제공한다. 평탄도·고도·물·입구 역할·고객/납품/확장 공간·전역 접근성은 보지 않는다.
- `BuildingData`는 이름/가격/prefab/tier만 보유한다. `BuildingRegistry`는 prefab name과 runtime GameObject를 추적한다. 둘 다 유지하되 안정 building ID, footprint, 역할 앵커와 지형 조건은 sidecar definition으로 보완한다.
- `ShopCustomizationController`는 실내 2m zone과 footprint/clearance/interaction, 경로 보존, 확장용 작은 `NavMeshSurface`/link를 이미 사용한다. 실내 배치 권위로 유지한다.

### 2.2 저장

- 현재 schema는 v10이다. `SaveData`는 플레이어/경제/시간/인벤토리/핫바/상점/진행과 absolute `BuildingSaveData`, grid 기반 `PlaceableSaveData`를 가진다.
- `SaveManager`는 `ISaveRepository`와 `LocalJsonSaveRepository`의 `savegame.json`을 사용한다. 기존 건물은 prefab name + absolute transform, 실내/실외 placeable은 zone/grid/rotation을 저장한다.
- 기존 v10은 절차 월드로 자동 해석하거나 변환하지 않는다. `LegacyFixed` 모드로 계속 로드한다. 신규 저장은 나중의 승인된 schema 티켓에서만 `worldMode`, seed, generationVersion과 sparse delta를 additive로 추가한다.

### 2.3 생활 활동

- `Farmland`/`Crop`은 Transform 자식과 실시간 coroutine 성장으로 현재 씬에 결합되며 작물 상태 저장이 없다.
- `Gatherable`은 채집 후 오브젝트를 파괴하고 reset/respawn/persistence가 없다.
- `DaytimeStockPrepPoint`와 `DayNightShopLoopController`는 day별 activity ID는 저장하지만 실제 resource node 상태는 저장하지 않는다. 런타임 활동점은 player/shop 주변 각도, raycast, 이름 기반 `WorkSpot`과 고정 resource zone에 의존한다.
- gameplay authority는 재사용하고, 월드 resource spawn key/제거·respawn delta와 역할 앵커 provider를 adapter로 제공해야 한다.

### 2.4 NPC와 상점

- `NpcController.shopLocation`, `NpcScheduleController.homePoint`, `ProducerNpcController.workSpot/dropOffPoint`는 serialized `Transform` 또는 `Shop` tag에 의존한다.
- `CustomerArrivalController`는 shop 주변 ring에서 진입점을 고르고 absolute return position을 보관한다.
- `SpecialistNpcController`와 `ShopCustomerApproachController`는 현행 회전 interaction cell, reservation, `NavMesh.SamplePosition`, 완전 경로 확인을 사용한다. 이는 새 역할 앵커 접근의 좋은 선례다.
- `PA_ShopLocator`는 Y 50 기준으로 지상 상점을 고르고, `InteriorCustomerController`는 `PA_StoreInterior`와 Y+100 관례를 사용한다.
- `BuildingEntrance`는 serialized `targetSpawn`, `ShopEvolutionController`는 이름 기반 건물/문/외부 spawn을 사용한다.
- `Shop`, `ShopSlot`, `EconomyService`, 구매 수학, Day/Night 영업 권위는 그대로 재사용한다. 위치 해석만 `WorldAnchorRegistry` adapter로 이동한다.

### 2.5 씬과 지형

- Unity 6.3 계열의 두 씬은 현재 환경에서 binary serialization이며 YAML 추측 편집 대상이 아니다.
- `PA_MapLayoutBuilder` 기준 현행 지형은 96×1×96m primitive Cube ground, primitive beach/water/roads/plaza와 고정 건물/marker 배치다. `TerrainData`/`TerrainCollider` 근거가 없어 Unity Terrain 권위가 아니다.
- Ground의 `NavMeshSurface`를 전역 bake하며 `PA_RuntimeSceneBinder`는 저장된 surface의 `AddData`와 최대 12m agent snap/warp만 수행한다.
- AI Navigation 2.0.12의 로컬 소스 `NavMeshSurface.UpdateNavMesh(NavMeshData)`는 비동기 `NavMeshBuilder.UpdateNavMeshDataAsync`를 호출한다. `CollectObjects.Volume` bounds는 가능하지만 하나의 거대한 surface를 진정한 독립 local tile로 패치하는 계약은 아니다. 따라서 독립 Chunk/sector surface 소유가 안전하다.

## 3. 아키텍처 선택지 비교

점수는 1(부적합/고위험)~5(적합/낮은 위험)다.

| 기준 | A Unity Terrain | B Custom Chunk Mesh | C Voxel/Block |
|---|---:|---:|---:|
| 단계형 테라포밍 | 2 | 5 | 5 |
| 섬/절벽/물 제어 | 3 | 5 | 5 |
| 셀 점유·건물 통합 | 2 | 5 | 4 |
| seed+delta 저장 | 3 | 5 | 2 |
| 부분 렌더/충돌 갱신 | 3 | 5 | 4 |
| 부분 NavMesh 소유 | 3 | 4 | 2 |
| Unity 6 URP 적합성 | 4 | 4 | 3 |
| 기존 2m 코드 통합 | 2 | 5 | 2 |
| 1인/AI bounded 구현 | 4 | 4 | 1 |
| 10h/30h 콘텐츠 적합 | 3 | 5 | 1 |
| 향후 멀티플레이 결정성 | 3 | 4 | 3 |
| 기술 부채 통제 | 3 | 4 | 1 |

### Option A — Unity Terrain

내장 LOD, collider와 Navigation source가 장점이다. 그러나 heightmap은 연속 surface라 셀마다 수직 절벽/물 경계/점유를 정확히 표현하기 어렵고, 브러시 변경과 저장 차이를 기존 2m 배치 권위에 맞추기 어렵다. Project P.A.의 권위 지형으로 채택하지 않는다. 먼 배경용 비권위 장식은 나중에 별도 검토할 수 있다.

### Option B — Custom Chunk Mesh

제한된 heightfield 셀에서 상면/절벽/물가를 결정적으로 생성하고 Chunk별 renderer/collider/nav source를 소유할 수 있다. 현행 2m grid와 직접 맞고 seed+delta 저장 및 AI의 bounded ticket 구현에 가장 적합하다. seam, normal/UV, dirty scheduling 책임이 생기므로 16×16 testbed와 명확한 validator로 제한한다.

### Option C — Voxel

동굴과 overhang까지 자유롭지만 렌더링, collider, 조명, 저장, nav, multiplayer 결정성을 모두 새 엔진 수준으로 만든다. 상점/NPC/경제가 중심인 1인 프로젝트의 10h/30h 콘텐츠를 잠식하므로 배제한다.

## 4. 권장 데이터 모델

후보 이름은 2026-08-04 현재 `Assets`에서 충돌이 없었다. 실제 코드는 WORLD-001 이후 확정한다.

```text
WorldDefinition
  long worldSeed
  int generationVersion
  int widthCells = 128
  int heightCells = 128
  float cellSize = 2.0
  int chunkSize = 16
  float elevationStep = 1.0
  byte minElevationLevel = 0
  byte maxElevationLevel = 6
  Vector3 worldOrigin  // cell(0,0)의 중심

WorldCellData
  CellCoord coord               // x,z는 배열 밖에서 중복 저장하지 않아도 됨
  byte elevationLevel
  GroundType groundType         // Grass, Soil, Sand, Rock, Mud
  WaterType waterType           // None, Ocean, River, Pond
  byte waterSurfaceLevel
  PathType pathType             // None, Dirt, Stone, Wood
  byte cliffMask                // N/E/S/W, 가능한 한 파생값
  OccupancyFlags occupancyFlags // Building, Decoration, Resource, Bridge, Ramp, Reserved
  BiomeType biome               // Coast, Meadow, Forest, Highland, Resource
  bool farmable                 // ground/water/경사로부터 파생 가능
  bool protectedCell
  bool modified

WorldChunkData
  ChunkCoord coord
  DirtyFlags dirty              // Visual, Collider, Water, Nav, Save
  int meshRevision
  int navRevision

WorldPlacedBuildingData
  string instanceId
  string buildingId
  CellCoord originCell
  byte rotationQuarterTurns
  int definitionRevision
  int currentStage

WorldModificationData
  List<ModifiedCellRecord> modifiedCells
  List<WorldPlacedBuildingData> placedBuildings
  List<PlacedDecorationRecord> placedDecorations
  List<BridgeRecord> bridges
  List<RampRecord> ramps
  List<ResourceStateRecord> resourceStates
```

### 데이터 불변식

- cell center: `worldOrigin + (x * cellSize, elevationLevel * elevationStep, z * cellSize)`.
- Chunk coord는 음수 안전 floor division을 사용하고 local coord는 0..15다.
- cliff mask, farmable과 walkability는 가능한 한 원본 필드에서 파생해 중복 진실을 줄인다.
- 물은 지형 바닥의 elevation과 별도 `waterSurfaceLevel`을 가져 깊이를 표현하되 overhang/동굴은 허용하지 않는다.
- 건물 footprint와 entrance/queue/delivery/expansion cell은 저장마다 복제하지 않고 revision이 있는 sidecar definition에서 산출한다. 저장은 안정 ID, 위치, 회전, stage를 가진다.

## 5. 점유와 건물 이동

`WorldGridService`가 지형과 월드 점유의 권위다. 기존 `GridService`는 다음처럼 공존한다.

- 실내 `shop.interior` zone과 현행 Core Slice는 기존 `GridService`를 유지한다.
- `village.outdoor`의 새 WorldSandbox 경로는 `WorldGridService`에 위임하는 adapter를 사용한다.
- 두 서비스가 같은 outdoor cell을 독립적으로 점유하지 않도록 feature mode별 단일 writer를 둔다.
- `BuildManager`, `OutdoorPlacementController`, `BuildingRegistry`는 삭제하지 않고 가격/아이템/preview/instance registry façade로 유지한다.

새 `WorldPlacementDefinition` sidecar는 stable building ID, footprint, 허용 회전, ground/water/slope, entrance, customer buffer, delivery, specialist interaction, expansion reserve와 보호 규칙을 제공한다.

이동은 원자적 transaction이다.

1. 목적 footprint/clearance/flatness/ground를 읽기 전용 검사한다.
2. entrance/customer/delivery/expansion과 핵심 경로를 cell graph로 검사한다.
3. 새 셀을 임시 예약하고 해당 instance의 route anchor를 무효화한다.
4. 오브젝트를 이동하고 collider/preview/registry/anchor를 갱신한다.
5. old/new Chunk와 이웃의 visual/collider/nav/save dirty를 표시한다.
6. 성공 뒤 기존 셀을 해제한다. 실패하면 예약·Transform·anchor·점유를 모두 rollback한다.

## 6. 저장 전략

전체 128×128 셀을 매번 저장하지 않는다.

- generator base: `worldSeed`, `generationVersion`, `WorldDefinition` revision.
- sparse delta: base와 다른 셀, 플레이어 건물/장식/다리/경사로/길, resource spawn key의 제거·respawn 상태.
- deterministic IDs: generator가 만드는 자원/랜드마크는 `(generationVersion, seed, spawnKey)`로 재현한다.
- generator 변경 시 과거 저장은 원래 generationVersion 구현을 유지하거나 명시적 migration을 거친다.
- 초기에는 v10 단일 `savegame.json` 안에 additive `WorldStateSaveData`를 중첩해 원자성을 유지하는 안을 권장한다. 실제 schema 추가와 버전 증가는 WORLD-007에서 사람 승인 후 수행한다.
- 기존 absolute `BuildingSaveData`와 `PlaceableSaveData`는 `LegacyFixed`에서 그대로 읽고 자동 변환하지 않는다.
- 기존 `ISaveRepository`/`LocalJsonSaveRepository`는 재사용한다. 파일 분할은 실제 delta 크기와 원자 저장 필요를 측정한 뒤 재검토한다.

## 7. Navigation 전략

### 7.1 두 단계 접근성

1. **논리 cell graph**: 지형 편집/건물 배치 전 4방향 walkability, 높이 차, water, cliff, ramp/bridge, protected route를 빠르게 검사한다.
2. **NavMesh proof**: 실제 agent radius/height/step/area로 `NavMesh.CalculatePath`가 complete인지 최종 확인한다.

플레이어 collider가 지나갈 수 있다는 사실은 NPC 도달 가능성을 증명하지 않는다. 상점 입구, customer approach/queue, delivery, specialist interaction, 각 주민 home/work, world entry를 critical anchor로 등록한다.

### 7.2 부분 갱신

- 각 terrain Chunk 또는 소수 Chunk sector가 자신의 `NavMeshSurface`/`NavMeshData`를 가진다.
- `CollectObjects.Volume` bounds와 AI Navigation 2.0.12의 비동기 `UpdateNavMesh`를 사용해 dirty Chunk와 seam 이웃만 재수집한다.
- 하나의 전역 surface를 local patch라 부르지 않는다. 독립 surface 경계에는 overlap margin과 검증된 `NavMeshLink`를 사용한다.
- 건물 이동 직후에는 현행 carving obstacle을 임시 차단으로 유지하고, terrain topology가 바뀐 Chunk는 queue에서 한 번씩 갱신한다.
- 여러 셀 brush는 프레임마다 bake하지 않고 edit transaction 종료 시 dirty 범위를 합친다.

### 7.3 NPC 안전

- dirty bounds를 지나거나 그 안에 있는 agent만 affected set에 넣고 이동 intent/role anchor를 보존한 채 안전 정지한다.
- surface update 중 새 목적지 예약과 배치 커밋을 잠시 막는다.
- 완료 후 agent를 가장 가까운 유효 지점에 제한 거리로 재투영하고 role anchor를 다시 resolve한 뒤 complete path일 때만 재개한다.
- critical anchor가 고립되는 terraform/building operation은 commit 전에 거부한다. 비핵심 영역 고립은 경고와 debug overlay로 표시한다.

### 7.4 D3D11/자동화 경계

Navigation update 검증은 headless data/path validator와 Unity D3D11 단일 validator를 분리한다. 같은 직접 `Camera.Render()` 경로는 네이티브 충돌 2회로 재시도하지 않는다. 한 Unity 실행에 여러 renderer validator를 묶지 않으며, Game View 확인은 사람 승인 뒤 별도 단계다.

## 8. Scene Development Strategy

### `Prototype_FirstDay.unity` — Golden Regression Scene

- Core Slice Reference Scene이자 안전한 졸업작품 제출 기준이다.
- Day 1~3 상점/NPC/Day-Night/채집/고객/정산/UI/validator를 보존한다.
- WorldGrid, Chunk terrain, Terraforming, procedural island를 직접 삽입하거나 hierarchy/serialized reference를 재구성하지 않는다.
- 새 월드가 실패하거나 일정이 부족해도 플레이 가능한 안전본으로 남긴다.

### `WorldSandbox.unity` — New World Technology Testbed

- 권장 경로는 `Assets/Scenes/WorldSandbox.unity`이며 현재 존재하지 않는다.
- WORLD-000에서는 만들지 않는다. 승인된 후속 티켓에서 editor builder/scene setup utility로 만든다.
- 초기에는 최소 player/test camera, WorldGrid root, 16×16 또는 32×32 test cells, 1~소수 Chunk, 높이/좌표/경계 debug, 작은 placement test object만 둔다.
- 기존 게임을 복제하거나 상점/경제/인벤토리/NPC를 재구현하지 않고 필요한 기존 서비스만 단계적으로 adapter로 연결한다.

### `MainGame.unity` — Validated World + Existing Gameplay Integration Scene

- WORLD-000과 초기 WORLD 티켓에서 수정하지 않는다.
- WorldSandbox에서 검증되지 않은 기술을 적용하지 않는다.
- 기존 중요한 구성을 무조건 덮어쓰지 않는다. 최종 통합 씬으로 사용할지는 Gate 결과와 별도 사람 승인으로 정한다.

## 9. 단계적 통합 Gate

| Gate | 통과 조건 |
|---|---|
| Gate 1: World Data | 좌표 변환, 셀/Chunk 조회, 높이·지면 유지, debug grid |
| Gate 2: Terrain | raise/lower, dirty Chunk만 갱신, 충돌, delta 복원, seam 무결성 |
| Gate 3: Building | 작은 건물 1종 ghost/footprint/rotation/flatness/entrance/occupancy/save |
| Gate 4: Navigation | local update, 목적지/고립 검사, 입구 도달, NPC 안전 정지·재개 |
| Gate 5: Existing Gameplay Integration | 기존 상점 1개, 고객·생산자·간판·Day/Night·경제·구매 권위 유지 |

Gate 1~4 전에는 Core Slice나 MainGame에 신규 월드를 강제 적용하지 않는다. Gate 5와 MainGame 통합은 WORLD-009 이후 별도 승인 티켓으로 분리한다.

## 10. 성능과 구현 제한

- WorldGrid 원본 데이터는 flat/native-friendly 배열을 우선하고 GameObject-per-cell을 금지한다.
- mesh/collider는 Chunk 단위로 합치고 debug overlay만 제한적으로 셀을 시각화한다.
- visible Chunk, collider, water, nav, save dirty flag를 분리한다.
- mesh generation은 versioned pure data 입력을 우선해 EditMode에서 결정성을 검사할 수 있게 한다.
- LOD/Jobs/Burst/멀티플레이는 측정 전 선행 조건이 아니다. 데이터 결정성과 stable IDs는 향후 확장을 막지 않는 수준으로만 보장한다.

## 11. 마이그레이션 순서

1. WORLD-001~008을 WorldSandbox에서 Gate 1~4까지 검증한다.
2. 기존 `GridService`, BuildManager, 저장, Shop/NPC 권위는 sidecar/adapter로 필요한 만큼만 연결한다.
3. WORLD-009~010에서 이동 상점과 role anchor를 검증해 Gate 5를 통과한다.
4. WORLD-011~012는 다리/경사와 장식 배치를 확장하되 기존 Core Slice를 계속 회귀 기준으로 둔다.
5. MainGame 통합은 별도 bounded ticket과 사람 승인 뒤 수행한다. `Prototype_FirstDay` 이전 여부도 그때 따로 판단한다.

WORLD-000은 문서만 만들었으며 어떠한 scene/code/save/package 변경도 수행하지 않았다.
