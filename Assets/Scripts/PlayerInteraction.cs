using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float interactDistance = 3.0f;
    public float interactRadius = 0.75f;
    public float interactOriginHeight = 1.0f;
    public float fallbackSearchRadius = 2.25f;
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

    void Update()
    {
        if (TryFindInteractable(out var interactable, out _))
        {
            InteractPromptUI.instance?.SetPrompt($"[Space] {interactable.GetInteractPrompt()}");
            return;
        }

        InteractPromptUI.instance?.ClearPrompt();
    }

    void TryInteract()
    {
        Vector3 origin = GetInteractOrigin();
        Vector3 direction = transform.forward;

        Item heldItem = Inventory.instance != null ? Inventory.instance.GetSelectedItem() : null;
        if (heldItem != null && (heldItem.toolType == ToolType.Hoe || heldItem.toolType == ToolType.Seed))
            direction = (transform.forward + Vector3.down).normalized;

        Debug.DrawRay(origin, direction * interactDistance, Color.red, 1.0f);

        if (TryFindInteractable(out var interactable, out _))
        {
            interactable.Interact(gameObject);
            return;
        }

        if (!Physics.SphereCast(origin, interactRadius, direction, out var hit, interactDistance))
            return;

        GameObject hitObj = hit.collider.gameObject;

        if (hitObj.CompareTag("Ground"))
        {
            if (heldItem != null && heldItem.toolType == ToolType.Hoe)
            {
                Debug.Log("밭을 갑니다.");
                Vector3 hitPos = hit.point;
                float x = Mathf.Round(hitPos.x / 2.0f) * 2.0f;
                float z = Mathf.Round(hitPos.z / 2.0f) * 2.0f;
                Vector3 landPos = new Vector3(x, hitPos.y + 0.05f, z);
                if (farmlandPrefab != null) Instantiate(farmlandPrefab, landPos, Quaternion.identity);
            }
        }
        else if (hitObj.CompareTag("Building"))
        {
            Chair chair = hitObj.GetComponent<Chair>();
            if (chair != null && !chair.isOccupied)
            {
                Debug.Log("의자에 앉았습니다.");
                GetComponent<PlayerController>().SitDown(chair.sitPoint);
            }
        }
    }

    Vector3 GetInteractOrigin()
    {
        return transform.position + Vector3.up * interactOriginHeight;
    }

    bool TryFindInteractable(out IInteractable interactable, out RaycastHit bestHit)
    {
        interactable = null;
        bestHit = default;

        Vector3 origin = GetInteractOrigin();
        Vector3 direction = transform.forward;

        if (Physics.SphereCast(origin, interactRadius, direction, out bestHit, interactDistance,
                ~0, QueryTriggerInteraction.Collide))
        {
            interactable = ResolveInteractable(bestHit.collider);
            if (interactable != null) return true;
        }

        float radius = Mathf.Max(fallbackSearchRadius, interactRadius);
        Collider[] nearby = Physics.OverlapSphere(origin, radius, ~0, QueryTriggerInteraction.Collide);
        float bestScore = float.MaxValue;
        Collider bestCollider = null;

        foreach (var col in nearby)
        {
            if (col == null || col.transform.IsChildOf(transform)) continue;

            var candidate = ResolveInteractable(col);
            if (candidate == null) continue;

            Vector3 closest = col.ClosestPoint(origin);
            Vector3 to = closest - origin;
            float distance = to.magnitude;
            if (distance > radius) continue;

            Vector3 flatTo = new Vector3(to.x, 0f, to.z);
            float facing = flatTo.sqrMagnitude > 0.001f
                ? Vector3.Dot(transform.forward, flatTo.normalized)
                : 1f;

            if (distance > 1.2f && facing < -0.15f) continue;

            float score = distance - Mathf.Max(0f, facing) * 0.55f;
            if (score >= bestScore) continue;

            bestScore = score;
            bestCollider = col;
            interactable = candidate;
        }

        if (interactable == null || bestCollider == null) return false;

        return true;
    }

    static IInteractable ResolveInteractable(Collider col)
    {
        if (col == null) return null;
        return col.GetComponent<IInteractable>()
            ?? col.GetComponentInParent<IInteractable>();
    }
}
