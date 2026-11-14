using UnityEngine;
using UnityEngine.UI;

public class PoCHud : MonoBehaviour
{
    public Camera playerCamera;
    public PoCPlayerHealth health;
    public bool pixelate = true;
    public int renderWidth = 640;
    public int renderHeight = 360;

    private Image[] healthHearts;
    private Image[] staminaHearts;
    private static readonly Color HealthColor = new Color(0.9f, 0.08f, 0.08f);
    private static readonly Color StaminaColor = new Color(0.95f, 0.45f, 0.15f);
    private static readonly Color EmptyColor = new Color(0.15f, 0.05f, 0.05f, 0.6f);

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (health == null) health = GetComponent<PoCPlayerHealth>();
        if (playerCamera == null || health == null)
        {
            Debug.LogError($"{name}: PoCHud potrebuje playerCamera a PoCPlayerHealth.", this);
            enabled = false;
            return;
        }

        Transform canvas = BuildCanvas();
        if (pixelate) BuildLowResView(canvas);

        healthHearts = BuildHearts(canvas, "Health", -24f, HealthColor, Mathf.CeilToInt(health.maxHealth));
        staminaHearts = BuildHearts(canvas, "Stamina", -66f, StaminaColor, Mathf.CeilToInt(health.maxStamina));
        BuildCrosshair(canvas);

        var minimap = new GameObject("MinimapRig").AddComponent<PoCMinimap>();
        minimap.Build(canvas, transform);
    }

    void Update()
    {
        Refresh(healthHearts, health.Health, HealthColor);
        Refresh(staminaHearts, health.Stamina, StaminaColor);
    }

    void Refresh(Image[] hearts, float value, Color full)
    {
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].color = value >= i + 1 ? full : value > i ? Color.Lerp(EmptyColor, full, value - i) : EmptyColor;
    }

    Transform BuildCanvas()
    {
        var go = new GameObject("HUD Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        return go.transform;
    }

    void BuildLowResView(Transform canvas)
    {
        var rt = new RenderTexture(renderWidth, renderHeight, 24) { filterMode = FilterMode.Point, antiAliasing = 1 };
        playerCamera.targetTexture = rt;

        RectTransform r = NewRect("LowResView", canvas);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        var raw = r.gameObject.AddComponent<RawImage>();
        raw.texture = rt;
        raw.raycastTarget = false;
    }

    Image[] BuildHearts(Transform canvas, string rowName, float y, Color color, int count)
    {
        Sprite heart = PoCPixelSprites.Heart();
        var result = new Image[count];
        for (int i = 0; i < count; i++)
        {
            RectTransform r = NewRect($"{rowName}_{i}", canvas);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(24f + i * 38f, y);
            r.sizeDelta = new Vector2(35f, 30f);
            result[i] = r.gameObject.AddComponent<Image>();
            result[i].sprite = heart;
            result[i].color = color;
            result[i].raycastTarget = false;
        }
        return result;
    }

    void BuildCrosshair(Transform canvas)
    {
        AddCenterImage(canvas, "CrosshairGlow", PoCPixelSprites.Glow(), 170f);
        AddCenterImage(canvas, "Crosshair", PoCPixelSprites.Cross(), 60f);
    }

    void AddCenterImage(Transform canvas, string name, Sprite sprite, float size)
    {
        RectTransform r = NewRect(name, canvas);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = Vector2.one * size;
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
}
