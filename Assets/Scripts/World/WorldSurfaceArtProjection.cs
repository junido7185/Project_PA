using System;
using System.Collections.Generic;
using UnityEngine;

// Demo256 오프닝 전용 표현 계층. cell 데이터·collider·NavMesh·저장 데이터는 읽기만 하고
// 시각 메시 복사본과 셰이더 입력(surface map, 전역 값)만 만든다.
public static class WorldSurfaceArtProjection
{
    public const int MaxShoreDistance = 12;
    // 해안 단붕(Dinkum ref 03·05): 해안선에서 bed를 ShelfRaise만큼 올려 수면 아래 얕은 모래 띠를 만들고,
    // ShelfEndCells..DropEndCells 사이에서 ShelfDrop만큼 떨어뜨려 깊은 물로 넘어간다.
    public const float ShelfRaise = 0.5f;
    public const float ShelfDrop = 2.4f;
    public const float ShelfEndCells = 1.2f;
    public const float DropEndCells = 5f;
    public const float ShoreTintFalloffCells = 6f;

    // 올린 bed가 corner의 가장 낮은 육지 top과 겹치지 않게 남기는 여유.
    const float ShelfLandClearance = 0.02f;
    const float HeightEpsilon = 0.0001f;
    const float CornerEpsilon = 0.001f;
    const float UpNormalThreshold = 0.99f;

    static readonly int SurfaceMapId = Shader.PropertyToID("_PA_SurfaceMap");
    static readonly int HeightMapId = Shader.PropertyToID("_PA_HeightMap");
    static readonly int SurfaceMapParamsId = Shader.PropertyToID("_PA_SurfaceMapParams");
    static readonly int SurfaceMapSizeId = Shader.PropertyToID("_PA_SurfaceMapSize");
    static readonly int WaterLevelId = Shader.PropertyToID("_PA_WaterLevel");
    static readonly int SurfaceArtEnabledId = Shader.PropertyToID("_PA_SurfaceArtEnabled");
    static readonly int HeightMapEnabledId = Shader.PropertyToID("_PA_HeightMapEnabled");

    static readonly Color32 White = new Color32(255, 255, 255, 255);
    static readonly Color32 WaterSide = new Color32(255, 0, 0, 255);

    public sealed class ShoreField
    {
        readonly float[] _cellBedHeights;
        readonly float[] _cellLandTops;

        internal ShoreField(WorldGridDefinition definition, float[] cornerDistances, float[] cellBedHeights,
            float[] cellLandTops)
        {
            Definition = definition;
            CornerDistances = cornerDistances;
            _cellBedHeights = cellBedHeights;
            _cellLandTops = cellLandTops;
        }

        public WorldGridDefinition Definition { get; }

        // (Width+1)*(Height+1), row-major. 육지에 닿은 corner는 0, 물만 닿은 corner는 1..MaxShoreDistance.
        public float[] CornerDistances { get; }

        public float CornerDistance(int i, int j) => CornerDistances[j * (Definition.Width + 1) + i];

        public bool TryGetCorner(Vector3 position, out int i, out int j)
        {
            float cellSize = Definition.CellSize;
            float fx = (position.x - Definition.WorldOrigin.x) / cellSize + 0.5f;
            float fz = (position.z - Definition.WorldOrigin.z) / cellSize + 0.5f;
            i = Mathf.RoundToInt(fx);
            j = Mathf.RoundToInt(fz);
            return Mathf.Abs(fx - i) <= CornerEpsilon && Mathf.Abs(fz - j) <= CornerEpsilon &&
                   i >= 0 && i <= Definition.Width && j >= 0 && j <= Definition.Height;
        }

