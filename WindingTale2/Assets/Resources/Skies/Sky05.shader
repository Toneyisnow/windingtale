Shader "Skybox/WT_Sky05"
{
    // SKY_05 -- a pitch-black night full of twinkling stars, for the chapters that float in
    // the void (23, 24, 27..30): the board's black void is cut out of the tiles there, and
    // this is what shows through. The field camera is tilted steeply down, so it mostly
    // sees the sky round the horizon and below it; the stars cover the whole sphere, not
    // just the dome overhead.
    //
    // Three layers of stars, each a 3D grid of cells over the view direction with at most
    // one star per cell: many faint small ones, fewer brighter ones, and a scattering of
    // big bright ones with a soft halo. Every star twinkles at its own rate and phase.
    // Behind them a very faint blue-violet haze keeps the black from looking like a hole.
    Properties
    {
        _SkyColor ("Night", Color) = (0.004, 0.005, 0.012, 1)
        _HazeColor ("Haze", Color) = (0.035, 0.030, 0.075, 1)
        _HazeStrength ("Haze Strength", Range(0, 1)) = 0.6
        _StarDensity ("Star Density", Float) = 1.0
        _StarBrightness ("Star Brightness", Float) = 1.2
        _TwinkleSpeed ("Twinkle Speed", Float) = 1.6
        _TwinkleDepth ("Twinkle Depth", Range(0, 1)) = 0.75
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

            fixed4 _SkyColor, _HazeColor;
            float _HazeStrength, _StarDensity, _StarBrightness, _TwinkleSpeed, _TwinkleDepth;

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
                o.dir = v.vertex.xyz;   // skybox vertex position = view direction
                return o;
            }

            float hash31(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float3 hash33(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            float vnoise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = hash31(i);
                float n100 = hash31(i + float3(1, 0, 0));
                float n010 = hash31(i + float3(0, 1, 0));
                float n110 = hash31(i + float3(1, 1, 0));
                float n001 = hash31(i + float3(0, 0, 1));
                float n101 = hash31(i + float3(1, 0, 1));
                float n011 = hash31(i + float3(0, 1, 1));
                float n111 = hash31(i + float3(1, 1, 1));
                return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                            lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
            }

            float fbm3(float3 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 4; k++)
                {
                    v += a * vnoise3(p);
                    p = p * 2.02 + float3(17.1, 9.7, 3.3);
                    a *= 0.5;
                }
                return v;
            }

            // One layer of stars. `scale` is cells across the unit sphere (bigger = smaller,
            // denser stars), `chance` the share of cells that hold one, `size` the star's
            // radius in cells and `glow` how far its soft halo reaches.
            float3 starLayer(float3 d, float scale, float chance, float size, float glow, float seed)
            {
                float3 p = d * scale + seed;
                float3 cell = floor(p);
                float3 h = hash33(cell);
                if (h.x > chance * _StarDensity)
                {
                    return 0;
                }

                // Keep the star inside its cell so it is never cut by the cell edge.
                float3 centre = cell + 0.25 + 0.5 * hash33(cell + 11.7);
                float dist = length(p - centre);

                float core = 1.0 - smoothstep(size * 0.35, size, dist);
                float halo = glow > 0.0 ? exp(-dist * dist / (glow * glow)) * 0.25 : 0.0;

                // Twinkle: each star its own speed and phase, never quite going out.
                float rate = _TwinkleSpeed * (0.6 + 1.4 * h.y);
                float phase = h.z * 6.2831;
                float tw = 0.5 + 0.5 * sin(_Time.y * rate + phase);
                tw *= 0.5 + 0.5 * sin(_Time.y * rate * 2.3 + phase * 1.7);
                float twinkle = lerp(1.0, tw, _TwinkleDepth);

                // Mostly white, some faintly blue, some faintly warm.
                float3 tint = h.y < 0.33 ? float3(0.75, 0.85, 1.0)
                            : (h.y > 0.85 ? float3(1.0, 0.88, 0.72) : float3(1, 1, 1));
                float brightness = 0.55 + 0.45 * hash31(cell + 5.3);
                return tint * (core + halo) * brightness * twinkle;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);

                // Faint haze, drifting very slowly, so the night has some depth.
                float haze = fbm3(d * 2.2 + float3(_Time.y * 0.004, 0, 0));
                haze = smoothstep(0.45, 0.85, haze) * _HazeStrength;
                float3 sky = lerp(_SkyColor.rgb, _HazeColor.rgb, haze);

                float3 stars = 0;
                stars += starLayer(d, 160.0, 0.30, 0.18, 0.0, 0.0) * 0.45;
                stars += starLayer(d, 90.0, 0.16, 0.16, 0.0, 37.0) * 0.8;
                stars += starLayer(d, 45.0, 0.06, 0.12, 0.22, 71.0) * 1.2;

                sky += stars * _StarBrightness;
                return fixed4(sky, 1);
            }
            ENDCG
        }
    }
}
