---
name: pa-visual-director
description: Direct Project P.A. screen-by-screen visual development from functional prototype to polished vertical slice while preserving existing gameplay authorities.
---

Follow `AGENTS.md`, the active `CODEX_HANDOFF.md`, and the current Opening Demo Canon.

## Purpose

Upgrade the player-visible experience screen by screen without replacing gameplay authorities or redesigning the game loop.

## Screen passes

Work on one approved pass at a time:

- S01 Arrival
- S02 Basic HUD
- S03 Inventory / Hotbar / Held Item
- S04 Item Acquisition
- S05 Gathering
- S06 Placement
- S07 Settlement Formation
- S08 Smartphone
- S09 Sunset Transition
- S10 Shop Interior
- S11 Display / Pricing
- S12 Customer Purchase
- S13 First Sale Presentation
- S14 Pioneer Report

Preserve the Opening Demo route and MUST PATH. Do not advance to another pass without approval.

## Reference philosophy

- Dinkum: world readability, camera, gathering, placement, and settlement grammar.
- Animal Crossing: item/UI readability and cozy interaction grammar.
- Moonlighter: shop, pricing, and customer-feedback grammar.

Use product grammar only. Never copy distinctive copyrighted assets or designs.

## Priorities

1. Player-visible structural quality.
2. Composition, hierarchy, and density.
3. Camera, motion, and interaction feedback.
4. Asset consistency.
5. Cosmetic polish.

Reuse the established Player, Camera, Inventory, Hotbar, Economy, Shop, World, Placement, and Save authorities. Never create a parallel authority.

Fix structural visual problems within the approved pass when they are proven: debug/prototype visuals, broken meshes, wrong prefabs, major scale or orientation errors, huge empty critical spaces, incoherent settlement composition, persistent placement ghosts, and severe camera obstruction. Defer cosmetic-only work when it is not required by the pass.

## Tool routing

1. Current docs and existing evidence.
2. Existing local assets and prefabs.
3. Serena for precise code navigation.
4. Compile and relevant tests.
5. Context7 only for external API facts.
6. Unity MCP only for narrow runtime or serialized facts.

Never use Unity MCP for broad exploration.

## Visual evidence and human QA

Use the real production route and real gameplay state. Do not fake states for screenshots. Prefer the existing `Tools/Project PA/Visual QA/` GameView capture commands and save evidence under `Logs/VisualQA/`.

Human visual verification may be unavailable; do not stop solely for that reason. Report each conclusion as one of:

- `OBJECTIVELY VERIFIED`: supported by compile, checks, runtime facts, or captured evidence.
- `STRUCTURALLY IMPROVED`: the implementation changed the intended visible structure, but subjective quality is not proven.
- `HUMAN-UNVERIFIED`: framing, feel, readability, or polish still needs human review.

Never claim subjective visual PASS without visual evidence.

After a meaningful batch, run compile, the relevant regression or integration check, and a Console error check. Preserve all staged, unstaged, dirty, and untracked state. Do not commit, push, reset, restore, clean, stash, delete scenes, or add external packages unless explicitly authorized.
