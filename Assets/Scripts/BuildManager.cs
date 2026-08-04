using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class BuildManager : MonoBehaviour
{
    public static BuildManager instance;
    public bool IsRelocating => movingPlacement != null;

    [Header("설정")]
    public float gridSize    = 2.0f;
    public float buildDistance = 2.0f;
    public LayerMask obstacleLayer;
    public Transform placementAnchor;

    [Header("상태")]
    public BuildingData currentBuilding;
    private GameObject ghostObject;
    private float currentRotationY = 0f;
    private bool canBuild = true;
    private Vector2Int currentOutdoorAnchor;
    private OutdoorPlacementController.PlacementHandle movingPlacement;
    private TextMeshProUGUI buildHintText;
    private GameObject buildHintPanel;
    private string transientStatus = string.Empty;
    private float transientStatusUntil;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (PlayerInputHandler.Instance != null)
        {
            PlayerInputHandler.Instance.OnBuildRotate += RotateGhost;
            PlayerInputHandler.Instance.OnBuildPlace  += TryPlaceBuild;
            PlayerInputHandler.Instance.OnBuildMove += BeginMoveNearest;
            PlayerInputHandler.Instance.OnBuildRecover += RecoverNearest;
        }
        EnsureBuildHint();
    }

    void OnDestroy()
    {
        if (PlayerInputHandler.Instance != null)
        {
            PlayerInputHandler.Instance.OnBuildRotate -= RotateGhost;
            PlayerInputHandler.Instance.OnBuildPlace  -= TryPlaceBuild;
            PlayerInputHandler.Instance.OnBuildMove -= BeginMoveNearest;
            PlayerInputHandler.Instance.OnBuildRecover -= RecoverNearest;
        }
    }

    void Update()
    {
        UpdateBuildHint();
        if (currentBuilding == null || ghostObject == null) return;

        Transform anchor = ResolvePlacementAnchor();

        // 고스트 위치 갱신 (매 프레임)
        Vector3 targetPos = anchor.position + anchor.forward * buildDistance;
        var outdoor = OutdoorPlacementController.Instance;
        if (outdoor != null && outdoor.IsReady)
        {
            int quarterTurns = Mathf.RoundToInt(currentRotationY / 90f);
            canBuild = outdoor.TryResolvePreview(currentBuilding, targetPos, quarterTurns, movingPlacement,
                out currentOutdoorAnchor, out Vector3 placementWorld, out string reason);
            ghostObject.transform.SetPositionAndRotation(placementWorld,
                Quaternion.Euler(0f, quarterTurns * 90f, 0f));
            SetGhostColor(canBuild);
            outdoor.ShowPreview(currentBuilding, currentOutdoorAnchor, quarterTurns, canBuild, movingPlacement);
            if (!canBuild && Time.unscaledTime >= transientStatusUntil) SetTransientStatus(reason, 0.2f);
            return;
        }

        float x = Mathf.Round(targetPos.x / gridSize) * gridSize;
        float z = Mathf.Round(targetPos.z / gridSize) * gridSize;
        ghostObject.transform.position = new Vector3(x, anchor.position.y, z);
        CheckPlaceable(ghostObject.transform.position);
    }

    // -------- 입력 핸들러 --------

    private void RotateGhost()
    {
        if (ghostObject == null) return;
        currentRotationY += 90f;
        ghostObject.transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);
    }

    private void TryPlaceBuild()
    {
        // 건설 모드가 아니면 무시 (클릭 이벤트는 항상 발행되므로 여기서 걸러낸다)
        if (currentBuilding == null || ghostObject == null) return;

        if (canBuild)
        {
            if (movingPlacement != null) MoveIt();
            else BuildIt();
        }
        else Debug.Log("🚫 장애물 때문에 건설 불가!");
    }

    // -------- 건설 모드 제어 --------

    public void SetBuildMode(BuildingData data)
    {
        if (currentBuilding == data) return;
        StopBuildMode();

        currentBuilding  = data;
        currentRotationY = 0f;
        movingPlacement = null;

        if (data.prefab != null)
        {
            ghostObject = Instantiate(data.prefab);
            PrepareGhost(ghostObject);
        }
    }

    public void StopBuildMode()
    {
        currentBuilding = null;
        if (ghostObject != null) Destroy(ghostObject);
        ghostObject = null;
        movingPlacement = null;
        OutdoorPlacementController.Instance?.ClearPreview();
    }

    // -------- 내부 로직 --------

    void CheckPlaceable(Vector3 pos)
    {
        // 1차 필터: GridService 점유맵 확인 (O(1))
        if (GridService.Instance != null && GridService.Instance.IsOccupiedWorld(pos))
        {
            canBuild = false;
            SetGhostColor(false);
            return;
        }

        // 2차 필터: 물리 OverlapBox (기존 장애물 판정)
        Vector3 boxSize = new Vector3(gridSize * 0.9f, 1f, gridSize * 0.9f);
        Vector3 center  = pos + Vector3.up * 1.0f;
        Collider[] hits = Physics.OverlapBox(center, boxSize / 2,
                            Quaternion.Euler(0, currentRotationY, 0), obstacleLayer);
        canBuild = (hits.Length == 0);

        SetGhostColor(canBuild);
    }

    void SetGhostColor(bool placeable)
    {
        if (ghostObject == null) return;
        Color color = placeable ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        foreach (Renderer r in ghostObject.GetComponentsInChildren<Renderer>())
        {
            r.SetPropertyBlock(block);
        }
    }

    void BuildIt()
    {
        if (Inventory.instance == null) return;

        Item heldItem = Inventory.instance.GetSelectedItem();
        if (heldItem == null || ghostObject == null) return;

        GameObject prefabToBuild = currentBuilding.prefab;
        Vector3    buildPos      = ghostObject.transform.position;
        Quaternion buildRot      = ghostObject.transform.rotation;
        int        price         = currentBuilding.price;

        // 티어 잠금 확인
        if (TierService.Instance != null && !TierService.Instance.IsUnlocked(currentBuilding.requiredTier))
        {
            Debug.Log($"🔒 [{currentBuilding.buildingName}] 건설 불가 — " +
                      $"Tier {currentBuilding.requiredTier} 이상 필요 (현재: Tier {TierService.Instance.CurrentTier})");
            return;
        }

        // 잔액 차감 (원자적 — 실패 시 건물 생성 없음)
        if (EconomyService.Instance == null || !EconomyService.Instance.TrySpend(price, "BuildManager.BuildIt"))
        {
            Debug.Log("💸 결제 실패 (잔액 부족 또는 서비스 부재)");
            return;
        }

        Inventory.instance.RemoveItems(heldItem, 1);
        Debug.Log("➖ 아이템 차감 완료");

        if (prefabToBuild != null)
        {
            var go = Instantiate(prefabToBuild, buildPos, buildRot);
            BuildingRegistry.Instance?.Register(currentBuilding, go);

            // 그리드 점유 등록
            var outdoor = OutdoorPlacementController.Instance;
            if (outdoor != null && outdoor.IsReady)
            {
                int quarterTurns = Mathf.RoundToInt(currentRotationY / 90f);
                if (!outdoor.TryCommitNew(currentBuilding, go, currentOutdoorAnchor, quarterTurns,
                        out _, out string reason))
                {
                    BuildingRegistry.Instance?.Unregister(go);
                    Destroy(go);
                    EconomyService.Instance.TryModifyMoney(price, "BuildManager.CommitRollback");
                    Inventory.instance.AddItem(heldItem, 1);
                    SetTransientStatus($"배치를 확정하지 못해 비용과 설계도를 돌려드렸습니다: {reason}", 4f);
                    return;
                }
                SetTransientStatus($"{currentBuilding.buildingName} 배치를 확정했습니다.", 3f);
            }
            else if (GridService.Instance != null)
            {
                GridService.Instance.TryOccupyWorld(buildPos);
            }

            Debug.Log("✅ 건설 성공!");
        }

        if (!Inventory.instance.HasItems(heldItem, 1))
            StopBuildMode();
    }

    Transform ResolvePlacementAnchor()
    {
        if (placementAnchor != null) return placementAnchor;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            placementAnchor = player.transform;
            return placementAnchor;
        }

        return transform;
    }

    void PrepareGhost(GameObject target)
    {
        if (target == null) return;
        target.name = "PA_OutdoorPlacementPreview";
        foreach (Collider collider in target.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (UnityEngine.AI.NavMeshObstacle obstacle in target.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>(true))
            obstacle.enabled = false;
        foreach (MonoBehaviour behaviour in target.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
    }

    void BeginMoveNearest()
    {
        var outdoor = OutdoorPlacementController.Instance;
        Transform anchor = ResolvePlacementAnchor();
        if (outdoor == null || !outdoor.IsReady || anchor == null) return;
        if (!outdoor.TryFindNearestMovable(anchor.position, out var handle))
        {
            SetTransientStatus("가까운 이동 가능 건물이 없습니다.", 3f);
            return;
        }
        if (handle.Data == null || handle.Data.prefab == null)
        {
            SetTransientStatus("이 건물의 배치 데이터를 찾지 못했습니다.", 3f);
            return;
        }

        StopBuildMode();
        movingPlacement = handle;
        currentBuilding = handle.Data;
        currentRotationY = handle.RotationQuarterTurns * 90f;
        ghostObject = Instantiate(handle.Data.prefab);
        PrepareGhost(ghostObject);
        SetTransientStatus($"{currentBuilding.buildingName} 이동 중 · R 회전 · 클릭 확정", 4f);
    }

    void MoveIt()
    {
        var outdoor = OutdoorPlacementController.Instance;
        if (outdoor == null || movingPlacement == null) return;
        int quarterTurns = Mathf.RoundToInt(currentRotationY / 90f);
        if (!outdoor.TryApplyMove(movingPlacement, currentOutdoorAnchor, quarterTurns, out string reason))
        {
            SetTransientStatus(reason, 3f);
            return;
        }
        string movedName = currentBuilding != null ? currentBuilding.buildingName : "건물";
        StopBuildMode();
        SetTransientStatus($"{movedName} 위치를 안전하게 옮겼습니다.", 3f);
    }

    void RecoverNearest()
    {
        var outdoor = OutdoorPlacementController.Instance;
        Transform anchor = ResolvePlacementAnchor();
        if (outdoor == null || !outdoor.IsReady || anchor == null) return;
        if (outdoor.TryRecoverNearest(anchor.position, true, out string reason))
        {
            StopBuildMode();
            SetTransientStatus(reason, 3f);
        }
        else SetTransientStatus(reason, 4f);
    }

    void EnsureBuildHint()
    {
        if (buildHintPanel != null) return;
        var canvasGo = new GameObject("OutdoorBuildHintCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 910;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        buildHintPanel = new GameObject("OutdoorBuildHint", typeof(RectTransform), typeof(Image));
        buildHintPanel.transform.SetParent(canvasGo.transform, false);
        RectTransform panelRect = buildHintPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 132f);
        panelRect.sizeDelta = new Vector2(760f, 58f);
        buildHintPanel.GetComponent<Image>().color = new Color(0.16f, 0.12f, 0.09f, 0.88f);

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(buildHintPanel.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 6f);
        textRect.offsetMax = new Vector2(-18f, -6f);
        buildHintText = textGo.GetComponent<TextMeshProUGUI>();
        buildHintText.alignment = TextAlignmentOptions.Center;
        buildHintText.fontSize = 22f;
        buildHintText.color = new Color(1f, 0.94f, 0.79f);
        buildHintText.textWrappingMode = TextWrappingModes.NoWrap;
        buildHintPanel.SetActive(false);
    }

    void UpdateBuildHint()
    {
        EnsureBuildHint();
        if (buildHintPanel == null || buildHintText == null) return;
        if (Time.unscaledTime < transientStatusUntil && !string.IsNullOrEmpty(transientStatus))
        {
            buildHintPanel.SetActive(true);
            buildHintText.text = transientStatus;
            return;
        }

        if (currentBuilding != null)
        {
            string footprint = OutdoorPlacementController.Instance != null
                ? OutdoorPlacementController.Instance.GetFootprintLabel(currentBuilding) : "격자 배치";
            buildHintPanel.SetActive(true);
            buildHintText.text = $"{currentBuilding.buildingName} · {footprint} · [R] 회전 · [클릭] 확정";
            return;
        }

        Transform anchor = ResolvePlacementAnchor();
        var outdoor = OutdoorPlacementController.Instance;
        if (anchor != null && outdoor != null && outdoor.IsReady
            && outdoor.TryFindNearestMovable(anchor.position, out var nearby))
        {
            buildHintPanel.SetActive(true);
            string recover = nearby.IsFixed ? "기본 창고: 회수 불가" : "[X] 빈 건물 회수";
            buildHintText.text = $"{nearby.Data.buildingName} · [M] 이동 · {recover} · [Space] 사용";
            return;
        }
        buildHintPanel.SetActive(false);
    }

    void SetTransientStatus(string message, float duration)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        transientStatus = message;
        transientStatusUntil = Time.unscaledTime + Mathf.Max(0.1f, duration);
    }
}
