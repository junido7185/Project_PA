# BUG_LOG — 버그·실패 기록

AI가 작업 중 만난 버그, 2회 실패로 중단한 문제, 발견했지만 고치지 않은 문제를 기록한다.
**규칙**: 실패 시 여기에 기록하고 멈춘다. 남의 버그를 발견해도 직접 고치지 말고 기록만 한다.

형식:

```
## [상태] YYYY-MM-DD — 짧은 제목
- 증상:
- 재현 방법:
- 원인 추정:
- 시도한 것:
- 중단 이유 / 필요한 판단:
- 관련 파일:
```

상태: `OPEN`(미해결) / `BLOCKED`(사람 판단 대기) / `RESOLVED`(해결, 해결 방법 기록) / `WONTFIX`(수정 안 함, 이유 기록)

---

## [BLOCKED] 2026-07-17 — 직접 Camera.Render 검증 경로의 Unity 네이티브 렌더 크래시

- 증상: `PA_ThemeCornerValidator` 첫 D3D11 Play Mode 실행이 빈 진열대 코너 0/월드 라벨 0 판정까지 통과한 뒤 첫 1920×1080 캡처의 `Camera.Render()`에서 Unity 프로세스 자체가 충돌했다. 관리 예외나 C# assertion 실패가 아니다.
- 재현 방법: Unity 6000.3.2f1, `-batchmode -nographics -force-d3d11`, `PA_ThemeCornerValidator.RunThemeCornerValidation`. 로그 `Logs/Codex_Task086_ThemeCorner.log`; 스택은 `GfxDevice::DrawSharedGeometryJobs` → URP `RenderSingleCamera` → `Camera.Render` → `PA_ThemeCornerValidator.cs:364`. 크래시 폴더 `C:/Users/sdjsd/AppData/Local/Temp/Unity/Editor/Crashes/Crash_2026-07-17_033941998`.
- 원인 추정: 검증기에서 Play Mode 프레임 렌더와 별도로 URP 카메라를 직접 `Camera.Render()`한 경로의 네이티브 D3D11 렌더 충돌. 테마 코너 C# 로직 실패 근거는 없다.
- 시도한 것: ThemeCorner 검증기의 직접 렌더를 일반 GameView `ScreenCapture`로 교체했다. 전용 D3D11 기능 검증은 Raw/Processed 인접, 대각선·간격·혼합 제외, 품절/보충, 회수/이동, 실제 판매 3건, Processed 마을 신호, v10 로드 재파생까지 PASS했다. 최종 기능 로그는 `Logs/Codex_Task086_ThemeCorner_CaptureFinal.log`이다.
- 두 번째 발생: 기존 회귀 `PA_ShopCustomizationValidator.RunShopCustomizationValidation`의 `PA_ShopCustomizationValidator.cs:388` 직접 `Camera.Render()`에서 같은 `GfxDevice::DrawSharedGeometryJobs`/`DrawLineOrTrailMultipleFromNodeQueue` 네이티브 충돌이 재현됐다. 로그 `Logs/Codex_Task086_ShopCustomizationRegression_Final.log`, 크래시 `Crash_2026-07-17_040714511`.
- 중단 이유 / 필요한 판단: 같은 네이티브 원인이 두 번 발생했으므로 세 번째 Unity 실행은 금지한다. 다음 작업은 특정 기능 재시도가 아니라 validator registry 전체에서 직접 `Camera.Render()` 캡처를 감사하고, 일반 GameView/Recorder 등 한 가지 안전한 경로로 교체하는 별도 단일 작업이어야 한다.
- 확인된 범위: ThemeCorner 기능 assertions와 Runtime/Editor 컴파일은 PASS. 깨끗한 none/raw/processed 동일 구도 이미지는 서로 다른 성공 실행에서 확보했다. 다만 ScreenCapture도 간헐적으로 검은 TMP/Canvas 프레임을 기록해 한 실행의 3장 모두가 안정적이라는 증거는 없다.
- 영향/관련 파일: Task 086은 기능 구현을 보존하지만 ShopCustomization/SaveRoundTrip/FinalDemoRoute 회귀와 플레이어 기본 화면의 최종 라벨 가독성은 확인 못 해 PARTIAL이다. 씬·프리팹·패키지·저장 스키마·그래픽 설정은 변경하지 않았다. 상세 `PROJECT_PA_CRASH_REPORT_20260717.md`.

## [RESOLVED] 2026-06-25 — Unity Editor D3D12 그래픽 디바이스 크래시

- 증상: Editor 실행 중 `d3d12: Device removed (887a0006)` 크래시 반복 (3회 연속).
- 원인 추정: D3D12 스왑체인 present 단계의 네이티브 그래픽 디바이스 오류. C# 코드와 무관.
- 해결: 사용자가 `-force-d3d11` 실행으로 안정성 수동 확인. D3D11이 승인된 baseline.
- 잔여 규칙: **D3D12 미승인.** 자동 Unity 실행은 D3D11에서만. 상세: 루트 `PROJECT_PA_CRASH_REPORT_20260625.md` (이동 금지 — preflight glob 대상), `Automation/LoopEngineering/State/crash-resolution.json`.

## [RESOLVED] 2026-06-26 — 배치 검증기 2종 실행 보류

- 증상: `PA_FinalDemoRouteValidator`, `PA_LongPlayProgressionValidator` 배치 실행 불가.
- 원인: Unity Editor가 같은 프로젝트로 열려 있어 batchmode 금지 규칙에 걸림.
- 해결: Editor가 닫힌 상태에서 D3D11 batchmode로 두 검증기를 실행했다. `PA_FinalDemoRouteValidator`는 2026-07-13에도 `paid=30G`로 재통과했고, `PA_LongPlayProgressionValidator`는 2026-07-12 `money=4633G` 기준선으로 통과했다.
- 증거: `Logs/Fable_V3_FinalDemoRoute.log`, `Logs/Fable_VisualPass_LongPlayRegression.log`.
- 관련 파일: `PROJECT_PA_TODO.md` Day 1-3 스프린트 미완 항목.

## [RESOLVED] 2026-07-15 — Task 039 낚시 자식 콜라이더 생성 실패

- 증상: D3D11 `PA_GatheringShopGateValidator` 실행 중 `FishingInteraction`에 `BoxCollider`가 없다는 `MissingComponentException`이 발생했고, 이어서 전용 `FishingSpot` 미발견으로 검증이 실패했다.
- 재현 방법: Unity 6000.3.2f1을 `-batchmode -force-d3d11`로 열어 `PA_GatheringShopGateValidator.RunGatheringShopGateValidation`을 실행한다. 로그: `Logs/Codex_Task039_Fishing.log`.
- 원인 추정: `DayNightShopLoopController.EnsureFishingSpot`의 `GetComponent<BoxCollider>() ?? AddComponent<BoxCollider>()`가 Unity의 특수 null 객체를 일반 C# null로 판정하지 못해 `AddComponent` 분기가 실행되지 않았다. 같은 형태의 `FishingSpot` 생성도 영향 범위다.
- 시도한 것: Task 039 런타임 사이드카와 검증기를 구현하고 Unity 스크립트 컴파일을 통과한 뒤 D3D11 Play Mode 검증 1회를 실행했다. 실패 뒤에는 수정 재시도를 하지 않았다.
- 해결: 다음 연속 작업에서 두 null 병합 연산자를 명시적 Unity null 검사와 분리된 `AddComponent` 호출로 교체했다. D3D11 `PA_GatheringShopGateValidator`가 전용 `FishingSpot` 발견부터 Fish 2개 지급·일일 제한·다음 날 리셋·저장 복원까지 PASS했고, FinalDemoRoute도 30G 판매로 재통과했다.
- 증거: `Logs/Codex_Task039_Fishing.log`, `Logs/Codex_Task039_FinalRouteRegression.log`.
- 관련 파일: `Assets/Scripts/DayNightShopLoopController.cs`, `Assets/Scripts/FishingSpot.cs`, `Assets/Editor/PA_GatheringShopGateValidator.cs`.

## [WONTFIX] 2026-07-15 — Unity MCP execute_code 긴 본문 Windows 명령 길이 실패

- 증상: MCP `execute_code`로 B01 프리팹과 씬 Renderer Bounds를 읽는 긴 C# 본문을 실행하면 Mono 실행 단계에서 `파일 이름이나 확장명이 너무 깁니다`로 실패했다.
- 재현 방법: Unity MCP 9.7.0 `execute_code(action=execute)`에 두 개의 장문 측정 코드를 batch로 전달한다. Unity 로그 `Logs/UnityMcpConnected2.log`의 `[ExecuteCode] Execution failed` 기록.
- 원인 추정: 프로젝트 코드가 아니라 Windows에서 MCP 임시 컴파일 명령/인수 길이가 한도를 넘은 도구 경로 문제다.
- 시도한 것: 1회 실행 후 같은 경로를 중단했다. `manage_scene`, `manage_asset`, `manage_prefabs`, `manage_camera` 구조화 도구로 계층·프리팹·캡처를 대체했다.
- 중단 이유 / 필요한 판단: 구조화 도구가 필요한 정보를 제공하므로 프로젝트에 임시 Editor 스크립트를 추가하거나 패키지 코드를 수정할 가치가 없다. 짧은 `execute_code` 사용 가능성은 별도지만 이번 작업에서는 사용하지 않는다.
- 관련 파일: `Docs/Codex/VISUAL_TOOLCHAIN.md`, `Logs/UnityMcpConnected2.log`.

## [RESOLVED] 2026-07-15 — FinalDemoRoute 첫 batch 호출이 검증 전 종료

- 증상: 첫 D3D11 호출이 `PA Final Route: entering Play Mode` 직후 종료되어 PASS/FAIL 마커를 만들지 않았다.
- 원인: 자체적으로 Play Mode와 `EditorApplication.Exit`을 관리하는 검증기에 일반 batch 종료용 `-quit`를 함께 전달해 조기 종료했다.
- 해결: 같은 검증기를 `-quit` 없이 1회 재실행했다. `stocked=BreadLoaf, paid=30G`와 `PA Final Route Validation finished successfully`를 확인했다.
- 증거: `Logs/Codex_VisualToolchain_FinalRouteRegression.log`.

## [RESOLVED] 2026-07-16 — ShopCustomization batch ScreenCapture 파일 미생성

- 증상: D3D11 `PA_ShopCustomizationValidator`에서 배치·이동·판매·v10 저장/복원 검사는 모두 통과했지만 `ScreenCapture.CaptureScreenshot` 호출 후 PNG 파일이 생성되지 않아 마지막 검사만 실패했다.
- 재현 방법: Unity 6000.3.2f1을 `-batchmode -force-d3d11`로 열고 `PA_ShopCustomizationValidator.RunShopCustomizationValidation`을 실행한다.
- 원인 추정: batch Editor의 비동기 `ScreenCapture.CaptureScreenshot`이 종료 전 프레임에서 디스크 쓰기를 완료하지 않는다. 기능 코드나 카메라 미존재 문제는 아니다.
- 시도한 것: 첫 검증 실행 1회. 동일 경로 반복은 중단했다.
- 해결: 메인 게임 카메라를 RenderTexture에 명시적으로 렌더링하고 `Texture2D.EncodeToPNG`로 동기 기록했다. 이어 실내 벽 안쪽 아이소메트릭 구도와 카메라 Canvas를 적용해 장부 UI까지 포함했다.
- 검증: D3D11 최종 실행에서 배치·판매·v10 저장/복원과 PNG 생성 모두 PASS. 직접 캡처를 확인해 남쪽 벽 가림과 UI 여백도 보정했다.
- 관련 파일: `Assets/Editor/PA_ShopCustomizationValidator.cs`, `Logs/ShopCustomizationValidator_FinalUI.log`, `Logs/ShopCustomization/20260716_103307/shop_customization_game_camera.png`.

## [RESOLVED] 2026-07-16 — BuildManager 광역 패치가 깨진 주석 문맥을 찾지 못함

- 증상: `BuildManager.cs`의 메서드 전체를 바꾸는 첫 `apply_patch`와 같은 문맥의 두 번째 패치가 verification failed로 중단됐다. 두 실행 모두 대상 파일에는 변경을 적용하지 않았다.
- 재현 방법: 콘솔에서 깨져 보이는 기존 한글 주석을 패치의 필수 문맥으로 포함해 `BuildManager.Update`/`BuildIt` 블록을 교체한다.
- 원인 추정: 파일의 기존 인코딩과 도구 출력에서 깨져 보인 주석 문자열이 정확히 일치하지 않았다. 새 야외 배치 로직 자체나 Unity 컴파일 문제는 아니다.
- 시도한 것: 같은 광역 문맥 방식은 2회 뒤 중단했다. 세 번째 동일 시도는 하지 않았다.
- 해결: ASCII 코드 줄만 최소 문맥으로 사용해 필드·메서드 단위로 패치했다. 이어 Unity D3D11 컴파일/Play 검증과 세 회귀 검증이 모두 PASS했다.
- 관련 파일: `Assets/Scripts/BuildManager.cs`, `Logs/OutdoorPlacementValidator.log`.

## [RESOLVED] 2026-07-16 — B10 전용 간판 검증이 TMP 렌더러를 에셋 렌더러로 중복 집계

- 증상: `PA_CottageVisualFinalizer.RunFinalValidationBatch`에서 B10 3채 정면 정렬, 레거시 중복 비활성화, 실제 문 연결까지 성공한 뒤 `purpose-built shop sign mesh is attached` 검사가 실패했다.
- 재현 방법: Unity 6000.3.2f1 D3D11 batch에서 위 검증 메서드를 실행한다. 로그: `Logs/B10_CottageFinalValidation.log`.
- 원인 추정: 전용 간판 루트의 `MeshRenderer`와 재사용한 `PrototypeWorldLabel`/TMP 자식 `MeshRenderer`를 `GetComponentsInChildren<MeshRenderer>`가 함께 반환했는데, 검증식이 정확히 1개만 허용했다.
- 시도한 것: 간판 메시·프리팹 생성과 첫 최종 검증 1회. 런타임 구현 로그는 정상이고 검증 어설션만 실패했다.
- 해결: 구현은 바꾸지 않고 검증 범위를 간판 루트의 전용 `MeshFilter`/`MeshRenderer`와 메시 이름으로 좁혔다. 두 번째 D3D11 검증에서 에셋 생성·B10 정렬·중복 제거·After 캡처가 모두 PASS했다.
- 관련 파일: `Assets/Editor/PA_CottageVisualFinalizer.cs`, `Assets/Resources/VisualFinalization/B10_Cottage_ShopSign.prefab`.

## [RESOLVED] 2026-07-16 — B10 Tier 간판 텍스트 재부착이 ShopEvolution 탐색 계약을 이탈

- 증상: B10 최종 에셋의 편집 모드 검증은 PASS했지만 Play Mode `PA_EnterableShopValidator`가 `exterior store sign exists`에서 실패했다.
- 재현 방법: Unity 6000.3.2f1 D3D11 batch에서 `PA_EnterableShopValidator.RunEnterableShopValidation`을 실행한다. 로그: `Logs/B10_EnterableShopRegression.log`.
- 원인 추정: `PrototypeWorldLabel`을 전용 간판과 함께 Cottage 자식으로 재부착해, `ShopEvolutionController`와 기존 검증기의 `PA_StoreDoor_Out.GetComponentInChildren<PrototypeWorldLabel>` 계약에서 벗어났다.
- 시도한 것: 실제 Play Mode Tier 0 잠금→Tier 1 왕복 회귀 1회. B10 정면 정렬 로그와 문 컴포넌트 검사는 통과했고 간판 탐색에서 중단됐다.
- 해결: `PA_StoreDoor_Out` 아래 비균일 스케일을 상쇄하는 빈 시각 앵커를 두고 전용 간판 메시와 텍스트를 함께 자식으로 배치했다. 문 하위 탐색 계약을 복원했고, 고정 3D 간판에서 빌보드를 끄고 텍스트 Rect 폭/overflow를 지정했다. 두 번째 D3D11 왕복 검증은 Tier 0 잠금→Tier 1 OPEN→입장→진열→가격→퇴장까지 PASS했다.
- 관련 파일: `Assets/Scripts/CottageVisualFinalizationController.cs`, `Assets/Scripts/ShopEvolutionController.cs`, `Logs/B10_EnterableShopRegression.log`.

