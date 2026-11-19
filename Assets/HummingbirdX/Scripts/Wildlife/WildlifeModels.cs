using System.Collections.Generic;
using UnityEngine;

namespace HummingbirdX
{
    public class WildSpecies
    {
        public string id, name, latin, description, kind;
        public static readonly List<WildSpecies> All = new List<WildSpecies>
        {
            new WildSpecies { id = "bf_morpho", kind = "Butterfly", name = "Blue Morpho", latin = "Morpho menelaus",
                description = "Its wings flash electric blue from microscopic scales that bend light, not from pigment. The undersides are brown camouflage." },
            new WildSpecies { id = "bf_postman", kind = "Butterfly", name = "Postman Butterfly", latin = "Heliconius melpomene",
                description = "Red and black with yellow bands. Toxic to predators, and one of the few butterflies that feeds on pollen as well as nectar." },
            new WildSpecies { id = "bf_glasswing", kind = "Butterfly", name = "Glasswing", latin = "Greta oto",
                description = "Almost invisible in flight: transparent wing panels let the forest show through." },
            new WildSpecies { id = "bf_owl", kind = "Butterfly", name = "Owl Butterfly", latin = "Caligo sp.", description = "Huge eyespots on the hindwings may startle predators. Most active at dusk." },
            new WildSpecies { id = "dragonfly", kind = "Insect", name = "Helicopter Damselfly", latin = "Mecistogaster sp.",
                description = "A giant, slow-flying damselfly with white-tipped wings that seem to rotate like rotor blades. Hunts spiders in forest gaps." },
            new WildSpecies { id = "orchidbee", kind = "Insect", name = "Orchid Bee", latin = "Euglossa sp.",
                description = "Metallic green bees. Males collect perfume from orchids to attract mates, and they pollinate many forest plants." },
            new WildSpecies { id = "macaw", kind = "Bird", name = "Scarlet Macaw", latin = "Ara macao",
                description = "Loud, brilliant and long-lived. Pairs fly over the canopy calling to each other and nest in tall emergent trees." },
            new WildSpecies { id = "toucan", kind = "Bird", name = "White-throated Toucan", latin = "Ramphastos tucanus",
                description = "Its enormous bill is light and honeycombed inside and helps shed heat. Its croaking yelp carries far across the Amazon canopy." },
            new WildSpecies { id = "tanager", kind = "Bird", name = "Paradise Tanager", latin = "Tangara chilensis",
                description = "Lime-green head, turquoise breast and a red-and-yellow rump. Travels in noisy mixed flocks through the canopy." },
            new WildSpecies { id = "firefly", kind = "Insect", name = "Fireflies", latin = "Lampyridae",
                description = "Beetles that make cold light through a chemical reaction. Each species flashes its own pattern to find a mate." },
        };
        public static WildSpecies Get(string id) { foreach (var s in All) if (s.id == id) return s; return null; }
    }

    /// <summary>Procedural meshes for forest birds and insects.</summary>
    public static class WildlifeModels
    {
        public class BirdParts { public Transform root, body, head, wingL, wingR, tail; }

        static Color A(Color c, float a) { c.a = a; return c; }

