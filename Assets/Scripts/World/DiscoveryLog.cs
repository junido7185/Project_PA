using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 발견 도감(Docs/01_GAME_DESIGN/Content/COLLECTION.md). 섬에서 처음 손에 넣은 물고기·곤충·해변·땅속 발견물을 기록한다.
// 권위는 가방(Inventory)이다: 0.5초마다 핫바·가방을 훑어 처음 보는 대상 아이템을 기록만 한다(아이템·재고는 건드리지 않음).
// 저장: SaveData.discoveredItemIds(v17 안 추가 필드). 이어하기 직후 가방에서 다시 찾은 것은 알림 없이 조용히 기록한다.
public sealed class DiscoveryLog : MonoBehaviour
{
    public static readonly (string section, int[] ids)[] Sections =
    {
        ("바다", new[] { 8, 3001, 3002 }),
        ("곤충", new[] { 2004 }),
        ("해변", new[] { 2034, 2035, 2036 }),
        ("땅속", new[] { 2039, 2040, 2041 }),
    };

    static readonly HashSet<int> Found = new HashSet<int>();
    static readonly HashSet<int> Tracked = new HashSet<int>();
    static DiscoveryLog _instance;
    public static int Total { get { EnsureTracked(); return Tracked.Count; } }
    public static int FoundCount => Found.Count;
    public static bool IsFound(int id) => Found.Contains(id);
    public static int Revision { get; private set; }

    const float QuietAfterLoad = 4f;
    float _nextScan, _loadedAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Found.Clear(); _instance = null; Revision = 0; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterBootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 섬을 새로 열 때마다 비운다(새 게임이 이전 세션 기록을 물려받지 않게). 이어하기는 곧이어 저장 기록을 되돌린다.
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != DemoRouteController.WorldScene) return;
        Found.Clear(); Revision++;
        if (_instance != null && _instance.gameObject.scene == scene) return;
        var host = new GameObject("PA_DiscoveryLog");
        SceneManager.MoveGameObjectToScene(host, scene);
        _instance = host.AddComponent<DiscoveryLog>();
    }

    static void EnsureTracked()
    {
        if (Tracked.Count > 0) return;
        foreach (var section in Sections) foreach (int id in section.ids) Tracked.Add(id);
    }

    public static List<int> Snapshot() => new List<int>(Found);

    public static void Restore(List<int> ids)
    {
        Found.Clear();
        if (ids != null) foreach (int id in ids) Found.Add(id);
        Revision++;
        if (_instance != null) _instance._loadedAt = Time.unscaledTime;
    }

    void Awake() => _loadedAt = Time.unscaledTime;

    // 아침 소식: 날이 바뀌면(불러오기 직후 제외) 새로 놓인 해변 발견물·파기 자국 수를 알리고 휴대폰 기록에 남긴다.
    int _newsDay = -1;
    float _newsAt = -1f;
    void MorningNews()
    {
        int day = GameClock.Instance != null ? GameClock.Instance.CurrentDay : 1;
        if (_newsDay < 0 || Time.unscaledTime - _loadedAt < QuietAfterLoad) { _newsDay = day; return; }
        if (day != _newsDay && _newsAt < 0f) _newsAt = Time.unscaledTime + 2f; // 두 배치가 새 날을 다 놓은 뒤
        if (_newsAt < 0f || Time.unscaledTime < _newsAt) return;
        _newsAt = -1f; _newsDay = day;
        int beach = BeachFinds.Instance != null ? BeachFinds.Instance.Placed : 0;
        int dig = DigSpots.Instance != null ? DigSpots.Instance.Spots.Count : 0;
        if (beach + dig == 0) return;
        FirstDayWorldPresentation.Toast($"Day {day} 아침\n해변 발견물 {beach}개 · 파기 자국 {dig}개가 새로 생겼어요");
        NewsShown++;
    }
    public static int NewsShown { get; private set; }
    void OnDestroy() { if (_instance == this) _instance = null; }

    void Update()
    {
        if (Time.unscaledTime < _nextScan) return;
        _nextScan = Time.unscaledTime + .5f;
        MorningNews();
        var inventory = Inventory.instance;
        if (inventory == null) return;
        EnsureTracked();
        bool quiet = Time.unscaledTime - _loadedAt < QuietAfterLoad;
        if (inventory.hotbar != null) Scan(inventory.hotbar.slots, quiet);
        Scan(inventory.slots, quiet);
    }

    static void Scan(List<InventorySlot> slots, bool quiet)
    {
        if (slots == null) return;
        foreach (var slot in slots)
        {
            if (slot == null || slot.IsEmpty || slot.item == null) continue;
            int id = slot.item.id;
            if (!Tracked.Contains(id) || !Found.Add(id)) continue;
            Revision++;
            if (quiet) continue;
            // 섹션(바다·곤충·해변·땅속)이나 도감 전체를 채운 순간은 완성 알림으로 바꿔 보여 준다.
            string done = Found.Count == Tracked.Count ? "도감을 모두 채웠어요!" : SectionCompleted(id);
            FirstDayWorldPresentation.Toast(done != null
                ? $"{done}\n마지막 발견: {ItemDisplayName.For(slot.item)} · 도감 {Found.Count}/{Tracked.Count}"
                : $"새 발견! {ItemDisplayName.For(slot.item)}\n도감 {Found.Count}/{Tracked.Count}");
            if (done != null) Completions++;
        }
    }

    public static int Completions { get; private set; }

    static string SectionCompleted(int id)
    {
        foreach (var (section, ids) in Sections)
        {
            if (System.Array.IndexOf(ids, id) < 0) continue;
            foreach (int other in ids) if (!Found.Contains(other)) return null;
            return $"도감 · {section} 완성!";
        }
        return null;
    }
}