## [RESOLVED] 2026-07-16 — B05 비동기 Play Mode 기준 캡처가 `-quit`로 조기 종료

- 증상: `PA_WorkbenchFinalizer.CaptureRuntimeBaselineBatch`가 Play Mode 진입을 예약한 직후 Unity가 종료되어 기준 PNG와 완료 마커를 만들지 못했다.
- 재현 방법: Unity 6000.3.2f1 D3D11 batch에서 비동기 Play Mode/`EditorApplication.Exit` 자체 관리 메서드에 `-quit`를 함께 전달한다. 로그: `Logs/B05_WorkbenchRuntimeBaseline.log`.
- 원인 추정: 검증기가 다음 Play Mode 프레임에서 캡처하고 스스로 종료해야 하는데, 일반 batch 종료 옵션이 먼저 Editor를 닫았다. B05 런타임 기능이나 카메라 오류가 아니다.
- 시도한 것: 기준 캡처 첫 실행 1회. 동일 인수 반복은 중단했다.
- 해결: `-quit`를 제거하고 검증기 자체 종료를 기다렸다. 재실행에서 실제 MainCamera 캡처와 플레이어/B05 위치 마커를 생성했고, interaction 면 기준 최종 기준 캡처까지 PASS했다.
- 증거: `Logs/B05_WorkbenchRuntimeBaseline_Retry.log`, `Logs/B05_WorkbenchRuntimeBaseline_AccessFace.log`, `Logs/B05_WorkbenchAudit/b05_runtime_before.png`.

## [RESOLVED] 2026-07-16 — B05 제작 검증기가 재료 문자열의 Plank를 출력 레시피로 오인

- 증상: 실제 Workbench/CraftingUI는 정상적으로 열렸지만 첫 최종 검증이 Tier 2 Furniture 레시피 버튼을 선택해 Wood→Plank 기대값을 만족하지 못했다.
- 재현 방법: Unity 6000.3.2f1 D3D11에서 `PA_WorkbenchFinalizer.RunFinalPlayValidationBatch`를 실행한다. 로그: `Logs/B05_WorkbenchFinalPlayValidation.log`.
- 원인 추정: 버튼 전체 텍스트에서 `Plank` 포함 여부만 검사해, 출력명이 아니라 재료 설명에 Plank가 포함된 Furniture 버튼을 먼저 선택했다. 구현 문제가 아니라 검증기 선택자 범위 문제다.
- 시도한 것: 파생 에셋/런타임 정렬 구현 뒤 첫 실제 제작 검증 1회. 같은 선택자를 반복하지 않았다.
- 해결: 버튼 라벨이 정확히 `Plank\n`으로 시작하는지 검사하도록 좁혔다. 재시도와 광량 조정 뒤 최종 실행 모두 Wood 2개 소비, Plank 1개 생성, clamp/light 피드백, UI 닫기까지 PASS했다.
- 증거: `Logs/B05_WorkbenchFinalPlayValidation_Retry.log`, `Logs/B05_WorkbenchFinalPlayValidation_Final.log`.

## [RESOLVED] 2026-07-16 — 캐릭터 최종 검증에서 플레이어 보행 재생 속도가 1로 유지

- 증상: NPC 8명의 실제 높이·접지·CapsuleCollider·NavMeshAgent·Humanoid Avatar·그림자 검사는 idle 단계에서 모두 통과했지만, walking 단계의 첫 검사에서 플레이어 `Animator.speed`가 1.0으로 남아 `1.45~2.75` 기대 범위를 만족하지 못했다. 규칙에 따라 NPC walking 검사와 After 보행 캡처 전에 중단했다.
- 재현 방법: Unity 6000.3.2f1 D3D11 batch에서 `PA_CharacterFinalizer.RunFinalValidation`을 실행한다. 로그: `Logs/CharacterFinalization_RuntimeFinal.log`.
- 원인 추정: 메인 씬의 시작 온보딩이 `Time.timeScale=0`으로 세계를 정지시킨 상태에서 검증기가 이를 해제하지 않았다. 새 `PlayerFootIkStabilizer.Update`는 `Time.deltaTime`으로 재생 속도를 보간하므로 검증 스테이징 중 1.0에서 진행하지 않았고, 같은 조건이면 NavMeshAgent도 이동하지 않는다. 기능 구현 자체의 실패인지는 아직 확인하지 못했다.
- 시도한 것: 신규 코드 Unity 컴파일 PASS 후 실제 Play Mode 최종 검증 1회. 동일 조건 재시도나 검증기 수정은 하지 않았다.
- 해결: `PA_CharacterFinalizer`가 기존 검증기와 동일하게 `PlayableDayScenarioController.RestoreSavedSession`을 호출해 시작 모달을 닫고 `Time.timeScale=1`을 명시한다. 런타임 캐릭터 코드는 추가로 바꾸지 않았다. 캡처 스테이징은 실제 동서 도로의 빈 구간에서 주민 8명 한 줄 뒤 플레이어를 맨 오른쪽에 두어 속도 차이에 따른 추월·오브젝트 가림을 줄였다.
- 현재 확인된 개선: 주민 높이 `1.748~1.751m`, 발 오프셋 `+0.032~+0.035m`, Capsule `1.8/0.4`, Agent `1.8/0.4/0.75`; `character_idle_after.png`에서 플레이어 1.85m와 주민 1.75m의 비율·접지가 이전보다 일관된다.
- 검증: D3D11 최종 walking에서 `timeScale=1`, 플레이어 `Animator/CurrentPlaybackSpeed=2.25`, NPC 8명 속도 `2.5m/s`, cadence `1.851~2.328`, 보행 중 높이·접지·Collider·Agent가 모두 PASS했다. 같은 게임 카메라 `character_walk_after.png`를 직접 확인했고 InteriorCustomer, CustomerArrival, FinalDemoRoute 30G 회귀도 PASS했다.
- 관련 파일: `Assets/Scripts/NpcPresentationNormalizer.cs`, `Assets/Scripts/NpcHumanoidProceduralAnimator.cs`, `Assets/Scripts/PlayerFootIkStabilizer.cs`, `Assets/Editor/PA_CharacterFinalizer.cs`, `Logs/CharacterFinalization/character_idle_after.png`, `character_walk_after.png`, `Logs/CharacterFinalization_RuntimeFinal_FinalFraming.log`, `Logs/CharacterFinalization_*Regression.log`.

## [RESOLVED] 2026-07-16 — B02~B04 에셋 감사 도구 Unity 컴파일 네임스페이스 누락

- 증상: 첫 D3D11 에셋 감사 실행이 `PA_ShopEvolutionVisualFinalizer.cs`의 `NavMeshObstacle` 형식을 찾지 못해 캡처 전에 중단됐다.
- 재현 방법: Unity 6000.3.2f1 D3D11 batch에서 `PA_ShopEvolutionVisualFinalizer.RunAssetAuditBatch`를 실행한다. 로그: `Logs/ShopEvolution_AssetAudit.log`.
- 원인 추정: 로컬 `dotnet build` 프로젝트가 전역 참조로 허용한 형식이지만 Unity Editor 어셈블리에는 `UnityEngine.AI` using이 명시되어 있지 않았다.
- 시도한 것: 감사 도구 생성 뒤 로컬 C# 빌드 PASS, 첫 Unity Editor 실행 1회. 같은 소스로 반복 실행하지 않았다.
- 해결: `UnityEngine.AI` using을 명시하고 D3D11 감사를 1회 재시도했다. B02~B04 메시/지면/래퍼 Shop·슬롯·콜라이더 감사와 12장 4면 캡처가 모두 PASS했다.
- 증거: `Logs/ShopEvolution_AssetAudit_Retry.log`, `Logs/ShopEvolutionAudit/b02_minus_x.png` 등 12장.
- 관련 파일: `Assets/Editor/PA_ShopEvolutionVisualFinalizer.cs`, `Logs/ShopEvolution_AssetAudit.log`.

## [RESOLVED] 2026-07-16 — 상점 진화 Play Mode 기준 캡처의 Tier 배열 도메인 재로드 누락

- 증상: 에셋 감사 PASS 뒤 첫 런타임 기준 캡처가 상점 배치 상태를 읽은 다음 `IndexOutOfRangeException`으로 중단됐다.
- 재현 방법: Unity 6000.3.2f1 D3D11 batch에서 `PA_ShopEvolutionVisualFinalizer.CaptureRuntimeBaselineBatch`를 실행한다. 로그: `Logs/ShopEvolution_RuntimeBaseline.log`.
- 원인 추정: Enter Play Mode 도메인 재로드 뒤 SessionState의 mode는 복원했지만 정적 `_runtimeTiers` 배열은 `Array.Empty<int>()`로 다시 초기화됐다.
- 시도한 것: 첫 런타임 기준 캡처 1회. Tier 강제 설정이나 게임 카메라 캡처 전에 감사 도구만 실패했다.
- 해결: 정적 생성자에서 SessionState mode를 읽어 Tier 배열을 재구성했다. 두 번째 D3D11 실행에서 Tier 1 기준 캡처, 상점 배치 저장 필드 보존, 정상 종료가 PASS했다.
- 증거: `Logs/ShopEvolution_RuntimeBaseline_Retry.log`, `Logs/ShopEvolutionAudit/shop_evolution_runtime_before.png`.
- 관련 파일: `Assets/Editor/PA_ShopEvolutionVisualFinalizer.cs`, `Logs/ShopEvolution_RuntimeBaseline.log`.

## [RESOLVED] 2026-07-17 — P5 상점 확장 조명이 `Light` 컴포넌트 없이 참조됨

- 증상: 첫 P5 D3D11 Play Mode 검증에서 `ShopCustomizationController.Start`가 완료되기 전 `MissingComponentException`이 발생했다. 검증기는 `shop customization controller initialized`에서 중단됐다.
- 재현 방법: Unity 6000.3.2f1 D3D11 batch에서 `PA_ShopProgressionUnlockValidator.RunShopProgressionUnlockValidation`을 실행한다. 로그 `Logs/P5_ShopProgression_D3D11.log` 581행 부근.
- 원인: `EnsureExpansionLight`의 `host.GetComponent<Light>() ?? host.AddComponent<Light>()`가 UnityEngine.Object의 가짜 null 참조를 정상 null로 판별하지 못해, 컴포넌트가 없는 래퍼를 그대로 사용했다. 첫 접근 `light.type`에서 예외가 발생했다.
- 시도한 것: 새 P5 검증기 첫 실행 1회. 같은 바이너리로 재시도하지 않았다.
- 해결: null 병합 연산자를 제거하고 `Light light = host.GetComponent<Light>(); if (light == null) light = host.AddComponent<Light>();`처럼 Unity의 오버로드된 null 비교를 명시했다. 이어 물리 진열 한도는 중복된 레거시 `Resources/Tiers` 값과 분리해 `6/6/8/12/20` 계약으로 고정하고, 동·북 확장 NavMesh를 기존 실내 island에 양방향 `NavMeshLink`로 연결했다.
- 후속 회귀에서 숨은 선반 템플릿까지 실내 `ShopSlot`으로 집계되는 첫 불일치가 발견됐다. 템플릿 루트를 상점 hierarchy 밖의 비활성 전용 루트로 옮겨 실제 기본 진열대는 계속 6개로 유지하고, 배치된 복제본만 기능 슬롯으로 등록되게 했다.
- 검증: 최종 D3D11 P5 실행이 Tier 0→4, `5×4→6×5→7×6`, 진열대 `6→8→12→20`, B05~B08 보상, 확장 완전 경로, 동적 선반, Processed 테마, v10 저장·복원을 모두 PASS했다. ShopCustomization, EnterableShop, SaveRoundTrip, FinalDemoRoute 회귀도 PASS했다.
- 증거: `Logs/P5_ShopProgression_D3D11_Release.log`, `Logs/ShopProgressionUnlock/20260717_005831/`, `Logs/P5_ShopCustomizationRegression.log`, `Logs/P5_EnterableShopRegression_Retry.log`, `Logs/P5_SaveRoundTripRegression.log`, `Logs/P5_FinalDemoRouteRegression.log`.
- 관련 파일: `Assets/Scripts/ShopCustomizationController.cs`, `Assets/Editor/PA_ShopProgressionUnlockValidator.cs`.

## [OPEN] 2026-07-17 — Task 044 정적 검증의 Tailor 소스 선택자 false negative

- 증상: Task 044 문서 자기검토용 PowerShell 검사에서 `PA_SceneAutoBuilder`의 Tailor→Bread 불일치 항목만 False가 되어 전체 정적 검증이 중단됐다.
- 실제 소스 증거: 앞선 `rg -n -C 5` 출력에서 `("Tailor", ...)`, 다음 줄 `WorkbenchType.SewingTable, "Bread", "HomePoint_Tailor"`가 직접 확인됐다. 설계 문서의 불일치 기록과는 모순되지 않는다.
- 원인 추정: PowerShell 안에서 `rg`에 전달한 이스케이프 패턴 `\("Tailor"`가 의도한 리터럴 시작부를 선택하지 못했다. 컨텍스트 범위를 2→3으로 늘려도 동일 selector라 결과가 변하지 않았다.
- 시도한 것: 같은 선택자를 사용한 자동 검사 2회. 첫 시도는 `-A 2`, 두 번째는 `-A 3`이었다.
- 중단: 같은 조건 2회 실패 규칙에 따라 세 번째 자동 검사와 추가 선택자 디버깅을 하지 않았다. 코드·씬·에셋에는 영향이 없다.
- 후속: 다음 문서/검증 작업에서 고정 문자열 검색(`rg -F`) 또는 행 범위 직접 판독으로 선택자를 교체하고 전체 정적 체크를 한 번 실행한다.
- 관련 파일: `AI_WORKFLOW/03_TASKS/DESIGN_RESIDENT_REQUEST.md`, `Assets/Editor/PA_SceneAutoBuilder.cs`.
## [RESOLVED] 2026-07-17 — Task 046 보조 로그 검색의 Windows `rg` 경로 와일드카드 오류

- 증상: Task 046의 기존 검증 로그를 추가 선별하는 보조 명령에서 `rg`에 `Logs/*.log`, 이어서 `Logs/Fable_T057*`를 검색 경로로 직접 넘기자 Windows가 두 경로를 리터럴로 처리해 `os error 123`으로 종료했다.
- 실제 조사 상태: `SalesLogManager`, `VillageChangeSignalController`, `VillageCultureVisualController`, `ShopSlot`, 저장 코드와 핵심 검증 로그는 이미 개별 경로 및 `-g '*.log'` 검색으로 확인했다. `PA Village Change Signal Validation passed. summary=Processed: 2 sale(s), 76G influence`, Fish/Ore Raw 판매 왕복, Processed 다음 날 시각 변화, v9 문화 상태 저장 왕복 근거는 확보했다.
- 원인: ripgrep의 검색 경로 인수에 PowerShell/Windows가 확장하지 않는 와일드카드를 직접 사용했다. 두 번째 명령에서도 같은 잘못된 경로 와일드카드 패턴을 반복했다.
- 시도한 것: 첫 번째 `Logs/*.log` 실패 후 `-g '*.log'`는 정상적으로 SaveRoundTrip PASS 로그를 찾았지만, 같은 명령 뒤쪽의 `Logs/Fable_T057*`가 다시 실패했다. 같은 원인 2회 실패이므로 세 번째 시도는 하지 않았다.
- 영향: 코드·씬·프리팹·에셋·패키지·저장 스키마는 변경되지 않았다. Task 046의 `VILLAGE_TREND.md`와 종료 문서는 아직 작성하지 않았다.
- 재개 조건: 경로 와일드카드를 인수로 넘기지 말고 `rg -g 'Fable_T057*.log' <pattern> Logs`처럼 glob 필터를 쓰거나, `Get-ChildItem`이 반환한 명시 파일 경로만 전달한다. 이미 확보한 조사 결과를 재사용하고 같은 선택자를 재시도하지 않는다.
## [RESOLVED] 2026-07-17 — Task 046 문서 고정 문자열 검사의 Markdown 백틱 누락

