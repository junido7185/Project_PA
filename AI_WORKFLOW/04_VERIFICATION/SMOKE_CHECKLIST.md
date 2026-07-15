# SMOKE_CHECKLIST — Project P.A. 핵심 플레이 경로

작성: 2026-07-15 (Codex)
기준 씬: `Assets/Scenes/Prototype_FirstDay.unity`
그래픽 기준: Unity 6000.3.2f1, D3D11

이 문서는 자동 검증이 증명한 상태 연결과 사람이 직접 확인해야 하는 플레이 감각을 분리한다. 체크하지 않은 사람 항목을 자동 PASS로 간주하지 않는다.

## 1. 자동 낚시→밤 판매 스모크

Editor 메뉴: `Project PA/Validation/Run Gathering + Shop Gate Validation`

배치 실행 시 `-force-d3d11`을 사용하고 `-quit`은 넣지 않는다. 검증기가 Play Mode 종료와 Editor 종료를 직접 처리한다.

- [x] Day 2 낮에 `shore-forage` 전용 `FishingSpot`과 낚싯대 프롬프트가 존재한다.
- [x] 상호작용하면 캐스팅 대기 상태에 들어가고 Fish 2개를 실제 인벤토리에 지급한다.
- [x] 같은 날 재낚시가 차단되고 다음 날 다시 활성화된다.
- [x] 합성 Fish 주입 없이 실제 어획 수량이 2→1로 줄며 `ShopSlot`에 1개 진열된다.
- [x] `ShopPriceUI`에서 Fish 기본가 18G를 확정할 수 있다.
- [x] Day 2 낮에는 손님 구매가 닫히고 밤 페이즈+간판 개점 후 열린다.
- [x] 실제 씬 NPC 이름 `Fisher_01`로 Fish 구매가 성공한다.
- [x] 잔액 500→518G, 누적매출 +18G, Raw `SalesLog`, 일일 구매 통계, MoneyHUD가 함께 갱신된다.
- [x] 당일 낚시 완료 상태가 저장 필드 write/reset/restore 뒤 복원된다.

PASS 증거: `Logs/Codex_Task041_FishingSaleRoundTrip.log` — `FishSale=18G, buyer=Fisher_01, shopGate=OK`.

## 2. Day 1 데모 회귀

Editor 메뉴: `Project PA/Validation/Run Final Demo Route Validation`

- [x] 핫바→진열→가격 확정 경로가 유지된다.
- [x] NPC 구매/피드백, 돈과 누적매출 증가가 유지된다.
- [x] Day 1 결산 버튼이 Day 2 준비 페이즈와 새 목표를 시작한다.

PASS 증거: `Logs/Codex_Task041_FinalRouteRegression.log` — `stocked=BreadLoaf, paid=30G`.

## 3. 사람 Play Mode 낚시 스모크

사전 조건: 새 게임 Day 2 낮 또는 Day 2 낮 세이브. F10 개발 표시는 OFF. 조작은 WASD, `Space`, `I`, 숫자 핫바 키를 사용한다.

- [ ] 해변의 물빛 표식·낚싯대·찌·바구니가 일반 채집 상자와 구분된다.
- [ ] 표식까지 WASD로 걸어갈 때 지형/NPC/트리거에 막히지 않는다.
- [ ] `[Space]` 후 `찌를 기다리는 중`→`찌가 흔들려요`→성공 흐름이 약 1.25초 안에 읽힌다.
- [ ] `I` 인벤토리에서 Fish 2개 증가를 확인하고, 같은 날 재상호작용 시 완료 안내가 보인다.
- [ ] 빈 판매대에서 `Space`를 눌러 Fish 1개를 진열하고 인벤토리에 Fish 1개가 남는지 확인한다.
- [ ] 같은 판매대에서 다시 `Space`를 눌러 가격 UI를 열고 18G를 확정한다.
- [ ] 밤 페이즈에 영업 간판을 열기 전에는 손님이 구매하지 않고, 연 뒤에는 손님이 접근·평가한다.
- [ ] 구매가 성립하면 Fish가 판매대에서 사라지고 돈 HUD가 18G 증가하며 구매 말풍선/피드백이 보인다.
- [ ] 다음 날 아침 해변 낚시터가 다시 이용 가능하다.

NPC 구매는 확률 판단이므로 첫 손님이 거절하면 가격을 바꾸지 말고 다음 손님까지 관찰한다. 자동 검증의 직접 거래 PASS는 실제 NPC 이동·평가의 체감 확인을 대체하지 않는다.

## 4. 완료 판정과 실패 처리

- 자동 스모크 2종은 위 로그의 최종 `finished successfully`까지 있어야 PASS다.
- 사람 스모크는 체크된 항목만 확인한 것으로 기록한다.
- 같은 원인으로 검증이 2회 실패하면 세 번째 실행 없이 `AI_WORKFLOW/05_LOGS/BUG_LOG.md`에 기록하고 중단한다.
- D3D12 자동 실행, 메인 씬 저장/덮어쓰기, 사용자 세이브 변조는 금지한다.
