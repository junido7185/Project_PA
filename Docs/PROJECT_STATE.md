+# Project P.A. Current Project State

기준일: 2026-08-25\
기준 브랜치/커밋: `milestone/gameplay-beta-85` / `29fb98f400b55dcc7f7e5c580c9b476041d378f4`\
Unity: `6000.3.2f1`, D3D11\
시각 증거: [`VisualAudit/2026-08-25-29fb98f/VISUAL_BASELINE.md`](VisualAudit/2026-08-25-29fb98f/VISUAL_BASELINE.md)

## 요약 판정

Project P.A.는 더 이상 코드 조각만 있는 초기 프로토타입은 아니다. `Prototype_FirstDay`에는 타이틀부터 Day 1 판매·감사·저장·결산까지 이어지는 실행 가능한 튜토리얼 루프가 있고, `WorldSandbox`에는 생성 월드의 낮 활동·제작·상점·고객·마을 반응·7일 진행·저장을 잇는 M85 구현이 들어 있다.

그러나 현재 플레이어가 실행하는 유일한 Build Settings 씬은 `Prototype_FirstDay`이다. 최근 M85 전체 루프는 `WorldSandbox` 전용이며 일반 게임 진입점에서 도달할 수 없다. M85 통합 검증도 저장 후 플레이어 방향 복원 실패로 중단되어 있다. 따라서 현재 상태는 **기능 데모는 플레이 가능하지만, 하나의 최종 게임으로 연결된 기능 완성형 빌드는 아님**으로 판정한다.

## 실행 진입점과 씬

| 항목 | 현재 상태 | 플레이어 접근성 |
|---|---|---|
| `Assets/Scenes/Prototype_FirstDay.unity` | Build Settings에서 유일하게 활성화된 씬. 타이틀, 신규 게임, Day 1 판매 튜토리얼, 저장/불러오기, 낮/밤 보조 루프가 실행됨 | 접근 가능 |
| `Assets/Scenes/WorldSandbox.unity` | WORLD-001~010과 BETA-001~010의 생성 월드 및 통합 플레이 표면 | Editor/validator로만 접근 가능. 일반 빌드에서는 `UNREACHABLE` |
| `Assets/Scenes/MainGame.unity` | 이전 메인 씬 자산은 존재하지만 Build Settings 비활성. Gate와 별도 사람 승인 전 통합 금지 | 일반 빌드에서는 `UNREACHABLE` |
| Standalone 빌드 | 저장소에서 실행 가능한 최신 `.exe`를 찾지 못함 | 현재 Editor Play Mode만 직접 감사함 |

`PA_RuntimeSceneBinder`는 로드된 씬마다 경제, 시간, 입력, 인벤토리 연계 UI, 감사, 고용, 판매 기록, Day/Night, 장기 진행, 고객 도착, 저장 등 공통 권위를 보완한다. `WorldGameplayAdapterService`와 `WorldAlphaPlayableController`는 코드와 런타임 가드 모두 `WorldSandbox` 전용이다.

## 현재 실제로 작동하는 시스템

다음 항목은 이번 D3D11 실행 또는 같은 런타임 권위를 사용하는 현재 검증에서 실제 동작을 확인했다.

