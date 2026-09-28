using UnityEngine;

// Selected NPC identity and home binding; the tent itself never acquires a profession.
public sealed class DemoResident : MonoBehaviour, IInteractable
{
    public string CompanionId { get; private set; }
    public NpcProfile Profile { get; private set; }
    public string TentId { get; private set; }
    public Transform Home { get; private set; }
    public bool HasReceivedTool { get; private set; }
    public WorksiteBinding Worksite { get; private set; }
    public void Configure(string id, NpcProfile profile) { CompanionId = id; Profile = profile; }
    public void Claim(WorldPlacedBuildingRuntime tent, WorldGridService grid)
    {
        if (!string.IsNullOrEmpty(TentId) && TentId != tent.InstanceId) return;
        TentId = tent.InstanceId;
        if (Home == null) { Home = new GameObject("ResidentHome").transform; Home.SetParent(tent.GameObject.transform, false); }
        if (grid.CellToWorld(tent.Entrance, out var position)) Home.position = position;
        var schedule = GetComponent<NpcScheduleController>();
        if (schedule != null) schedule.homePoint = Home;
    }
    public void BindWork(DemoToolUpgrade data, Transform activity)
    {
        if (Worksite != null || data == null || data.production == null || activity == null ||
            !CompanionId.StartsWith(data.companionPrefix, System.StringComparison.Ordinal)) return;
        Worksite = GetComponent<WorksiteBinding>() ?? gameObject.AddComponent<WorksiteBinding>();
        Worksite.BindDemoActivity(CompanionId, Profile, data.production, data.specialty, activity, Home);
        Worksite.Producer.ConfigureStarterSession();
        Worksite.Producer.ConfigureDemoTool(data.starterTool);
    }
    public bool Accepts(Item item)
    {
        var data = DemoPlaceableCatalog.Load()?.Upgrade(item);
        return data != null && Worksite != null && Worksite.Assigned && Worksite.Producer.specialty == data.specialty;
    }
    public void Interact(GameObject player)
    {
        Item held = EquipmentSystem.CurrentHeld(player);
        if (held != null)
        {
            if (!Accepts(held)) return;
            var data = DemoPlaceableCatalog.Load().Upgrade(held);
            if (Worksite.Producer.TryReceiveDemoTool(player.GetComponent<Inventory>(), data))
            {
                HasReceivedTool = true; Worksite.Producer.StartStarterWork();
                FirstDayWorldPresentation.Toast("도구를 전달했습니다. 이전 도구는 Hotbar로 돌아옵니다.");
            }
            return;
        }
        if (Worksite?.Producer.StarterBatchReady == true)
        {
            if (!Worksite.Producer.TryClaimStarterBatch(player.GetComponent<Inventory>()))
                FirstDayWorldPresentation.Toast("가방 공간을 비워주세요.");
            return;
        }
        GetComponent<NpcDialogue>()?.Interact(player);
    }
    public string GetInteractPrompt() => Accepts(Inventory.instance != null ? EquipmentSystem.CurrentHeld(Inventory.instance.gameObject) : null)
        ? "도구 전달 / 기존 도구 돌려받기" : Worksite?.Producer.StarterBatchReady == true ? "생산물 받기" : "대화";
}
