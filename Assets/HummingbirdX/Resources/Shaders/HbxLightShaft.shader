// Volumetric-looking sun shafts through the canopy (crossed additive quads).
// uv.y = 0 at the top of the shaft, 1 at the ground. Fades when close or edge-on.
Shader "HummingbirdX/LightShaft"
{
    Properties
    {
        _NoiseTex ("Noise", 2D) = "gray" {}
        _Color ("Colour", Color) = (1,0.92,0.7,1)
        _Intensity ("Intensity", Float) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "HbxCommon.cginc"

            sampler2D _NoiseTex;
            fixed4 _Color;
            float _Intensity;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float fade : TEXCOORD1; float3 wp : TEXCOORD2; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.wp = wp;
                float3 V = normalize(_WorldSpaceCameraPos - wp);
                float3 N = normalize(UnityObjectToWorldNormal(v.normal));
                float edge = saturate(abs(dot(V, N)) * 1.5);
                float dist = length(_WorldSpaceCameraPos - wp);
                o.fade = edge * saturate((dist - 2.0) / 6.0) * saturate(1.0 - dist / 220.0);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float across = sin(i.uv.x * 3.14159);
                float along = smoothstep(0.0, 0.25, i.uv.y) * (1.0 - smoothstep(0.75, 1.0, i.uv.y));
                float n = tex2D(_NoiseTex, float2(i.uv.x * 0.6 + _Time.x * 0.3, i.wp.y * 0.01)).r;
                float dust = tex2D(_NoiseTex, i.wp.xz * 0.05 + float2(0, _Time.x)).g;
                float a = across * across * along * (0.55 + 0.6 * n) * (0.8 + 0.4 * dust);
                return fixed4(_Color.rgb * a * _Intensity * i.fade * _HBX_ShaftIntensity, 1);
            }
            ENDCG
        }
    }
}
