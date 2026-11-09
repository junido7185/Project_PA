# DEMO_UI_STATUS — 데모 UI 항목별 상태

작성: 2026-07-12 (Claude Fable 5)
기준: 1920x1080, `PA_FinalPresentationReviewer` 2026-07-12 통과 + 캡처 5장 (`Logs/FinalPresentation/20260712_161412/`)

| 항목 | 상태 | 구현 | 비고 |
|---|---|---|---|
| 시간/날짜 | **DONE** | `ClockHUD` 좌상단 (시각 + Day·계절) | 페이즈 스트립과 시간 이중 표기 — 통합은 후속 |
| 돈 | **DONE** | `MoneyHUD` 우상단 (잔액 G, 판매 즉시 갱신) | |
| 티어/다음 목표 | **DONE** | `MoneyHUD` "Tier 0 · 생존자 / 다음: Tier 1 지점장 · 매출 10,000G 남음" | |
| 목표(스테이지) | **DONE** | `PlayableDayScenarioController` 상단 목표 패널 (1~7단계 진행) | |
| 페이즈/영업 상태 | **DONE** | `DayNightShopLoopController` 스트립 — 이번 패스로 한국어 통일 ("1일차 07:00 · 낮 준비 / 상점: 준비 중…") | |
| 핫바/인벤토리 | **DONE** | `HotbarUI` 하단 (아이콘+수량+선택 하이라이트), I 인벤토리, 드래그&드롭 | |
| 상호작용 힌트 | **DONE** | `InteractPromptUI` "[Space] 판매대에 상품 진열" 등 — 채집/간판 프롬프트 이번 패스로 한국어화 | |
| 가격 설정 | **DONE** | `ShopPriceUI` (진열 수량 + 가격 파생 일반/희귀 + 실제 품질 + 읽기 전용 추천 기준가 + 가격 조정/확정/회수 + NPC 반응 힌트 + 구매율) | Task 018 수량, Task 023 분류/품질, Task 025 `basePrice+quality` 추천가 추가 |
| 구매 반응 | **DONE** | `NpcBubbleUI`의 별도 `[주민]/[관광객]` 계층 태그 + 기존 "Farmer_01: 가격 적정, 구매 (71%)" 본문/거절 이유 | Task 031은 현재 8명 전원을 실제 일과표 기반 주민으로 표시하며 구매 수학은 변경하지 않음 |
| 영업 결과(정산) | **DONE** | Day 요약 (매출/돈 변화/판매 수/피드백/다음 행동/성장 목표) — 이번 패스로 본문 잘림(410/360) 수복 | |
| 마을 변화 알림 | **DONE** | `VillageCultureVisualController` 다음날 힌트 패널 + 가공품 코너 활성 / 요약 내 "Village direction" 섹션 | 요약 섹션 제목이 아직 영어 — 후속 한국어화 후보 |
| 감사/성장 앱 | **DONE** | `SmartphoneUI` 폰(P) + `AuditResultUI` (티어 요건/카운트다운) | |
| 수요/성향 패널 | **PARTIAL** | `CustomerDemandInsight`/`CustomerPreference` 패널은 기본 숨김(F10 개발 오버레이) | 데모에서는 의도적으로 숨김 유지 |
| 일시정지/설정 | **PARTIAL** | `PauseManager`+`SettingsUI` 존재 | 데모 루트에 없어 가독성 미점검 |
| 잔여 TODO | **TODO** | 시간 표기 이중화 해소, "Village direction" 제목 한국어화, 폰 UI(분홍 DT 패널) 톤 정리 | |

## 사람 확인 필요

- 한국어 폰트 실기기 렌더링과 광장 드레싱의 주관적 배치 품질 (자동 검증은 기하/문자열만 봄).
- F10 토글로 개발 오버레이 표시/숨김이 데모 리허설에서 의도대로인지.
