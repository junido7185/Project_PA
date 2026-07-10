# CODEX_FIRST_RUN_PLAYBOOK — Codex 첫 실행 가이드

작성: 2026-07-10
읽는 사람: **사용자(당신).** Codex를 처음 돌리기 전에 이 문서를 먼저 읽는다.

## 1. 목적

Codex 첫 실행은 **개발이 아니라 안전 시운전**이다. 목표는 "Codex가 이 프로젝트의 규칙을 지키며 안전하게 작동하는지"를 위험 없는 작업(문서·조사)으로 확인하는 것이다. 첫날에 기능을 만들지 않는다. 루프가 신뢰할 만하다는 것을 먼저 확인한 뒤 기능 개발로 넘어간다.

## 2. 첫 실행 전 준비

- [ ] `git status`가 clean인지 확인. 변경사항이 있으면 먼저 커밋(또는 사용자가 정리).
- [ ] Unity Editor를 닫는다(첫날은 검증기를 안 돌리므로 필수는 아니지만, batchmode 충돌 예방).
- [ ] `Library/`, `Temp/`, `Logs/`, `obj/` 같은 캐시 폴더는 **절대 커밋하지 않는다**(.gitignore 확인).
- [ ] AI_WORKFLOW 문서가 최신인지 확인(이 문서가 보이면 최신).
- [ ] `DECISION_REQUIRED_FOR_USER.md`를 훑어본다(첫날 작업엔 영향 없지만 곧 필요).

## 3. 첫 실행 순서

1. **CODEX_BOOTSTRAP_PROMPT 실행** — 아래 §6 프롬프트를 복사해 Codex에 붙여넣는다.
2. **결과 확인** — Codex가 구조/위험 파일/데모 흐름을 보고하고 코드를 수정하지 않았는지 확인(§8 판정 기준).
3. **이상 없으면 커밋** — `docs(ai): add codex bootstrap report` 등(§9).
4. **CODEX_REPEAT_PROMPT로 Task 001 수행** — 아래 §7 프롬프트 사용.
5. **Task 001 결과 확인** — `PROJECT_CODE_INDEX.md`가 생겼고 코드 변경이 없는지.
6. **Task 002 또는 003 진행** — 같은 방식으로 한 번에 하나씩.
7. **3개 작업 후 HANDOFF 갱신** — `CODEX_HANDOFF_PROMPT` 사용.

## 4. 첫날 권장 작업

- Task 001 (프로젝트 구조 인덱스)
- Task 002 (데모 플로우 문서화)
- Task 003 (컴파일 기준선 기록)
- **최대 Task 004 직전까지만.** (Task 004는 검증기 실행 = 사람 승인·Unity 필요)
- **기능 구현(Task 008 이후)은 첫날 금지.**

## 5. 첫 실행에서 절대 하지 말 것

- 기능 대량 구현.
- 씬/프리팹 수정.
- 저장 시스템 수정.
- 외부 패키지 추가.
- 여러 Task 동시 수행.
- 테스트 없이 "성공" 선언.

## 6. Codex에 붙여넣을 첫 프롬프트

```text
너는 Unity 게임 Project P.A.의 개발 작업자다. 이번 세션은 안전 시운전이며 코드를 절대 수정하지 않는다.

먼저 AI_WORKFLOW/00_START_HERE/CODEX_BOOTSTRAP_PROMPT.md 를 읽고 그 지시를 그대로 따른다.

이번 세션 규칙:
- 코드/씬/프리팹/ScriptableObject/메타/에셋 수정 금지.
- 파일 이동·삭제·git 커밋·푸시 금지.
- 구조 파악, 위험 파일 파악, Day 1 데모 흐름 파악만 한다.

사전 점검부터 보고하라:
1. 현재 경로와 git 루트가 C:\Users\sdjsd\Desktop\Unity\Project_PA 인지
2. git status 결과
3. Unity Editor 열림 여부

그 다음 아래를 보고하라:
1. 시스템별 코드 구조 요약(플레이어/인벤토리/상점/경제/NPC/낮밤/마을변화/저장)
2. 위험 파일 목록과 이유
3. Day 1 데모 흐름 단계
4. 확인 못 한 점
5. 추천하는 다음 작업(Task ID)

코드를 한 줄도 바꾸지 마라. 마지막에 "코드 수정 없음"을 확인해서 보고하라.
```

## 7. Bootstrap 성공 후 두 번째 프롬프트

```text
너는 Unity 게임 Project P.A.의 개발 작업자다. 한 번에 한 작업만 한다.

먼저 AI_WORKFLOW/00_START_HERE/CODEX_REPEAT_PROMPT.md 를 읽고 그 지시를 따른다.

이번 세션:
- AI_WORKFLOW/03_TASKS/TASK_QUEUE.md 에서 상태 TODO이고 선행이 모두 DONE인 첫 작업(=Task 001)을 고른다.
- 그 작업을 ACTIVE_TASK.md 양식으로 수행한다.
- 작업 전 보고(수정 예정 파일/보존 선언/검증 방법)를 먼저 하고 진행한다.
- 사용자 승인 없이 두 개 이상의 작업을 하지 않는다.
- 완료 후 검증 결과와 확인 못 한 점을 보고하고, CHANGELOG_AI.md / DONE_TASKS.md / TASK_QUEUE 상태를 갱신한다.

Task 001은 문서만 만드는 작업이다. 코드를 수정하지 마라.
```

## 8. 결과 판정 기준

| 상황 | 판정 |
|---|---|
| 코드 수정 없이 구조/위험/데모를 정확히 보고 | **계속 진행** → Task 001로 |
| Task가 문서만 만들고 코드 변경 없음, 검증 보고 정직 | **계속 진행** → 다음 Task |
| 코드를 건드렸거나, 여러 작업을 동시에 했거나, 검증을 추정으로 보고 | **멈춤** → 프롬프트를 다시 붙여넣고 규칙 강조. 반복되면 사람 검수 |
| 컴파일 에러/검증 실패가 났다 | `CODEX_BUGFIX_PROMPT`로 전환(버그 1개만). 2회 실패 시 사람 판단 |
| 방향(정체성/범위) 판단이 필요 | **사람/Fable 검수** — Codex는 결정자 아님 |

## 9. 첫 Codex 실행 후 커밋 메시지 예시

- `docs(ai): add codex bootstrap report`
- `docs(ai): index Project PA structure` (Task 001)
- `docs(ai): document current demo flow` (Task 002)
- `docs(ai): record compile baseline` (Task 003)

## 10. 사용자가 ChatGPT/Fable에게 다시 가져와야 할 정보

다음 검수·상담을 위해 아래를 복사해 온다:

- Codex 완료 보고 전문.
- 수정 파일 목록.
- 검증 결과(무엇을 실제로 실행했는지).
- 확인하지 못한 점.
- `git status` 결과.

이 5가지가 있으면 Fable이 다음 단계(승인/수정/진행)를 판단할 수 있다.
