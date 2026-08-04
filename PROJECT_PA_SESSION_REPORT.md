# PROJECT_PA_SESSION_REPORT.md

## 2026-07-16 Continuation — Tripo Character Unity Finalization Complete

- Audited C-01~C-09 source FBX files, Humanoid import settings, Avatars, meshes, materials/textures, polycounts, Idle/Walk clips, scene/runtime assignments, colliders, agents, and existing grounding code through Unity Editor API.
- All nine characters are intact, valid Humanoids with a coherent warm stylized identity. No replacement, Blender repair, source asset overwrite, or new generated art was justified.
- Found the main runtime defect in `NpcPresentationNormalizer`: every resident source was forced to 2× scale and every body/agent to 3.6m, overriding the safer scene values. Runtime baseline measured residents at 2.088–2.190m with feet as much as 0.132m below their root.
- Changed normalization to measure each rendered model, target 1.75m, align feet 0.02m above the root, and use 1.8m/0.4m CapsuleCollider and NavMeshAgent with 0.75m stopping distance. Character shadow settings are also normalized.
- Changed NPC procedural cadence from an arbitrary time/speed factor to actual planar speed divided by a 1.15m nominal stride, while preserving role-specific pose profiles. Added constrained Walk playback synchronization to the player's existing foot IK component using the existing clip's 1.174m/s average speed.
- Created `PA_CharacterFinalizer` for reproducible C-01~C-09 auditing, source lineup capture, same-camera runtime staging, metrics, and final assertions. It does not save the main scene.
- D3D11 compile passed. Actual Play idle validation passed for all eight residents: 1.748–1.751m rendered height, +0.032–+0.035m foot offset, 1.8/0.4 collider, 1.8/0.4/0.75 agent, valid Avatar, and shadows. `character_idle_after.png` visibly improves scale and grounding over the baseline.
- The first walking final validation stopped because startup onboarding left `Time.timeScale=0`. A follow-up single task reused `PlayableDayScenarioController.RestoreSavedSession`, explicitly restored game time, and kept runtime character code unchanged. The BUG_LOG entry is now RESOLVED.
- Final D3D11 walking validation passed with player 5m/s/Animator playback 2.25× and all eight NPCs at 2.5m/s with cadence 1.851–2.328. Walking bounds stayed 1.737–1.758m with +0.029–+0.037m foot offsets and the corrected physical bodies.
- Direct visual review iterated the validation-only staging from the obstructed shop front to the actual west road. The final `character_walk_after.png` separates all eight residents and the player so feet, silhouettes, direction, and shadows are readable; one roadside tree only partially overlaps one torso.
- D3D11 regressions passed: InteriorCustomer reserve→complete path→front stop→purchase→return, CustomerArrival invited 3/capped 2, and FinalDemoRoute BreadLoaf 30G sale.
- Runtime assignment audit also found Bori=C-03, Miner=C-04, Farmer/Fisher=C-05 duplicate, and C-02 unused. These are identity decisions, so no resident was remapped without user confirmation.
- No source FBX, Avatar, animation clip, texture, prefab, main scene, NavMesh bake, save schema, package, commit, or push operation was performed.

Next: audit B02~B04 shop-evolution models as the next single visual task, preserving existing Tier, placement, shop, and save contracts.

## 2026-07-16 Continuation — B05 Workbench Functional Art Finalization

- Audited the actual B05 source FBX, wrapper prefab, four faces, scene/runtime use, 2×2 placement, collider, carving obstacle, and existing crafting chain through Unity Editor API. The grounded 14,689-triangle source has a readable work surface, pegboard, stool, and drawer, so its visual identity was preserved.
- The defect was integration, not a broken mesh: the modeled work side faced local `+Z`, opposite the placement system's reserved local `-Z` interaction/clearance face, and the physical 3.2m-wide collider exceeded the 2.26m visible mesh.
- Extended only `Workbench` and the successful tail of `CraftingService`. BasicWorkbench rotates its source Visual 180°, uses a `(2.5,2,2.55)` physical collider/carving obstacle, and attaches a persistent Project-PA-owned preparation kit. The prefab collider remains untouched so its established 2×2 placement footprint does not change.
- Kept the existing `Workbench → CraftingUI → CraftingService` system and chose its existing Wood→Plank recipe as the purpose-readable minimum function. No parallel gathering, crafting, inventory, or progression system was added.
- Generated `B05_Workbench_PreparationKit` with low-poly raw Wood, cream guide board/rails, finished Plank pieces, and a coral clamp/handle. It reuses four existing Project PA materials and contains no random decorative tools.
- A successful craft animates the clamp handle and softly pulses a warm light for 0.72 seconds. Direct review found the initial 2.2 light too bright, so it was reduced to 0.9 and recaptured.
- Actual Play Mode MainCamera before/after: `Logs/B05_WorkbenchAudit/b05_runtime_before.png` → `b05_runtime_after.png`. The player, interaction face, work surface, material flow, and collider now agree.
- D3D11 final validation passed: open actual CraftingUI, spend 2 Wood, create 1 Plank, observe feedback, close UI. Processing-chain BreadLoaf, 2×2 placement/v10 persistence, Tier interior round trip, and FinalDemoRoute 30G regressions also passed.
- Two validation-infrastructure failures were recorded and resolved: asynchronous Play Mode capture launched with `-quit`, and a substring selector choosing the Furniture recipe whose ingredient contained “Plank”.
- No source FBX, texture, wrapper prefab, main scene, save schema, package, commit, or push operation was performed.

Next: preserve C-01~C-09 identities and finalize player/NPC grounding, walk speed/foot sliding, Animator/Avatar, collider, and NavMeshAgent through same-angle runtime captures.

## 2026-07-16 Continuation — B10 Cottage Visual Finalization

- Audited the Tripo-estimated B10 source, wrapper prefab, four scene instances, connected mesh components, bounds, colliders, and game-camera evidence through Unity Editor API.
- Four-side captures proved the FBX itself is grounded and intact; its authored door facade is local `-X`. The map had aimed local `-Z` at the plaza, while a separate primitive `PA_StoreDoor_Out` and cube sign were left floating at `local z=-3.819`.
- Added `CottageVisualFinalizationController`: all three authoritative map cottages face their real doors toward the plaza, and the legacy Static B10 is disabled at runtime. Collider/NavMeshObstacle rotate with each intact root.
- Preserved the modeled door, awning, windows, steps, `BuildingEntrance`, collider, Tier gate, and inside/outside spawn contract. Only the detached primitive door/sign renderers are hidden.
- Extended `PA_CottageVisualFinalizer` to audit 573 connected source components, generate a Project-PA-owned beveled two-material shop-sign mesh/prefab, validate the scene without saving it, and capture the actual Play Mode MainCamera.
- Reused existing `PA_Market_DarkWood` and `PA_Market_AwningCream`; no external asset, package, Blender, source-FBX overwrite, wrapper-prefab edit, or main-scene save was performed.
- Fixed the world sign presentation after direct inspection: scale-compensating door child anchor, fixed plaque-parallel text, explicit TMP Rect width/overflow, and hand-authored `P.A. SHOP - Tier 1/OPEN` copy.
- D3D11 validation passed: final asset/bounds/facade/duplicate checks, Play Mode MainCamera capture, Tier 0 lock→Tier 1 OPEN→enter→stock→price→exit, and FinalDemoRoute BreadLoaf 30G.
- Evidence: `Logs/B10_CottageAudit/b10_cottage_before.png`, `b10_cottage_after.png`, `b10_cottage_runtime.png`, `Logs/B10_CottageFinalValidation_Final.log`, `Logs/B10_EnterableShopRegression_Final.log`, `Logs/B10_FinalDemoRouteRegression.log`.
- Two failed checks were recorded and resolved in `BUG_LOG.md`: TMP renderer over-count in the editor assertion, and a temporary break of the door-child sign lookup contract.

Next: B05 Workbench functional art is the next single task. Preserve its existing `Workbench`, 2×2 placement, carving, save, and day-prep connection while replacing the temporary-table look with a purpose-readable work surface.

## 2026-07-16 Continuation — Shop Customer Approach P4

- Audited the actual `NpcController` FSM and confirmed it previously walked to `ShopSlot.transform.position`, then claimed only at evaluation time.
- Added `ShopCustomerApproachController` as a runtime sidecar. It resolves existing rotated interaction cells, excludes logically blocked cells, samples NavMesh, requires a complete calculated path, and reserves a slot by NPC owner before movement.
- Added a narrow read-only approach API to `ShopCustomizationController`; private placement definitions and the shared GridService remain authoritative.
- Extended `NpcController` without changing purchase math: reserve reachable slot → walk to front cell → face display → existing ShopSlot claim/PurchaseEvaluator → release reservation.
- A shelf moved during a visit invalidates the old source point and causes safe re-selection. Reservations are transient and do not alter save v10.
- First Unity import found one validator-only definite-assignment compile error; explicit initialization fixed it and the single retry passed. Unity compile has no C# errors; `dotnet build` has 0 errors and the pre-existing Unity source-generator warning CS8785.
- D3D11 ShopCustomization PASS: moved `(4,0)/r3` shelf maps to `(3,0)`, complete path, same-slot owner exclusion, different-slot concurrent reservation, and existing 61G/v10 checks.
- D3D11 InteriorCustomer PASS: real visitor reserve→approach stop→15G purchase→return. CustomerArrival PASS (`invited=3`, cap 2) and FinalDemoRoute PASS (BreadLoaf 30G).
- Captured `Logs/DemoViewShots/p4_shop_approach_final_20260716_114141.png`. The top-down camera shows the stocked interior/customer staging but does not isolate both reserved cells clearly enough for subjective visual approval.
- No main scene, prefab, NavMesh bake, save schema, package, commit, or push operation was performed.

Next: B10 Cottage detached/floating parts are the next single visual task; B05 functional art follows.

## 2026-07-16 Continuation — Outdoor Placement P3

- Audited `GridService`, `BuildManager`, `BuildingRegistry`, v10 save order, map roads/plaza, B09 prefab/collider/StorageBox, and previous physics evidence.
- Added `OutdoorPlacementController` as a sidecar over the existing grid: `village.outdoor` is 47×47 at 2m with 212 protected road/plaza/entrance/spawn cells.
- Extended the existing build path to multi-cell owner occupancy, front clearance, 90° preview, M relocation, and X safe recovery. `EquipmentSystem` only gained the narrow guard needed to keep relocation active without a selected blueprint.
- Finalized B09 for this milestone as an external village storage shed. The model identity and original FBX/prefab remain intact; Unity runtime alignment, occupancy, carving, interaction, duplicate suppression, and save behavior were corrected.
- Reused save schema v10. Outdoor records append to `placeables`; `BuildingRegistry` remains responsible for dynamic building recreation and the sidecar restores exact cell/rotation/storage contents.
- Added `PA_OutdoorPlacementValidator`. D3D11 PASS: protected=212, B09=9 cells, fixed `(15,31)/r3`, dynamic `(31,28)/r1`, stored items 2+1, unsafe recovery refusal, empty recovery, save/load.
- Captured and visually reviewed `Logs/OutdoorPlacement/20260716_111214/b09_outdoor_baseline.png` and `b09_outdoor_final.png`. B09 reads as storage and the final control hint is legible; B10 Cottage detached pieces are now the next visual priority.
- Regressions passed: SaveRoundTrip v10, ShopCustomization B05/shelf/61G, FinalDemoRoute BreadLoaf 30G.
- No main-scene, prefab, source FBX, package, commit, or push operation was performed.

Next: P4 NPC approach/reservation/reachability first; B10 visual repair and B05 functional-art pass follow without pausing the full-game loop.

Session date: 2026-06-19

## Goal

Advance Project_PA toward final submission while preserving `PROJECT_PA_DESIGN_INTENT.md`:

- Reverse supply-chain management simulation.
- Player as manager/operator, not primary laborer.
- NPCs as producers, consumers, specialists, and economic agents.
- Market stall as visible operating hub, not a decorative shop clone.
- ReferencePrototype used only for visual/readability inspiration.

## Completed Work

Priority 0 safety:

- Confirmed work happened inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Confirmed Unity folders and `Assets/Scenes/Prototype_FirstDay.unity` exist.
- Recorded Git status before/after work.
- Removed missing `Assets/Scenes/SampleScene.unity` from Build Settings.
- Confirmed `Assets/Scenes/Prototype_FirstDay.unity` is the first enabled build scene.
- Created scene backup at `Assets/Scenes/_Backups/Prototype_FirstDay_before_visual_acceleration_20260619.unity`.

T010 market stall visual pass:

- Added `Assets/Editor/PA_VisualAccelerationBuilder.cs`.
- Created Project_PA-owned market materials under `Assets/Materials/Market/`.
- Created `Assets/Prefabs/Market/PA_MarketStall_Hub.prefab`.
- Added `PA_MarketStall_Hub_Visual` to `Prototype_FirstDay.unity`.
- Added wood frame, awning, shelf, product crate, price tag, sign, supply, process, sale, and reinvestment visual markers.

T011 scene staging pass:

- Added `PA_DemoRoute_VisualMarkers`.
- Added route markers for talk -> stock -> price -> purchase -> revenue.
- Added delivery, customer approach, and reinvestment pads around the demo shop.
- Did not change `PlayableDayScenarioController`.

T012 UI pass:

- Inspected `ShopPriceUI` and `MoneyHUD`.
- No UI code was changed.
- `ShopPriceUI` already includes item name, current price, NPC reaction hint, approximate purchase rate, confirm, retrieve, and close controls.
- Manual 1920x1080 readability review is still required before changing UI layout.

T013 NPC presentation pass:

- Added `PA_EconomicRoleBadge` labels to existing NPCs.
- Role labels distinguish producers, specialists, guide NPC, and consumers.
- Did not change NPC AI/FSM/purchase logic.

T014 screenshot/presentation pass:

- Added `PA_ScreenshotCameraMarker_MarketHub`.
- Screenshot capture itself is still pending manual Unity/game view review.

NavMesh/runtime readiness pass:

- Repaired `Prototype_FirstDay.unity` NavMesh for the current staged demo.
- Snapped 8 NPCs to valid NavMesh positions.
- Saved NPC NavMeshAgents disabled in-scene so they do not initialize before NavMeshSurface data exists in a Windows player.
- Updated `PA_RuntimeSceneBinder` to add NavMeshSurface data first and then enable NPC NavMeshAgents.
- Confirmed RuntimeBinder reports `surfaces=1, agents=8/8, onMesh=8`.

Build/readme pass:

- Built Windows executable at `Builds/Windows/Project_PA.exe`.
- Smoke-launched the executable and confirmed the previous NavMeshAgent startup error is gone.
- Created root `README.md` with Unity version, source open steps, build run path, demo route, validation notes, and reference-policy notes.

Completion loop continuation:

- Created long-term planning documents for complete-game development.
- Implemented CL-001 Management Loop Objective Rewrite.
- Implemented CL-002 NPC Purchase Feedback.
- Implemented CL-003 Day Summary Improvement.
- Implemented CL-004 Tier 0 Goal Clarity.
- Kept all implementation inside Project_PA.
- Did not modify Project_D / ReferencePrototype.
- Did not copy assets.
- Did not rewrite core shop/economy/NPC purchase/tier/audit/save systems.

Final route validation continuation:

- Added Editor-only validation tool `Assets/Editor/PA_FinalDemoRouteValidator.cs`.
- Verified the route in batch Play Mode:
  - player movement components
  - first NPC dialogue
  - hotbar-to-shop-slot stocking
  - price UI open/confirm
  - NPC feedback bubble
  - sale/money/revenue update
  - MoneyHUD next-tier text
  - audit app next-tier text
  - Day 1 summary content
  - market hub, route markers, role badges, screenshot marker
- Rebuilt the Windows executable after the final route/readability fixes.
- Smoke-launched the Windows executable and confirmed RuntimeBinder/NavMesh startup.
- Kept changes minimal and presentation-focused.

Final presentation review continuation:

- Added Editor-only final presentation reviewer `Assets/Editor/PA_FinalPresentationReviewer.cs`.
- Generated final presentation captures for:
  - market hub/objective/HUD
  - `ShopPriceUI`
  - NPC purchase feedback bubble
  - smartphone audit goal
  - Day 1 summary
- Adjusted objective panel placement so it sits higher and blocks less of the market hub.
- Enlarged only the Day 1 summary body layout so the summary fits.
- Updated the audit app next-tier text so it wraps in the phone panel instead of truncating the requirement.
- Reworked NPC purchase feedback bubbles into screen-space follower UI so feedback stays readable from the demo camera.
- Hid active NPC bubbles when opening the smartphone or Day 1 summary so feedback does not cover management panels.
- Re-ran automated final route validation after these fixes.
- Rebuilt `Builds/Windows/Project_PA.exe` and smoke-launched it again.

Submission packaging readiness continuation:

- Read the required design/status/TODO/session/README documents.
- Confirmed current working directory and Git root are `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Confirmed `Builds/Windows/Project_PA.exe` exists.
- Inspected `Builds/Windows/` contents for executable packaging.
- Recorded that the human Windows executable route result was still pending during the 2026-06-19 readiness check.
- Finalized the source/executable package plan without creating archives yet.
- Deferred `.zip`, `.rar`, or `.7z` package creation until the human executable route pass was supplied.

Final submission packaging continuation:

- Recorded the user-confirmed human Windows executable route result as passed.
- Created `SubmissionPackages/Project_PA_Source_20260620.zip`.
- Created `SubmissionPackages/Project_PA_Windows_20260620.zip`.
- Verified both zip files open through the .NET zip reader.
- Scanned both zip files for forbidden entries.
- Confirmed the source package excludes `.git/`, `Library/`, `Temp/`, `Logs/`, `Builds/`, `UserSettings/`, `obj/`, `.vs/`, `SubmissionPackages/`, `GeneratedAssets_deleted/`, and generated cache folders.
- Confirmed the executable package excludes `Project_PA_BurstDebugInformation_DoNotShip/`, logs, source folders, cache folders, and generated debug/cache output.
- Did not modify Project_D, copy assets, import packages, change gameplay logic, commit, or push.

## Modified Files

- `Assets/Scenes/Prototype_FirstDay.unity`
- `Assets/Scripts/PA_RuntimeSceneBinder.cs`
- `Assets/Scripts/NpcController.cs`
- `Assets/Scripts/UI/NpcBubbleUI.cs`
- `Assets/Scripts/UI/ShopPriceUI.cs`
- `Assets/Scripts/UI/SmartphoneUI.cs`
- `Assets/Scripts/UI/PlayableDayScenarioController.cs`
- `Assets/Scripts/UI/MoneyHUD.cs`
- `Assets/Scripts/AuditResultUI.cs`
- `ProjectSettings/EditorBuildSettings.asset`
- `PROJECT_PA_STATUS.md`
- `PROJECT_PA_TODO.md`
- `PROJECT_PA_MIGRATION_PLAN.md`
- `PROJECT_PA_COMPLETION_PLAN.md`
- `PROJECT_PA_RELEASE_BACKLOG.md`
- `PROJECT_PA_CURRENT_MILESTONE.md`
- `README.md`

## Created Files And Folders

- `Assets/Editor/PA_VisualAccelerationBuilder.cs`
- `Assets/Editor/PA_FinalDemoRouteValidator.cs`
- `Assets/Editor/PA_FinalPresentationReviewer.cs`
- `Assets/Scenes/_Backups/Prototype_FirstDay_before_visual_acceleration_20260619.unity`
- `Assets/Prefabs/Market/PA_MarketStall_Hub.prefab`
- `Assets/Materials/Market/PA_Market_Wood.mat`
- `Assets/Materials/Market/PA_Market_DarkWood.mat`
- `Assets/Materials/Market/PA_Market_AwningCream.mat`
- `Assets/Materials/Market/PA_Market_AwningCoral.mat`
- `Assets/Materials/Market/PA_Market_PriceGold.mat`
- `Assets/Materials/Market/PA_Market_SupplyBlue.mat`
- `Assets/Materials/Market/PA_Market_CustomerGreen.mat`
- `Assets/Materials/Market/PA_Market_ProcessPurple.mat`
- `Assets/Materials/Market/PA_Market_PathClay.mat`
- `Assets/Materials/Market/PA_Market_LabelWarm.mat`
- `Assets/Art/Market/`
- `Builds/Windows/Project_PA.exe` and required Windows player support files
- `PROJECT_PA_COMPLETION_PLAN.md`
- `PROJECT_PA_RELEASE_BACKLOG.md`
- `PROJECT_PA_CURRENT_MILESTONE.md`
- `SubmissionPackages/`
- `SubmissionPackages/Project_PA_Source_20260620.zip`
- `SubmissionPackages/Project_PA_Windows_20260620.zip`

## Function Preservation Check

Preserved:

- `Shop` components remain in scene.
- `ShopSlot` components remain in scene.
- `ShopPriceUI` code was not changed.
- Inventory, hotbar, economy, purchase evaluator, save, tier, audit, hiring, friendship, and NPC purchase logic were not rewritten.
- ReferencePrototype was not modified.
- No ReferencePrototype assets were copied.
- No external packages were imported.
- No Git commit or push was performed.
- `Shop`, `ShopSlot`, `ShopPriceUI`, inventory, hotbar, economy, purchase evaluator, save, tier, audit, hiring, friendship, and NPC purchase decision logic were preserved.
- CL-001 to CL-004 were implemented as UI/text/feedback clarity work around existing systems.
- Purchase math, tier requirements, audit requirements, save data, and shop slot logic were not changed.
- Final route validation helper is Editor-only and does not ship in the runtime player.
- Latest readability fixes shortened UI/bubble text and adjusted bubble presentation without changing purchase/economy logic.

Unity batch verification:

- Compile check after changes: no C# compiler errors.
- Compile check after CL-004: no C# compiler errors.
- Scene verification passed:
  - shops=2
  - shopSlots=8
  - hubVisuals=1
  - routeMarkers=1
  - roleBadges=8
  - screenshotMarkers=1
  - buildSettingsOk=True

Automated Play Mode note:

- Batch Play Mode smoke test now passes after fixing domain-reload-safe test state.
- Logs show `PA_RuntimeSceneBinder` and `Shop.Start()` running.
- Play Smoke counts:
  - players=1
  - shops=2
  - shopSlots=8
  - economyServices=1
  - shopPriceUI=1
  - npcs=8
  - npcAgents=8/8
  - npcAgentsOnMesh=8
  - npcNearNavMesh=8
- Windows player smoke:
  - runtime binder reported surfaces=1, agents=8/8, onMesh=8
  - previous `Failed to create agent because there is no valid NavMesh` message did not appear
- Windows build:
  - build result: Success
  - output: `Builds/Windows/Project_PA.exe`
- CL-004 Play Smoke:
  - build/script compilation succeeded
  - player=1
  - shops=2
  - shopSlots=8
  - economyServices=1
  - shopPriceUI=1
  - npcs=8
  - npcAgents=8/8
  - npcAgentsOnMesh=8
- CL-004 Windows build:
  - build result: Success
  - existing Unity source-generator warning remains non-blocking
- CL-004 Windows player smoke:
  - launched current build
  - runtime binder reported surfaces=1, agents=8/8, onMesh=8
  - previous `Failed to create agent because there is no valid NavMesh` message did not appear
- Final route validation:
  - route validation result: Success
  - stocked item: BreadLoaf
  - paid amount: 30G
  - generated feedback example: `Lumberjack_01: 가격 적정, 구매 (73%)`
  - MoneyHUD/objective overlap check: passed
- Latest Windows build:
  - build result: Success
  - log: `Logs/Codex_FinalRoute_WindowsBuild.log`
- Latest Windows player smoke:
  - player state after 20 seconds: running
  - runtime binder reported surfaces=1, agents=8/8, onMesh=8
- Final presentation review:
  - result: Success
  - log: `Logs/Codex_FinalPresentation_Review11.log`
  - captures: `Logs/FinalPresentation/20260619_232447/`
  - verified market hub, objective panel, MoneyHUD, `ShopPriceUI`, NPC feedback bubble, audit app, and Day 1 summary readability.
- Final route validation after presentation fixes:
  - result: Success
  - log: `Logs/Codex_FinalPresentation_RouteValidation.log`
  - example result: stocked BreadLoaf, paid 30G, compact NPC feedback generated and displayed.
- Final Windows build after presentation fixes:
  - build result: Success
  - log: `Logs/Codex_FinalPresentation_WindowsBuild.log`
  - output: `Builds/Windows/Project_PA.exe`
- Final Windows player smoke after presentation fixes:
  - launched current build
  - runtime binder reported surfaces=1, agents=8/8, onMesh=8
- Packaging readiness:
  - human Windows executable route result: passed, user-confirmed on 2026-06-20
  - package creation: complete
  - source package: `SubmissionPackages/Project_PA_Source_20260620.zip`
  - executable package: `SubmissionPackages/Project_PA_Windows_20260620.zip`
  - source package size: about 359.17 MiB, 1,369 entries
  - executable package size: about 84.25 MiB, 183 entries
  - cache/generated folders to exclude: `.git/`, `Library/`, `Temp/`, `Logs/`, `Builds/` from source, `UserSettings/`, `obj/`, `.vs/`, and generated debug/cache folders
- Manual Windows executable route review was completed by the user and recorded as passed.

## Remaining Problems

- Automated Unity Editor route validation and final presentation capture review passed; human hands-on Play Mode review is still recommended.
- Need confirm by hand that the new market visuals do not block player interaction or NPC movement during real input.
- Stock -> price -> NPC purchase -> money update passed automated route validation; human feel/pacing review remains.
- Human Windows executable route verification is complete and recorded as passed.
- UI readability was checked by generated 1920x1080 captures; a real fullscreen monitor review is still recommended.
- Automated layout check confirmed `MoneyHUD` does not overlap the objective panel.
- Final presentation captures confirmed compact NPC bubble text, audit app text, and Day 1 summary readability.
- Need choose final report/presentation materials.
- Source and executable zip packages are created and ready for submission review.

## Human Unity Checklist

Open Unity 6000.3.2f1 and check:

- Load `Assets/Scenes/Prototype_FirstDay.unity`.
- Confirm Console has no compile errors.
- Enter Play Mode.
- Confirm player can move.
- Confirm interaction prompt appears at the shop/slots.
- Confirm a sellable item can be stocked.
- Confirm `ShopPriceUI` opens and price can be confirmed.
- Confirm an NPC can buy from a stocked slot.
- Confirm money HUD updates after sale.
- Confirm NPC feedback bubble is readable from the gameplay camera.
- Confirm MoneyHUD next-tier text is readable and not noisy.
- Confirm audit app next-tier text is readable.
- Confirm Day 1 summary is readable.
- Confirm new labels/markers are readable but not too visually noisy.
- Confirm NPC movement is not blocked by visual-only props.
- Confirm `PA_ScreenshotCameraMarker_MarketHub` is useful for report screenshots.
- Run `Builds/Windows/Project_PA.exe` and repeat the final demo route.

## Final Git Status Snapshot

```text
## master...origin/master
 M Assets/Fonts/Jalnan2_SDF.asset
 M Assets/Scenes/Prototype_FirstDay.unity
 M Assets/Scripts/AuditResultUI.cs
 M Assets/Scripts/NpcController.cs
 M Assets/Scripts/PA_RuntimeSceneBinder.cs
 M Assets/Scripts/UI/MoneyHUD.cs
 M Assets/Scripts/UI/NpcBubbleUI.cs
 M Assets/Scripts/UI/PlayableDayScenarioController.cs
 M Assets/Scripts/UI/ShopPriceUI.cs
 M Assets/Scripts/UI/SmartphoneUI.cs
 M Assets/Settings/PC_RPAsset.asset
 M Assets/Settings/UniversalRenderPipelineGlobalSettings.asset
 M ProjectSettings/EditorBuildSettings.asset
 M ProjectSettings/ProjectSettings.asset
 M ProjectSettings/UnityConnectSettings.asset
