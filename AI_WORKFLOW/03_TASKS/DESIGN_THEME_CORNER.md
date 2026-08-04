# 상품 진열 테마 코너 설계

작성일: 2026-07-17  
대상 작업: Task 024  
판정: **설계 완료 / 기능 구현·전용 검증 PASS / 전체 회귀·최종 시각 검토 미완**

## 1. 목적

해질녘에 플레이어가 같은 분류의 상품을 가까이 모아 진열하면, 그 배치를 하나의 읽기 쉬운 상품 코너로 인식한다. 코너는 새 재고나 별도 판매대를 만들지 않고 기존 `ShopSlot`의 위치와 `Item.category`에서 파생한다.

이 기능의 역할은 다음 한 문장으로 고정한다.

> 플레이어가 오늘 밀고 싶은 상품 분류를 공간으로 표현하고, 그 상품의 실제 밤 판매가 기존 마을 방향 신호로 이어짐을 준비 단계에서 이해하게 한다.

## 2. 용어 충돌 방지

현재 프로젝트에는 이미 상점 전체의 벽·가구·조명 색을 바꾸는 **상점 인테리어 테마**가 있다.

| 개념 | 현재 소유자 | 저장 | 의미 |
|---|---|---|---|
| 상점 인테리어 테마 | `ShopCustomizationController` | v10 `shop.theme/fixed.shop.theme` | `default`, `processed.warm`처럼 상점 전체 외관을 바꾸는 진행 보상 |
| 상품 진열 코너 | 이 문서의 대상 | 저장하지 않고 슬롯에서 파생 | 같은 카테고리 상품을 인접한 실제 진열대에 묶은 운영 배치 |

후속 코드와 UI에서는 두 개념을 모두 `theme`으로 부르지 않는다.

- 플레이어 표시: `원재료 코너`, `가공품 코너`, `실용품 코너`, `고급품 코너`
- 권장 코드 용어: `MerchandisingCorner` 또는 `DisplayCorner`
- 금지 코드 용어: `ShopThemeCorner`, `ThemeRecord`, `shop.theme.*`

상품 코너가 인테리어 테마를 자동 변경하거나 `processed.warm`을 해금하지 않는다. 기존 인테리어 테마의 진행·저장 의미를 보존한다.

## 3. 현재 구현 감사

### 3.1 재사용할 권위 있는 데이터

| 영역 | 현재 근거 | 코너에서의 사용 |
|---|---|---|
| 상품 상태 | `ShopSlot.currentItem`, `IsEmpty`, `EffectiveDisplayPrice` | 실제로 진열된 상품만 판정한다. 별도 코너 재고를 만들지 않는다. |
| 상품 분류 | `Item.category` | 같은 카테고리 여부의 유일한 원본이다. 이름·가격·색상으로 추측하지 않는다. |
| 판매 가능성 | `ShopSlot.CanStock` | `Tool` 또는 `toolType != None`인 품목은 정상 진열 경로에서 이미 차단된다. |
| 배치 위치 | `ShopCustomizationController`의 private `PlacementRuntime`과 `GridService` zone | 월드 거리나 씬 이름이 아니라 `shop.interior` 셀과 안정적인 placement ID를 읽는다. |
| 진열대 크기 | 현재 `shop.shelf` 정의의 `1×1` footprint | 현재는 한 진열대가 한 셀이지만, 읽기 API는 footprint 셀 목록을 반환해 미래 다중 셀 진열대를 막지 않는다. |
| 이동·회수 | 기존 배치 장부 | 이동, 회전, 회수 뒤 같은 코너 판정을 다시 계산한다. |
| 저장 | v10 가구 placement + `ShopSlotSaveData` | 코너 자체를 저장하지 않고 로드된 배치와 상품에서 재구성한다. |
| 마을 방향 | `ShopSlot.TryPurchaseByNpc` → `SalesLogManager.RecordSale` → `VillageChangeSignalController` | 코너 안 상품도 기존 성공 판매와 같은 카테고리 신호를 만든다. 별도 매출을 추가하지 않는다. |

