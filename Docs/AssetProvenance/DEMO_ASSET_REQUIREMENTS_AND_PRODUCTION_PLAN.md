# ART-000 Demo Asset Requirements and Blender Production Plan

2026-09-06 · 제작 계획, 후속 티켓 구현 승인 아님.

기준: `CONTENT_CANON_BIBLE.md`, 최신 `CONTENT_CAMPAIGN_DAY1_30.md`·`CONTENT_IMPLEMENTATION_BACKLOG.md`, `PROJECT_PA_DEMO_IDENTITY.md`, `Docs/PROJECT_STATE.md`, 실제 `Assets/Scripts`·`Assets/Resources`·기존 prefab. 캠페인 초안의 **고용 전 실제 유료 매입→고용 후 반복 작업 위임** 순서를 읽었으며 예전 데모 문서의 고용 먼저 시연과 혼동하지 않는다. 인물 이름/성격/직업과 가격·고용비·Tier는 아트 티켓에서 재정의하지 않는다.

기존 WorldSandbox 일반 진입과 BETA-010 facing 137° 저장 복원은 별도 CONTENT/BETA 작업이다. ART-000의 에셋 검증으로 해소되지 않는다. CONTENT 문서가 구현 중이므로 아트 적용 시 해당 티켓의 최종 계약을 다시 대조한다.

## 1. 실제 gameplay에서 역산한 요구

FOUND_DIRECT는 필요한 시각 모델이 발견됐다는 뜻이며 무보정/최종 품질 승인이 아니다. CAN_DERIVE는 부품 분리·비율·구조 수정이 필요하고, NEED_NEW_MODEL은 요구를 전달하는 구조를 새로 만들어야 한다.

