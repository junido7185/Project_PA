# VS-PRESENT-001-P3 — First Settlement assets

- 신규 구조물: `PA_SettlementHub_Tier0` (반개방형 물류 거점), `PA_StarterShelter` (공용 임시 거처). 거처는 P1 확정 직업의 색상 표식만 달라진다.
- 제작: `Blender/Scripts/build_settlement_assets.py`를 Blender 4.5.13 CLI로 실행. 기존 `ProjectPA_AssetLibrary.blend`의 `PA_REF_CUBEWORLDKIT_CHEST_CLOSED` 메시를 실제 조합했다. 원본 라이브러리 실행 전후 SHA256 일치.
- 출처: ART-000 `selected-assets.json`의 Quaternius CubeWorldKit Chest_Closed, 기존 CC0 intake 기록을 재사용. 새 외부 다운로드 없음. 구조물 자체는 프로젝트 신규 제작이며 캐릭터·생산 시스템 추가 없음.
- Blender 작업 파일: `Blender/Generated/VS_PRESENT_001_P3/PA_FirstSettlement_r01.blend`. 두 FBX의 해시·치수·삼각형 수·팔레트·reference render 경로는 `derived-assets.json` 참조.
- 규격: 지면 중앙 pivot, Unity wrapper 전면 -Z, 2×2 cell / 4×4m footprint, 외부 입구 1 cell (2m). 전면은 거점의 작업대 및 거처의 개구부다. Unity BoxCollider는 footprint 내부에 둔다.
- Unity wrapper: `Assets/Resources/DepartureTutorial/Settlement/`. 실제 기존 BuildingData와 WorldBuildingPlacementService로 생성한다. 재질은 DepartureTutorial의 ART-000 파생 팔레트를 재사용한다.
- Reference render: `PA_SettlementHub_Tier0_r01.png`, `PA_StarterShelter_r01.png` (1200×900), 직접 시각 확인 완료. Unity Setup.log WRAPPER_PASS 두 건 및 SETUP_PASS.
- 별도 운영/보관 기능은 없다. 거점은 P3 첫 정착 표시와 배치 대상이며 기존 B09 창고의 내용물/저장 기능을 복제하지 않는다.

- Unity 최종 시각 교정: 얇은 canvas의 backface 누락을 PA_ShelterCanvas(_Cull=0) 전용 파생 재질로 해결. 기존 FBX와 Blender SHA256 그대로 유지. D3D11-07 실제 저장 복원 화면에서 확인했다.
