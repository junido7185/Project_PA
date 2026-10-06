# Current state

**2026-10-06 최신:** `demoPolishFinishApproval` D0/D1 PASS, D2 INCOMPLETE/BLOCKED_BUDGET. 승인된 TooltipBinding 적용·compile 성공. 최신 섬 PASS31/오류0, 도입→동행→항해 PASS10/오류0. 1080p/720p 패널 검수에서 툴팁 Space 오안내·대화/안내 겹침·NPC 말풍선 캡처 누락이 남았다. 추가 포함 compile4/4·Play5/5·validator1/1 소진. D3 아이콘 보완 승인 보존, D3~D12 미착수. D5/D7만 Astra XHigh, 나머지 High. [재개 근거](../../Logs/CodexDemoPolish/D2-Resume-20261006-091224/EVIDENCE.md).

**최신 실행 상태 — 2026-10-05 Codex 직접 인수:** 기존 `demoFeedbackFinishApproval`과 P1~P11/Q1~Q6 결과를 보존하며 [최종 실행 계약](../../AI_WORKFLOW/03_TASKS/CODEX_OPENING_DEMO_FINAL_EXECUTION_PROMPT.md)을 적용한다. Unity 소유자는 Codex 한 세션이다. 사용자 후속 지시에 따라 경사로 없이 블록 단차/점프를 유지하고, 바다는 육지보다 낮게 표현하며 수영 진입은 막는다. 낚시는 기존 권위 위의 미니게임 → 로컬 CuteFish 뭍 팔닥임 → 도구 포획으로 확장됐다. 허공/잘못된 대상 도구 사용도 연결됐다. 최신 기능 증거는 loop-state의 takeover.supplementalResults에 연결되며 전체 presentation/human ACCEPTED가 아니다.

**최신 검증/미완료:** 도입부 목표 지속 표시·사과 아이콘·안내/반응 겹침 수정은 Editor targeted PASS10/오류0(compile1/Play2)이며 candidate4 실제 NEWGAME→동행 변경/Enter→항구·정상 저장 종료 증거도 보존했다. 합성 채집 효과음을 기존 AudioManager SFX 풀에 연결해 볼륨0 실제 믹스 출력0을 확인했다(compile2/Play2 PASS7/오류0, Windows 출력 전후 WAV). P9 양손 그립은 compile3/Play3 functional25와 별개로 실제 손/도구 화면 FAIL이며 네 번째 시도하지 않는다. P4 안내문 추가1+1, P6 실제 마우스의 검수 장치 대체를 분리할 QA 패치 추가1+1은 아직 답변 대기다. 가방 제목/닫기/색상은 컴파일됐지만 수정 후 GameView·Q2 수납 극한 검수는 미확인. 지지발 경험도 compile3/Play3 뒤 원본 배율을 유지했고 실제 접지 개선은 미해결이다. 항구/항해/섬 낮 환경음과 밤 전용 음원·fade 음소거·12초 반복 출력은 기존 AudioManager로 연결·검증했다(compile3/Play3, 마지막 targeted PASS6/오류0, 실제 Windows WAV). 전체 섬 표현/자연 NPC·주관적 청취·최신 동일 Windows 후보 자연 완주/fresh Continue/성능은 남아 있다. [Handoff](CODEX_HANDOFF.md).

**데모 제작 실행 기준:** `.agents/skills/pa-demo-factory/SKILL.md`와 기존 `loop-state.json`의 `demoProductionRouting`을 사용한다. 2026-10-03 사용자가 [후속 실행 계약](../../AI_WORKFLOW/03_TASKS/CLAUDE_OPENING_DEMO_EXECUTION_PROMPT.md)의 승인 메시지를 Claude에 직접 보내 선택 승인이 `demoFeedbackFinishApproval`(P1~P11, Unity 담당 Claude, 최종 FINAL_WINDOWS_HUMAN_REVIEW)로 바뀌었다(doctor `ELIGIBLE_PENDING_RUNTIME_AND_FILE_REVIEW`). 이전 `demoCompletionApproval`(D1~D4)은 **D4_GAMEVIEW_REVIEW 정지와 결과를 보존**하며 D4·옛 모션 결과는 ACCEPTED가 아니다. 옛 F1~F6 초안은 대체됐다. 실제 재개는 [Handoff](CODEX_HANDOFF.md).

