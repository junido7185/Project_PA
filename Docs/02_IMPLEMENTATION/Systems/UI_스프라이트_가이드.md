# 🎨 P.A. UI 스프라이트 제작 가이드 (Nano Banana 프롬프트 포함)

> **용도**: `PA_UIBuilder.cs` 가 자동 생성한 UI 계층에 스프라이트를 주입하기 위한 에셋 제작 가이드.
> **타겟 렌더러**: Gemini 2.5 Flash Image (Nano Banana). 각 스프라이트는 투명 배경 PNG 로 출력 받는다.
> **레퍼런스**: `Docs/레퍼런스/동물의 숲 인벤토리.jpg`, `데이브 더 다이버 스마트폰 토글 전/후.*`
> **공통 아트 톤**: 캐주얼 / 벡터풍 / 부드러운 파스텔 / 굵은 외곽선 없음 / 2D 평면 음영

---

## 1. 스프라이트 마스터 리스트

| # | 파일명 | 용도 / 배정 위치 | 권장 해상도 (px) | 9-Slice (L,R,T,B) | Pivot | 특이 사항 |
|---|--------|-----------------|------------------|-------------------|-------|---------|
| **인벤토리 (동물의 숲)** |
| 1 | `ui_inventory_panel.png` | InventoryPanel → Background | 512×200 | 100,100,0,0 | (0.5, 0.5) | 완전 둥근 양 끝 (캡슐), 순백색, 얇은 섀도 포함 |
| 2 | `ui_inventory_label.png` | InventoryPanel → SelectedItemLabel | 256×64 | 32,32,0,0 | (0.5, 0.5) | 작은 캡슐, 민트 그린 (#80D0AD) |
| 3 | `ui_dropshadow.png` | InventoryPanel → Shadow (범용) | 512×256 | 96,96,96,96 | (0.5, 0.5) | 부드러운 검정 Gaussian 블러 |
| **핫바 / 슬롯** |
| 4 | `ui_hotbar_bg.png` | HotbarRoot → 배경 | 512×96 | 40,40,0,0 | (0.5, 0.5) | 어두운 반투명 캡슐 (#000 35%) |
| 5 | `ui_slot.png` | 모든 슬롯 프리팹 배경 | 128×128 | 24,24,24,24 | (0.5, 0.5) | 샌드 베이지 (#EFE4D0), 둥근 사각형 |
| 6 | `ui_slot_selected.png` | 핫바 선택 슬롯 하이라이트 | 128×128 | 28,28,28,28 | (0.5, 0.5) | 노란 테두리 (#F5D76E) + 발광 |
| **스마트폰 (데이브 더 다이버)** |
| 7 | `ui_phone_body.png` | SmartphoneContainer 루트 | 512×1024 | 120,120,120,120 | (0.5, 0.5) | 코랄 레드 (#E85656), 둥근 사각형, 양 끝 radius 큼 |
| 8 | `ui_phone_bezel.png` | PhoneBezel | 512×1024 | 110,110,110,110 | (0.5, 0.5) | 검정 (#141414), 내부가 투명 (도넛형) |
| 9 | `ui_phone_screen.png` | PhoneScreen | 400×800 | 40,40,40,40 | (0.5, 0.5) | 핑크→코랄 세로 그라데이션 월페이퍼 |
| 10 | `ui_phone_statusbar.png` | StatusBar | 400×48 | 20,20,0,0 | (0.5, 0.5) | 반투명 어두운 띠 |
| 11 | `ui_app_icon_base.png` | 각 AppIcon 배경 | 128×128 | 24,24,24,24 | (0.5, 0.5) | 둥근 사각형 단색 (런타임에 색 설정) |
| **앱 아이콘 (4개 필수)** |
| 12 | `icon_app_audit.png` | 감사 앱 | 128×128 | — | (0.5, 0.5) | 📊 바 차트, 파란색 배경 (#63B6EC) |
| 13 | `icon_app_hiring.png` | 채용 앱 | 128×128 | — | (0.5, 0.5) | 🤝 악수, 주황 배경 (#F2A361) |
| 14 | `icon_app_feed.png` | 피드 앱 | 128×128 | — | (0.5, 0.5) | 📱 SNS 말풍선, 보라 배경 (#9882E4) |
| 15 | `icon_app_settings.png` | 설정 앱 | 128×128 | — | (0.5, 0.5) | ⚙ 톱니바퀴, 회색 배경 (#808080) |
| **부가 HUD (선택)** |
| 16 | `ui_money_capsule.png` | 화폐 카운터 | 256×64 | 32,32,0,0 | (0.5, 0.5) | 흰색 캡슐 + 금색 1px 테두리 |
| 17 | `ui_avatar_button.png` | 캐릭터 버튼 (동숲) | 96×96 | — | (0.5, 0.5) | 흰 원형 버튼 + 어두운 캐릭터 실루엣 |

### Unity Import 설정 공통

- **Texture Type**: `Sprite (2D and UI)`
- **Sprite Mode**: `Single`
- **Pixels Per Unit**: `100`
- **Filter Mode**: `Bilinear`
- **Compression**: `None` (UI 는 압축 금지 — 모서리 뭉개짐 방지)
- **Mesh Type**: `Full Rect` (9-slice 안전)
- 9-slice 있는 항목은 Sprite Editor 에서 Border L/R/T/B 값을 위 표대로 입력

---

## 2. Nano Banana (Gemini 2.5 Flash Image) 공통 시스템 프롬프트

각 스프라이트 요청 전 아래 블록을 공통 헤더로 먼저 입력:

```
You are generating 2D game UI sprites for a cozy Animal Crossing × Dave the Diver style farm/economy simulation.
Art direction:
- Flat vector look, soft pastel palette, no hard black outlines, subtle inner shading only.
- Rounded geometry, friendly casual tone, no text inside the sprite unless requested.
- Output as transparent background PNG at exact requested resolution.
- Composition must fit fully within the canvas with 4-8 px safe margin on all sides.
- Do NOT add logos, watermarks, or random decorative elements.
- Lighting: top-down soft directional, no drop shadow baked in unless specified.
```

---

## 3. 스프라이트별 개별 프롬프트

각 항목은 위 공통 헤더 뒤에 이어붙여 사용한다.

### 🟢 #1 `ui_inventory_panel.png` (512×200)

```
Create a pure white horizontal capsule-shaped UI panel, 512×200 px, transparent background.
- Fully rounded ends (border-radius = 100 px, i.e. half of height).
- Slight inner cream tint (#FFFEFB) with no gradient.
- Very subtle 2 px soft drop shadow extending 4 px below the bottom edge.
- Absolutely flat, no inner border, no texture, no gloss.
- 9-slice friendly: left 100 px and right 100 px will be preserved as corners,
  center 312 px will stretch horizontally, so keep the middle strip visually uniform.
- Inspired by Animal Crossing: New Horizons inventory bubble.
```

### 🟢 #2 `ui_inventory_label.png` (256×64)

```
Small mint-green pill-shaped label, 256×64 px, transparent background.
- Solid fill #80D0AD.
- Fully rounded ends (radius 32).
- Subtle 1 px lighter rim along the top edge.
- No text, no icon. Plain flat capsule only.
- Inspired by the teal "selected item" tag floating above the Animal Crossing inventory.
```

### 🟢 #3 `ui_dropshadow.png` (512×256)

```
Soft radial drop shadow, 512×256 px, transparent PNG.
- Centered black ellipse with Gaussian blur radius ~60 px.
- Maximum opacity 40% at center, fading smoothly to 0% at edges.
- No hard edge whatsoever. 9-slice safe — central region is uniform gray fade.
- Pure shadow, no object, no color tint.
```

### 🟢 #4 `ui_hotbar_bg.png` (512×96)

```
Horizontal translucent dark capsule, 512×96 px, transparent PNG.
- Fill: black at 35% alpha (#000000 @ 0.35).
- Fully rounded ends (radius 48).
- Very thin 1 px lighter inner highlight along the top edge.
- No texture. Flat, cozy, minimal.
- Will be used as the bottom-screen hotbar background for a farm game.
```

### 🟢 #5 `ui_slot.png` (128×128)

```
Rounded square inventory slot background, 128×128 px, transparent PNG.
- Fill: warm sand beige #EFE4D0.
- Border radius 22 px (about 17% of side).
- Subtle inner 2 px darker beige border (#D4C5A9) for definition.
- Tiny top highlight of 1 px lighter cream to suggest soft lighting.
- No text, no icon, no inset item. Flat vector look, Stardew Valley / AC:NH friendly.
```

### 🟢 #6 `ui_slot_selected.png` (128×128)

```
Highlighted/selected inventory slot, 128×128 px, transparent PNG.
- Base the same as ui_slot.png (sand beige #EFE4D0, radius 22).
- Add a bold 4 px golden yellow border (#F5D76E) along the inside edge.
- Add a soft outer glow halo of #F5D76E with 8 px blur and 60% opacity
  extending outside the rounded square.
- Keep inner area flat beige so item icons render cleanly on top.
```

### 🔴 #7 `ui_phone_body.png` (512×1024)

```
Smartphone body back plate, 512×1024 px, transparent PNG.
- Solid coral red fill #E85656.
- Rounded-rectangle silhouette, border radius 120 px (continuous/superellipse feel).
- Very subtle vertical glossy sheen from top (5% white) fading to bottom.
- One tiny 2 px darker edge line along the rim for depth.
- No camera cutouts, no buttons, no text. Pure body silhouette.
- Inspired by the Dave the Diver in-game phone, cartoon/vector style.
```

### 🔴 #8 `ui_phone_bezel.png` (512×1024) — 선택

```
Smartphone bezel frame (donut shape), 512×1024 px, transparent PNG.
- Outer rounded rectangle (radius 110) filled with near-black #141414.
- Inner cutout (radius 90), fully transparent — the screen will show through this hole.
- Frame thickness approximately 14 px uniform.
- Absolutely no other decoration.
- This sits on top of ui_phone_body.png to create a thin bezel around the screen.
```

### 🔴 #9 `ui_phone_screen.png` (400×800)

```
Vertical gradient wallpaper for a phone screen, 400×800 px, transparent PNG.
- Smooth top-to-bottom gradient: top #F57490 (pink coral) → bottom #E0587A (deep rose).
- Slight diffused soft light bloom in the upper third (white @ 8%).
- Rounded corners (radius 40) — outside the rounding is transparent.
- No icons, no widgets. Pure wallpaper.
- Dave the Diver mobile app home-screen reference.
```

### 🔴 #10 `ui_phone_statusbar.png` (400×48)

```
Phone status bar background, 400×48 px, transparent PNG.
- Translucent dark strip (#000000 @ 15%).
- Slight inner top highlight line (white @ 10%, 1 px).
- Rounded only on bottom corners? No — fully rectangular (will sit at top of screen).
- No icons, no text — plain band only.
```

### 🔴 #11 `ui_app_icon_base.png` (128×128)

```
Neutral-colored rounded square tile for app icons, 128×128 px, transparent PNG.
- Pure white fill (#FFFFFF) — will be tinted at runtime via Unity Image.color.
- Border radius 28 px.
- Very soft inner glossy gradient (top 10% whiter, bottom 5% darker) so runtime tint feels 3D.
- No icon content — this is a blank tile base for overlaying glyphs.
```

### 🟠 #12 `icon_app_audit.png` (128×128)

```
Casual flat icon: a minimalist bar chart rising to the right, centered on a rounded square.
- 128×128 px, transparent PNG.
- Tile background: #63B6EC (sky blue), radius 28.
- Foreground: 3 or 4 white vertical bars of increasing height, clean geometry, no outlines.
- No text. Playful and readable at 48×48 preview.
- Inspired by smartphone audit/stats apps.
```

### 🟠 #13 `icon_app_hiring.png` (128×128)

```
Casual flat icon: a handshake emblem centered on a rounded square tile.
- 128×128 px, transparent PNG.
- Tile background: #F2A361 (warm orange), radius 28.
- Foreground: two stylized cream-white hands clasped in a friendly handshake, simple vector shapes.
- No fingers detail, no text. Readable at 48×48.
- Represents NPC hiring in a farm sim.
```

### 🟠 #14 `icon_app_feed.png` (128×128)

```
Casual flat icon: a chat/feed bubble with three small dots, centered on a rounded square tile.
- 128×128 px, transparent PNG.
- Tile background: #9882E4 (soft purple), radius 28.
- Foreground: one rounded speech bubble in white with three small cream dots inside, tail bottom-left.
- No text.
- Represents a social feed / message app.
```

### 🟠 #15 `icon_app_settings.png` (128×128)

```
Casual flat icon: a chunky gear wheel centered on a rounded square tile.
- 128×128 px, transparent PNG.
- Tile background: #808080 (neutral gray), radius 28.
- Foreground: one white gear with 8 teeth and a hollow center, simple vector geometry.
- No text, no extra ornaments.
- Represents device settings.
```

### 🟡 #16 `ui_money_capsule.png` (256×64) — 선택

```
Small white horizontal capsule with a thin gold border, 256×64 px, transparent PNG.
- Fill: #FFFFFF.
- 2 px golden border (#E8B949) fully rounded.
- Border radius 32. No icon, no text.
- Animal Crossing money-counter style, pairs with ui_avatar_button.png.
```

### 🟡 #17 `ui_avatar_button.png` (96×96) — 선택

```
Circular white button with a small dark character-head silhouette, 96×96 px, transparent PNG.
- Circle fill: #FFFFFF, radius = canvas.
- 2 px light gray inner rim (#E0D9C7).
- Centered dark silhouette of a chibi character head (coral-brown #6B4A3F), filling about 55% of the circle.
- No facial features, pure silhouette.
- Style matches Animal Crossing HUD buttons.
```

---

## 4. 생성 후 Unity 배치 체크리스트

> **🚀 자동 주입**: `PA_UIBuilder.cs` 의 `TryLoadSprite()` / `ApplySprite()` 가 아래 경로들을 **자동 탐지**한다. 파일명만 표와 일치시키면 재빌드 시 자동으로 붙는다. (미발견 시 컬러 플랫으로 안전하게 폴백)

1. `Assets/Art/UI/` 폴더에 모든 PNG 배치 (폴더 없으면 생성)
2. 파일명은 표의 `파일명` 컬럼 그대로 사용 (예: `ui_inventory_panel.png`, `icon_app_audit.png`)
3. 각 스프라이트 선택 → Inspector:
   - Texture Type = **Sprite (2D and UI)**
   - Pixels Per Unit = **100**
   - Compression = **None**
   - `Sprite Editor` 진입 → Border 값을 표의 9-Slice 컬럼대로 입력 → Apply
4. Unity 메뉴 `P.A. System > Build UI System` 재실행 → 콘솔에서
   `🎨 스프라이트 미발견: ...` 경고가 남아있는지 확인 (없으면 전부 주입 완료)
5. Play 모드에서 창 크기를 변경해 9-slice 가 제대로 늘어나는지 확인

### 자동 주입 경로 매핑 (PA_UIBuilder.cs 상수)
| 상수 | 기대 경로 |
|------|-----------|
| `SPR_INV_PANEL` | `Assets/Art/UI/ui_inventory_panel.png` |
| `SPR_INV_LABEL` | `Assets/Art/UI/ui_inventory_label.png` |
| `SPR_DROPSHADOW` | `Assets/Art/UI/ui_dropshadow.png` |
| `SPR_HOTBAR_BG` | `Assets/Art/UI/ui_hotbar_bg.png` |
| `SPR_PHONE_BODY` | `Assets/Art/UI/ui_phone_body.png` |
| `SPR_PHONE_BEZEL` | `Assets/Art/UI/ui_phone_bezel.png` |
| `SPR_PHONE_SCREEN` | `Assets/Art/UI/ui_phone_screen.png` |
| `SPR_PHONE_STATUSBAR` | `Assets/Art/UI/ui_phone_statusbar.png` |
| `SPR_APP_ICON_BASE` | `Assets/Art/UI/ui_app_icon_base.png` |
| `SPR_ICON_AUDIT` | `Assets/Art/UI/icon_app_audit.png` |
| `SPR_ICON_HIRING` | `Assets/Art/UI/icon_app_hiring.png` |
| `SPR_ICON_FEED` | `Assets/Art/UI/icon_app_feed.png` |
| `SPR_ICON_SETTINGS` | `Assets/Art/UI/icon_app_settings.png` |

> **앱 아이콘 특례**: `icon_app_*.png` 가 존재하면 이모지 플레이스홀더 대신 별도 `IconFG` 자식 이미지(14px 안쪽 여백)로 덧씌운다. 없으면 이모지(📊🤝📱⚙) 가 자동 폴백.

---

## 5. 향후 확장 (MVP 이후)

- `ui_phone_screen_variants/` — 시간대별 월페이퍼 (아침/낮/밤)
- `icon_app_*` 4개 이상 추가 (화폐, 저장, 지도, 도감…)
- `ui_inventory_panel_glow.png` — 풀 찼을 때 빨갛게 발광하는 변형
- `ui_slot_locked.png` — 상점 확장 전 잠긴 슬롯

---

**필수 16~20개 스프라이트만 확보하면 현재 `PA_UIBuilder.cs` 가 생성한 단색 UI 가 레퍼런스 수준의 퀄리티로 즉시 업그레이드된다.** `ApplySprite()` 가 이미 모든 주입 지점에 배치되어 있으므로 **스프라이트 파일을 `Assets/Art/UI/` 에 넣고 메뉴를 다시 실행하기만 하면 된다.**
