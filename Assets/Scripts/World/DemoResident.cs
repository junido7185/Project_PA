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
                // P7: 동행의 반응과 생산 효과, 돌아온 이전 도구를 한 카드에 보인다.
                var routine = GetComponent<DemoCompanionRoutine>();
                string speaker = routine != null && !string.IsNullOrEmpty(routine.DisplayName) ? routine.DisplayName + ": " : "";
                FirstDayWorldPresentation.Toast($"{speaker}이 도구면 훨씬 빨리 할 수 있어요!\n{ItemDisplayName.For(held)} 전달 · 작업 속도 ×{data.npcEfficiency:0.#} · " +
                    $"이전 {(data.starterTool != null ? ItemDisplayName.For(data.starterTool) : "도구")}는 핫바로 돌아왔어요.");
            }
            return;
        }
        if (Worksite?.Producer.StarterBatchReady == true)
        {
            if (!Worksite.Producer.TryClaimStarterBatch(player.GetComponent<Inventory>()))
                FirstDayWorldPresentation.Toast("가방 공간을 비워주세요.");
            else if (Worksite.Producer.productionData != null) DemoHotbarPreference.Prefer(Worksite.Producer.productionData.producedItem);
            return;
        }
        GetComponent<NpcDialogue>()?.Interact(player);
    }
    public string GetInteractPrompt()
    {
        if (Accepts(Inventory.instance != null ? EquipmentSystem.CurrentHeld(Inventory.instance.gameObject) : null))
            return "도구 전달 / 기존 도구 돌려받기";
        if (Worksite?.Producer.StarterBatchReady == true) return "생산물 받기";
        // P7: 지금 무엇을 하는 동행인지 이름과 함께 보인다.
        var routine = GetComponent<DemoCompanionRoutine>();
        return routine != null && !string.IsNullOrEmpty(routine.DisplayName) ? $"{routine.DisplayName} · {routine.Status} · 대화" : "대화";
    }
}
