# Codex 전용 — Windows 데모 후보와 증거의 일치 검사

운영 우선순위 보완(2026-10-05): 사용자가 기한 내 게임 개발을 우선하도록 요청했다. 이 검사 도구는 후순위 선택 작업으로 두고, 제작 인수에는 `CODEX_OPENING_DEMO_TAKEOVER_PROMPT.md`를 사용한다. 이 파일을 제작의 선행 작업으로 실행하지 않는다.

작성일: 2026-10-04. 이 파일은 실행 지시서다. 파일 생성/읽기만으로 게임 제작 승인이나 기존 P1~P11의 담당 전환이 발생하지 않는다. 아래 시작 메시지를 사용자가 실제로 전달했을 때 이 지원 작업만 실행한다.

## 새 Codex 세션에 보낼 시작 메시지

```text
Project P.A.에서 CODEX-DEMO-DELIVERY-SUPPORT-001을 구현해줘.
AI_WORKFLOW/03_TASKS/CODEX_DEMO_DELIVERY_SUPPORT_PROMPT.md의 계약을 따라,
Windows 데모 후보·빌드 기록·현재 소스의 차이를 확인하는 오프라인 CLI를 완성해.
수정은 Tools/CodexDemoDeliverySupport/**와 Logs/CodexDemoDeliverySupport/**에만 승인한다.
Claude는 P1~P11과 Unity Editor·게임 코드·빌드·실제 플레이를 계속 담당한다.
공유 상태/문서/관찰 로그는 읽기 전용이며, 이 지원 작업의 진행·결과·재개 기록은 전용 Logs에 남겨.
이번 한정 작업에서는 이 기록 경로가 공통 문서/Handoff/관찰 로그 갱신 규칙보다 우선한다.
게임 티켓 승인/담당을 바꾸지 말고, 구현·필요한 테스트·기존 후보의 읽기 검사·사용법까지 끝내줘.
```

## 목적과 담당

데모를 전달할 때 오래된 실행 파일, 다른 후보의 증거, 현재 코드와 달라진 빌드 기록을 혼동하지 않도록 한다. 검사 도구 구현은 P11의 지원 작업이며, P11 실행·실제 플레이·납품 승인 자체를 넘겨받는 작업이 아니다.

| 담당 | 작업 | 수정/실행 범위 |
|---|---|---|
| Claude | P1~P11 기능·화면·모션, Unity 컴파일/Play, Windows 빌드와 실제 검수 | 기존 승인 범위. 아래 Codex 전용 폴더는 사용하지 않는다 |
| Codex | 기존 메타데이터를 읽는 CLI, 격리된 도구 테스트, 독립 보고서 | 전용 Tools/Logs만. Editor와 게임을 실행하지 않는다 |

Claude의 사용량 소진·중단·시간 경과를 게임 티켓 인수 승인으로 해석하지 않는다. 이 작업을 끝내면 멈춘다.

## 읽기와 충돌 방지

1. AGENTS.md, CURRENT_STATE → CAPABILITY_REGISTRY → GAME_LOOP_MAP → INTEGRATION_QUEUE, CODEX_HANDOFF와 ONE_PAGE_WORKFLOW의 필수 지침을 읽는다. 경로는 기존 Docs/00_CURRENT/와 AI_WORKFLOW/00_START_HERE/를 사용한다. 이미 읽은 불변 본문은 재출력하지 않는다.
2. loop-state.json의 demoProductionRouting과 선택된 승인 record를 읽어 Claude의 담당을 확인한다. Handoff의 게임 작업을 재개하지 않는다. 기존 실행 프롬프트에서는 P11과 사용량·검증 중복 방지 부분만 추가로 읽는다.
3. 아래 증거의 형식과 관련 기존 helper만 좁게 조사한다. 같은 목적의 도구가 있으면 재사용 가능한 부분을 우선한다. 기존 helper를 실행하기 전에 쓰기 경로를 확인하고, 공유 경로를 쓰는 helper는 실행하지 않는다.
4. 수정 전 실제 예정 파일과 검증 방법을 짧게 보고하고 진행한다. Codex 전용 폴더에 다른 작업이 있으면 내용을 보존하고 자신의 변경과 구분한다. 다른 작업과 같은 파일을 수정해야 하면 그 의존성만 기록하고 멈춘다.

작업 루트는 C:/Users/sdjsd/Desktop/Unity/Project_PA 하나다. 전용 두 폴더 밖에는 코드·기록·캐시·임시 파일을 쓰지 않는다. Assets, Packages, ProjectSettings, Library, Build 산출물, 기존 Tools, 기존 증거, 사용자 save, loop-state/validator-registry, 현재 문서·월별 로그·Handoff와 관찰 로그는 읽기 전용이다. 새 승인 record, 별도 게임 티켓 상태, Git stage/commit/push, 삭제/덮어쓰기/정리 작업을 만들지 않는다. Project_D는 이 작업에 필요하지 않다.

