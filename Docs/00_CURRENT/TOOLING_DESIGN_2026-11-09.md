# 게임 개발 도구 환경 설계 (2026-11-09)

사용자 지시: "모든 환경의 플러그인·스킬·MCP 세팅을 다시 설계해 게임 개발 모든 분야에서 문제 없도록, 최고의 무료 세팅." 하위 에이전트 5개(설정 구조, 3D·아트, 기획·서사, 유니티 개발, UI·효과·사운드)가 공식 문서·저장소를 조사했고, 이 문서는 그 결과를 합친 것이다. 기존 설정 백업: `Logs/EnvRedesign-20261109/backup/`.

## 1. 설계 원칙
1. **필요한 것만 정확히 켠다.** 스킬 설명은 컨텍스트 예산(기본 1%) 안에서만 보인다. 무관한 스킬 수백 개를 켜면 프로젝트 전용 스킬 설명이 밀려난다. 플러그인 안 스킬은 하나씩 끌 수 없어 플러그인 단위로 고른다.
2. **무료이고 데이터가 밖으로 나가지 않는 것을 우선한다.** 유료 키가 필요한 경로(유니티 MCP 생성 기능, Figma 유료 좌석)는 쓰지 않는다.
3. **유니티 진입점은 하나로 둔다.** 단일 Unity writer 규칙과 맞춘다(같은 에디터에 연결이 두 개면 명령이 두 번 적용될 수 있다).
4. **비밀값은 파일에 두지 않는다.** 토큰·키는 사용자가 직접 로그인하거나 사용자 환경변수에 둔다.
5. **자체 제작 원칙을 지킨다.** 받은 자료는 CC0 위주로 쓰고, 출처와 라이선스를 장부에 남긴다.

## 2. 적용한 변경 (2026-11-09, 새 설치 없음)
| 위치 | 변경 | 이유 |
| --- | --- | --- |
| 전역 `~/.claude/settings.json` | github 플러그인 끔 | 토큰이 없어 매 세션 400 오류. gh CLI 로그인 뒤 다시 켬 |
| 〃 | ECC `hook_profile`을 `minimal`로 | 다른 프로젝트에서 ECC를 쓸 때 훅 부담을 최소화(스킬·명령은 그대로) |
| 〃 | `skillListingBudgetFraction` 0.01 → 0.02 | 스킬 설명이 잘리는 것을 줄임 |
| 〃 | 소네트 모델 effort `low` → `high` | 소네트로 바꿔도 제작 품질 유지 |
| 프로젝트 `.claude/settings.json`·`settings.local.json` | humanize-korean 켬, ECC·claude-mem·Figma·claude-code-setup·github 끔 | 대사·문서 한국어 윤문은 쓰고, 소음·중복·유료 한도는 뺌 |
| `~/.claude.json`(프로젝트 local) | `UnityMCP` 중복 제거 | 대소문자만 다른 같은 주소 연결이 둘이었음 |
| 데스크탑 앱 `claude_desktop_config.json` | `unityMCP`(stdio, 10.2.0) 제거 | 데스크탑에서는 이 정의가 프로젝트 설정보다 우선해 CLI와 다르게 연결됐음 → 프로젝트 `.mcp.json` HTTP 하나로 통일. **데스크탑 앱 재시작 후 적용** → 11-09 15:25 켜져 있던 앱이 파일을 다시 써서 항목이 되살아났다. 앱을 완전히 끈 상태에서 `Tools/EnvRadar/fix_desktop_unity_mcp.py`로 지워야 한다(`NEXT_SESSION_BACKLOG_2026-11-09.md` C절). |
| 프로젝트 `.claude/profiles/full-tooling.settings.json` | 새 파일 | 전체 도구가 필요할 때 CLI에서 `claude --settings .claude/profiles/full-tooling.settings.json` |
| `CLAUDE.md` | 플러그인 방침 문장 갱신, Python 경로 명시 | 새 설계를 지침에 반영 |

되돌리기: `Logs/EnvRedesign-20261109/backup/`의 파일을 원래 위치로 복사한다.

추가(같은 날): 데스크탑 앱이 쓰는 소문자 경로 기록(`c:/Users/.../Project_PA`)에 남아 있던 `UnityMCP` 중복도 제거했다(백업 `claude.json.before-lowercase-fix`). 지속 업그레이드 체계는 `TOOL_UPGRADE_POLICY.md`, 점검기는 `Tools/EnvRadar/`, 글자 누락 검사는 `Tools/Art/check_glyphs.py`(결과: 사용 글자 981종 중 폰트에 없는 것은 캡처 오류 로그의 Δ 하나뿐 — 게임 화면 영향 없음).

