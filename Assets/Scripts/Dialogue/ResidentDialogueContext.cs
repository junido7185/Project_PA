using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 주민 대화 한 번에 쓰는 "지금 상황" 스냅샷. 기존 권위(정착·시계·가방·판매 기록·동행 일과)를 읽기만 한다.
//
// 설계 의도:
// - 대화는 드물게 일어나므로 말을 걸 때 한 번 모아 읽는다(매 프레임 폴링/이벤트 구독 없음).
// - 모든 싱글턴은 없을 수 있다(검증용 빈 씬, 다른 데모 경로). 없으면 해당 신호를 끈 채로 둔다.
// - 여기서는 아무 것도 쓰지 않는다: 저장·경제·배치·재고 상태를 바꾸지 않는다.
public enum ResidentDayPart { Dawn, Morning, Midday, Afternoon, Dusk, Evening, Night }
public enum ResidentPlace { Unknown, Harbor, Shore, Meadow, Forest, Highland, NearShop, Home }
public enum ResidentItemKind
{
    None, Fish, Wood, Ore, Butterfly, Crop, Goods, ShopKit, TentKit, WorkbenchKit, DisplayStand, Furniture,
    Axe, Pickaxe, ImprovedPickaxe, Net, Rod, Phone, FarmTool, Other
}

public sealed class ResidentDialogueContext
{
    // ---- 화자 ----
    public ResidentRole Role;
    public ResidentVoice Voice;
    public string Name;
    public bool IsCompanion;
    public int Familiarity;                 // 0~5 (친밀도 단계 또는 대화 누적에서 파생)
    public DemoCompanionRoutine.Activity? Activity;
    public bool HasHome, ReceivedTool, ProducerWorking;
    public string PartnerName;
    public ResidentRole PartnerRole = ResidentRole.Neighbor;
    public DemoCompanionRoutine.Activity? PartnerActivity;
    public bool PartnerHasHome, PartnerWorking, PartnerBatchReady, PartnerReceivedTool;

    // ---- 시간 ----
    public int Day = 1;
    public float Hour = 9f;
    public ResidentDayPart Part = ResidentDayPart.Morning;

    // ---- 데모 정착 진행 ----
    public bool DemoActive;
    public bool SupplyPending;
    public bool ShopPlaced, Established, NightReady, ShopOpen, SessionClosed, WorkbenchPlaced, ForgePlaced;
    public int Tents, StockedSlots;
    public DemoSpecialization Root;
    public bool RootPending;
    public DemoPioneerReportData Report;

    // ---- 일반 낮/밤 루프 ----
    public bool LoopActive;
    public PADayNightPhase Phase;
    public bool CanOpenShop;
    public int SalesToday, RevenueToday;
    public int YesterdaySales, YesterdayRevenue, YesterdayRejections;
    public string YesterdayTopItem;

    // ---- 플레이어 ----
    public Item Held;
    public ResidentItemKind HeldKind;
    public readonly int[] Counts = new int[System.Enum.GetValues(typeof(ResidentItemKind)).Length];
    public readonly Dictionary<ResidentItemKind, Item> Sample = new Dictionary<ResidentItemKind, Item>();
    public bool BagFull;
    public ResidentPlace Place;
    public string BrokenTool, BrokenToolKey;

    public int Count(ResidentItemKind kind) => Counts[(int)kind];
    public int SaleGoods => Count(ResidentItemKind.Fish) + Count(ResidentItemKind.Wood) + Count(ResidentItemKind.Ore) +
                            Count(ResidentItemKind.Butterfly) + Count(ResidentItemKind.Crop) + Count(ResidentItemKind.Goods);

    // "나무 4개, 광석 2개" — 진열 안내에 쓰는 실제 보유 상품 요약(많은 순 최대 2종).
    public string GoodsSummary()
    {
        var parts = new[] { ResidentItemKind.Wood, ResidentItemKind.Ore, ResidentItemKind.Fish, ResidentItemKind.Butterfly,
                ResidentItemKind.Crop, ResidentItemKind.Goods }
            .Where(k => Count(k) > 0).OrderByDescending(k => Count(k)).Take(2)
            .Select(k => $"{KindLabel(k)} {Count(k)}{Unit(k)}").ToArray();
        return parts.Length == 0 ? null : string.Join(", ", parts);
    }

    public string KindLabel(ResidentItemKind kind)
    {
        if (Sample.TryGetValue(kind, out var item) && item != null && kind != ResidentItemKind.Goods && kind != ResidentItemKind.Crop)
            return ResidentNames.ItemName(item);
        return kind switch
        {
            ResidentItemKind.Fish => "생선", ResidentItemKind.Wood => "나무", ResidentItemKind.Ore => "광석",
            ResidentItemKind.Butterfly => "나비", ResidentItemKind.Crop => "열매", ResidentItemKind.Goods => "상품",
            _ => "물건"
        };
    }

