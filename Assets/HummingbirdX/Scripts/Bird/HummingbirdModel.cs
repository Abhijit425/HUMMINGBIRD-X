using System.Collections.Generic;
using UnityEngine;

namespace HummingbirdX
{
    /// <summary>Plumage and proportions for a hummingbird species.</summary>
    public class HummerSpecies
    {
        public string id, name, latin, description;
        public Color back = new Color(0.12f, 0.55f, 0.18f);
        public Color belly = new Color(0.55f, 0.62f, 0.5f);
        public Color gorget = new Color(0.95f, 0.08f, 0.35f);
        public Color crown = new Color(0.10f, 0.5f, 0.2f);
        public Color tail = new Color(0.08f, 0.25f, 0.12f);
        public Color tailTip = new Color(0.05f, 0.08f, 0.06f);
        public Color nape = new Color(-1, 0, 0);   // r < 0 = same as back
        public float beakLength = 0.022f, beakCurve = 0f, tailLength = 0.03f, size = 1f, gorgetAmount = 1f;
        public float iridShift = 0.3f;

        public static HummerSpecies Player => new HummerSpecies
        {
            id = "hb_emerald", name = "Emerald Hummingbird (you)", latin = "Trochilidae",
            back = new Color(0.10f, 0.58f, 0.20f), belly = new Color(0.30f, 0.52f, 0.32f), crown = new Color(0.12f, 0.62f, 0.25f),
            gorget = new Color(0.98f, 0.10f, 0.42f), iridShift = 0.28f,
            description = "Emerald-green and fast. Hummingbirds beat their wings 50+ times a second, can hover and even fly backwards."
        };

        public static readonly HummerSpecies[] Others =
        {
            new HummerSpecies { id = "hb_jacobin", name = "White-necked Jacobin", latin = "Florisuga mellivora",
                back = new Color(0.12f, 0.5f, 0.2f), belly = new Color(0.92f, 0.94f, 0.95f), crown = new Color(0.1f, 0.25f, 0.85f),
                gorget = new Color(0.12f, 0.3f, 0.95f), nape = new Color(0.95f, 0.96f, 0.97f), tail = new Color(0.9f, 0.92f, 0.95f), tailTip = new Color(0.1f, 0.12f, 0.15f),
                iridShift = 0.55f,
                description = "Brilliant blue hood, a white crescent on the nape and a white belly. Common along Amazonian forest edges and often seen hawking insects." },
            new HummerSpecies { id = "hb_woodnymph", name = "Fork-tailed Woodnymph", latin = "Thalurania furcata",
                back = new Color(0.12f, 0.45f, 0.2f), belly = new Color(0.35f, 0.15f, 0.7f), crown = new Color(0.1f, 0.4f, 0.2f),
                gorget = new Color(0.2f, 0.85f, 0.35f), tail = new Color(0.05f, 0.07f, 0.12f), tailLength = 0.036f, iridShift = 0.15f,
                description = "Glittering green throat, violet-blue belly and a deeply forked tail. A feisty defender of flower patches inside the forest." },
            new HummerSpecies { id = "hb_hermit", name = "Long-tailed Hermit", latin = "Phaethornis superciliosus",
                back = new Color(0.42f, 0.36f, 0.22f), belly = new Color(0.62f, 0.52f, 0.4f), crown = new Color(0.35f, 0.3f, 0.2f),
                gorget = new Color(0.5f, 0.42f, 0.32f), tail = new Color(0.35f, 0.3f, 0.2f), tailTip = new Color(0.95f, 0.95f, 0.92f),
                beakLength = 0.036f, beakCurve = 0.006f, tailLength = 0.05f, gorgetAmount = 0f, size = 1.15f, iridShift = 0.1f,
                description = "Dull bronze plumage, a long curved bill for heliconias and white central tail streamers. It 'traplines', visiting the same flowers on a regular route." },
            new HummerSpecies { id = "hb_fairy", name = "Black-eared Fairy", latin = "Heliothryx auritus",
                back = new Color(0.15f, 0.6f, 0.25f), belly = new Color(0.97f, 0.97f, 0.97f), crown = new Color(0.15f, 0.65f, 0.3f),
                gorget = new Color(0.97f, 0.97f, 0.97f), gorgetAmount = 0f, tail = new Color(0.92f, 0.93f, 0.95f), tailTip = new Color(0.9f, 0.9f, 0.9f),
                beakLength = 0.018f, tailLength = 0.034f, iridShift = 0.25f,
                description = "Shining green above and snowy white below with a black mask. Often 'robs' nectar by piercing the base of flowers." },
        };
    }

