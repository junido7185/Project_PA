# Project P.A. 에셋·도구 출처 대장

- 갱신일: 2026-07-18
- 원칙: 출처·라이선스·버전이 확인되지 않은 외부 산출물은 최종 배포 에셋으로 확정하지 않는다.

## 외부 도구

| 도구 | 버전/고정값 | 출처 | 라이선스 | 용도·상태 |
|---|---|---|---|---|
| Unity Editor | 6000.3.2f1 `a9779f353c9b` | Unity Hub | Unity 약관 | 프로젝트 엔진 |
| MCP for Unity | UPM 9.7.0, commit `417cf351a152b483c91e6e2deaf7ae355fa8eff3` | CoplayDev GitHub release/tag v9.7.0 | MIT | Unity Editor 직접 감사·캡처·제어, 사용 중 |
| uv | 0.11.28, winget `astral-sh.uv`, SHA256 `0a23463216d09c6a72ff80ef5dc5a795f07dc1575cb84d24596c2f124a441b7b` | Astral/winget | Apache-2.0 또는 MIT | MCP Python 서버 실행, 사용 중 |
| mcpforunityserver | 9.7.0 고정 | PyPI, Unity MCP가 지정한 서버 패키지 | Unity MCP 저장소 기준 MIT | loopback HTTP 서버 |
| Blender | 미설치 | 해당 없음 | 해당 없음 | 필요 확정 전 보류 |

Unity MCP는 `Packages/manifest.json`의 안정 태그와 `packages-lock.json` 커밋 해시로 재현한다. 서버는 `127.0.0.1`에만 바인딩하며 원격 기능과 API key를 사용하지 않는다.

## 공식 Unity 패키지

URP 17.3.0, Shader Graph 17.3.0, Render Pipeline Core 17.3.0, AI Navigation 2.0.12, Timeline 1.8.9, Input System 1.17.0 등은 Unity Package Manager 출처다. 정확한 전체 목록과 전이 의존성은 패키지 lock 파일을 기준으로 한다.

## 프로젝트 에셋

| 에셋군 | 경로 | 확인된 출처/문서 | 현재 배포 판정 |
|---|---|---|---|
| Ultimate Nature Pack | `Assets/Art/Ultimate Nature Pack - Jun 2019` | 폴더 내 `License.txt`: Quaternius, CC0 1.0 Universal/Public Domain Dedication | 사용 가능, 원문 보존 |
| C-01~C-09 캐릭터 | `Assets/Art/Character` | 파일명/문서상 Tripo 계열로 추정, 에셋별 증빙 없음 | 임시. 외형 보존 우선, 최종 배포 전 계정/생성 기록 확인 |
| Tripo walking 변환 | `Assets/Art/Character/tripo_convert_...@Walking.fbx` | 파일명으로 Tripo 변환 확인 | 임시. 리깅/라이선스 증빙 필요 |
| B01~B12 건물 | `Assets/Models/Buildings` | 프로젝트 문서상 Tripo 추정 생성 에셋, 개별 계정/생성일/약관 증빙 없음. 별도 `.blend`/GLB 원본도 미발견 | B01~B05/B09/B10은 외형 유지·Unity 통합 1차 최종화. B06~B08은 기능/격자 통합 완료·시각 최종화 대기. B11은 Visual mesh 물리 정합 구현·실제 이동 확인 대기. B12는 Visual bounds 기반 Box/Obstacle 축소 보정 구현·해안 이동 확인 대기. 증빙 전 최종 배포 확정 아님 |
| Froggy Chair | `Assets/Art/animal-crossing-froggy-chair/source/FroggyChair.fbx` → `Resources/PA_DemoProps/Prop_FroggyChair.prefab` | `source` 폴더만 존재, 라이선스 문서 미발견 | Task 094에서 플레이어 런타임 생성 참조 2곳 제거. Resource/원본은 보존되어 최종 빌드 전 증빙 확보 또는 Resource 격리·검증된 대체물 교체가 계속 필요 |
| Jinxish Inventory Framework | `Assets/Jinxish/...` | 폴더 내 `readme.md` | 기존 시스템 의존성. 구매/라이선스 기록 별도 확인 필요 |
| TextMesh Pro/EmojiOne | `Assets/TextMesh Pro` | EmojiOne Attribution 포함 | 문서 보존 |
| Free RPG Icons/기타 UI 아이콘 | Assets 내 관련 폴더 | 이번 감사에서 라이선스 문서 미발견 | 출처 확인 전 신규 파생 사용 금지 |

## 생성·수정 기록

2026-07-15 중앙 상점 수정은 새 외부 에셋을 생성하지 않았다. 기존 `B01_MarketStall.prefab`의 `Visual`을 런타임에서 복제해 `Prototype_Shop_For_Demo`의 기존 ShopSlot과 정렬했다. 원본 FBX·프리팹·메인 씬은 변경하지 않았다.

