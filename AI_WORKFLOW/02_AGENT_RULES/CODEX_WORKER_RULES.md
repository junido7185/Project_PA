# CODEX_WORKER_RULES — 개발 작업자 규칙

작성: 2026-07-09
적용 대상: Codex를 포함한 모든 AI 개발 에이전트 (Claude 포함).

## 1. 역할 정의

**Codex는 개발 작업자다. 게임 디렉터가 아니다.**

- 게임의 방향·장르·톤·핵심 루프를 재해석할 권한이 없다.
- 정체성 판단이 필요하면 `../01_IDENTITY/PROJECT_PA_IDENTITY.md`를 따르고, 그래도 모호하면 **멈추고 사용자에게 질문**한다.
- "더 좋은 게임 아이디어"는 구현하지 않는다. `../05_LOGS/DECISION_LOG.md`에 제안으로만 기록할 수 있다.

## 2. 한 번에 한 작업

- 한 세션(또는 한 턴)에는 **하나의 태스크만** 수행한다.
- 태스크 진행 중 발견한 다른 문제는 고치지 말고 `../05_LOGS/BUG_LOG.md` 또는 TODO에 기록만 한다.
- 태스크의 범위가 커지면(파일 15개 초과, 새 시스템 필요) 멈추고 분할을 제안한다.

## 3. 수정 전 계획 보고

코드·씬에 손대기 전에 반드시 보고한다:

- 수정 예정 파일 목록 (경로 단위)
- 접근 방법 (기존 시스템 재사용 여부 포함)
- 건드리지 않을 것 (보존 선언)
- 검증 방법

## 4. 사전 점검 (매 작업 시작 시)

1. 현재 경로와 Git 루트가 `C:\Users\sdjsd\Desktop\Unity\Project_PA`인지 확인. 아니면 즉시 중단.
2. `git status` 확인 — 남의 미커밋 변경을 덮어쓸 위험이 있으면 중단·보고.
3. `Assets/`, `Packages/`, `ProjectSettings/` 존재 확인.
4. Unity Editor가 이 프로젝트로 열려 있는지 확인 — 열려 있으면 batchmode 금지 (Editor 메뉴 실행 또는 보류 문서화).
5. Unity 실행/검증이 필요한 작업이면 루트 최신 `PROJECT_PA_CRASH_REPORT_*.md` 확인. D3D12 미승인 — 자동 실행은 D3D11 baseline에서만.

## 5. 수정 후 검증 보고

- `../04_VERIFICATION/VERIFICATION_RULES.md`의 해당 항목을 실행하고 **실제 결과**를 보고한다.
- 검증하지 못한 항목은 반드시 "확인 못 함 + 이유 + 사람이 확인할 방법"으로 보고한다.
- 추정 완료 보고 금지 ("아마 될 것" 금지 — `AI_SLOP_PREVENTION.md` §7).

## 6. 실패 시 절차

1. 같은 원인으로 2회 실패하면 **세 번째 시도를 하지 않는다.**
2. `../05_LOGS/BUG_LOG.md`에 증상·원인 추정·시도한 것·중단 이유를 기록한다.
3. 작업물을 지우지 말고 그대로 둔 채 멈춘다 (파일 삭제·Git 되돌리기 금지).
4. 사용자에게 필요한 판단을 명시해 보고한다.

## 7. Git 규칙

- `git push` 금지.
- 사용자가 명시적으로 요청할 때만 커밋한다.
- `git reset --hard`, `git clean -fd`, `rm -rf` 등 파괴적 명령 금지.
- 사용자 변경분을 되돌리지 않는다.
- 작업 전후로 `git status`를 확인해 의도한 파일만 변경됐는지 검증한다.

## 8. 문서화 의무

의미 있는 작업 후 갱신: `../05_LOGS/CHANGELOG_AI.md`, `PROJECT_PA_STATUS.md`, `PROJECT_PA_TODO.md`, `PROJECT_PA_SESSION_REPORT.md`, `Docs/07_개발일지.md`. 루프 작업이면 `Automation/LoopEngineering/progress.md` + `State/loop-state.json` 추가. **커밋 메시지만 남기고 끝내는 것 금지.**

## 9. 사람 승인이 필요한 변경 (착수 전 반드시 질문)

- 저장 스키마의 비-추가적(non-additive) 변경
- 새 게임플레이 시스템 도입
- 씬 구조·메인 씬 변경
- 밸런스 수치 대폭 변경
- 외부 패키지 도입
- 핵심 시스템(`Shop`/`EconomyService`/`PurchaseEvaluator`/`NpcController`/`SaveManager`) 리팩터링
