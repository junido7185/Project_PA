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

상태: DONE (2026-07-13, 커밋 `f6cbbe1`) — 구매 소진 시 "품절 · 보충하세요" 라벨 + 프롬프트, 재진열/다음날 자동 해제. `PA_ShopSoldOutValidator` + FinalRoute/DayNight/CoreSlice PASS.
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

상태: DONE
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

상태: DONE
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

상태: DONE
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

상태: DONE
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

상태: DONE
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

상태: DONE
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

상태: PARTIAL
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

상태: DONE
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

상태: DONE
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

상태: DONE
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

상태: DONE
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

상태: DONE
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

상태: DONE
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

상태: DONE
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

상태: DONE
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

상태: DONE
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

상태: DONE (2026-07-17) — 현재 실제 v10을 기준으로 v11 최근 판매 원거래·일차 판단·카테고리 판매·7일 명명 트렌드 스냅샷, 보존 창, v10→v11 빈 기본값 마이그레이션, 소유권·복원·검증 계약을 `SAVE_SCHEMA.md`에 설계. 런타임/실제 저장 스키마는 미변경.
단계: Phase 6. Save & Persistence
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 카테고리별 판매 통계를 현재 v10→v11 추가 확장 패턴으로 `SAVE_SCHEMA.md`에 설계(구현 아님). 기존 v8→v9 문구는 역사적 목표이며 v9/v10 재사용 금지.

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
- Task 054 설계대로 판매 통계 필드를 저장 스키마에 추가(v11), 마이그레이션 포함.

Project P.A.와의 연결: 저장.

수정 가능 파일: `SaveData.cs`, `SaveManager.cs` + Task 054가 확인한 최소 소유자 API(`SalesLogManager.cs`, 공용 명명 트렌드 소유자, 관련 저장 왕복 검증기)를 작업 전 정확히 보고하고 사람 승인 후 확정
수정 금지 파일: 기존 저장 필드 제거/개명, 씬.

구현 조건: 추가 확장만. `[SerializeField]` 이름 보존. 구버전 세이브 로드 호환.
완료 조건: 컴파일 통과, 저장/로드 왕복 성공, 구버전 호환.
검증 방법: 저장→로드→통계 보존 확인.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: Task 054

## Task 056 - 상점 상태 저장 확인

상태: DONE
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
선행 작업: Task 011 (기존 v5 필드 왕복 검증). 판매 통계 v11 Task 055와는 독립.

## Task 057 - 마을 변화 상태 저장

상태: DONE (2026-07-13, 커밋 `71fa710`) — 사용자 세션 지시(우선순위 2 + 예시 커밋)로 승인. Save v8→v9 추가 확장: VillageCulture 대기/활성 상태 6필드 + 마이그레이션. `PA_SaveRoundTripValidator` 대기→다음날 활성→활성 왕복 PASS. VillageChangeSignal 트렌드 점수(Task 048)는 SalesLog 저장(Task 055) 승인 대기로 범위 외.
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

상태: PARTIAL — 활성 `/goal`/CASTLE BUILD 지시로 날짜 전환 연결부 승인. Day 1 결산→Day 2, Day 2 정산 간판→Day 3 아침 D3D11 PASS. Task 041과 사람 연속 플레이는 남음.
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

상태: PARTIAL — Task 039/041 실제 낚시→Fish 18G 판매, Task 093 당일 fishing 신호, Task 092 Raw 다음 날 변화까지 구현. 한 연속 플레이 가독성 확인 대기.
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

상태: PARTIAL
단계: Phase 8
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO

목표:
- 캠핑 또는 가구 중 하나를 보조 루프로 선택하고 기존 시스템 안에서 실제 플레이 경로를 연결한다.

Project P.A.와의 연결: 상점 운영 + 마을 변화(보조 재미).

수정 가능 파일: 가공 기회·플레이어 목표 표시와 관련 설계/상태 문서.
수정 금지 파일: Tier/수익 밸런스, 저장 스키마, 상점·경제·구매 코어, 씬.

구현 조건: 기존 B05→Plank3→Recipe_Furniture→목제 가구→ShopSlot→trend.furniture를 재사용한다. 10,000G/100,000G를 임의 변경하지 않는다.
완료 조건: 플레이어가 현재 잠금 원인과 다음 실제 행동을 알며, 실제 제작·진열·판매·결산 경로가 새 병렬 시스템 없이 이어진다.
검증 방법: Runtime/Editor 컴파일, 읽기 전용 안내 계약, 안전 Unity 경로 승인 뒤 전체 왕복.
실패 시 처리: Task 095가 단계 안내를 구현해 BLOCKED→PARTIAL. 실제 왕복·패널 가독성과 장기 수익 페이싱 결정은 남아 있다.
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

## Task 086 - 상품 진열 테마 코너 구현

상태: PARTIAL
단계: Phase 2
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — Task 024 설계 계약의 후속 구현

목표:
- 기존 `ShopSlot`과 `shop.interior` placement footprint에서 같은 카테고리 2칸 이상 코너를 파생하고, 플레이어가 배치 장부와 월드에서 읽게 한다.

Project P.A.와의 연결: 해질녘 진열 전략 → 밤 실제 판매 → 기존 마을 카테고리 방향.

수정 가능 파일: `MerchandisingCornerController.cs`(신규), `ShopCustomizationController.cs`, `PA_RuntimeSceneBinder.cs`, `PA_ThemeCornerValidator.cs`(신규), validator registry, 관련 상태 문서.
수정 금지 파일: `ShopSlot.cs`, `PurchaseEvaluator.cs`, `EconomyService.cs`, `SalesLogManager.cs`, `VillageChangeSignalController.cs`, `SaveData.cs`, `SaveManager.cs`, 씬/프리팹/패키지.

구현 조건: `DESIGN_THEME_CORNER.md`의 4방향 footprint 연결 요소, 파생 상태, 기존 판매 신호 단일 기록 계약을 따른다. 새 재고·보너스·저장 필드 금지.
완료 조건: 코너 없음/Raw 2칸/Processed 3칸, 품절·보충·이동·회수·로드 재파생, 장부 요약과 월드 라벨, 기존 판매→마을 방향이 실제 플레이에서 동작.
검증 방법: 컴파일, D3D11 전용 ThemeCorner, ShopCustomization, SaveRoundTrip, FinalDemoRoute, 1920×1080 캡처 직접 확인.
실패 시 처리: ThemeCorner 직접 렌더는 ScreenCapture로 교체해 전용 기능 검증을 PASS했다. 그러나 기존 ShopCustomization 회귀의 직접 `Camera.Render()`가 같은 네이티브 스택에서 두 번째로 충돌했다. 세 번째 Unity 실행 금지. 공용 validator 캡처 경로를 별도 감사·교체하기 전 전체 회귀와 최종 시각 판정은 미완이다.
선행 작업: Task 024

## Task 087 - Tripo3D 임시 에셋 감사 정합화

상태: DONE
단계: Phase 8
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 읽기 전용 자산/사용처 감사와 장기 정책 문서화

목표:
- Tripo 추정 모델과 관련 소품을 8분류, 실제 씬/프리팹 사용처, 기능, 2m 격자 footprint·clearance·interaction, 원본 보존, 출처 증빙 상태로 다시 대조한다.

Project P.A.와의 연결: 핵심 캐릭터 정체성 보존과 기능형 창고·작업대의 순차 최종화 기준.

수정 가능 파일: `Docs/Codex/TRIPO_ASSET_AUDIT.md`, `Docs/Codex/ASSET_AND_TOOL_PROVENANCE.md`, 관련 상태 문서.
수정 금지 파일: 코드, 씬, 프리팹, 원본 모델/텍스처, 패키지.

구현 조건: Unity를 실행하지 않고 바이너리 씬 문자열 색인, GUID/Resources/코드 참조, 기존 캡처와 라이선스 파일만 사용한다. 라이선스 미확인 에셋을 최종 확정하지 않는다.
완료 조건: B06~B08/B11/B12와 캐릭터·기타 소품의 실제 사용/기능/격자/분류/최종화 상태가 명확하며 다음 실제 작업 우선순위가 정해진다.
검증 방법: 모델 수·참조·레시피·배치 계약 표식 정적 검사, 라이선스 원문 확인, Markdown 표/whitespace 검사.
실패 시 처리: `BUG_LOG.md` 기록 후 멈춤.
선행 작업: 없음

## Task 088 - 주민 재료 요청 플레이 루프 구현

상태: PARTIAL
단계: Phase 4
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — Task 044 설계 계약의 후속 구현

목표:
- 전문 주민이 실제 담당 레시피의 첫 재료를 낮에 요청하고, 플레이어가 보유 수량을 확인해 전달하면 재료 차감·당일 완료 저장·친밀도 보상을 한 번만 받는 얇은 플레이 루프를 연결한다.

Project P.A.와의 연결: 낮 채집/재고 준비 → 주민 관계 → 저녁 상점 운영 준비.

수정 가능 파일: `NpcDialogue.cs`, `DayNightShopLoopController.cs`, `Inventory.cs`, Chef/Blacksmith/Carpenter 대화 에셋, 관련 상태 문서.
수정 금지 파일: `NpcController.cs`, `PurchaseEvaluator.cs`, `EconomyService.cs`, `SalesLogManager.cs`, `SaveData.cs`, `SaveManager.cs`, 씬/프리팹/패키지.

구현 조건: `DESIGN_RESIDENT_REQUEST.md` 계약을 따르고, 실제 `assignedRecipes`·기존 인벤토리·기존 일일 준비 저장 문자열·`FriendshipService`만 재사용한다. 별도 퀘스트 엔진, 신규 저장 필드, 코인/아이템 보상 금지.
완료 조건: Chef Wheat x3 요청의 부족/준비/전달/중복 차단이 연결되고, Blacksmith/Carpenter도 데이터 기반으로 동작하며, 전문 분야와 작업대가 불일치하는 Tailor 임시 레시피는 요청에서 제외된다.
검증 방법: Runtime/Editor C# 컴파일, API·대화 에셋·금지 시스템 무변경 정적 검사, 이후 안전한 Unity 캡처/검증 경로가 확보되면 D3D11 플레이 확인.
실패 시 처리: Runtime/Editor 컴파일과 정적 계약은 PASS했다. 현재 직접 `Camera.Render()` 동일 원인 2회 충돌 경계 때문에 Unity 3차 실행은 금지하며, 실제 낮 부족/전달/저장·로드/다음 날/밤 차단 플레이 검증은 안전 경로 승인 뒤 수행한다.
선행 작업: Task 044

## Task 089 - 고정 밭 Wheat 재배 상호작용 구현

상태: PARTIAL
단계: Phase 4
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — Task 043 F1/F2 설계 후속, 저장 스키마는 변경하지 않음

목표:
- 끊긴 씨앗→작물→Wheat 참조를 복구하고, 마을 고정 밭 2칸에서 씨앗 1개 차감→성장 안내→가방 여유 확인→Wheat 수확의 실제 상호작용 루프를 연결한다.

Project P.A.와의 연결: 낮 농사 → Wheat 재고 → 기존 Bread 가공/주민 요청/밤 상점 준비.

수정 가능 파일: `FarmPlotInteraction.cs`(신규), `Crop.cs`, `Farmland.cs`, `Inventory.cs`, `DayNightShopLoopController.cs`, `Item_15_Seed.asset`, `Crop_Corn.prefab`, 관련 상태 문서.
수정 금지 파일: `PlayerInteraction.cs`, 메인 씬, `SaveData.cs`, `SaveManager.cs`, `Shop`/`EconomyService`/`PurchaseEvaluator`/`NpcController`, 패키지.

구현 조건: 기존 `IInteractable`, `Farmland.Plant`, `Crop`, 인벤토리, 실제 Wheat Item을 재사용한다. 고정 밭 2칸만 만들고 자유 지형 농장/새 제작 시스템은 추가하지 않는다. 씨앗은 심기 성공 뒤 차감하며, 수확물은 가방 용량 확인 뒤 지급한다.
완료 조건: Seed asset→Crop prefab→Wheat 참조가 유효하고, 낮에 빈 밭/성장 중/수확 가능/가방 가득 참 프롬프트와 안전한 심기·수확이 연결된다.
검증 방법: Runtime/Editor 컴파일, 참조 GUID·API·고정 밭 수·금지 코어 비침범 정적 검사. 안전한 Unity 실행 경로 승인 후 D3D11 실플레이 확인.
실패 시 처리: Runtime/Editor 컴파일과 12개 정적 계약은 PASS했다. Unity 실플레이는 직접 렌더 충돌 2회 경계로 확인하지 못했으며, 날짜 성장과 plot 저장은 F3 저장 설계/승인 전 추가하지 않는다.
선행 작업: Task 043

## Task 090 - Day 2+ 생활–상점 운영 체크리스트 구현

상태: PARTIAL
단계: Phase 4
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 상태를 읽는 프레젠테이션 보강

목표:
- Day 2+ 플레이어가 낮 활동 → 판매 상품 2종 준비 → 진열/가격 확인 → 밤 개점 → 판매/정산 순서를 기존 퀘스트 패널에서 실시간으로 읽게 한다.

Project P.A.와의 연결: 새 낮 활동과 주민 요청을 밤 상점 운영으로 연결하는 플레이어 가시적 전체 루프.

수정 가능 파일: `PlayableDayScenarioController.cs`, 관련 상태 문서.
수정 금지 파일: 저장 스키마, `Shop`/`ShopSlot`/`EconomyService`/`PurchaseEvaluator`/NPC FSM 코어, 씬/프리팹/패키지.

구현 조건: 기존 `DayNightShopLoopController`, `Inventory`, `ShopSlot`, `SalesLogManager`, `NpcDialogue` 상태만 읽는다. 새 퀘스트 상태나 보상, 저장 필드를 추가하지 않는다.
완료 조건: Day 2+에서 실제 낮 활동, 판매 가능한 상품 종류 수, 진열/가격, 개점, 당일 구매, 정산 상태가 0.5초 이내 갱신되고 단계별 상단 목표가 일치한다.
검증 방법: Runtime/Editor C# 컴파일, 기존 코어 무변경 및 상태 판정 정적 계약. 안전한 Unity 실행 경로 승인 뒤 1920×1080 실제 가독성 확인.
실패 시 처리: Runtime/Editor 오류 0과 10개 정적 계약은 PASS했다. 직접 `Camera.Render()` 충돌 2회 경계 때문에 Unity를 실행하지 않았으며 1920×1080 실시간 상태 전환/가독성 확인 전까지 PARTIAL이다.
선행 작업: Task 068, Task 088, Task 089의 구현분

## Task 091 - 가공 결과물 수용량 선검사와 재료 소실 차단

상태: PARTIAL
단계: Phase 3
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 CraftingService 트랜잭션 안전 보강

목표:
- Plank·요리·제련·가구 등 모든 기존 레시피에서 결과물을 받을 공간이 없으면 재료를 차감하지 않고 제작을 중단한다.

Project P.A.와의 연결: 낮 원재료 → 가공품 → 밤 판매의 공용 가공 연결부에서 플레이어 재고 손실 방지.

