using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// First village-culture visual response.
// This is a read-only observer over sales records: it never changes price,
// purchase probability, inventory, money, NPC behavior, or save data.
public class VillageCultureVisualController : MonoBehaviour
{
    public const string ControllerName = "PA_VillageCultureVisualController";
    public const string ProcessedVisualRootName = "PA_VillageCulture_Processed";
    public const string RawVisualRootName = "PA_VillageCulture_Raw";
    public const string UtilityVisualRootName = "PA_VillageCulture_Utility";
    public const string LuxuryVisualRootName = "PA_VillageCulture_Luxury";
    public const string HintPanelName = "VillageCultureHintPanel";

    const string ProcessedWorkbenchDataResource = "Buildings/Building_B05_Workbench";
    const string ProcessedPreparationKitResource = "VisualFinalization/B05_Workbench_PreparationKit";
    const string UtilityForgeDataResource = "Buildings/Building_B07_BlacksmithForge";
    const string LuxurySewingDataResource = "Buildings/Building_B08_SewingTable";
    const string RawWoodLogResource = "PA_DemoProps/Prop_WoodLog";
    const string RawRockResource = "PA_DemoProps/Prop_Rock";
    const string VillageSignResource = "VisualFinalization/B10_Cottage_ShopSign";

    public static VillageCultureVisualController Instance { get; private set; }

    [Header("Village Culture Visual")]
    public bool autoCreateVisual = true;
    public bool autoCreateHint = true;
    public ItemCategory trackedCategory = ItemCategory.Processed;
    public int maxRecentSales = 40;
    public float refreshInterval = 0.75f;
    public float hintSeconds = 5f;

    GameObject _visualRoot;
    GameObject _rawVisualRoot;
    GameObject _utilityVisualRoot;
    GameObject _luxuryVisualRoot;
    Canvas _hintCanvas;
    GameObject _hintPanel;
    TextMeshProUGUI _hintText;

    readonly HashSet<int> _seenSaleHashes = new HashSet<int>();
    float _nextRefreshAt;
    float _hideHintAt;
    bool _hasPendingChange;
    bool _hasActiveCategory;
    bool _hintShownForActiveChange;
    int _pendingSaleDay;
    int _hintDisplayCount;
    ItemCategory _pendingCategory;
    ItemCategory _activeCategory;

    public GameObject VisualRoot => !_hasActiveCategory
        ? _visualRoot
        : _activeCategory switch
        {
            ItemCategory.Raw => _rawVisualRoot,
            ItemCategory.Utility => _utilityVisualRoot,
            ItemCategory.Luxury => _luxuryVisualRoot,
            _ => _visualRoot
        };
    public GameObject ProcessedVisualRoot => _visualRoot;
    public GameObject RawVisualRoot => _rawVisualRoot;
    public GameObject UtilityVisualRoot => _utilityVisualRoot;
    public GameObject LuxuryVisualRoot => _luxuryVisualRoot;
    public GameObject HintPanel => _hintPanel;
    public bool VisualActive => IsVisualActive(_visualRoot)
        || IsVisualActive(_rawVisualRoot)
        || IsVisualActive(_utilityVisualRoot)
        || IsVisualActive(_luxuryVisualRoot);
    public bool HasPendingChange => _hasPendingChange;
    public int PendingSaleDay => _pendingSaleDay;
    public int HintDisplayCount => _hintDisplayCount;
    public bool HasActiveCategory => _hasActiveCategory;
    public ItemCategory ActiveCategory => _activeCategory;
    public string ActiveCategoryName => _hasActiveCategory ? _activeCategory.ToString() : "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindFirstObjectByType<VillageCultureVisualController>() != null)
            return;

        var host = new GameObject(ControllerName);
        host.AddComponent<VillageCultureVisualController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (autoCreateVisual)
            EnsureVisual();

        if (autoCreateHint)
            EnsureHint();

