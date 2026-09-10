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
        if (!isActiveAndEnabled || (ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen) ||
            (InventoryUI.instance != null && InventoryUI.instance.gameObject.activeSelf) ||
            (SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen)) return;
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

        if (!isActiveAndEnabled) return false;
        Vector3 origin = GetInteractOrigin();
        float reach = Mathf.Min(interactDistance, 2.05f); // 2m cell + contact tolerance, not a two-cell radius.
        float bestAngle = float.MaxValue, bestDistance = float.MaxValue;
        bool bestAnchored = false;
        foreach (var col in Physics.OverlapSphere(origin, reach + .5f, ~0, QueryTriggerInteraction.Collide))
        {
            if (col == null || col.transform.IsChildOf(transform)) continue;
            var candidate = ResolveInteractable(col);
            var component = candidate as Component;
            if (component == null || candidate is Shop shop && !shop.allowDebugBulkSaleInteraction) continue;
            Transform anchor = component.transform.Find("InteractionAnchor");
            Vector3 point = anchor != null ? anchor.position : col.bounds.center;
            Vector3 flat = Vector3.ProjectOnPlane(point-transform.position, Vector3.up);
            float distance = flat.magnitude;
            if (distance > reach || distance < .05f) continue;
            float facing = Vector3.Dot(transform.forward, flat / distance);
            if (facing < .75f) continue;
            if (anchor != null)
            {
                var outward = Vector3.ProjectOnPlane(anchor.position-component.transform.position,Vector3.up).normalized;
                if (outward.sqrMagnitude < .1f) outward = Vector3.ProjectOnPlane(anchor.forward,Vector3.up).normalized;
                if (Vector3.Dot(-flat.normalized,outward) < .6f || distance > 1.25f) continue;
            }
            // 장애물 너머의 가까운 대상도 선택하지 않는다. 표식/trigger는 가림으로 취급하지 않는다.
            Vector3 endpoint = new Vector3(point.x,origin.y,point.z);
            bool blocked = false;
            foreach (var hit in Physics.RaycastAll(origin,(endpoint-origin).normalized,Mathf.Max(0,distance-.4f),~0,QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform) || ResolveInteractable(hit.collider) == candidate ||
                    hit.collider.transform.IsChildOf(component.transform)) continue;
                blocked = true; break;
            }
            if (blocked) continue;
            bool anchored = anchor != null;
            float angle = 1f-facing;
            if (interactable != null && (bestAnchored && !anchored || bestAnchored==anchored &&
                (angle > bestAngle+.001f || Mathf.Abs(angle-bestAngle)<=.001f && distance>=bestDistance))) continue;
            interactable = candidate; bestAnchored=anchored; bestAngle=angle; bestDistance=distance;
        }
        return interactable != null;
    }

    static IInteractable ResolveInteractable(Collider col)
    {
        if (col == null) return null;
        return col.GetComponent<IInteractable>()
            ?? col.GetComponentInParent<IInteractable>();
    }
}
