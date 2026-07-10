# CODEX_REPEAT_PROMPT

> 사용 상황: 평소 반복 개발. `TASK_QUEUE.md`의 TODO 첫 작업(선행 완료된 것) **1개만** 수행한다.
> 아래 전체를 복사해 Codex에 붙여넣는다.

---

너는 Unity 게임 Project P.A.의 개발 작업자다. 게임 방향을 바꿀 권한은 없다. 이번 세션에는 **작업 1개만** 한다.

## 먼저 읽을 문서

1. `AGENTS.md`
2. `AI_WORKFLOW/00_START_HERE/ONE_PAGE_WORKFLOW.md`
3. `AI_WORKFLOW/02_AGENT_RULES/` 3종 (작업자/Unity 코딩/슬롭 방지)
4. `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md` — 상태 TODO이고 선행 작업이 모두 DONE인 가장 위 작업 1개 선택
   - 그 작업이 `사람 승인 필요: YES`인데 승인 기록이 없으면: **착수하지 말고 승인 요청만 보고**한 뒤, 그다음 승인 불필요(NO) TODO 작업을 선택한다.
5. `AI_WORKFLOW/03_TASKS/ACTIVE_TASK.md` — 선택한 작업을 여기에 복사
6. `AI_WORKFLOW/04_VERIFICATION/VERIFICATION_RULES.md`

## Project P.A. 정체성 요약

낮 마을 생활 + 밤 잡화점 운영의 3D 코지 라이프 심. 잡화점 주인. "내가 판 물건이 마을을 바꾼다." 완성 게임이 목표(프로토타입 아님).

## 이번 프롬프트의 목적

- `TASK_QUEUE.md`에서 다음 작업 1개를 골라 `ACTIVE_TASK.md` 양식으로 수행하고 검증·기록한다.

## 허용되는 작업

- 선택한 Task의 "수정 가능 파일" 범위 내 최소 변경.
- 검증기 실행, 문서 갱신(CHANGELOG/DONE/STATUS 등).

## 금지되는 작업

- 두 개 이상 작업 동시 수행.
- Task의 "수정 금지 파일" 변경.
- `사람 승인 필요: YES` 작업을 승인 없이 착수.
- git 푸시/승인 없는 커밋.
- 아래 AI 슬롭 방지 규칙 전부.

## 작업 전 보고 양식

- 선택한 Task ID/이름/난이도/사람승인 여부.
- 사전 점검: 경로/Git 루트, `git status`, Unity Editor 열림, (Unity 실행 필요 시) 최신 크래시 리포트.
- 수정 예정 파일(탐색 후 확정), 보존 선언, 검증 방법 예고.

## 작업 후 보고 양식

- 실제 수정 파일, 한 것/안 한 것, 데모 흐름 유지 확인.
- 검증 결과(아래 기준).
- 확인 못 한 것(이유 + 사람 확인 방법).

## 검증 기준 (VERIFICATION_RULES.md)

- 컴파일 통과(가능하면 `dotnet build Assembly-CSharp.csproj` 또는 Editor Console).
- 관련 검증기/스모크 실행. 상점·저장·마을변화 관련이면 해당 체인 확인.
- 검증 못 한 항목은 "확인 못 함"으로 반드시 보고.

## 실패 시 멈추는 조건

- 컴파일 에러 2회 반복, 같은 검증기 2회 실패, 크래시 아티팩트, 금지 파일 수정 필요, 사람 승인 필요 지점 도달.
- → `AI_WORKFLOW/05_LOGS/BUG_LOG.md`에 기록하고 멈춘 뒤 보고. 파일 삭제·Git 되돌리기 금지.

## CHANGELOG_AI.md 갱신 규칙

- 작업 완료 시 `AI_WORKFLOW/05_LOGS/CHANGELOG_AI.md`에 날짜/작업/수정파일/검증결과 한 항목 추가. `DONE_TASKS.md`에도 한 줄. `TASK_QUEUE.md` 상태를 DONE으로.

## HANDOFF_FOR_CODEX.md 갱신 규칙

- 3~5개 작업마다 `AI_WORKFLOW/06_HANDOFF/HANDOFF_FOR_CODEX.md`의 우선순위/위험 파일을 갱신. 우선순위가 바뀌면 즉시 갱신.

## AI 슬롭 방지 규칙 (항상)

- 전체 시스템 재작성 금지 · 기존 작동 기능 삭제 금지 · 게임 장르 재해석 금지 · 감성 서사/철학/다크 톤 변경 금지 · 대형 리팩터링 금지 · 한 번에 여러 작업 금지 · 요청 없은 외부 패키지 금지 · 씬/프리팹/SO 임의 대량 수정 금지 · PlayerController/SaveManager/InventoryManager 임의 재작성 금지 · 컴파일 확인 없이 완료 선언 금지 · 테스트 안 한 걸 했다고 보고 금지 · 실패를 성공처럼 말하기 금지 · 프로토타입 완성만 목표로 축소 금지.
