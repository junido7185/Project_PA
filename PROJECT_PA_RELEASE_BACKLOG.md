# Project PA Release Backlog

Date: 2026-06-19
Scope: Long-term 1.0, single-player-first.

Status labels: `Todo`, `In Progress`, `Done`, `Blocked`, `Deferred`.

## 2026-06-21 Creative North Star Reframe

This backlog now follows `PROJECT_PA_CREATIVE_NORTH_STAR.md` first.

The previous reverse supply-chain backlog remains useful as the technical/economic foundation, but the full-game direction is now organized around a cozy day-to-night life and shop management loop:

- Day: explore, gather, fish, mine, farm, decorate, meet NPCs, buy or prepare stock.
- Night: open the shop, display products, set prices, watch customer reactions, close the day.
- Growth: sold goods change village demand, culture, facilities, NPC behavior, audits, reputation, and unlocks.
- NPC supply-chain systems become support, automation, and growth systems that reduce repetitive labor over time.

Do not treat the old management-only loop as the entire game. It is the backbone inside the larger life/shop loop.

## New Backlog Categories

### A. Cozy Day-To-Night Core

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Priority |
| --- | --- | --- | --- | --- | --- | --- | --- |
| CDN-001 | Day/Night Phase Flow | Todo | Establish a clear day preparation phase and night shop phase. | Player can see current phase, prepare goods by day, open/close shop at night, and receive settlement. | GameClock, LongPlayProgressionController, UI | High | P0 |
| CDN-002 | Shop Open/Close State | Todo | Make shop operation feel intentional instead of always-on. | Shop has readable open/closed state, customer flow respects it, and Day 1 route still works. | Shop, NpcController, GameClock | High | P0 |
| CDN-003 | Daily Settlement Screen | Todo | Close the loop with money, demand, category impact, and next action. | End-of-day summary explains sales, rejects, village effect, and next-day goal. | EconomyService, SalesLogManager, Audit UI | Medium | P0 |
| CDN-004 | Two Day Activities MVP | Todo | Add at least two meaningful daytime activities feeding shop stock. | Two activity sources create or improve sellable goods without breaking inventory/save. | Inventory, ItemData, scene interactables | High | P0 |

### B. Island Life

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Priority |
| --- | --- | --- | --- | --- | --- | --- | --- |
| IL-001 | Gathering Activity First Pass | Todo | Let the player personally collect basic stock early. | A simple collectable source adds item instances and is saved or respawns safely. | Inventory, ItemRegistry, SaveManager | Medium | P0 |
| IL-002 | Fishing/Mining/Farming Planning | Todo | Define which activity joins the MVP after gathering. | Chosen activity has item outputs, UI prompts, validation, and no external packages. | Docs, Inventory, scene | Medium | P1 |
| IL-003 | Decoration As Village Identity | Todo | Make decoration a future village-expression system. | Decoration tasks are planned as village culture/reputation signals, not only props. | BuildManager, BuildingData | Medium | P2 |

### C. Shop Personality

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Priority |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SPY-001 | Product Category Identity | Todo | Make product type visible and meaningful. | UI/settlement tells whether Food, Material, Crafted, Culture, Tool, or Premium goods shaped demand. | Item data, ShopPriceUI, settlement UI | Medium | P0 |
| SPY-002 | Customer Type Presentation | Todo | Make at least two customer types readable. | Customers show simple type/taste/budget labels and feedback without changing purchase math broadly. | NpcProfile, PurchaseEvaluator, NpcBubbleUI | Medium | P0 |
| SPY-003 | Shop Atmosphere By Stock | Todo | Let stock mix influence presentation later. | Planned: sold/displayed categories change shop/village mood cues. | Shop slots, scene visuals | Medium | P2 |

### D. Village Change

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Priority |
| --- | --- | --- | --- | --- | --- | --- | --- |
| VC-001 | Product Category Village Effect | Todo | Prove that what the player sells changes the village. | At least one category produces visible progress, unlock text, resident reaction, or facility direction. | SalesLogManager, AuditService, UI | High | P0 |
| VC-002 | Village Progress Meter | Todo | Show the player the town is changing. | A simple UI/log tracks one or more village identity axes from sales. | UI, save data | Medium | P1 |
| VC-003 | Facility Unlock From Culture | Todo | Connect category progress to a facility or event. | Reaching a category target unlocks or previews a facility/event. | TierService, BuildingData | High | P2 |

