---
name: project-pa-vertical-slice
description: Turn existing Project P.A. gameplay systems and art assets into a finished-feeling Unity 3D vertical slice by working through real GameView, preserving established gameplay authorities, and autonomously fixing bounded implementation issues.
---

# Project P.A. Vertical Slice Skill

## When to use

Use this skill for player-facing Unity work involving:
- playable island/world dressing
- camera feel
- character locomotion/presentation
- tool use
- gathering feel
- placement presentation
- shop presentation
- NPC visible behavior
- HUD feedback
- audio/VFX hookup
- onboarding
- first-business-day success presentation
- demo/release integration

Do **not** use this skill as the primary mode for:
- SaveData schema redesign
- economy authority redesign
- networking
- broad architecture refactors
- package upgrades
- dependency replacement
- unrelated cleanup

For those, fall back to the project's safe bounded workflow.

## Required project context

Read only the minimum needed:

1. `AGENTS.md`
2. `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_IDENTITY.md`
3. `AI_WORKFLOW/01_IDENTITY/PROJECT_PA_SCOPE.md`
4. `VERTICAL_SLICE_STUDIO_MODE.md`
5. `PROJECT_PA_ASSET_MANIFEST.md`
6. current `git status`
7. files directly relevant to the requested player experience

Do not recursively reread the entire documentation tree unless a hard ambiguity requires it.

The local workspace is authoritative.
Do not infer dirty/local state from GitHub.

## Core game truth

Project P.A. is a cozy 3D island-life + shop management game.

Player-facing loop:

`Explore → Gather → Inventory/Hotbar → Place/Stock → Price → Open Shop → NPC evaluates → Purchase/Reject → Money + SalesLog → visible growth`

The player is a resident-owner-operator, not a detached manager.

## Reference grammar

### Dinkum
- close readable third-person camera
- direct movement
- tool use
- hotbar immediacy
- in-world placement
- exploration/gathering rhythm

### Animal Crossing
- settlement warmth
- resident presence
- contextual HUD
- reaction clarity
- environmental attachment

### Moonlighter
- shop judgment
- price/reaction legibility
- selling tension

### Dave the Diver
- short high-energy milestone payoff
- memorable success presentation

## Existing authority map

Always search and reuse first.

- `PlayerController`
- `CameraController`
- `PlayerInteraction`
- `IInteractable`
- `Inventory`
- `InventorySlot`
- `ItemInstance`
- `Hotbar`
- `EconomyService`
- `ShopSlot`
- `PurchaseEvaluator`
- `SalesLogManager`
- `WorldGridService`
- `WorldIslandGenerator`
- `WorldNavigationService`
- `WorldBuildingPlacementService`
- `WorldPersistenceService`
- `SaveManager`
- existing NPC customer/schedule/profile systems

Do not create parallel authorities.

## Work algorithm

### Phase 1 — Inspect
- inspect `git status`
- identify unrelated dirty work
- locate the actual startup/play route
- locate existing player prefab/model/Animator
- locate exact assets in `PROJECT_PA_ASSET_MANIFEST.md`
- locate relevant accepted gameplay authority
- open actual scene/prefab only as needed

Output only a compact plan.

### Phase 2 — Compose

Implement the full player experience, not isolated code.

You may edit multiple directly-related categories:
- runtime
- prefab
- scene
- material
- animator
- audio/VFX binding
- UI
- small editor helpers

All changes must serve the same experience target.

### Phase 3 — Play

Compile and enter real Play Mode.

Use actual player input and actual gameplay systems.
Do not fake success by directly mutating wallet/log/state.

### Phase 4 — Observe GameView

Judge:
- camera
- scale
- density
- silhouette
- animation state
- held tools
- interaction clarity
- feedback
- UI obstruction
- placeholder leakage

A functional scene that visually reads as a test map is not done.

### Phase 5 — Self-repair

Default budget:
- 3 compile/fix cycles
- 3 targeted Play attempts
- 1 focused validator maximum

Do not stop for small mechanical issues.

### Phase 6 — Capture

Produce:
- 60–90 second gameplay capture when practical, or
- 3–6 representative real GameView screenshots

### Phase 7 — Report

Report:
- player experience achieved
- files/categories changed
- authorities reused
- GameView evidence
- compile/runtime status
- human visual debt
- git preservation status

