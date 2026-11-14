using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PoCGnollHealth : MonoBehaviour, IDamageable
{
    public float maxHealth = 3f;
    public float knockback = 1.2f;
    public float fallDuration = 0.6f;
    public float corpseLifetime = 6f;

    public bool IsDead { get; private set; }

    private float health;
    private NavMeshAgent agent;

    void Awake()
    {
        health = maxHealth;
        agent = GetComponent<NavMeshAgent>();
    }

    public void TakeDamage(float amount, Vector3 sourcePosition)
    {
        if (IsDead) return;
        health -= amount;

        Vector3 away = transform.position - sourcePosition;
        away.y = 0f;
        if (agent.enabled && agent.isOnNavMesh && away.sqrMagnitude > 0.001f)
            agent.Move(away.normalized * knockback);

        if (health <= 0f) Die();
    }

    void Die()
    {
        IsDead = true;

        PoCGnollAI ai = GetComponent<PoCGnollAI>();
        if (ai != null)
        {
            ai.CancelInvoke();
            ai.enabled = false;
        }

        agent.enabled = false;
        foreach (Collider c in GetComponents<Collider>()) c.enabled = false;
        StartCoroutine(Fall());
    }

    IEnumerator Fall()
    {
        Quaternion start = transform.rotation;
        Quaternion end = start * Quaternion.AngleAxis(-90f, Vector3.right);

        for (float t = 0f; t < 1f; t += Time.deltaTime / fallDuration)
        {
            transform.rotation = Quaternion.Slerp(start, end, t);
            yield return null;
        }
        transform.rotation = end;

        yield return new WaitForSeconds(corpseLifetime);
        Destroy(gameObject);
    }
}
