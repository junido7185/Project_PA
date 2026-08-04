# Project P.A. — AI 에이전트 입구

> Project P.A.는 **"낮에는 동물 마을을 만들고, 밤에는 그 마을의 유일한 잡화점을 운영하는 3D 코지 라이프 시뮬레이션"**이다. 핵심 차별점: "내가 판 물건이 마을의 풍경과 주민 생활을 바꾼다."

**개발 목표는 단순 프로토타입이 아니라 완성 게임까지 이어지는 개발이다.** 졸업 시연은 중간 마일스톤일 뿐이며, 모든 작업은 완성 게임의 지반이 된다는 전제로 수행한다.

작업 가능 경로는 `C:\Users\sdjsd\Desktop\Unity\Project_PA` 뿐이다. `Project_D`(참조 프로토타입)는 읽기 전용 — 수정·복사·병합 금지.

## 매번 읽어야 할 문서 (순서대로)

1. `AI_WORKFLOW/00_START_HERE/ONE_PAGE_WORKFLOW.md` — 작업 절차·검증·중단 조건
2. `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_IDENTITY.md` — 게임 정체성 (재해석 금지)
3. `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_SCOPE.md` — 현재 개발 단계와 범위
4. `AI_WORKFLOW/02_AGENT_RULES/` — 작업자 규칙 / Unity 코딩 규칙 / AI 슬롭 방지 (3종)
5. `AI_WORKFLOW/06_HANDOFF/HANDOFF_FOR_CODEX.md` — 현재 상태·우선순위·위험 파일
6. 작업 유형별 추가 문서 — `AI_WORKFLOW/00_START_HERE/DOCS_INDEX.md` §3 및 `Docs/AgentWorkflow/CONTEXT_INDEX.md`

## 절대 규칙

- **한 번에 한 작업만** 수행한다.
- **코드 수정 전에 수정 예정 파일 목록을 먼저 보고**한다.
- **전체 시스템 재작성 금지.** 기존 작동 시스템(`Shop`/`EconomyService`/`PurchaseEvaluator`/`NpcController`/`SaveManager` 등)을 갈아엎지 않는다.
- **Unity 씬/프리팹/저장 시스템 임의 변경 금지.**
- **메인 씬 경로(`Assets/Scenes/Prototype_FirstDay.unity`)를 임의로 덮어쓰지 않는다.** 새 씬이 필요해도 사용자 승인 없이 기존 메인 씬을 덮어쓰지 않는다.
- **WORLD 씬 분리 규칙:** `Prototype_FirstDay.unity`는 Golden Regression Scene이며 신규 WorldGrid/Chunk/Terraforming 실험 대상으로 사용하지 않는다. WORLD 티켓은 별도 명시가 없으면 승인된 `Assets/Scenes/WorldSandbox.unity`를 대상으로 한다. `MainGame.unity` 통합은 Gate 1~5와 별도 사람 승인 전 금지한다.
- **WORLD 씬 편집 규칙:** 후속 씬 생성·변경은 editor builder/setup utility와 validator를 우선하며 대규모 YAML/바이너리 직접 편집, 이름만 보고 오브젝트 삭제, serialized reference 일괄 교체를 금지한다.
- **컴파일/테스트 없이 완료 선언 금지.** 검증 기준: `AI_WORKFLOW/04_VERIFICATION/VERIFICATION_RULES.md`. 확인 못 한 항목은 반드시 "확인 못 함"으로 보고.
- **실패 시 `AI_WORKFLOW/05_LOGS/BUG_LOG.md`에 기록하고 멈춘다.** 같은 원인 2회 실패 후 세 번째 시도 금지.
- 외부 패키지 추가 금지. `git push` 금지. 승인 없는 커밋 금지. 파괴적 Git/파일 명령 금지. 파일 삭제 금지.
- `Docs/01~08` 번호 문서는 동결 (코드 주석이 § 인용). 루트 최신 `PROJECT_PA_CRASH_REPORT_*.md`는 이동 금지 (preflight glob).

## 작업 종료 시

`AI_WORKFLOW/05_LOGS/CHANGELOG_AI.md` + `PROJECT_PA_STATUS.md` + `PROJECT_PA_TODO.md` + `PROJECT_PA_SESSION_REPORT.md` + `Docs/07_개발일지.md`를 갱신하고, `AI_WORKFLOW/06_HANDOFF/HANDOFF_FOR_CODEX.md`의 우선순위를 다음 세션용으로 갱신한다.

---

구버전 상세 가이드(2026-06-26)는 `AI_WORKFLOW/99_ARCHIVE/old_docs/AGENTS_v1_20260626.md`에 보존되어 있다.
