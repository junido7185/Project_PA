using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// §레퍼런스.html — 📱 스마트폰 UI
// 좌하단에 숨겨져 있다가 P 키 또는 마우스 근접으로 화면 중앙까지 떠오른다.
// 인벤토리와 상호배타 (한 쪽이 열리면 다른 쪽은 강제로 닫힌다).
//
// 기존 폰 계층을 사용하며, 홈에는 연결된 앱만 노출한다.
public class SmartphoneUI : MonoBehaviour
{
    static Sprite roundedSprite;
    static Sprite wallpaperSprite;
    public static readonly Color Cream = new Color(.98f, .95f, .87f);
    public static readonly Color Ink = new Color(.10f, .22f, .24f);
    public static readonly Color Teal = new Color(.12f, .39f, .41f);
    public static Sprite RoundedSprite => roundedSprite != null ? roundedSprite : roundedSprite = MakeRoundedSprite();

    static Sprite MakeRoundedSprite()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "PA Phone Rounded UI";
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Max(13f - x, 0, x - 50f);
            float dy = Mathf.Max(13f - y, 0, y - 50f);
            byte alpha = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(14f - Mathf.Sqrt(dx * dx + dy * dy)));
            pixels[y * size + x] = new Color32(255, 255, 255, alpha);
        }
        texture.SetPixels32(pixels); texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(26, 26, 26, 26));
    }

    static Sprite WallpaperSprite()
    {
        if (wallpaperSprite != null) return wallpaperSprite;
        var texture = new Texture2D(2, 128, TextureFormat.RGBA32, false);
        texture.name = "PA Phone Wallpaper";
        var pixels = new Color[256];
        for (int y = 0; y < 128; y++)
        {
            var color = Color.Lerp(new Color(.73f, .43f, .35f), new Color(.16f, .49f, .51f), y / 127f);
            pixels[y * 2] = pixels[y * 2 + 1] = color;
        }
        texture.SetPixels(pixels); texture.wrapMode = TextureWrapMode.Clamp; texture.Apply();
        return wallpaperSprite = Sprite.Create(texture, new Rect(0, 0, 2, 128), new Vector2(.5f, .5f));
    }

    static Image Decor(Transform parent, string name, Vector2 min, Vector2 max, Vector2 offsetMin,
        Vector2 offsetMax, Color color, bool rounded = true)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        var image = go.GetComponent<Image>();
        image.color = color; image.raycastTarget = false;
        if (rounded) { image.sprite = RoundedSprite; image.type = Image.Type.Sliced; }
        return image;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string value, int size, Color color,
        Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax, bool bold = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        var label = go.GetComponent<TextMeshProUGUI>();
        label.text = value; label.fontSize = size; label.color = color;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        return label;
    }
    public static SmartphoneUI instance;

    [Header("참조")]
    public RectTransform root;          // 이동 대상 (SmartphoneContainer 자신)
    public RectTransform hoverTrigger;  // 마우스 근접 감지 영역 (보통 root 와 동일)
    public GameObject    homeScreen;    // 홈(앱 그리드) — 패널 진입 시 비활성/복귀 시 활성
    public GameObject[]  tabPanels;     // 4개 패널 (감사/채용/피드/설정)
    public Button[]      tabButtons;    // 4개 탭 버튼

    [Header("앵커 위치 (anchoredPosition 기준)")]
    // 기준: Canvas 1920x1080, 폰 380x680, anchor/pivot = (0,0) 좌하단
    // 데이브 더 다이버 레퍼런스: 폰 윗부분 80~140px 만 peek 되도록 설계
    // ⚠️ hiddenPos/peekPos 는 캔버스 해상도에 무관하게 좌하단 기준이라 고정값 OK.
    //    activePos 는 런타임에 ComputeActivePos() 로 계산 — Free Aspect 대응.
    public Vector2 hiddenPos = new Vector2(40f, -600f);    // 상단 80px peek (대부분 화면 밖)
    public Vector2 peekPos   = new Vector2(40f, -540f);    // 상단 140px peek (호버 시)

    [Header("트랜지션")]
    public float transitionTime = 0.45f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("마우스 근접 감지")]
    public float hoverPadding = 80f;    // hoverTrigger 바깥 허용 거리

    [Header("탭 색상")]
    public Color tabNormalColor   = new Color(1f, 1f, 1f, 0.9f);
    public Color tabSelectedColor = new Color(0.96f, 0.84f, 0.43f, 1f); // #F5D76E

    public bool IsOpen => _isOpen;
    public int CurrentTabIndex => _currentTab;

    bool      _isOpen;
    Coroutine _moveCo;
    int       _currentTab;
    Canvas    _canvas; // activePos 계산용 캔버스 참조
    TextMeshProUGUI _statusLabel;
    Vector2 ClosedPosition => PlayerInputHandler.Instance?.FirstDayControls == true && root != null
        ? new Vector2(24f, -root.rect.height - 24f) : hiddenPos;
    Color[]   _tabBaseColors;
    int _shopTab = -1, _recordTab = -1, _recordSeenSerial;
    Image _recordBadge;

    void Awake()
    {
        instance = this;
        if (root == null) root = transform as RectTransform;
        if (hoverTrigger == null) hoverTrigger = root;
        _canvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        // 초기 위치: 좌하단 숨김
        if (root != null) root.anchoredPosition = ClosedPosition;
        CacheTabButtonColors();
        BuildShopApp();

        // P 키 구독
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnPhoneToggle += Toggle;

        // 탭 버튼 콜백 (앱 아이콘 클릭 → 해당 패널만 활성화)
        if (tabButtons != null)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                int captured = i; // 클로저 캡처
                tabButtons[i].onClick.AddListener(() => SelectTab(captured));
            }
        }

        // ⚠ v3 변경: SelectTab(0) 자동 호출 제거.
        //   기존: Start 직후 AuditPanel 이 켜져 홈 그리드를 가렸음 (스크린샷 1 의 버그).
        //   변경: 시작 시 모든 패널 비활성 → 홈 그리드만 노출. 사용자가 앱 아이콘 클릭 시
        //         SelectTab(idx) 가 해당 패널을 띄움. 패널의 BackButton 이 닫음.
        ReturnToHome();
    }

    // 기존 폰 계층을 런타임에서 확장한다. 씬의 직렬화 참조는 그대로 유지한다.
    void BuildShopApp()
    {
        if (homeScreen == null || tabPanels == null || tabButtons == null ||
            homeScreen.transform.parent == null || homeScreen.transform.Find("ShopPanel_Tile") != null) return;

        if (root != null && root.TryGetComponent(out Image body))
        {
            body.sprite = RoundedSprite; body.type = Image.Type.Sliced;
            body.color = new Color(.045f, .10f, .12f);
            var shadow = root.GetComponent<Shadow>() ?? root.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.015f, .07f, .08f, .63f);
            shadow.effectDistance = new Vector2(11f, -15f);
            root.localRotation = Quaternion.Euler(0, 0, 1.3f);
            Decor(root, "MetalRim", Vector2.zero, Vector2.one, new Vector2(5, 5), new Vector2(-5, -5),
                new Color(.22f, .35f, .35f)).transform.SetAsFirstSibling();
            Decor(root, "SideKey", new Vector2(1, .57f), new Vector2(1, .71f),
                new Vector2(0, 0), new Vector2(5, 0), new Color(.17f, .27f, .28f));
        }
        var screenTransform = root != null ? root.Find("PhoneScreen") ?? root.Find("PhoneBezel/PhoneScreen") : null;
        if (screenTransform != null)
        {
            if (screenTransform.TryGetComponent(out Image screen))
            {
                screen.sprite = RoundedSprite; screen.type = Image.Type.Sliced; screen.color = Cream;
            }
            if (screenTransform.GetComponent<Mask>() == null) screenTransform.gameObject.AddComponent<Mask>();
            _statusLabel = screenTransform.Find("StatusBar")?.GetComponent<TextMeshProUGUI>();
            if (_statusLabel != null)
            {
                _statusLabel.text = "P.A.  ·  홈"; _statusLabel.fontSize = 14; _statusLabel.color = Cream;
            }
            Decor(screenTransform, "TopBar", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -50), Vector2.zero, Teal, false).transform.SetAsFirstSibling();
            var close = screenTransform.Find("CloseButton");
            if (close != null && close.TryGetComponent(out Image closeImage))
            {
                closeImage.sprite = RoundedSprite; closeImage.type = Image.Type.Sliced;
                closeImage.color = new Color(1, 1, 1, .18f);
                var closeText = close.GetComponentInChildren<TextMeshProUGUI>();
                if (closeText != null) closeText.color = Cream;
            }
            var hint = screenTransform.Find("CloseHint")?.GetComponent<TextMeshProUGUI>();
            if (hint != null) { hint.text = "P  닫기     ESC  뒤로"; hint.color = Ink; hint.fontSize = 14; }
            Decor(screenTransform, "Speaker", new Vector2(.43f, 1), new Vector2(.57f, 1),
                new Vector2(0, -5), new Vector2(0, -2), new Color(.02f, .07f, .08f)).transform.SetAsLastSibling();
        }
        var grid = homeScreen.GetComponent<GridLayoutGroup>();
        if (grid != null) grid.enabled = false;
        foreach (var button in tabButtons)
        {
            if (button == null) continue;
            button.gameObject.SetActive(false); // 기존 준비 중 앱은 홈에 표시하지 않는다.
        }
        var wallpaper = homeScreen.GetComponent<Image>() ?? homeScreen.AddComponent<Image>();
        wallpaper.sprite = WallpaperSprite(); wallpaper.color = Color.white; wallpaper.raycastTarget = false;
        Label(homeScreen.transform, "HomeTitle", "나의 하루", 28, Cream,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -83), new Vector2(-20, -34), true);
        Label(homeScreen.transform, "HomeSubtitle", "마을의 하루를 여기서 이어가요", 16,
            new Color(1, 1, 1, .86f), new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(10, -116), new Vector2(-10, -82));
        // Canon v2 §22: HUD 알림은 잠깐 보이고, 놓친 안내는 휴대폰 기록에서 다시 본다.
        // 기록 패널은 탭 순서를 추측하지 않고 FeedUI가 붙은 기존 패널을 찾는다.
        _recordTab = System.Array.FindIndex(tabPanels, p => p != null && p.GetComponentInChildren<FeedUI>(true) != null);
        float shopX = _recordTab >= 0 ? -74f : 0f;
        var buttonShop = HomeTile("ShopPanel_Tile", shopX, new Color(.98f, .86f, .62f), "상점", "영업 · 매출");
        // 간결한 가게 그림을 UI 도형으로 그려 폰트 글리프와 외부 이미지에 의존하지 않는다.
        Decor(buttonShop.transform, "ShopRoof", new Vector2(.20f, .59f), new Vector2(.80f, .73f),
            Vector2.zero, Vector2.zero, new Color(.71f, .31f, .27f));
        Decor(buttonShop.transform, "ShopFront", new Vector2(.27f, .27f), new Vector2(.73f, .61f),
            Vector2.zero, Vector2.zero, Cream);
        Decor(buttonShop.transform, "ShopDoor", new Vector2(.46f, .27f), new Vector2(.60f, .48f),
            Vector2.zero, Vector2.zero, Teal);
        if (_recordTab >= 0)
        {
            var record = HomeTile("RecordPanel_Tile", 74f, new Color(.80f, .91f, .86f), "기록", "놓친 안내");
            // 메모지와 줄 세 개(글리프 없이 도형만).
            Decor(record.transform, "Paper", new Vector2(.27f, .20f), new Vector2(.73f, .80f), Vector2.zero, Vector2.zero, Cream);
            for (int i = 0; i < 3; i++)
                Decor(record.transform, "Line" + i, new Vector2(.35f, .62f - i * .14f), new Vector2(.65f, .66f - i * .14f),
                    Vector2.zero, Vector2.zero, Teal, false);
            _recordBadge = Decor(record.transform, "NewBadge", new Vector2(.80f, .80f), new Vector2(.80f, .80f),
                new Vector2(-13, -13), new Vector2(13, 13), new Color(.84f, .33f, .27f));
            _recordBadge.gameObject.SetActive(false);
            int recordTab = _recordTab;
            record.onClick.AddListener(() => SelectTab(recordTab));
            var feedTitle = tabPanels[_recordTab].GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(t => t.text == "피드");
            if (feedTitle != null) feedTitle.text = "기록";
        }
        Decor(homeScreen.transform, "HomeDivider", new Vector2(.16f, 1), new Vector2(.84f, 1),
            new Vector2(0, -389), new Vector2(0, -387), new Color(1, 1, 1, .35f), false);
        Label(homeScreen.transform, "HomeFooter", "마을의 유일한 잡화점", 17, Cream,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(18, 50), new Vector2(-18, 88));

        var panel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(homeScreen.transform.parent, false);
        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = Cream;
        panel.AddComponent<ShopManagementPhoneUI>();
        panel.SetActive(false);
        int index = tabPanels.Length;
        System.Array.Resize(ref tabPanels, index + 1);
        System.Array.Resize(ref tabButtons, index + 1);
        tabPanels[index] = panel; tabButtons[index] = buttonShop;
        _shopTab = index;
        CacheTabButtonColors();
    }

    Button HomeTile(string name, float x, Color color, string caption, string hint)
    {
        var tile = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        tile.transform.SetParent(homeScreen.transform, false);
        var tileRect = (RectTransform)tile.transform;
        tileRect.anchorMin = tileRect.anchorMax = new Vector2(.5f, 1);
        tileRect.pivot = new Vector2(.5f, 1);
        tileRect.sizeDelta = new Vector2(124, 124);
        tileRect.anchoredPosition = new Vector2(x, -160);
        var tileImage = tile.GetComponent<Image>();
        tileImage.sprite = RoundedSprite; tileImage.type = Image.Type.Sliced;
        tileImage.color = color;
        var tileShadow = tile.AddComponent<Shadow>();
        tileShadow.effectColor = new Color(.18f, .12f, .12f, .5f);
        tileShadow.effectDistance = new Vector2(5, -7);
        var button = tile.GetComponent<Button>();
        button.colors = new ColorBlock { normalColor = Color.white, highlightedColor = new Color(.85f, 1f, .95f),
            pressedColor = new Color(.70f, .91f, .85f), selectedColor = Color.white,
            disabledColor = new Color(.65f, .68f, .63f), colorMultiplier = 1f, fadeDuration = .12f };
        Label(homeScreen.transform, name.Replace("_Tile", "_Caption"), caption, 22, Cream,
            new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(x - 70, -323), new Vector2(x + 70, -286), true);
        Label(homeScreen.transform, name.Replace("_Tile", "_Hint"), hint, 16, new Color(1, 1, 1, .9f),
            new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(x - 70, -350), new Vector2(x + 70, -319));
        return button;
    }

    // 마지막으로 기록을 연 뒤 새 안내가 남았으면 홈의 기록 타일에 점을 표시한다.
    void RefreshRecordBadge()
    {
        if (_recordBadge != null)
            _recordBadge.gameObject.SetActive(FirstDayWorldPresentation.DispatchSerial > _recordSeenSerial);
    }

    // 모든 패널 비활성화 → 홈 화면(앱 그리드) 만 보이게
    public void ReturnToHome()
    {
        if (tabPanels != null)
            foreach (var p in tabPanels)
                if (p != null) p.SetActive(false);
        if (homeScreen != null) homeScreen.SetActive(true);
        _currentTab = -1;
        if (_statusLabel != null) _statusLabel.text = "P.A.  ·  홈";
        RestoreTabButtonColors();
        RefreshRecordBadge();
    }

    void CacheTabButtonColors()
    {
        if (tabButtons == null)
        {
            _tabBaseColors = null;
            return;
        }

        _tabBaseColors = new Color[tabButtons.Length];
        for (int i = 0; i < tabButtons.Length; i++)
        {
            var img = tabButtons[i] != null ? tabButtons[i].GetComponent<Image>() : null;
            _tabBaseColors[i] = img != null ? img.color : Color.white;
        }
    }

    void RestoreTabButtonColors()
    {
        if (tabButtons == null) return;
        if (_tabBaseColors == null || _tabBaseColors.Length != tabButtons.Length)
            CacheTabButtonColors();

        for (int i = 0; i < tabButtons.Length; i++)
        {
            var img = tabButtons[i] != null ? tabButtons[i].GetComponent<Image>() : null;
            if (img != null && _tabBaseColors != null && i < _tabBaseColors.Length)
                img.color = _tabBaseColors[i];
        }
    }

    void OnDestroy()
    {
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnPhoneToggle -= Toggle;
    }

    void Update()
    {
        // 열린 상태에서는 hover peek 동작하지 않음
        if (_isOpen) return;
        if (PlayerInputHandler.Instance?.FirstDayControls == true)
        {
            if (root != null && _moveCo == null) root.anchoredPosition = ClosedPosition;
            return;
        }
        if (root == null || hoverTrigger == null) return;
        if (Mouse.current == null) return;

        Vector2 mouse = Mouse.current.position.ReadValue();
        bool near = IsMouseNear(hoverTrigger, mouse, hoverPadding);

        // 트랜지션 중이 아니면 스냅하듯 타겟 변경
        Vector2 targetPos = near ? peekPos : hiddenPos;
        if ((root.anchoredPosition - targetPos).sqrMagnitude > 0.1f
            && _moveCo == null)
        {
            StartMove(targetPos, transitionTime * 0.5f); // hover 전환은 빠르게
        }
    }

    // 📱 외부 토글 진입점 (P 키 or 버튼)
    // 닫힐 때는 항상 홈으로 복귀시켜 다음 열기에서 깨끗한 상태 보장.
    public void Toggle()
    {
        _isOpen = !_isOpen;
        if (_isOpen)
        {
            NpcBubbleUI.HideAll();
            RefreshRecordBadge();
        }

        // 상호배타: 열릴 때 인벤토리가 열려있으면 강제로 닫는다.
        if (_isOpen
            && InventoryUI.instance != null
            && InventoryUI.instance.gameObject.activeSelf)
        {
            InventoryUI.instance.Toggle();
        }

        // 🚪 열릴 때 activePos 를 캔버스 실제 크기 기준으로 재계산 (Free Aspect 대응)
        Vector2 target = _isOpen ? ComputeActivePos() : ClosedPosition;
        StartMove(target, transitionTime);

        // 닫힐 때: 홈 복귀 + 커서 정상 복구
        if (!_isOpen)
        {
            ReturnToHome();
        }

        // 커서 잠금: 폰이 열려 있을 때만 해제 — 닫히면 항상 게임 상태로 복구
        ApplyCursorState();
    }

    // 명시적 닫기 — UI 닫기 버튼/ESC 가 호출. 이미 닫혀있으면 noop.
    public void Close()
    {
        if (!_isOpen) return;
        Toggle();
    }

    // ESC 처리 — 패널이 열려 있으면 홈으로, 홈만 보이면 폰 자체를 닫는다.
    public void OnEscape()
    {
        if (!_isOpen) return;
        if (_currentTab >= 0)
        {
            // 앱 패널 → 홈 복귀
            ReturnToHome();
        }
        else
        {
            // 홈 → 폰 닫기
            Close();
        }
    }

    // 닫힘 상태에서 게임 커서로 복구 (안전한 단일 진입점)
    void ApplyCursorState()
    {
        PlayerInputHandler.RestoreGameplayCursor();
    }

    // 캔버스 실제 rect 크기를 기반으로 스마트폰이 정중앙에 오는 anchoredPosition 계산.
    // anchor/pivot 이 (0,0) 좌하단이므로: (canvasW - phoneW) / 2, (canvasH - phoneH) / 2
    Vector2 ComputeActivePos()
    {
        if (root == null) return new Vector2(770f, 200f);
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) return new Vector2(770f, 200f);

        Vector2 canvasSize = ((RectTransform)_canvas.transform).rect.size;
        Vector2 phoneSize  = root.rect.size;
        return new Vector2(
            (canvasSize.x - phoneSize.x) * 0.5f,
            (canvasSize.y - phoneSize.y) * 0.5f
        );
    }

    public void SelectTab(int index)
    {
        if (tabPanels == null || tabPanels.Length == 0 || !_isOpen) return;
        _currentTab = Mathf.Clamp(index, 0, tabPanels.Length - 1);
        if (_statusLabel != null) _statusLabel.text = _currentTab == _shopTab ? "P.A.  ·  상점" :
            _currentTab == _recordTab ? "P.A.  ·  기록" : "P.A.";
        if (_currentTab == _recordTab)
        {
            _recordSeenSerial = FirstDayWorldPresentation.DispatchSerial;
            RefreshRecordBadge();
        }

        // 홈 화면 비활성 — 패널이 아이콘 위에 겹쳐 보이는 시각 버그 방지
        if (homeScreen != null) homeScreen.SetActive(false);

        for (int i = 0; i < tabPanels.Length; i++)
        {
            if (tabPanels[i] != null) tabPanels[i].SetActive(i == _currentTab);

            if (tabButtons != null && i < tabButtons.Length && tabButtons[i] != null)
            {
                var img = tabButtons[i].GetComponent<Image>();
                if (img != null) img.color = (i == _currentTab) ? tabSelectedColor : tabNormalColor;
            }
        }
    }

    public void OpenShopApp()
    {
        if (!_isOpen) Toggle();
        if (_shopTab >= 0) SelectTab(_shopTab);
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────────────────────

    void StartMove(Vector2 target, float duration)
    {
        if (_moveCo != null) StopCoroutine(_moveCo);
        _moveCo = StartCoroutine(MoveCoroutine(target, duration));
    }

    IEnumerator MoveCoroutine(Vector2 target, float duration)
    {
        if (root == null) yield break;
        Vector2 start = root.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = easeCurve.Evaluate(Mathf.Clamp01(t / duration));
            root.anchoredPosition = Vector2.LerpUnclamped(start, target, k);
            yield return null;
        }
        root.anchoredPosition = target;
        _moveCo = null;
    }

    static bool IsMouseNear(RectTransform rt, Vector2 screenPoint, float padding)
    {
        // RectTransform 의 월드 모서리 4개 → 스크린 좌표 AABB 로 근접 판정
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        float minX = Mathf.Min(corners[0].x, corners[2].x) - padding;
        float maxX = Mathf.Max(corners[0].x, corners[2].x) + padding;
        float minY = Mathf.Min(corners[0].y, corners[2].y) - padding;
        float maxY = Mathf.Max(corners[0].y, corners[2].y) + padding;
        return screenPoint.x >= minX && screenPoint.x <= maxX
            && screenPoint.y >= minY && screenPoint.y <= maxY;
    }
}
