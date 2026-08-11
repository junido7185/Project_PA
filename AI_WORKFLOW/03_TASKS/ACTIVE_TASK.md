# ACTIVE_TASK — 현재 진행 중인 단일 작업

사용법: `TASK_QUEUE.md`에서 작업 하나를 골라 아래 양식에 **복사**해 채운다. 동시에 두 작업 금지.
작업이 끝나면 이 파일을 비우고, `TASK_QUEUE.md` 상태를 `DONE`으로, `DONE_TASKS.md`에 한 줄 기록, 3~5개마다 `HANDOFF_FOR_CODEX.md` 갱신.

먼저 읽을 문서: `../00_START_HERE/ONE_PAGE_WORKFLOW.md`, `../02_AGENT_RULES/`(3종), `../04_VERIFICATION/VERIFICATION_RULES.md`.

---

## 현재 작업

없음. `LOOP-POLICY-002 Enable Preapproved Autonomous Milestone Continuation`은 2026-08-11 완료됐다. 기본 bounded-ticket 사람 게이트를 유지하면서, loop-state에 사람이 승인한 정확한 milestone sequence가 있으면 그 범위 안에서만 다음 ticket을 자동 활성화하는 `PREAPPROVED_MILESTONE_CONTINUATION` 예외를 추가했다. M70 sequence는 이미 WORLD-010과 `M70_PLAYABLE_WORLD_ALPHA_COMPLETE`에 도달했으므로 WORLD-005부터 재실행하지 않는다. WORLD-011 이후와 MainGame 통합은 새 사람 승인 전 활성화하지 않는다.
