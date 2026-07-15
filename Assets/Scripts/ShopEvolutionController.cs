using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// S4 — Tier 0 가판대에서 Tier 1 실내 잡화점으로 이어지는 진행 연결.
// 기존 TierService/currentTier 저장을 진실 원본으로 사용하며 씬·저장 스키마를 추가하지 않는다.
public sealed class ShopEvolutionController : MonoBehaviour
{
    public static ShopEvolutionController Instance { get; private set; }

    public const int InteriorUnlockTier = 1;

    TierService _tierService;
    BuildingEntrance _exteriorEntrance;
    PrototypeWorldLabel _storeSign;
    Light _tierOneLight;
    CanvasGroup _unlockPanel;
    TextMeshProUGUI _unlockText;
    Coroutine _presentationRoutine;
    int _appliedTier = int.MinValue;

    public bool IsInteriorUnlocked => _tierService != null
        && _tierService.IsUnlocked(InteriorUnlockTier);
    public string CurrentStoreSign => _storeSign != null ? _storeSign.label : string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InstallForEnterableStore()
    {
        if (FindFirstObjectByType<ShopEvolutionController>() != null) return;
        if (GameObject.Find("PA_StoreDoor_Out") == null || GameObject.Find("PA_StoreInterior") == null) return;

        var host = new GameObject("PA_ShopEvolutionController");
        var services = GameObject.Find("[Services]");
        if (services != null) host.transform.SetParent(services.transform, false);
        host.AddComponent<ShopEvolutionController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (!TryBind()) return;

        // ForceSetTier(로드)는 OnTierAdvanced를 발생시키지 않는다.
        // 저장된 티어가 바뀐 경우에는 연출 없이 공간 상태만 재구성한다.
        if (_appliedTier != _tierService.CurrentTier)
            ApplyTierState(showUnlockPresentation: false);
    }

    void OnDestroy()
    {
        if (_tierService != null)
            _tierService.OnTierAdvanced -= OnTierAdvanced;
        if (Instance == this) Instance = null;
    }

    bool TryBind()
    {
        if (_tierService == null && TierService.Instance != null)
        {
            _tierService = TierService.Instance;
            _tierService.OnTierAdvanced += OnTierAdvanced;
        }

        if (_exteriorEntrance == null)
        {
            var door = GameObject.Find("PA_StoreDoor_Out");
            if (door == null) return false;

            _exteriorEntrance = door.GetComponent<BuildingEntrance>();
            _storeSign = door.GetComponentInChildren<PrototypeWorldLabel>(true);
            EnsureTierOneLight(door.transform);
            EnsureUnlockPanel();
        }

        if (_tierService == null || _exteriorEntrance == null) return false;

        _exteriorEntrance.ConfigureTierRequirement(
            InteriorUnlockTier,
            "잠김 · Tier 1 지점장 필요");
        return true;
    }

    void OnTierAdvanced(int oldTier, int newTier)
    {
        bool crossedInteriorUnlock = oldTier < InteriorUnlockTier && newTier >= InteriorUnlockTier;
        ApplyTierState(crossedInteriorUnlock);
    }

    void ApplyTierState(bool showUnlockPresentation)
    {
        if (_tierService == null || _exteriorEntrance == null) return;

        _appliedTier = _tierService.CurrentTier;
        bool unlocked = IsInteriorUnlocked;

        if (_storeSign != null)
        {
            _storeSign.Set(
                unlocked ? "잡화점 · OPEN" : "잡화점 준비 중 · Tier 1",
                unlocked ? new Color(0.35f, 0.22f, 0.12f) : new Color(0.45f, 0.34f, 0.22f),
                unlocked ? 1.35f : 1.0f);
        }

        if (_tierOneLight != null)
        {
            _tierOneLight.gameObject.SetActive(unlocked);
            _tierOneLight.intensity = 2.4f;
        }

        if (showUnlockPresentation && unlocked)
            ShowUnlockPresentation();

        Debug.Log(unlocked
            ? "🏪 [상점 진화] Tier 1 실내 잡화점 개방 — 외부 문과 실내 영업 연결 완료"
            : "🏪 [상점 진화] Tier 0 가판대 운영 중 — 실내 잡화점은 Tier 1에서 개방");
    }

