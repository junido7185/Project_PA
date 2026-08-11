# Project PA Current Milestone

## M85 Gameplay Beta — Active (2026-08-11)

- Branch: `milestone/gameplay-beta-85`; clean starting baseline: `b176bdcac8d140ffa53e8b907ef7da3576a44a70`.
- Completion target: `M85_GAMEPLAY_BETA_COMPLETE` — a new player can understand and play Day 1, continue through Day 7, earn and spend money, see customer/village response, and resume from save without developer guidance.
- Preapproved bounded sequence: `BETA-001 → BETA-002 → BETA-003 → BETA-004 → BETA-005 → BETA-006 → BETA-007 → BETA-008 → BETA-009 → BETA-010`.
- `BETA-001 Player Onboarding and World Readability` is complete: WorldSandbox now starts at Day 1 09:00 with a new-life prompt, WASD objective, shop/workbench landmark route, compact player HUD, and WORLD developer surfaces hidden until F10.
- Current single active ticket: `BETA-002 Daytime Activity Completion`.
- M70 remains a protected regression foundation. `Prototype_FirstDay` remains the Golden Regression Scene; no MainGame integration is implied.

Date: 2026-06-19

## 2026-06-21 Current Direction

This milestone now follows `PROJECT_PA_CREATIVE_NORTH_STAR.md`.

The previous "First Playable Management Loop" work is preserved as completed support for the night-shop/economy phase, but it is no longer the full Milestone 1 definition.

## Milestone 1 - Cozy Day-To-Night Shop Loop

The current milestone is to make the prototype reliably communicate and execute the cozy day-to-night loop:

daytime village activity
-> player prepares or obtains goods
-> player opens the shop at night
-> player displays goods
-> player sets price
-> NPC customers buy or reject based on price/preference
-> revenue and product category impact update
-> village/tier/audit/next goal updates
-> day summary points to the next life/shop/growth action

## Why This Milestone Comes First

Project PA already has many systems: inventory, shop slots, price UI, economy, NPC AI, crafting, hiring, friendship, tier, audit, save, runtime binders, and first-pass long-play support.

The nearest problem is not lack of systems. The nearest problem is that the first playable route must explain itself as a cozy life/shop game whose economic backbone is the reverse supply-chain loop.

Milestone 1 therefore focuses on clarity, feedback, and loop reliability across day preparation, night shop operation, customer response, settlement, and first village-change signal.

## Success Criteria

- `Prototype_FirstDay.unity` opens and enters Play Mode.
- Player can move and interact.
- Player can complete at least two daytime-preparation actions or their temporary MVP equivalents.
- Player can receive, collect, buy, process, or otherwise access a sellable product.
- Player can understand when the shop is open/closed or when the shop phase begins.
- Player can stock a `ShopSlot`.
- Player can set or confirm a price.
- At least two customer types can buy or reject.
- The game explains why the NPC bought or rejected.
- Money changes are visible.
- At least one product category has a visible village/growth effect.
- Current objective advances.
- Day summary or next-goal feedback appears.
- The new text/UI does not block gameplay.
- The result preserves Project PA's cozy day-to-night shop identity and its reverse supply-chain backbone.

## Milestone 1 Required Feature Targets

- Day phase with at least two activities such as gathering, fishing, mining, farming, decorating, talking, buying supply, or processing.
- Stock preparation information.
- Night shop opening/closing.
- Product display and pricing.
- At least two customer types.
- Buy/reject feedback.
- Revenue and daily settlement.
- Next-day change or unlock.
- At least one product category affecting village change.

## Previous Management Loop Work

CL-001 through CL-004 remain valid and useful. They are now considered completed support work for the night-shop management slice of Milestone 1.

## Priority A - Complete First Playable Management Loop

### CL-001 - Management Loop Objective Rewrite

Status: Done for first pass; manual readability review pending

Goal:

Rewrite first-day objectives so they describe management decisions instead of only simple actions.

Target copy direction:

- "NPC supply is arriving."
- "Accept or stock a product into the stall economy."
- "Set a price and watch customer reaction."
- "Use the result to plan the next day."

Completion criteria:

- [x] Objective text mentions supply, pricing, selling, revenue, audit/tier review, saving, and next management action.
- [x] The flow still uses existing `PlayableDayScenarioController`.
- [x] No core shop/economy logic rewrite.
- [ ] Manual camera/UI readability review.

### CL-002 - NPC Purchase Feedback

Status: Done for first pass; bubble readability review pending

Goal:

Show why a customer bought or rejected an item.

Completion criteria:

- [x] Buy/reject result produces short readable feedback.
- [x] Feedback appears through existing `NpcBubbleUI`.
- [x] Recent feedback is recorded for the day summary.
- [x] The deterministic purchase evaluator is preserved.
- [ ] Manual check that bubble length/position is readable.

### CL-003 - Day Summary Improvement

Status: Done for first pass; full-route confirmation pending

Goal:

Make day summary act like management feedback.

Completion criteria:

- [x] Summary includes sales count, revenue, money delta, and relationship points if available.
- [x] Summary includes recent purchase/rejection interpretation.
- [x] Summary includes next action such as adjust price, restock, or prepare better goods.
- [x] Summary includes next tier/growth requirement from `TierDefinition`.
- [ ] Manual full-route test to confirm the summary appears at the right time.

### CL-004 - Tier 0 Goal Clarity

Status: Done for first pass; layout review pending

Goal:

Make Tier 0 progress toward Tier 1 understandable.

Completion criteria:

- [x] Player can see next-tier direction in `MoneyHUD`.
- [x] Player can see next-tier direction in `AuditResultUI`.
- [x] Day summary includes current-to-next tier requirements from `TierDefinition`.
- [x] Tier progress does not require reading code or external docs.
- [ ] Manual 1920x1080 layout review to confirm the expanded HUD does not overlap important UI.

## Priority B - Day 1 To Day 3 Structure

- Day 1: first sale and first feedback.
- Day 2: repeat producer/customer loop.
- Day 3: revenue target or audit preparation.
- Connect day progression to save/load only after the Day 1 loop is reliable.

## Priority C - Tier 0 Completion

- Define Tier 0 as the survivor/operator onboarding tier.
- Make Tier 1 requirement visible.
- Connect Tier 1 to slot expansion or shop authority.
- Keep balancing simple until the loop is stable.

## Priority D - UI Explanation

The first UI improvements should answer:

- What am I managing now?
- What did the NPC do?
- Why did the sale happen or fail?
- What changed in money/tier/goal?
- What should I do next?

## Priority E - Visual / Audio / Presentation

Continue visual/audio work only when it supports management clarity:

- Market hub readability.
- NPC role readability.
- Product/price readability.
- Sale/rejection feedback.
- Day summary presentation.

## Implementation Rules

- Reuse `PlayableDayScenarioController`, `Shop`, `ShopSlot`, `ShopPriceUI`, `EconomyService`, `TierService`, `PurchaseEvaluator`, `NpcController`, and `NpcBubbleUI` where possible.
- Do not rewrite core systems broadly.
- Do not copy ReferencePrototype assets.
- Do not import packages.
- Stop if a change requires breaking existing serialized scene references.

## Current Verification Baseline

Known automated baseline before milestone implementation:

- Batch compile passed.
- Automated Play Smoke passed.
- Runtime counts included player 1, shops 2, shop slots 8, economy service 1, shop price UI 1, NPCs 8.
- NPC agents were 8/8 on NavMesh.
- Windows build succeeded.
- Windows player smoke reached runtime binder and shop startup without the previous NavMesh startup error.

Manual full-route verification is still required after CL-001 to CL-003.

## Implementation Update - 2026-06-19

Implemented in this session:

- CL-001 objective copy rewrite.
- CL-002 NPC purchase/rejection feedback.
- CL-003 Day 1 management summary.
- CL-004 next-tier goal visibility in HUD, audit app, and summary.

Automated verification after CL-004:

- Unity batch compile passed.
- Automated Play Smoke passed with player=1, shops=2, shopSlots=8, economyServices=1, shopPriceUI=1, npcs=8, npcAgents=8/8, npcAgentsOnMesh=8.
- Windows build succeeded.
- Windows player smoke reached runtime binder startup with NavMesh/NPC agents ready.

Still required:

- Manual Unity Editor full-route verification.
- Manual executable full-route verification.
- UI/bubble readability review at 1920x1080.

