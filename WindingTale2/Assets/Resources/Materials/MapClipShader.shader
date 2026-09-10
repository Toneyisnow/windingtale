Shader "Custom/MapClip"
{
    // Standard-lit shader (matches the obstacle/shape look) that additionally
    // discards any fragment falling outside the map rectangle. The rectangle is
    // supplied globally at map-build time via:
    //   Shader.SetGlobalVector("_MapClipMinMaxXZ", new Vector4(minX, minZ, maxX, maxZ));
    // Use it on geometry that may overhang the map edge (e.g. obstacles) so the
    // overhanging part is truncated instead of drawn outside the board.
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        // Self-illumination: the albedo, tinted by _EmissionColor, is added back at
        // _Emission strength. 0 (the default) leaves the surface lit only by the
        // scene; ObstacleGlow raises it on models that are meant to shine.
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _Emission ("Emission", Range(0,1)) = 0.0
        // Only texels whose brightest channel reaches this glow; the rest stay
        // scene-lit. 0 (the default) lets the whole surface glow. Chapter 25's lava
        // tiles use it so the white lava and its fire fringe shine while the brown
        // rock painted on the same tile does not.
        _EmissionMinBright ("Emission Min Brightness", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // addshadow regenerates the shadow caster from surf(), so clipped parts
        // cast no shadow either.
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _EmissionColor;
        half _Emission;
        half _EmissionMinBright;

        // xy = (minX, minZ), zw = (maxX, maxZ) of the map rectangle in world space.
        float4 _MapClipMinMaxXZ;

        // 1 for texels bright enough to glow, fading to 0 over the tenth below the
        // threshold; 1 everywhere when the threshold is 0.
        half glowMask(fixed3 rgb)
        {
            half brightest = max(rgb.r, max(rgb.g, rgb.b));
            return saturate((brightest - _EmissionMinBright + 0.1) * 10.0);
        }

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
            o.Emission = col.rgb * _EmissionColor.rgb * _Emission * glowMask(col.rgb);
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = col.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
