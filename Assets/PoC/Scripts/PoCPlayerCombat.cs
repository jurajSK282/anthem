using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PoCPlayerHealth))]
public class PoCPlayerCombat : MonoBehaviour
{
    public Transform cameraTransform;
    public Transform weaponHolder;
    public float damage = 1f;
    public float reach = 1.6f;
    public float hitRadius = 1.4f;
    public float swingDuration = 0.45f;
    public float staminaCost = 1f;
    public float hitStopTime = 0.06f;

    // Keyframes: cas (0-1), posun a rotacia holderu oproti pokojovej polohe.
    private static readonly float[] KeyTime = { 0f, 0.25f, 0.45f, 1f };
    private static readonly Vector3[] KeyPos =
    {
        Vector3.zero, new Vector3(0.1f, 0.1f, -0.1f), new Vector3(-0.4f, -0.15f, 0.25f), Vector3.zero
    };
    private static readonly Vector3[] KeyRot =
    {
        Vector3.zero, new Vector3(-20f, 0f, -40f), new Vector3(50f, 0f, 60f), Vector3.zero
    };

    private static readonly Collider[] hitBuffer = new Collider[16];

    private PoCPlayerHealth health;
    private Vector3 restPos;
    private Quaternion restRot;
    private float swingTime = -1f;
    private bool hitDone;

    void Awake()
    {
        health = GetComponent<PoCPlayerHealth>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (cameraTransform == null) { Debug.LogError($"{name}: chyba cameraTransform.", this); enabled = false; return; }
        if (weaponHolder == null) weaponHolder = cameraTransform.Find("WeaponHolder");
        if (weaponHolder == null) { Debug.LogError($"{name}: chyba weaponHolder.", this); enabled = false; return; }
        restPos = weaponHolder.localPosition;
        restRot = weaponHolder.localRotation;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (swingTime < 0f && mouse.leftButton.wasPressedThisFrame && health.TryUseStamina(staminaCost))
        {
            swingTime = 0f;
            hitDone = false;
        }
        if (swingTime < 0f) return;

        swingTime += Time.deltaTime / swingDuration;
        if (!hitDone && swingTime >= 0.35f)
        {
            hitDone = true;
            DoHit();
        }
        if (swingTime >= 1f)
        {
            swingTime = -1f;
            weaponHolder.localPosition = restPos;
            weaponHolder.localRotation = restRot;
            return;
        }
        ApplyPose(swingTime);
    }

    void ApplyPose(float t)
    {
        int i = 0;
        while (i < KeyTime.Length - 2 && t > KeyTime[i + 1]) i++;
        float k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(KeyTime[i], KeyTime[i + 1], t));
        weaponHolder.localPosition = restPos + Vector3.Lerp(KeyPos[i], KeyPos[i + 1], k);
        weaponHolder.localRotation = restRot * Quaternion.Euler(Vector3.Lerp(KeyRot[i], KeyRot[i + 1], k));
    }

    void DoHit()
    {
        Vector3 origin = cameraTransform.position;
        Vector3 forward = cameraTransform.forward;
        int count = Physics.OverlapSphereNonAlloc(origin + forward * reach, hitRadius, hitBuffer,
            ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider col = hitBuffer[i];
            if (col.transform.root == transform.root) continue;

            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target == null) continue;
            if (Vector3.Dot(forward, (col.bounds.center - origin).normalized) < 0.3f) continue;

            target.TakeDamage(damage, transform.position);
            StartCoroutine(HitStop());
            return;
        }
    }

    IEnumerator HitStop()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopTime);
        Time.timeScale = 1f;
    }
}
