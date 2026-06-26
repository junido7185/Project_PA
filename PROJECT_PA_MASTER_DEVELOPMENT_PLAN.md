# PROJECT_PA Master Development Plan

Last updated: 2026-06-20

This plan follows `PROJECT_PA_CREATIVE_NORTH_STAR.md` first, then `PROJECT_PA_DESIGN_INTENT.md`. Project_PA 1.0 is not a simple shop-sale prototype and not a passive management-only sim. It is a cozy 3D life and shop management game where the player prepares goods through village life by day, runs a personal shop at night, and uses NPC-supported supply chains to grow the town.

## Project_PA 1.0 One-Line Definition

Project_PA 1.0 is a cozy 3D life and shop management sim in which the player explores and prepares goods by day, opens a personal shop at night, and gradually changes the village through sold product categories, customer demand, NPC support, facilities, and reputation.

## Final Game Goal

The final game should let the player feel like a resident-owner-operator. The player can gather, fish, mine, farm, decorate, talk, prepare goods, and run the shop directly, especially early. NPCs produce, consume, judge prices, specialize, build relationships, and gradually reduce repetitive labor. The player reads this economy through the shop, UI, audits, relationships, stock levels, village-change signals, and growth goals, then makes life/shop/management decisions.

## 10+ Hour Play Structure

- Day 1 to Day 7: onboarding, visible stall loop, basic NPC producer intake, pricing, customer response, audit/tier goals.
- Week 2: first expansions, processing chains, storage pressure, early hiring, better customer variety.
- Weeks 3-4: town facility upgrades, reputation targets, multiple producer types, specialist roles, higher-value products.
- Repeatable daily loop: review goals -> do daytime activities or use NPC support -> prepare/process stock -> open night shop -> set prices -> observe NPC demand -> resolve settlement/category impact -> reinvest.

The 10-hour target depends on meaningful variation rather than grind: different NPC needs, supply uncertainty, processing choices, tier requirements, and facility unlocks.

## 30+ Hour Expansion Structure

- Month 2+ seasonal modifiers, larger town districts, advanced production chains, contract orders, specialist training, relationship-driven perks, and reputation branches.
- Multiple town growth paths: food-focused market, materials/industry route, artisan route, logistics route, and hybrid routes.
- Long-term pressure sources: storage limits, worker capacity, audit requirements, demand shifts, quality grades, and town satisfaction.

The 30-hour target should come from strategic breadth and long-term planning, not from copying any existing commercial life-sim structure.

## Core Play Pillars

### Town Growth

The town should visibly change as the economy matures. Buildings, NPC routes, delivery areas, storage, processing stations, and customer spaces should communicate growth.

### Cozy Day-To-Night Life

The player should have tactile daytime actions such as gathering, fishing, mining, farming, decorating, talking, buying supply, or processing stock. These actions feed the shop instead of existing as detached errands.

### NPC Production And Consumption Economy

NPC producers bring resources into the economy and later reduce repetitive labor. NPC customers evaluate goods by price, preference, personality, reputation, and availability. The player manages the flow without losing the life-sim feel.

### Shop Operation

The market stall and later shop facilities are operating hubs. They expose stock, price, demand response, and revenue, rather than functioning as decoration.

### Processing And Crafting

Processing turns raw NPC-produced materials into higher-value goods. The player decides whether to sell raw goods quickly or invest time/resources into value-added products.

### Hiring And Specialists

Hired NPCs should automate or improve parts of the supply chain: production, logistics, processing, customer service, audit prep, or quality control.

### Relationship And Personality

NPC relationships and personalities should affect buying behavior, production reliability, dialogue, hiring, and long-term town mood.

### Audit, Tier, And Reputation

Audits and tiers define management targets. They should measure revenue, reputation, stock health, worker capacity, and town growth.

### Building Expansion

Buildings unlock storage, processing, hiring, higher-tier goods, customer capacity, and town growth. They are economic tools, not only scenery.

### Long-Term Save And Progression

Save/load must preserve money, revenue, day/time, tier, audit state, relationships, hired NPCs, inventory, hotbar, shop slots, buildings, and long-play progression state.

## Day 1 To Day 30 Progression

### Day 1

- Teach talk -> stock -> price -> customer response -> money -> audit/tier summary.
- Preserve the current `Prototype_FirstDay.unity` route as the onboarding baseline.

### Days 2-7