UnityMCP/Editor/Play/BuildPlayer, Unity 프로젝트의 dotnet/msbuild, 후보 EXE 실행, GameView 촬영, 입력 주입, 글로벌 설정 변경, 패키지/외부 에셋 설치, 추가 에이전트와 새 Claude 세션은 사용하지 않는다. 게임 관련 결함을 발견하면 보고서에 근거만 남기며 게임 코드를 고치지 않는다.

## 구현할 결과

Python 표준 라이브러리로 작은 CLI를 만든다. 기본 구성은 check_delivery.py, test_check_delivery.py, README.md 세 파일이며 필요한 작은 분리는 허용한다. 새 검증 프레임워크·GUI·CI·감시 daemon은 만들지 않는다.

CLI 입력은 --project-root, --candidate-dir, --evidence-dir, --output-dir로 명확히 받는다. 후보/증거 폴더를 최신 수정 시각으로 자동 선택하지 않는다. 모든 입력 경로 및 메타데이터 안의 경로는 정규화 후 프로젝트 안인지 확인한다. junction/symlink를 통한 외부 접근도 허용하지 않는다. 출력은 전용 Logs 안의 고유 실행 폴더이며 기존 결과를 덮어쓰지 않는다. 메타데이터와 build.json에 들어 있는 명령/코드는 데이터로 읽고 실행하지 않는다.

다음을 JSON과 사람이 읽을 Markdown 보고서로 출력한다.

- 후보의 명시적 경로, 검사 시각, 실제 확인한 EXE/UnityPlayer/데이터 폴더와 관련 바이너리의 크기·SHA-256. 기존 빌드의 Mono/IL2CPP 구성을 확인하고, 확인되지 않은 backend의 파일 요구사항을 추측하지 않는다. 알려진 Mono용 파일 누락을 IL2CPP 후보에도 그대로 적용하지 않는다.
- 빌드 기록과 실제 후보의 연결: 후보 파일 해시, shipped/compilerOutput 해시 등 존재하는 근거를 대조한다. 다른 후보의 기록이거나 연결 근거가 없으면 구분해 표시한다. 기록 안의 matches=true 값을 그대로 믿지 않는다.
- 기존 compiled-source-checksums/build-input-files-sha256의 선언된 항목과 현재 파일을 다시 해시해 비교한다. 상대/절대 경로, SHA-256 대소문자, 한글·공백 경로를 처리한다. 누락/차이가 난 파일과 비교 범위를 명시한다. 현재 source hash를 새로 저장해 과거 빌드 입력이라고 주장하지 않는다.
- 명시된 증거 폴더와 그 안의 로컬 증거 링크가 실제 존재하는지 확인한다. PNG/로그의 존재만으로 플레이 성공·사람 수락·후보 동일성을 판정하지 않는다. 파일 밖 URL을 따라가거나 영상/스크린샷 내용을 재검수하지 않는다.
- artifactIntegrity, declaredSourceMatch, provenanceBinding, evidenceCompleteness를 각각 표시한다. MATCH/MISMATCH/MISSING/UNVERIFIED/UNSTABLE 등 정의한 결과, 근거와 한계를 함께 기록한다. 선언된 코드 일부의 일치를 전체 씬·에셋·설정의 일치로 확대하지 않는다. 기록상 연결 확인과 바이너리에서 소스를 독립적으로 검증한 결과도 구분한다.

EXE의 시각, Git HEAD, 현재 소스, PASS 개수만으로 '최신 빌드', '소스 동일', '데모 PASS', 'RELEASE ACCEPTED'를 선언하지 않는다. 빌드/컴파일러 기록에서 실제 후보로 연결되는 근거가 부족하면 provenanceBinding은 UNVERIFIED다. 이 도구는 소스에서 바이너리를 재빌드하거나 PDB를 재추출하는 도구가 아니다.

Claude가 검사 중 읽기 대상 파일을 변경하거나 기록을 쓰는 중이면 해당 읽기/비교 범위를 UNSTABLE로 표시한다. 해시 전후와 검사 경계에서 관련 파일의 메타데이터를 확인하는 등 소규모 방식을 사용하고, 원자적 전체 프로젝트 snapshot을 얻었다고 주장하지 않는다. Claude를 멈추거나 안정될 때까지 반복 대기하지 않는다. 잘못된 CLI/경로/JSON과 검출된 차이는 도구 오류와 구분한다. 종료 코드와 상태별 의미는 README에 적는다.

## 기존 실자료와 검증

예시 후보: Builds/Windows/Candidate-ShopPolish-20260927-194240
예시 증거: Logs/VisualQA/ReportCandidate-20260927
기존 자료: DELIVERY_REPORT.md, build-result.txt, compiled-assembly-binding.json, compiled-source-checksums.json, build-input-files-sha256.json, candidate-files-sha256.json, source-state.json. snapshot.py는 참고용이며 공유 출력 경로로 실행하지 않는다.

이 후보는 9월 27일 자료다. 존재 여부를 먼저 확인하고, 현재 P1~P3 코드가 포함된 후보라고 가정하지 않는다. 자료가 없거나 형식/연결이 부족해도 도구의 명확한 누락·미검증 보고가 정상 결과다. 현재 라이브 Library/compiler output과 과거 후보의 원본 기록을 혼동하지 않는다.

