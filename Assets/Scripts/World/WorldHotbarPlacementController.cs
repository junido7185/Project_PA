using UnityEngine;

// Input/presentation adapter only. WorldBuildingPlacementService owns every placement.
[DisallowMultipleComponent]
public sealed class WorldHotbarPlacementController : MonoBehaviour
{
    WorldBuildingPlacementService placement;
    WorldGridService grid;
    Inventory inventory;
    PlayerInputHandler input;
    InventorySlot selectedSlot;
    ItemInstance selectedStack;
    WorldBuildingPlacementDefinition definition;
    string instanceId;
    int turns;
    bool committing;
    bool moving;
    Item lastOffered;
    DemoPlaceableEntry demoEntry;
    public event System.Action<WorldPlacedBuildingRuntime, DemoPlaceableEntry> Placed;

    Vector2Int lastAnchor;
    int lastTurns = -1, lastRevision = -1;
    public bool IsPlacing { get; private set; }
    public string Status { get; private set; } = "";
    public WorldBuildingPlacementResult PreviewResult => placement != null ? placement.LastResult : default;

    public void Configure(WorldBuildingPlacementService service, Inventory bag)
    {
        placement = service; grid = service.GetComponent<WorldGridService>(); inventory = bag;
    }

    void Start()
    {
        input = PlayerInputHandler.Instance;
        if (input == null) return;
        input.OnBuildRotate += Rotate;
        input.OnBuildPlace += Confirm;
        input.OnInventoryToggle += Cancel;
        input.OnPhoneToggle += Cancel;
        input.OnSave += Cancel;
        input.OnLoad += Cancel;
    }

    void OnDisable() { Cancel(); }
    void OnDestroy()
    {
        if (input == null) return;
        input.OnBuildRotate -= Rotate; input.OnBuildPlace -= Confirm;
        input.OnInventoryToggle -= Cancel; input.OnPhoneToggle -= Cancel;
        input.OnSave -= Cancel; input.OnLoad -= Cancel;
    }

    bool SelectionIntact() => moving || inventory != null && inventory.hotbar != null &&
        ReferenceEquals(inventory.hotbar.GetSlot(inventory.selectedHotbarIndex), selectedSlot) &&
        ReferenceEquals(inventory.GetSelectedInstance(), selectedStack) && selectedSlot.count > 0 &&
        (!(PlayerInputHandler.Instance?.FirstDayControls ?? false) || EquipmentSystem.CurrentHeld(gameObject) == selectedStack.data);

    public bool TryUseSelected()
    {
        if (!isActiveAndEnabled || placement == null || inventory == null) return false;
        if (IsPlacing) { Confirm(); return true; }
        demoEntry = null;
        bool demo = PlayerInputHandler.Instance?.FirstDayControls == true;
        if (demo)
        {
            var held = EquipmentSystem.CurrentHeld(gameObject);
            demoEntry = DemoPlaceableCatalog.Load()?.Find(held);
            if (demoEntry == null) return false;
            if (DemoSettlementController.Instance != null && !DemoSettlementController.Instance.CanPlace(demoEntry)) return true;
            instanceId = "demo-" + demoEntry.key + "-" + System.Guid.NewGuid().ToString("N");
            definition = demoEntry.Definition;
        }
        else if (!WorldPlaceableKitCatalog.TryKit(inventory.GetSelectedItem(), out instanceId, out definition)) return false;
        if (placement.HasPreview) { Status = "Finish the current placement first."; return true; }
        if (!placement.TryGetPlacement(instanceId, out _)) placement.RegisterDefinition(instanceId, definition);
        selectedSlot = inventory.hotbar.GetSlot(inventory.selectedHotbarIndex);
        selectedStack = selectedSlot.instance;
        moving = false;
        turns = 0;
        lastTurns = -1;
        placement.BeginPlacementPreview(instanceId, ResolveAnchor(), turns);
        IsPlacing = placement.HasPreview;
        RefreshPreview();
        return true;
    }

    public bool TryMoveExisting(DemoPlacedObject target)
    {
        if (IsPlacing || target == null || !target.CanMove || placement == null || placement.HasPreview ||
            !placement.TryGetPlacement(target.InstanceId, out var placed)) return false;
        instanceId = placed.InstanceId; definition = placed.Definition; demoEntry = target.Entry;
        turns = placed.QuarterTurns; lastTurns = -1;
        var result = placement.BeginMovePreview(instanceId, placed.Anchor, turns, true);
        IsPlacing = placement.HasPreview;
        moving = IsPlacing;
        Status = result.Failure.ToString();
        return IsPlacing;
    }

