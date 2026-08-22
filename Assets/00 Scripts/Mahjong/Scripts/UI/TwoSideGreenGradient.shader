Shader "UI/GreenSideSparkleAnimated"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Green Color", Color) = (0.05, 1, 0.2, 1)

        _EdgeWidth ("Edge Width", Range(0.01, 0.5)) = 0.25
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.5

        _PulseSpeed ("Gradient Pulse Speed", Range(0, 5)) = 1.5
        _PulseAmount ("Gradient Pulse Amount", Range(0, 1)) = 0.35

        _SparkleDensity ("Sparkle Density", Range(5, 80)) = 25
        _SparkleSize ("Sparkle Size", Range(0.002, 0.1)) = 0.035
        _SparkleIntensity ("Sparkle Intensity", Range(0, 20)) = 8
        _SparkleSpeed ("Sparkle Speed", Range(0, 10)) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;

            fixed4 _Color;

            float _EdgeWidth;
            float _GlowIntensity;

            float _PulseSpeed;
            float _PulseAmount;

            float _SparkleDensity;
            float _SparkleSize;
            float _SparkleIntensity;
            float _SparkleSpeed;


            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));

                p += dot(
                    p,
                    p + 45.32
                );

                return frac(p.x * p.y);
            }


            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;

                return o;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;


                // ============================================
                // GRADIENT 2 BÊN
                // ============================================

                float left =
                    1.0 -
                    smoothstep(
                        0.0,
                        _EdgeWidth,
                        uv.x
                    );

                float right =
                    smoothstep(
                        1.0 - _EdgeWidth,
                        1.0,
                        uv.x
                    );

                float edge =
                    max(left, right);


                // ============================================
                // GRADIENT PULSE
                // ============================================

                float pulse =
                    sin(
                        _Time.y *
                        _PulseSpeed
                    );

                pulse =
                    pulse * 0.5 + 0.5;

                pulse =
                    lerp(
                        1.0 - _PulseAmount,
                        1.0,
                        pulse
                    );

                float gradient =
                    edge *
                    _GlowIntensity *
                    pulse;


                // ============================================
                // SPARKLE GRID
                // ============================================

                float2 grid =
                    uv *
                    float2(
                        _SparkleDensity,
                        _SparkleDensity * 1.5
                    );

                float2 cell =
                    floor(grid);

                float2 local =
                    frac(grid);


                // ============================================
                // RANDOM POSITION
                // ============================================

                float randomX =
                    hash21(cell);

                float randomY =
                    hash21(
                        cell + 17.31
                    );

                float2 sparklePosition =
                    float2(
                        randomX,
                        randomY
                    );


                // ============================================
                // DISTANCE
                // ============================================

                float distanceToSparkle =
                    distance(
                        local,
                        sparklePosition
                    );


                // ============================================
                // PARTICLE
                // ============================================

                float particle =
                    1.0 -
                    smoothstep(
                        0.0,
                        _SparkleSize,
                        distanceToSparkle
                    );


                // ============================================
                // TWINKLE
                // ============================================

                float random =
                    hash21(
                        cell + 73.21
                    );

                float sparkleTime =
                    _Time.y *
                    _SparkleSpeed *
                    (
                        0.5 +
                        random * 2.5
                    );

                float twinkle =
                    sin(
                        sparkleTime +
                        random * 6.28318
                    );

                twinkle =
                    twinkle * 0.5 + 0.5;

                twinkle =
                    pow(
                        twinkle,
                        4.0
                    );


                // ============================================
                // CHỈ SPARKLE Ở MÉP
                // ============================================

                float sparkleMask =
                    smoothstep(
                        0.0,
                        _EdgeWidth,
                        edge
                    );

                float sparkle =
                    particle *
                    twinkle *
                    sparkleMask *
                    _SparkleIntensity;


                // ============================================
                // FINAL
                // ============================================

                float finalIntensity =
                    saturate(
                        gradient +
                        sparkle
                    );


                fixed4 result;

                result.rgb =
                    _Color.rgb *
                    finalIntensity;

                result.a =
                    finalIntensity *
                    i.color.a;

                return result;
            }

            ENDCG
        }
    }
}