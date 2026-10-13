# Project PA

Project PA is now being developed as a cozy 3D life and shop management simulation. The player explores and prepares goods by day, opens a personal shop at night, and uses the products they handle and sell to change the village economy and culture over time.

The reverse supply-chain systems remain the economic backbone: NPCs produce, deliver, evaluate, buy, specialize, and help automate repetitive work as the town grows. The shop/stall is the visible hub where daytime effort, NPC support, pricing, customer reactions, settlement, and village change come together.

Before future development, read `PROJECT_PA_CREATIVE_NORTH_STAR.md` first.

## Day 1-3 Core Slice Baseline

The current long-term development baseline is documented in `PROJECT_PA_CORE_SLICE_PLAN.md`.

Purpose:

- Day 1-3 should become the smallest playable version of the final cozy management life-sim.
- Daytime preparation, NPC supply, night shop operation, customer response, settlement, and next-day planning should be readable before more systems are added.

Player-facing view policy:

- Core HUD stays visible: objective, money/tier, clock/date, hotbar, interaction prompt, dialogue, shop price UI, smartphone/audit app, NPC bubble feedback, and Day/Night phase status.
- Development/advisor overlays are hidden by default: LongPlay, Processing, Demand, Village, Customer Preference, and Purchase Feedback side panels.
- Presentation/debug route markers and NPC role badges are hidden by default.
- Press `F10` during runtime to toggle development overlays back on.

Validation note:

- Local C# build passed with `dotnet build Assembly-CSharp.csproj` (0 warnings, 0 errors).
- Unity batch validators could not run in the 2026-06-25 core-slice pass because this project was already open in Unity Editor. Run these from the open Editor menu or close the Editor and rerun batchmode:
  - `PA_FinalDemoRouteValidator.RunFinalDemoRouteValidation`
  - `PA_LongPlayProgressionValidator.RunLongPlayProgressionValidation`

## Unity Version

- Unity 6000.3.2f1
- Main demo scene: `Assets/Scenes/Prototype_FirstDay.unity`
- Build Settings start scene: `Assets/Scenes/Prototype_FirstDay.unity`

## Run The Windows Build

Run:

```text
Builds/Windows/Project_PA.exe
```

Keep the full `Builds/Windows/` folder together when moving the executable. The `.exe`, `Project_PA_Data/`, `UnityPlayer.dll`, and support folders are all required.

## Open The Source Project

1. Open Unity Hub.
2. Add/open this folder:
   `C:\Users\sdjsd\Desktop\Unity\Project_PA`
3. Use Unity 6000.3.2f1.
4. Open `Assets/Scenes/Prototype_FirstDay.unity`.
5. Check the Console before entering Play Mode.

## Demo Route

Use `Prototype_FirstDay.unity` to demonstrate:

1. Start the first-day scene.
2. Follow the objective guidance.
3. Talk to the first guide/settler NPC.
4. Stock a product into a shop slot.
5. Open the price UI and confirm a price.
6. Let an NPC customer approach and evaluate the product.
7. Read the NPC buy/reject feedback bubble and confirm sale/money feedback through the HUD.
8. Open the smartphone audit app to show the next tier/growth target.
9. End the day flow and explain the summary: revenue, feedback, next action, and growth requirement.
10. Explain the market stall as the visible operating hub: supply, processing, price, purchase judgment, revenue, and reinvestment.

Future Milestone 1 development should expand this route into a cozy day-to-night loop:

1. Daytime activity prepares stock through gathering, fishing, mining, farming, decorating, talking, NPC trade, or processing.
2. Night shop opens with display, pricing, customer feedback, and sales.
3. Daily settlement shows revenue, product-category impact, village change, and next-day direction.

## Controls

- Move: `WASD` or arrow keys
- Interact: `Space`
- Inventory: `I`
- Smartphone/apps: `P`
- Crafting: `C`
- Pause/settings: `Esc`
- Hotbar select: number keys `1` through `9`
- Hotbar scroll: mouse wheel
- Build rotate: `R`
- Build/place action: left mouse button when the pointer is not over UI
- Save: `F5`
- Load: `F9`

## Current Validation

