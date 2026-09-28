# Handoff 절차

1. [CURRENT_STATE](../../Docs/00_CURRENT/CURRENT_STATE.md)에서 checkpoint·dirty 보호·미해결 사항을 확인한다.
2. [INTEGRATION_QUEUE](../../Docs/00_CURRENT/INTEGRATION_QUEUE.md)와 마지막 사람 지시를 대조한다. 과거 active 값으로 새 티켓을 시작하지 않는다.
3. [CAPABILITY_REGISTRY](../../Docs/00_CURRENT/CAPABILITY_REGISTRY.md)와 [GAME_LOOP_MAP](../../Docs/00_CURRENT/GAME_LOOP_MAP.md)에서 재사용 대상과 연결 경계를 확인한다.
4. 재개 작업이면 해당 월의 마지막 기록·로그·실행 중인 검증 결과부터 회수한다. 이미 PASS한 항목을 이유 없이 반복하지 않는다.
5. 중단 시 월별 이력에 exact resume point·마지막 실패 원인·보호 파일·검증 증거를 기록한다. 상태가 바뀐 CURRENT 항목만 갱신한다.

이 파일에 현재 상태나 완료 로그를 복제하지 않는다. 이전 handoff 원문은 [월별 이력](../../Docs/04_DEVELOPMENT_LOG/README.md)에 보존되어 있다.
