# PROJECT_PA_ASSET_MANIFEST

> AI가 이미 프로젝트에 들어 있는 자산을 빠르게 찾아 **실제 GameView에 사용하기 위한 작업용 자산 지도**.
>
> 이 문서는 완전한 카탈로그가 아니다. GitHub에 커밋된 경로에서 확인된 자산 + 로컬에서 반드시 추가 조사해야 하는 슬롯을 함께 관리한다.
>
> **로컬 프로젝트가 최종 권위다.** `.gitignore`/미커밋/최근 import 자산은 Codex가 시작 시 1회만 조사해 이 문서의 `LOCAL DISCOVERY`에 보강한다.

## 1. 사용 규칙

자산 선택 우선순위:

1. `Assets/Art/ProjectPA`
2. `Assets/Art/Character`
3. `Assets/Art/External/Quaternius`
4. `Assets/Art/External/Kenney`
5. `Assets/Art/Ultimate Nature Pack - Jun 2019`
6. `Assets/Models`
7. primitive/debug placeholder

AI는 새 primitive를 만들기 전에 이 manifest와 실제 AssetDatabase를 검색한다.

## 2. PLAYER / NPC CHARACTER

### Confirmed root
`Assets/Art/Character/`

### Confirmed models
- `C-01.fbx`
- `C-02.fbx`
- `C-03.fbx`
- `C-04.fbx`
- `C-05.fbx`
- `C-06.fbx`
- `C-07.fbx`
- `C-08.fbx`
- `C-09.fbx`

### Confirmed animation/controller
- `Idle.anim`
- `Walk.anim`
- `Axe.anim`
- `Chop.fbx`
- `Sit.anim`
- `PlayerAnimator.controller`
- `NpcAnimator.controller`
- `tripo_convert_c58a6956-65c7-4884-ba45-9fb7e8bda216@Walking.fbx`

### Intended use
- Player/NPC actual visible model
- Idle/locomotion
- axe/chop action
- companion/NPC ambient motion

## 3. TOOLS

### Quaternius CubeWorldKit root
`Assets/Art/External/Quaternius/CubeWorldKit/Models/`

### Confirmed
- `Axe_Stone.fbx`
- `Pickaxe_Stone.fbx`

### Intended use
- Hotbar selected tool의 held model
- axe/pickaxe action presentation
- pickup/kit preview가 필요한 경우 실제 모델 기반

### Needs local discovery
- fishing rod
- net
- shovel
- watering can
- other tool meshes
- hand socket/prefab bindings

없다면 기존 ProjectPA/Models 전체에서 이름 변형 검색:
`Rod`, `Fishing`, `Net`, `Bug`, `Tool`, `Shovel`, `Water`, `Pick`, `Axe`.

## 4. SUPPLY / ARRIVAL

### Confirmed
`Assets/Art/External/Quaternius/CubeWorldKit/Models/Chest_Closed.fbx`

`Assets/Art/External/Quaternius/CubeWorldKit/Models/Chest_Open.fbx`

### Intended use
- P.A. supply crate
- opening reward chest
- tutorial starter equipment source

추천 연출:
- 도착 지점 근처에 실제 chest 배치
- interact → opened model swap
- required tools/kits 지급
- sellable gather resource는 지급하지 않음

## 5. WORLD / NATURE

### Confirmed root
`Assets/Art/Ultimate Nature Pack - Jun 2019/FBX/`

### Confirmed family example
- `BirchTree_1.fbx`
- `BirchTree_2.fbx`
- `BirchTree_3.fbx`
- `BirchTree_4.fbx`
- `BirchTree_5.fbx`

### Intended use
- forest canopy
- meadow edge
- landmark tree clusters
- environmental breakup
- visual biome differentiation

### Studio placement rule
- 3~6종 family mixing
- scale jitter
- yaw jitter
- clump + clearing 구조
- 플레이 경로 주변 silhouette 조절
- landmark cluster
- interaction resource와 pure decoration 구분

### Needs local discovery
FBX 폴더 전체에서:
- rock
- grass
- flower
- bush
- stump
- log
- mushroom
- pine
- dead tree
- plant

를 1회 inventory한다.

## 6. QUATERNIUS WORLD

### Root
`Assets/Art/External/Quaternius/`

### Confirmed packs
- `CubeWorldKit/`
- `CuteFish/`