        // corner에 닿은 물 cell 중 하나의 시각 바닥 높이(OpeningWaterHeight - ElevationStep)와 y가 같은가.
        public bool TouchesBedHeight(int i, int j, float y)
        {
            int width = Definition.Width;
            for (int dz = -1; dz <= 0; dz++)
            {
                int z = j + dz;
                if (z < 0 || z >= Definition.Height) continue;
                for (int dx = -1; dx <= 0; dx++)
                {
                    int x = i + dx;
                    if (x < 0 || x >= width) continue;
                    float bedY = _cellBedHeights[z * width + x];
                    if (!float.IsNaN(bedY) && Mathf.Abs(bedY - y) <= HeightEpsilon) return true;
                }
            }
            return false;
        }

        // corner에 닿은 육지 cell top 중 가장 낮은 높이. 육지가 없으면 +∞.
        public float LowestLandTop(int i, int j)
        {
            int width = Definition.Width;
            float lowest = float.PositiveInfinity;
            for (int dz = -1; dz <= 0; dz++)
            {
                int z = j + dz;
                if (z < 0 || z >= Definition.Height) continue;
                for (int dx = -1; dx <= 0; dx++)
                {
                    int x = i + dx;
                    if (x < 0 || x >= width) continue;
                    float topY = _cellLandTops[z * width + x];
                    if (!float.IsNaN(topY)) lowest = Mathf.Min(lowest, topY);
                }
            }
            return lowest;
        }
    }

    public readonly struct ChunkArt
    {
        public ChunkArt(Vector3[] vertices, Color32[] colors, Bounds bounds)
        {
            Vertices = vertices;
            Colors = colors;
            Bounds = bounds;
        }

        public Vector3[] Vertices { get; }
        public Color32[] Colors { get; }
        public Bounds Bounds { get; }
    }

