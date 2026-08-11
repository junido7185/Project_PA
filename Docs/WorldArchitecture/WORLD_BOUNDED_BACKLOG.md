# WORLD Bounded Backlog

> 기준: WORLD-000 / 2026-08-04  
> 공통 금지: `Prototype_FirstDay.unity`, `MainGame.unity`, `ProjectSettings/`, `Packages/`, `Project_D`, 외부 패키지, 기존 core rewrite, 승인 없는 save schema/scene 변경, Git commit/push/reset/clean.  
> 공통 회귀: Golden Regression Scene 파일 무변경, runtime/editor compile, 기존 Day 1~3 validator 경로 보존. 직접 `Camera.Render()` 재시도 금지.

WORLD-001~008은 기본적으로 승인 후 생성할 `Assets/Scenes/WorldSandbox.unity`만 대상으로 한다. WORLD-009부터 기존 Shop/NPC를 adapter로 연결한다. Gate 1~4 전 Core Slice/MainGame 통합은 금지한다.

## 2026-08-11 M70 실행 매핑과 선승인 경계

- `PREAPPROVED_MILESTONE_CONTINUATION` 승인 sequence는 `WORLD-005 → WORLD-006 → WORLD-006B → WORLD-007 → WORLD-008 → WORLD-009 → WORLD-010`이다.
- 실제 완료 목적 매핑은 WORLD-005 Relocatable Building, WORLD-006 Deterministic Island, WORLD-006B Movable Shop Furniture, WORLD-007 World Persistence, WORLD-008 Reachability/Navigation, WORLD-009 Existing Gameplay Adapter, WORLD-010 M70 Integration/Regression이다. 아래 초기 backlog의 WORLD-009/010 명칭보다 실제 완료 기록과 loop-state가 우선한다.
- 이 sequence 안에서는 ticket PASS와 local commit 뒤 다음 승인 ticket으로 자동 전환하며 중간 사람 메시지 부재는 blocker가 아니다. 동시에 하나의 ticket만 활성화한다.
- `WORLD-010` 또는 `M70_PLAYABLE_WORLD_ALPHA_COMPLETE`에서 반드시 멈춘다. WORLD-011, WORLD-012, `WORLD-MAIN-001`은 자동 후속이 아니며 새 사람 승인이 필요하다.
- 현재 sequence는 이미 WORLD-010까지 완료됐다. 정책 티켓 때문에 WORLD-005부터 재실행하지 않는다.

## Gate와 티켓 대응

| Gate | 티켓 |
|---|---|
| Gate 1: World Data | WORLD-001 |
| Gate 2: Terrain | WORLD-002~004, WORLD-007 delta |
| Gate 3: Building | WORLD-005, WORLD-007 delta |
| Gate 4: Navigation | WORLD-008, WORLD-011 |
| Gate 5: Existing Gameplay Integration | WORLD-009~010 |
| 확장/미감 | WORLD-012 |
| MainGame 통합 | 별도 `WORLD-MAIN-001`, Gate 1~5와 사람 승인 후 |

## WORLD-001 — World Cell Data Model + Read-Only Debug Grid

- **플레이어 경험:** 테스트 카메라에서 셀 좌표·높이·Chunk 경계를 읽을 수 있고 같은 좌표가 항상 같은 셀을 가리킨다.
- **구현 범위:** pure `WorldDefinition`/cell/chunk coords, read-only `WorldGridService`, 2m 변환, 16×16 debug grid, 최소 WorldSandbox editor builder와 data validator. 지형 편집 없음.
- **허용 경로:** 신규 `Assets/Scripts/World/**`, `Assets/Editor/World/**`, 승인된 `Assets/Scenes/WorldSandbox.unity`, WORLD 문서/검증 로그.
- **금지 경로:** 기존 scenes, SaveData/SaveManager, BuildManager/GridService 변경, terrain mesh/generator/NavMesh.
- **선행 티켓:** WORLD-000 `needs_human_review`의 scene 분리/기준값 승인.
- **최대 변경 파일 수:** 12.
- **자동 validator:** 10,000회 world↔cell round trip, 경계/음수 floor, chunk/local 좌표, 16×16 count, scene 필수 root 1개와 manager 중복 0.
- **사람 승인:** WorldSandbox 신규 scene 생성, 2m cell/16 chunk/1m step을 prototype baseline으로 사용하는 것.
- **중단 조건:** 이름 충돌, 기존 singleton 중복, 좌표 왕복 불일치, scene builder가 다른 scene을 저장하려 함.
- **회귀 목록:** Golden scene hash/diff 무변경, 기존 GridService tests/static contract, compile.
- **저장/씬/NavMesh 영향:** 저장 없음 / WorldSandbox 신규 1개 / NavMesh 없음.
- **예상 위험도:** 중간.

