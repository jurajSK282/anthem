using UnityEngine;

public static class PoCPixelSprites
{
    static readonly string[] HeartMask =
    {
        ".XX.XX.",
        "XXXXXXX",
        "XXXXXXX",
        ".XXXXX.",
        "..XXX..",
        "...X...",
    };

    public static Sprite Heart() => FromMask(HeartMask);

    public static Sprite Cross()
    {
        const int size = 15, c = 7;
        Texture2D tex = NewTexture(size, size);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Abs(x - c), dy = Mathf.Abs(y - c);
                bool arm = (dx <= 1 && dy <= c) || (dy <= 1 && dx <= c);
                bool core = (dx == 0 && dy <= c) || (dy == 0 && dx <= c);
                tex.SetPixel(x, y, !arm ? Color.clear : core ? Color.white : new Color(1f, 0.1f, 0.1f));
            }
        return ToSprite(tex);
    }

    public static Sprite Glow()
    {
        const int size = 64;
        Texture2D tex = NewTexture(size, size);
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float r = Vector2.Distance(new Vector2(x, y), Vector2.one * (size - 1) * 0.5f) / (size * 0.5f);
                tex.SetPixel(x, y, new Color(1f, 0.1f, 0.1f, Mathf.Pow(Mathf.Clamp01(1f - r), 2f) * 0.35f));
            }
        return ToSprite(tex);
    }

    public static Sprite Disc(int size, int ringWidth)
    {
        Texture2D tex = NewTexture(size, size);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * half);
                bool inside = r <= half;
                bool ring = ringWidth > 0 && r > half - ringWidth;
                bool solid = ringWidth > 0 ? inside && ring : inside;
                tex.SetPixel(x, y, solid ? Color.white : Color.clear);
            }
        return ToSprite(tex);
    }

    static Sprite FromMask(string[] rows)
    {
        int h = rows.Length, w = rows[0].Length;
        Texture2D tex = NewTexture(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, h - 1 - y, rows[y][x] == 'X' ? Color.white : Color.clear);
        return ToSprite(tex);
    }

    static Texture2D NewTexture(int w, int h)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
    }

    static Sprite ToSprite(Texture2D tex)
    {
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
