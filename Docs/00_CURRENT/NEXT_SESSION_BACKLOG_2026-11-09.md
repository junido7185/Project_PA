# 다음 세션 인수 목록 — 검토 안 한 것·건너뛴 것 (2026-11-09)

전시용 개발은 끝났다(r258, 회귀 42/42). 다음은 "진짜 개발" 준비다. 이 문서는 도구 재설계 세션(3e1ea1e0)에서 **확인하지 않았거나 건너뛴 것**만 모았다. 판정 근거는 `TOOL_UPGRADE_POLICY.md`, 적용 내역은 `TOOLING_DESIGN_2026-11-09.md`.

## A. 환경·도구 — 검토하지 않았거나 확인하지 못한 것
1. **후보 24건 조회 건너뜀:** GitHub 비인증 한도(시간당 60회)가 소진됐다. gh 로그인 뒤 `Tools/EnvRadar/check_tool_updates.py`를 다시 실행해야 watchlist 기준값(state.json)이 채워진다.
2. **원문을 확인하지 못한 것:**
   - Reddit 약관 원문
   - Meta Ad Library API 공식 페이지(403)
   - Handlus 플러그인 LICENSE
   - YouTube API 데이터 보관 주기 조항
   - Kun Chen 원 게시물(X)
   - "graphify 포함 6개 스킬 한 번에 설치" 묶음의 출처
   - hyperframes 텔레메트리 여부
   - unslop CC BY 4.0의 출처 표시 의무가 산출물까지 미치는지
3. **직접 재지 않은 효과 주장:** graphify "토큰 80% 절감", ponytail "토큰 −45%". 도입을 검토한다면 같은 과제로 직접 비교해야 한다.
4. **csharp-lsp 공식 플러그인:** Claude Code 쪽 버그가 해결됐는지 확인하지 않았다. Serena와는 비교하지 않았다.
5. **Unity 공식 플러그인(설치함):** 실제 스킬 사용은 검증하지 않았다. 33개 스킬 중 2D, 결제, 멀티플레이, 웹처럼 이 게임과 무관한 것도 함께 켜진다. 플러그인 안의 스킬은 하나씩 끌 수 없다. 상시 비용은 세션당 약 3,045토큰이다.
6. **하위 에이전트 기본 모델:** 사용량을 줄이는 설정(조사형 하위 에이전트를 가벼운 모델로 돌리기)은 검토하지 않았다.
7. **전역 설정:** ECC, claude-mem, figma, context7이 전역에서는 켜져 있다. 다른 프로젝트 기준으로는 검토하지 않았다.
8. **context7:** 인증이 필요하다. 실제로 써 보지 않았다.
9. **Unity MCP 연결:** 정리 뒤 실제 연결 시험을 하지 않았다(에디터 미실행). 데스크탑 앱을 재시작한 뒤 MCP 목록도 확인하지 않았다.
   - **11-09 오후 재확인: 데스크탑 설정 정리가 되돌아갔다.** `claude_desktop_config.json`에 stdio `unityMCP`(WinGet uvx, 10.2.0)가 다시 들어 있다. 파일 수정 시각 15:25:23은 새 세션이 열린 시각과 같다. 켜져 있는 앱이 메모리의 설정으로 파일을 다시 쓴 것이다. 실제로 앱 본체가 `mcp-for-unity --transport stdio`를 2개 띄워 둔 상태였다. 그래서 재시작만으로는 고쳐지지 않는다. 앱을 완전히 끈 상태에서 지워야 한다(C 절의 스크립트).
10. **글자 검사:** 자체 cmap 파서로만 검사했다(fontTools 교차 확인은 안 함). TMP가 동적 아틀라스라서 실제 화면 렌더 확인도 하지 않았다.

## B. 승인됐지만 아직 만들지 않은 것 (설치 없이 가능)
- Blender 4.5 포터블 헤드리스 스크립트 틀과 시험 FBX 내보내기(Assets 밖 스테이징 폴더로)
- 화면 비교 스크립트: ffmpeg SSIM/PSNR, odiff를 설치한 뒤에는 차이 맵
- CC0 자료 라이선스 장부 문서(Kenney, Quaternius, Poly Haven, ambientCG, 폰트 OFL)
- jsfxr, hanspell, Mermaid 사용 메모
- 약관 안 레퍼런스 수집기: YouTube 채널 RSS와 Reddit RSS를 DevKnowledge(`devk.py`)에 붙이기. 설계만 있음

