using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 파기 자국(Docs/01_GAME_DESIGN/Content/DIGGING.md). 레퍼런스 09(삽을 든 캐릭터와 땅 대상 표시)에서 가져온 '땅속 발견물' 고리.
// 날마다 정착지 둘레 풀밭에 흙이 들썩이는 자국이 생기고, 삽을 들고 [E]로 파면 점토·옛 동전·화석 조각이 나온다.
// 보상은 기존 바닥 드롭(GatherFeedback.SpawnDrop) → 빈손 [E] 줍기. 판 자리는 저장된다(SaveData.digSpotsDay/digSpotsDug).
public sealed class DigSpots : MonoBehaviour
{
    public static DigSpots Instance { get; private set; }
    public static bool Enabled = true;

    const int DailyCount = 6;
    const float MinSpacing = 9f, Reach = 70f;
    static readonly (string resource, int weight, int amount)[] Loot =
    {
        ("Items/Item_Clay", 55, 2),
        ("Items/Item_OldCoin", 27, 1),
        ("Items/Item_FossilShard", 18, 1),
    };

    public int Day { get; private set; } = -1;
    public int CandidateCells { get; private set; }
    public IReadOnlyList<DigSpot> Spots => _spots;
    public int DugToday => _dug.Count;

    WorldGridService _grid;
    float _nextCheck;
    int _keepFor = -1;
    readonly List<DigSpot> _spots = new List<DigSpot>();
    readonly HashSet<int> _dug = new HashSet<int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterBootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Enabled || scene.name != DemoRouteController.WorldScene) return;
        if (Instance != null && Instance.gameObject.scene == scene) return;
        var host = new GameObject("PA_DigSpots");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<DigSpots>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + 1f;
        if (_grid == null) _grid = FindFirstObjectByType<WorldGridService>();
        if (_grid == null || _grid.Definition == null || Inventory.instance == null) return;
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        if (day != Day) Refresh(day);
    }

    public List<int> DugSlots() => new List<int>(_dug);

    public void RestoreDug(int day, List<int> dug)
    {
        int today = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        _dug.Clear();
        if (day == today && dug != null) foreach (int slot in dug) _dug.Add(slot);
        Day = -1; _keepFor = today; _nextCheck = 0f;
        if (_grid != null && _grid.Definition != null && Inventory.instance != null) Refresh(today);
    }

    internal void MarkDug(int slot) => _dug.Add(slot);

    void Refresh(int day)
    {
        if (day != Day && day != _keepFor) _dug.Clear();
        _keepFor = -1;
        Day = day;
        foreach (var spot in _spots) if (spot != null) Destroy(spot.gameObject);
        _spots.Clear();
        var mound = Resources.Load<GameObject>("Dig/DigMound");
        var hole = Resources.Load<GameObject>("Dig/DigHole");
        if (mound == null || hole == null) { Debug.LogWarning("[DigSpots] Resources/Dig 모델이 없습니다(PA_DigSetup 실행 필요)."); return; }
        // 기준점: 운영 중인 상점, 없으면 도착 항구(둘 다 저장·이어하기 사이에 같은 자리라 판 자리 번호가 그대로 맞는다).
        var shop = DemoSettlementController.Instance != null ? DemoSettlementController.Instance.OperatingShop : null;
        var world = FirstDayWorldPresentation.Instance;
        Vector3 anchor = shop != null ? shop.transform.position : world != null && world.Harbor != Vector3.zero ? world.Harbor : Inventory.instance.transform.position;
        var cells = Candidates(anchor);
        CandidateCells = cells.Count;
        if (cells.Count == 0) { Debug.LogWarning("[DigSpots] 풀밭 후보 칸이 없습니다."); return; }
        var random = new System.Random(day * 6151 + 977);
        var chosen = new List<Vector3>();
        int total = 0; foreach (var kind in Loot) total += kind.weight;
        for (int attempt = 0; attempt < 400 && chosen.Count < DailyCount; attempt++)
        {
            var cell = cells[random.Next(cells.Count)];
            if (!_grid.CellToWorld(cell, out var ground)) continue;
            if (chosen.Exists(p => (p - ground).sqrMagnitude < MinSpacing * MinSpacing)) continue;
            int roll = random.Next(total), index = 0;
            while (roll >= Loot[index].weight) { roll -= Loot[index].weight; index++; }
            float yaw = (float)random.NextDouble() * 360f;
            int slot = chosen.Count;
            chosen.Add(ground);
            if (_dug.Contains(slot)) continue;
            var item = Resources.Load<Item>(Loot[index].resource);
            if (item == null) continue;
            if (Physics.Raycast(ground + Vector3.up * 3f, Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore)) ground.y = hit.point.y;
            var root = new GameObject($"DigSpot_{slot}");
            root.transform.SetParent(transform, false);
            root.transform.SetPositionAndRotation(ground, Quaternion.Euler(0, yaw, 0));
            var collider = root.AddComponent<SphereCollider>();
            collider.isTrigger = true; collider.radius = .45f; collider.center = Vector3.up * .15f;
            var spot = root.AddComponent<DigSpot>();
            var moundObject = Instantiate(mound, root.transform);
            spot.Setup(this, slot, item, Loot[index].amount, moundObject, Instantiate(hole, root.transform));
            FindSparkle.Attach(moundObject, slot + day * 11); // 파면 둔덕과 함께 꺼진다
            _spots.Add(spot);
        }
        Debug.Log($"[DigSpots] day={day} placed={_spots.Count} candidates={cells.Count} dugEarlier={_dug.Count}");
    }

    // 기준점 Reach 안의 걸을 수 있는 풀·흙 칸(길·건물·모래·물가 2칸 제외).
    List<Vector2Int> Candidates(Vector3 anchor)
    {
        var result = new List<Vector2Int>();
        var definition = _grid.Definition;
        if (!_grid.WorldToCell(anchor, out var center)) center = new Vector2Int(definition.Width / 2, definition.Height / 2);
        int r = Mathf.CeilToInt(Reach / definition.CellSize);
        for (int dz = -r; dz <= r; dz++)
            for (int dx = -r; dx <= r; dx++)
            {
                if ((dx * dx + dz * dz) * definition.CellSize * definition.CellSize > Reach * Reach) continue;
                var c = center + new Vector2Int(dx, dz);
                if (!_grid.TryGetCell(c, out var cell) || !cell.IsWalkable || cell.HasPath) continue;
                if (cell.GroundType != WorldGroundType.Grass && cell.GroundType != WorldGroundType.Soil) continue;
                if ((c - center).sqrMagnitude * definition.CellSize * definition.CellSize < 8f * 8f) continue; // 상점 문 앞은 비운다
                bool wet = false;
                for (int z = -2; z <= 2 && !wet; z++)
                    for (int x = -2; x <= 2; x++)
                        if (_grid.TryGetCell(c + new Vector2Int(x, z), out var near) && (near.HasWater || !near.IsWalkable)) { wet = true; break; }
                if (!wet) result.Add(c);
            }
        return result;
    }
}