    /// <summary>The bones of a built hummingbird.</summary>
    public class HummerRig
    {
        public Transform root, body, head, tail, wingL, wingR, blurL, blurR, feet;
        public Material blurMatL, blurMatR, wingMat;
        public Transform beakTip;
    }

    /// <summary>Builds a detailed procedural hummingbird mesh hierarchy (about 0.1 m long).</summary>
    public static class HummingbirdModel
    {
        static readonly Dictionary<string, Mesh[]> cache = new Dictionary<string, Mesh[]>();

        public static HummerRig Build(HummerSpecies sp, Transform parent, bool castShadows = true)
        {
            var meshes = GetMeshes(sp);
            var rig = new HummerRig();
            rig.root = new GameObject(sp.name).transform;
            rig.root.SetParent(parent, false);

            rig.body = Child("Body", rig.root, Vector3.zero);
            AddRenderer(rig.body, meshes[0], Mats.Feather, castShadows);

            rig.head = Child("Head", rig.body, new Vector3(0, 0.007f, 0.022f));
            AddRenderer(rig.head, meshes[1], new[] { Mats.Feather, Mats.Beak, Mats.Eye }, castShadows);
            rig.beakTip = Child("BeakTip", rig.head, new Vector3(0, -0.002f - sp.beakCurve, 0.013f + sp.beakLength));

            rig.tail = Child("Tail", rig.body, new Vector3(0, 0.001f, -0.028f));
            AddRenderer(rig.tail, meshes[2], Mats.Feather, castShadows);

            rig.wingMat = new Material(Mats.WingBlur);
            rig.wingR = Child("WingR", rig.body, new Vector3(0.008f, 0.009f, 0.009f));
            AddRenderer(rig.wingR, meshes[3], rig.wingMat, castShadows);
            rig.wingL = Child("WingL", rig.body, new Vector3(-0.008f, 0.009f, 0.009f));
            AddRenderer(rig.wingL, meshes[4], rig.wingMat, castShadows);

            rig.blurMatR = new Material(Mats.WingBlur);
            rig.blurMatL = rig.blurMatR;
            rig.blurR = Child("BlurR", rig.body, rig.wingR.localPosition);
            AddRenderer(rig.blurR, meshes[5], rig.blurMatR, false);
            rig.blurL = Child("BlurL", rig.body, rig.wingL.localPosition);
            AddRenderer(rig.blurL, meshes[6], rig.blurMatR, false);

            rig.feet = Child("Feet", rig.body, new Vector3(0, -0.011f, 0.004f));
            AddRenderer(rig.feet, meshes[7], Mats.Beak, false);

            rig.root.localScale = Vector3.one * sp.size;
            return rig;
        }

