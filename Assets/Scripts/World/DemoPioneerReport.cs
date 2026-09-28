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
    }
    static T[] SceneObjects<T>() where T : Component => Resources.FindObjectsOfTypeAll<T>()
        .Where(c => c.gameObject.scene.IsValid()).ToArray();
    void OnDestroy()
    {
        if (loop != null) loop.OpeningShopClosed -= OnClose;
        SalesLogManager.OnSaleRecorded -= OnSale;
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
        var data = new DemoPioneerReportData {
            settlement = (shop ? 9 : 0) + Mathf.Clamp(tents, 0, 2) * 8,
            commerce = Mathf.Min(10, sales * 5) + Mathf.Min(10, revenue / 10) + Mathf.Min(5, Mathf.Max(0, distinctSalePrices) + rejections),
            development = (root != DemoSpecialization.None ? 12 : 0) + (upgradedTool ? 8 : 0) + (npcGift ? 5 : 0),
            exploration = Mathf.Min(25, biomes.Length * 3 + actions.Length * 4),
            sales = sales, revenue = revenue, rejections = rejections, specialization = root,
            npcToolGift = npcGift, visitedBiomes = biomes, activities = actions
        };
        data.total = data.settlement + data.commerce + data.development + data.exploration;
        data.rankKey = data.total >= 85 ? "rank.s" : data.total >= 70 ? "rank.a" : data.total >= 55 ? "rank.b" : data.total >= 40 ? "rank.c" : "rank.d";
        var comments = new List<string> { shop && tents >= 2 ? "settlement.established" : "settlement.in_progress",
            sales > 0 ? "commerce.first_customers" : "commerce.no_sale" };
        if (npcGift) comments.Add("development.shared_tools");
        else if (root != DemoSpecialization.None) comments.Add("development.chosen_path");
        if (actions.Contains("fish") && actions.Contains("bug")) comments.Add("exploration.curiosity");
        else if (rejections > sales) comments.Add("commerce.price_experiment");
        else if (biomes.Length >= 3) comments.Add("exploration.traveller");
        data.commentKeys = comments.Take(4).ToArray();
        return data;
    }
}
