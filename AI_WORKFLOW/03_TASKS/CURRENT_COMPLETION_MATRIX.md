# CURRENT_COMPLETION_MATRIX — 9898f6a 기준 구현 증거 감사

작성: 2026-07-13 (Codex)
기준 커밋: `9898f6a`

판정 원칙: 문서의 기존 체크 표시는 증거로 사용하지 않고 코드·에셋·Git 이력·실행 로그를 대조했다. `PARTIAL`은 일부 구현 또는 검증만 존재하나 원 Task의 완료 조건을 모두 충족하지 못한 상태다.

## 집계

| DONE | PARTIAL | TODO | BLOCKED | DECISION_REQUIRED | 합계 |
|---:|---:|---:|---:|---:|---:|
| 46 | 61 | 2 | 4 | 18 | 131 |

갱신 2026-07-13 (Fable 5): Task 019 TODO→DONE(`f6cbbe1`), Task 057 DECISION_REQUIRED→DONE(`71fa710`, 사용자 세션 지시로 v9 승인).

갱신 2026-08-04 (Codex): Task 127~130 PARTIAL 4건과 Task 131 PARTIAL을 집계에 반영. Task 131은 기존 GRID/Tripo 장기 정책을 재대조하고 B06 Kitchen의 기존 Visual 기반 축소 전용 물리/Carving, 전면 anchor, 성공 제작 모델 pulse를 구현했다. Runtime/Editor 오류 0, 정적 계약 30/30 PASS, Unity 실제 화면은 사람 판단 게이트로 대기한다.

갱신 2026-07-15 (Codex): Task 039 PARTIAL→DONE(실제 `IInteractable` 낚시), Task 040 TODO→PARTIAL(대기/성공 프롬프트 구현·자동 확인, 실제 시간 경과 체감은 미확인).

갱신 2026-07-15 (Codex): Task 041 PARTIAL→DONE(실제 어획 Fish 2개 중 1개를 진열·가격 확정→Fisher_01 구매→18G 입금·매출/통계 기록).

갱신 2026-07-15 (Codex): Task 042 PARTIAL→DONE(`SMOKE_CHECKLIST.md` 신규, D3D11 자동 PASS와 미확인 사람 체감 절차 분리).

갱신 2026-07-15 (Codex): Task 043 TODO→DONE(`DESIGN_MINING_FARMING.md` 신규, 광질 우선 단일 슬라이스와 농사 복구/저장 경계 확정).

갱신 2026-07-17 (Codex): Task 044 TODO→DONE(`DESIGN_RESIDENT_REQUEST.md` 신규, 전문가 레시피 재료 기반의 비퀘스트엔진 요청 표시·일일 완료·납품 경계 확정).

갱신 2026-07-17 (Codex): Task 045 PARTIAL→DONE(`DAYTIME_ACTIVITIES.md` 신규, 현재 6개 일일 재고 원천과 Fish/Ore 낮→밤 판매 증거를 `PROJECT_PA_GAME_LOOP.md`에 동기화).

갱신 2026-07-17 (Codex): Task 092 PARTIAL 추가(Raw 실제 판매→다음 날 생산자 보관·수거 실모델 사이드카 구현, Unity GameCamera 확인 대기).

갱신 2026-07-17 (Codex): Task 093 PARTIAL 추가(Fish/생선구이/목제 가구 성공 판매의 당일 낚시·가구 명명 트렌드와 결산 요약 구현, 컴파일·정적 계약 PASS, Unity 결산 가독성 대기). 실제 낚시→판매와 Raw 다음 날 변화까지 존재하므로 Task 069 BLOCKED→PARTIAL로 정합화.

갱신 2026-07-17 (Codex): Task 046 PARTIAL→DONE(성공 거래 단위·최근 40건 카테고리 집계·다음 날 변화·저장 경계를 `VILLAGE_TREND.md`에 확정).

갱신 2026-07-17 (Codex): Task 047 PARTIAL→DONE(Fish/생선구이/목제 가구 명명 트렌드 매핑, 판매 전용 점수, 일차/7일 창과 동점 계약 설계).

갱신 2026-07-18 (Codex): Task 104 PARTIAL 추가(Tripo/Placeable 장기 ADR 고정, B11 원형 분수의 사각 루트 충돌을 실제 Visual mesh collider로 정합. 빌드·계약 PASS, 실제 이동/동일 구도 캡처 대기).

갱신 2026-07-18 (Codex): Task 105 PARTIAL 추가(Rest 단계 주민만 영업 방문을 일시 허용하고 외부·실내 초대의 성공/실패/timeout/close 뒤 원래 위치·Shop·Rest를 복구. Runtime/Editor 오류 0, 계약 18/18 PASS, 실제 18:30/20:30/22:30·23:00 확인 대기).

갱신 2026-07-18 (Codex): Task 106 PARTIAL 추가(Processed 다음 날 변화의 원시 큐브 5개를 제거하고 B05 실제 Visual·Project P.A. 준비 키트·간판의 비충돌 시각 전용 지점으로 전환. Runtime/Editor 오류 0, 계약 18/18 PASS, 동일 GameCamera 확인 대기).

갱신 2026-07-27 (Codex): Task 107 PARTIAL 추가(실제 `철제 도구` Utility 판매를 기존 pending→다음 날/v10 category 계약에 연결하고 B07 래퍼가 아닌 실제 Visual·Project P.A. `공구 수리대` 간판의 비충돌 시각 지점으로 전환. Runtime/Editor 오류 0, 계약 23/23 PASS, 동일 GameCamera 확인 대기).

갱신 2026-07-27 (Codex): Task 116 PARTIAL 추가(직접 `Camera.Render()` 12곳 감사, 공용 `ScreenCapture` GameView 도우미 추출, 알려진 충돌 ShopCustomization과 현재 ShopProgression 1차 전환. Runtime/Editor 오류 0, 계약 28/28 PASS. 잔여 직접 렌더 10곳 전환과 사람 승인 D3D11 확인 대기).

갱신 2026-07-27 (Codex): Task 117 PARTIAL 추가(VillageCulture·CustomerPanelLayout·FinalPresentation의 직접 렌더 3곳을 공용 async GameView 캡처로 2차 전환. Runtime/Editor 오류 0, 계약 38/38 PASS. 잔여 직접 렌더 7곳 전환과 사람 승인 D3D11 확인 대기).

갱신 2026-07-27 (Codex): Task 118 PARTIAL 추가(DemoView·GatheringShop·OutdoorPlacement의 직접 렌더 3곳을 공용 async GameView 캡처로 3차 전환. Runtime/Editor 오류 0, 계약 35/35 PASS. 잔여 직접 렌더 4곳 전환과 사람 승인 D3D11 확인 대기).

갱신 2026-07-27 (Codex): Task 119 PARTIAL 추가(Character·Cottage·Workbench의 직접 렌더 3곳을 공용 async GameView 캡처로 4차 전환. Runtime/Editor 오류 0, 계약 42/42 PASS. 잔여 ShopEvolution 직접 렌더 1곳 전환과 사람 승인 D3D11 확인 대기).

갱신 2026-07-27 (Codex): Task 120 PARTIAL 추가(마지막 ShopEvolution 직접 렌더를 공용 async GameView 캡처로 최종 전환. Runtime/Editor 오류 0, 계약 36/36 PASS, 저장소 실제 직접 호출 0. 사람 판단 뒤 격리 D3D11 GameView 확인 대기).

갱신 2026-07-27 (Codex): Task 108 PARTIAL 추가(기존 `목제 가구`/`의류` Luxury 판매를 pending→다음 날/v10 category 계약에 연결하고 B08 래퍼가 아닌 실제 Visual·Project P.A. `공예 전시대` 간판의 비충돌 시각 지점으로 전환. Runtime/Editor 오류 0, 계약 27/27 PASS, 동일 GameCamera 확인 대기).

갱신 2026-07-27 (Codex): Task 109 PARTIAL 추가(스마트폰 후보 8명의 빈 `spawnPrefab` 실패를 같은 전문 분야의 기존 C-02~C-09 주민 구성으로 안전 해소하고, 신규 채용·v10 복원·후보 정체성·전문가 레시피·UI 상태 피드백을 연결. Runtime/Editor 오류 0, 수정된 계약 36/36 PASS, 실제 채용/역할 행동/저장 복원 확인 대기).

