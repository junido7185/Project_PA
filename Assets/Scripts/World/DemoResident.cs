using UnityEngine;

// Selected NPC identity and home binding; the tent itself never acquires a profession.
public sealed class DemoResident : MonoBehaviour, IInteractable
{
    public string CompanionId { get; private set; }
    public NpcProfile Profile { get; private set; }
    public string TentId { get; private set; }
    public Transform Home { get; private set; }
    public bool HasReceivedTool { get; private set; }
    const int GiftFriendship = 5;
    public WorksiteBinding Worksite { get; private set; }
    public void Configure(string id, NpcProfile profile) { CompanionId = id; Profile = profile; }
    public void Claim(WorldPlacedBuildingRuntime tent, WorldGridService grid)
    {
        if (!string.IsNullOrEmpty(TentId) && TentId != tent.InstanceId) return;
        TentId = tent.InstanceId;
        if (Home == null) { Home = new GameObject("ResidentHome").transform; Home.SetParent(tent.GameObject.transform, false); }
        if (grid.CellToWorld(tent.Entrance, out var position)) Home.position = position;
        var schedule = GetComponent<NpcScheduleController>();
        if (schedule != null) schedule.homePoint = Home;
    }
    public void BindWork(DemoToolUpgrade data, Transform activity)
    {
        if (Worksite != null || data == null || data.production == null || activity == null ||
            !CompanionId.StartsWith(data.companionPrefix, System.StringComparison.Ordinal)) return;
        Worksite = GetComponent<WorksiteBinding>() ?? gameObject.AddComponent<WorksiteBinding>();
        Worksite.BindDemoActivity(CompanionId, Profile, data.production, data.specialty, activity, Home);
        Worksite.Producer.ConfigureStarterSession();
        Worksite.Producer.ConfigureDemoTool(data.starterTool);
    }
    // PlayerInteraction은 손에 든 물건이 Accepts일 때만 동행을 대상으로 고른다(아니면 "손 비우기" 안내). 도구 전달과 선물 둘 다 받는다.
    public bool Accepts(Item item) => AcceptsTool(item) || CanGift(item);

    public bool AcceptsTool(Item item)
    {
        var data = DemoPlaceableCatalog.Load()?.Upgrade(item);
        return data != null && Worksite != null && Worksite.Assigned && Worksite.Producer.specialty == data.specialty;
    }
    public void Interact(GameObject player)
    {
        Item held = EquipmentSystem.CurrentHeld(player);
        if (held != null)
        {
            if (!AcceptsTool(held)) { if (CanGift(held)) Gift(player, held); return; }
            var data = DemoPlaceableCatalog.Load().Upgrade(held);
            if (Worksite.Producer.TryReceiveDemoTool(player.GetComponent<Inventory>(), data))
            {
                HasReceivedTool = true; Worksite.Producer.StartStarterWork();
                // 도구 선물은 친밀도에도 남는다(대화창 이름표 하트).
                var talk = GetComponent<NpcDialogue>();
                if (talk != null && !string.IsNullOrEmpty(talk.friendshipId))
                    FriendshipService.Instance?.AddPoints(talk.friendshipId, GiftFriendship, "도구 선물");
                // P7: 동행의 반응과 생산 효과, 돌아온 이전 도구를 한 카드에 보인다.
                var routine = GetComponent<DemoCompanionRoutine>();
                string speaker = routine != null && !string.IsNullOrEmpty(routine.DisplayName) ? routine.DisplayName + ": " : "";
                FirstDayWorldPresentation.Toast($"{speaker}이 도구면 훨씬 빨리 할 수 있어요!\n{ItemDisplayName.For(held)} 전달 · 작업 속도 ×{data.npcEfficiency:0.#} · " +
                    $"이전 {(data.starterTool != null ? ItemDisplayName.For(data.starterTool) : "도구")}는 핫바로 돌아왔어요.");
            }
            return;
        }
        if (Worksite?.Producer.StarterBatchReady == true)
        {
            if (!Worksite.Producer.TryClaimStarterBatch(player.GetComponent<Inventory>()))
                FirstDayWorldPresentation.Toast("가방 공간을 비워주세요.");
            else if (Worksite.Producer.productionData != null) DemoHotbarPreference.Prefer(Worksite.Producer.productionData.producedItem);
            return;
        }
        GetComponent<NpcDialogue>()?.Interact(player);
    }
    // 2026-11-03 선물(레퍼런스 생활 게임 공통 고리): 도구가 아닌 판매 가능한 물건을 들고 [E] → 1개를 주고 친밀도를 올린다.
    // 하루 한 번(FriendshipService의 날짜 기록 "gift:<id>"에 남겨 기존 친밀도 저장과 함께 이어진다). 좋아하는 물건은 더 크게 오른다. 판매·재고 권위는 기존 Inventory.RemoveItems 하나.
    public const int GiftLiked = 8, GiftLuxury = 5, GiftPlain = 3;
    public int GiftsGiven { get; private set; }
    string GiftKey { get { var talk = GetComponent<NpcDialogue>(); return talk != null && !string.IsNullOrEmpty(talk.friendshipId) ? "gift:" + talk.friendshipId : null; } }
    static int Today => GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
    bool GiftedToday => GiftKey != null && FriendshipService.Instance != null &&
        FriendshipService.Instance.GetAllLastDialogueDays().TryGetValue(GiftKey, out int day) && day == Today;

