using UnityEngine;
using UnityEngine.EventSystems; 

public class BuildManager : MonoBehaviour
{
    public static BuildManager instance;

    [Header("설정")]
    public float gridSize = 2.0f;       
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

    void Update()
    {
        if (currentBuilding == null || ghostObject == null) return;

        Vector3 targetPos = transform.position + (transform.forward * buildDistance);
        
        float x = Mathf.Round(targetPos.x / gridSize) * gridSize;
        float z = Mathf.Round(targetPos.z / gridSize) * gridSize;
        float y = transform.position.y; 

        ghostObject.transform.position = new Vector3(x, y, z);

        if (Input.GetKeyDown(KeyCode.R)) 
        {
            currentRotationY += 90f;
            ghostObject.transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
        }

        CheckPlaceable(ghostObject.transform.position);

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            if (canBuild) BuildIt();
            else Debug.Log("🚫 장애물 때문에 건설 불가!");
        }
    }

    public void SetBuildMode(BuildingData data)
    {
        if (currentBuilding == data) return;
        StopBuildMode();

        currentBuilding = data;
        currentRotationY = 0f;

        if (data.prefab != null)
        {
            ghostObject = Instantiate(data.prefab);
            Collider[] cols = ghostObject.GetComponentsInChildren<Collider>();
            foreach (var c in cols) c.enabled = false;
        }
    }

    public void StopBuildMode()
    {
        currentBuilding = null;
        if (ghostObject != null) Destroy(ghostObject);
    }

    void CheckPlaceable(Vector3 pos)
    {
        Vector3 boxSize = new Vector3(gridSize * 0.9f, 1f, gridSize * 0.9f);
        Vector3 center = pos + Vector3.up * 1.0f; 

        Collider[] hits = Physics.OverlapBox(center, boxSize / 2, Quaternion.Euler(0, currentRotationY, 0), obstacleLayer);
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

        // ⭐ [수정] ItemData -> Item
        Item heldItem = Inventory.instance.GetSelectedItem();
        if (heldItem == null) return;
        if (ghostObject == null) return;

        GameObject prefabToBuild = currentBuilding.prefab; 
        Vector3 buildPos = ghostObject.transform.position;
        Quaternion buildRot = ghostObject.transform.rotation;
        int price = currentBuilding.price; 

        if (GameManager.instance.money < price)
        {
            Debug.Log("💸 돈 부족!");
            return;
        }

        // 자원 차감
        GameManager.instance.AddMoney(-price); 
        Inventory.instance.RemoveItems(heldItem, 1); 
        Debug.Log("➖ 아이템 차감 완료");

        if (prefabToBuild != null)
        {
            Instantiate(prefabToBuild, buildPos, buildRot);
            Debug.Log("✅ 건설 성공!");
        }

        // 남은 아이템 확인 (다 썼으면 건설모드 종료)
        if (Inventory.instance.HasItems(heldItem, 1) == false)
        {
            StopBuildMode();
        }
    }
}