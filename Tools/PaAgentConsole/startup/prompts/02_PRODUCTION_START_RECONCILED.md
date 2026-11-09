# 자율 제작 시작·재개 지시 — 현재 승인과 실제 checkpoint 보존

이 문안을 **내가 AI 대화에 직접 발송한 뒤에만** Project P.A. 제작을 재개해줘. 파일을 읽거나 복사한 것은 승인이 아니다. 최종 목표는 기존 Canon의 완성 게임이며 범위·콘텐츠·품질 목표를 줄이지 마. 현재 실행 묶음의 기존 종료 지점 `FINAL_WINDOWS_HUMAN_REVIEW`도 보존하고 중간 마일스톤을 전체 게임 완성으로 보고하지 마.

먼저 AGENTS와 필요한 현재 문서, CODEX_HANDOFF가 가리키는 이전 제작 Handoff, 선택된 approval의 실제 contract 및 첫머리 개정, HEAD/dirty/실제 실행 프로세스·Unity identity·in-flight를 대조해. 현재 선택 approval·활성 ticket·owner·checkpoint를 보존해. 2026-10-19 환경 감사에서 확인한 기준은 `demoPolishFinishApproval / D7 / claude-code`, stopped lease이며 이 자료 뒤 실제 상태가 바뀌었다면 최신 유효한 기록을 따른다. idle Claude 확장 호스트나 패널 조회를 실행 중인 제작 총괄로 기록하지 마.

Automation/LoopEngineering/loop-policy.json의 APPROVED_GOAL_PROGRESS_RECOVERY_20261019 및 기존 contract의 2026-10-09 개정과 `executionPolicy=APPROVED_GOAL_ADAPTIVE_PRODUCTION`을 적용해. 옛 티켓별 compile3/Play3/validator1과 D5/D7 모델 고정은 보존된 역사이며 현재 실행 gate로 되살리지 마. 현재 유효한 사용자 범위·반려·보류·디자인·결제 제한은 그대로 따른다. 고정 파일 수·작업 총시간·수정/compile/Play/validator 횟수·기계적 실패 횟수를 작업 상한으로 쓰지 마. 현재 업무 승인·사용량·과거 실패/거절은 보존하고, 미집계 사용량을0으로 만들지 마. 실제 시도와 비용은 누적 기록하며 provider/세션/구독 reset으로 초기화하지 마. 새 validator와 반복 검사는 필요한 목적과 기존 재사용 가능성을 먼저 확인하고 불필요하게 늘리지 마.

이 문안은 옛 D2 추가3/3/0을 새로 부여하거나 이미 진행한 월드 작업을 D2로 되돌리는 지시가 아니다. D2의 `INCOMPLETE_BLOCKED_BUDGET` 및 당시4/4·5/5·1/1은 원문 보존하고 해결·수락으로 바꾸지 마. D7의 옛 `NOT_STARTED` 행만으로 실제 월드 수정/컴파일/영상·후보 검증 기록을 지우거나 재실행하지 마. 실제 production Handoff의 미해결과 최신 증거에서 이어가.

최신 제작 순서인 **섬·바다·식생 → 캐릭터·채집 동작 → 아이템·생활 소품 → UI → 전체 조화**를 유지해. 대표 구역에서 이미 수행한 전후 비교·독립 비평·수정 결과를 먼저 인수하고 가장 앞의 미해결 품질 문제부터 진행해. 기존 D0~D12 범위를 재사용하되 옛 번호 순서가 이 우선순위를 막지 않게 해. 매 티켓 형식적 재승인을 묻거나 같은 조사·전체 레퍼런스 연구를 반복하지 마. 실제 완성 여부는 입력·GameView 시계열·같은 조건 비교·기능/표현/사람 수락으로 구분해.

초기 원본 Unity writer는 **실제 기존 state/lease의 owner**다. 현재 기준은 Claude Code 총괄·원본 통합, Codex 검토·격리 지원이다. 실행기를 쓰려고 owner만 바꾸거나 두 총괄을 동시에 실행하지 마. 정확한 이전 제작 child 종료와 현재 Unity 상태부터 확인하고 한 writer만 재개해. 평상시 Claude가 감독하고 Codex를 좁은 독립 작업에 활용하되 비 owner child에는 원본 Unity MCP·공유 Assets/meta 쓰기를 주지 말고 별도 후보를 owner가 검토/통합하게 해.

식별자 `PA_START_CURRENT_CHECKPOINT_20261019_V3`로 이번 실제 사용자 발송의 출처·시각·현재 approval/ticket/HEAD와 메시지 증거/SHA256를 기존 state의 `productionEnvironment.productionDispatch.resumeAuthorization`에 한 번만 기록해. 같은 식별자의 재발송으로 승인·예산·시도를 중복 추가하지 마. 파일 존재만으로 활성화하지 말고 현재 기록에 없는 lifetime 사용량을 초기화하지 마. 제작 helper/adapter는 현재 contract 정책과 원본 baseline·단일 writer·재개 증거를 함께 검사해야 한다. 이번 재개가 기존 user pause를 해제하는 범위는 위 승인 목표의 실제 단일 세션 재개에 한정하며 과거 pause/lease/history는 남겨. 재개 증거의 ticket/HEAD는 발송 당시 provenance다. 같은 승인 목표 안의 후속 작업마다 02 재발송이나 예산 추가를 요구하지 말고, 개별 실행 요청은 당시 최신 ticket/HEAD/dirty/lease로 다시 고정해.

한쪽 구독 quota/auth/네트워크가 막히면 가용 provider가 승인된 독립 지원을 이어가도록 해. 이 실행 묶음에서 **구현·검증된 생산 adapter를 통한 writer 인수인계는 승인**한다(`writerTransferAllowed=true`). 단, 정확한 이전 child/session 종료·실제 Unity 작업 없음·같은 dirty/baseline·입력 해제/save override·현재 정책/사용 기록을 관측하고 강제할 수 있을 때만 기존 owner 권위/MCP/lease를 일관되게 전환해. 하나라도 미확인이면 writer는 넘기지 마. 읽기 전용 지원 runner의 code0/TIMEOUT/raw429를 생산 완료나 구독 소진으로 판단하지 마. 유료 API로 자동 우회하지 마.

Golden·SmartphoneUI.cs·Save/KEEP 권위·Project_D·기존 dirty·다운로드/보호 파일 경계는 유지해. 대형 재작성·새 권위·외부 패키지/에셋·삭제·파괴적 Git·commit/push는 포함하지 않는다. 실제 HARD STOP·권위/저장/충돌·현재 유효한 범위/결제 제한·양쪽 사용 불가·모호한 실행·최종 사람 검수에서는 결과와 정확한 재개 근거를 보존해. 120초 복구 스냅샷과20분 안전 인계를 기존 helper/state로 유지하고 그 기록을 게임 PASS로 세지 마. 시간은 관측값이며 작업 총시간 제한이 아니다. helper/runner의 --timeout은 출력/소유 파일 활동이 끊긴 프로세스의 inactivity 검사용이다. 같은 행동을 진전 없이 반복하면 원인·바꾼 접근·새 근거를 기존 checkpoint/dispatch recovery에 남기고 복구해. 실행 상태가 모호하면 원인을 확인할 때까지 중복 실행하지 마.

보고는 짧게 ‘완료된 플레이 경험 / 실제 화면 증거 / 현재 blocker / 다음 자동 행동’으로 해. 지금까지의 valid 승인과 실제 checkpoint에서 최종 사람 검수까지 진행해줘.
