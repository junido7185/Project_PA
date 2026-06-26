# Project PA Migration Plan

Inspection date: 2026-06-19

This migration plan must follow `PROJECT_PA_CREATIVE_NORTH_STAR.md` first, then `PROJECT_PA_DESIGN_INTENT.md`.

Project_D / ReferencePrototype is not a direct merge target; it is only a visual, route, staging, and readability reference for Project_PA. It must not replace Project_PA's cozy day-to-night life and shop management identity, and no Project_D assets, scripts, scenes, materials, prefabs, textures, or project settings should be copied or merged without explicit future approval.

## 2026-06-19 Completion Loop Note

The CL-001 to CL-004 work completed after this migration plan did not migrate or copy any ReferencePrototype assets. It used Project_PA's existing systems to make the management loop clearer:

- Day 1 objectives now explain supply, stock, pricing, customer response, revenue, audit/tier review, and saving.
- NPC purchase/rejection feedback now explains customer economic judgment.
- Day summary now reports management feedback and next action.
- HUD/audit/summary now show next-tier requirements from Project_PA `TierDefinition` data.

Future visual acceleration should continue to use ReferencePrototype only as a broad readability reference. The target is a shop/stall that reads as a cozy day-to-night operating hub where daytime goods, night sales, customer feedback, and village-change signals come together, not a decorative market clone.

## Migration Goal

Accelerate Project_PA by using the reference prototype as a visual and staging reference for a clearer cozy life/shop demo, without copying unsafe assets, changing gameplay logic, or merging projects.

Primary target:

- Make the Project_PA market stall read as the night shop hub and visible interface for the local supply-chain/village-change loop within 10 seconds.
- Improve scene composition around the stall, NPC route, product display, and UI readability.
- Preserve Project_PA systems: shop slots, pricing, NPC purchase flow, smartphone apps, audit/tier/friendship systems, save flow, and guided first-day scenario.

No assets were copied during this inspection pass.

## Project Paths

Source reference project:

- `C:\Users\sdjsd\Desktop\Unity\Project_D\Project_D`
- Internal alias: `CozyMarketPrototype` / `ReferencePrototype`
- Unity version: 6000.0.50f1
- Not a direct merge target. Use only for visual organization, route clarity, staging ideas, and readability patterns.

Target final project:

- `C:\Users\sdjsd\Desktop\Unity\Project_PA`
- Unity version: 6000.3.2f1

## Verification Summary

Both roots were verified as Unity projects:

- Project_PA contains `Assets/`, `Packages/`, and `ProjectSettings/`.
- ReferencePrototype contains `Assets/`, `Packages/`, and `ProjectSettings/`.

The reference prototype is not a Git repository. Project_PA is a Git repository on `master`, tracking `origin/master`.

## Project_PA Current State

Likely main demo scene:

- `Assets/Scenes/Prototype_FirstDay.unity`

Additional fuller scene:

- `Assets/Scenes/MainGame.unity`

Core gameplay systems already implemented:

- Player movement, interaction, camera follow.
- Inventory and hotbar.
- Shop and `ShopSlot` stocking.
- Price confirmation through `ShopPriceUI`.
- NPC shopping/purchase flow.
- Economy and money HUD.
- Dialogue, friendship, hiring, smartphone apps.
- Crafting/workbench, production NPCs, storage, save/load.
- Guided day-one route through `PlayableDayScenarioController`.

Current shop/stall implementation:

- `Shop` owns and exposes child `ShopSlot` objects.
- `ShopSlot` stocks sellable items from inventory/hotbar, stores `displayPrice`, creates `ShopSlot_Display`, and opens `ShopPriceUI` when stocked.
- Tier service can enable a limited number of managed slots.
- The current system is suitable for visual polish because it already has explicit product-slot behavior.

Current UI implementation:

- Runtime-created HUD and panels: money, clock, objective, dialogue, interaction prompt, shop pricing, crafting, friendship, smartphone apps, pause/settings.
- Existing Project_PA UI image assets are under `Assets/Art/UI/`.
- `PlayableDayScenarioController` already has a clear six-step Korean objective flow.