| Priority | 필요한 오브젝트 / stable proposed ID | 판정 | 발견된 근거 / 부족한 점 | 기존 owner / 실제 표시 조건 | 담당 |
|---|---|---|---|---|---|
| P0 | P.A. shop shell | FOUND_DIRECT | 기존 B01~B04 상점 prefab과 visual finalization; 전체 상점 재모델링 불필요 | Shop/BuildingEntrance, 기존 티어·슬롯 | ART-003 |
| P0 | `PA_PROP_SHOP_EMPTY_DISPLAY_01` | CAN_DERIVE | MiniMarket display-bread/display-fruit는 상품이 붙어 있음; 빈 골격과 removable tray 필요 | ShopSlot의 item/수량/price/품절을 읽어 별도 상품 visual 생성 | ART-003/008 |
| P0 | checkout visual | FOUND_DIRECT | MiniMarket cash-register; 운영자면/고객면 정의 필요 | 기존 Shop, 금액은 EconomyService/SalesLog에만 있음 | ART-003 |
| P0 | `PA_PROP_SHOP_PRICE_HOLDER_01` | NEED_NEW_MODEL | 슬롯별 가독성 있는 빈 가격표 받침과 anchor가 없음 | ShopPriceUI/ShopSlot 표시값, 고정 가격 texture 금지 | ART-003/008 |
| P0 | `PA_PROP_PRODUCER_CRATE_01` | CAN_DERIVE | shopping-basket/Chest/바구니 부품; 비어 있는 내부·분리 덮개·적재 anchor 필요 | ProducerNpcController 실제 bag/납품 상태; DayPlan 공급을 특정 주민 생산물로 표시 금지 | ART-004/008 |
| P0 | storage | CAN_DERIVE | 기존 B09/StorageBox와 Cube Chest; 잠금쇠가 잠금 기능을 오인시키지 않도록 파생 | StorageBox, WorldBuildingPlacementService; 저장은 기존 authority | ART-003/008 |
| P0 | workbench | FOUND_DIRECT | 기존 B05 + B05_Workbench_PreparationKit | Workbench/BasicWorkbench, CraftingService | ART-003/006 |
| P0 | `PA_PROP_FARM_PLOT_BORDER_01` | NEED_NEW_MODEL | 원본 팩에 현재 고정 밭의 빈/씨앗/성장/수확 가능을 구분할 낮은 경계 구조가 없음 | Farmland/CropData/기존 planting 단계 | ART-004/008 |
| P0 | Wheat / produce | FOUND_DIRECT | Nature Wheat와 기존 Prop_Wheat, FoodKit carrot, 기존 Item_Wheat/Item_Carrot | Farmland/ItemInstance/producer 실제 Wheat 수량 | ART-004/005 |
| P0 | seed sack | CAN_DERIVE | FoodKit bag에서 열린 입구·씨앗 힌트·작은 라벨 구조 추가 | Item_15_Seed; 씨앗 잔량은 Inventory | ART-004/008 |
| P0 | mine area / `PA_PROP_ORE_OUTCROP_01` | CAN_DERIVE | Nature Rock_1에 광석 덩어리·채광면·고갈 silhouette 추가 | MiningSpot의 일일 자원 상태; WorldGrid 지형 대체 금지 | ART-004/008 |
| P0 | fishing pond | FOUND_DIRECT | 기존 WorldGrid water/pond와 FishingSpot; CuteFish Dock_Long_NoRope는 가장자리 파생 후보 | 기존 walkable shore와 fishing authority; 별도 물/지형 시스템 없음 | ART-002/004 |
| P0 | Fish visual | FOUND_DIRECT | CuteFish Tuna와 Fish item, rig/6 clips 존재 | Item_Fish/FishingSpot; Koi/Tuna를 신규 어종 item으로 만들지 않음 | ART-004/005 |
| P0 | BreadLoaf | FOUND_DIRECT | FoodKit loaf | Item_BreadLoaf/Recipe_Bread와 실제 소비/판매 | ART-005 |
| P0 | `PA_FOOD_GRILLED_FISH_01` | CAN_DERIVE | FoodKit fish+plate+새 grill mark/절단·익힘 실루엣 | Item_10_GrilledFish/Recipe_GrilledFish; raw fish와 구분 | ART-005/008 |
| P0 | baked crop product | CAN_DERIVE | 기존 Item_09_BakedPotato와 Recipe_BakedPotato의 실제 Carrot 입력; 팩에 potato 직접 후보 없음 | CONTENT 캐논 errata의 가공물 명칭 정합을 따른다. 아트가 임의로 원료/상품명을 교체하지 않음 | ART-005, CONTENT 의존 |
| P0 | kitchen | FOUND_DIRECT | 기존 B06 + FoodKit pot/cutting-board | Workbench Kitchen/SpecialistNpcController, 실제 craft 상태 | ART-006 |
| P1 | forge / `PA_PROP_FORGE_TOOL_RACK_01` | CAN_DERIVE | 기존 B07 shell + Cube Pickaxe/Axe 부품 + 새 peg/rack | Workbench Forge; 효율·품질 보너스 추가 없음 | ART-006/008 |
| P1 | carpentry workspace | FOUND_DIRECT | 기존 B05 + WoodLog/Plank 소품 | Carpenter/SpecialistNpcController, CraftingService | ART-006 |
| P1 | sewing workspace | NOT_REQUIRED_FOR_DEMO | 기존 B08은 보존. 최신 캠페인은 의류 완주를 첫 달 필수에서 제외 | Tailor 관계/소비 소개 유지, Tier/원료 잠금 우회 없음 | ART-006의 선택 범위 |
| P1 | resident role props | CAN_DERIVE | rod/pickaxe/axe/pot/wood에서 한 역할의 행동을 읽을 소품 구성 | 기존 8역할 캐릭터·workSpot·schedule 유지; 뼈/모델 교체 없음 | ART-006 |
| P1 | `PA_SIGN_SHOP_01` / `PA_SIGN_WAYFINDING_01` | NEED_NEW_MODEL | 기존 P.A. 표지 표현을 잇는 공통 판·기둥·anchor 구조 | 기존 상호작용과 WorldAlpha 목표의 실제 위치; 낯선 로고/설정 추가 없음 | ART-002/003/008 |
| P1 | `PA_PROP_PROCESSING_STOCK_TRAY_01` | CAN_DERIVE | FoodKit plate/tray와 MiniMarket 프레임으로 입력·출력의 빈 받침 | CraftingService 실제 입력/출력과 Inventory; 장식 재고 강제 생성 금지 | ART-005/006/008 |
| P1 | forest / meadow dressing | FOUND_DIRECT | 기존 Nature 8종 재사용, WoodLog/trees/bush/grass/flowers | WorldGrid 자원/통행, 기존 Gatherable | ART-002 |
| P2 | category growth props | CAN_DERIVE | 기존 VillageCultureVisualController와 꽃/목재/food 부품의 상태별 작은 구성 | SalesLog→실제 pending village→다음 날 변화; 아트만 배치해 경제 성공처럼 보이게 하지 않음 | ART-007 |
| P2 | arcade / luxury expansion | NOT_REQUIRED_FOR_DEMO | MiniArcade 참고 2종; 첫 주 필수 재고/시설 아님 | 실제 Culture/Luxury 연결이 승인되고 구현된 경우만 | ART-007 |
| P3 | seasonal/decorative variety | NOT_REQUIRED_FOR_DEMO | Nature autumn/snow, 다수 FoodKit 품목 | 기본 데모 기능·캐논 우선 | ART-010 이후 선택 |

