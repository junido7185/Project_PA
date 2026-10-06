using UnityEngine;

// Interaction adapter. Placement, stock and crafting remain in their existing authorities.
public sealed class DemoPlacedObject : MonoBehaviour, IInteractable
{
    public string InstanceId { get; private set; }
    public DemoPlaceableEntry Entry { get; private set; }
    public WorldBuildingPlacementService Placement { get; private set; }
    public bool CanMove => Entry != null && Entry.canMove &&
        !(DayNightShopLoopController.Instance?.IsShopOpenForCustomers ?? false);
    public void Bind(string id, DemoPlaceableEntry entry, WorldBuildingPlacementService service)
    {
        InstanceId = id; Entry = entry; Placement = service;
        if (entry.kind == DemoPlaceableKind.DisplayStand && displayLight == null)
        {
            displayLight = new GameObject("DisplayWarmLight").AddComponent<Light>();
            displayLight.transform.SetParent(transform, false);
            displayLight.transform.localPosition = new Vector3(0, 2.4f, 0);
            displayLight.type = LightType.Point;
            displayLight.color = new Color(1f, .88f, .73f);
            displayLight.intensity = .8f; displayLight.range = 4.5f;
            displayLight.shadows = LightShadows.None;
            displayLight.enabled = false;
        }
    }
    Light displayLight;
    void LateUpdate()
    {
        if (displayLight == null) return;
        var shop = DemoSettlementController.Instance?.OperatingShop;
        var room = shop != null ? shop.GetComponent<DemoShopInterior>() : null;
        displayLight.enabled = room != null && room.IsInside && room.CoversGround(transform.position)
            && GameClock.Instance != null && GameClock.Instance.CurrentHour >= 16;
    }
    public bool BeginMove(GameObject player) => CanMove &&
        player.GetComponent<WorldHotbarPlacementController>()?.TryMoveExisting(this) == true;
    public void Interact(GameObject player)
    {
        if (Entry?.kind == DemoPlaceableKind.ShopBase) { FirstDayWorldPresentation.Toast("상점 관리는 P키 스마트폰에서 할 수 있어요."); return; }
        var slot = GetComponentInChildren<ShopSlot>();
        if (slot != null)
        {
            if (slot.IsEmpty && EquipmentSystem.CurrentHeld(player) == null)
            { FirstDayWorldPresentation.Toast("상품을 손에 들고 E로 진열하세요."); return; }
            slot.Interact(player); return;
        }
        var bench = GetComponentInChildren<Workbench>();
        if (bench != null) { bench.Interact(player); return; }
        FirstDayWorldPresentation.Toast(Entry?.item != null ? ItemDisplayName.For(Entry.item) : name);
    }
    public string GetInteractPrompt()
    {
        if (Entry?.kind == DemoPlaceableKind.ShopBase)
            return "상점 관리: P키 스마트폰" + (CanMove ? " / E 길게: 이동" : "");
        var slot = GetComponentInChildren<ShopSlot>();
        if (slot != null) return slot.GetInteractPrompt() + (CanMove ? " / E 길게: 이동" : "");
        // 작업대 E = 제작(Canon v2 §10). 상점 운영은 P폰, 가판대는 진열/가격으로 구분한다.
        if (GetComponentInChildren<Workbench>() != null) return "작업대 · 제작" + (CanMove ? " / E 길게: 이동" : "");
        return (Entry?.item != null ? ItemDisplayName.For(Entry.item) : name) + (CanMove ? " · 사용 / E 길게: 이동" : " · 사용");
    }
}
