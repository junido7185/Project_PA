# BASELINE-001 - Bounded Ticket Loop Baseline Commit Review

Date: 2026-06-26  
Purpose: help the user create a local Git checkpoint commit for the current stabilized Project_PA work.  
This document does not stage, commit, push, reset, clean, delete, or modify gameplay files.

## Current Git Snapshot

- Project root: `C:\Users\sdjsd\Desktop\Unity\Project_PA`
- Git root: `C:/Users/sdjsd/Desktop/Unity/Project_PA`
- Current branch: `master`
- Last commit: `adc5d2f 프로토타입 구성 -1`
- Changed file/status entries using `git status --porcelain=v1 -uall`: 144
  - Modified tracked paths: 27
  - Untracked paths/files: 117
- `git diff --stat`: 27 tracked files, 3173 insertions, 129 deletions
- Unity Editor process for Project_PA during review: none detected
- `.gitignore`: exists

## D3D11 Stability Baseline

Crash report kept:

- `PROJECT_PA_CRASH_REPORT_20260625.md`

Crash resolution record created:

- `Automation/LoopEngineering/State/crash-resolution.json`

Recorded facts:

- User confirmed they manually launched Unity on the `-force-d3d11` baseline.
- User confirmed project opening and Play Mode stability manually.
- D3D12 is not approved and not verified.
- The crash report is preserved as the D3D12 cause/workaround record.
- Future launch-heavy automation is allowed only on the approved D3D11 path and only after the Git baseline is clean.

## A. Recommended For Local Baseline Commit

These files are current Project_PA source, documentation, validation, policy, or project-owned content that should usually be captured in the local checkpoint.

| Path / Group | Why include |
|---|---|
| `AGENTS.md`, `CLAUDE.md` | Shared agent safety rules for Codex/Claude. |
| `README.md` | Current run, validation, crash, package, and development notes. |
| `PROJECT_PA_*.md` | Design intent, creative north star, status, TODO, session report, milestone, backlog, core slice, crash reports, and planning docs. |
| `Docs/07_개발일지.md` | Development diary updated with missing June records. |
| `Docs/AgentWorkflow/` | Context routing for future bounded work. |
| `Docs/CustomerPresentation/`, `Docs/IslandLife/`, `Docs/VillageCulture/` | Feature documentation and blocked VC-001A note. |
| `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md` | This baseline review. |
| `Automation/LoopEngineering/loop-policy.json` | Bounded-ticket policy including D3D11-only crash-resolution gate. |
| `Automation/LoopEngineering/State/loop-state.json` | Current loop ticket state. |
| `Automation/LoopEngineering/State/crash-resolution.json` | Human-confirmed D3D11 stability approval record. |
| `Automation/LoopEngineering/progress.md` | Append-only loop progress record. |
| `Automation/LoopEngineering/ticket-template.md` | Template for future bounded tickets. |
| `Automation/LoopEngineering/Tickets/` | Existing dry-run ticket record. |
| `Automation/LoopEngineering/validator-registry.json` | Registry of existing Project_PA validators. |
| `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1` | Preflight tool; now distinguishes reviewed D3D11 crash baseline from unresolved crash artifact. |
| `Assets/Scripts/*.cs`, `Assets/Scripts/UI/*.cs` changed/created by recent work | Project_PA gameplay support, sidecar controllers, UI presentation, save additions, and runtime binders already used by current development baseline. |
| `Assets/Editor/PA_*Validator.cs` and matching `.meta` files | Editor-only validation/review tooling. |
| `Assets/Resources/NPCs/Profile_*.asset` | Intended per-resident customer/economy data. |
| `Assets/Fonts/Jalnan2_SDF.asset` | Korean UI readability/font asset update. |
| `Assets/Scenes/Prototype_FirstDay.unity` | Current main Day 1-3/Core Slice scene baseline. |
| `Assets/Materials/Market/`, `Assets/Prefabs/Market/`, `Assets/Art/Market.meta` | Project_PA-owned market visual assets created in the project. |
| `Assets/Settings/PC_RPAsset.asset`, `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` | Current render/visual baseline assets. |
| `ProjectSettings/ProjectSettings.asset` | Contains the D3D11 graphics backend workaround already documented in the crash report. Review before commit, but it is part of the current stability baseline. |

## B. Include Only After User Review

These may be useful, but they are backups, deliverables, generated/reference artifacts, or settings that should be consciously selected.

| Path / Group | Why review first |
|---|---|
| `Assets/Scenes/_Backups/` | Scene backups are useful locally, but may not belong in Git history unless the user wants recoverable scene milestones. |
| `Docs/VisualTargets/` | Visual target images and notes; include only if accepted as project reference material. |
| `SubmissionPackages/` | Large zip deliverables, currently about 465 MB total; usually keep outside source commits unless submission history is required. |
| `ProjectSettings/EditorBuildSettings.asset` | Build scene list; likely intentional, but confirm before staging. |
| `ProjectSettings/UnityConnectSettings.asset` | Unity service/cloud metadata; include only if intentionally changed. |
| `Project_PA.slnx` | IDE/solution metadata; currently tracked, but review whether it is useful in source history. |
| `Automation/LoopEngineering/RunLogs/.gitkeep` | May be useful to preserve the folder, but the run output itself should usually stay out. |
| New large source art/data files | Include only after confirming they are Project_PA-owned source assets, not generated caches. |

