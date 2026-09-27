Shader "Skybox/WT_Sky01"
{
    // SKY_01 -- the standard daytime sky: a blue gradient from a pale horizon up to a deep
    // zenith, with soft white clouds drifting across it. The clouds are a fractal noise laid
    // on a flat layer overhead (so they shrink towards the horizon like real ones), lit from
    // the top and greyer underneath, and fade into the haze at the horizon.
    Properties
    {
        _ZenithColor ("Zenith", Color) = (0.13, 0.34, 0.80, 1)
        _MidColor ("Mid Sky", Color) = (0.30, 0.55, 0.92, 1)
        _HorizonColor ("Horizon Haze", Color) = (0.74, 0.87, 0.98, 1)
        _GroundColor ("Below Horizon", Color) = (0.62, 0.76, 0.90, 1)
        _CloudColor ("Cloud Lit", Color) = (1, 1, 1, 1)
        _CloudShade ("Cloud Shade", Color) = (0.66, 0.72, 0.84, 1)
        _CloudCover ("Cloud Cover", Range(0, 1)) = 0.5
        _CloudScale ("Cloud Scale", Float) = 1.6
        _CloudSpeed ("Cloud Drift Speed", Float) = 0.012
        _SunDir ("Sun Direction", Vector) = (0.35, 0.55, 0.45, 0)
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

            fixed4 _ZenithColor, _MidColor, _HorizonColor, _GroundColor, _CloudColor, _CloudShade;
            float _CloudCover, _CloudScale, _CloudSpeed;
            float4 _SunDir;

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

            // Cloud density at a point on the cloud layer.
            float cloudDensity(float2 uv)
            {
                float n = fbm(uv);
                float threshold = lerp(0.78, 0.36, _CloudCover);
                return smoothstep(threshold, threshold + 0.22, n);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;

                // Sky gradient: haze at the horizon, up through mid blue to the zenith.
                float3 sky;
                if (h >= 0.0)
                {
                    float t = pow(saturate(h), 0.55);
                    sky = lerp(_HorizonColor.rgb, _MidColor.rgb, saturate(t * 2.0));
                    sky = lerp(sky, _ZenithColor.rgb, saturate(t * 2.0 - 1.0));
                }
                else
                {
                    sky = lerp(_HorizonColor.rgb, _GroundColor.rgb, saturate(-h * 4.0));
                }

                // A soft glow round the sun.
                float3 sun = normalize(_SunDir.xyz);
                float sunAmount = saturate(dot(d, sun));
                sky += float3(1.0, 0.95, 0.8) * (pow(sunAmount, 24.0) * 0.25 + pow(sunAmount, 600.0) * 0.8);

                // Clouds on a flat layer overhead; fade them out into the haze near the horizon.
                if (h > 0.0)
                {
                    float2 uv = d.xz / (h + 0.28) * _CloudScale + float2(_Time.y * _CloudSpeed, _Time.y * _CloudSpeed * 0.4);
                    float density = cloudDensity(uv);

                    // Shade: sample a little towards the sun; if the cloud thickens there, this
                    // spot is in shadow and reads greyer.
                    float2 towardSun = normalize(sun.xz + 1e-4) * 0.06;
                    float shade = saturate((cloudDensity(uv + towardSun) - density) * 3.0 + 0.25);

                    float3 cloud = lerp(_CloudColor.rgb, _CloudShade.rgb, shade);
                    float horizonFade = smoothstep(0.0, 0.22, h);
                    sky = lerp(sky, cloud, density * horizonFade);
                }

                return fixed4(sky, 1);
            }
            ENDCG
        }
    }
}
