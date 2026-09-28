# ONE PAGE WORKFLOW

## 작업 순서

1. Git 루트·branch·HEAD·dirty를 확인하고 기존 사용자 변경을 보호한다. Unity 작업은 실행 중인 Editor와 루트 최신 crash report를 먼저 확인한다.
2. [Docs 입구](../../Docs/README.md)의 `00_CURRENT` 네 문서를 읽는다. 추가 자료는 [유형별 안내](DOCS_INDEX.md)에서 필요한 것만 고른다.
3. 마지막 사용자 지시와 [작업 큐](../../Docs/00_CURRENT/INTEGRATION_QUEUE.md)의 승인 경계를 대조하여 한 작업만 진행한다. snapshot의 ACTIVE는 실행 승인이 아니다.
4. 코드 수정 전에 파일 목록·접근·검증 방법을 보고한다. 기존 authority를 재사용한다.
5. [작업자 규칙](../02_AGENT_RULES/CODEX_WORKER_RULES.md), [Unity 규칙](../02_AGENT_RULES/UNITY_CODING_RULES.md), [슬롭 방지](../02_AGENT_RULES/AI_SLOP_PREVENTION.md)를 따른다.
6. [검증 규칙](../04_VERIFICATION/VERIFICATION_RULES.md)의 작업 유형에 해당하는 검증을 실행한다. 미실행은 이유와 함께 명시한다.
7. 월별 [개발 이력](../../Docs/04_DEVELOPMENT_LOG/README.md)에 결과를 한 번 기록하고, 바뀐 `00_CURRENT` 항목만 갱신한다.

## 경계

- 게임 정체성 재설계, 전체 시스템 재작성, 승인 없는 씬·저장·패키지 변경 금지.
- Project_D 수정·복사·병합 금지. 사용자 dirty를 되돌리지 않는다.
- push·승인 없는 commit·파괴적 Git 명령 금지.
- 같은 원인 2회 실패 뒤 세 번째 실행 금지. 로그를 보존하고 원인·중단점을 보고한다.
- crash, 저장 파괴 가능성, 사용자 작업 충돌 등은 [버그 기록](../05_LOGS/BUG_LOG.md)과 함께 중단한다. 이미 명시적으로 승인된 범위는 다시 승인받지 않는다.
- 선승인 bounded sequence도 한 티켓씩만 수행하며 기록된 승인 범위에서 멈춘다. 자세한 정책은 [AGENTS](../../AGENTS.md).
