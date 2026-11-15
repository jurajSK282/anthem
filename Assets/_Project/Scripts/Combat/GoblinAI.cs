using UnityEngine;

// Goblin brain: idles until the player gets close, chases, telegraphs a club swing, then recovers.
// Light hits only flash/knock back a little; heavy hits (>= staggerDamage) interrupt the goblin.
// Animation is procedural (arms/legs/body on the "Rig" transforms) so it works with the primitive goblin.
[RequireComponent(typeof(CharacterController), typeof(Health))]
public class GoblinAI : MonoBehaviour
{
    [Header("Senses")]
    public float aggroRange = 14f;
    public float loseRange = 26f;

    [Header("Movement")]
    public float chaseSpeed = 3.3f;
    public float turnSpeed = 420f;
    public float gravity = 25f;

    [Header("Attack")]
    public float attackRange = 1.8f;     // starts a swing inside this distance
    public float attackReach = 2.3f;     // the swing can still land inside this distance
    public float attackDamage = 12f;
    public float windupTime = 0.6f;      // telegraph before the swing: dash (Q) out of reach!
    public float swingTime = 0.14f;
    public float recoverTime = 0.7f;
    public float attackCooldown = 0.5f;

    [Header("Reactions")]
    public float staggerDamage = 30f;    // hits at or above this damage interrupt the goblin
    public float staggerTime = 0.45f;
    public float knockbackDrag = 9f;
    public float corpseTime = 6f;

    [Header("Rig (assigned by CombatSetup)")]
    public Transform model;
    public Transform armR;
    public Transform armL;
    public Transform legR;
    public Transform legL;

    enum State { Idle, Chase, Attack, Stagger, Dead }

    const float ArmRest = -30f;
    const float ArmRaised = 160f;
    const float ArmStrike = -80f;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    State state = State.Idle;
    CharacterController cc;
    Health health;
    Transform target;
    Health targetHealth;

    Vector3 desiredMove;
    Vector3 knockback;
    float vertical;
    float stateTime;
    float cooldown;
    bool strikeDone;
    bool moving;
    float walkCycle;
    float walkBlend;
    float flash;
    float deadTime;
    Vector3 modelBasePos;
    Renderer[] renderers;
    MaterialPropertyBlock block;

