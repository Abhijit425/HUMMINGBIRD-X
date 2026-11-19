using System.Collections.Generic;
using UnityEngine;

namespace HummingbirdX
{
    public enum BloomShape { TubeCluster, Heliconia, ShrimpSpike, GingerSpike, Passion, Moonflower, Orchid }

    public class FlowerSpecies
    {
        public string id, name, latin, description;
        public BloomShape shape;
        public Color color, accent;
        public float nectar;          // units per full flower
        public float refillSeconds;
        public bool nightBloom;       // richer and open at night
        public bool glows;
        public Mesh mesh;
        public Material[] mats;
        public Vector3 mouthLocal;    // nectar point
        public Vector3 approachLocal; // direction the bird hovers from
        public float plantHeight;

        public static readonly List<FlowerSpecies> All = new List<FlowerSpecies>
        {
            new FlowerSpecies { id = "fl_hamelia", name = "Scarlet Bush", latin = "Hamelia patens", shape = BloomShape.TubeCluster,
                color = new Color(0.95f, 0.22f, 0.08f), accent = new Color(1f, 0.55f, 0.1f), nectar = 3f, refillSeconds = 30f,
                description = "Clusters of slender red-orange tubes. A native shrub of tropical America and a hummingbird favourite: its long tubes suit long bills." },
            new FlowerSpecies { id = "fl_heliconia", name = "Lobster-claw Heliconia", latin = "Heliconia rostrata", shape = BloomShape.Heliconia,
                color = new Color(0.9f, 0.1f, 0.08f), accent = new Color(1f, 0.85f, 0.15f), nectar = 4f, refillSeconds = 40f,
                description = "Hanging zigzag bracts in red tipped with yellow. The true flowers hide inside each bract, reachable by hummingbird bills." },
            new FlowerSpecies { id = "fl_shrimp", name = "Golden Shrimp Plant", latin = "Pachystachys lutea", shape = BloomShape.ShrimpSpike,
                color = new Color(1f, 0.8f, 0.1f), accent = new Color(0.98f, 0.98f, 0.95f), nectar = 2.5f, refillSeconds = 25f,
                description = "Upright golden bract spikes with short-lived white tubular flowers. Native to Peru." },
            new FlowerSpecies { id = "fl_ginger", name = "Blue Ginger", latin = "Dichorisandra thyrsiflora", shape = BloomShape.GingerSpike,
                color = new Color(0.25f, 0.3f, 0.95f), accent = new Color(0.9f, 0.9f, 1f), nectar = 1.5f, refillSeconds = 20f,
                description = "Dense spikes of blue-violet blooms on cane-like stems. Despite the name, it is a spiderwort, not a true ginger." },
            new FlowerSpecies { id = "fl_passion", name = "Red Passionflower", latin = "Passiflora coccinea", shape = BloomShape.Passion,
                color = new Color(0.9f, 0.08f, 0.18f), accent = new Color(0.7f, 0.25f, 0.85f), nectar = 5f, refillSeconds = 60f,
                description = "A vine of the Amazon basin with dazzling scarlet flowers and a purple corona. Pollinated by hummingbirds." },
            new FlowerSpecies { id = "fl_purple", name = "Purple Glory Trumpet", latin = "Brunfelsia sp.", shape = BloomShape.TubeCluster,
                color = new Color(0.55f, 0.18f, 0.85f), accent = new Color(0.95f, 0.9f, 1f), nectar = 2f, refillSeconds = 25f,
                description = "Purple tubular blooms that fade to white as they age, so one shrub can carry several colours at once." },
            new FlowerSpecies { id = "fl_moon", name = "Moonflower", latin = "Ipomoea alba", shape = BloomShape.Moonflower,
                color = new Color(0.97f, 0.98f, 0.95f), accent = new Color(0.85f, 1f, 0.8f), nectar = 3f, refillSeconds = 45f, nightBloom = true, glows = true,
                description = "Large white blooms that open at dusk and close by morning. Most active at night." },
            new FlowerSpecies { id = "fl_spirit", name = "Spirit Orchid", latin = "Legend of this forest", shape = BloomShape.Orchid,
                color = new Color(0.8f, 0.95f, 1f), accent = new Color(0.4f, 1f, 0.9f), nectar = 8f, refillSeconds = 90f, glows = true,
                description = "A softly glowing orchid found only in the hidden grotto of the Night Forest. Some say it blooms only for hummingbirds." },
        };

