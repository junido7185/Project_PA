# CHANGELOG_AI — AI 작업 변경 이력

AI 에이전트가 수행한 작업을 최신이 아래로 가도록 시간순(append-only)으로 기록한다.
형식: 날짜 / 에이전트 / 작업 / 변경 파일 / 검증 결과 / 비고.

---

## 2026-07-09 — Claude (Fable 5) — AI_WORKFLOW 운영 문서 구조 구축

- 작업: `AI_DOC_CLEANUP_PLAN.md` 기반으로 `AI_WORKFLOW/` 디렉토리 구조와 운영 문서 생성. 루트 `AGENTS.md`를 짧은 입구 안내문으로 재작성 (구버전은 `99_ARCHIVE/old_docs/AGENTS_v1_20260626.md`로 보관).
- 변경 파일: `AI_WORKFLOW/**` 신규 생성 (00_START_HERE 2종, 01_IDENTITY 3종, 02_AGENT_RULES 3종, 03_TASKS README, 04_VERIFICATION 1종, 05_LOGS 3종, 06_HANDOFF 1종, 07_FULL_GAME_ROADMAP 1종, 99_ARCHIVE 2종), 루트 `AGENTS.md` 재작성, 루트 기록 문서 4종 append.
- 코드/씬/에셋 변경: 없음.
- 문서 이동(`git mv`): **보류** — 워킹트리에 미커밋 VC-001A 변경이 있어 필수 제약 4에 따라 이동 없음. 이동 대기 목록은 `../00_START_HERE/DOCS_INDEX.md` §7-B.
- 검증: 문서 작업이므로 Unity 검증 해당 없음. 새 문서의 참조 경로는 실제 파일 기준으로 작성.
- 비고: 개발 목표를 "프로토타입"에서 "완성 게임"으로 문서에 고정 (`../01_IDENTITY/PROJECT_PA_SCOPE.md`).
