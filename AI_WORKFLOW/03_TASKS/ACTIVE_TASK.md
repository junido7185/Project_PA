# ACTIVE_TASK — 현재 진행 중인 단일 작업

사용법: `TASK_QUEUE.md`에서 작업 하나를 골라 아래 양식에 **복사**해 채운다. 동시에 두 작업 금지.
작업이 끝나면 이 파일을 비우고, `TASK_QUEUE.md` 상태를 `DONE`으로, `DONE_TASKS.md`에 한 줄 기록, 3~5개마다 `HANDOFF_FOR_CODEX.md` 갱신.

먼저 읽을 문서: `../00_START_HERE/ONE_PAGE_WORKFLOW.md`, `../02_AGENT_RULES/`(3종), `../04_VERIFICATION/VERIFICATION_RULES.md`.

---

## 현재 작업

- **Task ID**: (없음 — 큐에서 선택)
- **작업명**:
- **Phase / 난이도**:
- **사람 승인 필요**: YES/NO — (YES면 승인 확인:  )

## 목표

- (체크 가능한 목표를 TASK_QUEUE에서 복사)

## 수정 예정 파일 (작업 전 확정)

- (경로. 모르면 "탐색 후 확정" — 탐색 결과를 여기 갱신)

## 수정 금지 파일 (위험 파일)

- (해당 위험 파일. 예: `PlayerController.cs`, `SaveManager.cs`, `Inventory.cs`, 씬/프리팹)

---

## 작업 전 보고 (구현 시작 전 작성)

- 사전 점검: 경로/Git루트 확인 [ ], `git status` 확인 [ ], Unity Editor 열림 여부 [ ], (Unity 실행 필요 시) 최신 크래시 리포트 확인 [ ]
- 접근 방법 (기존 시스템 재사용 여부):
- 건드리지 않을 것 (보존 선언):
- 검증 방법 예고:

## 작업 후 보고 (구현 완료 후 작성)

- 실제 수정한 파일:
- 한 것 / 안 한 것:
- 기존 데모 흐름 유지 확인:

## 검증 결과 (VERIFICATION_RULES.md 양식)

- 컴파일: [통과/실패/보류] (증거:  )
- 검증기: [이름 + 결과] (증거:  )
- 상점 루프: [해당 항목/해당 없음]
- 저장/로드: [결과/해당 없음]
- 마을 변화: [결과/해당 없음]
- 확인 못 한 것: [항목 + 이유 + 사람이 확인하는 방법]

## 실패 시

- `../05_LOGS/BUG_LOG.md`에 증상/원인추정/시도/중단이유 기록. 같은 원인 2회 실패 후 세 번째 시도 금지. 파일 삭제·Git 되돌리기 금지.

## 완료 처리 체크

- [ ] `TASK_QUEUE.md`에서 이 Task 상태를 `DONE`으로
- [ ] `DONE_TASKS.md`에 한 줄 기록
- [ ] `../05_LOGS/CHANGELOG_AI.md` append
- [ ] 루트 기록 갱신(STATUS/TODO/SESSION_REPORT/개발일지) — 의미 있는 변경일 때
- [ ] 3~5개 작업마다 `../06_HANDOFF/HANDOFF_FOR_CODEX.md` 갱신
- [ ] 이 파일(ACTIVE_TASK) 비우기
