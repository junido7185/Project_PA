using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Canon v2 §6 / Dinkum ref 02·03·05·11·14: dense grass clumps with open walkable gaps, soft grass/sand edges,
// beach grass and visible shallow-water rocks/seaweed. GPU-instanced presentation only:
// no per-plant GameObjects, colliders, resources, NavMesh, occupancy or save state.
public sealed class CoastalVegetationField : MonoBehaviour
{
    const float ChunkMetres = 32f, DrawDistance = 110f, OccupancyDistance = 40f, OccupancyInterval = 1f;
    const float NoiseScale = .11f, EmptyGrassShare = .45f;
    // Verifier v6: budgets raised with the denser underwater caps so harbor-near land keeps its coverage.
    const int InstanceBudget = 36000, UnderwaterBudget = 14000, BatchLimit = 1023;
    // Critic v5: underwater dressing capped per 32 m chunk (not harbor-nearest globally) so every shore is dressed.
    // Verifier v6: 60 blades (~5-8 clumps) left the deep zone empty; ref 03 / trailer 0.667-1.417s carpet the seabed.
    const int SeaweedChunkCap = 150, RockChunkCap = 20, UnderwaterReach = 8;
    const int TintBase = 0, TintDry = 1, TintDeep = 2, TintSeaweed = 3, TintRock = 4, TintBeachRock = 5;
    const float TuftSpread = 1.3f; // x/z vs y: tufts read as wide fans (Dinkum ref 12·13·14)
    // Verifier v6: rocks read as narrow posts -> wide boulders, never taller than 0.9x their narrowest width.
    const float RockSpreadMin = 1.6f, RockSpreadMax = 2.2f, RockMaxAspect = .9f;
    // Verifier v7 (Dinkum ref 03·05·14): sand stays sand, dressed with dune-grass clumps (sand cells within
    // 10 cells of grass/water, low-frequency patches) and sparse small beach rocks.
    const float DuneNoiseScale = .17f, SpawnClear = 5f, SupplyClear = 3.5f, ScatterShadowMin = .2f;
    const int DuneReach = 10, DressDune = 1, DressScatter = 2;
    // Verifier v8 (Dinkum ref 02·14): v7 grew one uniform carpet on one side of the arrival. Patches now cover ~30% of
    // reach cells, a higher-frequency lane mask skips ~35% of patch cells, each 4x4-cell block keeps at most 6 dressed
    // cells, and a dressed cell holds 3-6 tufts in 2-4 sub-clusters (0.4-0.7 m) with open sand between them.
    const float DuneShare = .30f, DuneLaneScale = .45f, DuneLaneSkip = .35f;
    const float DuneClusterMin = .4f, DuneClusterMax = .7f, DuneClusterGap = .3f, DuneTuftHalf = .42f;
    const int DuneTuftsMin = 3, DuneTuftsMax = 6, DuneBlock = 4, DuneBlockCap = 6;
    // Arrival sand band: both sides of harbor.x within this radius get at least ArrivalMinCells dressed cells.
    const float ArrivalRadius = 24f;
    const int ArrivalMinCells = 6;
    static readonly int DuneMapId = Shader.PropertyToID("_PA_DuneMap"), DuneMapEnabledId = Shader.PropertyToID("_PA_DuneMapEnabled");
    static readonly int DuneMapSizeId = Shader.PropertyToID("_PA_DuneMapSize");
    const int DuneGroundSubdivisions = 4; // 0.5 m presentation tiles on the existing 2 m world grid.
    // Verifier v6: underwater dressing took the water's teal; HDR lift so green survives ~1-2 m of water tint.
    static readonly Color SeaweedLift = new Color(1.7f, 1.8f, 1.5f);
    static readonly Color[] Tints =
    {
        Color.white, new Color(.95f,.88f,.55f), new Color(.62f,.80f,.45f),
        // Rocks: HDR warm grey so lit tops read warm grey through the water tint (Dinkum ref 03), not teal glass.
        new Color(.30f,.55f,.42f), new Color(1.25f,1.18f,1.02f),
        // Beach rocks/shells on dry sand: pale warm stone, no water tint to compensate for.
        new Color(.95f,.90f,.82f)
    };
    // PA/StylizedFoliage root -> tip per tint (base, dry, deep, seaweed); replaces the dark untextured 'Green' sources.
    static readonly Color[] FoliageRoot =
    {
        new Color(.36f,.52f,.18f), new Color(.44f,.54f,.19f), new Color(.28f,.48f,.16f), new Color(.10f,.30f,.12f)
    };
    // Seaweed reads bright green (not water-blue or dark scratches) through the water tint (Dinkum ref 03·05·11).
    static readonly Color[] FoliageTip =
    {
        // Lead v3 (verifier: straw on lime ground read as dead stalks): tips lighter and greener than the ground.
        new Color(.74f,.95f,.36f), new Color(.86f,.95f,.44f), new Color(.58f,.86f,.30f), new Color(.45f,.82f,.30f)
    };
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"), ColorId = Shader.PropertyToID("_Color");
    static readonly int RootColorId = Shader.PropertyToID("_RootColor"), TipColorId = Shader.PropertyToID("_TipColor"),
        GradientHeightId = Shader.PropertyToID("_GradientHeight"), WindId = Shader.PropertyToID("_Wind"),
        WindSpeedId = Shader.PropertyToID("_WindSpeed"), TranslucencyId = Shader.PropertyToID("_Translucency");

    struct Part { public Mesh Mesh; public int Submesh; public Material Source; public Matrix4x4 Local; }
    // Prefab normalised like FirstDayStudioAssets.Place(): root rotation/scale basis, bounds bottom and height.
    sealed class Model { public Part[] Parts; public float MinY, SizeY, Radius, Width; }
    struct Placement
    {
        public Vector3 Position; public Vector2Int Cell; public int Model, Tint, Order, Dressing;
        public float Height, Yaw, Priority, Spread; public bool Underwater, Bush, NoShadow;
    }
    // Dune placements are generated contiguously per cell and keep the same priority; ByPriority preserves the group.
    struct LandGroup { public int Start, Count; }
    struct Instance
    {
        public Matrix4x4 Root; public Vector3 Position; public Vector2Int Cell; public int[] Batches;
        public int Model; public bool Underwater, Visible, Covered;
    }
    sealed class Batch
    {
        public Mesh Mesh; public int Submesh; public Material Material; public ShadowCastingMode Shadows;
        public Matrix4x4[] Matrices; public int Count;
    }
    sealed class Chunk { public Bounds Bounds; public Instance[] Instances; public Batch[] Batches; }

    readonly List<Model> _models = new List<Model>();
    readonly Dictionary<GameObject, int> _modelIndex = new Dictionary<GameObject, int>();
    readonly Dictionary<(Material, int), Material> _materials = new Dictionary<(Material, int), Material>();
    readonly Dictionary<(int, bool), Material> _foliageVariants = new Dictionary<(int, bool), Material>();
    Material _foliage;
    int _seaweed, _rocks, _shoreChunks, _landCandidates, _underwaterCandidates;
    int _duneCells, _duneCandidates, _duneKept, _scatterCandidates, _scatterKept;
    int _duneSubclusters, _duneLaneSkipped, _duneBlockCapped, _duneArrivalLeft, _duneArrivalRight, _duneArrivalPromoted, _duneMaskCells;
    int _landCandidateChunks, _landKeptChunks, _duneCandidateChunks, _duneKeptChunks, _duneKeptCells;
    int _duneArrivalKeptLeft, _duneArrivalKeptRight;
    float[] _duneMask;
    Color32[] _duneGroundPixels;
    Texture2D _duneMap;
    WorldGridService _grid;
    Transform _player;
    Chunk[] _chunks = System.Array.Empty<Chunk>();
    bool[] _baseWalkable, _basePath;
    int _width, _height;
    float _occupancyAt;