### 3.2 현재 실제 상품 범위

현재 플레이 경로의 `Assets/Resources/Items`에는 Raw 5종, Processed 5종, Utility 2종, Luxury 2종, Tool 1종이 있다. 이 중 Utility의 `씨앗`은 `toolType=Seed`, `호미`는 `Tool/Hoe`라 정상 판매대 진열 대상이 아니다.

첫 구현의 실제 코너 후보는 다음과 같다.

- Raw: Wood, Ore, Wheat, Carrot, Fish
- Processed: BreadLoaf, IronBar, Plank, 구운 감자, 생선구이
- Utility: 철제 도구. 같은 상품을 여러 실제 슬롯에 진열하는 경우에도 코너가 될 수 있다.
- Luxury: 목제 가구, 의류
- Tool 및 `toolType != None`: 코너에서 제외

같은 상품을 두 슬롯에 진열해도 같은 분류를 강조하는 실제 배치이므로 유효하다. 서로 다른 상품 종류 수를 새 조건으로 만들지 않는다.

### 3.3 Task 086 구현 상태

- `MerchandisingCornerController`가 실제 `ShopSlot`과 placement footprint를 읽어 카테고리별 4방향 연결 요소를 파생한다.
- 배치 장부 요약과 연결 요소당 `PrototypeWorldLabel` 하나를 갱신한다.
- `ShopCustomizationController`는 private placement를 유지한 채 진열대 전용 읽기 projection만 제공한다.
- `PA_ThemeCornerValidator`가 음성/양성 인접, 품절·보충, 회수·이동, 판매·마을 신호, v10 로드 재파생을 D3D11 실제 GameView에서 PASS했다.

Task 086은 기능 계약 자체는 동작하지만 기존 `PA_ShopCustomizationValidator`의 직접
`Camera.Render()`가 Unity 네이티브 충돌을 두 번째로 재현했다. 전체 회귀와 플레이어 기본 화면의
최종 라벨 가독성을 완료하지 못했으므로 프로젝트 판정은 PARTIAL을 유지한다.

## 4. 판정 계약

### 4.1 판정 단위

한 코너 후보는 다음 조건을 모두 만족하는 **서로 다른 진열대 placement**다.

1. `ShopCustomizationController`에 `shop.shelf`로 등록되어 있다.
2. 회수 상태가 아니며 GameObject가 hierarchy에서 활성이다.
3. 연결된 `ShopSlot`이 비어 있지 않다.
4. `currentItem.data`가 있고 `category != Tool`, `toolType == None`이다.
5. 같은 placement ID를 두 번 세지 않는다.

현재는 placement 하나에 `ShopSlot` 하나다. 미래에 한 가구에 여러 상품 슬롯이 생기면, 별도 sub-slot footprint가 정의되기 전까지 그 가구 전체를 하나의 판정 단위로 센다. 한 가구 내부의 슬롯 두 개만으로 가짜 코너를 만들지 않는다.

### 4.2 인접 규칙

두 진열대는 각 footprint의 셀 중 하나라도 상하좌우 변을 공유할 때 인접한다.

```text
인접: |ax - bx| + |ay - by| == 1
```

- 대각선만 닿는 진열대는 인접하지 않는다.
- 빈 셀 하나를 사이에 둔 진열대는 인접하지 않는다.
- 회전은 footprint 셀과 함께 계산하되, 현재 1×1 선반에서는 결과가 같다.
- 월드 좌표 거리, Collider bounds, GameObject 이름으로 인접성을 추측하지 않는다.

### 4.3 코너 성립

같은 `ItemCategory`의 인접 진열대를 4방향 flood fill로 묶는다. 연결 요소에 서로 다른 placement ID가 **2개 이상**이면 코너 하나가 성립한다.

예:

```text
R R .     R P     R .     R R R
. R .     . .     . R     . . .
```

