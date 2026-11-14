using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Lake: a water disc (Anthem/Water shader) at EmberTerrain.WaterY plus bright floating leaf patches near the shore.
public static class EmberLake
{
    static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    // Makes a cutout/lit material glow with its own texture.
    public static Material Emissive(Material m, Texture tex, Color c)
    {
        m.SetTexture("_EmissionMap", tex); m.SetColor("_EmissionColor", c);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(m);
        return m;
    }

    public static Mesh Save(Mesh m, string name)
    {
        m.name = name; m.RecalculateBounds(); m.RecalculateTangents();
        string path = $"{EmberTextures.Dir}/{name}.asset";
        AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static GameObject Obj(string name, Transform parent, Mesh mesh, Material mat)
    {
        var go = new GameObject(name); go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.isStatic = true;
        return go;
    }

    public static void Build(Transform root)
    {
        var parent = new GameObject("Lake").transform; parent.SetParent(root);
        Vector3 c = new Vector3(EmberTerrain.Lake.x, EmberTerrain.WaterY, EmberTerrain.Lake.y);

        // water disc; UVs in world metres so the ripple map tiles every 4 m
        const int seg = 28; float rx = EmberTerrain.LakeRx * 1.35f, rz = EmberTerrain.LakeRz * 1.35f;
        var v = new List<Vector3> { c }; var uv = new List<Vector2> { new Vector2(c.x, c.z) * 0.25f };
        var n = new List<Vector3> { Vector3.up }; var t = new List<int>();
        for (int i = 0; i < seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg;
            Vector3 p = c + new Vector3(Mathf.Cos(a) * rx, 0f, Mathf.Sin(a) * rz);
            v.Add(p); uv.Add(new Vector2(p.x, p.z) * 0.25f); n.Add(Vector3.up);
            t.AddRange(new[] { 0, 1 + (i + 1) % seg, 1 + i });
        }
        var water = new Mesh(); water.SetVertices(v); water.SetUVs(0, uv); water.SetNormals(n); water.SetTriangles(t, 0);
        var wm = EmberTextures.Lit("M_Water", null, new Color(0.030f, 0.022f, 0.016f), 1f);
        string np = $"{EmberTextures.Dir}/MJ/T_MJ_WaterN.png";
        if (AssetImporter.GetAtPath(np) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.NormalMap; imp.wrapMode = TextureWrapMode.Repeat; imp.SaveAndReimport();
            wm.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(np));
            wm.SetFloat("_BumpScale", 0.7f); wm.EnableKeyword("_NORMALMAP");
        }
        // custom water shader; it needs the camera depth texture for the soft shoreline
        Texture ripple = wm.GetTexture("_BumpMap");
        wm.shader = Shader.Find("Anthem/Water");
        wm.SetTexture("_BumpMap", ripple);
        wm.SetColor("_Deep", new Color(0.020f, 0.015f, 0.011f));
        wm.SetColor("_Haze", EmberAtmosphere.Fog);
        wm.SetVector("_MoonDir", Quaternion.Euler(-27f, EmberAtmosphere.SunEuler.y - 180f, 0f) * Vector3.forward);
        wm.SetColor("_Glint", new Color(2.4f, 1.4f, 0.55f)); wm.SetFloat("_Strength", 0.26f);
        wm.renderQueue = 3000;
        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp)
        {
            urp.supportsCameraDepthTexture = true; EditorUtility.SetDirty(urp);
        }
        EditorUtility.SetDirty(wm);
        Obj("Water", parent, Save(water, "EmberWater"), wm);

        // floating leaf patches, mostly in the shallows
        var r = new System.Random(8);
        v = new List<Vector3>(); uv = new List<Vector2>(); n = new List<Vector3>(); t = new List<int>();
        for (int i = 0; i < 900 && v.Count < 4 * 70; i++)
        {
            float a = R(r, 0f, Mathf.PI * 2f), d = R(r, 0.25f, 1.2f);
            float x = c.x + Mathf.Cos(a) * d * EmberTerrain.LakeRx, z = c.z + Mathf.Sin(a) * d * EmberTerrain.LakeRz;
            if (EmberTerrain.Height(x, z) > EmberTerrain.WaterY - 0.12f) continue;
            float s = R(r, 1.2f, 3.6f);
            Quaternion q = Quaternion.Euler(0f, R(r, 0f, 360f), 0f);
            Vector3 p = new Vector3(x, c.y + 0.02f + v.Count * 0.0004f, z), ax = q * Vector3.right * s, az = q * Vector3.forward * s * R(r, 0.6f, 1f);
            int k = v.Count;
            v.AddRange(new[] { p - ax - az, p + ax - az, p - ax + az, p + ax + az });
            uv.AddRange(new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
            for (int j = 0; j < 4; j++) n.Add(Vector3.up);
            t.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 });
        }
        var leaves = new Mesh(); leaves.SetVertices(v); leaves.SetUVs(0, uv); leaves.SetNormals(n); leaves.SetTriangles(t, 0);
        Texture2D lt = EmberTextures.User("T_MJ_LeafFloat", true, false);
        var lm = Emissive(EmberTextures.Lit("M_LeafFloat", lt, new Color(1f, 0.8f, 0.55f), 1f, true), lt, new Color(0.9f, 0.38f, 0.10f));
        Obj("FloatingLeaves", parent, Save(leaves, "EmberLakeLeaves"), lm);
    }
}
