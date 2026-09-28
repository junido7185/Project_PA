# 작업별 최소 컨텍스트

먼저 [Docs 입구](../README.md)의 `00_CURRENT` 네 문서를 읽는다. 설계·구현·검증·이력은 해당 기능에 필요한 부분만 추가한다. 과거 snapshot은 시작 지시가 아니다.

| 작업 | 추가 확인 | 검증 선택 |
|---|---|---|
| Gameplay / NPC / Economy | Capability의 코드·authority, 관련 Canon 및 시스템 계약 | 변경한 경로의 기존 validator 우선; 판매·반응·수익의 연결 확인 |
| UI / Opening | 최근 실제 캡처와 관련 presentation controller | 대상 UI·route 검사; 가독성·조작감은 사람 확인 |
| Save / Load | SaveManager와 해당 데이터의 migration 계약, 마지막 실패 | 기존 round-trip 검증. 사용자 save를 쓰지 않음 |
| World / Placement | World architecture와 placement 계약, 승인된 씬 | 기존 placement/storage regression. Golden Scene 실험 금지 |
| Art | Style Grammar, selected-assets/spec, provenance | 기존 Blender reference render·Unity wrapper 검사 |
| Build / Crash | 루트 최신 crash report, 기존 프로세스 로그 | 원인 분류 후 compile/load. 열린 Editor에 batchmode 중복 실행 금지 |
| 문서 | 현재 권위·원본 기록·코드의 문서 참조 | 링크·이력 보존·diff. tooling 경로 변경 시 compile |

Validator method는 [기존 registry](../../Automation/LoopEngineering/validator-registry.json) 및 해당 Editor 코드에서 확인한다. 모든 검증기를 매번 실행하지 않는다. 관련 targeted 검증 후 필요한 통합 검증만 수행한다.

규칙: [ONE_PAGE_WORKFLOW](../../AI_WORKFLOW/00_START_HERE/ONE_PAGE_WORKFLOW.md), [VERIFICATION_RULES](../../AI_WORKFLOW/04_VERIFICATION/VERIFICATION_RULES.md). 결과는 [월별 이력](../04_DEVELOPMENT_LOG/README.md)에 한 번 남긴다.
