using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HummingbirdX
{
    /// <summary>
    /// 3D rainforest soundscape: day/night ambience beds, wind and rain that follow the weather,
    /// spatial river and waterfall emitters, insects in the trees, bird calls placed around the
    /// listener, one-shots and per-category volume (master, nature, birds, water, ambient, UI).
    /// No AudioMixer asset is needed; categories are applied in code.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        class Src { public AudioSource s; public AudioCategory cat; public float baseVol; }
        readonly List<Src> loops = new List<Src>();
        readonly List<AudioSource> pool = new List<AudioSource>();
        AudioSource day, night, rain, wind, uiSrc;
        readonly List<(AudioSource s, Vector3 p, float range)> waterEmitters = new List<(AudioSource, Vector3, float)>();
        readonly List<AudioSource> insects = new List<AudioSource>();
        float callTimer = 2f;
        bool paused;

        static readonly string[] SongCalls = { "sfx_bird_1", "sfx_bird_2", "sfx_bird_3", "sfx_bird_4", "sfx_bird_5" };

        void Awake()
        {
            I = this;
            for (int i = 0; i < 12; i++)
            {
                var go = new GameObject("OneShot" + i);
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Logarithmic;
                s.minDistance = 2f; s.maxDistance = 120f;
                s.dopplerLevel = 0f;
                pool.Add(s);
            }
            uiSrc = gameObject.AddComponent<AudioSource>();
            uiSrc.playOnAwake = false; uiSrc.spatialBlend = 0f;
            Settings.Changed += ApplyVolumes;
        }

        void OnDestroy() { Settings.Changed -= ApplyVolumes; }

        public float Gain(AudioCategory c)
        {
            if (Settings.Muted) return 0f;
            float g = Settings.Volume(AudioCategory.Master);
            if (c != AudioCategory.Master) g *= Settings.Volume(c);
            return g;
        }

        public void BuildSoundscape(Transform listener)
        {
            day = Bed("amb_forest_day", AudioCategory.Nature);
            night = Bed("amb_forest_night", AudioCategory.Nature);
            rain = Bed("amb_rain", AudioCategory.Ambient);
            wind = Bed("amb_wind", AudioCategory.Ambient);

            var wg = WorldGenerator.I;
            foreach (var p in wg.WaterSoundPoints)
                waterEmitters.Add((CreateLoopAt(p, "loop_river", AudioCategory.Water, 0f, 2f, 28f), p, 28f));
            foreach (var f in wg.WaterfallSpots)
            {
                float range = f.width > 10f ? 160f : 60f;
                waterEmitters.Add((CreateLoopAt(f.pos, "loop_waterfall", AudioCategory.Water, 0f, f.width, range), f.pos, range));
            }
            int k = 0;
            foreach (var p in wg.InsectPoints)
            {
                if (k++ > 24) break;
                var s = CreateLoopAt(p, "loop_cicada", AudioCategory.Nature, 0f, 4f, 45f);
                s.pitch = Random.Range(0.85f, 1.15f);
                insects.Add(s);
            }

            var reverbGo = new GameObject("ForestReverb");
            reverbGo.transform.SetParent(listener, false);
            var rz = reverbGo.AddComponent<AudioReverbZone>();
            rz.reverbPreset = AudioReverbPreset.Forest;
            rz.minDistance = 1000f; rz.maxDistance = 2000f;
        }

        AudioSource Bed(string clip, AudioCategory cat)
        {
            var go = new GameObject("Bed_" + clip);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = Res.Clip(clip);
            s.loop = true; s.spatialBlend = 0f; s.volume = 0f; s.playOnAwake = false;
            s.Play();
            loops.Add(new Src { s = s, cat = cat, baseVol = 0f });
            return s;
        }

        public AudioSource CreateLoop(Transform parent, string clip, AudioCategory cat, float vol, float minD, float maxD)
        {
            var go = new GameObject("Loop_" + clip);
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = Res.Clip(clip);
            s.loop = true; s.spatialBlend = 1f; s.volume = vol; s.playOnAwake = false;
            s.rolloffMode = AudioRolloffMode.Logarithmic; s.minDistance = minD; s.maxDistance = maxD;
            s.dopplerLevel = 0.2f;
            if (s.clip) { s.time = Random.Range(0f, s.clip.length * 0.9f); s.Play(); }
            return s;
        }

        AudioSource CreateLoopAt(Vector3 p, string clip, AudioCategory cat, float vol, float minD, float maxD)
        {
            var s = CreateLoop(transform, clip, cat, vol, minD, maxD);
            s.transform.position = p;
            s.rolloffMode = AudioRolloffMode.Custom;
            s.SetCustomCurve(AudioSourceCurveType.CustomRolloff, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
            loops.Add(new Src { s = s, cat = cat, baseVol = 1f });
            return s;
        }

        public void PlayAt(string clip, Vector3 pos, AudioCategory cat, float vol = 1f, float pitch = 1f, float maxDist = 120f)
        {
            var c = Res.Clip(clip);
            if (c == null) return;
            AudioSource best = null;
            foreach (var s in pool) if (!s.isPlaying) { best = s; break; }
            if (best == null) best = pool[Random.Range(0, pool.Count)];
            best.transform.position = pos;
            best.clip = c;
            best.volume = vol * Gain(cat);
            best.pitch = pitch;
            best.maxDistance = maxDist;
            best.Play();
        }

        public void PlayUI(string clip, float vol = 0.6f)
        {
            var c = Res.Clip(clip);
            if (c) uiSrc.PlayOneShot(c, vol * Gain(AudioCategory.UI));
        }

        public void PlayDelayed(string clip, float delay, AudioCategory cat, float vol)
        {
            StartCoroutine(Delayed(clip, delay, cat, vol));
        }

        IEnumerator Delayed(string clip, float delay, AudioCategory cat, float vol)
        {
            yield return new WaitForSeconds(delay);
            var c = Res.Clip(clip);
            if (c) uiSrc.PlayOneShot(c, vol * Gain(cat));
        }

        void ApplyVolumes() { /* applied continuously in Update */ }

        public void SetPaused(bool p)
        {
            paused = p;
            AudioListener.pause = p;
        }

        void Update()
        {
            if (day == null) return;
            var env = EnvironmentSystem.I;
            var cam = Camera.main;
            if (env == null || cam == null) return;
            Vector3 lp = cam.transform.position;
            float night = env.Night;
            float rainAmt = env.Rain;
            float altitude = Mathf.Clamp01((lp.y - TerrainField.Height(lp.x, lp.z) - 10f) / 60f);
            float dt = Time.unscaledDeltaTime;
            float k = 1f - Mathf.Exp(-dt * 1.5f);

            float nature = Gain(AudioCategory.Nature), amb = Gain(AudioCategory.Ambient), water = Gain(AudioCategory.Water);
            day.volume = Mathf.Lerp(day.volume, nature * 0.75f * (1f - night) * (1f - rainAmt * 0.6f) * (1f - altitude * 0.4f), k);
            night.volume = Mathf.Lerp(night.volume, nature * 0.7f * night * (1f - rainAmt * 0.5f), k);
            rain.volume = Mathf.Lerp(rain.volume, amb * rainAmt * 0.85f, k);
            // wind is stronger above the canopy and in storms
            wind.volume = Mathf.Lerp(wind.volume, amb * Mathf.Clamp01(env.WindStrength * 0.35f + altitude * 0.55f + rainAmt * 0.2f), k);
            wind.pitch = 0.9f + altitude * 0.25f;

            foreach (var e in waterEmitters)
            {
                float d = Vector3.Distance(lp, e.p);
                bool active = d < e.range * 1.1f;
                if (active && !e.s.isPlaying) e.s.Play();
                else if (!active && e.s.isPlaying) e.s.Pause();
                e.s.volume = water * (e.range > 100f ? 1f : 0.7f);
            }
            foreach (var s in insects)
            {
                float hot = (1f - night) * Mathf.Clamp01(1f - Mathf.Abs(env.TimeOfDay - 14f) / 6f) * (1f - rainAmt);
                s.volume = nature * 0.45f * hot;
            }

            // bird calls around the listener: dawn chorus is busiest, rain quiets birds
            float activity = Mathf.Clamp01(1f - night * 0.85f) * (1f - rainAmt * 0.7f);
            float dawn = Mathf.Clamp01(1f - Mathf.Abs(env.TimeOfDay - 6.5f) / 2f);
            if (env.Weather == Weather.PostRain) activity *= 1.4f;
            callTimer -= Time.deltaTime * (0.5f + activity + dawn * 1.5f);
            if (callTimer <= 0f && !paused)
            {
                callTimer = Random.Range(2.5f, 7f);
                if (activity > 0.1f)
                {
                    Vector3 dir = Random.onUnitSphere; dir.y = Mathf.Abs(dir.y) * 0.6f + 0.1f;
                    float dist = Random.Range(12f, 70f);
                    PlayAt(SongCalls[Random.Range(0, SongCalls.Length)], lp + dir * dist, AudioCategory.Birds, Random.Range(0.35f, 0.8f), Random.Range(0.92f, 1.1f));
                }
            }
        }
    }
}
