using System.Collections.Generic;
using UnityEngine;

namespace HummingbirdX
{
    public enum Region { EmeraldForest, FlowerValley, WaterfallParadise, Canopy, GoldenSunset, NightForest }

    public struct WaterPoint
    {
        public Vector2 p; public float level; public float width;
        public WaterPoint(float x, float z, float level, float width) { p = new Vector2(x, z); this.level = level; this.width = width; }
    }

    public class Lake
    {
        public Vector2 c; public float r; public float level; public string name;
    }

    public class FallDef
    {
        public string id, name; public Vector3 top, bottom; public float width; public bool secret;
    }

    /// <summary>
    /// The analytic shape of the world: heights, rivers, lakes, waterfalls and regions.
    /// Everything else (meshes, plants, wildlife) samples this.
    /// </summary>
    public static class TerrainField
    {
        public const float Size = 800f;
        public const float Half = 400f;

        public static readonly string[] RegionNames =
            { "The Emerald Forest", "Flower Valley", "Waterfall Paradise", "The Rainforest Canopy", "The Golden Sunset", "The Mystical Night Forest" };
        public static readonly string[] RegionIds = { "emerald", "flowers", "waterfall", "canopy", "sunset", "night" };
        public static readonly Vector2[] RegionCenters =
        {
            new Vector2(-80, -40), new Vector2(175, -190), new Vector2(15, 120), new Vector2(-235, 235),
            new Vector2(-320, -110), new Vector2(270, 80)
        };
        /// Nectar needed to unlock fast travel to each region (free flight is never blocked).
        public static readonly int[] RegionUnlockNectar = { 0, 0, 40, 100, 180, 260 };

        public static readonly List<WaterPoint[]> Rivers = new List<WaterPoint[]>
        {
            new[] { new WaterPoint(40, 385, 45.0f, 7), new WaterPoint(30, 320, 44.2f, 8), new WaterPoint(22, 250, 43.6f, 9), new WaterPoint(14, 166, 43.0f, 10) },
            new[] { new WaterPoint(12, 118, 3.0f, 11), new WaterPoint(-28, 62, 2.7f, 12), new WaterPoint(-12, -18, 2.3f, 12),
                    new WaterPoint(58, -98, 1.9f, 13), new WaterPoint(138, -170, 1.6f, 14), new WaterPoint(168, -228, 1.4f, 14) },
        };

        public static readonly List<Lake> Lakes = new List<Lake>
        {
            new Lake { c = new Vector2(12, 132), r = 26f, level = 3.0f, name = "Plunge Pool" },
            new Lake { c = new Vector2(178, -255), r = 52f, level = 1.4f, name = "Valley Lake" },
            new Lake { c = new Vector2(-170, 212), r = 10f, level = 6.5f, name = "Hidden Pool" },
            new Lake { c = new Vector2(-236, 38), r = 9f, level = 0f, name = "Ridge Pool" },
        };

        public static readonly List<FallDef> Falls = new List<FallDef>();
        public static Vector2 SecretGrotto = new Vector2(305, 135);

        static bool initialised;

        public static void Init()
        {
            if (initialised) return;
            initialised = true;
            // ridge pool sits at whatever the ridge foot height is
            var rp = Lakes[3];
            rp.level = BaseHeight(rp.c.x, rp.c.y) - 1.2f;

            Falls.Clear();
            Falls.Add(new FallDef { id = "fall_main", name = "Great Emerald Falls", top = new Vector3(14, 43.0f, 163), bottom = new Vector3(13, 3.0f, 146), width = 15f });
            float hpTop = BaseHeight(-136, 212);
            Falls.Add(new FallDef { id = "fall_hidden", name = "Hidden Cascade", secret = true, top = new Vector3(-136, hpTop - 0.6f, 212), bottom = new Vector3(-162, Lakes[2].level, 212), width = 5f });
            float rTop = BaseHeight(-285, 38);
            Falls.Add(new FallDef { id = "fall_ridge", name = "Ridge Veil Falls", secret = true, top = new Vector3(-285, rTop - 0.3f, 38), bottom = new Vector3(-241, rp.level, 38), width = 6f });
        }

        public static float BaseHeight(float x, float z)
        {
            float h = 6f + Noise.Fbm(x / 150f, z / 150f, 4, 2f, 0.5f, 3.1f) * 6f + Noise.Fbm(x / 38f, z / 38f, 3, 2f, 0.5f, 8.7f) * 1.3f;

            // northern plateau with the main cliff
            float edgeZ = 156f + Noise.Fbm(x / 70f, 0.5f, 2, 2, 0.5f, 1.3f) * 7f;
            float plateau = Noise.Smooth(edgeZ - 6f, edgeZ + 6f, z) * Noise.Smooth(-152f, -136f, x) * (1f - Noise.Smooth(225f, 285f, x));
            h += plateau * 38f;

            // western ridge (Golden Sunset)
            float ridge = Noise.Smooth(-225f, -330f, x);
            h += ridge * (12f + Noise.Ridge(x / 95f, z / 95f, 3, 4.2f) * 18f);

            // sunset overlook: flat-topped knoll
            float dS = Vector2.Distance(new Vector2(x, z), new Vector2(-320, -110));
            h = Mathf.Lerp(h, 34f + Noise.Fbm(x / 20f, z / 20f, 2) * 0.6f, 1f - Noise.Smooth(28f, 60f, dS));

            // flower valley: wide gentle meadow
            float dV = Vector2.Distance(new Vector2(x, z), RegionCenters[1]);
            float valley = 1f - Noise.Smooth(70f, 140f, dV);
            h = Mathf.Lerp(h, 3.6f + Noise.Fbm(x / 60f, z / 60f, 3) * 1.6f, valley * 0.85f);

            // map boundary mountains keep the bird inside
            float edge = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
            h += Noise.Smooth(330f, 398f, edge) * (40f + Noise.Ridge(x / 60f, z / 60f, 3, 7.7f) * 30f);
            return h;
        }

