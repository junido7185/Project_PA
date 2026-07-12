# TASK_QUEUE — Project P.A. 작업 큐

작성: 2026-07-09
지위: Codex가 한 번에 **한 작업(Task)만** 꺼내 수행하는 순서 큐. 정체성 상위 문서: `../01_IDENTITY/PROJECT_PA_IDENTITY.md`.
목표: 졸업 프로토타입이 아니라 **완성 게임**. 순서는 안정화 → 검증 → 작은 기능 → 저장 → 시연 → 확장.

## 사용법

1. Codex는 상태가 `TODO`인 가장 위 작업(선행 작업이 모두 `DONE`인 것)을 하나 고른다.
2. 그 작업을 `ACTIVE_TASK.md`에 복사해 진행한다.
3. 완료 시 여기 상태를 `DONE`으로 바꾸고 `DONE_TASKS.md`에 기록한다.
4. `사람 승인 필요: YES`(대체로 난이도 XL) 작업은 Codex가 단독 착수 금지. 반드시 사람 승인 후.

## 난이도 등급

- **XS**: 문서/간단 설정/작은 enum/로그 추가
- **S**: 단일 스크립트 수정, 작은 UI/데이터 연결
- **M**: 시스템 1개 확장, 저장/검증 필요
- **L**: 여러 시스템 연결, 씬/프리팹 참조 위험 있음
- **XL**: 사람 승인 필요, 아키텍처 결정 필요, Codex 단독 진행 금지

## DECISION_REQUIRED (사용자 확정 대기 — 큐는 임시 기본값으로 진행)

- 졸업 발표 Demo Lock 날짜: **미정** → `DEVELOPMENT_TIMELINE.md` 역산표로 대응
- Vertical Slice 두 번째 낮 활동: **미정** → 임시 기본값 **낚시(Fishing)** (Task 033~037)
- 보류 검증기 실행 방식: **미정** → 임시 기본값 "Editor 닫고 D3D11 batchmode" (Task 004)
- 주간 작업 가능 시간: **미정** → `DEVELOPMENT_TIMELINE.md` 3시나리오로 대응

## Phase 목록 · 작업 분포

| Phase | 이름 | Task 범위 | 개수 |
|---|---|---|---|
| 0 | Baseline & Safety | 001–007 | 7 |
| 1 | MVP Stabilization | 008–016 | 9 |
| 2 | Shop Core | 017–026 | 10 |
| 3 | NPC Purchase Behavior | 027–035 | 9 |
| 4 | Daytime Life Loop | 036–045 | 10 |
| 5 | Village Trend System | 046–053 | 8 |
| 6 | Save & Persistence | 054–060 | 7 |
| 7 | Graduation Demo Milestone | 061–067 | 7 |
| 8 | Vertical Slice | 068–072 | 5 |
| 9 | Full Game Completion | 073–085 | 13 |
| | **합계** | | **85** |

---

# Phase 0. Baseline & Safety

## Task 001 - 프로젝트 구조 인덱스 작성

상태: DONE
단계: Phase 0. Baseline & Safety
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- `Assets/Scripts/` 전체 스크립트를 시스템별로 분류한 인덱스 문서를 `AI_WORKFLOW/03_TASKS/PROJECT_CODE_INDEX.md`로 작성.

Project P.A.와의 연결:
- 완성 게임 목표 — 이후 모든 작업의 파일 탐색 시간을 줄이는 기반.

수정 가능 파일:
- `AI_WORKFLOW/03_TASKS/PROJECT_CODE_INDEX.md` (신규, 문서만)

수정 금지 파일:
- 모든 `.cs`, 씬, 프리팹, 에셋 (읽기만)

구현 조건:
- 시스템 카테고리(플레이어/인벤토리/상점/경제/NPC/낮밤/마을변화/저장/활동/티어감사/UI/에디터)별로 파일 나열.
- 각 파일 1줄 역할 요약. 추정은 "추정"으로 표시.

완료 조건:
- 컴파일 영향 없음(문서만). 인덱스가 실제 파일 목록과 일치. 요청 작업만 수행.

검증 방법:
- `Assets/Scripts` 파일 수와 인덱스 항목 수 대조.

실패 시 처리:
- `BUG_LOG.md` 기록 후 멈춤.

선행 작업: 없음

## Task 002 - 현재 데모 플로우 문서화

상태: DONE
단계: Phase 0
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- `Prototype_FirstDay.unity` Day 1 데모 루트를 단계별로 `AI_WORKFLOW/03_TASKS/DEMO_FLOW.md`에 기록(README의 Demo Route 기준 + 실제 코드 진입점 확인).

Project P.A.와의 연결:
- 상점 운영 + 시연 — 안정화·Demo Lock의 기준선.

수정 가능 파일:
- `AI_WORKFLOW/03_TASKS/DEMO_FLOW.md` (신규)

수정 금지 파일:
- 모든 코드/씬/에셋.

구현 조건:
- 각 단계에 관련 스크립트(`PlayableDayScenarioController`, `DayNightShopLoopController`, `ShopSlot`, `ShopPriceUI`, `PurchaseEvaluator` 등)를 연결.

완료 조건:
- 문서만. 루트 10단계가 코드 진입점과 매칭.

검증 방법:
- README Demo Route와 대조.

실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 001

## Task 003 - 컴파일 기준선 기록

상태: DONE
단계: Phase 0
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 현재 `dotnet build Assembly-CSharp.csproj` 결과(경고/에러 수)를 `AI_WORKFLOW/03_TASKS/BASELINE_COMPILE.md`에 기록.

Project P.A.와의 연결:
- 완성 게임 — 이후 회귀 판단 기준.

수정 가능 파일:
- `AI_WORKFLOW/03_TASKS/BASELINE_COMPILE.md` (신규)

수정 금지 파일: 모든 코드/씬/에셋.

구현 조건:
- Unity Editor가 열려 있으면 batchmode 금지 — 그 경우 "Editor 열림으로 보류"를 기록.
- 실제 실행한 명령과 출력 요약만 기록. 실행 못 하면 실행 못 했다고 기록(추정 금지).

완료 조건: 문서만. 실제 결과 또는 보류 사유 기록.
검증 방법: 명령 출력 첨부.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: 없음

## Task 004 - 보류 검증기 실행 방식 결정 및 실행

상태: DONE
단계: Phase 0
난이도: S
예상 Codex 실행 횟수: 2
사람 승인 필요: YES — Unity 실행(D3D11/batchmode) 방식을 사람이 확정해야 함 (DECISION_REQUIRED)

목표:
- `PA_FinalDemoRouteValidator`, `PA_LongPlayProgressionValidator` 두 검증기를 실행하고 결과를 `BUG_LOG.md`의 BLOCKED 항목에 반영.

Project P.A.와의 연결:
- 시연 + 안정화.

수정 가능 파일:
- `AI_WORKFLOW/05_LOGS/BUG_LOG.md`, `PROJECT_PA_STATUS.md`

수정 금지 파일: 모든 코드/씬/에셋.

구현 조건:
- Unity Editor가 열려 있으면 batchmode 금지. 사람이 Editor 메뉴 실행 or Editor 닫기를 결정할 때까지 대기.
- D3D11 baseline에서만 자동 실행(D3D12 미승인).

완료 조건: 두 검증기 실제 결과 기록 또는 BLOCKED 유지 사유 기록.
검증 방법: 검증기 콘솔 출력.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: 없음

## Task 005 - 위험 파일 목록 확정

상태: PARTIAL
단계: Phase 0
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 씬/프리팹/저장 참조가 걸린 위험 파일 목록을 `AI_WORKFLOW/03_TASKS/RISK_FILES.md`로 확정(HANDOFF의 위험 파일 표를 근거로 확장).

Project P.A.와의 연결: 완성 게임 — 안전 개발 기반.

수정 가능 파일: `AI_WORKFLOW/03_TASKS/RISK_FILES.md` (신규)
수정 금지 파일: 모든 코드/씬/에셋.

구현 조건:
- 각 파일에 "왜 위험한가"(직렬화 필드/씬 Find/저장 스키마/코드 § 인용) 명시.

완료 조건: 문서만.
검증 방법: HANDOFF §4 위험 파일과 교차 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 001

## Task 006 - dirty git 작업 정책 정리

상태: PARTIAL
단계: Phase 0
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- Codex가 작업 전후 git 상태를 어떻게 다루는지 규칙을 `AI_WORKFLOW/02_AGENT_RULES/GIT_POLICY.md`로 정리(기존 규칙 재작성 아님, 요약 신설).

Project P.A.와의 연결: 완성 게임 — 안전.

수정 가능 파일: `AI_WORKFLOW/02_AGENT_RULES/GIT_POLICY.md` (신규)
수정 금지 파일: 모든 코드/씬/에셋.

구현 조건:
- push 금지, 승인 없는 커밋 금지, 파괴적 명령 금지, 작업 전 `git status` 확인, 남의 미커밋 변경 덮어쓰기 금지.

