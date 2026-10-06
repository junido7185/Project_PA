# Codex 병행 개발 — P6 작업대 제작 화면

연속 작업 선택: 사용량이 허용하는 동안 승인된 다음 UI 개발까지 이어가려면 `CODEX_PARALLEL_DEMO_CONTINUATION_PROMPT.md`를 사용한다. 그 계약에서는 이 파일의 C1 이후 중단 조건을 대신해 수납/동행 선택/진열·가격 UI를 순서대로 진행한다.

작성일: 2026-10-05. Claude는 새벽 3시 자동 재개와 기존 P1~P11 제작을 유지한다. Codex는 P6의 제작 UI 코드를 프로젝트의 Assets 밖에서 미리 구현한다. 사용자가 아래 메시지를 실제로 전달했을 때만 실행하며, 파일을 읽었다는 이유로 기존 승인/담당을 바꾸지 않는다.

## Codex 새 세션에 보낼 실행 메시지

```text
Project P.A.에서 CODEX-P6-CRAFTING-STAGED-001을 개발해줘.
AI_WORKFLOW/03_TASKS/CODEX_PARALLEL_P6_CRAFTING_PROMPT.md를 적용해.
Claude가 새벽 3시에 재개하므로 실제 Assets와 공유 Unity는 Claude 담당으로 유지한다.
Codex는 Assets 밖의 전용 폴더에 현재 CraftingUI.cs를 기준으로
작업대 제작 UI의 실제 C# 개선본과 적용 패치를 구현하고 필요한 검증까지 해줘.
수정 범위는 Automation/CodexParallel/P6CraftingUI/**,
Tools/CodexParallel/P6CraftingUI/**, Logs/CodexParallel/P6CraftingUI/**만 승인한다.
이 작업의 기록·재개 정보도 전용 Logs에만 남긴다.
이 한정 기록 경로는 공유 Handoff/월별 로그/상태/관찰 로그 갱신 규칙보다 우선한다.
현재 티켓과 Unity 담당을 바꾸거나 실제 Assets에 패치를 적용하지 마.
계획/감사 보고서에서 끝내지 말고 Claude가 P6에서 적용할 게임 코드까지 완성해줘.
```

## Claude에 전달할 범위 지시

```text
새벽 3시 재개 후 기존 P3 마무리 → P4 낚시 → P5 곤충 순서대로 계속해줘.
Codex가 P6의 Assets/Scripts/CraftingUI.cs 개선본을 Assets 밖에서 병행 개발한다.
해당 파일의 직접 수정은 Codex 결과 확인 전까지 보류하고 다른 P6 작업은 기존 범위대로 진행해.
P6에 도달하면 Logs/CodexParallel/P6CraftingUI/의 최신 인계 파일을 확인해줘.
Codex 결과가 준비됐다면 명시된 baseline hash/diff를 대조한 뒤 기존 CraftingUI에 적용하고,
실제 작업대 입력·제작·닫기/이동과 GameView를 기존 P6 검증 흐름에서 함께 확인해.
원본이 달라졌거나 결과가 아직 없으면 필요한 부분을 대조하거나 P6의 독립된 다른 작업부터 진행하고,
같은 UI를 동시에 다시 구현하지 마. Codex의 준비 결과로 P6 전체 PASS를 기록하지 마.
```

이 범위 지시는 자동 재개할 동일 Claude 세션에 실제로 전달한다. 전달되지 않아도 Codex의 작업은 Assets 밖에 머물러 live 파일/Editor를 건드리지 않지만, 중복 UI 제작을 줄이려면 Claude가 이 분담을 알아야 한다. 메시지 대기열이 받아들여졌다고 확인되지 않으면 전달 완료라고 주장하지 않는다.

## 분담과 읽기

작성 시 기존 activeTicket은 P3_DIRECT_GATHER였다. 최신 `Logs/VisualQA/20261004-233752-P3_DIRECT_GATHER/result.txt`는 33/33 PASS, Console 오류 0이다. 티켓 종료 문서화/시각 검수는 별개이며 실제 재개 순서는 Claude의 최신 상태를 따른다. P3 정리 → P4 → P5는 현재 계약에 따른 예상이고, P6의 시작 시각이나 소요 시간을 보장하지 않는다.

