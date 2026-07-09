# PROJECT_PA Full Game Backlog

Last updated: 2026-06-21

This backlog follows `PROJECT_PA_CREATIVE_NORTH_STAR.md`, `PROJECT_PA_DESIGN_INTENT.md`, and `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md`. Do not convert Project_PA into a simple shop clone, a passive management-only sim, or a clone of any commercial life-sim. Every full-game task should strengthen the cozy day-to-night life/shop loop while preserving the reverse supply-chain systems as the economic backbone.

## 2026-06-21 Backlog Reframe

New top priority is Milestone 1: Cozy Day-To-Night Shop Loop.

Required first:

- Day phase with at least two stock-preparation activities.
- Shop open/close state for night operation.
- Product display and pricing.
- At least two customer types.
- Buy/reject feedback.
- Daily settlement with revenue, next action, and one product-category village-change signal.

Existing FG-001/FG-002/FG-003/FG-004 work remains valid, but should now support this wider loop instead of defining the whole game.

## CN-001 Cozy Day-To-Night Core

- Goal: Build a clear day preparation phase and night shop phase.
- Current state: Day 1 shop route, Day 2-7 long-play objectives, producer delivery, demand insight, and processing advisor exist. First pass now adds `DayNightShopLoopController`, readable phase state, shop open/close display, and one day-prep stock basket while preserving Day 1 route access.
- Needed files/systems: `GameClock`, `LongPlayProgressionController`, `PlayableDayScenarioController`, `Shop`, `NpcController`, HUD/settlement UI.
- Risk: High. Shop open/close and phase behavior can affect NPC purchase flow if changed too broadly.
- Validation: Day 1 route still passes; player can enter day phase, prepare stock, open shop, complete sales, close day, and see settlement.
- Priority: P0.

## CN-002 Island Life Stock Sources

- Goal: Add at least two Project_PA-owned daytime activities or MVP equivalents that create/prepare sellable goods.
- Current state: Inventory and item systems exist; producer deliveries exist; first pass adds two runtime `DaytimeStockPrepPoint` MVP sources: `Garden Prep Basket` and `Producer Drop Box`. Future work should replace or deepen these with richer cozy activities.
- Needed files/systems: `Inventory`, `ItemRegistry`, interactables, item data, save hooks if needed.
- Risk: Medium-high. New activity outputs must not break item metadata, hotbar, shop stocking, or save/load.
- Validation: Activity adds valid sellable `ItemInstance` data; item can be stocked, priced, sold, and saved.
- Priority: P0.

## CN-003 Product Category Village Change

- Goal: Prove that what the player sells changes the village.
- Current state: Demand insight can track item categories; audit/tier UI exists. First pass adds `VillageChangeSignalController`, which reads recent `SalesLogManager` records and shows a read-only village direction signal by product category. Day 1 summary now includes that village direction signal as settlement feedback.
- Needed files/systems: `CustomerDemandInsightController`, `SalesLogManager`, `AuditService`, `TierService`, settlement UI, save data.
- Risk: Medium. Category progress should be clear and bounded, not a hidden grind.
- Validation: Selling at least one category updates a visible village progress signal and appears in settlement/next-day text.
- Priority: P0.

## FG-001 Core Multi-Day Loop

- Goal: Make Day 2+ repeatable with clear daily objectives, next-day transition, revenue review, and weekly progression.
- Current state: Day 1 is playable; `GameClock` supports days; first pass adds and validates `LongPlayProgressionController` for Days 2-7.
- Needed files/systems: `GameClock`, `PlayableDayScenarioController`, `LongPlayProgressionController`, `MoneyHUD`, `AuditResultUI`, `SaveManager`.
- Risk: Medium. It touches progression state and UI, but can remain sidecar if kept small.
- Validation: Compile, enter Play Mode, advance/simulate Day 2-7, confirm day/objective/revenue state.
- Priority: P0.

## FG-002 NPC Producer Economy

- Goal: Let NPC-produced goods enter the economy regularly through buy-ins, contracts, deliveries, or worker output.
- Current state: `ProducerNpcController` and `ProductionData` exist; first pass adds and validates daily Day 2-7 producer deliveries using existing `EconomyService` and `Inventory`.
- Needed files/systems: `ProducerNpcController`, `ProductionData`, `LongPlayProgressionController`, `EconomyService`, `Inventory`, `Item`.
- Risk: Medium. Inventory full and cash shortage cases must be readable.
- Validation: Day 2-7 producer delivery increases sellable inventory and spends/refunds money correctly.
- Priority: P0.

