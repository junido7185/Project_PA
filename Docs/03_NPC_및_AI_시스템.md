# 🏝️ 프로젝트 P.A. (Pioneer Assistance) - NPC 및 AI 시스템

## 1. NPC AI 아키텍처 (Implementation)
NPC는 단순한 장식 요소가 아닌, 섬의 경제 생태계를 실질적으로 구동하는 에이전트입니다. [cite_start]이를 위해 **NavMesh**와 **FSM**을 결합한 자율 행동 시스템을 사용합니다[cite: 94, 179].

### 1.1. 탐색 및 이동 (NavMesh)
* [cite_start]**목적:** 인공지능 에이전트가 복잡한 마을 공간 내에서 길을 찾기 위한 추상 데이터 구조로 활용함[cite: 97].
* [cite_start]**구현:** 모든 경로는 NavMesh를 통해 계산되어 마을의 지형지물과 충돌하지 않고 자연스럽게 이동함[cite: 103, 180].

### 1.2. 행동 제어 (Finite State Machine)
* [cite_start]**구현:** NPC의 상태는 FSM으로 관리되며, 성향 변수에 따라 각 상태 간의 전이 확률이 조정됨[cite: 101, 180].
* **상태 예시:** 배회(Wander) -> 대기(Idle) -> 이동(Move) -> 상호작용(Interact).
* [cite_start]**로직 적용:** 예를 들어, 휴식 상태에서 '광장으로 이동'할 확률은 외향적인(E) NPC가 내향적인(I) NPC보다 현저히 높게 설정됨[cite: 102, 180].

## 2. MBTI 기반 행동 및 소비 로직
[cite_start]NPC의 MBTI는 단순한 텍스트 설정이 아닌, 실제 **AI 연산 파라미터**로 작동하여 동적인 커뮤니티를 형성함[cite: 177, 178].

### 2.1. 성향별 내부 변수 (Internal Variables)
| MBTI 차원 | AI 반영 변수 (Variable) | [cite_start]실제 행동 양상 [cite: 93, 177, 178] |
| :--- | :--- | :--- |
| **E (외향)** | `Social_Weight`, `Area_Radius` | 광장 체류 시간 증가, 주변 NPC 행복도 버프 제공 |
| **I (내향)** | `Work_Efficiency`, `Home_Stay_Time` | 집에서의 작업 시간 증가, 생산 효율 가산점 |
| **S (감각)** | `Utility_Consumption_Rate` | 도구, 식재료 등 실용적인 물건 위주의 구매 패턴 |
| **N (직관)** | `Luxury_Consumption_Rate` | 비싼 가구, 장식품 등 심미적 가치 중심의 소비 |
| **J (계획)** | `Schedule_Drift_Value` | 정해진 일과표(Work-Rest)를 엄격하게 준수함 |
| **P (인식)** | `Random_Event_Probability` | 일과 외 돌발 행동이나 희귀 아이템 채집 변수 발생 |

### 2.2. 전략적 가격 탐색 로직 (Decision Logic)
* [cite_start]**메커니즘:** 유저가 상점 슬롯에 입력한 `DisplayPrice` 필드 값은 NPC의 MBTI 성향 파라미터와 대조되어 구매 여부를 결정함[cite: 198, 200].
* [cite_start]**공학적 구현:** NPC AI는 시스템 기준가(`IdealPrice`)와 유저 책정가(`DisplayPrice`)를 비교하며, 성향별 가중치(`MBTISensitivity`)를 필터로 사용하여 구매 확률을 연산함[cite: 196, 200].

## 3. 인사 관리(HR) 및 수집 시스템
### 3.1. 모듈형 캐릭터 및 T.O 관리
* **모듈형 생성:** 외형은 헤어, 눈, 옷 등 파츠 조합으로 생성되어 리소스 효율성을 확보함.
* **전략적 영입:** 플레이어는 섬의 발전 방향(미식/문화/기술 등)에 맞춰 효율적인 NPC를 선발하고 관리하는 인사 관리의 재미를 느낌.

### 3.2. 히든 청사진 (Hidden Blueprints)
* **개념:** 모든 NPC는 영입 시 비공개된 고유 기술(청사진)을 보유하고 있음.
* **해금 조건:** NPC와의 친밀도(Friendship) 수치가 최대치에 도달할 경우, 특수 에셋이나 제작법(예: 다이너마이트 제작법)이 해금됨.
* **수집 요소:** 원하는 기술을 얻기 위해 NPC를 면접 보고 영입하는 수집 및 가차(Gacha) 요소로 작동함.

## 4. NPC 대화 및 관계 시스템
* [cite_start]**MBTI 대응 대사:** 성향에 따라 대사 톤과 관심사가 변동됨[cite: 144].
* [cite_start]**사례:** 논리적인 T형 NPC는 경제적 효율성을, 감성적인 F형 NPC는 섬의 분위기와 친밀도를 주제로 대화함[cite: 145].