        static Transform Child(string name, Transform parent, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        static void AddRenderer(Transform t, Mesh m, Material mat, bool shadows) => AddRenderer(t, m, new[] { mat }, shadows);

        static void AddRenderer(Transform t, Mesh m, Material[] mats, bool shadows)
        {
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = m;
            var r = t.gameObject.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes;
        }

        static Color A(Color c, float a) { c.a = a; return c; }

        static Mesh[] GetMeshes(HummerSpecies sp)
        {
            if (cache.TryGetValue(sp.id, out var m)) return m;
            m = new Mesh[8];
            float irBack = 0.5f;
            Color nape = sp.nape.r < 0 ? sp.back : sp.nape;

            // ---------------- body: lofted torso from tail base to neck
            var b = new MeshBuilder();
            b.Loft(t => new Vector3(0, 0.004f * Mathf.Sin(t * Mathf.PI) + (t > 0.8f ? (t - 0.8f) * 0.02f : 0f), Mathf.Lerp(-0.032f, 0.026f, t)),
                   t =>
                   {
                       float p = Mathf.Pow(Mathf.Sin(Mathf.PI * (0.06f + 0.9f * t)), 0.65f);
                       float neck = t > 0.82f ? Mathf.Lerp(1f, 0.78f, (t - 0.82f) / 0.18f) : 1f;
                       return new Vector2(0.0135f * p * neck, 0.0155f * p * neck);
                   }, 18, 16,
                   (t, a) =>
                   {
                       float up = Mathf.Sin(a);
                       float topW = Mathf.Clamp01(up * 2.2f + 0.45f);
                       Color under = (t > 0.72f && sp.gorgetAmount > 0.5f) ? A(sp.gorget, 1f) : A(sp.belly, 0f);
                       if (t > 0.72f && sp.gorgetAmount <= 0.5f) under = A(sp.gorget, 0f);
                       Color top = t > 0.78f ? A(nape, nape == sp.back ? irBack : 0.1f) : A(sp.back, irBack);
                       // flank green wash
                       Color flank = Color.Lerp(A(sp.belly, 0f), A(sp.back, 0.35f), 0.55f);
                       Color c = up > 0 ? Color.Lerp(flank, top, topW) : Color.Lerp(under, flank, Mathf.Clamp01(up + 1f) * 0.6f);
                       // undertail coverts
                       if (t < 0.12f && up < 0) c = A(Color.Lerp(sp.belly, Color.white, 0.4f), 0f);
                       return c;
                   });
            m[0] = b.ToMesh(sp.id + "_body");

            // ---------------- head: rounded skull + gorget + eyes + bill
            var h = new MeshBuilder();
            h.Loft(t => new Vector3(0, 0.001f + Mathf.Sin(t * Mathf.PI) * 0.0015f, Mathf.Lerp(-0.007f, 0.0135f, t)),
                   t =>
                   {
                       float p = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Lerp(0.08f, 0.95f, t)), 0.55f);
                       return new Vector2(0.0098f * p, 0.0102f * p);
                   }, 14, 16,
                   (t, a) =>
                   {
                       float up = Mathf.Sin(a);
                       float side = Mathf.Abs(Mathf.Cos(a));
                       Color crown = A(sp.crown, 0.55f);
                       Color throat = sp.gorgetAmount > 0.5f ? A(sp.gorget, 1f) : A(sp.gorget, 0f);
                       // throat (gorget) wraps the lower half, blending into the crown at the cheeks
                       Color c = Color.Lerp(throat, crown, Mathf.Clamp01((up + 0.25f) * 4f));
                       if (sp.id == "hb_fairy" && side > 0.7f && up > -0.4f && up < 0.3f && t > 0.25f && t < 0.6f) c = A(new Color(0.03f, 0.03f, 0.03f), 0f); // black ear
                       if (sp.id == "hb_hermit" && up > 0.15f && up < 0.45f && side > 0.6f) c = A(new Color(0.85f, 0.8f, 0.65f), 0f); // pale brow stripe
                       // small white post-ocular spot
                       if (side > 0.8f && up > 0.05f && up < 0.35f && t > 0.2f && t < 0.32f) c = A(new Color(0.92f, 0.92f, 0.9f), 0f);
                       return c;
                   });
            var feather = new List<int>(h.T);
            h.T.Clear();
            // bill
            var bill = new MeshBuilder();
            var pts = new List<Vector3>(); var rad = new List<float>();
            for (int i = 0; i <= 8; i++)
            {
                float t = i / 8f;
                pts.Add(new Vector3(0, -0.0015f - sp.beakCurve * t * t, 0.0115f + t * sp.beakLength));
                rad.Add(Mathf.Lerp(0.0016f, 0.00035f, Mathf.Pow(t, 0.8f)));
            }
            bill.Col = Color.white;
            bill.Tube(pts, rad, 7, 1f, 0, 0, true);
            // eyes
            var eye = new MeshBuilder();
            eye.Sphere(new Vector3(0.0074f, 0.0028f, 0.0058f), 0.0023f, 8);
            eye.Sphere(new Vector3(-0.0074f, 0.0028f, 0.0058f), 0.0023f, 8);
            m[1] = ThreeSub(sp.id + "_head", h, feather, bill, eye);

            // ---------------- tail fan
            var tl = new MeshBuilder();
            int feathers = 8;
            for (int i = 0; i < feathers; i++)
            {
                float f = (i / (float)(feathers - 1)) * 2f - 1f;
                float ang = f * 22f;
                float len = sp.tailLength * (sp.id == "hb_woodnymph" ? (0.75f + Mathf.Abs(f) * 0.35f) : sp.id == "hb_hermit" ? (Mathf.Abs(f) < 0.3f ? 1.6f : 0.8f) : (1f - Mathf.Abs(f) * 0.15f));
                Quaternion q = Quaternion.Euler(4f, ang, 0);
                Vector3 dir = q * Vector3.back;
                Vector3 baseP = new Vector3(f * 0.003f, -0.0005f * Mathf.Abs(f), 0);
                tl.Loft(t => baseP + dir * (t * len) + Vector3.up * (0.0004f * f * f),
                        t => new Vector2(Mathf.Lerp(0.0022f, 0.0032f, t) * (t > 0.85f ? Mathf.Lerp(1f, 0.4f, (t - 0.85f) / 0.15f) : 1f), 0.0004f),
                        6, 4, (t, a) => t > 0.72f ? A(sp.tailTip, 0.1f) : A(sp.tail, 0.35f));
            }
            m[2] = tl.ToMesh(sp.id + "_tail");