## 3. 분야별 구성 (이 프로젝트 기준)
| 분야 | 지금 쓰는 것 (무료) | 새 후보 (승인 필요) |
| --- | --- | --- |
| C# 코드 탐색·편집 | Serena(Roslyn), dotnet typecheck 스크립트 | csharp-lsp 공식 플러그인(Claude Code 쪽 미해결 버그 확인 뒤) |
| 유니티 에디터 조작 | MCP for Unity(HTTP 하나, 단일 writer) — UI·VFX·셰이더·재질·ProBuilder·프로파일러·테스트·문서 조회 | 패키지·서버 버전을 v10.3.0으로 고정(전시 뒤) |
| 테스트·성능 | 캡처 하네스(42플랜), `-pa-perf-log`, Unity Test Framework `-runTests`(격리 복사본) | — |
| 화면 품질 비교 | ffmpeg(설치됨) SSIM/PSNR, Python PIL 접촉 시트 | **odiff**(픽셀 차이 맵) |
| 3D 모델링·리깅 | 에디터 스크립트 절차 생성(기존 방식) | **Blender 4.5 포터블 헤드리스 CLI**(이미 PC에 있음: `PA_ArtStaging/_tools`), Rigify(내장). 선택: MCP for Blender(텔레메트리 끔, Poly Haven만) |
| 텍스처·자료 | 자체 제작 | **CC0 자료**(Kenney, Quaternius, Poly Haven, ambientCG) + 라이선스 장부 |
| 이미지 생성 | — | 선택(전시 뒤): ComfyUI + FLUX.1 schnell(Apache 2.0, 8GB VRAM) |
| UI·HUD | impeccable 스킬, 유니티 MCP `manage_ui` | **Pretendard / Noto Sans KR**(OFL) 한글 폰트, Kenney UI 팩 |
| 효과음·음악 | C# 런타임 합성 | **jsfxr**(효과음 원형), **Kenney 사운드 팩**(CC0), **Freesound CC0**(무료 키, 환경변수). 선택: ACE-Step 1.5(로컬 음악, MIT) |
| 기획·시나리오·대사 | Markdown 문서, `ResidentDialogueLines.cs`, humanize-korean | **fontTools 글리프 검사**(대사 글자가 폰트·TMP 아틀라스에 다 있는지), **hanspell**(맞춤법, 문장이 다음·네이버로 전송됨 — 문자열만), **draw.io 플러그인 또는 Mermaid CLI**(흐름도) |
| 밸런스·경제 | xlsx 스킬 + 로컬 Python | — |
| 조사·레퍼런스 | deep-research 스킬, 내장 브라우저, DevKnowledge 라이브러리 | context7 인증(사용자가 `/mcp`에서) |
| 발표·문서 | pptx·docx·docs 스킬 | — (겹치는 슬라이드 도구는 pptx 하나로 통일) |
| 형상 관리 | git | **gh CLI**(`winget install --id GitHub.cli -e` 뒤 사용자가 `gh auth login --web`) |
| 기억·인계 | CODEX_HANDOFF.md, DevKnowledge | — (claude-mem은 훅이 모든 이벤트에 붙어 인계 체계와 겹침) |
| 실제 플레이 확인 | 데스크탑 앱 computer-use(사람 검수 보조) | — |

## 4. 쓰지 않기로 한 것과 이유
| 항목 | 이유 |
| --- | --- |
| 유니티 MCP `generate_image/model/audio` | fal.ai·OpenRouter·Tripo·Meshy 유료 키 필요 |
| Figma MCP 실사용 | 무료 플랜 월 20회 이하, 데스크탑 서버는 유료 좌석 |
| Hunyuan3D-2 | 라이선스 적용 지역에서 한국 제외 |
| TRELLIS.2 | VRAM 24GB 이상 필요 |
| FLUX.1 dev | 비상업 라이선스 |
| Sketchfab·Hyper3D 연동 | 라이선스 혼재·팬 모델 위험, 유료 키 |
| Shadertoy 변환 도구 | Shadertoy 코드 기본 라이선스가 비상업(CC BY-NC-SA) |
| Notion·Obsidian MCP | 기존 문서·DevKnowledge와 중복, Notion은 문서가 외부로 나감 |
| 서사 엔진(Ink·Yarn·Twine) | 전시 전 대사 구조 변경은 세이브 호환 위험. 전시 뒤 현지화가 필요하면 Yarn Spinner 1순위 |
| GitHub Actions + GameCI | 비공개 저장소는 무료 크레딧이 빨리 소진, 유니티 라이선스 활성화 불안정 |
| ECC 전체(이 프로젝트) | 293 스킬 대부분 무관, 훅 21개, 추가 MCP. 필요하면 full 프로필로 |

## 5. 사람이 해야 하는 것
- **context7 인증:** 데스크탑 앱이나 CLI의 `/mcp`에서 인증한다.
- **gh CLI 로그인:** 설치를 승인하면 `gh auth login --web`은 사용자가 직접 한다.
- **Freesound 키:** 쓰기로 하면 무료 계정의 키를 사용자 환경변수(`FREESOUND_API_KEY`)에 둔다. 저장소에는 넣지 않는다.
- **유니티 MCP:** 에디터의 MCP for Unity 창에서 HTTP 서버(포트 8080)를 켜야 연결된다.
- **데스크탑 앱 재시작:** 데스크탑 설정 변경(유니티 MCP 정리)을 반영한다.

## 6. 근거 (하위 에이전트 조사 출처 일부)
- Claude Code 설정·스킬 예산·MCP 우선순위: https://code.claude.com/docs/en/settings.md , https://code.claude.com/docs/en/skills.md , https://code.claude.com/docs/en/mcp.md , https://code.claude.com/docs/en/desktop.md
- MCP for Unity: https://github.com/CoplayDev/unity-mcp , https://coplaydev.github.io/unity-mcp/architecture/transports
- Blender MCP: https://pypi.org/project/blender-mcp/ ; 공식 Blender MCP: https://www.blender.org/lab/mcp-server/
- CC0 자료: https://polyhaven.com/license , https://docs.ambientcg.com/license/ , https://kenney.nl/support , https://quaternius.com/faq.html
- Hunyuan3D-2 라이선스: https://huggingface.co/tencent/Hunyuan3D-2/blob/main/LICENSE
- Figma MCP 한도: https://developers.figma.com/docs/figma-mcp-server/rate-limits-access/
- odiff: https://github.com/dmtrKovalenko/odiff ; Pretendard: https://github.com/orioncactus/pretendard
- hanspell: https://github.com/9beach/hanspell ; draw.io MCP: https://github.com/jgraph/drawio-mcp ; fontTools: https://github.com/fonttools/fonttools
- GameCI: https://game.ci/docs/github/activation
