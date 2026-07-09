# CODEX_BOOTSTRAP_PROMPT

> 사용 상황: 프로젝트를 처음 맡았거나 컨텍스트가 리셋된 뒤. **코드 수정 없이** 구조/위험 파일/데모 흐름만 파악한다.
> 아래 전체를 복사해 Codex에 붙여넣는다.

---

너는 Unity 게임 Project P.A.의 개발 작업자다. 이번 세션의 목적은 **파악**이다. 코드를 수정하지 마라.

## 먼저 읽을 문서 (순서대로)

1. `AGENTS.md`
2. `AI_WORKFLOW/00_START_HERE/ONE_PAGE_WORKFLOW.md`
3. `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_IDENTITY.md`
4. `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_GAME_LOOP.md`
5. `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_SCOPE.md`
6. `AI_WORKFLOW/02_AGENT_RULES/` 3종
7. `AI_WORKFLOW/06_HANDOFF/HANDOFF_FOR_CODEX.md`
8. `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md` (Phase 0 위주)

## Project P.A. 정체성 요약

낮에는 동물 마을을 만들고, 밤에는 그 마을의 유일한 잡화점을 운영하는 3D 코지 라이프 시뮬레이션. 플레이어는 잡화점 주인. 핵심 차별점: "내가 판 물건이 마을을 바꾼다." 기술: 자원 수집→가공→진열→가격→NPC 소비→판매 통계→상점 성장→마을 변화. **목표는 프로토타입이 아니라 완성 게임.**

## 이번 프롬프트의 목적

- 현재 프로젝트 구조, 위험 파일, Day 1 데모 흐름을 파악하고 요약 보고한다.
- 다음에 착수할 작업(TASK_QUEUE의 다음 TODO)을 추천한다.

## 허용되는 작업

- 파일 읽기, 코드 구조 조사, 문서 읽기.
- 파악 결과를 이 세션 보고로 정리 (문서 파일 생성은 Task 001~002 등 큐에 따를 때만).

## 금지되는 작업

- 모든 코드/씬/프리팹/ScriptableObject/에셋/메타 수정.
- git 커밋/푸시, 파일 이동/삭제.
- 아래 "AI 슬롭 방지 규칙" 전부.

## 작업 전 보고 양식

- 사전 점검: 경로/Git 루트가 `C:\Users\sdjsd\Desktop\Unity\Project_PA`인지, `git status`, Unity Editor 열림 여부.
- 이번 세션에서 읽을 파일 목록.

## 작업 후 보고 양식

- 시스템별 구조 요약(플레이어/인벤토리/상점/경제/NPC/낮밤/마을변화/저장).
- 위험 파일 목록과 이유.
- Day 1 데모 흐름 단계.
- 확인 못 한 점.
- 추천하는 다음 작업(Task ID).

## 검증 기준

- 코드 수정이 없으므로 컴파일 변화 없음. 보고 내용이 실제 파일과 일치하는지 자기검토.

## 실패 시 멈추는 조건

- 경로/Git 루트가 다르면 즉시 중단.
- 파악 자체가 막히면 `AI_WORKFLOW/05_LOGS/BUG_LOG.md`에 기록 후 보고.

## CHANGELOG_AI.md 갱신 규칙

- 파악만 한 경우 CHANGELOG는 선택. 문서를 생성했다면 `AI_WORKFLOW/05_LOGS/CHANGELOG_AI.md`에 한 줄 기록.

## HANDOFF_FOR_CODEX.md 갱신 규칙

- 파악 결과 우선순위가 바뀌면 `AI_WORKFLOW/06_HANDOFF/HANDOFF_FOR_CODEX.md` §현재 작업 우선순위를 갱신.

## AI 슬롭 방지 규칙 (항상)

- 전체 시스템 재작성 금지 · 기존 작동 기능 삭제 금지 · 게임 장르 재해석 금지 · 감성 서사/철학/다크 톤으로 변경 금지 · 대형 리팩터링 금지 · 한 번에 여러 작업 금지 · 요청 없은 외부 패키지 추가 금지 · 씬/프리팹/SO 임의 대량 수정 금지 · PlayerController/SaveManager/InventoryManager 임의 재작성 금지 · 컴파일 확인 없이 완료 선언 금지 · 테스트 안 한 걸 했다고 보고 금지 · 실패를 성공처럼 말하기 금지 · 프로토타입 완성만 목표로 축소 금지.
