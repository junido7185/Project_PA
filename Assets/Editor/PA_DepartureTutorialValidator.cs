using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// VS-PRESENT-001: 키보드 입력 → 기존 상호작용/UI → 실제 NPC FSM/경제를 연속 검증한다.
[InitializeOnLoad]
public static class PA_DepartureTutorialValidator
{
    const string Key = "PA.Departure.Validation";
    const string Output = "Docs/Presentation/2026-09-08";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static Task _task;
    static Keyboard _keyboard;
    static bool _errors;
    static double _started;

    static PA_DepartureTutorialValidator()
    {
        if (SessionState.GetBool(Key, false)) Subscribe();
        EditorApplication.delayCall += RunRequested;
    }

    static void RunRequested()
    {
        const string request = "Logs/VS_PRESENT_001/run-request.txt";
        if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string token = File.ReadAllText(request).Trim();
        if (string.IsNullOrEmpty(token) || SessionState.GetString(Key + ".Request", "") == token) return;
        SessionState.SetString(Key + ".Request", token);
        try { PA_DepartureTutorialBuilder.Build(); Run(); }
        catch (Exception error) { Debug.LogError("[VS-PRESENT-001] BUILD_OR_ENTRY_FAIL " + error); }
    }

    [MenuItem("Project PA/Validation/Run Departure Tutorial")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + ".Done", false);
        SessionState.SetBool(Key + ".Failed", false);
        Subscribe();
        EditorSceneManager.OpenScene("Assets/Scenes/PA_DepartureTutorial.unity", OpenSceneMode.Single);
        ValidateSerializedScene();
        ConfigureGameView();
        EditorApplication.EnterPlaymode();
    }

    static void Subscribe()
    {
        _started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged -= Mode;
        EditorApplication.playModeStateChanged += Mode;
        Application.logMessageReceived -= Log;
        Application.logMessageReceived += Log;
    }

    static void Log(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        _errors = true;
        SessionState.SetBool(Key + ".Failed", true);
    }

    static void Mode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode) _started = EditorApplication.timeSinceStartup;
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key + ".Done", false)) return;
        bool failed = SessionState.GetBool(Key + ".Failed", false);
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        Debug.Log(failed ? "[VS-PRESENT-001] VALIDATION_FAIL" : "[VS-PRESENT-001] P0_VALIDATION_PASS");
        if (Environment.GetCommandLineArgs().Contains("-executeMethod")) EditorApplication.Exit(failed ? 1 : 0);
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || SessionState.GetBool(Key + ".Done", false)) return;
        var tutorial = Object.FindFirstObjectByType<DepartureTutorialController>();
        if (_task == null && EditorApplication.isPlaying && tutorial != null && tutorial.IsReady)
            _task = Checks(tutorial);
        if (_task != null && _task.IsCompleted)
        {
            if (_task.IsFaulted)
            {
                _errors = true;
                Debug.LogError("[VS-PRESENT-001] " + _task.Exception?.GetBaseException());
            }
            Finish(_errors);
        }
        else if (EditorApplication.timeSinceStartup - _started > 190)
        {
            Debug.LogError("[VS-PRESENT-001] Runtime validation timeout.");
            Finish(true);
        }
    }

    static void Finish(bool failed)
    {
        if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
        _keyboard = null;
        SessionState.SetBool(Key + ".Failed", failed || SessionState.GetBool(Key + ".Failed", false));
        SessionState.SetBool(Key + ".Done", true);
        EditorApplication.ExitPlaymode();
    }

    static async Task Checks(DepartureTutorialController t)
    {
        Check(Object.FindObjectsByType<Inventory>(FindObjectsSortMode.None).Length == 1, "one Inventory authority");
        Check(Object.FindObjectsByType<EconomyService>(FindObjectsSortMode.None).Length == 1, "one Economy authority");
        Check(Object.FindObjectsByType<Shop>(FindObjectsSortMode.None).Length == 1, "one Shop authority");
        Check(Object.FindObjectsByType<NpcController>(FindObjectsSortMode.None).Length == 1, "one original NPC buyer");
        Check(Object.FindObjectsByType<SaveManager>(FindObjectsSortMode.None).Length == 0, "development entry cannot overwrite campaign save");
        Check(Inventory.instance.CountItems(t.fruit) == 0 && t.Stage == 1, "fresh spawn has no prefilled fruit or completed objectives");
        Check(DayNightShopLoopController.Instance.IsTutorialAlwaysOpen, "existing first-day tutorial purchase gate");
        _keyboard = InputSystem.AddDevice<Keyboard>("DepartureValidationKeyboard");
        _keyboard.MakeCurrent();
        await Task.Delay(700);
        Vector3 spawn = t.player.position;
        await Walk(t.player, new Vector3(t.movementCheckpoint.position.x, spawn.y, spawn.z));
        await Until(() => t.Stage == 2, "WASD movement reaches Bay 01 checkpoint");
        Check(Vector3.Distance(spawn, t.player.position) > 1f, "actual PlayerController movement, no teleport");
        await Walk(t.player, new Vector3(t.fruitTree.transform.position.x, 0f, -3f));
        await Walk(t.player, t.fruitTree.transform.position + Vector3.back * 1.6f);
        await Interact(t, t.fruitTree);
        await Until(() => t.Stage == 3, "tree interaction grants three fruit through Inventory");
        Check(t.FruitCount == 3 && Inventory.instance.CountItems(t.fruit) == 3, "exact three authoritative fruit");
        int money = EconomyService.Instance.Money;
        var revenue = EconomyService.Instance.CumulativeRevenue;
        await Walk(t.player, new Vector3(t.player.position.x, 0f, -3.4f));
        await Walk(t.player, new Vector3(t.trainingSlot.transform.position.x, 0f, -3.4f));
        await Walk(t.player, t.trainingSlot.transform.position + Vector3.back * 1.7f);
        await Interact(t, t.trainingSlot);
        await Until(() => t.Stage == 4, "Space stocks original ShopSlot");
        Check(Inventory.instance.CountItems(t.fruit) == 2 && !t.trainingSlot.IsEmpty, "stocking transfers one fruit, no duplicated inventory");
        await Interact(t, t.trainingSlot);
        Check(ShopPriceUI.instance.IsOpen, "Space opens original ShopPriceUI");
        await Capture("02a_Tutorial_Price.png");
        Vector3 buyerStart = t.buyer.transform.position;
        int affordable = Mathf.Max(1, Mathf.FloorToInt(t.fruit.basePrice * 0.7f));
        ConfirmPrice(affordable);
        await Until(() => t.Complete || t.Stage == 4, "real customer decision resolves");
        // 권장 범위에서도 난수 거절 가능성은 남는다. 일반 재가격 경로로 한 번 더 방문한다.
        if (!t.Complete)
        {
            await Interact(t, t.trainingSlot);
            ConfirmPrice(affordable);
            await Until(() => t.Complete, "normal repricing retry can finish certification");
        }
        Check(t.DecisionCount >= 1 && t.CompanionSelectionUnlocked && t.trainingSlot.IsEmpty, "completion requires actual decision and sale");
        Check(EconomyService.Instance.Money == money + affordable && EconomyService.Instance.CumulativeRevenue == revenue + affordable,
            "sale deposited once by existing EconomyService");
        Check(Inventory.instance.CountItems(t.fruit) == 2, "remaining harvested inventory preserved");
        Check(Vector3.Distance(buyerStart, t.buyer.transform.position) > 0.5f, "NPC physically approached shelf");
        Debug.Log("[VS-PRESENT-001] BASIC_FLOW_PASS before capture evidence");
        await Capture("02b_Certification_Complete.png");
        await Interact(t, t.trainingSlot);
        await Interact(t, t.trainingSlot);
        await Capture("02_Tutorial_PriceAndReaction.png");
        ShopPriceUI.instance.Close();
        Camera camera = Camera.main;
        CameraController follow = camera.GetComponent<CameraController>();
        follow.enabled = false;
        var captureCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.enabled).ToArray();
        foreach (var canvas in captureCanvases) canvas.enabled = false;
        await PA_SafeGameViewCapture.CaptureAsync(Path.GetFullPath(Output + "/01_PA_Company_FirstView.png"), camera, c =>
        {
            c.transform.position = new Vector3(3f, 23f, -21f);
            c.transform.LookAt(new Vector3(3f, 0f, 0f));
            c.orthographicSize = 11.5f;
        });
        foreach (var canvas in captureCanvases) canvas.enabled = true;
        follow.enabled = true;
        Check(Screen.width == 1920 && Screen.height == 1080, "actual GameView is 1920x1080");
        File.WriteAllText(Output + "/P0-validation.json", JsonUtility.ToJson(new Evidence
        {
            status = "PASS", stage = t.Stage, decisionCount = t.DecisionCount, fruitRemaining = Inventory.instance.CountItems(t.fruit),
            moneyBefore = money, moneyAfter = EconomyService.Instance.Money, saleAmount = affordable,
            input = "InputSystem keyboard -> PlayerInputHandler -> PlayerController / PlayerInteraction",
            screenshotWidth = Screen.width, screenshotHeight = Screen.height
        }, true));
        Debug.Log("[VS-PRESENT-001] P0_CHECKS_PASS spawn->movement->tree->inventory->display->price->NPC->decision->economy->complete");
    }

    static async Task Walk(Transform player, Vector3 destination)
    {
        double deadline = EditorApplication.timeSinceStartup + 18;
        while (EditorApplication.timeSinceStartup < deadline)
        {
            Vector3 delta = destination - player.position;
            delta.y = 0f;
            if (delta.magnitude < 0.35f) break;
            var keys = new List<UnityEngine.InputSystem.Key>();
            if (Mathf.Abs(delta.x) > 0.20f) keys.Add(delta.x > 0 ? UnityEngine.InputSystem.Key.D : UnityEngine.InputSystem.Key.A);
            if (Mathf.Abs(delta.z) > 0.20f) keys.Add(delta.z > 0 ? UnityEngine.InputSystem.Key.W : UnityEngine.InputSystem.Key.S);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys.ToArray()));
            await Task.Delay(40);
        }
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        await Task.Delay(250);
        Vector3 error = destination - player.position; error.y = 0f;
        Check(error.magnitude < 0.8f, $"walk reached {destination}; actual={player.position}");
    }

    static async Task Interact(DepartureTutorialController t, IInteractable expected)
    {
        var interaction = t.player.GetComponent<PlayerInteraction>();
        object[] args = { null, null };
        bool found = (bool)typeof(PlayerInteraction).GetMethod("TryFindInteractable", Private).Invoke(interaction, args);
        Check(found && ReferenceEquals(args[0], expected), "reachable Space target = " + (expected as Component)?.name);
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(UnityEngine.InputSystem.Key.Space));
        await Task.Delay(120);
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        await Task.Delay(180);
    }

    static void ConfirmPrice(int price)
    {
        ShopPriceUI ui = ShopPriceUI.instance;
        int pending = (int)typeof(ShopPriceUI).GetField("_pendingPrice", Private).GetValue(ui);
        typeof(ShopPriceUI).GetMethod("AdjustPrice", Private).Invoke(ui, new object[] { price - pending });
        ((Button)typeof(ShopPriceUI).GetField("_confirmBtn", Private).GetValue(ui)).onClick.Invoke();
        Check(!ui.IsOpen, "real price confirm button closes UI");
    }

    static Task Capture(string name) => PA_SafeGameViewCapture.CaptureAsync(Path.GetFullPath(Output + "/" + name), Camera.main, null, 1920, 1080, 600);

    static async Task Until(Func<bool> predicate, string message)
    {
        double deadline = EditorApplication.timeSinceStartup + 30;
        while (!predicate() && EditorApplication.timeSinceStartup < deadline) await Task.Delay(100);
        Check(predicate(), message);
    }

    static void ValidateSerializedScene()
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "missing script free: " + child.name);
            foreach (Component component in child.GetComponents<Component>())
            {
                if (component == null) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                        Check(property.objectReferenceInstanceIDValue == 0, "serialized reference: " + child.name + "/" + property.propertyPath);
            }
        }
    }

    static void ConfigureGameView()
    {
        Assembly editor = typeof(Editor).Assembly;
        Type viewType = editor.GetType("UnityEditor.GameView");
        EditorWindow view = EditorWindow.GetWindow(viewType);
        view.Show();
        Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
        object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(editor.GetType("UnityEditor.GameViewSizeGroupType"), "Standalone") });
        string[] labels = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
        int index = Array.FindIndex(labels, label => label.Contains("1920") && label.Contains("1080"));
        Check(index >= 0, "existing Full HD GameView preset available");
        viewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(view, index);
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log("[VS-PRESENT-001] CHECK_OK " + message);
    }

    [Serializable] class Evidence
    {
        public string status, input;
        public int stage, decisionCount, fruitRemaining, moneyBefore, moneyAfter, saleAmount, screenshotWidth, screenshotHeight;
    }
}
