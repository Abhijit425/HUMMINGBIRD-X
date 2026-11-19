using UnityEngine;

namespace HummingbirdX
{
    public static class Noise
    {
        public static float Fbm(float x, float z, int oct, float lac = 2f, float gain = 0.5f, float seed = 0f)
        {
            float a = 1f, f = 1f, s = 0f, norm = 0f;
            for (int i = 0; i < oct; i++)
            {
                s += a * (Mathf.PerlinNoise(x * f + seed + i * 17.13f, z * f - seed + i * 31.7f) * 2f - 1f);
                norm += a; a *= gain; f *= lac;
            }
            return s / norm;
        }

        public static float Ridge(float x, float z, int oct, float seed = 0f)
        {
            float a = 1f, f = 1f, s = 0f, norm = 0f;
            for (int i = 0; i < oct; i++)
            {
                float n = 1f - Mathf.Abs(Mathf.PerlinNoise(x * f + seed + i * 9.1f, z * f + seed * 0.7f) * 2f - 1f);
                s += a * n * n; norm += a; a *= 0.5f; f *= 2f;
            }
            return s / norm;
        }

        public static float Smooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }
    }

    /// <summary>Small deterministic RNG (xorshift) so the forest is identical on every device.</summary>
    public class Rng
    {
        uint s;
        public Rng(int seed) { s = (uint)(seed * 747796405 + 2891336453u); if (s == 0) s = 1; }
        public uint NextU() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return s; }
        public float Value => (NextU() & 0xFFFFFF) / 16777216f;
        public float Range(float a, float b) => a + (b - a) * Value;
        public int Range(int a, int b) => a + (int)(Value * (b - a));
        public Vector2 InCircle => Random2();
        Vector2 Random2()
        {
            float a = Value * Mathf.PI * 2f, r = Mathf.Sqrt(Value);
            return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }
        public bool Chance(float p) => Value < p;
        public T Pick<T>(T[] arr) => arr[Range(0, arr.Length)];
    }
}
