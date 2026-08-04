using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PA_FinalPresentationReviewer
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.FinalPresentation.Active";
    const string EnteredKey = "PA.FinalPresentation.Entered";
    const string RanKey = "PA.FinalPresentation.Ran";
    const string HadErrorKey = "PA.FinalPresentation.HadError";
    const string OutputKey = "PA.FinalPresentation.OutputDir";

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;
    static string _outputDir;
    static Task _runtimeTask;
    static ShopSlot _reviewSlot;
    static Item _reviewItem;

    static PA_FinalPresentationReviewer()
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;

        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _hadError = SessionState.GetBool(HadErrorKey, false);
        _outputDir = SessionState.GetString(OutputKey, string.Empty);
        RegisterCallbacks();

        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Final Presentation Review")]
    public static void RunFinalPresentationReview()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA Final Presentation: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        _entered = false;
        _ran = false;
        _hadError = false;
        _runtimeTask = null;
        _startedAt = EditorApplication.timeSinceStartup;
        _outputDir = CreateOutputDirectory();

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(HadErrorKey, false);
        SessionState.SetString(OutputKey, _outputDir);

        RegisterCallbacks();

        Debug.Log($"PA Final Presentation: entering Play Mode. Output={_outputDir}");
        EditorApplication.EnterPlaymode();
    }

    static void RegisterCallbacks()
    {
        Application.logMessageReceived -= OnLogMessage;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;

        Application.logMessageReceived += OnLogMessage;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
    }

    static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            _hadError = true;
            SessionState.SetBool(HadErrorKey, true);
        }
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _entered = true;
            _startedAt = EditorApplication.timeSinceStartup;
            SessionState.SetBool(EnteredKey, true);
        }

        if (state == PlayModeStateChange.EnteredEditMode
            && SessionState.GetBool(ActiveKey, false)
            && SessionState.GetBool(RanKey, false))
        {
            Finish();
        }
    }

    static void OnEditorUpdate()
    {
        double elapsed = EditorApplication.timeSinceStartup - _startedAt;

        if (!_entered)
        {
            if (elapsed > 60.0)
            {
                Debug.LogError("PA Final Presentation: timed out before entering Play Mode.");
                MarkFailedAndExit();
            }
            return;
        }

        if (!_ran && EditorApplication.isPlaying && _runtimeTask == null && elapsed > 2.5)
        {
            _runtimeTask = RunRuntimeChecksAsync();
            return;
        }

        if (!_ran && _runtimeTask != null && _runtimeTask.IsCompleted)
        {
            if (_runtimeTask.IsFaulted)
            {
                _hadError = true;
                SessionState.SetBool(HadErrorKey, true);
                Debug.LogError($"PA Final Presentation failed: {_runtimeTask.Exception?.GetBaseException()}");
            }

            _ran = true;
            SessionState.SetBool(RanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }

        if (_entered && !_ran && elapsed > 120.0)
        {
            Debug.LogError("PA Final Presentation: timed out before completing review.");
            MarkFailedAndExit();
        }
    }

    static async Task RunRuntimeChecksAsync()
    {
        PrepareRuntimeState();
        await CaptureAsync("01_market_hub_objective");
        await Task.Delay(900);

        await OpenPriceReviewAsync();
        await CaptureAsync("02_shop_price_ui");
        await Task.Delay(900);

        ShowNpcFeedbackReview();
        await CaptureAsync("03_npc_feedback_bubble");
        await Task.Delay(900);

        OpenAuditReview();
        await CaptureAsync("04_audit_app_goal");
        await Task.Delay(900);

        OpenSummaryReview();
        await CaptureAsync("05_day1_summary");
        await Task.Delay(900);

        ValidatePresentationLayout();
        ValidateCapturedFiles();
    }

    static void PrepareRuntimeState()
    {
        Time.timeScale = 1f;
        Screen.SetResolution(1920, 1080, false);

        var scenario = FindObject<PlayableDayScenarioController>("PlayableDayScenarioController");
        scenario.RestoreSavedSession("하늘", "green_bay", 0);

        PositionCameraAtPresentationMarker();
        Require(GameObject.Find("PA_MarketStall_Hub_Visual") != null, "market hub visual is present");
        Require(GameObject.Find("PA_DemoRoute_VisualMarkers") != null, "demo route markers are present");
        Require(Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None).Length > 0, "NPCs are present");
    }

    static async Task OpenPriceReviewAsync()
    {
        ClosePhoneIfOpen();

        var player = FindObject<PlayerController>("PlayerController");
        _reviewSlot = FindEmptyShopSlot();
        _reviewItem = FindSellableItem();
        SeedHotbarWith(_reviewItem);

        _reviewSlot.Interact(player.gameObject);
        Require(!_reviewSlot.IsEmpty, "shop slot stocks an item from hotbar");

        _reviewSlot.Interact(player.gameObject);
        var priceUi = FindObject<ShopPriceUI>("ShopPriceUI");
        Require(priceUi.IsOpen, "ShopPriceUI opens for stocked item");

        var itemName = GameObject.Find("ItemNameTxt")?.GetComponent<TextMeshProUGUI>();
        Require(itemName != null && itemName.text.Contains("재고 1개"),
            "ShopPriceUI shows the stocked quantity");

        var merchandising = GameObject.Find("MerchandisingTxt")?.GetComponent<TextMeshProUGUI>();
        Require(merchandising != null
                && merchandising.text.Contains("일반품")
                && merchandising.text.Contains("가격 파생값")
                && merchandising.text.Contains("품질 ×1.00"),
            "ShopPriceUI shows price-derived rarity and actual item quality");

        var recommendation = GameObject.Find("RecommendationTxt")?.GetComponent<TextMeshProUGUI>();
        Require(recommendation != null
                && recommendation.text.Contains("추천 기준가 30 G")
                && recommendation.text.Contains("기본가+품질"),
            "ShopPriceUI shows the base-price and quality recommendation");

        // 같은 패널에서 고가 상품 분기도 실제로 갱신되는지 확인한 뒤 캡처용 일반품을 복원한다.
        Item rareItem = Resources.Load<Item>("Items/Item_13_Clothes");
        Require(rareItem != null && rareItem.basePrice >= 100,
            "high-price catalog item is available for derived-rarity review");

        ItemInstance originalInstance = _reviewSlot.currentItem;
        int originalPrice = _reviewSlot.displayPrice;
        _reviewSlot.currentItem = new ItemInstance(rareItem, 1)
        {
            quality = 1.25f,
            currentPrice = rareItem.basePrice
        };
        _reviewSlot.displayPrice = rareItem.basePrice;
        priceUi.Open(_reviewSlot);
        Require(merchandising.text.Contains("희귀품")
                && merchandising.text.Contains("가격 파생값")
                && merchandising.text.Contains("품질 ×1.25"),
            "ShopPriceUI distinguishes the high-price derived-rarity branch");
        var pendingPrice = GameObject.Find("PriceTxt")?.GetComponent<TextMeshProUGUI>();
        Require(recommendation.text.Contains("추천 기준가 186 G")
                && pendingPrice != null
                && pendingPrice.text.Contains("165 G"),
            "ShopPriceUI recommends from quality without auto-applying the price");
        _reviewSlot.RefreshDisplay();
        await CaptureAsync("02b_shop_price_ui_rare");

        _reviewSlot.currentItem = originalInstance;
        _reviewSlot.displayPrice = originalPrice;
        _reviewSlot.RefreshDisplay();
        priceUi.Open(_reviewSlot);
    }

    static void ShowNpcFeedbackReview()
    {
        var priceUi = FindObject<ShopPriceUI>("ShopPriceUI");
        if (priceUi.IsOpen)
            InvokePrivate(priceUi, "OnConfirm");

        var npc = FindVisibleNpcForFeedback();
        var result = PurchaseEvaluator.Evaluate(npc.profile, _reviewSlot, new System.Random(11));
        string feedback = (string)InvokePrivate(npc, "BuildPurchaseFeedback", result, _reviewSlot);
        InvokePrivate(npc, "ShowBubbleMessage", feedback);
        Require(feedback.Length <= 34, $"NPC feedback is compact ({feedback.Length} chars)");

        var bubble = npc.GetComponentInChildren<NpcBubbleUI>(true);
        Require(bubble != null && bubble.gameObject.activeInHierarchy, "NPC feedback bubble is visible");
        var bubbleRT = bubble.bubbleText != null && bubble.bubbleText.transform.parent != null
            ? (RectTransform)bubble.bubbleText.transform.parent
            : (RectTransform)bubble.transform;
        LogScreenPoint("NPC feedback bubble", bubbleRT.position);
        Require(IsScreenPointWithinScreen(bubbleRT.position, 48f), "NPC feedback bubble is within camera frame");
    }

    static void OpenAuditReview()
    {
        var priceUi = Object.FindFirstObjectByType<ShopPriceUI>();
        if (priceUi != null && priceUi.IsOpen)
            priceUi.Close();

        if (_reviewSlot != null && !_reviewSlot.IsEmpty)
        {
            Require(_reviewSlot.TryPurchaseByNpc("PresentationReviewer", out int paid) && paid > 0,
                "presentation review records a Processed sale before opening the audit app");
        }

        var phone = Object.FindFirstObjectByType<SmartphoneUI>();
        Require(phone != null, "SmartphoneUI exists");
        if (!phone.IsOpen)
            phone.Toggle();
        phone.SelectTab(0);
        SnapPhoneOpenForCapture(phone);

        var audit = FindObject<AuditResultUI>("AuditResultUI", includeInactive: true);
        audit.Refresh();
        Require(audit.nextTierText != null && !string.IsNullOrWhiteSpace(audit.nextTierText.text), "audit next-tier text is visible");
        Require(audit.facilityDirectionText != null && audit.facilityDirectionText.text.Contains("조리·가공 작업대"),
            "audit facility preview is visible for the Processed sale capture");
    }

    static void OpenSummaryReview()
    {
        ClosePhoneIfOpen();

        var scenario = FindObject<PlayableDayScenarioController>("PlayableDayScenarioController");
        scenario.RecordManagementFeedback("Lumberjack_01: 가격 적정, 구매 (73%)");
        scenario.RestoreSavedSession("하늘", "green_bay", (int)PlayableDayScenarioController.Stage.Done);
        InvokePrivate(scenario, "ShowDaySummary");

        var title = GetPrivateField<TextMeshProUGUI>(scenario, "_flowTitle");
        var body = GetPrivateField<TextMeshProUGUI>(scenario, "_flowBody");
        Require(title != null && title.text.Contains("Day 1"), "Day 1 summary title is visible");
        Require(body != null && body.text.Contains("다음 성장 목표"), "Day 1 summary includes growth goal");
    }

    static void ValidatePresentationLayout()
    {
        var moneyPanel = GameObject.Find("MoneyHudPanel")?.GetComponent<RectTransform>();
        var objectiveBg = GameObject.Find("PlayableDayGuideCanvas")?.transform.Find("Bg")?.GetComponent<RectTransform>();
        if (moneyPanel != null && objectiveBg != null)
            Require(!ScreenRectsOverlap(moneyPanel, objectiveBg), "MoneyHUD does not overlap objective panel");

        var pricePanel = GameObject.Find("ShopPricePanel")?.GetComponent<RectTransform>();
        if (pricePanel != null)
            Require(IsWithinScreen(pricePanel, 16f), "ShopPriceUI panel is within screen bounds");

        var scenario = Object.FindFirstObjectByType<PlayableDayScenarioController>();
        if (scenario != null)
        {
            var body = GetPrivateField<TextMeshProUGUI>(scenario, "_flowBody");
            if (body != null && body.gameObject.activeInHierarchy)
            {
                body.ForceMeshUpdate();
                float available = ((RectTransform)body.transform).rect.height;
                Require(body.preferredHeight <= available + 12f, $"Day 1 summary body fits ({body.preferredHeight:0}/{available:0})");
            }
        }
    }

    static async Task CaptureAsync(string name)
    {
        if (string.IsNullOrEmpty(_outputDir))
            _outputDir = CreateOutputDirectory();

        string file = Path.Combine(_outputDir, $"{name}.png");
        var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (camera == null)
            throw new InvalidOperationException("Main camera not found for presentation capture.");

        var marker = GameObject.Find("PA_ScreenshotCameraMarker_MarketHub");
        await PA_SafeGameViewCapture.CaptureAsync(file, camera, captureCamera =>
        {
            if (marker != null)
            {
                captureCamera.transform.SetPositionAndRotation(marker.transform.position, marker.transform.rotation);
                captureCamera.fieldOfView = 46f;
            }

            captureCamera.cullingMask = -1;
        }, 1920, 1080, 1000);
        Debug.Log($"PA Final Presentation Capture: {file}");
    }

    static void ValidateCapturedFiles()
    {
        if (string.IsNullOrEmpty(_outputDir))
        {
            Debug.LogWarning("PA Final Presentation: output directory was not set.");
            return;
        }

        string[] names =
        {
            "01_market_hub_objective.png",
            "02_shop_price_ui.png",
            "02b_shop_price_ui_rare.png",
            "03_npc_feedback_bubble.png",
            "04_audit_app_goal.png",
            "05_day1_summary.png"
        };

        foreach (string name in names)
        {
            string path = Path.Combine(_outputDir, name);
            if (!File.Exists(path))
                Debug.LogWarning($"PA Final Presentation: screenshot not found yet: {path}");
        }

        Debug.Log($"PA Final Presentation Review passed. Output={_outputDir}");
    }

    static void PositionCameraAtPresentationMarker()
    {
        var camera = Camera.main;
        var marker = GameObject.Find("PA_ScreenshotCameraMarker_MarketHub");
        if (camera == null || marker == null)
            return;

        camera.transform.SetPositionAndRotation(marker.transform.position, marker.transform.rotation);
        camera.fieldOfView = 46f;
    }

    static void ClosePhoneIfOpen()
    {
        var phone = Object.FindFirstObjectByType<SmartphoneUI>();
        if (phone != null && phone.IsOpen)
            phone.Close();
    }

    static void SnapPhoneOpenForCapture(SmartphoneUI phone)
    {
        if (phone == null || phone.root == null)
            return;

        var canvas = phone.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        var canvasRT = (RectTransform)canvas.transform;
        Vector2 canvasSize = canvasRT.rect.size;
        Vector2 phoneSize = phone.root.rect.size;
        phone.root.anchoredPosition = new Vector2(
            (canvasSize.x - phoneSize.x) * 0.5f,
            (canvasSize.y - phoneSize.y) * 0.5f
        );
    }

    static NpcController FindVisibleNpcForFeedback()
    {
        var camera = Camera.main;
        var npcs = Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None);
        if (camera == null || npcs == null || npcs.Length == 0)
            return FindObject<NpcController>("NpcController");

        NpcController best = null;
        float bestScore = float.MaxValue;
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.55f);

        foreach (var npc in npcs)
        {
            if (npc == null || !npc.gameObject.activeInHierarchy)
                continue;

            Vector3 point = npc.transform.position + Vector3.up * 2.0f;
            Vector3 screen = camera.WorldToScreenPoint(point);
            if (screen.z <= 0f)
                continue;

            if (screen.x < 96f || screen.x > Screen.width - 96f
                || screen.y < 140f || screen.y > Screen.height - 220f)
                continue;

            float score = (new Vector2(screen.x, screen.y) - center).sqrMagnitude;
            if (score < bestScore)
            {
                bestScore = score;
                best = npc;
            }
        }

        return best != null ? best : FindObject<NpcController>("NpcController");
    }

    static ShopSlot FindEmptyShopSlot()
    {
        foreach (var slot in Object.FindObjectsByType<ShopSlot>(FindObjectsSortMode.None))
        {
            if (slot != null && slot.gameObject.activeInHierarchy && slot.IsEmpty)
                return slot;
        }

        throw new InvalidOperationException("No empty active ShopSlot found.");
    }

    static Item FindSellableItem()
    {
        Item fallback = Resources.Load<Item>("Items/Item_BreadLoaf");
        if (IsSellable(fallback)) return fallback;

        foreach (var item in Resources.LoadAll<Item>("Items"))
            if (IsSellable(item)) return item;

        throw new InvalidOperationException("No sellable item found under Resources/Items.");
    }

    static bool IsSellable(Item item)
    {
        return item != null
            && item.category != ItemCategory.Tool
            && item.toolType == ToolType.None
            && (TierService.Instance == null || TierService.Instance.IsUnlocked(item.requiredTier));
    }

    static void SeedHotbarWith(Item item)
    {
        if (Inventory.instance == null)
            throw new InvalidOperationException("Inventory.instance missing.");

        if (Inventory.instance.hotbar == null)
            throw new InvalidOperationException("Inventory.hotbar missing.");

        Inventory.instance.selectedHotbarIndex = 0;
        var slot = Inventory.instance.hotbar.GetSlot(0);
        if (slot == null)
            throw new InvalidOperationException("Hotbar slot 1 missing.");

        slot.SetInstance(new ItemInstance(item, 1)
        {
            quality = 1f,
            currentPrice = item.basePrice
        });
        Inventory.instance.RefreshAllUI();
    }

    static T FindObject<T>(string label, bool includeInactive = false) where T : Object
    {
        T[] objects = includeInactive
            ? Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            : Object.FindObjectsByType<T>(FindObjectsSortMode.None);

        foreach (var obj in objects)
            if (obj != null) return obj;

        throw new InvalidOperationException($"{label} not found.");
    }

    static object InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new MissingMethodException(target.GetType().Name, methodName);
        return method.Invoke(target, args);
    }

    static T GetPrivateField<T>(object target, string fieldName) where T : class
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return field?.GetValue(target) as T;
    }

    static bool ScreenRectsOverlap(RectTransform a, RectTransform b)
    {
        return GetScreenRect(a).Overlaps(GetScreenRect(b));
    }

    static bool IsWithinScreen(RectTransform rt, float padding)
    {
        Rect r = GetScreenRect(rt);
        return r.xMin >= padding
            && r.yMin >= padding
            && r.xMax <= Screen.width - padding
            && r.yMax <= Screen.height - padding;
    }

    static bool IsScreenPointWithinScreen(Vector3 screen, float padding)
    {
        return screen.x >= padding
            && screen.y >= padding
            && screen.x <= Screen.width - padding
            && screen.y <= Screen.height - padding;
    }

    static void LogScreenPoint(string label, Vector3 screen)
    {
        Debug.Log($"PA Final Presentation: {label} screen={screen}, screenSize={Screen.width}x{Screen.height}");
    }

    static Rect GetScreenRect(RectTransform rt)
    {
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float minX = corners[0].x;
        float maxX = corners[0].x;
        float minY = corners[0].y;
        float maxY = corners[0].y;

        for (int i = 1; i < corners.Length; i++)
        {
            minX = Mathf.Min(minX, corners[i].x);
            maxX = Mathf.Max(maxX, corners[i].x);
            minY = Mathf.Min(minY, corners[i].y);
            maxY = Mathf.Max(maxY, corners[i].y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    static string CreateOutputDirectory()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "FinalPresentation"));
        string dir = Path.Combine(root, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        Debug.Log($"PA Final Presentation Check OK: {message}");
    }

    static void MarkFailedAndExit()
    {
        _hadError = true;
        SessionState.SetBool(HadErrorKey, true);
        Cleanup();
        EditorApplication.Exit(1);
    }

    static void Finish()
    {
        bool hadError = _hadError || SessionState.GetBool(HadErrorKey, false);
        bool entered = _entered || SessionState.GetBool(EnteredKey, false);
        bool ran = _ran || SessionState.GetBool(RanKey, false);

        Cleanup();
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(EnteredKey);
        SessionState.EraseBool(RanKey);
        SessionState.EraseBool(HadErrorKey);
        SessionState.EraseString(OutputKey);

        if (!entered || !ran || hadError)
        {
            Debug.LogError("PA Final Presentation Review failed. Check log for the first failed check.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA Final Presentation Review finished successfully.");
        EditorApplication.Exit(0);
    }

    static void Cleanup()
    {
        Application.logMessageReceived -= OnLogMessage;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
    }
}