수정 가능 파일: `CraftingService.cs`, 관련 상태 문서.
수정 금지 파일: `Inventory`, 레시피/아이템 에셋, 작업대/UI, 저장 스키마, 상점/경제/구매/NPC 코어, 씬/프리팹/패키지.

구현 조건: 실제 결과 `ItemInstance`의 data/quality/currentPrice와 `CanStackWith` 규칙을 사용해 기존 메타 일치 스택 여유 또는 빈 슬롯을 차감 전에 확인한다. 기존 재료 수량/품질 계산과 성공 피드백은 보존한다.
완료 조건: 결과 공간 부족 시 재료 차감 경로에 진입하지 않고 false, 공간이 있으면 기존 차감→결과 추가→작업대 피드백이 유지된다.
검증 방법: Runtime/Editor C# 컴파일, 현재 8개 RecipeData의 출력/수량/재료 계약과 코드 순서 정적 검사. 안전한 Unity 실행 경로 승인 뒤 가방 가득 참/메타 동일 스택/빈 슬롯 세 분기 확인.
실패 시 처리: Runtime/Editor 오류 0과 현재 8개 레시피/트랜잭션 정적 계약 11개는 PASS했다. 직접 `Camera.Render()` 충돌 2회 경계 때문에 Unity를 실행하지 않았으며 가방 가득 참/메타 스택/빈 슬롯 실제 분기 확인 전까지 PARTIAL이다.
선행 작업: 기존 CraftingService/Inventory ItemInstance 경로

## Task 092 - Raw 판매의 다음 날 생산자 보관·수거 변화

상태: PARTIAL
단계: Phase 5
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — VC-001A와 v10 마을 변화 저장 계약의 카테고리 확장

목표:
- 실제 Fish/Ore/Wood 등 Raw 판매가 다음 날 마을에 생산자 보관·수거 지점을 남기도록 기존 마을 변화 사이드카를 확장한다.

Project P.A.와의 연결: 낮 원자재 활동 → 밤 판매 → 다음 날 눈에 보이는 마을 변화라는 핵심 차별점의 두 번째 카테고리 증거.

수정 가능 파일: `VillageCultureVisualController.cs`, VC-001A/출처 문서, 관련 상태 문서.
수정 금지 파일: `SaveData`, `SaveManager`, `Shop`/`ShopSlot`/`EconomyService`/`PurchaseEvaluator`/NPC 코어, 씬·프리팹 원본, Tripo 원본, 패키지.

구현 조건: 기존 `SaleRecord.category`와 v10의 pending/active category 문자열을 재사용한다. 같은 refresh의 여러 실제 판매는 `GetRecent` 최신순 중 가장 최근 Raw/Processed 한 건을 다음 날 대표 변화로 선택한다. Raw 표식은 새 원시 큐브 없이 기존 `Prop_WoodLog`/`Prop_Rock`과 Project P.A. 간판 메시를 사용하고 모든 복제 Collider/기능 컴포넌트를 제거한다.
완료 조건: Raw 판매 당일에는 비활성, 다음 날 DayPreparation에 Raw 전용 실모델 루트만 활성, 저장 복원에서 Raw 카테고리가 같은 시각으로 복원되고 Processed 기존 계약이 유지된다.
검증 방법: Runtime/Editor C# 컴파일, 리소스 존재·최신 판매 선택·당일/다음 날·Raw/Processed 상호 배타 활성·기존 저장 필드 재사용 정적 검사. 안전한 Unity 실행 경로 승인 뒤 실제 GameCamera 전후 캡처와 동선/라벨 확인.
실패 시 처리: 직접 `Camera.Render()` 동일 네이티브 충돌이 2회 발생했으므로 Unity 3차 실행 금지. 컴파일/정적 계약 후 실제 시각·동선 확인 전까지 PARTIAL.
선행 작업: Task 041, Task 057, VC-001A

## Task 093 - 실제 판매 기반 낚시·가구 명명 트렌드 연결

상태: PARTIAL
단계: Phase 5
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — Task 047에서 확정한 읽기 전용 점수 계약의 후속 구현

목표:
- Fish/생선구이/목제 가구의 실제 성공 판매만 오늘의 낚시·가구 문화 트렌드로 집계하고 기존 카테고리 신호와 결산에 함께 표시한다.

Project P.A.와의 연결: 낮 낚시·가공/가구 제작 → 밤 실제 판매 → 플레이어가 읽는 마을 생활 방향.

수정 가능 파일: `VillageChangeSignalController.cs`, `PA_VillageChangeSignalValidator.cs`, `VILLAGE_TREND.md`, 관련 상태 문서.
수정 금지 파일: `SalesLogManager.cs`, `ShopSlot.cs`, `PurchaseEvaluator.cs`, `EconomyService.cs`, NPC 코어, `SaveData.cs`, `SaveManager.cs`, 씬·프리팹·패키지.

구현 조건: `(Raw, Fish)`·`(Processed, 생선구이)`·`(Luxury, 목제 가구)` 정확 쌍만 읽고, 거래×1000+min(매출,999), 거래→매출→최근 시각→trendId 동점 규칙을 따른다. 캠핑은 매핑 없음 상태를 유지한다. 기존 카테고리 집계와 시설 방향 예고를 보존한다.
완료 조건: Fish 1건/18G가 fishing 1018점이며 Raw 카테고리에도 한 번 집계되고, 잘못된 카테고리·미등록 캠핑 이름은 명명 트렌드가 되지 않으며, 결산 요약에서 선도 생활 트렌드를 읽을 수 있다.
검증 방법: Runtime/Editor C# 컴파일, 기존 validator 계약 확장 및 정적 매핑·점수·금지 코어 무변경 검사. 안전한 Unity 실행 경로 승인 후 결산 화면 실제 가독성 확인.
실패 시 처리: Runtime/Editor 오류 0과 정적 계약 18개는 PASS했다. 직접 `Camera.Render()` 동일 네이티브 충돌 2회 경계 때문에 Unity 3차 실행 금지. 실제 결산 화면과 생선구이·목제 가구 판매 왕복 검증 전까지 PARTIAL.
선행 작업: Task 041, Task 047

## Task 094 - Tripo3D 장기 최종화 정책과 라이선스 미확인 소품 노출 정리

상태: PARTIAL
단계: Phase 8
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 외형·원본을 보존하고 미확인 소품의 런타임 생성만 제거

목표:
- Tripo 추정 캐릭터·건물·작업대·소품의 8분류, 캐릭터 정체성 보존, 원본/수정본 분리, Placeable 적합성, 출처·배포 게이트를 장기 지침에 고정한다.
- 라이선스 문서가 없는 Froggy Chair가 실내와 광장에 런타임 생성되는 두 경로를 제거한다.

Project P.A.와의 연결: 핵심 캐릭터와 기능 가구를 무작정 교체하지 않으면서 최종 빌드의 출처 위험과 화면상 임시 생성물 노출을 줄인다.

수정 가능 파일: `DemoVisualDressingController.cs`, `TRIPO_ASSET_AUDIT.md`, `ASSET_AND_TOOL_PROVENANCE.md`, `PLACEMENT_SYSTEM_ARCHITECTURE.md`, `PLACEABLE_ASSET_GUIDE.md`, 관련 상태 문서.
수정 금지 파일: 메인 씬, 프리팹, FBX/OBJ/텍스처/Resources 원본, 저장 스키마, `Shop`/`EconomyService`/`PurchaseEvaluator`/NPC/배치 코어, 패키지.

구현 조건: C-01~C-09와 B01~B12는 기존 감사 결정을 보존한다. B06 정면·재질은 캡처 증거 없이 수정하지 않는다. Froggy 원본과 생성 프리팹은 삭제하지 않고 런타임 참조만 제거하며, 기존 B01·상점 기능·광장 벤치·CC0 식생을 유지한다.
완료 조건: 장기 정책과 Placeable 온보딩 게이트가 문서화되고, 플레이어 실행 경로의 `Prop_FroggyChair` 참조가 0이며 다른 데모 소품/기능 경로가 유지된다.
검증 방법: Runtime/Editor C# 컴파일, 모델/출처/런타임 참조 정적 계약, `git diff --check`. 동일 네이티브 충돌 경계 때문에 Unity는 실행하지 않는다.
실패 시 처리: Runtime/Editor 오류 0, 플레이어 런타임 Froggy 참조 0, 원본 3종·B01·CC0 식생·광장 벤치 보존 정적 계약은 PASS했다. Unity 직접 렌더 충돌 2회 경계 때문에 게임 카메라를 확인하지 않았으므로 Task는 PARTIAL이다.
선행 작업: Task 087, P2~P5 배치 시스템

## Task 095 - 기존 가구 보조 루프의 플레이어 도달성 안내 연결

상태: PARTIAL
단계: Phase 8
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 상태의 읽기 전용 안내만 추가

목표:
- 기존 가구 보조 루프의 현재 잠금과 다음 실제 행동을 Day 4+ 플레이어 목표에 한 줄로 연결한다.

Project P.A.와의 연결: 낮의 Wood→Plank 준비가 Tier 2 가구 제작, 밤 진열·판매, 정산의 `가구 문화` 결과로 이어짐을 플레이어가 이해하게 한다.

수정 가능 파일: `ProcessingOpportunityController.cs`, `PlayableDayScenarioController.cs`, 관련 상태 문서.
수정 금지 파일: TierService/티어 정의, 레시피·아이템 밸런스, 저장 스키마, Shop/ShopSlot/Economy/Purchase/NPC 코어, 씬·프리팹·패키지.

구현 조건: `Recipe_Furniture`·Inventory·활성 BasicWorkbench·Tier·ShopSlot·당일 SaleRecord만 읽고 `Tier 1→B05→Plank3→Tier 2→제작→진열→판매`의 다음 한 단계만 표시한다. 아이템 지급·강제 승급·자동 제작/진열/판매 금지.
완료 조건: 당일 판매 완료까지 모든 상태가 기존 Day 4+ 체크리스트에서 하나의 다음 행동으로 표현되고, 기존 10,000G/100,000G 조건을 그대로 표시한다.
검증 방법: Runtime/Editor C# 컴파일, 정확 Recipe/Tier/Workbench/당일 판매와 상태 변경 호출 부재 정적 계약. 안전한 Unity 실행 경로 승인 뒤 1920×1080 가독성과 실제 왕복 확인.
실패 시 처리: Runtime/Editor 오류 0과 정적 계약 PASS. Unity 실행 금지 경계로 실제 패널/왕복을 확인하지 못해 PARTIAL.
선행 작업: Task 070, Task 090, Task 093

## Task 096 - 새 게임·이어하기 제품형 진입 화면 연결

상태: PARTIAL
단계: Phase 8
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 온보딩·저장 경로의 표시/진입 연결

목표:
- 게임 실행 직후 PROJECT P.A.의 정체성과 새 게임/이어하기 선택을 플레이어에게 제공한다.

Project P.A.와의 연결: 메인 씬을 곧바로 기능 데모 모달로 시작하지 않고, 새 플레이와 저장된 다일차 운영을 명시적으로 선택하는 처음부터의 게임 진입을 완성한다.

수정 가능 파일: `SaveManager.cs`, `PlayableDayScenarioController.cs`, 관련 상태 문서.
수정 금지 파일: 저장 스키마/SaveKey/세이브 삭제, 자동 로드, Day 1 단계, 메인 씬·프리팹·패키지.

구현 조건: `ISaveRepository.ExistsAsync`를 읽기 전용으로 노출한다. 저장이 없으면 이어하기를 비활성화하고, 있으면 기존 `LoadGameAsync`→`RestoreSavedSession`만 사용한다. 새 게임은 기존 이름 등록부터 시작하며 기존 저장을 즉시 삭제하지 않는다.
완료 조건: 타이틀→새 게임→기존 온보딩과 타이틀→이어하기→저장 상태 복원이 모두 플레이어 입력으로 연결되고, 실패 시 타이틀에서 복구된다.
검증 방법: Runtime/Editor C# 컴파일, 저장 존재/분기/복원/실패 복귀/F5·F9/상태 무변경 정적 계약. 안전 Unity 경로 승인 뒤 저장 없음/있음 두 화면과 실제 로드 왕복 확인.
실패 시 처리: Runtime/Editor 오류 0, 상태 전이 계약 15개와 정확 호출 토큰 격리 검사 PASS. Unity 실행 금지 경계 때문에 실제 화면/로드 왕복을 확인하지 못해 PARTIAL.
선행 작업: Task 002, Task 011, Task 068

## Task 097 - Pause 메뉴 제품 제어와 안전한 세션 상태 복구

상태: PARTIAL
단계: Phase 8
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 저장/입력 권위의 플레이어용 메뉴 연결

목표:
- 게임 플레이 중 ESC 메뉴에서 계속하기·저장·저장본 불러오기·저장 후 종료를 제공한다.
- 일시정지 전 시간 배율과 커서 잠금/표시 상태를 정확히 복구한다.

Project P.A.와의 연결: 타이틀에서 시작한 세션을 플레이어가 안전하게 중단·저장·복원·종료할 수 있어 처음부터 끝까지 플레이 가능한 제품 흐름을 보완한다.

수정 가능 파일: `PauseManager.cs`, 관련 상태 문서.
수정 금지 파일: `SaveManager` 권위/SaveKey/스키마, 씬·프리팹·패키지, 스마트폰·인벤토리 ESC 우선순위.

구현 조건: 저장 유무는 `HasSaveAsync`, 저장/로드는 기존 `SaveGameAsync`/`LoadGameAsync`만 사용한다. 비동기 작업 중 중복 입력을 막고 저장 실패 시 종료를 취소한다. 시작 타이틀 위에 Pause가 열리지 않아야 한다.
완료 조건: 네 메뉴 동작과 상태 피드백, 이전 timeScale/커서 복구, 저장본 없음 Load 비활성, 저장 성공 후에만 Quit 호출이 연결된다.
검증 방법: Runtime/Editor C# 컴파일, 버튼/상태/저장 권위/예외/종료 순서 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 실제 클릭과 1920×1080 가독성·빌드 종료 확인.
실패 시 처리: Runtime/Editor 오류 0과 정적 계약 11/11 PASS. Unity 실행 금지 경계 때문에 실제 ESC/클릭/저장·로드/종료를 확인하지 못해 PARTIAL.
선행 작업: Task 011, Task 096

## Task 098 - Day 7 첫 주 완주 요약과 저장 후 선택

상태: PARTIAL
단계: Phase 8
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 Day 7/정산/저장 권위의 제품형 종착점 연결

목표:
- Day 7 정산에서 플레이어의 첫 주 운영 성과를 요약한다.
- 저장 후 2주차 계속 또는 저장 후 종료를 명시적으로 선택하게 한다.

Project P.A.와의 연결: 새 게임부터 7일간의 낮 마을 생활·밤 상점 운영을 하나의 완주 가능한 게임 루프로 닫고, 원하면 같은 세계를 계속 운영하게 한다.

수정 가능 파일: `LongPlayProgressionController.cs`, `DayNightShopLoopController.cs`, 관련 상태 문서.
수정 금지 파일: `SaveData`/`SaveManager`/SaveKey, 경제·Tier·Day 7 수치, 씬·프리팹·패키지.

