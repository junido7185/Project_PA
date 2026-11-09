# Codex 제작 인수 — 기존 Day 1 데모의 남은 티켓 완성

현재 선택 보완: Claude의 새벽 3시 자동 재개를 유지하는 병행 작업에는 `CODEX_PARALLEL_P6_CRAFTING_PROMPT.md`를 사용한다. 이 제작 인수 프롬프트는 Claude를 실제 중단하고 담당을 전환할 때만 사용할 별도 선택지다.

작성일: 2026-10-05. 프롬프트 작성/파일 읽기는 제작 담당 전환이 아니다. 사용자가 아래 실행 메시지를 실제로 전달하고 Claude가 작업을 중단한 것을 확인한 뒤 인수한다.

## Claude에 보낼 중단·인계 메시지

```text
남은 데모 제작은 Codex가 이어받는다. 새 티켓과 새 검증 실행을 시작하지 마.
이미 진행 중인 저장/빌드/검증은 안전한 완료 지점까지 마무리하고,
현재 티켓의 변경 파일·결과·증거 폴더·남은 결함·시도 횟수·정확한 다음 행동을
기존 loop-state와 CODEX_HANDOFF.md에 한 번 기록한 뒤 중단해줘.
게임 파일과 Unity 조작을 더 하지 않는 상태임을 마지막 메시지로 알려줘.
사용량 때문에 완료하지 못한 항목은 미완료로 남기고 파일을 되돌리거나 정리하지 마.
```

이미 Claude가 한도에 도달해 중단했다면 재실행해 인계 문서만 다시 만들 필요는 없다. 실제 중단 메시지·남은 기록·관련 diff로 재개 지점을 복구한다. 프로세스 존재/부재, 사용량 수치, 시간 경과만으로 중단이나 담당 전환을 추측하지 않는다.

## Claude 중단 후 Codex에 보낼 실행 메시지

```text
Project P.A.의 남은 Day 1 데모 제작을 Codex가 이어받아 완성하도록 승인한다.
Claude의 제작 세션은 중단했으며 이후 게임 파일 수정과 Unity 조작을 맡기지 않는다.
AI_WORKFLOW/03_TASKS/CODEX_OPENING_DEMO_TAKEOVER_PROMPT.md를 적용해줘.
기존 demoFeedbackFinishApproval의 범위·수락 기준·완료 결과를 그대로 보존하고,
Unity 제작 담당만 codex로 전환해 실제 중단 티켓부터 P11까지 순서대로 진행해.
검사 도구 제작보다 게임 기능·모션·화면·실제 플레이 가능한 데모 완성을 우선한다.
딩컴·동물의 숲 레퍼런스, 기존 권위·저장 보호·검증 예산·최종 사람 검수는 유지한다.
한 티켓이 끝날 때마다 새 승인을 묻지 말고, 승인 범위 안에서 구현·검증·수정을 이어가.
범위 밖 기능·외부 설치·삭제·commit/push는 승인하지 않는다.
```

위 사용자 메시지의 실제 수신 이후에만 제작 변경을 시작한다. Claude가 아직 실행 중이거나 중단 상태를 확인할 수 없으면 인계 확인과 읽기 작업까지만 수행한다. 다른 작업자를 강제 종료하거나 두 번째 Editor를 띄우지 않는다.

## 기존 계약 재사용과 한정 변경

게임 목표는 `낮 채집·탐험 → 가공·설치·동료·성장 → 밤 상점 운영 → 정산`이 실제 조작으로 이어지는 Day 1 데모다. 자동 도달한 엔딩, 컴파일 PASS, 검사 개수만으로 완성 판정하지 않는다.

AGENTS.md와 필수 현재 문서를 지정 순서대로 읽고 CODEX_HANDOFF, 선택된 승인 record, 실제 최근 증거와 관련 diff를 대조한다. `.agents/skills/pa-demo-factory/SKILL.md`를 적용하고 Canon은 필요한 계약 부분을 읽는다. player-facing 작업에는 VERTICAL SLICE STUDIO MODE를 활성화하고 해당 규칙·로컬 asset manifest·project-pa-vertical-slice skill을 적용한다. 저장·경제 core 작업에는 Studio 예외를 적용하지 않는다.

기존 `CLAUDE_OPENING_DEMO_EXECUTION_PROMPT.md`의 목표·레퍼런스·모션 검수·P1~P11 수락 기준·사용량/검증 중복 방지·납품 기준을 그대로 재사용한다. 이 파일에서 변경하는 항목은 다음뿐이다.

