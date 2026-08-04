# WORLD-000 — Architecture Decision Records

> 상태 표기: Accepted for design / 구현은 해당 WORLD bounded ticket과 사람 Gate를 별도로 따른다.

## ADR-WORLD-001 — 셀/Chunk custom mesh heightfield

**결정**  
신규 절차 섬의 권위 지형은 2m 셀과 제한된 높이 단계의 custom chunk mesh heightfield로 한다. Unity Terrain과 완전 voxel은 권위 데이터가 아니다.

**이유**

- 현행 배치가 이미 2m cell/90도 회전을 사용한다.
- 절벽·물·길·건물 점유를 동일 cell topology로 검증할 수 있다.
- seed base와 sparse delta, dirty Chunk mesh/collider/nav가 자연스럽다.
- 상점/NPC/경제 중심 프로젝트에서 지형 엔진의 범위를 제한한다.

**대안**

- Unity Terrain heightmap: 내장 기능은 좋으나 단계형 절벽과 정확한 셀 점유가 약하다.
- voxel/block engine: 자유도는 높지만 렌더/저장/collider/nav 범위가 졸업작품 전체를 삼킨다.

**장점**  
결정적 데이터, bounded 구현, 현재 grid와 높은 호환성, 부분 갱신, 향후 network delta 기반.

**위험**  
mesh seam, normals/UV, collider cooking, chunk nav 경계 관리가 새 책임이다.

**재검토 조건**  
WORLD-002에서 16×16 prototype이 seam/collider/시각 기준을 만족하지 못하거나, 2m/1m 기준이 캐릭터와 회복 불가능하게 어긋날 때.

## ADR-WORLD-002 — 셀·Chunk·높이 기준값

**결정**  
cell 2.0m, Chunk 16×16, elevation step 1.0m, 0~6단계를 설계 기준으로 사용한다. 기본 섬은 128×128셀로 제안하되 `WorldDefinition`에서 구성 가능하게 한다.

**이유**

- `GridService`, `BuildManager`, Outdoor zone이 2m에 맞춰져 있다.
- 32m Chunk는 96m 기존 맵과 비교 가능한 작은 단위이고 128셀 섬은 8×8 Chunk다.
- 1m 단계는 2m 폭 ramp와 읽을 수 있는 cliff를 함께 허용한다.

**대안**  
1m cell은 기존 footprint를 두 배 복잡하게 하고, 4m cell은 소품/길 배치가 거칠다. 32×32 Chunk는 초기 dirty/rebuild 범위가 크다. 0.5m 높이는 단계가 약하고 2m 높이는 한 셀 ramp가 너무 가파르다.

**장점**  
현행 자산 스케일과 migration 비용을 줄이고 debug가 단순하다.

**위험**  
최종 카메라에서 1m cliff가 낮아 보이거나 128셀 콘텐츠 밀도가 부족할 수 있다.

**재검토 조건**  
WORLD-002 동일 구도 사람 검토에서 비율 문제가 명확할 때. 데이터 포맷은 수치 변경을 허용하되 이미 저장된 world의 definition은 고정한다.

## ADR-WORLD-003 — WorldGrid와 기존 GridService의 관계

**결정**  
`WorldGridService`를 outdoor 지형/점유 권위로 추가하고 기존 `GridService`를 삭제하지 않는다. 실내와 legacy scene은 기존 서비스를 유지하며 WorldSandbox outdoor는 adapter를 통해 단일 writer만 사용한다.

**이유**  
기존 서비스는 footprint/clearance/interaction/path와 많은 소비자가 검증되어 있지만 elevation/water/chunk를 소유하지 않는다.

**대안**  
기존 GridService 전면 확장 또는 완전 교체. 전자는 Core Slice 회귀 범위가 너무 크고 후자는 검증된 배치 기능을 버린다.

**장점**  
점진 migration, Golden scene 보존, 기존 UI/가격/registry 재사용.

**위험**  
adapter 경계가 흐리면 occupancy가 두 군데에 생길 수 있다.

**재검토 조건**  
WorldSandbox 통합에서 동일 outdoor cell에 두 writer가 필요해지는 설계가 발견되면 즉시 중단하고 ownership을 재정의한다.

## ADR-WORLD-004 — Seed + sparse delta 저장

**결정**  
base world는 seed와 generationVersion으로 재생성하고 플레이어 수정만 저장한다. 초기 권장안은 승인된 vNext `SaveData` 내부의 additive `WorldStateSaveData`로 원자 저장하며 v10은 `LegacyFixed`로 보존한다.

**이유**  
전체 셀 직렬화보다 작고 deterministic regeneration을 이용할 수 있다. 단일 파일은 현행 repository에서 부분 저장 실패를 줄인다.

**대안**  
전체 world snapshot은 단순하지만 크고 base와 중복된다. 별도 world 파일은 확장성이 있지만 현재 repository에 transaction manifest가 없어 split-brain 위험이 있다.

**장점**  
기존 save backend 재사용, legacy 안전, 작은 delta.

**위험**  
generator version 보존, stable spawn key와 migration 책임이 필요하다.