구현 조건: Day 7 Settlement에서 한 번만 표시하고 기존 이름·매출·돈·Tier·평판·당일 정산을 읽는다. 계속은 Day 7 저장→기존 다음 날 권위→Day 8 재저장, 종료는 저장 성공 뒤 Quit 순서를 지킨다. 모달 뒤 간판 입력은 차단한다.
완료 조건: 첫 주 완주 요약, 두 선택, timeScale/커서 복구, 실패 복구, Day 8의 영속 확인이 연결된다.
검증 방법: Runtime/Editor C# 컴파일, 표시 조건·성과 읽기·저장 순서·배경 차단·상태 복구 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 실제 Day 7 연속 플레이와 화면/버튼/빌드 재실행 확인.
실패 시 처리: Runtime/Editor 오류 0과 정적 계약 12/12 PASS. Unity 실행 금지 경계로 실제 Day 7 화면과 클릭/종료/재실행을 확인하지 못해 PARTIAL.
선행 작업: Task 068, Task 084, Task 096, Task 097

## Task 099 - 실제 입력 기반 시작 조작 안내

상태: PARTIAL
단계: Phase 8
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 온보딩 표시의 실제 입력 정보 보강

목표:
- 첫날 온보딩에서 플레이어가 외부 문서 없이 이동·상호작용·핵심 UI·건설·저장/불러오기·일시정지 조작을 확인한다.

Project P.A.와의 연결: 타이틀에서 첫날 월드로 진입한 직후 조작을 몰라 멈추는 제품 진입 공백을 닫는다.

수정 가능 파일: `PlayableDayScenarioController.cs`, 관련 상태 문서.
수정 금지 파일: `PlayerInputHandler` 바인딩, 타이틀/이어하기, Day 1·Day 2+ 단계, 저장 형식, 씬·프리팹·패키지.

구현 조건: 기존 `StartupStep` 흐름을 재사용하고 `PlayerInputHandler`의 현재 키와 정확히 일치하는 간결한 안내를 스마트폰 지급 뒤에 표시한다. 새 입력 시스템이나 별도 도움말 상태를 만들지 않는다.
완료 조건: 안내 확인 뒤 기존 보급품→도착→첫날 시작 흐름으로 이어지고 기존 제품 진입/게임 루프가 보존된다.
검증 방법: Runtime/Editor C# 컴파일, 입력 토큰·단계 순서·기존 흐름 보존 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 1920×1080 가독성 확인.
실패 시 처리: Runtime/Editor 오류 0, 실제 키 매핑·단계 순서·기존 흐름 보존 기능 계약 11/11 PASS. Unity 실행 금지 경계 때문에 실제 1920×1080 가독성과 클릭 전환을 확인하지 못해 PARTIAL.
선행 작업: Task 096, Task 097

## Task 100 - 타이틀 게임 종료 경로

상태: PARTIAL
단계: Phase 8
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 시작 Canvas의 제품 제어 보강

목표:
- 게임 실행 직후 타이틀에서 저장 여부와 무관하게 프로그램을 종료한다.

Project P.A.와의 연결: 새 게임과 이어하기만 있던 시작 화면을 완결된 제품 진입점으로 만들고, 플레이 세션을 시작하지 않아도 안전하게 나갈 수 있게 한다.

수정 가능 파일: `PlayableDayScenarioController.cs`, 관련 상태 문서.
수정 금지 파일: 타이틀 새 게임/이어하기, Task099 조작 안내, Day 1·Day 2+ 단계, Pause/Day 7 저장 후 종료, 저장 형식, 씬·프리팹·패키지.

구현 조건: 기존 `FirstDayPrototypeCanvas`와 버튼 생성 helper를 재사용한다. 종료 버튼은 Title에서만 보이고 이어하기 로딩 중에는 잠긴다. 실제 빌드에서 `Application.Quit`을 호출하며 Editor에서는 플레이를 강제 중단하지 않고 안내만 표시한다.
완료 조건: 타이틀에 새 게임/이어하기/게임 종료가 함께 배치되고, 다른 시작 단계와 Day 1 결산에는 종료 버튼이 노출되지 않는다.
검증 방법: Runtime/Editor C# 컴파일, 타이틀 전용 가시성·세 버튼 배치·로딩 잠금·Quit 격리·기존 흐름 보존 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 1920×1080 가독성과 실제 빌드 종료 확인.
실패 시 처리: Runtime/Editor 오류 0, 타이틀 전용 가시성·세 버튼 배치·이어하기 로딩 잠금·Quit 격리·기존 흐름 보존 계약 14/14 PASS. Unity 실행 금지 경계 때문에 실제 1920×1080 화면과 빌드 종료를 확인하지 못해 PARTIAL.
선행 작업: Task 096, Task 099

## Task 101 - 기존 저장 보호 새 게임 확인

상태: PARTIAL
단계: Phase 8
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 저장 존재 조회와 시작 UI만 사용

목표:
- 기존 저장이 있는 상태에서 새 게임을 시작할 때, 이후 저장 시 기존 단일 슬롯을 덮어쓴다는 사실을 명확히 확인받는다.

Project P.A.와의 연결: 이어하기 기록을 실수로 잃지 않게 하면서도 저장 파일을 즉시 삭제하지 않고 처음부터 플레이하는 제품 진입 경로를 완성한다.

수정 가능 파일: `PlayableDayScenarioController.cs`, 관련 상태 문서.
수정 금지 파일: `SaveManager`/SaveKey/저장 스키마·실제 저장 파일, 기존 이어하기·종료·조작 안내, Day 1·Day 2+ 단계, 씬·프리팹·패키지.

구현 조건: 타이틀의 기존 `HasSaveAsync` 결과를 재사용하고 확인 중에는 새 게임을 잠근다. 저장이 있을 때만 확인 단계를 표시하며, 취소는 타이틀로 돌아가 저장 상태를 다시 확인한다. 확인 화면에서는 저장·삭제 API를 호출하지 않는다.
완료 조건: 저장 없음은 이름 등록으로 직행하고, 저장 있음은 경고→새 게임 또는 타이틀 복귀로 분기하며 기존 이어하기·종료·후속 온보딩이 보존된다.
검증 방법: Runtime/Editor C# 컴파일, 저장 존재 분기·비동기 잠금·취소 복귀·저장 비침범·기존 흐름 보존 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 저장 있음/없음 실제 클릭과 1920×1080 가독성 확인.
실패 시 처리: Runtime/Editor 오류 0과 저장 존재 분기·비동기 잠금·취소 복귀·저장 비침범·기존 흐름 계약 15/15 PASS. Unity 실행 금지 경계 때문에 저장 있음/없음 실제 화면·클릭과 1920×1080 가독성을 확인하지 못해 PARTIAL.
선행 작업: Task 096, Task 100

## Task 102 - B09 외부 창고 실제 사용 UI

상태: PARTIAL
단계: Phase 8
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 StorageBox/StorageUI/v10 배치 저장 경로의 누락된 런타임 연결

목표:
- 플레이어가 B09 외부 창고 문 앞에서 실제 보관 화면을 열고, 핫바 물품을 맡기고 다시 꺼낼 수 있게 한다.

Project P.A.와의 연결: 낮에 얻은 상품·재료를 밤 영업과 다음 날 준비 사이에 보관하는 Week 2 저장/물류 기능을 실제 플레이 가능한 상태로 연결한다.

수정 가능 파일: `StorageUI.cs`, `PA_RuntimeSceneBinder.cs`, `PauseManager.cs`, 관련 상태 문서.
수정 금지 파일: `StorageBox` 데이터/직렬화, B09 모델·프리팹, `SaveData`/`SaveManager`/SaveKey, 인벤토리·핫바·상점 코어, 씬·패키지.

구현 조건: 기존 `StorageBox.items`와 `maxSlotCount`, `ItemInstance`, `Inventory.AddInstance`, v10 `storedItems`를 그대로 사용한다. 메인 씬에 없는 `StorageUI`를 기존 UI 루트에 한 개만 보장하고, 실제 아이콘·수량·품질/가격을 표시한다. 보관 시 현재 선택 핫바 스택에서 정확히 1개만 차감하고 메타를 보존한다. ESC는 창고를 먼저 닫고 원래 커서를 복원한다.
완료 조건: B09 상호작용→24칸 화면→선택 물품 1개 보관→클릭 회수→닫기 흐름이 연결되고, 기존 Pause/인벤토리/스마트폰과 겹치지 않으며 저장 구조가 보존된다.
검증 방법: Runtime/Editor C# 컴파일, 단일 UI 생성·B09 기존 OpenBox 진입·24칸·정확 선택 스택 차감·메타 보존·가방 가득 참·ESC 우선순위·v10 저장 경로 보존 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 B09 실제 화면·클릭·저장/로드와 1920×1080 가독성 확인.
실패 시 처리: Runtime/Editor 오류 0과 단일 UI·B09 OpenBox·24칸 6×4·아이콘/메타·정확 선택 스택 차감·가방 가득 참·커서/ESC·v10 저장 비침범 계약 14/14 PASS. Unity 실행 금지 경계 때문에 B09 실제 화면·보관/회수·저장/로드와 1920×1080 가독성을 확인하지 못해 PARTIAL.
선행 작업: Task 087, P3 야외 B09 최종화

## Task 103 - 제작 도감·작업대 제작 UI 제품 흐름 완성

상태: PARTIAL
단계: Phase 8
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 Workbench/CraftingUI/CraftingService 진입의 제품 UI 보강

목표:
- `[C] 제작`이 빈 패널 대신 전체 레시피와 필요한 작업대를 알려 주는 도감으로 동작하게 한다.
- 작업대 `[Space]`에서는 해당 작업대 레시피를 실제 재료·출력 정보와 함께 제작하고 결과 피드백을 확인하게 한다.

Project P.A.와의 연결: 낮에 얻은 재료를 작업대에서 가공해 밤 상점 상품으로 준비하는 핵심 보조 루프의 플레이어 진입 공백을 닫는다.

수정 가능 파일: `CraftingUI.cs`, `PauseManager.cs`, 관련 상태 문서.
수정 금지 파일: `CraftingService`, Recipe/Item 자산, B05~B08 모델·프리팹, Inventory/Tier/Friendship 권위, 메인 씬, 저장 스키마, 패키지.

구현 조건: `Resources/Recipes`의 기존 8개와 `Workbench.Interact → OpenForWorkbench → CraftingService.TryCraft` 권위를 유지한다. C 도감에서는 작업대 전용 레시피를 원격 제작하지 못하게 하며 필요한 작업대를 표시한다. 작업대 화면은 아이콘·전체 재료·출력 수량·잠금 상태·클릭 결과를 표시한다. 인벤토리·스마트폰·창고·Pause와 겹치지 않고 ESC가 제작 화면을 먼저 닫으며 기존 커서 상태를 복원한다.
완료 조건: C 도감에 기존 8개가 보이고 원격 제작은 불가하며, B05~B08 컨텍스트에서는 일치 레시피만 기존 서비스로 제작한다. 성공/실패 뒤 화면이 갱신되고 ESC/UI 상호배제가 연결된다.
검증 방법: Runtime/Editor C# 컴파일, 8레시피·도감/작업대 모드·원격 제작 차단·전체 재료/출력·CraftingService 단일 권위·ESC/커서/상호배제 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 실제 C/Space/클릭과 1920×1080 가독성 확인.
실패 시 처리: Runtime/Editor 오류 0과 제작 도감·작업대 컨텍스트·8레시피·원격 제작 차단·전체 재료/출력·아이콘·결과 피드백·전체 화면 입력 차단·ESC/커서/상호배제·기존 권위 보존 계약 16/16 PASS. 직접 `Camera.Render()` 동일 네이티브 충돌 2회 경계 때문에 Unity 3차 실행 금지. 실제 화면/클릭 확인 전까지 PARTIAL.
선행 작업: Task 091, Task 099

## Task 104 - Tripo3D 장기 정책 고정 및 B11 분수 충돌 정합

상태: PARTIAL
단계: Phase 8
난이도: S
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 B11 외형·역할·원본을 유지하는 Unity 런타임 물리 보정

목표:
- Grid 기반 커스터마이징과 Tripo3D 감사 지침을 기존 Placeable/에셋 문서의 장기 아키텍처 결정으로 고정한다.
- 광장 B11 분수의 6×6 사각 BoxCollider 모서리가 보이지 않는 충돌을 만드는 문제를 실제 원형 Visual 메시와 맞춘다.

Project P.A.와의 연결: 플레이어/NPC의 핵심 광장 동선을 시각 모델과 일치시키고, 임시 Tripo 에셋을 무작정 교체하지 않는 최종화 기준을 이후 모든 기능·공간 확장에 적용한다.

수정 가능 파일: `DemoVisualDressingController.cs`, `TRIPO_ASSET_AUDIT.md`, `ARCHITECTURE_DECISIONS.md`, `ASSET_AND_TOOL_PROVENANCE.md`, 관련 상태 문서.
수정 금지 파일: 메인 씬, B11 프리팹/YAML, B11 원본 FBX·텍스처, Grid/Shop/NPC/Save 권위, 패키지.

구현 조건: 기존 B11 실루엣·재질·배치와 캡슐형 carving `NavMeshObstacle`을 유지한다. 런타임의 활성 B11 루트 `BoxCollider`만 끄고 실제 Visual 자식의 원본 Mesh에 비볼록 `MeshCollider`를 붙인다. 메시가 없으면 기존 collider를 유지해 무충돌 상태를 만들지 않는다. Tripo 원본은 덮어쓰지 않고 에셋별 1~8 판정·캐릭터 정체성 보존·기능/Placeable/출처 게이트를 장기 결정으로 기록한다.
완료 조건: B11 메시가 있을 때 보이는 원형 구조와 물리가 일치하고 기존 NavMesh 우회가 유지된다. 모델/프리팹/씬/저장/패키지 무변경과 원본·라이선스 대기 상태가 문서에 명확하다.
검증 방법: Runtime/Editor C# 빌드, B11 단일 경로·실메시 확인 뒤 Box 비활성·MeshCollider 원본 메시/비볼록·NavMeshObstacle 비변경·중복 방지 정적 계약, 금지 파일 경계, `git diff --check`. 안전 Unity 경로 승인 뒤 동일 GameCamera와 실제 이동으로 최종 확인.
실패 시 처리: Runtime/Editor 오류 0, 실제 Visual mesh 확인 뒤 Box 비활성·원본 mesh 비볼록 collider·안전 폴백·NavMeshObstacle 보존·중복 방지·금지 파일 비침범 계약 12/12와 `git diff --check` PASS. 직접 `Camera.Render()` 동일 네이티브 충돌 2회 경계로 Unity를 실행하지 않아 실제 원형 접근 동선과 동일 구도 After 캡처 전까지 PARTIAL.
선행 작업: Task 087, Task 094, P1~P5 Placement

## Task 105 - 20~23시 영업 손님 흐름 복구

상태: PARTIAL
단계: Phase 8
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 시간표/구매 수학을 바꾸지 않는 기존 손님 초대 사이드카의 제한적 Rest override

목표:
- 18~23시 영업과 주민 Rest 시작 19~20시의 불일치 때문에 20시 이후 손님이 0명이 되는 핵심 밤 영업 공백을 닫는다.
- Tier 0 외부 상점과 Tier 1 실내 상점 모두 늦은 손님이 방문하고 종료 뒤 원래 일과로 복귀하게 한다.

