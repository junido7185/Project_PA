using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// P4 connects the selected opening companions to existing placement, activities and producers.
public sealed class FirstProductionController : MonoBehaviour
{
    public const string ResourceRoot = "DepartureTutorial/Worksites/";
    public StarterWorksiteProfile[] worksiteProfiles;
    public bool IsReady { get; private set; }
    public bool Complete => IsReady && _sites.Count == 2 && _sites.Values.All(s => s.Producer.StarterClaimed);
    public string Objective { get; private set; } = "동행자들의 첫 작업을 준비하세요.";
    public FirstIslandSettlementController Settlement { get; private set; }
    public IReadOnlyDictionary<string, StarterWorksiteInteraction> Sites => _sites;
    public string[] Owners => Settlement.Selection.ConfirmedIds.ToArray();
    readonly Dictionary<string, StarterWorksiteInteraction> _sites = new Dictionary<string, StarterWorksiteInteraction>();
    readonly Dictionary<string, Button> _buttons = new Dictionary<string, Button>();
    float _completedAt = -1;
    TextMeshProUGUI _status;

    // 기존 저장/검사 호출 호환용 조회. 생산자 설정은 인스턴스 프로필과 WorksiteBinding만 사용한다.
    static StarterWorksiteProfile AuthoredProfile(string owner) => StarterWorksiteProfile.Find(
        Resources.Load<GameObject>("DepartureTutorial/DepartureContinuation")?.GetComponent<FirstProductionController>()?.worksiteProfiles, owner);
    public static string Role(string id) => AuthoredProfile(id)?.profileId;
    StarterWorksiteProfile Profile(string owner) => StarterWorksiteProfile.Find(worksiteProfiles, owner);
    public static string WorksiteId(string owner) => "pa-worksite-" + owner;
    public bool OwnsWorksite(string id) => IsReady && Owners.Any(o => WorksiteId(o) == id);
    public static WorldBuildingPlacementDefinition Definition(string owner) => AuthoredProfile(owner)?.Definition;

    void Awake() { Settlement = GetComponent<FirstIslandSettlementController>(); }
    void Update()
    {
        if (!Settlement.SettlementCompleted) return;
        if (_completedAt < 0) _completedAt = Time.unscaledTime;
        if (!IsReady && Time.unscaledTime - _completedAt >= 3) Initialize();
        if (!IsReady) return;
        foreach (string owner in Owners)
        {
            if (!_sites.ContainsKey(owner) && Settlement.Placement.TryGetPlacement(WorksiteId(owner), out var placed)) Attach(owner, placed);
            if (_sites.TryGetValue(owner, out var site))
                site.Binding.RefreshAnchors();
        }
        string next = Owners.FirstOrDefault(o => !_sites.ContainsKey(o));
        Objective = next != null ? Profession(next) + "의 첫 작업터를 준비하세요." :
            _sites.Values.Any(s => !s.PlayerActivityCompleted) ? "작업터에서 첫 활동을 직접 해보세요." :
            _sites.Values.Any(s => s.Producer.StarterWorking) ? "동행자들의 첫 생산 활동을 확인하세요." :
            !Complete ? "정착 지원 물량을 수령하세요." : "첫 상품을 준비하세요.";
        foreach (string owner in Owners)
            _buttons[owner].GetComponentInChildren<TextMeshProUGUI>().text = Profession(owner) + (_sites.ContainsKey(owner) ? " 작업터 이동" : " 작업터 설치 · 무료");
        _status.text = string.Join("\n", Owners.Select(o => Profession(o) + " · " + (_sites.TryGetValue(o,out var s) ? s.Status : "작업터를 선택하세요")));
    }

