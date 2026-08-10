using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public enum WorldBiomeType
{
    Ocean = 0,
    Coast = 1,
    Meadow = 2,
    Forest = 3,
    Highland = 4
}

public enum WorldWaterType
{
    None = 0,
    Ocean = 1,
    River = 2,
    Pond = 3
}

public enum WorldGenerationAnchorKind
{
    Start = 0,
    Shop = 1,
    Beach = 2,
    MeadowActivity = 3,
    ForestActivity = 4,
    HighlandActivity = 5,
    PondActivity = 6
}

public enum WorldResourceKind
{
    Forage = 0,
    Timber = 1,
    Stone = 2,
    Fish = 3
}

public sealed class WorldIslandGenerationSettings
{
    public const int CurrentGenerationVersion = 1;
    public const int ProvisionalWidthCells = 128;
    public const int ProvisionalHeightCells = 128;

    public int GenerationVersion { get; }
    public int WidthCells { get; }
    public int HeightCells { get; }
    public float CellSize { get; }
    public int ChunkSize { get; }
    public float ElevationStep { get; }
    public int MinimumElevationLevel { get; }
    public int MaximumElevationLevel { get; }

    public static WorldIslandGenerationSettings Provisional128 { get; } =
        new WorldIslandGenerationSettings(
            CurrentGenerationVersion,
            ProvisionalWidthCells,
            ProvisionalHeightCells,
            WorldGridService.World001CellSize,
            WorldGridService.World001ChunkSize,
            WorldGridService.World001ElevationStep,
            WorldGridService.World001MinElevation,
            WorldGridService.World001MaxElevation);

    public WorldIslandGenerationSettings(
        int generationVersion,
        int widthCells,
        int heightCells,
        float cellSize,
        int chunkSize,
        float elevationStep,
        int minimumElevationLevel,
        int maximumElevationLevel)
    {
        if (generationVersion <= 0) throw new ArgumentOutOfRangeException(nameof(generationVersion));
        if (widthCells <= 0 || heightCells <= 0) throw new ArgumentOutOfRangeException(nameof(widthCells));
        if (chunkSize <= 0 || widthCells % chunkSize != 0 || heightCells % chunkSize != 0)
            throw new ArgumentException("Generated world dimensions must be positive chunk multiples.");
        if (cellSize <= 0f || elevationStep <= 0f)
            throw new ArgumentOutOfRangeException(nameof(cellSize));
        if (minimumElevationLevel > maximumElevationLevel)
            throw new ArgumentException("Minimum elevation cannot exceed maximum elevation.");

        GenerationVersion = generationVersion;
        WidthCells = widthCells;
        HeightCells = heightCells;
        CellSize = cellSize;
        ChunkSize = chunkSize;
        ElevationStep = elevationStep;
        MinimumElevationLevel = minimumElevationLevel;
        MaximumElevationLevel = maximumElevationLevel;
    }

    public WorldGridDefinition CreateGridDefinition()
    {
        return new WorldGridDefinition(
            CellSize,
            WidthCells,
            HeightCells,
            ChunkSize,
            ElevationStep,
            MinimumElevationLevel,
            MaximumElevationLevel,
            Vector3.zero);
    }
}

public readonly struct WorldGeneratedCell
{
    public WorldCellData Terrain { get; }
    public WorldBiomeType Biome { get; }
    public WorldWaterType WaterType { get; }
    public bool IsOcean => WaterType == WorldWaterType.Ocean;
    public bool IsDryLand => WaterType == WorldWaterType.None;

    internal WorldGeneratedCell(
        WorldCellData terrain,
        WorldBiomeType biome,
        WorldWaterType waterType)
    {
        Terrain = terrain;
        Biome = biome;
        WaterType = waterType;
    }
}

