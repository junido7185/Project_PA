# Tripo3D 임시 에셋 감사

- 최초 감사: 2026-07-15
- 최신 대조: 2026-08-04
- 상태: 전수 인벤토리·실사용 참조·기능/격자 계약 대조 완료. B01 중앙 상점·B02~B04 상점 진화·B09 생활형 외부 창고·B10 Cottage 정면/출입문·B05 목재 가공 작업대·C-01~C-09 Unity 설정/접지/보행은 1차 최종화 완료. B06은 기존 Visual을 보존한 채 렌더러 bounds 기반 축소 전용 물리/Carving 정합, 명시적 전면 interaction anchor, 제작 성공 모델 pulse를 구현했다. B11은 원형 Visual mesh 물리 보정, B12는 실제 Visual bounds보다 큰 역사적 Box/Obstacle 축소 보정을 구현했다. B06/B11/B12는 실제 이동·동일 게임 카메라 확인이 남았고 B07~B08은 시각 최종화 대기다. 라이선스 미확인 Froggy Chair는 런타임 노출을 제거했지만 Resource/원본 보존 때문에 최종 빌드 배포 게이트는 유지한다.
- 주의: `Tripo 추정`은 파일명·기존 문서·프로젝트 사용 방식에 따른 판정이다. 에셋별 생성 계정/라이선스 증빙이 발견되기 전에는 확정 출처로 보지 않는다.

## 분류 코드

1. 그대로 유지
2. Unity 설정만 수정
3. Blender 등에서 모델 일부 수정
4. 재질과 텍스처 재작업
5. 기존 모델 기반 재구성
6. 다른 기존 에셋으로 교체
7. 완전히 새로 제작
8. 최종 판단을 위해 사용자 확인 필요

## 장기 감사·최종화 정책

“임시 에셋”은 삭제 사유가 아니다. 모든 Tripo 추정 에셋은 실제 기능, 게임 카메라 노출, 형태·재질 일관성, 물리·상호작용, 출처를 대조한 뒤 위 1~8 중 하나로 개별 판정한다. 각 행은 에셋 경로, 사용 씬/프리팹, 현재 역할, Tripo 추정 근거, 문제, 판정, 수정 방법, 최종화 상태, 원본 보존 위치, 라이선스, 사용자 확인 필요 여부를 유지한다.

- 플레이어와 주민은 정체성 보존이 기본이다. 심각한 비율, 손상 메시/텍스처, 잘못된 리깅·스키닝, 애니메이션 적용 불가, 회복 불가능한 스타일 불일치, 라이선스 문제가 확인되지 않으면 전체 외형을 교체하지 않는다.
- 캐릭터는 Unity의 스케일·부모 피벗·접지·Humanoid Avatar·Animator 속도·재질·그림자·LOD·Collider·NavMeshAgent·상호작용 기준점을 먼저 보정한다. 메시/UV/노멀/웨이트 자체가 원인일 때만 원본과 분리된 Blender 수정본을 검토한다.
- 창고와 작업대는 외형만 유지하는 것이 목표가 아니다. 저장/가공/진열 준비라는 실제 역할, 문·상판·수납, 플레이어 사용 방향, NPC 접근, collider, footprint/clearance/interaction, 저장 상태가 일치해야 한다. 기능 맥락이 틀리면 기존 모델 기반 재구성 또는 교체를 허용한다.
- 신규 에셋은 `ART_DIRECTION.md`와 `PLACEABLE_ASSET_GUIDE.md`를 먼저 적용한다. 원시 큐브 조합, 무작위 소품, 가짜 손잡이·문·도구, 서로 다른 생성 스타일의 혼합은 최종화로 인정하지 않는다.
- 우선순위는 플레이어/NPC 접지와 보행 → 핵심 동선의 보이지 않는 충돌 → 상점 외관/실내 구조 → 기능 가구와 상품 → 진화/조명/카메라/UI → 배경 장식 순이다. 핵심 게임 루프 구현을 멈추고 전 에셋을 일괄 재작업하지 않는다.
- 게임 정체성을 바꾸는 플레이어/주민 전체 교체, 핵심 건물 역할 변경, 대량 삭제, 유료 서비스, 불명확한 라이선스 사용만 사용자 확인 대상으로 남긴다. 나머지 명백한 설정·물리·접지 문제는 기존 구조 안에서 직접 보정한다.
- 주요 시각 판정은 같은 게임 카메라의 Before/After를 비교한다. 스케일, 겹침, 동선, collider, 조명, 팔레트, UI, 개발용 흔적을 확인하지 못한 항목은 “시각 최종화 대기”로 유지한다.

## 인벤토리 요약

- 전체 모델: FBX 174, OBJ 150, GLB 0, Blend 0.
- Nature Pack: 모델 300개. Tripo 대상이 아니며 별도 라이선스 파일이 있다.
- 캐릭터: C-01~C-09, Chop, Tripo walking 변환 FBX.
- 건물/기능물: B01~B12 FBX와 대응 프리팹/BuildingData.
- Nature Pack을 제외한 프로젝트 고유 FBX는 24개다: C-01~C-09, `Chop`, Tripo walking 변환, B01~B12, Froggy Chair.
- C-01~C-09의 FBX importer `.meta`에는 `tripo_node_*` 본 이름이 남아 있어 Tripo 계열 판정의 직접 저장소 증거가 있다. B01~B12는 `Docs/Tripo3D_빌딩_생성_프롬프트.md`와 발표/기능 문서의 Tripo 제작 파이프라인을 근거로 “높음”을 유지하되, 개별 생성 계정·생성일·상업 이용 증빙과 동일시하지 않는다.
- 메인 씬 직접 사용: `PlayerModel_C01`, NPC 8종, `Workbench_Basic/Kitchen/Forge/SewingTable`, B09~B12. `Prototype_FirstDay.unity`는 Unity 6 바이너리 직렬화 파일이므로 YAML 추측 대신 읽기 전용 문자열 색인으로 이름/컴포넌트를 확인했다.
- 런타임/Editor 경로 로드: C-01~C-09. `PA_PlayableDayBuilder`/`PA_DevConsole`이 모델 경로로 구성하며, 캐릭터 FBX GUID의 씬 직렬화 참조가 없는 것은 미사용을 뜻하지 않는다.
- B01~B12 FBX GUID는 각각 같은 이름의 래퍼 프리팹이 참조하고, 래퍼는 `Resources/Buildings/Building_Bxx.asset`과 대응 청사진이 참조한다. B02~B04는 별도 visual-only 파생 프리팹도 참조한다.
- 별도 원본 `.blend`/GLB는 발견되지 않았다. 현재 FBX와 `.fbm/Color.jpg`가 저장소 안의 보존 원본이며, 생성 계정·생성일·다운로드 원본 묶음은 별도 증빙이 필요하다.

## 캐릭터

