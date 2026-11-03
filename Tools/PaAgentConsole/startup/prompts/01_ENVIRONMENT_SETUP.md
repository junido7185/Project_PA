# 지금 이 PC에서 개발 환경 연결을 끝내는 지시

내 목표는 Project P.A. 완성 게임을 AI 중심으로 계속 제작하는 것이다. 게임 범위나 품질 목표를 줄이지 마. 지금은 이 Windows PC에서 준비 패키지를 실제 연결하고 Claude 확장 문제를 고쳐 제작을 시작할 상태로 만들어줘. 설명만 남기거나 설치 계획만 제시하지 말고 접근 가능한 작업을 끝내줘.

## 1. 실제 권위와 설치 확인

허용 root C:\Users\sdjsd\Desktop\Unity\Project_PA의 AGENTS.md, Docs/00_CURRENT의 CURRENT_STATE/CAPABILITY_REGISTRY/GAME_LOOP_MAP/INTEGRATION_QUEUE, CODEX_HANDOFF.md를 읽고 실제 HEAD/branch/dirty를 확인해. master는 portfolio다. 준비 자료는 Tools/PaAgentConsole/startup과 Tools/PaAgentConsole/SESSION_RULES.md에 있다. 기존 setup journal과 진단 도구부터 사용하고 같은 조사·모든 문서 읽기를 반복하지 마.

패키지 설치는 소스/설정 준비이며 Claude·Codex 실제 인증, VS Code 오류 수정, Mods 실화면, Unity 생산 인계까지 통과했다는 뜻이 아니다. 현재 state가 이 자료보다 최신이면 실제 권위를 우선해. 한 번 읽은 AGENTS/skill은 내용이 바뀌지 않으면 재로딩하지 마.

## 2. Claude CLI와 VS Code 문제 수정

pa_console.py --diagnose, 실제 claude --version/doctor/auth status, codex --version/login status에서 필요한 정보만 확인해. credential 파일/키/토큰/환경변수 값은 열거나 출력하지 마. CLI와 확장은 내장 CLI 버전, 실제 host(Windows/WSL/remote), 사용자·프로젝트·local settings, VS Code profile, working directory, 실효 MCP가 다를 수 있다. 실제 로그의 원인 항목을 근거로 좁혀 고쳐. API 환경변수는 값 대신 존재 여부만 관찰하고 구독 계정으로 사용해.

확장 로그를 도구로 볼 수 없을 때만 나에게 Ctrl+Shift+P → Claude Code: Show Logs에서 관련 오류 줄을 요청해. 정상 Codex 패널은 보존해. 설정 변경은 관련 키만 백업하고 수정해. 버전상 해결된 오류라면 해당 공식 안내와 설치 방식에 맞게 필요한 구성요소만 갱신해. 로그인은 내가 직접 하게 해줘. WSL로 옮기거나 설정을 통째로 초기화하지 마.

## 3. 작업 화면 실제 연결

Tools/PaAgentConsole/pa-production-mod를 실제 설치된 Claude 버전의 claude plugin validate와 생성 타입에 맞춰 연결해. /pa, /pa-status, /pa-resume를 실제 CLI 화면에서 확인하고 지원되는 VS Code 확장·feature flag가 있으면 패널에도 연결해. 동봉 local marketplace(.claude-plugin/marketplace.json)는 이 도구 폴더를 가리킨다. 영구 등록/설치는 공식 plugin 명령의 scope를 확인하고 프로젝트에 한정해 적용해. --plugin-dir는 해당 세션에만 적용된다는 점을 구분해. 영구 설치와 --plugin-dir를 중복으로 로드하지 마.

화면에는 실제 active provider·현재 Unity writer·현재 ticket/gate/사용 budget·최근 완료 증거·다음 행동·관찰된 quota/reset/관찰 시각을 표시해. 없는 값은 N/A 또는 UNKNOWN으로 남겨. 작업 티켓 비율을 전체 게임 완성률로 표시하지 마. UI 조회/갱신 자체는 LLM 호출 없이 유지해. Mods가 막히면 백업된 statusLine을 이용해 CLI에서 작업을 이어가며 원인을 기록해. 설치된 statusLine이 실제 렌더되는지도 확인해.

