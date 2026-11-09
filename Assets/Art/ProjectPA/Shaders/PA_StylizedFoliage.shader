// Project P.A. instanced grass/seaweed/bush foliage. Presentation only.
// Two-sided blades lit with up-bent normals (no black back faces), dark root -> bright warm tip
// gradient like the reference grass clumps, soft sun translucency, gentle wind sway and
// received/cast shadows. Works with Graphics.RenderMeshInstanced (GPU instancing).
Shader "PA/StylizedFoliage"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _RootColor ("Root Color", Color) = (0.30, 0.42, 0.16, 1)
        _TipColor ("Tip Color", Color) = (0.80, 0.84, 0.36, 1)
        _GradientHeight ("Gradient Height (m)", Float) = 0.45
        _Wind ("Wind Sway (m)", Float) = 0.06
        _WindSpeed ("Wind Speed", Float) = 1.6
        _NormalBend ("Up Bend", Range(0, 1)) = 0.65
        _Translucency ("Translucency", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _RootColor;
            half4 _TipColor;
            float _GradientHeight;
            float _Wind;
            float _WindSpeed;
            half _NormalBend;
            half _Translucency;
        CBUFFER_END

        // World pivot of the current instance (translation column of the object matrix).
        float3 PA_Pivot() { return float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23); }

        float3 PA_Sway(float3 positionWS, out float heightFrac)
        {
            float3 pivot = PA_Pivot();
            heightFrac = saturate((positionWS.y - pivot.y) / max(_GradientHeight, 0.01));
            float phase = _Time.y * _WindSpeed + pivot.x * 0.71 + pivot.z * 0.53;
            float sway = sin(phase) + 0.35 * sin(phase * 2.3 + 1.7);
            float bend = heightFrac * heightFrac * _Wind;
            positionWS.x += sway * bend;
            positionWS.z += cos(phase * 0.8) * bend * 0.6;
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half heightFrac : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                float3 pivot : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float h;
                positionWS = PA_Sway(positionWS, h);
                o.positionWS = positionWS;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.heightFrac = h;
                o.pivot = PA_Pivot();
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings input, bool frontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 n = normalize(input.normalWS);
                n = frontFace ? n : -n;
                n = normalize(lerp(n, half3(0, 1, 0), _NormalBend));

                half h = input.heightFrac;
                half3 albedo = lerp(_RootColor.rgb, _TipColor.rgb, smoothstep(0.0, 1.0, h)) * _BaseColor.rgb;
                // Per-clump hue jitter so neighbouring tufts do not read as one flat mass.
                float jitter = frac(sin(dot(input.pivot.xz, float2(12.9898, 78.233))) * 43758.5453);
                albedo *= lerp(0.9, 1.08, jitter);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light sun = GetMainLight(shadowCoord, input.positionWS, half4(1, 1, 1, 1));
                half shadow = sun.shadowAttenuation * sun.distanceAttenuation;
                // Wide wrap: tufts read lighter than the ground they stand on (reference 02/12/13/14).
                half ndl = saturate(dot(n, sun.direction) * 0.45 + 0.6);
                half3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                half back = saturate(dot(-viewDir, sun.direction)) * _Translucency * h;
                half3 ambient = SampleSH(half3(0, 1, 0));
                half occlusion = lerp(0.72, 1.1, h);
                half3 color = albedo * (ambient * occlusion + sun.color * (ndl + back) * shadow);

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
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vertShadow(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float h;
                positionWS = PA_Sway(positionWS, h);
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
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vertDepth(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                float h;
                o.positionCS = TransformWorldToHClip(PA_Sway(TransformObjectToWorld(input.positionOS.xyz), h));
                return o;
            }

            half fragDepth(Varyings input) : SV_Target { return input.positionCS.z; }
            ENDHLSL
        }
    }

    FallBack Off
}
