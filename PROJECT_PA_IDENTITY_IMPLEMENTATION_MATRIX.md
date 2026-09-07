# Project P.A. Identity Implementation Matrix

- 기준: `milestone/gameplay-beta-85@29fb98f400b5`
- 목적: 정체성 요소가 기획, 코드, 현재 제품 진입점, 3~5분 데모에서 각각 어느 수준인지 분리한다.
- Build Settings 진입점: `Assets/Scenes/Prototype_FirstDay.unity` 단일 활성
- 최신 M85 기능 씬: `Assets/Scenes/WorldSandbox.unity` — 일반 진입점에서 `UNREACHABLE`

## 1. 요약

| 영역 | 시스템 보유 | 런타임 연결 | 일반 진입점 노출 | 정체성 전달 |
| --- | --- | --- | --- | --- |
| 낮 생활 | 높음 | 높음 | 중간 | 높음 |
| 상점·가격 | 높음 | 높음 | 높음 | 중간 |
| NPC 소비 | 높음 | 높음 | 높음 | 중상 |
| NPC 생산·매입 | 높음 | 부분 | 낮음 | 낮음 |
| 채용 | 높음 | sandbox 검증 | 없음 | 낮음 |
| 위임·자동화 | 기반 존재 | 부분 | 없음 | 매우 낮음 |
| 마을 변화 | 높음 | 부분/기존 검증 | 결산 신호 중심 | 중간 |
| 캐릭터 | 역할 데이터 높음 | 부분 | 낮음 | 매우 낮음 |
| 장기 성장 | 설계 높음 | 정적/부분 | 3~5분에 없음 | 낮음 |

핵심 결론: **코드는 원안에 가깝지만, 현재 관람객이 보는 경험은 원안에서 멀다.**

## 2. 상세 매트릭스

