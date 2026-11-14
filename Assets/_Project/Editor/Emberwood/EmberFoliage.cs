using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Dense, patchy undergrowth the 2006 way: thousands of alpha-tested crossed cards merged into a few chunk meshes.
// Submesh 0 = dark undergrowth, 1 = brighter autumn clumps, 2/3 = ferns. Normals point up so cards shade like the ground.
public static class EmberFoliage
{
    const float Chunk = 45f;

    class Buf
    {
        public List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        public List<Vector2> uv = new List<Vector2>();
        public List<int>[] tris = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
    }

    static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    // 0..1 how overgrown a spot is: big noise patches, thick bands on the path banks, bare path.
    static float Density(float x, float z)
    {
        float d = EmberTerrain.PathDist(x, z);
        if (d < 3.2f) return 0f;
        float patch = Mathf.PerlinNoise(x * 0.045f + 12f, z * 0.045f + 40f);
        float fine = Mathf.PerlinNoise(x * 0.16f + 3f, z * 0.16f + 8f);
        float bank = Mathf.Clamp01(1f - Mathf.Abs(d - 6f) / 6f);
        return Mathf.Clamp01(EmberTerrain.Ss(0.3f, 0.75f, patch) * 0.8f + bank * 0.55f) * Mathf.Lerp(0.35f, 1f, fine);
    }

    static void Plant(Buf b, System.Random r, Vector3 p, float w, float h, int sub)
    {
        int cards = r.Next(2, 4);
        float yaw = R(r, 0f, 180f);
        Vector3 lean = new Vector3(R(r, -0.3f, 0.3f), 0f, R(r, -0.3f, 0.3f)) * h;
        for (int c = 0; c < cards; c++)
        {
            Vector3 x = Quaternion.Euler(0f, yaw + c * 180f / cards + R(r, -15f, 15f), 0f) * Vector3.right * w * 0.5f;
            Vector3 b0 = p + Vector3.down * 0.22f * h, top = Vector3.up * h + lean;
            int i = b.v.Count;
            b.v.AddRange(new[] { b0 - x, b0 + x, b0 - x + top, b0 + x + top });
            bool flip = r.Next(2) == 0;
            b.uv.AddRange(new[] { new Vector2(flip ? 1 : 0, 0), new Vector2(flip ? 0 : 1, 0), new Vector2(flip ? 1 : 0, 1), new Vector2(flip ? 0 : 1, 1) });
            for (int k = 0; k < 4; k++) b.n.Add(Vector3.up);
            b.tris[sub].AddRange(new[] { i, i + 2, i + 1, i + 1, i + 2, i + 3 });
        }
    }

