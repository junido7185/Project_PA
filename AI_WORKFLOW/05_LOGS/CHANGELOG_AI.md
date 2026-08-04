# CHANGELOG_AI — AI 작업 변경 이력

AI 에이전트가 수행한 작업을 최신이 아래로 가도록 시간순(append-only)으로 기록한다.
형식: 날짜 / 에이전트 / 작업 / 변경 파일 / 검증 결과 / 비고.

---

## 2026-07-09 — Claude (Fable 5) — AI_WORKFLOW 운영 문서 구조 구축

- 작업: `AI_DOC_CLEANUP_PLAN.md` 기반으로 `AI_WORKFLOW/` 디렉토리 구조와 운영 문서 생성. 루트 `AGENTS.md`를 짧은 입구 안내문으로 재작성 (구버전은 `99_ARCHIVE/old_docs/AGENTS_v1_20260626.md`로 보관).
- 변경 파일: `AI_WORKFLOW/**` 신규 생성 (00_START_HERE 2종, 01_IDENTITY 3종, 02_AGENT_RULES 3종, 03_TASKS README, 04_VERIFICATION 1종, 05_LOGS 3종, 06_HANDOFF 1종, 07_FULL_GAME_ROADMAP 1종, 99_ARCHIVE 2종), 루트 `AGENTS.md` 재작성, 루트 기록 문서 4종 append.
- 코드/씬/에셋 변경: 없음.
- 문서 이동(`git mv`): **보류** — 워킹트리에 미커밋 VC-001A 변경이 있어 필수 제약 4에 따라 이동 없음. 이동 대기 목록은 `../00_START_HERE/DOCS_INDEX.md` §7-B.
- 검증: 문서 작업이므로 Unity 검증 해당 없음. 새 문서의 참조 경로는 실제 파일 기준으로 작성.
- 비고: 개발 목표를 "프로토타입"에서 "완성 게임"으로 문서에 고정 (`../01_IDENTITY/PROJECT_PA_SCOPE.md`).

## 2026-07-09 — Claude (Fable 5) — 작업 큐 + Codex 프롬프트 세트 + 개발 타임라인 구축

- 작업: Codex 자동 개발 루프를 위한 실행 문서 세트 생성. Git은 clean baseline(커밋 `8e79c0c`) 확인.
- 신규 생성:
  - `03_TASKS/TASK_QUEUE.md` — Phase 0~9, **85개 작업**(XS 15 / S 37 / M 26 / L 7, 사람 승인 21), 각 작업에 난이도·예상 실행 횟수·선행관계·수정 금지 파일 포함.
  - `03_TASKS/ACTIVE_TASK.md`, `03_TASKS/DONE_TASKS.md`.
  - `00_START_HERE/PROMPT_LIBRARY.md` + Codex 프롬프트 10종(`CODEX_BOOTSTRAP/REPEAT/VERIFY/BUGFIX/REFACTOR_GUARD/TASK_SPLIT/HANDOFF/DEMO_LOCK/FINAL_POLISH/FULL_GAME_EXPANSION_PROMPT.md`).
  - `07_FULL_GAME_ROADMAP/DEVELOPMENT_TIMELINE.md` — 난이도 분포·3시나리오(저강도/집중/크런치)·Demo Lock 역산표(D-7/14/30/60)·Fable 최종 판단.
- 갱신: `04_VERIFICATION/VERIFICATION_RULES.md`(씬 로드 검증 §B2, BLOCKED 검증기 §2-B, 2회 실패 사람요청 규칙), `06_HANDOFF/HANDOFF_FOR_CODEX.md`(큐/프롬프트/속도정책 반영, git clean 반영), `05_LOGS/DECISION_LOG.md`.
- 코드/씬/에셋 변경: 없음. Docs/01~08 이동·수정 없음. 최신 크래시 리포트 이동 없음.
- 검증: 문서 작업이라 Unity 검증 해당 없음. TASK_QUEUE의 파일 참조는 실제 `Assets/Scripts` 파일명 기준으로 작성.

## 2026-07-10 — Claude (Fable 5) — Codex 투입 전 최종 프리플라이트 + 첫 실행 패키지

- 커밋: 3회차 문서 세트를 커밋 `53fd115`로 저장(사용자 요청). 그 후 프리플라이트 검수 진행.
- 신규 생성:
  - 감사 보고서 3종: `00_START_HERE/AI_WORKFLOW_FINAL_AUDIT.md`, `00_START_HERE/CODEX_PROMPT_AUDIT.md`, `03_TASKS/TASK_QUEUE_REVIEW.md`.
  - 첫 실행 패키지: `00_START_HERE/CODEX_FIRST_RUN_PLAYBOOK.md`(복붙용 첫/두번째 프롬프트 포함), `07_FULL_GAME_ROADMAP/CODEX_FIRST_7_DAYS_PLAN.md`, `00_START_HERE/DECISION_REQUIRED_FOR_USER.md`(사용자 결정 10건 + 임시 기본값).
- 보완(갈아엎기 아님, 최소 수정):
  - `DEVELOPMENT_TIMELINE.md` 기간 표기를 "약 N주 / N~M주"로 통일(오독 방지), 실행 횟수 "약 N회".
  - `TASK_QUEUE.md` Task 041 선행작업 오타 수정(039, 010) + Review Notes 섹션 추가(YES 작업 스킵 규칙, 첫 실행 연결).
  - `CODEX_REPEAT_PROMPT.md`에 "미승인 YES 작업 스킵" 규칙 1줄 추가.
  - `PROMPT_LIBRARY.md`·`DOCS_INDEX.md`에 첫 실행/결정문서 참조 추가.
  - `HANDOFF_FOR_CODEX.md` §0 첫 실행 안내 추가, 우선순위 날짜 갱신.
- 판정: AI_WORKFLOW는 **Codex 투입 가능**. 첫 실행(Bootstrap + Task 001~003)은 지금 바로 안전.
- 코드/씬/에셋 변경: 없음. Docs/01~08 이동·수정 없음. 최신 크래시 리포트 이동 없음.

## 2026-07-10 — Codex — Task 001 프로젝트 구조 인덱스 작성

- 작업: `Assets/Scripts/**/*.cs` 전체를 시스템별로 분류해 `AI_WORKFLOW/03_TASKS/PROJECT_CODE_INDEX.md` 신규 작성.
- 변경 파일: `AI_WORKFLOW/03_TASKS/PROJECT_CODE_INDEX.md`, `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md`, `AI_WORKFLOW/03_TASKS/DONE_TASKS.md`, `AI_WORKFLOW/05_LOGS/CHANGELOG_AI.md`, `AI_WORKFLOW/06_HANDOFF/HANDOFF_FOR_CODEX.md`.
- 코드/씬/에셋 변경: 없음.
- 검증: `Assets/Scripts` 실제 `.cs` 파일 수 100개와 인덱스 분류 항목 100개를 대조. 문서 작업이므로 컴파일/Unity 검증은 실행하지 않음.
- 비고: 다음 작업 후보는 Task 002(현재 데모 플로우 문서화).

## 2026-07-10 — Codex — Task 002 현재 데모 플로우 문서화

- 작업: `README.md`의 Demo Route 10단계와 실제 Day 1 코드 진입점을 대조해 `AI_WORKFLOW/03_TASKS/DEMO_FLOW.md` 신규 작성.
- 변경 파일: `AI_WORKFLOW/03_TASKS/DEMO_FLOW.md`, `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md`, `AI_WORKFLOW/03_TASKS/DONE_TASKS.md`, `AI_WORKFLOW/05_LOGS/CHANGELOG_AI.md`.
- 코드/씬/에셋 변경: 없음.
- 검증: README Demo Route 10단계와 `PlayableDayScenarioController`, `PA_RuntimeSceneBinder`, `ShopSlot`, `ShopPriceUI`, `NpcController`, `PurchaseEvaluator`, `EconomyService`, `SalesLogManager`, `SaveManager`, `SmartphoneUI`, `AuditResultUI` 진입점을 텍스트로 대조. Unity/Play Mode/빌드/테스트는 실행하지 않음.
- 비고: 다음 작업 후보는 Task 003(컴파일 기준선 기록). 작업 전부터 `SubmissionPackages/*.zip` 2개 삭제 상태가 있었으며 이번 커밋에는 포함하지 않는다.

## 2026-07-10 — Codex — Task 003 컴파일 기준선 기록

- 작업: `dotnet build Assembly-CSharp.csproj --nologo`를 실행하고 현재 컴파일 기준선을 `AI_WORKFLOW/03_TASKS/BASELINE_COMPILE.md`에 기록.
- 변경 파일: `AI_WORKFLOW/03_TASKS/BASELINE_COMPILE.md`, `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md`, `AI_WORKFLOW/03_TASKS/DONE_TASKS.md`, `AI_WORKFLOW/05_LOGS/CHANGELOG_AI.md`, `AI_WORKFLOW/06_HANDOFF/HANDOFF_FOR_CODEX.md`.
- 코드/씬/에셋 변경: 없음.
- 검증: `dotnet build` exit code 0, 경고 1개(`CS8785 AttributeBasedFieldGenerator`), 오류 0개. Unity/Play Mode/검증기는 실행하지 않음.
- 비고: Task 001~003 완료 시점이라 `HANDOFF_FOR_CODEX.md`의 우선순위를 갱신. 작업 전부터 `SubmissionPackages/*.zip` 2개 삭제 상태가 있었으며 이번 커밋에는 포함하지 않는다.

## 2026-07-12 — Claude (Fable 5) — Visual Demo Integration Pass

- 작업: 데모 시각 통합. placeholder 큐브(채집 5곳/간판) 코지 드레싱 + 광장 소품 보강(벤치/화단/가로등/궤짝) + HUD 문구 한국어 통일 + Day 요약 본문 잘림 수복. 전부 런타임 사이드카, 씬 파일 무수정.
- 변경 파일: `Assets/Scripts/DemoVisualDressingController.cs`(+meta, 신규) / `PA_RuntimeSceneBinder.cs`(등록 1줄) / `DayNightShopLoopController.cs`·`DaytimeStockPrepPoint.cs`(표시 문자열만) / `UI/PlayableDayScenarioController.cs`(요약 본문 810x418) / `Assets/Editor/PA_DayNightShopLoopValidator.cs`(키워드 1줄) / `Assembly-CSharp.csproj`(include 1줄) / `AI_WORKFLOW/09_FINAL_FABLE_SPRINT/` 5종 신규 / 루트 기록 4종 + 본 문서 append.
- 검증 결과: dotnet build 0 오류. D3D11 batchmode — PA_DayNightShopLoopValidator·PA_FinalDemoRouteValidator(`paid=30G`)·PA_FinalPresentationReviewer(410/418)·PA_GatheringShopReview·PA_CoreSlicePlayabilityValidator·PA_LongPlayProgressionValidator(`money=4633G`) 전부 통과. 로그 `Logs/Fable_VisualPass_*.log`.
- 비고: 기존 BLOCKED 검증기 2종(FinalDemoRoute/LongPlay)이 Editor 닫힘 상태에서 실제 실행·통과됨. PresentationReviewer 1차 실패(410/360)는 이번 변경이 아닌 6/21 Village direction 섹션 추가로 인한 기존 잘림 — 요약 상태 본문 확장으로 수복 후 재통과. 광장 드레싱의 주관적 품질은 사람 확인 필요.

## 2026-07-12 (저녁) — Claude (Fable 5) — Visual Demo Integration Pass v2 (실제 Game View 기준)

- 작업: v1 불합격(실제 플레이 화면 기준) 재작업. 광장 베이스 플레이트로 맨땅 제거, 씬 저장 `Guide_*` 디버그 라벨/스테이징 라벨 기본 숨김, 상단 한 줄+좌측 퀘스트 패널, Item 아이콘 6종 연결, 판매대 카운터/쇼케이스/간판 한국어화. 판정 기준: 동일 카메라 Before/After 스크린샷.
- 변경 파일: `Assets/Editor/PA_DemoViewCapture.cs`(+meta, 신규 캡처 툴) / `DemoVisualDressingController.cs`(실측 지오메트리 전면 개정) / `CoreSlicePresentationMode.cs`(숨김 목록 확장) / `PlayableDayScenarioController.cs`(한 줄 목표+퀘스트 패널) / `DayNightShopLoopController.cs`(패널 좌측 이동, prep 이름 한국어) / `DaytimeStockPrepPoint.cs`(라벨 소형) / `Assets/Resources/Items/Item_{BreadLoaf,Carrot,Fish,Wheat,Ore,IronBar}.asset`(icon 참조만).
- 검증: dotnet build 0 오류. FinalRoute(`paid=30G`)/DayNight(`sellableInventory=10`)/PanelLayout 통과 — `Logs/Fable_VisualPass2_*.log`. Before `Logs/DemoViewShots/before_20260712_164931.png` → After `after5_20260712_223953.png`.
- 비고: Shop 원점(슬롯 부모) 방향 앵커 금지 — `PlazaFrame` 실측(슬롯 행+플레이어) 사용. 물리 지면 y=-0.5 (시각 지면과 상이) — 얇은 바닥 소품은 오프셋 계층 필수. 잔여: 조명 톤/러그 가시성 사람 확인.

## 2026-07-13 — Codex — Visual Demo Integration Pass v3 Final Presentation Lock

- 작업: Fable 세션의 중단 지점(`after_final2` 직전)부터 재개해 실제 Game View 캡처, 검증기 5종, 최종 문서, 냉정한 판정을 완료.
- Final Locked Screenshot: `Logs/DemoViewShots/after_locked_20260713_002356.png`.
- 시각 변경: 15시대 따뜻한 Trilight 조명, 기존 Nature Pack 실모델 소품 베이크/배치, 광장·NPC 2명 스테이징, Clock/Money HUD 폭·투명도 통일. 씬/프리팹/저장/경제·NPC 코어 무변경.
- 검증: 런타임/에디터 dotnet build 오류 0. D3D11 batchmode 5종 전부 Exit 0/PASS — FinalRoute(`paid=30G`), DayNight(`sellableInventory=10`), CustomerPanelLayout, CoreSlice(F10 OFF/ON/OFF), FinalPresentation(요약 410/418, 캡처 5장).
- 판정: **조건부 발표용**. 중앙 상점 primitive 실루엣과 UI 스타일 격차는 사람 확인 및 발표 후 개선 필요.
- 비고: 첫 `after_final2`는 에셋 재임포트 직후 검은 런타임 머티리얼 캡처 아티팩트가 발생했으나, 코드 수정 없는 1회 재실행에서 재현되지 않았다. 사용자 소유 `SubmissionPackages/*.zip` 삭제 2건은 변경·stage하지 않음.

## 2026-07-13 — Codex — Phase 0 현재 구현/TASK_QUEUE 증거 동기화

- 작업: 커밋 `9898f6a`의 코드·씬/프리팹 목록·SO 데이터·Git 이력·검증 로그를 Task 001~085 완료 조건과 대조했다.
- 결과: DONE 21 / PARTIAL 26 / TODO 12 / BLOCKED 6 / DECISION_REQUIRED 20.
- 신규: `CURRENT_COMPLETION_MATRIX.md`, `NEXT_COMPLETION_SPRINT.md`.
- 동기화: `TASK_QUEUE.md`, `DONE_TASKS.md`, `VERIFICATION_RULES.md`, `BUG_LOG.md`. 2026-06-26 보류 검증기 2종을 실제 PASS 증거에 따라 RESOLVED로 변경했다.
- 다음 스프린트: Task 007 → Task 011 → Task 018. 가장 큰 병목은 저장 v8 코드의 실제 저장소 왕복 증거 부재.
- 코드/씬/프리팹/SO 변경 없음. 사용자 ZIP 삭제 2건은 그대로 제외.

## 2026-07-13 — Codex — Task 007 저장 스키마 현황 확정

- `SaveData`, `SaveManager`, `ISaveRepository`, `LocalJsonSaveRepository`를 끝까지 읽고 v8 최상위 필드·DTO·v0→v8 마이그레이션·복원 순서를 `SAVE_SCHEMA.md`에 기록했다.
- 판매 이력·마을 트렌드·VC-001A 활성 상태가 현재 저장되지 않음을 명시했다.
- 저장 코드는 수정하지 않았다. 실제 저장소 왕복은 Task 011로 남겼다.

## 2026-07-13 — Codex — Task 011 격리 저장소 왕복 검증

- 신규 `PA_SaveRoundTripValidator`: 사용자 save 대신 `Logs/SaveRoundTrip/<timestamp>` customRoot를 실제 SaveManager에 주입한다.
- 실제 v8 JSON 왕복 PASS: 돈 1234, 누적매출 5678, 플레이어 위치, Day 2 09:30, 인벤토리 4, 핫바 3, ShopSlot 2개@77G, Day Prep activity, Day 1 이름/단계 복원.
- SaveManager/SaveData/씬/프리팹/SO 무변경. validator registry에 등록.
- 회귀: 런타임/에디터 컴파일 오류 0, FinalRoute `paid=30G`, DayNight `sellableInventory=10` PASS.

## 2026-07-13 — Codex — Task 018 가격 패널 진열 수량 표시

- `ShopPriceUI`의 기존 아이템 이름 줄에 실제 `ShopSlot.currentItem.count`를 사용해 `BreadLoaf · 재고 1개` 형식으로 표시했다.
- 별도 재고 모델·새 패널·판매 수학 변경 없음. FinalPresentation에 수량 assertion을 추가했다.
- Before: `Logs/FinalPresentation/20260713_002627/02_shop_price_ui.png`; After: `Logs/FinalPresentation/20260713_010817/02_shop_price_ui.png`.
- 컴파일 오류 0. FinalPresentation/FinalRoute/DayNight/PanelLayout 모두 PASS.

- 스프린트 종료 동기화: Task 011의 실제 ShopSlot 왕복 증거로 Task 056도 DONE 처리했다. 신규 저장 코드나 스키마 변경은 없다.

## 2026-07-13 — Claude (Fable 5) — Task 057 마을 변화 저장(v9) + Task 019 품절 표시

- 작업 1 (Task 057, `71fa710`): Save v8→v9 추가 확장 — `VillageCultureVisualController`의 대기(오늘 판매→내일 변화)/활성(변화 표시 중) 상태 6필드를 저장·복원. 사용자 세션 지시(우선순위 2 "판매 결과가 다음날 마을 변화로 지속되지 않는 문제" + 예시 커밋 문구)로 스키마 승인. `SaveManager` v9 마이그레이션 + Write/Restore 훅(DayNightShopLoop 패턴). `PA_SaveRoundTripValidator`를 v9로 확장해 대기→다음날 활성→활성 2차 왕복까지 증명. VC-001A 검증기의 구식 "저장 필드 없음" 단언을 v9 필드 존재 확인으로 갱신.
- 작업 2 (Task 019, `f6cbbe1`): `ShopSlot` 표시 전용 품절 상태 — NPC 구매로 소진된 슬롯에 "품절 · 보충하세요" 월드 라벨과 "(오늘 품절)" 프롬프트, 재진열·다음날(OnNewDay) 자동 해제. 검증 중 발견한 실제 버그 수복: `ClearDisplay`가 같은 프레임 이중 갱신 시 지연 파괴 대기 루트에 걸려 새 루트를 못 지우던 문제 → 전체 순회 제거로 교정. 신규 `PA_ShopSoldOutValidator`(프레임 분리 4스텝).
- 검증: dotnet build 런타임/에디터 0 오류. SaveRoundTrip(v9)/VillageCulture/ShopSoldOut/FinalRoute(`paid=30G`)/DayNight(`sellableInventory=10`)/CoreSlice 전부 PASS — `Logs/Fable_T057_*.log`, `Logs/Fable_T019_*.log`.
- 비고: 품절 상태는 런타임 전용(저장 안 함, 의도). 트렌드 점수(Task 048)·SalesLog(Task 055) 저장은 별도 승인 대기. `SubmissionPackages/*.zip` 삭제 2건은 커밋 제외 유지.

