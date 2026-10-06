using System.Collections;
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
    DemoPlacedObject pendingHold;
    float holdBegan;
    void OnDisable() { pendingHold = null; }


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
        if (PlayerInputHandler.ModalOpen || StorageUI.instance != null && StorageUI.instance.IsOpen)
        { pendingHold = null; InteractPromptUI.instance?.ClearPrompt(); return; }
        var placing = GetComponent<WorldHotbarPlacementController>();
        if (placing != null && placing.IsPlacing)
        { pendingHold = null; InteractPromptUI.instance?.SetPrompt(placing.Status + "  ·  마우스 위치 · [E] 확정 · [R] 회전 · [Esc/우클릭] 취소"); return; }
        if (pendingHold != null)
        {
            var pending = pendingHold;
            if (EquipmentSystem.CurrentHeld(gameObject) != null ||
                !TryFindInteractable(out var current, out _) || current != (IInteractable)pending)
                pendingHold = null;
            else if (PlayerInputHandler.Instance.InteractHeld)
            {
                if (Time.unscaledTime - holdBegan >= .5f) { pendingHold = null; pending.BeginMove(gameObject); }
            }
            else { pendingHold = null; pending.Interact(gameObject); }
        }
        if (TryFindInteractable(out var interactable, out _))
        {
            InteractPromptUI.instance?.SetPrompt($"[{(PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.FirstDayControls ? "E" : "Space")}] {interactable.GetInteractPrompt().Replace("Space", "E")}");
            return;
        }
        if (heldBlocked != null)
        {
            InteractPromptUI.instance?.SetPrompt($"[X] 손 비우기 → [E] {heldBlocked.GetInteractPrompt()}");
            return;
        }
        if (netHint != null)
        {
            InteractPromptUI.instance?.SetPrompt("나비 · 잠자리채를 들면 잡을 수 있어요");
            return;
        }

        if (toolHint != null)
        {
            InteractPromptUI.instance?.SetPrompt("[E] " + ToolDurability.DisplayName(EquipmentSystem.CurrentHeld(gameObject)) + " 휘두르기 · " + WrongToolHint(toolHint));
            return;
        }
        if (PlayerInputHandler.Instance?.FirstDayControls == true && FreeTool(EquipmentSystem.CurrentHeld(gameObject)))
        {
            InteractPromptUI.instance?.SetPrompt("[E] " + ToolDurability.DisplayName(EquipmentSystem.CurrentHeld(gameObject)) + " 휘두르기");
            return;
        }

        InteractPromptUI.instance?.ClearPrompt();
    }

    void TryInteract()
    {
        if (!isActiveAndEnabled || PlayerInputHandler.ModalOpen || (ShopPriceUI.instance != null && ShopPriceUI.instance.IsOpen) ||
            (InventoryUI.instance != null && InventoryUI.instance.gameObject.activeSelf) ||
            (SmartphoneUI.instance != null && SmartphoneUI.instance.IsOpen)) return;
        var directPlacement = GetComponent<WorldHotbarPlacementController>();
        if (directPlacement != null && directPlacement.TryUseSelected()) return;
        Vector3 origin = GetInteractOrigin();
        Vector3 direction = transform.forward;

        Item heldItem = EquipmentSystem.CurrentHeld(gameObject);
        if (heldItem != null && (heldItem.toolType == ToolType.Hoe || heldItem.toolType == ToolType.Seed))
            direction = (transform.forward + Vector3.down).normalized;

        Debug.DrawRay(origin, direction * interactDistance, Color.red, 1.0f);

        if (TryFindInteractable(out var interactable, out _))
        {
            if (interactable is DemoPlacedObject placed && placed.CanMove && EquipmentSystem.CurrentHeld(gameObject) == null)
            { pendingHold = placed; holdBegan = Time.unscaledTime; return; }
            bool freeToolTarget = PlayerInputHandler.Instance?.FirstDayControls == true && FreeTool(heldItem) &&
                (interactable is Gatherable || interactable is MiningSpot || interactable is BugCritter ||
                 interactable is FirstDayLandedFish || interactable is FirstDayToolSurface);
            var equipment = GetComponent<EquipmentSystem>();
            if (freeToolTarget && equipment != null && !equipment.TryBeginToolUse()) return;
            if (freeToolTarget && !(interactable is BugCritter)) GatherFeedback.Swish();
            if (freeToolTarget && interactable is FirstDayLandedFish landed && !landed.ReadyToClaim && !FirstDayLandedFish.CanStrike(heldItem))
            { StartCoroutine(FailedToolContact(equipment, interactable)); return; }
            interactable.Interact(gameObject);
            if (!freeToolTarget) equipment?.PlayAction();
            return;
        }

        if (PlayerInputHandler.Instance?.FirstDayControls == true && FreeTool(heldItem))
        {
            var equipment = GetComponent<EquipmentSystem>();
            if (equipment == null || !equipment.TryBeginToolUse()) return;
            GatherFeedback.Swish();
            StartCoroutine(FailedToolContact(equipment, toolHint ?? netHint));
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

    // 손에 든 물건 때문에 대상에서 빠진 가장 가까운 문·보급·대화·작업대(빈손 안내용).
    IInteractable heldBlocked;
    float heldBlockedDistance;
    // 잠자리채 없이 다가간 나비(도구 안내용, E로는 아무 일도 없다).
    IInteractable netHint;
    float netHintDistance;
    IInteractable toolHint;
    float toolHintDistance;

    static bool FreeTool(Item item) => item != null &&
        (item.toolType == ToolType.Axe || item.toolType == ToolType.Pickaxe || item.toolType == ToolType.Net);

    static string WrongToolHint(IInteractable target) => target is Gatherable ? "나무는 도끼로 벨 수 있어요" :
        target is MiningSpot ? "바위는 곡괭이로 캘 수 있어요" : target is FirstDayLandedFish ? "물고기는 도끼나 곡괭이로 포획하세요" : "나비는 잠자리채로 잡을 수 있어요";

    IEnumerator FailedToolContact(EquipmentSystem equipment, IInteractable target)
    {
        Item held = EquipmentSystem.CurrentHeld(gameObject);
        yield return new WaitForSeconds(GatherFeedback.ContactDelay);
        if (!isActiveAndEnabled || PlayerInputHandler.ModalOpen || EquipmentSystem.CurrentHeld(gameObject) != held) yield break;
        var component = target as Component;
        Vector3 flat = component != null ? Vector3.ProjectOnPlane(component.transform.position - transform.position, Vector3.up) : Vector3.zero;
        bool contact = component != null && component.gameObject.activeInHierarchy && flat.sqrMagnitude < 2.05f * 2.05f &&
            flat.sqrMagnitude > .0025f && Vector3.Dot(transform.forward, flat.normalized) >= .75f;
        string message = contact ? WrongToolHint(target) : "빗나갔어요.";
        equipment.FailedUse(message, contact);
        if (contact)
        {
            GatherFeedback.ToolBlocked();
            FirstDayWorldPresentation.Toast(message, false);
        }
    }

    bool TryFindInteractable(out IInteractable interactable, out RaycastHit bestHit)
    {
        interactable = null;
        bestHit = default;
        heldBlocked = null;
        heldBlockedDistance = float.MaxValue;
        netHint = null;
        netHintDistance = float.MaxValue;
        toolHint = null;
        toolHintDistance = float.MaxValue;

        if (!isActiveAndEnabled) return false;
        Vector3 origin = GetInteractOrigin();
        float reach = Mathf.Min(interactDistance, 2.05f); // 2m cell + contact tolerance, not a two-cell radius.
        float bestAngle = float.MaxValue, bestDistance = float.MaxValue;
        bool bestAnchored = false;
        int bestPriority = int.MaxValue;
        foreach (var col in Physics.OverlapSphere(origin, reach + .5f, ~0, QueryTriggerInteraction.Collide))
        {
            if (col == null || col.transform.IsChildOf(transform)) continue;
            var candidate = ResolveInteractable(col);
            var component = candidate as Component;
            if (component == null || candidate is Shop shop && !shop.allowDebugBulkSaleInteraction) continue;
            if (component is Behaviour behaviour && !behaviour.isActiveAndEnabled) continue;
            int priority = 1;
            bool needsEmptyHands = false, needsNet = false, needsTool = false;
            if (PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.FirstDayControls)
            {
                Item held = EquipmentSystem.CurrentHeld(gameObject);
                if (candidate is InventoryFramework.PickupItem pickup)
                { if (!pickup.contextual) continue; if (held != null) needsEmptyHands = true; priority = 0; }
                // 직접 채집 대상은 같은 높이(±2m)에서만 고른다. 절벽 위/아래에서 프롬프트만 뜨고 E가 안 먹는 일을 막는다.
                else if (candidate is Gatherable gather)
                { if (gather.DirectDepleted || Mathf.Abs(gather.transform.position.y - transform.position.y) > 2f) continue; needsTool = FreeTool(held) && held.toolType != ToolType.Axe; if (held?.toolType != ToolType.Axe && !needsTool) continue; priority = 2; }
                else if (candidate is MiningSpot mine)
                { if (mine.DirectDepleted || Mathf.Abs(mine.transform.position.y - transform.position.y) > 2f) continue; needsTool = FreeTool(held) && held.toolType != ToolType.Pickaxe; if (held?.toolType != ToolType.Pickaxe && !needsTool) continue; priority = 2; }
                else if (candidate is BugCritter bug)
                { if (bug.Captured || bug.Fleeing) continue; if (held?.toolType != ToolType.Net) needsNet = true; priority = 2; }
                else if (candidate is FishingSpot)
                { if (held?.toolType != ToolType.FishingRod) continue; priority = 2; }
                else if (candidate is FirstDayLandedFish fish)
                { if (!fish.OnLand) continue; priority = 0; }
                else if (candidate is FirstDayToolSurface)
                { if (!FreeTool(held)) continue; priority = 3; }
                else if (candidate is DaytimeStockPrepPoint tree)
                { if (held != null || !tree.PhysicalFruit || tree.FruitDropped) continue; priority = 3; }
                // 빈손이 필요한 대상은 제외하되, 손 때문에 빠진 가장 가까운 대상은 [X] 안내용으로 기억한다.
                else if (candidate is FirstDaySupplyBox)
                { needsEmptyHands = held != null; priority = 0; }
                else if (candidate is BuildingEntrance)
                { if (held != null) needsEmptyHands = true; priority = 0; }
                else if (candidate is DemoPlacedObject furniture && furniture.Entry?.kind != DemoPlaceableKind.DisplayStand && held != null)
                { if (furniture.GetComponentInChildren<Workbench>() == null) continue; needsEmptyHands = true; }
                else if (candidate is NpcDialogue && held != null) needsEmptyHands = true;
                else if (candidate is DemoResident resident && held != null && !resident.Accepts(held)) needsEmptyHands = true;
                else if (candidate is ShopSlot slot && slot.IsEmpty && (held == null || held.category == ItemCategory.Tool)) continue;
            }
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
            if (needsEmptyHands)
            {
                if (heldBlocked == null || distance < heldBlockedDistance) { heldBlocked = candidate; heldBlockedDistance = distance; }
                continue;
            }
            if (needsNet)
            {
                if (netHint == null || distance < netHintDistance) { netHint = candidate; netHintDistance = distance; }
                continue;
            }
            if (needsTool)
            {
                if (distance < toolHintDistance) { toolHint = candidate; toolHintDistance = distance; }
                continue;
            }
            bool anchored = anchor != null;
            float angle = 1f-facing;
            if (interactable != null && (priority > bestPriority || priority == bestPriority &&
                (bestAnchored && !anchored || bestAnchored==anchored &&
                (angle > bestAngle+.001f || Mathf.Abs(angle-bestAngle)<=.001f && distance>=bestDistance)))) continue;
            interactable = candidate; bestPriority=priority; bestAnchored=anchored; bestAngle=angle; bestDistance=distance;
        }
        return interactable != null;
    }

    static IInteractable ResolveInteractable(Collider col)
    {
        if (col == null) return null;
        if (col.GetComponent<BuildingEntrance>() is BuildingEntrance entrance) return entrance;
        if (col.GetComponent<StorageBox>() is StorageBox storage) return storage;
        if (PlayerInputHandler.Instance?.FirstDayControls == true && col.GetComponentInParent<DemoResident>() is DemoResident resident) return resident;
        if (PlayerInputHandler.Instance?.FirstDayControls == true && col.GetComponentInParent<DemoPlacedObject>() is DemoPlacedObject placed) return placed;
        if (PlayerInputHandler.Instance?.FirstDayControls == true && col.GetComponent<FishingSpot>() is FishingSpot fishing) return fishing;
        return col.GetComponent<IInteractable>()
            ?? col.GetComponentInParent<IInteractable>();
    }
}
