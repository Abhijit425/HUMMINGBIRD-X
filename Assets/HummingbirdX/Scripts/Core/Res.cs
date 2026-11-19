using System.Collections.Generic;
using UnityEngine;

namespace HummingbirdX
{
    /// <summary>Loads generated textures/audio/fonts from Resources and builds shared materials.</summary>
    public static class Res
    {
        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        static Font regular, semibold, light;

        public static Texture2D Tex(string name)
        {
            if (texCache.TryGetValue(name, out var t)) return t;
            t = Resources.Load<Texture2D>("Textures/" + name);
            if (t == null)
            {
                Debug.LogWarning("[HBX] missing texture " + name);
                t = Texture2D.whiteTexture;
            }
            texCache[name] = t;
            return t;
        }

        public static AudioClip Clip(string name)
        {
            if (clipCache.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + name);
            if (c == null) Debug.LogWarning("[HBX] missing audio " + name);
            clipCache[name] = c;
            return c;
        }

        public static Sprite Sprite(string name, float border = 0f)
        {
            string key = name + border;
            if (spriteCache.TryGetValue(key, out var s)) return s;
            var t = Tex(name);
            var b = border > 0 ? new Vector4(border, border, border, border) : Vector4.zero;
            s = UnityEngine.Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, b);
            spriteCache[key] = s;
            return s;
        }

        public static Font Regular => regular ? regular : (regular = LoadFont("Fonts/Inter-Regular"));
        public static Font SemiBold => semibold ? semibold : (semibold = LoadFont("Fonts/Inter-SemiBold"));
        public static Font Light => light ? light : (light = LoadFont("Fonts/InterDisplay-Light"));

        static Font LoadFont(string path)
        {
            var f = Resources.Load<Font>(path);
            if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return f;
        }

