Shader "Mahjong/UI/Tray Slot Neon Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Neon Color", Color) = (0.05, 1, 0.2, 1)
        _OutlineThickness ("Outline Thickness", Range(0.5, 8)) = 2.5
        _GlowIntensity ("Glow Intensity", Range(0, 8)) = 3
        _PulseSpeed ("Pulse Speed", Range(0, 8)) = 2.5
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.35
        _SparkleSpeed ("Sparkle Speed", Range(0, 10)) = 2
        _SparkleStrength ("Sparkle Strength", Range(0, 2)) = 0.8
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
            "IgnoreProjector" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        ColorMask [_ColorMask]

        Pass
        {
            Name "NeonOutline"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _OutlineThickness;
                float _GlowIntensity;
                float _PulseSpeed;
                float _PulseAmount;
                float _SparkleSpeed;
                float _SparkleStrength;
            CBUFFER_END

            float4 _ClipRect;
            float4 _MainTex_TexelSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 localPosition : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.localPosition = input.positionOS.xy;
                return output;
            }

            float SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            float ResolveOuterEdge(float2 uv, float centerAlpha)
            {
                float2 texel = _MainTex_TexelSize.xy * max(_OutlineThickness, 0.5);
                float nearbyAlpha = 0;
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(uv + float2(texel.x, 0)));
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(uv - float2(texel.x, 0)));
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(uv + float2(0, texel.y)));
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(uv - float2(0, texel.y)));
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(uv + texel * 0.7071));
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(uv - texel * 0.7071));
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(uv + float2(texel.x, -texel.y) * 0.7071));
                nearbyAlpha = max(nearbyAlpha, SampleAlpha(uv + float2(-texel.x, texel.y) * 0.7071));

                return saturate(nearbyAlpha - centerAlpha);
            }

            float ResolveClipAlpha(float2 position)
            {
                #ifdef UNITY_UI_CLIP_RECT
                    float2 inside = step(_ClipRect.xy, position) * step(position, _ClipRect.zw);
                    return inside.x * inside.y;
                #else
                    return 1;
                #endif
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float centerAlpha = SampleAlpha(input.uv);
                float edge = ResolveOuterEdge(input.uv, centerAlpha);
                float pulse = 1 + ((0.5 + (0.5 * sin(_Time.y * _PulseSpeed * 6.2831853))) * _PulseAmount);
                float sparkleWave = 0.5 + (0.5 * sin((input.uv.x * 13 + input.uv.y * 29) + (_Time.y * _SparkleSpeed * 6.2831853)));
                float sparkle = pow(saturate(sparkleWave), 12) * _SparkleStrength;
                float intensity = (edge * pulse * _GlowIntensity) + (edge * sparkle);
                float alpha = saturate(intensity * input.color.a) * ResolveClipAlpha(input.localPosition);
                return half4(_Color.rgb * intensity, alpha);
            }
            ENDHLSL
        }
    }
}
