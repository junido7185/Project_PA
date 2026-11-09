# Project P.A. Claude 자동 제작

프로젝트 루트에서 한 번 실행한다. 같은 총괄 세션을 인수하고 기존 Unity와 격리 작업자 경로를 사용한다.

```powershell
pwsh -NoProfile -File .\Tools\LoopEngineering\Start-ProjectPAClaudeDemo.ps1
```

상태와 안전 중지:

```powershell
pwsh -NoProfile -File .\Tools\LoopEngineering\Start-ProjectPAClaudeDemo.ps1 -Watch
pwsh -NoProfile -File .\Tools\LoopEngineering\Start-ProjectPAClaudeDemo.ps1 -Status
pwsh -NoProfile -File .\Tools\LoopEngineering\Start-ProjectPAClaudeDemo.ps1 -Pause
```

`-Watch`는 5초마다 총괄·현재 배치 작업자·최근 도구 동작·실제 사용량·한국 시간 자동 재개 시각을 한 화면에 표시한다. Ctrl+C는 조회만 닫으며 제작은 유지한다. 한 번만 확인하려면 `-Watch -Once`. 상세 읽기는 `-Status`, 작업자 원문/결과는 화면의 Logs 경로에서 확인한다. Watch는 기존 상태/세션 로그만 읽으며 새 Claude나 Unity를 실행하지 않는다.

Claude 대화에 이 Mods가 로드돼 있으면 `/pa-status`, `/pa-pause`도 가능하다. Pause는 다음 자동 재개를 즉시 금지하고 현재 원자 작업을 마친 뒤 인계를 남기게 한다. Unity나 작업자를 강제 종료하지 않는다. 진행 중인 생산 CLI가 있으면 새 런처는 중복 소유자를 만들지 않는다. 감독 프로세스를 강제 종료하지 말고 Pause 후 상태를 확인한다. legacy 수동 대화는 `-Interactive`, 읽기 확인은 `-CheckOnly`다.

Claude가 제작 총괄·원본 Unity 통합을 맡고, Codex는 격리 구현·독립 검토·환경 지원을 맡는다. 최신 사용자 목표는 데모 기한 내 품질을 높이는 릴레이다. Claude 검증 → Codex의 제한된 구현/비평 → Claude 통합·실제 Unity 검증 → 다음 제작을 이어가며, 같은 작업을 두 팀에 중복 배정하지 않는다. builder/critic/reviser/verifier는 기존 helper가 현재 HEAD와 dirty snapshot에서 만든 sparse worktree에서 지정 파일만 작업한다. 파일 예약·원본 해시 검토를 유지한다. 역할 4개가 상시 프로세스 4개를 뜻하지 않는다. 먼저 독립 작업 2개, 통합 후 독립 비평→수정→검증 순으로 사용량과 진전에 맞춰 배정한다. Claude native Agent Teams는 in-process MCP 상속이 Unity 격리에 맞지 않아 활성화하지 않는다.

Claude CLI 작업자는 strict empty MCP/hooks off다. Codex는 공식 App Server의 실제 config/read로 상속 서버를 열거하고 서버별 enabled=false와 plugins/apps/hooks/multi_agent=false를 확인한다. 빈 mcp_servers={}만으로 상속이 제거된다고 가정하지 않는다. worker 기본 --provider auto는 실제 여유와 capability가 확인된 팀에만 새 단위를 배정한다. 이전 exec CLI의 실제 read-only 거절과 사용량은 그대로 보존했다. 새로운 App Server의 실제 workspace-write/네트워크 off 및 격리 marker 생성·읽기, 실제 gpt-6.1-sol/medium 독립 critic 실행이 성공했다. Codex의 원본 Unity capability는 검증하지 않았으므로 원본 소유권은 Claude에 유지한다.

현재 후보 결과는 해당 작업 폴더의 `HANDOFF.md`, `INDEPENDENT_REVIEW.md`, `native-result.json`, `review.patch`와 `Validation/`에 있다. untracked/dirty 원본도 `source-baseline` 바이트와 비교한다. Claude는 정확한 SHA와 원본 해시/HEAD를 확인한 뒤 기존 helper `review --id World-dune-relay-codex-r2-20261011 --integrate --reviewed`로 백업·통합한다. `-Watch`에 두 제공자의 작업이 함께 표시된다. 현재 Codex 후보 이후의 무인 Codex CLI 지속 제작은 위 제한이 해소되기 전까지 검증되지 않았으며, Claude의 기존 자동 제작은 유지한다.

