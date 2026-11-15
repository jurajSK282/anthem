using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Sword combat: Left click = light attack (2-hit combo), Right click = heavy attack.
// The swings are real AnimationClips played by an Animator on the SwordHolder
// (Assets/_Project/Animations/Player). Damage lands on the "OnSwordHit" animation event,
// so it lines up with the visual swing. Tweak damage / range / timings below.
public class PlayerCombat : MonoBehaviour
{
    [System.Serializable]
    public class AttackData
    {
        public string trigger = "Light1";
        public float damage = 20f;
        public float range = 2.6f;       // metres from the camera
        public float arc = 110f;         // total cone angle in front of the camera
        public float knockback = 4f;
        public float lockTime = 0.5f;    // time before the next attack can start
        public float hitStop = 0.045f;   // brief freeze on impact (real seconds)
        public Vector3 camKick = new Vector3(0.4f, 0f, 1.2f);   // camera pitch / yaw / roll on hit (degrees)
    }

    [Header("References")]
    public Animator swordAnimator;
    public Transform cameraTransform;

    [Header("Attacks")]
    public AttackData[] lightCombo = new AttackData[]
    {
        new AttackData { trigger = "Light1", camKick = new Vector3(0.4f, 0f, 1.2f) },
        new AttackData { trigger = "Light2", camKick = new Vector3(0.4f, 0f, -1.2f), lockTime = 0.48f },
    };

    public AttackData heavy = new AttackData
    {
        trigger = "Heavy",
        damage = 45f,
        range = 3.0f,
        arc = 130f,
        knockback = 9f,
        lockTime = 0.95f,
        hitStop = 0.09f,
        camKick = new Vector3(2.2f, 0f, 0f),
    };

    [Header("Feel")]
    public float comboResetTime = 0.8f;   // pause longer than this and the combo restarts
    public float inputBuffer = 0.3f;      // click slightly early and the attack still happens

    public bool debugLogs = false;

    enum Kind { None, Light, Heavy }

    static readonly string[] TriggerNames = { "Light1", "Light2", "Heavy" };

    public bool IsAttacking => lockTimer > 0f;

    Health ownHealth;
    AttackData current;
    float lockTimer;
    float lastAttackEnd = -10f;
    int comboIndex;
    Kind buffered = Kind.None;
    float bufferTimer;
    bool hitDone;
    Vector3 kick;
    Coroutine hitStopRoutine;
    readonly HashSet<Health> hitThisSwing = new HashSet<Health>();

    void Awake()
    {
        ownHealth = GetComponent<Health>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (swordAnimator == null) swordAnimator = GetComponentInChildren<Animator>();
    }

    void OnDisable()
    {
        if (hitStopRoutine != null)
        {
            StopCoroutine(hitStopRoutine);
            hitStopRoutine = null;
        }
        Time.timeScale = 1f;
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        if (ownHealth != null && ownHealth.IsDead) return;

        float dt = Time.deltaTime;

        if (lockTimer > 0f)
        {
            lockTimer -= dt;
            if (lockTimer <= 0f) lastAttackEnd = Time.time;
        }

        if (bufferTimer > 0f)
        {
            bufferTimer -= dt;
            if (bufferTimer <= 0f) buffered = Kind.None;
        }

        // First click after the cursor was released just re-captures it instead of swinging.
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame) { buffered = Kind.Light; bufferTimer = inputBuffer; }
        if (mouse.rightButton.wasPressedThisFrame) { buffered = Kind.Heavy; bufferTimer = inputBuffer; }

        if (buffered != Kind.None && lockTimer <= 0f)
        {
            StartAttack(buffered);
            buffered = Kind.None;
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Small camera shake on hit. FirstPersonMotor sets the camera rotation absolutely each frame,
        // so adding the offset here every frame never accumulates.
        kick = Vector3.Lerp(kick, Vector3.zero, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        if (kick.sqrMagnitude > 0.0001f)
            cameraTransform.localRotation *= Quaternion.Euler(kick.x, kick.y, kick.z);
    }

    // Public so other scripts / tests can trigger attacks.
    public void LightAttack() { if (lockTimer <= 0f) StartAttack(Kind.Light); }
    public void HeavyAttack() { if (lockTimer <= 0f) StartAttack(Kind.Heavy); }

    void StartAttack(Kind kind)
    {
        AttackData a;
        if (kind == Kind.Heavy)
        {
            a = heavy;
            comboIndex = 0;
        }
        else
        {
            if (Time.time - lastAttackEnd > comboResetTime) comboIndex = 0;
            a = lightCombo[comboIndex % lightCombo.Length];
            comboIndex = (comboIndex + 1) % lightCombo.Length;
        }

        current = a;
        hitDone = false;
        hitThisSwing.Clear();
        lockTimer = a.lockTime;

        if (swordAnimator != null)
        {
            for (int i = 0; i < TriggerNames.Length; i++) swordAnimator.ResetTrigger(TriggerNames[i]);
            swordAnimator.SetTrigger(a.trigger);
        }

        if (debugLogs) Debug.Log("[PlayerCombat] Start " + a.trigger);
    }

    // Called by SwordAnimationEvents when the swing animation reaches its impact frame.
    public void OnSwordHit(string kind)
    {
        if (current == null || hitDone) return;
        hitDone = true;
        DealDamage(current);
    }

    void DealDamage(AttackData a)
    {
        if (cameraTransform == null) return;

        Vector3 origin = cameraTransform.position;
        Vector3 forward = cameraTransform.forward;
        bool landed = false;

        Collider[] cols = Physics.OverlapSphere(origin, a.range, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider c in cols)
        {
            Health h = c.GetComponentInParent<Health>();
            if (h == null || h == ownHealth || h.IsDead || hitThisSwing.Contains(h)) continue;

            Vector3 center = c.bounds.center;
            if (Vector3.Angle(forward, center - origin) > a.arc * 0.5f) continue;
            if (!HasLineOfSight(origin, center, h)) continue;

            hitThisSwing.Add(h);
            h.TakeDamage(a.damage, transform.position, a.knockback);
            landed = true;

            if (debugLogs) Debug.Log("[PlayerCombat] " + a.trigger + " hit " + h.name + " for " + a.damage + " (" + h.Current + " left)");
        }

        if (!landed)
        {
            if (debugLogs) Debug.Log("[PlayerCombat] " + a.trigger + " hit nothing");
            return;
        }

        kick += a.camKick;
        if (a.hitStop > 0f && hitStopRoutine == null) hitStopRoutine = StartCoroutine(HitStop(a.hitStop));
    }

    bool HasLineOfSight(Vector3 from, Vector3 to, Health target)
    {
        Vector3 d = to - from;
        RaycastHit[] hits = Physics.RaycastAll(from, d.normalized, d.magnitude, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            Health other = hit.collider.GetComponentInParent<Health>();
            if (other != null) continue;   // creatures never block each other
            return false;                  // terrain, trees, rocks ...
        }
        return true;
    }

    IEnumerator HitStop(float seconds)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(seconds);
        Time.timeScale = 1f;
        hitStopRoutine = null;
    }
}