?? Assets/Art/Market.meta
?? Assets/Editor/PA_FinalDemoRouteValidator.cs
?? Assets/Editor/PA_FinalDemoRouteValidator.cs.meta
?? Assets/Editor/PA_FinalPresentationReviewer.cs
?? Assets/Editor/PA_FinalPresentationReviewer.cs.meta
?? Assets/Editor/PA_VisualAccelerationBuilder.cs
?? Assets/Editor/PA_VisualAccelerationBuilder.cs.meta
?? Assets/Materials/Market.meta
?? Assets/Materials/Market/
?? Assets/Prefabs/Market.meta
?? Assets/Prefabs/Market/
?? Assets/Scenes/_Backups.meta
?? Assets/Scenes/_Backups/
?? Docs/VisualTargets/
?? PROJECT_PA_COMPLETION_PLAN.md
?? PROJECT_PA_CURRENT_MILESTONE.md
?? PROJECT_PA_DESIGN_INTENT.md
?? PROJECT_PA_MIGRATION_PLAN.md
?? PROJECT_PA_RELEASE_BACKLOG.md
?? PROJECT_PA_SESSION_REPORT.md
?? PROJECT_PA_STATUS.md
?? PROJECT_PA_TODO.md
?? README.md
?? SubmissionPackages/
```

## Next Recommended Work

Next work should review the generated packages and select the final report/presentation attachment. Automated route validation, final presentation capture review, Windows build, Windows smoke launch, human executable route review, and package creation have passed.

Recommended next Codex prompt:

```text
You are working only inside C:\Users\sdjsd\Desktop\Unity\Project_PA.

First read:
- PROJECT_PA_DESIGN_INTENT.md
- PROJECT_PA_COMPLETION_PLAN.md
- PROJECT_PA_CURRENT_MILESTONE.md
- PROJECT_PA_SESSION_REPORT.md
- PROJECT_PA_STATUS.md
- PROJECT_PA_TODO.md
- README.md

Goal:
Perform the final human Windows executable route check, then prepare submission packaging.

Do not copy Project_D assets.
Do not rewrite Shop, ShopSlot, ShopPriceUI, Inventory, Hotbar, EconomyService, PurchaseEvaluator, NPC purchase logic, save, tier, audit, hiring, or friendship systems.

Tasks:
1. In Unity 6000.3.2f1, load Assets/Scenes/Prototype_FirstDay.unity.
2. Check Console errors.
3. Enter Play Mode.
4. Run Builds/Windows/Project_PA.exe and manually repeat the route: move -> talk -> stock item -> set price -> wait for NPC buy/reject feedback -> confirm money change -> open audit app -> review Day 1 summary.
5. Fix only blocking visual/readability or executable-route issues.
6. If the executable route passes, create a source package and executable package with cache/generated folders excluded.
7. Update PROJECT_PA_STATUS.md, PROJECT_PA_TODO.md, PROJECT_PA_SESSION_REPORT.md, and README.md with the packaging result.
```

## 2026-06-20 Full Game 1.0 Development Session

Session goal:

- Shift Project_PA from final-submission prototype mode into long-term full-game development mode.
- Preserve the reverse supply-chain management identity from `PROJECT_PA_DESIGN_INTENT.md`.
- Begin the first safe sprint: FG-001 Core Multi-Day Loop + FG-002 NPC Producer Economy.

Completed work:

- Verified root and Git root are both `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Confirmed `Assets/`, `Packages/`, `ProjectSettings/`, and `Assets/Scenes/Prototype_FirstDay.unity`.
- Read the required design/status/TODO/session/README/docs before implementation.
- Preserved existing submission packages and build outputs.
- Created full-game master plan and full-game backlog.
- Created scene backup: `Assets/Scenes/_Backups/Prototype_FirstDay_before_fullgame_longplay_20260620_155934.unity`.
- Added `LongPlayProgressionController` as a sidecar service instead of rewriting Day 1 or core shop/economy/NPC logic.
- Added Day 2-7 long-play objectives and daily NPC producer buy-in deliveries.
- Added runtime long-play HUD text for daily objective, revenue progress, and producer delivery result.
- Added SaveData v7 long-play fields and SaveManager migration/save/load hooks.
- Added `PA_LongPlayProgressionValidator` for automated Day 2-7 progression validation.
- Updated `PA_RuntimeSceneBinder` to attach long-play progression automatically.

Modified or created files:

- `Assets/Scripts/LongPlayProgressionController.cs`
- `Assets/Scripts/SaveData.cs`
- `Assets/Scripts/SaveManager.cs`
- `Assets/Scripts/PA_RuntimeSceneBinder.cs`
- `Assets/Editor/PA_LongPlayProgressionValidator.cs`
- `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md`
- `PROJECT_PA_FULL_GAME_BACKLOG.md`
- `PROJECT_PA_STATUS.md`
- `PROJECT_PA_TODO.md`
- `PROJECT_PA_SESSION_REPORT.md`
- `README.md`

Verification:

- Unity batch compile after long-play code changes exited successfully.
- Compile log contains a non-blocking Unity source-generator warning, but no C# compile failure.
- Long-play validator did not run because another Unity instance already had the project open. The open Editor was not force-closed.

Function preservation notes:

- Day 1 route was not intentionally changed.
- `Shop`, `ShopSlot`, `ShopPriceUI`, `Inventory`, `Hotbar`, `EconomyService`, `PurchaseEvaluator`, `NpcController`, `ProducerNpcController`, `TierService`, `AuditService`, and existing save paths were not rewritten.
- The new long-play layer uses existing money/inventory APIs and can be disabled or refined without replacing the current route.

Remaining problems:

- Need run `PA_LongPlayProgressionValidator` once no Unity instance is locking the project.
- Need manually verify Day 1 route after the long-play layer.
- Need manually verify Day 2-7 loop, producer delivery, inventory growth, money spend/refund, and long-play HUD.
- Need save/load test after Day 2+.

Human Unity checklist:

- Open `Assets/Scenes/Prototype_FirstDay.unity`.
- Confirm Console has no compile errors.
- Enter Play Mode.
- Complete the Day 1 route: talk, stock, price, customer response, money, audit/summary.
- Advance/simulate Day 2-7 and confirm producer deliveries enter inventory.
- Confirm money changes when supplies are bought in.
- Confirm long-play objective HUD is readable and not blocking the Day 1 UI.
- Save after Day 3 or later, reload, and confirm long-play state resumes.

Next recommended work:

- Run the new long-play validator.
- If it passes, continue FG-003 Customer Simulation or FG-004 Processing And Crafting Chain.
- If it fails, fix only the smallest blocking issue in the long-play sidecar or save hook.

## 2026-06-20 Long-Play Validation Continuation

Goal:

- Validate FG-001/FG-002 first implementation in Unity automation.
- Confirm Day 1 remains intact.
- Confirm Day 2-7 producer delivery and long-play HUD.
- Confirm Day 3 save/load restores long-play state without writing to the user's normal OS save location.

Minimal fixes made:

- Fixed `SaveManager.SaveGameAsync` so `data.version = CurrentSaveVersion` is executed before serializing JSON.
- Updated `PA_LongPlayProgressionValidator` to inject a project-local validation repository at `Logs/LongPlayValidationSaves/`.
- Added Day 3 save/load checks to `PA_LongPlayProgressionValidator`.

Validation results:

- Compile:
  - Log: `Logs/Codex_LongPlay_Compile.log`
  - Result: passed.
  - No C# compile errors found.
- Day 1 route:
  - Log: `Logs/Codex_LongPlay_Day1RouteValidation2.log`
  - Result: passed.
  - Evidence: `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`.
- Day 2-7 long-play:
  - Log: `Logs/Codex_LongPlay_ProgressionValidation.log`
  - Result: passed.
  - Day 2-7 producer deliveries increased sellable inventory.
  - Day 3 save/load restored current day, money, long-play supply day, day-start revenue, day-start money, delivered inventory, and HUD.
  - Final evidence: `PA Long Play Validation passed. sellableInventory=38, money=4633G`.

Function preservation notes:

- Day 1 route remains validated.
- Existing shop, shop slot, price UI, economy, purchase evaluator, NPC, tier, audit, and core save logic were not broadly rewritten.
- The save fix was limited to ensuring the already intended schema version assignment actually executes.

Next automatic sprint:

- FG-004 Processing And Crafting Chain first pass.
- Goal: make raw producer goods vs processed goods visible as a management decision without changing existing crafting behavior.

## 2026-06-20 FG-004 Processing Chain First Pass

Goal:

- Continue automatically after FG-001/FG-002 validation.
- Add the first management-readable processing layer.
- Keep existing recipe/crafting logic intact.

Implemented:

- Added `Assets/Scripts/ProcessingOpportunityController.cs`.
- Added `Assets/Editor/PA_ProcessingChainValidator.cs`.
- Updated `Assets/Scripts/PA_RuntimeSceneBinder.cs` so the processing advisor is attached at runtime.
- The advisor reads existing `Resources/Recipes`, computes raw input value, expected processed output value, estimated margin, and whether inputs are currently available.
- The advisor shows a Day 4+ `Processing Focus` panel instead of changing crafting behavior.

Validation:

- Compile:
  - Log: `Logs/Codex_ProcessingChain_Compile.log`
  - Result: passed.
  - No C# compile errors found; existing source-generator warning remains non-blocking.
- Processing chain:
  - Log: `Logs/Codex_ProcessingChain_Validation.log`
  - Result: passed.
  - Evidence: `PA Processing Chain Validation passed. recipe=BreadLoaf, margin=3G`.
- Long-play regression:
  - Log: `Logs/Codex_ProcessingChain_LongPlayRegression.log`
  - Result: passed.
  - Evidence: `PA Long Play Validation passed. sellableInventory=38, money=4633G`.

Function preservation notes:

- `CraftingService`, `RecipeData`, `Workbench`, and `CraftingUI` were not rewritten.
- Existing recipes were reused.
- No new external packages or reference assets were added.
- The processing advisor is a sidecar readability/management layer.

Next recommended sprint:

- FG-003 Customer Simulation first pass.
- Recommended scope: customer demand/category insight based on existing `PurchaseEvaluator`, `NpcProfile`, NPC feedback, and sales logs, without changing purchase probability math.

## 2026-06-20 FG-003 Customer Demand Insight First Pass

Goal:

- Continue automatically after FG-004 first pass.
- Expose customer demand as a management signal.
- Do not change `PurchaseEvaluator` purchase probability or NPC sale behavior.

Implemented:

- Added `Assets/Scripts/CustomerDemandInsightController.cs`.
- Added `Assets/Editor/PA_CustomerDemandInsightValidator.cs`.
- Updated `Assets/Scripts/PA_RuntimeSceneBinder.cs` to attach the demand insight controller.
- Added a narrow observer hook in `Assets/Scripts/NpcController.cs` after purchase evaluation.
- The controller tracks item category, buy/pass result, probability, display price, and latest signal.
- The controller shows a Day 3+ `Demand Signals` panel.

Validation:

- Compile:
  - Log: `Logs/Codex_CustomerDemand_Compile.log`
  - Result: passed.
  - No C# compile errors found; existing source-generator warning remains non-blocking.
- Demand insight:
  - Log: `Logs/Codex_CustomerDemand_Validation.log`
  - Result: passed.
  - Evidence: `PA Customer Demand Insight Validation passed. item=BreadLoaf, category=Processed`.
- Day 1 regression:
  - Log: `Logs/Codex_CustomerDemand_Day1Regression.log`
  - Result: passed.
  - Evidence: `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`.
- Long-play regression:
  - Log: `Logs/Codex_CustomerDemand_LongPlayRegression.log`
  - Result: passed.
  - Evidence: `PA Long Play Validation passed. sellableInventory=38, money=4633G`.

Function preservation notes:

- `PurchaseEvaluator` was not changed.
- The NPC observer hook does not change buy/pass result, sale logic, money, inventory, or FSM behavior.
- Existing Day 1 and Day 2-7 validations still pass.

Next recommended sprint:

- FG-009 Tier / Audit / Reputation Progression.
- Recommended scope: connect revenue, supply intake, processing opportunities, and demand signals into Week 1 audit feedback without changing tier unlock math yet.

## 2026-06-21 Creative North Star Documentation Pass

Goal:

- Re-lock Project_PA's top creative direction before any further long-term implementation.
- Prevent the project from drifting into a passive management-only sim, simple shop-only game, or ReferencePrototype clone.

Completed:

- Created `PROJECT_PA_CREATIVE_NORTH_STAR.md`.
- Reframed Project_PA as a cozy 3D life and shop management simulation.
- Preserved reverse supply-chain management as the economic backbone behind the day-to-night shop loop.
- Reinterpreted NPC production, hiring, specialist, tier, audit, and automation systems as growth/support systems that reduce repetitive labor over time.
- Redefined current Milestone 1 as `Cozy Day-To-Night Shop Loop`.
- Reorganized backlog direction around day/night play, island life, shop personality, village change, NPC life, growth, and presentation.
- Updated reference/migration wording so Project_D remains visual/staging/readability reference only.

Modified or created files:

- `PROJECT_PA_CREATIVE_NORTH_STAR.md`
- `PROJECT_PA_DESIGN_INTENT.md`
- `PROJECT_PA_COMPLETION_PLAN.md`
- `PROJECT_PA_RELEASE_BACKLOG.md`
- `PROJECT_PA_CURRENT_MILESTONE.md`
- `PROJECT_PA_TODO.md`
- `PROJECT_PA_MIGRATION_PLAN.md`
- `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md`
- `PROJECT_PA_FULL_GAME_BACKLOG.md`
- `PROJECT_PA_STATUS.md`
- `PROJECT_PA_SESSION_REPORT.md`
- `README.md`

Function preservation notes:

- No gameplay code was modified.
- No scenes were modified.
- No UI implementation was modified.
- No assets were copied.
- No Project_D files were touched.
- No Unity build or Play Mode run was performed in this documentation-only pass.

Next recommended implementation sprint:

- CN-001 Cozy Day-To-Night Core.
- CN-002 Island Life Stock Sources.
- CN-003 Product Category Village Change.

Human review checklist:

- Read `PROJECT_PA_CREATIVE_NORTH_STAR.md`.
- Confirm the new direction is preferred before code work resumes.
- Next implementation should add day/night phase, shop open/close, two stock-prep activities, and category-based village change in small validated steps.

## 2026-06-21 Milestone 1 Implementation Continuation

Goal:

- Start `Milestone 1 - Cozy Day-To-Night Shop Loop`.
- Preserve the existing Day 1 route while adding the first day/night, stock-prep, and village-change signals.

Completed:

