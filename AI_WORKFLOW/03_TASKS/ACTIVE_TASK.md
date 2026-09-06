# ACTIVE_TASK — 현재 진행 중인 단일 작업

사용법: `TASK_QUEUE.md`에서 작업 하나를 골라 아래 양식에 **복사**해 채운다. 동시에 두 작업 금지.
작업이 끝나면 이 파일을 비우고, `TASK_QUEUE.md` 상태를 `DONE`으로, `DONE_TASKS.md`에 한 줄 기록, 3~5개마다 `HANDOFF_FOR_CODEX.md` 갱신.

먼저 읽을 문서: `../00_START_HERE/ONE_PAGE_WORKFLOW.md`, `../02_AGENT_RULES/`(3종), `../04_VERIFICATION/VERIFICATION_RULES.md`.

---

## 현재 작업

`CONTENT-000B Day 1–30 Campaign and Quest Architecture` — 2026-09-06 사용자 선승인으로 활성화된 유일한 티켓.

- Canon: `CONTENT_CANON_BIBLE.md`, PROVISIONAL CANON v1 승인 완료.
- 산출물: `CONTENT_CAMPAIGN_DAY1_30.md`, `CONTENT_IMPLEMENTATION_BACKLOG.md`.
- 기준: `milestone/gameplay-beta-85@29fb98f4`; 기존 사용자 dirty 파일을 보존하고 콘텐츠 변경만 커밋한다.
- 검증: 30일 분류, 12~16 anchor, 첫 주 구현 계약, 주민·요청·해금·경제·저장·복구 연결의 자기검증. 이번 티켓은 설계이며 Unity 실행은 후속 구현 티켓에서 한다.
- 다음: 자기검증 PASS와 로컬 커밋 뒤 CONTENT-001 자동 활성화. CONTENT-010까지 추가 사람 승인 요청 없음.
- HARD HUMAN GATE: 최신 사용자 지시 §16을 적용한다. 승인 부재는 blocker가 아니다. 기존 BETA-010의 137° 복원 실패를 해결됐다고 표시하거나 세 번째 동일 실행을 하지 않는다.

## 보존된 이전 작업 이력 — 현재 활성 티켓 아님

`BETA-010 Full Playable Beta Integration` — BETA-009 validation-debt checkpoint `6168d05` 뒤 M85 선승인 sequence로 자동 활성화된 유일한 티켓.

- Baseline: `milestone/gameplay-beta-85@6168d05`; 활성화 전 working tree clean.
- 목표: WorldSandbox에서 새 게임→온보딩→낮 활동→제작·가공→진열·가격→개점→고객 반응·구매/거절→정산→다음날 반응→Day 1~7→채용·Feed→저장→재시작→복원→계속 플레이를 하나의 실제 beta 경로로 연결한다.
- 첫 검증 우선순위: 교정된 BETA-009 save→Play 종료→재진입→load→continue→same-save repeat-load 경로를 실제 D3D11에서 먼저 증명한다.
- 통합 부채: BETA-007 실제 hire→판매→Day 2→주민 대화, BETA-008 Day 6~7/Week 1 completion을 같은 full-loop 계약에서 재검증한다.
- 기존 권위: ItemInstance, Inventory, CraftingService, EconomyService, Shop/ShopSlot, PurchaseEvaluator, NpcController, HiringService, SaveManager, WorldGrid를 우회하지 않는다.
- 보호: Prototype_FirstDay Golden, MainGame, Scene/Prefab/Packages/ProjectSettings, procedural world payload v11과 additive gameplay envelope v12 호환.
- 금지: assertion 약화, 강제 PASS, validator 전용 production 우회, D3D12, push/rebase/reset/clean, 별도 병렬 gameplay/save manager.
- 검증 예산: BETA-010 최초 D3D11 통합 1회와 결정적 비충돌 결함 최소 교정 뒤 1회. native crash, 데이터 손상, Golden/M70 핵심 회귀면 즉시 중단한다.
- 완료 조건: 기능 assertion·compile·Console/crash·Golden 회귀가 실제로 통과하고 30~45분 플레이 경로에 진행 차단이 없을 때만 `M85_GAMEPLAY_BETA_COMPLETE`로 기록한다.

## 현재 중단 지점

- 상태: `HARD_BLOCKER_BETA_010_PLAYER_FACING_RESTORE`.
- `Logs/BETA010_PersistenceRestart_Initial.log`는 save·Editor Play 종료·재진입·load 뒤 world checksum과 B09 contents까지 복원했지만 player facing assertion에서 실패했다.
- SaveManager가 모든 restore consumer 뒤에 pose를 다시 적용하도록 보정했으나 `Logs/BETA010_PersistenceRestart_Correction.log`도 같은 cell `(64,61)`은 복원하면서 facing만 정확히 `137°` 소실했다.
- 승인된 최초/교정 D3D11 두 실행을 모두 사용했다. 동일 원인 세 번째 실행, 추가 추측 수정, assertion 완화는 금지한다.
- 미도달: 복원 후 계속 판매, same-save repeat load, BETA-007/008 debt, full-loop/Golden regression, `M85_GAMEPLAY_BETA_COMPLETE`.
