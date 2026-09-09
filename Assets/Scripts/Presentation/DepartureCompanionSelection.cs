using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Opening session only: companionship is not hiring, residency, or a saved campaign.
public sealed class DepartureCompanionSelection : MonoBehaviour
{
    [Serializable]
    public sealed class Candidate
    {
        public string id, displayName, profession, production, firstGoods, future, personality;
        public NpcProfile profile;
        public GameObject model;
        public Color accent;
        public string Mbti => profile == null ? "TEMP · 미확정" :
            Axis(profile.traitEI, "I", "E") + Axis(profile.traitSN, "S", "N") +
            Axis(profile.traitTF, "T", "F") + Axis(profile.traitJP, "J", "P");
        static string Axis(float value, string low, string high) => value == 0 ? "?" : value < 0 ? low : high;
    }

    public Candidate[] candidates;
    public TMP_FontAsset font;
    public IReadOnlyList<string> SelectedIds => _selected.AsReadOnly();
    public IReadOnlyList<string> ConfirmedIds => Array.AsReadOnly(_confirmed);
    public bool IsOpen { get; private set; }
    public bool IsConfirmed => _confirmed.Length == 2;
    public Button DepartureButton { get; private set; }
    public Button[] CandidateButtons { get; private set; }
    public event Action<IReadOnlyList<string>> DepartureConfirmed;
    public DepartureTutorialController Tutorial { get; private set; }

    readonly List<string> _selected = new List<string>(2);
    string[] _confirmed = Array.Empty<string>();
    Canvas _canvas;
    Image[] _cards;
    TextMeshProUGUI[] _selectionLabels;
    TextMeshProUGUI _count;
    bool _hooked;
    static readonly Color Ink = new Color(.17f, .23f, .23f);
    static readonly Color Paper = new Color(.97f, .94f, .85f);
    static readonly Color Teal = new Color(.22f, .45f, .44f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
        SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != DepartureTutorialController.SceneName || FindFirstObjectByType<DepartureCompanionSelection>() != null) return;
        var prefab = Resources.Load<GameObject>("DepartureTutorial/DepartureContinuation");
        if (prefab != null) Instantiate(prefab).name = "PA_DepartureContinuation";
    }

    void Start()
    {
        Tutorial = FindFirstObjectByType<DepartureTutorialController>();
        if (Tutorial == null || candidates == null || candidates.Length != 3 ||
            candidates.Any(c => c.profile == null || c.model == null || string.IsNullOrEmpty(c.id)) ||
            candidates.Select(c => c.id).Distinct().Count() != 3)
        {
            Debug.LogError("[VS-P1] Missing or duplicate companion reference.");
            enabled = false;
        }
    }

    void Update()
    {
        if (Tutorial == null || Tutorial.presentation == null || Tutorial.presentation.CompanionButton == null) return;
        if (!_hooked)
        {
            Tutorial.presentation.CompanionButton.onClick.AddListener(OpenAfterCertification);
            _hooked = true;
        }
        Tutorial.presentation.CompanionButton.interactable = Tutorial.CompanionSelectionUnlocked && !IsConfirmed;
    }

    void OnDestroy()
    {
        if (_hooked && Tutorial != null && Tutorial.presentation != null && Tutorial.presentation.CompanionButton != null)
            Tutorial.presentation.CompanionButton.onClick.RemoveListener(OpenAfterCertification);
    }

    public void OpenAfterCertification()
    {
        if (Tutorial != null && Tutorial.CompanionSelectionUnlocked) OpenSelection();
    }

#if UNITY_EDITOR
    // Explicit editor entry into the same selection UI, without changing certification or save state.
    public void OpenDevelopmentSelection() => OpenSelection();