완료 조건: 문서만.
검증 방법: `CODEX_WORKER_RULES.md` §7과 일치.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: 없음

## Task 007 - 저장 스키마 현황 조사 문서

상태: DONE
단계: Phase 0
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- `SaveData.cs`, `SaveManager.cs`, `LocalJsonSaveRepository.cs`를 읽고 현재 저장 버전(v8 추정)과 저장 필드 목록을 `AI_WORKFLOW/03_TASKS/SAVE_SCHEMA.md`로 정리.

Project P.A.와의 연결: 저장 — 완성 게임의 지속성 기반.

수정 가능 파일: `AI_WORKFLOW/03_TASKS/SAVE_SCHEMA.md` (신규)
수정 금지 파일: `SaveData.cs`, `SaveManager.cs` 등 모든 코드(읽기만).

구현 조건:
- 현재 버전 번호, 마이그레이션 경로, 저장되는 데이터 항목을 표로. 추정은 "추정" 표시.

완료 조건: 문서만.
검증 방법: 실제 필드명과 대조.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 001

---

# Phase 1. MVP Stabilization

## Task 008 - 플레이어 이동/상호작용 회귀 확인

상태: DONE
단계: Phase 1. MVP Stabilization
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- `PlayerController`, `PlayerInteraction`, `PlayerInputHandler`가 현재 정상 동작하는지 코드 검토 + 컴파일 확인으로 확인하고 이상 항목을 `BUG_LOG.md`에 기록.

Project P.A.와의 연결: 낮 생활 — 이동/상호작용은 모든 활동의 기반.

수정 가능 파일:
- (조사만) `PlayerController.cs`, `PlayerInteraction.cs`, `PlayerInputHandler.cs` 읽기
- 이상 발견 시 기록: `AI_WORKFLOW/05_LOGS/BUG_LOG.md`

수정 금지 파일: `PlayerController.cs` 임의 재작성 금지 — 조사 단계.

구현 조건:
- 코드 수정 없이 조사. 실제 버그를 발견하면 별도 Task로 제안(직접 수정 금지).

완료 조건: 컴파일 영향 없음. 조사 결과 기록.
검증 방법: 컴파일 통과 확인 + 코드 리뷰 메모.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 003

## Task 009 - 인벤토리/핫바 회귀 확인

상태: PARTIAL
단계: Phase 1
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- `Inventory`, `InventorySlot`, `Hotbar`, `HotbarUI`, `InventoryUI`가 아이템 추가/선택/드래그에서 정상인지 조사하고 이상을 기록.

Project P.A.와의 연결: 낮 생활 + 상점 운영 — 재고 준비의 핵심.

수정 가능 파일: (조사만) 위 파일 읽기, 기록은 `BUG_LOG.md`.
수정 금지 파일: `Inventory.cs`(InventoryManager 역할) 임의 재작성 금지.

구현 조건: 코드 수정 없이 조사. 버그는 별도 Task로 제안.
완료 조건: 조사 결과 기록.
검증 방법: 코드 리뷰 + 컴파일 통과.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 003

## Task 010 - 상점 진열/가격 흐름 회귀 확인

상태: DONE
단계: Phase 1
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- `Shop`, `ShopSlot`, `ShopPriceUI`, `EconomyService` 흐름(진열→가격→구매→수익)이 정상인지 조사.

Project P.A.와의 연결: 상점 운영 — 핵심 루프.

수정 가능 파일: (조사만) 위 파일 읽기, 기록은 `BUG_LOG.md`.
수정 금지 파일: `Shop.cs`/`EconomyService.cs`/`PurchaseEvaluator.cs` 임의 재작성 금지.

구현 조건: 조사만.
완료 조건: 조사 결과 기록.
검증 방법: `VERIFICATION_RULES.md` §C 상점 루프 항목 대조.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 002

## Task 011 - 저장/불러오기 왕복 확인

상태: DONE
단계: Phase 1
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- F5 저장 → F9 로드 왕복 후 돈/인벤토리/진열/진행 상태가 보존되는지 실제 확인(가능하면 검증기/Play 모드).

Project P.A.와의 연결: 저장 — 완성 게임 지속성.

수정 가능 파일: (조사만) 결과는 `PROJECT_PA_STATUS.md`, `BUG_LOG.md`.
수정 금지 파일: `SaveManager.cs` 임의 재작성 금지.

구현 조건:
- Editor 열림이면 batchmode 금지 — 사람 Play 확인 요청.
- 실제 확인 못 하면 "확인 못 함"으로 기록(추정 금지).

완료 조건: 왕복 결과 기록 또는 보류 사유 기록.
검증 방법: 저장 전후 상태 비교.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 007

## Task 012 - D3D12 크래시 회피 가드 문서화

상태: PARTIAL
단계: Phase 1
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- Unity 자동 실행 시 D3D11 강제(`-force-d3d11`) 사용을 표준 절차로 `AI_WORKFLOW/04_VERIFICATION/UNITY_LAUNCH_POLICY.md`에 문서화.

Project P.A.와의 연결: 안정화.

수정 가능 파일: `AI_WORKFLOW/04_VERIFICATION/UNITY_LAUNCH_POLICY.md` (신규)
수정 금지 파일: 코드/설정 파일 수정 금지(문서만).

구현 조건:
- 루트 최신 크래시 리포트와 `crash-resolution.json` 근거 인용. ProjectSettings 수정 금지.

완료 조건: 문서만.
검증 방법: 크래시 리포트와 일치.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: 없음

## Task 013 - 콘솔 경고/에러 목록 스냅샷

상태: PARTIAL
단계: Phase 1
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 현재 씬 로드 시 콘솔 경고/에러를 조사해 `AI_WORKFLOW/03_TASKS/CONSOLE_SNAPSHOT.md`로 기록(우선순위 분류).

Project P.A.와의 연결: 안정화.

수정 가능 파일: `AI_WORKFLOW/03_TASKS/CONSOLE_SNAPSHOT.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 실제 확인 가능하면 로그 인용, 못 하면 보류 기록.
완료 조건: 문서만.
검증 방법: 콘솔 출력 인용.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 003

## Task 014 - 데모 안정성 스모크 체크리스트 작성

상태: PARTIAL
단계: Phase 1
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- Day 1 데모가 "깨지지 않았다"를 판정하는 스모크 체크리스트를 `AI_WORKFLOW/04_VERIFICATION/SMOKE_CHECKLIST.md`로 작성.

Project P.A.와의 연결: 시연 + 안정화.

수정 가능 파일: `AI_WORKFLOW/04_VERIFICATION/SMOKE_CHECKLIST.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: DEMO_FLOW 10단계를 체크 항목으로 변환.
완료 조건: 문서만.
검증 방법: DEMO_FLOW와 대조.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 002

## Task 015 - 첫 발견 버그 1개 수정 (있을 경우)

상태: DONE
단계: Phase 1
난이도: S
예상 Codex 실행 횟수: 2
사람 승인 필요: NO (단, 위험 파일 수정 필요 시 YES로 승격)

목표:
- Phase 1 조사(008~013)에서 나온 버그 중 위험도 낮은 것 1개만 수정.

Project P.A.와의 연결: 안정화.

수정 가능 파일: Codex가 작업 전 파일 탐색 후 보고 (버그별 상이).
수정 금지 파일: 위험 파일(PlayerController/SaveManager/Inventory/씬/프리팹) — 이들이 필요하면 사람 승인.

구현 조건: 한 버그만. 리팩터링 금지. 가장 작은 변경.
완료 조건: 컴파일 통과, 데모 흐름 유지, 해당 버그만 해결.
검증 방법: 재현 시나리오로 수정 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 008, 009, 010

## Task 016 - MVP Stabilization 완료 판정 기록

상태: DECISION_REQUIRED
단계: Phase 1
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: YES — 안정화 완료는 사람 판정 (Game view 가독성 포함)

목표:
- Phase 1 결과를 종합해 "MVP Stabilization 완료 여부"를 `DECISION_LOG.md`에 기록.

Project P.A.와의 연결: 안정화 → Stage 0 완료.

수정 가능 파일: `AI_WORKFLOW/05_LOGS/DECISION_LOG.md`, `PROJECT_PA_STATUS.md`
수정 금지 파일: 코드/씬.

구현 조건: 검증기 결과 + 스모크 결과 + 사람 가독성 확인 필요 여부 명시.
완료 조건: 판정 기록.
검증 방법: SMOKE_CHECKLIST 충족 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 011, 014, 015

---

# Phase 2. Shop Core

## Task 017 - 상품 카테고리 enum 정리/문서화

상태: PARTIAL
단계: Phase 2. Shop Core
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- `Item.cs`의 category(Raw/Processed/Luxury/Utility 등) 정의를 조사하고 카테고리별 의미를 `AI_WORKFLOW/03_TASKS/PRODUCT_CATEGORIES.md`로 문서화(코드 변경 없음).

