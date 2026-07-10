# DOCS_INDEX — Project P.A. 전체 문서 지도

작성: 2026-07-09 (근거: 루트 `AI_DOC_CLEANUP_PLAN.md` 감사 결과)
용도: AI 에이전트가 이 파일에서 필요한 문서만 골라 읽는다. 전체 정독 금지 — 최소 컨텍스트 원칙.

## 1. 문서 지도

```
Project_PA/
├─ AGENTS.md                     ← AI 에이전트 입구 (짧은 안내문)
├─ CLAUDE.md                     ← Claude 전용 규칙 (AGENTS와 쌍)
├─ README.md                     ← 빌드/실행/데모 루트
├─ AI_DOC_CLEANUP_PLAN.md        ← 문서 감사 결과 (이동 계획 원본)
├─ PROJECT_PA_*.md               ← 정체성/계획/기록 (아래 분류 참조)
├─ Docs/                         ← 기획 원본(01~08, 동결)·아트 가이드·기능 기록
│  └─ AgentWorkflow/CONTEXT_INDEX.md ← 스프린트별 컨텍스트 라우터
├─ Automation/LoopEngineering/   ← 자동 루프 정책/티켓/로그
└─ AI_WORKFLOW/                  ← AI 운영 문서 (이 폴더)
   ├─ 00_START_HERE/             ← 진입점 (이 파일 + ONE_PAGE_WORKFLOW)
   ├─ 01_IDENTITY/               ← 정체성/루프/범위 (최신 기준)
   ├─ 02_AGENT_RULES/            ← 작업자/코딩/슬롭 방지 규칙
   ├─ 03_TASKS/                  ← 태스크 큐 (준비 중)
   ├─ 04_VERIFICATION/           ← 검증 규칙
   ├─ 05_LOGS/                   ← CHANGELOG_AI / BUG_LOG / DECISION_LOG
   ├─ 06_HANDOFF/                ← 세션 인수인계
   ├─ 07_FULL_GAME_ROADMAP/      ← 완성 게임 로드맵
   └─ 99_ARCHIVE/old_docs/       ← 구버전 보관소
```

## 2. AI가 매번 읽어야 하는 문서 (순서대로)

| 순서 | 문서 | 이유 |
|---|---|---|
| 1 | `AI_WORKFLOW/00_START_HERE/ONE_PAGE_WORKFLOW.md` | 작업 절차·금지·중단 조건 |
| 2 | `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_IDENTITY.md` | 게임 정체성 (재해석 금지) |
| 3 | `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_SCOPE.md` | 지금 어느 단계 범위인지 |
| 4 | `AI_WORKFLOW/02_AGENT_RULES/` 3종 | 작업자 규칙 / Unity 코딩 규칙 / 슬롭 방지 |
| 5 | `AI_WORKFLOW/06_HANDOFF/HANDOFF_FOR_CODEX.md` | 현재 상태·우선순위·위험 파일 |
| 6 | `PROJECT_PA_TODO.md` (최신 섹션만) | 진행 중 체크리스트 |
| 7 | 작업 유형별 추가 문서 | `Docs/AgentWorkflow/CONTEXT_INDEX.md` 라우팅을 따름 |

Codex 실행 관련: 첫 실행은 `CODEX_FIRST_RUN_PLAYBOOK.md`, 상황별 프롬프트 선택은 `PROMPT_LIBRARY.md`, 사용자 미확정 항목은 `DECISION_REQUIRED_FOR_USER.md` 참조 (모두 `00_START_HERE/`).

작업 종료 시 갱신: `AI_WORKFLOW/05_LOGS/CHANGELOG_AI.md`, `PROJECT_PA_STATUS.md`, `PROJECT_PA_TODO.md`, `PROJECT_PA_SESSION_REPORT.md`, `Docs/07_개발일지.md` (+루프 작업이면 `Automation/LoopEngineering/progress.md`, `State/loop-state.json`).

## 3. 작업 유형별 문서 (요약)

세부 라우팅·검증기·스프린트별 금지사항은 `Docs/AgentWorkflow/CONTEXT_INDEX.md`가 계속 담당한다.

| 작업 유형 | 추가로 읽을 문서 |
|---|---|
| 게임플레이/루프 | `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_GAME_LOOP.md`, `PROJECT_PA_FULL_GAME_BACKLOG.md`, `PROJECT_PA_CORE_SLICE_PLAN.md` |
| 경제/NPC | `Docs/02_경제_및_아이템_설계.md`, `Docs/03_NPC_및_AI_시스템.md`, 기능 기록 3종 (`Docs/CustomerPresentation/`, `Docs/IslandLife/`, `Docs/VillageCulture/`) |
| 아트/씬/UI | `Docs/08_아트_및_씬_구성_가이드.md`, `Docs/VisualTargets/VISUAL_TARGETS.md`, `Docs/UI_스프라이트_가이드.md` |
| 아트 발주 | `Docs/Tripo3D_빌딩_생성_프롬프트.md` |
| 장기 계획/로드맵 | `AI_WORKFLOW/07_FULL_GAME_ROADMAP/FULL_GAME_COMPLETION_ROADMAP.md`, `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md` |
| Unity 실행/검증 | 루트 최신 `PROJECT_PA_CRASH_REPORT_*.md`, `Automation/LoopEngineering/validator-registry.json`, `AI_WORKFLOW/04_VERIFICATION/VERIFICATION_RULES.md` |
| 자동 루프 | `Automation/LoopEngineering/loop-policy.json` → `progress.md` → `ticket-template.md` |

