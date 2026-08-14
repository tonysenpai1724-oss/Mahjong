Shader "Mahjong/Combo Sweep"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _GlowColor ("Glow Color", Color) = (0.45, 0.95, 1.0, 1.0)
        _SweepColor ("Sweep Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _EdgeWidth ("Edge Width", Range(0.5, 4.0)) = 1.2
        _EdgeSoftness ("Edge Softness", Range(0.1, 2.0)) = 0.85
        _EdgeGlowIntensity ("Edge Glow Intensity", Range(0.0, 4.0)) = 0.08
        _SweepWidth ("Sweep Width", Range(0.005, 0.25)) = 0.045
        _SweepSoftness ("Sweep Softness", Range(0.001, 0.25)) = 0.02
        _SweepTailLength ("Sweep Tail Length", Range(0.01, 0.5)) = 0.18
        _SweepTailSoftness ("Sweep Tail Softness", Range(0.001, 0.25)) = 0.06
        _SweepSpeed ("Sweep Speed", Range(0.0, 4.0)) = 0.7
        _SweepGlowIntensity ("Sweep Glow Intensity", Range(0.0, 8.0)) = 3.4
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float4 _GlowColor;
            float4 _SweepColor;
            float _EdgeWidth;
            float _EdgeSoftness;
            float _EdgeGlowIntensity;
            float _SweepWidth;
            float _SweepSoftness;
            float _SweepTailLength;
            float _SweepTailSoftness;
            float _SweepSpeed;
            float _SweepGlowIntensity;
            CBUFFER_END

            float4 _BaseMap_TexelSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a;
            }

            float ResolveInnerEdge(float2 uv, float alpha)
            {
                float2 texelOffset = _BaseMap_TexelSize.xy * max(_EdgeWidth, 0.001);
                float alphaLeft = SampleAlpha(uv + float2(-texelOffset.x, 0.0));
                float alphaRight = SampleAlpha(uv + float2(texelOffset.x, 0.0));
                float alphaUp = SampleAlpha(uv + float2(0.0, texelOffset.y));
                float alphaDown = SampleAlpha(uv + float2(0.0, -texelOffset.y));

                float neighbourMinimum = min(min(alphaLeft, alphaRight), min(alphaUp, alphaDown));
                float edge = saturate(alpha - neighbourMinimum);
                edge *= smoothstep(0.001, 0.2 + _EdgeSoftness, alpha);
                return saturate(edge * (4.0 / max(_EdgeSoftness, 0.05)));
            }

            float ResolveSweepMask(float2 uv)
            {
                float2 centeredUv = uv - 0.5;
                float angle = atan2(centeredUv.y, centeredUv.x);
                float angle01 = frac((angle / 6.28318530718) + 0.5);
                float sweepHead = frac(_Time.y * _SweepSpeed);

                float travelFromHead = frac(sweepHead - angle01 + 1.0);
                float stripHead = 1.0 - smoothstep(_SweepWidth, _SweepWidth + _SweepSoftness, travelFromHead);
                float stripTail = 1.0 - smoothstep(_SweepTailLength, _SweepTailLength + _SweepTailSoftness, travelFromHead);
                return saturate(stripHead + (stripTail * 0.45));
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float alpha = baseSample.a;

                float edgeMask = ResolveInnerEdge(input.uv, alpha);
                float sweepMask = ResolveSweepMask(input.uv);

                float3 glow = _GlowColor.rgb * edgeMask * _EdgeGlowIntensity;
                glow += _SweepColor.rgb * edgeMask * sweepMask * _SweepGlowIntensity;

                float glowAlpha = edgeMask * saturate((_EdgeGlowIntensity * 0.2) + sweepMask);
                float3 finalColor = baseSample.rgb + glow;
                float finalAlpha = saturate(max(alpha, glowAlpha * alpha));
                return float4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
