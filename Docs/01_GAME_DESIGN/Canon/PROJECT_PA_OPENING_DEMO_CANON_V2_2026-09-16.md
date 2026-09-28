# Project P.A. — Opening Demo Canon v2
**Status:** LOCKED FOR DEMO IMPLEMENTATION  
**Date:** 2026-09-16  
**Purpose:** 지금까지의 대화에서 가장 최신 결정만 모아, 이전의 충돌하는 데모 계약을 대체하는 단일 기준 문서.

> 이 문서는 **데모에서 실제로 구현하고 보여줄 범위**를 확정한다.  
> 미래 기능은 설계 가능성을 남기되 이번 데모 구현에서는 중단한다.
>
> 이전의 단순 계약  
> `Arrival → Wood → Ore → Fish → Bug → Hub → Shop → Sale`  
> 은 더 이상 최신 기준이 아니다.

---

## 0. 최우선 원칙

Project P.A.의 데모는 다음 두 축이 하나의 하루 안에서 연결되는 경험이어야 한다.

1. **낮:** Dinkum처럼 직접 걷고, 탐험하고, 채집하고, 배치한다.
2. **밤:** Moonlighter처럼 직접 상품을 진열하고 가격을 정한 뒤 상점을 연다.

플레이어의 직접 채집은 초반 전용 기능이 아니다.  
**본게임에서도 계속 가능하다.**

다만 성장하면서 다음 선택지가 추가된다.

`직접 노동 → 더 좋은 도구 해금/제작 → 내가 사용하거나 NPC에게 지급 → NPC 생산량 증가 → 직접 노동과 위임을 자유롭게 혼합`

즉 Project P.A.는 직접 노동을 삭제하는 게임이 아니라,  
**직접 노동을 이해한 뒤 노동을 선택적으로 위임할 수 있는 게임**이다.

---

# 1. 데모 전체 흐름

## 1.1 Prologue — P.A. Company Tutorial
본사의 출항 인증 과정.

여기서는 게임 전체의 **공통 조작 문법만** 가르친다.

### 필수 학습
- WASD 이동
- Space 점프
- E 문맥 상호작용
- 나무 흔들기
- 사과 드롭
- 바닥 아이템 E 줍기
- Hotbar 반영
- 숫자 1~9 Hotbar 선택
- 선택한 아이템이 손에 등장
- X로 현재 들고 있는 아이템 집어넣기 / 빈손
- 상품을 손에 든 상태에서 가판대 E → 진열
- 가격 Drag
- NPC가 가격을 보고 구매 또는 거절

### 튜토리얼에서 가르치지 않는 것
- Axe/Pickaxe 직접 사용법
- Fishing
- Bug Catching
- 건물 배치
- Workbench
- Specialization
- NPC Tool 지급
- 전체 Shop 운영 절차
- 농업

이 기능들은 섬에서 처음 만났을 때 **짧은 1회성 contextual hint**만 제공한다.

### 본사 아이템
본사에서 사용하는 사과·가판대 등은 훈련용이다.  
실제 섬의 초기 지급 아이템으로 이어지지 않는다.

---

## 1.2 Companion Selection
기존 방향 유지.

- 후보 NPC 3명
- 정확히 2명 선택
- NPC는 각자 성격과 초기 전문성을 가진다.
- 선택 결과는 섬 정착 이후에도 유지한다.

### 주거
NPC 직업과 집 형태는 연결하지 않는다.

금지:
- Miner House
- Lumberjack House
- Farmer House

사용:
- `Resident Tent`
- `Resident Tent`

설치 후 집 없는 선택 NPC가 자동으로 텐트를 배정받는다.

---

## 1.3 Voyage
동행 NPC 선택 후 섬으로 이동.

### 연출
**Dave the Diver식 8~12초 Pixel Micro-Cinematic**

권장 Shot:
1. P.A. 항구에서 출항
2. 파도/배/동행 NPC
3. 멀리 섬 실루엣
4. 섬 확대
5. Fade → 실제 3D 섬

