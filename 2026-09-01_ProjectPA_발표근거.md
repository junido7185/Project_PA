# Project P.A. 2026 여름방학 진행상황 발표 근거

작성일: 2026-09-01\
집계 기간: 2026-06-26 ~ 2026-08-31\
기준 브랜치/HEAD: `milestone/gameplay-beta-85` / `29fb98f400b5`

## 판정 원칙

- 2026-06-26 이전에 존재한 기능은 방학 신규 성과로 세지 않았다.
- 커밋 시각이 재작성된 구간은 author date와 문서 기록을 함께 비교했다.
- 코드 존재와 플레이 가능 상태를 구분했다.
- 발표 문구는 `연결됨`, `검증됨`, `부분 가능`, `차단됨`, `진입 불가`, `계획`으로 나눠 표현했다.
- 수치가 불확실한 성과는 과장하지 않고 데모 경로와 검증 결과를 우선했다.

## 슬라이드별 주장과 근거

### 1. 표지

주장: 2026년 여름방학 개발 진행상황을 2026-06-26부터 2026-08-31까지 집계한다.

근거:

- Git author-date 기준 첫 방학 체크포인트 `09af7bc`와 현재 HEAD `29fb98f`.
- `2026-08-31_SUMMER_DEVELOPMENT_AUDIT.md`의 집계 범위 및 주의사항.

### 2. Project P.A.의 정의와 핵심 루프

주장: 낮의 생활·수급, 밤의 진열·가격 판단·판매, NPC 소비, 매출·관계·마을 변화가 하나의 게임 정체성을 이룬다.

근거:

- `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_IDENTITY.md`
- `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_SCOPE.md`
- 기존 디자인 마스터의 핵심 메커니즘 도식.

### 3. 6월 26일 기준선

주장: 이동, 인벤토리, 핫바, 상점 가격·구매 판단, NPC FSM, 경제, 기본 저장 등 기능 단위 기반은 이미 존재했지만, 낮 활동부터 결산·마을 반응까지 이어지는 연속 플레이는 완성되지 않았다.

근거:

- Git baseline tree `adc5d2f5eaa83b058b201eb15ec563cffd41275e`.
- 6월 21~25일 기록을 방학 신규 성과에서 제외한 `2026-08-31_SUMMER_DEVELOPMENT_AUDIT.md`.
- `PROJECT_PA_STATUS.md`와 과거 개발일지의 시스템별 상태 기록.

### 4. 여름방학의 실제 진척

주장: 방학 동안 핵심 루프 연결, 플레이 가능한 월드, NPC·경제 피드백, 저장·검증 체계가 확장됐다.

근거:

- WORLD 커밋 구간 `776fd3a` ~ `e0b5678`.
- BETA 커밋 구간 `9bf71d6` ~ `1539923`.
- 통합 체크포인트 `4bf89f4`, `46dbea9`, `6168d05`, `29fb98f`.
- `AI_WORKFLOW/06_HANDOFF/HANDOFF_FOR_CODEX.md`의 M70/M85/BETA 검증 기록.
- `2026-08-31_SUMMER_DEVELOPMENT_AUDIT.md`의 Before → Summer → Current 표.

### 5. 8월 31일 실제 플레이 상태

주장:

- `Prototype_FirstDay`에서는 안내를 동반한 Day 1 판매·감사·결산 흐름을 부분 시연할 수 있다.
- `WorldSandbox`에는 최신 M85 루프가 있으나 Build Settings에 포함되지 않아 일반 실행 경로에서 도달할 수 없다.
- 저장·불러오기 통합 검증에서 방향 복원 오차 `137°`가 발생해 BETA-010이 차단 상태다.

근거:

- `Docs/VisualAudit/2026-08-25-29fb98f/VISUAL_BASELINE.md`
- `ProjectSettings/EditorBuildSettings.asset`
- `Assets/Scenes/Prototype_FirstDay.unity`
- `Assets/Scenes/WorldSandbox.unity`
- `Docs/VisualAudit/2026-08-25-29fb98f/11_shop_price.png`
- `Docs/VisualAudit/2026-08-25-29fb98f/13_npc_feedback.png`
- `Docs/VisualAudit/2026-08-25-29fb98f/14_audit_app.png`

참고 관찰: 실제 판매 시 잔액이 500G에서 530G로 바뀌고 감사 화면에 30G가 반영됐다. 이는 경제 반영이 화면에서 관찰된 사례이며, 최신 전체 루프가 완결됐다는 뜻은 아니다.

### 6. 중간평가 피드백에서 졸업전시 Demo로

주장: 이번 학기 목표는 처음 보는 관람객이 3~5분 안에 역할을 이해하고, 운영 판단을 내리고, NPC 반응과 결과를 확인한 뒤 결산으로 사이클을 닫는 Vertical Slice Demo다.

근거:

- 사용자 제공 최종 통합 제작 지시의 발표 목표 및 중간평가 피드백.
- `PROJECT_PA_TODO.md`의 Demo Gap 및 우선순위.
- `PROJECT_PA_STATUS.md`의 현재 플레이 가능 범위.

### 7. 이번 학기 우선순위

주장: 우선순위는 P0 복원·진입, P1 3~5분 사이클, P2 화면 가독성, P3 standalone·Audio·Visual polish 순이다.

근거:

- `2026-08-31_SUMMER_DEVELOPMENT_AUDIT.md`의 Demo Gap 표.
- `PROJECT_PA_TODO.md`
- `Docs/VisualAudit/2026-08-25-29fb98f/VISUAL_BASELINE.md`
- `AI_WORKFLOW/04_VERIFICATION/VERIFICATION_RULES.md`

## 발표에서 피해야 할 표현

- “전체 루프 완성” — 최신 루프는 단일 진입 경로로 검증되지 않았다.
- “저장/불러오기 완료” — 방향 복원 blocker가 남아 있다.
- “졸업전시 빌드 완료” — WorldSandbox는 Build Settings에 포함되지 않았고 standalone 1920×1080 결과도 확인되지 않았다.
- “여름에 모든 기반 기능 구현” — 이동·상점·NPC·경제·기본 저장의 상당 부분은 방학 이전부터 존재했다.

## 확인하지 못한 항목

- 최종 PPT 제작 시점의 Unity 재컴파일 및 PlayMode 재실행: 이 작업은 발표자료 제작이며 Unity Editor를 열거나 프로젝트를 변경하지 않았다.
- 1920×1080 standalone 빌드의 최종 화면 품질: 현재 근거 문서에서 미관찰 상태다.
- 실제 5분 이내 관람객 반복 플레이 성공률: 아직 계획 단계다.
