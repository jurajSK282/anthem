using System;
using UnityEngine;

// Shared health for anything that can be hit (player, goblins, ...).
public class Health : MonoBehaviour
{
    public float maxHealth = 100f;

    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;
    public float Normalized => maxHealth > 0f ? Mathf.Clamp01(Current / maxHealth) : 0f;

    // amount, world position the hit came from, knockback strength
    public event Action<float, Vector3, float> OnDamaged;
    public event Action OnDied;

    bool initialised;

    void Awake()
    {
        Init();
    }

    void Init()
    {
        if (initialised) return;
        initialised = true;
        Current = maxHealth;
    }

    public void TakeDamage(float amount, Vector3 sourcePosition, float knockback = 0f)
    {
        Init();
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);
        OnDamaged?.Invoke(amount, sourcePosition, knockback);
        if (IsDead) OnDied?.Invoke();
    }

    public void ResetHealth()
    {
        initialised = true;
        Current = maxHealth;
    }
}