`/goal`은 승인된 품질 제작을 이어가며 최종 완료 조건 또는 실제 blocker/90분 안전 checkpoint를 평가한다. 모델 판정만으로 게임 완료를 표시하지 않는다. 최신 v9b는 PARTIAL/YES-WITH-LIMITS다. 다음은 Codex의 군락 보존·해안 예산 배분 후보를 실제 Unity에서 검증하고, 약한 흙/풀 대비·coast kerb/rim·원거리 밀도를 개선하는 것이다. 돌아온 canopy와 추가된 foam을 반복 구현하지 않는다. 정확한 인계는 `Docs/00_CURRENT/CODEX_HANDOFF.md`다. 월드→동작→소품→UI→전체 조화→Windows 후보·실제 플레이·격리 Save/quit/fresh Continue·성능·최종 사람 검수 순서를 유지한다. 셀/저장/기존 권위/개인 데이터와 거절·HOLD·옛 사용량을 보존한다. 설치된 딩컴은 실행하지 않는다.

Mods는 `session.usage()`의 실제 한도/회복 시각·context·CLI 버전을 모델 호출 없이 기존 helper에 전달하고 상태/안전 중지 명령만 추가한다. 네트워크·인증·권한 승인·자동 모델 전환·Unity 입력은 Mods에 없다. 공유 기록은 기존 `loop-state.json`의 `productionEnvironment.autonomous` 한 곳에 쓰며 helper lock과 고유 임시 파일을 사용한다. 기존 무관 플러그인은 계속 비활성, 제작 세션은 project/local 설정만 읽는다.

감독은 실제 `usage_limit_reached`/rejected API 사건과 현재 스트림의 미래 reset timestamp만 사용한다. 5시간을 추정하지 않는다. 일일·주간 차단 중 가장 늦은 실제 reset +60초에 같은 session ID를 재개한다. reset이 미확인/지난 시각이거나 인증·모델·계정·도구 오류이면 보존 중단한다. 계정·모델 대체와 추가 과금은 하지 않는다. 정상 종료 후에도 다음 checkpoint와 실제 변경/새 검증 증거가 있어야 이어가며 진전 없는 반복은 멈춘다. 최종 사람 검수에서는 자동 완료 승인을 만들지 않는다.

PC/감독 프로세스가 켜져 있어야 기다릴 수 있다. 절전 후에는 시각을 다시 확인하지만 재부팅 후 자동 실행을 설치하지 않았다. 실행 명령으로 재개한다. 잠긴 Windows에서는 실제 입력을 금지하며 화면 검증을 해제 후 수행한다. 사용자의 별도 Claude 대화는 보존하고, 다른 세션이 작업 중이면 감독은 충돌을 피하도록 중단한다. 허용되지 않은 계정·관리자 정책은 우회하지 않는다.

실제 세션/작업자/모델/오류는 기존 `Logs/ParallelProduction/ClaudeLead-...`, worker 결과와 중앙 상태에 남는다. 감독 로그는 `Logs/ParallelProduction/Autonomous-20261011/supervisor.log`, 실제 Mods 로딩 증거는 `mod-live-probe.jsonl`이다. 한도 회복 장시간 실측 여부는 별도로 보고한다. 리스트 가격 기준 API cost 표시를 실제 구독 청구액으로 해석하지 않는다.

## 조사와 검증

