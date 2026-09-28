# VISUAL_GAP_AND_PLACEHOLDER_PLAN — 남은 시각 격차와 placeholder 대체 계획

작성: 2026-07-12 (Claude Fable 5)
원칙: 격차는 "게임이 미완성으로 보이게 하는 순서"로 정렬한다. 대체는 전부 기존 에셋 우선.

## 1. 현재 placeholder 인벤토리

| placeholder | 현재 상태 | 대체 후보 (기존 에셋) | 난이도 |
|---|---|---|---|
| 채집 포인트 작물 구체 | 궤짝+색 구체+아이콘 빌보드 (이번 패스) | `Item.model` 실제 드롭 모델로 교체 (`Resources.Load<Item>` 경유라 런타임 가능) | S |
| 벤치/화단/가로등 | primitive 조합 (이번 패스) | Nature Pack `Flowers/Bush/Grass` FBX + `WoodLog` — **씬 편집 필요** (Resources 밖이라 런타임 로드 불가) | M |
| 지면 맨땅 | 단색 지형 | 광장 바닥 돌길(플레인+타일 머티리얼) 또는 Nature Pack Grass 산포 | M |
| VC-001A 가공품 코너 | primitive 큐브 조합 | `B06_KitchenStation` 프리팹 비주얼 차용 (BuildingData 경유 런타임 로드 가능) | S |
| 폰 UI (분홍 DT 패널) | 기능 우선 스타일 | 상점 팔레트(크림/목재 톤) 리스킨 | S |

## 2. 런타임 로드 가능 여부 (중요 제약)

- **가능**: `Assets/Resources/**` 의 Item/BuildingData/Recipe/NPC 프로필과 그들이 직렬화 참조하는 프리팹·아이콘·모델.
- **불가능**: `Assets/Art/Ultimate Nature Pack/**` FBX, `Assets/Prefabs/*.prefab` (Resources 밖) — 이걸 쓰려면 **에디터 툴로 씬에 정적 배치 + 씬 백업 + 사람 승인** 경로여야 한다 (T010/T011 선례).

## 3. 우선순위 큐 (다음 3회 작업 제안)

1. **[S] 채집 포인트 작물을 `Item.model` 실제 모델로** — 런타임만으로 가능, 즉시 품질 상승.
2. **[M/승인 필요] Nature Pack 광장 식생 배치 에디터 툴** — 씬 백업 후 나무/수풀/꽃 정적 배치, NavMesh 재베이크 포함. 사람 승인 후 착수.
3. **[S] "Village direction" 요약 섹션 제목 한국어화** — `PA_FinalDemoRouteValidator` 의 해당 Contains 검증 1줄 동기화 필요.

## 4. 하지 않기로 한 것

- 외부 에셋/패키지 도입 (금지 규칙).
- 메인 씬 대수술, 기존 상점/경제/NPC 시스템 재작성.
- 이미지 레퍼런스의 1:1 복제 (인상만 재현: 광장 중심·천막 상점·정돈된 HUD).
