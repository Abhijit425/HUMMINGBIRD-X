// General PBR lit surface (beaks, eyes, insects, props). Vertex colour multiplies albedo.
Shader "HummingbirdX/Lit"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _Color ("Colour", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.4
        _Metallic ("Metallic", Range(0,1)) = 0
        _EmissionColor ("Emission", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex, _BumpMap;
        fixed4 _Color, _EmissionColor;
        half _Glossiness, _Metallic;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color * IN.color;
            o.Albedo = c.rgb;
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Emission = _EmissionColor.rgb * IN.color.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