## Milestone 1 Implementation Update - 2026-06-21

First implementation pass completed:

- Added `DayNightShopLoopController` for `DayPreparation`, `ShopOpen`, and `Settlement` state.
- Added readable shop open/close state without blocking the existing Day 1 tutorial route.
- Added `DaytimeStockPrepPoint` as two daytime stock-preparation MVPs: `Garden Prep Basket` and `Producer Drop Box`.
- Added `VillageChangeSignalController` to show a read-only product-category village direction signal from recent sales.
- Added a Day 1 summary `Village direction` section so settlement feedback includes product-category impact.
- Registered the new sidecar services through `PA_RuntimeSceneBinder`.

Validation:

- `PA_DayNightShopLoopValidator` passed.
- `PA_VillageChangeSignalValidator` passed.
- `PA_FinalDemoRouteValidator` passed after the new Milestone 1 layer.
- `PA_LongPlayProgressionValidator` passed after the new Milestone 1 layer.

Remaining Milestone 1 gaps:

- Review the new phase and village-direction panels in an actual Game view.
- Replace the MVP stock sources with richer cozy daytime activities later, if desired.
- Keep shop open/close permissive until customer gating can be tested without breaking Day 1.

## Milestone 1 Implementation Update - 2026-06-22 (SPY-002)

Implemented SPY-002 "고객 타입 / 성향 프레젠테이션" (the last required Milestone 1 customer-type item):

- Added `CustomerPreferencePresentationController` — a read-only top-right "관심 손님 성향" panel that reads real `NpcProfile` traits (`traitSN` category preference, `traitTF` buy style, `traitEI` eagerness) for NPCs that are actively walking to or browsing the shop.
- Added `PurchaseFeedbackPresentationController` — a bottom-right "손님 반응" feed that turns each `PurchaseEvaluator.Result` into a short cozy buy/reject reason and adds a "마을 변화" tie line connecting the sale category to village direction. No probabilities or debug fields are shown.
- Added one read-only hook in `NpcController.EvaluateCurrentSlot`, mirroring the existing customer-demand-insight hook. Purchase probability, sale, money, inventory, and NPC FSM behavior are unchanged.
- Registered both controllers through `PA_RuntimeSceneBinder`.
- The existing head bubble (with %) from CL-002 is preserved unchanged.

Data honesty note: `priceSensitivity`, `utilityConsumption`, and `luxuryConsumption` are 1.0 on all eight profiles, so no per-NPC "price sensitive" label is fabricated. Only traits that actually differ (`traitSN`/`traitTF`/`traitEI`) drive the hint; the reject reason's price factor comes from the real per-decision display/base price ratio.

Validation (all passed):

- `PA_CustomerPresentationValidator` — `Logs/Codex_SPY002_PresentationValidation.log`.
- `PA_FinalDemoRouteValidator` — `Logs/Codex_SPY002_FinalRouteRegression.log` (`stocked=BreadLoaf, paid=30G`, head bubble text unchanged).
- `PA_DayNightShopLoopValidator` — `Logs/Codex_SPY002_DayNightRegression.log`.
- `PA_VillageChangeSignalValidator` — `Logs/Codex_SPY002_VillageRegression.log`.
- `PA_LongPlayProgressionValidator` — `Logs/Codex_SPY002_LongPlayRegression.log`.

Still required: manual 1920x1080 Game-view readability review of the two new panels (checklist in `Docs/CustomerPresentation/README.md`).

## Milestone 1 Implementation Update - 2026-06-24 (IL-001 + CDN-002)

Turned Milestone 1 from "display-only day basket + always-open shop" into a real playable loop:
gather by day → stock/price → open the shop at night so customers can buy → settle → next-day gather resets.

IL-001 real daytime gathering:
- `DayNightShopLoopController.EnsureDayPrepPoints` now creates 3 spread wild-forage points (`숲길 채집`/Carrot, `해변 채집`/Fish, `들판 채집`/Wheat) in addition to the existing Garden Prep Basket and Producer Drop Box (kept as tutorial/NPC-support backup). Reuses `DaytimeStockPrepPoint` (IInteractable), existing Raw items, daily reset, and sellable `ItemInstance` grants. Forage colliders are triggers so NPC movement is not blocked.
- Same-day-once gathering and next-day reactivation reuse the existing `_prepCollectionDays` day comparison.