public readonly struct WorldGenerationAnchor
{
    public WorldGenerationAnchorKind Kind { get; }
    public Vector2Int Coordinate { get; }
    public Vector2Int FootprintSize { get; }
    public Vector2Int EntranceCoordinate { get; }

    internal WorldGenerationAnchor(
        WorldGenerationAnchorKind kind,
        Vector2Int coordinate,
        Vector2Int footprintSize,
        Vector2Int entranceCoordinate)
    {
        Kind = kind;
        Coordinate = coordinate;
        FootprintSize = footprintSize;
        EntranceCoordinate = entranceCoordinate;
    }
}

public readonly struct WorldResourceSpawnRecord
{
    public string SpawnKey { get; }
    public WorldResourceKind Kind { get; }
    public Vector2Int Coordinate { get; }

    internal WorldResourceSpawnRecord(
        string spawnKey,
        WorldResourceKind kind,
        Vector2Int coordinate)
    {
        SpawnKey = spawnKey;
        Kind = kind;
        Coordinate = coordinate;
    }
}

public sealed class WorldGenerationResult
{
    readonly ReadOnlyCollection<WorldGeneratedCell> _cells;
    readonly ReadOnlyCollection<WorldCellData> _terrainCells;
    readonly ReadOnlyCollection<WorldGenerationAnchor> _anchors;
    readonly ReadOnlyCollection<WorldResourceSpawnRecord> _resourceSpawns;
    readonly Dictionary<WorldGenerationAnchorKind, WorldGenerationAnchor> _anchorByKind;

    public long Seed { get; }
    public int GenerationVersion { get; }
    public WorldGridDefinition Definition { get; }
    public IReadOnlyList<WorldGeneratedCell> Cells => _cells;
    public IReadOnlyList<WorldCellData> TerrainCells => _terrainCells;
    public IReadOnlyList<WorldGenerationAnchor> Anchors => _anchors;
    public IReadOnlyList<WorldResourceSpawnRecord> ResourceSpawns => _resourceSpawns;
    public ulong Checksum { get; }
    public int OceanCellCount { get; }
    public int DryLandCellCount { get; }
    public float LandRatio => (Definition.TotalCellCount - OceanCellCount) /
                              (float)Definition.TotalCellCount;

    internal WorldGenerationResult(
        long seed,
        int generationVersion,
        WorldGridDefinition definition,
        WorldGeneratedCell[] cells,
        WorldGenerationAnchor[] anchors,
        WorldResourceSpawnRecord[] resourceSpawns,
        ulong checksum)
    {
        Seed = seed;
        GenerationVersion = generationVersion;
        Definition = definition;
        _cells = Array.AsReadOnly(cells);
        _terrainCells = Array.AsReadOnly(cells.Select(cell => cell.Terrain).ToArray());
        _anchors = Array.AsReadOnly(anchors);
        _resourceSpawns = Array.AsReadOnly(resourceSpawns);
        _anchorByKind = anchors.ToDictionary(anchor => anchor.Kind);
        Checksum = checksum;
        OceanCellCount = cells.Count(cell => cell.IsOcean);
        DryLandCellCount = cells.Count(cell => cell.IsDryLand);
    }

    public bool TryGetCell(Vector2Int coordinate, out WorldGeneratedCell cell)
    {
        if (coordinate.x < 0 || coordinate.x >= Definition.Width ||
            coordinate.y < 0 || coordinate.y >= Definition.Height)
        {
            cell = default;
            return false;
        }

        cell = _cells[coordinate.y * Definition.Width + coordinate.x];
        return true;
    }

    public bool TryGetAnchor(
        WorldGenerationAnchorKind kind,
        out WorldGenerationAnchor anchor)
    {
        return _anchorByKind.TryGetValue(kind, out anchor);
    }
}

public static class WorldIslandGenerator
{
    const uint ShapeSalt = 0xA3C59AC3u;
    const uint ElevationSalt = 0x3C6EF372u;
    const uint CenterSalt = 0x9E3779B9u;
    const uint ResourceSalt = 0xBB67AE85u;

    struct MutableCell
    {
        public Vector2Int Coordinate;
        public int Elevation;
        public WorldGroundType Ground;
        public WorldPathType Path;
        public WorldWaterType Water;
        public WorldBiomeType Biome;
    }