## 2026-07-13 — Claude (Fable 5) — 접지/충돌/NPC 정비 + 들어갈 수 있는 상점 기초

- 작업 1 (`f3ef51a`): `PA_PhysicsAudit` 실측 기반 씬 수정 — ① 길 비주얼(Road_NS/EW/Pavement) y=-0.47/-0.46로 정렬해 0.48m 파묻힘 해소 ② PlayerModel_C01 localY +0.17로 발=캡슐 바닥 정렬 ③ 건물 BoxCollider 8개를 메시 로컬 bounds 기반으로 축소(최악 TradePort 10m→3.6m, "절대 키우지 않음" 가드) ④ NPC stoppingDistance 0.2→0.75 ⑤ NavMesh 리베이크. 씬 백업: `_Backups/Prototype_FirstDay_before_vslice_fix_20260713.unity`. 교훈: 회전 오브젝트는 월드 AABB→로컬 재변환이 이중 팽창함 / 상대 델타 이동은 재실행에 비멱등 — 절대 좌표로.
- 작업 2 (`f3ef51a`+`3bf30c9`): 들어갈 수 있는 잡화점 기초 — `BuildingEntrance` Y+100 관례 재사용. B10_Cottage_01 앞 "잡화점" 문 → PA_StoreInterior(12x9 방, 웜 라이트 2, 문라이터식 2x3 ShopSlot 그리드, 출구 문). 실내 슬롯은 SaveManager 계층 키로 자동 저장 호환, Shop 미등록으로 기존 앵커/NPC 로직 무영향. `PA_EnterableShopValidator` PASS: 진입(y100)→진열→가격UI→퇴장.
- 검증: dotnet build 0 오류. 수정 후 재감사 flagged=0. FinalRoute/DayNight/CoreSlice/SaveRoundTrip PASS, NavMesh agents 8/8 실패 0. Before/After: `Logs/DemoViewShots/before_vslice_20260713_111149.png` → `after_vslice_20260713_112849.png`.
- 잔여: 보행 애니메이션 질감(사람 에디터 판정), NPC look-at, 실내 실모델화, NPC 실내 진입(S3).

## 2026-07-13 (오후) — Claude (Fable 5) — 상점 진화 S2+S3: 실내 잡화점에 손님이 온다

- S2 (`PA_ShopLocator.cs` 신규): 실내 Shop 등록으로 `FindFirstObjectByType<Shop>` 앵커(간판/드레싱/마을변화 5곳)가 Y+100 실내를 집는 오염을 차단 — 지상(y<50) 최근접 상점 로케이터로 교체. 실내 `PA_StoreInterior`에 Shop 컴포넌트 등록 + NavMesh 리베이크(실내 아일랜드) — `PA_VerticalSliceFixer.RunRegisterInteriorShop`.
- S3 (`InteriorCustomerController.cs` 신규 사이드카 + 바인더 1줄): 영업 중(밤 개점, Day1 튜토리얼 제외)이고 실내 진열이 있으면 지상 Idle 주민을 실내로 워프 초대 → `NpcController.TryBeginShoppingVisitAt/RetargetShop`(추가 훅 2종, FSM 무변경)으로 기존 둘러보기/구매 수행 → 종료 시 원위치·원상점 복원. `_activeShop` 스테일 방지를 위해 복귀는 반드시 RetargetShop 경유.
- 검증: `PA_InteriorCustomerValidator` PASS — 초대 입장(y=100)→구매(500→515G)→퇴장 복귀. 회귀 5종(FinalRoute/DayNight/CoreSlice/EnterableShop/CustomerArrival) PASS. 컴파일 0 오류.
- 시행착오: 에이전트가 pathPending 영구 대기 — 원인은 Day1 온보딩 모달의 Time.timeScale=0. Play Mode 검증기는 `RestoreSavedSession` 으로 온보딩을 먼저 해제할 것 (공통 규칙).
- 캡처: `Logs/DemoViewShots/inside_shop_20260713_163425.png` — 실내 그리드 진열 4종 + 손님 쇼핑 장면 (`PA_SHOT_INSIDE=1` 분기 추가).
- 잔여: 실내 진열이 녹색 폴백 큐브(모델 없는 아이템) — S5에서 아이콘/실모델화. 동시 손님 1명 제한.

## 2026-07-13 (저녁) — Claude (Fable 5) — S5 시각 완성 3종 (검증 생략, 컴파일만)

- ① `ShopSlot` 진열 폴백 개선: 모델 없는 아이템이 원색 큐브 대신 **낮은 받침 + 실제 아이템 아이콘 빌보드**로 표시 (광장/실내 전 슬롯 공통, 표시 전용, 신규 nested `DisplayIconBillboard`).
- ② 실내 인테리어 드레싱 (`DemoVisualDressingController.DressStoreInterior`): 러그, 북벽 선반 2단+잡화, 계산대(카운터+금전함), 구석 화분 2(실모델 Prop_Flowers/BushBerries), 개구리의자, 벽 트림. 렌더러 전용.
- ③ `InteriorCustomerController.maxConcurrentVisitors` 1→2 (실내 동시 손님 2명).
- 검증: 사용자 지시로 생략. dotnet build 0 오류만 확인. **다음 세션 필수**: InteriorCustomer/FinalRoute/DayNight 회귀 + 실내 캡처(PA_SHOT_INSIDE=1)로 시각 확인.

## 2026-07-14 — Claude (Fable 5) — 미검증 S5 확인 + 정각 스케줄이 쇼핑을 초기화하던 버그 수복

- S5 회귀 1차 실행에서 `PA_InteriorCustomerValidator` FAIL — 방문객 3명 전원이 슬롯 평가 전에 로그 없이 Idle로 튕김. 원인 추적: `NpcScheduleController.OnHourTick → EvaluateAndApply → ApplyPhase`가 **매 정각 무조건 `Pause()`부터 호출**하고, `NpcController.Pause()`는 진행 중 쇼핑 FSM을 조용히 Idle로 초기화한다(Docs/03 §2.1 일과 게이팅의 부작용). 어제 PASS는 단일 방문객이 20:00 Rest 귀가(전문직 Shopping 18~20시) 전 30초 안에 구매를 끝낸 레이스 승리였다.
- 수복 4건: ① `NpcScheduleController` — resolved 페이즈가 직전과 같으면 재적용 생략(같은 페이즈 내 정각 틱이 쇼핑을 더 이상 끊지 않음; Shopping 진입 TryForceShop도 원 의도대로 진입 시 1회) ② `NpcController.IsSchedulePaused` 읽기 전용 공개(추가만) ③ `InteriorCustomerController` — 휴식/수면 NPC를 워프 전에 스킵(매초 워프 스팸 제거), 초대 실패 백오프 3초, 페이즈 변경으로 중단된 무구매 방문 1회 재개(moneyAtStart 비교) ④ 검증기/캡처 시각 19.5→18.25(전문직 쇼핑 창 초입, 레이스 제거).
- 검증: `PA_InteriorCustomerValidator` PASS — **동시 방문객 2명(Chef p=0.97, Tailor p=0.94) 모두 구매**, 500→530G, 퇴장 복귀. FinalRoute(`paid=30G`)/DayNight(`sellableInventory=10`) PASS. 실내 캡처 `Logs/DemoViewShots/s5_inside_20260714_103016.png` — 아이콘 받침 진열(Wheat/Carrot/Ore 라벨)·러그·선반 잡화·화분·벽 트림·실내 손님 확인. 컴파일 0 오류.
- 비고: 상점 영업(18~23시) vs NPC 일과(주민 19시·전문직 20시 전원 Rest) 겹침이 18~20시 2시간뿐 — 밤 손님 창이 좁은 것은 설계 이슈로 남김(스케줄 조정은 사람 승인 필요).

## 2026-07-15 — Codex — 상점 진화 S4 Tier 1 실내 해금 연결

- `ShopEvolutionController` 신규: 기존 `TierService.CurrentTier`를 원본으로 Tier 0 잠금 간판/문 게이트와 Tier 1 OPEN 간판·문 조명·비차단 해금 패널을 연결했다. 저장 스키마 추가 없이 로드된 Tier에서 상태를 재구성한다.
- `BuildingEntrance`에 기본값 0인 선택적 Tier 가드를 추가했다. 다른 문은 기존 동작을 유지하고 외부 잡화점 문만 런타임으로 Tier 1 요구를 받는다.
- `PA_EnterableShopValidator`를 S4 경로로 확장: Tier 0 잠금 → Tier 1 해금 → 실내 입장 → 진열 → 가격 UI → 퇴장 PASS (`Logs/Codex_S4_EnterableShop.log`).
- 런타임/에디터 `dotnet build` 오류 0. 기존 CS8785/CS0414 경고만 존재. 메인 씬·프리팹·저장 v9·경제·구매·NPC FSM 무변경.
- `Docs/Codex/` 완성 지도 4종을 추가했다. 전체 회귀, 3일 사람 플레이, 1920x1080 해금 패널 가독성은 최종 통합 검증으로 남겼다.

## 2026-07-15 — Codex — Task 034 일일 구매/거절 통계 연결

- `SalesLogManager`에 일차별 구매 완료/거절 평가 집계와 구매율, 정산 요약, 다음 날 조언 API를 추가했다. 구매 수는 성공한 `RecordSale`에서만 증가한다.
- 기존 `CustomerDemandInsightController` 관찰 경로에서 거절만 기록해 `PurchaseEvaluator` 확률과 NPC 구매 FSM은 그대로 유지했다.
- `DayNightShopLoopController` 정산 HUD와 다음 날 안내, `PlayableDayScenarioController` Day 1 결산을 같은 통계에 연결했다.
- 검증: 런타임/에디터 dotnet build 오류 0. D3D11 `PA_CustomerDemandInsightValidator` PASS — 구매 1/거절 1/구매율 50%, 정산 표시, 다음 날 가격 조언 (`Logs/Codex_Task034_DailyDecisionStats.log`).
- 비고: 메인 씬·프리팹·저장 v9·경제/재고·NPC FSM 무변경. 통계 저장은 Task 055 승인 범위로 남겼다. 전체 회귀/실기기 가독성은 확인하지 않았다.

## 2026-07-15 — Codex — Task 068 부분 완료: Day 1→3 플레이어 날짜 전환

- 감사 결과: `PA_LongPlayProgressionValidator`는 `ForceSet`으로 Day 2~7을 건너뛰어 실제 입력 경로를 증명하지 않았고, Day 1 결산 후/Day 2+ 정산에 하루 마감 입력이 없었다.
- `GameClock.AdvanceToNextDayMorning` 추가: 저장 복원용 `ForceSet`과 분리하고 `OnNewDay`·결과 아침 시각 이벤트를 발화해 생산자 납품, 채집 리셋, NPC 일과, 마을 변화 구독을 유지한다.
- Day 1 결산 버튼은 Day 2 06:00과 보이는 반복 운영 목표를 시작한다. Day 2+ 정산은 기존 `ShopOpenSign`을 하루 마감/다음 날 시작 상호작용으로 재사용한다.
- 검증: dotnet 런타임/에디터 오류 0. D3D11 FinalDemoRoute PASS(첫 판매 30G, Day 1→2), DayNightShopLoop PASS(Day 2 정산 간판→Day 3 06:00, 채집 재활성).
- 로그: `Logs/Codex_Task068_FinalRoute_Day2Transition.log`, `Logs/Codex_Task068_DayNight_Day3Transition.log`.
- 비고: 씬/프리팹/저장 v9/경제/구매/NPC FSM/시간대 밸런스 무변경. Task 041·사람 3일 연속 플레이·저장 재실행은 미확인이라 Task 068은 PARTIAL 유지.

## 2026-07-15 — Codex — Task 039 실제 낚시 상호작용

- 기존 `shore-forage`의 일일 수집·저장 상태를 유지하면서 자식 `FishingSpot`이 `IInteractable` 입력, 캐스팅 대기, Fish 지급을 담당하도록 연결했다.
- 해변 채집 상자 외형을 물빛 표식·낚싯대·찌·바구니로 구분하고 메인 씬/프리팹은 변경하지 않았다.
- 첫 D3D11 실행의 Unity 특수 null/`??` 실패를 BUG_LOG에 기록한 뒤 다음 연속 작업에서 명시적 null 검사로 해결했다.
- 검증: dotnet 오류 0(기준 CS8785 경고 1), D3D11 GatheringShopGate 낚시→Fish 2개→일일 제한/리셋→진열·가격/저장 PASS, FinalDemoRoute 30G PASS.
- 로그: `Logs/Codex_Task039_Fishing.log`, `Logs/Codex_Task039_FinalRouteRegression.log`.

## 2026-07-15 — Codex — Task 041 실제 어획 Fish의 밤 판매 왕복

- `PA_GatheringShopGateValidator`가 새 Fish를 핫바에 주입하던 우회를 제거하고, 실제 낚시로 얻은 Fish 2개 중 1개가 `ShopSlot` 진열로 차감되는지 확인하도록 바꿨다.
- 같은 슬롯에서 가격 확정과 밤 개점을 거쳐 Fisher_01 구매 18G, 잔액 500→518G, 누적매출, Raw `SalesLog`, 일일 구매 통계까지 단일 경로로 검증한다.
- 런타임 시스템은 수정하지 않았다. `ShopSlot`, `EconomyService`, `NpcController`, 씬, 프리팹, 저장 v9는 그대로다.
- 검증: 런타임 dotnet 경고/오류 0, 에디터 오류 0(기준 경고 2). D3D11 FishingSaleRoundTrip PASS, FinalDemoRoute 30G PASS.
- 로그: `Logs/Codex_Task041_FishingSaleRoundTrip.log`, `Logs/Codex_Task041_FinalRouteRegression.log`.

## 2026-07-15 — Codex — Task 042 낚시 스모크 체크리스트

- `AI_WORKFLOW/04_VERIFICATION/SMOKE_CHECKLIST.md`를 신설해 자동 낚시→판매 18G PASS, Day 1 회귀, 사람 Play Mode 체감 항목을 분리했다.
- 자동 항목은 실제 로그 마커만 체크했고 이동·1.25초 대기·NPC 접근/평가·다음 날 재낚시는 미확인 상태로 남겼다.
- `Docs/IslandLife/GATHERING_AND_SHOP_GATE.md`를 현재 FishingSpot/18G 판매 경로와 동기화했다.
- 코드·씬·프리팹·저장·경제/NPC 시스템 변경 없음. 직전 D3D11 로그를 증거로 재사용했다.

## 2026-07-15 — Codex — Task 043 광질/농사 확장 경계 설계

- `DESIGN_MINING_FARMING.md`를 신설해 기존 Crop/Farmland, PlayerInteraction, Item/Recipe, Rock/Crop 프리팹을 실제 파일 기준으로 대조했다.
- 다음 단일 구현은 `quarry-mining` 광질로 확정했다: 낮 Ore 2개 획득 → 1개 진열 → 15G 판매 → 돈/매출/Raw 기록/일일 구매 통계.
- 농사는 `Item_15_Seed.cropPrefab` null, 심기/수확 입력 부재, 3초 실시간 성장, 작물 상태 미저장, 프리팹 아이템 GUID 단절을 먼저 복구해야 함을 기록했다.
- 코드·씬·프리팹·에셋·저장 스키마는 변경하지 않았다. 검증은 정적 경로/API/GUID 대조와 문서 자체검토이며 Unity는 실행하지 않았다.

## 2026-07-15 — Codex — quarry-mining 실제 채굴→15G 판매 왕복

- `MiningSpot`과 `quarry-mining` 런타임 지점을 추가해 공용 곡괭이 타격 후 Ore 2개를 지급하고 같은 날 반복을 차단했다.
- 실제 채굴 Ore 1개를 `ShopSlot`에 옮겨 15G로 판매하고 돈·누적매출·Raw `SalesLog`·일일 구매 통계·MoneyHUD를 단일 경로로 검증했다.
- v9 격리 저장/로드와 Day 3 재활성화를 확인했다. 메인 씬·프리팹·저장 스키마·Shop/Economy/Purchase/NPC 코어는 변경하지 않았다.
- 검증: 런타임/에디터 빌드 경고 0, 오류 0. D3D11 MiningShopLoop와 FinalDemoRoute 종료 코드 0/PASS.
- 로그: `Logs/Codex_MiningShopLoop_Final.log`, `Logs/Codex_Mining_FinalRouteRegression.log`.
- 비고: 광산 primitive 드레싱은 임시 기능 표식이며 최종 비주얼 완료가 아니다.

## 2026-07-15 — Codex — 비주얼 툴체인 감사·Unity MCP·B01 상점 최종화

- Unity 6000.3.2f1/URP 17.3.0, manifest/lock, 공식 패키지, Cinemachine/Rigging 부재, AI Navigation/Timeline, Blender 부재, Codex MCP, 에셋 라이선스와 캡처 방식을 감사했다.
- 설치 전 승인된 체크포인트 `64860ff` 뒤 `com.coplaydev.unity-mcp` 9.7.0을 안정 태그/커밋에 고정하고 uv 0.11.28 loopback 서버와 프로젝트 `.codex/config.toml`로 연결했다. Editor/Runtime 컴파일, WebSocket, Project_PA/30 tools 등록을 확인했다.
- MCP로 메인 씬, B01 프리팹 스테이지, Play Mode와 Game View를 직접 검사했다. 중앙 상점의 원시 박스 조합을 기존 B01 Visual로 교체하고 겹친 안내 오브젝트의 Renderer/Collider만 런타임에서 비활성화했다.
- 메인 씬·프리팹·원본 에셋·Shop/ShopSlot/경제/저장 코어는 변경하지 않았다. Tripo 추정 캐릭터/건물은 유지·Unity 설정·재구성·사용자 확인으로 1차 분류했다.
- 문서: `Docs/Codex/VISUAL_TOOLCHAIN.md`, `ASSET_AND_TOOL_PROVENANCE.md`, `ART_DIRECTION.md`, `TRIPO_ASSET_AUDIT.md`.
- 알려진 도구 실패: 장문 MCP `execute_code`의 Windows 길이 한계는 `BUG_LOG.md`에 기록하고 구조화 도구로 우회했다.
- 검증: 동일 구도 Before `shot_20260715_164534.png` → After `shot_20260715_171613.png` 시각 개선. dotnet 런타임/Editor 오류 0, Unity Console 오류 0, D3D11 FinalDemoRoute `stocked=BreadLoaf, paid=30G` PASS (`Logs/Codex_VisualToolchain_FinalRouteRegression.log`).

## 2026-07-16 — Codex — 상점 실내 그리드 커스터마이징 P1/P2