- 증상: `VILLAGE_TREND.md` 초안의 필수 문구를 확인하는 PowerShell 검사에서 `SaleRecord 1건은 ...` 문자열을 찾지 못해 두 번 종료했다.
- 실제 문서: 문서에는 `` `SaleRecord` 1건은 **상품 1개가 아니라 성공한 ShopSlot 거래 1건**이다.``가 존재한다. 기능 내용 누락이 아니라 검사 선택자의 불일치다.
- 원인: 검사 문자열에서 `SaleRecord`를 감싸는 Markdown 백틱을 빠뜨렸다. 첫 실패를 PowerShell UTF-8 기본 해석 문제로 오판해 `-Encoding UTF8`만 추가했고, 같은 잘못된 문자열을 다시 사용했다.
- 시도한 것: 기본 `Get-Content -Raw` 검사 1회, `-Encoding UTF8` 검사 1회. 같은 누락 조건 2회 실패이므로 세 번째 검사는 실행하지 않았다.
- 영향: 코드·씬·프리팹·에셋·패키지·저장 스키마 영향 없음. `VILLAGE_TREND.md` 초안과 소스/로그 감사 결과는 보존했지만 Task 046 완료 표시는 철회했고 종료 문서는 갱신하지 않았다.
- 재개 조건: 해당 문구를 다시 선택하지 말고 다른 독립 표식(예: `paidAmount = EffectiveDisplayPrice × 진열 스택 수량`)과 섹션 제목을 사용해 한 번만 정적 검사한다. 이미 실패한 선택자는 재사용하지 않는다.
- 해결: 실패한 `SaleRecord` 문장 선택자를 재사용하지 않고 섹션 제목·수식·명시 소스/로그 표식으로 검증을 교체했다. 새 검증에서 문서 7개·소스 20개·로그 6개 표식이 PASS했다.

## [RESOLVED] 2026-07-17 — Task 046 독립 정적 검증의 SalesLogManager 소스 경로 오지정

- 증상: 실패했던 문장 선택자를 폐기하고 독립적인 문서·소스·로그 표식으로 실행한 정적 검증이 첫 소스 검사에서 중단됐다. `Assets/Scripts/Core/SalesLogManager.cs`를 읽으려 했으나 해당 경로가 존재하지 않았다.
- 원인: 이미 조사한 클래스의 실제 파일 경로를 다시 확인하지 않고 추정 경로를 검증 스크립트에 적었다. 문서 내용이나 게임 구현의 실패가 아니라 검증 입력 경로 오류다.
- 시도한 것: 독립 표식 검증 1회. 경로를 바꾼 재시도는 하지 않았다.
- 영향: 코드·씬·프리팹·에셋·패키지·저장 스키마 영향 없음. `VILLAGE_TREND.md` 초안은 보존하며 Task 046은 PARTIAL 상태로 유지한다.
- 재개 조건: `rg --files Assets`로 클래스별 실제 경로를 먼저 확정한 뒤, 명시 경로만 사용하는 정적 검증을 새로 실행한다. 존재하지 않는 `Assets/Scripts/Core/SalesLogManager.cs`는 재사용하지 않는다.
- 해결: `rg --files Assets`로 `SalesLogManager`, `VillageChangeSignalController`, `ShopSlot`, `SaveData`, `VillageCultureVisualController`가 모두 `Assets/Scripts/` 바로 아래에 있음을 확정했다. 실제 경로만 사용한 후속 정적 검증이 PASS했다.

## [RESOLVED] 2026-07-17 — Task 047 데이터 감사에서 폐기한 Windows 경로 와일드카드 재사용

- 증상: 낚시·캠핑·가구의 실제 데이터 사용처를 조사하는 `rg` 명령이 마지막 경로 인수 `PROJECT_PA_*.md`에서 `os error 123`으로 종료됐다.
- 원인: Task 046에서 이미 두 번 실패하고 폐기한 Windows 경로 와일드카드 방식을 다시 사용했다. `rg`의 `-g` 필터나 명시 파일 목록을 사용해야 했지만 같은 금지 패턴을 재도입했다.
- 시도한 것: Task 047 첫 데이터 감사 1회. 같은 원인의 누적 실패 규칙에 따라 수정 명령이나 두 번째 검색을 실행하지 않았다.
- 실패 전 확인된 범위: `ItemCategory`는 Raw/Processed/Utility/Luxury/Tool이고, Resources에는 Fish·GrilledFish·Furniture 등 실제 Item 에셋이 있다. 캠핑 전용 Item 존재 여부와 세 항목의 사용처 추적은 완료하지 못했다.
- 영향: 코드·씬·프리팹·에셋·패키지·저장 스키마 변경 없음. `VILLAGE_TREND.md`는 Task 046 완료본 그대로이며 Task 047은 PARTIAL/IN_PROGRESS 상태로 유지한다.
- 재개 조건: 경로 인수에 와일드카드를 절대 사용하지 않는다. `rg -g 'PROJECT_PA_*.md' <pattern> .` 또는 `Get-ChildItem`이 반환한 명시 경로를 사용하고, Assets/Docs/AI_WORKFLOW 검색도 경로와 glob 필터를 분리한다.
- 해결: 재개 시 검색 경로를 `Assets`, `Docs`, `AI_WORKFLOW`와 명시 루트 문서로 고정하고 glob은 `-g` 필터에만 사용했다. Fish/생선구이/목제 가구의 실제 Item·Recipe 참조와 캠핑 전용 Resources 부재를 확인했으며 Task 047 정적 검증이 PASS했다.

## [OPEN] 2026-07-17 — Task 047 종료 검사의 추적 매트릭스 경로 오인

- 증상: 종료 검사에서 존재하지 않는 `AI_WORKFLOW/03_TASKS/TRACEABILITY_MATRIX.md`를 지정해 1회 중단됐고, 이어 실제 파일명을 찾기 위한 `rg --files AI_WORKFLOW | rg "...$"`가 Windows 역슬래시 경로와 맞지 않아 결과 없이 1회 종료됐다.
- 원인: 현재 저장소의 추적 매트릭스 실제 파일명을 먼저 고정하지 않고 추정 경로와 경로 구분자 의존 정규식을 사용했다.
- 시도한 것: 추정한 명시 경로 검사 1회, 슬래시 방향을 고려하지 않은 파일명 검색 1회. 동일한 경로 가정 계열 실패가 2회 발생했으므로 세 번째 검사는 실행하지 않는다.
- 영향: 코드·씬·프리팹·에셋·패키지·저장 스키마 변경 없음. Task 047 설계 문서와 완료 추적 내용은 보존되며, 이번 추가 종료 검사만 완료하지 못했다.
- 재개 조건: `Get-ChildItem -LiteralPath 'AI_WORKFLOW/03_TASKS' -File`처럼 정규식 없이 파일 목록을 한 번 확인한 뒤, 확인된 명시 경로만 사용해 변경 로그 순서·매트릭스 집계·추적 마커 검사를 새로 실행한다.

## [RESOLVED] 2026-07-17 — Task 052 설계 문서 메타데이터의 Markdown 줄바꿈 공백 검출

- 증상: `DESIGN_EVENTS.md` 초안의 문서 표식 10개와 소스 계약 8개를 대조하는 정적 검사 마지막 단계에서 3~4행을 trailing whitespace로 판정해 중단됐다.
- 원인: 갱신일과 대상 작업 줄 끝에 Markdown 강제 줄바꿈용 공백 두 칸을 넣었고, 검사식 `Select-String -Pattern '[ \t]+$'`이 이를 의도와 무관하게 잡았다.
- 시도한 것: 신규 설계 문서 작성 뒤 정적 검사 1회. 동일 문서/검사로 재시도하지 않았다.
- 영향: 기능·데이터 불일치는 발견되지 않았고 코드·씬·프리팹·에셋·패키지·저장 스키마는 변경하지 않았다. Task 052는 완료 전환하지 않고 ACTIVE 상태로 유지한다.
- 재개 조건: 문서 3~4행의 강제 줄바꿈 공백을 일반 줄바꿈으로 바꾼 뒤, 실패한 전체 명령을 그대로 반복하지 말고 `git diff --check`와 독립 표식/소스 대조를 각각 한 번 실행한다.
- 해결: 문서 3~4행 끝의 공백 두 칸만 제거했다. 전체 `git diff --check`가 종료 코드 0으로 통과했고, 별도 검사에서 문서 계약 10개·소스/데이터 계약 10개·Task 041 판매 로그 표식 5개·이벤트 런타임 클래스 부재·문서 trailing whitespace 0건을 확인했다.

## [RESOLVED] 2026-07-17 — Task 054 v11 런타임 부재 검사의 지역 변수명 오탐

- 증상: `SAVE_SCHEMA.md`의 v11 판매 통계 설계 표식과 실제 v10 소스를 대조하는 정적 검사에서 “v11 런타임 미구현” 항목만 실패했다.
- 실제 출력: `PA_GatheringShopGateValidator.cs`와 `PA_MiningShopLoopValidator.cs`의 지역 변수 `recentSales`가 검색됐다. `SaveData.cs` 신규 필드나 `CurrentSaveVersion = 11` 구현을 찾은 것이 아니다.
- 원인: 검사식이 필드 선언 형식을 제한하지 않고 `recentSales` 단어를 전체 Runtime/Editor C#에서 검색해 기존 지역 변수까지 신규 저장 필드로 오인했다.
- 시도한 것: 설계 초안 작성 뒤 정적 검사 1회. 이어 중단 상태 동기화용 다중 파일 패치는 `CHANGELOG_AI.md` 제목 문맥 불일치로 적용 전에 검증 실패했으며 재시도하지 않았다.
- 영향: `SAVE_SCHEMA.md`의 v10→v11 설계 초안과 `ACTIVE_TASK.md`의 진행 상태만 보존됐다. C#·씬·프리팹·에셋·패키지·실제 저장 버전은 변경되지 않았고 Task 054는 완료 전환하지 않는다.
- 재개 조건: 넓은 `recentSales` 검색을 폐기하고 `SaveData.cs`의 정확한 `public List<...>` 선언과 `SaveManager.cs` 버전 상수만 별도로 검사한다. 실제 문서 제목을 먼저 읽은 뒤 중단/완료 추적 문서를 작은 패치로 동기화한다.
- 해결: 넓은 단어 검색을 재사용하지 않았다. `SaveData.cs`의 제안 필드/DTO 정확 선언 부재 8건, `SaveManager.cs`의 v10 상수·v9→v10 마이그레이션 유지와 v11 상수/마이그레이션 부재, `SalesLogManager` private 소유 상태를 독립 검사해 소스 계약 17/17 PASS했다. 문서 계약 12/12, whitespace 0, 전체 `git diff --check`도 PASS했다.

## [RESOLVED] 2026-07-17 — Task 023 ShopPriceUI 다중 패치의 색상 상수 문맥 매칭 실패

- 증상: `ShopPriceUI`에 가격 파생 희귀/일반 구분 줄을 추가하는 다중 파일 패치가 `C_ReactionBad` 상수 문맥을 찾지 못해 두 번 중단됐다.
- 실제 소스: 감사 직후 `rg -n -C 3` 출력에서 해당 상수와 `ItemNameTxt`·`RefreshUI` 위치가 존재함을 확인했다. C# 구현이나 Unity 컴파일 실패가 아니라 패치 적용 전 문맥 검증 실패다.
- 원인 추정: 넓은 첫 패치와 축소한 두 번째 패치가 동일 색상 상수 행을 고정 문맥으로 사용했고, 파일의 공백/줄끝 표현과 패치 입력이 일치하지 않았다.
- 시도한 것: 다중 파일 패치 1회, 필드·상수만 남긴 축소 패치 1회. 둘 다 같은 `C_ReactionBad` 문맥에서 실패했다.
- 중단 및 정리: 같은 원인 2회 실패 규칙에 따라 세 번째 구현 패치를 실행하지 않았다. 중간에 단독 적용된 미사용 필드 1개는 즉시 제거해 `ShopPriceUI.cs`를 작업 전 상태로 복원했다. 씬·프리팹·아이템 에셋·구매 수학·저장은 변경하지 않았다.
- 재개 조건: 색상 상수 행을 선택자로 재사용하지 않는다. `ItemNameTxt` 생성 블록과 `RefreshUI`의 회수 버튼 주석처럼 이미 직접 확인한 좁은 문맥을 각각 독립 패치하고, 가격 경계 상수는 클래스 상태 필드 인접 위치에 별도로 삽입한다.
- 해결: 실패한 `C_ReactionBad` 선택자를 재사용하지 않았다. 상태 필드 인접부, `ItemNameTxt` 생성 블록, `RefreshUI` 회수 버튼 앞을 독립 패치해 구현했고 Runtime/Editor 컴파일, D3D11 FinalPresentation 일반/희귀 분기와 2장 캡처, FinalDemoRoute 30G 회귀가 모두 PASS했다.

## [RESOLVED] 2026-07-17 — Task 093 금지 파일 무변경 검사의 기존 dirty diff 오인

- 증상: 명명 트렌드 소스·데이터 계약 17개가 PASS한 뒤, `git diff`가 비어 있어야 한다는 마지막 금지 파일 검사가 실패했다.
- 실제 상태: `SalesLogManager.cs`, `ShopSlot.cs`, `PurchaseEvaluator.cs`, `EconomyService.cs`는 diff가 없었다. `SaveData.cs`와 `SaveManager.cs`에는 Task 093 시작 전부터 유지하던 기존 추가 변경 62행이 남아 있어 기준 브랜치 대비 diff가 비어 있지 않았다.
- 원인: dirty worktree에서 “이번 태스크가 수정하지 않음”과 “기준 브랜치 대비 변경 없음”을 같은 조건으로 검사했다. Task 093의 모든 `apply_patch` 대상은 사전 보고한 신호 컨트롤러·검증기·추적 문서뿐이었고 저장 파일을 패치하지 않았다.
- 시도한 것: 기준 브랜치 대비 빈 diff 검사 1회. 기존 변경을 지우거나 같은 검사를 반복하지 않았다.
- 영향: Task 093 런타임/Editor 빌드는 오류 0이고 명명 트렌드 계약 17개는 PASS했다. 저장 스키마·소유자에는 Task 093 변경이 없다.
- 해결: 금지 파일의 기존 dirty 상태를 보존하고, 이번 패치 대상 명시 목록과 Task 093/명명 트렌드 표식의 허용 파일 한정 검사로 경계를 대조한다. 향후 dirty worktree 무변경 검사는 작업 전 해시/내용 스냅샷을 먼저 확보한다.

## [RESOLVED] 2026-07-17 — Task 093 종료 공백 검사의 동결 개발일지 기존 줄바꿈 오인

- 증상: 매트릭스 93행/상태 집계와 필수 인계 문서 10개 표식이 PASS한 뒤, 전체 Task 파일 trailing-whitespace 검사가 `Docs/07_개발일지.md`의 11개 기존 행 때문에 중단됐다.
- 실제 상태: 검출 행은 3~5, 644, 1495~1496, 1501, 1544~1545, 1919, 2035행의 기존 Markdown 강제 줄바꿈 공백이다. 이번에 추가한 Task 093 섹션 4506~4519행에는 trailing whitespace가 없다.
- 원인: 동결 개발일지 전체의 오래된 서식을 이번 패치 범위와 구분하지 않고 검사했다. Task 052에서 이미 확인한 Markdown 줄바꿈 공백 유형과 같다.
- 시도한 것: 전체 Task 파일 공백 검사 1회. 기존 동결 행을 정리하거나 같은 전체 검사를 반복하지 않았다.
- 영향: C# 빌드, 명명 트렌드 계약, 매트릭스 집계에는 영향이 없다. 기존 줄바꿈 서식과 사용자 변경을 보존한다.
- 해결: Task 093 신규 섹션만 행 범위로 검사하고, 추적된 변경 공백은 `git diff --check`로 별도 확인한다. 향후 동결 문서 검사는 작업 전 존재하던 공백을 기준선으로 제외한다.

## [OPEN] 2026-07-17 — Task 095 종료 후 다음 작업 감사의 게임 루프 문서 경로 오인