| 에셋/경로 | 사용처와 역할 | Tripo 추정 | 현재 문제 | 결정/방법 | 상태 | 원본·라이선스 | 사용자 확인 |
|---|---|---|---|---|---|---|---|
| `Assets/Art/Character/C-01.fbx` | `Prototype_FirstDay/Player/PlayerModel_C01`, 플레이어 | 높음 | 5m/s 이동 대비 Walk 1× 재생으로 발미끄럼 가능, 생성 증빙 없음 | **2**. 외형·Humanoid Avatar 유지, 기존 IK 컴포넌트에 Walk 재생 속도 동기화 추가 | 메시/Avatar/1.85m 접지 및 2.25× 보행 PASS | 원본 동일 경로 보존, 라이선스 미확인 | 전체 외형 교체 시에만 필요 |
| `Assets/Art/Character/C-02.fbx` | 런타임 기본 주민 후보. 현재 감사한 메인 씬 8명에는 실제 Avatar 사용 없음 | 높음 | 역할 기대와 실제 씬 할당이 불일치, 생성 증빙 없음 | **2 + 8**. 외형·Avatar 유지. 실제 주민 매핑은 별도 사용자 확인 후 정리 | 원본 Humanoid/메시 PASS, live 할당 보류 | 원본 보존, 미확인 | 역할 할당 확인 필요 |
| `Assets/Art/Character/C-03.fbx` | 현재 `NPC_Bori_FirstSettler`; 코드상 Lumberjack 후보 | 높음 | 광부형 헬멧 외형과 현재 Bori/코드 역할이 불일치 | **2 + 8**. 모델은 유지하고 1.75m 접지/물리 설정만 보정, 역할 재매핑은 보류 | idle 높이/접지/Avatar PASS | 원본 보존, 미확인 | 역할 외형 판단 필요 |
| `Assets/Art/Character/C-04.fbx` | 현재 `NPC_Miner`; 코드상 Miner | 높음 | 밀짚모자·멜빵 외형 때문에 C-03과 역할 인상이 뒤바뀐 가능성 | **2 + 8**. 모델 설정만 보정하고 C-03/C-04 역할 판단은 보류 | idle 높이/접지/Avatar PASS | 원본 보존, 미확인 | 역할 외형 판단 필요 |
| `Assets/Art/Character/C-05.fbx` | 현재 `NPC_Farmer`와 `NPC_Fisher`가 중복 사용 | 높음 | 서로 다른 두 역할이 같은 모델을 사용, 생성 증빙 없음 | **2 + 8**. 현 외형을 임의 교체하지 않고 설정 보정; C-02 포함 재할당은 별도 확인 | 두 인스턴스 idle 높이/접지/Avatar PASS | 원본 보존, 미확인 | 역할 중복 해소 시 필요 |
| `Assets/Art/Character/C-06.fbx` | 현재 `NPC_Chef` | 높음 | 별도 basecolor 명명과 생성 증빙 미확인 | **2**. 현재 재질 연결과 외형 유지, 높이·접지·그림자 보정 | idle 높이/접지/Avatar PASS | 원본 보존, 미확인 | 아니오 |
| `Assets/Art/Character/C-07.fbx` | 현재 `NPC_Blacksmith` | 높음 | 생성 증빙 없음 | **2**. 외형 유지, 높이·접지·그림자·Agent 보정 | idle 높이/접지/Avatar PASS | 원본 보존, 미확인 | 아니오 |
| `Assets/Art/Character/C-08.fbx` | 현재 `NPC_Tailor` | 높음 | 생성 증빙 없음 | **2**. 외형 유지, 높이·접지·그림자·Agent 보정 | idle 높이/접지/Avatar PASS | 원본 보존, 미확인 | 아니오 |
| `Assets/Art/Character/C-09.fbx` | 현재 `NPC_Carpenter` | 높음 | 생성 증빙 없음 | **2**. 외형 유지, 높이·접지·그림자·Agent 보정 | idle 높이/접지/Avatar PASS | 원본 보존, 미확인 | 아니오 |
| `tripo_convert_...@Walking.fbx` + `Walk.anim` | 플레이어 공용 보행 소스 | 확정에 가까움 | Walk 2.375초/평균 속도 1.174m/s와 게임 이동 속도 불일치 | **2**. 원본 클립·Avatar는 보존하고 런타임 재생 속도만 제한 동기화 | D3D11 실제 5m/s/2.25× 재생·접지 PASS | 원본 보존, 라이선스 미확인 | 심각한 리깅 문제일 때만 |
| `Chop.fbx`, `Axe.anim`, `Idle.anim`, `Sit.anim` | 활동/대기 애니메이션 | 혼합 | 실제 역할별 적용 범위 미확인 | **2**. 기존 Animator 재사용 우선 | 인벤토리 완료 | 원본 보존 | 아니오 |

현재 캐릭터는 스크린샷에서 월드와 대체로 일관된 둥근 형태 언어를 가진다. 심각한 메시 손상이나 회복 불가능한 스타일 불일치는 관찰되지 않아 전면 교체하지 않는다.

## 건물·상점·작업대

