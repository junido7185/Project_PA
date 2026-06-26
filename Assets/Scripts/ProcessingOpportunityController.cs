using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Management-readable processing advisor for the reverse supply-chain loop.
//
// This does not craft items and does not change recipe behavior. It reads the
// existing RecipeData assets and explains which raw producer goods can become
// higher-value processed goods.
public class ProcessingOpportunityController : MonoBehaviour
{
    public class Opportunity
    {
        public RecipeData recipe;
        public int inputBaseValue;
        public int expectedOutputValue;
        public int expectedMargin;
        public bool hasInputs;
        public bool unlocked;

        public float MarginRatio => inputBaseValue > 0
            ? expectedMargin / (float)inputBaseValue
            : 0f;
    }

    public static ProcessingOpportunityController Instance { get; private set; }

    [Header("Processing Advisor")]
    public bool autoCreateUI = true;
    public int visibleFromDay = 4;
    public int maxVisibleOpportunities = 2;

    [Header("UI")]
    public TextMeshProUGUI advisorText;

    RecipeData[] _recipes;
    Canvas _canvas;
    GameObject _panel;
    float _nextRefreshAt;
    string _lastAdvisorText = string.Empty;

    public string CurrentAdvisorText => advisorText != null ? advisorText.text : _lastAdvisorText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _recipes = Resources.LoadAll<RecipeData>("Recipes");

        if (autoCreateUI && advisorText == null)
            BuildUI();
    }

    void Start()
    {
        RefreshNow();
    }

    void Update()
    {
        if (Time.unscaledTime < _nextRefreshAt) return;
        _nextRefreshAt = Time.unscaledTime + 1.0f;
        RefreshNow();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public List<Opportunity> GetTopOpportunities(int maxCount, bool requireOwnedInputs)
    {
        var result = new List<Opportunity>();
        if (_recipes == null) _recipes = Resources.LoadAll<RecipeData>("Recipes");

        foreach (var recipe in _recipes)
        {
            var opportunity = Evaluate(recipe);
            if (opportunity == null) continue;
            if (!opportunity.unlocked) continue;
            if (requireOwnedInputs && !opportunity.hasInputs) continue;
            result.Add(opportunity);
        }

        result.Sort((a, b) =>
        {
            int marginCompare = b.expectedMargin.CompareTo(a.expectedMargin);
            if (marginCompare != 0) return marginCompare;
            return b.MarginRatio.CompareTo(a.MarginRatio);
        });

        if (maxCount > 0 && result.Count > maxCount)
            result.RemoveRange(maxCount, result.Count - maxCount);

        return result;
    }

    public Opportunity Evaluate(RecipeData recipe)
    {
        if (recipe == null || recipe.outputItem == null || recipe.ingredients == null || recipe.ingredients.Count == 0)
            return null;

        int inputValue = 0;
        bool hasInputs = Inventory.instance != null;

        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null || ingredient.count <= 0)
                return null;

            inputValue += Mathf.Max(1, ingredient.item.basePrice) * ingredient.count;

            if (Inventory.instance == null || !Inventory.instance.HasItems(ingredient.item, ingredient.count))
                hasInputs = false;
        }

        int outputBase = Mathf.Max(1, recipe.outputItem.basePrice) * Mathf.Max(1, recipe.outputCount);
        float qualityPriceFactor = 1f + 0.5f * (Mathf.Max(0.5f, recipe.baseOutputQuality) - 1f);
        int expectedOutput = Mathf.Max(1, Mathf.RoundToInt(outputBase * qualityPriceFactor));
        bool unlocked = (TierService.Instance == null || TierService.Instance.IsUnlocked(recipe.requiredTier))
            && (FriendshipService.Instance == null || FriendshipService.Instance.IsRecipeUnlocked(recipe));

        return new Opportunity
        {
            recipe = recipe,
            inputBaseValue = inputValue,
            expectedOutputValue = expectedOutput,
            expectedMargin = expectedOutput - inputValue,
            hasInputs = hasInputs,
            unlocked = unlocked
        };
    }

    public void RefreshNow()
    {
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        bool shouldShow = day >= visibleFromDay;
        if (_panel != null && _panel.activeSelf != shouldShow)
            _panel.SetActive(shouldShow);

        var owned = GetTopOpportunities(maxVisibleOpportunities, requireOwnedInputs: true);
        var candidates = owned.Count > 0
            ? owned
            : GetTopOpportunities(maxVisibleOpportunities, requireOwnedInputs: false);

        _lastAdvisorText = BuildAdvisorText(day, candidates, owned.Count > 0);
        if (advisorText != null)
            advisorText.text = _lastAdvisorText;
    }

    string BuildAdvisorText(int day, List<Opportunity> opportunities, bool hasOwnedInputs)
    {
        if (day < visibleFromDay)
            return "Processing Focus unlocks on Day 4.";

        if (opportunities == null || opportunities.Count == 0)
            return "Processing Focus\nNo valid processing recipes found yet.";

        var lines = new List<string>
        {
            hasOwnedInputs
                ? "Processing Focus - ready chains"
                : "Processing Focus - target chains"
        };

        foreach (var opportunity in opportunities)
        {
            string outputName = opportunity.recipe.outputItem != null
                ? opportunity.recipe.outputItem.itemName
                : opportunity.recipe.recipeName;
            string readiness = opportunity.hasInputs ? "ready" : "need inputs";
            lines.Add($"{outputName}: +{opportunity.expectedMargin}G est. ({readiness})");
        }

        return string.Join("\n", lines);
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("ProcessingOpportunityCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 53;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _panel = new GameObject("ProcessingOpportunityPanel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)_panel.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -252f);
        rt.sizeDelta = new Vector2(590f, 88f);

        var bg = _panel.GetComponent<Image>();
        bg.color = new Color(0.10f, 0.07f, 0.04f, 0.58f);
        bg.raycastTarget = false;

        var textGo = new GameObject("ProcessingOpportunityText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(_panel.transform, false);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(14f, 8f);
        textRt.offsetMax = new Vector2(-14f, -8f);

        advisorText = textGo.GetComponent<TextMeshProUGUI>();
        advisorText.fontSize = 14f;
        advisorText.fontStyle = FontStyles.Bold;
        advisorText.alignment = TextAlignmentOptions.TopLeft;
        advisorText.textWrappingMode = TextWrappingModes.Normal;
        advisorText.overflowMode = TextOverflowModes.Ellipsis;
        advisorText.color = new Color(1f, 0.91f, 0.70f, 1f);
        advisorText.raycastTarget = false;

        _panel.SetActive(false);
    }
}