## WORLD-002 — Chunk Testbed + Height Level Mesh Prototype

- **플레이어 경험:** 16×16 또는 32×32 테스트 땅에서 0~6 높이와 절벽 경계가 명확하고 플레이어/카메라 스케일이 자연스럽다.
- **구현 범위:** 1~4 Chunk의 top/cliff mesh, normals/UV/material slots, MeshCollider, dirty visual/collider rebuild, seam debug. 편집 UI와 generator 없음.
- **허용 경로:** WORLD 신규 runtime/editor 폴더, WorldSandbox, 신규 project-owned test material/mesh validator.
- **금지 경로:** Unity Terrain, voxel, 기존 art/source models, NavMesh/save/shop/NPC.
- **선행 티켓:** WORLD-001 Gate 1 PASS.
- **최대 변경 파일 수:** 14.
- **자동 validator:** vertex/index bounds, winding/normal, shared-edge position equality, collider bounds, dirty Chunk만 revision 증가.
- **사람 승인:** Game View에서 2m 셀·1m 높이·절벽 비율과 카메라 가독성.
- **중단 조건:** seam hole, non-manifold collider, GameObject-per-cell, 한 셀 변경이 모든 Chunk를 재생성.
- **회귀 목록:** WORLD-001 좌표, Golden scene diff, compile, no Unity Terrain component.
- **저장/씬/NavMesh 영향:** 저장 없음 / WorldSandbox만 / NavMesh 없음.
- **예상 위험도:** 높음.

## WORLD-003 — Single-Cell Raise/Lower Terraforming

- **플레이어 경험:** 한 셀을 선택해 한 단계 올리거나 내리고 인접 절벽·충돌이 즉시 일관되게 바뀐다.
- **구현 범위:** selection/debug input, preflight/edit transaction, 0~6 clamp, protected cell, dirty Chunk+seam, undo-in-session 1단계 또는 rollback. water/path 편집 없음.
- **허용 경로:** World 신규 data/edit/debug 코드, WorldSandbox, 해당 validator.
- **금지 경로:** save schema, buildings/NPC/NavMesh, brush mass edit, production UI/art.
- **선행 티켓:** WORLD-002 visual/collider PASS와 사람 비율 승인.
- **최대 변경 파일 수:** 12.
- **자동 validator:** min/max, adjacent cliffMask, boundary neighbor dirty, failed transaction no mutation, collider revision.
- **사람 승인:** raise/lower 조작 감각과 한 단계 피드백.
- **중단 조건:** protected cell 변경, failed edit가 일부 commit, seam/collider mismatch.
- **회귀 목록:** WORLD-001/002 validators, Golden scene diff, compile.
- **저장/씬/NavMesh 영향:** in-memory modification record만 / WorldSandbox / NavMesh 없음.
- **예상 위험도:** 중간.

## WORLD-004 — Ground/Path Paint and Water Cell Prototype

- **플레이어 경험:** grass/soil/sand/rock 지면과 dirt/stone path를 칠하고 작은 pond/river cell을 만들거나 메울 수 있다.
- **구현 범위:** ground/path/water data transactions, shoreline faces, farmable/walkability derivation, protected/invalid water checks. 최종 shader/art 없음.
- **허용 경로:** World 신규 data/render/edit/debug, WorldSandbox, project-owned test materials, validator.
- **금지 경로:** infinite fluid simulation, erosion, ocean shader package, bridges/ramps, save schema.
- **선행 티켓:** WORLD-003.
- **최대 변경 파일 수:** 14.
- **자동 validator:** ground/path enums, water level/depth invariant, shoreline seam, path-under-water reject, dirty flags, rollback.
- **사람 승인:** 물가 읽기, 색상은 prototype 판단만 하고 최종 아트 승인으로 간주하지 않음.
- **중단 조건:** fluid simulation으로 범위 확장, water collider가 player를 가둠, 인접 Chunk gap.
- **회귀 목록:** WORLD-001~003, Golden scene diff, compile.
- **저장/씬/NavMesh 영향:** in-memory delta / WorldSandbox / cell walkability만, bake 없음.
- **예상 위험도:** 높음.

## WORLD-005 — Relocatable Building MVP

