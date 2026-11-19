using UnityEngine;

namespace HummingbirdX
{
    /// <summary>
    /// Mobile post-processing for the Built-in pipeline: HDR bloom, ACES tone mapping, colour grading
    /// driven by time of day and weather, vignette and depth of field (cinematic and photo modes).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PostFx : MonoBehaviour
    {
        public bool DofEnabled;
        public float FocusDistance = 0.6f;
        public float FocusRange = 0.5f;
        public float Aperture = 1f;      // 0..1 photo slider
        public float ExposureBias = 0f;  // photo slider (stops)

        public float Exposure = 1f, Contrast = 1.05f, Saturation = 1.1f, Vignette = 0.55f;
        public Color Tint = Color.white;
        public Color Lift = new Color(0.05f, 0.1f, 0.08f);
        public float BloomIntensity = 0.55f, Threshold = 1.0f;

        Material mat;
        Camera cam;
        static readonly int idBloom = Shader.PropertyToID("_BloomTex"), idBlur = Shader.PropertyToID("_BlurTex");
        readonly RenderTexture[] chain = new RenderTexture[6];

        void Awake()
        {
            cam = GetComponent<Camera>();
            var sh = Shader.Find("Hidden/HummingbirdX/Post");
            if (sh != null && sh.isSupported) mat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            else Debug.LogWarning("[HBX] post shader unavailable; post effects disabled");
        }

        void Update()
        {
            bool dof = DofEnabled && QualityManager.I && QualityManager.I.PostFx;
            cam.depthTextureMode = dof ? DepthTextureMode.Depth : DepthTextureMode.None;
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            var q = QualityManager.I;
            if (mat == null || (q && !q.PostFx)) { Graphics.Blit(src, dst); return; }

            var fmt = src.format;
            RenderTexture working = src;
            RenderTexture dofOut = null;

            // ---- depth of field
            if (DofEnabled && cam.depthTextureMode != DepthTextureMode.None)
            {
                mat.SetVector("_Dof", new Vector4(FocusDistance, Mathf.Max(0.05f, FocusRange * FocusDistance * (1.2f - Aperture)), 4f + Aperture * 5f, 1f));
                var half = RenderTexture.GetTemporary(src.width / 2, src.height / 2, 0, fmt);
                Graphics.Blit(src, half, mat, 4);
                mat.SetTexture(idBlur, half);
                dofOut = RenderTexture.GetTemporary(src.width, src.height, 0, fmt);
                Graphics.Blit(src, dofOut, mat, 5);
                RenderTexture.ReleaseTemporary(half);
                working = dofOut;
            }

            // ---- bloom
            bool bloom = q == null || q.Bloom;
            RenderTexture bloomTex = null;
            if (bloom)
            {
                mat.SetFloat("_Threshold", Threshold);
                mat.SetFloat("_Knee", Threshold * 0.5f);
                int w = working.width / 2, h = working.height / 2;
                int levels = 0;
                chain[0] = RenderTexture.GetTemporary(w, h, 0, fmt);
                Graphics.Blit(working, chain[0], mat, 0);
                levels = 1;
                for (int i = 1; i < chain.Length; i++)
                {
                    w /= 2; h /= 2;
                    if (w < 8 || h < 8) break;
                    chain[i] = RenderTexture.GetTemporary(w, h, 0, fmt);
                    Graphics.Blit(chain[i - 1], chain[i], mat, 1);
                    levels++;
                }
                for (int i = levels - 1; i > 0; i--)
                    Graphics.Blit(chain[i], chain[i - 1], mat, 2);
                bloomTex = chain[0];
                for (int i = 1; i < levels; i++) { RenderTexture.ReleaseTemporary(chain[i]); chain[i] = null; }
            }

            // ---- composite
            mat.SetTexture(idBloom, bloomTex != null ? bloomTex : (Texture)Texture2D.blackTexture);
            mat.SetFloat("_BloomIntensity", bloom ? BloomIntensity : 0f);
            mat.SetFloat("_Exposure", Exposure * Mathf.Pow(2f, ExposureBias));
            mat.SetFloat("_Contrast", Contrast);
            mat.SetFloat("_Saturation", Saturation);
            mat.SetFloat("_Vignette", Vignette);
            mat.SetColor("_Tint", Tint);
            mat.SetColor("_Lift", Lift);
            Graphics.Blit(working, dst, mat, 3);

            if (bloomTex != null) { RenderTexture.ReleaseTemporary(bloomTex); chain[0] = null; }
            if (dofOut != null) RenderTexture.ReleaseTemporary(dofOut);
        }
    }
}
