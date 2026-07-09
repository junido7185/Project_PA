# PROMPT_LIBRARY — 상황별 Codex 프롬프트 목차

작성: 2026-07-09
용도: 사용자가 Codex를 돌릴 때 "지금 어떤 프롬프트를 붙여넣어야 하는가"를 고르는 목차. 각 프롬프트 전문은 이 폴더(`00_START_HERE/`)의 `CODEX_*_PROMPT.md` 파일에 있다.

## 기본 운영 루프

1. **CODEX_BOOTSTRAP_PROMPT** — 프로젝트를 처음 맡았을 때(또는 오랜만에 복귀). 코드 수정 없이 구조/위험 파일/데모 흐름만 파악.
2. **CODEX_REPEAT_PROMPT** — 평소 반복 개발. `TASK_QUEUE.md`의 TODO 첫 작업 1개만 수행.
3. **CODEX_VERIFY_PROMPT** — 직전 작업 결과를 코드 수정 없이 검증.
4. **CODEX_BUGFIX_PROMPT** — 검증 실패 시 `BUG_LOG.md`의 버그 1개만 수정.
5. **CODEX_TASK_SPLIT_PROMPT** — 작업이 너무 크면(파일 3개 초과/여러 기능) 쪼개기. `TASK_QUEUE.md`만 개선.
6. **CODEX_HANDOFF_PROMPT** — 3~5개 작업마다 인수인계 갱신.
7. **CODEX_DEMO_LOCK_PROMPT** — 발표 직전. 새 기능 금지, 안정화·치명 버그만.
8. **CODEX_FINAL_POLISH_PROMPT** — 포트폴리오/시연 마무리. 코드 대수술 금지.
9. **CODEX_FULL_GAME_EXPANSION_PROMPT** — 졸업 데모 이후 Full Game Completion 확장. Vertical Slice 비파괴.
10. **CODEX_REFACTOR_GUARD_PROMPT** — 리팩터링 유혹이 생길 때 과도한 리팩터링을 막는 가드(동작 유지가 목표일 때만).

## 각 프롬프트 사용 상황 / 사용 금지 상황

| 프롬프트 | 사용 상황 | 사용 금지 상황 |
|---|---|---|
| BOOTSTRAP | 첫 세션, 컨텍스트 리셋 후 | 이미 구조를 알고 바로 개발할 때(→ REPEAT) |
| REPEAT | 일상 개발, TODO 소화 | 발표 직전(→ DEMO_LOCK), 큰 작업(→ TASK_SPLIT) |
| VERIFY | 작업 직후, 커밋 전 | 아직 아무것도 안 했을 때 |
| BUGFIX | BUG_LOG에 버그가 있을 때 | 새 기능이 필요할 때(→ REPEAT), 버그 원인 미상 2회 실패 후(→ 사람) |
| TASK_SPLIT | 작업이 3파일 초과/여러 기능 | 작업이 이미 충분히 작을 때 |
| HANDOFF | 3~5개 작업마다 | 매 작업마다(과도) |
| DEMO_LOCK | Demo Lock 이후~발표 | 아직 기능 개발 단계일 때 |
| FINAL_POLISH | 시연/제출 마무리 | 기능이 미완일 때 |
| FULL_GAME_EXPANSION | 졸업 데모 확정 후 Stage 3+ | 시연 전, Vertical Slice 미완일 때 |
| REFACTOR_GUARD | 리팩터링 충동/코드 정리 요청 | 신기능 구현 중(리팩터링과 섞지 말 것) |

## 사람 승인이 필요한 조건 (프롬프트와 무관하게 항상)

- 저장 스키마 비-추가 변경, 새 게임플레이 시스템, 씬 구조/메인 씬 변경, 밸런스 대폭 변경, 외부 패키지, 핵심 시스템 리팩터링.
- `TASK_QUEUE.md`에서 `사람 승인 필요: YES`(대개 XL) 작업.
- Unity 실행/빌드(D3D11 방식 확정 필요).

## Fable(고급 모델) 없이 Codex만 돌려도 되는 상황

- `TASK_QUEUE.md`의 XS/S 작업(문서 작성, 조사, 단일 스크립트 소규모 수정, 표시/로그 추가).
- 검증기 실행과 결과 기록.
- BUG_LOG의 원인이 명확한 버그 1개 수정.
- 즉, "정답이 좁고 검증이 자동화된" 작업.

## 사람 또는 Fable 검수가 필요한 상황

- M/L/XL 작업, 특히 여러 시스템 연결·저장 스키마·씬/프리팹 참조.
- 게임 재미/가독성/톤 판정(주관적).
- 새 시스템 설계의 방향 결정.
- 검증기가 2회 실패한 문제(원인 규명).
- Demo Lock 선언, 시연/슬라이스/출시 완료 판정.
- 밸런스 값 확정.

## 운영 속도 (요약, 상세는 `../07_FULL_GAME_ROADMAP/DEVELOPMENT_TIMELINE.md` §7)

- 하루 권장: 저강도 1~2개 / 집중 3~5개 / 크런치 5~8개(품질 위험 있음).
- 3~5개마다 HANDOFF 갱신, 5~10개마다 사람 검수.
- 실패 2회 이상이면 BUGFIX 루프 중단 후 사람 판단.
