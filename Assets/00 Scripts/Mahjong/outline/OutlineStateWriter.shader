Shader "Hidden/Mahjong/OutlineStateWriter"
{
    Properties
    {
        _OutlineStateColor ("Outline State Color", Color) = (1, 1, 1, 1)
        _OutlineStateEnabled ("Outline State Enabled", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "OutlineStateWriter"
            ZTest LEqual
            ZWrite On
            Cull Back
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _OutlineStateColor;
            float _OutlineStateEnabled;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                return _OutlineStateEnabled > 0.5 ? _OutlineStateColor : float4(0.0, 0.0, 0.0, 0.0);
            }
            ENDHLSL
        }
    }
}
