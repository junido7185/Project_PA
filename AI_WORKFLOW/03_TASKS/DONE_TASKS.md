# DONE_TASKS — 완료 작업 기록

완료한 작업을 최신이 위로 오도록 기록한다. `TASK_QUEUE.md`의 상태도 함께 `DONE`으로 바꾼다.

| Task ID | 완료 날짜 | 수정 파일 | 검증 결과 | 커밋 해시 | 남은 위험 | 다음 작업 |
|---|---|---|---|---|---|---|
| Task 065 | 2026-07-13 | `DEMO_5_MINUTE_ROUTE.md`, Final Presentation 캡처 | 실패 대비 스크린샷 전환 경로 확인 | `9898f6a` | 백업 녹화본은 사람 준비 | Task 064 |
| Task 063 | 2026-07-13 | `FINAL_HUMAN_CHECKLIST.md` | Final Locked/검증 결과와 대조 | `9898f6a` | 실기기 확인 필요 | Task 064 |
| Task 062 | 2026-07-13 | Visual pass v1~v3 관련 파일 | 핵심 검증기 5종 PASS | `9898f6a` | 사람 Game View 확인 | Task 064 |
| Task 061 | 2026-07-13 | `DEMO_5_MINUTE_ROUTE.md` | FinalRoute PASS | `9898f6a` | 진짜 낚시는 미포함 | Task 063 |
| Task 053 | 2026-07-09 | `PA_VillageCultureVisualValidator.cs`, VC-001A 문서 | 판매→다음날 변화 PASS | `8e79c0c` | Processed 1종만 | Task 054 |
| Task 049 | 2026-07-09 | `VillageCultureVisualController.cs` | VillageCultureVisual PASS | `8e79c0c` | 저장 영속화 없음 | Task 053 |
| Task 048 | 2026-06-26 | `VillageChangeSignalController.cs` | VillageChangeSignal PASS 이력 | `87c1e44` | 런타임 최근 판매만 | Task 049 |
| Task 037 | 2026-06-26 | `DaytimeStockPrepPoint.cs`, DayNight loop | GatheringShopGate PASS | `87c1e44` | 채집만, 낚시 아님 | Task 038 |
| Task 035 | 2026-06-26 | 고객 검증기 3종 | Arrival/Presentation/Demand PASS 이력 | `87c1e44` | 관광객 계층 없음 | Task 036 |
| Task 030 | 2026-06-26 | Customer preference/demand UI | DemandInsight PASS | `87c1e44` | 요청 시스템 없음 | Task 031 |
| Task 028 | 2026-07-13 | Npc bubble, ShopPriceUI | FinalRoute 피드백 70% | `9898f6a` | 없음 | Task 029 |
| Task 027 | 2026-06-26 | `CustomerPreferencePresentationController.cs` | CustomerPresentation PASS | `87c1e44` | 없음 | Task 028 |
| Task 021 | 2026-07-13 | Day 1 summary UI | FinalPresentation 410/418 PASS | `9898f6a` | 영문 섹션 제목 | Task 022 |
| Task 020 | 2026-07-12 | DayNight HUD | DayNight PASS | `2b9c49b` | 없음 | Task 021 |
| Task 015 | 2026-07-13 | Visual pass 안정화 파일 | FinalPresentation/CoreSlice PASS | `9898f6a` | 실기기 확인 | Task 016 |
| Task 010 | 2026-07-13 | Shop core 기존 구현 | FinalRoute: 진열·가격·구매·30G PASS | 구현은 기존, 증거 `9898f6a` | 없음 | Task 011 |
| Task 008 | 2026-07-13 | Player 기존 구현 | FinalRoute/CoreSlice PASS | 구현은 기존, 증거 `9898f6a` | 이동 감각은 사람 확인 | Task 009 |
| Task 004 | 2026-07-13 | 검증 로그/상태 문서 | FinalRoute·LongPlay D3D11 PASS | 증거 `9898f6a` | D3D12 미승인 유지 | Task 005 |
| Task 003 | 2026-07-10 | `AI_WORKFLOW/03_TASKS/BASELINE_COMPILE.md`, `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md`, `AI_WORKFLOW/03_TASKS/DONE_TASKS.md`, `AI_WORKFLOW/05_LOGS/CHANGELOG_AI.md`, `AI_WORKFLOW/06_HANDOFF/HANDOFF_FOR_CODEX.md` | `dotnet build Assembly-CSharp.csproj --nologo` 실행. Exit code 0, 경고 1개, 오류 0개. | 이번 작업 커밋 | `CS8785 AttributeBasedFieldGenerator` 경고 1개는 기준선으로 남김. Play Mode/검증기 미실행. | Task 004 승인 확인 또는 Task 005 |
| Task 002 | 2026-07-10 | `AI_WORKFLOW/03_TASKS/DEMO_FLOW.md`, `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md`, `AI_WORKFLOW/03_TASKS/DONE_TASKS.md`, `AI_WORKFLOW/05_LOGS/CHANGELOG_AI.md` | 문서 생성. README Demo Route 10단계와 실제 코드 진입점을 텍스트로 대조. Unity/Play Mode 미실행. | 이번 작업 커밋 | 씬 Inspector 연결, 실제 UI 표시, Play Mode 흐름은 미검증. | Task 003 |
| Task 001 | 2026-07-10 | `AI_WORKFLOW/03_TASKS/PROJECT_CODE_INDEX.md`, `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md`, `AI_WORKFLOW/03_TASKS/DONE_TASKS.md`, `AI_WORKFLOW/05_LOGS/CHANGELOG_AI.md`, `AI_WORKFLOW/06_HANDOFF/HANDOFF_FOR_CODEX.md` | 문서 생성. `Assets/Scripts` 실제 `.cs` 100개와 인덱스 항목 100개 대조. 컴파일/Unity 검증 해당 없음. | 미커밋 | 코드/씬/에셋 미검증. 역할 요약 중 일부는 추정 표시 유지. | Task 002 |

## 기록 규칙

- **Task ID**: `TASK_QUEUE.md`의 번호 (예: Task 001).
- **검증 결과**: 실제 실행한 것만. 못 한 항목은 "미검증"으로. 추정 금지.
- **커밋 해시**: 사용자가 커밋한 경우만 기입. 미커밋이면 "미커밋".
- **남은 위험**: 이 작업이 남긴 잠재 문제나 후속 확인 필요 사항.
- **다음 작업**: 이 작업의 후속으로 권장되는 Task ID.
