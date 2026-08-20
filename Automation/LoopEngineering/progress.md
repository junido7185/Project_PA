# Project PA Loop Engineering Progress

Append-only log for guarded automation and dry-run loop work.

## 2026-08-11 - BETA-002 Completed

- Ticket ID: `BETA-002 Daytime Activity Completion`
- Baseline/branch: `9bf71d6c85cb35a8d18c735769e1969c3de9e6e9` / `milestone/gameplay-beta-85`
- Implemented: existing Gathering/Farming/Mining/Fishing relocated from origin fallback positions to generated Forest/Meadow/Highland/Pond activity cells.
- Player path: existing `PlayerInteraction`, `Inventory`, and `Hotbar` now bind to the M70 WorldSandbox player; objective/HUD show activity directions, completion, and resource counts.
- Results: Carrot x2, Wheat x3, Ore x2, Fish x2; all are existing sellable Raw items with 120G combined base value. Same-day duplicates are blocked and activity points reopen next morning.
- Validation: Runtime/Editor compile 0 errors; D3D11 BETA-002, BETA-001, M70 WORLD-010, Prototype Gathering/Fishing and Mining/Shop regressions PASS; blocking Console 0; new crash 0.
- Validator maintenance: stale Mining v10 assertion now follows existing v11 `WorldPersistenceMigration.AdditiveWorldSaveVersion`; schema itself is unchanged.
- Final state: `BETA_002_COMPLETE`; next preapproved ticket: `BETA-003`.

## 2026-08-11 - BETA-001 Completed

- Ticket ID: `BETA-001 Player Onboarding and World Readability`
- Baseline/branch: `b176bdcac8d140ffa53e8b907ef7da3576a44a70` / `milestone/gameplay-beta-85`
- Implemented: fresh Day 1 09:00 start, new-life prompt, WASD→shop→workbench→daytime-resource objective route, functional runtime labels, and compact player HUD.
- Development mode: WORLD grid/placement/generator/navigation surfaces default hidden; F10 restores and hides them together.
- Validation: Runtime/Editor compile 0 errors; D3D11 BETA-001 PASS; M70 WORLD-010 save/restart PASS; Prototype Core Slice and Final Demo Route Golden PASS; blocking Console 0; new crash 0.
- Protected paths: Scene/Prefab/Packages/ProjectSettings/Save schema/MainGame/Prototype_FirstDay content unchanged.
- Capture: `CAPTURE_EVIDENCE_DEBT`; an existing capture request did not create a file and did not block functional completion.
- Final state: `BETA_001_COMPLETE`; next preapproved ticket: `BETA-002`.

## 2026-08-11 - LOOP-POLICY-002 Completed

- Ticket ID: `LOOP-POLICY-002`
- Work type: bounded loop policy and state maintenance; no gameplay implementation
- Baseline: `milestone/world-alpha-70@e0b5678d163e06cd5605e19cd4474aa8cf3654f4`
- Implemented: `PREAPPROVED_MILESTONE_CONTINUATION`, explicit loop-state approval, single-active-ticket/per-ticket validation/local-commit boundary, hard blockers, terminal milestone stop, and one-time identical blocker reporting.
- Approved sequence: `WORLD-005 → WORLD-006 → WORLD-006B → WORLD-007 → WORLD-008 → WORLD-009 → WORLD-010` only.
- Validation: JSON parse PASS; bounded default gate retained; exact sequence and WORLD-010 boundary PASS; D3D11/crash and Git safety policy retained.
- Existing milestone state: M70 was already complete at the baseline, so no WORLD ticket was reexecuted. `sequenceStartTicket=WORLD-005`, `nextTicket=null`, `completionReached=true`.
- Changed gameplay files: none. Scene/Prefab/Packages/ProjectSettings/SaveData schema: none.
- Stop reason: `M70_PLAYABLE_WORLD_ALPHA_COMPLETE` already reached. WORLD-011/MainGame require new human approval.

## 2026-06-26 - LOOP-DRYRUN-001 Created

- Time: 2026-06-26
- Ticket ID: LOOP-DRYRUN-001
- Work type: documentation and dry-run safety infrastructure
- Implemented:
  - Root agent guides: `AGENTS.md`, `CLAUDE.md`
  - Context router: `Docs/AgentWorkflow/CONTEXT_INDEX.md`
  - Loop policy/state/progress/template files under `Automation/LoopEngineering/`
  - Preflight tool under `Tools/LoopEngineering/`
  - Validator registry under `Automation/LoopEngineering/`
- Changed gameplay files: none for this loop-engineering pass
- Validator result: pending preflight execution
- Screenshot evidence: none required
- AI visual review status: not applicable
- Human approval status: required before enabling anything beyond dry-run
- Stop reason: pending preflight
- Next action: run `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1`

## 2026-06-26 - LOOP-DRYRUN-001 Preflight Result

- Time: 2026-06-26T14:02:48
- Ticket ID: LOOP-DRYRUN-001
- Action: ran `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1`
- Execution note: direct script execution was blocked by local PowerShell execution policy, so the one-time check was run with `powershell -NoProfile -ExecutionPolicy Bypass -File`.
- Result: `BLOCKED_BY_DIRTY_GIT`
- Changed gameplay files: none for this loop-engineering pass
- Validator result:
  - Project root: correct
  - Git root: correct
  - Unity project folders: present
  - Git dirty baseline: true
  - Unity process count for Project_PA: 0
  - Latest crash report: `PROJECT_PA_CRASH_REPORT_20260625.md`
  - Policy mode: `dry-run`
- Evidence path: `Automation/LoopEngineering/RunLogs/preflight-20260626.json`
- Screenshot evidence: none required
- AI visual review status: not applicable
- Human approval status:
  - Required for dirty Git baseline
  - Required for accepting the 2026-06-25 D3D11 crash repair baseline before launch-heavy automation
- Stop reason: dirty Git baseline and crash-report baseline gate
- Next action: human should approve the current baseline or request a cleanup/commit strategy before any implementation loop proceeds.

## 2026-06-26 - Baseline Commit Review

- Time: 2026-06-26
- Ticket ID: BASELINE-REVIEW-001
- Work type: Git baseline classification, documentation only
- Implemented:
  - Created `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md`
  - Classified dirty Git paths into recommended baseline, user-review, and exclude groups
  - Summarized D3D12 crash/D3D11 workaround baseline from `PROJECT_PA_CRASH_REPORT_20260625.md`
  - Documented staging commands for the user to run manually
- Changed gameplay files: none
- Git commands intentionally not run:
  - `git add`
  - `git commit`
  - `git push`
  - `git reset`
  - `git clean`
- Current branch: `master`
- Last commit: `adc5d2f 프로토타입 구성 -1`
- Dirty count: 141 status lines, 27 tracked modifications, 114 untracked paths
- Validator result: not applicable; this was a Git/documentation review
- Human approval status:
  - Required before staging optional large deliverables such as `SubmissionPackages/`
  - Required before accepting the D3D11 crash repair as the automation baseline
- Next action: user manually stages the approved groups and verifies `git diff --cached --stat`.

## 2026-06-26 - VC-001A Preflight Blocked

- Time: 2026-06-26
- Ticket ID: VC-001A
- Requested work: one next-day plaza/market visual change based on a sold product category.
- Required gate: `READY_FOR_BOUNDED_TICKET_LOOP`
- Actual preflight result: `BLOCKED_BY_DIRTY_GIT`
- Project root: correct (`C:\Users\sdjsd\Desktop\Unity\Project_PA`)
- Git root: correct (`C:/Users/sdjsd/Desktop/Unity/Project_PA`)
- Unity process count: 0
- Latest crash report: `PROJECT_PA_CRASH_REPORT_20260625.md`
- Implementation started: no
- Changed gameplay files: none
- Scene/code/assets modified for VC-001A: none
- Validator result: only `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1` was run; implementation validators were not run because the gate failed.
- Screenshot evidence: none, because no visual implementation was allowed.
- Stop reason: dirty Git baseline plus crash-report baseline must be reviewed before bounded ticket automation.
- Next action: human should approve or commit the current dirty baseline, review the D3D11 crash baseline, then rerun preflight before implementing VC-001A.

## 2026-06-26 - BASELINE-001 Baseline Preparation

- Time: 2026-06-26
- Ticket ID: BASELINE-001
- Work type: documentation and loop-harness baseline preparation
- Implemented:
  - Updated `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md` with current Git classification and GitHub Desktop staging guidance.
  - Created `Automation/LoopEngineering/State/crash-resolution.json`.
  - Updated `Automation/LoopEngineering/loop-policy.json` with D3D11-only bounded automation settings.
  - Updated `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1` so reviewed D3D11 crash resolution no longer blocks solely because the crash report exists.
  - Updated loop state, status, TODO, and session records.
- User-confirmed fact recorded:
  - User manually launched Unity on the `-force-d3d11` baseline and confirmed project opening and Play Mode stability.
- Not claimed:
  - D3D12 is not marked resolved.
  - No new Unity validator, build, or route result was invented.
- Preflight after harness correction:
  - Result: `BLOCKED_BY_DIRTY_GIT`
  - Crash resolution valid: true
  - Approved graphics backend: D3D11
  - Unity process count: 0
- Changed gameplay files: none
- Unity launched: no
- Git commands intentionally not run:
  - `git add`
  - `git commit`
  - `git push`
  - `git reset`
  - `git clean`
- Status: `needs_human_review`
- Next action: user creates the local checkpoint commit in GitHub Desktop, then reruns preflight.

## 2026-06-26 - VC-001A Started

- Time: 2026-06-26
- Ticket ID: VC-001A
- Work type: bounded implementation ticket
- Requested work: implement one next-day plaza/market visual change driven by an actual sold product category.
- Preflight result: `READY_FOR_BOUNDED_TICKET_LOOP`
- Git baseline: clean
- Unity process count: 0
- Crash resolution: `human_verified_d3d11_stable`
- Approved graphics backend: D3D11
- Implementation rule: use only existing Project_PA sales/category/Village Direction data; no Save schema change; no Project_D access/copy; no ProjectSettings change.
- Initial changed paths:
  - `Automation/LoopEngineering/State/loop-state.json`
  - `Automation/LoopEngineering/progress.md`
