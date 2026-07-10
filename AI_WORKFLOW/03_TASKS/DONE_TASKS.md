# DONE_TASKS — 완료 작업 기록

완료한 작업을 최신이 위로 오도록 기록한다. `TASK_QUEUE.md`의 상태도 함께 `DONE`으로 바꾼다.

| Task ID | 완료 날짜 | 수정 파일 | 검증 결과 | 커밋 해시 | 남은 위험 | 다음 작업 |
|---|---|---|---|---|---|---|
| Task 003 | 2026-07-10 | `AI_WORKFLOW/03_TASKS/BASELINE_COMPILE.md`, `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md`, `AI_WORKFLOW/03_TASKS/DONE_TASKS.md`, `AI_WORKFLOW/05_LOGS/CHANGELOG_AI.md`, `AI_WORKFLOW/06_HANDOFF/HANDOFF_FOR_CODEX.md` | `dotnet build Assembly-CSharp.csproj --nologo` 실행. Exit code 0, 경고 1개, 오류 0개. | 이번 작업 커밋 | `CS8785 AttributeBasedFieldGenerator` 경고 1개는 기준선으로 남김. Play Mode/검증기 미실행. | Task 004 승인 확인 또는 Task 005 |
| Task 002 | 2026-07-10 | `AI_WORKFLOW/03_TASKS/DEMO_FLOW.md`, `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md`, `AI_WORKFLOW/03_TASKS/DONE_TASKS.md`, `AI_WORKFLOW/05_LOGS/CHANGELOG_AI.md` | 문서 생성. README Demo Route 10단계와 실제 코드 진입점을 텍스트로 대조. Unity/Play Mode 미실행. | 이번 작업 커밋 | 씬 Inspector 연결, 실제 UI 표시, Play Mode 흐름은 미검증. | Task 003 |
| Task 001 | 2026-07-10 | `AI_WORKFLOW/03_TASKS/PROJECT_CODE_INDEX.md`, `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md`, `AI_WORKFLOW/03_TASKS/DONE_TASKS.md`, `AI_WORKFLOW/05_LOGS/CHANGELOG_AI.md`, `AI_WORKFLOW/06_HANDOFF/HANDOFF_FOR_CODEX.md` | 문서 생성. `Assets/Scripts` 실제 `.cs` 100개와 인덱스 항목 100개 대조. 컴파일/Unity 검증 해당 없음. | 미커밋 | 코드/씬/에셋 미검증. 역할 요약 중 일부는 추정 표시 유지. | Task 002 |
| — | — | (아직 없음) | — | — | — | Task 001 |

## 기록 규칙

- **Task ID**: `TASK_QUEUE.md`의 번호 (예: Task 001).
- **검증 결과**: 실제 실행한 것만. 못 한 항목은 "미검증"으로. 추정 금지.
- **커밋 해시**: 사용자가 커밋한 경우만 기입. 미커밋이면 "미커밋".
- **남은 위험**: 이 작업이 남긴 잠재 문제나 후속 확인 필요 사항.
- **다음 작업**: 이 작업의 후속으로 권장되는 Task ID.