        public static float Height(float x, float z)
        {
            float h = BaseHeight(x, z);

            foreach (var river in Rivers)
            {
                float d = DistToRiver(river, x, z, out float level, out float width);
                if (d < width + 22f) h = CarveChannel(h, d, level, width);
            }
            foreach (var lake in Lakes)
            {
                float d = Vector2.Distance(new Vector2(x, z), lake.c);
                if (d < lake.r + 20f)
                {
                    float levee = lake.level + 0.7f;
                    float bank = Noise.Smooth(lake.r + 18f, lake.r, d);
                    h = Mathf.Lerp(h, Mathf.Max(h, levee), bank * Noise.Smooth(lake.r - 2f, lake.r + 4f, d));
                    if (d < lake.r + 2f)
                    {
                        float depth = 3.5f * (1f - Mathf.Pow(Mathf.Clamp01(d / (lake.r + 2f)), 2f));
                        float bed = lake.level - 0.4f - depth;
                        h = Mathf.Lerp(bed, h, Noise.Smooth(lake.r - 4f, lake.r + 2f, d));
                    }
                }
            }
            return h;
        }

        static float CarveChannel(float h, float d, float level, float width)
        {
            // raise the banks so water never floats above the ground, then cut the channel
            float leveeZone = 1f - Noise.Smooth(width * 0.5f, width + 20f, d);
            h = Mathf.Lerp(h, Mathf.Max(h, level + 0.8f), leveeZone);
            float bed = level - 0.3f - 2.2f * (1f - Mathf.Pow(Mathf.Clamp01(d / (width * 0.6f)), 2f));
            return Mathf.Lerp(bed, h, Noise.Smooth(width * 0.45f, width * 0.85f, d));
        }

        public static float DistToRiver(WaterPoint[] r, float x, float z, out float level, out float width)
        {
            Vector2 p = new Vector2(x, z);
            float best = float.MaxValue; level = 0; width = 8;
            for (int i = 0; i < r.Length - 1; i++)
            {
                Vector2 a = r[i].p, b = r[i + 1].p;
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                float d = Vector2.Distance(p, a + ab * t);
                // gentle meander
                d += Mathf.Sin((a + ab * t).magnitude * 0.05f + i) * 2.0f * (1f - Mathf.Abs(t - 0.5f) * 2f);
                if (d < best)
                {
                    best = d;
                    level = Mathf.Lerp(r[i].level, r[i + 1].level, t);
                    width = Mathf.Lerp(r[i].width, r[i + 1].width, t);
                }
            }
            return best;
        }

        /// <summary>Distance to the nearest water edge (rivers or lakes). Negative = in water.</summary>
        public static float WaterDistance(float x, float z, out float level)
        {
            float best = 999f; level = -100f;
            foreach (var river in Rivers)
            {
                float d = DistToRiver(river, x, z, out float l, out float w) - w * 0.5f;
                if (d < best) { best = d; level = l; }
            }
            foreach (var lake in Lakes)
            {
                float d = Vector2.Distance(new Vector2(x, z), lake.c) - lake.r;
                if (d < best) { best = d; level = lake.level; }
            }
            return best;
        }

        public static Vector3 Normal(float x, float z)
        {
            const float e = 1.0f;
            float hl = Height(x - e, z), hr = Height(x + e, z), hd = Height(x, z - e), hu = Height(x, z + e);
            return new Vector3(hl - hr, 2f * e, hd - hu).normalized;
        }

        public static Region RegionAt(float x, float z)
        {
            int best = 0; float bd = float.MaxValue;
            for (int i = 0; i < RegionCenters.Length; i++)
            {
                float w = i == 0 ? 1.35f : 1f; // emerald forest is the large central default
                float d = Vector2.Distance(new Vector2(x, z), RegionCenters[i]) / w;
                if (d < bd) { bd = d; best = i; }
            }
            return (Region)best;
        }

        /// <summary>0..1 how strongly a point belongs to its region (1 at the centre).</summary>
        public static float RegionWeight(Region r, float x, float z)
        {
            float d = Vector2.Distance(new Vector2(x, z), RegionCenters[(int)r]);
            return 1f - Noise.Smooth(60f, 170f, d);
        }

        public static Vector3 Ground(float x, float z) => new Vector3(x, Height(x, z), z);
    }
}
