Shader "Mahjong/Combo Tray Glow"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        _GlowColor ("Glow Color", Color) = (0.15, 0.85, 1, 1)
        _GradientColor ("Gradient Color", Color) = (0.72, 0.3, 1, 1)

        _GlowMode ("Glow Mode", Range(0, 1)) = 1
        _EffectIntensity ("Effect Intensity", Range(0, 2)) = 1

        // ============================================================
        // RAINBOW
        // ============================================================

        _RainbowMode ("Rainbow RGB Mode", Range(0, 1)) = 0

        _RainbowSpeed ("Rainbow Motion", Range(0, 3)) = 1.35
        _RainbowAngle ("Rainbow Angle", Range(0, 360)) = 18
        _RainbowCycles ("Rainbow Cycles", Range(0.25, 2)) = 0.75
        _RainbowStrength ("Rainbow Strength", Range(0, 1)) = 1

        _FillTintStrength ("Background Tint Strength", Range(0, 1)) = 0.72
        _BackgroundColorStrength ("Background Color Strength", Range(0, 1)) = 1

        _GradientStrength ("Gradient Strength", Range(0, 1)) = 0.68
        _GradientAngle ("Gradient Angle", Range(0, 360)) = 35
        _GradientSpeed ("Gradient Motion", Range(0, 2)) = 0.12

        _EdgeWidth ("Edge Width", Range(0.5, 6)) = 1.6
        _EdgeSoftness ("Edge Softness", Range(0.1, 2)) = 0.7
        _EdgeGlowIntensity ("Edge Glow Intensity", Range(0, 8)) = 2.6

        _PulseSpeed ("Pulse Speed", Range(0, 4)) = 1.15
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.2

        _BloomRadius ("Bloom Radius", Range(1, 8)) = 3
        _BloomIntensity ("Bloom Intensity", Range(0, 4)) = 0.42

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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UIUnlit"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)

            float4 _BaseMap_ST;

            float4 _BaseColor;
            float4 _GlowColor;
            float4 _GradientColor;

            float _GlowMode;
            float _EffectIntensity;

            float _RainbowMode;
            float _RainbowSpeed;
            float _RainbowAngle;
            float _RainbowCycles;
            float _RainbowStrength;

            float _FillTintStrength;
            float _BackgroundColorStrength;

            float _GradientStrength;
            float _GradientAngle;
            float _GradientSpeed;

            float _EdgeWidth;
            float _EdgeSoftness;
            float _EdgeGlowIntensity;

            float _PulseSpeed;
            float _PulseAmount;

            float _BloomRadius;
            float _BloomIntensity;

            CBUFFER_END

            float4 _BaseMap_TexelSize;
            float4 _ClipRect;

            // =========================================================
            // STRUCTS
            // =========================================================

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

            // =========================================================
            // VERTEX
            // =========================================================

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.color = input.color;

                output.uv =
                    TRANSFORM_TEX(input.uv, _BaseMap);

                output.localPosition =
                    input.positionOS.xy;

                return output;
            }

            // =========================================================
            // TEXTURE
            // =========================================================

            float SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(
                    _BaseMap,
                    sampler_BaseMap,
                    uv
                ).a;
            }

            // =========================================================
            // ANGLE
            // =========================================================

            float2 DirectionFromDegrees(float angle)
            {
                float radians =
                    angle * 0.01745329252;

                return float2(
                    cos(radians),
                    sin(radians)
                );
            }

            // =========================================================
            // INNER EDGE
            // =========================================================

            float ResolveInnerEdge(
                float2 uv,
                float alpha
            )
            {
                float2 offset =
                    _BaseMap_TexelSize.xy *
                    max(_EdgeWidth, 0.001);

                float left =
                    SampleAlpha(
                        uv - float2(offset.x, 0)
                    );

                float right =
                    SampleAlpha(
                        uv + float2(offset.x, 0)
                    );

                float up =
                    SampleAlpha(
                        uv + float2(0, offset.y)
                    );

                float down =
                    SampleAlpha(
                        uv - float2(0, offset.y)
                    );

                float neighbourMinimum =
                    min(
                        min(left, right),
                        min(up, down)
                    );

                float edge =
                    saturate(
                        alpha - neighbourMinimum
                    );

                edge *= smoothstep(
                    0.001,
                    0.2 + _EdgeSoftness,
                    alpha
                );

                return saturate(
                    edge *
                    (4.0 /
                    max(_EdgeSoftness, 0.05))
                );
            }

            // =========================================================
            // OUTER BLOOM
            // =========================================================

            float ResolveOuterBloom(
                float2 uv,
                float alpha
            )
            {
                float2 radius =
                    _BaseMap_TexelSize.xy *
                    max(_BloomRadius, 1.0);

                float diagonalScale =
                    0.70710678;

                float nearbyAlpha = 0;

                nearbyAlpha =
                    max(
                        nearbyAlpha,
                        SampleAlpha(
                            uv + float2(radius.x, 0)
                        )
                    );

                nearbyAlpha =
                    max(
                        nearbyAlpha,
                        SampleAlpha(
                            uv - float2(radius.x, 0)
                        )
                    );

                nearbyAlpha =
                    max(
                        nearbyAlpha,
                        SampleAlpha(
                            uv + float2(0, radius.y)
                        )
                    );

                nearbyAlpha =
                    max(
                        nearbyAlpha,
                        SampleAlpha(
                            uv - float2(0, radius.y)
                        )
                    );

                nearbyAlpha =
                    max(
                        nearbyAlpha,
                        SampleAlpha(
                            uv + radius *
                            diagonalScale
                        )
                    );

                nearbyAlpha =
                    max(
                        nearbyAlpha,
                        SampleAlpha(
                            uv - radius *
                            diagonalScale
                        )
                    );

                nearbyAlpha =
                    max(
                        nearbyAlpha,
                        SampleAlpha(
                            uv + float2(
                                radius.x,
                                -radius.y
                            ) *
                            diagonalScale
                        )
                    );

                nearbyAlpha =
                    max(
                        nearbyAlpha,
                        SampleAlpha(
                            uv + float2(
                                -radius.x,
                                radius.y
                            ) *
                            diagonalScale
                        )
                    );

                return saturate(
                    nearbyAlpha - alpha
                );
            }

            // =========================================================
            // NORMAL GRADIENT
            // =========================================================

            float ResolveGradient(float2 uv)
            {
                float2 direction =
                    DirectionFromDegrees(
                        _GradientAngle
                    );

                float coordinate =
                    dot(
                        uv - 0.5,
                        direction
                    ) + 0.5;

                float motion =
                    sin(
                        (
                            coordinate +
                            (_Time.y *
                            _GradientSpeed)
                        )
                        *
                        6.28318530718
                    );

                return saturate(
                    0.5 +
                    motion * 0.5
                );
            }

            // =========================================================
            // 7-COLOR RAINBOW
            //
            // 0.000 = RED
            // 0.166 = ORANGE
            // 0.333 = YELLOW
            // 0.500 = GREEN
            // 0.666 = CYAN
            // 0.833 = BLUE
            // 1.000 = MAGENTA/RED
            // =========================================================

            float3 Rainbow7(float hue)
            {
                hue = frac(hue);

                // 7 color stops
                static const float3 C0 =
                    float3(1.0, 0.05, 0.08); // RED

                static const float3 C1 =
                    float3(1.0, 0.35, 0.02); // ORANGE

                static const float3 C2 =
                    float3(1.0, 0.95, 0.05); // YELLOW

                static const float3 C3 =
                    float3(0.05, 1.0, 0.20); // GREEN

                static const float3 C4 =
                    float3(0.02, 1.0, 0.95); // CYAN

                static const float3 C5 =
                    float3(0.05, 0.25, 1.0); // BLUE

                static const float3 C6 =
                    float3(0.75, 0.08, 1.0); // VIOLET

                float segment =
                    hue * 7.0;

                float index =
                    floor(segment);

                float localT =
                    frac(segment);

                // Smooth interpolation
                localT =
                    localT *
                    localT *
                    (3.0 - 2.0 * localT);

                if (index < 1.0)
                {
                    return lerp(
                        C0,
                        C1,
                        localT
                    );
                }

                if (index < 2.0)
                {
                    return lerp(
                        C1,
                        C2,
                        localT
                    );
                }

                if (index < 3.0)
                {
                    return lerp(
                        C2,
                        C3,
                        localT
                    );
                }

                if (index < 4.0)
                {
                    return lerp(
                        C3,
                        C4,
                        localT
                    );
                }

                if (index < 5.0)
                {
                    return lerp(
                        C4,
                        C5,
                        localT
                    );
                }

                if (index < 6.0)
                {
                    return lerp(
                        C5,
                        C6,
                        localT
                    );
                }

                return lerp(
                    C6,
                    C0,
                    localT
                );
            }

            // =========================================================
            // RAINBOW FLOW
            // =========================================================

            float3 ResolveRainbowSegment(float2 uv)
            {
                float2 direction =
                    DirectionFromDegrees(
                        _RainbowAngle
                    );

                float coordinate =
                    dot(
                        uv - 0.5,
                        direction
                    ) + 0.5;

                // Primary rainbow
                float flow1 =
                    coordinate *
                    _RainbowCycles;

                flow1 +=
                    _Time.y *
                    _RainbowSpeed *
                    0.08;

                // Secondary rainbow moving opposite direction
                float flow2 =
                    coordinate *
                    (_RainbowCycles * 0.72);

                flow2 -=
                    _Time.y *
                    _RainbowSpeed *
                    0.045;

                float3 rainbow1 =
                    Rainbow7(flow1);

                float3 rainbow2 =
                    Rainbow7(flow2 + 0.43);

                // Blend two rainbow layers
                float3 rainbow =
                    lerp(
                        rainbow1,
                        rainbow2,
                        0.28
                    );

                // =====================================================
                // WHITE SPECTRAL HIGHLIGHT
                // =====================================================

                float highlightWave =
                    0.5 +
                    0.5 *
                    sin(
                        (
                            coordinate *
                            6.2831853 *
                            1.35
                        )
                        -
                        _Time.y *
                        _RainbowSpeed *
                        1.2
                    );

                float highlight =
                    smoothstep(
                        0.72,
                        1.0,
                        highlightWave
                    );

                rainbow =
                    lerp(
                        rainbow,
                        float3(
                            1.0,
                            1.0,
                            1.0
                        ),
                        highlight * 0.20
                    );

                return rainbow;
            }

            // =========================================================
            // EFFECT COLOR
            // =========================================================

            float3 ResolveEffectColor(
                float3 tierColor,
                float2 uv
            )
            {
                float3 rainbowColor =
                    ResolveRainbowSegment(uv);

                float rainbowBlend =
                    saturate(
                        _RainbowMode *
                        _RainbowStrength
                    );

                return lerp(
                    tierColor,
                    rainbowColor,
                    rainbowBlend
                );
            }

            // =========================================================
            // UI CLIP
            // =========================================================

            float ResolveClipAlpha(float2 position)
            {
                #ifdef UNITY_UI_CLIP_RECT

                float2 inside =
                    step(
                        _ClipRect.xy,
                        position
                    )
                    *
                    step(
                        position,
                        _ClipRect.zw
                    );

                return inside.x *
                       inside.y;

                #else

                return 1.0;

                #endif
            }

            // =========================================================
            // FRAGMENT
            // =========================================================

            half4 Frag(Varyings input) : SV_Target
            {
                half4 baseSample =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    )
                    *
                    _BaseColor
                    *
                    input.color;

                float alpha =
                    baseSample.a;

                float effect =
                    saturate(
                        _EffectIntensity
                    );

                // =====================================================
                // GRADIENT
                // =====================================================

                float gradient =
                    ResolveGradient(
                        input.uv
                    );

                float pulseWave =
                    0.5 +
                    0.5 *
                    sin(
                        _Time.y *
                        _PulseSpeed *
                        6.28318530718
                    );

                float pulse =
                    1.0 +
                    (
                        pulseWave *
                        _PulseAmount *
                        effect
                    );

                // =====================================================
                // TIER GRADIENT
                // =====================================================

                half3 tierGradientColor =
                    lerp(
                        _GlowColor.rgb,
                        _GradientColor.rgb,
                        gradient *
                        _GradientStrength
                    );

                // =====================================================
                // FINAL EFFECT COLOR
                // =====================================================

                half3 gradientColor =
                    ResolveEffectColor(
                        tierGradientColor,
                        input.uv
                    );

                half3 finalColor;

                float finalAlpha =
                    alpha;

                // =====================================================
                // TRAY BACKGROUND
                // =====================================================

                if (_GlowMode < 0.5)
                {
                    float tint =
                        saturate(
                            _FillTintStrength *
                            effect
                        );

                    float colorStrength =
                        saturate(
                            _BackgroundColorStrength *
                            effect
                        );

                    half3 colorizedBase =
                        lerp(
                            baseSample.rgb,
                            gradientColor * 1.12,
                            colorStrength
                        );

                    half3 detailedTint =
                        lerp(
                            baseSample.rgb,
                            colorizedBase,
                            tint
                        );

                    // Soft rainbow ambience
                    detailedTint +=
                        gradientColor *
                        tint *
                        0.22;

                    half3 ambientBloom =
                        gradientColor *
                        pulseWave *
                        _BloomIntensity *
                        0.20 *
                        effect *
                        alpha;

                    finalColor =
                        (detailedTint * pulse)
                        +
                        ambientBloom;
                }

                // =====================================================
                // SLOT
                // =====================================================

                else
                {
                    float edge =
                        ResolveInnerEdge(
                            input.uv,
                            alpha
                        );

                    float outerBloom =
                        ResolveOuterBloom(
                            input.uv,
                            alpha
                        );

                    // Main rainbow edge
                    half3 edgeGlow =
                        gradientColor *
                        edge *
                        _EdgeGlowIntensity *
                        pulse *
                        effect;

                    // Soft outer rainbow
                    half3 bloomLight =
                        gradientColor *
                        outerBloom *
                        _BloomIntensity *
                        pulse *
                        effect;

                    // =================================================
                    // EXTRA RAINBOW CORE
                    // =================================================

                    float centerDistance =
                        distance(
                            input.uv,
                            float2(0.5, 0.5)
                        );

                    float centerGlow =
                        1.0 -
                        smoothstep(
                            0.15,
                            0.75,
                            centerDistance
                        );

                    half3 rainbowCore =
                        gradientColor *
                        centerGlow *
                        0.12 *
                        effect;

                    // =================================================
                    // FINAL
                    // =================================================

                    finalColor =
                        baseSample.rgb
                        +
                        edgeGlow
                        +
                        bloomLight
                        +
                        rainbowCore;

                    finalAlpha =
                        saturate(
                            max(
                                alpha,
                                outerBloom *
                                _BloomIntensity *
                                effect
                            )
                        );
                }

                finalAlpha *=
                    ResolveClipAlpha(
                        input.localPosition
                    );

                return half4(
                    finalColor,
                    finalAlpha
                );
            }

            ENDHLSL
        }
    }
}