# Project PA TODO

## 2026-09-09 — P3 완료 / STOP

- [x] VS-PRESENT-001-P3: hub1 + selected companion shelter2, collision/cancel/rotate/move, companion linkage, SaveManager save/fresh reentry, B09 storage regression, final 1920×1080 evidence.
- [ ] 사람 확인: 마우스 배치/90도 회전·이동 조작, 새 Play에서 저장한 정착 이어서 불러오기, 최종 화면 가독성.
- P4 생산/First Night 및 CONTENT 후속은 시작하지 않는다. 다음 작업은 별도 사람 지시를 기다린다.


## Milestone M85 — GAMEPLAY BETA (2026-08-11)

Status: `IN_PROGRESS`; branch `milestone/gameplay-beta-85`; approved through `BETA-010`.

- [x] BETA-001 — Player Onboarding and World Readability: Day 1 start prompt, first objective route, functional landmarks, player-facing HUD, default-hidden development surfaces, D3D11/M70/Golden regressions.
- [x] BETA-002 — Daytime Activity Completion: generated-island Gathering/Farming/Mining/Fishing, Space interaction, Inventory, 120G Raw value, daily reset.
- [x] BETA-003 — Crafting and Production Expansion: connected daytime resources to 5 processed products, visible facility cards, preserved quality/value and real B01 sale.
- [x] BETA-004 — Shop Readability and Merchandising.
- [x] BETA-005 — Customer Strategy and Feedback: actual Miner/Tailor preferences, synchronous player-HUD evidence, reject/purchase feedback and demand signals; D3D11/regressions PASS.
- [x] BETA-006 — Phone Hiring and Feed Completion: runtime Phone, 8 visible candidates, exact-cost role hire, live sales/village Feed, Audit/Settings; D3D11/regressions PASS.
- [~] BETA-007 — Village Response and NPC Integration: `BETA_007_IMPLEMENTED_WITH_VALIDATION_DEBT` local checkpoint. 판매→Day 2→역할 시설→주민 대화/HUD 구현과 정적 compile은 완료했지만 두 D3D11 실행이 anchor readiness에서 종료되어 실제 거래 단계 미검증. 세 번째 실행은 새 승인 전 금지.
- [~] BETA-008 — 7-Day Progression: `BETA_008_IMPLEMENTED_WITH_VALIDATION_DEBT` local checkpoint. 일반 Day 1 관광객, Day 1~4 실제 판매/정산/날짜 증가, Day 2~5 명시 납품, Day 5 유료 Farmer 고용까지 D3D11 증명. 최종 서로 다른 고가 상품 fixture correction은 compile-only이며 Day 6~7/Week 1 완료는 미검증.
- [ ] BETA-009 — Persistence and Recovery Pass.
- [ ] BETA-010 — M85 Full Playable Beta Integration.
- [ ] M85 integrated human playtest once at milestone end; keep visual/capture/audio/B06/F10 polish in `M85_HUMAN_REVIEW_BACKLOG` unless it blocks comprehension.

## LOOP-POLICY-002 — Preapproved Milestone Continuation (2026-08-11)

- [x] 기본 bounded-ticket 사람 승인 게이트 유지.
- [x] 명시적으로 선승인되고 loop-state에 기록된 sequence 전용 `PREAPPROVED_MILESTONE_CONTINUATION` 추가.
- [x] M70 범위를 WORLD-005/006/006B/007/008/009/010으로 제한하고 WORLD-011 이후 자동 확장 금지.
- [x] 단일 active ticket, ticket별 검증, 승인된 local commit, hard blocker와 Git/D3D11/crash 안전 규칙 유지.
- [x] 동일 blocker 반복 보고 억제 및 context limit을 `PAUSED_BY_CONTEXT_LIMIT`로 분리.
- [x] baseline `e0b5678`의 M70 완료를 인식하고 완료된 WORLD ticket 재실행 방지 (`nextTicket=null`).
- [ ] WORLD-011/MainGame 또는 새 milestone은 사람의 별도 명시 승인 필요.

## Milestone M70 — PLAYABLE WORLD ALPHA (2026-08-11)

Status: `M70_PLAYABLE_WORLD_ALPHA_COMPLETE`; automatic continuation stopped.

- [x] WORLD-001~004 cell/chunk terrain, mesh, Terraform, surface/path/water foundation.
- [x] WORLD-005 B09 relocatable building and occupancy/reachability contract.
- [x] WORLD-006 deterministic provisional island generation and 64-chunk projection.
- [x] WORLD-006B functional movable shop furniture proof.
- [x] WORLD-007 v11 procedural world persistence with safe restore.
- [x] WORLD-008 logical reachability and local-sector NavMesh prototype.
- [x] WORLD-009 existing gameplay authority adapter.
- [x] WORLD-010 existing movement/camera plus gather→craft→stock→open→NPC sale→revenue→restart/load integration.
- [x] D3D11 primary M70 validation, nine Golden regressions, WORLD-006B/007/008/009 regressions, blocking Console 0, new crash 0.
- [x] Prototype_FirstDay/MainGame/scene YAML/prefab/Packages/ProjectSettings/SaveData DTO unchanged.
- [ ] Human integrated playtest: pacing, camera framing, coast/terrain feel, building/display placement feel, and shop readability.
- [ ] `CAPTURE_EVIDENCE_DEBT`: optional evidence capture only; no additional manual capture request is required.
- [ ] Nonblocking UI/polish backlog: inventory drag ghost, development overlay overlap, phone hiring/feed empty-state, B06 pulse/fallback audio, map composition.
- [ ] `WORLD-MAIN-001` MainGame integration requires separate explicit human approval.
- [ ] Permanent final island size remains undecided; 128×128 is provisional only.

Inspection date: 2026-06-19

## Sprint: B02~B04 Shop Evolution Visual Finalization — 2026-07-16

Status: 구현·동일 구도 시각 검토·D3D11 단계 전환/출입/판매 회귀 완료.

- [x] B02~B04 원본 FBX·래퍼·bounds/polycount·4면·콜라이더·Shop/ShopSlot 구성을 Unity Editor API로 감사.
- [x] 세 모델의 실제 정면이 로컬 `-X`이며 코지 마을 단계형 외관으로 유지 가능한지 게임 카메라에서 확인.
- [x] 원본 Visual만 참조하는 Shop/ShopSlot 없는 파생 외관 프리팹 3개 생성.
- [x] 보이는 모델과 일치하는 BoxCollider/NavMeshObstacle 및 Entrance/Sign 기준점 구성.
- [x] 기존 `currentTier`에서 Tier 0=B10, Tier 1=B02, Tier 2=B03, Tier 3+=B04 외관을 파생.
- [x] 기존 외부 문·스폰·BuildingEntrance·간판을 활성 모델의 실제 문에 정렬.
- [x] Tier 1/2/3 실제 Play Mode 동일 카메라 캡처 후 B02 입구 anchor 방향 오류 수정·재캡처.
- [x] 각 단계에서 외관 1개, Shop 중복 0, 기존 실내 배치 스냅샷 완전 보존 검증.
- [x] Tier 잠금/개방·입장·진열·가격·퇴장과 FinalDemoRoute 30G 회귀 PASS.
- [x] P5 상점 진화·배치 해금 완료. Tier 변화가 기존 배치를 삭제하지 않고 zone·진열 한도·작업대 카탈로그를 확장하며, Processed 테마와 v10 복원까지 연결했다.

## Sprint: Tripo Character Unity Finalization — 2026-07-16

Status: 구현·시각 검토·D3D11 walking 및 고객/최종루트 회귀 완료.

- [x] C-01~C-09 FBX, Humanoid import/Avatar, 메시·텍스처, polycount, Idle/Walk 클립을 Unity Editor API로 감사.
- [x] 게임 카메라 기준 원본 lineup과 런타임 동일 구도 idle/walking 기준 캡처 생성.
- [x] 원본 외형을 유지하고 주민별 실제 렌더 높이를 1.75m로 정규화.
- [x] 주민 발 접지, 그림자, CapsuleCollider `1.8/0.4`, NavMeshAgent `1.8/0.4/0.75` 구현.
- [x] NPC 절차 보행 cadence를 실제 이동 거리/명목 보폭 기준으로 변경.
- [x] 플레이어 기존 Foot IK에 Walk 클립/이동 속도 기반 제한 재생 속도 동기화 구현.
- [x] D3D11 컴파일과 실제 Play idle 수치/After 캡처 확인.
- [x] 검증 스테이징에서 시작 온보딩을 닫고 `Time.timeScale=1`을 복원해 walking 최종 검증 PASS.
- [x] 실제 도로/게임 카메라 `character_walk_after.png`를 직접 확인하고 InteriorCustomer/CustomerArrival/FinalDemoRoute 회귀 PASS.
- [ ] 사용자 확인: C-03/C-04 역할 인상 불일치, C-05 Farmer/Fisher 중복, C-02 미사용의 최종 주민 매핑.
- [x] B02~B04 상점 진화 모델의 입구·스케일·카메라 가림·Tier 전환 시 기존 배치 보존을 실제 캡처로 검증.

## Sprint: B05 Workbench Functional Art — 2026-07-16

Status: Implemented, visually reviewed, and D3D11 verified.

- [x] Audit the source FBX, wrapper prefab, mesh bounds/polycount, four faces, collider, obstacle, and real 2×2 runtime placement.
- [x] Preserve the modeled workbench identity and choose existing Wood→Plank processing as its minimum day-prep role.
- [x] Align the modeled work surface with the existing local `-Z` interaction/clearance contract without overwriting source assets.
- [x] Correct the runtime physical collider and carving obstacle while preserving prefab dimensions used by the 2×2 footprint calculation.
- [x] Generate a persistent low-poly preparation kit using only Project PA-owned materials: raw Wood, guide surface, finished Plank, and clamp.
- [x] Add short clamp/light feedback only after a successful existing crafting transaction.
- [x] Verify actual CraftingUI Wood 2→Plank 1, inventory delta, feedback, and UI close in Play Mode.
- [x] Review same-camera before/after captures and reduce excessive feedback light intensity.
- [x] Re-run processing chain, shop customization/save, enterable shop, and final demo route regressions.
- [x] Same-angle player/NPC walking, grounding, stride, collider, and NavMeshAgent finalization completed while preserving character identity.

## Sprint: B10 Cottage Visual Finalization — 2026-07-16

Status: Implemented, visually reviewed, and D3D11 verified.

- [x] Audit the Tripo source FBX, wrapper prefab, scene instances, collider, and detached overlay with Unity Editor API.
- [x] Confirm the source mesh is grounded/intact and identify authored facade as local `-X`.
- [x] Face all three map cottages toward the plaza without overwriting the source FBX, wrapper prefab, or main scene.
- [x] Disable the legacy Static B10 duplicate only when authoritative map cottages exist.
- [x] Hide the detached primitive door/sign renderers while preserving BuildingEntrance, collider, tier gate, and spawn references.
- [x] Generate a purpose-built beveled wood/cream shop sign mesh and prefab with Unity Editor API.
- [x] Verify fixed-angle Before/After and Play Mode MainCamera captures; remove billboard intersection and make the full sign text readable.
- [x] Re-run Tier 0→Tier 1 interior shop round trip and FinalDemoRoute 30G regressions.
- [x] Finalize B05 Workbench functional art around the existing day-prep/processing/placement role.
- [x] Same-angle player/NPC walking, grounding, stride, collider, and NavMeshAgent review completed.

## Sprint: Shop Customer Approach P4 — 2026-07-16

Status: Implemented and D3D11 verified.

- [x] Reuse placeable definition `interaction` cells as explicit ShopSlot front positions.
- [x] Rotate the approach cell with the moved shelf; `(4,0)/r3` resolves to `(3,0)`.
- [x] Reserve a ShopSlot approach by NPC owner before travel so two customers do not converge on one point.
- [x] Reject blocked authored cells and select another stocked, reachable shelf.
- [x] Require `NavMesh.SamplePosition` and `CalculatePath=PathComplete` before setting the destination.
- [x] Re-select if a shelf moves or rotates during a visit.
- [x] Stop at the reserved point, face the shelf, and preserve existing claim/purchase math.
- [x] Verify real interior reserve→approach→15G purchase→return and simultaneous reservation ownership.
- [x] Re-run CustomerArrival and FinalDemoRoute regressions.
- [x] Repair B10 Cottage detached/floating door-wall pieces without overwriting the Tripo source or main scene YAML.
- [x] Finalize B05 workbench functional art around the existing day-prep/processing role.

## Sprint: Outdoor Placement P3 — 2026-07-16

Status: Implemented and D3D11 verified.

- [x] Register shared 2m `village.outdoor` zone without a parallel grid.
- [x] Protect north-south/east-west roads, shop plaza, exterior entrances, and spawn cells.
- [x] Connect `BuildManager` to owner-based collider footprint and front clearance.
- [x] Keep R/click building controls and add M relocate/X safe recovery with player-facing hint.
- [x] Define B09 as an external 24-slot village storage shed after game-camera review.
- [x] Keep the authored B09 movable but non-recoverable; reject recovery of storage containing items.
- [x] Persist authored/player-built B09 zone, cell, rotation, and contents in save v10.
- [x] Suppress active legacy duplicate B09 renderer/collider at runtime.
- [x] Verify D3D11 road/plaza rejection, B09 9-cell footprint, move/build/recover, and v10 load.
- [x] Re-run SaveRoundTrip, ShopCustomization, and FinalDemoRoute regressions.
- [x] P4: ShopSlot interaction/approach cells, multi-customer reservation, and `NavMesh.CalculatePath` reachability completed.
- [x] Visual: repair B10 Cottage detached/floating door-wall pieces without overwriting the Tripo source.
- [x] Functional art: finalize B05 workbench silhouette around the existing prep/processing role.
- [x] Character pass: same-angle player/NPC walking capture, grounding, stride, collider, and agent tuning.

## Sprint: Day 1-3 Core Game Slice

Goal:

- Establish Day 1-3 as the long-term full-game baseline, not a submission-only prototype route.
- Make the current player view understandable before adding more features.
- Preserve the reverse supply-chain management identity while reducing debug/advisor clutter.

Status: First planning and presentation-mode pass complete; Unity Play Mode validator rerun is pending because the project is currently open in Unity Editor.

- [x] Read `PROJECT_PA_CREATIVE_NORTH_STAR.md` and `PROJECT_PA_DESIGN_INTENT.md` as the top design standard.
- [x] Inventory current player-visible UI, labels, runtime canvases, and presentation markers.
- [x] Separate final player-facing elements from development/presentation overlays.
- [x] Create `PROJECT_PA_CORE_SLICE_PLAN.md`.
- [x] Add `CoreSlicePresentationMode` as a presentation-only sidecar.
- [x] Register `CoreSlicePresentationMode` through `PA_RuntimeSceneBinder`.
- [x] Hide development/advisor canvases by default while preserving validator access.
- [x] Hide presentation/debug route markers and NPC role badges by default.
- [x] Keep objective, money/tier, clock, hotbar, interaction prompt, dialogue, ShopPriceUI, smartphone/audit, NPC bubble, and Day/Night phase HUD visible.
- [x] Verify C# build with `dotnet build Assembly-CSharp.csproj` (passed with 0 warnings and 0 errors after adding the script `.meta`).
- [ ] Run `PA_FinalDemoRouteValidator.RunFinalDemoRouteValidation` after closing the current Unity Editor instance or from the open Editor menu.
- [ ] Run `PA_LongPlayProgressionValidator.RunLongPlayProgressionValidation` after closing the current Unity Editor instance or from the open Editor menu.
- [ ] Manually inspect the Game view with development overlays hidden by default.
- [ ] Decide whether `DayNightShopLoopCanvas` should remain visible or be folded into the main objective/HUD in the next sprint.

Acceptance target:

- Day 1-3 reads like a coherent cozy management life-sim slice: day prep, night shop, customer response, settlement, and next-day planning.

## Creative North Star Lock - 2026-06-21

Status: Done for documentation pass; no code, scene, UI, or asset implementation was performed.

- [x] Created `PROJECT_PA_CREATIVE_NORTH_STAR.md`.
- [x] Reframed Project_PA as a cozy 3D life and shop management simulation.
- [x] Preserved reverse supply-chain design as the economic backbone/support system.
- [x] Reinterpreted NPC production as support, automation, and growth that reduces repetitive labor over time.
- [x] Reframed the shop/stall as a day-to-night shop hub and village-change interface.
- [x] Marked Project_D / ReferencePrototype and VisualTargets as reference-only material.
- [x] Updated planning docs so future visual/development work reads `PROJECT_PA_CREATIVE_NORTH_STAR.md` first.

Next Milestone 1 tasks:

- [x] CDN-001 Define and implement day/night phase flow.
- [x] CDN-002 Add readable shop open/close state.
- [x] CDN-003 Add daily settlement that includes money, customer feedback, product category impact, and next action.
- [x] CDN-004 Add at least two daytime activities or MVP equivalents that prepare shop stock.
- [x] VC-001 Add one visible product-category-to-village-change signal.
- [x] SPY-002 Present at least two customer types with readable buy/reject feedback.

SPY-002 implementation notes - 2026-06-22:

- [x] Added `CustomerPreferencePresentationController` (read-only "관심 손님 성향" panel from real `NpcProfile` traits).
- [x] Added `PurchaseFeedbackPresentationController` (cozy buy/reject reasons + village-change tie line, no debug numbers).
- [x] Added a single read-only hook in `NpcController.EvaluateCurrentSlot` (same pattern as the demand-insight hook).
- [x] Registered both controllers through `PA_RuntimeSceneBinder`.
- [x] Added `PA_CustomerPresentationValidator` and re-ran the four existing validators.
- [x] Preserved `PurchaseEvaluator`, `Shop`, `ShopSlot`, `ShopPriceUI`, `EconomyService`, NPC FSM, Save, and the existing head bubble.
- [x] Used only real data: traitSN (category), traitTF (buy style), traitEI (eagerness); priceSensitivity is identical (1.0) across profiles so it is intentionally not shown yet.
- [ ] Manual 1920x1080 Game-view review of the two new panels (see `Docs/CustomerPresentation/README.md`).

SPY-003 per-resident consumption data - 2026-06-22:

- [x] Varied `priceSensitivity` per resident (0.65 ~ 1.45) across the eight `Assets/Resources/NPCs/Profile_*.asset` files.
- [x] Varied `utilityConsumption`/`luxuryConsumption` to reflect each job archetype (data only; no `PurchaseEvaluator` code change).
- [x] "가격에 민감/관대" preference hint now appears from real data (Farmer/Miner sensitive, Tailor tolerant).
- [x] Extended `PA_CustomerPresentationValidator` to assert the data-driven price hints.
- [x] Re-ran all five validators (CustomerPresentation/FinalRoute/DayNight/Village/LongPlay) — all passed; `paid=30G` and `money=4633G` unchanged.
- [ ] Manual 1920x1080 Game-view review still pending.

SPY-002 panel layout validation - 2026-06-22:

- [x] Added `Assets/Editor/PA_CustomerPanelLayoutValidator.cs` (coordinate-based overlap/containment checks + 1920x1080 screenshot).
- [x] Improved the validator (hotbar region from active `InventorySlotUI`, onboarding modal dismissed) and re-ran it.
- [x] Run 2 caught a real overlap: `손님 반응` panel overlapped the hotbar's right edge.
- [x] Fixed by raising `PurchaseFeedbackPresentationController` `anchoredPosition.y` 24 → 170 (presentation only).
- [x] Run 3 passes all checks incl. hotbar (`Logs/Codex_PanelLayout_Validation3.log`); clean screenshot at `Logs/CustomerPanelReview/20260622_112554/`.
- [x] Re-ran the five existing validators after the panel move — all passed (`Logs/Codex_PanelLayout_Reg_*.log`).
- [ ] Human readability/Korean-font confirmation on a real monitor still recommended (geometry is automated, aesthetics are not).

Milestone 1 implementation notes - 2026-06-21:

- [x] Added `DayNightShopLoopController` as a sidecar day/night phase service.
- [x] Added readable `Day Prep`, `Night Shop Open`, and `Settlement` state text.
- [x] Preserved Day 1 tutorial shop access so the existing validated route still works.
- [x] Added two runtime daytime stock-prep MVPs through `DaytimeStockPrepPoint`: `Garden Prep Basket` and `Producer Drop Box`.
- [x] Added `VillageChangeSignalController` to show a product-category village direction signal from recent sales.
- [x] Added `Village direction` to the Day 1 summary/settlement flow.
- [x] Added `PA_DayNightShopLoopValidator` and `PA_VillageChangeSignalValidator`.
- [x] Re-ran Day 1 route and long-play validators after the new Milestone 1 layer.
- [ ] Replace the two MVP stock sources with richer cozy daytime activities later: gathering, fishing, talking, buying supply, or processing.
- [ ] Manually review the new phase, prep, village-direction, and summary UI in a real 1920x1080 Game view.

Before any Visual Acceleration, shop, NPC, economy, or long-play implementation task:

- Read `PROJECT_PA_CREATIVE_NORTH_STAR.md`.
- Preserve the day-to-night cozy life/shop identity.
- Keep Project_D as reference only.
- Do not copy assets or imitate commercial UI/systems exactly.

## T001 Project Status Check

Status: Done for initial inspection.

