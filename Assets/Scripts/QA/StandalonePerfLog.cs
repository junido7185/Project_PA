using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// P11 QA 전용: `-pa-perf-log`로 실행한 경우에만 Player.log에 실제 프레임 시간을 남긴다(인자가 없으면 아무것도 만들지 않는다).
// 게임플레이 코드는 이 컴포넌트를 참조하지 않는다. 2초마다 장면·해상도·평균/95%/최대 프레임 시간·FPS·플레이어 위치 한 줄.
public sealed class StandalonePerfLog : MonoBehaviour
{
    const float Window = 2f;
    readonly List<float> _frames = new List<float>(256);
    float _windowStart;
    StreamWriter _rawFrames;
    double _previousWallTime;

    void Awake()
    {
        _previousWallTime = Time.realtimeSinceStartupAsDouble;
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-pa-perf-frame-path");
        if (index < 0 || index + 1 >= args.Length) return;
        try
        {
            // Explicit QA output only, never overwrite a previous recording.
            string path = Path.GetFullPath(args[index + 1]);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            _rawFrames = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read));
            _rawFrames.WriteLine("frame,realtimeSeconds,wallMs,unityDeltaMs,scene,timeScale,captureFramerate");
            _rawFrames.Flush();
            Debug.Log("[PerfLog] raw frame output=" + path);
        }
        catch (Exception error) { Debug.LogWarning("[PerfLog] raw output unavailable: " + error.Message); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pa-perf-log") < 0) return;
        var go = new GameObject("PA_StandalonePerfLog");
        DontDestroyOnLoad(go);
        go.AddComponent<StandalonePerfLog>();
        Debug.Log($"⏱ [PerfLog] on · {SystemInfo.graphicsDeviceType} · {SystemInfo.graphicsDeviceName} · {SystemInfo.processorType} · vSync={QualitySettings.vSyncCount} targetFps={Application.targetFrameRate} quality={QualitySettings.names[QualitySettings.GetQualityLevel()]}");
        SceneManager.sceneLoaded += (scene, mode) => Debug.Log($"⏱ [PerfLog] scene loaded {scene.name} at {Time.realtimeSinceStartup:F1}s");
    }

    void Update()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        double wallMs = (now - _previousWallTime) * 1000d;
        _previousWallTime = now;
        if (_rawFrames != null)
            _rawFrames.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:F6},{2:F6},{3:F6},{4},{5:F3},{6}",
                Time.frameCount, now, wallMs, Time.unscaledDeltaTime * 1000f,
                SceneManager.GetActiveScene().name, Time.timeScale, Time.captureFramerate));
        _frames.Add(Time.unscaledDeltaTime * 1000f);
        if (Time.realtimeSinceStartup - _windowStart < Window) return;
        _frames.Sort();
        float sum = 0f; foreach (float ms in _frames) sum += ms;
        float avg = sum / _frames.Count;
        float p95 = _frames[Mathf.Min(_frames.Count - 1, Mathf.FloorToInt(_frames.Count * .95f))];
        float max = _frames[_frames.Count - 1];
        var player = Inventory.instance != null ? Inventory.instance.transform : null;
        string where = player != null ? player.position.ToString("F2") : "-";
        Debug.Log($"⏱ [PerfLog] t={Time.realtimeSinceStartup:F1}s scene={SceneManager.GetActiveScene().name} {Screen.width}x{Screen.height} frames={_frames.Count} avg={avg:F1}ms p95={p95:F1}ms max={max:F1}ms fps={1000f / avg:F0} player={where} timeScale={Time.timeScale:F1}");
        _frames.Clear();
        _rawFrames?.Flush();
        _windowStart = Time.realtimeSinceStartup;
    }

    void OnApplicationQuit() => CloseRawFrames();
    void OnDestroy() => CloseRawFrames();
    void CloseRawFrames() { _rawFrames?.Dispose(); _rawFrames = null; }
}
