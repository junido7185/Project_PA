using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// P3: 목표/UI/동행 거처 연결만 담당. 배치와 파일 쓰기는 기존 서비스에 위임한다.
public sealed class FirstIslandSettlementController : MonoBehaviour
{
    public const string HubId = "pa-settlement-hub";
    public const string ResourceRoot = "DepartureTutorial/Settlement/";
    public static readonly Vector2Int[] Footprint = { Vector2Int.zero, Vector2Int.right, Vector2Int.up, Vector2Int.one };
    public bool IsReady { get; private set; }
    public bool SettlementCompleted => IsReady && BuildingIds.All(id => Placement.TryGetPlacement(id, out _));
    public bool P4Unlocked => SettlementCompleted;
    public bool BuildMode => Placement != null && Placement.HasPreview;
    public WorldBuildingPlacementService Placement { get; private set; }
    public string[] BuildingIds => new[] { HubId, ShelterId(Selection.ConfirmedIds[0]), ShelterId(Selection.ConfirmedIds[1]) };
    public static string ShelterId(string npc) => "pa-shelter-" + npc;
    public DepartureCompanionSelection Selection { get; private set; }
    public DepartureVoyagePresentation Voyage { get; private set; }
    public string Objective { get; private set; }
    public Button ConfirmButton { get; private set; }
    public Button[] BuildingButtons { get; private set; }
    public string SaveFeedback { get; private set; } = "F5 저장  /  F9 불러오기";
    float _revealedAt, _completedAt = -1, _cameraSize;
    int _turn;
    string _previewId;
    Vector2Int _anchor;
    Vector2 _lastMouse;
    TextMeshProUGUI _hint, _saveStatus;
    Canvas _canvas;
    Button _resumeButton;
    bool _restoring, _saving;
    NavMeshSurface _navigation;
    readonly Dictionary<string, NavMeshAgent> _agents = new Dictionary<string, NavMeshAgent>();

    void Awake()
    {
        Selection = GetComponent<DepartureCompanionSelection>();
        Voyage = GetComponent<DepartureVoyagePresentation>();
    }