    static string Unit(ResidentItemKind kind) => kind == ResidentItemKind.Fish || kind == ResidentItemKind.Butterfly ? "마리" : "개";

    // -------- 수집 --------

    static WorldGridService _grid;

    public static ResidentDialogueContext Capture(NpcDialogue npc, GameObject player, Item notedHeld, int fallbackFamiliarity)
    {
        var ctx = new ResidentDialogueContext();
        GameObject go = npc.gameObject;
        NpcProfile profile = npc.ResolveProfile();
        ctx.Role = ResidentNames.ResolveRole(go, profile, npc.dialogueData);
        ctx.Voice = ResidentNames.VoiceOf(profile);
        ctx.Name = ResidentNames.ShortName(go, profile, npc.dialogueData);
        ctx.Familiarity = !string.IsNullOrEmpty(npc.friendshipId) && FriendshipService.Instance != null
            ? Mathf.Max(FriendshipService.Instance.GetLevel(npc.friendshipId), fallbackFamiliarity)
            : fallbackFamiliarity;

        ReadClock(ctx);
        ReadCompanion(ctx, go, profile);
        ReadSettlement(ctx);
        ReadLoopAndSales(ctx);
        ReadInventory(ctx, player, notedHeld);
        ReadPlace(ctx, go);
        ReadBrokenTool(ctx);
        return ctx;
    }

    static void ReadClock(ResidentDialogueContext ctx)
    {
        var clock = GameClock.Instance;
        ctx.Day = clock != null ? clock.CurrentDay : 1;
        ctx.Hour = clock != null ? clock.CurrentHour : 9f;
        float h = ctx.Hour;
        ctx.Part = h < 5f ? ResidentDayPart.Night : h < 8f ? ResidentDayPart.Dawn : h < 11f ? ResidentDayPart.Morning :
            h < 14f ? ResidentDayPart.Midday : h < 17f ? ResidentDayPart.Afternoon : h < 20f ? ResidentDayPart.Dusk :
            h < 23f ? ResidentDayPart.Evening : ResidentDayPart.Night;
    }

    static void ReadCompanion(ResidentDialogueContext ctx, GameObject go, NpcProfile profile)
    {
        var resident = go.GetComponent<DemoResident>();
        string ownId = resident != null ? resident.CompanionId : null;
        var selected = DemoRouteController.SelectedCompanions ?? System.Array.Empty<DepartureCompanionSelection.Candidate>();
        if (string.IsNullOrEmpty(ownId))
            ownId = selected.FirstOrDefault(c => c != null && profile != null && c.profile == profile && go.name.EndsWith(c.id))?.id;
        ctx.IsCompanion = !string.IsNullOrEmpty(ownId);
        if (!ctx.IsCompanion) return;

        var routine = go.GetComponent<DemoCompanionRoutine>();
        if (routine != null) ctx.Activity = routine.Current;
        if (resident != null)
        {
            ctx.HasHome = !string.IsNullOrEmpty(resident.TentId);
            ctx.ReceivedTool = resident.HasReceivedTool;
            var producer = resident.Worksite != null ? resident.Worksite.Producer : null;
            ctx.ProducerWorking = producer != null && producer.StarterWorking;
        }

        var partner = selected.FirstOrDefault(c => c != null && c.id != ownId);
        if (partner == null) return;
        ctx.PartnerName = ResidentNames.CompanionName(partner.id);
        ctx.PartnerRole = ResidentNames.RoleFromKey(partner.id);
        var root = FirstDayWorldPresentation.Instance != null ? FirstDayWorldPresentation.Instance.transform.Find("Companion_" + partner.id) : null;
        if (root == null) return;
        var partnerRoutine = root.GetComponent<DemoCompanionRoutine>();
        if (partnerRoutine != null) ctx.PartnerActivity = partnerRoutine.Current;
        var partnerResident = root.GetComponent<DemoResident>();
        if (partnerResident == null) return;
        ctx.PartnerHasHome = !string.IsNullOrEmpty(partnerResident.TentId);
        ctx.PartnerReceivedTool = partnerResident.HasReceivedTool;
        var partnerProducer = partnerResident.Worksite != null ? partnerResident.Worksite.Producer : null;
        ctx.PartnerWorking = partnerProducer != null && partnerProducer.StarterWorking;
        ctx.PartnerBatchReady = partnerProducer != null && partnerProducer.StarterBatchReady;
    }

