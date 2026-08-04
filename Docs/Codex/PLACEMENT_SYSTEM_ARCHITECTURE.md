# Project P.A. 배치 시스템 아키텍처

갱신: 2026-07-17
상태: P1 감사, P2 상점 실내, P3 마을 야외, P4 NPC 접근·예약·도달성, P5 상점 진화·해금 구현·검증 완료

## 1. 목적과 설계 원칙

배치는 단순 장식 편집기가 아니다. 낮에 준비한 상품과 설비가 밤 상점 운영 방식, NPC 접근, 저장 상태에 연결되는 장기 시스템이다.

핵심 원칙은 다음과 같다.

- 기존 `GridService`가 유일한 셀 권한자다. 별도 실내 그리드를 만들지 않는다.
- 기존 `Inventory`, `Item.buildingToBuild`, `ShopSlot`, `Workbench`, `StorageBox`, `NavMeshObstacle`, `SaveManager`를 재사용한다.
- 외형, 물리 footprint, 사용 clearance, 상호작용 셀을 분리한다.
- 문·스폰·핵심 통로를 보호하고, 배치 후에도 입구에서 서비스 셀까지 4방향 경로가 남아야 한다.
- 배치가 성공한 뒤에만 설계도를 소비한다. 회수 실패 시 상품·설계도·가구를 잃지 않는다.
- 메인 씬이나 프리팹 YAML을 직접 편집하지 않고 런타임 sidecar로 연결한다.

## 2. 기존 시스템 감사 결과

| 기존 시스템 | 감사 결과 | 이번 확장 |
|---|---|---|
| `GridService` | 월드 원점 기준 2m, 단일 셀 HashSet | 기존 API 유지 + zone/owner/footprint/clearance/path 추가 |
| `BuildManager` | 플레이어 전방 배치, R/클릭, 1셀, 물리 겹침 | `village.outdoor` owner/다중 footprint, R/클릭, M 이동, X 안전 회수로 확장; legacy fallback 유지 |
| `BuildingData` / 청사진 Item | 프리팹·가격·티어 연결 | B05~B08 정의를 실제 청사진에서 런타임 파생 |
| `ShopSlot` | 상품, 가격, NPC claim/구매, hierarchy save key | 객체 자체를 이동하므로 판매 타깃과 저장 키 유지 |
| `NpcController` | `ShopSlot.transform.position`으로 이동하고 도착 후 claim | P4에서 회전된 interaction 앞셀을 먼저 예약하고 완전한 NavMesh 경로로 이동한 뒤 기존 claim/구매를 수행 |
| B05 Workbench | Collider 3.2×2.6m, Workbench, carving obstacle | 2×2 footprint로 실제 배치 |
| B09 StorageShed | 7×5.5m 원본, 실제 씬 콜라이더 9셀, StorageBox 24칸, carving | 외부 공동 창고로 확정. 정적 B09 이동 가능/회수 불가, 추가 B09는 빈 상태에서 회수 가능 |
| `SaveManager` v9 | 건축·인벤토리·ShopSlot 등 복원 | v10 placeables sidecar와 마이그레이션 추가 |

## 3. 좌표계와 구역

현재 셀 크기는 기존과 같은 `2.0m`다.

상점 실내 구역:

| 항목 | 값 |
|---|---|
| Zone ID | `shop.interior` |
| 기준 Transform | `PA_StoreInterior` |
| 로컬 첫 셀 중심 | `(-4, 0, -3)` |
| 크기 | Tier 0/1 `5×4`, Tier 2 `6×5`, Tier 3+ `7×6` 셀 |
| 셀 중심 X | -4, -2, 0, 2, 4 |
| 셀 중심 Z | -3, -1, 1, 3 |
| 보호 입구 | `(2,0)` |
| 서비스 경로 목표 | 현재 zone 북쪽 `(2,height-1)` |

`WorldToZoneCell`은 zone root의 역변환 뒤 첫 셀 중심을 기준으로 반올림한다. 다중 셀 가구의 실제 Transform은 회전된 footprint 셀 중심의 평균으로 정한다.

마을 야외 구역:

| 항목 | 값 |
|---|---|
| Zone ID | `village.outdoor` |
| 기준 Transform | 런타임 `PA_OutdoorPlacement` |
| 월드 첫 셀 중심 | `(-46, 0, -46)` |
| 크기 | `47×47` 셀, 기존 96m Ground 범위 |
| 보호 마스크 | 남북/동서 도로, 상점 광장, 실외 BuildingEntrance, Player/Hiring spawn |
| 현재 보호 셀 | 212개(D3D11 검증 기준) |

야외는 큰 단일 BFS로 장식 자유도를 과도하게 제한하지 않는다. 실제 도로·광장·입구를 protected cell로 예약하고, 각 건물의 footprint와 문 앞 clearance를 owner 점유로 관리한다.

