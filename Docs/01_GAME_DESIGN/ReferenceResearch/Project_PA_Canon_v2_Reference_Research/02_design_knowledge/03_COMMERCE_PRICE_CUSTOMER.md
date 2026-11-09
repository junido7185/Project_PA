# COM-01: 진열·가격·고객·판매를 하나의 읽을 수 있는 사건으로

Canon §11·§16–18·§20·§23. 역분석 [Moonlighter](../01_reverse_analysis/03_MOONLIGHTER_COMMERCE.md). 직접 읽은 `ShopSlot`, `ShopPriceUI`, `PurchaseEvaluator`, `NpcController`를 기준으로 한다(R). 아래 개선/검수 계약은 H다.

## 현재 책임과 실제 순서 R

`ShopSlot`은 선택 판매품의 진열/추가/회수·claim·실제 판매를 담당한다. FirstDayControls에서는 선택 hotbar의 들고 있는 판매품만 후보가 되며, 일반 legacy 경로의 첫 판매품 fallback과 구별된다. 진열은 수량을 가판대 용량까지 이전하고 품질/currentPrice를 새 ItemInstance에 복사한다.

`ShopPriceUI`의 `_pendingPrice`는 편집 값이고 `OnConfirm`이 `displayPrice`를 갱신하고 표시를 refresh한 뒤 `OnPriceConfirmed`를 보낸다. ±1/±10과 drag 경로가 있다. 표시의 ‘현재 가격’은 확인 가능한 운영 값이며 추천 정답가를 뜻하지 않는다.

`NpcController`의 실제 enum은 `Idle / MovingToShop / BrowsingShop`이다. browse 접근이 끝난 뒤 slot claim → Evaluate → read-only 통찰/표현 훅 → 같은 메서드에서 `TryPurchaseByNpc`를 호출한다. **현재 읽은 경로에서 평가와 구매 사이에 await/다음 프레임 이동 단계는 없다.** 원작의 물건을 든 고객→계산대 이동을 그대로 추가하려면 이 경계가 바뀐다는 점을 고려해야 한다.

`TryPurchaseByNpc`는 개점 gate·claim·재고·경제/로그 권위를 검사하고 live EffectiveDisplayPrice로 입금·판매 기록·수량 감소를 실행하며 finally에서 claim을 해제한다. 판매 성공의 확정 출처는 이 경로다. 구매 의향만 기록한 이벤트는 실제 판매 기록이 아니다.

## 운영 상태와 상품 상태를 분리한다

| 상태 축 | 의미 H/C | 금지해야 하는 행동 |
|---|---|---|
| CLOSED | 배치·꾸미기·제작·진열·가격 준비 | 고객의 새 판매 commit |
| OPEN | 고객 평가/판매, 가격 수정 가능 | 가판대/가구 이동, 공간 계약을 깨는 변경 |
| CLOSE 처리 | 더 이상 새 평가/판매를 받지 않고 기존 루프가 정리 | 보고서 후 들어오는 지연 판매, 중복 마감 |
| 상품 Empty/Stocked | 물건·품질·수량의 상태 | 사라진/바뀐 상품의 가격창 확정 |
| 상품 Unclaimed/Claimed | 고객의 평가/거래 임시 점유 | 두 고객의 동일 재고 중복 판매 |
| 가격 Editing/Confirmed | pending과 운영 값 | 확인 전 운영 값 변경, 취소 후 잔류 pending |

‘CLOSE 처리’는 제안하는 논리 경계이며 새 저장 enum/새 controller를 요구하지 않는다. 기존 DayNightShopLoopController 종료 경로 위에서 idempotence와 입력 안내를 정리한다.

## 한 상품이 가판대에 올라가는 순간

H: 시작 상태는 selected instance, held 여부, 판매 가능 여부, 가판대의 item/category/capacity다. E는 빈칸 진열·동일 상품 추가·가격 열기 중 하나를 명확히 수행한다. 끝 상태에서 이전된 실제 수량과 품질을 유지하고 sourceSlot/held model/display가 같은 사실을 보여준다.

중요한 실패는 ‘입력이 실패했다’보다 **다른 상품이 올라갔다**, **메타데이터가 바뀌었다**, **가격이 이전 상품의 것인데 새 상품의 값처럼 보인다**이다. current 코드에는 기존 positive displayPrice를 유지하는 경로가 있으므로 품절 후 다른 물건/품질을 진열했을 때의 가격 정책을 검수한다(R/H). 이것을 곧바로 버그로 단정하지 않는다. 유지가 의도라면 새 상품과 유지 가격을 명확히 읽게 해야 한다.

## 고객 반응과 가격 학습

현재 `PurchaseEvaluator`는 quality를 반영한 idealPrice, 가격 비율, category·성향·민감도·RNG를 사용한다(R). `willBuy`는 가격만의 진위 판정이 아니다. 낮은 가격도 경우에 따라 pass하고 비싼 물건도 특정 고객이 살 수 있다.

H: 표시용 ‘가격에 대한 인식’과 ‘최종 구매/거절’을 구분한다. 정확한 probability/idealPrice/추천가를 UI에 노출하지 않는 Canon을 지킨다. ‘오늘은 다른 걸 볼게요’는 거절 결과에 맞지만 ‘너무 비싸서 안 샀다’는 가격 원인이 확인되는 표현 정책을 전제로 한다. 학습 자료가 많아져도 내부 정답을 알려주는 상점으로 바꾸지 않는다.