    public WorldPlacedBuildingRuntime Placed(string owner)
    {
        Settlement.Placement.TryGetPlacement(WorksiteId(owner),out var p); return p;
    }
    string Profession(string owner) => Settlement.Selection.candidates.First(c => c.id == owner).profession;
    public void Initialize()
    {
        if (IsReady || !Settlement.SettlementCompleted) return;
        if (Owners.Length != 2 || Owners.Any(owner => Profile(owner) == null || !Profile(owner).IsValid))
            throw new InvalidOperationException("P4 requires valid worksite profiles for the two confirmed companions.");
        foreach (var owner in Owners) Settlement.Placement.RegisterDefinition(WorksiteId(owner), Profile(owner).Definition);
        var root = transform.Find("PA_SettlementUI");
        for (int i=0;i<Owners.Length;i++)
        {
            string owner = Owners[i];
            var panel = DepartureCompanionSelection.Panel(root, WorksiteId(owner), 1480, 682+i*74, 405, 62, new Color(.65f,.34f,.21f));
            panel.GetComponent<Image>().raycastTarget = true;
            var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel.GetComponent<Image>();
            button.onClick.AddListener(() => Settlement.Begin(WorksiteId(owner)));
            var label = Settlement.Selection.Label(panel,"Label",Profession(owner),8,4,389,54,23,new Color(.97f,.94f,.85f)); label.alignment=TextAlignmentOptions.Center;
            _buttons[owner] = button;
        }
        var statusPanel = DepartureCompanionSelection.Panel(root,"ProductionStatus",1030,26,855,142,new Color(.97f,.94f,.85f,.98f));
        _status = Settlement.Selection.Label(statusPanel,"Status","",20,12,815,118,23,new Color(.17f,.23f,.23f));
        IsReady = true;
        Debug.Log("[VS-P4] READY owners=" + string.Join(",",Owners));
    }

    StarterWorksiteInteraction Attach(string owner, WorldPlacedBuildingRuntime placed)
    {
        var npc = Settlement.Voyage.Companions[Array.IndexOf(Owners,owner)];
        var binding = npc.GetComponent<WorksiteBinding>() ?? npc.AddComponent<WorksiteBinding>();
        Settlement.Placement.TryGetPlacement(FirstIslandSettlementController.ShelterId(owner), out var home);
        binding.Bind(owner, Settlement.Selection.candidates.First(c => c.id == owner).profile,
            Profile(owner), placed, Settlement.Voyage.IslandGrid, home?.GameObject.transform);
        binding.Producer.ConfigureStarterSession();
        var site = placed.GameObject.AddComponent<StarterWorksiteInteraction>();
        site.Configure(binding);
        _sites.Add(owner,site);
        return site;
    }

    public FirstProductionSaveData CaptureState()
    {
        var state = new FirstProductionSaveData { started = IsReady, completed = Complete };
        if (!state.started) return state;
        foreach (string owner in Owners)
        {
            var entry = new StarterWorksiteSaveData { companionId=owner, role=Role(owner) };
            if (_sites.TryGetValue(owner,out var site))
            {
                var p = Placed(owner);
                entry.placement = new WorldPlacedBuildingSaveData { instanceId=p.InstanceId, buildingId=p.Definition.StableId,
                    anchorX=p.Anchor.x, anchorZ=p.Anchor.y, rotationQuarterTurns=p.QuarterTurns };
                entry.playerActivityCompleted=site.PlayerActivityCompleted; entry.seedIssued=site.SeedIssued;
                entry.producer=site.Producer.CaptureStarterState(); entry.farm=site.Farm != null ? site.Farm.CaptureSaveState() : null;
            }
            state.worksites.Add(entry);
        }
        return state;
    }

    public static bool IsValidSave(FirstProductionSaveData state, FirstSettlementSaveData settlement)
    {
        if (state == null || state.version != 1 || state.worksites == null) return false;
        if (!state.started) return !state.completed && state.worksites.Count == 0;
        if (settlement == null || !settlement.settlementCompleted || settlement.companionIds == null ||
            settlement.companionIds.Length != 2 || settlement.companionIds.Any(string.IsNullOrWhiteSpace) ||
            settlement.companionIds.Distinct().Count() != 2 || state.worksites.Count!=2 ||
            state.worksites.Any(s=>s==null) || state.worksites.Select(s=>s.companionId).Distinct().Count()!=2) return false;
        foreach(var s in state.worksites)
        {
            var profile = AuthoredProfile(s.companionId);
            if (!settlement.companionIds.Contains(s.companionId) || profile == null || !profile.IsValid || s.role != profile.profileId) return false;
            if (IsEmptyPlacement(s.placement))
            {
                if (!IsEmptyProducer(s.producer) || !IsEmptyFarm(s.farm) || s.playerActivityCompleted || s.seedIssued) return false;
                continue;
            }
            var p=s.placement;var producer=s.producer;
            // Coordinates, footprint, obstacles and navigation belong to Placement.TryPlace during restore.
            if (p.instanceId!=WorksiteId(s.companionId) || p.buildingId!=profile.Definition.StableId ||
                p.rotationQuarterTurns<0 || p.rotationQuarterTurns>3 || p.storedItems != null && p.storedItems.Count != 0 || producer==null ||
                float.IsNaN(producer.elapsed) || float.IsInfinity(producer.elapsed) || producer.elapsed<0 || producer.pendingAmount<0 ||
                producer.stockCount<0 || producer.stockCount>profile.productionData.maxInventoryCount ||
                producer.claimed && (producer.stockCount!=0 || producer.active || producer.ready) ||
                producer.ready != (producer.stockCount>0) || producer.active && (producer.ready || producer.claimed) ||
                (producer.active || producer.ready || producer.claimed) && !s.playerActivityCompleted) return false;
            if (profile.activityKind != WorksiteActivityKind.Farming)
            {
                if (s.seedIssued || !IsEmptyFarm(s.farm)) return false;
            }
            else if (s.farm == null || s.farm.plotId != "p4-farm-" + s.companionId || s.farm.currentStageIndex < 0 ||
                float.IsNaN(s.farm.secondsUntilNextStage) || float.IsInfinity(s.farm.secondsUntilNextStage) ||
                s.farm.secondsUntilNextStage < 0 || s.farm.planted && !s.seedIssued) return false;
        }
        return state.completed==state.worksites.All(s=>s.producer!=null && s.producer.claimed);
    }

