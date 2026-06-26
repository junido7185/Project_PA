using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Long-play layer for Project_PA 1.0 development.
//
// This controller deliberately sits beside the Day 1 demo controller instead of
// replacing it. Day 1 remains the onboarding route; Day 2-7 add a repeatable
// operation loop: producer delivery -> buy-in -> stock/price -> customer
// response -> revenue -> next-day planning.
public class LongPlayProgressionController : MonoBehaviour
{
    [Serializable]
    class SupplyEntry
    {
        public string resourcePath;
        public int count;

        public SupplyEntry(string resourcePath, int count)
        {
            this.resourcePath = resourcePath;
            this.count = count;
        }
    }

    [Serializable]
    class DayPlan
    {
        public int day;
        public string title;
        public string objective;
        public string managementFocus;
        public long revenueTarget;
        public float buyPriceMultiplier;
        public List<SupplyEntry> supplies = new List<SupplyEntry>();
    }

    public static LongPlayProgressionController Instance { get; private set; }

    [Header("Long Play Runtime")]
    public bool autoCreateUI = true;
    public bool enableDailyNpcSupply = true;
    [Range(2, 30)] public int finalPlannedDay = 7;

    [Header("UI")]
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI supplyText;

    readonly List<DayPlan> _plans = new List<DayPlan>();

    Canvas _canvas;
    long _dayStartRevenue;
    int _dayStartMoney;
    int _lastSupplyDay;
    int _lastDisplayedDay = -1;
    string _lastSupplyResult = "NPC producer delivery pending.";
    bool _restoredFromSave;

    public int LastSupplyDay => _lastSupplyDay;
    public long DayStartRevenue => _dayStartRevenue;
    public int DayStartMoney => _dayStartMoney;
    public string CurrentGoalText => objectiveText != null ? objectiveText.text : string.Empty;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsurePlans();

