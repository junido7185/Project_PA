#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Visual Demo Integration Pass v2 — 실제 플레이 카메라 Before/After 캡처 툴.
//
// 목적: 검증기 통과가 아니라 "사용자가 보는 Game View"가 판정 기준이므로,
// 자동 캡처 마커 카메라가 아닌 **실제 플레이 추적 카메라** 그대로 캡처한다.
// - 온보딩 모달을 닫고, 사용자 스크린샷과 같은 시간대(Day 1 15:15)로 맞춘 뒤,
//   2560x1440 로 UI 포함 캡처한다 (PA_CustomerPanelLayoutValidator 캡처 방식 재사용).
// - 파일명 라벨은 환경변수 PA_SHOT_LABEL (기본 "shot") — before/after 비교용.
[InitializeOnLoad]
public static class PA_DemoViewCapture
{
    const string ScenePath = "Assets/Scenes/Prototype_FirstDay.unity";
    const string ActiveKey = "PA.DemoViewCapture.Active";
    const string EnteredKey = "PA.DemoViewCapture.Entered";
    const string RanKey = "PA.DemoViewCapture.Ran";
    const string HadErrorKey = "PA.DemoViewCapture.HadError";

    const int CaptureWidth = 2560;
    const int CaptureHeight = 1440;

    static bool _entered;
    static bool _ran;
    static bool _hadError;
    static double _startedAt;
    static Task _runtimeTask;