정적 코드에서 확인한 표현 위험 R/H:

- `BuildPurchaseFeedback`는 display/basePrice 비율을 사용하고, Evaluate는 quality 보정 idealPrice를 쓴다. 고품질 상품에서 두 해석 기준이 달라질 수 있다. 실제 오해가 생기는지 같은 상품의 품질별로 검수한다.
- reject 분기는 `DialogueTopic.ShopTooExpensive`로 요청한다. 실제 DialogueData의 문구는 이번 연구에서 읽지 않았다. topic이 모든 거절을 과가격 사유로 말하게 하는지 확인한다.
- 수익·판매 전에 호출되는 read-only presentation/demand 훅은 결정 기록이다. 결과적으로 purchase가 실패하면 이를 ‘판매 완료’로 표시해서는 안 된다.

가격 학습의 품질은 손님 한 명을 강제로 구매시켜 보장하지 않는다. 같은 조건에서 반복 관찰해도 가격·성향의 효과가 완전히 운으로 보이는지, 첫 밤에서 수정할 정보가 충분한지 확인한다. RNG 회귀는 고정 seed로 재현할 수 있지만 플레이 품질은 다양한 정상 seed/성향과 자연 입력에서 별도로 평가한다.

## 평가와 결제의 일관성

현재 동기 경로에 다음 프레임 가격 drift를 실행 버그로 주장할 근거는 없다. 그럼에도 반응 연출 때문에 평가→구매를 분리하거나 훅이 stock/price를 변경하는 재진입을 만들면 quote 일관성이 새 위험이 된다(H).

선택 가능한 최소 정책 H:

1. **동기 판매 유지:** 평가·구매를 기존 경로에서 끝내고, 결과 표현만 그 사실을 재생한다. 표현을 위해 실제 재고를 다시 이동/지급하지 않는다.
2. **평가 동안 해당 slot 잠금:** Canon이 허용하는 작은 잠금으로 price/stock mutation을 막고 기존 완료/취소에서 풀어준다.
3. **지연 판매가 꼭 필요한 경우:** slot/item/quality/price version을 평가 사실과 연결하고 commit에서 변화를 확인한다. 변화했으면 stale 결과를 버리고 새 평가 또는 취소 정책으로 처리한다. 기존 Economy/SalesLog/ShopSlot가 유일한 실제 거래 경로다.

현재 구현 목적에는 1 또는 작은 2의 검수가 먼저다. 3은 고객의 실제 물건 운반/계산대 거래를 새로 승인·설계할 때 필요한 계약이며 데모 의무가 아니다.

## 돈·재고·로그·표현의 한 사건

H: 판매 성공 하나는 실제 재고 1 감소, 결제액만큼 Money 증가, 기존 Sale record 1개, first-sale/report 관찰 1회와 같은 사실을 나타내야 한다. 실패는 판매 연출을 내지 않고 claim을 정리한다. 현재 메서드의 입금→로그→수량 순서에서 listener 예외/서비스 부재 등이 최종 사실을 불일치시키는지는 별도 검수 대상이다. 새 병렬 경제 ledger를 만들어 덮어쓰지 않는다.

CLOSE와 판매가 만날 때 기존 gate의 승인 순서를 기준으로 ‘마감 전 완료한 판매는 포함, 마감 뒤 신규 판매는 없음’을 일관되게 적용한다(H). 보고서는 완료된 SalesLog에서 계산하며 Browse decision count를 매출로 변환하지 않는다. 단순 sold-out 라벨은 오늘 품절이라는 정보이며 가방/재고 없음과 구별한다.

## 가장 중요한 검수

| ID | 실제 조건/입력 | 품질 판정 |
|---|---|---|
| Q-C01 | X 상태, 비판매 도구 선택, 서로 다른 판매품 선택 후 빈 stand E | held 판매품만 이전, 다른 슬롯 fallback 없음, 실제 수량/품질 보존 |
| Q-C02 | Drag·±1·±10·취소·확정, 낮/밤/다른 해상도 | pending/confirmed가 읽힘, 표시 값과 운영 값 일치, 추천가/확률 없음 |
| Q-C03 | 고객 접근/평가 경계에서 가격창 조작 | 반응의 상품·가격과 실제 결제가 일치, 잠금 있으면 짧고 해당 slot만 |
| Q-C04 | 품질이 다른 같은 상품, 같은 가격의 여러 고객 | 거절을 단일 가격 정답처럼 학습시키지 않음, debug와 표현의 기준 일관 |
| Q-C05 | 두 고객·마지막 1개·반복 입력·기존 listener 실패 모사 | 1개의 판매만, Money/stock/log/report 일치, claim 회복 |
| Q-C06 | 1개 진열만 하고 OPEN, 나머지 empty | 개점 가능(C), 고객이 해당 가판대 접근·반응·판매 |
| Q-C07 | 판매 진행 중 CLOSE/자동 마감/중복 CLOSE | 마감 1회, 정산 후 신규 매출 0, 이동/UI 복귀 |
| Q-C08 | 품절 후 새 상품/품질 진열·회수 가방 부족 | 가격 정책 명료, 회수 실패 보존, 모델이 최종 stock와 일치 |

고객 수·반응 노출시간·가격 구간은 원작 값으로 정하지 않는다. 현재 설정은 보존하고 위 불일치를 해결한 뒤 P.A. 목표에 맞춰 튜닝한다.