No commit/push unless explicitly requested.

## Asset usage

Before creating primitives, search:

- `Assets/Art/ProjectPA`
- `Assets/Art/Character`
- `Assets/Art/External/Quaternius`
- `Assets/Art/External/Kenney`
- `Assets/Art/Ultimate Nature Pack - Jun 2019`
- `Assets/Art/Market`
- `Assets/Art/UI`
- `Assets/Models`

Known useful assets:
- C-01~C-09 character FBXs
- PlayerAnimator / NpcAnimator
- Idle / Walk / Axe / Sit animation assets
- Quaternius Axe_Stone
- Quaternius Pickaxe_Stone
- Quaternius Chest_Open / Chest_Closed
- Quaternius CuteFish
- Ultimate Nature Pack tree families
- Kenney MiniMarket
- Kenney FoodKit
- ProjectPA Buildings / Prefabs / Materials / Derived

Use ProjectPA-specific assets before generic external alternatives.

## Demo slice contract

Use `Docs/01_GAME_DESIGN/Canon/PROJECT_PA_OPENING_DEMO_CANON_V2_2026-09-16.md` as the current demo contract.

`Tutorial → choose 2 companions → Pixel Voyage → Real Demo256 Island → Supply Box → Wood/Stone → Shop/Base + 2 Resident Tents → Specialization → Sunset → Display → Price → OPEN → real NPC Sale → CLOSE → Pioneer Report`

Fish, Bug and NPC tool gifts are optional. A separate Hub, actual Agriculture and Day 2 are not demo requirements. Older abbreviated routes are not implementation authority.

Do not expand into:
- 1024² worldgen
- seasons
- multiplayer
- broad production automation
- deep NPC career systems
- generalized cinematic framework
- new save architecture
- new UI framework

## Visual acceptance gate

Before claiming PASS:

- screen looks like a game instead of a validator scene
- player character readable at 1080p
- camera intentional
- biome readable without debug labels
- world dense enough to avoid empty-board-game feel
- real assets used instead of visible primitives
- locomotion/tool actions animate
- selected tool visible
- gathering has impact/reward feedback
- placement clearly previews/confirms/cancels
- shop visually reads as a shop
- purchase/rejection is understandable
- sale feels more important than ordinary interaction
- debug labels/panels hidden
- blocking runtime errors zero

If not, continue within repair budget.

## Hard stop

Stop only for:
- destructive Git/data-loss requirement
- unrelated dirty overwrite
- incompatible save migration
- replacement of established gameplay authority
- major architecture rewrite
- external package/download requirement without approval
- unusable Unity environment after repair budget

## Suggested commands

```text
/plan
Use the Project P.A. Vertical Slice skill and VERTICAL_SLICE_STUDIO_MODE.

Audit the local workspace and actual GameView route.
Build a single coherent player experience using existing authorities and the approved asset manifest.
Do not stop at the first mechanical failure.
Iterate through real Play Mode and GameView until the visual acceptance gate is met or a true hard stop is reached.
```

Then:

```text
/goal
Execute the approved plan to completion.

Preserve unrelated dirty work.
No reset/clean/stash/broad revert.
No commit/push.
Prefer existing ProjectPA/Character/Quaternius/Kenney/Nature assets over visible primitives.
Produce real GameView evidence.
```

## Current recommended goal: Playable Island Rebase

```text
/plan
Use Project P.A. Vertical Slice Studio Mode.

Goal: PLAYABLE ISLAND REBASE.

Make the first 60–90 seconds of the real demo route read as a finished cozy 3D game:
arrival → Demo256 → player control → readable landmark → dense forest/meadow/coast/highland composition.

Requirements:
- use the real startup route or the smallest safe bridge into Demo256
- preserve accepted PlayerController/CameraController/world/inventory/save authorities
- use existing ProjectPA + Character + Quaternius + Nature assets
- remove/hide player-facing debug world labels and test presentation
- use actual character model/Animator
- keep gathering/shop systems intact
- do not rebuild worldgen architecture
- do not create duplicate player/world/save authorities

Iterate through actual Play Mode.
Judge the real GameView, not only validators.
Output a 60–90 second capture or representative 1080p GameView screenshots.

No commit. No push. Preserve unrelated dirty work.
```