AGENTS와 필수 현재 문서 4개를 지정 순서대로 읽고 Handoff/loop-state/관련 diff를 대조한다. 기존 실행 계약의 P6 및 사용량·검증 중복 방지 규칙을 적용한다. 전체 계약과 이미 확인된 문서를 매번 재출력하지 않는다. Studio 규칙과 로컬 asset manifest를 작업에 필요한 범위에서 사용하되, 이 병행 작업에는 live 씬/프리팹/Editor 편집 권한을 부여하지 않는다.

기존 `Assets/Scripts/CraftingUI.cs`가 구현의 기준이다. `CraftingService.cs`, `RecipeData.cs`, `Workbench.cs`, `DemoPlaceableCatalog`, Tier/Friendship, Input/Inventory는 호출 계약 확인을 위한 읽기 전용이다. 관련 파일만 좁게 읽는다. 현재 UI에는 도감/작업대 구분, 재료 수량, 카드, 실제 CraftingService 호출과 레이아웃 보정이 이미 있다. 전체를 새로 만들지 말고 이를 보존·개선한다.

## 구현할 플레이어 경험

작업대 앞에서 E → 만들 상품과 필요한 재료/잠금 이유 확인 → 제작 클릭 → 실제 결과 피드백 → 닫기와 이동 복귀가 이해되는 화면을 만든다. C는 읽기 전용 제작 도감이며 원격 제작을 허용하지 않는다.

확인된 문제와 개선 요구:

- `OpenRecipeBook`과 모드 안내의 `[Space]`는 데모의 실제 작업대 `[E]` 안내와 맞춘다. 입력 바인딩이나 Workbench/PlayerInteraction을 변경하지 않는다.
- 현재 `IsUnlocked`는 Root/Tier/Friendship을 확인하지만 카드/실패 설명은 Tier 또는 교류로만 설명한다. 실제 실패한 조건을 기존 공개 API로 구분해 읽을 수 있게 표시한다. Mining 도구 업그레이드를 `Tier 0 잠김`으로 안내하지 않는다. 해금 조건·성장 규칙을 새로 만들지 않는다.
- 재료의 보유/필요 수량, 결과 이름·수량, 제작 가능/재료 부족/잠김/도감 상태를 한눈에 구분한다. 클릭 전 부족 상태가 읽혀야 하며, 제작 실패 후 수량을 성공처럼 표시하지 않는다.
- `item.itemName`을 그대로 쓰는 영어 표시를 이 제작 UI의 표시 계층에서 한국어로 개선한다. 실제 Item.id/itemName, 레시피 에셋, 저장 식별자를 변경하지 않는다. 확인된 데모 상품만 매핑하고 알 수 없는 이름은 원본으로 안전하게 표시한다.
- 반복된 긴 줄 대신 기존 카드의 제목·결과·재료·잠금 이유/사용처의 위계를 정리한다. 필요할 때만 선택 상세 영역을 추가한다. 기존 HUD/폰의 디자인과 어울리는 여백·색·대비, 1920×1080 가독성과 작은 해상도의 스크롤을 고려한다. 검증되지 않은 판매 가격·이익·추천 수치나 다음 성장 조건을 만들어 표시하지 않는다.
- 성공/실패, 연속 클릭, 패널 재개방과 다른 UI로 전환할 때 상태/리스너/커서 복원 계약을 유지한다. 공개 필드·메서드와 기존 serialized reference를 보존한다.

유일한 제작 실행은 `CraftingService.TryCraft`다. UI가 Inventory를 직접 차감/지급하거나 잠금을 우회하지 않는다. 빈 재료 슬롯이나 가방 공간을 다루는 기존 service 정책을 임의 변경하지 않는다. 기존 재료 부족/실패 안내도 정확한 정보가 없으면 추측하지 말고 일반 실패 이유로 남긴다.

레시피 추가/재료 수치 변경·작업대 종류·업그레이드 규칙·배치·수납·NPC 생산·상점·점수·저장·낚시·곤충·모션·섬 작업은 포함하지 않는다. P6 전체를 인수한 것으로 해석하지 않는다.

## 파일 격리와 산출물

