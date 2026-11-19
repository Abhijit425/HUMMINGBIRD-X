using UnityEngine;
using UnityEngine.Rendering;

namespace HummingbirdX
{
    public enum Weather { Sunny, Cloudy, LightRain, HeavyRain, MistyMorning, PostRain }

    /// <summary>
    /// Day/night cycle and weather: sun and moon lights, sky, ambient light, fog, wind, rain,
    /// thunder, wetness, light shafts, night glow, reflections and colour grading.
    /// </summary>
    public class EnvironmentSystem : MonoBehaviour
    {
        public static EnvironmentSystem I { get; private set; }

        public float TimeOfDay = 8.5f;          // hours 0..24
        public bool TimePaused;
        public Weather Weather { get; private set; } = Weather.Sunny;
        public float Night { get; private set; }       // 0 day .. 1 night
        public float SunElevation { get; private set; }
        public float Rain { get; private set; }        // 0..1
        public float WindStrength { get; private set; } = 0.5f;
        public Vector3 WindVector { get; private set; }
        public float Wetness { get; private set; }
        public float Cloud { get; private set; }
        public float Mist { get; private set; }
        public bool ThunderActive => Rain > 0.75f;

        Light sun, moon;
        ReflectionProbe probe;
        ParticleSystem rain, pollen, fireflies;
        float probeTimer, weatherTimer, thunderTimer, flash;
        // weather targets
        float tCloud, tRain, tMist, tWind = 0.5f, tWet;
        Vector3 windDir = new Vector3(0.8f, 0, 0.6f).normalized;

        public static readonly string[] WeatherNames = { "Sunny", "Cloudy", "Light rain", "Heavy rain", "Misty morning", "After the rain" };

        void Awake() { I = this; }

        public void Build(Transform cameraT)
        {
            var sGo = new GameObject("Sun");
            sGo.transform.SetParent(transform, false);
            sun = sGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sun.shadowBias = 0.04f;
            sun.shadowNormalBias = 0.3f;
            sun.renderMode = LightRenderMode.ForcePixel;
            RenderSettings.sun = sun;

            var mGo = new GameObject("Moon");
            mGo.transform.SetParent(transform, false);
            moon = mGo.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.shadows = LightShadows.None;
            moon.color = new Color(0.55f, 0.65f, 1f);
            moon.renderMode = LightRenderMode.ForcePixel;

            RenderSettings.skybox = Mats.Sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            var pGo = new GameObject("SkyProbe");
            pGo.transform.SetParent(transform, false);
            probe = pGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.size = new Vector3(2000f, 2000f, 2000f);
            probe.cullingMask = 0;
            probe.clearFlags = ReflectionProbeClearFlags.Skybox;
            probe.resolution = 64;
            probe.hdr = true;
            probe.importance = 0;

            rain = Fx.Rain(cameraT);
            pollen = Fx.Motes(cameraT, false);
            fireflies = Fx.Motes(cameraT, true);

            SetWeather(Weather.Sunny, true);
            Apply(0f, true);
        }

        public void SetTime(float hours) { TimeOfDay = Mathf.Repeat(hours, 24f); Apply(0, true); }

