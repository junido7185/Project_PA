using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class DemoPioneerReportData
{
    public int settlement, commerce, development, exploration, total;
    public int sales, revenue, rejections;
    public string rankKey;
    public string[] commentKeys, visitedBiomes, activities;
    public DemoSpecialization specialization;
    public bool npcToolGift;
}

// Read-only observer/evaluator. No grants, clock edits, economy operations or save writes.
public sealed class DemoPioneerReport : MonoBehaviour
{
    public DemoPioneerReportData LastReport { get; private set; }
    public event Action<DemoPioneerReportData> ReportAvailable;
    DemoSettlementController settlement;
    WorldGridService grid;
    Inventory inventory;
    DayNightShopLoopController loop;
    readonly HashSet<WorldBiomeType> visited = new HashSet<WorldBiomeType>();
    readonly HashSet<string> activities = new HashSet<string>(StringComparer.Ordinal);
    readonly HashSet<int> salePrices = new HashSet<int>();
    Gatherable[] trees;
    MiningSpot[] stones;
    BugCritter[] bugs;
    DaytimeStockPrepPoint[] fishing;
    bool craftedUpgrade;
    float nextSample;

    public void Configure(DemoSettlementController progress, WorldGridService world, Inventory player)
    {
        settlement = progress; grid = world; inventory = player; loop = DayNightShopLoopController.Instance;
        trees = SceneObjects<Gatherable>(); stones = SceneObjects<MiningSpot>(); bugs = SceneObjects<BugCritter>();
        fishing = SceneObjects<DaytimeStockPrepPoint>().Where(p => p.activityId == "shore-forage").ToArray();
        if (loop != null) loop.OpeningShopClosed += OnClose;
        SalesLogManager.OnSaleRecorded += OnSale;
        inventory.onItemChangedCallback += ObserveInventory;
        CraftingService.Crafted += OnCrafted;
    }
    void OnCrafted(RecipeData recipe) { if (LastReport == null && recipe != null) activities.Add("craft"); }
    static T[] SceneObjects<T>() where T : Component => Resources.FindObjectsOfTypeAll<T>()
        .Where(c => c.gameObject.scene.IsValid()).ToArray();
    void OnDestroy()
    {
        if (loop != null) loop.OpeningShopClosed -= OnClose;
        SalesLogManager.OnSaleRecorded -= OnSale;
        CraftingService.Crafted -= OnCrafted;
        if (inventory != null) inventory.onItemChangedCallback -= ObserveInventory;
    }
    void OnSale(SaleRecord record)
    { if (record != null && loop != null && loop.IsShopOpenForCustomers && LastReport == null) salePrices.Add(record.price); }
    void ObserveInventory()
    {
        if (LastReport != null) return;
        var catalog = DemoPlaceableCatalog.Load();
        if (catalog?.upgrades != null) craftedUpgrade |= catalog.upgrades.Any(u => inventory.CountItems(u.tool) > 0);
    }
    void Update()
    {
        if (LastReport != null || grid == null || inventory == null || Time.unscaledTime < nextSample) return;
        nextSample = Time.unscaledTime + .75f; Sample();
    }
    void Sample()
    {
        var world = WorldPersistenceService.Instance?.ActiveGeneratedWorld;
        if (world != null && grid.WorldToCell(inventory.transform.position, out var coordinate) && world.TryGetCell(coordinate, out var cell)) visited.Add(cell.Biome);
        if (trees.Any(t => t != null && t.IsDirectWorld && t.DirectDepleted)) activities.Add("forestry");
        if (stones.Any(t => t != null && t.IsDirectWorld && t.DirectDepleted)) activities.Add("mining");
        if (bugs.Any(b => b != null && b.Captured)) activities.Add("bug");
        if (fishing.Any(f => f != null && f.IsCollected)) activities.Add("fish");
    }
    // 마감 전 저장용: 지금까지 관찰한 방문 지형·활동. rankKey가 비어 있어 결말 Report와 구분된다.
    public DemoPioneerReportData CaptureProgress()
    {
        if (LastReport != null) return LastReport;
        if (grid != null && inventory != null) Sample();
        return new DemoPioneerReportData
        {
            specialization = settlement != null ? settlement.SelectedRoot : DemoSpecialization.None,
            visitedBiomes = visited.Select(v => v.ToString()).OrderBy(s => s, StringComparer.Ordinal).ToArray(),
            activities = activities.OrderBy(s => s, StringComparer.Ordinal).ToArray()
        };
    }

