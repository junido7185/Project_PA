# ACTIVE_TASK — 현재 진행 중인 단일 작업

사용법: `TASK_QUEUE.md`에서 작업 하나를 골라 아래 양식에 **복사**해 채운다. 동시에 두 작업 금지.
작업이 끝나면 이 파일을 비우고, `TASK_QUEUE.md` 상태를 `DONE`으로, `DONE_TASKS.md`에 한 줄 기록, 3~5개마다 `HANDOFF_FOR_CODEX.md` 갱신.

먼저 읽을 문서: `../00_START_HERE/ONE_PAGE_WORKFLOW.md`, `../02_AGENT_RULES/`(3종), `../04_VERIFICATION/VERIFICATION_RULES.md`.

---

## 현재 작업

- **Task ID**: Task 007
- **작업명**: 저장 스키마 현황 조사 문서
- **Phase / 난이도**: Phase 0 / S
- **사람 승인 필요**: NO

## 목표

- Save v8의 실제 필드·DTO·마이그레이션·미저장 상태를 `SAVE_SCHEMA.md`로 확정한다.

## 수정 예정 파일 (작업 전 확정)

- `AI_WORKFLOW/03_TASKS/SAVE_SCHEMA.md` 및 Task 기록 문서

## 수정 금지 파일 (위험 파일)

- `SaveData.cs`, `SaveManager.cs`, 저장소 코드, 씬/프리팹은 읽기만 하고 수정하지 않는다.

---

## 작업 전 보고 (구현 시작 전 작성)

- 사전 점검: 경로/Git루트 확인 [x], `git status` 확인 [x], Unity Editor 열림 여부 [x], Unity 실행 해당 없음
- 접근 방법 (기존 시스템 재사용 여부): 실제 Save v8 코드만 근거로 문서화
- 건드리지 않을 것 (보존 선언): 저장 코드·씬·프리팹·SO
- 검증 방법 예고: 문서 필드명과 코드 식별자 대조, Git 상태 확인

## 작업 후 보고 (구현 완료 후 작성)

- 실제 수정한 파일: `SAVE_SCHEMA.md` 및 기록 문서
- 한 것 / 안 한 것: 스키마 조사만 완료, 코드 수정 없음
- 기존 데모 흐름 유지 확인: 코드 무변경

## 검증 결과 (VERIFICATION_RULES.md 양식)

- 컴파일: 해당 없음 (문서 전용, 코드 무변경)
- 검증기: 해당 없음
- 상점 루프: [해당 항목/해당 없음]
- 저장/로드: [결과/해당 없음]
- 마을 변화: [결과/해당 없음]
- 확인 못 한 것: 실제 저장소 왕복 — Task 011에서 검증

## 실패 시

- `../05_LOGS/BUG_LOG.md`에 증상/원인추정/시도/중단이유 기록. 같은 원인 2회 실패 후 세 번째 시도 금지. 파일 삭제·Git 되돌리기 금지.

## 완료 처리 체크

- [x] `TASK_QUEUE.md`에서 이 Task 상태를 `DONE`으로
- [x] `DONE_TASKS.md`에 한 줄 기록
- [x] `../05_LOGS/CHANGELOG_AI.md` append
- [ ] 루트 기록 갱신(STATUS/TODO/SESSION_REPORT/개발일지) — 의미 있는 변경일 때
- [ ] 3~5개 작업마다 `../06_HANDOFF/HANDOFF_FOR_CODEX.md` 갱신
- [ ] 이 파일(ACTIVE_TASK) 비우기
