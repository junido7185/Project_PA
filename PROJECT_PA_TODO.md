# Project PA TODO

Inspection date: 2026-06-19

## Sprint: Day 1-3 Core Game Slice

Goal:

- Establish Day 1-3 as the long-term full-game baseline, not a submission-only prototype route.
- Make the current player view understandable before adding more features.
- Preserve the reverse supply-chain management identity while reducing debug/advisor clutter.

Status: First planning and presentation-mode pass complete; Unity Play Mode validator rerun is pending because the project is currently open in Unity Editor.

- [x] Read `PROJECT_PA_CREATIVE_NORTH_STAR.md` and `PROJECT_PA_DESIGN_INTENT.md` as the top design standard.
- [x] Inventory current player-visible UI, labels, runtime canvases, and presentation markers.
- [x] Separate final player-facing elements from development/presentation overlays.
- [x] Create `PROJECT_PA_CORE_SLICE_PLAN.md`.
- [x] Add `CoreSlicePresentationMode` as a presentation-only sidecar.
- [x] Register `CoreSlicePresentationMode` through `PA_RuntimeSceneBinder`.
- [x] Hide development/advisor canvases by default while preserving validator access.
- [x] Hide presentation/debug route markers and NPC role badges by default.
- [x] Keep objective, money/tier, clock, hotbar, interaction prompt, dialogue, ShopPriceUI, smartphone/audit, NPC bubble, and Day/Night phase HUD visible.
- [x] Verify C# build with `dotnet build Assembly-CSharp.csproj` (passed with 0 warnings and 0 errors after adding the script `.meta`).
- [ ] Run `PA_FinalDemoRouteValidator.RunFinalDemoRouteValidation` after closing the current Unity Editor instance or from the open Editor menu.
- [ ] Run `PA_LongPlayProgressionValidator.RunLongPlayProgressionValidation` after closing the current Unity Editor instance or from the open Editor menu.
- [ ] Manually inspect the Game view with development overlays hidden by default.
- [ ] Decide whether `DayNightShopLoopCanvas` should remain visible or be folded into the main objective/HUD in the next sprint.

Acceptance target:

- Day 1-3 reads like a coherent cozy management life-sim slice: day prep, night shop, customer response, settlement, and next-day planning.

## Creative North Star Lock - 2026-06-21

Status: Done for documentation pass; no code, scene, UI, or asset implementation was performed.

- [x] Created `PROJECT_PA_CREATIVE_NORTH_STAR.md`.
- [x] Reframed Project_PA as a cozy 3D life and shop management simulation.
- [x] Preserved reverse supply-chain design as the economic backbone/support system.
- [x] Reinterpreted NPC production as support, automation, and growth that reduces repetitive labor over time.
- [x] Reframed the shop/stall as a day-to-night shop hub and village-change interface.
- [x] Marked Project_D / ReferencePrototype and VisualTargets as reference-only material.
- [x] Updated planning docs so future visual/development work reads `PROJECT_PA_CREATIVE_NORTH_STAR.md` first.

Next Milestone 1 tasks:

- [x] CDN-001 Define and implement day/night phase flow.
- [x] CDN-002 Add readable shop open/close state.
- [x] CDN-003 Add daily settlement that includes money, customer feedback, product category impact, and next action.
- [x] CDN-004 Add at least two daytime activities or MVP equivalents that prepare shop stock.
- [x] VC-001 Add one visible product-category-to-village-change signal.
- [x] SPY-002 Present at least two customer types with readable buy/reject feedback.

SPY-002 implementation notes - 2026-06-22:

- [x] Added `CustomerPreferencePresentationController` (read-only "관심 손님 성향" panel from real `NpcProfile` traits).
- [x] Added `PurchaseFeedbackPresentationController` (cozy buy/reject reasons + village-change tie line, no debug numbers).
- [x] Added a single read-only hook in `NpcController.EvaluateCurrentSlot` (same pattern as the demand-insight hook).
- [x] Registered both controllers through `PA_RuntimeSceneBinder`.
- [x] Added `PA_CustomerPresentationValidator` and re-ran the four existing validators.
- [x] Preserved `PurchaseEvaluator`, `Shop`, `ShopSlot`, `ShopPriceUI`, `EconomyService`, NPC FSM, Save, and the existing head bubble.
- [x] Used only real data: traitSN (category), traitTF (buy style), traitEI (eagerness); priceSensitivity is identical (1.0) across profiles so it is intentionally not shown yet.
- [ ] Manual 1920x1080 Game-view review of the two new panels (see `Docs/CustomerPresentation/README.md`).

SPY-003 per-resident consumption data - 2026-06-22:

- [x] Varied `priceSensitivity` per resident (0.65 ~ 1.45) across the eight `Assets/Resources/NPCs/Profile_*.asset` files.
- [x] Varied `utilityConsumption`/`luxuryConsumption` to reflect each job archetype (data only; no `PurchaseEvaluator` code change).
- [x] "가격에 민감/관대" preference hint now appears from real data (Farmer/Miner sensitive, Tailor tolerant).
- [x] Extended `PA_CustomerPresentationValidator` to assert the data-driven price hints.
- [x] Re-ran all five validators (CustomerPresentation/FinalRoute/DayNight/Village/LongPlay) — all passed; `paid=30G` and `money=4633G` unchanged.
- [ ] Manual 1920x1080 Game-view review still pending.

SPY-002 panel layout validation - 2026-06-22:

- [x] Added `Assets/Editor/PA_CustomerPanelLayoutValidator.cs` (coordinate-based overlap/containment checks + 1920x1080 screenshot).
- [x] Improved the validator (hotbar region from active `InventorySlotUI`, onboarding modal dismissed) and re-ran it.
- [x] Run 2 caught a real overlap: `손님 반응` panel overlapped the hotbar's right edge.
- [x] Fixed by raising `PurchaseFeedbackPresentationController` `anchoredPosition.y` 24 → 170 (presentation only).
- [x] Run 3 passes all checks incl. hotbar (`Logs/Codex_PanelLayout_Validation3.log`); clean screenshot at `Logs/CustomerPanelReview/20260622_112554/`.
- [x] Re-ran the five existing validators after the panel move — all passed (`Logs/Codex_PanelLayout_Reg_*.log`).
- [ ] Human readability/Korean-font confirmation on a real monitor still recommended (geometry is automated, aesthetics are not).

Milestone 1 implementation notes - 2026-06-21:

- [x] Added `DayNightShopLoopController` as a sidecar day/night phase service.
- [x] Added readable `Day Prep`, `Night Shop Open`, and `Settlement` state text.
- [x] Preserved Day 1 tutorial shop access so the existing validated route still works.
- [x] Added two runtime daytime stock-prep MVPs through `DaytimeStockPrepPoint`: `Garden Prep Basket` and `Producer Drop Box`.
- [x] Added `VillageChangeSignalController` to show a product-category village direction signal from recent sales.
- [x] Added `Village direction` to the Day 1 summary/settlement flow.
- [x] Added `PA_DayNightShopLoopValidator` and `PA_VillageChangeSignalValidator`.
- [x] Re-ran Day 1 route and long-play validators after the new Milestone 1 layer.
- [ ] Replace the two MVP stock sources with richer cozy daytime activities later: gathering, fishing, talking, buying supply, or processing.
- [ ] Manually review the new phase, prep, village-direction, and summary UI in a real 1920x1080 Game view.

Before any Visual Acceleration, shop, NPC, economy, or long-play implementation task:

- Read `PROJECT_PA_CREATIVE_NORTH_STAR.md`.
- Preserve the day-to-night cozy life/shop identity.
- Keep Project_D as reference only.
- Do not copy assets or imitate commercial UI/systems exactly.

## T001 Project Status Check

Status: Done for initial inspection.

- [x] Confirmed project root.
- [x] Confirmed Git root.
- [x] Confirmed Unity folders.
- [x] Checked Git status.
- [x] Inventoried scenes, scripts, prefabs, docs, data assets, and build settings.
- [x] Created `PROJECT_PA_STATUS.md`.
- [x] Created `PROJECT_PA_TODO.md`.

Notes:

- Initial Git status was clean.
- Documentation files now exist and will make Git status dirty until committed or ignored intentionally.

## T002 Play Mode Error Fix If Needed

Status: Batch Play Smoke, automated final route validation, final presentation review, and human Windows executable route review passed.

- [x] Batch compile in Unity 6000.3.2f1 completed without C# compiler errors.
- [x] Automated smoke attempt loaded `Assets/Scenes/Prototype_FirstDay.unity`.
- [x] Runtime log showed `PA_RuntimeSceneBinder` and `Shop.Start()` running.
- [x] Fixed the automated Play Smoke test so it survives Unity domain reload and exits cleanly.
- [x] Automated Play Smoke passed.
- [x] Added Editor-only final route validator: `Assets/Editor/PA_FinalDemoRouteValidator.cs`.
- [x] Automated final route validation passed in Play Mode.
- [x] Final presentation review passed with generated Game-view captures.
- [x] Fixed Windows-player NavMesh startup blocker by rebaking `Prototype_FirstDay.unity` NavMesh and enabling NPC agents from `PA_RuntimeSceneBinder` after NavMeshSurface data is active.
- [x] Play Smoke counts: players=1, shops=2, shopSlots=8, economyServices=1, shopPriceUI=1, npcs=8, npcAgents=8/8, npcAgentsOnMesh=8.
- [x] Open/load `Assets/Scenes/Prototype_FirstDay.unity` by Unity automation for final presentation review.
- [x] Check Unity compile/log state by automation.
- [x] Enter Play Mode by automated validation.
- [x] Complete automated route: dialogue -> stock -> price -> NPC purchase -> money update -> HUD/audit/summary.
- [x] Complete the same full demo route by a human at the keyboard: move -> stock -> price -> NPC purchase -> money update.
- [ ] Capture exact compile/runtime errors if any appear in manual review.
- [ ] Fix only blocking errors required for final demo.
- [ ] Re-test Play Mode after each fix.