        public void SetWeather(Weather w, bool instant = false)
        {
            Weather = w;
            switch (w)
            {
                case Weather.Sunny: tCloud = 0.25f; tRain = 0; tMist = 0.1f; tWind = 0.45f; tWet = 0f; break;
                case Weather.Cloudy: tCloud = 0.75f; tRain = 0; tMist = 0.25f; tWind = 0.75f; tWet = 0.05f; break;
                case Weather.LightRain: tCloud = 0.85f; tRain = 0.4f; tMist = 0.35f; tWind = 0.8f; tWet = 0.7f; break;
                case Weather.HeavyRain: tCloud = 0.97f; tRain = 1f; tMist = 0.5f; tWind = 1.35f; tWet = 1f; break;
                case Weather.MistyMorning: tCloud = 0.45f; tRain = 0; tMist = 1f; tWind = 0.2f; tWet = 0.35f; break;
                case Weather.PostRain: tCloud = 0.5f; tRain = 0; tMist = 0.6f; tWind = 0.35f; tWet = 0.8f; break;
            }
            if (instant) { Cloud = tCloud; Rain = tRain; Mist = tMist; WindStrength = tWind; Wetness = tWet; }
            weatherTimer = Random.Range(180f, 360f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Apply(dt, false);
        }

        void Apply(float dt, bool instant)
        {
            if (!TimePaused && dt > 0f)
            {
                float hoursPerSecond = 24f / Mathf.Max(1f, Settings.DayLengthMinutes * 60f);
                TimeOfDay = Mathf.Repeat(TimeOfDay + dt * hoursPerSecond, 24f);
            }

            // weather blending + automatic changes
            float wk = instant ? 1f : 1f - Mathf.Exp(-dt / 12f);
            Cloud = Mathf.Lerp(Cloud, tCloud, wk);
            Rain = Mathf.Lerp(Rain, tRain, wk);
            Mist = Mathf.Lerp(Mist, tMist, wk);
            WindStrength = Mathf.Lerp(WindStrength, tWind, wk);
            Wetness = Mathf.Lerp(Wetness, Mathf.Max(tWet, Rain), instant ? 1f : 1f - Mathf.Exp(-dt / (tWet > Wetness ? 20f : 90f)));
            if (Settings.DynamicWeather && dt > 0f)
            {
                weatherTimer -= dt;
                if (weatherTimer <= 0f) SetWeather(NextWeather());
            }

            // sun path: rises at 6, sets at 18
            float dayT = (TimeOfDay - 6f) / 12f;
            float sunAngle = dayT * 180f;
            Quaternion sunRot = Quaternion.Euler(sunAngle, -35f, 0f) * Quaternion.Euler(0, 0, 0);
            sun.transform.rotation = sunRot;
            Vector3 sunDir = -sun.transform.forward;
            SunElevation = sunDir.y;
            moon.transform.rotation = Quaternion.Euler(sunAngle + 180f, -20f, 0f);
            Vector3 moonDir = -moon.transform.forward;

            float day = Mathf.Clamp01(SunElevation * 4f + 0.25f);
            Night = 1f - Mathf.Clamp01(SunElevation * 5f + 0.6f);
            float golden = Mathf.Clamp01(1f - Mathf.Abs(SunElevation - 0.12f) * 6f) * day;   // low sun warmth
            float overcast = Cloud;

            // ---- light colours
            Color noon = new Color(1f, 0.96f, 0.88f);
            Color gold = new Color(1f, 0.62f, 0.32f);
            Color sunC = Color.Lerp(noon, gold, golden);
            sun.color = sunC;
            flash = Mathf.MoveTowards(flash, 0f, dt * 6f);
            sun.intensity = (Mathf.Clamp01(SunElevation * 6f) * 1.35f * Mathf.Lerp(1f, 0.35f, overcast) + flash * 2.5f) * (1f - Mist * 0.25f);
            sun.shadowStrength = Mathf.Lerp(0.85f, 0.35f, overcast);
            sun.enabled = sun.intensity > 0.01f;
            moon.intensity = Night * 0.32f * Mathf.Lerp(1f, 0.4f, overcast) + flash * 0.8f * Night;
            moon.enabled = moon.intensity > 0.01f;

            // ---- sky
            Color zenithDay = Color.Lerp(new Color(0.18f, 0.42f, 0.82f), new Color(0.45f, 0.5f, 0.56f), overcast);
            Color horizonDay = Color.Lerp(new Color(0.68f, 0.82f, 0.9f), new Color(0.6f, 0.64f, 0.66f), overcast);
            Color zenithNight = new Color(0.01f, 0.018f, 0.05f);
            Color horizonNight = new Color(0.03f, 0.05f, 0.09f);
            Color zenith = Color.Lerp(zenithNight, zenithDay, day);
            Color horizon = Color.Lerp(horizonNight, horizonDay, day);
            horizon = Color.Lerp(horizon, new Color(1f, 0.55f, 0.3f), golden * 0.75f * (1f - overcast * 0.6f));
            zenith = Color.Lerp(zenith, new Color(0.35f, 0.38f, 0.6f), golden * 0.35f);
            var sky = Mats.Sky;
            sky.SetColor("_ZenithColor", zenith);
            sky.SetColor("_HorizonColor", horizon);
            sky.SetColor("_GroundColor", horizon * 0.35f);
            sky.SetColor("_SunColor", sunC * Mathf.Lerp(1f, 0.3f, overcast));
            sky.SetVector("_SunDir", sunDir);
            sky.SetVector("_MoonDir", moonDir);
            sky.SetFloat("_CloudCover", Mathf.Lerp(0.15f, 0.95f, Cloud));
            sky.SetColor("_CloudLit", Color.Lerp(new Color(0.25f, 0.28f, 0.35f), Color.Lerp(Color.white, new Color(1f, 0.75f, 0.55f), golden), day));
            sky.SetColor("_CloudShade", Color.Lerp(new Color(0.05f, 0.06f, 0.1f), new Color(0.5f, 0.55f, 0.62f), day) * Mathf.Lerp(1f, 0.75f, overcast));
            sky.SetFloat("_Night", Night);
            sky.SetFloat("_Exposure", 1f + flash * 2f);

            // ---- ambient (trilight) — the forest bounces green light
            Color ambSky = Color.Lerp(new Color(0.03f, 0.04f, 0.08f), Color.Lerp(new Color(0.42f, 0.55f, 0.68f), new Color(0.45f, 0.48f, 0.5f), overcast), day);
            ambSky = Color.Lerp(ambSky, new Color(0.7f, 0.5f, 0.38f), golden * 0.4f);
            Color ambEq = Color.Lerp(new Color(0.02f, 0.04f, 0.05f), new Color(0.24f, 0.33f, 0.22f), day);
            Color ambGround = Color.Lerp(new Color(0.01f, 0.02f, 0.02f), new Color(0.12f, 0.16f, 0.08f), day);
            RenderSettings.ambientSkyColor = ambSky + Color.white * flash * 0.4f;
            RenderSettings.ambientEquatorColor = ambEq;
            RenderSettings.ambientGroundColor = ambGround;

            // ---- fog
            Color fogDay = Color.Lerp(new Color(0.5f, 0.62f, 0.62f), new Color(0.58f, 0.62f, 0.64f), overcast);
            Color fogC = Color.Lerp(new Color(0.02f, 0.035f, 0.05f), fogDay, day);
            fogC = Color.Lerp(fogC, new Color(0.85f, 0.6f, 0.42f), golden * 0.45f);
            RenderSettings.fogColor = fogC;
            float baseDensity = 0.0042f * Mathf.Lerp(1f, 0.75f, QualityManager.I ? (QualityManager.I.DrawDistance - 180f) / 160f : 0.5f);
            RenderSettings.fogDensity = baseDensity + Mist * 0.008f + Rain * 0.004f + Night * 0.0015f;

            // ---- wind
            windDir = Quaternion.Euler(0, Mathf.Sin(Time.time * 0.01f) * 40f * dt, 0) * windDir;
            WindVector = windDir * WindStrength;
            float altitudeWind = 0f;
            if (BirdController.I)
            {
                var bp = BirdController.I.transform.position;
                altitudeWind = Mathf.Clamp01((bp.y - TerrainField.Height(bp.x, bp.z) - 15f) / 50f);
            }
            Shader.SetGlobalVector("_HBX_Wind", new Vector4(windDir.x, 0, windDir.z, WindStrength * (1f + altitudeWind * 0.3f)));
            Shader.SetGlobalFloat("_HBX_Wetness", Wetness);
            Shader.SetGlobalFloat("_HBX_Glow", Night);
            Shader.SetGlobalFloat("_HBX_ShaftIntensity", Mathf.Clamp01(SunElevation * 3f) * (1f - overcast * 0.85f) * (1f + Mist * 0.8f) * (1f - Rain));
            Mats.Shaft.SetColor("_Color", sunC);
            Mats.GlowFungus.SetColor("_EmissionColor", new Color(0.4f, 1f, 0.9f) * (0.15f + Night * 1.6f));
            foreach (var s in FlowerSpecies.All)
                if (s.glows && s.mats != null && s.mats.Length > 0)
                    s.mats[0].SetColor("_EmissionColor", s.accent * (0.05f + Night * 0.9f));

            // ---- particles
            if (rain)
            {
                var em = rain.emission;
                em.rateOverTime = Rain * 1600f;
            }
            if (pollen)
            {
                var em = pollen.emission;
                em.rateOverTime = 24f * day * (1f - Rain) * (1f + Mist);
            }
            if (fireflies)
            {
                var em = fireflies.emission;
                em.rateOverTime = 28f * Night * (1f - Rain);
            }

            // ---- thunder
            if (Rain > 0.75f && dt > 0f)
            {
                thunderTimer -= dt;
                if (thunderTimer <= 0f)
                {
                    thunderTimer = Random.Range(12f, 35f);
                    flash = 1f;
                    float delay = Random.Range(0.6f, 3f);
                    AudioManager.I.PlayDelayed(Random.value < 0.5f ? "sfx_thunder_1" : "sfx_thunder_2", delay, AudioCategory.Ambient, 0.9f);
                }
            }

            // ---- grading
            var cr = CameraRig.I;
            if (cr && cr.Post)
            {
                var p = cr.Post;
                p.Tint = Color.Lerp(Color.Lerp(new Color(0.75f, 0.85f, 1.1f), Color.white, day), new Color(1.12f, 0.98f, 0.85f), golden * 0.7f);
                p.Saturation = Mathf.Lerp(0.85f, 1.15f, day) * Mathf.Lerp(1f, 0.85f, overcast);
                p.Contrast = Mathf.Lerp(1.0f, 1.08f, day) - Mist * 0.06f;
                p.Exposure = Mathf.Lerp(1.6f, 1.0f, day) * (1f + Night * 0.25f);
                p.BloomIntensity = Mathf.Lerp(0.8f, 0.5f, day) + golden * 0.25f;
                p.Threshold = Mathf.Lerp(0.6f, 1.0f, day);
            }

            // ---- reflection probe refresh
            probeTimer -= dt;
            if (instant || probeTimer <= 0f)
            {
                probeTimer = QualityManager.I ? QualityManager.I.ReflectionInterval : 2f;
                RenderSettings.customReflectionTexture = probe.texture;
                probe.RenderProbe();
            }
        }

        Weather NextWeather()
        {
            float h = TimeOfDay;
            float r = Random.value;
            switch (Weather)
            {
                case Weather.HeavyRain: return Weather.PostRain;
                case Weather.LightRain: return r < 0.4f ? Weather.HeavyRain : Weather.PostRain;
                case Weather.PostRain: return Weather.Sunny;
                case Weather.MistyMorning: return Weather.Sunny;
            }
            if (h > 4.5f && h < 8f && r < 0.5f) return Weather.MistyMorning;
            if (r < 0.45f) return Weather.Sunny;
            if (r < 0.7f) return Weather.Cloudy;
            return Weather.LightRain;
        }

        public string TimeLabel()
        {
            float h = TimeOfDay;
            if (h >= 5f && h < 7f) return "Sunrise";
            if (h >= 7f && h < 11f) return "Morning";
            if (h >= 11f && h < 16f) return "Afternoon";
            if (h >= 16f && h < 17.6f) return "Golden hour";
            if (h >= 17.6f && h < 19f) return "Sunset";
            return "Night";
        }
    }
}
