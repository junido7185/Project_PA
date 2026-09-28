# Current state

**2026-09-27 보정 포함 새 Windows 후보 생성 / standalone 일부 확인.** `Candidate-ShopPolish-20260927-194240/Project_PA.exe`에 앞선 상점 표시 보정과 Report-open 중복 제목 숨김을 포함했다. 새 후보에서 실제 마우스로 NEW GAME→출항 교육 STEP1 확인; 이후 키보드 이동 확인 실패로 정착→Report는 standalone 미검증. 같은 자동 입력을 반복하지 않았다. 기존 후보·dirty/index·사용자 save 유지. [후보·증거·사람 검수 순서](../../Logs/VisualQA/ReportCandidate-20260927/DELIVERY_REPORT.md).

**2026-09-27 상점 표시 보정 완료.** 가격 추천/구매율/유불리 제거와 ±1/±10/드래그, 작은 확률 없는 말풍선, 한국어/Jalnan Report, 밤 판매대 조명을 수정했다.16m 카메라·배치·경제/구매/점수 계산 유지. 관련 실제 GameView와 자율 거래 회귀(재고1→0/8G/Report1건8G/첫판매1회) 확인. [전후 화면·검증](../../Logs/VisualQA/ShopPolish-20260927/EVIDENCE.md). 이전 Windows Candidate-20260927-011847은 보존됐으며 이 변경을 포함하지 않는다. standalone/HUMAN-UNVERIFIED 유지.

**2026-09-27 T7 납품 정리 완료 / 전체 standalone MUST PATH 미검증.** 후보와 기존 T2~T6 증거를 보존했다. 빌드 shader 경고185건을40원문/5원인으로 정리했으며 모두 Unity 추론 패키지에서 발생했다. 데모 의존 재질205개와 경고 shader 참조 교집합0, 확인된 영향 결함이 없어 소스·자산·설정 수정/재빌드 없음. 후보/dirty 식별, 소스 대응의 한계, 최신 캡처 전체 경로와 사람 확인 항목은 [납품 보고서](../../Logs/VisualQA/Continuation-20260926/T7/Delivery-20260927/DELIVERY_REPORT.md). Windows 입력 자동화 재시도 없음. 아래 T2~T6 검증 범위와 HUMAN-UNVERIFIED를 유지한다.

**2026-09-27: 승인된 T2~T6 구현·대표 GameView·관련 플레이 검증 완료, T7 Windows 후보 빌드·타이틀 실행 확인, 독립 실행 전체 경로는 창 포커스/입력 환경 차단. 시각 품질은 STRUCTURALLY IMPROVED / HUMAN-UNVERIFIED.** Demo256은 Shop/Base+텐트2개 이후 출입하며 Golden Tier1과 기존 gameplay authority를 유지한다. T2 외관·실내/고객 접근, T3 도구·획득 중복 방지, T4 1080p HUD/Inventory/P Phone, T5 밤 상품·가격/따뜻한 조명, T6 자율 관광객 구매→CLOSE→Report(1건/8G/첫판매1회)를 확인했다. 사용자 정의 부재 보류는 해제됐다. [증거](../../Logs/VisualQA/Continuation-20260926/EVIDENCE.md), [정확한 재개 지점](CODEX_HANDOFF.md).

**직전 기능 기준 2026-09-21: 제품 NEW GAME → Canon v2 Opening Demo 진입 CONNECTED / COMPILE·BUILD REFERENCE PASS / TARGETED PLAY PENDING.** `Prototype_FirstDay`의 기존 NEW GAME 핸들러가 `DepartureTutorialController.SceneName`을 사용해 `PA_DepartureTutorial`을 Single load하며, 타이틀이 정지한 `Time.timeScale`을 먼저 복원한다. Build Settings는 `Prototype_FirstDay` 첫 활성, `PA_DepartureTutorial`·`WorldSandbox` 활성 상태이며 세 씬/메타가 존재한다. 기존 Human 검증으로 DepartureTutorial·가격/구매·Stage 6·동행/출항·Demo256 전환과 이전 오브젝트 비지속을 유지했다. 기존 `Logs/OpeningDemoMustPath/result.txt`는 Departure 이후 MUST PATH PASS 증거이며, 갱신된 실제 제품 진입 validator는 아직 실행하지 않았다. 시각 체감과 연속 인간 입력 완주는 HUMAN-UNVERIFIED이며 broad Final Polish는 시작하지 않았다.

