using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 해변 줍기(Docs/01_GAME_DESIGN/Content/BEACHCOMBING.md). 레퍼런스 해안 장면(References/Dinkum 03)의 '모래사장 발견물' 고리.
// 날마다 물가 모래 칸에 가리비·소라·불가사리를 놓는다. 빈손 [E]로 줍는 기존 PickupItem(맥락 줍기)을 그대로 쓴다.
// 새 저장 필드는 없다: 놓인 위치는 (날짜 → 시드)로 다시 만들고, 오늘 주운 칸 번호만 이 세션 동안 기억한다.
public sealed class BeachFinds : MonoBehaviour
{
    public static BeachFinds Instance { get; private set; }
    public static bool Enabled = true;

    const int DailyCount = 9;            // 하루에 밀려오는 발견물 수
    const float MinSpacing = 7f;         // 발견물 사이 최소 거리(한 곳에 몰리지 않게)
    const int WaterReach = 2;            // 물에서 2칸(4m) 이내의 모래만 '물가'
    // size는 FirstDayStudioAssets.Place의 '높이'다. 납작한 모델이라 폭은 높이보다 크다:
    // 가리비 높이 .1 ≈ 폭 .43m, 소라 .28 ≈ 길이 .42m, 불가사리 .05 ≈ 폭 .5m(기본 카메라에서 읽히게 실물보다 조금 크게).
    static readonly (string resource, int weight, float size)[] Kinds =
    {
        // 2026-11-03 해초(아이템 생태 연구 H04): 하루 9개는 그대로, 기존 셋의 몫을 조금씩 덜어 16%. 해초 높이 .05 ≈ 길이 .6m(첫 .028은 기본 카메라에서 풀 조각처럼 작았다).
        // 2026-11-03 해변 유리(연구 H05): 기존 넷의 비율을 같이 줄여 10%(불가사리보다 조금 드묾). 유리 높이 .11 ≈ 폭 .45m(첫 .07은 기본 카메라에서 점처럼 작았다).
        // 2026-11-04 산호 조각(연구 H06) 8%: 하루 9개 그대로, 기존 다섯의 몫을 같이 줄였다. 산호 높이 .5(첫 .3은 기본 카메라에서 점처럼 작았다, beach-r108).
        ("Items/Item_ScallopShell", 37, .1f),
        ("Items/Item_ConchShell", 23, .28f),
        ("Items/Item_Starfish", 10, .05f),
        ("Items/Item_Seaweed", 13, .05f),
        ("Items/Item_BeachGlass", 9, .11f),
        ("Items/Item_CoralPiece", 8, .5f),
    };

    public int Day { get; private set; } = -1;
    public int Placed => _finds.Count;
    public int CollectedToday => _collected.Count;
    public int CandidateCells { get; private set; }
    public IReadOnlyList<InventoryFramework.PickupItem> Finds => _finds;

    WorldGridService _grid;
    float _nextCheck;
    readonly List<InventoryFramework.PickupItem> _finds = new List<InventoryFramework.PickupItem>();
    readonly HashSet<int> _collected = new HashSet<int>();

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
        var host = new GameObject("PA_BeachFinds");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<BeachFinds>();
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

    public List<int> CollectedSlots() => new List<int>(_collected);

    // 이어하기: 저장된 날짜가 지금 날짜와 같으면 그날 주운 자리를 빼고 다시 놓는다.
    public void RestoreCollected(int day, List<int> collected)
    {
        int today = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        _collected.Clear();
        if (day == today && collected != null) foreach (int slot in collected) _collected.Add(slot);
        Day = -1; _keepFor = today;
        _nextCheck = 0f;
        if (_grid != null && _grid.Definition != null && Inventory.instance != null) Refresh(today);
    }

    // 새 날: 어제 남은 것은 파도에 쓸려 가고 새 발견물이 놓인다.
    int _keepFor = -1;
    void Refresh(int day)
    {
        if (day != Day && day != _keepFor) _collected.Clear();
        _keepFor = -1;
        Day = day;
        foreach (var find in _finds) if (find != null) Destroy(find.gameObject);
        _finds.Clear();
        var assets = FirstDayStudioAssets.Load();
        var cells = ShoreSand();
        CandidateCells = cells.Count;
        if (cells.Count == 0) { Debug.LogWarning("[BeachFinds] 물가 모래 칸이 없어 발견물을 놓지 않았습니다."); return; }
        var random = new System.Random(day * 7919 + 4111);
        var chosen = new List<Vector3>();
        int total = 0; foreach (var kind in Kinds) total += kind.weight;
        for (int attempt = 0; attempt < 400 && chosen.Count < DailyCount; attempt++)
        {
            var cell = cells[random.Next(cells.Count)];
            if (!_grid.CellToWorld(cell, out var ground)) continue;
            float half = _grid.Definition.CellSize * .35f;
            ground += new Vector3((float)(random.NextDouble() * 2 - 1) * half, 0, (float)(random.NextDouble() * 2 - 1) * half);
            if (chosen.Exists(p => (p - ground).sqrMagnitude < MinSpacing * MinSpacing)) continue;
            int roll = random.Next(total), index = 0;
            while (roll >= Kinds[index].weight) { roll -= Kinds[index].weight; index++; }
            float yaw = (float)random.NextDouble() * 360f;
            int slot = chosen.Count;
            chosen.Add(ground);
            if (_collected.Contains(slot)) continue;
            var item = Resources.Load<Item>(Kinds[index].resource);
            var model = item != null ? item.model : null;
            if (model == null) continue;
            if (Physics.Raycast(ground + Vector3.up * 3f, Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore)) ground.y = hit.point.y;
            var go = FirstDayStudioAssets.Place(model, transform, ground, Kinds[index].size, yaw);
            go.name = $"BeachFind_{slot}_{item.itemName}";
            var collider = go.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = .3f / Mathf.Max(.001f, go.transform.lossyScale.x);
            var pickup = go.AddComponent<InventoryFramework.PickupItem>();
            pickup.item = item; pickup.amount = 1; pickup.contextual = true;
            pickup.Claimed = () => _collected.Add(slot);
            FindSparkle.Attach(go, slot + day * 7);
            _finds.Add(pickup);
        }
        Debug.Log($"[BeachFinds] day={day} placed={_finds.Count} candidates={cells.Count} collectedEarlier={_collected.Count}");
    }

    // 물에서 WaterReach칸 안의 걸을 수 있는 모래 칸(길·건물 제외).
    List<Vector2Int> ShoreSand()
    {
        var result = new List<Vector2Int>();
        var definition = _grid.Definition;
        for (int z = 0; z < definition.Height; z++)
            for (int x = 0; x < definition.Width; x++)
            {
                var c = new Vector2Int(x, z);
                if (!_grid.TryGetCell(c, out var cell) || cell.GroundType != WorldGroundType.Sand || !cell.IsWalkable || cell.HasPath) continue;
                bool shore = false;
                for (int dz = -WaterReach; dz <= WaterReach && !shore; dz++)
                    for (int dx = -WaterReach; dx <= WaterReach; dx++)
                        if (_grid.TryGetCell(c + new Vector2Int(dx, dz), out var near) && near.HasWater) { shore = true; break; }
                if (shore) result.Add(c);
            }
        return result;
    }
}