## 4. 두 구독으로 제작을 이어갈 연결 구현

평상시는 Claude가 감독하고 Codex를 좁은 독립 작업에 위임해. 현재 provider_runner.py는 지원용 읽기 전용이다. 이를 게임 제작 자동 failover로 보고하지 마. 실제 생산 연결은 기존 loop-state의 selectedApproval/contract/activeTicket/owner/누적 budget과 현재 Handoff를 읽도록 구현해. 기존 doctor가 숫자 예산을 강제하지 않는 부분을 포함해 실제 집행 guard를 연결해. 자동화 도구·운영 연결 코드 작성은 이번 환경 준비 범위 안에서 수행하되 게임 코드·씬·Assets 및 현재 업무 승인 상태는 변경하지 마.

각 생산 dispatch 전에 유효한 승인, 누적 사용량/상한, 같은 티켓, 정확한 baseline, 단일 writer를 검사하도록 해. 구조화된 결과·실제 증거·세션 ID·정확한 다음 행동을 Logs/AgentConsole에 남기되 비밀은 기록하지 마. code0/자연어 '완료'만으로 성공 판정하지 마. 비 owner child에는 Unity MCP와 공유 Assets/meta 쓰기 권한을 주지 말고, 별도 scratch 후보를 생성해 owner가 통합하게 해. 기존 도구 계약에 맞는 finite run을 사용하고 무한 재귀 위임을 금지해.

한쪽 quota/auth/네트워크가 막히면 남은 provider가 감독·독립 지원을 이어가게 해. 게임 writer 전환은 02_PRODUCTION_START.md를 내가 보낸 후에만 그 승인 범위와 실효 guard를 적용해. 이전 정확한 child가 종료됐는지·Unity in-flight·dirty·입력/save 상태를 관찰할 수 없으면 writer는 넘기지 마. 모호한 Unity 명령은 재실행하지 마. 다른 provider/새 세션/quota reset으로 업무 budget을 초기화하지 마. 지원 runner의 raw429는 구독 소진으로 단정하지 마. 둘 다 소진되면 실제 재개 근거를 남기고 대기해. 별도 유료 API로 우회하지 마.

## 5. 준비 완료 판정

현재 D2는 알려진 원본 기준 INCOMPLETE_BLOCKED_BUDGET, Codex owner, compile4/4·Play5/5·신규 validator1/1이다. 상태가 최신화되지 않았다면 이 차단과 누적 수치를 보존해. 환경 준비만으로 production approval을 활성화하거나 새 Editor 시작·compile/Play/build·게임 변경을 하지 마. 이미 열린 Editor의 읽기 전용 연결·신원·진행 상태 조회는 허용한다. 게임 작업 재개는 사용자가 다음 지시문을 발송한 뒤다.

실제 동작이 필요한 부분만 최소 검증하고 별도 자율성 시연 프로젝트·반복 벤치마크로 사용량을 낭비하지 마. 환경 관련 변경 기록은 기존 월별 이력에 한 번 남기고, 실제로 달라진 사실만 현재 문서에 반영해. 활성 게임 Handoff를 환경 보고서로 덮어쓰지 마. 운영 작업에만 SESSION_RULES.md를 읽도록 기존 AGENTS/CLAUDE에 짧은 조건부 포인터를 통합해. commit/push는 하지 마.

마지막에 ① 구성된 파일 ② 실제 로그인/CLI/패널/Mods/MCP 연결 관찰 결과 ③ 생산 adapter와 누적 budget 집행 구현 여부 ④ 아직 내가 해야 할 로그인/화면 확인 하나를 구분해서 짧게 보고해. 구현했으나 실제 관찰하지 못한 부분은 '현장 미확인'으로 표시해. 준비가 끝나면 나에게 02_PRODUCTION_START.md를 읽고 발송할 수 있다고 알려줘.
