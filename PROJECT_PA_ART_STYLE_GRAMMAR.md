# Project P.A. Art Style Grammar

ART-000 · 2026-09-06 · 원본 실측과 비교 렌더에 근거한 제작 규칙 v1.

낮의 생활과 주민 생산, 밤의 상품 선택, 다음 날의 마을 변화가 같은 공간의 일로 읽혀야 한다. 건축·가구는 Kenney Mini의 두꺼운 판과 둥근 모서리, 자연은 Quaternius Nature의 큰 수관 덩어리와 비대칭을 참고한다. 마지막 색·재질 판단은 Project P.A.의 기존 팔레트와 캐릭터에 맞춘다. 이번 문서는 기존 캐릭터, 콘텐츠 캐논, WorldGrid 지형이나 게임 기능을 변경하지 않는다.

측정 근거: [33종 측정표](Docs/AssetProvenance/STYLE_MEASUREMENTS.md), [전체 모델 목록](Docs/AssetProvenance/MODEL_INVENTORY.md), [비교 렌더와 관찰](Docs/AssetProvenance/PREVIEW_REVIEW.md), [Unity 크기·피벗](Docs/AssetProvenance/unity-validation.json), [Blender 재개방·색상 표본](Docs/AssetProvenance/library-reopen-validation.json).

## 1. 관찰한 형태와 제작에 적용할 규칙

| 항목 | 선정 원본에서 확인한 내용 | Project P.A. 제작 규칙 |
|---|---|---|
| Polygon density | 원본 OBJ surface area당 triangles와 팩별 평균을 측정표에 기록했다. FBX/BLEND/OBJ의 triangulation과 축·단위가 서로 다르다. | 원본 density 평균을 최종 예산으로 복사하지 않는다. 정규화된 크기·화면 점유와 실루엣을 먼저 보고 triangles를 배정한다. |
| Silhouette complexity | 계산대는 큰 기단·상판·등록기, 나무는 줄기와 2~4 수관, 물고기는 몸통·눈·지느러미로 구분된다. | 한 기능을 보여 주는 주 형태 1개와 보조 덩어리 2~4개. 가격표·상품·빈 칸이 우선 보이게 한다. |
| Bevel / edge softness | 계산대와 상자는 적용된 chamfer와 둥근 외곽이 보인다. 표본의 live Bevel modifier 수는 모두 0이며 이는 bevel geometry가 없다는 뜻이 아니다. | 새 가구 주요 모서리는 1~2 segment chamfer, 폭은 부품 두께의 5~12%를 초기값으로 한다. 작은 소품은 실루엣에 필요한 곳만 bevel. 이 수치는 제작 제안이며 원본 측정치가 아니다. |
| Primitive proportion | 판·상자·원통 조합이 가구의 주 구조다. FoodKit은 낮은 각수의 회전체, Nature는 불규칙한 덩어리, Fish는 길쭉한 몸통이다. 원본에 primitive 생성 이력이 없어 정확한 primitive 비율은 확인 못 함. | 가구는 직육면체/판, 병·식기는 8~12각 회전체를 시작점으로 사용한다. 자연에 정육면체 반복을 강제하지 않는다. |
| Object proportion | [Unity 규격표](Docs/AssetProvenance/SELECTED_ASSET_SPECIFICATIONS.md)에 실제 wrapper X/Y/Z를 기록했다. | 상품·손잡이·표지가 멀리서 읽히도록 두께를 보정한다. 표식만 과장하고 기존 캐릭터 신체/건물 footprint는 보존한다. |
| Surface shading | Kenney는 smooth 면과 단순 외곽을 함께 사용한다. Tuna·CommonTree 표본은 flat 면이다. | flat/smooth를 전 팩에 일괄 적용하지 않는다. 가구 넓은 면은 평탄, bevel 전이만 부드럽게; 자연 수관·바위는 큰 facet 유지. |
| Texture dependence | Kenney 선정 모델은 팩별 512×512 colormap, Cube는 32×32 Atlas에 의존한다. Nature/Fish 선정 원본은 주로 단색 material이다. | geometry 색 분할·palette UV·공유 material을 우선한다. 같은 이름의 다른 팩 colormap을 합치지 않는다. 사진 PBR 디테일·과도한 노멀맵을 넣지 않는다. |
| Color count / saturation | UV 정점이 참조한 텍스처색 또는 단색 material의 고유색과 HSV 채도를 측정했다. 조명 반영 화면의 색 수나 텍스처 전체 색 수와 다르다. | 오브젝트당 주색 2~4, 작은 강조색 1~2를 우선한다. 상품의 식별색을 자연/배경보다 선명하게 하되 원본 채도를 일괄 낮추지 않는다. |
| Roughness / metallic | Kenney source Principled roughness 1.0, Quaternius 선정 표본 0.5, metallic 모두 0. Unity Intake 래퍼는 임시 source-look smoothness 0.35/metallic 0. | ART-001 목표는 기본 roughness 0.65~0.9, metallic 0. 조리기구·잠금쇠도 작은 하이라이트로만 구분한다. Blender roughness와 Unity smoothness는 1-r 관계를 의도하되 shader 응답 차이를 화면에서 확인한다. |
| Shadow behavior | Cycles 비교 렌더에서 접지, 부두 기둥, 지느러미가 읽힌다. | 실제 URP 낮/밤 그림자와 이중면·shadow bias는 ART-001에서 검사. 얇은 식기/식물은 silhouette가 깨지지 않을 때만 backface culling을 적용한다. |