    void Update()
    {
        if (PlayerInputHandler.Instance?.FirstDayControls == true && !PlayerInputHandler.ModalOpen && !IsPlacing)
        {
            var held = EquipmentSystem.CurrentHeld(gameObject);
            if (held != lastOffered) { lastOffered = held; if (held != null) TryUseSelected(); }
        }
        if (!IsPlacing) return;
        if (PlayerInputHandler.ModalOpen) { Cancel(); return; }
        if (!SelectionIntact() || !placement.HasPreview || placement.LastResult.InstanceId != instanceId)
        { Cancel(); return; }
        RefreshPreview();
    }

    Vector2Int ResolveAnchor()
    {
        // Center the real footprint in front of the player. Movement naturally moves the preview.
        var offsets = definition.ResolveFootprint(Vector2Int.zero, turns);
        Vector2 mean = Vector2.zero;
        float radius = 0;
        foreach (var cell in offsets) mean += (Vector2)cell;
        mean /= offsets.Length;
        foreach (var cell in offsets) radius = Mathf.Max(radius, Vector2.Distance(cell, mean));
        Vector3 target = transform.position + transform.forward * ((radius + 1.25f) * grid.Definition.CellSize);
        target -= new Vector3(mean.x, 0, mean.y) * grid.Definition.CellSize;
        grid.WorldToCell(target, out var anchor);
        return anchor;
    }

    public void RefreshPreview()
    {
        if (!IsPlacing || !SelectionIntact() || !placement.HasPreview || placement.LastResult.InstanceId != instanceId) return;
        var anchor = ResolveAnchor();
        if (anchor == lastAnchor && turns == lastTurns && placement.Revision == lastRevision) return;
        lastAnchor = anchor; lastTurns = turns; lastRevision = placement.Revision;
        var result = placement.UpdatePreview(anchor, turns);
        Status = result.Succeeded ? "Ready" : result.Failure.ToString();
    }

    public void Rotate()
    {
        if (!IsPlacing || committing) return;
        turns = WorldBuildingPlacementDefinition.NormalizeQuarterTurns(turns + 1);
        RefreshPreview();
    }

    public void Confirm()
    {
        if (!IsPlacing || committing) return;
        if (!SelectionIntact() || !placement.HasPreview || placement.LastResult.InstanceId != instanceId) { Cancel(); return; }
        RefreshPreview();
        committing = true;
        try
        {
            // Synchronous preflight/commit/consume: no yield and no UI callback before deduction.
            var result = placement.CommitPreview();
            Status = result.Failure.ToString();
            if (!result.Succeeded) return;
            if (!moving) selectedSlot.AddCount(-1);
            if (demoEntry != null && placement.TryGetPlacement(instanceId, out var placed))
            {
                var interaction = placed.GameObject.GetComponent<DemoPlacedObject>() ?? placed.GameObject.AddComponent<DemoPlacedObject>();
                interaction.Bind(instanceId, demoEntry, placement);
                Placed?.Invoke(placed, demoEntry);
            }
            moving = false;
            IsPlacing = false;
            selectedSlot = null; selectedStack = null;
            Status = "Placed";
            inventory.RefreshAllUI();
        }
        finally { committing = false; }
    }

    public void Cancel()
    {
        if (!IsPlacing || committing) return;
        if (placement != null && placement.HasPreview && placement.LastResult.InstanceId == instanceId)
            if (!placement.CancelPreview()) { Status = "취소 복원 대기"; return; }
        IsPlacing = false; moving = false; selectedSlot = null; selectedStack = null;
        Status = "Cancelled";
    }

    void OnGUI()
    {
        if (PlayerInputHandler.Instance?.FirstDayControls == true) return;
        if (inventory == null || !WorldPlaceableKitCatalog.TryKit(inventory.GetSelectedItem(), out _, out _)) return;
        GUI.Box(new Rect((Screen.width - 640) * .5f, Screen.height - 154, 640, 52),
            IsPlacing ? "WASD: move preview   R: rotate   Space / Click: place   Esc: cancel\n" + Status :
            inventory.GetSelectedItem().itemName + "   Space: place near player");
    }
}