#endif

    void OpenSelection()
    {
        if (IsOpen || IsConfirmed || Tutorial == null) return;
        Tutorial.player.GetComponent<PlayerController>().enabled = false;
        Tutorial.player.GetComponent<PlayerInteraction>().enabled = false;
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
        BuildUI();
        IsOpen = true;
        Refresh();
    }

    public bool Toggle(string id)
    {
        if (!IsOpen || IsConfirmed || !candidates.Any(c => c.id == id)) return false;
        if (_selected.Contains(id)) _selected.Remove(id);
        else if (_selected.Count < 2) _selected.Add(id);
        else return false;
        Refresh();
        return true;
    }

    public void Confirm()
    {
        if (!IsOpen || IsConfirmed || _selected.Count != 2) return;
        _confirmed = _selected.ToArray();
        DepartureButton.interactable = false;
        foreach (var button in CandidateButtons) button.interactable = false;
        _count.text = string.Join(" + ", _confirmed.Select(id => candidates.First(c => c.id == id).profession)) + "  ·  출항 준비 완료";
        Debug.Log("[VS-P1] CONFIRMED " + string.Join(",", _confirmed));
        DepartureConfirmed?.Invoke(ConfirmedIds);
    }

    public void HideSelection()
    {
        if (_canvas != null) _canvas.enabled = false;
        IsOpen = false;
    }

    void Refresh()
    {
        _count.text = $"동행 {_selected.Count} / 2명  ·  함께 시작할 생산 분야를 선택하세요";
        DepartureButton.interactable = _selected.Count == 2;
        DepartureButton.GetComponent<Image>().color = _selected.Count == 2 ? Teal : new Color(.58f, .61f, .56f);
        for (int i = 0; i < candidates.Length; i++)
        {
            bool selected = _selected.Contains(candidates[i].id);
            _cards[i].color = selected ? new Color(.83f, .90f, .79f) : Color.white;
            _selectionLabels[i].text = selected ? "✓  동행 선택됨  ·  다시 눌러 취소" : _selected.Count == 2 ? "2명 선택 완료" : "+  함께 출발하기";
        }
    }

    void BuildUI()
    {
        var root = new GameObject("PA_CompanionSelection", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        _canvas = root.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        RectTransform background = Panel(root.transform, "Paper", 0, 0, 1920, 1080, Paper);
        Label(background, "Company", "P.A. COMPANY   /   PIONEER PARTNER", 70, 32, 1300, 35, 22, Teal);
        Label(background, "Title", "누구와 함께 개척을 시작할까요?", 70, 83, 1750, 70, 45, Ink);
        Label(background, "Context", "3명 중 2명  ·  당신의 선택으로 시작할 생산 분야가 달라집니다.", 74, 166, 1730, 45, 27, Ink);
        CandidateButtons = new Button[3];
        _cards = new Image[3];
        _selectionLabels = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++)
        {
            Candidate c = candidates[i];
            var card = Panel(background, "Candidate_" + c.id, 70 + i * 600, 255, 580, 605, Color.white);
            _cards[i] = card.GetComponent<Image>();
            _cards[i].raycastTarget = true;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = _cards[i];
            button.transition = Selectable.Transition.None;
            string id = c.id;
            button.onClick.AddListener(() => Toggle(id));
            CandidateButtons[i] = button;
            Panel(card, "Accent", 0, 0, 580, 10, c.accent);
            Label(card, "Number", "PARTNER 0" + (i + 1), 30, 29, 510, 30, 20, Teal);
            Label(card, "Profession", c.profession, 30, 83, 510, 67, 48, Ink);
            Label(card, "Name", c.displayName, 32, 159, 510, 36, 26, Ink);
            Label(card, "Field", "생산 분야     " + c.production, 32, 232, 510, 40, 26, Ink);
            Label(card, "Goods", "첫 상품         " + c.firstGoods, 32, 280, 510, 40, 26, Ink);
            Label(card, "Future", "향후 연결     " + c.future, 32, 328, 516, 40, 25, Ink);
            Label(card, "Personality", "성격 · TEMP\n" + c.personality, 32, 394, 516, 70, 23, Ink);
            Label(card, "MBTI", "MBTI  " + c.Mbti + "   ·  기존 성향값 / ? 미확정", 32, 474, 516, 40, 19, Teal);
            _selectionLabels[i] = Label(card, "Selection", "", 32, 543, 516, 42, 23, Teal);
        }
        _count = Label(background, "Count", "", 74, 904, 1250, 48, 28, Ink);
        Label(background, "Note", "동행자를 정하는 단계입니다. 향후 연결 직업은 성장 방향 안내입니다.", 74, 966, 1260, 36, 22, Teal);
        var depart = Panel(background, "Depart", 1420, 908, 430, 96, Teal);
        depart.GetComponent<Image>().raycastTarget = true;
        DepartureButton = depart.gameObject.AddComponent<Button>();
        DepartureButton.targetGraphic = depart.GetComponent<Image>();
        DepartureButton.onClick.AddListener(Confirm);
        var title = Label(depart, "Label", "이 두 사람과 출항  →", 20, 20, 390, 56, 28, Paper);
        title.alignment = TextAlignmentOptions.Center;
    }

    public static RectTransform Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = false;
        return rect;
    }

    public TextMeshProUGUI Label(Transform parent, string name, string text, float x, float y, float w, float h, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(w, h);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        return label;
    }
}
