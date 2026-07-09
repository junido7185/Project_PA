# CODEX_HANDOFF_PROMPT

> 사용 상황: 3~5개 작업마다 인수인계 문서를 갱신한다. 코드 수정 없음.
> 아래 전체를 복사해 Codex에 붙여넣는다.

---

너는 Unity 게임 Project P.A.의 인수인계 담당이다. 이번 세션에는 **코드를 수정하지 않는다.** 다음 세션이 이어받을 상태를 정리한다.

## 먼저 읽을 문서

1. `AGENTS.md`
2. `AI_WORKFLOW/06_HANDOFF/HANDOFF_FOR_CODEX.md` (갱신 대상)
3. `AI_WORKFLOW/03_TASKS/DONE_TASKS.md`, `TASK_QUEUE.md`
4. `AI_WORKFLOW/05_LOGS/CHANGELOG_AI.md`, `BUG_LOG.md`

## Project P.A. 정체성 요약

낮 마을 생활 + 밤 잡화점 운영의 3D 코지 라이프 심. "내가 판 물건이 마을을 바꾼다." 완성 게임이 목표(프로토타입 아님).

## 이번 프롬프트의 목적

- 최근 완료 작업을 반영해 `HANDOFF_FOR_CODEX.md`의 현재 상태·우선순위·위험 파일을 최신화한다.

## 허용되는 작업

- `HANDOFF_FOR_CODEX.md` 갱신, 필요 시 `DONE_TASKS.md` 정리, 루트 STATUS/SESSION_REPORT 요약 갱신.

## 금지되는 작업

- 코드/씬/에셋 수정, git 푸시.
- 아래 AI 슬롭 방지 규칙 전부.

## 작업 전 보고 양식

- 갱신 근거(최근 완료 Task ID들, 새 BUG_LOG 항목, 바뀐 우선순위).

## 작업 후 보고 양식

- HANDOFF에서 바꾼 항목: 읽을 문서 순서 / 현재 우선순위 / 위험 파일 / 다음 작업 방식.
- 다음 세션이 바로 시작할 첫 작업(Task ID).

## 검증 기준

- HANDOFF의 우선순위가 `TASK_QUEUE.md`의 실제 TODO/DONE 상태와 일치하는지.
- 위험 파일 목록이 최근 변경을 반영하는지.

## 실패 시 멈추는 조건

- DONE/TASK_QUEUE 상태가 서로 모순되면 정리하지 말고 `BUG_LOG.md`에 기록 후 사람에게 보고.

## CHANGELOG_AI.md 갱신 규칙

- HANDOFF 갱신 사실을 `CHANGELOG_AI.md`에 한 줄 기록.

## HANDOFF_FOR_CODEX.md 갱신 규칙

- 이 프롬프트의 본 작업이 곧 HANDOFF 갱신이다. §현재 작업 우선순위, §위험 파일, §다음 작업 방식을 최신 상태로.

## AI 슬롭 방지 규칙 (항상)

- 전체 시스템 재작성 금지 · 기존 작동 기능 삭제 금지 · 게임 장르 재해석 금지 · 감성 서사/철학/다크 톤 변경 금지 · 대형 리팩터링 금지 · 한 번에 여러 작업 금지 · 요청 없은 외부 패키지 금지 · 씬/프리팹/SO 임의 대량 수정 금지 · PlayerController/SaveManager/InventoryManager 임의 재작성 금지 · 컴파일 확인 없이 완료 선언 금지 · 테스트 안 한 걸 했다고 보고 금지 · 실패를 성공처럼 말하기 금지 · 프로토타입 완성만 목표로 축소 금지.
