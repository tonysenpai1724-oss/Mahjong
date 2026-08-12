Shader "Hidden/Mahjong/PerObjectFullscreenOutline"
{
    Properties
    {
        _OutlineColor ("Default Outline Color", Color) = (1, 1, 1, 1)
        _OutlineThickness ("Outline Thickness", Float) = 1.05
        _Intensity ("Intensity", Float) = 1
        [HideInInspector] _BlitTexture ("Blit Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "PerObjectFullscreenOutline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Assets/00 Scripts/Mahjong/outline/Outlines.hlsl"

            TEXTURE2D(_OutlineStateTex);
            SAMPLER(sampler_OutlineStateTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _OutlineColor;
            float _OutlineThickness;
            float _Intensity;
            CBUFFER_END

            float4 SampleOutlineState(float2 uv)
            {
                return SAMPLE_TEXTURE2D_LOD(_OutlineStateTex, sampler_OutlineStateTex, uv, 0.0);
            }

            float GetLinearDepthAt(float2 uv)
            {
                return SampleLinearEyeDepth(uv);
            }

            float4 ResolveOutlineState(float2 uv, float2 pixelSize)
            {
                float4 bestState = float4(0.0, 0.0, 0.0, 0.0);
                float bestDepth = 1e20;

                float2 offsets[5] =
                {
                    float2(0.0, 0.0),
                    float2(-pixelSize.x, 0.0),
                    float2(pixelSize.x, 0.0),
                    float2(0.0, pixelSize.y),
                    float2(0.0, -pixelSize.y)
                };

                [unroll]
                for (int index = 0; index < 5; index++)
                {
                    float2 sampleUv = saturate(uv + offsets[index]);
                    float4 sampleState = SampleOutlineState(sampleUv);
                    if (sampleState.a <= 0.001)
                    {
                        continue;
                    }

                    float sampleDepth = GetLinearDepthAt(sampleUv);
                    if (sampleDepth < bestDepth)
                    {
                        bestDepth = sampleDepth;
                        bestState = sampleState;
                    }
                }

                if (bestState.a <= 0.001)
                {
                    bestState = _OutlineColor;
                }

                bestState.a = 1.0;
                return bestState;
            }

            float4 Frag(Varyings input) : SV_Target0
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                float2 pixelSize = (1.0 / _ScaledScreenParams.xy) * max(_OutlineThickness, 0.001);

                float depthOutline;
                DepthBasedOutlines_float(uv, pixelSize, depthOutline);

                float normalOutline;
                NormalBasedOutlines_float(uv, pixelSize, normalOutline);

                float edgeMask = saturate(max(depthOutline, normalOutline) * _Intensity);
                float4 sceneColor = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel);
                if (edgeMask <= 0.0001)
                {
                    return sceneColor;
                }

                float4 outlineState = ResolveOutlineState(uv, pixelSize);
                return lerp(sceneColor, float4(outlineState.rgb, 1.0), edgeMask);
            }
            ENDHLSL
        }
    }
}