        SetVisualActive(false);
    }

    void Start()
    {
        RefreshNow();
    }

    void Update()
    {
        if (_hintPanel != null && _hintPanel.activeSelf && _hideHintAt > 0f && Time.unscaledTime >= _hideHintAt)
            _hintPanel.SetActive(false);

        if (Time.unscaledTime < _nextRefreshAt)
            return;

        _nextRefreshAt = Time.unscaledTime + Mathf.Max(0.1f, refreshInterval);
        RefreshNow();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RefreshNow()
    {
        EnsureVisual();
        EnsureHint();
        DetectNewTrackedSales();

        EvaluateForDayPreparation(CurrentDay(), IsDayPreparationPhase());
    }

    public void EvaluateForDayPreparation(int day, bool isDayPreparation)
    {
        if (!_hasPendingChange || !isDayPreparation || day <= _pendingSaleDay)
            return;

        ActivateChange(_pendingCategory);
    }

    // Task 057 — SaveManager 가 호출: 대기/활성 마을 변화 상태를 직렬화한다 (v9).
    public void WriteSaveFields(SaveData data)
    {
        if (data == null) return;

        data.villageCultureHasPendingChange = _hasPendingChange;
        data.villageCulturePendingSaleDay = _pendingSaleDay;
        data.villageCulturePendingCategory = _hasPendingChange ? _pendingCategory.ToString() : "";
        data.villageCultureHasActiveChange = _hasActiveCategory;
        data.villageCultureActiveCategory = _hasActiveCategory ? _activeCategory.ToString() : "";
        data.villageCultureHintShown = _hintShownForActiveChange;
    }

    // Task 057 — SaveManager 가 호출: 저장된 대기/활성 변화 상태를 복원한다.
    // 활성 변화는 즉시 시각을 켜고, 힌트는 저장된 표시 여부를 존중해 재표시하지 않는다.
    public void RestoreSavedState(bool hasPending, int pendingSaleDay, string pendingCategory,
        bool hasActive, string activeCategory, bool hintShown)
    {
        EnsureVisual();
        EnsureHint();

        _hasPendingChange = false;
        if (hasPending && Enum.TryParse(pendingCategory, true, out ItemCategory pending))
        {
            _hasPendingChange = true;
            _pendingCategory = pending;
            _pendingSaleDay = Mathf.Max(1, pendingSaleDay);
        }

        _hintShownForActiveChange = hintShown;

        if (hasActive && Enum.TryParse(activeCategory, true, out ItemCategory active))
        {
            _activeCategory = active;
            _hasActiveCategory = true;
            SetVisualActive(true);
        }
        else
        {
            _hasActiveCategory = false;
            SetVisualActive(false);
        }

        Debug.Log($"🏘️ [VillageCulture] 저장 상태 복원: pending={_hasPendingChange}({(_hasPendingChange ? _pendingCategory.ToString() : "-")}@day{_pendingSaleDay}), active={_hasActiveCategory}({(_hasActiveCategory ? _activeCategory.ToString() : "-")})");
    }

    public void ResetForValidation()
    {
        _seenSaleHashes.Clear();
        _hasPendingChange = false;
        _hasActiveCategory = false;
        _hintShownForActiveChange = false;
        _pendingSaleDay = 0;
        _hintDisplayCount = 0;
        SetVisualActive(false);

        if (_hintPanel != null)
            _hintPanel.SetActive(false);
    }

    void DetectNewTrackedSales()
    {
        if (SalesLogManager.Instance == null)
            return;

        var records = SalesLogManager.Instance.GetRecent(Mathf.Max(1, maxRecentSales));
        bool selectedNewestTrackedSale = false;
        ItemCategory newestCategory = trackedCategory;
        int newestSaleDay = 0;

        foreach (var record in records)
        {
            if (record == null)
                continue;

            int hash = BuildRecordHash(record);
            if (_seenSaleHashes.Contains(hash))
                continue;

            _seenSaleHashes.Add(hash);

            if (!Enum.TryParse(record.category, true, out ItemCategory category))
                continue;

            // VC-001A의 Processed 계약을 보존하면서 Raw/Utility/Luxury를 실제 변화로 확장한다.
            // GetRecent은 최신순이므로 한 refresh 사이에 여러 판매가 들어오면 가장 최근
            // 추적 카테고리 하나만 다음 날 대표 변화로 선택한다.
            if (category != trackedCategory
                && category != ItemCategory.Processed
                && category != ItemCategory.Raw
                && category != ItemCategory.Utility
                && category != ItemCategory.Luxury)
                continue;

            if (selectedNewestTrackedSale)
                continue;

            newestCategory = category;
            newestSaleDay = Mathf.Max(1, record.gameDay);
            selectedNewestTrackedSale = true;
        }

        if (selectedNewestTrackedSale)
        {
            _pendingCategory = newestCategory;
            _pendingSaleDay = newestSaleDay;
            _hasPendingChange = true;
            // 기존 활성 변화의 힌트 여부를 새 pending 변화가 이어받으면 두 번째
            // 카테고리의 설명이 영원히 생략된다. 새 변화마다 딱 한 번 다시 허용한다.
            _hintShownForActiveChange = false;
        }
    }

    void ActivateChange(ItemCategory category)
    {
        _activeCategory = category;
        _hasActiveCategory = true;
        _hasPendingChange = false;
        SetVisualActive(true);

        if (!_hintShownForActiveChange)
        {
            _hintShownForActiveChange = true;
            ShowHint(category);
        }
    }

    void EnsureVisual()
    {
        EnsureProcessedVisual();
        EnsureRawVisual();
        EnsureUtilityVisual();
        EnsureLuxuryVisual();
    }

    void EnsureProcessedVisual()
    {
        if (_visualRoot != null) return;

        var existing = GameObject.Find(ProcessedVisualRootName);
        if (existing != null)
        {
            _visualRoot = existing;
            return;
        }

        _visualRoot = new GameObject(ProcessedVisualRootName);
        _visualRoot.transform.SetParent(transform, false);
        PlaceVisualRoot(_visualRoot);
        _visualRoot.SetActive(false);

        CreateProcessedWorkbenchVisual();

        GameObject sign = CreateResourceProp(_visualRoot.transform, "PA_VillageCulture_Processed_Sign",
            VillageSignResource, new Vector3(0f, 1.66f, 0.58f), Vector3.zero, 0.44f);
        if (sign != null)
            CreateSignLabel(sign.transform, "PA_VillageCulture_Processed_SignLabel", "가공 준비대");

        SetVisualActive(false);
    }

    // 핵심 마을 변화에는 더 이상 원시 큐브 작업대를 만들지 않는다. 이미 실제 제작과
    // 카메라 검증을 거친 B05의 Visual 메시와 Project P.A. 준비 키트만 복제한다.
    // 래퍼의 Workbench/Collider/NavMeshObstacle은 처음부터 인스턴스화하지 않는다.
    void CreateProcessedWorkbenchVisual()
    {
        BuildingData definition = Resources.Load<BuildingData>(ProcessedWorkbenchDataResource);
        Transform sourceVisual = definition != null && definition.prefab != null
            ? definition.prefab.transform.Find("Visual")
            : null;
        if (sourceVisual == null)
        {
            Debug.LogWarning($"🏘️ [VillageCulture] B05 실제 Visual을 찾을 수 없습니다: {ProcessedWorkbenchDataResource}/prefab/Visual");
            return;
        }

        var holder = new GameObject("PA_VillageCulture_Processed_WorkbenchVisual");
        holder.transform.SetParent(_visualRoot.transform, false);
        holder.transform.localPosition = Vector3.zero;
        holder.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        holder.transform.localScale = Vector3.one * 0.58f;

        GameObject workbenchVisual = Instantiate(sourceVisual.gameObject, holder.transform, false);
        workbenchVisual.name = "PA_VillageCulture_Processed_B05Visual";
        StripToVisualOnly(workbenchVisual, removeLights: true);

        GameObject preparationKit = Resources.Load<GameObject>(ProcessedPreparationKitResource);
        if (preparationKit == null)
        {
            Debug.LogWarning($"🏘️ [VillageCulture] B05 준비 키트를 찾을 수 없습니다: {ProcessedPreparationKitResource}");
            return;
        }

        GameObject kitVisual = Instantiate(preparationKit, holder.transform, false);
        kitVisual.name = "PA_VillageCulture_Processed_PreparationKit";
        kitVisual.transform.localPosition = Vector3.zero;
        kitVisual.transform.localRotation = Quaternion.identity;
        kitVisual.transform.localScale = Vector3.one;
        StripToVisualOnly(kitVisual, removeLights: true);
    }

    // Raw 변화는 임시 원시 큐브를 늘리지 않는다. 프로젝트에 이미 들어와 출처가
    // 기록된 Nature Pack 실모델과 Project P.A. 자체 간판 메시만 조합한다.
    void EnsureRawVisual()
    {
        if (_rawVisualRoot != null) return;

        var existing = GameObject.Find(RawVisualRootName);
        if (existing != null)
        {
            _rawVisualRoot = existing;
            return;
        }

        _rawVisualRoot = new GameObject(RawVisualRootName);
        _rawVisualRoot.transform.SetParent(transform, false);
        PlaceVisualRoot(_rawVisualRoot);

        CreateResourceProp(_rawVisualRoot.transform, "PA_VillageCulture_Raw_WoodLogA",
            RawWoodLogResource, new Vector3(-0.48f, 0f, -0.06f), new Vector3(0f, 18f, 0f), 0.56f);
        CreateResourceProp(_rawVisualRoot.transform, "PA_VillageCulture_Raw_WoodLogB",
            RawWoodLogResource, new Vector3(0.02f, 0.02f, 0.18f), new Vector3(0f, -16f, 0f), 0.46f);
        CreateResourceProp(_rawVisualRoot.transform, "PA_VillageCulture_Raw_Rock",
            RawRockResource, new Vector3(0.50f, 0f, -0.02f), new Vector3(0f, -28f, 0f), 0.72f);

        GameObject sign = CreateResourceProp(_rawVisualRoot.transform, "PA_VillageCulture_Raw_Sign",
            VillageSignResource, new Vector3(0f, 1.08f, 0.42f), Vector3.zero, 0.52f);
        if (sign != null)
            CreateSignLabel(sign.transform, "PA_VillageCulture_Raw_SignLabel", "원자재 수거처");

        SetVisualActive(false);
    }

    // Utility 변화는 실제 철제 도구 제작과 연결된 B07의 Visual만 축소 재사용한다.
    // 원본 Workbench 래퍼와 물리·행동·열원 조명은 복제하지 않아 실제 제작대와
    // 혼동되거나 광장 동선을 막지 않는 다음 날 시각 신호로만 남긴다.
    void EnsureUtilityVisual()
    {
        if (_utilityVisualRoot != null) return;

        var existing = GameObject.Find(UtilityVisualRootName);
        if (existing != null)
        {
            _utilityVisualRoot = existing;
            return;
        }

        _utilityVisualRoot = new GameObject(UtilityVisualRootName);
        _utilityVisualRoot.transform.SetParent(transform, false);
        PlaceVisualRoot(_utilityVisualRoot);
        _utilityVisualRoot.SetActive(false);

        CreateUtilityForgeVisual();

        GameObject sign = CreateResourceProp(_utilityVisualRoot.transform, "PA_VillageCulture_Utility_Sign",
            VillageSignResource, new Vector3(0f, 1.46f, 0.84f), Vector3.zero, 0.44f);
        if (sign != null)
            CreateSignLabel(sign.transform, "PA_VillageCulture_Utility_SignLabel", "공구 수리대");

        SetVisualActive(false);
    }

    void CreateUtilityForgeVisual()
    {
        BuildingData definition = Resources.Load<BuildingData>(UtilityForgeDataResource);
        Transform sourceVisual = definition != null && definition.prefab != null
            ? definition.prefab.transform.Find("Visual")
            : null;
        if (sourceVisual == null)
        {
            Debug.LogWarning($"🏘️ [VillageCulture] B07 실제 Visual을 찾을 수 없습니다: {UtilityForgeDataResource}/prefab/Visual");
            return;
        }

        var holder = new GameObject("PA_VillageCulture_Utility_ForgeVisual");
        holder.transform.SetParent(_utilityVisualRoot.transform, false);
        holder.transform.localPosition = Vector3.zero;
        holder.transform.localRotation = Quaternion.identity;
        holder.transform.localScale = Vector3.one * 0.44f;

        GameObject forgeVisual = Instantiate(sourceVisual.gameObject, holder.transform, false);
        forgeVisual.name = "PA_VillageCulture_Utility_B07Visual";
        StripToVisualOnly(forgeVisual, removeLights: true);
    }

    // Luxury 변화는 의류 제작에 실제 연결된 B08의 파스텔 목재·천·마네킹
    // 실루엣을 생활 공예 전시로 재해석한다. B08 기능 래퍼와 물리·행동은
    // 복제하지 않아 실제 재봉 작업대가 아닌 다음 날 문화 신호로만 남긴다.
    void EnsureLuxuryVisual()
    {
        if (_luxuryVisualRoot != null) return;

        var existing = GameObject.Find(LuxuryVisualRootName);
        if (existing != null)
        {
            _luxuryVisualRoot = existing;
            return;
        }

        _luxuryVisualRoot = new GameObject(LuxuryVisualRootName);
        _luxuryVisualRoot.transform.SetParent(transform, false);
        PlaceVisualRoot(_luxuryVisualRoot);
        _luxuryVisualRoot.SetActive(false);

        CreateLuxuryCraftDisplayVisual();

        GameObject sign = CreateResourceProp(_luxuryVisualRoot.transform, "PA_VillageCulture_Luxury_Sign",
            VillageSignResource, new Vector3(0f, 1.42f, 0.76f), Vector3.zero, 0.44f);
        if (sign != null)
            CreateSignLabel(sign.transform, "PA_VillageCulture_Luxury_SignLabel", "공예 전시대");

        SetVisualActive(false);
    }

    void CreateLuxuryCraftDisplayVisual()
    {
        BuildingData definition = Resources.Load<BuildingData>(LuxurySewingDataResource);
        Transform sourceVisual = definition != null && definition.prefab != null
            ? definition.prefab.transform.Find("Visual")
            : null;
        if (sourceVisual == null)
        {
            Debug.LogWarning($"🏘️ [VillageCulture] B08 실제 Visual을 찾을 수 없습니다: {LuxurySewingDataResource}/prefab/Visual");
            return;
        }

        var holder = new GameObject("PA_VillageCulture_Luxury_CraftDisplayVisual");
        holder.transform.SetParent(_luxuryVisualRoot.transform, false);
        holder.transform.localPosition = Vector3.zero;
        holder.transform.localRotation = Quaternion.identity;
        holder.transform.localScale = Vector3.one * 0.48f;

        GameObject sewingVisual = Instantiate(sourceVisual.gameObject, holder.transform, false);
        sewingVisual.name = "PA_VillageCulture_Luxury_B08Visual";
        StripToVisualOnly(sewingVisual, removeLights: true);
    }

    void PlaceVisualRoot(GameObject root)
    {
        if (root == null)
            return;

        Shop shop = PA_ShopLocator.FindPlazaShop(); // S2 — 실내 상점 제외 앵커
        if (shop != null)
        {
            Vector3 right = shop.transform.right.sqrMagnitude > 0.001f ? shop.transform.right : Vector3.right;
            Vector3 forward = shop.transform.forward.sqrMagnitude > 0.001f ? shop.transform.forward : Vector3.forward;
            Vector3 target = shop.transform.position + right * 3.1f - forward * 1.9f;
            target = SnapNearGround(target, shop.transform.position.y);
            root.transform.position = target;
            root.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            return;
        }

        GameObject marker = GameObject.Find("PA_MarketStall_Hub_Visual");
        if (marker != null)
        {
            root.transform.position = SnapNearGround(marker.transform.position + new Vector3(3f, 0f, -2f), marker.transform.position.y);
            return;
        }

        root.transform.position = new Vector3(3f, 0f, -2f);
    }

    Vector3 SnapNearGround(Vector3 target, float fallbackY)
    {
        Vector3 rayStart = target + Vector3.up * 5f;
        if (Physics.Raycast(rayStart, Vector3.down, out var hit, 12f))
            return hit.point + Vector3.up * 0.04f;

        target.y = fallbackY + 0.04f;
        return target;
    }

    GameObject CreateResourceProp(Transform parent, string objectName, string resourcePath,
        Vector3 localPosition, Vector3 localEulerAngles, float uniformScale)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogWarning($"🏘️ [VillageCulture] 시각 리소스를 찾을 수 없습니다: {resourcePath}");
            return null;
        }

        GameObject instance = Instantiate(prefab, parent, false);
        instance.name = objectName;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.Euler(localEulerAngles);
        instance.transform.localScale = Vector3.one * uniformScale;

        // 마을 변화 표식은 읽기 전용 시각 사이드카다. 실제 저장함/채집 지점처럼
        // 오인되거나 동선을 막지 않도록 원본 기능·물리 컴포넌트를 복제하지 않는다.
        StripToVisualOnly(instance, removeLights: false);

        return instance;
    }

    void CreateSignLabel(Transform sign, string objectName, string labelText)
    {
        if (sign == null) return;

        var labelGo = new GameObject(objectName, typeof(TextMeshPro));
        labelGo.transform.SetParent(sign, false);
        labelGo.transform.localPosition = new Vector3(0f, 0f, 0.07f);
        labelGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        labelGo.transform.localScale = Vector3.one * 0.34f;

        TextMeshPro label = labelGo.GetComponent<TextMeshPro>();
        label.text = labelText;
        label.fontSize = 4.1f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.96f, 0.89f, 0.70f, 1f);
        label.overflowMode = TextOverflowModes.Overflow;
        label.rectTransform.sizeDelta = new Vector2(4.4f, 0.9f);
    }

    static void StripToVisualOnly(GameObject root, bool removeLights)
    {
        if (root == null)
            return;

        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
            DestroyUnityObject(collider);
        }

        foreach (var obstacle in root.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>(true))
        {
            obstacle.enabled = false;
            DestroyUnityObject(obstacle);
        }

        foreach (var body in root.GetComponentsInChildren<Rigidbody>(true))
        {
            body.detectCollisions = false;
            body.isKinematic = true;
            DestroyUnityObject(body);
        }

        foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            behaviour.enabled = false;
            DestroyUnityObject(behaviour);
        }

        if (!removeLights)
            return;

        foreach (var light in root.GetComponentsInChildren<Light>(true))
        {
            light.enabled = false;
            DestroyUnityObject(light);
        }
    }

    void EnsureHint()
    {
        if (!autoCreateHint || _hintPanel != null)
            return;

        var canvasGo = new GameObject("VillageCultureHintCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _hintCanvas = canvasGo.GetComponent<Canvas>();
        _hintCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _hintCanvas.sortingOrder = 58;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _hintPanel = new GameObject(HintPanelName, typeof(RectTransform), typeof(Image));
        _hintPanel.transform.SetParent(canvasGo.transform, false);
        var panelRt = (RectTransform)_hintPanel.transform;
        panelRt.anchorMin = new Vector2(0f, 0f);
        panelRt.anchorMax = new Vector2(0f, 0f);
        panelRt.pivot = new Vector2(0f, 0f);
        panelRt.anchoredPosition = new Vector2(22f, 170f);
        panelRt.sizeDelta = new Vector2(520f, 72f);

        var bg = _hintPanel.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.055f, 0.035f, 0.72f);
        bg.raycastTarget = false;

        var textGo = new GameObject("VillageCultureHintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(_hintPanel.transform, false);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(14f, 8f);
        textRt.offsetMax = new Vector2(-14f, -8f);

        _hintText = textGo.GetComponent<TextMeshProUGUI>();
        _hintText.fontSize = 15f;
        _hintText.fontStyle = FontStyles.Bold;
        _hintText.alignment = TextAlignmentOptions.TopLeft;
        _hintText.textWrappingMode = TextWrappingModes.Normal;
        _hintText.overflowMode = TextOverflowModes.Ellipsis;
        _hintText.color = new Color(1f, 0.90f, 0.66f, 1f);
        _hintText.raycastTarget = false;

        _hintPanel.SetActive(false);
    }

    void ShowHint(ItemCategory category)
    {
        if (_hintPanel == null || _hintText == null)
            return;

        _hintText.text = BuildHintText(category);
        _hintPanel.SetActive(true);
        _hintDisplayCount++;
        _hideHintAt = Time.unscaledTime + Mathf.Max(0.5f, hintSeconds);
    }

    string BuildHintText(ItemCategory category)
    {
        if (category == ItemCategory.Raw)
            return "마을 변화: 원자재 판매 덕분에 생산자 보관·수거 지점이 생겼어요.";

        if (category == ItemCategory.Processed)
            return "마을 변화: 가공품 판매 덕분에 따뜻한 준비 작업 공간이 생겼어요.";

        if (category == ItemCategory.Utility)
            return "마을 변화: 실용품 판매 덕분에 공구를 손보는 수리대가 생겼어요.";

        if (category == ItemCategory.Luxury)
            return "마을 변화: 고급품 판매 덕분에 생활 공예를 소개하는 전시대가 생겼어요.";

        return "마을 변화: 어제의 판매가 광장의 모습을 바꾸었어요.";
    }

    void SetVisualActive(bool active)
    {
        SetRootActive(_visualRoot, active && (!_hasActiveCategory || _activeCategory == ItemCategory.Processed));
        SetRootActive(_rawVisualRoot, active && _hasActiveCategory && _activeCategory == ItemCategory.Raw);
        SetRootActive(_utilityVisualRoot, active && _hasActiveCategory && _activeCategory == ItemCategory.Utility);
        SetRootActive(_luxuryVisualRoot, active && _hasActiveCategory && _activeCategory == ItemCategory.Luxury);
    }

    static bool IsVisualActive(GameObject root) => root != null && root.activeInHierarchy;

    static void SetRootActive(GameObject root, bool active)
    {
        if (root != null && root.activeSelf != active)
            root.SetActive(active);
    }

    int CurrentDay()
    {
        return GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
    }

    bool IsDayPreparationPhase()
    {
        var loop = DayNightShopLoopController.Instance;
        return loop == null || loop.CurrentPhase == PADayNightPhase.DayPreparation;
    }

    static int BuildRecordHash(SaleRecord record)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (record.itemName != null ? record.itemName.GetHashCode() : 0);
            hash = hash * 31 + (record.category != null ? record.category.GetHashCode() : 0);
            hash = hash * 31 + record.price;
            hash = hash * 31 + record.gameDay;
            hash = hash * 31 + record.gameHour;
            hash = hash * 31 + (record.buyerName != null ? record.buyerName.GetHashCode() : 0);
            return hash;
        }
    }

    static void DestroyUnityObject(UnityEngine.Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}
