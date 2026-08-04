# Project P.A. Placeable 에셋 제작 가이드

갱신: 2026-07-17
적용 대상: 상점 가구, 작업대, 보관함, 건물, 간판, 기능 소품

## 1. 에셋 정의 전에 답할 질문

1. 플레이어가 이 물건을 왜 놓는가?
2. 낮 생활과 밤 상점 중 어느 루프에 연결되는가?
3. 외형만 보고 진열/가공/보관/장식 역할을 이해할 수 있는가?
4. 어느 zone과 surface에 놓이는가?
5. footprint, 사용 공간, 상호작용 방향은 각각 몇 셀인가?
6. NPC가 접근해야 하는가, 플레이어만 쓰는가?
7. 회수할 때 보존해야 할 기능 상태와 내용물은 무엇인가?

답이 없는 소품은 무작위 장식으로 추가하지 않는다.

## 2. Placeable Definition 필수 항목

| 항목 | 규칙 |
|---|---|
| Stable ID | 에셋명을 바꿔도 저장 호환이 유지되는 문자열 |
| Display name | 플레이어 UI용 짧은 한국어 이름 |
| Category | Display, Workbench, Storage, Building, Sign, Decor 등 |
| Allowed zone | `shop.interior`, 향후 `village.outdoor` 등 |
| Surface | Floor가 기본; Wall/Table/Ceiling은 별도 규칙 전 금지 |
| Footprint | collider를 덮는 최소 셀 offset 목록 |
| Clearance | 문, 서랍, 작업 정면, 고객 서는 자리 |
| Interaction | 실제 접근/상호작용 셀; clearance와 다를 수 있음 |
| Rotation | 기본은 90도 단위만 |
| Move/Recover | 기능상 가능한지 명시 |
| Navigation | obstacle/carving 필요 여부 |
| Functional state | 재고, 처리 중 작업, 업그레이드 등 저장 대상 |
| Tags | 기능·스타일·티어 검색용 |

## 3. 모델 제작 기준

Unity 기준:

- 1 Unity unit = 1m
- 바닥에 닿는 면의 Y=0을 권장
- 피벗은 바닥 중심 또는 정의된 셀 앵커와 관계가 명확한 위치
- 기본 전면은 local -Z로 간주한다. clearance가 이 방향에 작성된다.
- 스케일은 프리팹 루트 `(1,1,1)`을 목표로 한다.
- 노멀 방향, 음수 스케일, 겹친 면, 떠 있는 조각을 정리한다.
- 재질 슬롯은 실제 재질군 단위로 최소화한다.
- LOD는 카메라에서 체감되는 크기와 반복 배치 수를 근거로 추가한다.

Blender 수정본은 원본을 덮어쓰지 않는다. `.blend` 소스, export FBX, Unity prefab의 관계와 출처를 provenance 문서에 기록한다.

### Tripo 추정 에셋의 수정 경계

- 먼저 `TRIPO_ASSET_AUDIT.md`의 1~8 중 하나로 개별 분류한다. “임시”라는 이유만으로 삭제·전면 재제작하지 않는다.
- 플레이어/주민은 Placeable이 아니다. 외형을 보존하고 스케일, 접지, Avatar, 리깅/Animator, 그림자, collider, interaction/NavMeshAgent를 Unity에서 먼저 보정한다.
- 창고·작업대·건축물은 현재 실루엣보다 실제 저장/가공/출입 기능과 공간 맥락을 우선한다. 기능과 외형이 어긋나면 기존 모델 기반 재구성 또는 별도 수정본을 허용한다.
- Unity로 해결할 수 있는 부모 피벗, 스케일, 재질, collider, LOD, prefab hierarchy, 기능/접근 anchor는 원본 FBX를 건드리지 않는다.
- 실루엣·비율·UV·노멀·폴리곤·메시 분리/결합·웨이트가 원인일 때만 Blender를 사용한다. `.blend`와 export를 별도 경로에 보존하고 기존 참조를 안전하게 마이그레이션한다.
- 출처·상업 이용 범위가 확인되지 않은 모델은 플레이어 런타임 경로와 최종 빌드에서 제외하거나 사용자 증빙을 기다린다. 임의 재연결 금지.

## 4. 콜라이더와 footprint

- collider는 모델의 사용 가능한 실루엣과 맞아야 한다.
- 장식용 돌출부까지 큰 BoxCollider 하나로 덮어 보이지 않는 벽을 만들지 않는다.
- 복잡한 정적 가구는 여러 primitive collider를 쓸 수 있지만 MeshCollider 남용은 피한다.
- footprint는 모든 물리 collider의 수평 bounds를 포함한다.
- 경계에 거의 닿는 경우 반 셀을 억지로 줄이지 말고 올림한다.
- collider 변경 시 footprint도 다시 계산·검증한다.

현재 2m 셀 예시:

- ShopSlot 1.05×0.85m → `1×1`
- B05 Workbench 3.2×2.6m → `2×2`
- B06 Kitchen 3.4×2.8m → `2×2`
- B07 Forge 4.4×3.0m → `3×2`
- B08 Sewing 3.4×2.6m → `2×2`
- B09 Shed 원본 7×5.5m → 최대 `4×3`; 메인 씬 보정 콜라이더는 `3×3`. `village.outdoor` 전용, 상점 실내 금지

## 5. clearance와 상호작용

footprint와 clearance를 합치지 않는다.

- 진열대: 고객/플레이어가 서는 앞 1열
- 작업대: 상판 정면 1열, 문·서랍이 열리면 그 범위 포함
- 보관함: 뚜껑/문과 플레이어가 서는 공간
- 건물: 출입문 앞과 문에서 도로까지 연결되는 통로

회전하면 footprint, clearance, interaction이 모두 같은 90도만큼 회전해야 한다.

## 6. 기능 컴포넌트

프리팹은 새 가짜 기능을 만들지 말고 기존 컴포넌트를 사용한다.

- 판매 가구: `ShopSlot`
- 제작/가공: `Workbench`와 실제 `WorkbenchType`
- 보관: `StorageBox`
- NPC 회피: `NavMeshObstacle` + `carving=true`
- 플레이어 상호작용: 기존 `IInteractable`
- 야외 건축: `BuildingData`, 청사진 Item, `BuildingRegistry`

배치 preview에서는 collider, obstacle, 기능 MonoBehaviour를 끄고 실제 배치가 확정된 뒤 원래 상태로 복원한다.

## 7. 회수 안전성

- ShopSlot 상품은 인벤토리로 되돌아간 것을 확인한 뒤 회수한다.
- 내용물이 든 StorageBox는 자동 폐기하지 않는다.
- 동적 가구는 설계도를 반환할 공간이 있을 때만 제거한다.
- 고정 핵심 가구는 삭제 대신 recovered 상태로 비활성 보존한다.
- 저장 ID가 바뀌는 hierarchy 재구성은 마이그레이션 없이 금지한다.

## 8. 비주얼 기준

`Docs/Codex/ART_DIRECTION.md`를 먼저 적용한다.

- 따뜻한 생활형 마을의 둥근 실루엣과 낮은 채도 목재/크림/세이지 계열
- 무작위 도구, 이유 없는 병·상자, 과도한 미세 장식 금지
- 원시 큐브 조합이 최종 가구처럼 보이면 안 된다.
- AI 생성 흔적, 재질 스타일 불일치, 실제 기능과 다른 가짜 손잡이·문을 금지한다.
- Tripo 추정 에셋은 `TRIPO_ASSET_AUDIT.md`의 유지/수정/교체 분류를 먼저 거친다.

Grid line과 반투명 셀 quad는 배치 모드 피드백이며 월드 장식 에셋이 아니다.

## 9. 프리팹 체크리스트

- [ ] 역할과 실제 시스템 연결이 문서화됨
- [ ] 루트 scale 1, 바닥 접지, 피벗/전면 명확
- [ ] collider와 모델 일치
- [ ] footprint가 collider bounds를 포함
- [ ] clearance/interaction이 문·작업 방향과 일치
- [ ] 0/90/180/270도 모두 구역 안에서 검증
- [ ] NavMeshObstacle carving 여부 결정
- [ ] NPC/플레이어 접근 가능
- [ ] 이동 후 기존 기능 유지
- [ ] 회수 시 내용물 손실 없음
- [ ] zone/cell/rotation/definition/instance/function 저장 복원
- [ ] 출처·라이선스·수정 원본 기록
- [ ] 게임 카메라 캡처에서 스타일·스케일·겹침 확인

## 10. 현재 허용 카탈로그

| 정의 | P2 실내 | 비고 |
|---|---|---|
| `shop.shelf` | 허용 | 기본 6개, Tier 2부터 추가 배치, 물리 한도 6/6/8/12/20, 1×1, 이동 후 전면 clearance |
| `Blueprint_B05_Workbench` | 허용 | Tier 1 장부 보상, 2×2, 가공 준비 |
| `Blueprint_B06_KitchenStation` | Tier 2부터 허용 | Tier 2 장부 보상, 2×2, 조리 |
| `Blueprint_B07_BlacksmithForge` | Tier 1부터 허용 | Tier 1 장부 보상, 3×2. 기본 5×4에서는 빈 진열대 두 칸 이상을 회수해 연속 공간과 전면 통로 확보 필요 |
| `Blueprint_B08_SewingTable` | Tier 3부터 허용 | Tier 3 장부 보상, 2×2, 포장/재봉 |
| `Blueprint_B09_StorageShed` | `village.outdoor` 허용 | 외부 공동/추가 창고. 실제 collider 9~12셀, 문 앞 1열 clearance, StorageBox 24칸, 내용물 저장, 빈 추가 창고만 회수 |

새 가구는 이 표에 임의로 추가하지 말고 Definition, 저장, 회수, 카메라 검증을 함께 완료한다.