- Unity batch compile: passed.
- Automated Play Mode smoke: passed after CL-001 to CL-004.
- Automated final demo route validation: passed.
- Play Smoke core counts: player 1, shops 2, shop slots 8, economy service 1, shop price UI 1, NPCs 8.
- NPC NavMesh check: runtime binder reported 8/8 NPC agents enabled and on NavMesh.
- Windows build: succeeded at `Builds/Windows/Project_PA.exe`.
- Windows player smoke: launched and reached runtime binder/shop startup without the previous NavMeshAgent startup error.
- Editor validation tool: `Assets/Editor/PA_FinalDemoRouteValidator.cs`.
- Final presentation review tool: `Assets/Editor/PA_FinalPresentationReviewer.cs`.
- Final presentation captures: `Logs/FinalPresentation/20260619_232447/`.
- First-pass management clarity work is implemented:
  - Day 1 objective rewrite.
  - NPC purchase/rejection feedback.
  - Day summary management feedback.
  - Next-tier goal text in HUD, audit app, and summary.
- Latest route validation confirmed:
  - NPC dialogue opens.
  - Product stocks from hotbar into a shop slot.
  - ShopPriceUI opens and confirms price.
  - NPC feedback appears in a readable screen-space bubble.
  - Sale updates money and cumulative revenue.
  - MoneyHUD, audit app, and Day 1 summary show growth direction.
- Latest final presentation review confirmed:
  - Market hub/objective/HUD are readable.
  - ShopPriceUI is readable.
  - NPC purchase/rejection feedback bubble is readable and stays in frame.
  - Audit app next-tier goal wraps inside the phone panel.
  - Day 1 summary body fits the panel.
- Latest Windows build after presentation fixes succeeded at `Builds/Windows/Project_PA.exe`.
- Human Windows executable final route: passed on 2026-06-20.
- Submission packages were created under `SubmissionPackages/`.

## Unity Editor Crash Note

On 2026-06-25, Unity Editor crashed through Bug Reporter because of a native D3D12 device-removed error, not a C# compile error.

Repair applied:

- Standalone graphics API is fixed to Direct3D 11 in `ProjectSettings/ProjectSettings.asset`.
- Crash report: `PROJECT_PA_CRASH_REPORT_20260625.md`

Verification after repair:

- `Logs/Codex_CrashRepair_ProjectGraphicsAPI_Load.log`
  - `Version: Direct3D 11.0`
  - `Tundra build success`
- `Logs/Codex_CrashRepair_FinalRoute_D3D11.log`
  - `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`

## Known Remaining Checks

- Human Game-view review remains useful for subjective player feel, but the final Windows executable route has passed.
- Manual executable route verification is complete for the current package candidate.
- A real fullscreen monitor review is still recommended even though generated 1920x1080 presentation captures passed.
- Final submission packaging should exclude cache/generated folders such as `Library/`, `Temp/`, `Logs/`, `.git/`, and source-control/build caches.
- The folder `Project_PA_BurstDebugInformation_DoNotShip/` is generated debug information and should not be included in the final executable package unless specifically required.

## Submission Packages

The current submission package candidates are:

- `SubmissionPackages/Project_PA_Source_20260620.zip`
  - Size: about 359.17 MiB
  - Entries: 1,369
- `SubmissionPackages/Project_PA_Windows_20260620.zip`
  - Size: about 84.25 MiB
  - Entries: 183

Package verification:

- Both zip files open through the .NET zip reader.
- Source package was scanned to confirm `.git/`, `Library/`, `Temp/`, `Logs/`, `Builds/`, `UserSettings/`, `obj/`, `.vs/`, `SubmissionPackages/`, and generated cache folders are excluded.
- Executable package was scanned to confirm `Project_PA_BurstDebugInformation_DoNotShip/`, logs, source folders, cache folders, and generated debug/cache output are excluded.

Source package should include:

- `Assets/`
- `Packages/`
- `ProjectSettings/`
- `Docs/`
- Root `PROJECT_PA_*.md` files
- `README.md`
- `.gitignore`
- Solution/project files if required by the reviewer

Source package should exclude:

- `.git/`
- `Library/`
- `Temp/`
- `Logs/`
- `Builds/`
- `UserSettings/`
- `obj/`, `.vs/`, and cache/generated folders

Executable package should include the runnable Windows build files from `Builds/Windows/`, including:

- `Project_PA.exe`
- `Project_PA_Data/`
- `UnityPlayer.dll`
- `UnityCrashHandler64.exe`
- `DirectML.dll`
- `D3D12/`
- `MonoBleedingEdge/`
- This README or copied run instructions

Executable package should exclude:

- `Project_PA_BurstDebugInformation_DoNotShip/`
- Logs, source folders, cache folders, and generated debug/cache output

## Final Submission File Checklist

Core submission files:

- Source package: `SubmissionPackages/Project_PA_Source_20260620.zip`
- Windows executable package: `SubmissionPackages/Project_PA_Windows_20260620.zip`

Report/presentation candidates in `Docs/`:

- `Docs/202121026-컴퓨터공학과-신동준-졸업작품-제안서.pptx` - original graduation project proposal deck, 8.99 MiB
- `Docs/졸업작품-제안서.pdf` - proposal PDF, 3.99 MiB
- `Docs/Project_PA_Week11_Presentation.pptx` - compact presentation deck, 0.03 MiB
- `Docs/발표_개발현황보고서.md` - development status report draft, 0.01 MiB
- `Docs/발표 스크립트_개발현황.txt` - development presentation script, 0.05 MiB

Suggested final bundle:

- Submit both zip packages above.
- Attach either the proposal PDF/PPTX or the compact presentation deck depending on the course requirement.
- Use the development status report/script as presentation support material if a live explanation is required.

## Full Game Development Mode

Project_PA is now continuing beyond the submitted prototype snapshot as a long-term Project_PA 1.0 development project.

Primary identity:

- Cozy 3D life and shop management simulation.
- Player role: resident-owner-operator who lives in the village by day and runs a shop at night.
- NPC role: neighbors, customers, producers, specialists, helpers, and economic agents.
- Market stall/shop role: visible hub for day-prepared stock, night sales, customer response, revenue, settlement, and village-change signals.
- Reverse supply-chain role: support/automation/growth backbone, not a replacement for the cozy life-sim experience.

Read these before future full-game development:

- `PROJECT_PA_CREATIVE_NORTH_STAR.md`
- `PROJECT_PA_DESIGN_INTENT.md`
- `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md`
- `PROJECT_PA_FULL_GAME_BACKLOG.md`
- `PROJECT_PA_STATUS.md`
- `PROJECT_PA_TODO.md`
- `PROJECT_PA_SESSION_REPORT.md`

Current first full-game sprint:

- FG-001 Core Multi-Day Loop
- FG-002 NPC Producer Economy
- FG-011 Save / Load / Persistence support
- FG-015 Long Play QA support

Implemented first pass:

- `Assets/Scripts/LongPlayProgressionController.cs`
- Day 2-7 long-play objectives.
- Daily NPC producer buy-in deliveries using existing economy/inventory APIs.
- Runtime long-play HUD for daily objective, revenue progress, and delivery result.
- Save schema v7 long-play fields.
- `Assets/Editor/PA_LongPlayProgressionValidator.cs`
- `Assets/Scripts/ProcessingOpportunityController.cs`
- Day 4+ processing opportunity advisor using existing recipes.
- `Assets/Editor/PA_ProcessingChainValidator.cs`
- `Assets/Scripts/CustomerDemandInsightController.cs`
- Day 3+ demand signal advisor using existing purchase evaluation results.
- `Assets/Editor/PA_CustomerDemandInsightValidator.cs`
- `Assets/Scripts/DayNightShopLoopController.cs`
- Day/night phase, shop open/close display, and daytime stock-prep MVP flow.
- `Assets/Scripts/DaytimeStockPrepPoint.cs`
- Runtime `Garden Prep Basket` and `Producer Drop Box` stock sources.
- `Assets/Scripts/VillageChangeSignalController.cs`
- Product-category village direction signal using recent sales logs.
- Day 1 summary `Village direction` section using the village-change signal.
- `Assets/Editor/PA_DayNightShopLoopValidator.cs`
- `Assets/Editor/PA_VillageChangeSignalValidator.cs`

Validation note:

- Unity batch compile after the long-play code changes exited successfully.
- Day 1 route validation passed after the long-play layer.
- Day 2-7 long-play validation passed.
- Day 3 project-local save/load validation passed.
- Long-play validation log: `Logs/Codex_LongPlay_ProgressionValidation.log`.
- Processing chain validation passed with existing `BreadLoaf` recipe.
- Processing validation log: `Logs/Codex_ProcessingChain_Validation.log`.
- Customer demand insight validation passed with existing `BreadLoaf`/Processed signal data.
- Customer demand validation log: `Logs/Codex_CustomerDemand_Validation.log`.
- Day 1 and Day 2-7 regression validations passed after the demand insight observer was added.
- Day/night loop validation passed with two daytime stock-prep MVP sources and Day 1 tutorial access preserved.
- Day/night validation log: `Logs/Codex_DayNight_TwoPrep_Validation.log`.
- Village-change signal validation passed with a Processed product-category signal.
- Village-change validation log: `Logs/Codex_VillageSignal_Validation.log`.
- Day 1 route validation passed after adding the Day/Night, two-prep-source, Village Direction, and Day 1 summary village-direction layers.
- Day 1 validation log: `Logs/Codex_SettlementVillage_Day1Validation.log`.
- Day 2-7 long-play regression validation passed after the settlement summary change.
- Long-play regression log: `Logs/Codex_SettlementVillage_LongPlayRegression.log`.

