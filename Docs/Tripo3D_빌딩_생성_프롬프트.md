# Tripo3D 빌딩 생성 프롬프트 시트

Project P.A.의 빌딩, 시설, 제작 스테이션을 Tripo3D에서 바로 생성하기 위한 복사 붙여넣기용 시트다.  
프롬프트에는 조형과 아트 방향만 넣고, 토폴로지와 품질 조건은 Tripo3D 설정에서 고정한다.

## 공통 생성 설정

### 빌딩 기본값

- 생성 모드: `HD 모델`
- 입력 탭: `텍스트 프롬프트`
- AI 모델: `v3.1 - 최고 품질`
- 텍스처: `ON`
- 고해상도 텍스처: `ON`
- PBR: `OFF`
- 토폴로지: `쿼드`
- 폴리곤 수: `10000`
- 울트라 메시 품질: `ON`
- 부분별 생성: `OFF`
- 개인정보 보호: `비공개`
- 포즈: `T-포즈 사용 안 함`

### 대형 빌딩 예외값

- 대상: `B-04 백화점`, `B-12 무역 항구 부두`
- 폴리곤 수: `15000`
- 나머지 설정은 빌딩 기본값과 동일

### 사용 주의

- 빌딩과 시설은 `스마트 메시`보다 `HD 모델`을 우선 사용한다.
- 프롬프트에 `all quad faces`, `watertight manifold`, `single solid object` 같은 제작 기술어를 길게 넣지 않는다.
- 문, 창문, 간판 글자는 실제 텍스트가 아니라 무늬나 빈 패널로 생성한다.
- 결과물이 너무 흐물거리면 같은 프롬프트로 재생성하고, 그래도 안 되면 폴리곤 수만 `12000-15000`으로 올린다.

## B-01 노점 상점

- 파일명 권장: `B01_MarketStall`
- 설정: `HD 모델 / 쿼드 / 폴리곤 10000 / 텍스처 ON / 고해상도 텍스처 ON / PBR OFF / 울트라 메시 품질 ON`

```text
low poly cozy 3D game building, stylized cartoon market stall for Project P.A., chunky toy-like silhouette, flat shaded matte diffuse, honey wood frame, cream beige cloth, sage green stripe accents, chocolate brown trim, soft painterly edges, earth tone palette #DCC8AB #A07850 #3E2A20 #9FBFA8,

small open-air wooden market stall, square counter shelf, two chunky corner posts, simple striped awning roof, two warm paper lantern shapes under the awning, two small barrels attached at one side, blank hanging sign panel above counter, readable from front and 3/4 view,

exterior prop only, closed construction with surface panels, flat bottom centered, scale 2x2x2.5 meters, isolated asset, terrain-free, character-free, readable-text-free, logo-free, base-plinth-free
```

## B-02 잡화점

- 파일명 권장: `B02_GeneralStore`
- 설정: `HD 모델 / 쿼드 / 폴리곤 10000 / 텍스처 ON / 고해상도 텍스처 ON / PBR OFF / 울트라 메시 품질 ON`

```text
low poly cozy 3D game building, stylized cartoon general store for Project P.A., chunky clean silhouette, flat shaded matte diffuse, cream stucco and honey wood facade, chocolate brown trim, warm terracotta roof, soft painterly edges, muted earth tones,

small cozy shop building, centered double wooden door, large square display window on the right, smaller window on the left, blank hanging sign board above door, shallow porch overhang, low pitched shingle roof, thick trim around roofline and base, friendly toy-like proportions,

closed exterior shell only, doors and windows are raised or painted surface panels on solid walls, flat bottom centered, scale 4x4x4 meters, isolated asset, terrain-free, character-free, readable-text-free, logo-free, base-plinth-free
```

## B-03 편의점

- 파일명 권장: `B03_ConvenienceStore`
- 설정: `HD 모델 / 쿼드 / 폴리곤 10000 / 텍스처 ON / 고해상도 텍스처 ON / PBR OFF / 울트라 메시 품질 ON`

```text
low poly cozy 3D game building, stylized cartoon convenience store for Project P.A., clean chunky silhouette, flat shaded matte diffuse, cream white stucco, sage mint accent stripe, chocolate brown trim, soft painterly edges, muted earth tone palette,

mid-size village mart, wide front facade, centered sliding double-door area as dark glass surface panel, large storefront window band on both sides, slim square pillars flanking entrance, flat roof with simple parapet, shallow striped awning strip above windows, neat base trim,

closed exterior shell only, doors and windows are surface panels on solid walls, flat bottom centered, scale 6x4x4.5 meters, isolated asset, terrain-free, character-free, readable-text-free, logo-free, base-plinth-free
```