- Next action: inspect `VillageChangeSignalController`, `SalesLogManager`, item categories, day/night transition, and scene layout before editing runtime visuals.

## 2026-06-26 - VC-001A Completed

- Time: 2026-06-26
- Ticket ID: VC-001A
- Status: passed
- Implemented:
  - `Assets/Scripts/VillageCultureVisualController.cs`
  - `Assets/Editor/PA_VillageCultureVisualValidator.cs`
- Selected category: `Processed`
- Basis:
  - Default Day 1 route sells `BreadLoaf`.
  - `BreadLoaf` is an existing `Processed` item.
  - Existing `SalesLogManager` and `VillageChangeSignalController` already expose the category signal.
- Runtime behavior:
  - `PA_VillageCulture_Processed` exists at runtime and is inactive at Day 1 start.
  - A Processed sale creates a pending visual change.
  - The visual does not appear immediately after sale.
  - The visual appears during the next `DayPreparation`.
  - The hint appears once and does not show debug/probability/system-code text.
- Validation passed:
  - `PA_VillageCultureVisualValidator.RunVillageCultureVisualValidation`
  - `PA_FinalDemoRouteValidator.RunFinalDemoRouteValidation`
  - `PA_DayNightShopLoopValidator.RunDayNightShopLoopValidation`
  - `PA_VillageChangeSignalValidator.RunVillageChangeSignalValidation`
  - `PA_LongPlayProgressionValidator.RunLongPlayProgressionValidation`
  - `PA_CustomerPresentationValidator.RunCustomerPresentationValidation`
  - `PA_CustomerPanelLayoutValidator.RunCustomerPanelLayoutValidation`
- Evidence:
  - `Logs/Codex_VC001A_VillageCultureVisual.log`
  - `Logs/Codex_VC001A_FinalDemoRouteRegression.log`
  - `Logs/Codex_VC001A_DayNightRegression.log`
  - `Logs/Codex_VC001A_VillageSignalRegression.log`
  - `Logs/Codex_VC001A_LongPlayRegression.log`
  - `Logs/Codex_VC001A_CustomerPresentationRegression.log`
  - `Logs/Codex_VC001A_CustomerPanelLayoutRegression.log`
  - `Logs/VillageCultureVisual/20260626_145257/vc001a_day1_default_no_change.png`
  - `Logs/VillageCultureVisual/20260626_145257/vc001a_after_processed_sale_same_day.png`
  - `Logs/VillageCultureVisual/20260626_145257/vc001a_day2_preparation_visual_active.png`
- Preserved:
  - No Project_D copy/edit.
  - No Save schema change.
  - No scene file edit.
  - No package or ProjectSettings edit.
  - No Git add/commit/push/reset/clean.
- Remaining human gate: visual quality approval for the primitive processed-goods corner.

## 2026-07-18 — Task 105 Late-Night Customer Flow

- Status: implemented / project partial.
- Connected the existing 18:00–23:00 shop gate to residents whose schedules enter Rest at 19:00–20:00 through a Rest-only temporary visit lease.
- Preserved schedule phases, Work/Sleep, Day 1, customer caps, NPC purchase FSM, purchase math, schedule assets, save schema, scenes, prefabs, and packages.
- Outdoor and interior completion, failed-start, timeout, and close paths restore original position, shop reference, priority, and Rest behavior.
- Runtime/Editor builds: zero errors. Static contracts: 18/18 passed. Diff check: passed.
- Remaining gate: approved safe Unity run at 18:30, 20:30, 22:30, and 23:00 for traffic, cap, and recall evidence.

## 2026-07-18 — Task 106 Processed Village Visual Real-Asset Replacement

- Status: implemented / project partial.
- Removed the five runtime primitive cubes and generated materials from the Processed next-day village response.
- Instantiates only `Building_B05_Workbench.prefab/Visual`, plus the existing Project P.A. B05 preparation kit and labeled sign; the functional wrapper is never cloned.
- Disables/removes Collider, Rigidbody, NavMeshObstacle, MonoBehaviour, and extra Light components while preserving Raw exclusivity and v10 category persistence.
- Runtime/Editor builds: zero errors. Static contracts: 18/18 passed. Diff check: passed.
- Remaining gate: same-GameCamera Processed sale-day/next-day scale, facing, overlap, and player/NPC route evidence through an approved safe Unity path.

## 2026-07-27 — Task 107 Utility Next-Day Repair Point

- Status: implemented / project partial.
- Reused the sellable Utility Tool Set, Forge recipe, generic pending/next-day flow, and v10 category strings.
- Instantiates only the B07 `Visual` at 0.44 scale with the Project P.A. `공구 수리대` sign; the functional wrapper is never cloned.
- Removes Collider, Rigidbody, NavMeshObstacle, MonoBehaviour, and Light components and keeps Processed/Raw/Utility mutually exclusive.
- Runtime/Editor builds: zero errors after standard local restore. Static contracts: 23/23 passed.
- Remaining gate: actual Utility sale-day/next-day/save-restore and same-GameCamera scale, facing, overlap, and player/NPC route evidence through an approved safe Unity path.

## 2026-07-27 — Task 108 Luxury Next-Day Craft Display

- Status: implemented / project partial.
- Reused the sellable Furniture and Clothes Luxury items, their existing BasicWorkbench/SewingTable recipes, generic pending/next-day flow, and v10 category strings.
- Instantiates only the B08 `Visual` at 0.48 scale with the Project P.A. `공예 전시대` sign; the functional wrapper is never cloned.
- Removes Collider, Rigidbody, NavMeshObstacle, MonoBehaviour, and Light components and keeps Processed/Raw/Utility/Luxury mutually exclusive.
- Runtime/Editor builds: zero errors. Corrected static contracts: 27/27 passed after one selector-only false-negative rerun.
- Remaining gate: actual Luxury sale-day/next-day/save-restore and same-GameCamera scale, facing, overlap, and player/NPC route evidence through an approved safe Unity path.

## 2026-07-27 — Task 109 Hiring Candidate Product Flow

- Status: implemented / project partial.
- Repaired the eight-candidate smartphone dead end by resolving empty dedicated prefabs to exact-role existing C-02–C-09 resident sources with real skinned characters.
- Preserves explicit prefab priority, rejects hired clones as templates, validates before spending, and shares the resolver between new hiring and v10 restore.
- Injects candidate identity and matching existing specialist recipes; adds first-open cards, role/cost/lock state, and result feedback to the hiring UI.
- Runtime/Editor builds: zero errors. Corrected static contracts: 36/36 passed after one comment-selector false negative.
- Remaining gate: actual smartphone hiring, Producer/Specialist work behavior, save/reload restore, duplicate protection, and 1920×1080 readability through an approved safe Unity path.

## 2026-07-27 — Task 110 Week-One Hiring Milestone

- Status: implemented / project partial.
- Replaced the abstract Day 5 hiring-preparation copy with an actual P.A. Phone hiring action.
- Day 5+ objectives and the existing operation checklist now show no-hire guidance or the real candidate name, Korean role, and hired count.
- `OnHired` refreshes presentation only; the week-one result records a deterministic summary of the existing hired roster.
- Preserved Day 1–4, daytime/product/stock/price/open/sale/settlement checks, and all hiring/economy/NPC/save authority.
- Runtime/Editor builds: zero errors. Static contracts: 29/29 passed.
- Remaining gate: actual Day 5 hire transition, Day 7 roster summary, and 1920×1080 readability through an approved safe Unity path.

## 2026-07-27 — Task 111 Atomic Producer Delivery

- Status: implemented / project partial.
- Made `Inventory.AddInstance` capacity failure atomic through an exact read-only metadata-stack/empty-slot preflight.
- Producer delivery now checks full-stack capacity before spending, preserves stock on full-bag or insufficient-funds outcomes, transfers `ItemInstance` metadata on success, and refunds through the existing economy authority on an unexpected post-charge failure.
- Reused the existing NPC bubble for success, full-bag hold, required funds, and refund feedback.
- Preserved EconomyService, LongPlay code, save authority, NPC schedule/FSM/data, hiring, shop purchase/sale, scenes, prefabs, assets, and packages.
- Runtime/Editor builds: zero errors. Static contracts: 30/30 passed. Diff check: passed.
- Remaining gate: actual full-bag no-charge/no-loss hold → free-space → exact retry delivery and bubble readability through an approved safe Unity path.

## 2026-07-27 — Task 112 Week-Two Operations Campaign

- Status: implemented / project partial.
- Replaced the generic Day 8+ fallback with explicit Days 8–14 storage, processing, workforce, category-mix, Tier, village-response, and assortment plans.
- The player checklist reads existing B09 contents, same-day sales, the hired roster, Tier, and active village culture every 0.5 seconds without owning any transaction or persistence.
- Preserved Days 1–7, Day 7 completion/save → Day 8, the core daytime-to-settlement checklist, and all economy/inventory/sales/craft/hire/Tier/save authority.
- Runtime/Editor builds: zero errors. Static contracts: 40/40 passed. Diff check: passed.
- Remaining gate: actual Day 7→8 and representative Day 8–14 state transitions plus 1920×1080 readability through an approved safe Unity path.

## 2026-07-27 — Task 113 Tripo Re-Audit And B12 Collision Hardening

