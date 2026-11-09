# Project P.A. — CODE REUSE MAP v1

**Freeze basis:** `milestone/gameplay-beta-85 @ 4374b2aa27e73c2c7fec086eac6c4130eb702306` + local P4 dirty diffs supplied 2026-09-11  
**Purpose:** prevent duplicate systems and constrain Codex/Astra to reuse-first implementation.  
**Status:** design/reuse freeze. P4 dirty itself is **not** a PASS checkpoint and must not be committed wholesale.

---

## 0. Decision vocabulary

- **KEEP** — current authority remains the canonical implementation. Do not replace.
- **EXTEND** — preserve authority/API and add narrowly scoped capability.
- **MIGRATE** — useful intent/logic exists, but move it to a more appropriate abstraction before it becomes long-term authority.
- **DEPRECATE** — temporary vertical-slice code or coupling; may remain until replacement is proven, but do not build new features on it.
- **NEW** — no suitable current authority exists; new code is allowed only here.

Priority rule:

`KEEP → EXTEND → ADAPT/WRAP → MIGRATE → NEW`

Foundational rewrites require explicit human approval.

---

# 1. Global authority locks

| Area | Canonical authority | Decision | Rule |
|---|---|---|---|
| Player movement | `Assets/Scripts/PlayerController.cs` | KEEP | No second controller / no locomotion rewrite |
| Camera follow | `Assets/Scripts/CameraController.cs` | KEEP + TUNE | Human-feel tuning only; build mode stays same authority |
| Interaction | `Assets/Scripts/PlayerInteraction.cs`, `IInteractable` | KEEP + EXTEND | New interactions implement/route through this contract |
| Inventory | `Inventory`, `InventorySlot`, `ItemInstance`, `Hotbar` | KEEP | No second inventory/hotbar |
| Economy wallet | `EconomyService` | KEEP | All money mutation through this authority |
| Shop transaction | `ShopSlot` + `PurchaseEvaluator` + `SalesLogManager` | KEEP + EXTEND | No ShopManager2/Economy2 |
| Production core | `ProducerNpcController` + `ProductionData` | KEEP + EXTEND | Production FSM is canonical; P4 tutorial exceptions must not fork it |
| NPC identity | `NpcProfile` | KEEP | Do not replace MBTI/behavior data model |
| NPC schedule | `NpcDailySchedule` / `NpcScheduleController` | KEEP + EXTEND | Home/work/shop anchors extend existing schedule |
| Hiring | `HiringService` + `NpcCandidateData` | KEEP + ADAPT | Progression Graph gates candidates before existing hire path |
| Macro rank | `TierService` | KEEP, SCOPE REDUCE | Settlement/P.A. rank only; not every unlock |
| World cells | `WorldGridService` | FOUNDATIONAL KEEP | No replacement grid |
| Terrain/chunks | `WorldChunkTerrain` | FOUNDATIONAL KEEP + SCALE | Add activation/streaming, not replacement |
| World generation | `WorldIslandGenerator` | EXTEND → Gen V2 | Same deterministic generation authority |
| Navigation | `WorldNavigationService` | KEEP + SCALE | Active-sector strategy; no parallel Nav authority |
| Building placement | `WorldBuildingPlacementService` | FOUNDATIONAL KEEP | All world building uses this |
| World persistence | `WorldPersistenceService` | FOUNDATIONAL KEEP | Seed + sparse delta stays canonical |
| Save schema/IO | `SaveData` + `SaveManager` + repository | FOUNDATIONAL KEEP + ADDITIVE MIGRATION | No SaveManager2 |
| Village response presentation | `VillageCultureVisualController` | KEEP + GENERALIZE | Becomes observer/presentation, not causal economy authority |

---

# 2. P4 dirty audit — file-level freeze

## 2.1 `Assets/Scripts/ProducerNpcController.cs`

**File decision: KEEP authority. Dirty P4 additions: MIGRATE OUT before finalization.**

The existing FSM, timer, inventory, production math and paid delivery are canonical. The dirty patch adds tutorial-specific state directly to the domain controller. Preserve the behavior intent, but do not freeze those fields as the permanent API.

