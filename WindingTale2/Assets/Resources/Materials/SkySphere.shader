Shader "Skybox/RetroGradient"
{
    // Retro / pixel-art sky as a real skybox (assign via RenderSettings.skybox, or the
    // Lighting window). Renders behind every camera whose Clear Flags = Skybox, so it
    // is independent of camera position. Vertical gradient (below-horizon -> horizon ->
    // zenith), quantized into colour bands and ordered-dithered in screen space to mimic
    // the limited palettes / dithering of old 2D PC games (EGA/VGA).
    Properties
    {
        _TopColor ("Sky Top", Color) = (0.16, 0.30, 0.58, 1)
        _HorizonColor ("Horizon", Color) = (0.95, 0.80, 0.60, 1)
        _BottomColor ("Below Horizon", Color) = (0.30, 0.34, 0.30, 1)
        _HorizonHeight ("Horizon Height", Range(-1, 1)) = 0.0
        _HorizonSharpness ("Gradient Curve", Range(0.2, 4)) = 1.0
        _Bands ("Colour Bands", Range(2, 32)) = 8
        _DitherStrength ("Dither Strength", Range(0, 1)) = 1
        _PixelSize ("Dither Pixel Size", Range(1, 8)) = 2
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

            fixed4 _TopColor, _HorizonColor, _BottomColor;
            float _HorizonHeight, _HorizonSharpness, _Bands, _DitherStrength, _PixelSize;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
                float4 screen : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;             // skybox vertex position = view direction
                o.screen = ComputeScreenPos(o.pos);
                return o;
            }

            // 4x4 ordered (Bayer) dither threshold in 0..1
            float bayer4x4(float2 p)
            {
                float m[16] = {
                     0.0,  8.0,  2.0, 10.0,
                    12.0,  4.0, 14.0,  6.0,
                     3.0, 11.0,  1.0,  9.0,
                    15.0,  7.0, 13.0,  5.0
                };
                int x = (int)fmod(p.x, 4.0);
                int y = (int)fmod(p.y, 4.0);
                return m[x + y * 4] / 16.0;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Elevation: 0 straight down .. 0.5 horizon .. 1 straight up.
                float h = normalize(i.dir).y * 0.5 + 0.5;

                // Pixel-aligned ordered dither (blocky for the retro look).
                float2 px = floor(i.screen.xy / i.screen.w * _ScreenParams.xy / _PixelSize);
                float dither = (bayer4x4(px) - 0.5) * (_DitherStrength / _Bands);

                // Quantize elevation into discrete bands with dithered edges.
                float hq = floor((h + dither) * _Bands + 0.5) / _Bands;

                // Gradient around the horizon line.
                float hh = _HorizonHeight * 0.5 + 0.5;
                fixed4 col;
                if (hq < hh)
                {
                    float t = pow(saturate(hq / max(hh, 1e-4)), _HorizonSharpness);
                    col = lerp(_BottomColor, _HorizonColor, t);
                }
                else
                {
                    float t = pow(saturate((hq - hh) / max(1.0 - hh, 1e-4)), _HorizonSharpness);
                    col = lerp(_HorizonColor, _TopColor, t);
                }
                return col;
            }
            ENDCG
        }
    }
}