## 4. 사람이 가끔 확인하면 되는 문서

- `PROJECT_PA_CREATIVE_NORTH_STAR.md` — 창작 방향 상세 원본 (AI_WORKFLOW/01_IDENTITY의 배경 문서)
- `PROJECT_PA_DESIGN_INTENT.md` — 금지선/역-공급망 백본 상세
- `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md` — 1.0 장기 계획
- `PROJECT_PA_CURRENT_MILESTONE.md` — 현재 마일스톤 정의
- `PROJECT_PA_STATUS.md` / `PROJECT_PA_SESSION_REPORT.md` — 상태/세션 이력 (사람의 주기적 리뷰 대상)
- `Docs/기능_명세서.md` — 312항목 명세 (상태표는 2026-04-28 기준)
- `AI_WORKFLOW/05_LOGS/DECISION_LOG.md` — 중요 결정 재검토
- `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md` — 커밋 기준선 검토 (커밋 후 아카이브 예정)

## 5. 동결 문서 (이동·개명·대수정 금지)

| 문서 | 동결 이유 |
|---|---|
| `Docs/01_개요_및_정체성.md` ~ `Docs/08_아트_및_씬_구성_가이드.md` (번호 문서 8종) | **코드 주석이 `Docs/03 §2.2` 형식으로 § 인용** (예: `DialogueData.cs`, `AuditService.cs`, `BuildingEntrance.cs`, `EconomyService.cs`). 위치·이름 영구 동결. 내용 대수정 금지 — 최신 기준은 `AI_WORKFLOW/01_IDENTITY/`에 있다 |
| `PROJECT_PA_CRASH_REPORT_20260625.md` (최신 크래시 리포트) | `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1:86`이 **루트에서 glob** (`PROJECT_PA_CRASH_REPORT_*.md`). 루트 고정 |
| `Assets/Jinxish/Drag & Drop Inventory & Hotbar Framework/readme.md` | 서드파티 에셋, `.meta` 동반, Unity 관리 영역 |

주의: `Docs/01`(순수 관리자 프레임), `Docs/04`(멀티플레이), `Docs/06`(외부 에셋 권장 §3.2)은 **초기 기획 원본**이라 현행 방향과 다르다. 이 문서들을 현재 기준으로 믿지 말 것 — 차이 정리는 `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_IDENTITY.md` §4 참조.

## 6. 이동 금지 문서 (위치 고정, 내용 갱신은 허용)

- 루트 `PROJECT_PA_*` 살아있는 기록: `STATUS`, `TODO`, `SESSION_REPORT` — 다수 문서·워크플로가 루트 경로로 참조
- `PROJECT_PA_CREATIVE_NORTH_STAR.md`, `PROJECT_PA_DESIGN_INTENT.md`, `PROJECT_PA_CURRENT_MILESTONE.md`, `PROJECT_PA_CORE_SLICE_PLAN.md`, `PROJECT_PA_FULL_GAME_BACKLOG.md`, `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md` — `CONTEXT_INDEX.md`가 루트 경로로 참조
- `Docs/AgentWorkflow/CONTEXT_INDEX.md` — `CLAUDE.md`/`AGENTS.md`가 참조하는 라우터
- `Docs/07_개발일지.md` — 매 세션 갱신 대상, 코드/문서 다수가 참조

## 7. 아카이브 문서

### 7-A. 아카이브 완료

| 파일 | 내용 |
|---|---|
| `AI_WORKFLOW/99_ARCHIVE/old_docs/AGENTS_v1_20260626.md` | 루트 `AGENTS.md` 구버전 (2026-07-09 입구 안내문으로 재작성되면서 보관) |

### 7-B. 이동 대기 (⚠ 2026-07-09 기준: Git 워킹트리에 미커밋 VC-001A 변경이 있어 `git mv` 보류)

체크포인트 커밋으로 워킹트리가 정리된 뒤, 아래를 **병합 → 이동 → 참조 갱신을 같은 커밋에서** 실행한다. 절차 상세: 루트 `AI_DOC_CLEANUP_PLAN.md` §5, §9.

| 현재 경로 | 이동 예정 경로 | 선행 조건 |
|---|---|---|
| `Docs/🏝️ 프로젝트 P A (Pioneer Assistance) 기획서 2d3ea077....md` | `AI_WORKFLOW/99_ARCHIVE/old_docs/PA_기획서_노션원본_2026-04.md` (개명) | 없음 (참조 없음 확인됨) |
| `PROJECT_PA_COMPLETION_PLAN.md` | `AI_WORKFLOW/99_ARCHIVE/old_docs/` | "완성 정의" 섹션을 `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md`에 병합 + `CONTEXT_INDEX.md:144` 참조 교체 |
| `PROJECT_PA_RELEASE_BACKLOG.md` | `AI_WORKFLOW/99_ARCHIVE/old_docs/` | CDN/IL/SPY/VC/NPC **ID 표 전체**를 `PROJECT_PA_FULL_GAME_BACKLOG.md`에 이식 |
| `PROJECT_PA_CRASH_REPORT_20260624.md` | `AI_WORKFLOW/99_ARCHIVE/old_docs/` | 없음 (최신본 0625는 루트 유지) |
| `Docs/발표_개발현황보고서.md` | `Docs/Presentation/` | `README.md:218` 참조 교체 |
| `Docs/Project_PA_Week11_SpeakerScript.md` | `Docs/Presentation/` | 없음 |
