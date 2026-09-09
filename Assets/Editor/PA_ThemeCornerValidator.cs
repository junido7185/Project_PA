#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Task 086 — D3D11 Play Mode validation for the derived merchandising-corner feature.
[InitializeOnLoad]
public static class PA_ThemeCornerValidator
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.ThemeCorner.Active";
    const string EnteredKey = "PA.ThemeCorner.Entered";
    const string RanKey = "PA.ThemeCorner.Ran";
    const string HadErrorKey = "PA.ThemeCorner.HadError";
    const string OutputKey = "PA.ThemeCorner.Output";

    static readonly Vector2Int[] ShelfCells =
    {
        new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
        new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(3, 2)
    };

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;
    static Task _runtimeTask;

    static PA_ThemeCornerValidator()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _hadError = SessionState.GetBool(HadErrorKey, false);
        RegisterCallbacks();
        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Run Theme Corner Validation")]
    public static void RunThemeCornerValidation()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA ThemeCorner: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "ThemeCorner", stamp));
        Directory.CreateDirectory(output);

        _entered = false;
        _ran = false;
        _hadError = false;
        _runtimeTask = null;
        _startedAt = EditorApplication.timeSinceStartup;
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(HadErrorKey, false);
        SessionState.SetString(OutputKey, output);
        RegisterCallbacks();
        Debug.Log($"PA ThemeCorner: entering Play Mode. Output={output}");
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
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        _hadError = true;
        SessionState.SetBool(HadErrorKey, true);
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
            && SessionState.GetBool(RanKey, false)) Finish();
    }

    static void OnEditorUpdate()
    {
        double elapsed = EditorApplication.timeSinceStartup - _startedAt;
        if (!_entered)
        {
            if (elapsed > 60d)
            {
                Debug.LogError("PA ThemeCorner: timed out entering Play Mode.");
                MarkFailedAndExit();
            }
            return;
        }

        if (!_ran && EditorApplication.isPlaying && _runtimeTask == null && elapsed > 4d)
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
                Debug.LogError($"PA Theme Corner Validation failed: {_runtimeTask.Exception?.GetBaseException()}");
            }
            _ran = true;
            SessionState.SetBool(RanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }

        if (!_ran && elapsed > 120d)
        {
            Debug.LogError("PA ThemeCorner: timed out before runtime checks completed.");
            MarkFailedAndExit();
        }
    }

    static async Task RunRuntimeChecksAsync()
    {
        Time.timeScale = 1f;
        var customization = RequireOne<ShopCustomizationController>("ShopCustomizationController");
        var corners = RequireOne<MerchandisingCornerController>("MerchandisingCornerController");
        for (int i = 0; i < 100 && (!customization.IsReady || !corners.IsReady); i++)
            await Task.Delay(50);

        Require(customization.IsReady && corners.IsReady, "customization and corner controllers initialized");
        Require(GridService.Instance != null && GridService.Instance.HasZone(customization.ZoneId),
            "corner geometry uses the authoritative shop grid zone");

        var loop = RequireOne<DayNightShopLoopController>("DayNightShopLoopController");
        loop.keepDay1TutorialShopOpen = false;
        GameClock.Instance.ForceSet(8f, 2, "PA ThemeCorner quiet validation window");
        loop.SetShopOpenedForValidation(false);

        var slots = new Dictionary<Vector2Int, ShopSlot>();
        foreach (Vector2Int cell in ShelfCells)
        {
            string id = customization.FindPlacementIdAt(cell);
            ShopSlot slot = customization.GetPlacementGameObject(id)?.GetComponent<ShopSlot>();
            Require(!string.IsNullOrWhiteSpace(id) && slot != null, $"authored ShopSlot resolved at {cell}");
            slots[cell] = slot;
        }

        Item fish = Resources.Load<Item>("Items/Item_Fish");
        Item bread = Resources.Load<Item>("Items/Item_BreadLoaf");
        Require(fish != null && fish.category == ItemCategory.Raw, "raw validation item resolved");
        Require(bread != null && bread.category == ItemCategory.Processed, "processed validation item resolved");

        foreach (ShopSlot slot in slots.Values) Empty(slot);
        Require(corners.RefreshNow() == 0 && corners.WorldLabelCount == 0,
            "empty shelves create no corner and no world label");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        customization.ToggleFromTerminal(player);
        string output = SessionState.GetString(OutputKey, string.Empty);
        Require(!string.IsNullOrWhiteSpace(output), "isolated validation output exists");
        await CaptureGameViewAsync(Path.Combine(output, "theme_corner_01_none.png"));

        Vector2Int a = new Vector2Int(1, 1);
        Vector2Int b = new Vector2Int(2, 1);
        Vector2Int c = new Vector2Int(3, 1);
        Vector2Int diagonal = new Vector2Int(2, 2);

        Stock(slots[a], fish);
        Stock(slots[b], fish);
        RequireRawCorner(corners, 2, "two edge-adjacent same-category shelves form one corner");
        Require(corners.WorldLabelCount == 1, "one qualifying component creates one world label");
        Require(FindProgressText()?.text.Contains("코너: 원재료 2") == true,
            "placement ledger reports the live corner");
        await CaptureGameViewAsync(Path.Combine(output, "theme_corner_02_raw_two.png"));

        Empty(slots[b]);
        Stock(slots[diagonal], fish);
        Require(corners.RefreshNow() == 0, "diagonal contact does not form a corner");

        Empty(slots[diagonal]);
        Stock(slots[c], fish);
        Require(corners.RefreshNow() == 0, "a one-cell gap does not form a corner");

        Empty(slots[c]);
        Stock(slots[b], bread);
        Require(corners.RefreshNow() == 0, "edge-adjacent mixed categories do not form a corner");

        Stock(slots[b], fish);
        Stock(slots[c], fish);
        RequireRawCorner(corners, 3, "a three-shelf connected component reports all three placements");

        int baselineSales = SalesLogManager.Instance.GetRecent(100).Count;
        Require(slots[b].TryPurchaseByNpc("ThemeCornerSoldOut", out int rawPaid) && rawPaid == fish.basePrice,
            "the existing purchase path empties the middle shelf");
        Require(corners.RefreshNow() == 0 && slots[b].IsSoldOutToday,
            "sold-out middle shelf immediately breaks the corner");
        Stock(slots[b], fish);
        RequireRawCorner(corners, 3, "restocking immediately restores the corner");

        Empty(slots[a]);
        RequireRawCorner(corners, 2, "remaining adjacent pair stays a corner");
        string cId = customization.FindPlacementIdAt(c);
        Require(customization.TryGetPlacementSnapshot(cId, out Vector2Int originalCell,
            out int originalRotation, out bool wasRecovered) && !wasRecovered,
            "movable shelf snapshot resolves before recovery");
        Require(customization.TryRecoverPlacementForValidation(cId, false, out string recoverReason),
            $"recovering a shelf updates placement state ({recoverReason})");
        Require(corners.RefreshNow() == 0, "recovered shelf no longer contributes to a corner");
        Require(customization.TryMovePlacementForValidation(cId, originalCell, originalRotation, out string moveReason),
            $"moving the recovered shelf back reactivates it ({moveReason})");
        Stock(slots[c], fish);
        RequireRawCorner(corners, 2, "restocking the reactivated shelf restores the adjacent corner");

        foreach (ShopSlot slot in slots.Values) Stock(slot, bread);
        RequireProcessedCorner(corners, 6, "six connected processed shelves form one component");
        Require(slots[a].TryPurchaseByNpc("ThemeCornerProcessedA", out int processedPaidA)
            && processedPaidA == bread.basePrice, "first processed sale uses the existing sale path");
        Require(slots[new Vector2Int(3, 2)].TryPurchaseByNpc("ThemeCornerProcessedB", out int processedPaidB)
            && processedPaidB == bread.basePrice, "second processed sale uses the existing sale path");
        RequireProcessedCorner(corners, 4, "remaining connected processed shelves keep the corner live");

        var recent = SalesLogManager.Instance.GetRecent(100);
        Require(recent.Count == baselineSales + 3,
            "only the three real purchases append sale records; corner detection creates none");
        VillageChangeSignalController.Instance.RefreshNow();
        Require(VillageChangeSignalController.Instance.CurrentSignalText.Contains("Processed"),
            "processed corner sales reach the existing village-direction signal");
        await CaptureGameViewAsync(Path.Combine(output, "theme_corner_03_processed_four.png"));

        foreach (ShopSlot slot in slots.Values) Empty(slot);
        Stock(slots[a], fish);
        Stock(slots[b], fish);
        RequireRawCorner(corners, 2, "save seed is a live raw corner");

        var save = RequireOne<SaveManager>("SaveManager");
        FieldInfo repositoryField = typeof(SaveManager).GetField("_repository",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(repositoryField != null, "SaveManager repository is injectable for isolated validation");
        repositoryField.SetValue(save, new LocalJsonSaveRepository(output));
        await save.SaveGameAsync();
        Require(File.Exists(Path.Combine(output, "savegame.json")), "isolated save file written");

        Empty(slots[a]);
        Empty(slots[b]);
        Require(corners.RefreshNow() == 0, "runtime mutation removes the derived corner before load");
        await save.LoadGameAsync();
        await Task.Delay(250);
        RequireRawCorner(corners, 2, "load rebuilds the corner from restored ShopSlot and placement state");
        Require(corners.CurrentCorners.Count == 1 && corners.CurrentCorners[0].PlacementIds.Count == 2,
            "corner state remains derived and has no duplicate persisted record");

        customization.CloseCustomization();
        Debug.Log($"PA Theme Corner Validation passed. adjacency=4-neighbor, saleRecords=3, " +
            $"villageSignal=Processed, saveDerived=Raw2, output={output}");
    }

    static void RequireRawCorner(MerchandisingCornerController controller, int count, string message)
    {
        controller.RefreshNow();
        Require(controller.CornerCount == 1
            && controller.TryGetFirstCorner(ItemCategory.Raw, out var corner)
            && corner.SlotCount == count, message);
    }

    static void RequireProcessedCorner(MerchandisingCornerController controller, int count, string message)
    {
        controller.RefreshNow();
        Require(controller.CornerCount == 1
            && controller.TryGetFirstCorner(ItemCategory.Processed, out var corner)
            && corner.SlotCount == count, message);
    }

    static void Stock(ShopSlot slot, Item item)
    {
        slot.currentItem = new ItemInstance(item, 1) { quality = 1f, currentPrice = item.basePrice };
        slot.displayPrice = item.basePrice;
        slot.RefreshDisplay();
    }

    static void Empty(ShopSlot slot)
    {
        slot.currentItem = null;
        slot.displayPrice = 0;
        slot.RefreshDisplay();
    }

    static TextMeshProUGUI FindProgressText()
    {
        foreach (TextMeshProUGUI text in Object.FindObjectsByType<TextMeshProUGUI>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (text != null && text.gameObject.name == "Progress") return text;
        return null;
    }

    static async Task CaptureGameViewAsync(string outputPath)
    {
        Camera camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        Transform interior = GameObject.Find("PA_StoreInterior")?.transform;
        Require(camera != null && interior != null, "game camera and shop interior exist for capture");

        await PA_SafeGameViewCapture.CaptureAsync(outputPath, camera, captureCamera =>
        {
            // 일반 Play Mode 프레임을 그대로 캡처한다. 배치 grid가 활성인 상태에서
            // URP 카메라를 별도 Camera.Render()하면 Unity 네이티브 렌더 충돌이 발생했다.
            captureCamera.transform.position = interior.TransformPoint(new Vector3(0f, 8.2f, -3.75f));
            captureCamera.transform.rotation = Quaternion.LookRotation(
                interior.TransformPoint(new Vector3(0f, 0.30f, 0.45f)) - captureCamera.transform.position,
                interior.up);
            captureCamera.orthographic = true;
            captureCamera.orthographicSize = 5.55f;

            foreach (PrototypeWorldLabel label in Object.FindObjectsByType<PrototypeWorldLabel>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Vector3 toCamera = label.transform.position - captureCamera.transform.position;
                if (toCamera.sqrMagnitude > 0.001f)
                    label.transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
            }
        }, 1920, 1080, 1000);
        Require(File.Exists(outputPath) && new FileInfo(outputPath).Length > 1024,
            $"capture generated: {Path.GetFileName(outputPath)}");
    }

    static T RequireOne<T>(string label) where T : Object
    {
        T value = Object.FindFirstObjectByType<T>();
        if (value == null) throw new InvalidOperationException($"{label} not found.");
        return value;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        Debug.Log($"PA ThemeCorner Check OK: {message}");
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
            Debug.LogError("PA Theme Corner Validation failed. Check the first error in the validation log.");
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log("PA Theme Corner Validation finished successfully.");
        EditorApplication.Exit(0);
    }

    static void Cleanup()
    {
        Application.logMessageReceived -= OnLogMessage;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        _runtimeTask = null;
    }
}

// 공용 검증 캡처는 일반 GameView 프레임만 요청한다. Play Mode 프레임 도중
// 별도 RenderTexture와 Camera.Render를 호출하는 경로는 2026-07-17 동일
// 네이티브 충돌을 두 번 일으켰으므로 이 도우미 안에서도 허용하지 않는다.
internal static class PA_SafeGameViewCapture
{
    internal static async Task CaptureAsync(string outputPath, Camera camera, Action<Camera> configureCamera,
        int width = 1920, int height = 1080, int settleMilliseconds = 1000)
    {
        if (camera == null) throw new InvalidOperationException("Game camera is required for capture.");
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new InvalidOperationException("Capture output path is required.");

        string directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        bool hadPreviousFile = File.Exists(outputPath);
        long previousLength = hadPreviousFile ? new FileInfo(outputPath).Length : -1L;
        DateTime previousWriteTime = hadPreviousFile ? File.GetLastWriteTimeUtc(outputPath) : DateTime.MinValue;
        Debug.Log($"[SafeGameViewCapture] baseline exists={hadPreviousFile} bytes={previousLength} utc={previousWriteTime:O} path={outputPath}");
        if (!Application.isPlaying || EditorApplication.isPaused ||
            SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            throw new InvalidOperationException("Capture requires an unpaused, rendered Play Mode GameView.");
        var gameView = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        gameView.Show();
        gameView.Focus();
        gameView.Repaint();
        int initialFrame = Time.frameCount;
        Debug.Log($"[SafeGameViewCapture] graphics={SystemInfo.graphicsDeviceType} gameView={gameView.position} frame={initialFrame}");

        int previousWidth = Mathf.Max(1, Screen.width);
        int previousHeight = Mathf.Max(1, Screen.height);
        bool previousFullScreen = Screen.fullScreen;
        RenderTexture previousTarget = camera.targetTexture;
        Vector3 previousPosition = camera.transform.position;
        Quaternion previousRotation = camera.transform.rotation;
        bool previousOrthographic = camera.orthographic;
        float previousOrthographicSize = camera.orthographicSize;
        float previousFieldOfView = camera.fieldOfView;
        int previousCullingMask = camera.cullingMask;
        Rect previousRect = camera.rect;

        try
        {
            camera.targetTexture = null;
            Screen.SetResolution(Mathf.Max(1, width), Mathf.Max(1, height), false);
            configureCamera?.Invoke(camera);

            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text != null) text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
            }
            Canvas.ForceUpdateCanvases();

            // D3D11 GameView와 일시 Canvas가 안정될 시간을 둔다. 이 지연은
            // ThemeCorner의 안전 경로에서 검은 UI 프레임 빈도를 줄인 값이다.
            await Task.Delay(Mathf.Max(100, settleMilliseconds));
            double frameDeadline = EditorApplication.timeSinceStartup + 3;
            while (Time.frameCount < initialFrame + 2 && EditorApplication.timeSinceStartup < frameDeadline)
            {
                gameView.Repaint();
                EditorApplication.QueuePlayerLoopUpdate();
                await Task.Delay(50);
            }
            if (Time.frameCount < initialFrame + 2)
                throw new InvalidOperationException("GameView frames did not advance before capture.");
            gameView.Focus();
            gameView.Repaint();
            ScreenCapture.CaptureScreenshot(outputPath);

            bool captured = false;
            long stableLength = -1;
            DateTime stableWriteTime = DateTime.MinValue;
            int stablePolls = 0;
            for (int i = 0; i < 120; i++)
            {
                gameView.Repaint();
                EditorApplication.QueuePlayerLoopUpdate();
                if (File.Exists(outputPath))
                {
                    var info = new FileInfo(outputPath);
                    bool fresh = !hadPreviousFile || info.LastWriteTimeUtc > previousWriteTime
                        || info.Length != previousLength;
                    if (fresh && info.Length > 1024)
                    {
                        stablePolls = info.Length == stableLength && info.LastWriteTimeUtc == stableWriteTime ? stablePolls + 1 : 0;
                        stableLength = info.Length;
                        stableWriteTime = info.LastWriteTimeUtc;
                        if (stablePolls >= 2)
                        {
                            captured = true;
                            Debug.Log($"[SafeGameViewCapture] fresh stable bytes={stableLength} utc={stableWriteTime:O} frame={Time.frameCount}");
                            break;
                        }
                    }
                }
                await Task.Delay(100);
            }

            if (!captured)
                throw new InvalidOperationException(
                    $"GameView capture was not written in time: {Path.GetFileName(outputPath)}");
        }
        finally
        {
            camera.targetTexture = previousTarget;
            camera.transform.position = previousPosition;
            camera.transform.rotation = previousRotation;
            camera.orthographic = previousOrthographic;
            camera.orthographicSize = previousOrthographicSize;
            camera.fieldOfView = previousFieldOfView;
            camera.cullingMask = previousCullingMask;
            camera.rect = previousRect;
            Screen.SetResolution(previousWidth, previousHeight, previousFullScreen);
        }
    }
}
#endif
