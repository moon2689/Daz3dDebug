#ifndef UNIVERSAL_LIT_INPUT_INCLUDED
#define UNIVERSAL_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"


// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
half4 _BaseColor;
half _Cutoff;
float4 _HurtPos;
CBUFFER_END

TEXTURE2D(_MaskMROMap);         SAMPLER(sampler_MaskMROMap);


half3 SampleNormalCommonLit(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap))
{
    half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
    return UnpackNormal(n);
}

SurfaceData InitializeStandardLitSurfaceData(float2 uv)
{
    SurfaceData surfaceData = (SurfaceData)0;
    
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    surfaceData.alpha = albedoAlpha.a;
    #if _ALPHATEST_ON
    clip(albedoAlpha.a - _Cutoff);
    #endif
    
    surfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;

    half4 maskMRO = SAMPLE_TEXTURE2D(_MaskMROMap, sampler_MaskMROMap, uv);
    surfaceData.metallic = maskMRO.r;
    surfaceData.smoothness = (1 - maskMRO.g);
    surfaceData.occlusion = maskMRO.b;

    surfaceData.specular = 0;
    
    surfaceData.normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv));
    #if _EMISSION_ON
    surfaceData.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb;
    #endif
    return surfaceData;

}

#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
