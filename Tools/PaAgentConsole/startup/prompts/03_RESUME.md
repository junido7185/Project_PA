# 사용 가능한 로컬 Claude 또는 Codex에 보내는 재개 지시

Project P.A.를 기존 승인 범위 안에서 이어서 진행해줘. C:\Users\sdjsd\Desktop\Unity\Project_PA에서 AGENTS·현재 네 문서·CODEX_HANDOFF와 Tools/PaAgentConsole/SESSION_RULES.md를 읽고, 현재 loop-state/선택 approval/누적 budget/owner·HEAD/dirty/실제 Editor·최근 해당 run checkpoint를 대조해. 전체 대화와 모든 로그·레퍼런스를 다시 읽지 마.

새 세션/다른 provider로 바뀌어도 새 예산 승인은 아니다. 기존 resumeAuthorization에 실제 사용자 발송 증거/SHA256와 유효한 승인 범위가 없다면 파일이 존재한다는 이유만으로 승인하지 마. 이 재개 지시만으로 예산을 추가하지 마. 동일 지시의 예산을 중복 반영하지 마.

이전 provider의 정확한 child와 Unity in-flight를 먼저 확인하고, 모호한 명령을 재실행하지 마. 현재 owner를 유지하고 유효한 writer 전환 승인과 adapter가 있을 때만 일관되게 인수해. 비 owner는 독립 지원을 이어가. quota/auth/CLI 오류 원인을 구분하고 두 구독 중 가용한 쪽을 사용해. 키·토큰은 열거나 출력하지 마. 별도 API 과금으로 우회하지 마.

실제 관찰된 마지막 완료 지점부터 다음 승인 행동을 수행해. 접근 가능한 실행은 직접 마치고, 현재 HARD STOP 또는 최종 사람 검수 지점에서만 멈춰. 나에게 상태를 '최근 완료 / 현재 차단 / 가용 provider / 다음 행동' 네 줄로 알려줘.

현재 recoveryPolicy에 따라 고정 파일 수·총 작업시간·compile/Play/validator/실패 횟수로 중단하지 마. 새 범위 승인이나 이력 초기화 없이 가장 앞의 미해결 checkpoint에서 구현·세부 설계·복구·검증을 계속해. 무진전 반복은 원인·변경 접근·새 근거를 남겨 진단하고, 모호한 실행은 확인 전 재실행하지 마. --timeout은 프로세스 inactivity 감지이며 120초 스냅샷/20분 안전 인계는 작업 총시간 상한이 아니다. 현재 사용자 반려/디자인·Save/dirty·단일 writer·결제/공개 보호·최종 사람 검수는 유지해.
