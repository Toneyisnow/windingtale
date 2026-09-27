Shader "Skybox/WT_Sky02"
{
    // SKY_02 -- the inside of a volcanic cave. Overhead a ceiling of dark rock, cracked by
    // thin veins of glowing magma and lit orange from beneath; round the horizon a bright
    // glow; and below it a sea of lava -- a slowly flowing river of orange between drifting
    // plates of black crust. Everything flickers a little, like firelight.
    Properties
    {
        _RockColor ("Rock", Color) = (0.09, 0.05, 0.045, 1)
        _RockLitColor ("Rock Lit By Lava", Color) = (0.55, 0.16, 0.05, 1)
        _LavaDeepColor ("Lava Deep", Color) = (0.75, 0.10, 0.02, 1)
        _LavaBrightColor ("Lava Bright", Color) = (1.0, 0.62, 0.12, 1)
        _LavaHotColor ("Lava Hot", Color) = (1.0, 0.92, 0.55, 1)
        _CrustColor ("Crust", Color) = (0.05, 0.028, 0.025, 1)
        _LavaCoverage ("Lava Coverage", Range(0, 1)) = 0.55
        _FlowSpeed ("Flow Speed", Float) = 0.05
        _VeinAmount ("Ceiling Veins", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _RockColor, _RockLitColor, _LavaDeepColor, _LavaBrightColor, _LavaHotColor, _CrustColor;
            float _LavaCoverage, _FlowSpeed, _VeinAmount;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 5; k++)
                {
                    v += a * vnoise(p);
                    p = p * 2.03 + float2(17.1, 9.7);
                    a *= 0.5;
                }
                return v;
            }

            // Thin bright ridges where the noise crosses 0.5 -- cracks, veins, the edges of plates.
            float ridge(float2 p)
            {
                return 1.0 - abs(fbm(p) * 2.0 - 1.0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                float t = _Time.y;

                // A slow firelight flicker on everything.
                float flicker = 0.92 + 0.08 * sin(t * 3.1) * sin(t * 1.7 + 1.3);

                // The glow that hangs round the horizon, brightest right on it.
                float glow = exp(-abs(h) * 5.5);
                float3 glowColor = lerp(_LavaDeepColor.rgb, _LavaBrightColor.rgb, glow);

                float3 col;

                if (h >= 0.0)
                {
                    // --- Ceiling and walls: dark rock lit from below. ---
                    float2 uv = d.xz / (h + 0.35) * 2.2;
                    float rock = fbm(uv * 1.3);
                    float3 rockCol = lerp(_RockColor.rgb * 0.5, _RockColor.rgb * 1.6, rock);

                    // Rock facing the lava is lit orange, fading with height.
                    float lit = saturate(1.0 - h * 1.6);
                    lit = lit * lit * (0.5 + 0.7 * rock);
                    rockCol = lerp(rockCol, _RockLitColor.rgb, lit);

                    // Veins of magma running through the rock.
                    float vein = ridge(uv * 0.9 + float2(t * _FlowSpeed * 0.15, 0));
                    vein = smoothstep(0.90, 0.985, vein) * _VeinAmount;
                    float pulse = 0.65 + 0.35 * sin(t * 1.4 + rock * 12.0);
                    rockCol += _LavaBrightColor.rgb * vein * pulse * saturate(1.3 - h);

                    col = rockCol;
                    col = lerp(col, glowColor, glow * 0.75);
                }
                else
                {
                    // --- The lava sea below the horizon. ---
                    float2 uv = d.xz / (-h + 0.12) * 1.1;
                    float2 flow = float2(t * _FlowSpeed, t * _FlowSpeed * 0.6);

                    // Two layers of plates drifting at different speeds; cracks between them
                    // show the magma, and the plates' own middles are black crust.
                    float plates = fbm(uv * 1.4 + flow);
                    float plates2 = fbm(uv * 2.6 - flow * 1.7 + 4.0);
                    float crustMask = smoothstep(lerp(0.75, 0.40, _LavaCoverage), lerp(0.85, 0.50, _LavaCoverage), plates * 0.6 + plates2 * 0.4);

                    float crack = ridge(uv * 1.9 + flow * 2.0);
                    float magma = saturate(smoothstep(0.80, 0.97, crack) + (1.0 - crustMask));

                    float heat = saturate(magma * (0.6 + 0.6 * fbm(uv * 3.5 - flow * 3.0)));
                    float3 lava = lerp(_LavaDeepColor.rgb, _LavaBrightColor.rgb, saturate(heat * 1.4));
                    lava = lerp(lava, _LavaHotColor.rgb, saturate(heat * heat * 1.3 - 0.35));

                    col = lerp(_CrustColor.rgb, lava, saturate(magma));

                    // Far away, the sea melts into the horizon glow.
                    float far = exp(-(-h) * 9.0);
                    col = lerp(col, glowColor, far * 0.7);
                }

                col *= flicker;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