### Symbol decisions

| Symbol / hunk | Decision | Final destination / rule |
|---|---|---|
| Existing `State` FSM | KEEP | Canonical producer lifecycle |
| Existing `_npcInventory` | KEEP | Actual producer stock authority |
| Existing `BeginWork()` | KEEP | Do not duplicate |
| Existing `ProduceItems()` | KEEP | Do not invoke via tutorial shortcuts |
| Existing paid delivery/procurement path | KEEP | Normal P6+ path |
| `_starterSession` | MIGRATE | Generic procurement/session policy, not producer core |
| `_starterClaimed` | MIGRATE | Opening support grant state / procurement state |
| `_starterReady` | MIGRATE | Derive from producer stock + session policy |
| `StarterBatchReady` | MIGRATE | Expose through adapter/policy |
| `StarterClaimed` | MIGRATE | Opening progression state |
| `StarterStockCount` | EXTEND concept | Prefer generic read-only `StockCount` API if missing |
| `StarterWorking` | MIGRATE | Derive from canonical FSM + opening session |
| `ConfigureStarterSession()` | DEPRECATE AS-IS | It clears inventory and mutates schedule/FSM; too destructive for generic producer |
| `StartStarterWork()` | MIGRATE | Opening flow requests canonical work via adapter |
| `TryClaimStarterBatch()` | MIGRATE | `ProcurementPolicy.SettlementSupport` transaction |
| `CaptureStarterState()` | MIGRATE | Opening/procurement save payload, not Producer core |
| `RestoreStarterState()` | MIGRATE | Restore through adapter/policy |
| `Update()` starter stop guard | MIGRATE | Session policy controls delivery/continuation |
| `FinishProduction()` early return for starter | MIGRATE | Generic delivery mode/event after a batch is produced |
| `Pause()` starter early-return change | **REJECT / REVERT** | Violates Pause semantics; `Pause()` must actually pause regardless of tutorial state |
| `CalculateProductionInterval()` 12–20s clamp | **REJECT / REVERT** | Tutorial pacing belongs in data/session multiplier, not production formula |

### Required replacement contract

**NEW:** `ProcurementPolicy` (name may vary, concept frozen)

Minimum modes:

- `SettlementSupport` — first opening batch transferred for 0G exactly once.
- `PaidDirectPurchase` — existing Producer paid handoff.
- `Contract` — later fixed amount/price/period.
- `DelegatedProcurement` — later automatic budgeted purchase.

The policy may observe producer stock, but it must not reimplement production.

---

## 2.2 `Assets/Scripts/Presentation/FirstProductionController.cs`

**File decision: MIGRATE / KEEP AS OPENING ORCHESTRATOR ONLY.**

Useful responsibilities:

- bind exactly the two selected companions;
- expose P4 objective;
- request starter worksite placement;
- observe player activity;
- observe first producer batch;
- mark P4 completion;
- capture/restore *opening progression*.

Forbidden responsibilities in final form:

- owning production math;
- hard-coding long-term professions/products;
- becoming building authority;
- becoming inventory/economy authority;
- being required by `SaveManager` for unrelated saves.

### Symbol decisions

| Symbol | Decision | Note |
|---|---|---|
| `IsReady` | KEEP concept | Opening stage readiness |
| `Complete` | KEEP concept | Completion predicate |
| `Objective` | KEEP concept | Later route through shared opening objective UI |
| `Settlement` | KEEP temporarily | Replace hard P3 coupling when Settlement v2 is introduced |
| `Sites` | KEEP concept | Runtime bindings, not save authority |
| `Owners` | KEEP | Must still be the exact two confirmed P1 IDs |
| `Role(string id)` | **DEPRECATE** | Hard-coded 3-ID mapping blocks 5-producer/future graph |
| `WorksiteId(owner)` | KEEP concept | Stable ID rule is useful |
| `OwnsWorksite(id)` | KEEP concept | Prefer generic registered-placement query later |
| `Definition(owner)` | **MIGRATE** | Worksite definition belongs in data assets |
| `Update()` flow | MIGRATE | Event-driven or bounded stage observer preferred; no domain duplication |
| `Profession(owner)` | KEEP display helper | Data-driven later |
| `Initialize()` | MIGRATE | Keep stage initialization; move hard-built UI to UI layer |
| `Attach()` | MIGRATE | Replace with generic `WorksiteBinding` |
| `CaptureState()` | MIGRATE | Keep opening-state intent; schema changes below |
| `IsValidSave()` | MIGRATE | Validate explicit started-state + real grid bounds |
| `ClearWorksitesForRestore()` | MIGRATE | Keep rollback intent; do not depend on broken tutorial Pause semantics |
| `RestoreState()` | MIGRATE | Restore via placement + worksite binding |