- 첫 번째: Raw 3칸 코너 1개
- 두 번째: Raw/Processed가 달라 코너 없음
- 세 번째: 대각선이라 코너 없음
- 네 번째: Raw 3칸 코너 1개

같은 카테고리라도 떨어진 연결 요소는 별도 코너다. 한 진열대는 상품 카테고리 하나만 가지므로 동시에 두 코너에 속하지 않는다.

### 4.4 실시간 갱신

첫 구현은 코너를 저장하거나 영업 시작 시 고정하지 않고 **현재 진열 상태에서 파생**한다.

- 진열, 회수, 품절, 보충, 가구 이동·회전·회수·복원 뒤 갱신한다.
- NPC가 한 슬롯을 구매해 연결 요소가 1칸만 남으면 코너 표시가 사라진다.
- 밤에 보충하면 다시 성립할 수 있다.
- 판매 이력은 코너가 사라져도 기존 `SalesLogManager`에 그대로 남는다.

이 규칙은 코너를 일회성 버프가 아니라 실제 매대 상태로 유지하며, Day 1의 항상 열린 튜토리얼 예외를 위한 별도 스냅샷 상태도 만들지 않는다.

`ShopSlot`에 변경 이벤트를 급히 추가하지 않는다. 최대 20개 진열대를 0.25초 간격으로 읽고, placement ID·셀·카테고리·점유 여부의 signature가 바뀔 때만 연결 요소와 표시를 다시 만드는 방식이면 충분하다. 이후 프로파일링에서 필요할 때만 좁은 변경 알림으로 교체한다.

## 5. 최소 런타임 구조

### 5.1 배치 읽기 API

`ShopCustomizationController`의 `PlacementRuntime`과 `PlaceableDefinition`은 계속 private로 둔다. 후속 구현은 변경 권한이 없는 스냅샷만 노출한다.

권장 형태:

```text
TryGetShopSlotPlacementSnapshot(
    ShopSlot slot,
    out placementId,
    out footprintCells,
    out recovered)
```

요구사항:

- `slot`이 placement의 루트이거나 자식인 기존 검색 규칙을 재사용한다.
- footprint는 기존 `GridService.GetZoneFootprintCells(anchor, definition.footprint, rotation)` 결과다.
- 회수/비활성 placement는 호출자가 명확히 제외할 수 있다.
- placement 객체, definition 참조, 점유 변경 API를 외부에 노출하지 않는다.
- `BuildStablePath`나 씬 hierarchy 이름을 새 컨트롤러가 다시 구현하지 않는다.

### 5.2 코너 소유자

신규 `MerchandisingCornerController` 하나가 읽기·그룹·표현만 담당한다.

- `PA_RuntimeSceneBinder`가 기존 services root에 한 번만 보장한다.
- 활성 `ShopSlot`을 찾고 위 스냅샷 API로 실제 placement만 선별한다.
- 카테고리별 연결 요소와 centroid를 계산한다.
- 다른 시스템에는 읽기 전용 `CurrentCorners`와 짧은 `BuildCornerSummary()`만 제공한다.
- 재고 이동, 가격, 구매, 매출, 트렌드 점수, 저장을 소유하지 않는다.

권장 런타임 스냅샷 값:

- category
- 정렬된 placement ID 목록
- 진열대 수
- footprint 셀 집합
- 월드 표시 centroid

코너 ID가 필요하면 `corner:{category}:{sortedPlacementIds}`처럼 런타임에서 결정적으로 만들되 저장 키로 사용하지 않는다.

## 6. 플레이어 피드백

### 6.1 배치 장부 요약

기존 `ShopCustomizationController` 진행 문구 아래에 한 줄만 추가한다.

- 코너 없음: `진열 코너: 같은 분류 상품을 나란히 2칸 이상 진열하세요.`
- 한 개: `진열 코너: 가공품 3칸 활성`
- 여러 개: `진열 코너: 원재료 2칸 · 고급품 2칸`

분류 표시명은 Raw=`원재료`, Processed=`가공품`, Utility=`실용품`, Luxury=`고급품`으로 고정한다. 내부 enum 문자열을 플레이어에게 그대로 노출하지 않는다.