### E. NPC Life

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Priority |
| --- | --- | --- | --- | --- | --- | --- | --- |
| NPC-001 | Neighbor/Customer Dual Role | Todo | NPCs should feel like residents, not vending machines. | At least two NPCs have readable neighbor role plus customer behavior. | NpcDialogue, NpcProfile, NpcController | Medium | P1 |
| NPC-002 | Producer Support Reinterpretation | Todo | Reframe production NPCs as support that grows after early manual play. | Producer deliveries are introduced as help/automation, not replacement of all player activity. | ProducerNpcController, LongPlayProgressionController | Medium | P1 |
| NPC-003 | Relationship Growth Hooks | Todo | Let relationships affect future support/unlocks. | Friendship changes have clear future reward direction in dialogue/UI. | FriendshipService, dialogue | Medium | P2 |

### F. Growth

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Priority |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GR-001 | Milestone 1 Unlock Direction | Todo | Show what Day 2+ unlocks from the first settlement. | Daily settlement names next activity, product category, facility, or customer goal. | LongPlayProgressionController, UI | Medium | P0 |
| GR-002 | NPC Automation Ladder | Todo | Plan how manual activity becomes supported over time. | Backlog defines which NPC/facility reduces gathering, processing, stock, pricing, or logistics labor. | Docs, hiring, producer systems | Low | P1 |
| GR-003 | Tier/Audit Reinterpretation | Todo | Make audits support village growth, not only corporate approval. | Audit feedback includes category impact, resident needs, and shop health. | AuditService, TierService | Medium | P1 |

### G. Presentation

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Priority |
| --- | --- | --- | --- | --- | --- | --- | --- |
| PR-001 | Creative North Star Alignment | Done | Lock the new top direction before implementation. | North Star doc exists and key planning docs reference it. | Docs | Low | P0 |
| PR-002 | Day/Night Readability | Todo | Make phase changes visible and cozy. | Lighting/UI labels/settlement copy distinguish day prep and night shop without scene confusion. | UI, scene, lighting | Medium | P1 |
| PR-003 | Reference Boundary Check | Done | Keep Project_D as reference only. | Docs state no Project_D merge/copy and no commercial clone. | Migration plan, README | Low | P0 |

## Legacy Technical Backlog

The sections below remain useful, but their tasks should be selected only when they support the Creative North Star and the new category structure above.

## Core Loop

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| CL-001 | Management Loop Objective Rewrite | Done | Reframe objectives around NPC goods -> pricing -> sale -> revenue. | Day 1 objective text explains the management loop and updates through sale. | PlayableDayScenarioController | Low | Small |
| CL-002 | NPC Purchase Feedback | Done | Explain why NPCs buy or reject. | Purchase/rejection displays short reason through bubble/log/UI. | PurchaseEvaluator, ShopSlot, NpcBubbleUI | Medium | Medium |
| CL-003 | Day Summary Improvement | Done | Turn day end into management feedback. | Summary shows sales, revenue, feedback highlights, tier progress, next action. | GameClock, EconomyService, SalesLogManager or scenario controller | Medium | Medium |
| CL-004 | Tier 0 Goal Clarity | Done | Make Tier 1 direction visible. | HUD/summary shows current Tier 0 goal and remaining progress. | TierService, AuditService | Medium | Medium |

## NPC Production

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| NP-001 | Producer Delivery Signal | Todo | Make produced goods entering the economy visible. | Producer route/drop-off text or marker communicates delivery. | ProducerNpcController, scene markers | Medium | Medium |
| NP-002 | Starter Supply Reliability | Todo | Ensure Day 1 always has a sellable product. | Fresh scene always gives or delivers at least one valid sellable item. | Inventory, ItemRegistry, PlayableDayScenarioController | Medium | Small |
| NP-003 | Producer Role UI | Todo | Distinguish producer types. | Farmer/miner/etc. labels or dialogue explain what they produce. | NpcDialogue, role badges | Low | Small |

## Shop / Stall

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SS-001 | Stall Hub Manual Review | In Progress | Ensure visual hub does not block gameplay. | Manual Play Mode confirms path, camera, and interaction readability. | Prototype_FirstDay scene | Medium | Small |
| SS-002 | Product Display Readability | Todo | Make stocked goods easier to see. | Stocked item, slot marker, and price tag are readable from camera. | ShopSlot display objects | Medium | Small |
| SS-003 | Slot Expansion Hook | Todo | Connect tier progress to slot expansion meaning. | Tier 0/Tier 1 slot count is explained and visible. | Shop, TierService | Medium | Medium |

