---
id: 7
title: "State-jump QA fixture bypassed tick events and captured a stale HUD"
status: open
type: open-source
skill: [pa-demo-factory, project-pa-vertical-slice]
proposes_skill: []
target_file: []
siblings_checked: "no family registry; pa-demo-factory added (labels QA fixtures, owns evidence honesty); project-pa-vertical-slice added (Phase 3 Play forbids faking state); pa-visual-director not read this session (assumed)"
area: "QA fixtures that set time/progression directly before GameView capture"
date: 2026-10-03
session_context: "Opening Demo D2-D4 play review: fixture called GameClock.ForceSet(20) during the sunset; the HUD clock stayed at 16:03 in captures labeled 20:00"
parked_until:
resolved:
resolution:
reference:
---

**Issue:** The review fixture jumped the game clock with a method documented as save/load-only.
That method fires no minute/hour tick, and the clock HUD updates only on ticks, so the following
captures showed 16:03 while the game was at 20:00 and night lighting. A numeric check on the clock
value passed; only reading the frame revealed the mismatch. Switching the fixture to accelerate
the product's own clock (natural ticks, 1 s per game hour) kept HUD, lighting and shop phase
consistent at the cost of four real seconds.

**Suggested improvement:** In pa-demo-factory "Implement, play, repair", add: when a fixture must
advance time or progression, accelerate the product's own progression rather than calling a
state-setter, and add one check that the visible HUD equals the authoritative value in captures
used as evidence.

**Principle:** A QA shortcut that writes state directly can bypass the notifications the UI
depends on; prefer speeding up the real progression, and verify that what the frame shows equals
the authoritative value before using the frame as evidence.