## 2. Missing asset list / 제작 배치

이번 티켓에서 파생 mesh를 생산하지 않는다. 아래 입력은 [원본 registry](EXTERNAL_ASSET_REGISTRY.md)와 [33종 manifest](selected-assets.json)의 stable ID/해시로 고정한다. 새 파일은 `Blender/Generated/<batch>/PA_DERIVED_*.blend`, FBX는 `Blender/Export/<batch>/`, 최종 게임 파일은 `Assets/Art/ProjectPA/Derived/<role>/`이다.

| Batch / priority | 만들 결과 | Blender 작업 내용 | 완성 판정 |
|---|---|---|---|
| A / P0 | EMPTY_DISPLAY, PRICE_HOLDER | 원본 display의 연결된 상품 mesh 섬을 조사해 frame만 append한다. 판·다리·빈 트레이를 PA 비율로 재구성하고 price/slot anchor 추가. 가격표 받침은 새 primitive mesh. | 기존 슬롯 빈/재고1/가득/품절 상태에서 표시 수 일치, 가시 상품이 frame에 남지 않음. 실제 ShopSlot 가격 변경 즉시 반영. |
| B / P0 | PRODUCER_CRATE, storage chest | basket/chest의 유효 부품을 append하고 손잡이·내부·덮개를 분리한다. 새 낮은 운반 상자 비율, 빈 바닥과 적재 기준점 추가. 잠금쇠 대신 PA 표식판 검토. | 생산0/일부/용량제한, 납품 전후가 실제 상태와 일치. 통로·dropOffPoint를 막지 않음. |
| C / P0 | FARM_PLOT_BORDER, seed sack | 새 낮은 4면 목재 경계, 토양은 기존 surface 재사용. bag 입구와 씨앗 표시부만 파생. Wheat는 성장 시각에 맞춰 별도 child로 둔다. | 빈/심음/성장/수확/다음 날 상태, 기존 seed consumption와 harvest 결과 보존. |
| D / P0 | ORE_OUTCROP, fishing shore trim | Rock_1 사본에 회수 가능한 광석 cluster와 넓은 작업면 생성. Dock는 실제 shore walkable cells에 맞춰 짧은 판과 기둥으로 재구성. | 채광 전후가 구분됨. collider가 물/절벽 경계를 넘지 않고 기존 자원·통행 기준 유지. |
| E / P0 | GRILLED_FISH, baked crop visual | fish+plate에 익힌 색상군·절개/구움 형상을 추가, raw fish와 silhouette 구분. baked crop은 CONTENT 결과 item/표시명 확정 후 기존 입력에 맞춰 제작. | 실제 ItemInstance visual mapping으로 Raw/Processed 구분, 원료/가공품/품질/스택/가격은 기존 데이터 사용. |
| F / P1 | FORGE_TOOL_RACK, role workspace kit, PROCESSING_STOCK_TRAY | 도구 부품+새 peg/rack/받침 구조. Kitchen/Forge/Carpentry 작업 구역에 맞는 입력·출력 anchor, 잡는 방향 규격 정의. | 기존 specialist 작업·대기·재료부족 상태가 읽히고 작업/품질 계산과 스케줄을 바꾸지 않음. |
| G / P1 | PA_SIGN_SHOP, PA_SIGN_WAYFINDING | 새 판·기둥·브래킷 mesh. 텍스트는 기존 글꼴/UI 또는 독립 표면에 연결, 공급자 logo 제거를 새 세계관 창작으로 확대하지 않음. | 1920×1080 실제 카메라에서 목적지 식별, 상호작용/HUD 가림 없음. |
| H / P2 | growth signal sets | 꽃/상자/가공품에서 기존 2~3 category signal의 작은 조합을 생성. source rename만 한 것을 파생으로 집계하지 않음. | 실제 판매 snapshot→다음 날 signal별 등장·리셋·load 재파생이 일치. |

