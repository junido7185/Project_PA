using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// VS-PRESENT-001-P2: 짧은 출항 표현. 선택 세션과 기존 WorldGrid/이동 권위를 연결한다.
[DefaultExecutionOrder(220)]
public sealed class DepartureVoyagePresentation : MonoBehaviour
{
    public GameObject boatPrefab, treePrefab, birchPrefab, rockPrefab, dockPrefab, cratePrefab;
    public Material waterMaterial;
    public float voyageSeconds = 12f;
    public bool Sailing { get; private set; }
    public bool Arrived { get; private set; }
    public bool ArrivalFadeFinished { get; private set; }
    public Transform IslandRoot => _world;

    public void SkipTravelForSavedArrival() { if (Sailing) Elapsed = voyageSeconds; }
    public void SetSettlementObjective(string text)
    {
        if (_objective != null) _objective.text = text;
    }
    public float Elapsed { get; private set; }
    public WorldGridService IslandGrid { get; private set; }
    public WorldChunkTerrain IslandTerrain { get; private set; }
    public Transform Boat { get; private set; }
    public IReadOnlyList<GameObject> Companions => _companions.AsReadOnly();
    public IReadOnlyList<string> ArrivedIds => Array.AsReadOnly(_arrivedIds);
    public Vector3 ArrivalPosition => new Vector3(114f, .55f, 106f);

    DepartureCompanionSelection _selection;
    Transform _player, _world, _cameraFraming;
    CharacterController _controller;
    CameraController _cameraFollow;
    readonly List<GameObject> _companions = new List<GameObject>(2);
    string[] _arrivedIds = Array.Empty<string>();
    bool _started;
    Image _fade;
    TextMeshProUGUI _objective, _subtitle;
    Vector3 _boatStart = new Vector3(114f, 0f, 97f);
    Vector3 _boatEnd = new Vector3(114f, 0f, 101f);

    void Awake()
    {
        _selection = GetComponent<DepartureCompanionSelection>();
        _selection.DepartureConfirmed += Depart;
    }

    void OnDestroy()
    {
        if (_selection != null) _selection.DepartureConfirmed -= Depart;
    }

    void Depart(IReadOnlyList<string> ids)
    {
        if (_started || ids.Count != 2 || ids.Distinct().Count() != 2) return;
        if (boatPrefab == null || treePrefab == null || birchPrefab == null || rockPrefab == null ||
            dockPrefab == null || cratePrefab == null || waterMaterial == null)
            throw new InvalidOperationException("[VS-P2] Missing existing voyage asset reference.");
        _started = true;
        _player = _selection.Tutorial.player;
        _controller = _player.GetComponent<CharacterController>();
        _cameraFollow = Camera.main.GetComponent<CameraController>();
        BuildIsland();
        BuildBoat(ids);
        _selection.HideSelection();
        _selection.Tutorial.buyer.Pause();
        _selection.Tutorial.buyer.gameObject.SetActive(false);
        MovePlayer(Boat.position + new Vector3(0, 1.4f, -.9f));
        _player.GetComponent<PlayerController>().enabled = true;
        _cameraFraming = new GameObject("VoyageCameraFraming").transform;
        _cameraFraming.SetParent(transform, false);
        UpdateCameraFraming();
        _cameraFollow.target = _cameraFraming;
        _cameraFollow.SnapToTarget();
        Camera.main.orthographicSize = 12f;
        BuildHUD();
        Sailing = true;
        Debug.Log("[VS-P2] SAILING ids=" + string.Join(",", ids) + " duration=" + voyageSeconds);
        StartCoroutine(Voyage());
    }

    void Update() => UpdateCameraFraming();

    void UpdateCameraFraming()
    {
        if (_cameraFraming != null && _player != null)
            _cameraFraming.position = _player.position + new Vector3(0, .2f, 3.5f);
    }

