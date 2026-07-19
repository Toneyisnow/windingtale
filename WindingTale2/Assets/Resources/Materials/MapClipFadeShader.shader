Shader "Custom/MapClipFade"
{
    // Transparent twin of "Custom/MapClip": same map-rectangle truncation, but the
    // surface is alpha blended so _Color.a fades the geometry out. Obstacles swap
    // their materials to this shader while the cursor (or a menu item) sits on one
    // of their tiles, and swap back to "Custom/MapClip" afterwards. The property
    // names match so the swap keeps the model's own texture/colour.
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        CGPROGRAM
        // alpha:fade = regular alpha blending, ZWrite off. No addshadow: a faded
        // obstacle should not keep casting its full opaque shadow.
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // xy = (minX, minZ), zw = (maxX, maxZ) of the map rectangle in world space.
        float4 _MapClipMinMaxXZ;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Truncate: discard fragments outside [min, max] on world X and Z.
            float2 mn = _MapClipMinMaxXZ.xy;
            float2 mx = _MapClipMinMaxXZ.zw;
            float inside = step(mn.x, IN.worldPos.x) * step(IN.worldPos.x, mx.x)
                         * step(mn.y, IN.worldPos.z) * step(IN.worldPos.z, mx.y);
            clip(inside - 0.5);

            fixed4 col = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = col.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = col.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