**2026-10-03 D1~D4 기능 PASS / 화면 STRUCTURALLY IMPROVED / 사람 검수 UNVERIFIED.** Demo256 Play 검수 73/73 PASS, Console 오류 0([결과·캡처](../../Logs/VisualQA/DemoCompletion-20261003-003953/result.txt)). Report 6건/99G/보류3 = SalesLog = 돈 증가 = 재고 감소, 관광객 7명·최대 동시 3·20:00→21:30 예고→22:00 자동 마감·정체 0, 휴대폰 상점 앱(P)으로 전문 분야·영업 시작/마감, Report 카드 E 닫기→휴대폰 기록→Esc→이동 복귀, HUD 시계·돈 일치와 우상단 3.5초 토스트를 확인했다. **휴대폰 기록(Canon §22)은 플레이어가 열 수 없다**: 피드 탭 타일을 보호 파일 `SmartphoneUI.cs`가 숨긴다(필요한 변경은 [Handoff](CODEX_HANDOFF.md)). QA fixture(직행·배치 API·재고 지급·가속 일몰)를 포함하며 NEW GAME 연속 사람 플레이·standalone 검증이 아니다.

**이어하기 검증 부채:** 두 경로의 기존 FAIL은 해소되지 않았다. 다만 `demoCompletionApproval.v16Decision`의 이후 사용자 결정은 **현재 형식 배치 복원만, v16은 지원 불가 안내, 원본 보존**이다. 따라서 과거의 v16 배치 재구성 승인 질문을 반복하거나 변환을 시작하지 않는다. 현재 FirstDay 동적 상점·가판대 저장/재진입 미지원 원인과 배치 누락 근거는 [결정 자료](../../Logs/VisualQA/TitleScreen-20260929/CONTINUE_COMPATIBILITY_DECISION.md)에 남긴다. 안내/현재 형식 복원의 구현 완료를 의미하지 않는다.

**2026-09-29 TASK 01-C 모션 품질 FAIL → 딩컴 기준 샘플 제작 후 중단 / 시각 품질 판정 대기(사용자 확인 전, 수치 PASS 아님).** 맨손 대기→달리기→정지 샘플 클립(`Assets/Art/Animation/PlayerMotionStyle/`, 생성 도구 `PA_PlayerMotionStyleLab`)과 비교 영상까지 만들었다. 제품 컨트롤러·코드 미적용, 추가 튜닝·점프 보류. [샘플 기록](../../Logs/VisualQA/Task01C-Locomotion/StyleRework-20260929/EVIDENCE.md).

**2026-09-28 TASK 01-C 클립 기반 플레이어 로코모션 전환 구현 완료 / 검수 PARTIAL / 체감 HUMAN-UNVERIFIED.** 튜토리얼·Demo256 플레이어가 Quaternius UAL1(CC0) Idle/Walk/Jog/Sprint 블렌드와 Jump/Airborne/Land를 쓰는 `PlayerLocomotion.controller` + `PlayerLocomotionAnimator` 브리지로 전환됐다. PlayerController 이동 권위·수치, 16m 카메라, NPC 절차 애니메이터, 경제·저장은 그대로다. 9/29 마무리로 스폰·워프 직후 무입력 대기도 접지 기준과 일치하고(실측 CC 간격만큼 모델 하강), JumpStart→Airborne은 물리 정점 통과, 단차 낙하는 발이 떨어진 프레임에 Airborne, 접지 프레임에 Land로 전이한다. Jump_Land는 Original 높이 기준이다. 손 도구 방향과 실제 키보드 체감은 미확인이다. [증거](../../Logs/VisualQA/Task01C-Locomotion/EVIDENCE.md), [마무리](../../Logs/VisualQA/Task01C-Locomotion/Closeout-20260929/EVIDENCE.md).

**2026-09-28 TASK 01-B 조사 완료(구현 없음).** 사용자 영상 검수 결과 TASK 01 이동은 여전히 부자연스럽다. 절차 기본 자세가 플레이어를 약 0.22m 띄우는 것도 확인됐다(TASK 01의 "부유 아님"은 정정). 로컬 Humanoid Idle/Walk 클립은 C-01에 정상 적용되지만, Run·Jump 계열 클립은 없다. 다음 구조는 Animator 클립 기반 hybrid 권장, 자산 확보 승인 대기. [조사](../../Logs/VisualQA/Task01B-Locomotion/FINDINGS.md).

**2026-09-28 TASK 01 맨손 이동·애니메이션 개선 완료 / 체감 HUMAN-UNVERIFIED.** 기존 절차 애니메이터에 FirstDay 플레이어 전용 달리기 주기(지지발 고정·골반 접지 보정), 걷기/달리기 구분, 점프 공중·착지 자세, 대기 팔·호흡을 추가했다. 튜토리얼과 Demo256의 같은 플레이어에 적용되며 PlayerController 수치·16m 카메라·NPC·손 장착/도구·경제·저장은 그대로다. 실제 제품 경로 전/후 영상과 수치: [증거](../../Logs/VisualQA/Task01-Locomotion/EVIDENCE.md). 입력은 QA 주입이다. 사용자 보고: 9/27 이후 Windows 후보를 직접 플레이해 엔딩에 도달했다(에이전트 자동 입력 미검증과 별개이며 에이전트가 관찰하지 않음).

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
