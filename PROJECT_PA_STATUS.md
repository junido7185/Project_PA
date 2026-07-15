# Project PA Status

Inspection date: 2026-06-19
Project root: `C:\Users\sdjsd\Desktop\Unity\Project_PA`
Unity version: 6000.3.2f1

## Verification

- Confirmed working directory is the allowed Project_PA root.
- Confirmed Git root is the allowed Project_PA root.
- Confirmed Unity project folders exist: `Assets/`, `Packages/`, `ProjectSettings/`.
- Git repository is present on branch `master`, tracking `origin/master`.
- Initial Git status was clean: no modified, staged, or untracked files before creating this status/TODO documentation.

## Current Project Summary

Project_PA appears to be a Unity game prototype/final-project build for "P.A. / Pioneer Assistance". The current implementation looks like a playable management/adventure vertical slice with:

- Player movement and interaction.
- Inventory and hotbar UI.
- Shop slots, pricing, and economy flow.
- NPC dialogue, schedules, shopping/production behavior, friendship, and hiring.
- Crafting/workbench systems.
- Building/grid/save systems.
- Runtime UI creation/binding helpers.
- A guided playable-day scenario flow.

Initial inspection did not open Unity Editor, enter Play Mode, modify gameplay code, or build an executable.

## 2026-06-19 Visual Acceleration Update

Design guardrail:

- `PROJECT_PA_DESIGN_INTENT.md` is now the primary working standard.
- This pass preserved Project_PA as a reverse supply-chain management simulation.
- The market stall work is framed as an operating hub for supply, processing, pricing, purchase judgment, revenue, and reinvestment.

Files changed or created in this pass:

- Corrected `ProjectSettings/EditorBuildSettings.asset` so the missing `Assets/Scenes/SampleScene.unity` is no longer listed.
- Created scene backup: `Assets/Scenes/_Backups/Prototype_FirstDay_before_visual_acceleration_20260619.unity`.
- Added Editor-only helper: `Assets/Editor/PA_VisualAccelerationBuilder.cs`.
- Created Project_PA-owned market visual prefab: `Assets/Prefabs/Market/PA_MarketStall_Hub.prefab`.
- Created Project_PA-owned market materials under `Assets/Materials/Market/`.
- Updated `Assets/Scenes/Prototype_FirstDay.unity` with child-only visual staging around the existing shop.
- Updated `Assets/Scripts/PA_RuntimeSceneBinder.cs` with a narrow NavMesh/NPC-agent startup safety fix.
- Created `README.md` with source/build run instructions and demo route notes.
- Generated Windows build output under `Builds/Windows/`.

Scene additions made by the visual acceleration helper:

- `PA_MarketStall_Hub_Visual` under the demo shop.
- Wood frame, awning, product shelves, product crates, lantern accents, supply/process/price labels.
- Slot markers and price tag markers aligned around existing `ShopSlot` positions.
- `PA_DemoRoute_VisualMarkers` for talk -> stock -> price -> purchase -> revenue staging.
- NPC role badges for producer/specialist/guide/consumer readability.
- `PA_ScreenshotCameraMarker_MarketHub` as a non-gameplay presentation marker.

What was intentionally not changed:

- No Project_D files were copied or modified.
- No external packages were imported.
- No broad merge was performed.
- `Shop`, `ShopSlot`, `ShopPriceUI`, inventory, hotbar, economy, purchase evaluator, NPC purchase logic, save, tier, audit, hiring, and friendship gameplay code were not rewritten.
- `PlayableDayScenarioController` flow was not changed.
- No source or executable submission zip package was created.

Verification performed:

- Unity 6000.3.2f1 batch compile completed with exit code 0 after the changes.
- `PA_VisualAccelerationBuilder.VerifyPrototypeFirstDayStaging` passed.
- Verification counts: shops=2, shopSlots=8, hubVisuals=1, routeMarkers=1, roleBadges=8, screenshotMarkers=1, buildSettingsOk=True.
- Automated Play Mode smoke test now passes after fixing domain-reload-safe test state.
- Play Smoke counts after NavMesh repair: players=1, shops=2, shopSlots=8, economyServices=1, shopPriceUI=1, npcs=8, npcAgents=8/8, npcAgentsOnMesh=8, npcNearNavMesh=8.
- Fixed Windows-player NavMesh startup issue by rebaking the scene NavMesh, snapping 8 NPCs to the NavMesh, saving NPC NavMeshAgents disabled in the scene, and letting `PA_RuntimeSceneBinder` enable them after NavMeshSurface data is active.
- Windows build succeeded at `Builds/Windows/Project_PA.exe`.
- Windows player smoke reached runtime startup with `surfaces=1, agents=8/8, onMesh=8` and no `Failed to create agent because there is no valid NavMesh` messages.
- Logs show `PA_RuntimeSceneBinder` and `Shop.Start()` running in Play Mode.
- Manual Unity Editor review is still recommended for player feel, NPC movement, and the full stock -> price -> purchase -> money update route.

Known warnings:

- Unity logs include a non-blocking licensing token update warning.
- Existing editor warnings remain, including `PA_ErrorTracker._autoScrollNew` unused and a Unity source-generator warning.
- NavMesh repair logs include non-blocking TextMeshPro mesh skip messages during bake; NavMesh samples and NPC agent checks passed.
- No new C# compiler errors were found in batch compilation.

## 2026-06-19 Completion Loop Update

Design guardrail:

- This pass followed `PROJECT_PA_DESIGN_INTENT.md`.
- Project_PA remains a reverse supply-chain management simulation: the player manages stock, price, feedback, revenue, and growth while NPCs act as producers, consumers, specialists, and economic agents.
- ReferencePrototype/Project_D was not modified and no assets were copied.

Planning files created:

- `PROJECT_PA_COMPLETION_PLAN.md`
- `PROJECT_PA_RELEASE_BACKLOG.md`
- `PROJECT_PA_CURRENT_MILESTONE.md`

Gameplay clarity work implemented:

- CL-001 Management Loop Objective Rewrite: `PlayableDayScenarioController` now frames Day 1 objectives around NPC supply, shop stocking, pricing, customer reaction, revenue, audit/tier review, and saving.
- CL-002 NPC Purchase Feedback: `NpcController` now turns purchase evaluator outcomes into short buy/reject feedback messages and records recent feedback for the scenario summary.
- CL-003 Day Summary Improvement: Day 1 summary now includes revenue, money delta, sales count, relationship points, recent customer feedback, and a next management action.
- CL-004 Tier 0 Goal Clarity: `MoneyHUD`, `AuditResultUI`, and the Day 1 summary now read `TierDefinition` data to show next-tier revenue/reputation/manual approval requirements.

Files changed in this pass:

- `Assets/Scripts/UI/PlayableDayScenarioController.cs`
- `Assets/Scripts/NpcController.cs`
- `Assets/Scripts/UI/MoneyHUD.cs`
- `Assets/Scripts/AuditResultUI.cs`
- Planning/status docs listed in this file and TODO/session report.

Verification performed after CL-004:

- Unity batch compile completed with exit code 0.
- Automated Play Mode smoke passed.
- Play Smoke counts after CL-004: players=1, shops=2, shopSlots=8, economyServices=1, shopPriceUI=1, npcs=8, npcAgents=8/8, npcAgentsOnMesh=8, npcNearNavMesh=8.
- Windows build completed with `Build Finished, Result: Success`.
- Windows player smoke launched the built executable and reached runtime startup with `surfaces=1, agents=8/8, onMesh=8`.
- The player smoke log did not show the earlier NavMeshAgent startup failure.

Remaining manual validation:

- Manually complete the full route in Unity Editor and the Windows executable: move -> talk -> stock -> set price -> NPC buy/reject -> money update -> day summary.
- Check whether the expanded money/tier HUD is readable at 1920x1080 and does not cover important gameplay.
- Check whether NPC purchase bubbles are readable and not too long in the gameplay camera.

## 2026-06-19 Final Route Validation Update

Scope:

- Verified the CL-001 to CL-004 route after the management-clarity pass.
- Added an Editor-only validation tool, `Assets/Editor/PA_FinalDemoRouteValidator.cs`, to reproduce the route in Play Mode without changing runtime gameplay logic.
- Applied small readability fixes only:
  - Shortened NPC purchase/rejection feedback text.
  - Enlarged/wrapped `NpcBubbleUI`.
  - Removed emoji from `ShopPriceUI` reaction labels to avoid font fallback issues.

Automated route validation result:

- `Assets/Scenes/Prototype_FirstDay.unity` opened in Unity batchmode.
- Play Mode entered.
- Player movement components were present: `PlayerController`, `PlayerInputHandler`, and `CharacterController`.
- First NPC dialogue opened `DialogueUI`.
- A sellable item was stocked from hotbar into a `ShopSlot`.
- `ShopPriceUI` opened on the stocked slot.
- Price confirmation incremented `ShopPriceUI.ConfirmCount` and closed the UI.
- NPC purchase/rejection feedback text was generated and displayed in `NpcBubbleUI`.
- `ShopSlot.TryPurchaseByNpc` completed a sale.
- Money and cumulative revenue increased.
- `MoneyHUD` showed money, tier, and next-tier goal text.
- `AuditResultUI` showed next-tier goal text.
- `MoneyHUD` did not overlap the objective panel in the automated layout check.
- Day 1 summary appeared and included purchase feedback, next growth goal, and tier revenue progress.
- Market hub visual, route markers, NPC role badges, and screenshot marker were present.

Validation evidence:

- `Logs/Codex_FinalRoute_PlayValidation.log`
  - `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`
  - `PA Final Route Validation finished successfully.`
- `Logs/Codex_FinalRoute_WindowsBuild.log`
  - `Build Finished, Result: Success.`
- `Logs/Codex_FinalRoute_WindowsPlayerSmoke.log`
  - `NavMesh/NPC agents ready: surfaces=1, agents=8/8, onMesh=8`

Remaining manual review:

- Human visual review in Unity Editor is still recommended for actual camera composition, player feel, and subjective readability.
- The Windows executable was smoke-launched successfully, but a human should still complete the full route in the executable before final packaging.

## 2026-06-19 Final Presentation Review Update

