using System.Collections.Generic;
using UnityEngine;

namespace HummingbirdX
{
    public struct Perch
    {
        public Vector3 pos;
        public Vector3 along;   // branch direction (bird faces across it)
        public bool high;
    }

    /// <summary>Output buckets for one world chunk.</summary>
    public class ChunkGeo
    {
        public MeshBuilder Bark = new MeshBuilder();
        public MeshBuilder Vine = new MeshBuilder();
        public MeshBuilder Leaf = new MeshBuilder();      // far group (tree crowns)
        public MeshBuilder Under = new MeshBuilder();     // near group (understory leaves)
        public MeshBuilder Fern = new MeshBuilder();
        public MeshBuilder Petal = new MeshBuilder();
        public MeshBuilder Rock = new MeshBuilder();
        public MeshBuilder Glow = new MeshBuilder();
        public List<Perch> Perches = new List<Perch>();
        public List<(Vector3 a, Vector3 b, float r)> Trunks = new List<(Vector3, Vector3, float)>();
    }

    /// <summary>Procedural rainforest plant models written straight into chunk mesh buffers.</summary>
    public static class PlantFactory
    {
        // atlas cells: x=u0, y=u1, z=v at leaf base, w=v at leaf tip
        public static readonly Vector4 UvBanana = new Vector4(0f, 0.5f, 1f, 0.5f);
        public static readonly Vector4 UvHeart = new Vector4(0.5f, 1f, 1f, 0.5f);
        public static readonly Vector4 UvMonstera = new Vector4(0f, 0.5f, 0.5f, 0f);
        public static readonly Vector4 UvPalm = new Vector4(0.5f, 1f, 0.5f, 0f);
        public static readonly Vector4 UvFull = new Vector4(0f, 1f, 0f, 1f);

        static readonly List<Vector3> pts = new List<Vector3>(32);
        static readonly List<float> rad = new List<float>(32);

        static Color Leafy(Rng r, float light = 1f)
        {
            float g = r.Range(0.8f, 1.15f) * light;
            return new Color(g * r.Range(0.85f, 1.05f), g, g * r.Range(0.8f, 1.0f), 1f);
        }