Project P.A.와의 연결: 상점 운영 + 마을 변화(카테고리→변화).

수정 가능 파일: `AI_WORKFLOW/03_TASKS/PRODUCT_CATEGORIES.md` (신규)
수정 금지 파일: `Item.cs` (읽기만).

구현 조건: 마을 변화 연결(어느 카테고리가 어떤 변화)까지 표로.
완료 조건: 문서만.
검증 방법: `Item.cs` 실제 enum과 대조.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 001

## Task 018 - 상점 슬롯 재고 수량 표시

상태: DONE
단계: Phase 2
난이도: S
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- `ShopSlot`에 진열 수량이 UI로 읽히도록 `ShopPriceUI` 또는 슬롯 표시에 수량 텍스트 노출(기존 데이터 재사용, 새 시스템 금지).

Project P.A.와의 연결: 상점 운영.

수정 가능 파일: `ShopSlot.cs`, `Assets/Scripts/UI/ShopPriceUI.cs` (작업 전 탐색 후 확정 보고)
수정 금지 파일: `EconomyService.cs`, `PurchaseEvaluator.cs`, 씬/프리팹 대량 수정.

구현 조건: 기존 재고 필드 재사용. 읽기 표시만 추가.
완료 조건: 컴파일 통과, 수량이 화면에 표시, 데모 흐름 유지.
검증 방법: Play 모드에서 진열 후 수량 표시 확인(불가 시 보류 기록).
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 010

## Task 019 - 품절 상태 표시

상태: TODO
단계: Phase 2
난이도: S
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- 슬롯 재고가 0이 되면 "품절" 상태가 시각/텍스트로 읽히게 처리.

Project P.A.와의 연결: 상점 운영.

수정 가능 파일: `ShopSlot.cs`, `ShopOpenSign.cs` 또는 슬롯 UI (탐색 후 보고)
수정 금지 파일: 구매 판단 로직(`PurchaseEvaluator.cs`) 변경 금지, 씬 대량 수정 금지.

구현 조건: 상태 표시만. 판매 수학 변경 금지.
완료 조건: 컴파일 통과, 품절 표시 동작, 데모 유지.
검증 방법: 재고 소진 시나리오 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 018

## Task 020 - 영업 시작/종료 상태 명확화

상태: DONE
단계: Phase 2
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- `DayNightShopLoopController`의 영업 개시/종료 상태가 HUD로 명확히 읽히도록 표시 보강(기존 페이즈 상태 재사용).

Project P.A.와의 연결: 상점 운영(밤 영업 게이트).

수정 가능 파일: `DayNightShopLoopController.cs`, HUD 관련(`Assets/Scripts/UI/ClockHUD.cs`) (탐색 후 보고)
수정 금지 파일: `GameClock.cs`(Services) 임의 재작성 금지, 씬 대량 수정 금지.

구현 조건: 상태 표시 강화만. 페이즈 전환 로직 변경 금지.
완료 조건: 컴파일 통과, 영업 상태 가독, 데모 유지.
검증 방법: 낮/밤 전환 시 표시 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 010

## Task 021 - 영업 결과 요약(정산) 항목 점검·보강

상태: DONE
단계: Phase 2
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- 하루 정산 요약에 매출/판매 수/거절 수/다음 행동이 모두 나오는지 점검하고 누락 1개만 보강.

Project P.A.와의 연결: 상점 운영 + 마을 변화(정산이 다음날로 연결).

수정 가능 파일: 정산 UI/컨트롤러 (탐색 후 보고)
수정 금지 파일: `EconomyService.cs`, `SalesLogManager.cs` 로직 변경 금지(읽기 사용).

구현 조건: 표시 항목 1개만 보강. 계산식 변경 금지.
완료 조건: 컴파일 통과, 요약 항목 확인, 데모 유지.
검증 방법: 정산 화면 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 010

## Task 022 - 수익 계산 로그 투명화

상태: PARTIAL
단계: Phase 2
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 판매 성사 시 수익 계산 과정을 디버그 로그(이모지 관례)로 남겨 검증 가능하게(계산식 변경 금지).

Project P.A.와의 연결: 상점 운영 검증.

수정 가능 파일: `EconomyService.cs` 또는 `ShopSlot.cs`에 로그만 추가 (탐색 후 보고)
수정 금지 파일: 수익 계산식 자체 변경 금지.

구현 조건: 로그만 추가. 동작 변경 없음.
완료 조건: 컴파일 통과, 로그 출력, 데모 유지.
검증 방법: 판매 시 콘솔 로그 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 010

## Task 023 - 희귀품/일반품 구분 데이터 표시

상태: TODO
단계: Phase 2
난이도: S
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- 아이템 희귀도(있으면 재사용, 없으면 가격 기반 임시 구분)를 진열 UI에 표시.

Project P.A.와의 연결: 상점 운영(해질녘 진열 전략).

수정 가능 파일: `Item.cs`(읽기), 진열 UI (탐색 후 보고)
수정 금지 파일: 씬/프리팹 대량 수정, 구매 로직.

구현 조건: 데이터가 없으면 임의 생성 금지 — 가격 기반 파생만, "파생값" 명시.
완료 조건: 컴파일 통과, 구분 표시, 데모 유지.
검증 방법: 진열 시 표시 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 017

## Task 024 - 테마 코너 구성 설계 문서

상태: TODO
단계: Phase 2
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- "테마 코너(같은 카테고리 묶음 진열)" 기능을 어떻게 구현할지 설계만 `AI_WORKFLOW/03_TASKS/DESIGN_THEME_CORNER.md`로 작성(구현 아님).

Project P.A.와의 연결: 상점 운영 + 마을 변화(테마→트렌드).

수정 가능 파일: `AI_WORKFLOW/03_TASKS/DESIGN_THEME_CORNER.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 기존 슬롯 시스템 재사용 전제로 설계. 새 대형 시스템 제안 금지.
완료 조건: 문서만.
검증 방법: 설계가 기존 `ShopSlot` 구조와 호환되는지 자기검토.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 017

## Task 025 - 상품 가격 프리셋/추천가 표시

상태: PARTIAL
단계: Phase 2
난이도: S
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- `ShopPriceUI`에 기존 `IdealSellPrice`/basePrice 기반 추천가 힌트를 표시(있으면 재사용).

Project P.A.와의 연결: 상점 운영(가격 결정 보조).

수정 가능 파일: `Assets/Scripts/UI/ShopPriceUI.cs`, `Item.cs`(읽기) (탐색 후 보고)
수정 금지 파일: `PurchaseEvaluator.cs`, 씬 대량 수정.

구현 조건: 표시만. 가격 자동 설정 금지(플레이어 결정 유지).
완료 조건: 컴파일 통과, 추천가 표시, 데모 유지.
검증 방법: 가격 UI 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 010

## Task 026 - Shop Core 검증기 점검

상태: PARTIAL
단계: Phase 2
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- Phase 2 변경 후 `PA_DayNightShopLoopValidator`, `PA_CoreSlicePlayabilityValidator`로 상점 루프 회귀 확인.

Project P.A.와의 연결: 상점 운영 검증.

수정 가능 파일: 결과 기록만 (`PROJECT_PA_STATUS.md`, `BUG_LOG.md`)
수정 금지 파일: 코드/씬.

구현 조건: Editor 열림이면 batchmode 금지 — 보류 기록.
완료 조건: 검증기 결과 기록.
검증 방법: 검증기 출력.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 018, 019, 020, 021

---

# Phase 3. NPC Purchase Behavior

## Task 027 - NPC 선호 카테고리 표시 강화

상태: DONE
단계: Phase 3. NPC Purchase Behavior
난이도: S
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- `NpcProfile`의 traitSN 등 기존 데이터로 손님 선호 카테고리 힌트를 읽기 전용으로 표시 강화(SPY-002 레이어 재사용).

Project P.A.와의 연결: 상점 운영(손님 이해).

수정 가능 파일: 고객 프레젠테이션 컨트롤러(`CustomerDemandInsightController.cs` 등, 탐색 후 보고)
수정 금지 파일: `PurchaseEvaluator.cs`, `NpcProfile.cs` 데이터 변경.

구현 조건: 읽기 전용 표시. 실제 존재하는 데이터만(임의 취향 생성 금지).
완료 조건: 컴파일 통과, 선호 표시, 데모 유지.
검증 방법: `PA_CustomerPresentationValidator` 또는 Play 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 017

## Task 028 - 구매 확률 힌트 가독성 개선

상태: DONE
단계: Phase 3
난이도: S
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- `PurchaseEvaluator.Result`의 probability를 손님 말풍선/패널에 더 읽기 쉽게 표시(수학 변경 금지).

Project P.A.와의 연결: 상점 운영.

수정 가능 파일: 말풍선/패널 UI (탐색 후 보고)
수정 금지 파일: `PurchaseEvaluator.cs` 계산 로직.

구현 조건: 표시만.
완료 조건: 컴파일 통과, 확률 가독, 데모 유지.
검증 방법: Play 모드 구매 상황.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 027

## Task 029 - 비싼 가격 거절 대사 다양화

상태: PARTIAL
단계: Phase 3
난이도: S
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- 가격 부담으로 거절할 때 대사를 2~3종으로 다양화(기존 대사 데이터 구조 재사용, 대량 수정 금지).

Project P.A.와의 연결: 상점 운영(손님 반응 읽힘).

수정 가능 파일: 대사 표시 로직 또는 `DialogueData` 사용부 (탐색 후 보고)
수정 금지 파일: `DialogueService.cs` 대규모 변경, 씬 대량 수정.

구현 조건: 문구 추가만. 코지·밝은 톤 유지(어두운 톤 금지).
완료 조건: 컴파일 통과, 대사 다양화, 데모 유지.
검증 방법: 고가 진열 후 거절 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 028

## Task 030 - 희귀품 요청 손님 힌트

상태: DONE
단계: Phase 3
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- 일부 손님이 희귀품/특정 카테고리를 원한다는 힌트를 표시(기존 선호 데이터 기반, 새 요청 시스템 금지).

Project P.A.와의 연결: 상점 운영 + 마을 변화(수요 신호).

수정 가능 파일: 고객 수요 인사이트 컨트롤러 (탐색 후 보고)
수정 금지 파일: `PurchaseEvaluator.cs`, 저장 스키마.

구현 조건: 힌트 표시만. 데이터 없는 취향 생성 금지.
완료 조건: 컴파일 통과, 힌트 표시, 데모 유지.
검증 방법: `PA_CustomerDemandInsightValidator`.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 023, 027

## Task 031 - 단골/관광객 구분 라벨 (표시)

상태: TODO
단계: Phase 3
난이도: M
예상 Codex 실행 횟수: 3
사람 승인 필요: NO

목표:
- 손님을 단골(주민)/관광객으로 구분하는 라벨을 표시(기존 NPC 분류 재사용, 없으면 간단 플래그 파생).

Project P.A.와의 연결: 상점 운영(손님 계층).

수정 가능 파일: `NpcController.cs`(읽기), `NpcProfile.cs`(읽기), 표시 UI (탐색 후 보고)
수정 금지 파일: `NpcController.cs` FSM 대규모 변경, 구매 로직.

구현 조건: 표시/파생만. 구매 수학 변경 금지.
완료 조건: 컴파일 통과, 라벨 표시, 데모 유지.
검증 방법: Play 모드 손님 관찰.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 027

## Task 032 - 영업 중 손님 흐름 조사 문서

상태: PARTIAL
단계: Phase 3
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- `CustomerArrivalController`, `NpcScheduleController`가 밤 영업 중 손님을 어떻게 유입시키는지 조사해 `AI_WORKFLOW/03_TASKS/CUSTOMER_FLOW.md`로 정리.

Project P.A.와의 연결: 상점 운영.

수정 가능 파일: `AI_WORKFLOW/03_TASKS/CUSTOMER_FLOW.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 조사만.
완료 조건: 문서만.
검증 방법: 실제 코드 흐름과 대조.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 001

