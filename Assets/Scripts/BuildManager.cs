using UnityEngine;
using UnityEngine.EventSystems; // UI 클릭 방지용

public class BuildManager : MonoBehaviour
{
    // 어디서든 부를 수 있게 싱글톤 유지
    public static BuildManager instance;

    [Header("설정 (건드리지 않아도 됨)")]
    public float gridSize = 2.0f;       // 그리드 크기
    public float buildDistance = 2.0f;  // 내 앞 몇 미터?
    public LayerMask obstacleLayer;     // 빨간불 띄울 장애물 레이어

    [Header("상태 (눈으로 확인용)")]
    public BuildingData currentBuilding; // 현재 지으려는 건물
    private GameObject ghostObject;      // 유령
    private float currentRotationY = 0f; // 회전 각도
    private bool canBuild = true;        // 지을 수 있나?

    void Awake()
    {
        instance = this; 
    }

    void Update()
    {
        // 1. 짓는 중이 아니면 아무것도 안 함 (철저한 무시)
        if (currentBuilding == null || ghostObject == null) return;

        // 2. 위치 계산 (내 발 위치 transform.position 사용 -> 절대 에러 안 남)
        Vector3 targetPos = transform.position + (transform.forward * buildDistance);
        
        // 그리드 스내핑 (반올림)
        float x = Mathf.Round(targetPos.x / gridSize) * gridSize;
        float z = Mathf.Round(targetPos.z / gridSize) * gridSize;
        float y = transform.position.y; // 높이는 내 발바닥

        // 3. 유령 이동
        ghostObject.transform.position = new Vector3(x, y, z);

        // 4. 회전 (R키)
        if (Input.GetKeyDown(KeyCode.R)) 
        {
            currentRotationY += 90f;
            ghostObject.transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
        }

        // 5. 빨간불/초록불 체크
        CheckPlaceable(ghostObject.transform.position);

        // 6. 건설 실행 (좌클릭)
        if (Input.GetMouseButtonDown(0))
        {
            // UI 누른 거면 무시
            if (EventSystem.current.IsPointerOverGameObject()) return;

            if (canBuild) BuildIt();
            else Debug.Log("🚫 장애물 때문에 건설 불가!");
        }
    }

    // 건설 모드 켜기 (아이템 들었을 때 호출)
    public void SetBuildMode(BuildingData data)
    {
        // 같은 거 들고 있으면 무시 (중복 생성 방지)
        if (currentBuilding == data) return;

        // 기존 거 정리
        StopBuildMode();

        currentBuilding = data;
        currentRotationY = 0f;

        // 유령 소환
        if (data.prefab != null)
        {
            ghostObject = Instantiate(data.prefab);
            
            // 유령의 충돌체(Collider) 제거 (그래야 빨간불 체크 가능)
            Collider[] cols = ghostObject.GetComponentsInChildren<Collider>();
            foreach (var c in cols) c.enabled = false;
        }
    }

    // 건설 모드 끄기 (빈손일 때 호출)
    public void StopBuildMode()
    {
        currentBuilding = null;
        if (ghostObject != null) Destroy(ghostObject);
    }

    void CheckPlaceable(Vector3 pos)
    {
        // 상자 크기 (그리드보다 살짝 작게)
        Vector3 boxSize = new Vector3(gridSize * 0.9f, 1f, gridSize * 0.9f);
        Vector3 center = pos + Vector3.up * 1.0f; // 바닥 위 1m 중심

        // 장애물 레이어랑 닿았는지 검사
        Collider[] hits = Physics.OverlapBox(center, boxSize / 2, Quaternion.Euler(0, currentRotationY, 0), obstacleLayer);
        canBuild = (hits.Length == 0);

        // 색깔 바꾸기 (초록/빨강)
        Color color = canBuild ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
        foreach (Renderer r in ghostObject.GetComponentsInChildren<Renderer>())
        {
            // 머테리얼이 있으면 색 변경
            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", color);
            else if (r.material.HasProperty("_Color")) r.material.color = color;
        }
    }

    void BuildIt()
    {
        Debug.Log("🏗️ 건설 시작 시도...");

        // 1. 필수 데이터 체크
        if (Inventory.instance == null || GameManager.instance == null) return;

        ItemData heldItem = Inventory.instance.GetSelectedItem();
        if (heldItem == null) return;
        if (ghostObject == null) return;

        // ⭐ [핵심 1] 중요 데이터 미리 백업 (대피시키기!)
        // 아이템을 지우면 currentBuilding도 null이 될 수 있으므로, 미리 프리팹과 위치를 빼둡니다.
        GameObject prefabToBuild = currentBuilding.prefab; 
        Vector3 buildPos = ghostObject.transform.position;
        Quaternion buildRot = ghostObject.transform.rotation;
        int price = currentBuilding.price; // 가격도 미리 저장

        // 2. 돈 검사
        if (GameManager.instance.money < price)
        {
            Debug.Log("💸 돈 부족!");
            return;
        }

        // 3. 자원 차감 (이제 여기서 currentBuilding이 null이 되어도 상관없음!)
        GameManager.instance.AddMoney(-price); 
        Inventory.instance.RemoveItems(heldItem, 1); 
        Debug.Log("➖ 아이템 차감 완료");

        // 4. 건물 생성 (백업해둔 prefabToBuild 사용)
        if (prefabToBuild != null)
        {
            Instantiate(prefabToBuild, buildPos, buildRot);
            Debug.Log("✅ 건설 성공! (건물 소환됨)");
        }
        else
        {
            Debug.LogError("🚨 프리팹이 비어있어서 건설 실패!");
        }

        // 5. 남은 아이템 확인
        if (Inventory.instance.HasItems(heldItem, 1) == false)
        {
            StopBuildMode();
        }
    }
}