## FG-003 Customer Simulation

- Goal: Expand customers beyond one-off purchases into demand patterns, preferences, budget behavior, and repeat reactions.
- Current state: `NpcController`, `NpcProfile`, and `PurchaseEvaluator` exist with buy/reject feedback. First pass adds and validates `CustomerDemandInsightController` as a read-only demand signal layer.
- Needed files/systems: `NpcController`, `NpcProfile`, `PurchaseEvaluator`, `SalesLogManager`, `NpcBubbleUI`.
- Risk: Medium-high. Customer simulation can break sales if it changes evaluation too broadly.
- Validation: Customers still buy reasonable goods, reject bad prices with readable feedback, and sales logs remain correct. First demand insight validator and Day 1 regression passed.
- Priority: P1.

## FG-004 Processing And Crafting Chain

- Goal: Make processing goods strategically meaningful through raw-to-processed value decisions.
- Current state: `CraftingService`, `CraftingUI`, `RecipeData`, `Workbench`, and processed items exist. First pass adds and validates `ProcessingOpportunityController` as a management-readable advisor.
- Needed files/systems: `CraftingService`, `RecipeData`, `Workbench`, `Inventory`, `Item`, `TierService`.
- Risk: Medium. Recipe costs and output prices can destabilize economy.
- Validation: Create processed items, compare margins, preserve inventory metadata. First validator passed with `BreadLoaf`.
- Priority: P1.

## FG-005 Shop Expansion

- Goal: Expand from a single market hub to more slots, displays, storage, and customer-facing upgrades.
- Current state: Shop slots and market hub visuals exist; shop logic should not be rewritten.
- Needed files/systems: `Shop`, `ShopSlot`, market prefabs/materials, `BuildingData`, `TierService`.
- Risk: Medium. Scene/prefab references can break stocking.
- Validation: Every new slot stocks, prices, sells, saves, and reloads correctly.
- Priority: P1.

## FG-006 Town Growth And Buildings

- Goal: Make town expansion unlock new economic capabilities and visible growth.
- Current state: Building data/prefabs, build manager, grid, and building registry exist.
- Needed files/systems: `BuildManager`, `BuildingData`, `BuildingRegistry`, `GridService`, `SaveManager`.
- Risk: Medium-high. Build placement and save/load can break if references are inconsistent.
- Validation: Build/place/save/load each facility and confirm it changes available management choices.
- Priority: P2.

## FG-007 Hiring And Specialist System

- Goal: Let the player hire NPCs who automate or improve production, processing, stock, pricing, or audit prep.
- Current state: `HiringService`, `HiringUI`, candidates, and hired NPC save fields exist.
- Needed files/systems: `HiringService`, `HiringUI`, `NpcCandidateData`, NPC FSM scripts, `SaveManager`.
- Risk: High. Active NPC state and transforms must remain persistent.
- Validation: Hire, assign, save/load, and confirm specialist effect without breaking NPC movement.
- Priority: P2.

## FG-008 Relationship And Personality System

- Goal: Connect relationships/personality to supply reliability, buying decisions, hiring, and town feel.
- Current state: `FriendshipService`, `FriendshipUI`, dialogue data, and NPC profiles exist.
- Needed files/systems: `FriendshipService`, `NpcProfile`, `NpcDialogue`, `PurchaseEvaluator`, `ProducerNpcController`.
- Risk: Medium. Behavioral modifiers must remain transparent to the player.
- Validation: Relationship points persist and produce clear, bounded effects.
- Priority: P2.

## FG-009 Tier / Audit / Reputation Progression

- Goal: Broaden audits beyond revenue to reputation, stock health, facility growth, and NPC economy health.
- Current state: `TierService`, `TierDefinition`, `AuditService`, and `AuditResultUI` exist.
- Needed files/systems: `TierService`, `AuditService`, `TierDefinition`, `SalesLogManager`, `MoneyHUD`.
- Risk: Medium. Progression gates can block players if targets are opaque.
- Validation: UI explains pass/fail and next target; tier unlocks do not regress existing items.
- Priority: P1.

## FG-010 Inventory / Storage / Logistics

- Goal: Turn storage and logistics into management choices, not only item containers.
- Current state: Inventory/hotbar exist; storage box scripts exist.
- Needed files/systems: `Inventory`, `Hotbar`, `StorageBox`, `StorageUI`, `ItemInstance`, `SaveManager`.
- Risk: Medium-high. Item metadata and stacking must stay reliable.
- Validation: Move goods between inventory/storage/shop, save/load, and confirm counts/quality/price persist.
- Priority: P2.

