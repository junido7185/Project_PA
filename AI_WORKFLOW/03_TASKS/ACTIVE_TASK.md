# ACTIVE_TASK — 현재 진행 중인 단일 작업

사용법: `TASK_QUEUE.md`에서 작업 하나를 골라 아래 양식에 **복사**해 채운다. 동시에 두 작업 금지.
작업이 끝나면 이 파일을 비우고, `TASK_QUEUE.md` 상태를 `DONE`으로, `DONE_TASKS.md`에 한 줄 기록, 3~5개마다 `HANDOFF_FOR_CODEX.md` 갱신.

먼저 읽을 문서: `../00_START_HERE/ONE_PAGE_WORKFLOW.md`, `../02_AGENT_RULES/`(3종), `../04_VERIFICATION/VERIFICATION_RULES.md`.

---

## 현재 작업

`BETA-008 Day 1–7 Progression` — 구현은 `BETA_008_IMPLEMENTED_WITH_VALIDATION_DEBT`로 체크포인트 준비가 끝났으며, 아직 BETA-009 구현은 시작하지 않았다.

- Baseline: `milestone/gameplay-beta-85@4bf89f4`; 활성화 전 working tree clean.
- 목표: fresh WorldSandbox에서 Day 1부터 Day 7까지 dead-end 없이 진행하며 제작·상품·고객·상점 성장·채용·마을 반응이 점진적인 목표와 보상으로 이어지게 한다.
- 필수 상태: 기존 `GameClock`, `DayNightShopLoopController`, `PlayableDayScenarioController`, `LongPlayProgressionController`, `TierService`, `EconomyService`, `HiringService`, `VillageCultureVisualController`와 실제 gameplay authority를 재사용한다.
- 보존: `Prototype_FirstDay.unity` Golden, M70 WorldSandbox, BETA-001~007 구현/checkpoint, 기존 경제·인벤토리·제작·판매·채용·시간·Tier·Save 권위, Save schema v11.
- 금지: 새 progression manager/quest database, 자동으로 플레이어 행동 수행, 가짜 일차·돈·판매·채용 상태, 장기 캠페인의 정적 계약을 실플레이 완료로 과장, Scene/Prefab/Packages/ProjectSettings/Save schema를 기본 해결책으로 사용, push/rebase/reset/clean.
- 구현 결과: New Game이 실제 clock을 시작하고 B01 간판을 runtime 상점에 결속한다. 고용 전에도 승인된 주민 wrapper를 읽기 전용 외형 원천으로 쓰는 관광객이 실제 구매 경로를 밟으며, Day 2~7 납품은 `B` 명시 구매·실제 비용 차감·실패 재시도를 사용한다. HUD와 Day 7 gate는 누적 매출·고용·마을 반응을 실제 권위에서 읽는다.
- 검증 결과: 두 번째 D3D11에서 Day 1 일반 관광객, Day 1~4 실제 판매/정산/다음날, Day 2~5 명시 납품과 Day 5 실제 Farmer 고용까지 PASS했다. fixture가 같은 고가 상품을 네 번 골라 Day 5 누적 매출 989/1050G에서 멈춘 뒤, 네 종류 고가 상품을 선택하도록 보정했고 Runtime/Editor compile 오류 0을 확인했다.
- 중단 상태: 승인된 두 D3D11 실행을 모두 사용했으므로 세 번째 실행은 하지 않는다. Day 6~7과 Week 1 완료는 미도달 검증 부채이며, PASS/COMPLETE로 기록하지 않는다. local checkpoint 뒤 선승인 `BETA-009 Persistence and Recovery Pass`만 활성화한다.