갱신 2026-07-27 (Codex): Task 110 PARTIAL 추가(Task 109의 실제 채용을 Day 5 이후 목표·운영 체크리스트와 Day 7 첫 주 결산에 연결하고 고용 이벤트로 즉시 갱신. Runtime/Editor 오류 0, 계약 29/29 PASS, 실제 Day 5 채용→Day 7 결산·1920×1080 확인 대기).

갱신 2026-07-27 (Codex): Task 111 PARTIAL 추가(`Inventory.AddInstance` 실패의 부분 이동을 전량 수용 선검사로 차단하고, 생산자 납품을 공간 확인→결제→메타 이전→예외 환불 순서로 복구. 가방/잔액 보류와 성공을 기존 말풍선으로 표시. Runtime/Editor 오류 0, 계약 30/30 PASS, 실제 납품 왕복 확인 대기).

갱신 2026-07-27 (Codex): Task 113 PARTIAL 추가(Tripo/Placeable 장기 정책 재대조, B12의 역사적 10×5m Box/Obstacle을 실제 Visual bounds로 축소 전용 정합. Runtime/Editor 오류 0, 계약 20/20 PASS, 실제 해안 이동/NPC 우회·동일 GameCamera 확인 대기).

갱신 2026-07-27 (Codex): Task 114 PARTIAL 추가(Day 15~30 보관·가공·고용·상품 구성·Tier·마을 변화 계획과 실제 상태 체크리스트, Day 30 첫 달 완주 요약, 저장→Day 31/저장→종료 연결. Runtime/Editor 경고 0·오류 0, 계약 56/56 PASS, 실제 대표 일차·완주 UI·Day 31 확인 대기).

갱신 2026-07-27 (Codex): Task 115 PARTIAL 추가(B07의 BuildingData·설계도·ToolSet 레시피/상품 Tier 1과 배치 Tier 3 불일치를 Tier 1 장부 보상으로 정합화. Day 23/24를 활성 Forge+정확한 ToolSet+다른 상품/Processed 판매로 교체하고 Day 14 뒤 매출 목표 역행 제거. Runtime/Editor 오류 0, 기존 기준 경고만 유지, 실행 가능 소스 계약 39/39 PASS, 실제 배치·제작·판매 확인 대기).

갱신 2026-07-27 (Codex): Task 121 PARTIAL 추가(Day 31~45 두 번째 달 진입 계획과 기존 보관·가공·채용·Forge/ToolSet·구성·마을 변화·매출 실제 상태 체크리스트 구현, 컴파일·정적 계약 PASS, Unity 상태 전환/가독성 대기).

갱신 2026-07-27 (Codex): Task 122 PARTIAL 추가(Day 46~76을 7단계 지역 경제 운영 리듬으로 연결하고 기존 100,000G 자동 Tier 2 승급을 Day 76 완료 조건으로 구현. 컴파일·정적 계약 PASS, Unity 대표 일차/Tier 승급/가독성 대기).

갱신 2026-07-27 (Codex): Task 123 PARTIAL 추가(Day 77~90 B06 주방·기존 세 조리품·Chef·Processed 마을 변화 캠페인 구현, Runtime/Editor 오류 0. 정적 검사식이 호출부 2개를 3개로 잘못 기대해 중단됐고 Unity 미실행).

갱신 2026-07-27 (Codex): Task 124 PARTIAL 추가(Task 123 호출부 2개 계약 복구 및 계획/case·Kitchen/Processed 핵심 44개 PASS. B06 최소 Tier와 두 출력 Item 이름 표현 3개 미확정으로 44/47 중단).

갱신 2026-07-27 (Codex): Task 125 DONE 추가(B06 `return 2` switch case와 두 Unity YAML Unicode-escaped Item 이름을 직접 확인해 검사 표현 불일치로 확정). Task 124 PARTIAL→DONE, Task 123 정적 계약 47/47 확정.

갱신 2026-07-17 (Codex): Task 051 TODO→DONE(성공 판매의 선도 카테고리를 감사 앱 시설 방향 예고로 표시하고 실제 해금 권한은 Tier/감사 조건에 보존).

갱신 2026-07-17 (Codex): Task 052 TODO→DONE(`DESIGN_EVENTS.md` 신규, 실제 Fish 판매가 다음 날 여는 해변 풍어제의 상태·전체 루프·저장/승인·검증 계약 설계).

갱신 2026-07-17 (Codex): Task 090 신규→PARTIAL(Day 2+ 실제 활동·상품 2종·진열/가격·개점·판매·정산 체크리스트 구현, 컴파일/정적 계약 PASS, Unity 가독성 확인 대기).

갱신 2026-07-17 (Codex): Task 091 신규→PARTIAL(모든 기존 레시피의 결과 메타/차감 후 슬롯 선검사로 가방 가득 참 재료 소실 차단, 컴파일/정적 계약 PASS, Unity 실제 분기 대기).

갱신 2026-07-17 (Codex): Task 086 신규→PARTIAL(테마 코너 런타임·장부·월드 라벨·전용 검증기 구현 및 Unity 컴파일 PASS. 첫 D3D11 검증은 빈 상태까지 PASS 후 직접 `Camera.Render()` 네이티브 크래시로 중단).

갱신 2026-07-17 (Codex): Task 086 PARTIAL 유지(전용 D3D11 기능 assertions와 v10 재파생 PASS. 기존 ShopCustomization 회귀의 직접 `Camera.Render()`가 같은 네이티브 충돌을 두 번째로 재현해 세 번째 실행 금지; 전체 회귀·최종 기본 화면 시각 검토 미완).

갱신 2026-07-17 (Codex): Task 054 TODO→DONE(현재 v10 기준 최근 판매·일차 판단·카테고리·7일 명명 트렌드의 v11 추가 확장, 마이그레이션·복원·검증 계약 설계).

갱신 2026-07-17 (Codex): Task 023 TODO→DONE(`ShopPriceUI`에 100G 기준 `가격 파생값` 일반/희귀 구분과 실제 `ItemInstance.quality` 표시, 일반·희귀 동일 카메라 캡처 및 D3D11 회귀 PASS).

갱신 2026-07-17 (Codex): Task 025 PARTIAL→DONE(`PurchaseEvaluator`와 같은 basePrice+quality 추천 기준가를 읽기 전용 표시, 30G 일반/165G→186G 고품질 분기·비자동적용 캡처와 D3D11 회귀 PASS).

갱신 2026-07-17 (Codex): Task 031 TODO→DONE(실제 마을 일과표 유무로 `[주민]/[관광객]` 표시만 파생, 현재 8명 주민과 기본 플레이 말풍선 태그·D3D11 30G 회귀 PASS).

갱신 2026-07-17 (Codex): Task 024 TODO→DONE(`DESIGN_THEME_CORNER.md` 신규, 실제 `ShopSlot` placement의 같은 카테고리 4방향 인접·2칸 이상 판정, `shop.theme` 충돌 방지, 저장/구매 수학 무변경 후속 구현 계약).

갱신 2026-07-17 (Codex): Task 087 신규→DONE(Tripo 추정 에셋의 전수 수량·바이너리 메인 씬·GUID/Resources/코드 사용처, B06~B08 기능/격자, B11/B12 정적 역할, 원본/라이선스/배포 게이트를 `TRIPO_ASSET_AUDIT.md`와 출처 대장에 정합화).

갱신 2026-07-17 (Codex): Task 088 신규→PARTIAL(전문 주민의 실제 레시피 재료 요청·보유량 프롬프트·정확 수량 전달·당일 완료 저장 문자열·친밀도 보상 구현 및 컴파일/정적 계약 PASS, Unity 실제 플레이 검증은 직접 렌더 충돌 2회 경계로 대기).

갱신 2026-07-17 (Codex): Task 089 신규→PARTIAL(Seed→Crop→Wheat 참조, 고정 밭 2칸, 낮 심기/성장/안전 수확, 실제 Wheat 시각, Runtime/Editor와 정적 계약 PASS. Unity 실플레이 및 F3 날짜 성장/저장 대기).

갱신 2026-07-17 (Codex): Task 094 신규→PARTIAL(Tripo 8분류·캐릭터 정체성 보존·Placeable/출처 게이트를 장기 지침에 통합하고 라이선스 미확인 Froggy Chair 런타임 생성 2곳 제거. Runtime/Editor와 정적 계약 PASS, GameCamera 확인 대기).

