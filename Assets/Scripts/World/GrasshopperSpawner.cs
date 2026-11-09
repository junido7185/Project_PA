using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 방아깨비(아이템 생태 연구 B04 "짧은 풀·흙 경계, 06–18, 긴 뒷다리, 도약 뒤 정지 때 다가감"): 실제 종 Acrida cinerea(갈색형).
// 풀밭과 모래·흙이 만나는 가장자리(CoastalVegetationField.FlowerSpots — 그 가장자리 칸의 꽃 자리) 옆 땅 위에 두 마리.
// 3~6초 멈춰 있다가 1~1.8m 폴짝 뛴다(처음 자리에서 4m 안, 걸을 수 있고 높이 차 .4m 안의 칸). 뛰는 동안은 잡을 수 없다(BugCritter.Fleeing).
// 잡기·보상·하루 반복·저장은 기존 BugCritter 그대로(땅에 앉아 있음, 2m 안에서 달려오면 멀리 뜀, 그물 1.3m). 6~18시에만.
public sealed class GrasshopperSpawner : MonoBehaviour
{
    public const string NamePrefix = "FirstDay_Insect_Grasshopper_";
    const float DayFrom = 6f, DayUntil = 18f;
    const float RestMin = 3f, RestMax = 6f, HopMin = 1f, HopMax = 1.8f, Leash = 4f, Lift = .02f;

    sealed class Hopper
    {
        public BugCritter Critter;
        public Vector3 Origin;
        public float LeaveAt;
    }

    Hopper[] _hoppers;
    WorldGridService _grid;
    float _nextCheck;
    public static int Hops { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Hops = 0;

    public static bool Daytime(float hour) => hour >= DayFrom && hour < DayUntil;
    public int Spawned => _hoppers != null ? _hoppers.Length : 0;

    void Update()
    {
        if (_hoppers != null) Hop();
        if (Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + .5f;
        if (_hoppers == null) TrySpawn();
        if (_hoppers == null) return;
        bool on = Daytime(GameClock.Instance != null ? GameClock.Instance.CurrentHour : 12f);
        foreach (var h in _hoppers) if (h.Critter != null && h.Critter.gameObject.activeSelf != on) h.Critter.gameObject.SetActive(on);
    }

    void Hop()
    {
        foreach (var h in _hoppers)
        {
            var c = h.Critter;
            if (c == null || !c.isActiveAndEnabled) continue;
            if (c.Captured) { h.LeaveAt = Time.time + RestMax; continue; }
            // 놀라 멀리 뛴 뒤에도 내려앉아 잠깐 멈춘다.
            if (c.Fleeing) { h.LeaveAt = Mathf.Max(h.LeaveAt, Time.time + RestMin); continue; }
            if (Time.time < h.LeaveAt) continue;
            h.LeaveAt = Time.time + Random.Range(RestMin, RestMax);
            if (!NextLanding(c.Home, h.Origin, out Vector3 to)) continue;
            if (c.FlyTo(to, .5f, .5f, 0f)) Hops++;
        }
    }

    // 지금 자리에서 1~1.8m 떨어진, 걸을 수 있고 처음 자리 땅 높이와 .4m 안인 칸(처음 자리에서 4m 안). 여덟 번 찾아 없으면 그대로.
    bool NextLanding(Vector3 from, Vector3 origin, out Vector3 to)
    {
        to = from;
        if (_grid == null) _grid = FindFirstObjectByType<WorldGridService>();
        float baseY = origin.y - Lift;
        for (int k = 0; k < 8; k++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            Vector3 candidate = from + new Vector3(dir.x, 0f, dir.y) * Random.Range(HopMin, HopMax);
            if (Flat(candidate - origin) > Leash * Leash) continue;
            if (!Ground(candidate, out float y) || Mathf.Abs(y - baseY) > .4f) continue;
            to = new Vector3(candidate.x, y + Lift, candidate.z);
            return true;
        }
        return false;
    }

    bool Ground(Vector3 at, out float y)
    {
        y = at.y;
        if (_grid == null) return false;
        if (!_grid.WorldToCell(at, out var cell) || !_grid.TryGetCell(cell, out var data) || !data.IsWalkable || !_grid.CellToWorld(cell, out var ground)) return false;
        y = ground.y;
        return true;
    }

    void TrySpawn()
    {
        var item = Resources.Load<Item>("Items/Item_Bug_Grasshopper");
        var player = Inventory.instance != null ? Inventory.instance.transform : null;
        var spots = CoastalVegetationField.FlowerSpots;
        if (_grid == null) _grid = FindFirstObjectByType<WorldGridService>();
        if (item == null || item.model == null || player == null || spots.Count == 0 || _grid == null) return;
        // 꽃 바로 위는 칠성무당벌레·호박벌 자리라, 꽃에서 .8m 옆 땅에 놓는다(같은 가장자리 칸).
        var taken = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Select(b => b.Home).ToList();
        var chosen = new List<Vector3>();
        foreach (var spot in spots.OrderBy(s => Flat(s - player.position)))
        {
            if (chosen.Any(c => Flat(c - spot) < 64f)) continue;
            Vector3 at = Vector3.zero;
            bool found = false;
            for (int k = 0; k < 6 && !found; k++)
            {
                float a = k * 60f * Mathf.Deg2Rad;
                Vector3 candidate = spot + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * .8f;
                if (!Ground(candidate, out float y) || Mathf.Abs(y - spot.y) > .3f || taken.Any(t => Flat(t - candidate) < 1f)) continue;
                at = new Vector3(candidate.x, y + Lift, candidate.z);
                found = true;
            }
            if (!found) continue;
            chosen.Add(at);
            if (chosen.Count == 2) break;
        }
        if (chosen.Count == 0) return;
        _hoppers = new Hopper[chosen.Count];
        for (int i = 0; i < chosen.Count; i++)
        {
            var go = new GameObject(NamePrefix + i);
            go.transform.SetParent(transform);
            go.transform.position = chosen[i];
            FirstDayStudioAssets.Place(item.model, go.transform, chosen[i], .2f); // grasshopper-r180: .13은 기본 카메라에서 25px 가는 막대였다
            go.transform.rotation = Quaternion.Euler(0f, 70f + i * 151f, 0f);
            go.AddComponent<SphereCollider>().isTrigger = true;
            var critter = go.AddComponent<BugCritter>();
            critter.Configure(item, i * 1.3f + .5f);
            critter.ConfigureSpecies(true, 2f, 1.3f);
            critter.UseOwnWings(); // 날개 축은 없다 — 뛰는 동안 몸 폭을 접지 않게
            _hoppers[i] = new Hopper { Critter = critter, Origin = chosen[i], LeaveAt = Time.time + RestMin + i * 1.2f };
        }
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude; }
}