- Confirmed current working directory and Git root are `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Read the creative north star, design intent, current milestone, full-game backlog, and TODO before implementation.
- Added `Assets/Scripts/DayNightShopLoopController.cs`.
- Added `Assets/Scripts/DaytimeStockPrepPoint.cs`.
- Added `Assets/Scripts/VillageChangeSignalController.cs`.
- Updated `Assets/Scripts/PA_RuntimeSceneBinder.cs` so the new sidecar services attach at runtime.
- Added `Assets/Editor/PA_DayNightShopLoopValidator.cs`.
- Added `Assets/Editor/PA_VillageChangeSignalValidator.cs`.

Implemented behavior:

- Day/night phase state now exists as `DayPreparation`, `ShopOpen`, and `Settlement`.
- Shop open/close state is visible through `IsShopOpen` and the runtime HUD.
- Day 1 tutorial shop access remains open so the existing first-day route is not blocked by the new phase layer.
- Two runtime day-prep stock sources can add sellable stock once per day during the day-prep phase:
  - `Garden Prep Basket`
  - `Producer Drop Box`
- Recent sales now produce a read-only `Village Direction` signal based on product category.
- Day 1 summary now includes a `Village direction` section sourced from recent sales.

Validation:

- Day/night validator passed:
  - `Logs/Codex_DayNight_TwoPrep_Validation.log`
  - `PA Day Night Shop Loop Validation passed. sellableInventory=4`
- Village-change signal validator passed:
  - `Logs/Codex_VillageSignal_Validation.log`
  - `PA Village Change Signal Validation passed. summary=Processed: 2 sale(s), 76G influence`
- Day 1 route regression passed:
  - `Logs/Codex_SettlementVillage_Day1Validation.log`
  - `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`
  - `PA Final Route Check OK: Day 1 summary includes village direction section`
- Day 2-7 long-play regression passed:
  - `Logs/Codex_SettlementVillage_LongPlayRegression.log`
  - `PA Long Play Validation passed. sellableInventory=38, money=4633G`

Function preservation notes:

- No Project_D files were modified or copied.
- No external packages were imported.
- No Git commit or push was performed.
- `Shop`, `ShopSlot`, `ShopPriceUI`, `Inventory`, `Hotbar`, `EconomyService`, `PurchaseEvaluator`, `NpcController`, save, tier, and audit logic were not broadly rewritten.
- The new systems are sidecar/readability layers and use existing inventory/sales APIs.

Remaining Milestone 1 work:

- Replace the two MVP daytime stock sources with richer cozy activities later, if desired.
- Review the new top/right HUD panels in a real 1920x1080 Game view.
- Decide when to make shop open/close state actively gate customers; current first pass keeps behavior permissive to preserve Day 1.

## 2026-06-25 Unity Editor Crash Repair

Goal:

- Diagnose and repair a Unity Editor hard crash that opened Bug Reporter instead of showing a normal C# Console error.

Completed:

- Read the latest Unity Editor crash logs.
- Confirmed the crash root was native D3D12 device removal/hang:
  - `d3d12: Device failed error (887a0005)`
  - `d3d12: Device removed reason (887a0006)`
  - native stack at `D3D12SwapChain::Present`
- Confirmed the log did not point to C# compile errors, NavMesh, save/load, or a managed UI exception as the primary crash cause.
- Updated `ProjectSettings/ProjectSettings.asset` so Standalone graphics API is fixed to Direct3D 11 instead of automatic/D3D12.
- Created `PROJECT_PA_CRASH_REPORT_20260625.md`.

Verification:

- D3D11 forced project load passed:
  - `Logs/Codex_CrashRepair_D3D11_Load.log`
  - `Forcing GfxDevice: Direct3D 11`
  - `Tundra build success`
- Project graphics API load without `-force-d3d11` passed:
  - `Logs/Codex_CrashRepair_ProjectGraphicsAPI_Load.log`
  - `Version: Direct3D 11.0`
  - `Tundra build success`
- Final route regression passed after the repair:
  - `Logs/Codex_CrashRepair_FinalRoute_D3D11.log`
  - `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`

Remaining manual check:

- Reopen Unity Editor normally from Hub or command line and confirm it no longer selects D3D12 or opens Bug Reporter.

## 2026-06-22 SPY-002 Customer Type / Preference Presentation

Goal:

- Implement SPY-002, the remaining Milestone 1 customer-type requirement.
- Make NPCs read as residents with their own taste, and explain buy/reject decisions, without changing purchase math.

Investigated existing systems before coding:

- `NpcProfile` — MBTI 4-axis (`traitEI/SN/TF/JP`) plus `socialWeight`, `priceSensitivity`, `utilityConsumption`, `luxuryConsumption`. Confirmed `traitSN/TF/EI` differ across the eight scene profiles, but `priceSensitivity`/`utility`/`luxury` are all 1.0.
- `PurchaseEvaluator` — deterministic; returns `Result { willBuy, probability, reason(debug) }`. Category bonus comes from `traitSN`; F amplifies preference, T strengthens price sensitivity; E/I give an impulse bias.
- `NpcController.EvaluateCurrentSlot` — already calls `PurchaseEvaluator`, builds a head bubble via `BuildPurchaseFeedback`, records scenario feedback, and calls `CustomerDemandInsightController.RecordEvaluation`.
- `CustomerDemandInsightController` / `VillageChangeSignalController` / `DayNightShopLoopController` — read-only sidecar pattern (singleton + own ScreenSpaceOverlay canvas, 1920x1080 scaler). Mapped the existing HUD stack to avoid overlap.
- `PA_FinalDemoRouteValidator` — asserts the head bubble text and `BuildPurchaseFeedback` length <= 34, so that path was left untouched.

Implemented:

- `Assets/Scripts/UI/CustomerPreferencePresentationController.cs` (top-right "관심 손님 성향" panel).
- `Assets/Scripts/UI/PurchaseFeedbackPresentationController.cs` (bottom-right "손님 반응" feed with village tie line, no numbers).
- One read-only hook added to `Assets/Scripts/NpcController.cs`.
- `Assets/Scripts/PA_RuntimeSceneBinder.cs` registers both controllers.
- `Assets/Editor/PA_CustomerPresentationValidator.cs` (new validator).
- `Docs/CustomerPresentation/README.md` (data sources, display rules, checklist).

Function preservation notes:

- No Project_D files were modified or copied; no packages imported; no commit/push.
- `PurchaseEvaluator`, `Shop`, `ShopSlot`, `ShopPriceUI`, `EconomyService`, NPC FSM, and Save schema were not changed.
- The CL-002 head bubble (with %) is preserved; FinalDemoRoute still reports the same bubble text.
- The new hook cannot change `willBuy`, sale, money, inventory, or FSM state.

Validation (all passed):

- Compile: `Logs/Codex_SPY002_Compile.log` (no `error CS`).
- `Logs/Codex_SPY002_PresentationValidation.log` (new SPY-002 validator).
- `Logs/Codex_SPY002_FinalRouteRegression.log`.
- `Logs/Codex_SPY002_DayNightRegression.log`.
- `Logs/Codex_SPY002_VillageRegression.log`.
- `Logs/Codex_SPY002_LongPlayRegression.log`.

Remaining:

- Manual 1920x1080 Game-view readability review of the two new panels.
- Optional: over-head preference tag, per-NPC price-sensitivity data, and a preference -> village -> unlock chain.

## 2026-06-22 SPY-003 Per-Resident Consumption Data

Goal:

- Close the data-honesty gap from SPY-002: make the "가격에 민감/관대" preference hint appear from real, per-NPC data.

Verified before editing that the data change is regression-safe:

- `PurchaseEvaluator.priceSensitivity` only applies when ratio > 1; FinalDemoRoute stocks at base price (ratio <= 1).
- `utilityConsumption`/`luxuryConsumption` only scale Utility/Luxury bonuses; FinalDemoRoute uses BreadLoaf (Processed).
- LongPlay money is deterministic producer buy-in, not live NPC sales.

Implemented (data + one validator only):

- Edited the eight `Assets/Resources/NPCs/Profile_*.asset` files: `priceSensitivity` 0.65 ~ 1.45, plus job-fitting `utilityConsumption`/`luxuryConsumption`.
- Extended `Assets/Editor/PA_CustomerPresentationValidator.cs` to assert Miner shows "가격에 민감", Tailor shows "가격에 관대", and Blacksmith (neutral price) falls back to a personality style.
- Updated `Docs/CustomerPresentation/README.md` with the per-resident table and re-validation results.

Function preservation notes:

- No `PurchaseEvaluator`, `Shop`, `ShopSlot`, `ShopPriceUI`, `EconomyService`, NPC FSM, or Save code was changed.
- `CustomerPreferencePresentationController` already read `priceSensitivity`, so the hint became data-driven with no controller change.
- No Project_D edit/copy, no package import, no commit/push.

Validation (all passed):

- Compile: `Logs/Codex_SPY003_Compile.log` (no `error CS`).
- `Logs/Codex_SPY003_PresentationValidation.log` — `장식·고급품 선호 · 가격에 민감` / `실용재(식료품·도구) 선호 · 가격에 관대`.
- `Logs/Codex_SPY003_FinalRouteRegression.log` — `stocked=BreadLoaf, paid=30G`.
- `Logs/Codex_SPY003_DayNightRegression.log`.
- `Logs/Codex_SPY003_VillageRegression.log`.
- `Logs/Codex_SPY003_LongPlayRegression.log` — `money=4633G` unchanged.

Remaining:

- Manual 1920x1080 Game-view readability review of the two SPY-002 panels (now showing the new price hints).

## 2026-06-22 SPY-002 Panel Layout Validation

Goal:

- Close the "manual panel-overlap review" gap with an automated, coordinate-based check plus a 1920x1080 screenshot for human reference.

Reused existing helpers:

- Modeled the harness on `PA_CustomerPresentationValidator` and the screen-rect/capture approach from `PA_FinalDemoRouteValidator` / `PA_FinalPresentationReviewer`.

Implemented (presentation/QA only):

- `Assets/Editor/PA_CustomerPanelLayoutValidator.cs`
  - 1920x1080 Play Mode, dismisses the onboarding modal, fills both panels (RecordDecision + a few NPCs set to MovingToShop) for a meaningful shot.
  - Checks both new panels for screen containment, mutual non-overlap, non-overlap with MoneyHUD/Demand/Village (top-right stack), and non-overlap with the hotbar (union of active `InventorySlotUI` rects).
  - Captures `Logs/CustomerPanelReview/<timestamp>/customer_panels_1920x1080.png`.

Result:

- Run 1 (`Logs/Codex_PanelLayout_Validation.log`) passed panel/HUD overlap + bounds, but skipped the hotbar check (`InventoryUI.slotParent` not found).
- The validator was improved (hotbar region from active `InventorySlotUI`; onboarding modal dismissed). Run 2 (`Logs/Codex_PanelLayout_Validation2.log`) then caught a real overlap: the `손님 반응` panel overlapped the hotbar's right edge.
- Fix: raised `PurchaseFeedbackPresentationController` `anchoredPosition.y` 24 → 170 (presentation-only, one coordinate). Run 3 (`Logs/Codex_PanelLayout_Validation3.log`): all checks pass incl. hotbar; `finished successfully`.
- Clean screenshot (modal dismissed): `Logs/CustomerPanelReview/20260622_112554/customer_panels_1920x1080.png` — inspected: feedback panel sits above the hotbar, right-side panels separated from the top-right Money/Village stack, Korean renders without tofu.
- After the panel move, re-ran all five existing validators — all passed (`Logs/Codex_PanelLayout_Reg_{Presentation,FinalRoute,DayNight,Village,LongPlay}.log`; `paid=30G`, `money=4633G` unchanged).
- Note: a pre-existing non-blocking `warning CS8785` (Unity source generator) and a licensing-token warning appear in logs; both predate this work and do not block compilation.

Function preservation:

- No gameplay/economy/NPC/save logic changed. Editor-only validator; not shipped in the player.
- No Project_D edit/copy, no package import, no commit/push.

Remaining:

- Human readability + Korean-font confirmation on a real monitor (geometry automated, aesthetics not).

## 2026-06-24 IL-001 + CDN-002 Real Gathering & Night Shop Gate

Goal: turn Milestone 1 from display-only prep + always-open shop into a real loop (gather by day, open shop at night for customers, settle, reset).

Investigated first: `DaytimeStockPrepPoint` (already daily-reset + sellable grant), `Gatherable`/`IInteractable`/`PlayerInteraction` (trigger colliders detected), `Item`/Resources items (existing Raw items), `SaveData`/`SaveManager` (v7 long-play pattern), `NpcController.EvaluateCurrentSlot` (purchase entry), `ShopSlot`/`ShopPriceUI`.

Implemented:
- IL-001: 3 spread wild-forage points via `DayNightShopLoopController.EnsureDayPrepPoints` (reuse `DaytimeStockPrepPoint`, existing Raw items, daily reset, trigger colliders). Backups kept.
- CDN-002: `IsShopOpenForCustomers` gate + `ShopOpenSign` open action + HUD; one guard block in `NpcController.EvaluateCurrentSlot`. Day 1 tutorial override preserved.
- Save v8 additive extension + DayNight save/restore hooks.
- New validators `PA_GatheringShopGateValidator`, `PA_GatheringShopReview`; updated `PA_DayNightShopLoopValidator` 1 line.

Function preservation: `PurchaseEvaluator`/`Shop`/`ShopSlot`/`EconomyService`/NPC FSM unchanged; Save change is additive (v8). No Project_D edit/copy, no packages, no commit/push.

Validation: new gate validator (33 checks) + 6 regressions all passed; 5 1920x1080 screenshots captured and reviewed. Logs under `Logs/Codex_IL001_*` and `Logs/GatheringShopReview/20260624_141557/`. Details: `Docs/IslandLife/GATHERING_AND_SHOP_GATE.md`.

Remaining: human play-feel pass + visual polish for placeholder forage/sign cubes + fixed-terrain forage placement.

## 2026-06-24 Customer Arrival Pacing (IL/CDN follow-up)

Goal: make opening the shop (CDN-002) actively pull customers in, instead of relying only on each NPC's random shopping FSM.

Investigated: `NpcScheduleController` drives shopping via `SchedulePhase.Shopping` (`SetShoppingPriority`/`TryForceShop`); `PlayableDayScenarioController` drives Day 1 customers; `NpcController.TryForceShop` no-ops on paused/non-idle NPCs (safe).

Implemented:
- `CustomerArrivalController` (read-only/event sidecar): when the shop is open for customers and not the Day 1 tutorial, invites idle customers one at a time (staggered) up to a concurrent cap via the existing idempotent `SetShoppingPriority`/`TryForceShop` API; disperses on close. Day 1 stays passive to preserve the scenario controller's tutorial pacing.
- Registered via `PA_RuntimeSceneBinder` (1 line).
- New `PA_CustomerArrivalValidator` (16 checks).

Function preservation: no change to `PurchaseEvaluator`/`ShopSlot`/`EconomyService`/NPC FSM internals or Save; only new sidecar + binder line. No Project_D edit/copy, no packages, no commit/push.

Validation: new validator + all 7 existing validators passed. Logs `Logs/Codex_Arrival_*`. Details: `Docs/IslandLife/GATHERING_AND_SHOP_GATE.md`.

Remaining: invite-interval/cap feel tuning; preference/relationship-biased arrival order; placeholder visual polish.

## 2026-06-25 Day 1-3 Core Slice Baseline

Goal:

- Treat Project_PA as a long-term full cozy management life-sim, not just a prototype to clean up.
- Define Day 1-3 as the first full-game core slice.
- Reduce visible debug/advisor clutter without removing validation systems.

Completed:

- Confirmed the active working path is `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Confirmed the Git root is `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Read the required creative/design/status/TODO/session/README planning context.
- Inventoried visible runtime UI and presentation objects from scripts and the current scene:
  - Player-facing: objective, money/tier, clock/date, hotbar/inventory, interact prompt, dialogue, ShopPriceUI, smartphone/audit, NPC bubble, Day 1 flow/summary, Day/Night phase HUD, market hub signs/price tags.
  - Development/presentation clutter: LongPlay, Processing, Demand, Village, Customer Preference, Purchase Feedback side panels, path-step route markers, role badges, screenshot marker, and route-only labels.
- Created `PROJECT_PA_CORE_SLICE_PLAN.md`.
- Added `Assets/Scripts/CoreSlicePresentationMode.cs`.
- Registered `CoreSlicePresentationMode` in `Assets/Scripts/PA_RuntimeSceneBinder.cs`.
- Updated `Assembly-CSharp.csproj` to include the new file for local C# build verification.

Implemented behavior:

- Development/advisor canvases are hidden by default with `CanvasGroup.alpha = 0` instead of being deleted or disabled.
- Route/debug world markers are hidden by default by disabling renderers/TMP/graphics/colliders.
- Core player HUD remains visible.
- Press `F10` during runtime to toggle development overlays back on for debugging or screenshots.

Function preservation:

- No Project_D files were modified or copied.
- No external packages were imported.
- No Git commit or push was performed.
- No gameplay core systems were rewritten.
- `Shop`, `ShopSlot`, `ShopPriceUI`, `Inventory`, `Hotbar`, `EconomyService`, `PurchaseEvaluator`, `NpcController`, `SalesLogManager`, `GameClock`, `DayNightShopLoopController`, `SaveManager`, `TierService`, and `AuditService` were preserved.

Validation:

- `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` passed with 0 warnings and 0 errors after adding the script `.meta`.
- Unity batch `PA_FinalDemoRouteValidator` was attempted but aborted because another Unity Editor instance already has this project open.
- Unity batch `PA_LongPlayProgressionValidator` was attempted but aborted for the same project-open lock.

Remaining:

- Run the Final Demo Route validator from the open Unity Editor menu or after closing the Editor.
- Run the Long Play Progression validator from the open Unity Editor menu or after closing the Editor.
- Manually inspect the Game view with development overlays hidden by default.
- Next sprint should replace placeholder daytime sources/route labels with diegetic low-poly activity nodes and fold key management signals into cleaner player-facing UI.

## 2026-06-25 Day 1-3 Core Slice Playability Pass

Goal:

- Continue the long-term full-game Day 1-3 core-slice direction.
- Check that the player-facing view is understandable without development/advisor clutter.
- Make only minimal visual/UI/readability fixes.

Completed:

- Added `Assets/Editor/PA_CoreSlicePlayabilityValidator.cs` and `.meta`.
- Added the new editor validator to `Assembly-CSharp-Editor.csproj` for local build verification.
- Moved `DayNightShopLoopPanel` below the Day 1 objective HUD instead of sharing the same top-center position.
- Left `CoreSlicePresentationMode` as the default overlay filter and kept F10 as the runtime development overlay toggle.

Function preservation:

- No Project_D edit/copy.
- No external package import.
- No Git push or commit.
- No core shop/economy/NPC/save rewrite.
- Preserved `Shop`, `ShopSlot`, `ShopPriceUI`, `Inventory`, `Hotbar`, `EconomyService`, `PurchaseEvaluator`, `NpcController`, `DayNightShopLoopController`, `CustomerArrivalController`, and `SaveManager`.

Validation:

- Runtime C# build passed with 0 errors.
- Editor C# build passed with 0 errors.
- Existing warnings remain: Unity source generator `CS8785`, plus unused `PA_ErrorTracker._autoScrollNew`.
- Batch Core Slice validator was attempted but could not run because Unity Editor already has the project open. Log: `Logs/Codex_CoreSlice_PlayabilityValidation.log`.

Still required:

- From the open Unity Editor menu, run `Project PA/Validation/Run Core Slice Playability Validation`.
- From the open Unity Editor menu, run Final Demo Route and Long Play Progression validators.
- Manual Game-view check: F10 overlay toggle, hidden development panels/path labels, Day/Night panel below objective, and Day 1-3 comprehension.

## 2026-06-26 Loop Engineering Dry-Run Guardrails

Goal:

- Create a safe, shared Codex/Claude development loop policy without actually starting autonomous implementation.
- Fill the missing `Docs/07_개발일지.md` June development records instead of only preparing commit-style summaries.

Completed:

- Created root guidance files: `AGENTS.md`, `CLAUDE.md`.
- Created `Docs/AgentWorkflow/CONTEXT_INDEX.md` to reduce context overload by task type.
- Created dry-run loop files under `Automation/LoopEngineering/`.
- Created `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1`.
- Created `Automation/LoopEngineering/validator-registry.json` from validators that actually exist in `Assets/Editor`.
- Ran preflight with a one-time PowerShell execution-policy bypass and saved result to `Automation/LoopEngineering/RunLogs/preflight-20260626.json`.
- Updated `Docs/07_개발일지.md` with missing June entries:
  - CL-001~CL-004 management loop
  - master plan / full-game backlog
  - long-play progression
  - processing/customer demand
  - creative north star/design intent
  - day/night and village signal
  - customer presentation and panel validation
  - real gathering/shop gate
  - customer arrival
  - Unity D3D12 crash repair
  - Day 1-3 core slice and playability pass
  - Loop Engineering dry-run

Preflight result:

- Status: `BLOCKED_BY_DIRTY_GIT`
- Root: correct
- Git root: correct
- Unity process count: 0
- Latest crash report: `PROJECT_PA_CRASH_REPORT_20260625.md`
- Evidence: `Automation/LoopEngineering/RunLogs/preflight-20260626.json`

Function preservation:

- No Project_D edit/copy.
- No gameplay, scene, prefab, material, package, ProjectSettings, commit, or push work.
- No Unity validator was run by this dry-run ticket.

Next:

- Human should approve the current dirty baseline and crash repair baseline before any implementation loop proceeds.

## 2026-06-26 Baseline Commit Review

Goal:

- Organize the current dirty Git state into a safe local baseline commit candidate.
- Provide staging guidance without running `git add`, `git commit`, `git push`, `git reset`, or `git clean`.

Completed:

- Verified Project_PA root and Git root.
- Captured branch and last commit:
  - branch `master`
  - last commit `adc5d2f 프로토타입 구성 -1`
- Counted dirty state:
  - 141 status lines
  - 27 modified tracked paths
  - 114 untracked paths
- Reviewed `.gitignore`.
- Read `PROJECT_PA_CRASH_REPORT_20260625.md`.
- Created `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md`.

Review conclusions:

- Core Project_PA code/docs/validators/tooling should be considered for the baseline.
- `Assets/Scenes/_Backups/`, `Docs/VisualTargets/`, `SubmissionPackages/`, build settings, and solution metadata require user review before inclusion.
- Cache/build/log folders should stay excluded.
- Suggested `.gitignore` additions were documented only; `.gitignore` was not modified.

Function preservation:

- No gameplay file was modified by this review task.
- No Unity scene/code/UI/asset/ProjectSettings file was modified by this review task.
- No Git write command was run.

## 2026-06-26 VC-001A Preflight Block

Goal:

- Start bounded ticket VC-001A: one next-day market/plaza visual change driven by previous-day sold category.
- Obey the bounded-ticket loop gate before touching scene, runtime code, UI, or assets.

Result:

- `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1` returned `BLOCKED_BY_DIRTY_GIT`, not `READY_FOR_BOUNDED_TICKET_LOOP`.
- The ticket was stopped before implementation.
- Unity process count was 0 during the check.
- `PROJECT_PA_CRASH_REPORT_20260625.md` is still a human review gate for launch-heavy automation.

Files updated:

- `Automation/LoopEngineering/State/loop-state.json`
- `Automation/LoopEngineering/progress.md`
- `Docs/VillageCulture/VC-001A.md`
- `PROJECT_PA_STATUS.md`
- `PROJECT_PA_TODO.md`
- `PROJECT_PA_CURRENT_MILESTONE.md`
- `PROJECT_PA_FULL_GAME_BACKLOG.md`
- `PROJECT_PA_SESSION_REPORT.md`

Function preservation:

- No VC-001A gameplay implementation was started.
- No Project_D files were modified or copied.
- No Unity scene, gameplay code, UI behavior, prefab, material, ProjectSettings, save schema, commit, or push was performed.
- Existing shop/economy/NPC/save/day-night systems remain untouched by this ticket attempt.

Next:

- Human baseline review is required before VC-001A can resume.
- After baseline acceptance, rerun preflight and continue only if the result is `READY_FOR_BOUNDED_TICKET_LOOP`.

## 2026-06-26 BASELINE-001 Bounded Ticket Loop Baseline Preparation

Goal:

- Prepare the current dirty Project_PA worktree for a user-created local checkpoint commit.
- Record the user's D3D11 manual stability confirmation in the Loop Engineering harness.
- Make preflight distinguish a reviewed D3D11 crash baseline from an unresolved crash artifact.

Completed:

- Verified current path and Git root are `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Captured current branch `master` and last commit `adc5d2f 프로토타입 구성 -1`.
- Captured dirty state:
  - 144 status entries with `git status --porcelain=v1 -uall`
  - 27 modified tracked paths
  - 117 untracked paths/files
  - `git diff --stat`: 27 tracked files, 3173 insertions, 129 deletions
- Confirmed no Unity Editor process for Project_PA was detected.
- Updated `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md`.
- Created `Automation/LoopEngineering/State/crash-resolution.json`.
- Updated `Automation/LoopEngineering/loop-policy.json`.
- Updated `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1`.
- Updated loop state/progress/status/TODO/session records.

D3D11 manual stability record:

- Recorded exactly what the user confirmed: Unity was manually run on the `-force-d3d11` baseline, and project opening plus Play Mode stability were manually confirmed.
- Did not claim D3D12 is resolved.
- Did not invent any validator, build, route, or screenshot result.

Preflight after correction:

- `BLOCKED_BY_DIRTY_GIT`
- `CrashResolutionValid: True`
- `CrashResolutionStatus: human_verified_d3d11_stable`
- `ApprovedGraphicsBackend: D3D11`
- `UnityProcessCount: 0`

Function preservation:

- No gameplay code, UI, scene, prefab, material, asset, Package, ProjectSettings, or Project_D file was modified.
- No Unity Editor or Unity batch validator was launched.
- No Git add, commit, push, reset, or clean command was run.

Status:

- `needs_human_review`
- User must create or explicitly accept the local checkpoint baseline before bounded ticket development resumes.

## 2026-06-26 VC-001A Village Culture Visual Change

Goal:

- Make one sold product category create a small visible next-day market/plaza response.

Implemented:

- Added `Assets/Scripts/VillageCultureVisualController.cs`.
- Added `Assets/Editor/PA_VillageCultureVisualValidator.cs`.
- Reused existing sales data from `SalesLogManager` and category interpretation from the current Village Direction path.
- Selected `Processed` because the default Day 1 route sells `BreadLoaf`.
- Added runtime visual root `PA_VillageCulture_Processed`, built from Project_PA-owned Unity primitives/materials.
- The visual is inactive at Day 1 start, remains inactive immediately after the sale, and activates on next `DayPreparation`.
- Added a one-time non-debug hint when the visual appears.

Function preservation:

- No Project_D file was read for implementation, modified, copied, or merged.
- No scene file was edited.
- No Save schema field was added.
- No package or ProjectSettings file was modified.
- No core rewrite of shop, economy, NPC purchase, day/night, or save logic.

Validation:

- `PA_VillageCultureVisualValidator` passed.
- `PA_FinalDemoRouteValidator` passed.
- `PA_DayNightShopLoopValidator` passed.
- `PA_VillageChangeSignalValidator` passed.
- `PA_LongPlayProgressionValidator` passed.
- `PA_CustomerPresentationValidator` passed.
- `PA_CustomerPanelLayoutValidator` passed.

Evidence:

- `Logs/Codex_VC001A_VillageCultureVisual.log`
- `Logs/Codex_VC001A_FinalDemoRouteRegression.log`
- `Logs/Codex_VC001A_DayNightRegression.log`
- `Logs/Codex_VC001A_VillageSignalRegression.log`
- `Logs/Codex_VC001A_LongPlayRegression.log`
- `Logs/Codex_VC001A_CustomerPresentationRegression.log`
- `Logs/Codex_VC001A_CustomerPanelLayoutRegression.log`
- `Logs/VillageCultureVisual/20260626_145257/vc001a_day1_default_no_change.png`
- `Logs/VillageCultureVisual/20260626_145257/vc001a_after_processed_sale_same_day.png`
- `Logs/VillageCultureVisual/20260626_145257/vc001a_day2_preparation_visual_active.png`

Remaining:

- Human visual quality review is still needed.
- Future category variants should stay additive and should not become persistent unlocks until a deliberate village-culture save design exists.

## Session 2026-07-09 - AI_WORKFLOW Operating Structure

Goal: build the AI_WORKFLOW documentation structure so future Codex sessions can develop Project P.A. toward a completable full game, based on the `AI_DOC_CLEANUP_PLAN.md` audit.

Completed:

- Created `AI_WORKFLOW/` with 16 new documents: entry manual (`ONE_PAGE_WORKFLOW.md`), document map (`DOCS_INDEX.md`), identity set (`PROJECT_PA_IDENTITY.md`, `PROJECT_PA_GAME_LOOP.md`, `PROJECT_PA_SCOPE.md`), agent rules (`CODEX_WORKER_RULES.md`, `UNITY_CODING_RULES.md`, `AI_SLOP_PREVENTION.md`), verification rules, logs (`CHANGELOG_AI.md`, `BUG_LOG.md`, `DECISION_LOG.md`), handoff (`HANDOFF_FOR_CODEX.md`), full-game roadmap (Stage 0 MVP Stabilization through Stage 5 Release Candidate), tasks placeholder, and archive README.
- Rewrote root `AGENTS.md` as a short entry guide; archived the 2026-06-26 version verbatim at `AI_WORKFLOW/99_ARCHIVE/old_docs/AGENTS_v1_20260626.md`.
- Fixed the goal framing in documentation: full game completion is the target; the graduation demo is a stable subset (Stage 2), not the end state.