    void Update()
    {
        if (Selection.IsOpen && !Selection.IsConfirmed && _resumeButton == null) BuildResumeButton();
        if (!IsReady && Voyage.ArrivalFadeFinished) Initialize();
        if (!IsReady) return;
        if (_resumeButton != null) _resumeButton.gameObject.SetActive(false);
        bool complete = SettlementCompleted;
        if (complete && _completedAt < 0) _completedAt = Time.unscaledTime;
        Objective = Time.unscaledTime < _revealedAt + 2.5f ? "섬에 도착했습니다." :
            !Placement.TryGetPlacement(HubId, out _) ? "첫 거점을 설치할 장소를 정하세요." :
            !complete ? $"동행자의 임시 거처를 준비하세요. ({ShelterCount()}/2)" :
            Time.unscaledTime < _completedAt + 2.5f ? "첫 정착이 완료되었습니다." : "오늘 밤 첫 영업을 준비하세요.";
        Voyage.SetSettlementObjective(Objective);
        _saveStatus.text = SaveFeedback;
        var keyboard = Keyboard.current;
        if (keyboard != null && !_restoring)
        {
            if (keyboard.bKey.wasPressedThisFrame) BeginNext();
            if (keyboard.escapeKey.wasPressedThisFrame) Cancel();
            if (BuildMode && keyboard.rKey.wasPressedThisFrame) Rotate();
            if (BuildMode && keyboard.enterKey.wasPressedThisFrame) Commit();
        }
        if (BuildMode && Mouse.current != null)
        {
            Vector2 mouse = Mouse.current.position.ReadValue();
            if (mouse != _lastMouse && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            {
                _lastMouse = mouse;
                Ray ray = Camera.main.ScreenPointToRay(mouse);
                if (Voyage.IslandTerrain.TerrainCollider.Raycast(ray, out RaycastHit hit, 250f) && Voyage.IslandGrid.WorldToCell(hit.point, out Vector2Int cell))
                    PreviewAt(cell, _turn);
            }
            if (Mouse.current.leftButton.wasPressedThisFrame && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) Commit();
        }
        for (int i = 0; i < BuildingButtons.Length; i++)
            BuildingButtons[i].interactable = !_restoring && (i == 0 || Placement.TryGetPlacement(HubId, out _));
    }

    void Initialize()
    {
        Placement = Voyage.IslandGrid.GetComponent<WorldBuildingPlacementService>() ?? Voyage.IslandGrid.gameObject.AddComponent<WorldBuildingPlacementService>();
        foreach (string id in BuildingIds) Placement.RegisterDefinition(id, Definition(id));
        // P2가 만든 소유 범위만 등록한다. 멀리 있는 튜토리얼 장식은 스캔하지 않는다.
        foreach (Collider col in Voyage.IslandRoot.GetComponentsInChildren<Collider>())
            if (col != Voyage.IslandTerrain.TerrainCollider && col.enabled &&
                !Voyage.Companions.Any(npc => col.transform.IsChildOf(npc.transform))) Placement.RegisterObstacle(col);
        Placement.RegisterObstacle(Selection.Tutorial.player.GetComponent<CharacterController>());
        var crate = Voyage.IslandRoot.Find("PA_SupplyCrate");
        if (crate != null)
        {
            var box = crate.gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(.6f,.6f,.6f); box.center = Vector3.up*.3f;
            Placement.RegisterObstacle(box);
        }
        _cameraSize = Camera.main.orthographicSize;
        _revealedAt = Time.unscaledTime;
        EnsureSaveManager();
        BuildUI();
        IsReady = true;
        Debug.Log("[VS-P3] READY companions=" + string.Join(",", Selection.ConfirmedIds));
    }

    public WorldBuildingPlacementDefinition Definition(string id) => new WorldBuildingPlacementDefinition(
        id == HubId ? "PA_SETTLEMENT_HUB_T0" : "PA_STARTER_SHELTER",
        ResourceRoot + (id == HubId ? "Hub" : "Shelter"), Footprint, new Vector2Int(0, -1));

    public SaveManager EnsureSaveManager()
    {
        if (SaveManager.instance != null) return SaveManager.instance;
        var go = new GameObject("DepartureSettlement_ExistingSaveManager");
        return go.AddComponent<SaveManager>();
    }

    int ShelterCount() => Selection.ConfirmedIds.Count(id => Placement.TryGetPlacement(ShelterId(id), out _));
    public void BeginNext()
    {
        if (!IsReady) return;
        string id = BuildingIds.FirstOrDefault(value => !Placement.TryGetPlacement(value, out _));
        if (id != null) Begin(id);
    }

    public void Begin(string id)
    {
        if (!IsReady || _restoring || !BuildingIds.Contains(id) || id != HubId && !Placement.TryGetPlacement(HubId, out _)) return;
        Cancel(); _previewId = id; _turn = 0;
        if (Placement.TryGetPlacement(id, out var placed))
        {
            _turn = placed.QuarterTurns; _anchor = placed.Anchor;
            Placement.BeginMovePreview(id, _anchor, _turn);
        }
        else
        {
            Voyage.IslandGrid.WorldToCell(Selection.Tutorial.player.position + Vector3.forward * 4, out _anchor);
            Placement.BeginPlacementPreview(id, _anchor, _turn);
        }
        Selection.Tutorial.player.GetComponent<PlayerController>().enabled = false;
        Selection.Tutorial.player.GetComponent<PlayerInteraction>().enabled = false;
        Camera.main.orthographicSize = _cameraSize + 2;
        _lastMouse = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        PreviewAt(_anchor, _turn);
    }

    public WorldBuildingPlacementResult PreviewAt(Vector2Int cell, int turns)
    {
        _anchor = cell; _turn = WorldBuildingPlacementDefinition.NormalizeQuarterTurns(turns);
        var result = Placement.UpdatePreview(_anchor, _turn);
        ConfirmButton.interactable = result.Succeeded;
        _hint.text = result.Succeeded ? "설치 가능  ·  클릭 / Enter 확정  ·  R 회전" : "설치 불가  ·  " + Reason(result.Failure);
        return result;
    }

    public void Rotate() { if (BuildMode) PreviewAt(_anchor, _turn + 1); }
    public bool Commit()
    {
        if (!BuildMode || _restoring) return false;
        string id = _previewId;
        var result = Placement.CommitPreview();
        if (!result.Succeeded) { _hint.text = "설치 불가  ·  " + Reason(result.Failure); return false; }
        Cancel();
        BindShelters();
        Debug.Log("[VS-P3] PLACED " + id + " " + result.Anchor + " rotation=" + result.QuarterTurns);
        return true;
    }

    public void Cancel()
    {
        if (Placement == null) return;
        Placement.CancelPreview();
        Selection.Tutorial.player.GetComponent<PlayerController>().enabled = true;
        Selection.Tutorial.player.GetComponent<PlayerInteraction>().enabled = true;
        Camera.main.orthographicSize = _cameraSize;
        if (ConfirmButton != null) ConfirmButton.interactable = false;
        if (_hint != null) _hint.text = "B 다음 설치  ·  건물을 다시 선택하면 이동  ·  첫 3개 무료";
    }

    static string Reason(WorldBuildingPlacementFailure failure) => failure switch
    {
        WorldBuildingPlacementFailure.PhysicalObstacle => "나무·바위·부두·사람과 겹칩니다.",
        WorldBuildingPlacementFailure.OccupiedCell => "이미 건물이 있는 칸입니다.",
        WorldBuildingPlacementFailure.UnevenFootprint => "평탄한 곳을 찾아주세요.",
        WorldBuildingPlacementFailure.EntranceBlocked => "입구 앞 한 칸을 비워주세요.",
        _ => "섬 안의 비어 있는 평지를 선택하세요."
    };

    void BindShelters()
    {
        // 이미 사용하는 NavMeshAgent 이동만 연결한다. 고용/생산/FSM을 새로 만들지 않는다.
        if (_navigation == null)
        {
            _navigation = Voyage.IslandGrid.gameObject.AddComponent<NavMeshSurface>();
            _navigation.collectObjects = CollectObjects.Children;
            _navigation.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        }
        _navigation.BuildNavMesh();
        for (int i = 0; i < Selection.ConfirmedIds.Count; i++)
        {
            string npcId = Selection.ConfirmedIds[i];
            if (!Placement.TryGetPlacement(ShelterId(npcId), out var shelter)) continue;
            var npc = Voyage.Companions[i];
            if (!_agents.TryGetValue(npcId, out var agent))
            {
                agent = npc.GetComponent<NavMeshAgent>();
                if (agent == null) agent = npc.AddComponent<NavMeshAgent>();
                agent.speed = 2.2f; agent.radius = .3f; agent.height = 1.8f; agent.stoppingDistance = .4f;
                _agents[npcId] = agent;
            }
            Voyage.IslandGrid.CellToWorld(shelter.Entrance, out Vector3 destination);
            if (NavMesh.SamplePosition(npc.transform.position, out NavMeshHit start, 4, NavMesh.AllAreas)) agent.Warp(start.position);
            if (agent.isOnNavMesh) agent.SetDestination(destination);
            var marker = shelter.GameObject.transform.Find("CompanionRole");
            if (marker == null)
            {
                marker = new GameObject("CompanionRole").transform;
                marker.SetParent(shelter.GameObject.transform, false);
                marker.localPosition = new Vector3(0, 2.7f, 0);
                var label = marker.gameObject.AddComponent<PrototypeWorldLabel>();
                label.Set(Selection.candidates.First(c => c.id == npcId).profession + "의 거처", new Color(.18f,.27f,.27f), 2.1f);
                // 공통 shelter의 작은 직업 색상만 변경한다.
                var accent = shelter.GameObject.transform.Find("ProfessionAccent");
                if (accent != null) accent.GetComponent<Renderer>().material.color = Selection.candidates.First(c => c.id == npcId).accent;
            }
        }
    }

    public FirstSettlementSaveData CaptureState()
    {
        var state = new FirstSettlementSaveData { companionIds = Selection.ConfirmedIds.ToArray(), settlementCompleted = SettlementCompleted };
        foreach (string id in BuildingIds)
            if (Placement.TryGetPlacement(id, out var p)) state.buildings.Add(new WorldPlacedBuildingSaveData {
                instanceId = id, buildingId = p.Definition.StableId, anchorX = p.Anchor.x, anchorZ = p.Anchor.y, rotationQuarterTurns = p.QuarterTurns });
        return state;
    }

    public async Task<bool> PrepareRestoreAsync(FirstSettlementSaveData state)
    {
        if (state == null || state.version != 1 || state.companionIds == null || state.companionIds.Length != 2 ||
            state.companionIds.Distinct().Count() != 2 || state.companionIds.Any(id => !Selection.candidates.Any(c => c.id == id)) ||
            state.buildings == null || state.buildings.Count > 3 || state.buildings.Any(b => b == null)) return false;
        var validIds = new[] { HubId, ShelterId(state.companionIds[0]), ShelterId(state.companionIds[1]) };
        if (state.buildings.Select(b => b.instanceId).Distinct().Count() != state.buildings.Count ||
            state.buildings.Any(b => !validIds.Contains(b.instanceId) || b.rotationQuarterTurns < 0 || b.rotationQuarterTurns > 3 ||
                b.anchorX < 0 || b.anchorX >= 16 || b.anchorZ < 0 || b.anchorZ >= 16 || b.buildingId != Definition(b.instanceId).StableId) ||
            state.settlementCompleted != (state.buildings.Count == 3) || state.buildings.Count > 0 && !state.buildings.Any(b => b.instanceId == HubId)) return false;
        if (!Selection.RestoreConfirmedSelection(state.companionIds)) return false;
        Voyage.SkipTravelForSavedArrival();
        float started = Time.realtimeSinceStartup;
        while (!IsReady && Time.realtimeSinceStartup - started < 15f) await Task.Delay(50);
        return IsReady;
    }

    public void RestoreState(FirstSettlementSaveData state)
    {
        Cancel(); _restoring = true;
        var previous = CaptureState();
        try
        {
            ApplyBuildings(state);
            _completedAt = state.settlementCompleted ? Time.unscaledTime - 3 : -1;
            BindShelters();
            SaveFeedback = "정착 위치와 동행자 연결을 복원했습니다.";
        }
        catch
        {
            ApplyBuildings(previous);
            BindShelters();
            throw;
        }
        finally { _restoring = false; }
    }

    void ApplyBuildings(FirstSettlementSaveData state)
    {
        foreach (string id in BuildingIds) if (Placement.TryGetPlacement(id, out _)) Placement.TryRemove(id);
        foreach (var record in state.buildings.OrderBy(b => b.instanceId == HubId ? 0 : 1))
        {
            var result = Placement.TryPlace(record.instanceId, new Vector2Int(record.anchorX, record.anchorZ), record.rotationQuarterTurns);
            if (!result.Succeeded) throw new InvalidOperationException("Settlement restore rejected: " + result.Failure);
        }
    }

    async void Save()
    {
        if (_saving) return;
        _saving = true;
        try { await EnsureSaveManager().SaveGameAsync(); SaveFeedback = "첫 정착을 저장했습니다. 다음 진입에서 이어갈 수 있습니다."; }
        catch (Exception e) { Debug.LogException(e); SaveFeedback = "저장 실패 · 기존 파일을 확인하세요."; }
        finally { _saving = false; }
    }

    public async Task ResumeAsync() => await EnsureSaveManager().LoadGameAsync();

    void BuildResumeButton()
    {
        var ui = Selection.transform.Find("PA_CompanionSelection/Paper");
        if (ui == null) return;
        _resumeButton = Button(ui, "ResumeSettlement", "저장한 정착 이어하기", 1390, 34, 460, 54,
            async () => { try { await ResumeAsync(); } catch (Exception e) { Debug.LogException(e); } });
    }

    void BuildUI()
    {
        var root = new GameObject("PA_SettlementUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        _canvas = root.GetComponent<Canvas>(); _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 112;
        var scaler = root.GetComponent<CanvasScaler>();scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution = new Vector2(1920,1080);
        BuildingButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            string id = BuildingIds[i];
            string text = i == 0 ? "관리 거점 Tier 0" : Selection.candidates.First(c => c.id == Selection.ConfirmedIds[i-1]).profession + "의 거처";
            BuildingButtons[i] = Button(root.transform, id, text, 1480, 190+i*76, 405, 64, () => Begin(id));
        }
        ConfirmButton = Button(root.transform,"Confirm","설치 / 이동 확정",1480,440,405,64,()=>Commit());ConfirmButton.interactable=false;
        Button(root.transform,"Rotate","R  ·  90° 회전",1480,516,195,55,Rotate);
        Button(root.transform,"Cancel","취소",1690,516,195,55,Cancel);
        Button(root.transform,"Save","정착 저장",1480,600,405,60,Save);
        var panel = DepartureCompanionSelection.Panel(root.transform,"Hint",30,840,1360,108,new Color(.97f,.94f,.85f,.97f));
        _hint=Selection.Label(panel,"Hint","B 다음 설치  ·  첫 거점과 거처 2개 무료",20,8,1320,44,24,new Color(.17f,.23f,.23f));
        _saveStatus=Selection.Label(panel,"SaveFeedback",SaveFeedback,20,56,1320,35,21,new Color(.22f,.45f,.44f));
    }

    Button Button(Transform parent,string name,string text,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
    {
        var rect=DepartureCompanionSelection.Panel(parent,name,x,y,w,h,new Color(.22f,.45f,.44f));
        var image=rect.GetComponent<Image>();image.raycastTarget=true;
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);
        var label=Selection.Label(rect,"Label",text,10,4,w-20,h-8,24,new Color(.97f,.94f,.85f));label.alignment=TextAlignmentOptions.Center;
        return button;
    }
}
