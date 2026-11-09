// Project P.A. opening island sea. Presentation only.
// Clear refracted shallows over the (visually sloped) seabed, depth-based turquoise -> teal,
// animated net-like surface lines, shoreline foam, soft sun glint and received shadows.
// Requires the URP depth + opaque textures (enabled on PC_RPAsset); falls back to vertex
// shore proximity when they are unavailable.
Shader "PA/StylizedWater"
{
    Properties
    {
        _BaseColor ("Base Color (unused, palette compat)", Color) = (0.22, 0.53, 0.59, 1)
        _ShallowTint ("Shallow Colour", Color) = (0.36, 0.84, 0.84, 1)
        _MidColor ("Mid Color", Color) = (0.06, 0.55, 0.62, 1)
        _DeepColor ("Deep Color", Color) = (0.04, 0.36, 0.48, 1)
        _FoamColor ("Foam Color", Color) = (0.97, 0.99, 0.96, 1)
        _LineColor ("Surface Line Color", Color) = (0.86, 1.0, 0.97, 1)
        _ClearDepth ("Clear Depth (m)", Float) = 0.9
        _DeepDepth ("Deep Depth (m)", Float) = 2.6
        _MaxOpacity ("Max Opacity", Range(0, 1)) = 0.9
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardWater"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "PA_StylizedCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShallowTint;
                half4 _MidColor;
                half4 _DeepColor;
                half4 _FoamColor;
                half4 _LineColor;
                float _ClearDepth;
                float _DeepDepth;
                half _MaxOpacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.color = input.color;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            // Returns vertical water thickness and view-ray thickness behind this surface pixel.
            float2 Thickness(float2 screenUV, float3 positionWS)
            {
                float3 camForward = -UNITY_MATRIX_V[2].xyz;
                float3 toFrag = positionWS - _WorldSpaceCameraPos;
                float surfaceEye = max(dot(toFrag, camForward), 1e-3);
                float sceneEye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float3 scenePos = _WorldSpaceCameraPos + toFrag / surfaceEye * sceneEye;
                return float2(max(0, positionWS.y - scenePos.y), max(0, (sceneEye - surfaceEye) * length(toFrag) / surfaceEye));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float3 positionWS = input.positionWS;
                float2 xz = positionWS.xz;
                half3 n = normalize(input.normalWS);
                bool top = n.y > 0.5;
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);

                // Vertex shore proximity (r) and foam band (g) from the presentation mesh.
                half shore = top ? input.color.r : 1;
                half band = top ? input.color.g : 0;

                float2 th = Thickness(screenUV, positionWS);
                float depth = th.x;
                bool hasDepth = depth <= 50;
                // Fallback when the depth texture is missing (far plane everywhere).
                if (!hasDepth) depth = lerp(_DeepDepth, _ClearDepth, shore);

                // v18 (north shore): the presentation seabed drops in cell-sized vertical steps, so the
                // per-pixel depth turned the shallow -> deep transition into square colour blocks. Body
                // colour/opacity use depth averaged over ~1.2 m neighbours; a neighbour counts only when
                // water is in front of the scene there, so land, rocks and characters add no halo.
                float colorDepth = depth;
                if (top && hasDepth)
                {
                    // Two 8-tap rings (0.6 m, 1.2 m; outer rotated 22.5 deg), centre-weighted, so the
                    // averaged step edges fall off smoothly instead of leaving offset ghost copies.
                    float sum = depth, weight = 1;
                    [unroll] for (int k = 0; k < 16; k++)
                    {
                        bool outer = k >= 8;
                        float angle = (k % 8) * 0.785398 + (outer ? 0.392699 : 0);
                        float radius = outer ? 1.2 : 0.6;
                        float3 tapWS = positionWS + float3(cos(angle) * radius, 0, sin(angle) * radius);
                        float4 tapSP = ComputeScreenPos(TransformWorldToHClip(tapWS));
                        float2 tapUV = tapSP.xy / tapSP.w;
                        if (any(tapUV < 0) || any(tapUV > 1)) continue;
                        float2 thN = Thickness(tapUV, tapWS);
                        float w = outer ? 0.5 : 0.75;
                        if (thN.y > 0.001 && thN.x <= 50) { sum += thN.x * w; weight += w; }
                    }
                    // v18c (r3 verifier: submerged rocks darkened/blurred): rocks and seaweed tops write depth
                    // too, so the average pulled them toward the deeper seabed around them. Limit how much
                    // deeper than its own depth a pixel may be coloured; the deep side of a terrace step can
                    // still lighten, which is what removed the square colour blocks. v18d: 0.5 m still darkened
                    // deep rocks (v18c verifier), so the margin is 0.2 m. P.A. trial value.
                    colorDepth = min(sum / weight, depth + 0.2);
                }

                // Gentle refraction that never pulls in geometry above the surface.
                float2 wobble = (float2(PA_ValueNoise(xz * 1.3 + t * 0.35), PA_ValueNoise(xz * 1.3 - t * 0.31 + 9.1)) - 0.5);
                float2 refractUV = screenUV + wobble * 0.014 * saturate(depth);
                float2 thR = Thickness(refractUV, positionWS);
                if (thR.y <= 0.001) refractUV = screenUV;
                half3 scene = SampleSceneColor(refractUV);

                // Presentation seabed: ~0.5 m sand shelf along the shore, dropping to ~2.9 m.
                // Shallow = clear turquoise over sand, then saturated teal, then deep teal-blue.
                half3 waterColor = lerp(_ShallowTint.rgb, _MidColor.rgb, smoothstep(0.35, 1.3, colorDepth));
                waterColor = lerp(waterColor, _DeepColor.rgb, smoothstep(1.3, _DeepDepth, colorDepth));
                // v6 verifier: submerged seaweed/rocks lost all colour; ease mid-depth opacity so objects
                // 1-2 m down keep their hue while open deep water still reads teal.
                // v11 (ref 03): the 0.5 m sand shelf must read as sand stairs under clear water, not a
                // pale cyan sheet, so shallow opacity/in-scatter stay low until the mid terrace.
                half opacity = lerp(0.16, _MaxOpacity, smoothstep(0.45, _DeepDepth + 0.8, colorDepth));
                // Transmitted seabed loses red with depth, and the water adds its own cyan in-scatter,
                // so orange sand reads warm turquoise (reference 03) instead of olive/khaki.
                scene *= lerp(half3(0.70, 0.96, 0.94), half3(0.45, 0.82, 0.88), saturate(colorDepth / 1.4));
                scene += _ShallowTint.rgb * 0.13 * saturate(colorDepth / 0.6);

                // Main light on the surface (with received shadow) for the water body colour.
                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                Light sun = GetMainLight(shadowCoord, positionWS, half4(1, 1, 1, 1));
                half shadow = sun.shadowAttenuation * sun.distanceAttenuation;
                half3 ambient = SampleSH(half3(0, 1, 0));
                half3 lightTerm = ambient * 0.75 + sun.color * saturate(sun.direction.y) * lerp(0.55, 1.0, shadow);

                // Body colour uses light intensity only, so the warm morning sun does not turn teal olive.
                half lightLum = dot(lightTerm, half3(0.299, 0.587, 0.114)) * 1.15;
                half3 color = lerp(scene, waterColor * lightLum, opacity);

                // Animated net-like surface lines (reference water surface pattern).
                // Small wavy cells (reference lines are ~0.5-1m apart and thin), domain-warped so they
                // read as ripples instead of polygon cracks.
                float id;
                float2 warp = (float2(PA_Fbm(xz * 0.7 + t * 0.12), PA_Fbm(xz * 0.7 - t * 0.1 + 5.2)) - 0.5) * 0.9;
                float4 v1 = PA_Voronoi((xz + warp) * 1.45 + float2(t * 0.06, t * 0.04), t * 0.7, id);
                float4 v2 = PA_Voronoi((xz - warp) * 2.3 + float2(-t * 0.05, t * 0.07) + 11.3, t * 0.6 + 2.0, id);
                float lines = (1 - smoothstep(0.006, 0.03, v1.y)) * 0.6 + (1 - smoothstep(0.005, 0.024, v2.y)) * 0.4;
                lines *= 0.22 * (1 - smoothstep(1.2, _DeepDepth + 0.4, colorDepth)) * lerp(0.6, 1.0, shadow);
                color += lines * _LineColor.rgb * sun.color * 0.55;

                // Shoreline foam: thin contact line from the depth buffer + a soft wobbling band
                // that breathes in and out along the coast.
                float edgeNoise = PA_ValueNoise(xz * 2.2 + t * 0.25);
                // v8 verifier: no visible foam. Contact line ~0.3 m plus a broken lace band that breathes
                // in and out along the shore (refs 03/05 show a light lace where sand meets water).
                float breathe = 0.5 + 0.5 * sin(t * 1.1 + PA_ValueNoise(xz * 0.35) * 6.28);
                // v10 verifier: a uniform 0.3-0.5 m white contact stripe read as a glass rim on straight
                // basin edges; keep it thin and broken so the lace carries the shoreline instead.
                float lace = smoothstep(0.45, 0.7, PA_ValueNoise(xz * 3.2 + float2(t * 0.15, -t * 0.12)));
                float contact = 1 - smoothstep(0.02, 0.10 + 0.10 * edgeNoise + 0.05 * breathe, th.y);
                // v16: still read as one uniform pale strip on straight basin edges. Break it into drifting
                // dashes (gaps reach zero) so the contact reads as lapping foam, not a glass rim.
                contact *= smoothstep(0.42, 0.62, PA_ValueNoise(xz * 2.4 - t * 0.25)) * 0.8;
                float bandFoam = smoothstep(0.55 + 0.25 * breathe, 0.95, band + (edgeNoise - 0.5) * 0.25) * lace;
                // v16 verifier (refs 03/05 show little or no foam): the lace band read as milky blobs along the
                // edge. Keep only its brightest cores, faintly.
                bandFoam = smoothstep(0.35, 0.8, bandFoam);
                float foam = saturate(contact * 0.85 + bandFoam * 0.3);
                color = lerp(color, _FoamColor.rgb * (ambient * 0.5 + sun.color * lerp(0.6, 1.0, shadow)), foam * 0.8);

                // Small sun glint on ripples.
                float3 viewDir = normalize(_WorldSpaceCameraPos - positionWS);
                float3 rippleN = normalize(float3(wobble.x * 0.35, 1, wobble.y * 0.35));
                float3 h = normalize(sun.direction + viewDir);
                float glint = pow(saturate(dot(rippleN, h)), 220) * 1.6 * shadow;
                color += glint * sun.color;

                if (!top) color *= 0.92;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
