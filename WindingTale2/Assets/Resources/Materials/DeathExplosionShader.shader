Shader "Custom/DeathExplosion"
{
    // The map death explosion (Resources/Animations/exploration/explosion_NN.obj): one
    // submesh per sprite colour, each drawn with its own _Color. Fire is self-lit, so the
    // colour is not lit by the scene; only a fixed top-down shade keeps the voxel form
    // readable (tops at full colour, sides and undersides darker). _Brightness above 1
    // is the only glow the built-in pipeline gives without bloom.
    Properties
    {
        _Color ("Colour", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0,3)) = 1.2
        _SideShade ("Side shade", Range(0,1)) = 0.7
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            half _Brightness;
            half _SideShade;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                half shade : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float up = UnityObjectToWorldNormal(v.normal).y;
                o.shade = lerp(_SideShade, 1.0, saturate(up * 0.5 + 0.5));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = _Color;
                c.rgb *= _Brightness * i.shade;
                return c;
            }
            ENDCG
        }
    }
}
