using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §5 SettingsUI — SmartphoneUI 설정 탭(index 3) 에 붙이는 컴포넌트.
// BGM/SFX 볼륨 슬라이더 + 저장/로드 버튼.
public class SettingsUI : MonoBehaviour
{
    [Header("직접 연결 (선택)")]
    public Slider bgmSlider;
    public Slider sfxSlider;

    bool _built;

    void OnEnable()
    {
        if (!_built) { BuildLayout(); _built = true; }
        RefreshSliders();
    }

    void BuildLayout()
    {
        var vlg = gameObject.GetComponent<VerticalLayoutGroup>()
                  ?? gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing   = 14f;
        vlg.padding   = new RectOffset(12, 12, 16, 12);

        AddLabel("설정", 22, FontStyles.Bold, new Color(1f, 0.85f, 0.4f));

        AddLabel("BGM 볼륨", 17, FontStyles.Normal, Color.white);
        bgmSlider = AddSlider("BGMSlider", 0.6f, v => AudioManager.SetBGMVolume(v));

        AddLabel("SFX 볼륨", 17, FontStyles.Normal, Color.white);
        sfxSlider = AddSlider("SFXSlider", 1.0f, v => AudioManager.SetSFXVolume(v));

        AddButton("저장 (F5)", new Color(0.2f, 0.6f, 0.3f), () =>
        {
            if (SaveManager.instance != null) SaveManager.instance.SaveGame();
        });

        AddButton("로드 (F9)", new Color(0.2f, 0.3f, 0.7f), () =>
        {
            if (SaveManager.instance != null) SaveManager.instance.LoadGame();
        });
    }

    void RefreshSliders()
    {
        if (bgmSlider != null && AudioManager.Instance != null)
            bgmSlider.SetValueWithoutNotify(AudioManager.Instance.bgmVolume);
        if (sfxSlider != null && AudioManager.Instance != null)
            sfxSlider.SetValueWithoutNotify(AudioManager.Instance.sfxVolume);
    }

    void AddLabel(string text, float size, FontStyles style, Color color)
    {
        var go  = new GameObject(text, typeof(TextMeshProUGUI));
        go.transform.SetParent(transform, false);
        var le  = go.AddComponent<LayoutElement>();
        le.preferredHeight = 28f;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.fontStyle = style;
        tmp.color     = color;
        tmp.raycastTarget = false;
    }

    Slider AddSlider(string goName, float defaultVal, System.Action<float> onChange)
    {
        var go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 28f;

        // 배경
        var bg = new GameObject("BG", typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var bgRT     = (RectTransform)bg.transform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        var faRT     = (RectTransform)fillArea.transform;
        faRT.anchorMin = Vector2.zero;
        faRT.anchorMax = Vector2.one;
        faRT.offsetMin = Vector2.zero;
        faRT.offsetMax = Vector2.zero;

        var fill  = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRT = (RectTransform)fill.transform;
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(defaultVal, 1f);
        fillRT.sizeDelta = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f);

        var slider          = go.AddComponent<Slider>();
        slider.minValue     = 0f;
        slider.maxValue     = 1f;
        slider.value        = defaultVal;
        slider.fillRect     = fillRT;
        slider.onValueChanged.AddListener(v => onChange?.Invoke(v));
        return slider;
    }

    void AddButton(string label, Color color, System.Action onClick)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 40f;
        go.GetComponent<Image>().color = color;

        var textGO = new GameObject("Label", typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        var trt    = (RectTransform)textGO.transform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var tmp       = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        go.GetComponent<Button>().onClick.AddListener(() => onClick?.Invoke());
    }
}
