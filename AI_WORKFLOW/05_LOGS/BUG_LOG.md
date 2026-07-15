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