## B-04 백화점

- 파일명 권장: `B04_DepartmentStore`
- 설정: `HD 모델 / 쿼드 / 폴리곤 15000 / 텍스처 ON / 고해상도 텍스처 ON / PBR OFF / 울트라 메시 품질 ON`

```text
low poly cozy 3D game building, stylized cartoon department store for Project P.A., clean chunky silhouette, flat shaded matte diffuse, cream stucco walls, warm terracotta mansard roof, chocolate brown wood trim, sage green fabric awnings, soft painterly edges, earth tone palette #DCC8AB #A07850 #3E2A20 #9FBFA8,

two-story boutique department store with clear front facade, wide centered double door, large display windows on both sides as dark glass panels, second floor has three evenly spaced arched windows, shallow balcony slab above entrance with simple rail bars, two roof dormers, small blank hanging shop sign, thick readable trim bands, symmetrical toy-like proportions,

exterior prop only, closed shell, doors and windows are painted or raised surface panels on solid walls, flat bottom centered, scale 8x6x7 meters, isolated asset, terrain-free, character-free, readable-text-free, logo-free, base-plinth-free
```

## B-05 기본 작업대

- 파일명 권장: `B05_BasicWorkbench`
- 설정: `HD 모델 / 쿼드 / 폴리곤 10000 / 텍스처 ON / 고해상도 텍스처 ON / PBR OFF / 울트라 메시 품질 ON`

```text
low poly cozy 3D game prop, stylized cartoon basic workbench for Project P.A., chunky readable silhouette, flat shaded matte diffuse, honey wood top, chocolate brown fixtures, cream beige highlights, soft painterly edges, muted earth tones,

rustic L-shaped wooden workbench station, thick tabletop and blocky legs, corner vise as attached solid block, upright back panel with pegboard pattern and simple tool silhouettes as flat inlays, small sturdy stool tucked under front side, scattered wood shaving shapes painted around the feet, all details visible from 3/4 view,

workshop station prop, closed solid construction with surface decorations, flat bottom centered, scale 2x2x1.5 meters, isolated asset, environment-free, character-free, readable-text-free, logo-free, base-plinth-free
```

## B-06 주방 스테이션

- 파일명 권장: `B06_KitchenStation`
- 설정: `HD 모델 / 쿼드 / 폴리곤 10000 / 텍스처 ON / 고해상도 텍스처 ON / PBR OFF / 울트라 메시 품질 ON`

```text
low poly cozy 3D game prop, stylized cartoon kitchen station for Project P.A., chunky readable silhouette, flat shaded matte diffuse, cream beige cabinet, terracotta brick stove, honey wood counter, chocolate brown trim, soft painterly edges,

cottage kitchen station, solid brick stove block with flat painted oven face, no open cavity, two pot shapes sitting on top as attached solid blocks, wooden counter beside stove with chopping board and rolling pin shapes, overhead shelf attached to back panel with plate and clay jar silhouettes, warm cabinet panels below,

workshop station prop, closed solid construction with surface panels only, flat bottom centered, scale 2x2x2 meters, isolated asset, environment-free, character-free, readable-text-free, logo-free, base-plinth-free
```

## B-07 대장간 스테이션

- 파일명 권장: `B07_ForgeStation`
- 설정: `HD 모델 / 쿼드 / 폴리곤 10000 / 텍스처 ON / 고해상도 텍스처 ON / PBR OFF / 울트라 메시 품질 ON`

```text
low poly cozy 3D game prop, stylized cartoon blacksmith forge for Project P.A., chunky readable silhouette, flat shaded matte diffuse, warm stone, terracotta ember paint, honey wood stump, chocolate brown metal fixtures, soft painterly edges,

blacksmith forge station, solid stone hearth block with flat painted orange fire glow on front face, no open fire cavity, blocky anvil mounted on tree stump beside it, back wall rack panel with tong and hammer silhouettes as flat inlays, small wooden quench bucket attached near anvil, sooty floor patch painted around base,

workshop station prop, closed solid construction with surface decorations, flat bottom centered, scale 3x2x2.2 meters, isolated asset, environment-free, character-free, readable-text-free, logo-free, base-plinth-free
```

## B-08 재봉 스테이션

- 파일명 권장: `B08_SewingStation`
- 설정: `HD 모델 / 쿼드 / 폴리곤 10000 / 텍스처 ON / 고해상도 텍스처 ON / PBR OFF / 울트라 메시 품질 ON`

