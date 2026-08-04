# WORLD-000 — System Impact Map

> 분류: **재사용** / **Adapter 필요** / **장기 교체·개선** / **강한 충돌** / **정보 부족**  
> 원칙: 분류는 삭제 명령이 아니다. 현행 Core Slice는 그대로 보존하고 WorldSandbox 경로에 필요한 seam만 추가한다.

## 1. 요약

| 영역 | 시스템 | 분류 | 근거와 WorldSandbox 연결 |
|---|---|---|---|
| 좌표/점유 | `GridService` | Adapter 필요 | 2m 변환, footprint/clearance/rotation/path는 재사용 가능. 지형/물/고도/Chunk가 없어 outdoor 월드 권위가 될 수 없음 |
| 건설 흐름 | `BuildManager` | Adapter 필요 | preview/회전/가격/인벤토리/registry façade 유지. terrain flatness/role anchor 검사는 신규 placement service에 위임 |
| 야외 배치 | `OutdoorPlacementController` | Adapter 필요 | 47×47 flat zone 이동/회수/저장 경험 재사용. hardcoded origin/2m/protected cells, no elevation/reachability는 WorldGrid adapter로 대체 |
| 건물 정의 | `BuildingData` | Adapter 필요 | prefab/price/tier 유지. stable ID/footprint/entrance/customer/delivery/expansion은 sidecar 필요 |
| 인스턴스 | `BuildingRegistry` | Adapter 필요 | runtime 등록은 유지. prefab name 대신 stable definition/instance ID 매핑 필요 |
| 실내 배치 | `ShopCustomizationController` | 재사용 | `shop.interior` zone, rotated interaction, path 보존, slot expansion은 별도 실내 권위로 유지 |
| 저장 백엔드 | `ISaveRepository`, `LocalJsonSaveRepository` | 재사용 | string key 저장 계약 재사용. world delta schema와 원자성은 별도 승인 |
| 저장 조정 | `SaveManager`, `SaveData` v10 | Adapter 필요/위험 | additive world mode/delta만 검토. 기존 fixed save 자동 변환 금지, schema 변경은 사람 승인 |
| 아이템/경제 | `Item`, `ItemInstance`, `Inventory`, `Hotbar`, `EconomyService` | 재사용 | 월드 위치와 독립적인 gameplay authority. 새 구현 금지 |
| 시간/루프 | `GameClock`, `DayNightShopLoopController` | Adapter 필요 | phase 권위 재사용. activity point/shop sign/resource zone의 고정 위치 해석만 provider로 교체 |
| 농사 | `Farmland`, `Crop`, `FarmPlotInteraction` | 장기 개선 | Transform/실시간 coroutine, 저장 없음, 이름 기반 `WorkSpot_Farmer`. cell/resource state adapter와 day persistence 필요 |
| 채집 | `Gatherable` | 장기 개선 | 파괴 후 respawn/reset/persistence 없음. generator spawn key와 resource delta 필요 |
| 낮 활동 | `DaytimeStockPrepPoint` | Adapter 필요 | activity ID와 보상 흐름 유지. 월드 spawn/role anchor에서 위치 공급 |
| NPC 상점 | `NpcController` | Adapter 필요 | shopping FSM/구매 유지. serialized `shopLocation`/tag를 role anchor resolver로 교체 |
| NPC 일정 | `NpcScheduleController` | Adapter 필요 | schedule 유지. serialized `homePoint`를 building instance home anchor로 resolve |
| 생산자 | `ProducerNpcController` | Adapter 필요 | 생산/납품 거래 유지. `workSpot`/`dropOffPoint`를 role anchors로 resolve |
| 전문가 | `SpecialistNpcController` | 재사용 + Adapter | 완전 경로/interaction reservation 선례 재사용. building instance anchor registry 연결 |
| 고객 도착 | `CustomerArrivalController` | Adapter 필요 | resident/tourist lifecycle 유지. shop ring/absolute return을 world entry와 shop anchors로 교체 |
| 고객 접근 | `ShopCustomerApproachController` | 재사용 + Adapter | rotated approach/reservation/complete path 유지. 새 shop definition anchors 공급 |
| 실내 고객 | `InteriorCustomerController` | 강한 scene 결합 | 이름 `PA_StoreInterior`, Y+100, absolute return, warp island 관례. 이동 상점 통합 전 별도 adapter/정책 필요 |
| 상점 찾기 | `PA_ShopLocator` | 강한 scene 결합 | Y<50과 player nearest heuristic. stable shop instance/role registry로 장기 교체 |
| 건물 진입 | `BuildingEntrance` | Adapter 필요 | interaction/warp 유지. serialized `targetSpawn`와 single-scene Y+100 관례를 role link로 보완 |
| 상점/슬롯 | `Shop`, `ShopSlot` | 재사용 | 재고/claim/purchase/sales 권위 유지. parent 이동과 approach anchor만 재연결 |
| 간판 | `ShopOpenSign` | 재사용 + Adapter | open interaction 유지. 생성/위치가 current shop transform에 결합되어 sign anchor 필요 |
| 상점 진화 | `ShopEvolutionController` | 강한 scene 결합 | 이름 기반 cottage/door/spawn/Resources stage. 이동 가능한 shop prefab/instance root로 단계적 이관 |
| Nav 런타임 | `PA_RuntimeSceneBinder` | Adapter 필요 | baked `AddData`와 scene service 보장 재사용 가능. global 12m agent snap은 chunk rebuild 정책으로 사용 금지 |
| 지형 구축 | `PA_MapLayoutBuilder` | Golden scene 전용 | primitive flat fixed map과 full surface bake. 신규 generator로 교체하지 않고 regression builder로 보존 |