프리렌더 장편 영상이나 범용 컷신 시스템을 새로 만들지 않는다.

---

# 2. Day 1 시작

튜토리얼과 항해는 **Day 1 인게임 시간에 포함하지 않는다.**

Day 1은 실제 섬 항구에서 플레이어 조작이 시작되는 시점부터 시작한다.

### 시작 상태
- 실제 Demo256 섬
- 플레이어 + 동행 NPC 2명은 이미 하선
- 배는 배경 landmark
- 승선/하선 시스템 없음
- 기존 배가 나무/부두와 겹치지 않도록 방향과 위치를 조정
- 가능하면 부두 방향과 맞춰 90° 전후로 재배치

---

# 3. Supply Box

항구 근처에 `P.A. Pioneer Supply` 보급 상자 배치.

E로 열면 실제 Inventory/Hotbar 권위를 통해 지급한다.

### 지급 항목
- Starter Axe
- Starter Pickaxe
- Starter Fishing Rod
- Starter Net
- Pioneer Shop/Base Blueprint
- Resident Tent Blueprint ×2
- P.A. Field Workbench Kit ×1

> **[2026-09-19 설계 확정]** P.A. Smartphone은 물리 아이템이 아닙니다.  
> Inventory/Hotbar를 차지하지 않으며, P 키로 항상 접근 가능한 **시스템 UI**입니다.  
> Supply Box에서 지급하지 않습니다. 에이전트는 Smartphone을 Item으로 재도입하지 마세요.

### 지급하지 않는 것
- Hoe

Agriculture는 데모에서 실제 농사 루프를 구현하지 않는다.

상자는 지급 완료 후 열림 연출 → 짧은 fade/shrink → 제거.

---

# 4. 조작 계약

## 이동
- WASD: 이동
- Left Shift: 달리기
- Space: 점프

## Hotbar
- 1~9: 정확한 Hotbar 슬롯 선택
- Mouse Wheel: 선택 이동
- 선택한 아이템/도구는 손에 실제로 보인다.
- X: 들고 있는 아이템/도구 집어넣기 → 빈손

## 문맥 행동
- E: 현재 문맥의 핵심 행동

예:
- 빈손 + 바닥 아이템 → 줍기
- 빈손 + 나무 → 흔들기
- Axe + 나무 → 벌목
- Pickaxe + 바위 → 채광
- Net + 곤충 → 포획
- Rod + 물가 → 낚시
- 상품 + 빈 Display Stand → 진열
- 빈손 + NPC → 대화
- 빈손 + Supply Box → 열기
- 빈손 + Workbench → 제작
- 빈손 + Display Stand → 관리

## 배치
- E: 배치 확정
- R: 회전
- Esc / RMB: 취소

## UI
- I 또는 Tab: Inventory
- P: Smartphone

### 문맥 우선순위
1. Ground Pickup
2. 명시적 Interactable
3. 현재 Tool의 유효 대상
4. 자연 상호작용

---

# 5. 도구와 내구도

## 5.1 플레이어 도구
내구도는 Swing 횟수가 아니라 **성공한 작업 결과**에만 차감한다.

예:
- 허공에 Axe 사용 → 0
- 잘못된 대상에 Axe 사용 → 0
- 나무에 유효한 Axe 작업 성공 → -1
- 곤충 실제 포획 → Net -1
- 물고기 실제 획득 → Rod -1

Starter Tool은 일회성 데모 도구지만,  
필수 개척 자원을 정확히 모으자마자 즉시 부서지게 만들지 않는다.

### 밸런스 원칙
필수 개척에 필요한 성공 행동 수 대비 약 25~35% 여유를 둔다.

정확한 내구도 숫자는 실제 동선/채집 시간 테스트 후 튜닝한다.

---

## 5.2 직접 채집은 본게임에서도 유지
직접 채집은 초반 bootstrap 전용이 아니다.

플레이어는 성장 이후에도:
- 직접 Forestry
- 직접 Mining
- 직접 Fishing
- 직접 Bug Catching

을 계속 할 수 있다.

---

