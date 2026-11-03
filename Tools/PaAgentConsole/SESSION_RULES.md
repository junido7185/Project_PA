# P.A. 두 에이전트 세션 규칙

평소 Claude가 감독하고 Codex가 독립 작업을 맡는다. 한쪽 quota가 소진되면 남은 쪽이 감독/실행을 맡을 수 있다. 실제 작업 권위는 기존 loop-state와 CODEX_HANDOFF이며 이 파일은 새 큐·승인이 아니다.

- 같은 조사·전체 레퍼런스 읽기를 두 에이전트에 반복시키지 않는다. 질문·관련 파일·기대 산출물을 좁혀 한 번 위임한다.
- 지원 작업 위임은 `provider_runner.py`의 `--primary codex` 또는 `--primary claude`를 사용한다. `--prepare`가 기본이며 `--run`은 구독 인증을 확인한 읽기 전용 작업만 실행한다. 사용법은 패키지 가이드 참조.
- UI 명령 `/pa`, `/pa-status`, `/pa-resume`는 모델 호출 없이 상태/재개 지시를 보여준다. Codex 사용률이 연결되지 않았으면 UNKNOWN을 유지한다.
- 평상시 감독자, 현재 가용 provider, Unity writer는 서로 다른 정보다. 비 owner child에는 Unity 연결을 주지 않는다.
- quota reset, 다른 모델, 다른 세션으로 compile/Play/validator 예산을 초기화하지 않는다. D2 당시 차단/사용 기록은 이력으로 보존하며 현재 실행 gate는 선택 contract의 최신 개정을 따른다.
- 실제 생산 전환은 기존 owner/approval/budget에 연결된 adapter가 있어야 한다. 현재 제공된 지원 runner를 생산 자동화 완료로 설명하지 않는다.
- 공유 Editor 변경은 단일 writer가 맡는다. 이전 실행이 종료됐는지, 명령이 반영됐는지 모호하면 상태를 먼저 확인한다.
- 키·credential 파일을 읽거나 출력하지 않는다. 두 구독이 소진돼도 별도 API 과금으로 자동 우회하지 않는다.
- 재개 시 원본 state·Handoff·최근 해당 run 관측만 읽는다. 마지막 자연어 요약에만 의존하거나 전체 transcript를 다시 로드하지 않는다.

2026-10-19 현장 연결: 현재 선택은 D7/Claude owner이며 D7 사용량은 미기록이다. D2의 INCOMPLETE_BLOCKED_BUDGET 및 4/4·5/5·1/1은 과거 결과로 보존한다. 패키지의 D2 예시로 현재 티켓/owner/예산을 덮어쓰지 않는다. 원본 Handoff가 가리키는 이전 제작 Handoff도 보존한다.

`production_adapter.py --status`는 조회만 한다. `--request <JSON>` 및 `--transfer-request <JSON>`도 기본은 준비/차단 증거만 남긴다. `--run`은 기존 loop-state의 `productionEnvironment.productionDispatch.resumeAuthorization`에 현재 사용자 제작 재개 메시지의 증거 경로/SHA256, enabled, approvalKey, ticket, baselineHead가 실제로 기록된 뒤만 가능하다. 환경 작업에서 이 레코드를 활성화하지 않는다. writer 전환은 그 메시지의 writerTransferAllowed=true도 요구한다.

요청은 `--status`의 현재 authority 중 approvalKey/ticket/baselineHead/workspaceSha256/stateSha256/handoffSha256/contractSha256를 그대로 고정하고 provider/sessionId/operation/params/runtimeObservation을 담는다. 현재 operation은 compile/play/stop/capture/build/검토된 worker integrate다. 모든 결과는 Logs/AgentConsole/production-*에 남는다. 기존 helper의 제작 진입은 이 adapter의 1회용 호출 허가가 필요하며, 읽기 전용 mcp catalog/상태/Console·조회는 계속 가능하다. 지원 child는 별도 읽기 전용 runner에서 결과/후보 patch를 Logs에 반환하고 원본 파일을 편집하지 않는다.