Current Milestone 1 status:

- Done first pass: day/night phase state.
- Done first pass: readable shop open/close state.
- Done first pass: two daytime stock-prep MVP activities.
- Done first pass: one product-category-to-village-change signal.
- Done first pass: settlement/day-summary integration for category impact.
- Still needed: manual 1920x1080 readability review and richer cozy replacements for the MVP stock sources later.

## Customer Type / Preference Presentation (SPY-002, 2026-06-22)

Read-only presentation layer so NPCs read as residents with their own tastes, and so the player can see why each customer buys or rejects. No purchase math, economy, or NPC FSM logic was changed.

- `Assets/Scripts/UI/CustomerPreferencePresentationController.cs`
  - Top-right "관심 손님 성향" panel for NPCs currently walking to / browsing the shop.
  - Hint uses only traits that actually differ across profiles: `traitSN` (category), `traitTF` (buy style), `traitEI` (eagerness). `priceSensitivity` is uniform 1.0, so no per-NPC price label is fabricated.
- `Assets/Scripts/UI/PurchaseFeedbackPresentationController.cs`
  - Bottom-right "손님 반응" feed: cozy buy/reject reasons (no probabilities) plus a "마을 변화" line tying the sold category to village direction.
- `Assets/Scripts/NpcController.cs`: one read-only hook in `EvaluateCurrentSlot` (same pattern as the demand-insight hook).
- `Assets/Scripts/PA_RuntimeSceneBinder.cs`: registers both controllers.
- `Assets/Editor/PA_CustomerPresentationValidator.cs`: new validator.
- Details and manual checklist: `Docs/CustomerPresentation/README.md`.

SPY-002 validation (all passed):

- `Logs/Codex_SPY002_PresentationValidation.log` (new validator).
- `Logs/Codex_SPY002_FinalRouteRegression.log` (Day 1 route, head bubble unchanged).
- `Logs/Codex_SPY002_DayNightRegression.log`.
- `Logs/Codex_SPY002_VillageRegression.log`.
- `Logs/Codex_SPY002_LongPlayRegression.log`.
- Still needed: manual 1920x1080 readability review of the two new panels.

### SPY-003 Per-Resident Consumption Data (2026-06-22)

Gave each resident distinct `priceSensitivity` / `utilityConsumption` / `luxuryConsumption` values (data only, no `PurchaseEvaluator` code change) so the "가격에 민감/관대" preference hint appears from real data.

- Edited eight `Assets/Resources/NPCs/Profile_*.asset` files (priceSensitivity 0.65 ~ 1.45).
- Price-sensitive: Farmer, Miner. Price-tolerant: Tailor. Others neutral.
- Extended `Assets/Editor/PA_CustomerPresentationValidator.cs` to assert the data-driven price hints.
- Re-validated all five validators: `Logs/Codex_SPY003_PresentationValidation.log`, `Logs/Codex_SPY003_FinalRouteRegression.log` (`paid=30G`), `Logs/Codex_SPY003_DayNightRegression.log`, `Logs/Codex_SPY003_VillageRegression.log`, `Logs/Codex_SPY003_LongPlayRegression.log` (`money=4633G` unchanged).

### SPY-002 Panel Layout Validation (2026-06-22)

Automated the panel-overlap review that was previously manual.

- `Assets/Editor/PA_CustomerPanelLayoutValidator.cs`: at 1920x1080, asserts the `관심 손님 성향` and `손님 반응` panels do not overlap MoneyHUD/Demand/Village/each other/the hotbar and stay within screen bounds; captures a Game-view PNG to `Logs/CustomerPanelReview/<timestamp>/`.
- Run: `PA_CustomerPanelLayoutValidator.RunCustomerPanelLayoutValidation`.
- Result: the strengthened hotbar check caught a real overlap between the `손님 반응` panel and the hotbar; fixed by raising the panel above the hotbar (`PurchaseFeedbackPresentationController` `anchoredPosition.y` 24 → 170, presentation-only). Re-run passes all checks (`Logs/Codex_PanelLayout_Validation3.log`); clean screenshot at `Logs/CustomerPanelReview/20260622_112554/`.
- All five existing validators were re-run after the panel move and passed (`Logs/Codex_PanelLayout_Reg_*.log`).
- Still recommended: human readability/Korean-font check on a real monitor (geometry is automated, aesthetics are not).

## Reference Policy