    public int LandInstanceCount { get; private set; }
    public int UnderwaterInstanceCount { get; private set; }

    public void Configure(WorldGridService grid, WorldGenerationResult generated, Transform player, Vector3 harbor, FirstDayStudioAssets assets)
    {
        _grid = grid; _player = player;
        if (grid == null || generated == null || assets == null) return;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        // Missing/unsupported foliage shader keeps the legacy tinted source copies.
        var foliage = Resources.Load<Material>("WorldArt/PA_Foliage");
        _foliage = foliage != null && foliage.shader != null && foliage.shader.isSupported ? foliage : null;
        Build(generated, harbor, assets);
        PublishDuneMap();
        int batches = 0;
        foreach (var chunk in _chunks) batches += chunk.Batches.Length;
        Debug.Log($"[CoastalVegetationField] land={LandInstanceCount}/{_landCandidates} underwater={UnderwaterInstanceCount}/{_underwaterCandidates} " +
                  $"seaweed={_seaweed} rocks={_rocks} shoreChunks={_shoreChunks} " +
                  $"beach=dune{_duneKept}/{_duneCandidates}(cells{_duneCells} sub{_duneSubclusters} laneSkipped{_duneLaneSkipped} " +
                  $"blockCapped{_duneBlockCapped} arrivalL{_duneArrivalLeft}/R{_duneArrivalRight} promoted{_duneArrivalPromoted} " +
                  $"mask{_duneMaskCells} selectedCells{_duneKeptCells} selectedArrivalL{_duneArrivalKeptLeft}/R{_duneArrivalKeptRight}) " +
                  $"scatter{_scatterKept}/{_scatterCandidates} landChunks{_landKeptChunks}/{_landCandidateChunks} " +
                  $"duneChunks{_duneKeptChunks}/{_duneCandidateChunks} " +
                  $"caps=weed{SeaweedChunkCap}/rock{RockChunkCap} budget={InstanceBudget}/{UnderwaterBudget} " +
                  $"chunks={_chunks.Length} batches={batches} materials={_materials.Count} foliage={(_foliage != null ? _foliageVariants.Count.ToString() : "fallback")} " +
                  $"build={watch.ElapsedMilliseconds}ms");
    }

    // Shop footprint etc.: permanently hide presentation instances inside the covered ground.
    public void HideWithin(System.Func<Vector3, bool> covers)
    {
        if (covers == null) return;
        foreach (var chunk in _chunks)
        {
            bool changed = false;
            for (int i = 0; i < chunk.Instances.Length; i++)
            {
                ref var instance = ref chunk.Instances[i];
                if (instance.Covered || !covers(instance.Position)) continue;
                instance.Covered = true; changed = true;
            }
            if (changed) Rebuild(chunk);
        }
    }

    void Build(WorldGenerationResult generated, Vector3 harbor, FirstDayStudioAssets assets)
    {
        var definition = _grid.Definition;
        _width = definition.Width; _height = definition.Height;
        float cellSize = definition.CellSize;
        int total = _width * _height;
        var cells = new WorldCellData[total];
        var valid = new bool[total];
        _baseWalkable = new bool[total]; _basePath = new bool[total];
        for (int z = 0; z < _height; z++)
        for (int x = 0; x < _width; x++)
        {
            int i = z * _width + x;
            valid[i] = _grid.TryGetCell(new Vector2Int(x, z), out cells[i]);
            _baseWalkable[i] = valid[i] && cells[i].IsWalkable;
            _basePath[i] = valid[i] && cells[i].HasPath;
        }
        var waterDistance = Distances(cells, valid, false); // water cells: steps from land
        var landDistance = Distances(cells, valid, true);   // land cells: steps from water
        var resources = new HashSet<Vector2Int>();
        foreach (var spawn in generated.ResourceSpawns) resources.Add(spawn.Coordinate);
        var anchors = generated.Anchors;
        var random = new System.Random(9101);
        float patchX = random.Next(1000, 9000) + .31f, patchZ = random.Next(1000, 9000) + .67f;
        float detailX = random.Next(1000, 9000) + .13f, detailZ = random.Next(1000, 9000) + .89f;
        float tintX = random.Next(1000, 9000) + .53f, tintZ = random.Next(1000, 9000) + .29f;
        float weedX = random.Next(1000, 9000) + .71f, weedZ = random.Next(1000, 9000) + .47f;
        Vector3 supply = harbor + new Vector3(2.8f, 0, 3);
        Vector3 boat = harbor + new Vector3(-6, 0, -8);

        bool InHarborLane(Vector3 p) => Mathf.Abs(p.x - harbor.x) < 3.5f && p.z > harbor.z - 8 && p.z < harbor.z + 85;
        float Patch(int x, int z) => Mathf.PerlinNoise(x * NoiseScale + patchX, z * NoiseScale + patchZ) * .75f +
                                     Mathf.PerlinNoise(x * NoiseScale * 2.7f + detailX, z * NoiseScale * 2.7f + detailZ) * .25f;
        // -1 excluded, 0 open land, 1 inside a Start/Shop clearing (sparse short tufts only).
        int LandClass(int i, Vector2Int c)
        {
            var cell = cells[i];
            if (!cell.IsWalkable || cell.HasWater || cell.HasPath || resources.Contains(c) ||
                !_grid.CellToWorld(c, out var p) || InHarborLane(p) || Flat(p - supply).magnitude <= 6f) return -1;
            // Same clearing rule as ComposeDressing: settlement anchors wide, activity anchors close.
            int result = 0;
            foreach (var anchor in anchors)
            {
                bool settlement = anchor.Kind == WorldGenerationAnchorKind.Start || anchor.Kind == WorldGenerationAnchorKind.Shop;
                if (Vector2Int.Distance(anchor.Coordinate, c) >= (settlement ? 9 : 4)) continue;
                if (!settlement) return -1;
                result = 1;
            }
            return result;
        }
        bool EligibleLand(int i, Vector2Int c) => LandClass(i, c) == 0;
        bool NearGround(int x, int z, WorldGroundType a, WorldGroundType b)
        {
            for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx, nz = z + dz;
                if ((dx == 0 && dz == 0) || nx < 0 || nz < 0 || nx >= _width || nz >= _height) continue;
                int n = nz * _width + nx;
                if (valid[n] && !cells[n].HasWater && (cells[n].GroundType == a || cells[n].GroundType == b)) return true;
            }
            return false;
        }

        // Pass 1: patch value per eligible grass cell; the lowest ~45% stay open ground.
        var value = new float[total];
        var edge = new bool[total];
        var grassValues = new List<float>();
        for (int z = 0; z < _height; z++)
        for (int x = 0; x < _width; x++)
        {
            int i = z * _width + x;
            value[i] = -1;
            if (!valid[i] || cells[i].GroundType != WorldGroundType.Grass || !EligibleLand(i, new Vector2Int(x, z))) continue;
            edge[i] = NearGround(x, z, WorldGroundType.Sand, WorldGroundType.Soil);
            value[i] = Patch(x, z) + (edge[i] ? .1f : 0);
            grassValues.Add(value[i]);
        }
        grassValues.Sort();
        float threshold = grassValues.Count > 0 ? grassValues[Mathf.Min(grassValues.Count - 1, (int)(grassValues.Count * EmptyGrassShare))] : float.MaxValue;
        float densest = grassValues.Count > 0 ? grassValues[grassValues.Count - 1] : 1;