| 정체성 요소 | 중요도 | 원래 의도 | 현재 코드·데이터 | Prototype 노출 | WorldSandbox 노출 | 검증 상태 | 핵심 Gap | 졸업 데모 조치 |
| --- | :---: | --- | --- | --- | --- | --- | --- | --- |
| Pioneer Assistance/개척자 지원 | Critical | 주민에게 도구·물자를 공급해 생산과 정착을 돕는다 | 주민 재료 요청, 생산자 거래, P.A. Phone, Tier/감사 존재 | 첫 이주민 브리핑 중심, 지원 거래는 약함 | 역할/시설/요청이 더 많이 연결 | `PARTIAL` | 왜 P.A.인지 플레이 행동으로 설명되지 않음 | 첫 문장과 첫 거래에서 “주민 생산 지원” 명시 |
| 직접 코지 생활 | High | 원안에서는 초반 생존 단계, 현재는 핵심 재미 절반 | 채집·낚시·광질·농사·활동 HUD | 일부 채집/Day 1 경로 | 네 활동 검증 | Prototype `CONNECTED`, M85 `VALIDATED/UNREACHABLE` | 이것만 보이면 일반 생활 시뮬레이션 | 20~30초만 사용하고 생산자 매입으로 전환 |
| 낮→밤 리듬 | Critical | 생활 준비와 상점 운영의 교대 | DayNightShopLoop, ShopOpenSign, GameClock | 실제 개점·결산 경로 | generated world에 연결 | 기존 validator `VALIDATED` | 원안 경제 축이 없는 리듬만 남을 수 있음 | 경제 가치사슬을 낮 준비에 삽입 |
| 진열·가격·상품 구성 | Critical | 유통 마진과 고객 분석의 핵심 | ShopSlot, ShopPriceUI, 추천가, 품질, merchandising | 가장 안정적으로 보임 | BETA-004 확장 | Prototype/BETA `VALIDATED` | 단독으로는 일반적인 상점 게임 | 반드시 생산자 출처와 판매 후 결과를 양쪽에 붙임 |
| NPC 소비·구매/거절 | Critical | 주민의 필요와 성향이 수요를 만든다 | PurchaseEvaluator, 8 NpcProfile, feedback/demand/sales log | 구매·거절 및 30G 판매 가능 | Miner/Tailor 대비 검증 | `VALIDATED` | 역할 NPC와 동일한 개인이라는 연결 약함 | 같은 상품의 고가 거절+적정가 구매 2반응 제시 |
| NPC 자율 생산 | Critical | 농부·광부·벌목꾼·어부가 1차 생산을 맡는다 | 4 Producer prefab, ProductionData, 일정, 내부 재고 | Day 1 핵심 화면에 없음 | 고용 역할/생산 anchor 연결 | 코드 `CONNECTED`, 전체 왕복 `PARTIAL` | 관람객은 상품을 누가 만들었는지 모름 | 고용 생산자 1명의 실제 작업/재고 증가를 보임 |
| 생산물 매입 | Critical | 플레이어가 주민에게 현금을 지불하고 원재료를 확보한다 | ProducerNpcController 거래, 안전한 가방/잔액 검사, BETA-008 `B` 매입 | 일반 Day 1에는 없음 | Day 2~5 매입 증거 | `PARTIAL`, `UNREACHABLE` | 역공급망을 정의하는 거래가 데모에서 빠짐 | 1회 유료 매입을 강제된 데모 비트로 지정 |
| 가공·부가가치 | Critical | 원재료를 직접 또는 전문가로 가공해 마진을 만든다 | 8 recipes, B05~B08, CraftingService, SpecialistNpcController | BreadLoaf 흐름은 있으나 원재료 출처가 약함 | BETA-003 5개 가공 검증 | Prototype 부분, M85 `VALIDATED/UNREACHABLE` | 제작이 단순 크래프팅처럼 보임 | 매입 Wheat→BreadLoaf 가격/가치 상승을 한 줄로 비교 |
| 유료 채용 | Critical | 인력·전문화를 선택해 직접 노동을 줄인다 | 후보 8명, 비용 300~700G, 실제 역할 prefab, roster/save | Phone 앱에 최신 경로 없음 | BETA-006 실제 비용·스폰 검증 | `VALIDATED`, `UNREACHABLE` | 모든 후보 Tier 0, 성장 단계 약함 | 생산자 1명 유료 채용을 데모 시작 비트로 고정 |
| 업무 위임 | Critical | 고용한 주민이 생산·가공 작업을 맡는다 | Producer/Specialist controllers, role/work anchors, schedules | 화면에 없음 | BETA-007 연결, hire→work 전체 미검증 | `PARTIAL` | 채용 카드 이후 “내 일이 줄었다”가 보이지 않음 | 고용 전 직접 1회, 고용 후 주민 1회로 전후 대비 |
| 자동화 | High | Tier 3 이후 시설·기계·계약으로 반복 노동을 안정화 | NPC 자동 생산/전문가 작업 기반, 상점 확장·키오스크는 기획 중심 | 없음 | 제한적 기반 | `PLANNED/PARTIAL` | 자동화 단계의 명시적 플레이·효율 지표 없음 | 현재 완성 기능으로 주장 금지; 엔딩 성장 방향으로만 설명 |
| 친밀도·주민 요청 | High | 관계가 공급 신뢰도·고용·기술 해금과 연결된다 | 대화 +2, 구매 +5, 요청 +4, 단계/청사진 구조, 저장 | 보리 친밀도 결산 | 역할별 요청·다음 날 반응 연결 | `PARTIAL` | 개인 캐릭터 설정 부재, 청사진 체감 부족 | 데모에서는 판매 후 주민 반응 1회만 확실히 표시 |
| 판매→마을 변화 | Critical | 판매 상품이 주민 생활·시설·풍경을 바꾼다 | 4 카테고리 pending/active, VillageCultureVisual, 정산 신호 | 정산의 village direction, 기존 next-day 검증 이력 | BETA-007 exact cause 연결 | `PARTIAL`; 전체 M85 왕복 미검증 | 판매 당일과 다음 날 인과가 한눈에 안 보임 | 전/후 고정 카메라 또는 즉시 다음 날 전환으로 1종 증명 |
| Tier·감사·상점 성장 | High | 생존자→지점장→관리자→사업가→파트너 | TierService, AuditService, 실내 확장, 진열 한도, 시설 해금 | 감사 앱/일부 상점 성장 | 장기 목표와 연결 | 과거 개별 validator 다수 PASS, 최신 전체 `PARTIAL` | 3~5분에 성장 사다리가 보이지 않음 | 데모 끝에 다음 단계 1장만 표시, 즉시 Tier 치트는 피함 |
| NPC 상점·납품·경쟁 | Medium/Long-term | 독점 상점이 아닌 마을 시장 생태계 | 현재 핵심 구현 증거 부족 | 없음 | 없음 | `PLANNED` | “유일한 잡화점”과 원안 생태계 사이 충돌 | 졸업 데모 범위 밖, 완성 게임 백로그 유지 |
| 개별 캐릭터 | Critical | 이름·성향·직업·관계·고유 기술이 한 주민으로 결합 | 역할 프로필 8, 이름 있는 미사용 프로필 5, 보리 키 존재 | 보리 이름만 강하게 노출 | 역할명 중심 | `IDENTITY GAP` | 개인 이름/약력 데이터가 실제 역할 prefab과 분리 | 새 이름 창작 없이 보리 1명부터 데이터 통합 결정 |
| 마을/섬 개척 공간 | High | 경제 성장에 따라 시설·구역·생활 공간 확장 | 절차 섬, Terraform, 건물/가구 배치, navigation | 고정 Prototype | M70 WORLD-001~010 | `VALIDATED`, `UNREACHABLE` | 경제 성장과 공간 변화가 제품 진입점에서 분리 | 3~5분에는 한 광장만 사용, 대형 월드 통합은 별도 Gate |
| 장기 캠페인 | High | Tier 0→4, 직업 조합, 감사, 무역항 | Day 1~129 목표·체크리스트 다수, 실제 상태 참조 | Day 1 중심 | Week 1 일부 실제 진행 | 정적 계약 다수, 실제 연속 플레이 `PARTIAL` | 콘텐츠 길이와 런타임 검증이 불균형 | 발표에서는 방향만 설명, 장기 완주 주장 금지 |
| 저장·복구 | Product Critical | 장기 경제와 고용·관계·공간을 보존 | Save v12 gameplay envelope | 동일 세션 복원 일부 증거 | 재시작 복원 검사 | `BLOCKED` — facingError 137° | 최종 제품 신뢰성과 M85 회귀 차단 | BETA-010 해결 전 standalone 완성 주장 금지 |

