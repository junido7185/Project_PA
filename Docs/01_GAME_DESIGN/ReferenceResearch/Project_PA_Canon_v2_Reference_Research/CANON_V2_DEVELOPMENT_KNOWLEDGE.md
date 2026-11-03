# Canon v2 개발용 설계 지식 — 읽기 입구

목표는 기존 Unity 권위를 보존하면서 플레이어의 기대·상태 변화·표현을 일치시키는 것이다. 새로운 기능 목록이나 Canon 변경안이 아니다. [원작 분석](REFERENCE_REVERSE_ANALYSIS.md)과 [Canon 추적표](00_basis/03_CANON_TRACEABILITY.md)에서 판단 근거를 찾는다.

| 구현 작업 | 먼저 읽을 개발 문서 | 주요 품질/실패 경계 |
|---|---|---|
| E/핫바/X·도구·획득·낚시 | [ACT-01/02](02_design_knowledge/01_ACTION_EQUIPMENT_GATHERING.md) | 후보와 프롬프트, selected/held 구분, 성공만 비용, 실제 지급, tap/hold·modal 취소 |
| 섬·kit·ghost·가구·정착 | [PLC-01/WORLD-01](02_design_knowledge/02_PLACEMENT_AND_SETTLEMENT.md) | 실제 bounds·입구·영역·동선, 원본 복원, commit 경합, 선택 동행 주거 |
| 상품·가격 UI·NPC·판매·마감 | [COM-01](02_design_knowledge/03_COMMERCE_PRICE_CUSTOMER.md) | 수량/품질/가격, 확률적 거절 설명, claim/commit, Money/log/report 일치 |
| milestone·Soft Clock·Root·NPC 도구 | [DAY-01](02_design_knowledge/04_PROGRESS_TIME_DELEGATION.md) | 선택 활동 비의존, 1회 결정/보상, old tool 반환, 정보 접근, 시간의 역할 |
| 모션·카메라·UI·SFX·항해·보고서 | [PRE-01/Q](02_design_knowledge/05_PRESENTATION_AND_QUALITY.md) | 각 표현의 사실 출처, 입력 복귀, grip/foot, 사실 기반 댓글, 자연 완주 |
| 실제 코드 변경 범위/인수인계 | [통합 맵/W](02_design_knowledge/06_INTEGRATION_AND_WORK_PACKETS.md) | 현재 권위·읽은 코드·남은 범위·W별 완료 기준 |
| 플레이어 한 행동의 전 과정 | [플레이 단위 사례](02_design_knowledge/07_PLAY_UNIT_CASES.md) | 첫 나무 작업·고객 browse 중 가격·Workbench 이동 취소의 통합 시퀀스 |

## 개발자가 먼저 보존할 여섯 조건 H/C

1. 선택 슬롯의 물건, 손의 물건, 실제 실행 대상이 같은 행동을 설명한다.
2. 실패·취소·가방 부족은 성공한 획득처럼 보이지 않고 기존 재고/세계를 보존한다.
3. 배치 preview의 규모·문·유효성은 실제 commit 결과와 같으며 이동 취소는 원본을 복원한다.
4. 고객의 판단은 해당 상품·가격에 연결되고 확률적 결과를 가격 정답으로 오해시키지 않는다.
5. 판매 하나는 기존 경제/재고/기록/보고서에서 하나의 같은 사실이다.
6. 직접 노동을 이해한 뒤 제작/위임을 선택하고, 선택 활동을 생략해도 Must Path가 끝난다.

## 작업 선택과 완료 보고

W-01 직접 행동 표현, W-02 고객 피드백, W-03 UI 접근/입력의 P0부터 검토한다. W-04 낚시 비용, W-05 배치 복원/경합, W-06 자연 완주, W-07 항해/보고서는 좁은 범위로 이어진다. 모든 작업은 관련 Q 시나리오와 연결되어 있다.

‘컴파일/기능 PASS’와 ‘실제 플레이가 읽힘’을 별도 기록한다. 현재 자료는 연구·정적 대조이므로 구현 후 실제 키/마우스·동일 후보 GameView·음향·실제 inventory/economy 결과로 확인한다. 자세한 검수 경로는 각 문서와 [미확인 실험](03_evidence/OPEN_QUESTIONS.md)에 있다.
