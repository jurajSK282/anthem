using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Placeholder low-poly meshes (irregular Gothic-style trees, foliage cards, glow bulbs).
// Trees: submesh 0 = bark, submesh 1 = leaf cards. Replace with Blender/Tripo models later.
public static class EmberMeshes
{
    class Buf
    {
        public List<Vector3> v = new List<Vector3>();
        public List<Vector2> uv = new List<Vector2>();
        public List<int> bark = new List<int>(), leaf = new List<int>();
    }

    static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    // Bent tapered tube along a wandering spine. Returns the tip position.
    static Vector3 Tube(Buf b, System.Random r, Vector3 start, Vector3 dir, float len, float r0, float r1,
        int segs, int sides, float wander, float flare, List<Vector3> spine = null)
    {
        Vector3 p = start;
        int baseIdx = b.v.Count;
        for (int s = 0; s <= segs; s++)
        {
            float t = s / (float)segs;
            float rad = Mathf.Lerp(r0, r1, t) * (1f + flare * Mathf.Pow(1f - t, 6f));
            Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);
            for (int i = 0; i <= sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                float k = rad * (i == sides ? 1f : R(r, 0.8f, 1.2f));
                if (i == sides) { b.v.Add(b.v[b.v.Count - sides] ); }
                else b.v.Add(p + q * new Vector3(Mathf.Cos(a) * k, 0f, Mathf.Sin(a) * k));
                b.uv.Add(new Vector2(i / (float)sides * 2f, t * len * 0.25f));
            }
            spine?.Add(p);
            dir = (dir + new Vector3(R(r, -1, 1), R(r, -0.3f, 0.6f), R(r, -1, 1)) * wander).normalized;
            p += dir * (len / segs);
        }
        int ring = sides + 1;
        for (int s = 0; s < segs; s++)
            for (int i = 0; i < sides; i++)
            {
                int a = baseIdx + s * ring + i, c = a + ring;
                b.bark.AddRange(new[] { a, c, a + 1, a + 1, c, c + 1 });
            }
        return p;
    }

    static void Card(Buf b, System.Random r, Vector3 c, float size)
    {
        Quaternion q = Quaternion.Euler(R(r, -50, 50), R(r, 0, 360), R(r, -50, 50));
        Vector3 x = q * Vector3.right * size, y = q * Vector3.up * size * R(r, 0.6f, 1f);
        int i = b.v.Count;
        b.v.AddRange(new[] { c - x - y, c + x - y, c - x + y, c + x + y });
        b.uv.AddRange(new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
        b.leaf.AddRange(new[] { i, i + 2, i + 1, i + 1, i + 2, i + 3 });
    }

    static Mesh Finish(Buf b, string name)
    {
        var m = new Mesh { name = name };
        m.SetVertices(b.v); m.SetUVs(0, b.uv);
        m.subMeshCount = b.leaf.Count > 0 && b.bark.Count > 0 ? 2 : 1;
        if (m.subMeshCount == 2) { m.SetTriangles(b.bark, 0); m.SetTriangles(b.leaf, 1); }
        else m.SetTriangles(b.bark.Count > 0 ? b.bark : b.leaf, 0);
        m.RecalculateNormals(); m.RecalculateBounds(); m.RecalculateTangents();
        string path = $"{EmberTextures.Dir}/{name}.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    public static Mesh Tree(int seed, float height, float radius, float leafiness)
    {
        var r = new System.Random(seed);
        var b = new Buf();
        var spine = new List<Vector3>();
        Vector3 lean = new Vector3(R(r, -0.12f, 0.12f), 1f, R(r, -0.12f, 0.12f)).normalized;
        Tube(b, r, Vector3.down * 1.5f, lean, height + 1.5f, radius, radius * 0.25f, 8, 6, 0.09f, 0.7f, spine);
        int branches = r.Next(4, 8);
        for (int i = 0; i < branches; i++)
        {
            int at = r.Next(3, spine.Count - 1);
            float ang = R(r, 0f, Mathf.PI * 2f);
            Vector3 dir = new Vector3(Mathf.Cos(ang), R(r, 0.15f, 0.8f), Mathf.Sin(ang)).normalized;
            float len = height * R(r, 0.25f, 0.5f);
            float br = radius * Mathf.Lerp(0.45f, 0.2f, at / (float)spine.Count);
            var bs = new List<Vector3>();
            Vector3 tip = Tube(b, r, spine[at], dir, len, br, 0.03f, 4, 4, 0.3f, 0f, bs);
            // twig fork
            Tube(b, r, bs[2], (dir + new Vector3(R(r, -1, 1), R(r, 0, 1), R(r, -1, 1))).normalized, len * 0.5f, br * 0.4f, 0.02f, 3, 3, 0.35f, 0f);
            int cards = Mathf.RoundToInt(R(r, 2f, 5f) * leafiness);
            for (int c = 0; c < cards; c++)
                Card(b, r, Vector3.Lerp(bs[2], tip, R(r, 0.2f, 1.1f)) + Random3(r) * 1.5f, R(r, 1.8f, 3.6f));
        }
        return Finish(b, $"EmberTree_{seed}");
    }

    static Vector3 Random3(System.Random r) => new Vector3(R(r, -1, 1), R(r, -0.6f, 0.8f), R(r, -1, 1));

    // 3 crossed leaf cards standing on the ground (bush / fern).
    public static Mesh Bush()
    {
        var b = new Buf();
        for (int i = 0; i < 3; i++)
        {
            Quaternion q = Quaternion.Euler(0f, i * 60f + 10f, 0f);
            Vector3 x = q * Vector3.right * 0.9f, lean = q * Vector3.forward * 0.25f;
            int k = b.v.Count;
            b.v.AddRange(new[] { -x, x, -x + Vector3.up * 1.5f + lean, x + Vector3.up * 1.5f + lean });
            b.uv.AddRange(new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
            b.leaf.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 });
        }
        return Finish(b, "EmberBush");
    }

    // Faceted glowing bulb (mushroom cap / spore pod), origin at ground.
    public static Mesh Bulb()
    {
        var r = new System.Random(3);
        var b = new Buf();
        Tube(b, r, Vector3.zero, Vector3.up, 0.5f, 0.02f, 0.22f, 2, 6, 0f, 0f);
        Tube(b, r, Vector3.up * 0.5f, Vector3.up, 0.3f, 0.22f, 0.02f, 2, 6, 0f, 0f);
        return Finish(b, "EmberBulb");
    }
}