- [공식 Mods 문서](https://code.claude.com/docs/en/plugins/mods/overview), [API](https://code.claude.com/docs/en/plugins/mods/api), [타입](https://github.com/anthropics/claude-code/blob/main/mods/types/claude-code.d.ts), [공식 예제](https://github.com/anthropics/claude-code-playground/tree/main/claude-code/mods)를 실제 읽었다. token-weather의 context 관찰, replay-theater의 편집 확인, blast-radius의 도구 전 이벤트 사례를 비교했다. 필요한 관찰/명령만 자체 소형 모듈로 구현했다.
- [공식 `/goal`](https://code.claude.com/docs/en/goal)은 headless를 지원한다. [기본 자동 한도 재개](https://code.claude.com/docs/en/interactive-mode#wait-for-a-usage-limit-to-reset)는 열린 interactive 구독 세션 한정이며 주간/절전/종료/반복 한도 제한이 있어 기존 helper 감독으로 보완했다.
- [공식 Agent Teams](https://code.claude.com/docs/en/agent-teams) 대신 검증된 외부 격리 작업자 경로를 유지한다. [터미널 키 입력 재개 사례](https://github.com/Muminur/auto-claude-resume-after-limit-reset)는 Unity 입력 소유권·Windows 포커스·별도 상태 문제 때문에 채택하지 않았다.
- 설치 CLI 2.1.296, Mods min2.1.287. 실제 CLI module validate/test와 model-free `/pa-status` 로딩을 검증한다. Python 감독 회귀 검증: `python -B -X utf8 Tools/LoopEngineering/tests/test_autonomy.py -v`.

## 복원

설정·런처·문서·상태 원본과 SHA256는 `Logs/ParallelProduction/Autonomous-20261011/backup-before/manifest.json`에 있다. 먼저 Pause→상태 stopped 확인→활성 작업자 종료/결과 보존 확인 후 **되돌릴 설정·도구 파일만** `backup-before`의 같은 상대 경로에서 복원한다. 현재 파일도 먼저 별도 백업한다. `.claude/settings*.json`을 복원하면 원래 hooks-off 설정으로 돌아간다. 기존 게임/Save/폰트/최신 loop-state를 옛 사본으로 일괄 덮지 않는다. 중앙 상태는 현재 이력을 보존하고 autonomous.enabled=false/stopRequested=true만 유지한다. 세션과 worktree 결과는 삭제하지 않는다. 기존 설정 복원 지침도 `Logs/ParallelProduction/20261009-ClaudeLead/RESTORE.md`에 있다.

이번 릴레이 변경 전 helper·문서·상태 8파일 백업은 `Logs/ParallelProduction/Competition-20261011/backup-before/manifest.json`이다. 폴더명은 최초 조사 당시 이름이며 경쟁 상태 체계/중복 런처는 만들지 않았다. 위 안전 중지 절차 후 필요한 도구·설정 파일만 선택 복원한다. 오래된 handoff/state를 최신 제작 이력에 덮지 않는다. 원본 게임 변경은 이번 Codex 작업에서 통합하지 않았고 Claude의 향후 통합 백업은 해당 작업의 `integration-backup`에 남는다.


자동 릴레이: 기존 supervisor가 실행·quota 대기 중에도 모델 없는 실제 Codex 계정 한도 조회와 relay tick을 수행한다. 90% usage 또는 85% context에서는 현재 원자 작업 후 인계하고, 관측이 없어도 120초 owned-source snapshot/20분 안전 checkpoint를 사용한다. snapshot은 content-addressed 사본과 HEAD/hash/evidence/다음 행동이며 validation PASS가 아니다. handoff_pending인 안전한 부분 작업만 같은 worktree에서 재개하며 이전 attempt/SID/usage/error/log를 유지한다. 완료 후보는 다시 수행하지 않는다. 소유권/원본 hash/HEAD/immutable baseline/attempt token/프로세스 중복/최종 사람 검수/사용자 Pause를 검사한다. 둘 다 제한일 때 회복 시각은 후보일 뿐이고 실제 availability를 다시 확인한다. Claude는 실제 quota 사건의 reset+60초 후 기존 총괄 세션을 재시도하며, context 안전 인계 뒤에는 과거 SID를 보존한 새 세션을 사용한다. 인증·모델·sandbox 오류를 quota로 분류하지 않는다.

실제 증거와 미검증 경계: `Logs/ParallelProduction/AutoRelay-20261011/EVIDENCE.md`. 실제 한도 조회·write probe·독립 critic·source snapshot·idle supervisor reload는 완료했다. 양방향 계정 제한 전환/회복 이후 실제 제작 재개는 아직 관찰하지 않았고 테스트 fixtures로만 검증했다. 현재 게임 후보의 원본 통합·Unity 검증은 Claude의 다음 작업이다.

이번 변경 전 10파일 백업은 `Logs/ParallelProduction/AutoRelay-20261011/backup-before/manifest.json`이다. 먼저 -Pause 후 총괄/작업자 안전 종료를 확인하고 필요한 도구·설정 파일만 선택 복원한다. 최신 loop-state/게임/Save를 옛 사본으로 일괄 덮지 않는다. 새 bridge는 기존 런처의 내부 모듈이며 별도 런처/계정/상태를 만들지 않았다.
