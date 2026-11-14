using UnityEngine;

// Lake set pieces: a large bush on the island and a low, thick-stumped mossy tree on the shore.
public static class EmberLakeProps
{
    public static void Build(Transform root, Material[] bushMats, Material bark, Material leaves, Material moss)
    {
        var parent = new GameObject("LakeProps").transform; parent.SetParent(root);
        var r = new System.Random(52);

        // island bush: three overlapping domes
        var b = new EmberFoliage.Buf();
        Vector2 isl = EmberTerrain.Island;
        for (int i = 0; i < 3; i++)
        {
            float x = isl.x + (i - 1) * 1.6f, z = isl.y + (i == 1 ? 0.8f : -0.6f);
            EmberFoliage.Shrub(b, r, new Vector3(x, EmberTerrain.Height(x, z), z), i == 1 ? 3.4f : 2.3f, i == 1 ? 1 : 0);
        }
        var bm = new Mesh();
        bm.SetVertices(b.v); bm.SetNormals(b.n); bm.SetUVs(0, b.uv);
        bm.subMeshCount = 4;
        for (int s = 0; s < 4; s++) bm.SetTriangles(b.tris[s], s);
        var bush = new GameObject("IslandBush"); bush.transform.SetParent(parent);
        bush.AddComponent<MeshFilter>().sharedMesh = EmberLake.Save(bm, "EmberIslandBush");
        var mr = bush.AddComponent<MeshRenderer>();
        mr.sharedMaterials = bushMats; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        bush.isStatic = true;

        // shore tree: walk west from the lake centre until the ground is clearly dry
        float tz = EmberTerrain.Lake.y - 14f, tx = EmberTerrain.Lake.x;   // well south of the island
        while (tx > -EmberTerrain.Half + 10f && EmberTerrain.Height(tx, tz) < EmberTerrain.WaterY + 0.45f) tx -= 0.5f;
        var tree = new GameObject("LakeTree"); tree.transform.SetParent(parent);
        tree.transform.position = new Vector3(tx, EmberTerrain.Height(tx, tz), tz);
        tree.AddComponent<MeshFilter>().sharedMesh = EmberMeshes.Tree(300, 9.5f, 1.5f, 1.8f, 3f);
        tree.AddComponent<MeshRenderer>().sharedMaterials = new[] { bark, leaves, moss };
        var c = tree.AddComponent<CapsuleCollider>();
        c.radius = 1.3f; c.height = 8f; c.center = Vector3.up * 3f;
        tree.isStatic = true;
    }
}