## 5.3 Ground Resource
나무 조각, 돌멩이 같은 기초 자원은 일부 바닥에서도 줍는다.

권장:
- 전체 초기 필수 자원의 약 25~35%는 Ground Pickup
- 나머지는 Tool Gathering

목적:
- 탐색 보상
- 초반 도구 의존 완화
- 채집 방식의 차이 체험

---

# 6. 섬 설계

## 6.1 기본 크기
기존 Demo256 논리 월드를 유지한다.

- 256 × 256 logical cells
- 2m per cell
- 약 512m × 512m

새 1024² 월드나 Infinite Streaming은 이번 데모 범위가 아니다.

---

## 6.2 Dinkum식 섬 구성
섬은 단순한 빈 테스트 평면이 아니라 **Biome + Spawn Rule 기반의 큰 생활 공간**으로 만든다.

핵심 개념:

- Chunk = 기술 단위
- Biome = 플레이 경험 단위

### 권장 Macro Layout
- South: Harbor / Coast
- Center-South: Settlement Meadow
- West / North-West: Forest
- North / North-East: Highland / Quarry
- East: Flower / Bug Meadow
- Coastline: Fishing

---

## 6.3 생성 철학
완전 수동 배치도 아니고 완전 random도 아니다.

**약 80% Rule-based Spawn + 20% Fixed Critical Space**

### 고정 공간
- Harbor
- Settlement Meadow
- 주요 이동 corridor
- 주요 Landmark
- 첫날 필수 자원 접근 구역

### Biome Spawn Data 예
- Spawn Catalog
- Density
- Minimum Separation
- Random Yaw
- Scale Range
- Slope Range
- Shoreline Range
- Exclusion Mask
- Building No-Spawn Zone
- Road/Path Clearance

### 생성 순서
1. Island shape / height
2. Biome 판정
3. Harbor / Settlement 등 Fixed Landmark
4. No-Spawn corridor
5. Biome Resource spawn
6. 큰 환경 오브젝트
7. Grass / Flower / small rock dressing
8. Fish / Bug activity

### 품질 목표
- debug label 없이 biome 구분 가능
- 빈 평원 금지
- 오브젝트 겹침 최소화
- 길이 나무/바위로 막히지 않음
- 주요 활동/랜드마크가 10~20초 이동 간격 내에 존재

---

# 7. Shop/Base = 플레이어의 집 + 상점 + 성장 거점

데모에서는 별도 Management Hub를 만들지 않는다.

`Pioneer Shop/Base` 하나가 다음 역할을 겸한다.

- 상점
- 플레이어 거주 공간
- 성장 거점
- 기본 관리 중심

Full Game에서 업그레이드 후:
- 2층 거주공간
- 별도 커스터마이징 Home
- 별도 관리 공간

등으로 확장할 수 있다.

이번 데모에서는 구현하지 않는다.

---

# 8. Shop Interior

상점 내부는 기능 테스트룸이 아니라 **실제 생활 공간**처럼 보여야 한다.

## 8.1 내부 영역
권장 비율:
- Shop Floor: 약 60~65%
- Player Living: 약 20~25%
- Utility: 약 15~20%

정확한 치수는 실제 모델과 GameView를 보고 조정한다.

### Shop Floor
- Display Stand 배치 가능
- 고객 이동 공간
- 가격 설정/판매 영역

### Player Living
- 자유 가구 배치
- Starter Bed
- Storage Chest
- Chair
- Small Table
- Lamp
- Rug

모든 가구가 데모에서 완전 기능을 가질 필요는 없다.

### Utility
- Workbench
- Storage
- 향후 제작 설비 가능

---

# 9. Placeable 공통 문법

작업대, 가판대, 가구를 각각 별도 배치 시스템으로 만들지 않는다.

## Placeable Family
### Utility
- Field Workbench

### Commerce
- Display Stand

### Furniture
- Bed
- Chair
- Lamp
- Table
- Rug
- Storage

### 공통 데이터
- Footprint
- PlacementBounds
- ValidSurface
- CollisionClearance
- RotationStep
- CanMove

