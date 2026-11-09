using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PA_ContentFirstShopNightValidator
{
    const string Key = "PA.Content002.Active";
    const string Output = "Logs/Content/CONTENT002/Runtime";
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    static Task _task;
    static double _started;
    static bool _failed;

    static PA_ContentFirstShopNightValidator()
    {
        if (SessionState.GetBool(Key, false)) Subscribe();
    }

    [MenuItem("Project PA/Validation/Run Content First Shop Night Validation")]
    public static void Run()
    {
        Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".Done", false);
        SessionState.SetBool(Key + ".Failed", false);
        SessionState.SetBool(Key + ".Audit", false);
        Subscribe();
        EditorSceneManager.OpenScene("Assets/Scenes/WorldSandbox.unity", OpenSceneMode.Single);
        if (!Application.isBatchMode)
            typeof(PA_ContentOpeningValidator).GetMethod("ConfigureExistingGameViewSize", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        EditorApplication.EnterPlaymode();
    }

    public static void AuditShopAsset()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/B01_MarketStall.prefab");
        foreach (ShopSlot slot in prefab.GetComponentsInChildren<ShopSlot>(true))
        {
            Debug.Log($"[CONTENT-002] ASSET_SLOT {slot.name} position={slot.transform.position} scale={slot.transform.lossyScale} active={slot.gameObject.activeSelf} colliders={slot.GetComponentsInChildren<Collider>(true).Length}");
            foreach (Collider collider in slot.GetComponentsInChildren<Collider>(true))
                Debug.Log($"[CONTENT-002] ASSET_COLLIDER {collider.name} enabled={collider.enabled} trigger={collider.isTrigger} bounds={collider.bounds}");
        }
    }

    public static void RunInteractionAudit()
    {
        Run();
        SessionState.SetBool(Key + ".Audit", true);
    }

    static void Subscribe()
    {
        _started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged -= OnMode;
        EditorApplication.playModeStateChanged += OnMode;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    static void OnLog(string message, string trace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        _failed = true;
        SessionState.SetBool(Key + ".Failed", true);
    }

    static void OnMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode) _started = EditorApplication.timeSinceStartup;
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key + ".Done", false)) return;
        bool failed = SessionState.GetBool(Key + ".Failed", false);
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        Debug.Log(failed ? "[CONTENT-002] VALIDATION_FAIL" : SessionState.GetBool(Key + ".Audit", false)
            ? "[CONTENT-002] INTERACTION_AUDIT_COMPLETE" : "[CONTENT-002] VALIDATION_PASS");
        if (Application.isBatchMode || Environment.GetCommandLineArgs().Contains("-executeMethod"))
            EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || SessionState.GetBool(Key + ".Done", false)) return;
        if (_task == null && EditorApplication.isPlaying && WorldAlphaPlayableController.Instance?.IsReady == true)
            _task = Checks();
        if (_task != null && _task.IsCompleted)
        {
            if (_task.IsFaulted)
            {
                _failed = true;
                Debug.LogError("[CONTENT-002] " + _task.Exception?.GetBaseException());
            }
            SessionState.SetBool(Key + ".Failed", _failed);
            SessionState.SetBool(Key + ".Done", true);
            EditorApplication.ExitPlaymode();
        }
        else if (EditorApplication.timeSinceStartup - _started > 180)
        {
            SessionState.SetBool(Key, false);
            Debug.LogError("[CONTENT-002] Runtime timeout.");
            if (Application.isBatchMode || Environment.GetCommandLineArgs().Contains("-executeMethod"))
                EditorApplication.Exit(1);
        }
    }

    static async Task Checks()
    {
        WorldAlphaPlayableController alpha = WorldAlphaPlayableController.Instance;
        Check(alpha.BeginNewGame(), "campaign starts from the real new-game entry");
        if (SessionState.GetBool(Key + ".Audit", false))
        {
            AuditRuntimeApproaches(alpha);
            return;
        }
        GameClock clock = GameClock.Instance;
        // 시계만 고정하는 fixture. 실제 간판·초대·FSM·확률·거래는 그대로 실행한다.
        clock.enabled = false;
        var loop = DayNightShopLoopController.Instance;
        var arrival = CustomerArrivalController.Instance;
        Check(arrival != null, "existing arrival authority is present");
        arrival.maxTouristsPerOpening = 0; // 동일 보리 거래의 재고·친밀도 검증에서 다른 고객만 제외.
        CampaignFirstShopNightController night = alpha.FirstNight;
        var repository = new LocalJsonSaveRepository(Path.GetFullPath(Output));
        SaveManager save = SaveManager.instance;
        typeof(SaveManager).GetField("_repository", PrivateInstance).SetValue(save, repository);
        GameObject bori = alpha.Opening.Bori;
        int boriInstance = bori.GetInstanceID();
        var talk = bori.GetComponent<NpcDialogue>();
        Interact(alpha, talk.transform, talk);
        DialogueUI.instance?.Hide();
        var forest = alpha.Adapter.FindDaytimeActivity("forest-forage");
        Interact(alpha, forest.transform, forest);
        await Task.Delay(150);
        Item carrot = Resources.Load<Item>("Items/Item_Carrot");
        Check(alpha.Opening.OpeningComplete && Inventory.instance.CountItems(carrot) == 2, "real greeting and gathering provide first stock");
        int beforeBulkAttempt = EconomyService.Instance.Money;
        alpha.Adapter.RuntimeShop.Interact(alpha.Adapter.PlayerRoot);
        Check(Inventory.instance.CountItems(carrot) == 2 && EconomyService.Instance.Money == beforeBulkAttempt &&
              !alpha.Adapter.RuntimeShop.GetInteractPrompt().Contains("디버그"), "World shop Space cannot consume inventory through debug bulk sale");
        Check(!loop.TryOpenShop() && arrival.TryInviteWave(1) == 0 && !night.HasObservedDecision, "day gate prevents invitations and fabricated decisions");
        ShopSlot slot = alpha.Adapter.RuntimeShopSlots.First(s => s != null && s.IsEmpty);
        Interact(alpha, slot.transform, slot);
        Check(!slot.IsEmpty && slot.currentItem.data == carrot && Inventory.instance.CountItems(carrot) == 1, "Space moves one actual carrot from bag to shelf");
        SetPrice(alpha, slot, carrot.basePrice * 5);
        Check(night.Capture().priceConfirmedDay == 1 && night.Capture().confirmedItemId == carrot.id, "price confirmation records the real displayed item");
        await save.SaveGameAsync();
        await save.LoadGameAsync();
        Check(!night.HasObservedDecision && !night.HasFirstSale && slot.currentItem.count == 1 &&
              slot.EffectiveDisplayPrice == carrot.basePrice * 5, "pre-opening load preserves stock and price without inventing a decision");

        loop.SimulatePhaseForValidation(18f, 1);
        var sign = alpha.Adapter.ShopSignTarget.GetComponent<ShopOpenSign>();
        int money = EconomyService.Instance.Money;
        Interact(alpha, sign.transform, sign);
        Check(loop.PlayerHasOpenedShopToday, "the player opens using the real sign");
        await Until(() => night.HasObservedDecision, "existing arrival and NpcController produce the first decision");
        arrival.enabled = false;
        Check(bori.GetInstanceID() == boriInstance && night.Capture().firstDecisionResidentId == "bori", "the daytime Bori is the actual first shopper");
        Check(!night.Capture().firstDecisionWantedToBuy && !night.HasFirstSale, "natural high-price decision is a rejection, separate from sale");
        Check(!slot.IsEmpty && slot.currentItem.count == 1 && EconomyService.Instance.Money == money &&
              FriendshipService.Instance.GetPoints("bori") == 2, "rejection keeps stock, money and friendship unchanged");
        Check(SalesLogManager.Instance.GetDailyDecisionStats(1).rejections == 1, "the existing decision read model records the rejection");
        Check(night.Summary.Contains("보리") && night.Summary.Contains("가격이 조금 부담"), "the player HUD retains the actual price rejection reason");
        DialogueUI.instance?.Hide();
        await PA_SafeGameViewCapture.CaptureAsync(Path.GetFullPath(Output + "/FirstDecision.png"), Camera.main, null);

        loop.SimulatePhaseForValidation(23f, 1);
        Interact(alpha, sign.transform, sign);
        Check(clock.CurrentDay == 2 && night.Complete && !night.HasFirstSale &&
              night.Capture().firstSettlementRevenue == 0, "zero-sale settlement completes A02 and continues to Day 2");
        await save.SaveGameAsync();
        await save.LoadGameAsync();
        Check(night.Complete && !night.HasFirstSale && night.Capture().firstDecisionResidentId == "bori", "no-sale first night remains complete across load");
        Check(night.Summary.Contains("가격이 조금 부담"), "load preserves the actual first customer's reason");

        SetPrice(alpha, slot, 1);
        loop.SimulatePhaseForValidation(18f, 2);
        Interact(alpha, sign.transform, sign);
        arrival.enabled = true;
        await Until(() => night.HasFirstSale, "the same Bori makes a natural affordable-price purchase");
        arrival.enabled = false;
        Check(slot.IsEmpty && EconomyService.Instance.Money == money + 1 &&
              EconomyService.Instance.CumulativeRevenue == 1, "successful sale consumes the displayed unit and deposits exactly its price");
        Check(FriendshipService.Instance.GetPoints("bori") == 7, "only the real purchase adds +5 to the same relationship");
        Check(night.Capture().firstSaleDay == 2 && night.Capture().firstSaleAmount == 1 &&
              night.Capture().firstSettlementRevenue == 0, "later first sale does not rewrite the zero-sale first settlement");
        var world = new SaveData();
        VillageCultureVisualController.Instance.WriteSaveFields(world);
        Check(world.villageCultureHasPendingChange && world.villageCulturePendingBuyerName == "보리", "only the successful sale schedules a village response for Bori");
        await save.SaveGameAsync();
        string v14 = await repository.LoadAsync("savegame");
        await save.LoadGameAsync();
        await save.LoadGameAsync();
        Check(night.Complete && night.HasFirstSale && EconomyService.Instance.Money == money + 1 &&
              FriendshipService.Instance.GetPoints("bori") == 7 && Inventory.instance.CountItems(carrot) == 1 &&
              Object.FindObjectsByType<NpcDialogue>(FindObjectsSortMode.None).Count(n => n.friendshipId == "bori") == 1,
            "repeat load preserves one Bori and never replays the sale or relationship reward");

        SaveData old = JsonUtility.FromJson<SaveData>(v14);
        old.version = 13;
        old.campaign.firstNight = null;
        await repository.SaveAsync("savegame", JsonUtility.ToJson(old));
        await save.LoadGameAsync();
        Check(alpha.Opening.OpeningComplete && !night.HasObservedDecision && !night.HasFirstSale &&
              night.Capture().priceConfirmedDay == 0, "v13 migration preserves A01 without inferring first-night evidence from money or friendship");
        await repository.SaveAsync("savegame", v14);
        await save.LoadGameAsync();
        Check(night.Complete && night.HasFirstSale, "v14 restores the authored first-night record");
        Debug.Log("[CONTENT-002] CHECKS_PASS sameBori=true realPrice=true naturalRejection=true zeroSaleNextDay=true naturalPurchase=true saveMigration=true");
    }

    static void SetPrice(WorldAlphaPlayableController alpha, ShopSlot slot, int price)
    {
        Interact(alpha, slot.transform, slot);
        ShopPriceUI ui = ShopPriceUI.instance;
        Check(ui != null && ui.IsOpen, "Space opens the real price panel");
        // 패널의 가격 조절/확정 경로를 구동한다. 거래 가격 필드를 직접 쓰지 않는다.
        int pending = (int)typeof(ShopPriceUI).GetField("_pendingPrice", PrivateInstance).GetValue(ui);
        typeof(ShopPriceUI).GetMethod("AdjustPrice", PrivateInstance).Invoke(ui, new object[] { price - pending });
        typeof(ShopPriceUI).GetMethod("OnConfirm", PrivateInstance).Invoke(ui, null);
        Check(slot.EffectiveDisplayPrice == price && !ui.IsOpen, "confirmed price is applied by existing UI authority");
    }

    static void AuditRuntimeApproaches(WorldAlphaPlayableController alpha)
    {
        var lines = new System.Collections.Generic.List<string>();
        var player = alpha.Adapter.PlayerRoot;
        var guard = player.GetComponent<WorldPlayerTraversalGuard>();
        var interaction = player.GetComponent<PlayerInteraction>();
        ShopSlot slot = alpha.Adapter.RuntimeShopSlots[0];
        lines.Add($"slot={slot.name} position={slot.transform.position} shop={alpha.Adapter.RuntimeShop.transform.position}");
        foreach (Collider collider in slot.GetComponentsInChildren<Collider>(true))
            lines.Add($"collider={collider.name} enabled={collider.enabled} active={collider.gameObject.activeInHierarchy} bounds={collider.bounds}");
        for (int radiusIndex = 1; radiusIndex <= 6; radiusIndex++)
        for (int angle = 0; angle < 16; angle++)
        {
            Vector3 candidate = slot.transform.position + Quaternion.Euler(0, angle * 22.5f, 0) * Vector3.forward * (radiusIndex * 0.5f);
            if (!alpha.Grid.WorldToCell(candidate, out Vector2Int cell) || !alpha.Grid.CellToWorld(cell, out Vector3 ground)) continue;
            candidate.y = ground.y + 0.05f;
            if (!guard.TryTeleportTo(candidate))
            {
                lines.Add($"radius={radiusIndex * 0.5f} angle={angle * 22.5f} cell={cell} ground={ground.y} walkable=false");
                continue;
            }
            Vector3 facing = slot.transform.position - player.transform.position;
            facing.y = 0;
            player.transform.rotation = Quaternion.LookRotation(facing);
            Physics.SyncTransforms();
            object[] args = { null, null };
            bool found = (bool)typeof(PlayerInteraction).GetMethod("TryFindInteractable", PrivateInstance).Invoke(interaction, args);
            lines.Add($"radius={radiusIndex * 0.5f} angle={angle * 22.5f} cell={cell} player={player.transform.position} found={found} selected={(args[0] as Component)?.name} exact={ReferenceEquals(args[0], slot)}");
        }
        File.WriteAllLines(Output + "/InteractionAudit.txt", lines);
        Debug.Log("[CONTENT-002] Runtime geometry audit written; no stocking or purchase assertions executed.");
    }

    static void Interact(WorldAlphaPlayableController alpha, Transform target, IInteractable expected)
    {
        GameObject player = alpha.Adapter.PlayerRoot;
        var guard = player.GetComponent<WorldPlayerTraversalGuard>();
        var interaction = player.GetComponent<PlayerInteraction>();
        for (int i = 0; i < 8; i++)
        {
            Vector3 direction = Quaternion.Euler(0, i * 45f, 0) * Vector3.forward;
            if (!guard.TryTeleportTo(target.position + direction * 1.2f + Vector3.up * 0.05f)) continue;
            Vector3 facing = target.position - player.transform.position;
            facing.y = 0;
            player.transform.rotation = Quaternion.LookRotation(facing);
            Physics.SyncTransforms();
            object[] args = { null, null };
            bool found = (bool)typeof(PlayerInteraction).GetMethod("TryFindInteractable", PrivateInstance).Invoke(interaction, args);
            if (!found || !ReferenceEquals(args[0], expected)) continue;
            typeof(PlayerInteraction).GetMethod("TryInteract", PrivateInstance).Invoke(interaction, null);
            return;
        }
        throw new InvalidOperationException("No walkable Space approach to " + target.name);
    }

    static async Task Until(Func<bool> predicate, string message)
    {
        float start = Time.realtimeSinceStartup;
        while (!predicate() && Time.realtimeSinceStartup - start < 40f) await Task.Delay(100);
        Check(predicate(), message);
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log("[CONTENT-002] CHECK_OK " + message);
    }
}