Scope:

- Performed a final presentation-readability pass after automated route validation.
- Kept the project aligned with `PROJECT_PA_DESIGN_INTENT.md`: Project_PA remains a reverse supply-chain management simulation, not a simple shop clone.
- Did not modify Project_D, copy assets, import packages, push to GitHub, or rewrite core shop/economy/NPC/tier/save logic.

Minimal UI/readability changes:

- Added Editor-only reviewer `Assets/Editor/PA_FinalPresentationReviewer.cs`.
- Adjusted objective panel runtime placement so it no longer sits too low over the market hub.
- Enlarged Day 1 summary body area only for the summary state.
- Updated `AuditResultUI` next-tier text to wrap inside the phone audit card.
- Reworked `NpcBubbleUI` as a screen-space follower so purchase/rejection feedback remains readable in the demo camera.
- Hid active NPC bubbles when opening the smartphone or Day 1 summary so feedback does not cover management panels.

Final presentation validation evidence:

- `Logs/Codex_FinalPresentation_Review11.log`
  - `PA Final Presentation Review passed.`
  - Market hub, route markers, NPCs, shop stocking, `ShopPriceUI`, NPC feedback bubble, audit app, MoneyHUD/objective layout, and Day 1 summary body fit all passed.
- Captures generated under `Logs/FinalPresentation/20260619_232447/`:
  - `01_market_hub_objective.png`
  - `02_shop_price_ui.png`
  - `03_npc_feedback_bubble.png`
  - `04_audit_app_goal.png`
  - `05_day1_summary.png`
- `Logs/Codex_FinalPresentation_RouteValidation.log`
  - Final route validation passed again after the UI changes.
- `Logs/Codex_FinalPresentation_WindowsBuild.log`
  - `Build Finished, Result: Success.`
- `Logs/Codex_FinalPresentation_WindowsPlayerSmoke.log`
  - Built exe launched and reached `NavMesh/NPC agents ready: surfaces=1, agents=8/8, onMesh=8`.

Current validation status:

- Unity compile/build pipeline has no blocking C# errors.
- Automated Play Mode route passed after final UI/readability fixes.
- Final presentation screenshots confirm the main HUD, market hub, price UI, NPC purchase feedback, audit app, and Day 1 summary are readable.
- Windows executable was rebuilt and smoke-launched successfully.
- A real human should still do one final hands-on pass for subjective player feel and route pacing before packaging.

## 2026-06-19 Submission Packaging Readiness Update

Scope:

- Reflected the Windows executable final-route status for submission readiness.
- No gameplay code, scene, Project_D asset, external package, Git commit, or Git push was performed in this packaging-readiness pass.

Windows exe manual route result:

- On 2026-06-19 this result was still pending because the human-at-keyboard executable route result had not been supplied.
- Superseded by the 2026-06-20 Final Submission Packaging Update below: the human executable route is now recorded as passed.
- Automated evidence remains strong: final route validation, final presentation review, Windows build, and Windows smoke launch all passed.
- Final package creation is no longer blocked by this item.

Packaging plan, ready after manual exe route pass:

- Source package target name: `Project_PA_Source_20260619.zip`.
- Executable package target name: `Project_PA_Windows_20260619.zip`.
- Optional report/presentation attachment should be chosen separately from `Docs/`.

Source package include list:

- `Assets/`
- `Packages/`
- `ProjectSettings/`
- `Docs/`
- Root project documents: `PROJECT_PA_*.md`, `README.md`, `.gitignore`
- Solution/project files if required by the course review workflow: `Project_PA.slnx`, `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`

Source package exclude list:

- `.git/`
- `Library/`
- `Temp/`
- `Logs/`
- `Builds/`
- `UserSettings/`
- `obj/`, `.vs/`, cache/generated folders, and any local IDE/cache output

Executable package include list:

- `Builds/Windows/Project_PA.exe`
- `Builds/Windows/Project_PA_Data/`
- `Builds/Windows/UnityPlayer.dll`
- `Builds/Windows/UnityCrashHandler64.exe`
- `Builds/Windows/DirectML.dll`
- `Builds/Windows/D3D12/`
- `Builds/Windows/MonoBleedingEdge/`
- `README.md` or a copied run-instructions text file

Executable package exclude list:

- `Builds/Windows/Project_PA_BurstDebugInformation_DoNotShip/`
- `Logs/`
- Any source/cache folder not required to run the executable

## 2026-06-20 Final Submission Packaging Update

Scope:

- Recorded the user-confirmed human Windows executable final-route result as passed.
- Created separate source and executable submission packages inside `SubmissionPackages/`.
- No Project_D files were modified or copied.
- No external packages were imported.
- No gameplay logic, scene layout, or UI behavior was changed during this packaging pass.
- No Git commit or push was performed.

Windows exe human route result:

- Passed by human execution of `Builds/Windows/Project_PA.exe`.
- Confirmed route: move -> talk -> stock item -> set price -> NPC buy/reject feedback -> money change -> audit app -> Day 1 summary.

Created packages:

- `SubmissionPackages/Project_PA_Source_20260620.zip`
  - Size: about 359.17 MiB
  - Entries: 1,369
- `SubmissionPackages/Project_PA_Windows_20260620.zip`
  - Size: about 84.25 MiB
  - Entries: 183

Package verification:

- Source package was created from `Assets/`, `Packages/`, `ProjectSettings/`, `Docs/`, root `PROJECT_PA_*.md`, `README.md`, `.gitignore`, and solution/project files.
- Source package excludes `.git/`, `Library/`, `Temp/`, `Logs/`, `Builds/`, `UserSettings/`, `obj/`, `.vs/`, `SubmissionPackages/`, `GeneratedAssets_deleted/`, and generated cache folders.
- Executable package includes the runnable Windows build files and `README.md`.
- Executable package excludes `Project_PA_BurstDebugInformation_DoNotShip/`, logs, source folders, cache folders, and generated debug/cache output.
- Both zip files were opened after creation and scanned for forbidden entries.

## Known Scenes

Project scenes found under `Assets/`:

- `Assets/Scenes/MainGame.unity`
- `Assets/Scenes/Prototype_FirstDay.unity`
- `Assets/Art/Free RPG Icons/Demo.unity`
- `Assets/Jinxish/Drag & Drop Inventory & Hotbar Framework/Scenes/Really Simple Demo Scene.unity`
- `Assets/_Recovery/0.unity`

Main project scenes:

- `Prototype_FirstDay.unity` appears to be the best current demo candidate. It includes prototype/demo flow names and references to systems such as `PlayableDayScenarioController`, `PA_RuntimeUI`, `[Services]`, `ShopSlot`, `SmartphoneUI`, `MoneyHUD`, and core services.
- `MainGame.unity` appears to be a fuller scene with player, NPCs, shop slots, workbenches, runtime UI, map root, and services.

Build settings:

- `ProjectSettings/EditorBuildSettings.asset` now enables:
  - `Assets/Scenes/Prototype_FirstDay.unity`
- Previous blocker fixed: missing `Assets/Scenes/SampleScene.unity` was removed from Build Settings.
- `MainGame.unity` exists but is not currently listed in build settings.

## Known Scripts And Systems

Runtime scripts under `Assets/Scripts/` include these major systems:

- Core/game flow: `GameManager`, `PlayerController`, `PlayerInputHandler`, `PlayerInteraction`, `CameraController`, `PauseManager`, `ScreenFader`.
- Inventory/hotbar/items: `Inventory`, `Hotbar`, `InventorySlot`, `InventoryUI`, `InventorySlotUI`, `Item`, `ItemInstance`, `ItemRegistry`, `EquipmentSystem`, `PickupItem`, `ItemPickupHandler`, `ItemTooltip`.
- Economy/shop: `EconomyService`, `Shop`, `ShopSlot`, `ShopPriceUI`, `PurchaseEvaluator`, `SalesLogManager`, `SaleRecord`.
- Crafting/production: `CraftingService`, `CraftingUI`, `RecipeData`, `Workbench`, `ProductionData`, `ProducerNpcController`, `SpecialistNpcController`.
- NPC/social: `NpcController`, `NpcDialogue`, `NpcProfile`, `NpcDailySchedule`, `NpcScheduleController`, `NpcSpecialty`, `DialogueData`, `DialogueService`, `FriendshipService`, `FriendshipUI`, `HiringService`, `HiringUI`, `NpcCandidateData`, `NpcBubbleUI`.
- World/building/save: `BuildManager`, `BuildingData`, `BuildingEntrance`, `BuildingRegistry`, `GridService`, `StorageBox`, `StorageUI`, `SaveData`, `SaveManager`, `LocalJsonSaveRepository`.
- Time/progression: `GameClock`, `DayNightVisual`, `TierService`, `TierDefinition`, `SeasonModifier`, `AuditService`, `AuditResultUI`.
- Demo/UI flow: `PlayableDayScenarioController`, `SmartphoneUI`, `ClockHUD`, `MoneyHUD`, `InteractPromptUI`, `FeedUI`, `SettingsUI`, `PrototypeWorldLabel`, `PA_RuntimeSceneBinder`.

Editor scripts under `Assets/Editor/` include automation and validation helpers:

- `PA_PlayableDayBuilder`
- `PA_SceneAutoBuilder`
- `PA_ContentAutoIntegrator`
- `PA_DataBootstrapper`
- `PA_DataCreator`
- `PA_DevConsole`
- `PA_FeatureChecker`
- `PA_MapLayoutBuilder`
- `PA_SceneValidator`
- `PA_SystemHub`
- `PA_UIBuilder`
- `PA_Week11MilestoneValidator`
- Other repair/import/check utilities.

## Known Prefabs And Data

Project prefabs found under `Assets/Prefabs/` include:

- Building/shop/storage/world prefabs such as `Building_Chest`, `Building_Shop`, `Farmland`, `Crop_Corn`, `UI_Slot`, `Recipe_Slot_Prefab`, trees/rocks/furniture, and selection/ghost-floor prefabs.
- Building prefabs `B01_MarketStall` through `B12_TradePort` under `Assets/Prefabs/Buildings/`.
- Additional third-party/example prefabs under the Jinxish drag-and-drop inventory framework.