| 에셋/경로 | 사용 씬/프리팹과 역할 | Tripo 추정 | 문제 | 결정/방법 | 최종화 상태 | 출처/확인 |
|---|---|---|---|---|---|---|
| `B01_MarketStall.fbx` / `B01_MarketStall.prefab` | 첫 노점, Shop 4슬롯 | 높음 | 메인 데모 중앙은 이 모델 대신 큰 큐브 조합 사용 | **1+2**. B01 Visual을 기존 ShopSlot에 정렬, 임시 Renderer/Collider 비활성화 | **1차 최종화 완료**. MCP/Play 캡처 확인 | 원본 보존, 라이선스 증빙 필요 |
| `Assets/Models/Buildings/B02_GeneralStore.fbx` / `Assets/Prefabs/Buildings/B02_GeneralStore.prefab` | Tier 1 잡화점 외관. `ShopEvolutionController`가 visual-only 파생 프리팹 사용 | 높음 | 실제 정면은 로컬 `-X`이나 기존 래퍼 슬롯은 `-Z`를 전제로 함. 래퍼 전체 사용 시 Shop 1개와 슬롯 8개가 기존 실내와 중복 | **2**. 원본 Visual만 파생하고 정확한 콜라이더·Obstacle·입구·간판 기준점을 Unity에서 구성 | **1차 최종화 완료**. Tier 1 실제 출입/캡처/배치 보존 PASS | 원본 FBX/래퍼 보존, 생성 증빙 필요. 외형 교체 확인 불필요 |
| `Assets/Models/Buildings/B03_ConvenienceStore.fbx` / `Assets/Prefabs/Buildings/B03_ConvenienceStore.prefab` | Tier 2 마켓 외관. 민트 차양과 중앙 이중문 사용 | 높음 | 래퍼 전체 사용 시 Shop 1개와 슬롯 16개가 중복되고, 래퍼 콜라이더가 보이는 모델보다 큼 | **2**. 4면/게임 카메라 감사에서 코지 스타일 적합 확인. Visual-only 파생, 정확한 물리 범위와 정면 기준점 구성 | **1차 최종화 완료**. Tier 2 스케일·입구·간판·배치 보존 PASS | 원본 FBX/래퍼 보존, 생성 증빙 필요. 스타일 변경 확인 불필요 |
| `Assets/Models/Buildings/B04_DepartmentStore.fbx` / `Assets/Prefabs/Buildings/B04_DepartmentStore.prefab` | Tier 3 이상 상점 외관. 맨사드 지붕의 코지 부티크 | 높음 | 래퍼 전체 사용 시 Shop 1개와 슬롯 32개가 중복되고, 기존 콜라이더가 실제 외관보다 크게 동선을 막음 | **2**. 마을 스케일을 해치지 않는 현재 실루엣 유지. Visual-only 파생, 모델 bounds 기반 물리 범위와 중앙 출입 기준점 구성 | **1차 최종화 완료**. Tier 3 스케일·카메라·출입·배치 보존 PASS | 원본 FBX/래퍼 보존, 생성 증빙 필요. 역할/외형 교체 확인 불필요 |
| `Assets/Models/Buildings/B05_Workbench.fbx` / `Assets/Prefabs/Buildings/B05_Workbench.prefab` | `Workbench_Basic`, 상점 실내 2×2 배치, Wood→Plank 목재 가공/상품 준비 | 높음 | 실제 모델은 정상이나 작업면이 기존 interaction `-Z` 반대쪽을 향함. 물리 콜라이더가 메시보다 넓고 입력·공정·출력 피드백이 없음 | **2+5**. 원본 외형을 유지하고 런타임 Visual을 180° 정렬, 물리 콜라이더/Obstacle 보정, 기존 모델 기반 파생 준비 키트와 성공 피드백 부착 | **1차 최종화 완료**. 실제 제작·2×2 배치·MainCamera 전후 캡처 PASS | 원본 FBX/래퍼 프리팹 보존. Tripo 생성 증빙 필요, 외형 교체 사용자 확인 불필요 |
| `Assets/Models/Buildings/B06_KitchenStation.fbx` / 대응 래퍼 | 메인 씬 `Workbench_Kitchen`, 기존 Kitchen 레시피, 상점 실내 Tier 2 배치 | 높음 | 래퍼의 3.4×2.8m 물리/Carving이 실제 Visual보다 큰 축에서 투명 벽을 만들 수 있었고 제작 성공 피드백이 B05 전용이라 Kitchen에는 전달되지 않았음. BuildingData의 역사적 `requiredTier=0`과 실제 배치 카탈로그 Tier 2가 다름 | **2 우선, 조건부 5**. 원본 Visual을 유지하고 런타임 렌더러 bounds보다 0.2m 이상 큰 X/Z 축만 0.16m 여유를 두고 축소한다. Box와 box형 Carving을 함께 정합하고, 로컬 `-Z` 앞에 interaction anchor를 두며 성공한 기존 제작 뒤 모델 자체를 0.72초/최대 3.5% pulse한다. 전용 파생 키트는 실제 캡처에서 목적 전달이 부족할 때만 검토 | **Unity 설정/피드백 구현, 실제 전면·통로·동일 카메라 확인 대기** | FBX·`.fbm/Color.jpg`·래퍼·재질 보존. 개별 Tripo 증빙 필요. 외형 교체 확인 불필요 |
| `Assets/Models/Buildings/B07_BlacksmithForge.fbx` / 대응 래퍼 | 메인 씬 `Workbench_Forge`, 기존 Forge 레시피, 상점 실내 Tier 1 배치 | 높음 | 3×2 크기와 전면 열의 통로, 열원 가독성, 코지 팔레트 대비를 게임 카메라로 검증하지 않음. 기본 5×4 실내에서는 빈 진열대 두 칸 이상을 회수해야 연속 배치 공간을 만들 수 있음 | **2 우선, 조건부 4/5**. 메시 수정 근거는 없다. Unity 정면/물리/피드백을 우선하고, 기존 텍스처의 붉은 열원 과채도나 목적 불명확이 캡처에서 확인될 때만 재질/파생물 보정 | **Tier 1 기능/배치 통합 완료, 시각 최종화 대기** | FBX·텍스처·래퍼 보존. 개별 Tripo 증빙 필요. 외형 교체 확인 불필요 |
| `Assets/Models/Buildings/B08_SewingTable.fbx` / 대응 래퍼 | 메인 씬 `Workbench_SewingTable`, 기존 Sewing 레시피, 상점 실내 Tier 3 배치 | 높음 | 2×2 배치 계약은 있으나 의류 작업면·상호작용 정면·콜라이더·성공 피드백을 게임 카메라로 확인하지 않음. BuildingData `requiredTier=1`과 실제 Tier 3가 다름 | **2 우선, 조건부 5**. 원본 파스텔 목재/천 실루엣을 유지하고 Unity 정면·물리·기존 제작 피드백부터 보정한다. Blender 수정 근거 없음 | **기능/배치 통합 완료, 시각 최종화 대기** | FBX·텍스처·래퍼 보존. 개별 Tripo 증빙 필요. 외형 교체 확인 불필요 |
| `B09_StorageShed` / `Assets/Prefabs/Buildings/B09_StorageShed.prefab` | `[PA_MapRoot]/[MapBuildings]/B09_StorageShed`, 외부 `StorageBox` 24칸 | 높음 | 기존 BuildManager가 7×5.5m를 1셀로 취급했고 레거시 중복 인스턴스가 남을 수 있었음. 내용물 저장·안전 회수·접근 안내 부재 | **2**. 외형 유지. `village.outdoor` 9~12셀 footprint, 전면 clearance, 90° 정렬, carving, `[M]` 이동, `[X]` 빈 추가 창고 회수, v10 내용물 저장. 기본 공동 창고는 마지막 저장 공간이라 회수 불가 | **외부형 생활 창고 1차 최종화 완료**. 문/손잡이/목재 팔레트·플레이어 접근을 게임 카메라로 확인 | 원본 FBX/프리팹 보존, 생성 증빙 필요. 전체 외형 교체 불필요 |
| `B10_Cottage` | 주민 주거 3채 + Tier 1 실내 상점의 외부 출입구 | 높음 | FBX 정면은 로컬 `-X`인데 맵이 `-Z`를 광장으로 향하게 배치했고, 별도 원시 `PA_StoreDoor_Out`/간판이 `local z=-3.819`에 떠 있었음. 레거시 Static B10도 중복 | **2+5**. 원본 외형 유지, 런타임에서 3채의 실제 문 정면을 광장으로 회전. 원시 문 렌더러는 숨기고 collider/BuildingEntrance 유지. Unity Editor API로 전용 모따기 목재 간판 파생 제작 | **1차 최종화 완료**. 실제 MainCamera Play 캡처·Tier 왕복 PASS | 원본 FBX/래퍼 프리팹 보존, Tripo 생성 증빙은 계속 필요 |
| `Assets/Models/Buildings/B11_PlazaFountain.fbx` / 대응 래퍼 | 메인 씬 광장 초점 정적 장식. `DemoVisualDressingController`가 분수 기준점을 사용 | 높음 | 6×6m 루트 BoxCollider의 사각 모서리가 보이는 원형 구조 밖까지 막음. 별도 상호작용 기능은 없음 | **2**. 현 실루엣/재질은 유지. 런타임에서 실제 Visual 메시의 비볼록 정적 MeshCollider를 사용하고 루트 Box만 끈다. 기존 캡슐형 carving obstacle은 NPC 우회를 위해 보존 | **Unity 설정 보정 구현, 실제 이동/동선 최종 확인 대기** | FBX·텍스처·래퍼 보존. 개별 Tripo 증빙 필요. 역할 변경 시에만 확인 |
| `Assets/Models/Buildings/B12_TradePort.fbx` / 대응 래퍼 | 메인 씬 해안의 `B12_TradePort` 정적 장식. 현재 런타임 배치 카탈로그/상호작용에는 미등록 | 높음 | 래퍼의 역사적 10×5m Box/Obstacle이 실제 Visual 약 3.63×1.96m보다 커 해안에 보이지 않는 벽과 carving 공백을 만들 수 있음. BuildingData·청사진은 있으나 교역 기능은 Task 079 선행 조건 미충족 | **2**. 활성 인스턴스의 Visual 로컬 bounds를 8모서리로 계산해 명백히 큰 루트 `BoxCollider`와 box형 `NavMeshObstacle`만 축소한다. 어떤 축도 키우지 않고 메시가 없으면 기존 물리를 유지한다. 재질/교역 역할은 추가하지 않는다 | **Unity 물리 보정 구현, 실제 해안 이동/NPC 우회 최종 확인 대기** | FBX·텍스처·래퍼·씬 보존. 개별 Tripo 증빙 필요. 역할 변경 시 사용자 확인 |

