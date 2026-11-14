using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Menu: PoC -> ... Vyžaduje balík AI Navigation.
public static class PoCSetup
{
    const string GnollFolder = "Assets/PoC/gnoll";
    const string GnollModel = "Assets/PoC/gnoll/gnoll.fbx";
    const string MaterialFolder = "Assets/PoC/Materials";

    [MenuItem("PoC/1. Gnoll anims -> Humanoid")]
    public static void SetupHumanoid()
    {
        // Každý FBX dostane vlastný Humanoid Avatar, retarget medzi nimi funguje
        var modelImporter = (ModelImporter)AssetImporter.GetAtPath(GnollModel);
        if (modelImporter == null)
        {
            Debug.LogError($"PoC: nenájdený {GnollModel}");
            return;
        }
        modelImporter.animationType = ModelImporterAnimationType.Human;
        modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        modelImporter.SaveAndReimport();

        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { GnollFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == GnollModel) continue;

            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            count++;
        }
        Debug.Log($"PoC: Humanoid nastavený na model + {count} animácií.");
    }

    [MenuItem("PoC/2. Build forest + NavMesh")]
    public static void BuildForest()
    {
        GameObject old = GameObject.Find("Forest");
        if (old != null) Object.DestroyImmediate(old);

        Material groundMat = CreateMaterial("M_PoC_Ground", new Color(0.18f, 0.24f, 0.16f));
        Material treeMat = CreateMaterial("M_PoC_Tree", new Color(0.25f, 0.17f, 0.10f));

        var root = new GameObject("Forest");
        Undo.RegisterCreatedObjectUndo(root, "Build forest");

        // Podlaha 100x100 (Plane je 10x10, scale 10)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform);
        ground.transform.localScale = new Vector3(10f, 1f, 10f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

        // Stromy (valce) náhodne, stred (spawn hráča) necháme voľný
        Random.InitState(42);
        for (int i = 0; i < 25; i++)
        {
            Vector2 p = Random.insideUnitCircle * 40f;
            if (p.magnitude < 8f) continue;

            var tree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tree.name = "Tree_" + i;
            tree.transform.SetParent(root.transform);
            tree.transform.localScale = new Vector3(0.8f, 6f, 0.8f);
            // Cylinder je 2 jednotky vysoký; pri scale Y=6 má výšku 12, stred je na Y=6
            tree.transform.position = new Vector3(p.x, 6f, p.y);
            tree.GetComponent<MeshRenderer>().sharedMaterial = treeMat;
        }

        // Hmla
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.04f;
        RenderSettings.fogColor = new Color(0.10f, 0.13f, 0.14f);

        // Directional Light: chladnejšie a tmavšie
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
        {
            if (light.type != LightType.Directional) continue;
            light.intensity = 0.5f;
            light.color = new Color(0.7f, 0.8f, 1f);
        }

        // NavMesh na podlahe
        var surface = ground.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.BuildNavMesh();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("PoC: les vytvorený, NavMesh zapečený.");
    }

    static Material CreateMaterial(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets/PoC", "Materials");

        string path = $"{MaterialFolder}/{name}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            AssetDatabase.DeleteAsset(path); // vždy pregeneruj (opraví ružový materiál po zmene pipeline)

        // Použije URP Lit ak je aktívny URP, inak Built-in Standard (aby materiál nikdy nebol ružový)
        Shader shader = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
            ? Shader.Find("Universal Render Pipeline/Lit")
            : Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
