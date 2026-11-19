#ifndef HBX_COMMON_INCLUDED
#define HBX_COMMON_INCLUDED

// Globals driven by EnvironmentSystem / BirdController
float4 _HBX_Wind;        // xyz = wind direction (normalised), w = strength 0..2
float4 _HBX_PlayerPos;   // xyz = bird position, w = push radius
float  _HBX_PlayerPush;  // 0..1 how hard the bird pushes foliage (speed based)
float  _HBX_Wetness;     // 0..1 rain wetness
float  _HBX_Glow;        // 0..1 night glow for bioluminescent plants
float  _HBX_ShaftIntensity;

// Bends a world-space position. weight: 0 = rigid (base), 1 = fully flexible tip.
float3 HbxWind(float3 wpos, float3 nrm, float weight, float phase)
{
    float t = _Time.y;
    float s = _HBX_Wind.w;
    float ph = phase * 6.2831 + dot(wpos, float3(0.071, 0.033, 0.052));
    float sway = sin(t * 1.1 + ph) * 0.55 + sin(t * 2.3 + ph * 1.7) * 0.25 + 0.45;
    float gust = saturate(sin(t * 0.35 + wpos.x * 0.02) * 0.5 + 0.5);
    float flutter = sin(t * 8.5 + ph * 4.0 + wpos.y * 3.0) * 0.08;
    float3 disp = _HBX_Wind.xyz * (sway * (0.12 + gust * 0.18)) * s * weight;
    disp += nrm * flutter * s * weight;

    // bird downwash / push: leaves move away from the hovering hummingbird
    float3 d = wpos - _HBX_PlayerPos.xyz;
    float dist = length(d);
    float push = saturate(1.0 - dist / max(_HBX_PlayerPos.w, 0.001));
    float3 away = d / max(dist, 0.001);
    away.y -= 0.6;
    disp += away * push * push * _HBX_PlayerPush * 0.18 * weight;
    disp += nrm * push * sin(t * 40.0 + ph) * 0.01 * _HBX_PlayerPush * weight;
    return wpos + disp;
}

float HbxHash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

#endif