### Intended use
CubeWorldKit:
- tools
- crate/chest
- possible world props

CuteFish:
- fishing visuals
- catch representation
- coastal life

## 7. KENNEY

### Root
`Assets/Art/External/Kenney/`

### Confirmed packs
- `FoodKit/`
- `MiniMarket/`

### Intended use
FoodKit:
- shop stock display
- food category visual

MiniMarket:
- B01 Shop dressing
- shelf/counter/sign
- checkout/retail visual language
- market clutter

상점은 기존 `ShopSlot` authority를 유지하면서 시각 구조만 Kenney/ProjectPA 자산으로 감싼다.

## 8. PROJECT P.A. ORIGINAL / DERIVED ART

### Root
`Assets/Art/ProjectPA/`

### Confirmed folders
- `Buildings/`
- `Derived/`
- `Materials/`
- `Prefabs/`

### Intended use
- Management Hub
- B01 Shop
- settlement pieces
- Project P.A. specific materials
- custom derived props/prefabs

동일 기능의 ProjectPA prefab과 generic external asset이 둘 다 있으면 **ProjectPA를 우선**한다.

### Needs local discovery
`Hub`, `Shop`, `Stall`, `Tent`, `Shelter`, `Building`, `Crate`, `Sign`, `Shelf`, `Counter`, `PA_` 키워드로 inventory한다.

## 9. LEGACY / OTHER MODEL ROOT

### Confirmed
`Assets/Models/Buildings/`

사용 전에:
- 실제 scene reference
- prefab reference
- style compatibility
확인.

## 10. UI

### Confirmed root
`Assets/Art/UI/`

### Needs local discovery
- hotbar
- inventory slot
- button
- interaction icon
- currency
- item category
- dialogue bubble
- phone
- shop
- sale feedback

새 UI framework를 만들지 않고 기존 HUD/Inventory/Hotbar/Smartphone 구조에 붙인다.

## 11. MARKET

### Confirmed root
`Assets/Art/Market/`

### Needs local discovery
- shelves
- store props
- signs
- stalls
- product display
- decorations

B01 Shop presentation pass 전에 반드시 조사한다.

## 12. FONTS / MATERIALS

### Confirmed roots
- `Assets/Fonts/`
- `Assets/Materials/`
- `Assets/Art/ProjectPA/Materials/`

새 material을 마구 증식하지 않는다.

## 13. 현재 권장 Asset Mapping

| Experience | Preferred assets |
|---|---|
| Player/NPC | `Art/Character/C-*` + existing Animator clips |
| Axe | Quaternius `Axe_Stone.fbx` |
| Pickaxe | Quaternius `Pickaxe_Stone.fbx` |
| Supply crate | Quaternius `Chest_Closed/Open.fbx` |
| Forest | Ultimate Nature Pack FBX family |
| Fishing | Quaternius `CuteFish` |
| Shop shell | ProjectPA Buildings/Prefabs first |
| Shop interior/dressing | Kenney MiniMarket + Market |
| Sellable food visuals | Kenney FoodKit |
| Hub/settlement | ProjectPA first |
| UI | existing Art/UI + existing HUD system |

## 14. LOCAL DISCOVERY — 시작 시 한 번만 수행

```text
[ ] player prefab exact path
[ ] NPC prefabs exact paths
[ ] actual held-tool socket / hand bone
[ ] all tool meshes
[ ] fishing rod
[ ] net
[ ] fish models
[ ] tree/rock/grass/flower families
[ ] Management Hub prefab
[ ] B01 Shop prefab
[ ] shelf/counter/sign prefabs
[ ] shop item display prefabs
[ ] existing particles
[ ] existing audio clips
[ ] existing post-processing/lighting assets
[ ] existing HUD prefabs
[ ] existing interaction prompt
[ ] existing sale feedback
[ ] existing camera rig
```

결과는 이 문서 끝에 **경로만 간단히 추가**한다.

## 15. Placeholder Policy

Debug primitive를 사용할 수 있는 경우:
- invisible collision
- temporary validator fixture
- non-player-facing helper
- one-session diagnostic

Player-facing final slice에는 visible cube tree, capsule NPC, flat gray building, debug world label, giant test text를 남기지 않는다.

남을 수밖에 없다면 최종 보고에서 `PLACEHOLDER DEBT`로 명시한다.
