using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// S4 — Tier 0 가판대에서 Tier 1 실내 잡화점으로 이어지는 진행 연결.
// 기존 TierService/currentTier 저장을 진실 원본으로 사용하며 씬·저장 스키마를 추가하지 않는다.
public sealed class ShopEvolutionController : MonoBehaviour
{
    public static ShopEvolutionController Instance { get; private set; }

    public const int InteriorUnlockTier = 1;
    const string ShopCottageName = "B10_Cottage_01";
    const string OutsideSpawnName = "PlayerSpawn_Outside";

    static readonly string[] ExteriorStageResources =
    {
        "VisualFinalization/ShopEvolution/B02_ShopEvolutionVisual",
        "VisualFinalization/ShopEvolution/B03_ShopEvolutionVisual",
        "VisualFinalization/ShopEvolution/B04_ShopEvolutionVisual"
    };

    TierService _tierService;
    BuildingEntrance _exteriorEntrance;
    PrototypeWorldLabel _storeSign;
    Light _tierOneLight;
    CanvasGroup _unlockPanel;
    TextMeshProUGUI _unlockText;
    Coroutine _presentationRoutine;
    int _appliedTier = int.MinValue;
    GameObject _shopCottage;
    Renderer[] _cottageRenderers = System.Array.Empty<Renderer>();
    Collider[] _cottageColliders = System.Array.Empty<Collider>();
    NavMeshObstacle[] _cottageObstacles = System.Array.Empty<NavMeshObstacle>();
    bool[] _cottageRendererStates = System.Array.Empty<bool>();
    bool[] _cottageColliderStates = System.Array.Empty<bool>();
    bool[] _cottageObstacleStates = System.Array.Empty<bool>();
    readonly GameObject[] _exteriorStageVisuals = new GameObject[3];
    GameObject _activeExteriorVisual;
    Transform _activeEntranceAnchor;
    Transform _activeSignAnchor;
    Transform _doorVisualAnchor;
    GameObject _outsideSpawn;
    Vector3 _tierZeroDoorPosition;
    Quaternion _tierZeroDoorRotation;
    Vector3 _tierZeroSignPosition;
    Quaternion _tierZeroSignRotation;
    Vector3 _tierZeroOutsidePosition;
    Quaternion _tierZeroOutsideRotation;
    bool _exteriorStagesBound;
    bool _reportedMissingStageAssets;

    public bool IsInteriorUnlocked => _tierService != null
        && _tierService.IsUnlocked(InteriorUnlockTier);
    public string CurrentStoreSign => _storeSign != null ? _storeSign.label : string.Empty;
    public int ActiveExteriorStage { get; private set; }
    public string ActiveExteriorStageName => ActiveExteriorStage switch
    {
        1 => "B02_GeneralStore",
        2 => "B03_ConvenienceStore",
        3 => "B04_DepartmentStore",
        _ => "B10_Cottage"
    };
    public GameObject ActiveExteriorVisual => _activeExteriorVisual;
    public Transform ActiveEntranceAnchor => _activeEntranceAnchor;
    public Transform ActiveSignAnchor => _activeSignAnchor;
    public int ActiveEvolutionVisualCount => _exteriorStageVisuals.Count(item => item != null && item.activeSelf);
    public bool HasAllExteriorStageAssets => _exteriorStageVisuals.All(item => item != null);

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

        EnsureExteriorStages();

        if (_tierService == null || _exteriorEntrance == null) return false;

