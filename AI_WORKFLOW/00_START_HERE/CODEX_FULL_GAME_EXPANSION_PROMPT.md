# CODEX_FULL_GAME_EXPANSION_PROMPT

> 사용 상황: **졸업 데모 확정(Demo Lock 종료) 이후** Full Game Completion 확장(Phase 9). 기존 Vertical Slice를 깨지 않고 확장 기능만 추가.
> 아래 전체를 복사해 Codex에 붙여넣는다.

---

너는 Unity 게임 Project P.A.의 확장 개발 작업자다. 이번 단계는 완성 게임을 향한 확장이다. 단, **이미 도는 Vertical Slice를 절대 깨지 마라.** 여전히 한 번에 한 작업만 한다.

## 먼저 읽을 문서

1. `AGENTS.md`
2. `AI_WORKFLOW/07_FULL_GAME_ROADMAP/FULL_GAME_COMPLETION_ROADMAP.md` (Stage 3~5), `DEVELOPMENT_TIMELINE.md`
3. `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_SCOPE.md` (§5 확장, §6 과욕 금지, §8 핵심 재미)
4. `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md` (Phase 9)
5. `AI_WORKFLOW/02_AGENT_RULES/` 3종

## Project P.A. 정체성 요약

낮 마을 생활 + 밤 잡화점 운영의 3D 코지 라이프 심. "내가 판 물건이 마을을 바꾼다." 완성 게임이 목표. 확장은 정체성/핵심 재미를 강화하는 방향만.

## 이번 프롬프트의 목적

- Phase 9의 확장 작업 1개를, 기존 Vertical Slice(1일 루프)를 회귀 검증으로 지키면서 추가한다.

## 허용되는 작업

- Phase 9 Task 1개(대개 사람 승인 필요). 기존 시스템 확장(SeasonModifier/TierService/VC 사이드카 등) 재사용.
- 설계 문서 작성.

## 금지되는 작업

- Vertical Slice(낮→재고→밤→통계→마을변화)를 깨는 변경.
- SCOPE §6의 과욕 기능(멀티플레이/오픈월드/새 장르/풀보이스 등).
- 기존 시스템 전면 재작성, 외부 패키지.
- 사람 승인 없이 XL 착수.
- 아래 AI 슬롭 방지 규칙 전부.

## 작업 전 보고 양식

- 대상 Phase 9 Task / 사람 승인 여부 확인.
- 어떤 기존 시스템을 재사용하는가 / Vertical Slice 비파괴 보장 방법.
- 수정 예정 파일, 보존 선언.

## 작업 후 보고 양식

- 수정 파일, 추가한 확장 기능.
- **Vertical Slice 회귀 검증 결과**(1일 루프 여전히 성립).
- 저장 스키마 변경 시 마이그레이션/왕복 확인.
- 확인 못 한 것.

## 검증 기준

- 컴파일 통과 + 확장 기능 동작 + **1일 루프 회귀 통과**(`PA_DayNightShopLoopValidator`, `PA_LongPlayProgressionValidator` 등).
- 저장 관련이면 세이브/로드 왕복 + 구버전 호환.
- 마을 변화 관련이면 판매→변화 증거 확인.

## 실패 시 멈추는 조건

- 확장이 Vertical Slice를 깨면 즉시 롤백 판단하고 중단(파일 삭제 금지, 그대로 두고 보고).
- 저장 마이그레이션 실패, 2회 검증 실패 시 `BUG_LOG.md` 기록 후 사람 판단.

## CHANGELOG_AI.md 갱신 규칙

- 확장 완료 시 `CHANGELOG_AI.md`에 기록, `DONE_TASKS.md`·`TASK_QUEUE.md` 상태 갱신.

## HANDOFF_FOR_CODEX.md 갱신 규칙

- Stage 진행 상황과 다음 확장 우선순위를 `HANDOFF_FOR_CODEX.md`에 갱신(3~5개마다).

## AI 슬롭 방지 규칙 (항상)

- 전체 시스템 재작성 금지 · 기존 작동 기능 삭제 금지 · 게임 장르 재해석 금지 · 감성 서사/철학/다크 톤 변경 금지 · 대형 리팩터링 금지 · 한 번에 여러 작업 금지 · 요청 없은 외부 패키지 금지 · 씬/프리팹/SO 임의 대량 수정 금지 · PlayerController/SaveManager/InventoryManager 임의 재작성 금지 · 컴파일 확인 없이 완료 선언 금지 · 테스트 안 한 걸 했다고 보고 금지 · 실패를 성공처럼 말하기 금지 · 프로토타입 완성만 목표로 축소 금지.
