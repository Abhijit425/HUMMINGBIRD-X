// Tree trunks, branches, vines and rocks: albedo + normal with world-up moss and rain wetness.
// Vertex colour: rgb = tint, a = wind weight (vines sway, trunks don't). uv2.x = phase.
Shader "HummingbirdX/Bark"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _MossTex ("Moss", 2D) = "white" {}
        _NoiseTex ("Noise", 2D) = "gray" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Moss ("Moss Amount", Range(0,1.5)) = 0.7
        _Gloss ("Smoothness", Range(0,1)) = 0.2
        _WindScale ("Wind Scale", Range(0,3)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard vertex:vert addshadow fullforwardshadows
        #pragma target 3.5
        #include "HbxCommon.cginc"

        sampler2D _MainTex, _BumpMap, _MossTex, _NoiseTex;
        fixed4 _Color;
        half _Moss, _Gloss, _WindScale;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
            float3 worldPos;
            float3 worldNormal; INTERNAL_DATA
        };

        void vert(inout appdata_full v)
        {
            if (v.color.a > 0.001)
            {
                float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 wn = UnityObjectToWorldNormal(v.normal);
                wpos = HbxWind(wpos, wn, v.color.a * _WindScale, v.texcoord1.x);
                v.vertex.xyz = mul(unity_WorldToObject, float4(wpos, 1.0)).xyz;
            }
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            fixed3 bark = tex2D(_MainTex, uv).rgb * IN.color.rgb * _Color.rgb;
            half3 n = UnpackNormal(tex2D(_BumpMap, uv));
            float3 up = WorldNormalVector(IN, float3(0, 0, 1));
            half nz = tex2D(_NoiseTex, IN.worldPos.xz * 0.05 + IN.worldPos.y * 0.03).r;
            half moss = saturate((up.y * 0.5 + 0.5) * 1.8 - 0.75 + (nz - 0.5) * 1.2) * _Moss;
            moss = saturate(moss * 1.5);
            fixed3 mossCol = tex2D(_MossTex, uv * 1.7).rgb;
            half wet = _HBX_Wetness;
            o.Albedo = lerp(bark, mossCol, moss) * (1.0 - wet * 0.35);
            o.Normal = normalize(lerp(n, half3(0, 0, 1), moss * 0.5));
            o.Smoothness = saturate(_Gloss + wet * 0.5 - moss * 0.1);
            o.Metallic = 0;
            o.Occlusion = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