- **플레이어 경험:** 기존 작은 건물 1종을 ghost로 옮기고 회전하며 유효/무효 이유와 입구를 이해한다.
- **구현 범위:** sidecar placement definition, 기존 BuildManager/BuildingRegistry/GridService adapter, footprint/rotation/flatness/ground/entrance/occupancy transaction, move rollback. 가격/경제 없음 또는 기존 권위 사용.
- **허용 경로:** 신규 World placement 코드/definition, 필요한 최소 BuildManager 또는 OutdoorPlacement adapter, WorldSandbox, 작은 기존 building data 참조, validator.
- **금지 경로:** BuildingData 전면 schema 변경, 모든 건물 migration, Shop/NPC/save/NavMesh, prefab/source asset 수정.
- **선행 티켓:** WORLD-004 Gate 2 PASS.
- **최대 변경 파일 수:** 15.
- **자동 validator:** rotated footprint/entrance, flatness, water/cliff reject, old/new occupancy atomicity, rollback, one registry instance.
- **사람 승인:** 사용할 기존 작은 건물 1종, ghost/entrance 시각, WorldSandbox scene 변경.
- **중단 조건:** 두 occupancy writer, 기존 building 삭제, invalid placement commit, rollback 실패.
- **회귀 목록:** 기존 outdoor placement move/recover static contract, WORLD-001~004, Golden scene diff, compile.
- **저장/씬/NavMesh 영향:** session-only / WorldSandbox / carving만 임시, nav proof는 WORLD-008.
- **예상 위험도:** 높음.

## WORLD-006 — World Seed + Minimal Island Generator

- **플레이어 경험:** seed를 바꾸면 해안·초원·숲·물·고도가 달라지지만 항상 안전한 시작 평지와 핵심 활동 후보지가 있다.
- **구현 범위:** deterministic 128×128 base cells, island mask, beach/elevation, minimal river/pond/biome/resource spawn keys, guaranteed start/shop/beach/activity connectivity. 미감 dressing 없음.
- **허용 경로:** 신규 generation/data validators, WorldSandbox debug, World docs.
- **금지 경로:** runtime save, full decoration, caves/voxel, existing scene replacement, final resource prefabs.
- **선행 티켓:** WORLD-004; WORLD-005 definition rules를 읽되 building code에 결합하지 않음.
- **최대 변경 파일 수:** 14.
- **자동 validator:** 100+ fixed seeds determinism/checksum, land ratio bounds, start/shop footprint, beach path, required biome/activity candidates, generation budget.
- **사람 승인:** seed 품질 표본과 기본 128×128 범위; 수치 변경 시 generatorVersion 정책.
- **중단 조건:** 유효 seed 실패, nondeterminism, 한 seed 예외를 hardcode, 콘텐츠/장식으로 범위 확장.
- **회귀 목록:** WORLD data/terrain validators, Golden scene diff, compile.
- **저장/씬/NavMesh 영향:** seed in memory / WorldSandbox / cell graph만.
- **예상 위험도:** 높음.

## WORLD-007 — Modified Cell Delta Save/Load

- **플레이어 경험:** seed 섬에서 바꾼 높이·지면·물·길과 옮긴 작은 건물이 재실행 뒤 정확히 돌아온다.
- **구현 범위:** 사람 승인된 additive vNext world payload, generationVersion, sparse modified cells/building/resource records, v10 LegacyFixed 분기, deterministic round trip/migration fixture.
- **허용 경로:** SaveData/SaveManager/identity save docs, 신규 World serialization DTO/tests, repository 재사용.
- **금지 경로:** 승인 없는 schema 변경, v10 absolute building 자동 변환, 기존 save 삭제, whole-world cell snapshot, repository rewrite.
- **선행 티켓:** WORLD-003~006 데이터 계약 PASS, save design 사람 승인.
- **최대 변경 파일 수:** 15.
- **자동 validator:** v10 legacy fixture unchanged, same seed+delta checksum, sparse count, invalid/out-of-range delta reject, missing generation version fail-safe, interrupted-save safety 가능한 범위.
- **사람 승인:** schema version/additive field/migration/backup plan과 실제 격리 save 왕복.
- **중단 조건:** 기존 save overwrite 위험, non-additive migration 필요, JsonUtility가 모델을 손실, delta가 whole snapshot으로 변질.
- **회귀 목록:** 기존 save round-trip validator의 안전 경로, inventory/hotbar/time/shop/placeables, Golden scene diff.
- **저장/씬/NavMesh 영향:** 높음 / WorldSandbox와 legacy fixture / 없음.
- **예상 위험도:** 매우 높음.

## WORLD-008 — Local Navigation Rebuild and Reachability Check

