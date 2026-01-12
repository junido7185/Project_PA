using UnityEngine;
using System.Collections;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 2.0f; // 상호작용 가능한 거리 (2미터)
    public LayerMask interactLayer;       // (심화) 특정 레이어만 감지할 때 사용
    private Animator anim; // 애니메이터 가져오기
    public GameObject farmlandPrefab; // 밭 프리팹 연결용

    void Start()
    {
        // 내 몸(또는 자식)에 있는 Animator 찾기
        anim = GetComponentInChildren<Animator>();
    }
    void Update()
    {
        // 스페이스바(Space)를 눌렀을 때 실행
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        // 1. 시작점: 가슴 높이
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        
        // 2. 기본 방향: 정면
        Vector3 direction = transform.forward;

        // 딩컴 스타일 보정
        // 만약 괭이(Hoe)를 들고 있다면? -> 시선을 '대각선 아래'로 깐다!
        ItemData heldItem = Inventory.instance.GetSelectedItem();
        if (heldItem != null && (heldItem.toolType == ToolType.Hoe || heldItem.toolType == ToolType.Seed))
        {
            // 정면(forward) + 아래(down) = 대각선 아래 ↘️
            direction = (transform.forward + Vector3.down).normalized;
        }

        RaycastHit hit;
        float radius = 0.5f;

        // 디버그: 빨간 선이 땅에 박히는지 눈으로 확인하세요!
        Debug.DrawRay(origin, direction * interactDistance, Color.red, 1.0f);

        if (Physics.SphereCast(origin, radius, direction, out hit, interactDistance))
        {
            // 디버그 로그: 뭐가 맞았는지 확인
            // Debug.Log($"🎯 맞은 놈: {hit.collider.name} / 태그: {hit.collider.tag}");

            GameObject hitObj = hit.collider.gameObject;

            if (hitObj.CompareTag("Tree"))
            {
                CheckToolAndChop(hitObj);
            }
            else if (hitObj.CompareTag("Shop"))
            {
                Shop shop = hitObj.GetComponent<Shop>();
                if (shop != null) shop.SellAllItems();
            }
            else if (hitObj.CompareTag("Worktable"))
            {
                if (CraftingUI.instance != null) CraftingUI.instance.ToggleUI();
            }
            // 경작지 (씨앗 심기)
            else if (hitObj.CompareTag("Farmland"))
            {
                heldItem = Inventory.instance.GetSelectedItem();
                Farmland land = hitObj.GetComponent<Farmland>();

                // 손에 '씨앗'을 들고 있고, 밭 스크립트가 있다면
                if (heldItem != null && heldItem.toolType == ToolType.Seed && land != null)
                {
                    if (heldItem.cropPrefab != null)
                    {
                        // 심기 시도 (성공하면 true 반환)
                        if (land.Plant(heldItem.cropPrefab))
                        {
                            // 씨앗 1개 소모
                            Inventory.instance.RemoveItems(heldItem, 1);
                        }
                    }
                }
                else
                {
                    Debug.Log("🌱 씨앗이 필요하거나, 이미 작물이 있습니다.");
                }
            }
            // 작물 수확
            else if (hitObj.CompareTag("Crop"))
            {
                // 맞은 놈이나 그 부모에게서 Crop 스크립트 찾기
                Crop crop = hitObj.GetComponent<Crop>();
                if (crop == null) crop = hitObj.GetComponentInParent<Crop>();

                // 작물이 있고, 다 자랐다면?
                if (crop != null && crop.isFullyGrown)
                {
                    crop.Harvest(); // 수확 실행!
                    
                    // (선택) 줍는 애니메이션
                    if (anim != null) anim.SetTrigger("DoChop"); // 임시로 도끼질 모션 사용
                }
                else
                {
                    Debug.Log("⏳ 아직 덜 자랐습니다.");
                }
            }
            // 땅 (Ground)
            else if (hitObj.CompareTag("Ground")) 
            {
                if (heldItem != null && heldItem.toolType == ToolType.Hoe)
                {
                    Debug.Log("🌱 땅을 갑니다!");
                    
                    Vector3 hitPos = hit.point;
                    float x = Mathf.Round(hitPos.x / 2.0f) * 2.0f;
                    float z = Mathf.Round(hitPos.z / 2.0f) * 2.0f;
                    Vector3 landPos = new Vector3(x, hitPos.y + 0.05f, z);

                    if (farmlandPrefab != null)
                    {
                        Instantiate(farmlandPrefab, landPos, Quaternion.identity);
                        // anim.SetTrigger("DoChop"); 
                    }
                }
            }
        }
    }

    // ⏳ 시간차 공격 함수 (Coroutine)
    IEnumerator ChopAndHarvest(GameObject treeObj)
    {
        // 1. 도끼질 애니메이션 실행!
        if (anim != null)
        {
            anim.SetTrigger("DoChop"); 
        }

        // 2. 도끼가 내려가는 시간(약 0.5초)만큼 기다림
        // (애니메이션 속도에 맞춰서 조절하세요)
        yield return new WaitForSeconds(0.5f);

        // 3. 나무가 아직 존재하면 채집 실행
        if (treeObj != null)
        {
            Gatherable gatherable = treeObj.GetComponent<Gatherable>();
            if (gatherable != null)
            {
                Debug.Log("🪓 쩍!");
                gatherable.Harvest();
            }
        }
    }

    void CheckToolAndChop(GameObject targetObj)
    {
        // 1. 대상(나무)이 요구하는 도구가 뭔지 확인
        Gatherable gatherable = targetObj.GetComponent<Gatherable>();
        if (gatherable == null) return;

        // 2. 현재 내가 손에 든 아이템 가져오기
        ItemData currentItem = Inventory.instance.GetSelectedItem();

        // 3. 비교 (맨손이거나, 도구 타입이 안 맞으면 거절)
        if (gatherable.requiredTool != ToolType.None)
        {
            if (currentItem == null || currentItem.toolType != gatherable.requiredTool)
            {
                Debug.Log("🚫 도구가 필요합니다! (" + gatherable.requiredTool + ")");
                return; // 함수 종료 (안 캡니다)
            }
        }

        // 4. 조건 통과하면 채집 시작
        StartCoroutine(ChopAndHarvest(targetObj));
    }
}