// 흙이 들썩이는 자국 하나. 삽을 들고 [E] → 삽날이 닿는 순간 파여 보상이 바닥에 떨어진다.
public sealed class DigSpot : MonoBehaviour, IInteractable
{
    DigSpots _owner;
    int _slot, _amount;
    Item _loot;
    GameObject _mound, _hole;
    bool _dug, _pending;
    float _seed;

    public bool Dug => _dug;
    public Item LootItem => _loot;

    internal void Setup(DigSpots owner, int slot, Item loot, int amount, GameObject mound, GameObject hole)
    {
        _owner = owner; _slot = slot; _loot = loot; _amount = amount; _mound = mound; _hole = hole;
        _hole.SetActive(false);
        _seed = slot * 1.37f;
    }

    public string GetInteractPrompt()
    {
        if (_dug) return "파낸 자리";
        bool shovel = Inventory.instance != null && EquipmentSystem.CurrentHeld(Inventory.instance.gameObject)?.toolType == ToolType.Shovel;
        return shovel ? "삽으로 파기 · 무언가 묻혀 있어요" : "흙이 들썩여요 · 삽을 들면 팔 수 있어요";
    }

    public void Interact(GameObject interactor)
    {
        if (_dug || _pending || interactor == null) return;
        Vector3 delta = interactor.transform.position - transform.position;
        if (Vector3.ProjectOnPlane(delta, Vector3.up).magnitude > 2.2f || Mathf.Abs(delta.y) > 2f) return;
        if (EquipmentSystem.CurrentHeld(interactor)?.toolType != ToolType.Shovel)
        {
            interactor.GetComponent<EquipmentSystem>()?.FailedUse("삽이 있어야 팔 수 있어요", false);
            FirstDayWorldPresentation.Toast("흙이 들썩이는 자리는 삽으로 팔 수 있어요. 삽은 현장 작업대에서 만들어요.", false);
            return;
        }
        StartCoroutine(Dig(interactor));
    }

    // 삽날이 땅에 닿는 순간(ContactDelay) 파인다. 그 사이 도구를 바꾸면 아무 일도 없다.
    IEnumerator Dig(GameObject interactor)
    {
        _pending = true;
        yield return new WaitForSeconds(GatherFeedback.ContactDelay);
        _pending = false;
        if (interactor == null || _dug || EquipmentSystem.CurrentHeld(interactor)?.toolType != ToolType.Shovel) yield break;
        _dug = true;
        _mound.SetActive(false);
        _hole.SetActive(true);
        GatherFeedback.Dig(transform.position + Vector3.up * .12f);
        var inventory = interactor.GetComponent<Inventory>();
        if (inventory != null) ToolDurability.ConsumeSuccess(inventory, inventory.GetSelectedInstance());
        int slot = _slot; var owner = _owner;
        GatherFeedback.SpawnDrop(_loot, transform.position, interactor.transform.position, () => { if (owner != null) owner.MarkDug(slot); }, _amount);
    }

    // 몇 초마다 흙이 한 번 들썩인다(멀리서도 '뭔가 있다'가 보이게). 판 뒤에는 멈춘다.
    void Update()
    {
        if (_dug || _mound == null) return;
        float t = Mathf.Repeat(Time.time * .45f + _seed, 1f);
        float bump = t < .14f ? Mathf.Sin(t / .14f * Mathf.PI) : 0f;
        _mound.transform.localScale = new Vector3(1f - .06f * bump, 1f + .28f * bump, 1f - .06f * bump);
    }
}
