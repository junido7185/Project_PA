# ACTIVE_TASK — 현재 진행 중인 단일 작업

사용법: `TASK_QUEUE.md`에서 작업 하나를 골라 아래 양식에 **복사**해 채운다. 동시에 두 작업 금지.
작업이 끝나면 이 파일을 비우고, `TASK_QUEUE.md` 상태를 `DONE`으로, `DONE_TASKS.md`에 한 줄 기록, 3~5개마다 `HANDOFF_FOR_CODEX.md` 갱신.

먼저 읽을 문서: `../00_START_HERE/ONE_PAGE_WORKFLOW.md`, `../02_AGENT_RULES/`(3종), `../04_VERIFICATION/VERIFICATION_RULES.md`.

---

## 현재 작업

- **Task ID**: Task 068 (연결부 1건)
- **작업명**: Day 1 결산부터 Day 3 아침까지 플레이어 날짜 전환 연결
- **Phase / 난이도**: Phase 8 / L 중 날짜 전환 하위 범위
- **사람 승인 필요**: YES — 활성 `/goal`과 CASTLE BUILD 완성 지시를 이 연결부의 승인으로 적용

## 목표

- 검증기의 `ForceSet` 우회 없이, Day 1 결산 버튼과 Day 2+ 정산 간판 상호작용으로 다음 날 아침을 시작할 수 있게 한다.

## 수정 예정 파일 (작업 전 확정)

- `Assets/Scripts/Services/GameClock.cs`
- `Assets/Scripts/DayNightShopLoopController.cs`
- `Assets/Scripts/ShopOpenSign.cs`
- `Assets/Scripts/UI/PlayableDayScenarioController.cs`
- `Assets/Editor/PA_FinalDemoRouteValidator.cs`
- `Assets/Editor/PA_DayNightShopLoopValidator.cs`
- Task 068 상태/검증 기록 및 필수 세션 문서

## 수정 금지 파일 (위험 파일)

- `Assets/Scenes/Prototype_FirstDay.unity`, 프리팹, Save v9 스키마
- `Shop`/`ShopSlot`/`EconomyService`/`PurchaseEvaluator`/`NpcController` 로직
- NPC 스케줄 시간대와 경제 밸런스

---

## 작업 전 보고 (구현 시작 전 작성)

- 사전 점검: 경로/Git 루트 정상, Unity Editor 닫힘, 최신 크래시 리포트 2026-06-25 확인. 기존 NPC/실내 변경 및 ZIP 삭제 보존.
- 접근 방법 (기존 시스템 재사용 여부): 기존 `GameClock.OnNewDay`, `ShopOpenSign`, Day 1 결산 버튼을 재사용한다.
- 건드리지 않을 것 (보존 선언): 씬/프리팹/저장/경제/구매/NPC FSM/시간대 밸런스 무변경.
- 검증 방법 예고: 런타임/에디터 컴파일, D3D11 FinalDemoRoute + DayNightShopLoop.

## 작업 후 보고 (구현 완료 후 작성)

- 실제 수정한 파일: `GameClock.cs`, `DayNightShopLoopController.cs`, `ShopOpenSign.cs`, `PlayableDayScenarioController.cs`, FinalRoute/DayNight 검증기, 상태 문서.
- 한 것 / 안 한 것: Day 1 결산→Day 2 및 Day 2 정산 간판→Day 3 전환을 연결. 낚시 왕복·밤 손님 시간대·저장 스키마는 변경하지 않음.
- 기존 데모 흐름 유지 확인: FinalDemoRoute가 첫 판매 30G 후 Day 2 전환까지 PASS.

## 검증 결과 (VERIFICATION_RULES.md 양식)

- 컴파일: 런타임/에디터 오류 0(기존 CS8785/CS0414 경고).
- 검증기: FinalDemoRoute PASS(`Logs/Codex_Task068_FinalRoute_Day2Transition.log`), DayNightShopLoop PASS(`Logs/Codex_Task068_DayNight_Day3Transition.log`).
- 상점 루프: Day 1 진열·가격·구매 30G 유지, Day 2 정산 간판 상호작용 후 Day 3 준비 재고 활성 확인.
- 저장/로드: 스키마 무변경. 새 전환 후 저장 종료/재실행은 이번 작업에서 확인 못 함.
- 마을 변화: 기존 OnNewDay 이벤트 경로를 보존했으나 별도 VillageCulture 회귀는 실행하지 않음.
- 확인 못 한 것: 사람 3일 연속 플레이, Windows 빌드, 1920x1080 목표/간판 가독성.

## 실패 시

- `../05_LOGS/BUG_LOG.md`에 증상/원인추정/시도/중단이유 기록. 같은 원인 2회 실패 후 세 번째 시도 금지. 파일 삭제·Git 되돌리기 금지.

## 완료 처리 체크

- [x] `TASK_QUEUE.md`에서 이 Task 상태를 `PARTIAL`로 갱신
- [ ] `DONE_TASKS.md`에 한 줄 기록
- [x] `../05_LOGS/CHANGELOG_AI.md` append
- [x] 루트 기록 갱신(STATUS/TODO/SESSION_REPORT/개발일지)
- [x] `../06_HANDOFF/HANDOFF_FOR_CODEX.md` 갱신
- [ ] 이 파일(ACTIVE_TASK) 비우기