갱신 2026-07-17 (Codex): Task 095 신규→PARTIAL(`Recipe_Furniture`·Inventory·B05·Tier·ShopSlot·당일 SaleRecord를 읽는 다음 행동 안내를 Day 4+ 체크리스트에 연결. Runtime/Editor와 읽기 전용 정적 계약 PASS, Unity 가독성/전체 왕복 대기). 이 연결로 Task 070 BLOCKED→PARTIAL.

갱신 2026-07-17 (Codex): Task 096 신규→PARTIAL(기존 첫날 패널 앞에 PROJECT P.A. 타이틀·새 게임·저장 존재 기반 이어하기를 연결하고 `LoadGameAsync` 복원 권한 보존. Runtime/Editor 오류 0, 상태 전이 15개와 mutator 부재 PASS, Unity 화면/로드 왕복 대기).

갱신 2026-07-17 (Codex): Task 097 신규→PARTIAL(ESC 메뉴에 계속하기·저장·저장본 불러오기·저장 후 종료를 연결하고 이전 timeScale/커서를 복원. Runtime/Editor 오류 0과 상태·저장 권위 계약 11/11 PASS, 실제 클릭/가독성/종료 확인 대기).

갱신 2026-07-17 (Codex): Task 098 신규→PARTIAL(Day 7 Settlement에 첫 주 성과 요약과 저장→Day 8/저장→종료 선택을 연결. Runtime/Editor 오류 0과 완주·저장 순서 계약 12/12 PASS, 실제 화면/클릭/빌드 종료 확인 대기).

갱신 2026-07-17 (Codex): Task 099 신규→PARTIAL(기존 첫날 시작 흐름에 실제 `PlayerInputHandler` 바인딩 기반 조작 안내를 연결. Runtime/Editor 오류 0과 입력·단계·기존 흐름 계약 11/11 PASS, 실제 1920×1080 가독성/클릭 확인 대기).

갱신 2026-07-18 (Codex): Task 100 신규→PARTIAL(기존 타이틀 Canvas에 저장 여부와 무관한 `게임 종료`를 추가하고 새 게임/이어하기/종료 3버튼 배치·로딩 잠금·Editor 안내를 연결. Runtime/Editor 오류 0과 상태 계약 14/14 PASS, 실제 화면/빌드 종료 확인 대기).

갱신 2026-07-18 (Codex): Task 101 신규→PARTIAL(기존 저장이 확인된 새 게임에 덮어쓰기 경고·취소 복귀를 연결하고 저장 확인 중 새 게임을 잠금. Runtime/Editor 오류 0과 저장 보호 계약 15/15 PASS, 실제 저장 있음/없음 화면·클릭 확인 대기).

갱신 2026-07-18 (Codex): Task 102 신규→PARTIAL(메인 씬에 없던 `StorageUI`를 런타임 바인더로 보장해 B09 24칸 보관/회수 화면과 정확한 선택 핫바 스택 차감을 연결. Runtime/Editor 오류 0과 창고 기능 계약 14/14 PASS, 실제 화면·저장 왕복 확인 대기).

갱신 2026-07-18 (Codex): Task 103 신규→PARTIAL(모든 기존 레시피가 작업대 전용이라 비어 있던 C 패널을 8개 제작 도감으로 교체하고, 작업대별 실제 카드·재료/출력·결과 피드백·ESC/커서/UI 차단을 연결. Runtime/Editor 오류 0과 제작 기능 계약 16/16 PASS, 실제 화면·클릭 확인 대기).

## Task별 판정