모든 B01~B12 원본 FBX는 `Assets/Models/Buildings`, Unity 래퍼 프리팹은 `Assets/Prefabs/Buildings`, 기능 정의는 `Assets/Resources/Buildings`에 보존되어 있다.

## 기타 소품

| 에셋군 | 현재 역할 | 결정 |
|---|---|---|
| Ultimate Nature Pack 300 models | 나무·바위·꽃·덤불·원목 등 마을 자연물 | **1/2**. 폴더의 `License.txt`가 Quaternius 제작, CC0 1.0을 명시한다. 원문 보존, 반복 밀도/콜라이더만 Unity에서 조정 |
| `Assets/Art/animal-crossing-froggy-chair/source/FroggyChair.fbx` | `FroggyChair.prefab` → `Resources/PA_DemoProps/Prop_FroggyChair.prefab`. Task 094에서 실내·광장 런타임 생성 제거 | **8**, 증빙이 없으면 **6**. 플레이어 경험의 참조는 0으로 만들었지만 Resource와 원본은 보존되어 빌드 포함 가능성이 남는다. 최종 빌드 전 출처/상업 이용 증빙을 확보하거나 Resource 파생물을 승인된 대체물로 교체·격리 |
| 런타임 원시 상자/화분/램프 | 임시 드레싱 | **5/6**. 핵심 카메라에 크게 보이는 것부터 기존 모델 또는 목적형 저폴리 에셋으로 교체 |

## 이번 수정 근거

변경 전 `Logs/DemoViewShots/shot_20260715_164534.png`에서는 중앙 상점이 갈색 원시 박스와 넓은 평판으로 보여 가장 큰 시각적 초점을 차지했다. MCP 프리팹 캡처 `Logs/VisualAudit/B01_MarketStall_MCP.png`에서 B01의 목재·크림·줄무늬 차양, 4슬롯 구조가 현재 캐릭터와 맞는 것을 확인했다.

`DemoVisualDressingController`는 B01 `Visual`만 런타임에서 복제한다. 기존 Shop/ShopSlot/경제/저장 로직은 바꾸지 않고, ShopSlot의 루트 원시 Renderer만 숨겨 상품 모델·아이콘 자식은 유지한다. 겹치던 Support_Crate, Shop_Tent_Kit, Sales_Tent_Preview는 런타임 Renderer와 Collider를 함께 비활성화했다. 메인 씬과 원본 프리팹은 저장하지 않았다.

동일 구도 최종 캡처 `Logs/DemoViewShots/shot_20260715_171613.png`에서 B01의 차양과 프레임, 네 상품 접근면이 보이고 변경 전의 큰 큐브/검은 아티팩트가 제거된 것을 확인했다. 기존 판매 왕복은 `Logs/Codex_VisualToolchain_FinalRouteRegression.log`에서 30G PASS다.

## B06~B08/B11/B12 기능·격자 대조 — 2026-07-17

| 에셋 | 현재 실제 기능 | 배치 계약 | 연결된 콘텐츠 | 이번 판정 |
|---|---|---|---|---|
| B06 Kitchen | `WorkbenchType.Kitchen` | `shop.interior`, 2×2 footprint, 로컬 전면 2셀 clearance/interaction, 90° 회전, 이동/회수 가능, 실제 해금 Tier 2 | Bread, Baked Potato, Grilled Fish; Chef 전문 작업대 | 기능과 배치는 이미 연결됨. 정면·물리·제작 성공 피드백의 게임 카메라 최종화만 남음 |
| B07 Forge | `WorkbenchType.Forge` | `shop.interior`, 3×2 footprint, 전면 3셀 clearance/interaction, 90° 회전, 이동/회수 가능, 실제 해금 Tier 1 | Iron Bar, Tool Set; Blacksmith 전문 작업대 | 기능과 배치는 이미 연결됨. Tier 1 5×4에서는 빈 진열대 두 칸 이상을 회수해 연속 공간을 만든다. 큰 폭의 통로와 열원 표현을 먼저 확인하고 필요할 때만 재질/파생물 보정 |
| B08 Sewing | `WorkbenchType.SewingTable` | `shop.interior`, 2×2 footprint, 전면 2셀 clearance/interaction, 90° 회전, 이동/회수 가능, 실제 해금 Tier 3 | Clothes; Tailor 전문 작업대 | 기능과 배치는 이미 연결됨. 의류 작업 방향과 성공 피드백의 시각 최종화만 남음 |
| B11 Fountain | 기능 컴포넌트 없는 광장 정적 장식 | 상점/야외 배치 카탈로그 미등록. 래퍼 6×6m collider/obstacle | `DemoVisualDressingController`의 광장 기준점 | 배치 가능한 가구로 오인하지 않는다. 보이는 원형 구조와 실제 보행 차단 범위만 맞춘다 |
| B12 TradePort | 기능 컴포넌트 없는 해안 정적 장식 | 상점/야외 배치 카탈로그 미등록. 래퍼 기본 10×5m collider/obstacle, 활성 런타임은 Visual bounds 축소 보정 | BuildingData/청사진은 존재하나 실제 교역 기능 없음 | 현 단계에서 가짜 상호작용을 만들지 않는다. Task 079 이전에는 정적 실루엣·해안 동선만 최종화 |

- 2m 셀 크기와 footprint는 `ShopCustomizationController`가 래퍼 BoxCollider 크기를 올림해 계산한다. B06 `(3.4×2.8m)→2×2`, B07 `(4.4×3.0m)→3×2`, B08 `(3.4×2.6m)→2×2`다.
- 세 작업대 모두 전면 1열을 clearance와 interaction으로 함께 예약한다. 따라서 모델의 실제 작업면이 이 전면과 반대면 Unity에서 Visual 회전/기준점 보정을 우선하며, 메시를 곧바로 Blender에서 고치지 않는다.
- `BuildingData.requiredTier`는 B06=0, B07/B08=1이다. 현재 플레이어가 경험하는 배치 해금은 B06=Tier 2, B07=Tier 1, B08=Tier 3이다. B07은 설계도·건물·`Recipe_ToolSet`·`Item_12_ToolSet` 네 데이터가 모두 Tier 1이므로 Task 115에서 배치 카탈로그를 이 권위에 정합했고, B06/B08의 기존 지연 진행은 보존했다. 데이터 에셋은 수정하지 않았다.
- B06~B08/B11/B12 FBX 임포터는 모두 global scale 1, file units/scale 사용, 자동 collider 없음, animation import 없음이다. 축/크기 보정과 물리는 래퍼에서 소유한다. 저장소에 `.blend`가 없고 메시 손상 증거도 없어 이번 단계에서 Blender 도입 근거가 없다.
- 기존 `Logs/ShopProgressionUnlock/20260717_005831/tier3_expanded_shop.png`는 배치 UI·Tier 해금 증거이지 B06~B08 모델 최종 캡처가 아니다. `Logs/DemoViewShots/shot_20260715_171613.png`에서 B11 분수의 광장 스타일 적합성은 보이지만, 콜라이더/우회 동선은 검증하지 않았다. 보지 않은 항목을 완료로 올리지 않는다.

## 배포 게이트와 다음 실제 작업 순서

