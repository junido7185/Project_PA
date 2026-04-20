using UnityEngine;
using UnityEngine.UI;
using TMPro;

// §1-B MoneyHUD — 화면 상단 항상 노출되는 재화 표시 HUD.
// 🐾 자기완결 싱글톤: Awake 에서 Canvas 를 직접 생성하므로 씬에 추가만 하면 된다.
//    PA_UIRoot 가 없어도 동작하지만, 있는 경우 PA_UIRoot 를 부모로 재사용한다.
//
// EconomyService.OnMoneyChanged 구독 → 잔액 갱신.
// TierService.OnTierAdvanced    구독 → 티어 이름 갱신.
public class MoneyHUD : MonoBehaviour
{
    public static MoneyHUD instance;

    [Header("직접 연결 (선택)")]
    [Tooltip("null 이면 Awake 에서 자동 생성")]
    public TextMeshProUGUI moneyText;
    [Tooltip("null 이면 Awake 에서 자동 생성")]
    public TextMeshProUGUI tierText;

    // ── 자동 생성된 오브젝트 (직접 연결 시 미사용) ──────────────────────────────
    Canvas _ownCanvas;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        if (moneyText == null || tierText == null)
            BuildHUD();
    }

    // ── HUD 자동 빌드 (씬에 배치만 해도 동작) ────────────────────────────────────
    void BuildHUD()
    {
        // PA_UIRoot 가 이미 있으면 그 아래에 붙인다 (z-order 공유)
        var uiRoot = GameObject.Find("PA_UIRoot");
        Transform parent;

        if (uiRoot != null)
        {
            parent = uiRoot.transform;
        }
        else
        {
            // 전용 Canvas 새로 만든다
            var canvasGO = new GameObject("MoneyHUD_Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _ownCanvas                    = canvasGO.GetComponent<Canvas>();
            _ownCanvas.renderMode         = RenderMode.ScreenSpaceOverlay;
            _ownCanvas.sortingOrder       = 50;
            var scaler                    = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution    = new Vector2(1920, 1080);
            scaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight     = 0.5f;
            parent                        = canvasGO.transform;
        }

        // ── HUD 컨테이너 (우상단 고정) ─────────────────────────────────────────
        var hudGO = new GameObject("MoneyHudPanel",
            typeof(RectTransform), typeof(Image));
        var hudRT = (RectTransform)hudGO.transform;
        hudRT.SetParent(parent, false);
        hudRT.anchorMin        = new Vector2(1f, 1f);   // 우상단
        hudRT.anchorMax        = new Vector2(1f, 1f);
        hudRT.pivot            = new Vector2(1f, 1f);
        hudRT.sizeDelta        = new Vector2(260, 70);
        hudRT.anchoredPosition = new Vector2(-20, -20);

        var bgImg = hudGO.GetComponent<Image>();
        bgImg.color         = new Color(0f, 0f, 0f, 0.45f); // 반투명 검정
        bgImg.raycastTarget = false;

        // ── 재화 텍스트 ────────────────────────────────────────────────────────
        var moneyGO  = new GameObject("MoneyText", typeof(TextMeshProUGUI));
        var moneyTRT = (RectTransform)moneyGO.transform;
        moneyTRT.SetParent(hudGO.transform, false);
        moneyTRT.anchorMin = Vector2.zero;
        moneyTRT.anchorMax = Vector2.one;
        moneyTRT.offsetMin = new Vector2(10, 36);  // 하단 절반
        moneyTRT.offsetMax = new Vector2(-10, -4);

        moneyText            = moneyGO.GetComponent<TextMeshProUGUI>();
        moneyText.text       = "💰 0 G";
        moneyText.fontSize   = 22;
        moneyText.fontStyle  = FontStyles.Bold;
        moneyText.color      = new Color(1f, 0.93f, 0.4f); // 골드
        moneyText.alignment  = TextAlignmentOptions.Right;
        moneyText.raycastTarget = false;

        // ── 티어 텍스트 ────────────────────────────────────────────────────────
        var tierGO  = new GameObject("TierText", typeof(TextMeshProUGUI));
        var tierTRT = (RectTransform)tierGO.transform;
        tierTRT.SetParent(hudGO.transform, false);
        tierTRT.anchorMin = Vector2.zero;
        tierTRT.anchorMax = Vector2.one;
        tierTRT.offsetMin = new Vector2(10, 4);
        tierTRT.offsetMax = new Vector2(-10, -38);

        tierText            = tierGO.GetComponent<TextMeshProUGUI>();
        tierText.text       = "Tier 0 · 생존자";
        tierText.fontSize   = 15;
        tierText.color      = new Color(0.8f, 0.8f, 0.8f);
        tierText.alignment  = TextAlignmentOptions.Right;
        tierText.raycastTarget = false;
    }

    // ── 서비스 구독 ──────────────────────────────────────────────────────────────
    void Start()
    {
        if (EconomyService.Instance != null)
        {
            EconomyService.Instance.OnMoneyChanged += OnMoneyChanged;
            OnMoneyChanged(EconomyService.Instance.Money); // 초기값 즉시 반영
        }
        if (TierService.Instance != null)
        {
            TierService.Instance.OnTierAdvanced += OnTierAdvanced;
            RefreshTierText(TierService.Instance.CurrentTier); // 초기값
        }
    }

    void OnDestroy()
    {
        if (EconomyService.Instance != null)
            EconomyService.Instance.OnMoneyChanged -= OnMoneyChanged;
        if (TierService.Instance != null)
            TierService.Instance.OnTierAdvanced -= OnTierAdvanced;
    }

    // ── 콜백 ─────────────────────────────────────────────────────────────────────
    void OnMoneyChanged(int amount)
    {
        if (moneyText != null)
            moneyText.text = $"💰 {amount:N0} G";
    }

    // TierService.OnTierAdvanced 는 (oldTier, newTier) 시그니처
    void OnTierAdvanced(int oldTier, int newTier) => RefreshTierText(newTier);

    void RefreshTierText(int tier)
    {
        if (tierText == null) return;

        string tierName = "생존자";
        if (TierService.Instance != null)
        {
            var def = TierService.Instance.GetDefinition(tier);
            if (def != null) tierName = def.tierName;
        }
        tierText.text = $"Tier {tier} · {tierName}";
    }
}