Scriptable/data assets exist under `Assets/Resources/` and `Assets/ScriptableObjects/`, including:

- Items
- Recipes
- Tiers
- NPC profiles
- NPC candidates
- Dialogue data
- Production data
- Building data
- Schedules

## UI And Menu Status

- There is no obvious dedicated title/menu scene found under `Assets/Scenes/`.
- UI appears to be mostly runtime-driven and/or embedded in the gameplay scenes.
- `PlayableDayScenarioController` appears to create an onboarding/demo flow UI with player name/map selection and stage objectives.
- `SmartphoneUI` appears to provide in-game app panels such as audit, hiring, feed, and settings.
- HUD/UI systems include money, clock, dialogue, interaction prompt, crafting, shop pricing, friendship, and pause/settings.

## Existing Builds And Submission Assets

- Windows build output now exists at `Builds/Windows/Project_PA.exe`.
- The full `Builds/Windows/` folder is required to run the executable.
- Submission zip packages now exist under `SubmissionPackages/`.
- Source package: `SubmissionPackages/Project_PA_Source_20260620.zip`.
- Windows executable package: `SubmissionPackages/Project_PA_Windows_20260620.zip`.
- `Builds/Windows/Project_PA_BurstDebugInformation_DoNotShip/` is generated debug information and should normally be excluded from the final executable package unless specifically required.
- Existing documentation/materials include multiple files under `Docs/`, including planning/design docs, development log, roadmap/spec docs, guides, a Week 11 presentation, and proposal/report files.
- A root `.tmp_template_proposal.pptx` exists and should be reviewed before final packaging because it looks temporary.

## Gameplay Status Review

What appears playable now:

- A first-day guided prototype loop likely exists in `Prototype_FirstDay.unity`.
- Player movement, interaction, shop slot stocking, price setting, NPC shopping/sales, money display, dialogue, and smartphone/hiring/settings UI appear implemented.
- `PA_RuntimeSceneBinder` adds or repairs runtime services/UI after scene load, which may make scenes more resilient.

Likely demo flow:

1. Open `Prototype_FirstDay.unity`.
2. Enter Play Mode.
3. Follow the playable-day objective UI.
4. Interact with NPC/dialogue, stock a shop slot, set a price, observe NPC shopping/sales, and review UI panels.

Known uncertainty:

- Initial inventory did not include manual Unity Editor Play Mode.
- Automated batch Play Smoke has since passed, but manual Unity Editor full-route verification is still needed.
- Compilation was verified by Unity batchmode, but the human-facing Editor Console still needs a final manual check.
- Scene files are partly binary/serialized, so the review is based on file inventory, build settings, script content, and searchable scene strings.
- Logs did not show current C# compiler errors, but logs are not a substitute for a final manual Unity review.

## Current Blockers

1. Need manual Unity Editor review for player feel and NPC movement.
2. Need manually complete the full stock -> price -> NPC purchase -> money update route.
3. Need UI readability check at 1920x1080 in Play Mode.
4. Need final report/presentation selection from existing `Docs/` files.
5. Optional demo video not found yet.

## Submission Readiness

- Unity Editor Play Mode test: Batch Play Smoke passed with NPC NavMesh validation; manual full-route review still recommended.
- Unity automated route test: Passed through dialogue, stocking, price confirmation, purchase, money update, HUD, audit text, and summary checks.
- Windows executable build: Build succeeded, smoke-launched, and the final executable route was confirmed by human review.
- Source code zip: Created at `SubmissionPackages/Project_PA_Source_20260620.zip`.
- Windows executable zip: Created at `SubmissionPackages/Project_PA_Windows_20260620.zip`.
- Final report/presentation: Materials exist under `Docs/`, but final attachment choice needs review.
- Optional demo video: Not found.

## Recommended Next Tasks

1. Open Unity 6000.3.2f1 and load `Prototype_FirstDay.unity`.
2. Check Console for compile errors before entering Play Mode.
3. Enter Play Mode and manually complete the first-day demo route.
4. Keep `SubmissionPackages/Project_PA_Windows_20260620.zip` and `SubmissionPackages/Project_PA_Source_20260620.zip` as the current final package candidates.
5. Review the new market hub visuals from the gameplay camera.
6. Check UI readability at 1920x1080.
7. Choose or update final report/presentation material from `Docs/`.
8. Optionally record a short demo video after the executable route is validated.

## Submission Checklist

- [x] Project opens in Unity 6000.3.2f1 via batch validation.
- [x] Console has no C# compile errors in batch validation.
- [x] `Prototype_FirstDay.unity` enters Play Mode.
- [x] Main demo loop passes automated route validation.
- [x] Build Settings no longer reference missing `SampleScene.unity`.
- [x] Final start scene is confirmed as `Assets/Scenes/Prototype_FirstDay.unity`.
- [x] Windows executable build succeeds.
- [x] Source/executable package include-exclude plan is defined.
- [x] Executable package is created after human exe route pass.
- [x] Source package is created without cache/generated folders after human exe route pass.
- [x] README/run instructions are included.
- [ ] Final report or presentation is selected/attached.
- [ ] Optional demo video is recorded and attached.

## 2026-06-20 Full Game Development Mode Update

Project_PA is now being tracked as a long-term Project_PA 1.0 development project, not only as a submission prototype. The existing submission build and packages remain preserved as a stable snapshot, while new development targets a cozy reverse supply-chain management life-sim.

New planning documents:

- `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md`
- `PROJECT_PA_FULL_GAME_BACKLOG.md`

Safety and baseline checks:

- Confirmed working root: `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Confirmed Git root: `C:/Users/sdjsd/Desktop/Unity/Project_PA`.
- Confirmed Unity folders: `Assets/`, `Packages/`, `ProjectSettings/`.
- Confirmed main scene exists: `Assets/Scenes/Prototype_FirstDay.unity`.
- Created scene backup before long-play work: `Assets/Scenes/_Backups/Prototype_FirstDay_before_fullgame_longplay_20260620_155934.unity`.
- Unity batch compile after long-play code changes exited successfully.
- Compile log note: Unity source-generator warning remains non-blocking; no C# compile failure was reported.
- Long-play Play Mode validator could not run in batch because another Unity instance already had this project open. The open Editor was not force-closed.

Implemented first full-game sprint foundation:

- Added `LongPlayProgressionController` as a sidecar runtime service.
- Added Day 1-7 long-play objective plan data.
- Added Day 2-7 daily NPC producer buy-in deliveries using existing `EconomyService` and `Inventory` paths.
- Added runtime long-play HUD text for day goal, daily/total revenue target, and producer delivery result.
- Added save schema v7 fields for long-play delivery/day baseline state.
- Updated `SaveManager` to save/load/migrate the v7 long-play fields.
- Updated `PA_RuntimeSceneBinder` to attach `LongPlayProgressionController` automatically.
- Added `PA_LongPlayProgressionValidator` for Day 2-7 progression smoke validation.

Current implementation reclassification:

- Strong foundation: Day 1 route, shop stocking, price UI, purchase evaluation, money/revenue, tier/audit UI, inventory/hotbar, save/load, NPC/hiring/friendship/crafting/building extension points.
- Demo-bound: `PlayableDayScenarioController` remains Day 1/tutorial oriented and should not become the whole long-term progression system.
- Long-play blockers being reduced: Day 2-7 now has a first-pass operations layer and producer delivery loop, but it still needs manual Play Mode verification and richer processing/hiring/town growth integration.
- Expandable: `GameClock.OnNewDay`, `ProductionData`, `EconomyService`, `ShopSlot`, `TierService`, `AuditService`, and `SaveManager` can support fuller progression if changes stay incremental.
- Risky systems: avoid broad rewrites of `Shop`, `ShopSlot`, `ShopPriceUI`, `Inventory`, `Hotbar`, `EconomyService`, `PurchaseEvaluator`, `NpcController`, `ProducerNpcController`, `SaveManager`, `TierService`, and `AuditService`.

Next validation needed:

- Close or reuse the currently open Unity Editor, then run the long-play validator:
  `PA_LongPlayProgressionValidator.RunLongPlayProgressionValidation`
- Manually confirm in Play Mode that Day 1 route is still intact.
- Advance or simulate Day 2-7 and verify producer deliveries, inventory growth, money spend/refund, day objective HUD, and save/load.

## 2026-06-20 Long-Play Validation Update

Scope:

- Verified FG-001/FG-002 first implementation pass after moving Project_PA into full-game development mode.
- Kept the work inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Did not modify Project_D, copy reference assets, import packages, push to GitHub, or rewrite core shop/economy/NPC/tier systems.

Minimal fixes made during validation:

- Fixed `SaveManager.SaveGameAsync` so `data.version = CurrentSaveVersion` is definitely executed before JSON serialization.
- Extended `PA_LongPlayProgressionValidator` to use a project-local validation save repository at `Logs/LongPlayValidationSaves/`.
- Added Day 3 save/load validation to confirm long-play day, money, delivered inventory, long-play supply day, day-start revenue, day-start money, and HUD text restore correctly.

Validation performed:

- Unity batch compile log: `Logs/Codex_LongPlay_Compile.log`
  - Result: passed.
  - No `error CS`, `Script compilation failed`, or `Compiler Error` entries found.
- Day 1 route validation log: `Logs/Codex_LongPlay_Day1RouteValidation2.log`
  - Result: passed.
  - Evidence: `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`.
  - Evidence: `PA Final Route Validation finished successfully.`
- Long-play progression validation log: `Logs/Codex_LongPlay_ProgressionValidation.log`
  - Result: passed.
  - Day 2-7 producer delivery simulations all returned true.
  - Day 2-7 producer deliveries increased sellable inventory.
  - Day 3 save/load restored current day, money, long-play supply day, day-start revenue, day-start money, delivered inventory, and HUD.
  - Final evidence: `PA Long Play Validation passed. sellableInventory=38, money=4633G`.
  - Final evidence: `PA Long Play Validation finished successfully.`

Current FG-001/FG-002 status:

- Day 1 onboarding route is preserved by automated validation.
- Day 2-7 first-week long-play loop is validated in Play Mode automation.
- Day 3 save/load persistence for long-play state is validated through a project-local save repository.
- Remaining work is now design depth, not blocker repair: richer producer presentation, processing-chain decisions, customer segmentation, and manual player-feel review.

## 2026-06-20 FG-004 Processing Chain First Pass

Scope:

- Started the next automatic sprint after FG-001/FG-002 validation passed.
- Goal was to make raw producer goods vs processed goods visible as a management decision without rewriting crafting.
- Kept existing `CraftingService`, `RecipeData`, `Workbench`, and `CraftingUI` behavior intact.

Implemented:

- Added `ProcessingOpportunityController`.
  - Loads existing `Resources/Recipes` data.
  - Calculates input base value, expected processed output value, and estimated margin.
  - Detects whether the player currently owns the required inputs.
  - Shows a small Day 4+ processing focus panel.
- Updated `PA_RuntimeSceneBinder` to attach `ProcessingOpportunityController`.
- Added `PA_ProcessingChainValidator`.
  - Selects an existing recipe.
  - Seeds required inputs at runtime.
  - Confirms the advisor finds a ready processing chain.
  - Crafts through existing `CraftingService.TryCraft`.
  - Confirms processed output is created.

Validation:

- Compile log: `Logs/Codex_ProcessingChain_Compile.log`
  - Result: passed.
  - Only the existing non-blocking Unity source-generator warning appeared.
- Processing chain validation log: `Logs/Codex_ProcessingChain_Validation.log`
  - Result: passed.
  - Evidence: `PA Processing Chain Validation passed. recipe=BreadLoaf, margin=3G`.
  - Evidence: `PA Processing Chain Validation finished successfully.`
- Long-play regression log after adding the processing advisor: `Logs/Codex_ProcessingChain_LongPlayRegression.log`
  - Result: passed.
  - Evidence: `PA Long Play Validation passed. sellableInventory=38, money=4633G`.

Current FG-004 status:

- First-pass processing opportunity layer is implemented and validated.
- Processing is now visible as a management signal starting around Day 4.
- Future work should deepen this into facility availability, customer demand for processed goods, and better recipe balancing.

## 2026-06-20 FG-003 Customer Demand Insight First Pass

Scope:

- Continued automatic development after FG-004 first pass.
- Goal was to expose customer demand signals without changing purchase probability math.
- Kept `PurchaseEvaluator` behavior intact.

Implemented:

- Added `CustomerDemandInsightController`.
  - Observes `PurchaseEvaluator.Result`.
  - Tracks category-level evaluations, buys, average interest, and latest customer signal.
  - Shows a Day 3+ demand signal panel.
- Updated `PA_RuntimeSceneBinder` to attach `CustomerDemandInsightController`.
- Added a narrow observer hook in `NpcController` after purchase evaluation.
  - The hook records the result for UI insight.
  - It does not change `willBuy`, probability, pricing, stock, sale, money, or NPC FSM behavior.
- Added `PA_CustomerDemandInsightValidator`.

Validation:

- Compile log: `Logs/Codex_CustomerDemand_Compile.log`
  - Result: passed.
  - Only the existing non-blocking Unity source-generator warning appeared.
- Customer demand validation log: `Logs/Codex_CustomerDemand_Validation.log`
  - Result: passed.
  - Evidence: `PA Customer Demand Insight Validation passed. item=BreadLoaf, category=Processed`.
- Day 1 regression log: `Logs/Codex_CustomerDemand_Day1Regression.log`
  - Result: passed.
  - Evidence: `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`.
- Long-play regression log: `Logs/Codex_CustomerDemand_LongPlayRegression.log`
  - Result: passed.
  - Evidence: `PA Long Play Validation passed. sellableInventory=38, money=4633G`.

Current FG-003 status:

- First-pass customer demand insight layer is implemented and validated.
- The player can now receive management-facing demand signals from customer evaluations.
- Future work should connect this to weekly demand trends, customer segments, pricing advice, and audit/reputation goals.

## 2026-06-21 Creative North Star Lock

Scope:

- Documentation-only direction reset for long-term full-game development.
- No gameplay code, scene, UI, prefab, material, build, package, or Project_D changes were made in this pass.

Created:

- `PROJECT_PA_CREATIVE_NORTH_STAR.md`

Updated planning direction:

- Project_PA is now framed as a cozy 3D life and shop management simulation.
- The player should live in the village by day, prepare goods through activities/relationships, run a personal shop at night, and grow the village through what they sell.
- Reverse supply-chain systems remain the economic backbone, but should support the cozy day-to-night loop instead of replacing it.
- NPC producer/specialist systems should gradually reduce repetitive labor and support growth.
- The shop/stall is now both the night shop hub and the visible interface where daytime goods, customer feedback, settlement, and village-change signals meet.
- Project_D / ReferencePrototype and VisualTargets remain reference-only material and are not merge/copy sources.

New Milestone 1 target:

- Milestone 1 is now `Cozy Day-To-Night Shop Loop`.
- Required first implementation targets: two daytime stock-preparation activities, shop open/close state, product display/pricing, at least two customer types, buy/reject feedback, daily settlement, next-day change/unlock, and one product category affecting village change.

Recommended next implementation focus:

1. CN-001 Day/night phase display and shop open/close state.
2. CN-002 Two Project_PA-owned daytime stock sources or MVP equivalents.
3. CN-003 Product-category village-change signal in settlement.
4. Customer type presentation pass.
5. Regression validation for Day 1 route and long-play loop.

## 2026-06-21 Milestone 1 First Implementation Update

Scope:

- Began `Milestone 1 - Cozy Day-To-Night Shop Loop` implementation.
- Kept the work inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Did not modify Project_D, copy reference assets, import packages, push to GitHub, or rewrite core shop/economy/NPC/save logic.

Implemented:

- Added `DayNightShopLoopController` as a runtime sidecar service.
  - Tracks `DayPreparation`, `ShopOpen`, and `Settlement` phase state.
  - Exposes `CurrentPhase`, `IsShopOpen`, `CanCollectDayPrepStock`, and `LastActivityResult`.
  - Keeps Day 1 tutorial shop access open so the existing final route remains intact.
  - Creates a small top HUD showing day/time, phase, shop open/closed state, and latest stock-prep result.
- Added `DaytimeStockPrepPoint` as the first two daytime stock-preparation MVPs.
  - Runtime-created as Project_PA-owned `Garden Prep Basket` and `Producer Drop Box` stock sources.
  - Adds a valid sellable item through existing `Inventory.AddInstance`.
  - Each source can be collected once per day during `DayPreparation`.
- Added `VillageChangeSignalController` as the first visible product-category-to-village-change signal.
  - Reads recent `SalesLogManager` records only.
  - Shows which sold product category is shaping village direction.
  - Does not change purchase probability, tier, save, NPC behavior, money, or shop logic.
- Updated the Day 1 summary/settlement text to include a `Village direction` section from `VillageChangeSignalController`.
- Updated `PA_RuntimeSceneBinder` to attach the new sidecar services automatically.
- Added Editor validation tools:
  - `Assets/Editor/PA_DayNightShopLoopValidator.cs`
  - `Assets/Editor/PA_VillageChangeSignalValidator.cs`

Validation:

- Day/night loop validation passed:
  - Log: `Logs/Codex_DayNight_TwoPrep_Validation.log`
  - Evidence: `PA Day Night Shop Loop Validation passed. sellableInventory=4`
- Village change signal validation passed:
  - Log: `Logs/Codex_VillageSignal_Validation.log`
  - Evidence: `PA Village Change Signal Validation passed. summary=Processed: 2 sale(s), 76G influence`
- Day 1 route regression passed after the new Milestone 1 layer:
  - Log: `Logs/Codex_SettlementVillage_Day1Validation.log`
  - Evidence: `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`
  - Evidence: `PA Final Route Check OK: Day 1 summary includes village direction section`
- Day 2-7 long-play regression passed after the new Milestone 1 layer:
  - Log: `Logs/Codex_SettlementVillage_LongPlayRegression.log`
  - Evidence: `PA Long Play Validation passed. sellableInventory=38, money=4633G`

Current Milestone 1 status:

- Day/night phase state: first pass complete.
- Shop open/close state: first pass complete and Day 1 route preserved.
- Daytime stock prep: two MVP activities complete.
- Product-category village-change signal: first pass complete as read-only UI.
- Daily settlement integration: first pass complete through Day 1 summary text.
- Manual Game-view readability review: still recommended for the new top/right HUD panels.

## 2026-06-25 Unity Editor Crash Repair

Scope:

- Investigated a Unity Editor hard crash that opened Bug Reporter.
- Confirmed this was not a normal C# Console compile error.
- Kept work inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`.

Finding:

- Latest `Editor.log` shows `Unrecoverable D3D12 device error`.
- Key evidence:
  - `d3d12: swapchain present failed (887a0005)`
  - `d3d12: Device removed reason (887a0006)`
  - native stack includes `D3D12SwapChain::Present`
  - log says GPU local/non-local memory was not exhausted
- Cause classification: D3D12/GPU driver/device-removed path, not Project_PA C# compile, NavMesh, save, or UI managed exception.

Repair:

- Updated `ProjectSettings/ProjectSettings.asset`.
- Standalone graphics API is now fixed to Direct3D 11:
  - `m_BuildTarget: Standalone`
  - `m_APIs: 02000000`
  - `m_Automatic: 0`

Verification:

- `Logs/Codex_CrashRepair_D3D11_Load.log`
  - `Forcing GfxDevice: Direct3D 11`
  - `Tundra build success`
- `Logs/Codex_CrashRepair_ProjectGraphicsAPI_Load.log`
  - Loaded without `-force-d3d11`
  - `Version: Direct3D 11.0`
  - `Tundra build success`
- `Logs/Codex_CrashRepair_FinalRoute_D3D11.log`
  - `Version: Direct3D 11.0`
  - `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`

Report:

- `PROJECT_PA_CRASH_REPORT_20260625.md`

## 2026-06-22 SPY-002 Customer Type / Preference Presentation

Scope:

- Implemented Milestone 1's remaining customer-type requirement: make NPCs read as residents with their own tastes, and explain why each buys or rejects.
- Kept the work inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Did not modify Project_D, copy reference assets, import packages, commit, or push.
- Did not change `PurchaseEvaluator`, `Shop`, `ShopSlot`, `ShopPriceUI`, `EconomyService`, NPC FSM, or the Save schema.

Implemented (read-only presentation layer):

- `Assets/Scripts/UI/CustomerPreferencePresentationController.cs`
  - Top-right "관심 손님 성향" panel.
  - Lists NPCs currently moving to / browsing the shop with a short preference hint.
  - Hint is derived only from traits that actually differ across the eight profiles: `traitSN` (category preference), `traitTF` (buy style), `traitEI` (eagerness).
  - `priceSensitivity`/`utilityConsumption`/`luxuryConsumption` are all 1.0, so no per-NPC price label is fabricated.
- `Assets/Scripts/UI/PurchaseFeedbackPresentationController.cs`
  - Bottom-right "손님 반응" feed.
  - Turns each `PurchaseEvaluator.Result` into a cozy buy/reject reason with no probabilities/debug fields.
  - Adds a "마을 변화" tie line connecting the sold category (or a display/price nudge on reject) to village direction.
- `Assets/Scripts/NpcController.cs`
  - One read-only hook in `EvaluateCurrentSlot`, mirroring the existing `CustomerDemandInsightController` hook. No effect on `willBuy`, sale, money, inventory, or FSM.
- `Assets/Scripts/PA_RuntimeSceneBinder.cs`
  - Registers both controllers automatically.
- `Assets/Editor/PA_CustomerPresentationValidator.cs`
  - New Editor-only Play Mode validator for SPY-002.

Validation:

- Batch compile: `Logs/Codex_SPY002_Compile.log` — no `error CS`.
- `PA_CustomerPresentationValidator`: `Logs/Codex_SPY002_PresentationValidation.log` — passed.
  - `utilHint='실용재(식료품·도구) 선호 · 감성 구매형'`
  - `luxHint='장식·고급품 선호 · 신중형'`
  - cozy buy/reject reasons generated; no `%` in the feed panel.
- `PA_FinalDemoRouteValidator`: `Logs/Codex_SPY002_FinalRouteRegression.log` — passed (`stocked=BreadLoaf, paid=30G`; head bubble text unchanged: `Lumberjack_01: 가격 적정, 구매 (73%)`).
- `PA_DayNightShopLoopValidator`: `Logs/Codex_SPY002_DayNightRegression.log` — passed.
- `PA_VillageChangeSignalValidator`: `Logs/Codex_SPY002_VillageRegression.log` — passed.
- `PA_LongPlayProgressionValidator`: `Logs/Codex_SPY002_LongPlayRegression.log` — passed.

Still required:

- Manual 1920x1080 Game-view readability review of the two new panels (checklist in `Docs/CustomerPresentation/README.md`). Automated validation only checked data/string logic, not on-screen layout.

## 2026-06-22 SPY-003 Per-Resident Consumption Data

Scope:

- Closed the data-honesty gap left by SPY-002: `priceSensitivity`, `utilityConsumption`, and `luxuryConsumption` were all 1.0 on every profile, so the "가격에 민감/관대" preference hint could never appear from real data.
- Changed only ScriptableObject asset values, not any code logic. `PurchaseEvaluator` math is unchanged.
- Did not modify Project_D, import packages, commit, or push.

Data change (`Assets/Resources/NPCs/Profile_*.asset`, eight files):

- Varied per resident archetype within 0.6 ~ 1.5:
  - Price-sensitive: Farmer (1.35), Miner (1.45).
  - Price-tolerant: Tailor (0.65).
  - Neutral: Blacksmith (0.9), Fisher (0.9), Chef (1.1), Carpenter (1.0), Lumberjack (1.15).
  - `utilityConsumption`/`luxuryConsumption` set to reflect each job (e.g., Chef utility 1.5, Tailor luxury 1.4, Miner luxury 0.6).
- `CustomerPreferencePresentationController.DescribePreference` already reads `priceSensitivity` (>=1.25 민감, <=0.75 관대), so the hint became data-driven with no code change to the controller.

Validator update:

- `Assets/Editor/PA_CustomerPresentationValidator.cs` now also asserts that Miner (1.45) shows "가격에 민감", Tailor (0.65) shows "가격에 관대", and a price-neutral profile (Blacksmith) falls back to a personality style.

Why this is safe (no regression):

- `priceSensitivity` only affects purchase probability when display price exceeds the ideal price (ratio > 1). FinalDemoRoute stocks at base price (ratio <= 1), so it is unaffected.
- `utilityConsumption`/`luxuryConsumption` only scale Utility/Luxury category bonuses. FinalDemoRoute sells BreadLoaf (Processed), whose bonus does not use those multipliers.
- LongPlay money comes from deterministic producer buy-ins, not live NPC sales.

Validation (all passed):

- Batch compile: `Logs/Codex_SPY003_Compile.log` — no `error CS`.
- `PA_CustomerPresentationValidator`: `Logs/Codex_SPY003_PresentationValidation.log` — `장식·고급품 선호 · 가격에 민감` (Miner), `실용재(식료품·도구) 선호 · 가격에 관대` (Tailor).
- `PA_FinalDemoRouteValidator`: `Logs/Codex_SPY003_FinalRouteRegression.log` — `stocked=BreadLoaf, paid=30G`.
- `PA_DayNightShopLoopValidator`: `Logs/Codex_SPY003_DayNightRegression.log`.
- `PA_VillageChangeSignalValidator`: `Logs/Codex_SPY003_VillageRegression.log`.
- `PA_LongPlayProgressionValidator`: `Logs/Codex_SPY003_LongPlayRegression.log` — `money=4633G` unchanged.

Still required:

- Manual 1920x1080 Game-view review still pending (data/string logic only is automated).

## 2026-06-22 SPY-002 Panel Layout Validation

Scope:

- Replaced the "human must check panel overlap" gap with a coordinate-based automated layout validator, plus a 1920x1080 Game-view screenshot for human reference.
- Presentation/QA only: no gameplay, economy, or NPC logic changed.

Added:

- `Assets/Editor/PA_CustomerPanelLayoutValidator.cs`
  - Enters Play Mode at 1920x1080, dismisses the onboarding modal for a clean shot.
  - Computes screen rects (`RectTransform.GetWorldCorners`) for the two SPY-002 panels and the existing HUD.
  - Asserts: both new panels are within screen bounds; `관심 손님 성향` (top-right) does not overlap MoneyHUD/Demand/Village; `손님 반응` (bottom-right) does not overlap the top-right stack; the two new panels do not overlap each other; `손님 반응` does not overlap the hotbar (computed from active `InventorySlotUI` rects).
  - Captures `Logs/CustomerPanelReview/<timestamp>/customer_panels_1920x1080.png`.
  - All panels share the same `ScaleWithScreenSize(1920x1080, match 0.5)` canvas, so overlap/containment is scale-invariant and valid at 1920x1080 even when batch resolution differs.

Result:

- Passed: the two new panels do not overlap each other, MoneyHUD, the Demand panel, or the Village panel, and stay within screen bounds.
- The 1920x1080 screenshot was generated and visually inspected: LongPlay (top-left), Day/Night (top-center), MoneyHUD + Village Direction (top-right), hotbar (bottom-center), and the `손님 반응` feed (bottom-right) all sit in distinct regions. The center "개척자 등록" dialog is the transient Day-1 onboarding modal, not a persistent HUD, and does not touch the corner panels.
- Logs: `Logs/Codex_PanelLayout_Validation.log`. Screenshot: `Logs/CustomerPanelReview/20260622_093117/customer_panels_1920x1080.png`.

Resolution (2026-06-22, completed):

- The improved validator (hotbar region from active `InventorySlotUI`, onboarding modal dismissed) recompiled cleanly and re-ran.
- Run 2 (`Logs/Codex_PanelLayout_Validation2.log`) caught a real overlap: the `손님 반응` (PurchaseFeedbackPanel) bottom-right panel overlapped the right edge of the hotbar.
- Fix: raised `PurchaseFeedbackPresentationController.BuildUI` `anchoredPosition.y` from 24 to 170 so the panel sits above the hotbar. Presentation-only (one coordinate); no controller logic, gameplay, economy, NPC, or save change.
- Run 3 (`Logs/Codex_PanelLayout_Validation3.log`): all checks pass — both panels within bounds; no overlap with MoneyHUD/Demand/Village, the hotbar, or each other; `finished successfully`.
- Clean screenshot (onboarding modal dismissed): `Logs/CustomerPanelReview/20260622_112554/customer_panels_1920x1080.png`. Visual check: feedback panel sits above the hotbar; right-side panels are separated from the top-right Money/Village stack; Korean renders without tofu/boxes.
- After the panel move, all five existing validators were re-run and passed: `Logs/Codex_PanelLayout_Reg_{Presentation,FinalRoute,DayNight,Village,LongPlay}.log` (`paid=30G`, `money=4633G` unchanged).

Still required:

- A human should still confirm subjective readability and Korean font rendering on a real monitor; the automated pass covers geometry, not aesthetics.

## 2026-06-24 IL-001 + CDN-002 Real Gathering & Night Shop Gate