- Add repeatable daily operations.
- Introduce NPC producer buy-ins.
- Show daily objectives and weekly revenue direction.
- Make stock and price decisions matter over several days.

### Days 8-14

- Add basic processing pressure.
- Add storage/logistics constraints.
- Introduce first hiring/specialist decision.
- Expand customer feedback variety.

### Days 15-30

- Add facility expansion goals.
- Add tier/reputation checks beyond simple revenue.
- Add higher-value processing chains.
- Add stronger NPC role differentiation.

## Week And Month Goals

### Week 1

- Stabilize the market hub.
- Understand NPC producer intake and customer response.
- Reach first meaningful audit/tier target.

### Week 2

- Build or unlock at least one processing or storage facility.
- Prepare first hire or specialist support.
- Introduce stronger customer segmentation.

### Month 1

- Establish a visible town economy loop.
- Unlock several production categories.
- Move from a single stall tutorial into a small but self-sustaining local economy.

## Tier Unlock Plan

- Tier 0: first stall, basic stock, price UI, basic NPC shopping, first audit.
- Tier 1: daily producer deliveries, simple processing, storage pressure, clearer demand feedback.
- Tier 2: hiring, specialist roles, town facility expansion, stronger reputation requirements.
- Tier 3: multi-step processing chains, quality grades, broader customer profiles, logistics tools.
- Tier 4+: district expansion, advanced audits, contracts, seasonal economy shifts, long-term town identity.

## NPC Job Group Plan

- Producers: farmers, fishers, woodcutters, miners, gatherers, artisans.
- Consumers: budget shoppers, quality-focused buyers, convenience buyers, loyal regulars, tourists/visitors.
- Specialists: processor, stock clerk, price analyst, delivery helper, audit advisor, relationship host.
- Town agents: builders, inspectors, recruiters, event organizers.

NPCs should be economic agents first. Their visual labels, placement, schedules, dialogue, and data should all support that role.

## Item, Resource, And Processing Plan

- Raw resources: wheat, fish, wood, ore, carrots, seasonal produce, gathered materials.
- Early processed goods: bread, planks, bars, simple meals, bundles.
- Mid-game goods: crafted sets, premium foods, refined materials, packaged goods.
- Late-game goods: specialist products, contract goods, high-reputation goods, town-branded goods.

Each item category should answer: who produces it, who buys it, what facility improves it, what price range is reasonable, and what growth goal it supports.

## Building And Facility Plan

- Market stall: visible operating hub for day-prepared stock, night shop operation, price, demand, settlement, and village-change signals.
- Storage: increases planning capacity and reduces daily pressure.
- Processing stations: create value-added goods.
- Hiring office or board: connects NPC labor to management.
- Audit/administration desk: shows tier, reputation, and risk.
- Logistics point: makes producer delivery and restocking readable.
- Town upgrades: increase customer variety, producer output, and reputation.

## UI And UX Completion Goals

- UI is the management cockpit.
- HUD must show day/time, money, tier, current objective, next growth target, and immediate operational feedback.
- Shop price UI must show item, current price, price suitability, likely customer response, confirm/retrieve/close.
- Audit UI must explain why the player passed or failed and what to improve next.
- Long-play UI must show current day goal, daily revenue progress, producer delivery state, and next strategic focus.

## Art And Scene Completion Goals

- Cozy low-poly visuals should make the economic system easier to read.
- The market hub should show stock, supply intake, price points, customer route, and NPC roles.
- Props should support gameplay clarity: delivery boxes, customer pads, role badges, storage areas, processing cues.
- Visual polish must not obscure interaction prompts, shop slots, NPC bubbles, or management HUD.

## Save And Load Completion Criteria

Save/load is complete for 1.0 when it reliably preserves:

- money and cumulative revenue
- day and time
- current tier, reputation, and audit state
- inventory, hotbar, stocked shop slots, prices, and item metadata
- buildings and town upgrades
- friendship and hired NPC state
- long-play day state and daily producer delivery progress

## Future Demo Build Separation Strategy

- Keep long-term development on the main project.
- Preserve `Prototype_FirstDay.unity` as the current onboarding/demo scene.
- When a new demo is needed, create a separate demo milestone branch or scene copy after the full-game systems are stable.
- Demo builds should be snapshots of full-game systems, not a separate design direction.

## Current Implementation Reclassification

### Already Strong Foundations

