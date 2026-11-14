using System.IO;
using UnityEditor;
using UnityEngine;

// Placeholder procedural textures + material helpers. Swap PNGs for Midjourney --tile textures later.
public static class EmberTextures
{
    public const string Dir = "Assets/_Project/Art/Emberwood";

    public static Texture2D Save(string name, Texture2D tex, bool alpha, bool repeat)
    {
        Directory.CreateDirectory(Dir);
        string path = $"{Dir}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = alpha;
        imp.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        imp.mipmapEnabled = true;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // Scattered leaf blobs. density 0..1, transparent background when alpha = true.
    public static Texture2D Leaves(string name, int size, int count, Color bg, Color a, Color b, bool alpha, int seed)
    {
        var rng = new System.Random(seed);
        var px = new Color[size * size];
        Color clear = alpha ? new Color(a.r, a.g, a.b, 0f) : bg;
        for (int i = 0; i < px.Length; i++)
        {
            float n = alpha ? 0f : (float)rng.NextDouble() * 0.25f;
            px[i] = alpha ? clear : Color.Lerp(bg, Color.black, n);
        }
        for (int l = 0; l < count; l++)
        {
            float cx = (float)rng.NextDouble() * size, cy = (float)rng.NextDouble() * size;
            // cards: keep leaves inside a ragged round clump so quads do not read as squares
            if (alpha && Vector2.Distance(new Vector2(cx, cy), Vector2.one * size * 0.5f) > size * (0.26f + 0.2f * (float)rng.NextDouble())) continue;
            float ang = (float)rng.NextDouble() * Mathf.PI, len = size * (0.05f + 0.05f * (float)rng.NextDouble());
            float wid = len * 0.45f;
            Color c = Color.Lerp(a, b, (float)rng.NextDouble()) * (0.55f + 0.6f * (float)rng.NextDouble());
            c.a = 1f;
            int r = Mathf.CeilToInt(len);
            for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                {
                    float u = x * Mathf.Cos(ang) + y * Mathf.Sin(ang), v = -x * Mathf.Sin(ang) + y * Mathf.Cos(ang);
                    if (u * u / (len * len) + v * v / (wid * wid) > 1f) continue;
                    int ix = Mathf.FloorToInt(cx + x), iy = Mathf.FloorToInt(cy + y);
                    if (alpha && (ix < 1 || iy < 1 || ix >= size - 1 || iy >= size - 1)) continue;
                    ix = (ix % size + size) % size; iy = (iy % size + size) % size;
                    px[iy * size + ix] = Mathf.Abs(v) < 0.6f ? c * 0.7f + new Color(0, 0, 0, 0.3f) : c;
                }
        }
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.SetPixels(px); t.Apply();
        return Save(name, t, alpha, !alpha);
    }

    public static Texture2D Bark(string name, int size, Color dark, Color light, int seed)
    {
        var rng = new System.Random(seed);
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var streak = new float[size];
        for (int x = 0; x < size; x++) streak[x] = (float)rng.NextDouble();
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int sx = (x + (int)(Mathf.Sin(y * 0.2f) * 2f) + size) % size;
                float n = streak[sx] * 0.7f + (float)rng.NextDouble() * 0.3f;
                t.SetPixel(x, y, Color.Lerp(dark, light, n * n));
            }
        t.Apply();
        return Save(name, t, false, true);
    }

    public static Texture2D SoftDot(string name, int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * size * 0.5f) / (size * 0.5f);
                float a = Mathf.Clamp01(1f - d);
                t.SetPixel(x, y, new Color(1, 1, 1, a * a));
            }
        t.Apply();
        return Save(name, t, true, false);
    }

    static Material Asset(string name, string shader)
    {
        string path = $"{Dir}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, path); }
        else m.shader = Shader.Find(shader);
        return m;
    }

    public static Material Lit(string name, Texture2D tex, Color tint, float tiling = 1f, bool cutout = false)
    {
        var m = Asset(name, "Universal Render Pipeline/Lit");
        m.SetTexture("_BaseMap", tex);
        m.SetTextureScale("_BaseMap", Vector2.one * tiling);
        m.SetColor("_BaseColor", tint);
        m.SetFloat("_Smoothness", 0f);
        m.SetFloat("_SpecularHighlights", 0f);
        m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        if (cutout)
        {
            m.SetFloat("_Cull", 0f); m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.4f);
            m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450; m.doubleSidedGI = true;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    public static Material Glow(string name, Color baseCol, Color emission)
    {
        var m = Asset(name, "Universal Render Pipeline/Lit");
        m.SetColor("_BaseColor", baseCol);
        m.SetColor("_EmissionColor", emission);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.SetFloat("_Smoothness", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    public static Material Particle(string name, Texture2D tex, Color tint, bool additive)
    {
        var m = Asset(name, "Universal Render Pipeline/Particles/Unlit");
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", tint);
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", additive ? 2f : 0f);
        m.SetFloat("_SrcBlend", 5f); m.SetFloat("_DstBlend", additive ? 1f : 10f);
        m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = 3000;
        EditorUtility.SetDirty(m);
        return m;
    }
}
