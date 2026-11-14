using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Scatters the stone models from Art/Stones: pebbles and small rocks along the path edges,
// medium rocks in huddles on the banks, a few boulders and standing stones deeper in the forest.
public static class EmberRocks
{
    const string Dir = "Assets/_Project/Art/Stones";

    static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    static float Ground(float x, float z)
    {
        Terrain t = Terrain.activeTerrain;
        return t != null ? t.SampleHeight(new Vector3(x, 0f, z)) + t.transform.position.y : EmberTerrain.Height(x, z);
    }

    static List<GameObject> Load(params string[] keys)
    {
        var list = new List<GameObject>();
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Dir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx")) continue;
            string name = Path.GetFileNameWithoutExtension(path);
            foreach (string k in keys)
                if (name.Contains(k)) { list.Add(AssetDatabase.LoadAssetAtPath<GameObject>(path)); break; }
        }
        return list;
    }

    static void Place(Transform parent, System.Random r, List<GameObject> set, Material mat, float x, float z, float scale, float tilt, bool collide)
    {
        float lim = EmberTerrain.Half - 3f;
        if (set.Count == 0 || Mathf.Abs(x) > lim || Mathf.Abs(z) > lim) return;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(set[r.Next(set.Count)], parent);
        // seat on the real terrain surface: lowest point under the footprint, so the downhill side never floats
        Vector3 ext = go.GetComponentInChildren<MeshFilter>().sharedMesh.bounds.extents * scale;
        float foot = Mathf.Max(ext.x, ext.z) * 0.7f, y = Ground(x, z);
        for (int i = 0; i < 4; i++)
            y = Mathf.Min(y, Ground(x + (i == 0 ? foot : i == 1 ? -foot : 0f), z + (i == 2 ? foot : i == 3 ? -foot : 0f)));
        go.transform.position = new Vector3(x, y - 0.05f * scale, z);
        go.transform.rotation = Quaternion.Euler(R(r, -tilt, tilt), R(r, 0f, 360f), R(r, -tilt, tilt));
        go.transform.localScale = Vector3.one * scale;
        foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
        {
            mr.sharedMaterial = mat;
            if (collide) mr.gameObject.AddComponent<MeshCollider>();
        }
        go.isStatic = true;
    }

    public static void Build(Transform root, Material mat)
    {
        var parent = new GameObject("Rocks").transform; parent.SetParent(root);
        var r = new System.Random(77);
        var small = Load("Pebble", "SmallRock", "Rubble", "Cluster_Small");
        var medium = Load("MediumRock", "Slab_Flat", "Slab_Thick", "Strata", "Cluster_Large");
        var big = Load("Boulder_A", "Boulder_B", "Menhir", "Slab_Leaning");
        var huge = Load("Boulder_Large");
        float lim = EmberTerrain.Half - 6f;

        // path edges: stones that rolled down the banks, a few lying on the path itself
        for (int i = 0; i < 170; i++)
        {
            float z = R(r, -lim, lim), side = r.Next(2) == 0 ? -1f : 1f;
            float d = r.NextDouble() < 0.15 ? R(r, 0f, 1.5f) : R(r, 1.8f, 4.8f);
            Place(parent, r, small, mat, EmberTerrain.PathX(z) + side * d, z, R(r, 0.6f, 1.5f), 12f, false);
        }
        // huddles on the banks and in the forest: one larger rock with small ones around it
        for (int i = 0; i < 46; i++)
        {
            float cx = R(r, -lim, lim), cz = R(r, -lim, lim);
            if (EmberTerrain.PathDist(cx, cz) < 3.5f) continue;
            Place(parent, r, medium, mat, cx, cz, R(r, 0.8f, 1.5f), 8f, true);
            int n = r.Next(1, 5);
            for (int k = 0; k < n; k++)
                Place(parent, r, small, mat, cx + R(r, -2.2f, 2.2f), cz + R(r, -2.2f, 2.2f), R(r, 0.7f, 1.6f), 15f, false);
        }
        // landmarks: boulders and standing stones away from the path
        for (int i = 0; i < 16; i++)
        {
            float cx = R(r, -lim, lim), cz = R(r, -lim, lim);
            if (EmberTerrain.PathDist(cx, cz) < 6f) continue;
            Place(parent, r, i % 5 == 0 ? huge : big, mat, cx, cz, R(r, 0.9f, 1.4f), 6f, true);
            Place(parent, r, medium, mat, cx + R(r, -3f, 3f), cz + R(r, -3f, 3f), R(r, 0.7f, 1.2f), 8f, true);
        }
        Debug.Log($"Emberwood: {parent.childCount} rocks.");
    }
}
