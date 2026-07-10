# HANDOFF_FOR_CODEX — 다음 세션 인수인계

최종 갱신: 2026-07-10 (Codex Task 001 프로젝트 구조 인덱스 완료)
규칙: **3~5개 작업마다** 이 문서의 "현재 상태"와 "우선순위"를 갱신한다. (`CODEX_HANDOFF_PROMPT` 사용)

## 0. Codex 첫 실행이라면 (중요)

- 아직 Codex를 실전 투입한 적이 없다. **첫 실행은 `../00_START_HERE/CODEX_FIRST_RUN_PLAYBOOK.md`를 따른다.**
- **첫날은 Bootstrap + Task 001~003(문서·조사)까지만.** 기능 구현(Task 008 이후)은 첫날 금지.
- 7일 가동 계획: `../07_FULL_GAME_ROADMAP/CODEX_FIRST_7_DAYS_PLAN.md`.
- 사용자 미확정 항목: `../00_START_HERE/DECISION_REQUIRED_FOR_USER.md` (첫 기능 작업 전 #3 검증기 실행 방식, #5 하루 작업 수만 확정하면 충분).
- **3~5개 작업마다 HANDOFF 갱신 / 5~10개마다 사람 검수 / 실패 2회 시 사람 판단 / Demo Lock 이후 새 기능 금지 / Full Game 확장은 Vertical Slice 안정화 이후에만.**

## 1. 읽을 문서 순서

1. 루트 `AGENTS.md` — 입구 (1분)
2. `../00_START_HERE/ONE_PAGE_WORKFLOW.md` — 작업 절차
3. `../00_START_HERE/PROMPT_LIBRARY.md` — 지금 어떤 프롬프트를 쓸지 선택
4. `../01_IDENTITY/PROJECT_PA_IDENTITY.md` — 정체성 (재해석 금지)
5. `../01_IDENTITY/PROJECT_PA_SCOPE.md` — 현재 단계: **Stage 0 MVP Stabilization**
6. `../02_AGENT_RULES/` 3종 — 규칙
7. `../03_TASKS/TASK_QUEUE.md` — 다음 작업 1개 선택 → `../03_TASKS/ACTIVE_TASK.md`에 복사
8. `../04_VERIFICATION/VERIFICATION_RULES.md` — 완료 조건
9. (일정 감각 필요 시) `../07_FULL_GAME_ROADMAP/DEVELOPMENT_TIMELINE.md`

## 2. 현재 게임 정체성 (한 줄)

> "낮에는 동물 마을을 만들고, 밤에는 그 마을의 유일한 잡화점을 운영하는 3D 코지 라이프 시뮬레이션" — 핵심 차별점: **내가 판 물건이 마을을 바꾼다.**

**목표는 프로토타입이 아니라 완성 게임이다.** 졸업 시연(Stage 2)은 완성 게임의 부분집합. 일회용 코드·하드코딩·저장 미지원으로 때우지 않는다.

## 3. 현재 작업 우선순위 (2026-07-10 기준)

Git은 **clean baseline**(커밋 `53fd115` 이후 프리플라이트 문서 추가). 4회차 프리플라이트 검수 통과 — 상세 `../00_START_HERE/AI_WORKFLOW_FINAL_AUDIT.md`.

1. **[Codex] Task 002** — 현재 데모 플로우 문서화. Task 001에서 작성한 `../03_TASKS/PROJECT_CODE_INDEX.md`를 참고해 `Prototype_FirstDay.unity` Day 1 루트와 코드 진입점을 대조.
2. **[Codex] Task 003** — 컴파일 기준선 기록. 문서만, 실제 실행 결과 또는 보류 사유를 기록.
3. **[사람+Codex] Task 004** — 보류 검증기 2종 실행 방식 결정(`DECISION_REQUIRED_FOR_USER` #3) 후 실행.
4. **[Codex] Task 005~007** — 위험 파일 목록 / git 정책 / 저장 스키마 조사.
5. **[Codex→사람] Task 008~016** — MVP Stabilization(기존 시스템 회귀 확인 + 버그 1개 + 완료 판정).
6. 이후 Phase 2(Shop Core) 진입.

미확정(사용자 결정 대기, `DECISION_REQUIRED_FOR_USER.md`): Demo Lock 날짜 / 두 번째 낮 활동(임시 낚시) / 검증기 실행 방식 / 주간 작업 시간 / 하루 작업 수.

## 4. 위험 파일 (수정 전 반드시 규칙 확인)

| 파일/영역 | 위험 |
|---|---|
| `Assets/Scenes/Prototype_FirstDay.unity` | 메인 씬. 덮어쓰기·재작성 금지. Build Settings 시작 씬 |
| `SaveManager.cs`/`SaveData.cs`/`LocalJsonSaveRepository.cs` | 저장 스키마. 추가 확장만(v증가+마이그레이션), 사람 승인 |
| `Shop.cs`/`ShopSlot.cs`/`EconomyService.cs`/`PurchaseEvaluator.cs`/`NpcController.cs` | 경제·구매 코어. 넓은 수정은 사람 승인. 표시/로그만 우선 |
| `PlayerController.cs`/`PlayerInteraction.cs`/`Inventory.cs` | 조작·인벤토리 코어. 임의 재작성 금지 |
| `Docs/01~08` | 동결 문서. 코드 주석이 `Docs/§번호` 인용 |
| `PROJECT_PA_CRASH_REPORT_20260625.md` | 루트 고정 (preflight glob) |
| `Assets/Jinxish/**` | 서드파티. 수정 금지 |

## 5. 동결 문서 (이동·개명·대수정 금지)

- `Docs/01_개요_및_정체성.md` ~ `Docs/08_아트_및_씬_구성_가이드.md` — 코드 § 인용. 최신 기준은 `../01_IDENTITY/`.
- `PROJECT_PA_CRASH_REPORT_20260625.md` — preflight 루트 glob.
- `Assets/Jinxish/**/readme.md` — Unity 관리 영역.
- 상세: `../00_START_HERE/DOCS_INDEX.md` §5.

## 6. 금지사항 (요약)

정체성 재해석 / 감성 서사·철학·다크·순수 타이쿤 변경 / 대형 리팩터링 / 기능 삭제 / 외부 패키지 / 멀티플레이 착수 / 씬·프리팹·저장 임의 변경 / 메인 씬 덮어쓰기 / Project_D 접근 / git push / 검증 없는 완료 선언 / 한 작업에 여러 기능. 전체: `../02_AGENT_RULES/AI_SLOP_PREVENTION.md`.

## 7. 다음 작업 방식

1. `PROMPT_LIBRARY.md`에서 상황에 맞는 `CODEX_*_PROMPT`를 골라 사용.
2. 사전 점검(경로/Git/Unity 프로세스/크래시 리포트) → `CODEX_WORKER_RULES.md` §4.
3. `TASK_QUEUE.md`에서 TODO 1개 → `ACTIVE_TASK.md` 복사 → 수정 계획 보고 → 최소 변경 → 검증 → 기록.
4. 실패 2회 → `BUG_LOG.md` 기록 후 정지, 사람 판단 요청.
5. **하루 권장 작업 수**: 저강도 1~2 / 집중 3~5 / 크런치 5~8(품질 위험). 상세 `DEVELOPMENT_TIMELINE.md` §7.
6. **위험 작업(사람 승인 YES) 전 반드시 사람 승인.**
7. **Demo Lock 이후 새 기능 금지** — `CODEX_DEMO_LOCK_PROMPT`만.
8. **3~5개 작업마다 이 문서(HANDOFF) 갱신**, 5~10개마다 사람 검수.

## 8. 알려진 제약

- Unity 자동 실행은 **D3D11 전용**(D3D12 크래시 이력, 미승인).
- Unity Editor 열려 있으면 batchmode 금지.
- 보류 검증기 2종(`PA_FinalDemoRouteValidator`, `PA_LongPlayProgressionValidator`)은 BLOCKED — `VERIFICATION_RULES.md` §2-B.
- 자동 루프는 dry-run 정책 + 클린 baseline 필요(`Automation/LoopEngineering/loop-policy.json`).
- PowerShell 스크립트 직접 실행은 정책에 막힐 수 있음 → `-ExecutionPolicy Bypass -File`.

## 9. DEVELOPMENT_TIMELINE.md를 읽어야 하는 시점

- 발표 날짜가 정해졌을 때(역산표 §6).
- 하루/주간 작업량을 정할 때(§5 시나리오, §7 속도 정책).
- "언제 무엇을 포기할지" 판단이 필요할 때(§8, §9).
- Full Game 확장 착수 전(§10 Fable 판단).
