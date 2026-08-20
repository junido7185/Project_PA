# ACTIVE_TASK — 현재 진행 중인 단일 작업

사용법: `TASK_QUEUE.md`에서 작업 하나를 골라 아래 양식에 **복사**해 채운다. 동시에 두 작업 금지.
작업이 끝나면 이 파일을 비우고, `TASK_QUEUE.md` 상태를 `DONE`으로, `DONE_TASKS.md`에 한 줄 기록, 3~5개마다 `HANDOFF_FOR_CODEX.md` 갱신.

먼저 읽을 문서: `../00_START_HERE/ONE_PAGE_WORKFLOW.md`, `../02_AGENT_RULES/`(3종), `../04_VERIFICATION/VERIFICATION_RULES.md`.

---

## 현재 작업

`BETA-006 Phone Hiring and Feed Completion` — 구현·D3D11 검증·회귀 완료, 승인된 로컬 ticket commit 직전의 유일한 티켓.

- Baseline: `milestone/gameplay-beta-85@0c9131b2f7256aa8ccb9b458b0fee2db5a4371c9`; 시작 working tree clean.
- 목표: 기존 `HiringService`와 후보 데이터를 실제 휴대폰 채용 루프로 노출하고, Feed가 판매 전 honest empty state와 판매 후 실제 판매·마을 변화 데이터를 보여 주게 한다.
- 필수 상태: 후보 목록, 역할, 실제 비용, 잔액/티어/중복 가능 여부, 채용 전·후, 실제 결과; Feed empty/sales/village link; Audit와 Settings 정상 유지.
- 보존: `Prototype_FirstDay.unity` Golden, WorldSandbox/M70, 기존 Hiring/Economy/Tier/SalesLog/Village/Save 권위, Save schema v11.
- 금지: Scene/Prefab/Packages/ProjectSettings/Save schema 변경을 기본 해결책으로 사용, 새 채용/피드 manager 생성, HiringService/EconomyService 재작성, 가짜 후보·판매·마을 데이터, push/rebase/reset/clean.
- 완료 결과: WorldSandbox runtime 휴대폰 4개 앱, 후보 8명, 비용/잔액/잠금/고용 후 roster, 실제 고용과 C-02~C-09 기반 역할별 주민 스폰, 판매 전 빈 Feed와 실제 판매 후 마을 변화 Feed를 연결했다.
- 검증: Runtime/Editor compile 오류 0. `BETA006_D3D11_Validation`, BETA-005, VillageChangeSignal, Golden FinalDemoRoute D3D11 회귀 PASS, blocking Console 0, 신규 crash 0.
- 보호 결과: 기존 Scene, Packages, ProjectSettings, Save schema/authority, 원본 C-02~C-09 FBX는 무변경이다. 새 주민 prefab은 원본을 중첩 참조하는 재현 가능한 wrapper다.
- 다음 단계: BETA-006 범위만 로컬 commit하고 push하지 않은 뒤 선승인 `BETA-007 Village Response and NPC Integration`을 자동 활성화한다.