도구 동작을 검증하는 작은 fixtures를 전용 Logs의 새 폴더에 만든다. 실제 사용자 save나 빌드 파일을 변경하지 않는다. 대략 다음 여섯 묶음만 검사한다: 일치하는 기록과 후보, 소스/바이너리 변경, 필수 파일 누락, 연결 기록/증거 누락, 외부 경로·잘못된 JSON·한글/공백 경로, 검사 도중 파일 변화. 정상 예제만 통과하도록 만든 테스트를 피하고, 미검증 상태가 긍정 판정으로 바뀌지 않는지 확인한다. 시간에 의존한 불안정한 race 테스트 대신 제어 가능한 읽기 경계를 사용한다.

필요한 테스트를 한 번 실행하고 기존 후보를 한 번 읽기 검사한다. pytest 설치, Unity 전체 회귀, P1 재로드 4세션, D2 영업 재실행, 전체 MUST PATH, 재빌드는 하지 않는다. 새 변경·실패가 있으면 영향받는 검사만 재실행한다. 기본 비 Studio retry 정책을 적용하며 명백한 같은 범위 기계적 수정은 한 번, 같은 실패가 다시 발생하면 근거를 남기고 멈춘다. 예산을 소진하려고 반복하지 않는다. Python -B 등으로 캐시 쓰기도 전용 범위에 한정하고 fixtures를 지우지 않는다.

README에는 새 후보/증거 경로로 실행하는 방법, 상태와 종료 코드, 확인 범위와 한계, Claude가 P11에서 기존 방식으로 남기면 활용 가능한 메타데이터만 적는다. 새 metadata 생성을 Claude의 다음 티켓 진행 조건으로 추가하지 않는다. 없으면 UNVERIFIED로 남긴다.

## 완료와 중단 기록

완료 조건은 도구 구현 + 필요한 테스트 결과 + 기존 후보의 실제 읽기 검사 결과(자료가 없으면 그 근거) + 실행법이다. 도구 검증 성공과 후보의 차이/누락/미검증을 별도로 보고한다. 게임 제작 완료/사람 검수 결과는 바꾸지 않는다.

기록은 Logs/CodexDemoDeliverySupport/<고유실행>/EVIDENCE.md 한 곳에 남긴다. 변경 파일, 실행 명령과 테스트 결과, 보고서 경로, 검출된 차이와 확인 못 한 항목, Exact Next Action, commit/push 없음만 담는다. 중단돼도 같은 장소에 재개 지점을 적는다. 공유 월별 로그/Handoff에 복제하지 않는다. 관찰 기록이 필요하면 이 전용 기록에 포함하고, 제작의 추가 완료 조건으로 삼지 않는다.

최종 답변은 무엇을 만들었는지, 어떻게 실행하는지, 실제 확인 결과와 남은 경계를 짧게 전달한다. 이미 통과한 검사를 새 이유 없이 확대하지 말고 이 한정 작업을 끝낸다.

## Claude에게 전달할 범위 통지

```text
P1~P11 게임 제작과 Unity·빌드·실제 검수는 계속 맡아줘.
Codex는 오프라인 납품 지원 도구만 별도로 구현한다.
Tools/CodexDemoDeliverySupport/**와 Logs/CodexDemoDeliverySupport/**는 Codex 전용이므로 수정·실행 대상으로 삼지 마.
도구 도입이나 추가 metadata 생성을 현재 티켓 진행 조건으로 넣지 말고 기존 순서대로 진행해줘.
```

## 공식 자료를 반영한 설계 근거

Codex의 작업 지시는 구체적인 결과·관련 파일·제약·작은 검증 범위로 구성할 수 있다. 이 지침을 이번 CLI 작업에 적용했다. [OpenAI 공식 Prompting](https://learn.chatgpt.com/docs/prompting)

OpenAI는 불필요한 문서 반복 읽기와 과도한 테스트 지시가 컨텍스트·작업 비용을 늘릴 수 있다고 설명하고 완료 조건을 먼저 명시할 것을 권한다. 필수 프로젝트 지침은 유지하되 이 지원 작업에서는 관련 자료, 도구 테스트, 후보 읽기 검사만 수행한다. [OpenAI 공식 지침](https://developers.openai.com/blog/rethinking-skills-and-prompts-for-gpt-6-astra)

병렬 작업의 변경 격리는 공식 worktree 문서에서도 다룬다. 이 프로젝트는 허용 작업 루트와 로컬 미커밋 자산, 공유 Editor 제약이 있으므로 이번에는 새 체크아웃 대신 전용 경로와 실행 소유권을 나누는 방식을 선택했다. 이는 프로젝트 조건에 따른 판단이다. [OpenAI 공식 Git worktrees](https://learn.chatgpt.com/docs/environments/git-worktrees)

이 자료는 Claude 대비 Codex의 모든 작업 우위를 입증하지 않는다. 현재 맡길 작업은 Codex의 코드 읽기·CLI 구현·경계 조건 검증을 활용하면서 Claude의 게임 제작과 수정/실행 범위를 분리할 수 있어 선택했다.