Project P.A.와의 연결: 밤 영업 전체가 실제 손님·구매/거절 기회를 갖게 해 낮 준비가 23시 정산까지 이어지는 핵심 루프를 완성한다.

수정 가능 파일: `NpcScheduleController.cs`, `CustomerArrivalController.cs`, `InteriorCustomerController.cs`, 관련 상태 문서.
수정 금지 파일: `NpcController`, Schedule ScriptableObject, `Shop`/`ShopSlot`/`PurchaseEvaluator`/`EconomyService`, 저장, 씬·프리팹·패키지.

구현 조건: 실제 시간표와 `_activePhase`는 변경하지 않는다. 현재 phase가 `Rest`이고 소비 컨트롤러가 있을 때만 임시 방문 override를 시작한다. 외부/실내 초대자는 원래 위치·Shop 참조·override 소유자를 기록하고 방문 완료·실패·timeout·영업 종료에 정확히 복구한다. `Sleep`, `Work`와 Day 1 튜토리얼은 깨우지 않는다. 기존 `TryForceShop`/`TryBeginShoppingVisitAt`와 구매 수학만 사용한다.
완료 조건: 20~23시 열린 외부/실내 상점에서 Rest 주민이 후보가 되며, 동시에 이미 활동 중인 주민 흐름과 동시 손님 상한은 유지된다. 영업 종료 후 임시 손님이 남지 않고 Rest로 돌아간다.
검증 방법: Runtime/Editor C# 빌드, Rest 전용·멱등 override·phase 변경 안전 해제·외부/실내 초대 성공/실패/timeout/close 복구·Day1/구매/저장 비침범 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 18:30/20:30/22:30 실제 손님과 23시 회수를 확인한다.
실행 결과: Runtime/Editor 빌드 오류 0. Rest 전용·멱등 override, phase 보존/변경 시 해제, 외부/실내 lease, 실패/종료/timeout/close 복구, Day 1과 기존 FSM 권위 보존 정적 계약 18/18 및 `git diff --check` PASS. 직접 `Camera.Render()` 계열 Unity 실행은 기존 2회 네이티브 충돌 때문에 시도하지 않았다.
남은 확인: 안전 Unity 경로에서 18:30/20:30/22:30 외부·실내 손님 유입, 기존 동시 손님 상한, 23:00 회수와 Rest 복귀를 확인한다.
선행 작업: CDN-002, S3 InteriorCustomer, Task 068

## Task 106 - Processed 다음 날 변화 실제 에셋 전환

상태: PARTIAL
단계: Phase 8
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 이미 사용·감사된 B05 Visual과 Project P.A. 파생 에셋의 비충돌 시각 재사용

목표:
- 핵심 차별점인 Processed 다음 날 변화에 남은 원시 큐브 작업대·상자·보드·배너를 제거한다.
- B05 실제 모델의 시각 정체성과 준비 공정 파생물을 사용해 목재 원재료→가공 준비라는 역할이 게임 카메라에서 읽힐 기반을 만든다.

Project P.A.와의 연결: 성공한 Processed 판매가 다음 날 코드 모형이 아니라 실제 마을 작업 공간으로 보이게 해 `판매 → 마을 변화`의 시각 신뢰도를 높인다.

수정 가능 파일: `VillageCultureVisualController.cs`, 관련 작업·에셋 감사·상태 문서.
수정 금지 파일: 판매/구매/경제/NPC 코어, `SalesLogManager`, 저장 스키마/권위, B05 원본 FBX·프리팹·BuildingData, Raw 변화, 씬·패키지.

구현 조건: `Buildings/Building_B05_Workbench`의 래퍼 전체가 아니라 `prefab/Visual`만 복제하고, `VisualFinalization/B05_Workbench_PreparationKit`과 기존 B10 간판을 같은 비충돌 시각 루트에 배치한다. 원시 primitive 생성과 임의 재질은 제거한다. 실제 B05 기능·배치·해금은 복제하지 않는다.
완료 조건: Processed 루트가 실제 메시/준비 키트/읽을 수 있는 간판으로 구성되고, Collider·NavMeshObstacle·Workbench가 0개이며 Raw와 상호 배타적으로만 활성화된다.
검증 방법: Runtime/Editor C# 빌드, primitive 생성 0·B05 BuildingData/Visual·준비 키트·간판·비충돌·pending/active/Raw/저장 권위 보존 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 같은 GameCamera Before/After에서 스케일·초점·동선·가독성을 확인한다.
실행 결과: Runtime/Editor 빌드 오류 0. primitive 생성 제거, B05 BuildingData/Visual-only 복제, 준비 키트·간판, 래퍼/Workbench 비복제, Collider/NavMeshObstacle/행동/Light 제거, Raw·pending/active·v10 저장 보존 계약 18/18 및 `git diff --check` PASS.
남은 확인: 안전 Unity 경로에서 기존 GameCamera와 같은 구도로 B05 기반 가공 준비대의 스케일·정면·간판·상점/분수/NPC 동선 비겹침을 확인한다.
선행 작업: VC-001A, Task 092, B05 시각 최종화

## Task 107 - Utility 다음 날 공구 수리대 변화

상태: PARTIAL
단계: Phase 8
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 실제 Utility 상품·Forge 레시피·B07 Visual과 기존 v10 범용 카테고리 문자열 재사용

목표:
- 실제 `철제 도구` Utility 판매를 기존 판매 감지→pending→다음 날 활성 계약에 연결한다.
- B07 대장간의 실제 실루엣을 비충돌 `공구 수리대` 시각 신호로 사용해 세 번째 판매 카테고리의 마을 반응을 만든다.

Project P.A.와의 연결: 가공품과 원자재에 이어 실용품 판매도 다음 날 마을의 수리·공구 준비 공간으로 보이게 해 `판매 → 마을 변화` 핵심 차별점을 확장한다.

수정 가능 파일: `VillageCultureVisualController.cs`, 관련 작업·마을 변화·에셋 출처·상태 문서.
수정 금지 파일: 판매/구매/경제/NPC 코어, `SalesLogManager`, 저장 스키마/권위, B07 원본 FBX·프리팹·BuildingData, Processed/Raw 변화, 씬·패키지.

구현 조건: 판매 가능한 `Item_12_ToolSet`과 `Recipe_ToolSet`/Forge 연결을 그대로 사용한다. `Buildings/Building_B07_BlacksmithForge`의 래퍼 전체가 아니라 `prefab/Visual`만 복제하고 기존 Project P.A. 간판에 `공구 수리대`를 표시한다. 복제 Collider·Rigidbody·NavMeshObstacle·MonoBehaviour·Light를 제거하며 실제 B07 기능·배치·해금은 복제하지 않는다.
완료 조건: Utility 판매가 다음 DayPreparation에 Utility 루트를 활성화하고 Processed/Raw와 상호 배타적이다. 시각 루트는 실제 B07 메시와 간판으로 구성되고 가짜 기능·물리·조명은 없다. v10 category 문자열 복원은 스키마 변경 없이 Utility를 수용한다.
검증 방법: Runtime/Editor C# 빌드, Utility 상품/레시피/B07 실제 참조, Utility 판매 추적·pending/다음 날·힌트·상호배타, 래퍼/기능/물리/조명 비복제, 기존 카테고리와 저장 권위 보존 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 같은 GameCamera로 스케일·정면·간판·동선을 확인한다.
실패 시 처리: 같은 빌드 또는 같은 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다. 직접 `Camera.Render()` 계열 Unity 실행은 기존 동일 네이티브 충돌 2회 때문에 금지한다.
실행 결과: 표준 로컬 복원 뒤 Runtime/Editor 빌드 오류 0. 판매 가능한 `철제 도구`·Forge 레시피·B07 Visual, Utility 추적/pending/다음 날/상호 배타/힌트, 래퍼 비복제, 물리·행동·Light 제거, Processed/Raw·v10 저장 권위 보존 정적 계약 23/23 PASS.
남은 확인: 안전 Unity 경로에서 Utility 판매 당일에는 변화가 없고 다음 DayPreparation에만 `공구 수리대`가 나타나는지, 같은 GameCamera에서 스케일·정면·간판·상점/분수·플레이어/NPC 동선을 확인한다.
선행 작업: Task 051, Task 103, Task 106

## Task 108 - Luxury 다음 날 공예 전시대 변화

상태: PARTIAL
단계: Phase 8
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 실제 Luxury 상품·기존 Furniture/Sewing 레시피·B08 Visual과 v10 범용 카테고리 문자열 재사용

목표:
- 실제 `목제 가구`/`의류` Luxury 판매를 기존 판매 감지→pending→다음 날 활성 계약에 연결한다.
- B08 재봉대의 파스텔 목재·천·마네킹 실루엣을 비충돌 `공예 전시대`로 사용해 네 번째 판매 카테고리의 마을 반응을 만든다.

Project P.A.와의 연결: 원자재·가공품·실용품에 이어 고급 공예품 판매도 다음 날 마을의 생활 문화 전시로 보이게 해 `판매 → 마을 변화` 핵심 차별점의 주요 판매 카테고리를 모두 연결한다.

수정 가능 파일: `VillageCultureVisualController.cs`, 관련 작업·마을 변화·에셋 감사·상태 문서.
수정 금지 파일: 판매/구매/경제/NPC 코어, `SalesLogManager`, 저장 스키마/권위, B08 원본 FBX·프리팹·BuildingData, Processed/Raw/Utility 변화, 씬·패키지.

구현 조건: 판매 가능한 `Item_11_Furniture`/`Item_13_Clothes`와 기존 `Recipe_Furniture`/`Recipe_Clothes`를 그대로 사용한다. `Buildings/Building_B08_SewingTable`의 래퍼 전체가 아니라 `prefab/Visual`만 0.48배로 복제하고 기존 Project P.A. 간판에 `공예 전시대`를 표시한다. 복제 Collider·Rigidbody·NavMeshObstacle·MonoBehaviour·Light를 제거하며 실제 B08 기능·배치·해금은 복제하지 않는다.
완료 조건: Luxury 판매가 다음 DayPreparation에 Luxury 루트를 활성화하고 Processed/Raw/Utility와 상호 배타적이다. 시각 루트는 실제 B08 메시와 간판으로 구성되고 가짜 기능·물리·조명은 없다. v10 category 문자열 복원은 스키마 변경 없이 Luxury를 수용한다.
검증 방법: Runtime/Editor C# 빌드, Luxury 상품/레시피/B08 실제 참조, Luxury 판매 추적·pending/다음 날·힌트·상호배타, 래퍼/기능/물리/조명 비복제, 기존 3카테고리와 저장 권위 보존 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 같은 GameCamera로 스케일·정면·간판·동선을 확인한다.
실패 시 처리: 같은 빌드 또는 같은 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다. 직접 `Camera.Render()` 계열 Unity 실행은 기존 동일 네이티브 충돌 2회 때문에 금지한다.
실행 결과: Runtime/Editor 빌드 오류 0. 판매 가능한 목제 가구/의류·Furniture/Sewing 레시피·B08 Visual, Luxury 추적/pending/다음 날/상호 배타/힌트, 래퍼 비복제, 물리·행동·Light 제거, 기존 3카테고리·v10 저장 권위 보존 정적 계약 27/27 PASS.
남은 확인: 안전 Unity 경로에서 Luxury 판매 당일에는 변화가 없고 다음 DayPreparation에만 `공예 전시대`가 나타나는지, v10 저장/복원과 같은 GameCamera의 스케일·정면·간판·상점/분수·플레이어/NPC 동선을 확인한다.
선행 작업: Task 047, Task 095, Task 103, Task 107

## Task 109 - 채용 후보 스폰·복원·UI 피드백 연결

상태: PARTIAL
단계: Phase 8
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 비어 있는 후보 프리팹을 역할별 기존 주민 구성으로 안전하게 대체하는 기존 시스템 복구

목표:
- 스마트폰에 표시되는 후보 8명이 빈 `spawnPrefab` 때문에 조용히 실패하던 성장 루프를 실제 채용 가능한 경로로 연결한다.
- 신규 채용과 기존 v10 저장 복원이 같은 역할 원본 해결 규칙을 공유하고, 후보 소개·비용·잠금·성공/실패를 UI에서 읽을 수 있게 한다.

Project P.A.와의 연결: 초반 직접 노동에서 주민·전문가 지원으로 확장되는 장기 성장 약속을 기존 주민 외형과 생산/전문가 시스템 위에서 실제 플레이 가능한 스마트폰 선택으로 만든다.

수정 가능 파일: `Assets/Scripts/HiringService.cs`, `Assets/Scripts/UI/HiringUI.cs`, 관련 작업·루프·상태 문서.
수정 금지 파일: 후보/NPC/레시피 ScriptableObject, 메인 씬·프리팹·C-02~C-09 FBX·텍스처·Avatar, `SaveData`/`SaveManager`/SaveKey, 경제·티어·NPC FSM·구매 권위, 패키지.

구현 조건: 후보의 명시 프리팹이 있으면 계속 최우선으로 사용한다. 없을 때만 같은 `NpcSpecialty`의 기존 Producer 또는 Specialist 중 `NpcController`와 실제 `SkinnedMeshRenderer`가 있는 원본 주민을 찾고, 런타임 채용 clone은 원본 후보에서 영구 배제한다. 비용 차감 전 후보·티어·원본·잔액을 검사하고 기존 `EconomyService.TrySpend`만 사용한다. 신규 채용과 저장 복원에 profile/specialty/schedule/dialogue/고유 친밀도 키를 주입하며 Specialist에는 해당 WorkbenchType의 기존 `Resources/Recipes`만 할당한다.
완료 조건: 후보 8명이 역할별 원본을 해결할 수 있고, 명시 프리팹 우선·중복 고용 차단·잔액/티어 차단·비용 차감 순서·저장 복원이 보존된다. UI는 첫 열기부터 카드가 보이고 소개·한글 역할·비용·잠금 상태와 클릭 결과를 표시한다.
검증 방법: Runtime/Editor C# 빌드, 후보 데이터·역할 소스·실제 메시·clone 제외·비용/복원 순서·정체성 주입·전문가 레시피·UI 상태·보호 파일 비침범 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 스마트폰에서 실제 채용·역할 행동·저장/로드·1920×1080 가독성을 확인한다.
실패 시 처리: 같은 빌드 또는 같은 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다. 직접 `Camera.Render()` 계열 Unity 실행은 기존 동일 네이티브 충돌 2회 때문에 금지한다.
실행 결과: Runtime/Editor 빌드 오류 0. 첫 보호 경계 검사 1건은 `SaveManager`의 설명 주석을 쓰기 호출로 오인한 selector false negative였고, 실제 파일을 대조해 바로잡은 단일 재검사에서 후보/역할/메시/clone/경제/복원/정체성/레시피/UI/보호 경계 계약 36/36 PASS.
남은 확인: 안전 Unity 경로에서 충분/부족 잔액, 8역할 중 Producer/Specialist 실제 스폰과 행동, 중복 방지, 저장 후 재실행 복원, 스마트폰 1920×1080 가독성을 확인한다.
선행 작업: 기존 HiringService/HiringUI, Task 096, Task 098

