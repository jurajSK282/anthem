using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Menu: Anthem -> Build Emberwood. Regenerates the whole first-area blockout (safe to re-run).
public static class EmberwoodBuilder
{
    const string ScenePath = "Assets/_Project/Scenes/Emberwood.unity";

    [MenuItem("Anthem/Build Emberwood")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty) { Debug.LogError("Emberwood: save or discard the open scene first."); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        foreach (GameObject go in scene.GetRootGameObjects()) Object.DestroyImmediate(go);
        Directory.CreateDirectory(EmberTextures.Dir);

        var root = new GameObject("Emberwood").transform;
        EmberAtmosphere.Build(root);

        // --- placeholder textures (replace PNGs with Midjourney tiles, keep names) ---
        Color orange = new Color(1.0f, 0.55f, 0.14f), red = new Color(0.80f, 0.30f, 0.08f);
        var tLeafGround = EmberTextures.Leaves("T_GroundLeaves", 128, 420, new Color(0.30f, 0.15f, 0.06f), orange, red, false, 1);
        var tFloor = EmberTextures.Leaves("T_ForestFloor", 128, 110, new Color(0.09f, 0.055f, 0.035f), orange * 0.6f, red * 0.5f, false, 2);
        var tSoil = EmberTextures.Bark("T_Soil", 64, new Color(0.05f, 0.035f, 0.025f), new Color(0.16f, 0.10f, 0.06f), 3);
        var tBark = EmberTextures.Bark("T_Bark", 64, new Color(0.02f, 0.015f, 0.012f), new Color(0.13f, 0.08f, 0.05f), 4);
        var tCanopy = EmberTextures.Leaves("T_LeafCard", 64, 110, Color.clear, orange, red, true, 5);
        var tDot = EmberTextures.SoftDot("T_SoftDot", 32);

        var mats = new[]
        {
            EmberTextures.Lit("M_GroundLeaves", tLeafGround, Color.white),
            EmberTextures.Lit("M_ForestFloor", tFloor, Color.white),
            EmberTextures.Lit("M_Soil", tSoil, Color.white),
        };
        var bark = EmberTextures.Lit("M_Bark", tBark, Color.white);
        var canopy = EmberTextures.Lit("M_Canopy", tCanopy, new Color(1f, 0.8f, 0.65f), 1f, true);
        var bush = EmberTextures.Lit("M_Bush", tCanopy, new Color(0.30f, 0.14f, 0.09f), 1f, true);
        var glow = EmberTextures.Glow("M_GlowBulb", new Color(1f, 0.5f, 0.15f), new Color(6f, 2.2f, 0.4f));
        var pLeaf = EmberTextures.Particle("M_P_Leaf", tCanopy, Color.white, false);
        var pFly = EmberTextures.Particle("M_P_Firefly", tDot, Color.white, true);
        var pFog = EmberTextures.Particle("M_P_Haze", tDot, new Color(0.55f, 0.28f, 0.10f, 0.10f), false);

        EmberTerrain.Build(root, mats);

        var trees = new Mesh[6];
        for (int i = 0; i < trees.Length; i++)
            trees[i] = EmberMeshes.Tree(100 + i, 13f + i * 2.2f, 0.45f + i * 0.09f, i % 3 == 0 ? 0.3f : 1f);
        EmberScatter.Trees(root, trees, bark, canopy);
        EmberScatter.Bushes(root, EmberMeshes.Bush(), bush);
        EmberScatter.GlowBulbs(root, EmberMeshes.Bulb(), glow);
        EmberScatter.Particles(root, pLeaf, pFly, pFog);

        BuildPlayer();

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Emberwood: built.");
    }

    static void BuildPlayer()
    {
        const float z = -72f;
        float x = EmberTerrain.PathX(z);
        var player = new GameObject("Player");
        player.transform.position = new Vector3(x, EmberTerrain.Height(x, z) + 1.2f, z);
        // face along the path, into the light
        player.transform.rotation = Quaternion.LookRotation(new Vector3(EmberTerrain.PathX(z + 12f) - x, 0f, 12f));
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.35f; cc.center = Vector3.up * 0.9f; cc.slopeLimit = 50f; cc.stepOffset = 0.4f;

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        camGo.transform.SetParent(player.transform, false);
        camGo.transform.localPosition = Vector3.up * 1.65f;
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 68f; cam.nearClipPlane = 0.08f; cam.farClipPlane = 260f;
        cam.allowHDR = true;
        camGo.AddComponent<AudioListener>();
        var data = camGo.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;

        player.AddComponent<FirstPersonMotor>().cameraTransform = camGo.transform;
    }
}
