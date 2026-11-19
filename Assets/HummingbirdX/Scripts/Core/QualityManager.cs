using UnityEngine;

namespace HummingbirdX
{
    /// <summary>
    /// Three graphics presets plus automatic device detection and an adaptive governor that
    /// steps down when the frame rate stays low (responsiveness over visuals).
    /// </summary>
    public class QualityManager : MonoBehaviour
    {
        public static QualityManager I { get; private set; }

        public GraphicsPreset Active { get; private set; }
        public float DrawDistance { get; private set; } = 260f;
        public float DetailDistance { get; private set; } = 110f;
        public float RenderScale { get; private set; } = 1f;
        public bool Bloom { get; private set; } = true;
        public bool PostFx { get; private set; } = true;
        public float FoliageDensity { get; private set; } = 1f;
        public int ReflectionInterval { get; private set; } = 2;
        public float Fps { get; private set; } = 60f;

        int nativeW, nativeH;
        float fpsAcc; int fpsFrames; float lowTimer; float stepCooldown;

        void Awake()
        {
            I = this;
            nativeW = Screen.width; nativeH = Screen.height;
            int choice = Settings.PresetChoice;
            Apply(choice < 0 ? Detect() : (GraphicsPreset)choice);
            Settings.Changed += OnSettings;
        }

        void OnDestroy() { Settings.Changed -= OnSettings; }

        void OnSettings()
        {
            int choice = Settings.PresetChoice;
            var want = choice < 0 ? Detect() : (GraphicsPreset)choice;
            if (want != Active) Apply(want);
        }

        public static GraphicsPreset Detect()
        {
            int mem = SystemInfo.systemMemorySize;          // MB
            int gmem = SystemInfo.graphicsMemorySize;
            int cores = SystemInfo.processorCount;
            string gpu = (SystemInfo.graphicsDeviceName ?? "").ToLowerInvariant();
            bool vulkan = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Vulkan;
            int score = 0;
            if (mem >= 7000) score += 2; else if (mem >= 5000) score += 1;
            if (cores >= 8) score += 1;
            if (gmem >= 3000) score += 1;
            if (vulkan) score += 1;
            if (gpu.Contains("adreno (tm) 7") || gpu.Contains("adreno (tm) 8") || gpu.Contains("immortalis") || gpu.Contains("xclipse")
                || gpu.Contains("mali-g7") || gpu.Contains("mali-g6") && mem >= 6000) score += 2;
            if (gpu.Contains("adreno (tm) 5") || gpu.Contains("mali-g5") || gpu.Contains("powervr")) score -= 2;
            if (Application.isEditor) return GraphicsPreset.Ultra;
            return score >= 5 ? GraphicsPreset.Ultra : score >= 3 ? GraphicsPreset.High : GraphicsPreset.Performance;
        }

        public void Apply(GraphicsPreset p)
        {
            Active = p;
            switch (p)
            {
                case GraphicsPreset.Ultra:
                    RenderScale = 1f; DrawDistance = 340f; DetailDistance = 150f; FoliageDensity = 1f;
                    Bloom = true; PostFx = true; ReflectionInterval = 1;
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    QualitySettings.shadowDistance = 90f;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.antiAliasing = 4;
                    QualitySettings.pixelLightCount = 2;
                    QualitySettings.lodBias = 1.6f;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                    QualitySettings.globalTextureMipmapLimit = 0;
                    QualitySettings.softParticles = false;
                    break;
                case GraphicsPreset.High:
                    RenderScale = 0.85f; DrawDistance = 260f; DetailDistance = 110f; FoliageDensity = 0.8f;
                    Bloom = true; PostFx = true; ReflectionInterval = 3;
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    QualitySettings.shadowDistance = 55f;
                    QualitySettings.shadowCascades = 1;
                    QualitySettings.antiAliasing = 2;
                    QualitySettings.pixelLightCount = 1;
                    QualitySettings.lodBias = 1.1f;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                    QualitySettings.globalTextureMipmapLimit = 0;
                    break;
                default:
                    RenderScale = 0.7f; DrawDistance = 180f; DetailDistance = 70f; FoliageDensity = 0.55f;
                    Bloom = false; PostFx = true; ReflectionInterval = 8;
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    QualitySettings.shadowDistance = 30f;
                    QualitySettings.shadowCascades = 1;
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.pixelLightCount = 0;
                    QualitySettings.lodBias = 0.7f;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                    QualitySettings.globalTextureMipmapLimit = 1;
                    break;
            }
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.skinWeights = SkinWeights.TwoBones;
            QualitySettings.realtimeReflectionProbes = true;
            ApplyResolution();
            Debug.Log($"[HBX] Graphics preset {p} (gpu={SystemInfo.graphicsDeviceName}, ram={SystemInfo.systemMemorySize}MB)");
            var world = FindAnyObjectByType<WorldGenerator>();
            if (world) world.OnQualityChanged();
        }

        void ApplyResolution()
        {
            if (Application.isEditor || nativeW <= 0) return;
            int w = Mathf.RoundToInt(nativeW * RenderScale);
            int h = Mathf.RoundToInt(nativeH * RenderScale);
            // keep the long edge at or below 2560 for fill-rate on high-dpi tablets
            float cap = 2560f / Mathf.Max(w, h);
            if (cap < 1f) { w = Mathf.RoundToInt(w * cap); h = Mathf.RoundToInt(h * cap); }
            Screen.SetResolution(w, h, true);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            fpsAcc += dt; fpsFrames++;
            if (fpsAcc >= 0.5f)
            {
                Fps = fpsFrames / fpsAcc;
                fpsAcc = 0; fpsFrames = 0;
            }
            if (!Settings.AdaptiveQuality || Settings.PresetChoice >= 0) return;
            if (GameManager.I == null || GameManager.I.State != GameState.Explore) return;
            stepCooldown -= dt;
            if (Fps < 42f) lowTimer += dt; else lowTimer = Mathf.Max(0, lowTimer - dt * 2f);
            if (lowTimer > 6f && stepCooldown <= 0f && Active != GraphicsPreset.Performance)
            {
                Apply(Active - 1);
                lowTimer = 0; stepCooldown = 20f;
                UIManager.Toast("Graphics adjusted for smooth flight");
            }
        }
    }
}
