# Project P.A. 처음 시작하기

작성 기준: 2026-10-19 한국 시간. **이 ZIP 하나로 시작한다. 이전 보고서·ZIP을 차례대로 설치할 필요가 없다.**

준비물은 설치기·진단/작업 화면 소스·레퍼런스 적용 패치·실제 PC 작업 지시문이다. 다운로드만으로 Windows 설정이 바뀌거나 게임 제작이 시작되지는 않는다.

## 1. 추천: 정상인 Codex 확장으로 준비를 맡긴다

1. ZIP을 **프로젝트 밖** 폴더에 압축 해제한다. 예: `C:\Users\sdjsd\Downloads\Project_PA_Start_Kit_2026-10-19`. 안에 `Setup-ProjectPA.ps1`, `setup_pa.py`, 이 문서가 보여야 한다. ZIP 안에서 실행하지 않는다.
2. 다른 Claude/Codex 작업이 진행 중이면 새 도구 호출을 멈추고 끝나게 한다. 기존 변경 파일을 지우거나 초기화하지 않는다.
3. VS Code **파일 → 폴더 열기**로 `C:\Users\sdjsd\Desktop\Unity\Project_PA`를 연다. Codex는 정상 작동하던 확장 패널을 그대로 쓴다. Unity를 새로 열 필요는 없다.
4. **파일 → 파일 열기**로 압축 푼 폴더의 `START_HERE_KO.md`를 연다. 작업 폴더 자체를 다운로드 폴더로 바꾸지 않는다.
5. 아래 문장을 Codex 확장 대화창에 붙여넣고 보낸다. AI가 파일을 못 찾으면 압축 푼 폴더 경로 하나만 알려준다.

```text
지금 에디터에 연 Project P.A. 시작 가이드가 있는 폴더의 prompts/00_RESCUE.md와 START_HERE_KO.md를 읽고, 이 Windows PC에서 설치와 prompts/01_ENVIRONMENT_SETUP.md의 환경 연결 작업을 끝내줘. 계획만 보고하지 말고 실제 적용해줘. 기존 dirty와 설정을 보존하고, 게임 제작은 02_PRODUCTION_START.md를 내가 보낸 뒤 시작해. 패키지를 못 찾으면 압축 푼 폴더 경로 하나만 물어봐.
```

6. 브라우저 로그인이 필요하면 본인이 로그인한다. Claude는 기존 Pro, Codex는 Plus가 있는 ChatGPT 계정을 선택한다. 비밀번호·키·토큰을 대화에 붙여넣지 않는다.
7. AI가 실제 환경 준비 결과를 보고하면 아래 **6번 제작 시작**으로 간다.

**이 경로에서는 다음 수동 설치 명령을 직접 실행할 필요가 없다.** 직접 설치하고 싶을 때만 2~5번을 사용한다. 두 경로를 동시에 진행하지 않는다.

## 2. 직접 설치: PowerShell 명령 두 번

VS Code **터미널 → 새 터미널**을 누른다. `PS C:\...>`이면 PowerShell이다. `>>>`는 Python 창, Claude/Codex 입력창은 AI 대화이므로 일반 터미널을 새로 연다.

예시 경로에 압축을 풀었다면 다음을 한 줄씩 실행한다. 다른 곳이면 첫 줄의 경로만 바꾼다. `$`나 `PS C:\>` 표시는 입력하지 않는다.

```powershell
Set-Location 'C:\Users\sdjsd\Downloads\Project_PA_Start_Kit_2026-10-19'
powershell -NoProfile -ExecutionPolicy Bypass -File .\Setup-ProjectPA.ps1
```

