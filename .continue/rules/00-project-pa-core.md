---
name: Project P.A. Core Development Rules
alwaysApply: true
---

# Project P.A.

Existing Unity 6 URP game project in active late-stage development.

Current priority:
finish the approved Opening Demo / Vertical Slice as a stable playable experience.
Do not redesign the game or implement speculative future features unless explicitly asked.

The CURRENT LOCAL WORKSPACE is authoritative.
GitHub may be behind local work.

# Before Work

For non-trivial tasks:

1. Read `AGENTS.md`.
2. Follow its current-document routing.
3. Inspect only the files relevant to the requested task.
4. Search for existing implementations and usages before creating anything.
5. Identify the first broken edge/root cause before editing.

Do not read the entire repository or all historical documentation unless required.

`CODEX_HANDOFF.md` is a resume aid, not the primary source of truth.
Current code + `Docs/00_CURRENT/*` + approved Canon override stale handoff text.

Human Play evidence overrides previous automated PASS or HUMAN_UNVERIFIED claims.

# Scope

Work on ONE bounded task at a time.

Prefer:
- fixing existing wiring
- completing existing implementations
- repairing lifecycle/event/binding issues
- the smallest correct change

Avoid:
- broad refactors
- architecture rewrites
- speculative fallbacks
- duplicate systems
- arbitrary delays/retries used to hide lifecycle bugs

If the root cause remains unclear after one focused investigation,
stop and report the uncertainty instead of stacking speculative edits.

# Existing Authorities

Do not create parallel authorities for existing systems such as:

- Inventory / Hotbar / Equipment
- Player interaction
- Shop / ShopSlot / pricing / customer purchasing
- Economy / SalesLog
- Placement / WorldGrid
- GameClock / day-night
- NPC production
- Save / persistence

Search usages before modifying shared classes or APIs.

# Unity Safety

Preserve Unity serialization compatibility.

Do not casually:
- rename serialized fields
- rename MonoBehaviour classes/scripts
- change namespaces
- change shared public APIs
- delete serialized fields
- move or rename assets

Do not modify `.meta` files manually.

Do not modify:
- Library/
- Temp/
- obj/
- generated Logs/

Do not change:
- ProjectSettings/
- Packages/
- scenes
- prefabs

unless the current task explicitly requires it.

If a scene/prefab/serialized change is required, explain the reason and risk before doing it.

# Workspace Safety

Preserve ALL existing staged, unstaged, dirty, and untracked work.

Never run without explicit user approval:
- git reset
- git clean
- git stash
- git restore
- destructive checkout
- git revert
- git commit
- git push
- destructive file deletion

Do not overwrite unrelated user/agent work.

# Implementation

Before editing, briefly state the exact files you expect to modify.

Use nearby code style and existing Unity APIs.

Do not invent classes, methods, fields, assets, packages, or APIs.
When uncertain, inspect the workspace.

After changes:

1. inspect the diff
2. check likely compile/integration issues
3. compile when practical
4. fix only errors caused by the task

Compilation is NOT gameplay acceptance.
Do not claim runtime/GameView success unless it was actually observed.

# Escalation

Stop instead of making risky guesses when:

- a SaveData schema change seems necessary
- serialized compatibility may break
- ownership between multiple authorities is unclear
- more than one competing runtime authority appears to exist
- a human-play failure contradicts the current implementation assumption
- the same attempted fix has already failed human verification

Report the proven facts and exact unresolved edge for a stronger model.

# Completion Report

Keep the final report concise:

ROOT CAUSE
FIX
FILES CHANGED
COMPILE STATUS
HUMAN RETEST

Do not write a long project recap.