    // JsonUtility can materialize null inline records as zero-filled objects. Only an entirely
    // empty record means absence; orphaned progress or partially populated IDs remain invalid.
    static bool IsEmptyPlacement(WorldPlacedBuildingSaveData p) => p == null ||
        string.IsNullOrEmpty(p.instanceId) && string.IsNullOrEmpty(p.buildingId) && p.anchorX == 0 && p.anchorZ == 0 &&
        p.rotationQuarterTurns == 0 && (p.storedItems == null || p.storedItems.Count == 0);
    static bool IsEmptyProducer(StarterProducerSaveData p) => p == null ||
        !p.active && !p.ready && !p.claimed && p.elapsed == 0 && p.pendingAmount == 0 && p.stockCount == 0;
    static bool IsEmptyFarm(FarmPlotSaveData p) => p == null ||
        string.IsNullOrEmpty(p.plotId) && !p.planted && p.currentStageIndex == 0 && p.secondsUntilNextStage == 0;

    public void ClearWorksitesForRestore()
    {
        foreach(var site in _sites.Values) if(site!=null) site.Binding.Unassign();
        if (Settlement != null && Settlement.Placement != null)
        {
            var owners = new HashSet<string>(_sites.Keys);
            if (Settlement.Selection != null)
                foreach (string owner in Settlement.Selection.ConfirmedIds) owners.Add(owner);
            foreach(string owner in owners)
                if(Settlement.Placement.TryGetPlacement(WorksiteId(owner),out _)) Settlement.Placement.TryRemove(WorksiteId(owner));
        }
        _sites.Clear();
    }

    public bool RestoreState(FirstProductionSaveData state)
    {
        var settlementState = state != null && state.started && Settlement != null && Settlement.IsReady ? Settlement.CaptureState() : null;
        if (!IsValidSave(state, settlementState)) return false;
        if (!state.started)
        {
            ClearWorksitesForRestore();
            foreach (var button in _buttons.Values) if (button != null) Destroy(button.gameObject);
            _buttons.Clear();
            if (_status != null) Destroy(_status.transform.parent.gameObject);
            _status = null;
            IsReady = false;
            _completedAt = -1;
            Objective = "동행자들의 첫 작업을 준비하세요.";
            return true;
        }
        Initialize();
        var playerCollider=Settlement.Selection.Tutorial.player.GetComponent<CharacterController>();
        bool playerWasEnabled=playerCollider.enabled;
        playerCollider.enabled=false; // Saved approach poses may occupy the reserved entrance, never the footprint.
        try
        {
        foreach(var record in state.worksites.Where(s=>!IsEmptyPlacement(s.placement)))
        {
            var p=record.placement;
            var result=Settlement.Placement.TryPlace(p.instanceId,new Vector2Int(p.anchorX,p.anchorZ),p.rotationQuarterTurns);
            if(!result.Succeeded)
            {
                Debug.LogWarning("[VS-P4] Restore placement rejected: " + p.instanceId + " / " + result.Failure);
                ClearWorksitesForRestore(); // remove any earlier placements created by this restore attempt
                return false;
            }
            Attach(record.companionId,Placed(record.companionId)).Restore(record);
        }
        return true;
        }
        finally { playerCollider.enabled=playerWasEnabled; }
    }
}