2026-07-16 B10 Cottage 최종화는 외부 모델·텍스처·서비스를 사용하지 않았다. Unity 6000.3.2f1 Editor API로 `B10_Cottage_ShopSign_Final` 저폴리 메시와 Resource 프리팹을 생성하고, Project PA 소유 `PA_Market_DarkWood`/`PA_Market_AwningCream` 재질을 재사용했다. 파생 경로는 `Assets/Art/ProjectPA/Buildings/B10/B10_Cottage_ShopSign.asset`과 `Assets/Resources/VisualFinalization/B10_Cottage_ShopSign.prefab`이다. 원본 Tripo 추정 FBX·텍스처·래퍼 프리팹·메인 씬은 변경하지 않았다. Blender는 미설치 상태를 유지했다.

2026-07-16 B05 Workbench 최종화도 외부 모델·텍스처·서비스를 사용하지 않았다. Unity 6000.3.2f1 Editor API로 입력 Wood, 가이드 작업면/레일, 출력 Plank, coral clamp/handle을 가진 저폴리 파생 키트를 생성했다. 파생 경로는 `Assets/Art/ProjectPA/Buildings/B05/B05_Workbench_PreparationKit.asset`과 `Assets/Resources/VisualFinalization/B05_Workbench_PreparationKit.prefab`이며, Project PA 소유 `PA_Market_Wood`, `PA_Market_AwningCream`, `PA_Market_AwningCoral`, `PA_Market_PriceGold` 재질만 재사용했다. 원본 `B05_Workbench.fbx`, 텍스처, 래퍼 프리팹, 메인 씬은 변경하지 않았다. Blender는 원본 메시가 정상이고 Unity 설정/파생물만으로 문제가 해결되어 사용하지 않았다.

2026-07-16 C-01~C-09 캐릭터 감사와 설정 보정은 새 외부 에셋·패키지·서비스를 사용하지 않았다. Unity 6000.3.2f1 Editor API와 기존 C-01~C-09 Humanoid FBX, 기존 `Walk.anim`, 기존 Project PA 런타임 컴포넌트만 사용했다. 원본 캐릭터 FBX·Avatar·텍스처·애니메이션·메인 씬·프리팹을 변경하지 않았고, 런타임 bounds 기반 높이/접지, CapsuleCollider/NavMeshAgent, 그림자, 보행 cadence만 코드로 보정했다. 감사 캡처와 로그는 `Logs/CharacterFinalization/` 및 `Logs/CharacterFinalization_*.log`에 있으며 배포 에셋이 아니다. Blender는 메시·리깅 손상이 발견되지 않아 사용하지 않았다. D3D11 실제 walking과 고객 이동/최종 판매 회귀까지 통과했지만, 에셋별 Tripo 생성 계정과 라이선스 증빙은 여전히 최종 배포 전 사용자 확인 항목이다.

2026-07-16 B02~B04 상점 진화 최종화는 외부 모델·텍스처·서비스·패키지를 사용하지 않았다. Unity 6000.3.2f1 Editor API로 기존 래퍼의 `Visual`만 복제해 `Assets/Resources/VisualFinalization/ShopEvolution/B02_ShopEvolutionVisual.prefab`, `B03_ShopEvolutionVisual.prefab`, `B04_ShopEvolutionVisual.prefab`을 생성했다. 파생물은 기존 임포트 메시·재질을 참조하며 Shop/ShopSlot을 포함하지 않고, 모델 bounds 기반 BoxCollider/NavMeshObstacle과 Entrance/Sign 기준점만 추가한다. 원본 B02~B04 FBX·텍스처·래퍼 프리팹·BuildingData/TierDefinition·메인 씬은 변경하지 않았다. Blender는 세 메시와 재질이 정상이고 Unity 통합만으로 해결되어 사용하지 않았다. Tripo 생성 계정·생성일·상업 이용 범위 증빙은 최종 배포 전 사용자 확인 항목이다.

2026-07-17 Tripo 감사 정합화는 외부 도구·에셋·서비스를 추가하지 않았다. FBX 174/OBJ 150/GLB 0/Blend 0 전수 수량, Unity 6 바이너리 메인 씬의 읽기 전용 문자열 색인, FBX GUID→래퍼→BuildingData/청사진, Resources/코드 참조, 기존 캡처를 대조했다. B06 Kitchen은 기존 Bread/Baked Potato/Grilled Fish와 2×2 Tier 2 배치, B07 Forge는 Iron Bar/Tool Set과 3×2 Tier 3 배치, B08 Sewing은 Clothes와 2×2 Tier 3 배치에 이미 연결되어 있다. B11/B12는 메인 씬 정적 장식이며 현재 배치 카탈로그나 상호작용 기능은 없다. 원본 FBX·텍스처·씬·프리팹·코드·패키지는 변경하지 않았다.