### 외부/Tripo 에셋 온보딩 게이트

에셋이 존재하거나 예쁘게 보인다는 이유만으로 Placeable Definition에 등록하지 않는다. `TRIPO_ASSET_AUDIT.md`의 1~8 판정과 출처를 먼저 확정하고, 원본을 보존한 Unity 래퍼 또는 별도 Blender 수정본에서 다음을 함께 만족해야 한다.

1. 1 unit=1m 스케일, Y=0 접지, local -Z 전면과 격자 피벗이 명확하다.
2. 보이는 메시와 collider가 일치하고 footprint, clearance, interaction/NPC approach가 분리되어 있다.
3. 0/90/180/270도 회전에서 모든 셀과 접근 방향이 함께 변한다.
4. `ShopSlot`, `Workbench`, `StorageBox` 등 기존 기능과 stable ID/저장 상태를 유지한다.
5. 이동·회수·저장 복원 뒤 기능과 내용물을 잃지 않으며 핵심 통로를 막지 않는다.
6. 아트 방향, 라이선스, 원본/수정본 경로, 게임 카메라 Before/After가 기록된다.

이 게이트를 통과하지 않은 B11/B12 같은 정적 세계 실루엣은 플레이어 배치 카탈로그에 노출하지 않는다. 캐릭터는 Placeable이 아니며 외형 정체성 보존과 별도 접지/Avatar/NavMeshAgent 감사를 따른다.

## 4. 셀 마스크 모델

각 Placeable Definition은 다음 데이터를 갖는다.

- `allowedZone`: 배치 가능한 구역
- `surfaceType`: 현재 `Floor`
- `footprint`: 실제 물리·동선 차단 셀
- `clearance`: 다른 가구가 침범할 수 없는 전면 사용 셀
- `interaction`: 플레이어/NPC가 접근해야 할 셀; P2에서는 clearance와 같은 값
- `rotate90`: 90도 회전 가능 여부
- `movable`, `recoverable`
- `blocksNavigation`
- 기능/스타일 태그

회전은 앵커를 중심으로 offset에 0/90/180/270도를 적용한다. footprint와 clearance가 같은 규칙으로 회전하므로 모델만 돌고 상호작용 면이 남는 오류를 막는다.

## 5. 점유와 검증 순서

`GridService` zone은 셀→owner와 owner→셀을 양방향으로 보관한다.

배치 검증 순서:

1. zone, owner ID, footprint 존재
2. 모든 footprint가 구역 안인지 확인
3. 보호 셀 침범 거부
4. 다른 owner의 footprint 겹침 거부
5. 다른 가구의 clearance 침범 거부
6. 새 가구 clearance가 구역 밖이거나 다른 footprint에 막히는지 확인
7. 현재 owner의 이전 점유를 무시하고 pending footprint를 넣은 BFS 실행
8. 입구 `(2,0)`에서 서비스 `(2,3)`까지 길이 남을 때만 점유 커밋

기존 6개 진열대는 제작된 기본 레이아웃을 깨지 않도록 최초 등록 시 footprint만 grandfather한다. 사용자가 한 번 이동한 뒤에는 전면 clearance와 통로 규칙을 모두 적용한다.

## 6. 플레이어 경험

상점 뒤쪽의 실제 월드 오브젝트 `상점 배치 장부`에 Space로 상호작용한다.

- 장부 열기/닫기
- 보유 청사진 또는 회수 가구 이전/다음 선택
- 선택 가구 미리보기
- 가까운 기존 가구 이동
- 가까운 기존 가구 안전 회수
- WASD로 플레이어 위치·방향을 바꿔 대상 셀 선택
- R로 90도 회전
- 바닥 클릭으로 확정

그리드 선, footprint 녹색/적색, clearance 황색, 보호 입구 주황색을 표시한다. 새 가구 미리보기는 실제 프리팹을 사용하고 collider·상호작용·NavMeshObstacle만 잠시 끈다.

첫 장부 사용 시 보유/배치된 B05가 없으면 실제 `Blueprint_B05_Workbench` 1개를 지급한다. 가방이 가득 차면 지급 플래그를 세우지 않아 한 칸을 비운 뒤 다시 받을 수 있다.

## 7. 이동·회수 트랜잭션

이동:

1. 대상의 원래 셀·회전은 런타임 record에 유지한다.
2. preview 동안 collider와 NavMeshObstacle을 끈다.
3. 새 셀이 유효할 때 owner 점유를 원자적으로 교체한다.
4. 실패/닫기 시 원래 Transform과 충돌 상태를 복원한다.

회수:

- 상품이 든 `ShopSlot`은 먼저 `RetrieveItem`; 가방이 가득 차 여전히 상품이 있으면 중단한다.
- 내용물이 든 `StorageBox`는 회수 거부한다.
- 동적 Workbench는 설계도를 먼저 돌려줄 수 있을 때만 제거한다.
- 기존 고정 진열대는 GameObject를 삭제하지 않고 recovered 비활성 상태로 보존한다.
- 상점 운영 최소 기능을 위한 앞의 진열대 2개는 이동 가능하지만 회수 불가다.
- 야외 기본 B09는 마을의 마지막 공동 저장소라 이동만 가능하다.
- 플레이어가 건설한 B09는 비어 있고 설계도를 받을 가방 공간이 있을 때만 회수한다.

## 8. Shop/NPC/NavMesh 연동

- `ShopSlot`의 부모 hierarchy를 바꾸지 않으므로 `Shop` 등록과 save key가 유지된다.
- `ShopCustomizationController`가 private 배치 정의의 회전된 `interaction` 셀을 월드 접근점으로 읽기 전용 노출한다.
- `ShopCustomerApproachController`는 NPC owner별로 한 `ShopSlot` 앞자리를 예약한다. 이동 중인 두 손님은 같은 슬롯 목적지를 공유하지 않는다.
- 접근 셀은 다른 가구 footprint로 막혀 있으면 후보에서 제외하며, 등록된 실내 가구는 임의 fallback 위치로 우회하지 않는다.
- `NavMesh.SamplePosition` 뒤 `NavMesh.CalculatePath`가 `PathComplete`인 접근점만 실제 목적지로 사용한다.
- 이동/회전 중 접근점이 바뀌면 기존 예약을 무효화하고 다음 도달 가능한 슬롯을 다시 선택한다.
- 도착 후 NPC는 진열대를 바라보며 기존 `ShopSlot.TryClaim`과 `PurchaseEvaluator`를 그대로 사용한다.
- 기존 진열대에는 runtime carving `NavMeshObstacle`을 보강한다.
- B05~B08 프리팹의 carving obstacle은 그대로 유지한다.
- grid BFS는 논리 통로를, NavMesh carving은 실제 agent 회피를 담당한다.

P4 예약은 런타임 전용 이동 상태다. 결제 원자성은 기존 `ShopSlot` claim이 계속 담당하며 저장 스키마에는 예약을 기록하지 않는다.

P5 확장은 원래 5×4 영역을 움직이지 않고 동쪽과 북쪽에만 물리 바닥·벽을 추가한다. 확장 전용 `NavMeshSurface`는 동·북 `NavMeshLink`로 기존 baked 실내 island와 연결하며, 단순 `SamplePosition`이 아니라 입구/스폰에서 먼 확장 셀까지 `CalculatePath=PathComplete`를 완료 조건으로 사용한다.

물리 진열 한도는 `6/6/8/12/20`이다. 기존 6개 `ShopSlot`은 그대로 유지하고, Tier 2 이상에서만 상점 hierarchy 밖 비활성 템플릿을 복제해 실제 배치 인스턴스에 `ShopSlot` 기능이 등록된다. 템플릿 자체는 실내 진열 수·Shop 등록·저장 대상이 아니다.

야외 건물은 프리팹/인스턴스 `BoxCollider`에서 footprint를 계산하고 기존 carving obstacle을 유지한다. 메인 맵 B09가 존재하면 레거시 `[WorldBuildings]` B09는 런타임 비활성화해 중복 충돌을 없앤다.

## 9. 저장 v10

`SaveData.placeables`는 `zoneId`, `definitionId`, `instanceId`, `gridX/Y`, `rotationQuarterTurns`, `isFixed`, `recovered`, 기능 상태와 보관 아이템을 저장한다.

Processed 마을 변화로 해금되는 테마는 새 스키마 필드 없이 `definitionId=shop.theme`, `instanceId=fixed.shop.theme`, `functionalState=default|processed.warm`인 특수 v10 레코드로 보존한다. 이 레코드는 점유·프리팹 생성 대상이 아니다.

로드 시 인벤토리 이후, ShopSlot 상품 이전에 가구를 복원한다. 동적 프리팹을 재생성하고 owner 점유를 다시 검증한다. 위험한 레이아웃은 억지로 복원하지 않고 회수 상태로 격리한다.

세부 필드와 복원 순서는 `AI_WORKFLOW/03_TASKS/SAVE_SCHEMA.md`를 따른다.

## 10. 검증 결과

D3D11 `PA_ShopCustomizationValidator` 최종 PASS:

- 5×4 zone, 2m cell, 보호 입구
- 기존 ShopSlot 6개 등록
- 보호 셀·겹침 배치 거부
- 오른쪽 진열대 2개 안전 회수
- 실제 B05 2×2 배치와 Workbench 상호작용
- carving NavMeshObstacle 유지
- 1×1 진열대 `(4,0)`으로 이동 및 270도 회전
- 이동한 진열대에서 NPC 구매 61G, 경제 반영
- v10 저장 후 Workbench/진열대 셀·회전, 상품 2개, 가격 73G 복원
- 복원 후 보호 통로 유지, Workbench 회수

증거:

- `Logs/ShopCustomizationValidator_FinalUI.log`
- `Logs/ShopCustomization/20260716_103307/savegame.json`
- `Logs/ShopCustomization/20260716_103307/shop_customization_game_camera.png`

D3D11 `PA_OutdoorPlacementValidator` PASS:

- 47×47 `village.outdoor`, 보호 셀 212개
- 남북/동서 도로와 상점 광장 다중 셀 B09 배치 거부
- 메인 B09 9셀, StorageBox/상호작용/carving 유지
- 정적 B09 `(15,31)`/270° 이동, 추가 B09 `(31,28)`/90° 건설
- 물품이 든 추가 창고 회수 거부, 빈 창고 회수 성공
- 기본/추가 창고 내용물 2+1개와 zone/cell/rotation v10 복원
- SaveRoundTrip, ShopCustomization, FinalDemoRoute 30G 회귀 PASS

증거: `Logs/OutdoorPlacementValidator.log`, `Logs/OutdoorPlacement/20260716_111214/`, `Logs/OutdoorPlacement_*Regression.log`.

D3D11 P4 ShopSlot 접근/예약 PASS:

- 이동된 선반 `(4,0)`/270°의 interaction offset이 앞셀 `(3,0)`으로 함께 회전
- 해당 셀 `NavMesh.SamplePosition` 및 `CalculatePath=PathComplete`
- 첫 NPC가 이동 선반을 예약한 동안 두 번째 NPC의 같은 owner 예약 거부
- 두 번째 NPC는 별도 도달 가능한 선반을 예약해 동시 예약 2개 유지
- 실제 실내 방문 FSM이 예약→앞셀 정지→15G 구매→퇴장 완료
- CustomerArrival 동시 초대 제한과 FinalDemoRoute BreadLoaf 30G 회귀 유지

증거: `Logs/P4_ShopCustomizationValidator.log`, `Logs/P4_InteriorCustomerValidator.log`,
`Logs/P4_CustomerArrivalRegression.log`, `Logs/P4_FinalDemoRouteRegression.log`,
`Logs/DemoViewShots/p4_shop_approach_final_20260716_114141.png`.

D3D11 P5 상점 진화·배치 해금 PASS:

- Tier 0/1 `5×4`, Tier 2 `6×5`, Tier 3/4 `7×6`; 원점·기존 6개 배치 보존
- 물리 진열 한도 `6/6/8/12/20`, Tier 2의 7·8번째와 Tier 3의 9번째 실제 `ShopSlot` 배치
- B05+B07/B06/B08 Tier 1/2/3 보상·카탈로그 해금. B07은 기존 BuildingData·설계도·ToolSet의 Tier 1과 일치
- 확장 먼 셀까지 기존 실내 NavMesh와 완전 경로
- Processed 문화의 따뜻한 공방 테마 해금·v10 저장·clear·restore
- 동일 구도 Tier 0/3 게임 카메라 캡처와 진행 원장 UI 확인
- ShopCustomization, EnterableShop, SaveRoundTrip, FinalDemoRoute 회귀 PASS

증거: `Logs/P5_ShopProgression_D3D11_Release.log`, `Logs/ShopProgressionUnlock/20260717_005831/`, `Logs/P5_*Regression.log`.

## 11. 현재 한계

- 구현 zone은 `shop.interior`와 `village.outdoor` 두 곳이다. 집/마당/벽/상판 surface는 아직 없다.
- 마우스 포인터 raycast 셀 선택이 아니라 기존 BuildManager와 같은 플레이어 전방 방식이다.
- 벽걸이/테이블 위/천장 surface는 아직 없다.
- 자유각 회전, 미세 오프셋, undo stack, 다중 선택은 범위 밖이다.
- B09는 외부형 생활 창고로 확정했으며 실내 배치는 계속 금지다.
- 기본 2×3 선반 중 앞셀이 다른 선반 footprint로 막힌 후보는 의도적으로 접근 불가 처리된다. 사용자는 P2 배치 장부로 통로가 있는 배치로 이동할 수 있다.
- 접근 예약은 고객 이동 중 런타임 상태이며 저장 대상이 아니다.
- P5 상점 진화·해금은 완료했지만 사용자가 이름을 붙여 저장하는 다중 배치 프리셋은 P6 UX 백로그다.