    // Fern: fronds arching out of one root. Texture runs base (u=0) to tip (u=1).
    static void Fern(Buf b, System.Random r, Vector3 p, float len, int sub)
    {
        int fronds = r.Next(4, 8);
        float yaw0 = R(r, 0f, 360f);
        for (int f = 0; f < fronds; f++)
        {
            Vector3 dir = Quaternion.Euler(0f, yaw0 + f * 360f / fronds + R(r, -20f, 20f), 0f) * Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, dir) * len * R(r, 0.2f, 0.3f);
            float l = len * R(r, 0.7f, 1.15f), rise = R(r, 0.35f, 0.75f);
            int i = b.v.Count;
            for (int s = 0; s < 3; s++)
            {
                float t = s * 0.5f;
                Vector3 c = p + dir * l * t + Vector3.up * (l * rise * Mathf.Sin(t * 2.2f) - 0.05f);
                b.v.Add(c - side); b.v.Add(c + side);
                b.uv.Add(new Vector2(t, 0f)); b.uv.Add(new Vector2(t, 1f));
                b.n.Add(Vector3.up); b.n.Add(Vector3.up);
            }
            b.tris[sub].AddRange(new[] { i, i + 2, i + 1, i + 1, i + 2, i + 3, i + 2, i + 4, i + 3, i + 3, i + 4, i + 5 });
        }
    }

    // Large bush: a dome of ordinary-sized leaf cards instead of one stretched card, so texel size stays the same.
    static void Shrub(Buf b, System.Random r, Vector3 p, float h, int sub)
    {
        int cards = 9 + (int)(h * 4f);
        float rad = h * R(r, 0.55f, 0.8f);
        for (int c = 0; c < cards; c++)
        {
            Vector3 d = new Vector3(R(r, -1f, 1f), R(r, 0.1f, 1f), R(r, -1f, 1f)).normalized;
            Vector3 c0 = p + new Vector3(d.x * rad, d.y * h * 0.8f, d.z * rad) * R(r, 0.5f, 1f);
            Quaternion q = Quaternion.Euler(R(r, -35f, 35f), R(r, 0f, 360f), R(r, -35f, 35f));
            float s = R(r, 0.5f, 0.85f);
            Vector3 x = q * Vector3.right * s, y = q * Vector3.up * s, n = (d + Vector3.up).normalized;
            int i = b.v.Count;
            b.v.AddRange(new[] { c0 - x - y, c0 + x - y, c0 - x + y, c0 + x + y });
            b.uv.AddRange(new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
            for (int k = 0; k < 4; k++) b.n.Add(n);
            b.tris[sub].AddRange(new[] { i, i + 2, i + 1, i + 1, i + 2, i + 3 });
        }
    }

    // mats: dark bush, bright bush, fern A, fern B
    public static void Build(Transform root, Material[] mats)
    {
        var parent = new GameObject("Foliage").transform; parent.SetParent(root);
        var r = new System.Random(41);
        var chunks = new Dictionary<Vector2Int, Buf>();
        float lim = EmberTerrain.Half - 2f;
        int plants = 0;
        for (int i = 0; i < 26000; i++)
        {
            float cx = R(r, -lim, lim), cz = R(r, -lim, lim);
            float den = Density(cx, cz);
            if (r.NextDouble() > den) continue;
            // a clump: a few plants of one kind huddled together, sizes all over the place
            // ferns keep to their own patches so the undergrowth changes character as you walk
            bool fern = Mathf.PerlinNoise(cx * 0.06f + 90f, cz * 0.06f + 15f) + (float)r.NextDouble() * 0.3f > 0.62f;
            int sub = fern ? (r.NextDouble() < 0.6 ? 2 : 3) : r.NextDouble() < 0.3 ? 1 : 0;
            bool tall = r.NextDouble() < 0.07;
            int n = tall && !fern ? r.Next(1, 4) : r.Next(1, 2 + Mathf.RoundToInt(den * 5f));   // sparser clumps: less overdraw, more open ground
            float spread = tall && !fern ? R(r, 1.5f, 3.5f) : R(r, 0.6f, 2.6f);
            for (int k = 0; k < n; k++)
            {
                float x = cx + R(r, -1f, 1f) * spread, z = cz + R(r, -1f, 1f) * spread;
                if (Mathf.Abs(x) > lim || Mathf.Abs(z) > lim || EmberTerrain.PathDist(x, z) < 3f || EmberTerrain.Wet(x, z)) continue;
                float h = tall ? R(r, 1.7f, 3.3f) : R(r, 0.4f, 1.3f) * Mathf.Lerp(0.7f, 1.3f, den);
                float w = h * R(r, 0.8f, 1.7f);
                var key = new Vector2Int(Mathf.FloorToInt(x / Chunk), Mathf.FloorToInt(z / Chunk));
                if (!chunks.TryGetValue(key, out Buf b)) chunks[key] = b = new Buf();
                var pos = new Vector3(x, EmberTerrain.Height(x, z), z);
                if (fern) Fern(b, r, pos, R(r, 0.6f, 1.2f), sub); else if (tall) Shrub(b, r, pos, h, sub); else Plant(b, r, pos, w, h, sub);
                plants++;
            }
        }

        string dir = $"{EmberTextures.Dir}/Foliage";
        AssetDatabase.DeleteAsset(dir);
        AssetDatabase.CreateFolder(EmberTextures.Dir, "Foliage");
        foreach (var kv in chunks)
        {
            var m = new Mesh { name = $"Foliage_{kv.Key.x}_{kv.Key.y}" };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(kv.Value.v); m.SetNormals(kv.Value.n); m.SetUVs(0, kv.Value.uv);
            m.subMeshCount = 4;
            for (int s = 0; s < 4; s++) m.SetTriangles(kv.Value.tris[s], s);
            m.RecalculateBounds(); m.RecalculateTangents();
            AssetDatabase.CreateAsset(m, $"{dir}/{m.name}.asset");
            var go = new GameObject(m.name);
            go.transform.SetParent(parent);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.isStatic = true;
        }
        Debug.Log($"Emberwood: {plants} plants in {chunks.Count} foliage chunks.");
    }
}
