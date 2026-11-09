using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 호박벌(아이템 생태 연구 B05 "꽃 군락 가장자리, 08–18, 통통한 몸, 꽃 사이 머무는 위치를 기다림"): 실제 종 Bombus ignitus.
// 꽃(CoastalVegetationField.FlowerSpots) 몇 송이를 돌며 한 송이에 4~7초 앉았다가 다음 꽃으로 붕 날아간다. 나는 동안은 잡을 수 없다(BugCritter.Fleeing).
// 꽃 목록과 플레이어가 준비되면 한 번, 플레이어에게 가까운 꽃 중 서로 8m 넘게 떨어진 두 곳을 출발 꽃으로 삼는다(칠성무당벌레가 앉은 꽃은 피한다).
// 잡기·보상·하루 반복·저장은 기존 BugCritter 그대로(앉아 있음, 2.4m 안에서 달려오면 날아감, 그물 1.3m). 잡힌 뒤에는 출발 꽃으로 돌아온다.
public sealed class BumblebeeSpawner : MonoBehaviour
{
    public const string NamePrefix = "FirstDay_Insect_Bumblebee_";
    const float DayFrom = 8f, DayUntil = 18f;
    const float SitMin = 4f, SitMax = 7f, Speed = 2.4f, Perch = .32f;
    const float RouteMin = 2f, RouteMax = 7f;
    const int RouteFlowers = 4;

    sealed class Bee
    {
        public BugCritter Critter;
        public Vector3[] Route;
        public int At;
        public float LeaveAt;
    }