        public static FlowerSpecies Get(string id) { foreach (var s in All) if (s.id == id) return s; return All[0]; }
    }

    /// <summary>One interactive flower plant. Bobs gently, reacts to the bird, holds nectar that refills.</summary>
    public class Flower : MonoBehaviour
    {
        public FlowerSpecies species;
        public float nectar;
        public Vector3 Mouth { get; private set; }
        public Vector3 Approach { get; private set; }
        public bool Visible { get; set; } = true;
        Quaternion baseRot;
        float phase, react, openAmount = 1f;
        Vector3 baseScale;
        public MeshRenderer Renderer { get; private set; }

        public void Init(FlowerSpecies s, float phase)
        {
            species = s;
            nectar = s.nectar;
            this.phase = phase;
            baseRot = transform.rotation;
            baseScale = transform.localScale;
            Renderer = GetComponent<MeshRenderer>();
            RecalcPoints();
        }

        void RecalcPoints()
        {
            Mouth = transform.TransformPoint(species.mouthLocal);
            Approach = transform.TransformDirection(species.approachLocal).normalized;
        }

        public float Fullness => species.nectar > 0 ? nectar / species.nectar : 0f;

        public float Drink(float amount)
        {
            float got = Mathf.Min(amount, nectar);
            nectar -= got;
            react = 1f;
            return got;
        }

        public void Tick(float dt, float bird, float night, float windStrength)
        {
            float rich = species.nightBloom ? Mathf.Lerp(0.4f, 1.6f, night) : Mathf.Lerp(1.2f, 0.5f, night);
            if (nectar < species.nectar) nectar = Mathf.Min(species.nectar, nectar + species.nectar / species.refillSeconds * dt * rich);
            float targetOpen = species.nightBloom ? Mathf.Lerp(0.55f, 1f, night) : Mathf.Lerp(1f, 0.85f, night);
            openAmount = Mathf.MoveTowards(openAmount, targetOpen, dt * 0.05f);
            react = Mathf.MoveTowards(react, bird, dt * 2f);
            float t = Time.time + phase * 10f;
            float sway = Mathf.Sin(t * 1.3f) * 2.5f * windStrength + Mathf.Sin(t * 3.1f) * 0.8f;
            float tremble = Mathf.Sin(t * 22f) * 3f * react;
            transform.rotation = baseRot * Quaternion.Euler(sway + tremble, 0, sway * 0.6f - tremble * 0.5f);
            transform.localScale = baseScale * openAmount;
            RecalcPoints();
        }
    }

    /// <summary>Spawns interactive flowers, manages nectar, culling and nearest-flower queries.</summary>
    public class FlowerManager : MonoBehaviour
    {
        public static FlowerManager I { get; private set; }
        public const int FlowerLayer = 8;
        public readonly List<Flower> Flowers = new List<Flower>();
        readonly Dictionary<long, List<Flower>> grid = new Dictionary<long, List<Flower>>();
        int cullIndex;
        public ParticleSystem Sparkle { get; private set; }
        public ParticleSystem Reward { get; private set; }

        void Awake() { I = this; }

