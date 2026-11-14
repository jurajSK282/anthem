using UnityEngine;
using UnityEngine.UI;

public class PoCMinimap : MonoBehaviour
{
    public float height = 35f;
    public float orthoSize = 22f;
    public int resolution = 128;
    public float uiSize = 150f;

    private Camera cam;
    private Transform target;

    public void Build(Transform canvas, Transform followTarget)
    {
        target = followTarget;

        var rt = new RenderTexture(resolution, resolution, 16) { filterMode = FilterMode.Point };
        var camGo = new GameObject("MinimapCamera");
        camGo.transform.SetParent(transform, false);
        cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.16f, 0.1f);
        cam.nearClipPlane = 1f;
        cam.farClipPlane = height + 20f;
        cam.depth = -10f;
        cam.targetTexture = rt;

        Sprite disc = PoCPixelSprites.Disc(128, 0);
        Sprite ring = PoCPixelSprites.Disc(128, 3);

        RectTransform root = NewRect("Minimap", canvas);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(1f, 1f);
        root.anchoredPosition = new Vector2(-24f, -24f);
        root.sizeDelta = Vector2.one * uiSize;
        var maskImg = root.gameObject.AddComponent<Image>();
        maskImg.sprite = disc;
        root.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        RectTransform view = NewRect("View", root);
        Stretch(view);
        var raw = view.gameObject.AddComponent<RawImage>();
        raw.texture = rt;
        raw.raycastTarget = false;

        RectTransform frame = NewRect("Frame", canvas);
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(1f, 1f);
        frame.anchoredPosition = root.anchoredPosition;
        frame.sizeDelta = root.sizeDelta;
        var frameImg = frame.gameObject.AddComponent<Image>();
        frameImg.sprite = ring;
        frameImg.raycastTarget = false;
    }

    void LateUpdate()
    {
        if (cam == null || target == null) return;
        cam.transform.position = target.position + Vector3.up * height;
        cam.transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