- **플레이어 경험:** 지형/작은 건물을 바꾼 뒤 NPC 테스트 agent가 멈춰 기다렸다가 새 경로로 재개하며 고립 배치는 거부된다.
- **구현 범위:** cell reachability graph, critical anchors, per-Chunk/sector `NavMeshSurface` volume, dirty queue, seam overlap/link, async update, affected-agent pause/reproject/repath, local performance probe.
- **허용 경로:** 신규 World navigation/editor validator, WorldSandbox, 필요한 최소 agent test harness, AI Navigation 2.0.12 기존 API.
- **금지 경로:** Packages 변경, global rebuild per cell, 기존 PA_RuntimeSceneBinder 전면 rewrite, production NPC migration, 직접 Camera.Render.
- **선행 티켓:** WORLD-005, WORLD-007; Gate 2/3 PASS.
- **최대 변경 파일 수:** 15.
- **자동 validator:** only dirty+neighbor revisions, complete/incomplete paths, isolated region, entrance reach, agent state resume, operation timeout, seam crossing.
- **사람 승인:** per-Chunk surface 전략, D3D11 단일 runtime test, 허용 rebuild 시간/agent pause 감각.
- **중단 조건:** agent loss/warp beyond limit, seam flapping, 전체 surface 반복 rebuild, update API instability, native crash 재발.
- **회귀 목록:** Specialist/customer path contracts, existing baked surface AddData, WORLD-001~007, Golden scene diff.
- **저장/씬/NavMesh 영향:** navRevision만 저장 후보 / WorldSandbox / 매우 높음.
- **예상 위험도:** 매우 높음.

## WORLD-009 — Shop Relocation Integration

- **플레이어 경험:** 기존 상점을 옮겨도 간판으로 개점하고 고객이 진열대에 접근하며 생산자가 새 납품점으로 온다.
- **구현 범위:** 기존 상점 1개 sidecar anchors(entrance/customer/delivery/sign/slots/open), BuildManager adapter, customer/producer retarget, Day/Night open loop와 purchase/economy 보존. WorldSandbox Gate 5 전용.
- **허용 경로:** 신규 World anchor/shop adapter, 필요한 최소 ShopOpenSign/DayNight/CustomerArrival/Producer seams, WorldSandbox, validator.
- **금지 경로:** Shop/PurchaseEvaluator/EconomyService rewrite, all shops, interior/evolution 전면 migration, MainGame/Prototype 씬.
- **선행 티켓:** WORLD-008 Gate 4 PASS.
- **최대 변경 파일 수:** 15.
- **자동 validator:** stable shop instance, sign follows anchor, customer complete path to available slot, producer delivery complete path, move invalidates stale target, open/purchase authority unchanged.
- **사람 승인:** 상점 이전 UX, customer/delivery buffer, 기존 상점 1개를 testbed에 연결하는 장면.
- **중단 조건:** 경제/구매 수학 변경 필요, stale customer lease 복구 실패, fixed scene 참조를 대량 변경해야 함.
- **회귀 목록:** Day 1 first sale, normal/tourist customer, producer inventory safety, ShopSlot claims, save legacy.
- **저장/씬/NavMesh 영향:** placed shop delta / WorldSandbox / dirty nav+anchors.
- **예상 위험도:** 매우 높음.

## WORLD-010 — NPC Home/Work/Shop Anchor Migration

- **플레이어 경험:** 주민 집·직장·상점 위치가 달라져도 주민이 귀가하고 일하고 쇼핑하며 위치가 사라지면 안전 대기한다.
- **구현 범위:** `WorldAnchorRegistry`, stable role keys, Npc/Schedule/Producer/Specialist adapter, subscribe/invalidate/repath, save restore binding. 먼저 1 resident+1 producer+1 specialist.
- **허용 경로:** 신규 World anchors, 필요한 최소 NPC controller seams, WorldSandbox, validator.
- **금지 경로:** NPC FSM/schedule/economy rewrite, 모든 주민 scene reference 일괄 교체, MainGame/Prototype 씬.
- **선행 티켓:** WORLD-009.
- **최대 변경 파일 수:** 15.
- **자동 validator:** anchor register/remove/move, role selection determinism, lease cleanup, complete paths, missing anchor safe idle, restore rebind.
- **사람 승인:** 세 대표 NPC의 이동/대기 자연스러움과 fixed Transform migration 범위.
- **중단 조건:** serialized reference 대량 교체 필요, NPC가 invalid NavMesh에 남음, gameplay state loss.
- **회귀 목록:** resident schedule, tourist lifecycle, specialist reservation, producer delivery, Golden scene.
- **저장/씬/NavMesh 영향:** binding stable IDs / WorldSandbox / repath 사용.
- **예상 위험도:** 매우 높음.

