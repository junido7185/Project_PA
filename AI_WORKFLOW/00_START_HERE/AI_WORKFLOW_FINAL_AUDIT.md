# AI_WORKFLOW_FINAL_AUDIT

검수일: 2026-07-10
검수자: Claude (Fable 5) — Codex 투입 전 최종 프리플라이트
대상: `AI_WORKFLOW/` 전체 (00~07, 99) + 루트 `AGENTS.md`, `CLAUDE.md`, `README.md`, `Docs/AgentWorkflow/CONTEXT_INDEX.md`
방법: 32개 문서를 순서대로 재검토, 10개 일관성 항목 점검, 문서 간 상호참조 대조.

## 1. 최종 검수 요약

AI_WORKFLOW 문서 세트는 **Codex 투입 가능 상태**다. 정체성 3문서(IDENTITY/GAME_LOOP/SCOPE)는 서로 충돌하지 않고, TASK_QUEUE는 안정화→핵심 루프→저장→데모→확장 순서를 지키며, 검증 규칙은 증거 기반이다. 이번 검수에서 발견한 결함은 전부 경미했고 그 자리에서 보완했다(표기 오타, 선행작업 오타, 승인-스킵 규칙 누락, 첫 실행 안내 부재). 완성 게임 목표가 졸업 데모에 묻히지 않도록 SCOPE와 ROADMAP이 4단계를 명확히 분리하고 있음을 확인했다.

핵심 결론: **첫 실행(Bootstrap + Task 001~003)을 지금 바로 돌려도 안전하다.** 단, 기능 구현(Task 008 이후)에 들어가기 전 사용자 결정 4건(`DECISION_REQUIRED_FOR_USER.md`)을 확정하는 것이 좋다.

## 2. 통과한 항목

| # | 점검 항목 | 결과 | 근거 |
|---|---|---|---|
| 1 | AGENTS.md가 Codex 진입점 역할 | 통과 | 정체성 1줄 + 완성게임 목표 + 읽을 문서 순서 6단계 + 절대 규칙 + 종료 시 갱신 문서 명시 |
| 2 | 매번 읽는 문서 순서 일관성 | 통과(보완 후) | AGENTS §매번 읽어야 할 문서 = DOCS_INDEX §2 = HANDOFF §1 = ONE_PAGE 순서가 일치. PROMPT_LIBRARY/FIRST_RUN 참조를 DOCS_INDEX·HANDOFF에 추가 |
| 3 | IDENTITY/GAME_LOOP/SCOPE 충돌 없음 | 통과 | 셋 다 "낮 생활+밤 상점+마을 변화", "완성 게임 목표", "낚시=임시 2번째 활동"으로 정합. SCOPE §8 핵심 재미 = GAME_LOOP §5 우선 규칙과 일치 |
| 4 | TASK_QUEUE 순서(안정화→루프→저장→데모→확장) | 통과 | Phase 0~1 안정화 → 2~5 핵심 루프 → 6 저장 → 7 데모 → 8 슬라이스 → 9 확장. 요구 순서와 일치 |
| 5 | VERIFICATION이 증거 기반 | 통과 | §1 "검증 없이 완료 없음", 실행 증거(로그/검증기 출력) 요구, "확인 못 함" 명시 강제, 추정 금지 |
| 6 | HANDOFF 구체성 | 통과 | 현재 우선순위가 Task 번호로 지정, 위험 파일 표, 동결 문서, 하루 작업 수, Demo Lock 규칙 포함 |
| 7 | PROMPT_LIBRARY 운영 루프 현실성 | 통과 | BOOTSTRAP→REPEAT→VERIFY→BUGFIX→TASK_SPLIT→HANDOFF→DEMO_LOCK→FINAL_POLISH→EXPANSION 흐름이 실제 사용 순서와 맞음 |
| 8 | TIMELINE이 난이도와 정합 | 통과(보정 후) | 85작업(XS15/S37/M26/L7) 분포로 시나리오 산정. 표기를 "약 N주"로 통일 |
| 9 | Full Game이 데모에 안 묻힘 | 통과 | SCOPE §1 Full Game Target을 §2 Demo보다 먼저 배치, ROADMAP Stage 0~5, TIMELINE에서 "큐 범위 vs 실제 출시급" 구분 |
| 10 | AI 슬롭 방지 규칙 반복 | 통과 | 프롬프트 10종 전부 말미에 13개 슬롭 방지 규칙 블록 포함(CODEX_PROMPT_AUDIT에서 개별 확인) |

