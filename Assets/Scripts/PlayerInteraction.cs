using UnityEngine;
using System.Collections;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 2.0f; 
    public LayerMask interactLayer;       
    private Animator anim; 
    public GameObject farmlandPrefab; 

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 direction = transform.forward;

        // ⭐ [수정] ItemData -> Item
        Item heldItem = Inventory.instance.GetSelectedItem();
        if (heldItem != null && (heldItem.toolType == ToolType.Hoe || heldItem.toolType == ToolType.Seed))
        {
            direction = (transform.forward + Vector3.down).normalized;
        }

        RaycastHit hit;
        Debug.DrawRay(origin, direction * interactDistance, Color.red, 1.0f);

        if (Physics.SphereCast(origin, 0.5f, direction, out hit, interactDistance))
        {
            GameObject hitObj = hit.collider.gameObject;

            // 1. 인터페이스(IInteractable) 체크 (상점, 보관함, 나무 등)
            IInteractable interactable = hitObj.GetComponent<IInteractable>();
            if (interactable == null) interactable = hitObj.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact(this.gameObject);
                return; 
            }

            // 2. 특수 케이스: 땅 (Ground) -> 밭 갈기
            if (hitObj.CompareTag("Ground")) 
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
                    }
                }
            }
            // 3. 특수 케이스: 의자 (Building 태그)
            else if (hitObj.CompareTag("Building"))
            {
                Chair chair = hitObj.GetComponent<Chair>();
                if (chair != null && !chair.isOccupied)
                {
                    Debug.Log("🪑 의자에 앉습니다.");
                    GetComponent<PlayerController>().SitDown(chair.sitPoint);
                }
            }
        }
    }
}