Safety compliance:

- No code/scene/prefab/asset/meta changes. No deletions. No pushes or commits.
- Git preflight found an uncommitted VC-001A baseline (8 modified + 4 untracked files), so every planned `git mv` (Notion original plan doc, COMPLETION_PLAN, RELEASE_BACKLOG, crash report 20260624, two presentation docs) was deferred per the dirty-baseline gate. Pending list: `AI_WORKFLOW/00_START_HERE/DOCS_INDEX.md` section 7-B.
- `Docs/01~08` and the latest crash report were not touched (frozen: code-comment section citations, preflight root glob).

Next actions:

1. Human: checkpoint commit for VC-001A + this documentation pass.
2. Run the two deferred validators (Editor-closed batchmode on D3D11, or from the open Editor menu).
3. Execute deferred document moves with reference updates in the same commit.
4. Build `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md` after the human inputs listed in `AI_WORKFLOW/03_TASKS/README.md`.


---

# Session 2026-07-12 — Visual Demo Integration Pass (Claude Fable 5)

## Goal

기능 추가가 아니라 시각 통합: 기존 구현/에셋을 연결·배치·문구 정리해서 5분 데모가 "완성된 코지 상점 게임"으로 보이게 한다.

## Completed Work

- 신규 런타임 사이드카 `DemoVisualDressingController` — 채집 포인트 5곳/영업 간판 placeholder 큐브 드레싱 + 광장 소품(벤치 3·화단 4·가로등 2·궤짝/통). 씬 파일 무수정, 렌더러 전용.
- HUD 문구 한국어 통일 (`DayNightShopLoopController`, `DaytimeStockPrepPoint`) — 로직 무변경, 표시 문자열만.
- Day 요약 본문 잘림 수복 (기존 문제, 410px 본문 vs 360px 영역 → 418px 확장).
- `AI_WORKFLOW/09_FINAL_FABLE_SPRINT/` 5종 문서: 폴리시 리포트 / 장면 배치 계획 / UI 상태표 / 5분 루트 / 시각 격차·placeholder 계획.

## Verification

- dotnet build 런타임+에디터 0 오류.
- 검증기 6종 D3D11 batchmode 전부 통과: DayNightShopLoop(`sellableInventory=10`), FinalDemoRoute(`paid=30G`), FinalPresentationReviewer(재실행, 410/418), GatheringShopReview(캡처 5장), CoreSlicePlayability, LongPlayProgression(`money=4633G` 불변).
- 로그: `Logs/Fable_VisualPass_*.log`, 캡처: `Logs/FinalPresentation/20260712_161412/`, `Logs/GatheringShopReview/20260712_161528/`.
- 기존 BLOCKED 2종(FinalDemoRoute/LongPlay)이 Editor 닫힘 상태 D3D11 batchmode 로 실제 실행·통과됨.

## Not Verified (human)

- 광장 드레싱 전경의 주관적 품질(자동 캡처가 카운터 클로즈업 위주), 실기기 한국어 폰트, F10 토글 리허설.

## Git

- 시작: `master`, 기존 dirty 2건(SubmissionPackages zip 삭제 표시)만 존재.
- 종료: 위 2건 + 이번 작업 파일(신규 스크립트/meta, 수정 6, 문서 11)만 변경. 커밋/푸시 없음(승인 대기).


## v2 재작업 (같은 날 저녁) — 실제 Game View 불합격 → Before/After 기준 재작업

- 사용자 판정: v1 은 검증기 통과였을 뿐 실제 Game View 는 테스트맵 인상 그대로 (디버그 라벨/원시 큐브/맨땅/UI 겹침).
- 재작업: 실측 지오메트리(`PlazaFrame`) 기반 광장 베이스 플레이트 + 구획, `Guide_*` 라벨 숨김, 좌측 퀘스트 패널 + 상단 한 줄, 아이콘 6종 연결, 판매대 카운터/쇼케이스/간판 한국어화.
- 증거: Before `Logs/DemoViewShots/before_20260712_164931.png` → After `Logs/DemoViewShots/after5_20260712_223953.png` (동일 카메라/해상도/시간).
- 회귀: FinalRoute(`paid=30G`) / DayNight(`sellableInventory=10`) / PanelLayout 통과 (`Logs/Fable_VisualPass2_*.log`).
- 잔여(사람 확인): 조명 어둑함(시스템 미수정), 러그/파빙 가시성, 하단 기존 갈색 플랫폼, 분수 벤치 품질.

---

# Session 2026-07-13 — Visual Demo Integration Pass v3 Final Presentation Lock

## 재개와 결과

- Fable 사용량 제한으로 중단된 `after_final2` 직전부터 재개했다.
- Final Locked Screenshot: `Logs/DemoViewShots/after_locked_20260713_002356.png`.
- 15시대 따뜻한 조명, 기존 에셋 실모델 소품, 광장 소품, NPC 손님 2명, 좌우 HUD 톤을 실제 플레이 카메라에서 확인했다.
- 첫 `after_final2`는 에셋 재임포트 직후 런타임 머티리얼이 검게 캡처됐으나 코드 수정 없는 1회 재실행에서 재현되지 않았다.

## 검증

- dotnet build: 런타임/에디터 오류 0.
- FinalDemoRoute: PASS, `stocked=BreadLoaf`, `paid=30G`.
- DayNightShopLoop: PASS, `sellableInventory=10`.
- CustomerPanelLayout: PASS, 화면 내 배치 및 비겹침.
- CoreSlicePlayability: PASS, F10 기본 OFF와 ON/OFF 복구.
- FinalPresentationReviewer: PASS, 5장 캡처 및 Day 1 요약 410/418.

## 판정과 남은 확인

- 냉정한 판정: **조건부 발표용**.
- 이유: 기능 루프와 발표 증거는 안정적이나 중앙 상점 primitive 실루엣, 분홍 스마트폰 UI, 넓은 좌측 컬럼이 남는다.
- 사람 1회 확인: 실제 Editor Game View와 Final Locked Screenshot의 일치, 폰트/겹침/검은 머티리얼 미재현.

---

# Session 2026-07-13 — Full Game Completion Phase 0 Sync

- `/goal` 기준선을 `9898f6a`로 등록하고 85개 Task를 실제 증거로 감사했다.
- 기존 큐의 완료 표시는 실제 구현보다 뒤처져 있었다. FinalRoute/LongPlay 보류는 이미 해소됐고, 고객 프레젠테이션·마을 변화·발표 문서 등 다수 작업이 구현돼 있었다.
- 최종 집계: DONE 21 / PARTIAL 26 / TODO 12 / BLOCKED 6 / DECISION_REQUIRED 20.
- 가장 큰 완성 병목은 Persistence: v8 저장 코드 범위는 넓지만 실제 저장소 왕복 검증이 없다.
- 다음 스프린트: Task 007, Task 011, Task 018. Phase 0에서는 코드·씬·프리팹·SO를 변경하지 않았다.

## Task 007

- Save v8의 실제 필드, DTO, v0→v8 마이그레이션과 복원 의존 순서를 문서화했다.
- 판매 이력/마을 트렌드/VC-001A 상태가 저장되지 않는 경계를 확인했다.
- 코드 변경 및 Unity 실행 없음. 실제 저장소 왕복은 아직 확인 못 함(Task 011).

## Task 011

- 실제 SaveManager API를 격리 LocalJsonSaveRepository에 연결한 전용 검증기를 추가했다.
- v8 JSON 생성·변조·로드 후 돈/매출/위치/시간/인벤토리/핫바/상점/Day Prep/Day 1 진행 복원을 확인했다.
- 사용자 save와 저장 스키마는 건드리지 않았다. 컴파일·FinalRoute·DayNight도 PASS.

## Task 018

- 가격 UI의 아이템 이름 줄에 현재 슬롯 재고 수량을 추가했다.
- Before/After 동일 카메라 캡처에서 `BreadLoaf` → `BreadLoaf · 재고 1개` 변화를 확인했다.
- 첫 캡처는 재컴파일 직후 검은 런타임 머티리얼 아티팩트가 있었고, 코드 수정 없는 1회 재실행에서 해소됐다.
- Presentation/FinalRoute/DayNight/PanelLayout 전부 PASS.

---

# Session 2026-07-15 — Castle Build S4 Shop Evolution

## 플레이어 경험 변화

- 이전에는 실내 잡화점이 처음부터 출입 가능해 Tier 진행과 공간 성장이 분리되어 있었다.
- 이제 Tier 0에서는 외부 문이 잠기고 다음 조건을 안내한다.
- Tier 1 도달 시 간판·문 조명·해금 안내가 바뀌며, 같은 문으로 실내에 들어가 6슬롯 진열과 기존 손님 영업을 사용할 수 있다.
- 상태는 저장된 Tier에서 파생되어 별도 해금 플래그나 저장 마이그레이션이 필요 없다.

## 최소 안전 확인

- `dotnet build Assembly-CSharp.csproj`: 오류 0, 기존 CS8785 경고 1.
- `dotnet build Assembly-CSharp-Editor.csproj`: 오류 0, 기존 CS8785/CS0414 경고 2.
- D3D11 `PA_EnterableShopValidator`: Tier 0 잠금→Tier 1 해금→입장→진열→가격 UI→퇴장 PASS.
- 증거: `Logs/Codex_S4_EnterableShop.log`.

## 확인하지 않은 것

- 전체 회귀 검증기, 3일 연속 사람 플레이, Windows 빌드, 1920x1080 주관적 UI 가독성은 확인하지 않았다.
- 사람 확인 방법: Tier 0 새 게임에서 잡화점 문 프롬프트를 확인하고, Tier 1 세이브/검증 상태에서 OPEN 간판과 해금 패널이 기존 HUD를 가리지 않는지 본다.

## 다음 체크포인트

- 승인 없이 가능한 Task 034 구매/거절 일일 통계 연결을 우선한다.
- 밤 손님 시간 창은 NPC 스케줄 밸런스 결정이 필요한 별도 작업으로 유지한다.

---

# Session 2026-07-15 — Task 034 Daily Customer Decision Summary

## 플레이어 경험 변화

- 밤 정산에서 그날 손님 판단을 구매 수·보류 수·구매율로 바로 읽을 수 있다.
- 거절 비중이 높으면 다음 날 시작 안내가 가격 점검을 준비 목표로 제안한다.
- Day 1 결산과 일반 일일 루프가 같은 `SalesLogManager` 통계를 사용한다.

## 최소 안전 확인

- `dotnet build Assembly-CSharp.csproj`: 오류 0, 기존 CS8785 경고 1.
- `dotnet build Assembly-CSharp-Editor.csproj`: 오류 0, 기존 CS8785/CS0414 경고.
- D3D11 `PA_CustomerDemandInsightValidator`: 구매 1건·거절 1건·구매율 50%, 정산 표시, 다음 날 가격 조언 PASS.
- 증거: `Logs/Codex_Task034_DailyDecisionStats.log`.

## 확인하지 않은 것

- 전체 회귀, Windows 빌드, 3일 연속 사람 플레이, 1920x1080 정산/Day 1 결산의 주관적 가독성은 확인하지 않았다.
- 일일 통계의 저장/로드는 구현하지 않았다. 저장 v10 후보 Task 055의 승인 범위로 남긴다.

## 다음 체크포인트

- 새 게임 Day 1 종료부터 Day 3 정산까지 실제 입력 경로를 감사하고 가장 큰 단절 1개를 다음 단일 작업으로 선정한다.
- 밤 손님 시간 창 조정과 Task 055 저장 확장은 각각 사람 결정/승인 전까지 보류한다.

---

# Session 2026-07-15 — Task 068 Partial: Day 1→3 Player Transition

## 감사 결과와 구현

- 기존 장기 검증은 `GameClock.ForceSet`으로 Day 2~7을 구성해 실제 플레이어 날짜 전환을 증명하지 않았다.
- 기본 60초/게임시간 기준 Day 1 결산 후 최대 약 17분을 기다려야 했고, Day 2+ 정산에는 다음 날 시작 입력이 없었다.
- `GameClock.AdvanceToNextDayMorning`을 추가해 `OnNewDay`와 결과 아침 시각 이벤트를 정상 발화한다.
- Day 1 결산 버튼은 Day 2 아침을 시작하고, 이후 보이는 목표/체크리스트를 반복 운영 안내로 바꾼다.
- Day 2+ 정산에서는 기존 `ShopOpenSign`이 영업 시작 간판에서 하루 마감 간판으로 역할을 전환한다.

## 검증

- 런타임/에디터 dotnet build 오류 0.
- D3D11 FinalDemoRoute PASS: 첫 판매 30G 유지, Day 1 결산 버튼→Day 2 준비 페이즈·목표 갱신.
- D3D11 DayNightShopLoop PASS: Day 2 정산 간판 프롬프트/Interact→Day 3 06:00·준비 페이즈·채집 재활성.
- 로그: `Logs/Codex_Task068_FinalRoute_Day2Transition.log`, `Logs/Codex_Task068_DayNight_Day3Transition.log`.
- 첫 Unity 호출은 잘못 포함한 `-quit` 때문에 Play Mode 진입 직후 종료됐으며 검증 결과로 계산하지 않았다. 같은 코드를 정상 인자로 재실행해 PASS했다.

## 확인하지 않은 것

- 새 게임부터 Day 3 정산까지 사람 연속 플레이, 새 날짜 전환 후 저장 종료/재실행, Windows 빌드, 1920x1080 가독성은 확인하지 않았다.
- Task 041 낚시→진열→판매 왕복과 밤 손님 시간 창은 이번 단일 연결 범위 밖이다.

## 다음 체크포인트

- Task 039 실제 낚시 상호작용을 해변의 기존 런타임 채집 지점과 `IInteractable` 패턴 위에 구현한다.
- 이어서 Task 041에서 낚시 결과의 진열·가격·NPC 구매 왕복을 한 경로로 증명한다.

---

# Session 2026-07-15 — Task 039 Real Fishing Interaction

## 플레이어 경험 변화

- 해변 표식에 다가가 `[Space]`를 누르면 즉시 물고기를 줍는 대신 낚싯대를 드리우고 찌의 입질을 기다린다.
- 성공하면 Fish 2개가 기존 인벤토리에 들어가며, 그날은 낚시 완료 상태가 표시되고 다음 날 다시 이용할 수 있다.
- 해변 지점은 일반 채집 상자가 아니라 물빛 원형 표식·낚싯대·찌·바구니로 읽힌다.

## 구현·검증

- `FishingSpot`은 기존 `DaytimeStockPrepPoint`의 일일 상태와 `TryCollectDayPrepStock` 인벤토리 경로를 재사용한다. 씬·저장·입력 코어는 변경하지 않았다.
- 첫 D3D11 검증에서 Unity 특수 null과 `??` 조합으로 자식 컴포넌트 생성이 실패해 BUG_LOG에 기록하고 중단했다. 다음 연속 작업에서 명시적 null 검사로 해결했다.
- `dotnet build Assembly-CSharp.csproj`: 오류 0, 기존 `CS8785` 경고 1.
- D3D11 GatheringShopGate PASS: 낚시 프롬프트/대기 상태→Fish 2개→동일 일차 차단→다음 날 재활성→진열·가격→저장 복원.
- D3D11 FinalDemoRoute PASS: BreadLoaf 판매 30G, Day 1 결산→Day 2 준비 유지.
- 증거: `Logs/Codex_Task039_Fishing.log`, `Logs/Codex_Task039_FinalRouteRegression.log`.

## 확인하지 않은 것

- 사람이 실제 1.25초 대기와 입질/성공 프롬프트의 코지한 체감을 확인하지 않았다(Task 040 잔여).
- 낚은 Fish를 같은 검증 경로에서 NPC가 구매하고 수익이 증가하는 마지막 구간은 Task 041로 남는다.
- Windows 빌드와 새 게임→Day 3 사람 연속 플레이는 확인하지 않았다.

## 다음 체크포인트

- Task 041에서 현재 낚시 검증 경로를 NPC 구매·수익 증가까지 연장한다.

---

# Session 2026-07-15 — Task 041 Fishing-to-Sale Round Trip

## 연결한 전체 경로

- Day 2 해변에서 실제로 Fish 2개를 낚는다.
- 합성 아이템을 넣지 않고 그 인벤토리에서 Fish 1개를 판매대에 진열한다.
- 기본가 18G를 가격 UI에서 확정하고 밤 영업 간판을 연다.
- 실제 씬 NPC 이름 `Fisher_01`로 구매 진입점을 호출해 같은 Fish가 판매된다.
- 잔액·누적매출·Raw 판매 기록·일일 구매 통계·MoneyHUD가 한 거래에서 갱신된다.

## 검증

- 첫 에디터 빌드에서 비공개 `NpcController.DisplayName` 접근 오류 1건을 발견했다. 공개 `NpcProfile.npcName`/GameObject 이름 경로로 수정 후 재컴파일 PASS.
- 런타임 dotnet: 경고 0, 오류 0. 에디터 dotnet: 오류 0, 기존 CS8785/CS0414 경고 2.
- D3D11 GatheringShopGate: Fish 2→1 진열, Fisher_01 구매 18G, 돈 500→518G, 누적매출/SalesLog/구매 통계 PASS.
- D3D11 FinalDemoRoute: BreadLoaf 30G 판매와 Day 2 전환 PASS.
- 증거: `Logs/Codex_Task041_FishingSaleRoundTrip.log`, `Logs/Codex_Task041_FinalRouteRegression.log`.

## 확인하지 않은 것

- 실제 NPC가 걸어와 Fish를 평가하는 시간감과 구매/거절 체감은 사람이 Play Mode에서 확인하지 않았다. 검증은 `NpcController`가 사용하는 동일 거래 진입점을 밤 영업 게이트 뒤에서 호출했다.
- 새 게임→Day 3 정산 연속 플레이, 저장 종료/재실행, Windows 빌드는 확인하지 않았다.

## 다음 체크포인트

- Task 042에서 이 경로를 스모크 체크리스트에 고정한 뒤 낚시 슬라이스를 닫는다.

---

# Session 2026-07-15 — Task 042 Fishing Smoke Checklist

## 산출물

- 신규 `AI_WORKFLOW/04_VERIFICATION/SMOKE_CHECKLIST.md`에 자동 낚시→판매 스모크, Day 1 회귀, 사람 Play Mode 낚시 스모크를 분리했다.
- 자동 체크는 `Codex_Task041_FishingSaleRoundTrip.log`와 `Codex_Task041_FinalRouteRegression.log`의 실제 PASS만 표시했다.
- 기존 IslandLife 문서를 즉시 해변 채집 큐브 설명에서 `FishingSpot` 캐스팅·18G 판매 왕복으로 동기화했다.

## 확인하지 않은 것

- 사람 입력으로 해변까지 이동, 1.25초 대기 문구, 실제 NPC 접근·평가, 다음 날 재낚시는 확인하지 않았고 체크리스트에서 미체크다.
- 이번 문서 작업에서는 Unity를 재실행하지 않았다. 사용한 자동 증거는 직전 Task 041에서 같은 현재 코드로 실행한 D3D11 PASS다.

## 다음 체크포인트

- Task 043에서 기존 Crop/Farmland와 아이템 데이터를 조사해 다음 낮 활동 구현 경계를 확정한다.

---

# Session 2026-07-15 — Task 043 Mining/Farming Design Boundary

## 결정

- 낚시 다음 낮 활동은 광질을 먼저 구현한다.
- 다음 단일 왕복은 `quarry-mining` 상호작용으로 Ore 2개를 얻고, 실제 획득 Ore 1개를 밤에 15G로 판매하는 경로다.
- 광질은 기존 `DaytimeStockPrepPoint`의 일일 완료/다음 날 리셋/v9 저장 상태를 재사용하므로 저장 스키마를 바꾸지 않는다.

## 농사 감사 결과

- 기존 `Crop`/`Farmland`는 보존·재사용한다.
- 현재는 씨앗의 cropPrefab null, Seed 입력 분기와 Harvest 호출자 부재, 테스트용 3초 성장, 작물 저장 부재, 프리팹 Item GUID 단절 때문에 플레이 가능한 농사 루프가 아니다.
- 농사는 데이터 참조 복구 → `IInteractable` 고정 밭 어댑터 → 날짜 성장/승인된 저장 → Wheat→Bread→판매 왕복 순으로 진행한다.

## 검증과 미확인

- 실제 스크립트, Item/Recipe 에셋, 프리팹 YAML과 GUID 존재 여부를 정적으로 대조했다.
- 문서 전용 작업이므로 컴파일·Unity Play Mode·D3D11 검증기는 실행하지 않았다.
- 광질 상호작용, 15G 판매, 일일 리셋/저장 왕복은 다음 구현 작업에서 실제 검증해야 한다.

## 다음 체크포인트

- 신규 `MiningSpot`과 런타임 광산 지점을 기존 일일 활동 상태에 붙이고, 합성 Ore 주입 없이 밤 판매까지 전용 검증한다.

---

# Session 2026-07-15 — Quarry Mining Sale Round Trip

## 구현

- `MiningSpot`이 플레이어 상호작용, 곡괭이 타격 대기, Ore 지급 피드백을 담당한다.
- `quarry-mining` 지점은 기존 일일 활동/v9 저장 상태를 재사용하며 씬이나 저장 스키마를 수정하지 않는다.
- 검증기는 합성 Ore 주입 없이 채굴→격리 저장/로드→1개 진열→15G 가격 확정→Miner 구매→Day 3 리셋을 수행한다.

## 검증

- `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`: 순차 빌드 경고 0, 오류 0.
- D3D11 MiningShopLoop: Ore 2→1 진열, 15G 판매, 500→515G, Raw 기록/일일 구매 통계, v9 복원, Day 3 재활성 PASS.
- D3D11 FinalDemoRoute: BreadLoaf 30G 기존 루프 PASS.
- 증거: `Logs/Codex_MiningShopLoop_Final.log`, `Logs/Codex_Mining_FinalRouteRegression.log`.

## 미확인·다음

- 사람 입력으로 광산까지 이동하는 동선, 타격 시간감, 실제 NPC 접근 장면, Windows 빌드는 확인하지 않았다.
- 바위·광맥·곡괭이는 기능 위치를 알리는 임시 primitive다. 다음 단일 작업에서 비주얼 도구체인·라이선스·아트 방향을 감사하고 실제 제작 도구로 교체를 시작한다.

---

# Session 2026-07-15 — Visual Toolchain Audit and B01 Market Stall

## 감사·설치

- Unity 6000.3.2f1, URP/Core/Shader Graph 17.3.0, AI Navigation 2.0.12, Timeline 1.8.9와 전체 manifest/lock을 확인했다. Cinemachine, Animation Rigging, Blender는 미설치다.
- 체크포인트 `64860ff` 뒤 Unity MCP 9.7.0을 안정 태그와 정확한 커밋에 고정했다. uv 0.11.28 서버를 `127.0.0.1`에서만 실행하고 프로젝트 `.codex/config.toml`로 연결했다.
- Editor/Runtime 컴파일, WebSocket, Project_PA 인스턴스, 30개 Unity 도구 등록을 확인했다. MCP로 활성 씬, 계층, B01 프리팹, Play Mode와 Game View를 직접 검사했다.

## 실제 비주얼 개선

- 변경 전 중앙 상점은 B01 에셋 대신 큰 갈색 원시 박스와 안내용 오브젝트가 시각적 초점을 차지했다.
- `DemoVisualDressingController`가 `Building_B01_MarketStall`의 `Visual`만 기존 4개 ShopSlot에 정렬한다. 슬롯의 상품 표시 자식은 유지하고 원시 루트 Renderer만 숨긴다.
- 겹치던 `Support_Crate`, `Shop_Tent_Kit`, `Sales_Tent_Preview`의 Renderer/Collider는 런타임에서만 끈다. 메인 씬·B01 프리팹·Shop·경제·저장 코어는 변경하지 않았다.
- Tripo 추정 플레이어/주민은 외형 보존 우선으로 분류했고, B09 창고와 B05 작업대는 실제 기능에 따른 재구성 후보로 남겼다.

## 증거·남은 확인

