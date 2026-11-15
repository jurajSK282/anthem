using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Builds the player's sword (model + AnimationClips + AnimatorController) and the Goblin prefab.
// Safe to re-run: it rebuilds the sword/animations from scratch and replaces the goblin in the scene.
//   Anthem > Combat > Setup Player Combat
//   Anthem > Combat > Create Goblin
public static class CombatSetup
{
    const string MatDir = "Assets/_Project/Materials";
    const string AnimDir = "Assets/_Project/Animations/Player";
    const string PrefabDir = "Assets/_Project/Prefabs";

    // Sword rest pose in camera space (right hand, low, blade pointing up and forward)
    static readonly Vector3 RestPos = new Vector3(0.40f, -0.42f, 0.55f);
    static readonly Vector3 RestEuler = new Vector3(30f, -5f, 12f);

    struct Pose
    {
        public float t;
        public Vector3 p;
        public Vector3 e;
        public Pose(float t, Vector3 p, Vector3 e) { this.t = t; this.p = p; this.e = e; }
    }

    static Pose P(float t, float px, float py, float pz, float ex, float ey, float ez)
    {
        return new Pose(t, new Vector3(px, py, pz), new Vector3(ex, ey, ez));
    }

    static Pose Rest(float t)
    {
        return new Pose(t, RestPos, RestEuler);
    }

    // ------------------------------------------------------------------ menu

