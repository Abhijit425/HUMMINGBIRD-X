// Procedural skybox: gradient, sun + halo, moon, stars, drifting clouds.
Shader "HummingbirdX/Sky"
{
    Properties
    {
        _NoiseTex ("Noise", 2D) = "gray" {}
        _ZenithColor ("Zenith", Color) = (0.20,0.45,0.85,1)
        _HorizonColor ("Horizon", Color) = (0.70,0.82,0.90,1)
        _GroundColor ("Ground", Color) = (0.12,0.16,0.12,1)
        _SunColor ("Sun", Color) = (1,0.95,0.85,1)
        _SunDir ("Sun Direction", Vector) = (0,0.5,0.8,0)
        _MoonDir ("Moon Direction", Vector) = (0,-0.5,-0.8,0)
        _CloudCover ("Cloud Cover", Range(0,1)) = 0.4
        _CloudLit ("Cloud Lit", Color) = (1,1,1,1)
        _CloudShade ("Cloud Shade", Color) = (0.55,0.6,0.7,1)
        _Night ("Night", Range(0,1)) = 0
        _Exposure ("Exposure", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _NoiseTex;
            fixed4 _ZenithColor, _HorizonColor, _GroundColor, _SunColor, _CloudLit, _CloudShade;
            float4 _SunDir, _MoonDir;
            float _CloudCover, _Night, _Exposure;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float hash3(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1); p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                float3 sunD = normalize(_SunDir.xyz);
                float3 moonD = normalize(_MoonDir.xyz);

                float3 col = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(saturate(h), 0.45));
                col = lerp(col, _GroundColor.rgb, saturate(-h * 6.0));

                float sd = dot(d, sunD);
                float halo = pow(saturate(sd), 48.0) * 0.6 + pow(saturate(sd), 6.0) * 0.25;
                col += _SunColor.rgb * halo * saturate(sunD.y * 4.0 + 0.6);
                // warm horizon glow towards the sun
                col += _SunColor.rgb * pow(saturate(sd * 0.5 + 0.5), 4.0) * pow(1.0 - saturate(abs(h)), 6.0) * 0.35;
                float disc = smoothstep(0.9993, 0.9997, sd) * saturate(h * 20.0 + 0.2);
                col += _SunColor.rgb * disc * 18.0;

                float md = dot(d, moonD);
                float moon = smoothstep(0.9990, 0.9994, md);
                col += float3(0.85, 0.9, 1.0) * (moon * 3.0 + pow(saturate(md), 200.0) * 0.4) * _Night;

                // stars
                float3 sp = floor(d * 380.0);
                float st = hash3(sp);
                float star = step(0.9975, st) * (0.6 + 0.4 * sin(_Time.y * (2.0 + st * 6.0) + st * 50.0));
                col += star * _Night * saturate(h * 5.0) * (1.0 - _CloudCover);

                // clouds
                float2 cuv = d.xz / (max(h, 0.0) + 0.18);
                float2 drift = float2(_Time.x * 0.18, _Time.x * 0.07);
                float n = tex2D(_NoiseTex, cuv * 0.11 + drift).g * 0.62 + tex2D(_NoiseTex, cuv * 0.33 - drift * 1.7).r * 0.38;
                float thr = 1.0 - _CloudCover;
                float cl = smoothstep(thr - 0.05, thr + 0.28, n) * saturate(h * 5.0);
                float lit = saturate(dot(d, sunD) * 0.5 + 0.5);
                float3 cc = lerp(_CloudShade.rgb, _CloudLit.rgb, lit * 0.7 + 0.3 * (1.0 - n));
                cc += _SunColor.rgb * pow(saturate(sd), 10.0) * 0.6 * (1.0 - cl * 0.5);
                col = lerp(col, cc, cl * 0.92);

                return fixed4(col * _Exposure, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
