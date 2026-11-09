# Codex 병행 연속 개발 — Day 1 데모 UI의 실제 코드

작성일: 2026-10-05. 이 지시서는 Codex의 연속 개발 범위를 정의한다. 사용자의 실제 실행 메시지 뒤에만 실행하며, Claude의 Unity 소유권과 기존 P1~P11 실행 상태는 유지한다.

## Codex에 보낼 실행 메시지

```text
Project P.A.의 병행 게임 코드 개발을 연속 진행하도록 승인한다.
AI_WORKFLOW/03_TASKS/CODEX_PARALLEL_DEMO_CONTINUATION_PROMPT.md를 적용해줘.
제작 UI → 수납 UI → 동행 선택 화면 → 진열/가격 UI 순서로,
계획에서 끝내지 말고 실제 C# 개선본·패치·필요한 검증까지 만들어.
작업마다 다음 승인을 묻지 말고 승인된 다음 작업으로 계속 넘어가.
Claude는 자동 재개와 live Unity/게임/빌드 담당을 유지한다.
Codex 수정은 Automation/CodexParallel/**, Tools/CodexParallel/**,
Logs/CodexParallel/** 안의 이번 작업별 전용 폴더에만 승인한다.
실제 Assets와 공유 상태·문서·Handoff·관찰 로그는 읽기 전용이다.
이번 작업의 진행·재개 기록은 전용 Logs에 남기며 공통 기록 규칙보다 우선한다.
승인 범위 완료, 실제 HARD BLOCKER, 사용자 중단, 사용량/환경 제한 외에는 계속 진행해.
제한에 대비해 안전한 코드 저장 지점마다 짧은 재개 기록을 남겨줘.
사용량을 소비하려고 무관 작업이나 중복 검증을 추가하지 마.
```

권장 설정은 선택 메뉴에 제공되는 GPT-6.1 Sol / High다. 실행 프롬프트로 실제 모델 설정이 바뀌었다고 주장하지 않는다. 사용자 설정을 임의 변경하지 않는다. `/goal`이 제공되는 Codex에서는 위 실행 메시지를 goal의 목표로 전달해 진행할 수 있고, 제공되지 않으면 일반 실행 메시지로 진행한다. goal의 완료 기준도 C1~C4 staged 코드 산출물이며 live 게임 전체 완료가 아니다.

## 공통 운영 계약

기존 `CODEX_PARALLEL_P6_CRAFTING_PROMPT.md`의 격리·기존 권위·baseline hash·실제 코드/patch·부분 compile·검증 한계를 재사용한다. 이 연속 승인에서 바꾸는 것은 **C1 뒤 중단 대신 C2~C4 자동 진행**, 전용 경로 및 재개 기록뿐이다. 첫 진입의 필수 문서와 기존 계약은 한 번 읽고 이후에는 관련 변화/심볼만 확인한다.

사용자가 인계 메시지를 실제로 보내고 Claude를 중단한 경우 외에는 `unityEditorOwner`를 바꾸지 않는다. 게임의 activeTicket을 C1~C4로 변경하거나 두 번째 게임 실행 큐를 만들지 않는다. 이 문서와 전용 CONTINUE.md는 staged 개발/납품 기록이며 live 티켓 승인·상태를 대신하지 않는다.

Codex는 live Assets를 수정하지 않고, 전용 Source 안에 기존 클래스의 개선본과 해당 원본을 대상으로 한 diff를 작성한다. live Unity 호출·import/compile·Play·씬/프리팹 편집·후보 실행·패키지 설치·삭제·Git stage/commit/push를 하지 않는다. 기존 component/API/serialized field/데이터 ID를 보존한다. 새로운 Player/Inventory/Shop/Storage/Save/성장/입력 권위를 만들지 않는다.

이미 P6CraftingUI 작업을 진행했다면 그 코드와 기록부터 이어받고 폴더를 새로 만들며 처음부터 재구현하지 않는다. 다른 사람의 staged 파일도 덮어쓰지 않는다. 모든 시작 기준은 실제 live 원본과 관련 현재 diff이며, snapshot 시각과 SHA-256을 기록한다.