## 3. 핵심 축 추적

| 축 | 현재 가장 가까운 구현 | 현재 가장 큰 단절 | 감사 판정 |
| --- | --- | --- | --- |
| 개척자 지원 | 주민 요청, 생산자 매입, P.A. Phone | 데모 서두에서 의미 설명 없음 | `PARTIAL` |
| 상점 운영 | 진열, 가격, 개점, 판매, 정산 | 과도하게 전면화되어 다른 축을 가림 | `VALIDATED` |
| NPC 생산/소비 | Producer/Specialist + PurchaseEvaluator | 동일 주민의 양면 행동이 이어져 보이지 않음 | `PARTIAL` |
| 채용 | 후보 8, 실제 비용·스폰 | 일반 진입점 접근 불가 | `VALIDATED/UNREACHABLE` |
| 위임/자동화 | 작업 컨트롤러와 일정 | 고용 전후 노동 감소·효율 지표 없음 | `PARTIAL/PLANNED` |
| 마을 경제 성장 | Tier, 감사, 상점 확장, 마을 변화 | 3~5분 안의 단일 결과선 부재 | `PARTIAL` |

## 4. Demo Critical Matrix

| 반드시 보여야 하는 것 | 현재 근거 | 남은 통합 필요 | 실패 시 정체성 손실 |
| --- | --- | --- | --- |
| 생산자 또는 전문가 유료 고용 1회 | BETA-006 | 일반 진입 경로/데모 상태 | 관리자·HR 판타지 소실 |
| 주민 생산물 유료 매입 1회 | BETA-008 Day 2~5 | 3~5분 압축·안정화 | 역공급망 소실 |
| 원재료→가공품 가치 상승 1회 | BETA-003, BreadLoaf | 매입 출처와 연속 연결 | 부가가치 판타지 소실 |
| 가격 결정과 NPC 구매/거절 | Prototype/BETA-005 | 두 반응의 빠른 보장 | 상점 판단의 전략성 소실 |
| 정산→다음 날 마을 변화 | VillageCulture/BETA-007 | same-run 인과 검증 | 현재 대표 차별점 소실 |
| 고용 후 반복 준비 감소 신호 | 컨트롤러 기반 | UI/목표 전후 비교 | 성장 판타지 소실 |

## 5. Demo Important / Optional / Out of Demo

### Important

- 1920×1080 UI safe area와 고정 카메라 가독성
- 같은 NPC 이름이 채용 카드·월드 이름표·판매 피드·대화에서 일치
- 돈, 재고, 생산자 재고, 판매 결과의 즉시 변화
- 30초 이내 역할 설명, 5분 이내 종료점
- 실패 대비 녹화본과 즉시 Reset 가능한 데모 상태

### Optional

- 네 가지 낮 활동 전부
- 다수 상품 카테고리와 여러 마을 변화
- 여러 명 채용
- 상점 인테리어 배치와 추가 merchandising
- 장기 통계 앱 전체

### Out of Demo

- 절차 월드 전체 소개와 Terraform 기능 전시
- Tier 0→4 실제 완주
- NPC 상점 경쟁·무역항·멀티플레이
- 대규모 캐릭터 DB와 모듈형 주민 생성
- 완성되지 않은 자동화 기계

## 6. 현재 주장 가능 범위

### 안전하게 주장 가능

- NPC가 상품 가격·품질·카테고리·성향에 따라 구매 또는 거절한다.
- 생산자 4직군과 전문가 4직군의 데이터와 역할 컨트롤러가 있다.
- 휴대폰에서 8개 역할 후보를 비용을 지불해 고용하는 경로가 검증되었다.
- 판매 카테고리를 다음 날 마을 변화와 연결하는 시스템이 있다.
- 현재 일반 빌드 진입점에서는 Day 1 상점 판매·정산 루프가 가장 안정적이다.

### 조건을 붙여야 함

- 생산자 매입, 전문가 위임, 7일 성장, 주민 다음 날 반응은 일부 실플레이 또는 정적 검증만 완료되었다.
- 최신 M85 통합 기능은 WorldSandbox 전용이며 일반 빌드에서 접근할 수 없다.
- 저장 v12는 player facing 복원 blocker가 남아 있다.

### 주장 금지

- 고용과 자동화가 완성 게임 수준으로 모두 연결되었다.
- 8명의 주민이 완성된 고유 인물 설정과 서사를 가진다.
- 일반 실행만으로 WorldSandbox의 전체 루프를 플레이할 수 있다.
- Day 1부터 Tier 4까지 연속 플레이가 런타임 검증되었다.