Acceptance target:

- `Prototype_FirstDay.unity` enters Play Mode without blocking console errors.

## T003 Main Scene Flow Verification

Status: Automated route validation and final presentation screenshot review passed; human camera/control feel review remains.

- [ ] Confirm intended final start scene: `Prototype_FirstDay.unity` or `MainGame.unity`.
- [x] Confirm intended final start scene: `Prototype_FirstDay.unity`.
- [x] Verify player movement components exist.
- [x] Verify presentation camera framing by generated screenshots.
- [x] Verify interact prompt appears in generated screenshots.
- [x] Verify NPC dialogue opens `DialogueUI`.
- [ ] Verify inventory/hotbar interaction.
- [x] Verify stocking a shop slot from hotbar.
- [x] Verify price-setting UI opens and confirms price.
- [x] Verify NPC purchase/sales/money update through route validator.
- [x] Verify audit panel goal text.
- [ ] Verify save/load behavior only if it is part of the final demo.

Acceptance target:

- A short final-demo route can be completed reliably from a fresh Play Mode start.

## T004 Visual Polish Pass

Status: First final-presentation pass done; human subjective polish pass remains.

- [x] Check final scene camera framing by generated presentation captures.
- [ ] Check lighting and day/night visibility.
- [ ] Check obvious placeholder objects.
- [x] Check NPC/player readability in presentation captures.
- [ ] Check shop/workbench/building labels.
- [ ] Keep polish scoped to final-demo clarity.

Acceptance target:

- Demo scene looks intentional enough for final submission screenshots/video.

## T005 UI Readability Pass

Status: Final presentation screenshot review passed; a human 1920x1080 playthrough is still recommended.

- [x] Check `MoneyHUD` does not overlap objective panel in automated layout validation.
- [x] Add next-tier goal text to `MoneyHUD`.
- [x] Update audit app next-tier text from actual `TierDefinition` data.
- [x] Update Day 1 summary to show next growth requirements from actual tier data.
- [x] Remove emoji from `ShopPriceUI` reaction labels to reduce font fallback risk.
- [x] Shorten NPC purchase/rejection feedback text.
- [x] Enlarge and wrap `NpcBubbleUI` for compact feedback.
- [x] Check Korean text rendering and font fallbacks through generated Game-view captures.
- [x] Check objective text readability.
- [ ] Check dialogue panel readability.
- [x] Check shop pricing panel readability.
- [x] Check smartphone audit panel readability.
- [ ] Check pause/settings panel.

Acceptance target:

- UI text is readable at the target build resolution and does not block key gameplay.

## T006 Windows Build

Status: Latest automated build, smoke launch, and human executable full-route review passed.

- [x] Remove missing `Assets/Scenes/SampleScene.unity` from Build Settings.
- [x] Add/confirm final start scene in Build Settings: `Assets/Scenes/Prototype_FirstDay.unity`.
- [x] Confirm Windows standalone build path.
- [x] Build into project-local folder: `Builds/Windows/`.
- [x] Run the produced executable in smoke mode.
- [x] Confirm player smoke log reaches runtime binder/shop startup.
- [x] Confirm previous `Failed to create agent because there is no valid NavMesh` message is gone.
- [x] Rebuilt Windows executable after final route/UI readability fixes.
- [x] Smoke-launch rebuilt Windows executable after final presentation fixes.
- [x] Verify the same final-demo route in the executable by a human.

Acceptance target:

- Windows executable launches and reaches the playable demo without blocking errors.

## T007 Submission Zip Package

Status: Done. Source and executable submission packages were created after the human Windows executable route pass.