        public void BuildAll()
        {
            foreach (var s in FlowerSpecies.All) BuildSpeciesMesh(s);
            Sparkle = Fx.NectarSparkle(transform);
            Reward = Fx.RewardBurst(transform);

            var r = new Rng(4242);
            // Flower Valley: hundreds of interactive flowers
            Scatter(r, TerrainField.RegionCenters[1], 125f, 340, new[] { "fl_hamelia", "fl_heliconia", "fl_shrimp", "fl_ginger", "fl_purple", "fl_passion" },
                    new[] { 0.24f, 0.2f, 0.2f, 0.16f, 0.15f, 0.05f });
            // river banks and forest clearings
            Scatter(r, new Vector2(-20, 20), 300f, 170, new[] { "fl_hamelia", "fl_heliconia", "fl_purple", "fl_ginger", "fl_passion" },
                    new[] { 0.3f, 0.3f, 0.15f, 0.15f, 0.1f }, nearWater: true);
            // waterfall paradise
            Scatter(r, TerrainField.RegionCenters[2], 80f, 50, new[] { "fl_heliconia", "fl_passion", "fl_ginger" }, new[] { 0.5f, 0.2f, 0.3f });
            // golden sunset knoll
            Scatter(r, TerrainField.RegionCenters[4], 60f, 45, new[] { "fl_shrimp", "fl_hamelia", "fl_passion" }, new[] { 0.45f, 0.4f, 0.15f });
            // night forest moonflowers
            Scatter(r, TerrainField.RegionCenters[5], 110f, 70, new[] { "fl_moon", "fl_purple" }, new[] { 0.75f, 0.25f });
            // secret grotto
            for (int i = 0; i < 6; i++)
            {
                Vector2 o = r.InCircle * 8f;
                Spawn(FlowerSpecies.Get("fl_spirit"), TerrainField.SecretGrotto + o, r);
            }
            // flight school glade ring
            var glade = WorldGenerator.Glade;
            string[] school = { "fl_hamelia", "fl_heliconia", "fl_shrimp", "fl_ginger", "fl_purple", "fl_hamelia" };
            for (int i = 0; i < school.Length; i++)
            {
                float a = i / (float)school.Length * Mathf.PI * 2f + 0.3f;
                Spawn(FlowerSpecies.Get(school[i]), glade + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 4.5f, r, faceCenter: glade);
            }
            // epiphyte flowers high in the canopy
            var wg = WorldGenerator.I;
            int high = 0;
            for (int i = 0; i < wg.Perches.Count && high < 60; i += 37)
            {
                var p = wg.Perches[i];
                if (!p.high || p.pos.y - TerrainField.Height(p.pos.x, p.pos.z) < 15f) continue;
                SpawnAt(FlowerSpecies.Get(r.Chance(0.5f) ? "fl_passion" : "fl_heliconia"), p.pos + Vector3.down * 0.05f, r, 0.6f);
                high++;
            }
        }

        void Scatter(Rng r, Vector2 center, float radius, int count, string[] ids, float[] weights, bool nearWater = false)
        {
            int made = 0;
            for (int attempt = 0; attempt < count * 8 && made < count; attempt++)
            {
                Vector2 p = center + r.InCircle * radius;
                float wd = TerrainField.WaterDistance(p.x, p.y, out _);
                if (wd < 1.5f) continue;
                if (nearWater && wd > 18f) continue;
                if (1f - TerrainField.Normal(p.x, p.y).y > 0.35f) continue;
                if (Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) > 350f) continue;
                float pick = r.Value, acc = 0; int k = 0;
                for (; k < weights.Length - 1; k++) { acc += weights[k]; if (pick < acc) break; }
                // flowers grow in little patches
                int patch = r.Range(1, 4);
                for (int j = 0; j < patch && made < count; j++)
                {
                    Spawn(FlowerSpecies.Get(ids[k]), p + r.InCircle * 1.5f, r);
                    made++;
                }
            }
        }

