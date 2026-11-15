using UnityEngine;

// Minimal combat HUD: health bar, red damage flash, crosshair dot, death screen + respawn.
[RequireComponent(typeof(Health))]
public class PlayerHUD : MonoBehaviour
{
    public float respawnDelay = 3f;
    public bool showCrosshair = true;

    Health health;
    FirstPersonMotor motor;
    PlayerCombat combat;
    CharacterController cc;
    Vector3 spawnPos;
    Quaternion spawnRot;
    float damageFlash;
    float deadTimer;
    float shownHealth = 1f;
    bool dead;
    GUIStyle bigStyle;

    void Awake()
    {
        health = GetComponent<Health>();
        motor = GetComponent<FirstPersonMotor>();
        combat = GetComponent<PlayerCombat>();
        cc = GetComponent<CharacterController>();
        spawnPos = transform.position;
        spawnRot = transform.rotation;
    }

    void OnEnable()
    {
        health.OnDamaged += HandleDamaged;
        health.OnDied += HandleDied;
    }

    void OnDisable()
    {
        health.OnDamaged -= HandleDamaged;
        health.OnDied -= HandleDied;
    }

    void HandleDamaged(float amount, Vector3 from, float knockback)
    {
        damageFlash = Mathf.Clamp01(0.35f + amount / 40f);
    }

    void HandleDied()
    {
        dead = true;
        deadTimer = respawnDelay;
        if (motor != null) motor.enabled = false;
        if (combat != null) combat.enabled = false;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        damageFlash = Mathf.MoveTowards(damageFlash, 0f, dt * 1.5f);
        shownHealth = Mathf.MoveTowards(shownHealth, health.Normalized, dt * 1.2f);

        if (!dead) return;
        deadTimer -= dt;
        if (deadTimer <= 0f) Respawn();
    }

    void Respawn()
    {
        if (cc != null) cc.enabled = false;
        transform.SetPositionAndRotation(spawnPos, spawnRot);
        if (cc != null) cc.enabled = true;

        health.ResetHealth();
        dead = false;
        damageFlash = 0f;
        if (motor != null) motor.enabled = true;
        if (combat != null) combat.enabled = true;
    }

    void OnGUI()
    {
        float w = Screen.width;
        float h = Screen.height;
        Texture2D white = Texture2D.whiteTexture;

        if (damageFlash > 0f)
        {
            GUI.color = new Color(0.8f, 0f, 0f, damageFlash * 0.45f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), white);
        }

        float bw = Mathf.Min(320f, w * 0.3f);
        float bh = 16f;
        float x = 24f;
        float y = h - 40f;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(x - 3f, y - 3f, bw + 6f, bh + 6f), white);
        GUI.color = new Color(0.3f, 0.05f, 0.05f, 1f);
        GUI.DrawTexture(new Rect(x, y, bw, bh), white);
        GUI.color = new Color(0.85f, 0.2f, 0.15f, 1f);
        GUI.DrawTexture(new Rect(x, y, bw * shownHealth, bh), white);

        if (showCrosshair && !dead)
        {
            GUI.color = new Color(1f, 1f, 1f, 0.7f);
            GUI.DrawTexture(new Rect(w * 0.5f - 2f, h * 0.5f - 2f, 4f, 4f), white);
        }

        if (dead)
        {
            if (bigStyle == null)
            {
                bigStyle = new GUIStyle(GUI.skin.label);
                bigStyle.fontSize = 64;
                bigStyle.fontStyle = FontStyle.Bold;
                bigStyle.alignment = TextAnchor.MiddleCenter;
            }
            GUI.color = new Color(0.9f, 0.15f, 0.1f, 1f);
            GUI.Label(new Rect(0f, 0f, w, h), "YOU DIED", bigStyle);
        }

        GUI.color = Color.white;
    }
}