## FG-011 Save / Load / Persistence

- Goal: Preserve long-term progression across multiple days, buildings, NPCs, storage, economy, relationships, and UI state.
- Current state: Save schema v7 after this sprint, with money, day/time, inventory, shop slots, buildings, audit, friendship, hiring, and long-play fields.
- Needed files/systems: `SaveData`, `SaveManager`, repository classes, all persistent systems.
- Risk: High. Save migrations must be small and tested.
- Validation: Save/load after Day 7, after building changes, after hiring, and after stocked shelves.
- Priority: P0.

## FG-012 UI Management Cockpit

- Goal: Make the UI explain management decisions: supply, stock, price, demand, tier, audit, and growth.
- Current state: Money HUD, objective panel, audit app, price UI, and long-play HUD exist.
- Needed files/systems: `MoneyHUD`, `ShopPriceUI`, `AuditResultUI`, `PlayableDayScenarioController`, `LongPlayProgressionController`.
- Risk: Medium. UI can overlap or over-explain.
- Validation: 1920x1080 and executable review for readability, no blocking overlays.
- Priority: P1.

## FG-013 World Art And Scene Polish

- Goal: Make the market/town visually communicate the economy without copying reference projects.
- Current state: Market hub, route markers, role badges, and visual props exist.
- Needed files/systems: Project_PA-only prefabs/materials, `Prototype_FirstDay.unity`, future town scenes.
- Risk: Medium. Visual props can block player/NPC routes.
- Validation: Player movement, NPC NavMesh, prompt visibility, screenshot readability.
- Priority: P2.

## FG-014 Tutorial And Onboarding

- Goal: Teach management concepts without turning the game into an errand chain.
- Current state: Day 1 route exists and has management-oriented objective text.
- Needed files/systems: `PlayableDayScenarioController`, `LongPlayProgressionController`, dialogue, UI panels.
- Risk: Medium. Tutorial text can become too heavy.
- Validation: New player can complete Day 1 and understand why Day 2 matters.
- Priority: P1.

## FG-015 Long Play QA

- Goal: Validate multi-day loops, save/load, economy balance, UI clarity, and no-progress blockers.
- Current state: Final route validator exists; long-play validator added and passed in this sprint, including Day 3 project-local save/load.
- Needed files/systems: Editor validation tools, logs, scene backups, manual QA checklist.
- Risk: Medium. Automated tests can miss subjective pacing.
- Validation: Batch compile, Play Mode route, Day 2-7 validation, manual executable loops.
- Priority: P0.

## FG-016 Performance / Build Stability

- Goal: Keep builds stable as more NPCs, UI, buildings, and systems are added.
- Current state: Windows build/package succeeded for the submission snapshot.
- Needed files/systems: build settings, URP settings, scene optimization, logs, CI-like batch checks.
- Risk: Medium. More runtime-created UI/objects can increase overhead.
- Validation: Windows build, smoke launch, no startup errors, acceptable FPS in market scene.
- Priority: P2.

## FG-017 Future Demo Build Branch

- Goal: Keep future demo builds separate from full-game development without splitting the design direction.
- Current state: Submission prototype packages exist and are preserved.
- Needed files/systems: README, build settings, version notes, release checklist.
- Risk: Low-medium. Demo snapshots can drift if unmanaged.
- Validation: Demo scene/build can be reproduced from documented steps.
- Priority: P3.

## Previous First Sprint Selection

Previously selected:

- FG-001 Core Multi-Day Loop
- FG-002 NPC Producer Economy
- FG-011 Save / Load / Persistence support for new fields
- FG-015 Long Play QA validation support

First-pass implementation target:

- Preserve Day 1.
- Add Day 2-7 objectives.
- Add daily producer buy-in deliveries.
- Add long-play HUD.
- Save and restore long-play delivery/day baseline fields.
- Validate with Unity compile and a Day 2-7 Editor validator when no other Unity instance is locking the project.

Validation result:

- Unity compile passed.
- Day 1 final route validation passed.
- Day 2-7 long-play progression validation passed.
- Day 3 project-local save/load validation passed.

Next selected backlog item:

- CN-001 Cozy Day-To-Night Core.
- CN-002 Island Life Stock Sources.
- CN-003 Product Category Village Change.
- FG-009 Tier / Audit / Reputation Progression remains important, but should support the new settlement/category feedback loop rather than being the next isolated sprint.

FG-004 first-pass result:

- Processing opportunity advisor added.
- Existing raw -> processed chain validation passed.
- Long-play regression passed after the advisor was attached.

