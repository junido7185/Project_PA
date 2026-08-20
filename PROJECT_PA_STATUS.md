# Project PA Status

## 2026-08-11 — BETA-002 COMPLETE / DAYTIME ACTIVITIES PLAYABLE

- WorldSandbox 원점 fallback에 있던 기존 낮 활동을 generated island에 연결했다: Forest Gathering, Meadow Farming, Highland Mining, Pond Fishing.
- M70 runtime player에 기존 `PlayerInteraction`과 `Hotbar`를 연결했다. 숲 활동은 실제 PlayerInteraction의 Space 탐색 경로로 검증했고 모든 보상은 canonical Inventory로 들어간다.
- 농사는 씨앗 주머니 Seed x2→고정 밭 심기→성장→Wheat x3 수확을 수행하며, 수확 완료는 기존 일일 활동 저장 목록에 `farm-harvest`로 기록된다.
- 광질은 타격 feedback 후 Ore x2, 낚시는 cast/catch 후 Fish x2를 지급한다. 숲 Carrot x2까지 합친 기존 Raw 상품 기본가는 하루 120G다.
- 당일 중복 보상은 차단되고 다음날 활동이 다시 열린다. 플레이어 HUD는 방향, 활동 완료, Carrot/Wheat/Ore/Fish 보유량을 표시한다. 원시 활동 큐브 renderer는 WorldSandbox에서 숨긴다.
- D3D11 BETA-002, BETA-001, WORLD-010, Prototype Gathering/Fishing, Mining/Shop 회귀 PASS; blocking Console 0, compile errors 0, new crash 0.
- Mining validator의 낡은 v10 고정 비교만 현재 v11 상수로 정합했다. Save schema/DTO, Scene, Prefab, Packages, ProjectSettings는 변경하지 않았다.
- Next: `BETA-003 Crafting and Production Expansion`.

## 2026-08-11 — BETA-001 COMPLETE / M85 GAMEPLAY BETA ACTIVE

- Started from clean `b176bdc` and created `milestone/gameplay-beta-85` under the explicitly preapproved `BETA-001`~`BETA-010` sequence.
- A fresh WorldSandbox session now starts on Day 1 at 09:00 and presents a player-facing new-life prompt. The first route progresses from WASD movement to the P.A. night shop, the crafting workbench, then daytime resource play.
- Shop/workbench role labels are attached to the existing runtime gameplay anchors. Landmark arrival uses a 4m interaction-scale radius so the nearby shop is not auto-completed at spawn.
- WORLD grid, placement, island-generation and navigation development surfaces default hidden. F10 restores all of them for diagnostics without serializing scene changes.
- D3D11 `BETA-001`, M70 WORLD-010 restart/save regression, Prototype Core Slice, and Final Demo Route all PASS with blocking Console 0. Runtime/Editor compile errors are 0; no new crash exists.
- Scene, Prefab, Packages, ProjectSettings, Save schema/DTO, MainGame, and `Prototype_FirstDay` content were not changed. Automatic capture remains `CAPTURE_EVIDENCE_DEBT` and is nonblocking.
- Next active ticket: `BETA-002 Daytime Activity Completion`.

## 2026-08-11 — LOOP-POLICY-002 COMPLETE

- `PREAPPROVED_MILESTONE_CONTINUATION`을 프로젝트 loop policy와 state에 추가했다. 기본 bounded-ticket 사람 게이트는 그대로이며, 명시적으로 선승인되고 state에 기록된 sequence만 자동 연속할 수 있다.
- M70 승인 sequence는 WORLD-005/006/006B/007/008/009/010으로 제한되며 ticket별 단일 active, 검증, 승인된 로컬 커밋, hard blocker, D3D11/crash 및 Git 안전 규칙을 유지한다.
- 현재 baseline `e0b5678`에서 M70이 이미 `M70_PLAYABLE_WORLD_ALPHA_COMPLETE`이므로 완료된 WORLD 구현을 재실행하지 않았다. 현재 `nextTicket=null`; WORLD-011과 MainGame은 새 사람 승인 전까지 비활성이다.
- 이번 정책 티켓은 게임 코드, Scene, Prefab, Packages, ProjectSettings, SaveData schema를 변경하지 않는다.

## 2026-08-11 — M70 PLAYABLE WORLD ALPHA COMPLETE

- `WORLD-001` through `WORLD-010` are complete on `milestone/world-alpha-70`. The final ticket baseline was `46f6515`.
- WorldSandbox now provides a connected playable alpha: existing movement/camera, provisional 128×128 generated island/64 chunks, gather, craft, terraform, B09 placement, movable functional B01 sales display, stocking, opening, NPC purchase, revenue, save and real restart/load.
- Existing Inventory/Crafting/Shop/NPC/Economy/DayNight/Save authorities remain canonical. No parallel gameplay, input, or save architecture was introduced.
- Primary validation: `Logs/WORLD010_M70_Validation_02.log` → `FINISHED_PASS M70_PLAYABLE_WORLD_ALPHA_COMPLETE`, D3D11, blocking Console 0.
- All requested Golden and WORLD-006B~009 regressions pass; Runtime/Editor compile errors and new crash artifacts are 0.
- Protected assets remain unchanged: Prototype_FirstDay Golden scene, WorldSandbox YAML, MainGame, prefabs, Packages, ProjectSettings, and SaveData schema/DTO.
- 128×128 is a provisional M70 target, not the permanent island-size decision. MainGame integration remains a separately approved future ticket.
- Remaining work is nonblocking human review/polish backlog: final terrain/coast/art feel, alpha UI/debug overlap, shop/map readability, drag ghost, phone hiring/feed empty-state, B06/audio polish, and capture evidence.

Inspection date: 2026-06-19
Project root: `C:\Users\sdjsd\Desktop\Unity\Project_PA`
Unity version: 6000.3.2f1

## 2026-07-16 B02~B04 Shop Evolution Visual Finalization — Complete

- B02/B03/B04 원본은 모두 지면 `minY=0`, 정상 메시·재질이며 실제 정면은 로컬 `-X`다. 작은 잡화점→민트 차양 마켓→맨사드 지붕 부티크의 단계가 기존 코지 마을 스타일과 일관되어 세 에셋 모두 분류 2(Unity 설정 수정)로 확정했다.
- 기존 래퍼 전체를 생성하면 단계마다 Shop 1개와 ShopSlot 8/16/32개가 기존 실내와 중복된다. 래퍼의 Visual만 참조하는 `Resources/VisualFinalization/ShopEvolution` 파생 프리팹 3개를 생성해 외관과 물리 범위만 교체한다.
- `ShopEvolutionController`는 기존 `currentTier`만 사용해 Tier 0=B10, Tier 1=B02, Tier 2=B03, Tier 3+=B04를 표시한다. 새 저장 필드나 병렬 진화 시스템은 추가하지 않았다.
- 활성 외관에 맞춰 기존 외부 문·외부 스폰·BuildingEntrance·3D 간판을 실제 모델 문 앞으로 이동한다. 기존 B10 렌더러·물리 범위는 상위 Tier에서만 숨기며, 한 번에 하나의 외관과 기존 하나의 Shop만 남는다.
- 실제 Play Mode에서 Tier 1/2/3 각각 접지, 문/플레이어/간판 정렬, 콜라이더, 카메라 가림, 단계별 스케일을 캡처했다. B02 첫 캡처에서 문 좌우 기준 오류를 발견해 anchor를 반대로 보정하고 같은 구도로 재검증했다.
- 단계 전환 전후 기존 `shop.interior` 배치 스냅샷은 instance/cell/rotation/recovered/function/storage까지 완전히 동일했다. 원본 FBX·텍스처·래퍼 프리팹·BuildingData/TierDefinition·메인 씬·저장 스키마·패키지는 변경하지 않았다.
- D3D11 PASS: 최종 외관 validator, Tier 0 잠금→Tier 1 개방→입장→6슬롯 진열→가격 UI→퇴장, FinalDemoRoute BreadLoaf 30G. `dotnet build` 런타임/Editor 오류 0(기존 CS8785 경고만 존재).
- 증거: `Logs/ShopEvolutionAudit/`, `Logs/ShopEvolution_RuntimeFinal_AnchorFix.log`, `Logs/ShopEvolution_EnterableShopRegression.log`, `Logs/ShopEvolution_FinalDemoRouteRegression.log`.

## 2026-07-16 Tripo Character Unity Finalization — Complete

- C-01~C-09 원본은 모두 valid Humanoid Avatar와 정상 메시/텍스처를 가진 일관된 캐릭터군으로 확인했다. 외형 교체나 Blender 수정 근거가 없어 분류 2(Unity 설정 수정)로 유지한다.
- 기존 런타임 정규화가 주민을 `2.088~2.190m`, Capsule/Agent를 `3.6m`로 덮어쓰던 원인을 제거했다. 주민은 개별 렌더 bounds 기준 `1.75m`, 발 clearance `0.02m`, Capsule/Agent `1.8m/0.4m`, stopping distance `0.75m`로 보정된다.
- NPC 보행 주기는 실제 NavMeshAgent 이동 거리 기준으로 계산하고, 플레이어는 기존 `Walk.anim` 평균 속도를 기준으로 기존 foot IK 컴포넌트에서 제한된 재생 속도 동기화를 수행하도록 구현했다. 원본 FBX·Avatar·클립·텍스처·씬·프리팹은 변경하지 않았다.
- D3D11 컴파일 PASS. 실제 idle 검증에서 주민 8명 높이 `1.748~1.751m`, 발 offset `+0.032~+0.035m`, 물리 몸체·Avatar·그림자가 모두 PASS했고 `character_idle_after.png`에서 플레이어/주민 비율과 접지가 개선됐다.
- 시작 온보딩을 기존 공개 복원 경로로 닫은 최종 walking 검증도 PASS했다. 플레이어는 5m/s에서 Animator/Walk playback `2.25×`, 주민 8명은 `2.5m/s`, cadence `1.851~2.328`, 보행 중 높이 `1.737~1.758m`, 발 offset `+0.029~+0.037m`를 유지했다.
- 실제 동서 도로의 게임 카메라 `character_walk_after.png`에서 9명 전원의 발·실루엣·방향·그림자를 확인했다. 길가 나무가 주민 1명의 몸통 일부와 겹치지만 식별과 발 자세는 읽히며 첫 상점 군집 캡처보다 명확히 개선됐다.
- 고객 경로 회귀 PASS: InteriorCustomer 예약→완전 경로→앞자리 정지→구매→복귀, CustomerArrival 초대 3/동시 2, FinalDemoRoute BreadLoaf 30G 판매.
- 주민 역할 할당도 별도 이슈다. 현재 Bori=C-03, Miner=C-04, Farmer/Fisher=C-05 중복, C-02 미사용으로 감사됐으며 캐릭터 정체성 변경이므로 임의 재매핑하지 않았다.
- 증거: `Logs/CharacterFinalization_AssetAudit.log`, `Logs/CharacterFinalization_RuntimeBaseline_Framed.log`, `Logs/CharacterFinalization_RuntimeFinal_FinalFraming.log`, `Logs/CharacterFinalization/character_source_lineup.png`, `character_idle_before.png`, `character_idle_after.png`, `character_walk_after.png`, `Logs/CharacterFinalization_*Regression.log`.

## 2026-07-16 B05 Workbench Functional Art Status

- B05 원본은 지면 `minY=0`, 15,629 vertices/14,689 triangles의 정상 작업대이며 상판·페그보드·스툴·서랍이 읽힌다. 전면 교체나 Blender 재제작 대상이 아니다.
- 기존 2×2 배치의 interaction 면은 로컬 `-Z`인데 모델 작업면은 반대쪽이었다. `Workbench`가 BasicWorkbench의 원본 Visual만 180° 정렬하고, 실제 BoxCollider/NavMeshObstacle을 보이는 모델에 맞춘다.
- 새 시스템 없이 기존 Wood→Plank 제작을 실제 역할로 확정했다. Project PA 재질로 원목 입력, 가이드 작업면, 완성 Plank, coral clamp를 가진 영속 파생 키트와 0.72초 성공 피드백을 만들었다.
- 실제 MainCamera 동일 구도 `b05_runtime_before.png`→`b05_runtime_after.png`를 확인했다. 플레이어와 작업면이 같은 상호작용 면을 향하며 첫 과광량은 2.2→0.9로 낮췄다.
- D3D11 실제 UI 제작 PASS: Wood 2개 소비, Plank 1개 생성, clamp/light 피드백, UI 닫기. 상점 2×2 배치/v10 저장, Tier 실내 왕복, 기존 BreadLoaf 가공, 최종 30G 판매도 회귀 PASS다.
- 원본 FBX·텍스처·래퍼 프리팹·메인 씬·저장 스키마는 변경하지 않았다. 다음 단일 비주얼 작업은 플레이어/NPC 같은 구도 보행·접지·발미끄럼·Animator/Collider/NavMeshAgent 최종화다.
- 증거: `Logs/B05_WorkbenchAudit/`, `Logs/B05_WorkbenchFinalAssets_Validation.log`, `Logs/B05_WorkbenchFinalPlayValidation_Final.log`, `Logs/B05_*Regression.log`.

## 2026-07-16 B10 Cottage Visual Finalization Status

- Unity Editor 4면 감사로 원본 B10 FBX가 손상되지 않았고 실제 정면이 로컬 `-X`임을 확인했다. 떠 있던 판은 FBX 조각이 아니라 잘못된 `-Z` 면에 놓인 원시 `PA_StoreDoor_Out`/간판이었다.
- 새 `CottageVisualFinalizationController`가 메인 맵 B10 3채의 실제 문 정면을 광장 쪽으로 맞추고 레거시 `[WorldBuildings]/B10_Cottage_Static` 중복을 런타임에서 끈다.
- 원본 모델의 문·차양·계단을 외형으로 사용한다. 원시 문 Renderer만 숨기고 기존 `BuildingEntrance`, Collider, Tier 잠금, 실내/외 spawn을 보존했다.
- Unity Editor API로 모따기 목재/크림 간판 메시와 Resource 프리팹을 제작했다. 외부 에셋이나 Blender는 사용하지 않았고 원본 FBX·텍스처·래퍼 프리팹·메인 씬은 변경하지 않았다.
- 동일 고정 구도 Before/After와 실제 Play Mode MainCamera를 직접 확인했다. 분리 판이 사라지고 `P.A. SHOP - Tier 1` 전체 문구가 고정 3D 간판 위에서 읽힌다.
- D3D11 PASS: B10 정면/간판/중복 검증, Tier 0 잠금→Tier 1 OPEN→실내 입장→진열→가격→퇴장, Day 1 BreadLoaf 30G 판매.
- 증거: `Logs/B10_CottageAudit/`, `Logs/B10_CottageFinalValidation_Final.log`, `Logs/B10_CottageRuntimeCapture_CompleteText.log`, `Logs/B10_EnterableShopRegression_Final.log`, `Logs/B10_FinalDemoRouteRegression.log`.
- 후속 B05 작업대 기능 아트도 완료됐다.

## 2026-07-16 Shop Customer Approach P4 Status

- `ShopCustomizationController` now exposes each rotated placement definition's existing `interaction` cells as read-only world approach points.
- New `ShopCustomerApproachController` reserves one reachable ShopSlot approach per NPC owner before movement. It accepts only `NavMesh.SamplePosition` + `CalculatePath=PathComplete` destinations.
- `NpcController` walks to the reserved front cell, stops within agent distance, faces the display, and then reuses the existing ShopSlot claim and PurchaseEvaluator flow.
- A moved/rotated shelf invalidates its stale approach reservation. A logically blocked authored interaction cell is rejected instead of falling back through furniture.
- Runtime reservations are intentionally not saved; purchase, economy, ShopSlot, scene, prefab, NavMesh bake, and save schema remain unchanged.
- D3D11 PASS: moved shelf `(4,0)/r3` approach `(3,0)`, two-owner exclusion/different-slot reservation, real interior reserve→approach→15G buy→return, customer arrival cap, and Day 1 BreadLoaf 30G regression.
- Evidence: `Logs/P4_ShopCustomizationValidator.log`, `Logs/P4_InteriorCustomerValidator.log`, `Logs/P4_CustomerArrivalRegression.log`, `Logs/P4_FinalDemoRouteRegression.log`, `Logs/DemoViewShots/p4_shop_approach_final_20260716_114141.png`.
- The top-down capture does not make the separation between both approach cells as legible as the FSM/path evidence. This is a future camera/interior presentation concern, not reported as visually final.

## 2026-07-16 Outdoor Placement P3 Status

- `village.outdoor`: 2m, 47×47, protected 212 cells for roads/plaza/entrances/spawns.
- Existing `BuildManager` now supports collider-derived multi-cell placement, 90° rotation, M relocate, and X safe recovery while retaining the legacy fallback.
- B09 is finalized for this milestone as a warm external 24-slot village storage shed. The authored shed is movable but protected from recovery; empty player-built sheds are recoverable.
- B09 placement and storage contents persist through the existing v10 placeables sidecar. No schema-version increase was required.
- Active legacy `[WorldBuildings]` B09 duplicates are suppressed at runtime when the authoritative map shed exists.
- D3D11 validation and SaveRoundTrip/ShopCustomization/FinalDemoRoute regressions passed. Evidence: `Logs/OutdoorPlacementValidator.log`, `Logs/OutdoorPlacement/20260716_111214/`, `Logs/OutdoorPlacement_*Regression.log`.
- Visual review passed for B09 scale, double-door readability, player approach, and control hint. B10 Cottage detached/floating door-wall pieces are the next obvious asset defect.

## Verification

- Confirmed working directory is the allowed Project_PA root.
- Confirmed Git root is the allowed Project_PA root.
- Confirmed Unity project folders exist: `Assets/`, `Packages/`, `ProjectSettings/`.
- Git repository is present on branch `master`, tracking `origin/master`.
- Initial Git status was clean: no modified, staged, or untracked files before creating this status/TODO documentation.

## Current Project Summary

Project_PA appears to be a Unity game prototype/final-project build for "P.A. / Pioneer Assistance". The current implementation looks like a playable management/adventure vertical slice with:

- Player movement and interaction.
- Inventory and hotbar UI.
- Shop slots, pricing, and economy flow.
- NPC dialogue, schedules, shopping/production behavior, friendship, and hiring.
- Crafting/workbench systems.
- Building/grid/save systems.
- Runtime UI creation/binding helpers.
- A guided playable-day scenario flow.

Initial inspection did not open Unity Editor, enter Play Mode, modify gameplay code, or build an executable.

## 2026-06-19 Visual Acceleration Update

Design guardrail:

- `PROJECT_PA_DESIGN_INTENT.md` is now the primary working standard.
- This pass preserved Project_PA as a reverse supply-chain management simulation.
- The market stall work is framed as an operating hub for supply, processing, pricing, purchase judgment, revenue, and reinvestment.

Files changed or created in this pass:

- Corrected `ProjectSettings/EditorBuildSettings.asset` so the missing `Assets/Scenes/SampleScene.unity` is no longer listed.
- Created scene backup: `Assets/Scenes/_Backups/Prototype_FirstDay_before_visual_acceleration_20260619.unity`.
- Added Editor-only helper: `Assets/Editor/PA_VisualAccelerationBuilder.cs`.
- Created Project_PA-owned market visual prefab: `Assets/Prefabs/Market/PA_MarketStall_Hub.prefab`.
- Created Project_PA-owned market materials under `Assets/Materials/Market/`.
- Updated `Assets/Scenes/Prototype_FirstDay.unity` with child-only visual staging around the existing shop.
- Updated `Assets/Scripts/PA_RuntimeSceneBinder.cs` with a narrow NavMesh/NPC-agent startup safety fix.
- Created `README.md` with source/build run instructions and demo route notes.
- Generated Windows build output under `Builds/Windows/`.

Scene additions made by the visual acceleration helper:

- `PA_MarketStall_Hub_Visual` under the demo shop.
- Wood frame, awning, product shelves, product crates, lantern accents, supply/process/price labels.
- Slot markers and price tag markers aligned around existing `ShopSlot` positions.
- `PA_DemoRoute_VisualMarkers` for talk -> stock -> price -> purchase -> revenue staging.
- NPC role badges for producer/specialist/guide/consumer readability.
- `PA_ScreenshotCameraMarker_MarketHub` as a non-gameplay presentation marker.

What was intentionally not changed:

- No Project_D files were copied or modified.
- No external packages were imported.
- No broad merge was performed.
- `Shop`, `ShopSlot`, `ShopPriceUI`, inventory, hotbar, economy, purchase evaluator, NPC purchase logic, save, tier, audit, hiring, and friendship gameplay code were not rewritten.
- `PlayableDayScenarioController` flow was not changed.
- No source or executable submission zip package was created.

Verification performed:

- Unity 6000.3.2f1 batch compile completed with exit code 0 after the changes.
- `PA_VisualAccelerationBuilder.VerifyPrototypeFirstDayStaging` passed.
- Verification counts: shops=2, shopSlots=8, hubVisuals=1, routeMarkers=1, roleBadges=8, screenshotMarkers=1, buildSettingsOk=True.
- Automated Play Mode smoke test now passes after fixing domain-reload-safe test state.
- Play Smoke counts after NavMesh repair: players=1, shops=2, shopSlots=8, economyServices=1, shopPriceUI=1, npcs=8, npcAgents=8/8, npcAgentsOnMesh=8, npcNearNavMesh=8.
- Fixed Windows-player NavMesh startup issue by rebaking the scene NavMesh, snapping 8 NPCs to the NavMesh, saving NPC NavMeshAgents disabled in the scene, and letting `PA_RuntimeSceneBinder` enable them after NavMeshSurface data is active.
- Windows build succeeded at `Builds/Windows/Project_PA.exe`.
- Windows player smoke reached runtime startup with `surfaces=1, agents=8/8, onMesh=8` and no `Failed to create agent because there is no valid NavMesh` messages.
- Logs show `PA_RuntimeSceneBinder` and `Shop.Start()` running in Play Mode.
- Manual Unity Editor review is still recommended for player feel, NPC movement, and the full stock -> price -> purchase -> money update route.

Known warnings:

- Unity logs include a non-blocking licensing token update warning.
- Existing editor warnings remain, including `PA_ErrorTracker._autoScrollNew` unused and a Unity source-generator warning.
- NavMesh repair logs include non-blocking TextMeshPro mesh skip messages during bake; NavMesh samples and NPC agent checks passed.
- No new C# compiler errors were found in batch compilation.

## 2026-06-19 Completion Loop Update

Design guardrail:

- This pass followed `PROJECT_PA_DESIGN_INTENT.md`.
- Project_PA remains a reverse supply-chain management simulation: the player manages stock, price, feedback, revenue, and growth while NPCs act as producers, consumers, specialists, and economic agents.
- ReferencePrototype/Project_D was not modified and no assets were copied.

Planning files created:

- `PROJECT_PA_COMPLETION_PLAN.md`
- `PROJECT_PA_RELEASE_BACKLOG.md`
- `PROJECT_PA_CURRENT_MILESTONE.md`

Gameplay clarity work implemented:

- CL-001 Management Loop Objective Rewrite: `PlayableDayScenarioController` now frames Day 1 objectives around NPC supply, shop stocking, pricing, customer reaction, revenue, audit/tier review, and saving.
- CL-002 NPC Purchase Feedback: `NpcController` now turns purchase evaluator outcomes into short buy/reject feedback messages and records recent feedback for the scenario summary.
- CL-003 Day Summary Improvement: Day 1 summary now includes revenue, money delta, sales count, relationship points, recent customer feedback, and a next management action.
- CL-004 Tier 0 Goal Clarity: `MoneyHUD`, `AuditResultUI`, and the Day 1 summary now read `TierDefinition` data to show next-tier revenue/reputation/manual approval requirements.

Files changed in this pass:

- `Assets/Scripts/UI/PlayableDayScenarioController.cs`
- `Assets/Scripts/NpcController.cs`
- `Assets/Scripts/UI/MoneyHUD.cs`
- `Assets/Scripts/AuditResultUI.cs`
- Planning/status docs listed in this file and TODO/session report.

Verification performed after CL-004:

- Unity batch compile completed with exit code 0.
- Automated Play Mode smoke passed.
- Play Smoke counts after CL-004: players=1, shops=2, shopSlots=8, economyServices=1, shopPriceUI=1, npcs=8, npcAgents=8/8, npcAgentsOnMesh=8, npcNearNavMesh=8.
- Windows build completed with `Build Finished, Result: Success`.
- Windows player smoke launched the built executable and reached runtime startup with `surfaces=1, agents=8/8, onMesh=8`.
- The player smoke log did not show the earlier NavMeshAgent startup failure.

Remaining manual validation:

- Manually complete the full route in Unity Editor and the Windows executable: move -> talk -> stock -> set price -> NPC buy/reject -> money update -> day summary.
- Check whether the expanded money/tier HUD is readable at 1920x1080 and does not cover important gameplay.
- Check whether NPC purchase bubbles are readable and not too long in the gameplay camera.

## 2026-06-19 Final Route Validation Update

Scope:

- Verified the CL-001 to CL-004 route after the management-clarity pass.
- Added an Editor-only validation tool, `Assets/Editor/PA_FinalDemoRouteValidator.cs`, to reproduce the route in Play Mode without changing runtime gameplay logic.
- Applied small readability fixes only:
  - Shortened NPC purchase/rejection feedback text.
  - Enlarged/wrapped `NpcBubbleUI`.
  - Removed emoji from `ShopPriceUI` reaction labels to avoid font fallback issues.

Automated route validation result:

- `Assets/Scenes/Prototype_FirstDay.unity` opened in Unity batchmode.
- Play Mode entered.
- Player movement components were present: `PlayerController`, `PlayerInputHandler`, and `CharacterController`.
- First NPC dialogue opened `DialogueUI`.
- A sellable item was stocked from hotbar into a `ShopSlot`.
- `ShopPriceUI` opened on the stocked slot.
- Price confirmation incremented `ShopPriceUI.ConfirmCount` and closed the UI.
- NPC purchase/rejection feedback text was generated and displayed in `NpcBubbleUI`.
- `ShopSlot.TryPurchaseByNpc` completed a sale.
- Money and cumulative revenue increased.
- `MoneyHUD` showed money, tier, and next-tier goal text.
- `AuditResultUI` showed next-tier goal text.
- `MoneyHUD` did not overlap the objective panel in the automated layout check.
- Day 1 summary appeared and included purchase feedback, next growth goal, and tier revenue progress.
- Market hub visual, route markers, NPC role badges, and screenshot marker were present.

Validation evidence:

- `Logs/Codex_FinalRoute_PlayValidation.log`
  - `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`
  - `PA Final Route Validation finished successfully.`
- `Logs/Codex_FinalRoute_WindowsBuild.log`
  - `Build Finished, Result: Success.`
- `Logs/Codex_FinalRoute_WindowsPlayerSmoke.log`
  - `NavMesh/NPC agents ready: surfaces=1, agents=8/8, onMesh=8`

Remaining manual review:

- Human visual review in Unity Editor is still recommended for actual camera composition, player feel, and subjective readability.
- The Windows executable was smoke-launched successfully, but a human should still complete the full route in the executable before final packaging.

## 2026-06-19 Final Presentation Review Update

Scope:

- Performed a final presentation-readability pass after automated route validation.
- Kept the project aligned with `PROJECT_PA_DESIGN_INTENT.md`: Project_PA remains a reverse supply-chain management simulation, not a simple shop clone.
- Did not modify Project_D, copy assets, import packages, push to GitHub, or rewrite core shop/economy/NPC/tier/save logic.

Minimal UI/readability changes:

- Added Editor-only reviewer `Assets/Editor/PA_FinalPresentationReviewer.cs`.
- Adjusted objective panel runtime placement so it no longer sits too low over the market hub.
- Enlarged Day 1 summary body area only for the summary state.
- Updated `AuditResultUI` next-tier text to wrap inside the phone audit card.
- Reworked `NpcBubbleUI` as a screen-space follower so purchase/rejection feedback remains readable in the demo camera.
- Hid active NPC bubbles when opening the smartphone or Day 1 summary so feedback does not cover management panels.

Final presentation validation evidence:

