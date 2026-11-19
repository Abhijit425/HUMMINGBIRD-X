// Lightweight mobile post stack for the Built-in pipeline:
// 0 bloom prefilter, 1 downsample, 2 upsample (additive), 3 composite + grading + vignette,
// 4 depth-of-field blur, 5 depth-of-field combine.
Shader "Hidden/HummingbirdX/Post"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }
    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _BloomTex;
    sampler2D _BlurTex;
    sampler2D_float _CameraDepthTexture;

    float _Threshold, _Knee, _BloomIntensity;
    float _Exposure, _Contrast, _Saturation, _Vignette;
    float4 _Tint;       // rgb tint, a unused
    float4 _Lift;       // shadows colour
    float4 _Dof;        // x = focus distance, y = focus range, z = max blur, w = enabled

    struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

    v2f vert(appdata_img v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord;
        return o;
    }

    half3 Box4(float2 uv, float d)
    {
        float4 o = _MainTex_TexelSize.xyxy * float4(-d, -d, d, d);
        half3 s = tex2D(_MainTex, uv + o.xy).rgb + tex2D(_MainTex, uv + o.zy).rgb
                + tex2D(_MainTex, uv + o.xw).rgb + tex2D(_MainTex, uv + o.zw).rgb;
        return s * 0.25;
    }

    half4 fragPrefilter(v2f i) : SV_Target
    {
        half3 c = Box4(i.uv, 1.0);
        half br = max(c.r, max(c.g, c.b));
        half soft = clamp(br - _Threshold + _Knee, 0, 2 * _Knee);
        soft = soft * soft / (4 * _Knee + 1e-4);
        half contrib = max(soft, br - _Threshold) / max(br, 1e-4);
        return half4(min(c * contrib, 30.0), 1);
    }

    half4 fragDown(v2f i) : SV_Target { return half4(Box4(i.uv, 1.0), 1); }

    half4 fragUp(v2f i) : SV_Target { return half4(Box4(i.uv, 0.5), 1); }

    half3 ACESFilm(half3 x)
    {
        return saturate((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14));
    }

    half4 fragComposite(v2f i) : SV_Target
    {
        half3 c = tex2D(_MainTex, i.uv).rgb;
        c += tex2D(_BloomTex, i.uv).rgb * _BloomIntensity;
        c *= _Exposure;
        c = ACESFilm(c);
        // grading in display space
        c = pow(max(c, 0), 1.0 / 2.2);
        c = (c - 0.5) * _Contrast + 0.5;
        half l = dot(c, half3(0.2126, 0.7152, 0.0722));
        c = lerp(l.xxx, c, _Saturation);
        c *= _Tint.rgb;
        c += _Lift.rgb * (1.0 - l) * 0.1;
        float2 d = i.uv - 0.5;
        c *= 1.0 - dot(d, d) * _Vignette;
        c = pow(saturate(c), 2.2);
        return half4(c, 1);
    }

    float Coc(float2 uv)
    {
        float z = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
        return saturate(abs(z - _Dof.x) / max(_Dof.y, 0.01));
    }

    half4 fragDofBlur(v2f i) : SV_Target
    {
        // 16-tap golden-angle disc
        half3 acc = 0; half w = 0;
        float r = _Dof.z;
        for (int k = 0; k < 16; k++)
        {
            float a = k * 2.39996;
            float rr = sqrt((k + 0.5) / 16.0) * r;
            float2 o = float2(cos(a), sin(a)) * rr * _MainTex_TexelSize.xy;
            acc += tex2D(_MainTex, i.uv + o).rgb; w += 1;
        }
        return half4(acc / w, 1);
    }

    half4 fragDofCombine(v2f i) : SV_Target
    {
        half3 sharp = tex2D(_MainTex, i.uv).rgb;
        half3 blur = tex2D(_BlurTex, i.uv).rgb;
        return half4(lerp(sharp, blur, smoothstep(0.0, 1.0, Coc(i.uv))), 1);
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass { CGPROGRAM
               #pragma vertex vert
               #pragma fragment fragPrefilter
               ENDCG }
        Pass { CGPROGRAM
               #pragma vertex vert
               #pragma fragment fragDown
               ENDCG }
        Pass { Blend One One
               CGPROGRAM
               #pragma vertex vert
               #pragma fragment fragUp
               ENDCG }
        Pass { CGPROGRAM
               #pragma vertex vert
               #pragma fragment fragComposite
               ENDCG }
        Pass { CGPROGRAM
               #pragma vertex vert
               #pragma fragment fragDofBlur
               #pragma target 3.0
               ENDCG }
        Pass { CGPROGRAM
               #pragma vertex vert
               #pragma fragment fragDofCombine
               ENDCG }
    }
}
