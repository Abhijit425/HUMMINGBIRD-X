using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HummingbirdX
{
    /// <summary>A reusable plant/tree model drawn with GPU instancing.</summary>
    public class Prototype
    {
        public string name;
        public Mesh[] lod = new Mesh[2];
        public Material[][] mats = new Material[2][];
        public bool near;                 // understory: only drawn inside the detail distance
        public bool castShadows = true;
        public float lod1Distance = 120f;
        public List<Perch> perches = new List<Perch>();
        public List<(Vector3 a, Vector3 b, float r)> trunks = new List<(Vector3, Vector3, float)>();
        public readonly List<Matrix4x4>[] visible = { new List<Matrix4x4>(256), new List<Matrix4x4>(256) };
    }

    public struct Inst
    {
        public int proto;
        public Matrix4x4 m;
        public int hash;
    }

    public class Chunk
    {
        public int cx, cz;
        public Bounds bounds;
        public MeshRenderer terrain;
        public List<Inst> inst = new List<Inst>();
        public List<GameObject> extras = new List<GameObject>();
        public bool visible, near; public int lod;
    }

    /// <summary>
    /// Builds the whole rainforest at startup (deterministic seed): terrain chunks with colliders,
    /// rivers, lakes, waterfalls, ~45 plant prototypes and tens of thousands of instances,
    /// perches for birds, light shafts and the map texture. Draws vegetation with RenderMeshInstanced.
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        public static WorldGenerator I { get; private set; }

        public const int Seed = 20260;
        public const float ChunkSize = 50f;
        public const int ChunksPerSide = 16;
        const int TerrainRes = 25; // quads per chunk side (2 m)

        public float Progress { get; private set; }
        public string ProgressLabel { get; private set; } = "";
        public bool Ready { get; private set; }
        public Texture2D MapTexture { get; private set; }
        public Vector3 SpawnPoint { get; private set; }
        public Vector3 SpawnPerch { get; private set; }

        public readonly List<Perch> Perches = new List<Perch>();
        readonly Dictionary<long, List<int>> perchGrid = new Dictionary<long, List<int>>();
        public readonly List<Prototype> Protos = new List<Prototype>();
        readonly Chunk[,] chunks = new Chunk[ChunksPerSide, ChunksPerSide];
        public readonly List<Vector3> WaterSoundPoints = new List<Vector3>();
        public readonly List<Vector3> InsectPoints = new List<Vector3>();

        Transform root, colliderRoot;
        readonly Plane[] planes = new Plane[6];
        int lastVisibleHash = -1;
        int protoBanana, protoHeli, protoMonstera, protoFern, protoRock, protoGlow, protoTwig, protoSapling;
        int[] protoEmergent, protoCanopy, protoPalm, protoTuft;
        static readonly Color[] TuftColors =
        {
            new Color(0.95f, 0.18f, 0.12f), new Color(0.62f, 0.25f, 0.85f), new Color(1f, 0.82f, 0.15f),
            new Color(0.25f, 0.45f, 1f), new Color(1f, 0.55f, 0.75f)
        };

        public static Vector2 Glade = new Vector2(-62, 28);

        void Awake() { I = this; }

        public IEnumerator Generate()
        {
            Ready = false;
            TerrainField.Init();
            Mats.Build();
            UnityEngine.Random.InitState(Seed);
            root = new GameObject("World").transform;
            colliderRoot = new GameObject("Colliders").transform;
            colliderRoot.SetParent(root);

            Step("Growing plants", 0.02f);
            yield return null;
            BuildPrototypes();

            Step("Shaping the land", 0.15f);
            yield return null;
            for (int cz = 0; cz < ChunksPerSide; cz++)
            {
                for (int cx = 0; cx < ChunksPerSide; cx++)
                    BuildTerrainChunk(cx, cz);
                Progress = 0.15f + 0.35f * (cz + 1) / ChunksPerSide;
                yield return null;
            }

            Step("Filling rivers and waterfalls", 0.5f);
            yield return null;
            BuildWater();

            Step("Planting the rainforest", 0.58f);
            for (int cz = 0; cz < ChunksPerSide; cz++)
            {
                for (int cx = 0; cx < ChunksPerSide; cx++)
                    PlaceVegetation(chunks[cx, cz]);
                Progress = 0.58f + 0.3f * (cz + 1) / ChunksPerSide;
                yield return null;
            }
            PlaceSpecialAreas();
            BuildLightShafts();

            Step("Drawing the map", 0.9f);
            yield return null;
            BuildMap();

            Progress = 1f;
            Ready = true;
        }

        void Step(string label, float p) { ProgressLabel = label; Progress = p; }

        // ------------------------------------------------------------------ prototypes
        static readonly Material[] PartMats = new Material[8];

        void BuildPrototypes()
        {
            PartMats[0] = Mats.Bark; PartMats[1] = Mats.Vine; PartMats[2] = Mats.Leaf; PartMats[3] = Mats.Leaf;
            PartMats[4] = Mats.Fern; PartMats[5] = Mats.Petal; PartMats[6] = Mats.Rock; PartMats[7] = Mats.GlowFungus;

            protoEmergent = new int[5];
            for (int i = 0; i < protoEmergent.Length; i++)
            {
                int s = 100 + i;
                protoEmergent[i] = AddProto("Emergent" + i, false, 140f,
                    g => PlantFactory.GiantTree(g, Vector3.zero, new Rng(s).Range(46f, 62f), new Rng(s * 7), 0.7f, true),
                    g => PlantFactory.GiantTree(g, Vector3.zero, new Rng(s).Range(46f, 62f), new Rng(s * 7), 0.28f, true));
            }
            protoCanopy = new int[6];
            for (int i = 0; i < protoCanopy.Length; i++)
            {
                int s = 200 + i;
                protoCanopy[i] = AddProto("Canopy" + i, false, 110f,
                    g => PlantFactory.GiantTree(g, Vector3.zero, new Rng(s).Range(22f, 34f), new Rng(s * 7), 0.8f, false),
                    g => PlantFactory.GiantTree(g, Vector3.zero, new Rng(s).Range(22f, 34f), new Rng(s * 7), 0.3f, false));
            }
            protoPalm = new int[4];
            for (int i = 0; i < protoPalm.Length; i++)
            {
                int s = 300 + i;
                protoPalm[i] = AddProto("Palm" + i, false, 100f, g => PlantFactory.Palm(g, Vector3.zero, new Rng(s).Range(9f, 16f), new Rng(s * 3)), null);
            }
            protoSapling = AddProto("Sapling", false, 90f, g => PlantFactory.GiantTree(g, Vector3.zero, 9f, new Rng(77), 0.5f, false), null);
            protoBanana = AddProto("Banana", true, 999f, g => PlantFactory.BananaClump(g, Vector3.zero, new Rng(401), false), null);
            protoHeli = AddProto("Heliconia", true, 999f, g => PlantFactory.BananaClump(g, Vector3.zero, new Rng(402), true), null);
            protoMonstera = AddProto("Monstera", true, 999f, g => PlantFactory.Monstera(g, Vector3.zero, new Rng(403)), null);
            protoFern = AddProto("Fern", true, 999f, g => PlantFactory.Fern(g, Vector3.zero, new Rng(404), 1f), null);
            protoRock = AddProto("Rock", false, 999f, g => PlantFactory.Rock(g, Vector3.zero, 1f, new Rng(405)), null);
            protoGlow = AddProto("GlowMushrooms", true, 999f, g => PlantFactory.GlowMushrooms(g, Vector3.zero, new Rng(406)), null);
            Protos[protoGlow].castShadows = false;
            protoTwig = AddProto("Twig", true, 999f, g => Twig(g, new Rng(407)), null);
            protoTuft = new int[TuftColors.Length];
            for (int i = 0; i < TuftColors.Length; i++)
            {
                int k = i;
                protoTuft[i] = AddProto("Tuft" + i, true, 999f, g => PlantFactory.FlowerTuft(g, Vector3.zero, new Rng(500 + k), TuftColors[k]), null);
                Protos[protoTuft[i]].castShadows = false;
            }
        }

        int AddProto(string name, bool near, float lod1Dist, Action<ChunkGeo> build0, Action<ChunkGeo> build1)
        {
            var p = new Prototype { name = name, near = near, lod1Distance = lod1Dist };
            var used = new List<int>();
            var g = new ChunkGeo();
            build0(g);
            p.lod[0] = MeshBuilder.Multi(name, Parts(g), used);
            p.mats[0] = MatsFor(used);
            p.perches.AddRange(g.Perches);
            p.trunks.AddRange(g.Trunks);
            if (build1 != null)
            {
                var g1 = new ChunkGeo();
                build1(g1);
                p.lod[1] = MeshBuilder.Multi(name + "_LOD1", Parts(g1), used);
                p.mats[1] = MatsFor(used);
            }
            else { p.lod[1] = p.lod[0]; p.mats[1] = p.mats[0]; }
            Protos.Add(p);
            return Protos.Count - 1;
        }

        static MeshBuilder[] Parts(ChunkGeo g) => new[] { g.Bark, g.Vine, g.Leaf, g.Under, g.Fern, g.Petal, g.Rock, g.Glow };

        static Material[] MatsFor(List<int> used)
        {
            var m = new Material[used.Count];
            for (int i = 0; i < used.Count; i++) m[i] = PartMats[used[i]];
            return m;
        }

        static void Twig(ChunkGeo g, Rng r)
        {
            var b = g.Bark;
            b.Col = new Color(0.8f, 0.75f, 0.65f, 0f);
            var pts = new List<Vector3> { new Vector3(0, -0.2f, 0), new Vector3(0.1f, 0.6f, 0.05f), new Vector3(0.15f, 1.3f, 0.1f) };
            var rad = new List<float> { 0.03f, 0.02f, 0.008f };
            b.Tube(pts, rad, 5, 2f, 0f, 0.15f);
            g.Perches.Add(new Perch { pos = new Vector3(0.14f, 1.22f, 0.09f), along = new Vector3(0.1f, 1f, 0.1f).normalized });
            for (int k = 0; k < 3; k++)
            {
                float a = k * 2.1f + 0.4f;
                Vector3 s0 = Vector3.Lerp(pts[1], pts[2], 0.2f + k * 0.25f);
                Vector3 dir = new Vector3(Mathf.Cos(a), 0.25f, Mathf.Sin(a)).normalized;
                var p2 = new List<Vector3> { s0, s0 + dir * 0.25f, s0 + dir * 0.45f + Vector3.up * 0.04f };
                var r2 = new List<float> { 0.008f, 0.005f, 0.003f };
                b.Tube(p2, r2, 4, 3f, 0.1f, 0.3f);
                g.Perches.Add(new Perch { pos = s0 + dir * 0.3f + Vector3.up * 0.008f, along = dir });
            }
        }

        // ------------------------------------------------------------------ terrain
        void BuildTerrainChunk(int cx, int cz)
        {
            var c = new Chunk { cx = cx, cz = cz };
            chunks[cx, cz] = c;
            float x0 = -TerrainField.Half + cx * ChunkSize, z0 = -TerrainField.Half + cz * ChunkSize;
            int n = TerrainRes + 1;
            float step = ChunkSize / TerrainRes;
            var hg = new float[n + 2, n + 2];
            for (int j = 0; j < n + 2; j++)
                for (int i = 0; i < n + 2; i++)
                    hg[i, j] = TerrainField.Height(x0 + (i - 1) * step, z0 + (j - 1) * step);

            var mb = new MeshBuilder();
            float minY = float.MaxValue, maxY = float.MinValue;
            Region reg = TerrainField.RegionAt(x0 + ChunkSize / 2, z0 + ChunkSize / 2);
            for (int j = 0; j < n; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    float x = x0 + i * step, z = z0 + j * step, y = hg[i + 1, j + 1];
                    var nrm = new Vector3(hg[i, j + 1] - hg[i + 2, j + 1], 2f * step, hg[i + 1, j] - hg[i + 1, j + 2]).normalized;
                    float wd = TerrainField.WaterDistance(x, z, out _);
                    float mud = 1f - Noise.Smooth(0f, 7f, wd);
                    Region r = TerrainField.RegionAt(x, z);
                    float moss = r == Region.FlowerValley ? 0.95f : r == Region.NightForest || r == Region.WaterfallParadise ? 0.55f
                               : r == Region.GoldenSunset ? 0.35f : 0.2f;
                    mb.Vert(new Vector3(x, y, z), nrm, new Vector2(x, z), new Color(mud, moss, 0, 1));
                    minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }
            }
            for (int j = 0; j < TerrainRes; j++)
                for (int i = 0; i < TerrainRes; i++)
                {
                    int a = j * n + i;
                    mb.Quad(a, a + n, a + n + 1, a + 1);
                }
            var mesh = mb.ToMesh($"Terrain_{cx}_{cz}", true, true);
            var go = new GameObject(mesh.name);
            go.transform.SetParent(root, false);
            go.isStatic = true;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Mats.Terrain;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            c.terrain = mr;
            c.bounds = new Bounds(new Vector3(x0 + ChunkSize / 2, (minY + maxY) / 2 + 20f, z0 + ChunkSize / 2),
                                  new Vector3(ChunkSize + 30f, (maxY - minY) + 110f, ChunkSize + 30f));
        }

        // ------------------------------------------------------------------ water
        void BuildWater()
        {
            var water = new GameObject("Water").transform;
            water.SetParent(root, false);
            int idx = 0;
            foreach (var river in TerrainField.Rivers)
            {
                var mb = new MeshBuilder();
                float vAcc = 0;
                int rows = 0;
                for (int i = 0; i < river.Length - 1; i++)
                {
                    var a = river[i]; var b = river[i + 1];
                    float len = Vector2.Distance(a.p, b.p);
                    int steps = Mathf.Max(2, (int)(len / 3f));
                    for (int s = 0; s < steps; s++)
                    {
                        float t = s / (float)steps;
                        Vector2 p = Vector2.Lerp(a.p, b.p, t);
                        float lvl = Mathf.Lerp(a.level, b.level, t);
                        float w = Mathf.Lerp(a.width, b.width, t) * 0.75f;
                        Vector2 dir = (b.p - a.p).normalized;
                        Vector2 side = new Vector2(-dir.y, dir.x);
                        for (int k = 0; k <= 2; k++)
                        {
                            float u = k / 2f;
                            Vector2 q = p + side * ((u - 0.5f) * 2f * w);
                            float edge = k == 1 ? 1f : 0.25f;
                            mb.Vert(new Vector3(q.x, lvl, q.y), Vector3.up, new Vector2(u * w * 0.2f, vAcc * 0.1f), new Color(0.6f, 0, 0, edge));
                        }
                        vAcc += len / steps;
                        rows++;
                        if (s % 13 == 0) WaterSoundPoints.Add(new Vector3(p.x, lvl, p.y));
                    }
                }
                for (int r = 0; r < rows - 1; r++)
                    for (int k = 0; k < 2; k++)
                    {
                        int a = r * 3 + k;
                        mb.Quad(a, a + 3, a + 4, a + 1);
                    }
                MakeObject("River" + idx++, mb.ToMesh("River"), Mats.Water, water, false);
            }

            foreach (var lake in TerrainField.Lakes)
            {
                var mb = new MeshBuilder();
                int seg = 40;
                int c = mb.Vert(new Vector3(lake.c.x, lake.level, lake.c.y), Vector3.up, Vector2.zero, new Color(0.1f, 0, 0, 1));
                for (int s = 0; s <= seg; s++)
                {
                    float a = s / (float)seg * Mathf.PI * 2f;
                    float rr = lake.r + 3f;
                    mb.Vert(new Vector3(lake.c.x + Mathf.Cos(a) * rr, lake.level, lake.c.y + Mathf.Sin(a) * rr), Vector3.up,
                        new Vector2(Mathf.Cos(a) * rr * 0.1f, Mathf.Sin(a) * rr * 0.1f), new Color(0.1f, 0, 0, 0.35f));
                }
                for (int s = 0; s < seg; s++) mb.Tri(c, c + 2 + s, c + 1 + s);
                // fix UV of centre
                MakeObject(lake.name, mb.ToMesh(lake.name), Mats.Water, water, false);
                WaterSoundPoints.Add(new Vector3(lake.c.x, lake.level, lake.c.y));
            }

            foreach (var f in TerrainField.Falls) BuildFall(f, water);
        }

        void BuildFall(FallDef f, Transform parent)
        {
            var mb = new MeshBuilder();
            int rows = 28;
            Vector3 flow = new Vector3(f.bottom.x - f.top.x, 0, f.bottom.z - f.top.z);
            Vector3 side = Vector3.Cross(Vector3.up, flow.normalized);
            float length = 0;
            Vector3 prev = f.top;
            for (int r = 0; r <= rows; r++)
            {
                float u = r / (float)rows;
                Vector3 p = new Vector3(Mathf.Lerp(f.top.x, f.bottom.x, u), 0, Mathf.Lerp(f.top.z, f.bottom.z, u));
                float y = Mathf.Lerp(f.top.y, f.bottom.y, u * u);
                float ground = TerrainField.Height(p.x, p.z) + 0.45f;
                p.y = Mathf.Max(y, ground);
                if (r == rows) p.y = f.bottom.y;
                length += Vector3.Distance(prev, p);
                prev = p;
                float w = f.width * Mathf.Lerp(1f, 1.25f, u);
                for (int k = 0; k <= 2; k++)
                {
                    float s = k / 2f;
                    Vector3 q = p + side * ((s - 0.5f) * w) + Vector3.up * Mathf.Sin(s * Mathf.PI) * 0.4f;
                    mb.Vert(q, -flow.normalized, new Vector2(s, u), new Color(1, 1, 1, 1f));
                }
            }
            for (int r = 0; r < rows; r++)
                for (int k = 0; k < 2; k++)
                {
                    int a = r * 3 + k;
                    mb.Quad(a, a + 3, a + 4, a + 1);
                }
            var mat = new Material(Mats.Waterfall);
            mat.SetFloat("_Stretch", Mathf.Max(0.5f, length / 18f));
            mat.SetFloat("_Speed", 1.0f + length / 60f);
            var go = MakeObject(f.id, mb.ToMesh(f.id), mat, parent, false);

            // mist + spray at the plunge point
            Fx.Mist(go.transform, f.bottom + Vector3.up * 0.5f, f.width);
            WaterSoundPoints.Add(f.bottom);
            WaterfallSpots.Add((f.bottom, f.width, f.secret, f.id, f.name));
        }

        public readonly List<(Vector3 pos, float width, bool secret, string id, string name)> WaterfallSpots =
            new List<(Vector3, float, bool, string, string)>();

        GameObject MakeObject(string name, Mesh mesh, Material mat, Transform parent, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        // ------------------------------------------------------------------ vegetation
        void PlaceVegetation(Chunk c)
        {
            var r = new Rng(Seed + c.cx * 131 + c.cz * 977);
            float x0 = -TerrainField.Half + c.cx * ChunkSize, z0 = -TerrainField.Half + c.cz * ChunkSize;
            Vector2 cc = new Vector2(x0 + ChunkSize / 2, z0 + ChunkSize / 2);
            float edge = Mathf.Max(Mathf.Abs(cc.x), Mathf.Abs(cc.y));
            Region reg = TerrainField.RegionAt(cc.x, cc.y);
            bool mountains = edge > 365f;

            int emergent = 0, canopy = 0, palms = 0, saplings = 0, banana = 0, heli = 0, monstera = 0, ferns = 0, rocks = 0, glow = 0, tufts = 0, twigs = 0;
            switch (reg)
            {
                case Region.EmeraldForest: emergent = r.Range(0, 3); canopy = r.Range(5, 8); palms = r.Range(0, 2); saplings = 3; banana = 4; heli = 3; monstera = 7; ferns = 26; rocks = 2; twigs = 2; break;
                case Region.Canopy: emergent = r.Range(3, 5); canopy = r.Range(4, 7); saplings = 2; banana = 2; heli = 1; monstera = 6; ferns = 22; rocks = 2; twigs = 1; break;
                case Region.FlowerValley: canopy = r.Range(0, 2); palms = r.Range(1, 3); saplings = 1; heli = 4; banana = 1; ferns = 6; tufts = 55; rocks = 1; twigs = 3; break;
                case Region.WaterfallParadise: emergent = r.Range(0, 2); canopy = r.Range(3, 6); palms = r.Range(1, 3); saplings = 2; banana = 4; heli = 4; monstera = 9; ferns = 36; rocks = 7; twigs = 2; break;
                case Region.GoldenSunset: emergent = r.Range(0, 2); canopy = r.Range(2, 4); palms = r.Range(1, 3); saplings = 2; banana = 1; heli = 2; monstera = 2; ferns = 12; tufts = 12; rocks = 4; twigs = 2; break;
                case Region.NightForest: emergent = r.Range(1, 3); canopy = r.Range(6, 9); saplings = 3; monstera = 8; ferns = 30; glow = 16; rocks = 3; twigs = 1; tufts = 4; break;
            }
            if (mountains) { emergent = 0; canopy = r.Range(3, 6); palms = 0; banana = heli = monstera = 0; ferns = 10; tufts = 0; rocks = 6; }

            Place(c, r, protoEmergent, emergent, 4f, 0.35f, 0.92f, 1.08f, 6f);
            Place(c, r, protoCanopy, canopy, 3f, 0.42f, 0.85f, 1.15f, 3.5f);
            Place(c, r, protoPalm, palms, 1.5f, 0.45f, 0.85f, 1.15f, 2f);
            Place(c, r, new[] { protoSapling }, saplings, 1f, 0.5f, 0.7f, 1.2f, 2f);
            Place(c, r, new[] { protoBanana }, banana, 0.5f, 0.55f, 0.8f, 1.2f, 1f);
            Place(c, r, new[] { protoHeli }, heli, 0.5f, 0.55f, 0.8f, 1.2f, 1f);
            Place(c, r, new[] { protoMonstera }, monstera, 0.3f, 0.6f, 0.8f, 1.3f, 0.5f);
            Place(c, r, new[] { protoFern }, ferns, 0f, 0.7f, 0.6f, 1.4f, 0f);
            Place(c, r, new[] { protoRock }, rocks, -1f, 1f, 0.6f, 2.8f, 0f, true);
            Place(c, r, new[] { protoGlow }, glow, 0.3f, 0.6f, 0.8f, 1.4f, 0f);
            Place(c, r, protoTuft, tufts, 0f, 0.5f, 0.8f, 1.3f, 0f);
            Place(c, r, new[] { protoTwig }, twigs, 0f, 0.5f, 0.8f, 1.3f, 0f);

            // insects / cicada sound points in forest chunks
            if (canopy + emergent >= 4 && r.Chance(0.35f))
                InsectPoints.Add(TerrainField.Ground(cc.x, cc.y) + Vector3.up * 12f);
        }

        void Place(Chunk c, Rng r, int[] protos, int count, float minWater, float maxSlope, float sMin, float sMax, float clearance, bool rocks = false)
        {
            float x0 = -TerrainField.Half + c.cx * ChunkSize, z0 = -TerrainField.Half + c.cz * ChunkSize;
            for (int i = 0; i < count; i++)
            {
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    float x = x0 + r.Range(0f, ChunkSize), z = z0 + r.Range(0f, ChunkSize);
                    if (Vector2.Distance(new Vector2(x, z), Glade) < 16f && clearance > 0.4f) continue;
                    if (Vector2.Distance(new Vector2(x, z), TerrainField.SecretGrotto) < 14f && clearance > 0.4f) continue;
                    float wd = TerrainField.WaterDistance(x, z, out _);
                    if (rocks) { if (wd > 14f && !r.Chance(0.3f)) continue; }
                    else if (wd < minWater) continue;
                    Vector3 n = TerrainField.Normal(x, z);
                    if (1f - n.y > maxSlope) continue;
                    if (FallClearance(x, z) < 8f && clearance > 0.4f) continue;
                    float y = TerrainField.Height(x, z);
                    float s = r.Range(sMin, sMax);
                    var rot = Quaternion.Euler(0, r.Range(0f, 360f), 0);
                    if (!rocks && protos.Length == 1 && protos[0] == protoFern) rot = Quaternion.FromToRotation(Vector3.up, Vector3.Lerp(Vector3.up, n, 0.5f)) * rot;
                    AddInstance(c, protos[r.Range(0, protos.Length)], new Vector3(x, y - (rocks ? s * 0.25f : 0.05f), z), rot, s);
                    break;
                }
            }
        }

        static float FallClearance(float x, float z)
        {
            float best = 999f;
            foreach (var f in TerrainField.Falls)
            {
                Vector2 a = new Vector2(f.top.x, f.top.z), b = new Vector2(f.bottom.x, f.bottom.z), p = new Vector2(x, z);
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.01f));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t) - f.width * 0.5f);
            }
            return best;
        }

        public void AddInstance(Chunk c, int proto, Vector3 pos, Quaternion rot, float scale)
        {
            var m = Matrix4x4.TRS(pos, rot, Vector3.one * scale);
            c.inst.Add(new Inst { proto = proto, m = m, hash = (int)((pos.x * 73.1f + pos.z * 19.7f) * 10f) & 0x7fffffff });
            var p = Protos[proto];
            foreach (var per in p.perches)
            {
                var wp = new Perch { pos = m.MultiplyPoint3x4(per.pos), along = m.MultiplyVector(per.along).normalized, high = per.high };
                AddPerch(wp);
            }
            foreach (var t in p.trunks)
            {
                Vector3 a = m.MultiplyPoint3x4(t.a), b = m.MultiplyPoint3x4(t.b);
                float rr = t.r * scale;
                var go = new GameObject("Trunk");
                go.transform.SetParent(colliderRoot, false);
                go.transform.position = (a + b) * 0.5f;
                go.transform.up = (b - a).normalized;
                var cap = go.AddComponent<CapsuleCollider>();
                cap.direction = 1;
                cap.radius = rr;
                cap.height = Vector3.Distance(a, b) + rr * 2f;
            }
            if (proto == protoRock)
            {
                var go = new GameObject("RockCol");
                go.transform.SetParent(colliderRoot, false);
                go.transform.position = pos;
                var sc = go.AddComponent<SphereCollider>();
                sc.radius = scale * 0.75f;
            }
        }

        void AddPerch(Perch p)
        {
            Perches.Add(p);
            long key = PerchKey(p.pos);
            if (!perchGrid.TryGetValue(key, out var list)) perchGrid[key] = list = new List<int>();
            list.Add(Perches.Count - 1);
        }

        static long PerchKey(Vector3 p) => ((long)Mathf.FloorToInt(p.x / 10f) << 32) ^ (uint)Mathf.FloorToInt(p.z / 10f);

        public bool NearestPerch(Vector3 from, float maxDist, out Perch best, Func<Perch, bool> filter = null)
        {
            best = default;
            float bd = maxDist * maxDist;
            bool found = false;
            int rad = Mathf.CeilToInt(maxDist / 10f);
            int gx = Mathf.FloorToInt(from.x / 10f), gz = Mathf.FloorToInt(from.z / 10f);
            for (int dz = -rad; dz <= rad; dz++)
                for (int dx = -rad; dx <= rad; dx++)
                {
                    long key = ((long)(gx + dx) << 32) ^ (uint)(gz + dz);
                    if (!perchGrid.TryGetValue(key, out var list)) continue;
                    foreach (int i in list)
                    {
                        var p = Perches[i];
                        float d = (p.pos - from).sqrMagnitude;
                        if (d < bd && (filter == null || filter(p))) { bd = d; best = p; found = true; }
                    }
                }
            return found;
        }

        public Perch RandomPerch(Rng r, Vector3 near, float radius)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 o = r.InCircle * radius;
                if (NearestPerch(near + new Vector3(o.x, 0, o.y), 15f, out var p)) return p;
            }
            return Perches.Count > 0 ? Perches[r.Range(0, Perches.Count)] : new Perch { pos = near };
        }

        Chunk ChunkAt(float x, float z)
        {
            int cx = Mathf.Clamp(Mathf.FloorToInt((x + TerrainField.Half) / ChunkSize), 0, ChunksPerSide - 1);
            int cz = Mathf.Clamp(Mathf.FloorToInt((z + TerrainField.Half) / ChunkSize), 0, ChunksPerSide - 1);
            return chunks[cx, cz];
        }

        void PlaceSpecialAreas()
        {
            // spawn glade: a twig perch in the middle, ferns and heliconias around the edge
            Vector3 g = TerrainField.Ground(Glade.x, Glade.y);
            var c = ChunkAt(g.x, g.z);
            var tw = Protos[protoTwig];
            var m = Matrix4x4.TRS(g, Quaternion.Euler(0, 30f, 0), Vector3.one * 1.1f);
            AddInstance(c, protoTwig, g, Quaternion.Euler(0, 30f, 0), 1.1f);
            SpawnPerch = m.MultiplyPoint3x4(tw.perches[0].pos);
            SpawnPoint = SpawnPerch;
            var r = new Rng(9);
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                float d = r.Range(9f, 15f);
                Vector3 p = TerrainField.Ground(Glade.x + Mathf.Cos(a) * d, Glade.y + Mathf.Sin(a) * d);
                int proto = i % 3 == 0 ? protoHeli : (i % 3 == 1 ? protoFern : protoMonstera);
                AddInstance(ChunkAt(p.x, p.z), proto, p, Quaternion.Euler(0, r.Range(0f, 360f), 0), r.Range(0.9f, 1.2f));
            }

            // secret grotto: ring of emergent giants, glowing floor
            var gr = TerrainField.SecretGrotto;
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f;
                Vector3 p = TerrainField.Ground(gr.x + Mathf.Cos(a) * 20f, gr.y + Mathf.Sin(a) * 20f);
                AddInstance(ChunkAt(p.x, p.z), protoEmergent[i % protoEmergent.Length], p, Quaternion.Euler(0, i * 51f, 0), 1.05f);
            }
            for (int i = 0; i < 14; i++)
            {
                Vector2 o = r.InCircle * 11f;
                Vector3 p = TerrainField.Ground(gr.x + o.x, gr.y + o.y);
                AddInstance(ChunkAt(p.x, p.z), i % 2 == 0 ? protoGlow : protoFern, p, Quaternion.Euler(0, r.Range(0f, 360f), 0), r.Range(0.9f, 1.4f));
            }
        }

        void BuildLightShafts()
        {
            var mb = new MeshBuilder();
            // two crossed quads, uv.y 0 top -> 1 bottom
            for (int q = 0; q < 2; q++)
            {
                Vector3 right = q == 0 ? Vector3.right : Vector3.forward;
                Vector3 n = Vector3.Cross(right, Vector3.up);
                int a = mb.Vert(-right * 2.5f + Vector3.up * 40f, n, new Vector2(0, 0));
                int b = mb.Vert(right * 2.5f + Vector3.up * 40f, n, new Vector2(1, 0));
                int c2 = mb.Vert(right * 4f, n, new Vector2(1, 1));
                int d = mb.Vert(-right * 4f, n, new Vector2(0, 1));
                mb.Quad(a, b, c2, d);
            }
            var mesh = mb.ToMesh("Shaft", false);
            var parent = new GameObject("LightShafts").transform;
            parent.SetParent(root, false);
            var r = new Rng(31);
            int made = 0;
            for (int i = 0; i < 400 && made < 70; i++)
            {
                float x = r.Range(-330f, 330f), z = r.Range(-330f, 330f);
                var reg = TerrainField.RegionAt(x, z);
                if (reg == Region.FlowerValley || reg == Region.NightForest) continue;
                if (TerrainField.WaterDistance(x, z, out _) < 4f) continue;
                Vector3 p = TerrainField.Ground(x, z) - Vector3.up * 0.5f;
                var go = MakeObject("Shaft", mesh, Mats.Shaft, parent, false);
                go.transform.position = p;
                go.transform.rotation = Quaternion.Euler(r.Range(-12f, 12f), r.Range(0f, 360f), r.Range(-12f, 12f));
                go.transform.localScale = new Vector3(r.Range(0.6f, 1.6f), r.Range(0.7f, 1.1f), r.Range(0.6f, 1.6f));
                ChunkAt(x, z).extras.Add(go);
                made++;
            }
        }

        // ------------------------------------------------------------------ map
        void BuildMap()
        {
            int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Map" };
            var px = new Color32[n * n];
            Color[] regionTint =
            {
                new Color(0.10f, 0.32f, 0.14f), new Color(0.38f, 0.42f, 0.16f), new Color(0.08f, 0.30f, 0.26f),
                new Color(0.06f, 0.24f, 0.10f), new Color(0.40f, 0.30f, 0.12f), new Color(0.10f, 0.14f, 0.26f)
            };
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = -TerrainField.Half + (i + 0.5f) / n * TerrainField.Size;
                    float z = -TerrainField.Half + (j + 0.5f) / n * TerrainField.Size;
                    float h = TerrainField.Height(x, z);
                    float hx = TerrainField.Height(x + 3f, z);
                    float shade = Mathf.Clamp(0.85f + (h - hx) * 0.12f, 0.55f, 1.25f);
                    Color col = regionTint[(int)TerrainField.RegionAt(x, z)] * shade * (0.8f + h / 120f);
                    float wd = TerrainField.WaterDistance(x, z, out float lvl);
                    if (wd < 0f && h < lvl) col = Color.Lerp(new Color(0.20f, 0.45f, 0.55f), new Color(0.10f, 0.28f, 0.40f), Mathf.Clamp01(-wd / 10f));
                    col.a = 1f;
                    px[j * n + i] = col;
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            MapTexture = tex;
        }

        // ------------------------------------------------------------------ runtime culling + instanced drawing
        public void OnQualityChanged() { lastVisibleHash = -1; }

        void LateUpdate()
        {
            if (!Ready) return;
            var cam = Camera.main;
            if (cam == null) return;
            var q = QualityManager.I;
            float draw = q ? q.DrawDistance : 260f;
            float detail = q ? q.DetailDistance : 110f;
            float density = q ? q.FoliageDensity : 1f;

            GeometryUtility.CalculateFrustumPlanes(cam, planes);
            Vector3 cp = cam.transform.position;
            int hash = 17;
            for (int cz = 0; cz < ChunksPerSide; cz++)
                for (int cx = 0; cx < ChunksPerSide; cx++)
                {
                    var c = chunks[cx, cz];
                    Vector3 cc = c.bounds.center;
                    float d = new Vector2(cc.x - cp.x, cc.z - cp.z).magnitude - ChunkSize * 0.7f;
                    bool vis = d < draw && GeometryUtility.TestPlanesAABB(planes, c.bounds);
                    bool near = vis && d < detail;
                    if (c.terrain.enabled != (d < draw + 40f)) c.terrain.enabled = d < draw + 40f;
                    c.visible = vis; c.near = near;
                    if (vis) hash = hash * 31 + (cx + cz * 64) * 3 + (near ? 1 : 0);
                    foreach (var e in c.extras) if (e.activeSelf != (vis && d < draw * 0.6f)) e.SetActive(vis && d < draw * 0.6f);
                }
            hash = hash * 31 + Mathf.RoundToInt(density * 100f) + Mathf.FloorToInt(cp.x / 25f) * 7919 + Mathf.FloorToInt(cp.z / 25f) * 104729;
            if (hash != lastVisibleHash)
            {
                lastVisibleHash = hash;
                RebuildVisible(cp, density);
            }
            Draw();
        }

        void RebuildVisible(Vector3 cp, float density)
        {
            foreach (var p in Protos) { p.visible[0].Clear(); p.visible[1].Clear(); }
            int keep = Mathf.RoundToInt(density * 100f);
            foreach (var c in chunks)
            {
                if (!c.visible) continue;
                foreach (var inst in c.inst)
                {
                    var p = Protos[inst.proto];
                    if (p.near && (!c.near || inst.hash % 100 >= keep)) continue;
                    Vector3 pos = inst.m.GetColumn(3);
                    float d = new Vector2(pos.x - cp.x, pos.z - cp.z).magnitude;
                    p.visible[d > p.lod1Distance ? 1 : 0].Add(inst.m);
                }
            }
        }

        void Draw()
        {
            var bounds = new Bounds(Vector3.zero, new Vector3(1000f, 600f, 1000f));
            foreach (var p in Protos)
            {
                for (int l = 0; l < 2; l++)
                {
                    var list = p.visible[l];
                    if (list.Count == 0) continue;
                    var mesh = p.lod[l];
                    var mats = p.mats[l];
                    for (int s = 0; s < mats.Length; s++)
                    {
                        var rp = new RenderParams(mats[s])
                        {
                            worldBounds = bounds,
                            shadowCastingMode = p.castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                            receiveShadows = true,
                            layer = 0,
                            lightProbeUsage = LightProbeUsage.BlendProbes,
                            reflectionProbeUsage = ReflectionProbeUsage.BlendProbes
                        };
                        for (int start = 0; start < list.Count; start += 1000)
                            Graphics.RenderMeshInstanced(rp, mesh, s, list, Mathf.Min(1000, list.Count - start), start);
                    }
                }
            }
        }
    }
}