        if (autoCreateUI && objectiveText == null)
            BuildUI();
    }

    void Start()
    {
        if (!_restoredFromSave)
            CaptureDayBaselines();

        if (GameClock.Instance != null)
            GameClock.Instance.OnNewDay += OnNewDay;

        if (EconomyService.Instance != null)
        {
            EconomyService.Instance.OnMoneyChanged += OnMoneyChanged;
            EconomyService.Instance.OnCumulativeRevenueChanged += OnRevenueChanged;
        }

        RefreshUI(force: true);
    }

    void OnDestroy()
    {
        if (GameClock.Instance != null)
            GameClock.Instance.OnNewDay -= OnNewDay;

        if (EconomyService.Instance != null)
        {
            EconomyService.Instance.OnMoneyChanged -= OnMoneyChanged;
            EconomyService.Instance.OnCumulativeRevenueChanged -= OnRevenueChanged;
        }

        if (Instance == this)
            Instance = null;
    }

    void OnNewDay(int day)
    {
        HandleNewDay(day, forceSupply: false);
    }

    void OnMoneyChanged(int _) => RefreshUI(force: false);
    void OnRevenueChanged(long _) => RefreshUI(force: false);

    void CaptureDayBaselines()
    {
        _dayStartRevenue = EconomyService.Instance != null ? EconomyService.Instance.CumulativeRevenue : 0L;
        _dayStartMoney = EconomyService.Instance != null ? EconomyService.Instance.Money : 0;
    }

    void HandleNewDay(int day, bool forceSupply)
    {
        CaptureDayBaselines();

        if (day >= 2 && day <= finalPlannedDay)
            TryGrantDailySupply(day, forceSupply);

        RefreshUI(force: true);
    }

    public bool SimulateNewDayForValidation(int day)
    {
        if (GameClock.Instance != null)
            GameClock.Instance.ForceSet(8f, day, "LongPlayProgressionValidator");

        int before = CountSellableItems();
        HandleNewDay(day, forceSupply: true);
        int after = CountSellableItems();

        return day < 2 || after > before;
    }

    public void RestoreSavedSession(int lastSupplyDay, long dayStartRevenue, int dayStartMoney)
    {
        _lastSupplyDay = Mathf.Max(0, lastSupplyDay);
        _dayStartRevenue = Math.Max(0L, dayStartRevenue);
        _dayStartMoney = Mathf.Max(0, dayStartMoney);
        _restoredFromSave = true;
        _lastSupplyResult = _lastSupplyDay > 0
            ? $"Restored NPC producer delivery state through Day {_lastSupplyDay}."
            : "NPC producer delivery pending.";
        RefreshUI(force: true);
    }

    public void WriteSaveFields(SaveData data)
    {
        if (data == null) return;

        data.longPlayLastSupplyDay = _lastSupplyDay;
        data.longPlayDayStartRevenue = _dayStartRevenue;
        data.longPlayDayStartMoney = _dayStartMoney;
    }

    void TryGrantDailySupply(int day, bool force)
    {
        if (!enableDailyNpcSupply) return;
        if (!force && _lastSupplyDay >= day) return;

        DayPlan plan = GetPlan(day);
        if (plan == null || plan.supplies == null || plan.supplies.Count == 0)
        {
            _lastSupplyResult = $"Day {day}: no producer delivery plan registered.";
            _lastSupplyDay = day;
            return;
        }

        int deliveredUnits = 0;
        int spent = 0;
        int skipped = 0;
        var summary = new List<string>();

        foreach (var supply in plan.supplies)
        {
            if (supply == null || string.IsNullOrEmpty(supply.resourcePath) || supply.count <= 0)
                continue;

            Item item = Resources.Load<Item>(supply.resourcePath);
            if (item == null)
            {
                skipped++;
                summary.Add($"missing:{supply.resourcePath}");
                continue;
            }

            int count = Mathf.Max(1, supply.count);
            int unitPrice = Mathf.Max(1, Mathf.RoundToInt(item.basePrice * Mathf.Max(0.1f, plan.buyPriceMultiplier)));
            int totalCost = unitPrice * count;

            if (EconomyService.Instance != null
                && !EconomyService.Instance.TrySpend(totalCost, $"LongPlay NPC buy-in Day {day}: {item.itemName} x{count}"))
            {
                skipped++;
                summary.Add($"{item.itemName} held: low cash");
                continue;
            }

            var instance = new ItemInstance(item, count)
            {
                quality = Mathf.Clamp(1f + 0.01f * day, 1f, 1.15f),
                currentPrice = item.basePrice
            };

            bool added = Inventory.instance != null && Inventory.instance.AddInstance(instance);
            if (!added)
            {
                if (EconomyService.Instance != null)
                    EconomyService.Instance.TryModifyMoney(totalCost, $"LongPlay buy-in refund Day {day}: inventory full");

                skipped++;
                summary.Add($"{item.itemName} held: inventory full");
                continue;
            }

            deliveredUnits += count;
            spent += totalCost;
            summary.Add($"{item.itemName} x{count}");
        }

        _lastSupplyDay = day;
        if (deliveredUnits > 0)
        {
            _lastSupplyResult =
                $"Day {day}: producer delivery {deliveredUnits} units / buy-in {spent}G / {string.Join(", ", summary)}";
        }
        else
        {
            _lastSupplyResult =
                $"Day {day}: delivery blocked or held ({skipped}) / {string.Join(", ", summary)}";
        }

        Debug.Log($"[LongPlay] {_lastSupplyResult}");
    }

    void RefreshUI(bool force)
    {
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        if (!force && day == _lastDisplayedDay && objectiveText == null && supplyText == null)
            return;

        _lastDisplayedDay = day;
        DayPlan plan = GetPlan(day);

        if (objectiveText != null)
        {
            string title = plan != null ? plan.title : ResolveLongTermTitle(day);
            string objective = plan != null
                ? plan.objective
                : "Repeat stock planning, price review, customer response, processing, and growth decisions.";
            string focus = plan != null
                ? plan.managementFocus
                : "Manager focus: balance inventory turnover, customer demand, and tier goals.";
            long target = plan != null ? plan.revenueTarget : ResolveLongTermRevenueTarget(day);
            long revenue = EconomyService.Instance != null ? EconomyService.Instance.CumulativeRevenue : 0L;
            long todayRevenue = Math.Max(0L, revenue - _dayStartRevenue);
            long remaining = Math.Max(0L, target - revenue);

            objectiveText.text =
                $"Long Play Day {day} - {title}\n" +
                $"{objective}\n" +
                $"Today revenue {todayRevenue:N0}G / total {revenue:N0}G / next target {remaining:N0}G left\n" +
                focus;
        }

        if (supplyText != null)
            supplyText.text = _lastSupplyResult;
    }

    string ResolveLongTermTitle(int day)
    {
        if (day <= 1) return "Operations onboarding";
        if (day <= 7) return "Week 1 operations stabilization";
        if (day <= 14) return "Week 2 expansion preparation";
        if (day <= 30) return "Month 1 town growth";
        return "Long-term economy operation";
    }

    long ResolveLongTermRevenueTarget(int day)
    {
        if (day <= 7) return 300 + 120L * day;
        if (day <= 14) return 1500 + 250L * (day - 7);
        if (day <= 30) return 3500 + 400L * (day - 14);
        return 10000 + 750L * (day - 30);
    }

    DayPlan GetPlan(int day)
    {
        EnsurePlans();
        foreach (var plan in _plans)
            if (plan.day == day) return plan;

        return null;
    }

    void EnsurePlans()
    {
        if (_plans.Count > 0) return;

        _plans.Add(new DayPlan
        {
            day = 1,
            title = "First operation day",
            objective = "Complete the first route: talk, stock, price, watch customer response, audit, and save.",
            managementFocus = "Focus: understand the first visible reverse supply-chain loop.",
            revenueTarget = 150,
            buyPriceMultiplier = 0.55f
        });

        _plans.Add(new DayPlan
        {
            day = 2,
            title = "Producer intake",
            objective = "Buy a small producer delivery and compare at least two stocked products.",
            managementFocus = "Focus: move NPC-produced resources into the shop economy.",
            revenueTarget = 300,
            buyPriceMultiplier = 0.50f,
            supplies = new List<SupplyEntry>
            {
                new SupplyEntry("Items/Item_Wheat", 4),
                new SupplyEntry("Items/Item_Fish", 2)
            }
        });

        _plans.Add(new DayPlan
        {
            day = 3,
            title = "Price experiment",
            objective = "Buy wood and ore, then compare fair, high, and low price reactions.",
            managementFocus = "Focus: find the balance between conversion chance and margin.",
            revenueTarget = 520,
            buyPriceMultiplier = 0.52f,
            supplies = new List<SupplyEntry>
            {
                new SupplyEntry("Items/Item_Wood", 5),
                new SupplyEntry("Items/Item_Ore", 3)
            }
        });

        _plans.Add(new DayPlan
        {
            day = 4,
            title = "Processing value check",
            objective = "Compare raw resource sales with processed goods and decide which chain deserves investment.",
            managementFocus = "Focus: move from raw sales toward a processing chain.",
            revenueTarget = 780,
            buyPriceMultiplier = 0.55f,
            supplies = new List<SupplyEntry>
            {
                new SupplyEntry("Items/Item_Carrot", 4),
                new SupplyEntry("Items/Item_Plank", 2)
            }
        });

        _plans.Add(new DayPlan
        {
            day = 5,
            title = "Relationship and hiring preparation",
            objective = "Use customer response and producer intake to identify future worker or specialist needs.",
            managementFocus = "Focus: treat NPCs as economic agents and hiring candidates, not decoration.",
            revenueTarget = 1050,
            buyPriceMultiplier = 0.57f,
            supplies = new List<SupplyEntry>
            {
                new SupplyEntry("Items/Item_Fish", 3),
                new SupplyEntry("Items/Item_Wheat", 3)
            }
        });

        _plans.Add(new DayPlan
        {
            day = 6,
            title = "Operations pressure",
            objective = "Compare missing inventory, slow movers, and high-value products to choose next supply priority.",
            managementFocus = "Focus: manage inventory turnover and tier goals together.",
            revenueTarget = 1350,
            buyPriceMultiplier = 0.60f,
            supplies = new List<SupplyEntry>
            {
                new SupplyEntry("Items/Item_IronBar", 2),
                new SupplyEntry("Items/Item_BreadLoaf", 2)
            }
        });

        _plans.Add(new DayPlan
        {
            day = 7,
            title = "Weekly audit preparation",
            objective = "Review cumulative revenue, reputation, stock state, and what Week 2 expansion should unlock.",
            managementFocus = "Focus: close Week 1 and prepare the next tier of management choices.",
            revenueTarget = 1700,
            buyPriceMultiplier = 0.62f,
            supplies = new List<SupplyEntry>
            {
                new SupplyEntry("Items/Item_Plank", 3),
                new SupplyEntry("Items/Item_Ore", 3),
                new SupplyEntry("Items/Item_Fish", 2)
            }
        });
    }

    int CountSellableItems()
    {
        int count = 0;
        if (Inventory.instance == null) return count;

        CountSellable(Inventory.instance.slots, ref count);
        if (Inventory.instance.hotbar != null)
            CountSellable(Inventory.instance.hotbar.slots, ref count);
        return count;
    }

    void CountSellable(List<InventorySlot> slots, ref int count)
    {
        if (slots == null) return;

        foreach (var slot in slots)
        {
            if (slot == null || slot.IsEmpty || slot.item == null) continue;
            if (slot.item.category != ItemCategory.Tool && slot.item.toolType == ToolType.None)
                count += slot.count;
        }
    }

    void BuildUI()
    {
        var canvasGo = new GameObject("LongPlayProgressionCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 54;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("LongPlayPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)panel.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -112f);
        rt.sizeDelta = new Vector2(590f, 128f);

        var bg = panel.GetComponent<Image>();
        bg.color = new Color(0.03f, 0.04f, 0.035f, 0.58f);
        bg.raycastTarget = false;

        objectiveText = CreateText(panel.transform, "LongPlayObjectiveText",
            new Vector2(14f, -10f), new Vector2(562f, 82f), 15f, FontStyles.Bold);
        objectiveText.alignment = TextAlignmentOptions.TopLeft;
        objectiveText.color = new Color(0.95f, 0.98f, 0.90f, 1f);

        supplyText = CreateText(panel.transform, "LongPlaySupplyText",
            new Vector2(14f, -92f), new Vector2(562f, 28f), 12f, FontStyles.Normal);
        supplyText.alignment = TextAlignmentOptions.TopLeft;
        supplyText.color = new Color(0.72f, 0.92f, 0.78f, 1f);
    }

    TextMeshProUGUI CreateText(Transform parent, string name, Vector2 topLeftOffset, Vector2 size, float fontSize, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = topLeftOffset;
        rt.sizeDelta = size;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }
}