## Task 033 - 손님 유입 속도 튜닝 노출

상태: DECISION_REQUIRED
단계: Phase 3
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: YES — 손님 유입 속도는 밸런스 변경 (사람 승인)

목표:
- `CustomerArrivalController`의 유입 간격을 Inspector 노출 필드로만 조정 가능하게(기본값 변경은 사람 승인).

Project P.A.와의 연결: 상점 운영(밤의 붐빔).

수정 가능 파일: `CustomerArrivalController.cs` (탐색 후 보고)
수정 금지 파일: 씬/프리팹 대량 수정, 구매 로직.

구현 조건: `[SerializeField]` 노출만. 기존 직렬화 필드 이름 보존. 기본 밸런스 값 변경은 사람 승인 후.
완료 조건: 컴파일 통과, 데모 유지.
검증 방법: Inspector 노출 확인 + Play.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 032

## Task 034 - 구매/거절 통계 집계 로그

상태: PARTIAL
단계: Phase 3
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 하루 동안 구매/거절 수를 `SalesLogManager`가 이미 기록하는지 조사하고, 없으면 로그만 추가(저장 스키마 변경 금지).

Project P.A.와의 연결: 상점 운영 + 마을 변화(통계).

수정 가능 파일: `SalesLogManager.cs`에 로그만 (탐색 후 보고)
수정 금지 파일: `SaveData.cs` 스키마.

구현 조건: 로그/집계만. 저장 확장은 Phase 6에서.
완료 조건: 컴파일 통과, 집계 로그, 데모 유지.
검증 방법: 판매 후 로그 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 032

## Task 035 - NPC Purchase 검증기 회귀

상태: DONE
단계: Phase 3
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- `PA_CustomerArrivalValidator`, `PA_CustomerPresentationValidator`, `PA_CustomerDemandInsightValidator`로 Phase 3 회귀 확인.

Project P.A.와의 연결: 상점 운영 검증.

수정 가능 파일: 결과 기록만.
수정 금지 파일: 코드/씬.

구현 조건: Editor 열림이면 보류 기록.
완료 조건: 검증기 결과 기록.
검증 방법: 검증기 출력.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 027, 030, 031

---

# Phase 4. Daytime Life Loop

## Task 036 - 낮 활동 구조 조사 문서

상태: PARTIAL
단계: Phase 4. Daytime Life Loop
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 기존 낮 활동(`DaytimeStockPrepPoint`, `Gatherable`, `Crop`, `Farmland`, `Workbench`)이 재고로 이어지는 구조를 `AI_WORKFLOW/03_TASKS/DAYTIME_ACTIVITIES.md`로 정리.

Project P.A.와의 연결: 낮 생활 → 상점 재고.

수정 가능 파일: `AI_WORKFLOW/03_TASKS/DAYTIME_ACTIVITIES.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 조사만. 낚시 부재를 명시(Phase 4의 우선 구현 대상).
완료 조건: 문서만.
검증 방법: 실제 코드와 대조.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 001

## Task 037 - 채집 포인트 재사용성 점검

상태: DONE
단계: Phase 4
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- `DaytimeStockPrepPoint` 5종(IL-001)이 하루 1회 리셋/재생성 정상인지 조사하고 이상 기록.

Project P.A.와의 연결: 낮 생활.

수정 가능 파일: (조사만) 결과는 `BUG_LOG.md`.
수정 금지 파일: `DayNightShopLoopController.cs` 대규모 변경.

구현 조건: 조사만.
완료 조건: 조사 결과 기록.
검증 방법: `PA_GatheringShopGateValidator`.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 036

## Task 038 - 낚시 아이템 데이터 설계 문서 (낚시 1/5)

상태: TODO
단계: Phase 4
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 낚시로 얻을 물고기/낚시용품 아이템을 기존 `Item`/`ItemInstance` 구조로 어떻게 정의할지 `AI_WORKFLOW/03_TASKS/DESIGN_FISHING.md`로 설계(구현 아님).

Project P.A.와의 연결: 낮 생활(낚시) — Vertical Slice 대표 루프.
DECISION_REQUIRED: Vertical Slice 두 번째 낮 활동 = 낚시(임시 기본값).

수정 가능 파일: `AI_WORKFLOW/03_TASKS/DESIGN_FISHING.md` (신규)
수정 금지 파일: 코드/씬/에셋.

구현 조건: 기존 아이템 카테고리 재사용 전제. 새 대형 시스템 금지.
완료 조건: 문서만.
검증 방법: `Item.cs` 구조와 호환성 자기검토.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 017, 036

## Task 039 - 낚시 상호작용 포인트 (낚시 2/5)

상태: PARTIAL
단계: Phase 4
난이도: M
예상 Codex 실행 횟수: 3
사람 승인 필요: NO

목표:
- 해변에 낚시 상호작용 포인트를 `DaytimeStockPrepPoint` 패턴(런타임 사이드카 생성)으로 추가해 물고기 아이템을 지급.

Project P.A.와의 연결: 낮 생활(낚시).

수정 가능 파일: 새 스크립트 `Assets/Scripts/FishingSpot.cs`(신규) 또는 기존 패턴 확장 (작업 전 보고)
수정 금지 파일: 씬 직렬화 임의 변경(런타임 생성 사용), `PlayerController.cs`, `SaveManager.cs`.

구현 조건: `IInteractable` 재사용. 씬 파일 수정 없이 런타임 배치. 하루 1회 리셋.
완료 조건: 컴파일 통과, 낚시로 아이템 획득, 데모 유지.
검증 방법: Play 모드 낚시 → 인벤토리 반영 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 038

## Task 040 - 낚시 미니 상호작용 피드백 (낚시 3/5)

상태: TODO
단계: Phase 4
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- 낚시 시 간단한 대기/성공 피드백(프롬프트/로그/간단 타이밍)을 추가(복잡한 미니게임 금지).

Project P.A.와의 연결: 낮 생활(코지 감각).

수정 가능 파일: `FishingSpot.cs`(신규 파일 확장)
수정 금지 파일: 씬 대량 수정, 입력 시스템 재작성.

구현 조건: 최소 피드백만. 외부 패키지 금지. 코지 톤.
완료 조건: 컴파일 통과, 피드백 동작, 데모 유지.
검증 방법: Play 모드 낚시 피드백 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 039

## Task 041 - 낚시 결과가 상점 재고로 연결 (낚시 4/5)

상태: PARTIAL
단계: Phase 4
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- 낚시로 얻은 물고기가 `ShopSlot`에 진열/판매 가능한지 확인하고 누락 연결만 보강.

Project P.A.와의 연결: 낮 생활 → 상점 운영(핵심 연결).

수정 가능 파일: `FishingSpot.cs`, 필요 시 아이템 등록부 (탐색 후 보고)
수정 금지 파일: `Shop.cs`/`ShopSlot.cs` 대규모 변경, `EconomyService.cs`.

구현 조건: 기존 진열 흐름 재사용. 아이템 메타 정상 보장.
완료 조건: 컴파일 통과, 낚시→진열→판매 성립, 데모 유지.
검증 방법: 낚시→진열→NPC 구매 왕복.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 039, Task 010

## Task 042 - 낚시 검증기/스모크 추가 (낚시 5/5)

상태: PARTIAL
단계: Phase 4
난이도: S
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- 낚시→진열→판매를 확인하는 스모크 항목을 `SMOKE_CHECKLIST.md`에 추가(신규 에디터 검증기는 선택).

Project P.A.와의 연결: 낮 생활 검증.

수정 가능 파일: `AI_WORKFLOW/04_VERIFICATION/SMOKE_CHECKLIST.md`, (선택)`Assets/Editor/PA_FishingFlowValidator.cs`(신규)
수정 금지 파일: 기존 검증기 대규모 변경, 씬.

구현 조건: 체크리스트 우선. 에디터 검증기는 기존 `PA_` 패턴 따를 때만.
완료 조건: 체크리스트 갱신 + (있으면) 검증기 컴파일 통과.
검증 방법: 스모크 실행.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 041

## Task 043 - 광질/농사 확장 후보 설계 문서

상태: TODO
단계: Phase 4
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 기존 `Crop`/`Farmland`(농사)와 광질 후보를 낚시 이후 확장안으로 `AI_WORKFLOW/03_TASKS/DESIGN_MINING_FARMING.md`에 설계.

Project P.A.와의 연결: 낮 생활 확장.

수정 가능 파일: `AI_WORKFLOW/03_TASKS/DESIGN_MINING_FARMING.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 기존 `Crop`/`Farmland` 재사용 전제. 설계만.
완료 조건: 문서만.
검증 방법: 기존 농사 코드와 호환성 자기검토.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 036

