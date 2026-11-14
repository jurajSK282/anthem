using System.Collections.Generic;
using UnityEngine;

// Contrast objects: patches of small glowing orange flowers along the path and around the lake shore.
// One merged mesh; each patch gets a dim point light so the ground around it picks up the colour.
public static class EmberFlowers
{
    static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    public static void Build(Transform root)
    {
        var parent = new GameObject("Flowers").transform; parent.SetParent(root);
        var r = new System.Random(19);
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var n = new List<Vector3>(); var t = new List<int>();
        float lim = EmberTerrain.Half - 8f;
        int patches = 0;
        for (int i = 0; i < 300 && patches < 26; i++)
        {
            float cx, cz;
            if (i % 2 == 0)
            {   // lake shore
                float a = R(r, 0f, Mathf.PI * 2f), d = EmberTerrain.LakeR * R(r, 0.9f, 1.5f);
                cx = EmberTerrain.Lake.x + Mathf.Cos(a) * d; cz = EmberTerrain.Lake.y + Mathf.Sin(a) * d;
            }
            else
            {   // beside the path
                cz = R(r, -lim, lim); cx = EmberTerrain.PathX(cz) + R(r, 3f, 10f) * (r.Next(2) * 2 - 1);
            }
            if (Mathf.Abs(cx) > lim || Mathf.Abs(cz) > lim || EmberTerrain.Wet(cx, cz)) continue;
            patches++;
            float rad = R(r, 1.6f, 4.5f);
            int count = (int)(rad * rad * R(r, 5f, 9f));
            for (int k = 0; k < count; k++)
            {
                // denser towards the middle of the patch
                float a = R(r, 0f, Mathf.PI * 2f), d = rad * Mathf.Pow((float)r.NextDouble(), 0.8f);
                float x = cx + Mathf.Cos(a) * d, z = cz + Mathf.Sin(a) * d;
                if (EmberTerrain.Wet(x, z) || EmberTerrain.PathDist(x, z) < 1.8f) continue;
                float h = R(r, 0.35f, 0.75f), yaw = R(r, 0f, 180f);
                Vector3 p = new Vector3(x, EmberTerrain.Height(x, z) - 0.03f, z);
                for (int c = 0; c < 2; c++)
                {
                    Vector3 ax = Quaternion.Euler(0f, yaw + c * 90f, 0f) * Vector3.right * h * 0.45f;
                    int j = v.Count;
                    v.AddRange(new[] { p - ax, p + ax, p - ax + Vector3.up * h, p + ax + Vector3.up * h });
                    uv.AddRange(new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
                    for (int m = 0; m < 4; m++) n.Add(Vector3.up);
                    t.AddRange(new[] { j, j + 2, j + 1, j + 1, j + 2, j + 3 });
                }
            }
            var lg = new GameObject("FlowerLight"); lg.transform.SetParent(parent);
            lg.transform.position = new Vector3(cx, EmberTerrain.Height(cx, cz) + 0.8f, cz);
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.55f, 0.18f);
            l.intensity = 3f; l.range = rad + 3f; l.shadows = LightShadows.None;
        }
        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetNormals(n); mesh.SetTriangles(t, 0);
        Texture2D tex = EmberTextures.User("T_MJ_Flower", true, false);
        var mat = EmberLake.Emissive(EmberTextures.Lit("M_Flower", tex, Color.white, 1f, true), tex, new Color(2.4f, 1.0f, 0.3f));
        var go = new GameObject("FlowerPatches"); go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh = EmberLake.Save(mesh, "EmberFlowers");
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.isStatic = true;
        Debug.Log($"Emberwood: {patches} flower patches, {v.Count / 8} flowers.");
    }
}
