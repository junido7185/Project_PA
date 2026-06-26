# Loop Engineering Ticket Template

## Ticket ID

`LOOP-000`

## Goal

Describe one small goal.

## Player Experience

What should the player understand or feel after this work?

## Design References

- `PROJECT_PA_CREATIVE_NORTH_STAR.md`
- `PROJECT_PA_DESIGN_INTENT.md`
- task-specific docs from `Docs/AgentWorkflow/CONTEXT_INDEX.md`

## Allowed Paths

- List exact writable paths.

## Forbidden Paths

- `C:\Users\sdjsd\Desktop\Unity\Project_D\Project_D`
- Any gameplay/core path not needed for this ticket.

## Maximum Changed Files

Default: 15.

## Maximum Attempts

Default: 2.

## Investigation Checklist

- [ ] Confirm project root.
- [ ] Confirm Git root/status.
- [ ] Check Unity Editor process.
- [ ] Read required context docs.
- [ ] Inspect existing implementation before editing.

## Implementation Scope

Smallest acceptable change.

## Completion Criteria

- [ ] Goal is met.
- [ ] No forbidden paths changed.
- [ ] Documentation updated.

## Automated Validators

- Add validator IDs from `validator-registry.json`.

## Screenshot Evidence

- Add screenshot paths when visual quality is part of the task.

## Human Gates

- Scene approval
- Save schema approval
- Balance approval
- New gameplay system approval
- Visual quality approval

## Stop Conditions

- Wrong root.
- Dirty baseline not approved.
- Unity Editor is open while batch validators are requested.
- Compile failure repeats.
- Validator failure repeats.
- Crash artifact appears.
- Forbidden path would be required.

## Recovery Plan

How to back out or pause safely without deleting files.

## Next Ticket Candidates

- Candidate 1
- Candidate 2
- Candidate 3