1. 사용자 보유 Tripo 계정/생성 기록에서 C-01~C-09, walking, B01~B12의 생성일·계정·상업 이용 범위를 증빙한다. 증빙 전에는 현재 통합 판정과 별개로 최종 배포 확정이 아니다.
2. Unity 재실행 안전 경로가 승인되면 B06 Kitchen 하나만 같은 게임 카메라 전후 캡처로 정면·2×2 footprint·전면 통로·콜라이더·실제 Bread 제작 피드백까지 최종화한다. 이후 B07, B08을 같은 방식으로 순차 처리한다.
3. B11은 광장 횡단 경로와 보이는 분수/벤치 구조에 맞춘 collider/obstacle만 검사한다. B12는 안전 Unity 경로에서 실제 Visual 경계 정지, 기존 10×5m 모서리 통과, 플레이어 해안 이동과 NPC carving 우회를 먼저 확인한다. 교역 상호작용은 Task 079의 선행 조건 전에는 추가하지 않는다.
4. C-03/C-04 역할 매핑, C-05 중복과 C-02 미사용은 외형 정체성 변경이므로 사용자 확인 전 재할당하지 않는다. 메시·Avatar·접지·보행 보정은 그대로 유지한다.
5. Froggy Chair의 실내·광장 런타임 생성은 Task 094에서 제거했다. 원본과 Resource 파생물은 보존되어 있으므로 최종 빌드 전에는 라이선스 증빙을 확보하거나 Resource 밖으로 격리/승인된 대체물로 교체한다. 확인되지 않은 채 “코지 소품”으로 다시 연결하지 않는다.

## Task 094 실제 조치 — 2026-07-17

- `DemoVisualDressingController`의 실내와 B11 광장 주변 `Prop_FroggyChair` 생성 2곳을 제거했다. 기존 B01 노점, Shop/ShopSlot, 실내 러그·선반·계산대, 광장 벤치, Quaternius CC0 식생은 유지한다.
- 원본 `FroggyChair.fbx`, 래퍼/Resource 프리팹, 메인 씬은 삭제·덮어쓰기하지 않았다. 따라서 이번 조치는 플레이어 노출 제거이며 최종 빌드 패키지의 라이선스 해소를 가장하지 않는다.
- B06 Kitchen은 현재 모델을 볼 수 있는 전용 게임 카메라 캡처가 없고 Unity 직접 렌더 충돌 경계가 유지된다. 정면·재질을 추측해 수정하지 않았으며, 안전한 캡처 경로 승인 뒤 B06 하나만 다음 시각 최종화 대상으로 유지한다.

## B02~B04 상점 진화 최종화 근거 — 2026-07-16

- 원본 감사: B02는 14,688 vertices/14,292 triangles, bounds `7.020×5.928×6.000m`; B03은 16,789/14,758, `4.366×4.911×7.000m`; B04는 26,780/22,763, `5.853×8.451×8.500m`다. 세 모델 모두 `minY=0`, 정상 메시·재질이며 실제 문 정면은 로컬 `-X`다.
- 시각 판단: B02의 작은 목재 잡화점, B03의 민트 차양 마켓, B04의 맨사드 지붕 부티크는 기존 둥근 캐릭터·따뜻한 마을 팔레트와 일관된다. 심각한 비율·리깅·메시·텍스처 손상이나 회복 불가능한 현대성은 관찰되지 않았다.
- 결정: 세 에셋 모두 분류 **2(Unity 설정만 수정)**다. Blender 수정, 재질 덮어쓰기, 외형 교체는 근거가 없어 수행하지 않았다.
- 중복 방지: 기존 래퍼는 각각 Shop 1개와 ShopSlot 8/16/32개를 포함하므로 진화 외관으로 통째로 생성하지 않는다. `Assets/Resources/VisualFinalization/ShopEvolution/` 아래의 B02/B03/B04 visual-only 파생 프리팹만 사용한다.
- 물리·입구: 파생 루트에 보이는 모델 bounds와 일치하는 BoxCollider/NavMeshObstacle, `EntranceAnchor`, `SignAnchor`를 구성했다. 실제 정면을 기준으로 B02 문은 로컬 Z `+1.45`, B03/B04는 중앙 문에 정렬했다.
- Tier 연결: 기존 `currentTier`에서 Tier 0=B10 Cottage, Tier 1=B02, Tier 2=B03, Tier 3+=B04를 파생한다. 새 저장 필드는 없으며 기존 `PA_StoreDoor_Out`, `PlayerSpawn_Outside`, `BuildingEntrance`, 간판을 활성 외관 기준점으로 이동한다.
- 배치 보존: 단계 전환 전후 `shop.interior`의 instance/cell/rotation/recovered/function/storage 스냅샷이 완전히 동일함을 실제 Play Mode에서 확인했다. 기존 실내 Shop/ShopSlot은 중복 생성되지 않는다.
- 시각 증거: `Logs/ShopEvolutionAudit/shop_evolution_runtime_before.png`와 `shop_evolution_tier1_after.png`, `shop_evolution_tier2_after.png`, `shop_evolution_tier3_after.png`. 같은 게임 카메라에서 단계별 문·플레이어·간판·실루엣을 직접 확인했다.
- 기능 증거: `Logs/ShopEvolution_RuntimeFinal_AnchorFix.log`, `Logs/ShopEvolution_EnterableShopRegression.log`, `Logs/ShopEvolution_FinalDemoRouteRegression.log`. Tier 0 잠금→Tier 1 개방→입장→6슬롯 진열→가격 UI→퇴장과 BreadLoaf 30G 판매가 PASS했다.
- 원본·출처: 원본 FBX·텍스처·래퍼 프리팹·BuildingData/TierDefinition·메인 씬은 변경하지 않았다. Tripo 생성 계정과 라이선스 증빙은 최종 배포 전 확인 항목으로 유지한다.

## B09 최종화 근거 — 2026-07-16

- 역할: 내부 입장 건물이 아니라 문 앞에서 사용하는 마을 공동 외부 창고. 큰 이중문·손잡이·낮은 기단·작은 창이 저장 기능을 즉시 전달한다.
- 기능: 기존 `StorageBox` 24칸, `IInteractable`, `NavMeshObstacle`, v10 `storedItems`를 그대로 사용한다. Task 102 감사에서 메인 씬 `StorageUI`가 0개라 실제 화면이 열리지 않던 단절을 발견해 런타임 바인더의 단일 6×4 보관 UI, 메타 보존 보관/회수, ESC/커서 우선순위를 연결했다. Runtime/Editor와 정적 계약은 PASS했으며 실제 GameCamera 화면·클릭·저장 왕복은 안전 Unity 경로 대기다.
- 배치: 2m `village.outdoor` zone에서 실제 인스턴스 콜라이더는 9셀, 원본 프리팹은 최대 4×3셀로 계산한다. 전면 1열은 사용 clearance다.
- 안전성: 도로·상점 광장·입구·스폰 보호 셀에 놓을 수 없다. 물품이 든 추가 창고는 회수할 수 없고 기본 공동 창고는 이동만 가능하다.
- 저장: v10 placeables sidecar에 zone/cell/rotation과 내용물 수량·품질·가격을 기록한다. 건물 자체는 기존 `BuildingRegistry` 경로를 유지한다.
- 중복: 메인 맵 B09가 있으면 `[WorldBuildings]` 레거시 B09를 런타임에서 비활성화해 중복 렌더러·보이지 않는 충돌을 제거한다.
- 캡처: `Logs/OutdoorPlacement/20260716_111214/b09_outdoor_baseline.png`와 `b09_outdoor_final.png`. 최종 화면에서 플레이어가 문 앞에 서며 `[Space] 사용`, `[M] 이동`, 기본 창고 회수 제한이 읽힌다.
- 검증: `Logs/OutdoorPlacementValidator.log` — 보호 셀 212, B09 9셀, 고정/동적 이동·저장·복원·회수 PASS.

