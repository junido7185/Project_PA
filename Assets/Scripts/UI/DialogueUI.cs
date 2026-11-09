using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §3 DialogueUI — 하단 바 대화창. NpcDialogue.ShowLine() 에서 호출.
// 자동빌드 싱글톤 — MoneyHUD 패턴.
// Show(name, line) → 타이핑 효과 → autoHide 후 자동 닫힘.
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI instance;

    // §Week12 — 시나리오/튜토리얼이 "대화창이 한 번이라도 열렸는가" 를 관찰할 수 있게 expose.
    // _panel 은 private 이라 외부에서 활성 상태를 알기 어려워 추가.
    public static bool IsOpen => instance != null
        && instance._panel != null && instance._panel.activeInHierarchy;

    // 지금 말하는 상대. 카메라 대화 구도(CameraController)와 동행의 멈춰 바라보기(DemoCompanionRoutine)가 읽는다.
    public static Transform Speaker { get; private set; }
    public static bool TalkingWith(Transform who) => who != null && IsOpen && Speaker == who;
    public static int HeartsShown { get; private set; } = -1;

    [Header("직접 연결 (선택)")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI bodyText;

    [Header("설정")]
    public float typeSpeed  = 0.03f; // 글자당 딜레이(초)
    public float autoHide   = 4f;    // 마지막 글자 출력 후 자동 숨김(초)

    GameObject _panel;
    Coroutine  _typingCoroutine;
    Coroutine  _hideCoroutine;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (nameText == null || bodyText == null) BuildUI();
        _panel.SetActive(false);
    }

    void BuildUI()
    {
        var uiRoot = GameObject.Find("PA_UIRoot");
        Transform parent;

        if (uiRoot != null)
        {
            parent = uiRoot.transform;
        }
        else
        {
            var canvasGO = new GameObject("DialogueUI_Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas             = canvasGO.GetComponent<Canvas>();
            canvas.renderMode      = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder    = 80;
            var scaler             = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode     = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            parent = canvasGO.transform;
        }

        // 하단 바 패널
        _panel = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image));
        var rt  = (RectTransform)_panel.transform;
        rt.SetParent(parent, false);
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.sizeDelta        = new Vector2(0f, 160f);
        rt.anchoredPosition = Vector2.zero;

        var bg   = _panel.GetComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.88f);
        bg.raycastTarget = false;

        // NPC 이름 라벨 (상단)
        var nameGO = new GameObject("NpcNameText", typeof(TextMeshProUGUI));
        var nameRT = (RectTransform)nameGO.transform;
        nameRT.SetParent(_panel.transform, false);
        nameRT.anchorMin        = new Vector2(0f, 1f);
        nameRT.anchorMax        = new Vector2(1f, 1f);
        nameRT.pivot            = new Vector2(0f, 1f);
        nameRT.sizeDelta        = new Vector2(0f, 36f);
        nameRT.anchoredPosition = new Vector2(20f, 0f);

        nameText              = nameGO.GetComponent<TextMeshProUGUI>();
        nameText.fontSize     = 18;
        nameText.fontStyle    = FontStyles.Bold;
        nameText.color        = new Color(1f, 0.85f, 0.4f);
        nameText.raycastTarget = false;

        // 대사 본문 (중하단)
        var bodyGO = new GameObject("DialogueBodyText", typeof(TextMeshProUGUI));
        var bodyRT = (RectTransform)bodyGO.transform;
        bodyRT.SetParent(_panel.transform, false);
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(20f, 12f);
        bodyRT.offsetMax = new Vector2(-20f, -40f);

        bodyText              = bodyGO.GetComponent<TextMeshProUGUI>();
        bodyText.fontSize     = 22;
        bodyText.color        = Color.white;
        bodyText.alignment    = TextAlignmentOptions.TopLeft;
        bodyText.raycastTarget = false;
    }

    // NpcDialogue.ShowLine() 에서 호출
    public void Show(string npcName, string line) => Show(npcName, line, null, null);

    // speaker: 말하는 NPC(카메라 구도·서로 바라보기). friendshipId: 이름표 하트(FriendshipService 누적 점수).
    public void Show(string npcName, string line, Transform speaker, string friendshipId)
    {
        if (_panel == null) return;
        _panel.SetActive(true);
        Speaker = speaker;
        // 안전망: 어떤 호출 경로든 내부 ID("Farmer_01")·"· TEMP" 가 이름표에 보이지 않게 한다(사람 이름은 그대로).
        if (nameText != null) nameText.text = ResidentNames.Sanitize(npcName);
        if (PAUiTheme.Active) ApplyTheme(friendshipId);
        // 말을 걸면 플레이어가 멈춰 선 채 상대를 돌아본다(이동 입력이 들어오면 취소).
        var player = Inventory.instance != null ? Inventory.instance.GetComponent<PlayerController>() : null;
        if (speaker != null && player != null && Vector3.Distance(player.transform.position, speaker.position) < 6f)
            player.TurnToward(speaker.position);

        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        if (_hideCoroutine   != null) StopCoroutine(_hideCoroutine);
        _typingCoroutine = StartCoroutine(TypeLine(line));
    }

    // 2026-11-03 레퍼런스(딩컴 대화창 04/05 장면) 구조 반영: 크림 본문 패널 + 왼쪽 위로 걸친 청록 이름표(이름 + 친밀도 하트) + 아래 그림자.
    // 색·서체·둥근 면은 PA 토큰(DESIGN.md)을 그대로 쓴다. 테두리 선은 쓰지 않는다.
    RectTransform _tab;
    Image[] _heartFills;
    GameObject _heartRow;

    void ApplyTheme(string friendshipId)
    {
        var panelImage = _panel.GetComponent<Image>();
        PAUiTheme.Surface(panelImage, PAUiTheme.Cream);
        var shadow = _panel.GetComponent<Shadow>() ?? _panel.AddComponent<Shadow>();
        shadow.effectColor = new Color(0,0,0,.30f); shadow.effectDistance = new Vector2(0,-10);
        var panel = (RectTransform)_panel.transform;
        panel.anchorMin = panel.anchorMax = new Vector2(.5f,0); panel.pivot = new Vector2(.5f,0);
        panel.sizeDelta = new Vector2(880,148); panel.anchoredPosition = new Vector2(0,150);

        if (_tab == null)
        {
            _tab = PAUiTheme.Panel(_panel.transform, "NameTab", PAUiTheme.Teal).rectTransform;
            _tab.GetComponent<Image>().raycastTarget = false;
            _tab.anchorMin = _tab.anchorMax = _tab.pivot = new Vector2(0,1);
            _tab.anchoredPosition = new Vector2(30,26);
            var tabShadow = _tab.gameObject.AddComponent<Shadow>();
            tabShadow.effectColor = new Color(0,0,0,.22f); tabShadow.effectDistance = new Vector2(0,-4);
            nameText.rectTransform.SetParent(_tab, false);
            _heartRow = new GameObject("Hearts", typeof(RectTransform));
            _heartRow.transform.SetParent(_tab, false);
            var row = (RectTransform)_heartRow.transform;
            row.anchorMin = row.anchorMax = new Vector2(1,.5f); row.pivot = new Vector2(0,.5f);
            row.sizeDelta = new Vector2(HeartStep * 5, 24);
            _heartFills = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var back = new GameObject("Heart" + i, typeof(RectTransform), typeof(Image));
                back.transform.SetParent(row, false);
                var backRect = (RectTransform)back.transform;
                backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(0,.5f);
                backRect.sizeDelta = new Vector2(24,22); backRect.anchoredPosition = new Vector2(i * HeartStep,0);
                var backImage = back.GetComponent<Image>();
                backImage.sprite = HeartSprite; backImage.color = new Color(1,1,1,.28f); backImage.raycastTarget = false;
                var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fill.transform.SetParent(back.transform, false);
                var fillRect = (RectTransform)fill.transform;
                fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one; fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
                var fillImage = fill.GetComponent<Image>();
                fillImage.sprite = HeartSprite; fillImage.color = PAUiTheme.Gold; fillImage.raycastTarget = false;
                fillImage.type = Image.Type.Filled; fillImage.fillMethod = Image.FillMethod.Horizontal;
                _heartFills[i] = fillImage;
            }
        }

        nameText.color = Color.white; nameText.fontSize = 24; nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.MidlineLeft; nameText.textWrappingMode = TextWrappingModes.NoWrap;
        var nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0,0); nameRect.anchorMax = new Vector2(0,1); nameRect.pivot = new Vector2(0,.5f);
        float nameWidth = Mathf.Ceil(nameText.GetPreferredValues(nameText.text, 600, 40).x);
        nameRect.sizeDelta = new Vector2(nameWidth, 0); nameRect.anchoredPosition = new Vector2(TabPad,0);

        bool hearts = TryHearts(friendshipId, out float[] fills);
        _heartRow.SetActive(hearts);
        HeartsShown = -1;
        float heartsWidth = 0f;
        if (hearts)
        {
            HeartsShown = 0;
            for (int i = 0; i < 5; i++) { _heartFills[i].fillAmount = fills[i]; if (fills[i] >= 1f) HeartsShown++; }
            heartsWidth = 16 + HeartStep * 5 - 3;
            ((RectTransform)_heartRow.transform).anchoredPosition = new Vector2(-(TabPad + HeartStep * 5 - 3), 0);
        }
        _tab.sizeDelta = new Vector2(TabPad + nameWidth + heartsWidth + TabPad, 52);

        bodyText.color = PAUiTheme.Ink; bodyText.fontSize = PAUiTheme.BodySize;
        var bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = Vector2.zero; bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(32,18); bodyRect.offsetMax = new Vector2(-32,-40);
    }

    const float TabPad = 22f, HeartStep = 27f;

    // 대화로 점수가 오른 직후(같은 E 한 번) 이름표 하트를 다시 채운다.
    public void RefreshHearts(string friendshipId)
    {
        if (_tab != null && IsOpen && PAUiTheme.Active) ApplyTheme(friendshipId);
    }

    // 하트 한 칸 = 친밀도 한 단계. 채워지는 중인 칸은 다음 단계까지의 진행만큼 차오른다(대화 하루 1회·선물·구매 점수).
    static bool TryHearts(string friendshipId, out float[] fills)
    {
        fills = new float[5];
        var service = FriendshipService.Instance;
        if (string.IsNullOrEmpty(friendshipId) || service == null || service.levelThresholds == null) return false;
        int level = service.GetLevel(friendshipId), points = service.GetPoints(friendshipId);
        var thresholds = service.levelThresholds;
        for (int i = 0; i < fills.Length; i++)
        {
            if (i < level) fills[i] = 1f;
            else if (i == level && i < thresholds.Length)
            {
                int previous = i > 0 ? thresholds[i - 1] : 0;
                fills[i] = Mathf.Clamp01((points - previous) / (float)Mathf.Max(1, thresholds[i] - previous));
            }
        }
        return true;
    }

    // 하트 모양은 이모지·기호 대신 런타임에 그린다(DESIGN.md §4). (x²+y²−1)³ − x²y³ ≤ 0, 4×4 표본으로 가장자리를 부드럽게.
    static Sprite _heartSprite;
    static Sprite HeartSprite
    {
        get
        {
            if (_heartSprite != null) return _heartSprite;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "PA_DialogueHeart" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int inside = 0;
                for (int sy = 0; sy < 4; sy++)
                for (int sx = 0; sx < 4; sx++)
                {
                    float u = ((x + (sx + .5f) / 4f) / size - .5f) * 2.7f;
                    float v = ((y + (sy + .5f) / 4f) / size - .45f) * 2.7f;
                    float a = u * u + v * v - 1f;
                    if (a * a * a - u * u * v * v * v <= 0f) inside++;
                }
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(inside * 255 / 16));
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            _heartSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
            return _heartSprite;
        }
    }

    IEnumerator TypeLine(string line)
    {
        if (bodyText == null) yield break;
        _typing = true; _fullLine = line;
        bodyText.text = "";
        foreach (char c in line)
        {
            bodyText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        _typing = false;
        _hideCoroutine = StartCoroutine(AutoHide());
    }

    // 2026-11-04 레퍼런스 04·05: [E]로 대화 넘기기 — 글자가 나오는 중이면 한 번에 다 보이고, 다 보인 뒤면 닫는다.
    // 다 나온 줄에는 아래 가운데에 '다음 ▼'를 띄운다(자동으로 닫히는 기존 동작은 그대로).
    bool _typing;
    string _fullLine;
    TextMeshProUGUI _nextMark;
    public static int Advances { get; private set; }

    public static bool TryAdvance()
    {
        if (!IsOpen || !PAUiTheme.Active) return false;
        var ui = instance;
        Advances++;
        if (ui._typing)
        {
            if (ui._typingCoroutine != null) ui.StopCoroutine(ui._typingCoroutine);
            ui._typing = false;
            if (ui.bodyText != null && ui._fullLine != null) ui.bodyText.text = ui._fullLine;
            if (ui._hideCoroutine != null) ui.StopCoroutine(ui._hideCoroutine);
            ui._hideCoroutine = ui.StartCoroutine(ui.AutoHide());
            return true;
        }
        ui.Hide();
        return true;
    }

    void Update()
    {
        if (_panel == null || !_panel.activeInHierarchy || !PAUiTheme.Active) return;
        if (_nextMark == null)
        {
            _nextMark = new GameObject("NextMark", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            _nextMark.transform.SetParent(_panel.transform, false);
            var rt = _nextMark.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, 0f); rt.pivot = new Vector2(.5f, 0f);
            rt.sizeDelta = new Vector2(120, 26);
            _nextMark.text = "E ▼"; _nextMark.fontSize = 18; _nextMark.fontStyle = FontStyles.Bold;
            _nextMark.alignment = TextAlignmentOptions.Center; _nextMark.color = PAUiTheme.Teal; _nextMark.raycastTarget = false;
            if (bodyText != null) _nextMark.font = bodyText.font;
        }
        bool done = !_typing;
        if (_nextMark.enabled != done) _nextMark.enabled = done;
        if (done) _nextMark.rectTransform.anchoredPosition = new Vector2(0f, 8f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 5f);
    }

    IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(autoHide);
        if (_panel != null) _panel.SetActive(false);
        Speaker = null;
        // 친밀도 UI 도 함께 숨김 — 대화창이 사라질 때 잔류하지 않도록
        if (FriendshipUI.instance != null) FriendshipUI.instance.Hide();
    }

    // Space 키 등으로 즉시 닫기
    public void Hide()
    {
        _typing = false;
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        if (_hideCoroutine   != null) StopCoroutine(_hideCoroutine);
        if (_panel != null) _panel.SetActive(false);
        Speaker = null;
        // 친밀도 UI 동기 숨김
        if (FriendshipUI.instance != null) FriendshipUI.instance.Hide();
    }
}
