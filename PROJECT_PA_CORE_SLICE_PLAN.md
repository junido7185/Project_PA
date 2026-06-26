# Project PA Core Slice Plan

Last updated: 2026-06-25

This plan follows `PROJECT_PA_CREATIVE_NORTH_STAR.md` and `PROJECT_PA_DESIGN_INTENT.md`.
Project_PA is a cozy life and shop management game with a reverse supply-chain economy as its backbone. Day 1 to Day 3 must become the smallest playable version of that full game, not a prototype-only checklist.

## One-Line Core Slice Definition

Day 1 to Day 3 teaches the player to live in the village by day, prepare goods through village activity and NPC supply, open the shop at night, read customer response, and use settlement feedback to decide the next day.

## Role In The Full Game

Day 1 to Day 3 should establish the full-game contract:

- The player is a resident-owner-operator, not a worker doing isolated errands.
- NPCs are producers, customers, specialists, and future automation partners.
- The shop is the visible operating hub where supply, display, pricing, customer judgment, revenue, and village change meet.
- Daytime actions create the conditions for night sales.
- Night sales create information and money for the next daytime plan.
- This slice must be expandable to Day 30+, buildings, hiring, relationships, audit, reputation, and town growth.

## Day 1 Goal

Purpose:

- Teach the first visible economy loop without overwhelming the player.

Player-facing beats:

- Arrive in the village and meet the first guide/settler.
- Understand that the shop is an operating hub, not a decorative stand.
- Stock one product.
- Set a price.
- Watch a customer buy or reject.
- See money, customer response, audit/tier direction, and a Day 1 summary.

Management meaning:

- "I can turn prepared goods into shop revenue, and customer behavior tells me what to do next."

## Day 2 Goal

Purpose:

- Convert the first sale tutorial into a repeatable day-to-night operation.

Player-facing beats:

- Gather or receive goods during the day.
- Notice NPC producer supply as an upstream source.
- Prepare at least two product choices.
- Open the shop at night.
- Compare customer reactions to stock and price choices.
- End the day with a clearer next action.

Management meaning:

- "My daytime supply choices shape my night shop options."

## Day 3 Goal

Purpose:

- Introduce customer demand as a planning signal and make the next-day decision matter.

Player-facing beats:

- Review what sold, what was rejected, and which customer type responded.
- Use demand feedback to choose a different product, price, or prep activity.
- See a first village-change signal from product category.
- Save/load should preserve the day, inventory, money, and slice state.

Management meaning:

- "The village economy is reacting to what I sell, and I can plan around that reaction."

## Daytime Activities

MVP activities already present or allowed for the slice:

- Forage/gather goods from Project_PA-owned daytime prep points.
- Receive or buy small producer deliveries from NPC economy support.
- Use a backup prep basket/drop box as a temporary onboarding support source.

Near-term activity upgrades:

- Replace placeholder cubes with diegetic low-poly forage nodes.
- Turn producer delivery into a named NPC interaction or delivery spot.
- Add a small processing prep choice once Day 1 to Day 3 is stable.

## Night Shop Flow

The night shop flow should be:

1. Prepare goods by day.
2. Approach the shop hub.
3. Display goods in shop slots.
4. Set a price.
5. Open the shop when the night phase begins.
6. Customers arrive and evaluate.
7. Buy/reject feedback appears in world or concise HUD form.
8. Money and sales logs update.
9. Settlement summarizes revenue, demand, and next-day direction.

The existing `Shop`, `ShopSlot`, `ShopPriceUI`, `EconomyService`, `PurchaseEvaluator`, `Inventory`, `Hotbar`, `NpcController`, `SalesLogManager`, `AuditService`, `TierService`, and `SaveManager` remain the core systems.

## Customer Types And Purchase Response

Current first-pass customer signals:

- NPC profile traits affect category preference and buying style.
- Price sensitivity data differs by resident archetype.
- Purchase evaluator returns buy/reject, probability, and reason data.
- NPC head bubbles already provide immediate buy/reject feedback.

