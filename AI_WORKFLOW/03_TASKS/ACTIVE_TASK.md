# ACTIVE_TASK — 현재 진행 중인 단일 작업

사용법: `TASK_QUEUE.md`에서 작업 하나를 골라 아래 양식에 **복사**해 채운다. 동시에 두 작업 금지.
작업이 끝나면 이 파일을 비우고, `TASK_QUEUE.md` 상태를 `DONE`으로, `DONE_TASKS.md`에 한 줄 기록, 3~5개마다 `HANDOFF_FOR_CODEX.md` 갱신.

먼저 읽을 문서: `../00_START_HERE/ONE_PAGE_WORKFLOW.md`, `../02_AGENT_RULES/`(3종), `../04_VERIFICATION/VERIFICATION_RULES.md`.

---

## 현재 작업

`BETA-005 Customer Strategy and Feedback` — BETA-004 완료 뒤 M85 선승인 sequence로 자동 활성화된 다음 단일 티켓.

- Baseline: BETA-004 로컬 커밋 직후 `milestone/gameplay-beta-85`
- 목표: 기존 고객 성향·구매 평가·수요 신호·피드백 권위를 WorldSandbox 밤 영업에 읽기 쉽게 연결해 가격과 상품 선택의 전략을 플레이어가 이해하게 한다.
- 보존: `Prototype_FirstDay.unity` Golden, M70 월드/저장 권위, Save schema v11, 기존 gameplay authority.
- 금지: Scene/Prefab/Packages/ProjectSettings/Save schema 변경, PurchaseEvaluator/NpcController/CustomerDemandInsight 권위 재작성, push/rebase/reset/clean.
- 전환 근거: `BETA-004` D3D11 전용 검사와 BETA-003/WORLD-006B 회귀가 모두 PASS; blocking Console 0, 신규 crash 0.