| 시스템 | 확인된 현재 상태 | 실행 증거 |
|---|---|---|
| 타이틀/신규 게임 온보딩 | 타이틀 → 이름 → 브리핑 → 맵 → 휴대폰 → 조작 → 보급품 → 도착 버튼 경로가 완료됨 | `01_launch.png`, `02_controls.png`, `03_day_spawn.png`; `VisualBaseline_AuditCapture` PASS |
| 플레이어/카메라/HUD | 플레이어, 카메라, 시계, 돈, 목표, 핫바, 상호작용 프롬프트가 Play Mode에서 활성 | `03_day_spawn.png`; Core Slice D3D11 PASS |
| 인벤토리 | `I` 경로가 연결되고 인벤토리 패널이 열림. 15개 아이템 카탈로그가 Resources에 존재 | `04_inventory.png`; 이전 사람 입력 smoke PASS |
| 낮 준비 채집 | 해변 채집 지점이 런타임에 존재하고 수집 전/후 상태와 아이템 지급 로그가 변함 | `05_gathering_before.png`, `06_gathering_after.png`; Gathering+Shop Review PASS |
| 상점 진열/가격 | 실제 `ShopSlot`에 상품을 넣고 가격 UI를 열며 일반/고가 품질 분기를 표시함 | `11_shop_price.png`, `12_shop_price_rare.png`; Final Presentation 관련 assertion PASS |
| 고객/구매/수익 | 실제 `PurchaseEvaluator`, `ShopSlot.TryPurchaseByNpc`, `SalesLogManager`, `EconomyService` 경로로 판매가 기록되고 돈이 500G에서 530G로 증가함 | `13_npc_feedback.png`, `14_audit_app.png`; 판매 assertion PASS |
| 감사/진행 피드백 | 누적 매출, Tier 진행, 다음 시설 방향을 휴대폰 감사 화면에 표시함 | `14_audit_app.png` |
| Day 1 결산/다음 날 | Day 1 결산 UI와 다음 날 상태 전환 코드가 실행됨 | `15_day1_summary.png`, `09_next_day.png`; Gathering+Shop Review PASS |
| 저장/불러오기 | 실제 `SaveManager` v12와 격리 `LocalJsonSaveRepository`로 저장 후 플레이어 위치를 복원함 | `16_save_reload.png`, `Docs/VisualAudit/2026-08-25-29fb98f/savegame.json`; 감사 캡처 PASS |
| 컴파일 | Runtime/Editor 컴파일 오류 0 | 2026-08-25 `dotnet build`; 기존 source-generator/unused-field 경고만 존재 |

현재 Resources 카탈로그에는 Item 15개, Recipe 8개, NPC profile 8개, Hiring candidate 8개, Building 12개, Tier 5개가 있다. 이는 콘텐츠 데이터의 존재량이며, 모든 항목이 일반 플레이에서 발견 가능하다는 뜻은 아니다.

## 코드가 존재하지만 현재 일반 플레이 완주로 입증되지 않은 시스템

| 시스템 | 코드/검증 상태 | 현재 플레이어 상태 |
|---|---|---|
| 128×128 생성 섬, Chunk mesh, elevation/terraform | WORLD 마일스톤 코드 및 검증 존재 | `WorldSandbox` 전용이므로 현재 빌드 타이틀에서 도달 불가 |
| 생성 월드 낮 활동(숲/농사/채광/낚시) | BETA-002 구현 및 validator 계약 존재 | `WorldSandbox` 전용 전체 경로는 일반 플레이에서 도달 불가 |
| B05~B08 제작 체인 | BETA-003과 8개 Recipe 데이터 존재 | 현재 감사에서 Basic 제작 UI와 실제 플레이어 제작 입력은 직접 관찰하지 않음 |
| 상점 커스터마이징/가구 이동 | `ShopCustomizationController`와 B01 이동 경로 존재 | 이번 감사에서 플레이어 진입·이동·저장 UX를 직접 관찰하지 않음 |
| 휴대폰 채용/Feed 완성 경로 | BETA-006 구현 및 8개 후보 데이터 존재 | Prototype 휴대폰에서는 감사 화면만 직접 관찰함 |
| 판매 → 다음 날 마을 변화/NPC 역할 반응 | BETA-007 구현, 기존 판매/마을 권위와 연결 | 전체 일반 플레이 연속 경로와 시각 결과는 직접 관찰하지 않음 |
| 7일 진행/주간 완료 | BETA-008 구현 및 checkpoint 존재 | 실제 새 게임부터 Day 7까지의 연속 플레이는 미검증 |
| 생성 월드 통합 저장/재시작 | v12 구현. 셀/월드 checksum/B09 복원까지 통과한 로그 존재 | BETA-010에서 137° 플레이어 방향이 identity로 복원되어 반복 실패 |

## 현재 Blocker

### P0 — 최종 게임 진입점 불연결

