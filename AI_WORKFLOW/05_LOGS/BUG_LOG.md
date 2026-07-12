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