- Status: implemented / project partial.
- Reconciled the latest Tripo/Grid directive with the existing per-asset, identity-preserving, Placeable, source-preservation, and provenance ADRs.
- Recounted FBX 174 / OBJ 150 / GLB 0 / Blend 0 and confirmed 24 non-Nature-Pack project FBX files.
- Added a shrink-only runtime correction for active B12 map/legacy root `BoxCollider` and box-shaped `NavMeshObstacle`, based on all eight transformed Visual mesh-bound corners.
- Missing meshes preserve physics; no axis grows; trade, Placeable, save, source FBX, prefab, scene, data, and package boundaries remain unchanged.
- Runtime/Editor builds: zero errors. Static contracts: 20/20 passed. Target diff check: passed.
- Remaining gate: approved safe Unity coastal traversal, visible-boundary stop, NPC carving avoidance, and same-GameCamera evidence.

## 2026-07-27 — Task 114 Month-One Campaign And Day-30 Completion

- Status: implemented / project partial.
- Added explicit Days 15–30 plans over existing storage, processing, hiring, category sales, Tier, and village-change state.
- Added real-state checklist goals for all sixteen days without creating a new quest, transaction, unlock, or persistence authority.
- Added Day 30 Settlement completion summary and save-success-first finish or Day 30 save → existing next-day authority → Day 31 save continuation.
- Preserved Day 7 automatic-supply cutoff and week-one save → Day 8 behavior.
- The first parallel build hit a shared Runtime output lock and the first static script hit a parser error; both are resolved in `BUG_LOG.md` without repeating the failed commands.
- Sequential Runtime/Editor builds: zero warnings, zero errors. Static contracts: 56/56 passed. Target diff check: passed.
- Remaining gate: approved safe Unity representative Day 15–30 transitions, both Day 30 actions, Day 31 continuation/save, and 1920×1080 readability.

## 2026-07-27 — Task 115 Tier-One Forge And Tool-Set Value Chain

- Status: implemented / project partial.
- Restored B07 Blacksmith Forge placement to Tier 1, matching the existing `BuildingData`, blueprint, Forge workbench, Tool Set recipe, and Tool Set item authorities.
- Tier 1 now grants the B05 Basic Workbench and B07 Blacksmith Forge kits once, while B06 remains Tier 2 and B08 remains Tier 3.
- Replaced the unattainable Day 23/24 generic category goals with an exact playable chain: recover two adjacent shelves, place the 3×2 forge, turn Plank 1 and Ore 4 into Iron Bar 2, craft Tool Set 1, and sell it with the required assortment.
- Repaired the month revenue target regression so Day 15 starts at 16,000G after the explicit Day 14 15,000G target and grows monotonically through Day 30.
- Preserved economy, inventory, crafting, Tier, sales, placement persistence, save schema, scenes, prefabs, data assets, and package authority.
- Sequential Runtime/Editor builds: zero errors with the existing CS8785 and Editor CS0414 baseline warnings. Executable source contracts: 39/39 passed. Target diff check: passed.
- Remaining gate: approved safe Unity verification of Tier 1 kit grants, two-shelf recovery and B07 placement/access, the exact craft/sale chain, Day 23/24 completion, and 1920×1080 readability.

## 2026-07-27 — Task 116 Safe GameView Capture Foundation Pass 1

- Status: implemented / project partial.
- Audited twelve actual direct `camera.Render()` calls under Editor tooling.
- Extracted the exercised ThemeCorner GameView screenshot flow into a shared helper with resolution setup, Canvas/TMP stabilization, fresh PNG polling, minimum-size validation, and camera/screen restoration.
- Migrated the known ShopCustomization crash site and both current ShopProgression Tier captures to the shared asynchronous `ScreenCapture` path.
- Target direct-render calls are zero. Runtime build: zero warnings/errors. Editor build: zero errors with the existing CS8785/CS0414 warnings. Static contracts: 28/28 passed. Target diff check: passed.
- Ten audited direct-render sites remain and Unity was not launched under the repeated native-crash boundary.
- Remaining gate: bounded migration of the ten sites, repository-wide direct-render count zero, then human-approved isolated D3D11 GameView capture and sequential validator checks.

## 2026-07-27 — Task 117 Safe GameView Capture Foundation Pass 2

- Status: implemented / project partial.
- Migrated VillageCulture, CustomerPanelLayout, and FinalPresentation from local RenderTexture/direct-camera rendering to the shared ordinary GameView screenshot helper.
- Added guarded asynchronous sequencing so three village-state shots, one customer-panel shot, and six final-presentation shots complete before their next state mutation.
- Preserved the 1920x1080 market-marker framing, all-layer culling, optional capture-warning boundaries, and all six final-presentation filenames.
- Target direct-render calls are zero; repository remainder is seven in Character/Cottage/DemoView/GatheringShop/OutdoorPlacement/ShopEvolution/Workbench.
- Runtime build: zero warnings/errors. Editor build: zero errors with existing CS8785/CS0414 warnings. Corrected static contracts: 38/38 passed. Target diff check: passed.
- Unity was not launched under the repeated native-crash boundary.
- Remaining gate: migrate the final seven sites, prove repository-wide direct-render count zero, then obtain human approval for one isolated D3D11 GameView capture and sequential validators.

## 2026-07-27 — Task 118 Safe GameView Capture Foundation Pass 3

- Status: implemented / project partial.
- Migrated DemoView, GatheringShop, and OutdoorPlacement from local RenderTexture/direct-camera rendering to the shared ordinary GameView screenshot helper.
- Preserved one actual tracking-camera 2560x1440 capture with indoor/outdoor staging, five 1920x1080 day-to-night review captures, and two same-frame 1280x720 orthographic placement captures.
- Added awaited sequencing plus temporary `CameraController` freeze/restore; retained OutdoorPlacement's nontrivial PNG checks.
- Target direct-render calls are zero; repository remainder is four in Character/Cottage/ShopEvolution/Workbench.
- Runtime build: zero warnings/errors. Editor build: zero errors with existing CS8785/CS0414 warnings. Static contracts: 35/35 passed. Target diff check: passed.
- Unity was not launched under the repeated native-crash boundary.
- Remaining gate: migrate the final four sites in bounded passes, prove repository-wide direct-render count zero, then obtain human approval for one isolated D3D11 GameView capture and sequential validators.

## 2026-07-27 — Task 119 Safe GameView Capture Foundation Pass 4

- Status: implemented / project partial.
- Migrated Character, Cottage, and Workbench from local RenderTexture/direct-camera rendering to the shared ordinary GameView screenshot helper.
- Preserved the 1600x900 character source/idle/walk sequence, seven 1920x1080 cottage views, both four-direction 1920x1080 workbench audits, and selected runtime baseline/final evidence.
- Preserved character movement timing, cottage renderer isolation, and the Workbench placement/collider/real Wood-to-Plank/feedback contracts.
- Added awaited sequencing plus temporary `CameraController` freeze/restore and exception-safe temporary-object/renderer restoration.
- Target direct-render calls are zero; repository remainder is one in ShopEvolution.
- Runtime build: zero warnings/errors. Editor build: zero errors with existing CS8785/CS0414 warnings. Static contracts: 42/42 passed. Target whitespace check: passed.
- Unity was not launched under the repeated native-crash boundary.
- Remaining gate: migrate the final ShopEvolution site, prove repository-wide direct-render count zero, then obtain human approval for one isolated D3D11 GameView capture and sequential validators.

## 2026-07-27 — Task 120 Safe GameView Capture Foundation Final Pass

- Status: implemented / project partial.
- Migrated the final ShopEvolution local RenderTexture/direct-camera path to the shared ordinary GameView screenshot helper.
- Preserved twelve 1600x900 B02-B04 source turntable files plus the runtime baseline and Tier 1-3 after evidence, including four-second startup, 750ms tier settling, orthographic 6.6 framing, and filenames.
- Added guarded asynchronous stage sequencing, temporary `CameraController` freeze, `clearFlags` restoration, and the existing before/after shop-placement save-field equality check.
- Target direct-render resources and calls are zero; repository-wide actual direct-render invocation count is zero.
- Runtime build: zero warnings/errors. Editor build: zero errors with existing CS8785/CS0414 warnings. Static contracts: 36/36 passed.
- Unity was not launched under the repeated native-crash boundary.
- Remaining gate: obtain human judgment, then run one isolated D3D11 GameView PNG capture and inspect freshness, size, readability, and restored camera/screen state before sequential validators.

## 2026-07-27 — Task 121 Day 31-45 Second-Month Opening Campaign

- Status: implemented / project partial.
- Replaced the generic post-Day-30 guidance with fifteen authored Day 31-45 plans and fifteen live checklist milestones.
- Reused storage, processed sales, hired roster, product/category breadth, the Tier 1 forge and exact ToolSet, active village culture, prepared sellable types, and cumulative revenue without creating new progression or save state.
- Preserved Day 1-30, the Day 30 completion flow, the monotonic revenue curve (32,500G on Day 31 to 53,500G on Day 45), Tier 2 at 100,000G, B06 at Tier 2, and B08 at Tier 3.
- Runtime build: zero errors with the existing CS8785 warning. Editor build: zero errors with existing CS8785/CS0414 warnings. Static contracts: 23/23 passed.
- Unity was not launched under the repeated native-crash boundary.
- Remaining gate: verify Day 30->31, representative Day 35/40/45 state transitions, 1920x1080 readability, and the Day 46 fallback after human judgment authorizes a safe D3D11 run.

## 2026-07-27 — Task 122 Day 46-76 Tier 2 Growth Campaign

- Status: implemented / project partial.
- Generated a thirty-day Day 46-75 operating rhythm across reserve logistics, processed sales, workforce/catalog preparation, category breadth, the forge value chain, village direction, and cumulative-revenue checkpoints.
- Added a separate Day 76 breakthrough milestone that completes only from the actual automatic Tier 2 state.
- Preserved the existing revenue curve from 55,000G on Day 46 to exactly 100,000G on Day 76, Tier2.asset requirements, B06/B08 placement tiers, and all existing gameplay/save authorities.
- Runtime build: zero errors with the existing CS8785 warning. Editor build: zero errors with existing CS8785/CS0414 warnings. Static contracts: 34/34 passed.
- Unity was not launched under the repeated native-crash boundary.
- Remaining gate: verify representative Day 46/52/59/66/73 transitions, Day 76 automatic Tier 2 advancement, 1920x1080 readability, and the Day 77 fallback after human judgment authorizes a safe D3D11 run.