- 증상: Task 095 코드·빌드·정적 계약·추적 문서 동기화가 모두 끝난 뒤 다음 체크포인트 후보를 찾는 읽기 전용 검색이 존재하지 않는 `AI_WORKFLOW/03_TASKS/PROJECT_PA_GAME_LOOP.md` 때문에 종료 코드 1을 반환했다. 이어 실제 위치를 찾으려 한 `rg --files | rg 'PROJECT_PA_GAME_LOOP\\.md$'`도 Windows 역슬래시 경로와 맞지 않아 결과 없이 종료됐다.
- 원인: `PROJECT_PA_GAME_LOOP.md`의 실제 위치를 먼저 명시 파일 목록으로 확정하지 않았고, 저장소 파일 목록에 슬래시 방향 의존 정규식을 다시 사용했다. Task 047에서 이미 기록된 경로 정규식 유형을 재도입했다.
- 시도한 것: 추정 명시 경로를 포함한 검색 1회, 슬래시 방향 의존 파일명 정규식 1회. 같은 경로 가정 계열 2회 실패이므로 세 번째 검색은 실행하지 않는다.
- 영향: 두 명령 모두 읽기 전용이며 Task 095 구현·Runtime/Editor 빌드 오류 0·정적 계약 PASS·집계 95건에는 영향이 없다. 코드·씬·프리팹·에셋·패키지·저장은 변경하지 않았다.
- 재개 조건: 다음 세션에서 `Get-ChildItem -LiteralPath 'Docs/Codex' -File` 또는 이미 확인된 문서 인덱스의 명시 경로만 사용한다. `PROJECT_PA_GAME_LOOP.md` 추정 경로와 슬래시 의존 `rg --files` 정규식은 재사용하지 않는다.
- 재발(다음 goal continuation): 필수 문서와 `Docs/Codex` 실제 목록은 안전하게 확인했지만, 추가 로드맵 감사에서 이전 요약에 있던 `AI_WORKFLOW/07_FULL_GAME_ROADMAP/PROJECT_PA_CURRENT_MILESTONE.md`를 디렉터리 목록 확인 없이 다시 사용해 명령이 중단됐다. 같은 경로 가정 계열의 연속 두 번째 goal turn이다. 코드·에셋 변경은 없으며 새 작업은 시작하지 않았다.
- 강화된 재개 조건: 다음 goal turn의 첫 추가 감사는 `Get-ChildItem -LiteralPath 'AI_WORKFLOW/07_FULL_GAME_ROADMAP' -File` 단일 명령만 수행한다. 그 출력에 실제로 존재하는 파일만 후속 명시 경로로 읽고, 대화 요약에 적힌 파일명은 증거로 사용하지 않는다.
- 재발(세 번째 연속 goal turn): 로드맵 디렉터리 실제 목록 확인과 필수 문서 읽기는 성공해 이전 중단 조건은 해소했다. 그러나 Task 096 후보의 저장 감사를 하면서 `LocalJsonSaveRepository.cs`를 `Assets/Scripts` 바로 아래라고 다시 추정했고, 해당 `Get-Content`만 파일 없음 오류를 냈다. 명령 앞부분의 `SaveManager` 읽기·diff는 유효하며 코드 수정은 시작하지 않았다.
- 현재 판정: 외부 상태나 사용자 입력이 필요한 impasse가 아니라 반복된 조사 절차 오류다. 전체 goal은 blocked로 전환하지 않는다. 이번 turn은 프로젝트 실패 규칙에 따라 중단한다.
- 다음 재개 조건: 저장 구현 파일은 이름을 추정하지 말고 `Get-ChildItem -LiteralPath 'Assets' -Recurse -File -Filter '*SaveRepository*.cs'`의 실제 출력만 읽는다. 그 전에는 Task 096을 등록하지 않는다.

## [OPEN] 2026-07-17 — Task 096 읽기 전용 HasSave 검사에서 메서드 이름 부분 문자열 오탐

- 증상: Task 096 Runtime/Editor 빌드 오류 0, 타이틀/새 게임/이어하기/실패 복귀/F5·F9 보존 등 정적 계약 15개가 모두 PASS한 뒤, `HasSaveAsync` 메서드 본문에 금지 상태 변경 호출이 없는지 확인하는 마지막 검사가 `SaveAsync`를 발견했다고 실패했다.
- 실제 상태: 검사가 잘라낸 본문에는 `_repository.ExistsAsync(SaveKey)`와 `Task.FromResult(false)`만 있다. 검출된 문자열은 메서드 선언 이름 `HasSaveAsync` 안의 `SaveAsync` 부분이며 `_repository.SaveAsync(...)` 호출이 아니다.
- 원인: 호출 구문을 검사하지 않고 `Contains('SaveAsync')`로 넓게 비교해 공개 읽기 API의 이름을 저장 실행으로 오인했다.
- 시도한 것: 신규 코드 컴파일 1회 PASS, 상태 전이/격리 정적 검사 1회. 실패 선택자는 재사용하거나 즉시 수정 재시도하지 않았다.
- 영향: Task 096 코드와 ACTIVE 등록은 보존한다. 저장 스키마·SaveKey·자동 로드·삭제·씬·프리팹에는 변경이 없으며 문서 완료 전환은 하지 않았다.
- 재개 조건: 전체 `SaveAsync` 단어 검사를 폐기한다. 잘라낸 본문에서 `._repository.SaveAsync(` 또는 `_repository.SaveAsync(`, `_repository.LoadAsync(`, `File.`, `Delete(` 같은 정확 호출 토큰만 독립적으로 한 번 검사한다. 이후 Runtime/Editor 빌드 결과를 재사용하고 종료 문서를 갱신한다.
- 해결: 넓은 단어 검사를 재사용하지 않고 `HasSaveAsync` 본문 범위에서 `_repository.SaveAsync(`, `_repository.LoadAsync(`, `File.`, `Delete(`, 마이그레이션·강제 복원 정확 호출 토큰만 독립 검사했다. 결과 `HAS_SAVE_BODY=PASS`, `FORBIDDEN_CALLS=NONE`이며 Task 096 추적 문서를 갱신했다.

## [RESOLVED] 2026-07-17 — Task 097 등록 전 추적 디렉터리명 오인

- 증상: Pause 메뉴 후보 감사 중 존재하지 않는 `AI_WORKFLOW/03_TASK_SYSTEM/ACTIVE_TASK.md`와 `TASK_QUEUE.md`를 읽으려 해 해당 두 `Get-Content`만 실패했다.
- 원인: 실제 저장소 디렉터리명 `03_TASKS`를 확인하지 않고 일반화한 `03_TASK_SYSTEM`을 추정했다.
- 시도한 것: 추정 경로 조회 1회. 같은 경로는 재시도하지 않았다.
- 영향: 읽기 전용 조회였고 코드·씬·프리팹·에셋·패키지·저장은 변경하지 않았다. 같은 명령의 검증 규칙·코드 감사 출력은 유효하다.
- 해결: `Get-ChildItem -LiteralPath 'AI_WORKFLOW' -Directory`와 파일명 필터로 실제 `03_TASKS/ACTIVE_TASK.md`, `03_TASKS/TASK_QUEUE.md`를 확정했다. Task097부터는 이 명시 경로만 사용한다.

## [RESOLVED] 2026-07-17 — Task 099 후보 감사의 SettingsUI 하위 디렉터리 오인

- 증상: 제품 입구의 조작/종료 후보를 읽는 감사에서 존재하지 않는 `Assets/Scripts/UI/SettingsUI.cs` 한 건이 파일 없음으로 실패했다.
- 원인: 실제 파일 목록을 먼저 확인하지 않고 다른 UI 컨트롤러와 같은 `UI` 하위 폴더에 있을 것으로 추정했다.
- 시도한 것: 추정 명시 경로 조회 1회. 같은 경로를 재시도하지 않았다.
- 영향: 같은 명령의 `PlayerInputHandler`, 타이틀 단계, Quit 사용처 감사는 유효하다. Task099 등록·코드·씬·프리팹·에셋·패키지·저장 변경은 시작하지 않았다. 완료된 Task098 구현·빌드·계약·문서에는 영향이 없다.
- 해결: `Get-ChildItem -LiteralPath 'Assets/Scripts' -Recurse -File -Filter 'SettingsUI.cs'`로 실제 경로 `Assets/Scripts/SettingsUI.cs`를 확정했다. 다음 진행은 이 경로만 사용하며, 프로젝트 실패 규칙에 따라 이번 turn에서는 후속 구현을 시작하지 않는다.

## [RESOLVED] 2026-07-17 — Task 099 빌드 명령 감사의 Windows 와일드카드 전달 오류

- 증상: 실제 `.csproj` 두 개와 과거 빌드 명령은 정상 출력됐지만, 같은 읽기 명령의 마지막 `rg ... PROJECT_PA_*.md` 인수가 Windows에서 유효한 경로로 해석되지 않아 전체 종료 코드가 1이 됐다.
- 원인: PowerShell이 아닌 `rg` 경로 인수에 Windows 파일명 와일드카드를 직접 전달했다.
- 시도한 것: 해당 읽기 전용 검색 1회. 같은 와일드카드 명령은 반복하지 않았다.
- 영향: 코드·씬·프리팹·에셋·패키지·저장은 변경하지 않았다. `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`와 표준 순차 빌드 명령 `dotnet build <project> --no-restore`는 출력으로 확인됐다.
- 해결: 확인된 두 명시 프로젝트 경로만 순차 빌드하며, 루트 Markdown 검색은 이번 Task099 검증에 불필요하므로 제거한다.

## [RESOLVED] 2026-07-17 — Task 099 첫 정적 계약 스크립트의 PowerShell 자동 변수 충돌

- 증상: Runtime/Editor 빌드 오류 0 뒤 첫 정적 계약 스크립트가 6개 중 5개만 집계하고 종료 코드 1을 반환했다. 입력 계약들은 평가되지 않았고 `controls_case`도 실패로 표시됐다.
- 실제 상태: 코드 컴파일은 통과했다. 실패 로그는 `$input`이 PowerShell의 자동 열거 변수와 충돌해 문자열 `.Contains`를 호출할 수 없었고, 서브셸 안의 `git` 종료 코드 식에도 잘못된 괄호가 있었음을 보여준다.
- 원인: 이미 PowerShell인 실행 셸 안에 다시 `powershell -Command -`를 중첩하고 예약 자동 변수명 `$input`을 사용했다. 한글 리터럴도 파이프 인코딩 영향을 받았다.
- 시도한 것: 첫 정적 계약 스크립트 1회. 같은 스크립트는 반복하지 않는다.
- 영향: 읽기 전용 검사만 실패했으며 코드·씬·프리팹·에셋·패키지·저장은 추가 변경되지 않았다.
- 해결: 중첩 셸을 제거하고 `$inputText`를 사용하며, 한글 리터럴 대신 ASCII 상태 식별자·키 토큰을 검사하고 `git diff --quiet` 종료 코드를 별도 문장으로 수집한다.

## [RESOLVED] 2026-07-17 — Task 099 입력 권위 보존 검사의 dirty 기준선 오인

- 증상: 수정된 정적 계약 검사에서 단계 순서·실제 키 매핑·읽기 전용 안내·기존 타이틀/이어하기/도착 흐름 11개는 모두 PASS했지만 `input_authority_unchanged` 1개가 FAIL해 전체 종료 코드가 1이 됐다.
- 실제 상태: `PlayerInputHandler.cs`의 Git diff는 기존 야외 배치 이동 M/회수 X 이벤트와 키 처리뿐이다. 이 바인딩들은 Task099 후보 감사 시점부터 이미 존재했고 이번 작업은 `PlayableDayScenarioController.cs`만 기능 파일로 수정했다.
- 원인: dirty worktree에서 “이번 작업이 수정하지 않음”을 확인하면서 작업 시작 스냅샷이 아니라 `HEAD` 대비 깨끗함을 요구했다.
- 시도한 것: 수정된 계약 검사 1회. `git diff --quiet` 기준은 재사용하지 않는다.
- 영향: Runtime/Editor 오류 0과 11개 기능 계약에는 영향이 없다. 기존 M/X 입력 변경은 사용자/선행 작업 소유로 보존한다.
- 해결: Task099 정적 결과는 실제 키 소스와 안내를 직접 비교한 기능 계약 11/11로 판정한다. 입력 권위 보존은 작업 전 보고 파일 목록과 이번 `apply_patch` 대상 기록으로 확인하며, dirty 파일의 `HEAD` 비교를 작업 소유권 증거로 사용하지 않는다.

## [RESOLVED] 2026-07-18 — Task 103 diff 미리보기의 조기 파이프 종료

- 증상: 제작 계약 15/15 PASS 뒤 `git diff --check`와 긴 diff 미리보기를 한 명령에 묶고 `Select-Object -First`로 출력을 자르자 전체 셸 종료 코드가 1이 됐다.
- 실제 상태: 앞선 `git diff --check`는 공백 오류를 출력하지 않았고, 실패는 미리보기 소비자가 먼저 종료되며 `git diff` 파이프가 끊긴 뒤 발생했다. Runtime/Editor 빌드와 기능 계약에는 영향이 없다.
- 시도한 것: 결합 명령 1회. 같은 조기 종료 파이프를 반복하지 않았다.
- 해결: diff 본문 미리보기를 검증 종료 코드에서 분리하고 `git diff --check`를 독립 실행했다. 최종 결과 `DIFF_CHECK_EXIT=0`, 제작 계약 16/16 PASS다.

## [RESOLVED] 2026-07-18 — Task 104 후보 인벤토리 프리팹 감사의 PowerShell Include 범위 오류

- 증상: 메인 씬의 InventoryUI/HotbarUI 문자열 수와 관련 프리팹을 한 번에 감사하던 명령이 10초 제한을 넘겨 종료 코드 124로 중단됐다.
- 실제 상태: 시간 제한 전 메인 씬에 `InventoryUI` 1, `HotbarUI` 1, `ItemTooltip` 1, `InventoryPanel` 1이 있고, 참조 후보 `Assets/Prefabs/UI_Slot.prefab`이 존재한다는 읽기 결과는 확보됐다. 코드·씬·프리팹 수정은 시작하지 않았고 Task 104도 등록하지 않았다.
- 원인: `Get-ChildItem -LiteralPath 'Assets' -Recurse -File -Include ...`의 필터가 예상대로 프리팹 이름에만 제한되지 않아 Assets 전체 파일을 후속 `Select-String` 루프에 전달했다.
- 시도한 것: 잘못된 광역 열거 1회. 같은 명령은 재사용하지 않는다.
- 영향: 읽기 전용 감사만 중단됐다. 완료된 Task 103 구현·빌드·계약·문서와 현재 dirty worktree에는 영향이 없다.
- 재개 조건: `Assets/Prefabs/UI_Slot.prefab`과 필요 시 `rg --files Assets/Prefabs | rg 'UI_Slot|Inventory|Hotbar|Tooltip'`의 실제 출력만 명시 경로로 읽는다. 메인 씬의 이미 확인된 컴포넌트 수를 다시 광역 검색하지 않는다.
- 해결: 광역 `Get-ChildItem -Recurse -Include`를 재사용하지 않고 명시 경로 `Assets/Prefabs/UI_Slot.prefab`과 `rg --files Assets/Prefabs` 결과만 읽었다. prefab의 `InventorySlotUI`, Icon, CountText 직렬화 참조가 유효하고 메인 씬에 InventoryUI/HotbarUI/ItemTooltip 각 1개가 존재함을 확인했다. 최신 사용자 지시에 따라 인벤토리 후보는 등록하지 않고 Task 104를 Tripo/B11 단일 작업으로 전환했다.

## [RESOLVED] 2026-07-27 — Task 112 문서 동기화 검사의 `Docs/07_개발일지.md` Git 변경 감지 오탐

