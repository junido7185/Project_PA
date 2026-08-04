# Project P.A. 비주얼 툴체인 감사

- 감사일: 2026-07-15
- 기준 프로젝트: `Project_PA`
- 메인 씬: `Assets/Scenes/Prototype_FirstDay.unity`
- 원칙: 기능 루프를 유지하면서 Unity Editor API/MCP를 우선하고, 실제 캡처로 확인한다.

## 1. 엔진과 렌더 파이프라인

| 항목 | 확인값 | 판정 |
|---|---|---|
| Unity Editor | 6000.3.2f1, revision `a9779f353c9b` | Unity 6.3 LTS 계열 |
| 그래픽 API | Windows D3D11 | 기존 크래시 정책에 따라 D3D12 사용 금지 |
| Render Pipeline | URP 17.3.0 | `Assets/Settings/PC_RPAsset.asset` 사용 |
| Render Pipeline Core | 17.3.0 | URP 전이 의존성 |
| Shader Graph | 17.3.0 | URP 전이 의존성으로 이미 설치됨 |
| AI Navigation | 2.0.12 | 설치·사용 중. 다수 `NavMeshSurface`/`NavMeshAgent` 존재 |
| Timeline | 1.8.9 | 설치됨 |
| Cinemachine | 없음 | 현재 추적 카메라가 작동하므로 이번 작업에서는 설치하지 않음 |
| Animation Rigging | 없음 | 캐릭터 보정 필요성이 확정될 때만 검토 |

직접 의존성은 `Packages/manifest.json`, 정확한 해시와 전이 의존성은 `Packages/packages-lock.json`이 재현 기준이다. 주요 직접 패키지는 AI Inference 2.4.1, AI Navigation 2.0.12, Input System 1.17.0, URP 17.3.0, Test Framework 1.6.0, Timeline 1.8.9, UGUI 2.0.0, Visual Scripting 1.9.9이다.

## 2. 공식 Unity 제작 도구 판단

| 도구 | 상태 | 이번 판단 |
|---|---|---|
| ProBuilder | 미설치 | B01 기존 노점으로 핵심 문제를 해결할 수 있어 설치하지 않음 |
| Shader Graph | 설치됨 | URP 재질/셰이더 작업에 사용 가능 |
| Animation Rigging | 미설치 | 현재 절차형 보행과 Avatar를 먼저 감사 |
| Cinemachine | 미설치 | 현재 카메라를 이유 없이 교체하지 않음 |
| Timeline | 설치됨 | 상점 진화 연출 단계에서 활용 후보 |
| FBX Exporter | 미설치 | Blender 왕복 작업이 시작될 때만 검토 |
| Recorder | 미설치 | 기존 무손실 Game View 캡처가 있어 중복 설치하지 않음 |
| Splines | 미설치 | 현재 핵심 미완성 문제와 직접 관계 없음 |
| Terrain Tools | 미설치 | 현재 평면형 마을 구조에서는 우선순위 아님 |
| AI Navigation | 설치됨 | NPC 접근·배치 장애물 검증에 계속 사용 |

패키지 수 자체는 진척으로 보지 않는다. 현재 미완성 문제를 직접 해결하지 않는 패키지는 설치하지 않는다.

## 3. Blender

- Blender 실행 파일, PATH, Windows 설치 레지스트리를 확인했으나 설치되지 않았다.
- 따라서 Blender MCP도 설치하지 않았다.
- 이번 중앙 상점 문제는 기존 B01 에셋을 안전하게 재사용해 해결했다.
- 창고/작업대 실루엣 재설계처럼 Unity 설정만으로 해결할 수 없는 작업이 확정되면 Blender 안정판, 라이선스, 내보내기 규약을 별도 체크포인트 뒤 도입한다.

## 4. Codex와 Unity MCP

감사 전에는 전역/프로젝트 Codex MCP 서버가 없었고 프로젝트 `.codex/config.toml`도 없었다.

설치 결과:

| 항목 | 값 |
|---|---|
| Unity 패키지 | `com.coplaydev.unity-mcp` 9.7.0 |
| UPM 출처 | `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v9.7.0` |
| 고정 커밋 | `417cf351a152b483c91e6e2deaf7ae355fa8eff3` |
| Unity 최소 버전 | 패키지 선언 2021.3, 현재 6000.3.2f1에서 컴파일 성공 |
| 라이선스 | MIT |
| 서버 실행 도구 | uv 0.11.28 |
| Codex CLI | 0.144.0-alpha.4 |
| 프로젝트 설정 | `.codex/config.toml` |
| 전송 | Streamable HTTP `http://127.0.0.1:8080/mcp` |
| 서버 범위 | 프로젝트 도구 범위 활성화 |