Current art/visual style:

- Low-poly and toy-like direction.
- Existing Project_PA building models `B01_MarketStall` through `B12_TradePort`.
- Existing market/stall material: `Assets/Materials/Buildings/Mat_B01_MarketStall.mat`.
- Existing environment props include trees, rocks, plaza fountain, cottages, workbenches, storage shed, trade port, and simple colored materials.

Current blockers for visual polish:

- Automated Play Mode smoke has been revalidated, including NPC NavMesh readiness.
- Stall readability now has a first-pass hub treatment, but it still needs manual gameplay camera review.
- UI readability needs a pass at target resolution.
- Dedicated final screenshot/demo composition has a marker, but screenshot capture has not been validated.
- Windows build exists and smoke-launches, but the full executable demo route still needs manual verification.

## ReferencePrototype Inspection Summary

Scenes:

- `Assets/scene1.unity`
- `Assets/ResetScene.unity`

Build settings:

- `Assets/scene1.unity`
- `Assets/ResetScene.unity`

Asset volume:

- Very large exported project structure.
- Major folders include `GameObject`, `Mesh`, `Texture2D`, `Material`, `MonoBehaviour`, `Scripts`, `AudioClip`, `AnimationClip`, `Plugins`, and `Resources`.
- Counts observed include thousands of prefabs/assets/textures and more than one thousand C# files.

Reference market/stall-related examples found:

- Market stall prefabs by wood/material variant.
- Marketplace/floor prefabs.
- Product crate prefabs for fruit/vegetable-style displays.
- Register/counter/display-case/display-pedestal prefabs.
- Sign, path, lamp, fence, bench, and basket prefabs.
- UI textures for slots, rounded buttons, speech bubbles, panels, icons, maps, clocks, and inventory-like surfaces.

Reference player/NPC/world systems:

- Extensive player, NPC, shop, town, quest, save, UI, localization, networking, water, and editor/helper scripts exist.
- These scripts appear to be broad external/decompiled/exported systems and are not appropriate for Project_PA migration.

## Reusable Candidates

Safe reusable ideas:

- Market stall silhouette: wooden frame, clear front counter, awning-like top shape, visible product area.
- Product presentation: crates/baskets/display pedestals near or on shop slots.
- Shop clarity: register/counter/sign/price-marker language as broad visual concepts.
- Environment dressing: path tiles, fence edges, lamps, benches, small landmark near market.
- UI readability: warm panel backgrounds, selected-slot highlight, compact buttons, speech bubble clarity, larger readable text.
- NPC staging: place first settler/customer near the stall route and force a short readable purchase path.

Potential file candidates only if ownership/licensing is explicitly confirmed later:

- Market stall variant prefabs.
- Product crate prefabs.
- Simple sign/lamp/path/fence/bench props.
- Generic UI slot/button/panel textures.

Important: these are not approved for direct copying yet. They are listed as candidates for future user review only.

## Unsafe Or Non-Reusable Files

Do not migrate directly:

- `Assets/Scripts/Assembly-CSharp/`
- `Assets/Plugins/Assembly-CSharp-firstpass/`
- Third-party/plugin code such as save, localization, networking, standard image effects, water, and example systems.
- `Assets/GameObject/` wholesale.
- `Assets/Mesh/` wholesale.
- `Assets/Texture2D/` wholesale.
- `Assets/Material/` wholesale.
- `Assets/AudioClip/`, `Assets/AnimationClip/`, `Assets/AnimatorController/`, `Assets/Avatar/`, and `Assets/MonoBehaviour/` wholesale.
- Exact UI textures, exact UI layouts, brand-specific names/logos, named NPCs, and commercial-looking game content.
- Reference project settings, package files, scenes, build settings, and scripts.

Reason:

- The reference project structure looks like a broad exported/decompiled project, not a small clean original asset pack.
- Ownership of individual files cannot be proven from inspection alone.
- Project_PA already has enough native systems and assets to recreate the useful visual ideas safely.

