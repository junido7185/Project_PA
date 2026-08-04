# Project PA Context Index

Use this file first. Pick the smallest context set needed for the task.

## Always Read For Gameplay-Related Work

- `PROJECT_PA_CREATIVE_NORTH_STAR.md`
- `PROJECT_PA_DESIGN_INTENT.md`
- `PROJECT_PA_CURRENT_MILESTONE.md`
- `PROJECT_PA_STATUS.md`
- `PROJECT_PA_TODO.md`
- `PROJECT_PA_SESSION_REPORT.md`

## Gameplay Sprint

Required:

- `PROJECT_PA_CREATIVE_NORTH_STAR.md`
- `PROJECT_PA_DESIGN_INTENT.md`
- `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md`
- `PROJECT_PA_CURRENT_MILESTONE.md`
- `PROJECT_PA_FULL_GAME_BACKLOG.md`
- `PROJECT_PA_CORE_SLICE_PLAN.md`

Validators:

- `PA_FinalDemoRouteValidator.RunFinalDemoRouteValidation`
- `PA_DayNightShopLoopValidator.RunDayNightShopLoopValidation`
- `PA_LongPlayProgressionValidator.RunLongPlayProgressionValidation`

Human check: required for route feel, readability, and scene comprehension.

Forbidden changes: Project_D, broad core rewrites, external packages, destructive Git.

## UI / UX Sprint

Required:

- `PROJECT_PA_CREATIVE_NORTH_STAR.md`
- `PROJECT_PA_DESIGN_INTENT.md`
- `PROJECT_PA_CORE_SLICE_PLAN.md`
- `PROJECT_PA_STATUS.md`
- `Docs/08_아트_및_씬_구성_가이드.md`

Optional:

- `Docs/CustomerPresentation/README.md`
- `Docs/VisualTargets/VISUAL_TARGETS.md`

Validators:

- `PA_CustomerPanelLayoutValidator.RunCustomerPanelLayoutValidation`
- `PA_CoreSlicePlayabilityValidator.RunCoreSlicePlayabilityValidation`
- `PA_FinalPresentationReviewer.RunFinalPresentationReview`

Human check: required for 1920x1080 readability and Korean font rendering.

Forbidden changes: UI behavior rewrites, copied reference layouts, Project_D assets.

## Art / Visual Sprint

Required:

- `PROJECT_PA_CREATIVE_NORTH_STAR.md`
- `PROJECT_PA_DESIGN_INTENT.md`
- `Docs/08_아트_및_씬_구성_가이드.md`
- `Docs/VisualTargets/VISUAL_TARGETS.md`

Optional:

- `PROJECT_PA_MIGRATION_PLAN.md`
- `Docs/IslandLife/GATHERING_AND_SHOP_GATE.md`

Validators:

- `PA_FinalPresentationReviewer.RunFinalPresentationReview`
- `PA_CoreSlicePlayabilityValidator.RunCoreSlicePlayabilityValidation`

Human check: required for visual quality approval.

Forbidden changes: Project_D copy/merge, commercial-game names/logos/exact UI, broad scene overwrite without backup.

## NPC / Economy Sprint

Required:

- `PROJECT_PA_CREATIVE_NORTH_STAR.md`
- `PROJECT_PA_DESIGN_INTENT.md`
- `Docs/02_경제_및_아이템_설계.md`
- `Docs/03_NPC_및_AI_시스템.md`
- `PROJECT_PA_FULL_GAME_BACKLOG.md`

Validators:

- `PA_CustomerPresentationValidator.RunCustomerPresentationValidation`
- `PA_CustomerDemandInsightValidator.RunCustomerDemandInsightValidation`
- `PA_CustomerArrivalValidator.RunCustomerArrivalValidation`
- `PA_GatheringShopGateValidator.RunGatheringShopGateValidation`

Human check: required for NPC role readability and customer-flow feel.

Forbidden changes: broad `PurchaseEvaluator`, `EconomyService`, `NpcController`, save-schema changes without approval.

## Save / Load Sprint

Required:

- `PROJECT_PA_DESIGN_INTENT.md`
- `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md`
- `PROJECT_PA_FULL_GAME_BACKLOG.md`
- `PROJECT_PA_STATUS.md`

Validators:

- `PA_LongPlayProgressionValidator.RunLongPlayProgressionValidation`
- `PA_FinalDemoRouteValidator.RunFinalDemoRouteValidation`

Human check: required for save schema approval before non-additive changes.

Forbidden changes: deleting/migrating user saves without explicit approval.

## Unity Crash Bug Sprint

Required:

- latest `PROJECT_PA_CRASH_REPORT_*.md`
- `PROJECT_PA_STATUS.md`
- `PROJECT_PA_SESSION_REPORT.md`
- `Automation/LoopEngineering/progress.md`

Validators:

- run compile/load checks only after crash cause is classified

Human check: required if crash persists or graphics API/driver changes are needed.

Forbidden changes: repeated Unity launches without reading logs, batchmode while Editor is open.

## Build / Package Sprint

Required:

- `README.md`
- `PROJECT_PA_COMPLETION_PLAN.md`
- `PROJECT_PA_STATUS.md`
- `PROJECT_PA_SESSION_REPORT.md`

Validators:

- `PA_FinalDemoRouteValidator.RunFinalDemoRouteValidation`
- executable human route check

Human check: required for final package acceptance.

Forbidden changes: include `Library`, `Temp`, `Logs`, `.git`, cache/generated folders in source package.

## Documentation Sprint

Required:

- `PROJECT_PA_STATUS.md`
- `PROJECT_PA_TODO.md`
- `PROJECT_PA_SESSION_REPORT.md`
- `Docs/07_개발일지.md`

Optional:

- files specific to the work being summarized

Validators:

- no Unity validator required unless docs describe a new validation result

Human check: optional unless the doc becomes submission material.

Forbidden changes: inventing validation results, changing gameplay files for documentation-only work.

## World Architecture Context (WORLD tickets)

Read in this order after the common entrance documents:

1. `PROJECT_PA_WORLD_NORTH_STAR.md`
2. `Docs/WorldArchitecture/WORLD_ARCHITECTURE_PLAN.md`
3. `Docs/WorldArchitecture/WORLD_DECISIONS.md`
4. `Docs/WorldArchitecture/WORLD_SYSTEM_IMPACT_MAP.md`
5. `Docs/WorldArchitecture/WORLD_BOUNDED_BACKLOG.md`

Official scene roles and modification priority:

- `Assets/Scenes/Prototype_FirstDay.unity`: **Golden Regression Scene**. Preserve Day 1~3/Core Slice/validator references; never use as a new-world experiment target.
- `Assets/Scenes/WorldSandbox.unity`: **New World Technology Testbed**. It does not exist at WORLD-000; create only in an approved follow-up with an editor builder and validator. WORLD tickets target it unless a ticket explicitly says otherwise.
- `Assets/Scenes/MainGame.unity`: **Validated World + Existing Gameplay Integration candidate**. Read-only until World Data, Terrain, Building, Navigation and Existing Gameplay Gates pass and a separate integration ticket is approved.

World scene edits prefer editor builder/setup utility → object/reference validator → Unity save → diff/Missing Reference → human Game View. Do not infer unused objects from names or perform broad scene serialization edits.
