# ACTIVE_TASK — 현재 진행 중인 단일 작업

사용법: `TASK_QUEUE.md`에서 작업 하나를 골라 아래 양식에 **복사**해 채운다. 동시에 두 작업 금지.
작업이 끝나면 이 파일을 비우고, `TASK_QUEUE.md` 상태를 `DONE`으로, `DONE_TASKS.md`에 한 줄 기록, 3~5개마다 `HANDOFF_FOR_CODEX.md` 갱신.

먼저 읽을 문서: `../00_START_HERE/ONE_PAGE_WORKFLOW.md`, `../02_AGENT_RULES/`(3종), `../04_VERIFICATION/VERIFICATION_RULES.md`.

---

## 현재 작업

`BETA-003 Crafting and Production Expansion` — M85 선승인 sequence의 다음 단일 활성 티켓.

- Baseline: BETA-002 로컬 커밋 직후 `milestone/gameplay-beta-85`
- 목표: BETA-002의 Carrot/Wheat/Ore/Fish/Wood 자원을 기존 RecipeData/CraftingService/Workbench와 연결해 실제 플레이용 가공품 세트와 판매 가치를 제공한다.
- 보존: `Prototype_FirstDay.unity` Golden, M70 월드/저장 권위, Save schema v11, 기존 gameplay authority.
- 금지: Scene/Prefab/Packages/ProjectSettings/Save schema 변경, 신규 병렬 제작/인벤토리/경제 시스템, push/rebase/reset/clean.
- 전환 근거: `BETA-002` D3D11 네 활동, BETA-001, M70 WORLD-010, Prototype Gathering/Fishing 및 Mining/Shop 회귀 모두 PASS; blocking Console 0.
