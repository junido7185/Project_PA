# Project PA Claude Guide

Claude Code must follow the same project rules as Codex.

Writable project root:

`C:\Users\sdjsd\Desktop\Unity\Project_PA`

Read-only reference prototype:

`C:\Users\sdjsd\Desktop\Unity\Project_D\Project_D`

Do not modify, copy, merge, or import anything from the reference project.

## First Documents

Start with `Docs/AgentWorkflow/CONTEXT_INDEX.md`, then read only the documents required for the current task type.

For gameplay, UI, NPC, economy, art, or scene work, include:

- `PROJECT_PA_CREATIVE_NORTH_STAR.md`
- `PROJECT_PA_DESIGN_INTENT.md`
- `PROJECT_PA_CURRENT_MILESTONE.md`
- `PROJECT_PA_STATUS.md`
- `PROJECT_PA_TODO.md`
- `PROJECT_PA_SESSION_REPORT.md`

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

After meaningful work, update:

- `PROJECT_PA_STATUS.md`
- `PROJECT_PA_TODO.md`
- `PROJECT_PA_SESSION_REPORT.md`
- `Docs/07_개발일지.md`

For loop/dry-run work, also update:

- `Automation/LoopEngineering/progress.md`
- `Automation/LoopEngineering/State/loop-state.json`

Do not only write a commit message. Keep the project records current.
