using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Menu: Anthem -> Setup URP. Creates the URP asset + renderer if the project has none assigned.
public static class AnthemPipelineSetup
{
    const string Dir = "Assets/_Project/Settings";

    [MenuItem("Anthem/Setup URP")]
    public static void Setup()
    {
        Directory.CreateDirectory(Dir);
        string assetPath = $"{Dir}/URP_Anthem.asset";
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
        if (asset == null)
        {
            MethodInfo create = typeof(UniversalRenderPipelineAsset).GetMethod("CreateRendererAsset",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            var data = (ScriptableRendererData)create.Invoke(null,
                new object[] { $"{Dir}/URP_Anthem_Renderer.asset", RendererType.UniversalRenderer, false, "Renderer" });
            asset = UniversalRenderPipelineAsset.Create(data);
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        asset.supportsHDR = true;
        asset.shadowDistance = 70f;
        asset.shadowCascadeCount = 2;
        asset.msaaSampleCount = 1;
        asset.maxAdditionalLightsCount = 8;
        var so = new SerializedObject(asset);
        Set(so, "m_MainLightShadowmapResolution", 2048);
        Set(so, "m_SoftShadowsSupported", 1);
        Set(so, "m_AdditionalLightsRenderingMode", 1); // per-pixel
        so.ApplyModifiedPropertiesWithoutUndo();

        // Forward+ so dozens of small glow lights are cheap.
        var rso = new SerializedObject(so.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue);
        Set(rso, "m_RenderingMode", (int)RenderingMode.ForwardPlus);
        rso.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(asset);
        GraphicsSettings.defaultRenderPipeline = asset;
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = asset;
        }
        QualitySettings.SetQualityLevel(current, true);
        AssetDatabase.SaveAssets();
        Debug.Log("Anthem: URP assigned -> " + assetPath);
    }

    static void Set(SerializedObject so, string prop, int value)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null) { Debug.LogWarning("Anthem: URP property not found: " + prop); return; }
        if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = value != 0; else p.intValue = value;
    }
}
