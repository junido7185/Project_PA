# 통합 책임 맵과 좁은 개발 작업 단위

기준 HEAD `b121140ae6f5ca0f92078db5c2f75512dc8fc95a`. 정적 연구이며 코드 수정·Unity 실행·commit/push를 수행하지 않았다. 다음 개발자는 실제 작업 시작 때 HEAD와 최신 CURRENT_STATE를 다시 읽고 기준 변경을 기록한다. Canon을 연구 결론으로 수정하지 않는다.

## 직접 읽은 코드와 사용할 경계

| 파일 R | 확인한 역할/경로 | 연구 적용 H |
|---|---|---|
| [PlayerInteraction](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Assets/Scripts/PlayerInteraction.cs) | modal gate, placement 우선 처리, 대상·held·pendingHold, tool 실패 경로 | 후보/프롬프트 일치, modal 종료 입력 누출, tap/hold 취소 |
| [EquipmentSystem](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Assets/Scripts/EquipmentSystem.cs) | IsHolstered·HeldItem·held model, TryBeginToolUse/FailedUse; 실제 reward는 대상 책임 | X/선택 분리, 도구 instance 안정성, 손잡이/회복 표현 |
| [CameraController](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Assets/Scripts/CameraController.cs) | 기존 follow 안 opening/build framing, 충돌 보정 | 대상·지면·ghost 가독성 튜닝 |
| [ShopSlot](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Assets/Scripts/ShopSlot.cs) | selected stock·topup·retrieve·claim·TryPurchaseByNpc·가격/품절 표현 | 수량/품질 보존·중복 판매·실패 회복 |
| [ShopPriceUI](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Assets/Scripts/UI/ShopPriceUI.cs) | pending price·drag/±·확정·회수·OnPriceConfirmed | pending/confirmed 명확성, 대상 무효화·OPEN 정책 |
| [PurchaseEvaluator](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Assets/Scripts/PurchaseEvaluator.cs) | profile/slot/RNG 순수 Evaluate, quality/가격/선호/성향 | 기존 수학 보존, debug와 사용자 정보 분리 |
| [NpcController](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Assets/Scripts/NpcController.cs) | 접근/browse→claim→평가/표현→동기 purchase, 취소/종료 claim 해제 | 고객 반응 의미, 품질 기준 차이, 기존 FSM 위에서 연출 |
| [FishingSpot](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Assets/Scripts/FishingSpot.cs) | direct cast/minigame/landed/claim, dry landing, 현재 선택 도구 차감 | 최종 획득·가방 부족·도구 변경·내구도 경계 |
| [WorldBuildingPlacementService](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Assets/Scripts/World/WorldBuildingPlacementService.cs) | Evaluate·preview/commit·reserved move/복원, 영역/입구/critical route | ghost 약속·취소 false 처리·OPEN 경합 |
| InteriorCustomerController | legacy PA_StoreInterior 방문 sidecar, 기존 FSM hook 사용 | 범위 구분만 참고. 이를 Demo256 현재 고객 경로라고 단정하지 않음 |

Inventory·EconomyService·SalesLog·DayNightShopLoop·DemoSettlement·Producer/Worksite 등은 최신 registry/loop map에서 책임을 확인했다. 전체 구현을 직접 읽거나 실행한 것처럼 표현하지 않는다. 변경 시 해당 실제 호출 경로를 추가로 읽는다.

## 재사용 원칙

새 Inventory, 별도 판매 ledger, 새 price evaluator, 새 NPC AI, 새 world placement/save authority를 만들지 않는다. 표현 child는 기존 진열/경제 데이터를 읽기만 한다. 광물·물고기·가구마다 다른 ‘연구용 상품’ 데이터를 발명하지 않는다. 기존 ItemRegistry/ItemInstance·selected metadata를 유지한다. 새 패키지 도입을 원작 인터뷰로 정당화하지 않는다.

낡은 CODE_REUSE_MAP의 Smartphone item·Tent3·Management Hub 목표는 Canon v2와 충돌한다. 그 파일은 기존 코드 위치를 찾는 지도이며 현재 데모 범위를 정의하지 않는다. 기능 PASS 기록은 검사한 날짜/경로/fixture 범위로 제한한다.

## 실행 가능한 작업 단위 H