        // ------------------------------------------------------------------ giant emergent / canopy tree
        public static void GiantTree(ChunkGeo g, Vector3 basePos, float height, Rng r, float density, bool emergent)
        {
            var bark = g.Bark;
            bark.M = Matrix4x4.identity;
            bark.Col = new Color(r.Range(0.85f, 1.1f), r.Range(0.85f, 1.05f), r.Range(0.8f, 1f), 0f);
            bark.Phase = r.Value;
            float r0 = emergent ? r.Range(1.3f, 2.1f) : r.Range(0.6f, 1.1f);
            float trunkTop = height * (emergent ? 0.80f : 0.68f);

            // trunk
            pts.Clear(); rad.Clear();
            Vector3 lean = new Vector3(r.Range(-1f, 1f), 0, r.Range(-1f, 1f)) * height * 0.03f;
            int segs = 9;
            for (int i = 0; i <= segs; i++)
            {
                float t = i / (float)segs;
                Vector3 wob = new Vector3(Mathf.Sin(t * 5f + r0) * 0.4f, 0, Mathf.Cos(t * 4f + r0) * 0.4f) * (t * (1 - t)) * 3f;
                pts.Add(basePos + new Vector3(0, -1.5f + t * (trunkTop + 1.5f), 0) + lean * t * t + wob);
                float flare = 1f + 1.4f * Mathf.Pow(1f - Mathf.Clamp01(t * 6f), 2f);
                rad.Add(r0 * Mathf.Lerp(1f, 0.38f, t) * flare);
            }
            bark.Tube(pts, rad, emergent ? 12 : 9, 0.45f);
            g.Trunks.Add((basePos, basePos + Vector3.up * trunkTop + lean, r0 * 0.95f));
            Vector3 top = pts[pts.Count - 1];

            // buttress roots
            if (emergent || r.Chance(0.4f))
            {
                int roots = r.Range(4, 7);
                for (int k = 0; k < roots; k++)
                {
                    float a = (k + r.Range(-0.2f, 0.2f)) / roots * Mathf.PI * 2f;
                    Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    float reach = r0 * r.Range(2.2f, 3.6f);
                    float hgt = r0 * r.Range(2.5f, 4.5f);
                    for (int layer = 0; layer < 3; layer++)
                    {
                        pts.Clear(); rad.Clear();
                        for (int i = 0; i <= 6; i++)
                        {
                            float t = i / 6f;
                            float y = hgt * (1f - t) * (1f - t) * (1f - layer * 0.28f);
                            pts.Add(basePos + dir * (r0 * 0.6f + reach * t) + Vector3.up * (y - 0.6f));
                            rad.Add(Mathf.Lerp(0.42f, 0.12f, t) * r0 * (layer == 0 ? 1f : 0.8f));
                        }
                        bark.Tube(pts, rad, 6, 0.6f, 0, 0, false);
                    }
                }
            }

            // crown branches
            int branches = emergent ? r.Range(6, 9) : r.Range(4, 7);
            float crownR = height * (emergent ? 0.32f : 0.26f);
            for (int b = 0; b < branches; b++)
            {
                float a = (b + r.Range(-0.3f, 0.3f)) / branches * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), r.Range(0.35f, 0.75f), Mathf.Sin(a)).normalized;
                float startT = r.Range(0.82f, 1f);
                Vector3 start = Vector3.Lerp(basePos + Vector3.up * trunkTop * 0.75f, top, startT);
                float len = crownR * r.Range(0.8f, 1.15f);
                Branch(g, start, dir, len, r0 * 0.32f, 2, r, density, emergent);
            }
            // leaf cap on top
            LeafCluster(g.Leaf, top + Vector3.up * crownR * 0.25f, crownR * 0.55f, (int)(26 * density), r, 2.4f, UvHeart);

