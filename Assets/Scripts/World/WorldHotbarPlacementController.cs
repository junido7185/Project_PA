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
    int offeredIndex = -1;
    bool offeredHolstered = true;
    DemoPlaceableEntry demoEntry;
    public event System.Action<WorldPlacedBuildingRuntime, DemoPlaceableEntry> Placed;

    Vector2Int lastAnchor;
    int lastTurns = -1, lastRevision = -1;
    public bool IsPlacing { get; private set; }
    public string Status { get; private set; } = "";
    // P6 배치 문법: 마우스로 캐릭터 주변(반경 MouseRadiusCells칸) 위치 지정, E 확정, R 회전, Esc/RMB 취소.
    // 마우스를 움직이기 전에는 기존처럼 캐릭터 앞에 둔다(WASD 이동으로 따라가는 호환 보조).
    public const float MouseRadiusCells = 3f;
    public bool MouseAiming => mouseAim;
    Vector2 mouseAtBegin;
    bool mouseAim;
    float footprintRadius;
    LineRenderer rangeRing;
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
        BeginAim();
        placement.BeginPlacementPreview(instanceId, ResolveAnchor(), turns);
        IsPlacing = placement.HasPreview;
        SetBuildView(IsPlacing);
        RefreshPreview();
        return true;
    }

    public bool TryMoveExisting(DemoPlacedObject target)
    {
        if (IsPlacing || target == null || !target.CanMove || placement == null || placement.HasPreview ||
            !placement.TryGetPlacement(target.InstanceId, out var placed)) return false;
        instanceId = placed.InstanceId; definition = placed.Definition; demoEntry = target.Entry;
        turns = placed.QuarterTurns; lastTurns = -1;
        BeginAim();
        var result = placement.BeginMovePreview(instanceId, placed.Anchor, turns, true);
        IsPlacing = placement.HasPreview;
        moving = IsPlacing;
        SetBuildView(IsPlacing);
        Status = Describe(result);
        return IsPlacing;
    }

    void Update()
    {
        if (PlayerInputHandler.Instance?.FirstDayControls == true && !PlayerInputHandler.ModalOpen && !IsPlacing)
        {
            var held = EquipmentSystem.CurrentHeld(gameObject);
            var equipment = GetComponent<EquipmentSystem>();
            bool holstered = equipment != null && equipment.IsHolstered;
            int index = inventory != null ? inventory.selectedHotbarIndex : -1;
            // 플레이어가 직접 고른 경우(번호/휠로 칸을 바꾸거나 X 뒤 다시 꺼냄)에만 미리보기를 연다.
            // 방금 비운 선택 칸에 보상(예: 가판대 지급)이 들어온 것은 고른 것이 아니다.
            bool chosen = index != offeredIndex || offeredHolstered && !holstered;
            if (held != lastOffered) { lastOffered = held; if (held != null && chosen) TryUseSelected(); }
            offeredIndex = index; offeredHolstered = holstered;
        }
        if (!IsPlacing) return;
        if (PlayerInputHandler.ModalOpen) { Cancel(); return; }
        if (!SelectionIntact() || !placement.HasPreview || placement.LastResult.InstanceId != instanceId)
        { Cancel(); return; }
        RefreshPreview();
        UpdateRangeRing();
    }

    void BeginAim()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        mouseAtBegin = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
        mouseAim = false;
    }

    // 마우스가 움직였으면 커서가 가리키는 바닥(캐릭터 발 높이 평면)을 반경 안으로 잘라 목표로 쓴다.
    bool TryMouseTarget(out Vector3 world)
    {
        world = default;
        var mouse = UnityEngine.InputSystem.Mouse.current;
        var view = Camera.main;
        if (mouse == null || view == null || PlayerInputHandler.Instance?.FirstDayControls != true) return false;
        Vector2 screen = mouse.position.ReadValue();
        if (!mouseAim)
        {
            if ((screen - mouseAtBegin).sqrMagnitude < 64f) return false;
            mouseAim = true;
        }
        var ray = view.ScreenPointToRay(screen);
        if (!new Plane(Vector3.up, transform.position).Raycast(ray, out float enter)) return false;
        world = ray.GetPoint(enter);
        Vector3 flat = Vector3.ProjectOnPlane(world - transform.position, Vector3.up);
        // 큰 건물은 중심이 캐릭터에서 (반경 + 건물 반지름)칸 안에 오면 된다.
        float max = (MouseRadiusCells + footprintRadius) * grid.Definition.CellSize;
        if (flat.magnitude > max) world = transform.position + flat.normalized * max;
        return true;
    }

    // 큰 건물(여러 칸)은 기존 camera build mode로 한 발 물러나 전체가 보이게 한다.
    void SetBuildView(bool active)
    {
        var follow = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        bool large = definition != null && definition.ResolveFootprint(Vector2Int.zero, 0).Length > 1;
        if (follow != null) follow.SetOpeningBuildMode(active && large);
        if (!active && rangeRing != null) rangeRing.gameObject.SetActive(false);
    }

    // 마우스 지정 범위를 캐릭터 발밑 원으로 보인다.
    void UpdateRangeRing()
    {
        if (PlayerInputHandler.Instance?.FirstDayControls != true || grid == null) return;
        if (rangeRing == null)
        {
            var ring = new GameObject("PlacementRange");
            rangeRing = ring.AddComponent<LineRenderer>();
            rangeRing.loop = true; rangeRing.useWorldSpace = true; rangeRing.positionCount = 48;
            rangeRing.widthMultiplier = .07f;
            rangeRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rangeRing.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            rangeRing.startColor = rangeRing.endColor = new Color(1f, .97f, .86f, .55f);
        }
        rangeRing.gameObject.SetActive(true);
        float radius = (MouseRadiusCells + footprintRadius + .5f) * grid.Definition.CellSize;
        for (int i = 0; i < 48; i++)
        {
            float a = i / 48f * Mathf.PI * 2f;
            rangeRing.SetPosition(i, transform.position + new Vector3(Mathf.Cos(a) * radius, .08f, Mathf.Sin(a) * radius));
        }
    }

    public static string Describe(WorldBuildingPlacementResult result) => result.Succeeded ? "설치 가능" : Describe(result.Failure);

    static string Describe(WorldBuildingPlacementFailure failure)
    {
        switch (failure)
        {
            case WorldBuildingPlacementFailure.OutOfBounds: return "섬 밖이에요";
            case WorldBuildingPlacementFailure.ProtectedCell: return "보호 구역이에요";
            case WorldBuildingPlacementFailure.OccupiedCell: return "다른 건물·가구와 겹쳐요";
            case WorldBuildingPlacementFailure.WaterCell: return "물 위에는 둘 수 없어요";
            case WorldBuildingPlacementFailure.PathCell: return "길 위에는 둘 수 없어요";
            case WorldBuildingPlacementFailure.InvalidGround:
            case WorldBuildingPlacementFailure.InvalidZone: return "여기에는 둘 수 없어요";
            case WorldBuildingPlacementFailure.UnevenFootprint: return "바닥이 고르지 않아요";
            case WorldBuildingPlacementFailure.EntranceBlocked: return "입구 앞이 막혀요";
            case WorldBuildingPlacementFailure.CriticalRouteBlocked: return "손님 통로를 막아요";
            case WorldBuildingPlacementFailure.PhysicalObstacle: return "나무·바위·사람이 가로막아요";
            case WorldBuildingPlacementFailure.MoveNotAllowed: return "옮길 수 없어요";
            default: return "설치할 수 없어요";
        }
    }

    Vector2Int ResolveAnchor()
    {
        // Center the real footprint in front of the player (or under the cursor). Movement naturally moves the preview.
        var offsets = definition.ResolveFootprint(Vector2Int.zero, turns);
        Vector2 mean = Vector2.zero;
        float radius = 0;
        foreach (var cell in offsets) mean += (Vector2)cell;
        mean /= offsets.Length;
        foreach (var cell in offsets) radius = Mathf.Max(radius, Vector2.Distance(cell, mean));
        footprintRadius = radius;
        Vector3 target = TryMouseTarget(out var aimed) ? aimed
            : transform.position + transform.forward * ((radius + 1.25f) * grid.Definition.CellSize);
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
        Status = Describe(result);
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
        if (demoEntry != null && placement.LastResult.Succeeded &&
            !DemoSettlementController.IsCustomerReachable(grid, placement.LastResult, demoEntry.kind))
        {
            Status = "손님이 들어올 수 없어요";
            FirstDayWorldPresentation.Toast("손님이 걸어 들어올 수 없는 자리예요.\n입구 앞이 같은 높이의 평지인 곳에 세워 주세요.");
            return;
        }
        committing = true;
        try
        {
            // Synchronous preflight/commit/consume: no yield and no UI callback before deduction.
            var result = placement.CommitPreview();
            Status = Describe(result);
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
            SetBuildView(false);
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
        SetBuildView(false);
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