        // Pass 2: land clumps, flowers on grass/sand edges, sparse dry beach grass, short tufts in Start/Shop clearings.
        var land = new List<Placement>();
        int order = 0;
        for (int z = 0; z < _height; z++)
        for (int x = 0; x < _width; x++)
        {
            int i = z * _width + x;
            var c = new Vector2Int(x, z);
            bool grass = value[i] >= 0 && value[i] >= threshold;
            bool sand = !grass && valid[i] && cells[i].GroundType == WorldGroundType.Sand &&
                        NearGround(x, z, WorldGroundType.Grass, WorldGroundType.Grass) && EligibleLand(i, c);
            bool clearing = !grass && !sand && valid[i] && cells[i].GroundType == WorldGroundType.Grass && LandClass(i, c) == 1;
            if ((!grass && !sand && !clearing) || !_grid.CellToWorld(c, out var ground)) continue;
            float priority = Flat(ground - harbor).magnitude - (landDistance[i] >= 0 && landDistance[i] <= 6 ? 60 : 0);
            float tone = Mathf.PerlinNoise(x * NoiseScale * .55f + tintX, z * NoiseScale * .55f + tintZ);
            int tint = tone < .4f ? TintDry : tone > .6f ? TintDeep : TintBase;
            if (grass)
            {
                float t = Mathf.InverseLerp(threshold, densest, value[i]);
                int tufts = 3 + Mathf.RoundToInt(t * 4) + (edge[i] ? 2 : 0);
                Vector3 centre = Jitter(random, cellSize * .22f);
                for (int k = 0; k < tufts; k++)
                {
                    var offset = ClampInCell(centre + Jitter(random, cellSize * .3f), cellSize * .46f);
                    Add(land, assets.grasses, random, ground + offset, c, Lerp(random, .40f, .75f),
                        random.NextDouble() < .15 ? TintBase : tint, false, priority, ref order, TuftSpread);
                }
                if (edge[i] && random.NextDouble() < .3)
                    Add(land, assets.flowers, random, ground + ClampInCell(Jitter(random, cellSize * .4f), cellSize * .46f), c,
                        Lerp(random, .3f, .45f), TintBase, false, priority, ref order);
                if (t > .7f && random.NextDouble() < .16)
                    Add(land, assets.bushes, random, ground + ClampInCell(centre, cellSize * .3f), c,
                        Lerp(random, .4f, .7f), tint, false, priority, ref order, 1, true);
            }
            else if (sand)
            {
                if (random.NextDouble() > .15 + .3 * Patch(x, z)) continue;
                int tufts = 1 + random.Next(3);
                for (int k = 0; k < tufts; k++)
                    Add(land, assets.grasses, random, ground + ClampInCell(Jitter(random, cellSize * .4f), cellSize * .46f), c,
                        Lerp(random, .35f, .6f), TintDry, false, priority, ref order, TuftSpread);
            }
            else
            {
                // Settlement clearing stays walkable-open but not bare: 25% of cells, 1-2 short tufts.
                if (random.NextDouble() >= .25) continue;
                int tufts = 1 + random.Next(2);
                for (int k = 0; k < tufts; k++)
                    Add(land, assets.grasses, random, ground + ClampInCell(Jitter(random, cellSize * .35f), cellSize * .46f), c,
                        Lerp(random, .25f, .4f), tint, false, priority, ref order, TuftSpread);
            }
        }

        // Pass 3: water 1-8 cells from land. Rocks/seaweed stand on the shelf-profile visual seabed
        // (WorldSurfaceArtProjection.SeabedOffset, cell centre ≈ corner distance - 0.5).
        // Critic v5 (Dinkum ref 03·11, trailer 0.667-1.417s): wide bright seaweed fans and lit grey rock clusters
        // reach into deep water, sparser with distance instead of stopping at 3 cells.
        var underwater = new List<Placement>();
        for (int z = 0; z < _height; z++)
        for (int x = 0; x < _width; x++)
        {
            int i = z * _width + x;
            int distance = waterDistance[i];
            var c = new Vector2Int(x, z);
            if (!valid[i] || !cells[i].HasWater || distance < 1 || distance > UnderwaterReach || !_grid.CellToWorld(c, out var centre)) continue;
            if (InHarborLane(centre) || Flat(centre - boat).magnitude < 7f) continue;
            float surface = WorldChunkMeshBuilder.OpeningWaterHeight(definition, cells[i]);
            // Lead: terrace profile -> cell-centre seabed matches the visual mesh (corner distances k-1..k).
            float seabed = surface - definition.ElevationStep - WorldSurfaceArtProjection.CellSeabedOffset(distance);
            float depth = surface - seabed;
            float weed = Mathf.PerlinNoise(x * NoiseScale + weedX, z * NoiseScale + weedZ);
            float weedThreshold = distance <= 3 ? .42f : distance <= 5 ? .48f : .55f;
            if (weed > weedThreshold)
            {
                // One clump shares a random priority (shallow slightly first) so the per-chunk cap thins evenly.
                float priority = (float)random.NextDouble() + distance * .08f;
                int blades = 6 + Mathf.RoundToInt(Mathf.InverseLerp(weedThreshold, .8f, weed) * 4);
                Vector3 clump = Jitter(random, cellSize * .2f);
                for (int k = 0; k < blades; k++)
                {
                    var offset = ClampInCell(clump + Jitter(random, cellSize * .22f), cellSize * .46f);
                    // Tops stay at least 0.15m below the water surface; no room -> no blade.
                    float height = Mathf.Min(Lerp(random, .6f, 1.1f), depth - .15f);
                    if (height < .25f) continue;
                    Add(underwater, assets.grasses, random, new Vector3(centre.x + offset.x, seabed, centre.z + offset.z), c,
                        height, TintSeaweed, true, priority, ref order, Lerp(random, 1.4f, 1.6f));
                }
            }
            if (random.NextDouble() < (distance <= 2 ? .25 : distance <= 5 ? .15 : .1))
            {
                float priority = (float)random.NextDouble() + distance * .08f;
                int rocks = 1 + (random.NextDouble() < .45 ? 1 : 0) + (random.NextDouble() < .2 ? 1 : 0);
                Vector3 cluster = Jitter(random, cellSize * .2f);
                // Rocks sink 10% into the bed; tops stay at least 0.2m below the water surface.
                float fit = (depth - .2f) / .9f;
                for (int k = 0; k < rocks; k++)
                {
                    float size = Lerp(random, .6f, 1.4f) * (k == 0 ? 1 : .75f);
                    float height = Mathf.Min(size, fit);
                    var offset = ClampInCell(cluster + Jitter(random, cellSize * .3f), cellSize * .4f);
                    if (height < .3f) continue;
                    // Wide boulders; depth-limited rocks lose height but keep the full-size width (flat lit tops, Dinkum ref 03).
                    float spread = Lerp(random, RockSpreadMin, RockSpreadMax) * size / height;
                    Add(underwater, assets.rocks, random, new Vector3(centre.x + offset.x, seabed - .1f * height, centre.z + offset.z), c,
                        height, TintRock, true, priority, ref order, spread, maxAspect: RockMaxAspect);
                }
            }
        }

