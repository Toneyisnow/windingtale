Shader "Custom/MoveIndicatorGlass"
{
    // Translucent, glossy "glass" look for the move/target indicators.
    //  - Standard PBR lighting with high smoothness + some metallic, so it catches
    //    specular glints from the scene light and reflects the skybox/reflection probes.
    //  - A Fresnel term brightens and solidifies the edges (glancing angles), which is
    //    what reads as a glass rim. The body stays faint; the rims glow.
    // _Color.a is the overall opacity, still pulsed by BlockBlinkEffect for the blink.
    Properties
    {
        _Color ("Tint", Color) = (0.6, 0.85, 1.0, 0.5)
        _Glossiness ("Smoothness", Range(0,1)) = 0.95
        _Metallic ("Metallic", Range(0,1)) = 0.4
        _FresnelColor ("Fresnel Color", Color) = (1, 1, 1, 1)
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3.0
        _FresnelStrength ("Fresnel Strength", Range(0, 4)) = 1.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        // Pull the indicator very slightly toward the camera in depth so it wins the
        // ZTest against the ground grass it interpenetrates, instead of z-fighting
        // (the per-pixel flicker). Small enough not to defeat real occlusion.
        Offset -1, -1

        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _FresnelColor;
        half _FresnelPower;
        half _FresnelStrength;

        struct Input
        {
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _Color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            // Rim is strongest where the surface faces away from the camera.
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            half fresnel = pow(rim, _FresnelPower) * _FresnelStrength;

            // Glowing glass edge, and make those edges read more solid than the body.
            o.Emission = _FresnelColor.rgb * fresnel;
            o.Alpha = saturate(_Color.a + fresnel);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