보안 설정:

- loopback `127.0.0.1`만 사용한다.
- HTTP Remote URL, API key, 인증 토큰은 설정하지 않았다.
- insecure remote HTTP는 비활성화했다.
- `DISABLE_TELEMETRY=true`로 서버/Editor를 실행했다.
- 프로젝트 설정에 비밀값을 기록하지 않았다.
- Codex 쓰기 도구는 승인 대상으로 설정했고 MCP 서버는 `required=false`로 두어 Editor가 닫혀도 Codex 시작을 막지 않는다.

연결 검증:

- Unity 패키지의 Editor/Runtime 어셈블리 컴파일 성공.
- Unity 프로세스와 로컬 서버 사이 WebSocket 연결 확인.
- 서버가 `Project_PA` 인스턴스와 30개 Unity 도구를 등록.
- `codex mcp list`에서 `unityMCP`가 enabled 상태로 확인됨.
- MCP로 활성 씬, 계층, B01 프리팹 계층, 패키지 상태, Cinemachine 부재, Console을 직접 조회.
- MCP로 B01 프리팹 스테이지를 열고 `Logs/VisualAudit/B01_MarketStall_MCP.png`을 캡처한 뒤 저장 없이 닫음.
- MCP로 Play Mode 진입/종료 및 `Logs/VisualAudit/after_b01_mcp.png` 캡처 수행.

알려진 제한:

- `execute_code`에 긴 C# 본문을 전달하면 Windows 경로/명령 길이 제한으로 실패했다. 이 경로는 재시도하지 않고 구조화된 Unity 도구로 대체한다.
- winget 설치 직후 이미 떠 있던 Unity에는 uv PATH가 반영되지 않았다. 이후 Unity 시작 환경에 uv 설치 경로를 포함해 해결했다.

## 5. 3D 에셋과 출처 상태

- 모델 총 324개: FBX 174개, OBJ 150개, GLB/Blend 0개.
- Ultimate Nature Pack 아래 300개 모델이 집중되어 있다.
- 캐릭터 폴더에는 C-01~C-09, Chop, Tripo walking 변환 FBX가 있다.
- 건물 폴더에는 B01~B12 FBX, 대응 텍스처, 프리팹, BuildingData가 있다.
- 상세 판정은 `TRIPO_ASSET_AUDIT.md`, 출처는 `ASSET_AND_TOOL_PROVENANCE.md`를 기준으로 한다.

현재 확인 가능한 라이선스 문서는 Ultimate Nature Pack의 `Assets/Art/Ultimate Nature Pack - Jun 2019/License.txt`이다. Tripo 캐릭터/건물, Froggy Chair, 일부 아이콘은 저장소 안에서 에셋별 원출처·라이선스 증빙을 찾지 못했으므로 최종 배포 전 확인 대상이다.

## 6. 캡처 자동화

초기 주 캡처는 `Assets/Editor/PA_DemoViewCapture.cs`의 `Project PA/Validation/Capture Demo Game View` 메뉴였다. 실제 추적 카메라, 2560×1440, UI 포함, Day 1 오후 조명 조건으로 `Logs/DemoViewShots`에 PNG를 기록한다. 그러나 이 도구를 포함한 일부 Editor 캡처는 별도 RenderTexture에 `Camera.Render()`를 호출하므로 현재는 실행 금지다.

2026-07-17 ThemeCorner와 ShopCustomization에서 같은 URP 네이티브 렌더 충돌이 두 번 발생했다. 일반 Play Mode 프레임의 `ScreenCapture.CaptureScreenshot`으로 바꾼 ThemeCorner 기능 검증은 끝까지 실행됐지만, 다른 검증기의 직접 렌더가 다시 충돌했다. 따라서 저장소 전체의 직접 렌더를 제거하기 전 세 번째 Unity 실행은 금지한다.

Task 116 1차 전환 결과:

- `Assets/Editor`의 실제 직접 `camera.Render()` 호출은 감사 시작 시 12곳이었다.
- ThemeCorner의 일반 GameView 흐름을 공용 `PA_SafeGameViewCapture`로 추출했다.
- 공용 흐름은 1920×1080 요청, Canvas/TMP 갱신, D3D11 안정화 대기, 일반 `ScreenCapture`, 새 PNG freshness/최소 크기 확인, 카메라 target/transform/projection/culling/viewport와 이전 화면 상태 복원을 수행한다.
- 두 번째 충돌 지점 `PA_ShopCustomizationValidator`와 현재 Tier/B07 검증기 `PA_ShopProgressionUnlockValidator`는 공용 비동기 경로로 전환됐고, 두 파일의 실제 직접 렌더 호출은 0이다.
- Character, Cottage, CustomerPanel, DemoView, FinalPresentation, GatheringShop, OutdoorPlacement, ShopEvolution, VillageCulture, Workbench Editor 도구의 10곳은 후속 분할 전환 대상이다.
- 잔여 호출이 0이 되고 사람 판단을 받은 뒤에만 D3D11 격리 GameView 캡처 1회로 새 공용 경로를 확인한다.

Task 117 2차 전환 결과:

- VillageCulture, CustomerPanelLayout, FinalPresentation의 로컬 RenderTexture/`camera.Render()` 캡처를 공용 `PA_SafeGameViewCapture`로 전환했다.
- 세 검증기는 단일 `Task` 실행 가드와 await 순서를 사용한다. 다음 날 변화 3장, 고객 패널 1장, 최종 프레젠테이션 6장이 실제 PNG 완료 뒤 다음 런타임 상태로 넘어간다.
- 시장 마커/FOV 46/전체 레이어/1920×1080 구도를 설정 콜백으로 보존하고, FinalPresentation의 일반·희귀 가격 화면을 포함한 6개 파일 목록도 유지했다.
- 세 대상의 `RenderTexture`·`ReadPixels`·실제 직접 `camera.Render()` 호출은 0이다.
- Character, Cottage, DemoView, GatheringShop, OutdoorPlacement, ShopEvolution, Workbench의 7곳이 마지막 후속 분할 전환 대상으로 남았다.
- Runtime은 경고/오류 0, Editor는 오류 0과 기존 CS8785/CS0414 경고 2개, 교정 정적 계약은 38/38 PASS다. Unity는 실행하지 않았다.

Task 118 3차 전환 결과:

- `PA_DemoViewCapture`는 기존 실내 11초/외부 4.5초 준비 뒤 실제 추적 카메라의 2560×1440 GameView PNG를 기다린다. 별도 검증용 위치로 카메라를 옮기지 않는다.
- `PA_GatheringShopReview`는 해안 채집 전/후, 밤 시장, 고객 반응, 다음 날 정산의 5개 1920×1080 상태를 공용 캡처 완료 순서로 유지한다.
- `PA_OutdoorPlacementValidator`는 기존 1280×720 직교 위치·초점·size 7.2를 보존해 baseline/final 두 장을 기다리고 기존 PNG 최소 크기 판정을 유지한다.
- 세 흐름은 캡처 중 `CameraController`를 일시 비활성화하고 `finally`에서 복원하며, 카메라·화면 상태 복원은 공용 도우미에 위임한다.
- 세 대상의 RenderTexture·ReadPixels·실제 직접 `camera.Render()`는 0이다. 잔여 직접 렌더는 Character, Cottage, ShopEvolution, Workbench 4곳이다.
- Runtime/Editor 오류 0, 정적 계약 35/35 PASS. 반복 네이티브 충돌 경계 때문에 Unity와 MCP 캡처는 실행하지 않았다.

Task 119 4차 전환 결과:

- `PA_CharacterFinalizer`는 1600×900 소스 lineup과 runtime idle/walk를 공용 GameView 완료까지 기다린다. 0.35초 준비, 0.6초 실제 이동, 0.25초 종료와 기존 접지·Avatar·콜라이더·NavMesh·cadence 판정을 보존한다.
- `PA_CottageVisualFinalizer`는 1920×1080 씬 전경, 격리 4방향, 최종, runtime의 7개 파일을 기다린다. 18m 거리·orthographic size 5.5와 renderer 격리/복원을 유지한다.
- `PA_WorkbenchFinalizer`는 감사/최종 각각의 4방향과 선택된 runtime baseline/final을 기다린다. 실제 접근 방향, collider/carving, Wood→Plank 제작, CraftingUI와 clamp/light 피드백 판정을 유지한다.
- 실제 게임 카메라 흐름은 `CameraController`를 일시 비활성화하고 `finally`에서 복원하며, 임시 Workbench 감사 오브젝트와 Cottage renderer 상태도 예외 시 복원한다.
- 세 대상의 RenderTexture·ReadPixels·실제 직접 `camera.Render()`는 0이다. 잔여 직접 렌더는 `PA_ShopEvolutionVisualFinalizer` 1곳이다.
- Runtime/Editor 오류 0, 정적 계약 42/42 PASS. 반복 네이티브 충돌 경계 때문에 Unity와 MCP 캡처는 실행하지 않았다.

Task 120 최종 전환 결과:

- `PA_ShopEvolutionVisualFinalizer`의 B02~B04 소스 감사는 각 4방향, 총 12개 1600×900 GameView PNG를 순서대로 기다린다.
- runtime baseline과 Tier 1~3 after는 단일 `Task` 가드로 진행하며 초기 4초, 단계별 0.75초, orthographic size 6.6, 외관/플레이어 구도와 기존 파일명을 유지한다.
- 실제 게임 카메라 흐름은 `CameraController`를 일시 비활성화하고 `clearFlags`와 함께 `finally`에서 복원한다. 공용 도우미가 카메라 transform/projection/culling/viewport/target과 화면 상태를 복원한다.
- Tier 전후 `ShopCustomizationController.WriteSaveFields` JSON 동등성 판정은 마지막 PNG 완료 뒤 실행돼 성장 연출이 기존 배치를 바꾸지 않는 계약을 유지한다.
- 대상의 RenderTexture·ReadPixels·실제 직접 `camera.Render()`는 0이며 저장소 전체 실제 직접 호출도 0이다. 남은 `Camera.Render()` 문자열 1개는 충돌 원인을 설명하는 주석이다.
- Runtime/Editor 오류 0, 정적 계약 36/36 PASS. 반복 네이티브 충돌 경계 때문에 Unity와 MCP 캡처는 실행하지 않았다.
- 다음 단계는 사람 판단 뒤 격리 D3D11 GameView PNG 1회를 생성해 freshness·파일 크기·UI 가독성·카메라/화면 복원을 확인하는 것이다. 성공 전에는 전체 검증기를 실행하지 않는다.

Unity MCP의 구조화된 Game View 캡처는 직접 `Camera.Render()` 구현과 별개지만, 현재는 프로젝트 Unity 자체의 세 번째 실행을 하지 않는다. 전수 전환 뒤 반복 비주얼 검토에서는 MCP 캡처를 우선하고, 자동 검증기는 공용 GameView 도우미만 사용한다.

이번 기준샷:

- 변경 전: `Logs/DemoViewShots/shot_20260715_164534.png`
- B01 프리팹: `Logs/VisualAudit/B01_MarketStall_MCP.png`
- Play Mode 1차 확인: `Logs/VisualAudit/after_b01_mcp.png`
- 동일 구도 최종: `Logs/DemoViewShots/shot_20260715_171613.png`

## 7. 이번 실제 적용

중앙 데모 상점은 실제 B01 노점이 아니라 `Support_Crate`, `Shop_Tent_Kit`, `Sales_Tent_Preview`와 런타임 큐브 판매대로 보였다. MCP 프리팹 스테이지에서 B01이 현재 캐릭터/마을에 맞는 목재·줄무늬 차양 노점임을 확인한 뒤 `DemoVisualDressingController`가 B01의 `Visual`만 기존 ShopSlot에 정렬하도록 변경했다.

판매/경제/저장/ShopSlot은 유지하고 임시 박스의 Renderer와 Collider만 런타임에서 비활성화한다. 메인 씬과 B01 프리팹은 수정하거나 저장하지 않았다.

동일 구도 비교에서 변경 전의 큰 갈색 큐브와 검은 렌더 아티팩트가 사라지고, 변경 후에는 목재 프레임·줄무늬 차양·실제 상품 접근면이 중앙 초점으로 읽혔다. D3D11 `PA_FinalDemoRouteValidator`도 `stocked=BreadLoaf, paid=30G`로 통과했다(`Logs/Codex_VisualToolchain_FinalRouteRegression.log`).