- 기존 `GridService`의 2m 세계 셀 API를 보존하면서 zone/owner/다중 footprint/clearance/protected cell/BFS 통로 점유를 추가했다. 병렬 그리드는 만들지 않았다.
- `ShopCustomizationController`가 `PA_StoreInterior`를 5×4 `shop.interior` zone으로 등록하고 월드 배치 장부, screen UI, 실제 프리팹 preview, 90도 회전, 이동, 안전 회수, 첫 B05 설계도 지급을 제공한다.
- 기존 6개 `ShopSlot` 객체를 그대로 이동한다. hierarchy와 `Shop` 등록이 유지되어 NPC 목적지·판매·ShopSlot 저장 키가 이동을 따라간다. 기존 슬롯에는 carving `NavMeshObstacle`을 보강했다.
- 실제 B05~B08 청사진/BuildingData/프리팹에서 footprint를 파생한다. B05는 2×2로 배치했고 B09는 4×3 이상이라 현 실내 카탈로그에서 제외했다.
- 저장을 v10으로 확장해 zone/definition/instance/cell/rotation/fixed/recovered/function/storage 상태를 기록한다. v9는 빈 배치 목록으로 기본 실내를 채택한다.
- D3D11 전용 검증에서 보호 입구·겹침 거부, 선반 2개 회수, B05 2×2 배치, 선반 `(4,0)`/270° 이동, 이동 후 NPC 구매 61G, v10 재로드 후 상품 2개/73G, Workbench 기능·회수를 모두 통과했다.
- batch ScreenCapture 미생성은 `BUG_LOG`에 기록한 뒤 RenderTexture 동기 PNG로 해결했다. 동일 구도 캡처를 직접 보고 벽 가림과 UI 상태 문구 여백을 보정했다.
- 문서: `Docs/Codex/PLACEMENT_SYSTEM_ARCHITECTURE.md`, `CUSTOMIZATION_ROADMAP.md`, `PLACEABLE_ASSET_GUIDE.md`, 갱신된 `SAVE_SCHEMA.md`.
- 증거: `Logs/ShopCustomizationValidator_FinalUI.log`, `Logs/ShopCustomization/20260716_103307/savegame.json`, `shop_customization_game_camera.png`.
- 회귀: 기존 `PA_SaveRoundTripValidator`를 v10 기대값으로 동기화해 돈/인벤토리/ShopSlot/마을 변화 왕복 PASS, `PA_FinalDemoRouteValidator`도 BreadLoaf 30G 판매 PASS. 로그 `Logs/ShopCustomization_SaveRoundTripRegression.log`, `Logs/ShopCustomization_FinalRouteRegression.log`.

## 2026-07-16 — Codex — 야외 그리드 P3와 B09 생활형 창고 최종화

- `OutdoorPlacementController`를 추가해 기존 2m `GridService`에 47×47 `village.outdoor` zone을 등록했다. 남북/동서 도로, 상점 광장, 실외 입구, Player/Hiring spawn 212셀은 배치로 막을 수 없다.
- `BuildManager` 신규 건설을 owner/다중 footprint/전면 clearance에 연결했다. 기존 R/클릭은 유지하고 `[M]` 가까운 건물 이동, `[X]` 빈 추가 건물 안전 회수와 플레이어용 하단 안내를 추가했다. 기존 1셀 API는 fallback으로 남겼다.
- B09를 내부 입장 건물이 아닌 문 앞에서 사용하는 24칸 외부 공동 창고로 확정했다. 현재 목재 실루엣·큰 이중문·손잡이를 유지하고 실제 콜라이더 9셀, 90° 정렬, carving, StorageBox 상호작용을 연결했다.
- 기본 B09는 마지막 저장 공간이라 이동만 가능하다. 추가 B09는 물품이 비어 있고 설계도를 받을 수 있을 때만 회수한다. 메인 맵 B09가 있으면 레거시 `[WorldBuildings]` 중복 B09를 런타임 비활성화한다.
- 저장 버전은 v10을 유지했다. 기존 placeables sidecar에 야외 zone/cell/rotation/instance와 StorageBox 물품 수량·품질·가격을 함께 기록하고, 건물 생성은 기존 `BuildingRegistry`를 재사용한다.
- 메인 씬, B09 원본 FBX/프리팹, 외부 패키지, 경제/구매/NPC 코어는 변경하지 않았다.
- D3D11 전용 검증 PASS: 보호 셀 212, B09 9셀, 정적 `(15,31)/270°`, 추가 `(31,28)/90°`, 물품 2+1 저장/복원, 점유 창고 회수 거부, 빈 창고 회수 성공.
- 캡처: `Logs/OutdoorPlacement/20260716_111214/b09_outdoor_baseline.png`, `b09_outdoor_final.png`. 최종 화면에서 문 앞 플레이어와 Space/M/회수 제한 안내를 확인했다. 같은 화면에서 B10 Cottage의 떠 있는 문/벽 조각을 다음 비주얼 결함으로 식별했다.
- 회귀 PASS: SaveRoundTrip v10, ShopCustomization B05/61G, FinalDemoRoute BreadLoaf 30G. 로그 `Logs/OutdoorPlacement_*Regression.log`.

## 2026-07-16 — Codex — P4 ShopSlot 고객 접근 셀·예약·NavMesh 도달성

- 기존 `PlaceableDefinition.interaction` 셀을 `ShopCustomizationController`의 읽기 전용 API로 노출해 이동·회전된 진열대의 실제 앞면을 고객 목적지로 사용했다.
- `ShopCustomerApproachController`를 추가해 NPC owner별 슬롯 예약, 논리적으로 막힌 셀 제외, `NavMesh.SamplePosition` 및 `CalculatePath=PathComplete` 검증을 이동 전에 수행한다.
- `NpcController`는 예약점으로 이동해 진열대를 바라본 뒤 기존 `ShopSlot.TryClaim`/`PurchaseEvaluator`/경제 흐름을 그대로 실행한다. 진열대가 이동하면 낡은 예약을 버리고 다시 선택한다.
- 예약은 런타임 이동 상태이며 저장하지 않는다. 메인 씬·프리팹·NavMesh bake·저장 v10·ShopSlot·PurchaseEvaluator·경제 수학은 변경하지 않았다.
- 첫 Unity import의 검증기 definite-assignment 오류 1건은 명시 초기화로 해결했고 재시도 컴파일 PASS. `dotnet build` 오류 0, 기존 Unity 소스 생성기 CS8785 경고 1.
- D3D11 PASS: 이동 선반 `(4,0)/r3`→앞셀 `(3,0)`, 완전 경로, 동일 슬롯 2인 예약 거부와 별도 슬롯 동시 예약, 실내 방문 15G 구매·퇴장, CustomerArrival cap 2, FinalDemoRoute 30G.
- 증거: `Logs/P4_ShopCustomizationValidator.log`, `P4_InteriorCustomerValidator.log`, `P4_CustomerArrivalRegression.log`, `P4_FinalDemoRouteRegression.log`, `Logs/DemoViewShots/p4_shop_approach_final_20260716_114141.png`.

## 2026-07-16 — Codex — B10 Cottage 원본 비파괴 정면·출입문·간판 최종화

- Unity Editor API로 B10 FBX/래퍼/씬 4인스턴스/콜라이더와 4면을 감사했다. 원본은 지면 `minY=0`, 정상 bounds였고 실제 문 정면은 로컬 `-X`였다.
- 맵이 로컬 `-Z`를 광장 쪽으로 향하게 한 상태에서 별도 원시 `PA_StoreDoor_Out`/간판이 `local z=-3.819`에 놓여, 문·벽 조각이 본체 밖에 떠 보였음을 확정했다.
- `CottageVisualFinalizationController`가 B10 3채의 실제 정면을 광장으로 맞추고 레거시 Static 중복을 비활성화한다. 모델 문/차양/계단과 기존 Collider/NavMeshObstacle을 그대로 쓴다.
- 원시 외부 문은 Renderer만 숨겼다. `BuildingEntrance`, Collider, Tier 잠금, 내부/외부 spawn과 `ShopEvolutionController` 탐색 계약은 유지했다.
- Unity Editor API로 `B10_Cottage_ShopSign_Final` 메시와 Resource 프리팹을 생성했다. 기존 Project PA 목재/크림 재질을 재사용했으며 외부 에셋/Blender/패키지는 추가하지 않았다.
- 시각 루프에서 간판의 비균일 스케일·빌보드 면 교차·TMP 폭을 보정하고 `P.A. SHOP - Tier 1/OPEN` 전체 문자열이 실제 Play MainCamera에서 읽히는 것을 확인했다.
- D3D11 PASS: 최종 에셋/정면/중복 검사, Tier 0 잠금→Tier 1 OPEN→입장→진열→가격→퇴장, FinalDemoRoute BreadLoaf 30G.
- 증거: `Logs/B10_CottageAudit/`, `Logs/B10_CottageFinalValidation_Final.log`, `Logs/B10_CottageRuntimeCapture_CompleteText.log`, `Logs/B10_EnterableShopRegression_Final.log`, `Logs/B10_FinalDemoRouteRegression.log`.
- 원본 `B10_Cottage.fbx`, 텍스처, 래퍼 프리팹, 메인 씬, 저장 스키마는 변경하지 않았다.

## 2026-07-16 — Codex — B05 목재 가공 작업대 기능 아트 최종화

- Unity Editor API로 B05 원본/래퍼/4면/실제 2×2 배치/콜라이더를 감사했다. 원본은 지면 `minY=0`, 15,629 vertices/14,689 triangles로 정상이며 상판·페그보드·스툴·서랍을 유지할 가치가 있다.
- 기존 배치의 local `-Z` interaction 면과 원본의 local `+Z` 작업면이 반대였던 원인을 확정했다. BasicWorkbench 런타임 Visual만 180° 정렬하고 실제 BoxCollider/NavMeshObstacle을 보이는 메시 크기에 맞췄다. 원본 프리팹 폭은 2×2 footprint 계약 때문에 보존했다.
- 기존 Wood→Plank 제작을 낮 준비 역할로 사용했다. Unity Editor API로 원목 입력→가이드 작업면→완성 Plank→coral clamp가 읽히는 저폴리 파생 메시/Resource 프리팹을 생성하고 Project PA 기존 재질만 재사용했다.
- 성공한 기존 `CraftingService` 트랜잭션 뒤에만 clamp handle/따뜻한 light의 0.72초 피드백을 연결했다. 동일 구도 캡처를 보고 과한 light intensity를 2.2→0.9로 낮췄다.
- 실제 Play Mode CraftingUI에서 Wood 2개→Plank 1개, 인벤토리 delta, 피드백, UI 닫기 PASS. ProcessingChain, ShopCustomization/v10, EnterableShop, FinalDemoRoute 30G 회귀 PASS. `dotnet build` 오류 0(기존 CS8785 경고 1).
- 증거: `Logs/B05_WorkbenchAudit/`, `Logs/B05_WorkbenchFinalAssets_Validation.log`, `Logs/B05_WorkbenchFinalPlayValidation_Final.log`, `Logs/B05_*Regression.log`.
- 원본 FBX/텍스처/래퍼 프리팹/메인 씬/저장 스키마/패키지는 변경하지 않았다. 다음 단일 작업은 플레이어/NPC 같은 구도 보행·접지 최종화다.

## 2026-07-16 — Codex — C-01~C-09 캐릭터 Unity 설정 보정 및 walking 검증 중단

- Unity Editor API로 C-01~C-09 Humanoid/Avatar/메시/텍스처/polycount, Idle/Walk 클립, 메인 씬 실제 할당과 런타임 크기·접지를 감사했다. 9종 모두 정상이고 스타일이 일관되어 외형 교체·Blender 수정은 하지 않았다.
- 고정 2× 배율과 3.6m 물리 몸체를 강제하던 `NpcPresentationNormalizer`를 실제 SkinnedMeshRenderer bounds 기준 1.75m 주민, +0.02m 접지, 1.8/0.4 Capsule/Agent, 0.75m stopping distance로 보정했다.
- NPC 절차 보행 cadence를 실제 이동 속도/1.15m 보폭 기준으로 바꾸고, 플레이어 기존 Foot IK에 `Walk.anim` 평균 속도 기반 제한 재생 속도 동기화를 추가했다.
- 재현 가능한 원본 lineup/동일 구도 Play 캡처와 수치 검사를 위해 `PA_CharacterFinalizer`를 추가했다. 메인 씬은 저장하지 않는다.
- D3D11 컴파일 PASS. 실제 idle에서 주민 8명 높이 1.748~1.751m, 발 +0.032~+0.035m, 물리 몸체·Agent·Avatar·그림자가 PASS했고 `character_idle_after.png`를 직접 확인했다.
- 첫 walking 최종 검증은 시작 온보딩의 `Time.timeScale=0` 때문에 플레이어 Animator speed가 1.0에 머물러 중단됐다. `BUG_LOG.md`에 OPEN으로 기록하고 동일 세션 재시도·회귀를 하지 않았다. 따라서 보행 최종화는 완료 선언하지 않는다.
- 현재 실제 할당 Bori=C-03, Miner=C-04, Farmer/Fisher=C-05 중복, C-02 미사용은 사용자 정체성 판단 전 임의로 바꾸지 않았다.
- 원본 FBX/Avatar/클립/텍스처/프리팹/메인 씬/NavMesh bake/저장/패키지는 변경하지 않았다. 다음 단일 작업은 timeScale 복원 후 walking 검증 1회다.

## 2026-07-16 — Codex — 캐릭터 walking 검증 OPEN 해소 및 최종 회귀

- `PA_CharacterFinalizer`가 기존 `PlayableDayScenarioController.RestoreSavedSession` 공개 경로로 시작 온보딩을 닫고 `Time.timeScale=1`을 확인하도록 보정했다. 플레이어/NPC 런타임 코드는 추가 변경하지 않았다.
- 최종 D3D11 walking에서 플레이어 5m/s/Animator·playback 2.25×, NPC 8명 2.5m/s/cadence 1.851~2.328, 보행 중 높이 1.737~1.758m와 발 +0.029~+0.037m가 PASS했다.
- 첫 캡처의 상점 기둥 군집·추월 겹침을 검증 전용 배치에서 제거했다. 실제 동서 도로 한 줄에 주민 8명을 두고 플레이어를 맨 오른쪽에 배치해 최종 `character_walk_after.png`에서 9명의 발·실루엣·방향·그림자를 확인했다.
- 회귀 PASS: InteriorCustomer 예약→완전 경로→앞자리 정지→구매→복귀, CustomerArrival 초대 3/동시 2 상한, FinalDemoRoute BreadLoaf 30G 판매.
- 기존 BUG_LOG 항목을 RESOLVED로 전환했다. 원본 C-01~C-09/Avatar/Walk 클립/텍스처/프리팹/메인 씬/NavMesh bake/저장/패키지는 변경하지 않았다.
- 증거: `Logs/CharacterFinalization_RuntimeFinal_FinalFraming.log`, `Logs/CharacterFinalization/character_walk_after.png`, `Logs/CharacterFinalization_InteriorCustomerRegression.log`, `CharacterFinalization_CustomerArrivalRegression.log`, `CharacterFinalization_FinalDemoRouteRegression.log`.

## 2026-07-16 — Codex — B02~B04 상점 진화 외관 비파괴 최종화

- Unity Editor API로 B02~B04 원본 FBX·래퍼·4면·bounds/polycount·콜라이더·Shop/ShopSlot 구성을 감사했다. 세 원본은 `minY=0`, 정상 메시·재질이고 실제 정면은 모두 로컬 `-X`였다.
- 작은 목재 잡화점, 민트 차양 마켓, 맨사드 지붕 부티크가 현재 코지 마을과 단계적으로 어울려 세 에셋을 분류 2(Unity 설정 수정)로 확정했다. Blender·재질 재작업·외형 교체는 수행하지 않았다.
- 기존 래퍼 전체를 생성할 때 생기는 Shop 1개와 ShopSlot 8/16/32개 중복을 피하도록 Visual-only Resource 프리팹 3개를 생성했다. 보이는 모델 bounds 기반 BoxCollider/NavMeshObstacle과 Entrance/Sign anchor만 포함한다.
- `ShopEvolutionController`가 기존 `currentTier`에서 Tier 0=B10, Tier 1=B02, Tier 2=B03, Tier 3+=B04를 파생하고, 기존 외부 문·스폰·BuildingEntrance·간판을 활성 모델의 문 앞으로 이동하도록 연결했다. 새 저장 필드나 진화 시스템은 추가하지 않았다.
- 실제 Play Mode Tier 1/2/3 캡처에서 접지·스케일·가림·문/플레이어/간판 정렬을 확인했다. B02 첫 After에서 입구 좌우 anchor 오류를 발견해 수정하고 같은 구도로 재캡처했다.
- D3D11 PASS: 단계별 외관 1개/Shop 중복 없음/물리 bounds/기존 배치 스냅샷 보존, Tier 0 잠금→Tier 1 개방→입장→6슬롯 진열→가격→퇴장, FinalDemoRoute BreadLoaf 30G. `dotnet build` 런타임/Editor 오류 0(기존 CS8785 경고만 존재).
- 변경: `Assets/Scripts/ShopEvolutionController.cs`, `Assets/Editor/PA_ShopEvolutionVisualFinalizer.cs`, `Assets/Resources/VisualFinalization/ShopEvolution/**`, 감사·출처·상태·인계 문서. 원본 FBX/텍스처/래퍼 프리팹/BuildingData/TierDefinition/메인 씬/저장/패키지는 변경하지 않았다.
- 증거: `Logs/ShopEvolutionAudit/`, `Logs/ShopEvolution_FinalAssets_AnchorFix.log`, `Logs/ShopEvolution_RuntimeFinal_AnchorFix.log`, `Logs/ShopEvolution_EnterableShopRegression.log`, `Logs/ShopEvolution_FinalDemoRouteRegression.log`.

## 2026-07-17 — Codex — P5 상점 진화·배치 해금 구현 중 / 첫 런타임 검증 중단

- 기존 `shop.interior` 원점과 v10 placement 레코드를 보존하면서 Tier 0/1=`5×4`, Tier 2=`6×5`, Tier 3+=`7×6`로 확장하는 런타임 구조를 구현했다.
- 기존 TierDefinition의 유효 ShopSlot 한도를 재사용해 6/8/12/20개 진열 한도를 적용했다. 기존 6개 진열대 중 하나를 비활성 런타임 템플릿으로 복제해 Tier 2부터 실제 `ShopSlot` 진열대를 추가 배치할 수 있게 했다.
- B05/B06/B07/B08을 Tier 1/2/3 해금과 장부 보상으로 연결하고, 기존 실내 바닥·동/북 벽·조명·장부 위치가 단계에 맞게 확장되도록 했다. 추가 바닥만 별도 `NavMeshSurface`로 연결한다.
- 기존 `VillageCultureVisualController`의 Processed 활성 상태에서만 따뜻한 공방 테마를 열며, 벽·진열대·조명·간판 색을 실제 기존 렌더러에 적용한다. 테마는 새 필드 없이 v10 `placeables`의 `shop.theme` 레코드로 저장한다.
- `ShopEvolutionController` Tier 배너를 5×4/B05, 6×5/8개/B06, 7×6/12개/B07+B08, Tier 4/20개 정보로 확장했다. 메인 씬·원본 모델/프리팹·TierDefinition·SaveData/SaveManager·패키지는 변경하지 않았다.
- 로컬 Runtime/Editor 어셈블리는 오류 0건(기존 CS8785/PA_ErrorTracker 경고만)이다. 첫 D3D11 Play 검증은 확장 조명 `Light`의 Unity 가짜 null 처리 오류로 초기화 전에 중단됐다. `BUG_LOG.md` OPEN 항목과 `Logs/P5_ShopProgression_D3D11.log`에 기록했으며 같은 실행은 반복하지 않았다.
- P5는 완료가 아니다. 다음 단일 조치는 `EnsureExpansionLight`의 null 검사를 명시형으로 고친 뒤 전용 검증 전체를 재실행하는 것이다.