숫자가 없는 사용량은 0이 아니다. 현재 유효한 고정 상한이 적용되는 계약은 소진·미확인을 차단한다. 현재 선택된 APPROVED_GOAL_ADAPTIVE_PRODUCTION은 실제 contract 첫 줄 개정까지 확인된 경우에만 옛 perTicketBudget을 역사로 취급한다. 이 경우 미집계 lifetime 합계를0으로 만들거나 임의 상한3/3/1로 차단하지 않는다. compile/Play/신규 validator의 실제 dispatch 비용은 실행 전에 누적 ledger에 예약하고 실패·timeout에도 지우지 않는다. 알려진 총계는 누적하고, adaptive 계약의 미집계 lifetime 총계는 미집계로 남긴 채 새 시도 비용을 별도 ledger에 보존한다. timeout/종료 여부 미확인은 ledger를 in_flight로 남겨 자동 재실행/owner 이전을 막는다. 결과 code0은 검토 대기이며 게임 PASS가 아니다. 원본 state의 기존 잠금을 재사용하고 승인·티켓·누적 기록을 새 파일로 대체하지 않는다.

owner 전환은 정확한 이전 child/session 종료, 기존 Editor PID·project·Edit/compile/in-flight·scene dirty·입력 해제·save override 상태의 60초 이내 관측, 같은 HEAD/dirty hash/승인/티켓과 알려진 잔여 예산이 모두 필요하다. 조건이 충족된 명시적 `--transfer-request --run`만 관련 MCP 키를 백업/전환하고 기존 lease를 이력에 보존한다. 자동 추측 전환은 없다. 전환 후 실제 새 세션 lease와 MCP를 다시 확인해야 하며 자동 제작 시작을 의미하지 않는다.

현재 로컬 Mods는 project scope로 영구 등록되어 Start-ProjectPA.ps1이 중복 --plugin-dir를 생략한다. 큰 state는 고정 Python의 읽기 전용 --mod-state 요약으로 표시한다. 화면 명령/갱신에는 LLM 호출이 없다. 최초 제공 패키지의 설치기와 이후 현장 수정은 구분하며 재설치 충돌 시 현장 파일을 덮어쓰지 않는다. 최신 현장 결과: `Logs/AgentConsole/setup/environment-20261019/ENVIRONMENT_RESULT.md`.

기존 lead/격리 worker 시작 경로는 제거하지 않았다. start/worker/supervise도 같은 사용자 재개 증거·현재 승인·HEAD·owner·현재 계약의 예산 정책 검사를 거친다. lead 시작은 정확한 이전 PID 종료, worker supervisor는 등록된 정확한 PID를 확인하며 headless/worker의 timeout은 출력/소유 파일 활동이 끊긴 inactivity 감지이며 총45분 상한은 폐기했다. 20분 안전 인계는 같은 승인 목표를 이어가는 복구 주기다. 격리 critic/verifier는 소진 시에도 기존의 읽기 전용/evidence 범위를 지킨다. 공유 Unity 변경 자체는 adapter를 사용한다.

기존 제작 Mods의 상세 상태 명령은 /pa-lead-status로 옮겨 새 영구 패널의 /pa-status와 중복 등록되지 않게 했다. 기존 /pa-usage·pause·usage/checkpoint 동작은 보존한다.

2026-10-19 후속 상태 감사: 설치 전 백업과 현재 state는 바이트 동일하다. D7/Claude owner는 기존 값이며, UNKNOWN_BUDGET을 현재 차단으로 사용한 설치 코드가 contract 첫 줄의 adaptive 개정을 놓친 오류였다. 해당 판정/화면만 정정했고 승인·ticket·owner·history·사용량은 수정하지 않았다. 미집계 숫자는 미집계로 남는다. 옛 ticketStatus 행은 실제 구현 진척이 아니며, timestamped checkpoint와 보존된 제작 Handoff를 대조한다. 최신 TIMEOUT 표시는 읽기 전용 환경 지원 결과일 뿐 gameplay blocker/quota 소진이 아니다.