- [x] Define source package target name: `Project_PA_Source_20260620.zip`.
- [x] Define executable package target name: `Project_PA_Windows_20260620.zip`.
- [x] Define source include list: `Assets/`, `Packages/`, `ProjectSettings/`, `Docs/`, root `PROJECT_PA_*.md`, `README.md`, `.gitignore`, and solution/project files if required.
- [x] Define source exclude list: `.git/`, `Library/`, `Temp/`, `Logs/`, `Builds/`, `UserSettings/`, `obj/`, `.vs/`, and cache/generated folders.
- [x] Define executable include list: `Project_PA.exe`, `Project_PA_Data/`, `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `DirectML.dll`, `D3D12/`, `MonoBleedingEdge/`, and run instructions.
- [x] Define executable exclude list: `Project_PA_BurstDebugInformation_DoNotShip/`, `Logs/`, source folders, cache folders, and generated debug folders.
- [x] Create executable package after human Windows exe route pass.
- [x] Create source package after human Windows exe route pass.
- [x] Confirm packages open and scan correctly with forbidden entries excluded.

Created packages:

- `SubmissionPackages/Project_PA_Source_20260620.zip` - about 359.17 MiB, 1,369 entries.
- `SubmissionPackages/Project_PA_Windows_20260620.zip` - about 84.25 MiB, 183 entries.

Acceptance target:

- Submission has a separate playable executable package and source package.

## T008 README / Run Instructions

Status: First draft done; update again after manual route verification.

- [x] Create or update root README.
- [x] Include Unity version: 6000.3.2f1.
- [x] Include how to open source project.
- [x] Include how to run executable.
- [x] Include controls from `PlayerInputHandler`.
- [x] Include final demo route.
- [x] Include known limitations if any remain.

Acceptance target:

- A grader can run the executable and understand the intended demo route without extra explanation.

## Sprint: Cozy Market Visual Acceleration

Goal:

- Use `CozyMarketPrototype` / `ReferencePrototype` as a visual and staging reference only.
- Improve Project_PA market clarity without copying unsafe assets or changing gameplay logic.
- Keep all implementation inside Project_PA.
- Read `PROJECT_PA_DESIGN_INTENT.md` before every Visual Acceleration task so visual work supports the reverse supply-chain management identity.

## T009 Reference Prototype Audit

Status: Done for planning.

- [x] Verified corrected reference root.
- [x] Confirmed reference Unity folders.
- [x] Inspected scenes, prefabs, models/meshes, materials, UI textures, scripts, and build settings.
- [x] Identified market/stall, crate, sign, path, lamp, bench, display, register, NPC, and UI reference ideas.
- [x] Classified broad reference asset folders and scripts as unsafe for direct migration.
- [x] Created `PROJECT_PA_MIGRATION_PLAN.md`.

Acceptance target:

- Reference ideas are documented without copying assets.

## T009.5 Design Intent Lock

Status: Done.

- [x] Created `PROJECT_PA_DESIGN_INTENT.md`.
- [x] Locked Project_PA as a reverse supply-chain management simulation.
- [x] Clarified that the player is a manager, not the primary laborer.
- [x] Clarified that Project_D / ReferencePrototype and VisualTargets are visual, route, staging, and readability references only.
- [x] Clarified that T010 Market Stall Visual Migration must make the stall a readable operating hub for the economy, not merely a prettier shop.

Required for later Visual Acceleration tasks:

- Read `PROJECT_PA_DESIGN_INTENT.md` before editing scenes, UI, prefabs, or presentation docs.
- Preserve Project_PA systems and naming.
- Do not copy assets from ReferencePrototype unless a later task explicitly approves a safe, file-by-file migration.

## T010 Market Stall Visual Migration

Status: Done for first visual pass; automated Play Smoke passed; needs manual camera review.

- [x] Confirm no direct reference assets will be copied unless ownership is explicitly approved.
- [x] Backup target scene before edits.
- [x] Create a Project_PA-owned market stall visual variant.
- [x] Add visible product shelf/slot markers.
- [x] Add Project_PA-created crate/basket-like display props.
- [x] Add price tag anchors or simple labels.
- [x] Add small signboard and warm accent props.
- [x] Verify `Shop` and `ShopSlot` scene references remain present by Unity batch verification.
- [x] Automated Play Smoke confirmed player/shop/slot/economy/UI/NPC runtime objects.
- [ ] Manually verify stock -> price -> NPC purchase in Play Mode.

Acceptance target:

- The stall reads as the reverse supply-chain operating hub from the gameplay camera.

## T011 Project_PA Market Scene Staging

Status: Done for first visual pass; needs manual route review.

- [x] Use `Prototype_FirstDay.unity` as the staging scene.
- [x] Create a scene backup first.
- [x] Add route markers around the market stall.
- [x] Add delivery/customer/reinvestment visual pads.
- [x] Add Project_PA-owned primitive signs and markers.
- [x] Preserve current scenario logic.
- [ ] Manually review whether player start and camera make the route visible.

Acceptance target:

- The first-day route visually guides talk, stock, price, and sale steps.

## T012 Shop UI Restyle

Status: Inspected; layout change deferred until manual 1920x1080 review.

- [ ] Review objective panel readability in Play Mode.
- [x] Review `ShopPriceUI` script for required information.
- [x] Review money HUD script for money/tier readability.
- [ ] Review interaction prompt placement.
- [ ] Use existing Project_PA UI sprites and colors first.
- [ ] Keep UI behavior unchanged.

Notes:

- `ShopPriceUI` already shows item name, current price, NPC reaction hint, approximate purchase rate, confirm, retrieve, and close controls.
- No UI behavior/layout code was changed in this pass because visual verification needs an actual 1920x1080 Play Mode view.

Acceptance target:

- Key demo UI is readable at target build resolution.

## T013 NPC/Customer Presentation Pass

Status: Done for first visual pass; needs manual readability review.

- [x] Add role badges for NPC economy roles.
- [x] Add customer approach pad near the shop.
- [x] Avoid gameplay logic changes.
- [ ] Check first settler visibility in Play Mode.
- [ ] Check NPC bubble/label readability in camera.
- [ ] Check player/NPC scale relative to stall.

Acceptance target:

- The evaluator can understand who to talk to and who buys from the stall.

## T014 Demo Route Screenshot Pass

Status: Done for final presentation capture pass.

- [x] Add `PA_ScreenshotCameraMarker_MarketHub` to the demo shop.
- [x] Capture and inspect final presentation views.
- [x] Verify stall, player, NPC, product display, price UI, and money HUD are visible.
- [x] Adjust UI/readability only, without changing gameplay logic.
- [ ] Save representative screenshots for report/presentation if requested.

Acceptance target:

- The final demo has at least one clear presentation-quality market view.

## T015 Build and Submission Package

Status: Done for submission packaging after human Windows executable route verification.

- [x] Remove missing build-settings scene entry.
- [x] Confirm final start scene.
- [x] Build Windows executable.
- [x] Smoke-launch executable.
- [x] Verify executable demo route manually.
- [x] Define executable/source package include-exclude plan.
- [x] Package executable after human route pass.
- [x] Package source without cache/generated folders after human route pass.
- [x] Include README/run instructions draft.
- [ ] Select/include final report/presentation.

Acceptance target:

- Final submission package contains a runnable build and source package.

## Sprint: Complete Game Foundation

Goal:

- Keep Project_PA aligned with `PROJECT_PA_DESIGN_INTENT.md`.
- Turn the first-day demo from a simple sale tutorial into a readable reverse supply-chain management loop.
- Use existing systems instead of rewriting `Shop`, `ShopSlot`, `ShopPriceUI`, `EconomyService`, `PurchaseEvaluator`, tier, audit, save, hiring, or NPC AI logic.

## T016 Completion Plan Documents

Status: Done.

- [x] Created `PROJECT_PA_COMPLETION_PLAN.md`.
- [x] Created `PROJECT_PA_RELEASE_BACKLOG.md`.
- [x] Created `PROJECT_PA_CURRENT_MILESTONE.md`.
- [x] Defined complete-game target, 1.0 target, submission-demo scope, Day 1 loop, Day 7 goals, tier growth, NPC roles, UI/art/build QA standards, and deferred items.

Acceptance target:

- Future Codex sessions can choose work from a clear long-term plan without drifting into a simple shop clone.

## CL-001 Management Loop Objective Rewrite

Status: Done for first pass; manual readability review required.

- [x] Rewrote first-day objective labels around supply, stocking, pricing, customer reaction, revenue, audit/tier review, and saving.
- [x] Preserved `PlayableDayScenarioController` stage flow.
- [x] Did not change core shop/economy logic.

Acceptance target:

- The objective panel communicates that the player is managing an economic loop, not just doing errands.

## CL-002 NPC Purchase Feedback

Status: Done for first pass; automated bubble compactness check passed, human readability review still recommended.

- [x] Added buy/reject feedback text in `NpcController`.
- [x] Feedback uses existing `PurchaseEvaluator.Result` data and existing `NpcBubbleUI`.
- [x] Recent feedback is recorded by `PlayableDayScenarioController` for the day summary.
- [x] Purchase probability and broad price/category reason are visible to the player.
- [x] Shortened feedback strings for NPC feedback bubble readability.
- [x] Automated validator confirmed generated feedback fit compact length and appeared in bubble.

Acceptance target:

- A player can tell why an NPC bought or rejected an item.

## CL-003 Day Summary Improvement

Status: Done for first pass; automated full-route validation passed.

- [x] Day 1 summary now shows revenue, money delta, sales count, relationship points, recent customer feedback, and next management action.
- [x] Summary remains inside the existing startup/flow panel.
- [x] Automated validator confirmed the summary appears and includes purchase feedback, next growth goal, and tier revenue progress.
- [ ] Manually confirm summary readability in Game view.

Acceptance target:

- End-of-day feedback points the player toward restocking, price tuning, or better goods.

## CL-004 Tier 0 Goal Clarity

Status: Done for first pass; automated HUD/audit validation passed, 1920x1080 human layout review required.

- [x] `MoneyHUD` now shows current money, current tier, and next-tier requirement.
- [x] `AuditResultUI` now summarizes the actual next `TierDefinition` requirements instead of only revenue or a generic message.
- [x] Day 1 summary now pulls next growth requirements from `TierDefinition` data.
- [x] No tier progression math or unlock conditions were changed.
- [x] Automated validator confirmed `MoneyHUD` and `AuditResultUI` next-tier goal text.
- [ ] Manually check that the expanded HUD does not overlap important UI/gameplay.

## T017 Final Demo Route Validation

Status: Automated validation, final presentation review, human executable route review, and package creation are done.

- [x] Created Editor-only final route validator.
- [x] Verified Play Mode entry.
- [x] Verified player movement components.
- [x] Verified first NPC dialogue opens.
- [x] Verified hotbar-to-shop-slot stocking.
- [x] Verified price UI open/confirm.
- [x] Verified compact NPC feedback appears in `NpcBubbleUI`.
- [x] Verified sale and money/revenue update.
- [x] Verified next-tier HUD and audit text.
- [x] Verified Day 1 summary content.
- [x] Verified market hub/route/role/screenshot markers still exist.
- [x] Rebuilt Windows executable.
- [x] Smoke-launched Windows executable.
- [x] Generated final presentation captures for market hub, price UI, NPC feedback, audit app, and Day 1 summary.
- [x] Re-ran automated final route validation after presentation fixes.
- [x] Submission package plan prepared.
- [x] Human Windows executable route result recorded as passed.
- [x] Human completed full route in the Windows executable.
- [x] Source package created at `SubmissionPackages/Project_PA_Source_20260620.zip`.
- [x] Windows executable package created at `SubmissionPackages/Project_PA_Windows_20260620.zip`.

Acceptance target:

- A final evaluator can complete the first-day route and understand the management loop.

Acceptance target:

- The player can see what moves them from Tier 0 toward Tier 1 without reading code or documentation.

## Sprint: Full Game 1.0 Development Mode

Goal:

- Move Project_PA beyond the submission prototype while preserving the original reverse supply-chain management intent.
- Keep existing submission packages intact as a snapshot.
- Build the full game through incremental, verifiable systems rather than a large rewrite.

## FG-001 Core Multi-Day Loop

Status: First implementation pass validated by automated Play Mode checks.

- [x] Created `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md`.
- [x] Created `PROJECT_PA_FULL_GAME_BACKLOG.md`.
- [x] Preserved Day 1 route as onboarding baseline.
- [x] Added `LongPlayProgressionController` sidecar service.
- [x] Added Day 1-7 long-play objective plan data.
- [x] Added long-play HUD text for day, objective, revenue target, and management focus.
- [x] Registered the controller through `PA_RuntimeSceneBinder`.
- [x] Run `PA_LongPlayProgressionValidator`.
- [x] Verify Day 1 route still works after the long-play layer through `PA_FinalDemoRouteValidator`.
- [x] Verify Day 2 to Day 7 progression through `PA_LongPlayProgressionValidator`.
- [ ] Manually review subjective player feel and HUD readability in the Unity Game view.

Acceptance target:

- A player can continue beyond the first day into a readable first-week operations loop.

## FG-002 NPC Producer Economy

Status: First implementation pass validated by automated Play Mode checks.

- [x] Added Day 2-7 planned producer deliveries.
- [x] Used existing `EconomyService.TrySpend` for producer buy-in.
- [x] Used existing `Inventory.AddInstance` for delivered goods.
- [x] Added refund path when inventory is full.
- [x] Preserved existing `ProducerNpcController` and `ProductionData` without rewriting them.
- [x] Validate Day 2-7 producer deliveries increase sellable inventory.
- [x] Validate daily buy-in uses money and leaves the economy path intact.
- [ ] Add richer producer NPC presentation and delivery source after validation.
- [ ] Decide how producer reliability, friendship, and town facilities modify daily supply.

Acceptance target:

- NPC production becomes visible as the upstream source of the shop economy.

## FG-011 Save / Load / Persistence

Status: v7 first pass validated for Day 3 long-play state.

- [x] Added `SaveData.longPlayLastSupplyDay`.
- [x] Added `SaveData.longPlayDayStartRevenue`.
- [x] Added `SaveData.longPlayDayStartMoney`.
- [x] Raised `SaveManager` schema version from v6 to v7.
- [x] Added v6 -> v7 migration defaults.
- [x] Added save/load hooks for `LongPlayProgressionController`.
- [x] Save on Day 3, mutate runtime state, reload, and confirm long-play state resumes correctly.
- [ ] Repeat save/load validation after future Day 8+ systems are added.

Acceptance target:

- Long-play day state survives save/load without damaging existing Day 1, inventory, shop slot, audit, tier, hiring, or friendship data.

## FG-015 Long Play QA

Status: Tool added and passed.

- [x] Added `Assets/Editor/PA_LongPlayProgressionValidator.cs`.
- [x] Unity batch compile after code changes exited successfully.
- [x] Run the validator when the project is not locked by another Unity Editor instance.
- [x] Validate Day 2-7 producer delivery and long-play HUD.
- [x] Validate Day 3 project-local save/load.
- [ ] Add future manual Day 2-7 checklist results after a human Game view pass.

Acceptance target:

- Day 2-7 producer delivery and long-play objective UI can be verified automatically before deeper systems are added.

## FG-004 Processing And Crafting Chain

Status: First implementation pass done and validated.

- [x] Inspect `CraftingService`, `RecipeData`, `Workbench`, `CraftingUI`, and available recipes.
- [x] Add a low-risk processing opportunity layer that shows raw-vs-processed value decisions.
- [x] Preserve existing crafting behavior and recipe data.
- [x] Use existing item base prices and recipe outputs to explain why processing matters.
- [x] Add validator coverage for at least one raw -> processed value chain.
- [x] Validate existing `CraftingService.TryCraft` creates processed output.
- [x] Re-run long-play regression validation after adding the processing advisor.
- [ ] Update long-play goals so Days 4-7 point to real processing choices in a more detailed way.
- [ ] Add facility/workbench availability to the management advisor.
- [ ] Balance recipes so multiple early chains have meaningful but not runaway margins.

Acceptance target:

- The player can understand why holding, processing, or selling a producer delivery changes management outcomes.

## FG-003 Customer Simulation

Status: First implementation pass done and validated.

- [x] Inspect `PurchaseEvaluator`, `NpcProfile`, `NpcController`, `SalesLogManager`, and NPC feedback flow.
- [x] Add a low-risk customer demand insight layer instead of rewriting purchase math.
- [x] Surface customer preference/category demand in the management UI.
- [x] Track recent accept/reject outcomes by item category.
- [x] Preserve current buy/reject probability and sale logic.
- [x] Add validator coverage that customer insight is generated after buy/pass signals.
- [x] Re-run Day 1 route regression after adding the observer hook.
- [x] Re-run long-play regression after adding the demand insight layer.
- [ ] Expand demand insight from immediate signals into daily/weekly trends.
- [ ] Connect demand categories to audit/reputation/tier goals.
- [ ] Add clearer per-customer segment labels after manual UI review.

Acceptance target:

- The player can understand not only what sold, but what type of customer demand the market is signaling.

## FG-009 Tier / Audit / Reputation Progression

Status: Recommended next sprint.

- [ ] Inspect `TierService`, `TierDefinition`, `AuditService`, `AuditResultUI`, `MoneyHUD`, and new long-play/demand/processing signals.
- [ ] Define Week 1 audit criteria beyond total revenue.
- [ ] Add stock health, processed-goods usage, and demand response as audit report signals.
- [ ] Preserve existing tier unlock math in the first pass.
- [ ] Add validator coverage for an expanded audit summary.

Acceptance target:

- The first-week loop closes with management feedback about supply, processing, customer demand, and revenue, not only a money total.

## Recommended Next Codex Prompt

Use this after opening or validating the project in Unity:

```text
You are still working only inside C:\Users\sdjsd\Desktop\Unity\Project_PA.
Do not delete files, do not push to GitHub, and do not import external packages.

