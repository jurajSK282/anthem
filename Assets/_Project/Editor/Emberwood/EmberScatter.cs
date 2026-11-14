using UnityEngine;

// Places trees, bushes, glow bulbs and atmosphere particles on the terrain.
public static class EmberScatter
{
    static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    static GameObject Spawn(string name, Transform parent, Mesh mesh, Material[] mats, Vector3 pos, float yaw, Vector3 scale, bool collide)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = mats;
        go.isStatic = true;
        if (collide)
        {
            var c = go.AddComponent<CapsuleCollider>();
            c.radius = 0.55f; c.height = 8f; c.center = Vector3.up * 3f;
        }
        return go;
    }

    static Vector3 Ground(float x, float z) => new Vector3(x, EmberTerrain.Height(x, z), z);

    public static void Trees(Transform root, Mesh[] meshes, Material bark, Material leaves, Material moss)
    {
        var parent = new GameObject("Trees").transform; parent.SetParent(root);
        var r = new System.Random(11);
        var mats = new[] { bark, leaves, moss };
        float lim = EmberTerrain.Half - 4f;
        int placed = 0;
        for (int i = 0; i < 4000 && placed < 230; i++)
        {
            float x = R(r, -lim, lim), z = R(r, -lim, lim);
            float d = EmberTerrain.PathDist(x, z);
            if (d < 4.2f || EmberTerrain.Wet(x, z, 0.4f)) continue;
            // dense wall right beside the path, thinner deep in the forest
            if (d > 14f && r.NextDouble() > 0.45) continue;
            float s = R(r, 0.8f, 1.35f);
            Spawn("Tree", parent, meshes[r.Next(meshes.Length)], mats, Ground(x, z), R(r, 0, 360), new Vector3(s, s * R(r, 0.9f, 1.2f), s), true);
            placed++;
        }
    }

    public static void Bushes(Transform root, Mesh mesh, Material mat)
    {
        var parent = new GameObject("Bushes").transform; parent.SetParent(root);
        var r = new System.Random(23);
        float lim = EmberTerrain.Half - 4f;
        for (int i = 0; i < 520; i++)
        {
            float z = R(r, -lim, lim);
            // most undergrowth hugs the path banks
            float x = r.NextDouble() < 0.7 ? EmberTerrain.PathX(z) + R(r, 3.2f, 11f) * (r.Next(2) * 2 - 1) : R(r, -lim, lim);
            if (EmberTerrain.PathDist(x, z) < 3f || Mathf.Abs(x) > lim) continue;
            float s = R(r, 0.45f, 1.15f);
            Spawn("Bush", parent, mesh, new[] { mat }, Ground(x, z) + Vector3.down * 0.1f, R(r, 0, 360), new Vector3(s, s * R(r, 0.7f, 1.3f), s), false);
        }
    }

    public static void GlowBulbs(Transform root, Mesh mesh, Material mat)
    {
        var parent = new GameObject("GlowBulbs").transform; parent.SetParent(root);
        var r = new System.Random(5);
        float lim = EmberTerrain.Half - 6f;
        for (int cluster = 0; cluster < 46; cluster++)
        {
            float z = R(r, -lim, lim);
            float x = EmberTerrain.PathX(z) + R(r, 2.2f, 13f) * (r.Next(2) * 2 - 1);
            if (EmberTerrain.Wet(x, z)) continue;
            int n = r.Next(1, 5);
            for (int i = 0; i < n; i++)
            {
                float bx = x + R(r, -1.2f, 1.2f), bz = z + R(r, -1.2f, 1.2f);
                float s = R(r, 0.7f, 1.9f);
                Spawn("Bulb", parent, mesh, new[] { mat }, Ground(bx, bz), R(r, 0, 360), Vector3.one * s, false);
            }
            var lg = new GameObject("BulbLight");
            lg.transform.SetParent(parent);
            lg.transform.position = Ground(x, z) + Vector3.up * 0.9f;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.5f, 0.15f);
            l.intensity = 9f; l.range = 7.5f; l.shadows = LightShadows.None;
        }
    }

    public static void Particles(Transform root, Material leaf, Material firefly, Material fog)
    {
        float size = EmberTerrain.Half * 2f - 20f;
        // falling leaves
        var ps = Make("FallingLeaves", root, leaf, new Vector3(0, 18f, 0), new Vector3(size, 6f, size), 70f, 14f, 1200);
        var m = ps.main;
        m.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        m.startSpeed = 0f; m.gravityModifier = 0.03f;
        m.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
        m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.1f), new Color(0.7f, 0.2f, 0.05f));
        Drift(ps, 0.9f, 0.25f);
        var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

        // fireflies / embers near the ground
        ps = Make("Fireflies", root, firefly, new Vector3(0, 4f, 0), new Vector3(size, 9f, size), 45f, 10f, 600);
        m = ps.main;
        m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
        m.startSpeed = 0f; m.startColor = new Color(3f, 1.5f, 0.4f, 1f);
        Drift(ps, 0.5f, 0.35f);
        Fade(ps);

        // big soft haze sheets for depth layering
        ps = Make("GroundHaze", root, fog, new Vector3(0, 2.5f, 0), new Vector3(size, 4f, size), 9f, 22f, 220);
        m = ps.main;
        m.startSize = new ParticleSystem.MinMaxCurve(14f, 28f);
        m.startSpeed = 0f;
        Drift(ps, 0.25f, 0.05f);
        Fade(ps);
        ps.Simulate(20f, true, true); ps.Play();
    }

    static ParticleSystem Make(string name, Transform root, Material mat, Vector3 pos, Vector3 box, float rate, float life, int max)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root);
        go.transform.position = pos + Vector3.up * EmberTerrain.Height(0, 0);
        var ps = go.AddComponent<ParticleSystem>();
        var m = ps.main;
        m.startLifetime = life; m.maxParticles = max; m.prewarm = true; m.loop = true;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        var e = ps.emission; e.rateOverTime = rate;
        var s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = box;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        return ps;
    }

    static void Drift(ParticleSystem ps, float strength, float freq)
    {
        var n = ps.noise; n.enabled = true; n.strength = strength; n.frequency = freq; n.scrollSpeed = 0.2f;
    }

    static void Fade(ParticleSystem ps)
    {
        var c = ps.colorOverLifetime; c.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        c.color = g;
    }
}
