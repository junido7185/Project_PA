# Project P.A. 커스터마이징 로드맵

갱신: 2026-07-17
원칙: 핵심 게임 루프를 끊지 않고, 각 단계마다 실제 플레이 가능한 최소 구역을 완성한다.

## 단계 현황

| 단계 | 범위 | 상태 | 완료 증거 |
|---|---|---|---|
| P1 | 기존 Grid/Build/Shop/Save/에셋 감사 | 완료 | `PLACEMENT_SYSTEM_ARCHITECTURE.md` §2 |
| P2 | 상점 실내 1×1/다중 셀 배치·회전·이동·회수·저장 | 완료 | D3D11 `PA_ShopCustomizationValidator` PASS |
| P3 | 마을 야외 건물·기능 소품 zone | 완료 | D3D11 `PA_OutdoorPlacementValidator` PASS |
| P4 | NPC 접근점·예약 셀·실제 NavMesh 도달성 | 완료 | D3D11 `PA_ShopCustomizationValidator` + `PA_InteriorCustomerValidator` PASS |
| P5 | 상점 진화와 배치 해금·프리셋 | 완료 | D3D11 `PA_ShopProgressionUnlockValidator` + v10/회귀 PASS |
| P6 | 집/마당·장식 surface·UX 고도화 | 백로그 | 미착수 |

## P1 — 기존 구조 감사

완료:

- `GridService` 2m 단일 셀, `BuildManager` 전방 배치, 청사진과 `BuildingData` 연결 확인
- PA_StoreInterior 12×9m, 2×3 ShopSlot 배치와 문/스폰 확인
- B05 3.2×2.6m → 2×2, B09 7×5.5m → 현 실내 부적합 확인
- ShopSlot Transform 기반 NPC 목적지와 hierarchy 기반 저장 확인
- 저장 v9 복원 순서 확인

## P2 — 상점 실내 MVP

완료:

- `shop.interior` 5×4 zone과 보호 입구/서비스 경로
- owner 기반 footprint/clearance 점유
- 90도 단위 회전
- 실제 프리팹 preview와 셀 색상 피드백
- 기존 6개 진열대 이동, 안전 회수, 최소 2개 보호
- B05~B08 실내 작업대 정의; 실제 B05 배치 검증
- 이동 후 ShopSlot NPC 구매와 Workbench 기능 유지
- carving NavMeshObstacle
- 배치 장부 월드 상호작용과 screen UI
- 기본 B05 청사진 1회 스타터 지급
- 저장 v10과 v9→v10 마이그레이션
- 동일 구도 캡처 기반 UI 여백/벽 가림 보정

P2에서 의도적으로 남긴 것:

- 커서 raycast 배치
- 명시적 NPC approach Transform/예약
- 벽·테이블·천장 surface
- undo/redo와 프리셋
- 외부 창고 B09(P3로 이관했으며 현재 완료)

## P3 — 마을 야외 기능 배치

목표: 낮 활동에서 얻은 기능 오브젝트를 마을에 놓고 상점 운영 준비와 연결한다.

구현 순서:

1. `village.outdoor` zone과 도로/광장/건물 입구 보호 셀 정의
2. 기존 `BuildManager`를 zone owner/다중 footprint API에 연결하되 기존 1셀 호환 유지
3. B09 창고 역할 결정: 외부 보관함인지 입장 건물인지 먼저 확정
4. 창고의 실제 StorageBox 내용물 저장과 안전 회수
5. B05 작업대는 “낮 가공→밤 진열” 기능으로 외부/실내 허용 규칙 분리
6. 작은 장식은 기능·동선 문제가 해결된 뒤 추가

완료 기준:

- 도로와 핵심 건물 입구를 막을 수 없다.
- 건물 footprint와 collider가 맞는다.
- 저장/재시작 뒤 같은 zone/cell/rotation/기능 상태다.
- B09는 산업용 임시 창고가 아니라 Project P.A.의 생활형 보관 기능과 외형이 일치한다.

완료:

- 96m Ground를 덮는 2m `village.outdoor` 47×47 zone과 212개 보호 셀
- 도로·상점 광장·실외 입구·Player/Hiring spawn 보호
- 기존 `BuildManager`의 신규 건설을 다중 footprint/owner 점유에 연결하고 legacy fallback 유지
- 플레이어 전방 preview, R 회전, 클릭 확정, M 가까운 건물 이동, X 빈 추가 건물 회수
- B09를 문 앞에서 사용하는 24칸 외부 공동 창고로 확정
- 기본 B09는 이동 가능/회수 불가, 추가 B09는 물품이 없을 때만 회수
- B09 내용물·zone/cell/rotation을 기존 v10 sidecar에 저장·복원
- 레거시 `[WorldBuildings]` B09 중복 렌더러/충돌 런타임 비활성화
- 동일 구도 게임 카메라에서 목재 실루엣·이중문·플레이어 접근·조작 안내 확인

## P4 — NPC와 상호작용 정밀화

완료:

- 정의별 기존 `interaction` 셀을 실제 ShopSlot 접근점으로 연결
- 한 슬롯에 한 NPC owner만 이동 중 예약하고, 다른 손님은 별도 도달 가능한 슬롯을 선택
- `NavMesh.SamplePosition`과 `CalculatePath=PathComplete`를 통과한 목적지만 사용
- 이동·회전된 선반의 앞셀도 같이 회전하고, 가구 이동 중 낡은 예약은 다시 선택
- 고객이 예약점에 멈춰 진열대를 바라본 뒤 기존 구매/거절 평가 실행
- 실제 실내 고객 예약→접근→15G 구매→퇴장과 Day 1 30G 판매 회귀 검증

P4에서 의도적으로 남긴 것:

- B05 작업대 사용 시 플레이어 상판 정면 정렬과 짧은 작업 피드백은 기능 아트 태스크로 이관
- 예약은 이동 중 일시 상태이므로 저장하지 않음
- 탑다운 실내 캡처에서 두 접근점 분리는 기능 로그보다 덜 선명하며 카메라/실내 가림 개선 시 재검토

## P5 — 진화·해금

완료:

- Tier 0/1 `5×4`, Tier 2 `6×5`, Tier 3+ `7×6`로 동·북 방향만 단조 확장하며 기존 원점과 placement ID/셀/회전을 보존
- 물리 진열 한도 `6/6/8/12/20`과 Tier 2부터 기존 authored 선반 복제 배치 연결
- B05+B07/B06/B08을 Tier 1/2/3 장부 보상과 카탈로그 해금으로 연결. B07은 BuildingData·설계도·ToolSet 레시피/상품의 기존 Tier 1 권위에 정합
- 확장 바닥 전용 NavMeshSurface와 동·북 NavMeshLink를 구성하고 멀리 있는 확장 셀까지 `PathComplete` 검증
- 기존 Processed 마을 변화가 활성일 때만 따뜻한 공방 테마를 해금하고 벽·가구·조명·간판에 적용
- 테마를 v10의 특수 `shop.theme/fixed.shop.theme` placeable 레코드로 저장·복원
- Tier 0/3 동일 게임 카메라 캡처를 비교해 확장 스케일·통로·조명·색상·UI를 확인하고 상태 문구 겹침 보정

## P6 — 장기 UX

- 마우스/패드 셀 포인팅
- undo/redo, 최근 회수 목록
- 카테고리/검색/보유 수량
- 벽걸이·테이블 위·천장 surface
- 저장 가능한 상점 프리셋
- 접근성 색상/패턴, 패드 포커스, 현지화 길이 대응

## 다음 작업 우선순위

1. Task 044 기존 Dialogue/Demand 기반 주민 의뢰 표시 설계
2. P6 집/마당 zone은 핵심 Day 3 연속 플레이 연결 뒤 착수
3. 벽·상판 surface, undo/redo, 프리셋은 기능 루프가 요구할 때 단계적으로 추가

검증보다 구현 완성을 우선하되, 각 단계마다 최소 한 번은 게임 카메라 캡처와 실제 저장 라운드트립을 남긴다.