- 증상: Task 112 Runtime/Editor 오류 0, 기능 계약 40/40, 코드 파일 대상 `git diff --check` PASS 뒤 필수 문서 동기화 교차 검사에서 첫 selector가 29/32를 반환했다. 실제 Task 112 표식은 모든 대상 문서에서 확인됐으나 경로별 변경 감지 식이 실패했다. selector를 명시 경로 배열로 한 번 바로잡은 재검사는 44/45였고 `Docs/07_개발일지.md`의 Task 112 내용 표식은 PASS하면서 Git 변경 목록 포함 여부만 FAIL했다.
- 실제 상태: `Docs/07_개발일지.md`에는 Task 112 구현·검증·미확인 경계가 기록되어 있다. 두 실패 모두 코드·씬·에셋을 변경하지 않은 읽기 전용 문서 추적 검사이며 Runtime/Editor 빌드와 40개 기능 계약에는 영향이 없다.
- 원인: dirty worktree의 파일 소유권/추적 상태를 `git diff --name-only` 결과만으로 판정했다. 기존 파일이 untracked·assume-unchanged·다른 추적 상태일 가능성을 분리하지 않아 물리적 내용 검사와 Git 변경 감지를 잘못 결합했다.
- 시도한 것: 전체 문서 변경 감지 selector 1회, 명시 경로 배열로 수정한 selector 1회. 같은 Git 변경 목록 원인의 두 번째 실패이므로 세 번째 재검사는 실행하지 않는다.
- 영향: Task 112는 `PARTIAL`을 유지한다. 필수 문서 내용은 갱신됐지만 문서 동기화 자동 검증은 44/45이며, 전체 worktree `git diff --check`와 Unity 실플레이는 이 중단 뒤 추가 실행하지 않았다.
- 재개 조건: `Docs/07_개발일지.md`의 물리적 Task 112 표식을 이미 확보한 증거로 사용하고, Git 추적 확인이 꼭 필요하면 사용자 소유 dirty 기준선을 보존한 채 `git status --short --untracked-files=all -- 'Docs/07_개발일지.md'`와 `git ls-files -v -- 'Docs/07_개발일지.md'`를 각각 읽는다. `git diff --name-only` 단일 결과를 “이번 작업에서 수정되지 않음”의 증거로 재사용하지 않는다.
- 해결: 다음 goal continuation에서 지정된 두 명령을 각각 실행했다. `git status --short --untracked-files=all -- 'Docs/07_개발일지.md'`는 추적 파일의 수정 상태를, `git ls-files -v -- 'Docs/07_개발일지.md'`는 정상 추적 표식 `H`를 반환했다. 기존 selector는 Git이 한글 경로를 octal-quoted 출력한 값을 리터럴 경로와 직접 비교해 발생한 false negative였다. 실제 Task 112 내용과 변경 상태가 모두 확인됐으므로 44/45 검사는 회복 완료로 닫는다.

## [RESOLVED] 2026-07-27 — Task 114 Runtime/Editor 병렬 빌드의 공유 출력 파일 잠금 충돌

- 증상: Day 15–30 캠페인과 Day 30 완주점 구현 뒤 `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`를 동시에 빌드하자 `Temp/obj/Assembly-CSharp/Assembly-CSharp.dll` 쓰기에서 `CS2012`가 발생했다.
- 원인: Editor 프로젝트가 Runtime 프로젝트를 참조하는데 두 `dotnet build`를 병렬 실행해 같은 Runtime 출력 파일을 동시에 열었다. 코드 진단 오류가 아니라 검증 명령의 동시성 오류다.
- 시도한 것: 병렬 빌드 1회. 동일 병렬 명령은 재사용하지 않는다.
- 영향: 소스·씬·프리팹·에셋·패키지·저장에는 추가 변경이 없고 Unity Editor도 실행하지 않았다. 컴파일 성공 여부는 아직 확정하지 않는다.
- 재개 조건: 확인된 명시 프로젝트를 `Assembly-CSharp.csproj` → `Assembly-CSharp-Editor.csproj` 순서로 한 번만 빌드한다. 같은 파일 잠금이 다시 발생하면 세 번째 빌드는 시도하지 않고 Task 114를 중단한다.
- 해결: 병렬 명령을 재사용하지 않고 Runtime을 먼저 빌드한 뒤 Editor를 빌드했다. 두 프로젝트 모두 경고 0, 오류 0으로 통과했으며 파일 잠금은 재발하지 않았다.

## [RESOLVED] 2026-07-27 — Task 114 첫 정적 계약 스크립트의 PowerShell 콜론 보간 오류

- 증상: Runtime/Editor 빌드 통과 뒤 Day 15–30 계약을 집계하려던 스크립트가 `"case $day:"`를 파싱하는 단계에서 `InvalidVariableReferenceWithDrive`로 중단됐다.
- 원인: 큰따옴표 문자열 안에서 변수 바로 뒤의 콜론을 변수 범위와 분리하지 않았다.
- 시도한 것: 첫 정적 계약 스크립트 1회. 검사 본문은 실행되기 전이어서 PASS/FAIL 집계가 생성되지 않았다.
- 영향: 읽기 전용 검사만 중단됐으며 기능 코드·씬·프리팹·에셋·패키지·저장에는 추가 변경이 없다. 직전 Runtime/Editor 빌드 오류 0 결과에도 영향이 없다.
- 재개 조건: 실패한 보간식을 재사용하지 않고 `('case {0}:' -f $day)` 형식 문자열로 한 번만 검사한다. 같은 파서 오류가 반복되면 세 번째 정적 검사를 시도하지 않는다.
- 해결: 형식 문자열을 사용한 두 번째 검사에서 Day 7 경계, Day 15–30 계획·체크리스트, Day 30 정산, Day 31 계속, 기존 시스템 재사용 계약이 56/56 PASS했고 대상 파일 `git diff --check`도 통과했다.

## [RESOLVED] 2026-07-27 — Task 123 정적 계약 검사의 호출 수 기대값 오류

- 증상: Day 77~90 B06 주방 가치사슬 구현과 Runtime/Editor 순차 빌드 오류 0 뒤 실행한 정적 계약 검사가 `objective-hook`에서 종료 코드 1로 중단됐다.
- 실제 상태: `TryResolveTierTwoKitchenMilestone(day, ...)` 호출부는 상단 목표와 운영 체크리스트의 정확히 두 곳이다. 검사식은 메서드 정의까지 같은 정규식으로 셀 수 있다고 잘못 가정해 3개를 기대했다.
- 원인: 호출식 전용 패턴 `TryResolveTierTwoKitchenMilestone\(day`와 메서드 정의 `TryResolveTierTwoKitchenMilestone(int day`의 문법 차이를 기대값에 반영하지 않았다.
- 시도한 것: 잘못된 정적 계약 검사 1회. 같은 `-eq 3` 기대값은 재사용하지 않는다.
- 영향: 두 C# 프로젝트는 오류 0으로 빌드됐고 코드 diff 공백 검사도 통과했다. 나머지 리소스·14일 분기 계약 집계는 첫 실패에서 중단됐으므로 Task 123은 `PARTIAL`이다. Unity는 실행하지 않았다.
- 재개 조건: 다음 단일 검증 작업에서 호출부 기대값을 정확히 2로 두고 한 번만 실행한다. Day 77~90 계획 14개, 런타임 case 14개, B06/세 Kitchen 레시피/출력 Item, 기존 Day 1~76·Tier·저장 경계를 함께 확인한다.
- 해결: Task 124에서 호출부 기대값을 정확히 2로 둔 독립 계약이 PASS했다. 목표와 체크리스트 두 연결, 단일 메서드 정의, Day 77~90 계획/case는 모두 확인됐다.

## [RESOLVED] 2026-07-27 — Task 124 B06 Tier·출력 Item 이름 정적 표현 계약 불일치

- 증상: Task 123 정적 계약 복구 검사에서 호출부 2개를 포함한 44개 계약은 PASS했으나 `b06-placement-minimum-tier-two`, 구운 감자와 생선구이의 `output-name` 2개가 FAIL해 전체 결과가 44/47이었다.
- 확인된 상태: Day 77~90 계획 14개와 case 14개, 두 UI 호출부, 단일 판정 정의, Day 76 경계, B06 프리팹 `workbenchType: 2`, 세 레시피의 `requiredWorkbench: 2`, 세 출력 Item의 Processed category, 요구 리소스 12개 존재는 PASS했다.
- 원인: 현재 결과만으로 실제 데이터 결함인지 검사 정규식이 현재 C#/YAML 표현과 맞지 않는지 확정할 수 없다.
- 시도한 것: 기록된 정적 계약 복구 검사 1회. 실패 뒤 같은 작업에서 소스 재조회나 패턴 수정 재시도는 하지 않았다.
- 영향: Task 123 구현 코드는 변경하지 않았고 Unity도 실행하지 않았다. 주방 캠페인의 핵심 구조는 정적으로 확인됐지만 B06 해금 표현과 두 판매 Item 이름 권위가 미확정이므로 Task 124는 `PARTIAL`이다.
- 재개 조건: 다음 단일 작업은 검증기를 먼저 재실행하지 말고 `ShopCustomizationController.ResolveMinimumTier`의 B06 실제 행과 두 Item asset의 실제 `itemName` 직렬화 행을 명시 경로에서 한 번 읽는다. 데이터가 올바르면 검사 계약만 정정하고, 데이터가 틀리면 별도 수정 범위를 먼저 보고한다.
- 해결: Task 125의 명시 경로 감사에서 B06은 `case "Blueprint_B06_KitchenStation": return 2;`로 정확히 Tier 2를 반환했다. 두 Item은 Unity YAML이 한글을 Unicode escape로 저장해 각각 `"\uAD6C\uC6B4 \uAC10\uC790"`→`구운 감자`, `"\uC0DD\uC120\uAD6C\uC774"`→`생선구이`였다. 실제 데이터는 정상이고 `=> 2`/평문 한글만 허용한 검사 표현이 원인이므로 44개 자동 계약+3개 직접 권위 행으로 47/47을 확정한다.

## [RESOLVED] 2026-07-27 — Task 126 필수 문서 일괄 패치의 TODO 문맥 불일치

- 증상: 코드 구현·순차 빌드 오류 0·정적 계약 24/24 뒤 12개 필수 문서를 한 번에 갱신한 첫 `apply_patch`가 `PROJECT_PA_TODO.md`의 예상 Task 125 두 줄을 찾지 못해 적용 전 전체 중단됐다.
- 원인: 실제 TODO의 마지막 항목은 “Day 91 이후 장기 플레이 연결”이었으나 패치 문맥은 “안전 Unity 경로에서 Task 123 실플레이 확인”을 기대했다.
- 시도한 것: 문서 일괄 패치 1회. 실패한 문맥은 재사용하지 않았다.
- 영향: 실패한 패치는 원자적으로 적용 전 중단되어 코드·씬·프리팹·에셋·저장·패키지·ProjectSettings 상태를 바꾸지 않았다. 이미 완료된 Runtime/Editor 빌드와 24/24 계약에도 영향이 없다.
- 해결: 실제 Task 125 문맥을 명시 경로에서 읽고, 문서군을 좁은 대상별 패치로 나눠 Task 126 기록을 동기화했다. 최종 JSON·문서 표식·공백 검사를 별도로 수행한다.

## [RESOLVED] 2026-08-04 — Task 127 검증 대상 프로젝트·ACTIVE_TASK 경로 오인

- 증상: B05~B08 전문 주민 전면 interaction 셀 접근 구현 뒤 첫 컴파일 명령이 존재하지 않는 `Project_PA.Runtime.csproj`를 지정해 `MSB1009`로 중단됐다. 중단 기록을 준비하는 읽기 전용 진단 묶음에서도 존재하지 않는 `AI_WORKFLOW/03_TASK_MANAGEMENT/ACTIVE_TASK.md`를 조회해 `ItemNotFoundException`이 출력됐다.
- 실제 상태: 프로젝트의 확인된 C# 프로젝트는 `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj`이며, 작업 문서의 확인된 경로는 `AI_WORKFLOW/03_TASKS/ACTIVE_TASK.md`다. 두 번 모두 경로 오인으로 코드 컴파일 자체는 시작되지 않았다.
- 원인: 이전 Task 126의 명시 경로를 재사용하지 않고 일반화한 파일명을 추측했고, 두 번째 명령에서도 요약에 있던 정확한 task 경로 대신 존재하지 않는 디렉터리명을 사용했다.
- 시도한 것: 잘못된 Runtime 프로젝트 빌드 1회, 잘못된 ACTIVE_TASK 읽기 1회. 같은 “경로 추측” 원인이 두 번 발생했으므로 이번 Task에서 세 번째 컴파일·경로 재시도는 하지 않는다.
- 영향: `Assets/Scripts/ShopCustomizationController.cs`와 `Assets/Scripts/SpecialistNpcController.cs` 구현은 적용됐지만 Runtime/Editor 컴파일과 정적 계약은 미확인이다. Unity Editor는 실행하지 않았고 씬·프리팹·FBX·재질·저장·패키지·ProjectSettings는 변경하지 않았다.
- 재개 조건: 다음 goal continuation은 새 구현을 추가하지 말고 확인된 `Assembly-CSharp.csproj` → `Assembly-CSharp-Editor.csproj`를 순차로 각 1회 빌드한다. 성공한 뒤에만 원점 목적지 호출 0, Workbench 접근점 투영, NavMesh 완전 경로, 셀 예약/해제, 이동·회수 무효화 계약을 검사한다.
- 추가 중단: Task 종료 문서 12개를 한 번에 갱신한 `apply_patch`가 `HANDOFF_FOR_CODEX.md`의 Task 126 예상 문맥 불일치로 원자적으로 적용 전 중단됐다. 코드와 이 BUG_LOG 항목 외 문서에는 Task 127 표식이 적용되지 않았다. 같은 대형 일괄 패치를 재사용하지 말고 다음 continuation에서 실제 Handoff 문맥을 읽은 뒤 작은 문서군으로 나눠 동기화한다. 문서 동기화가 끝난 후에만 위 순차 빌드 재개 조건으로 이동한다.
- 문서 복구 결과: 다음 continuation에서 12개 필수 상태/인계 문서를 작은 단위로 모두 동기화했고 `loop-state.json`도 `Task-127/in_progress`로 갱신했다.
- 새 중단: 문서 표식 검사기가 11개 Markdown 모두에서 평문 `Task 127`을 찾도록 작성되어 `CURRENT_COMPLETION_MATRIX.md`만 누락으로 판정했다. 실제 매트릭스는 기존 표 관례대로 `| 127 | IN_PROGRESS | ...` 행을 정확히 포함하므로 문서 누락이 아니라 검사 표현 오류다. 결과는 10/11에서 중단됐고 Runtime/Editor 빌드는 아직 시작하지 않았다.
- 새 재개 조건: 다음 continuation은 결합 문서 표식 검사를 재실행하지 말고 `CURRENT_COMPLETION_MATRIX.md`의 `| 127 |` 행을 직접 증거로 인정한다. 그 뒤 확인된 `Assembly-CSharp.csproj` → `Assembly-CSharp-Editor.csproj`를 순차 빌드하고, 성공한 경우에만 Task 127 코드 계약을 한 번 수행한다.
- 해결: 2026-08-04 continuation에서 매트릭스의 실제 `| 127 |` 행을 직접 확인하고 결합 문서 검사기를 재사용하지 않았다. 확인된 Runtime→Editor 순차 빌드는 각각 오류 0(기존 CS8785/CS0414만), Task 127 접근·예약 정적 계약은 29/29 PASS했다. Unity는 별도 반복 네이티브 충돌 게이트로 실행하지 않았다.

## [RESOLVED] 2026-08-04 — Task 128 NPC 직렬화 GUID 감사의 PowerShell MatchInfo 변환 오류