## C. Recommended To Exclude From Baseline Commit

These are cache, build, log, crash dump, generated, or local-machine outputs.

| Path / Pattern | Why exclude |
|---|---|
| `Library/` | Unity import/cache folder; never commit. |
| `Temp/` | Unity temporary output; never commit. |
| `Logs/` | Batch/editor/player logs; useful locally, noisy in source history. |
| `Builds/` | Build output; keep separate from source checkpoint. |
| `UserSettings/` | Local editor/user preferences. |
| `obj/`, `[Oo]bj/` | Build intermediates. |
| `.vs/` | Visual Studio cache. |
| `*.log` | Debug/runtime logs. |
| `sysinfo.txt`, `mono_crash.*` | Unity/Mono crash byproducts. |
| `*.dmp`, `*.mdmp`, `*.crash`, `*.stacktrace`, `Crash_*/` | Crash dump/temp output; keep only summarized markdown reports. |
| `Automation/LoopEngineering/RunLogs/*.json` | Execution evidence is useful locally, but future run logs will create churn. Keep out unless the user explicitly wants evidence logs committed. |
| `GeneratedAssets_deleted/`, generated cache folders | Generated/scratch output; exclude unless proven to be source material. |
| Unity Bug Reporter temp folders | Native crash temp output; do not commit. |

## `.gitignore` Review

Current `.gitignore` already covers:

- `Library/`
- `Temp/`
- `Logs/`
- `Builds/`
- `UserSettings/`
- `obj/`
- `.vs/`
- `*.log`
- `sysinfo.txt`
- `mono_crash.*`

Suggested additions only; this ticket did not edit `.gitignore`:

```gitignore
# Project PA local deliverables
/SubmissionPackages/

# Generated or scratch asset output
/GeneratedAssets_deleted/

# Crash dump byproducts if copied into the project
*.dmp
*.mdmp
*.crash
*.stacktrace
Crash_*/

# Optional: local loop run outputs
/Automation/LoopEngineering/RunLogs/*.json
!/Automation/LoopEngineering/RunLogs/.gitkeep
```

## Recommended GitHub Desktop Staging Flow

Do this manually in GitHub Desktop:

1. Open repository `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
2. Confirm the branch is `master`.
3. In the Changes list, stage the A group first:
   - root docs: `AGENTS.md`, `CLAUDE.md`, `README.md`, `PROJECT_PA_*.md`
   - `Docs/AgentWorkflow/`, `Docs/CustomerPresentation/`, `Docs/IslandLife/`, `Docs/VillageCulture/`
   - `Docs/07_개발일지.md`
   - `Automation/LoopEngineering/` except `RunLogs/preflight-20260626.json` unless you intentionally want the evidence log
   - `Tools/LoopEngineering/`
   - intended `Assets/Scripts/`, `Assets/Scripts/UI/`, `Assets/Editor/`, `Assets/Resources/NPCs/`, `Assets/Fonts/Jalnan2_SDF.asset`
   - `Assets/Scenes/Prototype_FirstDay.unity`
   - `Assets/Materials/Market/`, `Assets/Prefabs/Market/`, `Assets/Art/Market.meta`
   - intended render assets in `Assets/Settings/`
   - `ProjectSettings/ProjectSettings.asset` if you accept the D3D11 stability baseline
4. Review the B group and either stage or leave unstaged:
   - `Assets/Scenes/_Backups/`
   - `Docs/VisualTargets/`
   - `SubmissionPackages/`
   - `ProjectSettings/EditorBuildSettings.asset`
   - `ProjectSettings/UnityConnectSettings.asset`
   - `Project_PA.slnx`
5. Leave C group unstaged.
6. Before committing, check GitHub Desktop's file list carefully for `Library`, `Temp`, `Logs`, `Builds`, `.vs`, `obj`, crash dump, or generated cache folders.

Optional command-line verification after staging:

```powershell
git status --short
git diff --cached --stat
git diff --cached --name-status
```

## Recommended Local Commit Messages

Option 1:

```text
Establish Project PA long-play core slice baseline
```

Option 2:

```text
Baseline cozy day-night shop loop and D3D11 guardrails
```

## When Preflight Can Become READY

Current preflight result after BASELINE-001 harness correction:

- `BLOCKED_BY_DIRTY_GIT`
- `CrashResolutionValid: True`
- `CrashResolutionStatus: human_verified_d3d11_stable`
- `ApprovedGraphicsBackend: D3D11`

Preflight can become `READY_FOR_BOUNDED_TICKET_LOOP` only when:

1. The local baseline commit is created or the working tree is otherwise made clean.
2. `git status --short` has no unstaged/staged source changes.
3. Unity Editor is not open for Project_PA when batch automation is expected.
4. `Automation/LoopEngineering/State/crash-resolution.json` remains present and valid.
5. `PROJECT_PA_CRASH_REPORT_20260625.md` remains preserved as the historical crash report.
6. Automation stays on the approved D3D11 path; D3D12 remains not approved/not verified.

BASELINE-001 status before the user's local checkpoint commit:

- `needs_human_review`
