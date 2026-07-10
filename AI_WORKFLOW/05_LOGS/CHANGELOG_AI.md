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

## 2026-07-09 — Claude (Fable 5) — 작업 큐 + Codex 프롬프트 세트 + 개발 타임라인 구축

- 작업: Codex 자동 개발 루프를 위한 실행 문서 세트 생성. Git은 clean baseline(커밋 `8e79c0c`) 확인.
- 신규 생성:
  - `03_TASKS/TASK_QUEUE.md` — Phase 0~9, **85개 작업**(XS 15 / S 37 / M 26 / L 7, 사람 승인 21), 각 작업에 난이도·예상 실행 횟수·선행관계·수정 금지 파일 포함.
  - `03_TASKS/ACTIVE_TASK.md`, `03_TASKS/DONE_TASKS.md`.
  - `00_START_HERE/PROMPT_LIBRARY.md` + Codex 프롬프트 10종(`CODEX_BOOTSTRAP/REPEAT/VERIFY/BUGFIX/REFACTOR_GUARD/TASK_SPLIT/HANDOFF/DEMO_LOCK/FINAL_POLISH/FULL_GAME_EXPANSION_PROMPT.md`).
  - `07_FULL_GAME_ROADMAP/DEVELOPMENT_TIMELINE.md` — 난이도 분포·3시나리오(저강도/집중/크런치)·Demo Lock 역산표(D-7/14/30/60)·Fable 최종 판단.
- 갱신: `04_VERIFICATION/VERIFICATION_RULES.md`(씬 로드 검증 §B2, BLOCKED 검증기 §2-B, 2회 실패 사람요청 규칙), `06_HANDOFF/HANDOFF_FOR_CODEX.md`(큐/프롬프트/속도정책 반영, git clean 반영), `05_LOGS/DECISION_LOG.md`.
- 코드/씬/에셋 변경: 없음. Docs/01~08 이동·수정 없음. 최신 크래시 리포트 이동 없음.
- 검증: 문서 작업이라 Unity 검증 해당 없음. TASK_QUEUE의 파일 참조는 실제 `Assets/Scripts` 파일명 기준으로 작성.

## 2026-07-10 — Claude (Fable 5) — Codex 투입 전 최종 프리플라이트 + 첫 실행 패키지

- 커밋: 3회차 문서 세트를 커밋 `53fd115`로 저장(사용자 요청). 그 후 프리플라이트 검수 진행.
- 신규 생성:
  - 감사 보고서 3종: `00_START_HERE/AI_WORKFLOW_FINAL_AUDIT.md`, `00_START_HERE/CODEX_PROMPT_AUDIT.md`, `03_TASKS/TASK_QUEUE_REVIEW.md`.
  - 첫 실행 패키지: `00_START_HERE/CODEX_FIRST_RUN_PLAYBOOK.md`(복붙용 첫/두번째 프롬프트 포함), `07_FULL_GAME_ROADMAP/CODEX_FIRST_7_DAYS_PLAN.md`, `00_START_HERE/DECISION_REQUIRED_FOR_USER.md`(사용자 결정 10건 + 임시 기본값).
- 보완(갈아엎기 아님, 최소 수정):
  - `DEVELOPMENT_TIMELINE.md` 기간 표기를 "약 N주 / N~M주"로 통일(오독 방지), 실행 횟수 "약 N회".
  - `TASK_QUEUE.md` Task 041 선행작업 오타 수정(039, 010) + Review Notes 섹션 추가(YES 작업 스킵 규칙, 첫 실행 연결).
  - `CODEX_REPEAT_PROMPT.md`에 "미승인 YES 작업 스킵" 규칙 1줄 추가.
  - `PROMPT_LIBRARY.md`·`DOCS_INDEX.md`에 첫 실행/결정문서 참조 추가.
  - `HANDOFF_FOR_CODEX.md` §0 첫 실행 안내 추가, 우선순위 날짜 갱신.
- 판정: AI_WORKFLOW는 **Codex 투입 가능**. 첫 실행(Bootstrap + Task 001~003)은 지금 바로 안전.
- 코드/씬/에셋 변경: 없음. Docs/01~08 이동·수정 없음. 최신 크래시 리포트 이동 없음.
