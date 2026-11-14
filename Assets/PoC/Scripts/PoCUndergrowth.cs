using UnityEngine;

public class PoCUndergrowth : MonoBehaviour
{
    public Material bushMaterial;
    public Material flowerMaterial;
    public int bushCount = 220;
    public int flowerClusters = 14;
    public int flowersPerCluster = 7;
    public float radius = 48f;
    public int seed = 7;

    void Start()
    {
        if (bushMaterial == null || flowerMaterial == null)
        {
            Debug.LogError($"{name}: chyba bushMaterial alebo flowerMaterial.", this);
            return;
        }

        flowerMaterial.EnableKeyword("_EMISSION");
        flowerMaterial.SetColor("_EmissionColor", new Color(1f, 0.1f, 0.05f) * 2.5f);

        var rng = new System.Random(seed);
        var root = new GameObject("Undergrowth").transform;

        for (int i = 0; i < bushCount; i++)
        {
            float w = Range(rng, 0.9f, 2.0f), h = Range(rng, 0.6f, 1.3f);
            Spawn(root, bushMaterial, RandomPoint(rng, 0f), new Vector3(w, h, w), h * 0.3f, rng);
        }

        for (int c = 0; c < flowerClusters; c++)
        {
            Vector3 center = RandomPoint(rng, 0f);
            for (int f = 0; f < flowersPerCluster; f++)
            {
                Vector3 p = center + new Vector3(Range(rng, -1.5f, 1.5f), 0f, Range(rng, -1.5f, 1.5f));
                Spawn(root, flowerMaterial, p, Vector3.one * 0.12f, Range(rng, 0.3f, 0.7f), rng);
            }
        }
    }

    Vector3 RandomPoint(System.Random rng, float y)
    {
        float angle = Range(rng, 0f, Mathf.PI * 2f);
        float dist = Mathf.Sqrt((float)rng.NextDouble()) * radius;
        return new Vector3(Mathf.Cos(angle) * dist, y, Mathf.Sin(angle) * dist);
    }

    static void Spawn(Transform parent, Material mat, Vector3 pos, Vector3 scale, float height, System.Random rng)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, height, pos.z);
        go.transform.localScale = scale;
        go.transform.rotation = Quaternion.Euler(0f, Range(rng, 0f, 360f), 0f);

        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    static float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}