    public static bool CanGift(Item item) => item != null && item.toolType == ToolType.None && item.category != ItemCategory.Tool &&
        item.basePrice > 0 && !WorldPlaceableKitCatalog.TryKit(item, out _, out _);

    // 역할별로 반기는 물건: 광부 = 동전·화석·주괴, 벌목꾼 = 판재·공예품·토기, 농부 = 조리한 음식·작물.
    public bool Likes(Item item)
    {
        var role = ResidentNames.RoleFromKey(CompanionId);
        switch (role)
        {
            case ResidentRole.Miner: return item.id == 2040 || item.id == 2041 || item.id == 6 || item.id == 2029;
            case ResidentRole.Lumberjack: return item.id == 7 || item.id == 2025 || item.id == 2037 || item.id == 2042;
            case ResidentRole.Farmer: return item.id == 10 || item.id == 2032 || item.id == 2033 || item.id == 1 || item.id == 2 || item.id == 5;
        }
        return item.category == ItemCategory.Luxury;
    }

    void Gift(GameObject player, Item held)
    {
        var talk = GetComponent<NpcDialogue>();
        string name = ItemDisplayName.For(held);
        if (GiftedToday) { talk?.SayToPlayer("오늘은 벌써 선물을 받았어요. 마음만 받을게요!"); return; }
        var inventory = player != null ? player.GetComponent<Inventory>() : null;
        if (inventory == null || inventory.CountItems(held) <= 0) return;
        inventory.RemoveItems(held, 1);
        if (GiftKey != null) FriendshipService.Instance?.ForceSetLastDialogueDay(GiftKey, Today);
        GiftsGiven++;
        bool liked = Likes(held);
        int points = liked ? GiftLiked : held.category == ItemCategory.Luxury ? GiftLuxury : GiftPlain;
        _inGift = true; _thanksLine = null;
        try
        {
            if (talk != null && !string.IsNullOrEmpty(talk.friendshipId))
                FriendshipService.Instance?.AddPoints(talk.friendshipId, points, "선물");
        }
        finally { _inGift = false; }
        GatherFeedback.Pickup();
        string line = liked ? $"{name}{ResidentNames.Josa(name, "이다").Replace("다", "라니")}! 제가 정말 좋아하는 거예요. 고마워요!"
            : held.category == ItemCategory.Luxury ? $"이렇게 좋은 걸요? {name}, 소중히 둘게요." : $"{name}, 고마워요. 잘 쓸게요.";
        talk?.SayToPlayer(_thanksLine != null ? line + " " + _thanksLine : line);
        _thanksLine = null;
    }

    // 2026-11-03 친밀도 답례(GIFTS.md): 하트 단계가 오르는 순간 동행이 역할에 맞는 물건을 건넨다.
    // FriendshipService.OnLevelChanged는 점수가 실제로 오를 때만 울리고 불러오기(ForceSetPoints)에서는 울리지 않아 중복 지급이 없다.
    public int ThanksGiven { get; private set; }
    bool _thanksHooked, _inGift;
    string _thanksLine;

