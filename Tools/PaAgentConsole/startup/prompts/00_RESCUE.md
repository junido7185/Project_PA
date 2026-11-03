# 로컬 AI에 보내는 설치 복구 지시

Project P.A. 시작 패키지 설치를 이 Windows PC에서 직접 마무리해줘. 내가 패키지 폴더의 setup_pa.py 또는 START_HERE_KO.md를 에디터에 열어두었으니 실제 경로부터 확인해. 파일을 아직 못 찾았다면 내게 패키지 폴더 경로 하나만 물어봐.

허용 게임 경로는 C:\Users\sdjsd\Desktop\Unity\Project_PA다. 그 안의 AGENTS.md와 현재 네 문서, CODEX_HANDOFF.md, Git HEAD/branch/dirty를 먼저 확인해. master는 portfolio라서 설치 대상이 아니다. 브랜치가 다르거나 gameplay baseline이 없으면 기존 dirty와 사용자 작업을 보존하는 전환안을 실제 확인한 뒤 진행해. reset/clean/stash/강제 checkout/새 clone/Project_D 복사로 우회하지 마.

이 시작 패키지는 기존 운영 도구, 레퍼런스 13개 결정 문서 패치, 진단/상태줄, Mods 소스, 설치기와 이후 지시문을 포함한다. 전체를 다시 연구하지 말고 패키지와 검증 기록을 읽어 설치 실패 원인을 좁혀 수정해. Python은 실제 3.11+ exe를 확인해 Windows Store 별칭을 피하고, 없는 경우 공식 설치를 안내해. 설치기의 경로·충돌 guard를 삭제해서 통과시키지 말고 실제 충돌 파일만 비교해 통합해. 기존 설정·모델·권한·MCP는 보존하고 바꾸는 항목만 백업해. 새 파일은 목적 위치에 추가하고 레퍼런스 패치는 git apply --check 후 적용하거나 변경된 문서의 해당 부분만 수동 통합해. 기존 폴더 전체를 덮어쓰지 마.

환경 준비 요청이므로 Unity 실행/compile/Play와 D2 예산 재개는 아직 하지 마. 로그인·계정 연결은 내가 직접 한다. API key/토큰/credential 파일을 읽거나 출력하지 마. 정상 Codex 확장은 보존해.

설치가 되면 Tools/PaAgentConsole/startup/prompts/01_ENVIRONMENT_SETUP.md의 실제 환경 연결 작업까지 이어서 수행해. 완료한 파일·관찰한 오류 원인·현장 동작 여부·정확한 다음 행동을 짧게 보고해. Git commit/push는 하지 마.