| Task | 판정 | 증거 파일 | 검증 증거 | 남은 작업 / 다음 행동 | 승인 |
|---|---|---|---|---|---|
| 001 | DONE | `PROJECT_CODE_INDEX.md` | 2026-07-10 스크립트 100개 대조 | 현재 101개로 늘어 후속 갱신만 필요 | NO |
| 002 | DONE | `DEMO_FLOW.md`, `PlayableDayScenarioController.cs` | FinalRoute PASS | Final Locked 루트와 문구 동기화는 후속 | NO |
| 003 | DONE | `BASELINE_COMPILE.md` | `daa57ad`, 현재도 오류 0 | 기준 경고만 유지 | NO |
| 004 | DONE | FinalRoute/LongPlay 검증기 | `Logs/Fable_V3_FinalDemoRoute.log`, `Logs/Fable_VisualPass_LongPlayRegression.log` PASS | BUG/검증 문서의 오래된 BLOCKED 표기 제거 | YES→실행 승인 완료 |
| 005 | PARTIAL | `HANDOFF_FOR_CODEX.md` 위험 파일 표 | 실제 파일 존재 대조 | 전용 `RISK_FILES.md` 미작성 | NO |
| 006 | PARTIAL | `CODEX_WORKER_RULES.md` §7 | 현재 ZIP 삭제만 dirty | 전용 `GIT_POLICY.md` 미작성 | NO |
| 007 | DONE | `SAVE_SCHEMA.md`, Save v8 코드 | 최상위 필드·DTO·v0→v8 체인 실제 이름 대조 | 실제 저장소 왕복은 Task 011 | NO |
| 008 | DONE | Player 3종, CharacterController | FinalRoute PASS, CoreSlice PASS | 실제 손맛은 사람 확인 | NO |
| 009 | PARTIAL | Inventory/Hotbar 코드 | FinalRoute의 핫바→진열, DayNight 재고 증가 PASS | 드래그·전체 InventoryUI 실플레이 미검증 | NO |
| 010 | DONE | Shop/ShopSlot/ShopPriceUI/Economy | FinalRoute: 진열·가격·구매·30G PASS | 없음 | NO |
| 011 | DONE | `PA_SaveRoundTripValidator.cs`, Save v8 | 실제 격리 저장소 왕복 PASS: 돈/매출/위치/시간/인벤토리/핫바/진열/가격/Day Prep/Day 1 단계 | 사람 F5/F9 체감 확인만 선택 사항 | NO |
| 012 | PARTIAL | D3D11 규칙이 Worker/Handoff에 존재 | 모든 최근 Unity 검증 D3D11 PASS | `UNITY_LAUNCH_POLICY.md` 미작성 | NO |
| 013 | PARTIAL | `Logs/Fable_V3_*.log` | 핵심 5종 오류 없이 종료 | `CONSOLE_SNAPSHOT.md` 미작성 | NO |
| 014 | PARTIAL | `FINAL_HUMAN_CHECKLIST.md` | CoreSlice/FinalPresentation PASS | 전용 `SMOKE_CHECKLIST.md` 미작성 | NO |
| 015 | DONE | Visual pass의 요약 잘림·화면 결함 수정 | FinalPresentation 410/418 PASS | 새 버그는 별도 Task | 조건부 |
| 016 | DECISION_REQUIRED | Stage 0 증거 다수 | 자동 검증 통과, 저장 왕복/사람 가독성 미완 | 사람의 MVP Stabilization 판정 필요 | YES |
| 017 | PARTIAL | `Item.cs` ItemCategory, Village signal 의미표 | 코드 enum 대조 | `PRODUCT_CATEGORIES.md` 미작성 | NO |
| 018 | DONE | `ShopPriceUI.cs`, FinalPresentation assertion | 동일 카메라 Before/After + FinalPresentation/FinalRoute/DayNight/PanelLayout PASS | 다중 슬롯 합계가 아닌 현재 슬롯 수량 표시 | NO |
| 019 | DONE | `ShopSlot.cs` 품절 라벨/프롬프트, `PA_ShopSoldOutValidator.cs` | 전용 검증기 + FinalRoute/DayNight/CoreSlice PASS (`f6cbbe1`) | 없음 (품절은 런타임 전용이 의도) | NO |
| 020 | DONE | `DayNightShopLoopController` 한국어 페이즈/HUD | DayNight PASS | 없음 | NO |
| 021 | DONE | Day 요약: 매출·판매 수·피드백·다음 목표 | FinalPresentation 410/418 PASS | `Village direction` 한국어화 후보 | NO |
| 022 | PARTIAL | ShopSlot→Economy Deposit 사유 로그 | FinalRoute 30G PASS | 단가×수량 계산 로그 명료화 미완 | NO |
| 023 | DONE | `ShopPriceUI` 가격 파생값+실제 quality | FinalPresentation 일반/희귀 assertion·2장 캡처, FinalRoute 30G PASS | 임시 100G 경계는 실제 rarity 데이터 도입 시 교체 | NO |
| 024 | DONE | `DESIGN_THEME_CORNER.md`, 실제 ShopSlot/placement/category/판매 신호 감사 | 기존 구조 참조·판정/저장/검증 계약 정적 검사 PASS | 기능은 미구현. 별도 단일 작업에서 런타임 코너 판정·표시·D3D11 검증 | NO |
| 025 | DONE | `ShopPriceUI` basePrice+quality 추천 기준가 | FinalPresentation 30G/186G·비자동적용 assertion와 2장 캡처, FinalRoute 30G PASS | NPC별 성향은 기존 예상 구매율/실제 평가에서 처리 | NO |
| 026 | PARTIAL | DayNight/CoreSlice 검증기 | 둘 다 최근 PASS | 018/019 미완이라 Phase 2 완료 검증 아님 | NO |
| 027 | DONE | `CustomerPreferencePresentationController.cs` | CustomerPresentation PASS 이력 | 없음 | NO |
| 028 | DONE | Npc bubble 확률 %, 가격 UI 예상 구매율 | FinalRoute 피드백 70% PASS | 없음 | NO |
| 029 | PARTIAL | 가격/선호 사유별 문구 분기 | FinalRoute 구매 문구 PASS | 고가 거절 문구 2~3종 랜덤화 미완 | NO |
| 030 | DONE | Preference/DemandInsight 기존 데이터 힌트 | DemandInsight PASS 이력 | 없음 | NO |
| 031 | DONE | `CustomerPreferencePresentationController`, `NpcBubbleUI`의 일과표 기반 `[주민]/[관광객]` 표시 | CustomerPresentation 8명 주민/관광객 폴백, PanelLayout 태그 캡처, FinalRoute 30G PASS | 실제 관광객 생성 콘텐츠는 아직 없으며 현재 주민을 임의로 관광객 처리하지 않음 | NO |
| 032 | PARTIAL | CustomerArrival/Npc FSM 구현 | Arrival 검증 PASS 이력 | `CUSTOMER_FLOW.md` 미작성 | NO |
| 033 | DECISION_REQUIRED | 유입 간격 코드 존재 | 기본 흐름 검증됨 | 밸런스/Inspector 값 사람 결정 | YES |
| 034 | DONE | SalesLogManager 일일 구매/거절 판단 집계 + 정산/다음날 조언 | CustomerDemandInsight 전용 D3D11 검증 PASS(구매 1/거절 1/구매율 50%) | 런타임 전용, 저장 미지원(Task 055) | NO |
| 035 | DONE | Arrival/Presentation/Demand 검증기 | Visual pass에서 3종 PASS 이력 | 없음 | NO |
| 036 | PARTIAL | 채집/Crop/Farmland/Workbench 코드, 실제 FishingSpot | GatheringShopGate 낚시 경로 PASS | `DAYTIME_ACTIVITIES.md` 미작성, 광질/농사 생활 루프 미연결 | NO |
| 037 | DONE | 5개 DaytimeStockPrepPoint, 일별 수집 기록 | GatheringShopGate PASS | 없음 | NO |
| 038 | TODO | Fish 아이템은 존재 | 낚시 설계 문서 없음 | 두 번째 낮 활동 결정/설계 | NO(방향은 사람) |
| 039 | DONE | `FishingSpot.cs`, shore-forage 런타임 자식 상호작용·낚시 외형 | D3D11 GatheringShopGate: 캐스팅→Fish 2개→일일 제한/리셋 PASS, FinalRoute PASS | 없음 | NO |
| 040 | PARTIAL | 캐스팅 대기·입질·성공 프롬프트/상태 | 검증기가 캐스팅 진입과 즉시/성공 피드백 확인 | 실제 1.25초 시간 경과와 코지 체감은 사람 확인 필요 | NO |
| 041 | DONE | 실제 어획 Fish를 합성 주입 없이 `ShopSlot` 진열·가격·NPC 구매 경로에 사용 | D3D11 GatheringShopGate: Fish 2→1 진열, Fisher_01 구매 18G, 돈/누적매출/SalesLog/일일 통계 PASS | 없음 | NO |
| 042 | DONE | `SMOKE_CHECKLIST.md` + GatheringShopGate 전체 낚시 왕복 assertion | D3D11 FishSale 18G/FinalRoute 30G PASS, 사람 입력 항목은 별도 미체크 | 없음(사람 체감은 Task 040/최종 체크) | NO |
| 043 | DONE | `DESIGN_MINING_FARMING.md`, 기존 Crop/Farmland/Item/Recipe/프리팹 정적 감사 | 씨앗·작물·바위 GUID/API 호환성 자기검토, 광질 왕복 완료 조건 확정 | 다음 단일 구현: `quarry-mining` Ore 2→15G 밤 판매. 농사는 저장 승인 후 단계적 복구 | NO |
| 044 | DONE | `DESIGN_RESIDENT_REQUEST.md`, Dialogue/Demand/전문가 레시피/인벤토리/일일 활동 정적 감사 | 기존 `Economy` 토픽과 실제 레시피 재료를 재사용하는 최소 요청 계약 자기검토 | 기능 미구현. 후속 코드는 별도 단일 작업과 D3D11 검증 필요 | NO |
| 045 | DONE | `DAYTIME_ACTIVITIES.md`, 갱신된 `PROJECT_PA_GAME_LOOP.md`, 현재 낮 활동 소스 | Fish 2→1 진열→18G/Fisher 구매와 Ore 2→1 진열→15G/Miner 구매 기존 D3D11 로그 대조, 6개 activityId 정적 검사 PASS | 농사·주민 의뢰 미구현, Carrot/Wheat 개별 판매 왕복은 미검증 | NO |
| 046 | DONE | `VILLAGE_TREND.md`에 SalesLogManager→VillageChangeSignal 실제 거래 단위·최근 40건·카테고리 집계·저장 경계를 코드와 대조 | Processed 2건/76G 선도 신호, Fish/Ore Raw 실제 판매, Processed 다음날 변화·저장 기존 D3D11 로그 대조 및 독립 표식 정적 검사 PASS | 장기 통계 저장과 4카테고리 실제 GameCamera 판매/다음 날 확인은 후속 작업 | NO |
| 047 | DONE | `VILLAGE_TREND.md`에 낚시·캠핑·가구 명명 트렌드 매핑, 점수 상한, 일차/주간 창, 동점 규칙 설계 | Fish/생선구이/목제 가구 Item·Recipe·Workbench·SaleRecord·ShopSlot 데이터 정적 대조, Task 093 당일 코드 계약 PASS | 캠핑 콘텐츠와 7일 저장 없음, 생선구이·목제 가구 밤 판매 미검증 | NO |
| 048 | DONE | 카테고리별 런타임 누적/선도 신호 | VillageChangeSignal PASS | 저장은 별도 | NO |
| 049 | DONE | `VillageCultureVisualController` 4카테고리 다음날 변화 기반 | Processed 전용 validator PASS 이력, Raw/Utility/Luxury Runtime/Editor·정적 계약 PASS | Raw/Utility/Luxury 실제 판매·저장·동일 GameCamera 확인 대기 | NO |
| 050 | DECISION_REQUIRED | NPC 스케줄/FSM 존재 | 행동 변화 검증 없음 | 구매 흐름 영향 승인 필요 | YES |
| 051 | DONE | 성공 판매 선도 카테고리→감사 앱 시설 방향 예고 | Runtime/Editor 오류 0, D3D11 FinalRoute·FinalPresentation PASS, 1920×1080 감사 앱 캡처 확인 | 실제 시설 해금은 기존 Tier/감사 조건 소유, Raw/Utility/Luxury 실제 판매 캡처 미검증 | NO |
| 052 | DONE | `DESIGN_EVENTS.md`, 실제 낚시/판매/트렌드/날짜 소유권 대조 | 문서 계약 10·소스/데이터 10·Task041 로그 5·whitespace 검사 PASS | 이벤트·이벤트 저장은 미구현, Task 078 승인 필요 | NO |
| 053 | DONE | VillageCulture 검증기 + VC-001A 문서 | 판매→다음날 시각 변화 PASS 이력 | 없음 | NO |
| 054 | DONE | `SAVE_SCHEMA.md` v11 판매 통계 추가 확장 설계 | 문서 계약 12·정확한 v10 소스 계약 17·whitespace·전체 diff 검사 PASS | 실제 v11 구현은 Task 055 사용자 승인 필요 | NO |
| 055 | DECISION_REQUIRED | SalesLog는 런타임 전용, v11 설계 완료 | 저장 왕복 없음 | v11 추가 스키마와 최소 소유자 API 범위 승인 필요 | YES |
| 056 | DONE | v5 ShopSlot 저장 + `PA_SaveRoundTripValidator` | Task 011에서 item/count/quality/currentPrice/displayPrice 실제 왕복 PASS | 신규 스키마 불필요 | NO |
| 057 | DONE | Save v9: VillageCulture 대기/활성 6필드 + 마이그레이션 (`71fa710`) | SaveRoundTrip v9 왕복 PASS(대기→다음날 활성→활성) | 트렌드 점수(048)/SalesLog(055)는 별도 승인 | YES→세션 지시로 승인 |
| 058 | PARTIAL | hired NPC/FSM 캡처·복원 코드 | 실제 저장소 왕복 없음 | NPC 상태 왕복 검증 | NO |
| 059 | TODO | Save 규칙 일부 존재 | 체크리스트 없음 | `SAVE_REGRESSION.md` 작성 | NO |
| 060 | PARTIAL | Coding Rules에 추가 확장/마이그레이션 규칙 | 코드 v0→v8 대조 | 전용 정책 문서 미작성 | NO |
| 061 | DONE | `DEMO_5_MINUTE_ROUTE.md` | FinalRoute PASS | 진짜 낚시는 포함하지 않음 | NO |
| 062 | DONE | Visual pass v1~v3 버그 스윕 | 핵심 5종 PASS | 사람 실기기 확인만 남음 | NO |
| 063 | DONE | `FINAL_HUMAN_CHECKLIST.md` | Final lock 증거와 대조 | 없음 | NO |
| 064 | DECISION_REQUIRED | Windows 빌드 파일 이력은 과거 ZIP뿐 | 현재 9898f6a 빌드 실행 증거 없음 | 새 빌드/리허설 승인 | YES |
| 065 | DONE | 5분 루트 발표 안전선·백업 캡처 경로 | FinalPresentation 5장 생성 | 녹화본은 사람 준비 | NO |
| 066 | DECISION_REQUIRED | Final Presentation Lock 문서는 존재 | 날짜/코드 프리즈 사람 결정 없음 | Demo Lock 날짜 확정 | YES |
| 067 | DECISION_REQUIRED | 조건부 발표용 판정 | 빌드·사람 리허설 미완 | 사람의 Demo 완료 판정 | YES |
| 068 | PARTIAL | Day 1 결산 버튼→Day 2 아침, Day 2 정산 간판→Day 3 아침 입력 + 낚시 밤 판매 경로 | FinalRoute/DayNight/FishingSale D3D11 PASS, OnNewDay 납품·채집 리셋 동기화 | 새 게임→Day 3 사람 연속 플레이·저장 종료/재실행 미확인 | YES→활성 `/goal`로 날짜 전환 연결 승인 |
| 069 | PARTIAL | 실제 `FishingSpot`→Fish→밤 18G 판매, 당일 `trend.fishing`, Raw 다음 날 생산자 수거처 구현 | Task039/041 D3D11 판매 PASS + Task092/093 컴파일·정적 계약 PASS | 같은 연속 플레이에서 결산 명명 트렌드→다음 날 시각 변화 확인 대기 | NO |
| 070 | PARTIAL | 기존 B05→Plank3→Tier2 가구 제작→진열→판매→`trend.furniture`, Task 095 단계 안내 | Runtime/Editor 오류 0, 읽기 전용 계약 PASS | 실제 전체 왕복·UI 가독성 및 100,000G 장기 페이싱 결정 | NO |
| 071 | DECISION_REQUIRED | 조건부 발표용 AI 판정 | 사람 재미 플레이 없음 | 사람 피드백 필요 | YES |
| 072 | DECISION_REQUIRED | Vertical Slice 조건 일부 | 저장·낚시·확장 미완 | 사람 완료 판정 불가 상태 | YES |
| 073 | DECISION_REQUIRED | SeasonModifier 존재 | 확장 설계/승인 없음 | Vertical Slice 후 승인 | YES |
| 074 | DECISION_REQUIRED | NPC 프로필 8종 | 신규 콘텐츠 없음 | 073/콘텐츠 승인 | YES |
| 075 | DECISION_REQUIRED | 판매 아이템 15종 | 신규 상품군 없음 | 콘텐츠·밸런스 승인 | YES |
| 076 | DECISION_REQUIRED | Tier/Shop 기반 존재 | 확장 저장 왕복 없음 | 성장·씬·저장 승인 | YES |
| 077 | DECISION_REQUIRED | VC-001A 사이드카 패턴 | 추가 시설 없음 | 시각/저장 승인 | YES |
| 078 | DECISION_REQUIRED | Fish 데이터만 존재 | 이벤트 시스템 없음 | 새 이벤트 승인 | YES |
| 079 | BLOCKED | TradePort BuildingData 존재 | 078 미완 | 선행 이벤트/Vertical Slice 필요 | NO |
| 080 | DECISION_REQUIRED | 가격·수요 데이터 존재 | 밸런스 패스 없음 | 사람 수치 승인 | YES |
| 081 | BLOCKED | Day 1 온보딩은 구현·검증됨 | Vertical Slice 미완 | 072 후 문구 보강 | NO |
| 082 | DECISION_REQUIRED | Final Locked 2560×1440, Panel validator | 사람 1920×1080/폰트 확인 미완 | 사람 가독성 판정 | YES |
| 083 | BLOCKED | AudioManager 존재, 핵심 이벤트 연결 없음 | 사운드 실청취 없음 | 072 및 기존 클립 확인 선행 | NO |
| 084 | BLOCKED | LongPlay validator PASS 이력 | 마을 변화 저장(Task057) 미완 | Persistence Lock 후 Day30+ 재검증 | NO |
| 085 | DECISION_REQUIRED | Roadmap Stage 5 기준 존재 | RC 증거 없음 | 출시 판정은 사람 | YES |
| 086 | PARTIAL | `MerchandisingCornerController`, placement 읽기 API, 장부 요약·월드 라벨, 전용 validator 구현 | Runtime/Editor 오류 0; D3D11 Raw/Processed 인접·품절/보충·회수/이동·실제 판매 3건·Processed 마을 신호·v10 로드 Raw2 재파생 PASS | ShopCustomization 회귀의 직접 `Camera.Render()`가 같은 네이티브 충돌을 두 번째로 재현. 기존 3회귀와 기본 화면 라벨 가독성 미완 | NO |
| 087 | DONE | `TRIPO_ASSET_AUDIT.md`, `ASSET_AND_TOOL_PROVENANCE.md`, 실제 FBX/씬/프리팹/Resources/코드 참조 감사 | FBX 174/OBJ 150/non-Nature FBX 24, B06~B08 레시피·Tier·footprint, B11/B12 정적 역할, 바이너리 씬 실사용, Nature Pack CC0 표식 정적 검사 PASS | Tripo 생성 계정/상업 이용 증빙과 Froggy Chair 라이선스 미확인. B06~B08/B11/B12 게임 카메라 물리·피드백 최종화는 후속 | NO |
| 088 | PARTIAL | `NpcDialogue`, `DayNightShopLoopController`, `Inventory`, 전문 주민 Economy 대사 | Runtime/Editor 오류 0; Chef Wheat3·Blacksmith Ore2·Carpenter Wood2, Tailor 작업대 불일치 제외, 당일 ID/저장 목록/정확 차감/친밀도 전용 보상 정적 계약 PASS | D3D11 실제 부족→준비→전달→중복 차단, 저장/로드·다음 날·밤 차단, UI 캡처는 공용 안전 캡처 경로 승인 뒤 확인 | NO |
| 089 | PARTIAL | `FarmPlotInteraction`, `Crop`, `Farmland`, Seed/Crop/Wheat 참조, 농장 씨앗 주머니 | Runtime/Editor 오류 0; Seed→Crop→Wheat GUID, 2개 고정 밭, 낮 전용, 심기 성공 후 씨앗 차감, 용량 확인 후 Wheat3 수확, 실제 Wheat 프리팹 표현, 금지 코어 비침범 12개 정적 계약 PASS | D3D11 실제 씨앗 수령→2칸 심기→성장→가방 가득 차단→수확 및 UI/동선 미확인. 날짜 성장/plot 저장은 F3 승인 대기 | NO |
| 090 | PARTIAL | `PlayableDayScenarioController` Day 2+ 퀘스트 패널 | Runtime/Editor 오류 0; 0.5초 갱신, 낚시·채광·농사·주민 도움, 가방·핫바·진열 상품 종류, 개점·당일 판매·정산 10개 정적 계약 PASS | 안전한 Unity 실행 경로 승인 뒤 Day 2 낮→밤→정산 상태 전환과 1920×1080 가독성 확인 필요 | NO |
| 091 | PARTIAL | `CraftingService` 결과 수용량 선검사 | Runtime/Editor 오류 0; 현재 레시피 8개, 양수 출력/재료, 결과 생성→메타 스택/차감 후 빈 슬롯 선검사→재료 차감 순서, 성공 피드백·금지 코어 비침범 11개 정적 계약 PASS | 안전한 Unity 실행 경로 승인 뒤 가방 가득 참 무차감, 메타 일치 스택, 재료로 비는 슬롯 실제 분기 확인 필요 | NO |
| 092 | PARTIAL | `VillageCultureVisualController`, Raw/Processed 실제 판매 카테고리, Nature Pack 실모델·Project P.A. 간판 리소스 | Raw 최신 판매를 기존 pending/다음 날/v10 category 계약에 연결하고 원시 큐브 없는 Raw 전용 시각 루트·무충돌 복제 계약 구현 | Runtime/Editor 빌드·정적 계약 후에도 직접 렌더 2회 충돌 경계로 실제 GameCamera 전후·라벨·동선 확인 필요 | NO |
| 093 | PARTIAL | `VillageChangeSignalController` 당일 명명 트렌드, 기존 결산 요약, 확장 validator | Runtime/Editor 오류 0; 정확 3쌍, 1018/2036, 현재 일차, 매출 상한, 동점, 캠핑 비활성, 기존 카테고리 보존 정적 계약 18개 PASS | 안전한 Unity 경로에서 결산 줄바꿈/가독성과 생선구이·목제 가구 제작→밤 판매 확인 필요 | NO |
| 094 | PARTIAL | Tripo/Placeable/출처 장기 정책, `DemoVisualDressingController` 런타임 노출 정리 | Runtime/Editor 오류 0; `Assets/Scripts` Froggy 참조 0, 원본·Resource 3종과 B01/CC0 식생/광장 벤치 보존 정적 계약 PASS | 같은 GameCamera에서 실내·광장 빈자리/초점/동선 확인. 최종 빌드 전 Resource 격리 또는 라이선스 증빙 필요 | NO |
| 095 | PARTIAL | `ProcessingOpportunityController` 가구 단계 projection, Day 4+ `PlayableDayScenarioController` 체크리스트 | Runtime/Editor 오류 0; Recipe/Tier/B05/Plank/stock/당일 sale·상태 무변경 계약 PASS | 안전 Unity 경로에서 Tier별 문구·1920×1080 패널·제작→판매→정산 확인 | NO |
| 096 | PARTIAL | `SaveManager.HasSaveAsync`, 기존 첫날 패널의 Title/새 게임/이어하기 분기 | Runtime/Editor 오류 0; ExistsAsync·기존 Load/Restore·실패 복귀·F5/F9·mutator 부재 계약 PASS | 안전 Unity 경로에서 저장 없음/있음 화면과 실제 v10 로드 확인 | NO |
| 097 | PARTIAL | `PauseManager` 제품 메뉴, 기존 `SaveManager` 저장/불러오기 권위 | Runtime/Editor 오류 0; 4개 메뉴 버튼, 이전 timeScale/커서 복원, 저장 존재 Load 게이트, 저장 성공 뒤 Quit, 예외 복구 계약 11/11 PASS | 안전 Unity 경로에서 ESC/클릭, 1920×1080 가독성, 저장·로드·실제 빌드 종료 확인 | NO |
| 098 | PARTIAL | `LongPlayProgressionController` Day 7 완주 UI, `DayNightShopLoopController` 배경 진행 게이트 | Runtime/Editor 오류 0; Day 7 Settlement 단일 표시, 기존 성과 읽기, Day7 저장→Day8→재저장, 저장 후 Quit, 예외 복구 계약 12/12 PASS | 안전 Unity 경로에서 Day 7 정산 화면·1920×1080 가독성·두 버튼·Day8/빌드 종료·이어하기 확인 | NO |
| 099 | PARTIAL | `PlayableDayScenarioController` 시작 조작 안내 | Runtime/Editor 오류 0; PhoneIntro→Controls→Supplies 순서, 이동·상호작용·핵심 UI·핫바·건설·저장/불러오기·Pause 실제 입력 매핑과 기존 타이틀/이어하기/도착 흐름 계약 11/11 PASS | 안전 Unity 경로에서 1920×1080 본문 잘림과 `조작 확인`→보급품→도착 클릭 전환 확인 | NO |
| 100 | PARTIAL | `PlayableDayScenarioController` 타이틀 전용 게임 종료 | Runtime/Editor 오류 0; 기존 Canvas/helper 재사용, 종료/이어하기/새 게임 3버튼 위치, Title 전용 노출, 비Title·결산 숨김, 이어하기 로딩 잠금, 단일 Quit·Editor 안내·저장 비침범 계약 14/14 PASS | 안전 Unity 경로에서 1920×1080 세 버튼 가독성·클릭과 실제 Windows 빌드 종료 확인 | NO |
| 101 | PARTIAL | `PlayableDayScenarioController` 저장 존재 기반 새 게임 확인 | Runtime/Editor 오류 0; 저장 확인 중 새 게임 잠금, 저장 없음 Name 직행, 저장 있음 경고→시작/타이틀 복귀, 기존 HasSave/Load/Quit/Controls 보존, 저장·삭제 비침범 계약 15/15 PASS | 안전 Unity 경로에서 저장 있음/없음 실제 클릭, 타이틀 재조회, 1920×1080 경고 가독성과 첫 F5 덮어쓰기 동작 확인 | NO |
| 102 | PARTIAL | `StorageUI` 런타임 24칸 화면, `PA_RuntimeSceneBinder` 단일 연결, Pause ESC 우선순위 | Runtime/Editor 오류 0; B09 OpenBox, 6×4 슬롯, 실제 아이콘·수량·품질/가격, 선택 핫바 정확 1개 차감, AddInstance 회수·가방 가득 참 보존, 커서/패널 상호배타, v10 저장 비침범 계약 14/14 PASS | 안전 Unity 경로에서 B09 접근→보관/회수→ESC, 1920×1080 가독성, v10 저장/로드 실제 왕복 확인 | NO |
| 103 | PARTIAL | `CraftingUI` 제작 도감/작업대 화면, `PauseManager` ESC 우선순위 | Runtime/Editor 오류 0; 기존 레시피 8개, C 전체 도감·원격 제작 차단, Workbench 컨텍스트, 아이콘·전체 재료 보유량·출력, 결과 갱신, 전체 화면 입력 차단, 커서/패널 상호배타 계약 16/16 PASS | 안전 Unity 경로에서 C 도감 8개·B05~B08 Space 필터/클릭·성공/실패·ESC와 1920×1080 가독성 확인 | NO |
| 104 | PARTIAL | Tripo/Placeable 장기 ADR, B11 Visual MeshCollider 런타임 정합 | Runtime/Editor 오류 0; 메시 준비 뒤 루트 Box만 비활성, 원본 mesh 비볼록 collider, 실패 시 기존 Box 유지, 기존 carving obstacle/씬/프리팹/FBX 보존 계약 12/12 PASS | 안전 Unity 경로에서 분수 둘레 실제 이동·벤치 접근·NPC 우회와 동일 GameCamera After 확인 | NO |
| 105 | PARTIAL | `NpcScheduleController` Rest 전용 방문 override, 외부·실내 손님 lease | Runtime/Editor 오류 0; 실제 phase 비변경, Rest만 Resume, 외부/실내 성공·실패·timeout·close 원위치/Shop/Rest 복구, Day 1·기존 FSM/구매 권위 보존 계약 18/18 PASS | 안전 Unity 경로에서 18:30/20:30/22:30 외부·실내 유입, 동시 손님 상한, 23:00 회수와 Rest 복귀 확인 | NO |
| 106 | PARTIAL | `VillageCultureVisualController` Processed 실제 에셋 전환 | Runtime/Editor 오류 0; primitive 0, B05 wrapper가 아닌 `Visual`만 복제, 준비 키트·Project P.A. 간판, Collider/NavMeshObstacle/행동/Light 제거, Raw·pending/active·v10 저장 계약 18/18 PASS | 안전 Unity 경로에서 동일 GameCamera의 스케일·정면·간판·상점/분수/플레이어/NPC 동선 확인 | NO |
| 107 | PARTIAL | `VillageCultureVisualController` Utility 다음 날 공구 수리대 | Runtime/Editor 오류 0; 판매 가능한 철제 도구·Forge 레시피·B07 실제 `Visual`, Utility 추적/pending/다음 날/상호 배타/힌트, 래퍼 비복제·물리/행동/Light 제거, Processed/Raw·v10 저장 계약 23/23 PASS | 안전 Unity 경로에서 실제 Utility 판매 당일/다음 날과 동일 GameCamera의 스케일·정면·간판·상점/분수/플레이어/NPC 동선 확인 | NO |
| 108 | PARTIAL | `VillageCultureVisualController` Luxury 다음 날 공예 전시대 | Runtime/Editor 오류 0; 판매 가능한 목제 가구/의류·Furniture/Sewing 레시피·B08 실제 `Visual`, Luxury 추적/pending/다음 날/상호 배타/힌트, 래퍼 비복제·물리/행동/Light 제거, 기존 3카테고리·v10 저장 계약 27/27 PASS | 안전 Unity 경로에서 실제 Luxury 판매 당일/다음 날·v10 복원과 동일 GameCamera의 스케일·정면·간판·상점/분수/플레이어/NPC 동선 확인 | NO |
| 109 | PARTIAL | `HiringService`, `HiringUI`, 기존 C-02~C-09 역할 주민·8개 레시피 기반 채용 제품 흐름 | Runtime/Editor 오류 0; 후보 8명·역할별 정확 매칭·실제 SkinnedMesh·명시 프리팹 우선·채용 clone 재사용 차단·비용 차감 전 검증·신규/복원 공통 원본·profile/specialty/schedule/dialogue/친밀도 주입·전문가 작업대별 기존 레시피·첫 열기 UI·소개/비용/잠금/결과 계약 36/36 PASS | 안전 Unity 경로에서 스마트폰 실제 채용·정확한 역할 스폰/행동·잔액 부족·중복 방지·저장/로드 복원과 1920×1080 UI 가독성 확인 | NO |
| 110 | PARTIAL | `PlayableDayScenarioController`, `LongPlayProgressionController`, 기존 `HiringService` 읽기 전용 roster | Runtime/Editor 오류 0; Day 5 이후 미고용/고용 목표, 운영 체크리스트 인원·이름·한글 역할, 고용 이벤트 즉시 갱신, 결정론 roster, Day 7 결산, Day 1~4·생활/진열/가격/개점/판매/정산·채용/경제/저장 권위 보존 계약 29/29 PASS | 안전 Unity 경로에서 Day 5 스마트폰 채용 전후 체크리스트 전환→Day 7 첫 주 결산 roster와 1920×1080 가독성 확인 | NO |
| 111 | PARTIAL | `Inventory.CanAddInstance`/`AddInstance`, `ProducerNpcController` 납품 | Runtime/Editor 오류 0; 전량 수용 읽기 전용 선검사·실패 무변경, 가방/잔액 보류, 공간→결제→메타 이전→예외 환불→성공 제거, 기존 말풍선/FSM/LongPlay 계약 30/30 PASS | 안전 Unity 경로에서 가방 가득 참 돈·양쪽 재고 보존→공간 확보 후 정확 수량/비용 납품·성공/보류 말풍선 확인 | NO |
| 112 | PARTIAL | Task 112 `LongPlayProgressionController` Day 8~14 계획, `PlayableDayScenarioController` 2주차 실제 상태 체크리스트 | Runtime/Editor 오류 0; Week 1/Day 7 완주 보존, 보관·Processed 판매·채용·2카테고리·Tier 1·마을 변화·2상품 읽기 전용 목표와 권위 비침범 계약 40/40 PASS | 안전 Unity 경로에서 Day 7→8, Day 8~14 대표 상태 전환, 1920×1080 목표/체크리스트 가독성 확인 | NO |
| 113 | PARTIAL | Tripo/Placeable 장기 ADR 재감사, B12 활성 인스턴스 Visual bounds 기반 BoxCollider/NavMeshObstacle 축소 | Runtime/Editor 오류 0; map/legacy 탐색, 8모서리 bounds, 축소 전용 Box/Obstacle, 메시 실패 보존, 원본/프리팹/씬/기능/Placeable 비침범 계약 20/20 PASS | 안전 Unity 경로에서 기존 투명 벽 영역 통과·보이는 부두 경계 정지·NPC carving 우회와 동일 GameCamera 확인 | NO |
| 114 | PARTIAL | `LongPlayProgressionController` Day 15~30 계획/Day 30 완주, `PlayableDayScenarioController` 첫 달 실제 상태 체크리스트, 공용 다음 날 게이트 | Runtime/Editor 경고 0·오류 0; 16개 계획·16개 상태 목표, Day 7 보급/완주 보존, Day 30 Settlement 요약, 저장→Day 31→재저장, 기존 권위 재사용 계약 56/56 PASS | 안전 Unity 경로에서 대표 Day 15~30 상태 전환, Day 30 두 버튼, Day 31 이어하기와 1920×1080 가독성 확인 | NO |
| 115 | PARTIAL | `ShopCustomizationController` B07 Tier 1 보상/배치 조회, Day 23/24 Forge 목표, P5 검증 기대값 | Runtime/Editor 오류 0(기존 CS8785/CS0414만); BuildingData·설계도·레시피·상품·프리팹 권위, 중복 보상 방지, 활성 배치, 정확한 당일 ToolSet, 단조 매출 목표, B06/B08 보존 계약 39/39 PASS | 안전 Unity 경로에서 Tier 1 장부 B05/B07 지급, 진열대 회수→B07 배치/접근, IronBar/ToolSet 제작·혼합 판매와 Day 23/24 UI 확인 | NO |
| 116 | PARTIAL | 공용 `PA_SafeGameViewCapture`, ShopCustomization/ShopProgression 안전 GameView 캡처 | Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414), 캡처·freshness·상태 복원·await·대상 직접 Render 0 계약 28/28 PASS | 잔여 직접 `Camera.Render()` 10곳을 분할 전환한 뒤 사람 승인 D3D11 격리 캡처와 두 검증기 실제 확인 | NO |
| 117 | PARTIAL | VillageCulture/CustomerPanelLayout/FinalPresentation 공용 GameView 캡처 | Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414); async 가드·10개 await 캡처·1920×1080/시장 구도·공용 복원 위임·대상 직접 Render 0·잔여 7 계약 38/38 PASS | Character/Cottage/DemoView/GatheringShop/OutdoorPlacement/ShopEvolution/Workbench 전환 뒤 사람 승인 D3D11 격리 캡처와 순차 검증 | NO |
| 118 | PARTIAL | DemoView/GatheringShop/OutdoorPlacement 공용 GameView 캡처 | Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414); async 가드·1/5/2개 await 캡처·2560×1440/1920×1080/1280×720·준비/구도/컨트롤러 복원·대상 직접 Render 0·잔여 4 계약 35/35 PASS | Character/Cottage/ShopEvolution/Workbench 전환 뒤 사람 승인 D3D11 격리 캡처와 순차 검증 | NO |
| 119 | PARTIAL | Character/Cottage/Workbench 공용 GameView 캡처 | Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414); async 가드·소스/idle/walk·B10 7파일·B05 회전/runtime 캡처·1600×900/1920×1080·controller/renderer 복원·대상 직접 Render 0·잔여 1 계약 42/42 PASS | ShopEvolution 마지막 전환 뒤 사람 승인 D3D11 격리 캡처와 순차 검증 | NO |
| 120 | PARTIAL | ShopEvolution 소스 감사/runtime 성장 공용 GameView 캡처 | Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414); async/중복 가드·B02~B04 4방향 12파일·Tier 1~3 runtime·1600×900·4초/0.75초·controller/clearFlags/배치 저장 복원·저장소 직접 Render 0 계약 36/36 PASS | 사람 판단 뒤 격리 D3D11 PNG 1회로 freshness·가독성·카메라/화면 복원을 확인하고 성공 시 검증기 순차 실행 | NO |
| 121 | PARTIAL | `LongPlayProgressionController` Day 31~45 계획, `PlayableDayScenarioController` 두 번째 달 실제 상태 체크리스트 | Runtime/Editor 오류 0(기존 CS8785/CS0414); 15개 계획·15개 상태 목표, 보관/가공/채용/상품/카테고리/Forge·ToolSet/마을 변화/준비 재고/매출, Day 1~30·Tier·저장 비침범 계약 23/23 PASS | 안전 Unity 경로에서 Day 30→31, 대표 Day 35/40/45 상태 전환, 1920×1080 가독성과 Day 46 폴백 확인 | NO |
| 122 | PARTIAL | `LongPlayProgressionController` Day 46~76 7단계 계획 생성, `PlayableDayScenarioController` Tier 2 실제 상태 체크리스트 | Runtime/Editor 오류 0(기존 CS8785/CS0414); 31일·7단계 분포, 보관 12→20/가공 2→4/지원 인력+4상품/3카테고리/B07+ToolSet+Processed/마을 변화+4상품/매출, Day 76 Tier 2·55,000G→100,000G·기존 권위 보존 계약 34/34 PASS | 안전 Unity 경로에서 대표 주간 상태, Day 76 자동 승급, 1920×1080 가독성과 Day 77 폴백 확인 | NO |
| 123 | PARTIAL | `LongPlayProgressionController` Day 77~90 계획, `PlayableDayScenarioController` B06/세 주방 상품/Chef/Processed 실제 상태 체크리스트 | Runtime/Editor 오류 0(기존 CS8785/CS0414); Task 124~125의 44개 자동 계약+3개 직접 권위 행으로 정적 계약 47/47 PASS | 안전 Unity 경로에서 B06 배치·세 제작/판매·Chef·마을 변화·Day 90·1920×1080 확인 | NO |
| 124 | DONE | Task 123 정적 계약 복구 | 호출부 2개, 단일 정의, 계획/case 14개, Day 76 경계, B06 Kitchen, 세 레시피/Processed 출력/리소스 44개 자동 PASS + Task 125 직접 권위 3개 PASS | 없음 | NO |
| 125 | DONE | B06 Tier·두 출력 Item 이름 권위 행 감사 | B06 `case ... return 2`, BakedPotato/GrilledFish Unity YAML Unicode escape 해석값을 명시 경로에서 확인. 데이터 결함 없음 | 없음 | NO |
| 126 | PARTIAL | Day 91~105 Tier 3 공동 공방 계획, 주민 요청 기반 일일 평판, B08/의류/가구/재단사/Luxury 실제 상태 체크리스트 | Runtime/Editor 오류 0(기존 CS8785/CS0414); 15개 계획·15개 상태 목표, 하루 1회 저장 표식, Tier 3/B08/두 Luxury 레시피/마을 방향 계약 24/24 PASS | 안전 Unity 경로에서 주민 요청 3일→Tier 3, B08 배치, 의류·가구 제작/판매, Luxury 변화, Day 105·1920×1080 확인 | NO |
| 127 | PARTIAL | B05~B08 Workbench interaction 셀→전문 주민 NavMesh 접근·셀 예약 | Runtime/Editor 오류 0(기존 CS8785/CS0414); 장애물 원점 이동 제거, interaction 투영, 완전 경로, 셀 예약/해제, 이동·회수 무효화, 복원 재접근 계약 29/29 PASS | 사람 판단 뒤 안전 Unity에서 실제 접근/정면/겹침 방지/제작 확인 | NO |
| 128 | PARTIAL | `CustomerArrivalController` 세션 한정 관광객, 기존 `NpcController` 쇼핑 FSM/분류 UI | Runtime/Editor 오류 0(기존 CS8785/CS0414); 정상 플레이 관광객 0명 감사, 영업당 2/동시 1명, 주민 SkinnedMesh 시각 전용 복제, 무일과표·무직업·무친밀도·비영속, NavMesh 완전 진입/복귀, 기존 Day 1·주민 초대 보존 계약 46/46 PASS | 사람 판단 뒤 안전 Unity에서 실제 입장·`[관광객]` 표시·구매/거절·퇴장·동시 손님/1920×1080 확인 | NO |
| 129 | PARTIAL | `LongPlayProgressionController` Day 106+ 실제 감사 조건·평판 5 경로·Tier 4 완주, `PlayableDayScenarioController` 최종 감사 목표/체크리스트 | Runtime/Editor 오류 0(기존 CS8785/CS0414); 감사 요구 평판까지 하루 1회, 실제 500,000G/평판5/고용3·다음 감사일, AuditService 단독 수동 승급, Tier4 Settlement 완주·저장/자유 운영 계약 40개 자동+1개 직접 권위 PASS | 사람 판단 뒤 안전 Unity에서 평판 4/5→정기 감사→Tier4→최종 모달→저장/계속·종료·재실행·1920×1080 확인 | NO |
| 130 | PARTIAL | `AuditService` 읽기 전용 결과 상태/갱신 이벤트, `AuditResultUI` 실제 감사 조건·최근 결과·다음 행동 | Runtime/Editor 오류 0(기존 CS8785/CS0414); 500,000G/평판5/고용3/7일·단독 수동 승급 보존, 실패/통과/최고/보류 분기, 구독 해제, UI 비변경 권위, 폰 높이 계약 48/48 PASS | 사람 판단 뒤 안전 Unity에서 감사 실패/성공 즉시 갱신·Tier4 문구·1920×1080 카드 잘림/겹침 확인 | NO |
| 131 | PARTIAL | 기존 GRID/Tripo 장기 정책 재대조, `Workbench` B06 Visual 기반 물리·interaction·제작 피드백 | Runtime/Editor 오류 0(기존 CS8785/CS0414); FBX174/OBJ150, B05 보존, B06 축소 전용 Box/Obstacle·`-Z` anchor·성공 후 모델 pulse·Tier2/2×2·정책 계약 30/30 PASS | 사람 판단 뒤 안전 Unity에서 B06 배치·플레이어/Chef 접근·물리 경계·Bread 제작 pulse·동일 GameCamera Before/After 확인 | NO |