    static void ReadSettlement(ResidentDialogueContext ctx)
    {
        var settlement = DemoSettlementController.Instance;
        var loop = DayNightShopLoopController.Instance;
        ctx.DemoActive = settlement != null;
        if (settlement == null) return;
        var supply = FirstDayWorldPresentation.Instance != null ? FirstDayWorldPresentation.Instance.Supply : null;
        ctx.SupplyPending = supply != null && !supply.Collected && supply.gameObject.activeInHierarchy;
        ctx.ShopPlaced = settlement.HasShopBase;
        ctx.Tents = settlement.TentCount;
        ctx.Established = settlement.Established;
        ctx.Root = settlement.SelectedRoot;
        ctx.RootPending = settlement.Established && settlement.SelectedRoot == DemoSpecialization.None && settlement.LicensePoints > 0;
        ctx.NightReady = settlement.NightReady;
        ctx.WorkbenchPlaced = settlement.PlacedWorkbench != null;
        ctx.ForgePlaced = settlement.HasPlaced("field-forge");
        ctx.Report = settlement.PioneerReport;
        ctx.SessionClosed = loop != null && loop.OpeningSessionCompleted;
        ctx.ShopOpen = loop != null && loop.IsShopOpenForCustomers && settlement.NightReady && !loop.OpeningSessionCompleted;
        var shop = settlement.OperatingShop;
        if (shop != null && shop.Slots != null)
            ctx.StockedSlots = shop.Slots.Count(s => s != null && !s.IsEmpty);
    }

    static void ReadLoopAndSales(ResidentDialogueContext ctx)
    {
        var loop = DayNightShopLoopController.Instance;
        ctx.LoopActive = loop != null && !ctx.DemoActive;
        if (loop != null)
        {
            ctx.Phase = loop.CurrentPhase;
            ctx.CanOpenShop = loop.CanPlayerOpenShop;
            if (!ctx.DemoActive) ctx.ShopOpen = loop.IsShopOpenForCustomers && loop.CurrentPhase == PADayNightPhase.ShopOpen;
        }
        var log = SalesLogManager.Instance;
        if (log == null) return;
        var records = log.GetRecent(100);
        ctx.SalesToday = log.GetDailyDecisionStats(ctx.Day).purchases;
        ctx.RevenueToday = records.Where(r => r != null && r.gameDay == ctx.Day).Sum(r => r.price);
        if (ctx.Day <= 1) return;
        var yesterday = records.Where(r => r != null && r.gameDay == ctx.Day - 1).ToList();
        var stats = log.GetDailyDecisionStats(ctx.Day - 1);
        ctx.YesterdaySales = Mathf.Max(stats.purchases, yesterday.Count);
        ctx.YesterdayRevenue = yesterday.Sum(r => r.price);
        ctx.YesterdayRejections = stats.rejections;
        string top = yesterday.GroupBy(r => r.itemName).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
        if (!string.IsNullOrEmpty(top))
        {
            var item = ItemRegistry.Instance != null ? ItemRegistry.Instance.Find(0, top) : null;
            ctx.YesterdayTopItem = item != null ? ResidentNames.ItemName(item) : top;
        }
    }