    public static Texture2D BuildSurfaceMap(WorldGridDefinition definition, IReadOnlyList<WorldCellData> cells)
    {
        ValidateInput(definition, cells);
        var texture = new Texture2D(definition.Width, definition.Height, TextureFormat.RGBA32, false, true)
        {
            name = "PA_SurfaceMap_Runtime",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        var pixels = new Color32[definition.TotalCellCount];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = SurfaceWeights(cells[i]);
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    // 셰이더가 단차 너머로 지면 종류를 섞지 않도록 cell 높이 단계를 그대로 싣는다(물 cell은 시각 바닥 단계).
    public static Texture2D BuildHeightMap(WorldGridDefinition definition, IReadOnlyList<WorldCellData> cells)
    {
        ValidateInput(definition, cells);
        var texture = new Texture2D(definition.Width, definition.Height, TextureFormat.RFloat, false, true)
        {
            name = "PA_HeightMap_Runtime",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        var pixels = new Color[definition.TotalCellCount];
        for (int i = 0; i < pixels.Length; i++)
        {
            WorldCellData cell = cells[i];
            pixels[i] = new Color(cell.HasWater ? -100f : cell.ElevationLevel, 0f, 0f, 1f);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    // R grass, G sand, B soil/dirt path, A rock/stone path. 물 cell은 모래 바닥으로 읽힌다.
    static Color32 SurfaceWeights(WorldCellData cell)
    {
        if (cell.HasWater) return new Color32(0, 255, 0, 0);
        if (cell.PathType == WorldPathType.Dirt) return new Color32(0, 0, 255, 0);
        if (cell.PathType == WorldPathType.Stone) return new Color32(0, 0, 0, 255);
        switch ((int)cell.GroundType)
        {
            case 1: return new Color32(0, 0, 255, 0);
            case 2: return new Color32(0, 255, 0, 0);
            case 3: return new Color32(0, 0, 0, 255);
            default: return new Color32(255, 0, 0, 0);
        }
    }

    public static ShoreField BuildShoreField(WorldGridDefinition definition, IReadOnlyList<WorldCellData> cells)
    {
        ValidateInput(definition, cells);
        int width = definition.Width;
        int height = definition.Height;
        int count = definition.TotalCellCount;
        var distances = new int[count];
        var bedHeights = new float[count];
        var landTops = new float[count];
        var queue = new int[count];
        int head = 0;
        int tail = 0;
        for (int i = 0; i < count; i++)
        {
            WorldCellData cell = cells[i];
            if (cell.HasWater)
            {
                distances[i] = -1;
                bedHeights[i] = WorldChunkMeshBuilder.OpeningWaterHeight(definition, cell) -
                                definition.ElevationStep;
                landTops[i] = float.NaN;
            }
            else
            {
                distances[i] = 0;
                bedHeights[i] = float.NaN;
                landTops[i] = definition.WorldOrigin.y + cell.ElevationLevel * definition.ElevationStep;
                queue[tail++] = i;
            }
        }

        // 모든 육지 cell을 출발점으로 하는 4방향 BFS. O(cells).
        while (head < tail)
        {
            int index = queue[head++];
            int next = distances[index] + 1;
            if (next > MaxShoreDistance) continue;
            int x = index % width;
            int z = index / width;
            if (x > 0) Enqueue(distances, queue, ref tail, index - 1, next);
            if (x < width - 1) Enqueue(distances, queue, ref tail, index + 1, next);
            if (z > 0) Enqueue(distances, queue, ref tail, index - width, next);
            if (z < height - 1) Enqueue(distances, queue, ref tail, index + width, next);
        }
        for (int i = 0; i < count; i++)
        {
            if (distances[i] < 0) distances[i] = MaxShoreDistance;
        }

        var corners = new float[(width + 1) * (height + 1)];
        for (int j = 0; j <= height; j++)
        {
            for (int i = 0; i <= width; i++)
            {
                int best = MaxShoreDistance;
                for (int dz = -1; dz <= 0; dz++)
                {
                    int z = j + dz;
                    if (z < 0 || z >= height) continue;
                    for (int dx = -1; dx <= 0; dx++)
                    {
                        int x = i + dx;
                        if (x < 0 || x >= width) continue;
                        best = Mathf.Min(best, distances[z * width + x]);
                    }
                }
                corners[j * (width + 1) + i] = best;
            }
        }
        return new ShoreField(definition, corners, bedHeights, landTops);
    }

    static void Enqueue(int[] distances, int[] queue, ref int tail, int index, int distance)
    {
        if (distances[index] >= 0) return;
        distances[index] = distance;
        queue[tail++] = index;
    }

    // Build 결과를 복사해 물 cell의 시각 바닥만 해안 단붕 프로필로 옮기고 vertex color를 채운다. O(vertices).
    // offset은 아래쪽이 양수라 해안 단붕(올림)은 음수다. 적용 여부는 applied로만 판정한다.
    public static ChunkArt ProjectChunk(ShoreField field, WorldChunkMeshData data)
    {
        if (field == null) throw new ArgumentNullException(nameof(field));
        if (data == null) throw new ArgumentNullException(nameof(data));

        Vector3[] source = data.Vertices;
        Vector3[] normals = data.Normals;
        var vertices = (Vector3[])source.Clone();
        var colors = new Color32[source.Length];
        for (int i = 0; i < colors.Length; i++) colors[i] = White;
        var offsets = new float[source.Length];
        var applied = new bool[source.Length];
        var visited = new bool[source.Length];

        // 1) 지면 slot의 위쪽 면: 물 cell 바닥 quad만 bed 높이와 corner 조건에 맞는다(육지에 닿은 corner 포함).
        //    육지 top·길은 정수 단 높이라 bed 높이(…-2.08 step)와 일치하지 않는다.
        for (int slot = 0; slot < WorldSurfaceMaterialSlots.Count; slot++)
        {
            if (slot == WorldSurfaceMaterialSlots.Cliff || slot == WorldSurfaceMaterialSlots.Water) continue;
            int[] indices = data.VisualSubmeshIndices[slot];
            for (int k = 0; k < indices.Length; k++)
            {
                int v = indices[k];
                if (visited[v]) continue;
                visited[v] = true;
                if (normals[v].y < UpNormalThreshold) continue;
                applied[v] = TryBedOffset(field, source[v], out offsets[v]);
            }
        }

        // 2) 절벽: bed 높이의 꼭짓점(육지 절벽 바닥, 물-물 단차, 맵 외곽 절벽 위쪽)은 같은 corner의 bed와 같은 offset을 받는다.
        //    bed 아래로 걸린 짝(맵 외곽 절벽 바닥)만 같은 xz의 offset을 따라가 면이 뒤집히지 않게 한다.
        //    bed 위의 짝(육지 top)은 움직이지 않는다.
        var quadOffsets = new float[4];
        var quadApplied = new bool[4];
        int[] cliff = data.VisualSubmeshIndices[WorldSurfaceMaterialSlots.Cliff];
        for (int k = 0; k + 5 < cliff.Length; k += 6)
        {
            int start = cliff[k];
            for (int q = 0; q < 4; q++) quadApplied[q] = TryBedOffset(field, source[start + q], out quadOffsets[q]);
            for (int q = 0; q < 4; q++)
            {
                if (quadApplied[q]) continue;
                for (int p = 0; p < 4; p++)
                {
                    if (p == q || !quadApplied[p] || source[start + q].y >= source[start + p].y ||
                        !SameColumn(source[start + q], source[start + p])) continue;
                    offsets[start + q] = quadOffsets[p];
                    applied[start + q] = true;
                    break;
                }
            }
            for (int q = 0; q < 4; q++)
            {
                if (!quadApplied[q]) continue;
                offsets[start + q] = quadOffsets[q];
                applied[start + q] = true;
            }
        }

        // 3) 물 slot: 수면은 해안 근접도/포말 띠, 옆면은 r=1,g=0. 옆면의 아래쪽 꼭짓점만 bed를 따라간다.
        int[] water = data.VisualSubmeshIndices[WorldSurfaceMaterialSlots.Water];
        for (int k = 0; k + 5 < water.Length; k += 6)
        {
            int start = water[k];
            if (normals[start].y >= UpNormalThreshold)
            {
                for (int q = 0; q < 4; q++)
                {
                    float distance = field.TryGetCorner(source[start + q], out int i, out int j)
                        ? field.CornerDistance(i, j)
                        : MaxShoreDistance;
                    colors[start + q] = new Color32(
                        ToByte(1f - Mathf.Clamp01(distance / ShoreTintFalloffCells)),
                        ToByte(Mathf.Clamp01(1f - distance)),
                        0,
                        255);
                }
                continue;
            }

            float bottomY = source[start].y;
            for (int q = 1; q < 4; q++) bottomY = Mathf.Min(bottomY, source[start + q].y);
            for (int q = 0; q < 4; q++)
            {
                colors[start + q] = WaterSide;
                if (Mathf.Abs(source[start + q].y - bottomY) <= HeightEpsilon)
                    applied[start + q] = TryBedOffset(field, source[start + q], out offsets[start + q]);
            }
        }

        Bounds bounds = data.Bounds;
        for (int v = 0; v < vertices.Length; v++)
        {
            if (!applied[v]) continue;
            vertices[v].y -= offsets[v];
            bounds.Encapsulate(vertices[v]);
        }
        return new ChunkArt(vertices, colors, bounds);
    }

    // 해안 corner 거리 → bed 기본 높이(OpeningWaterHeight - ElevationStep)에서 아래로 옮길 양. 음수는 올림.
    // 해안선(0..~1.2)은 ShelfRaise만큼 올라 얕은 단붕, DropEndCells 이후는 ShelfDrop - ShelfRaise만큼 깊다.
    // v5 critic(ref 03 계단식 해저): 매끈한 경사 대신 두 단의 테라스. corner 거리는 정수라
    // 0~1 = 0.5m 단붕, 2~3 = 1.3m 중간 단, 4+ = 2.9m 깊은 물이 되고 그 사이 한 cell만 급경사로 떨어진다.
    public const float TerraceMidDrop = 0.8f;
    public static float SeabedOffset(float cornerDistance)
    {
        return -ShelfRaise + TerraceMidDrop * SmoothStep(1.4f, 2f, cornerDistance) +
               (ShelfDrop - TerraceMidDrop) * SmoothStep(3.4f, 4f, cornerDistance);
    }

    // 물 cell 중심의 시각 해저 offset(그 cell이 닿는 corner 거리 k-1..k의 평균 근사). 수중 식생 배치용.
    public static float CellSeabedOffset(int cellDistance)
    {
        return 0.5f * (SeabedOffset(Mathf.Max(0, cellDistance - 1)) + SeabedOffset(cellDistance));
    }

    // bed 높이에 놓인 corner 꼭짓점이면 offset을 돌려준다. 올림은 corner의 가장 낮은 육지 top을 넘지 않는다.
    // 높이에 단조라 같은 corner의 여러 bed 높이(물-물 단차)도 순서가 뒤집히지 않는다.
    static bool TryBedOffset(ShoreField field, Vector3 position, out float offset)
    {
        offset = 0f;
        if (!field.TryGetCorner(position, out int i, out int j) || !field.TouchesBedHeight(i, j, position.y))
            return false;
        offset = SeabedOffset(field.CornerDistance(i, j));
        if (offset < 0f)
        {
            float ceiling = field.LowestLandTop(i, j) - ShelfLandClearance;
            offset = Mathf.Min(0f, Mathf.Max(offset, position.y - ceiling));
        }
        return true;
    }

    static bool SameColumn(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(a.x - b.x) <= CornerEpsilon && Mathf.Abs(a.z - b.z) <= CornerEpsilon;
    }

    // HLSL smoothstep(edge0,edge1,x). Mathf.SmoothStep은 보간 함수라 쓰지 않는다.
    static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    static byte ToByte(float value)
    {
        return (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
    }

    public static void ApplyGlobals(
        WorldGridDefinition definition,
        IReadOnlyList<WorldCellData> cells,
        Texture2D surfaceMap,
        Texture2D heightMap = null)
    {
        ValidateInput(definition, cells);
        float halfCell = definition.CellSize * 0.5f;
        float waterLevel = definition.WorldOrigin.y;
        for (int i = 0; i < cells.Count; i++)
        {
            WorldCellData cell = cells[i];
            if (!cell.HasWater) continue;
            waterLevel = WorldChunkMeshBuilder.OpeningWaterHeight(definition, cell);
            break;
        }
        Shader.SetGlobalTexture(SurfaceMapId, surfaceMap);
        Shader.SetGlobalVector(SurfaceMapParamsId, new Vector4(
            definition.WorldOrigin.x - halfCell,
            definition.WorldOrigin.z - halfCell,
            1f / (definition.Width * definition.CellSize),
            1f / (definition.Height * definition.CellSize)));
        Shader.SetGlobalVector(SurfaceMapSizeId, new Vector4(
            definition.Width, definition.Height, definition.CellSize, definition.ElevationStep));
        if (heightMap != null) Shader.SetGlobalTexture(HeightMapId, heightMap);
        Shader.SetGlobalFloat(HeightMapEnabledId, heightMap != null ? 1f : 0f);
        Shader.SetGlobalFloat(WaterLevelId, waterLevel);
        Shader.SetGlobalFloat(SurfaceArtEnabledId, 1f);
    }

    public static void DisableGlobals()
    {
        Shader.SetGlobalFloat(SurfaceArtEnabledId, 0f);
        Shader.SetGlobalFloat(HeightMapEnabledId, 0f);
    }

    static void ValidateInput(WorldGridDefinition definition, IReadOnlyList<WorldCellData> cells)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (cells == null) throw new ArgumentNullException(nameof(cells));
        if (cells.Count != definition.TotalCellCount)
            throw new ArgumentException("Cell count does not match the world definition.", nameof(cells));
    }
}