---

# 10. Workbench

작업대는 건물이 아니다.

**Dinkum식 Placeable Furniture/Object**

### 획득
Supply Box에서:
- `P.A. Field Workbench Kit ×1`

### 배치 가능
- Settlement ground
- Shop Utility Area
- 필요한 경우 일부 Living Area

### 사용
빈손 + Workbench:
- E → Crafting
- Hold E 약 0.5s → Move Mode

Move Mode:
- WASD 위치 조절
- R 회전
- E 확정
- Esc 취소 → 원래 위치 복원

---

# 11. Display Stand

가판대는 ShopSlot 자체가 아니라 **ShopSlot 기능을 가진 Placeable Furniture**로 취급한다.

예:
`Starter Display Stand`
→ 내부에 기존 `ShopSlot`

장기적으로 다른 가판대 모양이 생겨도 판매 권위는 그대로 재사용한다.

---

## 11.1 첫 상점의 가판대 수
**Starter Display Stand ×3**

이 수를 데모 기준으로 사용한다.

이유:
- 2개는 화면이 비어 보임
- 4개 이상은 첫날 관리량이 과도함
- 3개면 서로 다른 가격 전략을 시험할 수 있음

Shop/Base 완성 후 Starter Display Stand 3개를 바로 사용할 수 있게 한다.

### OPEN 조건
가판대 3개를 모두 채울 필요는 없다.

**1개 이상 진열되면 OPEN 가능**

2~3개를 채우는 것은 플레이어 선택.

---

## 11.2 진열
상품을 손에 든 상태 + 빈 Display Stand:

`[E] 진열`

실제 ItemInstance를 기존 Inventory/ShopSlot 권위를 통해 이동한다.

---

## 11.3 가판대 관리와 이동
빈손 + Display Stand:

- E → 관리
- Hold E 약 0.5s → 이동

이동:
- Preview
- R 회전
- E 확정
- Esc → 원래 위치 복원

### 영업 중
SHOP OPEN 동안:
- Display Stand 이동 금지
- Furniture 이동 금지

SHOP CLOSED 동안:
- 자유롭게 이동 가능

---

# 12. Placement Zone

자유 배치이지만 기능적으로 필요한 제한은 둔다.

### Display Stand
- Shop Floor: 가능
- Living: 불가
- Utility: 불가
- Outdoor: 불가

### Workbench
- Utility: 가능
- Outdoor Settlement: 가능
- Living: 제한적 가능
- Shop Floor: 기본적으로 비권장/제한

### Home Furniture
- Living: 가능
- 일부 Shop Floor/Utility: 오브젝트별 허용

### 필수 Collision/Navigation Rule
- Door 앞 No-Placement
- 고객 통로 확보
- 다른 큰 가구와 최소 간격
- 벽 clipping 방지
- NPC path blocking 방지

---

# 13. Specialization

## 13.1 해금 시점
다음이 완료되면:

- Pioneer Shop/Base 설치
- Resident Tent ×2 설치

→ `Settlement Established`
→ License Point +1

별도 Hub 설치는 요구하지 않는다.

---

## 13.2 첫 Root
4개 모두 선택 가능.

- Forestry
- Mining
- Fisheries
- Agriculture

선택 NPC 전문성과 맞는 Root에는:

`★ COMPANION SYNERGY`

표시.

하지만 다른 Root 선택을 막지 않는다.

---

## 13.3 데모의 Tree 깊이
전체 특성화 트리를 만들지 않는다.

데모:
- Root 4개 표시
- 선택 Root의 다음 1단계 정도만 Preview

예:
Forestry I
→ Better Axe
→ Carpentry

실제 미래 기능은 구현하지 않는다.

---

## 13.4 첫 선택의 permanence
- Demo: 첫 선택 고정
- Full Game: 초기 1회 무료 respec 고려

Full Game 규칙은 아직 최종 확정 아님.

---

## 13.5 Agriculture 예외
데모에서 Agriculture gameplay를 구현하지 않는다.