CDN-002 real night shop gate:
- Added `IsShopOpenForCustomers` gate. Day 1 tutorial stays always-open (`IsTutorialAlwaysOpen`); Day 2+ requires the `ShopOpen` phase AND the player opening the shop via a new `ShopOpenSign` (IInteractable). The gate is a single guard block in `NpcController.EvaluateCurrentSlot` (no `PurchaseEvaluator`/`ShopSlot`/FSM rewrite); closed-shop NPCs hold and wander instead of buying.

Save v8 (additive, mirrors the v7 long-play pattern):
- `SaveData.dayPrepCollectedDay` + `dayPrepCollectedActivities`; `SaveManager` v7→v8 migration + save/restore hooks; `DayNightShopLoopController.WriteSaveFields/RestoreSavedState` so same-day gathering survives save/load.

Validation (all passed): new `PA_GatheringShopGateValidator` plus FinalRoute / DayNight / LongPlay / CustomerPresentation / Village / CustomerPanelLayout regressions. Details and screenshots: `Docs/IslandLife/GATHERING_AND_SHOP_GATE.md`.

Still required: human play-feel pass (walk-to-gather distance, NPC pathing around forage cubes, night-open → customers arrive) and low-poly visual polish for the placeholder forage/sign cubes.

## VC-001A Village Culture Visual Change - 2026-06-26

Goal:

- Add the first small next-day village/market visual response from the previous day's sold product category.

Current status:

- Implemented and validated.
- Preflight returned `READY_FOR_BOUNDED_TICKET_LOOP`.
- Selected category: `Processed`, based on the existing Day 1 `BreadLoaf` sale route and current Village Direction data.
- Added `VillageCultureVisualController` and `PA_VillageCultureVisualValidator`.
- Runtime visual root: `PA_VillageCulture_Processed`.
- The visual is inactive at Day 1 start, remains inactive immediately after sale, and appears on next `DayPreparation`.

Why it matters for this milestone:

- It moves Milestone 1 from text-only village direction toward a visible village response.
- It keeps the reverse supply-chain identity intact: what the player sells becomes part of tomorrow's market space.
- It remains additive: no Save schema, shop logic, economy logic, NPC purchase logic, scene file, ProjectSettings, or Project_D asset change.

Validation:

- `PA_VillageCultureVisualValidator` passed.
- Final demo route, day/night, village signal, long-play, customer presentation, and customer panel layout regressions passed.

Remaining:

- Human visual review should confirm that the primitive processed-goods prep corner is readable and pleasant enough.
- Future category visuals can expand the same pattern for `Raw`, `Utility`, and `Luxury`.

## Long-Play Implementation Update - 2026-07-27 (Task 112)

Day 7 already offered save-and-continue to Day 8, but Day 8+ still used one generic objective. The second week is now connected to existing playable systems:

- Day 8: store reserve stock in B09.
- Day 9: complete a Processed product sale.
- Day 10: operate with at least one hired producer or specialist.
- Day 11: sell two different product categories.
- Day 12: reach Tier 1 and the indoor shop stage.
- Day 13: inspect the active next-day village response.
- Day 14: close the week with two different sold products.

The Day 2+ player checklist reads the actual storage, same-day sales, hired roster, Tier, and village-culture state. It does not mutate economy, inventory, crafting, hiring, tier, village-change, or save authority. Automatic onboarding supply remains limited to Days 2–7.

Validation:

- Runtime and Editor builds passed with zero errors.
- Task 112 static contracts passed 40/40.
- `git diff --check` passed.

Remaining:

- Use an approved safe Unity path to verify Day 7 → Day 8 and representative Day 8–14 state transitions.
- Review the objective/checklist at 1920×1080.

## Visual And Asset-Finalization Update - 2026-07-27 (Task 113)

The durable Tripo policy remains per-asset rather than wholesale replacement: character identity is preserved, functional furniture must satisfy the existing placement/access/save contract, source assets remain untouched, and provenance is a separate deployment gate.