- 제작 실행자와 Unity 담당: Claude → Codex. `.claude/skills` 대신 canonical `.agents/skills`를 사용한다.
- 기존 record와 ticket sequence를 유지한다. 새 실행 큐나 중복 승인 sequence를 만들지 않는다. 완료된 P1/P2/D 티켓은 유효한 증거를 재사용한다.
- 원래 계약의 Opus/max·Claude 전용 실행기/설정은 Codex에 적용하지 않는다. 사용자가 선택한 Codex 모델/추론 설정을 임의 변경하지 않는다.
- Claude 사용량을 기다리지 않고 Codex가 남은 승인 티켓을 연속 진행한다. 최종 milestone과 HARD STOP은 유지한다.
- 납품 검사 도구는 별도 후순위다. 데모 제작의 선행 조건으로 만들거나 이 sequence 안에서 자동 추가하지 않는다.

## 인수와 첫 행동

1. 실제 사용자 전환 메시지와 Claude 중단 근거를 확인한다. 인수 기록에는 실제 메시지와 날짜/증거를 사용하며 파일 작성일을 승인일로 대신하지 않는다.
2. loop-state의 `demoProductionRouting.selectedApproval=demoFeedbackFinishApproval`을 유지하고 `unityEditorOwner=codex`로 바꾼다. routing에 실제 전환 근거를 짧게 기록한다. 옛 notes의 Claude-only 담당 설명은 역사 기록으로 보존하고 현재 담당을 명확히 한다. 원 승인 메시지/티켓 정의/완료 결과/보호 조건/기존 D4 검수·모션 REJECTED/HOLD를 덮어쓰지 않는다. stopAtMilestoneReached와 activeTicket을 무조건 초기화하지 않는다.
3. 기존 환경 도구 `Tools/LoopEngineering/Test-ProjectPADemoEnvironment.py --agent codex`로 한 번 점검하고 실제 결과를 읽는다. 도구의 쓰기 경로를 먼저 확인한다. routing만 바뀌었다고 Unity 연결 PASS를 주장하지 않는다. 연결 인스턴스의 프로젝트 경로/버전, 현재 compile/Play/Console 상태를 실제 확인한다. 열린 같은 Editor를 재사용한다.
4. 인수 전 읽은 상태가 Claude 마지막 기록과 달라졌다면 새 기록을 대조하고 공유 상태를 덮어쓰지 않는다. 완료된 조사/시험을 다시 하지 않고 해당 티켓의 남은 첫 결함부터 이어간다.

작성 시 기록은 P1/P2 기능 PASS, activeTicket=P3_DIRECT_GATHER였다. P3 ticketStatus가 NOT_STARTED여도 실제 작업/증거가 있으면 처음부터 시작하지 않는다. 이 관찰을 현재 상태라고 고정하지 말고 인계 시점의 기록을 따른다. Handoff의 오래된 P1/P2 next action도 최신 ticketStatus/evidence와 맞춰 정합화한다.

미완료 티켓에서 이미 사용한 compile/fix·targeted Play·validator 예산은 작업자가 바뀌어도 그대로 이어받는다. 마지막 검사가 fixture 산술/입력/증거 문제라면 제품 결함과 구분해 증명된 원인만 고친다. 새 세션이라는 이유로 시도 횟수를 초기화하거나 기존 validator를 복제하지 않는다. 기록만으로 예산을 알 수 없으면 확인 못 함을 남기고 명확한 소규모 진단부터 한다. 이미 소진된 복구 예산이나 같은 blocker 반복은 기존 HARD STOP을 따른다.

## 작업과 검증

한 번에 현재 경험 하나만 수정한다. 파일 목록·기존 권위·플레이어에게 보일 결과·필요한 검증을 먼저 짧게 보고한다. 기존 dirty와 로컬 미커밋/ignored 자산을 보존한다. 실제 asset을 먼저 사용하고 Player/Inventory/Economy/Shop/World/Save 권위를 재작성하지 않는다.

P3 직접 도구 채집 → P4 기본 낚시 → P5 기본 곤충 → P6 제작/배치/수납 → P7 동료/성장 → P8 영업/정산 → P9 모션 → P10 섬/첫날 연출 → P11 Windows 납품 순서를 유지한다. 이전 티켓이 실제로 끝났으면 그 결과를 보존하고 다음을 활성화한다. 진행 중인 결함을 숨기고 뒤 티켓으로 뛰지 않는다.