Agriculture Root를 선택해도:
- 농업 specialization 선택 가능
- 향후 Hoe / Crop Plot / Seed 시스템 Preview 가능

하지만:
- 실제 Hoe 지급/사용
- 밭 갈기
- 씨앗
- 물주기
- 성장
- 수확

은 데모에서 구현하지 않는다.

---

# 14. Tool Upgrade와 NPC Delegation

Specialization을 통해 Workbench에 업그레이드 Recipe가 열린다.

업그레이드된 도구는:

1. 플레이어가 직접 사용하거나
2. 호환되는 NPC에게 지급할 수 있다.

NPC에게 지급하면 해당 NPC의 생산량/작업 효율이 증가한다.

### NPC Tool Rule
- NPC Tool은 내구도 없음
- 장기 유지
- Player가 임의로 벗겨오는 별도 Unequip UI 없음
- 새 도구를 지급할 때만 기존 도구 반환

예:
NPC가 Basic Axe 보유  
→ Improved Axe 지급  
→ Basic Axe는 Player에게 반환  
→ NPC는 Improved Axe 장착

### Demo Objective
Specialization 선택은 필수.

NPC Tool 지급은 **선택적 보너스 행동**.

이유:
플레이어가 동행 NPC와 맞지 않는 Root를 선택할 자유를 보장해야 한다.

시연 영상에서는 Companion과 맞는 Root를 선택해 이 시스템을 보여주는 것을 권장한다.

---

# 15. Day 1 Time Design

데모는 **하루 안에 끝낸다.**

Day 2는 이번 데모에 넣지 않는다.

### 이유
한 번의 흐름으로:
- 도착
- 개척
- 정착
- 성장
- 첫 영업
- 결과

까지 닫히는 것이 더 강하다.

---

## 15.1 Soft Clock
실제 시간 배율만 강제하지 않는다.

빠른 플레이어와 느린 플레이어 모두를 위해 milestone 기반 pacing을 사용한다.

개념:
- 시간은 자연스럽게 흐름
- 필수 정착이 아직 안 되었는데 밤이 갑자기 오지 않게 보호
- 필수 정착 완료 후 Sunset까지 자연스럽게 가속 가능
- Night Shop으로 연결

### Full Game Day Length
아직 확정하지 않는다.

Demo Day 1 pacing은 Opening 전용이다.

---

# 16. Night Shop

판매는 **상시가 아니다.**

SHOP OPEN 상태에서만 판매한다.

## CLOSED
가능:
- 가구 배치
- 가판대 이동
- 상품 진열
- 가격 설정
- 제작
- 인테리어

NPC 구매 AI 비활성.

## OPEN
- NPC Customer Flow 활성
- 상품 구매/거절
- 가격 변경 가능
- Furniture/Display 이동 금지
- 현재 특정 상품을 평가 중인 NPC가 있으면 해당 Stand 가격은 평가 종료까지 잠시 Lock 가능

## CLOSE
- 판매 종료
- 정산
- Demo Pioneer Report

---

# 17. 가격 시스템

Moonlighter식 “내가 가격을 정한다”는 감각을 유지한다.

### 기본 UI
- Current Price
- Drag Control
- ±1 / ±10 수준 미세 조정 가능
- 필요시 Mouse Wheel Fine Tune

### 핵심
가격의 정답을 UI가 미리 직접 알려주지 않는다.

플레이어는:
- 구매
- 고민
- 거절

NPC 반응을 보고 학습한다.

---

# 18. First Shop Mini Guide

본사 튜토리얼과 별개로, 실제 첫 상점에서 **한 번만** 간단한 운영 가이드를 준다.

1. 상품을 손에 들고 가판대 E → 진열
2. 가격표 Drag → 판매가 결정
3. 가판대 1개 이상 진열 → OPEN 가능
4. 첫 NPC 반응 후 → 가격을 다시 조정할 수 있다는 Toast

긴 팝업 튜토리얼이나 체크리스트 HUD를 사용하지 않는다.

---

# 19. Day → Sunset → Night