The B12 Trade Port had a concrete coastal-route risk. Its historical wrapper Box/Obstacle is 10×5m while the measured Visual is about 3.63×1.96m. The active runtime instance now derives local bounds from all transformed mesh corners and only shrinks clearly oversized root `BoxCollider` and box-shaped `NavMeshObstacle` axes. Missing mesh data preserves existing physics.

Validation:

- Runtime and Editor builds passed with zero errors.
- Task 113 static contracts passed 20/20.
- Target `git diff --check` passed.

Remaining:

- Use an approved safe Unity path to walk through the former invisible-wall area and stop at the visible dock.
- Verify NPC carving avoidance and capture the same GameCamera view.
- Obtain per-asset Tripo generation and commercial-use evidence before final distribution.

## Long-Play Completion Update - 2026-07-27 (Task 114)

The playable campaign now has an authored first-month endpoint instead of dropping back to a generic loop after Day 14:

- Days 15–30 rotate existing storage, processing, workforce, category assortment, Tier, and village-response decisions.
- Every daily milestone reads the actual runtime state and leaves transaction, unlock, hiring, crafting, and save authority with the existing systems.
- Day 30 Settlement opens a first-month completion record with cumulative economy, Tier/reputation, workforce, village direction, and final-day sales.
- The player can save and finish or save, advance through the existing day authority, and continue on Day 31.
- Day 7 supply cutoff and week-one save → Day 8 remain unchanged.

Validation:

- Sequential Runtime and Editor builds passed with zero warnings and zero errors.
- Task 114 static contracts passed 56/56.
- Target `git diff --check` passed.

Remaining:

- Use an approved safe Unity path to verify representative Day 15–30 state transitions.
- Verify both Day 30 choices, Day 31 continuation/save, and 1920×1080 completion-screen readability.

## Month-One Forge Value-Chain Update - 2026-07-27 (Task 115)

The Month-One attainability audit found that B07's BuildingData, blueprint, Tool Set recipe, and Tool Set item were all Tier 1 while only the placement catalog delayed the forge to Tier 3. Day 23 could also be completed by selling seeds, and Day 24 demanded Luxury before the existing 100,000G Tier-2 gate.

- B07 now joins B05 as a duplicate-safe Tier-1 ledger reward; B06 remains Tier 2 and B08 remains Tier 3.
- Day 23 requires an active forge, an exact same-day Tool Set sale, and a second distinct product.
- Day 24 requires an active forge, an exact Tool Set sale, and a Processed sale.
- The player-facing route is shelf recovery → B07 placement → Plank 1 + Ore 4 → Iron Bars 2 → Tool Set 1 → mixed night sales.
- Month-One cumulative target display now grows from Day 15 16,000G to Day 30 31,000G and continues upward after completion.
- Tier definitions, recipe/item/building assets, crafting/sales/economy/save authority, scenes, prefabs, and packages remain unchanged.

Validation:

- Sequential Runtime and Editor builds passed with zero errors; only existing CS8785/CS0414 warnings remain.
- Executable source contracts passed 39/39.
- Target `git diff --check` passed.

Remaining:

- Verify Tier-1 B05/B07 reward, two-shelf recovery, B07 placement/access, Iron Bar/Tool Set crafting and Day 23/24 sales in an approved safe Unity path.
- Review the guidance and checklist at 1920×1080.

## 2026-08-04 — WORLD-000 Architecture Reframe

Current bounded ticket is WORLD-000 and it is **design/investigation only**. The world runtime, generator, terrain mesh, terraforming, SaveData schema, NavMesh runtime update, and `WorldSandbox.unity` were not created.

- Recommended architecture: 2m cell, 16×16-cell custom mesh Chunk, 1m elevation levels 0~6, 128×128 default logical island, seed + sparse modification delta.
- Scene roles: Prototype_FirstDay = Golden Regression; future WorldSandbox = technology testbed; MainGame = Gate 1~5 이후 별도 통합 후보.
- WORLD-001 is not active. It requires human confirmation of the scene split, WorldSandbox creation, and prototype baseline.
- The pre-existing unverified `AudioManager.cs`/`SalesLogManager.cs` change remains untouched and outside WORLD-000.
- WORLD-000 final loop state is `needs_human_review`; documentation can be complete while implementation gates remain unapproved.
