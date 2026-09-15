Shader "Custom/FightFx"
{
    // The slash / trail glows lifted out of the 2D fight frames (Resources/Fights3D/NNN/
    // Fight_NNN_A_FF_fx.obj, a one-voxel-thin sheet). In the original drawing those pixels
    // were painted over the body, so the sheet is drawn the same way: unlit, straight from
    // the palette, and never hidden by the body model it sits inside (ZTest Always).
    // _Brightness above 1 pushes it past the lit body, which is the only glow the built-in
    // pipeline gives without bloom.
    Properties
    {
        _MainTex ("Palette (RGB)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0,3)) = 1.15
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        ZTest Always
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            half _Brightness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                c.rgb *= _Brightness;
                return c;
            }
            ENDCG
        }
    }
}