기본 실행은 **조회만 한다**. Python exe, 게임 브랜치/HEAD, 기존 변경, 선택된 승인/티켓/owner, 레퍼런스 상태, 설치 대상 수가 출력된다. 마지막에 `CHECK_OK`면 다음을 실행한다.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Setup-ProjectPA.ps1 -Apply
```

`SETUP_OK`면 로컬 파일 준비가 끝났다. `ExecutionPolicy Bypass`는 이번 호출에만 적용하며 컴퓨터 전체 정책을 바꾸지 않는다.

| 항목 | 설치기의 처리 |
|---|---|
| Python | Store 별칭을 피하고 실제 3.11+ exe 확인 |
| 프로젝트 | 허용 경로, Git origin, gameplay 이력, 필수 파일 확인 |
| 레퍼런스 | 문서 16개에 13개 설계 결정과 진입 연결; 기존 적용 반복 금지 |
| 도구 | `Tools/PaAgentConsole`에 진단/상태/지원 실행기/Mods/시작 지시 추가 |
| Claude 상태줄 | local 설정 백업 후 `statusLine`만 설정 |
| 업무 상태 | loop-state/Handoff/누적 예산/owner 보존 확인 |
| 기록 | `Logs/AgentConsole/setup/<실행 ID>/before.json`, `result.json` |

branch 변경, pull/reset/clean/stash, commit/push, 로그인, 외부 프로그램 설치, Unity 실행, compile/Play, D2 재개는 설치기에 포함되지 않는다.

`SETUP_BLOCKED`, `LOCAL_MERGE_REQUIRED`이면 반복 실행하거나 파일을 지우지 말고 1번 방법으로 Codex에 맡긴다. 설치 도중 실패하면 일부 도구가 복사됐을 수 있으며 journal이 진행 지점을 보여준다. 같은 파일은 재설치하지 않고 다른 파일은 덮어쓰지 않는다.

## 3. 프로그램이 없을 때

정상 작동하는 프로그램은 다시 설치하지 않는다. AI에게 오류를 맡겨도 된다.

| 프로그램 | 확인 | 없을 때 |
|---|---|---|
| Git for Windows | `git --version` | [공식 설치](https://git-scm.com/download/win) |
| Python 3.11+ | 설치기가 실제 exe 자동 확인 | [Windows 공식 다운로드](https://www.python.org/downloads/windows/)에서 3.12 이상 안정 버전 설치 후 새 터미널 |
| Claude CLI | `claude --version` | 아래 공식 설치 명령 |
| Codex CLI | `codex --version` | 아래 공식 설치 명령; 확장과 CLI 설치는 별개 |
| VS Code | 현재 작업 환경 사용 | 기존 Windows host/profile 유지 |
| Unity | `ProjectVersion.txt` | 7번에 따라 기존 버전 사용 |

Claude CLI가 없을 때 PowerShell:

```powershell
irm https://claude.ai/install.ps1 | iex
```

Codex CLI가 없을 때 PowerShell:

```powershell
irm https://chatgpt.com/codex/install.ps1 | iex
```

공식 설치는 네트워크 다운로드를 수행한다. 패키지 설치기는 자동 실행하지 않는다. 설치 후 **새 PowerShell 터미널**에서 버전을 확인한다. 실패하면 관련 오류만 Codex에 알려준다. 관리자 실행·글로벌 정책 변경·여러 설치 방식 중복 사용으로 우회하지 않는다.

Python이 설치됐는데 자동 탐색만 실패하면 실제 exe를 지정한다. 아래 경로에 파일이 있을 때만 사용한다. 성공하면 끝에 `-Apply`를 붙인다.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Setup-ProjectPA.ps1 -PythonExecutable 'C:\Users\sdjsd\AppData\Local\Programs\Python\Python312\python.exe'
```

## 4. 구독 로그인

새 PowerShell에서:

```powershell
Set-Location 'C:\Users\sdjsd\Desktop\Unity\Project_PA'
claude auth status
codex login status
```

필요하면 `claude`를 실행해 기존 Claude 구독 계정으로 로그인한다. Codex는 `codex login` 후 **Sign in with ChatGPT**를 선택한다. 처음에는 기존 Windows 작업 host를 유지한다. Windows/WSL 사이를 옮기면 설정·인증 경로가 달라질 수 있다.

이 구독 경로에 API key나 API 결제 추가는 필요하지 않다. 진단에 API 환경변수 존재가 보이면 AI가 충돌 항목만 확인하도록 한다. 인증 파일·전체 환경변수·settings-backups 폴더를 첨부하지 않는다.

## 5. 실제 환경 연결과 작업 화면

