# ACTIVE_TASK — 현재 진행 중인 단일 작업

사용법: `TASK_QUEUE.md`에서 작업 하나를 골라 아래 양식에 **복사**해 채운다. 동시에 두 작업 금지.
작업이 끝나면 이 파일을 비우고, `TASK_QUEUE.md` 상태를 `DONE`으로, `DONE_TASKS.md`에 한 줄 기록, 3~5개마다 `HANDOFF_FOR_CODEX.md` 갱신.

먼저 읽을 문서: `../00_START_HERE/ONE_PAGE_WORKFLOW.md`, `../02_AGENT_RULES/`(3종), `../04_VERIFICATION/VERIFICATION_RULES.md`.

---

## 현재 작업

`BETA-007 Village Response and NPC Integration` — `BETA_007_IMPLEMENTED_WITH_VALIDATION_DEBT` 체크포인트 정리 중인 유일한 티켓.

- Baseline: `milestone/gameplay-beta-85@1539923cc9a34659e7b43bf9a3c6b14073152efa`; 활성화 전 working tree clean.
- 목표: 실제 판매 결과가 다음날 마을·주민·월드 반응으로 이어지고, 플레이어가 판매→기록→다음날 변화의 인과관계를 이해하게 한다.
- 필수 상태: 기존 `SalesLogManager`, `VillageChangeSignalController`, `VillageCultureVisualController`, NPC schedule/role, WorldSandbox 역할·시설 anchor와 GameClock/day transition을 재사용한다.
- 보존: `Prototype_FirstDay.unity` Golden, M70 WorldSandbox, 기존 NPC/판매/경제/마을/시간/Save 권위, Save schema v11.
- 금지: 고정 좌표 남발, 새 마을/주민 manager, 가짜 판매 결과, 기존 NPC lifecycle 우회, Scene/Prefab/Packages/ProjectSettings/Save schema를 기본 해결책으로 사용, push/rebase/reset/clean.
- 현재 단계: 판매→다음날 exact 상품 snapshot→역할별 시설 변화→주민 대화/HUD 연결 구현 완료. Runtime/Editor 정적 컴파일 오류 0. 두 D3D11 실행은 B08/HUD/bootstrap까지 통과했지만 주민 anchor 준비 단계에서 종료되어 실제 hire·판매·Day 2 assertion에는 도달하지 못했다.
- 보존 로그: `Logs/BETA007_D3D11_Validation.log`, `Logs/BETA007_D3D11_Correction.log`.
- 정적 후속 보정: runtime obstacle/NavMesh가 안정된 뒤 anchor를 구성하고, 현재 navigation revision에서 유효한 지점만 `HiringService.spawnPointRotation`에 동기화한다. 실제 상품의 ProductionData/RecipeData가 지정하는 역할·시설과 월드 변화 안내도 일치시켰다.
- 체크포인트 규칙: 세 번째 Unity 실행과 assertion 완화는 하지 않는다. 명시된 장기 Goal의 validator-debt 지속 정책에 따라 `BETA_007_IMPLEMENTED_WITH_VALIDATION_DEBT` local checkpoint까지만 만들고, push 없이 선승인된 `BETA-008`을 다음 단일 티켓으로 활성화한다.