### 6.2 월드 표시

각 성립 코너의 진열대 centroid 위에 작은 `PrototypeWorldLabel` 하나를 둔다.

- 문구: `가공품 코너 · 3칸`
- 기존 `ShopSlot` 카테고리 색상 계열을 재사용하되 본문 가독성을 우선한다.
- Collider, 상호작용, 별도 Canvas 패널을 만들지 않는다.
- 카메라를 가리거나 상품 가격 라벨과 겹치면 높이·크기를 조정하고, 코너마다 장식 프리미티브를 추가하지 않는다.
- F10 개발 UI가 아니라 기본 플레이 화면에서 읽히되, 1920×1080 기준으로 한 코너당 한 라벨만 보인다.

UI 문구는 `판매 보너스`를 약속하지 않는다. 필요하면 배치 장부에 `이 분류 상품의 실제 판매가 마을 방향에 반영됩니다.`라는 설명 한 줄만 제공한다.

## 7. 상점 운영과 마을 변화 연결

첫 구현은 구매 확률·가격·매출·트렌드 점수에 배수를 주지 않는다.

```text
같은 카테고리 ShopSlot 2개 이상 인접
→ 상품 코너로 시각 인식
→ 기존 NPC가 기존 PurchaseEvaluator로 각 슬롯 평가
→ 실제 구매 성공
→ 기존 ShopSlot.TryPurchaseByNpc
→ 기존 SalesLogManager.RecordSale(category)
→ 기존 VillageChangeSignalController/정산의 마을 방향
```

이 연결의 핵심은 코너가 새 점수를 만드는 것이 아니라, 플레이어가 어떤 카테고리 판매를 의도하고 있는지 공간과 문구로 이해하게 하는 데 있다.

- 진열만 하고 팔리지 않은 상품은 마을 변화 점수를 만들지 않는다.
- 코너 상품 판매를 `RecordSale`에 두 번 기록하지 않는다.
- 거절은 기존 일일 구매 판단 통계에만 들어간다.
- Processed 코너 자체가 다음 날 문화를 켜지 않으며, 실제 Processed 판매가 있어야 기존 변화가 예약된다.
- 기존 `count × 1000 + revenue` 카테고리 집계를 바꾸지 않는다.

향후 코너 전용 구매 보정이나 트렌드 가중치를 도입하려면 `SaleRecord`에 판매 출처를 정확히 남기고 밸런스 승인을 받는 별도 작업으로 연다. 첫 구현에서 보이지 않는 확률 보너스를 하드코딩하지 않는다.

## 8. 저장과 복원

코너는 다음 두 기존 상태의 파생값이다.

1. v10 `placeables`: zone, placement ID, cell, rotation, recovered
2. `shopSlots`: 상품, 수량, 품질, 가격

로드 순서는 이미 가구 복원 후 ShopSlot 상품 복원이다. 코너 컨트롤러는 둘이 준비된 다음 signature를 다시 읽으면 된다.

- `SaveData`, `SaveManager`, `CurrentSaveVersion` 변경 없음
- `PlaceableSaveData.functionalState`에 코너를 넣지 않음
- `shop.theme/fixed.shop.theme` 레코드에 코너를 섞지 않음
- 판매로 사라진 현재 코너를 별도 영구 상태로 복구하지 않음

## 9. Task 086 구현 파일과 보존 경계

### 9.1 예상 수정 파일

- 신규 `Assets/Scripts/MerchandisingCornerController.cs`
- `Assets/Scripts/ShopCustomizationController.cs`: 진열대 placement 읽기 API와 장부 요약 한 줄
- `Assets/Scripts/PA_RuntimeSceneBinder.cs`: 컨트롤러 1개 보장
- 신규 `Assets/Editor/PA_ThemeCornerValidator.cs`
- `Automation/LoopEngineering/validator-registry.json`: 전용 D3D11 검증 등록

