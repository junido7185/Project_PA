# PLC-01/WORLD-01: 예측 가능한 배치와 살아 있는 정착

Canon §6–13·§16·§23. 근거 DK-04–08, AC-D2/3, PA-CODE `WorldBuildingPlacementService`. 새 배치/지형 권위를 만들지 않는다.

## 고정 규칙 C

Demo256은 256×256 logical cell, 2m cell의 512m 규모다. biome는 의미 단위, chunk는 기술 단위다. 고정 항구·정착·주요 corridor/NoSpawn가 진행을 보호한다. Shop/Base는 집·상점·성장 거점이며 별도 Hub가 아니다. Resident Tent는 2개이며 직업 집을 만들지 않는다.

Placeable은 실제 footprint/bounds·valid surface·clearance·rotation·CanMove를 공유한다. Workbench는 작은 오브젝트이며 건물이 아니다. 가판대는 Shop Floor만, Workbench는 Utility/야외/허용된 Living, 가구는 자신의 영역 규칙을 따른다. 문·입구·손님 경로를 보존한다. CLOSED에서 배치/이동 가능, OPEN 중 이동 금지.

## ghost는 결과에 대한 약속 H

| 플레이어 질문 | ghost가 보여줄 정보 | 동일해야 하는 실제 값 |
|---|---|---|
| 크기가 맞나? | 모델·footprint·영역의 규모 | 실제 놓일 collider/bounds와 회전 footprint |
| 어디가 앞인가? | 입구/상호작용 면의 방향 | entrance anchor/접근 지점 |
| 왜 invalid인가? | 막힌 셀·영역/문/동선 이유 | Evaluate의 실제 실패 이유 |
| 확정하면 어디에 놓이나? | anchor와 높이 | commit의 anchor/quarterTurns/terrain |
| 취소하면 어떻게 되나? | 이동 모드/원래 자리 정보 | 원본 instance·점유·활성/재고 복원 |

유효/무효 색만으로 해결되지 않는다. `InvalidGround`, `EntranceBlocked`, `CriticalRouteBlocked` 같은 기존 failure를 ‘이 바닥에는 놓을 수 없어요’, ‘입구 앞을 비워 주세요’, ‘통행로를 남겨 주세요’ 수준으로 전달한다(H). 내부 enum 이름은 사용자 UI에 노출하지 않는다.

## 미리보기와 확정의 경계

H: preview는 후보 검사이며 inventory consumption이나 정착 완료를 일으키지 않는다. 확정 직전에 현재 surface·occupancy·reserved source·shop open state를 다시 검사한다. 성공하면 실제 object/점유/가방 kit의 이전이 한번만 일어나고, 이후 정착 관찰이 진행된다. 실패하면 preview를 유지하거나 일관된 취소 정책으로 돌아가며 kit는 남는다.

R: 서비스에는 `BeginPlacementPreview`, `BeginMovePreview`, `UpdatePreview`, `CommitPreview`, `CancelPreview`와 footprint 일괄 commit이 있다. 이동에서 원본 점유를 잠시 해제하는 모드는 source footprint를 예약하고 원본을 비활성화한다. 복원 실패 시 기존 `ClearPreview`는 원본/예약/preview를 보존하고 false를 반환하여 재시도 가능하게 한다. 호출자가 반환 결과를 무시하고 UI만 닫으면 화면과 점유가 어긋날 수 있으므로 호출 경계 검수가 필요하다(H).

이동은 새 kit 소비/새 instance 생성이 아니다. 기존 수량·상품·가격·배정 주민을 가진 같은 대상의 위치 변경이다(H/C). 표현 모델만 복제한 새 ShopSlot을 만들면 재고/판매 권위가 갈라진다.

## 배치와 영업은 같은 공간을 공유한다

