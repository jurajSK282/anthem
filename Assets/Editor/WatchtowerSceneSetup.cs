using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tools > Watchtower > Add To Open Scene
// Instantiates Watchtower.fbx at the origin of the active scene and makes sure
// its materials sample the shared 256px atlas.
public static class WatchtowerSceneSetup
{
    const string FbxPath = "Assets/Models/Watchtower/Watchtower.fbx";
    const string AtlasPath = "Assets/Models/Watchtower/Watchtower_Atlas.png";

    [MenuItem("Tools/Watchtower/Add To Open Scene")]
    static void AddToScene()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (model == null)
        {
            Debug.LogError($"Watchtower: no model at {FbxPath}");
            return;
        }

        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, SceneManager.GetActiveScene());
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.name = "Watchtower";
        Undo.RegisterCreatedObjectUndo(instance, "Add Watchtower");

        if (atlas != null)
        {
            foreach (var r in instance.GetComponentsInChildren<Renderer>())
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") == null)
                        m.SetTexture("_BaseMap", atlas);
                    if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") == null)
                        m.SetTexture("_MainTex", atlas);
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = instance;
        Debug.Log("Watchtower added to the open scene. Save the scene (Ctrl+S).");
    }
}