정확한 파일 목록은 구현 작업 시작 전 다시 보고한다. `ShopSlot.cs`, `PurchaseEvaluator.cs`, `SalesLogManager.cs`, `VillageChangeSignalController.cs`, `SaveData.cs`, `SaveManager.cs`, 메인 씬은 첫 구현에서 수정하지 않는다.

### 9.2 구현하지 않을 것

- 새 인벤토리, 새 상품 슬롯, 코너 전용 판매 기록
- 수동 코너 이름 입력, 코너 프리셋, 무작위 장식 생성
- 가격/구매확률/매출/트렌드 배수
- 인테리어 테마 자동 변경
- 코너 저장 스키마
- GameObject 이름이나 월드 거리 기반 그룹
- Tool 판매 허용

## 10. 검증 계약

전용 D3D11 검증기는 실제 2m `shop.interior`와 실제 `ShopSlot`을 사용해 다음을 확인한다.

1. 인접한 두 슬롯에 같은 Raw 상품을 진열하면 Raw 2칸 코너 하나가 생긴다.
2. 같은 상품 두 개도 서로 다른 placement면 유효하다.
3. Raw와 Processed가 인접해도 코너가 생기지 않는다.
4. 같은 Raw 두 슬롯이 대각선이거나 빈 셀을 사이에 두면 코너가 생기지 않는다.
5. ㄱ자 Raw 세 슬롯은 코너 하나, 3칸으로 집계된다.
6. 한 슬롯이 품절되면 다음 갱신에서 1칸만 남은 코너가 사라지고, 보충하면 다시 생긴다.
7. 진열대를 이동·회전·회수하면 새 footprint 기준으로 재판정한다.
8. recovered/비활성/template/Tool 대상은 집계하지 않는다.
9. Processed 코너 상품의 실제 NPC 구매가 기존 단일 `SaleRecord`와 마을 카테고리 신호를 만들며 추가 매출이나 중복 기록이 없다.
10. v10 저장→불러오기 후 같은 가구 셀·상품에서 코너가 다시 파생된다.
11. 기존 ShopCustomization, SaveRoundTrip, FinalDemoRoute 30G 회귀가 통과한다.

시각 체크포인트는 게임 카메라 1920×1080에서 코너 없음/Raw 2칸/Processed 3칸의 같은 구도를 캡처한다. 상품명·가격·코너 라벨, 플레이어 동선, 기존 HUD와 겹침을 직접 확인한다. 사람 확인은 최종 글자 크기와 색 구분 체감만 남긴다.

## 11. 자기검토 결과

- 기존 `ShopSlot` 재사용: **예**. 상품·수량·가격·품절·구매 의미를 복제하지 않는다.
- 기존 격자 재사용: **예**. `shop.interior`의 placement ID와 footprint 셀을 읽는다.
- 1×1 전용 폐쇄 설계: **아니오**. 현재 선반은 1×1이지만 footprint 셀 집합의 변 인접으로 정의한다.
- 상점 전체 테마와 충돌: **아니오**. 명칭·소유자·저장 키·해금 의미를 분리한다.
- 마을 변화 연결: **예**. 코너 상품의 실제 성공 판매만 기존 카테고리 신호로 이어진다.
- 새 대형 시스템: **아니오**. 읽기 전용 컨트롤러 하나와 좁은 placement 스냅샷 API만 제안한다.
- 저장 스키마 변경: **없음**.
- 코드 변경: **있음**. Task 086의 컨트롤러, 좁은 placement 읽기 API, 런타임 바인딩, 전용 validator를 구현했다.
- 씬·프리팹·패키지·저장 스키마 변경: **없음**.
- 플레이 가능 기능 여부: **전용 기능 검증 PASS / 프로젝트 완료 판정 PARTIAL**. 기존 직접 `Camera.Render()` 회귀 경로를 안전한 공용 캡처로 교체한 뒤 ShopCustomization·SaveRoundTrip·FinalDemoRoute와 기본 플레이 라벨 가독성을 다시 확인해야 한다.