## Proposed Project_PA Target Folders

Use these only in a later implementation pass after approval:

- `Assets/Prefabs/Market/`
- `Assets/Materials/Market/`
- `Assets/Art/Market/`
- `Assets/Scenes/_Backups/`
- `Docs/Migration/`

Recommended target naming:

- Use Project_PA names only.
- Use terms such as `PA_MarketStall`, `PA_ProductCrate`, `PA_PriceTag`, `PA_MarketSign`, `PA_DemoMarketLayout`.
- Do not use source-project names in asset filenames, class names, namespaces, scene names, or docs beyond the approved aliases `CozyMarketPrototype` and `ReferencePrototype`.

## Practical Mapping Table

| Reference idea | Project_PA target | Safe implementation approach |
| --- | --- | --- |
| Market stall visual | `Assets/Prefabs/Buildings/B01_MarketStall.prefab`, `Shop`, `ShopSlot` | Use Project_PA stall as base. Add Project_PA-owned child props/labels/slot markers so the stall reads as an economic operating hub in a backed-up scene or new market prefab. |
| Product crates/baskets | `ShopSlot_Display`, Project_PA item display, simple primitives | Recreate crates/baskets with cubes/materials or Project_PA-owned props. Do not copy reference crates unless later approved. |
| Register/counter/readable shop front | `ShopSlot`, `ShopPriceUI`, stall child transforms | Add a small Project_PA-created counter/register/sign visual as non-gameplay decoration. |
| Environment props | `B11_PlazaFountain`, cottages, trees, rocks, paths, lamps/fences created in Project_PA | Stage a compact market plaza around the first-day route. Prefer existing Project_PA models and primitives. |
| UI panel style | `ShopPriceUI`, `SmartphoneUI`, objective HUD, interaction prompt | Restyle toward warm translucent panels, strong selected states, larger Korean text, and clear confirm/cancel colors. |
| Item display | `ShopSlot_Display`, item prefabs/icons, price text | Improve display height, add simple price tag/label, and ensure stocked item is visible from demo camera. |
| NPC placement | `PlayableDayScenarioController`, NPC objects, work/home points | Place first settler/customer and purchase path so the evaluator immediately sees talk-stock-price-sell flow. |

## Step-by-Step Migration Tasks

1. Snapshot Project_PA state.
   - Confirm Git status.
   - Create scene backup before any scene edit.
   - Do not touch ReferencePrototype.

2. Fix build/readiness blocker first.
   - Remove missing `Assets/Scenes/SampleScene.unity` from Build Settings.
   - Confirm final start scene.
   - Revalidate Play Mode.
   - Status: completed for automated smoke; manual full-route validation remains.

3. Create a safe Project_PA market visual shell.
   - Start from Project_PA `B01_MarketStall`.
   - Add visible product shelf/slot markers, price tag anchors, signboard, small lamp, and crate-like props made from Project_PA primitives/materials.
   - Show the reverse supply-chain loop visually: goods received, priced, displayed, sold, and reinvested.
   - Keep all gameplay components unchanged.

4. Stage `Prototype_FirstDay.unity`.
   - Backup scene first.
   - Center camera route on market stall.
   - Place first NPC/customer and interaction guide near the stall.
   - Keep day-one scenario logic unchanged.

5. Improve item display readability.
   - Adjust `ShopSlot` display transform only if required after visual testing.
   - Prefer scene/prefab child positioning before touching code.

6. Restyle UI readability.
   - Use existing Project_PA UI assets and colors.
   - Keep UI behavior unchanged.
   - Prioritize objective panel, shop price panel, money HUD, and interaction prompt.

7. Screenshot/demo pass.
   - Capture final scene composition.
   - Verify player, stall, product, price UI, NPC, and money HUD are visible and readable.

8. Build and package.
   - Build Windows executable.
   - Status: Windows build exists and smoke-launches.
   - Create executable package.
   - Create source package without cache folders.
   - Include README/run instructions and final presentation/report.