    public static WorldGenerationResult Generate(
        long seed,
        WorldIslandGenerationSettings settings = null)
    {
        settings ??= WorldIslandGenerationSettings.Provisional128;
        WorldGridDefinition definition = settings.CreateGridDefinition();
        MutableCell[] mutable = CreateBaseIsland(seed, settings);

        Vector2Int center = ResolveCenter(seed, settings);
        var start = center;
        var shop = center + new Vector2Int(3, -1);
        var meadow = center + new Vector2Int(-14, -10);
        var forest = center + new Vector2Int(0, 22);
        var highland = center + new Vector2Int(-18, 8);
        var pondCenter = center + new Vector2Int(14, 12);
        var pondActivity = pondCenter + new Vector2Int(-3, 0);

        PaintDryPatch(mutable, settings, center, new Vector2Int(10, 8),
            2, WorldGroundType.Default, WorldBiomeType.Meadow, WorldPathType.None);
        CarvePondAndRiver(mutable, settings, pondCenter);
        Vector2Int beach = FindLastDryLandEast(mutable, settings, center.y - 18);

        CarveDryRoute(mutable, settings, start, shop + new Vector2Int(1, -1), 2, 2);
        CarveDryRoute(mutable, settings, start, meadow, 2, 1);
        CarveDryRoute(mutable, settings, start, forest, 2, 2);
        CarveDryRoute(mutable, settings, start, highland, 2, 4);
        CarveDryRoute(mutable, settings, start, pondActivity, 2, 1);
        CarveDryRoute(mutable, settings, start, beach, 2, 0);

        PaintDryPatch(mutable, settings, start, Vector2Int.one,
            2, WorldGroundType.Default, WorldBiomeType.Meadow, WorldPathType.Dirt);
        PaintDryPatch(mutable, settings, meadow, Vector2Int.one,
            1, WorldGroundType.Default, WorldBiomeType.Meadow, WorldPathType.Dirt);
        PaintDryPatch(mutable, settings, forest, Vector2Int.one,
            2, WorldGroundType.Default, WorldBiomeType.Forest, WorldPathType.Dirt);
        PaintDryPatch(mutable, settings, highland, Vector2Int.one,
            4, WorldGroundType.Rock, WorldBiomeType.Highland, WorldPathType.Dirt);
        PaintDryPatch(mutable, settings, pondActivity, Vector2Int.one,
            1, WorldGroundType.Default, WorldBiomeType.Meadow, WorldPathType.Dirt);
        PaintDryPatch(mutable, settings, beach, Vector2Int.one,
            0, WorldGroundType.Sand, WorldBiomeType.Coast, WorldPathType.Dirt);

        WorldGenerationAnchor[] anchors = CreateAnchors(
            start, shop, beach, meadow, forest, highland, pondActivity);
        WorldGeneratedCell[] cells = FreezeCells(mutable, settings);
        WorldResourceSpawnRecord[] resourceSpawns =
            CreateResourceSpawns(seed, settings.GenerationVersion, cells, settings);
        ulong checksum = ComputeChecksum(
            seed, settings, cells, anchors, resourceSpawns);
        return new WorldGenerationResult(
            seed,
            settings.GenerationVersion,
            definition,
            cells,
            anchors,
            resourceSpawns,
            checksum);
    }