기본 낚시·곤충은 선택 활동이어도 실제 입력·획득·실패/취소·판매 연결을 완성한다. 전 해안 자유 캐스팅·육지 물고기 추격/칼 포획·희귀종 반격, Day 2·농사·계절·여러 작업대·전체 성장 트리는 이 sequence에서 추가하지 않는다.

딩컴과 동물의 숲은 실제 필요한 동작/화면 구간을 보고 관찰 요소를 구현으로 연결한다. 기존 확인 자료부터 사용하고 경험당 대표 레퍼런스 1~2개로 시작한다. 검색 제목이나 설명만 읽고 영상을 봤다고 하지 않는다. 모션은 실제 게임 카메라에서 원속 연속 before/after 영상으로 비교하고, 실제 사용자 수락 전 시각 ACCEPTED를 기록하지 않는다.

관련 수정 묶음 → compile → 현재 티켓의 필요한 검사 → 실제 제품 입력 → GameView → 범위 안 수정 순서로 마무리한다. 기존 증거가 현재 변경에도 유효한지 확인하고 영향받는 회귀만 수행한다. 파일마다 전체 compile/Play를 반복하지 않는다. P1 4세션, D2 영업, 전체 validator/MUST PATH를 매 티켓마다 반복하지 않는다. source·scene·asset·입력/저장/경제 권위 변화의 영향이 있으면 해당 필수 회귀는 수행한다.

실제 입력으로 결과를 얻은 증거와 fixture/API 준비를 구분한다. 숫자 PASS로 가림·지면·그립·모션·오디오·가독성·사람 REJECTED를 덮지 않는다. 검증기를 늘려 제작을 대체하지 않는다. 중간 문서 반복 갱신과 전체 저장소 덤프, 관찰 backlog/스킬 개선 리뷰를 추가 완료 조건으로 넣지 않는다.

전체 NEW GAME→Report, 선택 낮 활동 대표 경로, 격리 저장→종료→재실행→Continue, 실제 standalone 성능·소리는 P11에서 검증한다. 기존 Windows 후보를 현재 소스가 반영된 납품물로 취급하지 않는다. 빌드는 기존 방식으로 고유 후보를 만들며 동일 입력 자동화 실패만으로 반복 빌드하지 않는다. 연결/빌드/자동 조작을 확인 못 하면 정확한 상태와 사람 재현 순서를 남기고 성공을 주장하지 않는다.

## 연속 진행과 종료

기존 demoFeedbackFinishApproval의 approvedTickets/preapprovedThrough=P11, stopAtMilestone=FINAL_WINDOWS_HUMAN_REVIEW, no commit/push를 유지한다. 기능·presentation·human 결과를 구분해 기존 record에 기록하고 다음 승인 티켓을 자동 활성화한다. 기존 D4 검수 대기로 돌아가거나 매 티켓마다 다음 진행 승인을 다시 요구하지 않는다. 새 명시적 사용자 hold와 기존 HARD STOP은 준수한다.

월별 로그/현재 문서는 경험 완료 뒤 필요한 부분만 한 번, Handoff는 세션 중단 시 한 번 갱신한다. 문서에는 완료된 조사·검증과 남은 정확한 다음 행동, 증거·미해결·소진한 예산을 남긴다. 한도/환경/HARD STOP으로 중단되면 실제 상태를 기록하고 재개 가능한 지점으로 마무리한다. 자동 AI 재호출/무한 실행기를 만들지 않는다.

최종 사람 검수까지 데모 전체 PASS/RELEASE ACCEPTED를 선언하지 않는다. 실제 기한이 주어지면 티켓과 검증 완료 상태를 기준으로 위험과 남은 핵심을 짧게 보고하되, 범위·수락 기준을 임의 축소하거나 완료 시간/절감률을 약속하지 않는다.

공식 근거: Codex는 코드 편집·도구 실행·결과 관찰·실패 수정과 외부화된 상태를 활용하는 장기 개발 작업을 지원한다. 이 계약은 그 실행 루프를 기존 프로젝트 승인과 검증 예산에 맞춰 적용한 것이다. [OpenAI 공식 장기 작업 지침](https://developers.openai.com/blog/run-long-horizon-tasks-with-codex)
