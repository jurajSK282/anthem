using System.Collections.Generic;
using UnityEngine;

// Glowing mushrooms: the scene's small light sources. Submesh 0 = stem, submesh 1 = glowing cap and gills.
// Clusters line the path and are also sprinkled through the forest; each cluster carries a point light.
public static class EmberMushrooms
{
    const int Sides = 7;

    static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    static int Ring(List<Vector3> v, List<Vector2> uv, System.Random r, Vector3 c, float rad, float vCoord, float jitter)
    {
        int first = v.Count;
        for (int i = 0; i <= Sides; i++)
        {
            float a = i / (float)Sides * Mathf.PI * 2f, k = rad * (1f + R(r, -jitter, jitter));
            v.Add(i == Sides ? v[first] : c + new Vector3(Mathf.Cos(a) * k, 0f, Mathf.Sin(a) * k));
            uv.Add(new Vector2(i / (float)Sides, vCoord));
        }
        return first;
    }

    // Quads between two rings; lower ring first gives outward faces, flip for the underside of the cap.
    static void Stitch(List<int> t, int lo, int hi, bool flip)
    {
        for (int i = 0; i < Sides; i++)
        {
            int a = lo + i, c = hi + i;
            if (flip) t.AddRange(new[] { a, a + 1, c, a + 1, c + 1, c });
            else t.AddRange(new[] { a, c, a + 1, a + 1, c, c + 1 });
        }
    }

    static Mesh Make(int seed, float h, float capR, float capH)
    {
        var r = new System.Random(seed);
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        var stem = new List<int>(); var cap = new List<int>();
        float sr = h * 0.11f;
        Vector3 bend = new Vector3(R(r, -1f, 1f), 0f, R(r, -1f, 1f)) * h * 0.18f, top = Vector3.up * h + bend;

        int s0 = Ring(v, uv, r, Vector3.down * 0.05f, sr * 1.5f, 0f, 0.1f);
        int s1 = Ring(v, uv, r, Vector3.up * h * 0.5f + bend * 0.35f, sr, 0.5f, 0.1f);
        int s2 = Ring(v, uv, r, top, sr * 0.8f, 1f, 0.05f);
        Stitch(stem, s0, s1, false); Stitch(stem, s1, s2, false);

        // gills: from the stem top out and slightly down to the rim
        int rim = Ring(v, uv, r, top - Vector3.up * capH * 0.25f, capR, 0f, 0.12f);
        Stitch(cap, s2, rim, true);
        // dome: rim up to the tip
        int prev = rim;
        for (int k = 1; k <= 3; k++)
        {
            float a = k / 3f * Mathf.PI * 0.5f;
            int ring = Ring(v, uv, r, top + Vector3.up * (capH * Mathf.Sin(a) - capH * 0.25f), Mathf.Max(0.01f, capR * Mathf.Cos(a)), k / 3f, 0.06f);
            Stitch(cap, prev, ring, false);
            prev = ring;
        }

        var m = new Mesh();
        m.SetVertices(v); m.SetUVs(0, uv);
        m.subMeshCount = 2; m.SetTriangles(stem, 0); m.SetTriangles(cap, 1);
        m.RecalculateNormals();
        return EmberLake.Save(m, $"EmberMushroom_{seed}");
    }

    public static void Build(Transform root, Material stemMat, Material capMat)
    {
        var parent = new GameObject("Mushrooms").transform; parent.SetParent(root);
        var meshes = new[] { Make(1, 0.55f, 0.30f, 0.20f), Make(2, 0.75f, 0.20f, 0.30f), Make(3, 0.38f, 0.40f, 0.13f), Make(4, 0.60f, 0.27f, 0.24f) };
        var mats = new[] { stemMat, capMat };
        var r = new System.Random(5);
        float lim = EmberTerrain.Half - 6f;
        for (int cluster = 0; cluster < 82; cluster++)
        {
            bool onPath = cluster < 46;   // the rest fill the forest so no area is without a light
            float z = R(r, -lim, lim);
            float x = onPath ? EmberTerrain.PathX(z) + R(r, 2.2f, 13f) * (r.Next(2) * 2 - 1) : R(r, -lim, lim);
            if (Mathf.Abs(x) > lim || EmberTerrain.Wet(x, z) || (!onPath && EmberTerrain.PathDist(x, z) < 12f)) continue;
            int n = r.Next(2, 7);
            for (int i = 0; i < n; i++)
            {
                float bx = x + R(r, -1.1f, 1.1f), bz = z + R(r, -1.1f, 1.1f), s = i == 0 ? R(r, 1.3f, 2.1f) : R(r, 0.5f, 1.3f);
                var go = new GameObject("Mushroom"); go.transform.SetParent(parent);
                go.transform.SetPositionAndRotation(new Vector3(bx, EmberTerrain.Height(bx, bz), bz), Quaternion.Euler(R(r, -8f, 8f), R(r, 0f, 360f), R(r, -8f, 8f)));
                go.transform.localScale = Vector3.one * s;
                go.AddComponent<MeshFilter>().sharedMesh = meshes[r.Next(meshes.Length)];
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = mats; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.isStatic = true;
            }
            var lg = new GameObject("MushroomLight"); lg.transform.SetParent(parent);
            lg.transform.position = new Vector3(x, EmberTerrain.Height(x, z) + 0.9f, z);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.5f, 0.15f);
            l.intensity = onPath ? 9f : 6f; l.range = onPath ? 7.5f : 6.5f; l.shadows = LightShadows.None;
        }
    }
}
