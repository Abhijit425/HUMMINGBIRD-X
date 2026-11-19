// Falling water sheet: streaky scrolling foam, brighter at the lip and the plunge.
// uv.x across the fall, uv.y = 0 at the top, 1 at the bottom.
Shader "HummingbirdX/Waterfall"
{
    Properties
    {
        _NoiseTex ("Noise", 2D) = "gray" {}
        _Color ("Colour", Color) = (0.85,0.93,0.95,1)
        _Speed ("Speed", Float) = 1.2
        _Stretch ("Stretch", Float) = 0.12
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _NoiseTex;
            fixed4 _Color;
            float _Speed, _Stretch;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; UNITY_FOG_COORDS(1) };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float2 uv1 = float2(i.uv.x * 2.0, i.uv.y * _Stretch - t * 0.35);
                float2 uv2 = float2(i.uv.x * 3.3 + 0.37, i.uv.y * _Stretch * 1.6 - t * 0.5);
                float n = tex2D(_NoiseTex, uv1).r * 0.6 + tex2D(_NoiseTex, uv2).g * 0.4;
                float streak = smoothstep(0.35, 0.75, n);
                float edges = smoothstep(0.0, 0.12, i.uv.x) * smoothstep(0.0, 0.12, 1.0 - i.uv.x);
                float lip = smoothstep(0.06, 0.0, i.uv.y) * 0.6;
                float plunge = smoothstep(0.75, 1.0, i.uv.y);
                float a = saturate((0.35 + streak * 0.65 + lip + plunge * 0.5) * edges) * i.color.a;
                fixed4 c = fixed4(_Color.rgb * (0.75 + streak * 0.35 + plunge * 0.2), a);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}