- 증상: 실제 관광객 정상 플레이 진입을 감사하면서 `NpcController.cs.meta`와 `NpcScheduleController.cs.meta`의 GUID를 추출해 씬/프리팹 참조를 찾으려던 읽기 전용 명령이 종료 코드 1로 중단됐다. 추출값에 `C:\Users\...meta:2:guid:` 전체 위치 문자열이 섞여 `rg`가 `invalid hexadecimal digit` 정규식 오류를 냈다.
- 원인: `Select-String`이 반환한 `MatchInfo` 객체 자체를 `-split`했고, 실제 본문인 `.Line`을 먼저 선택하지 않았다.
- 시도한 것: 잘못된 GUID 감사 명령 1회. 같은 명령은 이번 연속 작업에서 재사용하지 않는다.
- 영향: 읽기 전용 감사만 중단됐다. 관광객 기능 코드·씬·프리팹·에셋·저장·패키지·ProjectSettings는 변경하지 않았고 Unity도 실행하지 않았다. Task 128 구현과 상태 집계는 시작하지 않았다.
- 재개 조건: 다음 goal continuation에서 각 GUID를 `(Select-String ...).Line -replace '^guid:\s*',''`로 정확히 추출한 뒤 명시 GUID의 `.unity`/`.prefab` 참조만 한 번 조회한다. 그 결과를 기반으로 실제 무일과표 `NpcController` 인스턴스 존재 여부와 도착/구매 진입 단절을 분류한다.
- 해결: 다음 continuation에서 지정한 `.Line -replace` 교정식으로 두 GUID를 정확히 추출했다. 씬/프리팹 직렬화 참조는 0이었고, `PA_SceneAutoBuilder`와 기존 고객 검증기는 런타임 작성 고객 8명 전원이 유효한 일과표를 가진 주민이며 관광객 0명임을 확인했다. 같은 실패 명령을 재사용하지 않았고 이 감사 결과로 Task 128의 실제 정상 플레이 단절을 구현했다.

## [RESOLVED] 2026-08-04 — Task 129 Windows 와일드카드 감사·더티 기준선 범위 검사 오류

- 증상 1: 감사 조건 사용처를 찾는 읽기 전용 `rg` 명령에서 PowerShell이 아닌 `rg` 경로 인자로 `PROJECT_PA_*.md`를 전달해 Windows 오류 123으로 종료 코드 1이 발생했다.
- 해결 1: 실패한 와일드카드 인자를 재사용하지 않고 `rg -g "*.md" -g "*.cs"`와 명시 루트를 사용했다. 실제 정상 플레이 `AddReputation` 호출은 LongPlay 한 곳뿐이며, `AuditService`는 500,000G·평판 5·고용 3명을 요구하고 Tier 4는 수동 승인임을 확인했다.
- 증상 2: Task 129 정적 계약 41개 중 마지막 범위 검사가 더티 워크트리의 기존 `Crop_Corn.prefab`, `SaveData.cs`, `SaveManager.cs`, `Packages/manifest.json`, `packages-lock.json` 변경까지 이번 작업 변경으로 간주해 40/41에서 실패했다.
- 원인 2: 작업 시작부터 133개 변경이 있던 공유 워크트리에서 금지 경로의 `git diff`가 비어 있다고 가정한 검사식 결함이다. 기능 계약이나 컴파일 실패가 아니다.
- 해결 2: 결합 검사를 재실행하지 않고 실제 Task 129 구현 경로인 `LongPlayProgressionController.cs`와 `PlayableDayScenarioController.cs`를 직접 조회했다. `TryManualAdvance`, `ForceSetTier`, 감사 조건 대입은 두 파일 모두 0이며 이번 작업의 모든 `apply_patch` 대상도 선언한 15개 안에 있다. 따라서 40개 자동 계약+1개 직접 범위 권위로 정합화한다.
- 영향: Unity·씬·프리팹·저장 코어·패키지는 실행하거나 수정하지 않았다. Runtime/Editor 순차 빌드는 각각 오류 0이고 기존 CS8785/CS0414 경고만 유지된다.

## [RESOLVED] 2026-08-04 — Task 130 읽기 전용 참조 검색의 존재하지 않는 `Assets/Data` 경로

- 증상: 감사 UI와 Tier 데이터 참조를 함께 찾는 읽기 전용 `rg` 명령에 존재하지 않는 `Assets/Data`를 포함해, 유효한 앞선 검색 결과 뒤 종료 코드 1이 발생했다.
- 원인: 실제 Tier 에셋이 `Assets/Resources/Tiers`에 있는데 후보 경로를 존재 확인 없이 함께 전달했다.
- 해결: 실패한 경로 인자를 재사용하지 않고 이후 조회를 `Assets/Scripts`, `Assets/Editor`, `Assets/Resources`의 명시 존재 경로로 분리했다. Tier 4 수동 승인 데이터와 감사 앱/서비스 권위를 정상 확인했다.
- 영향: 읽기 전용 감사 외 변경은 없었다. 코드 패치 전 실제 파일 내용을 다시 읽었고 Runtime/Editor 빌드와 48개 정적 계약은 모두 통과했다.

## [RESOLVED] 2026-08-04 — Task 131 계획/상태 문서 경로 추측과 Windows 와일드카드 인자 오류

- 증상: 읽기 전용 preflight에서 루트의 `PROJECT_PA_FULL_GAME_BACKLOG.md`/`PROJECT_PA_CORE_SLICE_PLAN.md`를 존재하지 않는 `AI_WORKFLOW/03_PLANNING` 아래로 추측했다. 뒤이어 실제 `Automation/LoopEngineering/State/loop-state.json`을 상위 폴더로 추측해 한 번 더 `ItemNotFoundException`이 발생했다.
- 추가 증상: B06 참조 검색에서 Windows의 `rg` 경로 인자로 `PROJECT_PA_*.md`를 직접 전달해 오류 123이 발생했다. 유효한 명시 경로 검색 결과는 앞에서 출력됐지만 명령 전체는 종료 코드 1이었다.
- 원인: 이미 제공된 정확한 파일명을 디렉터리 구조까지 일반화했고, PowerShell/Windows에서 wildcard를 `rg`의 경로 인자로 넘겼다.
- 시도한 것: 존재하지 않는 계획 경로 1회, 존재하지 않는 loop-state 경로 1회, 잘못된 wildcard 인자 1회. 같은 추측 경로와 wildcard 명령은 재사용하지 않았다.
- 해결: `Get-ChildItem -Recurse -File -Filter`로 실제 경로를 한 번씩 확정한 뒤 루트 계획 문서와 `Automation/LoopEngineering/State/loop-state.json`을 명시 경로로 읽었다. 이후 검색은 확인된 개별 파일/디렉터리만 사용했다.
- 영향: 모두 코드 수정 전후의 읽기 전용 조회 실패다. 씬·프리팹·에셋·저장·패키지·Unity 상태에는 영향이 없고, Task 131 Runtime/Editor 순차 빌드와 30/30 계약은 별도로 통과했다.

## [RESOLVED] 2026-08-06 — WORLD-001 WorldGridDebugView 스크립트 자산명 불일치

- 증상: `WorldSandbox` builder 직후의 같은 도메인 검사는 `WorldGridDebugView` 1개를 확인했지만, 첫 D3D11 validator 프로세스가 씬을 다시 연 뒤에는 `WorldGridService`만 남고 `WorldGridDebugView` 수가 0이어서 Play Mode 진입 전에 중단됐다.
- 원인: 12경로 상한을 지키려고 두 `MonoBehaviour`를 `WorldGridService.cs` 한 파일에 함께 선언했다. Unity 씬 재로드에 필요한 스크립트 자산명과 `WorldGridDebugView` 클래스명이 일치하지 않아, 같은 도메인의 `AddComponent<T>`는 성공해도 다음 Editor 프로세스에서 해당 컴포넌트를 복원할 수 없었다.
- 시도한 것: builder 1회 PASS 뒤 전용 validator 1회 실패. 좌표/데이터 검사는 시작 전이었고 기존 씬·저장·Packages·ProjectSettings는 변경되지 않았다.
- 복구 계획: 이미 만든 Editor 도구 파일과 `.meta`를 `Assets/Scripts/World/WorldGridDebugView.cs`로 이동해 GUID를 재사용하고, 실제 컴포넌트 이름과 파일명을 맞춘다. debug 구현은 공통 base에 두며 Editor 도구는 같은 파일의 `UNITY_EDITOR` 구간에 유지한다. WorldSandbox만 builder로 다시 저장한 뒤 전용 validator를 한 번만 재실행한다.
- 중단 조건: 같은 `WorldGridDebugView` 재로드 실패가 반복되면 세 번째 실행 없이 중단한다.
- 복구 진행: 파일명/GUID 복구 후 builder는 재로드 가능한 `WorldGridDebugView` 1개와 Missing Script/Reference 0으로 PASS했다. 다음 validator는 해당 원인이 아니라 기존 `PA_RuntimeSceneBinder`가 Play Mode에서 전역 서비스 root를 만드는 동안 runtime root도 정확히 3개라고 가정한 validator 계약 때문에 멈췄다. Edit Mode의 authored root 3개/manager 0 계약은 유지하고, Play Mode는 필수 root 보존·동일 manager 중복 0·Shop/NPC instance 0을 검사하도록 분리한다.
- 해결: 수정된 계약으로 `WorldSandbox`를 다시 열어 D3D11 Play Mode 검증을 완료했다. authored root 3개, `WorldGridDebugView` 1개, Missing Script/Reference 0, runtime 전역 manager 중복 0, Shop/NPC instance 0을 확인했고 256셀·10,000회 좌표 왕복·경계·chunk·읽기 전용·debug geometry 검사가 모두 PASS했다. 최종 로그는 `Logs/WORLD001_Validation_Final.log`이며 blocking Console Error/Exception/Assert와 신규 crash는 0이다.

## [RESOLVED] 2026-08-06 — WORLD-002 합성 Chunk seam 검사 범위 오류

- 증상: 첫 D3D11 WORLD-002 validator에서 단일 Chunk의 256 top face, 280 cliff face, winding/normal/index, 결정론 checksum까지 통과한 뒤 합성 2-Chunk seam의 `17개` 기대 검사만 실패했다.
- 원인: 경계 X 좌표의 모든 정점을 모아 상면 17개뿐 아니라 월드 남·북 외곽 cliff cap의 동일한 아래쪽 정점까지 포함했다. 두 Chunk의 경계 위치 집합은 같은 방식으로 생성되지만 검사 수량의 범위가 상면 seam 계약보다 넓었다.
- 시도한 것: 구현/씬을 바꾸지 않고 첫 validator를 중단했다. 로그는 `Logs/WORLD002_Validation.log`이며 Unity native crash, compile error, scene/save/package 변경은 없다.
- 복구 계획: 합성 평면의 권위 높이 2m에 있는 상면 경계 정점만 모아 Z=-1..31의 17개 위치와 양쪽 집합 동일성을 검사한다. 수정 뒤 같은 validator는 한 번만 재실행한다.
- 중단 조건: 같은 상면 seam 불일치가 반복되면 세 번째 실행 없이 WORLD-002를 중단한다.
- 해결: 상면 경계 높이로 한정한 두 번째 D3D11 검사에서 두 Chunk의 17개 경계 위치가 정확히 일치했다. 최종 WORLD-002 Edit/Play Mode 검증도 top face 256, cliff face 280, checksum `ADF9201BC8265BC5`, blocking Console 0으로 통과했다. 증거는 `Logs/WORLD002_Validation_Final.log`에 남겼다.

## [RESOLVED] 2026-08-06 — WORLD-001 회귀 검증 배치 진입점 이름 오기

- 증상: WORLD-002 완료 직전 WORLD-001 D3D11 회귀 명령이 `executeMethod method 'RunWorldSandboxValidation' ... could not be found`로 검증 본문 진입 전에 종료됐다.
- 원인: 실제 공개 진입점 `PA_WorldSandboxTools.RunValidation` 대신 존재하지 않는 메서드 이름을 명령 인자로 전달했다.
- 영향: Unity 스크립트 컴파일은 오류 0으로 끝났고 코드·씬·에셋은 변경되지 않았다. 기능 실패나 회귀 결과가 아니다.
- 해결: 잘못된 명령을 재사용하지 않고 실제 공개 진입점으로 한 번만 다시 실행해 D3D11 회귀 결과를 확정한다. 같은 진입 오류가 반복되면 세 번째 실행 없이 중단한다.
- 최종 결과: `PA_WorldSandboxTools.RunValidation` 재실행에서 Edit/Play 검증, 256셀 결정성, 10,000회 좌표 왕복, debug mesh, blocking Console 0이 모두 통과했다. 증거는 `Logs/WORLD002_WORLD001_Regression_Final.log`에 남겼다.

## [RESOLVED] 2026-08-06 — WORLD-004 validator 지역 변수 definite-assignment 컴파일 오류

- 증상: WORLD-004 구현 뒤 첫 Runtime compile에서 `WorldChunkTerrain.cs`의 validator가 `CS0165: 할당되지 않은 'pond' 지역 변수를 사용했습니다` 한 건으로 중단됐다.
- 원인: `Require(water.Succeeded && TryGetCell(..., out pond) && ...)` 조건식에서 할당한 지역 변수를 다음 `Require`에서도 사용했으나, C# 컴파일러는 사용자 정의 `Require`의 성공이 해당 `out` 할당을 보장한다고 추론하지 않는다.
- 영향: validator 표현 한 곳의 컴파일 문제이며 surface transaction, mesh, scene, Save, Package, ProjectSettings에는 실행 또는 변경 영향이 없다. Unity는 아직 실행하지 않았다.
- 해결: `WorldCellData pond`를 조건식 전에 명시 선언해 definite-assignment를 보장하고 같은 순차 Runtime/Editor compile을 한 번만 다시 실행한다. 동일 오류가 반복되면 세 번째 시도 없이 중단한다.
- 복구 실패 및 중단: 선언을 조건식 앞으로 옮겼지만 초기값을 주지 않아 컴파일러의 definite-assignment 판정은 달라지지 않았고, 두 번째 Runtime compile도 같은 `CS0165`로 중단됐다. 규칙에 따라 세 번째 compile과 추가 구현을 수행하지 않는다.
- 다음 재개 지점: 새 continuation에서 먼저 `WorldCellData pond = default;`로 초기화하거나 `TryGetCell` 성공 검사를 별도 문장으로 분리한 뒤, Runtime compile을 1회만 실행한다. 통과하기 전에는 Editor compile, Unity D3D11 validator, WORLD-004 완료 문서, 로컬 commit, WORLD-005를 시작하지 않는다.
- 최종 결과: 새 continuation에서 `pond`를 `default`로 초기화하고 물 편집 성공·셀 조회·water invariant 검사를 별도 문장으로 분리했다. 이어서 Runtime/Editor 순차 compile이 모두 오류 0으로 통과했으며 기존 `CS8785`와 Editor `CS0414` 경고만 유지됐다.

## [RESOLVED] 2026-08-06 — WORLD-004 water collider validator의 vertex 순서 가정 오류

- 증상: 첫 D3D11 WORLD-004 검증에서 물 셀 생성, shoreline 및 전용 water material slot은 통과했으나 `water geometry is excluded from terrain collider triangle streams` 계약이 실패했다.
- 원인: validator가 모든 terrain collider index가 `WaterIndices.Min()`보다 작아야 한다고 가정했다. 그러나 물 셀이 Chunk 순회 초반에 있으면 그 뒤 생성되는 정상 지면·절벽 vertex index는 첫 water vertex보다 커지므로, 실제 collider 포함 여부와 관계없이 실패한다.
- 영향: 실패 지점은 Edit Mode validator의 검증식이다. 생성기는 물 triangle을 `WaterIndices`에만 추가하고 collider mesh는 `TopIndices`와 `CliffIndices`만 사용하지만, Play Mode raycast 증거까지는 아직 도달하지 못했다.
- 복구 계획: 물 전용 vertex index 집합과 `TopIndices`/`CliffIndices`의 교집합이 비어 있는지 직접 검사한다. 같은 D3D11 validator를 한 번만 재실행하고, 동일 계약이 다시 실패하면 추가 시도 없이 중단한다.
- 최종 결과: `WaterIndices`의 vertex 집합과 `TopIndices`/`CliffIndices`의 교집합이 비어 있는지 직접 검사하도록 수정했다. 두 compile이 오류 0으로 통과했고, 두 번째 D3D11 실행은 `EDIT_MODE_PASS`, `PLAY_MODE_PASS`, `FINISHED_PASS ground=4 path=2 water=true shoreline=true walkability=true rollback=true`로 완료됐다. collider raycast는 수면이 아니라 terrain bed를 맞았으며 blocking 예외와 새 crash는 0이었다.

## [RESOLVED] 2026-08-10 — WORLD-005 scene-local MonoScript 직렬화

