// Project P.A. opening island ground/cliff/path surface.
// Presentation only: reads a per-cell ground-type map (+ elevation map) published by
// WorldGeneratedIslandDebugView and draws soft, noisy biome borders, leaf-print grass,
// rippled sand, layered cliff sides with a grass lip, and seabed caustics.
// Cell data, colliders and gameplay are untouched.
Shader "PA/StylizedWorldSurface"
{
    Properties
    {
        _BaseColor ("Base Color (fallback/path)", Color) = (0.5, 0.6, 0.35, 1)
        _SurfaceKind ("Kind 0 Top 1 Cliff 2 Dirt Path 3 Stone Path", Float) = 0
        _GrassA ("Grass Shade", Color) = (0.42, 0.60, 0.24, 1)
        _GrassB ("Grass Light", Color) = (0.62, 0.74, 0.30, 1)
        _SandA ("Sand", Color) = (0.93, 0.80, 0.56, 1)
        _SoilA ("Soil", Color) = (0.66, 0.46, 0.31, 1)
        _RockA ("Rock", Color) = (0.62, 0.60, 0.55, 1)
        _CliffDarken ("Cliff Darken", Range(0.4, 1)) = 0.74
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _GrassA;
            half4 _GrassB;
            half4 _SandA;
            half4 _SoilA;
            half4 _RockA;
            half _CliffDarken;
            float _SurfaceKind;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "PA_StylizedCommon.hlsl"

            TEXTURE2D(_PA_SurfaceMap);
            TEXTURE2D(_PA_HeightMap);
            float4 _PA_SurfaceMapParams;   // origin corner x, origin corner z, 1/(W*cell), 1/(H*cell)
            float4 _PA_SurfaceMapSize;     // W, H, cell size, elevation step
            float _PA_WaterLevel;
            float _PA_SurfaceArtEnabled;
            float _PA_HeightMapEnabled;
            TEXTURE2D(_PA_DuneMap);
            float _PA_DuneMapEnabled;
            float4 _PA_DuneMapSize; // texture width/height, presentation tile size in metres

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            int2 CellOf(float2 xz)
            {
                float2 c = (xz - _PA_SurfaceMapParams.xy) / max(_PA_SurfaceMapSize.z, 1e-3);
                return clamp(int2(floor(c)), int2(0, 0), int2(_PA_SurfaceMapSize.xy) - 1);
            }

            float HeightAt(int2 cell)
            {
                return _PA_HeightMapEnabled > 0.5 ? LOAD_TEXTURE2D(_PA_HeightMap, cell).r : 0;
            }

            // Soft per-cell type weights. Bilinear across cell centres, jittered by noise so the
            // grass/sand/soil border wobbles like hand-painted tiles; never blends across a height step.
            float4 TypeWeights(float2 xz, int2 ownCell)
            {
                float cell = max(_PA_SurfaceMapSize.z, 1e-3);
                float2 jitter = (float2(PA_Fbm(xz * 0.42), PA_Fbm(xz * 0.42 + 31.7)) - 0.5) * 1.25;
                float2 q = (xz + jitter - _PA_SurfaceMapParams.xy) / cell - 0.5;
                int2 i0 = int2(floor(q));
                float2 f = frac(q);
                float ownHeight = HeightAt(ownCell);
                int2 maxCell = int2(_PA_SurfaceMapSize.xy) - 1;
                float4 sum = 0;
                float total = 0;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    int2 o = int2(k & 1, k >> 1);
                    int2 c = clamp(i0 + o, int2(0, 0), maxCell);
                    float w = (o.x == 1 ? f.x : 1 - f.x) * (o.y == 1 ? f.y : 1 - f.y);
                    if (abs(HeightAt(c) - ownHeight) > 0.01) w = 0;
                    sum += LOAD_TEXTURE2D(_PA_SurfaceMap, c) * w;
                    total += w;
                }
                float4 own = LOAD_TEXTURE2D(_PA_SurfaceMap, ownCell);
                float4 m = total > 1e-3 ? sum / total : own;
                float e = (PA_ValueNoise(xz * 2.1) - 0.5) * 0.16;
                float4 s = smoothstep(0.36 + e, 0.64 + e, m);
                float ss = dot(s, 1);
                return ss > 1e-3 ? s / ss : own;
            }

            half3 GrassColor(float2 xz)
            {
                half3 c = lerp(_GrassA.rgb, _GrassB.rgb, smoothstep(0.30, 0.78, PA_Fbm(xz * 0.16)));
                c *= lerp(0.94, 1.06, PA_ValueNoise(xz * 0.9));
                // Star-like leaf prints (reference grass top), alternating lighter/darker.
                float id;
                float4 v = PA_Voronoi(xz * 1.9, 0, id);
                float ang = atan2(v.w, v.z);
                float star = v.x * (0.78 + 0.22 * cos(5.0 * ang + id * 6.28));
                float print = 1 - smoothstep(0.13, 0.24, star);
                c *= 1 + print * (id > 0.5 ? 0.10 : -0.10);
                return c;
            }

            half3 SandColor(float2 xz)
            {
                half3 c = _SandA.rgb * lerp(0.96, 1.03, PA_Fbm(xz * 0.25));
                // Fine wind ripples (~0.35m), bent by noise and only in patches, so the beach
                // gets the reference's small low-contrast texture instead of long diagonal bands.
                float bendNoise = PA_Fbm(xz * 0.9);
                float ripple = sin(dot(xz, float2(0.78, 0.62)) * 17.0 + bendNoise * 9.0);
                float patch = smoothstep(0.35, 0.7, PA_Fbm(xz * 0.22 + 13.1));
                c *= 1 + 0.028 * ripple * patch;
                c *= lerp(0.975, 1.025, PA_Hash21(floor(xz * 14)));
                return c;
            }

            half3 SoilColor(float2 xz)
            {
                half3 c = _SoilA.rgb * lerp(0.9, 1.06, PA_Fbm(xz * 0.3));
                float id;
                float4 v = PA_Voronoi(xz * 2.6, 0, id);
                c *= 1 - (1 - smoothstep(0.10, 0.17, v.x)) * (id > 0.6 ? 0.16 : 0.0);
                return c;
            }

            half3 RockColor(float2 xz)
            {
                half3 c = _RockA.rgb * lerp(0.9, 1.05, PA_Fbm(xz * 0.4));
                float id;
                float4 v = PA_Voronoi(xz * 0.9, 0, id);
                c *= lerp(0.82, 1.0, smoothstep(0.02, 0.07, v.y));
                return c;
            }

            half3 TypeColor(float4 w, float2 xz)
            {
                half3 c = 0;
                if (w.x > 0.001) c += w.x * GrassColor(xz);
                if (w.y > 0.001) c += w.y * SandColor(xz);
                if (w.z > 0.001) c += w.z * SoilColor(xz);
                if (w.w > 0.001) c += w.w * RockColor(xz);
                // Darker trim on the grass side of a border, like the reference tile edges.
                float rim = saturate(4.0 * w.x * (1 - w.x));
                c *= 1 - 0.16 * rim;
                return c;
            }

            // Wet sand follows continuous distance to water-cell edges/corners (reference 03).
            // Fixed 0.4/0.8/1.2 m probes produced three flat stripes resembling a raised kerb.
            // The 1.3 m band fits inside the existing 2 m cells; smaller grids cap the band
            // at one cell so an immediate-neighbour search never misses a nearer second ring.
            half WetBand(float2 xz, int2 ownCell)
            {
                if (_PA_HeightMapEnabled < 0.5 || HeightAt(ownCell) < -50) return 0;
                float cellSize = max(_PA_SurfaceMapSize.z, 1e-3);
                float bandWidth = min(1.3, cellSize);
                float2 local = clamp(xz - _PA_SurfaceMapParams.xy - float2(ownCell) * cellSize,
                                     float2(0, 0), float2(cellSize, cellSize));
                float4 edgeDistance = float4(local.x, cellSize - local.x, local.y, cellSize - local.y);
                int2 limit = int2(_PA_SurfaceMapSize.xy) - 1;
                float nearestSq = bandWidth * bandWidth;

                // Cardinal water cells share a full edge. Candidates outside the current
                // nearest distance cannot affect the band and need no height-map read.
                [unroll] for (int wetAxisIndex = 0; wetAxisIndex < 4; wetAxisIndex++)
                {
                    int2 offset = wetAxisIndex == 0 ? int2(-1, 0) : wetAxisIndex == 1 ? int2(1, 0) :
                                  wetAxisIndex == 2 ? int2(0, -1) : int2(0, 1);
                    int2 neighbour = ownCell + offset;
                    float candidateSq = edgeDistance[wetAxisIndex] * edgeDistance[wetAxisIndex];
                    if (candidateSq < nearestSq && all(neighbour >= int2(0, 0)) && all(neighbour <= limit))
                        if (HeightAt(neighbour) < -50) nearestSq = candidateSq;
                }

                // A diagonal-only water cell meets dry land at a corner, not an axis edge.
                // Use Euclidean distance to that corner; skip it when an axis is nearer.
                [unroll] for (int wetCornerIndex = 0; wetCornerIndex < 4; wetCornerIndex++)
                {
                    int xSide = wetCornerIndex & 1, zSide = wetCornerIndex >> 1;
                    int2 neighbour = ownCell + int2(xSide == 0 ? -1 : 1, zSide == 0 ? -1 : 1);
                    float2 cornerDistance = float2(edgeDistance[xSide], edgeDistance[2 + zSide]);
                    float candidateSq = dot(cornerDistance, cornerDistance);
                    if (candidateSq < nearestSq && all(neighbour >= int2(0, 0)) && all(neighbour <= limit))
                        if (HeightAt(neighbour) < -50) nearestSq = candidateSq;
                }

                // Match the old near-shore mean (~0.69) without its quantised steps/noise.
                // Smooth value and slope at the dry edge; TopAlbedo retains the wet-sand tint.
                return 0.7 * (1 - smoothstep(0, bandWidth, sqrt(nearestSq)));
            }

            // R = actual retained tuft footprint/soil shoulder, G = grass interior.
            // Published on a finer visual lattice, independent of authoritative world cells.
            float2 DuneGround(int2 cell)
            {
                return LOAD_TEXTURE2D(_PA_DuneMap, clamp(cell, int2(0, 0), int2(_PA_DuneMapSize.xy) - 1)).rg;
            }

            // v22 (north-left strip, 2026-10-23 raycast: flat top, Sand elev0, dune shoulder/interior on boundary
            // cell 189): the soil choice is hashed per 3-cell world block while patches use the finer dune lattice,
            // so a soil block clipping a patch's boundary cells painted one-cell strips. v21 (patch in all four
            // neighbours) removed the strips but also every dirt area; dirt now needs soil-with-patch in at least
            // one x and one z neighbour, which keeps 2x2+ dirt areas and drops one-cell strips and staircases.
            // v23: patches are painted around tuft footprints on the finer lattice, so a cell's exact centre is
            // often unpainted inside a visible patch (v21/v22 removed ~95% of dirt). Test the four quarter points.
            bool PatchAtCellCentre(int2 worldCell)
            {
                float worldSize = max(_PA_SurfaceMapSize.z, 1e-3);
                float tile = max(_PA_DuneMapSize.z, 1e-3);
                float2 base = float2(worldCell) * worldSize / tile;
                float q = 0.25 * worldSize / tile, r = 0.75 * worldSize / tile;
                return DuneGround(int2(floor(base + float2(q, q)))).r > 0 || DuneGround(int2(floor(base + float2(r, q)))).r > 0
                    || DuneGround(int2(floor(base + float2(q, r)))).r > 0 || DuneGround(int2(floor(base + float2(r, r)))).r > 0;
            }

            // ~3-cell blocks pick soil or dry grass so colour areas read as solid shapes. Each row/column of
            // the block grid is shifted by 0-2 cells so blocks form stepped blobs instead of rectangles.
            // Integer hash: PA_Hash21 on lattice inputs is linearly correlated and clumps soil.
            float SoilBlockValue(int2 worldCell)
            {
                uint2 cu = uint2(worldCell);
                uint sx = ((cu.y * 2654435761u) >> 28) % 3u, sy = ((cu.x * 2246822519u) >> 28) % 3u;
                uint2 b = uint2((cu.x + sx) / 3u, (cu.y + sy) / 3u);
                uint h = (b.x * 73856093u) ^ (b.y * 19349663u) ^ 0x9E3779B9u;
                h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
                return (h & 0xFFFFu) / 65535.0;
            }

            bool SoilWithPatch(int2 worldCell)
            {
                return SoilBlockValue(worldCell) < 0.22 && PatchAtCellCentre(worldCell);
            }

            half3 TopAlbedo(float3 positionWS)
            {
                float2 xz = positionWS.xz;
                if (_PA_SurfaceArtEnabled < 0.5) return _BaseColor.rgb;
                int2 own = CellOf(xz);
                float4 w = TypeWeights(xz, own);
                half3 c = TypeColor(w, xz);
                half wet = WetBand(xz, own) * w.y;
                c *= lerp(half3(1, 1, 1), half3(0.80, 0.78, 0.76), wet);
                // v8 verifier (refs 02/14): dune clumps sit on a soil/dry-grass ground change, not plain sand.
                // v10 verifier (refs 02/03/14): solid areas with stepped, cell-snapped edges, not a see-through wash.
                // Per-cell mask published by CoastalVegetationField; v11 verifier: straight cell edges (ref 03/14), no fringe.
                if (_PA_DuneMapEnabled > 0.5 && w.y > 0.01)
                {
                    float cellSize = max(_PA_DuneMapSize.z, 1e-3);
                    float2 dunePosition = (xz - _PA_SurfaceMapParams.xy) / cellSize;
                    int2 patchCell = int2(floor(dunePosition));
                    int2 duneCell = own;
                    float2 ground = DuneGround(patchCell);
                    float patch = ground.r;
                    // Waterline strip stays bare wet sand (ref 03): no patch on, or beside, a cell touching water.
                    if (patch > 0)
                    {
                        // Painted trim on the patch side of any sand neighbour, like the reference tile lips.
                        float2 f = frac(dunePosition);
                        float edge = 1;
                        if (DuneGround(patchCell + int2(-1, 0)).r < 0.5) edge = min(edge, f.x);
                        if (DuneGround(patchCell + int2(1, 0)).r < 0.5) edge = min(edge, 1 - f.x);
                        if (DuneGround(patchCell + int2(0, -1)).r < 0.5) edge = min(edge, f.y);
                        if (DuneGround(patchCell + int2(0, 1)).r < 0.5) edge = min(edge, 1 - f.y);
                        float block = SoilBlockValue(duneCell);
                        // v11 verifier: the flat fill read as a blockout. Reuse the island's grained soil and
                        // leaf-print grass, the grass warmed toward Dinkum's ochre dry grass (refs 02/14).
                        half3 soil = SoilColor(xz) * half3(0.96, 0.95, 0.93);
                        half3 dryGrass = GrassColor(xz) * half3(1.22, 1.02, 0.7);
                        // Mostly grass with occasional dirt (refs 02/03), not dirt-dominated.
                        bool soilBlock = block < 0.22
                            && (SoilWithPatch(duneCell + int2(1, 0)) || SoilWithPatch(duneCell - int2(1, 0)))
                            && (SoilWithPatch(duneCell + int2(0, 1)) || SoilWithPatch(duneCell - int2(0, 1)));
                        half3 mosaic = soilBlock ? soil : dryGrass;
                        // v16 verifier (refs 02/14): an all-soil shoulder outlined every grass group. Keep soil in the
                        // shoulder only on the larger soil-leaning blocks so dirt reads as areas, not rings.
                        // v20 (r3 verifier: left arrival/coast patches still ringed): blocks 0.22-0.45 had soil shoulders
                        // around grass interiors. Use the interior threshold so a shoulder is soil only where the patch is.
                        half3 shoulder = soilBlock ? soil : dryGrass * 0.93;
                        mosaic = lerp(shoulder, mosaic, ground.g);
                        mosaic *= 1 - 0.10 * (1 - smoothstep(0.06, 0.18, edge));
                        c = lerp(c, mosaic, saturate(w.y * 2));
                    }
                }
                return c;
            }

            half3 CliffAlbedo(float3 positionWS, half3 normalWS)
            {
                float levelStep = max(_PA_SurfaceMapSize.w, 1e-3);
                float along = positionWS.x + positionWS.z;
                if (_PA_SurfaceArtEnabled < 0.5) return _BaseColor.rgb;
                // Upper cell = a quarter cell behind the face.
                int2 upper = CellOf(positionWS.xz - normalWS.xz * _PA_SurfaceMapSize.z * 0.25);
                float4 type = LOAD_TEXTURE2D(_PA_SurfaceMap, upper);
                half3 top = TypeColor(type, positionWS.xz);
                half3 side = (type.x > 0.5 ? half3(0.60, 0.43, 0.29) : top) * _CliffDarken;
                if (type.x > 0.5) side = lerp(side, _SoilA.rgb * _CliffDarken, 0.35);
                // Layered strata with wobbly bands.
                float y = positionWS.y / levelStep;
                float band = frac(y * 2.6 + PA_ValueNoise(float2(along * 0.6, y)) * 0.55);
                side *= lerp(0.90, 1.05, smoothstep(0.35, 0.65, band));
                side *= lerp(0.94, 1.04, PA_ValueNoise(float2(along * 3.0, y * 9.0)));
                // Grass/sand lip hanging over the edge with a jagged drip line.
                float topDist = (ceil(y - 0.001) - y) * levelStep;
                float lip = 0.13 + 0.11 * PA_ValueNoise(float2(along * 2.7, 0.5)) +
                            0.06 * PA_ValueNoise(float2(along * 9.0, 3.1));
                float lipMask = 1 - smoothstep(lip - 0.015, lip + 0.015, topDist);
                if (type.x > 0.5 || type.y > 0.5)
                    side = lerp(side, top * 0.9, lipMask);
                // Darker foot where the wall meets the lower ground.
                float foot = frac(y);
                side *= lerp(0.80, 1.0, smoothstep(0.0, 0.35, foot));
                // v16 (WORLD_V15 review: thin bright line at the water/sand contact): a shore face is only the
                // few cm above the water, so it was all light lip while the sand top beside it is wet-darkened.
                // Faces whose front cell is water get the same wet tone, strongest at the waterline.
                int2 front = CellOf(positionWS.xz + normalWS.xz * _PA_SurfaceMapSize.z * 0.25);
                if (_PA_HeightMapEnabled > 0.5 && HeightAt(front) < -50)
                {
                    // v16 verifier: the submerged part of the face must stay light like ref 03's sand steps,
                    // so the wet tone covers only the band just above the waterline.
                    half wetSide = (1 - smoothstep(_PA_WaterLevel + 0.02, _PA_WaterLevel + 0.45, positionWS.y))
                                 * step(_PA_WaterLevel - 0.01, positionWS.y);
                    side *= lerp(half3(1, 1, 1), half3(0.74, 0.72, 0.70), wetSide);
                }
                return side;
            }

            half3 PathAlbedo(float3 positionWS, bool stone)
            {
                float2 xz = positionWS.xz;
                half3 c = (stone ? _BaseColor.rgb : _SoilA.rgb * 1.08) * lerp(0.92, 1.05, PA_Fbm(xz * 0.5));
                float id;
                float4 v = PA_Voronoi(xz * (stone ? 1.6 : 3.2), 0, id);
                if (stone) c *= lerp(0.72, 1.0, smoothstep(0.03, 0.09, v.y)) * lerp(0.94, 1.06, id);
                else c *= 1 - (1 - smoothstep(0.08, 0.14, v.x)) * (id > 0.55 ? 0.14 : -0.06);
                return c;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                half3 albedo;
                if (_SurfaceKind > 2.5) albedo = PathAlbedo(input.positionWS, true);
                else if (_SurfaceKind > 1.5) albedo = PathAlbedo(input.positionWS, false);
                else if (_SurfaceKind > 0.5 || abs(n.y) < 0.5) albedo = CliffAlbedo(input.positionWS, n);
                else albedo = TopAlbedo(input.positionWS);

                half shadow;
                half3 color = PA_MainLighting(albedo, input.positionWS, n, shadow);

                // Underwater ground: animated sun caustics, visible through the clear shallow sea.
                float below = _PA_WaterLevel - input.positionWS.y;
                if (_PA_SurfaceArtEnabled > 0.5 && below > 0.02)
                {
                    float t = _Time.y;
                    float id;
                    float4 v1 = PA_Voronoi(input.positionWS.xz * 1.7 + float2(t * 0.07, t * 0.05), t * 0.8, id);
                    float4 v2 = PA_Voronoi(input.positionWS.xz * 2.6 - float2(t * 0.05, -t * 0.04) + 7.3, t * 0.6 + 1.7, id);
                    float caustic = (1 - smoothstep(0.0, 0.06, v1.y)) * 0.6 + (1 - smoothstep(0.0, 0.05, v2.y)) * 0.4;
                    float fade = saturate(1 - below / 3.5);
                    // Water absorbs red first: the seabed turns teal and darker with depth.
                    color *= lerp(half3(1, 1, 1), half3(0.50, 0.78, 0.80), saturate(below / 2.6));
                    color += caustic * fade * 0.18 * (0.35 + 0.65 * shadow) * _MainLightColor.rgb;
                }

                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vertShadow(Attributes input)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = positionCS;
                return o;
            }

            half4 fragShadow(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vertDepth(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half fragDepth(Varyings input) : SV_Target { return input.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vertDN
            #pragma fragment fragDN

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; };

            Varyings vertDN(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 fragDN(Varyings input) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS), 0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