### Hard-coded values to remove before final world

- `Lumberjack_01 / Miner_01 / Farmer_01` mapping
- `Wood / Ore / Wheat` switch logic
- `anchorX/Z < 16`
- fixed `PA_SettlementUI` positions
- “exactly 2 worksites” should remain only in opening P4, not generic industry code.

---

## 2.3 `Assets/Scripts/Presentation/StarterWorksiteInteraction.cs`

**File decision: DEPRECATE AS FINAL AUTHORITY; MIGRATE useful pieces into generic worksite binding + opening tutorial adapter.**

### Keep/migrate

| Part | Decision |
|---|---|
| `IInteractable` entry | KEEP contract |
| Owner → producer association | MIGRATE → `WorksiteBinding` |
| Reuse of `FarmPlotInteraction` | KEEP/reuse |
| Reuse of `MiningSpot` | KEEP/reuse |
| Reuse of `Gatherable` | KEEP/reuse concept |
| First activity → producer starts working | KEEP opening design, migrate orchestration |
| First batch visual | KEEP presentation concept |
| Status prompt | KEEP UX concept |
| One-time support claim | MIGRATE → ProcurementPolicy |

### Reject as long-term behavior

- `if (Producer==null) AddComponent<ProducerNpcController>()`
  - final companion prefabs/data must own their producer capability deterministically.
- Runtime mutation of `productionData`, `specialty`, `workSpot`, `dropOffPoint` as the long-term binding mechanism.
- Wheat-specific free seed grant embedded in worksite interaction.
- `growthSecondsPerStage = 3` embedded in runtime code.
- Ore activity ID and resource path hard-coded in code.
- Wood interaction creates a temporary `Gatherable` with `ToolType.None` on each interaction.
- Hand-authored sine-wave tool animation as domain logic.
- `_flow` currently carries no useful authority and should not survive without a reason.

**NEW:** generic `WorksiteBinding` / `ProducerAssignment` component or service.

Minimum data:

- stable worksite instance ID
- producer/resident ID
- production profile/data ID
- interaction/activity profile ID
- work anchor
- drop-off/procurement anchor
- home anchor reference (resident side)
- enabled/assigned state

---

## 2.4 `Assets/Scripts/Presentation/FirstIslandSettlementController.cs` P4 hunks

**Base file: KEEP for current P3. P4 dirty coupling: MIGRATE.**

| Dirty change | Decision | Reason |
|---|---|---|
| Read `FirstProductionController.Objective` in P3 `Update()` | MIGRATE | Objective handoff should be opening-stage coordinator/UI, not P3 knowing P4 |
| `Begin()` accepts P4 worksite IDs via `GetComponent<FirstProductionController>()` | MIGRATE | P3 should not own P4 placement authorization |
| Shelter destination suppressed while `Producer.StarterWorking` | MIGRATE OUT | Work/home commute belongs NPC schedule/work state |

Final Settlement v2 should expose generic placement/UI methods usable by P3 and P4 without P3 importing P4-specific types.

---

## 2.5 `Assets/Resources/DepartureTutorial/DepartureContinuation.prefab`

**Decision: KEEP hook, CLEAN DIFF.**

- Adding `FirstProductionController` to the opening continuation prefab is valid for the current opening architecture.
- Whitespace-only `m_Name:` → `m_Name: ` changes are noise and should not be intentionally committed.
- When Settlement v2 is built, the component may remain as the P4 stage orchestrator if its responsibilities are narrowed as above.