## 승인된 순서와 작업별 한계

### C1 — P6 작업대 제작 화면

원본: `Assets/Scripts/CraftingUI.cs`.
전용 폴더: 기존 `P6CraftingUI`.
기존 한정 프롬프트 그대로: E 안내, 실제 Root/Tier/교류 잠금 이유, 재료/결과/가능 상태, 표시 계층 한국어, 읽기 쉬운 카드와 제작 피드백. 기존 CraftingService 호출·도감 원격 제작 차단·커서/닫기 계약 유지.

### C2 — P6 실제 수납 화면

원본: `Assets/Scripts/StorageUI.cs`.
전용 폴더: `P6StorageUI`.
기존 StorageBox/Inventory/선택 핫바 거래 코드는 유지하고, 상자 내용·빈칸/용량·수량·선택 물품 보관·꺼내기·가득참/부분 이동 결과를 쉽게 읽도록 UI 표시를 개선한다. 상품명 한국어는 표시 계층에서만 처리한다. 품질·책정 가격을 보존하는 현재 동작과 구분되는 설명을 제공한다. 실제 기본 보관이 1개씩이면 그 사실을 정확히 표시하고 새 수량 전송/정렬/수납 규칙을 추가하지 않는다. 입력·다른 UI 전환·커서 복원을 유지한다.

OnClickTakeItem/OnClickStoreItem의 수량 이동과 재고 보존을 리팩터하거나 새 저장 경로를 만들지 않는다. 실제 거래 결함이 발견되면 근거와 영향만 기록하고 UI 수정을 통해 숨기지 않는다.

### C3 — P7 동행 선택의 이유가 보이는 화면

원본: `Assets/Scripts/Presentation/DepartureCompanionSelection.cs`.
전용 폴더: `P7CompanionSelection`.
기존 후보3/동행2, Candidate/NpcProfile/로컬 모델과 선택·확정·출항 계약을 유지한다. 현재 제공되는 직업·생산·첫 상품·특기 데이터를 읽어 카드의 위계·선택 상태·확정 버튼과 설명을 개선한다. TEMP/디버그 중심 설명은 플레이어에게 필요한 정보로 정리한다. 데이터가 없는 전문 분야 효과·직업·능력·추천 이유를 지어내지 않는다. 기존 모델과 배치를 재사용하고 새 모델/애니메이션을 만들어 넣지 않는다.

동료 생산·경로 탐색·AI schedule·도구 지급·선택 저장·시나리오 gate는 수정하지 않는다. 표시 개선을 실제 동료 작업이 구현됐다는 증거로 취급하지 않는다.

### C4 — P8 진열과 가격 입력 화면

원본: `Assets/Scripts/UI/ShopPriceUI.cs`.
전용 폴더: `P8ShopPriceUI`.
현재 상품/수량/가격·적용 전 임시 값·확정/취소와 진열/가격 모드가 읽히도록 기존 UI를 개선한다. 데모 상품 표시 이름과 여백·대비·버튼 피드백을 정리한다. 이미 작동하는 ±/slider/drag·TutorialPriceDragged·OnPriceConfirmed·커서/닫기 계약을 보존한다. 제품 입력을 실제 검증한 것으로 주장하지 않는다.

ShopSlot/PurchaseEvaluator/EconomyService, 가격식·매출·거래·손님·튜토리얼 인증·OPEN/CLOSE는 바꾸지 않는다. 임의 적정가·수익 예측·자동 가격·새 세금/평가 규칙을 추가하지 않는다. 적용/취소 event를 중복 발생시키지 않도록 기존 호출 경로를 보존한다.

## 경험마다 필요한 산출물

각 경험은 기존 source의 개선본, 원본 대비 patch, baseline hash와 변경 설명, 가능한 부분 compile/필요한 작은 검사 결과, Claude의 적용/실제 Play 검수 순서를 한 묶음으로 끝낸다. 기준 사본과 출력은 해당 Automation/Tools/Logs 전용 폴더에만 둔다.

