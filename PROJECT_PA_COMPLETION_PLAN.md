# Project PA Completion Plan

Date: 2026-06-19
Project root: `C:\Users\sdjsd\Desktop\Unity\Project_PA`

## Design Standard

This plan follows `PROJECT_PA_CREATIVE_NORTH_STAR.md` first, then `PROJECT_PA_DESIGN_INTENT.md`.

Project_PA is now framed as a cozy 3D life and shop management simulation. The player lives in the village by day, prepares goods through activities and relationships, opens a personal shop at night, and uses NPC-supported supply chains to grow the town over time.

The earlier reverse supply-chain concept remains a core system, but it is no longer the only surface fantasy. It is the backbone that turns daytime goods, NPC support, processing, shop sales, and village growth into one loop.

ReferencePrototype and VisualTargets are references only. They do not replace Project PA's identity and should not be copied or merged into this project.

## Definition Of A Complete Game

Project PA is complete when a player can repeatedly live through a cozy day-to-night shop loop across several in-game days and clearly understand both personal and economic consequences:

1. The player plans the day from money, stock, village needs, and growth goals.
2. Daytime activities produce or prepare stock through gathering, fishing, mining, farming, decorating, dialogue, NPC trade, or processing.
3. NPCs increasingly support production, logistics, processing, and automation.
4. The player opens the shop at night, displays items, sets prices, and watches customers react.
5. Customer NPCs evaluate price, item category, quality, and preference.
6. Sales or rejections generate readable feedback.
7. Daily settlement updates money, demand, product category influence, tier/audit/reputation, unlock direction, and next-day goals.
8. The player reinvests into better slots, processing, staff, facilities, village culture, and settlement growth.

A complete build should feel like a small but coherent cozy life/shop management game, not only a technology demo or a simple sales prototype.

## Version 1.0 Goal

Version 1.0 should deliver a stable single-player cozy day-to-night life/shop loop from Day 1 through a multi-week arc.

The target experience:

- Day 1 teaches movement, talk, first stock preparation, display, price, customer reaction, and settlement.
- Day 2 repeats the loop with at least one additional daytime activity and clearer producer/customer behavior.
- Day 3 introduces demand, category impact, revenue, or audit pressure.
- Days 4-7 add simple growth choices: better stock mix, processing, shop open/close rhythm, hiring support, slot expansion, village-change signal, or tier progress.
- The player can save/load the run and understand what to do next.

## Submission Demo vs Long-Term 1.0

Graduation/final submission build:

- Must reliably demonstrate the first playable management loop.
- Needs a clear market hub, readable UI, Windows executable, source package, and final report/presentation material.
- Can leave advanced progression, deeper balancing, and broad content expansion documented as future work.

Long-term 1.0:

- Must support repeatable multi-day play.
- Needs fuller Tier 0 to Tier 1 progression, stable save/load, clearer audits, richer NPC roles, more processing choices, and better feedback history.
- Should remain single-player-first until the management loop is strong.

## Core Game Loop

1. Morning planning: review goals, money, village needs, and stock.
2. Daytime life: gather, fish, mine, farm, decorate, talk, trade, or prepare goods.
3. Supply support: producer NPCs, specialists, and facilities add or improve goods as the village grows.
4. Shop preparation: choose display items, process/store stock, and set up the stall/shop.
5. Night shop: open the shop, set prices, observe customers, and handle buy/reject feedback.
6. Settlement: money, sales log, demand, category impact, tier, audit, and reputation progress update.
7. Next day: village changes, unlocks, requests, or demand shifts point to the next action.

## Milestone 1 Demo Loop - Cozy Day-To-Night Shop Loop

Milestone 1 should be short, reliable, and readable:

1. Day phase introduces at least two activities such as gathering, fishing, mining, farming, decorating, talking, buying supply, or processing.
2. Guide NPC frames the player as a village resident and shop owner.
3. Player prepares or receives sellable stock.
4. Objective UI tells the player to stock a shop slot and prepare for night sales.
5. Shop opens, `ShopPriceUI` explains current price and likely customer reaction.
6. At least two customer types evaluate and buy/reject.
7. Feedback explains why.
8. Money, village/product category signal, and objective update.
9. Day summary recommends the next life/shop/growth action.

## Play Goals Through Day 7

- Day 1: First daytime preparation, first shop opening, first sale, and first feedback.
- Day 2: Repeat day activity and night shop with two or more NPC roles visible.
- Day 3: Introduce demand/category impact, revenue target, or audit preparation.
- Day 4: Introduce processing or specialist value-add.
- Day 5: Introduce hiring/friendship as a strategic lever.
- Day 6: Introduce stock mix, price tuning, decoration, or slot pressure.
- Day 7: Run a simple settlement/audit and show the next village/shop direction.

## Tier Growth Structure

Tier 0: Survivor Operator

- Focus: learn stock, price, and customer evaluation.
- Required clarity: first revenue target, first accepted product, first sale feedback.
- Unlock direction: better stall slots, stronger supply, basic processing.

Tier 1: Branch Manager

- Focus: stable shop operation and basic workforce use.
- Required clarity: slot expansion, audit progress, reliable producer loop.

Tier 2+: Later 1.0 or post-1.0

- Processing depth, specialist chains, automation, broader buildings, richer audits.

## NPC Role Structure

Producer NPCs:

- Create raw goods on schedule.
- Deliver goods or make supply state visible.
- Represent the upstream side of the economy.

Consumer NPCs:

- Visit the stall.
- Evaluate product, price, quality, and preference.
- Explain purchase/rejection in short readable feedback.

Specialist NPCs:

- Process raw goods into higher-value goods.
- Unlock recipes or boost quality through friendship/hiring.

Guide/Settler NPCs:

- Keep onboarding readable.
- Translate the management loop into objectives.

## System Connections

Shop/Stall:

- `Shop` and `ShopSlot` remain the sale surface.
- Stall visuals must expose product, price, customer route, supply route, and reinvestment.

Processing/Crafting:

- Processing should increase value or quality.
- It should not turn the player into the primary laborer.

Hiring/Friendship:

- Hiring is a management lever.
- Friendship unlocks better production, specialists, blueprints, or trust.

Tier/Audit/Growth:

- Tier and audit should give medium-term direction.
- Audit should be feedback, not surprise punishment.

Save/Load:

- 1.0 must preserve money, inventory, displayed stock, player position, day/time, built objects, and key NPC state.
- Multiplayer/cloud persistence remains a future expansion until local single-player save is reliable.

## UI Completion Standard

The UI is a management cockpit. It is complete enough for 1.0 when it clearly shows:

- Current money, day/time, tier, and active goal.
- Item name, price, value/quality, and expected reaction in ShopPriceUI.
- Sale/rejection reason in NPC bubble, prompt, or log.
- Daily sales and next action in day summary.
- Tier 0 to Tier 1 progress.
- Save/load and core menu controls.

UI should prioritize decision clarity over decorative styling.

## Art Completion Standard

The art is complete enough for 1.0 when:

- The market stall reads as the economic hub within 10 seconds.
- Producer, consumer, specialist, and guide roles are visually distinguishable.
- Product slots and price tags are readable.
- Day 1 route is visible from the gameplay camera.
- Cozy low-poly style supports readability.
- Presentation screenshots can explain the game without long narration.

## Build And QA Standard

Before a release candidate:

- Unity compile has no C# errors.
- `Prototype_FirstDay.unity` enters Play Mode.
- Windows build succeeds.
- Executable launches and completes the demo route.
- No blocking NavMesh, UI, input, save, or missing-reference errors.
- Manual route: move -> talk -> receive/stock item -> set price -> NPC buy/reject -> money update -> summary.
- Source package excludes `Library/`, `Temp/`, `Logs/`, `.git/`, and generated caches.

## Current State Re-evaluation

Stable or mostly stable now:

- Player movement and interaction.
- Inventory/hotbar.
- ShopSlot stocking and price UI.
- Economy money service.
- NPC shopping/purchase logic.
- NavMesh runtime startup after recent repair.
- Windows build smoke launch.
- Visual market hub first pass.

Implemented but not yet game-clear:

- Producer/specialist systems exist but Day 1 delivery meaning is not clear enough.
- Tier/audit systems exist but the player needs clearer Tier 0 goals.
- Sales log and feedback exist in pieces, but purchase/rejection reasoning is not prominent enough.
- Day summary needs to act as management feedback.

UI exists but needs stronger explanation:

- Objective text should frame management decisions, not simple errands.
- ShopPriceUI should make price suitability obvious.
- NPC bubbles/logs should explain purchase/rejection.
- HUD should make money/tier/next action obvious.

Missing for long-term 1.0:

- Day 1 to Day 7 progression.
- Robust local save coverage for all relevant systems.
- Clear audit/tier progression loop.
- More production and processing content.
- Final menu, settings, audio feedback, and release packaging.

Must happen before final submission:

- Manual Unity full-route verification.
- Manual executable full-route verification.
- UI readability review.
- README update with verified limitations.
- Package source and executable.
- Select final report/presentation material.

Future expansion:

- Multiplayer/relay/cloud authority.
- Larger shop tiers.
- Deeper automation.
- Broad content expansion beyond Tier 1.

## Things Not To Do Now

- Do not implement multiplayer before the single-player loop is stable.
- Do not rewrite core shop/economy/NPC systems broadly.
- Do not trap the player as a repetitive gatherer for the whole game.
- Do not remove daytime life-sim activities from the core loop.
- Do not copy ReferencePrototype assets or layouts.
- Do not add external packages without approval.
- Do not prioritize decorative art over economic readability.

## Deferred Until Later

- Full multiplayer.
- Cloud save.
- Large-scale building interiors.
- Large content packs.
- Complex automation chains.
- Deep balancing across all tiers.
- Full title/menu polish unless needed for submission.

## Implementation Update - 2026-06-19

Completed first-pass implementation:

- CL-001 Management Loop Objective Rewrite.
- CL-002 NPC Purchase Feedback.
- CL-003 Day Summary Improvement.
- CL-004 Tier 0 Goal Clarity.

Current behavior after this update:

- Day 1 objectives now frame the player as the operator of supply, stock, price, customer response, revenue, and growth.
- NPC customers explain buy/reject decisions with short feedback based on price ratio, product category, and purchase probability.
- Day summary now reports revenue, money delta, sales count, recent feedback, next action, and next-tier growth requirements.
- Money HUD and audit app now read `TierDefinition` data to show the next growth target.

Automated validation after this update:

- Unity batch compile passed.
- Automated Play Smoke passed.
- Windows build succeeded.
- Windows player smoke reached runtime startup with NavMesh/NPC agents ready.

Manual validation still required:

- Complete the full route in Unity Editor and Windows build.
- Check expanded HUD and NPC feedback bubble readability at 1920x1080.
- Package source and executable only after manual route verification.