        _exteriorEntrance.ConfigureTierRequirement(
            InteriorUnlockTier,
            "잠김 · Tier 1 지점장 필요");
        return true;
    }

    void OnTierAdvanced(int oldTier, int newTier)
    {
        ApplyTierState(newTier > oldTier);
    }

    void ApplyTierState(bool showUnlockPresentation)
    {
        if (_tierService == null || _exteriorEntrance == null) return;

        _appliedTier = _tierService.CurrentTier;
        bool unlocked = IsInteriorUnlocked;
        int exteriorStage = ApplyExteriorStage(showUnlockPresentation && unlocked);

        if (_storeSign != null)
        {
            _storeSign.Set(
                GetStageSign(exteriorStage, unlocked),
                new Color(0.30f, 0.17f, 0.08f),
                unlocked ? 2.25f : 2.05f);
        }

        if (_tierOneLight != null)
        {
            _tierOneLight.gameObject.SetActive(unlocked);
            _tierOneLight.intensity = 2.4f;
        }

        if (showUnlockPresentation && unlocked)
            ShowUnlockPresentation(_tierService.CurrentTier);

        Debug.Log(unlocked
            ? $"🏪 [상점 진화] Tier {_tierService.CurrentTier} {ActiveExteriorStageName} — " +
              "외부 문·실내 영업·기존 배치 보존"
            : "🏪 [상점 진화] Tier 0 가판대 운영 중 — 실내 잡화점은 Tier 1에서 개방");
    }

    void EnsureExteriorStages()
    {
        if (_exteriorStagesBound || _exteriorEntrance == null) return;
        _shopCottage = GameObject.Find(ShopCottageName);
        if (_shopCottage == null) return;

        // Cache only the authored B10 components before adding any derived stage visuals.
        _cottageRenderers = _shopCottage.GetComponentsInChildren<Renderer>(true);
        _cottageColliders = _shopCottage.GetComponentsInChildren<Collider>(true);
        _cottageObstacles = _shopCottage.GetComponentsInChildren<NavMeshObstacle>(true);
        _cottageRendererStates = _cottageRenderers.Select(item => item.enabled).ToArray();
        _cottageColliderStates = _cottageColliders.Select(item => item.enabled).ToArray();
        _cottageObstacleStates = _cottageObstacles.Select(item => item.enabled).ToArray();

        _tierZeroDoorPosition = _exteriorEntrance.transform.position;
        _tierZeroDoorRotation = _exteriorEntrance.transform.rotation;
        _doorVisualAnchor = _exteriorEntrance.transform.Find("B10_EntranceVisualAnchor");
        if (_doorVisualAnchor != null)
        {
            _tierZeroSignPosition = _doorVisualAnchor.position;
            _tierZeroSignRotation = _doorVisualAnchor.rotation;
        }
        _outsideSpawn = GameObject.Find(OutsideSpawnName);
        if (_outsideSpawn != null)
        {
            _tierZeroOutsidePosition = _outsideSpawn.transform.position;
            _tierZeroOutsideRotation = _outsideSpawn.transform.rotation;
        }

        for (int index = 0; index < ExteriorStageResources.Length; index++)
        {
            string instanceName = $"PA_ShopEvolution_B0{index + 2}";
            Transform existing = _shopCottage.transform.Find(instanceName);
            GameObject stage = existing != null ? existing.gameObject : null;
            if (stage == null)
            {
                GameObject prefab = Resources.Load<GameObject>(ExteriorStageResources[index]);
                if (prefab != null)
                {
                    stage = Instantiate(prefab, _shopCottage.transform, false);
                    stage.name = instanceName;
                }
            }

            if (stage != null)
            {
                stage.transform.localPosition = Vector3.zero;
                stage.transform.localRotation = Quaternion.identity;
                stage.transform.localScale = Vector3.one;
                stage.SetActive(false);
            }
            _exteriorStageVisuals[index] = stage;
        }

        _exteriorStagesBound = true;
        if (!HasAllExteriorStageAssets && !_reportedMissingStageAssets)
        {
            _reportedMissingStageAssets = true;
            Debug.LogWarning("[상점 진화] B02~B04 visual-only Resources 프리팹 일부가 없어 B10 외형을 유지합니다.");
        }
    }

    int ApplyExteriorStage(bool showPresentation)
    {
        EnsureExteriorStages();
        int requestedStage = GetExteriorStageForTier(_tierService != null ? _tierService.CurrentTier : 0);
        int resolvedStage = requestedStage;
        if (requestedStage > 0 && _exteriorStageVisuals[requestedStage - 1] == null)
            resolvedStage = 0;

        RestoreCottageComponentStates(resolvedStage == 0);
        for (int index = 0; index < _exteriorStageVisuals.Length; index++)
        {
            GameObject stage = _exteriorStageVisuals[index];
            if (stage == null) continue;
            bool active = resolvedStage == index + 1;
            stage.SetActive(active);
            stage.transform.localScale = active && showPresentation && _unlockPanel != null
                ? Vector3.one * 0.94f
                : Vector3.one;
        }

        _activeExteriorVisual = resolvedStage > 0 ? _exteriorStageVisuals[resolvedStage - 1] : null;
        _activeEntranceAnchor = _activeExteriorVisual != null
            ? _activeExteriorVisual.transform.Find("EntranceAnchor")
            : null;
        _activeSignAnchor = _activeExteriorVisual != null
            ? _activeExteriorVisual.transform.Find("SignAnchor")
            : null;

        if (_activeEntranceAnchor != null)
        {
            _exteriorEntrance.transform.SetPositionAndRotation(
                _activeEntranceAnchor.position, _activeEntranceAnchor.rotation);
            if (_outsideSpawn != null)
            {
                Vector3 spawnPosition = _activeEntranceAnchor.position + _activeEntranceAnchor.forward * 1.35f;
                spawnPosition.y = _shopCottage.transform.position.y + 0.05f;
                _outsideSpawn.transform.SetPositionAndRotation(spawnPosition, _activeEntranceAnchor.rotation);
            }
            if (_doorVisualAnchor != null && _activeSignAnchor != null)
                _doorVisualAnchor.SetPositionAndRotation(_activeSignAnchor.position, _activeSignAnchor.rotation);
        }
        else
        {
            _exteriorEntrance.transform.SetPositionAndRotation(_tierZeroDoorPosition, _tierZeroDoorRotation);
            if (_outsideSpawn != null)
                _outsideSpawn.transform.SetPositionAndRotation(_tierZeroOutsidePosition, _tierZeroOutsideRotation);
            if (_doorVisualAnchor != null)
                _doorVisualAnchor.SetPositionAndRotation(_tierZeroSignPosition, _tierZeroSignRotation);
        }

        ActiveExteriorStage = resolvedStage;
        Physics.SyncTransforms();
        return resolvedStage;
    }

    void RestoreCottageComponentStates(bool restore)
    {
        for (int index = 0; index < _cottageRenderers.Length; index++)
            if (_cottageRenderers[index] != null)
                _cottageRenderers[index].enabled = restore && _cottageRendererStates[index];
        for (int index = 0; index < _cottageColliders.Length; index++)
            if (_cottageColliders[index] != null)
                _cottageColliders[index].enabled = restore && _cottageColliderStates[index];
        for (int index = 0; index < _cottageObstacles.Length; index++)
            if (_cottageObstacles[index] != null)
                _cottageObstacles[index].enabled = restore && _cottageObstacleStates[index];
    }

    static int GetExteriorStageForTier(int tier) => Mathf.Clamp(tier, 0, 3);

    static string GetStageSign(int exteriorStage, bool unlocked)
    {
        if (!unlocked) return "P.A. SHOP - Tier 1";
        return exteriorStage switch
        {
            1 => "P.A. GENERAL - OPEN",
            2 => "P.A. MARKET - OPEN",
            3 => "P.A. DEPT. - OPEN",
            _ => "P.A. SHOP - OPEN"
        };
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

    void ShowUnlockPresentation(int tier)
    {
        if (_unlockPanel == null) return;
        if (_unlockText != null)
        {
            _unlockText.text = tier switch
            {
                1 => "잡화점 진화 완료!\n실내 5×4 · 기본 작업대 설계도가 열렸습니다.",
                2 => "마을 마트 확장 완료!\n실내 6×5 · 진열대 8개 · 조리 준비대가 열렸습니다.",
                3 => "부티크 백화점 확장 완료!\n실내 7×6 · 진열대 12개 · 수리/재봉 준비대가 열렸습니다.",
                4 => "파트너 상점 운영 승인!\n진열대 한도가 20개로 늘어 더 큰 매장을 꾸밀 수 있습니다.",
                _ => "상점 진화 완료!\n기존 진열과 배치는 그대로 유지됩니다."
            };
        }
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
            if (_activeExteriorVisual != null)
            {
                float eased = Mathf.SmoothStep(0f, 1f, _unlockPanel.alpha);
                _activeExteriorVisual.transform.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, eased);
            }
            if (_tierOneLight != null)
                _tierOneLight.intensity = Mathf.Lerp(2.4f, 4.2f, _unlockPanel.alpha);
            yield return null;
        }

        if (_activeExteriorVisual != null) _activeExteriorVisual.transform.localScale = Vector3.one;

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
