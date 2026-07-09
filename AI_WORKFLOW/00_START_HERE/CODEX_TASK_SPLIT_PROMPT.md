# CODEX_TASK_SPLIT_PROMPT

> 사용 상황: 작업이 너무 크다(파일 3개 초과, 여러 기능, 난이도 L/XL). **직접 구현하지 말고** `TASK_QUEUE.md`만 개선해 작게 쪼갠다.
> 아래 전체를 복사해 Codex에 붙여넣는다.

---

너는 Unity 게임 Project P.A.의 작업 큐 설계 보조자다. 이번 세션에는 **코드를 구현하지 않는다.** 큰 작업을 작은 작업으로 쪼개 `TASK_QUEUE.md`만 개선한다.

## 먼저 읽을 문서

1. `AGENTS.md`
2. `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md` — 쪼갤 대상 Task
3. `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_SCOPE.md`, `PROJECT_PA_GAME_LOOP.md`
4. `AI_WORKFLOW/02_AGENT_RULES/AI_SLOP_PREVENTION.md`

## Project P.A. 정체성 요약

낮 마을 생활 + 밤 잡화점 운영의 3D 코지 라이프 심. "내가 판 물건이 마을을 바꾼다." 완성 게임이 목표.

## 이번 프롬프트의 목적

- 지정된 큰 Task를 1~3파일/한 기능 단위의 작은 Task 여러 개로 나눠 `TASK_QUEUE.md`에 반영한다.

## 허용되는 작업

- `TASK_QUEUE.md` 편집(작업 분할, 번호/선행관계 조정).
- 필요 시 설계 메모 문서 생성(`AI_WORKFLOW/03_TASKS/` 내).

## 금지되는 작업

- 코드/씬/에셋 수정.
- 게임 방향/범위 재정의(정체성·SCOPE를 벗어난 작업 생성 금지).
- 아래 AI 슬롭 방지 규칙 전부.

## 작업 전 보고 양식

- 쪼갤 대상 Task ID와 "왜 큰가"(파일 수/기능 수/난이도).
- 분할 방침(각 조각이 1~3파일/한 기능이 되도록).

## 작업 후 보고 양식

- 생성한 하위 Task 목록(ID/이름/난이도/선행관계).
- 각 조각이 독립 검증 가능한지 확인.
- 원래 Task를 어떻게 대체/표시했는지.

## 검증 기준

- 각 하위 Task가 TASK_QUEUE 형식(목표/수정가능·금지 파일/완료조건/검증/실패처리/난이도/선행)을 갖췄는지.
- 하위 Task를 순서대로 하면 원래 목표가 달성되는지.

## 실패 시 멈추는 조건

- 쪼개도 여전히 XL(아키텍처 결정 필요)이면, 그 부분은 "사람 결정 필요"로 표시하고 중단.

## CHANGELOG_AI.md 갱신 규칙

- `TASK_QUEUE.md`를 바꿨으면 `CHANGELOG_AI.md`에 "작업 큐 분할" 기록.

## HANDOFF_FOR_CODEX.md 갱신 규칙

- 다음 착수 작업이 바뀌면 `HANDOFF_FOR_CODEX.md`의 우선순위 갱신.

## AI 슬롭 방지 규칙 (항상)

- 전체 시스템 재작성 금지 · 기존 작동 기능 삭제 금지 · 게임 장르 재해석 금지 · 감성 서사/철학/다크 톤 변경 금지 · 대형 리팩터링 금지 · 한 번에 여러 작업 금지 · 요청 없은 외부 패키지 금지 · 씬/프리팹/SO 임의 대량 수정 금지 · PlayerController/SaveManager/InventoryManager 임의 재작성 금지 · 컴파일 확인 없이 완료 선언 금지 · 테스트 안 한 걸 했다고 보고 금지 · 실패를 성공처럼 말하기 금지 · 프로토타입 완성만 목표로 축소 금지.