        public static Shader Shader(string name)
        {
            var s = UnityEngine.Shader.Find(name);
            if (s == null)
            {
                Debug.LogError("[HBX] shader not found: " + name);
                s = UnityEngine.Shader.Find("HummingbirdX/Lit");
            }
            return s;
        }
    }

    /// <summary>Shared material set. Created once; every generated mesh uses these.</summary>
    public static class Mats
    {
        public static Material Terrain, Bark, Vine, Rock, Leaf, Fern, Petal, FlowerSolid, Water, Waterfall,
            Feather, Beak, Eye, WingBlur, ButterflyWing, InsectBody, Shaft, ParticleAdd, ParticleAlpha, Sky, GlowFungus,
            DragonflyWing;

        static bool built;

        public static void Build()
        {
            if (built) return;
            built = true;

            Terrain = new Material(Res.Shader("HummingbirdX/Terrain")) { name = "Terrain" };
            Terrain.SetTexture("_GroundTex", Res.Tex("ground"));
            Terrain.SetTexture("_GroundNrm", Res.Tex("ground_n"));
            Terrain.SetTexture("_RockTex", Res.Tex("rock"));
            Terrain.SetTexture("_RockNrm", Res.Tex("rock_n"));
            Terrain.SetTexture("_MossTex", Res.Tex("moss"));
            Terrain.SetTexture("_NoiseTex", Res.Tex("noise_mask"));

            Bark = BarkMat("Bark", 0.75f, 0.18f);
            Vine = BarkMat("Vine", 1.1f, 0.25f);
            Vine.SetColor("_Color", new Color(0.55f, 0.62f, 0.42f));
            Rock = new Material(Res.Shader("HummingbirdX/Bark")) { name = "Rock" };
            Rock.SetTexture("_MainTex", Res.Tex("rock"));
            Rock.SetTexture("_BumpMap", Res.Tex("rock_n"));
            Rock.SetTexture("_MossTex", Res.Tex("moss"));
            Rock.SetTexture("_NoiseTex", Res.Tex("noise_mask"));
            Rock.SetFloat("_Moss", 0.9f);
            Rock.SetFloat("_Gloss", 0.3f);

            Leaf = FoliageMat("Leaf", "leaf_atlas", "leaf_atlas_n", 0.42f, 1.0f, 0.38f);
            Fern = FoliageMat("Fern", "fern", null, 0.4f, 1.1f, 0.25f);
            Petal = FoliageMat("Petal", "petal", null, 0.35f, 0.9f, 0.45f);
            Petal.SetColor("_TransColor", new Color(1f, 0.85f, 0.8f));
            Petal.SetColor("_GlowColor", new Color(0.3f, 0.9f, 1.0f));
            GlowFungus = new Material(Res.Shader("HummingbirdX/Lit")) { name = "GlowFungus" };
            GlowFungus.SetColor("_Color", new Color(0.75f, 0.85f, 0.8f));
            GlowFungus.SetFloat("_Glossiness", 0.6f);

            FlowerSolid = new Material(Res.Shader("HummingbirdX/Lit")) { name = "FlowerSolid" };
            FlowerSolid.SetFloat("_Glossiness", 0.55f);

            Water = new Material(Res.Shader("HummingbirdX/Water")) { name = "Water" };
            Water.SetTexture("_WaterNrm", Res.Tex("water_n"));
            Waterfall = new Material(Res.Shader("HummingbirdX/Waterfall")) { name = "Waterfall" };
            Waterfall.SetTexture("_NoiseTex", Res.Tex("noise_mask"));

            Feather = new Material(Res.Shader("HummingbirdX/Feather")) { name = "Feather" };
            Feather.SetTexture("_DetailNrm", Res.Tex("feather_n"));
            Feather.SetTexture("_DetailMask", Res.Tex("feather_mask"));
            Feather.enableInstancing = true;

            Beak = new Material(Res.Shader("HummingbirdX/Lit")) { name = "Beak" };
            Beak.SetColor("_Color", new Color(0.05f, 0.045f, 0.04f));
            Beak.SetFloat("_Glossiness", 0.55f);
            Eye = new Material(Res.Shader("HummingbirdX/Lit")) { name = "Eye" };
            Eye.SetColor("_Color", new Color(0.01f, 0.01f, 0.012f));
            Eye.SetFloat("_Glossiness", 0.97f);

            WingBlur = new Material(Res.Shader("HummingbirdX/Wing")) { name = "HBWing" };
            WingBlur.SetTexture("_MainTex", Res.Tex("hb_wing"));
            WingBlur.SetFloat("_Gloss", 0.7f);
            WingBlur.SetFloat("_Irid", 0.15f);

            ButterflyWing = FoliageMat("ButterflyWing", "butterfly_atlas", null, 0.3f, 0.8f, 0.4f);
            ButterflyWing.SetFloat("_WindScale", 0f);
            ButterflyWing.SetColor("_TransColor", new Color(1f, 1f, 1f));
            DragonflyWing = new Material(Res.Shader("HummingbirdX/Wing")) { name = "DragonflyWing" };
            DragonflyWing.SetTexture("_MainTex", Res.Tex("hb_wing"));
            DragonflyWing.SetColor("_Color", new Color(0.8f, 0.9f, 1f, 0.6f));
            DragonflyWing.SetFloat("_Irid", 0.6f);

            InsectBody = new Material(Res.Shader("HummingbirdX/Lit")) { name = "InsectBody" };
            InsectBody.SetFloat("_Glossiness", 0.6f);
            InsectBody.enableInstancing = true;

            Shaft = new Material(Res.Shader("HummingbirdX/LightShaft")) { name = "Shaft" };
            Shaft.SetTexture("_NoiseTex", Res.Tex("noise_mask"));

            ParticleAdd = new Material(Res.Shader("HummingbirdX/Particle")) { name = "ParticleAdd" };
            ParticleAdd.SetTexture("_MainTex", Res.Tex("ui_softdot"));
            ParticleAdd.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ParticleAdd.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            ParticleAlpha = new Material(Res.Shader("HummingbirdX/Particle")) { name = "ParticleAlpha" };
            ParticleAlpha.SetTexture("_MainTex", Res.Tex("ui_softdot"));
            ParticleAlpha.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            ParticleAlpha.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);

            Sky = new Material(Res.Shader("HummingbirdX/Sky")) { name = "Sky" };
            Sky.SetTexture("_NoiseTex", Res.Tex("noise_mask"));
        }

        static Material BarkMat(string name, float moss, float gloss)
        {
            var m = new Material(Res.Shader("HummingbirdX/Bark")) { name = name };
            m.SetTexture("_MainTex", Res.Tex("bark"));
            m.SetTexture("_BumpMap", Res.Tex("bark_n"));
            m.SetTexture("_MossTex", Res.Tex("moss"));
            m.SetTexture("_NoiseTex", Res.Tex("noise_mask"));
            m.SetFloat("_Moss", moss);
            m.SetFloat("_Gloss", gloss);
            return m;
        }

        static Material FoliageMat(string name, string tex, string nrm, float cutoff, float trans, float gloss)
        {
            var m = new Material(Res.Shader("HummingbirdX/Foliage")) { name = name };
            m.SetTexture("_MainTex", Res.Tex(tex));
            if (nrm != null) m.SetTexture("_BumpMap", Res.Tex(nrm));
            m.SetFloat("_Cutoff", cutoff);
            m.SetFloat("_Translucency", trans);
            m.SetFloat("_Gloss", gloss);
            m.enableInstancing = true;
            return m;
        }

        public static Material ParticleMat(bool additive, string texture)
        {
            var m = new Material(additive ? ParticleAdd : ParticleAlpha);
            m.SetTexture("_MainTex", Res.Tex(texture));
            return m;
        }
    }
}