## Pricing / Purchase Evaluation

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| PE-001 | Price Suitability Text | Todo | Make price decisions understandable. | ShopPriceUI shows cheap/fair/expensive text and rough chance. | ShopPriceUI, PurchaseEvaluator | Low | Small |
| PE-002 | Purchase Reason Data | Done | Convert evaluator outcome into readable reason. | Buy/reject result carries reason string without breaking deterministic logic. | PurchaseEvaluator, NpcController | Medium | Medium |
| PE-003 | Reaction History | In Progress | Give feedback memory to the player. | Recent buy/reject reasons appear in feed or summary. | SalesLogManager, FeedUI | Medium | Medium |

## Processing / Crafting

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| PC-001 | Day 4 Basic Processing Goal | Todo | Introduce value-add after first sale loop. | Player can process one raw item into one higher-value item. | Workbench, CraftingUI, RecipeData | Medium | Medium |
| PC-002 | Specialist Value Explanation | Todo | Show why specialists matter. | Specialist dialogue/UI explains processing boost or recipe. | SpecialistNpcController, DialogueData | Medium | Small |

## Hiring / Friendship

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| HF-001 | Hiring Panel Route | Todo | Make hiring accessible as a management lever. | Player can open hiring app and understand candidate cost/role. | HiringService, HiringUI | Medium | Medium |
| HF-002 | Friendship Reward Clarity | Todo | Explain relationship strategy. | Dialogue or UI shows friendship level and next unlock direction. | FriendshipService, NpcDialogue | Medium | Medium |

## Tier / Audit / Growth

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TG-001 | Tier 0 Progress Widget | Done | Show progress toward first growth step. | HUD or summary shows revenue/goal and next unlock. | TierService, MoneyHUD | Medium | Medium |
| TG-002 | Audit As Feedback | Todo | Make audits feel like guidance. | Audit result explains strongest/weakest area and next action. | AuditService, AuditResultUI | Medium | Medium |
| TG-003 | Growth Unlock Copy | Todo | Clarify what new tiers change. | Tier-up message names slot/function/building unlock. | TierService | Low | Small |

## Day Progression / Calendar

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DP-001 | Day 1 To Day 3 Script | Todo | Build a reliable early progression arc. | Day 1 sale, Day 2 repeat, Day 3 revenue/audit goal happen in order. | GameClock, PlayableDayScenarioController | High | Medium |
| DP-002 | Day End Trigger | Todo | Let the player understand day completion. | End-of-day can be triggered or naturally reached and shows summary. | GameClock, summary UI | Medium | Medium |
| DP-003 | Next-Day Objective Carryover | Todo | Connect summary to next goal. | Next day starts with objective based on prior result. | Scenario controller, save data | High | Medium |

## Save / Load

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SL-001 | Save Coverage Audit | Todo | Identify what currently persists. | Document money, inventory, day/time, shops, NPC, hiring, friendship coverage. | SaveManager | Medium | Small |
| SL-002 | Shop Stock Save | Todo | Preserve displayed stock and prices. | Save/load restores shop slot items and display prices. | SaveData, ShopSlot | High | Medium |
| SL-003 | Day/Objective Save | Todo | Preserve campaign continuity. | Save/load restores current day and current objective step. | GameClock, scenario controller | High | Medium |

## UI / UX

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| UX-001 | 1920x1080 Readability Pass | Todo | Ensure demo UI is readable. | HUD, objective, price UI, prompt, and bubbles do not overlap key gameplay. | Existing UI | Medium | Small |
| UX-002 | Management Cockpit HUD | In Progress | Put money/day/tier/goal in clear hierarchy. | Player can read current state in under 5 seconds. | MoneyHUD, ClockHUD, TierService | Medium | Medium |
| UX-003 | Feedback Bubble Polish | In Progress | Make NPC reactions readable but unobtrusive. | Bubble appears near NPC for 1-3 seconds and does not block price UI. | NpcBubbleUI | Medium | Small |