    static MutableCell[] CreateBaseIsland(
        long seed,
        WorldIslandGenerationSettings settings)
    {
        int width = settings.WidthCells;
        int height = settings.HeightCells;
        Vector2Int center = ResolveCenter(seed, settings);
        int radiusX = Mathf.Max(16, width * 43 / 100 + HashRange(seed, 0, 0, CenterSalt, -3, 4));
        int radiusZ = Mathf.Max(16, height * 39 / 100 + HashRange(seed, 1, 0, CenterSalt, -3, 4));
        var cells = new MutableCell[width * height];

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                var coordinate = new Vector2Int(x, z);
                int dx = x - center.x;
                int dz = z - center.y;
                long radialQ = (long)dx * dx * 1000000L / (radiusX * radiusX) +
                               (long)dz * dz * 1000000L / (radiusZ * radiusZ);
                int shapeNoise = SampleValueNoise(seed, x, z, 12, ShapeSalt);
                long thresholdQ = 1000000L + shapeNoise * 5L;
                bool borderOcean = x < 4 || z < 4 || x >= width - 4 || z >= height - 4;
                bool isLand = !borderOcean && radialQ <= thresholdQ;
                if (!isLand)
                {
                    cells[index] = new MutableCell
                    {
                        Coordinate = coordinate,
                        Elevation = 0,
                        Ground = WorldGroundType.Sand,
                        Path = WorldPathType.None,
                        Water = WorldWaterType.Ocean,
                        Biome = WorldBiomeType.Ocean
                    };
                    continue;
                }

                int elevationNoise = SampleValueNoise(seed, x, z, 8, ElevationSalt);
                ResolveLandBand(radialQ, elevationNoise,
                    out int elevation,
                    out WorldGroundType ground,
                    out WorldBiomeType biome);
                cells[index] = new MutableCell
                {
                    Coordinate = coordinate,
                    Elevation = Mathf.Clamp(
                        elevation,
                        settings.MinimumElevationLevel,
                        settings.MaximumElevationLevel),
                    Ground = ground,
                    Path = WorldPathType.None,
                    Water = WorldWaterType.None,
                    Biome = biome
                };
            }
        }
        return cells;
    }

    static void ResolveLandBand(
        long radialQ,
        int elevationNoise,
        out int elevation,
        out WorldGroundType ground,
        out WorldBiomeType biome)
    {
        if (radialQ > 780000L)
        {
            elevation = 0;
            ground = WorldGroundType.Sand;
            biome = WorldBiomeType.Coast;
        }
        else if (radialQ > 500000L)
        {
            elevation = 1;
            ground = WorldGroundType.Default;
            biome = WorldBiomeType.Meadow;
        }
        else if (radialQ > 210000L)
        {
            elevation = elevationNoise > 9000 ? 3 : 2;
            ground = WorldGroundType.Default;
            biome = WorldBiomeType.Forest;
        }
        else
        {
            elevation = radialQ < 55000L ? 6 : elevationNoise > 6000 ? 5 : 4;
            ground = WorldGroundType.Rock;
            biome = WorldBiomeType.Highland;
        }
    }

    static Vector2Int ResolveCenter(long seed, WorldIslandGenerationSettings settings)
    {
        int x = settings.WidthCells / 2 + HashRange(seed, 0, 0, CenterSalt, -3, 4);
        int z = settings.HeightCells / 2 + HashRange(seed, 0, 1, CenterSalt, -3, 4);
        return new Vector2Int(x, z);
    }

    static void CarvePondAndRiver(
        MutableCell[] cells,
        WorldIslandGenerationSettings settings,
        Vector2Int pondCenter)
    {
        for (int z = -1; z <= 1; z++)
        {
            for (int x = -1; x <= 1; x++)
                SetWater(cells, settings, pondCenter + new Vector2Int(x, z), WorldWaterType.Pond);
        }

        Vector2Int cursor = pondCenter + Vector2Int.right * 2;
        int bend = 0;
        while (cursor.x < settings.WidthCells - 4)
        {
            int index = cursor.y * settings.WidthCells + cursor.x;
            if (cells[index].Water == WorldWaterType.Ocean) break;
            SetWater(cells, settings, cursor, WorldWaterType.River);
            cursor.x++;
            bend++;
            if (bend % 7 == 0 && cursor.y > 6) cursor.y--;
        }
    }

    static void SetWater(
        MutableCell[] cells,
        WorldIslandGenerationSettings settings,
        Vector2Int coordinate,
        WorldWaterType waterType)
    {
        if (!IsValid(settings, coordinate)) return;
        int index = coordinate.y * settings.WidthCells + coordinate.x;
        MutableCell cell = cells[index];
        cell.Elevation = 0;
        cell.Ground = WorldGroundType.Sand;
        cell.Path = WorldPathType.None;
        cell.Water = waterType;
        if (waterType == WorldWaterType.Ocean) cell.Biome = WorldBiomeType.Ocean;
        cells[index] = cell;
    }

    static Vector2Int FindLastDryLandEast(
        MutableCell[] cells,
        WorldIslandGenerationSettings settings,
        int row)
    {
        int clampedRow = Mathf.Clamp(row, 5, settings.HeightCells - 6);
        Vector2Int last = new Vector2Int(settings.WidthCells / 2, clampedRow);
        for (int x = settings.WidthCells / 2; x < settings.WidthCells - 4; x++)
        {
            int index = clampedRow * settings.WidthCells + x;
            if (cells[index].Water == WorldWaterType.Ocean) break;
            if (cells[index].Water == WorldWaterType.None) last = new Vector2Int(x, clampedRow);
        }
        return last;
    }

    static void CarveDryRoute(
        MutableCell[] cells,
        WorldIslandGenerationSettings settings,
        Vector2Int from,
        Vector2Int to,
        int startElevation,
        int endElevation)
    {
        List<Vector2Int> route = BuildManhattanRoute(from, to);
        int denominator = Mathf.Max(1, route.Count - 1);
        for (int i = 0; i < route.Count; i++)
        {
            Vector2Int coordinate = route[i];
            if (!IsValid(settings, coordinate)) continue;
            int elevation = Mathf.RoundToInt(Mathf.Lerp(
                startElevation,
                endElevation,
                i / (float)denominator));
            int index = coordinate.y * settings.WidthCells + coordinate.x;
            MutableCell cell = cells[index];
            cell.Elevation = Mathf.Clamp(elevation,
                settings.MinimumElevationLevel,
                settings.MaximumElevationLevel);
            cell.Ground = endElevation == 0 && i > denominator - 4
                ? WorldGroundType.Sand
                : WorldGroundType.Default;
            cell.Path = WorldPathType.Dirt;
            cell.Water = WorldWaterType.None;
            if (cell.Biome == WorldBiomeType.Ocean)
                cell.Biome = endElevation == 0 ? WorldBiomeType.Coast : WorldBiomeType.Meadow;
            cells[index] = cell;
        }
    }

    static List<Vector2Int> BuildManhattanRoute(Vector2Int from, Vector2Int to)
    {
        var route = new List<Vector2Int>();
        Vector2Int cursor = from;
        route.Add(cursor);
        bool xFirst = true;
        while (cursor != to)
        {
            int dx = to.x - cursor.x;
            int dz = to.y - cursor.y;
            bool moveX = dx != 0 && (dz == 0 || Mathf.Abs(dx) > Mathf.Abs(dz) || xFirst);
            if (moveX) cursor.x += Math.Sign(dx);
            else cursor.y += Math.Sign(dz);
            route.Add(cursor);
            xFirst = !xFirst;
        }
        return route;
    }

    static void PaintDryPatch(
        MutableCell[] cells,
        WorldIslandGenerationSettings settings,
        Vector2Int center,
        Vector2Int radius,
        int elevation,
        WorldGroundType ground,
        WorldBiomeType biome,
        WorldPathType path)
    {
        for (int z = -radius.y; z <= radius.y; z++)
        {
            for (int x = -radius.x; x <= radius.x; x++)
            {
                Vector2Int coordinate = center + new Vector2Int(x, z);
                if (!IsValid(settings, coordinate)) continue;
                int index = coordinate.y * settings.WidthCells + coordinate.x;
                MutableCell cell = cells[index];
                cell.Elevation = elevation;
                cell.Ground = ground;
                cell.Biome = biome;
                cell.Water = WorldWaterType.None;
                cell.Path = path;
                cells[index] = cell;
            }
        }
    }

    static WorldGenerationAnchor[] CreateAnchors(
        Vector2Int start,
        Vector2Int shop,
        Vector2Int beach,
        Vector2Int meadow,
        Vector2Int forest,
        Vector2Int highland,
        Vector2Int pondActivity)
    {
        return new[]
        {
            new WorldGenerationAnchor(WorldGenerationAnchorKind.Start,
                start, Vector2Int.one, start),
            new WorldGenerationAnchor(WorldGenerationAnchorKind.Shop,
                shop, new Vector2Int(4, 3), shop + new Vector2Int(1, -1)),
            new WorldGenerationAnchor(WorldGenerationAnchorKind.Beach,
                beach, Vector2Int.one, beach),
            new WorldGenerationAnchor(WorldGenerationAnchorKind.MeadowActivity,
                meadow, Vector2Int.one, meadow),
            new WorldGenerationAnchor(WorldGenerationAnchorKind.ForestActivity,
                forest, Vector2Int.one, forest),
            new WorldGenerationAnchor(WorldGenerationAnchorKind.HighlandActivity,
                highland, Vector2Int.one, highland),
            new WorldGenerationAnchor(WorldGenerationAnchorKind.PondActivity,
                pondActivity, Vector2Int.one, pondActivity)
        };
    }

    static WorldGeneratedCell[] FreezeCells(
        MutableCell[] mutable,
        WorldIslandGenerationSettings settings)
    {
        var cells = new WorldGeneratedCell[mutable.Length];
        for (int i = 0; i < mutable.Length; i++)
        {
            MutableCell source = mutable[i];
            bool hasWater = source.Water != WorldWaterType.None;
            var terrain = new WorldCellData(
                source.Coordinate,
                source.Elevation,
                source.Ground,
                source.Path,
                hasWater ? 1 : source.Elevation,
                hasWater ? 1 : 0,
                WorldCellOccupancy.Empty);
            cells[i] = new WorldGeneratedCell(terrain, source.Biome, source.Water);
        }
        return cells;
    }

    static WorldResourceSpawnRecord[] CreateResourceSpawns(
        long seed,
        int generationVersion,
        WorldGeneratedCell[] cells,
        WorldIslandGenerationSettings settings)
    {
        var records = new List<WorldResourceSpawnRecord>();
        AddRankedSpawns(records, seed, generationVersion, cells, settings,
            WorldResourceKind.Forage, 6,
            cell => cell.IsDryLand && cell.Biome == WorldBiomeType.Meadow && !cell.Terrain.HasPath);
        AddRankedSpawns(records, seed, generationVersion, cells, settings,
            WorldResourceKind.Timber, 8,
            cell => cell.IsDryLand && cell.Biome == WorldBiomeType.Forest && !cell.Terrain.HasPath);
        AddRankedSpawns(records, seed, generationVersion, cells, settings,
            WorldResourceKind.Stone, 6,
            cell => cell.IsDryLand && cell.Biome == WorldBiomeType.Highland && !cell.Terrain.HasPath);
        AddRankedSpawns(records, seed, generationVersion, cells, settings,
            WorldResourceKind.Fish, 4,
            cell => cell.WaterType == WorldWaterType.Pond ||
                    cell.WaterType == WorldWaterType.River);
        return records
            .OrderBy(record => record.Kind)
            .ThenBy(record => record.Coordinate.y)
            .ThenBy(record => record.Coordinate.x)
            .ToArray();
    }

    static void AddRankedSpawns(
        List<WorldResourceSpawnRecord> output,
        long seed,
        int generationVersion,
        WorldGeneratedCell[] cells,
        WorldIslandGenerationSettings settings,
        WorldResourceKind kind,
        int count,
        Func<WorldGeneratedCell, bool> predicate)
    {
        var candidates = new List<(uint rank, Vector2Int coordinate)>();
        foreach (WorldGeneratedCell cell in cells)
        {
            if (!predicate(cell)) continue;
            Vector2Int coordinate = cell.Terrain.Coordinate;
            uint rank = Hash(seed, coordinate.x, coordinate.y,
                ResourceSalt + (uint)kind * 0x9E3779B9u);
            candidates.Add((rank, coordinate));
        }

        foreach ((uint rank, Vector2Int coordinate) candidate in candidates
                     .OrderBy(candidate => candidate.rank)
                     .ThenBy(candidate => candidate.coordinate.y)
                     .ThenBy(candidate => candidate.coordinate.x)
                     .Take(count))
        {
            string key = $"g{generationVersion}:{unchecked((ulong)seed):X16}:{kind}:" +
                         $"{candidate.coordinate.x}:{candidate.coordinate.y}";
            output.Add(new WorldResourceSpawnRecord(key, kind, candidate.coordinate));
        }
    }

    static ulong ComputeChecksum(
        long seed,
        WorldIslandGenerationSettings settings,
        WorldGeneratedCell[] cells,
        WorldGenerationAnchor[] anchors,
        WorldResourceSpawnRecord[] spawns)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        unchecked
        {
            Add(ref hash, (ulong)seed, prime);
            Add(ref hash, (uint)settings.GenerationVersion, prime);
            Add(ref hash, (uint)settings.WidthCells, prime);
            Add(ref hash, (uint)settings.HeightCells, prime);
            foreach (WorldGeneratedCell cell in cells)
            {
                WorldCellData terrain = cell.Terrain;
                Add(ref hash, (uint)terrain.Coordinate.x, prime);
                Add(ref hash, (uint)terrain.Coordinate.y, prime);
                Add(ref hash, (uint)terrain.ElevationLevel, prime);
                Add(ref hash, (uint)terrain.GroundType, prime);
                Add(ref hash, (uint)terrain.PathType, prime);
                Add(ref hash, (uint)terrain.WaterSurfaceLevel, prime);
                Add(ref hash, (uint)terrain.WaterDepthLevels, prime);
                Add(ref hash, (uint)cell.Biome, prime);
                Add(ref hash, (uint)cell.WaterType, prime);
            }
            foreach (WorldGenerationAnchor anchor in anchors)
            {
                Add(ref hash, (uint)anchor.Kind, prime);
                Add(ref hash, (uint)anchor.Coordinate.x, prime);
                Add(ref hash, (uint)anchor.Coordinate.y, prime);
                Add(ref hash, (uint)anchor.EntranceCoordinate.x, prime);
                Add(ref hash, (uint)anchor.EntranceCoordinate.y, prime);
            }
            foreach (WorldResourceSpawnRecord spawn in spawns)
            {
                Add(ref hash, (uint)spawn.Kind, prime);
                Add(ref hash, (uint)spawn.Coordinate.x, prime);
                Add(ref hash, (uint)spawn.Coordinate.y, prime);
                foreach (char character in spawn.SpawnKey)
                    Add(ref hash, character, prime);
            }
        }
        return hash;
    }

    static void Add(ref ulong hash, ulong value, ulong prime)
    {
        hash = (hash ^ value) * prime;
    }

    static int SampleValueNoise(long seed, int x, int z, int scale, uint salt)
    {
        int gridX = x / scale;
        int gridZ = z / scale;
        int localX = x % scale;
        int localZ = z % scale;
        int tx = SmoothQ16(localX * 65536 / scale);
        int tz = SmoothQ16(localZ * 65536 / scale);
        int southWest = SignedNoise(seed, gridX, gridZ, salt);
        int southEast = SignedNoise(seed, gridX + 1, gridZ, salt);
        int northWest = SignedNoise(seed, gridX, gridZ + 1, salt);
        int northEast = SignedNoise(seed, gridX + 1, gridZ + 1, salt);
        int south = LerpQ16(southWest, southEast, tx);
        int north = LerpQ16(northWest, northEast, tx);
        return LerpQ16(south, north, tz);
    }

    static int SmoothQ16(int value)
    {
        long t = value;
        long tSquared = t * t >> 16;
        return (int)(tSquared * (3L * 65536L - 2L * t) >> 16);
    }

    static int LerpQ16(int a, int b, int t)
    {
        return a + (int)(((long)(b - a) * t) >> 16);
    }

    static int SignedNoise(long seed, int x, int z, uint salt)
    {
        return (int)(Hash(seed, x, z, salt) & 0xFFFFu) - 32768;
    }

    static int HashRange(
        long seed,
        int x,
        int z,
        uint salt,
        int minimumInclusive,
        int maximumExclusive)
    {
        uint range = (uint)(maximumExclusive - minimumInclusive);
        return minimumInclusive + (int)(Hash(seed, x, z, salt) % range);
    }

    static uint Hash(long seed, int x, int z, uint salt)
    {
        unchecked
        {
            ulong value = (ulong)seed;
            value ^= (ulong)(uint)x * 0x9E3779B185EBCA87UL;
            value ^= (ulong)(uint)z * 0xC2B2AE3D27D4EB4FUL;
            value ^= salt;
            value += 0x9E3779B97F4A7C15UL;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            value ^= value >> 31;
            return (uint)(value ^ (value >> 32));
        }
    }

    static bool IsValid(
        WorldIslandGenerationSettings settings,
        Vector2Int coordinate)
    {
        return coordinate.x >= 0 && coordinate.x < settings.WidthCells &&
               coordinate.y >= 0 && coordinate.y < settings.HeightCells;
    }
}