## 2026-07-17 — Codex — P5 상점 진화·배치 해금 완료

- 확장 조명의 Unity 가짜 null을 명시적 null 검사로 수정했다. 중복된 레거시 `Resources/Tiers` 용량과 물리 실내 계약을 분리해 진열 한도를 Tier 0~4 `6/6/8/12/20`으로 고정했다.
- Tier 0/1 `5×4`, Tier 2 `6×5`, Tier 3+ `7×6` 확장은 기존 원점과 배치 레코드를 유지한다. 확장 전용 NavMeshSurface를 동·북 양방향 NavMeshLink로 기존 실내 island와 연결하고 먼 확장 셀까지 완전 경로를 검증했다.
- 기존 authored 선반을 비활성 템플릿으로 재사용하되 템플릿 루트는 상점 hierarchy 밖에 두어 기본 ShopSlot 6개와 Shop 등록을 오염시키지 않는다. Tier 2/3에서 배치된 복제본만 실제 판매 슬롯이 된다.
- B05/B06/B07+B08 Tier 보상과 Processed 문화의 따뜻한 공방 테마를 연결했다. 테마는 v10 `shop.theme/fixed.shop.theme` 특수 레코드로 저장·복원한다.
- 동일 게임 카메라 Tier 0/3 캡처를 직접 비교해 확장 스케일, 선반 통로, 조명, 색상, 진행 원장을 확인했다. 테마 버튼과 상태 문구의 겹침을 보정하고 재캡처했다.
- `dotnet build` Runtime/Editor 오류 0. D3D11 P5 전체, ShopCustomization, EnterableShop, SaveRoundTrip, FinalDemoRoute가 PASS했다. 증거: `Logs/P5_ShopProgression_D3D11_Release.log`, `Logs/ShopProgressionUnlock/20260717_005831/`, `Logs/P5_*Regression.log`.
- 변경: `Assets/Scripts/ShopCustomizationController.cs`, `Assets/Scripts/ShopEvolutionController.cs`, `Assets/Editor/PA_ShopProgressionUnlockValidator.cs`와 `.meta`, 배치/저장/상태/인계 문서. 메인 씬·원본 에셋/프리팹·TierDefinition·BuildingData·SaveData/SaveManager·패키지는 변경하지 않았다.

## 2026-07-17 — Codex — Task 044 주민 의뢰(간단 요청) 표시 설계

- Dialogue/NpcProfile/DemandInsight/전문가 레시피/Inventory/Friendship/일일 활동 저장을 실제 코드와 Resources 에셋으로 대조했다.
- 신규 `DESIGN_RESIDENT_REQUEST.md`에 Chef_01의 Wheat 3개를 첫 요청으로 확정했다. 기존 `Economy` 대사 토픽, `assignedRecipes`, 일일 활동 문자열 저장을 재사용하고 범용 퀘스트 엔진과 저장 스키마 추가를 금지했다.
- `PA_SceneAutoBuilder`가 Tailor에게 `Recipe_Clothes`가 아닌 `Recipe_Bread`를 배정하는 기존 불일치를 기록하고, 데이터 수복 전 요청 후보에서 제외했다.
- Task 044를 DONE으로 전환하고 완료 매트릭스를 DONE 33 / PARTIAL 21 / TODO 7 / BLOCKED 6 / DECISION_REQUIRED 18로 갱신했다.
- 검증: 관련 경로/API/에셋 정적 대조 및 `git diff --check` 통과. 문서 전용 Task이므로 Unity/Play Mode는 실행하지 않았다.
- 코드·씬·에셋·패키지·저장 스키마 변경 없음. 커밋·push 없음.

## 2026-07-17 — Codex — Task 045 낮 활동 결과→재고 연결 문서 동기화

- `DAYTIME_ACTIVITIES.md`를 신규 작성해 현재 6개 일일 재고 원천, 공통 `ItemInstance`/일일 제한/저장 계약, NPC 공급·가공의 부분 연결을 실제 소스 기준으로 정리했다.
- 기존 D3D11 로그의 Fish 18G/Fisher_01과 Ore 15G/Miner_01 획득→진열→가격→밤 개점→구매→경제/통계 왕복만 완료 증거로 사용했다.
- Carrot/Wheat 개별 판매, 농사, 주민 의뢰, Wood 자연 공급→B05 가공→판매는 미검증/미구현으로 분리했다.
- `PROJECT_PA_GAME_LOOP.md`의 낮·해질녘·밤 현재 상태를 최신화했다.
- Task 045를 PARTIAL→DONE으로 전환하고 매트릭스를 DONE 34 / PARTIAL 20 / TODO 7 / BLOCKED 6 / DECISION_REQUIRED 18로 갱신했다.
- 검증: activityId 6종 소스/문서 대응, Fish/Ore 최종 로그, 루프 표 문구, 문서 whitespace 정적 검사 PASS. Unity는 문서 전용 범위라 실행하지 않았다.
- 코드·씬·프리팹·에셋·패키지·저장 스키마 변경 없음. 커밋·push 없음.

## 2026-07-17 — Codex — Task 046 카테고리별 판매 통계 조사 완료

- 신규 `VILLAGE_TREND.md`에 실제 판매 성공→`SaleRecord`→최근 판매 재집계→선도 카테고리→결산/다음 날 변화 경계를 기록했다.
- `SaleRecord` 1건은 상품 수량 1개가 아니라 성공한 `ShopSlot` 거래 1건이며, `price`는 `EffectiveDisplayPrice × 진열 스택 수량`인 총 결제액임을 소스와 대조했다.
- `SalesLogManager` 기본 100건 보관, `VillageChangeSignalController` 최근 40건, `count×1000+revenue`, Tool 제외, 명시적 동점 규칙 부재를 확정했다.
- Processed 다음 날 시각 변화의 pending/active 상태는 v10에서 복원되지만 판매 기록·일일 판단 Dictionary·카테고리 통계는 런타임 전용임을 분리했다.
- 기존 D3D11 로그에서 Processed 2건/76G, Fish 18G, Ore 15G, BreadLoaf 30G, Processed 다음 날 변화, pending→active 저장 왕복을 재확인했다.
- Task 046을 PARTIAL→DONE으로 전환하고 매트릭스를 DONE 35 / PARTIAL 19 / TODO 7 / BLOCKED 6 / DECISION_REQUIRED 18로 갱신했다.
- 검증: 실제 경로 기반 문서 표식 7개·소스 표식 20개·로그 표식 6개·참조 파일 4개와 trailing whitespace PASS. 문서 전용 작업이므로 Unity를 새로 실행하지 않았다.
- 코드·씬·프리팹·에셋·패키지·저장 스키마 변경 없음. 커밋·push 없음.

## 2026-07-17 — Codex — Task 047 낚시·캠핑·가구 트렌드 점수 데이터 설계 완료

- `VILLAGE_TREND.md`를 확장해 기존 ItemCategory 신호 위에 이름이 있는 생활 트렌드를 얹는 데이터 계약을 작성했다.
- 실제 데이터 기준 fishing은 Fish(Raw, 18G)와 생선구이(Processed, 52G), furniture는 목제 가구(Luxury, 185G/Tier 2/Plank 3)를 정확한 `(category,itemName)` 쌍으로 매핑했다.
- camping은 전용 Item/Recipe/활동/Resources가 없으므로 비활성으로 확정했다. 개발용 `Shop_Tent_Kit`과 배치 선반·작업대·보관함은 판매 통계에서 제외했다.
- 성공 `SaleRecord`만 점수를 만들며 `transactions×1000+min(revenue,999)`로 거래 1건 우선성을 보장한다. 판매 수량·품질·거절·배치 수는 v1 점수에 넣지 않는다.
- 오늘 방향은 결산 일차 판매만, 장기 방향은 향후 7개 일차 스냅샷으로 분리했다. 동점은 거래→제한 전 매출→최신 판매→trendId 순서로 결정한다.
- Task 047을 PARTIAL→DONE으로 전환하고 매트릭스를 DONE 36 / PARTIAL 18 / TODO 7 / BLOCKED 6 / DECISION_REQUIRED 18로 갱신했다.
- 최종 정적 검증: 문서 10·데이터/소스 37·SaleRecord 필드 6·캠핑 에셋 용어 9·점수 예시 6·whitespace PASS. 문서 전용 작업이므로 Unity를 새로 실행하지 않았다.
- 코드·씬·프리팹·에셋·패키지·저장 스키마 변경 없음. 커밋·push 없음.

## 2026-07-17 — Codex — Task 051 시설 해금 방향 예고 표시 완료

- 기존 `VillageChangeSignalController`가 집계한 성공 판매 선도 카테고리를 감사 앱용 시설 후보 문구로 번역했다. Raw=생산자 보관·수거, Processed=조리·가공, Utility=수리·공구, Luxury=포장·문화 진열 방향이다.
- `AuditResultUI`에 “시설 방향 예고” 카드를 추가했다. 판매 카테고리·다음 시설 후보·거래 건수/매출 신호와 `실제 해금: 티어·감사 조건`을 함께 보여 실제 해금으로 오해하지 않게 했다.
- `TierService`, `AuditService`, 판매 수학, 저장 스키마, 메인 씬·프리팹·패키지는 변경하지 않았다.
- Runtime/Editor `dotnet build` 오류 0. 기존 `CS8785` 및 Editor `CS0414` 경고만 유지됐다.
- D3D11 FinalDemoRoute에서 BreadLoaf 30G 판매 후 감사 앱의 가공품→조리·가공 작업대 예고와 해금 권한 경계를 확인했고 전체 Day 1 루트가 PASS했다.
- 첫 1920×1080 캡처에서 긴 문구의 말줄임을 발견해 네 줄의 짧은 문구로 보정했다. 같은 구도 최종 `Logs/FinalPresentation/20260717_021259/04_audit_app_goal.png`에서 전체 문구·전화 패널·HUD·핫바 비겹침을 직접 확인했으며 FinalPresentation도 PASS했다.
- 첫 Unity 호출은 `-quit`가 Play Mode 콜백 전에 종료되어 검증 증거로 사용하지 않았다. 최종 증거는 `Logs/Codex_Task051_FinalDemoRoute_Final.log`과 `Logs/Codex_Task051_FinalPresentation_Final.log`이다. 커밋·push 없음.

## 2026-07-17 — Codex — Task 052 이벤트 후보 설계 정적 검증 중단

- 신규 `DESIGN_EVENTS.md` 초안에 `해변 풍어제 — 낚시 대회와 밤 장터`를 첫 후보로 설계했다. 전날 qualifying 생선 판매→다음 날 행사→기존 낮 낚시/가공 선택→밤 판매→정산 경로다.
- Task 047의 정확한 Fish/생선구이 판매 매핑과 기존 `GameClock`, Day/Night 페이즈, `SalesLogManager`, `TierService` 경계를 재사용하고 구매·가격·티어·시설 해금 소유권은 변경하지 않도록 했다.
- 문서 표식과 소스 계약 대조 후 trailing-whitespace 검사에서 메타데이터 2개 행의 Markdown 강제 줄바꿈 공백을 검출했다. `BUG_LOG.md` OPEN 기록 후 같은 검사 재시도 없이 중단했다.
- Task 052는 ACTIVE/TODO 상태와 기존 집계를 유지한다. 코드·씬·프리팹·에셋·패키지·저장 변경, Unity 실행, 커밋·push 없음.

## 2026-07-17 — Codex — Task 052 이벤트 해금 후보 설계 완료

- `DESIGN_EVENTS.md`의 첫 후보를 `해변 풍어제 — 낚시 대회와 밤 장터`로 확정했다. 전날 Fish/생선구이 성공 판매가 정산에서 다음 날 행사를 예약하고, 행사 날 기존 낮 낚시→가공 선택→밤 판매→정산을 그대로 사용한다.
- 이벤트 상태·비처벌 재시도·평판 +1 후보·시각/동선 기준·공용 명명 트렌드·저장 필드 후보·Task 078 승인과 런타임 검증 계약을 문서화했다.
- 중단 원인이던 메타데이터 공백 두 칸만 제거했다. 전체 `git diff --check`, 문서 계약 10, 소스/데이터 10, Task041 로그 5, whitespace 0, 이벤트 런타임 클래스 0 검사가 PASS했다.
- Task 052를 TODO→DONE으로 전환하고 매트릭스를 DONE 38 / PARTIAL 17 / TODO 6 / BLOCKED 6 / DECISION_REQUIRED 18로 갱신했다.
- Unity는 문서 전용 범위라 실행하지 않았다. 코드·씬·프리팹·에셋·패키지·저장 스키마 변경, 커밋·push 없음.

## 2026-07-17 — Codex — Task 054 판매 통계 v11 추가 확장 설계 완료

- 현재 실제 저장 v10과 v0→v10 마이그레이션을 기준으로 `SAVE_SCHEMA.md`에 v11 판매 통계 계약을 추가했다.
- 최근 판매 원거래, 일차 구매/거절, 일차·카테고리 판매, 최근 7개 완료 일차의 명명 트렌드를 서로 다른 저장 리스트와 DTO로 설계했다.
- 성공 판매·거절·밤 정산의 단일 기록 권한, 원거래 기본 100건/집계 7일 보존, `GameClock` 뒤·소비자 평가 앞 복원 순서, v10→v11 빈 기본값과 과거 통계 비추정 원칙을 확정했다.
- Task 055의 기존 `SaveData`/`SaveManager` 전용 범위만으로는 private SalesLog 상태를 복원할 수 없음을 기록하고, 사용자 승인 요청에 `SalesLogManager` 최소 API·공용 명명 트렌드 소유자·관련 검증기를 포함하도록 경계를 명시했다.
- 오탐 선택자를 폐기한 재개 검증이 문서 계약 12/12, 정확한 v10 소스 계약 17/17, whitespace 0, 전체 `git diff --check` PASS했다.
- Task 054를 TODO→DONE으로 전환했다. Unity는 문서 전용이라 실행하지 않았으며 C#·씬·프리팹·에셋·패키지·실제 저장 스키마 변경, 커밋·push 없음.

## 2026-07-17 — Codex — Task 023 패치 적용 전 중단

- 별도 rarity 필드가 없고 `ItemInstance.quality`만 실제 전략 데이터임을 확인했다. 현재 판매 카탈로그는 기본가 8~52G와 150~185G로 분리되어 있어 100G 경계를 `가격 파생`으로 명시하는 최소 UI 변경을 계획했다.
- `ShopPriceUI` 다중 패치가 같은 색상 상수 문맥에서 두 번 실패했다. 규칙에 따라 세 번째 시도 없이 `BUG_LOG.md`에 OPEN 기록하고 중단했다.
- 단독 적용됐던 미사용 필드는 제거했다. C#·씬·프리팹·아이템 에셋·구매 수학·저장·패키지의 최종 변경은 없고 Task 023은 TODO를 유지한다. Unity 컴파일/캡처는 구현 전 중단되어 확인하지 못했다.

## 2026-07-17 — Codex — Task 023 희귀품/일반품 데이터 표시 완료

- 별도 rarity 원본 필드가 없는 현재 카탈로그의 8~52G/150~185G 가격대 사이인 100G를 임시 경계로 사용하고, 화면에 `가격 파생값`임을 명시했다.
- `ShopPriceUI`에 `일반품|희귀품 · 가격 파생값 · 품질 ×N.NN` 줄을 추가했다. 실제 `ItemInstance.quality`를 읽으며 일반품은 연녹색, 희귀품은 금색이다.
- FinalPresentation이 BreadLoaf/1.00 일반품과 의류/1.25 희귀품을 모두 assertion하고 각각 1920×1080으로 캡처하도록 확장했다.
- Runtime/Editor `dotnet build` 오류 0. D3D11 FinalPresentation PASS(`Logs/Codex_Task023_FinalPresentation_RarityCapture.log`)와 FinalDemoRoute BreadLoaf 30G PASS(`Logs/Codex_Task023_FinalDemoRoute.log`).
- 같은 카메라 Before `Logs/FinalPresentation/20260717_021259/02_shop_price_ui.png`, After 일반/희귀 `Logs/FinalPresentation/20260717_030256/02_shop_price_ui.png`, `02b_shop_price_ui_rare.png`를 직접 비교했다. 글자 잘림·버튼/HUD 겹침 없음.
- 씬·프리팹·Item 에셋·구매 수학·저장·패키지·커밋·push 변경 없음. Task 023을 DONE으로 전환했다.

## 2026-07-17 — Codex — Task 025 읽기 전용 추천 기준가 표시 완료

- 별도 `IdealSellPrice` 필드가 없음을 확인하고 `PurchaseEvaluator`의 실제 `basePrice × (1 + 0.5 × max(0, quality-1))` 기준을 가격 UI에서 읽기 전용으로 재사용했다.
- `추천 기준가 N G · 기본가+품질`을 현재 설정가 아래에 표시한다. 가격 자동 설정은 없으며 고품질 의류에서 현재가 165G와 추천가 186G가 동시에 보인다.
- 기존 예상 반응 힌트도 같은 품질 보정 기준가를 사용해 추천 숫자와 화면 반응의 기준을 일치시켰다. 구매 수학은 변경하지 않았다.
- 첫 1920×1080 캡처에서 추천가와 +/- 버튼 겹침을 발견해 패널 높이·세로 여백·글자 대비를 보정했다. 같은 구도 최종 일반/희귀 캡처에서 잘림·겹침 없음.
- Runtime/Editor `dotnet build` 오류 0. D3D11 FinalPresentation 추천가 30G/186G·비자동적용 PASS(`Logs/Codex_Task025_FinalPresentation_Final.log`), FinalDemoRoute BreadLoaf 30G PASS(`Logs/Codex_Task025_FinalDemoRoute.log`).
- 최종 캡처: `Logs/FinalPresentation/20260717_031416/02_shop_price_ui.png`, `02b_shop_price_ui_rare.png`. 씬·프리팹·Item 에셋·`PurchaseEvaluator`·저장·패키지·커밋·push 변경 없음.

## 2026-07-17 — Codex — Task 031 주민/관광객 계층 표시 완료

- `NpcProfile`에 계층 필드가 없고, 현재 씬의 소비형 NPC 8명 모두가 `NpcScheduleController`의 실제 마을 일과표를 가진 상주 주민임을 감사했다.
- 가짜 관광객을 지정하지 않고 유효한 마을 일과표가 있으면 `[주민]`, 없으면 향후 임시 방문 손님용 `[관광객]`으로 표시만 파생했다. 이 값은 구매·FSM·스케줄·저장에 입력되지 않는다.
- F10 성향 패널의 이름 옆에 계층을 추가하고, 기본 플레이에서는 기존 `NpcBubbleUI` 본문과 분리된 녹색 `[주민]` 태그를 표시한다. FinalRoute가 검사하는 말풍선 본문은 그대로다.
- 첫 캡처에서 성향 패널이 기본 F10 숨김 상태임을 확인해 머리 위 태그로 실제 노출을 보강했다. 최종 1920×1080 캡처 `Logs/CustomerPanelReview/20260717_032821/customer_panels_1920x1080.png`에서 태그·본문·HUD·핫바 비겹침을 직접 확인했다.
- Runtime/Editor 순차 빌드 오류 0. D3D11 CustomerPresentation(주민 8/가짜 관광객 0/폴백), CustomerPanelLayout, FinalDemoRoute BreadLoaf 30G PASS. 첫 병렬 dotnet 시도는 공용 출력 DLL 잠금으로 1회 실패했으며 같은 방식을 폐기한 순차 빌드가 통과했다.
- `NpcController`, `NpcProfile`, `PurchaseEvaluator`, 경제 수학, 씬·프리팹·저장·패키지·커밋·push 변경 없음. Task 031을 DONE으로 전환했다.

