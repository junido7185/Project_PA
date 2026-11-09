using System;
using System.Linq;
using UnityEngine;

public enum WorksiteActivityKind { Gathering, Mining, Farming }

// Docs/02_IMPLEMENTATION/PROJECT_PA_CODE_REUSE_MAP_v1.md §2.2–2.3:
// 작은 직렬화 프로필. 기존 continuation 설정에서 보관하며 역할별 런타임 분기를 대신한다.
[Serializable]
public sealed class StarterWorksiteProfile
{
    public string companionId;
    public string profileId;
    public ProductionData productionData;
    public NpcSpecialty specialty;
    public string buildingId;
    public string buildingResourcePath;
    public Vector2Int[] footprint = { Vector2Int.zero };
    public Vector2Int entranceOffset = Vector2Int.down;
    public WorksiteActivityKind activityKind;
    public string activityIdPrefix;
    public string activityLabel;
    public string activityPrompt;
    public string activityItemResourcePath;
    public int activityAmount = 1;
    public float growthSecondsPerStage = 3;
    public Item starterSeed;
    public ToolType gatheringTool = ToolType.None;
    public GameObject toolPrefab;

    public WorldBuildingPlacementDefinition Definition => new WorldBuildingPlacementDefinition(
        buildingId, buildingResourcePath, footprint, entranceOffset);

    public bool IsValid => !string.IsNullOrWhiteSpace(companionId) && !string.IsNullOrWhiteSpace(profileId) &&
        productionData != null && productionData.producedItem != null && Definition.IsValid() &&
        Enum.IsDefined(typeof(WorksiteActivityKind), activityKind) && !string.IsNullOrWhiteSpace(activityPrompt) &&
        (activityKind != WorksiteActivityKind.Mining ||
            (!string.IsNullOrWhiteSpace(activityItemResourcePath) && activityAmount > 0)) &&
        (activityKind != WorksiteActivityKind.Farming || (starterSeed != null && growthSecondsPerStage > 0));

    public static StarterWorksiteProfile Find(StarterWorksiteProfile[] profiles, string companionId) =>
        profiles?.SingleOrDefault(p => p != null && p.companionId == companionId);
}

// Docs/02_IMPLEMENTATION/PROJECT_PA_CODE_REUSE_MAP_v1.md §6.2:
// 선택된 기존 주민 객체에 붙는 배정 어댑터. 배치·생산·조달 권위는 기존 서비스에 남긴다.
[DisallowMultipleComponent]
public sealed class WorksiteBinding : MonoBehaviour
{
    public string WorksiteInstanceId { get; private set; }
    public string CompanionId { get; private set; }
    public ProducerNpcController Producer { get; private set; }
    public StarterWorksiteProfile Profile { get; private set; }
    public Transform WorkAnchor { get; private set; }
    public Transform ProcurementAnchor { get; private set; }
    public Transform HomeAnchor { get; private set; }
    public bool Assigned => Producer != null && WorkAnchor != null && (_demoActivity || _placed != null && _placed.GameObject != null);
    bool _demoActivity;
    public bool AssignmentEnabled => Assigned && Producer.isActiveAndEnabled;

    WorldPlacedBuildingRuntime _placed;
    WorldGridService _grid;

    public void Bind(string companionId, NpcProfile residentProfile, StarterWorksiteProfile profile,
        WorldPlacedBuildingRuntime placed, WorldGridService grid, Transform homeAnchor = null)
    {
        if (profile == null || !profile.IsValid || profile.companionId != companionId || residentProfile == null ||
            placed == null || placed.GameObject == null || placed.Definition.StableId != profile.buildingId ||
            string.IsNullOrWhiteSpace(placed.InstanceId) || grid == null ||
            !grid.CellToWorld(placed.Entrance, out var entrance))
            throw new InvalidOperationException("Invalid worksite assignment: " + companionId);
        if (Assigned)
        {
            if (CompanionId != companionId || Profile != profile || _placed != placed)
                throw new InvalidOperationException("Unassign the current worksite before replacing it.");
            RefreshAnchors();
            return;
        }

        Producer = EnsureProducer();
        CompanionId = companionId;
        Profile = profile;
        WorksiteInstanceId = placed.InstanceId;
        _placed = placed;
        _grid = grid;
        HomeAnchor = homeAnchor;
        WorkAnchor = new GameObject("WorkPosition_" + placed.InstanceId).transform;
        WorkAnchor.SetParent(placed.GameObject.transform, false);
        WorkAnchor.position = entrance;
        ProcurementAnchor = WorkAnchor;
        Producer.profile = residentProfile;
        Producer.productionData = profile.productionData;
        Producer.specialty = profile.specialty;
        Producer.workSpot = WorkAnchor;
        Producer.dropOffPoint = ProcurementAnchor;
        Producer.arriveDistance = .5f;
    }

    // Existing generated activity binding for the opening demo; no building or save record is invented.
    public void BindDemoActivity(string companionId, NpcProfile resident, ProductionData data,
        NpcSpecialty specialty, Transform activity, Transform home)
    {
        if (Assigned || resident == null || data == null || data.producedItem == null || activity == null)
            throw new InvalidOperationException("Invalid demo activity assignment.");
        Producer = EnsureProducer(); CompanionId = companionId; WorksiteInstanceId = "demo-activity-" + companionId;
        _demoActivity = true; WorkAnchor = activity; ProcurementAnchor = home != null ? home : activity; HomeAnchor = home;
        Producer.profile = resident; Producer.productionData = data; Producer.specialty = specialty;
        Producer.workSpot = WorkAnchor; Producer.dropOffPoint = ProcurementAnchor;
    }

    ProducerNpcController EnsureProducer()
    {
        // DEPRECATED compatibility fallback — P4-R3 명시 승인.
        // 현재 P2는 동행자 객체, P3는 NavMeshAgent를 먼저 만든다. 여기서만 생산자를 보충한다.
        // 향후 P2/P3 bootstrap이 생산자 capability를 제공하면 AddComponent 분기를 제거한다.
        // 기존 생산자의 Awake/FSM/재고를 다시 만들거나 복제하지 않는다.
        var existing = GetComponent<ProducerNpcController>();
        return existing != null ? existing : gameObject.AddComponent<ProducerNpcController>();
    }

    public void RefreshAnchors()
    {
        if (Assigned && !_demoActivity && _grid.CellToWorld(_placed.Entrance, out var entrance)) WorkAnchor.position = entrance;
    }

    public void Unassign()
    {
        if (Producer != null)
        {
            Producer.Pause();
            if (Producer.workSpot == WorkAnchor) Producer.workSpot = null;
            if (Producer.dropOffPoint == ProcurementAnchor) Producer.dropOffPoint = null;
        }
        // 앵커 객체의 수명은 기존 배치 객체가 소유한다. 재고·지원 수령 자격은 건드리지 않는다.
        WorkAnchor = ProcurementAnchor = HomeAnchor = null;
        WorksiteInstanceId = null;
        _demoActivity = false;
        _placed = null;
        _grid = null;
    }
}