- 증상: 첫 Editor builder 저장에서 placement service와 debug controller가 asset GUID 대신 scene-local `MonoScript` 객체를 참조했다.
- 원인: attachable MonoBehaviour 두 개가 클래스명과 일치하지 않는 하나의 `WorldBuildingPlacement.cs`에 함께 있었다.
- 영향: 첫 builder는 기능 코드를 정상 compile했지만 GUID/meta 장기 안정성 기준을 만족하지 못했다. Prototype_FirstDay, MainGame, prefab, Save, Packages, ProjectSettings에는 영향이 없었다.
- 해결: service와 controller를 각각 클래스명과 일치하는 파일로 분리하고 meta GUID를 고정한 뒤 Editor builder가 두 컴포넌트를 재부착했다. 최종 WorldSandbox는 direct GUID reference 2개, embedded MonoScript 0, authored root 3이며 전체 validator와 회귀가 통과했다.

## [RESOLVED] 2026-08-10 — WORLD-002 회귀 executeMethod 클래스명 오기

- 증상: WORLD-005 회귀 묶음 중 WORLD-002 명령이 `PA_WorldTerrainTools` 클래스를 찾지 못해 validator 본문 진입 전에 종료됐다.
- 원인: 실제 공개 진입점 `PA_WorldChunkTerrainTools.RunTerrainValidationBatch` 대신 존재하지 않는 클래스명을 전달했다.
- 영향: compile/validator 구현 실패가 아니며 코드·씬·에셋 변경 및 crash는 0이었다. 뒤의 WORLD-001 명령은 같은 shell이 중단되어 별도로 실행했다.
- 해결: 잘못된 명령을 반복하지 않고 실제 선언을 확인한 뒤 올바른 진입점으로 한 번 실행했다. `Logs/WORLD005_WORLD002_Regression_Rerun.log`는 Edit/Play 및 blocking Console 0으로 `FINISHED_PASS`했고 WORLD-001도 이어서 PASS했다.

## [RESOLVED] 2026-08-10 — WORLD-006B 고정 판매대 이동 취소 Transform 스냅

- 증상: 첫 D3D11 WORLD-006B validator에서 보호 셀 거부와 원자성까지 통과했지만, 실제 배치 장부의 `가까운 가구 이동`을 연 뒤 닫았을 때 판매대가 제작된 시작 Transform과 정확히 일치하지 않았다. 셀·회전 record는 그대로였다.
- 원인: `CancelActivePlacement`가 이동 시작 Transform을 보존하지 않고 `anchor`와 `rotation`에서 셀 중심 Transform을 다시 계산했다. 최초 배치가 grandfather된 고정 판매대처럼 제작 오프셋을 가진 경우 취소가 같은 셀 안에서 시각적 스냅을 만들었다.
- 영향: Play Mode 런타임 검증에서만 발생했고 씬은 저장되지 않았다. 재고, `ShopSlot`, 점유 record, SaveData, Packages, ProjectSettings에는 손상이 없으며 신규 crash도 없다.
- 복구: 이동 시작 시 실제 world position/rotation을 캡처하고, 취소 시 해당 Transform을 정확히 복원하는 최소 수정만 적용한다. 컴파일 뒤 같은 validator를 한 번만 재실행하며 동일 실패가 반복되면 세 번째 시도 없이 중단한다.
- 최종 결과: 수정 뒤 D3D11 검증에서 실제 배치 장부의 이동 취소가 authored 시작 위치·회전을 정확히 복원했다. 이어서 이동·270° 회전, 고객 완전 경로, 146G 판매, v10 배치 투영과 세 회귀가 모두 PASS했다.

## [RESOLVED] 2026-08-10 — WORLD-006B validator의 ShopSlot 단일 수량 판매 가정

- 증상: 취소 복원 수정 뒤 validator는 고객의 완전 NavMesh 경로까지 통과했고 실제 판매 로그도 `BreadLoaf +146G`를 남겼지만, 검증기는 판매 실패로 판정했다.
- 원인: 실제 `ShopSlot.TryPurchaseByNpc`는 진열된 스택 전체를 한 번에 판매한다. 검증기는 2개 × 73G 중 한 개만 팔려 73G와 잔여 1개가 남는다고 잘못 가정했다.
- 영향: 판매·경제·통계 기능은 정상 작동했고 EconomyService 잔액은 500G→646G로 증가했다. 실패는 validator assertion 하나뿐이며 씬·저장·패키지에는 영향이 없다.
- 해결: 기존 스택 전체 판매 계약에 맞춰 146G 입금과 빈 슬롯을 검사한다. 이는 구현 수정이 아닌 명백한 validator 가정 정정이며, 해당 원인으로는 한 번만 재실행한다.

## [RESOLVED] 2026-08-10 — WORLD-007 격리 repository fixture 디렉터리 누락

- 증상: 첫 D3D11 WORLD-007 validator가 v10→v11, 128×128 seed, 4개 sparse edit, B09와 resource capture까지 통과한 뒤 `Logs/WorldPersistence/<timestamp>/world007.json` 쓰기에서 `DirectoryNotFoundException`으로 중단됐다.
- 원인: custom-root `LocalJsonSaveRepository`는 전달된 root가 이미 존재한다는 기존 계약인데 validator가 timestamp 디렉터리를 만들지 않았다.
- 영향: 격리 증거 파일이 생성되기 전의 fixture 실패다. 프로덕션 persistentDataPath, 저장 파일, live Scene/Prefab/Packages/ProjectSettings와 기존 사용자 저장에는 영향이 없고 crash도 없다.
- 복구: validator가 custom repository를 만들기 직전에 해당 격리 디렉터리를 생성한다. 구현 저장 경로는 바꾸지 않으며 같은 validator를 한 번만 재실행한다.
- 확인: `Logs/WORLD007_Validation_Pass.log`에서 격리 JSON 저장·로드와 전체 WORLD-007 검증이 PASS했다. 프로덕션 repository 계약 변경은 없다.

## [RESOLVED] 2026-08-10 — WORLD-007 dry terraform waterSurface 불변식 불일치

- 증상: repository JSON 왕복까지 통과한 두 번째 WORLD-007 실행에서 높이를 0→1로 올린 dry cell `(59,13)`이 `waterDepthLevels=0`, `waterSurfaceLevel=0`으로 저장되어 restore preflight에 거부됐다.
- 원인: `WorldCellData.WithElevationLevel`이 dry cell에서도 이전 `WaterSurfaceLevel`을 복사했다. 기존 생성자와 snapshot 계약은 물 없는 셀의 `waterSurfaceLevel == elevationLevel`을 요구한다.
- 영향: 잘못된 delta는 live world 적용 전에 거부되어 복원 상태나 사용자 저장 손상은 없다. 물 셀 terraform은 기존 preflight가 계속 차단한다.
- 복구: dry cell의 높이를 바꿀 때만 water surface sentinel을 새 elevation과 동기화한다. 실제 물이 있는 셀은 기존 값을 유지하며, 이 원인으로 validator를 한 번만 재실행한다.
- 확인: `Logs/WORLD007_Validation_Pass.log`에서 높이·지면·길·물 4개 sparse delta의 repository round-trip과 오염 payload 사전 거부가 PASS했다.

## [RESOLVED] 2026-08-10 — WORLD-008 신규 validator C# definite-assignment/import 충돌

- 증상: 첫 D3D11 compile에서 신규 `WorldNavigationService.cs`만 `Debug` 이름 충돌과 short-circuit `out` 변수 definite-assignment 오류로 중단됐다.
- 원인: `System.Diagnostics` 전체 import가 `UnityEngine.Debug`와 충돌했고, 조건식 뒤에서 사용하는 `level`/`destinationHit`을 조건 내부에서 선언해 C# 컴파일러가 항상 할당된 것으로 증명할 수 없었다.
- 영향: 새 WORLD-008 assembly가 생성되기 전의 정적 오류다. Scene·Prefab·Packages·ProjectSettings·SaveData와 기존 런타임 상태에는 변화가 없고 Unity native crash도 없다.
- 복구: `Stopwatch`만 alias하고 `out` 변수를 조건식 전에 초기화한다. 기능 설계나 API는 바꾸지 않으며 이 원인에 대한 컴파일 재시도는 한 번만 수행한다.
- 확인: `Logs/WORLD008_Compile_Retry.log`와 강화된 `Logs/WORLD008_Strengthened_Compile.log`가 Runtime/Editor compile 오류 0으로 종료됐고, `Logs/WORLD008_Validation_Final.log`도 전체 PASS했다.

## [RESOLVED] 2026-08-11 — WORLD-009 validator 두 번째 채집 사유 definite-assignment

- 증상: WORLD-009 첫 D3D11 compile에서 `WorldGameplayAdapterService.cs` validator의 `gatherReasonB`가 `CS0165`로 중단됐다.
- 원인: 두 번의 `TryGatherNext`를 `&&`로 연결한 조건 안에서 두 번째 `out` 변수를 선언한 뒤 Require 메시지에서 사용해, 첫 호출 실패 시 두 번째 호출이 실행되지 않는 단락 평가를 컴파일러가 정확히 감지했다.
- 영향: 신규 validator assembly 생성 전의 정적 오류다. 런타임 adapter, Scene, Prefab, Packages, ProjectSettings, SaveData와 사용자 저장에는 실행 또는 변경 영향이 없고 Unity native crash도 없다.
- 복구: 두 채집 호출을 별도 bool 문장으로 평가한 뒤 결과를 함께 검증한다. 기능/API를 바꾸지 않으며 같은 D3D11 validator는 한 번만 재실행한다. 동일 오류가 반복되면 추가 구현 없이 중단한다.
- 확인: 두 호출을 별도 문장으로 분리한 재시도에서 Runtime/Editor compile과 WORLD-009 Edit Mode 계약이 오류 0으로 통과했다. 다음 중단은 이 원인이 아니라 기존 `PA_RuntimeSceneBinder` 전역 권위와 신규 어댑터 권위 수량 계약의 통합 문제였다.

## [RESOLVED] 2026-08-11 — WORLD-009 기존 RuntimeSceneBinder 권위 중복 생성

- 증상: WORLD-009 첫 Play Mode 검증에서 D3D11과 adapter Ready는 통과했지만 Economy/GameClock/DayNight/Save/ItemRegistry를 각 1개만 유지한다는 수량 계약이 실패했다.
- 원인: 모든 씬에서 먼저 실행되는 기존 `PA_RuntimeSceneBinder`가 `[Services]`에 전역 권위를 이미 제공하는데, 초기 adapter 구현이 별도 비활성 runtime root에 같은 컴포넌트를 추가했다. 활성화 시 일부 singleton은 지연 파괴되고 `SaveManager`처럼 자체 중복 방어가 없는 타입은 그대로 중복됐다.
- 영향: validator가 자원·제작·판매를 시작하기 전에 중단됐다. 격리 repository 주입 전이므로 사용자 저장 영향은 없고 Scene/Prefab/Packages/ProjectSettings/SaveData 변경 및 Unity native crash도 없다.
- 복구: adapter는 `PA_RuntimeSceneBinder`가 만든 기존 권위를 조회·채택하고, generated runtime root에는 Player Inventory와 B01/B05 기능 인스턴스만 둔다. 수량 진단을 assertion에 포함하고 같은 Play Mode 경로를 한 번 검증한다.
- 확인: 수정 후 Economy/GameClock/DayNight/Save는 모두 정확히 1개로 확인되어 별도 manager stack 중복이 해소됐다. 남은 `Inventory=0` 표시는 실제 파괴가 아니라 `HideFlags.DontSave` 런타임 증거를 제외하는 validator 조회 API 문제로 분리됐다.

## [RESOLVED] 2026-08-11 — WORLD-009 DontSave Player Inventory 조회 누락

- 증상: 기존 전역 권위 채택 수정 후 수량 진단은 `adapter=1, inventory=0, economy=1, clock=1, loop=1, save=1`로 종료됐다.
- 원인: player와 gameplay runtime root는 scene YAML 오염 방지를 위해 `HideFlags.DontSave`다. validator가 사용한 `Object.FindObjectsByType<Inventory>`는 이 객체를 결과에서 제외했지만 adapter의 직접 참조와 `Inventory.instance`는 정상 활성 상태였다.
- 영향: 수량 assertion 이전까지 adapter Ready와 기존 전역 권위 단일화는 통과했다. 자원/제작/판매와 저장은 아직 실행 전이며 사용자 저장, Scene, Prefab, Packages, ProjectSettings, SaveData 영향 및 crash는 없다.
- 복구: `Resources.FindObjectsOfTypeAll<Inventory>` 결과를 유효하고 로드된 scene 객체로 제한해 DontSave runtime inventory를 세고, 그 하나가 `Inventory.instance`와 adapter의 PlayerInventory에 동일한지 함께 검증한다. 프로덕션 로직은 변경하지 않는다.
- 확인: `Logs/WORLD009_Validation_DontSaveFix.log`에서 adapter/player/B01/B05 권위, Timber 2회 채집, Plank 제작·진열, NpcController 이동·구매, Economy 수익, 격리 SaveManager 저장, 재시작 상태 변조, seed/resource/inventory/clock/shop/economy 복원이 모두 PASS했다. blocking Console Error/Exception/Assert와 신규 crash는 0이다.

## [RESOLVED] 2026-08-11 — WORLD-001 회귀의 runtime Light 1개 구가정

- 증상: WORLD-009 전용 validator와 WORLD-007/008, CraftingRecipeCard, CustomerArrival, FinalDemoRoute 회귀 통과 후 WORLD-001 회귀가 Play Mode의 `scene has exactly one Light`에서 중단됐다. Edit Mode authored scene의 Light 1개, Missing Script/Reference 0 계약은 이미 통과했다.
- 원인: WORLD-001 당시 WorldSandbox에는 gameplay instance가 없었지만, WORLD-009가 실제 B01/B05 기능 프리팹을 runtime-only root에 연결하면서 해당 기능 에셋의 보조 Light가 함께 활성화됐다. validator가 authored light와 승인된 runtime helper light를 구분하지 않았다.
- 영향: 첫 WORLD-001 runtime assertion 단계의 구계약 실패다. WorldGrid와 adapter 전용 검증은 정상이며 Scene YAML, Prefab, Packages, ProjectSettings, SaveData, 사용자 저장 및 crash 영향은 없다.
- 복구: Edit Mode는 정확히 Light 1개를 계속 요구한다. Play Mode는 authored `Directional Light` root의 Directional Light 1개를 유지하면서 추가 Light가 오직 `WorldGameplay_Runtime` 아래에만 있는지 검사한다. 무관한 runtime light는 계속 거부한다.
- 확인: 수정 후 같은 회귀에서 authored Directional Light 1개와 adapter-local helper Light 1개만 존재하는 계약이 PASS했다. 다음 중단은 Light가 아니라 WORLD-009 이후에도 runtime grid가 16×16일 것이라는 별도의 구가정이었다.

## [RESOLVED] 2026-08-11 — WORLD-001 회귀의 runtime 16×16 fixture 구가정

- 증상: Light 계약 수정 후 WORLD-001 회귀가 Play Mode grid `128×128`을 발견하고 `16×16` 기대에서 중단됐다. Edit Mode의 authored 16×16 fixture 검증은 전체 PASS했다.
- 원인: WORLD-001은 authored fixture와 Play Mode authority가 동일한 시절의 validator다. WORLD-009 adapter는 승인된 deterministic seed 9009의 provisional 128×128 결과를 실제 `WorldGridService`에 설치하므로 runtime 크기·지면/물/길 구성·chunk 수·checksum이 의도적으로 달라진다.
- 영향: generated gameplay adapter의 정상 부트 이후 stale assertion만 실패했다. 전용 WORLD-009와 WORLD-007/008 및 Golden gameplay 회귀는 PASS이며 Scene/Prefab/Packages/ProjectSettings/SaveData/사용자 저장과 crash 영향은 없다.
- 복구: Edit Mode는 기존 16×16, 256셀, 단일 chunk checksum 계약을 그대로 유지한다. 승인 adapter가 Ready인 Play Mode는 128×128, 16,384셀, 8×8 chunk 좌표, 유효 surface enum, deterministic seed 9009 checksum을 검증하고 이후 프레임 안정성은 runtime checksum끼리 비교한다.
- 확인: WORLD-009 회귀에서 authored fixture와 generated runtime 권위를 분리한 뒤 PASS했고, WORLD-010은 같은 128×128 grid를 64개 visible chunk로 투영해 restart 후에도 유지했다.

