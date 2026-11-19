// Bird plumage: vertex-coloured feather regions with thin-film iridescence.
// Vertex colour: rgb = base plumage, a = iridescence (0 none, ~0.5 back sheen, 1 = gorget flash).
Shader "HummingbirdX/Feather"
{
    Properties
    {
        _DetailNrm ("Feather Normal", 2D) = "bump" {}
        _DetailMask ("Feather Mask", 2D) = "white" {}
        _Tiling ("Feather Tiling", Float) = 6
        _Color ("Tint", Color) = (1,1,1,1)
        _GorgetColor ("Gorget Colour", Color) = (0.9,0.05,0.25,1)
        _IridShift ("Iridescence Shift", Range(0,1)) = 0.3
        _IridStrength ("Iridescence Strength", Range(0,2)) = 1
        _Gloss ("Smoothness", Range(0,1)) = 0.55
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.5

        sampler2D _DetailNrm, _DetailMask;
        float _Tiling;
        fixed4 _Color, _GorgetColor;
        half _IridShift, _IridStrength, _Gloss;

        struct Input
        {
            float2 uv_DetailNrm;
            float4 color : COLOR;
            float3 viewDir;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_DetailNrm * _Tiling;
            half3 n = UnpackNormal(tex2D(_DetailNrm, uv));
            n.xy *= 0.6;
            o.Normal = normalize(n);
            half mask = tex2D(_DetailMask, uv).r;

            half ndv = saturate(dot(normalize(IN.viewDir), o.Normal));
            half irid = IN.color.a * _IridStrength;
            // thin-film style hue rotation with view angle
            float h = ndv * 1.4 + _IridShift;
            half3 film = 0.5 + 0.5 * cos(6.28318 * (float3(0.0, 0.33, 0.67) + h));
            half gorget = saturate((IN.color.a - 0.75) * 4.0);

            half3 base = IN.color.rgb * _Color.rgb * (0.82 + 0.18 * mask);
            half3 green = base * lerp(half3(1, 1, 1), film * 1.6, saturate(irid) * 0.55);
            half3 throat = lerp(_GorgetColor.rgb * 0.15, _GorgetColor.rgb * (0.6 + film * 0.6), pow(ndv, 2.0));
            o.Albedo = lerp(green, throat, gorget);
            o.Metallic = saturate(irid * 0.55);
            o.Smoothness = saturate(_Gloss + irid * 0.2) * (0.85 + 0.15 * mask);
            o.Emission = _GorgetColor.rgb * pow(ndv, 6.0) * gorget * 0.6;
            o.Occlusion = 0.85 + 0.15 * mask;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