FG-003 first-pass result:

- Customer demand insight advisor added.
- Demand insight validation passed.
- Day 1 and long-play regressions passed after the NPC observer hook was attached.

Milestone 1 first-pass result:

- Day/night phase sidecar added and validated.
- Shop open/close state added as visible/readable state, with Day 1 tutorial access preserved.
- Two daytime stock-prep MVP sources added and validated.
- Product-category village direction signal added and validated.
- Day 1 summary settlement feedback now includes village direction.
- Day 1 final route and Day 2-7 long-play regressions passed after the Milestone 1 sidecar layers.

## SPY-002 Customer Type / Preference Presentation — 2026-06-22

- Goal: make each NPC read as a resident with its own taste, and explain why each buys or rejects, without changing purchase math.
- Result (first pass, validated):
  - Added `CustomerPreferencePresentationController` (read-only "관심 손님 성향" panel from real `NpcProfile` traits).
  - Added `PurchaseFeedbackPresentationController` (cozy buy/reject reason + "마을 변화" tie line; no debug numbers).
  - Added one read-only hook in `NpcController.EvaluateCurrentSlot` (same shape as the demand-insight hook).
  - Preserved `PurchaseEvaluator`, `Shop`, `ShopSlot`, `ShopPriceUI`, `EconomyService`, NPC FSM, Save, and the existing head bubble.
  - Used only traits that actually differ across profiles (`traitSN`/`traitTF`/`traitEI`); `priceSensitivity` is uniform 1.0 so no "price sensitive" label is fabricated yet.
- Validation: `PA_CustomerPresentationValidator` plus FinalDemoRoute / DayNight / Village / LongPlay regressions all passed.
- Docs: `Docs/CustomerPresentation/README.md`.

## Next Sprint Recommendation

Milestone 1 customer-type requirement is now met. Next safe steps:

- Manually review the two new SPY-002 panels in a real 1920x1080 Game view.
- Optionally surface the preference hint as a small over-head tag/bubble (currently panel-based).
- Make `priceSensitivity`/`utilityConsumption`/`luxuryConsumption` differ per NPC so data-driven "price sensitive" hints become honest (SPY-003 candidate).
- Connect customer preference → Village Direction → facility/event unlocks (Milestone 2).
- Replace MVP stock-prep boxes with richer cozy activities when the loop is stable.
- Preserve existing tier unlock math, shop logic, economy logic, NPC purchase logic, and save paths.

## IL-001 + CDN-002 — Real Gathering & Night Shop Gate (2026-06-24)

- Goal: make Milestone 1 a real playable loop — gather by day, open the shop at night so customers actually buy, settle, reset next day.
- Result (validated):
  - IL-001: 3 spread wild-forage points + 2 backup prep points, reusing `DaytimeStockPrepPoint`, existing Raw items, daily reset, and a v8 save extension for same-day gather state.
  - CDN-002: `IsShopOpenForCustomers` gate respected at the NPC purchase entry; `ShopOpenSign` open action; Day 1 tutorial override preserved.
  - No rewrite of `PurchaseEvaluator`/`Shop`/`ShopSlot`/`EconomyService`/NPC FSM/Save structure.
- Validation: `PA_GatheringShopGateValidator` + FinalRoute/DayNight/LongPlay/CustomerPresentation/Village/CustomerPanelLayout — all passed.
- Docs/screenshots: `Docs/IslandLife/GATHERING_AND_SHOP_GATE.md`, `Logs/GatheringShopReview/20260624_141557/`.

## VC-001A Village Culture Visual Change (implemented, 2026-06-26)

- Goal: implement one next-day plaza/market visual response from the previous day's sold product category.
- Backlog fit: supports the village-growth and feedback-loop track by making sales categories visibly affect the market.
- Current status: implemented and validated as the first one-category proof.
- Selected category: `Processed`, using the existing Day 1 `BreadLoaf` sale route and current Village Direction data.
- Result: `PA_VillageCulture_Processed` appears on the next `DayPreparation`, not immediately after the sale.
- Risk status: low for core systems. The feature is additive and does not modify Save schema, scene files, shop/economy/NPC purchase logic, packages, ProjectSettings, or Project_D.
- Validation status: VC-001A validator plus required regressions passed.
- Evidence: `Docs/VillageCulture/VC-001A.md` and `Logs/VillageCultureVisual/20260626_145257/`.
- Next backlog direction: add category variants for `Raw`, `Utility`, and `Luxury` only after human review confirms the first visual reads well.
