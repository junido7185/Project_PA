using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 왕사마귀(아이템 생태 연구 B10 "낮은 꽃/덤불 잎 옆, 06–18, 접은 앞다리·삼각 머리·짧은 좌우 흔들림. 색위장만으로 숨기지 않음"): 실제 종 Tenodera sinensis(녹색형).
// 꽃(CoastalVegetationField.FlowerSpots) 위에 한 마리. 칠성무당벌레·호박벌이 자리 잡은 뒤(5초까지만 기다림) 다른 곤충과 6m 넘게 떨어진, 플레이어에게 가까운 꽃.
// 바람에 흔들리는 잎처럼 좌우로 살랑(MantisSway) — 연두 몸이 풀색에 묻히지 않게 움직임과 갈색 날개 가장자리가 단서. 6~18시에만.
// 잡기·보상·하루 반복·저장은 기존 BugCritter 그대로(앉아 있음, 2.2m 안에서 달려오면 날아감, 그물 1.3m).
public sealed class MantisSpawner : MonoBehaviour
{
    public const string Name = "FirstDay_Insect_Mantis_0";
    const float DayFrom = 6f, DayUntil = 18f;
    BugCritter _bug;
    float _nextCheck, _firstTry = -1f;

    public static bool Daytime(float hour) => hour >= DayFrom && hour < DayUntil;

    void Update()
    {
        if (Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + .5f;
        if (_bug == null) TrySpawn();
        if (_bug == null) return;
        bool on = Daytime(GameClock.Instance != null ? GameClock.Instance.CurrentHour : 12f);
        if (_bug.gameObject.activeSelf != on) _bug.gameObject.SetActive(on);
    }

    void TrySpawn()
    {
        var item = Resources.Load<Item>("Items/Item_Bug_Mantis");
        var player = Inventory.instance != null ? Inventory.instance.transform : null;
        var spots = CoastalVegetationField.FlowerSpots;
        if (item == null || item.model == null || player == null || spots.Count == 0) return;
        if (_firstTry < 0f) _firstTry = Time.unscaledTime;
        var ladybugs = FindFirstObjectByType<LadybugSpawner>();
        var bees = FindFirstObjectByType<BumblebeeSpawner>();
        bool othersReady = (ladybugs == null || ladybugs.Spawned > 0) && (bees == null || bees.Spawned > 0);
        if (!othersReady && Time.unscaledTime - _firstTry < 5f) return;
        var critters = FindObjectsByType<BugCritter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var taken = critters.Select(b => b.Home).ToList();
        // mantis-r198: 호박벌 경로(출발 꽃에서 2~7m)의 꽃에 앉으면 벌이 옮겨 갈 꽃이 막혔다 → 벌에게서 8m 넘게.
        var beeHomes = critters.Where(b => b.name.StartsWith(BumblebeeSpawner.NamePrefix)).Select(b => b.Home).ToList();
        Vector3? pick = null;
        foreach (var spot in spots.OrderBy(s => Flat(s - player.position)))
            if (!taken.Any(t => Flat(t - spot) < 36f) && !beeHomes.Any(h => Flat(h - spot) < 64f)) { pick = spot; break; }
        if (pick == null) pick = spots.OrderBy(s => Flat(s - player.position)).Skip(Mathf.Min(3, spots.Count - 1)).First();
        Vector3 at = pick.Value + Vector3.up * .3f; // 꽃 송이 높이
        var go = new GameObject(Name);
        go.transform.SetParent(transform);
        go.transform.position = at;
        var model = FirstDayStudioAssets.Place(item.model, go.transform, at, .24f); // mantis-r199: .17은 기본 카메라에서 작았다
        go.transform.rotation = Quaternion.Euler(0f, 210f, 0f);
        go.AddComponent<SphereCollider>().isTrigger = true;
        var critter = go.AddComponent<BugCritter>();
        critter.Configure(item, .9f);
        critter.ConfigureSpecies(true, 2.2f, 1.3f);
        critter.UseOwnWings(); // 날개 축은 없다 — 놀라 날 때 몸 폭을 접지 않게
        go.AddComponent<MantisSway>().Body = model.transform;
        _bug = critter;
    }

    static float Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude; }
}

// 바람에 흔들리는 잎처럼: 몇 초 살랑(좌우 ±8°, 앞뒤 조금) → 잠깐 멈춤. 잡히거나 날아가는 중에는 멈춘다.
public sealed class MantisSway : MonoBehaviour
{
    public Transform Body;
    Quaternion _rest;
    BugCritter _critter;
    public static float LastAngle { get; private set; }

    void Start()
    {
        _critter = GetComponent<BugCritter>();
        if (Body != null) _rest = Body.localRotation;
    }

    void Update()
    {
        if (Body == null || _critter == null) return;
        if (_critter.Captured || _critter.Fleeing) { Body.localRotation = _rest; return; }
        float t = Time.time;
        float gust = Mathf.Clamp01(Mathf.Sin(t * .9f) * 1.6f + .6f); // 살랑이는 구간(약 3분의 2)과 잠깐 멈춘 구간
        float roll = Mathf.Sin(t * 7.5f) * 8f * gust, pitch = Mathf.Sin(t * 5.1f + 1f) * 3f * gust;
        Body.localRotation = Quaternion.Euler(pitch, 0f, roll) * _rest;
        LastAngle = Mathf.Max(LastAngle, Mathf.Abs(roll));
    }
}