        public static BirdParts BuildBird(string kind, Transform parent)
        {
            var p = new BirdParts();
            p.root = new GameObject(kind).transform;
            p.root.SetParent(parent, false);
            p.body = new GameObject("Body").transform; p.body.SetParent(p.root, false);
            p.head = new GameObject("Head").transform; p.head.SetParent(p.body, false);
            p.tail = new GameObject("Tail").transform; p.tail.SetParent(p.body, false);
            p.wingL = new GameObject("WingL").transform; p.wingL.SetParent(p.body, false);
            p.wingR = new GameObject("WingR").transform; p.wingR.SetParent(p.body, false);

            float L; Color back, belly, headC, tailC, wingC, wingTip, beakC; float beakLen, beakDepth, tailLen, irid = 0.1f;
            Color bib = new Color(-1, 0, 0);
            switch (kind)
            {
                case "macaw":
                    L = 0.42f; back = new Color(0.88f, 0.08f, 0.06f); belly = back; headC = back; tailC = new Color(0.85f, 0.1f, 0.08f);
                    wingC = new Color(1f, 0.82f, 0.1f); wingTip = new Color(0.1f, 0.3f, 0.9f); beakC = new Color(0.92f, 0.88f, 0.8f);
                    beakLen = 0.05f; beakDepth = 0.045f; tailLen = 0.45f; irid = 0.1f; break;
                case "toucan":
                    L = 0.32f; back = new Color(0.03f, 0.03f, 0.035f); belly = back; headC = back; tailC = back;
                    wingC = back; wingTip = back; beakC = new Color(0.08f, 0.08f, 0.09f); bib = new Color(0.97f, 0.95f, 0.85f);
                    beakLen = 0.17f; beakDepth = 0.06f; tailLen = 0.16f; irid = 0.05f; break;
                default: // tanager
                    L = 0.12f; back = new Color(0.03f, 0.03f, 0.03f); belly = new Color(0.15f, 0.65f, 0.85f); headC = new Color(0.45f, 0.9f, 0.2f);
                    tailC = new Color(0.04f, 0.04f, 0.05f); wingC = new Color(0.05f, 0.05f, 0.06f); wingTip = new Color(0.15f, 0.3f, 0.6f); beakC = new Color(0.05f, 0.05f, 0.05f);
                    beakLen = 0.01f; beakDepth = 0.006f; tailLen = 0.05f; irid = 0.3f; break;
            }

            // body
            var b = new MeshBuilder();
            float R = L * 0.14f;
            b.Loft(t => new Vector3(0, 0, Mathf.Lerp(-L * 0.32f, L * 0.22f, t)),
                   t => { float pr = Mathf.Pow(Mathf.Sin(Mathf.PI * (0.05f + 0.9f * t)), 0.6f); return new Vector2(R * pr, R * 1.1f * pr); }, 12, 12,
                   (t, a) =>
                   {
                       float up = Mathf.Sin(a);
                       Color c = up > 0 ? A(back, irid) : A(belly, irid * 0.5f);
                       if (kind == "toucan" && t > 0.75f && up < 0.3f) c = A(bib, 0f);
                       if (kind == "toucan" && t < 0.15f && up < 0) c = A(new Color(0.85f, 0.1f, 0.1f), 0f);
                       if (kind == "tanager" && t < 0.3f && up > 0) c = A(t < 0.15f ? new Color(1f, 0.85f, 0.1f) : new Color(0.95f, 0.15f, 0.08f), 0f);
                       return c;
                   });
            AddMR(p.body, b.ToMesh(kind + "_body"), Mats.Feather);

            // head + beak
            p.head.localPosition = new Vector3(0, R * 0.7f, L * 0.24f);
            var h = new MeshBuilder();
            float hr = R * 0.75f;
            h.Loft(t => new Vector3(0, 0, Mathf.Lerp(-hr, hr, t)),
                   t => { float pr = Mathf.Sqrt(Mathf.Max(0.02f, 1f - Mathf.Pow(t * 2f - 1f, 2f))); return new Vector2(hr * pr, hr * pr); }, 8, 10,
                   (t, a) => (kind == "toucan" && Mathf.Sin(a) < 0.2f) ? A(bib, 0f) : A(headC, irid));
            var beak = new MeshBuilder();
            beak.Loft(t => new Vector3(0, -beakDepth * 0.15f * t * t, hr * 0.7f + t * beakLen),
                      t => { float w = Mathf.Lerp(beakDepth * 0.45f, beakDepth * 0.05f, Mathf.Pow(t, 1.4f)); return new Vector2(w * 0.7f, w); }, 8, 8,
                      (t, a) =>
                      {
                          if (kind == "toucan") return Mathf.Sin(a) > 0.7f ? new Color(0.95f, 0.8f, 0.15f) : (t < 0.12f ? new Color(0.3f, 0.5f, 0.95f) : beakC);
                          if (kind == "macaw") return Mathf.Sin(a) < 0 ? new Color(0.1f, 0.1f, 0.1f) : beakC;
                          return beakC;
                      });
            var eye = new MeshBuilder();
            eye.Sphere(new Vector3(hr * 0.75f, hr * 0.25f, hr * 0.35f), hr * 0.18f, 6);
            eye.Sphere(new Vector3(-hr * 0.75f, hr * 0.25f, hr * 0.35f), hr * 0.18f, 6);
            var hm = MeshBuilder.Multi(kind + "_head", new[] { h, beak, eye }, new List<int>());
            var hmr = AddMR(p.head, hm, Mats.Feather);
            hmr.sharedMaterials = new[] { Mats.Feather, Mats.InsectBody, Mats.Eye };

            // tail
            p.tail.localPosition = new Vector3(0, R * 0.1f, -L * 0.3f);
            var tl = new MeshBuilder();
            int feathers = kind == "macaw" ? 4 : 6;
            for (int i = 0; i < feathers; i++)
            {
                float f = feathers == 1 ? 0 : (i / (float)(feathers - 1)) * 2f - 1f;
                Vector3 dir = Quaternion.Euler(6f, f * 9f, 0) * Vector3.back;
                float len = tailLen * (kind == "macaw" ? (1f - Mathf.Abs(f) * 0.3f) : 1f);
                Color tc = kind == "macaw" && Mathf.Abs(f) > 0.5f ? new Color(0.15f, 0.35f, 0.9f) : tailC;
                tl.Loft(t => new Vector3(f * R * 0.3f, 0, 0) + dir * (t * len),
                        t => new Vector2(Mathf.Lerp(R * 0.25f, R * 0.18f, t), R * 0.03f), 4, 4, (t, a) => A(tc, irid));
            }
            AddMR(p.tail, tl.ToMesh(kind + "_tail"), Mats.Feather);

            // wings: broad feathered planes
            for (int side = -1; side <= 1; side += 2)
            {
                var w = new MeshBuilder();
                float span = L * (kind == "macaw" ? 1.05f : kind == "toucan" ? 0.7f : 0.75f);
                float chord = L * 0.28f;
                int spans = 4;
                for (int s = 0; s <= spans; s++)
                {
                    float u = s / (float)spans;
                    for (int c = 0; c <= 1; c++)
                    {
                        float ch = chord * Mathf.Lerp(1f, 0.55f, u);
                        float z = c == 0 ? ch * 0.3f : -ch * 0.7f - u * u * chord * 0.3f;
                        Color col = Color.Lerp(wingC, wingTip, Mathf.Clamp01(u * 1.4f - 0.3f + (c == 1 ? 0.2f : 0f)));
                        w.Vert(new Vector3(side * u * span, 0, z), Vector3.up, new Vector2(u, c), A(col, irid));
                    }
                }
                for (int s = 0; s < spans; s++)
                {
                    int a = s * 2;
                    if (side > 0) w.Quad(a, a + 1, a + 3, a + 2); else w.Quad(a, a + 2, a + 3, a + 1);
                    // back faces so wings are visible from below
                    if (side > 0) w.Quad(a, a + 2, a + 3, a + 1); else w.Quad(a, a + 1, a + 3, a + 2);
                }
                var wt = side < 0 ? p.wingL : p.wingR;
                wt.localPosition = new Vector3(side * R * 0.6f, R * 0.55f, L * 0.06f);
                AddMR(wt, w.ToMesh(kind + "_wing" + side), Mats.Feather);
            }
            return p;
        }

