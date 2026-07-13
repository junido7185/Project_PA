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
    public const string HintPanelName = "VillageCultureHintPanel";

    public static VillageCultureVisualController Instance { get; private set; }

    [Header("Village Culture Visual")]
    public bool autoCreateVisual = true;
    public bool autoCreateHint = true;
    public ItemCategory trackedCategory = ItemCategory.Processed;
    public int maxRecentSales = 40;
    public float refreshInterval = 0.75f;
    public float hintSeconds = 5f;

    GameObject _visualRoot;
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

    public GameObject VisualRoot => _visualRoot;
    public GameObject HintPanel => _hintPanel;
    public bool VisualActive => _visualRoot != null && _visualRoot.activeInHierarchy;
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

            if (category != trackedCategory)
                continue;

            _pendingCategory = category;
            _pendingSaleDay = Mathf.Max(1, record.gameDay);
            _hasPendingChange = true;
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
        if (_visualRoot != null)
            return;

        var existing = GameObject.Find(ProcessedVisualRootName);
        if (existing != null)
        {
            _visualRoot = existing;
            return;
        }

        _visualRoot = new GameObject(ProcessedVisualRootName);
        _visualRoot.transform.SetParent(transform, false);
        PlaceVisualRoot();

        CreateCube("PA_VillageCulture_Processed_Workbench",
            new Vector3(0f, 0.24f, 0f), new Vector3(1.45f, 0.22f, 0.72f),
            new Color(0.52f, 0.32f, 0.18f, 1f));
        CreateCube("PA_VillageCulture_Processed_CrateA",
            new Vector3(-0.46f, 0.55f, 0.06f), new Vector3(0.36f, 0.32f, 0.32f),
            new Color(0.92f, 0.63f, 0.33f, 1f));
        CreateCube("PA_VillageCulture_Processed_CrateB",
            new Vector3(0.02f, 0.56f, -0.02f), new Vector3(0.42f, 0.34f, 0.30f),
            new Color(0.96f, 0.70f, 0.38f, 1f));
        CreateCube("PA_VillageCulture_Processed_RecipeBoard",
            new Vector3(0.55f, 0.72f, -0.08f), new Vector3(0.12f, 0.78f, 0.62f),
            new Color(0.18f, 0.34f, 0.28f, 1f));
        CreateCube("PA_VillageCulture_Processed_WarmBanner",
            new Vector3(0f, 1.15f, -0.12f), new Vector3(1.25f, 0.14f, 0.08f),
            new Color(0.95f, 0.50f, 0.24f, 1f));

        SetVisualActive(false);
    }

    void PlaceVisualRoot()
    {
        if (_visualRoot == null)
            return;

        Shop shop = PA_ShopLocator.FindPlazaShop(); // S2 — 실내 상점 제외 앵커
        if (shop != null)
        {
            Vector3 right = shop.transform.right.sqrMagnitude > 0.001f ? shop.transform.right : Vector3.right;
            Vector3 forward = shop.transform.forward.sqrMagnitude > 0.001f ? shop.transform.forward : Vector3.forward;
            Vector3 target = shop.transform.position + right * 3.1f - forward * 1.9f;
            target = SnapNearGround(target, shop.transform.position.y);
            _visualRoot.transform.position = target;
            _visualRoot.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            return;
        }

        GameObject marker = GameObject.Find("PA_MarketStall_Hub_Visual");
        if (marker != null)
        {
            _visualRoot.transform.position = SnapNearGround(marker.transform.position + new Vector3(3f, 0f, -2f), marker.transform.position.y);
            return;
        }

        _visualRoot.transform.position = new Vector3(3f, 0f, -2f);
    }

    Vector3 SnapNearGround(Vector3 target, float fallbackY)
    {
        Vector3 rayStart = target + Vector3.up * 5f;
        if (Physics.Raycast(rayStart, Vector3.down, out var hit, 12f))
            return hit.point + Vector3.up * 0.04f;

        target.y = fallbackY + 0.04f;
        return target;
    }

    void CreateCube(string objectName, Vector3 localPosition, Vector3 localScale, Color color)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(_visualRoot.transform, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = localScale;

        foreach (var collider in cube.GetComponentsInChildren<Collider>(true))
            DestroyUnityObject(collider);

        var renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = CreateMaterial(objectName + "_Mat", color);
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
        if (category == ItemCategory.Processed)
            return "Market hub update: processed goods inspired a warm prep corner for tomorrow.";

        return "Market hub update: yesterday's sales changed the plaza mood.";
    }

    void SetVisualActive(bool active)
    {
        if (_visualRoot != null && _visualRoot.activeSelf != active)
            _visualRoot.SetActive(active);
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

    static Material CreateMaterial(string name, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        return new Material(shader) { name = name, color = color };
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
