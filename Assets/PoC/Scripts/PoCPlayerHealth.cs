using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PoCPlayerHealth : MonoBehaviour, IDamageable
{
    public float maxHealth = 4f;
    public float maxStamina = 4f;
    public float staminaRegenPerSecond = 0.8f;
    public float respawnDelay = 1.5f;

    public float Health { get; private set; }
    public float Stamina { get; private set; }
    public bool IsDead { get; private set; }

    private CharacterController controller;
    private Vector3 spawnPosition;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        spawnPosition = transform.position;
        Health = maxHealth;
        Stamina = maxStamina;
    }

    void Update()
    {
        if (!IsDead)
            Stamina = Mathf.Min(maxStamina, Stamina + staminaRegenPerSecond * Time.deltaTime);
    }

    public bool TryUseStamina(float amount)
    {
        if (IsDead || Stamina < amount) return false;
        Stamina -= amount;
        return true;
    }

    public void TakeDamage(float amount, Vector3 sourcePosition)
    {
        if (IsDead) return;
        Health -= amount;
        if (Health <= 0f)
        {
            Health = 0f;
            StartCoroutine(Respawn());
        }
    }

    IEnumerator Respawn()
    {
        IsDead = true;
        yield return new WaitForSeconds(respawnDelay);

        controller.enabled = false;
        transform.position = spawnPosition;
        controller.enabled = true;

        Health = maxHealth;
        Stamina = maxStamina;
        IsDead = false;
    }
}
