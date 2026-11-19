using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HummingbirdX
{
    /// <summary>
    /// Accumulates geometry (with an optional transform) and produces a Mesh.
    /// Used for every procedural model: trees, plants, flowers, birds, insects, terrain.
    /// </summary>
    public class MeshBuilder
    {
        public readonly List<Vector3> V = new List<Vector3>(1024);
        public readonly List<Vector3> N = new List<Vector3>(1024);
        public readonly List<Vector2> UV = new List<Vector2>(1024);
        public readonly List<Vector2> UV2 = new List<Vector2>(1024);
        public readonly List<Color> C = new List<Color>(1024);
        public readonly List<int> T = new List<int>(2048);

        public Matrix4x4 M = Matrix4x4.identity;
        public Color Col = Color.white;
        public float Phase;

        public int Count => V.Count;

        public void Clear()
        {
            V.Clear(); N.Clear(); UV.Clear(); UV2.Clear(); C.Clear(); T.Clear();
            M = Matrix4x4.identity; Col = Color.white; Phase = 0;
        }

        public int Vert(Vector3 p, Vector3 n, Vector2 uv)
        {
            V.Add(M.MultiplyPoint3x4(p));
            N.Add(M.MultiplyVector(n).normalized);
            UV.Add(uv);
            UV2.Add(new Vector2(Phase, 0));
            C.Add(Col);
            return V.Count - 1;
        }

        public int Vert(Vector3 p, Vector3 n, Vector2 uv, Color c)
        {
            var old = Col; Col = c;
            int i = Vert(p, n, uv);
            Col = old;
            return i;
        }

        public void Tri(int a, int b, int c) { T.Add(a); T.Add(b); T.Add(c); }
        public void Quad(int a, int b, int c, int d) { T.Add(a); T.Add(b); T.Add(c); T.Add(a); T.Add(c); T.Add(d); }

        /// <summary>
        /// Double-sided card rendered two-sided by the foliage shader. 'origin' is the base (attachment) point,
        /// the card extends along 'up'. uv: x=u0, y=u1, z=v at base, w=v at tip.
        /// </summary>
        public void Card(Vector3 origin, Vector3 right, Vector3 up, Vector4 uv, float bend = 0f, float windBase = 0f, float windTip = 1f)
        {
            Vector3 n = Vector3.Cross(right, up).normalized;
            // 2x3 grid so the leaf can bend along its length
            int rows = 3;
            int start = V.Count;
            for (int r = 0; r <= rows; r++)
            {
                float t = r / (float)rows;
                Vector3 bendOff = n * (bend * t * t);
                Color c = Col; c.a = Mathf.Lerp(windBase, windTip, t);
                for (int s = 0; s <= 1; s++)
                {
                    Vector3 p = origin + right * (s - 0.5f) + up * t + bendOff;
                    Vector3 nn = (n - up.normalized * (bend * 2f * t) * 0.3f).normalized;
                    Vert(p, nn, new Vector2(Mathf.Lerp(uv.x, uv.y, s), Mathf.Lerp(uv.z, uv.w, t)), c);
                }
            }
            for (int r = 0; r < rows; r++)
            {
                int a = start + r * 2;
                Quad(a, a + 2, a + 3, a + 1);
            }
        }

        /// <summary>Tube along a polyline (trunks, branches, vines, stems, bird necks).</summary>
        public void Tube(IList<Vector3> pts, IList<float> radii, int sides, float vScale, float windBase = 0f, float windTip = 0f, bool capEnd = true)
        {
            int n = pts.Count;
            if (n < 2) return;
            int start = V.Count;
            Vector3 prevSide = Vector3.zero;
            float vAcc = 0;
            for (int i = 0; i < n; i++)
            {
                Vector3 dir = i == 0 ? pts[1] - pts[0] : (i == n - 1 ? pts[n - 1] - pts[n - 2] : pts[i + 1] - pts[i - 1]);
                dir.Normalize();
                Vector3 side = i == 0
                    ? Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.95f ? Vector3.right : Vector3.up).normalized
                    : Vector3.ProjectOnPlane(prevSide, dir).normalized;
                if (side.sqrMagnitude < 0.001f) side = Vector3.Cross(dir, Vector3.right).normalized;
                prevSide = side;
                Vector3 up2 = Vector3.Cross(dir, side);
                if (i > 0) vAcc += Vector3.Distance(pts[i], pts[i - 1]) / Mathf.Max(radii[i] * 6.2832f, 0.01f) * vScale;
                float t = i / (float)(n - 1);
                Color c = Col; c.a = Mathf.Lerp(windBase, windTip, t);
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    Vector3 off = side * Mathf.Cos(a) + up2 * Mathf.Sin(a);
                    Vert(pts[i] + off * radii[i], off, new Vector2(s / (float)sides, vAcc), c);
                }
            }
            for (int i = 0; i < n - 1; i++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int a = start + i * (sides + 1) + s;
                    int b = a + sides + 1;
                    Quad(a, b, b + 1, a + 1);
                }
            }
            if (capEnd && radii[n - 1] > 0.0001f)
            {
                Vector3 dir = (pts[n - 1] - pts[n - 2]).normalized;
                Color c = Col; c.a = windTip;
                int ci = Vert(pts[n - 1] + dir * radii[n - 1] * 0.3f, dir, new Vector2(0.5f, vAcc), c);
                int ring = start + (n - 1) * (sides + 1);
                for (int s = 0; s < sides; s++) Tri(ring + s, ci, ring + s + 1);
            }
        }

        /// <summary>Ellipsoid / lofted body from a profile: radius(t) along local Z, cross-section squash.</summary>
        public void Loft(System.Func<float, Vector3> center, System.Func<float, Vector2> radius, int rings, int sides,
                         System.Func<float, float, Color> color = null)
        {
            int start = V.Count;
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                Vector3 c = center(t);
                Vector3 c2 = center(Mathf.Min(1f, t + 0.01f)) - center(Mathf.Max(0f, t - 0.01f));
                Vector3 fwd = c2.sqrMagnitude > 1e-8f ? c2.normalized : Vector3.forward;
                Vector3 side = Vector3.Cross(Vector3.up, fwd);
                if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
                side.Normalize();
                Vector3 up = Vector3.Cross(fwd, side);
                Vector2 rad = radius(t);
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    Vector3 off = side * (ca * rad.x) + up * (sa * rad.y);
                    Vector3 nrm = (side * (ca / Mathf.Max(rad.x, 1e-4f)) + up * (sa / Mathf.Max(rad.y, 1e-4f))).normalized;
                    if (r == 0) nrm = Vector3.Lerp(nrm, -fwd, 0.7f).normalized;
                    if (r == rings) nrm = Vector3.Lerp(nrm, fwd, 0.7f).normalized;
                    Color col = color != null ? color(t, a) : Col;
                    Vert(c + off, nrm, new Vector2(s / (float)sides, t), col);
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sides; s++)
                {
                    int a = start + r * (sides + 1) + s;
                    int b = a + sides + 1;
                    Quad(a, a + 1, b + 1, b);
                }
        }

        public void Sphere(Vector3 c, float radius, int seg)
        {
            Loft(t => c + Vector3.forward * ((t - 0.5f) * 2f * radius),
                 t => { float r = Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((t - 0.5f) * 2f, 2f))) * radius; return new Vector2(r, r); },
                 seg, seg);
        }

        public void Append(MeshBuilder other)
        {
            int off = V.Count;
            for (int i = 0; i < other.V.Count; i++)
            {
                V.Add(M.MultiplyPoint3x4(other.V[i]));
                N.Add(M.MultiplyVector(other.N[i]).normalized);
            }
            UV.AddRange(other.UV); UV2.AddRange(other.UV2); C.AddRange(other.C);
            for (int i = 0; i < other.T.Count; i++) T.Add(other.T[i] + off);
        }

        public Mesh ToMesh(string name, bool tangents = true, bool keepReadable = false)
        {
            var m = new Mesh { name = name };
            if (V.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(V);
            m.SetNormals(N);
            m.SetUVs(0, UV);
            m.SetUVs(1, UV2);
            m.SetColors(C);
            m.SetTriangles(T, 0, true);
            if (tangents && V.Count > 0) m.RecalculateTangents();
            m.RecalculateBounds();
            m.UploadMeshData(!keepReadable);
            return m;
        }
    
        /// <summary>Merges several builders into one mesh with one submesh per non-empty builder.</summary>
        public static Mesh Multi(string name, IList<MeshBuilder> parts, List<int> usedParts, bool keepReadable = false)
        {
            var all = new MeshBuilder();
            var subs = new List<List<int>>();
            usedParts.Clear();
            for (int p = 0; p < parts.Count; p++)
            {
                var b = parts[p];
                if (b == null || b.T.Count == 0) continue;
                int off = all.V.Count;
                all.V.AddRange(b.V); all.N.AddRange(b.N); all.UV.AddRange(b.UV); all.UV2.AddRange(b.UV2); all.C.AddRange(b.C);
                var tri = new List<int>(b.T.Count);
                for (int i = 0; i < b.T.Count; i++) tri.Add(b.T[i] + off);
                subs.Add(tri);
                usedParts.Add(p);
            }
            var m = new Mesh { name = name };
            if (all.V.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(all.V); m.SetNormals(all.N); m.SetUVs(0, all.UV); m.SetUVs(1, all.UV2); m.SetColors(all.C);
            m.subMeshCount = subs.Count;
            for (int i = 0; i < subs.Count; i++) m.SetTriangles(subs[i], i, false);
            if (all.V.Count > 0) m.RecalculateTangents();
            m.RecalculateBounds();
            m.UploadMeshData(!keepReadable);
            return m;
        }
    }
}