## 3. 보완한 항목

이번 검수에서 발견 즉시 고친 것들:

1. **TIMELINE 기간 표기 오독 위험** → `~9~10주`→`약 9~10주` 등 전 구간을 "약 N주 / N~M주"로 통일. 난이도 분포표의 실행 횟수도 "약 N회"로 명시.
2. **TASK_QUEUE Task 041 선행작업 오타** ("Task 039, 041 선행인 010" → "Task 039, Task 010").
3. **REPEAT 프롬프트의 승인-스킵 규칙 부재** → 첫 TODO가 `사람 승인 필요: YES`면 승인 요청만 보고하고 다음 NO 작업을 고르도록 명시. TASK_QUEUE Review Notes에도 동일 규칙 기록.
4. **첫 실행 안내 부재** → PROMPT_LIBRARY 0단계와 DOCS_INDEX에 `CODEX_FIRST_RUN_PLAYBOOK` 참조 추가(플레이북 본체는 작업 E에서 생성).

## 4. 남은 위험

| 위험 | 성격 | 완화 |
|---|---|---|
| 보류 검증기 2종 BLOCKED | Codex가 "통과"로 오인 가능 | VERIFICATION §2-B에 BLOCKED 명시, Task 004에서 사람 승인 후 실행 |
| 사용자 결정 4건 미확정 | 기능 구현 단계에서 방향 모호 | `DECISION_REQUIRED_FOR_USER.md`로 분리, 임시 기본값 제공. 첫날 작업(001~003)은 영향 없음 |
| Codex 실전 미검증 | 프롬프트가 실제로 잘 작동하는지 미확인 | `CODEX_FIRST_RUN_PLAYBOOK`으로 안전 시운전부터. 첫날 기능 구현 금지 |
| 사람 검수 병목 | 과속 시 슬롭 누적 | 5~10작업마다 검수, 하루 작업 수 제한(TIMELINE §7) |
| Docs/01~08 동결 규약 의존 | 코드 § 인용 깨짐 위험 | DOCS_INDEX §5·DECISION_LOG에 동결 명문화, HANDOFF 위험 파일 표 |

## 5. Codex 투입 가능 여부

**가능.** 단계적 투입을 권장한다:

- **지금 즉시 가능**: `CODEX_FIRST_RUN_PLAYBOOK` → Bootstrap → Task 001~003 (문서/조사만, 위험 0).
- **사용자 결정 후 권장**: Task 004(검증기 실행 방식) 및 Phase 1 기능 안정화 → `DECISION_REQUIRED_FOR_USER.md`의 검증기 실행 방식·주간 작업 시간 확정 후.
- **아직 이르다**: Phase 8~9(Vertical Slice 통합, Full Game 확장) → Graduation Demo 안정화 이후.

## 6. 첫 실행 전 사용자 확인사항

1. Unity Editor를 닫았는가? (batchmode 검증 대비 — 첫날엔 검증기 안 돌리므로 필수는 아님)
2. `git status`가 clean인가? (현재 커밋 `53fd115` 기준 clean — 확인 완료)
3. `DECISION_REQUIRED_FOR_USER.md`의 4건 중 최소 "검증기 실행 방식"과 "하루 작업 수"는 첫 기능 작업 전 결정.
4. 첫날은 기능 구현을 하지 않는다는 점 인지 (`CODEX_FIRST_RUN_PLAYBOOK` §5).

## 7. 다음 커밋 권장 메시지

```
docs(ai): finalize codex preflight audit and first-run package

- AI_WORKFLOW_FINAL_AUDIT / CODEX_PROMPT_AUDIT / TASK_QUEUE_REVIEW
- CODEX_FIRST_RUN_PLAYBOOK + CODEX_FIRST_7_DAYS_PLAN
- DECISION_REQUIRED_FOR_USER
- fix timeline notation, task 041 prerequisite typo, add YES-skip rule
```