- 변경 전 `Logs/DemoViewShots/shot_20260715_164534.png`, B01 원본 `Logs/VisualAudit/B01_MarketStall_MCP.png`, 1차 Play 확인 `Logs/VisualAudit/after_b01_mcp.png`.
- 동일 구도 최종 `Logs/DemoViewShots/shot_20260715_171613.png`: 큰 갈색 큐브와 검은 아티팩트가 없어지고 B01 차양·목재 프레임·상품 접근면이 보인다.
- dotnet 런타임 경고 1/오류 0, Editor 경고 2/오류 0(기존 기준 경고), Unity Console 오류 0. D3D11 FinalDemoRoute가 `stocked=BreadLoaf, paid=30G`로 PASS했다.
- Unity MCP의 장문 `execute_code` Windows 길이 실패는 `BUG_LOG.md`에 기록하고 구조화 도구로 우회했다.
- 다음 단일 작업은 기존 그리드/건설/저장 구조를 감사한 뒤 상점 실내 배치 MVP를 연결하는 것이다. 저장 스키마 변경은 별도 승인 조건을 유지한다.

---

# Session 2026-07-16 — Shop Interior Grid Customization P1/P2

## 구현

- 기존 `GridService`를 유일한 권한자로 유지하면서 zone/owner/footprint/clearance/protected cell/BFS 통로 API를 추가했다.
- `shop.interior`는 2m 5×4 셀이며 문 앞 `(2,0)`과 안쪽 `(2,3)` 연결을 보호한다.
- 상점 배치 장부와 UI에서 실제 프리팹 preview, 90도 회전, 플레이어 전방 셀 배치, 기존 가구 이동·회수, 첫 B05 청사진 지급을 제공한다.
- 기존 ShopSlot을 그대로 이동해 NPC 목적지, 가격, 상품, Shop 등록과 hierarchy 저장 키를 보존한다. 기존 슬롯에 carving obstacle을 보강했다.
- 실제 B05 Workbench를 2×2로 연결했다. B06~B08은 실제 청사진에서 정의되며 B09는 공간·역할 재설계 전 실내 금지다.
- `SaveData`/`SaveManager`를 v10으로 확장해 zone/definition/instance/cell/rotation/recovered/function/storage를 저장·복원한다.

## 검증과 시각 확인

- Unity 6000.3.2f1 D3D11 컴파일 성공. 새 Runtime/Editor 어셈블리 오류 0.
- 보호 셀·겹침 거부, 선반 2개 회수, B05 2×2 배치, 선반 `(4,0)`/270° 이동, 이동 후 NPC 판매 61G PASS.
- 격리 v10 JSON 저장 뒤 Workbench, 이동 선반, 상품 2개, 표시 가격 73G와 보호 통로 복원 PASS.
- Workbench 기능/차폐 obstacle/회수 PASS.
- 기존 SaveRoundTrip을 v10 기대값으로 갱신해 경제·인벤토리·ShopSlot·마을 변화 복원 PASS. 기존 FinalDemoRoute도 BreadLoaf 30G 판매 PASS.
- 첫 batch 캡처 파일 미생성을 `BUG_LOG`에 기록하고 RenderTexture 동기 PNG로 해결했다.
- 최종 이미지 `Logs/ShopCustomization/20260716_103307/shop_customization_game_camera.png`를 직접 확인해 앞벽 가림을 줄이고 UI 상태 문구 여백을 보정했다.
- 회귀 로그: `Logs/ShopCustomization_SaveRoundTripRegression.log`, `Logs/ShopCustomization_FinalRouteRegression.log`.

## 문서와 다음 작업

- 작성: `PLACEMENT_SYSTEM_ARCHITECTURE.md`, `CUSTOMIZATION_ROADMAP.md`, `PLACEABLE_ASSET_GUIDE.md`.
- 갱신: 저장 v10 `SAVE_SCHEMA.md`와 종료 기록 6종.
- 미확인: 사람 손으로 장부까지 걸어가 장시간 배치하는 체감, Windows 빌드, 명시적 NPC approach 셀.
- 다음 단일 구현: P3 `village.outdoor` zone과 도로/건물 입구 보호. B09 창고는 현재 임시 외형을 확정하지 않고 기능 역할부터 재설계한다.

---

## 2026-07-16 Continuation — B02~B04 Shop Evolution Visual Finalization

- Audited B02~B04 source FBX files, wrapper prefabs, four faces, mesh bounds/polycounts, colliders, and embedded Shop/ShopSlot counts through Unity Editor API. All three sources are grounded and intact; their actual facades face local `-X`.
- Classified all three as category 2, Unity configuration only. B02 reads as a small wood general store, B03 as a mint-awning market, and B04 as a mansard-roof cozy boutique; no mesh repair, Blender work, material replacement, or identity replacement was justified.
- Found that spawning the existing wrappers as evolution stages would duplicate one Shop plus 8/16/32 ShopSlots over the already functional interior. Generated three visual-only Resource prefabs that reference the existing Visual, add exact BoxCollider/NavMeshObstacle bounds, and provide Entrance/Sign anchors without economy objects.
- Extended the existing `ShopEvolutionController` instead of creating a parallel evolution system. The current saved Tier derives Tier 0=B10 Cottage, Tier 1=B02, Tier 2=B03, and Tier 3+=B04; no new save field was added.
- Existing `PA_StoreDoor_Out`, outside player spawn, BuildingEntrance, and the B10 3D sign now follow the active modeled doorway. The old B10 render/collision shell is hidden only while an upgraded stage is active, and exactly one evolution visual remains active.
- Captured actual Play Mode Tier 1/2/3 views from the same game camera. Direct review caught the first B02 entrance anchor on the wrong horizontal side; it was corrected from negative to positive local Z and all stage captures/assertions were rerun.
- Final D3D11 validation passed for resource loading, single active visual, no duplicate Shop/slots, grounding, collider fit, entrance/sign alignment, and exact preservation of the existing placement snapshot across Tier transitions.
- EnterableShop regression passed Tier 0 lock→Tier 1 B02→interior entry→six-slot stock→price UI→exit. FinalDemoRoute retained the BreadLoaf 30G sale.
- Two audit-tool failures were recorded and resolved: a missing `UnityEngine.AI` import and SessionState tier reconstruction after domain reload. Neither failure was repeated.
- No source FBX, texture, wrapper prefab, BuildingData, TierDefinition, main scene, save schema, package, commit, or push operation was performed.

Next: P5 shop-evolution placement unlock—expand zone size and catalog by Tier while preserving every existing placement record.

---

## 2026-07-17 Continuation — P5 Shop Progression Unlock (Stopped on First Runtime Failure)

- Implemented a monotonic shop interior expansion over the existing `shop.interior` grid: Tier 0/1 `5x4`, Tier 2 `6x5`, Tier 3+ `7x6`. The original origin and every existing placement identity/cell/rotation contract remain unchanged.
- Reused authored TierDefinition capacity values with a six-display floor, producing limits 6/8/12/20. A hidden clone of the authored shelf provides new functional ShopSlot displays without introducing a new primitive visual or duplicate Shop system.
- Connected B05/B06/B07+B08 to Tier 1/2/3 ledger rewards. Added physical floor/wall/light/ledger expansion, an expansion-only runtime NavMesh surface, and a Processed-culture warm theme persisted through a special v10 placeable record.
- Extended the existing evolution banner with player-readable zone, display capacity, and preparation-bench unlocks. No main scene, source asset, wrapper prefab, TierDefinition, BuildingData, SaveData, SaveManager, package, commit, or push operation was performed.
- Both local Runtime and Editor assemblies compile with zero errors. The first D3D11 play validation stopped during initialization because `GetComponent<Light>() ?? AddComponent<Light>()` retained Unity's fake-null Light wrapper and threw `MissingComponentException` at `light.type`.
- Recorded the failure as OPEN in `BUG_LOG.md`; no identical rerun was attempted. P5 is not complete, no generated visual checkpoint is accepted, and regressions were not run.

Next: replace the null-coalescing Light lookup with an explicit Unity null check, then run the full P5 validator from Tier 0 through Tier 4.

---

## 2026-07-17 Continuation — P5 Shop Progression Unlock Complete

- Replaced the Unity fake-null `Light` lookup with an explicit Unity null check and rebuilt both Runtime and Editor assemblies with zero errors.
- Finalized monotonic shop growth: Tier 0/1 `5x4`, Tier 2 `6x5`, Tier 3+ `7x6`; physical display limits are `6/6/8/12/20` and existing placement IDs/cells/rotations stay intact.
- Connected the expansion NavMesh surface to the authored interior through east/north bidirectional links and verified a complete path to the far expansion cell.
- Kept the authored six shelves, moved the hidden clone template outside the Shop hierarchy, and verified only placed clones register as functional ShopSlots.
- Verified B05/B06/B07+B08 Tier rewards, the Processed warm-workshop theme, and v10 clear/restore of both theme and dynamic shelf records.
- Directly reviewed the same-camera Tier 0 and Tier 3 captures. Corrected the ledger status/theme-button overlap and accepted the final capture at `Logs/ShopProgressionUnlock/20260717_005831/`.
- D3D11 PASS: `P5_ShopProgression_D3D11_Release.log`, ShopCustomization, EnterableShop, SaveRoundTrip, and FinalDemoRoute regressions. No main scene, source asset, prefab, TierDefinition, BuildingData, save core, package, commit, or push change was made.

Next: Task 044 resident-request display design, grounded in the existing Dialogue/Demand/day-activity/village-change data.

---

## 2026-07-17 Continuation — Task 044 Resident Request Display Design Complete

- Audited the live dialogue path, all eight role dialogue assets, customer-demand insight, resident profiles, specialist recipes, inventory removal, friendship rewards, presentation filtering, and daily activity persistence.
- Confirmed that no personal item-request state exists today: dialogue assets contain Greeting only, demand insight is category-level runtime data, and resident profiles have no request item fields.
- Designed the first request as Chef_01 asking for Wheat x3, derived directly from the existing `Recipe_Bread` ingredient. Blacksmith Ore x2 and Carpenter Wood x2 are the next compatible cases.
- Kept the design outside a generic quest engine: reuse `NpcDialogue`/`DialogueUI`, the existing `Economy` topic, `assignedRecipes`, `Inventory`, `FriendshipService`, and namespaced daily activity IDs.
- Found an existing data mismatch: `PA_SceneAutoBuilder` assigns Bread to Tailor instead of the available Clothes recipe. Tailor is explicitly excluded until a separate data correction is approved and verified.
- This task was documentation-only by queue contract. No code, scene, prefab, asset, package, save schema, commit, or push action was performed. Static compatibility review and `git diff --check` passed; Unity/Play Mode was not applicable.

Next: Task 045 documentation sync for daytime activity results flowing into stock. Resident-request code requires a separate single-task authorization because Task 044 explicitly forbids code and scene changes.

---

## 2026-07-17 Continuation — Task 045 Daytime Activity To Stock Sync Complete

- Created `AI_WORKFLOW/03_TASKS/DAYTIME_ACTIVITIES.md` from current source and existing D3D11 evidence rather than old task-state assumptions.
- Documented six daily stock-source owners: two onboarding/NPC-support points plus forest Carrot, shore Fishing/Fish, meadow Wheat, and quarry Mining/Ore activities.
- Reconfirmed the complete Fish loop from the existing log: Fish x2, stock one, confirm 18G, Fisher_01 purchase, money 500→518G, cumulative revenue, Raw SalesLog, daily stats, and HUD update.
- Reconfirmed the complete Ore loop: Ore x2, isolated save/load, stock one, confirm 15G, Miner_01 purchase, money 500→515G, revenue/log/stats, and next-day quarry reset.
- Updated `PROJECT_PA_GAME_LOOP.md` so its day, preparation, and night status rows include both verified loops and explicitly retain farming and resident requests as unimplemented.
- Kept Carrot/Wheat at “inventory grant implemented, item-specific full sale round trip unverified”; no validation result was invented.
- Documentation-only scope: no code, scene, prefab, asset, package, save schema, commit, or push action. Static source/log/document correspondence passed; Unity was not rerun.

Next: Task 046 documents the real category-sales aggregation and village-trend boundary in `VILLAGE_TREND.md`.

---

## 2026-07-17 Continuation — Task 046 Category Sales Aggregation Audit Complete

- Created `AI_WORKFLOW/03_TASKS/VILLAGE_TREND.md` from the live sale, signal, presentation, culture-visual, and save sources.
- Confirmed that one `SaleRecord` represents one successful ShopSlot transaction, while its price is the full stack payment. Rejections remain separate daily-decision statistics.
- Confirmed the runtime limits and formula: SalesLog retains 100 records by default; the village signal rebuilds from the latest 40, excludes Tool, and selects by `transaction count × 1000 + revenue` without an explicit tie contract.
- Rechecked existing D3D11 evidence for Processed 2 sales/76G, Fish 18G, Ore 15G, BreadLoaf 30G, next-morning Processed activation, and Processed pending-to-active save restoration.
- Documented the persistence boundary: v10 restores the Processed visual pending/active state, but does not restore sale records, daily decision dictionaries, or category trend statistics.
- Static verification passed against seven document markers, twenty source markers, six log markers, four referenced files, and trailing whitespace. Unity was not rerun because Task 046 is documentation-only.
- No code, scene, prefab, asset, package, save-schema, commit, or push action was performed.

Next: Task 047 completes the fishing/camping/furniture trend-score data contract in `VILLAGE_TREND.md` without code or scene changes.

---

## 2026-07-17 Continuation — Task 047 Named Trend Score Data Design Complete

- Extended `VILLAGE_TREND.md` with a named-life-trend layer that preserves the existing ItemCategory signal.
- Mapped fishing to the real Fish (Raw, 18G) and Grilled Fish (Processed, 52G) data, and furniture to the real Wooden Furniture item (Luxury, 185G, Tier 2, three Planks at a BasicWorkbench).
- Confirmed that no camping Item, Recipe, activity, or Resources content exists. The legacy `Shop_Tent_Kit` is a hidden prototype shop marker and cannot produce camping trend points.
- Defined sale-only scoring as `qualified transactions × 1000 + min(revenue, 999)`. Crafting, stocking, rejection, placeable furniture, and blueprint unlocks produce no points.
- Defined a settlement-day window, a future persisted seven-day snapshot boundary, and deterministic ties by transactions, uncapped revenue, latest sale, then trendId.
- Kept the current category formula and Processed pending/active save ownership separate. Named trend code and weekly persistence remain unimplemented and require later tasks.
- Final static verification passed for ten document markers, thirty-seven data/source markers, six SaleRecord fields, nine camping asset terms, six score examples, and whitespace. Unity was not rerun because this was documentation-only.
- No code, scene, prefab, asset, package, save-schema, commit, or push action was performed.

Next: Task 051 connects the existing Village direction to a player-readable facility-unlock preview without changing TierService unlock logic.

---

## 2026-07-17 Continuation — Task 051 Facility Unlock Direction Preview Complete

- Reused the existing successful-sale category aggregation and exposed a read-only facility preview: Raw storage/collection, Processed cooking/processing, Utility repair/tools, and Luxury packaging/culture display.
- Added a compact facility-direction card to the audit app with the leading category, candidate facility, sale count/revenue signal, and an explicit statement that actual unlocks remain owned by tier/audit conditions.
- Preserved `TierService`, `AuditService`, sale math, save v10, the main scene, prefabs, packages, and every actual unlock rule.
- Runtime and Editor builds completed with zero errors; only the existing generator and unused-field warnings remain.
- D3D11 FinalDemoRoute passed after a real BreadLoaf 30G sale and asserted the Processed→cooking/processing preview plus the advisory boundary.
- FinalPresentation recorded the Processed sale before opening the audit app. Direct review of the first 1920×1080 capture found truncated copy; the text was shortened and the same view was recaptured.
- Final visual evidence: `Logs/FinalPresentation/20260717_021259/04_audit_app_goal.png`. All four decision lines are visible and the phone, HUD, and hotbar do not overlap.

Next: Task 052 designs small event-unlock candidates grounded in the real fishing loop and current village-direction signals without implementing a new event system.

---

## 2026-07-17 Continuation — Task 052 Event Candidate Design Paused During Static Verification

- Drafted `DESIGN_EVENTS.md` around `event.fishing.harvest_market`, a small seaside harvest festival that is unlocked by a completed fishing-product sale and becomes active the following morning.
- Kept the playable route grounded in the verified loop: normal one-per-day fishing, optional Grilled Fish processing, existing stocking/pricing/shop opening, successful `SaleRecord`, and settlement.
- Defined the event states, non-punitive retry, provisional one-time reputation reward, visual/navigation safety lines, additive sidecar ownership, save boundary, Task 078 approval gate, and future validation contract.
- No event, trend, dialogue, reward, scene, asset, package, or save implementation was performed.
- Static marker and source checks reached the whitespace stage, where the Markdown hard-break spaces on lines 3–4 were rejected. The failure is recorded in `BUG_LOG.md`; Task 052 remains active and its queue/matrix counts are unchanged.

Next: remove the two metadata hard-break spaces, run independent `git diff --check` and source/marker checks once, then close Task 052 if both pass.

---

## 2026-07-17 Continuation — Task 052 Event Candidate Design Complete

- Removed only the two Markdown hard-break spaces recorded by the paused verification; no gameplay content was altered.
- Finalized `DESIGN_EVENTS.md` with the seaside harvest festival as the first implementation candidate: a verified fishing-product sale schedules the next-day event, and the active day preserves normal fishing, optional processing, stocking, pricing, shop opening, successful sale, and settlement.
- Fixed the event's lifecycle, non-punitive retry, provisional one-time reputation reward, visual/navigation constraints, shared named-trend ownership, additive save candidates, Task 078 approval gate, and future runtime validation contract.
- Verification passed independently: whole-worktree `git diff --check`; 10 document contracts; 10 live source/data contracts; five Task 041 fishing-sale log markers; zero trailing-whitespace lines; zero existing event runtime classes.
- Unity was not run because Task 052 is documentation-only. No code, scene, prefab, asset, package, save schema, commit, or push action was performed.

Next: Task 054 designs the additive persistence boundary for daily/category sales and seven-day named-trend snapshots against the current v10 schema. Task 055 implementation remains approval-gated.

---

## 2026-07-17 Continuation — Task 054 Sales Persistence v11 Design Complete

- Finalized the additive v10-to-v11 persistence contract in `AI_WORKFLOW/03_TASKS/SAVE_SCHEMA.md` without changing runtime serialization.
- Separated bounded recent sale records, daily purchase/rejection decisions, daily category aggregates, and seven-day named-trend snapshots so the current feed/recent-40 behavior and longer history can both survive a future load.
- Defined DTO fields, successful-sale and rejection ownership, one-time settlement finalization, seven completed days plus the current day retention, deterministic normalization, and restore ordering after `GameClock` but before trend/event consumers.
- Defined v10 migration as empty statistics only. Existing money, cumulative revenue, ShopSlots, v9 village-culture state, and v10 placement state cannot be used to fabricate historical sales.
- Documented that the stale Task 055 SaveData/SaveManager-only scope is insufficient for private SalesLog state. Its approval request must explicitly include minimal SalesLog owner APIs, the shared named-trend owner, and affected round-trip validators.
- Replaced the failed broad word search with exact declarations and paths. Verification passed: 12/12 document contracts, 17/17 live v10 source contracts, zero schema-document trailing whitespace, and whole-worktree `git diff --check` exit 0.
- Unity was not run because this was documentation-only. No C# runtime, scene, prefab, asset, package, actual save-version, commit, or push action was performed.

Next: Task 055 requires explicit user approval for the additive v11 implementation. If approval is not available, Task 060 can document the version-management policy without changing runtime state.

---

## 2026-07-17 Continuation — Task 023 Stopped Before Implementation

- Audited the actual merchandising data: no rarity field exists, while each displayed stack preserves `ItemInstance.quality`.
- The current sellable Resources catalog forms two base-price bands, 8–52G and 150–185G. The intended minimal UI therefore labels a 100G split explicitly as price-derived and shows the real quality multiplier alongside it.
- Two patch applications failed on the same color-constant context. Per the repository stop rule, no third implementation attempt was made and the failure was recorded in `BUG_LOG.md`.
- The one partially added unused field was removed. Runtime C#, scenes, prefabs, item assets, purchase math, saves, and packages have no final changes from this attempt. Task 023 remains TODO; Unity compile and visual capture were not run.

Next: resume Task 023 with independent patches anchored to the verified `ItemNameTxt` creation and `RefreshUI` blocks, never the failed color-constant selector.

---

## 2026-07-17 Continuation — Task 023 Rarity And Quality Display Complete

- Resumed with independent patch anchors and never reused the failed color-constant selector.
- Added a compact merchandising line to the existing runtime-built `ShopPriceUI`: `Normal|Rare · price-derived value · quality ×N.NN` in the Korean player-facing copy.
- The current catalog has no rarity source field, so the rule is explicitly temporary and price-derived: below 100G is normal, 100G or above is rare. The displayed quality is the real `ItemInstance.quality`, not a fabricated tier.
- Normal goods use a soft green label; rare goods use the existing gold accent. The existing item, stock count, price controls, customer reaction, and player confirmation flow remain unchanged.
- Runtime and Editor builds completed with zero errors. FinalPresentation asserted BreadLoaf quality 1.00 and Clothes quality 1.25, captured both states, and passed. FinalDemoRoute retained the real BreadLoaf 30G sale.
- Direct same-camera review compared the earlier `20260717_021259` price panel with the final `20260717_030256` normal and rare captures. The new line is readable without clipping or overlap.
- No scene, prefab, Item asset, purchase math, save, package, commit, or push change was made.

Next: Task 025 can expose the existing price basis as a read-only recommended-price hint while preserving manual price choice.

---

## 2026-07-17 Continuation — Task 025 Read-only Recommended Price Complete

- Confirmed there is no separate `IdealSellPrice` owner. The live purchase evaluator derives its price basis from base price plus positive quality, so the UI now mirrors that exact basis as read-only information.
- Added `추천 기준가 N G · 기본가+품질` beneath the player's current price. Nothing applies the recommendation automatically; manual +/- adjustment and confirmation remain unchanged.
- BreadLoaf quality 1.00 shows current/recommended 30G. Clothes quality 1.25 deliberately keeps the current 165G while showing a 186G recommendation.
- The approximate reaction bar now compares against the same quality-adjusted basis, removing an internal presentation mismatch without changing `PurchaseEvaluator` or any NPC decision.
- Direct review of the first capture found the recommendation overlapping the adjustment buttons. Increased the runtime panel height and spacing, then recaptured the same normal and rare views. The final text, controls, HUD, and interaction prompt do not overlap.
- Runtime and Editor builds completed with zero errors. D3D11 FinalPresentation passed both recommendation assertions and the non-auto-apply assertion. FinalDemoRoute retained the BreadLoaf 30G sale.
- No scene, prefab, Item asset, purchase math, save, package, commit, or push change was made.

Next: Task 031 adds an honest resident/tourist customer-class label from existing NPC data without changing purchase behavior.

---

## 2026-07-17 Continuation — Task 031 Resident/Tourist Customer Label Complete

- Audited `NpcProfile`, the scene-authoring path, schedules, current customer controllers, the hidden preference panel, and the player-facing head bubble. There is no explicit tourist field; all eight current customers have valid village schedules and are residents.
- Added a presentation-only derivation: a valid `NpcScheduleController.scheduleData` means `[주민]`; an unscheduled future visiting customer means `[관광객]`. No current resident was fabricated as a tourist.
- The F10 preference panel now formats active customers as `name [class] · preference`. Because direct capture proved that panel is intentionally hidden in the default player view, the existing `NpcBubbleUI` now also renders a separate small class tag above its unchanged body text.
- The class is never used by `NpcController`, `PurchaseEvaluator`, scheduling, economy, or save logic. Real tourist generation and differentiated behavior remain future content.
- A first parallel dotnet validation attempt caused a shared output-DLL lock; that command shape was discarded. Sequential Runtime and Editor builds then completed with zero errors.
- D3D11 CustomerPresentation passed all eight resident labels, zero fabricated tourists, the tourist fallback, the visible head tag, and body-text preservation. CustomerPanelLayout passed tag bounds and produced `Logs/CustomerPanelReview/20260717_032821/customer_panels_1920x1080.png`, which was directly reviewed. FinalDemoRoute preserved the exact feedback body and BreadLoaf 30G sale.
- No `NpcController`, `NpcProfile`, purchase math, scene, prefab, save, package, commit, or push change was made.