        // Pass 4 (verifier v7/v8): beach dressing on walkable sand; ground types/cell data unchanged. Own random stream
        // and noise offsets so passes 1-3 stay identical. Dune grass on the top ~30% of a low-frequency patch value,
        // broken by lanes and a per-block cap into separate tuft groups; sparse small rocks denser at the waterline.
        // Arrival spawn and supply box stay open.
        var beachRandom = new System.Random(9203);
        float duneX = beachRandom.Next(1000, 9000) + .37f, duneZ = beachRandom.Next(1000, 9000) + .61f;
        float duneDetailX = beachRandom.Next(1000, 9000) + .19f, duneDetailZ = beachRandom.Next(1000, 9000) + .83f;
        float laneX = beachRandom.Next(1000, 9000) + .43f, laneZ = beachRandom.Next(1000, 9000) + .07f;
        float Lane(int i) => Mathf.PerlinNoise(i % _width * DuneLaneScale + laneX, i / _width * DuneLaneScale + laneZ);
        Vector3 arrivalSpawn = harbor + new Vector3(0, 0, 2);
        var grassDistance = GroundDistances(cells, valid, WorldGroundType.Grass);
        bool BeachClear(Vector3 p) => Flat(p - arrivalSpawn).magnitude > SpawnClear && Flat(p - supply).magnitude > SupplyClear;
        bool BeachCell(int i, Vector2Int c, out Vector3 p)
        {
            p = default;
            var cell = cells[i];
            if (!valid[i] || cell.GroundType != WorldGroundType.Sand || !cell.IsWalkable || cell.HasWater || cell.HasPath ||
                resources.Contains(c) || !_grid.CellToWorld(c, out p) || InHarborLane(p) || !BeachClear(p)) return false;
            // Same anchor clearings as LandClass (settlement wide, activity close).
            foreach (var anchor in anchors)
            {
                bool settlement = anchor.Kind == WorldGenerationAnchorKind.Start || anchor.Kind == WorldGenerationAnchorKind.Shop;
                if (Vector2Int.Distance(anchor.Coordinate, c) < (settlement ? 9 : 4)) return false;
            }
            return true;
        }
        var beach = new List<(int Index, Vector3 Ground, float Dune)>();
        var duneValues = new List<float>();
        for (int z = 0; z < _height; z++)
        for (int x = 0; x < _width; x++)
        {
            int i = z * _width + x;
            if (!BeachCell(i, new Vector2Int(x, z), out var ground)) continue;
            bool reach = grassDistance[i] >= 0 && grassDistance[i] <= DuneReach || landDistance[i] >= 0 && landDistance[i] <= DuneReach;
            float dune = -1;
            if (reach)
            {
                dune = Mathf.PerlinNoise(x * DuneNoiseScale + duneX, z * DuneNoiseScale + duneZ) * .8f +
                       Mathf.PerlinNoise(x * DuneNoiseScale * 2.3f + duneDetailX, z * DuneNoiseScale * 2.3f + duneDetailZ) * .2f;
                duneValues.Add(dune);
            }
            beach.Add((i, ground, dune));
        }
        duneValues.Sort();
        float duneThreshold = duneValues.Count > 0 ? duneValues[Mathf.Min(duneValues.Count - 1, (int)(duneValues.Count * (1 - DuneShare)))] : float.MaxValue;
        float duneTop = duneValues.Count > 0 ? duneValues[duneValues.Count - 1] : 1;
        // Lanes: the lowest ~35% of a higher-frequency mask inside the patches stays open sand.
        var laneValues = new List<float>();
        foreach (var (i, _, dune) in beach)
            if (dune >= 0 && dune >= duneThreshold) laneValues.Add(Lane(i));
        laneValues.Sort();
        float laneThreshold = laneValues.Count > 0 ? laneValues[Mathf.Min(laneValues.Count - 1, (int)(laneValues.Count * DuneLaneSkip))] : 0;
        // Strongest patch cells first; each 4x4-cell block keeps at most DuneBlockCap so patches spread over the beach.
        // strength: 0 undressed, 0.6-1 dressed (patch value), shared with the dune ground mask.
        var strength = new float[total];
        int blocksX = (_width + DuneBlock - 1) / DuneBlock;
        var blockCounts = new int[blocksX * ((_height + DuneBlock - 1) / DuneBlock)];
        int BlockOf(int i) => i / _width / DuneBlock * blocksX + i % _width / DuneBlock;
        var patch = new List<(int Index, float Dune)>();
        _duneLaneSkipped = _duneBlockCapped = 0;
        foreach (var (i, _, dune) in beach)
        {
            if (dune < 0 || dune < duneThreshold) continue;
            if (Lane(i) < laneThreshold) { _duneLaneSkipped++; continue; }
            patch.Add((i, dune));
        }
        patch.Sort((a, b) => a.Dune != b.Dune ? b.Dune.CompareTo(a.Dune) : a.Index.CompareTo(b.Index));
        foreach (var (i, dune) in patch)
        {
            int block = BlockOf(i);
            if (blockCounts[block] >= DuneBlockCap) { _duneBlockCapped++; continue; }
            blockCounts[block]++;
            strength[i] = Mathf.Lerp(.6f, 1f, Mathf.InverseLerp(duneThreshold, duneTop, dune));
        }
        // Arrival band: each side of harbor.x gets at least ArrivalMinCells dressed reach cells, promoting the best
        // remaining ones (lane cells last), still under the block cap.
        _duneArrivalPromoted = 0;
        for (int side = 0; side < 2; side++)
        {
            var arrival = new List<(int Index, float Dune, bool Lane)>();
            int dressed = 0;
            foreach (var (i, ground, dune) in beach)
            {
                if (dune < 0 || (ground.x < harbor.x ? 0 : 1) != side || Flat(ground - harbor).magnitude > ArrivalRadius) continue;
                if (strength[i] > 0) dressed++;
                else arrival.Add((i, dune, Lane(i) < laneThreshold));
            }
            arrival.Sort((a, b) => a.Lane != b.Lane ? a.Lane.CompareTo(b.Lane) : a.Dune != b.Dune ? b.Dune.CompareTo(a.Dune) : a.Index.CompareTo(b.Index));
            foreach (var (i, _, _) in arrival)
            {
                if (dressed >= ArrivalMinCells) break;
                int block = BlockOf(i);
                if (blockCounts[block] >= DuneBlockCap) continue;
                blockCounts[block]++; dressed++; _duneArrivalPromoted++;
                strength[i] = .6f;
            }
            if (side == 0) _duneArrivalLeft = dressed; else _duneArrivalRight = dressed;
        }
        _duneCells = _duneSubclusters = 0;
        var clusterCentres = new Vector3[4];
        var clusterRadii = new float[4];
        foreach (var (i, ground, _) in beach)
        {
            var c = new Vector2Int(i % _width, i / _width);
            // Same promotion as land: coast-adjacent sand and anything within 60 m of the harbor first.
            float harborDistance = Flat(ground - harbor).magnitude;
            bool coastal = landDistance[i] >= 0 && landDistance[i] <= 6;
            float priority = harborDistance - (coastal || harborDistance <= 60 ? 60 : 0);
            if (strength[i] > 0)
            {
                _duneCells++;
                int tufts = DuneTuftsMin + Mathf.RoundToInt((strength[i] - .6f) / .4f * (DuneTuftsMax - DuneTuftsMin));
                // 2-4 sub-cluster discs with at least DuneClusterGap of open sand between them; ones that don't fit are dropped.
                int wanted = Mathf.Min(tufts, 2 + beachRandom.Next(3)), clusters = 0;
                for (int attempt = 0; attempt < 16 && clusters < wanted; attempt++)
                {
                    float radius = Lerp(beachRandom, DuneClusterMin, DuneClusterMax);
                    var centre = Jitter(beachRandom, cellSize * .5f - radius * .5f);
                    bool open = true;
                    for (int k = 0; k < clusters && open; k++)
                        open = Flat(centre - clusterCentres[k]).magnitude >= radius + clusterRadii[k] + DuneClusterGap;
                    if (!open) continue;
                    clusterCentres[clusters] = centre; clusterRadii[clusters++] = radius;
                }
                _duneSubclusters += clusters;
                for (int k = 0; k < tufts; k++)
                {
                    // Round-robin so every sub-cluster holds at least one tuft; kept inside the cell with a margin
                    // so neighbouring dressed cells don't merge into a carpet.
                    int cluster = k % clusters;
                    var offset = clusterCentres[cluster] + Jitter(beachRandom, clusterRadii[cluster] * .75f);
                    var position = ground + ClampInCell(offset, cellSize * DuneTuftHalf);
                    float height = Lerp(beachRandom, .45f, .8f), spread = Lerp(beachRandom, 1.3f, 1.5f);
                    if (!BeachClear(position)) continue;
                    Add(land, assets.grasses, beachRandom, position, c, height, TintDry, false, priority, ref order, spread,
                        dressing: DressDune);
                }
            }
            if (beachRandom.NextDouble() < (landDistance[i] >= 0 && landDistance[i] <= 2 ? .12 : .035))
            {
                int stones = 1 + (beachRandom.NextDouble() < .35 ? 1 : 0);
                for (int k = 0; k < stones; k++)
                {
                    var position = ground + ClampInCell(Jitter(beachRandom, cellSize * .4f), cellSize * .46f);
                    float height = Lerp(beachRandom, .12f, .28f), spread = Lerp(beachRandom, 1.2f, 1.6f);
                    if (!BeachClear(position)) continue;
                    Add(land, assets.rocks, beachRandom, position, c, height, TintBeachRock, false, priority, ref order, spread,
                        noShadow: height < ScatterShadowMin, dressing: DressScatter);
                }
            }
        }

