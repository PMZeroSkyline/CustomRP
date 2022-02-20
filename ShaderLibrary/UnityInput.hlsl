#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);
TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);
TEXTURECUBE(unity_SpecCube0);
SAMPLER(samplerunity_SpecCube0);

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade; // x is the fade value ranging within [0,1]. y is x quantized into 16 levels
    float4 unity_WorldTransformParams; // w is usually 1.0, or -1.0 for odd-negative scale transforms
    float4 unity_RenderingLayer;

    real4 unity_LightData;
    real4 unity_LightIndices[2];

    float4 unity_ProbesOcclusion;
    float4 unity_SpecCube0_HDR;

    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;

    float4 unity_OrthoParams;
    float4 _ProjectionParams;
    float4 _ScreenParams;
    float4 _ZBufferParams;



    float4 unity_SHAr;
    float4 unity_SHAg;
    float4 unity_SHAb;
    float4 unity_SHBr;
    float4 unity_SHBg;
    float4 unity_SHBb;
    float4 unity_SHC;

    float4 unity_ProbeVolumeParams;
    float4x4 unity_ProbeVolumeWorldToObject;
    float4 unity_ProbeVolumeSizeInv;
    float4 unity_ProbeVolumeMin;
CBUFFER_END

TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
SAMPLER(samplerunity_ProbeVolumeSH);

CBUFFER_START(UnityPerFrame)

    fixed4 glstate_lightmodel_ambient;
    fixed4 unity_AmbientSky;
    fixed4 unity_AmbientEquator;
    fixed4 unity_AmbientGround;
    fixed4 unity_IndirectSpecColor;
    
#if !defined(USING_STEREO_MATRICES)
    float4x4 glstate_matrix_projection;
    float4x4 unity_MatrixV;
    float4x4 unity_MatrixInvV;
    float4x4 unity_MatrixVP;
    int unity_StereoEyeIndex;
    #endif
    
    fixed4 unity_ShadowColor;
    float3 _WorldSpaceCameraPos;

CBUFFER_END

#endif