            // hanging lianas
            int lianas = emergent ? r.Range(2, 5) : r.Range(0, 3);
            for (int k = 0; k < lianas; k++)
            {
                float a = r.Range(0f, Mathf.PI * 2f);
                Vector3 from = top + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * crownR * r.Range(0.3f, 0.8f) - Vector3.up * r.Range(0f, 3f);
                float drop = Mathf.Min(from.y - basePos.y - 1f, r.Range(height * 0.35f, height * 0.85f));
                Liana(g, from, drop, r);
            }
        }

        static void Branch(ChunkGeo g, Vector3 start, Vector3 dir, float len, float r0, int depth, Rng r, float density, bool emergent)
        {
            pts.Clear(); rad.Clear();
            int segs = 5;
            Vector3 p = start;
            Vector3 d = dir;
            for (int i = 0; i <= segs; i++)
            {
                float t = i / (float)segs;
                pts.Add(p);
                rad.Add(Mathf.Lerp(r0, r0 * 0.3f, t));
                d = (d + new Vector3(r.Range(-0.15f, 0.15f), -0.06f, r.Range(-0.15f, 0.15f))).normalized;
                p += d * (len / segs);
            }
            var local = new List<Vector3>(pts);
            g.Bark.Tube(pts, rad, depth == 2 ? 7 : 5, 0.5f, 0f, 0.05f);
            Vector3 end = local[local.Count - 1];

            // perches along fairly horizontal stretches
            for (int i = 2; i < local.Count - 1; i += 2)
            {
                Vector3 along = (local[i + 1] - local[i - 1]).normalized;
                if (Mathf.Abs(along.y) < 0.55f)
                    g.Perches.Add(new Perch { pos = local[i] + Vector3.up * rad[i] * 0.9f, along = along, high = true });
            }

            if (depth > 1)
            {
                int kids = r.Range(2, 4);
                for (int k = 0; k < kids; k++)
                {
                    int idx = r.Range(2, local.Count);
                    Vector3 kd = (dir + new Vector3(r.Range(-0.9f, 0.9f), r.Range(0.1f, 0.6f), r.Range(-0.9f, 0.9f))).normalized;
                    Branch(g, local[idx - 1], kd, len * r.Range(0.4f, 0.6f), r0 * 0.5f, depth - 1, r, density, emergent);
                }
            }
            float clusterR = len * (depth > 1 ? 0.5f : 0.6f);
            int cards = (int)((depth > 1 ? 30 : 22) * density);
            Vector4 uv = emergent ? (r.Chance(0.5f) ? UvHeart : UvBanana) : (r.Chance(0.3f) ? UvMonstera : UvHeart);
            LeafCluster(g.Leaf, end, clusterR, cards, r, emergent ? 2.2f : 1.9f, uv);
        }

        public static void LeafCluster(MeshBuilder mb, Vector3 c, float radius, int cards, Rng r, float leafSize, Vector4 uv)
        {
            mb.M = Matrix4x4.identity;
            mb.Phase = r.Value;
            for (int i = 0; i < cards; i++)
            {
                Vector3 o = Random.onUnitSphere;
                o = new Vector3(o.x * r.Range(0.3f, 1f), Mathf.Abs(o.y) * 0.6f * r.Range(0.2f, 1f) - 0.15f, o.z * r.Range(0.3f, 1f));
                Vector3 p = c + o * radius;
                Vector3 outward = (p - c).normalized;
                if (outward.sqrMagnitude < 0.01f) outward = Vector3.up;
                Vector3 up = (outward + Vector3.down * r.Range(0.2f, 0.7f) + Random.insideUnitSphere * 0.4f).normalized * leafSize * r.Range(0.75f, 1.25f);
                Vector3 right = Vector3.Cross(up, Vector3.up + Random.insideUnitSphere * 0.5f).normalized * up.magnitude;
                if (right.sqrMagnitude < 0.01f) right = Vector3.right * leafSize;
                mb.Col = Leafy(r, Mathf.Lerp(0.75f, 1.05f, Mathf.Clamp01(o.y + 0.5f)));
                mb.Card(p, right, up, uv, -up.magnitude * r.Range(0.05f, 0.25f), 0.25f, 0.9f);
            }
        }

        public static void Liana(ChunkGeo g, Vector3 from, float drop, Rng r)
        {
            if (drop < 4f) return;
            var mb = g.Vine;
            mb.M = Matrix4x4.identity; mb.Col = Color.white; mb.Phase = r.Value;
            pts.Clear(); rad.Clear();
            int segs = Mathf.Clamp((int)(drop / 2.5f), 4, 20);
            Vector3 sway = new Vector3(r.Range(-1f, 1f), 0, r.Range(-1f, 1f)) * 1.5f;
            for (int i = 0; i <= segs; i++)
            {
                float t = i / (float)segs;
                pts.Add(from + Vector3.down * drop * t + sway * Mathf.Sin(t * Mathf.PI) + new Vector3(Mathf.Sin(t * 9f), 0, Mathf.Cos(t * 7f)) * 0.2f);
                rad.Add(Mathf.Lerp(0.09f, 0.05f, t));
            }
            mb.Tube(pts, rad, 5, 0.3f, 0f, 0.7f);
            // climbing leaves along the liana
            var leaf = g.Under;
            leaf.M = Matrix4x4.identity; leaf.Phase = mb.Phase;
            for (int i = 1; i < pts.Count; i++)
            {
                if (!r.Chance(0.55f)) continue;
                float t = i / (float)(pts.Count - 1);
                Vector3 up = (Random.onUnitSphere + Vector3.down * 0.6f).normalized * r.Range(0.35f, 0.6f);
                Vector3 right = Vector3.Cross(up, Vector3.up).normalized * up.magnitude;
                if (right.sqrMagnitude < 0.001f) continue;
                leaf.Col = Leafy(r, 0.95f);
                leaf.Card(pts[i], right, up, UvHeart, -0.1f, 0.3f + t * 0.4f, 0.8f);
            }
        }

        // ------------------------------------------------------------------ palm
        public static void Palm(ChunkGeo g, Vector3 basePos, float height, Rng r)
        {
            var bark = g.Bark;
            bark.M = Matrix4x4.identity; bark.Col = new Color(0.9f, 0.86f, 0.75f, 0f); bark.Phase = r.Value;
            pts.Clear(); rad.Clear();
            Vector3 bend = new Vector3(r.Range(-1f, 1f), 0, r.Range(-1f, 1f)).normalized * height * r.Range(0.05f, 0.18f);
            for (int i = 0; i <= 8; i++)
            {
                float t = i / 8f;
                pts.Add(basePos + Vector3.up * (t * height - 0.5f) + bend * t * t);
                rad.Add(Mathf.Lerp(0.32f, 0.2f, t) * (i == 0 ? 1.4f : 1f));
            }
            bark.Tube(pts, rad, 7, 1.2f, 0f, 0.08f);
            g.Trunks.Add((basePos, pts[pts.Count - 1], 0.3f));
            Vector3 crown = pts[pts.Count - 1];
            var leaf = g.Leaf;
            leaf.M = Matrix4x4.identity; leaf.Phase = bark.Phase;
            int fronds = r.Range(9, 14);
            for (int f = 0; f < fronds; f++)
            {
                float a = (f + r.Range(-0.2f, 0.2f)) / fronds * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), r.Range(0.1f, 0.7f), Mathf.Sin(a)).normalized;
                float len = r.Range(3.2f, 4.6f);
                Vector3 right = Vector3.Cross(dir, Vector3.up).normalized * len * 0.9f;
                leaf.Col = Leafy(r, 1.05f);
                // inner half rising, outer half drooping
                Vector3 mid = crown + dir * len * 0.5f;
                leaf.Card(crown, right, dir * len * 0.55f, new Vector4(UvPalm.x, UvPalm.y, UvPalm.z, 0.25f), 0.2f, 0.15f, 0.55f);
                Vector3 droop = (dir + Vector3.down * 0.9f).normalized * len * 0.55f;
                leaf.Card(mid + dir * len * 0.02f, right * 0.85f, droop, new Vector4(UvPalm.x, UvPalm.y, 0.25f, 0f), -0.3f, 0.55f, 1f);
            }
            g.Perches.Add(new Perch { pos = crown + Vector3.up * 0.2f, along = Vector3.right, high = true });
        }

        // ------------------------------------------------------------------ understory
        public static void BananaClump(ChunkGeo g, Vector3 basePos, Rng r, bool heliconia)
        {
            var leaf = g.Under;
            var stem = g.Vine;
            int stems = r.Range(2, 5);
            for (int s = 0; s < stems; s++)
            {
                Vector3 b = basePos + new Vector3(r.Range(-0.8f, 0.8f), 0, r.Range(-0.8f, 0.8f));
                float h = r.Range(1.6f, 3.6f);
                stem.M = Matrix4x4.identity; stem.Col = new Color(0.55f, 0.75f, 0.35f, 0f); stem.Phase = r.Value;
                pts.Clear(); rad.Clear();
                pts.Add(b - Vector3.up * 0.3f); rad.Add(0.12f);
                pts.Add(b + Vector3.up * h * 0.5f); rad.Add(0.1f);
                pts.Add(b + Vector3.up * h); rad.Add(0.07f);
                stem.Tube(pts, rad, 6, 1.5f, 0f, 0.3f);
                Vector3 top = b + Vector3.up * h;
                int leaves = r.Range(4, 8);
                leaf.M = Matrix4x4.identity; leaf.Phase = stem.Phase;
                for (int l = 0; l < leaves; l++)
                {
                    float a = r.Range(0f, Mathf.PI * 2f);
                    Vector3 dir = new Vector3(Mathf.Cos(a), r.Range(0.4f, 1.3f), Mathf.Sin(a)).normalized;
                    float len = r.Range(1.6f, 2.8f);
                    Vector3 right = Vector3.Cross(dir, Vector3.up).normalized * len * 0.55f;
                    leaf.Col = Leafy(r, 1.05f);
                    leaf.Card(top - Vector3.up * r.Range(0f, h * 0.3f), right, dir * len, UvBanana, -len * 0.35f, 0.2f, 1f);
                }
                if (heliconia && r.Chance(0.8f))
                    HeliconiaBract(g, top + Vector3.up * 0.1f, r);
            }
        }

        static void HeliconiaBract(ChunkGeo g, Vector3 p, Rng r)
        {
            var pet = g.Petal;
            pet.M = Matrix4x4.identity; pet.Phase = r.Value;
            Color c = r.Chance(0.6f) ? new Color(0.95f, 0.15f, 0.08f, 0) : new Color(1f, 0.75f, 0.1f, 0);
            Vector3 down = Vector3.down;
            float a = r.Range(0f, 6.28f);
            Vector3 side = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            for (int i = 0; i < 7; i++)
            {
                Vector3 at = p + down * (i * 0.12f);
                Vector3 dir = (side * ((i % 2 == 0) ? 1 : -1) + Vector3.up * 0.5f).normalized * 0.28f;
                Vector3 right = Vector3.Cross(dir, side).normalized * 0.12f;
                pet.Col = c; pet.Col.a = 0.4f;
                pet.Card(at, right, dir, UvFull, 0.05f, 0.4f, 0.6f);
            }
        }

        public static void Monstera(ChunkGeo g, Vector3 basePos, Rng r)
        {
            var leaf = g.Under;
            var stem = g.Vine;
            int n = r.Range(5, 10);
            leaf.M = Matrix4x4.identity; leaf.Phase = r.Value;
            for (int i = 0; i < n; i++)
            {
                float a = r.Range(0f, Mathf.PI * 2f);
                Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                float h = r.Range(0.5f, 1.3f);
                Vector3 tip = basePos + dir * r.Range(0.3f, 0.8f) + Vector3.up * h;
                stem.M = Matrix4x4.identity; stem.Col = new Color(0.4f, 0.6f, 0.25f, 0); stem.Phase = leaf.Phase;
                pts.Clear(); rad.Clear();
                pts.Add(basePos); rad.Add(0.03f);
                pts.Add(Vector3.Lerp(basePos, tip, 0.5f) + Vector3.up * 0.2f); rad.Add(0.025f);
                pts.Add(tip); rad.Add(0.02f);
                stem.Tube(pts, rad, 4, 2f, 0f, 0.4f, false);
                float size = r.Range(0.9f, 1.5f);
                Vector3 up = (dir + Vector3.down * r.Range(0.2f, 0.7f)).normalized * size;
                Vector3 right = Vector3.Cross(up, Vector3.up).normalized * size;
                leaf.Col = Leafy(r, 0.9f);
                leaf.Card(tip, right, up, r.Chance(0.6f) ? UvMonstera : UvHeart, -size * 0.2f, 0.35f, 0.9f);
            }
        }

        public static void Fern(ChunkGeo g, Vector3 basePos, Rng r, float scale = 1f)
        {
            var mb = g.Fern;
            mb.M = Matrix4x4.identity; mb.Phase = r.Value;
            int n = r.Range(7, 13);
            for (int i = 0; i < n; i++)
            {
                float a = (i + r.Range(-0.3f, 0.3f)) / n * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), r.Range(0.6f, 1.4f), Mathf.Sin(a)).normalized;
                float len = r.Range(0.9f, 1.8f) * scale;
                Vector3 right = Vector3.Cross(dir, Vector3.up).normalized * len * 0.5f;
                mb.Col = Leafy(r, 1.0f);
                mb.Card(basePos, right, dir * len, UvFull, -len * 0.45f, 0.1f, 1f);
            }
        }

        public static void Rock(ChunkGeo g, Vector3 p, float size, Rng r)
        {
            var mb = g.Rock;
            mb.M = Matrix4x4.TRS(p, Quaternion.Euler(r.Range(-10f, 10f), r.Range(0f, 360f), r.Range(-10f, 10f)),
                                 new Vector3(size * r.Range(0.8f, 1.4f), size * r.Range(0.5f, 0.9f), size * r.Range(0.8f, 1.3f)));
            mb.Col = new Color(1, 1, 1, 0); mb.Phase = 0;
            int start = mb.Count;
            mb.Sphere(Vector3.zero, 1f, 8);
            // lumpy displacement
            for (int i = start; i < mb.Count; i++)
            {
                Vector3 w = mb.V[i];
                float n = Mathf.PerlinNoise(w.x * 0.7f + 3f, w.z * 0.7f + w.y * 0.3f) - 0.5f;
                mb.V[i] = w + mb.N[i] * n * size * 0.35f;
            }
            mb.M = Matrix4x4.identity;
        }

        public static void GlowMushrooms(ChunkGeo g, Vector3 p, Rng r)
        {
            var mb = g.Glow;
            int n = r.Range(3, 9);
            for (int i = 0; i < n; i++)
            {
                Vector3 b = p + new Vector3(r.Range(-0.6f, 0.6f), 0, r.Range(-0.6f, 0.6f));
                float h = r.Range(0.08f, 0.3f), cap = r.Range(0.05f, 0.14f);
                Color c = r.Chance(0.5f) ? new Color(0.3f, 1f, 0.85f, 0) : new Color(0.55f, 0.6f, 1f, 0);
                mb.M = Matrix4x4.identity; mb.Col = c; mb.Phase = 0;
                pts.Clear(); rad.Clear();
                pts.Add(b - Vector3.up * 0.05f); rad.Add(0.015f);
                pts.Add(b + Vector3.up * h); rad.Add(0.012f);
                mb.Tube(pts, rad, 4, 1f, 0, 0, false);
                Vector3 top = b + Vector3.up * h;
                mb.Loft(t => top + Vector3.up * (t * cap * 0.6f),
                        t => { float rr = Mathf.Lerp(cap, 0.005f, t * t); return new Vector2(rr, rr); }, 3, 8);
            }
        }

        /// <summary>Small decorative flowers (Flower Valley carpets). Non-interactive.</summary>
        public static void FlowerTuft(ChunkGeo g, Vector3 p, Rng r, Color petal)
        {
            var pet = g.Petal;
            var leaf = g.Under;
            int heads = r.Range(3, 8);
            for (int h = 0; h < heads; h++)
            {
                Vector3 b = p + new Vector3(r.Range(-0.5f, 0.5f), 0, r.Range(-0.5f, 0.5f));
                float hgt = r.Range(0.25f, 0.7f);
                leaf.M = Matrix4x4.identity; leaf.Phase = r.Value;
                leaf.Col = Leafy(r, 1.1f);
                Vector3 sd = new Vector3(r.Range(-1f, 1f), 0, r.Range(-1f, 1f)).normalized * 0.12f;
                leaf.Card(b, sd, Vector3.up * hgt, UvBanana, 0.05f, 0f, 0.6f);
                pet.M = Matrix4x4.identity; pet.Phase = leaf.Phase;
                Vector3 c = b + Vector3.up * hgt;
                int petals = 5;
                float ps = r.Range(0.06f, 0.11f);
                float rot = r.Range(0f, 6.28f);
                Color pc = petal * r.Range(0.85f, 1.1f); pc.a = 0.6f;
                pet.Col = pc;
                for (int k = 0; k < petals; k++)
                {
                    float a = rot + k / (float)petals * Mathf.PI * 2f;
                    Vector3 dir = new Vector3(Mathf.Cos(a), 0.35f, Mathf.Sin(a)).normalized * ps;
                    Vector3 right = Vector3.Cross(dir, Vector3.up).normalized * ps * 0.8f;
                    pet.Card(c, right, dir, UvFull, 0.02f, 0.5f, 0.7f);
                }
            }
        }
    }
}
