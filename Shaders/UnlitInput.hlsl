#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED
TEXTURE2D(_BaseMap);
TEXTURE2D(_DistortionMap);
SAMPLER(sampler_BaseMap);


UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeDistance)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeRange)
    UNITY_DEFINE_INSTANCED_PROP(float, _SoftParticlesDistance)
    UNITY_DEFINE_INSTANCED_PROP(float, _SoftParticlesRange)
    UNITY_DEFINE_INSTANCED_PROP(float, _DistortionStrength)
    UNITY_DEFINE_INSTANCED_PROP(float, _DistortionBlend)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)
struct InputConfig
{
    Fragment fragment;
    float4 color;
    float2 baseUV;
    float3 flipbookUVB;
    bool flipbookBlending;
    bool nearFade;
    bool softParticles;
};
InputConfig GetInputConfig(float4 positionSS, float2 baseUV)
{
    InputConfig c;
    c.fragment = GetFragment(positionSS);
    c.color = 1.0;
    c.baseUV = baseUV;
    c.flipbookUVB = 0.0;
    c.flipbookBlending = false;
    c.nearFade = false;
    c.softParticles = false;
    return c;
}
float4 GetBase(InputConfig c)
{
    float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.baseUV);
    if (c.flipbookBlending)
    {
        map = lerp(map, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.flipbookUVB.xy), c.flipbookUVB.z);
    }
    if (c.nearFade)
    {
        float nearAttenuation = (c.fragment.depth - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NearFadeDistance)) / UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NearFadeRange);
        map.a *= saturate(nearAttenuation);
    }
    if (c.softParticles)
    {
        float depthDelta = c.fragment.bufferDepth - c.fragment.depth;
        float nearAttenuation = (depthDelta - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SoftParticlesDistance)) / UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SoftParticlesRange);
        map.a *= saturate(nearAttenuation);
    }
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    return map * color * c.color;
}
float GetCutoff(InputConfig c)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}
float GetMetallic(InputConfig c)
{
    return 0.0;
}
float GetSmoothness(InputConfig c)
{
    return 0.0;
}
float3 GetEmission (InputConfig c) {
    return GetBase(c).rgb;
}
float GetFresnel (InputConfig c) {
    return 0.0;
}
float GetFinalAlpha(float alpha)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_ZWrite) ? 1.0 : alpha;
}
float2 TransformBaseUV(float2 baseUV)
{
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}
float2 GetDistortion(InputConfig c)
{
    float4 rawMap = SAMPLE_TEXTURE2D(_DistortionMap, sampler_linear_clamp, c.baseUV);
    if (c.flipbookBlending)
    {
        rawMap = lerp(rawMap, SAMPLE_TEXTURE2D(_DistortionMap, sampler_linear_clamp, c.flipbookUVB.xy), c.flipbookUVB.z);
    }
    return DecodeNormal(rawMap, UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DistortionStrength)).xy;
}
float GetDistortionBlend(InputConfig c)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DistortionBlend);
}
#endif