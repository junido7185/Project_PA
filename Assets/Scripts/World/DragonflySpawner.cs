using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 고추잠자리(아이템 생태 연구 B06): 물가 바위(CoastalVegetationField.WaterlineStones) 위, 모래 쪽에 가까이 떠서 맴돈다. 8~19시에만.
// 물가 바위 목록과 플레이어가 준비되면 한 번, 플레이어에게 가까운 바위 중 서로 8m 넘게 떨어진 두 곳에 놓는다(물 가장자리 바로 위, 모래 끝에서 그물이 닿게 .7).
// 잡기·보상·하루 반복·저장은 기존 BugCritter 그대로(떠다님, 1.8m 안에서 달려오면 날아감, 그물 1.3m).
public sealed class DragonflySpawner : MonoBehaviour
{
    public const string NamePrefix = "FirstDay_Insect_Dragonfly_";
    const float DayFrom = 8f, DayUntil = 19f;
    BugCritter[] _bugs;
    float _nextCheck;

    public static bool Daytime(float hour) => hour >= DayFrom && hour < DayUntil;

    void Update()
    {
        if (Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + .5f;
        if (_bugs == null) TrySpawn();
        if (_bugs == null) return;
        bool on = Daytime(GameClock.Instance != null ? GameClock.Instance.CurrentHour : 12f);
        foreach (var bug in _bugs) if (bug != null && bug.gameObject.activeSelf != on) bug.gameObject.SetActive(on);
    }

    void TrySpawn()
    {
        var item = Resources.Load<Item>("Items/Item_Bug_Dragonfly");
        var player = Inventory.instance != null ? Inventory.instance.transform : null;
        var stones = CoastalVegetationField.WaterlineStones;
        if (item == null || item.model == null || player == null || stones.Count == 0) return;
        var chosen = new List<(Vector3 Stone, Vector3 Shore)>();
        foreach (var s in stones.OrderBy(s => Flat(s.Shore - player.position)))
        {
            if (chosen.Any(c => Flat(c.Shore - s.Shore) < 64f)) continue;
            chosen.Add(s);
            if (chosen.Count == 2) break;
        }
        _bugs = new BugCritter[chosen.Count];
        for (int i = 0; i < chosen.Count; i++)
        {
            Vector3 shore = chosen[i].Shore, stone = chosen[i].Stone;
            Vector3 at = Vector3.Lerp(new Vector3(shore.x, shore.y, shore.z), new Vector3(stone.x, shore.y, stone.z), .7f) + Vector3.up * 1.05f; // dragonfly-r176: .45는 물가에서 1.5m 떨어진 모래 위였다 → 물 가장자리 위
            var go = new GameObject(NamePrefix + i);
            go.transform.SetParent(transform);
            go.transform.position = at;
            go.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(stone - shore, Vector3.up).normalized + Vector3.right * .2f);
            FirstDayStudioAssets.Place(item.model, go.transform, at, .14f);
            go.AddComponent<SphereCollider>().isTrigger = true;
            var critter = go.AddComponent<BugCritter>();
            critter.Configure(item, i * 1.9f + .6f);
            critter.ConfigureSpecies(false, 1.8f, 1.3f);
            _bugs[i] = critter;
        }
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude; }
}
