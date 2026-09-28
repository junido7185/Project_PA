# Project PA Claude Guide

Claude Code must follow the same project rules as Codex.

Writable project root:

`C:\Users\sdjsd\Desktop\Unity\Project_PA`

Read-only reference prototype:

`C:\Users\sdjsd\Desktop\Unity\Project_D\Project_D`

Do not modify, copy, merge, or import anything from the reference project.

## First Documents

Read the four documents under `Docs/00_CURRENT/` first: CURRENT_STATE, CAPABILITY_REGISTRY, GAME_LOOP_MAP, INTEGRATION_QUEUE. Follow `AI_WORKFLOW/00_START_HERE/ONE_PAGE_WORKFLOW.md`; select only task-relevant references through `Docs/AgentWorkflow/CONTEXT_INDEX.md`.

## Project Identity

Project PA is a cozy 3D life and shop management game.

The player prepares goods through village life by day, opens and operates a personal shop at night, and grows a village economy through NPC support, product categories, pricing, customer response, audit/tier goals, and long-term progression.

Preserve this identity. Do not turn the project into a simple shop-sale prototype, a passive spreadsheet manager, or a clone of a commercial game/reference project.

## Safety Rules

- Work only in Project_PA.
- Do not modify Project_D.
- Do not import external packages.
- Do not delete local files.
- Do not run destructive Git or filesystem commands.
- Do not push to GitHub.
- Do not auto-commit without explicit user approval.
- Do not create scripts that repeatedly call AI tools or run forever.
- Do not rewrite core shop/economy/NPC/save systems unless the user explicitly approves a scoped refactor.

## Bounded Ticket Continuation

The default is to wait for a new human instruction after one bounded ticket. The limited exception is `PREAPPROVED_MILESTONE_CONTINUATION`:

- The human must explicitly approve a milestone and exact ticket sequence.
- The approval must be recorded in `Automation/LoopEngineering/State/loop-state.json`.
- Only one ticket may be active, and every ticket keeps its own validation and diff review.
- A missing intermediate human message is not a blocker while the recorded next ticket remains inside the approved sequence.
- A local ticket commit is allowed only when the approval record explicitly permits it; push remains forbidden.
- Stop at `preapprovedThrough`, `stopAtMilestone`, a HARD BLOCKER, or a context-limit pause. Never continue into the next unapproved ticket.
- Report an identical blocker once, then remain terminal until human input changes the state.

## Unity Rules

Before running validators:

- Check whether Unity Editor is already open for Project_PA.
- If it is open, do not run batchmode for the same project.
- Run validators from the open Editor menu or document that validation is blocked by the open Editor.

If Unity crashed recently, read the latest `PROJECT_PA_CRASH_REPORT_*.md` before launching or validating.

## Validation Rules

Use `Automation/LoopEngineering/validator-registry.json` for known validator methods.

Stop and document the issue when:

- compile errors repeat
- a validator fails twice for the same reason
- Unity crash artifacts appear
- a change would require forbidden paths
- a change requires human approval

## Documentation Rules

작업 결과는 `Docs/04_DEVELOPMENT_LOG/YYYY-MM.md`에 날짜·티켓·변경·검증·미해결 사항을 한 번 기록한다. 현재 상태가 바뀌면 `Docs/00_CURRENT/CURRENT_STATE.md`, 기능 계약은 `CAPABILITY_REGISTRY.md`, 실제 연결은 `GAME_LOOP_MAP.md`, 다음 우선순위는 `INTEGRATION_QUEUE.md`의 해당 부분만 갱신한다. 완료 티켓과 긴 이력을 현재 문서에 누적하지 않는다. CHANGELOG·handoff·구 개발일지에 결과를 복제하지 않는다.

For authorized automation work, maintain its existing machine state/progress contract without copying it into multiple current reports.

## WORLD Scene Rules

- `Assets/Scenes/Prototype_FirstDay.unity` is the Golden Regression Scene. Do not use it for WorldGrid, procedural island, chunk terrain, or terraforming experiments.
- WORLD tickets target an explicitly approved `Assets/Scenes/WorldSandbox.unity` unless their scope says otherwise. WORLD-000 does not create that scene.
- `Assets/Scenes/MainGame.unity` remains read-only until the World Data, Terrain, Building, Navigation, and Existing Gameplay integration gates pass and a separate human-approved integration ticket exists.
- Prefer an editor builder/setup utility, object/reference validator, Unity save, diff/Missing Reference review, then human Game View. Do not broadly edit scene serialization or replace serialized references by name.
- Reuse existing Project P.A. services through adapters/sidecars; do not create duplicate Shop, Economy, Inventory, NPC, Clock, Save, Grid, or registry authorities in WorldSandbox.