같은 감사에서 `Ultimate Nature Pack - Jun 2019/License.txt`가 Quaternius 및 CC0 1.0을 명시함을 확인했다. 반대로 C-01~C-09, Tripo walking, B01~B12의 개별 생성 계정/생성일/상업 이용 증빙과 Froggy Chair의 라이선스 문서는 저장소에서 발견하지 못했다. 기능 통합/시각 적합 판정은 라이선스 판정을 대체하지 않으며, 이 항목들은 최종 빌드 배포 게이트로 유지한다.

2026-07-17 Task 092 Raw 마을 변화는 새 외부 에셋·서비스·패키지를 추가하지 않았다. `Assets/Resources/PA_DemoProps/Prop_WoodLog.prefab`과 `Prop_Rock.prefab`은 위에 기록한 Quaternius Ultimate Nature Pack CC0 원본을 참조하고, `Assets/Resources/VisualFinalization/B10_Cottage_ShopSign.prefab`은 2026-07-16 기록의 Project P.A. 자체 파생 메시다. 런타임 복제본만 `PA_VillageCulture_Raw` 아래에 배치하며 원본 FBX/OBJ·Resource 프리팹·재질·씬은 변경하지 않는다. Collider와 기능 컴포넌트는 복제본에서 제거한다. Unity 실제 카메라 판정 전에는 시각 최종화가 아니라 기능 구현 `PARTIAL`로 유지한다.

2026-07-17 Task 094는 새 도구·에셋·서비스를 추가하지 않았다. 저장소에서 라이선스 문서를 찾지 못한 Froggy Chair가 `DemoVisualDressingController`를 통해 실내와 B11 광장에 생성되던 두 참조를 제거했다. 원본 FBX와 래퍼/Resource 프리팹은 출처 확인 및 사용자 자료 보존을 위해 변경하지 않았으므로, 최종 빌드 패키지에서는 별도 증빙 또는 Resource 격리가 여전히 필요하다. 기존 Quaternius CC0 식생, Project P.A. 광장 벤치, B01 노점과 상점 기능은 그대로 유지한다.

2026-07-18 Task 104는 새 도구·에셋·서비스·패키지를 추가하지 않았다. 기존 Tripo 추정 `B11_PlazaFountain.fbx`의 Visual mesh를 런타임 정적 `MeshCollider` 원본으로 재사용하고, 사각 모서리의 보이지 않는 충돌을 만들던 래퍼 루트 `BoxCollider`만 활성 인스턴스에서 끈다. 원본 FBX·텍스처·래퍼 프리팹·메인 씬은 변경하지 않았으며, 기존 캡슐형 carving `NavMeshObstacle`과 광장 배치는 보존한다. B11의 개별 생성 계정·생성일·상업 이용 증빙은 계속 최종 배포 게이트다.

2026-07-27 Task 113은 새 도구·에셋·서비스·패키지를 추가하지 않았다. 기존 Tripo 추정 `B12_TradePort.fbx`의 Visual mesh bounds를 활성 런타임 인스턴스의 물리 기준으로만 읽어, 역사적 10×5m 루트 `BoxCollider`와 box형 `NavMeshObstacle`이 실제 Visual보다 명백히 클 때 축소한다. 어떤 축도 기존보다 키우지 않고 Visual 메시가 없으면 기존 물리를 보존한다. 원본 FBX·텍스처·래퍼 프리팹·메인 씬·BuildingData·청사진은 변경하지 않았고 교역 기능이나 Placeable 등록도 추가하지 않았다. B12의 개별 생성 계정·생성일·상업 이용 증빙은 계속 최종 배포 게이트다.

2026-07-27 Task 115는 새 도구·에셋·서비스·패키지를 추가하지 않았다. 기존 `B07_BlacksmithForge.fbx`·래퍼 프리팹·BuildingData·설계도·`Recipe_ToolSet`·`Item_12_ToolSet`을 그대로 재사용하고, 플레이어 배치 카탈로그의 최소 Tier와 장부 설계도 보상만 기존 네 데이터의 Tier 1에 맞췄다. 원본 모델·텍스처·프리팹·데이터 에셋은 변경하지 않았다. 3×2 B07의 실제 정면·열원·콜라이더·전면 접근과 5×4 실내의 진열대 회수 후 배치는 안전 Unity 경로 확인 전 `PARTIAL`이며, 개별 Tripo 생성/상업 이용 증빙 게이트도 유지한다.

## 장기 규칙

- Tripo 원본은 덮어쓰지 않는다. 수정본은 별도 소스/내보내기 경로와 이름을 쓴다.
- Blender 도입 시 `.blend`, 내보낸 FBX, 피벗/스케일/노멀/UV/재질 슬롯, 사용 씬을 함께 기록한다.
- 생성형 텍스처·모델은 프롬프트만으로 출처를 대체하지 않는다. 서비스 약관, 생성 계정, 생성일, 상업 이용 범위를 기록한다.
- 유료 결제·로그인·약관 동의가 필요한 항목은 사용자 승인 전 실행하지 않는다.
- 최종 빌드 전 `미확인` 항목을 사용자 증빙 또는 대체 에셋으로 해소한다.
