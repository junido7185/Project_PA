using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 제작 패널. C 키는 모든 기존 레시피와 필요한 작업대를 보여 주는 읽기 전용 도감,
// Workbench.Interact는 해당 작업대에서 실제 제작하는 제품 진입점이다.
// 재료 차감·결과 생성의 단일 권위는 계속 CraftingService.TryCraft에 둔다.
public class CraftingUI : MonoBehaviour
{
    public static CraftingUI instance;

    [Header("UI 참조")]
    public GameObject craftingPanel;
    public Transform slotParent;
    public GameObject slotPrefab;

    [Header("표시 옵션")]
    [Tooltip("티어/워크샵으로 잠긴 레시피를 회색으로 표시할 색상")]
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

    private RecipeData[] allRecipes;
    private Workbench _activeWorkbench;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _modeText;
    private TextMeshProUGUI _statusText;
    private PlayerInputHandler _input;
    private bool _cursorCaptured;
    private CursorLockMode _cursorLockBeforeOpen = CursorLockMode.Locked;
    private bool _cursorVisibleBeforeOpen;

    public bool IsOpen => craftingPanel != null && craftingPanel.activeSelf;
    public bool IsRecipeBookMode => IsOpen && _activeWorkbench == null;
    public Workbench ActiveWorkbench => _activeWorkbench;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            // PA_RuntimeUI의 다른 UI 컴포넌트까지 파괴하지 않는다.
            Destroy(this);
            return;
        }

        instance = this;
        allRecipes = Resources.LoadAll<RecipeData>("Recipes");
        System.Array.Sort(allRecipes, CompareRecipes);

        if (craftingPanel == null || slotParent == null || slotPrefab == null)
            BuildRuntimeUI();
    }

    void Start()
    {
        if (craftingPanel != null) craftingPanel.SetActive(false);

        _input = PlayerInputHandler.Instance;
        if (_input == null) return;

        _input.OnCraftToggle += ToggleUI;
        _input.OnInventoryToggle += CloseForOtherPanel;
        _input.OnPhoneToggle += CloseForOtherPanel;
    }

    void OnDestroy()
    {
        if (_input != null)
        {
            _input.OnCraftToggle -= ToggleUI;
            _input.OnInventoryToggle -= CloseForOtherPanel;
            _input.OnPhoneToggle -= CloseForOtherPanel;
        }

        if (_cursorCaptured) RestoreCursor();
        if (instance == this) instance = null;
    }

    // -------- 외부 진입점 --------

    // Workbench.Interact 에서 호출. 해당 작업대 종류에 맞는 레시피만 표시한다.
    public void OpenForWorkbench(Workbench wb)
    {
        if (wb == null)
        {
            OpenRecipeBook();
            return;
        }

        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            return;

        EnsureOpenState();
        _activeWorkbench = wb;
        GenerateSlotsForContext();
        SetStatus($"{wb.displayName}에서 만들 상품을 선택하세요.");
    }

    // C 키는 원격 제작이 아니라 전체 레시피와 요구 작업대를 확인하는 도감이다.
    public void OpenRecipeBook()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            return;

        EnsureOpenState();
        _activeWorkbench = null;
        GenerateSlotsForContext();
        SetStatus("도감에서는 제작할 수 없습니다. 월드의 작업대 앞에서 [Space]를 누르세요.");
    }

    public void ToggleUI()
    {
        if (IsOpen) Close();
        else OpenRecipeBook();
    }

    public void Close()
    {
        Close(restoreCursor: true);
    }

    void Close(bool restoreCursor)
    {
        if (craftingPanel != null) craftingPanel.SetActive(false);
        _activeWorkbench = null;

        if (restoreCursor) RestoreCursor();
        else _cursorCaptured = false;
    }

    void CloseForOtherPanel()
    {
        if (IsOpen)
            Close(restoreCursor: false);
    }

    void EnsureOpenState()
    {
        if (craftingPanel == null || slotParent == null || slotPrefab == null)
            BuildRuntimeUI();

        if (!IsOpen)
        {
            CloseConflictingPanels();
            CaptureCursor();
        }

        craftingPanel.transform.SetAsLastSibling();
        craftingPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseConflictingPanels()
    {
        if (StorageUI.instance != null && StorageUI.instance.IsOpen)
            StorageUI.instance.CloseBox();

        if (SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen)
            SmartphoneUI.instance.Toggle();

        if (InventoryUI.instance != null && InventoryUI.instance.gameObject.activeSelf)
            InventoryUI.instance.Toggle();
    }

    void CaptureCursor()
    {
        _cursorLockBeforeOpen = Cursor.lockState;
        _cursorVisibleBeforeOpen = Cursor.visible;
        _cursorCaptured = true;
    }

    void RestoreCursor()
    {
        if (!_cursorCaptured) return;
        Cursor.lockState = _cursorLockBeforeOpen;
        Cursor.visible = _cursorVisibleBeforeOpen;
        _cursorCaptured = false;
    }

    // -------- 슬롯 생성 (컨텍스트 필터 적용) --------

    void GenerateSlotsForContext()
    {
        if (slotParent == null || slotPrefab == null) return;

        // 기존 자식 슬롯 제거
        foreach (Transform child in slotParent) Destroy(child.gameObject);

        if (allRecipes == null) return;

        int visibleCount = 0;
        foreach (RecipeData recipe in allRecipes)
        {
            if (recipe == null) continue;
            if (!PassesWorkbenchFilter(recipe)) continue;
            visibleCount++;

            bool unlocked = IsUnlocked(recipe);
            bool hasIngredients = HasAllIngredients(recipe);
            bool canCraftHere = _activeWorkbench != null && ContextMatches(recipe, _activeWorkbench);

            GameObject newSlot = Instantiate(slotPrefab, slotParent);
            newSlot.SetActive(true);
            newSlot.name = $"Recipe_{recipe.name}";

            TMP_Text slotText = newSlot.GetComponentInChildren<TMP_Text>(true);
            if (slotText != null)
                slotText.text = BuildSlotLabel(recipe, unlocked);

            Transform iconTransform = newSlot.transform.Find("Icon");
            if (iconTransform != null && iconTransform.TryGetComponent(out Image icon))
            {
                icon.sprite = recipe.icon != null
                    ? recipe.icon
                    : recipe.outputItem != null ? recipe.outputItem.icon : null;
                icon.enabled = icon.sprite != null;
            }

            if (newSlot.TryGetComponent(out Image card))
                card.color = ResolveCardColor(unlocked, hasIngredients, canCraftHere);

            Button btn = newSlot.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = unlocked && canCraftHere;
                RecipeData captured = recipe;
                Workbench wbCaptured = _activeWorkbench;
                btn.onClick.AddListener(() => TryCraftRecipe(captured, wbCaptured));
            }
        }

        // Runtime-generated cards are created while the panel is being opened. Force the
        // layout now so the first visible frame has non-zero content/card geometry.
        Canvas.ForceUpdateCanvases();
        if (slotParent is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        Canvas.ForceUpdateCanvases();

        if (_titleText != null)
            _titleText.text = _activeWorkbench == null
                ? "제작 도감"
                : $"{_activeWorkbench.displayName} 제작";

        if (_modeText != null)
            _modeText.text = _activeWorkbench == null
                ? $"전체 레시피 {visibleCount}개 · 작업대에서 [Space]로 제작"
                : $"{GetWorkbenchLabel(_activeWorkbench.workbenchType)} · 가능한 레시피 {visibleCount}개";
    }

    // 컨텍스트(작업대 종류)에 맞는지 판정한다.
    bool PassesWorkbenchFilter(RecipeData recipe)
    {
        // 도감은 전체 레시피를 보여 주되 버튼을 비활성화해 원격 제작을 차단한다.
        if (_activeWorkbench == null)
            return true;

        return ContextMatches(recipe, _activeWorkbench);
    }

    string BuildSlotLabel(RecipeData recipe, bool unlocked)
    {
        string recipeName = string.IsNullOrWhiteSpace(recipe.recipeName) ? recipe.name : recipe.recipeName;
        string outputName = recipe.outputItem != null ? recipe.outputItem.itemName : "결과 누락";
        string lockMark = unlocked ? "" : $"<color=#E9A59A>잠김 · Tier {recipe.requiredTier}</color>  ";
        string station = GetWorkbenchLabel(recipe.requiredWorkbench);
        string ingredients = BuildIngredientText(recipe);

        return $"{lockMark}<b>{recipeName}</b>  →  {outputName} ×{Mathf.Max(1, recipe.outputCount)}\n" +
               $"<size=78%><color=#C8D8CA>재료: {ingredients}</color></size>\n" +
               $"<size=72%><color=#91B69A>{station}</color></size>";
    }

    string BuildIngredientText(RecipeData recipe)
    {
        if (recipe.ingredients == null || recipe.ingredients.Count == 0)
            return "재료 정보 없음";

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null || ingredient.count <= 0) continue;
            if (result.Length > 0) result.Append(" · ");

            int owned = Inventory.instance != null ? Inventory.instance.CountItems(ingredient.item) : 0;
            string color = owned >= ingredient.count ? "#CDE7C9" : "#F0B39E";
            result.Append($"<color={color}>{ingredient.item.itemName} {owned}/{ingredient.count}</color>");
        }

        return result.Length > 0 ? result.ToString() : "재료 정보 없음";
    }

    void TryCraftRecipe(RecipeData recipe, Workbench workbench)
    {
        if (recipe == null || workbench == null)
        {
            SetStatus("도감에서는 제작할 수 없습니다. 필요한 작업대를 이용하세요.", true);
            return;
        }

        bool success = CraftingService.TryCraft(recipe, workbench);
        GenerateSlotsForContext();

        string recipeName = string.IsNullOrWhiteSpace(recipe.recipeName) ? recipe.name : recipe.recipeName;
        SetStatus(success
            ? $"{recipeName} 제작 완료 · 가방과 핫바를 갱신했습니다."
            : BuildFailureMessage(recipe), !success);
    }

    string BuildFailureMessage(RecipeData recipe)
    {
        if (!IsUnlocked(recipe))
            return $"Tier {recipe.requiredTier} 또는 주민 교류 조건이 아직 잠겨 있습니다.";

        if (Inventory.instance == null)
            return "플레이어 인벤토리를 찾지 못했습니다.";

        if (!HasAllIngredients(recipe))
            return "재료가 부족합니다. 카드의 보유량/필요량을 확인하세요.";

        return "제작하지 못했습니다. 결과물을 받을 가방 공간을 확인하세요.";
    }

    bool IsUnlocked(RecipeData recipe)
    {
        return (TierService.Instance == null || TierService.Instance.IsUnlocked(recipe.requiredTier))
            && (FriendshipService.Instance == null || FriendshipService.Instance.IsRecipeUnlocked(recipe));
    }

    bool HasAllIngredients(RecipeData recipe)
    {
        if (Inventory.instance == null || recipe.ingredients == null || recipe.ingredients.Count == 0)
            return false;

        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null || ingredient.count <= 0) continue;
            if (!Inventory.instance.HasItems(ingredient.item, ingredient.count)) return false;
        }

        return true;
    }

    static bool ContextMatches(RecipeData recipe, Workbench workbench)
    {
        return recipe != null && workbench != null
            && (recipe.requiredWorkbench == WorkbenchType.None
                || recipe.requiredWorkbench == workbench.workbenchType);
    }

    Color ResolveCardColor(bool unlocked, bool hasIngredients, bool canCraftHere)
    {
        if (!unlocked) return lockedColor;
        if (!canCraftHere) return new Color(0.12f, 0.16f, 0.14f, 0.90f);
        return hasIngredients
            ? new Color(0.15f, 0.34f, 0.23f, 0.98f)
            : new Color(0.31f, 0.22f, 0.15f, 0.96f);
    }

    void SetStatus(string message, bool isError = false)
    {
        if (_statusText == null) return;
        _statusText.text = message;
        _statusText.color = isError
            ? new Color(1f, 0.67f, 0.57f, 1f)
            : new Color(0.77f, 0.88f, 0.79f, 1f);
    }

    static int CompareRecipes(RecipeData left, RecipeData right)
    {
        if (ReferenceEquals(left, right)) return 0;
        if (left == null) return 1;
        if (right == null) return -1;

        int workbench = left.requiredWorkbench.CompareTo(right.requiredWorkbench);
        if (workbench != 0) return workbench;
        int tier = left.requiredTier.CompareTo(right.requiredTier);
        if (tier != 0) return tier;
        return string.Compare(left.recipeName, right.recipeName, System.StringComparison.Ordinal);
    }

    static string GetWorkbenchLabel(WorkbenchType type)
    {
        switch (type)
        {
            case WorkbenchType.BasicWorkbench: return "목재 가공 작업대";
            case WorkbenchType.Kitchen: return "주방 작업대";
            case WorkbenchType.Forge: return "대장간 작업대";
            case WorkbenchType.SewingTable: return "재봉 작업대";
            default: return "기본 제작";
        }
    }

    void BuildRuntimeUI()
    {
        // 씬의 부분 직렬화 패널이 세 참조를 모두 갖추지 못했다면 화면만 숨기고
        // 재현 가능한 런타임 제품 UI를 별도로 구성한다.
        if (craftingPanel != null)
            craftingPanel.SetActive(false);

        Transform parent = ResolveUiParent();

        craftingPanel = new GameObject("CraftingOverlay", typeof(RectTransform), typeof(Image));
        var overlayRT = (RectTransform)craftingPanel.transform;
        overlayRT.SetParent(parent, false);
        Stretch(overlayRT);
        craftingPanel.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.02f, 0.78f);

        var window = new GameObject("CraftingPanel", typeof(RectTransform), typeof(Image));
        var panelRT = (RectTransform)window.transform;
        panelRT.SetParent(craftingPanel.transform, false);
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(720f, 720f);
        panelRT.anchoredPosition = Vector2.zero;
        window.GetComponent<Image>().color = new Color(0.055f, 0.085f, 0.068f, 0.985f);

        var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        var titleRT = (RectTransform)titleGO.transform;
        titleRT.SetParent(window.transform, false);
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(0f, 48f);
        titleRT.anchoredPosition = new Vector2(0f, -12f);

        _titleText = titleGO.GetComponent<TextMeshProUGUI>();
        _titleText.text = "제작 도감";
        _titleText.fontSize = 30;
        _titleText.fontStyle = FontStyles.Bold;
        _titleText.alignment = TextAlignmentOptions.Center;
        _titleText.color = Color.white;
        _titleText.raycastTarget = false;

        var modeGO = new GameObject("Mode", typeof(RectTransform), typeof(TextMeshProUGUI));
        var modeRT = (RectTransform)modeGO.transform;
        modeRT.SetParent(window.transform, false);
        modeRT.anchorMin = new Vector2(0f, 1f);
        modeRT.anchorMax = new Vector2(1f, 1f);
        modeRT.pivot = new Vector2(0.5f, 1f);
        modeRT.sizeDelta = new Vector2(-100f, 30f);
        modeRT.anchoredPosition = new Vector2(0f, -57f);

        _modeText = modeGO.GetComponent<TextMeshProUGUI>();
        _modeText.text = "전체 레시피";
        _modeText.fontSize = 16f;
        _modeText.alignment = TextAlignmentOptions.Center;
        _modeText.color = new Color(0.65f, 0.78f, 0.68f, 1f);
        _modeText.raycastTarget = false;

        var closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var closeRT = (RectTransform)closeGO.transform;
        closeRT.SetParent(window.transform, false);
        closeRT.anchorMin = closeRT.anchorMax = new Vector2(1f, 1f);
        closeRT.pivot = new Vector2(1f, 1f);
        closeRT.sizeDelta = new Vector2(72f, 36f);
        closeRT.anchoredPosition = new Vector2(-12f, -10f);
        closeGO.GetComponent<Image>().color = new Color(0.55f, 0.27f, 0.22f, 0.96f);

        var closeTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        var closeTextRT = (RectTransform)closeTextGO.transform;
        closeTextRT.SetParent(closeGO.transform, false);
        Stretch(closeTextRT);
        var closeText = closeTextGO.GetComponent<TextMeshProUGUI>();
        closeText.text = "닫기";
        closeText.fontSize = 16;
        closeText.fontStyle = FontStyles.Bold;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.color = Color.white;
        closeText.raycastTarget = false;
        closeGO.GetComponent<Button>().onClick.AddListener(Close);

        var scrollGO = new GameObject("RecipeScroll", typeof(RectTransform), typeof(ScrollRect));
        var scrollRT = (RectTransform)scrollGO.transform;
        scrollRT.SetParent(window.transform, false);
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(20f, 76f);
        scrollRT.offsetMax = new Vector2(-20f, -96f);

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRT = (RectTransform)viewportGO.transform;
        viewportRT.SetParent(scrollGO.transform, false);
        Stretch(viewportRT);
        // Mask uses the graphic alpha when writing its stencil. showMaskGraphic hides the
        // viewport background, so the mask graphic itself must stay opaque for its children.
        viewportGO.GetComponent<Image>().color = Color.white;
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        var contentRT = (RectTransform)contentGO.transform;
        contentRT.SetParent(viewportGO.transform, false);
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = Vector2.zero;

        var layout = contentGO.GetComponent<VerticalLayoutGroup>();
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 8f;
        layout.padding = new RectOffset(4, 4, 4, 4);
        contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.viewport = viewportRT;
        scroll.content = contentRT;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 34f;

        var statusGO = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
        var statusRT = (RectTransform)statusGO.transform;
        statusRT.SetParent(window.transform, false);
        statusRT.anchorMin = new Vector2(0f, 0f);
        statusRT.anchorMax = new Vector2(1f, 0f);
        statusRT.pivot = new Vector2(0.5f, 0f);
        statusRT.sizeDelta = new Vector2(-40f, 56f);
        statusRT.anchoredPosition = new Vector2(0f, 12f);

        _statusText = statusGO.GetComponent<TextMeshProUGUI>();
        _statusText.text = "작업대에서 [Space]를 누르면 제작할 수 있습니다.";
        _statusText.fontSize = 15f;
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.color = new Color(0.77f, 0.88f, 0.79f, 1f);
        _statusText.textWrappingMode = TextWrappingModes.Normal;
        _statusText.raycastTarget = false;

        slotParent = contentRT;
        slotPrefab = CreateRuntimeSlotPrefab(window.transform);
        craftingPanel.SetActive(false);
    }

    GameObject CreateRuntimeSlotPrefab(Transform parent)
    {
        var go = new GameObject("RecipeSlot_RuntimePrefab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.SetActive(false);
        go.GetComponent<Image>().color = new Color(0.13f, 0.20f, 0.16f, 0.96f);
        go.GetComponent<LayoutElement>().preferredHeight = 96f;

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var iconRT = (RectTransform)iconGO.transform;
        iconRT.SetParent(go.transform, false);
        iconRT.anchorMin = iconRT.anchorMax = new Vector2(0f, 0.5f);
        iconRT.pivot = new Vector2(0f, 0.5f);
        iconRT.anchoredPosition = new Vector2(14f, 0f);
        iconRT.sizeDelta = new Vector2(68f, 68f);
        var icon = iconGO.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRT = (RectTransform)labelGO.transform;
        labelRT.SetParent(go.transform, false);
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(94f, 8f);
        labelRT.offsetMax = new Vector2(-14f, -8f);

        var label = labelGO.GetComponent<TextMeshProUGUI>();
        label.fontSize = 16.5f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;

        return go;
    }

    Transform ResolveUiParent()
    {
        var uiRoot = GameObject.Find("PA_UIRoot");
        if (uiRoot != null) return uiRoot.transform;

        var canvasGO = new GameObject("CraftingUI_Canvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvasGO.transform;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