모든 쓰기는 승인된 세 전용 폴더 안에서만 한다. Assets/Packages/ProjectSettings/Library/기존 Tools/Builds와 공유 상태·문서·관찰 로그는 읽기 전용이다. Unity 호출, live C# 변경으로 발생하는 자동 import/compile, 새 Editor, 게임/빌드 실행, 원본 git index/branch/commit/push, 삭제와 외부 설치를 하지 않는다. 새로운 런타임 권위를 만들지 않는다.

원본 CraftingUI.cs의 정확한 SHA-256·관련 API 근거를 남기고 기준 사본을 전용 폴더에 둔다. 기본 산출물:

1. `Automation/CodexParallel/P6CraftingUI/Source/CraftingUI.cs` — 기존 클래스/API를 보존한 실제 개선본. Assets에 넣기 전까지 import되지 않는다.
2. `Automation/CodexParallel/P6CraftingUI/crafting-ui.patch` — 기준 원본 대비 적용 가능한 diff. 경로는 실제 `Assets/Scripts/CraftingUI.cs`를 가리킨다. 명령의 패치 dry run도 live 파일/인덱스를 바꾸지 않는 방법만 사용한다.
3. `Logs/CodexParallel/P6CraftingUI/<고유실행>/HANDOFF.md` — baseline hash, 변경 파일·메서드, 검증 결과, 적용 절차와 짧은 실제 Play 검수 순서. 적용 코드 경로와 작성 시각을 명시한다.

helper가 직접 필요하면 Tools 전용 폴더에 작게 만든다. 원본 적용용 자동 watcher/교체 스크립트는 만들지 않는다. staged 소스는 기존 컴포넌트의 적용 후보이며 별도 runtime authority로 제품에 함께 넣지 않는다.

## 검증과 완료

이미 설치된 C# compiler/.NET와 로컬 Unity/프로젝트의 캐시 assembly를 읽기 전용 참조로 사용할 수 있으면 **전용 폴더의 격리된 작은 프로젝트**로 개선본을 compile한다. Unity의 .sln/.csproj나 빌드 산출 디렉터리를 실행/변경하지 않는다. compiler 출력/cache도 전용 경로로 지정한다. 이 부분 검사는 Unity 전체 compile이나 실제 Play를 증명하지 않으며 캐시의 출처/시각·대상·한계를 적는다.

환경상 부분 compile이 불가능하면 이유와 확인 못 함을 적고 실제 C# 구현/patch는 끝낸다. 설치로 해결하거나 Unity owner를 가져오지 않는다. 제어 가능한 새 순수 표시 판정이 있다면 대표 잠금/재료/도감 상태만 필요한 작은 검사로 확인한다. Unity 동작을 가짜 stub으로 통과시키거나 코드 문자열 검사만으로 게임 PASS를 만들지 않는다.

관련 수정은 묶어서 검사하고 실패한 부분/영향 범위만 다시 확인한다. 기본 비 Studio retry 정책을 적용한다. 같은 실패 반복, 공유 범위 변경 필요 또는 원본 동시 수정 충돌은 근거를 남기고 멈춘다. 처음부터 게임 전체 validator/영업/저장/MUST PATH를 재실행하지 않는다.

Claude 적용 시 원본 baseline hash가 다르면 전체 파일 교체를 하지 않고 변경 부분을 대조한다. Claude가 P6에서 기존 제작 검증 흐름으로 대표 기존 레시피·잠긴 Root 업그레이드·재료 부족·도감·닫기/이동·GameView를 함께 검증한다. Golden 씬을 여는 기존 `PA_CraftingRecipeCardValidator`와 고정 출력 helper는 무조건 실행하지 않는다.

완료 결과는 `CODE_READY`와 부분 compile의 실제 결과를 보고하며, `LIVE_APPLY / UNITY_COMPILE / PLAY / GAMEVIEW / HUMAN = NOT RUN 또는 UNVERIFIED`를 구분한다. P6 전체 완료나 UI 제품 수락은 Claude의 실제 적용/플레이/사람 검수 뒤에 기록한다. 후속 적용에 필요한 것만 한 번 인계하고, 이 한정 개발을 끝낸 뒤 다른 티켓으로 자동 확장하지 않는다.