    void EnsureTierOneLight(Transform door)
    {
        var existing = door.Find("Tier1_StoreLight");
        GameObject lightGo;
        if (existing != null)
        {
            lightGo = existing.gameObject;
            _tierOneLight = lightGo.GetComponent<Light>();
        }
        else
        {
            lightGo = new GameObject("Tier1_StoreLight");
            lightGo.transform.SetParent(door, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.6f, -0.45f);
            _tierOneLight = lightGo.AddComponent<Light>();
        }

        if (_tierOneLight == null) _tierOneLight = lightGo.AddComponent<Light>();
        _tierOneLight.type = LightType.Point;
        _tierOneLight.color = new Color(1f, 0.68f, 0.34f);
        _tierOneLight.range = 5.5f;
        _tierOneLight.shadows = LightShadows.None;
        lightGo.SetActive(false);
    }

    void EnsureUnlockPanel()
    {
        if (_unlockPanel != null) return;

        var canvasGo = new GameObject("ShopEvolutionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 820;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var panelGo = new GameObject("Tier1UnlockPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panelGo.transform.SetParent(canvasGo.transform, false);
        var rect = (RectTransform)panelGo.transform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -92f);
        rect.sizeDelta = new Vector2(660f, 116f);

        var image = panelGo.GetComponent<Image>();
        image.color = new Color(0.18f, 0.105f, 0.055f, 0.94f);
        image.raycastTarget = false;

        _unlockPanel = panelGo.GetComponent<CanvasGroup>();
        _unlockPanel.alpha = 0f;
        _unlockPanel.interactable = false;
        _unlockPanel.blocksRaycasts = false;

        var textGo = new GameObject("Tier1UnlockText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(panelGo.transform, false);
        var textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(28f, 16f);
        textRect.offsetMax = new Vector2(-28f, -14f);

        _unlockText = textGo.GetComponent<TextMeshProUGUI>();
        _unlockText.text = "잡화점 확장 완료!\n광장의 새 문으로 들어가 상품을 진열하고 손님을 맞이하세요.";
        _unlockText.fontSize = 25f;
        _unlockText.fontStyle = FontStyles.Bold;
        _unlockText.alignment = TextAlignmentOptions.Center;
        _unlockText.color = new Color(1f, 0.91f, 0.70f);
        _unlockText.textWrappingMode = TextWrappingModes.Normal;
        _unlockText.raycastTarget = false;

        panelGo.SetActive(false);
    }

    void ShowUnlockPresentation()
    {
        if (_unlockPanel == null) return;
        if (_presentationRoutine != null) StopCoroutine(_presentationRoutine);
        _presentationRoutine = StartCoroutine(CoShowUnlockPresentation());
    }

    IEnumerator CoShowUnlockPresentation()
    {
        _unlockPanel.gameObject.SetActive(true);
        _unlockPanel.alpha = 0f;

        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            _unlockPanel.alpha = Mathf.Clamp01(t / 0.35f);
            if (_tierOneLight != null)
                _tierOneLight.intensity = Mathf.Lerp(2.4f, 4.2f, _unlockPanel.alpha);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(4.5f);

        t = 0f;
        while (t < 0.45f)
        {
            t += Time.unscaledDeltaTime;
            _unlockPanel.alpha = 1f - Mathf.Clamp01(t / 0.45f);
            yield return null;
        }

        if (_tierOneLight != null) _tierOneLight.intensity = 2.4f;
        _unlockPanel.gameObject.SetActive(false);
        _presentationRoutine = null;
    }
}