완성도가 가장 높은 최근 게임 루프는 `WorldSandbox`에 있지만 Build Settings와 `Prototype_FirstDay` 타이틀에서 연결되지 않는다. 코드가 구현됐어도 처음 플레이하는 사용자는 해당 루프에 들어갈 수 없다.

### P0 — BETA-010 저장 재시작 회귀

`HARD_BLOCKER_BETA_010_PLAYER_FACING_RESTORE`: 저장한 player cell `(64,61)`과 월드/B09 데이터는 복원되지만 저장된 137° 방향이 identity로 돌아간다. 같은 assertion에서 D3D11 두 번 실패했으며, 이후 계속 판매·repeated load·BETA-007/008 통합·Golden/M70 회귀는 미도달이다.

### P0 — 목표 화면비 증거 부재

현재 실행 가능한 standalone 빌드가 없고, 비배치 Editor Game View가 476×1297로 캡처됐다. 따라서 현재 캡처는 이 실제 Editor 창에서의 문제를 입증하지만, 목표 1920×1080의 최종 레이아웃을 입증하지 않는다.

### P1 — 카메라/가림 및 UI 안전영역

20:06 상점 화면과 Day 1 결산 화면에서 차양과 구조물이 화면 대부분을 가린다. 타이틀 뒤에도 HUD와 상호작용 프롬프트가 그대로 보이고, 목표/돈/상단 안내와 핫바/폰/프롬프트가 겹친다. Final Presentation validator도 `MoneyHUD does not overlap objective panel` assertion에서 실패했다.

### P1 — 기능 오브젝트의 비주얼 임시성

해변 채집 지점은 큰 주황/회색 큐브, 막대, 원형 받침과 거대한 월드 텍스트로 보인다. 상점 판매대와 실내/실외 경계도 설명 없이 즉시 식별하기 어렵다.

## 저장 및 데이터 상태

- Save schema: v12.
- 실제 사용자 저장: `C:/Users/sdjsd/AppData/LocalLow/DefaultCompany/Project_PA/savegame.json`.
- 사용자 저장의 마지막 수정 시각은 2026-08-05이며 이번 감사에서 변경하지 않았다.
- 감사 저장은 `Docs/VisualAudit/2026-08-25-29fb98f/savegame.json`에 격리했다.
- 동일 세션의 실제 `SaveManager.SaveGameAsync/LoadGameAsync`로 player position 복원을 확인했다.
- 프로세스 종료/재실행을 포함한 생성 월드 저장은 BETA-010의 방향 복원 blocker 때문에 최종 PASS가 아니다.

## 자동 검증 가능 여부

| 검증 | 현재 결과 |
|---|---|
| Runtime/Editor C# compile | 가능, 오류 0 |
| D3D11 Play Mode 진입 | 가능, PASS |
| Core Slice 런타임 구성 | 가능, PASS |
| Gathering + Shop 기능 검증 | 가능, PASS |
| 비배치 Game View 캡처 | 가능, 16장 생성 |
| 배치 Game View 캡처 | 현재 timeout. 기능 실패와 분리해야 함 |
| Final Presentation 레이아웃 | FAIL: MoneyHUD/objective overlap |
| 격리 save/load | 가능, Prototype 위치 복원 PASS |
| BETA-010 통합 재시작 | HARD BLOCKER: player facing 복원 |
| 목표 1920×1080 standalone 시연 | 최신 빌드 부재로 확인 못 함 |

비배치 Editor 종료 시 `JobTempAlloc` 잔여 할당 경고가 기록됐지만 native crash는 생성되지 않았다. 관광객 생성 중 `Animator is not playing an AnimatorController` 경고도 2건 있었다. 둘 다 이번 감사에서는 기능 차단이 아니라 후속 런타임 부채로 분류한다.

## 감사 중 발생한 비프로젝트 산출물

첫 Unity 실행 시 `.claude/settings.json`이 예기치 않게 untracked로 생성됐다. 내용은 `humanize-korean@im-not-ai` 플러그인 활성화 설정이며 생성 주체를 프로젝트 코드에서 확인하지 못했다. 감사 범위 밖 파일로 보존했으며 수정·삭제하지 않았다.