## 2. 고정 좌표·Transform·씬 의존성 목록

| 위치 | 현재 결합 | 위험 | 필요한 seam |
|---|---|---|---|
| `GridService` | world Y=0, singleton, one-cell legacy occupancy | elevation과 outdoor 이중 진실 | `IGridCoordinateProvider`/outdoor adapter, 단일 writer |
| `OutdoorPlacementController` | `ZoneOrigin=(-46,-46)`, 47×47, entry/service, 도로·건물 셀 상수 | seed 섬과 직접 충돌 | WorldDefinition bounds/protected/critical anchors 공급 |
| `BuildManager` | player 앞 Y, 2m, fallback one cell | 절벽 위 floating/false valid | terrain sample/footprint transaction provider |
| `SaveManager` | absolute building Transform, fixed scene reset | seed 재생성과 중복 instance | `worldMode` 분기, stable IDs, legacy 보존 |
| `NpcController` | `shopLocation`, `Shop` tag | shop 이동 뒤 stale cache/path | shop role subscription + repath |
| `NpcScheduleController` | `homePoint` Transform | 집 이동 뒤 귀가 실패 | resident-home binding by building instance |
| `ProducerNpcController` | `workSpot`, `dropOffPoint`, Shop tag | 생산/납품 목적지 stale | work/delivery anchor binding |
| `CustomerArrivalController` | shop ring, absolute return position | water/cliff/world edge와 충돌 | world entry anchor + safe return token |
| `InteriorCustomerController` | object name, Y+100, warp | 이동 shop/다중 섬/복수 interior 불가 | interior portal and visit lease registry |
| `PA_ShopLocator` | Y threshold 50, player nearest | 새 elevation/복수 shop에서 오판 | active player shop stable ID |
| `BuildingEntrance` | serialized paired Transform | moved/reinstantiated building stale reference | role link keyed by instance |
| `ShopEvolutionController` | `B10_Cottage_01`, named door/spawn | prefab 이동/새 scene에서 bind 실패 | shop stage root interface/sidecar anchors |
| `DayNightShopLoopController` | shop/tag/player 주변 activity placement | seed 지형과 collider에 따라 불안정 | world activity spawn service |
| `FarmPlotInteraction` | `WorkSpot_Farmer`, fallback (-15,0,10) | seed마다 잘못된 위치 | farm role/resource cell query |
| `PA_RuntimeSceneBinder` | scene surfaces + 12m sample/warp | dirty nav 중 agent 순간 이동 | affected-agent coordinator |

## 3. 신규 월드 seam 후보

이름은 현재 Assets 내 충돌이 없음을 확인했지만 실제 선언은 각 티켓에서 재확인한다.