Scope:
- Strengthened Milestone 1 into a real playable loop: gather by day → stock/price → open the shop at night so customers can buy → settle → next-day gather resets.
- Kept work inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`. No Project_D edit/copy, no packages, no commit/push.
- Did NOT rewrite `PurchaseEvaluator`, `Shop`, `ShopSlot`, `EconomyService`, NPC FSM, or the Save schema shape (additive v8 only).

Implemented:
- IL-001 real daytime gathering: `DayNightShopLoopController` now creates 3 spread wild-forage points (`숲길 채집`/Carrot, `해변 채집`/Fish, `들판 채집`/Wheat) reusing `DaytimeStockPrepPoint`, existing Raw items, daily reset, and sellable `ItemInstance`. Forage colliders are triggers (NPC pathing not blocked). Garden Prep Basket / Producer Drop Box kept as backup/NPC support.
- CDN-002 night shop gate: `IsShopOpenForCustomers` (Day 1 tutorial always open; Day 2+ needs ShopOpen phase + player opens via `ShopOpenSign`). Gate is one guard block in `NpcController.EvaluateCurrentSlot`; closed-shop NPCs hold and wander with an occasional "가게 열면 다시 올게요" bubble.
- Save v8: `SaveData.dayPrepCollectedDay` + `dayPrepCollectedActivities`; `SaveManager` v7→v8 migration + save/restore hooks; `DayNightShopLoopController.WriteSaveFields/RestoreSavedState`.
- New `ShopOpenSign.cs` (IInteractable) open action.

Files changed/created:
- `Assets/Scripts/DayNightShopLoopController.cs`, `Assets/Scripts/ShopOpenSign.cs` (new), `Assets/Scripts/NpcController.cs`, `Assets/Scripts/SaveData.cs`, `Assets/Scripts/SaveManager.cs`.
- `Assets/Editor/PA_GatheringShopGateValidator.cs` (new), `Assets/Editor/PA_GatheringShopReview.cs` (new), `Assets/Editor/PA_DayNightShopLoopValidator.cs` (1-line update: collect every forage point before asserting exhausted).
- `Docs/IslandLife/GATHERING_AND_SHOP_GATE.md` (new) + planning docs.

Validation (all passed):
- Compile `Logs/Codex_IL001_Compile2.log` (no `error CS`).
- `PA_GatheringShopGateValidator` `Logs/Codex_IL001_GateValidation.log` (`gatherPoints=5, gatheredInventory=2, shopGate=OK`, 33 checks).
- Regressions: `Logs/Codex_IL001_Reg_FinalRoute.log` (`paid=30G`), `Logs/Codex_IL001_Reg_DayNight2.log`, `Logs/Codex_IL001_Reg_LongPlay.log` (`money=4633G`), `Logs/Codex_IL001_Reg_Presentation.log`, `Logs/Codex_IL001_Reg_Village.log`, `Logs/Codex_IL001_Reg_PanelLayout.log`.
- Screenshots `Logs/GatheringShopReview/20260624_141557/` (5 states), visually reviewed: phase HUD + Korean render OK.

Still required (human):
- Real-input play-feel pass: walk-to-gather distance, NPC pathing around forage cubes, night-open → customers actually arrive, Day 1 first sale still unblocked.
- Low-poly visual polish for the placeholder forage/sign cubes; move forage points to fixed scene terrain.

## 2026-06-24 Customer Arrival Pacing (IL/CDN follow-up)

Scope: after CDN-002 gated customer purchases, make opening the shop actively bring customers so the night-open action feels meaningful. Read-only/event-based; no `PurchaseEvaluator`/`ShopSlot`/`EconomyService`/NPC FSM rewrite; no Project_D edit, no packages, no commit/push.

Implemented:
- `Assets/Scripts/CustomerArrivalController.cs` (new sidecar, registered in `PA_RuntimeSceneBinder`). Polls `IsShopOpenForCustomers`; on Day 2+ open it invites idle customers one at a time up to `maxConcurrentCustomers` via existing `NpcController.SetShoppingPriority`/`TryForceShop`; disperses on close. Day 1 tutorial stays passive (scenario controller keeps managing Day 1 flow); paused/shopping NPCs are ignored by `TryForceShop`.

Validation (all passed):
- New `PA_CustomerArrivalValidator` — `Logs/Codex_Arrival_Validation.log` (`npcs=8, invited=3, capped=2`, 16 checks).
- Regressions: `Logs/Codex_Arrival_Reg_{Gate,FinalRoute,DayNight,LongPlay,Presentation,Village,PanelLayout}.log` — all passed (FinalRoute `paid=30G`, LongPlay `money=4633G` unchanged).
- Compile `Logs/Codex_Arrival_Compile2.log` (no `error CS`).

Still required (human): play-feel tuning of invite interval/concurrent cap; placeholder forage/sign visual polish.

## 2026-06-25 Day 1-3 Core Slice Baseline

Scope:

- Shifted the current work away from submission-prototype cleanup and toward a full-game Day 1-3 core slice.
- Followed `PROJECT_PA_CREATIVE_NORTH_STAR.md` and `PROJECT_PA_DESIGN_INTENT.md`.
- Kept work inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Did not modify Project_D, copy reference assets, import packages, push to GitHub, or rewrite core shop/economy/NPC/save logic.

Created:

- `PROJECT_PA_CORE_SLICE_PLAN.md`
  - Defines Day 1-3 as the smallest playable version of the full cozy management life-sim.
  - Separates final player-facing HUD/world elements from development/presentation overlays.
  - Preserves the reverse supply-chain backbone while reframing it through day activity -> night shop -> settlement -> next-day planning.

Implemented:

- `Assets/Scripts/CoreSlicePresentationMode.cs`
  - Runtime presentation-only sidecar.
  - Hides development/advisor canvases by default without deleting them: `LongPlayProgressionCanvas`, `ProcessingOpportunityCanvas`, `CustomerDemandInsightCanvas`, `VillageChangeSignalCanvas`, `CustomerPreferenceCanvas`, and `PurchaseFeedbackCanvas`.
  - Hides presentation/debug route markers by default: `PA_DemoRoute_VisualMarkers`, `PA_PathStep_*`, `PA_DemoRoute_Label`, `PA_CustomerApproach_Label`, `PA_Reinvestment_Label`, `PA_EconomicRoleBadge`, and `PA_ScreenshotCameraMarker_MarketHub`.
  - Keeps core player HUD visible: objective, money/tier, clock, hotbar, interaction prompt, dialogue, shop price UI, smartphone/audit app, NPC bubble feedback, and Day/Night phase HUD.
  - Development overlays can be toggled at runtime with `F10`.
- Registered `CoreSlicePresentationMode` in `PA_RuntimeSceneBinder`.
- Updated `Assembly-CSharp.csproj` with the new script include for local `dotnet build` verification; Unity may regenerate this file later.

Validation:

- `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` passed with 0 warnings and 0 errors after adding the script `.meta`.
- Attempted `PA_FinalDemoRouteValidator.RunFinalDemoRouteValidation`, but Unity batchmode aborted because the project was already open in another Unity Editor instance.
- Attempted `PA_LongPlayProgressionValidator.RunLongPlayProgressionValidation`, but Unity batchmode aborted for the same open-project lock.

Current core-slice status:

- Day 1-3 design baseline is documented.
- Development/advisor overlay clutter is hidden by default while keeping validation objects available.
- Core gameplay systems remain preserved.
- Final Unity Play Mode validation still needs to be run from the currently open Editor, or after closing the Editor and rerunning batchmode.

## 2026-06-25 Day 1-3 Core Slice Playability Pass

Scope:

- Continued the long-term full-game core-slice direction rather than submission packaging.
- Kept work inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Did not touch Project_D, copy reference assets, import packages, push to GitHub, or rewrite core shop/economy/NPC/save logic.

Implemented:

- Added `Assets/Editor/PA_CoreSlicePlayabilityValidator.cs`.
  - Opens `Assets/Scenes/Prototype_FirstDay.unity`, enters Play Mode, and checks that player-facing HUD roots exist.
  - Verifies development/advisor canvases are hidden by default through `CoreSlicePresentationMode`.
  - Verifies the same overlay path can be restored and hidden again through `SetDevelopmentOverlaysVisible`, matching the runtime F10 debug-toggle intent.
  - Captures a reference screenshot under `Logs/CoreSlicePlayability/<timestamp>/` when it can run.
- Moved `DayNightShopLoopPanel` below the Day 1 objective HUD as a compact supporting strip.
  - This is presentation-only: no phase, shop gate, economy, NPC, inventory, or save behavior changed.

Validation:

- `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` passed with 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:minimal` passed with 0 errors. Existing warnings remain: Unity source-generator `CS8785` and unused `PA_ErrorTracker._autoScrollNew`.
- Batch `PA_CoreSlicePlayabilityValidator` was attempted but aborted because the project is already open in Unity Editor:
  - `Logs/Codex_CoreSlice_PlayabilityValidation.log`
  - Reason: `Multiple Unity instances cannot open the same project`.

Still required:

- In the open Unity Editor, run `Project PA/Validation/Run Core Slice Playability Validation`.
- In the open Unity Editor, run `Project PA/Validation/Run Final Demo Route Validation`.
- In the open Unity Editor, run `Project PA/Validation/Run Long Play Progression Validation`.
- Human Game-view pass: confirm F10 physically toggles overlays, basic player view hides development panels/path labels, and Day 1-3 flow reads as day prep -> night shop -> customer reaction -> settlement -> next-day plan.

## 2026-06-26 Loop Engineering Dry-Run Guardrails

Scope:

- Documentation/tooling only. No gameplay, scene, prefab, material, UI behavior, ProjectSettings, Project_D, package, commit, or push work.
- Added shared agent guidance so Codex/Claude work from the same Project_PA rules.
- Added dry-run loop infrastructure that stops before automated implementation when the project baseline needs human approval.

Created:

- `AGENTS.md`
- `CLAUDE.md`
- `Docs/AgentWorkflow/CONTEXT_INDEX.md`
- `Automation/LoopEngineering/loop-policy.json`
- `Automation/LoopEngineering/State/loop-state.json`
- `Automation/LoopEngineering/progress.md`
- `Automation/LoopEngineering/ticket-template.md`
- `Automation/LoopEngineering/Tickets/LOOP-DRYRUN-001.md`
- `Automation/LoopEngineering/validator-registry.json`
- `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1`
- `Automation/LoopEngineering/RunLogs/preflight-20260626.json`

Updated:

- `Docs/07_개발일지.md` now includes the missing June records from CL-001 through Core Slice Playability and Loop Engineering.

Preflight result:

- `BLOCKED_BY_DIRTY_GIT`
- Root and Git root are correct.
- Unity process count for Project_PA was 0 during the check.
- Latest crash report exists: `PROJECT_PA_CRASH_REPORT_20260625.md`.
- Human baseline approval is required before enabling any implementation loop.

## 2026-06-26 Baseline Commit Review

Scope:

- Git/documentation review only.
- No gameplay code, scene, UI behavior, assets, ProjectSettings, commit, push, reset, or clean command was run.

Created:

- `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md`