    IEnumerator Voyage()
    {
        while (Elapsed < voyageSeconds)
        {
            Elapsed = Mathf.Min(voyageSeconds, Elapsed + Time.deltaTime);
            Vector3 next = Vector3.Lerp(_boatStart, _boatEnd, Elapsed / voyageSeconds);
            Vector3 delta = next - Boat.position;
            Boat.position = next;
            Physics.SyncTransforms();
            _controller.Move(delta);
            yield return null;
        }
        Sailing = false;
        _player.GetComponent<PlayerController>().enabled = false;
        yield return Fade(0f, 1f);
        MovePlayer(ArrivalPosition);
        var traversal = _player.GetComponent<WorldPlayerTraversalGuard>() ?? _player.gameObject.AddComponent<WorldPlayerTraversalGuard>();
        traversal.Configure(IslandGrid);
        if (!traversal.TryTeleportTo(ArrivalPosition)) throw new InvalidOperationException("[VS-P2] Arrival is not a dry walkable cell.");
        for (int i = 0; i < _companions.Count; i++)
        {
            Transform npc = _companions[i].transform;
            npc.SetParent(_world, true);
            npc.position = ArrivalPosition + new Vector3(i == 0 ? -1.7f : 1.7f, 0, .25f);
            npc.rotation = Quaternion.Euler(0, 180, 0);
        }
        _arrivedIds = _selection.ConfirmedIds.ToArray();
        _objective.text = "첫 거점을 세울 장소를 찾아보세요.";
        _subtitle.text = "P.A. COMPANY  /  FIRST ARRIVAL";
        UpdateCameraFraming();
        _cameraFollow.SnapToTarget();
        _player.GetComponent<PlayerController>().enabled = true;
        _player.GetComponent<PlayerInteraction>().enabled = true;
        Arrived = true;
        Debug.Log("[VS-P2] ARRIVED same companions=" + string.Join(",", _arrivedIds));
        yield return Fade(1f, 0f);
        ArrivalFadeFinished = true;
    }

    IEnumerator Fade(float from, float to)
    {
        for (float t = 0; t < .6f; t += Time.unscaledDeltaTime)
        {
            _fade.color = new Color(.12f, .2f, .22f, Mathf.Lerp(from, to, t / .6f));
            yield return null;
        }
        _fade.color = new Color(.12f, .2f, .22f, to);
    }

    void BuildIsland()
    {
        if (FindFirstObjectByType<WorldGridService>() != null)
            throw new InvalidOperationException("[VS-P2] Development departure requires no pre-existing world grid.");
        _world = new GameObject("PA_FirstArrival_Island").transform;
        _world.SetParent(transform, false);
        var gridObject = new GameObject("OpeningIsland_ExistingWorldGrid");
        gridObject.transform.SetParent(_world, false);
        IslandGrid = gridObject.AddComponent<WorldGridService>();
        var definition = new WorldGridDefinition(2f, 16, 16, 16, .25f, 0, 6, new Vector3(100, 0, 100));
        var cells = new WorldCellData[256];
        for (int z = 0; z < 16; z++)
        for (int x = 0; x < 16; x++)
        {
            float distance = new Vector2((x - 7.5f) / 6.5f, (z - 8f) / 6.4f).magnitude;
            bool water = distance > 1f;
            int elevation = water ? 0 : distance > .76f ? 2 : 3;
            if (!water && distance < .72f && z >= 9) elevation = Mathf.Min(6, 3 + (z - 8));
            WorldGroundType ground = water || distance > .76f ? WorldGroundType.Sand : WorldGroundType.Grass;
            cells[z * 16 + x] = new WorldCellData(new Vector2Int(x, z), elevation, ground,
                WorldPathType.None, water ? 1 : elevation, water ? 1 : 0, WorldCellOccupancy.Empty);
        }
        // WORLD-007 검증된 초기 스냅샷 경계에 작은 authored island cell 데이터를 전달한다.
        if (!IslandGrid.TryRestoreSnapshot(definition, cells)) throw new InvalidOperationException("[VS-P2] Island cell initialization rejected.");
        IslandTerrain = gridObject.AddComponent<WorldChunkTerrain>();
        if (!IslandTerrain.IsReady) throw new InvalidOperationException("[VS-P2] Existing terrain mesh/collider not ready.");
        var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
        sea.name = "Ocean_ExistingPalette";
        sea.transform.SetParent(_world, false);
        sea.transform.position = new Vector3(115, .23f, 110);
        sea.transform.localScale = Vector3.one * 16;
        sea.GetComponent<Collider>().enabled = false;
        sea.GetComponent<Renderer>().sharedMaterial = IslandTerrain.GetComponent<Renderer>().sharedMaterials[WorldSurfaceMaterialSlots.Water];

        var trees = new[] { new Vector2Int(3,6), new Vector2Int(4,8), new Vector2Int(3,10),
            new Vector2Int(5,11), new Vector2Int(7,12), new Vector2Int(9,11), new Vector2Int(11,9),
            new Vector2Int(12,7), new Vector2Int(10,6), new Vector2Int(5,7), new Vector2Int(9,9), new Vector2Int(7,10) };
        for (int i = 0; i < trees.Length; i++)
        {
            IslandGrid.CellToWorld(trees[i], out Vector3 point);
            GameObject tree = Place(i % 3 == 0 ? birchPrefab : treePrefab, "TimberResource_" + i, _world, point, 1f);
            Renderer[] renderers = tree.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            tree.transform.localScale = Vector3.one * ((4.2f + i % 3 * .4f) / Mathf.Max(.1f, bounds.size.y));
            var trunk = tree.AddComponent<CapsuleCollider>();
            trunk.radius = .10f; trunk.height = .8f; trunk.center = Vector3.up * .4f;
        }
        foreach (Vector2Int cell in new[] { new Vector2Int(4,4), new Vector2Int(10,5), new Vector2Int(11,10), new Vector2Int(6,12) })
        {
            IslandGrid.CellToWorld(cell, out Vector3 point);
            var rock = Place(rockPrefab, "StoneResource_" + cell, _world, point, 2f);
            rock.AddComponent<SphereCollider>().radius = .32f;
        }
        Place(dockPrefab, "TemporaryDock", _world, new Vector3(114, .25f, 103), 3f);
        Box("DockWalkSurface", _world, new Vector3(114, .42f, 103), new Vector3(1.95f, .16f, 4.5f));
        Place(cratePrefab, "PA_SupplyCrate", _world, new Vector3(117.4f, .5f, 105.8f), 2.2f);
    }

