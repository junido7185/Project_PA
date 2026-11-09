using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Only responds to project-local production commands; reuses the installed Coplay bridge.
[InitializeOnLoad]
public static class PA_ProductionEnvironment
{
    [Serializable] class Request { public string id; public string action; }
    [Serializable] class Response { public string id; public string action; public bool success; public string detail; public string path; }
    static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    static string Folder => Path.Combine(Root, "Logs", "ParallelProduction");
    static bool busy;
    static double nextPoll;
    static int frames;
    static double start;
    static double totalMs;
    static double maxMs;
    static Request measurement;

    static PA_ProductionEnvironment() { EditorApplication.update += Poll; }

    static async void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll || busy || EditorApplication.isCompiling) return;
        nextPoll = EditorApplication.timeSinceStartup + .5;
        string path = Path.Combine(Folder, "editor-request.json");
        if (!File.Exists(path)) return;
        Request request;
        try { request = JsonUtility.FromJson<Request>(File.ReadAllText(path)); }
        catch { return; } // Atomic replacement may be in progress; no mutation.
        if (request == null || string.IsNullOrEmpty(request.id) || request.id == SessionState.GetString("PA.Production.LastRequest", "")) return;
        SessionState.SetString("PA.Production.LastRequest", request.id);
        busy = true;
        try
        {
            switch (request.action)
            {
                case "connect": await Connect(request); break;
                case "build": Build(request); break;
                case "capture":
                    if (!EditorApplication.isPlaying) throw new InvalidOperationException("GameView capture requires Play Mode");
                    string screenshot = Path.Combine(Folder, "gameview-" + request.id + ".png");
                    ScreenCapture.CaptureScreenshot(screenshot);
                    Finish(request, true, "Actual rendered GameView; wait for PNG write", screenshot); break;
                case "performance":
                    if (!EditorApplication.isPlaying) throw new InvalidOperationException("Performance requires Play Mode");
                    measurement = request; frames = 0; totalMs = 0; maxMs = 0; start = EditorApplication.timeSinceStartup;
                    EditorApplication.update += Measure;
                    break;
                default: throw new InvalidOperationException("Unsupported production action");
            }
        }
        catch (Exception ex) { Finish(request, false, ex.GetType().Name + ": " + ex.Message); }
        finally { busy = false; }
    }

    static async Task Connect(Request request)
    {
        // Reflection avoids a new assembly dependency; the installed package remains authority.
        var locator = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MCPForUnity.Editor.Services.MCPServiceLocator")).FirstOrDefault(t => t != null);
        if (locator == null) throw new InvalidOperationException("Installed Coplay package unavailable");
        bool reachable;
        using (var tcp = new TcpClient())
        {
            try { await tcp.ConnectAsync("127.0.0.1", 8080); reachable = true; }
            catch { reachable = false; }
        }
        if (!reachable)
        {
            object server = locator.GetProperty("Server").GetValue(null);
            var method = server.GetType().GetMethod("TryGetLocalHttpServerCommandParts", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) throw new InvalidOperationException("Installed server command API unavailable");
            object[] parts = { null, null, null, null };
            if (!(bool)method.Invoke(server, parts)) throw new InvalidOperationException("Cannot resolve cached MCP server: " + parts[3]);
            var info = new ProcessStartInfo((string)parts[0], (string)parts[1]) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Root };
            info.EnvironmentVariables["UV_OFFLINE"] = "1";
            // Does not stop an existing process, clear caches, download or modify global preferences.
            Process.Start(info);
            await Task.Delay(1500);
        }
        object bridge = locator.GetProperty("Bridge").GetValue(null);
        bool connected = await (Task<bool>)bridge.GetType().GetMethod("StartAsync").Invoke(bridge, null);
        Finish(request, connected, connected ? "Existing Coplay HTTP transport connected" : "Bridge did not connect; inspect Console");
    }

    static void Build(Request request)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before building");
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) throw new InvalidOperationException("No existing build scenes enabled");
        string output = Path.Combine(Folder, "Candidates", request.id, "ProjectPA.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = output,
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
        Finish(request, report.summary.result == BuildResult.Succeeded,
            "Windows candidate " + report.summary.result + "; errors=" + report.summary.totalErrors +
            "; bytes=" + report.summary.totalSize + "; seconds=" + report.summary.totalTime.TotalSeconds, output);
    }

    static void Measure()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Measure; Finish(measurement, false, "Play stopped before 15-second sample"); return; }
        if (Time.unscaledDeltaTime > 0) { frames++; double ms = Time.unscaledDeltaTime * 1000; totalMs += ms; maxMs = Math.Max(maxMs, ms); }
        if (EditorApplication.timeSinceStartup - start < 15) return;
        EditorApplication.update -= Measure;
        Finish(measurement, true, "Editor Play sample (not standalone): samples=" + frames + "; meanFrameMs=" + (totalMs / Math.Max(1, frames)) + "; maxFrameMs=" + maxMs);
    }

    static void Finish(Request request, bool success, string detail, string path = null)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Path.Combine(Folder, "editor-result-" + request.id + ".json"),
            JsonUtility.ToJson(new Response { id = request.id, action = request.action, success = success, detail = detail, path = path }, true));
        if (!success) Debug.LogError("PA production environment: " + detail);
        else Debug.Log("PA production environment: " + detail);
    }
}