첫날 정착 핵심이 끝나면 Sunset으로 자연스럽게 연결한다.

예:
- Shop/Base
- Tent ×2
- Specialization

등이 완료되면 Day pacing을 Sunset 구간으로 연결.

### Presentation
- 하늘 변화
- 실내/외 조명 변화
- NPC 이동 변화
- P.A. Dispatch Toast

예:
`첫 영업을 준비하세요.`

밤에는 상점 내부가 따뜻하게 빛나야 한다.

---

# 20. Pioneer Report — Demo Only

이 시스템은 **본게임 핵심 시스템이 아니다.**

졸업 데모의 완결감과 반복 시연 재미를 위한 Presentation Layer.

### 공개 시점
**SHOP CLOSE 직후**

### 낮 동안
점수/랭크를 보여주지 않는다.

체크리스트 게임처럼 느껴지지 않게 한다.

---

## 20.1 평가 영역
권장 초기 배점:

- Settlement: 25
- Commerce: 30
- Development: 25
- Exploration: 20
- Total: 100

정확한 숫자는 Playtest로 튜닝 가능.

### 평가 예
Settlement:
- Shop/Base
- Tent ×2

Commerce:
- 진열
- 가격
- 판매
- 매출

Development:
- Specialization
- Tool craft
- Optional NPC tool gift

Exploration:
- 추가 biome
- Fish
- Bug
- 추가 채집

Fish/Bug는 필수 진행이 아니라 **자유 행동 + Rank bonus**.

---

## 20.2 Rank
Rank는 실패 판정이 아니다.

플레이 스타일에 대한 결과 표현.

초기 튜닝 예:
- S: 90+
- A: 75+
- B: 60+
- C: below 60

정확한 threshold는 Playtest로 조정 가능.

---

## 20.3 Social-style Comments
Instagram/SNS처럼 행동 기반 댓글을 보여준다.

LLM 생성 필요 없음.

Rule-based Comment Pool 사용.

예:
- 비싼 가격 위주 → 가격 관련 댓글
- Bug 많이 포획 → 벌레 관련 댓글
- 높은 매출 → 상점 관련 댓글
- NPC Tool 지급 → 동료 관련 댓글
- 빠른 정착 → 개척 관련 댓글

결과 화면:
- Score
- Rank
- 2~4개의 행동 기반 댓글

---

# 21. 데모의 필수와 선택

## MUST PATH
Tutorial
→ Companion 3명 중 2명
→ Pixel Voyage
→ Real Island
→ Supply Box
→ Wood/Stone 확보
→ Shop/Base
→ Tent ×2
→ Specialization
→ Sunset
→ Display
→ Price
→ OPEN
→ 실제 NPC Sale
→ CLOSE
→ Pioneer Report

## OPTIONAL / BONUS
- Fishing
- Bug catching
- 추가 biome 탐험
- 추가 Ground Pickup
- 추가 상품 가격 실험
- NPC Tool Upgrade/Gift
- 추가 상호작용

Optional을 강제 Quest로 만들지 않는다.

---

# 22. HUD / Quest Guidance

고정 Quest List를 화면에 계속 띄우지 않는다.

### 기본 방식
- 상단/우측 Toast
- 2~4초 후 사라짐
- 놓친 목표는 Smartphone의 P.A. Dispatch에서 확인 가능

즉:
- HUD = 순간 알림
- Smartphone = 기록

---

# 23. 완성도 목표

미래 기능은 과감히 잘라내지만,  
**데모에서 실제로 보이는 범위의 품질은 완성 게임 수준을 목표**로 한다.

### Player
- Idle
- Walk
- Run
- Jump
- Tool animation
- Held item/tool

### Camera
- Dinkum식 readable third-person
- 건물/나무 obstruction 대응
- 적절한 FOV / 거리 / pitch

### World
- 바이옴 명확
- 충분한 밀도
- 빈 테스트 평면 금지
- visible primitive/debug asset 금지

### Gathering
- animation
- impact timing
- SFX
- drop/reward feedback

