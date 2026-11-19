// Rivers and lakes: scrolling normals, fresnel sky reflection (from the realtime reflection probe),
// sun glints, rain ripples. Vertex colour: a = shoreline opacity, r = flow speed.
// uv = flow-aligned coordinates (u across, v along the flow).
Shader "HummingbirdX/Water"
{
    Properties
    {
        _WaterNrm ("Water Normal", 2D) = "bump" {}
        _DeepColor ("Deep Colour", Color) = (0.03,0.09,0.07,1)
        _ShallowColor ("Shallow Colour", Color) = (0.16,0.30,0.22,1)
        _FlowSpeed ("Flow Speed", Float) = 0.25
        _Tiling ("Tiling", Float) = 0.35
        _Opacity ("Opacity", Range(0,1)) = 0.86
    }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 300
        ZWrite Off

        CGPROGRAM
        #pragma surface surf Standard alpha:fade vertex:vert
        #pragma target 3.5
        #include "HbxCommon.cginc"

        sampler2D _WaterNrm;
        fixed4 _DeepColor, _ShallowColor;
        float _FlowSpeed, _Tiling;
        half _Opacity;

        struct Input
        {
            float2 uv_WaterNrm;
            float4 color : COLOR;
            float3 viewDir;
            float3 worldPos;
        };

        void vert(inout appdata_full v)
        {
            float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
            v.vertex.y += sin(_Time.y * 1.3 + wp.x * 0.4 + wp.z * 0.3) * 0.015;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float t = _Time.y;
            float speed = _FlowSpeed * (0.3 + IN.color.r);
            float2 uv = IN.uv_WaterNrm * _Tiling;
            half3 n1 = UnpackNormal(tex2D(_WaterNrm, uv + float2(0.0, -t * speed)));
            half3 n2 = UnpackNormal(tex2D(_WaterNrm, uv * 1.7 + float2(t * 0.03, -t * speed * 1.4)));
            half3 n3 = UnpackNormal(tex2D(_WaterNrm, IN.worldPos.xz * 0.9 + t * 0.21));
            half3 n = normalize(half3(n1.xy + n2.xy + n3.xy * _HBX_Wetness * 1.5, 2.2));
            o.Normal = n;

            half ndv = saturate(dot(normalize(IN.viewDir), n));
            half fres = pow(1.0 - ndv, 4.0);
            o.Albedo = lerp(_ShallowColor.rgb, _DeepColor.rgb, saturate(IN.color.a * 1.2)) * (1.0 - fres * 0.6);
            o.Smoothness = 0.96;
            o.Metallic = 0.0;
            o.Alpha = saturate(lerp(_Opacity * 0.85, 1.0, fres)) * IN.color.a;
        }
        ENDCG
    }
}