    [MenuItem("Anthem/Combat/Setup Player Combat")]
    public static void SetupPlayerCombat()
    {
        FirstPersonMotor motor = Object.FindFirstObjectByType<FirstPersonMotor>();
        if (motor == null)
        {
            Debug.LogError("[CombatSetup] No FirstPersonMotor found in the open scene.");
            return;
        }

        GameObject player = motor.gameObject;
        Camera cam = player.GetComponentInChildren<Camera>();
        if (cam == null)
        {
            Debug.LogError("[CombatSetup] Player has no child Camera.");
            return;
        }

        EnsureFolder(MatDir);
        EnsureFolder(AnimDir);

        // ---- sword model
        Transform old = cam.transform.Find("SwordHolder");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        Material steel = Mat("Sword_Steel", new Color(0.72f, 0.75f, 0.8f), 0.9f, 0.75f, null);
        Material guard = Mat("Sword_Guard", new Color(0.75f, 0.58f, 0.2f), 0.85f, 0.6f, null);
        Material leather = Mat("Sword_Grip", new Color(0.22f, 0.12f, 0.07f), 0f, 0.3f, null);

        GameObject holder = new GameObject("SwordHolder");
        Undo.RegisterCreatedObjectUndo(holder, "Create Sword");
        holder.transform.SetParent(cam.transform, false);
        holder.transform.localPosition = RestPos;
        holder.transform.localEulerAngles = RestEuler;

        Transform h = holder.transform;
        Prim(PrimitiveType.Cylinder, "Grip", h, new Vector3(0f, 0f, 0f), new Vector3(0.035f, 0.09f, 0.035f), Vector3.zero, leather, false);
        Prim(PrimitiveType.Cube, "Guard", h, new Vector3(0f, 0.105f, 0f), new Vector3(0.22f, 0.028f, 0.05f), Vector3.zero, guard, false);
        Prim(PrimitiveType.Cube, "Blade", h, new Vector3(0f, 0.47f, 0f), new Vector3(0.055f, 0.70f, 0.012f), Vector3.zero, steel, false);
        Prim(PrimitiveType.Cube, "Tip", h, new Vector3(0f, 0.82f, 0f), new Vector3(0.0389f, 0.0389f, 0.012f), new Vector3(0f, 0f, 45f), steel, false);
        Prim(PrimitiveType.Sphere, "Pommel", h, new Vector3(0f, -0.10f, 0f), new Vector3(0.05f, 0.05f, 0.05f), Vector3.zero, guard, false);

        foreach (Renderer r in holder.GetComponentsInChildren<Renderer>())
        {
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // ---- animations
        AnimatorController controller = BuildAnimations();

        Animator animator = holder.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        holder.AddComponent<SwordAnimationEvents>();

        // ---- player components
        Health health = GetOrAdd<Health>(player);
        health.maxHealth = 100f;

        PlayerCombat combat = GetOrAdd<PlayerCombat>(player);
        combat.swordAnimator = animator;
        combat.cameraTransform = cam.transform;

        GetOrAdd<PlayerHUD>(player);

        EditorUtility.SetDirty(health);
        EditorUtility.SetDirty(combat);
        EditorSceneManager.MarkSceneDirty(player.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[CombatSetup] Player combat ready: sword, animations, PlayerCombat, Health, PlayerHUD.");
    }

    [MenuItem("Anthem/Combat/Create Goblin")]
    public static void CreateGoblin()
    {
        EnsureFolder(MatDir);
        EnsureFolder(PrefabDir);

        // Remove any earlier goblin placeholder in the scene
        GameObject oldGoblin = GameObject.Find("Goblin");
        while (oldGoblin != null)
        {
            Undo.DestroyObjectImmediate(oldGoblin);
            oldGoblin = GameObject.Find("Goblin");
        }

        Material skin = Mat("Goblin", new Color(0.35f, 0.6f, 0.22f), 0f, 0.25f, null);
        Material dark = Mat("Goblin_Dark", new Color(0.22f, 0.38f, 0.14f), 0f, 0.2f, null);
        Material eyes = Mat("Goblin_Eyes", new Color(1f, 0.85f, 0.2f), 0f, 0.5f, new Color(2.5f, 1.8f, 0.2f));
        Material wood = Mat("Goblin_Wood", new Color(0.32f, 0.19f, 0.1f), 0f, 0.15f, null);

        GameObject root = new GameObject("Goblin");
        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 1.4f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.7f, 0f);
        cc.stepOffset = 0.3f;
        cc.slopeLimit = 50f;

        Health health = root.AddComponent<Health>();
        health.maxHealth = 60f;
        GoblinAI ai = root.AddComponent<GoblinAI>();

        Transform model = new GameObject("Model").transform;
        model.SetParent(root.transform, false);

        Prim(PrimitiveType.Capsule, "Body", model, new Vector3(0f, 0.62f, 0f), new Vector3(0.7f, 0.5f, 0.7f), Vector3.zero, skin, false);
        Prim(PrimitiveType.Sphere, "Head", model, new Vector3(0f, 1.2f, 0f), new Vector3(0.5f, 0.45f, 0.5f), Vector3.zero, skin, false);
        Prim(PrimitiveType.Cube, "Ear_L", model, new Vector3(-0.36f, 1.25f, 0f), new Vector3(0.34f, 0.07f, 0.16f), new Vector3(0f, 0f, -20f), skin, false);
        Prim(PrimitiveType.Cube, "Ear_R", model, new Vector3(0.36f, 1.25f, 0f), new Vector3(0.34f, 0.07f, 0.16f), new Vector3(0f, 0f, 20f), skin, false);
        Prim(PrimitiveType.Sphere, "Eye_L", model, new Vector3(-0.1f, 1.24f, 0.21f), new Vector3(0.09f, 0.09f, 0.09f), Vector3.zero, eyes, false);
        Prim(PrimitiveType.Sphere, "Eye_R", model, new Vector3(0.1f, 1.24f, 0.21f), new Vector3(0.09f, 0.09f, 0.09f), Vector3.zero, eyes, false);
        Prim(PrimitiveType.Sphere, "Nose", model, new Vector3(0f, 1.17f, 0.235f), new Vector3(0.09f, 0.1f, 0.1f), Vector3.zero, dark, false);

        Transform legL = Pivot("LegL", model, new Vector3(-0.14f, 0.4f, 0f));
        Prim(PrimitiveType.Capsule, "Leg", legL, new Vector3(0f, -0.2f, 0f), new Vector3(0.16f, 0.2f, 0.16f), Vector3.zero, dark, false);
        Transform legR = Pivot("LegR", model, new Vector3(0.14f, 0.4f, 0f));
        Prim(PrimitiveType.Capsule, "Leg", legR, new Vector3(0f, -0.2f, 0f), new Vector3(0.16f, 0.2f, 0.16f), Vector3.zero, dark, false);

        Transform armL = Pivot("ArmL", model, new Vector3(-0.4f, 0.98f, 0f));
        Prim(PrimitiveType.Capsule, "Arm", armL, new Vector3(0f, -0.2f, 0f), new Vector3(0.13f, 0.2f, 0.13f), Vector3.zero, skin, false);
        Transform armR = Pivot("ArmR", model, new Vector3(0.4f, 0.98f, 0f));
        Prim(PrimitiveType.Capsule, "Arm", armR, new Vector3(0f, -0.2f, 0f), new Vector3(0.13f, 0.2f, 0.13f), Vector3.zero, skin, false);
        Prim(PrimitiveType.Cylinder, "ClubHandle", armR, new Vector3(0f, -0.6f, 0f), new Vector3(0.07f, 0.2f, 0.07f), Vector3.zero, wood, false);
        Prim(PrimitiveType.Sphere, "ClubHead", armR, new Vector3(0f, -0.82f, 0f), new Vector3(0.2f, 0.2f, 0.2f), Vector3.zero, wood, false);

        ai.model = model;
        ai.armR = armR;
        ai.armL = armL;
        ai.legR = legR;
        ai.legL = legL;

        string prefabPath = PrefabDir + "/Goblin.prefab";
        // Save at the origin first so the prefab asset stays clean. This returns the prefab ASSET root;
        // 'root' itself becomes the connected scene instance, which is what we place.
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);

        PlaceInFrontOfPlayer(root, 10f);
        Undo.RegisterCreatedObjectUndo(root, "Create Goblin");
        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(root.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[CombatSetup] Goblin prefab saved to " + prefabPath + " and placed in the scene.");
    }

    // ------------------------------------------------------------------ animations

    static AnimatorController BuildAnimations()
    {
        // Idle: gentle breathing sway, loops
        AnimationClip idle = BuildClip("Sword_Idle", true, new Pose[]
        {
            Rest(0f),
            P(1.5f, RestPos.x, RestPos.y + 0.010f, RestPos.z, RestEuler.x + 1.5f, RestEuler.y, RestEuler.z + 0.6f),
            Rest(3f),
        });

        // Light 1: raised on the right, slash down and across to the left
        AnimationClip light1 = BuildClip("Sword_Light1", false, new Pose[]
        {
            Rest(0f),
            P(0.12f, 0.55f, -0.15f, 0.40f, 10f, 15f, -35f),     // wind-up, blade up and out to the right
            P(0.24f, 0.05f, -0.35f, 0.65f, 80f, -10f, 40f),     // impact, blade sweeping across the centre
            P(0.36f, -0.35f, -0.45f, 0.50f, 85f, -30f, 70f),    // follow-through to the left
            Rest(0.55f),
        }, Hit(0.24f, "Light"));

        // Light 2: back-slash from the left to the right
        AnimationClip light2 = BuildClip("Sword_Light2", false, new Pose[]
        {
            Rest(0f),
            P(0.10f, -0.30f, -0.42f, 0.50f, 85f, -30f, 65f),    // wind-up low on the left
            P(0.22f, 0.10f, -0.35f, 0.68f, 80f, 10f, -35f),     // impact
            P(0.34f, 0.60f, -0.35f, 0.50f, 80f, 30f, -60f),     // follow-through to the right
            Rest(0.52f),
        }, Hit(0.22f, "Light"));

        // Heavy: raise over the shoulder, hold, overhead chop
        AnimationClip heavy = BuildClip("Sword_Heavy", false, new Pose[]
        {
            Rest(0f),
            P(0.18f, 0.45f, -0.20f, 0.35f, -10f, 0f, 10f),
            P(0.36f, 0.40f, 0.10f, 0.25f, -50f, 0f, 8f),        // blade back over the shoulder
            P(0.45f, 0.40f, 0.12f, 0.25f, -55f, 0f, 8f),        // hold (telegraph)
            P(0.52f, 0.10f, -0.45f, 0.75f, 100f, 0f, 0f),       // impact
            P(0.70f, 0.05f, -0.55f, 0.70f, 110f, 0f, 0f),
            Rest(1.00f),
        }, Hit(0.52f, "Heavy"));

        // Controller: Idle <-> attack states driven by triggers
        string path = AnimDir + "/SwordAnimator.controller";
        AssetDatabase.DeleteAsset(path);
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        ctrl.AddParameter("Light1", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Light2", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Heavy", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState idleState = sm.AddState("Idle");
        idleState.motion = idle;
        sm.defaultState = idleState;

        AddAttackState(sm, idleState, "Light1", light1);
        AddAttackState(sm, idleState, "Light2", light2);
        AddAttackState(sm, idleState, "Heavy", heavy);

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        return ctrl;
    }

    static void AddAttackState(AnimatorStateMachine sm, AnimatorState idle, string name, AnimationClip clip)
    {
        AnimatorState st = sm.AddState(name);
        st.motion = clip;

        AnimatorStateTransition any = sm.AddAnyStateTransition(st);
        any.AddCondition(AnimatorConditionMode.If, 0f, name);
        any.hasExitTime = false;
        any.hasFixedDuration = true;
        any.duration = 0.05f;
        any.canTransitionToSelf = true;

        AnimatorStateTransition back = st.AddTransition(idle);
        back.hasExitTime = true;
        back.exitTime = 0.9f;
        back.hasFixedDuration = true;
        back.duration = 0.1f;
    }

    static AnimationEvent Hit(float time, string kind)
    {
        AnimationEvent e = new AnimationEvent();
        e.time = time;
        e.functionName = "OnSwordHit";
        e.stringParameter = kind;
        return e;
    }

    static AnimationClip BuildClip(string name, bool loop, Pose[] poses, params AnimationEvent[] events)
    {
        AnimationClip clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = 60f;

        string[] pn = { "localPosition.x", "localPosition.y", "localPosition.z" };
        string[] en = { "localEulerAnglesRaw.x", "localEulerAnglesRaw.y", "localEulerAnglesRaw.z" };

        for (int a = 0; a < 3; a++)
        {
            AnimationCurve pc = new AnimationCurve();
            AnimationCurve ec = new AnimationCurve();
            foreach (Pose k in poses)
            {
                pc.AddKey(k.t, k.p[a]);
                ec.AddKey(k.t, k.e[a]);
            }
            for (int i = 0; i < pc.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(pc, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(pc, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyLeftTangentMode(ec, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(ec, i, AnimationUtility.TangentMode.ClampedAuto);
            }
            clip.SetCurve("", typeof(Transform), pn[a], pc);
            clip.SetCurve("", typeof(Transform), en[a], ec);
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (events != null && events.Length > 0) AnimationUtility.SetAnimationEvents(clip, events);

        string path = AnimDir + "/" + name + ".anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // ------------------------------------------------------------------ helpers

    static void PlaceInFrontOfPlayer(GameObject go, float distance)
    {
        FirstPersonMotor motor = Object.FindFirstObjectByType<FirstPersonMotor>();
        if (motor == null) return;

        Transform p = motor.transform;
        Vector3 fwd = p.forward;
        fwd.y = 0f;
        fwd.Normalize();
        Vector3 pos = p.position + fwd * distance;

        RaycastHit[] hits = Physics.RaycastAll(pos + Vector3.up * 100f, Vector3.down, 300f, ~0, QueryTriggerInteraction.Ignore);
        bool found = false;
        float y = pos.y;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider is TerrainCollider) { y = hit.point.y; found = true; break; }
        }
        if (!found && hits.Length > 0) y = hits[0].point.y;
        pos.y = y + 0.05f;

        go.transform.position = pos;
        Vector3 toPlayer = p.position - pos;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(toPlayer);
    }

    static Transform Pivot(string name, Transform parent, Vector3 localPos)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        return t;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Vector3 euler, Material mat, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = scale;
        if (!keepCollider)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }

    static Material Mat(string name, Color color, float metallic, float smoothness, Color? emission)
    {
        string path = MatDir + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