## 2. 공통 팔레트

기준은 동결 가이드 `Docs/08` §2의 색이다. 아래 값은 ART-001의 새 material palette 작업을 위한 계약이며 이번에 최종 material로 적용한 값이 아니다.

| 역할 | HEX | 적용 |
|---|---|---|
| 자연 기본 | `#8DB87A` | 풀·수관 주색; 나무별 명도 차이 유지 |
| 흙/모래 | `#D4B896` | 발밑과 농지 경계; 실제 WorldGrid surface와 조정 |
| 나무 가구 | `#A07850` | 상점·수납·작업대의 공통 목재 |
| 지붕/작업 강조 | `#D4714A` | 시설 구분·작은 표식 |
| 하늘 | `#B0D8E8` | 환경 조명의 참고색 |
| 물 | `#5BAFC0` | 기존 물 surface와 낚시 가독성 |
| 플레이어/상호작용 | `#F08070` | 작은 상호작용 강조; 기존 캐릭터 색 보존 |
| 선택/가격 강조 | `#F5D76E` | 선택·금전·P.A. 표지의 제한된 포인트 |

물건의 원재료색은 유지한다. Carrot의 주황색, Wheat의 황금색, Fish의 청회색을 모두 같은 목재색으로 통일하지 않는다. Production 재고·ShopSlot 상품 수는 실제 상태로 표시하고, 장식용 완제품 더미로 채우지 않는다.

## 3. 크기·방향·피벗 계약

- Unity 1 unit=1m, 현재 WorldGrid cell=2m. 원본 좌표는 변경하지 않고 PA visual child에 transform을 적용한다.
- 새 standalone prop의 root는 local `(0,0,0)` 바닥 중앙이다. ART-000 래퍼 28종은 오차 0.005m 이내로 검증했다.
- Blender authoring up은 +Z, Unity up은 +Y다. 기존 FBX importer의 축 변환을 그대로 쓴다. 상호작용 대상의 목표 face는 Unity -Z이며 `InteractionAnchor`가 실제 owner의 접근면을 가리킨다.
- 현재 래퍼는 원본 회전을 보존했다. 시각 앞면이 -Z라는 최종 보증은 없다. 계산대의 운영자면/고객면, Chest 잠금쇠면, 낚싯대 끝 방향은 ART-001/003/004에서 명시적으로 정한다.
- 상품 초기 크기는 0.15~0.35m, 작업면 높이는 약 1m, 가구는 1~2m, 나무는 약 4m를 시작점으로 삼는다. 개별 실제 값은 선정 규격표가 우선하며 씬/기존 BuildingData를 자동 변경하지 않는다.
- 바닥 중앙만으로 placement가 완성되지 않는다. collider, grid footprint, NavMesh obstacle, clearance, interaction/save identity는 기존 ShopSlot/Workbench/StorageBox/WorldBuildingPlacementService가 소유한다.

## 4. 초안 예산과 합격 기준

초기 제작 예산은 단순 상품 200~800 triangles, 작은 소품 300~1,500, 판매대/가구 800~3,000, 일반 나무 1,000~4,000이다. 측정한 기존 원본 중 예산을 넘는 항목을 자동 decimate하지 않는다. 최종 카메라 크기와 동시 렌더 수에 따라 후속 티켓에서 조정한다.

일반 화면에서 원료/가공품, 빈/가득 찬 재고, 작업 중/작업 가능, 판매대/장식 가구가 구분되어야 한다. 새 mesh는 소유한 gameplay 기능 때문에 필요한 변경을 갖춰야 하며 단순 rename은 파생 제작으로 세지 않는다.

ART-001에서는 격리 Unity preview의 낮/밤, 1920×1080와 실제 카메라 거리, 앞/옆/뒤, 비균일 scale, 얇은 면, material 중복과 texture dependency를 검사한다. 사람의 최종 시각 승인과 실제 demo scene 적용은 아직 확인 못 함이다. ART-000은 측정·입고·제작 계약 단계다.
