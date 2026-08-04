using TMPro;
using UnityEngine;
using UnityEngine.UI;

// §6 PauseManager — ESC 일시정지와 제품 수준의 세션 제어 메뉴.
// 저장/불러오기 상태의 권위는 SaveManager에 두고 이 클래스는 플레이어 입력만 연결한다.
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    public bool IsPaused { get; private set; }

    GameObject _overlay;
    TextMeshProUGUI _statusText;
    Button _resumeButton;
    Button _saveButton;
    Button _loadButton;
    Button _saveQuitButton;

    float _timeScaleBeforePause = 1f;
    CursorLockMode _cursorLockBeforePause = CursorLockMode.Locked;
    bool _cursorVisibleBeforePause;
    bool _hasSave;
    bool _busy;
    int _loadAvailabilityRequest;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildOverlay();
    }

    void Start()
    {
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnPauseToggle += Toggle;
    }

    void OnDestroy()
    {
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnPauseToggle -= Toggle;

        if (IsPaused)
            RestorePauseState();

        if (Instance == this)
            Instance = null;
    }

    public void Toggle()
    {
        // ESC 우선순위:
        //   1) 창고 닫기
        //   2) 제작 도감/작업대 화면 닫기
        //   3) 스마트폰 앱/홈 닫기
        //   4) 인벤토리 닫기
        //   5) 게임 플레이 중 일시정지 토글
        if (StorageUI.instance != null && StorageUI.instance.IsOpen)
        {
            StorageUI.instance.CloseBox();
            return;
        }

        if (CraftingUI.instance != null && CraftingUI.instance.IsOpen)
        {
            CraftingUI.instance.Close();
            return;
        }

        if (SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen)
        {
            SmartphoneUI.instance.OnEscape();
            return;
        }

        if (InventoryUI.instance != null && InventoryUI.instance.gameObject.activeSelf)
        {
            InventoryUI.instance.Toggle();
            return;
        }

        if (_busy)
            return;

        if (IsPaused)
        {
            Resume();
            return;
        }

        var playableDay = FindFirstObjectByType<PlayableDayScenarioController>();
        if (playableDay != null && !playableDay.StartupFlowCompleted)
            return;

        Pause();
    }

    public void Pause()
    {
        if (IsPaused)
            return;

        _timeScaleBeforePause = Time.timeScale;
        _cursorLockBeforePause = Cursor.lockState;
        _cursorVisibleBeforePause = Cursor.visible;

        IsPaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (_overlay != null)
        {
            _overlay.transform.SetAsLastSibling();
            _overlay.SetActive(true);
        }

        SetBusy(false);
        SetStatus("저장 상태를 확인하는 중...");
        _ = RefreshLoadAvailabilityAsync();
    }

    public void Resume()
    {
        if (!IsPaused)
            return;

        RestorePauseState();
        if (_overlay != null)
            _overlay.SetActive(false);
    }

    void RestorePauseState()
    {
        IsPaused = false;
        Time.timeScale = _timeScaleBeforePause;
        Cursor.lockState = _cursorLockBeforePause;
        Cursor.visible = _cursorVisibleBeforePause;
        _loadAvailabilityRequest++;
        _busy = false;
    }

    async System.Threading.Tasks.Task RefreshLoadAvailabilityAsync()
    {
        int request = ++_loadAvailabilityRequest;
        _hasSave = false;
        if (_loadButton != null)
            _loadButton.interactable = false;

        try
        {
            _hasSave = SaveManager.instance != null
                && await SaveManager.instance.HasSaveAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Pause] 저장 상태 확인 실패: {ex.Message}");
            _hasSave = false;
        }

        if (this == null || request != _loadAvailabilityRequest || !IsPaused)
            return;

        if (_loadButton != null)
            _loadButton.interactable = _hasSave && !_busy;

        if (!_hasSave && !_busy)
            SetStatus("불러올 저장본이 없습니다.");
        else if (_hasSave && !_busy && _statusText != null
            && _statusText.text == "저장 상태를 확인하는 중...")
            SetStatus("저장본을 불러오면 현재 진행 상태를 교체합니다.");
    }

    async void SaveFromPause()
    {
        if (_busy)
            return;

        if (SaveManager.instance == null)
        {
            SetStatus("저장 서비스를 찾지 못했습니다.", true);
            return;
        }

        SetBusy(true);
        SetStatus("게임을 저장하는 중...");

        try
        {
            await SaveManager.instance.SaveGameAsync();
            if (this == null)
                return;

            _hasSave = true;
            SetBusy(false);
            SetStatus("저장했습니다. 계속 플레이해도 안전합니다.");
        }
        catch (System.Exception ex)
        {
            if (this == null)
                return;

            Debug.LogError($"[Pause] 저장 실패: {ex.Message}");
            SetBusy(false);
            SetStatus("저장하지 못했습니다. 게임은 종료되지 않았습니다.", true);
        }
    }

    async void LoadFromPause()
    {
        if (_busy || !_hasSave)
            return;

        if (SaveManager.instance == null)
        {
            SetStatus("불러오기 서비스를 찾지 못했습니다.", true);
            return;
        }

        SetBusy(true);
        SetStatus("저장본을 불러오는 중...");

        try
        {
            await SaveManager.instance.LoadGameAsync();
            if (this == null)
                return;

            SetBusy(false);
            Debug.Log("[Pause] 저장본 불러오기 완료");
            Resume();
        }
        catch (System.Exception ex)
        {
            if (this == null)
                return;

            Debug.LogError($"[Pause] 불러오기 실패: {ex.Message}");
            SetBusy(false);
            SetStatus("불러오지 못했습니다. 현재 진행 상태를 유지합니다.", true);
        }
    }

    async void SaveAndQuitFromPause()
    {
        if (_busy)
            return;

        if (SaveManager.instance == null)
        {
            SetStatus("저장할 수 없어 종료를 취소했습니다.", true);
            return;
        }

        SetBusy(true);
        SetStatus("저장한 뒤 게임을 종료하는 중...");

        try
        {
            await SaveManager.instance.SaveGameAsync();
            if (this == null)
                return;

            _hasSave = true;
            SetStatus("저장 완료 · 게임을 종료합니다.");
            Application.Quit();

            // Application.Quit은 Editor 플레이 모드를 끝내지 않는다.
            if (Application.isEditor)
            {
                SetBusy(false);
                SetStatus("저장 완료 · 실제 빌드에서는 여기서 종료됩니다.");
            }
        }
        catch (System.Exception ex)
        {
            if (this == null)
                return;

            Debug.LogError($"[Pause] 저장 후 종료 실패: {ex.Message}");
            SetBusy(false);
            SetStatus("저장에 실패해 종료를 취소했습니다.", true);
        }
    }

    void SetBusy(bool busy)
    {
        _busy = busy;
        if (_resumeButton != null) _resumeButton.interactable = !busy;
        if (_saveButton != null) _saveButton.interactable = !busy;
        if (_loadButton != null) _loadButton.interactable = !busy && _hasSave;
        if (_saveQuitButton != null) _saveQuitButton.interactable = !busy;
    }

    void SetStatus(string message, bool isError = false)
    {
        if (_statusText == null)
            return;

        _statusText.text = message;
        _statusText.color = isError
            ? new Color(1f, 0.68f, 0.55f, 1f)
            : new Color(0.78f, 0.86f, 0.80f, 1f);
    }

    void BuildOverlay()
    {
        var uiRoot = GameObject.Find("PA_UIRoot");
        Transform parent;

        if (uiRoot != null)
        {
            parent = uiRoot.transform;
        }
        else
        {
            var canvasGo = new GameObject("PauseMenu_Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            parent = canvasGo.transform;
        }

        _overlay = new GameObject("PauseOverlay", typeof(RectTransform), typeof(Image));
        var overlayRt = (RectTransform)_overlay.transform;
        overlayRt.SetParent(parent, false);
        Stretch(overlayRt);
        _overlay.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.02f, 0.76f);

        var panel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(_overlay.transform, false);
        var panelRt = (RectTransform)panel.transform;
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(560f, 650f);
        panel.GetComponent<Image>().color = new Color(0.07f, 0.11f, 0.09f, 0.97f);

        var title = CreateText(panel.transform, "PauseLabel", "일시정지",
            new Vector2(0f, 246f), new Vector2(480f, 70f), 46f, Color.white, FontStyles.Bold);
        title.alignment = TextAlignmentOptions.Center;

        var subtitle = CreateText(panel.transform, "PauseSubtitle", "PROJECT P.A. · 오늘의 가게 운영",
            new Vector2(0f, 196f), new Vector2(470f, 38f), 18f,
            new Color(0.72f, 0.84f, 0.76f, 1f), FontStyles.Normal);
        subtitle.alignment = TextAlignmentOptions.Center;

        _resumeButton = CreateMenuButton(panel.transform, "ResumeButton", "계속하기  (ESC)",
            110f, new Color(0.12f, 0.48f, 0.30f, 1f), Resume);
        _saveButton = CreateMenuButton(panel.transform, "SaveButton", "게임 저장",
            38f, new Color(0.18f, 0.36f, 0.29f, 1f), SaveFromPause);
        _loadButton = CreateMenuButton(panel.transform, "LoadButton", "저장본 불러오기",
            -34f, new Color(0.18f, 0.31f, 0.35f, 1f), LoadFromPause);
        _saveQuitButton = CreateMenuButton(panel.transform, "SaveQuitButton", "저장 후 종료",
            -106f, new Color(0.43f, 0.23f, 0.20f, 1f), SaveAndQuitFromPause);

        _statusText = CreateText(panel.transform, "StatusText", "",
            new Vector2(0f, -205f), new Vector2(470f, 74f), 17f,
            new Color(0.78f, 0.86f, 0.80f, 1f), FontStyles.Normal);
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.textWrappingMode = TextWrappingModes.Normal;

        _overlay.SetActive(false);
    }

    Button CreateMenuButton(Transform parent, string name, string label, float y,
        Color background, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(390f, 58f);

        go.GetComponent<Image>().color = background;
        var button = go.GetComponent<Button>();
        button.onClick.AddListener(onClick);
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.82f, 0.88f, 0.84f, 1f);
        colors.disabledColor = new Color(0.44f, 0.48f, 0.45f, 0.65f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var text = CreateText(go.transform, "Label", label, Vector2.zero,
            new Vector2(360f, 48f), 21f, Color.white, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string value,
        Vector2 position, Vector2 size, float fontSize, Color color, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