    static PA_DemoViewCapture()
    {
        if (!SessionState.GetBool(ActiveKey, false))
            return;

        _entered = SessionState.GetBool(EnteredKey, false);
        _ran = SessionState.GetBool(RanKey, false);
        _hadError = SessionState.GetBool(HadErrorKey, false);
        RegisterCallbacks();

        if (_ran && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            EditorApplication.delayCall += Finish;
    }

    [MenuItem("Project PA/Validation/Capture Demo Game View")]
    public static void RunDemoViewCapture()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"PA DemoViewCapture: failed to open scene at {ScenePath}");
            EditorApplication.Exit(1);
            return;
        }

        _entered = false;
        _ran = false;
        _hadError = false;
        _runtimeTask = null;
        _startedAt = EditorApplication.timeSinceStartup;

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetBool(EnteredKey, false);
        SessionState.SetBool(RanKey, false);
        SessionState.SetBool(HadErrorKey, false);

        RegisterCallbacks();

        Debug.Log($"PA DemoViewCapture: entering Play Mode. label={ShotLabel()}");
        EditorApplication.EnterPlaymode();
    }

    static string ShotLabel()
    {
        string label = Environment.GetEnvironmentVariable("PA_SHOT_LABEL");
        return string.IsNullOrWhiteSpace(label) ? "shot" : label.Trim();
    }

    static void RegisterCallbacks()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
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
                Debug.LogError("PA DemoViewCapture: timed out before entering Play Mode.");
                MarkFailedAndExit();
            }
            return;
        }

        if (!_ran && EditorApplication.isPlaying && _runtimeTask == null && elapsed > 3.0)
        {
            _runtimeTask = RunRuntimeCaptureAsync();
            return;
        }

        if (!_ran && _runtimeTask != null && _runtimeTask.IsCompleted)
        {
            if (_runtimeTask.IsFaulted)
            {
                _hadError = true;
                SessionState.SetBool(HadErrorKey, true);
                Debug.LogError($"PA DemoViewCapture failed: {_runtimeTask.Exception?.GetBaseException()}");
            }

            _ran = true;
            SessionState.SetBool(RanKey, true);
            EditorApplication.ExitPlaymode();
            return;
        }

        if (_entered && !_ran && elapsed > 120.0)
        {
            Debug.LogError("PA DemoViewCapture: timed out before capture.");
            MarkFailedAndExit();
        }
    }

    static async Task RunRuntimeCaptureAsync()
    {
        PrepareRuntimeState();

        // v3 — 손님 NPC 가 판매대에 도착할 시간을 확보 (스테이징 후 이동 시간).
        // P4 실내 샷은 입구→예약 접근셀까지 실제로 걷는 두 손님을 기다린다.
        int settleMilliseconds = Environment.GetEnvironmentVariable("PA_SHOT_INSIDE") == "1" ? 11000 : 4500;
        await Task.Delay(settleMilliseconds);
        await CapturePlayCameraAsync();
    }

    // 사용자 스크린샷과 같은 조건: 온보딩 닫힘 + Day 1 오후 + 실제 추적 카메라 유지.
    static void PrepareRuntimeState()
    {
        Time.timeScale = 1f;
        Screen.SetResolution(CaptureWidth, CaptureHeight, false);

        var scenario = Object.FindFirstObjectByType<PlayableDayScenarioController>();
        if (scenario != null)
        {
            try { scenario.RestoreSavedSession("하늘", "green_bay", 0); }
            catch (Exception ex) { Debug.LogWarning($"PA DemoViewCapture: RestoreSavedSession skipped: {ex.Message}"); }
        }

        if (GameClock.Instance != null)
            GameClock.Instance.ForceSet(15.25f, 1, "PA_DemoViewCapture");

        // v3 — ForceSet 은 OnHourTick 을 발화하지 않아 조명이 시작 시각(새벽)에 머무는
        // 캡처 아티팩트가 있었다. 강제 시각에 맞춰 태양/앰비언트를 즉시 재적용한다.
        foreach (var visual in Object.FindObjectsByType<DayNightVisual>(FindObjectsSortMode.None))
            visual.ApplyHour(15);

        // 주의: 카메라를 마커로 옮기지 않는다 — 실제 플레이 카메라 프레이밍 그대로 캡처한다.

        // S3 — PA_SHOT_INSIDE=1 이면 실내 잡화점 샷: Day 2 밤 영업 + 진열 + 손님 초대 + 플레이어 입장.
        if (Environment.GetEnvironmentVariable("PA_SHOT_INSIDE") == "1")
            PrepareInsideShot();

        LogLightingDiagnostics();
    }

    static void PrepareInsideShot()
    {
        var loop = DayNightShopLoopController.Instance;
        if (loop != null)
        {
            // 18:15 — 전문직 Shopping 창(18~20시) 초입. 19.5 는 20:00 Rest 강제 귀가까지
            // 실시간 30초뿐이라 손님 연출이 레이스로 사라질 수 있었다.
            loop.SimulatePhaseForValidation(18.25f, 2);
            loop.SetShopOpenedForValidation(true);
        }

        var interior = GameObject.Find("PA_StoreInterior");
        if (interior == null) { Debug.LogWarning("[DemoViewCapture] PA_StoreInterior missing"); return; }

        string[] items = { "Items/Item_BreadLoaf", "Items/Item_Carrot", "Items/Item_Ore", "Items/Item_Wheat" };
        var slots = interior.GetComponentsInChildren<ShopSlot>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            var item = Resources.Load<Item>(items[i % items.Length]);
            if (item == null) continue;
            slots[i].currentItem = new ItemInstance(item, 1) { quality = 1f, currentPrice = item.basePrice };
            slots[i].displayPrice = item.basePrice;
            slots[i].RefreshDisplay();
        }

        // P4 — 서로 다른 진열대 앞자리를 예약하는 동시 손님 2명을 실제 게임 카메라로 확인한다.
        if (InteriorCustomerController.Instance != null)
        {
            InteriorCustomerController.Instance.TryInviteOne();
            InteriorCustomerController.Instance.TryInviteOne();
        }

        var player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        var door = GameObject.Find("PA_StoreDoor_Out");
        var entrance = door != null ? door.GetComponent<BuildingEntrance>() : null;
        if (player != null && entrance != null)
            entrance.Interact(player);
    }

    // v3 — 조명/포그/앰비언트 실측 + 하단 갈색 플랫폼 정체 확인용 진단.
    static void LogLightingDiagnostics()
    {
        var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in sun)
        {
            if (l.type != LightType.Directional) continue;
            Debug.Log($"[DemoViewDiag] DirLight '{l.name}' color={l.color} intensity={l.intensity:0.00} "
                + $"rot={l.transform.rotation.eulerAngles} shadows={l.shadows} hasVisual={(l.GetComponent<DayNightVisual>() != null)}");
        }

        Debug.Log($"[DemoViewDiag] fog={RenderSettings.fog} fogColor={RenderSettings.fogColor} fogMode={RenderSettings.fogMode} "
            + $"fogDensity={RenderSettings.fogDensity:0.0000} fogStart={RenderSettings.fogStartDistance:0.0} fogEnd={RenderSettings.fogEndDistance:0.0}");
        Debug.Log($"[DemoViewDiag] ambientMode={RenderSettings.ambientMode} ambientLight={RenderSettings.ambientLight} "
            + $"ambientIntensity={RenderSettings.ambientIntensity:0.00} ambientSky={RenderSettings.ambientSkyColor} skybox={(RenderSettings.skybox != null ? RenderSettings.skybox.name : "null")}");

        // 플레이어 남쪽(화면 하단)의 갈색 플랫폼 정체 확인.
        var player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        if (player != null)
        {
            for (int i = 2; i <= 8; i += 2)
            {
                Vector3 probe = player.transform.position - player.transform.forward * i + Vector3.up * 5f;
                if (Physics.Raycast(probe, Vector3.down, out var hit, 15f))
                    Debug.Log($"[DemoViewDiag] probe back {i}m → '{hit.collider.gameObject.name}' at y={hit.point.y:0.00} (root={hit.collider.transform.root.name})");
            }

            // 콜라이더 없는 시각 전용 오브젝트까지 포함 — 플레이어 남쪽 박스 안 렌더러 나열.
            Vector3 c = player.transform.position;
            var boxMin = new Vector3(c.x - 9f, -2f, c.z - 12f);
            var boxMax = new Vector3(c.x + 9f, 3f, c.z - 1.5f);
            int listed = 0;
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled) continue;
                Vector3 b = r.bounds.center;
                if (b.x < boxMin.x || b.x > boxMax.x || b.z < boxMin.z || b.z > boxMax.z || b.y < boxMin.y || b.y > boxMax.y) continue;
                if (r.bounds.size.x < 1.5f && r.bounds.size.z < 1.5f) continue; // 큰 판만
                Debug.Log($"[DemoViewDiag] south renderer '{r.gameObject.name}' (root={r.transform.root.name}) center={b} size={r.bounds.size}");
                if (++listed >= 8) break;
            }
        }
    }

    static async Task CapturePlayCameraAsync()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "DemoViewShots"));
        Directory.CreateDirectory(root);
        string file = Path.Combine(root, $"{ShotLabel()}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (camera == null)
            throw new InvalidOperationException("Main camera not found for capture.");

        var cameraController = camera.GetComponent<CameraController>()
            ?? camera.GetComponentInParent<CameraController>();
        bool controllerWasEnabled = cameraController != null && cameraController.enabled;
        if (cameraController != null)
            cameraController.enabled = false;
        try
        {
            // 실제 추적 카메라가 준비한 현재 구도를 고정한 뒤 일반 GameView 프레임만 캡처한다.
            await PA_SafeGameViewCapture.CaptureAsync(file, camera, captureCamera =>
            {
                captureCamera.cullingMask = -1;
            }, CaptureWidth, CaptureHeight, 1000);
        }
        finally
        {
            if (cameraController != null)
                cameraController.enabled = controllerWasEnabled;
        }

        Debug.Log($"PA DemoViewCapture: saved {file}");
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
        bool ran = _ran || SessionState.GetBool(RanKey, false);

        Cleanup();
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseBool(EnteredKey);
        SessionState.EraseBool(RanKey);
        SessionState.EraseBool(HadErrorKey);

        if (!ran || hadError)
        {
            Debug.LogError("PA DemoViewCapture failed. Check log.");
            EditorApplication.Exit(1);
        }

        Debug.Log("PA DemoViewCapture finished successfully.");
        EditorApplication.Exit(0);
    }

    static void Cleanup()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnEditorUpdate;
        _runtimeTask = null;
    }
}
#endif
