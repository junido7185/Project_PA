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
        {
            pendingHold = null;
            // wallhang-r207: 기념패는 고르는 순간 바닥 배치 미리보기가 열린다 → 벽 걸이 자리 앞이면 [E]는 벽에 걸기(배치는 취소).
            if (TryFindInteractable(out var wall, out _) && wall is IPrecedesPlacement)
                InteractPromptUI.instance?.SetPrompt("[E] " + wall.GetInteractPrompt() + "  ·  바닥에 두려면 벽에서 떨어져요", AnchorOf(wall));
            else InteractPromptUI.instance?.SetPrompt(placing.Status + "  ·  마우스 위치 · [E] 확정 · [R] 회전 · [Esc/우클릭] 취소");
            return;
        }
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
            InteractPromptUI.instance?.SetPrompt($"[{(PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.FirstDayControls ? "E" : "Space")}] {interactable.GetInteractPrompt().Replace("Space", "E")}", AnchorOf(interactable));
            return;
        }
        if (heldBlocked != null)
        {
            InteractPromptUI.instance?.SetPrompt($"[X] 손 비우기 → [E] {heldBlocked.GetInteractPrompt()}", AnchorOf(heldBlocked));
            return;
        }
        if (netHint != null)
        {
            InteractPromptUI.instance?.SetPrompt($"{(netHint is BugCritter bug ? bug.Noun : "곤충")} · 잠자리채를 들면 잡을 수 있어요", AnchorOf(netHint));
            return;
        }
        if (waitHint != null)
        {
            InteractPromptUI.instance?.SetPrompt(waitHint.GetInteractPrompt(), AnchorOf(waitHint));
            return;
        }

        if (toolHint != null)
        {
            InteractPromptUI.instance?.SetPrompt("[E] " + ToolDurability.DisplayName(EquipmentSystem.CurrentHeld(gameObject)) + " 휘두르기 · " + WrongToolHint(toolHint), AnchorOf(toolHint));
            return;
        }
        if (equipHint != null)
        {
            InteractPromptUI.instance?.SetPrompt(EquipHint(equipHint), AnchorOf(equipHint));
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
        // 대화창이 떠 있으면 [E]는 대화 넘기기(글자 다 보이기 → 닫기)만 한다(레퍼런스 04·05).
        if (DialogueUI.TryAdvance()) return;
        var directPlacement = GetComponent<WorldHotbarPlacementController>();
        // 기념패(배치 장식)를 들고 벽 걸이 자리를 보고 있으면 바닥 배치보다 벽에 걸기가 먼저다.
        if (TryFindInteractable(out var wallFirst, out _) && wallFirst is IPrecedesPlacement)
        {
            if (directPlacement != null && directPlacement.IsPlacing) directPlacement.Cancel();
            wallFirst.Interact(gameObject); return;
        }
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
    // 안내 말풍선을 붙일 대상의 Transform(MonoBehaviour가 아니면 화면 아래 그대로).
    static Transform AnchorOf(IInteractable target) => target is Component component && component != null ? component.transform : null;

    // 잠자리채 없이 다가간 곤충(도구 안내용, E로는 아무 일도 없다).
    IInteractable netHint;
    float netHintDistance;
    // 2026-11-09 날고 있는 곤충(호박벌·방아깨비의 이동, 놀라 나는 중): 대상은 아니지만 "내려앉을 때까지 기다려요"를 붙인다(E는 빈 휘두르기 그대로).
    IInteractable waitHint;
    float waitHintDistance;
    IInteractable toolHint;
    float toolHintDistance;
    // 빈손(이어하기 직후·X 뒤)으로 나무/바위 앞에 서면 아무 안내가 없어 E가 먹지 않는 것처럼 보였다(10/12 Windows 후보).
    // 핫바에 맞는 도구가 있으면 그 번호를 알려 준다. 선택·채집 권위는 그대로 숫자키/E 경로다.
    IInteractable equipHint;
    float equipHintDistance;

    static string EquipHint(IInteractable target)
    {
        bool tree = target is Gatherable;
        ToolType want = tree ? ToolType.Axe : ToolType.Pickaxe;
        string tool = tree ? "도끼" : "곡괭이", act = tree ? "나무 채집" : "광석 채집";
        var hotbar = Inventory.instance != null ? Inventory.instance.hotbar : null;
        if (hotbar != null)
            for (int i = 0; i < hotbar.slots.Count; i++)
                if (!hotbar.slots[i].IsEmpty && hotbar.slots[i].item != null && hotbar.slots[i].item.toolType == want)
                    return "[" + (i + 1) + "] " + tool + " 들기 → [E] " + act;
        return tree ? "나무는 도끼로 벨 수 있어요" : "바위는 곡괭이로 캘 수 있어요";
    }

    static bool FreeTool(Item item) => item != null &&
        (item.toolType == ToolType.Axe || item.toolType == ToolType.Pickaxe || item.toolType == ToolType.Net);

    static string WrongToolHint(IInteractable target) => target is Gatherable ? "나무는 도끼로 벨 수 있어요" :
        target is MiningSpot ? "바위는 곡괭이로 캘 수 있어요" : target is FirstDayLandedFish ? "물고기는 도끼나 곡괭이로 포획하세요"
        : target is BugCritter bug ? BugCritter.Josa(bug.Noun, "은", "는") + " 잠자리채로 잡을 수 있어요" : "곤충은 잠자리채로 잡을 수 있어요";

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
        waitHint = null;
        waitHintDistance = float.MaxValue;
        toolHint = null;
        toolHintDistance = float.MaxValue;
        equipHint = null;
        equipHintDistance = float.MaxValue;

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
            bool needsEmptyHands = false, needsNet = false, needsTool = false, needsEquip = false, needsLanding = false;
            if (PlayerInputHandler.Instance != null && PlayerInputHandler.Instance.FirstDayControls)
            {
                Item held = EquipmentSystem.CurrentHeld(gameObject);
                if (candidate is InventoryFramework.PickupItem pickup)
                { if (!pickup.contextual) continue; if (held != null) needsEmptyHands = true; priority = 0; }
                // 직접 채집 대상은 같은 높이(±2m)에서만 고른다. 절벽 위/아래에서 프롬프트만 뜨고 E가 안 먹는 일을 막는다.
                else if (candidate is Gatherable gather)
                { if (gather.DirectDepleted || Mathf.Abs(gather.transform.position.y - transform.position.y) > 2f) continue; needsTool = FreeTool(held) && held.toolType != ToolType.Axe; needsEquip = held == null; if (held?.toolType != ToolType.Axe && !needsTool && !needsEquip) continue; priority = 2; }
                else if (candidate is MiningSpot mine)
                { if (mine.DirectDepleted || Mathf.Abs(mine.transform.position.y - transform.position.y) > 2f) continue; needsTool = FreeTool(held) && held.toolType != ToolType.Pickaxe; needsEquip = held == null; if (held?.toolType != ToolType.Pickaxe && !needsTool && !needsEquip) continue; priority = 2; }
                else if (candidate is BugCritter bug)
                { if (bug.Captured) continue; if (bug.Fleeing) needsLanding = true; else if (held?.toolType != ToolType.Net) needsNet = true; priority = 2; }
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
            if (needsLanding)
            {
                if (waitHint == null || distance < waitHintDistance) { waitHint = candidate; waitHintDistance = distance; }
                continue;
            }
            if (needsTool)
            {
                if (distance < toolHintDistance) { toolHint = candidate; toolHintDistance = distance; }
                continue;
            }
            if (needsEquip)
            {
                if (distance < equipHintDistance) { equipHint = candidate; equipHintDistance = distance; }
                continue;
            }
            bool anchored = anchor != null;
            float angle = 1f-facing;
            if (interactable != null && (priority > bestPriority || priority == bestPriority &&
                (bestAnchored && !anchored || bestAnchored==anchored &&
                (angle > bestAngle+.001f || Mathf.Abs(angle-bestAngle)<=.001f && distance>=bestDistance)))) continue;
            interactable = candidate; bestPriority=priority; bestAnchored=anchored; bestAngle=angle; bestDistance=distance;
        }
        // 2026-11-09 날고 있는 곤충이 장식 나무·바위(벌목·채광 불가 표면)만큼 가까우면 그 안내 대신 "내려앉을 때까지 기다려요"를 보인다
        // (grasshopper-reg189: 장식 나무 옆에 내려앉는 방아깨비를 보는데 "장식 나무 · 벌목 불가"가 떴다). E는 빈 휘두르기 그대로.
        // reg205: 방아깨비가 장식 나무보다 .3m 넘게 멀리 뛰어도 나무 안내가 이겼다 → 앞(손 닿는 거리·정면)에 나는·뛰는 곤충이 있으면 거리와 상관없이 기다림 안내.
        if (interactable is FirstDayToolSurface && waitHint != null) interactable = null;
        return interactable != null;
    }

    static IInteractable ResolveInteractable(Collider col)
    {
        if (col == null) return null;
        if (col.GetComponent<BuildingEntrance>() is BuildingEntrance entrance) return entrance;
        if (col.GetComponent<StorageBox>() is StorageBox storage) return storage;
        // r204: 러그 교체 지점은 가게 배치물의 자식이라 아래 DemoPlacedObject로 먼저 풀려 '든 물건' 때문에 빠졌다 → 자기 것을 먼저.
        if (col.GetComponent<ShopRugSpot>() is ShopRugSpot rug) return rug;
        if (col.GetComponent<ShopWallSpot>() is ShopWallSpot wall) return wall;
        if (col.GetComponent<ShopDeskSpot>() is ShopDeskSpot desk) return desk;
        if (PlayerInputHandler.Instance?.FirstDayControls == true && col.GetComponentInParent<DemoResident>() is DemoResident resident) return resident;
        if (PlayerInputHandler.Instance?.FirstDayControls == true && col.GetComponentInParent<DemoPlacedObject>() is DemoPlacedObject placed) return placed;
        if (PlayerInputHandler.Instance?.FirstDayControls == true && col.GetComponent<FishingSpot>() is FishingSpot fishing) return fishing;
        return col.GetComponent<IInteractable>()
            ?? col.GetComponentInParent<IInteractable>();
    }
}