## B10 최종화 근거 — 2026-07-16

- 원인: Unity 수치 감사에서 원본 FBX는 단일 렌더러, 지면 `minY=0`, 정상 bounds였다. 4면 캡처로 실제 문 정면이 로컬 `-X`임을 확인했지만 맵 배치는 `-Z`를 광장에 향하게 했다. 떠 있던 두 판은 FBX 조각이 아니라 별도 `PA_StoreDoor_Out` 원시 문/간판이었다.
- 결정: 모델 손상이 아니므로 Blender 수정이나 외형 교체를 하지 않았다. `CottageVisualFinalizationController`가 메인 맵 B10 3채의 실제 정면을 광장에 맞추고, `[WorldBuildings]/B10_Cottage_Static` 중복을 런타임에서 비활성화한다.
- 출입: 원본 모델의 문·차양·계단을 실제 외형으로 사용한다. 기존 원시 문은 Renderer만 끄고 `BuildingEntrance`, Collider, Tier 잠금, 실내/외 spawn 계약은 보존했다.
- 파생 에셋: `Assets/Art/ProjectPA/Buildings/B10/B10_Cottage_ShopSign.asset`과 `Assets/Resources/VisualFinalization/B10_Cottage_ShopSign.prefab`. Unity Editor API로 만든 2재질 모따기 저폴리 간판이며 기존 Project PA 목재/크림 재질을 재사용한다.
- 원본 보존: `Assets/Models/Buildings/B10_Cottage.fbx`, `.fbm/Color.jpg`, `Assets/Prefabs/Buildings/B10_Cottage.prefab`, 메인 씬은 수정하지 않았다.
- 시각 증거: 동일 고정 구도 `Logs/B10_CottageAudit/b10_cottage_before.png` → `b10_cottage_after.png`; 실제 Play Mode MainCamera `b10_cottage_runtime.png`. 분리 판 제거, 정면 문/계단 노출, `P.A. SHOP - Tier 1` 가독성을 직접 확인했다.
- 기능 검증: `Logs/B10_EnterableShopRegression_Final.log`과 `Logs/B10_FinalDemoRouteRegression.log`. Tier 0 잠금→Tier 1 OPEN→입장→진열→가격→퇴장, Day 1 30G 판매가 PASS했다.

## B05 최종화 근거 — 2026-07-16

- 기능·시각 역할: 새 거대 제작 시스템을 만들지 않고 기존 `Workbench → CraftingUI → CraftingService`와 `BasicWorkbench` 레시피를 사용한다. B05는 Lumberjack 공급 경로의 Wood를 Plank로 가공해 상점 상품 준비로 이어지는 목재 가공 작업대다.
- 원본 감사: FBX bounds center `(0,1,0)`, size `(2.260,2.000,2.507)`, `minY=0`; 1 mesh/renderer, 15,629 vertices, 14,689 triangles, 2 submeshes다. 상판·페그보드·스툴·서랍이 있어 실루엣 교체나 Blender 수정은 필요하지 않다.
- 문제 원인: 원본의 넓은 작업면은 로컬 `+Z` 쪽인데 기존 2×2 배치 시스템은 로컬 `-Z`를 interaction/clearance 면으로 예약했다. 래퍼의 폭 3.2m BoxCollider도 2.26m 메시보다 넓어 실제 통로를 불필요하게 막았다.
- 결정: 분류 **2+5**. 런타임에서 원본 `Visual`만 Y 180° 회전해 기존 `-Z` interaction 계약에 맞추고, 실제 물리 BoxCollider/NavMeshObstacle을 `(2.5,2,2.55)`로 정합했다. 원본 프리팹의 3.2m 폭은 2×2 footprint 산정 계약 때문에 변경하지 않았다.
- 파생 에셋: `Assets/Art/ProjectPA/Buildings/B05/B05_Workbench_PreparationKit.asset`, `Assets/Resources/VisualFinalization/B05_Workbench_PreparationKit.prefab`. 기존 Project PA 재질로 원목 2개→크림 가이드 작업면/레일→완성 Plank 2개와 coral clamp를 구성했다. 무작위 가짜 도구는 넣지 않았다.
- 피드백: 성공한 기존 제작 트랜잭션 뒤에만 clamp handle이 짧게 움직이고 따뜻한 점광원이 0.72초 pulse한다. 첫 캡처의 과한 광량을 2.2→0.9로 낮춘 뒤 같은 구도에서 재확인했다.
- 원본 보존: `Assets/Models/Buildings/B05_Workbench.fbx`, 관련 텍스처, `Assets/Prefabs/Buildings/B05_Workbench.prefab`, 메인 씬은 수정하지 않았다. 원본 보존 위치는 기존 경로이며 파생물은 위 ProjectPA/Resources 경로에 분리했다.
- 시각 증거: `Logs/B05_WorkbenchAudit/b05_runtime_before.png` → `b05_runtime_after.png`. 플레이어가 실제 예약 앞면에 서고, 작업면·원재료·완성 판재·clamp가 같은 방향에서 읽히는 것을 직접 확인했다. 4면 격리 캡처도 같은 폴더에 있다.
- 기능 검증: `Logs/B05_WorkbenchFinalPlayValidation_Final.log`에서 실제 UI를 열어 Wood 2개→Plank 1개, 인벤토리 차감/추가, clamp/light 피드백, UI 닫기를 확인했다. `B05_ShopCustomizationRegression.log`, `B05_EnterableShopRegression.log`, `B05_FinalDemoRouteRegression.log`, `B05_ProcessingChainRegression.log`도 PASS했다.
- 라이선스·사용자 확인: Tripo 추정은 높으나 개별 생성/라이선스 증빙은 아직 필요하다. 외형 정체성을 유지한 설정/파생 보정이므로 현재 단계에서 사용자 취향 확인은 필요하지 않으며, 전체 외형 교체 시에만 확인한다.

## C-01~C-09 캐릭터 설정 감사 — 2026-07-16