전환 전 승인된 대체 Play 1회는 실제 Demo256 항구·동행 Miner_01/Farmer_01·Supply 부분/중복 지급·도구·지역별 이동 등 84개 기능 검사 PASS, 런타임 오류 0이었다. [75.12초 실제 GameView](../../Logs/FirstDayStudio/Play-20260917-134119/GameView-75s.mp4)는 1920×1080, 약 9.16fps, 무음이며 접근 Pose와 테스트 fixture가 포함된다. PASS 1 시각 게이트는 미완료: 급격한 지형 단차·빈 공간·항구 프레이밍/연결 동선 부채. 이 영상은 이후 A–H 변경의 증거가 아니다. 항해는 임시이며 최종 아트 별도 공급. [재개 지점](CODEX_HANDOFF.md), [개발 로그](../04_DEVELOPMENT_LOG/2026-09.md).

기준: **2026-09-10**, `milestone/gameplay-beta-85`, 구현 checkpoint **`4374b2aa27e73c2c7fec086eac6c4130eb702306`**. 이번 문서 정리는 구현 상태를 바꾸지 않는다. 현재 상태의 단일 권위는 이 파일이며, 상세 기능과 다음 작업은 같은 폴더의 [기능 목록](CAPABILITY_REGISTRY.md), [실제 루프](GAME_LOOP_MAP.md), [작업 큐](INTEGRATION_QUEUE.md)에만 기록한다.

## 지금 실행되는 범위

| 진입점 | 확인된 범위 | 경계 |
|---|---|---|
| `Assets/Scenes/Prototype_FirstDay.unity` | Build Settings의 첫 활성 씬. 기존 타이틀 NEW GAME → `PA_DepartureTutorial` Single load 연결 | Golden Regression Scene 자체는 변경하지 않음. 코드/Build reference PASS, 실제 버튼 targeted Play는 미실행 |
| `Assets/Scenes/PA_DepartureTutorial.unity` | Editor에서 씬을 열고 Play. P0 출항 교육 → P1 후보 3명 중 2명 → P2 배/섬 → P3 거점 1·거처 2 → 저장/복원 | **INTEGRATION-01**에서는 도착 페이드 후 WorldSandbox Demo256으로 전환. 기존 P3/P4 경로와 구분 |
| `Assets/Scenes/WorldSandbox.unity` | `WorldAlphaPlayableController`와 기존 world adapter의 생성 월드, 낮 활동·제작·상점·캠페인 구현 | 출항 도착 브리지로 연결되며 Build Settings 등록. 전체 M85/CONTENT 완주 PASS로 승격하지 않음 |
| `Assets/Scenes/MainGame.unity` | 씬 자산 존재 | Build Settings 비활성. 통합 승인·Gate 확인 전 사용하지 않음 |

WorldSandbox direct Timber/Stone gathering: required gameplay acceptance PASS (2026-09-15), three hits with Axe/Pickaxe, Inventory-first reward and persistent depletion. Optional batchmode screenshots unavailable; human visuals deferred. See [September log](../04_DEVELOPMENT_LOG/2026-09.md).

WorldSandbox direct fishing and Net/Butterfly catching: targeted gameplay PASS (2026-09-16). Fish uses the existing daily reward state; three Meadow butterflies have session-local depletion. Human visuals and full-save reload are not validated for this extension.

Unity `6000.3.2f1` / D3D11. 새 standalone 빌드 검증은 이번 문서 작업에서 하지 않았다.

## 가장 최근의 실제 검증

2026-09-10 [최종 연속 검사](../90_PRESENTATION/Evidence/2026-09-10/GameFeel/FinalRoute-checks.txt): 실제 이동 → 나무 상호작용/열매 3 → 기존 ShopSlot 진열 1 → ShopPriceUI 확정 → PurchaseEvaluator → 판매/진열 제거/금액 1회 → 마우스 CTA → 선택 2/Enter → 페이드/배/섬 → P3 배치/NPC 거처 도착 → SaveManager 저장·잔액 변경·복원 → 낙하 복구/정지. Runtime 오류 0, 필수 참조 검사 통과. Runtime·Editor compile 오류 0.