**재검토 조건**  
WORLD-007 측정에서 delta가 단일 save의 안정/용량 기준을 넘거나 atomic multi-file repository가 마련될 때.

## ADR-WORLD-005 — Cell graph + Chunk NavMeshSurface

**결정**  
논리 접근성은 cell graph가 사전 검증하고, 실제 agent 이동은 Chunk/sector별 `NavMeshSurface`가 증명한다. dirty Chunk와 seam 이웃만 AI Navigation 2.0.12의 비동기 update 대상으로 삼는다.

**이유**  
현행 package는 `NavMeshSurface.UpdateNavMesh(NavMeshData)`/`CollectObjects.Volume`을 제공하지만 한 전역 surface의 임의 local patch를 독립 보장하지 않는다. cell graph는 terraform commit 전에 고립을 탐지할 수 있다.

**대안**  
매 편집마다 전역 bake는 stall과 agent 오류 위험이 크다. carving만 사용하면 절벽/물/ramp topology를 표현할 수 없다.

**장점**  
빠른 사전 거부, bounded rebuild, agent 규격의 최종 proof.

**위험**  
surface seam/link, update 중 agent 상태와 성능이 실제 runtime에서 아직 미확인이다.

**재검토 조건**  
WORLD-008에서 seam path가 불안정하거나 dirty update가 목표 frame budget을 지속적으로 넘을 때. 이 항목은 구현 전 사람 review를 유지한다.

## ADR-WORLD-006 — 역할 앵커 기반 NPC 목적지

**결정**  
home/work/shop/customer/delivery/specialist/portal 목적지는 `(buildingInstanceId, role, index)`로 `WorldAnchorRegistry`에서 resolve한다. controller의 gameplay FSM은 유지하고 Transform은 cache일 뿐 권위가 아니다.

**이유**  
현재 serialized Transform, tag, name, Y threshold는 건물 이전 후 stale해진다. Specialist/customer approach의 interaction cell과 complete path는 재사용할 수 있다.

**대안**  
이동할 때 모든 serialized Transform을 대량 교체하거나 매번 tag/nearest search. 전자는 scene 손상 위험, 후자는 결정성이 없다.

**장점**  
건물 이동 transaction과 NPC repath를 연결하고 저장 stable ID와 맞는다.

**위험**  
복수 역할, 건물 회수, save restore 중 anchor lifecycle을 명확히 해야 한다.

**재검토 조건**  
WORLD-009~010에서 기존 NPC lease/schedule/interior portal을 역할 앵커로 표현할 수 없는 사례가 발견될 때.

## ADR-WORLD-007 — 씬 분리 개발 전략

**결정**  
신규 절차 생성 월드는 `Prototype_FirstDay.unity`를 개조하지 않고 승인 후 별도 `Assets/Scenes/WorldSandbox.unity`에서 개발한다. `MainGame.unity`는 Gate 1~5 뒤 별도 통합 대상으로만 검토한다.

**이유**

- 기존 Core Slice와 validator 보존
- AI 변경 범위와 실패 격리
- binary scene/serialized reference 손상 방지
- 일정이 부족해도 제출 가능한 Golden Regression Scene 유지
- 신규 world data/terrain/building/navigation을 독립적으로 검증 가능

**대안**

1. `Prototype_FirstDay` 직접 개조: 플레이 가능 기준과 실험을 한곳에서 깨뜨릴 위험 때문에 선택하지 않음.
2. `MainGame` 즉시 재작성: 사용 중인 기능과 의존성을 보존할 근거가 부족하고 사람 승인 없이 허용되지 않음.
3. 기존 씬을 복제해 전면 수정: 이름과 serialized dependency까지 복제해 두 진실과 drift를 만들므로 선택하지 않음.

**장점**  
실패 격리, 작은 scene, validator가 명확하고 adapter를 필요한 시점에만 연결할 수 있다.

**위험**  
WorldSandbox만 진전되고 실제 게임 통합이 늦어지는 기술 데모 함정이 있다. Gate 5와 MainGame integration ticket으로 통제한다.

**재검토 조건**  
Gate 1~5 완료 후 MainGame을 최종 통합 씬으로 사용할지, 별도 integration scene이 필요한지 사람 승인할 때.

## ADR-WORLD-008 — 씬 편집 방식

**결정**  
후속 scene 생성/변경은 editor builder 또는 안전한 setup utility → validator → Unity 저장 → diff/Missing Reference → 사람 Game View 순서를 사용한다. 대규모 YAML 추측 편집은 사용하지 않는다.

**이유**  
현재 주요 scene이 binary serialization이며 이름만으로 참조 사용 여부를 판단할 수 없다.

**대안**  
직접 text/binary 수정 또는 기존 scene hierarchy 일괄 교체. 복구와 검증이 어렵다.

**장점**  
재현 가능하고 필수 오브젝트 계약을 자동화할 수 있다.

**위험**  
builder가 scene과 함께 새 권위가 되거나 manager를 중복 생성할 수 있다.

**재검토 조건**  
Unity가 공식 serialization 경로를 바꾸거나, builder가 기존 scene 참조를 보존하지 못한다는 증거가 있을 때.
