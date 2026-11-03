using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 전시(포스터·팸플릿) 촬영용 포토 모드. 모든 빌드에서 스스로 설치된다(게임플레이 코드는 참조하지 않는다).
// F12 = 2배 슈퍼샘플 스크린샷(<persistentDataPath>/Screenshots 또는 `-pa-shot-dir <폴더>`),
// F8  = 화면 공간 Canvas 숨김/복원(깨끗한 화면). F9는 PlayerInputHandler의 불러오기(OnLoad)라 쓰지 않는다.
// 저장 알림 토스트는 촬영 프레임에서 빠진 뒤에만 표시된다.
public sealed class ExhibitionPhotoMode : MonoBehaviour
{
    public const int SuperSize = 2;
    public const float ToastSeconds = 1.5f;
    public const string ArgShotDir = "-pa-shot-dir";
    public static ExhibitionPhotoMode Instance { get; private set; }
    public static bool HudHidden => Instance != null && Instance._hudHidden;
    public string ShotDirectory { get; private set; }
    public string LastSavedPath { get; private set; }
    public int SavedCount { get; private set; }
    public bool Capturing => _capturing;

    readonly List<Canvas> _hiddenByToggle = new List<Canvas>();
    bool _hudHidden, _capturing;
    Canvas _toastCanvas;
    CanvasGroup _toastGroup;
    TextMeshProUGUI _toastLabel;
    float _toastUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (Instance != null) return;
        var go = new GameObject("PA_ExhibitionPhotoMode");
        DontDestroyOnLoad(go);
        go.AddComponent<ExhibitionPhotoMode>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ShotDirectory = ResolveShotDirectory();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    // 씬이 바뀌면 숨김 상태를 풀어 남은(DontDestroyOnLoad) Canvas가 영구히 꺼져 있지 않게 한다.
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || !_hudHidden) return;
        RestoreCanvases(_hiddenByToggle);
        _hudHidden = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.f12Key.wasPressedThisFrame) RequestScreenshot();
            if (keyboard.f8Key.wasPressedThisFrame) ToggleHud();
        }
        if (_toastCanvas != null && _toastCanvas.enabled)
        {
            float remaining = _toastUntil - Time.unscaledTime;
            _toastGroup.alpha = Mathf.Clamp01(remaining / .3f);
            if (remaining <= 0f) _toastCanvas.enabled = false;
        }
    }

    public bool RequestScreenshot()
    {
        if (_capturing || !isActiveAndEnabled) return false;
        StartCoroutine(CaptureRoutine());
        return true;
    }

    public void ToggleHud() => SetHudHidden(!_hudHidden);

    public void SetHudHidden(bool hidden)
    {
        if (hidden == _hudHidden) return;
        if (hidden)
        {
            _hiddenByToggle.Clear();
            _hiddenByToggle.AddRange(HideScreenSpaceCanvases(_toastCanvas));
            _hudHidden = true;
            ShowToast("화면 UI 숨김 · F8로 다시 표시");
        }
        else
        {
            RestoreCanvases(_hiddenByToggle);
            _hudHidden = false;
            ShowToast("화면 UI 표시");
        }
    }

    IEnumerator CaptureRoutine()
    {
        _capturing = true;
        // 같은 Update에서 끄면 이번 프레임은 토스트 없이 그려진다.
        if (_toastCanvas != null) _toastCanvas.enabled = false;
        yield return new WaitForEndOfFrame();
        Texture2D shot = null;
        string error = null;
        try
        {
            shot = ScreenCapture.CaptureScreenshotAsTexture(SuperSize);
            if (shot == null) throw new InvalidOperationException("ScreenCapture returned no texture");
            Directory.CreateDirectory(ShotDirectory);
            string path = Path.Combine(ShotDirectory, $"PA_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
            File.WriteAllBytes(path, EncodeOpaquePng(shot));
            LastSavedPath = path;
            SavedCount++;
            Debug.Log($"📸 [PhotoMode] {shot.width}x{shot.height} 저장: {path}");
        }
        catch (Exception e)
        {
            error = e.Message;
            Debug.LogWarning("[PhotoMode] 스크린샷 실패: " + e.Message);
        }
        finally
        {
            if (shot != null) Destroy(shot);
            _capturing = false;
        }
        ShowToast(error == null ? "스크린샷 저장됨" : "스크린샷 저장 실패");
    }

    // 백버퍼 알파가 1이 아닐 때 인쇄용 PNG가 반투명해지지 않도록 RGB로 저장한다.
    public static byte[] EncodeOpaquePng(Texture2D source)
    {
        if (source == null) return null;
        if (source.format == TextureFormat.RGB24) return source.EncodeToPNG();
        var opaque = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
        try
        {
            opaque.SetPixels32(source.GetPixels32());
            opaque.Apply(false);
            return opaque.EncodeToPNG();
        }
        finally { Destroy(opaque); }
    }

    // 활성 화면 공간(Overlay/Camera) Canvas 컴포넌트를 끄고, 끈 목록을 돌려준다. World Space는 장면의 일부라 둔다.
    public static List<Canvas> HideScreenSpaceCanvases(Canvas keep = null)
    {
        var targets = new List<Canvas>();
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas == null || !canvas.enabled || canvas == keep) continue;
            Canvas root = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
            if (root.renderMode == RenderMode.WorldSpace) continue;
            targets.Add(canvas);
        }
        foreach (var canvas in targets) canvas.enabled = false;
        return targets;
    }

    // 숨긴 Canvas만 정확히 되돌린다(그사이 파괴된 것은 건너뛴다).
    public static void RestoreCanvases(List<Canvas> hidden)
    {
        if (hidden == null) return;
        foreach (var canvas in hidden)
            if (canvas != null) canvas.enabled = true;
        hidden.Clear();
    }

    public static string ResolveShotDirectory()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == ArgShotDir && !string.IsNullOrWhiteSpace(args[i + 1]))
                return args[i + 1];
        return Path.Combine(Application.persistentDataPath, "Screenshots");
    }

    void ShowToast(string message)
    {
        EnsureToast();
        _toastLabel.text = message;
        _toastGroup.alpha = 1f;
        _toastUntil = Time.unscaledTime + ToastSeconds;
        _toastCanvas.enabled = true;
    }

    void EnsureToast()
    {
        if (_toastCanvas != null) return;
        var go = new GameObject("PA_PhotoModeToast", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);
        _toastCanvas = go.GetComponent<Canvas>();
        _toastCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _toastCanvas.sortingOrder = 32760;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
        _toastGroup = go.GetComponent<CanvasGroup>();
        _toastGroup.interactable = false;
        _toastGroup.blocksRaycasts = false;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
        panel.transform.SetParent(go.transform, false);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -36f);
        rect.sizeDelta = new Vector2(400f, 78f);
        var image = panel.GetComponent<Image>();
        image.sprite = PAUiTheme.RoundedSprite;
        image.type = Image.Type.Sliced;
        image.color = PAUiTheme.Cream;
        image.raycastTarget = false;
        var outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(PAUiTheme.Ink.r, PAUiTheme.Ink.g, PAUiTheme.Ink.b, .35f);
        outline.effectDistance = new Vector2(2f, -2f);

        Label(panel.transform, "Tag", "P.A. COMPANY  /  PHOTO", 14f, PAUiTheme.Teal, -10f, 20f);
        _toastLabel = Label(panel.transform, "Message", string.Empty, 26f, PAUiTheme.Ink, -34f, 36f);
        _toastCanvas.enabled = false;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color, float y, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(380f, height);
        var label = go.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }
}