- `Prototype_FirstDay.unity` is a working first-day route.
- Shop slot stocking, price UI, NPC purchase evaluation, money, cumulative revenue, tier/audit UI, inventory/hotbar, and save/load already exist.
- `GameClock` already supports multi-day time and `OnNewDay`.
- NPC, hiring, friendship, production data, crafting, buildings, and runtime scene binding already exist as extension points.
- Submission build/package artifacts are preserved and should not constrain the new 1.0 direction.

### Demo-Bound Areas

- `PlayableDayScenarioController` is currently Day 1 oriented.
- First-day summary and route markers are useful but not enough for long-term play.
- Build settings are submission-demo oriented, with `Prototype_FirstDay.unity` as the first scene.

### Structures That Block Long Play If Left Alone

- No fully designed Day 2+ operating loop before this update.
- Producer supply exists but is not yet a full recurring economy.
- Processing, hiring, relationships, and town growth need stronger daily integration.
- UI needs a long-play management layer beyond the Day 1 guide.

### Expandable Systems

- `GameClock.OnNewDay` can drive recurring daily systems.
- `ProductionData` and producer NPC logic can feed supply.
- `EconomyService`, `PurchaseEvaluator`, and `ShopSlot` can continue to handle sale economics.
- `TierService` and `AuditService` can become broader progression evaluators.
- `SaveManager` can migrate small additional fields safely.

### Systems To Refactor Carefully Later

- Day/scenario progression should eventually move from tutorial-specific stage logic into data-driven objectives.
- Production chains need a clearer data model for daily yield, quality, and facility modifiers.
- UI should consolidate overlapping objective/HUD layers once long-play information grows.

### Risky Core Systems To Avoid Rewriting Broadly

- `Shop`, `ShopSlot`, `ShopPriceUI`
- `Inventory`, `Hotbar`, `ItemInstance`
- `EconomyService`, `PurchaseEvaluator`
- `NpcController`, `ProducerNpcController`
- `SaveManager`, `SaveData`
- `TierService`, `AuditService`
- `HiringService`, `FriendshipService`

## First Full-Game Sprint

Previous selected sprint: FG-001 Core Multi-Day Loop + FG-002 NPC Producer Economy.

Goal:

- Preserve the Day 1 route.
- Add a safe Day 2-7 repeatable operations layer.
- Let daily NPC producer supply enter the inventory through the existing economy/inventory systems.
- Show long-play daily goals and next growth direction without rewriting core systems.

Implementation status:

- Added `LongPlayProgressionController` as a sidecar runtime service.
- Added Day 1-7 plan data inside the controller for the first pass.
- Added daily producer buy-in deliveries for Days 2-7.
- Added long-play HUD text for current day goal, revenue target, and delivery result.
- Added v7 save fields for long-play delivery/day baseline state.
- Added an Editor validator for the Day 2-7 loop.
- Validated Day 1 route after the long-play layer.
- Validated Day 2-7 producer deliveries and long-play HUD in Play Mode automation.
- Validated Day 3 project-local save/load for long-play state.

Next sprint:

- FG-004 Processing And Crafting Chain.
- First pass should expose raw-vs-processed value decisions using existing recipes and item prices.
- Do not rewrite existing crafting behavior; add a management/readability layer first.

FG-004 first-pass status:

- Added `ProcessingOpportunityController`.
- Added a Day 4+ processing focus panel.
- Added `PA_ProcessingChainValidator`.
- Validated an existing raw -> processed chain through `CraftingService.TryCraft`.
- Long-play regression still passes after adding the processing advisor.

Next sprint recommendation:

- FG-003 Customer Simulation.
- Start with customer demand/category insight and recent buy/reject reasons.
- Do not rewrite `PurchaseEvaluator`; expose its signals more clearly first.

FG-003 first-pass status:

- Added `CustomerDemandInsightController`.
- Added a Day 3+ demand signals panel.
- Added `PA_CustomerDemandInsightValidator`.
- Added a read-only observer hook after NPC purchase evaluation.
- Validated demand insight generation.
- Day 1 and long-play regressions still pass after the observer hook.

Next sprint recommendation:

- Creative North Star Milestone 1 implementation.
- Add day/night phase flow, shop open/close state, two daytime stock-prep activities, daily settlement, and one product-category village-change signal.
- Use the already visible supply, processing, and demand signals as support. Preserve existing tier unlock math in the first pass.
