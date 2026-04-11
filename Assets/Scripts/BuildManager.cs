using UnityEngine;
using UnityEngine.EventSystems;

public class BuildManager : MonoBehaviour
{
    public static BuildManager instance;

    [Header("설정")]
    public float gridSize    = 2.0f;
    public float buildDistance = 2.0f;
    public LayerMask obstacleLayer;

    [Header("상태")]
    public BuildingData currentBuilding;
    private GameObject ghostObject;
    private float currentRotationY = 0f;
    private bool canBuild = true;

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
        }
    }

    void OnDestroy()
    {
        if (PlayerInputHandler.Instance != null)
        {
            PlayerInputHandler.Instance.OnBuildRotate -= RotateGhost;
            PlayerInputHandler.Instance.OnBuildPlace  -= TryPlaceBuild;
        }
    }

    void Update()
    {
        if (currentBuilding == null || ghostObject == null) return;

        // 고스트 위치 갱신 (매 프레임)
        Vector3 targetPos = transform.position + transform.forward * buildDistance;
        float x = Mathf.Round(targetPos.x / gridSize) * gridSize;
        float z = Mathf.Round(targetPos.z / gridSize) * gridSize;
        ghostObject.transform.position = new Vector3(x, transform.position.y, z);

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

        if (canBuild) BuildIt();
        else Debug.Log("🚫 장애물 때문에 건설 불가!");
    }

    // -------- 건설 모드 제어 --------

    public void SetBuildMode(BuildingData data)
    {
        if (currentBuilding == data) return;
        StopBuildMode();

        currentBuilding  = data;
        currentRotationY = 0f;

        if (data.prefab != null)
        {
            ghostObject = Instantiate(data.prefab);
            foreach (var c in ghostObject.GetComponentsInChildren<Collider>()) c.enabled = false;
        }
    }

    public void StopBuildMode()
    {
        currentBuilding = null;
        if (ghostObject != null) Destroy(ghostObject);
    }

    // -------- 내부 로직 --------

    void CheckPlaceable(Vector3 pos)
    {
        Vector3 boxSize = new Vector3(gridSize * 0.9f, 1f, gridSize * 0.9f);
        Vector3 center  = pos + Vector3.up * 1.0f;
        Collider[] hits = Physics.OverlapBox(center, boxSize / 2,
                            Quaternion.Euler(0, currentRotationY, 0), obstacleLayer);
        canBuild = (hits.Length == 0);

        Color color = canBuild ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
        foreach (Renderer r in ghostObject.GetComponentsInChildren<Renderer>())
        {
            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", color);
            else if (r.material.HasProperty("_Color")) r.material.color = color;
        }
    }

    void BuildIt()
    {
        if (Inventory.instance == null || GameManager.instance == null) return;

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
            Debug.Log("✅ 건설 성공!");
        }

        if (!Inventory.instance.HasItems(heldItem, 1))
            StopBuildMode();
    }
}
