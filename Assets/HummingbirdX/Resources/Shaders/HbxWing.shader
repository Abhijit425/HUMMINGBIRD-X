// Translucent lit wings (hummingbird motion-blurred wings, dragonfly / butterfly wings).
// _Blur fades the wing into a soft motion disc; vertex colour alpha scales opacity.
Shader "HummingbirdX/Wing"
{
    Properties
    {
        _MainTex ("Wing (RGBA)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Opacity ("Opacity", Range(0,1)) = 1
        _Gloss ("Smoothness", Range(0,1)) = 0.6
        _Irid ("Iridescence", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        Cull Off ZWrite Off

        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        half _Opacity, _Gloss, _Irid;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
            float3 viewDir;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            half ndv = saturate(abs(dot(normalize(IN.viewDir), half3(0, 0, 1))));
            half3 film = 0.5 + 0.5 * cos(6.28318 * (half3(0.0, 0.33, 0.67) + ndv * 1.3 + 0.55));
            o.Albedo = lerp(c.rgb, c.rgb * film * 1.8, _Irid) * IN.color.rgb;
            o.Smoothness = _Gloss;
            o.Metallic = _Irid * 0.4;
            o.Alpha = c.a * _Opacity * IN.color.a;
        }
        ENDCG
    }
}