2026-10-19 운영 정리에서 startup/prompts/02_PRODUCTION_START.md와 RECONCILED 호환 사본 및 다운로드 폴더02를 같은 최신 문안으로 갱신했다. production-prompt 복사 명령은 정식02를 선택한다. 이전 원문은 운영 백업에 보존한다. 새 문안은 읽기/복사만으로 활성화되지 않고 기존 D2 숫자를 재설정하거나 추가하지 않는다. 이번 사용자 감사 요청은 제작 재개 승인이 아니다.

현재 적용 정책은 loop-policy.json의 APPROVED_GOAL_PROGRESS_RECOVERY_20261019다. 이전45분 상한/6턴 지원 제한/기계적 실패 횟수/옛 티켓·모델 고정은 폐기됐다. 승인 범위의 세부 설계/구현/복구/검증은 단계별 사람 재승인 없이 진행한다. resumeAuthorization.ticket은 최초 재개 provenance이며 같은 승인 목표의 후속 approvedTickets에도 유효하다. 개별 요청은 최신 authority/dirty/lease를 고정한다. 이전 실패와 동일 workspace에서 재시도할 때는 recovery.cause/changedApproach/evidence에 실제 새 근거 파일을 지정한다. 이것은 진단 checkpoint 요구이며 새 사용자 승인이 아니다. unity operation의 params.toolCall은 기존 구조화 Editor 제작 도구를 단일 owner로 실행한다. 임의 shell/C#·삭제·패키지 도입은 이 경로에 추가하지 않는다. 이미 dirty인 scene의 추가 구조화 편집에는 실제 scenePath/sceneDirtyOwnerSessionId와 params.preserveDirtyScene=true가 필요하며 Play/build/integration 전에 보존한다. 180초 RPC 응답 deadline은 모호한 호출을 재생하지 않는 전송 안전장치이며 작업 전체의 시간 예산이 아니다. 실제 모델/권한/구독·관리자 제한은 관측대로 따르고 유료 우회하지 않는다. 환경·운영 수정은 제작 재개나 owner 이전을 활성화하지 않는다.

2026-10-19 사용량 재개 연결 감사: `-p` 자율 lead는 서버 reset 시각(rate_limit_event/Mods 관측)+60초까지 로컬 타이머로 기다리며 LLM으로 회복을 확인하지 않는다. 대기 예약은 `autonomous.resumeReservation`에 남아 supervisor 재시작 시에도 모델 호출 없이 이어진다. pause는 예약을 취소한다. 매 재실행 전 `resume_gate_errors`가 현재 승인·02 resumeAuthorization·baseline·owner/lease generation·in_flight ledger·이전 child 종료·직전 cycle 복구 checkpoint를 다시 확인하고, 하나라도 어긋나면 `paused_resume_gate_blocked`로 멈춘다. 사용량 회복은 승인·사용 기록 초기화가 아니다. lead·worker·지원 runner에서 `isUsingOverage=true`(유료 초과 사용)가 관측되면 즉시 child를 멈추고 plan 소진으로 처리한다. 지원 runner는 두 provider가 모두 소진되면 `quotaWait`에 검증된 reset만 기록하며 자동 재시도하지 않는다. 같은 날 사용자 승인으로 entry guard는 같은 generation 토큰·자기/부모 PID인 자율 launcher 자신의 `--foreground` child만 통과시킨다. 대기 진입 시 작업 스케줄러 `\ProjectPA\UsageResume`(reset+120초, StartWhenAvailable)을 1회 등록해 재부팅·supervisor 종료 후에도 `autonomy resume-due`가 승인·pause·owner/lease·생존 프로세스·in_flight를 다시 확인한 뒤에만 supervisor 하나를 띄운다. 막히면 모델을 호출하지 않으며 in-process 재개·pause 시 작업은 해제된다. 대화형 CLI의 native `autoContinueAtUsageLimit`은 project-local 값이 실제 적용되는지 미검증이다.
