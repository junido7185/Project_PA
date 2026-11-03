#ifndef PA_STYLIZED_COMMON_INCLUDED
#define PA_STYLIZED_COMMON_INCLUDED

// Project P.A. world art helpers: hand-written procedural noise (no external textures)
// and the shared main-light model used by the opening-island surface and water shaders.

float PA_Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float2 PA_Hash22(float2 p)
{
    float n = PA_Hash21(p);
    return float2(n, PA_Hash21(p + n + 17.17));
}

float PA_ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = PA_Hash21(i);
    float b = PA_Hash21(i + float2(1, 0));
    float c = PA_Hash21(i + float2(0, 1));
    float d = PA_Hash21(i + float2(1, 1));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float PA_Fbm(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    [unroll] for (int k = 0; k < 4; k++)
    {
        v += a * PA_ValueNoise(p);
        p = p * 2.03 + 17.1;
        a *= 0.5;
    }
    return v;
}

// x = distance to nearest feature, y = F2-F1 (cell edge), zw = vector from the nearest feature.
// cellId receives a stable random value for the nearest cell.
float4 PA_Voronoi(float2 p, float time, out float cellId)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float f1 = 8.0;
    float f2 = 8.0;
    float2 nearest = 0;
    cellId = 0;
    [unroll] for (int y = -1; y <= 1; y++)
    [unroll] for (int x = -1; x <= 1; x++)
    {
        float2 g = float2(x, y);
        float2 o = PA_Hash22(i + g);
        o = 0.5 + 0.42 * sin(time + 6.2831 * o);
        float2 r = g + o - f;
        float d = length(r);
        if (d < f1)
        {
            f2 = f1;
            f1 = d;
            nearest = r;
            cellId = PA_Hash21(i + g + 3.7);
        }
        else if (d < f2)
        {
            f2 = d;
        }
    }
    return float4(f1, f2 - f1, nearest);
}

// Main light with soft shadows + SH ambient. Shadows get a slightly cool tint like the
// reference's warm sun / blue-ish shade split.
half3 PA_MainLighting(half3 albedo, float3 positionWS, half3 normalWS, out half shadow)
{
    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    Light mainLight = GetMainLight(shadowCoord, positionWS, half4(1, 1, 1, 1));
    half ndl = saturate(dot(normalWS, mainLight.direction));
    shadow = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
    half3 ambient = SampleSH(normalWS);
    half lit = ndl * shadow;
    half3 shadeTint = lerp(half3(0.86, 0.92, 1.08), half3(1, 1, 1), lit);
    return albedo * (ambient * shadeTint + mainLight.color * lit);
}

#endif
