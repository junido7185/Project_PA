using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
    public GameObject axeModel;     
    public GameObject pickaxeModel;
    public bool FirstDayPresentation { get; private set; }
    public bool IsHolstered { get; private set; } = true;
    public Item HeldItem => FirstDayPresentation && IsHolstered ? null : Inventory.instance?.GetSelectedItem();
    public GameObject HeldVisual { get; private set; }
    public float ActionStartedAt { get; private set; } = -100;
    public float FailedContactAt { get; private set; } = -100;
    public string LastUseFeedback { get; private set; } = string.Empty;
    Transform _hand;
    Item _shown;
    Quaternion _authoredRotation;
    Inventory _inventory;
    public void ConfigureFirstDay()
    {
        if (FirstDayPresentation) return;
        FirstDayPresentation = true;
        _inventory = GetComponent<Inventory>();
        if (_inventory != null) _inventory.SelectionChanged += EquipSelection;
        if (PlayerInputHandler.Instance != null) PlayerInputHandler.Instance.OnHolster += Holster;
        var animator = GetComponentInChildren<Animator>();
        _hand = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
        if (_hand == null) { Debug.LogError("[FIRST-DAY] Character has no right-hand socket."); return; }
        if (axeModel != null) axeModel.SetActive(false);
        if (pickaxeModel != null) pickaxeModel.SetActive(false);
    }
    public void Holster() { IsHolstered = true; RefreshHeldVisual(); }
    public void ResetHeldPresentation()
    {
        IsHolstered = true;
        ActionStartedAt = -100;
        FailedContactAt = -100;
        LastUseFeedback = string.Empty;
        _shown = null;
        if (HeldVisual != null) { HeldVisual.SetActive(false); Destroy(HeldVisual); }
        HeldVisual = null;
        if (axeModel != null) axeModel.SetActive(false);
        if (pickaxeModel != null) pickaxeModel.SetActive(false);
    }
    void EquipSelection(int index) { IsHolstered = false; RefreshHeldVisual(); }
    public void PlayAction() { ActionStartedAt = Time.time; FailedContactAt = -100; LastUseFeedback = string.Empty; }
    // Shared presentation timing. Resource/bug/fish components still decide success and rewards.
    public bool TryBeginToolUse()
    {
        if (Time.time - ActionStartedAt < .62f) return false;
        PlayAction();
        return true;
    }
    public void FailedUse(string message, bool contacted)
    {
        LastUseFeedback = message;
        if (contacted) FailedContactAt = Time.time;
    }
    public static Item CurrentHeld(GameObject player)
    {
        var equipment = player != null ? player.GetComponent<EquipmentSystem>() : null;
        return equipment != null ? equipment.HeldItem : Inventory.instance?.GetSelectedItem();
    }
    void RefreshHeldVisual()
    {
        Item item = HeldItem;
        if (_hand == null || _shown == item && (item == null || HeldVisual != null)) return;
        if (HeldVisual != null) { HeldVisual.SetActive(false); Destroy(HeldVisual); }
        _shown = item;
        if (item == null) return;
        var assets = FirstDayStudioAssets.Load();
        GameObject model = assets != null ? assets.ModelFor(item) : item.model;
        if (model == null) { Debug.LogWarning("[FIRST-DAY] Missing held model: " + item.itemName); return; }
        _authoredRotation = model.transform.localRotation;
        HeldVisual = Instantiate(model, Vector3.zero, _authoredRotation);
        HeldVisual.name = "Held_" + item.id;
        foreach (var col in HeldVisual.GetComponentsInChildren<Collider>()) col.enabled = false;
        foreach (var body in HeldVisual.GetComponentsInChildren<Rigidbody>()) body.isKinematic = true;
        if (item.toolType != ToolType.None) FirstDayStudioAssets.ScaleVisual(HeldVisual, .85f);
        else
        {
            var renderers = HeldVisual.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                HeldVisual.transform.localScale *= .38f / Mathf.Max(.001f, Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)));
            }
        }
        HeldVisual.transform.SetParent(_hand, true);
        HeldVisual.transform.position = _hand.position + _hand.rotation * new Vector3(0, .035f, .02f);
        HeldVisual.transform.localRotation = Quaternion.Euler(0, 0, 90) * _authoredRotation;
    }

    void LateUpdate()
    {
        if (!FirstDayPresentation || HeldVisual == null || _shown == null || _shown.toolType != ToolType.None) return;
        // 작은 물건은 뼈의 회전으로 몸 안에 파묻히지 않게 몸 바깥에서 운반한다.
        HeldVisual.transform.rotation = transform.rotation * _authoredRotation;
        var renderers = HeldVisual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        HeldVisual.transform.position += transform.TransformPoint(new Vector3(.42f, 1f, .35f)) - bounds.center;
    }

    void Start()
    {
        // ⭐ 인벤토리 이벤트 이름이 바뀌었을 수 있으니 다시 연결
        if (Inventory.instance != null)
            Inventory.instance.onItemChangedCallback += RefreshEquipment;
    }

    void OnDestroy()
    {
        if (_inventory != null) _inventory.SelectionChanged -= EquipSelection;
        if (PlayerInputHandler.Instance != null) PlayerInputHandler.Instance.OnHolster -= Holster;
        if (Inventory.instance != null)
            Inventory.instance.onItemChangedCallback -= RefreshEquipment;
    }

    // Inventory.cs에서 RefreshAllUI()가 호출될 때 같이 실행됨
    void RefreshEquipment()
    {
        if (FirstDayPresentation) { RefreshHeldVisual(); return; }
        // 1. 모델 끄기
        if (axeModel != null) axeModel.SetActive(false);
        if (pickaxeModel != null) pickaxeModel.SetActive(false);

        BuildManager buildMgr = BuildManager.instance;

        // 2. 현재 든 아이템 확인
        if (Inventory.instance == null) return;
        Item item = Inventory.instance.GetSelectedItem();
        if (Inventory.instance.GetComponent<WorldHotbarPlacementController>() != null &&
            WorldPlaceableKitCatalog.TryKit(item, out _, out _))
        {
            if (buildMgr != null && !buildMgr.IsRelocating) buildMgr.StopBuildMode();
            return;
        }
        if (item == null)
        {
            if (buildMgr != null && !buildMgr.IsRelocating) buildMgr.StopBuildMode();
            return;
        }

        // --- 도구 모델 켜기 ---
        if (item.toolType == ToolType.Axe && axeModel != null) axeModel.SetActive(true);
        if (item.toolType == ToolType.Pickaxe && pickaxeModel != null) pickaxeModel.SetActive(true);

        // --- 건설 아이템이면 건설 모드 켜기 ---
        if (item.buildingToBuild != null && buildMgr != null)
        {
            if (buildMgr.currentBuilding != item.buildingToBuild)
                buildMgr.SetBuildMode(item.buildingToBuild);
        }
        else if (buildMgr != null && !buildMgr.IsRelocating)
        {
            buildMgr.StopBuildMode();
        }
    }
    
    // 핫바 UI에서 휠 돌릴 때마다 호출해주면 반응 속도가 더 빠름
    void Update()
    {
        // (최적화를 위해 매 프레임 체크하기보다 이벤트 방식 권장하지만, 
        // 핫바 변경 타이밍을 확실히 잡기 위해 Update에서 체크해도 됨)
        RefreshEquipment();
    }
}