검증 대상은 **P4 dirty를 제외한 `4374b2a`와 동일한 코드**다. 원래 작업공간 전체가 PASS한 것은 아니다. fresh Play 복원과 이전 실패의 차이는 [9월 이력](../04_DEVELOPMENT_LOG/2026-09.md), [보존 중인 버그 원문](../../AI_WORKFLOW/05_LOGS/BUG_LOG.md)에 있다. 로그는 `Logs/OpeningFeel001/`; `CheckpointValidation`은 이 저장소 안의 검증 사본이므로 별도 작업 대상으로 삼지 않는다.

## 미커밋 상태와 남은 부채

- **P4 보류**: `ProducerNpcController.cs`, `SaveData.cs`, `SaveManager.cs`, `FirstIslandSettlementController.cs`, `DepartureContinuation.prefab`의 P4 변경, 신규 production/worksite 코드·자산·검증 자료가 남아 있다. 문서 정리에서 내용과 위치를 보존했다.
- 저장: checkpoint의 Save v15와 현재 dirty v16을 구분한다. P4의 빈 `firstProduction` 직렬화/검증 충돌로 Load가 거절된다. 같은 상태가 남아 있다는 assertion만으로 복원 성공을 판단하지 않는다. schema나 P4를 이번에 수정하지 않는다.
- 기존 M85 BETA-009/010의 플레이어 방향 137° 복원 및 통합 검증 부채는 출항 경로 PASS로 해소되지 않는다. 예전 CONTENT 선승인 기록도 새로운 실행 승인이나 완료 증거로 해석하지 않는다.
- 사람 확인: 발 접지·회전 리듬, 가까운 카메라의 건물/돛대 가림, P3 UI 점유.
- `.claude/settings.json`, `Assets/_Recovery`, P4 provenance/presentation, dirty `BUG_LOG.md`는 보호 대상이다. 삭제·reset·다른 변경과 일괄 커밋하지 않는다.

## 현재 작업

P4 작업터 연결은 `WorksiteBinding`과 직렬화된 동행자 프로필로 이전했다. Runtime·Editor compile 오류 0, 정적 검사 40건·managed 바인딩 검사 23건·Editor 자산 검사 6건 PASS이며 Unity Play는 0회다. 근거는 `Logs/P4-R3C/`, 상세 결과는 [9월 이력](../04_DEVELOPMENT_LOG/2026-09.md)에 있다. P4-R2 지원 수령 정책의 기존 17건 managed PASS와 P4-R1 복원을 보존했다. 전체 P4 플레이·저장 PASS를 뜻하지 않는다. 임시 생산자 Ensure/Add는 바인딩 내부에만 남으며 P2/P3 bootstrap 이전 때 제거해야 한다. P4-R4 저장 부채와 기존 dirty는 유지하고 추가 착수·commit·push 없이 중단한다. [INTEGRATION_QUEUE](INTEGRATION_QUEUE.md)의 나머지 경계와 기존 staged 상태를 유지한다.

WorldSandbox Hub/Shop direct Hotbar placement: targeted gameplay PASS (2026-09-16). Space enters a near-player preview, WASD moves it, R rotates, Space/click confirms, Esc cancels. Both kits use existing world placement and persistence; human visual alignment/readability remain deferred. See [September log](../04_DEVELOPMENT_LOG/2026-09.md).

WorldSandbox placed B01 shop operation: SHOP-LOOP-01 targeted PASS (2026-09-16), four existing slots, direct stocking/pricing/sign OPEN, automatic NPC rejection and purchase, exact money/log and continued restocking. Human shop polish deferred; see [September log](../04_DEVELOPMENT_LOG/2026-09.md).

INTEGRATION-01 connected demo (2026-09-16): departure presentation -> Demo256 -> four real gathers -> Hub/B01 -> gathered Wood stock/price/OPEN -> automatic NPC purchase -> DEMO_SUCCESS, 90 checks PASS, runtime errors 0. Session-only completion; human visuals and standalone build remain unverified. See [September log](../04_DEVELOPMENT_LOG/2026-09.md).
