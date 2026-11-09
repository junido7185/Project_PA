using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 칠성무당벌레(아이템 생태 연구 B03): 풀밭 가장자리 꽃(CoastalVegetationField.FlowerSpots) 위에 두 마리. 6~18시에만 보인다.
// 꽃 목록과 플레이어가 준비되면 한 번, 플레이어에게 가까운 꽃 중 서로 6m 넘게 떨어진 두 송이에 놓는다.
// 잡기·보상·하루 반복·저장은 기존 BugCritter 그대로(앉아 있음, 2.2m 안에서 달려오면 날아감, 그물 1.3m).
public sealed class LadybugSpawner : MonoBehaviour
{
    public const string NamePrefix = "FirstDay_Insect_Ladybug_";
    const float DayFrom = 6f, DayUntil = 18f;
    BugCritter[] _bugs;
    float _nextCheck;

    public static bool Daytime(float hour) => hour >= DayFrom && hour < DayUntil;
    public int Spawned => _bugs != null ? _bugs.Length : 0;

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
        var item = Resources.Load<Item>("Items/Item_Bug_Ladybug");
        var player = Inventory.instance != null ? Inventory.instance.transform : null;
        var spots = CoastalVegetationField.FlowerSpots;
        if (item == null || item.model == null || player == null || spots.Count == 0) return;
        var chosen = new List<Vector3>();
        foreach (var spot in spots.OrderBy(s => Flat(s - player.position)))
        {
            if (chosen.Any(c => Flat(c - spot) < 36f)) continue;
            chosen.Add(spot);
            if (chosen.Count == 2) break;
        }
        _bugs = new BugCritter[chosen.Count];
        for (int i = 0; i < chosen.Count; i++)
        {
            Vector3 at = chosen[i] + Vector3.up * .3f; // 꽃 송이 높이(.3~.45m)
            var go = new GameObject(NamePrefix + i);
            go.transform.SetParent(transform);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(0f, i * 137f, 0f);
            FirstDayStudioAssets.Place(item.model, go.transform, at, .16f); // ladybug-r174: .09는 기본 카메라에서 빨간 점 하나였다(장수풍뎅이와 같은 .16)
            go.AddComponent<SphereCollider>().isTrigger = true;
            var critter = go.AddComponent<BugCritter>();
            critter.Configure(item, i * 1.1f + .2f);
            critter.ConfigureSpecies(true, 2.2f, 1.3f);
            _bugs[i] = critter;
        }
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude; }
}
