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
        public string Mbti => profile == null ? "미확인" :
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
    TextMeshProUGUI _departureLabel;
    bool _hooked;
    float _certifiedAt = -1f;
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
        if (prefab != null)
        {
            var continuation = Instantiate(prefab); continuation.name = "PA_DepartureContinuation";
            continuation.AddComponent<OpeningFeelPresentation>();
        }
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
        if (Tutorial.Complete && _certifiedAt < 0) _certifiedAt = Time.unscaledTime;
        var key = UnityEngine.InputSystem.Keyboard.current;
        if (Tutorial.Complete && !IsOpen && !IsConfirmed && Time.unscaledTime > _certifiedAt + .35f &&
            key != null && (key.enterKey.wasPressedThisFrame || key.spaceKey.wasPressedThisFrame)) OpenAfterCertification();
        else if (IsOpen && !IsConfirmed && key != null && key.enterKey.wasPressedThisFrame)
        {
            Confirm();
        }
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
        PlayerInputHandler.RestoreGameplayCursor();
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
        _count.text = string.Join(" + ", _confirmed.Select(id => CandidateName(candidates.First(c => c.id == id)))) + "  ·  동행 확정";
        if (_departureLabel != null) _departureLabel.text = "출항 준비 완료";
        Debug.Log("[VS-P1] CONFIRMED " + string.Join(",", _confirmed));
        DepartureConfirmed?.Invoke(ConfirmedIds);
    }

    public void HideSelection()
    {
        if (_canvas != null) _canvas.enabled = false;
        IsOpen = false;
    }

    // 저장 복원은 같은 선택 상태를 사용하며 새 선택 결과를 만들지 않는다.
    public bool RestoreConfirmedSelection(string[] ids)
    {
        if (ids == null || ids.Length != 2 || ids.Distinct().Count() != 2 ||
            ids.Any(id => !candidates.Any(c => c.id == id))) return false;
        if (IsConfirmed) return ConfirmedIds.SequenceEqual(ids);
        OpenSelection();
        _selected.Clear();
        _selected.AddRange(ids);
        Refresh();
        Confirm();
        return IsConfirmed;
    }

    void Refresh()
    {
        string names = string.Join(" · ", _selected.Select(id => CandidateName(candidates.First(c => c.id == id))));
        _count.text = _selected.Count == 0 ? "동행 0 / 2명 · 직업과 첫 상품을 보고 골라 주세요"
            : $"동행 {_selected.Count} / 2명 · {names}";
        if (_departureLabel != null)
            _departureLabel.text = _selected.Count == 2 ? "선택 확정 후 출항 [Enter]"
                : $"{2 - _selected.Count}명을 더 선택하세요";
        DepartureButton.interactable = _selected.Count == 2;
        DepartureButton.GetComponent<Image>().color = _selected.Count == 2 ? Teal : new Color(.58f, .61f, .56f);
        for (int i = 0; i < candidates.Length; i++)
        {
            bool selected = _selected.Contains(candidates[i].id);
            _cards[i].color = selected ? new Color(.83f, .90f, .79f) : Color.white;
            _selectionLabels[i].text = selected ? "선택됨 · 다시 눌러 해제" : _selected.Count == 2 ? "다른 동행을 해제하면 선택 가능" : "+  동행으로 선택";
        }
    }

    // 원본 후보/프로필은 보존하고 확인된 제작용 접미사만 화면에서 숨긴다.
    static string CandidateName(Candidate candidate)
    {
        string name = candidate.displayName;
        if (string.IsNullOrWhiteSpace(name) && candidate.profile != null) name = candidate.profile.npcName;
        return PlayerText(name, "이름 미확인");
    }

    static string PlayerText(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        const string suffix = " · TEMP";
        string text = value.Trim();
        if (text.EndsWith(suffix, StringComparison.Ordinal))
            text = text.Substring(0, text.Length - suffix.Length).TrimEnd();
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
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
        Label(background, "Company", "P.A. COMPANY   /   개척 동행", 70, 32, 1300, 35, 22, Teal);
        Label(background, "Title", "누구와 함께 개척을 시작할까요?", 70, 83, 1750, 70, 45, Ink);
        Label(background, "Context", "3명 중 2명을 선택하세요 · 함께할 직업과 준비할 첫 상품을 비교해 보세요.", 74, 166, 1730, 45, 27, Ink);
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
            button.onClick.AddListener(() =>
            {
                Toggle(id);
                // A pointer-selected card must not receive Enter as another toggle.
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            });
            CandidateButtons[i] = button;
            Panel(card, "Accent", 0, 0, 580, 10, c.accent);
            Label(card, "Number", "동행 후보 0" + (i + 1), 30, 29, 510, 30, 20, Teal);
            Label(card, "Profession", PlayerText(c.profession, "직업 미확인"), 30, 78, 510, 64, 44, Ink);
            Label(card, "Name", CandidateName(c), 32, 148, 510, 40, 28, Ink);
            Label(card, "Field", "생산 분야  ·  " + PlayerText(c.production, "정보 미확인"), 32, 222, 510, 44, 26, Ink);
            Label(card, "Goods", "첫 상품  ·  " + PlayerText(c.firstGoods, "정보 미확인"), 32, 275, 510, 62, 27, Ink);
            Label(card, "Future", "성장 방향  ·  " + PlayerText(c.future, "정보 미확인"), 32, 345, 516, 50, 22, Teal);
            Label(card, "Personality", PlayerText(c.personality, "소개 정보 미확인"), 32, 407, 516, 64, 24, Ink);
            string temperament = c.profile == null || c.Mbti.Contains("?") ? "성향 정보 미확인" : "성향  ·  " + c.Mbti;
            Label(card, "MBTI", temperament, 32, 480, 516, 36, 20, Teal);
            Panel(card, "SelectionDivider", 32, 528, 516, 2, c.accent);
            _selectionLabels[i] = Label(card, "Selection", "", 32, 548, 516, 38, 23, Teal);
        }
        _count = Label(background, "Count", "", 74, 904, 1250, 48, 28, Ink);
        Label(background, "Note", "선택을 바꾸려면 카드를 다시 누르세요 · 성장 방향은 이후의 직업 안내예요.", 74, 966, 1260, 36, 22, Teal);
        var depart = Panel(background, "Depart", 1420, 908, 430, 96, Teal);
        depart.GetComponent<Image>().raycastTarget = true;
        DepartureButton = depart.gameObject.AddComponent<Button>();
        DepartureButton.targetGraphic = depart.GetComponent<Image>();
        DepartureButton.onClick.AddListener(Confirm);
        _departureLabel = Label(depart, "Label", "동행 2명을 선택하세요", 16, 20, 398, 56, 25, Paper);
        _departureLabel.alignment = TextAlignmentOptions.Center;
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