Next: Task 024 is the next unapproved-safe queue item; it should define a theme-corner contract over existing ShopSlots before any implementation task is opened.

---

## 2026-07-17 Continuation — Task 024 Merchandising Theme Corner Design Complete

- Audited the real `ShopSlot`, Item categories, `shop.interior` placement/footprint APIs, v10 restore order, sales log, and village-direction path.
- Wrote `AI_WORKFLOW/03_TASKS/DESIGN_THEME_CORNER.md`. A corner is a four-neighbour connected component of at least two distinct, active, stocked shelf placements with the same sellable `ItemCategory`.
- Kept the feature separate from the existing whole-interior `shop.theme/fixed.shop.theme` preset. The recommended runtime name is `MerchandisingCorner`, not another `ShopTheme` record.
- The corner is derived live after stocking, sold-out, replenishment, move, rotation, recovery, and load. It creates no parallel inventory, save record, purchase modifier, revenue multiplier, or trend multiplier.
- The existing successful-sale path remains the only village signal: corner item purchase → one `SaleRecord` → existing category direction. Display alone never changes the village.
- Defined the narrow placement read API, runtime controller ownership, placement-ledger summary, one label per connected corner, implementation file boundary, and eleven D3D11/save/regression assertions.
- Static checks passed for required contracts, real referenced paths, prohibited boundaries, and whitespace. This documentation-only task did not run Unity and changed no code, scene, prefab, asset, package, or save schema.

Next: register and execute the Task 024 follow-up as one implementation task, reporting the exact code files before edits.

## 2026-07-17 Continuation — Task 086 Theme Corner Implementation Partial

- Registered Task 086 and implemented a read-only derived merchandising-corner controller over existing `ShopSlot`, placement IDs, and authoritative grid footprints.
- Added four-neighbour same-category component detection, placement-ledger summary, one world label per component, runtime binding, and a dedicated validator. No parallel inventory, bonus, sale, village score, save field, scene, prefab, or package change was introduced.
- Unity 6000.3.2f1 Runtime and Editor assemblies compiled successfully. The first D3D11 Play Mode run passed controller readiness, grid ownership, all six authored shelves, both item categories, and the empty-state zero-corner/zero-label assertion.
- The first 1920×1080 capture crashed inside Unity native rendering at `Camera.Render()` before a PNG was written. The run did not reach positive-corner, sold-out, move/recover, sales/village signal, isolated save/load, regression, or visual-review assertions.
- Recorded the crash in `BUG_LOG.md` and stopped without a same-source retry, as required. Task 086 remains PARTIAL and active.

Next: replace the validator's direct `Camera.Render()` with an already proven project capture path, then perform one D3D11 completion run and the three planned regressions.

## 2026-07-17 Continuation — Task 086 Function Pass, Regression Capture Blocked

- Replaced the ThemeCorner validator's explicit off-screen `Camera.Render()` with ordinary D3D11 GameView `ScreenCapture` and aligned the validation-only move helper with the real placement commit state.
- The dedicated run passed four-neighbour Raw/Processed grouping, negative adjacency cases, sold-out/replenishment, recover/move, exactly three existing sales, Processed village direction, and v10 load-derived Raw2 restoration.
- Runtime and Editor builds completed with zero errors. The successful feature log is `Logs/Codex_Task086_ThemeCorner_CaptureFinal.log`.
- Direct image review found intermittent black TMP/Canvas frames in different captures rather than a state-bound gameplay mesh. Clean same-framing evidence exists for none (`20260717_130433`), Raw2 (`20260717_130201`), and Processed4 (`20260717_130433`), but the registration modal still obscures the world label.
- The existing ShopCustomization regression then crashed Unity in its own direct `Camera.Render()` call with the same native `GfxDevice::DrawSharedGeometryJobs` stack. This is the second occurrence, so no third Unity run or remaining regressions were attempted.
- Task 086 remains PARTIAL: its feature assertions pass, while full regression coverage and default-player-view label readability remain unverified. The user's Grid-Based Customization and Tripo3D finalization directives are preserved in the existing Codex architecture/audit documents and queued for the next safe asset-audit task.

Next: obtain human direction for replacing all validator `Camera.Render()` capture paths; otherwise continue the Tripo3D audit without Unity execution, prioritizing player/NPC identity preservation and functional warehouse/workbench classification.

## 2026-07-17 Continuation — Task 087 Tripo Asset Audit Reconciled

- Counted 174 FBX, 150 OBJ, zero GLB, and zero Blend files; 24 FBX files remain outside the separately licensed Ultimate Nature Pack.
- Read the Unity 6 binary main scene through a non-mutating ASCII index and confirmed live player/resident, B05–B08 workbench, and B09–B12 object names. GUID, Resources, prefab, and code references were cross-checked without editing the scene.
- Classified B06 Kitchen, B07 Forge, and B08 Sewing as existing functional workbenches rather than decorative placeholders. Their authoritative player-facing contracts are 2×2/Tier 2, 3×2/Tier 3, and 2×2/Tier 3 with a full front clearance/interaction row and existing recipe/specialist links.
- Kept Blender/model edits conditional because the importers and wrappers show no current mesh-damage evidence. Functional integration is complete; same-camera facing, collider, aisle, and success-feedback review remains pending.
- Classified B11 Fountain and B12 TradePort as static world dressing in the current runtime. Their BuildingData/blueprints do not make them active placeables or interactables; only physical-route finalization is currently authorized.
- Verified the bundled Quaternius Nature Pack license as CC0 1.0. Individual Tripo generation/commercial-use records and the runtime Froggy Chair license remain release gates.
- Updated `TRIPO_ASSET_AUDIT.md` and `ASSET_AND_TOOL_PROVENANCE.md`. Static census, placement/recipe, binary-scene, and license marker checks passed. Unity was not launched because the two-occurrence direct-render crash boundary remains in force.
- No code, scene, prefab, model, texture, package, save schema, commit, or push operation was performed. Task 087 is DONE; Task 086 remains PARTIAL.

Next: approve a safe shared validator capture path, then finalize B06 through an actual game-camera before/after pass before moving to B07 and B08.

## 2026-07-17 Continuation — Task 088 Resident Material Request Implemented / Project Partial

- Registered one implementation task over the existing Task 044 design without adding a quest engine or save field.
- `NpcDialogue` now derives one day request from the first unlocked assigned recipe whose workbench exactly matches the resident specialty. This yields Chef Wheat 3, Blacksmith Ore 2, and Carpenter Wood 2 while excluding the temporary Tailor→Bread mismatch.
- The existing interaction prompt and dialogue UI expose unseen, insufficient, ready-to-deliver, and completed-today states. Insufficient dialogue preserves the existing daily dialogue-points path.
- `Inventory.CountItems` provides a null-safe combined inventory/hotbar count. Delivery rechecks the exact count, removes it once, records the deterministic request ID in the existing day-prep activity list, and grants friendship only.
- Added Economy-topic request tone lines to Chef, Blacksmith, and Carpenter data. No money, item reward, sale, demand, village signal, scene, prefab, package, or save-schema change was made.
- Sequential Runtime and Editor builds completed with zero errors. Static contracts passed for recipes, assignments, workbench mismatch exclusion, daily APIs, save-list reuse, exact count, dialogue pools, and economy isolation.
- Unity was not launched because a third run through the twice-crashed direct-render validation boundary is prohibited. Live insufficient/delivery/repeat/save-load/next-day/night-block and camera/UI evidence remain unverified, so Task 088 is PARTIAL.

Next: after approving a safe shared validator capture path, run the Task 088 live route and its save/day/night checks before marking it DONE.

## 2026-07-17 Continuation — Task 089 Fixed Wheat Plots F1/F2 Implemented / Project Partial

- Repaired the actual data chain from `Item_15_Seed.cropPrefab` to `Crop_Corn` and from its harvest item to `Item_Wheat`, with a harvest count of three.
- Added `FarmPlotInteraction : IInteractable` without changing `PlayerInteraction`. It ensures two fixed plots near `WorkSpot_Farmer` and exposes empty, growing, harvest-ready, inventory-full, and night-blocked states through the existing prompt/dialogue surfaces.
- Added a once-per-day seed pouch using the existing day-prep activity path, making two seeds reachable in a fresh route.
- Planting removes one seed only after `Farmland.Plant` and crop configuration succeed. Harvesting checks full inventory capacity before adding Wheat 3, so failure preserves the mature crop.
- Reused the existing real Wheat resource for three visual stages and replaced the visible primitive crop stages at runtime. The plot surface is a purposeful tilled-ridge mesh rather than an undecorated cube.
- Runtime and Editor builds completed with zero errors after including the new script in the locally generated project file. Twelve static contracts passed.
- Unity live interaction and camera evidence remain unverified under the two-occurrence native-render crash boundary. Date-based growth and plot persistence remain the separately approved F3 scope, so Task 089 is PARTIAL.

Next: once the shared safe capture path is approved, validate Task 088 and Task 089 live routes; then design/approve F3 plot persistence before replacing the temporary elapsed-seconds growth rule.

## 2026-07-17 Continuation — Task 090 Day 2+ Operations Checklist Implemented / Project Partial

- Replaced the static Day 2+ repeat-operations text in `PlayableDayScenarioController` with a read-only live checklist.
- The panel refreshes every 0.5 seconds from existing fishing, mining, farming/prep, forage, and resident-request completion signals.
- Prepared product variety is counted across inventory, hotbar, and stocked displays while excluding tools and tier-locked items. Display/price, actual shop-open gate, same-day purchases, and settlement are also read from their existing owners.
- The top objective now follows `DayPreparation`, pre-open night, active shop operation, and `Settlement` without adding a quest engine, reward, or persistence field.
- Runtime and Editor builds completed with zero errors. Ten static contracts passed, including economy/save isolation.
- Unity was not launched under the two-occurrence native-render crash boundary. Live Day 2 transitions and 1920×1080 readability remain unverified, so Task 090 is PARTIAL.

Next: after approving a safe shared capture path, validate Tasks 088–090 in one Day 2 daytime-to-settlement pass; keep F3 persistence separate until explicitly approved.

## 2026-07-17 Continuation — Task 091 Craft Output Capacity Guard Implemented / Project Partial

- Audited all eight current `RecipeData` assets and the shared `CraftingService` → `Inventory.AddInstance` path.
- Fixed the common failure where ingredients were removed before a full inventory rejected the crafted result.
- The service now creates the exact result metadata first, simulates ingredient consumption in the same hotbar-then-inventory order, and accepts only an exact `CanStackWith` destination or a slot that is/will become empty.
- A blocked result returns before ingredient removal. Successful crafting preserves the existing ingredient-quality calculation, removal, metadata result, and workbench feedback.
- Runtime and Editor builds completed with zero errors. All eight recipes and eleven static transaction/isolation contracts passed.
- Unity was not launched under the two-occurrence native-render crash boundary. Full-bag, matching-stack, and ingredient-freed-slot live branches remain unverified, so Task 091 is PARTIAL.

Next: include Task 091's three inventory branches in the same approved safe Day 2 validation pass, then continue with the existing-system furniture secondary-loop design rather than adding a parallel crafting system.

## 2026-07-17 Continuation — Task 092 Raw Next-Day Village Change Implemented / Project Partial

- Reused the existing successful-sale observer and v10 pending/active category strings to make `Raw` the second implemented next-day village visual category.
- `SalesLogManager.GetRecent` is newest-first; when several tracked sales arrive between refreshes, the newest real Raw/Processed sale becomes the single representative next-day change.
- The current sale day remains unchanged. A later `DayPreparation` activates only `PA_VillageCulture_Raw` and hides the Processed root; restore follows the same category without adding a save field.
- The Raw supply point creates no new primitive cube. It instantiates the existing Quaternius CC0 WoodLog/Rock resources and the Project-P.A.-owned B10 sign mesh, adds a Korean `원자재 수거처` label, and strips copied colliders/functional behaviours.
- Runtime and Editor sequential builds completed with zero errors. Fourteen static contracts passed for resource provenance, latest-sale selection, one hint per newly pending change, phase timing, exclusive roots, collision isolation, readable copy, and existing save-field reuse.
- One initial parallel build failed only because both builds locked the same output DLL; the competing invocation was discarded and the sequential commands passed.
- Unity was not launched under the two-occurrence native-render crash boundary. GameCamera before/after, sign facing/scale, overlap, and NPC/player route readability remain unverified, so Task 092 is PARTIAL.
- No shop/economy/NPC/save-schema, scene, prefab source, Tripo source, package, commit, or push change was made.

Next: after approving a safe common capture path, include Task 092's Raw sale-day/next-day/save-restore camera check in the same bounded Day 2 validation pass.

## 2026-07-17 Continuation — Task 093 Named Fishing/Furniture Trends Implemented / Project Partial

- Extended the existing read-only `VillageChangeSignalController`; no parallel trend service or reward system was added.
- Only current-day successful sales matching `(Raw, Fish)`, `(Processed, 생선구이)`, or `(Luxury, 목제 가구)` contribute to named trends. Camping remains inactive and similar names fail closed.
- Implemented `transactions * 1000 + min(revenue, 999)` and deterministic transaction, revenue, recency, then trend-id selection. One 18G Fish transaction scores 1018; two score 2036.
- Preserved the legacy category summary and facility preview. The settlement-facing summary now adds a second `생활 트렌드` line for fishing life or furniture culture.
- Runtime and Editor builds completed with zero errors. Eighteen static contracts passed. A dirty-worktree baseline check that mistook pre-existing save diffs for Task 093 edits was logged and replaced with an ownership-limited check.
- Unity was not launched under the two-occurrence native-render crash boundary. Settlement wrapping/readability and actual grilled-fish/furniture craft-to-sale round trips remain unverified, so Task 093 and the integrated Task 069 route are PARTIAL.

Next: once the common safe capture path is approved, validate Fish sale → settlement named trend → next-day Raw visual in one bounded route, then validate the existing furniture recipe through workbench, stocking, purchase, and furniture-trend feedback.

## 2026-07-17 Continuation — Task 094 Tripo Asset Policy and Unverified Prop Exposure / Project Partial

- Integrated the user's long-term Tripo directive into the existing audit and placeable architecture instead of creating a parallel asset system.
- The persistent policy now requires per-asset 1–8 classification, player/resident identity preservation, function-first storage/workbench decisions, Unity-before-Blender correction, source/derivative separation, placeable footprint/clearance/interaction, provenance, and same-camera review.
- Removed both runtime `Prop_FroggyChair` spawns from the shop interior and B11 plaza because no license document exists in the repository. Preserved the source FBX, wrapper/Resource prefabs, B01 stall, shop systems, licensed CC0 vegetation, and plaza benches.
- Runtime and Editor builds completed with zero errors. Static contracts confirmed zero runtime references, three preserved source/resource assets, and preserved B01/vegetation/bench paths.
- Unity was not launched under the two-occurrence native-render crash boundary. Visual absence/empty-space composition is unverified, and the Resource asset still requires provenance or build-time quarantine, so Task 094 is PARTIAL.

Next: return to the audited furniture secondary loop. Resolve its Tier 1 B05/Tier 2 furniture/10,000G–100,000G reachability mismatch through one explicit existing-system implementation without silently changing progression balance.

## 2026-07-17 Continuation — Task 095 Furniture Secondary-Loop Guidance Implemented / Project Partial

- Reused the existing processing advisor as the sole read-only owner of furniture-loop guidance; no quest, reward, crafting, or progression service was added.
- The Day 4+ player checklist now exposes one current step across Tier 1 revenue, B05 installation, three Planks, Tier 2 revenue, furniture crafting, stocking, same-day sale, and settlement furniture-culture feedback.
- Tier definitions, the 10,000G/100,000G thresholds, recipe/item assets, shop/economy/purchase systems, and save schema remain unchanged.
- Runtime and Editor builds completed with zero errors. Static contracts passed for the exact furniture recipe, Tier/Workbench/ingredient/stock/same-day-sale reads, UI bridge, and absence of gameplay mutators.
- Unity was not launched under the two-occurrence native-render crash boundary. Tier-state text, 1920×1080 clipping, and the complete craft-to-settlement route remain unverified, so Task 095 and Task 070 are PARTIAL.

Next: continue implementation outside the blocked Unity-visual batch. Do not change the 100,000G progression gate without explicit balance approval.

## 2026-07-17 Continuation — Task 096 New Game / Continue Entry Implemented / Project Partial

- Reused the existing first-day Canvas instead of adding a parallel menu scene or manager.
- Added a PROJECT P.A. title step with the core day-village/night-shop fantasy, New Game, and save-aware Continue.
- `SaveManager.HasSaveAsync` only delegates to the existing repository `ExistsAsync(SaveKey)`. Continue uses the existing v10 `LoadGameAsync` and `RestoreSavedSession`; New Game proceeds to the existing name registration without deleting the old save.
- A missing save disables Continue, and a failed load returns control to the title.
- Runtime and Editor builds completed with zero errors. Fifteen state-transition contracts and an exact mutator-absence check passed.
- Unity was not launched under the two-occurrence native-render crash boundary. The no-save/save-present screens and actual v10 continue round trip remain unverified, so Task 096 is PARTIAL.

Next: continue from the current completion map with another approval-free player-facing gap while keeping the safe Unity capture batch pending.

## 2026-07-17 Continuation — Task 097 Product Pause Menu Implemented / Project Partial

- Expanded the existing pause overlay into Resume, Save Game, Load Save, and Save & Quit controls; no parallel save or menu manager was added.
- Pause now captures the prior time scale and cursor lock/visibility, exposes the mouse for UI use, and restores the exact prior state on resume or destruction.
- Load is gated by the existing save-presence API. Save/load continue through the authoritative `SaveManager`; busy input is locked and any save failure cancels quitting.
- The startup title cannot be covered by Pause, while the existing smartphone and inventory ESC priority remains intact.
- Runtime and Editor builds completed with zero errors. Eleven static contracts passed for menu controls, state restoration, save authority, exception recovery, and save-before-quit ordering.
- Unity was not launched under the two-occurrence native-render crash boundary. Live ESC/click behavior, 1920×1080 readability, and built-player quit/relaunch remain unverified, so Task 097 is PARTIAL.

Next: continue with another approval-free product gap; keep Tasks 088–097 live verification grouped behind the safe Unity capture-path decision.

## 2026-07-17 Continuation — Task 098 Week-One Completion Implemented / Project Partial

- Added the missing product endpoint to the existing long-play owner: Day 7 Settlement now opens a one-time Week One completion summary.
- The summary reads the existing player name, cumulative revenue, cash, tier, reputation, and Day 7 settlement record; it creates no reward, progression service, or save field.
- Continue performs a Day 7 save, closes the modal, advances through the authoritative day-loop method, and saves Day 8. Quit is called only after a successful save.
- The modal preserves time/cursor state and blocks the background shop sign from advancing the day before the player chooses.
- Runtime and Editor builds completed with zero errors. Twelve static contracts passed for display gating, state reads, save ordering, background blocking, and recovery.
- Unity was not launched under the two-occurrence native-render crash boundary. Live Day 7 presentation, 1920×1080 readability, built-player quit, and Continue reload remain unverified, so Task 098 is PARTIAL.

Next: finish another approval-free player-facing gap while the bounded live-validation batch remains pending.

## 2026-07-17 Continuation — Task 099 Input-Accurate Startup Controls Implemented / Project Partial

- Added a controls step to the existing first-day startup state machine immediately after the P.A. Phone introduction.
- The copy mirrors the authoritative `PlayerInputHandler`: WASD/arrows, Space, I/P/C, hotbar 1–9/wheel, build click/R/M/X, F5/F9, and ESC.
- Confirming the screen continues through the existing supplies, arrival, and Day 1 flow. No parallel help manager, input state, or save field was added.
- Runtime and Editor builds completed with zero errors. Eleven functional contracts passed for step order, exact input mapping, read-only presentation, and preservation of the title/Continue and arrival paths.
- Two invalid verification assumptions were logged and retired: a nested PowerShell `$input` collision and a dirty-worktree `HEAD` cleanliness check for the pre-existing M/X input diff.
- Unity was not launched under the two-occurrence native-render crash boundary. Live 1920×1080 readability and click progression remain unverified, so Task 099 is PARTIAL.

Next: continue with one approval-free player-facing product gap; keep Task 099's live UI check in the bounded safe-capture validation batch.

## 2026-07-18 Continuation — Task 100 Title Quit Path Implemented / Project Partial

- Reused the existing `FirstDayPrototypeCanvas` and button factory to add a title-only Quit Game control.
- The title lays out Quit, Continue, and New Game at three non-overlapping positions. Non-title onboarding and the Day 1 summary restore the existing button layout and hide Quit.
- Continue loading disables Quit until failure recovery. A built player calls `Application.Quit`; the Editor keeps Play Mode under developer control and shows an explanatory message.
- The title quit path performs no save mutation and preserves New Game, save-aware Continue, Task 099 controls, Pause Save & Quit, and the week-one endpoint.
- Runtime and Editor builds completed with zero errors. Fourteen contracts passed for visibility, layout, loading lock, isolated Quit ownership, Editor fallback, and preservation boundaries.
- Unity was not launched under the two-occurrence native-render crash boundary. Live 1920×1080 layout and built-player process exit remain unverified, so Task 100 is PARTIAL.

Next: continue with one approval-free player-facing completion gap while keeping Tasks 096–100 in the bounded live product-control validation batch.

## 2026-07-18 Continuation — Task 101 Existing-Save New-Game Guard Implemented / Project Partial

- Reused the title's authoritative save-presence result to guard New Game; no save manager, key, schema, or file was changed.
- New Game remains direct when no save exists. When a save exists, it now explains that the current file is not deleted immediately but a later save will overwrite the single slot.
- Confirm continues to the existing name-registration flow. Cancel returns to the title and refreshes save presence. New Game is disabled while the asynchronous check is pending.
- The confirmation path performs no save, load, or delete mutation and preserves Continue, title Quit, and the input-accurate onboarding.
- Runtime and Editor builds completed with zero errors. Fifteen contracts passed for state order, both save branches, async locking, back navigation, authority preservation, and mutation isolation.
- Unity was not launched under the two-occurrence native-render crash boundary. Live no-save/save-present clicks, 1920×1080 readability, and the first subsequent F5 overwrite behavior remain unverified, so Task 101 is PARTIAL.

Next: continue with one approval-free player-facing completion gap; keep Tasks 096–101 in the bounded live product-control validation batch.

## 2026-07-18 Continuation — Task 102 B09 Storage UI Implemented / Project Partial

- Confirmed that the binary main scene contains no `StorageUI` component: B09 already had a 24-slot `StorageBox` and v10 `storedItems` persistence, but interaction ended after the OpenBox call with no usable screen.
- The runtime binder now guarantees one `StorageUI` under the existing UI root. Its 6×4 panel shows actual item icons, counts, quality, and effective price.
- Storing creates a one-item metadata-preserving split and decrements only the selected hotbar slot after `StorageBox.AddInstance` succeeds. Withdrawal keeps the existing `Inventory.AddInstance` authority and preserves storage contents when the bag is full.
- Storage is mutually exclusive with inventory, phone, and crafting panels. ESC closes storage before Pause and restores the cursor state captured before opening.
- Runtime and Editor builds completed with zero errors. Fourteen final contracts passed for B09 entry, singleton UI, slot layout, metadata, exact transfer, full-bag safety, panel/cursor priority, and v10 persistence isolation.
- Unity was not launched under the two-occurrence native-render crash boundary. Live B09 interaction, 1920×1080 presentation, transfer clicks, and v10 save/load remain unverified, so Task 102 is PARTIAL.

Next: continue with another approval-free end-to-end gameplay gap while keeping Task 102 in the bounded safe-capture validation batch.

## 2026-07-18 Continuation — Task 103 Crafting Product Flow Implemented / Project Partial