        Flower Spawn(FlowerSpecies s, Vector2 p, Rng r, Vector2? faceCenter = null)
        {
            Vector3 g = TerrainField.Ground(p.x, p.y);
            var f = SpawnAt(s, g, r, r.Range(0.85f, 1.25f));
            if (faceCenter.HasValue)
            {
                Vector3 to = new Vector3(faceCenter.Value.x - p.x, 0, faceCenter.Value.y - p.y);
                f.transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
                f.Init(s, r.Value);
            }
            return f;
        }

        Flower SpawnAt(FlowerSpecies s, Vector3 pos, Rng r, float scale)
        {
            var go = new GameObject(s.name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, r.Range(0f, 360f), 0);
            go.transform.localScale = Vector3.one * scale;
            go.layer = FlowerLayer;
            go.AddComponent<MeshFilter>().sharedMesh = s.mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = s.mats;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.center = s.mouthLocal;
            col.radius = 0.18f;
            var f = go.AddComponent<Flower>();
            f.Init(s, r.Value);
            Flowers.Add(f);
            long key = Key(pos);
            if (!grid.TryGetValue(key, out var list)) grid[key] = list = new List<Flower>();
            list.Add(f);
            return f;
        }

        static long Key(Vector3 p) => ((long)Mathf.FloorToInt(p.x / 8f) << 32) ^ (uint)Mathf.FloorToInt(p.z / 8f);

        public Flower Nearest(Vector3 from, float maxDist, bool needNectar = true)
        {
            Flower best = null;
            float bd = maxDist * maxDist;
            int rad = Mathf.CeilToInt(maxDist / 8f);
            int gx = Mathf.FloorToInt(from.x / 8f), gz = Mathf.FloorToInt(from.z / 8f);
            for (int dz = -rad; dz <= rad; dz++)
                for (int dx = -rad; dx <= rad; dx++)
                {
                    long key = ((long)(gx + dx) << 32) ^ (uint)(gz + dz);
                    if (!grid.TryGetValue(key, out var list)) continue;
                    foreach (var f in list)
                    {
                        if (needNectar && f.nectar < 0.2f) continue;
                        float d = (f.Mouth - from).sqrMagnitude;
                        if (d < bd) { bd = d; best = f; }
                    }
                }
            return best;
        }

        void Update()
        {
            if (Flowers.Count == 0) return;
            var cam = Camera.main;
            var bird = BirdController.I;
            float detail = QualityManager.I ? QualityManager.I.DetailDistance * 1.25f : 140f;
            float night = EnvironmentSystem.I ? EnvironmentSystem.I.Night : 0f;
            float wind = EnvironmentSystem.I ? EnvironmentSystem.I.WindStrength : 0.5f;
            Vector3 cp = cam ? cam.transform.position : Vector3.zero;
            Vector3 bp = bird ? bird.transform.position : Vector3.one * 9999f;
            float dt = Time.deltaTime;

            // culling round-robin
            int n = Mathf.Min(120, Flowers.Count);
            for (int i = 0; i < n; i++)
            {
                cullIndex = (cullIndex + 1) % Flowers.Count;
                var f = Flowers[cullIndex];
                bool vis = (f.transform.position - cp).sqrMagnitude < detail * detail;
                if (f.Renderer.enabled != vis) f.Renderer.enabled = vis;
            }
            // animate visible flowers near the camera; refill nectar for all (cheap)
            foreach (var f in Flowers)
            {
                if (!f.Renderer.enabled)
                {
                    if (f.nectar < f.species.nectar) f.nectar = Mathf.Min(f.species.nectar, f.nectar + f.species.nectar / f.species.refillSeconds * dt);
                    continue;
                }
                float bd = (f.Mouth - bp).magnitude;
                float near = Mathf.Clamp01(1f - bd / 0.8f);
                f.Tick(dt, near, night, wind);
            }
        }