## [RESOLVED] 2026-08-11 — WORLD-010 컴파일 definite-assignment 오류 2건

- 증상: 첫 컴파일에서 `WorldAlphaPlayableController`의 `out reason`, 세 번째 컴파일에서 validator의 `gatherB`/`fishReason`이 `CS0165`로 중단됐다.
- 원인: null 검사와 `out` 호출을 `&&` short-circuit 식으로 결합한 뒤 해당 지역 변수를 실패 메시지에서 사용했다.
- 영향: 새 WORLD-010 assembly가 생성되기 전의 정적 오류였다. Scene/Prefab/Packages/ProjectSettings/SaveData와 런타임 상태에는 영향이 없고 native crash도 없었다.
- 해결: 모든 `out` 값을 조건문 전에 초기화하고 호출 결과를 별도 bool 문장으로 평가했다. `Logs/WORLD010_Compile_04.log`, `Logs/WORLD010_Compile_05.log`에서 Runtime/Editor compile 오류 0을 확인했다.

## [RESOLVED] 2026-08-11 — WORLD-010 validator의 가짜 scene restart

- 증상: 첫 M70 검증은 save 전 단계까지 통과했지만 `SceneManager.LoadScene` 직후 새 `WorldAlphaPlayableController`를 찾지 못해 중단됐다.
- 원인: 같은 Play Mode 안의 scene reload는 프로젝트 전역 `RuntimeInitializeOnLoadMethod` 조립을 다시 수행하지 않는다. validator가 이를 실제 Play Mode restart와 같은 계약으로 잘못 취급했다.
- 영향: production save/load나 runtime 조립 결함이 아니라 검증 절차의 restart 모델 오류였다. 저장 파일은 격리된 `Logs/WorldAlpha/<timestamp>` 아래에만 생성됐고 신규 crash는 없었다.
- 해결: validator가 실제로 Play Mode를 종료하고 저장된 깨끗한 WorldSandbox를 다시 연 뒤 Play Mode에 재진입하도록 변경했다. `Logs/WORLD010_M70_Validation_02.log`에서 save→exit→re-enter→load와 v11 checksum까지 PASS했다.

## [RESOLVED] 2026-08-11 — DayNightShopLoop Golden validator의 비판매 Seed 선택

- 증상: 첫 Golden DayNightShopLoop 실행은 실제 판매 가능한 준비 지점이 존재하는데도 sellable inventory 기대값에서 실패했다.
- 원인: validator가 sellable Fish/Wheat/Ore와 non-sellable Seed가 섞인 목록에서 임의의 첫 두 지점을 골랐다. Seed를 수집한 뒤에도 두 항목 모두 판매 가능하다고 가정했다.
- 영향: 실제 낮 준비·판매 기능의 회귀가 아니라 비결정적인 validator fixture 선택 문제였다.
- 해결: 기존 ShopSlot 계약에 맞춰 sellable 준비 지점을 우선 정렬하고 두 개가 존재하는지 명시적으로 검증했다. `Logs/WORLD010_Golden_DayNightShopLoop_02.log`에서 `sellableInventory=20`으로 PASS했다.

## [RESOLVED] 2026-08-11 — WORLD-009 회귀 checksum의 B01 가구 누락

- 증상: M70에서 B01 판매대를 이동·저장하도록 연결한 뒤 첫 WORLD-009 회귀가 restore checksum 기대값에서만 실패했다.
- 원인: 구 validator의 기대 checksum은 terrain/building/resource만 캡처하고, 이제 기존 `PlaceableSaveData`로 투영되는 B01 판매대 가구 record를 포함하지 않았다.
- 영향: 실제 restore 결과는 정상이며 검증 기대값만 오래된 상태였다. SaveData schema는 v11 그대로다.
- 해결: 기대 checksum도 실제 adapter capture를 사용하도록 정합했다. `Logs/WORLD010_Regression_WORLD009_02.log`에서 seed/resource/inventory/clock/shop/economy/furniture restore와 Console 0이 PASS했다.

## [RESOLVED] 2026-08-11 — 승인된 M70 범위에서 blocked → active → blocked 반복

- 증상: WORLD bounded ticket 완료 뒤 다음 사람 메시지를 요구하는 기본 규칙이 이미 선승인된 M70 연속 범위에도 적용되어 동일한 `next ticket required` blocker가 반복됐다.
- 원인: bounded-ticket 기본 게이트는 있었지만, 사람이 milestone과 ticket sequence를 명시적으로 선승인한 경우를 표현하고 소비하는 제한적 정책 예외가 없었다.
- 해결: `PREAPPROVED_MILESTONE_CONTINUATION`을 추가했다. loop-state에 승인 범위가 기록되고 다음 ticket이 그 범위 안에 있을 때만 한 번에 하나씩 검증·로컬 커밋 후 자동 전환하며, 동일 blocker는 한 번만 기록한다.
- 안전 경계: M70은 `WORLD-005`, `WORLD-006`, `WORLD-006B`, `WORLD-007`, `WORLD-008`, `WORLD-009`, `WORLD-010`만 승인한다. hard blocker, D3D11/crash, Git 안전 규칙은 유지하고 WORLD-010 또는 `M70_PLAYABLE_WORLD_ALPHA_COMPLETE`에서 중단한다.
- 현재 상태: 기준 commit `e0b5678`에서 위 sequence가 이미 완료됐으므로 재실행하지 않았다. `nextTicket=null`, completion reached로 기록했고 WORLD-011/MainGame은 새 사람 승인 대상이다.

## [RESOLVED] 2026-08-11 — BETA-003 workbench 전환 validator의 deferred card destruction

- 증상: `Logs/BETA003_D3D11_Validation.log`와 `Logs/BETA003_D3D11_Validation_Retry.log`에서 B05/B06/B07 런타임 배치, D3D11, walkable cell, 시설 간격, 역할 표지, Kitchen 카드 3개의 활성·크기·viewport·재료 부족 표시까지 PASS한 뒤 Forge 카드 수가 5개로 집계되어 중단됐다.
- 원인: `CraftingUI.GenerateSlotsForContext`는 이전 카드를 Play Mode의 지연 `Destroy`로 제거하고 새 카드를 같은 프레임에 만든다. validator가 Forge 컨텍스트를 연 그 프레임에 Kitchen 카드 3개와 Forge 카드 2개를 함께 세었다. 첫 재시도는 Kitchen 검증과 Forge 열기 사이에 프레임을 두었지만, 실제 제거 예약은 Forge를 여는 시점에 발생하므로 같은 원인이 반복됐다.
- 영향: 실제 제작·품질·가격·B01 판매 assertion에 도달하기 전의 validator 순서 문제다. Runtime/Editor 정적 컴파일은 오류 0이며 Scene/Prefab/Packages/ProjectSettings/Save schema 변경, 사용자 저장 영향, native crash는 없다. BETA-003은 완료 또는 커밋되지 않았고 M85 다음 티켓도 시작하지 않았다.
- 중단: 프로젝트 규칙의 동일 원인 2회 실패 후 세 번째 시도 금지에 따라 이 세션에서는 추가 Unity 실행을 하지 않는다.
- 권장 다음 조치: 새 세션에서 Forge를 한 번 연 뒤 최소 한 프레임을 기다리고 카드 수를 검사하도록 validator stage를 분리한다. 프로덕션 UI 로직을 변경할 필요는 없다. 그 후 BETA-003 D3D11 통합 검증을 1회 수행하고, 통과할 때만 회귀·문서·로컬 티켓 커밋으로 진행한다.
- 사람 승인 및 해결: 2026-08-12 사람이 stage 최소 수정과 세 번째 격리 D3D11 실행을 명시적으로 승인했다. Kitchen·Forge·Basic 각 컨텍스트를 연 뒤 다음 validator stage/frame에서 검사하도록 분리했다. `Logs/BETA003_D3D11_Validation_ThirdApproved.log`에서 카드 수 3/2/2, 재료 부족 무차감·무지급, 5개 실제 제작, 품질·가격, B01 진열·판매와 Console 0이 PASS했다. 네 번째 재시도는 필요하지 않았다.

## [HARD_BLOCKER] 2026-08-12 — BETA-005 짧은 고객 방문의 성향 패널 관찰 프레임 누락

- 증상: `Logs/BETA005_D3D11_Validation.log`와 `Logs/BETA005_D3D11_Validation_Final.log`가 모두 Miner/Tailor 실데이터, D3D11, 고가 Miner 방문 시작과 월드/HUD 성향 표시를 PASS한 뒤 `the visible Miner preference remains readable during the actual visit` assertion에서 중단됐다.
- 확인된 실제 경로: 두 실행 모두 `Miner_01`이 Plank 250G를 `PurchaseEvaluator`로 평가해 `p=0.00`, PASS(보류) 결정을 냈고 `SalesLogManager`가 Day 2 보류 1건을 기록했다. native crash, Scene/Prefab/Packages/ProjectSettings/Save schema 변경은 없다.
- 원인: validator가 stage 전환마다 처음 3번의 Editor update를 일괄 건너뛴다. WorldSandbox의 짧은 고객 이동·평가가 그 사이 완료되어, 실제로 갱신된 `CustomerPreferencePresentationController`를 validator가 non-idle 프레임에 샘플링하지 못했다. 고객 속도/구경 시간과 FSM 시작 직후 명시 refresh 보정 후에도 같은 validator 관찰 assertion이 반복됐다.
- 영향: BETA-005의 두 번째 Tailor 저가 구매와 최종 구매/거절 통합 assertion에는 도달하지 못했다. 구현은 미커밋 상태로 보존하며 BETA-006은 시작하지 않는다.
- 중단: 동일 원인 2회 실패 규칙에 따라 세 번째 BETA-005 D3D11 실행과 추가 validator 수정은 금지한다. BETA-005는 `BETA_005_HARD_BLOCKER`다.
- 최소 후속 조치: 사람 승인 후 validator의 전역 `frames < 4` 대기를 초기 부트 stage에만 적용하거나 stage 1/2에서는 첫 update부터 `ObservePreference`를 실행하도록 순서만 고친 뒤 D3D11을 1회 실행한다. assertion 삭제·경고화·하드코딩·프로덕션 권위 변경은 하지 않는다.
- 승인 실행 결과(2026-08-13): 전역 frame skip을 bootstrap stage 0에만 제한하고 승인된 추가 D3D11 1회를 실행했다. 카테고리 반응을 포함한 stage 0 assertion과 실제 Miner 250G 보류/SalesLog 기록은 PASS했지만, 첫 stage 1 Editor callback 전에 실제 방문이 완료되어 동일 가시성 assertion이 세 번째로 실패했다.
- 현재 판정: `HARD_BLOCKER_BETA_005_VALIDATION`. 승인된 실행 횟수를 모두 소비했으므로 네 번째 실행과 추가 수정을 하지 않는다. 다음 최소 후보는 `TryBeginCustomerVisit` 직후 같은 stage에서 실제 live preference UI를 동기 표본화하는 것으로, 새 사람 승인 대상이다.

## [HARD_BLOCKER] 2026-08-20 — BETA-005 승인 실행 전 Unity Editor 라이선스 부재

- 증상: `Logs/BETA005_D3D11_Validation_SynchronousPreference.log`에서 Unity 6000.3.2f1이 `No valid Unity Editor license found`를 기록하고 return code 198로 종료했다.
- 범위: project load, GfxDevice/D3D11 초기화, `PA_Beta005CustomerStrategyValidator` executeMethod, Play Mode가 모두 시작되지 않았다. 로그의 `[BETA-005]`와 graphics 초기화 표식은 각각 0건이다.
- 사전 결과: 동기 live-HUD 표본 수정은 Runtime/Editor `dotnet build` 오류 0이다. 기존 CS8785/CS0414 경고 외 새 컴파일 문제는 없다.
- 영향: 기능 assertion 결과가 아니므로 BETA-005 PASS나 승인된 validation-debt 판정을 만들 수 없다. 새 crash, Scene/Prefab/Packages/ProjectSettings/Save schema 변경도 없다.
- 중단: 동일 Unity 실행을 자동 재시도하지 않는다. 사용자가 Unity Hub/Editor에서 유효한 라이선스를 복구한 뒤 보존된 13개 경로와 현재 validator에서 재개한다.

## [RESOLVED] 2026-08-20 — BETA-005 라이선스 및 live preference 증거 차단 해소

- 라이선스 해결: Unity Hub/Licensing Client가 Personal activation `statusCode=200`, `LicenseUpdate Added`, EULA `Agreed`를 기록했다. 이전 return code 198 실행은 project load 전 환경 실패였으므로 기능 시도 횟수로 소비하지 않았다.
- 증거 해결: 기본 플레이에서 alpha 0인 개발 `CustomerPreferenceCanvas` 대신 `BeginNewGame()` 이후 실제 WorldSandbox player HUD와 현재 runtime customer를 방문 생성 성공 직후 같은 stage에서 표본화했다. 고객 활성, profile 문구, HUD screen bounds를 실제로 확인하고 숨겨진 Canvas는 진단값으로만 남겼다.
- 결과: `Logs/BETA005_D3D11_Validation_SynchronousPreference_Licensed_Correction.log`가 Miner 보류와 Tailor 구매, 재고·경제·SalesLog·feedback·demand·HUD assertion 및 Console 0으로 `FINISHED_PASS`했다.
- 회귀: BETA-004, CustomerPresentation, CustomerArrival 검증도 모두 D3D11 PASS했다. 새 crash와 Scene/Prefab/Packages/ProjectSettings/Save schema content 변경은 없다.
- 최종 판정: 두 기존 blocker는 종료하고 `BETA_005_COMPLETE`로 기록한다. 자동 캡처만 비차단 `CAPTURE_EVIDENCE_DEBT`로 유지한다.

## [VALIDATION_DEBT] 2026-08-21 — BETA-007 generated resident spawn NavMesh readiness

- 증상: `Logs/BETA007_D3D11_Validation.log`는 WorldSandbox/D3D11/HUD/B08를 PASS한 뒤 generated resident spawn anchor의 NavMesh assertion에서 중단됐다. 승인된 보정은 런타임 carving 뒤 anchor를 실제 NavMesh hit로 재투영하고 준비 상태를 기다리게 했지만 `Logs/BETA007_D3D11_Correction.log`도 `ResidentSpawnAnchorsReady` timeout으로 같은 bootstrap 단계에서 종료됐다.
- 영향: 실제 Farmer hire, Wheat ShopSlot 판매, Day 2 전환, village visual, resident DialogueUI assertion에는 도달하지 못했다. 두 실행 모두 native crash 0이며 Scene/Prefab/Packages/ProjectSettings/SaveData/SaveManager 변경은 없다.
- 원인 분류: 실제 고용 이전에 validator가 종료되어 hired resident 실패는 증명되지 않았다. 다만 생성 시 유효했던 네 anchor 중 하나만 나중에 투영 실패해도 invalid Transform을 영구 보존하고 전체 readiness를 false로 두는 production 위험이 확인됐다.
- 정적 보정: runtime building obstacle과 NavMesh rebuild가 안정된 뒤 anchor를 만들고, current navigation revision에서 재투영되는 지점만 유지해 `HiringService.spawnPointRotation`에 다시 동기화한다. seed 재결속 실패도 성공으로 숨기지 않는다.
- 사후 결과: Runtime/Editor 정적 compile 오류 0. 기존 CS8785/CS0414 경고만 유지된다. 두 허용 실행을 이미 사용했으므로 보정 후 세 번째 D3D11 실행은 하지 않았고 실제 hire→판매→Day 2→대화는 미검증으로 남긴다.
- 판정: validator-only 사유를 HARD BLOCKER로 확대하지 않는 장기 Goal 정책에 따라 `BETA_007_IMPLEMENTED_WITH_VALIDATION_DEBT` checkpoint로 보존한다. assertion 삭제·완화와 결과 하드코딩은 하지 않았다.