## Task 110 - 첫 주 채용 성장 목표 연결

상태: PARTIAL
단계: Phase 8
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — Task 109의 기존 채용 상태를 첫 주 목표·체크리스트·결산에서 읽는 프레젠테이션 연결

목표:
- Day 5의 추상적인 인력 필요 안내를 실제 P.A. Phone 채용 행동으로 바꾼다.
- Day 5 이후 운영 체크리스트에서 고용 전/후와 현재 생산자·전문가 지원 인력을 읽게 한다.
- Day 7 첫 주 결산에 실제 고용 인력 이름·역할을 남긴다.

Project P.A.와의 연결: 첫 주 안에 직접 생활 노동에서 주민·전문가 지원으로 확장되는 성장 약속을 발견→실행→결산의 플레이 경로로 만든다.

수정 가능 파일: `Assets/Scripts/UI/PlayableDayScenarioController.cs`, `Assets/Scripts/LongPlayProgressionController.cs`, 관련 작업·루프·상태 문서.
수정 금지 파일: `HiringService`, 후보/NPC/레시피 에셋, 경제·티어·채용 비용, NPC FSM, 저장 스키마/권위, 메인 씬·프리팹·FBX·텍스처·Avatar, 패키지.

구현 조건: 기존 `HiringService.HiredCount`와 `GetHiredCandidates()`만 읽는다. Day 1~4와 생활 활동·상품 2종·진열/가격·개점·판매/정산 체크리스트를 보존한다. 고용 이벤트는 표시 갱신만 하며 고용·비용·스폰·저장은 기존 소유자가 담당한다.
완료 조건: Day 5 이후 0명일 때 스마트폰 채용 행동, 1명 이상일 때 인원·실제 후보 이름·한글 역할이 완료 상태로 보인다. 같은 roster가 Day 7 결산에 표시되고 고용 직후 LongPlay UI가 갱신된다.
검증 방법: Runtime/Editor 빌드, Day 5 전후·0명/1명 이상·실제 후보 identity/8역할·결정론 roster·고용 이벤트 구독/해제·Day 7 결산·기존 체크리스트/권위 비침범 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 Day 5 채용→Day 7 결산과 1920×1080 가독성을 확인한다.
실패 시 처리: 같은 빌드 또는 같은 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다. 직접 `Camera.Render()` 계열 Unity 실행은 기존 동일 네이티브 충돌 2회 때문에 금지한다.
실행 결과: Runtime/Editor 빌드 오류 0. Day 5 이후 목표/체크리스트·미고용/고용 분기·실제 roster·8개 한글 역할·이벤트 갱신·첫 주 결산·Day 1~4 및 기존 권위 보존 정적 계약 29/29 PASS.
남은 확인: 안전 Unity 경로에서 Day 5 P.A. Phone 채용 전후 체크리스트가 즉시 바뀌고 Day 7 결산에 같은 인력이 표시되는지, 긴 후보 이름과 roster가 1920×1080에서 잘리지 않는지 확인한다.
선행 작업: Task 090, Task 098, Task 109

## Task 111 - 생산자 납품 원자 거래·피드백 복구

상태: PARTIAL
단계: Phase 8
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 Inventory/Economy/Producer 경로의 확인된 상품·돈 유실 복구

목표:
- 생산자 NPC가 가방이 가득 찬 플레이어에게 납품할 때 돈과 생산물이 함께 사라지던 거래 단절을 제거한다.
- 채용 이후 생산 지원이 `NPC 재고 → 매입 → 플레이어 재고 → 가공/진열`로 안전하게 이어지고, 성공·보류 이유가 플레이어에게 보이게 한다.

Project P.A.와의 연결: 직접 노동에서 NPC 지원으로 성장하는 역-공급망의 실제 소유권 이전을 안전하게 만들어 낮 준비와 밤 판매 사이의 재고 연결을 보존한다.

수정 가능 파일: `Assets/Scripts/Inventory.cs`, `Assets/Scripts/ProducerNpcController.cs`, 관련 작업·루프·상태 문서.
수정 금지 파일: `EconomyService`, `LongPlayProgressionController`, `SaveData`/`SaveManager`/SaveKey, NPC FSM/스케줄·프로필·생산 데이터, 채용·구매/판매 권위, 씬·프리팹·아이템 에셋·패키지.

구현 조건: `Inventory.AddInstance`는 메타 일치 스택/빈 슬롯의 전량 수용을 변경 없이 먼저 검사해 실패 시 부분 이동하지 않는다. 생산자는 결제 전에 전량 수용을 확인하고 가방 가득 참·잔액 부족이면 NPC 재고를 유지한다. 결제 뒤 예상 밖 실패는 기존 `EconomyService.TryModifyMoney`로 전액 환불하며 재고를 제거하지 않는다. 성공한 `ItemInstance`의 quality/currentPrice를 보존하고 기존 `NpcBubbleUI`로 성공·보류 피드백을 표시한다.
완료 조건: 가방 가득 참은 돈/NPC 재고/플레이어 재고 무변경, 잔액 부족은 재고 보존, 성공은 정확 수량·비용·메타 소유권 이전 후에만 NPC 재고 제거다. 기존 일일 LongPlay `AddInstance`/환불 경로, NPC FSM·스케줄, 저장·상점 권위는 유지된다.
검증 방법: Runtime/Editor C# 빌드, `CanAddInstance` 읽기 전용·AddInstance 실패 원자성·생산자 공간→결제→메타 이전→예외 환불→성공 제거 순서·말풍선·LongPlay 호환 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 가방 가득 참 무차감/재고 보존→공간 확보→정상 납품과 말풍선을 확인한다.
실패 시 처리: 같은 빌드 또는 같은 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다. 직접 `Camera.Render()` 계열 Unity 실행은 기존 동일 네이티브 충돌 2회 때문에 금지한다.
실행 결과: Runtime/Editor 오류 0. 전량 수용 선검사, 실패 무변경, 가방/잔액 보류, 결제 후 예외 환불, 메타 이전, 성공 후 제거, 기존 말풍선/FSM/LongPlay 경로 계약 30/30과 `git diff --check` PASS.
남은 확인: 안전 Unity 경로에서 실제 Producer가 가방 가득 참 상태에서 돈·양쪽 재고를 보존하고, 공간 확보 후 같은 스택을 정확한 비용으로 납품하며 성공/보류 말풍선이 보이는지 확인한다.
선행 작업: FG-002, Task 091, Task 102, Task 109, Task 110

## Task 112 - 2주차 운영 캠페인 연결

상태: PARTIAL
단계: Phase 8
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 이미 구현된 보관·가공·채용·판매·Tier·마을 변화 상태를 Day 8~14 목표로 연결하는 읽기 전용 진행 표시

목표:
- Day 7 완주 후 Day 8이 일반 반복 문구로 떨어지는 장기 루프 공백을 제거한다.
- 2주차에 매일 다른 실제 운영 결정을 요구하고, 기존 생활–상점 체크리스트에서 완료 상태를 읽게 한다.

Project P.A.와의 연결: 첫 주의 낮 준비→밤 영업을 보관·가공·인력·상품 구성·상점 확장·마을 변화로 확장해 “판 물건이 다음 날과 성장 방향을 바꾼다”는 반복 플레이를 두 번째 주까지 연결한다.

수정 가능 파일: `Assets/Scripts/LongPlayProgressionController.cs`, `Assets/Scripts/UI/PlayableDayScenarioController.cs`, 관련 작업·루프·상태 문서.
수정 금지 파일: `StorageBox`/`SalesLogManager`/`HiringService`/`TierService`/`VillageCultureVisualController` 권위, 경제·구매·제작·채용·저장 스키마, 메인 씬·프리팹·에셋·패키지.

구현 조건: Day 8~14는 B09 보관→Processed 판매→채용→2카테고리 판매→Tier 1→활성 마을 변화→2상품 판매 순으로 기존 런타임 상태만 읽는다. Day 1~7 계획과 Day 7 완주 모달/Day 8 전환, 기본 활동·상품 2종·진열/가격·개점·판매/정산 체크리스트를 보존한다. 2주차에는 자동 온보딩 상품을 지급하지 않는다.
완료 조건: Day 8~14의 서로 다른 목표가 LongPlay 계획과 플레이어 체크리스트에 존재하고 실제 보관량·당일 판매 기록·채용 roster·Tier·마을 변화로 완료 상태가 바뀐다. 어떠한 돈·아이템·제작·채용·Tier·저장 변경도 새 표시 계층이 수행하지 않는다.
검증 방법: Runtime/Editor C# 빌드, 7개 Week 2 계획·실제 상태 읽기·당일 범위·Day 1~7/Day 7 완주/기본 체크리스트·권위 비침범 정적 계약, `git diff --check`. 안전 Unity 경로 승인 뒤 Day 7→8과 Day 8~14 대표 목표 전환 및 1920×1080 가독성을 확인한다.
실패 시 처리: 같은 빌드 또는 같은 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다. 직접 `Camera.Render()` 계열 Unity 실행은 기존 동일 네이티브 충돌 2회 때문에 금지한다.
실행 결과: Runtime/Editor 오류 0. Week 1 보존, Day 8~14 계획, 실제 B09/판매/채용/Tier/마을 변화 읽기, 표시 전용 권위 계약 40/40과 `git diff --check` PASS.
남은 확인: 안전 Unity 경로에서 Day 7 저장→Day 8 시작 뒤 보관 목표부터 Day 14 상품 다양화까지 대표 상태가 0.5초 안에 바뀌고, 목표/체크리스트가 1920×1080에서 기존 HUD와 겹치지 않는지 확인한다.
선행 작업: FG-001, Task 034, Task 090, Task 098, Task 102, Task 103, Task 109, Task 110

## Task 113 - Tripo 장기 정책 재감사와 B12 항구 보이지 않는 충돌 방지

상태: PARTIAL
단계: Phase 8
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 Tripo/Placeable 장기 정책과 검증된 메시 bounds를 사용하는 축소 전용 런타임 보정

목표:
- 최신 Tripo3D 임시 에셋 최종화 지침을 기존 8분류·Placeable·출처 ADR에 재대조해 장기 기준으로 유지한다.
- B12 TradePort의 역사적 10×5m 루트 물리가 실제 약 3.63×1.96m Visual보다 커 만드는 해안의 보이지 않는 벽과 NPC carving 공백을 제거한다.

Project P.A.와의 연결: 핵심 낮 생활·마을 이동에서 보이는 모델과 실제 이동 경계를 일치시키고, 임시 에셋을 무작정 교체하지 않으면서 향후 격자/기능 승격 기준을 유지한다.

수정 가능 파일: `Assets/Scripts/DemoVisualDressingController.cs`, `Docs/Codex/TRIPO_ASSET_AUDIT.md`, `Docs/Codex/ASSET_AND_TOOL_PROVENANCE.md`, `Docs/Codex/ARCHITECTURE_DECISIONS.md`, 관련 작업·루프·상태 문서.
수정 금지 파일: B12 원본 FBX·텍스처·래퍼 프리팹·메인 씬·BuildingData·청사진, 교역/이벤트 기능, 배치 카탈로그, 저장 스키마, Shop/NPC 코어, 패키지.

구현 조건: 활성 B12 Visual의 로컬 mesh bounds를 8모서리로 계산한다. 루트 `BoxCollider`와 box형 `NavMeshObstacle`이 명백히 큰 경우에만 각 축을 축소하며 절대 키우지 않는다. 메시가 없으면 기존 물리를 유지하고 다음 갱신에서 재시도한다. B12는 Task 079 전까지 정적 Protected/Developer 세계 에셋으로 유지하고 가짜 상호작용·Placeable·NPC 접근점을 추가하지 않는다.
완료 조건: 활성 B12의 플레이어 충돌과 NPC carving 범위가 같은 Visual 근거로 축소되고, 원본/프리팹/씬/기능/저장이 보존된다. 안전 Unity 경로에서 기존 투명 벽 영역 통과, 보이는 항구 경계 정지와 NPC 우회를 확인한다.
검증 방법: Runtime/Editor C# 빌드, B12 map/legacy 탐색·메시 bounds·8모서리·축소 전용 Box/Obstacle·메시 실패 보존·원본/기능 비침범 정적 계약, `git diff --check`. 안전 Unity 경로에서 같은 GameCamera와 실제 이동/NPC 우회를 확인한다.
실패 시 처리: 같은 빌드 또는 같은 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다. 직접 `Camera.Render()` 계열 Unity 실행은 기존 동일 네이티브 충돌 2회 때문에 금지한다.
실행 결과: Runtime/Editor 빌드 오류 0. 활성 map/legacy B12, Visual bounds, 8모서리, Box/Obstacle 축소 전용, 메시 실패 보존, 원본/기능/Placeable 경계 계약 20/20과 대상 diff 검사 PASS.
남은 확인: 안전 Unity 경로에서 B12 좌우의 기존 10×5m 투명 벽 영역을 실제로 통과하고 보이는 부두 경계에서 정지하는지, NPC carving 우회와 같은 GameCamera 구도를 확인한다.
선행 작업: Task 087, Task 094, Task 104

## Task 114 - 첫 달 운영 캠페인과 Day 30 완주점

상태: PARTIAL
단계: Phase 8
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 보관·가공·채용·판매·Tier·마을 변화와 저장/다음 날 권위를 읽어 장기 진행을 연결

목표:
- Day 15부터 Day 30까지 일반 반복 문구로 남던 구간을 실제 운영 목표가 있는 첫 달 캠페인으로 연결한다.
- Day 30 정산에 완주 요약과 저장 후 종료/Day 31 계속 선택을 제공해 플레이어가 도달할 수 있는 기능적 끝점을 만든다.

Project P.A.와의 연결: 낮의 생활·생산 지원·재고 관리가 밤의 상품 구성과 판매를 거쳐 마을 변화와 상점 성장으로 누적되는 한 달의 완전한 게임 루프를 만든다.

수정 가능 파일: `Assets/Scripts/LongPlayProgressionController.cs`, `Assets/Scripts/UI/PlayableDayScenarioController.cs`, `Assets/Scripts/DayNightShopLoopController.cs`, 관련 작업·루프·상태 문서.
수정 금지 파일: 보관/판매/채용/Tier/마을 변화/경제/제작 권위, `SaveData`/`SaveManager`/SaveKey, 메인 씬·프리팹·에셋·패키지.