| ID·우선순위 | 구체적 문제·작업 범위 | 완료 기준 | 주요 문서·검수 |
|---|---|---|---|
| W-01 P0 | 직접 행동의 읽기: current held/선택·실패·접촉·획득 표현을 실제 E 순서에 맞춤. 알려진 grip/foot 결함 우선 | 정상/허공/오도구/가방 부족이 구별되고 비용·reward 일치. 기본/근접 촬영 | ACT-01, Q-A01–04, Q-V01 |
| W-02 P0 | 고객 피드백: quality 기준 차이와 reject dialogue topic 검토, 반응 source와 실제 판매 사실 분리 | 구매/거절/commit 실패 각각 올바른 의미. internal ideal/probability 숨김. 기존 Evaluate 보존 | COM-01, Q-C03–05 |
| W-03 P0 | UI 접근·입력: 현재 Space tooltip·dialog/hint 겹침·Phone DispatchLog 진입 문제의 실제 경로 확인/보완 | 세계/모달 owner 일치, 놓친 기록 접근·복귀, 720p/1080p 읽기 | DAY-01/PRE-01, Q-A06/Q-D06 |
| W-04 P1 | 낚시 착지/최종 획득의 비용: 현재 `ConsumeSuccess` 시점·사용 instance·포획 도구 비용을 Canon에 대조 | 착지 실패/최종 지급 실패/취소/도구 변경을 포함한 비용 표와 재현 영상. 검토 결과에 따른 최소 보정 | ACT-02, Q-A07/U-04 |
| W-05 P1 | 배치 복원·경합: CancelPreview false를 caller가 처리하는지, preview 중 OPEN·영역/문/동선 검사 | kit·원본·점유·stock 보존, 복원 실패 때 재시도, 고객 자연 경로 유지 | PLC-01, Q-P02–04/Q-P07 |
| W-06 P1 | 자연 완주 증거: 같은 standalone NEW GAME 후보에서 fixture 없는 Must Path·선택 활동 생략 경로 | Canon Pass1/2/3의 실제 입력/화면/음향·미해결 결함 목록, Report 데이터 일치 | Q-V01–05/Q-D02 |
| W-07 P2 | 항해/보고서 표현: 동행·지리·clock·입력 복귀, 사실 기반 댓글 | Canon 항해 목표·도착 상태 충족, 없는 사건 언급 없음, 닫기 정상 복귀 | PRE-01, Q-D01/Q-C07 |

W-06은 마지막에만 시작하는 작업이 아니다. W-01부터 짧은 자연 경로를 계속 찍고 마지막 같은 후보 완주로 연결한다. fixture 회귀는 어려운 실패/경합을 재현하는 데 쓰되 제품 품질 증거와 구분한다.

## 시스템 간 최소 사실 계약 H

| 사실 | 단일 실제 소유자 | 읽는 쪽 | 불변 조건 |
|---|---|---|---|
| 자원 작업/드롭 | 기존 대상 component | motion/audio/world feedback | 실패 비용/보상 없음 |
| 실제 획득 | 기존 Inventory/활동 지급 | hotbar/held/획득 표현 | 실재 증가량과 물건 일치 |
| 배치 성공/복원 | 기존 placement service | settlement/nav/ghost/UI | 같은 instance·점유·가방 보존 |
| Root/정착 완료 | 기존 settlement/growth 경로 | clock/phone/workbench | 1회 결정/보상, 선택 활동 비의존 |
| 가격 확정 | ShopPriceUI→ShopSlot | display/NPC 평가 | pending과 운영 값 분리 |
| 구매 판단 | PurchaseEvaluator를 호출한 기존 NPC | bubble/read-only insight | RNG를 표시 UI가 다시 호출하지 않음 |
| 실제 판매 | ShopSlot→Economy/SalesLog | 통화/품절/report | 1거래·1수량·1기록 |
| 영업 종료 | 기존 shop loop | NPC/정산/report/UI | 보고서 뒤 지연 매출/중복 close 없음 |

이는 새 event bus API 명세가 아니다. 기존 callback을 재사용할 때 같은 사실이 어디서 오는지 정하는 표다. 애니메이션/UI가 해당 사실의 두 번째 권위가 되면 안 된다.

## 다음 AI에게 넘길 때

작업 ID와 관련 문서, Canon 조항, 현재 실제 코드 경로, 미확인 질문, 완료 기준을 함께 넘긴다. 구현 완료 보고에는 ‘무엇을 바꿨나 → 어떤 상태/실패를 보존했나 → 실제 입력에서 무엇을 확인했나 → 남은 경계’를 쓴다. 연구 제안을 이미 승인된 새 기능처럼 다루지 않는다. 예를 들어 고객의 수동 계산대/운반 상태 추가는 이 문서의 의무가 아니다.