## 2026-07-27 — Task 123 Day 77-90 Tier 2 Kitchen Value Chain

- Status: implemented / verification partial.
- Added fourteen authored plans and matching live milestones for B06 placement, BreadLoaf, Baked Potato, Grilled Fish, menu breadth, full preparation, Chef support, category balance, batch sales, processed village culture, revenue review, and the Day 90 value-chain finale.
- Reused existing placement, inventory/hotbar/shelf, exact SaleLog, hired-roster, village-culture, and economy state without adding save/progression/content authority.
- Runtime build: zero errors with the existing CS8785 warning. Editor build: zero errors with existing CS8785/CS0414 warnings. Target whitespace check: passed.
- Static contract audit stopped on a checker defect: it expected three `TryResolveTierTwoKitchenMilestone(day` call sites although the objective and checklist correctly provide two. The failed selector was logged and not retried.
- Unity was not launched under the repeated native-crash boundary.
- Remaining gate: run one corrected two-call source-contract audit, then later verify B06 placement, all three craft-to-sale paths, Chef, processed culture, Day 90, and 1920x1080 readability after human judgment authorizes a safe D3D11 run.

## 2026-07-27 — Task 124 Task 123 Kitchen Contract Recovery

- Status: verification partial.
- The corrected two-call objective/checklist contract passed.
- Fourteen authored plans, fourteen runtime cases, one evaluator definition, the Day 76 boundary, B06 Kitchen prefab, three Kitchen recipes, three Processed output categories, and all required resources passed.
- Result: 44/47. The B06 minimum-tier source expression and two Korean output-name YAML expressions did not match the audit patterns.
- The combined audit was not retried and no implementation or Unity state changed.
- Remaining gate: inspect the three authoritative source lines first, classify data defect versus checker mismatch, then resume implementation after the contract is resolved.

## 2026-07-27 — Task 125 B06 Tier and Output-Name Authority Audit

- Status: done.
- Inspected only the three authoritative lines; the combined audit was not rerun.
- B06 resolves to Tier 2 through an ordinary switch `case ... return 2`.
- Baked Potato and Grilled Fish names are valid Unity YAML Unicode-escaped Korean strings.
- The three Task 124 failures were checker-format mismatches, so Task 123 static evidence is 47/47 using 44 automated contracts plus three direct authoritative checks.
- No implementation or Unity state changed.
- Next: resume implementation at Day 91 onward.

## 2026-07-27 — Task 126 Day 91-105 Tier 3 Community Atelier Campaign

- Status: implemented / project partial.
- Found Tier 3 unreachable through normal play because its existing reputation requirement was 3 and no gameplay caller invoked `AddReputation`.
- Connected completed specialist resident requests to one saved `tier3-reputation:{day}` marker and one reputation point per DayPreparation while the shop is Tier 2.
- Added fifteen authored plans and live milestones for reputation 1/2/3, automatic Tier 3, B08, exact Clothes/Furniture preparation and sales, Tailor support, Luxury village culture, category/product breadth, revenue, and the Day 105 finale.
- Preserved tier data, recipes, items, economy, save schema, scenes, prefabs, assets, packages, and player agency.
- Runtime build: zero errors with the existing CS8785 warning. Editor build: zero errors with existing CS8785/CS0414 warnings. Static contracts: 24/24 passed.
- Unity was not launched under the repeated native-crash boundary.
- Remaining gate: verify the three-day request/reputation route, Tier 3 advancement, B08 grant/placement, both Luxury craft-to-sale paths, village change, Day 105, and 1920x1080 readability.
- The attached GRID/Tripo directives remain bound to the existing placement architecture, customization roadmap, placeable guide, and Tripo audit rather than a parallel rewrite.

## 2026-08-04 — Task 127 B05-B08 Specialist Front-Cell Approach

- Status: implemented / static validation complete / project partial.
- Reused each placed Workbench definition's rotated interaction cells instead of sending specialist NPCs to the collider and NavMeshObstacle center.
- Added complete-path selection, per-cell reservation, arrival facing, movement/recovery invalidation, schedule/disable cleanup, and restore-time reacquisition. Legacy non-grid workbenches use a collider-front fallback.
- Preserved scenes, prefabs, source models, materials, recipes, items, economy, save schema, packages, and ProjectSettings.
- The previous continuation's path and documentation-check failures were recovered from direct authoritative evidence and closed in `BUG_LOG.md`.
- Runtime build: zero errors with the existing CS8785 warning. Editor build: zero errors with existing CS8785/CS0414 warnings. Static contracts: 29/29 passed.
- Unity was not launched under the repeated native-crash boundary. Actual B05-B08 specialist approach, facing, reservation overlap prevention, and crafting remain runtime-unverified.

## 2026-08-04 — Task 128 Normal-Play Tourist Customer Entry

- Status: implemented / static validation complete / project partial.
- Audited all eight authored customer NPCs: every one has a valid resident schedule, and the existing presentation validator explicitly required zero tourists. No normal-play tourist generation path existed.
- Added a bounded Day 2+ visitor lifecycle to `CustomerArrivalController`: at most two tourists per opening and one concurrently, using only a valid authored profile/schedule source and its real SkinnedMesh character visual.
- Tourist instances receive a runtime-only profile and existing customer controller/bubble flow, but no resident schedule, producer, specialist, or dialogue role and no save registration.
- Visitors enter from a complete NavMesh path outside the shop, use the existing `NpcController` shopping FSM and `PurchaseEvaluator`, keep the purchase/rejection reaction visible briefly, then return to their entry point and clean up. Shop close, timeout, and controller destruction also clean them up.
- Preserved Day 1 tutorial behavior, normal resident arrivals, customer capacity, purchase math, economy, save schema, scenes, prefabs, packages, and ProjectSettings.
- Runtime build: zero errors with the existing CS8785 warning. Editor build: zero errors with existing CS8785/CS0414 warnings. Static contracts: 46/46 passed.
- Unity was not launched under the repeated native-crash boundary. Actual tourist entry, purchase/rejection, exit, visual identity, and concurrent resident behavior remain runtime-unverified.
- Next: audit the Day 106+ generic long-play fallback and connect one smallest gap using existing authority; do not deepen the tourist system first.

## 2026-08-04 — Task 129 Day 106+ Headquarters Audit And Tier 4 Finale

- Status: implemented / static validation complete / project partial.
- Audited the only normal-play `AddReputation` caller and found it stopped at Tier 2 reputation 3 while the existing headquarters audit requires reputation 5, making Tier 4 unreachable.
- Extended the existing saved daily specialist-request marker only for Day 106+ Tier 3 operation, granting one reputation per day up to the live `AuditService.requiredReputationForAudit` value.
- Added one shared partner-campaign status resolver used by the long-play objective and player checklist. It reads the live audit reputation, hiring, revenue, last-audit-day, and interval authorities in that order.
- Preserved `AuditService` as the sole `TierService.TryManualAdvance` caller; no force-tier path or audit-balance change was added.
- Reused the existing milestone completion UI after a real Day 106+ Tier 4 settlement, showing campaign revenue, money, tier, reputation, workforce, village direction, and closing sales, with save-and-continue free operation or save-and-quit.
- Preserved the save schema by deriving post-continue acknowledgement from the existing saved `lastAuditDay`.
- Runtime build: zero errors with the existing CS8785 warning. Editor build: zero errors with existing CS8785/CS0414 warnings.
- Static evidence: 40 automated contracts passed plus one direct implementation-scope audit. The combined clean-worktree assumption failed on pre-existing dirty Save/Package/Crop files and was not rerun; it is recorded as a resolved checker defect.
- Unity was not launched under the repeated native-crash boundary. Actual Day 106/107 reputation 4/5, scheduled audit, Tier 4, finale, save/continue/quit, and 1920×1080 readability remain runtime-unverified.
- Next: audit whether audit pass/fail and final unlock results are player-visible; if still log-only, connect one smallest feedback path through the existing audit app and completion presentation.

## 2026-08-04 — Task 130 Headquarters Audit Player Feedback

- Status: implemented / static validation complete / project partial.
- Confirmed the audit app only showed the next audit date while failure, pass, and advancement-blocked outcomes remained in console/Inspector state.
- Added read-only live requirement/result state and one refresh event to `AuditService`; every terminal audit branch publishes through the same path.
- The existing audit app now shows exact revenue/reputation/hiring values, met/missing states, next audit day, the latest result, the closest next action, and the Tier 4 unlock result. Manual-tier progress now reflects the three real audit requirements.
- Preserved the 500000G, reputation 5, three hires, seven-day interval, sole manual-advance call, LastAuditDay persistence, scenes, prefabs, save schema, economy, hiring, tier data, packages, and ProjectSettings.
- Runtime build: zero errors with the existing CS8785 warning. Editor build: zero errors with existing CS8785/CS0414 warnings. Static contracts: 48/48 passed.
- Unity was not launched under the repeated native-crash boundary. Actual failure/pass refresh and 1920x1080 phone readability remain runtime-unverified.
- Next: audit existing AudioManager and provenance-known in-project clips, then connect one highest-impact missing purchase/open/settlement sound without introducing external assets or changing gameplay authority.

## 2026-08-04 — Task 131 Tripo Policy Recheck And B06 Kitchen Functional Visual

