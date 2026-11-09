# 작업자 규칙

공통 계약은 AGENTS.md를 따른다. 원본 Git/dirty/세이브 보호, 소유권, 실제 검증, 연속 진행과 복구 정책을 여기서 중복 정의하지 않는다.

- 현재 state/lease의 총괄(현재 기준 Claude Code): 원본 통합, 단일 Unity Editor, diff 검토, 실제 입력/GameView/빌드 확인.
- 하위 작업자(Claude 또는 Codex 독립 지원): 현재 HEAD와 관련 로컬 상태를 반영한 격리 worktree; 지정 파일만 수정. 원본 MCP 제외, Editor 실행 금지. 비 owner 원본 세션은 읽기·검토만 한다. 현재 가용성만으로 writer를 바꾸지 않는다.
- 배정: 목표, 기준 HEAD/branch, 소유 파일, 검증 방법, inactivity timeout과 복구 checkpoint 주기를 기존 loop-state에 기록.
- 반환: 변경 파일, 실행한 검증, 미해결, 다음 행동, 세션 ID. Unity 확인 전 게임 완료 주장 금지.
- 통합: 원본 해시가 배정 시점과 같은지 확인하고 범위 밖 diff는 통합하지 않는다. 변경 전 백업.
- 모델: D5/D7 포함 실제 역량/결과로 배정한다. 요청 모델/effort와 실제 응답 모델/적용 확인을 분리한다. 복잡한 통합은 High 이상, 일상 코드는 Medium/High로 시작하고 결과로 조절한다.
- 도구: Serena는 선택. rg/Read가 기본 복구 경로다. Task Observer는 선택 관찰이다.

2026-10-19 현재 운영 정책: 기존 Canon의 전체 게임 목표와 현재 실행 묶음의 FINAL_WINDOWS_HUMAN_REVIEW를 구분한다. 승인 목표 안의 구현·세부 설계·복구·검증은 자율 진행하며 단계마다 사용자 재승인을 요구하지 않는다. 파일 수·작업 총시간·compile/Play/validator 횟수·기계적 연속 실패·옛 티켓/모델/담당자 배정을 실행 상한으로 쓰지 않는다. 파일 소유 범위는 충돌 확인 후 기존 registry에서 조정하고, 순서는 최신 checkpoint와 품질 우선순위를 따른다. 시도·사용량·실패·거절·승인 이력은 누적 보존한다. 같은 행동을 진전 없이 반복하면 원인·바꾼 접근·새 근거를 checkpoint에 남긴 뒤 복구하고, 실행 상태가 모호하면 확인 전 재실행하지 않는다. 실제 출력/소유 파일 활동이 끊긴 프로세스의 inactivity timeout은 유지하며 작업 총시간 상한과 분리한다. 120초 복구 스냅샷·20분 안전 인계는 재개 가능한 복구 주기다. 실제 실행 정책은 Automation/LoopEngineering/loop-policy.json의 recoveryPolicy를 따른다. 사용자 dirty·Save 호환성·기존 권위/디자인 조건·현재 반려/보류·단일 Unity writer·미승인 결제/외부 공개 금지·최종 사람 검수는 유지한다. 과거 고정 담당자 문구보다 현재 유효한 state/lease와 검증된 인수인계가 우선하며, 이번 운영 정리는 제작 재개나 writer 이전 승인이 아니다.
