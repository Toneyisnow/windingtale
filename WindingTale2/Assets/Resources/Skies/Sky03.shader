Shader "Skybox/WT_Sky03"
{
    // SKY_03 -- an overcast day with a storm coming on: a flat grey sky under a heavy deck of
    // dark cloud. Built like SKY_01 (a fractal-noise layer overhead, and a low bank of 3D
    // noise round the horizon and below it, which is all the steeply tilted field camera
    // ever sees), but the cover is near total, the clouds are dark slate with only their
    // tops catching a dull light, and there is no sun -- just a faint pale patch where it
    // would be behind the cloud.
    Properties
    {
        _ZenithColor ("Zenith", Color) = (0.30, 0.32, 0.37, 1)
        _MidColor ("Mid Sky", Color) = (0.40, 0.42, 0.47, 1)
        _HorizonColor ("Horizon Haze", Color) = (0.52, 0.54, 0.58, 1)
        _GroundColor ("Below Horizon", Color) = (0.44, 0.46, 0.50, 1)
        _CloudColor ("Cloud Lit", Color) = (0.50, 0.51, 0.56, 1)
        _CloudShade ("Cloud Dark", Color) = (0.09, 0.10, 0.13, 1)
        _CloudCover ("Cloud Cover", Range(0, 1)) = 0.72
        _CloudScale ("Cloud Scale", Float) = 1.3
        _CloudSpeed ("Cloud Drift Speed", Float) = 0.02
        _SunDir ("Hidden Sun Direction", Vector) = (0.35, 0.55, 0.45, 0)
        _LowCloudCover ("Low Cloud Cover", Range(0, 1)) = 0.62
        _LowCloudScale ("Low Cloud Scale", Float) = 2.8
        _LowCloudOpacity ("Low Cloud Opacity", Range(0, 1)) = 1.0
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

            // Overhead deck. A softer ramp than SKY_01's puffs: storm cloud is ragged, not crisp.
            float cloudDensity(float2 uv)
            {
                float n = fbm(uv);
                float threshold = lerp(0.78, 0.30, _CloudCover);
                return smoothstep(threshold, threshold + 0.12, n);
            }

            // The low bank round the horizon, in flat layers (see SKY_01).
            float lowCloudDensity(float3 d)
            {
                float3 p = d * float3(_LowCloudScale, _LowCloudScale * 3.0, _LowCloudScale)
                         + float3(_Time.y * _CloudSpeed * 2.0, 0, _Time.y * _CloudSpeed * 0.8);
                float n = fbm3(p);
                float threshold = lerp(0.72, 0.30, _LowCloudCover);
                return smoothstep(threshold, threshold + 0.14, n);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;

                float3 sky;
                if (h >= 0.0)
                {
                    float t = pow(saturate(h), 0.6);
                    sky = lerp(_HorizonColor.rgb, _MidColor.rgb, saturate(t * 2.0));
                    sky = lerp(sky, _ZenithColor.rgb, saturate(t * 2.0 - 1.0));
                }
                else
                {
                    sky = lerp(_HorizonColor.rgb, _GroundColor.rgb, saturate(-h * 4.0));
                }

                // The sun is only a pale smear behind the cloud.
                float3 sun = normalize(_SunDir.xyz);
                float sunAmount = saturate(dot(d, sun));
                sky += float3(0.55, 0.55, 0.52) * pow(sunAmount, 6.0) * 0.12;

                if (h > 0.0)
                {
                    float2 uv = d.xz / (h + 0.28) * _CloudScale + float2(_Time.y * _CloudSpeed, _Time.y * _CloudSpeed * 0.4);
                    float density = cloudDensity(uv);

                    // Thick cloud is dark: the deeper into the deck (the denser a little
                    // further on), the nearer the dark colour.
                    float depth = saturate(cloudDensity(uv * 0.7 + 3.1) * 0.7 + (1.0 - density) * 0.2);
                    float3 cloud = lerp(_CloudColor.rgb, _CloudShade.rgb, depth);
                    float horizonFade = smoothstep(0.0, 0.18, h);
                    sky = lerp(sky, cloud, density * horizonFade);
                }

                float lowBand = 1.0 - smoothstep(0.10, 0.40, h);
                if (lowBand > 0.0)
                {
                    float low = lowCloudDensity(d);

                    // The bank's tops catch what light there is; everything under them is slate.
                    float above = lowCloudDensity(d + float3(0, 0.04, 0));
                    float lowShade = saturate((above - low) * 2.0 + 0.9);
                    float3 lowCloud = lerp(_CloudColor.rgb, _CloudShade.rgb, lowShade);
                    sky = lerp(sky, lowCloud, low * lowBand * _LowCloudOpacity);
                }

                return fixed4(sky, 1);
            }
            ENDCG
        }
    }
}