- Status: implemented / static validation complete / project partial.
- Confirmed the attached GRID directive is already represented by the shared 2m GridService zones, multi-cell footprint/clearance/interaction, rotation, move/recover, NPC reservations, and v10 placeable persistence. No parallel placement system was added.
- Recounted 174 FBX, 150 OBJ, zero GLB, zero Blend, and 24 non-Nature project FBX files. The per-asset 1-8 decisions, character-identity preservation, source preservation, and provenance deployment gates remain authoritative in `TRIPO_ASSET_AUDIT.md`.
- Finalized the next exposed incomplete functional asset, B06 Kitchen, by shrinking only clearly oversized X/Z BoxCollider axes to renderer bounds plus padding and synchronizing the box-shaped carving obstacle. No axis grows and missing renderers preserve existing physics.
- Added a runtime local -Z interaction anchor and a 0.72-second, 3.5% maximum pulse of the existing B06 model after a successful craft transaction.
- Preserved B05 functional art, B06 Tier 2 / 2x2 / Kitchen recipes, specialist access, persistence, source FBX, prefab, scene, materials, BuildingData, packages, and ProjectSettings.
- Runtime build: zero errors with existing CS8785 warning. Editor build: zero errors with existing CS8785/CS0414 warnings. Static contracts: 30/30 passed.
- Unity was not launched under the repeated native-crash human-judgment gate. Actual placement, player/Chef approach, physical boundary, Bread feedback, and same-camera before/after remain runtime-unverified.
- Next: after safe Unity approval, validate B06 once; otherwise select B07 Forge functional presentation or return to the existing audio feedback priority as a separate bounded task.

## 2026-08-04 — WORLD-000 Procedural Island + Grid Terraforming Architecture Reframe

- Active ticket: WORLD-000, investigation/design/documentation only. Started and stopped on 2026-08-04; WORLD-001 was not started.
- Preflight: Task-131 was the completed loop-state. The worktree already contained 136 changed paths and Unity process count was zero. The unverified `AudioManager.cs`/`SalesLogManager.cs` edits from the paused work were preserved and excluded from WORLD-000.
- Audited: `GridService`, `BuildManager`, `BuildingData`, `BuildingRegistry`, `OutdoorPlacementController`, shop customization/expansion, v10 `SaveData`/`SaveManager`/repositories, farmland/crop/gathering/day activities, NPC/customer/producer/specialist/anchors, Shop/ShopSlot/sign/evolution/interior, both main scenes, `PA_MapLayoutBuilder`, `PA_RuntimeSceneBinder`, manifest and local AI Navigation 2.0.12 source.
- Scene evidence: current map is primitive/mesh flat ground rather than an authoritative Unity Terrain; both main scenes use Unity 6 binary serialization and were not edited. `WorldSandbox.unity` does not yet exist.
- Recommended architecture: 2m logical cells, 16x16-cell custom mesh chunks, 1m elevation levels 0..6, configurable 128x128 default island, seed + generationVersion + sparse player deltas, atomic building movement, stable role anchors, cell-graph preflight plus per-chunk/sector NavMeshSurface proof.
- Local API evidence: AI Navigation 2.0.12 `NavMeshSurface.UpdateNavMesh(NavMeshData)` calls async `NavMeshBuilder.UpdateNavMeshDataAsync`; Volume bounds are supported. Runtime chunk seams/performance remain unproven and are assigned to WORLD-008.
- Scene strategy: Prototype_FirstDay is the Golden Regression Scene; approved WorldSandbox is the new-world testbed; MainGame is a separate Gate 1~5 integration candidate.
- Outputs: five new world documents plus the requested direction/status/rule/loop records. The user-enumerated 19 documentation paths and two mandatory repository handoff logs form an explicit documentation-only exception to the general 15-path ticket cap.
- Actual game files modified by WORLD-000: none. No code, scene, asset, prefab, SaveData schema, Package, ProjectSettings, Unity state, Git history, or external tool changed.
- Read-only command recovery: one unsupported PowerShell Latin1 property was replaced once by code page 28591; one broken regex quoting command was replaced once by split fixed patterns. Neither changed the project and neither failure was repeated.
- Documentation checker recovery: the first exact-string scene Gate check expected unpunctuated labels while the plan used abbreviated table labels. The required content was present; labels were normalized to the official `Gate N: Name` form and only the two missed terms were directly rechecked. The combined checker was not rerun.
- Content-matrix recovery: the North Star expressed all four world pillars in Korean prose but omitted the four required official English labels. Added only the explicit definition block and directly checked those four labels; the 21-term combined matrix was not rerun.
- Human decisions: approve scene separation/WorldSandbox creation; accept 2m/16/1m as prototype baseline; approve WORLD-001 scope; later approve vNext save schema, WORLD-008 runtime navigation, and MainGame integration choice.
- Final status: `needs_human_review`. This reflects follow-up implementation gates, not incomplete WORLD-000 documentation.
- WORLD-001 start condition: a new explicit bounded-ticket instruction after the scene/baseline/scope gates; do not auto-continue.

## 2026-08-04 — BASELINE-STABILIZE-001 Current Playable Baseline Integration Validation

- Started from clean `master` at `0b07d71`, synchronized with `origin/master`; preflight returned `READY_FOR_BOUNDED_TICKET_LOOP` with D3D11 approved and no Unity process.
- Asset/meta/GUID/history integrity passed: no missing or orphan meta, duplicate GUID, tracked SubmissionPackages ZIP, >100 MB history blob, dirty scene, logical ProjectSettings content change, or new crash artifact.
- Runtime and Editor builds passed with zero errors. Existing warnings remain CS8785 (Runtime/Editor) and CS0414 in `PA_ErrorTracker` (Editor).
- D3D11 Unity 6000.3.2f1 load and package resolution passed. The CoplayDev Unity MCP package compiled; the licensing token-refresh message did not prevent entitlement resolution.
- Core route validators passed: FinalDemoRoute, DayNightShopLoop, LongPlayProgression, CoreSlicePlayability, GatheringShopGate, and SaveRoundTrip.
- CustomerArrival, CustomerPresentation, and InteriorCustomer passed. CustomerPanelLayout found a real 4 px preference/village overlap; one minimal runtime-UI coordinate fix moved the preference panel down 20 px and the sole rerun passed.
- ShopCustomization functional checks passed through placement, movement, save restore, functionality, and protected route, then failed because batchmode GameView capture was not written. ShopProgressionUnlock passed Tier 0 functional assertions and hit the same capture timeout.
- The repeated identical GameView capture failure triggered the ticket stop rule. No more Unity validators were launched; Village/economy, mining, outdoor placement, and remaining visual validators are unrun, while farming validators are not available.
- Save authority remains v10 with v9→v10 placement migration. AudioManager/SalesLogManager static event, exception-isolation, lifecycle, pool, fallback, and generated-clip contracts are consistent; hearing remains human review.
- Task 131 B06 static physics/carving/anchor/pulse contracts remain present, but approach feel, collider feel, scale, view, and pulse subtlety remain human review.
- Final state: `NEEDS_HUMAN_RUNTIME_REVIEW`. WORLD-001 and WorldSandbox were not started.

## 2026-08-05 — BASELINE-CRAFTING-UI-FIX-001 Restore Visible Basic Crafting Recipe Cards

- Started on `master` at `0b07d71e7dc6d259713a97d2011d181efa73b200`; the seven already accepted baseline-review changes were preserved. No scene, prefab, Save schema, Package, or ProjectSettings path changed.
- The runtime-only hierarchy is `CraftingOverlay > CraftingPanel > RecipeScroll > Viewport(Image+Mask) > Content > Recipe_*`. Basic data contained exactly two recipes and `GenerateSlotsForContext` instantiated both, but the Viewport Mask graphic used `Color.clear`. Its zero alpha prevented the stencil from exposing either card while the independent count text still reported two.
- Changed the mask graphic to opaque white while retaining `showMaskGraphic=false`, and forced the generated Content layout before the first visible frame. No recipe filtering, crafting authority, inventory, unlock, or workbench rule changed.
- Added the bounded `PA_CraftingRecipeCardValidator`. D3D11 Play Mode proved 2 recipes = 2 cards, both active, 672×96, inside Viewport bounds, visible alpha, and containing output plus owned/required ingredient text.
- With zero Wood, both cards remained visible and the Plank selection path consumed/granted nothing. With two Wood, the same UI path consumed 2 Wood, granted 1 Plank, and drove the production B05 success pulse. The existing ProcessingChain validator also passed with BreadLoaf and a positive margin.
- The first dedicated run used a scene placeholder workbench that had no B05 functional-art feedback and produced a validator-fixture false negative only after crafting, consumption, and output had passed. The validator was corrected to instantiate the production B05 prefab; the single rerun passed every assertion. Production code was not broadened for that recovery.
- Runtime and Editor compilation passed with zero errors. Final ticket logs contain zero blocking exception/crash patterns and no new crash report. The exact Input Manager notice remains covered by the previously approved human smoke result and did not appear in these batch runs.
- Game View capture was not attempted because the known timeout had already reached its retry boundary. Hierarchy, alpha, geometry, bounds, and functional evidence passed, so this remains `CAPTURE_EVIDENCE_DEBT` rather than a ticket failure.
- Final state: `BASELINE_READY_FOR_WORLD_001`. WORLD-001 and WorldSandbox were not started; wait for the next explicit bounded ticket.

## 2026-08-06 — WORLD-001 WorldSandbox Bootstrap and Read-Only World Cell Grid