        // ------------------------------------------------------------------ flower meshes
        void BuildSpeciesMesh(FlowerSpecies s)
        {
            var solid = new MeshBuilder();
            var leaf = new MeshBuilder();
            var petal = new MeshBuilder();
            var r = new Rng(s.id.GetHashCode());
            float H = 0.9f;
            var pts = new List<Vector3>(); var rad = new List<float>();
            Color stemCol = new Color(0.32f, 0.55f, 0.22f, 0f);
            Color c = s.color; c.a = 0f;
            Color a2 = s.accent; a2.a = 0f;

            void Stem(Vector3 from, Vector3 to, float r0)
            {
                pts.Clear(); rad.Clear();
                pts.Add(from); rad.Add(r0);
                pts.Add(Vector3.Lerp(from, to, 0.5f) + new Vector3(0.02f, 0, 0.01f)); rad.Add(r0 * 0.8f);
                pts.Add(to); rad.Add(r0 * 0.6f);
                solid.Col = stemCol;
                solid.Tube(pts, rad, 5, 2f, 0, 0, false);
            }

            void Leaves(int count, float size, float hMin, float hMax)
            {
                leaf.Col = new Color(0.85f, 1f, 0.8f, 0f);
                for (int i = 0; i < count; i++)
                {
                    float a = i * 2.39996f;
                    float h = Mathf.Lerp(hMin, hMax, i / (float)Mathf.Max(1, count - 1));
                    Vector3 dir = new Vector3(Mathf.Cos(a), 0.45f, Mathf.Sin(a)).normalized;
                    Vector3 up = dir * size;
                    Vector3 right = Vector3.Cross(dir, Vector3.up).normalized * size * 0.6f;
                    leaf.Card(new Vector3(0, h, 0), right, up, PlantFactory.UvBanana, -size * 0.25f, 0f, 0.5f);
                }
            }

            switch (s.shape)
            {
                case BloomShape.TubeCluster:
                {
                    H = 0.9f;
                    Stem(Vector3.zero, new Vector3(0, H, 0), 0.012f);
                    Leaves(6, 0.22f, 0.25f, H * 0.85f);
                    Vector3 top = new Vector3(0, H, 0);
                    for (int i = 0; i < 9; i++)
                    {
                        float a = i * 2.39996f;
                        Vector3 dir = new Vector3(Mathf.Cos(a) * 0.8f, 0.6f - (i % 3) * 0.25f, Mathf.Sin(a) * 0.8f).normalized;
                        if (i == 0) dir = new Vector3(0, 0.15f, 1f).normalized;
                        Vector3 b = top + dir * 0.01f;
                        float len = 0.055f;
                        solid.Col = Color.Lerp(c, a2, i / 9f * 0.5f);
                        solid.Loft(t => b + dir * (t * len), t => { float rr = Mathf.Lerp(0.0035f, 0.0065f, t * t); return new Vector2(rr, rr); }, 4, 6);
                    }
                    s.mouthLocal = top + new Vector3(0, 0.15f, 1f).normalized * 0.068f;
                    s.approachLocal = new Vector3(0, 0.1f, 1f);
                    break;
                }
                case BloomShape.Heliconia:
                {
                    H = 1.2f;
                    Stem(Vector3.zero, new Vector3(0, H, 0), 0.018f);
                    Leaves(4, 0.55f, 0.3f, 0.9f);
                    // pendant zigzag inflorescence
                    Vector3 hang = new Vector3(0, H, 0);
                    pts.Clear(); rad.Clear();
                    for (int i = 0; i <= 6; i++) { pts.Add(hang + new Vector3(0, -i * 0.055f, 0.02f * (i % 2))); rad.Add(0.006f); }
                    solid.Col = new Color(0.6f, 0.15f, 0.1f, 0);
                    solid.Tube(pts, rad, 4, 2f, 0, 0, false);
                    for (int i = 0; i < 7; i++)
                    {
                        float side = (i % 2 == 0) ? 1f : -1f;
                        Vector3 b = hang + new Vector3(0, -i * 0.055f - 0.01f, 0);
                        Vector3 dir = new Vector3(side * 0.9f, 0.45f, 0.15f).normalized;
                        float len = Mathf.Lerp(0.09f, 0.05f, i / 7f);
                        Color bc = c;
                        solid.Loft(t => b + dir * (t * len), t =>
                        {
                            float w = Mathf.Sin(Mathf.Min(1f, t * 1.1f) * Mathf.PI) * 0.018f + 0.002f;
                            return new Vector2(w * 0.55f, w);
                        }, 6, 6, (t, ang) => Color.Lerp(bc, a2, Mathf.Clamp01((t - 0.6f) * 3f)));
                    }
                    s.mouthLocal = hang + new Vector3(0.05f, -0.03f, 0.03f);
                    s.approachLocal = new Vector3(0.7f, 0f, 0.7f);
                    break;
                }
                case BloomShape.ShrimpSpike:
                case BloomShape.GingerSpike:
                {
                    bool ginger = s.shape == BloomShape.GingerSpike;
                    H = ginger ? 1.0f : 0.75f;
                    Stem(Vector3.zero, new Vector3(0, H, 0), 0.012f);
                    Leaves(ginger ? 5 : 6, ginger ? 0.3f : 0.22f, 0.2f, H * 0.8f);
                    Vector3 b0 = new Vector3(0, H, 0);
                    int rows = ginger ? 9 : 8;
                    for (int i = 0; i < rows; i++)
                    {
                        for (int k = 0; k < 4; k++)
                        {
                            float a = k * Mathf.PI * 0.5f + i * 0.8f;
                            Vector3 dir = new Vector3(Mathf.Cos(a), 0.9f, Mathf.Sin(a)).normalized;
                            Vector3 p = b0 + new Vector3(0, i * 0.018f, 0) + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.008f;
                            float sz = (ginger ? 0.012f : 0.022f) * (1f - i / (float)rows * 0.5f);
                            solid.Col = c;
                            if (ginger) solid.Sphere(p + dir * sz, sz, 5);
                            else solid.Loft(t => p + dir * (t * sz * 1.4f), t => { float w = Mathf.Sin(t * Mathf.PI) * sz * 0.6f + 0.001f; return new Vector2(w, w * 0.4f); }, 4, 5);
                        }
                    }
                    // white (or pale) tubular flowers poking out
                    for (int k = 0; k < 3; k++)
                    {
                        float a = k * 2.1f;
                        Vector3 p = b0 + new Vector3(0, 0.05f + k * 0.03f, 0);
                        Vector3 dir = new Vector3(Mathf.Cos(a), 0.3f, Mathf.Sin(a)).normalized;
                        if (k == 0) dir = new Vector3(0, 0.3f, 1f).normalized;
                        solid.Col = a2;
                        solid.Loft(t => p + dir * (0.01f + t * 0.035f), t => { float rr = Mathf.Lerp(0.003f, 0.006f, t); return new Vector2(rr, rr); }, 3, 5);
                    }
                    s.mouthLocal = b0 + new Vector3(0, 0.05f, 0) + new Vector3(0, 0.3f, 1f).normalized * 0.05f;
                    s.approachLocal = new Vector3(0, 0.15f, 1f);
                    break;
                }
                case BloomShape.Passion:
                {
                    H = 1.1f;
                    Stem(Vector3.zero, new Vector3(0.05f, H, 0), 0.008f);
                    Leaves(5, 0.18f, 0.3f, H * 0.9f);
                    Vector3 cen = new Vector3(0.05f, H, 0.01f);
                    Vector3 face = new Vector3(0, 0.25f, 1f).normalized;
                    Quaternion q = Quaternion.LookRotation(face, Vector3.up);
                    for (int k = 0; k < 10; k++)
                    {
                        float a = k / 10f * Mathf.PI * 2f;
                        Vector3 dir = q * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.15f);
                        petal.Col = c; petal.Col.a = 0.3f;
                        Vector3 right = Vector3.Cross(dir, face).normalized * 0.035f;
                        petal.Card(cen, right, dir * 0.065f, PlantFactory.UvFull, 0.01f, 0.3f, 0.4f);
                    }
                    for (int k = 0; k < 28; k++)
                    {
                        float a = k / 28f * Mathf.PI * 2f;
                        Vector3 dir = q * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.35f);
                        solid.Col = Color.Lerp(a2, Color.white, (k % 3) * 0.3f);
                        pts.Clear(); rad.Clear();
                        pts.Add(cen + face * 0.004f); rad.Add(0.0012f);
                        pts.Add(cen + face * 0.006f + dir * 0.03f); rad.Add(0.0008f);
                        solid.Tube(pts, rad, 3, 1f, 0, 0, false);
                    }
                    solid.Col = new Color(0.9f, 0.9f, 0.6f, 0);
                    solid.Sphere(cen + face * 0.012f, 0.008f, 6);
                    s.mouthLocal = cen + face * 0.02f;
                    s.approachLocal = face;
                    break;
                }
                case BloomShape.Moonflower:
                {
                    H = 0.8f;
                    Stem(Vector3.zero, new Vector3(0, H, 0), 0.01f);
                    Leaves(5, 0.2f, 0.2f, H * 0.85f);
                    Vector3 b = new Vector3(0, H, 0);
                    Vector3 dir = new Vector3(0, 0.35f, 1f).normalized;
                    solid.Col = c;
                    solid.Loft(t => b + dir * (t * 0.09f), t =>
                    {
                        float rr = t < 0.7f ? Mathf.Lerp(0.006f, 0.012f, t / 0.7f) : Mathf.Lerp(0.012f, 0.06f, (t - 0.7f) / 0.3f);
                        return new Vector2(rr, rr);
                    }, 10, 12, (t, ang) => Color.Lerp(a2, c, t));
                    s.mouthLocal = b + dir * 0.085f;
                    s.approachLocal = dir;
                    break;
                }
                case BloomShape.Orchid:
                {
                    H = 0.55f;
                    Stem(Vector3.zero, new Vector3(0, H, 0.05f), 0.006f);
                    Leaves(3, 0.2f, 0.05f, 0.15f);
                    Vector3 cen = new Vector3(0, H, 0.05f);
                    Vector3 face = new Vector3(0, 0.1f, 1f).normalized;
                    Quaternion q = Quaternion.LookRotation(face, Vector3.up);
                    for (int k = 0; k < 5; k++)
                    {
                        float a = k / 5f * Mathf.PI * 2f + Mathf.PI * 0.5f;
                        Vector3 d = q * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0.2f);
                        petal.Col = c; petal.Col.a = 0.3f;
                        petal.Card(cen, Vector3.Cross(d, face).normalized * 0.05f, d * 0.07f, PlantFactory.UvFull, 0.01f, 0.3f, 0.5f);
                    }
                    solid.Col = a2;
                    solid.Loft(t => cen + face * (t * 0.03f) + Vector3.down * 0.01f, t => { float w = 0.012f * Mathf.Sin(t * Mathf.PI) + 0.002f; return new Vector2(w, w * 0.7f); }, 5, 7);
                    s.mouthLocal = cen + face * 0.03f;
                    s.approachLocal = face;
                    break;
                }
            }
            s.plantHeight = H;
            var used = new List<int>();
            s.mesh = MeshBuilder.Multi(s.id, new[] { solid, leaf, petal }, used);
            var mats = new List<Material>();
            foreach (int u in used)
            {
                if (u == 0)
                {
                    var m = new Material(Mats.FlowerSolid) { name = s.id };
                    m.enableInstancing = true;
                    if (s.glows) m.SetColor("_EmissionColor", new Color(0.05f, 0.12f, 0.12f));
                    mats.Add(m);
                }
                else mats.Add(u == 1 ? Mats.Leaf : Mats.Petal);
            }
            s.mats = mats.ToArray();
        }
    }
}