- 원본 품질: C-01~C-09는 모두 `Humanoid/Create From This Model`이며 Avatar가 valid/human이다. 전체 40,643 vertices/52,070 triangles이고 개별 메시가 깨지거나 텍스처가 누락된 흔적은 없었다. `Logs/CharacterFinalization/character_source_lineup.png`에서 둥근 얼굴·큰 손발·따뜻한 저채도 의상이라는 공통 형태 언어도 확인했다.
- 원본 수치: raw height는 C-01 `1.100`, C-02 `1.063`, C-03 `1.048`, C-04 `1.063`, C-05 `1.061`, C-06 `1.052`, C-07 `1.094`, C-08 `1.043`, C-09 `1.058m`다. 하나의 고정 2× 배율을 쓰면 주민이 `2.088~2.190m`로 커지고 캐릭터별 발 오차가 생기는 것이 근본 원인이었다.
- Unity 설정 보정: `NpcPresentationNormalizer`가 각 SkinnedMeshRenderer 실제 bounds를 기준으로 주민을 `1.75m`에 맞추고 발바닥을 root 위 `0.02m`에 둔다. CapsuleCollider와 NavMeshAgent는 각각 `1.8m/0.4m`, 정지 거리는 `0.75m`로 통일하고 모델 그림자를 켠다. 원본 FBX/Avatar/텍스처/메인 씬은 수정하지 않았다.
- 보행 보정: NPC 절차 보행은 절대 시간 배율 대신 실제 NavMeshAgent 이동 속도와 `1.15m` 명목 보폭으로 cadence를 계산한다. 플레이어는 기존 `Walk.anim` 평균 속도 `1.174m/s`를 기준으로 기존 IK 컴포넌트에서 최대 2.25×까지 재생 속도를 보정하도록 구현했다. 원본 애니메이션 클립을 재가공하지 않았다.
- 검증 완료 범위: Unity D3D11 컴파일 PASS. 실제 Play idle에서 주민 8명 높이 `1.748~1.751m`, 발 offset `+0.032~+0.035m`, Capsule `1.8/0.4`, Agent `1.8/0.4/0.75`, valid Humanoid와 그림자가 모두 PASS했다. 보행 중에는 플레이어 5m/s/재생 2.25×, NPC 8명 2.5m/s/cadence `1.851~2.328`, 높이 `1.737~1.758m`, 발 offset `+0.029~+0.037m`를 유지했다.
- 시각 검증: `character_idle_before.png` → `character_idle_after.png`에서 플레이어 1.85m와 주민 1.75m 비율·접지가 일관되어졌다. `character_walk_after.png`는 실제 동서 도로/게임 카메라에서 주민 8명과 플레이어를 분리 배치해 발·실루엣·보행 방향·그림자를 확인했다. 오른쪽 길가 나무와 주민 1명의 몸통이 일부 겹치지만 캐릭터 식별과 발 자세는 유지되며, 상점 기둥 뒤에 군집하던 첫 캡처보다 명확히 개선됐다.
- 기능 회귀: `InteriorCustomer`의 예약→완전 경로→앞자리 정지→구매→복귀, `CustomerArrival`의 3명 초대/동시 2명 상한, `FinalDemoRoute`의 BreadLoaf 30G 판매가 모두 PASS했다. 원래 timeScale 검증 실패는 `BUG_LOG.md`에서 RESOLVED로 전환했다.
- 사용자 확인: 모델 전체 교체는 필요하지 않다. 다만 C-03 광부형 헬멧/C-04 농부형 밀짚모자와 코드 역할의 불일치, C-05의 Farmer/Fisher 중복, C-02 미사용은 주민 정체성 변경이므로 임의 재매핑하지 않고 사용자 확인 대상으로 유지한다.

## Task 106 B05 Processed 마을 변화 재사용 — 2026-07-18

- 대상: `Assets/Models/Buildings/B05_Workbench.fbx`의 기존 래퍼 `Visual`, `Assets/Resources/VisualFinalization/B05_Workbench_PreparationKit.prefab`, B10 Project P.A. 간판 파생물.
- 현재 역할: Processed 성공 판매 다음 날 광장 옆에 나타나는 비충돌 `가공 준비대` 시각 신호. 실제 B05 작업대·배치·해금의 복제본이 아니다.
- 결정: **1+2 유지/Unity 설정 재사용**. 이미 B05 기능·시각 검증을 통과한 실루엣과 준비 공정 파생물을 0.58배 시각 전용으로 재사용하고, 기존 원시 큐브 5개는 생성하지 않는다.
- 안전 경계: BuildingData 래퍼 전체를 인스턴스화하지 않고 `prefab/Visual`만 복제한다. Collider·Rigidbody·NavMeshObstacle·MonoBehaviour·추가 Light를 제거해 가짜 상호작용과 동선 차단을 막는다.
- 원본/출처: B05 FBX·래퍼·BuildingData·Project P.A. 파생 에셋은 모두 기존 경로에 보존된다. 새 외부 에셋은 없으며 B05의 Tripo 생성/상업 이용 증빙 게이트는 그대로다.
- 최종화 상태: Runtime/Editor 오류 0, 계약 18/18 PASS. 같은 GameCamera의 Processed 판매 전/다음 날 스케일·정면·간판·상점/분수/NPC 동선 확인 전까지 시각 최종화 대기.

## Task 107 B07 Utility 마을 변화 재사용 — 2026-07-27

- 대상: `Assets/Models/Buildings/B07_BlacksmithForge.fbx`의 기존 래퍼 `Visual`, `Assets/Resources/Buildings/Building_B07_BlacksmithForge.asset`, B10 Project P.A. 간판 파생물.
- 현재 역할: 판매 가능한 `철제 도구` Utility 성공 판매 다음 날 광장 옆에 나타나는 비충돌 `공구 수리대` 시각 신호. 실제 B07 Forge·배치·해금의 복제본이 아니다.
- 기능 근거: `Recipe_ToolSet`은 Iron Bar 1+Ore 2를 기존 `WorkbenchType.Forge`에서 철제 도구 1개로 만들고, 해당 상품은 `ToolType.None`, `ItemCategory.Utility`, Tier 1 판매 가능 데이터다.
- 결정: **1+2 유지/Unity 설정 재사용**. 원본 B07 실루엣을 0.44배 시각 전용으로 재사용한다. 메시 수정 근거는 없으며 원본의 열원 방향·색상은 안전 Unity 동일 구도 캡처 전까지 최종 판정하지 않는다.
- 안전 경계: BuildingData 래퍼 전체를 인스턴스화하지 않고 `prefab/Visual`만 복제한다. Collider·Rigidbody·NavMeshObstacle·MonoBehaviour·Light를 제거해 가짜 Forge 상호작용과 동선 차단을 막는다.
- 원본/출처: B07 FBX·텍스처·래퍼·BuildingData는 기존 경로에 보존된다. 새 외부 에셋은 없으며 B07의 개별 Tripo 생성 계정·생성일·상업 이용 증빙 게이트는 그대로다.
- 최종화 상태: Runtime/Editor 오류 0, 계약 23/23 PASS. 같은 GameCamera의 Utility 판매 전/다음 날 스케일·정면·간판·상점/분수/NPC 동선과 실제 B07 기능 작업대와의 시각 구분 확인 전까지 `PARTIAL`.

## Task 108 B08 Luxury 마을 변화 재사용 — 2026-07-27

- 대상: `Assets/Models/Buildings/B08_SewingTable.fbx`의 기존 래퍼 `Visual`, `Assets/Resources/Buildings/Building_B08_SewingTable.asset`, B10 Project P.A. 간판 파생물.
- 현재 역할: 판매 가능한 `목제 가구` 또는 `의류` Luxury 성공 판매 다음 날 광장 옆에 나타나는 비충돌 `공예 전시대` 시각 신호. 실제 B08 Sewing Table·배치·해금의 복제본이 아니다.
- 기능 근거: `Recipe_Furniture`는 Plank 3을 기존 BasicWorkbench에서 목제 가구 1개로 만들고, `Recipe_Clothes`는 Wheat 2+Plank 1을 기존 SewingTable에서 의류 1개로 만든다. 두 결과는 `ToolType.None`, `ItemCategory.Luxury`, Tier 2 판매 가능 데이터다.
- 결정: **1+2 유지/Unity 설정 재사용**. B08의 파스텔 목재·천 롤·마네킹 실루엣을 0.48배 생활 공예 전시로 재사용한다. B08 정면·색상·가구 판매까지의 범용성은 안전 Unity 동일 구도 캡처 전까지 최종 판정하지 않는다.
- 안전 경계: BuildingData 래퍼 전체를 인스턴스화하지 않고 `prefab/Visual`만 복제한다. Collider·Rigidbody·NavMeshObstacle·MonoBehaviour·Light를 제거해 가짜 SewingTable 상호작용과 동선 차단을 막는다.
- 원본/출처: B08 FBX·텍스처·래퍼·BuildingData는 기존 경로에 보존된다. 새 외부 에셋은 없으며 B08의 개별 Tripo 생성 계정·생성일·상업 이용 증빙 게이트는 그대로다.
- 최종화 상태: Runtime/Editor 오류 0, 계약 27/27 PASS. 같은 GameCamera의 Luxury 판매 전/다음 날·v10 복원, 스케일·정면·간판·상점/분수/NPC 동선과 실제 B08 기능 작업대의 시각 구분 확인 전까지 `PARTIAL`.