        public static MeshRenderer AddMR(Transform t, Mesh m, Material mat)
        {
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = m;
            var r = t.gameObject.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            return r;
        }

        static readonly Dictionary<string, Mesh> insectCache = new Dictionary<string, Mesh>();

        /// <summary>Butterfly: body + one wing pair per side using the atlas cell 0..3.</summary>
        public static void BuildButterfly(Transform root, int cell, out Transform wingL, out Transform wingR, float size)
        {
            var bodyM = InsectBodyMesh("bfbody", 0.022f, 0.0025f, new Color(0.08f, 0.07f, 0.06f));
            var bt = new GameObject("Body").transform; bt.SetParent(root, false);
            AddMR(bt, bodyM, Mats.InsectBody).shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            wingR = new GameObject("WingR").transform; wingR.SetParent(root, false);
            wingL = new GameObject("WingL").transform; wingL.SetParent(root, false);
            AddMR(wingR, ButterflyWing(cell, 1f), Mats.ButterflyWing);
            AddMR(wingL, ButterflyWing(cell, -1f), Mats.ButterflyWing);
            root.localScale = Vector3.one * size;
        }

        static Mesh ButterflyWing(int cell, float side)
        {
            string key = "bfw" + cell + side;
            if (insectCache.TryGetValue(key, out var m)) return m;
            float u0 = (cell % 2) * 0.5f, v1 = 1f - (cell / 2) * 0.5f, v0 = v1 - 0.5f;
            var w = new MeshBuilder();
            float s = 0.05f;
            // wing quad: root at x=0, extends to side; front at +z
            int a = w.Vert(new Vector3(0, 0, s * 0.5f), Vector3.up, new Vector2(u0, v1), new Color(1, 1, 1, 0));
            int b = w.Vert(new Vector3(side * s, 0, s * 0.5f), Vector3.up, new Vector2(u0 + 0.5f, v1), new Color(1, 1, 1, 0));
            int c = w.Vert(new Vector3(side * s, 0, -s * 0.5f), Vector3.up, new Vector2(u0 + 0.5f, v0), new Color(1, 1, 1, 0));
            int d = w.Vert(new Vector3(0, 0, -s * 0.5f), Vector3.up, new Vector2(u0, v0), new Color(1, 1, 1, 0));
            if (side > 0) w.Quad(a, b, c, d); else w.Quad(a, d, c, b);
            m = w.ToMesh(key);
            insectCache[key] = m;
            return m;
        }

