# ACTIVE_TASK — 현재 진행 중인 단일 작업

사용법: `TASK_QUEUE.md`에서 작업 하나를 골라 아래 양식에 **복사**해 채운다. 동시에 두 작업 금지.
작업이 끝나면 이 파일을 비우고, `TASK_QUEUE.md` 상태를 `DONE`으로, `DONE_TASKS.md`에 한 줄 기록, 3~5개마다 `HANDOFF_FOR_CODEX.md` 갱신.

먼저 읽을 문서: `../00_START_HERE/ONE_PAGE_WORKFLOW.md`, `../02_AGENT_RULES/`(3종), `../04_VERIFICATION/VERIFICATION_RULES.md`.

---

## 현재 작업

`BETA-009 Persistence and Recovery Pass` — BETA-008 validation-debt checkpoint `46dbea9` 뒤 M85 선승인 sequence로 자동 활성화된 유일한 티켓.

- Baseline: `milestone/gameplay-beta-85@46dbea9`; 활성화 전 working tree clean.
- 목표: 실제 WorldSandbox beta 상태를 저장하고 게임 종료·재실행 뒤 복원하여 같은 저장에서 정상 플레이를 이어 가며, 반복 load에도 runtime 객체가 중복되지 않게 한다.
- 필수 상태: world seed/delta/building/player, Inventory/Hotbar/ItemInstance, Economy, GameClock, shop/furniture, LongPlay progression, Hiring, Feed, village response와 M85 신규 상태를 기존 `SaveManager`·v11 권위에 연결한다.
- 보존: 기존 Golden save 회귀, v10 이하 `LegacyFixed` migration, BETA-001~008 구현/checkpoint, crash-safe repository 경계, Scene/Prefab/Packages/ProjectSettings.
- 허용: 기존 schema 안의 additive field, 기본값, backward-compatible serialization, null-safe restore, 중복 생성 방지.
- 금지: 기존 필드 의미 변경, destructive migration, 저장 데이터 삭제, 호환성 중단, 병렬 save path/manager, push/rebase/reset/clean.
- 현재 단계: SaveManager capture/restore 순서와 각 M85 시스템의 실제 write/restore seam을 감사하고, 종료→재실행→load→계속 플레이 및 같은 저장 반복 load의 누락·중복 위험을 확정한다.
- 검증 예산: BETA-009 최초 D3D11 1회와 결정적 비충돌 결함의 최소 교정 뒤 1회까지 선승인. 반드시 D3D11이며 native crash 발생 시 즉시 중단한다.

## 체크포인트 결과

- 상태: `BETA_009_IMPLEMENTED_WITH_VALIDATION_DEBT`.
- 구현: additive gameplay save envelope v12를 도입하되 procedural world payload와 `LegacyFixed` 의미는 v11로 유지했다. Sales/Feed, exact village response, 농작물, B09 저장물, shop-open, hotbar 선택, player pose/온보딩과 load-boundary 정리를 기존 권위에 연결했다.
- 정적 검증: Runtime/Editor compile 오류 0, `git diff --check` PASS, JSON parse PASS, native crash 0.
- D3D11: 첫 실행은 validator 지역변수 compile 오류, 보정 실행은 저장 직전 잘못된 B01 이동 좌표 `(1,-1)`에서 중단됐다. 좌표를 기존 유효 계약 `(1,0)`으로 수정한 최종 소스는 compile PASS지만 세 번째 실행은 하지 않았다.
- 미검증: 실제 save → Editor Play 종료 → 재진입 → load → 계속 플레이 → 동일 save 재로드 중복 방지. BETA-010 통합 검증에서 가장 먼저 이어 간다.
- 보호: Scene/Prefab/Packages/ProjectSettings 변경 없음, push 없음.