## Task 113 B12 해안 보이지 않는 충돌 방지 — 2026-07-27

- 재감사 근거: `Logs/Fable_VS_PhysicsAudit.log`는 B12의 10×5m 루트 Box가 실제 Visual 약 3.63×1.96m보다 커 좌우 약 3.2m의 투명 벽을 만들었다고 실측했다. `Fable_VS_PhysicsAudit2.log`는 씬 보정 뒤 oversized collider `flagged=0`을 기록하지만 원본 래퍼의 box형 `NavMeshObstacle`은 여전히 10×5m 기본값을 가진다.
- 결정: **2(Unity 설정만 수정)**를 유지한다. `DemoVisualDressingController`가 활성 `B12_TradePort`/레거시 Static 인스턴스의 실제 Visual 로컬 bounds를 계산하고, 명백히 큰 루트 `BoxCollider`와 box형 `NavMeshObstacle`만 축소한다. 이미 작은 축은 유지하고 어떤 축도 확대하지 않는다.
- 안전 경계: Visual 메시가 없으면 아무것도 바꾸지 않고 다음 갱신에서 재시도한다. FBX·텍스처·래퍼 프리팹·메인 씬·BuildingData·청사진·교역 기능·저장·배치 카탈로그는 변경하지 않았다.
- 격자/기능 판정: B12는 현재 `Protected Gameplay Asset/Developer Only` 성격의 정적 세계 구조이며 Placeable로 등록하지 않는다. 향후 교역 기능을 승인받기 전에는 footprint·interaction·NPC approach·저장 ID를 가진 가짜 기능 가구로 승격하지 않는다.
- 검증 상태: Runtime/Editor C# 빌드 오류 0과 축소 전용·메시 실패 보존·Box/Obstacle 동시 정합 정적 계약을 통과했다. 반복 직접 렌더 네이티브 충돌 경계 때문에 실제 해안 이동·NPC 우회·동일 GameCamera After는 확인하지 않았으며 최종 상태는 `PARTIAL`이다.

## Task 131 장기 정책 재확인과 B06 Kitchen 보정 — 2026-08-04

- 전수 재감사: `Assets`의 모델 수는 FBX 174, OBJ 150, GLB 0, Blend 0으로 기존 감사와 일치했다. Ultimate Nature Pack을 제외한 프로젝트 고유 FBX 24개도 그대로이며 새 외부 모델·텍스처·도구는 추가하지 않았다.
- 정책 판정: 사용자 지시의 1~8 개별 분류, 플레이어/주민 외형 정체성 보존, 창고/작업대 기능 우선, 원본 비파괴, 출처 게이트, 같은 게임 카메라 Before/After 원칙은 이 문서와 `PLACEMENT_SYSTEM_ARCHITECTURE.md`, `PLACEABLE_ASSET_GUIDE.md`에 이미 장기 규칙으로 포함되어 있다. 별도 경쟁 문서를 만들지 않고 이 대장을 단일 자산별 판정 권위로 유지한다.
- B06 결정: **2(Unity 설정만 수정), 조건부 5**를 유지한다. 기존 `Workbench`, Kitchen 레시피 3종, Tier 2, 2×2 footprint, 전면 clearance, NPC 접근, 저장 권위를 바꾸지 않는다.
- 실제 수정: 런타임 B06 인스턴스의 기존 `Visual` renderer bounds를 루트 로컬 공간에서 8모서리로 측정한다. 래퍼 Box보다 0.2m 이상 큰 X/Z 축만 0.16m 여유를 두고 줄이며, box형 `NavMeshObstacle`도 같은 center/size로 정합한다. 축을 키우거나 메시가 없을 때 기존 물리를 바꾸지 않는다.
- 상호작용/피드백: 로컬 `-Z` 물리 앞면에서 0.65m 떨어진 `PA_KitchenInteractionAnchor`를 런타임에 명시하고, 기존 제작 거래가 성공해 결과 아이템이 가방에 들어간 뒤에만 원본 B06 모델 자체가 0.72초 동안 최대 3.5% pulse한다. 원시 오브젝트·무작위 도구·가짜 기능은 추가하지 않았다.
- 원본 보존: `B06_KitchenStation.fbx`, `.fbm/Color.jpg`, 래퍼 프리팹, BuildingData, 설계도, 재질, 메인 씬은 변경하지 않았다. Blender는 메시/UV/노멀 손상 증거와 안전 캡처 판정이 없어 사용하지 않았다.
- 검증: Runtime 오류 0(기존 CS8785 경고 1), Editor 오류 0(기존 CS8785/CS0414 경고 2), B05 보존·B06 축소 전용 물리·anchor·성공 후 pulse·Tier/2×2/출처 정책 계약 30/30 PASS. 반복 네이티브 충돌의 사람 판단 게이트 때문에 실제 전면, 통로, collider 체감, pulse, 동일 게임 카메라 Before/After는 확인하지 못해 `PARTIAL`이다.

## 2026-08-21 — BETA-006 주민 채용 wrapper 최종화 기록

- 대상/출처: 기존 `Assets/Models/Characters/C-02`~`C-09` 계열 주민 SkinnedMesh. 원본 FBX·Avatar·텍스처·meta는 수정하지 않았다.
- 결정: **2(Unity 설정만 수정) + 5(기존 모델 기반 재구성)**. 새 외형을 만들지 않고 `Assets/Resources/Residents/Resident_*.prefab` 8개를 역할별 runtime wrapper로 생성했다.
- 역할 매핑: Farmer=C-02, Lumberjack=C-03, Miner=C-04, Fisher=C-05, Chef=C-06, Blacksmith=C-07, Tailor=C-08, Carpenter=C-09. 각 wrapper는 기존 NpcController와 정확한 Producer/Specialist controller, schedule/dialogue, NavMeshAgent, collider, presentation normalizer를 사용한다.
- 생성 방식: `Assets/Editor/PA_HiringResidentTemplateBuilder.cs`가 원본 SkinnedMesh를 중첩 참조하고 후보 `spawnPrefab`을 재현 가능하게 연결한다. 원본을 덮어쓰거나 메시/UV/노멀/재질을 임의 변경하지 않는다.
- 사용 위치/역할: WorldSandbox runtime 휴대폰 채용의 실제 spawn source. 기존 Golden/Main의 resident와 scene hierarchy는 변경하지 않는다.
- 검증: builder `prefabs=8 linked=8 originalsUntouched=true`; D3D11에서 후보 8명, exact-role SkinnedMesh template, 실제 비용 차감, active spawn, roster와 중복 방지가 PASS했다.
- 라이선스/사용자 확인: 기존 C-series 출처/라이선스 항목을 상속하며 새 외부 에셋은 없다. 외형 교체나 게임 전체 스타일 변경이 아니므로 추가 사용자 확인은 필요하지 않다.