- [x] Confirmed project root.
- [x] Confirmed Git root.
- [x] Confirmed Unity folders.
- [x] Checked Git status.
- [x] Inventoried scenes, scripts, prefabs, docs, data assets, and build settings.
- [x] Created `PROJECT_PA_STATUS.md`.
- [x] Created `PROJECT_PA_TODO.md`.

Notes:

- Initial Git status was clean.
- Documentation files now exist and will make Git status dirty until committed or ignored intentionally.

## T002 Play Mode Error Fix If Needed

Status: Batch Play Smoke, automated final route validation, final presentation review, and human Windows executable route review passed.

- [x] Batch compile in Unity 6000.3.2f1 completed without C# compiler errors.
- [x] Automated smoke attempt loaded `Assets/Scenes/Prototype_FirstDay.unity`.
- [x] Runtime log showed `PA_RuntimeSceneBinder` and `Shop.Start()` running.
- [x] Fixed the automated Play Smoke test so it survives Unity domain reload and exits cleanly.
- [x] Automated Play Smoke passed.
- [x] Added Editor-only final route validator: `Assets/Editor/PA_FinalDemoRouteValidator.cs`.
- [x] Automated final route validation passed in Play Mode.
- [x] Final presentation review passed with generated Game-view captures.
- [x] Fixed Windows-player NavMesh startup blocker by rebaking `Prototype_FirstDay.unity` NavMesh and enabling NPC agents from `PA_RuntimeSceneBinder` after NavMeshSurface data is active.
- [x] Play Smoke counts: players=1, shops=2, shopSlots=8, economyServices=1, shopPriceUI=1, npcs=8, npcAgents=8/8, npcAgentsOnMesh=8.
- [x] Open/load `Assets/Scenes/Prototype_FirstDay.unity` by Unity automation for final presentation review.
- [x] Check Unity compile/log state by automation.
- [x] Enter Play Mode by automated validation.
- [x] Complete automated route: dialogue -> stock -> price -> NPC purchase -> money update -> HUD/audit/summary.
- [x] Complete the same full demo route by a human at the keyboard: move -> stock -> price -> NPC purchase -> money update.
- [ ] Capture exact compile/runtime errors if any appear in manual review.
- [ ] Fix only blocking errors required for final demo.
- [ ] Re-test Play Mode after each fix.

Acceptance target:

- `Prototype_FirstDay.unity` enters Play Mode without blocking console errors.

## T003 Main Scene Flow Verification

Status: Automated route validation and final presentation screenshot review passed; human camera/control feel review remains.

- [ ] Confirm intended final start scene: `Prototype_FirstDay.unity` or `MainGame.unity`.
- [x] Confirm intended final start scene: `Prototype_FirstDay.unity`.
- [x] Verify player movement components exist.
- [x] Verify presentation camera framing by generated screenshots.
- [x] Verify interact prompt appears in generated screenshots.
- [x] Verify NPC dialogue opens `DialogueUI`.
- [ ] Verify inventory/hotbar interaction.
- [x] Verify stocking a shop slot from hotbar.
- [x] Verify price-setting UI opens and confirms price.
- [x] Verify NPC purchase/sales/money update through route validator.
- [x] Verify audit panel goal text.
- [ ] Verify save/load behavior only if it is part of the final demo.

Acceptance target:

- A short final-demo route can be completed reliably from a fresh Play Mode start.

## T004 Visual Polish Pass

Status: First final-presentation pass done; human subjective polish pass remains.

- [x] Check final scene camera framing by generated presentation captures.
- [ ] Check lighting and day/night visibility.
- [ ] Check obvious placeholder objects.
- [x] Check NPC/player readability in presentation captures.
- [ ] Check shop/workbench/building labels.
- [ ] Keep polish scoped to final-demo clarity.

Acceptance target:

- Demo scene looks intentional enough for final submission screenshots/video.

## T005 UI Readability Pass

Status: Final presentation screenshot review passed; a human 1920x1080 playthrough is still recommended.

- [x] Check `MoneyHUD` does not overlap objective panel in automated layout validation.
- [x] Add next-tier goal text to `MoneyHUD`.
- [x] Update audit app next-tier text from actual `TierDefinition` data.
- [x] Update Day 1 summary to show next growth requirements from actual tier data.
- [x] Remove emoji from `ShopPriceUI` reaction labels to reduce font fallback risk.
- [x] Shorten NPC purchase/rejection feedback text.
- [x] Enlarge and wrap `NpcBubbleUI` for compact feedback.
- [x] Check Korean text rendering and font fallbacks through generated Game-view captures.
- [x] Check objective text readability.
- [ ] Check dialogue panel readability.
- [x] Check shop pricing panel readability.
- [x] Check smartphone audit panel readability.
- [ ] Check pause/settings panel.

Acceptance target:

- UI text is readable at the target build resolution and does not block key gameplay.

## T006 Windows Build

Status: Latest automated build, smoke launch, and human executable full-route review passed.

- [x] Remove missing `Assets/Scenes/SampleScene.unity` from Build Settings.
- [x] Add/confirm final start scene in Build Settings: `Assets/Scenes/Prototype_FirstDay.unity`.
- [x] Confirm Windows standalone build path.
- [x] Build into project-local folder: `Builds/Windows/`.
- [x] Run the produced executable in smoke mode.
- [x] Confirm player smoke log reaches runtime binder/shop startup.
- [x] Confirm previous `Failed to create agent because there is no valid NavMesh` message is gone.
- [x] Rebuilt Windows executable after final route/UI readability fixes.
- [x] Smoke-launch rebuilt Windows executable after final presentation fixes.
- [x] Verify the same final-demo route in the executable by a human.

Acceptance target:

- Windows executable launches and reaches the playable demo without blocking errors.

## T007 Submission Zip Package

Status: Done. Source and executable submission packages were created after the human Windows executable route pass.

- [x] Define source package target name: `Project_PA_Source_20260620.zip`.
- [x] Define executable package target name: `Project_PA_Windows_20260620.zip`.
- [x] Define source include list: `Assets/`, `Packages/`, `ProjectSettings/`, `Docs/`, root `PROJECT_PA_*.md`, `README.md`, `.gitignore`, and solution/project files if required.
- [x] Define source exclude list: `.git/`, `Library/`, `Temp/`, `Logs/`, `Builds/`, `UserSettings/`, `obj/`, `.vs/`, and cache/generated folders.
- [x] Define executable include list: `Project_PA.exe`, `Project_PA_Data/`, `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `DirectML.dll`, `D3D12/`, `MonoBleedingEdge/`, and run instructions.
- [x] Define executable exclude list: `Project_PA_BurstDebugInformation_DoNotShip/`, `Logs/`, source folders, cache folders, and generated debug folders.
- [x] Create executable package after human Windows exe route pass.
- [x] Create source package after human Windows exe route pass.
- [x] Confirm packages open and scan correctly with forbidden entries excluded.

Created packages:

- `SubmissionPackages/Project_PA_Source_20260620.zip` - about 359.17 MiB, 1,369 entries.
- `SubmissionPackages/Project_PA_Windows_20260620.zip` - about 84.25 MiB, 183 entries.

Acceptance target:

- Submission has a separate playable executable package and source package.

## T008 README / Run Instructions

Status: First draft done; update again after manual route verification.

- [x] Create or update root README.
- [x] Include Unity version: 6000.3.2f1.
- [x] Include how to open source project.
- [x] Include how to run executable.
- [x] Include controls from `PlayerInputHandler`.
- [x] Include final demo route.
- [x] Include known limitations if any remain.

Acceptance target:

- A grader can run the executable and understand the intended demo route without extra explanation.

## Sprint: Cozy Market Visual Acceleration

Goal:

- Use `CozyMarketPrototype` / `ReferencePrototype` as a visual and staging reference only.
- Improve Project_PA market clarity without copying unsafe assets or changing gameplay logic.
- Keep all implementation inside Project_PA.
- Read `PROJECT_PA_DESIGN_INTENT.md` before every Visual Acceleration task so visual work supports the reverse supply-chain management identity.

## T009 Reference Prototype Audit

Status: Done for planning.

- [x] Verified corrected reference root.
- [x] Confirmed reference Unity folders.
- [x] Inspected scenes, prefabs, models/meshes, materials, UI textures, scripts, and build settings.
- [x] Identified market/stall, crate, sign, path, lamp, bench, display, register, NPC, and UI reference ideas.
- [x] Classified broad reference asset folders and scripts as unsafe for direct migration.
- [x] Created `PROJECT_PA_MIGRATION_PLAN.md`.

Acceptance target:

- Reference ideas are documented without copying assets.

## T009.5 Design Intent Lock

Status: Done.

- [x] Created `PROJECT_PA_DESIGN_INTENT.md`.
- [x] Locked Project_PA as a reverse supply-chain management simulation.
- [x] Clarified that the player is a manager, not the primary laborer.
- [x] Clarified that Project_D / ReferencePrototype and VisualTargets are visual, route, staging, and readability references only.
- [x] Clarified that T010 Market Stall Visual Migration must make the stall a readable operating hub for the economy, not merely a prettier shop.

Required for later Visual Acceleration tasks:

- Read `PROJECT_PA_DESIGN_INTENT.md` before editing scenes, UI, prefabs, or presentation docs.
- Preserve Project_PA systems and naming.
- Do not copy assets from ReferencePrototype unless a later task explicitly approves a safe, file-by-file migration.

## T010 Market Stall Visual Migration

Status: Done for first visual pass; automated Play Smoke passed; needs manual camera review.

- [x] Confirm no direct reference assets will be copied unless ownership is explicitly approved.
- [x] Backup target scene before edits.
- [x] Create a Project_PA-owned market stall visual variant.
- [x] Add visible product shelf/slot markers.
- [x] Add Project_PA-created crate/basket-like display props.
- [x] Add price tag anchors or simple labels.
- [x] Add small signboard and warm accent props.
- [x] Verify `Shop` and `ShopSlot` scene references remain present by Unity batch verification.
- [x] Automated Play Smoke confirmed player/shop/slot/economy/UI/NPC runtime objects.
- [ ] Manually verify stock -> price -> NPC purchase in Play Mode.

Acceptance target:

- The stall reads as the reverse supply-chain operating hub from the gameplay camera.

## T011 Project_PA Market Scene Staging

Status: Done for first visual pass; needs manual route review.

- [x] Use `Prototype_FirstDay.unity` as the staging scene.
- [x] Create a scene backup first.
- [x] Add route markers around the market stall.
- [x] Add delivery/customer/reinvestment visual pads.
- [x] Add Project_PA-owned primitive signs and markers.
- [x] Preserve current scenario logic.
- [ ] Manually review whether player start and camera make the route visible.

Acceptance target:

- The first-day route visually guides talk, stock, price, and sale steps.

## T012 Shop UI Restyle

Status: Inspected; layout change deferred until manual 1920x1080 review.

- [ ] Review objective panel readability in Play Mode.
- [x] Review `ShopPriceUI` script for required information.
- [x] Review money HUD script for money/tier readability.
- [ ] Review interaction prompt placement.
- [ ] Use existing Project_PA UI sprites and colors first.
- [ ] Keep UI behavior unchanged.

Notes:

- `ShopPriceUI` already shows item name, current price, NPC reaction hint, approximate purchase rate, confirm, retrieve, and close controls.
- No UI behavior/layout code was changed in this pass because visual verification needs an actual 1920x1080 Play Mode view.

Acceptance target:

- Key demo UI is readable at target build resolution.

## T013 NPC/Customer Presentation Pass

Status: Done for first visual pass; needs manual readability review.

- [x] Add role badges for NPC economy roles.
- [x] Add customer approach pad near the shop.
- [x] Avoid gameplay logic changes.
- [ ] Check first settler visibility in Play Mode.
- [ ] Check NPC bubble/label readability in camera.
- [ ] Check player/NPC scale relative to stall.

Acceptance target:

- The evaluator can understand who to talk to and who buys from the stall.

## T014 Demo Route Screenshot Pass

Status: Done for final presentation capture pass.

- [x] Add `PA_ScreenshotCameraMarker_MarketHub` to the demo shop.
- [x] Capture and inspect final presentation views.
- [x] Verify stall, player, NPC, product display, price UI, and money HUD are visible.
- [x] Adjust UI/readability only, without changing gameplay logic.
- [ ] Save representative screenshots for report/presentation if requested.

Acceptance target:

- The final demo has at least one clear presentation-quality market view.

## T015 Build and Submission Package

Status: Done for submission packaging after human Windows executable route verification.

- [x] Remove missing build-settings scene entry.
- [x] Confirm final start scene.
- [x] Build Windows executable.
- [x] Smoke-launch executable.
- [x] Verify executable demo route manually.
- [x] Define executable/source package include-exclude plan.
- [x] Package executable after human route pass.
- [x] Package source without cache/generated folders after human route pass.
- [x] Include README/run instructions draft.
- [ ] Select/include final report/presentation.

Acceptance target:

- Final submission package contains a runnable build and source package.

## Sprint: Complete Game Foundation

Goal:

- Keep Project_PA aligned with `PROJECT_PA_DESIGN_INTENT.md`.
- Turn the first-day demo from a simple sale tutorial into a readable reverse supply-chain management loop.
- Use existing systems instead of rewriting `Shop`, `ShopSlot`, `ShopPriceUI`, `EconomyService`, `PurchaseEvaluator`, tier, audit, save, hiring, or NPC AI logic.

## T016 Completion Plan Documents

Status: Done.

- [x] Created `PROJECT_PA_COMPLETION_PLAN.md`.
- [x] Created `PROJECT_PA_RELEASE_BACKLOG.md`.
- [x] Created `PROJECT_PA_CURRENT_MILESTONE.md`.
- [x] Defined complete-game target, 1.0 target, submission-demo scope, Day 1 loop, Day 7 goals, tier growth, NPC roles, UI/art/build QA standards, and deferred items.

Acceptance target:

- Future Codex sessions can choose work from a clear long-term plan without drifting into a simple shop clone.

## CL-001 Management Loop Objective Rewrite

Status: Done for first pass; manual readability review required.

- [x] Rewrote first-day objective labels around supply, stocking, pricing, customer reaction, revenue, audit/tier review, and saving.
- [x] Preserved `PlayableDayScenarioController` stage flow.
- [x] Did not change core shop/economy logic.

Acceptance target:

- The objective panel communicates that the player is managing an economic loop, not just doing errands.

## CL-002 NPC Purchase Feedback

Status: Done for first pass; automated bubble compactness check passed, human readability review still recommended.

- [x] Added buy/reject feedback text in `NpcController`.
- [x] Feedback uses existing `PurchaseEvaluator.Result` data and existing `NpcBubbleUI`.
- [x] Recent feedback is recorded by `PlayableDayScenarioController` for the day summary.
- [x] Purchase probability and broad price/category reason are visible to the player.
- [x] Shortened feedback strings for NPC feedback bubble readability.
- [x] Automated validator confirmed generated feedback fit compact length and appeared in bubble.

Acceptance target:

- A player can tell why an NPC bought or rejected an item.

## CL-003 Day Summary Improvement

Status: Done for first pass; automated full-route validation passed.

- [x] Day 1 summary now shows revenue, money delta, sales count, relationship points, recent customer feedback, and next management action.
- [x] Summary remains inside the existing startup/flow panel.
- [x] Automated validator confirmed the summary appears and includes purchase feedback, next growth goal, and tier revenue progress.
- [ ] Manually confirm summary readability in Game view.

Acceptance target:

- End-of-day feedback points the player toward restocking, price tuning, or better goods.

## CL-004 Tier 0 Goal Clarity

Status: Done for first pass; automated HUD/audit validation passed, 1920x1080 human layout review required.

- [x] `MoneyHUD` now shows current money, current tier, and next-tier requirement.
- [x] `AuditResultUI` now summarizes the actual next `TierDefinition` requirements instead of only revenue or a generic message.
- [x] Day 1 summary now pulls next growth requirements from `TierDefinition` data.
- [x] No tier progression math or unlock conditions were changed.
- [x] Automated validator confirmed `MoneyHUD` and `AuditResultUI` next-tier goal text.
- [ ] Manually check that the expanded HUD does not overlap important UI/gameplay.

## T017 Final Demo Route Validation

Status: Automated validation, final presentation review, human executable route review, and package creation are done.

- [x] Created Editor-only final route validator.
- [x] Verified Play Mode entry.
- [x] Verified player movement components.
- [x] Verified first NPC dialogue opens.
- [x] Verified hotbar-to-shop-slot stocking.
- [x] Verified price UI open/confirm.
- [x] Verified compact NPC feedback appears in `NpcBubbleUI`.
- [x] Verified sale and money/revenue update.
- [x] Verified next-tier HUD and audit text.
- [x] Verified Day 1 summary content.
- [x] Verified market hub/route/role/screenshot markers still exist.
- [x] Rebuilt Windows executable.
- [x] Smoke-launched Windows executable.
- [x] Generated final presentation captures for market hub, price UI, NPC feedback, audit app, and Day 1 summary.
- [x] Re-ran automated final route validation after presentation fixes.
- [x] Submission package plan prepared.
- [x] Human Windows executable route result recorded as passed.
- [x] Human completed full route in the Windows executable.
- [x] Source package created at `SubmissionPackages/Project_PA_Source_20260620.zip`.
- [x] Windows executable package created at `SubmissionPackages/Project_PA_Windows_20260620.zip`.

Acceptance target:

- A final evaluator can complete the first-day route and understand the management loop.

Acceptance target:

- The player can see what moves them from Tier 0 toward Tier 1 without reading code or documentation.

## Sprint: Full Game 1.0 Development Mode

Goal:

- Move Project_PA beyond the submission prototype while preserving the original reverse supply-chain management intent.
- Keep existing submission packages intact as a snapshot.
- Build the full game through incremental, verifiable systems rather than a large rewrite.

## FG-001 Core Multi-Day Loop

Status: First implementation pass validated by automated Play Mode checks.

- [x] Created `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md`.
- [x] Created `PROJECT_PA_FULL_GAME_BACKLOG.md`.
- [x] Preserved Day 1 route as onboarding baseline.
- [x] Added `LongPlayProgressionController` sidecar service.
- [x] Added Day 1-7 long-play objective plan data.
- [x] Added long-play HUD text for day, objective, revenue target, and management focus.
- [x] Registered the controller through `PA_RuntimeSceneBinder`.
- [x] Run `PA_LongPlayProgressionValidator`.
- [x] Verify Day 1 route still works after the long-play layer through `PA_FinalDemoRouteValidator`.
- [x] Verify Day 2 to Day 7 progression through `PA_LongPlayProgressionValidator`.
- [ ] Manually review subjective player feel and HUD readability in the Unity Game view.

Acceptance target:

- A player can continue beyond the first day into a readable first-week operations loop.

## FG-002 NPC Producer Economy

Status: First implementation pass validated by automated Play Mode checks.

- [x] Added Day 2-7 planned producer deliveries.
- [x] Used existing `EconomyService.TrySpend` for producer buy-in.
- [x] Used existing `Inventory.AddInstance` for delivered goods.
- [x] Added refund path when inventory is full.
- [x] Preserved existing `ProducerNpcController` and `ProductionData` without rewriting them.
- [x] Validate Day 2-7 producer deliveries increase sellable inventory.
- [x] Validate daily buy-in uses money and leaves the economy path intact.
- [ ] Add richer producer NPC presentation and delivery source after validation.
- [ ] Decide how producer reliability, friendship, and town facilities modify daily supply.

Acceptance target:

- NPC production becomes visible as the upstream source of the shop economy.

## FG-011 Save / Load / Persistence

Status: v7 first pass validated for Day 3 long-play state.

- [x] Added `SaveData.longPlayLastSupplyDay`.
- [x] Added `SaveData.longPlayDayStartRevenue`.
- [x] Added `SaveData.longPlayDayStartMoney`.
- [x] Raised `SaveManager` schema version from v6 to v7.
- [x] Added v6 -> v7 migration defaults.
- [x] Added save/load hooks for `LongPlayProgressionController`.
- [x] Save on Day 3, mutate runtime state, reload, and confirm long-play state resumes correctly.
- [ ] Repeat save/load validation after future Day 8+ systems are added.

Acceptance target:

- Long-play day state survives save/load without damaging existing Day 1, inventory, shop slot, audit, tier, hiring, or friendship data.

## FG-015 Long Play QA

Status: Tool added and passed.

- [x] Added `Assets/Editor/PA_LongPlayProgressionValidator.cs`.
- [x] Unity batch compile after code changes exited successfully.
- [x] Run the validator when the project is not locked by another Unity Editor instance.
- [x] Validate Day 2-7 producer delivery and long-play HUD.
- [x] Validate Day 3 project-local save/load.
- [ ] Add future manual Day 2-7 checklist results after a human Game view pass.

Acceptance target:

- Day 2-7 producer delivery and long-play objective UI can be verified automatically before deeper systems are added.

## FG-004 Processing And Crafting Chain

Status: First implementation pass done and validated.

- [x] Inspect `CraftingService`, `RecipeData`, `Workbench`, `CraftingUI`, and available recipes.
- [x] Add a low-risk processing opportunity layer that shows raw-vs-processed value decisions.
- [x] Preserve existing crafting behavior and recipe data.
- [x] Use existing item base prices and recipe outputs to explain why processing matters.
- [x] Add validator coverage for at least one raw -> processed value chain.
- [x] Validate existing `CraftingService.TryCraft` creates processed output.
- [x] Re-run long-play regression validation after adding the processing advisor.
- [ ] Update long-play goals so Days 4-7 point to real processing choices in a more detailed way.
- [ ] Add facility/workbench availability to the management advisor.
- [ ] Balance recipes so multiple early chains have meaningful but not runaway margins.

Acceptance target:

- The player can understand why holding, processing, or selling a producer delivery changes management outcomes.

## FG-003 Customer Simulation

Status: First implementation pass done and validated.

- [x] Inspect `PurchaseEvaluator`, `NpcProfile`, `NpcController`, `SalesLogManager`, and NPC feedback flow.
- [x] Add a low-risk customer demand insight layer instead of rewriting purchase math.
- [x] Surface customer preference/category demand in the management UI.
- [x] Track recent accept/reject outcomes by item category.
- [x] Preserve current buy/reject probability and sale logic.
- [x] Add validator coverage that customer insight is generated after buy/pass signals.
- [x] Re-run Day 1 route regression after adding the observer hook.
- [x] Re-run long-play regression after adding the demand insight layer.
- [ ] Expand demand insight from immediate signals into daily/weekly trends.
- [ ] Connect demand categories to audit/reputation/tier goals.
- [ ] Add clearer per-customer segment labels after manual UI review.

Acceptance target:

- The player can understand not only what sold, but what type of customer demand the market is signaling.

## FG-009 Tier / Audit / Reputation Progression

Status: Recommended next sprint.

- [ ] Inspect `TierService`, `TierDefinition`, `AuditService`, `AuditResultUI`, `MoneyHUD`, and new long-play/demand/processing signals.
- [ ] Define Week 1 audit criteria beyond total revenue.
- [ ] Add stock health, processed-goods usage, and demand response as audit report signals.
- [ ] Preserve existing tier unlock math in the first pass.
- [ ] Add validator coverage for an expanded audit summary.

Acceptance target:

- The first-week loop closes with management feedback about supply, processing, customer demand, and revenue, not only a money total.

## Recommended Next Codex Prompt

Use this after opening or validating the project in Unity:

```text
You are still working only inside C:\Users\sdjsd\Desktop\Unity\Project_PA.
Do not delete files, do not push to GitHub, and do not import external packages.