## Art / Scene

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| AS-001 | Market Hub Visual First Pass | Done | Make stall read as operating hub. | Hub, slot markers, route markers, role badges exist and verify. | VisualAccelerationBuilder | Low | Done |
| AS-002 | Camera Framing Review | Todo | Confirm the first view explains the game. | Gameplay camera sees player, stall, NPC, product slots, and route. | Prototype_FirstDay scene | Medium | Small |
| AS-003 | Presentation Screenshot Setup | Todo | Prepare report/demo imagery. | Screenshot marker or camera produces a usable composition. | Scene marker | Low | Small |

## Tutorial / Onboarding

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TO-001 | First Settler Dialogue Rewrite | Todo | Explain the operator fantasy. | First dialogue says NPC production and player management matter. | DialogueData, NpcDialogue | Low | Small |
| TO-002 | Objective Step Timing | Todo | Avoid overwhelming the player. | Objective changes only after clear player actions. | PlayableDayScenarioController | Medium | Medium |

## Audio / Feedback

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| AF-001 | Sale/Reject SFX Hooks | Todo | Add immediate economic feedback. | Sale and rejection can trigger distinct optional sounds. | AudioManager or lightweight fallback | Medium | Small |
| AF-002 | UI Click Feedback | Todo | Improve panel feel. | Price confirm/close/hiring buttons have consistent feedback. | UI buttons, AudioManager | Low | Small |

## Build / QA

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| QA-001 | Automated Play Smoke | Done | Ensure scene enters Play Mode. | Smoke reports player/shop/NPC/NavMesh core counts. | VisualAccelerationBuilder | Low | Done |
| QA-002 | Windows Build Smoke | Done | Ensure executable launches. | Player log reaches runtime binder without NavMesh startup error. | Build Settings | Low | Done |
| QA-003 | Manual Full Route Test | Todo | Verify actual gameplay route. | Human completes move -> stock -> price -> sale -> summary. | Unity Editor or executable | Medium | Small |
| QA-004 | Release Package Check | Todo | Verify package content. | Source/exe packages extract and run without cache folders. | Build output, README | Medium | Medium |

## Submission / Presentation

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SP-001 | Final Report Selection | Todo | Choose final report/presentation attachments. | Selected files are listed in README or status. | Docs folder | Low | Small |
| SP-002 | Demo Video Plan | Todo | Optional video path. | Script and capture checklist exist. | Screenshot/camera setup | Low | Small |
| SP-003 | Submission Checklist Closure | Todo | Avoid missed packaging steps. | Status/TODO checklist all final submission items checked or explained. | QA-003, QA-004 | Medium | Small |

## Future Multiplayer

| ID | Name | Status | Purpose | Completion Criteria | Dependencies | Risk | Estimated Scope |
| --- | --- | --- | --- | --- | --- | --- | --- |
| FM-001 | Multiplayer Scope Freeze | Deferred | Keep multiplayer from destabilizing 1.0. | Docs clearly mark multiplayer as post-single-player-stability. | Completion plan | Low | Small |
| FM-002 | Network Authority Prototype | Deferred | Later validate server-authoritative economy. | Host controls money, shop stock, NPC state. | Stable single-player economy | High | Large |
| FM-003 | Cloud Persistence Prototype | Deferred | Later explore cloud save/host handoff. | Cloud snapshot proof-of-concept. | Save/load coverage | High | Large |

## IL-001 + CDN-002 — Real Gathering & Night Shop Gate (2026-06-24)

- Status: First implementation pass done and validated.
- IL-001: 3 spread daytime wild-forage points (reusing `DaytimeStockPrepPoint`, existing Raw items, daily reset, save/load v8). Garden Prep Basket / Producer Drop Box kept as backup/NPC support.
- CDN-002: night shop gate (`IsShopOpenForCustomers`) — Day 2+ customers buy only after the player opens the shop via `ShopOpenSign` during the `ShopOpen` phase; Day 1 tutorial stays always-open.
- Preserved `PurchaseEvaluator`, `Shop`, `ShopSlot`, `EconomyService`, NPC FSM, and the Save schema shape (additive v8 only).
- Validated by `PA_GatheringShopGateValidator` + 6 regressions; 5 screenshots captured.
- Docs: `Docs/IslandLife/GATHERING_AND_SHOP_GATE.md`.
- Next: visual polish of forage/sign, fixed-terrain forage placement, settlement close interaction, gathering→processing chain.