| 경합 H | 필요한 정책 | 실패 사례 |
|---|---|---|
| 이동 preview 중 OPEN 요청 | 완료/취소를 실제로 해결한 뒤 개점하거나 명확히 거절 | 원본 가판대 숨김 상태로 손님 유입 |
| 가격 UI 열림 중 대상 이동/회수 | 대상 유효성·점유/stock를 재확인 | 가격창이 사라진 상품의 값을 확정 |
| 두 kit/이동이 같은 셀 요구 | 기존 placement commit/예약이 하나만 승인 | 겹친 모델·중복 정착 이벤트 |
| 문/상점 경로를 가구가 차단 | preview와 commit 모두 거절 | valid였는데 손님이 입구에서 정지 |
| 취소와 scene disable 겹침 | 복원 결과를 확인한 단일 종료 경로 | 원본을 삭제하고 kit도 잃음 |

전체 씬의 nav validation을 매 입력마다 무거운 재생성으로 해결하라는 뜻이 아니다. 기존 corridor·clearance·WorldNavigationService 기준과 같은 데이터에서 필요한 검사를 수행한다. 비용 최적화는 측정 후 결정한다.

## 작은 정착의 의미를 보인다

H: Base와 텐트 2개의 설치가 끝나면 선택했던 두 동행이 실제 해당 장소에서 읽혀야 한다. 배정은 자동이며 직업과 집 형태를 연결하지 않는다(C). 텐트 배정 실패는 ‘건물 수는 2이므로 완료’로 감추지 않는다. 정착 완료는 실제 세 instance와 두 배정의 조건을 확인하고 License Point를 한번만 연다.

Workbench와 판매대는 건물 이후의 세부 공간 판단을 만든다. 생활·판매·utility 구역의 바닥/벽·상호작용 면이 분명하면 기능 메뉴 없이 장소의 용도를 이해한다. 내부 권장 비율은 Canon의 설계 범위이며 원작 방 크기 측정값도, 임의 모든 퍼센트 합산값도 아니다.

## 섬의 밀도와 행동 간격

H: 항구에서 주요 활동으로 가는 길은 landmark → 접근 가능 지면 → 다음 후보가 읽히는 구조로 검수한다. 자원 spawn 밀도와 실제 보이는 활동 밀도는 다르다. 숲의 나무가 카메라와 대상 사이를 계속 막으면 풍경 밀도가 조작 비용이 된다. 단차 jump 가능·물 진입 제한은 지면/해안 높이의 표현과 일치시킨다. ‘Nav Complete’는 조작과 경로 가독성의 충분 조건이 아니다.

Canon 10–20초 간격은 첫 방문 동선에서 측정한다. 경로의 지도 거리만으로 PASS하지 않는다. essential resource의 Ground Resource 비중과 내구도 여유(각 Canon 25–35% 권장)는 자원/레시피 필수 소모량을 기준으로 계산하고, 무한 보급·정답 위치 화살표로 대체하지 않는다.

## 품질 시나리오

| ID | 실제 입력 | 판정 |
|---|---|---|
| Q-P01 | Base/텐트/Workbench/stand 각각 회전·배치 | 실제 규모·입구·ghost·surface가 일치 |
| Q-P02 | 문 앞·좁은 통행·금지 구역 확정 | 실패 이유가 환경의 문제를 가리키고 kit/점유 불변 |
| Q-P03 | 이동 후 Esc/RMB, 복원 실패 모사 뒤 재시도 | 같은 instance의 원위치/재고/배정 복원, 실패 때 UI 성공 종료 금지 |
| Q-P04 | CLOSED→preview→OPEN→이동 시도 | 숨은 원본/열린 preview/손님 경합 없음, OPEN 이동 금지 |
| Q-P05 | 본사 선택 2명→텐트 2개 설치·재진입 | 선택 결과 유지·자동 배정·License 1회 |
| Q-P06 | 항구→숲→채석→정착, 기본 카메라 | 걷기/점프·대상·출입을 화면만으로 읽음, 물 진입 정책 일치 |
| Q-P07 | 가판대 3개·생활 가구 배치 후 손님 자연 방문 | browse/평가/판매/귀가 동선이 유지됨 |

Q-P03의 실패 모사는 기능 회귀를 위한 fixture이며 자연 플레이 검증과 구분한다. 최종 품질은 같은 후보의 실제 키·마우스 경로로 확인한다.
