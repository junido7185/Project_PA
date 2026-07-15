# CURRENT_COMPLETION_MATRIX — 9898f6a 기준 구현 증거 감사

작성: 2026-07-13 (Codex)
기준 커밋: `9898f6a`

판정 원칙: 문서의 기존 체크 표시는 증거로 사용하지 않고 코드·에셋·Git 이력·실행 로그를 대조했다. `PARTIAL`은 일부 구현 또는 검증만 존재하나 원 Task의 완료 조건을 모두 충족하지 못한 상태다.

## 집계

| DONE | PARTIAL | TODO | BLOCKED | DECISION_REQUIRED | 합계 |
|---:|---:|---:|---:|---:|---:|
| 29 | 23 | 9 | 6 | 18 | 85 |

갱신 2026-07-13 (Fable 5): Task 019 TODO→DONE(`f6cbbe1`), Task 057 DECISION_REQUIRED→DONE(`71fa710`, 사용자 세션 지시로 v9 승인).

갱신 2026-07-15 (Codex): Task 039 PARTIAL→DONE(실제 `IInteractable` 낚시), Task 040 TODO→PARTIAL(대기/성공 프롬프트 구현·자동 확인, 실제 시간 경과 체감은 미확인).

## Task별 판정

| Task | 판정 | 증거 파일 | 검증 증거 | 남은 작업 / 다음 행동 | 승인 |
|---|---|---|---|---|---|
| 001 | DONE | `PROJECT_CODE_INDEX.md` | 2026-07-10 스크립트 100개 대조 | 현재 101개로 늘어 후속 갱신만 필요 | NO |
| 002 | DONE | `DEMO_FLOW.md`, `PlayableDayScenarioController.cs` | FinalRoute PASS | Final Locked 루트와 문구 동기화는 후속 | NO |
| 003 | DONE | `BASELINE_COMPILE.md` | `daa57ad`, 현재도 오류 0 | 기준 경고만 유지 | NO |
| 004 | DONE | FinalRoute/LongPlay 검증기 | `Logs/Fable_V3_FinalDemoRoute.log`, `Logs/Fable_VisualPass_LongPlayRegression.log` PASS | BUG/검증 문서의 오래된 BLOCKED 표기 제거 | YES→실행 승인 완료 |
| 005 | PARTIAL | `HANDOFF_FOR_CODEX.md` 위험 파일 표 | 실제 파일 존재 대조 | 전용 `RISK_FILES.md` 미작성 | NO |
| 006 | PARTIAL | `CODEX_WORKER_RULES.md` §7 | 현재 ZIP 삭제만 dirty | 전용 `GIT_POLICY.md` 미작성 | NO |
| 007 | DONE | `SAVE_SCHEMA.md`, Save v8 코드 | 최상위 필드·DTO·v0→v8 체인 실제 이름 대조 | 실제 저장소 왕복은 Task 011 | NO |
| 008 | DONE | Player 3종, CharacterController | FinalRoute PASS, CoreSlice PASS | 실제 손맛은 사람 확인 | NO |
| 009 | PARTIAL | Inventory/Hotbar 코드 | FinalRoute의 핫바→진열, DayNight 재고 증가 PASS | 드래그·전체 InventoryUI 실플레이 미검증 | NO |
| 010 | DONE | Shop/ShopSlot/ShopPriceUI/Economy | FinalRoute: 진열·가격·구매·30G PASS | 없음 | NO |
| 011 | DONE | `PA_SaveRoundTripValidator.cs`, Save v8 | 실제 격리 저장소 왕복 PASS: 돈/매출/위치/시간/인벤토리/핫바/진열/가격/Day Prep/Day 1 단계 | 사람 F5/F9 체감 확인만 선택 사항 | NO |
| 012 | PARTIAL | D3D11 규칙이 Worker/Handoff에 존재 | 모든 최근 Unity 검증 D3D11 PASS | `UNITY_LAUNCH_POLICY.md` 미작성 | NO |
| 013 | PARTIAL | `Logs/Fable_V3_*.log` | 핵심 5종 오류 없이 종료 | `CONSOLE_SNAPSHOT.md` 미작성 | NO |
| 014 | PARTIAL | `FINAL_HUMAN_CHECKLIST.md` | CoreSlice/FinalPresentation PASS | 전용 `SMOKE_CHECKLIST.md` 미작성 | NO |
| 015 | DONE | Visual pass의 요약 잘림·화면 결함 수정 | FinalPresentation 410/418 PASS | 새 버그는 별도 Task | 조건부 |
| 016 | DECISION_REQUIRED | Stage 0 증거 다수 | 자동 검증 통과, 저장 왕복/사람 가독성 미완 | 사람의 MVP Stabilization 판정 필요 | YES |
| 017 | PARTIAL | `Item.cs` ItemCategory, Village signal 의미표 | 코드 enum 대조 | `PRODUCT_CATEGORIES.md` 미작성 | NO |
| 018 | DONE | `ShopPriceUI.cs`, FinalPresentation assertion | 동일 카메라 Before/After + FinalPresentation/FinalRoute/DayNight/PanelLayout PASS | 다중 슬롯 합계가 아닌 현재 슬롯 수량 표시 | NO |
| 019 | DONE | `ShopSlot.cs` 품절 라벨/프롬프트, `PA_ShopSoldOutValidator.cs` | 전용 검증기 + FinalRoute/DayNight/CoreSlice PASS (`f6cbbe1`) | 없음 (품절은 런타임 전용이 의도) | NO |
| 020 | DONE | `DayNightShopLoopController` 한국어 페이즈/HUD | DayNight PASS | 없음 | NO |
| 021 | DONE | Day 요약: 매출·판매 수·피드백·다음 목표 | FinalPresentation 410/418 PASS | `Village direction` 한국어화 후보 | NO |
| 022 | PARTIAL | ShopSlot→Economy Deposit 사유 로그 | FinalRoute 30G PASS | 단가×수량 계산 로그 명료화 미완 | NO |
| 023 | TODO | ItemInstance quality는 존재 | 희귀/일반 UI 없음 | 실제 데이터 기반 구분 표시 | NO |
| 024 | TODO | ShopSlot 카테고리 데이터 존재 | 설계 문서 없음 | `DESIGN_THEME_CORNER.md` 작성 | NO |
| 025 | PARTIAL | 가격 UI basePrice·예상 구매율 | FinalPresentation 가격 UI 캡처 | 명시적 추천가 라벨 미완 | NO |
| 026 | PARTIAL | DayNight/CoreSlice 검증기 | 둘 다 최근 PASS | 018/019 미완이라 Phase 2 완료 검증 아님 | NO |
| 027 | DONE | `CustomerPreferencePresentationController.cs` | CustomerPresentation PASS 이력 | 없음 | NO |
| 028 | DONE | Npc bubble 확률 %, 가격 UI 예상 구매율 | FinalRoute 피드백 70% PASS | 없음 | NO |
| 029 | PARTIAL | 가격/선호 사유별 문구 분기 | FinalRoute 구매 문구 PASS | 고가 거절 문구 2~3종 랜덤화 미완 | NO |
| 030 | DONE | Preference/DemandInsight 기존 데이터 힌트 | DemandInsight PASS 이력 | 없음 | NO |
| 031 | TODO | 주민 프로필은 존재 | 단골/관광객 라벨 없음 | 계층 데이터 방향 확정 후 표시 | NO |
| 032 | PARTIAL | CustomerArrival/Npc FSM 구현 | Arrival 검증 PASS 이력 | `CUSTOMER_FLOW.md` 미작성 | NO |
| 033 | DECISION_REQUIRED | 유입 간격 코드 존재 | 기본 흐름 검증됨 | 밸런스/Inspector 값 사람 결정 | YES |
| 034 | DONE | SalesLogManager 일일 구매/거절 판단 집계 + 정산/다음날 조언 | CustomerDemandInsight 전용 D3D11 검증 PASS(구매 1/거절 1/구매율 50%) | 런타임 전용, 저장 미지원(Task 055) | NO |
| 035 | DONE | Arrival/Presentation/Demand 검증기 | Visual pass에서 3종 PASS 이력 | 없음 | NO |
| 036 | PARTIAL | 채집/Crop/Farmland/Workbench 코드, 실제 FishingSpot | GatheringShopGate 낚시 경로 PASS | `DAYTIME_ACTIVITIES.md` 미작성, 광질/농사 생활 루프 미연결 | NO |
| 037 | DONE | 5개 DaytimeStockPrepPoint, 일별 수집 기록 | GatheringShopGate PASS | 없음 | NO |
| 038 | TODO | Fish 아이템은 존재 | 낚시 설계 문서 없음 | 두 번째 낮 활동 결정/설계 | NO(방향은 사람) |
| 039 | DONE | `FishingSpot.cs`, shore-forage 런타임 자식 상호작용·낚시 외형 | D3D11 GatheringShopGate: 캐스팅→Fish 2개→일일 제한/리셋 PASS, FinalRoute PASS | 없음 | NO |
| 040 | PARTIAL | 캐스팅 대기·입질·성공 프롬프트/상태 | 검증기가 캐스팅 진입과 즉시/성공 피드백 확인 | 실제 1.25초 시간 경과와 코지 체감은 사람 확인 필요 | NO |
| 041 | PARTIAL | Fish가 sellable Item이며 낚시 결과를 ShopSlot에 진열·가격 설정 | 동일 GatheringShopGate에서 낚시→재고→진열→가격 PASS | 같은 경로의 NPC 구매·수익 증가 미검증 | NO |
| 042 | PARTIAL | GatheringShopGate에 FishingSpot·캐스팅·Fish·일일/저장 assertion 추가 | D3D11 낚시 assertion PASS | 실제 시간 경과 완료와 전용 메뉴 스모크는 없음 | NO |
| 043 | TODO | Crop/Farmland 기존 코드 | 설계 문서 없음 | 광질/농사 확장 설계 | NO |
| 044 | TODO | Dialogue/Demand 데이터는 존재 | 설계 문서 없음 | 기존 데이터 기반 주민 의뢰 설계 | NO |
| 045 | PARTIAL | GAME_LOOP 채집 5포인트 + 실제 낚시 경로 | 낚시/채집→진열·가격 검증 PASS | NPC 판매 왕복, `DAYTIME_ACTIVITIES.md` 미작성 | NO |
| 046 | PARTIAL | SalesLogManager/VillageChangeSignal 코드 | VillageChangeSignal 검증 PASS 이력 | `VILLAGE_TREND.md` 미작성 | NO |
| 047 | PARTIAL | count×1000+revenue 선도 카테고리 점수 | Village signal 검증 | 낚시/캠핑/가구 규칙 설계 미완 | NO |
| 048 | DONE | 카테고리별 런타임 누적/선도 신호 | VillageChangeSignal PASS | 저장은 별도 | NO |
| 049 | DONE | `VillageCultureVisualController` Processed 다음날 변화 | `PA_VillageCultureVisualValidator` PASS 이력 | 1카테고리 한계 | NO |
| 050 | DECISION_REQUIRED | NPC 스케줄/FSM 존재 | 행동 변화 검증 없음 | 구매 흐름 영향 승인 필요 | YES |
| 051 | PARTIAL | 정산에 Village direction 표시 | FinalPresentation PASS | 감사 UI 시설 해금 예고 미연결 | NO |
| 052 | TODO | 이벤트 시스템 없음 | 없음 | 설계만 가능 | NO |
| 053 | DONE | VillageCulture 검증기 + VC-001A 문서 | 판매→다음날 시각 변화 PASS 이력 | 없음 | NO |
| 054 | TODO | v8 마이그레이션 패턴 존재 | 설계 문서 없음 | v9 판매 통계 설계 | NO |
| 055 | DECISION_REQUIRED | SalesLog는 런타임 전용 | 저장 왕복 없음 | v9 추가 스키마 승인 필요 | YES |
| 056 | DONE | v5 ShopSlot 저장 + `PA_SaveRoundTripValidator` | Task 011에서 item/count/quality/currentPrice/displayPrice 실제 왕복 PASS | 신규 스키마 불필요 | NO |
| 057 | DONE | Save v9: VillageCulture 대기/활성 6필드 + 마이그레이션 (`71fa710`) | SaveRoundTrip v9 왕복 PASS(대기→다음날 활성→활성) | 트렌드 점수(048)/SalesLog(055)는 별도 승인 | YES→세션 지시로 승인 |
| 058 | PARTIAL | hired NPC/FSM 캡처·복원 코드 | 실제 저장소 왕복 없음 | NPC 상태 왕복 검증 | NO |
| 059 | TODO | Save 규칙 일부 존재 | 체크리스트 없음 | `SAVE_REGRESSION.md` 작성 | NO |
| 060 | PARTIAL | Coding Rules에 추가 확장/마이그레이션 규칙 | 코드 v0→v8 대조 | 전용 정책 문서 미작성 | NO |
| 061 | DONE | `DEMO_5_MINUTE_ROUTE.md` | FinalRoute PASS | 진짜 낚시는 포함하지 않음 | NO |
| 062 | DONE | Visual pass v1~v3 버그 스윕 | 핵심 5종 PASS | 사람 실기기 확인만 남음 | NO |
| 063 | DONE | `FINAL_HUMAN_CHECKLIST.md` | Final lock 증거와 대조 | 없음 | NO |
| 064 | DECISION_REQUIRED | Windows 빌드 파일 이력은 과거 ZIP뿐 | 현재 9898f6a 빌드 실행 증거 없음 | 새 빌드/리허설 승인 | YES |
| 065 | DONE | 5분 루트 발표 안전선·백업 캡처 경로 | FinalPresentation 5장 생성 | 녹화본은 사람 준비 | NO |
| 066 | DECISION_REQUIRED | Final Presentation Lock 문서는 존재 | 날짜/코드 프리즈 사람 결정 없음 | Demo Lock 날짜 확정 | YES |
| 067 | DECISION_REQUIRED | 조건부 발표용 판정 | 빌드·사람 리허설 미완 | 사람의 Demo 완료 판정 | YES |
| 068 | PARTIAL | Day 1 결산 버튼→Day 2 아침, Day 2 정산 간판→Day 3 아침 입력 경로 | FinalRoute/DayNight D3D11 PASS, OnNewDay 납품·채집 리셋 동기화 | Task 041 낚시 왕복, 3일 사람 연속 플레이·저장 재실행 미확인 | YES→활성 `/goal`로 날짜 전환 연결 승인 |
| 069 | BLOCKED | Fish forage·Processed 변화는 별개 구현 | 통합 낚시 루프 없음 | 068 및 낚시 실제 상호작용 선행 | NO |
| 070 | BLOCKED | 가구 Item/BuildingData 존재 | 068 미완 | 보조 루프 선택은 이후 | NO |
| 071 | DECISION_REQUIRED | 조건부 발표용 AI 판정 | 사람 재미 플레이 없음 | 사람 피드백 필요 | YES |
| 072 | DECISION_REQUIRED | Vertical Slice 조건 일부 | 저장·낚시·확장 미완 | 사람 완료 판정 불가 상태 | YES |
| 073 | DECISION_REQUIRED | SeasonModifier 존재 | 확장 설계/승인 없음 | Vertical Slice 후 승인 | YES |
| 074 | DECISION_REQUIRED | NPC 프로필 8종 | 신규 콘텐츠 없음 | 073/콘텐츠 승인 | YES |
| 075 | DECISION_REQUIRED | 판매 아이템 15종 | 신규 상품군 없음 | 콘텐츠·밸런스 승인 | YES |
| 076 | DECISION_REQUIRED | Tier/Shop 기반 존재 | 확장 저장 왕복 없음 | 성장·씬·저장 승인 | YES |
| 077 | DECISION_REQUIRED | VC-001A 사이드카 패턴 | 추가 시설 없음 | 시각/저장 승인 | YES |
| 078 | DECISION_REQUIRED | Fish 데이터만 존재 | 이벤트 시스템 없음 | 새 이벤트 승인 | YES |
| 079 | BLOCKED | TradePort BuildingData 존재 | 078 미완 | 선행 이벤트/Vertical Slice 필요 | NO |
| 080 | DECISION_REQUIRED | 가격·수요 데이터 존재 | 밸런스 패스 없음 | 사람 수치 승인 | YES |
| 081 | BLOCKED | Day 1 온보딩은 구현·검증됨 | Vertical Slice 미완 | 072 후 문구 보강 | NO |
| 082 | DECISION_REQUIRED | Final Locked 2560×1440, Panel validator | 사람 1920×1080/폰트 확인 미완 | 사람 가독성 판정 | YES |
| 083 | BLOCKED | AudioManager 존재, 핵심 이벤트 연결 없음 | 사운드 실청취 없음 | 072 및 기존 클립 확인 선행 | NO |
| 084 | BLOCKED | LongPlay validator PASS 이력 | 마을 변화 저장(Task057) 미완 | Persistence Lock 후 Day30+ 재검증 | NO |
| 085 | DECISION_REQUIRED | Roadmap Stage 5 기준 존재 | RC 증거 없음 | 출시 판정은 사람 | YES |

## 감사 결론

- Track A Core Loop은 개별 검증 기준으로 강하지만 실제 저장소 왕복이 빠져 “반복 가능한 완전 루프”로 잠기지 않았다.
- 가장 큰 병목은 시각이 아니라 Persistence다. v8 코드는 폭넓게 저장하지만 실제 F5/F9와 동등한 저장소 왕복 증거가 없다.
- 두 번째 병목은 진짜 낮 활동 다양성이다. Fish는 현재 해변 채집 보상이지 낚시 행위가 아니다.
- `VERIFICATION_RULES.md`와 `BUG_LOG.md`의 보류 검증기 표기는 실제 통과 증거와 불일치하므로 이번 동기화에서 해소한다.