---

## 2.6 `Assets/Scripts/SaveData.cs` P4 dirty

**Decision: MIGRATE. Do not commit current v16 contract unchanged.**

Current dirty schema:

- `FirstProductionSaveData`
- `StarterWorksiteSaveData`
- `StarterProducerSaveData`

Useful information exists, but the current model is vertical-slice-specific and relies on nullable optional state that has already failed a real round trip.

### Frozen immediate rule

Do **not** represent “P4 not started” solely by `firstProduction == null`.

If the temporary opening save remains, use an explicit discriminator:

```text
FirstProductionSaveData
- version
- started
- completed
- worksites[]
```

Valid states:

1. `started=false` → `worksites` must be empty; valid.
2. `started=true, completed=false` → 2 owner records; placements may be absent/present according to progress.
3. `started=true, completed=true` → both selected opening producers have consumed their one-time support grant state.

Long term, producer stock should migrate toward the canonical producer/world state instead of being indefinitely duplicated in opening-specific `StarterProducerSaveData`.

---

## 2.7 `Assets/Scripts/SaveManager.cs` P4 dirty

**Base file: FOUNDATIONAL KEEP. Dirty v16 patch: BLOCKED / MIGRATE.**

### Keep concepts

- additive version migration;
- capture P4 opening state when present;
- validate before applying;
- rollback/live-state preservation on invalid save;
- restore order: selection/settlement → worksites → producer/opening state → final player pose.

### Reject/fix before version bump

1. **Current v16 is not shippable/committable as a PASS schema.**
   - A P4-not-started/null payload was observed returning as an empty `firstProduction` object and rejected by `IsValidSave`.
2. `SaveManager` should not permanently depend on presentation-specific validation semantics for every save.
3. Migration must explicitly define “not started”.
4. P4 validation must use current world/grid dimensions, not `16`.
5. No existing P3 v15 save may invent a free batch or P4 completion.

### Version rule

Keep `CurrentSaveVersion = 15` on the approved baseline until the corrected P4 save contract passes:
- v15 migration,
- save-before-P4,
- save-mid-worksite,
- save-mid-production,
- save-batch-ready,
- save-after-claim,
- fresh reentry,
- no duplicate support claim.

Only then promote the next schema version.

---

# 3. P4 asset/tooling freeze

## `Assets/Editor/PA_FirstProductionSetup.cs`

**Decision: KEEP TOOLING, ADAPT PATHS/DATA.**

Keep:
- deterministic wrapper generation;
- provenance-based derived FBX usage;
- material remap;
- collider/interaction-anchor generation;
- bounds checks;
- BuildingData creation;
- tool visual wrapper generation.

Migrate:
- final assets out of `DepartureTutorial/Worksites` when they become general content;
- worksite footprint/entrance/production role into data, not hard-coded role switch;
- starter worksite can remain Tier-0 art even if later workshops are larger.

## `Assets/Editor/PA_FirstProductionChecks.cs`

**Decision: KEEP TEST INTENT, REVISE TEST IMPLEMENTATION.**

High-value assertions to preserve:
- all 3 current pair combinations;
- same selected companion objects survive P1→P4;
- 2 worksites, no duplicate placement;
- natural obstacle rejection;
- direct player activity uses real existing activity components;
- real producer timers, no timer injection;
- one-time free support claim;
- 0G support grant;
- save/load at in-flight crop and batch-ready states;
- fresh reentry;
- repeat claim rejected;
- no duplicate companions/structures;
- v15 migration does not invent P4 state.

Revise before PASS:
- do not write/claim P4 PASS until all assertions finish;
- replace 16×16 search assumptions for WorldGen V2;
- add explicit `not started` save roundtrip;
- add inventory-stack regression for Farmer path;
- add full-inventory claim failure test (must preserve producer stock);
- add save-after-failed-claim test;
- add same-session reload and fresh-scene reload separately.

---

# 4. P4 art/resource decisions