## Task 044 - 주민 의뢰(간단 요청) 표시 설계

상태: TODO
단계: Phase 4
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 주민이 특정 아이템을 원하는 간단 의뢰를 기존 대사/수요 데이터로 표시하는 설계를 `AI_WORKFLOW/03_TASKS/DESIGN_RESIDENT_REQUEST.md`로 작성.

Project P.A.와의 연결: 낮 생활 + 상점 운영(수요).

수정 가능 파일: `AI_WORKFLOW/03_TASKS/DESIGN_RESIDENT_REQUEST.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 새 퀘스트 엔진 금지. 기존 시스템 재사용 설계만.
완료 조건: 문서만.
검증 방법: 기존 대사/수요 구조와 호환성 검토.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 030

## Task 045 - 낮 활동 결과→재고 연결 요약 갱신

상태: PARTIAL
단계: Phase 4
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 채집+낚시가 재고로 이어지는 전체 그림을 `DAYTIME_ACTIVITIES.md`에 갱신하고 `GAME_LOOP.md` 상태 표를 최신화.

Project P.A.와의 연결: 낮 생활 → 상점 운영.

수정 가능 파일: `AI_WORKFLOW/03_TASKS/DAYTIME_ACTIVITIES.md`, `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_GAME_LOOP.md`
수정 금지 파일: 코드/씬.

구현 조건: 문서만. 구현 상태 정확히 반영(추정 금지).
완료 조건: 문서 갱신.
검증 방법: 실제 구현과 대조.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 041

---

# Phase 5. Village Trend System

## Task 046 - 카테고리별 판매 통계 조사

상태: PARTIAL
단계: Phase 5. Village Trend System
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- `SalesLogManager`, `VillageChangeSignalController`가 카테고리별 판매를 어떻게 집계하는지 `AI_WORKFLOW/03_TASKS/VILLAGE_TREND.md`로 조사 정리.

Project P.A.와의 연결: 마을 변화(핵심 차별점).

수정 가능 파일: `AI_WORKFLOW/03_TASKS/VILLAGE_TREND.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 조사만.
완료 조건: 문서만.
검증 방법: 실제 코드와 대조.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 001, 034

## Task 047 - 트렌드 점수(낚시/캠핑/가구) 데이터 설계

상태: PARTIAL
단계: Phase 5
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 카테고리 판매 누적을 "마을 트렌드 점수"로 환산하는 규칙을 설계 문서로 작성(구현은 다음 Task).

Project P.A.와의 연결: 마을 변화.

수정 가능 파일: `AI_WORKFLOW/03_TASKS/VILLAGE_TREND.md` (갱신)
수정 금지 파일: 코드/씬.

구현 조건: 기존 `VillageChangeSignalController` 확장 전제. 설계만.
완료 조건: 문서만.
검증 방법: 기존 신호 컨트롤러와 호환성 검토.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 046

## Task 048 - 트렌드 점수 누적 구현 (읽기 신호)

상태: DONE
단계: Phase 5
난이도: M
예상 Codex 실행 횟수: 3
사람 승인 필요: NO

목표:
- `VillageChangeSignalController`에 카테고리별 누적 점수(읽기 전용 신호)를 추가(저장은 Phase 6).

Project P.A.와의 연결: 마을 변화.

수정 가능 파일: `VillageChangeSignalController.cs` (탐색 후 보고)
수정 금지 파일: `SaveData.cs`(이번엔 저장 안 함), `EconomyService.cs`, 씬 대량 수정.

구현 조건: 런타임 집계만. 기존 판매 기록 재사용. 마을 정체성 훼손 금지.
완료 조건: 컴파일 통과, 점수 집계 로그/표시, 데모 유지.
검증 방법: 판매 후 점수 증가 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 047

## Task 049 - 조건 달성 시 마을 변화 트리거 (표시)

상태: DONE
단계: Phase 5
난이도: M
예상 Codex 실행 횟수: 3
사람 승인 필요: NO

목표:
- 트렌드 점수가 임계값을 넘으면 다음날 마을 변화 신호를 발생(VC-001A `VillageCultureVisualController` 패턴 재사용).

Project P.A.와의 연결: 마을 변화(핵심 차별점).

수정 가능 파일: `VillageCultureVisualController.cs` 확장 또는 신규 사이드카 (탐색 후 보고)
수정 금지 파일: 씬 직렬화 임의 변경(런타임 생성 사용), `SaveManager.cs`.

구현 조건: VC-001A와 동일한 런타임 사이드카 패턴. 코지 시각. 새 대형 시스템 금지.
완료 조건: 컴파일 통과, 조건 달성 시 변화, 데모 유지.
검증 방법: `PA_VillageCultureVisualValidator` 또는 Play 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 048

## Task 050 - 주민 행동 변화(간단) 표시

상태: DECISION_REQUIRED
단계: Phase 5
난이도: M
예상 Codex 실행 횟수: 3
사람 승인 필요: YES — NPC 행동 변경은 구매 흐름 영향 가능 (사람 승인)

목표:
- 특정 트렌드 달성 시 주민이 해변/광장에 더 모이는 등 가벼운 행동 변화를 표시(구매 수학 변경 금지).

Project P.A.와의 연결: 마을 변화 + NPC 생활.

수정 가능 파일: `NpcScheduleController.cs` 또는 사이드카 (탐색 후 보고)
수정 금지 파일: `NpcController.cs` FSM 대규모 변경, `PurchaseEvaluator.cs`.

구현 조건: 가벼운 배치/이동 변화만. 밸런스/구매 영향 시 사람 승인.
완료 조건: 컴파일 통과, 행동 변화 관찰, 데모 유지.
검증 방법: 조건 달성 후 NPC 관찰.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 049

## Task 051 - 시설 해금 신호(표시) 연결

상태: PARTIAL
단계: Phase 5
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- 트렌드 달성이 시설 해금 방향을 예고하는 텍스트/신호를 티어/감사 UI에 표시(실제 해금 로직은 Phase 9).

Project P.A.와의 연결: 마을 변화 → 성장.

수정 가능 파일: `AuditResultUI.cs` 또는 정산 UI (탐색 후 보고)
수정 금지 파일: `TierService.cs` 로직 변경, 저장 스키마.

