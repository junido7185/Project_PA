# CODEX_FIRST_7_DAYS_PLAN — Codex 첫 7일 운영 계획

작성: 2026-07-10
목적: Codex를 처음 돌리는 7일 동안의 작업 순서 안내. "안전 시운전 → 기준선 → 안정화 → 첫 기능"으로 점진 가동한다.
전제: 첫 실행 절차는 `../00_START_HERE/CODEX_FIRST_RUN_PLAYBOOK.md`. "Day"는 달력 하루가 아니라 **작업 세션 단위**로 읽어도 된다(하루 작업 수는 `DEVELOPMENT_TIMELINE.md` §7).

> 공통: 각 Day 시작 시 `git status` 확인, 한 번에 한 작업, 검증 후 기록, 실패 2회 시 중단·사람 판단. 위험 작업(사람 승인 YES)은 사용자 승인 전 착수 금지.

## Day 1. Bootstrap & Baseline

- **목표**: Codex가 규칙을 지키며 작동하는지 확인하고 프로젝트 구조를 문서화.
- **사용 프롬프트**: `CODEX_BOOTSTRAP_PROMPT` → `CODEX_REPEAT_PROMPT`
- **Task 후보**: (Bootstrap 보고) → Task 001, 002, 003
- **성공 조건**: 코드 수정 0, 구조/데모/컴파일 기준선 문서 생성, 검증 보고 정직.
- **중단 조건**: 코드를 건드림 / 여러 작업 동시 / 검증을 추정으로 보고.

## Day 2. Safety & Verification

- **목표**: 안전 기준선 확정(위험 파일·git 정책·저장 스키마·검증기 실행 방식).
- **사용 프롬프트**: `CODEX_REPEAT_PROMPT` (검증기 실행은 사람 승인 후)
- **Task 후보**: Task 005(위험 파일), 006(git 정책), 007(저장 스키마), 012(D3D12 회피 기록) / Task 004는 **사람 승인 후에만**
- **성공 조건**: 위험 파일 목록·git 정책·저장 스키마 문서화. D3D11 실행 정책 기록.
- **중단 조건**: Editor 열린 채 batchmode 시도 / 검증기 실행 방식 미정인데 강행.

## Day 3. MVP Stabilization 1

- **목표**: 기존 데모 흐름을 깨지 않고 회귀 확인 시작.
- **사용 프롬프트**: `CODEX_REPEAT_PROMPT`
- **Task 후보**: Task 008(플레이어), 009(인벤토리), 010(상점 흐름) — **조사 위주**, 버그는 기록만
- **성공 조건**: 각 시스템 회귀 조사 결과 기록, 컴파일 통과 확인.
- **중단 조건**: 조사 단계인데 코드를 수정 / PlayerController·Inventory 재작성 시도.

## Day 4. MVP Stabilization 2

- **목표**: Day 3에서 나온 버그 1개 처리 + BUGFIX 루프 사용법 익히기.
- **사용 프롬프트**: `CODEX_BUGFIX_PROMPT`
- **Task 후보**: Task 013(콘솔 스냅샷), 014(스모크 체크리스트), 015(버그 1개 수정)
- **성공 조건**: 버그 1개 재현→수정→재현 사라짐 확인. 데모 흐름 유지.
- **중단 조건**: 같은 원인 2회 실패 후 3번째 시도 / 위험 파일 대규모 변경 필요.

## Day 5. Shop Core 시작

- **목표**: 상점 핵심의 작은 표시 개선(로직 변경 없음).
- **사용 프롬프트**: `CODEX_REPEAT_PROMPT`
- **Task 후보**: Task 017(카테고리 문서), 018(재고 수량 표시), 022(수익 로그) — 표시/로그만
- **성공 조건**: 표시 기능 동작 + `EconomyService`/`PurchaseEvaluator` 로직 불변.
- **중단 조건**: 구매/수익 계산식을 건드림.

## Day 6. Verification Day

- **목표**: 그동안 변경분을 검증하고 저장 위험 점검, 인수인계 갱신.
- **사용 프롬프트**: `CODEX_VERIFY_PROMPT` → `CODEX_HANDOFF_PROMPT`
- **Task 후보**: Task 011(저장 왕복 확인, 사람 Play 필요 가능), 026(Shop 검증기) / HANDOFF 갱신
- **성공 조건**: 검증 결과 문서화(못 한 항목은 "확인 못 함"), HANDOFF 우선순위 최신화.
- **중단 조건**: 검증 없이 통과 선언 / 저장 스키마를 승인 없이 변경.

## Day 7. Review & Next Sprint

- **목표**: 첫 주 정리, 다음 주 작업 선택, 위험 작업 승인 여부 결정.
- **사용 프롬프트**: `CODEX_HANDOFF_PROMPT` (+ 필요 시 `CODEX_TASK_SPLIT_PROMPT`)
- **Task 후보**: DONE_TASKS 정리, Task 016(MVP 안정화 판정, 사람) 준비, Phase 2 다음 작업 선정
- **성공 조건**: DONE_TASKS 최신, 다음 주 후보 확정, 사용자 결정 필요 항목 정리.
- **중단 조건**: 사람 판정(016) 없이 Phase 2를 임의로 전진.

## 첫 주 이후

- MVP Stabilization(Phase 0~1)이 사람 판정으로 완료되면 Phase 2(Shop Core) 본격 진입.
- 속도·검수 주기는 `DEVELOPMENT_TIMELINE.md` §5·§7.
- Demo Lock 날짜가 정해지면 §6 역산표로 범위 조정.
