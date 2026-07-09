# HANDOFF_FOR_CODEX — 다음 세션 인수인계

최종 갱신: 2026-07-09 (AI_WORKFLOW 구조 구축 세션)
규칙: 매 세션 종료 시 이 문서의 "현재 상태"와 "우선순위"를 갱신한다.

## 1. 읽을 문서 순서

1. 루트 `AGENTS.md` — 입구 (1분)
2. `../00_START_HERE/ONE_PAGE_WORKFLOW.md` — 작업 절차
3. `../01_IDENTITY/PROJECT_PA_IDENTITY.md` — 정체성 (재해석 금지)
4. `../01_IDENTITY/PROJECT_PA_SCOPE.md` — 현재 단계: **Stage 0 MVP Stabilization**
5. `../02_AGENT_RULES/` 3종 — 규칙
6. 이 문서 §3 우선순위
7. 작업 유형별 추가 문서 → `../00_START_HERE/DOCS_INDEX.md` §3

## 2. 현재 게임 정체성 (한 줄)

> "낮에는 동물 마을을 만들고, 밤에는 그 마을의 유일한 잡화점을 운영하는 3D 코지 라이프 시뮬레이션" — 핵심 차별점: **내가 판 물건이 마을을 바꾼다.**

**목표는 프로토타입이 아니라 완성 게임이다.** 졸업 시연(Stage 2)은 완성 게임의 부분집합일 뿐이다. 일회용 코드·하드코딩·저장 미지원 구현으로 때우지 않는다.

## 3. 현재 작업 우선순위 (2026-07-09 기준)

1. **[사람] 체크포인트 커밋** — VC-001A 작업분(신규 스크립트 4파일 + 문서 8파일)이 미커밋. 클린 baseline 없이는 문서 이동·자동 루프 진행 불가.
2. **[사람+Codex] 보류된 검증기 2종 실행** — `PA_FinalDemoRouteValidator`, `PA_LongPlayProgressionValidator`. Unity Editor가 닫힌 상태에서 batchmode(D3D11) 또는 열린 Editor 메뉴에서 수동 실행.
3. **[Codex] 문서 이동 실행** — 클린 baseline 확보 후 `../00_START_HERE/DOCS_INDEX.md` §7-B의 이동 대기 목록 처리 (병합→이동→참조 갱신 같은 커밋).
4. **[Codex] 저장 v8 왕복 확인** — F5/F9 + 구버전 세이브 호환.
5. **[사람] 1920x1080 가독성 수동 확인** — 개발 오버레이 숨김 상태의 Game view.
6. 이후: `../03_TASKS/`에 TASK_QUEUE 구축 → Stage 1 Vertical Slice 착수.

## 4. 위험 파일 (수정 전 반드시 규칙 확인)

| 파일/영역 | 위험 |
|---|---|
| `Assets/Scenes/Prototype_FirstDay.unity` | 메인 씬. 덮어쓰기·재작성 금지. Build Settings 시작 씬 |
| `SaveManager` 계열 | 저장 스키마 v8. 추가 확장만 허용 |
| `Shop`/`ShopSlot`/`EconomyService`/`PurchaseEvaluator`/`NpcController` | 경제 코어. 넓은 수정은 사람 승인 |
| `Docs/01~08` | 동결 문서. 코드 주석이 § 인용 |
| `PROJECT_PA_CRASH_REPORT_20260625.md` | 루트 고정 (preflight glob) |
| `Assets/Jinxish/**` | 서드파티. 수정 금지 |
| 미커밋 상태의 `VillageCultureVisualController.cs` + `PA_VillageCultureVisualValidator.cs` | VC-001A 작업분. 커밋 전 덮어쓰기 주의 |

## 5. 금지사항 (요약)

정체성 재해석 / 대형 리팩터링 / 기능 삭제 / 외부 패키지 / 멀티플레이 착수 / 씬·프리팹·저장 임의 변경 / Project_D 접근 / git push / 검증 없는 완료 선언 / 한 작업에 여러 기능. 전체: `../02_AGENT_RULES/AI_SLOP_PREVENTION.md`.

## 6. 다음 작업 방식

1. 사전 점검 (경로/Git/Unity 프로세스/크래시 리포트) → `CODEX_WORKER_RULES.md` §4
2. 태스크 하나 선택 → 수정 계획 보고 → 최소 변경 → 검증 → 기록.
3. 실패 2회 → `BUG_LOG.md` 기록 후 정지.
4. 세션 종료 시 이 문서 §3을 갱신하고, 다음 세션이 이어받을 수 있는 상태로 남긴다.

## 7. 알려진 제약

- Unity 자동 실행은 **D3D11 전용** (D3D12 크래시 이력, 미승인).
- Unity Editor가 열려 있으면 batchmode 금지.
- 자동 루프는 dry-run 정책 + 클린 Git baseline 필요 (`Automation/LoopEngineering/loop-policy.json`).
- PowerShell 스크립트 직접 실행은 실행 정책에 막힐 수 있음 → `-ExecutionPolicy Bypass -File` 패턴 사용 이력 있음.