- `Logs/Codex_FinalPresentation_Review11.log`
  - `PA Final Presentation Review passed.`
  - Market hub, route markers, NPCs, shop stocking, `ShopPriceUI`, NPC feedback bubble, audit app, MoneyHUD/objective layout, and Day 1 summary body fit all passed.
- Captures generated under `Logs/FinalPresentation/20260619_232447/`:
  - `01_market_hub_objective.png`
  - `02_shop_price_ui.png`
  - `03_npc_feedback_bubble.png`
  - `04_audit_app_goal.png`
  - `05_day1_summary.png`
- `Logs/Codex_FinalPresentation_RouteValidation.log`
  - Final route validation passed again after the UI changes.
- `Logs/Codex_FinalPresentation_WindowsBuild.log`
  - `Build Finished, Result: Success.`
- `Logs/Codex_FinalPresentation_WindowsPlayerSmoke.log`
  - Built exe launched and reached `NavMesh/NPC agents ready: surfaces=1, agents=8/8, onMesh=8`.

Current validation status:

- Unity compile/build pipeline has no blocking C# errors.
- Automated Play Mode route passed after final UI/readability fixes.
- Final presentation screenshots confirm the main HUD, market hub, price UI, NPC purchase feedback, audit app, and Day 1 summary are readable.
- Windows executable was rebuilt and smoke-launched successfully.
- A real human should still do one final hands-on pass for subjective player feel and route pacing before packaging.

## 2026-06-19 Submission Packaging Readiness Update

Scope:

- Reflected the Windows executable final-route status for submission readiness.
- No gameplay code, scene, Project_D asset, external package, Git commit, or Git push was performed in this packaging-readiness pass.

Windows exe manual route result:

- On 2026-06-19 this result was still pending because the human-at-keyboard executable route result had not been supplied.
- Superseded by the 2026-06-20 Final Submission Packaging Update below: the human executable route is now recorded as passed.
- Automated evidence remains strong: final route validation, final presentation review, Windows build, and Windows smoke launch all passed.
- Final package creation is no longer blocked by this item.

Packaging plan, ready after manual exe route pass:

- Source package target name: `Project_PA_Source_20260619.zip`.
- Executable package target name: `Project_PA_Windows_20260619.zip`.
- Optional report/presentation attachment should be chosen separately from `Docs/`.

Source package include list:

- `Assets/`
- `Packages/`
- `ProjectSettings/`
- `Docs/`
- Root project documents: `PROJECT_PA_*.md`, `README.md`, `.gitignore`
- Solution/project files if required by the course review workflow: `Project_PA.slnx`, `Assembly-CSharp.csproj`, `Assembly-CSharp-Editor.csproj`

Source package exclude list:

- `.git/`
- `Library/`
- `Temp/`
- `Logs/`
- `Builds/`
- `UserSettings/`
- `obj/`, `.vs/`, cache/generated folders, and any local IDE/cache output

Executable package include list:

- `Builds/Windows/Project_PA.exe`
- `Builds/Windows/Project_PA_Data/`
- `Builds/Windows/UnityPlayer.dll`
- `Builds/Windows/UnityCrashHandler64.exe`
- `Builds/Windows/DirectML.dll`
- `Builds/Windows/D3D12/`
- `Builds/Windows/MonoBleedingEdge/`
- `README.md` or a copied run-instructions text file

Executable package exclude list:

- `Builds/Windows/Project_PA_BurstDebugInformation_DoNotShip/`
- `Logs/`
- Any source/cache folder not required to run the executable

## 2026-06-20 Final Submission Packaging Update

Scope:

- Recorded the user-confirmed human Windows executable final-route result as passed.
- Created separate source and executable submission packages inside `SubmissionPackages/`.
- No Project_D files were modified or copied.
- No external packages were imported.
- No gameplay logic, scene layout, or UI behavior was changed during this packaging pass.
- No Git commit or push was performed.

Windows exe human route result:

- Passed by human execution of `Builds/Windows/Project_PA.exe`.
- Confirmed route: move -> talk -> stock item -> set price -> NPC buy/reject feedback -> money change -> audit app -> Day 1 summary.

Created packages:

- `SubmissionPackages/Project_PA_Source_20260620.zip`
  - Size: about 359.17 MiB
  - Entries: 1,369
- `SubmissionPackages/Project_PA_Windows_20260620.zip`
  - Size: about 84.25 MiB
  - Entries: 183

Package verification:

- Source package was created from `Assets/`, `Packages/`, `ProjectSettings/`, `Docs/`, root `PROJECT_PA_*.md`, `README.md`, `.gitignore`, and solution/project files.
- Source package excludes `.git/`, `Library/`, `Temp/`, `Logs/`, `Builds/`, `UserSettings/`, `obj/`, `.vs/`, `SubmissionPackages/`, `GeneratedAssets_deleted/`, and generated cache folders.
- Executable package includes the runnable Windows build files and `README.md`.
- Executable package excludes `Project_PA_BurstDebugInformation_DoNotShip/`, logs, source folders, cache folders, and generated debug/cache output.
- Both zip files were opened after creation and scanned for forbidden entries.

## Known Scenes

Project scenes found under `Assets/`:

- `Assets/Scenes/MainGame.unity`
- `Assets/Scenes/Prototype_FirstDay.unity`
- `Assets/Art/Free RPG Icons/Demo.unity`
- `Assets/Jinxish/Drag & Drop Inventory & Hotbar Framework/Scenes/Really Simple Demo Scene.unity`
- `Assets/_Recovery/0.unity`

Main project scenes:

- `Prototype_FirstDay.unity` appears to be the best current demo candidate. It includes prototype/demo flow names and references to systems such as `PlayableDayScenarioController`, `PA_RuntimeUI`, `[Services]`, `ShopSlot`, `SmartphoneUI`, `MoneyHUD`, and core services.
- `MainGame.unity` appears to be a fuller scene with player, NPCs, shop slots, workbenches, runtime UI, map root, and services.

Build settings:

- `ProjectSettings/EditorBuildSettings.asset` now enables:
  - `Assets/Scenes/Prototype_FirstDay.unity`
- Previous blocker fixed: missing `Assets/Scenes/SampleScene.unity` was removed from Build Settings.
- `MainGame.unity` exists but is not currently listed in build settings.

## Known Scripts And Systems

Runtime scripts under `Assets/Scripts/` include these major systems:

- Core/game flow: `GameManager`, `PlayerController`, `PlayerInputHandler`, `PlayerInteraction`, `CameraController`, `PauseManager`, `ScreenFader`.
- Inventory/hotbar/items: `Inventory`, `Hotbar`, `InventorySlot`, `InventoryUI`, `InventorySlotUI`, `Item`, `ItemInstance`, `ItemRegistry`, `EquipmentSystem`, `PickupItem`, `ItemPickupHandler`, `ItemTooltip`.
- Economy/shop: `EconomyService`, `Shop`, `ShopSlot`, `ShopPriceUI`, `PurchaseEvaluator`, `SalesLogManager`, `SaleRecord`.
- Crafting/production: `CraftingService`, `CraftingUI`, `RecipeData`, `Workbench`, `ProductionData`, `ProducerNpcController`, `SpecialistNpcController`.
- NPC/social: `NpcController`, `NpcDialogue`, `NpcProfile`, `NpcDailySchedule`, `NpcScheduleController`, `NpcSpecialty`, `DialogueData`, `DialogueService`, `FriendshipService`, `FriendshipUI`, `HiringService`, `HiringUI`, `NpcCandidateData`, `NpcBubbleUI`.
- World/building/save: `BuildManager`, `BuildingData`, `BuildingEntrance`, `BuildingRegistry`, `GridService`, `StorageBox`, `StorageUI`, `SaveData`, `SaveManager`, `LocalJsonSaveRepository`.
- Time/progression: `GameClock`, `DayNightVisual`, `TierService`, `TierDefinition`, `SeasonModifier`, `AuditService`, `AuditResultUI`.
- Demo/UI flow: `PlayableDayScenarioController`, `SmartphoneUI`, `ClockHUD`, `MoneyHUD`, `InteractPromptUI`, `FeedUI`, `SettingsUI`, `PrototypeWorldLabel`, `PA_RuntimeSceneBinder`.

Editor scripts under `Assets/Editor/` include automation and validation helpers:

- `PA_PlayableDayBuilder`
- `PA_SceneAutoBuilder`
- `PA_ContentAutoIntegrator`
- `PA_DataBootstrapper`
- `PA_DataCreator`
- `PA_DevConsole`
- `PA_FeatureChecker`
- `PA_MapLayoutBuilder`
- `PA_SceneValidator`
- `PA_SystemHub`
- `PA_UIBuilder`
- `PA_Week11MilestoneValidator`
- Other repair/import/check utilities.

## Known Prefabs And Data

Project prefabs found under `Assets/Prefabs/` include:

- Building/shop/storage/world prefabs such as `Building_Chest`, `Building_Shop`, `Farmland`, `Crop_Corn`, `UI_Slot`, `Recipe_Slot_Prefab`, trees/rocks/furniture, and selection/ghost-floor prefabs.
- Building prefabs `B01_MarketStall` through `B12_TradePort` under `Assets/Prefabs/Buildings/`.
- Additional third-party/example prefabs under the Jinxish drag-and-drop inventory framework.

Scriptable/data assets exist under `Assets/Resources/` and `Assets/ScriptableObjects/`, including:

- Items
- Recipes
- Tiers
- NPC profiles
- NPC candidates
- Dialogue data
- Production data
- Building data
- Schedules

## UI And Menu Status

- There is no obvious dedicated title/menu scene found under `Assets/Scenes/`.
- UI appears to be mostly runtime-driven and/or embedded in the gameplay scenes.
- `PlayableDayScenarioController` appears to create an onboarding/demo flow UI with player name/map selection and stage objectives.
- `SmartphoneUI` appears to provide in-game app panels such as audit, hiring, feed, and settings.
- HUD/UI systems include money, clock, dialogue, interaction prompt, crafting, shop pricing, friendship, and pause/settings.

## Existing Builds And Submission Assets

- Windows build output now exists at `Builds/Windows/Project_PA.exe`.
- The full `Builds/Windows/` folder is required to run the executable.
- Submission zip packages now exist under `SubmissionPackages/`.
- Source package: `SubmissionPackages/Project_PA_Source_20260620.zip`.
- Windows executable package: `SubmissionPackages/Project_PA_Windows_20260620.zip`.
- `Builds/Windows/Project_PA_BurstDebugInformation_DoNotShip/` is generated debug information and should normally be excluded from the final executable package unless specifically required.
- Existing documentation/materials include multiple files under `Docs/`, including planning/design docs, development log, roadmap/spec docs, guides, a Week 11 presentation, and proposal/report files.
- A root `.tmp_template_proposal.pptx` exists and should be reviewed before final packaging because it looks temporary.

## Gameplay Status Review

What appears playable now:

- A first-day guided prototype loop likely exists in `Prototype_FirstDay.unity`.
- Player movement, interaction, shop slot stocking, price setting, NPC shopping/sales, money display, dialogue, and smartphone/hiring/settings UI appear implemented.
- `PA_RuntimeSceneBinder` adds or repairs runtime services/UI after scene load, which may make scenes more resilient.

Likely demo flow:

1. Open `Prototype_FirstDay.unity`.
2. Enter Play Mode.
3. Follow the playable-day objective UI.
4. Interact with NPC/dialogue, stock a shop slot, set a price, observe NPC shopping/sales, and review UI panels.

Known uncertainty:

- Initial inventory did not include manual Unity Editor Play Mode.
- Automated batch Play Smoke has since passed, but manual Unity Editor full-route verification is still needed.
- Compilation was verified by Unity batchmode, but the human-facing Editor Console still needs a final manual check.
- Scene files are partly binary/serialized, so the review is based on file inventory, build settings, script content, and searchable scene strings.
- Logs did not show current C# compiler errors, but logs are not a substitute for a final manual Unity review.

## Current Blockers

1. Need manual Unity Editor review for player feel and NPC movement.
2. Need manually complete the full stock -> price -> NPC purchase -> money update route.
3. Need UI readability check at 1920x1080 in Play Mode.
4. Need final report/presentation selection from existing `Docs/` files.
5. Optional demo video not found yet.

## Submission Readiness

- Unity Editor Play Mode test: Batch Play Smoke passed with NPC NavMesh validation; manual full-route review still recommended.
- Unity automated route test: Passed through dialogue, stocking, price confirmation, purchase, money update, HUD, audit text, and summary checks.
- Windows executable build: Build succeeded, smoke-launched, and the final executable route was confirmed by human review.
- Source code zip: Created at `SubmissionPackages/Project_PA_Source_20260620.zip`.
- Windows executable zip: Created at `SubmissionPackages/Project_PA_Windows_20260620.zip`.
- Final report/presentation: Materials exist under `Docs/`, but final attachment choice needs review.
- Optional demo video: Not found.

## Recommended Next Tasks

1. Open Unity 6000.3.2f1 and load `Prototype_FirstDay.unity`.
2. Check Console for compile errors before entering Play Mode.
3. Enter Play Mode and manually complete the first-day demo route.
4. Keep `SubmissionPackages/Project_PA_Windows_20260620.zip` and `SubmissionPackages/Project_PA_Source_20260620.zip` as the current final package candidates.
5. Review the new market hub visuals from the gameplay camera.
6. Check UI readability at 1920x1080.
7. Choose or update final report/presentation material from `Docs/`.
8. Optionally record a short demo video after the executable route is validated.

## Submission Checklist

- [x] Project opens in Unity 6000.3.2f1 via batch validation.
- [x] Console has no C# compile errors in batch validation.
- [x] `Prototype_FirstDay.unity` enters Play Mode.
- [x] Main demo loop passes automated route validation.
- [x] Build Settings no longer reference missing `SampleScene.unity`.
- [x] Final start scene is confirmed as `Assets/Scenes/Prototype_FirstDay.unity`.
- [x] Windows executable build succeeds.
- [x] Source/executable package include-exclude plan is defined.
- [x] Executable package is created after human exe route pass.
- [x] Source package is created without cache/generated folders after human exe route pass.
- [x] README/run instructions are included.
- [ ] Final report or presentation is selected/attached.
- [ ] Optional demo video is recorded and attached.

## 2026-06-20 Full Game Development Mode Update

Project_PA is now being tracked as a long-term Project_PA 1.0 development project, not only as a submission prototype. The existing submission build and packages remain preserved as a stable snapshot, while new development targets a cozy reverse supply-chain management life-sim.

New planning documents:

- `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md`
- `PROJECT_PA_FULL_GAME_BACKLOG.md`

Safety and baseline checks:

- Confirmed working root: `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Confirmed Git root: `C:/Users/sdjsd/Desktop/Unity/Project_PA`.
- Confirmed Unity folders: `Assets/`, `Packages/`, `ProjectSettings/`.
- Confirmed main scene exists: `Assets/Scenes/Prototype_FirstDay.unity`.
- Created scene backup before long-play work: `Assets/Scenes/_Backups/Prototype_FirstDay_before_fullgame_longplay_20260620_155934.unity`.
- Unity batch compile after long-play code changes exited successfully.
- Compile log note: Unity source-generator warning remains non-blocking; no C# compile failure was reported.
- Long-play Play Mode validator could not run in batch because another Unity instance already had this project open. The open Editor was not force-closed.

Implemented first full-game sprint foundation:

- Added `LongPlayProgressionController` as a sidecar runtime service.
- Added Day 1-7 long-play objective plan data.
- Added Day 2-7 daily NPC producer buy-in deliveries using existing `EconomyService` and `Inventory` paths.
- Added runtime long-play HUD text for day goal, daily/total revenue target, and producer delivery result.
- Added save schema v7 fields for long-play delivery/day baseline state.
- Updated `SaveManager` to save/load/migrate the v7 long-play fields.
- Updated `PA_RuntimeSceneBinder` to attach `LongPlayProgressionController` automatically.
- Added `PA_LongPlayProgressionValidator` for Day 2-7 progression smoke validation.

Current implementation reclassification:

- Strong foundation: Day 1 route, shop stocking, price UI, purchase evaluation, money/revenue, tier/audit UI, inventory/hotbar, save/load, NPC/hiring/friendship/crafting/building extension points.
- Demo-bound: `PlayableDayScenarioController` remains Day 1/tutorial oriented and should not become the whole long-term progression system.
- Long-play blockers being reduced: Day 2-7 now has a first-pass operations layer and producer delivery loop, but it still needs manual Play Mode verification and richer processing/hiring/town growth integration.
- Expandable: `GameClock.OnNewDay`, `ProductionData`, `EconomyService`, `ShopSlot`, `TierService`, `AuditService`, and `SaveManager` can support fuller progression if changes stay incremental.
- Risky systems: avoid broad rewrites of `Shop`, `ShopSlot`, `ShopPriceUI`, `Inventory`, `Hotbar`, `EconomyService`, `PurchaseEvaluator`, `NpcController`, `ProducerNpcController`, `SaveManager`, `TierService`, and `AuditService`.

Next validation needed:

- Close or reuse the currently open Unity Editor, then run the long-play validator:
  `PA_LongPlayProgressionValidator.RunLongPlayProgressionValidation`
- Manually confirm in Play Mode that Day 1 route is still intact.
- Advance or simulate Day 2-7 and verify producer deliveries, inventory growth, money spend/refund, day objective HUD, and save/load.

## 2026-06-20 Long-Play Validation Update

Scope:

- Verified FG-001/FG-002 first implementation pass after moving Project_PA into full-game development mode.
- Kept the work inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Did not modify Project_D, copy reference assets, import packages, push to GitHub, or rewrite core shop/economy/NPC/tier systems.

Minimal fixes made during validation:

- Fixed `SaveManager.SaveGameAsync` so `data.version = CurrentSaveVersion` is definitely executed before JSON serialization.
- Extended `PA_LongPlayProgressionValidator` to use a project-local validation save repository at `Logs/LongPlayValidationSaves/`.
- Added Day 3 save/load validation to confirm long-play day, money, delivered inventory, long-play supply day, day-start revenue, day-start money, and HUD text restore correctly.

Validation performed:

- Unity batch compile log: `Logs/Codex_LongPlay_Compile.log`
  - Result: passed.
  - No `error CS`, `Script compilation failed`, or `Compiler Error` entries found.
- Day 1 route validation log: `Logs/Codex_LongPlay_Day1RouteValidation2.log`
  - Result: passed.
  - Evidence: `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`.
  - Evidence: `PA Final Route Validation finished successfully.`
- Long-play progression validation log: `Logs/Codex_LongPlay_ProgressionValidation.log`
  - Result: passed.
  - Day 2-7 producer delivery simulations all returned true.
  - Day 2-7 producer deliveries increased sellable inventory.
  - Day 3 save/load restored current day, money, long-play supply day, day-start revenue, day-start money, delivered inventory, and HUD.
  - Final evidence: `PA Long Play Validation passed. sellableInventory=38, money=4633G`.
  - Final evidence: `PA Long Play Validation finished successfully.`

Current FG-001/FG-002 status:

- Day 1 onboarding route is preserved by automated validation.
- Day 2-7 first-week long-play loop is validated in Play Mode automation.
- Day 3 save/load persistence for long-play state is validated through a project-local save repository.
- Remaining work is now design depth, not blocker repair: richer producer presentation, processing-chain decisions, customer segmentation, and manual player-feel review.

## 2026-06-20 FG-004 Processing Chain First Pass

Scope:

- Started the next automatic sprint after FG-001/FG-002 validation passed.
- Goal was to make raw producer goods vs processed goods visible as a management decision without rewriting crafting.
- Kept existing `CraftingService`, `RecipeData`, `Workbench`, and `CraftingUI` behavior intact.

Implemented:

- Added `ProcessingOpportunityController`.
  - Loads existing `Resources/Recipes` data.
  - Calculates input base value, expected processed output value, and estimated margin.
  - Detects whether the player currently owns the required inputs.
  - Shows a small Day 4+ processing focus panel.
- Updated `PA_RuntimeSceneBinder` to attach `ProcessingOpportunityController`.
- Added `PA_ProcessingChainValidator`.
  - Selects an existing recipe.
  - Seeds required inputs at runtime.
  - Confirms the advisor finds a ready processing chain.
  - Crafts through existing `CraftingService.TryCraft`.
  - Confirms processed output is created.

Validation:

- Compile log: `Logs/Codex_ProcessingChain_Compile.log`
  - Result: passed.
  - Only the existing non-blocking Unity source-generator warning appeared.
- Processing chain validation log: `Logs/Codex_ProcessingChain_Validation.log`
  - Result: passed.
  - Evidence: `PA Processing Chain Validation passed. recipe=BreadLoaf, margin=3G`.
  - Evidence: `PA Processing Chain Validation finished successfully.`
- Long-play regression log after adding the processing advisor: `Logs/Codex_ProcessingChain_LongPlayRegression.log`
  - Result: passed.
  - Evidence: `PA Long Play Validation passed. sellableInventory=38, money=4633G`.

Current FG-004 status:

- First-pass processing opportunity layer is implemented and validated.
- Processing is now visible as a management signal starting around Day 4.
- Future work should deepen this into facility availability, customer demand for processed goods, and better recipe balancing.

## 2026-06-20 FG-003 Customer Demand Insight First Pass

Scope:

- Continued automatic development after FG-004 first pass.
- Goal was to expose customer demand signals without changing purchase probability math.
- Kept `PurchaseEvaluator` behavior intact.

Implemented:

- Added `CustomerDemandInsightController`.
  - Observes `PurchaseEvaluator.Result`.
  - Tracks category-level evaluations, buys, average interest, and latest customer signal.
  - Shows a Day 3+ demand signal panel.
- Updated `PA_RuntimeSceneBinder` to attach `CustomerDemandInsightController`.
- Added a narrow observer hook in `NpcController` after purchase evaluation.
  - The hook records the result for UI insight.
  - It does not change `willBuy`, probability, pricing, stock, sale, money, or NPC FSM behavior.
- Added `PA_CustomerDemandInsightValidator`.

Validation:

- Compile log: `Logs/Codex_CustomerDemand_Compile.log`
  - Result: passed.
  - Only the existing non-blocking Unity source-generator warning appeared.
- Customer demand validation log: `Logs/Codex_CustomerDemand_Validation.log`
  - Result: passed.
  - Evidence: `PA Customer Demand Insight Validation passed. item=BreadLoaf, category=Processed`.
- Day 1 regression log: `Logs/Codex_CustomerDemand_Day1Regression.log`
  - Result: passed.
  - Evidence: `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`.
- Long-play regression log: `Logs/Codex_CustomerDemand_LongPlayRegression.log`
  - Result: passed.
  - Evidence: `PA Long Play Validation passed. sellableInventory=38, money=4633G`.

Current FG-003 status:

- First-pass customer demand insight layer is implemented and validated.
- The player can now receive management-facing demand signals from customer evaluations.
- Future work should connect this to weekly demand trends, customer segments, pricing advice, and audit/reputation goals.

## 2026-06-21 Creative North Star Lock

Scope:

- Documentation-only direction reset for long-term full-game development.
- No gameplay code, scene, UI, prefab, material, build, package, or Project_D changes were made in this pass.

Created:

- `PROJECT_PA_CREATIVE_NORTH_STAR.md`

Updated planning direction:

- Project_PA is now framed as a cozy 3D life and shop management simulation.
- The player should live in the village by day, prepare goods through activities/relationships, run a personal shop at night, and grow the village through what they sell.
- Reverse supply-chain systems remain the economic backbone, but should support the cozy day-to-night loop instead of replacing it.
- NPC producer/specialist systems should gradually reduce repetitive labor and support growth.
- The shop/stall is now both the night shop hub and the visible interface where daytime goods, customer feedback, settlement, and village-change signals meet.
- Project_D / ReferencePrototype and VisualTargets remain reference-only material and are not merge/copy sources.

New Milestone 1 target:

- Milestone 1 is now `Cozy Day-To-Night Shop Loop`.
- Required first implementation targets: two daytime stock-preparation activities, shop open/close state, product display/pricing, at least two customer types, buy/reject feedback, daily settlement, next-day change/unlock, and one product category affecting village change.

Recommended next implementation focus:

1. CN-001 Day/night phase display and shop open/close state.
2. CN-002 Two Project_PA-owned daytime stock sources or MVP equivalents.
3. CN-003 Product-category village-change signal in settlement.
4. Customer type presentation pass.
5. Regression validation for Day 1 route and long-play loop.

## 2026-06-21 Milestone 1 First Implementation Update

Scope:

- Began `Milestone 1 - Cozy Day-To-Night Shop Loop` implementation.
- Kept the work inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Did not modify Project_D, copy reference assets, import packages, push to GitHub, or rewrite core shop/economy/NPC/save logic.

Implemented:

- Added `DayNightShopLoopController` as a runtime sidecar service.
  - Tracks `DayPreparation`, `ShopOpen`, and `Settlement` phase state.
  - Exposes `CurrentPhase`, `IsShopOpen`, `CanCollectDayPrepStock`, and `LastActivityResult`.
  - Keeps Day 1 tutorial shop access open so the existing final route remains intact.
  - Creates a small top HUD showing day/time, phase, shop open/closed state, and latest stock-prep result.
- Added `DaytimeStockPrepPoint` as the first two daytime stock-preparation MVPs.
  - Runtime-created as Project_PA-owned `Garden Prep Basket` and `Producer Drop Box` stock sources.
  - Adds a valid sellable item through existing `Inventory.AddInstance`.
  - Each source can be collected once per day during `DayPreparation`.
- Added `VillageChangeSignalController` as the first visible product-category-to-village-change signal.
  - Reads recent `SalesLogManager` records only.
  - Shows which sold product category is shaping village direction.
  - Does not change purchase probability, tier, save, NPC behavior, money, or shop logic.
- Updated the Day 1 summary/settlement text to include a `Village direction` section from `VillageChangeSignalController`.
- Updated `PA_RuntimeSceneBinder` to attach the new sidecar services automatically.
- Added Editor validation tools:
  - `Assets/Editor/PA_DayNightShopLoopValidator.cs`
  - `Assets/Editor/PA_VillageChangeSignalValidator.cs`

Validation:

- Day/night loop validation passed:
  - Log: `Logs/Codex_DayNight_TwoPrep_Validation.log`
  - Evidence: `PA Day Night Shop Loop Validation passed. sellableInventory=4`
- Village change signal validation passed:
  - Log: `Logs/Codex_VillageSignal_Validation.log`
  - Evidence: `PA Village Change Signal Validation passed. summary=Processed: 2 sale(s), 76G influence`
- Day 1 route regression passed after the new Milestone 1 layer:
  - Log: `Logs/Codex_SettlementVillage_Day1Validation.log`
  - Evidence: `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`
  - Evidence: `PA Final Route Check OK: Day 1 summary includes village direction section`
- Day 2-7 long-play regression passed after the new Milestone 1 layer:
  - Log: `Logs/Codex_SettlementVillage_LongPlayRegression.log`
  - Evidence: `PA Long Play Validation passed. sellableInventory=38, money=4633G`

Current Milestone 1 status:

- Day/night phase state: first pass complete.
- Shop open/close state: first pass complete and Day 1 route preserved.
- Daytime stock prep: two MVP activities complete.
- Product-category village-change signal: first pass complete as read-only UI.
- Daily settlement integration: first pass complete through Day 1 summary text.
- Manual Game-view readability review: still recommended for the new top/right HUD panels.

## 2026-06-25 Unity Editor Crash Repair

Scope:

- Investigated a Unity Editor hard crash that opened Bug Reporter.
- Confirmed this was not a normal C# Console compile error.
- Kept work inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`.

Finding:

- Latest `Editor.log` shows `Unrecoverable D3D12 device error`.
- Key evidence:
  - `d3d12: swapchain present failed (887a0005)`
  - `d3d12: Device removed reason (887a0006)`
  - native stack includes `D3D12SwapChain::Present`
  - log says GPU local/non-local memory was not exhausted
- Cause classification: D3D12/GPU driver/device-removed path, not Project_PA C# compile, NavMesh, save, or UI managed exception.

Repair:

- Updated `ProjectSettings/ProjectSettings.asset`.
- Standalone graphics API is now fixed to Direct3D 11:
  - `m_BuildTarget: Standalone`
  - `m_APIs: 02000000`
  - `m_Automatic: 0`

Verification:

- `Logs/Codex_CrashRepair_D3D11_Load.log`
  - `Forcing GfxDevice: Direct3D 11`
  - `Tundra build success`
