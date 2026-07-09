# 03_TASKS — 태스크 큐 (준비 중)

이 폴더에는 `TASK_QUEUE.md`(우선순위 큐)와 개별 태스크 파일이 들어갈 예정이다.
개별 태스크 양식은 기존 `Automation/LoopEngineering/ticket-template.md`를 재사용한다 (Goal / Allowed Paths / Max Changed Files / Stop Conditions 포함).

## TASK_QUEUE.md 를 만들기 위해 필요한 정보 (사람 입력 대기)

1. **체크포인트 커밋 여부** — VC-001A 작업분 커밋 완료 확인 (모든 이동·루프 작업의 선행 조건).
2. **졸업 발표 일정** — Stage 2 Demo Lock 날짜가 있어야 Stage 0/1 태스크의 마감 우선순위를 정할 수 있다.
3. **Vertical Slice의 두 번째 낮 활동 선택** — 낚시/광질/농사 중 무엇을 먼저 실장할지 (IL-002 결정).
4. **보류 검증기 실행 방식** — Editor를 닫고 batchmode(D3D11)로 돌릴지, 열린 Editor 메뉴에서 수동 실행할지.
5. **주간 작업 가능 시간** — 태스크 크기(1~3시간 단위) 조정 기준.

## 임시 우선순위 (TASK_QUEUE 생성 전까지)

`../06_HANDOFF/HANDOFF_FOR_CODEX.md` §3의 우선순위를 따른다.