Core-slice target:

- Keep immediate feedback short and readable.
- Show preference/demand information as contextual management feedback, not permanent screen clutter.
- Later convert customer preference side panels into optional apps, shop ledger, or dialogue hints.

## Settlement And Next-Day Goals

Settlement should answer:

- What sold?
- How much money did the shop make?
- What did customers reject?
- What category is shaping the village direction?
- What should the player prepare tomorrow?

Day 1 should remain guided. Day 2 and Day 3 should gradually shift toward player planning.

## Village Change Signals

Current first-pass signal:

- Recent sales by product category produce a read-only village direction.

Core-slice target:

- Day 1: signal can appear in the end-of-day summary.
- Day 2: signal can suggest what to prepare next.
- Day 3: signal should begin pointing toward a future unlock, facility, or relationship effect.

## Current Visible UI, Labels, And Temporary Objects

Player-facing UI to keep visible:

- `PlayableDayGuideCanvas` / objective text.
- `MoneyHUD_Canvas` / money, tier, next goal.
- `ClockHUD_Canvas` / time and date.
- Hotbar and inventory UI.
- `InteractPrompt_Canvas`.
- `DialogueUI_Canvas`.
- `ShopPriceUI_Canvas`.
- Smartphone and Audit app panels.
- `NpcBubbleUI` head/screen-space customer feedback.
- `FirstDayPrototypeCanvas` startup and Day 1 summary flow.
- `DayNightShopLoopCanvas` for the current phase/shop-open state until it is folded into a cleaner HUD.

Player-facing world objects to keep visible for now:

- Market hub model and product shelves.
- Shop slot markers and price tag markers.
- `PA_MarketSign_Label`, `PA_Supply_Label`, `PA_Process_Label`, `PA_Sale_Label`, and `PA_PriceTagLabel_*` until they are replaced with polished diegetic signs.
- `PA_ShopOpenSign`.
- `DaytimeStockPrepPoint` labels for gather/prep interactions.

Development or presentation elements to hide by default:

- `LongPlayProgressionCanvas`.
- `ProcessingOpportunityCanvas`.
- `CustomerDemandInsightCanvas`.
- `VillageChangeSignalCanvas`.
- `CustomerPreferenceCanvas`.
- `PurchaseFeedbackCanvas`.
- `PA_DemoRoute_VisualMarkers`.
- `PA_PathStep_*`.
- `PA_DemoRoute_Label`.
- `PA_CustomerApproach_Label`.
- `PA_Reinvestment_Label`.
- `PA_EconomicRoleBadge`.
- `PA_ScreenshotCameraMarker_MarketHub`.

These elements are useful for validation, screenshots, and planning, but they make the customer-facing game view feel noisy. They should remain available through a development overlay toggle rather than being deleted.

## Core Systems To Preserve

Do not rewrite these during the core-slice pass:

- `Shop`
- `ShopSlot`
- `ShopPriceUI`
- `Inventory`
- `Hotbar`
- `EconomyService`
- `PurchaseEvaluator`
- `NpcController`
- NPC FSM / schedules / NavMesh startup
- `SalesLogManager`
- `GameClock`
- `DayNightShopLoopController`
- `SaveManager` and save schema migration
- `TierService`
- `AuditService`

## Extension To Day 30+

Day 1 to Day 3 should scale into:

- Week 1: stable daily shop loop and first customer demand patterns.
- Week 2: processing, storage, better producer reliability, and early facility unlocks.
- Month 1: hiring/specialists, building expansion, deeper relationships, audits, and visible village growth.
- Day 30+: less repetitive manual prep, stronger NPC automation, meaningful town identity from product categories, and long-term reputation/economy goals.

## Immediate Implementation Rule

The first implementation should not add another major feature. It should make the current slice readable:

- Hide development/advisor overlays by default.
- Keep core player HUD visible.
- Preserve validation access through a development toggle.
- Keep Day 1 route and long-play validators passing.

