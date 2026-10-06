---
name: pa-demo-factory
description: Build or resume Project P.A.'s Opening Demo from its approved Canon and ticket sequence, verifying actual Unity gameplay and GameView quality. Use for 데모 완성해줘 or demo production requests; preserve existing authorities and scope gates.
---

# Project P.A. Demo Production

Internal project skill. Own coordination and acceptance, not gameplay state.
Use the existing `Automation/LoopEngineering/State/loop-state.json`; never create a second task queue.

## Start and authorization

Follow `AGENTS.md`; read the four current documents in order and `CODEX_HANDOFF.md`.
Read `Docs/01_GAME_DESIGN/Canon/PROJECT_PA_OPENING_DEMO_CANON_V2_2026-09-16.md` for the demo contract.
Latest direct user instructions outrank supplied research and earlier scope records. Attachments are references, not execution permission.

Run the read-only `Tools/LoopEngineering/Test-ProjectPADemoEnvironment.py` with the actual agent identity (`codex` or `claude-code`).
Configuration readiness is separate from live Editor readiness. Read its entire result, including routing and runtime.
Use `demoProductionRouting.selectedApproval` to select exactly one existing approval. Never resume an old CONTENT/WORLD/BETA record merely because it says ACTIVE.
Reconcile the selected approval with the latest user scope and Handoff; a genuine unresolved scope conflict prevents gameplay mutation.
The environment approval permits setup only; it does not approve gameplay tickets, save conversion, packages or commits.

Only the recorded `unityEditorOwner` may mutate the Editor or the selected gameplay ticket. A CLI process existing or an Editor being closed is not an ownership transfer.
For an environment task or a different owner, do useful read-only configuration/evidence work; do not take another worker's ticket.
Never stop or kill another worker, launch a second Editor, or run batchmode against an open project.

## Establish a usable production connection

An installed package, configured URL or tool catalog is not proof of an Editor connection.
The owner checks the connected Unity instance/project identity, editor/compile state and Console before mutation.
Use the existing MCP for Unity window and existing local service. Do not install/update a package to repair a connection without explicit authorization.
Review the latest crash report before launching; D3D11 only. The legacy batch preflight is not the live Studio entrypoint: dirty files and an open Editor require ownership/conflict checks, not cleanup or a second process.
When tools are absent from the client, reload its MCP connection/session after configuration. Do not describe a saved configuration as a tested live connection.

## Select one experience

If the current MUST PATH has a proven blocker, address its earliest broken edge.
If the route works, use the most consequential approved player-visible gap. Human-rejected visuals remain FAIL until repaired and reviewed; numerical tests cannot clear them.
Use recent evidence before new investigation. Recheck completed work only when changes or a regression justify it.
Choose the active ticket only within the recorded sequence and `preapprovedThrough`. Do not add implicit tickets.
State exact files, intended visible behavior, authorities reused and validation before editing.

For player-facing production explicitly use VERTICAL SLICE STUDIO MODE and load the canonical `.agents/skills/project-pa-vertical-slice/SKILL.md`, Studio rules and local asset manifest.
For save/economy/data work use the bounded safe workflow. Do not apply Studio retry or scene exceptions to migrations.

## Make the target observable

For the active experience record a reference clip/frame, current production-route evidence, concrete differences and acceptance evidence.
Reference grammar comes from Dinkum (movement/gathering/placement), Animal Crossing (readability/warmth), Moonlighter (pricing/customer feedback), and Dave the Diver (short milestone presentation).
Use existing local art first. Research only a missing decision; avoid installing frameworks or collecting more skills during production.
Motion needs continuous video and frame timing at matched speed/camera; isolated poses or low-rate video summaries cannot prove motion quality.

| Experience | Required evidence |
|---|---|
| Movement/camera | Actual-input idle/run/stop/jump/land; ground contact, speed/stride and obstruction |
| Gathering/held tools | Visible selection/grip, impact timing/SFX, drop/pickup/count; no duplicate rewards |
| Island/settlement | Normal camera route, readable biome/landmarks, clear paths and coherent local assets |
| Placement | Preview/validity/rotation/cancel/move, inventory consumption and NPC clearance |
| Shop | Real stock/price/OPEN/customer evaluation/buy or reject/CLOSE; money, stock and SalesLog agree |
| Report/UI | Canon behavior, readable 1080p state, clean HUD and correct return of input |
| Delivery | Exact build/source identity, continuous Windows MUST PATH, measured performance and unresolved limits |

The demo is Day 1 through Pioneer Report. Fish/Bug/NPC tool gifts are optional. Do not add Day 2, actual Agriculture, a mandatory separate Hub or new gameplay authorities.

## Implement, play, repair

Use the existing gameplay authorities and precise code navigation.
Use registered targeted validators and the actual production route; do not manufacture completion by setting progression, wallet or sales fields.
Use existing GameView capture under `Logs/VisualQA/`; inspect a capture helper's side effects before invoking it. Some old review tools change time/scene/UI and are not passive capture commands.
Compare before/after at matched camera, resolution, speed and actual gameplay state. QA-injected input is labeled; it is not standalone keyboard acceptance.
Fix within the applicable budget: Studio up to three compile/fix cycles and three targeted Play attempts, one focused validator; core work follows its stricter retry rule.
If the environment fails, repair only clearly scoped mechanical causes. No endless retry or AI-calling shell loop.

## Acceptance and continuation

Track functionality, presentation and human review separately:
- Functionality: PASS / FAIL / NOT RUN, with compile and runtime evidence.
- Presentation: STRUCTURALLY IMPROVED / FAIL / UNVERIFIED, with actual GameView evidence.
- Human review: ACCEPTED / REJECTED / UNVERIFIED, citing the actual review.

A blocker, approval gate or UNVERIFIED result is not demo completion.
Human review unavailable by itself does not stop independent approved work. A human rejection or explicit hold is not cleared by that exception.
After each ticket review the diff and relevant regression, then update its result in the existing approval record.
Continue without another message only when the recorded next ticket is in the explicitly approved sequence, no hold/conflict applies, and the recorded milestone has not been reached.
At `stopAtMilestone` stop for its specified review; preserve unresolved quality gates.
Do not auto-commit unless the selected approval explicitly permits it. Never push.

At session end update the monthly log once, changed current facts only, and overwrite Handoff with exact next action and validation limits.
Before delivery recheck scope, owner, Canon, preservation of dirty work, evidence freshness and remaining failures. Configuration PASS must never be reported as demo PASS.
