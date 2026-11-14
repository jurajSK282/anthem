using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Low-poly Gothic-style terrain: one mesh, per-polygon materials (0 path leaves, 1 forest floor, 2 soil/steep).
public static class EmberTerrain
{
    public const float Half = 90f;   // terrain spans -Half..Half on X and Z
    const int Cells = 60;            // 3 m quads

    // Sunken hollow-way winding along +Z.
    public static float PathX(float z) => 14f * Mathf.Sin(z * 0.045f) + 6f * Mathf.Sin(z * 0.11f + 1.3f);

    public static float PathDist(float x, float z) => Mathf.Abs(x - PathX(z));

    static float Fbm(float x, float z)
    {
        float h = 0f, amp = 1f, f = 0.018f;
        for (int i = 0; i < 4; i++)
        {
            h += (Mathf.PerlinNoise(x * f + 31.7f, z * f + 77.3f) - 0.5f) * amp;
            amp *= 0.5f; f *= 2.1f;
        }
        return h;
    }

    public static float Height(float x, float z)
    {
        float rise = z * 0.07f;                                             // walk uphill towards the light
        float wild = rise + Fbm(x, z) * 14f + 2.2f;                         // forest floor with hills
        float path = rise + Mathf.Sin(z * 0.09f) * 1.6f + Fbm(x, z) * 2f;   // path rolls up and down
        float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.5f, 11f, PathDist(x, z)));
        return Mathf.Lerp(path, wild, t);
    }

    public static GameObject Build(Transform parent, Material[] mats)
    {
        int n = Cells + 1;
        float step = Half * 2f / Cells;
        var verts = new Vector3[n * n];
        var uvs = new Vector2[n * n];
        var rng = new System.Random(7);
        for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                // jitter interior vertices so the grid does not read as a grid
                bool edge = x == 0 || z == 0 || x == n - 1 || z == n - 1;
                float jx = edge ? 0f : ((float)rng.NextDouble() - 0.5f) * step * 0.55f;
                float jz = edge ? 0f : ((float)rng.NextDouble() - 0.5f) * step * 0.55f;
                float wx = -Half + x * step + jx, wz = -Half + z * step + jz;
                verts[z * n + x] = new Vector3(wx, Height(wx, wz), wz);
                uvs[z * n + x] = new Vector2(wx, wz) * 0.55f;
            }

        var tris = new List<int>[] { new List<int>(), new List<int>(), new List<int>() };
        for (int z = 0; z < Cells; z++)
            for (int x = 0; x < Cells; x++)
            {
                int a = z * n + x, b = a + 1, c = a + n, d = c + 1;
                Vector3 mid = (verts[a] + verts[b] + verts[c] + verts[d]) * 0.25f;
                float slope = Vector3.Angle(Vector3.Cross(verts[c] - verts[a], verts[b] - verts[a]), Vector3.up);
                float patch = Mathf.PerlinNoise(mid.x * 0.07f + 5f, mid.z * 0.07f + 9f);
                int m = slope > 30f ? 2 : (PathDist(mid.x, mid.z) < 4.5f || patch > 0.56f) ? 0 : 1;
                tris[m].AddRange(new[] { a, c, b, b, c, d });
            }

        var mesh = new Mesh { name = "EmberTerrain" };
        mesh.vertices = verts; mesh.uv = uvs;
        mesh.subMeshCount = 3;
        for (int i = 0; i < 3; i++) mesh.SetTriangles(tris[i], i);
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
        string path = $"{EmberTextures.Dir}/EmberTerrain.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);

        var go = new GameObject("Terrain");
        go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = mats;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
        go.isStatic = true;
        return go;
    }
}