`Project_D\Project_D` was used only as ReferencePrototype / CozyMarketPrototype for visual and staging ideas. No ReferencePrototype assets, scripts, materials, prefabs, scenes, or project settings were copied into Project PA.

## Real Daytime Gathering & Night Shop Gate (IL-001 + CDN-002, 2026-06-24)

Milestone 1 is now a real playable loop: gather goods by walking up to forage points during the day, stock and price them, open the shop at night via the shop sign so customers actually buy, settle, and gather again the next day.

- IL-001: `DayNightShopLoopController` creates 3 spread wild-forage points (`숲길/해변/들판 채집`) reusing `DaytimeStockPrepPoint`, existing Raw items, daily reset, and a v8 save extension for same-day gather state. Garden Prep Basket / Producer Drop Box remain as backup/NPC support.
- CDN-002: customers only buy when the shop is open for them — Day 2+ requires the night `ShopOpen` phase and the player opening the shop via `ShopOpenSign`; Day 1 tutorial stays always-open. The gate is a single guard in `NpcController.EvaluateCurrentSlot`; `PurchaseEvaluator`/`Shop`/`ShopSlot`/`EconomyService`/NPC FSM are unchanged.
- Save schema is v8 (additive: `dayPrepCollectedDay`, `dayPrepCollectedActivities`).
- Validation: `PA_GatheringShopGateValidator` (33 checks) + FinalRoute/DayNight/LongPlay/CustomerPresentation/Village/CustomerPanelLayout regressions — all passed. Screenshots: `Logs/GatheringShopReview/20260624_141557/`.
- Details: `Docs/IslandLife/GATHERING_AND_SHOP_GATE.md`.
- Still recommended: human play-feel pass and low-poly visual polish for the placeholder forage/sign cubes.

## Day 1-3 Core Slice Playability Pass (2026-06-25)

Current direction: Project PA is now being developed as a long-term cozy management life-sim, not only as a submission prototype. The Day 1-3 slice is the first playable standard for the full game loop.

- Player-facing view: objective HUD, money/tier, clock, hotbar, interaction prompt, dialogue, ShopPriceUI, audit/smartphone, NPC bubbles, and Day/Night HUD stay available.
- Development/advisor clutter: LongPlay, Processing, Demand, Village, Customer Preference, Purchase Feedback, path markers, route labels, role badges, and screenshot markers are hidden by default through `CoreSlicePresentationMode`.
- Runtime debug toggle: press `F10` in Play Mode to restore/hide development overlays.
- UI readability fix: `DayNightShopLoopPanel` now sits below the Day 1 objective HUD instead of overlapping the same top-center area.
- New validator: `Project PA/Validation/Run Core Slice Playability Validation`.

Validation status:

- Runtime and editor C# builds pass with 0 errors.
- Batch Play Mode validation could not run while the project is already open in Unity Editor. Run the Core Slice, Final Demo Route, and Long Play validators from the open Editor menu, or close the Editor and rerun them in batchmode.

## Loop Engineering Dry-Run Guardrails (2026-06-26)

Project PA now has dry-run-only loop engineering files for safer long-term Codex/Claude work:

- `AGENTS.md` and `CLAUDE.md` define shared agent rules.
- `Docs/AgentWorkflow/CONTEXT_INDEX.md` maps task types to required docs and validators.
- `Automation/LoopEngineering/validator-registry.json` lists existing Unity validators only.
- `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1` checks root, Git status, Unity process state, crash reports, and policy validity without launching Unity.

Latest preflight:

- Result: `BLOCKED_BY_DIRTY_GIT`
- Evidence: `Automation/LoopEngineering/RunLogs/preflight-20260626.json`
- Reason: dirty Git baseline plus existing `PROJECT_PA_CRASH_REPORT_20260625.md` human baseline gate.

The development diary at `Docs/07_개발일지.md` has also been updated with the missing June development records.

## Customer Arrival Pacing (2026-06-24)

Opening the shop at night now actively brings customers in, so the open action feels meaningful.

- `Assets/Scripts/CustomerArrivalController.cs` (read-only/event sidecar): on Day 2+ when the player opens the shop, it invites idle customers one at a time (up to a concurrent cap) using the existing `NpcController.SetShoppingPriority`/`TryForceShop` API; it disperses them on close. Day 1 tutorial stays passive (the scenario controller keeps managing Day 1 flow). `PurchaseEvaluator`/`ShopSlot`/`EconomyService`/NPC FSM are unchanged.
- Validation: new `PA_CustomerArrivalValidator` (16 checks) + all 7 existing validators — all passed (`Logs/Codex_Arrival_*`).