| Asset group | Decision | Rule |
|---|---|---|
| `Assets/Art/ProjectPA/Derived/Worksites/*` | KEEP | Tier-0 starter worksite art, assuming existing provenance remains valid |
| `Assets/Resources/DepartureTutorial/Worksites/PA_StarterWorksite_*` | KEEP NOW / MIGRATE PATH LATER | General content path when P4 leaves tutorial-only status |
| `Tool_Wood/Ore/Wheat` | KEEP presentation asset | Animation control should move out of worksite domain logic |
| `Blender/Scripts/build_worksite_assets.py` | KEEP TOOLING | Reproducible derived-art pipeline |
| `Docs/AssetProvenance/VS_PRESENT_001/P4/*` | KEEP EVIDENCE | Evidence, not gameplay authority |
| `Assets/_Recovery/*` | DO NOT TOUCH | Recovery only; never reuse as canonical |

---

# 5. P4 failure diagnosis / blocker freeze

Observed baseline facts:

- P4 work reached at least real producer first-batch/claim behavior before being stopped.
- Opening-feel validation intentionally excluded P4.
- The local v16 save path has a reproducible optional-`firstProduction` representation/validation problem.
- Therefore P4 dirty is **useful implementation material but not a PASS checkpoint**.

### Current code risks found in the diff

1. `Pause()` is weakened by a starter-state early return.
   - This can make restore cleanup semantically unreliable.
   - **Action: revert this hunk.**

2. `IsValidSave()` hard-codes a 16×16 world.
   - **Action: migrate to actual grid/world definition.**

3. Optional P4 state relies on null surviving serialization/deserialization.
   - **Action: explicit `started` discriminator.**

4. Tutorial timing is injected into the producer's production formula.
   - **Action: move to opening session/data override.**

5. Starter support is embedded inside `ProducerNpcController`.
   - **Action: move transaction/grant semantics to `ProcurementPolicy`.**

6. Worksite behavior hard-codes 3 producer IDs/roles.
   - **Action: migrate to data-driven companion/industry profile before 5-producer expansion.**

7. P3 controller knows P4 controller details.
   - **Action: remove cross-stage knowledge when Settlement v2 is introduced.**

---

# 6. Required NEW abstractions — allowed list

Only the following new domain abstractions are currently justified.

## 6.1 `ProcurementPolicy`
Owns how producer stock transfers economically.

Required modes:
- `SettlementSupport`
- `PaidDirectPurchase`
- `Contract`
- `DelegatedProcurement`

Must call existing `Inventory` and `EconomyService`; must not replace them.

## 6.2 `WorksiteBinding` / `ProducerAssignment`
Connects a placed worksite to a resident/producer and anchors.

Must use:
- `WorldBuildingPlacementService`
- `ProducerNpcController`
- existing NPC/Nav/schedule stack

## 6.3 `ProgressionGraphService`
Future technology/industry/resident/facility/logistics/commerce unlock graph.

Must gate existing authorities, not reimplement them.

## 6.4 `ResidentConsumptionService`
Turns actual sales into resident-owned/used outcomes.

Must observe:
- Shop/SalesLog
- NPC profile/life state
- VillageCultureVisualController as presentation observer

## 6.5 WorldGen V2 profile / active-sector layer
Extends existing world generator/chunk/navigation stack for final island scale.

---

# 7. Forbidden NEW systems

Until explicitly approved, Codex/Astra must not create:

- `PlayerController2`
- a second camera authority
- `InventoryManager2`
- `EconomyManager2`
- `ShopManager2`
- a new producer FSM replacing `ProducerNpcController`
- a second world/grid service
- a second building-placement system
- a second save manager/schema file
- a parallel hiring system
- an opening-only item database
- a P4-only currency
- duplicated gathering/mining/farming implementations

---

# 8. P4 salvage plan

## P4-R1 — isolate salvage, no gameplay expansion
Goal: recover reusable P4 code without claiming PASS.

1. Preserve dirty diff/archive.
2. Revert/rework only rejected core contaminations:
   - `ProducerNpcController.Pause()` starter early return
   - 12–20s interval clamp
