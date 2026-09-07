# VS-PRESENT-001 출항 인증 코스 파생 에셋

2026-09-07 · P0 · 최종 제작판 `r02`. ART-000의 입고 결과와 Style Grammar를 사용했다. ART-000 라이브러리와 원본은 읽기 전용으로 유지했으며, 새 외부 다운로드는 없다.

현재 Unity FBX 5종은 [derived-assets.json](derived-assets.json)의 경로와 SHA256으로 식별한다. 편집 가능한 제작 파일은 `Blender/Generated/VS_PRESENT_001/PA_DepartureTrainingKit_r02.blend`, 최종 참조 렌더는 [r02/](r02/)다. 기존 초판과 r01 파일은 삭제하지 않았다. 이 PNG는 Blender 모델 참조 렌더이며 실제 Unity 플레이 캡처가 아니다.

## 출처와 파생 작업

| 결과 | 보존한 원본 / 라이선스 | 실제 파생 작업 |
|---|---|---|
| TrainingShelf | Kenney Mini Market의 `display-bread.glb`, ART-000 library의 `display-bread` mesh / [CC0-1.0 원문](../Licenses/MiniMarket_License.txt) | 원본 빵 두 개를 제외한 빈 틀만 append. 치수 보정, 약 1m 상판, 다리 네 개, 버팀대, 빈 가격표 제작. 실제 재고는 ShopSlot이 표시한다. |
| TrainingBoards | Project P.A.에서 직접 제작한 Blender geometry | 공통 목재 보드와 이동·열매 채집·진열/가격 그림 3종. 글자를 mesh로 변환. 외부 게임의 간판·배치·그림을 복제하지 않았다. |
| Checkpoint | Project P.A.에서 직접 제작한 Blender geometry | 두 개의 낮은 신호 기둥과 통과 방향을 읽는 바닥 문턱. |
| Fruit | Kenney Food Kit의 `apple.glb` / [CC0-1.0 원문](../Licenses/FoodKit_License.txt) | ART-000에서 입고·해시 검증한 사과를 새로 선택해 높이 0.28m로 정규화하고 팔레트 적용. 이번 열매 ItemData에 사용하는 TEMP 시각 자료. |
| Boat | Quaternius Cute Fish의 `Boat.blend` / [CC0-1.0 원문](../Licenses/CuteFish_License.txt) | 길이 5.8m 정규화, P.A. 목재 팔레트, 중앙 승선판과 회사 깃발 추가. P0 항구 장식이며 이동 기능은 포함하지 않는다. |

공식 출처와 팩 버전은 기존 [EXTERNAL_ASSET_REGISTRY.md](../EXTERNAL_ASSET_REGISTRY.md)를 따른다. 새로 직접 제작한 geometry에 외부 팩의 CC0 라이선스를 임의로 부여하지 않는다.

| 보존 원본 | SHA256 |
|---|---|
| `Blender/Library/ProjectPA_AssetLibrary.blend` | `1c446a916071b1997abc5917fcefab1e56102e431578b97b070f00a0f61ede09` |
| `ExternalAssetSources/Kenney/MiniMarket/Extracted/Models/GLB format/display-bread.glb` | `33a57fee0e331e5e40df5f0dc7af302e93266fd978dda810ab9f0ea4a2c205d1` |
| `ExternalAssetSources/Kenney/FoodKit/Extracted/Models/GLB format/apple.glb` | `2a80ab65d5bc2cb4f2bca1e8176cbb591849a288596d4b2e76e411338761a526` |
| `ExternalAssetSources/Quaternius/CuteFish/Extracted/Cute Fish Pack - Feb 2020/Blends/Boat.blend` | `df6babfe08f04749327c564f65ac77afa3c6194142829ff36dc4e911ddd5c4bd` |

## Unity 배치 계약

1 unit = 1m. 모든 모델의 원점은 바닥 중앙이며 생성 시 바닥 및 X/Y 중앙 오차를 0.006m 미만으로 검사한다. Blender +Z up / -Y 앞면을 Unity +Y up / -Z 상호작용면으로 export한다. 실제 importer 축과 접근면 확인은 Unity wrapper 검증 대상이다.