            // ---------------- wings (right span +X, left mirrored)
            m[3] = WingMesh(1f, sp.id + "_wingR");
            m[4] = WingMesh(-1f, sp.id + "_wingL");
            m[5] = BlurMesh(1f);
            m[6] = BlurMesh(-1f);

            // ---------------- feet
            var ft = new MeshBuilder();
            for (int s = -1; s <= 1; s += 2)
            {
                var fp = new List<Vector3> { new Vector3(s * 0.004f, 0.002f, 0), new Vector3(s * 0.0042f, -0.004f, 0.001f), new Vector3(s * 0.0042f, -0.0055f, 0.004f) };
                var fr = new List<float> { 0.0009f, 0.0006f, 0.0004f };
                ft.Tube(fp, fr, 4, 1f, 0, 0, true);
            }
            m[7] = ft.ToMesh(sp.id + "_feet");

            cache[sp.id] = m;
            return m;
        }

        static Mesh ThreeSub(string name, MeshBuilder head, List<int> headTris, MeshBuilder bill, MeshBuilder eye)
        {
            var a = new MeshBuilder();
            a.V.AddRange(head.V); a.N.AddRange(head.N); a.UV.AddRange(head.UV); a.UV2.AddRange(head.UV2); a.C.AddRange(head.C);
            a.T.AddRange(headTris);
            var parts = new List<MeshBuilder> { a, bill, eye };
            return MeshBuilder.Multi(name, parts, new List<int>());
        }

        static Mesh WingMesh(float side, string name)
        {
            var w = new MeshBuilder();
            const float L = 0.058f;
            int spans = 6, chords = 2;
            for (int s = 0; s <= spans; s++)
            {
                float u = s / (float)spans;
                for (int c = 0; c <= chords; c++)
                {
                    float v = c / (float)chords;
                    float chord = Mathf.Lerp(0.019f, 0.006f, Mathf.Pow(u, 1.3f));
                    float z = Mathf.Lerp(-chord * 0.75f, chord * 0.25f, v) - 0.012f * u * u;
                    float y = Mathf.Sin(v * Mathf.PI) * 0.0012f * (1 - u);
                    var p = new Vector3(side * u * L, y, z);
                    w.Vert(p, Vector3.up, new Vector2(u * 0.98f + 0.01f, Mathf.Lerp(0.2f, 0.85f, v)), new Color(1, 1, 1, 1));
                }
            }
            int row = chords + 1;
            for (int s = 0; s < spans; s++)
                for (int c = 0; c < chords; c++)
                {
                    int a = s * row + c;
                    if (side > 0) w.Quad(a, a + 1, a + row + 1, a + row);
                    else w.Quad(a, a + row, a + row + 1, a + 1);
                }
            return w.ToMesh(name);
        }

        /// <summary>A soft fan representing the blurred stroke of a wing.</summary>
        static Mesh BlurMesh(float side)
        {
            var w = new MeshBuilder();
            const float L = 0.058f;
            int seg = 10;
            int c = w.Vert(Vector3.zero, Vector3.up, new Vector2(0.02f, 0.5f), new Color(1, 1, 1, 0.9f));
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Lerp(-70f, 70f, i / (float)seg) * Mathf.Deg2Rad;
                var p = new Vector3(side * Mathf.Cos(a) * L, 0, Mathf.Sin(a) * L);
                float edgeFade = Mathf.Sin(i / (float)seg * Mathf.PI);
                w.Vert(p, Vector3.up, new Vector2(0.95f, Mathf.Lerp(0.15f, 0.85f, i / (float)seg)), new Color(1, 1, 1, 0.35f + 0.65f * edgeFade));
            }
            for (int i = 0; i < seg; i++)
            {
                if (side > 0) w.Tri(c, c + 2 + i, c + 1 + i);
                else w.Tri(c, c + 1 + i, c + 2 + i);
            }
            return w.ToMesh(side > 0 ? "BlurR" : "BlurL", false);
        }
    }
}