        // Underwater retains its existing capped selection. Land keeps whole dune-cell groups and a fair
        // per-chunk share before using harbor priority for the remaining budget. Coverage is budget-dependent.
        underwater.Sort(ByPriority); land.Sort(ByPriority);
        _landCandidates = land.Count; _underwaterCandidates = underwater.Count;
        // Underwater: per-chunk seaweed/rock caps so every shore keeps coverage; the global safety cap then
        // thins evenly (random clump priority), never by harbor distance.
        var weedCounts = new Dictionary<Vector2Int, int>();
        var rockCounts = new Dictionary<Vector2Int, int>();
        int kept = 0;
        for (int i = 0; i < underwater.Count; i++)
        {
            bool seaweed = underwater[i].Tint == TintSeaweed;
            var counts = seaweed ? weedCounts : rockCounts;
            var key = ChunkKey(underwater[i].Position);
            counts.TryGetValue(key, out int count);
            if (count >= (seaweed ? SeaweedChunkCap : RockChunkCap)) continue;
            counts[key] = count + 1;
            underwater[kept++] = underwater[i];
        }
        underwater.RemoveRange(kept, underwater.Count - kept);
        UnderwaterInstanceCount = Mathf.Min(underwater.Count, UnderwaterBudget);
        _seaweed = _rocks = 0;
        var shoreChunks = new HashSet<Vector2Int>();
        for (int i = 0; i < UnderwaterInstanceCount; i++)
        {
            if (underwater[i].Tint == TintSeaweed) _seaweed++; else _rocks++;
            shoreChunks.Add(ChunkKey(underwater[i].Position));
        }
        _shoreChunks = shoreChunks.Count;
        var landSelected = SelectLand(land, InstanceBudget - UnderwaterInstanceCount, out int landKept);
        LandInstanceCount = landKept;
        _duneCandidates = _duneKept = _scatterCandidates = _scatterKept = 0;
        var duneShown = new bool[total];
        _duneKeptCells = _duneArrivalKeptLeft = _duneArrivalKeptRight = 0;
        for (int i = 0; i < land.Count; i++)
        {
            bool inBudget = landSelected[i];
            if (land[i].Dressing == DressDune)
            {
                _duneCandidates++;
                if (!inBudget) continue;
                _duneKept++;
                int cellIndex = land[i].Cell.y * _width + land[i].Cell.x;
                if (!duneShown[cellIndex])
                {
                    duneShown[cellIndex] = true; _duneKeptCells++;
                    if (_grid.CellToWorld(land[i].Cell, out var centre) && Flat(centre - harbor).magnitude <= ArrivalRadius)
                    {
                        if (centre.x < harbor.x) _duneArrivalKeptLeft++; else _duneArrivalKeptRight++;
                    }
                }
            }
            else if (land[i].Dressing == DressScatter) { _scatterCandidates++; if (inBudget) _scatterKept++; }
        }
        // Dune ground mask: patch strength under dressed cells whose tufts survived the budget, 0.5 on dry neighbours.
        _duneMask = new float[total];
        _duneMaskCells = 0;
        for (int z = 0; z < _height; z++)
        for (int x = 0; x < _width; x++)
        {
            int i = z * _width + x;
            if (duneShown[i]) { _duneMask[i] = strength[i]; _duneMaskCells++; continue; }
            if (!valid[i] || cells[i].HasWater) continue;
            for (int n = 0; n < 9 && _duneMask[i] == 0; n++)
            {
                int nx = x + n % 3 - 1, nz = z + n / 3 - 1;
                if (nx >= 0 && nz >= 0 && nx < _width && nz < _height && duneShown[nz * _width + nx]) _duneMask[i] = .5f;
            }
        }
        // The coarse neighbour skirt made rectangular turf panels. Paint only the footprints of
        // retained tufts, on smaller snapped tiles, with a narrow soil shoulder around each group.
        // This is a visual map: selection, instance count, grid cells, paths and saves stay unchanged.
        int groundWidth = _width * DuneGroundSubdivisions;
        int groundHeight = _height * DuneGroundSubdivisions;
        float groundTile = cellSize / DuneGroundSubdivisions;
        Vector3 groundOrigin = _grid.Definition.WorldOrigin - new Vector3(cellSize * .5f, 0, cellSize * .5f);
        _duneGroundPixels = new Color32[groundWidth * groundHeight];
        var groundEligible = new bool[total];
        foreach (var entry in beach) groundEligible[entry.Index] = true;
        for (int p = 0; p < land.Count; p++)
        {
            if (!landSelected[p] || land[p].Dressing != DressDune) continue;
            var tuft = land[p];
            var model = _models[tuft.Model];
            float radius = Mathf.Clamp(model.Radius / Mathf.Max(model.SizeY, .001f) * tuft.Height * tuft.Spread, .45f, .85f) + .3f;
            float shoulder = radius + groundTile * .7f;
            float px = (tuft.Position.x - groundOrigin.x) / groundTile;
            float pz = (tuft.Position.z - groundOrigin.z) / groundTile;
            int loX = Mathf.Max(0, Mathf.FloorToInt(px - shoulder / groundTile));
            int hiX = Mathf.Min(groundWidth - 1, Mathf.FloorToInt(px + shoulder / groundTile));
            int loZ = Mathf.Max(0, Mathf.FloorToInt(pz - shoulder / groundTile));
            int hiZ = Mathf.Min(groundHeight - 1, Mathf.FloorToInt(pz + shoulder / groundTile));
            for (int z = loZ; z <= hiZ; z++)
            for (int x = loX; x <= hiX; x++)
            {
                int cx = x / DuneGroundSubdivisions, cz = z / DuneGroundSubdivisions;
                int ci = cz * _width + cx;
                if (!groundEligible[ci]) continue;
                Vector3 center = groundOrigin + new Vector3((x + .5f) * groundTile, 0, (z + .5f) * groundTile);
                if (!BeachClear(center)) continue;
                float lx = (x % DuneGroundSubdivisions + .5f) * groundTile;
                float lz = (z % DuneGroundSubdivisions + .5f) * groundTile;
                // Keep a half-metre wet-sand strip instead of excluding the whole shoreline cell.
                if (cx > 0 && cells[ci - 1].HasWater && lx < .5f ||
                    cx + 1 < _width && cells[ci + 1].HasWater && cellSize - lx < .5f ||
                    cz > 0 && cells[ci - _width].HasWater && lz < .5f ||
                    cz + 1 < _height && cells[ci + _width].HasWater && cellSize - lz < .5f) continue;
                float distance = Flat(center - tuft.Position).sqrMagnitude;
                if (distance > shoulder * shoulder) continue;
                int pixel = z * groundWidth + x;
                var colour = _duneGroundPixels[pixel];
                colour.r = 255;
                if (distance <= radius * radius) colour.g = 255;
                colour.a = 255;
                _duneGroundPixels[pixel] = colour;
            }
        }
        var groups = new Dictionary<Vector2Int, List<Placement>>();
        void Group(List<Placement> source, int count, bool[] selected = null)
        {
            for (int i = 0; i < count; i++)
            {
                if (selected != null && !selected[i]) continue;
                var p = source[i];
                var key = ChunkKey(p.Position);
                if (!groups.TryGetValue(key, out var list)) groups.Add(key, list = new List<Placement>());
                list.Add(p);
            }
        }
        Group(land, land.Count, landSelected);
        Group(underwater, UnderwaterInstanceCount);
        var chunks = new List<Chunk>(groups.Count);
        foreach (var group in groups.Values) chunks.Add(CreateChunk(group));
        _chunks = chunks.ToArray();
    }

    // Build-time only: reserve one preferred whole dune group per allocation chunk when their total cost fits,
    // then distribute remaining capacity equally, dunes first. Spare capacity returns to harbor/coast priority. A dune cell is
    // accepted whole or skipped, so neither the fair pass nor the remainder can cut a group at the budget edge.
    // If the budget cannot cover one group per chunk, coverage remains best effort; no budget is increased.
    bool[] SelectLand(List<Placement> land, int budget, out int selectedCount)
    {
        var selected = new bool[land.Count];
        var groups = new List<LandGroup>();
        var byChunk = new Dictionary<Vector2Int, List<int>>();
        var candidateChunks = new HashSet<Vector2Int>();
        var duneChunks = new HashSet<Vector2Int>();
        for (int start = 0; start < land.Count;)
        {
            int count = 1;
            if (land[start].Dressing == DressDune)
                while (start + count < land.Count && land[start + count].Dressing == DressDune &&
                       land[start + count].Cell == land[start].Cell) count++;
            int groupIndex = groups.Count;
            groups.Add(new LandGroup { Start = start, Count = count });
            var key = ChunkKey(land[start].Position);
            if (!byChunk.TryGetValue(key, out var indices)) byChunk.Add(key, indices = new List<int>());
            indices.Add(groupIndex);
            for (int p = start; p < start + count; p++)
            {
                var actualKey = ChunkKey(land[p].Position);
                candidateChunks.Add(actualKey);
                if (land[p].Dressing == DressDune) duneChunks.Add(actualKey);
            }
            start += count;
        }
        _landCandidateChunks = candidateChunks.Count; _duneCandidateChunks = duneChunks.Count;
        budget = Mathf.Clamp(budget, 0, land.Count);
        selectedCount = 0;
        var keys = new List<Vector2Int>(byChunk.Keys);
        keys.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        var groupSelected = new bool[groups.Count];
        int coverageCost = 0;
        for (int chunk = 0; chunk < keys.Count; chunk++)
        {
            var indices = byChunk[keys[chunk]];
            indices.Sort((a, b) =>
            {
                bool duneA = land[groups[a].Start].Dressing == DressDune;
                bool duneB = land[groups[b].Start].Dressing == DressDune;
                return duneA != duneB ? (duneA ? -1 : 1) : a.CompareTo(b);
            });
            coverageCost += groups[indices[0]].Count;
        }
        // A large preferred group must not miss its chunk share just because neighbouring groups are smaller.
        // Reserving all first groups together avoids this whenever complete coverage is affordable.
        if (coverageCost <= budget)
        {
            foreach (var key in keys)
            {
                int index = byChunk[key][0];
                groupSelected[index] = true; selectedCount += groups[index].Count;
            }
        }
        int remaining = budget - selectedCount;
        int share = keys.Count > 0 ? remaining / keys.Count : 0;
        int extra = keys.Count > 0 ? remaining % keys.Count : 0;
        for (int chunk = 0; chunk < keys.Count; chunk++)
        {
            var indices = byChunk[keys[chunk]];
            int quota = share + (chunk < extra ? 1 : 0), used = 0;
            foreach (int index in indices)
            {
                int count = groups[index].Count;
                if (groupSelected[index] || count > quota - used) continue;
                groupSelected[index] = true; used += count;
            }
            selectedCount += used;
        }
        // groups retain ByPriority order, so spare capacity favours the harbor without starving other chunks.
        for (int i = 0; i < groups.Count && selectedCount < budget; i++)
        {
            if (groupSelected[i] || groups[i].Count > budget - selectedCount) continue;
            groupSelected[i] = true; selectedCount += groups[i].Count;
        }
        var keptChunks = new HashSet<Vector2Int>();
        var keptDuneChunks = new HashSet<Vector2Int>();
        for (int i = 0; i < groups.Count; i++)
        {
            if (!groupSelected[i]) continue;
            var group = groups[i];
            for (int p = group.Start; p < group.Start + group.Count; p++)
            {
                selected[p] = true;
                var actualKey = ChunkKey(land[p].Position);
                keptChunks.Add(actualKey);
                if (land[p].Dressing == DressDune) keptDuneChunks.Add(actualKey);
            }
        }
        _landKeptChunks = keptChunks.Count; _duneKeptChunks = keptDuneChunks.Count;
        return selected;
    }

    // Per-cell dune strength for the ground shader (soil/grass patch under the tufts). Same cell layout as
    // _PA_SurfaceMap, so the shader maps world -> UV with the existing _PA_SurfaceMapParams.
    void PublishDuneMap()
    {
        if (_duneMap != null) Destroy(_duneMap);
        _duneMap = null;
        int width = _width * DuneGroundSubdivisions, height = _height * DuneGroundSubdivisions;
        if (_duneGroundPixels == null || _duneGroundPixels.Length != width * height || _width <= 0 || _height <= 0)
        {
            Shader.SetGlobalFloat(DuneMapEnabledId, 0);
            return;
        }
        _duneMap = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
        {
            name = "PA_DuneMap_Runtime", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave
        };
        _duneMap.SetPixels32(_duneGroundPixels);
        _duneMap.Apply(false, true);
        Shader.SetGlobalTexture(DuneMapId, _duneMap);
        Shader.SetGlobalVector(DuneMapSizeId, new Vector4(width, height, _grid.Definition.CellSize / DuneGroundSubdivisions, 0));
        Shader.SetGlobalFloat(DuneMapEnabledId, 1);
    }

    Chunk CreateChunk(List<Placement> placements)
    {
        var batchIndex = new Dictionary<(Mesh, int, Material, ShadowCastingMode), int>();
        var variants = new Dictionary<(int, int, bool, bool, bool), int[]>();
        var batches = new List<Batch>();
        var sizes = new List<int>();
        var chunk = new Chunk { Instances = new Instance[placements.Count] };
        for (int i = 0; i < placements.Count; i++)
        {
            var p = placements[i];
            var model = _models[p.Model];
            if (!variants.TryGetValue((p.Model, p.Tint, p.Underwater, p.Bush, p.NoShadow), out var map))
            {
                map = new int[model.Parts.Length];
                // Underwater dressing and tiny beach scatter (<0.2 m) cast no shadows.
                var shadows = p.Underwater || p.NoShadow ? ShadowCastingMode.Off : ShadowCastingMode.On;
                for (int k = 0; k < map.Length; k++)
                {
                    var part = model.Parts[k];
                    var key = (part.Mesh, part.Submesh, PartMaterial(part.Source, p.Tint, p.Bush), shadows);
                    if (!batchIndex.TryGetValue(key, out int index))
                    {
                        index = batches.Count; batchIndex.Add(key, index); sizes.Add(0);
                        batches.Add(new Batch { Mesh = key.Item1, Submesh = key.Item2, Material = key.Item3, Shadows = shadows });
                    }
                    map[k] = index;
                }
                variants.Add((p.Model, p.Tint, p.Underwater, p.Bush, p.NoShadow), map);
            }
            foreach (int index in map) sizes[index]++;
            float scale = p.Height / model.SizeY;
            var root = Matrix4x4.TRS(new Vector3(p.Position.x, p.Position.y - scale * model.MinY, p.Position.z),
                Quaternion.Euler(0, p.Yaw, 0), new Vector3(scale * p.Spread, scale, scale * p.Spread));
            chunk.Instances[i] = new Instance
            {
                Root = root, Position = p.Position, Cell = p.Cell, Batches = map, Model = p.Model,
                Underwater = p.Underwater, Visible = true
            };
            float radius = model.Radius * scale * p.Spread;
            var bounds = new Bounds(p.Position + Vector3.up * (p.Height * .5f), new Vector3(radius * 2, p.Height, radius * 2) + Vector3.one * .2f);
            if (i == 0) chunk.Bounds = bounds; else chunk.Bounds.Encapsulate(bounds);
        }
        for (int i = 0; i < batches.Count; i++) batches[i].Matrices = new Matrix4x4[sizes[i]];
        chunk.Batches = batches.ToArray();
        Rebuild(chunk);
        return chunk;
    }

    void Rebuild(Chunk chunk)
    {
        foreach (var batch in chunk.Batches) batch.Count = 0;
        foreach (var instance in chunk.Instances)
        {
            if (!instance.Visible || instance.Covered) continue;
            var parts = _models[instance.Model].Parts;
            for (int k = 0; k < parts.Length; k++)
            {
                var batch = chunk.Batches[instance.Batches[k]];
                batch.Matrices[batch.Count++] = instance.Root * parts[k].Local;
            }
        }
    }

    void Add(List<Placement> list, GameObject[] family, System.Random random, Vector3 position, Vector2Int cell,
        float height, int tint, bool underwater, float priority, ref int order, float spread = 1, bool bush = false, float maxAspect = 0,
        bool noShadow = false, int dressing = 0)
    {
        if (family == null || family.Length == 0) return;
        int model = ModelIndex(family[random.Next(family.Length)]);
        float yaw = random.Next(360);
        if (model < 0) return;
        // height <= maxAspect * narrowest width: x/z scale is (height / SizeY) * spread, so spread >= SizeY / (maxAspect * Width).
        if (maxAspect > 0) spread = Mathf.Max(spread, _models[model].SizeY / (maxAspect * _models[model].Width));
        list.Add(new Placement
        {
            Position = position, Cell = cell, Model = model, Tint = tint, Height = height, Yaw = yaw, Spread = spread,
            Underwater = underwater, Bush = bush, NoShadow = noShadow, Dressing = dressing, Priority = priority, Order = order++
        });
    }

    int ModelIndex(GameObject prefab)
    {
        if (prefab == null) return -1;
        if (_modelIndex.TryGetValue(prefab, out int index)) return index;
        var root = prefab.transform;
        var basis = Matrix4x4.TRS(Vector3.zero, root.localRotation, root.localScale);
        var toRoot = root.worldToLocalMatrix;
        // Place() measures every renderer; only LOD0 renderers are drawn.
        var lowerLods = new HashSet<Renderer>();
        foreach (var group in prefab.GetComponentsInChildren<LODGroup>(true))
        {
            var lods = group.GetLODs();
            for (int l = 1; l < lods.Length; l++)
                foreach (var renderer in lods[l].renderers) if (renderer != null) lowerLods.Add(renderer);
        }
        var parts = new List<Part>();
        bool measured = false;
        Bounds bounds = default;
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>())
        {
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
            else if (renderer is MeshRenderer && renderer.TryGetComponent<MeshFilter>(out var filter)) mesh = filter.sharedMesh;
            if (mesh == null) continue;
            var local = basis * toRoot * renderer.transform.localToWorldMatrix;
            var box = TransformBounds(mesh.bounds, local);
            if (measured) bounds.Encapsulate(box); else { bounds = box; measured = true; }
            if (!(renderer is MeshRenderer) || !renderer.enabled || lowerLods.Contains(renderer)) continue;
            var materials = renderer.sharedMaterials;
            for (int s = 0; s < mesh.subMeshCount && s < materials.Length; s++)
                if (materials[s] != null) parts.Add(new Part { Mesh = mesh, Submesh = s, Source = materials[s], Local = local });
        }
        index = -1;
        if (measured && parts.Count > 0)
        {
            float radius = 0;
            for (int corner = 0; corner < 4; corner++)
            {
                float cx = (corner & 1) == 0 ? bounds.min.x : bounds.max.x, cz = (corner & 2) == 0 ? bounds.min.z : bounds.max.z;
                radius = Mathf.Max(radius, new Vector2(cx, cz).magnitude);
            }
            index = _models.Count;
            _models.Add(new Model
            {
                Parts = parts.ToArray(), MinY = bounds.min.y, SizeY = Mathf.Max(.001f, bounds.size.y), Radius = radius,
                Width = Mathf.Max(.001f, Mathf.Min(bounds.size.x, bounds.size.z))
            });
        }
        _modelIndex.Add(prefab, index);
        return index;
    }

    // Ultimate Nature 'Green'/'DarkGreen' parts (grass, bush, stems, moss) are single-sided, untextured URP/Lit:
    // they read near-black from the play camera, so they use the two-sided PA/StylizedFoliage gradient instead.
    // Petals (Cyan/Yellow) and rocks keep the tinted source copy.
    Material PartMaterial(Material source, int tint, bool bush)
    {
        if (_foliage == null || tint >= TintRock || !(source.name.StartsWith("Green", System.StringComparison.Ordinal) ||
            source.name.StartsWith("DarkGreen", System.StringComparison.Ordinal))) return InstancedMaterial(source, tint);
        if (_foliageVariants.TryGetValue((tint, bush), out var material)) return material;
        material = new Material(_foliage) { enableInstancing = true, hideFlags = HideFlags.DontSave, name = "PA_Foliage_PAInstanced" + tint + (bush ? "_Bush" : "") };
        // Seaweed: HDR multiplier over the unchanged root/tip palette; land variants stay white.
        material.SetColor(BaseColorId, tint == TintSeaweed ? SeaweedLift : Color.white);
        material.SetColor(RootColorId, FoliageRoot[tint]);
        material.SetColor(TipColorId, FoliageTip[tint]);
        material.SetFloat(GradientHeightId, bush ? .8f : .55f);
        if (tint == TintSeaweed)
        {
            material.SetFloat(WindId, .03f); material.SetFloat(WindSpeedId, .8f); material.SetFloat(TranslucencyId, .25f);
        }
        else material.SetFloat(WindId, bush ? .02f : .06f);
        _foliageVariants.Add((tint, bush), material);
        return material;
    }

    Material InstancedMaterial(Material source, int tint)
    {
        if (_materials.TryGetValue((source, tint), out var material)) return material;
        material = new Material(source) { enableInstancing = true, hideFlags = HideFlags.DontSave, name = source.name + "_PAInstanced" + tint };
        if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, material.GetColor(BaseColorId) * Tints[tint]);
        if (material.HasProperty(ColorId)) material.SetColor(ColorId, material.GetColor(ColorId) * Tints[tint]);
        _materials.Add((source, tint), material);
        return material;
    }

    void LateUpdate()
    {
        if (_player == null || _chunks.Length == 0) return;
        Vector3 player = _player.position;
        if (Time.unscaledTime >= _occupancyAt)
        {
            _occupancyAt = Time.unscaledTime + OccupancyInterval;
            RefreshOccupancy(player);
        }
        if (!SystemInfo.supportsInstancing) return;
        for (int c = 0; c < _chunks.Length; c++)
        {
            var chunk = _chunks[c];
            if (chunk.Bounds.SqrDistance(player) > DrawDistance * DrawDistance) continue;
            for (int b = 0; b < chunk.Batches.Length; b++)
            {
                var batch = chunk.Batches[b];
                if (batch.Count == 0) continue;
                var parameters = new RenderParams(batch.Material)
                {
                    shadowCastingMode = batch.Shadows, receiveShadows = true, layer = 0, worldBounds = chunk.Bounds
                };
                for (int start = 0; start < batch.Count; start += BatchLimit)
                    Graphics.RenderMeshInstanced(parameters, batch.Mesh, batch.Submesh, batch.Matrices,
                        Mathf.Min(BatchLimit, batch.Count - start), start);
            }
        }
    }

    // Same 3x3 rule as RefreshApproachDressing, measured against the composed baseline so coast and
    // resource neighbours present at build time do not hide plants; new buildings/paths do.
    void RefreshOccupancy(Vector3 player)
    {
        if (_grid == null) return;
        foreach (var chunk in _chunks)
        {
            if (chunk.Bounds.SqrDistance(player) > OccupancyDistance * OccupancyDistance) continue;
            bool changed = false;
            for (int i = 0; i < chunk.Instances.Length; i++)
            {
                ref var instance = ref chunk.Instances[i];
                if (instance.Covered) continue;
                bool clear = instance.Underwater
                    ? _grid.TryGetCell(instance.Cell, out var water) && water.HasWater
                    : LandClear(instance.Cell);
                if (clear == instance.Visible) continue;
                instance.Visible = clear; changed = true;
            }
            if (changed) Rebuild(chunk);
        }
    }

    bool LandClear(Vector2Int coordinate)
    {
        for (int z = -1; z <= 1; z++)
        for (int x = -1; x <= 1; x++)
        {
            var c = coordinate + new Vector2Int(x, z);
            if (c.x < 0 || c.y < 0 || c.x >= _width || c.y >= _height || !_grid.TryGetCell(c, out var cell)) continue;
            int i = c.y * _width + c.x;
            if (_baseWalkable[i] && !cell.IsWalkable || !_basePath[i] && cell.HasPath) return false;
        }
        return true;
    }

    int[] Distances(WorldCellData[] cells, bool[] valid, bool fromWater)
    {
        var distance = new int[cells.Length];
        var queue = new int[cells.Length];
        int head = 0, tail = 0;
        for (int i = 0; i < cells.Length; i++)
        {
            distance[i] = -1;
            if (valid[i] && cells[i].HasWater == fromWater) { distance[i] = 0; queue[tail++] = i; }
        }
        while (head < tail)
        {
            int i = queue[head++], x = i % _width, z = i / _width;
            for (int d = 0; d < 4; d++)
            {
                int nx = x + (d == 0 ? 1 : d == 1 ? -1 : 0), nz = z + (d == 2 ? 1 : d == 3 ? -1 : 0);
                if (nx < 0 || nz < 0 || nx >= _width || nz >= _height) continue;
                int n = nz * _width + nx;
                if (distance[n] >= 0 || !valid[n] || cells[n].HasWater == fromWater) continue;
                distance[n] = distance[i] + 1; queue[tail++] = n;
            }
        }
        return distance;
    }

    // Dry cells: steps (4-neighbour, over dry land) to the nearest dry cell of the given ground type; -1 unreachable.
    int[] GroundDistances(WorldCellData[] cells, bool[] valid, WorldGroundType ground)
    {
        var distance = new int[cells.Length];
        var queue = new int[cells.Length];
        int head = 0, tail = 0;
        for (int i = 0; i < cells.Length; i++)
        {
            distance[i] = -1;
            if (valid[i] && !cells[i].HasWater && cells[i].GroundType == ground) { distance[i] = 0; queue[tail++] = i; }
        }
        while (head < tail)
        {
            int i = queue[head++], x = i % _width, z = i / _width;
            for (int d = 0; d < 4; d++)
            {
                int nx = x + (d == 0 ? 1 : d == 1 ? -1 : 0), nz = z + (d == 2 ? 1 : d == 3 ? -1 : 0);
                if (nx < 0 || nz < 0 || nx >= _width || nz >= _height) continue;
                int n = nz * _width + nx;
                if (distance[n] >= 0 || !valid[n] || cells[n].HasWater) continue;
                distance[n] = distance[i] + 1; queue[tail++] = n;
            }
        }
        return distance;
    }

    static int ByPriority(Placement a, Placement b)
    {
        int compare = a.Priority.CompareTo(b.Priority);
        return compare != 0 ? compare : a.Order.CompareTo(b.Order);
    }

    static Bounds TransformBounds(Bounds local, Matrix4x4 matrix)
    {
        var extents = local.extents;
        var x = matrix.MultiplyVector(new Vector3(extents.x, 0, 0));
        var y = matrix.MultiplyVector(new Vector3(0, extents.y, 0));
        var z = matrix.MultiplyVector(new Vector3(0, 0, extents.z));
        var size = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
            Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z)) * 2;
        return new Bounds(matrix.MultiplyPoint3x4(local.center), size);
    }

    static Vector2Int ChunkKey(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x / ChunkMetres), Mathf.FloorToInt(p.z / ChunkMetres));
    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);
    static float Lerp(System.Random random, float a, float b) => a + (b - a) * (float)random.NextDouble();
    static Vector3 Jitter(System.Random random, float range) =>
        new Vector3(((float)random.NextDouble() * 2 - 1) * range, 0, ((float)random.NextDouble() * 2 - 1) * range);
    static Vector3 ClampInCell(Vector3 offset, float half) =>
        new Vector3(Mathf.Clamp(offset.x, -half, half), 0, Mathf.Clamp(offset.z, -half, half));

    void OnDestroy()
    {
        foreach (var material in _materials.Values) if (material != null) Destroy(material);
        foreach (var material in _foliageVariants.Values) if (material != null) Destroy(material);
        _materials.Clear(); _foliageVariants.Clear();
        if (_duneMap == null) return;
        Destroy(_duneMap);
        _duneMap = null;
        Shader.SetGlobalFloat(DuneMapEnabledId, 0);
    }
}