    // Continue: 결말 Report면 관찰을 멈추고, 진행 기록이면 이전 방문/활동을 이어서 센다.
    public void RestoreProgress(DemoPioneerReportData data, bool final)
    {
        if (data == null) return;
        if (final) { LastReport = data; return; }
        foreach (var biome in data.visitedBiomes ?? Array.Empty<string>())
            if (Enum.TryParse(biome, out WorldBiomeType parsed)) visited.Add(parsed);
        foreach (var action in data.activities ?? Array.Empty<string>())
            if (!string.IsNullOrEmpty(action)) activities.Add(action);
    }

    void OnClose(int sales, int revenue, int rejections)
    {
        if (LastReport != null) return;
        Sample(); ObserveInventory();
        LastReport = Evaluate(settlement.HasShopBase, settlement.TentCount, settlement.SelectedRoot,
            craftedUpgrade, settlement.Residents.Any(r => r.HasReceivedTool), sales, revenue, rejections,
            salePrices.Count, visited.Select(v => v.ToString()).ToArray(), activities.ToArray());
        settlement.ShowPioneerReport(LastReport);
        ReportAvailable?.Invoke(LastReport);
    }
    public static DemoPioneerReportData Evaluate(bool shop, int tents, DemoSpecialization root,
        bool upgradedTool, bool npcGift, int sales, int revenue, int rejections, int distinctSalePrices,
        string[] biomes, string[] actions)
    {
        biomes = (biomes ?? Array.Empty<string>()).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();
        actions = (actions ?? Array.Empty<string>()).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();
        sales = Mathf.Max(0, sales); revenue = Mathf.Max(0, revenue); rejections = Mathf.Max(0, rejections);
        // Canon v2 §20.1 배점: Settlement 25 / Commerce 30 / Development 25 / Exploration 20.
        // Commerce는 실제 영업량에 비례한다(판매 8건·매출 120G에서 만점). 2건·24G는 후한 점수가 되지 않는다.
        // Exploration은 필수 경로(벌목·채광)보다 선택 행동(낚시·곤충·추가 지형)에 더 준다.
        int optionalFinds = (actions.Contains("fish") ? 4 : 0) + (actions.Contains("bug") ? 4 : 0);
        int requiredGathering = (actions.Contains("forestry") ? 2 : 0) + (actions.Contains("mining") ? 2 : 0);
        var data = new DemoPioneerReportData {
            settlement = (shop ? 9 : 0) + Mathf.Clamp(tents, 0, 2) * 8,
            commerce = Mathf.Min(12, Mathf.RoundToInt(sales * 1.5f)) + Mathf.Min(12, revenue / 10) +
                       Mathf.Min(6, Mathf.Max(0, distinctSalePrices) * 2 + Mathf.Min(2, rejections)),
            development = (root != DemoSpecialization.None ? 12 : 0) + (upgradedTool ? 8 : 0) + (npcGift ? 5 : 0),
            exploration = Mathf.Min(20, Mathf.Min(8, Mathf.Max(0, biomes.Length - 1) * 2) + requiredGathering + optionalFinds),
            sales = sales, revenue = revenue, rejections = rejections, specialization = root,
            npcToolGift = npcGift, visitedBiomes = biomes, activities = actions
        };
        data.total = data.settlement + data.commerce + data.development + data.exploration;
        // Canon v2 §20.2: Rank는 실패 판정이 아니므로 C가 최저다.
        data.rankKey = data.total >= 90 ? "rank.s" : data.total >= 75 ? "rank.a" : data.total >= 60 ? "rank.b" : "rank.c";
        var comments = new List<string> { shop && tents >= 2 ? "settlement.established" : "settlement.in_progress",
            sales > 0 ? "commerce.first_customers" : "commerce.no_sale" };
        // 성장 댓글은 한 줄: 동료 도구 지급 > 직접 만든 물건(P8) > 전문 분야 선택. 배점(§20.1)은 그대로다.
        if (npcGift) comments.Add("development.shared_tools");
        else if (actions.Contains("craft")) comments.Add("development.crafted_goods");
        else if (root != DemoSpecialization.None) comments.Add("development.chosen_path");
        if (actions.Contains("fish") && actions.Contains("bug")) comments.Add("exploration.curiosity");
        else if (rejections > sales) comments.Add("commerce.price_experiment");
        else if (biomes.Length >= 3) comments.Add("exploration.traveller");
        data.commentKeys = comments.Take(4).ToArray();
        return data;
    }
}
