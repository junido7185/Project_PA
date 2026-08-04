# PROJECT_PA_DESIGN_INTENT.md

## 2026-06-21 Creative North Star Update

`PROJECT_PA_CREATIVE_NORTH_STAR.md` is now the highest creative direction for Project_PA.

This document remains valid, but the reverse supply-chain management concept should be interpreted inside a broader cozy 3D life and shop management game:

- Daytime: the player explores, gathers, fishes, mines, farms, decorates, talks, and prepares goods.
- Nighttime: the player opens and operates a small shop, displays goods, sets prices, reads customer feedback, and closes the day.
- Long-term: the categories and quality of goods passing through the shop change the village economy, culture, facilities, NPC behavior, and reputation.
- NPC production, specialists, hiring, tier, audit, and automation are support/growth systems that gradually reduce repetitive labor. They should not erase the player's life-sim experience.
- The stall/shop is both a personal shop and the visible hub of the local supply-chain/village-change loop.

Future Codex work must read `PROJECT_PA_CREATIVE_NORTH_STAR.md` before using this document.

## Purpose

This document locks the original design intent of Project_PA so future Codex work does not drift into a simple shop-selling game or a clone of any reference project.

All future implementation, visual polish, UI, scene staging, and migration work should read this document before making changes.

## One-Line Definition

Project_PA is a cozy 3D life and shop management simulation where the player prepares goods through village life by day, operates a personal shop by night, and uses NPC-supported supply chains to grow and change the settlement over time.

## Core Identity

- Project_PA is about moving from personal effort into supported village-scale operation over time.
- The player may gather, fish, mine, farm, decorate, and prepare goods, especially early, but should not be trapped in repetitive labor forever.
- NPCs are neighbors, producers, consumers, specialists, helpers, and economic agents that keep the island economy moving.
- The main fantasy is living in and improving a visible local economy through daytime preparation, nighttime shop operation, pricing, processing, relationships, personnel decisions, and growth planning.
- Cozy low-poly presentation is a readability layer for complex management systems, not the goal by itself.
- The identity of Project_PA is Pioneer Assistance: helping a settlement grow through systems, relationships, shop operation, audits, tiers, and economic circulation.

## Player Role

The player is a resident-owner-operator: a village resident during the day, shopkeeper at night, and long-term economic planner across weeks.

The player's core responsibilities are:

- Buy products and resources produced by NPCs.
- Decide display prices and sales strategy.
- Route raw goods into processing or specialist production.
- Manage shop slots, stock, product mix, and customer flow.
- Hire, assign, and develop NPC workers.
- Improve relationships, unlock specialists or blueprints, and grow the settlement.
- Respond to audit, tier, revenue, happiness, and operational goals.
- Reinvest profits into better facilities, stronger production chains, and clearer town flow.

Direct manual actions are part of the cozy life-sim fantasy, especially in the early game. The long-term arc should turn those actions into strategic choices by adding NPC support, facilities, specialists, storage, and automation.

## Core Game Loop

1. Morning planning shows money, stock, village needs, and growth direction.
2. Daytime play produces or prepares goods through exploration, gathering, fishing, mining, farming, decorating, NPC trade, or processing.
3. NPC producers and specialists add supported supply as the village grows.
4. Night shop operation turns prepared goods into displays, prices, customer decisions, and revenue.
5. Customer NPCs evaluate the item, price, category, quality, and their MBTI-driven preferences.
6. Daily settlement explains revenue, rejection/buy feedback, product category impact, and next-day goals.
7. Revenue and product categories support tier growth, hiring, facility expansion, village identity, audit success, and new production routes.
8. New workers, buildings, and blueprints reduce repetitive labor while expanding the player's strategic choices.

## Reverse Supply-Chain Structure

Project_PA should preserve the four-part reverse supply-chain structure described in the planning documents, but present it as the economic backbone behind the cozy day-to-night loop:

- Process 1.0: Resource production by primary producer NPCs such as farmers, miners, lumberjacks, fishers, and other workers.
- Process 2.0: Distribution and buy-in, where the shop/player purchases NPC output and moves ownership into the store economy.
- Process 3.0: Secondary processing, where raw goods become higher-value processed goods through workshops, crafting, or specialist NPCs.
- Process 4.0: Final sale and reinvestment, where shop slots, display price, NPC preferences, and customer decisions convert goods into revenue and growth.

The design should always make this loop easier to understand, easier to play, easier to test, and easier to show in a final demo.

The loop must not erase life-sim activities. It should explain how daytime activity, NPC supply, processing, night sales, and village growth connect.

## NPC Role

NPCs are not decorative crowds and they are not only customers.

NPCs should function as:

- Producers who create resources through jobs and schedules.
- Consumers who evaluate products and prices.
- Specialists who process goods or unlock higher-value production.
- Social agents whose friendship and personality affect recruitment, blueprints, schedules, and buying behavior.
- Economic signals that show whether the player's management choices are working.

MBTI, schedules, purchase evaluation, job roles, and relationship systems are part of the project's identity because they turn the town into an economy rather than a static market scene.

## True Meaning of the Stall and Shop

The market stall is not just a pretty prop.

The stall/shop is the visible operating hub of the reverse supply-chain loop. It should communicate:

- What goods entered the economy.
- Which goods are being displayed.
- What price the player chose.
- Which NPCs are interested or rejecting the price.
- When a sale succeeds.
- How revenue, tier growth, and shop expansion are connected.
- How the town economy passes through the player's management decisions.

Future stall visual work should make the economic loop visible first. Visual appeal matters, but "beautiful shop" is secondary to "readable operating hub."

## UI Role

The UI is a management cockpit for a cozy economic simulation.

It should help the player understand:

- Current money, day/time, tier, audit, and operational goals.
- Item identity, quality, stock, cost basis, and display price.
- NPC feedback such as purchase interest, price rejection, job state, or relationship state.
- Smartphone, hiring, audit, crafting, shop, and dialogue flows.
- Why an economic event happened, not only that it happened.

UI polish should prioritize readability, decision clarity, and final-demo explanation over decorative styling.

## Visual Reference Boundaries

Project_D, the ReferencePrototype, CozyMarketPrototype, and Docs/VisualTargets are reference material only.

They may be used for:

- Market readability ideas.
- Stall silhouette and staging inspiration.
- NPC route clarity.
- Product display density.
- Cozy low-poly color, lighting, and scale direction.
- UI readability principles.

They must not be used to:

- Replace Project_PA's identity.
- Copy copyrighted commercial assets, names, logos, exact UI layouts, or exact scene compositions.
- Merge another project into Project_PA.
- Turn the project into a clone of the reference prototype.
- Rename Project_PA systems, scenes, folders, or documents after the reference source.

Project_PA's design intent always takes priority over visual references.

## Things That Must Not Change

- Do not turn Project_PA into a simple shop-selling game.
- Do not trap the player as a repetitive primary laborer for the whole game.
- Do not remove daytime life-sim actions such as gathering, fishing, mining, farming, decorating, and relationship play.
- Do not remove NPC production as the source of economic motion.
- Do not reduce NPCs to decorative customers only.
- Do not treat the stall as only a decorative storefront.
- Do not replace the reverse supply-chain loop with a linear buy-low/sell-high loop.
- Do not remove or bypass the importance of ItemInstance quality/price data, ShopSlot display price, EconomyService flow, PurchaseEvaluator logic, NPC schedules, MBTI-driven behavior, tier growth, audit direction, hiring, friendship, or specialist unlocks.
- Do not make cozy low-poly visuals the project goal instead of the presentation style.
- Do not directly merge or copy the reference prototype into Project_PA.

## Codex Work Priorities

Future Codex work should follow this order:

1. Preserve `PROJECT_PA_CREATIVE_NORTH_STAR.md`.
2. Build the cozy day-to-night shop loop before adding broad feature depth.
3. Preserve and clarify the reverse supply-chain management backbone.
4. Fix Play Mode, build, or data blockers that prevent the loop from being demonstrated.
5. Make the stall/shop read as the operating hub of the economy and village-change loop.
6. Improve UI readability for life/shop/management decisions.
7. Stage NPC producers, customers, neighbors, and specialists so their roles are visible.
8. Keep data-driven systems compatible with existing ScriptableObject, save, economy, tier, audit, hiring, and NPC patterns.
9. Apply visual polish only when it supports cozy play and economic clarity.
10. Avoid broad rewrites, broad merges, external packages, or reference-project copying unless explicitly approved.

## T010 Guardrail

Before starting T010 Market Stall Visual Migration, read this document first.

T010 should not be framed as making a prettier stall. It should be framed as making the shop/stall a readable operating hub where the reverse supply-chain economy can be seen by a player, evaluator, or viewer within the first few moments of the demo.

## WORLD-000 Design Intent Addendum

The player-customizable island is a spatial extension of the same reverse supply-chain loop. Terrain and building placement must clarify where stock is gathered, processed, delivered, displayed, purchased, and reflected back into village life. They must not replace `Shop`, `EconomyService`, `PurchaseEvaluator`, NPC roles, or the Day/Night loop.

Architecture is fixed before map decoration: 2m logical cells, 16×16-cell chunks, limited 1m elevation steps, custom chunk mesh terrain, seed plus sparse modification delta, and role-anchor NPC destinations. Final island silhouette, plaza composition, decoration density, lighting, and landmarks remain later art/design decisions. New world work targets WorldSandbox until integration gates pass; the Golden Regression Scene is not an experiment surface.
