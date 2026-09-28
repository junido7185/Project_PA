# Integration queue

**최신 승인 범위 2026-09-27: Report 중복 제목 보정 + 별도 Windows 후보 — 빌드 완료, standalone 입력 경계.** 새 `Candidate-ShopPolish-20260927-194240`에서 NEW GAME→출항 STEP1 확인. 다음 우선순위는 물리 WASD/Space부터 정착→실제 판매→CLOSE→Report 사람 플레이 확인이다. 이동 확인 실패 이후 동일 자동 입력 반복 금지. 이전 후보/dirty/index/save 유지. [실행 경로·조작순서·알려진 문제](../../Logs/VisualQA/ReportCandidate-20260927/DELIVERY_REPORT.md). 아래 기록은 이전 검증 범위이며 새 후보의 전체 PASS가 아니다.

**Priority 2026-09-27: 현재 후보 보존 + T7 납품 정리만 — 완료.** 최신 사용자 지시가 이전 연속 구현 승인을 제한한다. 경고 원인/데모 영향, 후보 소스·dirty 식별과 증거 경로 정리 완료. [납품 보고서](../../Logs/VisualQA/Continuation-20260926/T7/Delivery-20260927/DELIVERY_REPORT.md). 전체 standalone MUST PATH는 미검증이며 다음 검수는 사람의 후보 플레이다. 같은 Windows 포커스·입력 자동화 및 근거 없는 T2~T6 재검증 금지. Canon/authority/dirty/index 유지, commit/push 없음.

아래는 기존 사용자 확정 작업 정의이며 새 구현 착수 지시가 아니다.

| 티켓 | 확정 범위와 완료 기준 |
|---|---|
| T2 마무리 | footprint/출입 유지; 과한 외관·임시 문·상점/생활/작업 가구 개선. 정상 카메라에서 용도/입구 식별, 이동/고객/가판대 공간 확보 |
| T3 | 손 아이템 중첩·도구/대상·타격→드롭→줍기→수량 피드백. 실제 사용/획득 식별, 보상·내구도·지급 중복 없음 |
| T4 | 기존 HUD/Inventory/Smartphone의 PHONE/Hotbar 잘림·아이콘·선택/간격/대비.1080p 잘림 없음, 열기/닫기/입력 복귀. P 시스템 UI, 상시 퀘스트 목록 금지 |
| T5 마무리 | 기존 낮밤 연결 유지, 밤 캐릭터/상품/가격과 따뜻한 실내 조명 가독성. 낮/밤 대표 영업 화면 |
| T6 | 실제 진열→가격→NPC 반응/구매→CLOSE→Report. 재고/금액/기록/피드백 일치, 첫 판매 연출 중복 없음. 자율 구매와 API 검증 분리 |
| T7 | 기존 Windows 빌드 경로로 후보 생성, 가능한 전체 MUST PATH. 빌드 위치/실행/문제/미검증 기록; 실제 측정 성능만 보고 |

2026-09-27 진행: T2~T6 관련 구현·GameView·기능 검증 완료. T6는 실제 자율 관광객 구매와 Report 금액 일치까지 확인. T7 후보 빌드 성공·독립 타이틀 실행/정상 종료 확인. 남은 standalone MUST PATH는 Windows 창 포커스/입력 차단으로 BLOCKED. 테스트 후보와 정확한 재개 순서는 handoff/T7 README 참조. 전체 주관적 품질 HUMAN-UNVERIFIED.

막힌 티켓은 근거 후 독립 작업 계속. HUMAN-UNVERIFIED만으로 중단하지 않음. [Handoff](CODEX_HANDOFF.md).

이전 우선순위 (2026-09-21): 제품 NEW GAME → PA_DepartureTutorial 진입은 코드/Build reference/compile 기준 CONNECTED다. 갱신된 실제 제품 진입 validator의 targeted Play와 인간 연속 완주·시각 확인이 남았다. broad Final Polish는 별도 지시 전 시작하지 않는다. Full Agriculture/트리/Day 2/새 Save 권위로 확장하지 않는다.

현재 우선순위의 단일 권위. 기준일 2026-09-10. 완료 티켓은 [개발 이력](../04_DEVELOPMENT_LOG/README.md)으로 보내고 이곳에 누적하지 않는다. **아래는 후속 판단 순서이며 구현 착수 승인이 아니다.**

| 순서 | 상태 | 다음 integration 판단 | 기존 구현과 완료 판단 |
|---|---|---|---|
| 1 | 사람 확인 대기 | OPENING-FEEL의 발 접지·회전·카메라 가림·P3 UI 체감 | `4374b2a` 실제 경로와 캡처를 사용. 같은 PASS를 무의미하게 반복하지 않음 |
| 2 | HOLD — 별도 지시 필요 | P4-R4 저장 계약·dirty v16 빈 생산 상태·전체 P4 실행 검증 부채 | 현재 바인딩과 지원 수령 정책을 재사용. 바인딩 계약 검사 PASS는 전체 P4 저장/플레이 PASS가 아님. P2/P3 bootstrap의 임시 Ensure/Add 제거는 후속 이전 범위. 자동 착수 없음 |
| 3 | 기존 검증 부채 | WorldSandbox M85/CONTENT 장기 경로의 저장·재진입·방향 복원 및 실제 완주 | 이전 실패 로그부터 확인. 출항 Save PASS와 별도 판정; 동일 원인 무분석 재실행 금지 |

현재 문서 정리가 끝나도 P4·CONTENT를 자동 시작하지 않는다. `Automation/LoopEngineering/State/loop-state.json`의 기존 선승인은 보존하되, 마지막 사용자 범위와 실제 repo 상태를 먼저 대조한다.

설계와 장기 후보는 [Content backlog](../02_IMPLEMENTATION/Content/CONTENT_IMPLEMENTATION_BACKLOG.md), [Full-game backlog](../02_IMPLEMENTATION/Backlog/PROJECT_PA_FULL_GAME_BACKLOG.md), [World backlog](../02_IMPLEMENTATION/Architecture/WORLD_BOUNDED_BACKLOG.md)에 있다. 이들은 상세 계약/과거 실행 순서이며 이 큐와 경쟁하는 현재 티켓 목록이 아니다.

Next human review for direct WorldSandbox gathering: hit feel, tool-selection clarity, resource appearance/grounding, Fishing BITE prompt and Net/Butterfly readability. Automated gameplay acceptance is recorded in the September log. Bug population persistence remains outside the session-local demo scope; no further ticket is auto-authorized.

Human review for direct Hub/Shop placement: preview quality, valid/invalid colors, placement animation and final building alignment. Automated PLACEABLE-01 acceptance is in the September log. Await the next explicit ticket; no automatic expansion into stocking/customer sales.

Human review for placed B01 operation: direct stocking/price UI readability, relocated OPEN sign, customer motion/reaction polish and display alignment. SHOP-LOOP-01 automated result is recorded in the September log. Await the next explicitly authorized ticket.

Next demo review: INTEGRATION-01 human transition/route/UI/placement/customer visuals. DEMO_SUCCESS is available for an explicitly authorized CUTSCENE ticket; no cinematic work is auto-authorized. Default Golden title NEW GAME connection, standalone build and save-persistent completion remain separate decisions.
