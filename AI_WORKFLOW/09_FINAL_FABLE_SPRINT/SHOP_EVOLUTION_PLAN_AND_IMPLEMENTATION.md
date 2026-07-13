# SHOP_EVOLUTION_PLAN_AND_IMPLEMENTATION — 가판대 → 들어갈 수 있는 잡화점

작성: 2026-07-13 (Fable 5), 커밋 `f3ef51a`(구조) + `3bf30c9`(검증)

## 1. 현재 가판대 구조 (유지)

- 광장 판매대: `Shop` + 자식 `ShopSlot` 4개 (진열→`ShopPriceUI` 가격→NPC 구매 게이트). Day 1~ 데모 루트의 기준이며 **그대로 보존**.

## 2. 이번에 구축한 enterable shop 구조 (실제 씬에 존재)

`BuildingEntrance`의 단일 씬 Y+100 실내 관례(Docs/08, Moonlighter식)를 그대로 재사용:

```
[외부] PA_StoreDoor_Out (B10_Cottage_01 광장면 앞, "잡화점" 간판)
   └─ BuildingEntrance → PlayerSpawn_Inside      ("잡화점 들어가기")
[실내] PA_StoreInterior @ (0, 100, -40)
   ├─ Floor/Wall x4 (콜라이더 있는 방 12x9m) + 웜 포인트 라이트 2
   ├─ SalesGrid — InteriorShopSlot_{row}_{col} 2행x3열 = ShopSlot 6개
   │    · 진열대 테이블 비주얼 + 기존 ShopSlot 그대로 (진열/가격/구매/저장 호환)
   └─ Door_In └─ BuildingEntrance → PlayerSpawn_Outside ("잡화점 나가기")
```

- **기존 시스템 재사용률 100%**: BuildingEntrance(워프/페이드/카메라 스냅), ShopSlot(진열·가격·구매·품절), ShopPriceUI, SaveManager(계층 경로 키 기반이라 실내 슬롯 6개 자동 저장/복원).
- 실내 슬롯은 의도적으로 `Shop` 미등록 → 기존 `FindFirstObjectByType<Shop>` 앵커(간판/드레싱/VC-001A)와 NPC 슬롯 탐색에 영향 0. (Awake 경고 6건은 알려진 의도)

## 3. 실증 (`PA_EnterableShopValidator`, PASS)

외부 문 상호작용 → 실내 워프(y=100.1) → 실내 슬롯 핫바 진열 → `ShopPriceUI` 열림 → 출구 문 → 외부 복귀(y=0.1). 회귀 4종(FinalRoute/DayNight/CoreSlice/SaveRoundTrip) 동시 PASS.

## 4. 문라이터식 최종 구조까지 남은 단계

| 단계 | 내용 | 선행 |
|---|---|---|
| S1 (완료) | 실내 공간 + 양방향 문 + 슬롯 그리드 + 가격/저장 호환 | — |
| S2 | 실내 `Shop` 등록 + 그리드 편집(슬롯 추가/이동) 데이터화 | 앵커 로직을 "가장 가까운 Shop"으로 교체 |
| S3 | NPC 실내 진입: 실내 NavMesh(별도 Surface) + 문 통과 오프메시 링크 or 워프 연출 | S2 |
| S4 | 외부 가판대 → 실내 상점 전환/병행 티어 연출 (Tier 1 해금과 연결) | S2, TierService |
| S5 | 실내 인테리어 실모델化 (선반/카운터/조명, PA_DemoProps 재사용) | — |

## 5. 다음 작업 3개

1. S2: 실내 Shop 등록 + Shop 앵커 탐색을 플레이어 최근접 기준으로 교체 (위험 파일 3곳 소규모).
2. S3: 실내 NavMeshSurface + `CustomerArrivalController` 확장으로 손님 실내 방문.
3. S5: 실내 벽/진열대를 PA_DemoProps 실모델로 교체.
