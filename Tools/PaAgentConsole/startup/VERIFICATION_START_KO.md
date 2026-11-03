# 시작 패키지 확인 범위

2026-10-19 한국 시간. 이 기록은 Windows PC에서 설치와 게임 제작이 이미 실행됐다는 기록이 아니다.

## 수행한 검증

- 설치기 Python 검증11개 PASS: 허용 환경 guard, 패키지 훼손, 누락/해시 미검증 source, 대소문자 중복 목적지, 상위 경로의 일반 파일, 경로 이탈/symlink, portfolio 브랜치 차단, 충돌 전 전체 쓰기 차단, Git 패치 재적용 방지, 사용자 dirty·설정 항목·업무 권위 보존, 실행 중 다른 writer 변경 감지와 중단 기록.
- 기존 운영 도구 Python 검증33개 PASS: 한도/인증/청구/timeout 구분, 유한 지원 전환, 스트림 성공 판정, 관리 프로세스 정리, 민감 값 제외, 상태줄의 예산/구독 사용률 분리, N/A 유지.
- Mods JavaScript 구문 검사와 로컬 모의 이벤트 계약 PASS. **실제 Claude plugin validate·생성 타입·실제 렌더 PASS를 뜻하지 않는다.**
- PowerShell 소스는 ASCII 호환이다. Windows PowerShell5.1의 한국어 native pipe를 위해 statusline.ps1에 `$OutputEncoding` UTF-8을 추가했다. 상태줄 호출에도 호출 범위의 ExecutionPolicy Bypass를 지정하며 글로벌 정책은 바꾸지 않는다.
- 공개 GitHub 게임 브랜치 HEAD는 `b121140ae6f5ca0f92078db5c2f75512dc8fc95a`로 확인했다. master는 portfolio다.
- 입력 운영 ZIP의 기존16개 파일 hash를 확인한 뒤 통합했다. 새 starter-manifest.json은 전달 파일 hash와 설치 source→destination을 기록한다.
- 전체 payload Git fixture 통합 PASS: 실제 기준 브랜치에서 읽은 문서 사본에22개 설치 경로·16개 레퍼런스 결과 hash를 확인했다. 두 번째 설치는 동일 결과를 유지했고 원본 state/Handoff/HEAD/owner/사용자 dirty를 보존했다. Windows root guard와 실제 상태줄 호출은 실행하지 않은 fixture 검증이다.

## 추가한 실제 준비물

`Setup-ProjectPA.ps1` + `setup_pa.py`: 기본 조회, 명시적 -Apply 설치. 실제 Python exe 탐색, branch/history/root 확인, whitelist 파일 추가, 레퍼런스 패치 적용 또는 충돌 중단, local 상태줄 설정, 원본 백업과 단계 journal.

`Start-ProjectPA.ps1`: 설치된 실제 Python 경로 사용, 실행 전 프로젝트 확인, 환경 지시 클립보드 복사, Claude/Codex 열기, 상태/재개 출력, 제작 시작 문안 복사. 제작 시작 문안을 자동 발송하지 않는다.

`prompts/00~03`: 설치 복구, 현장 환경 연결, 한 번 발송하는 bounded 제작 재개 승인, provider가 바뀌어도 예산을 보존하는 재개 지시.

`START_HERE_KO.md`: 정상 Codex 확장에 맡기는 추천 경로와 직접 설치 경로, 성공 출력, 프로그램 설치/구독 로그인/Mods/Unity/한도 소진/복구 절차.

## 실제 PC에서 남은 확인

- PowerShell 구문/실기동, 설치된 Python 경로와 상태줄 실제 표시.
- Claude/Codex 실제 버전·구독 인증과 계정별 한도.
- Claude VS Code 확장 오류의 실제 원인 수정과 패널 관찰.
- 설치된 Claude 버전에 맞는 Mods validate/type/render와 feature flag.
- 기존 Unity MCP/Editor 실제 신원과 in-flight 상태. 새 Editor/compile/Play는 제작 승인 뒤 수행.
- **생산 adapter·누적 budget 집행·게임 writer 자동 인수인계 연결. 현재 provider_runner는 읽기 전용 지원용이다.** 현장 AI가01번 지시에서 연결한 결과를 별도로 보고해야 한다.
- 게임 변경, compile/Play/빌드, D2~D12 실제 제작, 사람 최종 화면 검수.

공유 게임 코드·scene·Assets·Save·원본 업무 예산·owner·GitHub 브랜치는 이 준비 작업으로 변경하지 않았다. 설치기 로직 검증은 임시 Git fixture에서 수행했다. 설정 보존 통합 검증의 상태줄 설치 부분은 mock을 사용했으며 실제 Windows 설정 실행으로 보고하지 않는다.

02번 제작 지시는 파일로 제공한 사용자 발송용 문안이다. 파일 존재·다운로드·설치·복사만으로 추가 실행 예산 승인이 생기지 않는다. 사용자가 읽고 실제 AI 대화에 발송한 뒤 적용한다. 이는 기존 선택 승인에 D2 추가3/3/0을 한 번 기록하는 문안이며 전체 게임 범위 축소를 포함하지 않는다.