Summary:

- Current branch: `master`
- Last commit: `adc5d2f 프로토타입 구성 -1`
- Git status lines: 141
  - Modified tracked paths: 27
  - Untracked paths: 114
- `git diff --stat`: 27 tracked files, 3173 insertions, 129 deletions
- `.gitignore` exists and already ignores `Library/`, `Temp/`, `Logs/`, `Builds/`, `UserSettings/`, `obj/`, `.vs/`, and `*.log`.
- Review document splits dirty state into:
  - A. recommended baseline commit paths
  - B. include only after user review
  - C. recommended exclusions
- `SubmissionPackages/` is untracked and large, about 465 MB, so it is marked as user-review before inclusion.
- Crash baseline summary from `PROJECT_PA_CRASH_REPORT_20260625.md` confirms the D3D12 crash was worked around with D3D11 and needs human launch confirmation before automation is enabled.

Next:

- User should inspect `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md`, stage approved groups manually, verify `git diff --cached --stat`, then make a local baseline commit if desired.

## 2026-06-26 VC-001A Village Culture Visual Change

Scope:

- Implemented one next-day plaza/market visual response from an existing sold product category.
- Chosen category: `Processed`, because the current Day 1 route sells `BreadLoaf` and existing `VillageChangeSignalController` reads `Processed` from `SalesLogManager`.
- Used only Project_PA runtime primitives/materials; no Project_D asset, scene, material, prefab, or script was copied.

Implementation:

- Added `Assets/Scripts/VillageCultureVisualController.cs`.
- Added `Assets/Editor/PA_VillageCultureVisualValidator.cs`.
- Runtime object: `PA_VillageCultureVisualController`.
- Visual object: `PA_VillageCulture_Processed`.
- Day 1 start: visual inactive.
- After Processed sale: pending next-day change, visual still inactive.
- Next `DayPreparation`: processed-goods prep corner becomes visible near the market hub and a one-time hint appears.

Preserved systems:

- No Save schema change.
- No scene file edit.
- No package or ProjectSettings edit.
- No core rewrite of `Shop`, `ShopSlot`, `ShopPriceUI`, `Inventory`, `EconomyService`, `PurchaseEvaluator`, `NpcController`, `SaveManager`, or day/night logic.

Validation:

- `PA_VillageCultureVisualValidator` passed.
- `PA_FinalDemoRouteValidator` passed.
- `PA_DayNightShopLoopValidator` passed.
- `PA_VillageChangeSignalValidator` passed.
- `PA_LongPlayProgressionValidator` passed.
- `PA_CustomerPresentationValidator` passed.
- `PA_CustomerPanelLayoutValidator` passed.

Evidence:

- Logs:
  - `Logs/Codex_VC001A_VillageCultureVisual.log`
  - `Logs/Codex_VC001A_FinalDemoRouteRegression.log`
  - `Logs/Codex_VC001A_DayNightRegression.log`
  - `Logs/Codex_VC001A_VillageSignalRegression.log`
  - `Logs/Codex_VC001A_LongPlayRegression.log`
  - `Logs/Codex_VC001A_CustomerPresentationRegression.log`
  - `Logs/Codex_VC001A_CustomerPanelLayoutRegression.log`
- Screenshots:
  - `Logs/VillageCultureVisual/20260626_145257/vc001a_day1_default_no_change.png`
  - `Logs/VillageCultureVisual/20260626_145257/vc001a_after_processed_sale_same_day.png`
  - `Logs/VillageCultureVisual/20260626_145257/vc001a_day2_preparation_visual_active.png`

Next:

- Human visual review should judge whether the primitive processed-goods corner reads clearly enough.
- Future village-culture work can add category variants for `Raw`, `Utility`, and `Luxury` using the same next-day activation rule.

## 2026-06-26 BASELINE-001 Bounded Ticket Loop Baseline Preparation

Scope:

- Documentation and loop-harness baseline preparation only.
- No gameplay code, scene, UI, prefab, material, asset, Package, ProjectSettings, Project_D, Unity launch, Unity batch validator, Git add, commit, push, reset, or clean command was performed.

Current Git snapshot:

- Branch: `master`
- Last commit: `adc5d2f 프로토타입 구성 -1`
- Changed file/status entries using `git status --porcelain=v1 -uall`: 144
- Modified tracked paths: 27
- Untracked paths/files: 117
- `git diff --stat`: 27 tracked files, 3173 insertions, 129 deletions
- Unity Editor process for Project_PA: none detected during this check.

Created/updated:

- Updated `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md`.
- Created `Automation/LoopEngineering/State/crash-resolution.json`.
- Updated `Automation/LoopEngineering/loop-policy.json`.
- Updated `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1`.
- Updated `Automation/LoopEngineering/State/loop-state.json`.
- Updated `Automation/LoopEngineering/progress.md`.

D3D11 stability record:

- User stated they manually launched Unity using the `-force-d3d11` baseline and confirmed project opening and Play Mode stability.
- This is recorded as `human_verified_d3d11_stable` in `Automation/LoopEngineering/State/crash-resolution.json`.
- `PROJECT_PA_CRASH_REPORT_20260625.md` remains preserved.
- D3D12 is not marked resolved, approved, or verified.

Preflight after BASELINE-001 harness correction:

- Result: `BLOCKED_BY_DIRTY_GIT`
- Crash resolution valid: true
- Approved graphics backend: D3D11
- Unity process count: 0
- Policy mode: `bounded-ticket-loop`

Next:

- User should use GitHub Desktop to stage the approved baseline files and create a local checkpoint commit.
- After the working tree is clean and Unity is closed, rerun preflight. It can become `READY_FOR_BOUNDED_TICKET_LOOP` only if the D3D11 crash-resolution record remains valid.

## 2026-07-09 AI Workflow Structure Pass

- Created `AI_WORKFLOW/` operating-document structure (00_START_HERE, 01_IDENTITY, 02_AGENT_RULES, 03_TASKS, 04_VERIFICATION, 05_LOGS, 06_HANDOFF, 07_FULL_GAME_ROADMAP, 99_ARCHIVE/old_docs).
- Rewrote root `AGENTS.md` as a short entry guide; previous version preserved at `AI_WORKFLOW/99_ARCHIVE/old_docs/AGENTS_v1_20260626.md`.
- Fixed the development goal in documentation: the target is a completable full game, not a submission-only prototype (`AI_WORKFLOW/01_IDENTITY/PROJECT_PA_SCOPE.md`).
- No code, scene, prefab, asset, or meta changes. No file deletion. No `git mv` moves: working tree still has uncommitted VC-001A changes, so all planned document moves stay deferred (`AI_WORKFLOW/00_START_HERE/DOCS_INDEX.md` section 7-B).
- Docs/01~08 remain frozen in place (code comments cite them by section number). Latest crash report stays in root (preflight glob).

## 2026-07-12 Visual Demo Integration Pass (Fable 5)

Scope:

- 새 시스템 없이 기존 구현·에셋을 연결/배치/문구 정리해 데모 화면을 "완성된 코지 상점 게임"으로 통합.
- 씬 파일·프리팹·저장 스키마·경제/NPC 코어 무변경. Project_D 미접근, 패키지 추가 없음, 커밋/푸시 없음.

Implemented:

- `Assets/Scripts/DemoVisualDressingController.cs` (신규, VC-001A 런타임 사이드카 패턴):
  - 채집 포인트 5곳: placeholder 큐브 → 나무 궤짝 + 아이템 색 작물 + 실제 아이템 아이콘 빌보드.
  - 영업 간판: 나무 기둥/걸이대/발광 랜턴 드레싱.
  - 광장: 분수 둘레 벤치 3, 동선 화단 4, 가로등 2, 상점 옆 궤짝/통. 전부 렌더러 전용(콜라이더 제거).
- HUD 문구 한국어 통일: `DayNightShopLoopController`(페이즈/영업 상태/활동 결과), `DaytimeStockPrepPoint`(프롬프트/라벨).
- Day 요약 본문 잘림 수복: Village direction 섹션 추가 후 본문 410px > 영역 360px 였던 기존 문제 → 요약 상태 본문 810x418 로 확장 (`PlayableDayScenarioController`).
- `PA_RuntimeSceneBinder` 등록 1줄, `PA_DayNightShopLoopValidator` 성공 키워드 1줄 동기화("prepared"→"낮 준비 완료"), `Assembly-CSharp.csproj` include 1줄.

Verification (Editor 닫힘 + D3D11 batchmode, 전부 통과):

- dotnet build 런타임/에디터 0 오류 (기존 CS8785 경고만).
- `PA_DayNightShopLoopValidator` — `Logs/Fable_VisualPass_DayNightValidation.log` (`sellableInventory=10`).
- `PA_FinalDemoRouteValidator` — `Logs/Fable_VisualPass_FinalRouteRegression.log` (`stocked=BreadLoaf, paid=30G`). 기존 BLOCKED 상태였던 검증기가 실제 실행·통과됨.
- `PA_FinalPresentationReviewer` — 1차 실행에서 기존 요약 잘림(410/360) 검출 → 수복 후 재실행 통과 (410/418). 캡처 5장: `Logs/FinalPresentation/20260712_161412/`.
- `PA_GatheringShopReview` — 캡처 5장: `Logs/GatheringShopReview/20260712_161528/`.
- `PA_CoreSlicePlayabilityValidator` — 통과.
- `PA_LongPlayProgressionValidator` — 통과 (`money=4633G` 기준선 불변). 기존 BLOCKED 검증기 실행·통과.

Documents:

- 신규: `AI_WORKFLOW/09_FINAL_FABLE_SPRINT/` 5종 (VISUAL_POLISH_REPORT, DEMO_SCENE_LAYOUT_PLAN, DEMO_UI_STATUS, DEMO_5_MINUTE_ROUTE, VISUAL_GAP_AND_PLACEHOLDER_PLAN).

Still required (human):

- 광장 드레싱(벤치/화단/가로등) 전경의 주관적 배치 품질을 Editor Game view 에서 확인 (자동 캡처는 카운터 클로즈업 위주).
- 한국어 폰트 실기기 확인, F10 개발 오버레이 토글 리허설.

