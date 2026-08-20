#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PA_Beta007VillageResponseValidator
{
    const string ScenePath = "Assets/Scenes/WorldSandbox.unity";
    const string ActiveKey = "PA.BETA007.Active";
    const string FailedKey = "PA.BETA007.Failed";
    const string ConsoleErrorKey = "PA.BETA007.ConsoleErrors";
    const string FrameKey = "PA.BETA007.Frames";
    const string StageKey = "PA.BETA007.Stage";

    static WorldAlphaPlayableController _alpha;
    static WorldGameplayAdapterService _adapter;
    static VillageCultureVisualController _culture;
    static NpcCandidateData _farmerCandidate;
    static GameObject _farmer;
    static ShopSlot _saleSlot;
    static int _salesBefore;
    static int _moneyBeforeSale;
    static float _stageStarted;

    static PA_Beta007VillageResponseValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        Subscribe();
        if (EditorApplication.isPlaying)
        {
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
    }

    [MenuItem("Project PA/Beta/BETA-007/Validate Village Response Loop")]
    public static void RunBeta007Validation() => RunInternal();

    public static void RunBeta007ValidationBatch() => RunInternal();

    static void RunInternal()
    {
        try
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetInt(ConsoleErrorKey, 0);
            SetStage(0);
            Subscribe();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Require(scene.IsValid() && scene.isLoaded && !scene.isDirty,
                "WorldSandbox opens saved and clean");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    static void Subscribe()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        Application.logMessageReceived += OnLogMessage;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SetStage(0);
            EditorApplication.update -= ValidateRuntime;
            EditorApplication.update += ValidateRuntime;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            Finish();
        }
    }

    static void ValidateRuntime()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
        int frames = SessionState.GetInt(FrameKey, 0) + 1;
        SessionState.SetInt(FrameKey, frames);
        int stage = SessionState.GetInt(StageKey, 0);

        try
        {
            ResolveRuntime();
            bool ready = _adapter != null && _adapter.IsReady && _adapter.ProductionFacilitiesBound &&
                         _alpha != null && _alpha.IsReady && _culture != null &&
                         HiringService.Instance != null && SalesLogManager.Instance != null &&
                         DayNightShopLoopController.Instance != null && DialogueUI.instance != null;
            if (!ready && frames < 900) return;
            if (!ready)
                throw new TimeoutException("WorldSandbox village-response authorities did not initialize. " +
                                           BuildReadinessDiagnostic());

            if (stage == 0)
            {
                Require(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11,
                    $"D3D11 is active ({SystemInfo.graphicsDeviceType})");
                Require(_alpha.BeginNewGame() && _alpha.PlayerFacingHudVisible,
                    "the real player session exposes the WorldSandbox HUD");
                Require(_adapter.RuntimeSewing != null &&
                        _adapter.RuntimeSewing.workbenchType == WorkbenchType.SewingTable,
                    "B08 gives the Tailor an existing functional facility anchor");
                Require(_adapter.ResidentSpawnAnchors.Count >= 1 &&
                        _adapter.ResidentSpawnAnchorsReady,
                    "generated Start cells provide resident NavMesh spawn anchors");

                _culture.ResetForValidation();
                _farmerCandidate = Resources.Load<NpcCandidateData>("Candidates/Candidate_Farmer");
                Require(_farmerCandidate != null && _farmerCandidate.specialty == NpcSpecialty.Farmer,
                    "the existing Farmer candidate is available");
                EconomyService.Instance.ForceSet(_farmerCandidate.hireCost,
                    "BETA-007 exact hiring fixture");
                Require(HiringService.Instance.TryHire(_farmerCandidate, out _farmer,
                            out string hireReason) && _farmer != null,
                    $"HiringService completes the actual Farmer hire ({hireReason})");
                SetStage(1);
                return;
            }

            if (stage == 1)
            {
                if (frames < 4) return;
                ResolveFarmer();
                ProducerNpcController producer = _farmer.GetComponent<ProducerNpcController>();
                NpcScheduleController schedule = _farmer.GetComponent<NpcScheduleController>();
                NpcDialogue dialogue = _farmer.GetComponent<NpcDialogue>();
                NavMeshAgent agent = _farmer.GetComponent<NavMeshAgent>();
                Transform farmAnchor = _adapter.GetResidentRoleAnchor(NpcSpecialty.Farmer);
                DaytimeStockPrepPoint dropOff = _adapter.FindDaytimeActivity("producer-dropbox");
                Require(producer != null && schedule != null && dialogue != null && agent != null,
                    "the hired Farmer keeps producer, schedule, dialogue and navigation authorities");
                Require(agent.isOnNavMesh && farmAnchor != null && producer.workSpot == farmAnchor,
                    "the Farmer is on NavMesh and bound to the generated farm role anchor");
                Require(dropOff != null && producer.dropOffPoint == dropOff.transform,
                    "the Farmer delivery route uses the generated producer drop-off");
                Require(schedule.homePoint != null && _adapter.ResidentSpawnAnchors.Contains(schedule.homePoint),
                    "the Farmer schedule returns to a generated resident home anchor");
                Require(!_culture.TryBuildResidentResponse(_farmer, out _),
                    "no resident response exists before an earned next-day change");

                Item wheat = Resources.Load<Item>("Items/Item_Wheat");
                Require(wheat != null && producer.productionData != null &&
                        producer.productionData.producedItem == wheat,
                    "the Farmer's real ProductionData identifies Wheat");
                ClearInventoryAndDisplays();
                Require(_adapter.PlayerInventory.AddInstance(new ItemInstance(wheat, 1)
                    {
                        quality = 1.05f,
                        currentPrice = wheat.basePrice
                    }), "Wheat enters the existing Inventory authority");
                _saleSlot = _adapter.RuntimeShopSlots.First();
                _saleSlot.Interact(_adapter.PlayerRoot);
                Require(!_saleSlot.IsEmpty && _saleSlot.currentItem.data == wheat,
                    "ShopSlot.Interact stocks the actual Wheat item");
                _saleSlot.displayPrice = 1;
                _saleSlot.RefreshDisplay();

                Require(_adapter.TryOpenShopForNight(1, out string openReason),
                    $"the existing night-shop gate opens ({openReason})");
                _salesBefore = SalesLogManager.Instance.GetRecent(100).Count;
                _moneyBeforeSale = EconomyService.Instance.Money;
                Require(_saleSlot.TryPurchaseByNpc("BETA007_FARMER_CUSTOMER", out int paid) && paid == 1,
                    "ShopSlot completes a real Wheat sale through the existing transaction path");
                Require(_saleSlot.IsEmpty && EconomyService.Instance.Money == _moneyBeforeSale + paid,
                    "the real sale atomically empties stock and deposits revenue");
                SaleRecord sale = SalesLogManager.Instance.GetRecent(1).FirstOrDefault();
                Require(SalesLogManager.Instance.GetRecent(100).Count == _salesBefore + 1 &&
                        sale != null && sale.itemName == wheat.itemName &&
                        sale.category == ItemCategory.Raw.ToString() && sale.gameDay == 1,
                    "SalesLog records the exact Wheat, Raw category and sale day");
                Require(_culture.HasPendingChange && _culture.PendingSaleDay == 1 &&
                        _culture.PendingItemName == wheat.itemName && !_culture.VisualActive,
                    "the sale event immediately creates a same-day pending response without early activation");
                Require(_alpha.VillageResponseSummary.Contains("Wheat") &&
                        _alpha.VillageResponseSummary.Contains("다음 날"),
                    "the visible HUD explains the pending sale-to-next-day causality");

                DayNightShopLoopController.Instance.SimulatePhaseForValidation(23f, 1);
                Require(DayNightShopLoopController.Instance.TryStartNextDay(),
                    "the actual settlement transition starts the next morning");
                SetStage(2);
                return;
            }

            if (stage == 2)
            {
                if (frames < 2) return;
                ResolveFarmer();
                Transform farmAnchor = _adapter.GetResidentRoleAnchor(NpcSpecialty.Farmer);
                Require(GameClock.Instance.CurrentDay == 2 &&
                        DayNightShopLoopController.Instance.CurrentPhase == PADayNightPhase.DayPreparation,
                    "GameClock and day loop enter the real Day 2 preparation phase");
                Require(_culture.HasActiveCategory && _culture.ActiveCategory == ItemCategory.Raw &&
                        !_culture.HasPendingChange && _culture.ActiveItemName == "Wheat" &&
                        _culture.ActiveSaleDay == 1 && _culture.ActiveResponseDay == 2,
                    "Day 2 activates the exact Day 1 Wheat response snapshot");
                Require(_culture.VisualRoot != null && _culture.VisualRoot.activeInHierarchy &&
                        _culture.CurrentFacilityAnchor == farmAnchor &&
                        FlatDistance(_culture.VisualRoot.transform.position, farmAnchor.position) <= 3.5f,
                    "the active Raw visual is anchored beside the generated Farmer facility");
                Require(_culture.VisualRoot.GetComponentsInChildren<Collider>(true)
                        .All(collider => collider == null || !collider.enabled || collider.isTrigger),
                    "the village response visual does not add a blocking collider");
                Require(_alpha.PlayerFacingHudVisible &&
                        _alpha.VillageResponseSummary.Contains("Day 1") &&
                        _alpha.VillageResponseSummary.Contains("Wheat") &&
                        _alpha.VillageResponseSummary.Contains("Day 2"),
                    "the player HUD persistently connects the sale day, product and response day");

                NpcDialogue dialogue = _farmer.GetComponent<NpcDialogue>();
                dialogue.Interact(_adapter.PlayerRoot);
                Require(DialogueUI.IsOpen && dialogue.LastVillageResponseDay == 2 &&
                        dialogue.LastLine.Contains("Wheat") && dialogue.LastLine.Contains("농부"),
                    "the hired Farmer explains the earned change through the real DialogueUI path");
                _stageStarted = Time.realtimeSinceStartup;
                SetStage(3);
                return;
            }

            if (stage == 3)
            {
                ResolveFarmer();
                NpcDialogue dialogue = _farmer.GetComponent<NpcDialogue>();
                TMP_Text body = DialogueUI.instance.bodyText;
                if ((body == null || !body.text.Contains("Wheat")) &&
                    Time.realtimeSinceStartup - _stageStarted < 8f)
                    return;

                Require(body != null && body.text.Contains("Wheat"),
                    "the visible dialogue body renders the exact sold product");
                Require(DialogueUI.instance.nameText != null &&
                        DialogueUI.instance.nameText.text.Contains(_farmerCandidate.profile.npcName),
                    "the visible dialogue identifies the responding resident");
                Require(SessionState.GetInt(ConsoleErrorKey, 0) == 0,
                    "blocking runtime Console Error/Exception/Assert count is 0");
                Require(!SceneManager.GetActiveScene().isDirty,
                    "BETA-007 remains runtime-only and leaves WorldSandbox scene clean");
                Debug.Log("[BETA-007] PLAY_MODE_PASS sale=true nextDay=true resident=true " +
                          "anchors=true hud=true dialogue=true console=0");
                EditorApplication.update -= ValidateRuntime;
                EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception exception)
        {
            EditorApplication.update -= ValidateRuntime;
            Fail(exception);
        }
    }

    static void ResolveRuntime()
    {
        _alpha = WorldAlphaPlayableController.Instance ??
                 UnityEngine.Object.FindFirstObjectByType<WorldAlphaPlayableController>();
        _adapter = WorldGameplayAdapterService.Instance ??
                   UnityEngine.Object.FindFirstObjectByType<WorldGameplayAdapterService>();
        _culture = VillageCultureVisualController.Instance ??
                   UnityEngine.Object.FindFirstObjectByType<VillageCultureVisualController>();
    }

    static void ResolveFarmer()
    {
        if (_farmer != null) return;
        HiringService.HiredNpcRuntimeRecord record = HiringService.Instance.GetHiredRuntimeRecords()
            .FirstOrDefault(candidate => candidate.Candidate == _farmerCandidate);
        _farmer = record.Instance;
        if (_farmer == null)
            throw new InvalidOperationException("The hired Farmer runtime instance was lost.");
    }

    static void ClearInventoryAndDisplays()
    {
        Inventory inventory = _adapter.PlayerInventory;
        foreach (InventorySlot slot in inventory.slots) slot?.Clear();
        if (inventory.hotbar != null)
            foreach (InventorySlot slot in inventory.hotbar.slots) slot?.Clear();
        inventory.RefreshAllUI();

        foreach (ShopSlot slot in _adapter.RuntimeShopSlots)
        {
            slot.currentItem = null;
            slot.displayPrice = 0;
            slot.RefreshDisplay();
        }
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    static string BuildReadinessDiagnostic()
    {
        WorldNavigationService navigation = UnityEngine.Object.FindFirstObjectByType<WorldNavigationService>();
        string navigationState = navigation == null
            ? "navigation=missing"
            : $"navigation=revision:{navigation.NavigationRevision},rebuilding:{navigation.IsRebuilding}," +
              $"pending:{navigation.PendingSectorCount},failure:{navigation.LastFailure}";
        string anchorState = _adapter == null
            ? "anchors=adapter-missing"
            : "anchors=" + string.Join(";", _adapter.ResidentSpawnAnchors.Select(anchor =>
            {
                if (anchor == null) return "null";
                bool sampled = NavMesh.SamplePosition(anchor.position, out NavMeshHit hit,
                    3f, NavMesh.AllAreas);
                return $"{anchor.name}@{anchor.position}:sampled={sampled}," +
                       $"hit={(sampled ? hit.position.ToString() : "none")}," +
                       $"flatDistance={(sampled ? FlatDistance(anchor.position, hit.position) : -1f):F2}";
            }));
        int hiringSpawnCount = HiringService.Instance?.spawnPointRotation?.Count ?? -1;
        return $"adapter={_adapter?.State.ToString() ?? "missing"}, " +
               $"residentReady={_adapter != null && _adapter.ResidentSpawnAnchorsReady}, " +
               $"{navigationState}, hiringSpawnCount={hiringSpawnCount}, {anchorState}";
    }

    static void SetStage(int stage)
    {
        SessionState.SetInt(StageKey, stage);
        SessionState.SetInt(FrameKey, 0);
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!SessionState.GetBool(ActiveKey, false) ||
            (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
        SessionState.SetInt(ConsoleErrorKey, SessionState.GetInt(ConsoleErrorKey, 0) + 1);
    }

    static void Fail(Exception exception)
    {
        SessionState.SetBool(FailedKey, true);
        Debug.LogError($"[BETA-007] FAIL {exception.Message}\n{exception}");
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else Finish();
    }

    static void Finish()
    {
        EditorApplication.update -= ValidateRuntime;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Application.logMessageReceived -= OnLogMessage;
        bool failed = SessionState.GetBool(FailedKey, false) ||
                      SessionState.GetInt(ConsoleErrorKey, 0) != 0;
        int errors = SessionState.GetInt(ConsoleErrorKey, 0);
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(FailedKey);
        SessionState.EraseInt(ConsoleErrorKey);
        SessionState.EraseInt(FrameKey);
        SessionState.EraseInt(StageKey);
        Debug.Log(failed
            ? $"[BETA-007] FINISHED_WITH_ERRORS consoleErrors={errors}"
            : "[BETA-007] FINISHED_PASS sale=true nextDay=true resident=true " +
              "anchors=true hud=true dialogue=true console=0");
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"[BETA-007] PASS {message}");
    }
}
#endif
