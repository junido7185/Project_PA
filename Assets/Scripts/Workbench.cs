using System.Collections;
using UnityEngine;

// 월드에 배치되는 작업대(Workbench) — 가공 시스템의 진입점.
//
// 역할:
// - 플레이어가 Space 키로 상호작용하면 CraftingUI 를 "이 작업대 컨텍스트" 로 연다.
// - workbenchType 으로 종류를 분기하여, 같은 종류의 RecipeData 만 표시되게 한다.
//   (예: BasicWorkbench → 가구·기초 가공품, Kitchen → 요리, Forge → 무기/도구, SewingTable → 의류)
//
// 씬 설정:
// - 빈 GameObject 에 Collider 를 부착하고 이 컴포넌트를 추가한다.
// - workbenchType 을 인스펙터에서 선택한다.
// - PlayerInteraction.cs 의 Raycast LayerMask 에 닿는 위치/레이어로 배치한다.
[RequireComponent(typeof(Collider))]
public class Workbench : MonoBehaviour, IInteractable
{
    const string SourceVisualName = "Visual";
    const string B05ArtName = "B05_Workbench_PreparationKit";
    const string B05ArtResource = "VisualFinalization/B05_Workbench_PreparationKit";
    const string B05HandleName = "B05_ProcessHandle";
    const string B05LightName = "B05_SuccessLight";
    const string KitchenInteractionAnchorName = "PA_KitchenInteractionAnchor";

    [Header("작업대 정보")]
    [Tooltip("이 작업대에서 사용할 수 있는 RecipeData.requiredWorkbench 종류와 매칭된다.")]
    public WorkbenchType workbenchType = WorkbenchType.BasicWorkbench;

    [Tooltip("UI 프롬프트에 표시될 이름")]
    public string displayName = "작업대";

    Transform _feedbackHandle;
    Transform _feedbackVisual;
    Transform _interactionAnchor;
    Light _successLight;
    Quaternion _feedbackHandleRest;
    Vector3 _feedbackVisualRestScale;
    Coroutine _feedbackRoutine;

    public bool IsFunctionalArtReady { get; private set; }
    public bool IsCraftFeedbackActive { get; private set; }
    public int CraftFeedbackCount { get; private set; }
    public string LastCraftedItem { get; private set; } = string.Empty;
    public Transform FunctionalArtRoot { get; private set; }
    public Transform InteractionAnchor => _interactionAnchor;

    void Awake()
    {
        ApplyFunctionalArt();
    }

    // Tripo 추정 원본과 래퍼 프리팹은 읽기 전용으로 유지한다. 실제 인스턴스에서만
    // 기능 역할에 필요한 정면, 물리 범위, 접근점과 짧은 성공 피드백을 정합한다.
    public bool ApplyFunctionalArt()
    {
        if (IsFunctionalArtReady && FunctionalArtRoot != null) return true;

        if (workbenchType == WorkbenchType.Kitchen)
            return ApplyKitchenPresentation();
        if (workbenchType != WorkbenchType.BasicWorkbench) return false;

        Transform sourceVisual = transform.Find(SourceVisualName);
        if (sourceVisual == null) return false;

        Transform existing = transform.Find(B05ArtName);
        if (existing == null)
        {
            GameObject prefab = Resources.Load<GameObject>(B05ArtResource);
            if (prefab == null) return false;

            // 원본은 +Z 쪽이 작업면이지만 배치 정의의 clearance/interaction은 -Z다.
            // 부모 로컬 Y축으로 180° 회전해 시각 작업면과 실제 접근 셀을 일치시킨다.
            sourceVisual.localRotation = Quaternion.Euler(0f, 180f, 0f) * sourceVisual.localRotation;
            GameObject instance = Instantiate(prefab, transform, false);
            instance.name = B05ArtName;
            existing = instance.transform;
        }

        FunctionalArtRoot = existing;
        FunctionalArtRoot.localPosition = Vector3.zero;
        FunctionalArtRoot.localRotation = Quaternion.identity;
        FunctionalArtRoot.localScale = Vector3.one;

        _feedbackHandle = FunctionalArtRoot.Find(B05HandleName);
        _successLight = FunctionalArtRoot.Find(B05LightName)?.GetComponent<Light>();
        if (_feedbackHandle != null) _feedbackHandleRest = _feedbackHandle.localRotation;
        if (_successLight != null)
        {
            _successLight.intensity = 0f;
            _successLight.enabled = false;
        }

        // 기존 3.2m 폭 collider는 실제 2.26m 메시보다 좌우로 과도하게 넓었다.
        // 2x2 Grid 점유는 prefab 정의에서 계속 유지하고, 물리/Carving만 모델에 맞춘다.
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            box.center = new Vector3(0f, 1f, 0f);
            box.size = new Vector3(2.5f, 2f, 2.55f);
        }
        UnityEngine.AI.NavMeshObstacle obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (obstacle != null)
        {
            obstacle.center = box != null ? box.center : new Vector3(0f, 1f, 0f);
            obstacle.size = box != null ? box.size : new Vector3(2.5f, 2f, 2.55f);
        }

        if (displayName == "작업대" || displayName == "기본 작업대")
            displayName = "목재 가공 작업대";