설치 성공 후 PowerShell에서:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File 'C:\Users\sdjsd\Desktop\Unity\Project_PA\Tools\PaAgentConsole\Start-ProjectPA.ps1' -Provider claude
```

환경 연결 지시문이 클립보드에 복사되고 프로젝트에서 Claude가 열린다. Claude 입력창에서 **Ctrl+V → Enter**를 누른다. 이번 세션에만 `--plugin-dir`로 Mods를 연결한다. 게임 작업은 자동 발송되지 않는다.

Claude도 안 되면 정상 Codex 확장에 `Tools/PaAgentConsole/startup/prompts/01_ENVIRONMENT_SETUP.md를 읽고 이 PC에서 실행해줘`라고 보낸다. plugin 로딩 오류가 나면 시작 명령 끝에 `-NoMod`를 붙여 Claude를 먼저 열고 지시를 보낸다.

환경 AI는 CLI/확장의 실제 오류 원인 수정, 상태줄 표시, Mods 검증·화면 확인, 좁은 위임 연결, **생산 adapter와 누적 budget 집행 연결**을 마무리한다. 외부에서 현재 PC의 로그·화면을 볼 수 없었으므로 이미 고쳐졌다고 전제하지 않는다.

Mods가 로드되면 **Claude 대화창**에 `/pa`, `/pa-status`, `/pa-resume`를 입력한다. PowerShell 명령이 아니다. 작업 패널·현재 상태·재개 포인터를 표시하며 조회 자체는 LLM을 호출하지 않는다.

공식 안내 기준 CLI Mods는 2.1.287+, VS Code 렌더는 확장 2.1.299+와 feature flag가 필요하다. 재개 때 패널이 계속 죽는 문제는 2.1.301+ 안내를 확인한다. CLI와 확장 업데이트는 별도이며 버전 충족만으로 이번 코드의 실화면이 검증된 것은 아니다.

영구 설치용 로컬 marketplace도 포함했다. AI가 실제 CLI validate와 공식 scope를 확인해 적용한다. 영구 설치 후에는 시작 명령에 `-NoMod`를 붙여 `--plugin-dir`와 중복 로딩하지 않는다.

## 6. 제작 시작: 02번을 읽고 한 번 발송

환경 준비 결과를 확인하고 `Tools/PaAgentConsole/startup/prompts/02_PRODUCTION_START.md`를 읽는다. 이 문안은 **D2 compile/fix 3회·Play 3회 추가 승인, 신규 validator 추가0개**다. 알려진 원본 기준 누적 상한은7/8/1이다. 전체 게임 범위는 그대로 유지한다.

복사용 PowerShell 명령:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File 'C:\Users\sdjsd\Desktop\Unity\Project_PA\Tools\PaAgentConsole\Start-ProjectPA.ps1' -Provider production-prompt
```

**복사는 승인/실행이 아니다.** 문안을 읽고 환경 연결을 끝낸 AI 대화에 Ctrl+V → Enter로 보내면 시작한다. 한 provider에만 보낸다. 다른 provider를 같은 Unity의 별도 writer로 직접 동시에 켜지 않는다. 반복 발송해도 예산은 한 번만 추가하도록 지시에 명시했다.

AI는 D2 남은 품질 수정부터 기존 D3~D12 순서로 진행한다. 초기 Unity writer는 Codex, Claude는 감독/독립 위임을 맡는다. 한쪽 소진 시 writer 전환은 실제 adapter가 종료·in-flight·dirty·save·owner·누적 예산을 검증할 수 있을 때만 한다. **지원 runner만으로 게임 제작 자동 전환이 완성된 것은 아니다.** 연결 전에는 다른 provider가 독립 작업을 이어가거나 8번으로 재개한다.

이번 실행 묶음은 `FINAL_WINDOWS_HUMAN_REVIEW`에서 실제 화면 피드백을 받는다. 이 중간 마일스톤이 전체 게임 출시 완료는 아니며 이후 범위는 기존 설계에 보존한다.

## 7. Unity는 제작 시작 후 기존 버전 하나만

알려진 버전은 `6000.3.2f1`, 경로는 `C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe`다. 로컬 `ProjectSettings/ProjectVersion.txt`가 최신 기준이며 임의 업그레이드하지 않는다.

제작 AI가 기존 Editor와 진행 중 작업을 먼저 확인한다. 이미 열려 있으면 그대로 연결한다. 열어야 하면 Unity Hub **Add/Open → 기존 `C:\Users\sdjsd\Desktop\Unity\Project_PA`**를 선택한다. 새 프로젝트 생성·Project_D 복사가 아니며 Editor 하나만 사용한다.