구현 조건: Day 7 자동 보급 종료와 첫 주 완주를 보존한다. Day 15~30은 보관량, 당일 카테고리/상품 판매, 실제 고용 인원, Tier, 활성 마을 변화만 읽는다. Day 30 Settlement에서 한 달 성과를 표시하며 계속은 Day 30 저장→기존 다음 날 권위→Day 31 재저장, 종료는 저장 성공 뒤 Quit 순서를 사용한다.
완료 조건: Day 15~30 계획과 체크리스트가 모두 존재하고, Day 30 완주 모달이 누적 매출·돈·Tier·평판·고용·마을 변화·당일 정산을 표시한다. Day 7 경계와 모든 기존 시스템 권위가 보존된다.
검증 방법: Runtime/Editor C# 빌드, 16개 계획·16개 상태 목표·Day 7 보급 경계·Day 30 Settlement·Day 31 저장 전환·공용 완료 게이트 정적 계약, `git diff --check`. 안전 Unity 경로에서 대표 Day 15~30 상태 전환, Day 30 두 버튼, Day 31 이어하기와 1920×1080 가독성을 확인한다.
실패 시 처리: 병렬 Runtime/Editor 빌드는 공유 출력 잠금을 만들 수 있으므로 순차 빌드만 사용한다. 같은 빌드 또는 같은 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다. 직접 `Camera.Render()` 계열 Unity 실행은 기존 동일 네이티브 충돌 2회 때문에 금지한다.
실행 결과: Runtime/Editor 순차 빌드 경고 0·오류 0. Day 7 보존, Day 15~30 계획/체크리스트, Day 30 요약, Day 31 계속, 기존 상태 권위 재사용 계약 56/56과 대상 diff 검사 PASS.
남은 확인: 안전 Unity 경로에서 대표 Day 15~30 목표가 실제 상태에 맞춰 갱신되는지, Day 30 정산 모달의 두 분기와 Day 31 이어하기, 1920×1080 가독성을 확인한다.
선행 작업: Task 034, Task 098, Task 102, Task 109, Task 112

## Task 115 - Tier 1 대장간·철제 도구 첫 달 가치사슬

상태: PARTIAL
단계: Phase 8
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 B07/ToolSet의 Tier 1 권위 데이터를 배치 보상과 첫 달 목표에 정합화

목표:
- `Building_B07`, B07 설계도, `Recipe_ToolSet`, `Item_12_ToolSet`이 모두 Tier 1인데 배치 카탈로그만 Tier 3으로 지연되던 기능 단절을 제거한다.
- Day 23의 씨앗으로 우회 가능한 Utility 목표와 Day 24의 100,000G Tier 2 전에는 불가능한 Luxury 목표를 실제 대장간 가치사슬로 교체한다.
- Day 14의 15,000G 뒤 Day 15 목표가 3,900G로 역행하던 첫 달 누적 매출 표시를 단조 증가로 교정한다.

Project P.A.와의 연결: 낮의 Wood/Ore 확보→B05 Plank→B07 IronBar/ToolSet 제작→밤 Utility/Processed 혼합 판매→다음 날 공구 수리대 변화가 첫 달 안에서 실제 플레이 가능한 고부가가치 역-공급망이 되게 한다.

수정 가능 파일: `Assets/Scripts/ShopCustomizationController.cs`, `Assets/Scripts/LongPlayProgressionController.cs`, `Assets/Scripts/UI/PlayableDayScenarioController.cs`, `Assets/Editor/PA_ShopProgressionUnlockValidator.cs`, 관련 작업·루프·배치·에셋 감사·상태 문서.
수정 금지 파일: `TierService`/`TierDefinition`, 레시피·아이템·BuildingData, B07 원본 FBX·프리팹, 제작/판매/경제/저장 권위, 메인 씬, 패키지.

구현 조건: B07의 배치 최소 Tier와 장부 설계도 보상만 기존 권위 데이터의 Tier 1에 맞춘다. B05 starter 보상과 B06 Tier 2/B08 Tier 3을 보존한다. Day 23은 활성 B07+정확한 당일 ToolSet+다른 상품 1종, Day 24는 활성 B07+정확한 ToolSet+Processed 1건을 기존 상태에서만 읽는다. 아이템 지급·자동 제작/배치/판매·Tier 강제는 금지한다. 첫 달 목표는 Day 15=16,000G부터 Day 30=31,000G, Day 31 이후도 역행 없이 증가한다.
완료 조건: Tier 1 장부를 열면 중복 없이 B05/B07 설계도를 받고, B07을 배치해 기존 Forge 레시피를 사용할 수 있다. 씨앗만 팔아 Day 23을 완료하거나 Tier 2 전 Luxury 때문에 Day 24가 막히지 않는다. B06/B08과 10,000G/100,000G Tier 조건은 유지된다.
검증 방법: Runtime/Editor 순차 C# 빌드, B07 BuildingData/설계도/레시피/상품/프리팹 권위, 보상 중복 방지, 활성 배치, 정확한 당일 상품, Day 23/24 완료 조건, 매출 목표 단조 증가, P5 Tier 1/2/3 기대값 정적 계약, 대상 `git diff --check`. 안전 Unity 경로에서 실제 Tier 1 장부→진열대 회수→B07 배치→IronBar/ToolSet 제작→Day 23/24 판매를 확인한다.
실패 시 처리: 같은 빌드 또는 같은 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다. 직접 `Camera.Render()` 계열 Unity 실행은 기존 동일 네이티브 충돌 2회 때문에 금지한다.
실행 결과: Runtime/Editor 순차 빌드 오류 0. 기존 기준 경고 CS8785와 Editor CS0414만 유지됐다. B07 네 권위 데이터·Tier 보상·중복 방지·활성 배치·정확한 당일 ToolSet·Day 23/24·단조 매출 목표·P5 Tier 보존 실행 가능 소스 계약 39/39과 대상 diff 검사 PASS.
남은 확인: 안전 Unity 경로에서 Tier 1 장부가 B05/B07을 함께 지급하는지, 빈 진열대 두 칸 회수 뒤 B07 3×2 배치와 접근이 가능한지, Plank1+Ore4→IronBar2→ToolSet1 제작·판매, Day 23/24 완료 전환과 1920×1080 안내 가독성을 확인한다.
선행 작업: Task 090, Task 103, Task 107, Task 112, Task 114

## Task 116 - 안전 GameView 캡처 기반 1차 전환

상태: PARTIAL
단계: Phase 0 / Phase 8 공용 검증 기반
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 코드 감사·전환만. Unity 재실행은 직접 렌더 전수 전환과 사람 판단 뒤 별도 수행

목표:
- 2026-07-17 같은 URP 네이티브 충돌을 두 번 일으킨 별도 `RenderTexture`+`Camera.Render()` 캡처 경로를 전수 감사한다.
- 이미 실제 실행을 완료한 ThemeCorner의 일반 GameView `ScreenCapture` 방식을 공용 도우미로 추출한다.
- 두 번째 충돌 지점인 ShopCustomization과 현재 Tier/B07 검증용 ShopProgression을 같은 안전 경로로 1차 전환한다.

Project P.A.와의 연결: 실제 게임 카메라를 보지 못해 PARTIAL로 남은 배치·상점 성장·낮 생활·마을 변화·UI 작업을 다시 검증 가능한 상태로 복구한다.

수정 가능 파일: `Assets/Editor/PA_ThemeCornerValidator.cs`, `Assets/Editor/PA_ShopCustomizationValidator.cs`, `Assets/Editor/PA_ShopProgressionUnlockValidator.cs`, 비주얼 툴체인·작업·상태 문서.
수정 금지 파일: 모든 런타임 게임 코드, 메인 씬·프리팹·에셋, 저장/경제/NPC/배치 권위, 패키지, ProjectSettings, 그래픽 API.

구현 조건: 공용 도우미는 `Screen.SetResolution`→Canvas/TMP 갱신→일반 `ScreenCapture.CaptureScreenshot`→새 PNG/최소 크기 대기 순서를 사용하고, 카메라 target/transform/projection/culling/viewport와 이전 화면 상태를 `finally`에서 복원한다. 어떤 직접 `Camera.Render()`도 호출하지 않는다. 이번 1차 작업은 세 파일만 바꾸고 저장소에 남은 10곳은 정확히 기록한다.
완료 조건: ThemeCorner·ShopCustomization·ShopProgression의 실제 직접 렌더 호출이 0이고, 후자의 기존 캡처 구도와 파일 검증이 공용 비동기 경로로 유지된다.
검증 방법: Runtime/Editor 순차 C# 빌드, 공용 캡처·파일 freshness/크기·상태 복원·두 검증기 await·직접 렌더 0·남은 호출 10 계약, 대상 `git diff --check`. Unity는 동일 원인 세 번째 실행 금지 경계를 지킨다.
실패 시 처리: 같은 빌드 또는 같은 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다. 남은 직접 렌더가 있는 동안 Unity 검증을 실행하지 않는다.
실행 결과: Runtime 빌드 경고/오류 0. Editor 오류 0, 기존 CS8785/CS0414 경고 2개. 공용 안전 캡처와 두 검증기 전환 계약 28/28, 대상 직접 렌더 0, 저장소 잔여 직접 렌더 10, 대상 diff 검사 PASS.
남은 확인: 잔여 10개 Editor 캡처 호출을 후속 분할 작업으로 전환하고 정적 전수 0을 확인한 뒤, 사람 승인 아래 D3D11에서 격리 캡처 1회와 ShopCustomization/ShopProgression을 순차 확인한다.
선행 작업: PROJECT_PA_CRASH_REPORT_20260717, Task 086, Task 115

## Task 117 - 안전 GameView 캡처 기반 2차 전환

상태: PARTIAL
단계: Phase 0 / Phase 8 공용 검증 기반
난이도: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 코드 전환만. Unity 재실행은 직접 렌더 전수 전환과 사람 판단 뒤 별도 수행

목표:
- Task 116 이후 남은 직접 `Camera.Render()` 10곳 중 플레이어 노출도가 높은 VillageCulture, CustomerPanelLayout, FinalPresentation을 공용 GameView 캡처로 전환한다.
- 세 검증기의 마을 변화 전후, 1920×1080 UI, 최종 프레젠테이션 6장 상태 진행을 캡처 완료까지 기다리는 비동기 순서로 유지한다.

Project P.A.와의 연결: Raw/Processed/Utility/Luxury 다음 날 변화와 고객 UI·최종 발표 화면을 같은 안전한 실제 GameView 경로로 다시 확인할 기반을 넓힌다.

수정 가능 파일: `Assets/Editor/PA_VillageCultureVisualValidator.cs`, `Assets/Editor/PA_CustomerPanelLayoutValidator.cs`, `Assets/Editor/PA_FinalPresentationReviewer.cs`, 비주얼 툴체인·작업·상태 문서.
수정 금지 파일: 공용 도우미 구현, 모든 런타임 게임 코드, 메인 씬·프리팹·에셋, 저장/경제/NPC/배치 권위, 패키지, ProjectSettings, 그래픽 API.

구현 조건: 각 검증기는 `Task` 실행 가드로 중복 진입을 막고 모든 캡처를 `await`한다. 시장 마커/FOV 46/전체 레이어/1920×1080 구도를 공용 도우미 설정 콜백에 전달한다. VillageCulture·CustomerPanel의 기존 헤드리스 캡처 실패 경고 정책과 FinalPresentation의 6개 출력 파일 계약을 보존한다.
완료 조건: 세 대상 파일의 `RenderTexture`·`ReadPixels`·직접 `camera.Render()`가 0이고, 공용 캡처 await와 기존 상태/파일 순서가 유지된다. 저장소 잔여 실제 직접 렌더는 정확히 7곳으로 줄어든다.
검증 방법: Runtime/Editor 순차 C# 빌드, 비동기 가드·캡처 수·1920×1080·시장 구도·전체 레이어·공용 복원 위임·대상 직접 렌더 0·잔여 7곳 정적 계약, 대상 `git diff --check`. Unity는 동일 원인 세 번째 실행 금지 경계를 지킨다.
실패 시 처리: 같은 빌드 또는 같은 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다. 남은 직접 렌더가 있는 동안 Unity 검증을 실행하지 않는다.
실행 결과: Runtime 빌드 경고/오류 0. Editor 오류 0, 기존 CS8785/CS0414 경고 2개. 세 검증기의 대상 직접 렌더 0, 안전 캡처 계약 38/38, 저장소 잔여 직접 렌더 7, 대상 diff 검사 PASS.
남은 확인: Character/Cottage/DemoView/GatheringShop/OutdoorPlacement/ShopEvolution/Workbench 7곳을 후속 분할 작업으로 전환하고 정적 전수 0을 확인한 뒤, 사람 승인 아래 D3D11에서 격리 캡처 1회와 순차 검증을 수행한다.
선행 작업: PROJECT_PA_CRASH_REPORT_20260717, Task 116

## Task 118 - 안전 GameView 캡처 기반 3차 전환

상태: PARTIAL
단계: Phase 0 / Phase 8 공용 검증 기반
사이즈: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 코드 전환만. Unity 재실행은 직접 렌더 전수 전환과 사람 판단 뒤 별도 수행

목표:
- Task 117 이후 남은 직접 `Camera.Render()` 7곳 중 실제 플레이 카메라 기준점, 낮 활동→밤 판매 검토, 야외 배치·충돌 검토를 담당하는 DemoView, GatheringShop, OutdoorPlacement를 공용 GameView 캡처로 전환한다.
- DemoView의 실내/외부 준비 대기와 실제 추적 카메라, GatheringShop의 5단계 상태 순서, OutdoorPlacement의 동일 직교 구도 전후 2장을 캡처 완료까지 기다리는 비동기 순서로 유지한다.

Project P.A.와의 연결: 실제 플레이 화면, 낮 채집→밤 상점 연결, 보이지 않는 충돌과 배치 정합을 같은 안전한 GameView 경로로 다시 확인할 기반을 넓힌다.

수정 가능 파일: `Assets/Editor/PA_DemoViewCapture.cs`, `Assets/Editor/PA_GatheringShopReview.cs`, `Assets/Editor/PA_OutdoorPlacementValidator.cs`, 비주얼 툴체인·작업·상태 문서.
수정 금지 파일: 공용 도우미 구현, 모든 런타임 게임 코드, 메인 씬·프리팹·에셋, 저장/경제/NPC/배치 권위, 패키지, ProjectSettings, 그래픽 API.

구현 조건: 각 캡처 흐름은 `Task` 실행 가드와 `await`를 사용한다. DemoView는 기존 실내 11초/외부 4.5초 준비와 실제 추적 카메라·2560×1440을, GatheringShop은 해안/시장 구도와 5개 1920×1080 상태를, OutdoorPlacement는 전후 동일 1280×720 직교 구도와 파일 크기 판정을 보존한다. 캡처 중 `CameraController`만 일시 정지하고 반드시 복원한다.
완료 조건: 세 대상 파일의 `RenderTexture`·`ReadPixels`·직접 `camera.Render()`가 0이고, 공용 캡처 await와 기존 상태/파일/구도가 유지된다. 저장소 잔여 실제 직접 렌더는 정확히 4곳으로 줄어든다.
검증 방법: Runtime/Editor 순차 C# 빌드, 비동기 가드·캡처 수·해상도·준비 시간·구도·컨트롤러 복원·파일 판정·대상 직접 렌더 0·잔여 4곳 정적 계약, 대상 `git diff --check`. Unity는 동일 원인 세 번째 실행 금지 경계를 지킨다.
실패 시 처리: 같은 빌드 또는 같은 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다. 남은 직접 렌더가 있는 동안 Unity 검증을 실행하지 않는다.
실행 결과: Runtime 빌드 경고/오류 0. Editor 오류 0, 기존 CS8785/CS0414 경고 2개. 세 검증기의 대상 직접 렌더 0, 안전 캡처 계약 35/35, 저장소 잔여 직접 렌더 4, 대상 diff 검사 PASS.
남은 확인: Character/Cottage/ShopEvolution/Workbench 4곳을 후속 분할 작업으로 전환하고 정적 전수 0을 확인한 뒤, 사람 승인 아래 D3D11에서 격리 캡처 1회와 순차 검증을 수행한다.
선행 작업: PROJECT_PA_CRASH_REPORT_20260717, Task 116, Task 117