    Bee[] _bees;
    float _nextCheck, _firstTry = -1f;
    public static int Hops { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Hops = 0;

    public static bool Daytime(float hour) => hour >= DayFrom && hour < DayUntil;
    public int Spawned => _bees != null ? _bees.Length : 0;
    public int RouteLength(int bee) => _bees != null && bee >= 0 && bee < _bees.Length ? _bees[bee].Route.Length : 0;

    void Update()
    {
        if (_bees != null) Wander();
        if (Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + .5f;
        if (_bees == null) TrySpawn();
        if (_bees == null) return;
        bool on = Daytime(GameClock.Instance != null ? GameClock.Instance.CurrentHour : 12f);
        foreach (var bee in _bees) if (bee.Critter != null && bee.Critter.gameObject.activeSelf != on) bee.Critter.gameObject.SetActive(on);
    }

    void Wander()
    {
        foreach (var bee in _bees)
        {
            var c = bee.Critter;
            if (c == null || !c.isActiveAndEnabled) continue;
            // 잡히면 출발 꽃으로 돌아오므로 경로도 처음부터. 돌아온 뒤 한동안은 앉아 있는다.
            if (c.Captured) { bee.At = 0; bee.LeaveAt = Time.time + SitMax; continue; }
            // 놀라 날아간 뒤(Startle)에도 내려앉아 잠깐 쉬었다가 다음 꽃으로 간다.
            if (c.Fleeing) { bee.LeaveAt = Mathf.Max(bee.LeaveAt, Time.time + SitMin); continue; }
            if (bee.Route.Length < 2 || Time.time < bee.LeaveAt) continue;
            // 다음 꽃부터 차례로, 다른 곤충이 앉지 않았고 지금 자리가 아닌 꽃을 고른다. 없으면 1초 뒤 다시 본다.
            // bumblebee-r181: 앉은 꽃을 건너뛸 때 순번만 넘기고 날지 않아, 다음 비행이 지금 앉은 꽃으로 '제자리 비행'(0m)이 됐다.
            int next = -1;
            for (int k = 1; k < bee.Route.Length && next < 0; k++)
            {
                int i = (bee.At + k) % bee.Route.Length;
                if (Flat(bee.Route[i] - c.Home) >= 1f && !Occupied(bee.Route[i], c)) next = i;
            }
            if (next < 0) { bee.LeaveAt = Time.time + 1f; continue; }
            Vector3 to = bee.Route[next] + Vector3.up * Perch;
            float seconds = Mathf.Clamp(Mathf.Sqrt(Flat(to - c.transform.position)) / Speed, 1.2f, 3f);
            if (!c.FlyTo(to, seconds, .45f, .35f)) continue;
            bee.At = next;
            bee.LeaveAt = Time.time + seconds + Random.Range(SitMin, SitMax);
            Hops++;
        }
    }

    // 꽃 위(.5m 안)에 다른 곤충이 앉아 있다. 꽃 .8m 옆 땅의 방아깨비는 꽃을 막지 않는다.
    static bool Occupied(Vector3 flower, BugCritter self) =>
        FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Any(b => b != self && b.isActiveAndEnabled && !b.Captured && Flat(b.Home - flower) < .5f * .5f);

    void TrySpawn()
    {
        var item = Resources.Load<Item>("Items/Item_Bug_Bumblebee");
        var player = Inventory.instance != null ? Inventory.instance.transform : null;
        var spots = CoastalVegetationField.FlowerSpots;
        if (item == null || item.model == null || player == null || spots.Count == 0) return;
        // 칠성무당벌레가 먼저 자리를 잡게 둔다(5초까지만 기다린다).
        if (_firstTry < 0f) _firstTry = Time.unscaledTime;
        var ladybugs = FindFirstObjectByType<LadybugSpawner>();
        if (ladybugs != null && ladybugs.Spawned == 0 && Time.unscaledTime - _firstTry < 5f) return;
        var taken = FindObjectsByType<BugCritter>(FindObjectsSortMode.None).Select(b => b.Home).ToList();
        var free = spots.Where(s => !taken.Any(t => Flat(t - s) < 1.2f * 1.2f)).ToList();
        var starts = new List<(Vector3 Start, List<Vector3> Route)>();
        // 다음 꽃이 있는 출발 꽃을 가까운 30송이 안에서 고른다. 못 찾으면 가장 가까운 꽃 하나에 앉아만 있는다.
        foreach (var spot in free.OrderBy(s => Flat(s - player.position)).Take(30))
        {
            if (starts.Any(c => Flat(c.Start - spot) < 64f)) continue;
            var route = BuildRoute(spot, free);
            if (route.Count < 2) continue;
            starts.Add((spot, route));
            if (starts.Count == 2) break;
        }
        if (starts.Count == 0 && free.Count > 0)
        {
            var spot = free.OrderBy(s => Flat(s - player.position)).First();
            starts.Add((spot, BuildRoute(spot, free)));
        }
        if (starts.Count == 0) return;
        _bees = new Bee[starts.Count];
        for (int i = 0; i < starts.Count; i++)
        {
            Vector3 at = starts[i].Start + Vector3.up * Perch; // 꽃 송이 높이(.3~.45m)
            var go = new GameObject(NamePrefix + i);
            go.transform.SetParent(transform);
            go.transform.position = at;
            var model = FirstDayStudioAssets.Place(item.model, go.transform, at, .22f); // bumblebee-r178: .18은 기본 카메라에서 작은 검은 점에 가까웠다
            AttachWings(go.transform, model.transform);
            go.transform.rotation = Quaternion.Euler(0f, 40f + i * 137f, 0f);
            go.AddComponent<SphereCollider>().isTrigger = true;
            var critter = go.AddComponent<BugCritter>();
            critter.Configure(item, i * 1.7f + .4f);
            critter.ConfigureSpecies(true, 2.4f, 1.3f);
            critter.UseOwnWings();
            _bees[i] = new Bee { Critter = critter, Route = starts[i].Route.ToArray(), At = 0, LeaveAt = Time.time + SitMin + i * 1.5f };
        }
    }

    // 출발 꽃에서 2~7m 안의 꽃을 가까운 순으로 최대 3송이 더 이어 한 바퀴 경로를 만든다(다음 꽃은 지금 꽃에서 가장 가까운 꽃).
    static List<Vector3> BuildRoute(Vector3 start, List<Vector3> free)
    {
        var near = free.Where(s => { float d = Flat(s - start); return d >= RouteMin * RouteMin && d <= RouteMax * RouteMax; }).ToList();
        var route = new List<Vector3> { start };
        while (route.Count < RouteFlowers && near.Count > 0)
        {
            Vector3 last = route[route.Count - 1];
            var next = near.OrderBy(s => Flat(s - last)).First();
            near.Remove(next);
            if (route.Any(r => Flat(r - next) < RouteMin * RouteMin)) continue;
            route.Add(next);
        }
        return route;
    }

    // 모델의 날개(BeeWingL/R)를 날개 뿌리 축(LeftWing/RightWing) 아래로 옮긴다. BugCritter.Flap이 축을 돌려 날갯짓한다.
    static void AttachWings(Transform bug, Transform model)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            var wing = model.Find(side < 0 ? "BeeWingL" : "BeeWingR");
            if (wing == null) continue;
            var pivot = new GameObject(side < 0 ? "LeftWing" : "RightWing").transform;
            pivot.SetParent(bug, false);
            pivot.position = model.TransformPoint(new Vector3(side * .03f, .15f, -.04f));
            pivot.rotation = bug.rotation;
            wing.SetParent(pivot, true);
        }
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude; }
}