Next goal: submit or review the generated source/executable packages, then select the final report/presentation attachment.

First read PROJECT_PA_DESIGN_INTENT.md, PROJECT_PA_COMPLETION_PLAN.md, PROJECT_PA_CURRENT_MILESTONE.md, PROJECT_PA_STATUS.md, PROJECT_PA_TODO.md, PROJECT_PA_SESSION_REPORT.md, and README.md.

Please run Builds/Windows/Project_PA.exe and manually repeat the route: move -> first NPC talk -> stock product -> set price -> wait for NPC buy/reject feedback -> confirm money change -> open audit app -> review Day 1 summary.
Fix only blocking visual/readability or executable-route errors and keep changes minimal.
If the executable route passes, prepare source and executable submission packaging while excluding cache/generated folders.

After changes, update PROJECT_PA_STATUS.md, PROJECT_PA_TODO.md, PROJECT_PA_SESSION_REPORT.md, and README.md with what was verified and what remains.
```

## IL-001 + CDN-002 — Real Gathering & Night Shop Gate (2026-06-24)

- [x] IL-001 Added 3 spread wild-forage points (`숲길/해변/들판 채집`) via `DayNightShopLoopController`, reusing `DaytimeStockPrepPoint`, existing Raw items, daily reset, sellable `ItemInstance`. Trigger colliders so NPCs are not blocked.
- [x] Kept Garden Prep Basket / Producer Drop Box as tutorial/NPC-support backup stock.
- [x] CDN-002 Added `IsShopOpenForCustomers` gate + `ShopOpenSign` (IInteractable) open action + HUD status; single guard block in `NpcController.EvaluateCurrentSlot` (no PurchaseEvaluator/ShopSlot/FSM rewrite).
- [x] Day 1 tutorial override (`IsTutorialAlwaysOpen`) preserves the validated first-sale route.
- [x] Save v8 additive extension (`dayPrepCollectedDay` + `dayPrepCollectedActivities`) so same-day gathering survives save/load.
- [x] Added `PA_GatheringShopGateValidator` (33 checks) — passed.
- [x] Re-ran all six existing validators — passed (DayNight validator updated 1 line: collect every point before asserting exhausted).
- [x] Generated 5 1920x1080 screenshots (`Logs/GatheringShopReview/20260624_141557/`), visually reviewed.
- [ ] Human play-feel pass: walk-to-gather, NPC pathing around forage cubes, night-open → customers arrive.
- [ ] Low-poly visual polish for placeholder forage/sign cubes; move forage points to fixed scene terrain.

## Customer Arrival Pacing (IL/CDN follow-up, 2026-06-24)

- [x] Added `CustomerArrivalController` (read-only/event sidecar) that pulls customers to the shop after the player opens it (Day 2+), staggered up to a concurrent cap, via existing `SetShoppingPriority`/`TryForceShop`.
- [x] Day 1 tutorial stays passive (no double-driving with `PlayableDayScenarioController`); paused/shopping NPCs are ignored by `TryForceShop`.
- [x] Registered via `PA_RuntimeSceneBinder`; no `PurchaseEvaluator`/`ShopSlot`/`EconomyService`/FSM/Save change.
- [x] Added `PA_CustomerArrivalValidator` (16 checks) — passed; re-ran all 7 existing validators — passed.
- [ ] Tune invite interval / concurrent cap for feel; optionally bias arrival order by preference/relationship.
- [ ] Low-poly visual polish for placeholder forage/sign cubes (still open from IL-001).

## Day 1-3 Core Slice Playability Pass (2026-06-25)

- [x] Add `PA_CoreSlicePlayabilityValidator` for player-HUD/development-overlay checks.
- [x] Confirm runtime and editor C# builds pass with 0 errors.
- [x] Move the Day/Night phase HUD below the Day 1 objective HUD so the two top-center panels no longer compete for the same space.
- [x] Preserve core systems: shop, shop slot, price UI, inventory, hotbar, economy, purchase evaluation, NPC flow, day/night gate, save/load.
- [ ] Run `Project PA/Validation/Run Core Slice Playability Validation` inside the currently open Unity Editor.
- [ ] Run `Project PA/Validation/Run Final Demo Route Validation` inside the currently open Unity Editor.
- [ ] Run `Project PA/Validation/Run Long Play Progression Validation` inside the currently open Unity Editor.
- [ ] Human Game-view check: F10 toggles development overlays, normal player view hides debug panels/path labels, and Day 1-3 reads as day prep -> night shop -> customer reaction -> settlement -> next-day plan.

## Loop Engineering Dry-Run Guardrails (2026-06-26)

- [x] Create `AGENTS.md` and `CLAUDE.md`.
- [x] Create `Docs/AgentWorkflow/CONTEXT_INDEX.md`.
- [x] Create `Automation/LoopEngineering/loop-policy.json`.
- [x] Create `Automation/LoopEngineering/validator-registry.json`.
- [x] Create `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1`.
- [x] Create `Automation/LoopEngineering/Tickets/LOOP-DRYRUN-001.md`.
- [x] Run preflight and save evidence to `Automation/LoopEngineering/RunLogs/preflight-20260626.json`.
- [x] Fill missing June records in `Docs/07_개발일지.md`.
- [ ] Human reviews dirty Git baseline before any implementation loop.
- [ ] Human reviews/accepts `PROJECT_PA_CRASH_REPORT_20260625.md` as the current graphics-crash baseline.

## VC-001A Village Culture Visual Change (blocked, 2026-06-26)

- [x] Read shared Project_PA loop/design context before implementation.
- [x] Run `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1`.
- [x] Record blocked state in `Automation/LoopEngineering/State/loop-state.json`.
- [x] Add blocked ticket note at `Docs/VillageCulture/VC-001A.md`.
- [ ] Resolve dirty Git baseline gate.
- [ ] Review/accept `PROJECT_PA_CRASH_REPORT_20260625.md` as the D3D11 crash-repair baseline.
- [ ] Rerun preflight until it reports `READY_FOR_BOUNDED_TICKET_LOOP`.
- [ ] After the gate is ready, implement one additive next-day market/plaza visual response from an existing sold category.

Implementation note:

- VC-001A did not start because preflight returned `BLOCKED_BY_DIRTY_GIT`. Do not select a product category or modify scene/code/assets for this ticket until the bounded-ticket loop gate is ready.

## BASELINE-001 Bounded Ticket Loop Baseline Preparation (2026-06-26)

- [x] Verify Project_PA working path and Git root.
- [x] Record current branch and last commit.
- [x] Record `git status --short` and `git diff --stat`.
- [x] Confirm no Unity Editor process for Project_PA was detected during the check.
- [x] Update `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md`.
- [x] Create `Automation/LoopEngineering/State/crash-resolution.json`.
- [x] Record the user's D3D11 manual stability confirmation without claiming D3D12 is fixed.
- [x] Update `Automation/LoopEngineering/loop-policy.json` for D3D11-only bounded automation.
- [x] Update `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1` so valid crash resolution is distinguished from unresolved crash artifacts.
- [x] Re-run preflight after harness correction and confirm it remains blocked by dirty Git.
- [ ] User reviews A/B/C file groups in `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md`.
- [ ] User creates the local baseline checkpoint commit in GitHub Desktop.
- [ ] Rerun preflight after the local checkpoint commit.

Current status:

- `needs_human_review`
- Preflight is still `BLOCKED_BY_DIRTY_GIT` until the user creates or otherwise accepts the baseline checkpoint.