| 후보 | 책임 | 기존 소비자 |
|---|---|---|
| `WorldDefinition` | seed/version/크기/셀/Chunk/높이 규칙 | generator, grid, renderer, save |
| `WorldGridService` | cell 원본, 좌표 변환, walkability, occupancy transaction | GridService adapter, terrain edit, placement |
| `WorldChunk`/`WorldChunkRenderer` | dirty data, mesh/collider/water 표현 | Navigation service, debug validator |
| `TerrainEditService` | raise/lower/paint/water의 사전 검증·commit·rollback | player tool, save delta, nav dirty queue |
| `WorldGenerationService` | deterministic base island와 spawn keys | SaveManager adapter, resource nodes |
| `WorldModificationData` | sparse player delta | SaveManager adapter |
| `WorldBuildingPlacementService` | footprint/flatness/role/transaction | BuildManager/OutdoorPlacement façade |
| `WorldNavigationService` | cell graph, dirty surface queue, NPC pause/repath | NPC controllers, placement validator |
| `WorldAnchorRegistry` | building instance별 home/work/shop/customer/delivery/portal roles | NPC, ShopOpenSign, DayNight loop |

## 4. 씬별 영향

### Prototype_FirstDay — 변경 금지 우선

강결합 시스템: `PA_MapLayoutBuilder`, `OutdoorPlacementController`의 fixed cells, 이름/태그/Y+100 기반 shop·interior·door·home/work markers, baked NavMesh와 다수 validator. 이 씬을 신규 월드 실험 대상으로 쓰면 직렬화 참조와 Golden regression을 동시에 위험에 빠뜨린다. WORLD 티켓은 adapter를 이 씬에 강제 설치하지 않는다.

### WorldSandbox — 최소 연결

WORLD-001~004는 World data/render/debug만 연결한다. WORLD-005에서 `BuildingData`/`BuildingRegistry`/BuildManager seam 일부, WORLD-007에서 승인된 Save adapter, WORLD-008에서 AI Navigation, WORLD-009 이후에만 Shop/NPC를 단계적으로 연결한다. 동일 manager/service를 새로 재구현하지 않는다.

### MainGame — 조사 후 통합

현재 scene은 NavMeshSurface와 home/work marker가 더 많고 기존 기능을 포함할 가능성이 있다. Gate 1~5 전 수정하지 않는다. 최종 통합은 hierarchy 덮어쓰기 대신 기존 기능 inventory, adapter 설치 위치, rollback plan을 별도 티켓에서 확정한다.

## 5. 재사용 순서

1. WORLD-001: 기존 시스템을 거의 연결하지 않고 data/debug만 검증.
2. WORLD-002~004: custom mesh/collider/edit와 dirty Chunk 검증.
3. WORLD-005: `BuildingData`, `BuildingRegistry`, `GridService`의 자유 기능을 adapter로 연결.
4. WORLD-007: `ISaveRepository`/`LocalJsonSaveRepository`와 승인된 additive delta 연결.
5. WORLD-008: AI Navigation 2.0.12 surface API와 NPC-safe queue 검증.
6. WORLD-009~010: 기존 Shop/NPC 권위를 역할 앵커에 연결.

## 6. 정보 부족과 사람 확인

- Unity Game View에서 2m 셀/1m 높이 단계가 현재 캐릭터·건물과 자연스러운지는 WORLD-002 시각 검토가 필요하다.
- per-Chunk NavMeshSurface seam 성능과 link 안정성은 API 존재만으로 증명되지 않아 WORLD-008 profiler/runtime 증거가 필요하다.
- MainGame을 최종 통합 씬으로 쓸지 새 integration scene을 둘지는 Gate 5와 scene inventory 뒤 사람 승인 사항이다.
- v10 save에 additive world payload를 넣는 schema/version 변경은 WORLD-007 전에 사람 승인이 필요하다.

## 7. 금지된 해석

- Adapter 필요는 기존 코어 재작성 허가가 아니다.
- 장기 개선은 WORLD-000 또는 초기 testbed에서 농사/채집/NPC를 새로 구현한다는 뜻이 아니다.
- binary scene string은 사용 상태의 보조 증거일 뿐 대규모 YAML/바이너리 직접 편집 근거가 아니다.
- 이름만 보고 기존 scene object를 삭제하거나 serialized reference를 대량 교체하지 않는다.