- Confirmed that all eight existing recipes require a workbench, so the onboarding-advertised C panel filtered every recipe out and opened empty.
- C now opens a read-only recipe book containing all eight recipes and their required stations; remote crafting is disabled. Workbench interaction still filters by station and delegates the only transaction to `CraftingService.TryCraft`.
- Recipe cards use existing item art and show output quantity, every ingredient's owned/required count, tier/friendship lock state, and refreshed success/failure feedback.
- Added a full-screen input blocker, mutual exclusion with storage/inventory/phone, captured cursor restoration, and crafting-first ESC handling. Duplicate components no longer destroy the shared runtime UI host.
- Runtime and Editor builds completed with zero errors. Sixteen contracts passed for recipes, station context, remote-craft blocking, presentation, feedback, input blocking, cursor/ESC behavior, and authority preservation; diff check passed.
- Unity was not launched under the two-occurrence native-render crash boundary. Live C/Space interaction, B05–B08 click results, ESC transitions, and 1920×1080 presentation remain unverified, so Task 103 is PARTIAL.

Next: continue with one approval-free end-to-end gameplay gap while Tasks 096–103 remain in the bounded safe-capture validation batch.

## 2026-07-18 Continuation — Task 104 Tripo Policy And B11 Fountain Physics Implemented / Project Partial

- Reconciled the new Grid-customization and Tripo-finalization directives with the already implemented P1–P5 placement system and the existing full asset audit instead of creating a parallel system.
- Added durable architecture decisions for per-asset 1–8 classification, player/resident identity preservation, functional-furniture placement/access/save contracts, non-destructive derivatives, and provenance gates.
- Identified B11's remaining concrete defect: the circular fountain used a 6×6 root BoxCollider, so its square corners blocked space outside the visible stone silhouette.
- The runtime visual sidecar now reuses each actual B11 Visual mesh as a non-convex static MeshCollider and disables only root BoxColliders after at least one valid mesh has been configured. Missing meshes leave the safe original collision in place.
- The existing capsule-shaped carving NavMeshObstacle, model, materials, placement, source FBX, wrapper prefab, and binary main scene remain untouched.
- Runtime and Editor builds completed with zero errors. Twelve contracts passed for ordering, idempotency, real-mesh ownership, safe fallback, root-only Box removal, NavMesh authority preservation, protected-file boundaries, ADR markers, and diff integrity.
- Unity was not launched under the two-occurrence native-render crash boundary. Live four-sided walking, bench access, NPC avoidance, and a same-camera After capture remain unverified, so Task 104 is PARTIAL.

Next: continue the approval-free full-game completion path; keep Task 104's live movement/capture check in the bounded safe Unity validation batch.

## 2026-07-18 Continuation — Task 105 Late-Night Customer Flow Implemented / Project Partial

- Audited the existing resident schedules against the 18:00–23:00 shop window and confirmed that Rest starts at 19:00–20:00, removing all eligible customers before closing.
- Added an idempotent Rest-only shop-visit override without changing the active schedule phase. Work, Sleep, Day 1 behavior, and already active residents remain protected.
- Outdoor and interior customer sidecars now lease original position, shop reference, and schedule override ownership. Completion, failed start, timeout, and shop close restore every borrowed state.
- Existing `NpcController` movement/purchase FSM, customer caps, `TryForceShop`/`TryBeginShoppingVisitAt`, purchase math, schedule assets, save schema, scenes, prefabs, and packages remain unchanged.
- Runtime and Editor builds completed with zero errors. Eighteen contracts passed for Rest/phase restrictions, outdoor/interior leases, every restore path, and existing authority preservation; diff check passed.
- Unity was not launched under the two-occurrence native-render crash boundary. Live 18:30/20:30/22:30 traffic, customer caps, and 23:00 recall remain unverified, so Task 105 is PARTIAL.

Next: continue one approval-free end-to-end gameplay gap; batch Task 105's live clock checks with the eventual approved safe Unity validation route.

## 2026-07-18 Continuation — Task 106 Processed Village Visual Real-Asset Replacement / Project Partial

- Audited the first Processed next-day village response and confirmed that its workbench, two crates, board, and banner were still five runtime primitive cubes, directly contradicting the current art direction and asset-finalization policy.
- Replaced the blockout with the existing B05 workbench definition's `Visual` child only. The functional wrapper is never instantiated; the already validated Project P.A. wood-to-plank preparation kit and B10-derived sign provide process and role readability.
- Matched the validated B05 working-face correction with a 180-degree holder rotation and bounded the plaza footprint at 0.58 scale. The sign reads `가공 준비대`.
- Every cloned Collider, Rigidbody, NavMeshObstacle, MonoBehaviour, and extra Light is disabled and removed. Existing B05 functionality, placement, unlocks, Raw visual exclusivity, next-day timing, and v10 category persistence remain unchanged.
- Runtime and Editor builds completed with zero errors. Eighteen contracts passed for primitive removal, real resource resolution, wrapper isolation, visual-only stripping, Raw preservation, and persistence authority; diff check passed.
- Unity was not launched under the two-occurrence native-render crash boundary. Existing B05 source evidence was inspected, but no new same-camera plaza capture exists, so Task 106 is PARTIAL.

Next: continue one approval-free end-to-end implementation gap; include Task 106's Processed before/after composition in the eventual approved safe Unity batch.

## 2026-07-27 Continuation — Task 107 Utility Repair Point Implemented / Project Partial

- Confirmed an honest end-to-end content basis: `Item_12_ToolSet` is a sellable Utility item and `Recipe_ToolSet` uses the existing B07 Forge.
- Extended the existing sale observer and generic v10 category-string persistence; no category, reward, unlock, or save field was added.
- Utility sales now create a pending change that activates only on a later DayPreparation. Processed, Raw, and Utility visuals are mutually exclusive.
- Instantiates only `Building_B07_BlacksmithForge.prefab/Visual` at 0.44 scale with the Project P.A. `공구 수리대` sign. The functional wrapper is never instantiated.
- Collider, Rigidbody, NavMeshObstacle, MonoBehaviour, and Light components are disabled and removed, preserving the real B07 station, placement, unlocks, economy, purchase, NPC, scene, prefab, FBX, and package boundaries.
- The first `--no-restore` build stopped because Unity had removed `Temp/obj` assets. Standard restore-inclusive Runtime and Editor builds then completed with zero errors; 23/23 static contracts passed.
- Unity was not launched under the repeated direct-render native-crash boundary. Utility sale-day/next-day timing, save restore, same-camera composition, and player/NPC routes remain unverified, so Task 107 is PARTIAL.

Next: continue one approval-free implementation gap; include Task 107 with the bounded safe Unity village-change validation batch.

## 2026-07-27 Continuation — Task 108 Luxury Craft Display Implemented / Project Partial

- Confirmed an honest content basis: `Item_11_Furniture` and `Item_13_Clothes` are both sellable Luxury items; their existing recipes use the Basic Workbench and B08 Sewing Table.
- Extended the existing sale observer and generic v10 category-string persistence; no category, reward, unlock, or save field was added.
- Luxury sales now create a pending change that activates only on a later DayPreparation. Processed, Raw, Utility, and Luxury visuals are mutually exclusive.
- Instantiates only `Building_B08_SewingTable.prefab/Visual` at 0.48 scale with the Project P.A. `공예 전시대` sign. The functional wrapper is never instantiated.
- Collider, Rigidbody, NavMeshObstacle, MonoBehaviour, and Light components are disabled and removed, preserving the real B08 station, placement, unlocks, economy, purchase, NPC, scene, prefab, FBX, and package boundaries.
- Runtime and Editor builds completed with zero errors. The first static audit had two selector false negatives because B08 stores `Visual` as a prefab override and save/package files were already dirty. The corrected single rerun passed 27/27 contracts.
- Unity was not launched under the repeated direct-render native-crash boundary. Luxury sale-day/next-day timing, v10 restore, same-camera composition, and player/NPC routes remain unverified, so Task 108 is PARTIAL.

Next: continue one approval-free implementation gap; include Task 108 with the bounded safe Unity village-change validation batch.

## 2026-07-27 Continuation — Task 109 Hiring Product Flow Implemented / Project Partial

- Audited all eight smartphone candidates and confirmed every dedicated `spawnPrefab` was null, making the existing hiring screen a dead end.
- Preserved future explicit prefab priority and added a role-exact fallback to the existing C-02–C-09 Producer/Specialist residents. A usable source must include `NpcController` and a real `SkinnedMeshRenderer`; runtime hired clones cannot become future templates.
- New hiring and existing v10 restore share the same resolver. Candidate profile, specialty, schedule, dialogue, and unique friendship identity are injected without changing SaveManager or candidate assets.
- Specialists receive only the existing Resources recipes matching their workbench type, so a hired Tailor no longer inherits the source resident's unrelated Bread assignment.
- Candidate, tier, source, and balance validation happens before the existing `EconomyService.TrySpend`. The UI now builds on first open and shows bio, Korean role, cost, unavailable state, and success/failure feedback.
- Runtime and Editor builds completed with zero errors. An initial protected-boundary selector misread a SaveManager explanatory comment as a write; the corrected single rerun passed 36/36 static contracts.
- Unity was not launched under the repeated direct-render native-crash boundary. Actual hiring, role behavior, save/reload restore, and 1920×1080 UI readability remain unverified, so Task 109 is PARTIAL.

Next: continue one approval-free end-to-end gap; include Task 109 in the eventual approved safe Unity product-flow validation batch.

## 2026-07-27 Continuation — Task 110 Week-One Hiring Milestone Implemented / Project Partial

- Audited the first-week plan after Task 109 and found that Day 5 still asked the player only to “identify future worker needs”; successful hiring was absent from the playable checklist and week-one result.
- Day 5+ daytime objectives now direct an unhired player to P.A. Phone > Hiring. The existing operations checklist adds one workforce line without replacing activity, product, stocking, pricing, shop-open, sale, or settlement progress.
- After a hire, the line immediately changes to the real candidate name, Korean role, and total hired count. LongPlay listens to the existing `OnHired` event only to refresh presentation.
- Week-one completion reads the same existing hired-candidate collection, sorts it deterministically, and summarizes up to three names/roles plus any remaining count.
- Hiring, cost, spawn, specialist behavior, economy, tier, NPC FSM, save authority, candidate/recipe assets, scene, prefabs, and packages remain unchanged.
- Runtime and Editor builds completed with zero errors. Twenty-nine static contracts passed for Day 5 boundaries, no-hire/hired states, real roster identity, all eight role labels, event lifecycle, week-one summary, existing checklist preservation, and read-only authority.
- Unity was not launched under the repeated direct-render native-crash boundary. The live Day 5 hire transition, Day 7 summary, and 1920×1080 readability remain unverified, so Task 110 is PARTIAL.

Next: continue one approval-free end-to-end gap; include Tasks 109–110 in the eventual approved safe Unity hiring and first-week validation batch.

## 2026-07-27 Continuation — Task 111 Atomic Producer Delivery Implemented / Project Partial

- Audited the post-hiring producer-to-player transfer and found a concrete loss path: the producer charged through `EconomyService`, then deleted its stock when the player's inventory rejected the delivery. The existing `AddInstance` could also fill part of a matching stack before returning false.
- Added an exact read-only `Inventory.CanAddInstance` preflight using the same metadata-stack and empty-slot rules as `AddInstance`. `AddInstance` now exits before all mutations when the full stack cannot fit.
- Reordered actual producer delivery to validate player inventory and full-stack capacity before the existing spend call. Full-bag, missing-inventory, and insufficient-funds outcomes keep producer stock and make no inventory transfer.
- Preserved `ItemInstance` quality/currentPrice on success and remove producer stock only after the transfer completes. An unexpected post-charge add failure refunds the full buy-in through the existing economy authority and retains producer stock.
- Reused the existing per-NPC bubble for successful delivery, full-bag holding, required buy-in money, and exceptional refund feedback; no new UI or economy service was created.
- Runtime and Editor builds completed with zero errors. Thirty static contracts passed for preflight immutability, failure atomicity, operation ordering, metadata transfer, refund, bubble feedback, FSM preservation, and the existing LongPlay delivery path.
- Unity was not launched under the repeated direct-render native-crash boundary. The live full-bag hold → free-space → retry delivery and bubble readability remain unverified, so Task 111 is PARTIAL.
- No EconomyService, LongPlay code, save schema/authority, NPC schedule/FSM/data, hiring, shop purchase/sale, scene, prefab, asset, package, commit, or push operation was performed.

Next: continue one approval-free end-to-end gap; include Tasks 109–111 in the eventual approved safe Unity hiring, producer delivery, and first-week validation batch.

## 2026-07-27 Continuation — Task 112 Week-Two Operations Campaign Implemented / Project Partial

- Audited the Day 7 completion path and confirmed that save → Day 8 worked, but every Day 8+ daytime objective fell back to one generic operations sentence unrelated to the project's existing growth systems.
- Added explicit Day 8–14 plans: reserve stock in B09, sell a Processed product, hire village support, sell two categories, reach Tier 1, inspect the next-day village response, and close the week with two different products.
- The player-facing continuation checklist now reads actual stored units, same-day sale records, hired candidates, Tier, and active village-culture state every 0.5 seconds. It does not own or mutate any of those systems.
- Kept automatic onboarding deliveries limited to Days 2–7. Week 2 requires direct gathering, producer buy-ins, processing, storage, display, pricing, and night sales through existing gameplay.
- Preserved Day 1–7 plans, the Day 7 completion modal and save → Day 8 transition, and the activity/product/stock/price/open/sale/settlement checklist.
- Runtime and Editor builds completed with zero errors. Forty static contracts and the diff check passed.
- Unity was not launched under the repeated direct-render native-crash boundary. Live Day 7→8, representative Day 8–14 transitions, and 1920×1080 readability remain unverified, so Task 112 is PARTIAL.
- No economy, inventory, sales, crafting, hiring, Tier, village-change, save-schema/authority, scene, prefab, asset, package, commit, or push operation was performed.

Next: continue one approval-free end-to-end gap; include Task 112 in the eventual approved safe Unity multi-day validation batch.

## 2026-07-27 Continuation — Task 113 Tripo Re-Audit And B12 Trade-Port Collision Hardening / Project Partial

- Reconciled the latest Tripo temporary-asset and Grid placement directive with the existing per-asset classification, character-identity, Placeable, non-destructive-source, and provenance ADRs.
- Recounted 174 FBX, 150 OBJ, zero GLB, and zero Blend files; the 24 non-Nature-Pack project FBX files remain fully represented in the audit. C-01–C-09 importer metadata contains direct `tripo_node_*` evidence, while individual commercial-use records remain a deployment gate.
- Confirmed the historical B12 wrapper uses a 10×5m root Box/Obstacle while the measured Visual is about 3.63×1.96m. The saved-scene Box audit had passed earlier, but the source wrapper and box-shaped carving obstacle could still recreate the invisible coastal barrier.
- Extended the existing runtime visual sidecar to calculate B12 Visual bounds from all eight transformed mesh corners and shrink only clearly oversized root `BoxCollider` and box-shaped `NavMeshObstacle` axes.
- Missing meshes preserve existing physics and retry later; no axis can grow. No trade interaction, Placeable registration, fake approach point, or save state was added.
- Runtime and Editor builds completed with zero errors. Twenty static contracts and the target diff check passed.
- Unity was not launched under the repeated direct-render native-crash boundary. Live coastal traversal, visible-boundary stopping, NPC carving avoidance, and a same-camera capture remain unverified, so Task 113 is PARTIAL.
- No source FBX, texture, wrapper prefab, main scene, BuildingData, blueprint, save schema, package, external tool/asset/service, commit, or push operation was performed.

Next: continue one approval-free end-to-end implementation gap; include Task 113 with Task 104 and the visual-route checks in the eventual approved safe Unity batch.

## 2026-07-27 Continuation — Task 114 Month-One Campaign And Day-30 Completion / Project Partial

- Audited the current long-play route and confirmed that authored progression stopped at Day 14 even though the design roadmap promises facility, workforce, assortment, reputation, and village-economy growth through Day 30.
- Added sixteen Day 15–30 plans that rotate existing storage, processing, hiring, category sales, Tier, and active village-change state rather than introducing another quest or crafting framework.
- The visible continuation checklist reads actual storage contents, same-day sale records, hired count, Tier, and active culture. It performs no transaction, unlock, hire, craft, or persistence mutation.
- Added an explicit Day 30 Settlement completion record showing cumulative revenue, money, Tier, reputation, hired roster, active village direction, and the final day's sales decision summary.
- Continue performs Day 30 save → the existing next-day authority → Day 31 save; quit still requires a successful save. Day 7 supply cutoff and week-one save → Day 8 remain separate and intact.
- The first parallel build caused a shared Runtime output lock and the first static script had a PowerShell parser error; both were logged and resolved without repeating the failed commands. Sequential Runtime/Editor builds completed with zero warnings and zero errors, and 56/56 static contracts plus target diff checks passed.
- Unity was not launched under the repeated direct-render native-crash boundary. Representative Day 15–30 transitions, both Day 30 actions, Day 31 continuation, and 1920×1080 readability remain unverified, so Task 114 is PARTIAL.
- No save schema/authority, economy, sales, crafting, hiring, Tier, village-change authority, scene, prefab, asset, package, commit, or push operation was performed.

Next: continue one approval-free functional completion gap; include Task 114 in the eventual approved safe multi-day/save/UI validation batch.

## 2026-07-27 Continuation — Task 115 Tier-1 Forge And Tool-Set Value Chain / Project Partial

- Audited every Month-One goal against player-reachable data. `Building_B07_BlacksmithForge`, its blueprint, `Recipe_ToolSet`, and `Item_12_ToolSet` are all authored at Tier 1, while only the placement catalog delayed B07 to Tier 3.
- Aligned the B07 catalog tag, minimum Tier, and duplicate-safe ledger reward with that existing Tier-1 authority. The separate B05 starter, B06 Tier 2, B08 Tier 3, and TierService's 10,000G/100,000G requirements remain unchanged.
- Replaced the seed-bypassable Day 23 Utility check with active B07 + exact same-day Tool Set + a second product. Replaced the pre-Tier-2-impossible Day 24 Luxury check with active B07 + exact Tool Set + a Processed sale.
- The checklist explains the actual route: recover two empty shelves, place the 3x2 forge, process Plank 1 and Ore 4 into Iron Bars 2 and Tool Set 1, then sell a mixed assortment. It grants, crafts, unlocks, or sells nothing.
- Corrected the cumulative goal display from its Day-14 15,000G → Day-15 3,900G regression to a monotonic Day-15 16,000G → Day-30 31,000G curve with continued post-month growth. Tier balance data was not changed.
- Sequential Runtime and Editor builds completed with zero errors. Only the existing Unity generator CS8785 and Editor CS0414 warnings remain. Executable source contracts passed 39/39 and the target diff check passed.
- Unity was not launched under the repeated direct-render native-crash boundary. Actual Tier-1 reward, shelf recovery, B07 placement/access, crafting/sales, Day 23/24 UI transition, and 1920x1080 readability remain unverified, so Task 115 is PARTIAL.
- No TierDefinition, recipe/item/building/prefab/scene, crafting/sales/economy/save authority, package, commit, or push operation was performed.

Next: continue one approval-free functional completion gap; retain Task 115 with Task 114 in the approved safe multi-day/forge/save/UI validation batch.

## 2026-07-27 Continuation — Task 116 Safe GameView Capture Foundation Pass 1 / Project Partial

- Audited every actual direct `camera.Render()` call under `Assets/Editor` and found twelve. The ThemeCorner source contains one explanatory comment but no remaining direct invocation.
- Extracted the already exercised ThemeCorner GameView flow into `PA_SafeGameViewCapture`: requested resolution, Canvas/TMP refresh, settle delay, ordinary `ScreenCapture.CaptureScreenshot`, fresh nontrivial PNG polling, and `finally` restoration of camera and screen state.
- Migrated the known second native-crash site in `PA_ShopCustomizationValidator` and both current Tier/B07 captures in `PA_ShopProgressionUnlockValidator` to the shared asynchronous path.
- The three target files now contain zero actual direct camera-render calls. Runtime build completed with zero warnings and zero errors; Editor completed with zero errors and only the existing CS8785/CS0414 warnings.
- Twenty-eight source contracts and the target diff check passed. Ten audited direct-render sites remain in Character/Cottage/CustomerPanel/DemoView/FinalPresentation/GatheringShop/OutdoorPlacement/ShopEvolution/VillageCulture/Workbench Editor tooling.
- Unity was not launched because the same native cause has already crashed twice. Pass 1 does not authorize a third attempt while those ten sites remain, so Task 116 is PARTIAL.
- No runtime gameplay code, scene, prefab, asset, save/economy/NPC/placement authority, package, ProjectSettings, graphics API, commit, or push operation was performed.

Next: migrate the ten remaining direct-render sites in bounded passes, prove the repository-wide actual-call count is zero, then request the already-required human judgment for one isolated D3D11 GameView capture.

## 2026-07-27 Continuation — Task 117 Safe GameView Capture Foundation Pass 2 / Project Partial

- Selected the three highest player-facing sites from the ten remaining direct-render tools: VillageCulture for next-day category visuals, CustomerPanelLayout for 1920x1080 UI, and FinalPresentation for the six-shot final review.
- Replaced each validator's synchronous step loop with one guarded asynchronous task, so every ordinary GameView PNG is awaited before the validator mutates the next sale, day, price, NPC, audit, or settlement state.
- Routed all ten captures through the existing `PA_SafeGameViewCapture`. The market marker, 46-degree FOV, all-layer culling, 1920x1080 request, optional warning policy for VillageCulture/CustomerPanel, and FinalPresentation's normal/rare price file list remain intact.
- Removed the three local RenderTexture/ReadPixels/direct-camera-render implementations. The target call count is zero and the repository remainder is seven: Character, Cottage, DemoView, GatheringShop, OutdoorPlacement, ShopEvolution, and Workbench.
- Runtime build completed with zero warnings and zero errors. Editor build completed with zero errors and only the existing CS8785/CS0414 warnings. The first source audit used four outdated helper variable selectors and ended at 34/38; after re-reading the helper and replacing those selectors rather than retrying them, the corrected contracts passed 38/38. Target diff checks passed.
- Unity was not launched because the same native cause has already crashed twice. Pass 2 does not authorize a third attempt while seven direct-render sites remain, so Task 117 is PARTIAL.
- No shared helper, runtime gameplay code, scene, prefab, asset, save/economy/NPC/placement authority, package, ProjectSettings, graphics API, commit, or push operation was performed.

Next: migrate the seven remaining direct-render sites in bounded passes, prove the repository-wide actual-call count is zero, then obtain human judgment before one isolated D3D11 GameView capture.

## 2026-07-27 Continuation — Task 118 Safe GameView Capture Foundation Pass 3 / Project Partial

- Selected DemoView, GatheringShop, and OutdoorPlacement from the seven remaining direct-render tools because they cover the actual tracking-camera reference, the day-activity-to-night-sale review, and outdoor placement/collision evidence.
- DemoView now awaits one 2560x1440 ordinary GameView PNG after the existing 11-second indoor or 4.5-second outdoor staging delay. It keeps the actual tracking camera instead of introducing a validator-only frame.
- GatheringShop awaits five 1920x1080 shots in the preserved forage, gathered, night-market, customer-reaction, and next-day-settlement sequence. OutdoorPlacement awaits two 1280x720 shots with the same orthographic position, focus, size, and nontrivial-file checks.
- Each flow temporarily disables the active `CameraController` during capture and restores it in `finally`; the shared helper remains the owner of camera and screen-state restoration.
- The three targets contain no RenderTexture, ReadPixels, or actual direct camera-render calls. The repository remainder is four: Character, Cottage, ShopEvolution, and Workbench.
- Runtime build completed with zero warnings and zero errors. Editor build completed with zero errors and only the existing CS8785/CS0414 warnings. Static contracts passed 35/35 and target diff checks passed.
- Unity was not launched because the same native cause has already crashed twice. Pass 3 does not authorize a third attempt while four direct-render sites remain, so Task 118 is PARTIAL.
- No shared helper, runtime gameplay code, scene, prefab, asset, save/economy/NPC/placement authority, package, ProjectSettings, graphics API, commit, or push operation was performed.

