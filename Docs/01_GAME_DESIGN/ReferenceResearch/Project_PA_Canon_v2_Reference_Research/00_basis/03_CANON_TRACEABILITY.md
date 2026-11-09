# Canon v2 추적표

C=Canon, R=현재 코드/기록, H=제안, V/D=원작 근거. 아래 각 행은 구현의 지시 우선순위가 아니라 Canon 계약이 어디서 연구·검수되는지 찾는 지도다. 원작 미확인 범위를 숨기지 않는다.

| Canon 조항 | 보존할 계약 C | 주 근거·한계 | 개발 단위·검수 |
|---|---|---|---|
| §0·IDENTITY §1–3 | 낮 직접 생활·밤 직접 상품/가격 판단, 직접 노동+선택 위임 | DK·ML·DV 상호 보완, 내부 연동 U | ACT/COM/DAY, Q-D02/Q-V04 |
| §1.1 | 공통 문법만 본사 교육, 훈련 물건 초기 지급에 남기지 않음 | AC 문헌, P.A. 기록 | ACT/DAY, Q-A01/Q-D01 |
| §1.2 | 후보3 정확히2, 일반 텐트2 자동 배정 | P.A. Canon/registry | PLC/DAY, Q-P05 |
| §1.3·§2 | 8–12초 픽셀 항해, 조작 시작에서 Day1, 하선 완료/배 landmark | DV 연출 V, 조작 전환 U | PRE/DAY, Q-D01 |
| §3 | 실제 Inventory 보급, Phone item/Hoe 없음 | P.A. Canon/registry | ACT/DAY, Q-D01/Q-D04 |
| §4 | WASD/Space/E·숫자/X·I/Tab·P, 문맥 우선순위, 배치 E/R/Esc/RMB | P.A. 코드 정적 | ACT/PLC, Q-A01/Q-A06/Q-P01 |
| §5 | 성공만 내구도, 실패0, 직접 채집 지속, essential Ground Resource 여유 | DK 행동 일부 V, 연속 나무/돌 U | ACT/WORLD, Q-A02–04/U-01/U-04 |
| §6 | Demo256·biome/chunk 분리·고정 corridor·활동 간격 | DK 환경 V, 일반 카메라/간격 U | WORLD, Q-P06/Q-V01 |
| §7–8 | Base=집/상점/성장, 내부 판매/생활/utility | DK 내부 V, AC 꾸미기 D | PLC/PRE, Q-P01/Q-P07 |
| §9–12 | 공통 placeable, Workbench object, stand3·1stock OPEN, 구역/문/동선 | P.A. placement code R, 원작 preview U | PLC/COM, Q-P01–04/Q-C06 |
| §13 | License1회·Root4·첫 선택 고정·다음 preview·Agriculture 예외 | P.A. registry R | DAY, Q-D04 |
| §14 | 제작품 내가 쓰거나 compatible NPC, old tool 반환·NPC 내구도/unequip 없음, gift optional | P.A. registry R, 전체 gift code 미독 | DAY, Q-D05 |
| §15 | 핵심 정착을 보호하는 Soft Clock, fullgame daylength 미확정 | P.A. clock 기록 R, 원작 시간 복사 금지 | DAY/PRE, Q-D03 |
| §16–18 | CLOSED/OPEN/CLOSE, 이동 금지/가격 조정, 정답 숨김·4단계 가이드 | ML 일부 V, Drag/거절 U, P.A. shop code R | COM/DAY, Q-C01–07 |
| §19 | 핵심 뒤 sunset·하늘/조명/NPC/Dispatch | DV 낮밤 V, P.A. 자연 일몰 기록 R | DAY/PRE, Q-D03/Q-V04 |
| §20 | demo only·close후·낮 점수 없음·C실패아님·rule댓글 | Canon·P.A. report 기록 R, report code 미독 | PRE/COM, Q-C07/Q-V04 |
| §21–22 | Must Path·optional 분리, contextual hint·P 기록 | AC 정보 D, P.A. Feed 접근 부재 R | DAY/PRE, Q-D02/Q-D06 |
| §23–25 | 기능과 체감 품질, 실제 입력 Pass1/2/3 | 원작 I + P.A. 미해결 품질 R | Q-V01–05, W-01–07 |
| §24·§26 | 범위 확장 금지·최신 Canon 우선 | IDENTITY·낡은 reuse map 대조 | 모든 W의 변경 범위 |

## Canon과 현재 코드가 다를 가능성이 있는 검토

낚시의 육지 착지/도구 포획 단계는 현재 R이며 Canon의 새 필수가 아니다. 착지 때 rod 내구도 차감과 ‘물고기 실제 획득’ C 사이의 경계는 U-04에서 실제 상태를 대조한다. Phone 기록이 있지만 진입할 tile이 없는 현재 상태는 §22 접근성 계약의 미완료로 취급한다. UI Space 문구가 남으면 §4의 E 계약에 맞춰 실제 입력과 함께 검수한다.

가격 평가/구매는 현재 동기 R 경로다. 미래 연출을 위해 다음 프레임으로 분리하는 순간 quote 일관성 H가 필요해진다. 현재 코드에 이미 지연 가격 drift 버그가 있다고 단정하지 않는다. 품질 기준이 다른 고객 반응 표현은 실제 quality별 검수 대상으로 삼는다.