## 3. Export / wrapper / 검증 순서

1. source 사본 SHA256 확인, collection `PA_DERIVED_*` 생성, source collection은 보존한다.
2. mesh bounds와 단위 확인, bottom center/interaction face -Z(Blender -Y) 기준으로 새 root를 정한다. 적용한 회전/축/scale을 manifest에 기록한다.
3. `PROJECT_PA_ART_STYLE_GRAMMAR.md`에 따라 PA 공유 재질을 remap하고, 실제 source의 UV/flat 면을 보존할 부분을 검사한다.
4. 한 runtime 형식 FBX로 export하고 texture/material dependency를 manifest로 고정한다. OBJ/GLB/BLEND를 Assets에 중복 복사하지 않는다.
5. Editor utility로 기존 owner의 visual child 또는 별도 PA wrapper를 만들고 collider/anchor/NavMesh 계약을 검증한다. source prefab 자체를 경제/저장 authority로 쓰지 않는다.
6. 격리 import/prefab 왕복 후 승인된 WorldSandbox/Demo surface에서 실제 기능 상태를 검증한다. 씬 생성/편집은 Editor builder와 validator를 사용한다. Golden/MainGame 덮어쓰기 없음.
7. compile→해당 validator→관련 상점/생산/저장 회귀→diff/GUID/meta/큰 파일 검사→허용된 local commit. 같은 원인 2회 실패나 새 Unity crash는 중단한다.

## 4. 후속 ART 티켓과 사용자 작업

ART-001 palette/import normalization → ART-002 world/nature → ART-003 shop → ART-004 production → ART-005 products → ART-006 residents/workspaces → ART-007 culture → ART-008 missing-model production → ART-009 integration → ART-010 consistency. 이는 목표 파일의 권장 순서이며 이번 ART-000에서 자동 활성화하지 않는다. ART-003~007의 파생 수요는 ART-008에 모으되, 각 적용 티켓은 아직 존재하지 않는 모델을 완료로 기록하지 않는다.

에셋 원본 이동이나 Unity 수동 import는 사용자에게 요구하지 않는다. 이미 자동 처리했다. 사람에게 남는 것은 후속 ART 작업의 범위 선택과 실제 게임 화면의 최종 시각 판단이다. CONTENT milestone의 별도 승인·활성 상태는 아트 입고 작업으로 덮어쓰지 않는다.
