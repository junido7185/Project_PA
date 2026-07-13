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
| **S2 (완료, 2026-07-13 오후)** | 실내 `Shop` 등록 + `PA_ShopLocator.FindPlazaShop()`(y<50 지상 최근접)로 앵커 5곳(간판/드레싱/마을변화) 오염 차단 | — |
| **S3 (완료, 2026-07-13 오후)** | 손님 실내 방문 — `InteriorCustomerController` 사이드카: 영업 중+실내 진열 존재 시 지상 Idle 주민을 실내 아일랜드로 워프 초대 → `NpcController.TryBeginShoppingVisitAt`(신규 훅)로 **기존 FSM 그대로** 둘러보기/구매 → Idle 복귀 시 원위치·원상점 복원(`RetargetShop`) | S2, NavMesh 리베이크(실내 아일랜드) |
| S4 | 외부 가판대 → 실내 상점 전환/병행 티어 연출 (Tier 1 해금과 연결) | TierService |
| S5 | 실내 인테리어 실모델化 (선반/카운터/조명, PA_DemoProps 재사용) + 진열 폴백 큐브 개선 | — |

## 4-b. S2/S3 실증 (2026-07-13)

- `PA_InteriorCustomerValidator` PASS: **NPC_Blacksmith 초대 입장(y=100) → 둘러보기 → 15G 구매(500→515G) → 퇴장 복귀(Idle)**. Day 1 튜토리얼은 개입 제외(검증 포함).
- 회귀 5종 PASS: FinalRoute / DayNight / CoreSlice / EnterableShop / CustomerArrival.
- 시각 증거: `Logs/DemoViewShots/inside_shop_20260713_163425.png` — 실내 그리드 4종 진열(가격 라벨) + 손님이 진열대 앞 쇼핑 중.
- 진단 기록: 온보딩 모달의 `Time.timeScale=0` 이 에이전트 경로를 영구 pending 시킴 → 검증기에서 세션 복원으로 해제 (다른 Play Mode 검증기 공통 주의점).

## 5. 다음 작업 3개

1. S5: 실내 벽/진열대 실모델化 + 진열 표시를 아이콘/모델 기반으로 (녹색 폴백 큐브 제거).
2. S4: Tier 1 해금과 실내 상점 전환 연출 연결.
3. 실내 동시 손님 2명 + 대기 줄 연출 (`maxConcurrentVisitors` 확장).