- Started from clean `master` at `befc1738dd868d24b06a2c8f673a13293b60d36b`, synchronized with `origin/master`, using Unity 6000.3.2f1 and D3D11.
- Created `Assets/Scenes/WorldSandbox.unity` only through an Editor builder. The authored scene contains exactly Main Camera, Directional Light, and WorldGrid roots; no shop, NPC, save, or gameplay-manager copy is authored.
- Added the deterministic read-only baseline: 16×16 cells, 2 m cell size, one 16×16 chunk, 1 m elevation steps with allowed levels 0..6, initial elevation 0, Default ground, Empty occupancy, and world origin at the centre of cell (0,0).
- Added cell/index/world/chunk conversion and safe `Try*` bounds APIs. Storage is a read-only row-major collection and no public cell mutation API exists.
- Added a runtime line-only debug view for regular cells, chunk boundary, origin, sparse coordinates, and hover inspection. It does not generate terrain, water, paths, save data, or editable world state.
- Recovery: the first reload exposed Unity's MonoBehaviour filename requirement. The existing editor-tool asset GUID was moved to the matching `WorldGridDebugView.cs` filename, then the scene was rebuilt. A second validator-only assumption was narrowed from exactly three runtime roots to exactly three authored roots plus zero duplicated runtime managers. Both recoveries are recorded in `BUG_LOG.md`.
- Final D3D11 validation passed: 256 unique cells; four corners and out-of-bounds behavior; 256 index round trips; chunk (0,0); 10,000 seeded world/cell round trips; immutable data/API contracts; deterministic checksum `AD517449E587DBE5`; debug geometry; scene references; and blocking Console Error/Exception/Assert 0.
- Runtime and Editor compilation passed with no errors. Only pre-existing CS8785 and CS0414 warnings remain. No new crash was created.
- `Prototype_FirstDay.unity`, `MainGame.unity`, SaveData/SaveManager/repository authority, Packages, and ProjectSettings remain byte-for-byte unchanged from the ticket baseline.
- Capture was not attempted. This is `CAPTURE_EVIDENCE_DEBT`, not a functional failure, because hierarchy, geometry, bounds, determinism, and D3D11 Play Mode checks passed.
- Final state: `WORLD_001_COMPLETE`. Stop here; WORLD-002 requires a new explicit bounded ticket and is not started.

## 2026-08-06 — WORLD-002 Chunk Testbed and Height-Level Mesh Prototype

- Continued M70 on `milestone/world-alpha-70` from WORLD-001 commit `776fd3a`; no protected scene, Save authority, Package, or ProjectSettings path was changed.
- Added deterministic `World002Terraces` bootstrap levels 0..6 and a pure chunk mesh builder. The single 16×16 chunk produces 256 top faces, 280 exposed cliff faces, 2,144 vertices, normals/UVs and checksum `ADF9201BC8265BC5` without Unity Terrain or per-cell GameObjects.
- `WorldChunkTerrain` owns separate runtime visual/collider meshes, two project-owned runtime material slots, a `MeshCollider`, and isolated visual/collider dirty revisions for later selective rebuilds.
- The first synthetic seam check included lower cliff-cap vertices in an upper-edge count. The contract was narrowed to the 17 top-edge positions; the one allowed rerun passed. A WORLD-001 regression command typo was also corrected once to its actual public entry point. Both are recorded in `BUG_LOG.md`.
- Final D3D11 Edit/Play validation passed mesh streams, winding, finite values, deterministic checksum, two-chunk seam equality, collider/bounds, dirty-rebuild isolation, one chunk object, and blocking Console 0.
- WORLD-001 D3D11 regression passed all 256-cell, 10,000-round-trip, debug-view and read-only contracts. Runtime/Editor builds passed with zero errors; only existing CS8785/CS0414 warnings remain; new crash count is zero.
- Optional capture was skipped and recorded as `CAPTURE_EVIDENCE_DEBT` because geometry, seams, collider, bounds, materials and Play Mode contracts passed.
- Final state: `WORLD_002_COMPLETE`. After the local ticket commit, continue automatically to existing backlog ticket WORLD-003 only.

## 2026-08-06 — WORLD-003 Single-Cell Raise/Lower Terraforming

- Added transaction-owned one-cell elevation editing while keeping `WorldGridService.Cells` read-only to consumers. Raise/lower changes exactly one level within 0..6 and preserves ground, water, path and occupancy data.
- Cell (0,0) is explicitly protected. Out-of-bounds, protected, minimum, maximum and unavailable-undo requests return typed failures with no cell hash, revision, dirty chunk or mesh change.
- Dirty-chunk resolution returns only the owner plus cardinal seam-sharing chunks. Synthetic 32×16 checks prove both sides of the x=15/16 boundary dirty chunks (0,0) and (1,0), while an interior edit dirties one chunk.
- `WorldChunkTerrain` subscribes to successful edits and rebuilds visual mesh and `MeshCollider` exactly once. Adjacent cliff masks, collider raycast height and visual/collider bounds update immediately.
- WorldSandbox debug controls are left-click selection, `R` raise, `F` lower and `Z` one-step undo; the overlay reports selection, protected state, result and revision. No production UI was added.
- `Logs/WORLD003_Validation.log` passed D3D11 Edit/Play checks on the first run. WORLD-002 and WORLD-001 D3D11 regressions also passed; Runtime/Editor compilation has zero errors and no new crash exists.
- No scene, Save schema, Package, ProjectSettings, water/path, building, NPC or NavMesh path changed. Optional capture remains `CAPTURE_EVIDENCE_DEBT`.
- Final state: `WORLD_003_COMPLETE`. After the local ticket commit, continue automatically to existing backlog ticket WORLD-004 only.

## 2026-08-06 — WORLD-004 Ground/Path Paint and Water Cell Prototype

- Continued on `milestone/world-alpha-70` from committed WORLD-003 baseline `de1ada54edb4db2f5719c11ffc0826f9007c73ff`; the ticket result remains uncommitted for human review.
- Extended the cell contract with four ground types, two path types and bounded water surface/depth. Farmability and walkability remain derived rather than duplicated authority.
- Added atomic ground/path/water transactions, typed protected/invalid/path-water failures, owner-plus-seam dirty chunks and one-step rollback. Failed edits publish no revision or dirty chunk.
- Routed visual tops through eight runtime material slots and generated water tops plus exposed shoreline faces. Water triangles are absent from the terrain collider streams, so runtime raycasts remain on the terrain bed.
- Added WorldSandbox debug controls `G` ground, `T` path, `V` water and `X` surface undo while preserving WORLD-003 left-click/R/F/Z controls.
- The initial continuation compile issue was fixed by separating water edit success and cell lookup. The first D3D11 validator then exposed a validator-only vertex-order assumption; direct set-disjointness replaced it and the single rerun passed. Both recoveries are resolved in `BUG_LOG.md`.
- `Logs/WORLD004_Validation.log` passed Edit/Play surface, material, shoreline, collider, rollback and Console contracts. WORLD-003/002/001 regression logs all finished PASS. Runtime/Editor compile errors and new crashes are zero.
- Prototype_FirstDay, WorldSandbox and MainGame blobs match HEAD. No Prefab, Save schema/authority, Package or ProjectSettings content changed. Optional capture remains `CAPTURE_EVIDENCE_DEBT`.
- Final state: `WORLD_004_COMPLETE`. Stop here without commit or WORLD-005 work; await explicit user instruction.

## 2026-08-10 — WORLD-005 Relocatable Building MVP

- Continued the preapproved M70 sequence on `milestone/world-alpha-70` from committed WORLD-004 baseline `c23f4be`.
- Added a sidecar definition for the existing `B09_StorageShed`: a conservative 4x3 footprint on 2m cells, an exterior entrance offset that rotates with the footprint, and no source prefab or `BuildingData` schema change.
- Added a single WorldGrid occupancy writer with atomic place/move/remove batches. Water, paths, protected cells, occupied cells, out-of-bounds cells, invalid ground, blocked entrances and uneven/cliff-spanning footprints return typed failures without partial mutation.
- Added an existing-model green/red ghost and WorldSandbox controls: `B` place, `M` move, `Q/E` rotate, `Enter` confirm and `Escape` cancel. The committed instance preserves `StorageBox`; occupancy makes footprint cells non-walkable and blocks height/surface editing.
- The first Editor save revealed scene-local MonoScript references because two attachable classes shared a non-matching filename. Service and debug controller were split into filename-matched scripts, GUID/meta references were rebuilt through the Editor API, and the scene now has zero embedded MonoScripts and the same three roots.
- `Logs/WORLD005_Validation.log` passed Edit/Play ghost, rotation, entrance, water/cliff/overlap rejection, cancel, occupied edit guards, rollback, atomic old/new occupancy, one registry record and exact baseline checksum restoration. WORLD-004~001 regressions all finished PASS.
- Runtime/Editor compile errors, blocking Console errors and new crashes are zero. Only existing CS8785/CS0414 warnings remain. Prototype_FirstDay, MainGame, prefab sources, Save schema, Packages and ProjectSettings are unchanged.
- The optional capture was skipped as `CAPTURE_EVIDENCE_DEBT`; renderer presence, prefab identity, transform, footprint and runtime behavior are automatically proven.
- Final state: `WORLD_005_COMPLETE`. Create the approved local ticket commit, then continue to existing backlog `WORLD-006 World Seed + Minimal Island Generator`.

## 2026-08-10 — WORLD-006 World Seed + Minimal Island Generator

- Continued the preapproved M70 sequence from clean WORLD-005 commit `971d23d9c5c186299e33a59f263df0ab39773414`.
- Added generationVersion 1 with a configurable but provisional 128x128 definition, 2m cells and 16x16 chunks. The integer hash/value-noise pipeline uses no Unity/System random state or special-case seed.
- Generated a bounded irregular island with ocean border, sand coast, meadow, forest, highland, minimal river and pond. A safe start plateau, independent flat 4x3 shop candidate, beach and four activity anchors are connected by dry cell routes with elevation steps no greater than one.
- Added stable forage/timber/stone/fish spawn keys derived from generationVersion, seed, kind and coordinate; no final resource prefab or save payload was created.
- WorldSandbox `J` builds the selected seed as 64 existing custom-mesh chunks, `[`/`]` changes seed and `K` clears. The view uses eight existing surface slots and no per-cell GameObjects; it remains idle on Play start to preserve regressions.
- `Logs/WORLD006_Validation.log` passed 128 seeds generated twice: all deterministic, 128 unique checksums, land ratio 44.9%..58.2%, all anchors reachable, and 256 generations in 1599ms. D3D11 sample mesh generation was 52ms/31ms with water and shoreline geometry.
- WORLD-005~001 regressions all finished PASS. Runtime/Editor compile errors, blocking Console errors and new crashes are zero; only existing CS8785/CS0414 remain.
- Prototype_FirstDay, MainGame, prefab sources, Save schema, Packages and ProjectSettings are unchanged. Capture remains `CAPTURE_EVIDENCE_DEBT`.
- Final state: `WORLD_006_COMPLETE`. Create the approved local ticket commit, then activate preapproved `WORLD-006B Movable Shop Furniture`.