구현 조건: 예고 표시만.
완료 조건: 컴파일 통과, 예고 표시, 데모 유지.
검증 방법: 조건 달성 후 UI 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 049

## Task 052 - 이벤트 해금 후보 설계 문서

상태: TODO
단계: Phase 5
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 낚시 대회 등 트렌드 기반 이벤트 후보를 `AI_WORKFLOW/03_TASKS/DESIGN_EVENTS.md`로 설계(구현 아님).

Project P.A.와의 연결: 마을 변화(핵심 차별점 강화).

수정 가능 파일: `AI_WORKFLOW/03_TASKS/DESIGN_EVENTS.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 낚시 대표 루프와 연결. 설계만.
완료 조건: 문서만.
검증 방법: 기존 시스템 호환성 검토.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 047

## Task 053 - Village Trend 검증기/스모크 추가

상태: DONE
단계: Phase 5
난이도: S
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- "판매→트렌드→다음날 변화" 최소 증거를 확인하는 스모크 항목 추가.

Project P.A.와의 연결: 마을 변화 검증.

수정 가능 파일: `AI_WORKFLOW/04_VERIFICATION/SMOKE_CHECKLIST.md`, (선택) 검증기 신규
수정 금지 파일: 씬, 기존 검증기 대규모 변경.

구현 조건: 체크리스트 우선.
완료 조건: 체크리스트 갱신.
검증 방법: 스모크 실행.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 049

---

# Phase 6. Save & Persistence

## Task 054 - 판매 통계 저장 확장 설계

상태: TODO
단계: Phase 6. Save & Persistence
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 카테고리별 판매 통계를 저장 스키마에 추가하는 방법을 v8→v9 추가 확장 패턴으로 `SAVE_SCHEMA.md`에 설계(구현 아님).

Project P.A.와의 연결: 저장 + 마을 변화 지속성.

수정 가능 파일: `AI_WORKFLOW/03_TASKS/SAVE_SCHEMA.md` (갱신)
수정 금지 파일: `SaveData.cs`, `SaveManager.cs` (이번엔 설계만).

구현 조건: 추가 확장만(필드 제거/의미 변경 금지). 마이그레이션 경로 명시.
완료 조건: 문서만.
검증 방법: 기존 스키마 버전 규칙과 일치.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 007, 048

## Task 055 - 판매 통계 저장 구현

상태: DECISION_REQUIRED
단계: Phase 6
난이도: M
예상 Codex 실행 횟수: 3
사람 승인 필요: YES — 저장 스키마 변경 (사람 승인)

목표:
- Task 054 설계대로 판매 통계 필드를 저장 스키마에 추가(v9), 마이그레이션 포함.

Project P.A.와의 연결: 저장.

수정 가능 파일: `SaveData.cs`, `SaveManager.cs` (작업 전 보고, 사람 승인 후)
수정 금지 파일: 기존 저장 필드 제거/개명, 씬.

구현 조건: 추가 확장만. `[SerializeField]` 이름 보존. 구버전 세이브 로드 호환.
완료 조건: 컴파일 통과, 저장/로드 왕복 성공, 구버전 호환.
검증 방법: 저장→로드→통계 보존 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 054

## Task 056 - 상점 상태 저장 확인

상태: PARTIAL
단계: Phase 6
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- 진열/가격/재고가 저장·복원되는지 확인하고 누락 1개만 보강(스키마 추가 시 사람 승인).

Project P.A.와의 연결: 저장 + 상점 운영.

수정 가능 파일: `SaveManager.cs` 사용부 (탐색 후 보고)
수정 금지 파일: 기존 저장 필드 개명, 씬.

구현 조건: 최소 보강. 스키마 추가 필요 시 사람 승인.
완료 조건: 컴파일 통과, 상점 상태 왕복, 데모 유지.
검증 방법: 진열 후 저장/로드 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 055

## Task 057 - 마을 변화 상태 저장

상태: DECISION_REQUIRED
단계: Phase 6
난이도: M
예상 Codex 실행 횟수: 3
사람 승인 필요: YES — 저장 스키마 변경 (사람 승인)

목표:
- 트렌드 점수/마을 변화 상태를 저장해 다음 세션에도 유지(Task 048/049 결과 지속화).

Project P.A.와의 연결: 마을 변화 지속성(핵심 차별점의 영속화).

수정 가능 파일: `SaveData.cs`, `VillageChangeSignalController.cs` 저장 연결 (사람 승인 후)
수정 금지 파일: 기존 저장 필드 개명, 씬.

구현 조건: 추가 확장만. 마이그레이션 포함.
완료 조건: 컴파일 통과, 마을 변화 왕복 보존.
검증 방법: 변화 발생 후 저장/로드 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 049, 055

## Task 058 - NPC 상태 저장 회귀 확인

상태: PARTIAL
단계: Phase 6
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- NPC 친밀도/고용/스케줄 상태가 기존 저장으로 복원되는지 회귀 확인(11주차 v4~ 패턴 참고).

Project P.A.와의 연결: 저장 + NPC 생활.

수정 가능 파일: (조사만) 결과는 `BUG_LOG.md`.
수정 금지 파일: `SaveManager.cs` 대규모 변경, `NpcController.cs`.

구현 조건: 조사만. 버그는 별도 Task.
완료 조건: 조사 결과 기록.
검증 방법: NPC 상태 저장/로드 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 007

## Task 059 - 저장 회귀 테스트 체크리스트

상태: TODO
단계: Phase 6
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 저장/불러오기 회귀 테스트 항목을 `AI_WORKFLOW/04_VERIFICATION/SAVE_REGRESSION.md`로 정리.

Project P.A.와의 연결: 저장 안정성.

수정 가능 파일: `AI_WORKFLOW/04_VERIFICATION/SAVE_REGRESSION.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 문서만. 구버전→신버전 호환 항목 포함.
완료 조건: 문서만.
검증 방법: SAVE_SCHEMA와 일치.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 055

## Task 060 - 저장 버전 관리 정책 문서

상태: PARTIAL
단계: Phase 6
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 저장 버전 증가 규칙(추가 확장만, 마이그레이션 필수)을 `AI_WORKFLOW/04_VERIFICATION/SAVE_VERSION_POLICY.md`로 명문화.

Project P.A.와의 연결: 저장 안정성.

수정 가능 파일: `AI_WORKFLOW/04_VERIFICATION/SAVE_VERSION_POLICY.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 문서만.
완료 조건: 문서만.
검증 방법: `UNITY_CODING_RULES.md` §2와 일치.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 054

---

# Phase 7. Graduation Demo Milestone

## Task 061 - 5분 시연 루트 대본 작성

상태: DONE
단계: Phase 7. Graduation Demo Milestone
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 낮 채집/낚시 → 진열/가격 → 밤 영업 → 정산 → 마을 변화까지 5분 시연 대본을 `AI_WORKFLOW/03_TASKS/DEMO_SCRIPT_5MIN.md`로 작성.

Project P.A.와의 연결: 시연(Graduation Demo).

수정 가능 파일: `AI_WORKFLOW/03_TASKS/DEMO_SCRIPT_5MIN.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 실제 구현된 기능만 사용(미구현 기능 시연 금지).
완료 조건: 문서만.
검증 방법: DEMO_FLOW와 대조.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 002, 041, 049

## Task 062 - 데모 안정화 버그 스윕

상태: DONE
단계: Phase 7
난이도: M
예상 Codex 실행 횟수: 3
사람 승인 필요: NO

목표:
- 5분 루트에서 나오는 치명 버그만 1개씩 순차 수정(새 기능 금지).

Project P.A.와의 연결: 시연 안정화.

수정 가능 파일: 버그별 상이 (탐색 후 보고)
수정 금지 파일: 위험 파일 대규모 변경, 새 기능 추가.

구현 조건: 한 번에 버그 1개. 리팩터링 금지.
완료 조건: 컴파일 통과, 루트 안정, 데모 유지.
검증 방법: 5분 루트 재실행.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 061

## Task 063 - 발표용 체크리스트 작성

상태: DONE
단계: Phase 7
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 발표 당일 점검 체크리스트를 `AI_WORKFLOW/03_TASKS/DEMO_DAY_CHECKLIST.md`로 작성.

Project P.A.와의 연결: 시연.

