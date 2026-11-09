# 의자 색 변형 (2026-11-09, 너굴 리스트 연구 H4)

- 무엇: 나무 의자(소유 모델 `PA_Decor_CozyChair`)의 쿠션 색 변형 둘 — **산호빛 의자**(2058, 키 `furniture-chair-coral`)·**바다빛 의자**(2059, 키 `furniture-chair-sea`).
- 만들기(현장 작업대 "칠하기"): 나무 의자 1 + 산호 조각 1 → 산호빛 의자, 나무 의자 1 + 해변 유리 조각 1 → 바다빛 의자. 섬에서 모은 발견물이 가게 꾸밈 색이 된다(수집 → 꾸밈 연결).
- 저장: 색마다 다른 아이템 id·배치 키라 **저장 형식은 바꾸지 않는다**(연구가 걱정한 아이템 인스턴스 메타 없음). 기존 저장은 그대로, 새 것은 추가만.
- 쓰임: 기존 의자와 똑같이 손님 쉼터 의자(`DemoSettlementController.ChairKeys` — 앉기 `ShopChairs`·꾸밈 수 `CountShopDecor` 모두 포함). 모양·충돌체·앉는 점(`Sitpoint`)은 기존 의자 배치물을 복사해 그대로.
- 툴팁 한 줄(나무·산호빛·바다빛 공통): "색 변형 · 나무·산호빛·바다빛 — 현장 작업대에서 산호 조각·해변 유리로 칠해요".
- 연구 H4의 "도감 보유/전체"는 이번에 넣지 않았다(도감은 생물·발견물 전용 — 꾸밈 수집 칸은 별도 결정).
- 코드: `Editor/PA_ChairVariantsSetup.cs`(모델·배치물·아이템·레시피·카탈로그, `ExistingEntries()`로 보존), `Editor/PA_OwnedChairSetup.cs`(모델 공용), `World/DemoSettlementController.ChairKeys`, `World/DecorThemeSets.TooltipLine`, 캡처 `-pa-capture-plan chairpaint`(칠하기 2번 → 툴팁 → 가게에 두 의자 → 영업 → 손님 앉음).
- 보유/전체(연구 H4, 2026-11-09): 도감은 생물·발견물 전용이라 의자 툴팁에 '색 변형 · 나무·산호빛·바다빛 (보유 n/3)'(가방 + 가게 안, 저장 추가 없음). r258 chairpaint '보유 2/3'.