## 2026-08-10 — WORLD-006B Movable Shop Furniture

- Reused the existing `ShopCustomizationController` and `shop.interior` 2m grid as the sole interior furniture authority; no parallel world-furniture service, scene, prefab or save schema was added.
- The dedicated no-capture D3D11 validator moved one real authored `ShopSlot` to `(4,0)`, rotated it 270 degrees, preserved its hierarchy/component/stock/price, and proved an in-bounds authored approach with a complete customer NavMesh path.
- Protected entrance movement was rejected atomically and the entry-to-service route remained connected. The first run found a real authored-offset snap on move cancellation; the controller now captures the exact move-start Transform and restores it on cancel.
- A validator-only assumption expected one-unit sales, while the real ShopSlot sold its two-item stack for 146G. The assertion was corrected to the established full-stack contract; production sale code was unchanged.
- Existing v10 `PlaceableSaveData` projected the same stable instance ID, `shop.interior`, cell `(4,0)` and rotation 3, preparing WORLD-007 without changing schema 10.
- `Logs/WORLD006B_Validation_Pass.log`, InteriorCustomer, SaveRoundTrip and FinalDemoRoute all passed. Runtime/Editor compile errors, blocking Console errors and new crashes are zero; existing CS8785/CS0414 remain.
- Prototype_FirstDay was opened read-only and remained clean. WorldSandbox, MainGame, prefabs, SaveData, Packages and ProjectSettings are unchanged; capture remains nonblocking `CAPTURE_EVIDENCE_DEBT`.
- Final state: `WORLD_006B_COMPLETE`. Create the approved local ticket commit, then activate preapproved `WORLD-007 World Persistence`.

## 2026-08-10 — WORLD-007 World Persistence

- Advanced the additive save schema to v11. Existing v10 payloads migrate only to `LegacyFixed`; absolute legacy buildings and placeables are not silently converted into procedural records.
- Added `worldSeed` plus `generationVersion`, sparse modified terrain cells, stable B09 building records, `shop.interior` furniture projection, stable generated-resource state and a safe player position/cell.
- Restore performs complete preflight before live mutation, regenerates the deterministic base, applies four terrain delta kinds, derives building occupancy through the placement authority and prevents duplicate building/furniture restoration.
- Corrupt out-of-range deltas and missing generation versions fail without partial world mutation. The existing local JSON repository remains authoritative; crash-safe temp/backup writing is separate debt.
- Fixed the dry-cell terraform invariant so a height edit keeps its non-water surface sentinel aligned. Both bounded validator recoveries are closed in `BUG_LOG.md`.
- `Logs/WORLD007_Validation_Pass.log` and SaveRoundTrip/WORLD-004/WORLD-005/WORLD-006 regressions passed under D3D11. Runtime/Editor compile errors, blocking Console errors and new crashes are zero.
- No Scene, Prefab, Package or ProjectSettings content changed. Capture remains nonblocking `CAPTURE_EVIDENCE_DEBT`.
- Final state: `WORLD_007_COMPLETE`. After the approved local ticket and recovery-record commits, activate preapproved `WORLD-008 Reachability and Navigation Prototype`.

## 2026-08-10 — WORLD-008 Reachability and Navigation Prototype

- Reused the already installed official AI Navigation 2.0.12 package; Packages and ProjectSettings remained unchanged.
- Added one logical cardinal cell graph with a maximum one-level step. Seed 8008 reached all seven critical anchors through 8,755 cells, while a complete water barrier was correctly isolated.
- Building placement now asks the navigation authority before commit. A B09 move that severed the only critical corridor and its entrance was rejected with `CriticalRouteBlocked`, preserving the original transform and occupancy.
- Added 2x2-chunk (32x32-cell) local `NavMeshSurface` sectors, one-cell overlap, grouped equal-elevation seam links and an async dirty queue. An interior edit rebuilt one sector; a boundary edit rebuilt exactly two.
- The affected runtime NPC test agent paused, reprojected within 2m, repathed, crossed a sector seam, arrived and settled without jitter. A provisional 128x128 seed built 4x4=16 sectors in 57ms and produced a complete start-to-shop-entrance path.
- Persistence preflight now rejects a saved B09 that would isolate critical anchors before live world mutation. Save schema and repository authority did not change.
- `Logs/WORLD008_Validation_Final.log`, WORLD-007/005/006, InteriorCustomer and FinalDemoRoute regressions passed. Runtime/Editor compile errors, dedicated blocking Console errors and new crashes are zero.
- InteriorCustomer emitted one post-pass Play Mode teardown diagnostic (`Failed to create agent because there is no valid NavMesh`) after its successful full visit; retain as nonblocking teardown harness debt.
- No Scene, Prefab, Package or ProjectSettings content changed. Capture remains `CAPTURE_EVIDENCE_DEBT`.
- Final state: `WORLD_008_COMPLETE`. Create the approved local ticket commit, then activate preapproved `WORLD-009 Existing Gameplay World Adapter`.

## 2026-08-11 — WORLD-009 Existing Gameplay World Adapter

- Added a WorldSandbox runtime-only adapter and adopted the existing `PA_RuntimeSceneBinder` authorities instead of creating parallel Inventory, economy, crafting, shop, customer, clock or save systems.
- Stable Timber spawns grant Wood through Inventory; the real Recipe_Plank/CraftingService/B05 path crafts Plank, B01 ShopSlot stocks it, and the existing NpcController visits after explicit night opening and purchases through EconomyService.
- The real SaveManager used an Editor-only isolated LocalJsonSaveRepository during validation. Seed 9009, resource consumption, Fish inventory, Day 3 clock, the post-sale empty slot and revenue restored after deliberate restart-state mutation; schema remains v11.
- `Logs/WORLD009_Validation_DontSaveFix.log` passed D3D11 Edit/Play, sixteen navigation sectors, the full loop, save/restart/restore and blocking Console 0.
- WORLD-001/007/008, CraftingRecipeCard, CustomerArrival and FinalDemoRoute regressions passed. WORLD-001 now distinguishes the authored 16x16 fixture from the approved generated 128x128 runtime authority.
- Scene/Prefab/Packages/ProjectSettings/SaveData schema remain unchanged; new crash count is zero and capture remains `CAPTURE_EVIDENCE_DEBT`.
- Final state: `WORLD_009_COMPLETE`. Create the approved local ticket commit, then activate preapproved `WORLD-010 M70 Integration and Regression`.

## 2026-08-11 — WORLD-010 M70 Integration and Regression

- Baseline: `milestone/world-alpha-70@46f6515`; target: `M70_PLAYABLE_WORLD_ALPHA_COMPLETE`.
- Runtime-only alpha controller connected existing movement/camera to the provisional 128×128 generated island and projected all 64 chunks without per-cell GameObjects.
- Integrated player-safe traversal, gather/craft/terraform, B09 place/move, functional B01 display move/rotate, stock/open/customer/revenue and isolated save/restart/load.
- Primary proof: `Logs/WORLD010_M70_Validation_02.log` — Play Mode exit/re-enter restore, v11 checksum, 64 chunks, blocking Console 0.
- Golden PASS: PrototypeFirstDay, DayNightShopLoop, CoreSlicePlayability, FinalDemoRoute, LongPlayProgression, SaveRoundTrip, CraftingRecipeCard, CustomerArrival and CustomerPresentation.
- Bounded regressions PASS: WORLD-006B, WORLD-007, WORLD-008 and WORLD-009.
- Runtime/Editor compile PASS; no new crash. Scene/Prefab/Packages/ProjectSettings/SaveData schema unchanged.
- Automatic continuation stops at M70. Next action requires a new bounded human-approved ticket; MainGame integration remains separately gated.

## 2026-08-12 — BETA-003 explicit validator retry approval

- Human explicitly released the BETA-003 validator-stage and third isolated D3D11 execution blockers.
- Preserved `milestone/gameplay-beta-85@23a881a` and the three approved dirty BETA-003 paths; no unexpected user change was present.
- Both prior D3D11 logs passed B05/B06/B07 binding and Kitchen card visibility, then counted five Forge cards because `CraftingUI` defers destruction of the three prior Kitchen cards until the end of the frame.
- Approved minimal correction: split each UI context transition into `open -> next validator stage/frame -> inspect`, without removing assertions or changing CraftingService, Inventory, RecipeData, ItemInstance, scenes, prefabs, Packages, ProjectSettings, or save schema.
- Exactly one third isolated D3D11 BETA-003 run is authorized. A repeated stage failure or native crash stops the ticket without a fourth automatic retry.

## 2026-08-12 — BETA-003 Crafting and Production Expansion

- Split Kitchen, Forge and Basic UI validation into open/context-frame/inspect stages while retaining every card, layout, viewport, shortage and transaction assertion.
- The human-approved third isolated run `Logs/BETA003_D3D11_Validation_ThirdApproved.log` passed D3D11, three facilities, recipe cards 3/2/2, five actual crafts, ItemInstance quality/base-price metadata, raw 118G to processed 203G, B01 stock/sale 28G, scene clean and Console 0.
- `BETA003_Regression_CraftingRecipeCard.log`, `BETA003_Regression_ProcessingChain.log` and `BETA003_Regression_BETA002.log` passed the required Basic/Wood-to-Plank, processing chain and daytime-resource-to-Inventory regressions.
- Runtime/Editor compile passed with zero errors and pre-existing CS8785/CS0414 warnings only. No new crash or Scene/Prefab/Packages/ProjectSettings/Save schema change occurred.
- Final state: `BETA_003_COMPLETE`. Create the approved local ticket commit and activate preapproved `BETA-004 Shop Readability and Merchandising`.