```text
low poly cozy 3D game prop, stylized cartoon sewing workshop for Project P.A., chunky readable silhouette, flat shaded matte diffuse, light honey wood, cream beige fabric, dusty sage cloth rolls, chocolate brown trim, soft painterly edges,

pastel sewing station, sturdy table with vintage sewing machine silhouette as attached solid block on top, side shelf with rolled fabric cylinders, dressmaker mannequin as simple attached figure beside table, small button jar shape on counter, back panel with thread spool inlays, friendly craft-shop proportions,

workshop station prop, closed solid construction with surface details, flat bottom centered, scale 2x2x1.8 meters, isolated asset, environment-free, character-free, readable-text-free, logo-free, base-plinth-free
```

## B-09 창고

- 파일명 권장: `B09_StorageShed`
- 설정: `HD 모델 / 쿼드 / 폴리곤 10000 / 텍스처 ON / 고해상도 텍스처 ON / PBR OFF / 울트라 메시 품질 ON`

```text
low poly cozy 3D game building, stylized cartoon storage shed for Project P.A., chunky simple silhouette, flat shaded matte diffuse, honey wood plank walls, chocolate brown trim, warm terracotta roof, soft painterly edges, muted earth tones,

small wooden storage shed, centered double barn-door surface panel with painted iron strap hinges, vertical plank wall pattern, simple pitched gable roof, tiny square side window panel, two crates and one barrel attached flush to front wall, thick base trim, readable from front and 3/4 view,

closed exterior shell only, doors and windows are surface panels on solid walls, flat bottom centered, scale 4x3x3.5 meters, isolated asset, terrain-free, character-free, readable-text-free, logo-free, base-plinth-free
```

## B-10 NPC 집

- 파일명 권장: `B10_NpcHouse`
- 설정: `HD 모델 / 쿼드 / 폴리곤 10000 / 텍스처 ON / 고해상도 텍스처 ON / PBR OFF / 울트라 메시 품질 ON`

```text
low poly cozy 3D game building, stylized cartoon cottage house for Project P.A., cute chunky silhouette, flat shaded matte diffuse, cream stucco walls, warm terracotta roof, honey wood door, chocolate brown trim, dusty sage flower accents, soft painterly edges,

small cozy single-family cottage, centered wooden door with small porch overhang, round dormer window on roof, tiny chimney block, one front window with flower box beneath, short picket fence gate attached to one side, thick roofline and base trim, friendly toy-house proportions,

closed exterior shell only, doors and windows are surface panels on solid walls, flat bottom centered, scale 4x4x4 meters, isolated asset, terrain-free, character-free, readable-text-free, logo-free, base-plinth-free
```

## B-11 광장 중심 시설

- 파일명 권장: `B11_PlazaCenterpiece`
- 설정: `HD 모델 / 쿼드 / 폴리곤 10000 / 텍스처 ON / 고해상도 텍스처 ON / PBR OFF / 울트라 메시 품질 ON`

```text
low poly cozy 3D game outdoor structure, stylized cartoon village plaza centerpiece for Project P.A., chunky readable silhouette, flat shaded matte diffuse, warm stone beige, honey wood benches, dusty sage accents, soft painterly edges, muted earth tones,

central plaza centerpiece, low round stone fountain bowl with still blue-green water disc on top as flat surface, no water spray, four curved bench segments arranged around it, small square paving medallion under fountain, simple circular composition, all parts visually connected and centered for town square placement,

outdoor prop only, flat bottom centered, scale 4x4x1.5 meters, isolated asset, terrain-free beyond the small paving medallion, character-free, readable-text-free, logo-free, base-plinth-free
```

## B-12 무역 항구 부두

- 파일명 권장: `B12_TradePortPier`
- 설정: `HD 모델 / 쿼드 / 폴리곤 15000 / 텍스처 ON / 고해상도 텍스처 ON / PBR OFF / 울트라 메시 품질 ON`

```text
low poly cozy 3D game outdoor structure, stylized cartoon trade port pier for Project P.A., chunky readable silhouette, flat shaded matte diffuse, honey wood planks, chocolate brown posts, cream rope, soft painterly edges, muted earth tones,

small wooden trade-port pier, narrow rectangular walkway with thick plank seams, simple rope handrails on evenly spaced posts, two stout mooring bollards near far end, small rowboat tied flush to one side as attached decorative shape, blank cargo sign panel near entrance, readable from top and 3/4 view,

outdoor prop only, flat bottom centered at deck support level, scale 6x3x1 meters, isolated asset, water-free, terrain-free, character-free, readable-text-free, logo-free, base-plinth-free
```
