using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 가공(Crafting) 패널 UI.
//
// 두 가지 진입 경로:
// 1) Workbench 에서 OpenForWorkbench(Workbench) 호출 — 정식 진입.
//    - 해당 워크샵 종류와 일치하는 레시피만 표시.
//    - 티어 잠긴 레시피는 회색 처리(클릭은 가능하나 CraftingService 가 차단).
// 2) C 키 글로벌 토글 (디버그) — _activeWorkbench=null 로 동작.
//    - requiredWorkbench == None 인 레시피만 표시.
//    - 워크샵 진입 흐름이 미완성인 동안의 임시 도구.
//
// 클릭 시:
// - CraftingService.TryCraft(recipe, _activeWorkbench) 로 모든 검사·차감·결과 생성을 위임.
// - UI 는 패널 표시/필터링만 담당하고, 비즈니스 로직은 보유하지 않는다.
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

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        allRecipes = Resources.LoadAll<RecipeData>("Recipes");

        if (craftingPanel == null || slotParent == null || slotPrefab == null)
            BuildRuntimeUI();
    }

    void Start()
    {
        if (craftingPanel != null) craftingPanel.SetActive(false);

        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnCraftToggle += ToggleUI;
    }

    void OnDestroy()
    {
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnCraftToggle -= ToggleUI;
    }

    // -------- 외부 진입점 --------

    // Workbench.Interact 에서 호출. 해당 작업대 종류에 맞는 레시피만 표시한다.
    public void OpenForWorkbench(Workbench wb)
    {
        _activeWorkbench = wb;
        GenerateSlotsForContext();

        if (craftingPanel != null) craftingPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // C 키 글로벌 디버그 토글. _activeWorkbench=null → requiredWorkbench=None 만 표시.
    public void ToggleUI()
    {
        if (craftingPanel == null) return;

        if (craftingPanel.activeSelf)
        {
            craftingPanel.SetActive(false);
            _activeWorkbench = null;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // 디버그 글로벌 모드로 열기
            OpenForWorkbench(null);
        }
    }

    // -------- 슬롯 생성 (컨텍스트 필터 적용) --------

    void GenerateSlotsForContext()
    {
        if (slotParent == null || slotPrefab == null) return;

        // 기존 자식 슬롯 제거
        foreach (Transform child in slotParent) Destroy(child.gameObject);

        if (allRecipes == null) return;

        foreach (RecipeData recipe in allRecipes)
        {
            if (recipe == null) continue;
            if (!PassesWorkbenchFilter(recipe)) continue;

            bool unlocked = (TierService.Instance == null
                             || TierService.Instance.IsUnlocked(recipe.requiredTier))
                            && (FriendshipService.Instance == null
                                || FriendshipService.Instance.IsRecipeUnlocked(recipe));

            GameObject newSlot = Instantiate(slotPrefab, slotParent);
            newSlot.SetActive(true);

            // 라벨 — 첫 재료를 미리보기로 표시 (다중 재료는 첫 재료 + ⋯ 로 압축)
            TMP_Text slotText = newSlot.GetComponentInChildren<TMP_Text>();
            if (slotText != null)
            {
                slotText.text = BuildSlotLabel(recipe, unlocked);
            }

            // 잠금 시 이미지 회색 처리
            if (!unlocked)
            {
                foreach (var img in newSlot.GetComponentsInChildren<Image>())
                {
                    img.color = lockedColor;
                }
            }

            Button btn = newSlot.GetComponent<Button>();
            if (btn != null)
            {
                RecipeData captured = recipe;       // 클로저 캡처
                Workbench wbCaptured = _activeWorkbench;
                btn.onClick.AddListener(() => CraftingService.TryCraft(captured, wbCaptured));
            }
        }
    }

    // 컨텍스트(작업대 종류)에 맞는지 판정한다.
    bool PassesWorkbenchFilter(RecipeData recipe)
    {
        // 디버그 글로벌 모드: requiredWorkbench=None 만 보이게
        if (_activeWorkbench == null)
            return recipe.requiredWorkbench == WorkbenchType.None;

        // 정식 모드: None 은 어디서나, 그 외는 종류 일치 시
        return recipe.requiredWorkbench == WorkbenchType.None
            || recipe.requiredWorkbench == _activeWorkbench.workbenchType;
    }

    string BuildSlotLabel(RecipeData recipe, bool unlocked)
    {
        string lockMark = unlocked ? "" : $"🔒 (Tier {recipe.requiredTier}) ";
        string ingredientText;

        if (recipe.ingredients != null && recipe.ingredients.Count > 0 && recipe.ingredients[0] != null && recipe.ingredients[0].item != null)
        {
            var first = recipe.ingredients[0];
            string firstName = first.item.itemName;
            string suffix = recipe.ingredients.Count > 1 ? $" 외 {recipe.ingredients.Count - 1}종" : "";
            ingredientText = $"{firstName} ×{first.count}{suffix}";
        }
        else
        {
            ingredientText = "재료 누락";
        }

        return $"{lockMark}{recipe.recipeName}\n<size=80%>({ingredientText})</size>";
    }

    void BuildRuntimeUI()
    {
        Transform parent = ResolveUiParent();

        craftingPanel = new GameObject("CraftingPanel", typeof(RectTransform), typeof(Image));
        var panelRT = (RectTransform)craftingPanel.transform;
        panelRT.SetParent(parent, false);
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(560f, 640f);
        panelRT.anchoredPosition = Vector2.zero;

        var bg = craftingPanel.GetComponent<Image>();
        bg.color = new Color(0.96f, 0.90f, 0.78f, 0.98f);

        var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        var titleRT = (RectTransform)titleGO.transform;
        titleRT.SetParent(craftingPanel.transform, false);
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.sizeDelta = new Vector2(0f, 52f);
        titleRT.anchoredPosition = Vector2.zero;

        var title = titleGO.GetComponent<TextMeshProUGUI>();
        title.text = "제작";
        title.fontSize = 26;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.18f, 0.13f, 0.08f, 1f);
        title.raycastTarget = false;

        var closeGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        var closeRT = (RectTransform)closeGO.transform;
        closeRT.SetParent(craftingPanel.transform, false);
        closeRT.anchorMin = closeRT.anchorMax = new Vector2(1f, 1f);
        closeRT.pivot = new Vector2(1f, 1f);
        closeRT.sizeDelta = new Vector2(72f, 36f);
        closeRT.anchoredPosition = new Vector2(-12f, -10f);
        closeGO.GetComponent<Image>().color = new Color(0.22f, 0.18f, 0.14f, 0.9f);

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
        closeGO.GetComponent<Button>().onClick.AddListener(ToggleUI);

        var scrollGO = new GameObject("RecipeScroll", typeof(RectTransform), typeof(ScrollRect));
        var scrollRT = (RectTransform)scrollGO.transform;
        scrollRT.SetParent(craftingPanel.transform, false);
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(18f, 18f);
        scrollRT.offsetMax = new Vector2(-18f, -64f);

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRT = (RectTransform)viewportGO.transform;
        viewportRT.SetParent(scrollGO.transform, false);
        Stretch(viewportRT);
        viewportGO.GetComponent<Image>().color = Color.clear;
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

        slotParent = contentRT;
        slotPrefab = CreateRuntimeSlotPrefab(craftingPanel.transform);
        craftingPanel.SetActive(false);
    }

    GameObject CreateRuntimeSlotPrefab(Transform parent)
    {
        var go = new GameObject("RecipeSlot_RuntimePrefab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.SetActive(false);
        go.GetComponent<Image>().color = new Color(0.22f, 0.18f, 0.14f, 0.92f);
        go.GetComponent<LayoutElement>().preferredHeight = 68f;

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRT = (RectTransform)labelGO.transform;
        labelRT.SetParent(go.transform, false);
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(12f, 6f);
        labelRT.offsetMax = new Vector2(-12f, -6f);

        var label = labelGO.GetComponent<TextMeshProUGUI>();
        label.fontSize = 17;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
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