| 모델 | 예상 Unity XYZ (m) | footprint XZ (m) | 앞면 clearance (m) | triangles |
|---|---|---|---:|---:|
| TrainingShelf | 1.799 × 1.215 × 1.3225 | 1.8 × 1.35 | 1.2 | 1,070 |
| TrainingBoards (각 child) | 1.78 × 2.56 × 0.60 | 1.8 × 0.6 | 0.8 | 3종 합계는 JSON 실측 |
| Checkpoint | 2.82 × 1.575 × 0.55 | 2.82 × 0.55 | 1.0 | 844 |
| Fruit | 0.2881 × 0.28 × 0.2881 | 0.3 × 0.3 | 0 | 136 |
| Boat | 3.0052 × 2.23 × 5.80 | 3.1 × 5.8 | 1.0 | 1,032 |

`TrainingBoards.fbx`에는 `Board_Move`, `Board_Harvest`, `Board_Trade`가 같은 원점에 있다. Bay마다 해당 child 한 개만 배치한다. 참조 렌더의 01→02→03 간격은 렌더 전용이며 모델 배치에 저장되지 않는다. 체크포인트 중앙 2m는 통행 구간이므로 단일 전체 BoxCollider로 막지 않는다.

FBX에는 collider와 NavMesh, 상호작용 코드를 넣지 않았다. 기존 gameplay owner가 primitive collider·InteractionAnchor·ShopSlot·가격·재고를 소유한다. 선반 상판은 약 1m, 보트 중앙 승선판의 높이는 약 0.75m다. 모델을 실제 배 이동에 사용할 경우 승객 위치·충돌·경로는 별도 P2 검증이 필요하다.

공유 재질 이름과 sRGB HEX는 `derived-assets.json.paletteSrgb`에 있다. Style Grammar의 목재 `A07850`, 밝은 목재 `D4B896`, 잎 `8DB87A`, terracotta `D4714A`, teal `5BAFC0`, gold `F5D76E`를 사용한다. 보조색은 cream `F5EAD5`, ink `344B4E`, 어두운 잎 `547957`, 열매 `D6604F`, coral `F08070`다. Blender roughness 0.8 / metallic 0이며 Unity 기본 대응은 smoothness 0.2 / metallic 0이다. 조명에 따른 최종 색감은 Unity 화면에서 확인한다.

## 실행과 실제 확인

프로젝트 루트에서 실행한 명령:

```powershell
& 'ExternalAssetSources/Tools/Blender/Portable/blender-4.5.13-windows-x64/blender.exe' --background --factory-startup --disable-autoexec --python-exit-code 1 --python 'Blender/Scripts/build_departure_assets.py' -- --revision 02
```

동일 revision의 기존 `.blend` 또는 export가 있으면 스크립트가 덮어쓰기를 거부한다. 다음 수정은 사용하지 않은 revision 번호로 생성한다. Unity 출력은 직전 manifest SHA와 일치할 때만 이번 작업의 FBX를 갱신하며, 원본과 ART-000 library SHA는 생성 전후 대조한다.

- Blender 4.5.13 LTS 종료 코드 0. `Logs/VS_PRESENT_001/BlenderProduction_r02.log`에 5종 export, 5종 reference render, `VS_PRESENT_BLENDER_PRODUCTION_PASS assets=5` 기록.
- Cycles CPU, 20 samples, denoise, Standard view. Boards 1600×900, 나머지 4종 960×960. r02 PNG 5장을 직접 열어 확인했다.
- 안내판: 01→02→03 순서, 이동 화살표·SPACE·PRICE 식별, 채집 수관의 겹침과 Trade 선반 다리의 검은 접합면, PRICE 글자 간섭이 최종 렌더에서 해소됐다.
- 선반: 빈 상판·가격표·다리와 버팀대가 구분된다. 뒤쪽 상판 가장자리가 참조 조명에서 매우 어둡게 보이며 실제 URP 화면의 normal/shadow 확인 대상이다.
- 체크포인트: 두 기둥·노란 문턱·빈 중앙 통행 구간 확인. 열매: 붉은 본체·짙은 잎과 낮은 polygon 실루엣 확인. 보트: 선체·승선판·P.A. 깃발 확인.
- [artifact-integrity.json](artifact-integrity.json): 최종 FBX/Blender/render/script/license SHA, export와 Unity 복사본 일치, 원본 해시 보존을 실제 파일로 검사했다.
- 이 문서의 검증은 Blender와 파일 무결성 범위다. Unity compile·wrapper·실제 gameplay flow·1920×1080 캡처는 해당 milestone 검증 보고서로 판정하며, 사람의 최종 미술 승인을 대신하지 않는다.
