Shader "Custom/LightShaft"
{
    // Chapter 24's light shafts: columns of light hanging from under the floating island
    // into the void (LightShaft.cs builds the mesh). Self-lit, coloured across the width
    // exactly like the 2D art -- a six-step orange ramp on each side of a pale core, one
    // step per original pixel -- and flickering between bright and weak _FlickerHz times a
    // second. The last stretch, past the map's bottom edge, fades out so the column has no
    // hard end. uv.x runs across the column's silhouette (0 and 1 are its two edges), uv.y
    // down its length.
    Properties
    {
        _EdgeFraction ("Ramp Width (fraction of shaft width, per side)", Range(0, 0.5)) = 0.158
        _FlickerHz ("Flickers Per Second", Float) = 5
        _WeakLevel ("Weak Brightness", Range(0, 1)) = 0.72
        _FadeStart ("Fade Start (uv.y)", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _EdgeFraction, _FlickerHz, _WeakLevel, _FadeStart;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // The shaft's own colours, outermost pixel first (Chapter 24's tiles 36 / 40 / 44).
            static const float3 Ramp[6] =
            {
                float3(168, 84, 0) / 255.0,
                float3(192, 108, 0) / 255.0,
                float3(216, 132, 0) / 255.0,
                float3(240, 156, 12) / 255.0,
                float3(252, 188, 48) / 255.0,
                float3(252, 220, 104) / 255.0,
            };
            static const float3 Core = float3(252, 252, 212) / 255.0;

            fixed4 frag (v2f i) : SV_Target
            {
                float fromEdge = min(i.uv.x, 1.0 - i.uv.x);
                int step = (int)floor(fromEdge / max(_EdgeFraction, 1e-4) * 6.0);
                float3 colour = step >= 6 ? Core : Ramp[clamp(step, 0, 5)];

                float bright = frac(_Time.y * _FlickerHz) < 0.5 ? 1.0 : _WeakLevel;
                float alpha = 1.0 - smoothstep(_FadeStart, 1.0, i.uv.y);
                return fixed4(colour * bright, alpha);
            }
            ENDCG
        }
    }
}