    bool TargetAlive => target != null && (targetHealth == null || !targetHealth.IsDead);

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        health = GetComponent<Health>();
        health.OnDamaged += HandleDamaged;
        health.OnDied += HandleDied;
        renderers = GetComponentsInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        if (model != null) modelBasePos = model.localPosition;
    }

    void OnDestroy()
    {
        if (health == null) return;
        health.OnDamaged -= HandleDamaged;
        health.OnDied -= HandleDied;
    }

    void Start()
    {
        FirstPersonMotor motor = FindFirstObjectByType<FirstPersonMotor>();
        if (motor != null)
        {
            target = motor.transform;
            targetHealth = motor.GetComponent<Health>();
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        stateTime += dt;
        cooldown -= dt;
        UpdateFlash(dt);

        desiredMove = Vector3.zero;
        moving = false;

        switch (state)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Chase: UpdateChase(dt); break;
            case State.Attack: UpdateAttack(dt); break;
            case State.Stagger:
                if (stateTime >= staggerTime) SetState(State.Chase);
                break;
            case State.Dead:
                UpdateDead(dt);
                return;
        }

        ApplyMotion(dt);
        Animate(dt);
    }

    // ---------------------------------------------------------------- states

    void UpdateIdle()
    {
        if (TargetAlive && Flat().magnitude < aggroRange) SetState(State.Chase);
    }

    void UpdateChase(float dt)
    {
        if (!TargetAlive) { SetState(State.Idle); return; }

        Vector3 d = Flat();
        float dist = d.magnitude;
        if (dist > loseRange) { SetState(State.Idle); return; }

        Face(d, dt, 1f);

        if (dist <= attackRange && cooldown <= 0f && Vector3.Angle(transform.forward, d) < 35f)
        {
            SetState(State.Attack);
            return;
        }

        if (dist > attackRange * 0.7f)
        {
            desiredMove = transform.forward * chaseSpeed;
            moving = true;
        }
    }

    void UpdateAttack(float dt)
    {
        float t = stateTime;

        if (t < windupTime && TargetAlive) Face(Flat(), dt, 0.5f);   // tracks slowly while winding up

        if (!strikeDone && t >= windupTime)
        {
            strikeDone = true;
            DoStrike();
        }

        if (t >= windupTime + swingTime + recoverTime)
        {
            cooldown = attackCooldown;
            SetState(State.Chase);
        }
    }

    void DoStrike()
    {
        if (!TargetAlive || targetHealth == null) return;
        Vector3 d = Flat();
        if (d.magnitude <= attackReach && Vector3.Angle(transform.forward, d) < 75f)
            targetHealth.TakeDamage(attackDamage, transform.position, 0f);
    }

    void UpdateDead(float dt)
    {
        deadTime += dt;
        if (model == null) return;

        float fall = Mathf.Clamp01(deadTime / 0.45f);
        fall = 1f - (1f - fall) * (1f - fall);
        model.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -90f, fall), 0f, 0f);

        float sink = Mathf.Clamp01((deadTime - (corpseTime - 2.5f)) / 2.5f);
        model.localPosition = modelBasePos + Vector3.down * (sink * 0.6f);
    }

    void SetState(State s)
    {
        state = s;
        stateTime = 0f;
        strikeDone = false;
    }

    // ---------------------------------------------------------------- reactions

    void HandleDamaged(float amount, Vector3 from, float kb)
    {
        flash = 0.15f;

        Vector3 away = transform.position - from;
        away.y = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : -transform.forward;
        knockback += away * kb;

        if (state == State.Dead) return;
        if (state == State.Idle) SetState(State.Chase);
        if (amount >= staggerDamage) SetState(State.Stagger);
    }

    void HandleDied()
    {
        SetState(State.Dead);
        deadTime = 0f;
        cc.enabled = false;   // corpse no longer blocks or gets hit
        Destroy(gameObject, corpseTime);
    }

    // ---------------------------------------------------------------- motion

    Vector3 Flat()
    {
        Vector3 d = target.position - transform.position;
        d.y = 0f;
        return d;
    }

    void Face(Vector3 d, float dt, float turnMultiplier)
    {
        if (d.sqrMagnitude < 0.0001f) return;
        Quaternion want = Quaternion.LookRotation(d.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * turnMultiplier * dt);
    }

    void ApplyMotion(float dt)
    {
        if (cc.isGrounded && vertical < 0f) vertical = -2f;
        vertical -= gravity * dt;
        knockback = Vector3.MoveTowards(knockback, Vector3.zero, knockbackDrag * dt);
        cc.Move((desiredMove + knockback + Vector3.up * vertical) * dt);
    }

    // ---------------------------------------------------------------- procedural animation

    void Animate(float dt)
    {
        walkBlend = Mathf.MoveTowards(walkBlend, moving ? 1f : 0f, 6f * dt);
        walkCycle += dt * 9f * walkBlend;
        float s = Mathf.Sin(walkCycle);

        float armRX = ArmRest + s * 18f * walkBlend;
        float armLX = ArmRest - s * 18f * walkBlend;
        float lean = 6f * walkBlend;

        if (state == State.Attack)
        {
            float t = stateTime;
            if (t < windupTime)
            {
                float p = t / windupTime;
                p = 1f - (1f - p) * (1f - p);
                armRX = Mathf.Lerp(ArmRest, ArmRaised, p);
                lean = Mathf.Lerp(0f, -12f, p);
            }
            else if (t < windupTime + swingTime)
            {
                float p = (t - windupTime) / swingTime;
                p *= p;
                armRX = Mathf.Lerp(ArmRaised, ArmStrike, p);
                lean = Mathf.Lerp(-12f, 18f, p);
            }
            else
            {
                float p = Mathf.Clamp01((t - windupTime - swingTime) / recoverTime);
                p = p * p * (3f - 2f * p);
                armRX = Mathf.Lerp(ArmStrike, ArmRest, p);
                lean = Mathf.Lerp(18f, 0f, p);
            }
        }
        else if (state == State.Stagger)
        {
            armRX = 95f;
            armLX = 70f;
            lean = -22f;
        }

        if (armR != null) armR.localRotation = Quaternion.Euler(armRX, 0f, -8f);
        if (armL != null) armL.localRotation = Quaternion.Euler(armLX, 0f, 8f);
        if (legR != null) legR.localRotation = Quaternion.Euler(s * 35f * walkBlend, 0f, 0f);
        if (legL != null) legL.localRotation = Quaternion.Euler(-s * 35f * walkBlend, 0f, 0f);

        if (model != null)
        {
            model.localPosition = modelBasePos + Vector3.up * (Mathf.Abs(s) * 0.05f * walkBlend);
            model.localRotation = Quaternion.Euler(lean, 0f, s * 3f * walkBlend);
        }
    }

    void UpdateFlash(float dt)
    {
        if (flash <= 0f) return;
        flash -= dt;

        Color hit = new Color(1f, 0.25f, 0.2f);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            if (flash > 0f)
            {
                block.SetColor(BaseColorId, hit);
                renderers[i].SetPropertyBlock(block);
            }
            else
            {
                renderers[i].SetPropertyBlock(null);
            }
        }
    }
}
