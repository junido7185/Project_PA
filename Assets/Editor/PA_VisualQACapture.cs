using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// Captures the current real GameView only. It does not drive gameplay, move the
// camera, load scenes, or manufacture review states.
public static class PA_VisualQACapture
{
    const string MenuRoot = "Tools/Project PA/Visual QA/";
    static bool _captureInProgress;

    [MenuItem(MenuRoot + "Capture S01 Arrival")]
    public static void CaptureS01Arrival() => Capture("S01_Arrival.png");

    [MenuItem(MenuRoot + "Capture S02 HUD")]
    public static void CaptureS02Hud() => Capture("S02_HUD.png");

    [MenuItem(MenuRoot + "Capture S03 Inventory-Hotbar-Held Item")]
    public static void CaptureS03Inventory() => Capture("S03_Inventory.png");

    [MenuItem(MenuRoot + "Capture S04 Pickup")]
    public static void CaptureS04Pickup() => Capture("S04_Pickup.png");

    [MenuItem(MenuRoot + "Capture S05 Gathering")]
    public static void CaptureS05Gathering() => Capture("S05_Gathering.png");

    [MenuItem(MenuRoot + "Capture S06 Placement")]
    public static void CaptureS06Placement() => Capture("S06_Placement.png");

    [MenuItem(MenuRoot + "Capture S07 Settlement")]
    public static void CaptureS07Settlement() => Capture("S07_Settlement.png");

    static async void Capture(string fileName)
    {
        try
        {
            await CaptureAsync(fileName);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Visual QA] Capture failed: {exception.GetBaseException().Message}");
        }
    }

    static async Task CaptureAsync(string fileName)
    {
        if (_captureInProgress)
            throw new InvalidOperationException("A Visual QA capture is already in progress.");
        if (!EditorApplication.isPlaying || EditorApplication.isPaused)
            throw new InvalidOperationException("Enter unpaused Play Mode and reach the real review state before capturing.");

        Camera camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera == null)
            throw new InvalidOperationException("No active game camera was found.");

        string outputPath = Path.GetFullPath(Path.Combine(
            Application.dataPath, "..", "Logs", "VisualQA", fileName));

        _captureInProgress = true;
        try
        {
            // 기존 검증기의 Full HD GameView preset을 재사용한다. Screen.SetResolution만으로는
            // Editor의 Free Aspect 캡처 크기가 바뀌지 않는다.
            PA_DepartureContinuationChecks.ConfigureGameView();
            await PA_SafeGameViewCapture.CaptureAsync(
                outputPath, camera, null, 1920, 1080, 800);
            Debug.Log($"[Visual QA] Captured current runtime state: {outputPath}");
        }
        finally
        {
            _captureInProgress = false;
        }
    }
}
