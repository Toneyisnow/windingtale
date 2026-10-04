Shader "Skybox/WT_Sky04"
{
    // SKY_04 -- a burning sunset (火烧云). The same sky as SKY_01 -- the same overhead cloud
    // layer, the same low bank past the map's edges, the same cloud cover -- recoloured for
    // dusk: a dim violet zenith down through orange to a glowing golden horizon, the sun
    // sitting low, and the clouds burning red against it, smouldering crimson in their shade.
    // Clouds near the sun glow brighter, as evening clouds do.
    Properties
    {
        _ZenithColor ("Zenith", Color) = (0.28, 0.12, 0.30, 1)
        _MidColor ("Mid Sky", Color) = (0.95, 0.50, 0.24, 1)
        _HorizonColor ("Horizon Haze", Color) = (1.00, 0.76, 0.40, 1)
        _GroundColor ("Below Horizon", Color) = (0.92, 0.55, 0.28, 1)
        _CloudColor ("Cloud Lit", Color) = (1.00, 0.40, 0.26, 1)
        _CloudShade ("Cloud Shade", Color) = (0.46, 0.08, 0.14, 1)
        _CloudCover ("Cloud Cover", Range(0, 1)) = 0.55
        _CloudScale ("Cloud Scale", Float) = 1.6
        _CloudSpeed ("Cloud Drift Speed", Float) = 0.012
        _SunDir ("Sun Direction", Vector) = (0.55, 0.10, 0.45, 0)
        _SunGlow ("Sun Glow", Color) = (1.00, 0.75, 0.40, 1)
        _CloudSunBoost ("Cloud Glow Near Sun", Range(0, 2)) = 0.6
        _LowCloudCover ("Low Cloud Cover", Range(0, 1)) = 0.6
        _LowCloudScale ("Low Cloud Scale", Float) = 3.2
        _LowCloudOpacity ("Low Cloud Opacity", Range(0, 1)) = 0.95
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

            fixed4 _ZenithColor, _MidColor, _HorizonColor, _GroundColor, _CloudColor, _CloudShade, _SunGlow;
            float _CloudSunBoost;
            float _CloudCover, _CloudScale, _CloudSpeed;
            float4 _SunDir;
            float _LowCloudCover, _LowCloudScale, _LowCloudOpacity;

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

            float hash31(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
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
                for (int k = 0; k < 5; k++)
                {
                    v += a * vnoise3(p);
                    p = p * 2.02 + float3(17.1, 9.7, 3.3);
                    a *= 0.5;
                }
                return v;
            }

            // Low cloud density for a view direction. The noise is stretched three times as
            // fine vertically as across, so the puffs lie in flat banks along the horizon.
            float lowCloudDensity(float3 d)
            {
                float3 p = d * float3(_LowCloudScale, _LowCloudScale * 3.0, _LowCloudScale)
                         + float3(_Time.y * _CloudSpeed * 2.0, 0, _Time.y * _CloudSpeed * 0.8);
                float n = fbm3(p);
                float threshold = lerp(0.72, 0.32, _LowCloudCover);
                return smoothstep(threshold, threshold + 0.08, n);
            }

            // Cloud density at a point on the cloud layer.
            float cloudDensity(float2 uv)
            {
                float n = fbm(uv);
                float threshold = lerp(0.78, 0.36, _CloudCover);
                // A narrow ramp gives the puffs a defined edge.
                return smoothstep(threshold, threshold + 0.07, n);
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
                sky += _SunGlow.rgb * (pow(sunAmount, 8.0) * 0.35 + pow(sunAmount, 600.0) * 0.9);

                // Evening clouds facing the sun burn brighter than those across the sky.
                float cloudGlow = 1.0 + _CloudSunBoost * pow(sunAmount, 4.0);

                // Clouds on a flat layer overhead; fade them out into the haze near the horizon.
                if (h > 0.0)
                {
                    float2 uv = d.xz / (h + 0.28) * _CloudScale + float2(_Time.y * _CloudSpeed, _Time.y * _CloudSpeed * 0.4);
                    float density = cloudDensity(uv);

                    // Shade: sample a little towards the sun; if the cloud thickens there, this
                    // spot is in shadow and reads greyer.
                    float2 towardSun = normalize(sun.xz + 1e-4) * 0.06;
                    float shade = saturate((cloudDensity(uv + towardSun) - density) * 4.0 + 0.3);

                    float3 cloud = lerp(_CloudColor.rgb * cloudGlow, _CloudShade.rgb, shade);
                    float horizonFade = smoothstep(0.0, 0.22, h);
                    sky = lerp(sky, cloud, density * horizonFade);
                }

                // The low bank: fades in from h = -0.42 to -0.28 -- below the horizon, where the
                // camera looks past the map's far edge -- and is gone by h = 0.4, so it takes
                // over where the overhead layer fades out. Further down the box stays clear sky.
                float lowBand = smoothstep(-0.42, -0.28, h) * (1.0 - smoothstep(0.12, 0.4, h));
                if (lowBand > 0.0)
                {
                    float low = lowCloudDensity(d);

                    // Lit on top: if the cloud is thicker a little higher up, this spot is
                    // on the underside and reads greyer.
                    float lowShade = saturate((lowCloudDensity(d + float3(0, 0.035, 0)) - low) * 4.0 + 0.2);
                    float3 lowCloud = lerp(_CloudColor.rgb * cloudGlow, _CloudShade.rgb, lowShade);
                    sky = lerp(sky, lowCloud, low * lowBand * _LowCloudOpacity);
                }

                return fixed4(sky, 1);
            }
            ENDCG
        }
    }
}
