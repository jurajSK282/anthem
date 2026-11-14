using UnityEditor;
using UnityEngine;

// Low-poly Unity Terrain with 4 noise-blended layers (soil, leaf litter, orange drifts, trodden path).
// It is a normal Terrain, so heights and layers can also be hand-painted with the Terrain tools afterwards.
public static class EmberTerrain
{
    public const float Half = 90f;   // terrain spans -Half..Half on X and Z
    const int HeightRes = 129;       // ~1.4 m cells keeps the chunky silhouette
    const int AlphaRes = 1024;
    const float MinY = -16f, RangeY = 40f;

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

    // Lake beside the path: a shallow basin with a flattened shore. WaterY is the water surface height.
    public const float LakeZ = 15f, LakeR = 19f;
    public static readonly Vector2 Lake = new Vector2(PathX(LakeZ) + 24f, LakeZ);
    public static readonly float WaterY = Base(PathX(LakeZ), LakeZ) - 0.9f;

    // 0 at the lake centre, ~1 at the (wobbly) shoreline.
    public static float LakeQ(float x, float z) =>
        new Vector2(x - Lake.x, z - Lake.y).magnitude / (LakeR * (0.8f + 0.45f * N(x, z, 0.06f, 33f)));

    public static bool Wet(float x, float z) => Height(x, z) < WaterY + 0.2f;

    public static float Height(float x, float z)
    {
        float h = Base(x, z), q = LakeQ(x, z);
        if (q >= 2f) return h;
        h = Mathf.Lerp(WaterY + 0.6f, h, Ss(1.05f, 2f, q));
        return Mathf.Lerp(WaterY - 0.7f, h, Ss(0.6f, 1.05f, q));
    }

    static float Base(float x, float z)
    {
        float rise = z * 0.07f;                                             // walk uphill towards the light
        float wild = rise + Fbm(x, z) * 14f + 2.2f;                         // forest floor with hills
        float path = rise + Mathf.Sin(z * 0.09f) * 1.6f + Fbm(x, z) * 2f;   // path rolls up and down
        float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.5f, 11f, PathDist(x, z)));
        return Mathf.Lerp(path, wild, t);
    }

    // GLSL-style smoothstep(edge0, edge1, x). Mathf.SmoothStep(from, to, t) is a different function.
    public static float Ss(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

    static float N(float x, float z, float f, float o) => Mathf.PerlinNoise(x * f + o, z * f + o * 1.7f);

    // Layer weights: 0 soil, 1 litter, 2 drift, 3 path. Fine noise makes every border ragged.
    static void Weights(float x, float z, float[] w)
    {
        float fine = N(x, z, 0.55f, 3f) - 0.5f, grit = N(x, z, 2.3f, 9f) - 0.5f;
        float d = PathDist(x, z) + fine * 2.2f;
        float slope = Mathf.Atan(new Vector2(Height(x + 0.7f, z) - Height(x - 0.7f, z), Height(x, z + 0.7f) - Height(x, z - 0.7f)).magnitude / 1.4f) * Mathf.Rad2Deg;

        float litter = Ss(0.30f, 0.50f, N(x, z, 0.085f, 21f) + fine * 0.35f + grit * 0.15f);
        float drift = Ss(0.47f, 0.62f, N(x, z, 0.035f, 55f) * 0.6f + N(x, z, 0.11f, 70f) * 0.4f + fine * 0.3f + grit * 0.12f);
        // leaves pile up in the gutter at the foot of the banks
        drift = Mathf.Max(drift, Ss(0.45f, 0.7f, (1f - Mathf.Abs(d - 4.2f) / 2.2f) * (0.55f + N(x, z, 0.06f, 5f))));
        float path = (1f - Ss(1.2f, 3.2f, d)) * 0.7f;
        float steep = Ss(24f, 38f, slope + grit * 10f);

        float soil = 1f, lit = 0f, dr = 0f, pa = 0f;
        lit = litter; soil *= 1f - litter;
        dr = drift; soil *= 1f - drift; lit *= 1f - drift;
        pa = path; soil *= 1f - path; lit *= 1f - path; dr *= 1f - path;
        soil += (lit + dr) * steep; lit *= 1f - steep; dr *= 1f - steep;
        float wet = 1f - Ss(WaterY + 0.1f, WaterY + 0.6f, Height(x, z));   // bare mud at the waterline and below
        w[0] = Mathf.Lerp(soil, 1f, wet); w[1] = lit * (1f - wet); w[2] = dr * (1f - wet); w[3] = pa * (1f - wet);
    }

    static TerrainLayer Layer(string name, Texture2D tex, float tile)
    {
        string path = $"{EmberTextures.Dir}/{name}.terrainlayer";
        var l = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (l == null) { l = new TerrainLayer(); AssetDatabase.CreateAsset(l, path); }
        l.diffuseTexture = tex; l.tileSize = Vector2.one * tile;
        l.smoothness = 0f; l.metallic = 0f; l.specular = Color.black;
        EditorUtility.SetDirty(l);
        return l;
    }

    // texs: soil, litter, drift, path
    public static GameObject Build(Transform parent, Texture2D[] texs)
    {
        string dataPath = $"{EmberTextures.Dir}/EmberTerrainData.asset";
        AssetDatabase.DeleteAsset(dataPath);
        var data = new TerrainData { heightmapResolution = HeightRes, alphamapResolution = AlphaRes, baseMapResolution = 1024 };
        data.size = new Vector3(Half * 2f, RangeY, Half * 2f);
        AssetDatabase.CreateAsset(data, dataPath);

        var heights = new float[HeightRes, HeightRes];
        for (int z = 0; z < HeightRes; z++)
            for (int x = 0; x < HeightRes; x++)
            {
                float wx = -Half + x / (HeightRes - 1f) * Half * 2f, wz = -Half + z / (HeightRes - 1f) * Half * 2f;
                heights[z, x] = Mathf.Clamp01((Height(wx, wz) - MinY) / RangeY);
            }
        data.SetHeights(0, 0, heights);

        data.terrainLayers = new[]
        {
            Layer("L_Soil", texs[0], 6.3f), Layer("L_Litter", texs[1], 5.1f),
            Layer("L_Drift", texs[2], 4.3f), Layer("L_Path", texs[3], 5.7f),
        };
        var alpha = new float[AlphaRes, AlphaRes, 4];
        var w = new float[4];
        for (int z = 0; z < AlphaRes; z++)
            for (int x = 0; x < AlphaRes; x++)
            {
                Weights(-Half + (x + 0.5f) / AlphaRes * Half * 2f, -Half + (z + 0.5f) / AlphaRes * Half * 2f, w);
                float sum = w[0] + w[1] + w[2] + w[3];
                for (int i = 0; i < 4; i++) alpha[z, x, i] = w[i] / sum;
            }
        data.SetAlphamaps(0, 0, alpha);

        GameObject go = Terrain.CreateTerrainGameObject(data);
        go.name = "Terrain";
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(-Half, MinY, -Half);
        var t = go.GetComponent<Terrain>();
        string matPath = $"{EmberTextures.Dir}/M_Terrain.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
        t.materialTemplate = mat;
        t.heightmapPixelError = 1f;
        t.basemapDistance = 400f;
        t.drawInstanced = true;
        go.isStatic = true;
        return go;
    }
}
