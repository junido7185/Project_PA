# ART-000 Completion Audit

2026-09-06 · 범위: External Asset Intake, Style Grammar and Production Pipeline.

목표 파일 `goal-objective.md`의 ACTIVE TICKET은 ART-000이며 §17은 대규모 씬 적용을 제외하고 §21은 ART-001~010 자동 구현을 금지한다. 따라서 이 판정은 입고·측정·library·isolated import·제작 계획의 완료이며 최종 Vertical Slice Demo 아트 완성을 뜻하지 않는다.

## 요구별 근거

| 목표 절 | 요구 | 현재 근거 / 판정 |
|---:|---|---|
| 1 | Downloads에서 6개 팩 탐색, 내부 식별, 안전 압축 해제 | registry의 실제 ZIP 이름/해시/원본 구조. 6개 모두 탐색; CRC·경로·크기 검사 뒤 staging, 그 뒤 선별 Unity 복사. |
| 2 | master source COPY, 변경·삭제·rename 금지 | `verify_intake.py`가 master와 중복 다운로드, staged archive와 2,590개 extracted file SHA256을 대조. PASS. |
| 3 | Source/Blender/Unity/provenance 구조 | ExternalAssetSources 6팩, Blender Source/Library/Generated/Export/Scripts, Assets External과 ProjectPA Derived/Materials/Prefabs, Docs/AssetProvenance. Nature는 기존 경로/GUID 재사용, reference-only Arcade는 Unity 미입고. |
| 4 | 원본 archive Git 크기 관리 | 시작 Git 39.22+855.04 MiB 확인. source와 portable tool은 .gitignore. 최종 packed library와 runtime 후보만 추적. |
| 5 | pack/version/date/URL/license/SHA256/formats/usage/derivative 기록 | `EXTERNAL_ASSET_REGISTRY.md`, `asset-inventory.json`, 원본 license 5개 사본. Cube license 파일 부재와 공식 페이지 CC0 증거 명시. |
| 6 | 모든 형식·모델·texture/material·README/license/preview/animation 조사와 분류 | 550종 `MODEL_INVENTORY.md`, 2,590 파일 inventory, `format-audit.json`. FBX animation metadata와 glTF animation arrays 조사. Cube의 Button은 OBJ/BLEND에만 존재함을 확인. |
| 7 | 6팩 역할, Cube terrain 비대체 | registry와 production plan. Cube terrain/캐릭터/전투 모델은 Unity 입고하지 않음. WorldGrid source hash 유지. |
| 8 | PA 공통 미술 방향 | 루트 `PROJECT_PA_ART_STYLE_GRAMMAR.md`, Kenney 가구와 Nature 형태 관찰, PA 팔레트 계약. |
| 9 | 선정 모델 style 측정 | Blender/OBJ polygon·density, smooth face, live bevel, bounds, sampled color/saturation, roughness/metallic, texture 의존, source contact shadows. 원본 primitive 생성 비율·Unity 최종 조명/face는 측정 불가/후속 확인으로 명시. |
| 10 | Blender library와 namespace | packed `Blender/Library/ProjectPA_AssetLibrary.blend`, 6 reference collections·33 Asset Browser collections, PA_DERIVED_RESERVED. 파일 재개방 PASS. |
| 11 | Blender CLI/Python 자동화, 원본 보존 | 공식 4.5.13 portable SHA256 검증, 3개 Blender scripts. 원본은 read-only, 이전 생성 library는 Generated/ART000에 보존. |
| 12 | ID/크기/pivot/face/footprint/clearance/collider/NavMesh/owner/material 계약 | `selected-assets.json`, `SELECTED_ASSET_SPECIFICATIONS.md`, 28 Unity wrapper 실측. 실제 배치/상호작용/저장 권위는 기존 owner에 남음. |
| 13 | 선별 Unity import와 wrapper 검사 | 신규 FBX 20개+기존 Nature 8개; wrapper 28개, 공유 URP material 20개. D3D11 hash/mesh/bounds/pivot/size/reference/prefab reload PASS. Tuna rig/6 clips 존재 확인. |
| 14 | gameplay 중복 구현 금지 | 새 코드는 Editor intake validator와 authoring scripts만. existing runtime source/씬/설정 158개 hash 무변경. 새 Inventory/Economy/Shop/Save authority 없음. |
| 15 | Canon/campaign/demo와 실제 구현의 gap 분석 | `DEMO_ASSET_REQUIREMENTS_AND_PRODUCTION_PLAN.md`의 27개 요구 행. 현재 캐논·Day1–30 설계·기존 Resources와 실제 클래스 기준. |
| 16 | CAN_DERIVE/NEED_NEW_MODEL의 Blender 생산 계획, P0~P3 | 8개 production batch, 구조적 변경/기능 이유/입력/결과/검증/담당 ART 티켓 명시. mesh 제작은 이번 범위 밖. |
| 17 | isolated test 허용, Golden/MainGame 대규모 수정 금지 | Editor preview scene에서 생성·prefab round-trip 후 폐기. 어떤 .unity도 저장하지 않음. |
| 18 | 원본 보호·meta-preserving 이동 | Downloads move/rename/delete 없음, 기존 Unity 자산 이동 없음. Nature 8개 GUID 유지. Unity가 새 .meta 생성. |
| 19 | runtime/source 형식 하나 선택·중복 검사 | Kenney source GLB, Quaternius source BLEND, Unity FBX. source duplicate와 palette dependency를 검사. 라이브러리 물고기 source 동작 6개씩 모두 보존/연결. |
| 20 | Git review·archive 제외·승인된 local commit만 | ART 전용 경로 manifest와 diff/large-file/meta/GUID 검사. 별도 CONTENT staging 및 사용자 dirty 파일은 커밋에 포함하지 않는 것이 원칙. push/이력 재작성/파일 삭제 없음. |
| 21 | 다음 ART 티켓은 권장만, 자동 구현 금지 | 다음 권장 ART-001. 별도 CONTENT milestone 승인은 보존; 그 작업을 이 ART 대화가 재실행하지 않음. |
| 22 | 최종 산출물 17종과 사용자 작업 | 아래 대응표와 registry/grammar/library/production plan. 수동 원본 이동·Unity import 요구 없음. |

