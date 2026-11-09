# Project P.A. Asset Library

ART-000 reference library: `ProjectPA_AssetLibrary.blend`, Blender 4.5.13 LTS. 원본은 append/import한 사본이며 외부 텍스처는 library 안에 packed했다. 생성 파일은 Git 관리 대상이다. `Sources/`의 사본과 전체 source ZIP은 Git 제외 경로다.

`library-manifest.json`에 33종 source path/hash, purpose, gameplay owner, Unity 대응 경로가 있다. 아래 6개 collection 아래의 개별 `PA_REF_*` collection은 Asset Browser asset으로 표시되어 있다. 기본 viewport/render는 숨겨 두었다. 원하는 asset collection을 append하거나 instance하여 파생 작업을 시작한다.

- `PA_REF_KENNEY_MARKET`: 5
- `PA_REF_KENNEY_FOOD`: 9
- `PA_REF_KENNEY_ARCADE`: 2
- `PA_REF_Q_FISH`: 4
- `PA_REF_Q_NATURE`: 8
- `PA_REF_Q_CUBE`: 5

`PA_DERIVED_RESERVED`는 아직 비어 있다. ART-000은 파생 제작 계획까지이며 새 mesh 생산은 후속 티켓에 있다. 생성한 library를 다시 열어 33 asset, source hash, gameplay owner, packed images와 비교 render를 검증했다. 근거는 `Docs/AssetProvenance/library-reopen-validation.json`이다.

재현 도구:

```powershell
python Tools/Art/asset_intake.py
# 위 출력과 asset-inventory.json의 archive/license/classification을 검토한 뒤:
python Tools/Art/asset_intake.py --import-selected
& 'ExternalAssetSources/Tools/Blender/Portable/blender-4.5.13-windows-x64/blender.exe' --background --factory-startup --disable-autoexec --python-exit-code 1 --python 'Blender/Scripts/build_asset_library.py'
```

기존 library가 있으면 생성기는 overwrite를 거부한다. 현재 결과를 쓸 때는 재생성하지 않는다. 새 source 선정/코드 수정으로 재생성이 필요한 경우 새 versioned library 출력을 정한 뒤 진행한다. 원본 BLEND/FBX/ZIP을 덮어쓰지 않는다.

원본 참조 렌더와 재개방 검증:

```powershell
& 'ExternalAssetSources/Tools/Blender/Portable/blender-4.5.13-windows-x64/blender.exe' --background 'Blender/Library/ProjectPA_AssetLibrary.blend' --disable-autoexec --python-exit-code 1 --python 'Blender/Scripts/verify_and_render_library.py'
```

위 검증은 library를 다시 저장하지 않는다. 실제 원본/생성 모델의 구분, 라이선스, Unity scale/앞면/owner 연결은 `Docs/AssetProvenance/`와 루트 Style Grammar를 따른다. Blender 공식 도구의 URL/SHA256은 `blender-toolchain.json`에 있으며 Unity Packages에는 추가하지 않았다.