## 2026-07-17 — Codex — Task 024 상품 진열 테마 코너 설계 완료

- 실제 `ShopSlot`, `Item.category`, `ShopCustomizationController`의 placement/footprint, `GridService`, v10 복원 순서, 판매→마을 방향 경로를 감사했다.
- 신규 `DESIGN_THEME_CORNER.md`에서 같은 판매 가능 카테고리의 활성 진열대 placement가 4방향으로 인접하고 2개 이상 연결될 때 코너가 성립하도록 계약했다.
- 기존 상점 전체 `shop.theme/fixed.shop.theme`와 이름·저장·해금 의미를 분리했다. 코너는 품절·보충·이동·회수·로드 뒤 실시간 파생하며 저장하지 않는다.
- 코너 배치만으로 마을 점수나 보너스를 만들지 않는다. 실제 구매 성공의 기존 단일 `SaleRecord`만 카테고리 방향에 반영된다.
- 후속 좁은 placement 읽기 API, `MerchandisingCornerController`, 배치 장부/월드 라벨, 예상 수정 파일과 D3D11·저장·회귀 검증 11개를 명시했다.
- 정적 검사 PASS. Unity는 문서 전용 범위라 실행하지 않았다. 코드·씬·프리팹·에셋·패키지·저장 스키마·커밋·push 변경 없음. Task 024를 DONE으로 전환했다.

## 2026-07-17 — Codex — Task 086 상품 진열 테마 코너 구현 PARTIAL

- 신규 `MerchandisingCornerController.cs`에 실제 `ShopSlot`과 placement footprint 기반의 같은 카테고리 4방향 연결 요소 판정을 구현했다. Tool/recovered/빈 진열은 제외하며 별도 저장·보너스·판매 기록을 만들지 않는다.
- `ShopCustomizationController`에 코너 전용 읽기 projection과 장부 요약 갱신을 추가하고 `PA_RuntimeSceneBinder`에 런타임 서비스를 등록했다.
- 신규 `PA_ThemeCornerValidator.cs`와 validator registry 항목을 추가했다. 목표 검증 범위는 음성/양성 인접, 품절·보충, 회수·이동, 기존 판매→마을 방향, v10 격리 로드 재파생, 동일 구도 캡처다.
- Unity Runtime/Editor 컴파일 PASS. 첫 D3D11 실행은 컨트롤러/grid/6개 슬롯/Raw·Processed/빈 상태 코너 0·라벨 0까지 PASS했다.
- 첫 캡처 직접 `Camera.Render()`에서 Unity 네이티브 크래시가 발생해 `BUG_LOG.md`에 기록하고 같은 소스 재시도 없이 중단했다. 나머지 기능·회귀·시각 검토는 확인 못 함. Task 086은 PARTIAL 유지.
- 씬·프리팹·패키지·저장 스키마·커밋·push 변경 없음.

## 2026-07-17 — Codex — Task 086 기능 검증 PASS / 전체 회귀 BLOCKED

- `PA_ThemeCornerValidator`의 직접 `Camera.Render()`를 일반 GameView `ScreenCapture`로 교체하고, 실제 커밋 경로와 일치하도록 검증 전용 회수 가구 재배치 상태를 보정했다.
- D3D11 전용 검증에서 Raw/Processed 4방향 연결, 대각선·간격·혼합 제외, 품절·보충, 회수·이동, 실제 구매 3건, Processed 마을 방향, v10 저장 후 Raw2 재파생이 모두 PASS했다.
- Runtime/Editor 빌드는 오류 0이다. world label의 TMP outline을 제거해 검은 재질 면이 코너 자체에 고정되는 문제는 피했지만, ScreenCapture에는 실행별로 이동하는 검은 Canvas/TMP 프레임이 간헐적으로 남았다.
- 기능 로그 `Logs/Codex_Task086_ThemeCorner_CaptureFinal.log`. 깨끗한 동일 구도 증거는 none=`20260717_130433`, Raw2=`20260717_130201`, Processed4=`20260717_130433`이다.
- 기존 ShopCustomization 회귀가 `PA_ShopCustomizationValidator.cs:388`의 직접 `Camera.Render()`에서 같은 Unity 네이티브 충돌을 두 번째로 재현했다. 프로젝트 규칙에 따라 세 번째 Unity 실행과 나머지 회귀를 중단했다.
- Task 086은 기능 구현을 보존하되 전체 회귀와 기본 플레이 화면의 코너 라벨 가독성이 미완이므로 PARTIAL을 유지한다. 씬·프리팹·패키지·저장 스키마·그래픽 설정·커밋·push 변경 없음.

## 2026-07-17 — Codex — Task 087 Tripo3D 임시 에셋 감사 정합화 완료

- FBX 174/OBJ 150/GLB 0/Blend 0과 Nature Pack 외 FBX 24개를 전수 대조했다. Unity 6 바이너리 메인 씬은 문자열 색인만 사용해 C-01~C-09, B05~B12 실사용을 확인했다.
- GUID→래퍼 프리팹→BuildingData/청사진, Resources와 코드 경로를 대조했다. B06 Kitchen은 2×2/Tier 2, B07 Forge는 3×2/Tier 3, B08 Sewing은 2×2/Tier 3 배치와 기존 레시피/전문 주민에 이미 연결되어 있음을 확정했다.
- 세 작업대는 기능 통합 완료·시각 최종화 대기로 분류했다. B11/B12는 현재 정적 장식이며 배치 카탈로그/상호작용 기능이 없음을 명시했다. 캡처 증거 없는 Blender/재질 재작업은 확정하지 않았다.
- Ultimate Nature Pack의 Quaternius/CC0 1.0 원문을 확인했다. 개별 Tripo 생성/상업 이용 증빙과 Froggy Chair 라이선스 미확인은 최종 배포 게이트로 기록했다.
- `TRIPO_ASSET_AUDIT.md`, `ASSET_AND_TOOL_PROVENANCE.md`와 작업/상태 문서를 갱신했다. 정적 모델 수·배치/레시피·씬·라이선스 표식 검사 PASS.
- 직접 렌더 크래시 2회 경계 때문에 Unity는 실행하지 않았다. 코드·씬·프리팹·원본 모델/텍스처·패키지·저장·커밋·push 변경 없음. Task 087 DONE, Task 086 PARTIAL 유지.

## 2026-07-17 — Codex — Task 088 주민 재료 요청 구현(PARTIAL)

- `NpcDialogue`에 전문 분야/작업대와 일치하는 실제 담당 레시피의 첫 재료를 당일 요청으로 파생하는 흐름을 추가했다. Chef Wheat3, Blacksmith Ore2, Carpenter Wood2이며 Tailor→Bread 불일치는 제외한다.
- 기존 상호작용 프롬프트와 DialogueUI에 요청 확인·보유량·건네기·완료 상태를 연결하고 전문 주민 Economy 대사를 보강했다.
- `Inventory.CountItems`로 인벤토리+핫바 수량을 합산하고 정확 수량 차감 뒤 기존 낮 준비 활동 문자열 저장 경로에 완료 ID를 기록한다. 보상은 친밀도만 지급한다.
- Runtime/Editor 컴파일 오류 0, 레시피/배정/수량/저장 목록/금지 경제 경계 정적 검사 PASS.
- 동일 직접 렌더 네이티브 충돌 2회 경계 때문에 Unity는 실행하지 않았다. 실플레이·저장 왕복·다음 날·밤 차단·UI 캡처 미확인으로 Task 088은 PARTIAL이다.
- 씬·프리팹·SaveData/SaveManager·경제/구매/판매·패키지·커밋·push 변경 없음.

## 2026-07-17 — Codex — Task 089 고정 밭 Wheat 재배 F1/F2 구현(PARTIAL)

- `Item_15_Seed.cropPrefab`→`Crop_Corn`, `Crop_Corn.harvestItem`→`Item_Wheat` 참조와 Wheat 3개 수확량을 복구했다.
- 신규 `FarmPlotInteraction`이 농부 작업 지점 근처 경작 이랑 메시 2칸, 낮 전용 심기/성장/수확 프롬프트, 실제 Wheat 3단계 표현을 연결한다.
- 기존 일일 활동으로 씨앗 2개를 제공한다. 심기 성공 후 씨앗 1개를 차감하고, 인벤토리 전량 수용 가능 확인 후 Wheat 3개를 지급해 실패 시 상태를 보존한다.
- Runtime/Editor 컴파일 오류 0, 데이터/고정 밭/낮 게이트/안전 순서/금지 경계 12개 정적 검사 PASS.
- Unity 실플레이·카메라 확인은 직접 렌더 충돌 2회 경계로 미실행이다. F3 날짜 성장/plot 저장은 승인 전 미구현이라 Task 089는 PARTIAL이다.
- `PlayerInteraction`, 씬, 저장 스키마, 상점/경제/구매/NPC 코어, 패키지·커밋·push 변경 없음.

## 2026-07-17 — Codex — Task 090 Day 2+ 생활–상점 운영 체크리스트 구현(PARTIAL)

- 기존 `PlayableDayScenarioController`의 정적 Day 2+ 안내를 실제 상태 기반 체크리스트로 교체했다.
- 0.5초마다 낚시·채광·농사/농사 준비·채집·주민 재료 도움의 당일 완료, 가방+핫바+진열대의 판매 가능 상품 종류, 진열/가격, 밤 개점, 당일 판매, 정산 상태를 읽는다.
- 상단 목표도 낮 준비/밤 개점 전/영업 중/정산 단계에 맞춰 바뀐다. 새 퀘스트 상태·보상·저장 필드는 만들지 않았다.
- Runtime/Editor 순차 빌드 오류 0. 실시간 갱신, 네 활동군, 상품 종류 집계, Tool/Tier 제외, 실제 일일 판매, 경제·저장 비변경의 정적 계약 10개 PASS.
- 직접 렌더 네이티브 충돌 2회 경계를 지켜 Unity는 실행하지 않았다. Day 2 실제 상태 전환과 1920×1080 패널 가독성 확인 전까지 Task 090은 PARTIAL이다.
- `Shop`/`ShopSlot`/경제/구매/NPC FSM/씬/프리팹/저장 스키마/패키지·커밋·push 변경 없음.

## 2026-07-17 — Codex — Task 091 가공 결과물 수용량 선검사 구현(PARTIAL)

- 기존 `CraftingService`가 재료를 먼저 차감하고 결과 추가에 실패하던 공용 손실 경로를 수정했다.
- 결과 품질과 `ItemInstance`를 먼저 계산하고, 실제 `RemoveItems` 순서인 핫바→가방을 모의한다. 재료 소비로 비게 될 슬롯과 `CanStackWith`가 허용하는 품질·가격 메타 일치 스택을 확인한 뒤에만 실제 재료를 차감한다.
- 공간이 없으면 false와 명확한 경고를 반환하며 재료는 그대로 남는다. 공간이 있으면 기존 품질 계산, 차감, `AddInstance`, B05 작업대 성공 피드백을 유지한다.
- 현재 Resources 레시피 8개 모두 유효한 양수 재료/출력을 확인했다. Runtime/Editor 순차 빌드 오류 0, 트랜잭션·금지 경계 정적 계약 11개 PASS.
- 직접 렌더 네이티브 충돌 2회 경계를 지켜 Unity는 실행하지 않았다. 가방 가득 참/메타 일치 스택/재료로 비는 슬롯 실제 분기 확인 전 Task 091은 PARTIAL이다.
- `Inventory`, 레시피/아이템/작업대/UI, 저장, 상점/경제/NPC, 씬·프리팹·패키지·커밋·push 변경 없음.

## 2026-07-17 — Codex — Task 092 Raw 다음 날 생산자 보관·수거 변화 구현(PARTIAL)

- 기존 VC-001A의 실제 판매→pending→다음 날 `DayPreparation` 계약을 `Raw`까지 확장했다. 한 refresh에 여러 판매가 들어오면 최신순 실제 Raw/Processed 판매 한 건을 대표 변화로 선택한다.
- Raw 활성 시 기존 Processed 루트는 꺼지고 `PA_VillageCulture_Raw`만 켜진다. 저장은 v10의 기존 pending/active category 문자열을 그대로 사용한다.
- 새 원시 큐브 없이 출처가 확인된 Quaternius CC0 `Prop_WoodLog` 2개·`Prop_Rock` 1개와 Project P.A. 자체 `B10_Cottage_ShopSign` 메시를 재사용해 `원자재 수거처`를 구성했다. 복제 Collider/기능 MonoBehaviour는 제거한다.
- Runtime/Editor 순차 빌드 오류 0. 실리소스·최신 판매·새 변화 힌트 1회 재설정·당일/다음 날·Raw/Processed 상호 배타·무충돌·기존 저장 계약 정적 검사 14개 PASS.
- 첫 병렬 빌드는 두 빌드가 공용 DLL을 동시에 잠가 1회 실패했다. 병렬 방식을 폐기하고 순차 빌드로 통과했으며 코드 오류가 아니었다.
- 직접 렌더 네이티브 충돌 2회 경계를 지켜 Unity는 실행하지 않았다. 실제 GameCamera 전후, 간판 방향·스케일·겹침·동선 확인 전 Task 092는 PARTIAL이다.
- 판매/가격/NPC, `SaveData`/`SaveManager`, 씬·프리팹·원본 에셋·패키지·커밋·push 변경 없음.

## 2026-07-17 — Codex — Task 093 실제 판매 기반 낚시·가구 명명 트렌드 구현(PARTIAL)

- 기존 `VillageChangeSignalController`가 오늘의 성공 `SaleRecord`에서만 Fish/생선구이→`trend.fishing`, 목제 가구→`trend.furniture`를 정확 쌍으로 집계하도록 구현했다. 캠핑은 활성 매핑이 없다.
- 점수 `transactions×1000+min(revenue,999)`와 거래→매출→최근 판매→trendId 오름차순 동점 규칙을 적용했다. Fish 18G 1건=1018, 2건/36G=2036이다.
- 기존 카테고리 `count×1000+revenue`, 결산 첫 줄, 시설 방향 예고를 보존하고 결산 둘째 줄에 생활 트렌드·거래·매출·점수를 추가했다.
- 기존 Play Mode validator에 정확 점수, 잘못된 카테고리, 캠핑 비활성, 매출/거래 우선순위 assertions를 추가했다.
- Runtime/Editor 순차 빌드 오류 0, 정적 계약 18개 PASS. dirty worktree 기준선 오인 검사 1회는 `BUG_LOG.md`에 기록하고 Task 093 런타임 소유권 한정 검사로 해소했다.
- 직접 렌더 네이티브 충돌 2회 경계를 지켜 Unity는 실행하지 않았다. 결산 가독성과 생선구이·목제 가구 전체 왕복 확인 전 Task 093은 PARTIAL이다. 판매/가격/NPC/저장, 씬·프리팹·패키지·커밋·push 변경 없음.

## 2026-07-17 — Codex — Task 094 Tripo3D 장기 정책과 미확인 소품 노출 정리(PARTIAL)

- 사용자 추가 지침을 기존 `TRIPO_ASSET_AUDIT.md`, 배치 아키텍처와 Placeable 가이드에 통합했다. 8분류 개별 판정, 플레이어/주민 정체성 보존, 기능 우선 창고/작업대 판정, Unity/Blender 수정 경계, 원본/수정본 분리, 같은 카메라 검증과 출처·배포 게이트를 장기 규칙으로 고정했다.
- 저장소에서 라이선스 문서를 찾지 못한 `Prop_FroggyChair`의 실내·B11 광장 런타임 생성 2곳을 `DemoVisualDressingController`에서 제거했다. 기존 B01 노점, Shop/ShopSlot, 실내 러그·선반·계산대·CC0 식생, 광장 벤치는 보존했다.
- 원본 FBX와 래퍼/Resource 프리팹은 삭제·덮어쓰기하지 않았다. 플레이어 런타임 참조는 0이지만 Resource의 최종 빌드 포함 가능성은 라이선스 증빙 또는 격리 전까지 배포 게이트다.
- Runtime/Editor 순차 빌드 오류 0. 기존 CS8785와 Editor CS0414 경고만 유지됐다. 런타임 참조 0, 원본 3종, B01/식생/벤치 보존 정적 계약 PASS.
- Unity는 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 실내·광장 빈자리/초점/동선의 GameCamera 확인 전 Task 094는 PARTIAL이다. 씬·프리팹·원본 에셋·Resources·패키지·저장·경제·NPC·배치 코어·커밋·push 변경 없음.

## 2026-07-17 — Codex — Task 095 기존 가구 보조 루프 단계 안내 연결(PARTIAL)

- `ProcessingOpportunityController`에 기존 `Recipe_Furniture`를 기준으로 가구 루프의 다음 실제 행동을 계산하는 읽기 전용 projection을 추가했다.
- Day 4+ 운영 체크리스트는 Tier 1 매출, B05 설치, Plank 3개 준비, Tier 2 매출, 가구 제작, 진열, 당일 판매 중 현재 한 단계만 표시한다. 판매 완료 뒤 기존 결산의 가구 문화 확인을 안내한다.
- 아이템/청사진 지급, Tier 강제, 자동 제작·진열·판매, 가격/수익 임계치, 저장 스키마를 변경하지 않았다.
- Runtime/Editor 순차 빌드 오류 0. 정확 레시피·Tier 2·BasicWorkbench·재료 수량·ShopSlot·당일 SaleRecord·상태 변경 호출 부재 정적 계약 PASS.
- 직접 렌더 네이티브 충돌 2회 경계를 지켜 Unity는 실행하지 않았다. 실제 Tier별 문구, 1920×1080 잘림과 제작→진열→판매→정산 왕복 확인 전 Task 095와 Task 070은 PARTIAL이다.
- 씬·프리팹·레시피/아이템/Tier 에셋·저장·상점/경제/구매/NPC 코어·패키지·커밋·push 변경 없음.

## 2026-07-17 — Codex — Task 096 새 게임·이어하기 제품형 진입 화면 연결(PARTIAL)

- 기존 `FirstDayPrototypeCanvas`의 맨 앞에 `PROJECT P.A.` 타이틀과 핵심 판타지 문구, `새 게임`, `이어하기` 선택을 추가했다.
- `SaveManager.HasSaveAsync`는 기존 `ISaveRepository.ExistsAsync(SaveKey)`만 노출한다. 저장이 없으면 `저장 없음`으로 비활성, 있으면 기존 `LoadGameAsync`→`RestoreSavedSession`을 호출한다.
- 새 게임은 기존 이름 등록→브리핑→지도→스마트폰→보급→도착 흐름을 그대로 사용하고, 기존 저장을 즉시 삭제하지 않는다. 로드 실패 시 타이틀에 남아 새 게임 선택이 가능하다.
- Runtime/Editor 순차 빌드 오류 0. 타이틀/새 게임/저장 유무/기존 Load·Restore/실패 복귀/결산 닫기/F5·F9 보존 등 상태 전이 15개와 `HasSaveAsync` mutator 부재 검사 PASS.
- Unity는 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 저장 없음/있음 실제 화면과 v10 로드 왕복 확인 전 Task 096은 PARTIAL이다.
- 저장 스키마·SaveKey·삭제·자동 로드·씬·프리팹·Day 1 단계·패키지·커밋·push 변경 없음.