        IsFunctionalArtReady = _feedbackHandle != null && _successLight != null;
        return IsFunctionalArtReady;
    }

    bool ApplyKitchenPresentation()
    {
        Transform sourceVisual = transform.Find(SourceVisualName);
        if (sourceVisual == null) return false;

        FunctionalArtRoot = sourceVisual;
        _feedbackVisual = sourceVisual;
        _feedbackVisualRestScale = sourceVisual.localScale;

        // B06의 2x2 설치 footprint와 전면 clearance는 GridService가 계속 소유한다.
        // 여기서는 실제 Visual보다 명백히 큰 물리/Carving 축만 줄여 보이지 않는 벽을 막는다.
        FitKitchenPhysicsToVisibleModel(sourceVisual);

        BoxCollider box = GetComponent<BoxCollider>();
        _interactionAnchor = transform.Find(KitchenInteractionAnchorName);
        if (_interactionAnchor == null)
        {
            var anchor = new GameObject(KitchenInteractionAnchorName);
            _interactionAnchor = anchor.transform;
            _interactionAnchor.SetParent(transform, false);
        }

        float frontZ = box != null ? box.center.z - box.size.z * 0.5f : -1.4f;
        _interactionAnchor.localPosition = new Vector3(
            box != null ? box.center.x : 0f,
            0.02f,
            frontZ - 0.65f);
        _interactionAnchor.localRotation = Quaternion.identity;
        _interactionAnchor.localScale = Vector3.one;

        if (displayName == "작업대" || displayName == "주방 스테이션")
            displayName = "조리 준비대";

        IsFunctionalArtReady = true;
        return true;
    }

    void FitKitchenPhysicsToVisibleModel(Transform sourceVisual)
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null || !TryGetLocalRendererBounds(sourceVisual, out Bounds visible)) return;

        const float padding = 0.16f;
        const float minimumOversize = 0.2f;
        Vector3 size = box.size;
        Vector3 center = box.center;
        float paddedX = visible.size.x + padding;
        float paddedZ = visible.size.z + padding;

        if (size.x - paddedX > minimumOversize)
        {
            size.x = paddedX;
            center.x = visible.center.x;
        }
        if (size.z - paddedZ > minimumOversize)
        {
            size.z = paddedZ;
            center.z = visible.center.z;
        }

        box.size = size;
        box.center = center;

        UnityEngine.AI.NavMeshObstacle obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (obstacle != null && obstacle.shape == UnityEngine.AI.NavMeshObstacleShape.Box)
        {
            obstacle.size = size;
            obstacle.center = center;
        }
    }

    bool TryGetLocalRendererBounds(Transform sourceVisual, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = sourceVisual.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            Bounds world = renderer.bounds;
            Vector3 min = world.min;
            Vector3 max = world.max;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 corner = new Vector3(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z);
                Vector3 local = worldToLocal.MultiplyPoint3x4(corner);
                if (!found)
                {
                    bounds = new Bounds(local, Vector3.zero);
                    found = true;
                }
                else bounds.Encapsulate(local);
            }
        }

        return found && bounds.size.x > 0.01f && bounds.size.z > 0.01f;
    }

    public void Interact(GameObject interactor)
    {
        if (CraftingUI.instance == null)
        {
            Debug.LogWarning("🛠 Workbench: 씬에 CraftingUI 가 없습니다.");
            return;
        }
        CraftingUI.instance.OpenForWorkbench(this);
    }

    public void PlayCraftFeedback(string craftedItemName)
    {
        if (!ApplyFunctionalArt()) return;
        LastCraftedItem = craftedItemName ?? string.Empty;
        CraftFeedbackCount++;
        if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
        _feedbackRoutine = StartCoroutine(CraftFeedbackRoutine());
    }

    IEnumerator CraftFeedbackRoutine()
    {
        IsCraftFeedbackActive = true;
        if (_successLight != null)
        {
            _successLight.enabled = true;
            _successLight.intensity = 0.9f;
        }

        const float duration = 0.72f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float phase = Mathf.Clamp01(elapsed / duration);
            float press = Mathf.Sin(phase * Mathf.PI);
            if (_feedbackHandle != null)
                _feedbackHandle.localRotation = _feedbackHandleRest * Quaternion.Euler(0f, 0f, -22f * press);
            if (_feedbackVisual != null)
                _feedbackVisual.localScale = Vector3.Lerp(
                    _feedbackVisualRestScale,
                    _feedbackVisualRestScale * 1.035f,
                    press);
            if (_successLight != null)
                _successLight.intensity = Mathf.Lerp(0.9f, 0f, phase);
            yield return null;
        }

        if (_feedbackHandle != null) _feedbackHandle.localRotation = _feedbackHandleRest;
        if (_feedbackVisual != null) _feedbackVisual.localScale = _feedbackVisualRestScale;
        if (_successLight != null)
        {
            _successLight.intensity = 0f;
            _successLight.enabled = false;
        }
        IsCraftFeedbackActive = false;
        _feedbackRoutine = null;
    }

    void OnDisable()
    {
        if (_feedbackHandle != null) _feedbackHandle.localRotation = _feedbackHandleRest;
        if (_feedbackVisual != null) _feedbackVisual.localScale = _feedbackVisualRestScale;
        if (_successLight != null)
        {
            _successLight.intensity = 0f;
            _successLight.enabled = false;
        }
        IsCraftFeedbackActive = false;
        _feedbackRoutine = null;
    }

    public string GetInteractPrompt() => $"{displayName} 사용하기";
}
