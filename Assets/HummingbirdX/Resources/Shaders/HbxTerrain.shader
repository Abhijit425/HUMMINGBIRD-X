// Ground: world-projected leaf litter on flats, rock on slopes, moss patches, mud near water.
// Vertex colour: r = mud/wet (near rivers), g = moss bias.
Shader "HummingbirdX/Terrain"
{
    Properties
    {
        _GroundTex ("Ground", 2D) = "white" {}
        _GroundNrm ("Ground Normal", 2D) = "bump" {}
        _RockTex ("Rock", 2D) = "white" {}
        _RockNrm ("Rock Normal", 2D) = "bump" {}
        _MossTex ("Moss", 2D) = "white" {}
        _NoiseTex ("Noise", 2D) = "gray" {}
        _Tiling ("Ground Tiling", Float) = 0.18
        _RockTiling ("Rock Tiling", Float) = 0.12
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.5
        #include "HbxCommon.cginc"

        sampler2D _GroundTex, _GroundNrm, _RockTex, _RockNrm, _MossTex, _NoiseTex;
        float _Tiling, _RockTiling;

        struct Input
        {
            float3 worldPos;
            float4 color : COLOR;
            float3 worldNormal; INTERNAL_DATA
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 wp = IN.worldPos;
            float3 wn = WorldNormalVector(IN, float3(0, 0, 1));
            float slope = 1.0 - saturate(wn.y);
            half nz = tex2D(_NoiseTex, wp.xz * 0.013).g;
            half nz2 = tex2D(_NoiseTex, wp.xz * 0.061).r;

            float2 guv = wp.xz * _Tiling;
            fixed3 ground = tex2D(_GroundTex, guv).rgb;
            half3 gn = UnpackNormal(tex2D(_GroundNrm, guv));

            // rock: pick the projection facing the slope
            float2 ruv = (abs(wn.x) > abs(wn.z) ? wp.zy : wp.xy) * _RockTiling;
            fixed3 rock = tex2D(_RockTex, ruv).rgb;
            half3 rn = UnpackNormal(tex2D(_RockNrm, ruv));

            fixed3 moss = tex2D(_MossTex, wp.xz * 0.21).rgb;
            half rockAmt = saturate((slope - 0.38 + (nz2 - 0.5) * 0.3) * 6.0);
            half mossAmt = saturate((nz - 0.45) * 3.0 + IN.color.g * 0.8) * (1.0 - rockAmt * 0.6);
            half mud = saturate(IN.color.r);

            fixed3 col = lerp(ground, moss, mossAmt * 0.85);
            col = lerp(col, col * fixed3(0.45, 0.38, 0.3), mud * 0.8);
            col = lerp(col, rock, rockAmt);
            half wet = saturate(_HBX_Wetness + mud * 0.6);
            o.Albedo = col * (1.0 - wet * 0.3);
            o.Normal = normalize(lerp(gn, rn, rockAmt));
            o.Smoothness = saturate(0.08 + wet * 0.55 + rockAmt * 0.1);
            o.Metallic = 0;
            o.Occlusion = lerp(0.75, 1.0, nz2);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