3. Introduce/locate `ProcurementPolicy.SettlementSupport`.
4. Make starter support use actual producer stock.
5. Do not change Inventory/Economy authority.

## P4-R2 — data-driven worksite binding
1. Replace `Role(id)` switch with data mapping.
2. Replace runtime producer component creation with deterministic producer configuration/binding.
3. Keep existing farm/mining/gathering activity components.
4. Preserve Tier-0 worksite art.

## P4-R3 — save contract correction
1. Explicit `started`.
2. Keep v15 approved baseline until corrected schema proves itself.
3. Test:
   - before P4,
   - after P4 unlock/no worksite,
   - one worksite,
   - activity complete,
   - producer working,
   - batch ready,
   - failed inventory claim,
   - claimed,
   - complete,
   - same-session load,
   - fresh load.
4. Zero duplicate grants.

## P4-R4 — validator recovery
Run targeted P4 checks only.
Do not repeat full P0→P3 regressions unless P4 touched a foundational authority whose contract changed.

---

# 9. Settlement v2 compatibility

Current P3 remains a proved checkpoint, but its presentation flow is not the final settlement design.

Migration target:

`Arrival → Supply Crate → Phone/Tools/Tent Kits → Tent ×3 → direct gather → Management Hub → starter worksites → P4`

Reuse:
- existing P1 selected IDs
- existing P2 voyage presentation
- existing `WorldBuildingPlacementService`
- P3 save/placement concepts
- existing companion objects
- P4 worksite art
- existing Producer FSM
- existing Inventory/activities

Do not preserve as final:
- Hub-before-shelter ordering
- three-free-buildings being the full settlement
- 16×16 bounds
- P3 owning P4 placement authorization
- P4 hard-coded 3-role switch.

---

# 10. Codex/Astra implementation constitution for P4

For every P4 ticket:

1. Read current `00_CURRENT` authority docs.
2. Confirm HEAD is the expected checkpoint.
3. Inspect actual authority file before adding code.
4. Reuse before new implementation.
5. Touch at most the ticket-approved files.
6. Do not modify save schema unless the ticket is explicitly a save ticket.
7. No full-regression repetition when existing P0–P3 evidence is unaffected.
8. Targeted Unity Play runs: default max 2.
9. Full opening route: max 1 only after targeted PASS.
10. If the same blocker recurs twice, STOP and hand off evidence.
11. No commit of P4 until:
    - targeted functional PASS,
    - corrected save roundtrip PASS,
    - fresh reentry PASS,
    - zero duplicate starter grants,
    - runtime errors 0.
12. Commit and STOP; do not begin the next milestone automatically.

---

# 11. Final frozen decisions

### KEEP
- Existing Producer FSM/timer/inventory/paid delivery
- Existing world, placement, inventory, shop, economy, save authorities
- P4 worksite derived art/tooling
- direct activity reuse principle
- selected companion continuity
- one-time settlement support concept
- P4 validator intent

### EXTEND
- Generic producer stock/read API if needed
- production worksite data
- worksite-to-producer binding
- existing UI/phone later
- existing placement/data catalogs

### MIGRATE
- starter session state out of Producer core
- first-free handoff into ProcurementPolicy
- worksite binding out of StarterWorksiteInteraction special cases
- P3→P4 direct controller coupling
- P4 save payload to explicit started-state
- role mapping to data
- hard-coded 16-cell validation to real world bounds

### DEPRECATE
- `Role(id)` switch
- runtime `AddComponent<ProducerNpcController>()`
- free-seed logic inside generic interaction
- temporary wood Gatherable spawning with no tool requirement
- tutorial timing clamp in production formula
- P4-specific knowledge inside P3
- tutorial-only screen coordinates as final UI

### NEW
- `ProcurementPolicy`
- generic `WorksiteBinding/ProducerAssignment`
- `ProgressionGraphService`
- `ResidentConsumptionService`
- WorldGen V2 profile/active-sector scaling layer

---

**Freeze statement:**  
Any future implementation that contradicts this map must stop and obtain an explicit design decision before rewriting a KEEP authority or promoting a DEPRECATE/MIGRATE element into permanent architecture.