        public static Mesh InsectBodyMesh(string key, float len, float rad, Color col)
        {
            if (insectCache.TryGetValue(key, out var m)) return m;
            var b = new MeshBuilder();
            b.Col = col;
            b.Loft(t => new Vector3(0, 0, Mathf.Lerp(-len * 0.5f, len * 0.5f, t)),
                   t => { float r = rad * (0.4f + 0.6f * Mathf.Sin(t * Mathf.PI)); return new Vector2(r, r); }, 8, 6);
            m = b.ToMesh(key);
            insectCache[key] = m;
            return m;
        }

        public static Mesh FlatWing(string key, float len, float width)
        {
            if (insectCache.TryGetValue(key, out var m)) return m;
            var w = new MeshBuilder();
            int a = w.Vert(new Vector3(0, 0, width * 0.5f), Vector3.up, new Vector2(0.02f, 0.75f), Color.white);
            int b = w.Vert(new Vector3(len, 0, width * 0.3f), Vector3.up, new Vector2(0.98f, 0.6f), Color.white);
            int c = w.Vert(new Vector3(len, 0, -width * 0.3f), Vector3.up, new Vector2(0.98f, 0.4f), Color.white);
            int d = w.Vert(new Vector3(0, 0, -width * 0.5f), Vector3.up, new Vector2(0.02f, 0.3f), Color.white);
            w.Quad(a, b, c, d);
            w.Quad(a, d, c, b);
            m = w.ToMesh(key);
            insectCache[key] = m;
            return m;
        }
    }
}
