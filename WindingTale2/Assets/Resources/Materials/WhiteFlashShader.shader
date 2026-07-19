Shader "Custom/WhiteFlash"
{
    // Renders the whole mesh as flat white, independent of scene lighting: used for
    // the one-frame "recovering" flash (see CreatureRecovering). Albedo is black and
    // the colour is emitted instead, so no light direction can shade it back down.
    Properties
    {
        _Color ("Flash Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = fixed3(0, 0, 0);
            o.Emission = _Color.rgb;
            o.Metallic = 0;
            o.Smoothness = 0;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