    static void ReadInventory(ResidentDialogueContext ctx, GameObject player, Item notedHeld)
    {
        Inventory inventory = player != null ? player.GetComponent<Inventory>() : null;
        if (inventory == null) inventory = Inventory.instance;
        if (player == null && inventory != null) player = inventory.gameObject;
        ctx.Held = player != null ? EquipmentSystem.CurrentHeld(player) : null;
        // 대화는 빈손으로만 시작된다. 직전에 들고 다가온 물건(NpcDialogue.NoteHeldItem)을 손에 든 것으로 본다.
        if (ctx.Held == null) ctx.Held = notedHeld;
        ctx.HeldKind = Classify(ctx.Held);
        if (inventory == null) return;
        int empty = 0;
        void Add(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty) { empty++; return; }
            var kind = Classify(slot.item);
            ctx.Counts[(int)kind] += slot.count;
            if (!ctx.Sample.ContainsKey(kind)) ctx.Sample[kind] = slot.item;
        }
        if (inventory.slots != null) foreach (var slot in inventory.slots) Add(slot);
        if (inventory.hotbar != null && inventory.hotbar.slots != null) foreach (var slot in inventory.hotbar.slots) Add(slot);
        ctx.BagFull = empty == 0 && (inventory.slots != null && inventory.slots.Count > 0);
    }

    static void ReadPlace(ResidentDialogueContext ctx, GameObject go)
    {
        Vector3 at = go.transform.position;
        var resident = go.GetComponent<DemoResident>();
        if (resident != null && resident.Home != null && Flat(at, resident.Home.position) < 4f) { ctx.Place = ResidentPlace.Home; return; }
        var shop = DemoSettlementController.Instance != null ? DemoSettlementController.Instance.OperatingShop : null;
        if (shop != null && Flat(at, shop.transform.position) < 12f) { ctx.Place = ResidentPlace.NearShop; return; }
        var world = FirstDayWorldPresentation.Instance;
        if (world != null && Flat(at, world.Harbor) < 16f) { ctx.Place = ResidentPlace.Harbor; return; }
        var generated = WorldPersistenceService.Instance != null ? WorldPersistenceService.Instance.ActiveGeneratedWorld : null;
        if (generated == null) return;
        if (_grid == null) _grid = Object.FindFirstObjectByType<WorldGridService>();
        if (_grid == null || !_grid.WorldToCell(at, out var cell) || !generated.TryGetCell(cell, out var biomeCell)) return;
        ctx.Place = biomeCell.Biome switch
        {
            WorldBiomeType.Coast => ResidentPlace.Shore, WorldBiomeType.Meadow => ResidentPlace.Meadow,
            WorldBiomeType.Forest => ResidentPlace.Forest, WorldBiomeType.Highland => ResidentPlace.Highland,
            _ => ResidentPlace.Unknown
        };
    }

    // 도구 파손은 ToolDurability 가 휴대폰 기록(DispatchLog)에 남기는 알림으로 관찰한다(새 이벤트 경로를 만들지 않음).
    const string BrokeMarker = "닳아서 부서졌어요";
    static void ReadBrokenTool(ResidentDialogueContext ctx)
    {
        var log = FirstDayWorldPresentation.DispatchLog;
        if (log == null) return;
        foreach (string entry in log.Take(4))
        {
            if (string.IsNullOrEmpty(entry)) continue;
            int marker = entry.IndexOf(BrokeMarker, System.StringComparison.Ordinal);
            if (marker < 0) continue;
            int start = entry.IndexOf("  ", System.StringComparison.Ordinal);
            start = start >= 0 && start < marker ? start + 2 : 0;
            string name = entry.Substring(start, marker - start).Trim();
            if (name.EndsWith("가") || name.EndsWith("이")) name = name.Substring(0, name.Length - 1).Trim();
            if (string.IsNullOrEmpty(name)) continue;
            ctx.BrokenTool = name;
            ctx.BrokenToolKey = entry;
            return;
        }
    }

    static float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

    // -------- 아이템 분류 --------

    public static ResidentItemKind Classify(Item item)
    {
        if (item == null) return ResidentItemKind.None;
        var entry = DemoPlaceableCatalog.Load()?.Find(item);
        if (entry != null)
            return entry.kind switch
            {
                DemoPlaceableKind.ShopBase => ResidentItemKind.ShopKit, DemoPlaceableKind.ResidentTent => ResidentItemKind.TentKit,
                DemoPlaceableKind.Workbench => ResidentItemKind.WorkbenchKit, DemoPlaceableKind.DisplayStand => ResidentItemKind.DisplayStand,
                _ => ResidentItemKind.Furniture
            };
        if (item.id == 2010) return ResidentItemKind.Phone;
        switch (item.toolType)
        {
            case ToolType.Axe: return ResidentItemKind.Axe;
            case ToolType.Pickaxe: return item.id == 2022 ? ResidentItemKind.ImprovedPickaxe : ResidentItemKind.Pickaxe;
            case ToolType.Net: return ResidentItemKind.Net;
            case ToolType.FishingRod: return ResidentItemKind.Rod;
            case ToolType.Hoe: case ToolType.Seed: return ResidentItemKind.FarmTool;
        }
        if (item.category == ItemCategory.Tool) return ResidentItemKind.Other;
        if (item.category != ItemCategory.Raw) return ResidentItemKind.Goods;
        string name = (item.itemName ?? string.Empty).ToLowerInvariant();
        if (item.id == 8 || item.id >= 3001 && item.id <= 3099 || Any(name, "fish", "생선", "도미", "옐로탱", "참치")) return ResidentItemKind.Fish;
        if (item.id == 2004 || Any(name, "butterfly", "나비")) return ResidentItemKind.Butterfly;
        if (Any(name, "wood", "원목", "나무", "목재")) return ResidentItemKind.Wood;
        if (Any(name, "ore", "광석", "돌")) return ResidentItemKind.Ore;
        if (Any(name, "wheat", "carrot", "fruit", "apple", "밀", "당근", "감자", "열매", "사과")) return ResidentItemKind.Crop;
        return ResidentItemKind.Goods;
    }

    static bool Any(string text, params string[] tokens)
    {
        foreach (string token in tokens) if (text.Contains(token)) return true;
        return false;
    }
}