## C. 사용자가 직접 할 것
- `gh auth login --web` (gh 2.103.3은 Codex가 설치함)
- ~~`npm install -g odiff-bin hanspell jsfxr @mermaid-js/mermaid-cli`~~ **11-09 완료·실행 확인.** odiff 4.5.0(시험 이미지 차이 441픽셀 정확히 검출, 차이 맵 생성), mermaid-cli 12.2.0(한글 흐름도 SVG 렌더 성공), jsfxr 1.4.1(명령 없는 라이브러리라 `NODE_PATH=$(npm root -g)`로 불러 효과음 wav 생성 성공), hanspell 1.0.1(도움말만 확인. 문장을 다음·네이버로 보내므로 시험 문장도 보내지 않았다).
- 업그레이드 (11-09 진행):
  - ~~`claude update`~~ 완료: CLI 2.1.303 → 2.1.319. 데스크탑 앱 세션은 앱에 묶인 2.1.317을 따로 쓴다.
  - ~~`uv self update`~~ 완료: 0.12.19 → 0.13.7. 처음 실패한 원인은 켜져 있던 Serena 3개(이 세션, 이전 3e1ea1e0 세션, VS Code Codex)가 `~/.local/bin/uvx.exe`를 쓰고 있던 것이다. 실행 중인 파일은 이름 변경이 되므로 `uvx.exe`·`uvw.exe`를 `*.old-20261109-152726`으로 비켜 두고 업데이트했다. 이 두 파일은 위 세션들이 모두 닫힌 뒤(재부팅 뒤가 확실) 지워도 된다.
  - ~~`uvx --refresh --from serena-agent serena --help`~~ 완료: Serena 2.1.0. MCP 명령과 같은 `-p 3.13`으로 받았다.
  - **fonttools 4.53.0 → 4.68.0: 미완료.** 자동 모드가 pip 설치를 거부했다. 직접 실행: `C:/Users/sdjsd/AppData/Local/Programs/Python/Python312/python.exe -m pip install --upgrade fonttools`
  - 참고: uv가 두 벌 설치돼 있다. `~/.local/bin`(독립 설치, PATH 우선, 0.13.7)과 WinGet(0.11.28). WinGet 쪽은 `uv self update`가 안 되고 `winget upgrade astral-sh.uv`로 올린다. 지금은 데스크탑 설정의 stdio unityMCP만 WinGet 경로를 쓰므로, 그 항목을 지우면 WinGet uv는 제거를 검토할 수 있다.
- 데스크탑 앱이나 CLI의 `/mcp`에서 context7 인증
- **데스크탑 Unity MCP 정리(재시작만으로는 안 됨, A-9 참고):** 앱을 트레이까지 완전히 종료한다. 그다음 앱 밖의 PowerShell에서 `C:/Users/sdjsd/AppData/Local/Programs/Python/Python312/python.exe Tools/EnvRadar/fix_desktop_unity_mcp.py`를 실행하고 앱을 다시 켠다. 스크립트는 앱이 켜져 있으면 멈추고, 백업을 `Logs/EnvRedesign-20261109/backup/`에 남긴 뒤 `mcpServers.unityMCP`만 지운다. 다시 켠 뒤 `--dry-run`으로 항목이 사라졌는지 확인한다.
- Unity 에디터의 MCP for Unity 창에서 HTTP 서버(8080) 켜기
- (선택) YouTube Data API 키를 발급해 사용자 환경변수에 넣기

## D. 전시 빌드 뒤로 미룬 업그레이드
MCP for Unity 10.3.0(`#v10.3.0`으로 고정), ffmpeg 9, pillow 13, Node 26 LTS, Git 2.56, Blender 4.5.15(패치)와 5.x(별도 폴더에서 시험).

## E. 게임 개발 — 진짜 개발 전에 정할 것
- **플레이어 이동 애니메이션 라이선스:** `Assets/Art/Animation/PlayerLocomotionMocap/PA_MC_*` 18개는 반다이남코 연구 모캡(**CC BY-NC 4.0, 비상업 전용**)으로 만들었다. 전시는 비상업이라 괜찮지만, **상업 출시 전에는 반드시 교체**해야 한다. 원본은 `Assets/Art/External/BandaiNamcoResearch/`에 있다.
- commit/push 허용 여부: 미커밋 변경이 많다.
- 미사용 메시 삭제 허용 여부
- 영상 음성 분석(yt-dlp, faster-whisper 설치 거부): YouTube 다운로드 금지 약관과 부딪칠 수 있다. 쓴다면 사용자 소유 영상만 대상으로 한다.
- 벌목 업그레이드(전시 뒤로 결정), 보류한 모션 작업 재개
- 최종 사람 검수(실제 손 조작 체감)
- 전시 빌드: 월요일에 사용자가 `EXHIBITION_BUILD_GUIDE.md` 순서대로 만든다.

## F. 사용량 운영 (실측 근거: TOOL_UPGRADE_POLICY.md §5-1)
- 이 세션은 일주일을 이어 왔고, 호출마다 평균 약 50만 토큰을 다시 읽었다. **다음 작업은 새 세션에서 시작한다.**
- `/model` 전환은 세션을 시작할 때만 한다. 도중에 바꾸면 캐시가 깨져 약 80만 토큰을 새로 캐시한다.
- 인계 문서 보관은 끝났다(100,641자 → 39,603자). 11-07 이전 항목은 `archive/CODEX_HANDOFF_ARCHIVE_20261026-20261107.md`에 있다.

## 이번 세션에서 끝낸 것 (재작업 금지)
- 플러그인과 MCP 재설계, Unity MCP 중복 3곳 정리, 전체 도구 프로필 추가
- 업그레이드 점검기와 watchlist, 판정 규칙 문서
- SNS 후보 25건 판정
- 글자 누락 검사(981종 중 Δ 1개, 화면 영향 없음)
- Unity 공식 플러그인 설치(프로젝트 범위)
- 인계 문서 보관
- 매월 1일 10시 점검 예약(`pa-tool-upgrade-radar`, 자동 모드로 저장, 앱이 켜져 있을 때 실행)
