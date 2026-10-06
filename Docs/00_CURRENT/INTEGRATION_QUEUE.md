# Integration queue

**2026-10-06 현재:** `demoProductionRouting`은 `demoPolishFinishApproval`/D2/Codex. D0/D1 PASS; D2 TooltipBinding 적용 및 섬31/도입10 검사 오류0. 툴팁 E 조작 표시·대화/안내 겹침·NPC 말풍선 검수 누락으로 INCOMPLETE. 추가 포함 compile4/4·Play5/5·validator1/1 소진: 예산 재승인 전 수정/Play 중지. 이후 D3 아이콘2005/2012/2013 구분·2010 교체를 D3 예산에서 수행(아이템 데이터 불변). D5/D7 Astra XHigh, 나머지 High. 사용자가 현재 D2 정지 지점까지 1회성 commit/push를 승인했으며 이후 자동 commit/push는 다시 금지한다.

**최신 실행 우선순위 — 2026-10-05 Codex 인수:** 기존 approval/P1~P11/Q1~Q6를 유지한다. 현재 P11 한정 SFX 음량 수정은 실제 믹스·Windows 출력으로 PASS7. P4 안내문(3compile/2Play), P6 가방/수납 QA(3compile/3Play) 추가1+1 요청은 답변 대기이고 P9 그립(3/3)은 화면 FAIL로 보존한다. 같은 blocker/예산을 새 이름으로 초기화하지 않는다. 독립적인 지지발/자연 NPC/남은 전환을 검토하고, 필요한 제품 수정이 정해진 뒤 최신 동일 Windows 후보의 자연 NEWGAME→Report·격리 저장→정상 종료→새 프로세스 Continue·실제 성능/소리를 검증한다. 최종 사람 검수와 no commit/push 유지. [checkpoint](CODEX_HANDOFF.md).

**현재 실행 선택 (`demoProductionRouting`):** 2026-10-03 사용자가 [실행 계약](../../AI_WORKFLOW/03_TASKS/CLAUDE_OPENING_DEMO_EXECUTION_PROMPT.md)의 승인 메시지를 Claude에 직접 보내 `demoFeedbackFinishApproval`이 선택됐다(Unity 담당 Claude, preapprovedThrough P11_WINDOWS_DELIVERY, stop FINAL_WINDOWS_HUMAN_REVIEW, commit/push 없음). 순서: 현재 이어하기 → 전화/D4 → 직접 채집 → 기본 낚시 → 곤충 → 제작/배치/수납 → 동료/성장 → 영업/Report → 새 모션 재작업 → 섬/첫날 → Windows 최종 사람 검수. 기존 `demoCompletionApproval`은 D4_GAMEVIEW_REVIEW 정지·결과를 보존하며 D4와 옛 모션 결과는 ACCEPTED가 아니다. 저장/phone은 계약의 한정 범위만, v16 변환·원본 save·schema 변경은 금지. 오래된 CONTENT/WORLD/BETA/T7 ACTIVE로 자동 전환하지 않는다. [Handoff](CODEX_HANDOFF.md).

**이어하기 별도 부채:** `demoCompletionApproval.v16Decision`은 현재 형식의 배치만 복원하고 v16은 지원 불가 안내하며 원본을 보존하는 결정이다. v16 새 배치 재구성/변환은 실행하지 않는다. 두 이어하기의 기존 FAIL과 현재 형식 복원 검증 부채는 유지한다. [근거](../../Logs/VisualQA/TitleScreen-20260929/CONTINUE_COMPATIBILITY_DECISION.md). 아래 모션 재작업은 사용자 품질 판정 대기로 유지한다.

**최신 2026-09-29 TASK 01-C 모션 품질 FAIL → 샘플 제작 후 중단(시각 품질 판정 대기).** 다음 우선순위는 사용자가 대기→달리기→정지 샘플 비교 영상을 확인하는 것이다. 그 전까지 추가 튜닝·에셋 도입·제품 적용은 하지 않는다. 승인되면 샘플 클립을 제품 컨트롤러에 적용(보폭 속도 4.22/6.50)하고, 맨손 점프 레퍼런스를 추가 확보해 점프·착지를 같은 스타일로 맞춘다. 접지·상태 전이 검증은 유지한다. [샘플](../../Logs/VisualQA/Task01C-Locomotion/StyleRework-20260929/EVIDENCE.md).

**2026-09-29 TASK 01-C 구현 완료 / 검수 PARTIAL(스폰 접지·점프/낙하 전이 마무리 포함).** 사람이 실제 키보드로 16m 로코모션·점프·착지를 확인하는 항목(스폰 대기 발 위치, 점프 체공 자세 전환, 착지 깊이)은 재작업 적용 후 다시 본다. 이후 별도 승인 작업으로 손 장착(도구 그립 방향·상체 레이어)·도구 사용 클립을 진행하며, UAL2의 TreeChopping 등은 그때 검토한다. TASK 01-C와 9/29 마무리 코드는 기존 Windows 후보에 포함되지 않는다. [증거](../../Logs/VisualQA/Task01C-Locomotion/EVIDENCE.md), [마무리](../../Logs/VisualQA/Task01C-Locomotion/Closeout-20260929/EVIDENCE.md).

**최신 2026-09-28 TASK 01-B 조사 완료.** 다음 후보는 TASK 01-C 플레이어 Animator 로코모션 전환(PlayerController 권위 유지, 휴면 Speed 연결 사용, Idle/Walk/Run 블렌드 + Jump/Fall/Land, 착지·부유 보정 최소화)이다. 착수 전 Run·Jump·Fall·Land 클립 출처 승인이 필요하다(로컬에 없음). [조사](../../Logs/VisualQA/Task01B-Locomotion/FINDINGS.md).

**최신 2026-09-28: TASK 01 맨손 이동·애니메이션 완료.** 다음 우선순위는 사람의 16m 실제 키보드 플레이 체감 확인(걷기/달리기 리듬, 카메라 쪽 달리기 팔 벌어짐, 점프·착지)이다. 손 장착·도구 사용·NPC·UI는 별도 승인 작업으로 남는다. [증거](../../Logs/VisualQA/Task01-Locomotion/EVIDENCE.md). 사용자는 9/27 후보 Windows 빌드로 엔딩까지 직접 플레이했다고 보고했다. 아래 standalone 미검증 문구는 에이전트 자동 입력 기준이다.

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
