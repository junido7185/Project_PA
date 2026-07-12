# VERIFICATION_RULES — 완료 선언 조건

작성: 2026-07-09
원칙: **검증 없이 "완료" 없음. 검증하지 못한 항목은 반드시 "확인 못 함"으로 보고.**

## 1. 완료 선언의 최소 조건

어떤 작업이든 "완료"라고 말하려면 아래를 만족해야 한다:

1. 해당 작업 유형의 검증 항목(§2)을 실제로 실행했고 결과 증거(로그/검증기 출력/스크린샷 경로)가 있다.
2. 실행하지 못한 항목이 "확인 못 함 + 이유 + 사람이 확인하는 방법"으로 보고서에 명시됐다.
3. `git status`로 의도한 파일만 변경됐음을 확인했다.
4. 기록 문서(CHANGELOG_AI + 루트 기록 4종)가 갱신됐다.

## 2. 작업 유형별 검증 항목

### A. 컴파일 확인 (모든 코드 작업의 기본)

- `dotnet build Assembly-CSharp.csproj` → 0 errors. (신규 파일은 `.csproj` 반영 여부 감안)
- Unity Editor가 닫혀 있으면 batchmode 컴파일 가능. **열려 있으면 batchmode 금지** — Editor Console 확인을 사람에게 요청.

### B. 플레이 모드 확인 (게임플레이 변경 시)

- `Automation/LoopEngineering/validator-registry.json`의 해당 검증기 실행.
- 핵심 검증기: `PA_FinalDemoRouteValidator.RunFinalDemoRouteValidation` (Day 1 루트), `PA_DayNightShopLoopValidator` (낮-밤 루프), `PA_LongPlayProgressionValidator` (Day 2~7), `PA_CoreSlicePlayabilityValidator` (코어 슬라이스).
- Editor가 열려 있어 실행 불가면: "검증 보류 — Editor 열림"을 명시하고 Editor 메뉴 실행 방법을 안내.

### B2. 씬 로드 확인 (씬/바인더/런타임 생성 관련 변경 시)

- [ ] `Prototype_FirstDay.unity`가 에러 없이 로드되고 Play 모드 진입.
- [ ] `PA_RuntimeSceneBinder`가 런타임 오브젝트를 정상 바인딩(플레이어/상점/NPC/HUD 생성 카운트 확인 — README 스모크 카운트 참고: player 1, shops 2, shop slots 8, NPCs 8 등).
- [ ] 씬 직렬화를 건드리지 않았음을 확인(런타임 사이드카 패턴 유지). 씬 파일이 실제로 변경됐다면 사람 승인 필요.

### C. 상점 루프 확인 (상점·경제·NPC 관련 변경 시 전부)

아래 체인이 끊기지 않았음을 확인한다:

- [ ] **상점 진열**: 핫바 → `ShopSlot` 진열 성공
- [ ] **가격 설정**: `ShopPriceUI` 열림 → 가격 확정 동작
- [ ] **NPC 구매**: 손님 NPC가 접근·평가 → 구매 또는 거절 + 이유 말풍선 표시
- [ ] **수익 증가**: 판매 시 돈 변화가 HUD에 반영, 누적 매출 갱신
- [ ] **영업 게이트**: 밤에만 구매 가능 규칙이 유지됨 (낮 구매 차단)

### D. 저장/불러오기 확인 (데이터·진행 상태를 건드린 작업 시)

- 자동 기준: `PA_SaveRoundTripValidator.RunSaveRoundTripValidation`을 D3D11 batchmode로 실행한다. 사용자 save 대신 `Logs/SaveRoundTrip/<timestamp>` 격리 경로를 사용한다.
- [ ] F5 저장 → F9 로드 왕복 후 돈/인벤토리/진열/진행 상태 보존
- [ ] 구버전 세이브 호환 (저장 스키마를 확장했다면 마이그레이션 경로 확인)
- [ ] 저장 스키마 변경이 있었다면: **추가 확장(v7→v8 패턴)인지** 재확인 — 아니면 사람 승인 필요

### E. 마을 변화 확인 (핵심 차별점 관련 작업 시)

- [ ] 판매 결과가 마을 변화로 이어지는 **최소 증거** 1건: 카테고리 판매 → `SalesLogManager` 기록 → `VillageChangeSignalController` 신호 또는 다음날 시각 변화(VC-001A 계열) 발생
- [ ] 변화가 플레이어 화면에서 읽히는지 (로그로만 남으면 불충분)

### F. UI/가독성 확인 (UI 변경 시)

- [ ] 1920x1080 Game view 기준 텍스트 잘림/겹침 없음
- [ ] 한국어 폰트 렌더링 정상
- [ ] 새 UI가 기존 HUD/상호작용을 가리지 않음
- ⚠ 최종 가독성 판정은 **사람 몫** — AI는 "사람 확인 필요"로 표시

## 2-B. 과거 BLOCKED 검증기 해소 (2026-07-13 동기화)

2026-06-26 당시 Editor가 열려 있어 보류됐던 2종은 Editor를 닫고 D3D11 batchmode로 실제 실행해 해소됐다.

| 검증기 | 현재 상태 | 최신 증거 |
|---|---|---|
| `PA_FinalDemoRouteValidator.RunFinalDemoRouteValidation` | PASS | `Logs/Fable_V3_FinalDemoRoute.log` — BreadLoaf, paid=30G |
| `PA_LongPlayProgressionValidator.RunLongPlayProgressionValidation` | PASS | `Logs/Fable_VisualPass_LongPlayRegression.log` — money=4633G 기준선 |

D3D12는 여전히 미승인이다. 자동 Unity 실행은 계속 `-force-d3d11`을 사용한다.

## 3. 검증 실패 시

- 같은 검증기가 같은 이유로 **2회 실패 → 중단.** Codex는 세 번째 시도나 독단 수정을 하지 않고 `../05_LOGS/BUG_LOG.md`에 기록한 뒤 **사람 판단을 요청**한다.
- 컴파일 에러 2회 반복 → 동일하게 중단·사람 요청.
- Unity 크래시 아티팩트 발생 → 즉시 중단, 루트 크래시 리포트 규칙 확인, 새 크래시면 `PROJECT_PA_CRASH_REPORT_YYYYMMDD.md`를 **루트에** 생성 (preflight glob 규약).
- 위험 파일 대규모 변경이 필요하면 중단·사람 승인.

## 4. 검증 결과 보고 양식

```
### 검증 결과
- 컴파일: [통과/실패/보류] (증거: ...)
- 검증기: [이름 + 결과] (증거: ...)
- 상점 루프: [해당 항목별 결과]
- 저장/로드: [결과 또는 해당 없음]
- 마을 변화: [결과 또는 해당 없음]
- 확인 못 한 것: [항목 + 이유 + 사람이 확인하는 방법]
```
