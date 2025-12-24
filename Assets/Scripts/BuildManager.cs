using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public static BuildManager instance;

    public BuildingData currentBuilding; // 현재 짓으려고 선택한 건물
    private GameObject ghostObject;      // 마우스 따라다니는 미리보기 건물

    void Awake()
    {
        instance = this;
    }

    // 외부(UI 등)에서 "이 건물을 짓겠다!"고 호출하는 함수
    public void StartBuilding(BuildingData building)
    {
        currentBuilding = building;

        // 기존에 떠있던 고스트가 있으면 삭제
        if (ghostObject != null) Destroy(ghostObject);

        // 새 고스트 생성 (반투명하게 보이게 하면 좋지만, 일단은 그냥 생성)
        ghostObject = Instantiate(currentBuilding.prefab);
        
        // 고스트는 충돌하면 안 되니까 Collider를 끕니다 (중요!)
        // (만약 프리팹 구조가 복잡하면 GetComponentsInChildren로 다 꺼야 함)
        Collider col = ghostObject.GetComponent<Collider>();
        if(col != null) col.enabled = false;

        Debug.Log("🔨 건설 모드 시작: " + building.buildingName);
    }

    void Update()
    {
        // 👇 테스트용 치트키 추가
        if (Input.GetKeyDown(KeyCode.B))
        {
            // Resources 폴더에서 상점 데이터를 불러와서 건설 시작
            BuildingData shopData = Resources.Load<BuildingData>("Buildings/Data_Shop");
            if(shopData != null) StartBuilding(shopData);
        }
        
        // 건설 모드가 아니면 아무것도 안 함
        if (currentBuilding == null || ghostObject == null) return;

        // 1. 마우스가 가리키는 땅의 위치 찾기 (Raycast)
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // "Ground" 레이어만 체크하면 좋지만, 일단 전체 체크
        if (Physics.Raycast(ray, out hit, 100f))
        {
            // 2. 그리드 스내핑 (반올림해서 딱딱 끊어지게 위치 잡기)
            // Mathf.Round를 쓰면 1.2 -> 1.0, 1.8 -> 2.0 으로 보정됨
            float x = Mathf.Round(hit.point.x);
            float z = Mathf.Round(hit.point.z);

            // 고스트 이동 (Y값은 건물 높이 절반인 1.5f 정도로 고정)
            ghostObject.transform.position = new Vector3(x, 1.5f, z);

            // 3. 클릭해서 건설 확정
            if (Input.GetMouseButtonDown(0)) // 좌클릭
            {
                BuildIt(x, z);
            }
        }
        
        // 우클릭하면 건설 취소
        if (Input.GetMouseButtonDown(1))
        {
            CancelBuilding();
        }
    }

    void BuildIt(float x, float z)
    {
        // 돈 확인
        if (GameManager.instance.money < currentBuilding.price)
        {
            Debug.Log("💸 돈이 부족합니다!");
            return;
        }

        // 돈 차감
        GameManager.instance.AddMoney(-currentBuilding.price);

        // 실제 건물 생성
        Instantiate(currentBuilding.prefab, new Vector3(x, 1.5f, z), Quaternion.identity);
        Debug.Log("✅ 건설 완료!");

        // 건설 모드 종료
        CancelBuilding();
    }

    void CancelBuilding()
    {
        currentBuilding = null;
        if (ghostObject != null) Destroy(ghostObject);
    }
}