### Placement
- clear preview
- valid/invalid
- rotation
- cancel
- move

### Shop
- 실제 상점처럼 보이는 interior
- 3 Display Stands
- 실제 상품 모델
- NPC reaction
- warm night lighting

### UI
- minimal HUD
- no debug labels
- no validator panels

### Audio
- ambient
- action SFX
- shop transition
- sale feedback

### Ending
- Pioneer Report
- action-based comments
- rank

---

# 24. 이번 데모에서 절대 확장하지 않는 범위

- 1024² world
- Infinite streaming
- Full procedural world variety
- 여러 날 캠페인
- Day 2
- Seasons
- Multiplayer
- Full Specialization Tree
- 실제 Agriculture loop
- Hoe gameplay
- 다단계 NPC Equipment progression
- NPC Tool durability
- Full Home customization
- 2F
- Wallpaper/floor editor
- Furniture catalog dozens
- Furniture shop
- Full interior rating
- 여러 Workbench 종류
- generalized cutscene framework
- 새 Save architecture
- 새 Economy authority
- 새 Inventory authority
- 새 Shop authority
- Rank persistence into Full Game
- 무한 SNS feed/comment generation

미래 기능은 Preview 또는 문서 설계까지만 허용한다.

---

# 25. 개발 우선순위

## Pass 1 — FIRST DAY WORLD & CONTROL
- Tutorial rebase
- Dinkum locomotion/animation
- Hotbar / Held Item / X
- contextual E
- Pixel Voyage
- Harbor
- Supply Box
- Hero Demo256 biome world
- Ground pickup
- Jump
- Camera
- actual assets

**결과:** 실제 60~90초 GameView 영상

---

## Pass 2 — SETTLEMENT & DELEGATION
- Starter durability
- Workbench Kit
- Shop/Base
- Resident Tent ×2
- Interior shell
- 3 Starter Display Stands
- common Placeable/move grammar
- Specialization roots
- one representative tool upgrade
- optional NPC tool gift
- Sunset

**결과:** Arrival → Settlement → Growth 영상

---

## Pass 3 — FIRST BUSINESS DAY
- actual item display
- Price Drag
- Shop OPEN/CLOSE
- customer buy/reject
- sale feedback
- night lighting
- shop mini guide
- Pioneer Report
- comments
- Rank
- animation/SFX/presentation polish

**결과:** 전체 12~15분 데모 완주 영상

---

# 26. 결정 우선순위 / 충돌 처리

이 문서와 이전 문서/프롬프트가 충돌하면:

1. 이 `Opening Demo Canon v2`
2. 최신 사용자 직접 지시
3. Studio Mode
4. 기존 Vertical Slice 문서
5. 구버전 task/prompt

순으로 해석한다.

단, 다음 권위는 기존 프로젝트를 계속 재사용한다.

- PlayerController
- CameraController
- PlayerInteraction / IInteractable
- Inventory / ItemInstance
- Hotbar
- ShopSlot
- PurchaseEvaluator
- EconomyService
- SalesLogManager
- World Grid / Generation / Navigation
- Placement
- Save
- 기존 NPC systems

두 번째 권위를 새로 만들지 않는다.

---

# 27. 현재 최종 한 줄

> **P.A. 본사에서 기본 조작과 가격 결정을 인증받고, 두 동료를 골라 픽셀 항해 연출로 무인도에 도착한다. 보급 상자의 스타터 도구·작업대·상점/텐트 설계도로 직접 섬을 개척하고, Shop/Base와 두 텐트를 세운 뒤 첫 특성화를 선택한다. 플레이어는 직접 채집을 계속할 수도 있고 장기적으로 더 좋은 도구를 NPC에게 넘겨 생산을 위임할 수도 있다. 해질 무렵 자신이 배치한 가판대에 낮에 얻은 상품을 올리고 직접 가격을 정해 밤 영업을 시작하며, 영업 종료 후 그날의 행동을 반영한 P.A. Pioneer Report의 댓글·점수·Rank로 데모가 마무리된다.**