## 2026-07-12 (저녁) Visual Demo Integration Pass v2 — 실제 Game View 기준 재작업

Scope:

- v1 이 실제 플레이 화면 기준으로 불합격 판정을 받아, 판정 기준을 "같은 플레이 카메라 Before/After 스크린샷"으로 바꿔 재작업.
- Before: `Logs/DemoViewShots/before_20260712_164931.png` / After: `Logs/DemoViewShots/after5_20260712_223953.png` (2560x1440, Day 1 15:30 동일 조건).

Implemented:

- 신규 `Assets/Editor/PA_DemoViewCapture.cs` — 실제 추적 카메라 그대로 UI 포함 캡처하는 Before/After 툴 (PA_SHOT_LABEL 환경변수로 라벨).
- 갈색 맨땅 제거: `DemoVisualDressingController` 에 광장 베이스 플레이트(30x30 석재 톤) + 판매 데크/러그/파빙/준비 매트. 지오메트리는 Shop 원점이 아니라 실측(`PlazaFrame`: 슬롯 행 방향 + 슬롯 중심→플레이어 방향 + 물리 지면 y=-0.5) 기준.
- 씬 저장 디버그 라벨 7종+ (`Guide_*`: "0. 플레이어 WASD…" 등) 과 SUPPLY/PRICE/SALE 스테이징 라벨을 `CoreSlicePresentationMode` 기본 숨김에 추가 (F10 으로만 표시).
- 상단 중앙 UI 정리: 목표 한 줄(780x44)만 유지, 페이즈 스트립 좌측 컬럼 이동, 좌측 퀘스트 체크리스트 패널(✓/▶/○) 신설 (`PlayableDayScenarioController.BuildQuestPanel`).
- `Item.icon` 6종 데이터 연결 (Free RPG Icons: 빵13/당근9/생선1/밀19/광석4/철괴5) → 핫바/쇼케이스/채집 아이콘 실물화.
- 판매대 슬롯 4개 카운터+가격판 드레싱, 상품 쇼케이스(5종), "MANAGEMENT HUB"→"코지 잡화점" 런타임 교체, 채집 포인트 이름 한국어("텃밭 바구니"/"생산자 납품함") + 라벨 소형화.

Verification (전부 통과):

- dotnet build 0 오류. `PA_FinalDemoRouteValidator`(`paid=30G`) / `PA_DayNightShopLoopValidator`(`sellableInventory=10`) / `PA_CustomerPanelLayoutValidator` — `Logs/Fable_VisualPass2_*.log`.

Still required (human, After 스크린샷 기준 잔여 문제):

- 조명이 어둑함(15:30 청회색) — DayNightVisual 커브는 미수정. 러그/파빙 가시성, 하단 기존 갈색 플랫폼 정체, 분수 벤치 품질 확인.
- 상세: `AI_WORKFLOW/09_FINAL_FABLE_SPRINT/VISUAL_POLISH_REPORT.md` v2 섹션.

## 2026-07-13 Visual Demo Integration Pass v3 — Final Presentation Lock

- Final Locked Screenshot: `Logs/DemoViewShots/after_locked_20260713_002356.png` (2560×1440, Day 1 15:42, UI 포함).
- 상태: 조명·실모델 소품·광장/NPC 스테이징·좌우 HUD 통일 완료. 메인 씬/프리팹/저장/경제·NPC 코어 무변경.
- 컴파일: 런타임/에디터 오류 0 (기존 CS8785/CS0414 경고만).
- 검증: FinalDemoRoute, DayNightShopLoop, CustomerPanelLayout, CoreSlicePlayability, FinalPresentationReviewer 전부 D3D11 batchmode Exit 0/PASS.
- 판정: **조건부 발표용**. 실제 화면은 기능과 루프가 읽히지만 중앙 상점 primitive 실루엣과 UI 스타일 불일치가 남는다.
- 사람 확인: 실제 Editor Game View가 Final Locked Screenshot과 동일하고 폰트·UI·머티리얼 이상이 없는지 1회 확인.

## 2026-07-13 Completion Matrix Sync

- 기준 커밋 `9898f6a`에서 Task 001~085를 실제 구현/검증 증거로 재판정했다.
- 집계: DONE 21 / PARTIAL 26 / TODO 12 / BLOCKED 6 / DECISION_REQUIRED 20.
- Core Loop의 개별 연결은 검증됐지만 Persistence Lock은 미완이다. v8이 돈·인벤토리·핫바·진열·가격·NPC FSM·Day Prep을 저장하도록 구현돼 있으나 실제 저장소 왕복 증거가 없다.
- 다음: Task 007 저장 스키마 확정 → Task 011 안전한 저장 왕복 검증 → Task 018 진열 수량 표시.
- 상세: `AI_WORKFLOW/03_TASKS/CURRENT_COMPLETION_MATRIX.md`.

### Task 007 완료

- Save v8 필드·DTO·마이그레이션·복원 순서를 `AI_WORKFLOW/03_TASKS/SAVE_SCHEMA.md`로 확정했다.
- 현재 미저장: 판매 이력, 카테고리 트렌드, 마을 변화 pending/active 상태.
- 저장 코드 무변경. 다음은 사용자 세이브를 보호하는 실제 저장소 왕복 검증(Task 011).

### Task 011 완료

- 사용자 save 대신 `Logs/SaveRoundTrip/20260713_010249`를 사용해 실제 SaveManager v8 왕복 PASS.
- 돈·누적매출·위치·시간/일차·인벤토리·핫바·진열 아이템/수량/가격·Day Prep·Day 1 단계가 복원됐다.
- Save 런타임 코드와 스키마는 변경하지 않았다. FinalRoute/DayNight 회귀 PASS.

### Task 018 완료

- 가격 설정 패널이 현재 ShopSlot의 실제 진열 수량을 `아이템 · 재고 N개`로 표시한다.
- 동일 카메라 Before/After를 확보했고 FinalPresentation/FinalRoute/DayNight/PanelLayout이 모두 PASS했다.
- 판매·구매·저장 로직 무변경.

## 2026-07-15 Shop Evolution S4 — Tier 1 Interior Unlock

- Tier 0에서는 외부 잡화점 문이 실제 워프를 차단하고 `Tier 1 지점장 필요`를 표시한다.
- 누적 진행으로 Tier 1에 도달하면 문이 열리고 간판이 `잡화점 · OPEN`으로 바뀌며, 따뜻한 문 조명과 짧은 해금 안내가 표시된다.
- 해금 상태는 별도 저장 필드가 아니라 이미 v9에 저장되는 `currentTier`에서 파생되므로 저장/로드 후에도 중복 상태 없이 복원된다.
- D3D11 Play Mode 스모크 PASS: Tier 0 잠금 → Tier 1 해금 → 실내 입장(y=100.1) → BreadLoaf 진열 → 가격 UI → 외부 복귀(y=0.1).
- 컴파일 오류 0. 씬/프리팹/저장 스키마/Shop·경제·구매·NPC FSM은 변경하지 않았다.
- 완성 관점 현황과 다음 순서는 `Docs/Codex/` 4종에 정리했다.

## 2026-07-15 Task 034 — Daily Customer Decision Summary

- 실제 판매가 완료될 때 구매 수를, 구매 평가가 거절될 때 거절 수를 일차별로 집계한다.
- 밤 정산 HUD에 `구매/보류/구매율`을 표시하고, 다음 날 시작 안내를 높은 거절 비율에 맞춘 가격 점검 조언으로 연결한다.
- Day 1 결산도 같은 통계 원본을 사용해 화면마다 수치가 어긋나지 않게 했다.
- 런타임/에디터 컴파일 오류 0. D3D11 전용 검증에서 구매 1건·거절 1건·구매율 50%, 정산 문구와 다음 날 조언을 확인했다.
- 메인 씬·프리팹·구매 확률·돈/재고·NPC FSM·저장 v9는 변경하지 않았다. 일일 통계는 런타임 전용이며 저장 영속화는 Task 055 승인 범위다.

## 2026-07-15 Task 068 Partial — Player-Controlled Day Transition

- 실제 플레이 경로 감사에서 Day 2~7 검증기가 `ForceSet`으로 날짜를 우회하고, 플레이어에게 하루 마감 입력이 없음을 최대 단절로 확인했다.
- Day 1 결산의 `다음 날 시작` 버튼은 Day 2 06:00과 새 플레이어 목표를 연다.
- Day 2+ 정산에서는 기존 가게 간판을 `[Space]`로 사용해 다음 날 06:00을 시작한다. `OnNewDay`를 정상 발화하므로 생산자 납품·채집 리셋·NPC 일과·마을 변화 구독 경로가 유지된다.
- 런타임/에디터 컴파일 오류 0. D3D11 FinalDemoRoute와 DayNightShopLoop PASS.
- 메인 씬·프리팹·저장 v9·경제/구매·NPC FSM·스케줄 시간대는 변경하지 않았다.
- Task 068 전체 완료에는 Task 041 낚시→진열→판매 왕복과 새 전환 경로를 통한 사람 3일 연속 플레이/저장 재실행이 남는다.

## 2026-07-15 Task 039 — 실제 낚시 상호작용 완료

- 해변의 `shore-forage`는 이제 일반 즉시 채집이 아니라 `[Space]`로 낚싯대를 드리우고 1.25초 동안 입질을 기다린 뒤 Fish 2개를 얻는 실제 플레이어 행동이다.
- 전용 자식 트리거가 `IInteractable`을 담당하고, 기존 `DaytimeStockPrepPoint`는 하루 1회·다음 날 리셋·v9 저장 복원의 단일 상태 원본으로 유지된다.
- 물빛 표식·낚싯대·찌·어획 바구니 런타임 외형을 추가했다. 씬·프리팹·저장 스키마·입력 코어는 변경하지 않았다.
- D3D11 GatheringShopGate에서 캐스팅 상태, Fish 2개, 동일 일차 차단, 다음 날 재활성, 진열·가격, 저장 복원을 확인했다. FinalDemoRoute도 30G 판매와 Day 2 전환을 유지했다.
- 다음 기능 연결은 Task 041의 낚시→진열→NPC 구매·수익 증가 단일 왕복이다.
