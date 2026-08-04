# Project PA Unity Editor Crash Report - 2026-07-17

## Summary

Task 086의 첫 D3D11 Play Mode 검증은 C# 예외가 아니라 검증기 전용 오프스크린 캡처 중 Unity 네이티브 렌더 스레드에서 충돌했다.

- 프로젝트: `C:\Users\sdjsd\Desktop\Unity\Project_PA`
- Unity: `6000.3.2f1 (a9779f353c9b)`
- 실행 백엔드: `-force-d3d11`
- 실행 메서드: `PA_ThemeCornerValidator.RunThemeCornerValidation`
- 충돌 시각: 2026-07-17 12:39 KST

## Evidence

- 검증 로그: `Logs/Codex_Task086_ThemeCorner.log`
- Unity 크래시 폴더: `C:\Users\sdjsd\AppData\Local\Temp\Unity\Editor\Crashes\Crash_2026-07-17_033941998`
- 크래시 아티팩트: `Codex_Task086_ThemeCorner.log`, `crash.dmp`

충돌 직전까지 다음 항목은 PASS했다.

- `ShopCustomizationController`와 `MerchandisingCornerController` 초기화
- authoritative `shop.interior` grid 확인
- authored ShopSlot 6개 확인
- Raw/Processed 검증 Item 확인
- 빈 진열 상태의 코너 0개·월드 라벨 0개
- 격리 출력 경로 확인

네이티브 스택의 핵심 구간:

```text
GfxDevice::DrawSharedGeometryJobs
DrawUtil::DrawLineOrTrailMultipleFromNodeQueue
ScriptableRenderLoopDraw
UniversalRenderPipeline.RenderSingleCamera
Camera.StandaloneRender
Camera_CUSTOM_Render
PA_ThemeCornerValidator.CaptureGameCamera
```

실제 호출 위치는 당시 `Assets/Editor/PA_ThemeCornerValidator.cs:364`의 `camera.Render()`였다. PNG 파일 쓰기 전 충돌했다.

## Cause Classification

가장 좁은 원인 분류:

- Play Mode가 이미 프레임을 렌더링하는 동안 검증기가 별도 1920×1080 RenderTexture로 URP 카메라를 즉시 `Camera.Render()`한 경로의 네이티브 충돌.
- 스택은 Line/Trail 공유 지오메트리 제출 중 충돌을 가리킨다. 당시 상점 배치 장부의 grid overlay가 활성 상태였다.

현재 증거가 지지하지 않는 원인:

- C# 컴파일 실패
- 테마 코너 연결 요소 계산의 관리 예외
- D3D12 재선택
- 저장/판매/경제 시스템 오류
- GPU 메모리 부족

기존 D3D11 기준선 전체가 불안정하다고 판단하지 않는다. 같은 프로젝트의 일반 Play Mode와 여러 D3D11 검증은 통과 이력이 있으며, 이번 충돌은 검증기 전용 명시적 `Camera.Render()` 호출에서 발생했다.

## Safe Recovery Plan

1. 동일한 직접 `Camera.Render()` 구현을 다시 실행하지 않는다.
2. 검증기는 일반 Play Mode 프레임에 카메라 구도를 적용한다.
3. `Screen.SetResolution(1920, 1080, false)`와 `ScreenCapture.CaptureScreenshot`을 사용해 다음 프레임 결과를 요청한다.
4. PNG 생성 완료를 기다린 뒤 크기와 파일 존재를 검증하고 카메라 상태를 복원한다.
5. Task 086 기능 검증과 회귀를 D3D11에서 한 번 실행한다.

같은 네이티브 원인이 두 번째로 발생하면 세 번째 Unity 실행을 하지 않고 사람 판단을 요청한다.

## Preserved Boundaries

- D3D12는 계속 미승인이다.
- 그래픽 API, 드라이버, `ProjectSettings`, 패키지, 메인 씬은 변경하지 않는다.
- 크래시 복구는 `PA_ThemeCornerValidator`의 캡처 경로에만 한정한다.

## Follow-up Result — 2026-07-17 13:07 KST

`PA_ThemeCornerValidator`의 직접 `Camera.Render()`는 일반 Play Mode 프레임의
`ScreenCapture.CaptureScreenshot`으로 교체했다. 이후 실제 GameView D3D11 실행은
여러 차례 관리 예외 없이 완료됐고, 다음 기능 계약이 모두 PASS했다.

- Raw 2칸, Processed 6칸/판매 후 4칸, 대각선·빈칸·혼합 분류 제외
- 품절 시 해체, 보충 시 복원, 회수·이동·재배치 반영
- 실제 구매 3건만 판매 기록에 추가되고 Processed 마을 방향 신호로 연결
- v10 격리 저장 후 Raw 2칸 코너 재파생

최종 기능 로그는 `Logs/Codex_Task086_ThemeCorner_CaptureFinal.log`이며
`PA Theme Corner Validation finished successfully.`로 종료됐다. 깨끗하게 판독 가능한
동일 구도 캡처는 `Logs/ThemeCorner/20260717_130433/theme_corner_01_none.png`,
`Logs/ThemeCorner/20260717_130201/theme_corner_02_raw_two.png`,
`Logs/ThemeCorner/20260717_130433/theme_corner_03_processed_four.png`다. ScreenCapture 결과에는
실행별로 이동하는 검은 TMP/Canvas 프레임이 간헐적으로 포함돼 캡처 자동화 안정성은 해결되지 않았다.

기존 회귀 `PA_ShopCustomizationValidator`를 실행하자 그 검증기의 기존
`CaptureGameCamera`가 다시 직접 `Camera.Render()`를 호출했고 같은 네이티브 스택에서
Unity가 두 번째로 충돌했다.

- 로그: `Logs/Codex_Task086_ShopCustomizationRegression_Final.log`
- 크래시 폴더: `C:\Users\sdjsd\AppData\Local\Temp\Unity\Editor\Crashes\Crash_2026-07-17_040714511`
- 호출 위치: `Assets/Editor/PA_ShopCustomizationValidator.cs:388`
- 스택: `GfxDevice::DrawSharedGeometryJobs` → `DrawLineOrTrailMultipleFromNodeQueue` →
  URP `RenderSingleCamera` → `Camera.Render`

따라서 같은 원인의 세 번째 Unity 실행은 금지한다. `PA_ShopCustomizationValidator`와
직접 `Camera.Render()`를 사용하는 관련 검증기의 캡처 전략을 한 번에 감사·교체하기 전에는
Task 086의 남은 ShopCustomization/SaveRoundTrip/FinalDemoRoute 회귀를 재시도하지 않는다.
그래픽 API, 패키지, ProjectSettings, 씬, 프리팹, 저장 스키마는 변경하지 않았다.