## WORLD-011 — Bridge/Ramp and Cliff Navigation

- **플레이어 경험:** 절벽과 물 때문에 끊긴 길에 1셀 이상 경사로와 다리를 배치해 실제 player/NPC 경로를 연결한다.
- **구현 범위:** ramp/bridge definitions, occupancy/protection, height/water compatibility, mesh/collider, explicit cell graph/NavMesh links, save delta records.
- **허용 경로:** World placement/render/navigation/save DTO의 승인된 최소 확장, WorldSandbox, validators.
- **금지 경로:** arbitrary freeform bridges, physics construction, full architecture set, external assets.
- **선행 티켓:** WORLD-008, WORLD-010; WORLD-007 save contract.
- **최대 변경 파일 수:** 15.
- **자동 validator:** endpoints/height, water span, rotated footprint, link add/remove, isolated→connected graph, move/remove save round trip.
- **사람 승인:** ramp 길이/경사와 bridge 폭, Game View/collider/NPC traversal.
- **중단 조건:** manual links leak, invalid height connects, agent falls/warps, save migration 확대.
- **회귀 목록:** terrain seam, local nav, building entrances, shop/customer/producer paths.
- **저장/씬/NavMesh 영향:** bridge/ramp delta / WorldSandbox / 높음.
- **예상 위험도:** 높음.

## WORLD-012 — Decoration, Tree, Flower, Fence, Path Placement

- **플레이어 경험:** 경로를 막지 않으면서 나무·꽃·울타리·장식을 배치/이동/회수해 자신만의 마을을 만든다.
- **구현 범위:** category별 occupancy/blocking, existing provenance-known assets, simple growth/resource identity where already supported, path overlay integration, bounded density/LOD rules.
- **허용 경로:** World decoration definitions/adapter, approved existing assets/prefabs/material references, WorldSandbox, validators, provenance docs.
- **금지 경로:** 무작위 원시 큐브/AI slop, 불명확 라이선스, 전체 art replacement, 새로운 거대 crafting system, MainGame 즉시 통합.
- **선행 티켓:** WORLD-011, art/provenance review.
- **최대 변경 파일 수:** 15.
- **자동 validator:** stable IDs, occupancy category, fence adjacency, path compatibility, move/recover/save, density/performance budget, missing reference 0.
- **사람 승인:** 자산/밀도/색상 일관성, 실제 Game View, 출처/라이선스 gate.
- **중단 조건:** provenance 불명, 핵심 동선 차단, primitive/무작위 장식으로 품질 대체, frame budget 초과.
- **회귀 목록:** shop/NPC paths, resource state, world delta, Golden scene, ART_DIRECTION 기준.
- **저장/씬/NavMesh 영향:** decoration/path delta / WorldSandbox / blocking category만 dirty.
- **예상 위험도:** 중간~높음.

## 별도 후속 — WORLD-MAIN-001 MainGame Integration Gate

WORLD-012의 자동 후속이 아니다. Gate 1~5 evidence와 사람 승인이 있어야 활성화한다.

- **목표:** MainGame의 실제 기능 inventory를 보존하면서 검증된 world bootstrap/adapter를 통합할지, 새 integration scene을 둘지 결정·실행한다.
- **필수 선행:** WORLD-001~012 중 통합에 필요한 티켓 PASS, Golden regression PASS, save backup/migration 승인, scene inventory, rollback plan.
- **금지:** MainGame hierarchy 전면 재작성, 기존 기능 삭제, Prototype_FirstDay migration 동시 수행.
- **사람 결정:** MainGame 사용 vs 새 integration scene, Prototype_FirstDay는 tutorial로 영구 보존할지 추후 별도 migration을 검토할지.

## WORLD-001 시작 조건

WORLD-000 결과 보고 후 자동 시작하지 않는다. 최소 다음 사람 확인이 필요하다.

1. 씬 분리 전략과 `WorldSandbox.unity` 신규 생성 허가.
2. custom chunk mesh 방향과 2m/16×16/1m 기준을 prototype baseline으로 승인.
3. WORLD-001 단일 티켓/최대 12경로/금지 경로 승인.
4. 현재 미완료 Task132 `AudioManager.cs`/`SalesLogManager.cs` 변경을 별도 상태로 유지한다는 확인.
5. Unity가 필요할 때 직접 Camera.Render가 아닌 승인된 D3D11 안전 경로를 사용할 것.