## 2026-07-17 — Codex — Task 097 Pause 메뉴 제품 제어와 안전한 세션 복구(PARTIAL)

- 기존 단일 `PauseManager` 오버레이를 계속하기·게임 저장·저장본 불러오기·저장 후 종료의 네 버튼 제품 메뉴로 확장했다.
- Pause 진입 전에 `Time.timeScale`, 커서 잠금, 커서 표시 상태를 보존하고 Resume/파괴 시 원래 값으로 복구한다. 타이틀 흐름이 끝나기 전에는 Pause를 열지 않는다.
- 저장 존재는 기존 `HasSaveAsync`, 저장/로드는 기존 `SaveGameAsync`/`LoadGameAsync`만 호출한다. 비동기 작업 중 메뉴를 잠그고 예외를 상태 문구로 복구하며 저장 실패 시 종료를 취소한다.
- Runtime/Editor 빌드 오류 0. 네 버튼·상태 복구·저장 존재 Load 게이트·저장 성공 뒤 Quit·저장 권위 격리·예외 처리 정적 계약 11/11 PASS.
- Unity는 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 실제 ESC/버튼 클릭, 1920×1080 가독성, 저장·로드와 빌드 종료 확인 전 Task 097은 PARTIAL이다.
- `SaveManager`/SaveKey/스키마·씬·프리팹·에셋·패키지·커밋·push 변경 없음.

## 2026-07-17 — Codex — Task 098 Day 7 첫 주 완주 요약과 저장 후 선택(PARTIAL)

- 기존 `LongPlayProgressionController`가 Day 7 Settlement를 감지해 `첫 주 운영 완료` 요약을 한 번 표시한다.
- 플레이어 이름, 누적 매출, 보유금, Tier, 평판, 기존 Day 7 판매 정산을 읽어 낮 마을 생활과 밤 상점 운영의 첫 주 완주를 명시한다.
- `저장하고 2주차 계속`은 Day 7 저장→기존 `TryStartNextDay`→Day 8 재저장 순서를 따른다. `저장 후 종료`는 저장 성공 뒤에만 Quit을 호출한다.
- 완주 모달이 열린 동안 배경 간판의 다음 날 상호작용을 차단하고, timeScale/커서를 보존·복구하며 저장/전환 실패 시 모달에서 회복한다.
- Runtime/Editor 빌드 오류 0, 완주 조건·요약 권위·저장 순서·배경 차단·예외 복구 계약 12/12 PASS.
- Unity는 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 실제 Day 7 정산 화면·클릭·1920×1080 가독성·빌드 종료/재실행 확인 전 Task 098은 PARTIAL이다.
- 저장 스키마·경제/Tier/Day 7 수치·씬·프리팹·에셋·패키지·커밋·push 변경 없음.

## 2026-07-17 — Codex — Task 099 실제 입력 기반 시작 조작 안내(PARTIAL)

- 기존 첫날 `StartupStep`의 스마트폰 지급 뒤에 `현장 조작 안내`를 추가했다. 별도 도움말 매니저나 입력 상태는 만들지 않았다.
- 안내는 실제 `PlayerInputHandler`의 이동 WASD/방향키, Space, I/P/C, 1~9/휠, 좌클릭/R/M/X, F5/F9/ESC를 그대로 표시한다.
- 확인 뒤 기존 보급품→도착→Day 1 흐름으로 이어진다. 타이틀/이어하기, Day 1·Day 2+ 상태, 입력 바인딩, 저장 스키마는 변경하지 않았다.
- Runtime/Editor 순차 빌드 오류 0. 단계 순서·실제 키 매핑·읽기 전용 안내·기존 제품 진입 보존 기능 계약 11/11 PASS.
- 첫 정적 스크립트의 PowerShell 자동 변수 충돌과 dirty `PlayerInputHandler`를 HEAD 변경으로 오인한 비기능 계약은 `BUG_LOG.md`에 분리 기록하고 재사용하지 않았다.
- Unity는 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 실제 1920×1080 본문 가독성과 `조작 확인` 클릭 전환 전 Task 099는 PARTIAL이다.
- 씬·프리팹·에셋·패키지·저장·상점/경제/NPC·입력 권위·커밋·push 변경 없음.

## 2026-07-18 — Codex — Task 100 타이틀 게임 종료 경로(PARTIAL)

- 기존 `FirstDayPrototypeCanvas`와 `CreateButton` helper를 재사용해 타이틀 전용 `게임 종료` 버튼을 추가했다.
- 타이틀에서는 종료/이어하기/새 게임이 `-260/0/260` 위치에 나란히 보이고, 다른 온보딩 단계와 Day 1 결산에서는 종료를 숨기고 기존 버튼 배치로 복구한다.
- 이어하기 비동기 로딩 중에는 종료를 잠근다. 실제 빌드는 `Application.Quit`, Editor는 Play Mode를 강제 중단하지 않고 본문 안내를 표시한다.
- 종료 경로는 저장을 호출하지 않는다. 타이틀 새 게임/이어하기, Task099 조작 안내, Pause/Day 7 저장 후 종료는 보존했다.
- Runtime/Editor 순차 빌드 오류 0. 타이틀 전용 가시성·3버튼 배치·비Title 숨김·로딩 잠금·단일 Quit·Editor 안내·저장 비침범 계약 14/14 PASS.
- Unity는 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 실제 1920×1080 화면과 Windows 빌드 종료 확인 전 Task 100은 PARTIAL이다.
- 씬·프리팹·에셋·패키지·저장·경제/NPC·입력 권위·커밋·push 변경 없음.

## 2026-07-18 — Codex — Task 101 기존 저장 보호 새 게임 확인(PARTIAL)

- 기존 `SaveManager.HasSaveAsync` 결과를 재사용해 저장이 있을 때만 `기존 저장 기록 확인` 단계를 표시한다.
- 경고는 새 게임이 저장을 즉시 삭제하지 않지만 이후 저장하면 기존 단일 슬롯을 덮어쓴다고 알린다. 계속은 기존 이름 등록으로, 취소는 타이틀로 돌아가 저장 존재를 다시 조회한다.
- 저장 존재 조회가 끝나기 전에는 `새 게임`을 잠가 비동기 경합으로 확인 단계를 건너뛰지 않게 했다.
- 확인 화면은 저장·불러오기·삭제를 호출하지 않는다. 저장 없음은 이름 등록으로 직행하고 기존 이어하기·게임 종료·Task099 조작 안내를 보존했다.
- Runtime/Editor 순차 빌드 오류 0. 저장 있음/없음 분기·비동기 잠금·승인/취소·HasSave/Load/Quit 보존·저장 비침범 계약 15/15 PASS.
- Unity는 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 저장 있음/없음 실제 클릭, 타이틀 재조회와 1920×1080 경고 가독성 확인 전 Task 101은 PARTIAL이다.
- `SaveManager`/SaveKey/스키마·실제 저장 파일·씬·프리팹·에셋·패키지·커밋·push 변경 없음.

## 2026-07-18 — Codex — Task 102 B09 외부 창고 실제 사용 UI(PARTIAL)

- 메인 씬 바이너리에는 `StorageUI` 컴포넌트가 0개이고 런타임 바인더도 생성하지 않아, B09 `StorageBox.Interact`가 로그 뒤 실제 화면 없이 끝나는 단절을 확인했다.
- 기존 `PA_UIRoot`에 `StorageUI` 한 개를 보장하고, 24칸 6×4 보관 화면에 실제 Item 아이콘·수량·품질·유효 가격을 표시한다.
- 선택 핫바 물품 1개 보관은 원형 Item 기준 전체 차감 대신 선택 슬롯을 직접 1 감소해 동종 상품의 품질/책정 가격 메타를 보존한다. 슬롯 클릭 회수는 기존 `Inventory.AddInstance`를 사용하며 가방이 가득 차면 창고 수량을 보존한다.
- 창고는 기존 인벤토리·스마트폰·제작 패널과 상호배타이며 ESC가 Pause보다 먼저 닫고 열기 전 커서 상태를 복구한다.
- Runtime/Editor 순차 빌드 오류 0. B09 진입·단일 UI·24칸·아이콘/메타·정확 차감·회수 실패 보존·커서/ESC·v10 저장 비침범 계약 14/14 PASS.
- Unity는 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 실제 B09 화면·보관/회수·ESC·v10 저장/로드·1920×1080 가독성 확인 전 Task 102는 PARTIAL이다.
- `StorageBox`/Inventory/Hotbar·B09 모델/프리팹·씬·`SaveData`/`SaveManager`·패키지·커밋·push 변경 없음.

## 2026-07-18 — Codex — Task 103 제작 도감·작업대 UI 제품 흐름(PARTIAL)

- 기존 8개 레시피가 모두 작업대 전용이라 `[C] 제작`이 빈 패널이던 단절을 전체 레시피 읽기 전용 도감으로 교체했다. 필요한 작업대를 카드마다 표시하고 도감의 원격 제작은 차단한다.
- B05~B08 작업대 `[Space]` 진입은 기존 Workbench 컨텍스트와 `CraftingService.TryCraft` 권위를 그대로 사용한다. 카드에 실제 출력 아이콘·출력 수량·전체 재료 보유/필요량·Tier/주민 잠금을 표시하고 성공/실패 뒤 상태와 수량을 갱신한다.
- 창고 UI와 같은 전체 화면 입력 차단 오버레이, 인벤토리·스마트폰·창고 상호배제, 열기 전 커서 복원, ESC의 제작 화면 우선 닫기를 연결했다. 중복 `CraftingUI`가 공유 `PA_RuntimeUI` 전체를 파괴하지 않도록 컴포넌트만 제거한다.
- Runtime/Editor 순차 빌드 오류 0. 기존 CS8785와 Editor CS0414 경고만 유지됐다. 제작 도감·8레시피·작업대 컨텍스트·원격 제작 차단·표시/피드백·입력 차단·커서/ESC·기존 권위 계약 16/16 PASS, `git diff --check` PASS.
- Unity는 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 실제 C 도감·B05~B08 Space 제작·성공/실패 클릭·ESC·1920×1080 가독성 확인 전 Task 103은 PARTIAL이다.
- `CraftingService`·레시피/아이템·B05~B08 모델/프리팹·Inventory/Tier/Friendship·씬·저장·패키지·커밋·push 변경 없음.

## 2026-07-18 — Codex — Task 104 Tripo 장기 정책·B11 분수 충돌 정합(PARTIAL)

- 첨부된 Grid 기반 커스터마이징/Tripo 최종화 지침을 기존 P1~P5 배치 구조 위의 ADR-004~006으로 고정했다. 일괄 교체 금지, 캐릭터 정체성 보존, 기능 가구의 footprint/clearance/interaction·저장 연결, 원본/출처 게이트를 명시했다.
- B11 원형 분수는 시각 모델이 아니라 6×6 루트 `BoxCollider`의 사각 모서리가 보이지 않는 충돌을 만들던 문제였다. 활성 B11의 실제 Visual `MeshFilter`마다 비볼록 정적 `MeshCollider`를 보장하고, 메시 준비 성공 뒤에만 루트 Box를 끈다.
- 실제 Visual 메시가 없으면 기존 Box를 유지한다. 기존 캡슐형 carving `NavMeshObstacle`, B11 실루엣·재질·배치, 메인 씬·래퍼 프리팹·원본 FBX/텍스처는 변경하지 않았다.
- Runtime/Editor 순차 빌드 오류 0. 기존 CS8785와 Editor CS0414 경고만 유지됐다. 호출 순서·중복 방지·실메시·안전 폴백·루트 Box 한정·NavMesh 권위 보존·금지 파일 비침범 계약 12/12와 `git diff --check` PASS.
- Unity는 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 실제 분수 둘레 이동, 보이는 경계 정지, 벤치 접근, NPC 우회와 동일 구도 After 확인 전 Task 104는 PARTIAL이다.
- 새 외부 도구·에셋·서비스·패키지·Blender·커밋·push 변경 없음. B11의 Tripo 생성/상업 이용 증빙은 최종 배포 게이트로 유지한다.

## 2026-07-18 — Codex — Task 105 20~23시 영업 손님 흐름 복구(PARTIAL)

- 실제 NPC 시간표는 19~20시부터 Rest인데 영업은 23시까지라, 20시 이후 외부·실내 손님 후보가 사라지는 밤 루프 단절을 확인했다.
- `NpcScheduleController`에 실제 phase를 바꾸지 않는 Rest 전용 방문 override를 추가했다. 소비 컨트롤러가 있고 현재 Rest일 때만 기존 ShoppingPriority/Resume를 빌리며 Work·Sleep은 깨우지 않는다.
- `CustomerArrivalController`와 `InteriorCustomerController`가 원래 위치·Shop 참조·override 소유를 lease로 기록한다. 방문 완료·시작 실패·timeout·폐점에서 우선순위를 내리고 원위치/Shop/Rest를 복구한다.
- 기존 Day 1 게이트, 동시 손님 상한, `NpcController` FSM, `TryForceShop`/`TryBeginShoppingVisitAt`, `PurchaseEvaluator`와 저장은 변경하지 않았다.
- Runtime/Editor 순차 빌드 오류 0. 기존 CS8785와 Editor CS0414 경고만 유지됐다. Rest/phase/외부·실내/복구/권위 계약 18/18과 `git diff --check` PASS.
- Unity는 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 실제 18:30/20:30/22:30 유입, 동시 상한, 23:00 회수 확인 전 Task 105는 PARTIAL이다.

## 2026-07-18 — Codex — Task 106 Processed 다음 날 변화 실제 에셋 전환(PARTIAL)

- 핵심 마을 변화의 첫 대표였던 `PA_VillageCulture_Processed`가 작업대·상자·보드·배너를 원시 큐브 5개와 런타임 재질로 구성하던 상태를 확인했다.
- `Building_B05_Workbench`의 래퍼 전체가 아니라 `prefab/Visual`만 복제하고, 이미 B05에서 검증된 Project P.A. 목재→판재 준비 키트와 B10 파생 간판의 `가공 준비대` 라벨을 결합했다.
- 기존 B05 정면 보정과 일치하는 Y 180°, 이전 광장 점유 범위에 맞춘 0.58배를 적용했다. Workbench·Collider·Rigidbody·NavMeshObstacle·MonoBehaviour·추가 Light는 비활성/제거한다.
- Processed primitive 생성과 임의 재질 helper를 삭제했다. Raw 실모델 지점, 최신 판매 선택, pending→다음 날, 상호배타, v10 category 저장/복원은 유지했다.
- Runtime/Editor 순차 빌드 오류 0. 기존 CS8785와 Editor CS0414 경고만 유지됐다. 실제 에셋/무기능·무충돌/Raw·저장 권위 계약 18/18과 `git diff --check` PASS.
- Unity는 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 기존 B05 source 캡처는 확인했지만 새 광장 동일 GameCamera After는 없으므로 Task 106은 PARTIAL이다.

## 2026-07-27 — Codex — Task 107 Utility 다음 날 공구 수리대 변화(PARTIAL)

- 실제 `Item_12_ToolSet`은 판매 가능한 Utility이고 `Recipe_ToolSet`이 B07 Forge를 요구함을 데이터로 확인했다.
- 기존 `VillageCultureVisualController`의 판매 감지→pending→다음 DayPreparation→v10 category 문자열 계약에 Utility를 세 번째 실제 변화로 추가했다.
- `Building_B07_BlacksmithForge` 래퍼 전체가 아니라 `prefab/Visual`만 0.44배로 복제하고 Project P.A. 간판에 `공구 수리대`를 표시한다.
- 복제 Collider·Rigidbody·NavMeshObstacle·MonoBehaviour·Light는 제거하며 실제 B07 Workbench·배치·해금, Processed/Raw, 판매/구매/저장 권위는 유지했다.
- 첫 `--no-restore` 빌드는 Unity가 정리한 `Temp/obj` 자산 파일 부재로 중단됐다. 같은 명령을 반복하지 않고 표준 복원 포함 빌드로 Runtime/Editor 오류 0을 확인했다. 기존 CS8785와 Editor CS0414 경고만 유지됐다.
- Utility 상품/레시피/B07, 추적·다음 날·상호배타·힌트, visual-only·v10 보존 정적 계약 23/23 PASS. Unity는 반복 네이티브 렌더 크래시 경계를 지켜 실행하지 않아 Task 107은 PARTIAL이다.

## 2026-07-27 — Codex — Task 108 Luxury 다음 날 공예 전시대 변화(PARTIAL)

- 실제 `Item_11_Furniture`와 `Item_13_Clothes`는 판매 가능한 Luxury이고, 기존 `Recipe_Furniture`/`Recipe_Clothes`가 BasicWorkbench/B08 SewingTable에 연결됨을 데이터로 확인했다.
- 기존 `VillageCultureVisualController`의 판매 감지→pending→다음 DayPreparation→v10 category 문자열 계약에 Luxury를 네 번째 실제 변화로 추가했다.
- `Building_B08_SewingTable` 래퍼 전체가 아니라 `prefab/Visual`만 0.48배로 복제하고 Project P.A. 간판에 `공예 전시대`를 표시한다.
- 복제 Collider·Rigidbody·NavMeshObstacle·MonoBehaviour·Light는 제거하며 실제 B08 Workbench·배치·해금, Processed/Raw/Utility, 판매/구매/저장 권위는 유지했다.
- Runtime/Editor 순차 빌드 오류 0. 기존 CS8785와 Editor CS0414 경고만 유지됐다.
- 첫 정적 감사 2건은 B08 `Visual`의 프리팹 override 직렬화와 선행 dirty 저장/패키지를 잘못 가정한 검사식 false negative였다. 실제 파일을 대조해 검사식을 바로잡은 단일 재검사에서 Luxury 상품/레시피/B08, 추적·다음 날·상호배타·힌트, visual-only·v10 보존 계약 27/27 PASS.
- Unity는 반복 네이티브 렌더 크래시 경계를 지켜 실행하지 않았다. Luxury 판매 당일/다음 날·v10 복원과 동일 GameCamera의 스케일·정면·간판·동선 확인 전 Task 108은 PARTIAL이다.

## 2026-07-27 — Codex — Task 109 채용 후보 제품 흐름(PARTIAL)