Next: migrate the four remaining direct-render sites in bounded passes, prove the repository-wide actual-call count is zero, then obtain human judgment before one isolated D3D11 GameView capture.

## 2026-07-27 Continuation — Task 119 Safe GameView Capture Foundation Pass 4 / Project Partial

- Selected Character, Cottage, and Workbench from the four remaining direct-render tools because they cover the first three visual priorities: player/NPC grounding and locomotion, frequently seen cottage scale, and the functional preparation station connecting day activity to night sales.
- Character now awaits its 1600x900 source lineup plus runtime idle and walking shots. The existing 350ms preparation, 600ms actual movement loop, 250ms completion delay, grounding/Avatar/collider/NavMesh/cadence checks, and filenames remain intact.
- Cottage awaits its 1920x1080 scene overview, four isolated directions, final view, and runtime view. Its 18m orthographic framing, renderer isolation, entrance/label checks, and renderer restoration remain intact.
- Workbench awaits both four-direction audit sets and the selected runtime baseline or final view. Its front-side placement, collider/carving, real Wood-to-Plank interaction, CraftingUI, and feedback checks remain intact.
- Actual game-camera captures temporarily disable `CameraController` and restore it in `finally`. Temporary Workbench audit objects and Cottage renderer states also restore after awaited captures.
- The three targets contain no RenderTexture, ReadPixels, or actual direct camera-render calls. The repository remainder is one: ShopEvolution.
- Runtime build completed with zero warnings and zero errors. Editor build completed with zero errors and only the existing CS8785/CS0414 warnings. Static contracts passed 42/42 and target whitespace checks passed.
- Unity was not launched because the same native cause has already crashed twice. Pass 4 does not authorize a third attempt while one direct-render site remains, so Task 119 is PARTIAL.
- No shared helper, runtime gameplay code, scene, prefab, asset, save/economy/NPC/placement authority, package, ProjectSettings, graphics API, commit, or push operation was performed.

Next: migrate the final ShopEvolution direct-render site, prove the repository-wide actual-call count is zero, then obtain human judgment before one isolated D3D11 GameView capture.

## 2026-07-27 Continuation — Task 120 Safe GameView Capture Foundation Final Pass / Project Partial

- Migrated the final direct-render site in `PA_ShopEvolutionVisualFinalizer` to the shared ordinary GameView screenshot helper.
- The B02-B04 source audit now awaits twelve 1600x900 four-direction captures. Runtime evidence awaits the baseline or Tier 1-3 after files while preserving the four-second initial setup, 750ms tier settling, orthographic 6.6 framing, and filenames.
- A single runtime `Task` guard advances only after each PNG is complete. The existing before/after `ShopCustomizationController.WriteSaveFields` JSON equality check still runs after the final tier.
- Runtime GameCamera capture temporarily disables `CameraController`; its enabled state and `clearFlags` restore in `finally`, while the shared helper restores transform, projection, culling, viewport, target texture, and screen state.
- The target contains no RenderTexture, ReadPixels, or actual direct camera-render call. Repository-wide actual direct `Camera.Render()` invocation count is zero; one explanatory crash comment remains.
- Runtime build completed with zero warnings and zero errors. Editor build completed with zero errors and only the existing CS8785/CS0414 warnings. Static contracts passed 36/36.
- Unity was not launched because the same native cause has already crashed twice. Human judgment is still required before a third attempt, so Task 120 is PARTIAL.
- No shared helper, runtime gameplay code, scene, prefab, asset, save/economy/NPC/placement authority, package, ProjectSettings, graphics API, commit, or push operation was performed.

Next: obtain human judgment, then run one isolated D3D11 GameView PNG capture and inspect freshness, file size, readability, and restored camera/screen state before any sequential validator run.

## 2026-07-27 Continuation — Task 121 Day 31-45 Second-Month Opening Campaign / Project Partial

- Selected the first post-month gap that did not require a new system or human balance approval: after the Day 30 completion record, Day 31 and later fell back to a generic long-term sentence.
- Added fifteen authored Day 31-45 plans to `LongPlayProgressionController`. The preserved revenue curve now gives player-facing checkpoints from 32,500G on Day 31 to 53,500G on Day 45.
- Added fifteen live milestone evaluators to `PlayableDayScenarioController`: storage reserves, processed sales, the existing hired roster, product/category breadth, the Tier 1 forge and exact ToolSet sales, active village culture, prepared catalog breadth, and cumulative revenue.
- Day 45 requires both its existing revenue checkpoint and four distinct same-day product sales. Day 46 returns to the existing long-term fallback.
- The implementation does not create quest/save state or mutate inventory, sales, hiring, placement, village culture, or tier progression. Day 1-30, the Day 30 completion UI, Tier 2 at 100,000G, B06 at Tier 2, and B08 at Tier 3 remain authoritative.
- Sequential Runtime and Editor builds completed with zero errors. Runtime reported the existing CS8785 generator warning; Editor reported the existing CS8785/CS0414 warnings. Static campaign contracts passed 23/23.
- Unity was not launched because the same native cause has already crashed twice. Day 30->31, representative Day 35/40/45 transitions, 1920x1080 readability, and the Day 46 fallback remain unverified, so Task 121 is PARTIAL.
- No scene, prefab, asset, save schema, economy/purchase/crafting/hiring/tier/village authority, package, ProjectSettings, commit, or push operation was performed.

Next: continue the authored long-play route beyond Day 45 toward the existing Tier 2 threshold without changing its balance, unless human judgment first authorizes the isolated D3D11 GameView safety check.

## 2026-07-27 Continuation — Task 122 Day 46-76 Tier 2 Growth Campaign / Project Partial

- Audited the authoritative Tier 2 route before implementation. `Tier2.asset` requires exactly 100,000G, no reputation, and no manual approval; `TierService` reevaluates automatically on cumulative-revenue changes.
- Confirmed the existing monotonic campaign curve reaches 55,000G on Day 46 and exactly 100,000G on Day 76, so no balance change is needed to create a complete Tier 2 arc.
- Added a generated thirty-day Day 46-75 operating rhythm: reserve logistics, processed sales, three-worker/four-product preparation, three-category demand, active forge plus ToolSet/Processed sales, active village direction plus four products, and a revenue checkpoint.
- Reserve targets scale from 12 to 20 units and processed sales from two to four. All other milestones reuse existing state without creating quest, reward, or save authority.
- Day 76 is a separate breakthrough plan and completes only when the actual `TierService.CurrentTier` is at least 2. This creates the functional entrance to the existing Tier 2 B06 kitchen route.
- Sequential Runtime and Editor builds completed with zero errors. Runtime reported the existing CS8785 warning; Editor reported the existing CS8785/CS0414 warnings. Static Tier 2 campaign contracts passed 34/34.
- Unity was not launched because the same native cause has already crashed twice. Representative weekly transitions, automatic Tier 2 advancement, 1920x1080 readability, and the Day 77 fallback remain unverified, so Task 122 is PARTIAL.
- No scene, prefab, asset, save schema, economy/purchase/crafting/hiring/tier/village authority, package, ProjectSettings, commit, or push operation was performed.

Next: connect Day 77 onward to the newly available B06 kitchen and its existing Bread, Baked Potato, and Grilled Fish value chains without adding new recipes or changing Tier balance.

## 2026-07-27 Continuation — Task 123 Day 77-90 Tier 2 Kitchen Value Chain / Project Partial

- Audited B06 as the existing Tier 2 placement-ledger unlock and confirmed that its prefab is a Kitchen workbench.
- Confirmed the existing Wheat→BreadLoaf, Carrot→Baked Potato, and Fish→Grilled Fish recipes, output items, and sale-record paths without adding content.
- Added fourteen authored Day 77-90 plans and live milestones covering B06 placement, each recipe line, two/three-product menus, full pre-opening preparation, a hired Chef, category balance, batch sales, processed village culture, revenue review, and the final ingredient-to-village loop.
- Completion reads only active placement, inventory/hotbar/shelf contents, exact daily sales, the hired roster, village culture, and cumulative revenue. It creates no quest/save state, reward, recipe, item, tier change, or automatic player action.
- Sequential Runtime and Editor builds completed with zero errors. Runtime reported the existing CS8785 warning; Editor reported the existing CS8785/CS0414 warnings. Target whitespace checks passed.
- The source-contract audit stopped because its own `objective-hook` assertion expected three call sites although the objective and checklist correctly provide two. The failed selector was logged and was not retried under project policy.
- Unity was not launched under the repeated native-crash boundary. Task 123 remains PARTIAL until the corrected static audit and later safe runtime validation.
- No scene, prefab, asset, save schema, economy/purchase/crafting/hiring/tier/village authority, package, ProjectSettings, commit, or push operation was performed.

Next: run one corrected Task 123 source-contract audit with an exact two-call expectation; if it passes, continue long-play implementation in a separate task.

## 2026-07-27 Continuation — Task 124 Task 123 Kitchen Contract Recovery / Project Partial

- Ran the recorded source-contract recovery once with the correct expectation of two `TryResolveTierTwoKitchenMilestone(day` call sites.
- The two UI hooks, single evaluator definition, fourteen Day 77-90 plans, fourteen runtime branches, Day 76 boundary, B06 Kitchen prefab, all three Kitchen recipes, all three Processed output categories, and required resources passed.
- The audit stopped at 44/47 because the expected B06 minimum-tier C# expression and two Korean output-name YAML expressions did not match the current source text.
- No second query or adjusted-pattern retry was made in this task. Current evidence does not distinguish an actual data issue from a source-format mismatch.
- No code, scene, prefab, asset, save, package, ProjectSettings, Unity run, commit, or push operation was performed.

Next: inspect only the three authoritative C#/YAML serialization lines, classify data defect versus checker mismatch, and do not rerun the combined audit before that classification.

## 2026-07-27 Continuation — Task 125 B06 Tier and Output-Name Authority Audit / Done

- Inspected only the three authoritative lines identified by Task 124 and did not rerun the combined audit.
- `ShopCustomizationController.ResolveMinimumTier` contains `case "Blueprint_B06_KitchenStation": return 2;`.
- The Baked Potato item serializes its Korean name as `"\uAD6C\uC6B4 \uAC10\uC790"`, which decodes to `구운 감자`.
- The Grilled Fish item serializes its Korean name as `"\uC0DD\uC120\uAD6C\uC774"`, which decodes to `생선구이`.
- All three mismatches were checker-format assumptions, not data defects. Task 123 therefore has 47/47 static evidence from 44 automated contracts plus these three direct authoritative checks.
- Task 124 and Task 125 are DONE. Task 123 remains project-PARTIAL only because its Unity runtime route has not been safely executed.
- No code, scene, prefab, asset, save, package, ProjectSettings, Unity run, commit, or push operation was performed.

Next: return to implementation and connect Day 91 onward instead of spending another task on the resolved checker.

## 2026-07-27 — Task 126 Day 91~105 Tier 3 공동 공방 캠페인

### 결과

- 기존 Tier 3가 평판 3을 요구하지만 평판 증가 호출이 없어 정상 플레이로 도달 불가능한 연결 단절을 확인했다.
- 전문 주민의 낮 재료 요청 완료를 기존 일일 활동 저장 목록에 하루 한 번 기록하고 평판 +1로 연결했다.
- Day 91~105의 열다섯 목표를 기존 Tier 3, B08, 의류/가구 Luxury 제작·판매, 재단사, 마을 변화와 연결했다.
- Day 105는 주민 도움→평판→Tier 3→공방→판매→Luxury 마을 변화의 전체 결과를 함께 요구한다.

### 검증과 경계

- Runtime 빌드 오류 0/기존 CS8785 경고 1개.
- Editor 빌드 오류 0/기존 CS8785·CS0414 경고 2개.
- 정적 계약 24/24, 대상 공백 검사 PASS.
- Unity는 동일 네이티브 원인의 세 번째 실행 전 사람 판단 정책 때문에 실행하지 않았다.
- 씬·프리팹·에셋·저장 스키마·Tier 수치·레시피·아이템·경제식·패키지·ProjectSettings는 변경하지 않았다.
- 첨부 GRID/Tripo 지침은 이미 구축된 P1/P2/P4/P5와 Codex 문서 4종의 장기 권위로 유지하며 다음 단일 에셋 작업에서 재대조한다.

## 2026-08-04 — Task 127 B05~B08 Specialist Workbench Approach

### 결과

- B05~B08의 저작된 전면 interaction 셀이 전문 주민에게 사용되지 않고 작업대 collider/NavMeshObstacle 중심이 목적지였음을 확인했다.
- 기존 배치의 회전된 Workbench interaction 셀을 읽기 전용 좌표로 노출하고, 전문 주민이 완전 경로가 있는 빈 셀을 예약해 이동하도록 연결했다.
- 작업대 이동·회수, 일정 중단, NPC 비활성화 때 예약/제작을 해제하고 도착 시 작업대 정면을 바라보며, 저장 상태 복원 뒤 현재 배치에 다시 접근한다.
- 씬·프리팹·FBX/재질·레시피·인벤토리/경제·저장 스키마는 변경하지 않았다.

### 검증과 경계

- 이전 continuation의 잘못된 csproj/task 경로 2회, 대형 문서 패치와 표식 검사 오류는 실제 `| 127 |` 행을 직접 확인한 뒤 `BUG_LOG.md`에서 RESOLVED 처리했다.
- Runtime 빌드 오류 0/기존 CS8785 경고 1개.
- Editor 빌드 오류 0/기존 CS8785·CS0414 경고 2개.
- Task 127 접근·예약 정적 계약 29/29와 대상 공백 검사 PASS.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트 때문에 실행하지 않는다.
- 실제 B05~B08 전문 주민 이동·정면·겹침 방지·제작은 확인 못 했으므로 Task 127의 프로젝트 판정은 PARTIAL이다.

## 2026-08-04 — Task 128 Tourist Customer Normal-Play Entry

### 결과

- 기존 `[관광객]`은 무일과표 NPC용 표시 폴백뿐이고 실제 작성 고객 8명은 모두 주민이라 두 번째 손님 계층이 정상 플레이에 없음을 확인했다.
- 기존 `CustomerArrivalController` 안에 세션 한정 관광객 입장·쇼핑·퇴장 생명주기를 연결했다.
- 관광객은 작성 주민의 검증된 SkinnedMesh/Avatar 시각만 복제하고 별도 런타임 이름을 사용한다. 주민 일과·직업·의뢰/친밀도·채용·저장에는 등록되지 않는다.
- Day 2+ 실제 개점에만 영업당 최대 2명/동시 1명이 가게 주변 NavMesh 완전 경로에서 들어오며, 기존 `NpcController`와 `PurchaseEvaluator`를 그대로 거친다.
- 구매/거절 말풍선을 읽을 시간 뒤 같은 진입점으로 걸어 나가 런타임 루트와 프로필을 함께 정리한다.

### 검증과 경계

- 교정된 GUID 감사로 씬/프리팹 직렬화 참조 0, SceneAutoBuilder 작성 주민 8명과 기존 관광객 0명 검증 계약을 확인해 이전 감사 오류를 `BUG_LOG.md`에서 RESOLVED 처리했다.
- Runtime 빌드 오류 0/기존 CS8785 경고 1개.
- Editor 빌드 오류 0/기존 CS8785·CS0414 경고 2개.
- Task 128 정적 계약 46/46과 대상 공백 검사 PASS.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트 때문에 실행하지 않았다.
- 실제 입장·`[관광객]` 표시·구매/거절·퇴장·동시 손님/1920×1080은 확인 못 했으므로 프로젝트 판정은 PARTIAL이다.

## 2026-08-04 — Task 129 Headquarters Audit And Tier 4 Full-Campaign Finale

### 결과

- Day 106 이후 일반 목표를 조사하는 과정에서 정상 플레이의 유일한 평판 지급이 Tier 2의 3점에서 끝나고, 기존 본사 감사가 평판 5를 요구해 Tier 4가 도달 불가능함을 확인했다.
- Day 106+ Tier 3에서는 전문 주민의 실제 낮 재료 요청을 완료할 때 기존 날짜별 활동 표식으로 감사 요구 평판까지만 하루 1점을 얻는다.
- 상단 목표와 운영 체크리스트가 실제 감사 조건의 다음 미달 항목을 평판→고용→누적 매출→다음 정기 감사일 순서로 표시한다.
- 모든 조건 충족 뒤 승급은 기존 `AuditService`만 수행한다. LongPlay는 Tier를 직접 변경하지 않는다.
- Tier 4 통과 후 첫 Settlement에 전체 캠페인 기록이 열리고, 저장→다음 날 자유 운영→재저장 또는 저장 성공 후 종료를 선택할 수 있다.

### 검증과 경계

- Runtime 빌드 오류 0/기존 CS8785 경고 1개.
- Editor 빌드 오류 0/기존 CS8785·CS0414 경고 2개.
- 기능 계약 40개 자동 PASS. 더티 기준선을 오인한 범위 검사 1개는 구현 파일의 금지 mutator 0 직접 확인으로 정합화해 총 40개 자동+1개 직접 권위 PASS다.
- 잘못된 Windows 와일드카드 감사와 더티 기준선 검사 오류는 `BUG_LOG.md`에서 RESOLVED 처리했다.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트 때문에 실행하지 않았다.
- 실제 평판 4/5, 감사 성공/실패, Tier 4, 최종 모달, 저장/계속·종료와 화면 가독성은 확인 못 했으므로 프로젝트 판정은 PARTIAL이다.

## 2026-08-04 — Task 130 Headquarters Audit Player Feedback

### 결과

- `AuditService`의 실제 결과가 콘솔과 Inspector 문자열에만 남고 감사 앱은 다음 날짜만 표시하는 피드백 단절을 확인했다.
- 서비스의 기존 판정 뒤 실패·통과·최고 등급·승급 보류 결과를 현재 세션 상태로 발행하고, 앱이 열려 있으면 즉시 새로 고친다.
- 앱은 실제 누적 매출·평판·고용의 현재/요구값, 완료/부족, 다음 감사일, 최근 결과와 가장 가까운 다음 행동을 표시한다.
- 수동 승인 Tier의 진행 바는 더 이상 고정 0이 아니라 감사 세 조건의 실제 부분 진행을 평균한다.
- 기존 스마트폰 높이 안에서 콘텐츠 총 높이를 유지하도록 Tier/매출/시설/감사 카드와 여백을 재배치했다.

### 검증과 경계

- Runtime 빌드 오류 0/기존 CS8785 경고 1개.
- Editor 빌드 오류 0/기존 CS8785·CS0414 경고 2개.
- 감사 기준·유일 승급 권위·모든 결과 분기·이벤트 구독/해제·UI 비변경 권위·고정 높이·메인 씬 비침범 계약 48/48 PASS.
- 읽기 전용 참조 검색에 존재하지 않는 `Assets/Data`를 넣은 오류는 명시 존재 경로로 복구해 `BUG_LOG.md`에 RESOLVED로 기록했다.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트 때문에 실행하지 않았다.
- 실제 실패/성공 결과, 열린 앱 즉시 갱신, Tier 4 문구, 1920×1080 가독성은 확인 못 했으므로 프로젝트 판정은 PARTIAL이다.

## 2026-08-04 — Task 131 Tripo Long-Term Policy Recheck And B06 Kitchen Finalization

### 결과

- 첨부 GRID 기반 커스터마이징 요구가 별도 프로토타입이 아니라 기존 `GridService` 2m zone, 상점 실내/마을 야외 P1~P5, footprint/clearance/interaction, NPC 접근 예약, v10 placeable 저장에 이미 구현되어 있음을 확인했다.
- `Assets` 모델 재고는 FBX 174/OBJ 150/GLB 0/Blend 0이며 non-Nature 고유 FBX 24개로 기존 전수 감사와 일치했다.
- Tripo 대장은 1~8 개별 분류, 캐릭터 외형 정체성 보존, 창고/작업대 기능 우선, 원본 비파괴, 출처/라이선스 배포 게이트를 단일 장기 권위로 유지한다.
- 다음 고노출 미완성인 B06은 기존 Visual renderer bounds를 루트 로컬에서 측정해 래퍼 물리보다 0.2m 이상 작은 X/Z 축만 0.16m 여유로 축소한다. box형 Carving도 같은 center/size를 사용하며 어떤 축도 키우지 않는다.
- 로컬 `-Z` 물리 앞에 `PA_KitchenInteractionAnchor`를 만들고, 성공한 기존 제작 트랜잭션 뒤에만 B06 모델 자체가 0.72초/최대 3.5% pulse한다.
- B05 기능 아트, B06 Tier 2/2×2/Kitchen 레시피, 전문 주민 접근, 저장, FBX·프리팹·씬·재질·BuildingData·설계도는 보존했다. 새 외부 에셋·도구·패키지는 없다.

### 검증과 경계

- Runtime 빌드 오류 0/기존 CS8785 경고 1개.
- Editor 빌드 오류 0/기존 CS8785·CS0414 경고 2개.
- B05 회귀, B06 기존 Visual 재사용, 축소 전용 물리/Carving, 전면 anchor, 성공 후 pulse, Tier 2/2×2, 자산 정책 계약 30/30 PASS.
- 잘못 추측한 계획/loop-state 경로와 Windows wildcard 조회는 정확한 파일 검색으로 복구해 `BUG_LOG.md`에 RESOLVED로 기록했다.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트 때문에 실행하지 않았다.
- 실제 B06 배치, 플레이어/Chef 전면 접근, 모델-물리 경계, Bread 제작 pulse, 동일 GameCamera Before/After는 확인 못 했으므로 프로젝트 판정은 PARTIAL이다.

## 2026-08-04 — WORLD-000 Procedural Island Architecture

### 범위와 보호

- WORLD-000만 활성 ticket으로 삼고 조사·설계·문서화만 수행했다.
- Task 131 loop-state와 136-path dirty worktree를 읽었다. `AudioManager.cs`/`SalesLogManager.cs`의 중단된 미검증 변경은 그대로 보존하고 수정·검증·롤백하지 않았다.
- Git commit/push/reset/clean, Unity 실행, 코드/scene/asset/save/package/ProjectSettings 변경은 없었다.

### 조사 결과와 결정

- `GridService`/BuildManager/OutdoorPlacement는 2m 배치와 footprint/clearance/rotation/move/recover/v10 placeable을 제공하지만 flat fixed zone이며 고도·물·Chunk·role reachability가 없다.
- v10 SaveManager와 repository는 유지하되 legacy fixed world를 자동 변환하지 않는다. 새 world는 사람 승인된 additive seed+generationVersion+sparse delta를 쓴다.
- NPC/shop은 `shopLocation`, `homePoint`, `workSpot`, `dropOffPoint`, 이름/tag/Y+100 등 고정 참조를 role anchor adapter로 단계 이관한다.
- 현행 지형은 Unity Terrain이 아니라 primitive/mesh 기반 fixed map이다. AI Navigation 2.0.12는 async surface update API가 있으나 chunk seam runtime 증거는 없다.
- 최종 권장안은 2m cell, 16×16 Chunk, 1m 높이 0~6, 128×128 기본 논리 섬의 custom chunk mesh다. 완전 voxel과 Unity Terrain 권위는 배제했다.

### 씬 전략

- Prototype_FirstDay: Golden Regression Scene/Core Slice/제출 안전본. 신규 월드 실험 금지.
- WorldSandbox: 승인 후 editor builder로 만들 최소 기술 testbed. WORLD-000에서는 생성하지 않음.
- MainGame: Gate 1~5 이후 기존 기능 inventory와 사람 승인에 따라 별도 통합. 즉시 재작성 금지.

### 결과

- World North Star, architecture plan, system impact map, WORLD-001~012 backlog, ADR을 작성하고 기존 방향 문서에 최소 반영했다.
- WORLD-001은 scene 분리·WorldSandbox 생성·prototype 수치 승인 전 시작하지 않는다.
- 최종 loop-state는 `needs_human_review`다. 이는 WORLD-000 문서 미완성이 아니라 후속 구현의 scene/save/nav/MainGame 사람 Gate를 뜻한다.
