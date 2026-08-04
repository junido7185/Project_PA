# Project P.A. Architecture Decisions

최종 갱신: 2026-07-27

## ADR-001 — 실내 잡화점 해금은 Tier에서 파생한다

- 상태: 적용
- 결정: 별도 `interiorShopUnlocked` 저장 필드를 만들지 않고 `TierService.CurrentTier >= 1`을 실내 잡화점 해금의 원본으로 사용한다.
- 이유: Tier는 이미 v9 저장/로드 대상이며 단조 증가한다. 중복 저장 플래그는 불일치와 마이그레이션 위험만 늘린다.
- 결과: 로드 시 `ForceSetTier` 후 S4 프레젠테이션이 문·간판 상태를 조용히 재구성한다.

## ADR-002 — 기존 문 워프에 선택적 Tier 게이트를 추가한다

- 상태: 적용
- 결정: `BuildingEntrance`의 기존 워프를 교체하지 않고 기본값 0인 선택적 `requiredTier` 가드를 추가한다.
- 이유: 기존 외부/실내 양방향 워프, 페이드, 카메라 스냅을 그대로 보존하면서 Tier 0 조기 진입만 막을 수 있다.
- 결과: 다른 건물 문은 기존 동작 그대로이며, S4 컨트롤러가 외부 잡화점 문에만 런타임으로 Tier 1 요구를 설정한다.

## ADR-003 — 상점 진화는 런타임 사이드카로 연출한다

- 상태: 적용
- 결정: 메인 씬을 다시 저장하지 않고 `ShopEvolutionController`가 기존 `PA_StoreDoor_Out`과 `TierService.OnTierAdvanced`를 연결한다.
- 이유: 이미 검증된 실내 공간·Shop·슬롯·손님 FSM과 씬 직렬화를 보존한다.
- 결과: Tier 0 잠금 안내, Tier 1 간판/문 조명, 비차단 해금 패널만 추가된다. 경제·구매·NPC 판단은 변경하지 않는다.

## ADR-004 — Tripo 추정 에셋은 개별 감사 후 정체성을 보존하며 최종화한다

- 상태: 적용
- 결정: Tripo 추정 에셋을 “임시”라는 이유만으로 일괄 삭제·교체하지 않는다. `TRIPO_ASSET_AUDIT.md`의 1~8 분류로 사용처, 게임 카메라 노출, 기능, 스케일·피벗·접지, collider, footprint/clearance/interaction, 리깅·재질, 출처를 개별 판정한다.
- 캐릭터 경계: 플레이어와 주민은 심각한 메시/비율/리깅/라이선스 문제가 확인되지 않는 한 전체 외형을 유지한다. Unity 설정과 기존 애니메이션 보정이 우선이며 역할 외형 교체는 사용자 확인 대상으로 남긴다.
- 기능 가구 경계: 창고·작업대·진열대는 외형만으로 확정하지 않는다. 기존 Inventory/Storage/Crafting/Shop/Placement 권위와 실제 사용 방향, interaction/NPC approach, 저장 상태가 일치해야 한다. 기능 맥락이 틀리면 원본 보존 아래 재구성·교체가 가능하다.
- 제작 경계: 원본 FBX·텍스처는 덮어쓰지 않는다. Unity 부모/파생 프리팹을 우선하고 메시·UV·노멀·실루엣 자체가 원인일 때만 별도 Blender 소스/내보내기 산출물을 만든다. 현재 Blender는 미설치다.
- 배포 경계: 기능·시각 적합 판정은 라이선스 판정을 대체하지 않는다. C-01~C-09, Tripo walking, B01~B12의 생성 계정·생성일·상업 이용 증빙은 최종 배포 게이트다.

## ADR-005 — Placeable 기능과 시각 에셋은 같은 공간 계약을 사용한다

- 상태: 적용
- 결정: Grid 기반 커스터마이징은 기존 `GridService` 2m 셀과 zone/owner 권위를 사용한다. Tripo/외부 모델도 예외 없이 object footprint, clearance, interaction/NPC approach, pivot/front, collider, 회전, 회수, 저장 ID를 정의해야 한다.
- 이유: 고정 디오라마와 별도 배치 데모가 병존하면 가구 이동 시 Shop·Workbench·Storage·NPC 참조와 저장이 끊긴다.
- 결과: 현재 P1~P5는 실내 상점과 야외 B09, B05~B08 기능 가구, NPC 접근/예약, v10 복원을 연결한다. 집·벽·상판·표면·구조 커스터마이징은 기존 데이터/zone을 확장하는 P6 이후 범위이며 미구현 버튼을 노출하지 않는다.

## ADR-006 — B11 원형 분수 물리는 런타임 실메시로 정합한다

- 상태: 적용, 실제 이동 확인 대기
- 결정: 메인 씬·B11 래퍼·원본 FBX를 수정하지 않고 `DemoVisualDressingController`가 활성 B11의 Visual `MeshFilter`에 비볼록 정적 `MeshCollider`를 보장한 뒤 6×6 루트 `BoxCollider`를 비활성화한다.
- 안전 조건: Visual 메시가 하나도 없으면 기존 Box를 유지한다. 기존 캡슐형 carving `NavMeshObstacle`, 시각 모델, 광장 배치와 기능 역할은 변경하지 않는다.
- 이유: 사각 collider의 네 모서리가 보이는 원형 구조 밖에서 플레이어를 막는 반면, 실제 메시 collider는 보이는 경계와 물리를 일치시킨다.

## ADR-007 — B12 항구의 레거시 물리는 활성 인스턴스에서 축소 전용으로 정합한다 (Task 113)

- 상태: 적용, 실제 해안 이동/NPC 우회 확인 대기
- 결정: B12는 현재 정적 세계 구조로 유지한다. 활성 `B12_TradePort`의 Visual 로컬 메시 bounds를 계산해 루트 `BoxCollider`와 box형 `NavMeshObstacle`이 명백히 클 때만 각 축을 축소한다.
- 안전 조건: Visual 메시가 없으면 기존 물리를 유지한다. 어떤 축도 기존보다 키우지 않으며 FBX·래퍼·메인 씬·BuildingData·청사진을 수정하지 않는다.
- 이유: 기존 물리 감사에서 10×5m Box와 실제 약 3.63×1.96m Visual 사이 좌우 약 3.2m 투명 벽이 실측됐다. 씬의 Box 보정만으로는 원본 래퍼와 carving obstacle의 재발을 막지 못하므로 같은 Visual 근거로 플레이어 충돌과 NPC 경로 공백을 함께 제한한다.
- 기능 경계: Task 079 선행 조건 전에는 교역 상호작용, Placeable 등록, 저장 상태, 가짜 NPC 접근점을 추가하지 않는다.

## 보존 결정

- `PA_ShopLocator`의 지상/실내 Shop 구분 정책 유지.
- Day 1 튜토리얼의 상점 구매 예외 유지.
- 실내 손님은 `InteriorCustomerController`와 기존 `NpcController` 쇼핑 FSM을 계속 사용.
- 저장 스키마 v9, 메인 씬, 프리팹, 외부 패키지는 S4에서 변경하지 않음.
