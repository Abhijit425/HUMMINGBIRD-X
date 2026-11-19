// Two-sided alpha-tested foliage with wind, bird push, translucency, wetness and night glow.
// Vertex colour: rgb = tint, a = wind weight. uv2.x = per-plant phase.
Shader "HummingbirdX/Foliage"
{
    Properties
    {
        _MainTex ("Albedo (RGBA)", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.45
        _Translucency ("Translucency", Range(0,3)) = 1.0
        _TransColor ("Translucency Colour", Color) = (0.55,0.85,0.15,1)
        _Gloss ("Smoothness", Range(0,1)) = 0.35
        _WindScale ("Wind Scale", Range(0,3)) = 1
        _GlowColor ("Night Glow Colour", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
        LOD 300
        Cull Off

        CGPROGRAM
        #pragma surface surf Foliage vertex:vert alphatest:_Cutoff addshadow fullforwardshadows
        #pragma target 3.5
        #include "UnityPBSLighting.cginc"
        #include "HbxCommon.cginc"

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        half _Translucency;
        fixed4 _TransColor;
        half _Gloss;
        half _WindScale;
        fixed4 _GlowColor;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
            float facing : VFACE;
        };

        void vert(inout appdata_full v)
        {
            float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float3 wn = UnityObjectToWorldNormal(v.normal);
            wpos = HbxWind(wpos, wn, v.color.a * _WindScale, v.texcoord1.x);
            v.vertex.xyz = mul(unity_WorldToObject, float4(wpos, 1.0)).xyz;
        }

        inline half4 LightingFoliage(SurfaceOutputStandard s, half3 viewDir, UnityGI gi)
        {
            half4 c = LightingStandard(s, viewDir, gi);
            half3 L = gi.light.dir;
            half back = saturate(dot(viewDir, -L));
            half thru = pow(back, 3.0) * 0.8 + saturate(dot(-s.Normal, L)) * 0.35;
            c.rgb += s.Albedo * _TransColor.rgb * gi.light.color * thru * _Translucency;
            return c;
        }

        inline void LightingFoliage_GI(SurfaceOutputStandard s, UnityGIInput data, inout UnityGI gi)
        {
            LightingStandard_GI(s, data, gi);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            half3 tint = IN.color.rgb * _Color.rgb;
            half wet = _HBX_Wetness;
            o.Albedo = c.rgb * tint * (1.0 - wet * 0.25);
            half3 n = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            n.z *= (IN.facing > 0) ? 1.0 : -1.0;
            o.Normal = n;
            o.Smoothness = saturate(_Gloss + wet * 0.45);
            o.Metallic = 0;
            o.Occlusion = lerp(0.65, 1.0, IN.uv_MainTex.y);
            o.Emission = _GlowColor.rgb * c.rgb * _HBX_Glow * 2.0;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}