## 2026-07-27 Continuation — Task 126 Day 91~105 Tier 3 Community Atelier / Project Partial

- Added the missing normal-play reputation bridge: one completed specialist request creates one saved daily marker and one reputation point while the shop remains Tier 2.
- Existing Tier 3 automatic advancement, B08, Clothes/Furniture recipes, Tailor hiring, exact daily sales, and Luxury village culture drive fifteen authored Day 91~105 objectives.
- Sequential Runtime/Editor builds completed with zero errors; baseline CS8785/CS0414 warnings remain. Static contracts passed 24/24.
- Unity was not launched under the repeated native-crash boundary, so the three-day route, B08 craft-to-sale paths, village response, finale, and UI readability remain runtime-unverified.
- No scene, prefab, asset, save schema, tier value, recipe, item, economy, package, ProjectSettings, commit, or push operation was performed.

## 감사 결론

- Track A Core Loop은 개별 검증 기준으로 강하지만 실제 저장소 왕복이 빠져 “반복 가능한 완전 루프”로 잠기지 않았다.
- 가장 큰 병목은 저장 v10의 실제 종료/재실행 왕복과 승인 대기 중인 판매 통계 v11이다.
- 두 번째 병목은 직접 렌더 네이티브 충돌 이후 Task 086/088~093의 실제 카메라·연속 플레이 증거가 묶여 있다는 점이다. Fish는 이미 전용 낚시 상호작용과 밤 판매를 갖췄다.
- `VERIFICATION_RULES.md`와 `BUG_LOG.md`의 보류 검증기 표기는 실제 통과 증거와 불일치하므로 이번 동기화에서 해소한다.