## Task 119 - 안전 GameView 캡처 기반 4차 전환

상태: PARTIAL
단계: Phase 0 / Phase 8 공용 검증 기반
사이즈: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 코드 전환만. Unity 재실행은 직접 렌더 전수 전환과 사람 판단 뒤 별도 수행

목표:
- Task 118 이후 남은 직접 `Camera.Render()` 4곳 중 비주얼 우선순위가 높은 플레이어/NPC 접지·보행, B10 건축 스케일, B05 작업대 기능 정합을 담당하는 Character, Cottage, Workbench를 공용 GameView 캡처로 전환한다.
- 소스/런타임/회전 감사의 기존 파일명·해상도·구도와 접지·콜라이더·작업 방향·실제 제작 계약을 캡처 완료까지 기다리는 비동기 순서로 유지한다.

Project P.A.와의 연결: 플레이어와 주민의 자연스러운 이동, 생활형 건축물, 낮 가공→밤 판매 준비 공간을 같은 안전한 실제 GameView 경로로 확인할 기반을 완성 직전까지 넓힌다.

수정 가능 파일: `Assets/Editor/PA_CharacterFinalizer.cs`, `Assets/Editor/PA_CottageVisualFinalizer.cs`, `Assets/Editor/PA_WorkbenchFinalizer.cs`, 비주얼 툴체인·작업·상태 문서.
수정 금지 파일: 공용 도우미 구현, `PA_ShopEvolutionVisualFinalizer`, 모든 런타임 게임 코드, 메인 씬·프리팹·에셋, 저장/경제/NPC/배치 권위, 패키지, ProjectSettings, 그래픽 API.

구현 조건: 세 검증기는 `Task` 실행 가드와 `await`를 사용한다. Character는 1600×900 소스 lineup과 idle/walk, 0.35초 준비·0.6초 이동·0.25초 종료 순서를 보존한다. Cottage는 1920×1080 전경·4방향·최종·런타임 7개 파일과 renderer 격리를 보존한다. Workbench는 1920×1080 감사/최종 4방향과 runtime baseline/final, 실제 Wood→Plank 제작 계약을 보존한다. 실제 카메라 캡처 중 `CameraController`만 일시 정지하고 반드시 복원한다.
완료 조건: 세 대상 파일의 `RenderTexture`·`ReadPixels`·직접 `camera.Render()`가 0이고, 공용 캡처 await와 기존 상태/파일/구도가 유지된다. 저장소 잔여 실제 직접 렌더는 `PA_ShopEvolutionVisualFinalizer` 1곳뿐이다.
검증 방법: Runtime/Editor 순차 C# 빌드, 비동기 가드·캡처 수·파일명·해상도·이동/구도·renderer/controller 복원·기능 판정·대상 직접 렌더 0·잔여 1곳 정적 계약, 대상 전체 공백 검사. Unity는 동일 원인 세 번째 실행 금지 경계를 지킨다.
실패 시 처리: 같은 빌드 또는 같은 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다. 남은 직접 렌더가 있는 동안 Unity 검증을 실행하지 않는다.
실행 결과: Runtime 빌드 경고/오류 0. Editor 오류 0, 기존 CS8785/CS0414 경고 2개. 세 검증기의 대상 직접 렌더 0, 안전 캡처 계약 42/42, 저장소 잔여 직접 렌더 1, 대상 공백 검사 PASS.
남은 확인: `PA_ShopEvolutionVisualFinalizer` 1곳을 마지막 분할 작업으로 전환하고 저장소 실제 직접 렌더 0을 확인한 뒤, 사람 승인 아래 D3D11에서 격리 캡처 1회와 순차 검증을 수행한다.
선행 작업: PROJECT_PA_CRASH_REPORT_20260717, Task 116, Task 117, Task 118

## Task 120 - 안전 GameView 캡처 기반 최종 전환

상태: PARTIAL
단계: Phase 0 / Phase 8 공용 검증 기반
사이즈: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 코드 전환만. 세 번째 Unity 실행과 실제 캡처는 사람 판단 뒤 별도 수행

목표:
- 저장소에 마지막으로 남은 `PA_ShopEvolutionVisualFinalizer`의 직접 `Camera.Render()`를 공용 GameView 캡처로 전환해 실제 직접 렌더 호출을 전수 0으로 만든다.
- B02~B04 소스 감사 4방향 12장과 런타임 Tier 1~3 성장 증거의 파일명·1600×900 구도·Tier 안정화·배치 저장 불변 판정을 캡처 완료까지 기다리는 비동기 순서로 유지한다.

Project P.A.와의 연결: 상점 성장 단계가 플레이어의 운영 성과를 실제 외관 변화로 보여 주는 핵심 프레젠테이션이므로, 반복 충돌 경로 없이 동일 성장 증거를 생성할 수 있는 마지막 자동화 기반을 닫는다.

수정 가능 파일: `Assets/Editor/PA_ShopEvolutionVisualFinalizer.cs`, 비주얼 툴체인·작업·상태 문서.
수정 금지 파일: 공용 도우미 구현, 모든 런타임 게임 코드, 메인 씬·프리팹·에셋, 저장/경제/NPC/배치 권위, 패키지, ProjectSettings, 그래픽 API.

구현 조건: 소스 감사는 중복 실행 가드와 `await`를 사용하고 B02~B04 각각 minus/plus Z/X 캡처를 보존한다. 런타임은 단일 `Task` 가드로 Tier 1→2→3을 순차 처리하며 초기 4초, 단계별 0.75초, 기존 위치·orthographic size 6.6, baseline/after 파일명과 저장 스냅샷 동등성을 유지한다. 실제 카메라 캡처 중 `CameraController`만 일시 정지하고 반드시 복원한다.
완료 조건: 대상의 `RenderTexture`·`ReadPixels`·직접 `camera.Render()`가 0이고, 저장소 전체 실제 직접 렌더 호출도 0이다. 공용 캡처 await와 기존 단계·파일·구도·저장 불변 판정이 유지된다.
검증 방법: Runtime/Editor 순차 C# 빌드, 비동기 가드·12개 회전 파일·런타임 파일·1600×900·4초/0.75초·구도·controller/clearFlags 복원·배치 저장 동등성·대상/저장소 직접 렌더 0 정적 계약, 13개 변경 경로·JSON·공백 검사. Unity는 동일 원인 세 번째 실행 금지 경계를 지킨다.
실패 시 처리: 같은 빌드 또는 같은 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다. 사람 판단 전 Unity를 실행하지 않는다.
실행 결과: Runtime 빌드 경고/오류 0. Editor 오류 0, 기존 CS8785/CS0414 경고 2개. 안전 캡처·성장 단계·상태 복원 계약 36/36 PASS, 대상과 저장소 실제 직접 렌더 0.
남은 확인: 사람 판단 뒤 D3D11에서 격리 GameView PNG 1회를 실행해 freshness·파일 크기·가독성·카메라/화면 복원을 확인한다. 성공한 경우에만 전환된 검증기를 순차 실행한다.
선행 작업: PROJECT_PA_CRASH_REPORT_20260717, Task 116, Task 117, Task 118, Task 119

## Task 121 - Day 31~45 두 번째 달 진입 캠페인

상태: PARTIAL
단계: Phase 7 / 장기 플레이 캠페인
사이즈: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 시스템의 플레이어 목표 연결만. Unity 재실행은 별도 사람 판단 필요

목표:
- Day 30 저장→Day 31 진입 뒤 일반 반복 문구만 남던 구간을 기존 생활·상점·마을 변화 시스템으로 구성한 Day 31~45 캠페인으로 연결한다.
- 보관, Processed 판매, 채용 roster, 상품/카테고리 구성, Tier 1 대장간과 ToolSet, 활성 마을 변화, 준비 재고, 누적 매출을 실제 런타임 상태에서 판정한다.

Project P.A.와의 연결: 첫 달 완주가 끝이 아니라 더 넓은 지역 경제 운영으로 이어지며, 낮 준비→밤 판매→다음 날 마을 변화→장기 Tier 성장이라는 전체 게임 루프를 두 번째 달 전반까지 플레이 가능하게 연장한다.

수정 가능 파일: `Assets/Scripts/LongPlayProgressionController.cs`, `Assets/Scripts/UI/PlayableDayScenarioController.cs`, 게임 루프·작업·상태 문서.
수정 금지 파일: 메인 씬·프리팹·에셋, 저장 스키마, 경제/구매/제작/채용/Tier/마을 변화 권위, B06 Tier 2·B08 Tier 3 정책, 패키지, ProjectSettings.

구현 조건: Day 31~45에 정확히 15개 계획과 15개 실제 상태 목표를 둔다. 기존 Day 1~30, Day 7 자동 보급 종료, Day 30 완주 UI, 단조 매출 목표 수식, Tier 임계값을 보존한다. 목표는 플레이어 행동을 대신 수행하거나 새로운 퀘스트/저장 상태를 만들지 않는다.
완료 조건: Day 31~45 상단 목표와 체크리스트가 매일 하나의 실제 운영 상태를 읽고 완료/다음 행동을 표시한다. Day 45는 기존 매출 점검선과 당일 4상품 판매를 함께 요구하며 이후에는 기존 장기 운영 폴백으로 돌아간다.
검증 방법: Runtime/Editor 순차 C# 빌드, 계획 일차·상태 분기·매출 32,500G→53,500G·기존 Day 30/Tier/저장 경계 정적 계약. 사람 판단 전 Unity는 실행하지 않는다.
실패 시 처리: 같은 빌드 또는 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다.
실행 결과: Runtime 오류 0과 기존 CS8785 경고 1개. Editor 오류 0과 기존 CS8785/CS0414 경고 2개. Day 31~45 캠페인 계약 23/23 PASS.
남은 확인: 안전 Unity 재실행이 승인되면 Day 30 저장→Day 31, 대표 Day 35/40/45 상태 전환, 1920×1080 목표·체크리스트 가독성과 Day 46 폴백을 확인한다.
선행 작업: Task 114, Task 115, Task 120

## Task 122 - Day 46~76 Tier 2 성장 캠페인

상태: PARTIAL
단계: Phase 9 / 장기 플레이 성장
사이즈: M
예상 Codex 실행 횟수: 1
사람 승인 필요: NO — 기존 목표 곡선·Tier 권위의 플레이어 경로 연결만. Unity 재실행은 별도 사람 판단 필요

목표:
- Day 45 뒤 일반 장기 운영 폴백으로 돌아가던 Day 46~76을 기존 지역 경제 시스템의 반복 가능한 성장 캠페인으로 연결한다.
- 기존 매출 곡선이 정확히 100,000G에 도달하는 Day 76에서 `TierService`의 자동 Tier 2 승급을 플레이어 목표와 체크리스트로 확인한다.

Project P.A.와의 연결: 첫 달에 배운 보관·가공·채용·철제 가치사슬·상품 구성·마을 변화가 장기 성장 리듬으로 결합되고, 작은 Tier 1 상점이 Tier 2와 B06 주방 가치사슬을 여는 완결된 성장 구간이 된다.

수정 가능 파일: `Assets/Scripts/LongPlayProgressionController.cs`, `Assets/Scripts/UI/PlayableDayScenarioController.cs`, 게임 루프·작업·상태 문서.
수정 금지 파일: 메인 씬·프리팹·에셋, 저장 스키마, 경제/구매/제작/채용/Tier/마을 변화 권위, Tier 2 100,000G, B06 Tier 2·B08 Tier 3 정책, 패키지, ProjectSettings.

구현 조건: Day 46~75의 30일은 보관→가공→지원 인력+4상품 준비→3카테고리→활성 B07+ToolSet+Processed→활성 마을 변화+4상품→누적 매출의 7일 리듬을 사용한다. 보관 목표는 12→20개, 가공 판매는 2→4건으로 기존 기능 범위 안에서 상승한다. Day 76은 Tier 2 실제 상태만 완료로 인정한다.
완료 조건: Day 46~76 모든 날짜에 계획과 실제 상태 판정이 있고, Day 76 목표 매출은 기존 수식과 TierDefinition 모두 100,000G다. Day 77부터는 기존 폴백으로 돌아가며 후속 B06 캠페인을 별도 작업으로 연결할 수 있다.
검증 방법: Runtime/Editor 순차 C# 빌드, 31일 생성·7단계 분포·공유 임계값·55,000G→100,000G·Tier 2 자동 조건·B06/Day 1~45/저장 경계 정적 계약. 사람 판단 전 Unity는 실행하지 않는다.
실패 시 처리: 같은 빌드 또는 계약이 같은 이유로 2회 실패하면 세 번째 시도 없이 `BUG_LOG.md`에 기록하고 중단한다.
실행 결과: Runtime 오류 0과 기존 CS8785 경고 1개. Editor 오류 0과 기존 CS8785/CS0414 경고 2개. Day 46~76/Tier 2 캠페인 계약 34/34 PASS.
남은 확인: 안전 Unity 재실행이 승인되면 대표 Day 46/52/59/66/73, Day 76 매출→Tier 2 자동 승급, 1920×1080 목표·체크리스트 가독성과 Day 77 폴백을 확인한다.
선행 작업: Task 115, Task 121

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

## Task 123 - Day 77~90 Tier 2 주방 가치사슬 캠페인

상태: PARTIAL
단계: Phase 9 / 장기 플레이 성장
사이즈: M
사람 승인 필요: NO

목표:
- Day 76 Tier 2 돌파 뒤 기존 B06 Kitchen과 BreadLoaf·구운 감자·생선구이 세 레시피를 실제 플레이 캠페인으로 연결한다.

구현 조건:
- Day 77~90 계획과 실제 상태 체크리스트를 추가한다.
- B06 배치, 세 출력 상품의 정확한 당일 판매, 영업 전 3종 준비, Chef 고용, 3카테고리 조합, Processed 마을 변화를 기존 읽기 권위로 판정한다.
- Day 90은 활성 B06+세 주방 상품 판매+Processed 마을 방향으로 재료→주방→상점→마을 변화 루프를 닫는다.
- 새 레시피·아이템·퀘스트·보상·저장 필드·Tier/경제 수치·자동 플레이를 추가하지 않는다.

검증 방법:
- Runtime/Editor 순차 C# 빌드, Day 77~90 계획 14개·case 14개·목표/체크리스트 호출부 2개·B06/세 레시피/출력 Item·기존 Day 1~76/Tier/저장 경계 정적 계약. 사람 판단 전 Unity는 실행하지 않는다.

실행 결과:
- Runtime 오류 0과 기존 CS8785 경고 1개. Editor 오류 0과 기존 CS8785/CS0414 경고 2개.
- Task 124에서 호출부 2개 계약은 복구됐고 계획/case·UI 연결·Kitchen/Processed 핵심 계약 44개가 PASS했다.
- B06 최소 Tier 표현과 구운 감자·생선구이 Item 이름 표현 3개가 미확정으로 남아 전체 결과는 44/47이다.