## 최종 산출물 17종

| 번호 | 산출물 | 파일/근거 |
|---:|---|---|
| 1~3 | 6개 archive, SHA256, 압축 해제 위치 | `EXTERNAL_ASSET_REGISTRY.md`, `asset-inventory.json` |
| 4 | 모델/texture/material inventory | `MODEL_INVENTORY.md`, `format-audit.json`, `asset-inventory.json` |
| 5~7 | 직접/보정 후보, 파생 원본, 비사용 분류 | `selected-assets.json`, 전체 MODEL_INVENTORY |
| 8 | provenance registry | `EXTERNAL_ASSET_REGISTRY.md`, `Licenses/` |
| 9 | Project PA Style Grammar | 루트 `PROJECT_PA_ART_STYLE_GRAMMAR.md`, `STYLE_MEASUREMENTS.md` |
| 10 | Blender library | `Blender/Library/ProjectPA_AssetLibrary.blend`, manifest/README |
| 11 | Unity import 구조 | `Assets/Art/External/`, `Assets/Art/ProjectPA/`, 기존 Nature; `unity-validation.json` |
| 12~14 | demo requirements, missing list, Blender production plan | `DEMO_ASSET_REQUIREMENTS_AND_PRODUCTION_PLAN.md` |
| 15 | Git 영향 | 최종 후보 약 24.4 MB, 단일 최댓값 library 약 3.6 MB; source/tool archive는 제외. 정확한 값은 `git-review.json` |
| 16 | 다음 ART | ART-001, 이번에 자동 활성화하지 않음 |
| 17 | 사용자 작업 | 수동 copy/import 불필요. 이후 범위 선택과 최종 게임 화면의 사람 시각 판단만 남음 |

## 검증 결과와 한계

- Runtime/Editor compile: 오류 0. 새 validator가 Editor csproj에 포함됐고 Unity executeMethod가 실제 실행됨. 기존 CS8785/CS0414 경고는 남는다. `Logs/ART000/Editor_Compile.log`.
- Unity D3D11: `Logs/ART000/Unity_Intake_01.log`의 `ART000_INTAKE_PASS assets=28`. 실행 초기 licensing access-token 갱신 오류가 있었으나 기존 entitlement로 import/검증/정상 종료했다. Console/log가 완전히 무경고라는 주장은 하지 않는다.
- Blender: `Blender_Library_AnimationComplete.log`, `Blender_Animation_Audit_Final.log`, `Blender_ReferenceRender_Final.log` 모두 PASS. 원본별 animation set과 최종 library 재개방/packed pixels/33 renders 확인.
- 무결성: `final-integrity-report.json` PASS, 2,886 checks, Assets GUID 1,199개 중 중복0, 새 scope missing meta0, 보호 hash 변경0.
- 시각 증거: 원본 33장과 contact sheets 3장 직접 검사. 최종판의 물고기 source render도 다시 확인했다. 최종 Unity scene/1920×1080/day-night shadows/interaction face/실제 loop/save-load/standalone 완주는 이번 작업에서 확인 못 함. 후속 ART-001 이후 격리/통합 validation과 사람 시각 판단이 필요하다.
- 완성 게임/캐논/콘텐츠/경제/저장 부채를 이 체크포인트로 완료 처리하지 않는다. ART-000에서 생산한 파생 mesh 수는 0이다.