    void BuildBoat(IReadOnlyList<string> ids)
    {
        Boat = new GameObject("PA_DepartureBoat_Presentation").transform;
        Boat.SetParent(transform, false);
        Boat.position = _boatStart;
        Place(boatPrefab, "ExistingDepartureBoat", Boat, _boatStart, 1.8f);
        Box("DeckCollision", Boat, new Vector3(0, 1.28f, 0), new Vector3(2.7f, .16f, 4.14f), true);
        Box("PortRail", Boat, new Vector3(-1.4f, 2, 0), new Vector3(.15f, 2, 4.4f), true);
        Box("StarboardRail", Boat, new Vector3(1.4f, 2, 0), new Vector3(.15f, 2, 4.4f), true);
        Box("BowRail", Boat, new Vector3(0, 2, 2.15f), new Vector3(3, 2, .15f), true);
        Box("SternRail", Boat, new Vector3(0, 2, -2.15f), new Vector3(3, 2, .15f), true);
        for (int i = 0; i < ids.Count; i++)
        {
            var c = _selection.candidates.First(candidate => candidate.id == ids[i]);
            var npc = new GameObject("Companion_" + c.id);
            npc.transform.SetParent(Boat, false);
            npc.transform.localPosition = new Vector3(i == 0 ? -.78f : .78f, 1.38f, .9f);
            Instantiate(c.model, npc.transform).name = "CharacterVisual";
            npc.AddComponent<NpcPresentationNormalizer>();
            npc.transform.localRotation = Quaternion.Euler(0, 180, 0);
            _companions.Add(npc);
        }
    }

    static GameObject Place(GameObject prefab, string name, Transform parent, Vector3 position, float scale)
    {
        var go = Instantiate(prefab, parent);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    static void Box(string name, Transform parent, Vector3 position, Vector3 size, bool local = false)
    {
        var go = new GameObject(name, typeof(BoxCollider));
        go.transform.SetParent(parent, false);
        if (local) go.transform.localPosition = position; else go.transform.position = position;
        go.GetComponent<BoxCollider>().size = size;
    }

    void MovePlayer(Vector3 position)
    {
        _controller.enabled = false;
        _player.position = position;
        _player.rotation = Quaternion.identity;
        _controller.enabled = true;
        Physics.SyncTransforms();
    }

    void BuildHUD()
    {
        var root = new GameObject("PA_VoyageHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var paper = new Color(.97f, .94f, .85f, .97f);
        var ink = new Color(.17f, .23f, .23f);
        var header = DepartureCompanionSelection.Panel(root.transform, "Objective", 32, 28, 715, 117, paper);
        _subtitle = _selection.Label(header, "Stage", "P.A. COMPANY  /  DEPARTURE", 22, 12, 665, 26, 19, ink);
        _objective = _selection.Label(header, "Objective", "선택한 동료들과 섬으로 향하고 있습니다.", 22, 50, 670, 46, 27, ink);
        var footer = DepartureCompanionSelection.Panel(root.transform, "Companions", 32, 965, 930, 81, paper);
        string names = string.Join("  +  ", _selection.ConfirmedIds.Select(id => _selection.candidates.First(c => c.id == id).profession));
        _selection.Label(footer, "Party", "함께 온 생산자  ·  " + names + "     /     W A S D 이동", 22, 14, 890, 51, 25, ink);
        _fade = DepartureCompanionSelection.Panel(root.transform, "ArrivalFade", 0, 0, 1920, 1080, Color.clear).GetComponent<Image>();
    }
}