Next goal: submit or review the generated source/executable packages, then select the final report/presentation attachment.

First read PROJECT_PA_DESIGN_INTENT.md, PROJECT_PA_COMPLETION_PLAN.md, PROJECT_PA_CURRENT_MILESTONE.md, PROJECT_PA_STATUS.md, PROJECT_PA_TODO.md, PROJECT_PA_SESSION_REPORT.md, and README.md.

Please run Builds/Windows/Project_PA.exe and manually repeat the route: move -> first NPC talk -> stock product -> set price -> wait for NPC buy/reject feedback -> confirm money change -> open audit app -> review Day 1 summary.
Fix only blocking visual/readability or executable-route errors and keep changes minimal.
If the executable route passes, prepare source and executable submission packaging while excluding cache/generated folders.

After changes, update PROJECT_PA_STATUS.md, PROJECT_PA_TODO.md, PROJECT_PA_SESSION_REPORT.md, and README.md with what was verified and what remains.
```

## IL-001 + CDN-002 — Real Gathering & Night Shop Gate (2026-06-24)

- [x] IL-001 Added 3 spread wild-forage points (`숲길/해변/들판 채집`) via `DayNightShopLoopController`, reusing `DaytimeStockPrepPoint`, existing Raw items, daily reset, sellable `ItemInstance`. Trigger colliders so NPCs are not blocked.
- [x] Kept Garden Prep Basket / Producer Drop Box as tutorial/NPC-support backup stock.
- [x] CDN-002 Added `IsShopOpenForCustomers` gate + `ShopOpenSign` (IInteractable) open action + HUD status; single guard block in `NpcController.EvaluateCurrentSlot` (no PurchaseEvaluator/ShopSlot/FSM rewrite).
- [x] Day 1 tutorial override (`IsTutorialAlwaysOpen`) preserves the validated first-sale route.
- [x] Save v8 additive extension (`dayPrepCollectedDay` + `dayPrepCollectedActivities`) so same-day gathering survives save/load.
- [x] Added `PA_GatheringShopGateValidator` (33 checks) — passed.
- [x] Re-ran all six existing validators — passed (DayNight validator updated 1 line: collect every point before asserting exhausted).
- [x] Generated 5 1920x1080 screenshots (`Logs/GatheringShopReview/20260624_141557/`), visually reviewed.
- [ ] Human play-feel pass: walk-to-gather, NPC pathing around forage cubes, night-open → customers arrive.
- [ ] Low-poly visual polish for placeholder forage/sign cubes; move forage points to fixed scene terrain.

## Customer Arrival Pacing (IL/CDN follow-up, 2026-06-24)

- [x] Added `CustomerArrivalController` (read-only/event sidecar) that pulls customers to the shop after the player opens it (Day 2+), staggered up to a concurrent cap, via existing `SetShoppingPriority`/`TryForceShop`.
- [x] Day 1 tutorial stays passive (no double-driving with `PlayableDayScenarioController`); paused/shopping NPCs are ignored by `TryForceShop`.
- [x] Registered via `PA_RuntimeSceneBinder`; no `PurchaseEvaluator`/`ShopSlot`/`EconomyService`/FSM/Save change.
- [x] Added `PA_CustomerArrivalValidator` (16 checks) — passed; re-ran all 7 existing validators — passed.
- [ ] Tune invite interval / concurrent cap for feel; optionally bias arrival order by preference/relationship.
- [ ] Low-poly visual polish for placeholder forage/sign cubes (still open from IL-001).

## Day 1-3 Core Slice Playability Pass (2026-06-25)

- [x] Add `PA_CoreSlicePlayabilityValidator` for player-HUD/development-overlay checks.
- [x] Confirm runtime and editor C# builds pass with 0 errors.
- [x] Move the Day/Night phase HUD below the Day 1 objective HUD so the two top-center panels no longer compete for the same space.
- [x] Preserve core systems: shop, shop slot, price UI, inventory, hotbar, economy, purchase evaluation, NPC flow, day/night gate, save/load.
- [ ] Run `Project PA/Validation/Run Core Slice Playability Validation` inside the currently open Unity Editor.
- [ ] Run `Project PA/Validation/Run Final Demo Route Validation` inside the currently open Unity Editor.
- [ ] Run `Project PA/Validation/Run Long Play Progression Validation` inside the currently open Unity Editor.
- [ ] Human Game-view check: F10 toggles development overlays, normal player view hides debug panels/path labels, and Day 1-3 reads as day prep -> night shop -> customer reaction -> settlement -> next-day plan.

## Loop Engineering Dry-Run Guardrails (2026-06-26)

- [x] Create `AGENTS.md` and `CLAUDE.md`.
- [x] Create `Docs/AgentWorkflow/CONTEXT_INDEX.md`.
- [x] Create `Automation/LoopEngineering/loop-policy.json`.
- [x] Create `Automation/LoopEngineering/validator-registry.json`.
- [x] Create `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1`.
- [x] Create `Automation/LoopEngineering/Tickets/LOOP-DRYRUN-001.md`.
- [x] Run preflight and save evidence to `Automation/LoopEngineering/RunLogs/preflight-20260626.json`.
- [x] Fill missing June records in `Docs/07_개발일지.md`.
- [ ] Human reviews dirty Git baseline before any implementation loop.
- [ ] Human reviews/accepts `PROJECT_PA_CRASH_REPORT_20260625.md` as the current graphics-crash baseline.

## VC-001A Village Culture Visual Change (implemented, 2026-06-26)

- [x] Read shared Project_PA loop/design context before implementation.
- [x] Run `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1`.
- [x] Confirm preflight returned `READY_FOR_BOUNDED_TICKET_LOOP`.
- [x] Select an actual existing Day 1 sold category: `Processed` from `BreadLoaf`.
- [x] Add `VillageCultureVisualController` as a read-only sales observer.
- [x] Add runtime visual root `PA_VillageCulture_Processed`.
- [x] Keep Day 1 start visual inactive.
- [x] Keep visual inactive immediately after sale.
- [x] Activate visual only on the next `DayPreparation`.
- [x] Show a one-time non-debug hint.
- [x] Add `PA_VillageCultureVisualValidator`.
- [x] Run VC-001A validation and required regressions.
- [x] Update `Docs/VillageCulture/VC-001A.md`.
- [ ] Human visual review: confirm the primitive processed-goods prep corner is readable and placed well.
- [ ] Future ticket: add `Raw`, `Utility`, and `Luxury` visual variants after the one-category rule is approved.

Implementation note:

- VC-001A uses existing `SalesLogManager` and `VillageChangeSignalController` data only. It does not add Save fields and does not change shop/economy/NPC purchase behavior.

## BASELINE-001 Bounded Ticket Loop Baseline Preparation (2026-06-26)

- [x] Verify Project_PA working path and Git root.
- [x] Record current branch and last commit.
- [x] Record `git status --short` and `git diff --stat`.
- [x] Confirm no Unity Editor process for Project_PA was detected during the check.
- [x] Update `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md`.
- [x] Create `Automation/LoopEngineering/State/crash-resolution.json`.
- [x] Record the user's D3D11 manual stability confirmation without claiming D3D12 is fixed.
- [x] Update `Automation/LoopEngineering/loop-policy.json` for D3D11-only bounded automation.
- [x] Update `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1` so valid crash resolution is distinguished from unresolved crash artifacts.
- [x] Re-run preflight after harness correction and confirm it remains blocked by dirty Git.
- [ ] User reviews A/B/C file groups in `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md`.
- [ ] User creates the local baseline checkpoint commit in GitHub Desktop.
- [ ] Rerun preflight after the local checkpoint commit.

Current status:

- `needs_human_review`
- Preflight is still `BLOCKED_BY_DIRTY_GIT` until the user creates or otherwise accepts the baseline checkpoint.

## AI Workflow Structure - 2026-07-09

Status: documentation pass complete; moves deferred.

- [x] Create `AI_WORKFLOW/` structure and operating documents (identity, rules, verification, logs, handoff, roadmap).
- [x] Rewrite root `AGENTS.md` as entry guide (old version archived).
- [x] Record deferred archive moves in `AI_WORKFLOW/00_START_HERE/DOCS_INDEX.md` section 7-B.
- [ ] Human: checkpoint commit including VC-001A work (blocks moves and loop automation).
- [ ] After clean baseline: execute deferred document moves (merge -> git mv -> reference updates in the same commit, per `AI_DOC_CLEANUP_PLAN.md` section 5/9).
- [ ] Build `AI_WORKFLOW/03_TASKS/TASK_QUEUE.md` (needs inputs listed in `AI_WORKFLOW/03_TASKS/README.md`).

## Visual Demo Integration Pass - 2026-07-12

Status: 구현·검증 완료. 사람 시각 확인 대기.

- [x] 채집 포인트 5곳 placeholder 큐브를 궤짝+작물+아이템 아이콘 빌보드로 드레싱 (`DemoVisualDressingController`, 런타임 전용).
- [x] 영업 간판 기둥/걸이대/랜턴 드레싱.
- [x] 광장 소품 보강: 분수 벤치 3, 화단 4, 가로등 2, 궤짝 더미/통 (렌더러 전용, NavMesh 무영향).
- [x] HUD 문구 한국어 통일 (`DayNightShopLoopController`, `DaytimeStockPrepPoint`) + 검증기 키워드 1줄 동기화.
- [x] Day 요약 본문 잘림(410/360) 수복 — 요약 상태 본문 810x418 확장.
- [x] 검증: dotnet build 0 오류 + 검증기 6종 통과 (DayNight/FinalRoute/Presentation/GatheringReview/CoreSlice/LongPlay, D3D11 batchmode).
- [x] `AI_WORKFLOW/09_FINAL_FABLE_SPRINT/` 문서 5종 작성.
- [ ] 사람: Editor Game view 에서 광장 드레싱 전경·한국어 폰트·F10 토글 확인.
- [ ] 다음: 채집 포인트 작물 구체를 `Item.model` 실제 모델로 교체 (런타임만으로 가능).
- [ ] 다음(승인 필요): Nature Pack 식생 정적 배치 에디터 툴 (씬 백업 + NavMesh 재베이크).
- [ ] 다음: Day 요약 "Village direction" 제목 한국어화 (+FinalRoute 검증기 1줄 동기화).

## Visual Demo Integration Pass v2 (실제 Game View 기준) - 2026-07-12 저녁

Status: Before/After 스크린샷 기준 재작업 완료. 회귀 3종 통과. 사람 시각 확인 대기.

- [x] 실제 플레이 카메라 Before/After 캡처 툴 (`PA_DemoViewCapture`) + before/after5 캡처.
- [x] 씬 저장 `Guide_*` 디버그 라벨 + SUPPLY/PRICE/SALE 스테이징 라벨 기본 숨김 (F10 토글로만).
- [x] 광장 베이스 플레이트(30x30)로 갈색 맨땅 제거 + 데크/러그/파빙/매트 오프셋 계층화.
- [x] 상단 목표 한 줄 + 좌측 퀘스트 체크리스트 패널 + 페이즈 스트립 좌측 이동.
- [x] Item 아이콘 6종 연결 → 핫바/쇼케이스 실제 아이콘.
- [x] 판매대 카운터/가격판, 쇼케이스 상품 5종, 간판 "코지 잡화점".
- [x] 회귀: FinalRoute(`paid=30G`) / DayNight / PanelLayout 통과.
- [ ] 사람: After 스크린샷(`Logs/DemoViewShots/after5_*.png`) vs 실기기 Game view 대조, 러그/파빙 가시성, 조명 톤 판단.
- [ ] 다음: 조명 커브(15시대 밝기) 조정 검토 — DayNightVisual 수정은 승인 필요.
- [ ] 다음: 하단 중앙 기존 갈색 플랫폼 정체 확인 및 정리.

## Visual Demo Integration Pass v3 Final Lock - 2026-07-13

- [x] `after_final2` 캡처 실행 및 실제 이미지 확인.
- [x] 첫 캡처의 검은 머티리얼 아티팩트 확인 후 코드 수정 없이 1회 재캡처.
- [x] `after_locked_20260713_002356.png`를 Final Locked Screenshot으로 확정.
- [x] 런타임/에디터 컴파일 오류 0 확인.
- [x] 핵심 검증기 5종 D3D11 batchmode PASS.
- [x] Final Lock 문서 5종 및 종료 기록 갱신.
- [ ] 사람: 실제 Editor Game View와 Final Locked Screenshot 일치, 한국어 폰트, 검은 머티리얼 미재현을 1회 확인.
- [ ] 발표 후: 정식 상점 메시 교체, HUD 스타일 통합, NPC 쇼핑 애니메이션 연결.

## Full Game Completion Phase 0 Sync - 2026-07-13

- [x] Task 001~085를 코드·데이터·Git·검증 증거로 재판정.
- [x] CURRENT_COMPLETION_MATRIX 생성.
- [x] TASK_QUEUE/DONE_TASKS와 오래된 검증 BLOCKED 기록 동기화.
- [x] 다음 Persistence 우선 스프린트 3개 선정.
- [x] Task 007 — 저장 스키마 현황 확정.
- [x] Task 011 — 실제 저장소 왕복 검증.
- [x] Task 018 — 가격 패널 진열 수량 표시.

## Castle Build Completion Track - 2026-07-15

- [x] S4 — Tier 0 외부 문 잠금과 Tier 1 실내 잡화점 해금 연결.
- [x] S4 — 플레이어가 읽는 간판/해금 패널/문 조명 전환.
- [x] S4 — 저장 v9의 `currentTier`에서 실내 해금 상태 재구성(스키마 변경 없음).
- [x] S4 스모크 — 잠금→해금→입장→실내 진열·가격→퇴장 PASS.
- [x] Task 034 — 구매/거절 일일 통계를 정산과 다음 날 준비 목표에 연결.
- [x] Day 1→Day 3 입력 경로 감사 — 결산 후 다음 날 전환 부재를 연결.
- [x] Day 1 결산 버튼→Day 2 아침, Day 2+ 정산 간판→다음 날 아침 D3D11 검증.
- [x] Task 039 — 기존 해변 채집/`IInteractable` 경로를 캐스팅·대기·Fish 획득 낚시로 교체하고 D3D11 검증.
- [x] Task 041 — 실제 어획 Fish→진열·가격→Fisher_01 구매→18G 수익·판매 통계 단일 왕복 D3D11 PASS.
- [x] Task 042 — 자동 낚시→판매 PASS와 사람이 직접 확인할 이동·대기·NPC 접근 절차를 스모크 체크리스트에 고정.
- [x] Task 043 — Crop/Farmland·광질 데이터 호환성 감사와 광질 우선 구현 경계 확정.
- [x] `quarry-mining` — 실제 Ore 2개 획득→1개 진열→Miner 15G 판매·v9 격리 저장/일일 리셋 D3D11 PASS.
- [x] 비주얼 도구체인 감사: Unity/URP/패키지/Cinemachine/Navigation/Rigging/Blender/Codex MCP/라이선스/캡처 기록.
- [x] Unity MCP 9.7.0 안정 태그/lock hash 고정, 프로젝트 범위 loopback 연결, Editor/씬/프리팹/Play Mode 직접 확인.
- [x] `VISUAL_TOOLCHAIN.md`, `ASSET_AND_TOOL_PROVENANCE.md`, `ART_DIRECTION.md`, `TRIPO_ASSET_AUDIT.md` 작성.
- [x] 중앙 원시 박스 상점을 기존 B01 노점 Visual로 교체하고 겹친 임시 충돌 제거. Shop/경제/저장/씬/프리팹 원본 무변경.
- [x] 그리드 기반 커스터마이징 P1/P2 — 기존 GridService/BuildManager/save/inventory/shop/interior 감사, 병렬 그리드 없이 상점 실내 배치 MVP 연결.
- [x] `PLACEMENT_SYSTEM_ARCHITECTURE.md`, `CUSTOMIZATION_ROADMAP.md`, `PLACEABLE_ASSET_GUIDE.md`와 1×1·다중 셀 배치/회전/이동/회수/저장, protected cell, 상호작용 면, NPC 경로 검사 구현·검증.
- [x] 저장 v10 — zone/definition/instance/cell/rotation/recovered/function 상태와 v9→v10 마이그레이션, 이동 ShopSlot 상품/가격 재로드 PASS.
- [x] P3 야외 `village.outdoor` 완료 — 47×47 공유 GridService zone, 도로·광장·출입구 212셀 보호, B09 외부 24칸 보관함의 9셀 footprint·전면 clearance·이동·안전 회수·v10 저장을 구현하고 D3D11 검증했다.
- [x] P4 ShopSlot 전면 approach 셀·NPC 예약·실제 NavMesh 도달성 완료 — 이동 선반 앞셀 회전, owner 예약, 완전 경로, 실제 15G 구매를 D3D11에서 확인했다.
- [x] B10 Cottage 분리·부유 문/벽 파츠를 원본 비파괴 방식으로 수복하고 실제 Play Mode 카메라·Tier 왕복으로 검증했다.
- [x] B05 작업대를 기존 Wood→Plank 가공·인벤토리·2×2 배치와 연결되는 최소 준비 기능 및 일관된 실루엣으로 최종화했다. 원본은 덮어쓰지 않았다.
- [x] 플레이어/NPC 보행을 같은 구도에서 캡처하고 접지·발미끄럼·Animator speed·Collider·NavMeshAgent를 외형 보존 우선으로 최종화했다.
- [ ] primitive 기반 채굴 표식은 최종 아트로 인정하지 않으며, 배치 MVP를 방해하지 않는 체크포인트에서 목적형 에셋으로 교체.
- [~] Task 089 농사 F1/F2 구현 — Seed→Crop→Wheat 참조, 낮 씨앗 주머니 2개, 농부 작업 지점 인근 고정 밭 2칸, 씨앗 1개 심기→성장 안내→가방 용량 확인→Wheat 3 수확을 연결했다. 컴파일/정적 계약 PASS, Unity 실플레이 확인은 대기한다.
- [~] Task 090 Day 2+ 운영 체크리스트 — 실제 낮 활동·판매 상품 2종·진열/가격·개점·당일 판매·정산 상태와 단계별 상단 목표를 0.5초마다 갱신한다. 컴파일/정적 계약 PASS, Unity 1920×1080 상태 전환/가독성 확인은 대기한다.
- [~] Task 091 가공 안전 트랜잭션 — 현재 8개 레시피 모두 결과 메타 스택/재료 소비 후 빈 슬롯을 먼저 검사하고, 공간 부족이면 재료를 차감하지 않는다. 컴파일/정적 계약 PASS, Unity 가방 3분기 확인은 대기한다.
- [ ] 승인 후 농사 F3 — 날짜 기반 성장과 밭/작물 상태 저장 마이그레이션.
- [ ] 결정 후 구현: 밤 영업 18~23시와 NPC 휴식 20시 이후의 손님 시간 창 확대.
- [ ] 최종 통합: 새 게임부터 Day 3 정산까지 사람 연속 플레이 + 저장 종료/재실행.
- [ ] 사람 확인: 1920x1080 Tier 1 해금 패널 가독성 및 기존 HUD 비겹침.
- [x] P5 확장 조명의 Unity 가짜 null 오류를 명시적 Unity null 검사로 수정했다.
- [x] P5 전용 D3D11 전체 검증 — Tier 0→4 zone/진열 한도/B05~B08 보상, 확장 NavMesh 완전 경로, 추가 ShopSlot 기능, Processed 테마, v10 저장·복원 PASS.
- [x] P5 동일 구도 `tier0_shop.png`/`tier3_expanded_shop.png`를 직접 비교하고 진행 원장 상태 문구와 테마 버튼 겹침을 보정했다.
- [x] ShopCustomization/EnterableShop/SaveRoundTrip/FinalDemoRoute 회귀 PASS.
- [x] Task 044 주민 의뢰 표시 설계 — `DESIGN_RESIDENT_REQUEST.md`에서 기존 Dialogue/Demand/전문가 레시피/Inventory/Friendship/일일 활동을 감사하고, Chef의 Wheat 3 요청을 첫 비퀘스트엔진 계약으로 확정했다.
- [~] Task 088 주민 재료 요청 구현 — Chef Wheat3, Blacksmith Ore2, Carpenter Wood2를 실제 담당 레시피에서 파생해 보유량 프롬프트→정확 차감→당일 완료 저장 문자열→친밀도 보상을 연결했다. Tailor의 잘못된 Bread 배정은 작업대 불일치로 제외한다. 컴파일/정적 계약 PASS, Unity 실제 전달·저장 왕복·UI 확인은 안전 캡처 경로 승인 뒤 완료한다.
- [x] Task 045 낮 활동 결과→재고 연결 문서 동기화 — 현재 6개 일일 재고 원천과 Fish 18G/Ore 15G 실제 낮→밤 왕복을 `DAYTIME_ACTIVITIES.md`와 게임 루프 표에 반영했다.
- [x] Task 046 카테고리별 판매 통계 조사 — 거래 1건/총 결제액, 최근 40건, `count×1000+revenue`, Processed 다음 날 변화와 런타임/저장 경계를 `VILLAGE_TREND.md`에 확정했다.
- [x] Task 047 트렌드 점수 데이터 설계 — Fish/생선구이/목제 가구의 정확한 판매 매핑, 캠핑 비활성, 점수 상한·일차/주간 창·동점 규칙을 `VILLAGE_TREND.md`에 확정했다.
- [x] Task 051 시설 해금 신호 표시 연결 — 성공 판매의 Village direction을 감사 앱의 다음 시설 후보 예고로 연결하고, 실제 해금 권한은 기존 Tier/감사 조건에 유지했다.
- [x] Task 052 이벤트 해금 후보 설계 — `해변 풍어제`의 판매 기반 해금·다음 날 행사·낮 낚시→밤 판매·정산·저장/승인 경계를 확정하고 독립 정적 검사를 통과했다.
- [x] Task 054 판매 통계 저장 확장 설계 — 현재 v10 다음의 v11로 최근 원거래·일차 판단·카테고리 판매·7일 명명 트렌드, 빈 기본값 마이그레이션, 소유권·복원·검증 경계를 `SAVE_SCHEMA.md`에 확정했다.
- [ ] 승인 대기: Task 055 v11 판매 통계 저장 구현 — `SaveData`/`SaveManager`와 최소 `SalesLogManager`/공용 명명 트렌드/검증기 범위를 작업 전 보고하고 사용자 승인 후 구현한다.
- [ ] 저장 문서 후속: Task 060 저장 버전 관리 정책 문서 — Task 054 v11 설계와 현재 v0→v10 체인을 기준으로 전용 정책을 동기화한다.
- [x] Task 023 희귀품/일반품 구분 — `일반품|희귀품 · 가격 파생값 · 품질 ×N.NN` 표시, 일반/희귀 FinalPresentation 캡처와 FinalDemoRoute 회귀 PASS.
- [x] Task 025 읽기 전용 추천 기준가 — `basePrice+quality` 기준, 일반 30G·고품질 186G, 현재가 자동 변경 없음, FinalPresentation/FinalRoute PASS.
- [x] Task 031 주민/관광객 계층 표시 — 현재 8명은 실제 일과표 기반 `[주민]`, 일과표 없는 향후 방문 손님은 `[관광객]` 폴백. 기본 머리 위 말풍선 태그와 D3D11 30G 회귀 PASS.
- [x] Task 024 테마 코너 설계 — 기존 `ShopSlot`과 placement footprint를 재사용해 같은 카테고리 4방향 인접 2칸 이상을 코너로 파생하고, 기존 상점 전체 테마·저장·구매 수학과 분리했다.
- [~] Task 086 상품 진열 테마 코너 구현 — 전용 D3D11에서 양성/음성 인접·품절/재진열·회수/이동·실제 판매 3건·Processed 마을 신호·v10 저장 재파생까지 PASS. 기존 ShopCustomization 회귀의 직접 `Camera.Render()`가 같은 네이티브 충돌을 두 번째로 재현해 전체 회귀와 기본 화면 라벨 가독성은 미완이다.
- [ ] 재개 전 필수: validator registry의 직접 `Camera.Render()` 사용처를 한 번에 감사해 안전한 공용 GameView 캡처 경로로 교체한다. 같은 직접 렌더 경로의 세 번째 Unity 실행은 금지한다.
- [x] Task 087 사용자 추가 장기 지침 1차 통합 — `TRIPO_ASSET_AUDIT.md`에 전수 실사용, 8분류, B06~B08 기능/격자, B11/B12 정적 역할, 원본·출처·라이선스·배포 게이트를 정합화했다.
- [~] Task 094 장기 최종화 정책/실제 정리 — 캐릭터 정체성 보존·Unity/Blender 경계·Placeable 온보딩을 장기 지침에 고정하고 Froggy Chair 런타임 생성 2곳을 제거했다. 컴파일/정적 계약 PASS, GameCamera 빈자리/초점 확인 대기.
- [ ] 사용자 증빙: C-01~C-09/walking/B01~B12의 Tripo 생성 계정·생성일·상업 이용 범위. 증빙 전 최종 배포 확정 금지.
- [ ] Froggy Chair: 플레이어 런타임 참조는 0. 최종 빌드 전 라이선스 증빙을 확보하거나 `Resources` 파생물을 보존 가능한 격리 경로로 이동/검증된 기존·자체 에셋으로 교체.
- [ ] Unity 재실행 안전 경로 승인 후 B06 Kitchen부터 동일 게임 카메라로 정면·2×2 통로·콜라이더·Bread 제작 피드백 최종화. 이어 B07 Forge, B08 Sewing 순서.
- [ ] B11 Fountain 광장 횡단 동선/물리 정합, B12 TradePort 해안 동선/물리 정합. 교역 기능은 Task 079 선행 조건 전 추가 금지.
- [x] 사용자 제공 Grid-Based Player Customization 지침 재대조 — 기존 P1~P5의 footprint·clearance·interaction·zone·회전·저장·NPC 접근은 구현/검증됨을 확인하고 Tripo/외부 에셋 온보딩 게이트만 문서에 보강했다. 집/마당/벽/상판 surface는 P6 백로그 유지.
- [~] Task 092 Raw 다음 날 변화 — 실제 Raw 판매를 기존 v10 카테고리 pending/active 계약에 연결하고, CC0 통나무·바위와 Project P.A. 간판 메시의 무충돌 `원자재 수거처`를 구현했다. Runtime/Editor/정적 계약 PASS, 안전 캡처 경로 승인 후 같은 GameCamera에서 당일 없음→다음 날 등장·간판 방향·스케일·ShopSlot/NPC 동선 비겹침을 확인한다.
- [~] Task 093 당일 낚시·가구 명명 트렌드 — Fish/생선구이/목제 가구의 실제 성공 판매만 `낚시 생활|가구 문화`로 집계해 기존 결산 카테고리 아래에 표시한다. Runtime/Editor 오류 0, 정적 계약 18개 PASS. 안전 캡처 경로 승인 후 결산 가독성과 생선구이·목제 가구 실제 판매를 확인한다.

## Task 095 가구 보조 루프 단계 안내 — 2026-07-17

- [x] 기존 `Recipe_Furniture`·Plank3·BasicWorkbench·Tier2·ShopSlot·당일 SalesLog 경로를 단일 권위로 재사용.
- [x] Day 4+ 체크리스트에 현재 잠금 또는 다음 실제 행동 한 줄 연결.
- [x] Tier 1/2 누적 매출은 실제 `TierDefinition`에서 읽고 10,000G/100,000G 값을 변경하지 않음.
- [x] 지급·강제 승급·자동 제작/진열/판매 없는 읽기 전용 계약 및 Runtime/Editor 오류 0 확인.
- [ ] 안전 Unity 경로 승인 뒤 Tier 0/1/2, Plank 0~3, 제작품 보유, 진열, 당일 판매 문구가 1920×1080 패널에서 잘리지 않는지 확인.
- [ ] 같은 실행에서 B05→Plank3→목제 가구 제작→진열·가격→실제 구매→정산 `가구 문화` 전체 왕복 확인.
- [ ] 100,000G가 장기 플레이 시간과 맞는지 사용자/기획 판단. 승인 없이 수익·Tier 임계치를 낮추지 않음.

## Task 096 새 게임·이어하기 제품형 진입 — 2026-07-17

- [x] 기존 첫날 Canvas 맨 앞에 PROJECT P.A. 타이틀·핵심 판타지·새 게임/이어하기 선택 연결.
- [x] `ExistsAsync` 기반 저장 유무 확인과 저장 없음 비활성 상태 구현.
- [x] 이어하기는 기존 v10 `LoadGameAsync`→`RestoreSavedSession`, 새 게임은 기존 이름 등록 이후 흐름 재사용.
- [x] 로드 실패 복귀, 결산 닫기 버튼, F5/F9 보존 및 저장 삭제·자동 로드·스키마 무변경 확인.
- [x] Runtime/Editor 오류 0, 상태 전이 15개와 읽기 전용 호출 계약 PASS.
- [ ] 안전 Unity 경로 승인 뒤 저장 없음/있음 타이틀 화면, 버튼 가독성, 새 게임→이름 등록을 확인.
- [ ] 격리 v10 세이브로 이어하기→이름/지도/일차/시간/돈/Tier/인벤토리/진열/배치 실제 복원 확인.

## Task 097 Pause 메뉴 제품 제어 — 2026-07-17

- [x] 계속하기·게임 저장·저장본 불러오기·저장 후 종료 버튼 연결.
- [x] Pause 전 `Time.timeScale`·커서 잠금·표시 상태 저장과 Resume/파괴 시 복구.
- [x] 저장 존재 기반 Load 비활성, 비동기 중복 입력 잠금, 예외 상태 문구 구현.
- [x] 기존 `SaveManager` 공개 API만 사용하고 저장 성공 뒤에만 `Application.Quit` 호출.
- [x] Runtime/Editor 오류 0, 정적 계약 11/11, `git diff --check` PASS.
- [ ] 안전 Unity 경로 승인 뒤 타이틀에서 ESC 차단, 플레이 중 ESC 열기/닫기, 네 버튼 클릭을 확인.
- [ ] 1920×1080 가독성, 저장 없음/있음 Load 상태, 실제 빌드의 저장 후 종료와 재실행 이어하기를 확인.

## Task 098 Day 7 첫 주 완주 — 2026-07-17

- [x] Day 7 Settlement 단일 감지와 `첫 주 운영 완료` 모달 연결.
- [x] 기존 플레이어 이름·매출·돈·Tier·평판·Day 7 정산 읽기 전용 요약.
- [x] Day 7 저장→기존 다음 날 전환→Day 8 재저장 경로 구현.
- [x] 저장 성공 뒤 종료, 저장/전환 실패 복구, 배경 간판 진행 차단 구현.
- [x] timeScale/커서 보존·복구와 Runtime/Editor 오류 0, 정적 계약 12/12 PASS.
- [ ] 안전 Unity 경로 승인 뒤 Day 7 정산에서 모달 단일 표시, 1920×1080 줄바꿈/버튼 가독성을 확인.
- [ ] 두 버튼으로 Day 8 저장/계속과 실제 빌드 종료→타이틀 이어하기→Day 7 모달 재진입을 확인.

## Task 099 실제 입력 기반 시작 조작 안내 — 2026-07-17

- [x] 기존 `StartupStep`을 재사용해 Phone 지급 뒤 조작 안내 단계 연결.
- [x] WASD/방향키·Space·I/P/C·1~9/휠·좌클릭/R/M/X·F5/F9/ESC를 실제 입력 권위와 대조.
- [x] 안내가 상태를 변경하지 않고 기존 보급품→도착→Day 1 흐름으로 이어지는 계약 확인.
- [x] Runtime/Editor 오류 0, 입력·단계·기존 흐름 기능 계약 11/11 PASS.
- [ ] 안전 Unity 경로 승인 뒤 1920×1080에서 다섯 줄 본문이 잘리지 않고 키 그룹이 읽히는지 확인.
- [ ] `조작 확인` 클릭이 보급품→도착→첫날 시작으로 한 단계씩 이어지고 타이틀/이어하기가 그대로 동작하는지 확인.

## Task 100 타이틀 게임 종료 경로 — 2026-07-18

- [x] 기존 시작 Canvas/helper에 타이틀 전용 `게임 종료` 버튼 연결.
- [x] 종료/이어하기/새 게임 3버튼 배치와 비Title·Day 1 결산 숨김 처리.
- [x] 이어하기 로딩 중 종료 잠금, 실제 빌드 Quit, Editor 안전 안내 구현.
- [x] 종료 경로의 저장 비침범과 기존 타이틀/조작 안내/진행 흐름 보존 확인.
- [x] Runtime/Editor 오류 0, 상태 계약 14/14 PASS.
- [ ] 안전 Unity 경로 승인 뒤 1920×1080에서 세 버튼이 겹치지 않고 저장 없음/있음 양쪽에서 종료가 활성인지 확인.
- [ ] Windows 빌드에서 타이틀 `게임 종료` 클릭 시 프로세스가 정상 종료되는지 확인.

## Task 101 기존 저장 보호 새 게임 확인 — 2026-07-18

- [x] 타이틀 저장 존재 확인이 끝날 때까지 새 게임 잠금.
- [x] 저장 없음은 기존 이름 등록으로 직행하고 저장 있음은 덮어쓰기 경고 표시.
- [x] 경고 승인→이름 등록, 취소→타이틀 복귀·저장 상태 재조회 연결.
- [x] 확인 화면 저장/삭제 비침범과 기존 이어하기·종료·조작 안내 보존 확인.
- [x] Runtime/Editor 오류 0, 저장 보호 상태 계약 15/15 PASS.
- [ ] 안전 Unity 경로 승인 뒤 저장 없음/있음 각각의 실제 버튼 분기와 타이틀 재조회 확인.
- [ ] 1920×1080 경고 문구·두 버튼 가독성 및 새 게임 뒤 첫 F5에서 기존 슬롯을 덮어쓰는 실제 제품 동작 확인.

## Task 102 B09 외부 창고 실제 사용 UI — 2026-07-18

- [x] 메인 씬 `StorageUI` 0개와 B09 `Interact→OpenBox` 무화면 단절 확인.
- [x] 기존 `PA_UIRoot`에 `StorageUI` 단일 런타임 폴백 연결.
- [x] B09 24칸 6×4, 실제 아이콘·수량·품질·유효 가격 표시.
- [x] 선택 핫바 스택의 메타를 복사하고 성공 뒤 해당 슬롯만 정확히 1개 차감.
- [x] 클릭 회수·가방 가득 참 보존·커서 복구·ESC/Pause 우선순위·다른 패널 상호배타 구현.
- [x] Runtime/Editor 오류 0, 창고 기능·v10 저장 비침범 계약 14/14 PASS.
- [ ] 안전 Unity 경로 승인 뒤 B09 문 앞 `[Space]`→24칸 화면→보관/회수→ESC를 실제 확인.
- [ ] 1920×1080 아이콘/한국어/버튼 가독성과 보관 전후 v10 저장→로드 수량·품질·가격 왕복 확인.

## Task 103 제작 도감·작업대 제작 UI 제품 흐름 — 2026-07-18

- [x] 기존 8개가 모두 작업대 전용이라 C 패널이 비어 있던 원인 확인.
- [x] `[C]` 전체 레시피 도감과 필요한 작업대 표시, 원격 제작 차단.
- [x] B05~B08 작업대 컨텍스트별 실제 아이콘·출력·전체 재료 보유/필요량·잠금 표시.
- [x] 기존 `CraftingService.TryCraft` 단일 권위로 제작하고 성공/실패 뒤 카드와 상태 갱신.
- [x] 전체 화면 입력 차단, 인벤토리·스마트폰·창고 상호배제, 커서 복원과 ESC 우선순위 구현.
- [x] Runtime/Editor 오류 0, 제작 기능 계약 16/16, `git diff --check` PASS.
- [ ] 안전 Unity 경로 승인 뒤 `[C]` 도감 8개와 B05~B08 `[Space]` 필터/제작/실패 피드백을 실제 클릭으로 확인.
- [ ] 1920×1080 카드 세 줄·아이콘·스크롤·상태 문구와 ESC/다른 패널 전환을 같은 화면에서 확인.

## Task 104 Tripo 장기 정책·B11 분수 충돌 정합 — 2026-07-18

- [x] 기존 Tripo 전수 감사·P1~P5 Placeable·출처 문서를 첨부 Grid/에셋 지침과 대조.
- [x] 에셋별 1~8 판정, 캐릭터 정체성 보존, 기능 가구의 footprint/clearance/interaction·저장, 원본/라이선스 게이트를 ADR로 고정.
- [x] B11 6×6 사각 루트 BoxCollider와 원형 Visual 불일치 확인.
- [x] 실제 Visual mesh별 비볼록 정적 MeshCollider 구성 뒤에만 루트 Box를 끄는 안전한 런타임 보정 구현.
- [x] 기존 캡슐형 carving obstacle, B11 모델·재질·배치, 메인 씬·프리팹·원본 FBX/텍스처 보존.
- [x] Runtime/Editor 오류 0, 물리/폴백/중복/금지 경계 계약 12/12, `git diff --check` PASS.
- [ ] 안전 Unity 경로 승인 뒤 분수 둘레 네 방향 이동, 보이는 석재 경계, 벤치 접근과 NPC 우회를 실제 확인.
- [ ] 기존 GameCamera 구도의 Before와 같은 조건으로 After를 만들고 스케일·동선·충돌 정합이 명확히 개선됐는지 판정.

## Task 105 20~23시 영업 손님 흐름 복구 — 2026-07-18

- [x] 18~23시 영업과 19~20시 주민 Rest 시작의 시간 창 단절 확인.
- [x] 실제 phase를 바꾸지 않는 Rest 전용·멱등 방문 override 구현. Work·Sleep 제외.
- [x] Tier 0 외부 초대의 성공·실패·timeout·폐점 lease와 원위치/Shop/Rest 복구 구현.
- [x] Tier 1 실내 초대의 시작 실패·구매 후 퇴장 복구와 기존 동시 손님 상한 보존.
- [x] 기존 Day 1, `NpcController` FSM, 구매 수학, schedule 에셋, 저장, 씬·프리팹·패키지 비침범.
- [x] Runtime/Editor 오류 0, 기능 계약 18/18, `git diff --check` PASS.
- [ ] 안전 Unity 경로에서 18:30/20:30/22:30 외부·실내 손님 유입과 기존 동시 손님 상한 확인.
- [ ] 23:00 폐점과 timeout 뒤 임시 손님이 남지 않고 원래 위치·Shop·Rest로 복귀하는지 확인.

## Task 106 Processed 다음 날 변화 실제 에셋 전환 — 2026-07-18

- [x] `PA_VillageCulture_Processed`의 원시 큐브 5개·런타임 재질 구성 확인.
- [x] `Building_B05_Workbench` 래퍼 대신 `prefab/Visual`만 복제해 실제 실루엣 사용.
- [x] 기존 B05 목재→판재 준비 키트와 Project P.A. 간판 `가공 준비대` 결합.
- [x] Y 180° 작업면 보정·0.58배 광장 점유 범위 적용.
- [x] Workbench·Collider·Rigidbody·NavMeshObstacle·MonoBehaviour·추가 Light 제거, 가짜 기능/충돌 차단.
- [x] Raw 상호배타·pending→다음 날·v10 category 저장/복원과 B05 원본/기능/배치 비침범.
- [x] Runtime/Editor 오류 0, 기능 계약 18/18, `git diff --check` PASS.
- [ ] 안전 Unity 경로에서 같은 GameCamera의 판매 전/다음 날 스케일·정면·간판·상점/분수 겹침을 확인.
- [ ] 플레이어와 NPC가 시각 루트를 통과할 수 있고 기능 작업대로 오인되지 않는지 확인.

## Task 107 Utility 다음 날 공구 수리대 변화 — 2026-07-27

- [x] 판매 가능한 `Item_12_ToolSet` Utility와 `Recipe_ToolSet`→Forge→B07 실제 데이터 경로 확인.
- [x] Utility 성공 판매를 기존 pending→다음 DayPreparation→v10 category 문자열 계약에 연결.
- [x] `Building_B07_BlacksmithForge` 래퍼 대신 `prefab/Visual`만 0.44배 비충돌 시각으로 복제.
- [x] Project P.A. 간판 `공구 수리대`와 Utility 전용 다음 날 힌트 연결.
- [x] Workbench·Collider·Rigidbody·NavMeshObstacle·MonoBehaviour·Light 제거와 Processed/Raw 상호배타 구현.
- [x] Runtime/Editor 오류 0, 기능 계약 23/23 PASS.
- [ ] 안전 Unity 경로에서 Utility 판매 당일 없음→다음 DayPreparation 등장과 v10 저장/복원을 실제 확인.
- [ ] 같은 GameCamera에서 B07 축소 시각의 스케일·정면·간판·상점/분수 겹침, 플레이어/NPC 동선과 실제 기능 Forge와의 구분을 확인.

## Task 108 Luxury 다음 날 공예 전시대 변화 — 2026-07-27

- [x] 판매 가능한 `Item_11_Furniture`/`Item_13_Clothes` Luxury와 기존 Furniture/Sewing 레시피 경로 확인.
- [x] Luxury 성공 판매를 기존 pending→다음 DayPreparation→v10 category 문자열 계약에 연결.
- [x] `Building_B08_SewingTable` 래퍼 대신 `prefab/Visual`만 0.48배 비충돌 시각으로 복제.
- [x] Project P.A. 간판 `공예 전시대`와 Luxury 전용 다음 날 힌트 연결.
- [x] Workbench·Collider·Rigidbody·NavMeshObstacle·MonoBehaviour·Light 제거와 Processed/Raw/Utility 상호배타 구현.
- [x] Runtime/Editor 오류 0, 수정된 기능 계약 27/27 PASS.
- [ ] 안전 Unity 경로에서 Luxury 판매 당일 없음→다음 DayPreparation 등장과 v10 저장/복원을 실제 확인.
- [ ] 같은 GameCamera에서 B08 축소 시각의 스케일·정면·간판·상점/분수 겹침, 플레이어/NPC 동선과 실제 기능 Sewing Table과의 구분을 확인.

## Task 109 채용 후보 제품 흐름 — 2026-07-27

- [x] 후보 8명의 `spawnPrefab`이 모두 비어 실제 고용이 실패하는 원인 확인.
- [x] 명시 프리팹 우선과 같은 전문 분야의 기존 Producer/Specialist 원본 폴백 구현.
- [x] `NpcController`·실제 SkinnedMesh 요구와 런타임 채용 clone 원본 재사용 차단.
- [x] 후보·티어·원본·잔액을 비용 차감 전에 검사하고 기존 `EconomyService.TrySpend` 권위 보존.
- [x] 신규 채용/v10 복원 공통 resolver와 profile/specialty/schedule/dialogue/후보별 친밀도 주입.
- [x] Specialist의 WorkbenchType에 맞는 기존 `Resources/Recipes`만 할당.
- [x] 첫 열기 카드 생성과 소개·한글 역할·비용·잠금·성공/실패 UI 피드백 구현.
- [x] Runtime/Editor 오류 0, 수정된 정적 계약 36/36, `git diff --check` PASS.
- [ ] 안전 Unity 경로에서 충분/부족 잔액, Producer/Specialist 실제 채용과 역할 행동·중복 차단을 확인.
- [ ] 저장 후 재실행 복원과 스마트폰 1920×1080 카드/피드백 가독성을 확인.

## Task 110 첫 주 채용 성장 목표 — 2026-07-27

- [x] Day 5의 추상적인 채용 준비 문구를 실제 P.A. Phone 채용 행동으로 교체.
- [x] Day 5 이후 미고용 상태의 낮 목표와 운영 체크리스트 안내 구현.
- [x] 고용 뒤 실제 후보 이름·한글 역할·총 인원수를 체크리스트 완료 상태로 표시.
- [x] `HiringService.OnHired` 구독/해제로 LongPlay 목표 즉시 갱신.
- [x] 후보 roster를 실제 `GetHiredCandidates()`에서 이름순으로 결정론 생성.
- [x] Day 7 첫 주 결산에 총 인원·최대 3명 roster·`외 N명` 요약 추가.
- [x] Day 1~4와 기존 생활 활동·상품 2종·진열/가격·개점·판매/정산 체크리스트 보존.
- [x] Runtime/Editor 오류 0, 정적 계약 29/29 PASS.
- [ ] 안전 Unity 경로에서 Day 5 미고용→채용 성공 직후 목표/체크리스트 전환을 실제 확인.
- [ ] Day 7 첫 주 결산 roster 일치와 1920×1080 긴 이름·역할 가독성을 확인.

## Task 111 생산자 납품 원자 거래 — 2026-07-27

- [x] 기존 돈 선차감→가방 실패→NPC 상품 삭제 유실 경로 확인.
- [x] 메타 일치 스택/빈 슬롯의 전량 수용을 변경 없이 검사하는 `Inventory.CanAddInstance` 구현.
- [x] `AddInstance` 실패 시 기존 슬롯과 전달 인스턴스가 부분 변경되지 않는 원자성 복구.
- [x] Producer의 가방 연결/전량 공간 확인을 `TrySpend`보다 앞으로 이동.
- [x] 가방 가득 참·잔액 부족 시 결제 없음/NPC 재고 유지.
- [x] 결제 뒤 예상 밖 추가 실패의 기존 Economy 전액 환불/NPC 재고 유지.
- [x] 성공 시 원본 ItemInstance 메타 이전 뒤에만 NPC 재고 제거.
- [x] 성공·공간/잔액 보류·환불을 기존 NPC 말풍선으로 표시.
- [x] Runtime/Editor 오류 0, 정적 거래 계약 30/30, `git diff --check` PASS.
- [ ] 안전 Unity 경로에서 메타 불일치 스택까지 가득 찬 가방의 돈·양쪽 재고 무변경을 실제 확인.
- [ ] 공간 확보 뒤 같은 생산물의 정확 수량/비용 납품과 성공/보류 말풍선을 확인.

## Task 112 2주차 운영 캠페인 — 2026-07-27

- [x] Day 8+ 일반 반복 문구와 Day 8 전환 뒤 실제 성장 목표 단절 확인.
- [x] Day 8~14 보관·가공·채용·상품 구성·Tier·마을 변화·주간 다양화 계획 추가.
- [x] 실제 B09 보관량과 당일 Processed/카테고리/상품 판매 기록 판정.
- [x] 실제 채용 roster, Tier 1, 활성 다음 날 마을 변화 판정.
- [x] 2주차 자동 온보딩 보급 미추가와 직접 채집/생산자/가공/정리 안내.
- [x] Day 1~7·Day 7 완주/Day 8 저장 전환·기본 생활–상점 체크리스트 보존.
- [x] 경제·인벤토리·판매·제작·채용·Tier·저장 권위 비침범.
- [x] Runtime/Editor 오류 0, 정적 계약 40/40, `git diff --check` PASS.
- [ ] 안전 Unity 경로에서 Day 7 저장→Day 8 시작과 Day 8~14 대표 목표의 0.5초 완료 전환 확인.
- [ ] 1920×1080에서 2주차 목표/체크리스트가 기존 HUD와 겹치거나 잘리지 않는지 확인.

## Task 113 Tripo 재감사·B12 항구 충돌 방지 — 2026-07-27

- [x] 첨부 Grid/Tripo 지침을 기존 8분류·Placeable·출처 ADR과 재대조.
- [x] FBX 174/OBJ 150/GLB 0/Blend 0과 Nature Pack 외 고유 FBX 24개 재집계.
- [x] C-01~C-09 `.meta`의 `tripo_node_*`, B01~B12 제작 문서/GUID 사용처를 출처 추정 근거로 기록.
- [x] B12 10×5m 물리와 실제 약 3.63×1.96m Visual 차이 및 box형 carving obstacle 재발 경로 확인.
- [x] 활성 map/legacy B12의 Visual 로컬 bounds 8모서리 계산 구현.
- [x] 명백히 큰 루트 BoxCollider/NavMeshObstacle의 축소 전용 정합과 메시 실패 보존 구현.
- [x] B12를 정적 Protected/Developer 세계 에셋으로 유지하고 교역/Placeable/가짜 접근점 미추가.
- [x] Runtime/Editor 오류 0, 정적 계약 20/20, 대상 `git diff --check` PASS.
- [ ] 안전 Unity 경로에서 기존 10×5m 투명 벽 영역 통과와 보이는 부두 경계 정지를 확인.
- [ ] NPC carving 우회와 동일 GameCamera Before/After를 확인.
- [ ] 최종 배포 전 B12를 포함한 Tripo 개별 생성 계정·생성일·상업 이용 증빙 확보.

## Task 114 첫 달 운영 캠페인·Day 30 완주점 — 2026-07-27

- [x] Day 14 이후 일반 반복 문구 공백 확인.
- [x] Day 15~30 보관·가공·고용·상품 구성·Tier·마을 변화 기반 운영 계획 16개 등록.
- [x] 각 날짜에 기존 런타임 상태만 읽는 첫 달 체크리스트 목표 연결.
- [x] Day 30 Settlement 첫 달 완주 모달과 누적 성과/당일 정산 요약 구현.
- [x] 저장 후 Day 31 계속과 저장 성공 뒤 종료 분기 연결.
- [x] Day 7 자동 보급 종료·첫 주 완주·Day 8 저장 전환 보존.
- [x] 저장 스키마·경제·판매·제작·채용·Tier·마을 변화 권위 비침범.
- [x] Runtime/Editor 순차 빌드 경고 0·오류 0, 정적 계약 56/56, 대상 `git diff --check` PASS.
- [ ] 안전 Unity 경로에서 대표 Day 15~30 목표 완료 전환과 Day 30 모달 표시를 확인.
- [ ] Day 30 저장→Day 31→재저장, 저장→종료→이어하기 두 분기를 격리 저장으로 확인.
- [ ] 1920×1080에서 첫 달 요약·버튼·기존 HUD 겹침과 잘림을 확인.

## Task 115 Tier 1 대장간·철제 도구 첫 달 가치사슬 — 2026-07-27

- [x] Day 15~30 목표를 BuildingData·설계도·레시피·상품·Tier 요구까지 역추적.
- [x] B07 BuildingData/설계도/ToolSet 레시피/ToolSet 상품은 Tier 1인데 배치만 Tier 3인 불일치 확인.
- [x] B07 카탈로그 태그·최소 Tier·장부 보상을 Tier 1에 정합.
- [x] B05 starter, B06 Tier 2, B08 Tier 3, Tier 1=10,000G/Tier 2=100,000G 보존.
- [x] 중복 설계도 방지: 보유 또는 배치 상태면 재지급하지 않음.
- [x] 활성 B07 배치를 읽는 비변경 조회 연결.
- [x] Day 23을 활성 B07+정확한 철제 도구+다른 상품 1종 판매로 교체.
- [x] Day 24를 활성 B07+정확한 철제 도구+Processed 1건 판매로 교체.
- [x] 빈 진열대 두 칸 회수와 Plank1+Ore4→IronBar2→ToolSet1 경로 안내.
- [x] Day 15=16,000G→Day 30=31,000G→Day 31 이후 단조 증가 표시 목표로 역행 제거.
- [x] Runtime/Editor 순차 빌드 오류 0, 기존 CS8785/CS0414만 유지.
- [x] 실행 가능 소스 계약 39/39, 대상 `git diff --check` PASS.
- [ ] 안전 Unity 경로에서 Tier 1 장부 첫 열기에 B05/B07 설계도가 함께 지급되는지 확인.
- [ ] 빈 진열대 두 칸 회수→B07 3×2 배치, 전면 접근·콜라이더·NavMesh 통로 확인.
- [ ] Wood/Ore 확보→Plank/IronBar/ToolSet 제작→ToolSet+일상 상품/Processed 판매 실제 왕복 확인.
- [ ] Day 23/24 체크리스트 즉시 전환과 1920×1080 문구 가독성 확인.

## Task 116 안전 GameView 캡처 기반 1차 전환 — 2026-07-27

- [x] `Assets/Editor` 직접 `camera.Render()` 실제 호출 12곳 전수 감사.
- [x] ThemeCorner의 일반 GameView `ScreenCapture` 흐름을 공용 `PA_SafeGameViewCapture`로 추출.
- [x] 1920×1080 해상도, Canvas/TMP 갱신, 안정화 대기, 새 PNG freshness/최소 크기 판정 구현.
- [x] camera target/transform/orthographic/FOV/culling/viewport와 이전 화면 상태 `finally` 복원.
- [x] 알려진 두 번째 충돌 지점 `PA_ShopCustomizationValidator`를 공용 비동기 캡처로 전환.
- [x] Task 115용 `PA_ShopProgressionUnlockValidator`의 Tier 0/Tier 3 캡처 2회를 공용 경로로 전환.
- [x] 대상 세 파일의 실제 직접 `camera.Render()` 호출 0 확인.
- [x] Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414 경고 2), 계약 28/28, 대상 diff 검사 PASS.
- [ ] 남은 직접 렌더 10곳을 파일 수 경계에 맞춰 후속 분할 전환.
- [ ] 저장소 전체 실제 직접 `camera.Render()` 호출 0을 정적으로 확인.
- [ ] 사람 승인 뒤 D3D11 격리 GameView 캡처 1회로 PNG와 상태 복원을 확인.
- [ ] ShopCustomization과 ShopProgression을 순차 실행해 이전 네이티브 충돌이 재발하지 않는지 확인.

## Task 117 안전 GameView 캡처 기반 2차 전환 — 2026-07-27

- [x] 남은 직접 렌더 10곳에서 VillageCulture/CustomerPanelLayout/FinalPresentation 3개를 2차 대상으로 확정.
- [x] 세 검증기를 중복 실행 방지 `Task` 가드와 캡처 완료 대기 async/await 순서로 전환.
- [x] VillageCulture의 Day 1/판매 당일/다음 날 캡처 3장을 공용 GameView 경로로 전환.
- [x] CustomerPanelLayout의 1920×1080 패널 캡처를 공용 GameView 경로로 전환.
- [x] FinalPresentation의 시장/일반 가격/희귀 가격/NPC/감사/정산 6장 캡처를 공용 GameView 경로로 전환.
- [x] 시장 마커, FOV 46, 전체 레이어, 1920×1080 구도와 기존 캡처 실패 정책/출력 목록 보존.
- [x] 세 대상의 `RenderTexture`·`ReadPixels`·실제 직접 `camera.Render()` 호출 0 확인.
- [x] Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414 경고 2), 교정 계약 38/38, 대상 diff 검사 PASS.
- [x] 저장소 잔여 직접 렌더를 Character/Cottage/DemoView/GatheringShop/OutdoorPlacement/ShopEvolution/Workbench 7곳으로 축소.
- [ ] 남은 직접 렌더 7곳을 파일 수 경계에 맞춰 후속 분할 전환.
- [ ] 저장소 전체 실제 직접 `camera.Render()` 호출 0을 정적으로 확인.
- [ ] 사람 승인 뒤 D3D11 격리 GameView 캡처 1회와 VillageCulture/CustomerPanel/FinalPresentation 순차 검증.

## Task 118 안전 GameView 캡처 기반 3차 전환 — 2026-07-27

- [x] 남은 직접 렌더 7곳에서 DemoView/GatheringShop/OutdoorPlacement 3개를 3차 대상으로 확정.
- [x] DemoView를 중복 실행 방지 `Task` 가드와 공용 GameView 캡처 완료 대기 흐름으로 전환.
- [x] 실내 11초/외부 4.5초 준비, 실제 추적 카메라, 2560×1440 출력 보존.
- [x] GatheringShop의 낮 채집→수집 후→밤 상점→고객 반응→다음 날 정산 5개 1920×1080 캡처 전환.
- [x] OutdoorPlacement의 baseline/final 동일 직교 구도 2개 1280×720 캡처와 PNG 최소 크기 판정 보존.
- [x] 세 검증기에서 캡처 중 `CameraController` 동결과 `finally` 복원 구현.
- [x] 세 대상의 `RenderTexture`·`ReadPixels`·실제 직접 `camera.Render()` 호출 0 확인.
- [x] Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414 경고 2), 계약 35/35, 대상 diff 검사 PASS.
- [x] 저장소 잔여 직접 렌더를 Character/Cottage/ShopEvolution/Workbench 4곳으로 축소.
- [ ] 남은 직접 렌더 4곳을 파일 수 경계에 맞춰 후속 분할 전환.
- [ ] 저장소 전체 실제 직접 `camera.Render()` 호출 0을 정적으로 확인.
- [ ] 사람 승인 뒤 D3D11 격리 GameView 캡처 1회와 전환된 검증기 순차 실행.

## Task 119 안전 GameView 캡처 기반 4차 전환 — 2026-07-27

- [x] 남은 직접 렌더 4곳에서 Character/Cottage/Workbench 3개를 4차 대상으로 확정.
- [x] Character의 소스 lineup과 runtime idle/walk를 단일 `Task` 가드·await GameView 흐름으로 전환.
- [x] Character의 1600×900 구도, 0.35초 준비·0.6초 이동·0.25초 종료와 접지/보행 판정 보존.
- [x] Cottage의 전경·4방향·최종·runtime 7개 1920×1080 캡처와 renderer 격리 복원 보존.
- [x] Workbench의 감사/최종 4방향과 runtime baseline/final 1920×1080 캡처 전환.
- [x] Workbench의 접근 방향·콜라이더·Wood→Plank 실제 제작·피드백 판정 보존.
- [x] 실제 게임 카메라 캡처 중 `CameraController` 동결과 `finally` 복원 구현.
- [x] 세 대상의 `RenderTexture`·`ReadPixels`·실제 직접 `camera.Render()` 호출 0 확인.
- [x] Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414 경고 2), 계약 42/42, 대상 공백 검사 PASS.
- [x] 저장소 잔여 직접 렌더를 `PA_ShopEvolutionVisualFinalizer` 1곳으로 축소.
- [ ] 마지막 ShopEvolution 직접 렌더를 공용 GameView 경로로 전환.
- [ ] 저장소 전체 실제 직접 `camera.Render()` 호출 0을 정적으로 확인.
- [ ] 사람 승인 뒤 D3D11 격리 GameView 캡처 1회와 전환된 검증기 순차 실행.

## Task 120 안전 GameView 캡처 기반 최종 전환 — 2026-07-27

- [x] 마지막 `PA_ShopEvolutionVisualFinalizer` 직접 렌더를 최종 전환 대상으로 확정.
- [x] B02~B04 4방향 소스 감사 12장을 중복 실행 가드·await GameView 흐름으로 전환.
- [x] runtime baseline과 Tier 1~3 after를 단일 `Task` 가드로 순차 처리.
- [x] 기존 1600×900, 초기 4초, 단계별 0.75초, orthographic size 6.6, 파일명과 구도 보존.
- [x] 실제 게임 카메라의 `CameraController`·`clearFlags`를 `finally`에서 복원.
- [x] Tier 전후 `ShopCustomizationController.WriteSaveFields` JSON 동등성 판정 보존.
- [x] 대상 `RenderTexture`·`ReadPixels`·실제 직접 `camera.Render()` 호출 0 확인.
- [x] 저장소 전체 실제 직접 `camera.Render()` 호출 0 정적 확인.
- [x] Runtime 경고/오류 0, Editor 오류 0(기존 CS8785/CS0414 경고 2), 계약 36/36 PASS.
- [ ] 사람 판단 뒤 D3D11 격리 GameView PNG 1회로 freshness·파일 크기·가독성·카메라/화면 복원 확인.
- [ ] 격리 캡처 성공 뒤 ShopEvolution부터 전환된 검증기를 순차 실행.

## Task 121 Day 31~45 두 번째 달 진입 캠페인 — 2026-07-27

- [x] Day 30 이후의 일반 장기 운영 안내를 다음 단일 플레이 루프 단절로 확정.
- [x] `LongPlayProgressionController`에 Day 31~45 계획 15개와 두 번째 달 제목/캠페인 범위 추가.
- [x] `PlayableDayScenarioController`에 Day 31~45 실제 상태 완료 판정 15개 연결.
- [x] 보관 10/12개, Processed 2/3건, 지원 인력 3명, 상품 3/4종, 3카테고리, B07/ToolSet, 활성 마을 변화, 영업 전 4상품 준비 판정.
- [x] 기존 목표 수식을 공용 읽기 API로 사용해 Day 31 32,500G→Day 45 53,500G 보존.
- [x] Day 1~30·Day 30 완주 UI·Tier 2 100,000G·B06 Tier 2·B08 Tier 3·저장 스키마 비침범.
- [x] Runtime/Editor 오류 0, 기존 CS8785/CS0414 경고만 확인.
- [x] Day 31~45 캠페인 정적 계약 23/23 PASS.
- [ ] 사람 판단 뒤 안전 Unity 경로에서 Day 30 저장→Day 31 시작과 대표 Day 35/40/45 상태 전환 확인.
- [ ] 1920×1080 상단 목표·체크리스트 가독성과 Day 46 기존 장기 운영 폴백 확인.

## Task 122 Day 46~76 Tier 2 성장 캠페인 — 2026-07-27

- [x] Tier2.asset의 누적 매출 100,000G·평판 0·자동 승급과 Day 76 기존 목표 수식 100,000G 정합 확인.
- [x] `LongPlayProgressionController`에 Day 46~75 7단계 운영 리듬 30일과 Day 76 Tier 2 돌파 계획 추가.
- [x] `PlayableDayScenarioController`에 같은 7단계 실제 상태 완료 판정과 Day 76 `CurrentTier >= 2` 판정 연결.
- [x] 보관 12→20개, Processed 2→4건, 지원 인력 3명+상품 4종 준비, 3카테고리 목표 연결.
- [x] 활성 B07+정확한 ToolSet+Processed, 활성 마을 변화+상품 4종, 주간 누적 매출 점검 연결.
- [x] Day 1~45·Tier 2 100,000G·B06 Tier 2·B08 Tier 3·저장/경제/제작/채용 권위 비침범.
- [x] Runtime/Editor 오류 0, 기존 CS8785/CS0414 경고만 확인.
- [x] Day 46~76/Tier 2 캠페인 정적 계약 34/34 PASS.
- [ ] 사람 판단 뒤 안전 Unity 경로에서 대표 Day 46/52/59/66/73 상태 전환 확인.
- [ ] Day 76 매출 도달→자동 Tier 2 승급과 1920×1080 목표/체크리스트, Day 77 폴백 확인.

## Task 123 Day 77~90 Tier 2 주방 가치사슬 캠페인 — 2026-07-27

- [x] B06 BuildingData/Blueprint/Prefab의 Tier 2 배치 권위와 Kitchen 작업대 타입 확인.
- [x] BreadLoaf·구운 감자·생선구이 세 기존 레시피의 재료·출력·Kitchen 요구와 출력 Item 경로 확인.
- [x] `LongPlayProgressionController`에 Day 77~90 계획 14개와 주방 캠페인 제목/범위 추가.
- [x] `PlayableDayScenarioController`에 B06 배치, 세 상품 판매/준비, Chef, 3카테고리, Processed 변화 실제 상태 판정 연결.
- [x] Day 90 활성 B06+세 주방 상품 당일 판매+Processed 마을 방향 가치사슬 완주 조건 연결.
- [x] Day 1~76·Tier/경제/제작/채용/마을 변화/저장 권위와 기존 리소스 비침범.
- [x] Runtime/Editor 오류 0, 기존 CS8785/CS0414 경고만 확인.
- [ ] 호출부 2개를 3개로 잘못 기대한 정적 검사식을 폐기하고 정확한 기대값으로 계약 검사 1회 수행.
- [ ] 사람 판단 뒤 안전 Unity 경로에서 B06 지급/배치와 BreadLoaf·구운 감자·생선구이 제작→판매 확인.
- [ ] Chef 고용, Processed 다음 날 변화, Day 90 완료와 1920×1080 목표/체크리스트 가독성 확인.

## Task 124 Task 123 주방 캠페인 정적 계약 복구 — 2026-07-27

- [x] `TryResolveTierTwoKitchenMilestone(day` 목표/체크리스트 호출부 기대값을 실제 2개로 정정해 PASS 확인.
- [x] Day 77~90 계획 14개와 런타임 case 14개 확인.
- [x] 단일 판정 정의, Day 76 Tier 2 경계, 캠페인 제목/보급 경계 확인.
- [x] B06 프리팹 Kitchen 타입, 세 레시피 Kitchen 요구, 세 출력 Processed category와 요구 리소스 존재 확인.
- [x] `ShopCustomizationController`의 B06 최소 Tier 실제 C# 행을 명시 경로에서 확인.
- [x] 구운 감자·생선구이 Item asset의 실제 `itemName` YAML 행을 명시 경로에서 확인.
- [x] 결합 검증을 재실행하지 않고 세 직접 권위 행으로 검사 표현 불일치를 확정.

## Task 125 B06 Tier·출력 Item 이름 권위 행 감사 — 2026-07-27

- [x] B06 실제 C# 행 `case "Blueprint_B06_KitchenStation": return 2;` 확인.
- [x] BakedPotato YAML `"\uAD6C\uC6B4 \uAC10\uC790"`를 `구운 감자`로 해석 확인.
- [x] GrilledFish YAML `"\uC0DD\uC120\uAD6C\uC774"`를 `생선구이`로 해석 확인.
- [x] 실제 데이터 결함 없음, 검사 표현 불일치로 분류.
- [x] Task 124 DONE 및 Task 123 정적 계약 47/47 확정.
- [x] 다음 단일 구현: Day 91 이후 장기 플레이가 일반 폴백으로 돌아가는 지점을 기존 시스템으로 연결.

## Task 126 Day 91~105 Tier 3 공동 공방 캠페인 — 2026-07-27

- [x] Tier 3 평판 3·자동 승인과 `AddReputation` 미호출 도달 불가 경로 감사.
- [x] 전문 주민 요청 완료→기존 일일 활동 표식→Tier 2 동안 하루 1 평판 연결.
- [x] Day 91~105 계획/상태 판정 15개와 목표·체크리스트 두 경로 연결.
- [x] B08 Tier 3, 의류 Sewing(4), 가구 BasicWorkbench(1), 두 Item Luxury(3) 권위 재사용.
- [x] Runtime/Editor 순차 빌드 오류 0, 기존 CS8785/CS0414만 유지.
- [x] 정적 계약 24/24와 대상 `git diff --check` PASS.
- [ ] 안전 Unity 경로에서 주민 요청 3일→평판 3→Tier 3 자동 승급 확인.
- [ ] Tier 3 장부 B08 지급·배치→의류/가구 준비·판매→Luxury 다음 날 변화 확인.
- [ ] Day 91~105 목표/체크리스트 상태 전환과 1920×1080 가독성 확인.
- [ ] 기존 GRID P1/P2/P4/P5와 Tripo 감사 문서에서 B06/B07/B08/B09/B05 남은 기능성 에셋 결함 하나를 다음 단일 작업으로 선정.

## Task 127 B05~B08 전문 주민 전면 접근 연결 — 2026-07-27

- [x] B05~B08 footprint/clearance/interaction과 Specialist 원점 목적지 단절 감사.
- [x] Workbench 배치의 회전된 interaction 셀을 읽기 전용 접근점으로 투영.
- [x] 활성 동일 타입 작업대의 NavMesh 완전 경로 선택과 셀 단위 예약/해제 연결.
- [x] 이동·회수 시 이동/가공 중단, 도착 후 정면 보기, 저장 상태 복원 후 재접근 연결.
- [x] 씬·프리팹·FBX·재질·저장·레시피·경제 권위 비침범.
- [x] 확인된 `Assembly-CSharp.csproj`→`Assembly-CSharp-Editor.csproj` 순차 빌드 오류 0.
- [x] 원점 목적지 호출 0, Workbench interaction 투영, 완전 경로, 예약/해제, 이동·회수 무효화 정적 계약 29/29 PASS.
- [ ] 사람 판단 뒤 안전 Unity 경로에서 B06/B07/B08 전문 주민 접근·정면·겹침 방지·실제 제작 확인.
- [x] 다음 단일 구현 후보: 기존 고객/NPC 권위를 감사해 실제 관광객 계층이 라벨뿐인지 확인하고, 가장 작은 정상 플레이 진입 단절 하나를 선정.

## Task 128 관광객 손님 정상 플레이 진입 — 2026-08-04

- [x] `NpcController`/`NpcScheduleController` 직렬화와 씬 생성기·기존 검증기에서 작성 고객 8명=주민, 관광객 0명 확인.
- [x] Day 2+ 실제 개점에 영업당 최대 2명/동시 1명 세션 한정 관광객 진입 연결.
- [x] 주민의 SkinnedMesh/Avatar 시각만 복제하고 런타임 프로필 이름·`[관광객]` 표시 연결.
- [x] 관광객의 일과표·생산/전문가·대화/친밀도·채용·저장 기록 미생성.
- [x] 가게 주변 NavMesh 완전 경로 진입점→기존 쇼핑 FSM/구매 수학→같은 진입점 퇴장 연결.
- [x] Day 1 시나리오, 주민 Rest lease, 기존 동시 고객 상한·경제/저장 권위 보존.
- [x] Runtime/Editor 순차 빌드 오류 0, 기존 CS8785/CS0414만 유지.
- [x] 관광객 진입·분류·역할 비복제·비영속·퇴장·기존 흐름 정적 계약 46/46과 대상 공백 검사 PASS.
- [ ] 사람 판단 뒤 안전 Unity 경로에서 Day 2+ 개점→관광객 입장→`[관광객]` 말풍선/성향→구매 또는 거절→퇴장을 확인.
- [ ] 주민/관광객 동시 손님, Tier 0 외부·Tier 1 실내 주변 동선, 1920×1080 가독성을 확인.
- [x] 다음 단일 구현 후보: Day 105 뒤 일반 폴백 구간을 감사해 기존 성장 권위로 연결할 실제 단절 하나를 선정.

## Task 129 Day 106+ 본사 감사·Tier 4 최종 완주 — 2026-08-04

- [x] 정상 플레이 평판 공급 1곳과 기존 AuditService 500,000G/평판5/고용3 조건을 대조해 Tier4 도달 불가 확인.
- [x] Day 106+ Tier3 전문 주민 요청→기존 일일 저장 표식→감사 요구 평판까지 하루 1점 연결.
- [x] 실제 평판·고용·누적 매출·다음 감사일을 Day106+ 상단 목표와 운영 체크리스트에 연결.
- [x] AuditService 단독 `TryManualAdvance`와 Tier4 수동 승인·조건 수치 보존.
- [x] Tier4 첫 Settlement→전체 캠페인 기록→저장 후 다음 날 자유 운영/저장 후 종료 연결.
- [x] 새 저장 필드 없이 기존 `lastAuditDay`로 저장 후 계속한 완주 확인 복원.
- [x] Runtime/Editor 순차 빌드 오류 0, 기존 CS8785/CS0414만 유지.
- [x] 최종 감사·평판 상한·일일 중복 방지·목표/체크리스트·완주 저장 흐름 40개 자동+1개 직접 권위 PASS.
- [ ] 사람 판단 뒤 안전 Unity 경로에서 Day106/107 요청→평판4/5→정기 감사→Tier4 확인.
- [ ] Tier4 첫 정산 완주 화면, 저장→자유 운영→재실행, 저장 후 종료, 1920×1080 가독성 확인.
- [x] 다음 단일 구현 후보: Tier4 감사 성공/실패와 최종 해금이 플레이어 화면에서 실제로 읽히는지 감사하고 로그 전용 단절 하나를 연결.

## Task 130 본사 감사 성공·실패 플레이어 피드백 — 2026-08-04

- [x] 기존 감사 앱이 날짜만 표시하고 실제 결과는 로그 전용임을 확인.
- [x] AuditService에 현재 매출·평판·고용/조건 완료/다음 감사일/최근 결과 읽기 API와 갱신 이벤트 추가.
- [x] 감사 실패·통과·최고 등급·내부 승급 보류 결과를 한 경로로 발행.
- [x] 감사 앱에 실제 3조건 현재값·완료/부족·최근 결과·다음 행동 표시.
- [x] 수동 승인 Tier 진행 바를 감사 3조건의 실제 부분 진행과 연결.
- [x] 고정 폰 높이 안에서 Tier/매출/시설/감사 카드 재배치.
- [x] 500,000G/평판5/고용3/7일, 단독 수동 승급, LastAuditDay 저장, 씬/프리팹/저장 스키마 보존.
- [x] Runtime/Editor 순차 빌드 오류 0, 정적 계약 48/48 PASS.
- [ ] 사람 판단 뒤 조건 미달 감사→실패/다음 행동, 조건 충족 감사→Tier4 해금 앱 갱신 확인.
- [ ] 1920×1080에서 Tier/시설/감사 카드 텍스트 잘림·겹침과 열린 앱의 날짜 전환 즉시 갱신 확인.
- [x] 다음 단일 구현 후보: 기존 AudioManager/프로젝트 내 출처 확인 클립을 감사해 구매·개점·정산 중 가장 영향력 큰 무음 피드백 하나 연결.

## Task 131 Tripo 장기 정책 재확인·B06 Kitchen 보정 — 2026-08-04

- [x] 첨부 GRID/Tripo 요구와 기존 `GridService` P1~P5, v10 저장, Placeable 가이드/Tripo 감사의 중복 여부 대조.
- [x] FBX 174/OBJ 150/GLB 0/Blend 0, non-Nature 고유 FBX 24개 재확인.
- [x] 1~8 개별 분류, 플레이어/주민 정체성 보존, 기능 가구 역할 우선, 원본 비파괴, 출처 배포 게이트 장기 지침 유지.
- [x] B06 기존 Visual renderer bounds 기반으로 명백히 큰 X/Z Box를 축소 전용 정합.
- [x] box형 NavMeshObstacle을 같은 center/size로 정합하고 메시 누락·이미 작은 축 보존.
- [x] 로컬 `-Z` 물리 앞 `PA_KitchenInteractionAnchor`와 성공 제작 뒤 0.72초/최대 3.5% 모델 pulse 연결.
- [x] B05 기능 아트, B06 Tier 2/2×2/Kitchen 레시피, 전문 주민 접근, 저장 권위 보존.
- [x] FBX·프리팹·메인 씬·재질·BuildingData·설계도·패키지 무변경.
- [x] Runtime/Editor 순차 빌드 오류 0, 정적 계약 30/30 PASS.
- [ ] 사람 판단 뒤 안전 Unity에서 B06 배치, 플레이어/Chef 전면 접근, 모델-물리 경계, Bread 제작 pulse 확인.
- [ ] 같은 GameCamera Before/After에서 통로·스케일·그림자·UI 겹침 개선 확인.
- [ ] 최종 배포 전 B06 포함 Tripo 개별 생성 계정·생성일·상업 이용 증빙 확보.
- [x] 다음 단일 후보: B07 Forge 전면/열원/물리/성공 피드백 감사 또는 기존 오디오 피드백 우선순위 복귀.

## WORLD-000 Procedural Island Architecture — 2026-08-04

- [x] 현재 loop-state/dirty Git/선행 변경을 감사하고 기존 변경을 보존했다.
- [x] 월드/건설/저장/생활/NPC/상점/씬/NavMesh 실제 코드와 필드를 분류했다.
- [x] Unity Terrain/custom chunk mesh/voxel을 비교하고 custom chunk mesh를 권장안으로 확정했다.
- [x] 2m cell, 16×16 Chunk, 1m elevation 0~6, seed+sparse delta, building transaction, role anchor, chunk nav 전략을 문서화했다.
- [x] Prototype_FirstDay/WorldSandbox/MainGame 역할과 Gate 1~5를 분리했다.
- [x] WORLD-001~012 bounded backlog와 별도 MainGame integration gate를 작성했다.
- [x] WORLD-000에서는 코드·씬·에셋·SaveData·Packages·ProjectSettings를 수정하지 않았다.
- [ ] **WORLD-001 전 사람 확인:** Prototype_FirstDay를 Golden Regression Scene으로 보존하고 신규 `Assets/Scenes/WorldSandbox.unity`를 별도 생성하는 전략 승인.
- [ ] **WORLD-001 전 사람 확인:** 2m/16×16/1m·custom mesh를 prototype baseline으로 승인. 최종 미감 승인이 아니라 WORLD-002에서 재검토 가능.
- [ ] **WORLD-001 전 사람 확인:** WORLD-001의 단일 범위·최대 12경로·scene builder/validator 방식 승인.
- [ ] **별도 티켓:** 중단된 `AudioManager.cs`/`SalesLogManager.cs` 변경의 의도와 검증 상태를 정리. WORLD 티켓에 섞지 않음.
- [ ] **WORLD-007 전:** additive save schema/version/migration/격리 왕복 승인.
- [ ] **WORLD-008 전:** per-Chunk NavMeshSurface와 D3D11 안전 runtime 검증 승인.
- [ ] **Gate 1~5 후:** MainGame 사용 vs 새 integration scene, Prototype_FirstDay 장기 tutorial 유지 여부 승인.
- [ ] WORLD-001은 새 지시 전 자동 실행하지 않는다.

## BASELINE-STABILIZE-001 — 2026-08-04

- [x] `master@0b07d71`, `origin/master`, clean 시작 상태와 bounded-ticket preflight 확인.
- [x] scene/ProjectSettings 내용, meta/GUID, 신규 asset 참조, SubmissionPackages, history 대용량 blob, 새 crash artifact 무결성 확인.
- [x] Runtime/Editor 오류 0 빌드와 Unity 6000.3.2f1 D3D11 로드 확인.
- [x] Core 5종과 SaveRoundTrip, CustomerArrival/Presentation/InteriorCustomer PASS.
- [x] CustomerPreference/Village 4px 겹침을 단일 좌표 수정하고 유일한 재검증 PASS.
- [x] ShopCustomization 기능 단언과 ShopProgression Tier 0 단언을 캡처 직전까지 확인.
- [x] 동일 GameView capture timeout 2회에서 티켓 규칙대로 Unity 검증 중단.
- [x] Save v10/v9→v10, AudioManager/SalesLogManager, B06 Kitchen 계약 정적 대조.
- [ ] 사람 검토: 실제 판매 `sale.confirm` 1회 청취와 BGM/SFX 회귀.
- [ ] 사람 검토: 1920×1080에서 상점 커스터마이징·Tier 진행 캡처, 선호/Village 패널 간격과 전체 HUD 가독성.
- [ ] 사람 검토: B06 Kitchen 플레이어/Chef 전면 접근, collider/carving 체감, 크기·시야, Bread 성공 pulse.
- [ ] 사람 검토 뒤 별도 bounded ticket으로 미실행 Village/economy/mining/outdoor validator 범위를 결정.
- [ ] WORLD-001은 이번 결과 보고 뒤 자동 시작하지 않는다.

## BASELINE-CRAFTING-UI-FIX-001 — 2026-08-05

- [x] `master@0b07d71e7dc6d259713a97d2011d181efa73b200`, 기존 의도된 dirty 7경로, Unity 6000.3.2f1 D3D11, 신규 crash 0 확인.
- [x] Basic recipe 데이터·CraftingUI 필터/집계/카드 생성과 Runtime hierarchy 조사.
- [x] 투명 Viewport Mask가 생성된 두 카드를 모두 가리는 D/E 유형 원인 확정.
- [x] Mask alpha 최소 수정과 생성 직후 Content layout 확정.
- [x] 전용 D3D11 Play validator 추가: recipe 2 = card 2, active, 672×96, bounds, alpha, 결과/재료/보유량 표기 PASS.
- [x] Wood 0/2에서도 카드 유지, 선택 가능, 제작 차감·지급 차단 PASS.
- [x] Wood 2/2에서 Wood 2 차감, Plank 1 지급, B05 pulse PASS.
- [x] Runtime/Editor compile 오류 0, ProcessingChain 회귀 PASS, blocking Console pattern 0, 신규 crash 0.
- [x] 캡처 재시도 없이 `CAPTURE_EVIDENCE_DEBT`로 이관하고 사람 캡처 요청 생략.
- [x] `INVENTORY-DRAG-GHOST-UI-DEBT`, `DEVELOPMENT_OVERLAY_LAYOUT_POLISH`, `PHONE-HIRING-FEED-INCOMPLETE`, `SHOP-READABILITY-AND-MAP-COMPOSITION-DEBT`, `SHOP-FURNITURE-PLACEMENT-001`, 판매음/B06 미감, 커스터마이징/Tier 캡처를 비차단 backlog로 유지.
- [x] 최종 판정 `BASELINE_READY_FOR_WORLD_001`.
- [ ] WORLD-001은 이번 보고에서 시작하지 않고 다음 명시적 bounded ticket으로만 시작한다.

## WORLD-001 WorldSandbox Bootstrap and Read-Only World Cell Grid — 2026-08-06

- [x] clean `master@befc1738dd868d24b06a2c8f673a13293b60d36b`, `origin/master` 일치, Unity 6000.3.2f1 D3D11 확인.
- [x] Editor builder로 별도 `Assets/Scenes/WorldSandbox.unity` 생성; authored root 3개와 manager 사본 0 확인.
- [x] 16×16셀·2m·16×16 chunk·1m elevation·level 0..6·초기 0·Default/Empty 읽기 전용 모델 구현.
- [x] cell/world/index/chunk 변환, 경계 `Try*`, row-major 읽기 전용 열거 구현.
- [x] 일반 셀·chunk 경계·원점·희소 좌표·hover용 line-only debug view 구현.
- [x] D3D11에서 256셀, 네 모서리, OOB, 256 index 왕복, chunk, seeded 10,000회 좌표 왕복, checksum `AD517449E587DBE5` PASS.
- [x] Runtime/Editor compile 오류 0, Missing Reference 0, manager 중복 0, blocking Console 0, 신규 crash 0.
- [x] Prototype_FirstDay/MainGame, Save schema/authority, Packages, ProjectSettings byte-for-byte 무변경 확인.
- [x] 자동 캡처는 실행하지 않고 `CAPTURE_EVIDENCE_DEBT`로 기록.
- [x] 최종 상태 `WORLD_001_COMPLETE`.
- [ ] WORLD-002는 자동 시작하지 않는다. 새 명시적 bounded ticket과 해당 시각 스케일 승인 기준을 받은 뒤 시작한다.

## WORLD-002 Chunk Testbed + Height Level Mesh Prototype — 2026-08-06

- [x] `World002Terraces` 결정론적 bootstrap으로 level 0~6을 모두 포함하는 16×16 높이 데이터 구성.
- [x] 셀별 GameObject 없이 한 Chunk의 상면·노출 절벽 custom mesh, normal, UV, index 생성.
- [x] 상면 256, 절벽면 280, 정점 2,144, checksum `ADF9201BC8265BC5` 검증.
- [x] 합성 2-Chunk의 상면 seam 17지점 정확 일치 검증.
- [x] 시각/collider mesh 분리, `MeshCollider`, 재질 2슬롯, visual/collider 선택 dirty rebuild 검증.
- [x] D3D11 Edit/Play, WORLD-001 회귀, Runtime/Editor compile 오류 0, blocking Console 0, 신규 crash 0.
- [x] Prototype_FirstDay/MainGame, Save schema/authority, Packages, ProjectSettings 무변경.
- [x] 선택 캡처는 실행하지 않고 `CAPTURE_EVIDENCE_DEBT`로 기록.
- [x] 최종 상태 `WORLD_002_COMPLETE`.
- [ ] 다음 단일 티켓: WORLD-003 Single-Cell Raise/Lower Terraforming. 생성기·저장·NavMesh·건물/상점 통합은 아직 시작하지 않는다.

## WORLD-003 Single-Cell Raise/Lower Terraforming — 2026-08-06

- [x] 좌클릭 셀 선택, `R` 한 level 상승, `F` 한 level 하강, `Z` 마지막 성공 편집 1단계 undo.
- [x] `(0,0)` 보호, out-of-bounds, level 0/6 clamp, no-undo typed failure와 무변경 원자성.
- [x] 성공 편집에서 elevation만 변경하고 ground/water/path/occupancy 보존.
- [x] 내부 셀은 소유 Chunk만, x=15/16 seam 셀은 양쪽 Chunk만 dirty 처리.
- [x] 인접 cliff mask와 visual/collider mesh를 성공/undo마다 정확히 1회 갱신.
- [x] 편집 셀 top에 대한 `MeshCollider` raycast와 visual/collider bounds 일치.
- [x] D3D11 WORLD-003, WORLD-002·001 회귀, Runtime/Editor 오류 0, blocking Console 0, 신규 crash 0.
- [x] scene/Save schema/Packages/ProjectSettings 무변경, 선택 캡처 `CAPTURE_EVIDENCE_DEBT`.
- [x] 최종 상태 `WORLD_003_COMPLETE`.
- [ ] 다음 단일 티켓: WORLD-004 Ground/Path Paint and Water Cell Prototype. save/building/NPC/NavMesh는 시작하지 않는다.

## WORLD-004 Ground/Path Paint and Water Cell Prototype — 2026-08-06

- [x] Grass/Soil/Sand/Rock 지면과 Dirt/Stone 길 데이터 및 8개 visual material slot 구현.
- [x] water surface level/depth invariant와 물 상면·노출 shoreline face 구현.
- [x] farmable/walkability를 ground/path/water 상태에서 파생하고 물 셀을 non-walkable/non-farmable로 처리.
- [x] protected/invalid water/dry drain/path-under-water typed failure와 무변경 원자성.
- [x] ground/path/water 성공 transaction, owner/seam dirty Chunk, 마지막 성공 편집 1단계 undo.
- [x] 물 geometry를 terrain collider triangle stream에서 제외하고 bed raycast 유지.
- [x] WorldSandbox `G` ground, `T` path, `V` water, `X` surface undo Play Mode 조작.
- [x] Runtime/Editor compile 오류 0, D3D11 WORLD-004 및 WORLD-003·002·001 회귀 PASS, blocking Console 0, 신규 crash 0.
- [x] 세 Scene/Prefab/Save schema/Packages/ProjectSettings 무변경, 선택 캡처 `CAPTURE_EVIDENCE_DEBT`.
- [x] 최종 상태 `WORLD_004_COMPLETE`.
- [ ] 사람이 현재 11개 dirty 경로를 검토·commit한다.
- [ ] WORLD-005는 자동 시작하지 않는다. 다음 명시적 bounded-ticket 지시를 기다린다.

## WORLD-005 Relocatable Building MVP — 2026-08-10

- [x] 기존 B09 창고 prefab/StorageBox를 보존하는 4x3 footprint·회전 entrance sidecar.
- [x] 물/길/보호/점유/범위 밖/지면/단차/입구 typed placement preflight.
- [x] 실제 모델 기반 valid/invalid ghost, `B/M/Q/E/Enter/Escape` WorldSandbox 조작.
- [x] 배치·이동·회수 occupancy 원자 commit, 실패 이동 rollback, 단일 session registry.
- [x] 점유 셀 non-walkable 및 height/surface 편집 차단.
- [x] service/debug controller 파일명-GUID 정합, scene embedded MonoScript 0, root 3 유지.
- [x] Runtime/Editor compile 오류 0, D3D11 WORLD-005 및 WORLD-004~001 회귀 PASS, blocking Console 0, 신규 crash 0.
- [x] Prototype_FirstDay/MainGame, prefab 원본, Save schema, Packages, ProjectSettings 무변경.
- [x] 선택 캡처 `CAPTURE_EVIDENCE_DEBT`; 자동 renderer/prefab/transform/footprint 증거 통과.
- [x] 최종 상태 `WORLD_005_COMPLETE`.
- [ ] 승인된 WORLD-005 로컬 ticket commit 생성.
- [ ] 다음 단일 티켓: 기존 backlog `WORLD-006 World Seed + Minimal Island Generator`. Save/NavMesh/gameplay 통합은 아직 시작하지 않는다.

## WORLD-006 World Seed + Minimal Island Generator — 2026-08-10

- [x] generationVersion 1, configurable provisional 128x128/2m/16x16 Chunk definition.
- [x] deterministic ocean border, irregular coast, meadow, forest, highland, river and pond.
- [x] safe start plateau와 independent flat 4x3 shop footprint/entrance 후보.
- [x] start/shop/beach/meadow/forest/highland/pond anchor dry cell-graph 연결.
- [x] Forage/Timber/Stone/Fish stable spawn key; final prefab와 save payload는 제외.
- [x] WorldSandbox `J`, `[`/`]`, `K`와 64-chunk/8-material debug mesh, per-cell GameObject 0.
- [x] 128 seed x 2 deterministic corpus, checksum 128개 고유, land ratio 44.9~58.2%, 1,599ms.
- [x] Runtime/Editor compile 오류 0, D3D11 WORLD-006 및 WORLD-005~001 회귀 PASS, blocking Console 0, 신규 crash 0.
- [x] Prototype_FirstDay/MainGame, prefab, Save schema, Packages, ProjectSettings 무변경.
- [x] 선택 캡처 `CAPTURE_EVIDENCE_DEBT`; 최종 상태 `WORLD_006_COMPLETE`.
- [ ] 승인된 WORLD-006 로컬 ticket commit 생성.
- [ ] 다음 단일 티켓: `WORLD-006B Movable Shop Furniture`. World save/NavMesh/gameplay integration은 아직 시작하지 않는다.

## WORLD-006B Movable Shop Furniture — 2026-08-10

- [x] 기존 `ShopCustomizationController`/`shop.interior`를 단일 실내 배치 권위로 재사용하고 병렬 시스템 추가 금지.
- [x] 실제 `ShopSlot` 판매대 1개를 상점 안에서 이동하고 270° 회전.
- [x] 보호 입구 거부, 점유 원자성, 입구→서비스 통로 유지.
- [x] 이동 취소 시 셀 중심이 아니라 정확한 authored 시작 Transform 복원.
- [x] ShopSlot hierarchy/component/재고/가격 유지, 고객 완전 NavMesh 접근.
- [x] 이동 뒤 Bread 2개 전체 스택 146G 판매와 EconomyService 반영.
- [x] 기존 v10 placeable stable ID/zone/cell/rotation 투영으로 WORLD-007 저장 대상 준비.
- [x] Runtime/Editor compile, 전용 D3D11, InteriorCustomer, SaveRoundTrip, FinalDemoRoute PASS; blocking Console/crash 0.
- [x] Scene/Prefab/Save schema/Packages/ProjectSettings 무변경, 캡처 `CAPTURE_EVIDENCE_DEBT`.
- [x] 최종 상태 `WORLD_006B_COMPLETE`.
- [ ] 승인된 WORLD-006B 로컬 ticket commit 생성.
- [ ] 다음 단일 티켓: `WORLD-007 World Persistence` — seed/generationVersion/sparse terrain/building/furniture/occupancy/safe player round-trip.

## WORLD-007 World Persistence

- [x] additive save schema v11과 v10 `LegacyFixed` 무손실 이행.
- [x] seed/generationVersion 및 높이·지면·길·물 sparse delta round-trip.
- [x] B09 stable record, 실제 배치 권위를 통한 점유 재구축, 중복 복원 방지.
- [x] `shop.interior` 가구 투영, stable generated-resource 상태, 안전한 플레이어 위치 복원.
- [x] out-of-range delta와 누락 generationVersion을 live mutation 전에 거부.
- [x] Runtime/Editor compile, WORLD-007 D3D11, SaveRoundTrip, WORLD-004/005/006 회귀 PASS; blocking Console/crash 0.
- [x] Scene/Prefab/Packages/ProjectSettings 무변경, 캡처 `CAPTURE_EVIDENCE_DEBT`.
- [x] 최종 상태 `WORLD_007_COMPLETE`.
- [ ] 후속 저장 부채: temp/backup 기반 crash-safe JSON write와 관련 recovery 검증.
- [ ] 후속 회귀 부채: v10을 하드코딩한 Mining/Outdoor/ShopCustomization validator를 각 담당 티켓에서 v11 계약으로 갱신.
- [ ] 승인된 WORLD-007 로컬 ticket commit과 별도 recovery 기록 commit 생성.
- [ ] 다음 단일 티켓: `WORLD-008 Reachability and Navigation Prototype`.

## WORLD-008 Reachability and Navigation Prototype

- [x] 주요 생성 anchor 7개 logical cell graph 연결과 고립 장벽 판정.
- [x] B09 entrance 및 핵심 경로를 끊는 place/move의 `CriticalRouteBlocked` 원자 거부.
- [x] 기존 AI Navigation 2.0.12로 2×2 Chunk / 32×32 cell local sector surface 구성.
- [x] 1-cell overlap과 동일 높이 seam portal link 생성.
- [x] interior edit 1-sector, boundary edit owner+neighbor 2-sector async rebuild.
- [x] 영향받은 테스트 NPC pause→2m 이내 reproject→complete repath→resume→arrival/settle.
- [x] 128×128 실제 seed 16 sector/57ms 및 start→shop entrance 완전 NavMesh path.
- [x] 저장 restore preflight에 critical building reachability를 추가하되 schema v11은 유지.
- [x] Runtime/Editor compile, WORLD-008 D3D11, WORLD-007/005/006, InteriorCustomer, FinalDemoRoute PASS.
- [x] Scene/Prefab/Packages/ProjectSettings 무변경, 신규 crash 0, 캡처 `CAPTURE_EVIDENCE_DEBT`.
- [ ] 비차단: InteriorCustomer 성공 종료 뒤 late-visitor NavMesh teardown 진단 문구 정리.
- [ ] 승인된 WORLD-008 로컬 ticket commit 생성.
- [ ] 다음 단일 티켓: `WORLD-009 Existing Gameplay World Adapter` — 기존 Inventory/Crafting/Shop/Gathering/DayNight/NPC 목적지를 월드 권위에 연결.

## WORLD-009 Existing Gameplay World Adapter

- [x] WorldSandbox runtime-only adapter와 deterministic seed 9009 bootstrap.
- [x] stable Forage/Timber/Stone/Fish spawn을 기존 Item/Inventory에 매핑.
- [x] Timber 2 → Recipe_Plank/CraftingService/B05 → Plank 1 → B01 ShopSlot 진열.
- [x] 기존 Day/Night 개점 gate와 NpcController generated Start→B01 구매 경로.
- [x] EconomyService balance/cumulative revenue 반영.
- [x] 격리된 실제 SaveManager save → 다른 seed/state 변조 → seed/resource/inventory/clock/shop/economy 복원.
- [x] schema v11 유지, Scene/Prefab/Packages/ProjectSettings/사용자 저장 무변경.
- [x] WORLD-009 및 WORLD-001/007/008, CraftingRecipeCard, CustomerArrival, FinalDemoRoute D3D11 PASS.
- [x] 최종 상태 `WORLD_009_COMPLETE`.
- [ ] 승인된 WORLD-009 local ticket commit 생성.
- [ ] 다음 단일 티켓: `WORLD-010 M70 Integration and Regression` — 10~20분 World Alpha와 Golden regression 최종 통합.

## BETA-003 Crafting and Production Expansion

- [x] generated WorldSandbox에 기존 B05 Basic/B06 Kitchen/B07 Forge 기능 시설 연결.
- [x] Kitchen/Forge/Basic 카드 3/2/2 표시와 프레임 안전 컨텍스트 전환.
- [x] 재료 부족 보유량 표시, 무차감·무지급, 충분 시 선택·실제 제작.
- [x] BETA-002의 Carrot/Wheat/Fish/Ore/Wood를 기존 5개 RecipeData/CraftingService와 연결.
- [x] 가공 결과 ItemInstance 품질과 기본 가격 메타 보존.
- [x] 원재료 118G → 가공품 203G 가치 상승과 B01 실제 28G 판매.
- [x] Basic 카드 2개/Wood→Plank, ProcessingChain, BETA-002 낮 활동 회귀 PASS.
- [x] Runtime/Editor 오류 0, blocking Console 0, 신규 crash 0.
- [x] Scene/Prefab/Packages/ProjectSettings/Save schema 무변경.
- [x] 최종 상태 `BETA_003_COMPLETE`.
- [ ] 비차단: M85 통합 플레이테스트에서 시설 간격·카드 미감 확인 (`CAPTURE_EVIDENCE_DEBT`).
- [ ] 다음 단일 티켓: `BETA-004 Shop Readability and Merchandising`.

## BETA-004 Shop Readability and Merchandising

- [x] 실제 B01 4칸 판매대와 밤 영업 구역을 월드 표지로 식별.
- [x] 빈 칸/상품명/수량/가격/품질/오늘 품절 상태를 실제 `ShopSlot`에서 표시.
- [x] 플레이 HUD 재고·가격 요약과 이동된 판매대 방향 안내.
- [x] 판매대 이동·270° 회전 뒤 표지/ShopSlot/재고/가격/고객 접근 유지.
- [x] 범위 밖 이동 원자 거부와 기존 v11 pose projection 유지.
- [x] BETA-004, BETA-003, WORLD-006B D3D11 PASS; blocking Console 0, 신규 crash 0.
- [ ] 비차단: M85 통합 플레이테스트에서 월드 라벨 크기·밀도 최종 확인 (`CAPTURE_EVIDENCE_DEBT`).
- [ ] 다음 단일 티켓: `BETA-005 Customer Strategy and Feedback`.

## BETA-005 Customer Strategy and Feedback — COMPLETE

- [x] 기존 Miner/Tailor profile, 월드/HUD 성향 힌트, 실제 구매/보류 feedback 연결.
- [x] 기존 `PurchaseEvaluator`의 profile 민감도와 Processed/Luxury 카테고리 반응 확인.
- [x] 실제 player HUD와 숨겨진 개발 Canvas 계약을 구분하고 동기 live-HUD 표본 최소 수정.
- [x] Runtime/Editor compile 오류 0; 기존 CS8785/CS0414만 유지.
- [x] Unity Personal 라이선스 복구 확인: activation 200, LicenseUpdate Added, EULA Agreed.
- [x] Miner live HUD/profile/화면 경계와 250G 보류, Tailor live HUD/profile과 1G 구매 검증.
- [x] 보류 시 재고·돈 무변경, SalesLog 보류 1건, 실제 feedback/demand/HUD 설명 검증.
- [x] BETA-004, CustomerPresentation, CustomerArrival 회귀와 blocking Console 0, 신규 crash 0.
- [ ] 비차단: 자동 캡처 미생성은 `CAPTURE_EVIDENCE_DEBT`로 M85 통합 플레이테스트에 유지.
- [ ] 다음 단일 티켓: `BETA-006 Phone Hiring and Feed Completion`.

## BETA-006 Phone Hiring and Feed Completion — COMPLETE

- [x] WorldSandbox에서 `P`로 열리는 화면 내 runtime Phone과 Input System EventSystem, Audit/Hiring/Feed/Settings 4개 탭.
- [x] 권위 후보 8명 카드에 이름·역할·실제 비용·잔액·부족/고용/중복 상태와 roster 표시.
- [x] C-02~C-09 원본을 보존하는 역할별 wrapper prefab 8개와 후보 `spawnPrefab` 연결.
- [x] 실제 Hiring button이 `EconomyService`에서 정확한 비용을 차감하고 동일 역할 주민을 한 번만 스폰.
- [x] Hiring/Feed alpha 0 mask 결함 수정, 판매 전 empty state와 실제 `ShopSlot` 판매 즉시 Feed 갱신.
- [x] Feed에 상품·가격·구매자·시간·category·quality·마을 변화 방향 표시, Audit/Settings 보존.
- [x] Runtime/Editor compile, BETA-006, BETA-005, VillageChangeSignal, Golden FinalDemoRoute PASS; blocking Console 0, 신규 crash 0.
- [ ] 비차단: 자동 Game View 캡처는 `CAPTURE_EVIDENCE_DEBT`로 M85 통합 플레이테스트에 유지.
- [ ] 다음 단일 티켓: `BETA-007 Village Response and NPC Integration`.

## BETA-009 Persistence and Recovery — IMPLEMENTED WITH VALIDATION DEBT

- [x] additive gameplay envelope v12와 기존 procedural world payload v11/LegacyFixed 호환 유지.
- [x] SalesLog/Feed, village exact context, farm crops, B09 storage, player pose/hotbar/shop-open/WorldAlpha resume 저장.
- [x] outdoor placeable 보존 merge, load-boundary transient 정리, hiring repeated-load 중복 완화.
- [x] Runtime/Editor compile 오류 0, diff/JSON 검사 PASS, native crash 0.
- [ ] 실제 save→재시작→load→continue→동일 save 반복 load 검증: validator가 save 전 invalid B01 fixture 좌표에서 중단되어 BETA-010으로 이관.
- [ ] 다음 단일 티켓: `BETA-010 Full Playable Beta Integration`.

## BETA-010 Full Playable Beta Integration — BLOCKED

- [x] 실제 v12 save, Play 종료·재진입, world checksum/B09/player cell 복원.
- [x] Runtime/Editor compile 오류 0, native crash/save corruption/금지 경로 변경 0.
- [ ] player facing 137° 복원: 다음 frame identity 덮어쓰기 원인 계측 및 authoritative PlayerRoot/PlayerController 동기화 필요.
- [ ] 복원 후 continue sale와 same-save repeated load.
- [ ] BETA-007 실제 주민 반응, BETA-008 Day 6~7/Week 1, Golden/M70 full regression.
- [ ] `M85_GAMEPLAY_BETA_COMPLETE` — 현재 미달성.

## 2026-08-25 Visual Baseline 이후 우선순위

1. `BETA-010-PERSISTENCE-RECOVERY-001`: player facing 복원 원인을 해결하고 restart/repeated-load, BETA-007/008, Golden/M70 회귀를 완료한다.
2. `PLAYER-ENTRY-INTEGRATION-001`: Golden 보존 및 MainGame 별도 승인 규칙 아래 타이틀에서 생성 월드 전체 루프로 가는 하나의 제품 진입 경로를 만든다.
3. `TARGET-ASPECT-AND-CAMERA-GATE-001`: 1920×1080 기준, HUD safe area, 상점 차양 가림 방지, 팝업 크기 체계를 확정한다.
4. `SHOP-READABILITY-ONBOARDING-001`: 상점 입구/내부/판매대/개점 간판/첫 동선을 즉시 읽히게 한다.
5. `DIEGETIC-ACTIVITY-VISUALS-001`: 해변·숲·광산·농장의 원시 큐브와 거대 라벨을 기능 에셋과 획득 피드백으로 교체한다.
6. `PLAYABLE-BUILD-GATE-001`: standalone 후보를 만들어 새 프로필부터 저장 재실행까지 완주·16:9 증거를 확보한다.

후순위 유지: 인벤토리 drag ghost, F10 개발 오버레이, 판매 fallback 음색, B06 pulse, 추가 장식/맵/고급 테마.

## 2026-08-31 발표자료

- [x] 6월 26일 기준선과 여름방학 신규/확장 성과를 분리한 개발 감사 작성.
- [x] 디자인 마스터 기반 7장 PPTX/PDF, 5분 대본, 발표 근거 작성 및 1920×1080 렌더 검수.
- [x] 현재 상태를 `PARTIAL`/`UNREACHABLE`/`BLOCKED`로 구분하고 미완성 항목을 완료로 표현하지 않음.
- [ ] 다음 단일 개발 티켓은 기존 우선순위 1번 `BETA-010-PERSISTENCE-RECOVERY-001`로 유지한다.

## 2026-09-01 정체성 감사

- [x] 원안·현재 고정 정체성·Git 변천·실제 구현을 대조한 Identity & Scope Audit 작성.
- [x] 실제 역할 NPC 8종, 보리, 미사용 이름 프로필 5종과 경제 데이터를 창작 없이 정리.
- [x] Identity Implementation Matrix와 3~5분 Demo Identity/Run of Show 작성.
- [x] PPT를 수정하지 않고 발표 문장 후보 5종·Demo Identity Sentence 3종 및 선택안 기록.
- [ ] 다음 개발 티켓은 여전히 `BETA-010-PERSISTENCE-RECOVERY-001`; 정체성 감사가 자동으로 통합/구현 승인을 부여하지 않는다.
- [ ] 이후 사람 승인된 데모 통합 티켓에서 `유료 채용→생산자 매입→가공→가격 판단→NPC 소비→다음 날 마을 변화` 연속 경로를 검증한다.
- [ ] 보리와 역할 NPC의 개인 정체성 통합은 별도 기획 승인 전 이름·역할·외형을 임의 변경하지 않는다.

## 2026-09-06 — CONTENT-000B Campaign Architecture / SELF-AUDIT PASS

- CONTENT-000을 PROVISIONAL CANON v1으로 채택한 사람 승인과 CONTENT-000B~010 순차 진행·검증·로컬 커밋 선승인을 기록했다. 승인 대기는 해소됐다.
- CONTENT_CAMPAIGN_DAY1_30.md와 CONTENT_IMPLEMENTATION_BACKLOG.md를 작성했다. 주요 사건 14개, 첫 주 사건 7개×17항목, 기능 노출 25개, 핵심 주민 8명의 관계 사건 24개·개인 요청 16개를 기존 시스템에 배정했다.
- 실제 생산 재고 매입, 고용 전 작업지 거래→고용 후 상점 운반, 전문가 입력/요청 소모 구분, additive 저장과 동일 인물 복원, 자유·회복일 및 실제 Day30 완료 근거를 명시했다.
- 검증: Tools/LoopEngineering/Test-ContentCampaignArchitecture.ps1 PASS. 증거 Logs/Content/CONTENT000B/ArchitectureValidation.json. Unity runtime/editor compile·D3D11·실제30일·save restart는 설계 티켓에서 실행하지 않았다.
- 다음: 이 설계 체크포인트 local commit 뒤 CONTENT-001 Opening & Bori를 자동 활성화한다. 이후 CONTENT-010까지 중간 승인 요청 없이 진행한다. 기존 BETA-010 facing 137° 실패는 미해결 기술 이력으로 보존했다.
- 기존 사용자 변경 및 별도 ART-000 자료는 보존하고 이 작업 기록의 추가분만 커밋한다. 런타임 코드·Unity 씬/프리팹·ProjectSettings·Packages·Save schema 변경 없음.

## 2026-09-06 — ART-000 Asset Intake / VERIFIED

- 목표 파일의 ART-000 범위에서 공식 CC0 6팩 ZIP을 원본 보존 COPY로 입고했다. 550종/2,590파일을 분류하고 Blender 33종, Unity 28종(새 FBX20+기존 Nature8 재사용)을 선정했다.
- 산출물: Docs/AssetProvenance/EXTERNAL_ASSET_REGISTRY.md, PROJECT_PA_ART_STYLE_GRAMMAR.md, Blender/Library/ProjectPA_AssetLibrary.blend, demo 요구·missing-model·8개 제작 배치 계획, 원본 비교 렌더33장/시트3장.
- Unity 6000.3.2f1 D3D11 전용 검사: 28개 hash/mesh/크기/바닥 pivot/URP 재질/프리팹 참조 왕복 PASS. Runtime/Editor compile 오류0; 기존 CS8785/CS0414 경고는 보존. 기존 runtime/scene/settings 보호 hash 변경0.
- Blender 4.5.13 공식 portable 체크섬 검증 후 33개 library 생성·재개방·packed texture·render PASS. 물고기 원본 동작6개씩 모두 library에 보존했다. 원본 mesh/ZIP 변경 없음.
- 무결성: Docs/AssetProvenance/final-integrity-report.json PASS — 2,886 checks, GUID1,199개 중 중복0, 신규 missing meta0. 자세한 요구별 판정은 ART000_COMPLETION_AUDIT.md.
- 확인 못 함: 최종 Unity 장면의 1920x1080 시각 품질/낮밤 그림자/interaction face, 실제 gameplay 및 save/load 회귀, 최종 Vertical Slice Demo 완주. 이번은 격리 입고·제작 계획이며 ART-001 이후 적용 검증이 필요하다.
- 다음 ART 권장: ART-001 Material Palette and Unity Import Normalization. 목표 파일 §21에 따라 후속 ART 자동 구현은 시작하지 않는다. 별도 CONTENT-000B~010 승인·활성 상태와 사용자 staging은 유지한다.
- 사용자 원본 이동/Unity 수동 import 요구 없음. 파일/출처/검증이 바뀌지 않는 한 완료된 ART-000 intake를 재실행하지 않는다.

## 2026-09-07 — CONTENT-001 Opening & Bori / VERIFIED

- WorldSandbox 새 캠페인에 독립 생활자 보리, 전용 Profile/Dialogue, 기존 C-01 기반 별도 외형을 연결했다. Golden 보리(Profile_Lumberjack), 기존 8역할·후보·경제식을 보존했다.
- 실제 PlayerInteraction 인사와 실제 가방 판매 재고로 A01을 기록한다. 다른 NPC 대화로 완료되지 않으며 +2는 하루 한 번, 순서 교환·중복 시작·반복 로드에도 보상/주민/재고 중복이 없다.
- 승인된 additive envelope v13에 캠페인 4필드를 추가하고 world payload v11 및 v12 recovery 필드 의미를 유지했다. 과거 저장에 캠페인 시작을 강제하지 않는다. 로드 후 직전 채집 성공 문구를 지워 현재 재고와 일치시켰다.
- 검증 PASS: Logs/Content/CONTENT001/OpeningValidation_Release.log, RuntimeCompile_Release.log, EditorCompile_Release.log, GoldenRegression.log, WorldOnboardingRegression.log, WorldDaytimeRegression.log, SaveRoundTripRegression.log. Compile 오류0, 기존 CS8785/CS0414 경고는 남음.
- 1920×1080 Runtime/Opening.png에서 한글·새 안내 영역 잘림/겹침을 확인했다. 최종 전체 미술·기존 휴대폰 패널·플레이어 표현은 후속 통합/사람 검토 대상. BETA-010 restart 후 facing 137° 문제와 실제 첫 달 완주는 이번 검증 범위 밖이며 해결로 표시하지 않는다.
- 다음: CONTENT-001 선택 local commit 직후 CONTENT-002 First Shop Night 자동 활성화. 같은 보리를 실제 소비자로 연결하고 첫 판단과 첫 판매를 구분한다. 중간 승인 요청 없이 CONTENT-010 선승인 범위를 유지한다.
- 실패 교정 이력: editor sync 공개 API, 구체 Collider 선행 생성, batch 캡처를 일반 D3D11 GameView로 교정해 각각 해소. native crash 없음. 기존 사용자 dirty와 ART-000 기록 보존, 씬/기존 프리팹/ProjectSettings/Packages 변경0, push 없음.


## 2026-09-07 — 데모 창작 결정 상담 / 제안만 기록

- 사용자 요청에 따라 현재 개발 상태와 오프닝·인물·첫 판매 결과·첫 달 흐름의 창작 결정 가이드를 `Docs/DEMO_CREATIVE_DECISIONS.md`에 작성했다.
- 8월 25일 PROJECT_STATE와 9월 7일 CONTENT-001 완료/CONTENT-002 ACTIVE를 구분했다. CONTENT001 기존 로그의 CHECKS_PASS/VALIDATION_PASS를 읽었으며 이번 세션에서 새 runtime 검증을 수행한 것은 아니다.
- 배 도착→목수 루카의 길/가게 안내→보리의 생활 필요/첫 손님 역할은 미확정 제안이다. 현행 루카 Day4 소개와 차이를 명시했고 기존 캐논·콘텐츠 순서·선승인 범위·활성 티켓은 변경하지 않았다.
- 다음 상담 우선순위: 시작 장면, 안내자 역할, 첫 마을 변화, 체험 길이/마지막 장면에 대한 사용자 생각을 받는다. 구현 작업의 기존 우선순위 CONTENT-002는 유지한다.
- 문서 상담만 수행. 컴파일/Play Mode/빌드/재시작은 이번에 실행하지 않아 확인 못 함. 기존 사용자 미커밋 변경 보존, 코드·씬·저장 변경 및 commit/push 없음.


## 2026-09-07 — 무인도 정착·동행 선택 구상 검토 / 설계 상담

- 사용자 구상: 본사 교육→동행 NPC 선택→배로 무인도 이동→거주 겸 상점/주민 텐트 직접 배치→입주민에 따른 기술 트리→입주 후 고용. 적용 적합성을 검토하고 `Docs/DEMO_CREATIVE_DECISIONS.md`에 기존 정착지 권고의 한계를 명시했다.
- WORLD_NORTH_STAR의 절차 섬/자유 배치 방향과 부합한다. 현재 WorldGameplayAdapterService의 시설 자동 생성, HiringService의 고용 시 Instantiate, 레시피의 기존 시설/Tier 조건을 코드로 확인했다. 모든 주거/입주/해금이 이미 구현된 것으로 보고하지 않는다.
- 권고: 공통 설치 키트와 기본 채집, 입주→기술 전수와 유료 고용 분리, 주민 동일 ID·주거 앵커 저장, 기존 목수/벌목꾼·광부/대장장이 역할 구분. 선택하지 않은 분야는 후속 입주로 열고 첫 판매와 실제 마을 반응을 유지한다.
- 다음 설계 우선순위: 동행/입주/전수/고용 계약과 시작 설치 조건, 기존 날짜별 캠페인 충돌 범위 정리. 기존 활성 CONTENT-002나 선승인 sequence는 이 상담에서 변경/실행하지 않았다. 새 구상 전체의 구현 승인을 기존 승인에서 추론하지 않는다.
- 새 compile/Unity/빌드/save restart는 실행하지 않아 확인 못 함. 코드·씬·프리팹·저장·캐논 무변경, 기존 dirty 보존, commit/push 없음. 문서 diff 검사를 수행한다.


## 2026-09-07 — 1차 생산자·본사 기술 전수·섬 특화 / 아이디어 수집

- 사용자 추가 구상: 시작 동행은 1차 생산자(목수/광부/낚시꾼/곤충 채집꾼/농사꾼), 구성에 따른 초기 상품 차이, 돈을 통한 입주 확대, 직업별 자원 본사 제출로 기술 해금, 2차 생산과 관광·과학 등 섬 특화, 정기 배 방문객 연결. 상세는 Docs/DEMO_CREATIVE_DECISIONS.md 마지막 절.
- 이전 에이전트의 NPC 직접 기술 전수안과 본사 제출안을 구분했다. 목수의 1차 역할 표현은 보존하고 기존 전문가/벌목꾼 재배정, 입주비와 고용비의 관계, 가격·해금 수치, 후반 기능의 데모 필수 여부는 미확정으로 남겼다.
- 다음 대화 우선순위: 사용자가 이어서 서술할 아이디어를 누적한다. 확정 질문/구현 착수로 브레인스토밍을 중단하지 않는다. 기존 활성 티켓과 선승인 범위는 이번 기록에서 변경/실행하지 않는다.
- 문서만 수정했다. 신규 compile/Unity/빌드/밸런스 실험은 실행하지 않아 확인 못 함. 기존 사용자 변경 보존, 코드·씬·저장·캐논 무변경, commit/push 없음.


## 2026-09-07 — VS-PRESENT-001 P0 / PASS · STOP

- 별도 PA_DepartureTutorial 씬에서 실제 키보드 이동→나무 상호작용→열매3→ShopSlot 진열→ShopPriceUI 7G 확정→NPC 접근/평가→기존 Economy 0G→7G→출항 인증 완료를 연속 통과했다.
- Runtime/Editor compile 오류0(기존 CS8785/CS0414 경고 유지), D3D11 Play Mode, serialized reference 검사와 1920×1080 실캡처 PASS. 근거 Docs/Presentation/2026-09-08/P0-validation-excerpt.txt, P0-validation.json, P0-integrity.json.
- Blender 기존 Departure 자산5종과 ART-000 tree/cargo를 적용했다. 최종 화면은 01_PA_Company_FirstView.png / 02_Tutorial_PriceAndReaction.png. 전체보기는 실제 씬에서 HUD만 숨긴 캡처, 가격/반응은 실제 판매 후 남은 열매1개 재진열 상태다.
- 개발 진입: Project PA > Presentation > Open Departure Tutorial > Play. WASD/SPACE, 실습 가격7G. 튜토리얼 세션만 유지하며 SaveManager가 없어 기존 campaign save를 쓰지 않는다. NEW GAME/standalone 통합 및 저장 재개는 이번에 확인 못 함.
- 기존 dirty CONTENT/Save/World 코드를 수정하거나 이 checkpoint에 넣지 않았다. 이 작업은 현재 working tree의 기존 ShopPriceUI.OnPriceConfirmed 및 PurchaseFeedbackPresentationController.OnDecisionRecorded 관찰 seam을 사용하므로, checkpoint 단독 checkout은 기존 CONTENT 작업의 별도 checkpoint 없이는 재현 가능한 clean baseline이 아니다.
- P1/P2 NOT_STARTED. 사용자 최신 지시에 따라 고가 거절 edge case·추가 제작·다음 단계 구현은 수행하지 않고 STOP. 다음 세션은 사람의 직접 플레이 확인/명시 지시를 기다린다.


## 2026-09-07 — 현재 작업 통합 checkpoint / push 승인

- 사용자가 현재까지 작업의 commit/push를 명시 승인했다. 기존 P0 0b4e511에 이어 보존했던 CONTENT 코드·관찰 이벤트·설계/발표 자료를 함께 checkpoint한다. CONTENT-002의 미완료 검증 상태와 BETA 저장 부채는 해소됐다고 표시하지 않는다.
- Runtime/Editor compile 오류0, 기존 경고3, diff 검사 PASS. 증거 Logs/VS_PRESENT_001/PrePushCompile_Restored.log. 이번 Git 작업에서 새 Play Mode/저장/빌드 검증은 하지 않았다.
- 기존 Word 문서 삭제 상태는 그대로 기록한다. 로컬 개인 플러그인 설정 .claude/settings.json은 commit 대상에서 제외한다. 강제 push/이력 재작성 없음.
- P0가 사용하는 ShopPriceUI/PurchaseFeedback 관찰 이벤트가 이번 checkpoint에 포함되므로, 이전 P0 보고서의 '미커밋 이벤트 의존' 제한은 이 통합 checkpoint에서 해소된다. clean checkout 실행은 별도 수행하지 않았다.
- 다음은 사람의 P0 수동 확인/명시적 후속 지시다. P1/P2 및 CONTENT 후속 구현은 자동 시작하지 않는다.

## 2026-09-08 — VS-PRESENT-001 P1 재개 / 검증 실패 · STOP

- 사용자 요청: 현재 git 변경만 최소 확인하여 중단 작업을 마무리하고 검증 후 이번 변경만 로컬 commit, push 금지. 시작 HEAD는 e02c3a0. 기존 미추적 P1 코드 3종·meta·설정 프리팹·캡처·JSON을 확인했으며 .claude/settings.json은 제외했다.
- PA_DepartureContinuationChecks만 보강: 검증 중 프리팹 재생성 제거, 재실행 Task/오류 상태 초기화, 준비 단계까지 90초 제한, 미인증 P0 유지·필수 참조·SaveManager 부재·1명 확정 거부·unknown ID 거부 검사 추가. Runtime/Setup/기존 씬·저장·경제 코드는 수정하지 않았다.
- 컴파일: dotnet build Assembly-CSharp-Editor.csproj 통과(오류 0, 기존 CS8785/CS0414 경고 3), Unity 스크립트 재컴파일 후 Console 오류 0. 증거 Logs/VS_PRESENT_001/ResumeCompile.log. 최초 --no-restore는 임시 project.assets.json 부재(NETSDK1004)로 실행되지 않았고 일반 build 복원으로 해소했다.
- D3D11 기존 Editor에서 Run Companion Selection Checks 1회 실행: 미인증 진입 차단, 0/1명 출항 차단, 2명 활성화, 3번째 차단, 선택 취소/교체, 참조 검사까지 PASS. 이후 PA_SafeGameViewCapture의 GameView capture was not written in time: 03_CompanionSelection.png 예외로 [VS-P1] FAIL. 자동으로 Edit Mode 복귀했다. 증거 Logs/VS_PRESENT_001/ResumeP1_EditorExcerpt.log 및 Docs/Presentation/2026-09-08/P1-validation.json.
- 추가 관찰: 기존 ShopOpenSign.Awake → PrototypeWorldLabel.OnValidate → TextMeshPro 생성 경로에서 SendMessage cannot be called during Awake, CheckConsistency, or OnValidate 오류가 발생했다. 이번 P1 코드 원인으로 단정하지 않으며 기존 시스템을 임의 수정하지 않았다.
- 확인 못 함: 확정 후 ID 1회 전달/동결(캡처 다음 검사여서 미도달), P0 실제 인증 완료→P1 버튼 연결, 새 캡처 가독성, 전체 Golden 회귀, standalone, 저장 재시작. 기존 P1 PASS JSON은 과거 실행 기록으로 분리했고 이번 실행은 FAIL로 기록했다. 캡처 파일이 갱신됐더라도 검증 통과 증거로 인정하지 않는다.
- AGENTS.md의 실패 시 BUG_LOG 기록 후 중단 규칙을 적용했다. 추가 수정/재실행 없이 STOP, commit/push 없음. 다음 세션은 BUG_LOG의 캡처 타임아웃과 기존 OnValidate 오류를 먼저 확인한다. 정상화 후 같은 메뉴로 P1을 재검증하고, P0 실제 인증 완료 뒤 동행 선택을 수동 확인한다. P2 신규 구현·CONTENT/ART 자동 진행 없음.

## 2026-09-09 — VS-PRESENT-001-P1-RECOVERY PASS

- 사람의 recovery ticket으로 이전 STOP 해제. 기존 P1 코드/후보/프리팹을 보존하고 공용 GameView 캡처 lifecycle과 PrototypeWorldLabel/ShopOpenSign 초기화만 최소 교정했다. OnValidate 구조 생성 제거, ShopOpenSign 초기화 Start로 이동. 새 label/캡처 framework 없음.
- Runtime → Editor 순차 build 오류0, 기존 CS8785/CS0414 경고만 유지. D3D11 실제 GameView 첫 recovery 실행 PASS. P0 미인증 보호, 후보3/선택2, 0·1명 확정/unknown ID/3번째 거부, 취소·교체·참조, 확정 ID 1회 전달·동결 모두 PASS.
- 새 03_CompanionSelection.png 1920×1080, 229720 bytes, 2026-09-09T01:35:56Z. 기존 파일 기준선과 새 쓰기/연속 크기 안정화 확인. Camera.Render 호출 없음. blocking Console/OnValidate 오류/native crash 0. 종료 시 기존 JobTempAlloc 진단은 별도 기존 Editor 종료 부채로 남긴다.
- 증거: Docs/Presentation/2026-09-08/P1-validation.json, P1-recovery-excerpt.txt. 전체 로그와 순차 compile: Logs/VS_PRESENT_001/P1_Recovery_Editor.log, Recovery_RuntimeCompile.log, Recovery_EditorCompile.log.
- 범위: 기존 미커밋 P1 구현과 이번 recovery만 local checkpoint, 개인 .claude/settings.json 제외. P0/CONTENT/Blender/씬/Save/ProjectSettings 변경 없음. 실제 P0 인증 완료부터 연결은 이번 개발용 진입 검사에서 확인 못 함.
- 다음: 사용자 §13 승인에 따라 P1 local commit 후 VS-PRESENT-001-P2 자동 진행. 배/WorldGrid 재사용, 신규 권위 없음, push 금지.

## 2026-09-09 — VS-PRESENT-001-P2 PASS / local checkpoint 후 STOP

- P1 recovery 첫 실행 PASS 및 6b283c1 local commit 후 명시 승인된 P2 자동 진행. P1 선택 ID → 기존 배/실제 NPC2 → 갑판 WASD → 12초 항해 → fade → 같은 NPC2와 WorldGrid 섬 도착 → 섬 이동 PASS. 최종 캡처 조합은 광부·농부이며 벌목꾼·농부 조합도 앞선 P2 검사에서 PASS했다.
- 기존 WorldGridService/WorldChunkTerrain/WorldPlayerTraversalGuard/PlayerController/CameraController와 ART-000/P0 자산만 재사용. 새 presentation+전용 checks, 기존 continuation setup/prefab 연결. P0/Golden/MainGame/WorldSandbox 씬 및 WorldGrid·Save·경제 권위 소스, Packages/ProjectSettings 변경 없음.
- Runtime/Editor compile 오류0(기존 CS8785/CS0414 경고), D3D11 blocking Console 오류0/native crash0, 필수 참조/모델/청크 collider PASS. 최종 PNG2장 fresh stable write/1920×1080 확인. 증거 Docs/Presentation/2026-09-08/P2-validation.json, P2-validation-excerpt.txt; 전체 Logs/VS_PRESENT_001/P2_Presentation_Editor.log.
- P0 인증부터 연결한 전체 연속 플레이·standalone·save/load는 이번에 확인 못 함. P0 validator 재실행 없음. 생산 실행/입주/고용/채집 보상/placement preview/저장 확장은 DEFER. 개발용 메뉴 Play Companion Selection (Development Entry)로 바로 확인 가능.
- 이번 P2 변경만 local checkpoint 후 STOP. 개인 .claude/settings.json 보존/미포함, push 없음. 다음은 사람의 전체 첫 플레이 연결과 화면/조작감 확인이다.