- 후보 8명의 역할·비용·프로필은 존재하지만 모든 `spawnPrefab`이 null이라 기존 스마트폰 채용이 실제로 완료될 수 없음을 확인했다.
- `HiringService`는 향후 명시 프리팹을 우선하고, 없을 때 같은 전문 분야의 기존 Producer/Specialist 중 `NpcController`와 실제 SkinnedMesh가 있는 원본만 선택한다. 런타임 채용 인스턴스 ID는 해제 후에도 원본 선택에서 배제한다.
- 신규 채용과 v10 복원이 같은 resolver를 사용한다. 후보 profile/specialty/schedule/dialogue/후보별 친밀도 ID를 주입하고, Specialist에는 해당 WorkbenchType의 기존 `Resources/Recipes`만 정렬해 할당한다.
- 후보·티어·원본·잔액을 기존 `EconomyService.TrySpend` 전에 모두 검사하며 기존 bool overload와 SaveManager 호출 계약을 보존했다.
- `HiringUI`는 `Awake`에서 ScrollView를 먼저 구성해 첫 열기 카드 누락을 막고, 소개·한글 역할·비용·잔액/티어/중복 잠금·성공/실패 footer를 표시한다.
- Runtime/Editor 순차 빌드 오류 0. 기존 CS8785와 Editor CS0414 경고만 유지됐다. 첫 정적 검사 1건은 SaveManager 설명 주석을 쓰기로 오인한 selector false negative였고, 바로잡은 단일 재검사에서 후보·역할·메시·clone·경제·복원·정체성·레시피·UI·보호 경계 계약 36/36 PASS.
- Unity는 반복 네이티브 렌더 크래시 경계를 지켜 실행하지 않았다. 실제 채용·정확한 역할 행동·저장 복원·1920×1080 가독성 확인 전 Task 109는 PARTIAL이다.
- 후보 에셋·씬·프리팹·FBX/텍스처/Avatar·SaveData/SaveManager/SaveKey·경제/티어/NPC FSM·패키지·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 110 첫 주 채용 성장 목표(PARTIAL)

- Task 109 이후에도 Day 5 `LongPlayProgressionController` 계획이 실제 채용을 요구하지 않고, `PlayableDayScenarioController` 체크리스트와 Day 7 첫 주 결산이 고용 상태를 읽지 않던 연결 공백을 확인했다.
- Day 5 이후 낮 목표는 고용 0명일 때 `[P] P.A. Phone` 채용 앱에서 첫 생산자/전문가를 고용하도록 안내한다. 고용 뒤에는 지원 인원수 기반 운영 문구로 전환한다.
- 기존 Day 2+ 생활–상점 체크리스트에 Day 5 이후만 workforce 줄을 추가했다. 실제 후보 이름·한글 역할·인원수를 표시하며 활동·상품 2종·진열/가격·개점·판매/정산 줄은 보존했다.
- LongPlay는 `HiringService.OnHired`를 UI 갱신에만 구독/해제한다. Day 5 계획은 실제 고용 행동으로 바뀌고, Day 7 결산은 `GetHiredCandidates()`를 이름순 정렬해 최대 3명과 `외 N명`을 표시한다.
- Runtime/Editor 순차 빌드 오류 0. 기존 CS8785와 Editor CS0414 경고만 유지됐다. Day 5 경계·0명/1명 이상·identity/8역할·이벤트·결산·기존 루프·읽기 전용 권위 계약 29/29 PASS.
- Unity는 반복 직접 렌더 크래시 경계를 지켜 실행하지 않았다. 실제 Day 5 채용→체크리스트 전환→Day 7 결산과 1920×1080 가독성 확인 전 Task 110은 PARTIAL이다.
- `HiringService`·후보/NPC/레시피 에셋·경제/티어/채용 비용·NPC FSM·저장 스키마/권위·씬·프리팹·FBX/텍스처/Avatar·패키지·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 111 생산자 납품 원자 거래·피드백(PARTIAL)

- `ProducerNpcController`가 돈을 먼저 차감한 뒤 `Inventory.AddItem` 실패 시 NPC 재고까지 삭제해 돈과 상품이 함께 유실되던 기존 경로를 확인했다.
- `Inventory.CanAddInstance`를 추가해 quality/currentPrice가 같은 스택 여유와 빈 슬롯을 변경 없이 계산한다. `AddInstance`는 이 전량 수용 검사를 통과한 뒤에만 스택/전달 인스턴스를 변경하므로 false 반환 시 부분 상품 이동이 없다.
- 생산자는 가방 연결/전량 공간을 결제 전에 검사한다. 가방 가득 참과 잔액 부족은 결제 없이 NPC 재고를 보존하고, 결제 뒤 예상 밖 추가 실패는 기존 `EconomyService.TryModifyMoney`로 전액 환불하며 재고를 유지한다.
- 성공 경로는 원본 `ItemInstance` 메타를 플레이어 재고로 이전한 뒤에만 NPC 재고를 제거한다. 성공·가방 가득 참·매입금 부족·환불은 기존 `NpcBubbleUI`로 표시하며 3.5초 쿨다운으로 반복을 제한한다.
- Runtime/Editor 순차 빌드 오류 0. 기존 CS8785와 Editor CS0414 경고만 유지됐다. 원자성·순서·메타·환불·말풍선·FSM/LongPlay 호환 계약 30/30과 `git diff --check` PASS.
- Unity는 반복 직접 렌더 크래시 경계를 지켜 실행하지 않았다. 실제 Producer의 가방 가득 참 보류→공간 확보→재납품과 말풍선 확인 전 Task 111은 PARTIAL이다.
- `EconomyService`·LongPlay 코드·SaveData/SaveManager/SaveKey·NPC FSM/스케줄/데이터·채용·구매/판매·씬·프리팹·에셋·패키지·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 112 2주차 운영 캠페인(PARTIAL)

- Day 7 완주 뒤 Day 8 전환은 존재했지만 Day 8+는 보관·가공·채용·성장과 무관한 일반 반복 문구만 표시하던 장기 루프 공백을 확인했다.
- `LongPlayProgressionController`에 Day 8 B09 보관, Day 9 가공품 판매, Day 10 채용, Day 11 2카테고리 판매, Day 12 Tier 1, Day 13 다음 날 마을 변화, Day 14 2상품 판매 계획을 추가했다.
- `PlayableDayScenarioController`는 실제 `StorageBox` 보관량, 당일 `SalesLogManager` 기록, `HiringService` roster, `TierService`, `VillageCultureVisualController` 활성 변화를 0.5초 체크리스트에서 읽는다.
- Day 1~7 계획·Day 7 완주 모달/Day 8 저장 전환·기본 생활/상품/진열/가격/개점/판매/정산 체크리스트는 보존했다. 2주차 자동 보급은 추가하지 않았다.
- Runtime/Editor 순차 빌드 오류 0. 기존 CS8785와 Editor CS0414 경고만 유지됐다. Week 1 보존·7개 계획·실제 상태 읽기·당일 범위·표시 전용 권위 계약 40/40과 `git diff --check` PASS.
- Unity는 반복 직접 렌더 크래시 경계를 지켜 실행하지 않았다. Day 7→8과 Day 8~14 대표 상태 전환·1920×1080 가독성 확인 전 Task 112는 PARTIAL이다.
- 경제·인벤토리·판매·제작·채용·Tier·마을 변화·저장 권위, SaveData/SaveManager, 씬·프리팹·에셋·패키지·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 113 Tripo 장기 정책 재감사와 B12 항구 충돌 방지(PARTIAL)

- 최신 Tripo/Grid 지침을 기존 `TRIPO_ASSET_AUDIT`, Placeable 가이드와 ADR에 재대조했다. FBX 174/OBJ 150/GLB 0/Blend 0, Nature Pack 외 고유 FBX 24개가 유지되며 C-01~C-09 `.meta`의 `tripo_node_*`를 직접 출처 추정 근거로 추가했다.
- 이전 물리 감사에서 B12의 10×5m 루트 Box와 실제 약 3.63×1.96m Visual 사이 좌우 약 3.2m 투명 벽이 실측됐고, 원본 래퍼의 box형 `NavMeshObstacle`도 같은 10×5m 범위를 유지함을 확인했다.
- `DemoVisualDressingController`가 활성 map/legacy B12의 Visual 로컬 mesh bounds를 8모서리로 계산해 명백히 큰 `BoxCollider`와 box형 `NavMeshObstacle`만 축소한다. 어떤 축도 키우지 않으며 메시가 없으면 기존 물리를 보존한다.
- B12 원본 FBX·텍스처·래퍼 프리팹·메인 씬·BuildingData·청사진·교역 기능·Placeable·저장·패키지는 변경하지 않았다. 새 외부 도구/에셋/서비스도 없다.
- Runtime/Editor 빌드 오류 0. 기존 CS8785와 Editor CS0414 경고만 유지됐다. B12 물리·안전 폴백·원본/기능 경계 정적 계약 20/20과 대상 `git diff --check` PASS.
- Unity는 반복 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 실제 해안 이동·보이는 경계 정지·NPC carving 우회·동일 GameCamera 확인 전 Task 113은 PARTIAL이다.

## 2026-07-27 — Codex — Task 114 첫 달 운영 캠페인과 Day 30 완주점(PARTIAL)

- Day 14 이후의 일반 반복 목표를 기존 보관·가공·채용·카테고리 판매·Tier·마을 변화에 기반한 Day 15~30 계획 16개로 교체했다.
- `PlayableDayScenarioController`는 보관량, 당일 판매 카테고리/상품, 실제 고용 인원, Tier, 활성 마을 변화만 읽어 각 날짜의 완료 상태를 표시한다. 새 퀘스트·제작·경제·진행 권위를 만들지 않았다.
- Day 30 Settlement에서 첫 달 완주 요약을 표시한다. 누적 매출·돈·Tier·평판·고용 roster·마을 변화·Day 30 정산을 보여 주며, 계속은 Day 30 저장→기존 다음 날 전환→Day 31 재저장, 종료는 저장 성공 뒤 Quit을 사용한다.
- Day 7 자동 보급 종료와 첫 주 모달/Day 8 전환을 별도 경계로 보존하고, 배경 다음 날 입력 차단은 주차/월간 공용 완료 상태를 읽도록 정리했다.
- 첫 병렬 빌드는 공유 Runtime 출력 파일 잠금으로 실패해 `BUG_LOG.md`에 기록했다. 순차 재빌드는 Runtime/Editor 모두 경고 0·오류 0으로 통과했고, 첫 정적 검사 파서 오류도 기록 후 형식 문자열 재검사로 해결해 계약 56/56과 대상 `git diff --check`가 PASS했다.
- Unity는 반복 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 대표 Day 15~30 상태 전환, Day 30 두 버튼, Day 31 이어하기와 1920×1080 가독성 확인 전 Task 114는 PARTIAL이다.
- 저장 스키마/권위, 경제·판매·제작·채용·Tier·마을 변화 권위, 씬·프리팹·에셋·패키지·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 115 Tier 1 대장간·철제 도구 첫 달 가치사슬(PARTIAL)

- B07 BuildingData·설계도, `Recipe_ToolSet`, `Item_12_ToolSet`이 모두 Tier 1인데 `ShopCustomizationController`만 B07을 Tier 3으로 지연하던 권위 불일치를 확인했다.
- B07 배치 태그·최소 Tier·중복 방지 장부 보상을 Tier 1에 정합했다. B05 starter, B06 Tier 2, B08 Tier 3, 기존 10,000G/100,000G Tier 조건은 보존했다.
- Day 23은 활성 B07+정확한 당일 ToolSet+다른 상품, Day 24는 활성 B07+ToolSet+Processed 판매로 바꿔 씨앗 우회와 Tier 2 전 불가능한 Luxury 목표를 제거했다.
- Day 14 15,000G 뒤 Day 15 3,900G로 역행하던 표시 목표를 Day 15 16,000G→Day 30 31,000G→이후 단조 증가로 교정했다. Tier 데이터는 변경하지 않았다.
- Runtime/Editor 순차 빌드 오류 0. 기존 CS8785와 Editor CS0414 경고만 유지됐다. 실행 가능 소스 계약 39/39과 대상 `git diff --check` PASS.
- Unity는 반복 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 실제 B07 보상·배치/접근·제작/판매·Day 23/24 UI·1920×1080 확인 전 Task 115는 PARTIAL이다.
- TierDefinition·레시피/아이템/BuildingData·B07 원본 FBX/프리팹·제작/판매/경제/저장 권위·씬·패키지·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 116 안전 GameView 캡처 기반 1차 전환(PARTIAL)

- `Assets/Editor`의 실제 직접 `camera.Render()` 호출을 전수 감사해 12곳을 확정했다.
- ThemeCorner에서 이미 사용한 일반 GameView `ScreenCapture` 흐름을 공용 `PA_SafeGameViewCapture`로 추출했다. 해상도·Canvas/TMP·안정화·새 PNG/최소 크기 대기·카메라/화면 복원을 포함한다.
- 두 번째 충돌 지점 ShopCustomization과 현재 Tier/B07 ShopProgression 검증기의 직접 렌더를 공용 async/await 경로로 교체했다. 대상 실제 직접 호출은 0이다.
- Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414 경고 2). 안전 캡처 계약 28/28과 대상 diff 검사 PASS.
- 잔여 직접 렌더 10곳이 있어 Unity는 실행하지 않았다. 전수 0과 사람 승인 D3D11 격리 캡처 전 Task 116은 PARTIAL이다.
- 런타임 게임 코드·씬·프리팹·에셋·저장/경제/NPC/배치 권위·패키지·ProjectSettings·그래픽 API·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 117 안전 GameView 캡처 기반 2차 전환(PARTIAL)

- Task 116 뒤 남은 직접 렌더 10곳에서 다음 날 4카테고리 시각 변화, 1920×1080 고객 패널, 최종 프레젠테이션 6장을 담당하는 `PA_VillageCultureVisualValidator`, `PA_CustomerPanelLayoutValidator`, `PA_FinalPresentationReviewer`를 2차 대상으로 선정했다.
- 세 검증기의 단계 진행을 단일 `Task` 가드와 async/await 순서로 전환해 GameView PNG가 실제 기록되기 전에 다음 UI·판매·날짜 상태로 넘어가지 않도록 했다.
- 기존 시장 마커, FOV 46, 전체 레이어, 1920×1080 구도를 공용 `PA_SafeGameViewCapture` 설정 콜백으로 유지했다. VillageCulture/CustomerPanel의 헤드리스 캡처 경고 정책과 FinalPresentation의 일반·희귀 가격 포함 6개 파일 계약도 보존했다.
- 세 파일의 `RenderTexture`·`ReadPixels`·실제 직접 `camera.Render()` 호출은 0이다. 저장소에는 Character/Cottage/DemoView/GatheringShop/OutdoorPlacement/ShopEvolution/Workbench 7곳만 남는다.
- Runtime 빌드는 경고/오류 0, Editor 빌드는 오류 0과 기존 CS8785/CS0414 경고 2개다. 첫 정적 감사의 도우미 복원 변수 선택자 4개가 실제 `previous*` 이름과 달라 34/38로 종료됐고, 실패 선택자를 재사용하지 않고 실제 소스를 다시 읽은 교정 감사는 38/38 PASS했다. 대상 diff 검사도 PASS했다.
- Unity는 같은 네이티브 원인의 세 번째 실행 금지 경계를 지켜 실행하지 않았다. 잔여 7곳 전수 전환과 사람 승인 실제 D3D11 캡처 전 Task 117은 PARTIAL이다.
- 공용 도우미·런타임 게임 코드·씬·프리팹·에셋·저장/경제/NPC/배치 권위·패키지·ProjectSettings·그래픽 API·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 120 안전 GameView 캡처 기반 최종 전환(PARTIAL)

- 저장소에 마지막으로 남은 `PA_ShopEvolutionVisualFinalizer`의 별도 RenderTexture/직접 `camera.Render()` 캡처를 공용 `PA_SafeGameViewCapture`로 전환했다.
- B02~B04의 4방향 소스 감사 12장과 runtime baseline/Tier 1~3 after 파일을 모두 await한다. 기존 1600×900, 초기 4초, 단계별 0.75초, orthographic size 6.6, 외관/플레이어 구도와 파일명을 유지했다.
- 런타임은 단일 `Task` 가드로 단계 완료 뒤에만 다음 Tier로 진행한다. 기존 `ShopCustomizationController.WriteSaveFields` 전후 JSON 동등성 판정도 마지막 캡처 뒤 유지된다.
- 실제 게임 카메라의 `CameraController`와 `clearFlags`는 캡처 동안만 변경하고 `finally`에서 복원한다. 공용 도우미가 transform/projection/culling/viewport/화면 상태를 복원한다.
- 대상의 `RenderTexture`·`ReadPixels`·실제 직접 `camera.Render()`는 0이며, 저장소 전체 실제 직접 호출도 0이다. 남은 문자열 1개는 두 번 발생한 네이티브 충돌을 설명하는 주석이다.
- Runtime 빌드는 경고/오류 0, Editor 빌드는 오류 0과 기존 CS8785/CS0414 경고 2개다. 안전 캡처·성장 단계·복원 계약 36/36 PASS.
- Unity는 같은 네이티브 원인의 세 번째 실행 금지 경계를 지켜 실행하지 않았다. 사람 판단 뒤 격리 D3D11 GameView PNG 1회와 순차 검증 전 Task 120은 PARTIAL이다.
- 공용 도우미·런타임 게임 코드·씬·프리팹·에셋·저장/경제/NPC/배치 권위·패키지·ProjectSettings·그래픽 API·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 119 안전 GameView 캡처 기반 4차 전환(PARTIAL)

- Task 118 뒤 남은 직접 렌더 4곳에서 플레이어/NPC 접지·보행, B10 건축 스케일, B05 작업대 기능 정합을 담당하는 `PA_CharacterFinalizer`, `PA_CottageVisualFinalizer`, `PA_WorkbenchFinalizer`를 4차 대상으로 선정했다.
- Character의 1600×900 소스 lineup과 runtime idle/walk, Cottage의 1920×1080 전경·4방향·최종·runtime 7개 파일, Workbench의 감사/최종 4방향과 runtime baseline/final을 모두 공용 GameView 완료까지 await하도록 전환했다.
- Character의 0.35초 준비·0.6초 실제 이동·0.25초 종료, Cottage의 renderer 격리/복원과 18m·orthographic 5.5 구도, Workbench의 실제 Wood→Plank 제작·작업 방향·콜라이더 계약을 보존했다.
- 실제 게임 카메라를 사용하는 세 흐름은 `CameraController`를 캡처 동안만 비활성화하고 `finally`에서 복원한다. 임시 감사 카메라·조명·바닥과 Cottage renderer 상태도 예외 시 복원한다.
- 세 파일의 `RenderTexture`·`ReadPixels`·실제 직접 `camera.Render()` 호출은 0이다. 저장소에는 `PA_ShopEvolutionVisualFinalizer` 1곳만 남는다.
- Runtime 빌드는 경고/오류 0, Editor 빌드는 오류 0과 기존 CS8785/CS0414 경고 2개다. 안전 캡처 정적 계약 42/42와 대상 공백 검사도 PASS했다.
- Unity는 같은 네이티브 원인의 세 번째 실행 금지 경계를 지켜 실행하지 않았다. 마지막 1곳 전환과 사람 승인 실제 D3D11 캡처 전 Task 119는 PARTIAL이다.
- 공용 도우미·런타임 게임 코드·씬·프리팹·에셋·저장/경제/NPC/배치 권위·패키지·ProjectSettings·그래픽 API·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 118 안전 GameView 캡처 기반 3차 전환(PARTIAL)