- `Logs/Codex_CrashRepair_ProjectGraphicsAPI_Load.log`
  - Loaded without `-force-d3d11`
  - `Version: Direct3D 11.0`
  - `Tundra build success`
- `Logs/Codex_CrashRepair_FinalRoute_D3D11.log`
  - `Version: Direct3D 11.0`
  - `PA Final Route Validation passed. stocked=BreadLoaf, paid=30G`

Report:

- `PROJECT_PA_CRASH_REPORT_20260625.md`

## 2026-06-22 SPY-002 Customer Type / Preference Presentation

Scope:

- Implemented Milestone 1's remaining customer-type requirement: make NPCs read as residents with their own tastes, and explain why each buys or rejects.
- Kept the work inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Did not modify Project_D, copy reference assets, import packages, commit, or push.
- Did not change `PurchaseEvaluator`, `Shop`, `ShopSlot`, `ShopPriceUI`, `EconomyService`, NPC FSM, or the Save schema.

Implemented (read-only presentation layer):

- `Assets/Scripts/UI/CustomerPreferencePresentationController.cs`
  - Top-right "관심 손님 성향" panel.
  - Lists NPCs currently moving to / browsing the shop with a short preference hint.
  - Hint is derived only from traits that actually differ across the eight profiles: `traitSN` (category preference), `traitTF` (buy style), `traitEI` (eagerness).
  - `priceSensitivity`/`utilityConsumption`/`luxuryConsumption` are all 1.0, so no per-NPC price label is fabricated.
- `Assets/Scripts/UI/PurchaseFeedbackPresentationController.cs`
  - Bottom-right "손님 반응" feed.
  - Turns each `PurchaseEvaluator.Result` into a cozy buy/reject reason with no probabilities/debug fields.
  - Adds a "마을 변화" tie line connecting the sold category (or a display/price nudge on reject) to village direction.
- `Assets/Scripts/NpcController.cs`
  - One read-only hook in `EvaluateCurrentSlot`, mirroring the existing `CustomerDemandInsightController` hook. No effect on `willBuy`, sale, money, inventory, or FSM.
- `Assets/Scripts/PA_RuntimeSceneBinder.cs`
  - Registers both controllers automatically.
- `Assets/Editor/PA_CustomerPresentationValidator.cs`
  - New Editor-only Play Mode validator for SPY-002.

Validation:

- Batch compile: `Logs/Codex_SPY002_Compile.log` — no `error CS`.
- `PA_CustomerPresentationValidator`: `Logs/Codex_SPY002_PresentationValidation.log` — passed.
  - `utilHint='실용재(식료품·도구) 선호 · 감성 구매형'`
  - `luxHint='장식·고급품 선호 · 신중형'`
  - cozy buy/reject reasons generated; no `%` in the feed panel.
- `PA_FinalDemoRouteValidator`: `Logs/Codex_SPY002_FinalRouteRegression.log` — passed (`stocked=BreadLoaf, paid=30G`; head bubble text unchanged: `Lumberjack_01: 가격 적정, 구매 (73%)`).
- `PA_DayNightShopLoopValidator`: `Logs/Codex_SPY002_DayNightRegression.log` — passed.
- `PA_VillageChangeSignalValidator`: `Logs/Codex_SPY002_VillageRegression.log` — passed.
- `PA_LongPlayProgressionValidator`: `Logs/Codex_SPY002_LongPlayRegression.log` — passed.

Still required:

- Manual 1920x1080 Game-view readability review of the two new panels (checklist in `Docs/CustomerPresentation/README.md`). Automated validation only checked data/string logic, not on-screen layout.

## 2026-06-22 SPY-003 Per-Resident Consumption Data

Scope:

- Closed the data-honesty gap left by SPY-002: `priceSensitivity`, `utilityConsumption`, and `luxuryConsumption` were all 1.0 on every profile, so the "가격에 민감/관대" preference hint could never appear from real data.
- Changed only ScriptableObject asset values, not any code logic. `PurchaseEvaluator` math is unchanged.
- Did not modify Project_D, import packages, commit, or push.

Data change (`Assets/Resources/NPCs/Profile_*.asset`, eight files):

- Varied per resident archetype within 0.6 ~ 1.5:
  - Price-sensitive: Farmer (1.35), Miner (1.45).
  - Price-tolerant: Tailor (0.65).
  - Neutral: Blacksmith (0.9), Fisher (0.9), Chef (1.1), Carpenter (1.0), Lumberjack (1.15).
  - `utilityConsumption`/`luxuryConsumption` set to reflect each job (e.g., Chef utility 1.5, Tailor luxury 1.4, Miner luxury 0.6).
- `CustomerPreferencePresentationController.DescribePreference` already reads `priceSensitivity` (>=1.25 민감, <=0.75 관대), so the hint became data-driven with no code change to the controller.

Validator update:

- `Assets/Editor/PA_CustomerPresentationValidator.cs` now also asserts that Miner (1.45) shows "가격에 민감", Tailor (0.65) shows "가격에 관대", and a price-neutral profile (Blacksmith) falls back to a personality style.

Why this is safe (no regression):

- `priceSensitivity` only affects purchase probability when display price exceeds the ideal price (ratio > 1). FinalDemoRoute stocks at base price (ratio <= 1), so it is unaffected.
- `utilityConsumption`/`luxuryConsumption` only scale Utility/Luxury category bonuses. FinalDemoRoute sells BreadLoaf (Processed), whose bonus does not use those multipliers.
- LongPlay money comes from deterministic producer buy-ins, not live NPC sales.

Validation (all passed):

- Batch compile: `Logs/Codex_SPY003_Compile.log` — no `error CS`.
- `PA_CustomerPresentationValidator`: `Logs/Codex_SPY003_PresentationValidation.log` — `장식·고급품 선호 · 가격에 민감` (Miner), `실용재(식료품·도구) 선호 · 가격에 관대` (Tailor).
- `PA_FinalDemoRouteValidator`: `Logs/Codex_SPY003_FinalRouteRegression.log` — `stocked=BreadLoaf, paid=30G`.
- `PA_DayNightShopLoopValidator`: `Logs/Codex_SPY003_DayNightRegression.log`.
- `PA_VillageChangeSignalValidator`: `Logs/Codex_SPY003_VillageRegression.log`.
- `PA_LongPlayProgressionValidator`: `Logs/Codex_SPY003_LongPlayRegression.log` — `money=4633G` unchanged.

Still required:

- Manual 1920x1080 Game-view review still pending (data/string logic only is automated).

## 2026-06-22 SPY-002 Panel Layout Validation

Scope:

- Replaced the "human must check panel overlap" gap with a coordinate-based automated layout validator, plus a 1920x1080 Game-view screenshot for human reference.
- Presentation/QA only: no gameplay, economy, or NPC logic changed.

Added:

- `Assets/Editor/PA_CustomerPanelLayoutValidator.cs`
  - Enters Play Mode at 1920x1080, dismisses the onboarding modal for a clean shot.
  - Computes screen rects (`RectTransform.GetWorldCorners`) for the two SPY-002 panels and the existing HUD.
  - Asserts: both new panels are within screen bounds; `관심 손님 성향` (top-right) does not overlap MoneyHUD/Demand/Village; `손님 반응` (bottom-right) does not overlap the top-right stack; the two new panels do not overlap each other; `손님 반응` does not overlap the hotbar (computed from active `InventorySlotUI` rects).
  - Captures `Logs/CustomerPanelReview/<timestamp>/customer_panels_1920x1080.png`.
  - All panels share the same `ScaleWithScreenSize(1920x1080, match 0.5)` canvas, so overlap/containment is scale-invariant and valid at 1920x1080 even when batch resolution differs.

Result:

- Passed: the two new panels do not overlap each other, MoneyHUD, the Demand panel, or the Village panel, and stay within screen bounds.
- The 1920x1080 screenshot was generated and visually inspected: LongPlay (top-left), Day/Night (top-center), MoneyHUD + Village Direction (top-right), hotbar (bottom-center), and the `손님 반응` feed (bottom-right) all sit in distinct regions. The center "개척자 등록" dialog is the transient Day-1 onboarding modal, not a persistent HUD, and does not touch the corner panels.
- Logs: `Logs/Codex_PanelLayout_Validation.log`. Screenshot: `Logs/CustomerPanelReview/20260622_093117/customer_panels_1920x1080.png`.

Resolution (2026-06-22, completed):

- The improved validator (hotbar region from active `InventorySlotUI`, onboarding modal dismissed) recompiled cleanly and re-ran.
- Run 2 (`Logs/Codex_PanelLayout_Validation2.log`) caught a real overlap: the `손님 반응` (PurchaseFeedbackPanel) bottom-right panel overlapped the right edge of the hotbar.
- Fix: raised `PurchaseFeedbackPresentationController.BuildUI` `anchoredPosition.y` from 24 to 170 so the panel sits above the hotbar. Presentation-only (one coordinate); no controller logic, gameplay, economy, NPC, or save change.
- Run 3 (`Logs/Codex_PanelLayout_Validation3.log`): all checks pass — both panels within bounds; no overlap with MoneyHUD/Demand/Village, the hotbar, or each other; `finished successfully`.
- Clean screenshot (onboarding modal dismissed): `Logs/CustomerPanelReview/20260622_112554/customer_panels_1920x1080.png`. Visual check: feedback panel sits above the hotbar; right-side panels are separated from the top-right Money/Village stack; Korean renders without tofu/boxes.
- After the panel move, all five existing validators were re-run and passed: `Logs/Codex_PanelLayout_Reg_{Presentation,FinalRoute,DayNight,Village,LongPlay}.log` (`paid=30G`, `money=4633G` unchanged).

Still required:

- A human should still confirm subjective readability and Korean font rendering on a real monitor; the automated pass covers geometry, not aesthetics.

## 2026-06-24 IL-001 + CDN-002 Real Gathering & Night Shop Gate

