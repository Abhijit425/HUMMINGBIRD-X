// Soft particle sprites. Pass blend chosen by _SrcBlend/_DstBlend (additive for pollen/fireflies/sparkles,
// alpha for rain/mist). Fog fades additive particles to black, alpha particles to fog colour.
Shader "HummingbirdX/Particle"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _TintColor ("Tint", Color) = (1,1,1,1)
        _Boost ("Intensity", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        _FadeNear ("Near Fade Distance", Float) = 0.3
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TintColor;
            float _Boost, _DstBlend, _FadeNear;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float fade : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _TintColor;
                float3 vp = UnityObjectToViewPos(v.vertex);
                o.fade = saturate((-vp.z - _FadeNear) / max(_FadeNear, 0.01));
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                c.rgb *= _Boost;
                c.a *= i.fade;
                if (_DstBlend > 0.5 && _DstBlend < 1.5)
                {
                    c.rgb *= c.a; // additive (One/One or SrcAlpha/One)
                    UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0, 0, 0, 0));
                }
                else
                {
                    UNITY_APPLY_FOG(i.fogCoord, c);
                }
                return c;
            }
            ENDCG
        }
    }
}
