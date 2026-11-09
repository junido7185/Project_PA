using System.Collections.Generic;
using UnityEngine;

// 2026-11-09 화면 QA(qa232 붕어 화면): 고지대 연못 구덩이 안 서쪽 턱(수면 높이 땅)이 맨 주황 흙 판으로 보여 물가로 읽히지 않았다.
// 표시 전용 장식: 연못 칸에 맞닿은(2칸 안) 수면 높이 땅에 기존 풀 덤불·꽃 프리팹(FirstDayStudioAssets)을 고정 난수로 둔다.
// 지형·격자·점유·NavMesh·저장은 건드리지 않고, 놓은 장식의 충돌은 끈다.
public static class PondShoreDressing
{
    public static int Placed { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Placed = 0;

    public static void Build(Transform parent, FirstDayStudioAssets assets, WorldGridService grid, IReadOnlyCollection<Vector2Int> pond)
    {
        Placed = 0;
        if (parent == null || assets == null || grid == null || pond == null || pond.Count == 0 || assets.grasses == null || assets.grasses.Length == 0) return;
        int surface = int.MinValue;
        foreach (var c in pond) if (grid.TryGetCell(c, out var w)) surface = Mathf.Max(surface, w.WaterSurfaceLevel);
        // pond-r243: 2칸 반경으로는 턱 서쪽 끝이 주황으로 남았다 → 연못에서 시작해 수면 높이 이하의 맞닿은 땅을 이어서 찾는다(최대 40칸).
        var ledge = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        Vector2Int[] around = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var c in pond) foreach (var d in around) queue.Enqueue(c + d);
        while (queue.Count > 0 && ledge.Count < 40)
        {
            var k = queue.Dequeue();
            if (ledge.Contains(k) || !grid.TryGetCell(k, out var land) || land.HasWater || land.ElevationLevel > surface) continue;
            ledge.Add(k);
            foreach (var d in around) queue.Enqueue(k + d);
        }
        var root = new GameObject("PA_PondShoreDressing").transform;
        root.SetParent(parent, false);
        var random = new System.Random(9009 + ledge.Count);
        float half = grid.Definition.CellSize * .5f;
        // pond-r242: 풀 덤불만으로는 턱이 여전히 주황 흙 판이었다 → 턱 칸 위 1cm에 풀빛 덮개(표시 전용, 충돌 없음)를 깔고 덤불을 더 빽빽하게.
        var cover = CoverMaterial();
        foreach (var cell in ledge)
        {
            if (!grid.CellToWorld(cell, out var centre)) continue;
            if (cover != null && Ground(centre, out float gy))
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.name = "PondShoreGrass";
                Object.Destroy(tile.GetComponent<Collider>());
                tile.transform.SetParent(root, false);
                tile.transform.SetPositionAndRotation(new Vector3(centre.x, gy + .012f, centre.z), Quaternion.Euler(90f, 0f, 0f));
                tile.transform.localScale = new Vector3(half * 2f + .02f, half * 2f + .02f, 1f);
                var r = tile.GetComponent<MeshRenderer>();
                r.sharedMaterial = cover; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (assets.bushes != null && assets.bushes.Length > 0 && random.NextDouble() < .35)
            {
                var at = centre + new Vector3((float)(random.NextDouble() * 2 - 1) * half * .5f, 0f, (float)(random.NextDouble() * 2 - 1) * half * .5f);
                if (Ground(at, out float by)) { at.y = by; Put(assets.bushes[random.Next(assets.bushes.Length)], root, at, .55f, random.Next(360)); }
            }
            int tufts = 6 + random.Next(3);
            for (int i = 0; i < tufts; i++)
            {
                var at = centre + new Vector3((float)(random.NextDouble() * 2 - 1) * half * .85f, 0f, (float)(random.NextDouble() * 2 - 1) * half * .85f);
                if (!Ground(at, out float y)) continue;
                at.y = y;
                var prefab = assets.grasses[random.Next(assets.grasses.Length)];
                Put(prefab, root, at, .55f + (float)random.NextDouble() * .35f, random.Next(360));
            }
            if (assets.flowers != null && assets.flowers.Length > 0 && random.NextDouble() < .6)
            {
                var at = centre + new Vector3((float)(random.NextDouble() * 2 - 1) * half * .6f, 0f, (float)(random.NextDouble() * 2 - 1) * half * .6f);
                if (Ground(at, out float y)) { at.y = y; Put(assets.flowers[random.Next(assets.flowers.Length)], root, at, .35f, random.Next(360)); }
            }
        }
    }

    static Material s_cover;
    // 초원 풀빛(맑은 날 화면의 잔디 톤에 맞춤). URP Lit — 빌드에 늘 포함된다.
    static Material CoverMaterial()
    {
        if (s_cover != null) return s_cover;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return null;
        s_cover = new Material(shader) { name = "PA_PondShoreGrass (runtime)", hideFlags = HideFlags.DontSave };
        var green = new Color(.42f, .6f, .2f);
        if (s_cover.HasProperty("_BaseColor")) s_cover.SetColor("_BaseColor", green);
        if (s_cover.HasProperty("_Smoothness")) s_cover.SetFloat("_Smoothness", .05f);
        return s_cover;
    }

    static void Put(GameObject prefab, Transform root, Vector3 at, float height, float yaw)
    {
        if (prefab == null) return;
        var go = FirstDayStudioAssets.Place(prefab, root, at, height, yaw);
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        Placed++;
    }

    // 땅 높이: 위에서 내려 쏴 가장 위의 단단한 면(턱 위). 구덩이 안 턱이라 둘레 바위보다 낮다.
    static bool Ground(Vector3 at, out float y)
    {
        y = 0f;
        var hits = Physics.RaycastAll(new Vector3(at.x, 80f, at.z), Vector3.down, 160f, ~0, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0) return false;
        float best = float.MinValue;
        foreach (var h in hits) best = Mathf.Max(best, h.point.y);
        y = best;
        return true;
    }
}