기존 Unity MCP 설정은 `http://127.0.0.1:8080/mcp`다. 연결 실패 시 기존 Editor·MCP 설정과 로그부터 확인한다. 새 패키지·두 번째 서버·다른 Editor로 우회하지 않는다. Serena/Context7도 기존 `.mcp.json`과 `.codex/config.toml`을 재사용한다.

## 8. 사용량 소진 또는 다음 날 재개

사용 가능한 Claude/Codex 패널 또는 CLI에 보낸다.

```text
Tools/PaAgentConsole/startup/prompts/03_RESUME.md를 읽고 기존 승인과 실제 checkpoint에서 이어가줘. 새 예산 승인이 아니며, 이전 child/Unity in-flight와 단일 writer를 확인한 후 작업해.
```

제작 adapter가 연결됐다면 감독자가 처리하며 연결 여부는 환경 결과에서 확인한다. 둘 다 소진되면 작업 상태와 재개 근거를 저장하고 대기한다. 유료 API로 자동 우회하지 않는다.

상태만 보려면:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File 'C:\Users\sdjsd\Desktop\Unity\Project_PA\Tools\PaAgentConsole\Start-ProjectPA.ps1' -Provider status
```

재개 포인터는 `status`를 `resume`로 바꾼다. context 사용률·구독 잔량·작업 예산은 서로 다르다. 미제공 quota는 N/A이며 새 세션·모델·날짜로 업무 예산이 초기화되지 않는다.

## 9. 막혔을 때

| 문제 | 다음 행동 |
|---|---|
| Python 없음 | 공식 설치 후 새 터미널 또는 실제 exe 지정 |
| Wrong/detached branch, baseline missing | 정상 Codex에00번 복구 지시; 강제 checkout 금지 |
| LOCAL_MERGE_REQUIRED | AI가 충돌한 문서/도구만 비교 통합 |
| SETUP_OK인데 D2 WORK_BLOCKED | 설치만 완료된 정상 상태;02번을 읽고 발송 |
| CLI만 되고 확장은 실패 | 확장 버전/host/profile/settings 비교, `Claude Code: Show Logs` |
| 패널 재개 때 계속 멈춤 | 공식2.1.301+ 안내, 확장 갱신·Reload Window |
| `/pa`를 모름 | `/plugin`·버전·validate 확인; CLI경로는 세션 한정 |
| 값이 N/A/UNKNOWN | 아직 실제 관찰되지 않은 값; 성공으로 바꾸지 않음 |
| 상태줄 오류 | 실제 Python 경로/PS UTF-8 확인; `statusLine` 키만 복구 |
| 한쪽 소진 | 가용 provider로03번 재개; 두 번째 Unity writer 금지 |

복구 시 설정 전체를 과거 백업으로 덮지 말고 바꾼 `statusLine` 키만 비교한다. 문서도 해당 변경 부분만 통합한다. 최근 journal과 현재 dirty를 AI에게 확인시킨다.

## 10. 본인이 해야 할 일과 검증 범위

처음에는 압축 풀기, Codex에 첫 지시, 브라우저 로그인, 준비 결과 확인,02번 발송이다. 이후 핵심 역할은 실제 게임 화면의 취향/재미 피드백과 최종 검수다. 실제 PC에서 관찰하지 못한 연결은 현장 AI가 먼저 마무리해야 한다.

이 가이드와 prompts가 최신 시작 절차다. `payload/`의 과거 운영 문서는 배경 자료이며 작은 게임에 대한 과거 기간 예측은 이번 전체 범위 견적이 아니다. 시작할 때 전부 읽지 않는다. `VERIFICATION_START_KO.md`에서 실제 검증과 남은 현장 확인을 구분했다.

공식 자료는 막힌 해당 항목만 확인한다.

- [Codex CLI](https://learn.chatgpt.com/docs/codex/cli), [구독 인증](https://learn.chatgpt.com/docs/auth)
- [Claude 설치](https://code.claude.com/docs/en/setup), [CLI](https://code.claude.com/docs/en/cli-reference)
- [Claude VS Code](https://code.claude.com/docs/en/vs-code), [Mods](https://code.claude.com/docs/en/plugins/mods/overview), [로컬 marketplace](https://code.claude.com/docs/en/plugin-marketplaces)
- [게임 source 브랜치](https://github.com/junido7185/Project_PA/tree/milestone/gameplay-beta-85), 확인 HEAD `b121140ae6f5ca0f92078db5c2f75512dc8fc95a`
