using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 2.0f;
    public LayerMask interactLayer;
    private Animator anim;
    public GameObject farmlandPrefab;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();

        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnInteractPressed += TryInteract;
    }

    void OnDestroy()
    {
        if (PlayerInputHandler.Instance != null)
            PlayerInputHandler.Instance.OnInteractPressed -= TryInteract;
    }

    // §3 InteractPromptUI — 매 프레임 근접 IInteractable 감지 → 프롬프트 갱신
    void Update()
    {
        Vector3 origin    = transform.position + Vector3.up * 0.5f;
        Vector3 direction = transform.forward;
        RaycastHit hit;

        if (Physics.SphereCast(origin, 0.5f, direction, out hit, interactDistance))
        {
            var go = hit.collider.gameObject;
            IInteractable interactable = go.GetComponent<IInteractable>()
                                      ?? go.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                InteractPromptUI.instance?.SetPrompt($"[Space] {interactable.GetInteractPrompt()}");
                return;
            }
        }
        InteractPromptUI.instance?.ClearPrompt();
    }

    void TryInteract()
    {
        Vector3 origin    = transform.position + Vector3.up * 0.5f;
        Vector3 direction = transform.forward;

        Item heldItem = Inventory.instance.GetSelectedItem();
        if (heldItem != null && (heldItem.toolType == ToolType.Hoe || heldItem.toolType == ToolType.Seed))
            direction = (transform.forward + Vector3.down).normalized;

        RaycastHit hit;
        Debug.DrawRay(origin, direction * interactDistance, Color.red, 1.0f);

        if (!Physics.SphereCast(origin, 0.5f, direction, out hit, interactDistance)) return;

        GameObject hitObj = hit.collider.gameObject;

        // 1. IInteractable 체크
        IInteractable interactable = hitObj.GetComponent<IInteractable>()
                                  ?? hitObj.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            interactable.Interact(gameObject);
            return;
        }

        // 2. 땅 — 밭 갈기
        if (hitObj.CompareTag("Ground"))
        {
            if (heldItem != null && heldItem.toolType == ToolType.Hoe)
            {
                Debug.Log("🌱 땅을 갑니다!");
                Vector3 hitPos = hit.point;
                float x = Mathf.Round(hitPos.x / 2.0f) * 2.0f;
                float z = Mathf.Round(hitPos.z / 2.0f) * 2.0f;
                Vector3 landPos = new Vector3(x, hitPos.y + 0.05f, z);
                if (farmlandPrefab != null) Instantiate(farmlandPrefab, landPos, Quaternion.identity);
            }
        }
        // 3. 의자
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