## Implementation Status - 2026-06-19

Completed first safe visual acceleration pass:

- Removed missing `SampleScene` entry from Build Settings and confirmed `Prototype_FirstDay.unity` as the first enabled scene.
- Created `Assets/Scenes/_Backups/Prototype_FirstDay_before_visual_acceleration_20260619.unity`.
- Added `Assets/Editor/PA_VisualAccelerationBuilder.cs` as an Editor-only helper for Project_PA-owned visual staging.
- Created `Assets/Prefabs/Market/PA_MarketStall_Hub.prefab`.
- Created Project_PA-owned market materials under `Assets/Materials/Market/`.
- Updated `Assets/Scenes/Prototype_FirstDay.unity` with child-only visual objects around the existing shop.
- Added a narrow `PA_RuntimeSceneBinder` startup fix so NPC NavMeshAgents are enabled only after scene NavMeshSurface data is active.
- Created root `README.md` with source/build run instructions and demo route notes.
- Built Windows executable at `Builds/Windows/Project_PA.exe`.

The new stall pass is not a copied reference asset. It uses Unity primitives, Project_PA naming, and Project_PA-owned materials to communicate:

- NPC supply/drop-off.
- Processing/value-add.
- ShopSlot display positions.
- Price tag positions.
- Customer purchase judgment.
- Revenue/reinvestment.

Verification:

- Unity batch compile completed without C# compiler errors.
- Scene verification passed: shops=2, shopSlots=8, hubVisuals=1, routeMarkers=1, roleBadges=8, screenshotMarkers=1, buildSettingsOk=True.
- Automated Play Mode smoke test now passes after making the test domain-reload safe.
- Play Smoke counts after NavMesh repair: players=1, shops=2, shopSlots=8, economyServices=1, shopPriceUI=1, npcs=8, npcAgents=8/8, npcAgentsOnMesh=8, npcNearNavMesh=8.
- Windows build succeeded.
- Windows player smoke reached runtime startup with `surfaces=1, agents=8/8, onMesh=8` and no `Failed to create agent because there is no valid NavMesh` messages.
- Manual Play Mode review is still required for camera readability, player feel, NPC movement, and the full stock -> price -> NPC purchase -> money update route.

Deferred:

- T012 UI layout changes are deferred until manual 1920x1080 Play Mode review.
- Final source/executable packaging should wait for manual full-route verification.

## Risks

- Direct copying from ReferencePrototype may introduce licensing or attribution risk.
- Reference scripts are not compatible with Project_PA architecture and could break the project.
- Reference project is Unity 6000.0.50f1 while Project_PA is Unity 6000.3.2f1.
- Scene edits can break serialized references if done without backup.
- UI restyling can reduce readability if not tested at target resolution.
- Windows build exists, but manual executable route verification is still required.

## Rollback Plan

- Before scene edits, duplicate the target scene into `Assets/Scenes/_Backups/`.
- Before prefab edits, duplicate the prefab into `Assets/Prefabs/Market/` or make a new variant.
- Keep all migrated/recreated visual assets in dedicated Project_PA folders.
- Use Git status before and after each implementation pass.
- If a change breaks Play Mode, revert only the new Project_PA files or restore the backed-up scene/prefab.
- Never modify ReferencePrototype.

## First Implementation Task Recommendation

Start with a Project_PA-owned market stall visual pass that treats the stall as a cozy day-to-night operating hub, not merely a prettier shop:

1. Backup `Assets/Scenes/Prototype_FirstDay.unity`.
2. Create a Project_PA market prefab/variant based on `B01_MarketStall`.
3. Add simple Project_PA-created child props: daytime stock prep area, product crates, signboard, price tag anchors, slot markers, customer path, and warm accent materials.
4. Stage it in the demo route without changing shop gameplay code.
5. Run Play Mode to verify stock-price-purchase still works.

This gives the highest visual improvement while avoiding direct reference asset copying.
