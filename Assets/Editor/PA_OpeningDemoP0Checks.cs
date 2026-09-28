using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opening Demo P0: bounded text-only lifecycle probe. No save IO or scene serialization.
[InitializeOnLoad]
public static class PA_OpeningDemoP0Checks
{
    const string Key = "PA.OpeningP0Probe";
    static readonly StringBuilder Report = new StringBuilder();
    static int step;
    static double started;
    static int screenlessFrames;
    static bool skipped;
    static bool worldEntered;
    static bool previousRunInBackground;

    static PA_OpeningDemoP0Checks()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
            {
                previousRunInBackground = Application.runInBackground;
                Application.runInBackground = true;
                started = EditorApplication.timeSinceStartup; step = 0; skipped = false;
                worldEntered = false; screenlessFrames = 0; Report.Clear();
            }
        };
    }

    [MenuItem("Project PA/Verification/Opening Demo P0 Text Probe")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != DepartureTutorialController.SceneName || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Open the clean local Departure scene in Edit mode first.");
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (started == 0) started = EditorApplication.timeSinceStartup;
        try
        {
            if (EditorApplication.timeSinceStartup - started > 60)
            { CaptureArrival(); Finish("INCOMPLETE bounded timeout at step " + step); return; }
            if (step == 0)
            {
                var t = UnityEngine.Object.FindFirstObjectByType<DepartureTutorialController>();
                var s = UnityEngine.Object.FindFirstObjectByType<DepartureCompanionSelection>();
                if (t == null || !t.IsReady || s == null || s.Tutorial == null) return;
                // Movement/harvest fixture only; exercise actual hand selection, stock and price edges.
                Invoke(t, "SetStage", 3);
                Inventory.instance.TryReceiveToHotbar(t.fruit, 3);
                int index = Inventory.instance.hotbar.slots.FindIndex(slot => !slot.IsEmpty && slot.item == t.fruit);
                Inventory.instance.SelectHotbarSlot(index);
                t.player.GetComponent<EquipmentSystem>().Holster();
                Invoke(t, "ObserveHandHolster");
                Inventory.instance.SelectHotbarSlot(index);
                t.trainingSlot.Interact(t.player.gameObject);
                Report.AppendLine("First stock: stage=" + t.Stage + " handComplete=" + t.HandTrainingComplete + " stocked=" + !t.trainingSlot.IsEmpty);
                ShopPriceUI.instance.Open(t.trainingSlot);
                Invoke(ShopPriceUI.instance, "OnConfirm");
                Report.AppendLine("Same-frame first confirmation: stage=" + t.Stage + " buyer=" + t.buyer.GetFsmState() + " paused=" + t.buyer.IsSchedulePaused);
                s.OpenDevelopmentSelection();
                s.Toggle(s.candidates[0].id); s.Toggle(s.candidates[1].id); s.Confirm();
                step = 1;
            }
            if (step == 1)
            {
                if (!Camera.allCameras.Any(c => c.isActiveAndEnabled && c.targetTexture == null)) screenlessFrames++;
                if (!worldEntered && SceneManager.GetActiveScene().name == DemoRouteController.WorldScene)
                {
                    worldEntered = true;
                    Report.AppendLine("World loaded: players before adapter=" + UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
                }
                var voyage = UnityEngine.Object.FindFirstObjectByType<DepartureVoyagePresentation>();
                if (voyage != null && voyage.Sailing && !skipped)
                {
                    Report.AppendLine("Voyage screen cameras=" + Camera.allCameras.Count(c => c.targetTexture == null));
                    voyage.SkipTravelForSavedArrival(); skipped = true;
                }
                var alpha = WorldAlphaPlayableController.Instance;
                if (alpha == null || !alpha.IsReady || alpha.DemoRoute == null || !alpha.DemoRoute.IsPlayable) return;
                step = 2;
                return;
            }
            if (step == 2)
            {
                CaptureArrival();
                Finish("OBSERVED");
            }
        }
        catch (Exception ex) { Report.AppendLine(ex.ToString()); Finish("ERROR"); }
    }

    static void Invoke(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, args);
    static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;

    static void CaptureArrival()
    {
        Report.AppendLine("Arrival scene=" + SceneManager.GetActiveScene().name + " screenlessFrames=" + screenlessFrames);
        Report.AppendLine("TRANSITION overlay=" + (DemoRouteController.TransitionOverlay != null ? DemoRouteController.TransitionOverlay.name : "none") +
            " active=" + (DemoRouteController.TransitionOverlay != null && DemoRouteController.TransitionOverlay.activeInHierarchy));
        foreach (var p in UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Report.AppendLine("PLAYER " + Path(p.transform) + " pos=" + p.transform.position + " active=" + p.isActiveAndEnabled);
        foreach (var i in UnityEngine.Object.FindObjectsByType<Inventory>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Report.AppendLine("INVENTORY " + Path(i.transform) + " authority=" + (i == Inventory.instance));
        foreach (var a in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Report.AppendLine("CHARACTER " + Path(a.transform) + " pos=" + a.transform.position + " active=" + a.isActiveAndEnabled);
            foreach (var r in a.GetComponentsInChildren<Renderer>(true))
                Report.AppendLine(" MESH " + Path(r.transform) + " enabled=" + r.enabled + " bounds=" + r.bounds);
        }
        foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            Report.AppendLine("CAMERA " + Path(c.transform) + " enabled=" + c.enabled + " target=" + c.targetTexture + " tag=" + c.tag);
        Report.AppendLine("LISTENERS " + UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a => a.isActiveAndEnabled));
        foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Report.AppendLine("CANVAS " + Path(c.transform) + " enabled=" + c.enabled + " active=" + c.gameObject.activeInHierarchy);
        Report.AppendLine("CLOCK " + (GameClock.Instance != null ? GameClock.Instance.CurrentHour.ToString() : "none") +
            " enabled=" + (GameClock.Instance != null && GameClock.Instance.enabled));
        foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type == LightType.Directional))
            Report.AppendLine("SUN " + Path(l.transform) + " enabled=" + l.enabled + " color=" + l.color + " intensity=" + l.intensity + " rotation=" + l.transform.eulerAngles);
        Report.AppendLine("AMBIENT sky=" + RenderSettings.ambientSkyColor + " equator=" + RenderSettings.ambientEquatorColor + " ground=" + RenderSettings.ambientGroundColor);
    }

    static void Finish(string status)
    {
        Report.AppendLine(status);
        Directory.CreateDirectory("Logs/OpeningDemoP0");
        File.WriteAllText("Logs/OpeningDemoP0/Probe-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt", Report.ToString());
        SessionState.SetBool(Key, false);
        Application.runInBackground = previousRunInBackground;
        EditorApplication.isPlaying = false;
    }
}