수정 가능 파일: `AI_WORKFLOW/03_TASKS/DEMO_DAY_CHECKLIST.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 빌드/실행/백업 녹화/조작키 안내 포함.
완료 조건: 문서만.
검증 방법: SMOKE_CHECKLIST와 연계.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 061

## Task 064 - Windows 빌드 생성·실행 리허설

상태: DECISION_REQUIRED
단계: Phase 7
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: YES — 빌드/실행은 Unity 실행 필요, D3D11 방식 확인

목표:
- Windows 빌드를 생성하고 실행 리허설 결과를 `PROJECT_PA_STATUS.md`에 기록.

Project P.A.와의 연결: 시연.

수정 가능 파일: 결과 기록만.
수정 금지 파일: 코드/씬(빌드 산출물만).

구현 조건: Editor 열림이면 조정. D3D11 baseline. 빌드 실패 시 원인 기록.
완료 조건: 빌드 산출 또는 실패 원인 기록.
검증 방법: `Builds/Windows/Project_PA.exe` 실행.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 062

## Task 065 - 발표 중 실패 대비 플랜

상태: DONE
단계: Phase 7
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 실시간 시연 실패 시 백업 녹화본/스크린샷 사용 플랜을 `DEMO_DAY_CHECKLIST.md`에 추가.

Project P.A.와의 연결: 시연 안정성.

수정 가능 파일: `AI_WORKFLOW/03_TASKS/DEMO_DAY_CHECKLIST.md` (갱신)
수정 금지 파일: 코드/씬.

구현 조건: 문서만.
완료 조건: 문서 갱신.
검증 방법: 자기검토.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 063

## Task 066 - Demo Lock 기준 정의

상태: DECISION_REQUIRED
단계: Phase 7
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: YES — Demo Lock 선언은 사람 결정 (DECISION_REQUIRED: 발표 날짜)

목표:
- "이 시점 이후 새 기능 금지, 검증/버그수정만"의 Demo Lock 기준을 `AI_WORKFLOW/03_TASKS/DEMO_LOCK.md`로 정의.

Project P.A.와의 연결: 시연.

수정 가능 파일: `AI_WORKFLOW/03_TASKS/DEMO_LOCK.md` (신규), `DECISION_LOG.md`
수정 금지 파일: 코드/씬.

구현 조건: `DEVELOPMENT_TIMELINE.md` 역산표와 연계. 날짜는 사람 확정.
완료 조건: 문서만.
검증 방법: 타임라인과 일치.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 061

## Task 067 - Graduation Demo 완료 판정 기록

상태: DECISION_REQUIRED
단계: Phase 7
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: YES — 시연 완료는 사람 판정

목표:
- Graduation Demo Milestone 달성 여부를 `DECISION_LOG.md`에 기록.

Project P.A.와의 연결: 시연 → Stage 2 완료.

수정 가능 파일: `AI_WORKFLOW/05_LOGS/DECISION_LOG.md`, `PROJECT_PA_STATUS.md`
수정 금지 파일: 코드/씬.

구현 조건: 검증기+빌드+대본 충족 확인.
완료 조건: 판정 기록.
검증 방법: DEMO_SCRIPT_5MIN 충족.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 064, 066

---

# Phase 8. Vertical Slice

## Task 068 - 1일 완전 루프 통합 점검

상태: DECISION_REQUIRED
단계: Phase 8. Vertical Slice
난이도: L
예상 Codex 실행 횟수: 3
사람 승인 필요: YES — 여러 시스템 통합, 데모 흐름 영향

목표:
- 낮 활동 → 재고 → 밤 영업 → 판매 통계 → 다음날 마을 변화까지 1일 루프가 끊김 없이 도는지 통합 점검하고 끊긴 연결 1개만 보강.

Project P.A.와의 연결: 낮 생활 + 상점 운영 + 마을 변화(핵심 재미 압축).

수정 가능 파일: 연결부 (탐색 후 보고, 사람 승인)
수정 금지 파일: 위험 파일 대규모 변경.

구현 조건: 연결 보강 1개만. 새 시스템 금지.
완료 조건: 컴파일 통과, 1일 루프 성립, 데모 유지.
검증 방법: 1일 루프 전체 Play.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 041, 049, 057

## Task 069 - 낚시 대표 루프 강화

상태: BLOCKED
단계: Phase 8
난이도: M
예상 Codex 실행 횟수: 3
사람 승인 필요: NO

목표:
- 낚시→낚시용품/물고기 판매→해변 트렌드→다음날 변화가 직관적으로 읽히게 표시/연결 보강.

Project P.A.와의 연결: 낮 생활(낚시 대표 루프) + 마을 변화.

수정 가능 파일: `FishingSpot.cs`, `VillageChangeSignalController.cs` 표시부 (탐색 후 보고)
수정 금지 파일: 구매 로직, 저장 스키마(이미 Phase 6에서), 씬 대량.

구현 조건: 표시/연결 강화만.
완료 조건: 컴파일 통과, 낚시 루프 가독, 데모 유지.
검증 방법: 낚시 루프 Play.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 068

## Task 070 - 보조 루프(캠핑/가구 중 1) 설계

상태: BLOCKED
단계: Phase 8
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 캠핑 또는 가구 중 하나를 보조 루프로 선택하는 설계를 `AI_WORKFLOW/03_TASKS/DESIGN_SECONDARY_LOOP.md`로 작성(구현 아님).

Project P.A.와의 연결: 상점 운영 + 마을 변화(보조 재미).

수정 가능 파일: `AI_WORKFLOW/03_TASKS/DESIGN_SECONDARY_LOOP.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 기존 가구(`BuildingData`/`Chair`/`StorageBox`) 재사용 검토. 설계만.
완료 조건: 문서만.
검증 방법: 기존 시스템 호환성 검토.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 068

## Task 071 - Vertical Slice 재미 체감 점검(사람)

상태: DECISION_REQUIRED
단계: Phase 8
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: YES — 재미 판정은 사람

목표:
- Vertical Slice가 "핵심 재미(SCOPE §8)"를 전달하는지 사람 플레이 피드백을 `AI_WORKFLOW/03_TASKS/SLICE_FEEDBACK.md`로 수집.

Project P.A.와의 연결: 핵심 재미 검증.

수정 가능 파일: `AI_WORKFLOW/03_TASKS/SLICE_FEEDBACK.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 사람 피드백 수집. Codex는 정리만.
완료 조건: 피드백 기록.
검증 방법: SCOPE §8 항목 대조.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 069

## Task 072 - Vertical Slice 완료 판정

상태: DECISION_REQUIRED
단계: Phase 8
난이도: XS
예상 Codex 실행 횟수: 1
사람 승인 필요: YES — 사람 판정

목표:
- Vertical Slice 달성 여부를 `DECISION_LOG.md`에 기록.

Project P.A.와의 연결: Stage 1(Vertical Slice) 완료.

수정 가능 파일: `AI_WORKFLOW/05_LOGS/DECISION_LOG.md`, `PROJECT_PA_STATUS.md`
수정 금지 파일: 코드/씬.

구현 조건: 판정 근거 명시.
완료 조건: 판정 기록.
검증 방법: SLICE_FEEDBACK + 스모크.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 071

---

# Phase 9. Full Game Completion

> Phase 9의 모든 신규 시스템 착수는 기본적으로 사람 승인이 필요하다(대형 시스템/밸런스/씬 변경). Vertical Slice를 깨지 않는 확장만 허용.

## Task 073 - 계절/날씨 시스템 확장 설계

상태: DECISION_REQUIRED
단계: Phase 9. Full Game Completion
난이도: L
예상 Codex 실행 횟수: 2
사람 승인 필요: YES — 새 대형 시스템

목표:
- 기존 `SeasonModifier` 재사용 전제로 계절/날씨 확장 설계를 `AI_WORKFLOW/03_TASKS/DESIGN_SEASON_WEATHER.md`로 작성(구현 아님).

Project P.A.와의 연결: 완성 게임(장기 지속).

수정 가능 파일: `AI_WORKFLOW/03_TASKS/DESIGN_SEASON_WEATHER.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 기존 `SeasonModifier` 확장. Vertical Slice 비파괴.
완료 조건: 문서만.
검증 방법: 기존 시스템 호환성 검토.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 072

## Task 074 - 추가 주민 데이터 확장

상태: DECISION_REQUIRED
단계: Phase 9
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: YES — 콘텐츠/밸런스

목표:
- 기존 `NpcProfile`/`NpcCandidateData` 구조로 주민 1명 추가(데이터만, 씬 배치는 사람 승인).

Project P.A.와의 연결: NPC 생활 확장.

수정 가능 파일: SO 에셋 생성은 사람 승인 후 (탐색 후 보고)
수정 금지 파일: `NpcController.cs`, 씬 대량 수정.

구현 조건: 기존 구조 재사용. 데이터 추가만.
완료 조건: 컴파일 통과, 데모 유지.
검증 방법: 신규 주민 로드 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 073

## Task 075 - 추가 상품군 확장

상태: DECISION_REQUIRED
단계: Phase 9
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: YES — 콘텐츠/밸런스

목표:
- 기존 아이템 카테고리로 신규 상품군 1종 추가(데이터/등록만).

Project P.A.와의 연결: 상점 운영 확장.

수정 가능 파일: 아이템 데이터/등록부 (사람 승인 후)
수정 금지 파일: `Item.cs` 구조 변경, 씬.

구현 조건: 기존 구조 재사용.
완료 조건: 컴파일 통과, 진열/판매 성립.
검증 방법: 신규 상품 판매 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 072