Scope:
- Strengthened Milestone 1 into a real playable loop: gather by day → stock/price → open the shop at night so customers can buy → settle → next-day gather resets.
- Kept work inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`. No Project_D edit/copy, no packages, no commit/push.
- Did NOT rewrite `PurchaseEvaluator`, `Shop`, `ShopSlot`, `EconomyService`, NPC FSM, or the Save schema shape (additive v8 only).

Implemented:
- IL-001 real daytime gathering: `DayNightShopLoopController` now creates 3 spread wild-forage points (`숲길 채집`/Carrot, `해변 채집`/Fish, `들판 채집`/Wheat) reusing `DaytimeStockPrepPoint`, existing Raw items, daily reset, and sellable `ItemInstance`. Forage colliders are triggers (NPC pathing not blocked). Garden Prep Basket / Producer Drop Box kept as backup/NPC support.
- CDN-002 night shop gate: `IsShopOpenForCustomers` (Day 1 tutorial always open; Day 2+ needs ShopOpen phase + player opens via `ShopOpenSign`). Gate is one guard block in `NpcController.EvaluateCurrentSlot`; closed-shop NPCs hold and wander with an occasional "가게 열면 다시 올게요" bubble.
- Save v8: `SaveData.dayPrepCollectedDay` + `dayPrepCollectedActivities`; `SaveManager` v7→v8 migration + save/restore hooks; `DayNightShopLoopController.WriteSaveFields/RestoreSavedState`.
- New `ShopOpenSign.cs` (IInteractable) open action.

Files changed/created:
- `Assets/Scripts/DayNightShopLoopController.cs`, `Assets/Scripts/ShopOpenSign.cs` (new), `Assets/Scripts/NpcController.cs`, `Assets/Scripts/SaveData.cs`, `Assets/Scripts/SaveManager.cs`.
- `Assets/Editor/PA_GatheringShopGateValidator.cs` (new), `Assets/Editor/PA_GatheringShopReview.cs` (new), `Assets/Editor/PA_DayNightShopLoopValidator.cs` (1-line update: collect every forage point before asserting exhausted).
- `Docs/IslandLife/GATHERING_AND_SHOP_GATE.md` (new) + planning docs.

Validation (all passed):
- Compile `Logs/Codex_IL001_Compile2.log` (no `error CS`).
- `PA_GatheringShopGateValidator` `Logs/Codex_IL001_GateValidation.log` (`gatherPoints=5, gatheredInventory=2, shopGate=OK`, 33 checks).
- Regressions: `Logs/Codex_IL001_Reg_FinalRoute.log` (`paid=30G`), `Logs/Codex_IL001_Reg_DayNight2.log`, `Logs/Codex_IL001_Reg_LongPlay.log` (`money=4633G`), `Logs/Codex_IL001_Reg_Presentation.log`, `Logs/Codex_IL001_Reg_Village.log`, `Logs/Codex_IL001_Reg_PanelLayout.log`.
- Screenshots `Logs/GatheringShopReview/20260624_141557/` (5 states), visually reviewed: phase HUD + Korean render OK.

Still required (human):
- Real-input play-feel pass: walk-to-gather distance, NPC pathing around forage cubes, night-open → customers actually arrive, Day 1 first sale still unblocked.
- Low-poly visual polish for the placeholder forage/sign cubes; move forage points to fixed scene terrain.

## 2026-06-24 Customer Arrival Pacing (IL/CDN follow-up)

Scope: after CDN-002 gated customer purchases, make opening the shop actively bring customers so the night-open action feels meaningful. Read-only/event-based; no `PurchaseEvaluator`/`ShopSlot`/`EconomyService`/NPC FSM rewrite; no Project_D edit, no packages, no commit/push.

Implemented:
- `Assets/Scripts/CustomerArrivalController.cs` (new sidecar, registered in `PA_RuntimeSceneBinder`). Polls `IsShopOpenForCustomers`; on Day 2+ open it invites idle customers one at a time up to `maxConcurrentCustomers` via existing `NpcController.SetShoppingPriority`/`TryForceShop`; disperses on close. Day 1 tutorial stays passive (scenario controller keeps managing Day 1 flow); paused/shopping NPCs are ignored by `TryForceShop`.

Validation (all passed):
- New `PA_CustomerArrivalValidator` — `Logs/Codex_Arrival_Validation.log` (`npcs=8, invited=3, capped=2`, 16 checks).
- Regressions: `Logs/Codex_Arrival_Reg_{Gate,FinalRoute,DayNight,LongPlay,Presentation,Village,PanelLayout}.log` — all passed (FinalRoute `paid=30G`, LongPlay `money=4633G` unchanged).
- Compile `Logs/Codex_Arrival_Compile2.log` (no `error CS`).

Still required (human): play-feel tuning of invite interval/concurrent cap; placeholder forage/sign visual polish.

## 2026-06-25 Day 1-3 Core Slice Baseline

Scope:

- Shifted the current work away from submission-prototype cleanup and toward a full-game Day 1-3 core slice.
- Followed `PROJECT_PA_CREATIVE_NORTH_STAR.md` and `PROJECT_PA_DESIGN_INTENT.md`.
- Kept work inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Did not modify Project_D, copy reference assets, import packages, push to GitHub, or rewrite core shop/economy/NPC/save logic.

Created:

- `PROJECT_PA_CORE_SLICE_PLAN.md`
  - Defines Day 1-3 as the smallest playable version of the full cozy management life-sim.
  - Separates final player-facing HUD/world elements from development/presentation overlays.
  - Preserves the reverse supply-chain backbone while reframing it through day activity -> night shop -> settlement -> next-day planning.

Implemented:

- `Assets/Scripts/CoreSlicePresentationMode.cs`
  - Runtime presentation-only sidecar.
  - Hides development/advisor canvases by default without deleting them: `LongPlayProgressionCanvas`, `ProcessingOpportunityCanvas`, `CustomerDemandInsightCanvas`, `VillageChangeSignalCanvas`, `CustomerPreferenceCanvas`, and `PurchaseFeedbackCanvas`.
  - Hides presentation/debug route markers by default: `PA_DemoRoute_VisualMarkers`, `PA_PathStep_*`, `PA_DemoRoute_Label`, `PA_CustomerApproach_Label`, `PA_Reinvestment_Label`, `PA_EconomicRoleBadge`, and `PA_ScreenshotCameraMarker_MarketHub`.
  - Keeps core player HUD visible: objective, money/tier, clock, hotbar, interaction prompt, dialogue, shop price UI, smartphone/audit app, NPC bubble feedback, and Day/Night phase HUD.
  - Development overlays can be toggled at runtime with `F10`.
- Registered `CoreSlicePresentationMode` in `PA_RuntimeSceneBinder`.
- Updated `Assembly-CSharp.csproj` with the new script include for local `dotnet build` verification; Unity may regenerate this file later.

Validation:

- `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` passed with 0 warnings and 0 errors after adding the script `.meta`.
- Attempted `PA_FinalDemoRouteValidator.RunFinalDemoRouteValidation`, but Unity batchmode aborted because the project was already open in another Unity Editor instance.
- Attempted `PA_LongPlayProgressionValidator.RunLongPlayProgressionValidation`, but Unity batchmode aborted for the same open-project lock.

Current core-slice status:

- Day 1-3 design baseline is documented.
- Development/advisor overlay clutter is hidden by default while keeping validation objects available.
- Core gameplay systems remain preserved.
- Final Unity Play Mode validation still needs to be run from the currently open Editor, or after closing the Editor and rerunning batchmode.

## 2026-06-25 Day 1-3 Core Slice Playability Pass

Scope:

- Continued the long-term full-game core-slice direction rather than submission packaging.
- Kept work inside `C:\Users\sdjsd\Desktop\Unity\Project_PA`.
- Did not touch Project_D, copy reference assets, import packages, push to GitHub, or rewrite core shop/economy/NPC/save logic.

Implemented:

- Added `Assets/Editor/PA_CoreSlicePlayabilityValidator.cs`.
  - Opens `Assets/Scenes/Prototype_FirstDay.unity`, enters Play Mode, and checks that player-facing HUD roots exist.
  - Verifies development/advisor canvases are hidden by default through `CoreSlicePresentationMode`.
  - Verifies the same overlay path can be restored and hidden again through `SetDevelopmentOverlaysVisible`, matching the runtime F10 debug-toggle intent.
  - Captures a reference screenshot under `Logs/CoreSlicePlayability/<timestamp>/` when it can run.
- Moved `DayNightShopLoopPanel` below the Day 1 objective HUD as a compact supporting strip.
  - This is presentation-only: no phase, shop gate, economy, NPC, inventory, or save behavior changed.

Validation:

- `dotnet build Assembly-CSharp.csproj -nologo -v:minimal` passed with 0 errors.
- `dotnet build Assembly-CSharp-Editor.csproj -nologo -v:minimal` passed with 0 errors. Existing warnings remain: Unity source-generator `CS8785` and unused `PA_ErrorTracker._autoScrollNew`.
- Batch `PA_CoreSlicePlayabilityValidator` was attempted but aborted because the project is already open in Unity Editor:
  - `Logs/Codex_CoreSlice_PlayabilityValidation.log`
  - Reason: `Multiple Unity instances cannot open the same project`.

Still required:

- In the open Unity Editor, run `Project PA/Validation/Run Core Slice Playability Validation`.
- In the open Unity Editor, run `Project PA/Validation/Run Final Demo Route Validation`.
- In the open Unity Editor, run `Project PA/Validation/Run Long Play Progression Validation`.
- Human Game-view pass: confirm F10 physically toggles overlays, basic player view hides development panels/path labels, and Day 1-3 flow reads as day prep -> night shop -> customer reaction -> settlement -> next-day plan.

## 2026-06-26 Loop Engineering Dry-Run Guardrails

Scope:

- Documentation/tooling only. No gameplay, scene, prefab, material, UI behavior, ProjectSettings, Project_D, package, commit, or push work.
- Added shared agent guidance so Codex/Claude work from the same Project_PA rules.
- Added dry-run loop infrastructure that stops before automated implementation when the project baseline needs human approval.

Created:

- `AGENTS.md`
- `CLAUDE.md`
- `Docs/AgentWorkflow/CONTEXT_INDEX.md`
- `Automation/LoopEngineering/loop-policy.json`
- `Automation/LoopEngineering/State/loop-state.json`
- `Automation/LoopEngineering/progress.md`
- `Automation/LoopEngineering/ticket-template.md`
- `Automation/LoopEngineering/Tickets/LOOP-DRYRUN-001.md`
- `Automation/LoopEngineering/validator-registry.json`
- `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1`
- `Automation/LoopEngineering/RunLogs/preflight-20260626.json`

Updated:

- `Docs/07_개발일지.md` now includes the missing June records from CL-001 through Core Slice Playability and Loop Engineering.

Preflight result:

- `BLOCKED_BY_DIRTY_GIT`
- Root and Git root are correct.
- Unity process count for Project_PA was 0 during the check.
- Latest crash report exists: `PROJECT_PA_CRASH_REPORT_20260625.md`.
- Human baseline approval is required before enabling any implementation loop.

## 2026-06-26 Baseline Commit Review

Scope:

- Git/documentation review only.
- No gameplay code, scene, UI behavior, assets, ProjectSettings, commit, push, reset, or clean command was run.

Created:

- `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md`

Summary:

- Current branch: `master`
- Last commit: `adc5d2f 프로토타입 구성 -1`
- Git status lines: 141
  - Modified tracked paths: 27
  - Untracked paths: 114
- `git diff --stat`: 27 tracked files, 3173 insertions, 129 deletions
- `.gitignore` exists and already ignores `Library/`, `Temp/`, `Logs/`, `Builds/`, `UserSettings/`, `obj/`, `.vs/`, and `*.log`.
- Review document splits dirty state into:
  - A. recommended baseline commit paths
  - B. include only after user review
  - C. recommended exclusions
- `SubmissionPackages/` is untracked and large, about 465 MB, so it is marked as user-review before inclusion.
- Crash baseline summary from `PROJECT_PA_CRASH_REPORT_20260625.md` confirms the D3D12 crash was worked around with D3D11 and needs human launch confirmation before automation is enabled.

Next:

- User should inspect `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md`, stage approved groups manually, verify `git diff --cached --stat`, then make a local baseline commit if desired.

## 2026-06-26 VC-001A Village Culture Visual Change

Scope:

- Implemented one next-day plaza/market visual response from an existing sold product category.
- Chosen category: `Processed`, because the current Day 1 route sells `BreadLoaf` and existing `VillageChangeSignalController` reads `Processed` from `SalesLogManager`.
- Used only Project_PA runtime primitives/materials; no Project_D asset, scene, material, prefab, or script was copied.

Implementation:

- Added `Assets/Scripts/VillageCultureVisualController.cs`.
- Added `Assets/Editor/PA_VillageCultureVisualValidator.cs`.
- Runtime object: `PA_VillageCultureVisualController`.
- Visual object: `PA_VillageCulture_Processed`.
- Day 1 start: visual inactive.
- After Processed sale: pending next-day change, visual still inactive.
- Next `DayPreparation`: processed-goods prep corner becomes visible near the market hub and a one-time hint appears.

Preserved systems:

- No Save schema change.
- No scene file edit.
- No package or ProjectSettings edit.
- No core rewrite of `Shop`, `ShopSlot`, `ShopPriceUI`, `Inventory`, `EconomyService`, `PurchaseEvaluator`, `NpcController`, `SaveManager`, or day/night logic.

Validation:

- `PA_VillageCultureVisualValidator` passed.
- `PA_FinalDemoRouteValidator` passed.
- `PA_DayNightShopLoopValidator` passed.
- `PA_VillageChangeSignalValidator` passed.
- `PA_LongPlayProgressionValidator` passed.
- `PA_CustomerPresentationValidator` passed.
- `PA_CustomerPanelLayoutValidator` passed.

Evidence:

- Logs:
  - `Logs/Codex_VC001A_VillageCultureVisual.log`
  - `Logs/Codex_VC001A_FinalDemoRouteRegression.log`
  - `Logs/Codex_VC001A_DayNightRegression.log`
  - `Logs/Codex_VC001A_VillageSignalRegression.log`
  - `Logs/Codex_VC001A_LongPlayRegression.log`
  - `Logs/Codex_VC001A_CustomerPresentationRegression.log`
  - `Logs/Codex_VC001A_CustomerPanelLayoutRegression.log`
- Screenshots:
  - `Logs/VillageCultureVisual/20260626_145257/vc001a_day1_default_no_change.png`
  - `Logs/VillageCultureVisual/20260626_145257/vc001a_after_processed_sale_same_day.png`
  - `Logs/VillageCultureVisual/20260626_145257/vc001a_day2_preparation_visual_active.png`

Next:

- Human visual review should judge whether the primitive processed-goods corner reads clearly enough.
- Future village-culture work can add category variants for `Raw`, `Utility`, and `Luxury` using the same next-day activation rule.

## 2026-06-26 BASELINE-001 Bounded Ticket Loop Baseline Preparation

Scope:

- Documentation and loop-harness baseline preparation only.
- No gameplay code, scene, UI, prefab, material, asset, Package, ProjectSettings, Project_D, Unity launch, Unity batch validator, Git add, commit, push, reset, or clean command was performed.

Current Git snapshot:

- Branch: `master`
- Last commit: `adc5d2f 프로토타입 구성 -1`
- Changed file/status entries using `git status --porcelain=v1 -uall`: 144
- Modified tracked paths: 27
- Untracked paths/files: 117
- `git diff --stat`: 27 tracked files, 3173 insertions, 129 deletions
- Unity Editor process for Project_PA: none detected during this check.

Created/updated:

- Updated `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md`.
- Created `Automation/LoopEngineering/State/crash-resolution.json`.
- Updated `Automation/LoopEngineering/loop-policy.json`.
- Updated `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1`.
- Updated `Automation/LoopEngineering/State/loop-state.json`.
- Updated `Automation/LoopEngineering/progress.md`.

D3D11 stability record:

- User stated they manually launched Unity using the `-force-d3d11` baseline and confirmed project opening and Play Mode stability.
- This is recorded as `human_verified_d3d11_stable` in `Automation/LoopEngineering/State/crash-resolution.json`.
- `PROJECT_PA_CRASH_REPORT_20260625.md` remains preserved.
- D3D12 is not marked resolved, approved, or verified.

Preflight after BASELINE-001 harness correction:

- Result: `BLOCKED_BY_DIRTY_GIT`
- Crash resolution valid: true
- Approved graphics backend: D3D11
- Unity process count: 0
- Policy mode: `bounded-ticket-loop`

Next:

- User should use GitHub Desktop to stage the approved baseline files and create a local checkpoint commit.
- After the working tree is clean and Unity is closed, rerun preflight. It can become `READY_FOR_BOUNDED_TICKET_LOOP` only if the D3D11 crash-resolution record remains valid.

## 2026-07-09 AI Workflow Structure Pass

- Created `AI_WORKFLOW/` operating-document structure (00_START_HERE, 01_IDENTITY, 02_AGENT_RULES, 03_TASKS, 04_VERIFICATION, 05_LOGS, 06_HANDOFF, 07_FULL_GAME_ROADMAP, 99_ARCHIVE/old_docs).
- Rewrote root `AGENTS.md` as a short entry guide; previous version preserved at `AI_WORKFLOW/99_ARCHIVE/old_docs/AGENTS_v1_20260626.md`.
- Fixed the development goal in documentation: the target is a completable full game, not a submission-only prototype (`AI_WORKFLOW/01_IDENTITY/PROJECT_PA_SCOPE.md`).
- No code, scene, prefab, asset, or meta changes. No file deletion. No `git mv` moves: working tree still has uncommitted VC-001A changes, so all planned document moves stay deferred (`AI_WORKFLOW/00_START_HERE/DOCS_INDEX.md` section 7-B).
- Docs/01~08 remain frozen in place (code comments cite them by section number). Latest crash report stays in root (preflight glob).

## 2026-07-12 Visual Demo Integration Pass (Fable 5)

Scope:

- 새 시스템 없이 기존 구현·에셋을 연결/배치/문구 정리해 데모 화면을 "완성된 코지 상점 게임"으로 통합.
- 씬 파일·프리팹·저장 스키마·경제/NPC 코어 무변경. Project_D 미접근, 패키지 추가 없음, 커밋/푸시 없음.

Implemented:

- `Assets/Scripts/DemoVisualDressingController.cs` (신규, VC-001A 런타임 사이드카 패턴):
  - 채집 포인트 5곳: placeholder 큐브 → 나무 궤짝 + 아이템 색 작물 + 실제 아이템 아이콘 빌보드.
  - 영업 간판: 나무 기둥/걸이대/발광 랜턴 드레싱.
  - 광장: 분수 둘레 벤치 3, 동선 화단 4, 가로등 2, 상점 옆 궤짝/통. 전부 렌더러 전용(콜라이더 제거).
- HUD 문구 한국어 통일: `DayNightShopLoopController`(페이즈/영업 상태/활동 결과), `DaytimeStockPrepPoint`(프롬프트/라벨).
- Day 요약 본문 잘림 수복: Village direction 섹션 추가 후 본문 410px > 영역 360px 였던 기존 문제 → 요약 상태 본문 810x418 로 확장 (`PlayableDayScenarioController`).
- `PA_RuntimeSceneBinder` 등록 1줄, `PA_DayNightShopLoopValidator` 성공 키워드 1줄 동기화("prepared"→"낮 준비 완료"), `Assembly-CSharp.csproj` include 1줄.

Verification (Editor 닫힘 + D3D11 batchmode, 전부 통과):

- dotnet build 런타임/에디터 0 오류 (기존 CS8785 경고만).
- `PA_DayNightShopLoopValidator` — `Logs/Fable_VisualPass_DayNightValidation.log` (`sellableInventory=10`).
- `PA_FinalDemoRouteValidator` — `Logs/Fable_VisualPass_FinalRouteRegression.log` (`stocked=BreadLoaf, paid=30G`). 기존 BLOCKED 상태였던 검증기가 실제 실행·통과됨.
- `PA_FinalPresentationReviewer` — 1차 실행에서 기존 요약 잘림(410/360) 검출 → 수복 후 재실행 통과 (410/418). 캡처 5장: `Logs/FinalPresentation/20260712_161412/`.
- `PA_GatheringShopReview` — 캡처 5장: `Logs/GatheringShopReview/20260712_161528/`.
- `PA_CoreSlicePlayabilityValidator` — 통과.
- `PA_LongPlayProgressionValidator` — 통과 (`money=4633G` 기준선 불변). 기존 BLOCKED 검증기 실행·통과.

Documents:

- 신규: `AI_WORKFLOW/09_FINAL_FABLE_SPRINT/` 5종 (VISUAL_POLISH_REPORT, DEMO_SCENE_LAYOUT_PLAN, DEMO_UI_STATUS, DEMO_5_MINUTE_ROUTE, VISUAL_GAP_AND_PLACEHOLDER_PLAN).

Still required (human):

- 광장 드레싱(벤치/화단/가로등) 전경의 주관적 배치 품질을 Editor Game view 에서 확인 (자동 캡처는 카운터 클로즈업 위주).
- 한국어 폰트 실기기 확인, F10 개발 오버레이 토글 리허설.

## 2026-07-12 (저녁) Visual Demo Integration Pass v2 — 실제 Game View 기준 재작업

Scope:

- v1 이 실제 플레이 화면 기준으로 불합격 판정을 받아, 판정 기준을 "같은 플레이 카메라 Before/After 스크린샷"으로 바꿔 재작업.
- Before: `Logs/DemoViewShots/before_20260712_164931.png` / After: `Logs/DemoViewShots/after5_20260712_223953.png` (2560x1440, Day 1 15:30 동일 조건).

Implemented:

- 신규 `Assets/Editor/PA_DemoViewCapture.cs` — 실제 추적 카메라 그대로 UI 포함 캡처하는 Before/After 툴 (PA_SHOT_LABEL 환경변수로 라벨).
- 갈색 맨땅 제거: `DemoVisualDressingController` 에 광장 베이스 플레이트(30x30 석재 톤) + 판매 데크/러그/파빙/준비 매트. 지오메트리는 Shop 원점이 아니라 실측(`PlazaFrame`: 슬롯 행 방향 + 슬롯 중심→플레이어 방향 + 물리 지면 y=-0.5) 기준.
- 씬 저장 디버그 라벨 7종+ (`Guide_*`: "0. 플레이어 WASD…" 등) 과 SUPPLY/PRICE/SALE 스테이징 라벨을 `CoreSlicePresentationMode` 기본 숨김에 추가 (F10 으로만 표시).
- 상단 중앙 UI 정리: 목표 한 줄(780x44)만 유지, 페이즈 스트립 좌측 컬럼 이동, 좌측 퀘스트 체크리스트 패널(✓/▶/○) 신설 (`PlayableDayScenarioController.BuildQuestPanel`).
- `Item.icon` 6종 데이터 연결 (Free RPG Icons: 빵13/당근9/생선1/밀19/광석4/철괴5) → 핫바/쇼케이스/채집 아이콘 실물화.
- 판매대 슬롯 4개 카운터+가격판 드레싱, 상품 쇼케이스(5종), "MANAGEMENT HUB"→"코지 잡화점" 런타임 교체, 채집 포인트 이름 한국어("텃밭 바구니"/"생산자 납품함") + 라벨 소형화.

Verification (전부 통과):

- dotnet build 0 오류. `PA_FinalDemoRouteValidator`(`paid=30G`) / `PA_DayNightShopLoopValidator`(`sellableInventory=10`) / `PA_CustomerPanelLayoutValidator` — `Logs/Fable_VisualPass2_*.log`.

Still required (human, After 스크린샷 기준 잔여 문제):

- 조명이 어둑함(15:30 청회색) — DayNightVisual 커브는 미수정. 러그/파빙 가시성, 하단 기존 갈색 플랫폼 정체, 분수 벤치 품질 확인.
- 상세: `AI_WORKFLOW/09_FINAL_FABLE_SPRINT/VISUAL_POLISH_REPORT.md` v2 섹션.

## 2026-07-13 Visual Demo Integration Pass v3 — Final Presentation Lock

- Final Locked Screenshot: `Logs/DemoViewShots/after_locked_20260713_002356.png` (2560×1440, Day 1 15:42, UI 포함).
- 상태: 조명·실모델 소품·광장/NPC 스테이징·좌우 HUD 통일 완료. 메인 씬/프리팹/저장/경제·NPC 코어 무변경.
- 컴파일: 런타임/에디터 오류 0 (기존 CS8785/CS0414 경고만).
- 검증: FinalDemoRoute, DayNightShopLoop, CustomerPanelLayout, CoreSlicePlayability, FinalPresentationReviewer 전부 D3D11 batchmode Exit 0/PASS.
- 판정: **조건부 발표용**. 실제 화면은 기능과 루프가 읽히지만 중앙 상점 primitive 실루엣과 UI 스타일 불일치가 남는다.
- 사람 확인: 실제 Editor Game View가 Final Locked Screenshot과 동일하고 폰트·UI·머티리얼 이상이 없는지 1회 확인.

## 2026-07-13 Completion Matrix Sync

- 기준 커밋 `9898f6a`에서 Task 001~085를 실제 구현/검증 증거로 재판정했다.
- 집계: DONE 21 / PARTIAL 26 / TODO 12 / BLOCKED 6 / DECISION_REQUIRED 20.
- Core Loop의 개별 연결은 검증됐지만 Persistence Lock은 미완이다. v8이 돈·인벤토리·핫바·진열·가격·NPC FSM·Day Prep을 저장하도록 구현돼 있으나 실제 저장소 왕복 증거가 없다.
- 다음: Task 007 저장 스키마 확정 → Task 011 안전한 저장 왕복 검증 → Task 018 진열 수량 표시.
- 상세: `AI_WORKFLOW/03_TASKS/CURRENT_COMPLETION_MATRIX.md`.

### Task 007 완료

- Save v8 필드·DTO·마이그레이션·복원 순서를 `AI_WORKFLOW/03_TASKS/SAVE_SCHEMA.md`로 확정했다.
- 현재 미저장: 판매 이력, 카테고리 트렌드, 마을 변화 pending/active 상태.
- 저장 코드 무변경. 다음은 사용자 세이브를 보호하는 실제 저장소 왕복 검증(Task 011).

### Task 011 완료

- 사용자 save 대신 `Logs/SaveRoundTrip/20260713_010249`를 사용해 실제 SaveManager v8 왕복 PASS.
- 돈·누적매출·위치·시간/일차·인벤토리·핫바·진열 아이템/수량/가격·Day Prep·Day 1 단계가 복원됐다.
- Save 런타임 코드와 스키마는 변경하지 않았다. FinalRoute/DayNight 회귀 PASS.

### Task 018 완료

- 가격 설정 패널이 현재 ShopSlot의 실제 진열 수량을 `아이템 · 재고 N개`로 표시한다.
- 동일 카메라 Before/After를 확보했고 FinalPresentation/FinalRoute/DayNight/PanelLayout이 모두 PASS했다.
- 판매·구매·저장 로직 무변경.

## 2026-07-15 Shop Evolution S4 — Tier 1 Interior Unlock

- Tier 0에서는 외부 잡화점 문이 실제 워프를 차단하고 `Tier 1 지점장 필요`를 표시한다.
- 누적 진행으로 Tier 1에 도달하면 문이 열리고 간판이 `잡화점 · OPEN`으로 바뀌며, 따뜻한 문 조명과 짧은 해금 안내가 표시된다.
- 해금 상태는 별도 저장 필드가 아니라 이미 v9에 저장되는 `currentTier`에서 파생되므로 저장/로드 후에도 중복 상태 없이 복원된다.
- D3D11 Play Mode 스모크 PASS: Tier 0 잠금 → Tier 1 해금 → 실내 입장(y=100.1) → BreadLoaf 진열 → 가격 UI → 외부 복귀(y=0.1).
- 컴파일 오류 0. 씬/프리팹/저장 스키마/Shop·경제·구매·NPC FSM은 변경하지 않았다.
- 완성 관점 현황과 다음 순서는 `Docs/Codex/` 4종에 정리했다.

## 2026-07-15 Task 034 — Daily Customer Decision Summary

- 실제 판매가 완료될 때 구매 수를, 구매 평가가 거절될 때 거절 수를 일차별로 집계한다.
- 밤 정산 HUD에 `구매/보류/구매율`을 표시하고, 다음 날 시작 안내를 높은 거절 비율에 맞춘 가격 점검 조언으로 연결한다.
- Day 1 결산도 같은 통계 원본을 사용해 화면마다 수치가 어긋나지 않게 했다.
- 런타임/에디터 컴파일 오류 0. D3D11 전용 검증에서 구매 1건·거절 1건·구매율 50%, 정산 문구와 다음 날 조언을 확인했다.
- 메인 씬·프리팹·구매 확률·돈/재고·NPC FSM·저장 v9는 변경하지 않았다. 일일 통계는 런타임 전용이며 저장 영속화는 Task 055 승인 범위다.

## 2026-07-15 Task 068 Partial — Player-Controlled Day Transition

- 실제 플레이 경로 감사에서 Day 2~7 검증기가 `ForceSet`으로 날짜를 우회하고, 플레이어에게 하루 마감 입력이 없음을 최대 단절로 확인했다.
- Day 1 결산의 `다음 날 시작` 버튼은 Day 2 06:00과 새 플레이어 목표를 연다.
- Day 2+ 정산에서는 기존 가게 간판을 `[Space]`로 사용해 다음 날 06:00을 시작한다. `OnNewDay`를 정상 발화하므로 생산자 납품·채집 리셋·NPC 일과·마을 변화 구독 경로가 유지된다.
- 런타임/에디터 컴파일 오류 0. D3D11 FinalDemoRoute와 DayNightShopLoop PASS.
- 메인 씬·프리팹·저장 v9·경제/구매·NPC FSM·스케줄 시간대는 변경하지 않았다.
- Task 068 전체 완료에는 Task 041 낚시→진열→판매 왕복과 새 전환 경로를 통한 사람 3일 연속 플레이/저장 재실행이 남는다.

## 2026-07-15 Task 039 — 실제 낚시 상호작용 완료

- 해변의 `shore-forage`는 이제 일반 즉시 채집이 아니라 `[Space]`로 낚싯대를 드리우고 1.25초 동안 입질을 기다린 뒤 Fish 2개를 얻는 실제 플레이어 행동이다.
- 전용 자식 트리거가 `IInteractable`을 담당하고, 기존 `DaytimeStockPrepPoint`는 하루 1회·다음 날 리셋·v9 저장 복원의 단일 상태 원본으로 유지된다.
- 물빛 표식·낚싯대·찌·어획 바구니 런타임 외형을 추가했다. 씬·프리팹·저장 스키마·입력 코어는 변경하지 않았다.
- D3D11 GatheringShopGate에서 캐스팅 상태, Fish 2개, 동일 일차 차단, 다음 날 재활성, 진열·가격, 저장 복원을 확인했다. FinalDemoRoute도 30G 판매와 Day 2 전환을 유지했다.
- 다음 기능 연결은 Task 041의 낚시→진열→NPC 구매·수익 증가 단일 왕복이다.

## 2026-07-15 Task 041 — 낚시 결과의 밤 판매 왕복 완료

- 기존 검증기의 합성 Fish 핫바 주입을 제거해 실제 어획물과 상점 재고가 같은 경로인지 증명했다.
- Day 2 낚시 Fish 2개 중 1개가 진열로 차감되고, 기본가 18G를 확정한 뒤 밤 간판을 열어 Fisher_01이 구매한다.
- 거래 결과 잔액 500→518G, 누적매출 +18G, Raw 판매 기록과 Day 2 구매 통계 +1, MoneyHUD 갱신이 함께 확인됐다.
- D3D11 GatheringShopGate와 FinalDemoRoute 모두 PASS. 런타임·씬·프리팹·저장·경제/NPC 코어는 변경하지 않았다.
- Task 068의 자동 기능 선행 조건은 닫혔고, 전체 완료에는 사람 새 게임→Day 3 연속 플레이와 저장 종료/재실행이 남는다.

## 2026-07-15 Task 042 — 낚시 스모크 경로 고정

- `AI_WORKFLOW/04_VERIFICATION/SMOKE_CHECKLIST.md`에 실제 낚시→Fish 2개→1개 진열→18G 가격→밤 개점→Fisher_01 구매→돈/매출/통계 경로를 기록했다.
- 자동 D3D11 PASS와 사람이 실제로 확인할 이동·1.25초 대기·NPC 접근/평가를 분리해, 미확인 체감을 완료로 과장하지 않는다.
- 낚시 Phase 4의 기능·연결·스모크 문서가 닫혔다. 사람 체감은 Task 040/최종 사람 체크에 남는다.
- 런타임과 씬은 변경하지 않았다. 다음 승인 불필요 작업은 Task 043 광질/농사 확장 경계 설계다.

## 2026-07-15 Task 043 — 광질 우선 확장 경계 확정

- `DESIGN_MINING_FARMING.md`에 광질과 농사의 기존 코드/데이터 호환성을 감사했다.
- 광질은 유효한 `Item_Ore`, `Recipe_IronBar`, MineZone/Miner 데이터, v9 일일 활동 상태를 재사용할 수 있어 다음 단일 구현으로 선정했다.
- 다음 왕복은 낮 `quarry-mining`에서 Ore 2개 획득 → 1개 진열 → 15G 판매 → 잔액/누적매출/Raw SalesLog/일일 구매 통계로 고정했다.
- 농사는 기존 `Crop`/`Farmland`를 보존하되 씨앗/수확 참조, 상호작용, 날짜 성장, 저장을 단계적으로 복구한다. 저장 확장은 사람 승인 전 구현하지 않는다.
- 집계는 DONE 32 / PARTIAL 21 / TODO 8 / BLOCKED 6 / DECISION_REQUIRED 18이다. 코드·씬·프리팹·에셋·저장은 변경하지 않았고 Unity는 실행하지 않았다.

## 2026-07-15 Quarry Mining → Night Sale

- 낮 `quarry-mining`에서 `[Space]` 타격 행동으로 Ore 2개를 얻고 같은 날 반복 채굴은 차단한다.
- 실제 획득 Ore 1개를 진열해 15G로 판매하면 잔액 500→515G, 누적매출, Raw 판매 기록, 일일 구매 통계와 HUD가 함께 갱신된다.
- v9 격리 저장/로드가 Day 2 채굴 완료와 Ore 2개를 복원하며 Day 3에는 채굴 지점이 다시 활성화된다.
- 런타임·에디터 컴파일 경고/오류 0. D3D11 채굴 왕복과 기존 FinalDemoRoute가 모두 종료 코드 0/PASS다.
- 광산 primitive 드레싱은 임시 기능 표식이다. 다음 우선 작업은 Unity/Blender/MCP/패키지/라이선스/캡처 감사와 실제 아트 기준 수립·도구 연결이다.

## 2026-07-15 Visual Toolchain + B01 Market Stall Finalization

- Unity 6000.3.2f1, URP/Core/Shader Graph 17.3.0, AI Navigation 2.0.12, Timeline 1.8.9, 설치 패키지와 캡처 경로를 실제 프로젝트 기준으로 감사했다. Cinemachine·Animation Rigging·Blender는 현재 미설치이며 이번 문제에 필요하지 않아 추가하지 않았다.
- 설치 전 Git 체크포인트 `64860ff` 뒤 CoplayDev Unity MCP 9.7.0을 안정 태그와 lock hash `417cf351a152b483c91e6e2deaf7ae355fa8eff3`에 고정했다. 프로젝트 범위 Codex 설정, `127.0.0.1` loopback, 원격/인증/텔레메트리 비활성 정책으로 Unity Editor 연결과 30개 도구 등록을 확인했다.
- MCP로 메인 씬과 B01 프리팹을 직접 비교하고, 중앙 상점의 큰 원시 박스 조합을 기존 `B01_MarketStall` Visual로 교체했다. 기존 Shop/ShopSlot/상품 표시/경제/저장과 씬·프리팹 원본은 유지했고 겹친 안내 박스의 Renderer/Collider만 런타임에서 비활성화했다.
- Tripo 추정 캐릭터·건물과 전체 FBX/OBJ 사용처를 1차 감사했다. 플레이어/NPC 외형은 보존 우선, B09 창고와 B05 작업대는 기능 기반 재구성 후보로 분류했다. 출처 불명 에셋은 최종 배포 확정 금지 상태다.
- 기준 문서: `Docs/Codex/VISUAL_TOOLCHAIN.md`, `ASSET_AND_TOOL_PROVENANCE.md`, `ART_DIRECTION.md`, `TRIPO_ASSET_AUDIT.md`.
- 동일 구도 Before `shot_20260715_164534.png` → After `shot_20260715_171613.png`에서 큰 갈색 큐브/검은 아티팩트 제거와 B01 차양·목재 프레임·상품 접근면 개선을 직접 확인했다.
- 검증: 런타임 빌드 경고 1/오류 0, Editor 빌드 경고 2/오류 0(기존 기준 경고), Unity Console 오류 0, D3D11 FinalDemoRoute `paid=30G` PASS.
- 다음 핵심 구현은 기존 `GridService`/`BuildManager`를 재사용하는 상점 실내 그리드 배치 MVP다. 병렬 시스템을 만들지 않고 배치·회전·이동·회수·저장·protected cell·NPC 동선을 하나의 플레이 루프로 연결한다.

## 2026-07-16 Shop Interior Customization P2

- 상점 실내에 기존 2m `GridService`를 확장한 5×4 zone을 연결했다. 입구 `(2,0)`과 입구→안쪽 서비스 셀 경로는 가구로 막을 수 없다.
- 플레이어는 실내 `상점 배치 장부`에 Space로 접근해 실제 보유 청사진/회수 가구를 선택하고, WASD 전방 셀·R 회전·클릭 확정으로 배치한다.
- 기존 ShopSlot 6개는 삭제/복제하지 않고 이동 가능한 고정 placeable이 됐다. 최소 2개는 운영 보호, 나머지는 상품 안전 회수 뒤 회수할 수 있다.
- 실제 B05 Workbench를 2×2로 배치하며 Workbench 상호작용과 carving obstacle이 유지된다. B06~B08 정의도 실제 청사진에서 파생되며, B09는 현 실내에 부적합해 제외했다.
- 저장 스키마는 v10이다. zone/definition/instance/cell/90도 회전/회수/기능 상태를 저장하고 이동 ShopSlot의 상품·가격과 Workbench를 재시작 상당 로드에서 복원한다.
- D3D11 최종 검증 PASS: 선반 이동 `(4,0)/270°`, B05 2×2, 이동 선반 NPC 판매 61G, 저장 후 상품 2개/73G 복원, 보호 통로, Workbench 회수.
- 기존 저장 라운드트립도 v10에서 돈 1234/누적 5678/ShopSlot 2@77G/마을 변화 복원 PASS, FinalDemoRoute BreadLoaf 30G PASS.
- 최종 실내+UI 캡처를 직접 확인해 앞벽 가림과 상태 문구 여백을 보정했다. 증거는 `Logs/ShopCustomization/20260716_103307/`.
- 다음 구현 우선순위는 P3 야외 zone/도로·입구 보호와 B09 창고의 실제 역할·크기·실루엣 재설계다.

## 2026-07-17 P5 Shop Progression Unlock — IN PROGRESS

- 구현됨(미검증): Tier 기반 실내 zone `5×4 → 6×5 → 7×6`, 기존 TierDefinition 기반 진열 한도 `6 → 8 → 12 → 20`, 실제 ShopSlot 추가 진열대, B05~B08 단계 해금/장부 보상, 확장 벽·바닥·조명·NavMesh, Processed 문화 기반 따뜻한 공방 테마와 v10 sidecar 저장.
- 보존: 기존 배치 원점/instance/cell/rotation, 기존 6개 ShopSlot, 메인 씬, 원본 FBX/프리팹, TierDefinition/BuildingData, SaveData/SaveManager와 스키마 v10.
- 컴파일: Runtime/Editor 오류 0. 기존 Unity source generator/PA_ErrorTracker 경고만 존재.
- 중단: 첫 D3D11 Play 검증에서 `PA_TierExpansionLight_2`의 `Light` 가짜 null 처리로 `MissingComponentException`; 컨트롤러가 ready가 되지 못했다. Tier/배치/NavMesh/테마/저장/캡처는 확인 못 함.
- 다음: `EnsureExpansionLight` null 병합을 명시적 Unity null 검사로 교체하고 `PA_ShopProgressionUnlockValidator`를 처음부터 실행한다.

## 2026-07-17 P5 Shop Progression Unlock — COMPLETE

- 상점 실내는 기존 원점/배치 보존 상태로 Tier 0/1 `5×4`, Tier 2 `6×5`, Tier 3+ `7×6`가 됐다. 실제 진열 한도는 `6/6/8/12/20`이다.
- 기존 6개 진열대는 그대로이며 Tier 2부터 authored 선반 외형의 추가 실제 `ShopSlot`을 배치할 수 있다. 숨은 템플릿은 상점 hierarchy 밖이라 운영 슬롯으로 집계되지 않는다.
- B05/B06/B07+B08이 Tier 1/2/3 장부 보상으로 열리고, Processed 마을 변화는 따뜻한 공방 테마를 연다. 테마와 동적 선반은 v10에서 복원된다.
- 확장 바닥 NavMesh는 동·북 link를 통해 기존 실내와 연결되며 먼 확장 셀까지 `PathComplete`를 통과했다.
- 최종 동일 구도 캡처에서 Tier 0 소형 매장과 Tier 3 확장 매장, 9/20 진열대, 따뜻한 조명·색상, 진행 원장을 확인했다. UI 상태 문구 겹침도 해소했다.
- 컴파일: Runtime/Editor 오류 0. 기존 CS8785/CS0414 경고만 존재.
- 검증: P5 전체, ShopCustomization, EnterableShop, SaveRoundTrip, FinalDemoRoute D3D11 PASS. 기준 `Logs/P5_ShopProgression_D3D11_Release.log`, 캡처 `Logs/ShopProgressionUnlock/20260717_005831/`.
- Task 044 주민 의뢰 표시 설계를 완료했다. 실제 기능 구현은 아직 시작하지 않았으며 다음 단일 큐 작업은 Task 045다.

## 2026-07-17 Task 044 — Resident Request Display Design COMPLETE

- `AI_WORKFLOW/03_TASKS/DESIGN_RESIDENT_REQUEST.md`를 신규 작성했다.
- 현재 Dialogue 8종은 Greeting만 보유하고, CustomerDemandInsight는 개인 요청이 아닌 런타임 카테고리 통계이며, `NpcProfile`에도 요청 품목 필드가 없음을 확인했다.
- 새 퀘스트 엔진 없이 `NpcDialogue`/`DialogueUI`, 기존 `DialogueTopic.Economy`, `SpecialistNpcController.assignedRecipes`, `Inventory`, `FriendshipService`, 일일 활동 문자열 저장을 재사용하는 계약을 확정했다.
- 첫 요청은 Chef_01의 실제 `Recipe_Bread` 입력인 Wheat 3개다. Blacksmith의 Ore 2개와 Carpenter의 Wood 2개가 후속 데이터 사례다.
- `PA_SceneAutoBuilder`가 Tailor에게 Clothes가 아닌 Bread를 배정하는 기존 불일치를 발견해, 수정·검증 전까지 Tailor를 요청 후보에서 제외했다.
- 이번 Task는 정의상 문서 전용이다. 코드·씬·에셋·패키지·저장 스키마를 변경하지 않았고 Unity/Play Mode 검증은 해당하지 않는다.
- 정적 경로/API 대조와 `git diff --check`를 통과했다. 집계는 DONE 33 / PARTIAL 21 / TODO 7 / BLOCKED 6 / DECISION_REQUIRED 18이다.
- 다음 단일 작업: Task 045 낮 활동 결과→재고 연결 문서 동기화.

## 2026-07-17 Task 045 — Daytime Activity To Stock Documentation COMPLETE

- 신규 `AI_WORKFLOW/03_TASKS/DAYTIME_ACTIVITIES.md`에 현재 낮 활동 결과가 인벤토리와 밤 상점으로 이어지는 실제 경로를 정리했다.
- 현재 소스 기준 일일 재고 원천은 6개다: 보조 원천 2개와 숲길 Carrot·해변 Fish·들판 Wheat·광산 Ore 활동 4개다.
- 기존 D3D11 로그에서 Fish 2→1 진열→18G/Fisher_01 구매와 Ore 2→1 진열→15G/Miner_01 구매, 돈·누적매출·Raw SalesLog·일일 통계 갱신을 재확인했다.
- `PROJECT_PA_GAME_LOOP.md`의 낮·해질녘·밤 현재 상태를 Fish와 Ore 왕복, 저장 v10, 농사·주민 의뢰 미구현 상태로 갱신했다.
- Carrot/Wheat는 실제 인벤토리 지급 경로가 있으나 품목별 전 구간 판매 로그가 없어 별도 미검증으로 표시했다.
- Task 045 정의에 따라 코드·씬·프리팹·에셋·패키지·저장 스키마를 변경하지 않았고 Unity를 새로 실행하지 않았다.
- 정적 검사: activityId 6종 소스/문서 대응, Fish/Ore 최종 로그, 게임 루프 링크, trailing whitespace 모두 PASS.
- 집계: DONE 34 / PARTIAL 20 / TODO 7 / BLOCKED 6 / DECISION_REQUIRED 18.
- 다음 단일 작업: Task 046 카테고리별 판매 통계 조사.

## 2026-07-17 Task 046 — Category Sales Aggregation Audit COMPLETE

- 신규 `AI_WORKFLOW/03_TASKS/VILLAGE_TREND.md`에 실제 성공 거래가 카테고리 신호와 다음 날 마을 변화로 이어지는 현재 경계를 정리했다.
- 판매 기록 1건은 성공한 `ShopSlot` 거래 1건이고 `price`는 진열 스택 전체의 총 결제액이다. 거절은 일일 판단 통계에만 들어가며 카테고리 신호에는 들어가지 않는다.
- `SalesLogManager`는 기본 100건을 런타임에 보관하고, 마을 신호는 최근 40건을 `count×1000+revenue`로 비교한다. Tool은 제외되며 명시적 동점·날짜 창 규칙은 없다.
- Raw는 Fish 18G/Ore 15G 실제 왕복, Processed는 2건/76G 신호·BreadLoaf 30G·다음 날 변화·문화 상태 저장 증거가 있다. Utility/Luxury 전체 왕복과 고유 시각은 아직 없다.
- Processed pending/active 시각 상태는 v10에서 저장되지만 판매 기록, 일일 구매/거절 Dictionary, 최근 40건 카테고리 통계는 저장되지 않는다.
- 실제 경로 기반 정적 검사에서 문서 7·소스 20·로그 6·참조 파일 4개와 whitespace가 PASS했다. 문서 전용 작업이라 Unity는 새로 실행하지 않았다.
- 코드·씬·프리팹·에셋·패키지·저장 스키마 변경 없음. 집계는 DONE 35 / PARTIAL 19 / TODO 7 / BLOCKED 6 / DECISION_REQUIRED 18이다.
- 다음 단일 작업: Task 047 트렌드 점수(낚시/캠핑/가구) 데이터 설계.

## 2026-07-17 Task 047 — Named Trend Score Data Design COMPLETE

- `VILLAGE_TREND.md`에 기존 카테고리 집계를 보존하는 별도 명명 트렌드 계약을 추가했다.
- fishing은 Fish(Raw, 18G)와 생선구이(Processed, 52G), furniture는 목제 가구(Luxury, 185G, Tier 2)를 실제 Item/Recipe 데이터로 매핑했다.
- camping은 실제 상품·레시피·낮 활동이 없다. `Shop_Tent_Kit`은 숨겨진 개발용 상점 표식이므로 판매 또는 캠핑 트렌드 데이터가 아니다.
- 성공 판매만 `transactions×1000+min(revenue,999)` 점수를 만든다. 제작·진열·거절·가구 배치는 점수를 만들지 않으며 스택 1거래와 개별 여러 거래도 구분한다.
- 단기 창은 결산 일차, 장기 창은 향후 7개 일차 스냅샷이다. 동점은 거래 수→제한 전 매출→최신 판매→trendId 순서로 해소한다.
- 명명 트렌드 코드와 주간 저장은 아직 없고 캠핑은 비활성이다. 생선구이·목제 가구의 실제 제작→밤 판매 왕복도 확인 못 했다.
- 최종 정적 검사에서 문서 10·데이터 37·SaleRecord 6·캠핑 용어 9·점수 예시 6과 whitespace가 PASS했다. Unity는 문서 전용 범위라 실행하지 않았다.
- 코드·씬·프리팹·에셋·패키지·저장 스키마 변경 없음. 집계는 DONE 36 / PARTIAL 18 / TODO 7 / BLOCKED 6 / DECISION_REQUIRED 18이다.
- 다음 단일 작업: Task 051 시설 해금 신호 표시 연결.

## 2026-07-17 Task 051 — Facility Unlock Direction Preview COMPLETE

- 성공 판매의 선도 카테고리를 기존 `VillageChangeSignalController`에서 감사 앱용 시설 후보로 읽을 수 있게 했다: 원자재→생산자 보관·수거, 가공품→조리·가공, 실용품→수리·공구, 고급품→포장·문화 진열.
- 감사 앱은 카테고리, 다음 후보, 거래 수/매출 신호와 `실제 해금: 티어·감사 조건`을 함께 표시한다. 실제 시설 해금이나 티어 판정은 바꾸지 않았다.
- Runtime/Editor 빌드는 오류 0이며 기존 `CS8785`/`CS0414` 경고만 남았다.
- D3D11 FinalDemoRoute PASS: BreadLoaf 30G Processed 판매 후 가공품→조리·가공 작업대 예고, 해금 권한 경계, 전체 Day 1 루프 확인.
- D3D11 FinalPresentation PASS. 첫 캡처에서 말줄임을 발견해 문구를 줄인 뒤 최종 1920×1080 `Logs/FinalPresentation/20260717_021259/04_audit_app_goal.png`에서 모든 줄과 주변 UI 비겹침을 확인했다.
- 메인 씬·프리팹·`TierService`·`AuditService`·판매 수학·저장 스키마·패키지 변경 없음. 집계는 DONE 37 / PARTIAL 17 / TODO 7 / BLOCKED 6 / DECISION_REQUIRED 18이다.
- 다음 단일 작업: Task 052 이벤트 해금 후보 설계.

## 2026-07-17 Task 052 — Event Candidate Design IN PROGRESS

- 신규 `DESIGN_EVENTS.md` 초안에 첫 후보 `해변 풍어제 — 낚시 대회와 밤 장터`의 실제 Fish 판매 기반 해금, 다음 날 Active, 낮 낚시→밤 판매→정산, 평판 +1 후보, 비처벌 재시도와 저장/승인 경계를 설계했다.
- 이벤트·명명 트렌드·저장 코드는 아직 없으며 코드·씬·프리팹·에셋·패키지·저장 스키마를 변경하지 않았다.
- 첫 정적 검사에서 문서 메타데이터의 Markdown 줄바꿈 공백 2개가 trailing whitespace로 검출됐다. 규칙에 따라 `BUG_LOG.md`에 OPEN 기록하고 Task 052를 완료 전환하지 않았다.
- 집계는 DONE 37 / PARTIAL 17 / TODO 7 / BLOCKED 6 / DECISION_REQUIRED 18로 유지한다.
- 다음 단일 작업: Task 052 정적 검사 재개 조건을 적용해 설계 문서를 검증하고 종료한다.

## 2026-07-17 Task 052 — Event Candidate Design COMPLETE

- `DESIGN_EVENTS.md`에서 첫 구현 후보를 `해변 풍어제 — 낚시 대회와 밤 장터`로 확정했다.
- 전날 `trend.fishing` qualifying 판매가 정산에서 다음 날 행사를 예약하고, Active 날 기존 낚시→선택 가공→진열·가격→밤 구매→정산으로 끝나는 전체 경험을 설계했다.
- 상태 수명주기, 비처벌 재시도, 평판 +1 후보, 임시 해변 시각, 동선 안전선, 공용 명명 트렌드 소유권, 이벤트 저장 필드 후보와 Task 078 승인 경계를 명시했다.
- 재개 검증 PASS: 전체 `git diff --check`, 문서 계약 10/10, 소스·데이터 10/10, Task 041 Fish 판매 로그 5/5, 문서 whitespace 0, 기존 이벤트 런타임 클래스 0.
- 문서 전용 범위라 Unity는 실행하지 않았다. 코드·씬·프리팹·에셋·패키지·저장 스키마 변경 없음.
- 집계는 DONE 38 / PARTIAL 17 / TODO 6 / BLOCKED 6 / DECISION_REQUIRED 18이다.
- 다음 단일 작업: Task 054 판매 통계 저장 확장 설계.

## 2026-07-17 Task 054 — Sales Persistence v11 Design COMPLETE

- 현재 런타임 v10을 덮어쓰지 않고 다음 추가 확장을 v11로 확정했다. Task Queue의 v8→v9/v9 문구는 이미 점유된 역사적 버전이므로 재사용하지 않는다.
- `SAVE_SCHEMA.md`에 `recentSales`, `dailyDecisionStats`, `dailyCategorySales`, `dailyTrendSnapshots`와 각 DTO, 최근 7개 완료 일차+현재 일차 보존 창을 설계했다.
- v10→v11 구버전 마이그레이션은 신규 리스트를 빈 값으로 초기화하며 돈·누적 매출·ShopSlot·마을 문화 상태에서 과거 판매를 역산하지 않는다.
- 저장/로드 뒤 기존 판매 피드·최근 40건 방향·당일 구매/거절·주간 명명 트렌드가 같은 소유권으로 복원되도록 기록·정산·복원 순서와 Task 055 격리 검증 계약을 확정했다.
- 재개 검증: 문서 12/12, 정확한 v10 소스 17/17, 문서 whitespace 0, 전체 `git diff --check` PASS. Unity는 문서 전용 범위라 실행하지 않았다.
- 코드·씬·프리팹·에셋·패키지·실제 저장 버전은 변경하지 않았다. 집계는 DONE 39 / PARTIAL 17 / TODO 5 / BLOCKED 6 / DECISION_REQUIRED 18이다.
- 다음: Task 055 v11 구현은 사용자 승인과 최소 소유자 API 파일 범위 확정이 필요하다. 승인 없이 진행할 수 있는 저장 문서 후속은 Task 060이다.

## 2026-07-17 Task 023 — Rarity/Quality Display STOPPED BEFORE IMPLEMENTATION

- 실제 rarity 필드는 없으며 진열 스택에는 `ItemInstance.quality`가 존재한다. Task 규칙에 따라 가격 파생 희귀/일반 구분과 실제 품질 배수를 기존 `ShopPriceUI`에 표시하는 방향을 확정했다.
- 현재 판매 가능 Resources 카탈로그의 기본가는 8~52G와 150~185G로 분리되어 있어 임시 경계를 100G로 정하고 UI에는 반드시 `가격 파생`이라고 밝힐 계획이다.
- 동일 `apply_patch` 문맥 매칭이 두 번 실패해 구현 전 중단했다. 부분 필드는 제거했으며 런타임 코드·씬·에셋·구매·저장은 변경되지 않았다.
- Task 023은 TODO, 집계는 DONE 39 / PARTIAL 17 / TODO 5 / BLOCKED 6 / DECISION_REQUIRED 18을 유지한다. 컴파일과 시각 캡처는 확인 못 했다.

## 2026-07-17 Task 023 — Rarity/Quality Display COMPLETE

- 기존 진열 가격 패널이 현재 슬롯의 실제 `ItemInstance.quality`를 표시한다.
- rarity 원본 데이터가 없으므로 현재 판매 카탈로그의 가격대 단절을 따라 기본가 100G 미만은 일반품, 이상은 희귀품으로 표시하고 반드시 `가격 파생값`이라고 밝힌다.
- 일반품은 연녹색, 희귀품은 금색이다. BreadLoaf/품질 1.00과 의류/품질 1.25 두 분기를 FinalPresentation에서 assertion했다.
- Runtime/Editor 빌드 오류 0. D3D11 FinalPresentation과 FinalDemoRoute(BreadLoaf 30G)가 PASS했다.
- 1920×1080 동일 카메라 Before/After와 일반/희귀 2장을 직접 확인했다. 패널 내 잘림, 버튼·HUD·상호작용 안내 겹침은 없다.
- 씬·프리팹·아이템 에셋·구매 수학·저장·패키지 변경 없음. 집계는 DONE 40 / PARTIAL 17 / TODO 4 / BLOCKED 6 / DECISION_REQUIRED 18이다.
- 다음 단일 기능 작업: Task 025 기존 가격 데이터를 이용한 읽기 전용 추천가 표시.

## 2026-07-17 Task 025 — Read-only Recommended Price COMPLETE

- 가격 설정 패널에 `추천 기준가 N G · 기본가+품질`을 추가했다.
- 추천 기준은 실제 `PurchaseEvaluator`의 품질 보정 기준가와 같다. 별도 가격 데이터나 밸런스 규칙을 만들지 않았다.
- 추천가는 비교 정보일 뿐이다. 일반 BreadLoaf는 현재가/추천가 30G, 품질 1.25 의류는 현재가 165G를 유지한 채 추천가 186G를 표시한다.
- 예상 반응 힌트도 같은 추천 기준가 대비 비율을 사용하지만 실제 NPC 성향·구매 수학은 기존 `PurchaseEvaluator`가 계속 소유한다.
- 첫 캡처의 추천가/버튼 겹침을 보정한 뒤 최종 1920×1080 일반/희귀 화면에서 텍스트·버튼·HUD 비겹침을 직접 확인했다.
- Runtime/Editor 오류 0, D3D11 FinalPresentation과 FinalDemoRoute 30G PASS. 씬·프리팹·아이템 에셋·구매 수학·저장·패키지 변경 없음.
- 집계는 DONE 41 / PARTIAL 16 / TODO 4 / BLOCKED 6 / DECISION_REQUIRED 18이다. 다음 단일 기능 작업은 Task 031 손님 계층 표시다.

## 2026-07-17 Task 031 — Resident/Tourist Customer Label COMPLETE

- 현재 손님 8명은 모두 실제 마을 일과표가 있는 상주 주민이다. 일부를 임의 관광객으로 바꾸지 않고 일과표 있음=`[주민]`, 없음=`[관광객]` 표시 계약을 파생했다.
- F10 성향 패널은 `이름 [주민|관광객] · 취향` 형식이며, 기본 플레이에서는 기존 머리 위 말풍선에 본문과 분리된 계층 태그가 보인다.
- 주민/관광객 표시는 읽기 전용이다. `NpcController` FSM, 구매 수학, 스케줄 동작, 프로필 데이터, 저장은 변경하지 않았다.
- Runtime/Editor 오류 0. D3D11 CustomerPresentation은 현재 주민 8명·가짜 관광객 0명·관광객 폴백·말풍선 본문 보존을 확인했고, PanelLayout 캡처와 FinalDemoRoute 30G도 PASS했다.
- 최종 캡처 `Logs/CustomerPanelReview/20260717_032821/customer_panels_1920x1080.png`를 직접 확인했다. 태그와 본문이 구분되고 HUD·핫바를 가리지 않는다.
- 집계는 DONE 42 / PARTIAL 16 / TODO 3 / BLOCKED 6 / DECISION_REQUIRED 18이다. 다음 승인 없는 큐 작업은 Task 024 테마 코너 설계다.

## 2026-07-17 Task 024 — Merchandising Theme Corner Design COMPLETE

- `DESIGN_THEME_CORNER.md`에 상점 전체의 기존 `shop.theme/fixed.shop.theme`와 상품 진열 코너를 명확히 분리했다.
- 코너는 실제 `shop.interior` placement의 footprint 셀이 4방향으로 맞닿고, 활성·비어 있지 않은 서로 다른 `ShopSlot` 2개 이상이 같은 `ItemCategory`일 때 성립한다.
- 상품명·가격·월드 거리·씬 이름이 아니라 기존 placement ID, footprint, `Item.category`를 사용하며 Tool과 `toolType != None`은 제외한다.
- 품절·보충·이동·회수에 따라 실시간 파생하고 v10 가구/ShopSlot 복원 뒤 다시 계산한다. 별도 재고, 코너 저장, 구매 확률·가격·매출·트렌드 배수는 만들지 않는다.
- 실제 코너 상품이 팔릴 때만 기존 `ShopSlot`→`SalesLogManager`→`VillageChangeSignalController` 경로가 마을 방향을 만든다. 중복 판매 기록은 금지했다.
- 후속 구현 파일/API/UI/월드 라벨과 11개 D3D11·저장·회귀 검증 계약을 고정했다. 이번 작업은 문서 전용이라 Unity는 실행하지 않았고 코드·씬·프리팹·에셋·패키지·저장 스키마 변경은 없다.
- 정적 검사: 문서 존재/핵심 계약/참조 경로/금지 경계/whitespace PASS. 집계는 DONE 43 / PARTIAL 16 / TODO 2 / BLOCKED 6 / DECISION_REQUIRED 18이다.
- 다음 단일 작업: Task 024 후속 런타임 구현을 큐에 등록한 뒤, 정확한 코드 파일 목록을 보고하고 구현·D3D11 게임 카메라 검증한다.

## 2026-07-17 Task 086 상품 진열 테마 코너 — PARTIAL

- `MerchandisingCornerController`가 기존 `ShopSlot` 재고와 `shop.interior` placement footprint를 읽어 같은 판매 카테고리의 4방향 연결 요소를 파생하도록 구현했다. 서로 다른 실제 placement 2개 이상만 코너가 된다.
- Tool/recovered/비활성/빈 진열대는 제외하고, 코너는 새 재고·가격·보너스·판매 기록·저장 필드를 소유하지 않는다.
- 기존 상점 배치 장부 두 번째 줄에 현재 코너 요약을 추가하고 연결 요소당 월드 라벨 하나를 배치한다. 런타임 바인더와 전용 D3D11 validator도 등록했다.
- Unity 6000.3.2f1 Runtime/Editor 어셈블리 컴파일 PASS. 첫 D3D11 검증에서 컨트롤러/grid/6개 슬롯/아이템/빈 상태 코너 0·라벨 0까지 PASS했다.
- 첫 1920×1080 캡처의 직접 `Camera.Render()`에서 Unity 네이티브 렌더 크래시가 발생해 프로젝트 규칙에 따라 재시도 없이 중단했다. 양성 코너·품절/재진열·회수/이동·판매→마을 방향·격리 저장 로드·3개 회귀와 캡처 직접 검토는 확인 못 함.
- 씬·프리팹·패키지·저장 스키마는 변경하지 않았다. 현재 집계는 DONE 43 / PARTIAL 17 / TODO 2 / BLOCKED 6 / DECISION_REQUIRED 18(총 86)이다.

## 2026-07-17 Task 086 상품 진열 테마 코너 — FUNCTION PASS / PROJECT PARTIAL

- 전용 D3D11 실제 GameView 검증은 같은 분류 4방향 인접, 대각선·간격·혼합 제외, Raw2/Processed6, 품절·보충, 회수·이동, 실제 구매 3건, Processed 마을 방향, v10 저장 후 Raw2 재파생을 모두 PASS했다.
- 검증 전용 재배치 helper는 실제 `CommitActivePlacement`와 동일하게 recovered 상태를 해제하도록 정합화했다. 게임 구매·경제·판매·저장 소유권은 변경하지 않았다.
- Runtime/Editor 빌드 오류 0. 기능 로그는 `Logs/Codex_Task086_ThemeCorner_CaptureFinal.log`이다.
- 동일 구도 캡처에서 초점은 상점 2×3 진열 구역과 오른쪽 배치 장부에 유지되고, 플레이어/NPC 비율·통로·기존 가구·조명은 상태 전환 사이 변하지 않는다. 장부의 코너 수는 없음→원재료2→가공품4로 변한다. 다만 사업자 등록 모달이 월드 라벨을 가리고 ScreenCapture가 간헐적으로 검은 TMP/Canvas 프레임을 기록해 기본 플레이 가독성 완료 증거로는 부족하다.
- 기존 ShopCustomization 회귀의 직접 `Camera.Render()`가 두 번째 동일 네이티브 충돌을 재현했다. 세 번째 Unity 실행 금지에 따라 SaveRoundTrip/FinalDemoRoute 포함 남은 회귀를 실행하지 않았다.
- 집계는 DONE 43 / PARTIAL 17 / TODO 2 / BLOCKED 6 / DECISION_REQUIRED 18(총 86)을 유지한다. 다음 구현 전에 공용 validator 캡처 경로에 대한 사람 판단이 필요하다.

## 2026-07-17 Task 095 기존 가구 보조 루프 단계 안내 — IMPLEMENTED / PROJECT PARTIAL

- 기존 `ProcessingOpportunityController`가 `Recipe_Furniture`, Plank 보유량, 활성 BasicWorkbench, Tier, ShopSlot 진열, 당일 SalesLog를 읽어 가구 루프의 다음 행동 하나를 계산한다.
- Day 4+ 좌측 운영 체크리스트에서 `Tier 1 매출 → B05 설치 → Plank 3 → Tier 2 매출 → 목제 가구 제작 → 진열 → 당일 판매 → 정산의 가구 문화 확인` 경로가 현재 상태에 맞춰 보인다.
- 이 연결은 상태를 읽기만 한다. 아이템 지급, 강제 승급, 자동 제작/진열/판매, 10,000G/100,000G 조건, 저장 스키마는 그대로다.
- Runtime/Editor 빌드 오류 0, 읽기 전용 정적 계약 PASS. Unity는 동일 네이티브 충돌 2회 경계를 지켜 실행하지 않았다.
- 실제 1920×1080 패널 잘림과 Tier별 전환·전체 제작/판매 왕복은 확인 못 했다. Task 095와 Task 070은 PARTIAL이다.
- 최신 집계: DONE 44 / PARTIAL 27 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 95).

## 2026-07-17 Task 096 새 게임·이어하기 제품형 진입 — IMPLEMENTED / PROJECT PARTIAL

- 게임 실행 직후 기존 첫날 기능 모달보다 먼저 `PROJECT P.A.` 타이틀과 핵심 판타지, `새 게임 / 이어하기` 선택이 표시되도록 연결했다.
- 저장 존재 여부는 기존 `ISaveRepository.ExistsAsync`만 읽는다. 이어하기는 v10 `LoadGameAsync`와 기존 세션 복원 권한을 그대로 사용한다.
- 저장이 없으면 이어하기가 비활성이고, 로드 실패 시 타이틀에 남는다. 새 게임은 기존 이름 등록과 Day 1 흐름으로 진입하며 기존 저장을 즉시 삭제하지 않는다.
- Runtime/Editor 오류 0, 상태 전이 15개·상태 변경 호출 부재 검사 PASS. Unity 실제 화면과 저장 로드는 확인 못 했다.
- 저장 스키마·SaveKey·자동 로드·삭제·씬·프리팹·Day 1 단계·F5/F9는 변경하지 않았다.
- 최신 집계: DONE 44 / PARTIAL 28 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 96).

## 2026-07-17 Task 087 Tripo3D 임시 에셋 감사 정합화 — COMPLETE

- Assets 전수 수량은 FBX 174, OBJ 150, GLB 0, Blend 0이며 Nature Pack을 제외한 FBX 24개를 캐릭터·건물·소품 원본으로 대조했다.
- Unity 6 바이너리 메인 씬의 읽기 전용 문자열 색인과 GUID/Resources/코드 참조로 C-01~C-09, B05~B12의 실제 사용을 확인했다. 씬 YAML을 추측하거나 수정하지 않았다.
- B06 Kitchen은 기존 Kitchen 레시피/Bread·Baked Potato·Grilled Fish/2×2/Tier 2, B07 Forge는 Iron Bar·Tool Set/3×2/Tier 3, B08 Sewing은 Clothes/2×2/Tier 3에 이미 연결된 기능 작업대다. 세 에셋은 기능 통합 완료·게임 카메라 시각 최종화 대기로 분리했다.
- B11 Fountain과 B12 TradePort는 현재 메인 씬 정적 장식이며 배치 카탈로그나 상호작용 기능에 등록되지 않았다. 가짜 기능을 추가하지 않고 물리·동선만 후속 최종화한다.
- Quaternius Ultimate Nature Pack의 CC0 1.0 원문은 확인했다. Tripo 추정 모델의 개별 생성/상업 이용 증빙과 실제 런타임 Froggy Chair 라이선스는 미확인이라 최종 빌드 배포 게이트로 유지한다.
- 코드·씬·프리팹·모델·텍스처·패키지·저장 변경 및 Unity 실행 없음. 정적 모델/참조/레시피/씬/라이선스 표식 검사 PASS. 집계는 DONE 44 / PARTIAL 17 / TODO 2 / BLOCKED 6 / DECISION_REQUIRED 18(총 87)이다.

## 2026-07-17 Task 088 주민 재료 요청 플레이 루프 — IMPLEMENTED / PROJECT PARTIAL

- 전문 주민의 실제 `assignedRecipes`와 전문 분야 작업대를 대조해 Chef는 Wheat 3, Blacksmith는 Ore 2, Carpenter는 Wood 2를 낮에 요청한다. Tailor의 임시 Bread 배정은 SewingTable/Kitchen 불일치로 자동 제외한다.
- 기존 상호작용 프롬프트와 DialogueUI에 요청 확인, 보유량, 건네기, 당일 완료 상태를 연결했다. 부족한 첫 대화는 기존 일일 대화 친밀도를 유지한다.
- 준비된 재료는 인벤토리+핫바 합산으로 검사해 정확 수량만 차감하고, `resident-request:{day}:{residentId}:{recipe.name}:{item.id}`를 기존 `dayPrepCollectedActivities` 경로에 기록한 뒤 친밀도만 보상한다. 코인·아이템·판매·마을 신호 보상은 없다.
- Runtime/Editor 순차 컴파일 오류 0. 레시피/배정/작업대 제외/수량/당일 API/저장 목록 재사용/Economy 대사/경제 비침범 정적 계약 PASS.
- 직접 `Camera.Render()` 동일 네이티브 충돌 2회 경계를 지켜 Unity는 실행하지 않았다. 실제 낮 부족→준비→전달→중복 차단, 저장/로드·다음 날 초기화·밤 차단·UI 캡처는 확인 못 했다.
- 씬·프리팹·저장 스키마·경제/구매/판매·패키지는 변경하지 않았다. 집계는 DONE 44 / PARTIAL 18 / TODO 2 / BLOCKED 6 / DECISION_REQUIRED 18(총 88)이다.

## 2026-07-17 Task 089 고정 밭 Wheat 재배 상호작용 — F1/F2 IMPLEMENTED / PROJECT PARTIAL

- 끊겨 있던 `Item_15_Seed`→`Crop_Corn`→`Item_Wheat` 참조를 복구하고 수확량을 Wheat 3개로 고정했다.
- 기존 `Farmland`와 `Crop`을 유지한 `FarmPlotInteraction`이 농부 작업 지점 인근 고정 밭 2칸을 보장한다. 낮에 씨앗 주머니에서 씨앗 2개를 받고, 각 밭에 1개씩 심어 성장 단계와 수확 가능 상태를 프롬프트/월드 라벨로 읽는다.
- 씨앗은 작물 생성 성공 뒤 차감하며, 수확은 인벤토리 전량 수용 가능성을 먼저 검사한 뒤 Wheat 3개를 지급한다. 가방이 차면 작물이 남는다.
- 작물 단계는 기존 실제 `PA_DemoProps/Prop_Wheat`를 1/3/5개 군집으로 재사용하며, 밭은 경작 이랑이 있는 단일 메시다. 원시 큐브 작물 표현은 런타임에서 숨긴다.
- Runtime/Editor 순차 컴파일 오류 0, 데이터 GUID·고정 밭 2·낮 전용·심기/수확 안전 순서·실제 Wheat 시각·금지 코어 비침범 12개 정적 계약 PASS.
- Unity 실플레이와 화면/동선은 직접 렌더 충돌 2회 경계로 확인 못 했다. 날짜 기반 성장과 plot 저장은 F3 저장 설계/승인 전 추가하지 않았다.
- `PlayerInteraction`, 씬, 저장 스키마, 상점/경제/구매/NPC 코어, 패키지는 변경하지 않았다. 집계는 DONE 44 / PARTIAL 19 / TODO 2 / BLOCKED 6 / DECISION_REQUIRED 18(총 89)이다.

## 2026-07-17 Task 090 Day 2+ 생활–상점 운영 체크리스트 — IMPLEMENTED / PROJECT PARTIAL

- Day 2+의 정적 반복 안내를 실제 플레이 상태 기반 체크리스트로 교체했다. 낮 활동, 판매 상품 2종, 진열/가격, 밤 개점, 당일 판매, 정산의 다섯 줄이 0.5초 간격으로 갱신된다.
- 낮 활동은 기존 `shore-forage`, `quarry-mining`, `farm-seed-pouch`/고정 밭, 채집 활동과 `NpcDialogue` 주민 요청 완료를 읽는다. 상품 종류는 가방·핫바·진열대를 합산하되 Tool과 잠긴 Tier 상품을 제외한다.
- 상단 목표는 현재 `DayPreparation`, `ShopOpen`, `Settlement`과 실제 고객 영업 개점 상태에 맞춰 바뀐다. 새 진행 상태, 보상, 저장 필드는 추가하지 않았다.
- Runtime/Editor 순차 컴파일 오류 0, 정적 계약 10개 PASS. Unity 실행과 1920×1080 가독성은 직접 렌더 충돌 2회 경계로 확인하지 못했다.
- 코어 시스템·씬·프리팹·저장 스키마·패키지는 변경하지 않았다. 집계는 DONE 44 / PARTIAL 20 / TODO 2 / BLOCKED 6 / DECISION_REQUIRED 18(총 90)이다.

## 2026-07-17 Task 091 가공 결과물 수용량 선검사 — IMPLEMENTED / PROJECT PARTIAL

- `CraftingService`가 재료 차감 후 결과물을 받지 못해 재료를 잃던 경로를 차단했다.
- 결과 품질/가격 메타를 가진 실제 `ItemInstance`를 먼저 만든 뒤, 핫바→가방의 재료 차감 결과를 모의해 메타 일치 스택 여유 또는 비게 될 슬롯을 확인한다.
- 수용 불가면 재료 차감 전에 중단한다. 수용 가능하면 기존 재료 품질 평균, 차감, 결과 추가, 작업대 성공 피드백 순서를 보존한다.
- 현재 8개 레시피 데이터, Runtime/Editor 오류 0, 정적 계약 11개 PASS. Unity 실제 인벤토리 세 분기는 직접 렌더 충돌 2회 경계로 확인 못 했다.
- `Inventory`, 데이터 에셋, UI, 저장, 경제/상점/NPC 코어, 씬·프리팹·패키지는 변경하지 않았다. 집계는 DONE 44 / PARTIAL 21 / TODO 2 / BLOCKED 6 / DECISION_REQUIRED 18(총 91)이다.

## 2026-07-17 Task 092 Raw 다음 날 마을 변화 — IMPLEMENTED / PROJECT PARTIAL

- 실제 Fish/Ore/Wood의 `Raw` 판매를 기존 VC-001A pending→다음 날 준비 페이즈 계약에 연결했다. 같은 갱신 구간의 판매는 최신 실제 Raw/Processed 카테고리 한 건이 다음 날 대표 변화가 된다.
- 다음 날에는 기존 Processed 시각과 상호 배타적으로 `PA_VillageCulture_Raw`가 켜지고, v10의 기존 카테고리 문자열 저장/복원 계약을 그대로 사용한다.
- Raw 지점은 원시 큐브가 아니라 Quaternius CC0 통나무·바위 실모델과 Project P.A. 자체 간판 메시로 구성하며 `원자재 수거처`로 역할을 표시한다. 복제본은 동선을 막거나 가짜 저장함처럼 동작하지 않는다.
- Runtime/Editor 순차 빌드 오류 0, 새 카테고리 힌트 1회 재설정을 포함한 정적 계약 14개 PASS. Unity 실제 카메라/동선은 직접 렌더 충돌 2회 경계로 확인 못 했다.
- 저장 스키마·판매/경제/NPC·씬·프리팹 원본·Tripo 원본·패키지는 변경하지 않았다. 집계는 DONE 44 / PARTIAL 22 / TODO 2 / BLOCKED 6 / DECISION_REQUIRED 18(총 92)이다.

## 2026-07-17 Task 093 실제 판매 기반 명명 트렌드 — IMPLEMENTED / PROJECT PARTIAL

- 기존 `VillageChangeSignalController`에 당일 성공 판매 전용 낚시·가구 문화 집계를 추가했다. 정확한 `(Raw, Fish)`, `(Processed, 생선구이)`, `(Luxury, 목제 가구)`만 허용하며 캠핑은 계속 비활성이다.
- 점수는 `거래×1000+min(매출,999)`이고 거래→매출→최근 판매→trendId 순서로 선도를 고른다. Fish 1건/18G는 1018점, 2건/36G는 2036점이다.
- 기존 카테고리 신호와 시설 방향 예고를 보존하고 Day 1 결산 요약 둘째 줄에 `생활 트렌드 · 낚시 생활|가구 문화`를 추가했다. 저장·보상·구매 수학은 만들지 않았다.
- Runtime/Editor 순차 빌드 오류 0, 정확 매핑·일차·점수·동점·캠핑 비활성·소유권 정적 계약 18개 PASS. 기존 dirty 저장 diff를 이번 변경으로 오인한 검사 1회는 기준선 오류로 `BUG_LOG.md`에 기록·해소했다.
- Unity는 직접 렌더 충돌 2회 경계를 지켜 실행하지 않았다. 결산 줄바꿈/가독성과 생선구이·목제 가구의 제작→밤 판매 왕복은 미확인이다. 집계는 DONE 44 / PARTIAL 24 / TODO 2 / BLOCKED 5 / DECISION_REQUIRED 18(총 93)이다.

## 2026-07-17 Task 094 Tripo3D 장기 정책과 미확인 소품 노출 — IMPLEMENTED / PROJECT PARTIAL

- 기존 Tripo 감사에 8분류 장기 판정, 캐릭터 외형 보존, 창고/작업대 기능 맥락, Unity/Blender 수정 경계, Placeable 온보딩, 원본/출처/동일 카메라 배포 게이트를 통합했다.
- 라이선스 문서가 없는 Froggy Chair의 실내·광장 런타임 생성 2곳을 제거했다. B01 노점·상점 기능·실내 가구·Quaternius CC0 식생·광장 벤치는 유지했다.
- 원본 FBX와 래퍼/Resource 프리팹은 보존했다. Runtime 코드 참조는 0이지만 Resource의 최종 빌드 포함 가능성은 증빙/격리 전까지 배포 게이트다.
- Runtime/Editor 순차 빌드 오류 0, 정적 계약 PASS. Unity GameCamera는 직접 렌더 충돌 2회 경계로 확인하지 못했다. 집계는 DONE 44 / PARTIAL 25 / TODO 2 / BLOCKED 5 / DECISION_REQUIRED 18(총 94)이다.

## 2026-07-17 Task 097 Pause 메뉴 제품 제어 — IMPLEMENTED / PROJECT PARTIAL

- ESC 메뉴가 계속하기·저장·저장본 불러오기·저장 후 종료를 제공한다. 저장본이 없으면 Load가 비활성이고 비동기 작업 중에는 중복 입력이 잠긴다.
- 일시정지 전 시간 배율과 커서 잠금/표시 상태를 보존해 Resume 또는 오브젝트 파괴 시 원상 복구한다. 시작 타이틀 위에는 Pause를 열지 않는다.
- 모든 저장 작업은 기존 `SaveManager` 공개 API만 사용하고, 저장 실패 시 종료를 취소해 현재 세션을 유지한다.
- Runtime/Editor 오류 0, Pause/저장 권위 정적 계약 11/11 PASS. Unity 실제 클릭과 빌드 종료는 확인 못 했다.
- 최신 집계: DONE 44 / PARTIAL 29 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 97).

## 2026-07-17 Task 098 Day 7 첫 주 완주 — IMPLEMENTED / PROJECT PARTIAL

- Day 7 정산에 첫 주 운영 완료 화면과 이름·누적 매출·보유금·Tier·평판·마지막 정산 요약을 연결했다.
- 플레이어는 Day 7을 저장한 뒤 기존 다음 날 경로로 Day 8을 시작하고 다시 저장하거나, 저장 성공 뒤 게임을 종료할 수 있다.
- 완주 화면이 열린 동안 배경의 다음 날 상호작용을 막고 timeScale/커서를 복구한다. 저장·전환 실패는 현재 화면에서 회복한다.
- Runtime/Editor 오류 0, 완주/저장 순서 계약 12/12 PASS. Unity 실제 Day 7 화면과 빌드 종료·재실행은 확인 못 했다.
- 최신 집계: DONE 44 / PARTIAL 30 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 98).

## 2026-07-17 Task 099 실제 입력 기반 시작 조작 안내 — IMPLEMENTED / PROJECT PARTIAL

- 첫날 스마트폰 지급 뒤에 현재 게임의 실제 조작을 한 화면으로 안내한다.
- 이동·상호작용·인벤토리/스마트폰/제작·핫바·건설 배치/회전/이동/회수·저장/불러오기·Pause 키가 `PlayerInputHandler`와 일치한다.
- 안내 확인 뒤 기존 보급품→도착→Day 1 목표로 이어지며 별도 진행 상태나 저장 필드를 만들지 않았다.
- Runtime/Editor 오류 0, 기능 계약 11/11 PASS. Unity 실제 화면과 클릭은 직접 렌더 충돌 2회 경계로 확인하지 못했다.
- 최신 집계: DONE 44 / PARTIAL 31 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 99).

## 2026-07-18 Task 100 타이틀 게임 종료 경로 — IMPLEMENTED / PROJECT PARTIAL

- 시작 타이틀에 `게임 종료`를 추가해 새 게임·이어하기·종료의 세 제품 제어가 모두 연결됐다.
- 종료 버튼은 Title에서만 보이고, 다른 온보딩 단계와 Day 1 결산에서는 기존 레이아웃으로 돌아간다.
- 이어하기 로딩 중 종료를 잠그며, 빌드는 `Application.Quit`, Editor는 안전한 안내 문구를 사용한다. 저장 호출은 없다.
- Runtime/Editor 오류 0, 상태 계약 14/14 PASS. Unity 실제 화면과 Windows 빌드 종료는 확인하지 못했다.
- 최신 집계: DONE 44 / PARTIAL 32 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 100).

## 2026-07-18 Task 101 기존 저장 보호 새 게임 확인 — IMPLEMENTED / PROJECT PARTIAL

- 타이틀 저장 조회가 끝날 때까지 새 게임을 잠그고, 기존 저장이 있을 때만 이후 저장의 단일 슬롯 덮어쓰기 위험을 확인받는다.
- 계속은 기존 이름 등록으로 진행하고 취소는 타이틀로 돌아가 저장 상태를 다시 조회한다. 저장 없음은 추가 화면 없이 이름 등록으로 직행한다.
- 확인 경로는 저장·삭제를 실행하지 않으며 기존 이어하기·게임 종료·조작 안내 흐름을 보존한다.
- Runtime/Editor 오류 0, 저장 보호 상태 계약 15/15 PASS. Unity 실제 화면과 클릭은 직접 렌더 충돌 2회 경계로 확인하지 못했다.
- 최신 집계: DONE 44 / PARTIAL 33 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 101).

## 2026-07-18 Task 102 B09 외부 창고 실제 사용 UI — IMPLEMENTED / PROJECT PARTIAL

- B09에 `StorageBox`와 v10 내용물 저장은 있었지만 메인 씬 `StorageUI`가 0개여서 실제 상호작용 화면이 열리지 않던 기능 단절을 연결했다.
- 런타임 UI는 24칸 6×4, 실제 아이콘·수량·품질·유효 가격, 선택 핫바 1개 보관과 클릭 회수를 제공한다.
- 보관은 선택 슬롯에서만 정확히 1개를 차감하고, 회수는 기존 `Inventory.AddInstance`와 가방 가득 참 보존 규칙을 따른다. ESC/커서/인벤토리·폰·제작 패널 우선순위도 연결했다.
- Runtime/Editor 오류 0, 창고 기능 계약 14/14 PASS. Unity 실제 화면·클릭·v10 저장 왕복은 직접 렌더 충돌 2회 경계로 확인하지 못했다.
- 최신 집계: DONE 44 / PARTIAL 34 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 102).

## 2026-07-18 Task 103 제작 도감·작업대 제작 UI — IMPLEMENTED / PROJECT PARTIAL

- 빈 화면이던 `[C] 제작`을 기존 8개 레시피 전체 도감으로 교체하고 필요한 작업대를 표시했다. 도감에서는 원격 제작할 수 없다.
- 작업대 `[Space]` 화면은 해당 종류 레시피만 보여 주며 실제 아이콘, 출력, 전체 재료 보유량, 잠금, 제작 결과를 표시한다. 재료 차감/결과 생성은 기존 `CraftingService`만 수행한다.
- 전체 화면 입력 차단, 인벤토리·스마트폰·창고 상호배제, 커서 복원, ESC 우선 닫기를 연결했다.
- Runtime/Editor 오류 0, 제작 기능 계약 16/16과 diff 검사 PASS. Unity 실제 C/Space 화면·클릭·1920×1080 가독성은 직접 렌더 충돌 2회 경계로 확인하지 못했다.
- 최신 집계: DONE 44 / PARTIAL 35 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 103).

## 2026-07-18 Task 104 Tripo 장기 정책·B11 분수 충돌 정합 — IMPLEMENTED / PROJECT PARTIAL

- 기존 `TRIPO_ASSET_AUDIT`, Placeable P1~P5와 출처 대장을 장기 ADR로 연결했다. 임시 Tripo 에셋 일괄 삭제 금지, 캐릭터 외형 정체성 보존, 기능 가구의 배치/접근/저장 계약, 원본 비파괴와 라이선스 게이트가 기준이다.
- B11은 원형 모델에 6×6 사각 루트 BoxCollider가 붙어 모서리에서 보이지 않는 충돌을 만들었다. 런타임에서 실제 Visual mesh에 비볼록 정적 MeshCollider를 붙인 뒤 루트 Box만 끈다.
- 메시가 없으면 기존 Box를 유지하고, 기존 캡슐형 carving obstacle과 시각/배치/원본/프리팹/씬은 보존한다.
- Runtime/Editor 오류 0, B11 물리·안전 폴백·NavMesh/원본 보존 계약 12/12와 diff 검사 PASS. Unity 실제 이동/동일 GameCamera After는 직접 렌더 충돌 2회 경계로 확인하지 못했다.
- 최신 집계: DONE 44 / PARTIAL 36 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 104).

## 2026-07-18 Task 105 20~23시 영업 손님 흐름 복구 — IMPLEMENTED / PROJECT PARTIAL

- 주민 시간표가 19~20시에 Rest로 전환돼 23시 폐점 전 손님이 사라지던 핵심 밤 영업 공백을 확인했다.
- 실제 schedule phase는 유지한 채 Rest 주민만 한시적으로 쇼핑 우선순위를 받아 외부·실내 상점을 방문한다. Work·Sleep·Day 1과 기존 활성 손님은 건드리지 않는다.
- 외부/실내 초대는 원래 위치와 Shop 참조를 보존하며 완료·실패·timeout·폐점에서 priority와 override를 해제하고 원래 Rest로 돌려보낸다.
- Runtime/Editor 오류 0, 늦은 손님 흐름·원상복귀·기존 FSM/구매 권위 보존 계약 18/18과 diff 검사 PASS. Unity 실제 시간대 확인은 직접 렌더 충돌 2회 경계로 수행하지 않았다.
- 최신 집계: DONE 44 / PARTIAL 37 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 105).

## 2026-07-18 Task 106 Processed 다음 날 변화 실제 에셋 전환 — IMPLEMENTED / PROJECT PARTIAL

- Processed 판매 다음 날 나타나는 핵심 시각 변화에 원시 큐브 5개가 남아 있어 최신 아트 지침과 실제 에셋 정책을 위반하던 부분을 확인했다.
- 기존 B05 `BuildingData`에서 기능 래퍼가 아닌 실제 `Visual`만 복제하고, Project P.A. 준비 키트와 `가공 준비대` 간판을 결합했다. 새 에셋·서비스·패키지는 추가하지 않았다.
- 복제 시각에는 Workbench·Collider·Rigidbody·NavMeshObstacle·행동·추가 Light가 남지 않는다. 실제 B05 기능/배치/해금과 Raw 다음 날 변화·v10 저장은 그대로다.
- Runtime/Editor 오류 0, primitive 제거·실제 리소스·비충돌·카테고리/저장 보존 계약 18/18과 diff 검사 PASS. 같은 GameCamera의 새 광장 구도는 직접 렌더 충돌 2회 경계로 확인하지 못했다.
- 최신 집계: DONE 44 / PARTIAL 38 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 106).

## 2026-07-27 Task 107 Utility 다음 날 공구 수리대 변화 — IMPLEMENTED / PROJECT PARTIAL

- 판매 가능한 `철제 도구`와 기존 Forge 레시피를 세 번째 카테고리별 마을 변화에 연결했다.
- Utility 성공 판매는 기존 SalesLog→pending→다음 DayPreparation→v10 category 문자열을 그대로 사용한다. 판매 당일 즉시 변화나 새 저장 필드는 없다.
- B07 기능 래퍼 대신 실제 `Visual`만 0.44배로 복제하고 `공구 수리대` 간판을 결합했다. Workbench·Collider·Rigidbody·NavMeshObstacle·행동·Light는 복제하지 않는다.
- Processed/Raw/Utility는 상호 배타적이며 실제 B07 기능·배치·해금, 판매/가격/구매/NPC/저장, 씬·프리팹·FBX·패키지는 변경하지 않았다.
- Runtime/Editor 오류 0, 기능·데이터·비충돌·저장 계약 23/23 PASS. 같은 GameCamera의 Utility 판매 전/다음 날 구도는 반복 직접 렌더 충돌 경계로 확인하지 못했다.
- 최신 집계: DONE 44 / PARTIAL 39 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 107).

## 2026-07-27 Task 108 Luxury 다음 날 공예 전시대 변화 — IMPLEMENTED / PROJECT PARTIAL

- 판매 가능한 `목제 가구`와 `의류`, 기존 Furniture/Sewing 레시피를 네 번째 카테고리별 마을 변화에 연결했다.
- Luxury 성공 판매는 기존 SalesLog→pending→다음 DayPreparation→v10 category 문자열을 그대로 사용한다. 판매 당일 즉시 변화나 새 저장 필드는 없다.
- B08 기능 래퍼 대신 실제 `Visual`만 0.48배로 복제하고 `공예 전시대` 간판을 결합했다. Workbench·Collider·Rigidbody·NavMeshObstacle·행동·Light는 복제하지 않는다.
- Processed/Raw/Utility/Luxury는 상호 배타적이며 실제 B08 기능·배치·해금, 판매/가격/구매/NPC/저장, 씬·프리팹·FBX·패키지는 변경하지 않았다.
- Runtime/Editor 오류 0, 수정된 기능·데이터·비충돌·저장 계약 27/27 PASS. 같은 GameCamera의 Luxury 판매 전/다음 날과 v10 복원은 반복 직접 렌더 충돌 경계로 확인하지 못했다.
- 최신 집계: DONE 44 / PARTIAL 40 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 108).

## 2026-07-27 Task 109 채용 후보 제품 흐름 — IMPLEMENTED / PROJECT PARTIAL

- 모든 스마트폰 후보의 빈 `spawnPrefab` 때문에 실제 고용이 실패하던 경로를 복구했다. 명시 프리팹은 계속 우선하고, 없을 때만 같은 전문 분야의 기존 C-02~C-09 역할 주민 구성을 사용한다.
- `NpcController`와 실제 SkinnedMesh가 있는 원본만 허용하며, 생성된 채용 인스턴스는 이후 원본 선택에서 배제한다. 신규 채용과 v10 복원이 같은 원본 해결 규칙을 사용한다.
- 후보 profile/specialty/schedule/dialogue/고유 친밀도 키를 주입하고, 전문가는 해당 WorkbenchType의 기존 레시피만 받는다. 후보·티어·원본·잔액은 기존 `EconomyService.TrySpend` 전에 검사한다.
- 채용 UI는 첫 열기부터 카드가 생성되고 소개·한글 역할·비용·잔액/티어/중복 상태와 성공/실패 피드백을 표시한다.
- Runtime/Editor 오류 0, 수정된 정적 계약 36/36 PASS. 후보 에셋·씬·프리팹·FBX·저장 스키마·경제/NPC FSM·패키지는 변경하지 않았다. 실제 스마트폰 채용·역할 행동·저장 복원·1920×1080 가독성은 안전 Unity 경로 대기다.
- 최신 집계: DONE 44 / PARTIAL 41 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 109).

## 2026-07-27 Task 110 첫 주 채용 성장 목표 — IMPLEMENTED / PROJECT PARTIAL

- Day 5의 기존 “향후 인력 필요 파악” 문구를 실제 `[P] P.A. Phone → 채용` 행동으로 바꿨다.
- Day 5 이후 낮 목표와 운영 체크리스트는 고용 0명일 때 첫 생산자/전문가 고용을 안내하고, 고용 뒤에는 실제 후보 이름·한글 역할·총 인원수를 완료 상태로 표시한다.
- `HiringService.OnHired`를 표시 갱신에만 구독하며, roster는 실제 후보 집합을 이름순으로 정렬한다. 채용·비용·스폰·NPC 행동·저장 권위는 기존 시스템 그대로다.
- Day 7 첫 주 결산은 총 고용 인원과 최대 3명의 이름·역할을 보여 주며 추가 인원은 `외 N명`으로 요약한다.
- Runtime/Editor 오류 0, 정적 계약 29/29 PASS. `HiringService`·후보/레시피 에셋·경제/티어·NPC FSM·저장 스키마·씬·프리팹·패키지는 변경하지 않았다.
- 실제 Day 5 채용 전후 체크리스트→Day 7 결산과 1920×1080 가독성은 반복 직접 렌더 충돌 경계로 확인하지 못했다.
- 최신 집계: DONE 44 / PARTIAL 42 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 110).

## 2026-07-27 Task 111 생산자 납품 원자 거래 — IMPLEMENTED / PROJECT PARTIAL

- 실제 생산자 납품은 플레이어 돈을 먼저 차감한 뒤 가방 추가 실패 시 생산자 재고를 삭제해 돈과 상품을 함께 유실했다. `Inventory.AddInstance`도 꽉 찬 가방의 기존 스택 일부를 채운 뒤 false를 반환할 수 있었다.
- `Inventory.CanAddInstance`가 메타 일치 스택 여유와 빈 슬롯을 읽기 전용으로 계산하며, `AddInstance`는 전량 수용 가능할 때만 변경된다.
- 생산자는 가방 공간을 결제 전에 검사한다. 가방 가득 참·잔액 부족은 돈과 NPC 재고를 유지하고, 결제 뒤 예외 실패는 기존 Economy 권위로 전액 환불하며 재고를 보존한다.
- 성공 시 원본 `ItemInstance`의 quality/currentPrice를 보존해 플레이어 재고로 이전한 뒤에만 NPC 재고를 제거한다. 성공·공간/잔액 보류·환불은 기존 NPC 말풍선으로 표시한다.
- Runtime/Editor 오류 0, 거래 안전 계약 30/30과 diff 검사 PASS. `EconomyService`·LongPlay 코드·저장·FSM/스케줄·씬·프리팹·에셋·패키지는 변경하지 않았다.
- 실제 가방 가득 참 보류→공간 확보→재납품과 말풍선은 반복 직접 렌더 충돌 경계로 확인하지 못했다.
- 최신 집계: DONE 44 / PARTIAL 43 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 111).

## 2026-07-27 Task 112 2주차 운영 캠페인 — IMPLEMENTED / PROJECT PARTIAL

- Day 7 저장/Day 8 전환 뒤 일반 반복 문구만 남던 구간을 Day 8~14의 명시적 운영 캠페인으로 연결했다.
- 순서는 B09 예비 재고 보관→가공품 1건 판매→지원 인력 채용→서로 다른 2카테고리 판매→Tier 1 실내 잡화점→다음 날 광장 변화 확인→서로 다른 2상품 판매다.
- 완료 판정은 기존 보관함 내용, 당일 판매 기록, 실제 채용 roster, Tier, 활성 마을 변화만 읽는다. 플레이어 행동을 대신 수행하거나 돈·아이템·Tier·저장을 변경하지 않는다.
- Day 1~7 계획, Day 7 완주 모달과 Day 8 저장 전환, 생활 활동·상품 2종·진열/가격·개점·판매/정산 체크리스트를 보존했다.
- Runtime/Editor 오류 0, 2주차 계획·상태·권위 계약 40/40과 diff 검사 PASS. 실제 Day 7→8/대표 목표 전환과 1920×1080 가독성은 반복 직접 렌더 충돌 경계로 확인하지 못했다.
- 최신 집계: DONE 44 / PARTIAL 44 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 112).

## 2026-07-27 Task 113 Tripo 장기 정책 재감사·B12 항구 충돌 방지 — IMPLEMENTED / PROJECT PARTIAL

- 최신 Tripo 임시 에셋 정책과 첨부 Grid 배치 기준은 기존 8분류·캐릭터 정체성 보존·기능 가구/Placeable·원본 비파괴·출처 ADR에 통합 상태임을 재확인했다.
- 전체 모델 수는 FBX 174/OBJ 150/GLB 0/Blend 0이며 Nature Pack 외 고유 FBX는 24개다. C-01~C-09 importer의 `tripo_node_*`는 직접 Tripo 추정 증거지만 개별 상업 이용 증빙을 대체하지 않는다.
- B12의 역사적 10×5m Box/Obstacle은 실제 약 3.63×1.96m Visual보다 커 해안의 보이지 않는 플레이어 벽과 NPC carving 공백을 만들 수 있었다.
- 활성 map/legacy B12에서 Visual 로컬 mesh bounds를 계산해 명백히 큰 루트 `BoxCollider`와 box형 `NavMeshObstacle`만 축소한다. 실패 시 기존 물리를 유지하고 어떤 축도 키우지 않는다.
- Runtime/Editor 오류 0, 정적 계약 20/20과 대상 diff 검사 PASS. 원본/프리팹/씬/교역/Placeable/저장/패키지는 변경하지 않았다.
- 실제 해안 이동·NPC 우회·동일 GameCamera는 반복 직접 렌더 충돌 경계로 확인하지 못했다.
- 최신 집계: DONE 44 / PARTIAL 45 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 113).

## 2026-07-27 Task 114 첫 달 운영 캠페인·Day 30 완주점 — IMPLEMENTED / PROJECT PARTIAL

- Day 15~30에 보관, 가공품, 2~3명 지원 인력, 2~3카테고리/상품, Tier 1, 마을 변화, Utility/Luxury, 최종 예비 재고를 순환하는 16개 운영 계획을 추가했다.
- 체크리스트는 기존 `StorageBox`, 당일 `SalesLogManager`, `HiringService`, `TierService`, `VillageCultureVisualController` 상태만 읽고 플레이를 대신 수행하지 않는다.
- Day 30 Settlement는 누적 매출·돈·Tier·평판·고용 roster·주된 마을 변화·당일 정산을 요약하며 저장 후 종료 또는 Day 31 계속을 제공한다.
- Day 7 자동 보급/첫 주 완주/Day 8 전환은 그대로 보존했고 저장 스키마·경제·판매·제작·채용·Tier·마을 변화 권위는 변경하지 않았다.
- Runtime/Editor 순차 빌드 경고 0·오류 0, 정적 계약 56/56과 대상 diff 검사 PASS. 병렬 빌드 출력 잠금과 첫 정적 검사 파서 오류는 `BUG_LOG.md`에서 해결 상태로 기록했다.
- 실제 Day 15~30 대표 전환, Day 30 모달 두 분기, Day 31 이어하기와 1920×1080 가독성은 반복 직접 렌더 충돌 경계로 확인하지 못했다.
- 최신 집계: DONE 44 / PARTIAL 46 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 114).

## 2026-07-27 Task 115 Tier 1 대장간·철제 도구 첫 달 가치사슬 — IMPLEMENTED / PROJECT PARTIAL

- 첫 달 목표 달성 가능성을 데이터부터 역추적했다. B07 BuildingData·설계도, `Recipe_ToolSet`, `Item_12_ToolSet`은 모두 Tier 1이지만 `ShopCustomizationController`만 B07을 Tier 3으로 지연해 실제 제작 루프를 막고 있었다.
- B07 배치 최소 Tier와 장부 설계도 보상을 Tier 1에 맞췄다. B05 starter, B06 Tier 2, B08 Tier 3, TierService의 10,000G/100,000G 조건은 그대로다.
- Day 23은 활성 B07+정확한 당일 철제 도구+다른 상품 1종, Day 24는 활성 B07+철제 도구+Processed 1건을 요구한다. 씨앗 Utility 우회와 Tier 2 전 불가능한 Luxury 목표를 제거했다.
- 안내는 빈 진열대 두 칸 회수→B07 3×2 배치→B05 Plank1+B07 Ore4→IronBar2→ToolSet1→혼합 판매 경로를 보여 주며 어떤 아이템·Tier·제작·판매도 대신 수행하지 않는다.
- Day 14 15,000G 뒤 Day 15가 3,900G로 역행하던 표시 목표를 Day 15=16,000G→Day 30=31,000G→Day 31 이후 단조 증가로 교정했다. 이는 표시 목표이며 Tier 조건은 바꾸지 않았다.
- Runtime/Editor 순차 빌드 오류 0. 기존 Unity 소스 생성기 CS8785와 Editor CS0414 경고만 유지됐다. 실행 가능 소스 계약 39/39과 대상 `git diff --check` PASS.
- Unity는 반복 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 실제 Tier 1 보상·배치/접근·제작·Day 23/24 판매/화면 확인 전 PARTIAL이다.
- 최신 집계: DONE 44 / PARTIAL 47 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 115).

## 2026-07-27 Task 116 안전 GameView 캡처 기반 1차 전환 — IMPLEMENTED / PROJECT PARTIAL

- `Assets/Editor`의 실제 직접 `camera.Render()` 호출을 전수 감사해 12곳을 확인했다. ThemeCorner의 주석뿐인 언급은 호출 수에서 제외했다.
- ThemeCorner에서 실제 실행을 완료했던 일반 GameView `ScreenCapture` 흐름을 `PA_SafeGameViewCapture`로 추출했다. 해상도 설정, Canvas/TMP 갱신, 안정화 대기, 새 PNG freshness/최소 크기 확인과 카메라·화면 상태 복원을 한 계약으로 묶었다.
- 두 번째 네이티브 충돌 지점인 `PA_ShopCustomizationValidator`와 Task 115의 Tier/B07 검증기 `PA_ShopProgressionUnlockValidator`를 공용 비동기 경로로 전환했다. 두 대상의 직접 `Camera.Render()` 호출은 0이다.
- Runtime 빌드는 경고/오류 0, Editor 빌드는 오류 0과 기존 CS8785/CS0414 경고 2개다. 안전 캡처 계약 28/28과 대상 diff 검사 PASS.
- 저장소에는 Character/Cottage/CustomerPanel/DemoView/FinalPresentation/GatheringShop/OutdoorPlacement/ShopEvolution/VillageCulture/Workbench의 직접 렌더 10곳이 남아 있다.
- Unity는 동일 원인 세 번째 실행 금지 경계를 지켜 실행하지 않았다. 잔여 10곳 전환과 사람 승인 D3D11 실제 캡처 전 Task 116은 PARTIAL이다.
- 최신 집계: DONE 44 / PARTIAL 48 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 116).

## 2026-07-27 Task 117 안전 GameView 캡처 기반 2차 전환 — IMPLEMENTED / PROJECT PARTIAL

- VillageCulture, CustomerPanelLayout, FinalPresentation 검증기의 별도 RenderTexture/`camera.Render()` 캡처를 Task 116 공용 `PA_SafeGameViewCapture`로 전환했다.
- 마을 변화 3장, 고객 패널 1장, 최종 프레젠테이션 6장은 각각 await되어 캡처 완료 뒤에만 다음 판매·날짜·UI 상태로 진행한다.
- 시장 마커/FOV 46/전체 레이어/1920×1080 구도, VillageCulture·CustomerPanel의 경고 전용 캡처 정책, FinalPresentation의 일반·희귀 가격 포함 출력 목록을 보존했다.
- 세 대상의 직접 렌더 자원과 실제 호출은 0이다. 저장소 잔여는 Character/Cottage/DemoView/GatheringShop/OutdoorPlacement/ShopEvolution/Workbench 7곳이다.
- Runtime 경고/오류 0, Editor 오류 0과 기존 CS8785/CS0414 경고 2개. 교정 정적 계약 38/38과 대상 diff 검사 PASS.
- Unity는 반복 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 잔여 7곳 전환과 사람 승인 D3D11 실제 캡처 전 PARTIAL이다.
- 최신 집계: DONE 44 / PARTIAL 49 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 117).

## 2026-07-27 Task 118 안전 GameView 캡처 기반 3차 전환 — IMPLEMENTED / PROJECT PARTIAL

- DemoView, GatheringShop, OutdoorPlacement 검증기의 별도 RenderTexture/`camera.Render()` 캡처를 공용 `PA_SafeGameViewCapture`로 전환했다.
- DemoView의 실내 11초/외부 4.5초 준비와 실제 추적 카메라 2560×1440, GatheringShop의 해안→시장 5단계 1920×1080, OutdoorPlacement의 동일 직교 구도 전후 1280×720을 보존했다.
- 모든 캡처를 await하고 `CameraController`를 캡처 동안만 동결한 뒤 복원한다. OutdoorPlacement의 PNG 크기 판정도 유지했다.
- 세 대상의 직접 렌더 자원과 실제 호출은 0이다. 저장소 잔여는 Character/Cottage/ShopEvolution/Workbench 4곳이다.
- Runtime 경고/오류 0, Editor 오류 0과 기존 CS8785/CS0414 경고 2개. 정적 계약 35/35와 대상 diff 검사 PASS.
- Unity는 반복 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 잔여 4곳 전환과 사람 승인 D3D11 실제 캡처 전 PARTIAL이다.
- 최신 집계: DONE 44 / PARTIAL 50 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 118).

## 2026-07-27 Task 119 안전 GameView 캡처 기반 4차 전환 — IMPLEMENTED / PROJECT PARTIAL

- Character, Cottage, Workbench 검증기의 별도 RenderTexture/`camera.Render()` 캡처를 공용 `PA_SafeGameViewCapture`로 전환했다.
- Character의 1600×900 소스 lineup·runtime idle/walk와 실제 이동 시간, Cottage의 1920×1080 전경·4방향·최종·runtime 7개 파일, Workbench의 감사/최종 4방향·runtime baseline/final을 보존했다.
- 모든 캡처를 await하고 실제 게임 카메라의 `CameraController`, Cottage 격리 renderer, Workbench 임시 카메라·조명·바닥을 예외 경로에서도 복원한다.
- 세 대상의 직접 렌더 자원과 실제 호출은 0이다. 저장소 잔여는 `PA_ShopEvolutionVisualFinalizer` 1곳이다.
- Runtime 경고/오류 0, Editor 오류 0과 기존 CS8785/CS0414 경고 2개. 정적 계약 42/42와 대상 공백 검사 PASS.
- Unity는 반복 직접 렌더 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 마지막 1곳 전환과 사람 승인 D3D11 실제 캡처 전 PARTIAL이다.
- 최신 집계: DONE 44 / PARTIAL 51 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 119).

## 2026-07-27 Task 120 안전 GameView 캡처 기반 최종 전환 — IMPLEMENTED / PROJECT PARTIAL

- 마지막 ShopEvolution 검증기의 별도 RenderTexture/`camera.Render()` 캡처를 공용 `PA_SafeGameViewCapture`로 전환했다.
- B02~B04의 4방향 소스 감사 12장과 runtime baseline/Tier 1~3 after를 await하며 1600×900, 초기 4초, 단계별 0.75초, orthographic size 6.6과 기존 파일명을 보존했다.
- 런타임 단일 `Task` 가드, `CameraController`·`clearFlags` 복원, Tier 전후 배치 저장 JSON 동등성 판정을 유지했다.
- 대상의 직접 렌더 자원과 실제 호출은 0이며 저장소 전체 실제 직접 `Camera.Render()` 호출도 0이다.
- Runtime 경고/오류 0, Editor 오류 0과 기존 CS8785/CS0414 경고 2개. 정적 계약 36/36 PASS.
- Unity는 반복 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 사람 판단 뒤 격리 D3D11 GameView PNG와 순차 검증 전 PARTIAL이다.
- 최신 집계: DONE 44 / PARTIAL 52 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 120).

## 2026-07-27 Task 121 Day 31~45 두 번째 달 진입 캠페인 — IMPLEMENTED / PROJECT PARTIAL

- Day 30 완주 뒤 일반 반복 안내로 돌아가던 Day 31~45에 열다섯 개의 날짜별 계획과 실제 상태 체크리스트를 연결했다.
- 기존 B09 보관, Processed 판매, Hiring roster, 3~4상품/3카테고리 구성, B07/ToolSet, 활성 마을 변화, 영업 전 4상품 준비, 누적 매출만 읽으며 플레이어 행동·보상·저장을 대신 만들지 않는다.
- 기존 단조 매출 목표를 공용 읽기 API로 사용해 Day 31 32,500G→Day 45 53,500G를 표시한다. Tier 2 100,000G, B06 Tier 2, B08 Tier 3, Day 1~30과 Day 30 완주 UI는 변경하지 않았다.
- Runtime 빌드 오류 0과 기존 CS8785 경고 1개, Editor 오류 0과 기존 CS8785/CS0414 경고 2개. Day 31~45 계약 23/23 PASS.
- 씬·프리팹·에셋·저장 스키마·경제/구매/제작/채용/Tier/마을 변화 권위·패키지는 변경하지 않았다.
- Unity는 반복 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 실제 Day 30→31, 대표 Day 35/40/45 전환, 1920×1080 가독성과 Day 46 폴백 확인 전 PARTIAL이다.
- 최신 집계: DONE 44 / PARTIAL 53 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 121).

## 2026-07-27 Task 122 Day 46~76 Tier 2 성장 캠페인 — IMPLEMENTED / PROJECT PARTIAL

- Day 45 뒤 일반 장기 운영 폴백으로 돌아가던 Day 46~76을 기존 지역 경제 시스템의 7일 운영 리듬으로 연결했다.
- Day 46~75는 보관→Processed 판매→지원 인력 3명+판매 상품 4종 준비→3카테고리→활성 B07+ToolSet+Processed→활성 마을 변화+4상품→누적 매출 점검을 반복한다.
- 보관 목표는 주기별 12→20개, 가공 판매는 2→4건으로 상승한다. Day 76은 기존 100,000G 도달 뒤 `TierService.CurrentTier >= 2`만 완료로 인정한다.
- 기존 매출 수식은 Day 46 55,000G→Day 76 100,000G이며 Tier2.asset의 100,000G·평판 0·자동 승급, B06 Tier 2/B08 Tier 3를 그대로 보존했다.
- Runtime 빌드 오류 0과 기존 CS8785 경고 1개, Editor 오류 0과 기존 CS8785/CS0414 경고 2개. Day 46~76 계약 34/34 PASS.
- 씬·프리팹·에셋·저장 스키마·경제/구매/제작/채용/Tier/마을 변화 권위·패키지는 변경하지 않았다.
- Unity는 반복 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 대표 Day 46/52/59/66/73, Day 76 자동 승급, 1920×1080 가독성과 Day 77 폴백 확인 전 PARTIAL이다.
- 최신 집계: DONE 44 / PARTIAL 54 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 122).

## 2026-07-27 Task 123 Day 77~90 Tier 2 주방 가치사슬 캠페인 — IMPLEMENTED / VERIFICATION PARTIAL

- B06은 기존 Tier 2 장부 해금 Kitchen 작업대이고 BreadLoaf·구운 감자·생선구이 세 기존 레시피와 출력 Item이 모두 유효함을 확인했다.
- Day 77~90을 B06 설치→세 조리 라인→2종/3종 메뉴→영업 전 3종 준비→Chef 고용→3카테고리/4개 배치 판매→Processed 마을 변화→Day 90 가치사슬 완주로 연결했다.
- 완료 판정은 기존 배치·인벤토리/핫바/진열·당일 판매 기록·고용 명단·마을 변화·누적 매출만 읽는다. 새 레시피·아이템·퀘스트·보상·저장 필드·Tier/경제 수치·자동 행동은 없다.
- Runtime 빌드 오류 0과 기존 CS8785 경고 1개, Editor 오류 0과 기존 CS8785/CS0414 경고 2개. 대상 코드 공백 검사 PASS.
- 정적 계약은 목표/체크리스트 호출부 2개를 3개로 잘못 기대한 검사식 오류로 중단됐다. 프로젝트 실패 정책에 따라 재시도하지 않고 `BUG_LOG.md`에 기록했다.
- Unity는 반복 네이티브 충돌 2회 경계를 지켜 실행하지 않았다. 정적 계약 복구와 실제 B06 배치·조리·판매·Chef·Processed 변화·Day 90·1920×1080 확인 전 PARTIAL이다.
- 최신 집계: DONE 44 / PARTIAL 55 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 123).

## 2026-07-27 Task 124 Task 123 주방 캠페인 정적 계약 복구 — VERIFICATION PARTIAL

- 잘못된 호출부 3개 기대값을 실제 목표/체크리스트 2개로 바로잡은 계약은 PASS했다.
- Day 77~90 계획/case 각 14개, 단일 판정 정의, Day 76 경계, B06 Kitchen 프리팹, 세 Kitchen 레시피, 세 Processed 출력과 요구 리소스 존재 등 44개 계약을 확인했다.
- B06 최소 Tier C# 표현과 구운 감자·생선구이 Item 이름 YAML 표현 2개가 검사 정규식과 일치하지 않아 44/47에서 중단했다.
- 같은 검사 재시도나 코드/데이터 수정, Unity 실행은 하지 않았다. 다음 단일 작업에서 세 실제 직렬화 행을 먼저 확인해야 한다.
- 최신 집계: DONE 44 / PARTIAL 56 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 124).

## 2026-07-27 Task 125 B06 Tier·출력 Item 이름 권위 행 감사 — DONE

- B06 최소 Tier 실제 C# 행은 switch case에서 정확히 `return 2`다.
- 구운 감자와 생선구이 `itemName`은 Unity YAML Unicode escape로 저장됐으며 해석값은 의도한 한국어 이름과 정확히 일치한다.
- Task 124의 세 실패는 데이터 결함이 아니라 검사 표현 불일치로 확정했다.
- Task 123 정적 증거는 44개 자동 계약+3개 직접 권위 행으로 47/47이다. 실제 Unity B06 배치·제작·판매·마을 변화 확인은 여전히 대기한다.
- 코드·에셋·Unity 상태를 변경하지 않았다.
- 최신 집계: DONE 46 / PARTIAL 55 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 125).

## 2026-07-27 Task 126 Day 91~105 Tier 3 공동 공방 캠페인 — IMPLEMENTED / PROJECT PARTIAL

- Tier 3의 실제 조건은 평판 3·자동 승인인데 기존 플레이 경로에는 `AddReputation` 호출이 없어 도달할 수 없었다.
- Day 91 이후 전문 주민 재료 요청을 완료하면 기존 일일 활동 저장 표식으로 하루 한 번 평판 +1을 지급하고, 세 번째 날 기존 `TierService`가 Tier 3로 자동 승급한다.
- Day 91~105를 평판 1/2/3→B08→의류/가구→재단사→Luxury 마을 변화→Day 105 완주로 연결했다.
- 완료 판정은 기존 배치, 인벤토리/핫바/진열, 당일 판매, 고용 roster, 마을 변화, 누적 매출만 사용한다.
- Runtime/Editor 오류 0, 기존 CS8785/CS0414 경고만 유지. 정적 계약 24/24와 대상 공백 검사 PASS.
- Unity는 반복 네이티브 충돌 2회 경계로 실행하지 않았다. 실제 3일 평판·Tier 3·B08·제작/판매·Luxury 변화·UI 확인 전 PARTIAL이다.
- 최신 집계: DONE 46 / PARTIAL 56 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 126).

## 2026-08-04 Task 127 B05~B08 전문 주민 전면 접근 연결 — IMPLEMENTED / PROJECT PARTIAL

- 기존 GRID 배치의 Workbench interaction 셀을 전문 주민 이동 목적지로 연결했다. 주민은 NavMeshObstacle 내부 원점 대신 회전된 전면 셀 중 완전 경로가 있고 예약되지 않은 지점을 사용한다.
- 셀 단위 예약/해제, 작업대 이동·회수 시 이동/가공 중단, 도착 후 작업대 정면 보기, 저장 상태 복원 뒤 현재 배치 기준 재접근을 추가했다.
- 메인 씬·프리팹·모델·재질·저장·경제/제작 권위는 보존했다.
- 이전 검증 경로·문서 검사 실패는 매트릭스 직접 증거와 정확한 순차 빌드로 복구해 `BUG_LOG.md`에서 RESOLVED 처리했다.
- Runtime 오류 0/기존 CS8785 경고 1개, Editor 오류 0/기존 CS8785·CS0414 경고 2개. 접근·예약 계약 29/29와 대상 공백 검사 PASS.
- Unity는 반복 네이티브 충돌 2회 경계로 실행하지 않았다. 실제 B05~B08 주민 접근·정면·겹침 방지·제작 확인 전 PARTIAL이다.
- 최신 집계: DONE 46 / PARTIAL 57 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 127).

## 2026-08-04 Task 128 관광객 손님 정상 플레이 진입 — IMPLEMENTED / PROJECT PARTIAL

- 기존 고객 8명 전원이 유효한 마을 일과표를 가진 주민이고 정상 플레이 관광객이 0명임을 직렬화 GUID·씬 생성기·기존 검증기에서 확인했다.
- Day 2+ 개점 뒤 세션 한정 관광객이 영업당 최대 2명/동시 1명 들어온다. 주민의 검증된 캐릭터 시각만 재사용하며 `[관광객]` 태그와 별도 런타임 이름으로 표시된다.
- 관광객은 일과표·생산/전문가·대화/친밀도·채용·저장 기록 없이 기존 쇼핑 FSM과 구매 수학을 사용하고, 쇼핑 뒤 진입점까지 걸어 나가 제거된다.
- Day 1 시나리오, 주민 Rest 방문 lease, 동시 고객 상한, 경제·판매·저장 권위와 원본 에셋은 보존했다.
- Runtime 오류 0/기존 CS8785 경고 1개, Editor 오류 0/기존 CS8785·CS0414 경고 2개. 관광객 계약 46/46과 대상 공백 검사 PASS.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트로 실행하지 않았다. 실제 관광객 입장·구매/거절·퇴장과 화면 확인 전 PARTIAL이다.
- 최신 집계: DONE 46 / PARTIAL 58 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 128).

## 2026-08-04 Task 129 Day 106+ 본사 감사·Tier 4 최종 완주 — IMPLEMENTED / PROJECT PARTIAL

- Day 105 뒤 실제 성장 권위를 감사해 정상 플레이 평판이 Tier 2의 3점에서 멈추지만 `AuditService`는 Tier 4 감사에 평판 5를 요구해 최종 승급이 불가능함을 확인했다.
- Day 106+ Tier 3 전문 주민 요청은 기존 일일 활동 저장 표식을 재사용해 감사 요구 평판까지만 하루 1점을 추가한다.
- Day 106+ 목표/체크리스트는 실제 감사 조건인 누적 매출 500,000G·평판 5·고용 3명과 다음 정기 감사일을 안내한다. 최종 승급은 기존 AuditService의 수동 승급 권위만 사용한다.
- Tier 4 통과 뒤 첫 정산에서 전체 캠페인 기록을 표시하고 저장 후 다음 날 자유 운영 또는 저장 후 종료를 선택할 수 있다. 별도 저장 필드는 추가하지 않았다.
- Runtime 오류 0/기존 CS8785 경고 1개, Editor 오류 0/기존 CS8785·CS0414 경고 2개. 최종 감사 계약 40개 자동+1개 직접 권위 PASS.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트로 실행하지 않았다. 실제 평판 4/5·정기 감사·Tier4·완주 모달·저장 왕복 전 PARTIAL이다.
- 최신 집계: DONE 46 / PARTIAL 59 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 129).

## 2026-08-04 Task 130 본사 감사 성공·실패 플레이어 피드백 — IMPLEMENTED / PROJECT PARTIAL

- 기존 감사 앱은 다음 감사일까지의 날짜만 표시했고, 실제 감사 미달 원인·성공·승급 보류 결과는 `_debugLastResult`와 콘솔에만 남았다.
- `AuditService`가 현재 매출·평판·고용, 조건별 완료 여부, 다음 감사일, 현재 세션의 최근 결과를 읽기 전용으로 제공하고 결과가 끝날 때 갱신 이벤트를 발행한다.
- 감사 앱은 세 조건의 현재/요구값과 완료·부족, 최근 실패/통과/최고/보류, 가장 가까운 다음 행동을 표시한다. 수동 승인 Tier 진행 바도 실제 세 조건 진행을 사용한다.
- 500,000G·평판 5·고용 3명·7일 주기, 단독 `TryManualAdvance()`, `LastAuditDay` 저장, Tier/경제/채용/씬/프리팹/저장 스키마는 변경하지 않았다.
- Runtime 오류 0/기존 CS8785 경고 1개, Editor 오류 0/기존 CS8785·CS0414 경고 2개. 정적 계약 48/48 PASS.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트로 실행하지 않았다. 실제 실패/성공 즉시 갱신과 1920×1080 가독성 확인 전 PARTIAL이다.
- 최신 집계: DONE 46 / PARTIAL 60 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 130).

## 2026-08-04 Task 131 Tripo 장기 정책 재확인·B06 Kitchen 보정 — IMPLEMENTED / PROJECT PARTIAL

- 첨부 GRID/Tripo 지시는 기존 P1~P5 배치 아키텍처, v10 placeable 저장, `PLACEABLE_ASSET_GUIDE.md`, `TRIPO_ASSET_AUDIT.md`에 이미 통합되어 있음을 코드·에셋·문서로 재확인했다.
- 에셋 수는 FBX 174/OBJ 150/GLB 0/Blend 0이며 non-Nature 고유 FBX 24개다. 캐릭터 외형 보존, 기능 가구 우선, 1~8 개별 분류, 원본 비파괴, 라이선스/출처 배포 게이트를 유지한다.
- 감사상 다음 미완성인 B06 Kitchen은 기존 Visual과 레시피/배치 권위를 유지하면서 실제 renderer bounds보다 명백히 큰 X/Z Box/box형 Carving만 축소 전용으로 정합한다.
- 로컬 `-Z` 앞 interaction anchor와 성공한 제작 뒤 0.72초 모델 pulse를 추가했다. 원시 큐브·무작위 소품·새 가짜 제작 시스템은 없다.
- B05, B06 Tier 2/2×2, Kitchen 레시피 3종, 전문 주민 접근, 저장, FBX·프리팹·씬·재질·BuildingData는 보존했다.
- Runtime 오류 0/기존 CS8785 경고 1개, Editor 오류 0/기존 CS8785·CS0414 경고 2개. 정적 계약 30/30 PASS.
- Unity는 반복 네이티브 충돌의 사람 판단 게이트로 실행하지 않았다. 실제 B06 배치·전면 접근·물리 경계·Bread 제작 pulse·동일 GameCamera 전후 확인 전 PARTIAL이다.
- 최신 집계: DONE 46 / PARTIAL 61 / TODO 2 / BLOCKED 4 / DECISION_REQUIRED 18(총 131).

## 2026-08-04 WORLD-000 Procedural Island + Grid Terraforming Architecture Reframe — DOCUMENTED / NEEDS HUMAN REVIEW

- 선행 loop-state는 Task 131 completed였다. 이후 시작된 것으로 보이는 `AudioManager.cs`/`SalesLogManager.cs` 변경(112 insertions, 4 deletions)은 검증·기록 전 상태로 보존했고 WORLD-000에 섞거나 수정하지 않았다.
- 현행 outdoor 배치는 2m flat grid와 47×47 fixed zone, 현행 map은 primitive Cube ground/beach/water/road/buildings, save는 v10 absolute buildings + grid placeables, nav는 baked surface/AddData와 작은 shop expansion full build를 사용한다.
- 권장안은 2m cell, 16×16 Chunk, 1m 0~6 높이의 custom chunk mesh heightfield다. Unity Terrain/voxel은 권위 지형에서 제외한다.
- 기본 world는 seed+generationVersion으로 재생성하고 modified cells/buildings/decorations/resources/bridges/ramps/paths의 sparse delta만 저장한다. 실제 schema 변경은 하지 않았다.
- NPC/shop 위치는 stable building instance + role anchor로 단계 이관하며 기존 Shop/Economy/Purchase/NPC FSM/Grid/Build/Save backend는 adapter로 재사용한다.
- Prototype_FirstDay는 Golden Regression Scene으로 보존하고, 구현은 승인 후 WorldSandbox에서 Gate 1~4를 거쳐 WORLD-009 이후 Gate 5를 연결한다. MainGame 통합은 별도 사람 승인 티켓이다.
- AI Navigation 2.0.12의 `NavMeshSurface.UpdateNavMesh`/Volume API를 로컬 source에서 확인했으나 per-Chunk seam/성능/runtime 안정성은 WORLD-008 증거가 없어 사람 review를 유지한다.
- 신규/갱신 문서만 변경했으며 게임 코드·씬·에셋·저장 스키마·package·ProjectSettings·Git 상태 변경 작업은 하지 않았다. WORLD-001은 시작하지 않았다.
- 상태: `needs_human_review` (scene 생성, prototype 수치, save schema, runtime nav, MainGame integration gate).

## 2026-08-04 BASELINE-STABILIZE-001 — NEEDS HUMAN RUNTIME REVIEW

- 기준선은 `master@0b07d71`, `origin/master` 동기화, 시작 시 clean이었다. Preflight는 `READY_FOR_BOUNDED_TICKET_LOOP`, D3D11 승인, Unity 프로세스 0을 반환했다.
- meta/GUID/대용량 history/SubmissionPackages/씬/ProjectSettings/크래시 무결성 검사는 통과했다. Unity가 건드린 폰트와 EditorBuildSettings는 HEAD blob과 동일하게 정리됐고 저장할 내용 변경은 없다.
- Runtime/Editor 컴파일은 오류 0으로 통과했다. 기존 CS8785 1개와 Editor의 기존 CS8785/CS0414 2개만 유지된다.
- Unity 6000.3.2f1 D3D11 로드와 package resolve는 통과했다. Core 5종과 SaveRoundTrip, CustomerArrival/Presentation/InteriorCustomer는 PASS다.
- `CustomerPanelLayout`은 Village 높이를 92px로 가정해 4px 겹치는 실제 결함을 찾았다. `CustomerPreferencePresentationController` 좌표를 20px 내리는 단일 최소 수정 후 재검증 PASS다.
- `ShopCustomization`과 `ShopProgressionUnlock`은 각 캡처 직전 기능 단언까지 통과했으나 batchmode GameView PNG가 기록되지 않았다. 동일 환경 실패 2회로 티켓 중단 조건이 발동했다.
- Save v10·v9→v10 migration과 Audio/Sales 정적 계약은 정합하다. 실제 판매음 청취, B06 Kitchen 통로·콜라이더 체감·스케일·시야·pulse, 1920×1080 Game View는 사람 검토가 필요하다.
- WORLD-001/WorldSandbox는 시작하지 않았다. 기능 기준선의 자동 증거는 강하지만 필수 상점 시각 검증 2건이 환경 종료됐으므로 최종 판정은 `NEEDS_HUMAN_RUNTIME_REVIEW`다.

## 2026-08-05 BASELINE-CRAFTING-UI-FIX-001 — BASELINE READY FOR WORLD-001

- 사람 런타임 검토는 입력 Smoke `PASS`, Console `PASS_WITH_EXACT_ALLOWLIST`, 기본 상점 기능 가능, Customer UI 치명 차단 없음으로 종료 승인됐다. 추가 수동 캡처는 요구하지 않는다.
- 실제 기준선 차단자는 Basic 제작 UI가 레시피 2개를 집계·생성하면서도 런타임 `Viewport`의 `Mask`용 Image를 완전 투명하게 만들어 두 카드를 모두 가린 것이었다. 재료 부족 필터나 데이터 전달 실패는 아니었다.
- `CraftingUI.cs`에서 Mask 그래픽만 불투명하게 바꾸고 `showMaskGraphic=false`는 유지했다. 카드 생성 직후 Content layout도 확정해 첫 프레임의 0 크기 가능성을 제거했다. 씬·프리팹·제작 데이터·해금·인벤토리·Save authority는 변경하지 않았다.
- D3D11 전용 검증 결과: Basic recipe 2 / card 2, active 2, 각 672×96, Viewport 내부, CanvasGroup/Image alpha 정상, 결과 아이템과 보유/필요 재료 표기 PASS다.
- Wood 0/2에서도 두 카드가 유지되고 실행은 차감·지급 없이 차단됐다. Wood 2/2에서는 Plank 버튼이 활성화되어 Wood 2 차감, Plank 1 지급, B05 성공 pulse가 모두 통과했다.
- Runtime/Editor 컴파일 오류 0, 전용 validator PASS, 기존 ProcessingChain(BreadLoaf) 회귀 PASS, blocking Console pattern 0, 신규 crash 0이다.
- 자동 Game View 캡처는 알려진 timeout 재시도 한계를 존중해 실행하지 않았다. `CAPTURE_EVIDENCE_DEBT`이며 기능 차단이 아니다.
- 남은 항목은 드래그 ghost, 개발 overlay, Phone Hiring/Feed, 상점/맵 가독성, 이동식 판매대, 음색/B06 미감, 커스터마이징/Tier 캡처 증거의 비차단 backlog다.
- 최종 판정은 `BASELINE_READY_FOR_WORLD_001`. WORLD-001은 시작하지 않았으며 다음 명시적 bounded ticket에서만 진행한다.

## 2026-08-06 WORLD-001 WorldSandbox Bootstrap and Read-Only World Cell Grid — COMPLETE

- clean `master@befc1738dd868d24b06a2c8f673a13293b60d36b`에서 시작했고 `origin/master`와 일치했다. Unity 6000.3.2f1, D3D11만 사용했다.
- Editor builder로만 신규 `Assets/Scenes/WorldSandbox.unity`를 생성했다. 저장된 root는 Main Camera, Directional Light, WorldGrid 정확히 3개이며 상점·NPC·Save·게임플레이 manager 사본은 없다.
- 2m, 16×16셀, 16×16 chunk 1개, 1m elevation step, 허용 level 0..6, 초기 level 0, Default ground, Empty occupancy의 결정론적 읽기 전용 grid를 구현했다. cell (0,0)의 중심이 world origin이다.
- cell/world/index/chunk 변환과 안전한 경계 `Try*` API, row-major 읽기 전용 열거를 제공한다. 지형 mesh·물·길·저장·편집 mutation은 아직 만들지 않았다.
- line-only debug view는 일반 셀, chunk 경계, 원점, 희소 좌표와 hover 정보를 표시한다. 월드 데이터는 변경하지 않는다.
- D3D11 최종 validator는 256셀, 네 모서리, 경계 밖 처리, 256 index 왕복, chunk (0,0), seeded 10,000회 world/cell 왕복, 불변 계약, checksum `AD517449E587DBE5`, debug geometry, Missing Reference 0, manager 중복 0을 통과했다.
- Runtime/Editor compile 오류 0, blocking Console Error/Exception/Assert 0, 신규 crash 0이다. 기존 CS8785·CS0414 경고만 남는다.
- Prototype_FirstDay/MainGame, Save schema/authority, Packages, ProjectSettings는 시작 기준과 byte-for-byte 동일하다. 캡처는 시도하지 않아 `CAPTURE_EVIDENCE_DEBT`로 남긴다.
- 최종 판정은 `WORLD_001_COMPLETE`. WORLD-002는 시작하지 않았고 다음 명시적 bounded ticket을 기다린다.

## 2026-08-06 WORLD-002 Chunk Testbed + Height Level Mesh Prototype — COMPLETE

- `WorldSandbox`의 16×16 셀을 높이 level 0~6의 결정론적 동심 테라스로 초기화하고, Unity Terrain 없이 제한 높이 custom chunk mesh를 생성한다.
- 한 Chunk는 상면 256개, 노출 절벽면 280개, 정점 2,144개와 checksum `ADF9201BC8265BC5`를 가진다. 인접 셀보다 높은 면과 월드 경계만 절벽을 생성한다.
- 시각 mesh와 collider mesh를 분리하고 `MeshCollider`, 프로젝트 소유 runtime 재질 2슬롯, visual/collider 선택 dirty revision을 제공한다. 셀별 GameObject는 만들지 않는다.
- 합성 2-Chunk의 상면 seam 17지점 일치, winding/normal/UV/index/finite 값, mesh/collider bounds, 선택 rebuild가 D3D11 Edit/Play Mode에서 통과했다.
- WORLD-001 전체 회귀, Runtime/Editor compile 오류 0, blocking Console 0, 신규 crash 0을 확인했다. 기존 CS8785/CS0414 경고만 남는다.
- `Prototype_FirstDay`, `MainGame`, Save schema/authority, Packages, ProjectSettings는 변경하지 않았다. 캡처는 `CAPTURE_EVIDENCE_DEBT`다.
- 최종 판정은 `WORLD_002_COMPLETE`. M70 자동 진행 규칙에 따라 로컬 커밋 뒤 WORLD-003만 다음 활성 티켓으로 전환한다.

## 2026-08-06 WORLD-003 Single-Cell Raise/Lower Terraforming — COMPLETE

- WorldSandbox에서 좌클릭으로 셀을 선택하고 `R`/`F`로 한 level 올리거나 내리며 `Z`로 마지막 성공 편집을 한 번 되돌릴 수 있다.
- `(0,0)` 보호 셀, 범위 밖, level 0 하강, level 6 상승, 두 번째 undo는 typed failure로 끝나며 cell hash·revision·dirty Chunk·mesh에 변화가 없다.
- 성공 편집은 elevation만 1m 바꾸고 ground/water/path/occupancy를 보존한다. 내부 셀은 소유 Chunk 하나, Chunk 경계 셀은 맞닿은 양쪽 Chunk만 dirty 처리한다.
- `WorldChunkTerrain`은 성공/undo마다 visual mesh와 `MeshCollider`를 정확히 한 번 재생성한다. 인접 절벽 mask, collider raycast 높이, visual/collider bounds가 즉시 일치한다.
- WORLD-003 D3D11 Edit/Play와 WORLD-002·001 회귀, Runtime/Editor compile 오류 0, blocking Console 0, 신규 crash 0을 확인했다.
- 씬, Save schema/authority, Packages, ProjectSettings는 변경하지 않았다. 캡처는 `CAPTURE_EVIDENCE_DEBT`다.
- 최종 판정은 `WORLD_003_COMPLETE`; 로컬 커밋 뒤 WORLD-004만 다음 활성 티켓이다.

## 2026-08-06 WORLD-004 Ground/Path Paint and Water Cell Prototype — COMPLETE

- `WorldCellData`가 Grass/Soil/Sand/Rock, Dirt/Stone, water surface level/depth를 표현하고 농사 가능/보행 가능 상태를 단일 원본 데이터에서 파생한다.
- surface transaction은 protected cell, 잘못된 수면, dry drain, path-under-water를 원자적으로 거부한다. 성공한 ground/path/water 편집과 1단계 undo만 revision 및 owner/seam dirty Chunk를 발행한다.
- Chunk visual mesh는 grass/soil/sand/rock/dirt/stone/cliff/water 8슬롯이며 물 상면과 shoreline face를 표시한다. 물은 collider triangle stream에서 제외되고 Play Mode raycast는 terrain bed를 유지한다.
- WorldSandbox debug는 `G/T/V/X`를 제공하며 surface 편집은 visual mesh만 갱신한다. 기존 terraforming collider와 `R/F/Z` 계약은 회귀 통과했다.
- Runtime/Editor compile 오류 0, WORLD-004와 WORLD-003~001 D3D11 회귀 PASS, blocking Console 0, 신규 crash 0이다. 기존 CS8785/CS0414만 남는다.
- Prototype_FirstDay/WorldSandbox/MainGame, Prefab, Save schema, Packages, ProjectSettings는 변경하지 않았다. 캡처는 `CAPTURE_EVIDENCE_DEBT`다.
- 최종 판정 `WORLD_004_COMPLETE`. 아직 commit하지 않았으며 WORLD-005는 시작하지 않는다.

## 2026-08-10 WORLD-005 Relocatable Building MVP — COMPLETE

- 기존 `B09_StorageShed` 외형과 `StorageBox` 기능을 보존한 채 2m 셀 기준 4x3 footprint와 회전 가능한 외부 entrance sidecar를 연결했다. prefab 및 `BuildingData` 원본은 수정하지 않았다.
- `WorldBuildingPlacementService`가 WorldGrid occupancy의 단일 writer로서 배치·이동·회수를 원자 처리한다. 물/길/보호/점유/범위 밖/부적합 지면/절벽·단차/막힌 입구는 typed failure로 거부한다.
- WorldSandbox에서 실제 창고 모델 ghost를 유효 초록/무효 빨강으로 표시하고 `B/M/Q/E/Enter/Escape`로 배치·이동·회전·확정·취소할 수 있다. 점유 셀은 non-walkable이며 높이·surface 편집을 거부한다.
- `WorldSandbox`는 기존 3개 root를 유지하고 배치 service/debug controller 각 1개만 추가했다. 두 컴포넌트는 파일명과 일치하는 고정 GUID를 사용하며 embedded MonoScript는 0개다.
- Runtime/Editor compile 오류 0, WORLD-005와 WORLD-004~001 D3D11 회귀 PASS, blocking Console 0, 신규 crash 0이다. 기존 CS8785/CS0414만 남는다.
- Prototype_FirstDay와 MainGame blob, prefab, Save schema, Packages, ProjectSettings는 변경하지 않았다. 캡처는 `CAPTURE_EVIDENCE_DEBT`다.
- 최종 판정 `WORLD_005_COMPLETE`. 승인된 로컬 commit 뒤 기존 backlog `WORLD-006 World Seed + Minimal Island Generator`로 자동 진행한다.

## 2026-08-10 WORLD-006 World Seed + Minimal Island Generator — COMPLETE

- generationVersion 1과 configurable한 provisional 128x128 정의를 추가했다. 2m 셀·16x16 Chunk를 유지하며 Unity/System random 전역 상태나 seed별 예외 없이 integer hash/value noise로 결정 생성한다.
- 생성 섬은 ocean border, sand coast, meadow, forest, highland, river, pond를 갖는다. start, flat 4x3 shop, beach, meadow/forest/highland/pond activity anchor는 높이차 1 이하의 dry cell graph로 연결된다.
- Forage/Timber/Stone/Fish 후보는 generationVersion·seed·kind·coordinate 기반 stable spawn key를 사용한다. 실제 resource prefab이나 저장 schema는 추가하지 않았다.
- WorldSandbox에서 `J` 생성, `[`/`]` seed 변경, `K` clear가 가능하다. 128x128은 64개 chunk와 16,384 top face, 8개 surface material로 보이며 per-cell GameObject는 없다.
- 128 seed를 각각 두 번 생성한 256회 corpus는 1,599ms, checksum 128개 모두 고유, land ratio 44.9~58.2%, 모든 anchor 연결 PASS다. D3D11 WORLD-006 및 WORLD-005~001 회귀도 blocking Console 0으로 PASS했다.
- Prototype_FirstDay/MainGame, prefab, Save schema, Packages, ProjectSettings는 변경하지 않았다. 캡처는 `CAPTURE_EVIDENCE_DEBT`다.
- 최종 판정 `WORLD_006_COMPLETE`. 승인된 로컬 commit 뒤 `WORLD-006B Movable Shop Furniture`로 자동 진행한다.

## 2026-08-10 WORLD-006B Movable Shop Furniture — COMPLETE

- 새 병렬 시스템을 만들지 않고 기존 `ShopCustomizationController`와 2m `shop.interior` 격자를 M70 실내 가구 권위로 채택했다.
- 실제 고정 판매대 1개를 `(4,0)`으로 이동하고 270° 회전했다. hierarchy, `ShopSlot`, Bread 2개, 73G 표시가는 이동 뒤에도 유지됐다.
- 보호 입구 이동은 원자적으로 거부되고, 상점 입구→서비스 통로와 회전된 고객 접근점의 완전 NavMesh 경로가 유지됐다.
- 이동 취소가 authored 미세 오프셋을 셀 중심으로 스냅하던 결함을 수정해 시작 world Transform을 정확히 복원한다.
- 이동된 판매대에서 기존 전체 스택 판매 계약으로 146G가 EconomyService에 입금됐다. v10 `PlaceableSaveData`는 stable ID/zone/cell/rotation을 WORLD-007 입력으로 투영한다.
- 전용 D3D11, InteriorCustomer, SaveRoundTrip, FinalDemoRoute 회귀가 PASS했다. compile 오류·blocking Console·신규 crash는 0이다.
- Scene/Prefab/Save schema/Packages/ProjectSettings는 변경하지 않았다. 캡처는 `CAPTURE_EVIDENCE_DEBT`; 최종 판정 `WORLD_006B_COMPLETE`다.

## 2026-08-10 WORLD-007 World Persistence — COMPLETE

- 저장 스키마를 additive v11로 올렸다. v10 이하는 `LegacyFixed`로만 이행하며 기존 절대 좌표 건물·placeable을 절차 월드 데이터로 임의 변환하지 않는다.
- Procedural 월드는 seed/generationVersion, sparse terrain delta, B09 건물, 상점 가구 투영, 생성 자원 상태, 안전한 플레이어 위치를 저장·복원한다.
- 전체 payload를 먼저 검증한 뒤 base world를 재생성하고 delta와 점유를 적용한다. 중복 복원은 인스턴스를 늘리지 않으며 손상 데이터는 live world 변경 전에 거부된다.
- `WORLD007_Validation_Pass`, SaveRoundTrip, WORLD-004/005/006 회귀가 D3D11에서 PASS했다. compile 오류·blocking Console·신규 crash는 0이다.
- Scene/Prefab/Packages/ProjectSettings는 변경하지 않았다. 로컬 JSON의 temp/backup 원자 저장과 구 validator의 v10 고정 assertion은 후속 부채다.
- 캡처는 `CAPTURE_EVIDENCE_DEBT`; 최종 판정 `WORLD_007_COMPLETE`, 다음 활성 티켓은 승인된 WORLD-008이다.

## 2026-08-10 WORLD-008 Reachability and Navigation Prototype — COMPLETE

- 기존 공식 AI Navigation 2.0.12를 그대로 사용해 2×2 Chunk, 즉 32×32 cell 단위의 local NavMesh sector 권위를 추가했다. 패키지는 바꾸지 않았다.
- 논리 셀 그래프는 상하좌우 보행과 1 level 높이 차를 처리한다. seed 8008의 핵심 anchor 7개가 8,755개 reachable cell로 연결됐고 완전한 물 장벽은 고립으로 판정됐다.
- B09 배치·이동 전에 핵심 anchor와 건물 entrance 접근성을 검사한다. 유일한 통로를 막는 이동은 `CriticalRouteBlocked`로 원자 거부됐다.
- 내부 셀 변경은 sector 1개, sector 경계 변경은 정확히 이웃 포함 2개만 `UpdateNavMesh`로 비동기 갱신했다.
- 테스트 NPC는 갱신 중 pause, 최대 2m 재투영, complete-path repath 뒤 seam을 건너 도착하고 떨림 없이 정지했다.
- 실제 128×128 seed는 16개 sector를 57ms에 구성했고 시작점→상점 입구 NavMesh 경로가 완전했다.
- WORLD-007/005/006, InteriorCustomer, FinalDemoRoute 회귀가 PASS했다. compile 오류·전용 blocking Console·신규 crash는 0이다.
- Scene/Prefab/Packages/ProjectSettings/Save schema는 무변경이다. 캡처와 고객 validator 종료 뒤 late-visitor NavMesh teardown 진단은 비차단 부채다.
- 최종 판정 `WORLD_008_COMPLETE`; 다음 활성 티켓은 승인된 WORLD-009다.

## 2026-08-11 WORLD-009 Existing Gameplay World Adapter — COMPLETE

- WorldSandbox Play Mode가 seed 9009의 provisional 128×128 월드를 실제 grid로 설치하고 generated anchor에 Player Inventory, 실제 B01 MarketStall, B05 Workbench를 runtime-only로 연결한다.
- 기존 `PA_RuntimeSceneBinder`의 EconomyService, GameClock, DayNightShopLoopController, ItemRegistry, SalesLogManager, SaveManager를 단일 권위로 재사용한다. 병렬 gameplay manager stack은 없다.
- 검증 루프는 Timber x2 → Wood x2 → Recipe_Plank/B05 제작 → B01 ShopSlot 진열 → 밤 개점 → NpcController 구매 → Economy 1G → save → restart 상태 변조 → seed/resource/inventory/clock/shop/economy 복원이다.
- `Logs/WORLD009_Validation_DontSaveFix.log`와 WORLD-001/007/008, CraftingRecipeCard, CustomerArrival, FinalDemoRoute 회귀가 D3D11에서 PASS했다. blocking Console과 신규 crash는 0이다.
- 저장 schema v11, Prototype_FirstDay, WorldSandbox YAML, MainGame, Prefab, Packages, ProjectSettings는 변경하지 않았다. 다음 활성 티켓은 선승인된 WORLD-010이다.

## 2026-08-12 BETA-003 Crafting and Production Expansion — COMPLETE

- WorldSandbox의 generated Meadow/Highland에 기존 B06 Kitchen과 B07 Forge를 runtime-only로 배치했다. B05/B06/B07은 각각 Basic/Kitchen/Forge 기존 Workbench 권위를 유지한다.
- 플레이어 HUD는 낮 활동 산출물에 따라 실제 가공 시설과 진열 다음 단계를 안내한다. Kitchen/Forge/Basic recipe card는 각각 3/2/2개가 보이고, 재료 부족 상태에서도 보유량/필요량을 확인할 수 있다.
- 부족 상태는 재료와 결과를 변경하지 않는다. 충분한 Carrot/Wheat/Fish/Ore/Wood는 기존 `CraftingService`를 통해 구운 감자·BreadLoaf·생선구이·IronBar·Plank로 가공되며 품질과 가격 메타가 보존된다.
- 원재료 기본가 합계 118G가 가공품 203G로 증가했고, 품질이 보존된 가공품 1개가 B01에서 실제 28G에 판매됐다.
- BETA-003, CraftingRecipeCard, ProcessingChain, BETA-002 D3D11 검증이 PASS했다. blocking Console 0, 신규 crash 0, Runtime/Editor compile 오류 0이다.
- Scene/Prefab/Packages/ProjectSettings/Save schema는 무변경이다. 캡처는 `CAPTURE_EVIDENCE_DEBT`; 다음 활성 티켓은 `BETA-004 Shop Readability and Merchandising`이다.

## 2026-08-12 — BETA-004 Shop Readability and Merchandising 완료

- WorldSandbox의 실제 B01 4칸 판매대를 `밤 영업 구역 · 상품 판매대`로 명확히 식별하고, 각 슬롯에 빈 칸/상품명/수량/가격/품질/품절 후속 행동을 runtime-only로 표시한다.
- 플레이 HUD가 실제 슬롯 재고와 가격을 요약하며, 가공품 진열 목표는 이동 가능한 실제 판매대 위치를 가리킨다.
- Bread 1개, 품질 125%, 43G 상태와 가격 갱신을 확인했다. 판매대 `(1,1)` 이동과 270° 회전 뒤에도 표지·ShopSlot·재고·가격·고객 접근이 유지됐다.
- 범위 밖 이동은 transform을 바꾸지 않고 거부됐고, 기존 v11 `shopFurniture` 투영에 stable ID와 pose가 보존됐다. 이동된 판매대에서 고객 구매와 수익 입금, `오늘 품절 · 다음 상품 진열` 안내까지 완료됐다.
- BETA-004, BETA-003, WORLD-006B D3D11 검증 PASS. Runtime/Editor 오류 0, blocking Console 0, 신규 crash 0. Scene/Prefab/Packages/ProjectSettings/Save schema 무변경이다.
- 최종 상태 `BETA_004_COMPLETE`; 다음 활성 티켓은 `BETA-005 Customer Strategy and Feedback`이다.

## 2026-08-12 — BETA-005 Customer Strategy and Feedback (HARD BLOCKER)

- WorldSandbox 고객의 `NpcProfile` null과 거절 timeout 결손을 확인했다. 기존 Miner/Tailor profile 교대 배정, 실제 성향/결과 월드 라벨·HUD, 정상 거절 완료를 미커밋 구현했으며 기존 NPC/구매/경제/수요 권위는 보존했다.
- Runtime/Editor compile 오류 0. 두 D3D11 실행 모두 Miner 250G `p=0.00` 평가와 Day 2 보류 1건 기록까지 진행했지만 validator stage frame skip 때문에 같은 preference-panel 관찰 assertion이 반복 실패했다.
- 판정: `BETA_005_HARD_BLOCKER`. 세 번째 실행과 추가 수정, commit, BETA-006 시작은 중단했다. Scene/Prefab/Packages/ProjectSettings/Save schema/native crash 변경은 0이다.

## 2026-08-13 — BETA-005 승인 재검증 결과

- Runtime/Editor compile은 오류 0이다. 기존 CS8785/CS0414 경고만 유지됐다.
- 승인된 `Logs/BETA005_D3D11_Validation_ApprovedResume.log`는 D3D11, Miner/Tailor 성향 대비, Processed/Luxury 평가 차이, 실제 Miner 방문과 250G 보류·SalesLog 기록을 PASS했다.
- bootstrap frame skip 수정 뒤에도 첫 stage 1 Editor 관찰 전에 짧은 방문이 끝나 동일한 preference-panel assertion이 다시 실패했다. 새 crash와 금지 경로 변경은 0이다.
- 판정은 `HARD_BLOCKER_BETA_005_VALIDATION`. 승인된 추가 실행을 소비했으며 BETA-005 commit과 BETA-006 시작은 하지 않았다.

## 2026-08-20 — BETA-005 동기 live-HUD 증거 / Unity license blocker

- 기존 13개 BETA-005 변경을 보존하고, 실제 WorldSandbox 플레이 HUD를 `BeginNewGame()`으로 활성화한 뒤 Miner/Tailor 방문 성공 직후 같은 stage에서 현재 profile·visit 상태·고객 활성·라이브 성향 문구·HUD screen bounds를 검사하도록 최소 수정했다.
- 숨겨진 `CustomerPreferenceCanvas`는 개발 오버레이임을 유지한다. Canvas 활성·enabled·CanvasGroup alpha·text·RectTransform은 진단값으로 기록하되 플레이어 가시성 PASS 근거로 사용하지 않는다.
- Runtime/Editor compile 오류 0. 승인된 D3D11 launch는 Unity Editor license 부재로 project load 전 return code 198이어서 validator, Play Mode, graphics 초기화는 수행되지 않았다.
- 새 crash와 금지 경로 변경은 0이다. 현재 상태 `HARD_BLOCKER_BETA_005_UNITY_LICENSE`; local commit과 BETA-006은 시작하지 않았다.

## 2026-08-20 — BETA-005 Customer Strategy and Feedback 완료

- Unity Personal 라이선스 복구 뒤 실제 WorldSandbox player session에서 Miner/Tailor 성향과 방문 고객 활성, player HUD 문구·screen bounds를 방문 생성 직후 동기 검증했다. 숨겨진 개발 Canvas는 진단 전용으로 유지했다.
- Miner는 Plank 250G를 보류했고 재고·돈은 그대로이며 SalesLog 보류 1건이 남았다. Tailor는 저가 상품을 구매해 1G가 입금됐다. 실제 feedback, demand 통계, HUD가 이 차이를 설명한다.
- 주 BETA-005 D3D11 validator, BETA-004, CustomerPresentation, CustomerArrival 회귀가 모두 PASS했다. Runtime/Editor compile 오류 0, blocking Console 0, 신규 crash 0이다.
- Scene/Prefab/Packages/ProjectSettings/Save schema content는 변경하지 않았다. 캡처는 `CAPTURE_EVIDENCE_DEBT`이며 다음 활성 티켓은 선승인된 `BETA-006 Phone Hiring and Feed Completion`이다.

## 2026-08-21 — BETA-006 Phone Hiring and Feed Completion 완료

- WorldSandbox의 P 휴대폰 진입과 4개 앱을 runtime으로 연결했다. 기존 phone이 있는 Golden/Main은 재사용하고 새 hierarchy를 중복 생성하지 않는다.
- 8개 후보는 역할·실제 비용·잔액·잠금/부족/고용 상태를 보인다. C-02~C-09 원본을 참조하는 역할별 주민 wrapper로 실제 비용 차감, 스폰, roster 증가, 중복 차단을 완료했다.
- alpha 0 UI Mask 때문에 숨겨졌던 Hiring/Feed 카드를 복구했다. Feed는 첫 판매 전 명확한 빈 상태, 실제 판매 뒤 상품·가격·구매자·시각·category·quality·마을 변화 방향을 즉시 표시한다.
- D3D11 BETA-006과 BETA-005/VillageChangeSignal/Golden FinalDemoRoute 회귀 PASS. Runtime/Editor compile 오류 0, blocking Console 0, 신규 crash 0이다.
- 기존 Scene, Packages, ProjectSettings, Save schema/authority, C-02~C-09 FBX는 무변경이다. 최종 상태 `BETA_006_COMPLETE`; 다음은 선승인 `BETA-007 Village Response and NPC Integration`이다.

## 2026-08-21 — BETA-007 주민·마을 반응 VALIDATION DEBT CHECKPOINT

- 판매 상품/판매일을 다음날 마을 비주얼·HUD·관련 주민 대화에 연결하고 8개 채용 역할을 생성 활동 지점과 B05~B08에 결속했다.
- Runtime/Editor compile은 오류 0이다. 두 D3D11 실행 모두 crash 없이 WorldSandbox를 구성했지만 resident spawn anchor가 NavMesh ready 상태가 되지 않아 실제 판매 단계 전에 중단됐다.
- 추가 실행 없이 obstacle/NavMesh 안정화 뒤 유효 anchor만 채용 서비스에 전달하도록 정적으로 보정하고, 판매 상품의 실제 생산 역할과 월드 시설도 일치시켰다. Scene/Prefab/Packages/ProjectSettings/Save schema 변경은 0이다.
- 세 번째 실행은 승인 범위를 초과하므로 하지 않는다. PASS가 아닌 `BETA_007_IMPLEMENTED_WITH_VALIDATION_DEBT` local checkpoint로 보존하며 실제 hire→판매→Day 2→DialogueUI는 미검증이다.

## 2026-08-21 — BETA-008 Day 1–7 진행 IMPLEMENTED WITH VALIDATION DEBT

- WorldSandbox New Game이 동결 bootstrap clock을 실제 속도로 시작하고, 기존 상점 간판은 generated B01의 플레이어 접근 위치로 재결속된다. 고용 전 첫 고객은 승인된 주민 wrapper를 외형/profile 원천으로만 재사용하며 실제 NPC·가격 평가·ShopSlot 거래 권위는 그대로 통과한다.
- Day 2~7 납품은 자동 행동이 아니라 `B` 명시 구매이며 실제 돈 차감, 가방 preflight, 실패 재시도와 rollback을 사용한다. HUD와 Day 7 gate는 누적 매출 1,700G, 유료 고용, earned village response를 실제 서비스에서 읽는다.
- 첫 D3D11은 관광객 FSM이 creation frame의 `NpcController.Start`에 덮이는 lifecycle 결함을 찾았다. 두 번째 D3D11은 이를 보정한 뒤 일반 Day 1 관광객 구매, Day 1~4 판매·정산·실제 날짜 증가, Day 2~5 납품, Day 5 Farmer 유료 고용까지 PASS했다.
- 두 번째 실행은 validator가 같은 GrilledFish를 네 슬롯에 반복 진열해 Day 5 누적 매출이 989/1,050G에 머문 fixture 결함에서 종료됐다. 서로 다른 네 최고가 상품을 고르도록 보정했고 Runtime/Editor compile 오류 0을 다시 확인했다.
- 승인된 두 실행을 모두 사용해 추가 D3D11은 하지 않았다. Day 6~7과 Week 1 completion은 미검증이며 상태는 PASS/COMPLETE가 아닌 `BETA_008_IMPLEMENTED_WITH_VALIDATION_DEBT`다. Scene/Prefab/Packages/ProjectSettings/Save schema/native crash 변경은 0이다.

## 2026-08-21 — BETA-009 Persistence and Recovery IMPLEMENTED WITH VALIDATION DEBT

- gameplay save envelope를 v12로 additive 확장했다. procedural world payload v11과 v10 이하 `LegacyFixed` 의미는 유지한다.
- world/player/inventory/hotbar/economy/day/time/shop/furniture/progression/hiring/Feed/village와 M85 신규 상태의 capture·restore 누락을 보완했다. 농작물, B09 내용물, 판매 이력과 exact village response, load 중 transient 정리와 반복 load 중복 방지도 포함한다.
- Runtime/Editor compile 오류 0, 정적 검사 PASS, Scene/Prefab/Packages/ProjectSettings 변경과 native crash 0이다.
- 승인된 두 D3D11은 validator-local compile 오류와 invalid B01 fixture 좌표에서 save 전에 중단됐다. 좌표 교정본은 compile PASS지만 restart/repeated-load는 미검증이다.
- 상태는 `BETA_009_IMPLEMENTED_WITH_VALIDATION_DEBT`; 다음 활성 티켓은 `BETA-010 Full Playable Beta Integration`이다.

## 2026-08-21 — BETA-010 Full Integration BLOCKED

- 실제 v12 save→Editor Play restart→load에서 world checksum, B09 storage, player cell은 복원됐다.
- 저장 JSON의 137° player quaternion은 존재하지만 runtime PlayerRoot는 load 다음 frame에 identity로 돌아간다. 최종 pose 재적용 뒤 교정 실행도 `facingError=137`로 반복 실패했다.
- 두 승인 실행을 모두 사용했으며 같은 원인 세 번째 시도는 금지다. continue/repeated-load, Day 6~7, resident response와 Golden/M70 통합 회귀는 미검증이다.
- 최종 상태 `HARD_BLOCKER_BETA_010_PLAYER_FACING_RESTORE`; `M85_GAMEPLAY_BETA_COMPLETE`가 아니다.