- Task 117 뒤 남은 직접 렌더 7곳에서 실제 플레이 카메라 기준 캡처, 낮 채집→밤 판매 검토, 야외 배치·충돌 전후를 담당하는 `PA_DemoViewCapture`, `PA_GatheringShopReview`, `PA_OutdoorPlacementValidator`를 3차 대상으로 선정했다.
- DemoView는 기존 실내 11초/외부 4.5초 준비와 실제 추적 카메라를 유지한 2560×1440 한 장, GatheringShop은 해안과 시장 구도를 오가는 1920×1080 다섯 장, OutdoorPlacement는 같은 직교 구도의 1280×720 전후 두 장을 모두 await한다.
- 캡처 중 기존 `CameraController`만 일시 비활성화하고 `finally`에서 복원한다. OutdoorPlacement의 PNG 최소 크기 판정과 최종 preview 활성화 상태도 보존했다.
- 세 파일의 `RenderTexture`·`ReadPixels`·실제 직접 `camera.Render()` 호출은 0이다. 저장소에는 Character/Cottage/ShopEvolution/Workbench 4곳만 남는다.
- Runtime 빌드는 경고/오류 0, Editor 빌드는 오류 0과 기존 CS8785/CS0414 경고 2개다. 안전 캡처 정적 계약 35/35와 대상 diff 검사도 PASS했다.
- Unity는 같은 네이티브 원인의 세 번째 실행 금지 경계를 지켜 실행하지 않았다. 잔여 4곳 전수 전환과 사람 승인 실제 D3D11 캡처 전 Task 118은 PARTIAL이다.
- 공용 도우미·런타임 게임 코드·씬·프리팹·에셋·저장/경제/NPC/배치 권위·패키지·ProjectSettings·그래픽 API·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 121 Day 31~45 두 번째 달 진입 캠페인(PARTIAL)

- Day 30 완주 뒤 Day 31부터 일반 반복 문구만 남던 장기 루프에 두 번째 달 전반 15일 계획을 추가했다.
- 상단 목표와 기존 0.5초 체크리스트는 B09 보관, Processed 판매, Hiring roster, 상품/카테고리 다양성, 활성 B07과 정확한 ToolSet 판매, 활성 마을 변화, 영업 전 4상품 준비, 누적 매출을 실제 상태에서 읽는다.
- Day 31~45 목표 매출은 기존 단조 수식을 공용으로 읽어 32,500G→53,500G이며, Day 45는 매출 점검선과 당일 4상품 판매를 함께 요구한다.
- Day 1~30·Day 30 완주 UI·Tier 2 100,000G·B06 Tier 2·B08 Tier 3·저장 스키마와 모든 게임플레이 권위를 보존했다.
- Runtime 오류 0/기존 CS8785 경고 1개, Editor 오류 0/기존 CS8785·CS0414 경고 2개. Day 31~45 정적 계약 23/23 PASS.
- Unity는 반복 네이티브 충돌 2회 경계로 실행하지 않았다. 실제 Day 30→31, 대표 Day 35/40/45, 1920×1080 가독성, Day 46 폴백 확인 전 Task 121은 PARTIAL이다.
- 씬·프리팹·에셋·저장 스키마·경제/구매/제작/채용/Tier/마을 변화 권위·패키지·ProjectSettings·커밋·push 변경 없음.

## 2026-08-04 — Codex — Task 130 본사 감사 성공·실패 플레이어 피드백(PARTIAL)

- `AuditService`의 성공·실패는 `_debugLastResult`와 콘솔에만 남고 기존 감사 앱은 다음 감사일까지의 날짜만 표시하는 실제 피드백 단절을 확인했다.
- 기존 감사 판정 뒤 실패·통과·최고 등급·내부 승급 보류 결과를 런타임 상태로 발행하고, 앱이 열린 상태에서는 결과 이벤트로 즉시 갱신한다.
- 감사 앱은 누적 매출·평판·고용의 실제 현재/요구값과 완료/부족, 다음 감사일, 최근 결과, 가장 가까운 다음 행동을 보여 준다. 수동 승인 Tier 진행 바도 같은 세 조건의 부분 진행을 사용한다.
- 500,000G·평판 5·고용 3명·7일 주기, 유일한 `TierService.TryManualAdvance()` 호출, `LastAuditDay` 저장, Tier/경제/채용/씬/프리팹/저장 스키마는 변경하지 않았다.
- Runtime 오류 0/기존 CS8785 경고 1개, Editor 오류 0/기존 CS8785·CS0414 경고 2개. 정적 계약 48/48과 대상 공백 검사 PASS.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트로 실행하지 않았다. 실제 실패/성공 갱신과 1920×1080 가독성 확인 전 프로젝트 판정은 PARTIAL이다.

## 2026-08-04 — Codex — Task 129 Day 106+ 본사 감사·Tier 4 최종 완주(PARTIAL)

- Day 106 이후 일반 운영 폴백을 감사해 정상 플레이 평판이 Tier 2의 3점에서 끝나지만 기존 본사 감사가 평판 5를 요구하는 최종 성장 단절을 확인했다.
- Day 106+ Tier 3에서 전문 주민의 실제 낮 재료 요청을 완료하면 기존 일일 활동 저장 표식으로 감사 요구 평판까지만 하루 1점을 이어 준다.
- 목표와 체크리스트는 `AuditService`가 소유한 실제 500,000G·평판 5·고용 3명 조건과 다음 정기 감사일을 표시한다. 최종 승급은 기존 `AuditService → TierService.TryManualAdvance()`만 수행한다.
- Tier 4 통과 뒤 첫 Settlement에서 기존 완주 UI를 재사용해 전체 캠페인 매출·돈·Tier·평판·고용·마을 변화·당일 정산을 기록하고, 저장 후 다음 날 자유 운영 또는 저장 후 종료를 제공한다.
- 별도 저장 필드 없이 기존 `lastAuditDay`로 저장 후 계속한 완주 확인 상태를 복원한다.
- Runtime 오류 0/기존 CS8785 경고 1개, Editor 오류 0/기존 CS8785·CS0414 경고 2개. 정적 증거는 40개 자동 계약+1개 직접 범위 권위 PASS다.
- 더티 워크트리를 깨끗한 기준선으로 가정한 결합 범위 검사 1개와 Windows 와일드카드 감사 오류는 직접 권위로 분류·복구해 `BUG_LOG.md`에 기록했다.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트로 실행하지 않았다. 실제 평판 4/5·정기 감사·Tier 4·완주 화면·저장/계속·종료 전 프로젝트 판정은 PARTIAL이다.
- `AuditService`, `TierService`, 저장 스키마, 씬, 프리팹, 에셋, 경제·구매 수학, 패키지, ProjectSettings, 커밋, push 변경 없음.

## 2026-08-04 — Codex — Task 128 관광객 손님 정상 플레이 진입(PARTIAL)

- 기존 작성 고객 8명은 모두 유효한 마을 일과표를 가진 주민이고, `[관광객]`은 무일과표 분류 함수와 안내 문구만 있을 뿐 정상 플레이 생성 경로가 없음을 확인했다.
- `CustomerArrivalController`가 Day 2+ 실제 개점 뒤 영업당 최대 2명/동시 1명의 세션 한정 관광객을 만든다.
- 관광객은 기존 주민의 검증된 SkinnedMesh/Avatar 시각만 복제하고 런타임 프로필 이름을 사용한다. 일과표·생산·전문가·대화/친밀도·채용·저장 상태는 만들지 않는다.
- 가게 주변 NavMesh 완전 경로의 진입점에서 기존 `NpcController` 쇼핑 FSM과 `PurchaseEvaluator`로 입장·구매/거절하고, 말풍선을 읽을 시간 뒤 같은 진입점으로 걸어 나가 제거된다.
- `PA_CustomerArrivalValidator`에 실제 관광객 생성, FSM 진입, `[관광객]` 분류, 실제 캐릭터 시각, 역할/프로필 비영속 계약을 추가했다.
- Runtime 오류 0/기존 CS8785 경고 1개, Editor 오류 0/기존 CS8785·CS0414 경고 2개. 정적 계약 46/46과 대상 공백 검사 PASS.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트로 실행하지 않았다. 실제 입장·표시·구매/거절·퇴장·1920×1080 확인 전 프로젝트 판정은 PARTIAL이다.
- 씬·프리팹·원본 캐릭터/프로필·저장 스키마·경제/구매 수학·Tier·패키지·ProjectSettings·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 126 Day 91~105 Tier 3 공동 공방 캠페인(PARTIAL)

- `Tier3.asset`의 실제 조건이 매출 0·평판 3·자동 승인인데 평판을 올리는 플레이 경로가 없음을 확인했다.
- Day 91 이후 전문 주민의 실제 낮 재료 요청 완료를 기존 일일 활동 저장 표식과 연결해 Tier 2 동안 하루 한 번 평판 +1을 지급한다. 세 번째 날에는 기존 `TierService`가 Tier 3로 자동 승급한다.
- Day 91~105에 평판 1/2/3, B08 배치, 정확한 의류/가구 준비·판매, 재단사, 3카테고리/4상품, Luxury 마을 방향, 매출 점검과 최종 가치사슬을 추가했다.
- 새 저장 필드·Tier 수치·레시피·아이템·경제식·자동 제작/판매는 추가하지 않았다.
- Runtime 빌드 오류 0/기존 CS8785 경고 1개, Editor 오류 0/기존 CS8785·CS0414 경고 2개. 정적 계약 24/24와 대상 공백 검사 PASS.
- Unity는 같은 네이티브 원인의 세 번째 실행 금지 경계를 지켜 실행하지 않았다. 실제 주민 요청 3일→Tier 3→B08→의류/가구→Luxury 변화와 UI 확인 전 PARTIAL이다.
- 첨부 GRID/Tripo 지침은 기존 `Docs/Codex/PLACEMENT_SYSTEM_ARCHITECTURE.md`, `CUSTOMIZATION_ROADMAP.md`, `PLACEABLE_ASSET_GUIDE.md`, `TRIPO_ASSET_AUDIT.md`의 장기 권위로 계속 유지한다.

## 2026-08-04 — Codex — Task 127 B05~B08 전문 주민 전면 접근 연결(PARTIAL)

- B05~B08 배치 정의는 footprint와 전면 clearance/interaction 셀을 이미 갖지만, 전문 주민은 `NavMeshObstacle` 내부인 작업대 원점을 목적지로 사용하고 있음을 확인했다.
- `ShopCustomizationController`가 private 배치 권위를 유지한 채 Workbench의 회전된 interaction 셀을 읽기 전용 월드 좌표로 투영하도록 확장했다.
- `SpecialistNpcController`는 같은 타입의 활성 작업대 중 예약되지 않은 전면 셀과 NavMesh 완전 경로를 고르고, 셀 단위 예약/해제·도착 후 정면 보기·이동/회수 시 중단·로드 후 재접근을 수행한다.
- 씬·프리팹·FBX·재질·저장 스키마·레시피·아이템·경제·패키지·ProjectSettings는 변경하지 않았다.
- 이전 continuation의 잘못된 경로 추측 2회·문서 일괄 패치·표식 검사 오류는 `BUG_LOG.md`에 기록했고, 실제 매트릭스 행을 직접 확인해 복구했다.
- Runtime 빌드 오류 0/기존 CS8785 경고 1개, Editor 오류 0/기존 CS8785·CS0414 경고 2개. 접근·예약 정적 계약 29/29와 대상 공백 검사 PASS.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트로 실행하지 않았다. 실제 B05~B08 전문 주민 접근·정면·겹침 방지·제작 확인 전 프로젝트 판정은 PARTIAL이다.

## 2026-07-27 — Codex — Task 123 Day 77~90 Tier 2 주방 가치사슬 캠페인(PARTIAL)

- 기존 B06 주방, `Recipe_Bread`·`Recipe_BakedPotato`·`Recipe_GrilledFish`, 세 출력 Item을 감사해 모두 Kitchen 작업대와 실제 판매 경로에 연결됨을 확인했다.
- Day 77~90에 B06 배치, 세 단일 조리품 판매, 2종/3종 메뉴, 영업 전 3종 준비, Chef 고용, 3카테고리, 주방 상품 4개, Processed 마을 변화와 Day 90 가치사슬 완주 목표를 추가했다.
- 완료 판정은 기존 활성 배치, 인벤토리/핫바/진열, SalesLog, Hiring roster, VillageCulture, Economy 상태를 읽으며 새 퀘스트·보상·저장·자동 행동은 없다.
- Runtime 오류 0/기존 CS8785 경고 1개, Editor 오류 0/기존 CS8785·CS0414 경고 2개. 대상 코드 공백 검사 PASS.
- 정적 계약 검사는 목표/체크리스트 호출부 2개를 3개로 잘못 기대해 중단됐다. 같은 검사식은 재사용하지 않고 `BUG_LOG.md`에 재개 조건을 기록했다.
- Unity는 반복 네이티브 충돌 2회 경계로 실행하지 않았다. 최종 정적 계약과 실제 B06/조리/판매/마을 변화/화면 확인 전 Task 123은 PARTIAL이다.
- 씬·프리팹·에셋·저장 스키마·경제/구매/제작/채용/Tier/마을 변화 권위·패키지·ProjectSettings·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 124 Task 123 주방 캠페인 정적 계약 복구(PARTIAL)

- 이전 검사식의 호출부 기대값을 3에서 실제 목표/체크리스트 2개로 바로잡은 계약은 PASS했다.
- Day 77~90 계획 14개/case 14개, 단일 판정 정의, Day 76 경계, B06 Kitchen 프리팹, 세 Kitchen 레시피, 세 Processed 출력과 요구 리소스 존재 등 44개 계약이 PASS했다.
- B06 최소 Tier C# 표현, 구운 감자·생선구이 Item 이름 YAML 표현 2개가 검사식과 일치하지 않아 결과는 44/47에서 중단됐다.
- 현재 증거만으로 데이터 결함과 검사 표현 불일치를 구분하지 않으며 같은 검사 재시도 없이 `BUG_LOG.md`에 명시 경로 재개 조건을 기록했다.
- 코드·씬·프리팹·에셋·저장·패키지·ProjectSettings·Unity 실행·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 125 B06 Tier·출력 Item 이름 권위 행 감사(DONE)

- 결합 검증을 재실행하지 않고 기록된 세 권위 행만 명시 경로에서 읽었다.
- B06은 `ResolveMinimumTier` switch의 `case "Blueprint_B06_KitchenStation": return 2;`로 정확히 Tier 2다.
- 구운 감자와 생선구이 Item 이름은 Unity YAML Unicode escape로 저장되어 있으며 해석값이 각각 정확히 `구운 감자`, `생선구이`다.
- Task 124의 3개 실패는 실제 데이터 결함이 아니라 `=> 2`와 평문 한글만 허용한 검사식 불일치다.
- 44개 자동 계약+3개 직접 권위 행으로 Task 123 정적 계약 47/47을 확정하고 Task 124를 DONE으로 닫았다.
- 코드·씬·프리팹·에셋·저장·패키지·ProjectSettings·Unity 실행·커밋·push 변경 없음.

## 2026-07-27 — Codex — Task 122 Day 46~76 Tier 2 성장 캠페인(PARTIAL)

- Day 46~75에 보관·가공·지원 인력/상품 준비·3카테고리·B07 철제 가치사슬·마을 방향/4상품·매출 점검의 7단계 운영 리듬을 생성했다.
- 보관 목표는 12→20개, Processed 판매는 2→4건으로 상승하며, 나머지 목표는 기존 Hiring/ShopCustomization/SalesLog/VillageCulture/Economy 상태를 읽기만 한다.
- Day 76은 기존 목표 수식 100,000G와 Tier2.asset의 100,000G 자동 승급이 일치하며 실제 `CurrentTier >= 2`만 완료로 표시한다.
- Day 1~45, Tier 수치, B06 Tier 2/B08 Tier 3, 저장 스키마와 모든 게임플레이 권위를 보존했다.
- Runtime 오류 0/기존 CS8785 경고 1개, Editor 오류 0/기존 CS8785·CS0414 경고 2개. Day 46~76 정적 계약 34/34 PASS.
- Unity는 반복 네이티브 충돌 2회 경계로 실행하지 않았다. 대표 운영 주간·Day 76 자동 승급·1920×1080 가독성·Day 77 폴백 확인 전 Task 122는 PARTIAL이다.
- 씬·프리팹·에셋·저장 스키마·경제/구매/제작/채용/Tier/마을 변화 권위·패키지·ProjectSettings·커밋·push 변경 없음.

## 2026-08-04 — Codex — Task 131 Tripo 장기 정책 재확인·B06 Kitchen 보정(PARTIAL)

- 첨부 GRID 커스터마이징 요구는 기존 2m `GridService`, 상점 실내/마을 야외 P1~P5, footprint/clearance/interaction, NPC 예약 접근, v10 placeable 저장에 이미 구현되어 있어 병렬 시스템을 만들지 않았다.
- FBX 174/OBJ 150/GLB 0/Blend 0과 non-Nature 고유 FBX 24개를 재확인하고 `TRIPO_ASSET_AUDIT.md`의 1~8 분류·캐릭터 정체성 보존·기능 가구 우선·원본 비파괴·출처 게이트를 최신화했다.
- B06은 기존 Visual renderer bounds보다 0.2m 이상 큰 래퍼 Box의 X/Z 축만 0.16m 여유로 줄이고 box형 NavMeshObstacle을 같은 center/size로 정합한다. 어떤 축도 키우지 않으며 메시가 없으면 기존 물리를 유지한다.
- 로컬 `-Z` 전면에 런타임 interaction anchor를 명시하고, 기존 제작 거래가 성공해 결과물이 가방에 들어간 뒤에만 B06 모델 자체가 0.72초/최대 3.5% pulse한다.
- B05 기능 아트, B06 Tier 2/2×2/Kitchen 레시피, NPC 접근·저장, 원본 FBX·프리팹·씬·재질·BuildingData·설계도는 보존했다. 새 외부 에셋·도구·패키지는 없다.
- Runtime 오류 0/기존 CS8785 경고 1개, Editor 오류 0/기존 CS8785·CS0414 경고 2개. 정적 계약 30/30 PASS.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트로 실행하지 않았다. 실제 통로·접근·collider 체감·제작 pulse·동일 GameCamera 전후 확인 전 PARTIAL이다.

## 2026-08-04 — Codex — WORLD-000 절차 섬·셀 테라포밍 아키텍처 재정의(NEEDS HUMAN REVIEW)

- 기존 Task 131 완료 상태와 dirty baseline을 감사하고 선행 `AudioManager.cs`/`SalesLogManager.cs` 미검증 변경을 수정·검증·롤백 없이 보존했다.
- 현행 2m grid/build/outdoor placement, v10 save/repository, 생활 활동, 고정 NPC/shop Transform, primitive fixed map과 NavMesh 구조를 실제 코드·직렬화 필드·로컬 AI Navigation 2.0.12 source로 조사했다.
- Unity Terrain/custom chunk mesh/voxel을 비교해 2m cell, 16×16 Chunk, 1m 높이 0~6의 custom chunk mesh heightfield를 권장안으로 정했다.
- seed+generationVersion+sparse delta, atomic building placement/move, `WorldAnchorRegistry`, cell graph+per-Chunk surface navigation, legacy v10 보존과 adapter migration을 설계했다.
- `PROJECT_PA_WORLD_NORTH_STAR.md`와 `Docs/WorldArchitecture/`의 plan/impact/backlog/ADR 5개 신규 문서를 만들고 WORLD-001~012 및 별도 MainGame integration gate를 정의했다.
- `Prototype_FirstDay`는 Golden Regression Scene, 미래 `WorldSandbox`는 기술 testbed, `MainGame`은 Gate 1~5 뒤 별도 통합 후보로 기록했다.
- 지정된 기존 방향·상태·규칙·loop 문서만 최소 갱신했다. 게임 코드/씬/에셋/저장 스키마/Packages/ProjectSettings/Unity/Git history는 WORLD-000에서 변경하지 않았다.
- 후속 scene/baseline/save/nav/MainGame 사람 Gate 때문에 최종 상태는 `needs_human_review`; WORLD-001은 시작하지 않았다.