public static class WorldGenerationConnectivity
{
    static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.left,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.up
    };

    public static bool CanReachAllAnchors(
        WorldGenerationResult result,
        out int reachableCellCount)
    {
        if (result == null ||
            !result.TryGetAnchor(WorldGenerationAnchorKind.Start, out WorldGenerationAnchor start))
        {
            reachableCellCount = 0;
            return false;
        }

        bool[] visited = FloodFill(result, start.Coordinate, out reachableCellCount);
        foreach (WorldGenerationAnchor anchor in result.Anchors)
        {
            Vector2Int target = anchor.Kind == WorldGenerationAnchorKind.Shop
                ? anchor.EntranceCoordinate
                : anchor.Coordinate;
            if (!TryIndex(result.Definition, target, out int index) || !visited[index])
                return false;
        }
        return true;
    }

    public static bool CanTraverse(
        WorldGenerationResult result,
        Vector2Int from,
        Vector2Int to)
    {
        if (result == null || !TryIndex(result.Definition, to, out int targetIndex)) return false;
        bool[] visited = FloodFill(result, from, out _);
        return visited[targetIndex];
    }

    static bool[] FloodFill(
        WorldGenerationResult result,
        Vector2Int start,
        out int reachableCellCount)
    {
        var visited = new bool[result.Definition.TotalCellCount];
        reachableCellCount = 0;
        if (!result.TryGetCell(start, out WorldGeneratedCell startCell) ||
            !IsTraversable(startCell))
        {
            return visited;
        }

        var queue = new Queue<Vector2Int>();
        int startIndex = start.y * result.Definition.Width + start.x;
        visited[startIndex] = true;
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            reachableCellCount++;
            result.TryGetCell(current, out WorldGeneratedCell currentCell);
            foreach (Vector2Int direction in CardinalDirections)
            {
                Vector2Int next = current + direction;
                if (!TryIndex(result.Definition, next, out int nextIndex) || visited[nextIndex] ||
                    !result.TryGetCell(next, out WorldGeneratedCell nextCell) ||
                    !IsTraversable(nextCell) ||
                    Mathf.Abs(nextCell.Terrain.ElevationLevel -
                              currentCell.Terrain.ElevationLevel) > 1)
                {
                    continue;
                }
                visited[nextIndex] = true;
                queue.Enqueue(next);
            }
        }
        return visited;
    }

    static bool IsTraversable(WorldGeneratedCell cell)
    {
        return cell.IsDryLand && cell.Terrain.IsWalkable;
    }

    static bool TryIndex(
        WorldGridDefinition definition,
        Vector2Int coordinate,
        out int index)
    {
        if (coordinate.x < 0 || coordinate.x >= definition.Width ||
            coordinate.y < 0 || coordinate.y >= definition.Height)
        {
            index = -1;
            return false;
        }
        index = coordinate.y * definition.Width + coordinate.x;
        return true;
    }
}