남은 확인:
- 수정된 호출부 기대값 2로 정적 계약을 한 번 실행한다.
- 안전 Unity 재실행이 승인되면 B06 해금/배치, 세 제작·판매 경로, Chef, Processed 변화, Day 90 완료와 1920×1080 가독성을 확인한다.

선행 작업: Task 115, Task 122

## Task 124 - Task 123 주방 캠페인 정적 계약 복구

상태: DONE
단계: Phase 9 / 장기 플레이 검증
사이즈: XS
사람 승인 필요: NO

목표:
- Task 123의 잘못된 호출부 기대값을 바로잡고 Day 77~90 소스·리소스 계약을 확정한다.

실행 결과:
- 목표와 체크리스트 호출부 2개, 단일 판정 정의, Day 77~90 계획 14개/case 14개, Day 76 경계, B06 Kitchen 프리팹, 세 Kitchen 레시피, 세 Processed 출력과 요구 리소스 존재를 포함해 44개 계약 PASS.
- B06 최소 Tier C# 표현과 구운 감자·생선구이 Item 이름 YAML 표현 2개가 정규식과 일치하지 않아 44/47에서 중단.
- 코드·씬·프리팹·에셋 수정과 Unity 실행 없음.

남은 확인:
- Task 125에서 실제 C#/YAML 직렬화 행을 명시 경로로 확인해 세 항목 모두 검사 표현 불일치로 분류했다.
- 44개 자동 계약+3개 직접 권위 행으로 47/47 완료.

선행 작업: Task 123

## Task 125 - B06 Tier·출력 Item 이름 권위 행 감사

상태: DONE
단계: Phase 9 / 장기 플레이 검증
사이즈: XS
사람 승인 필요: NO

목표:
- Task 124에서 미확정된 B06 최소 Tier와 구운 감자·생선구이 Item 이름의 실제 C#/YAML 표현을 확인한다.

실행 결과:
- `ResolveMinimumTier`는 B06 case에서 정확히 `return 2`.
- `Item_09_BakedPotato.asset`의 Unicode escape는 `구운 감자`.
- `Item_10_GrilledFish.asset`의 Unicode escape는 `생선구이`.
- 실제 데이터 결함 없음. 검사식이 switch return과 Unity YAML Unicode escape를 허용하지 않은 것이 원인.
- 코드·씬·프리팹·에셋·Unity 상태 변경 없음.

완료 조건:
- Task 124 DONE, Task 123 정적 계약 47/47 확정.

선행 작업: Task 124

## Task 126 - Day 91~105 Tier 3 공동 공방 캠페인

상태: PARTIAL
단계: Phase 9 / 장기 플레이 구현
사이즈: M
사람 승인 필요: NO

목표:
- Day 90 뒤의 일반 폴백을 기존 주민 요청·평판·Tier 3·B08·Luxury 제작/판매·마을 변화로 연결한다.

실행 결과:
- 전문 주민 요청 완료를 기존 일일 활동 목록의 `tier3-reputation:{day}` 표식과 연결해 Tier 2 동안 하루 한 번 평판 +1을 지급한다.
- 평판 3은 기존 `TierService`의 자동 Tier 3 승급을 그대로 사용하며 Tier 데이터와 저장 스키마를 변경하지 않는다.
- Day 91~105 계획/판정 15개를 추가해 B08 배치, 의류/가구 판매·준비, 재단사, 3카테고리/4상품, Luxury 마을 방향과 최종 가치사슬을 실제 상태에서 읽는다.
- Runtime/Editor 순차 빌드 오류 0. 기존 CS8785/CS0414 경고만 유지. 정적 계약 24/24 PASS.

남은 확인:
- 사람 판단 뒤 안전한 Unity 경로에서 주민 요청 3일→Tier 3, B08 지급/배치, 의류·가구 제작/판매, Luxury 다음 날 변화, Day 105와 1920×1080 가독성을 확인한다.

보존:
- 씬·프리팹·에셋·저장 스키마·Tier 수치·레시피·아이템·경제/구매/제작/고용/마을 변화 권위·패키지·ProjectSettings 무변경.

선행 작업: Task 123, Task 125

## Task 127 - B05~B08 전문 주민 전면 접근 연결

상태: PARTIAL
단계: Phase 8 / GRID 기능성 에셋 정합
사이즈: M
사람 승인 필요: NO

목표:
- 기존 B05~B08 배치의 전면 interaction 셀을 전문 주민 이동·제작에 연결해 작업대 장애물 원점 이동을 제거한다.

구현 상태:
- `ShopCustomizationController`의 private 배치 권위를 유지하며 Workbench interaction 셀의 읽기 전용 월드 좌표 투영을 추가했다.
- `SpecialistNpcController`가 동일 타입 작업대의 예약되지 않은 셀 중 NavMesh 완전 경로를 선택한다.
- 셀 예약/해제, 도착 후 정면 보기, 이동·회수/일정 중단/비활성화 시 무효화, 저장 상태 복원 후 재접근을 연결했다.
- 씬·프리팹·FBX·재질·저장·레시피·아이템·경제 권위는 변경하지 않았다.

검증 결과:
- 이전 continuation의 잘못된 경로·문서 검사 실패는 실제 매트릭스 행과 정확한 경로로 복구해 `BUG_LOG.md`에서 RESOLVED 처리했다.
- Runtime/Editor 순차 빌드 오류 0. 기존 CS8785/CS0414 경고만 유지.
- 접근·예약 정적 계약 29/29와 대상 공백 검사 PASS.
- Unity 실제 주민 접근·정면·겹침 방지·제작은 반복 네이티브 충돌의 사람 판단 게이트로 확인 못 함.

선행 작업: Task 086, Task 103, Task 115, Task 126

## Task 128 - 관광객 손님 정상 플레이 진입

상태: PARTIAL
단계: Phase 9 / 밤 영업 고객 생태
사이즈: M
사람 승인 필요: NO

목표:
- `[관광객]` 표시 폴백에 머문 두 번째 손님 계층을 Day 2+ 실제 개점·도착·구매 흐름에 연결한다.

구현 상태:
- 기존 작성 고객 8명이 모두 유효한 마을 일과표를 가진 주민이며 정상 플레이 관광객 생성 경로가 0임을 직렬화·생성기·검증기에서 확인했다.
- 개점 뒤 영업당 최대 2명/동시 1명의 세션 한정 관광객을 기존 `CustomerArrivalController`가 생성한다.
- 관광객은 주민의 검증된 SkinnedMesh/Avatar 시각만 복제하고 런타임 `NpcProfile` 이름을 사용한다. 일과표·생산·전문가·대화/친밀도·채용·저장 기록은 만들지 않는다.
- 진입점은 가게 주변 NavMesh 완전 경로에서 고르고, 기존 `NpcController` FSM과 `PurchaseEvaluator`로 구매/거절한 뒤 같은 진입점으로 걸어 나가 제거된다.
- Day 1 시나리오 초대, 주민 Rest lease, 동시 손님 상한, 경제/구매/저장 권위는 보존했다.

검증 결과:
- Runtime 빌드 오류 0/기존 CS8785 경고 1개.
- Editor 빌드 오류 0/기존 CS8785·CS0414 경고 2개.
- 관광객 진입·분류·역할 비복제·비영속·퇴장·기존 주민 흐름 정적 계약 46/46과 대상 공백 검사 PASS.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트로 실행하지 않았다.

남은 확인:
- 안전 Unity 경로에서 Day 2+ 개점→관광객 입장→`[관광객]` 말풍선/성향→구매 또는 거절→진입점 퇴장을 확인한다.
- 주민/관광객 동시 손님 상한, Tier 0 외부·Tier 1 실내 주변 동선, 1920×1080 가독성을 확인한다.

선행 작업: Task 031, Task 105

## Task 129 - Day 106+ 본사 감사·Tier 4 최종 완주 경로

상태: PARTIAL
단계: Phase 10 / 최종 성장·완주
사이즈: M
사람 승인 필요: NO

목표:
- Day 105 뒤 일반 운영 폴백에서 끊긴 기존 본사 감사와 최종 Tier 4를 정상 플레이로 도달 가능하게 만들고, 전체 캠페인의 기능적 완주점을 제공한다.

구현 상태:
- 정상 플레이 `AddReputation` 호출이 `LongPlayProgressionController` 한 곳뿐이고 Tier 2 평판 3에서 중단되어, `AuditService`의 평판 5 조건이 도달 불가능함을 확인했다.
- Day 106+ Tier 3 상태에서 전문 주민 요청을 완료하면 기존 `tier3-reputation:{day}` 일일 저장 표식으로 감사 요구 평판까지만 하루 1점을 추가한다.
- Day 106+ 목표와 체크리스트는 `AuditService`의 실제 평판·고용·누적 매출 조건과 다음 정기 감사일을 순서대로 표시한다.
- 최종 승급은 계속 `AuditService → TierService.TryManualAdvance()`만 소유하며 LongPlay는 Tier를 강제하지 않는다.
- Tier 4 통과 후 첫 정산에서 기존 완주 UI를 재사용해 매출·보유금·Tier·평판·고용·마을 변화·당일 정산을 기록하고, 저장 후 다음 날 자유 운영 또는 저장 후 종료를 선택한다.
- 저장 스키마를 늘리지 않고 기존 `lastAuditDay`로 저장 후 계속한 완주 화면의 재표시를 억제한다.

검증 결과:
- Runtime 빌드 오류 0/기존 CS8785 경고 1개.
- Editor 빌드 오류 0/기존 CS8785·CS0414 경고 2개.
- 최종 감사 진입·조건 권위·평판 상한·일일 중복 방지·목표/체크리스트·Tier 비강제·완주 저장/계속/종료 계약 40개 자동 PASS.
- 결합 범위 검사 1개는 더티 워크트리의 기존 `SaveManager`·`SaveData`·패키지·Crop 프리팹 변경을 이번 작업으로 오인했다. 이번 구현 파일 2개에서 금지 mutator 0을 직접 확인해 40개 자동+1개 직접 권위로 정합화했고 재실행하지 않았다.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트로 실행하지 않았다.

남은 확인:
- 안전 Unity 경로에서 Day 106/107 전문 주민 요청→평판 4/5, 감사 앱 조건·다음 감사일, 정기 감사→Tier 4를 확인한다.
- Tier 4 첫 Settlement 완주 화면, 저장→다음 날 자유 운영→재실행, 저장 후 종료, 1920×1080 가독성을 확인한다.

선행 작업: Task 126

## Task 130 - 본사 감사 성공·실패 플레이어 피드백

상태: PARTIAL
단계: Phase 10 / 정산·성장 피드백
사이즈: S
사람 승인 필요: NO

목표:
- 콘솔 로그에만 남던 본사 감사 성공·실패와 미달 원인을 기존 P.A. Phone 감사 앱에서 실제 플레이어가 읽게 한다.

구현 상태:
- `AuditService`가 현재 누적 매출·평판·고용 수치, 조건별 완료 여부, 다음 감사일, 현재 세션의 최근 결과를 읽기 전용으로 제공한다.
- 감사가 실패·통과·최고 등급 확인·내부 승급 보류로 끝날 때 하나의 상태 이벤트를 발행하고, 앱이 열려 있으면 즉시 갱신한다.
- 감사 앱은 세 조건의 실제 현재값/요구값과 완료·부족, 다음 감사일, 최근 결과, 평판→채용→매출 순의 가장 가까운 행동을 표시한다.
- 수동 승인 Tier의 진행 바도 감사 3조건의 실제 부분 진행을 사용한다.
- 기존 폰 콘텐츠 높이 안에서 Tier/매출/시설/감사 카드 높이와 여백을 재배치했다.

검증 결과:
- Runtime 빌드 오류 0/기존 CS8785 경고 1개.
- Editor 빌드 오류 0/기존 CS8785·CS0414 경고 2개.
- 기준값·결과 분기·이벤트 구독/해제·UI 읽기 전용·고정 높이·메인 씬 비침범 정적 계약 48/48 PASS.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트로 실행하지 않았다.

남은 확인:
- 안전 Unity 경로에서 조건 미달 정기 감사→앱의 실패/다음 행동, 조건 충족 감사→Tier 4 해금 문구를 확인한다.
- 앱을 열린 채 날짜가 바뀔 때 즉시 갱신되는지와 1920×1080에서 Tier/시설/감사 카드가 잘리거나 겹치지 않는지 확인한다.

보존:
- 500,000G·평판 5·고용 3명·7일 주기, `AuditService → TryManualAdvance()` 단독 권위, `LastAuditDay` 저장, 씬·프리팹·저장 스키마·경제/채용/Tier·패키지·ProjectSettings 무변경.

선행 작업: Task 129

## Task 131 - Tripo 장기 정책 재확인·B06 Kitchen 실제 보정

상태: PARTIAL
단계: Phase 11 / 기능성 에셋 최종화
사이즈: S
사람 승인 필요: NO

목표:
- 첨부 GRID 기반 커스터마이징과 Tripo 임시 에셋 정책을 기존 구현에 중복 없이 장기 지침으로 유지하고, 가장 자주 노출되는 미완성 기능성 에셋 하나를 실제 개선한다.

구현 상태:
- 기존 `GridService` 2m zone, 상점 실내/마을 야외 P1~P5, v10 placeable 저장, footprint/clearance/interaction, NPC 예약 접근이 이미 구현됨을 확인했다.
- FBX 174/OBJ 150/GLB 0/Blend 0과 non-Nature 고유 FBX 24개를 다시 확인하고 `TRIPO_ASSET_AUDIT.md`의 1~8 개별 판정·캐릭터 정체성 보존·원본 비파괴·출처 게이트를 최신화했다.
- B06 Kitchen은 원본 Visual을 유지하고 렌더러 bounds보다 0.2m 이상 큰 X/Z 물리 축만 0.16m 여유로 줄인다. box형 Carving도 같은 center/size를 사용하고 어떤 축도 확대하지 않는다.
- 로컬 `-Z` 물리 앞에 `PA_KitchenInteractionAnchor`를 만들고 기존 제작 성공 뒤에만 B06 모델 자체가 0.72초/최대 3.5% pulse한다.
- B05 기능 아트, B06 Tier 2/2×2/Kitchen 레시피, NPC 접근, 저장, FBX·프리팹·씬·재질·BuildingData는 보존했다.

검증 결과:
- Runtime 오류 0/기존 CS8785 경고 1개.
- Editor 오류 0/기존 CS8785·CS0414 경고 2개.
- B05 회귀 보존, B06 기존 Visual 재사용, 축소 전용 Box/Obstacle, 전면 anchor, 성공 후 pulse, Tier 2/2×2, 8분류/출처 정책 계약 30/30 PASS.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트로 실행하지 않았다.

남은 확인:
- 안전 Unity 경로에서 B06을 2×2로 배치하고 플레이어·Chef의 전면 접근, 모델과 물리 경계, BreadLoaf 제작 pulse를 확인한다.
- 같은 GameCamera Before/After에서 스케일·통로·그림자·UI 겹침이 개선됐는지 확인한다.
- 개별 Tripo 생성 계정·생성일·상업 이용 증빙을 최종 배포 전 확보한다.

선행 작업: Task 087, Task 103, Task 123, Task 127
