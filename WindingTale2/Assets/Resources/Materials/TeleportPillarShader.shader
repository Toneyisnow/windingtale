Shader "Custom/TeleportPillar"
{
    // The white column of light of 传送术 (see TeleportPillar). Unlit and additive, so it
    // only brightens what stands behind it. It fades out towards the top (uv.y 0 at the
    // foot, 1 at the top), and its sides are brightest where they face the camera, so the
    // column reads as round from any camera yaw.
    Properties
    {
        _Color ("Colour", Color) = (1, 1, 1, 1)
        _Falloff ("Upward falloff", Float) = 1.6
        _EdgeLevel ("Brightness at the silhouette edge", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Falloff;
            float _EdgeLevel;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Facing is judged flat on the ground: the camera looks down at the column,
                // and the sides' normals are horizontal.
                float3 n = i.worldNormal;
                n.y = 0;
                float3 v = i.viewDir;
                v.y = 0;
                float facing = abs(dot(normalize(n + 1e-5), normalize(v + 1e-5)));

                float core = lerp(_EdgeLevel, 1.0, facing);
                float fade = pow(saturate(1.0 - i.uv.y), _Falloff);

                fixed4 c = _Color;
                c.a *= core * fade;
                return c;
            }
            ENDCG
        }
    }
}
