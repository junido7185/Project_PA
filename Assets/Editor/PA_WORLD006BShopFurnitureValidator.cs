#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// WORLD-006B adopts the existing shop.interior placement authority instead of
// introducing a second world-furniture system. This validator intentionally
// avoids capture work and exercises the real ShopSlot, grid and NPC paths.
[InitializeOnLoad]
public static class PA_WORLD006BShopFurnitureValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.WORLD006B.Active";
    const string FailedKey = "PA.WORLD006B.Failed";
    const string ConsoleErrorKey = "PA.WORLD006B.ConsoleErrors";
    const string WaitFramesKey = "PA.WORLD006B.WaitFrames";

    static PA_WORLD006BShopFurnitureValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        SubscribeCallbacks();
    }

    [MenuItem("Project PA/World/WORLD-006B/Validate Movable Shop Furniture")]
    public static void RunWorld006BValidation()
    {
        RunWorld006BValidationInternal();
    }

    public static void RunWorld006BValidationBatch()
    {
        RunWorld006BValidationInternal();
    }

    static void RunWorld006BValidationInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SessionState.SetInt(WaitFramesKey, 0);
            SubscribeCallbacks();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "validator opens the Golden Regression scene read-only");
            Require(!scene.isDirty, "Prototype_FirstDay starts clean");
            Debug.Log("[WORLD-006B] EDIT_MODE_PASS existing shop placement authority selected");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void SubscribeCallbacks()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Application.logMessageReceived += OnLogMessage;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetInt(WaitFramesKey, 0);
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            FinishValidation();
        }
    }

    static void ValidateRuntime()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
        int frames = SessionState.GetInt(WaitFramesKey, 0) + 1;
        SessionState.SetInt(WaitFramesKey, frames);

        ShopCustomizationController controller = ShopCustomizationController.Instance
            ?? Object.FindFirstObjectByType<ShopCustomizationController>();
        if ((controller == null || !controller.IsReady) && frames < 360) return;

        EditorApplication.update -= ValidateRuntime;
        try
        {
            Require(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11,
                $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
            Require(controller != null && controller.IsReady,
                "ShopCustomizationController initializes in Play Mode");
            Require(GridService.Instance != null && GridService.Instance.HasZone(controller.ZoneId),
                "existing GridService owns shop.interior");
            Require(controller.ZoneId == ShopCustomizationController.ShopInteriorZoneId &&
                    Mathf.Abs(controller.GridCellSize - 2f) < 0.01f,
                "shop furniture keeps the established 2m interior grid authority");
            Require(GridService.Instance.IsZoneCellProtected(controller.ZoneId, new Vector2Int(2, 0)),
                "shop entrance cell is protected");
            Require(GridService.Instance.HasZonePath(controller.ZoneId),
                "authored layout retains its entry-to-service protected route");

            string placementId = controller.FindPlacementIdAt(new Vector2Int(1, 1));
            if (string.IsNullOrWhiteSpace(placementId))
                placementId = controller.GetMovableShopSlotIds().FirstOrDefault();
            Require(!string.IsNullOrWhiteSpace(placementId),
                "at least one authored sales display is movable");

            GameObject displayObject = controller.GetPlacementGameObject(placementId);
            ShopSlot display = displayObject != null ? displayObject.GetComponent<ShopSlot>() : null;
            Require(display != null && displayObject.activeInHierarchy,
                "movable furniture is the real active ShopSlot object");
            Transform originalParent = displayObject.transform.parent;
            Require(controller.TryGetPlacementSnapshot(placementId, out Vector2Int originalCell,
                    out int originalRotation, out bool originalRecovered) && !originalRecovered,
                "sales display has a stable placement identity and baseline transform");
            Vector3 originalPosition = displayObject.transform.position;
            Quaternion originalWorldRotation = displayObject.transform.rotation;

            Item bread = Resources.Load<Item>("Items/Item_BreadLoaf");
            Require(bread != null, "real sellable Bread item data loads");
            display.currentItem = new ItemInstance(bread, 2)
            {
                quality = 0.91f,
                currentPrice = bread.basePrice
            };
            display.displayPrice = 73;
            display.RefreshDisplay();

            bool invalidMove = controller.TryMovePlacementForValidation(
                placementId, new Vector2Int(2, 0), 1, out string protectedReason);
            Require(!invalidMove && !string.IsNullOrWhiteSpace(protectedReason),
                "protected entrance rejects a furniture move");
            Require(controller.TryGetPlacementSnapshot(placementId, out Vector2Int afterRejectCell,
                    out int afterRejectRotation, out bool afterRejectRecovered) &&
                    afterRejectCell == originalCell && afterRejectRotation == originalRotation &&
                    !afterRejectRecovered &&
                    Vector3.Distance(displayObject.transform.position, originalPosition) < 0.001f &&
                    Quaternion.Angle(displayObject.transform.rotation, originalWorldRotation) < 0.01f,
                "failed move is atomic and leaves the original transform and occupancy intact");

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Require(player != null, "player exists for the real customization UI route");
            CharacterController character = player.GetComponent<CharacterController>();
            bool characterWasEnabled = character != null && character.enabled;
            if (characterWasEnabled) character.enabled = false;
            player.transform.position = displayObject.transform.position;
            if (characterWasEnabled) character.enabled = true;
            controller.ToggleFromTerminal(player);
            Button moveButton = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .FirstOrDefault(button => button != null && button.name == "Move");
            Require(moveButton != null && moveButton.interactable,
                "player-facing customization panel exposes the move action");
            moveButton.onClick.Invoke();
            displayObject.transform.position += new Vector3(1.25f, 0f, 0.75f);
            displayObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            controller.CloseCustomization();
            Require(controller.TryGetPlacementSnapshot(placementId, out Vector2Int cancelledCell,
                    out int cancelledRotation, out bool cancelledRecovered) &&
                    cancelledCell == originalCell && cancelledRotation == originalRotation &&
                    !cancelledRecovered &&
                    Vector3.Distance(displayObject.transform.position, originalPosition) < 0.001f &&
                    Quaternion.Angle(displayObject.transform.rotation, originalWorldRotation) < 0.01f,
                "closing an active move cancels back to the original cell and rotation");

            Require(controller.TryMovePlacementForValidation(
                    placementId, new Vector2Int(4, 0), 3, out string moveReason),
                $"sales display moves inside the shop and rotates 270 degrees ({moveReason})");
            Require(controller.TryGetPlacementSnapshot(placementId, out Vector2Int movedCell,
                    out int movedRotation, out bool movedRecovered) &&
                    movedCell == new Vector2Int(4, 0) && movedRotation == 3 && !movedRecovered,
                "committed furniture state stores the new cell and quarter-turn rotation");
            Require(displayObject.transform.parent == originalParent &&
                    displayObject.GetComponent<ShopSlot>() == display &&
                    display.currentItem != null && display.currentItem.data == bread &&
                    display.currentItem.count == 2 && display.displayPrice == 73,
                "move preserves hierarchy, ShopSlot component, stock and display price");
            Require(GridService.Instance.HasZonePath(controller.ZoneId),
                "committed move preserves the protected shop route");

            var approachWorld = new List<Vector3>();
            var approachCells = new List<Vector2Int>();
            Require(controller.TryGetNpcApproachPoints(display, approachWorld, approachCells,
                    out string approachOwner) && approachOwner == placementId &&
                    approachWorld.Count > 0 && approachWorld.Count == approachCells.Count &&
                    approachCells.All(cell => GridService.Instance.IsZoneCellInBounds(controller.ZoneId, cell)),
                "rotated display exposes an in-bounds authored customer approach cell");

            ShopCustomerApproachController approach =
                Object.FindFirstObjectByType<ShopCustomerApproachController>();
            NpcController customer = Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None)
                .FirstOrDefault(candidate =>
                {
                    NavMeshAgent agent = candidate != null ? candidate.GetComponent<NavMeshAgent>() : null;
                    return agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
                });
            Require(approach != null && customer != null,
                "existing customer approach service and an active NavMesh customer exist");
            NavMeshAgent customerAgent = customer.GetComponent<NavMeshAgent>();
            Transform insideSpawn = GameObject.Find("PA_StoreInterior")?.transform.Find("PlayerSpawn_Inside");
            Require(insideSpawn != null && NavMesh.SamplePosition(insideSpawn.position, out NavMeshHit insideHit,
                    2.5f, NavMesh.AllAreas) && customerAgent.Warp(insideHit.position),
                "customer starts on the authored interior NavMesh island");
            Require(approach.TryResolveReachablePoint(customerAgent, display,
                    out _, out _, out NavMeshPathStatus pathStatus) &&
                    pathStatus == NavMeshPathStatus.PathComplete,
                "customer has a complete NavMesh path to the moved display");

            int moneyBefore = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;
            const int expectedStackSale = 73 * 2;
            Require(display.TryPurchaseByNpc("WORLD006B", out int paid) &&
                    paid == expectedStackSale && display.IsEmpty,
                "existing ShopSlot sells the complete displayed stack after movement");
            if (EconomyService.Instance != null)
                Require(EconomyService.Instance.Money == moneyBefore + expectedStackSale,
                    "moved display sale still deposits through EconomyService");

            var saveProjection = new SaveData();
            controller.WriteSaveFields(saveProjection);
            PlaceableSaveData placementRecord = saveProjection.placeables.FirstOrDefault(record =>
                record != null && record.instanceId == placementId);
            Require(placementRecord != null &&
                    placementRecord.zoneId == ShopCustomizationController.ShopInteriorZoneId &&
                    placementRecord.gridX == 4 && placementRecord.gridY == 0 &&
                    placementRecord.rotationQuarterTurns == 3 && placementRecord.isFixed &&
                    !placementRecord.recovered,
                "existing v10 projection prepares stable furniture transform data for WORLD-007");
            Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                "blocking runtime Console Error/Exception/Assert count is 0");

            Debug.Log("[WORLD-006B] PLAY_MODE_PASS furniture=1 move=true rotate=270 " +
                      "shopSlot=true customerPath=true protectedRoute=true cancelRollback=true saveReady=v10");
            EditorApplication.ExitPlaymode();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        SessionState.SetInt(ConsoleErrorKey, SessionState.GetInt(ConsoleErrorKey, 0) + 1);
    }

    static void Fail(Exception ex)
    {
        SessionState.SetBool(FailedKey, true);
        Debug.LogError($"[WORLD-006B] FAIL {ex.Message}\n{ex}");
        EditorApplication.update -= ValidateRuntime;
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else FinishValidation();
    }

    static void FinishValidation()
    {
        bool failed = SessionState.GetBool(FailedKey, false) ||
                      SessionState.GetInt(ConsoleErrorKey, 0) != 0;
        int consoleErrors = SessionState.GetInt(ConsoleErrorKey, 0);
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(FailedKey);
        SessionState.EraseInt(ConsoleErrorKey);
        SessionState.EraseInt(WaitFramesKey);
        EditorApplication.update -= ValidateRuntime;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Debug.Log(failed
            ? $"[WORLD-006B] FINISHED_WITH_ERRORS consoleErrors={consoleErrors}"
            : "[WORLD-006B] FINISHED_PASS furniture=1 move=true rotate=true shopSlot=true " +
              "customerAccess=true protectedRoute=true cancelRollback=true saveProjection=true");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[WORLD-006B] PASS {message}");
    }
}
#endif