## 2026-08-12 — BETA-004 Shop Readability and Merchandising

- Kept the existing B01 `Shop`/four `ShopSlot` hierarchy, movable-display pose and v11 furniture projection as the only authorities; no parallel inventory, pricing, shop or placement system was added.
- Added a runtime-only read model attached to the same movable display root. One shop-zone header identifies `밤 영업 구역 · 상품 판매대` and four slot labels expose empty, item/count/price/quality and sold-out/next-stocking states.
- Added the same live merchandising summary to the player HUD and pointed the processed-product objective at the actual movable display target.
- `Logs/BETA004_D3D11_Validation_Final.log` passed B01=4, labels=5, empty/stocked/sold-out states, Bread 1 at 43G/125%, price refresh, bounded move/270-degree rotation, atomic rejection, v11 pose projection, moved-display customer sale and Console 0.
- `Logs/BETA004_Regression_BETA003.log` preserved five recipes, quality/UI and the 28G processed sale. `Logs/BETA004_Regression_WORLD006B.log` preserved movable furniture, customer route, protected route, exact cancel rollback and save projection.
- Runtime/Editor compile passed with zero errors. No new crash or Scene/Prefab/Packages/ProjectSettings/Save schema change occurred. Capture remains `CAPTURE_EVIDENCE_DEBT` for the M85 integrated playtest.
- Final state: `BETA_004_COMPLETE`. Create the approved local ticket commit and activate preapproved `BETA-005 Customer Strategy and Feedback`.

## 2026-08-12 — BETA-005 Customer Strategy and Feedback — HARD BLOCKER

- Baseline: branch `milestone/gameplay-beta-85`, commit `9d2a81e`. BETA-004 작업과 clean baseline을 보존했다.
- WorldSandbox runtime 고객이 profile null 평균형이던 결손을 확인했다. 기존 `Resources/NPCs/Profile_Miner`와 `Profile_Tailor`를 방문 순서대로 배정하고, 실제 성향/구매/피드백/수요/판매 통계 권위를 그대로 연결했다.
- 기존 adapter가 구매만 완료로 보던 문제를 보정해 실제 SalesLog 거절도 정상 방문 완료로 처리하며 재고/돈은 유지한다. HUD와 고객 월드 라벨은 실제 profile 성향과 구매/보류 결과를 표시한다.
- Runtime/Editor compile 오류 0. D3D11 두 실행 모두 Miner 250G 평가와 Day 2 보류 기록까지 진행했지만 validator의 공통 frame skip이 non-idle preference-panel 관찰 구간을 놓쳐 같은 assertion이 반복 실패했다.
- 동일 원인 2회 실패 규칙에 따라 세 번째 실행, 추가 수정, commit, BETA-006 시작을 중단했다. native crash와 금지 경로 변경은 0이다. 상태: `BETA_005_HARD_BLOCKER`.

## 2026-08-13 — BETA-005 human-approved resume

- 사람이 BETA-005 stage 1/2 관찰 순서 최소 수정과 추가 D3D11 실행 1회를 명시적으로 승인했다. 통과 뒤 BETA-006~010 자동 진행도 2026-08-14 09:00 KST까지 승인됐다.
- 시작 상태는 `milestone/gameplay-beta-85@9d2a81e`, staged 0, 승인된 BETA-005 미커밋 경로 13개, Unity 프로세스 0, 신규 crash 0으로 이전 중단점과 일치한다.
- 실제 원인은 stage마다 frame counter를 reset한 뒤 validator 전체가 3 update를 건너뛴 것이었다. 실제 Miner는 그 사이 이동·평가·거절을 완료해 production 동작과 SalesLog는 정상인데 non-idle preference panel 표본만 놓쳤다.
- 수정 범위는 bootstrap stage 0만 초기 frame 대기를 유지하고 stage 1/2는 첫 update부터 실제 UI를 관찰하는 orchestration 변경이다. assertion과 PurchaseEvaluator 권위는 유지한다.

## 2026-08-13 — BETA-005 approved additional validation — HARD BLOCKER

- Runtime/Editor compile은 각각 오류 0으로 PASS했다. 기존 CS8785 및 Editor CS0414 경고만 유지됐다.
- `Logs/BETA005_D3D11_Validation_ApprovedResume.log`에서 D3D11, Miner/Tailor 실 profile 대비, 플레이어용 성향 힌트, 기존 `PurchaseEvaluator`의 Processed/Luxury 카테고리 반응, 밤 영업 gate, Miner 방문 시작과 월드/HUD 전략 표시는 PASS했다.
- 실제 Miner는 Plank 250G를 `p=0.00`으로 보류했고 Day 2 SalesLog에 보류 1건이 기록됐다. 그러나 첫 stage 1 Editor callback 전에 짧은 실제 방문이 완료되어 `the visible Miner preference remains readable during the actual visit`가 같은 지점에서 다시 실패했다.
- 승인된 추가 실행 1회는 소비됐다. native crash와 Scene/Prefab/Packages/ProjectSettings/Save schema 변경은 없고, 자동 캡처와 회귀 검사는 주 validator PASS 전제에 도달하지 않아 수행하지 않았다.
- 최종 상태는 `HARD_BLOCKER_BETA_005_VALIDATION`이다. 네 번째 실행, 추가 validator 약화, commit, BETA-006 시작 없이 중단한다.

## 2026-08-20 — BETA-005 synchronous live-evidence approval

- 사람이 기존 13개 BETA-005 변경 보존, `TryBeginCustomerVisit` 성공 직후 실제 플레이어용 WorldSandbox HUD 동기 표본, 격리 D3D11 실행 1회와 PASS 또는 명시적 `BETA005_LIVE_PREFERENCE_EVIDENCE_DEBT` 처리, 이후 BETA-006~010 자동 진행을 승인했다.
- 이전 `HARD_BLOCKER_BETA_005_VALIDATION`은 해제했고 티켓을 `BETA_005_ACTIVE`로 재개했다. 기존 세 번의 실패 로그와 변경은 삭제하거나 재생성하지 않는다.
- `CustomerPreferenceCanvas`는 기본 player mode에서 숨겨지는 개발 오버레이이며, 실제 BETA 플레이어 표면은 `BeginNewGame()` 이후의 WorldSandbox 상단 HUD이다. 새 검증은 그 라이브 getter, 현재 customer/profile, 화면 경계와 활성 상태를 같은 stage에서 읽고, 숨겨진 Canvas 상태도 진단값으로 별도 기록한다.

## 2026-08-20 — BETA-005 Unity license launch blocker

- 동기 표본 수정 뒤 Runtime/Editor `dotnet build`는 오류 0으로 PASS했다. 기존 CS8785/CS0414 경고만 유지됐다.
- 승인된 실행은 `Logs/BETA005_D3D11_Validation_SynchronousPreference.log`에 명령행을 남겼지만 Unity Licensing이 유효한 Editor 라이선스를 찾지 못해 return code 198로 종료했다. `[BETA-005]` 0건, graphics/D3D11 초기화 0건으로 project load·executeMethod·Play Mode·validator는 시작되지 않았다.
- 같은 실행을 자동 재시도하지 않았다. Unity 프로세스 0, crash report 3개로 기존과 동일하고, dirty/staged/untracked는 승인된 13/0/0을 유지한다.
- 상태는 `HARD_BLOCKER_BETA_005_UNITY_LICENSE`다. 사용자가 Unity Hub/Editor 라이선스를 복구한 뒤 현재 컴파일된 validator 지점에서 재개해야 하며 BETA-006과 local commit은 시작하지 않는다.

## 2026-08-20 — Unity Personal license recovered; BETA-005 resumed

- Unity Hub와 Licensing Client가 재실행됐고 token/license 파일이 23:18 KST에 갱신됐다. Hub/Client 로그는 Unity Personal activation `statusCode=200`, `LicenseUpdate Added`, Personal EULA `Agreed`를 기록했다.
- 이전 return code 198 launch는 project load·graphics·executeMethod 이전에 끝났으므로 승인된 기능 validation은 미소비 상태로 정합했다. 기존 13개 변경을 보존하고 `BETA_005_ACTIVE`에서 compile과 실제 D3D11 validator를 재개한다.

## 2026-08-20 — BETA-005 Customer Strategy and Feedback COMPLETE

- `Logs/BETA005_D3D11_Validation_SynchronousPreference_Licensed_Correction.log`가 실제 player session, Miner/Tailor profile 대비, current customer 활성, live HUD 문구·화면 경계, Miner 보류와 Tailor 구매를 D3D11에서 PASS했다.
- Miner 보류는 재고·돈을 바꾸지 않고 SalesLog에 정확히 1건 남았다. 기존 feedback은 Miner를, 실제 demand summary는 `Processed: 0/1 bought`를, player HUD는 `보류`를 설명했다. Tailor 구매는 1G를 입금했다.
- Day 3 이전 잠금 안내인 demand-insight 표시 문자열을 고객 식별 근거로 묶었던 validator를 실제 feedback/demand/HUD 권위의 직접 assertion으로 분리했다. assertion 삭제·경고화·production 우회는 없다.
- BETA-004 movable shop/readability, CustomerPresentation, CustomerArrival 회귀가 모두 PASS했다. Runtime/Editor compile 오류 0, blocking Console 0, 신규 crash 0, Scene/Prefab/Packages/ProjectSettings/Save schema content diff 0이다.
- 자동 캡처는 `CAPTURE_EVIDENCE_DEBT`다. 최종 상태 `BETA_005_COMPLETE`; 승인된 local ticket commit 뒤 선승인 `BETA-006 Phone Hiring and Feed Completion`을 활성화한다.