## Task 076 - 가게 확장 1단계 구현

상태: DECISION_REQUIRED
단계: Phase 9
난이도: L
예상 Codex 실행 횟수: 3
사람 승인 필요: YES — 성장 시스템, 씬/저장 영향

목표:
- 슬롯 증가 또는 시설 해금 1단계를 기존 `TierService`/`Shop` 확장으로 구현.

Project P.A.와의 연결: 성장(작은 좌판→상점).

수정 가능 파일: `Shop.cs`, `TierService.cs` 확장 (사람 승인 후)
수정 금지 파일: 저장 필드 개명, 씬 임의 재작성.

구현 조건: 기존 시스템 확장. 저장 추가 시 v증가+마이그레이션.
완료 조건: 컴파일 통과, 확장 동작, 저장 왕복.
검증 방법: 확장 조건 달성 후 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 051, 072

## Task 077 - 마을 시설 확장(시각) 1종

상태: DECISION_REQUIRED
단계: Phase 9
난이도: L
예상 Codex 실행 횟수: 3
사람 승인 필요: YES — 씬/시각 변경

목표:
- 트렌드 기반 마을 시설 1종을 VC-001A 런타임 사이드카 패턴으로 추가.

Project P.A.와의 연결: 마을 변화(핵심 차별점 확장).

수정 가능 파일: 사이드카 컨트롤러 확장 (사람 승인 후)
수정 금지 파일: 씬 직렬화 임의 변경, `SaveManager.cs` 대규모.

구현 조건: 런타임 생성. 코지 톤. Vertical Slice 비파괴.
완료 조건: 컴파일 통과, 시설 표시, 저장 연계.
검증 방법: 조건 달성 후 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 049, 076

## Task 078 - 낚시 대회 이벤트 구현

상태: DECISION_REQUIRED
단계: Phase 9
난이도: L
예상 Codex 실행 횟수: 3
사람 승인 필요: YES — 새 이벤트 시스템

목표:
- Task 052 설계 기반 낚시 대회 이벤트를 최소 형태로 구현(대표 차별점 강화).

Project P.A.와의 연결: 마을 변화 + 낮 생활(낚시 대표 루프).

수정 가능 파일: 신규 이벤트 컨트롤러(사이드카) (사람 승인 후)
수정 금지 파일: 구매 로직, 씬 임의 재작성.

구현 조건: 기존 낚시/트렌드 재사용. 최소 구현.
완료 조건: 컴파일 통과, 이벤트 발생, 데모 유지.
검증 방법: 이벤트 트리거 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 052, 069

## Task 079 - 지역 확장(항구 상점) 설계

상태: BLOCKED
단계: Phase 9
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO (설계만)

목표:
- 항구 상점/희귀 물고기 가치 상승 지역 확장을 `AI_WORKFLOW/03_TASKS/DESIGN_REGION_HARBOR.md`로 설계.

Project P.A.와의 연결: 완성 게임(지역 확장) + 낚시 차별점.

수정 가능 파일: `AI_WORKFLOW/03_TASKS/DESIGN_REGION_HARBOR.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 설계만. Vertical Slice 비파괴 전제.
완료 조건: 문서만.
검증 방법: 기존 시스템 호환성 검토.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 078

## Task 080 - 밸런싱 1차 패스

상태: DECISION_REQUIRED
단계: Phase 9
난이도: L
예상 Codex 실행 횟수: 3
사람 승인 필요: YES — 밸런스 변경

목표:
- 가격/수요/성장 곡선 초기값을 데이터 기반으로 조정(수치만, 로직 변경 금지).

Project P.A.와의 연결: 완성 게임(밸런스).

수정 가능 파일: 밸런스 SO/데이터 (사람 승인 후)
수정 금지 파일: `PurchaseEvaluator.cs`/`EconomyService.cs` 로직, 씬.

구현 조건: 데이터 값만. 근거 기록.
완료 조건: 컴파일 통과, 데모 유지.
검증 방법: 조정 전후 비교 로그.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 072

## Task 081 - 튜토리얼/온보딩 보강

상태: BLOCKED
단계: Phase 9
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- Day 1 온보딩 문구/프롬프트를 보강(기존 시나리오 재사용, 대량 수정 금지).

Project P.A.와의 연결: 완성 게임(접근성).

수정 가능 파일: 시나리오/프롬프트 표시부 (탐색 후 보고)
수정 금지 파일: `PlayableDayScenarioController` 대규모 변경, 씬.

구현 조건: 문구/표시 보강만. 코지 톤.
완료 조건: 컴파일 통과, 온보딩 개선, 데모 유지.
검증 방법: Day 1 진입 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 072

## Task 082 - 접근성(폰트/가독성) 점검

상태: DECISION_REQUIRED
단계: Phase 9
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: YES — 최종 가독성은 사람 판정

목표:
- 1920x1080 한국어 가독성 이슈를 조사해 `AI_WORKFLOW/03_TASKS/ACCESSIBILITY.md`로 정리(수정은 별도 Task).

Project P.A.와의 연결: 완성 게임(접근성).

수정 가능 파일: `AI_WORKFLOW/03_TASKS/ACCESSIBILITY.md` (신규)
수정 금지 파일: 코드/씬(조사만).

구현 조건: 조사만. 사람 판정 항목 표시.
완료 조건: 문서만.
검증 방법: `VERIFICATION_RULES.md` §F 대조.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 072

## Task 083 - 사운드(BGM/SFX) 연결 점검

상태: BLOCKED
단계: Phase 9
난이도: M
예상 Codex 실행 횟수: 2
사람 승인 필요: NO

목표:
- `AudioManager` 재사용으로 핵심 이벤트(판매 성사/낮밤 전환)에 SFX 연결 누락을 점검·1개 보강.

Project P.A.와의 연결: 완성 게임(연출).

수정 가능 파일: `AudioManager.cs` 사용부 (탐색 후 보고)
수정 금지 파일: `AudioManager.cs` 대규모 변경, 씬 대량 수정.

구현 조건: 연결 1개만. 외부 에셋 금지(기존 오디오 사용).
완료 조건: 컴파일 통과, SFX 재생, 데모 유지.
검증 방법: 판매/전환 시 사운드 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 072

## Task 084 - 장기 플레이 QA(Day 30+) 회귀

상태: BLOCKED
단계: Phase 9
난이도: L
예상 Codex 실행 횟수: 3
사람 승인 필요: NO

목표:
- `PA_LongPlayProgressionValidator`로 장기 진행 회귀를 확인하고 저장 내구성 이슈 기록.

Project P.A.와의 연결: 완성 게임(장기 안정성).

수정 가능 파일: 결과 기록만.
수정 금지 파일: 코드/씬.

구현 조건: Editor 열림이면 보류. D3D11.
완료 조건: 검증 결과 기록.
검증 방법: 검증기 출력.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 057

## Task 085 - 출시 후보 안정화 체크리스트

상태: DECISION_REQUIRED
단계: Phase 9
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: YES — 출시 판정은 사람

목표:
- Release Candidate 판정 체크리스트를 `AI_WORKFLOW/03_TASKS/RELEASE_CANDIDATE.md`로 작성.

Project P.A.와의 연결: 완성 게임(Stage 5).

수정 가능 파일: `AI_WORKFLOW/03_TASKS/RELEASE_CANDIDATE.md` (신규)
수정 금지 파일: 코드/씬.

구현 조건: 크래시 제로/저장 호환/빌드 고정 항목 포함.
완료 조건: 문서만.
검증 방법: ROADMAP Stage 5와 대조.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 084

---

## 사람 승인 필요(XL/YES) 작업 요약

Task 004(검증기 실행 방식), 016(안정화 판정), 033(유입 밸런스), 050(주민 행동), 055(저장 스키마), 057(마을변화 저장), 064(빌드 실행), 066(Demo Lock), 067(시연 판정), 068(1일 루프 통합), 071/072(재미·슬라이스 판정), 073~078·080·082·085(Phase 9 대형 확장/밸런스/씬/출시).

## 바로 시작 가능한 첫 작업

**Task 001 (프로젝트 구조 인덱스)** — 문서만, 위험 없음, 선행 없음. 그다음 002, 003을 병렬로 진행 가능.

## Review Notes (2026-07-09 최종 검수)

- 이 큐는 4회차 프리플라이트 검수를 통과했다. 상세: `TASK_QUEUE_REVIEW.md`.
- Task 041의 선행 작업 표기 오타를 수정했다 (Task 039, Task 010).
- 작업 선택 규칙 보강: 큐의 첫 TODO가 `사람 승인 필요: YES`인데 승인이 없으면, **승인 요청만 보고하고 그다음 승인 불필요(NO) 작업을 선택**한다. (예: Task 004가 미승인이면 005~007로 진행)
- 첫 실행은 `../00_START_HERE/CODEX_FIRST_RUN_PLAYBOOK.md`를 따른다 — 첫날은 Task 001~003까지만 권장.