    void OnEnable() => HookThanks();
    void Start() => HookThanks();
    void OnDisable()
    {
        if (_thanksHooked && FriendshipService.Instance != null) FriendshipService.Instance.OnLevelChanged -= OnFriendshipLevel;
        _thanksHooked = false;
    }

    void HookThanks()
    {
        if (_thanksHooked || FriendshipService.Instance == null) return;
        FriendshipService.Instance.OnLevelChanged += OnFriendshipLevel;
        _thanksHooked = true;
    }

    void OnFriendshipLevel(string id, int oldLevel, int newLevel)
    {
        var talk = GetComponent<NpcDialogue>();
        if (talk == null || id != talk.friendshipId || newLevel <= oldLevel) return;
        var (path, count) = ThanksFor(newLevel);
        var item = Resources.Load<Item>(path);
        var inventory = Inventory.instance;
        if (item == null || inventory == null) return;
        if (!inventory.TryReceiveToHotbar(item, count) && !inventory.AddInstance(new ItemInstance(item, count)))
            GatherFeedback.SpawnDrop(item, transform.position, inventory.transform.position, null, count);
        ThanksGiven++;
        string name = ItemDisplayName.For(item);
        string thanks = $"우리 사이가 한 뼘 가까워졌네요. 이거 받아 줘요, {name} ×{count}.";
        if (_inGift) _thanksLine = thanks; else talk.SayToPlayer(thanks);
        FirstDayWorldPresentation.Toast($"친밀도 {newLevel}단계!\n{name} ×{count} 받았어요");
    }

    // 역할별 답례: 1단계는 일하며 모은 재료, 2단계부터는 한 단계 위 물건.
    (string path, int count) ThanksFor(int level)
    {
        switch (ResidentNames.RoleFromKey(CompanionId))
        {
            case ResidentRole.Miner: return level <= 1 ? ("Items/Item_CopperBar", 2) : ("Items/Item_IronBar", 1);
            case ResidentRole.Lumberjack: return level <= 1 ? ("Items/Item_Plank", 3) : ("DemoStructure/Item_SimpleFurniture", 1);
            case ResidentRole.Farmer: return level <= 1 ? ("Items/Item_10_GrilledFish", 2) : ("Items/Item_FishSkewer", 1);
        }
        return ("Items/Item_Plank", 2);
    }

    public string GetInteractPrompt()
    {
        Item held = Inventory.instance != null ? EquipmentSystem.CurrentHeld(Inventory.instance.gameObject) : null;
        // 손에 든 것을 대화 기억에 남긴다: [X]로 손을 비우고 말을 걸어도 "그 설계도요…"처럼 이어 말할 수 있다.
        var talk = GetComponent<NpcDialogue>();
        if (talk != null) talk.NoteHeldItem(held);
        if (AcceptsTool(held))
            return "도구 전달 / 기존 도구 돌려받기";
        if (CanGift(held))
        {
            var giftRoutine = GetComponent<DemoCompanionRoutine>();
            string who = giftRoutine != null && !string.IsNullOrEmpty(giftRoutine.DisplayName) ? giftRoutine.DisplayName : ResidentNames.ShortName(gameObject, Profile);
            return GiftedToday ? $"{who} · 오늘 선물 완료" : $"{who}에게 {ItemDisplayName.For(held)} 선물하기";
        }
        if (Worksite?.Producer.StarterBatchReady == true) return "생산물 받기";
        // P7: 지금 무엇을 하는 동행인지 이름과 함께 보인다. 이름이 아직 없으면 내부 ID 대신 캐논 이름을 쓴다.
        var routine = GetComponent<DemoCompanionRoutine>();
        string name = routine != null && !string.IsNullOrEmpty(routine.DisplayName) ? routine.DisplayName : ResidentNames.ShortName(gameObject, Profile);
        return routine != null ? $"{name} · {routine.Status} · 대화" : $"{name} · 대화";
    }
}