기존 UI에 이미 요구가 충족돼 있으면 이를 보존하고 확인된 차이만 구현한다. 새로 만들 것이 없으면 근거로 해당 항목을 재사용 처리하고 다음 경험으로 넘어간다. 결과물을 늘리려고 UI 전체를 새로 쓰거나 구현을 그대로 따라 쓰는 테스트/모의 GameView를 만들지 않는다.

부분 compile은 전용 격리 프로젝트와 설치된 compiler/캐시 assembly만 사용하며 출력/cache/임시 파일 경로도 전용 폴더로 지정한다. compile 불가가 단순 환경 제한이고 실제 코드 작업을 할 수 있으면 한 번 기록해 UNVERIFIED로 둔다. 이 경우 다음 독립 경험을 진행할 수 있지만, 코드 자체 compile 실패·authority/API 전제 오류·동시 수정 충돌 등 실제 HARD BLOCKER는 해결 예산을 적용한 뒤 멈춘다. 같은 원인을 반복 시도하지 않는다.

## 계속 진행과 재개

사용량 소진을 개발 목표로 만들지 않는다. 목표는 C1~C4 실제 코드 산출물의 완성이다. 승인된 다음 경험이 남아 있으면 한 경험 완료 보고로 턴을 끝내지 않고 다음을 시작한다. 제한 때문에 멈췄다가 사용자가 재개시키면 정확한 다음 행동부터 이어간다. 계정 제한을 우회하거나 AI를 자동 재호출하는 실행기를 만들지 않는다.

`Logs/CodexParallel/DemoUI/CONTINUE.md` 하나에 현재 경험·코드 저장 지점·완료한 조사/검사·미해결·정확한 다음 행동·각 결과 HANDOFF 경로만 짧게 유지한다. 경험 완료 때와 큰 구현 묶음을 저장한 안전한 중간 지점에 갱신해 예고 없는 사용량 제한에 대비한다. 파일 하나마다 긴 기록을 반복하지 않는다. 읽을 수 없는 계정 잔량이나 초기화 시각을 추측하지 않는다.

C1~C4 완료 또는 HARD BLOCKER/사용자 중단/실제 제한이 중단 조건이다. 모두 끝나면 남은 사용량을 소비하려고 무관 기능·반복 테스트를 추가하지 않는다. 실제 제품 반영·Unity compile·Play·GameView·human은 Claude의 해당 P 티켓에서 검증하고, 이 staged 완료로 P6~P8 전체를 PASS 처리하지 않는다.

## Claude에 보낼 분담 지시

```text
자동 재개와 기존 P1~P11 순서는 유지해줘.
Codex가 Assets 밖에서 아래 UI 코드 개선본을 순서대로 개발한다.
CraftingUI.cs / StorageUI.cs / DepartureCompanionSelection.cs / ShopPriceUI.cs.
동일 UI를 중복 구현하기 전에 Logs/CodexParallel/DemoUI/CONTINUE.md와
거기에 명시된 각 HANDOFF를 확인해줘.
해당 P6/P7/P8 티켓에서 원본 hash/diff를 대조해 준비된 개선본을 적용하고,
기존 검증 흐름으로 실제 입력·GameView와 관련 회귀를 함께 확인해줘.
다른 gameplay/배치/생산/영업/모션/섬/빌드 작업은 계속 맡아줘.
아직 준비되지 않은 UI는 독립된 다른 작업부터 진행하고 두 작업자가 동시에 다시 만들지 않게 해줘.
```

위 분담이 현재 Claude 세션에 실제로 전달됐는지는 별도로 확인한다. Codex의 전용 경로는 live 파일 충돌과 자동 compile 간섭을 피하지만, 예정 순서 예측만으로 중복 제작 방지를 보장하지 않는다.

공식 설정 근거: 시간·비용도 고려하는 복잡한 작업에는 GPT-6.1 Sol을 검토할 수 있으며, high는 복잡한 agentic 작업, xhigh는 추가 지연/비용에 대한 이득이 확인된 작업에 적합하다. 이 작업에서 Sol/High를 시작 설정으로 선택한 것은 프로젝트 범위에 따른 추천이다. [모델 선택](https://learn.chatgpt.com/docs/model-selection), [추론 수준](https://developers.openai.com/api/docs/guides/reasoning)
