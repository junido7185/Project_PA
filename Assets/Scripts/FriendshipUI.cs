using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §6 FriendshipUI — 대화창 하단 친밀도 레벨 표시.
// DialogueUI.Show() 호출 시 ShowForNpc(id) 를 함께 호출하면 업데이트된다.
// 자동빌드: DialogueUI 아래 슬롯에 붙이거나 독립 Canvas 로 생성.
public class FriendshipUI : MonoBehaviour
{
    public static FriendshipUI instance;

    [Header("직접 연결 (선택)")]
    public TextMeshProUGUI levelText;
    public Slider          levelSlider;

    GameObject _panel;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (levelText == null || levelSlider == null) BuildUI();
        _panel.SetActive(false);
    }

    void Start()
    {
        if (FriendshipService.Instance != null)
            FriendshipService.Instance.OnLevelChanged += OnLevelChanged;
    }

    void OnDestroy()
    {
        if (FriendshipService.Instance != null)
            FriendshipService.Instance.OnLevelChanged -= OnLevelChanged;
    }

    // NpcDialogue.ShowLine 과 동시에 호출해 현재 NPC 친밀도 갱신
    public void ShowForNpc(string friendshipId)
    {
        if (string.IsNullOrEmpty(friendshipId) || FriendshipService.Instance == null) return;
        int level  = FriendshipService.Instance.GetLevel(friendshipId);
        int points = FriendshipService.Instance.GetPoints(friendshipId);
        RefreshDisplay(friendshipId, level, points);
        if (_panel != null) _panel.SetActive(true);
    }

    public void Hide()
    {
        if (_panel != null) _panel.SetActive(false);
    }

    void OnLevelChanged(string id, int _, int newLevel)
    {
        if (FriendshipService.Instance == null) return;
        int points = FriendshipService.Instance.GetPoints(id);
        RefreshDisplay(id, newLevel, points);
    }

    void RefreshDisplay(string id, int level, int points)
    {
        if (levelText != null)
            levelText.text = $"친밀도 Lv.{level}";

        if (levelSlider != null && FriendshipService.Instance != null)
        {
            var thresholds = FriendshipService.Instance.levelThresholds;
            int maxLv      = FriendshipService.Instance.MaxLevel;

            if (level >= maxLv)
            {
                levelSlider.value = 1f;
            }
            else
            {
                int prev = level > 0 ? thresholds[level - 1] : 0;
                int next = thresholds[level];
                levelSlider.value = next > prev
                    ? Mathf.Clamp01((float)(points - prev) / (next - prev))
                    : 1f;
            }
        }
    }

    void BuildUI()
    {
        var uiRoot = GameObject.Find("PA_UIRoot");
        Transform parent = uiRoot != null ? uiRoot.transform : transform;

        _panel = new GameObject("FriendshipPanel", typeof(RectTransform), typeof(Image));
        var rt  = (RectTransform)_panel.transform;
        rt.SetParent(parent, false);
        // DialogueUI (Y=0, h=160) 바로 위에 붙임
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0.35f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.sizeDelta        = new Vector2(0f, 36f);
        rt.anchoredPosition = new Vector2(0f, 160f);

        _panel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.85f);

        // 레벨 텍스트
        var textGO = new GameObject("LevelText", typeof(TextMeshProUGUI));
        var trt    = (RectTransform)textGO.transform;
        trt.SetParent(_panel.transform, false);
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(0.45f, 1f);
        trt.offsetMin = new Vector2(10f, 4f);
        trt.offsetMax = new Vector2(-4f, -4f);

        levelText           = textGO.GetComponent<TextMeshProUGUI>();
        levelText.text      = "친밀도 Lv.0";
        levelText.fontSize  = 14;
        levelText.color     = new Color(1f, 0.85f, 0.4f);
        levelText.raycastTarget = false;

        // 슬라이더
        var sliderGO = new GameObject("LevelSlider", typeof(RectTransform));
        var sliderRT = (RectTransform)sliderGO.transform;
        sliderRT.SetParent(_panel.transform, false);
        sliderRT.anchorMin = new Vector2(0.45f, 0.2f);
        sliderRT.anchorMax = new Vector2(0.95f, 0.8f);
        sliderRT.offsetMin = Vector2.zero;
        sliderRT.offsetMax = Vector2.zero;

        var bg   = new GameObject("BG", typeof(Image));
        bg.transform.SetParent(sliderGO.transform, false);
        var bgRT = (RectTransform)bg.transform;
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform, false);
        var faRT = (RectTransform)fillArea.transform;
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = Vector2.zero; faRT.offsetMax = Vector2.zero;

        var fill  = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRT = (RectTransform)fill.transform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0f, 1f);
        fillRT.sizeDelta = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(1f, 0.6f, 0.2f);

        levelSlider             = sliderGO.AddComponent<Slider>();
        levelSlider.minValue    = 0f;
        levelSlider.maxValue    = 1f;
        levelSlider.value       = 0f;
        levelSlider.fillRect    = fillRT;
        levelSlider.interactable = false;
    }
}
