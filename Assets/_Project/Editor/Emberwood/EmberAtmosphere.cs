using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Everything that sells the mood: sky, fog, sun, ambient, post-processing. Tune values here.
public static class EmberAtmosphere
{
    public static readonly Color Fog = new Color(0.64f, 0.36f, 0.14f);
    public static readonly Color SunColor = new Color(1.00f, 0.56f, 0.24f);
    public const float FogDensity = 0.034f;
    // Sun sits low ahead of the player (+Z), so trees are back-lit silhouettes.
    public static readonly Vector3 SunEuler = new Vector3(13f, 196f, 0f);

    public static void Build(Transform parent)
    {
        var sunGo = new GameObject("Sun");
        sunGo.transform.SetParent(parent);
        sunGo.transform.rotation = Quaternion.Euler(SunEuler);
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = SunColor;
        sun.intensity = 3.2f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.92f;

        string skyPath = $"{EmberTextures.Dir}/M_EmberSky.mat";
        var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (sky == null) { sky = new Material(Shader.Find("Anthem/GradientSky")); AssetDatabase.CreateAsset(sky, skyPath); }
        sky.shader = Shader.Find("Anthem/GradientSky");
        sky.SetColor("_Horizon", Fog);
        sky.SetColor("_Zenith", new Color(0.30f, 0.15f, 0.07f));
        sky.SetColor("_Ground", Fog * 0.8f);
        sky.SetColor("_SunGlow", new Color(1.0f, 0.62f, 0.30f));
        sky.SetVector("_SunDir", -sunGo.transform.forward);
        EditorUtility.SetDirty(sky);

        RenderSettings.skybox = sky;
        RenderSettings.sun = sun;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = Fog;
        RenderSettings.fogDensity = FogDensity;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.46f, 0.27f, 0.12f);
        RenderSettings.ambientEquatorColor = new Color(0.27f, 0.15f, 0.07f);
        RenderSettings.ambientGroundColor = new Color(0.06f, 0.03f, 0.02f);
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.reflectionIntensity = 0f;

        string profPath = $"{EmberTextures.Dir}/EmberVolume.asset";
        AssetDatabase.DeleteAsset(profPath);
        var prof = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(prof, profPath);

        var bloom = Add<Bloom>(prof);
        bloom.threshold.value = 0.85f; bloom.intensity.value = 1.6f; bloom.scatter.value = 0.78f;
        bloom.tint.value = new Color(1f, 0.75f, 0.5f);
        var tone = Add<Tonemapping>(prof);
        tone.mode.value = TonemappingMode.Neutral;
        var col = Add<ColorAdjustments>(prof);
        col.postExposure.value = 0.1f; col.contrast.value = 22f; col.saturation.value = 12f;
        col.colorFilter.value = new Color(1f, 0.97f, 0.90f);
        var vig = Add<Vignette>(prof);
        vig.intensity.value = 0.38f; vig.smoothness.value = 0.5f; vig.color.value = new Color(0.08f, 0.02f, 0f);
        var grain = Add<FilmGrain>(prof);
        grain.intensity.value = 0.25f;
        EditorUtility.SetDirty(prof);

        var volGo = new GameObject("PostFX Volume");
        volGo.transform.SetParent(parent);
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = prof;
    }

    static T Add<T>(VolumeProfile prof) where T : VolumeComponent
    {
        var c = prof.Add<T>(true);
        c.name = typeof(T).Name;
        AssetDatabase.AddObjectToAsset(c, prof);
        return c;
    }
}
