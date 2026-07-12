using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Player-facing presentation filter for the Day 1-3 core slice.
// It hides validation/advisor overlays by default without deleting them, so
// automated validators and future development toggles can still access data.
public class CoreSlicePresentationMode : MonoBehaviour
{
    public static CoreSlicePresentationMode Instance { get; private set; }

    [Header("Player View")]
    public bool hideDevelopmentOverlaysByDefault = true;
    public bool hidePresentationRouteMarkersByDefault = true;
    public KeyCode toggleDevelopmentOverlayKey = KeyCode.F10;
    public float refreshInterval = 1.0f;

    static readonly string[] DevelopmentCanvasNames =
    {
        "LongPlayProgressionCanvas",
        "ProcessingOpportunityCanvas",
        "CustomerDemandInsightCanvas",
        "VillageChangeSignalCanvas",
        "CustomerPreferenceCanvas",
        "PurchaseFeedbackCanvas"
    };

    static readonly string[] DevelopmentWorldNames =
    {
        "PA_DemoRoute_VisualMarkers",
        "PA_DemoRoute_Label",
        "PA_CustomerApproach_Label",
        "PA_Reinvestment_Label",
        "PA_ScreenshotCameraMarker_MarketHub",
        "PA_EconomicRoleBadge",
        // Visual Demo Integration Pass v2 — 시장 허브의 영어 스테이징 라벨(SUPPLY/PRICE/SALE)은
        // 발표 화면에서 디버그 텍스트로 읽히므로 기본 숨김 (F10 개발 오버레이로만 표시).
        "PA_Supply_Label",
        "PA_Process_Label",
        "PA_Sale_Label"
    };

    static readonly string[] DevelopmentWorldPrefixes =
    {
        "PA_PathStep_",
        // v2 — 씬에 저장된 튜토리얼 안내 라벨(0. 플레이어 / 1. 첫 이주자 / 2. 판매대 슬롯 ...)은
        // 좌측 퀘스트 패널과 상호작용 프롬프트가 같은 정보를 주므로 기본 숨김.
        "Guide_"
    };

    bool _developmentOverlaysVisible;
    float _nextRefreshAt;

    public bool DevelopmentOverlaysVisible => _developmentOverlaysVisible;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _developmentOverlaysVisible = !hideDevelopmentOverlaysByDefault;
    }

    IEnumerator Start()
    {
        yield return null;
        ApplyPresentationMode();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (WasTogglePressed())
        {
            _developmentOverlaysVisible = !_developmentOverlaysVisible;
            ApplyPresentationMode();
        }

        if (Time.unscaledTime < _nextRefreshAt) return;
        _nextRefreshAt = Time.unscaledTime + Mathf.Max(0.25f, refreshInterval);
        ApplyPresentationMode();
    }

    bool WasTogglePressed()
    {
        try
        {
            return Input.GetKeyDown(toggleDevelopmentOverlayKey);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public void SetDevelopmentOverlaysVisible(bool visible)
    {
        _developmentOverlaysVisible = visible;
        ApplyPresentationMode();
    }

    public void ApplyPresentationMode()
    {
        bool showDevelopment = _developmentOverlaysVisible;

        foreach (string canvasName in DevelopmentCanvasNames)
            ApplyCanvasVisibility(canvasName, showDevelopment);

        bool showRouteMarkers = showDevelopment || !hidePresentationRouteMarkersByDefault;
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            ApplyWorldVisibilityRecursive(go.transform, showRouteMarkers);
    }

    void ApplyCanvasVisibility(string canvasName, bool visible)
    {
        var go = FindSceneObject(canvasName);
        if (go == null) return;

        var group = go.GetComponent<CanvasGroup>();
        if (group == null)
            group = go.AddComponent<CanvasGroup>();

        group.alpha = visible ? 1f : 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        group.ignoreParentGroups = false;
    }

    void ApplyWorldVisibilityRecursive(Transform root, bool routeMarkersVisible)
    {
        if (root == null) return;

        bool isDevelopmentMarker = IsDevelopmentWorldObject(root.name);
        if (isDevelopmentMarker)
            SetWorldObjectVisible(root, routeMarkersVisible);

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (isDevelopmentMarker)
                SetWorldObjectVisible(child, routeMarkersVisible);

            ApplyWorldVisibilityRecursive(child, routeMarkersVisible);
        }
    }

    static bool IsDevelopmentWorldObject(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;

        foreach (string exact in DevelopmentWorldNames)
            if (objectName == exact) return true;

        foreach (string prefix in DevelopmentWorldPrefixes)
            if (objectName.StartsWith(prefix, StringComparison.Ordinal)) return true;

        return false;
    }

    static void SetWorldObjectVisible(Transform root, bool visible)
    {
        if (root == null) return;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = visible;

        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            text.enabled = visible;

        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            graphic.enabled = visible;

        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = visible;
    }

    static GameObject FindSceneObject(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return null;

        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var found = FindRecursive(root.transform, objectName);
            if (found != null) return found.gameObject;
        }

        return null;
    }

    static Transform FindRecursive(Transform root, string objectName)
    {
        if (root == null) return null;
        if (root.name == objectName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindRecursive(root.GetChild(i), objectName);
            if (found != null) return found;
        }

        return null;
    }
}

