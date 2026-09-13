Shader "Custom/VoxelCreature"
{
    // Lighting for the map creature models (Resources/Icons/NNN/Icon_NNN_FF.obj).
    //
    // Those models are voxels: big axis-aligned faces, each one a single flat
    // colour out of a 256x1 palette. Under Standard that reads as a stack of
    // cubes -- a face either faces the sun or it doesn't, the terminator is a
    // hard line, and anything pointing away goes to near-black. The mesh export
    // now rounds the outer edges and writes smooth vertex normals, which gives
    // the light something to run across; this shader is the other half of it:
    //
    //   * wrapped diffuse -- the terminator falls off over a wide band instead
    //                        of snapping, so a face is shaded, not filled;
    //   * shadow fill     -- what the key light misses keeps its own colour,
    //                        tinted cool, instead of dropping to black;
    //   * rim             -- a thin light along the silhouette, which is what
    //                        separates a creature from the tile behind it;
    //   * sheen           -- a broad, weak highlight: enough to read as a
    //                        surface, not enough to look wet.
    //
    // Applied at spawn time by GameRenderer.ApplyCreatureMaterial, which copies
    // the palette texture off the material the model importer generated. Every
    // term is a material property, so the look can be dialled in from the
    // Inspector on a live creature without re-exporting anything.
    //
    // The blend/ZWrite properties exist because Creature.SetTransparency fades a
    // creature out by poking exactly these (see also GreyoutShader); the
    // defaults are fully opaque.
    Properties
    {
        _MainTex ("Palette (RGB)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _LightWrap ("Light wrap", Range(0,1)) = 0.45
        _Indirect ("Scene ambient", Range(0,2)) = 1.0

        _ShadowColor ("Shadow tint", Color) = (0.55,0.62,0.82,1)
        _ShadowFill ("Shadow fill", Range(0,1)) = 0.18

        _RimColor ("Rim colour", Color) = (0.72,0.80,1.0,1)
        _RimStrength ("Rim strength", Range(0,1)) = 0.48
        _RimPower ("Rim falloff", Range(0.5,24)) = 9.0

        _Sheen ("Sheen", Range(0,1)) = 0.10
        _SheenPower ("Sheen tightness", Range(1,64)) = 14

        // Runtime-controlled blend state (Creature.SetTransparency).
        [HideInInspector] _Mode ("__mode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]

        CGPROGRAM
        // The GI form of the custom lighting function, so the scene's ambient
        // is added here, once, under _Indirect -- rather than by the generated
        // code on top of whatever fill this shader already applied.
        #pragma surface surf VoxelSoft fullforwardshadows keepalpha
        #pragma target 3.0

        #include "UnityPBSLighting.cginc"

        sampler2D _MainTex;
        fixed4 _Color;

        half _LightWrap;
        half _Indirect;

        fixed4 _ShadowColor;
        half _ShadowFill;

        fixed4 _RimColor;
        half _RimStrength;
        half _RimPower;

        half _Sheen;
        half _SheenPower;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        inline void LightingVoxelSoft_GI(SurfaceOutput s, UnityGIInput data, inout UnityGI gi)
        {
            gi = UnityGlobalIllumination(data, 1.0, s.Normal);
        }

        inline half4 LightingVoxelSoft(SurfaceOutput s, half3 viewDir, UnityGI gi)
        {
            half3 n = normalize(s.Normal);
            half3 l = gi.light.dir;

            // _LightWrap = 0 is plain Lambert; at 0.45 the surface still catches
            // light about 27 degrees past the terminator, which is what turns a
            // flat voxel face into a gradient.
            half wrapped = saturate((dot(n, l) + _LightWrap) / (1.0 + _LightWrap));

            half3 h = normalize(l + normalize(viewDir));
            half sheen = pow(saturate(dot(n, h)), _SheenPower) * _Sheen * wrapped;

            half4 c;
            c.rgb = s.Albedo * gi.light.color * wrapped + gi.light.color * sheen;
            // Both fills are per-surface, not per-light: gi.indirect is already
            // zero in an additive pass, and the shadow fill is skipped there by
            // hand, so a second light in the scene cannot double them up.
            #ifndef UNITY_PASS_FORWARDADD
            c.rgb += s.Albedo * gi.indirect.diffuse * _Indirect;
            c.rgb += s.Albedo * _ShadowColor.rgb * ((1.0 - wrapped) * _ShadowFill);
            #endif
            c.a = s.Alpha;
            return c;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;

            // Rim is a view effect: through Emission so it is added once in the
            // base pass rather than again for every light.
            half rim = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), _RimPower);
            o.Emission = _RimColor.rgb * (rim * _RimStrength);
        }
        ENDCG
    }

    FallBack "Diffuse"
}
