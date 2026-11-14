using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class PoCGnollAI : MonoBehaviour
{
    public Transform player;
    public float chaseRange = 15f;
    public float attackRange = 2.2f;
    public float attackCooldown = 2f;
    public float attackDamage = 1f;
    public float attackHitDelay = 0.6f;

    [Header("Boltgun / Retro 12-FPS Efekt")]
    public bool useSteppedAnimation = true;
    public float animationFPS = 12f;

    private NavMeshAgent agent;
    private Animator animator;
    private float lastAttackTime;
    private float animTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (animator.runtimeAnimatorController == null)
            Debug.LogWarning($"{name}: chyba Animator Controller (AC_Gnoll).", this);

        // NavMeshAgent riadi pohyb, root motion z animácie by ho posúvala mimo navigácie
        animator.applyRootMotion = false;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Retro trhaná animácia: automatický update Animatora vypneme
        if (useSteppedAnimation)
            animator.enabled = false;
    }

    void DealDamage()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) > attackRange + 0.8f) return;

        IDamageable target = player.GetComponent<IDamageable>();
        if (target != null) target.TakeDamage(attackDamage, transform.position);
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attackRange)
        {
            agent.ResetPath();
            animator.SetBool("IsMoving", false);

            // Otoč sa čelom k hráčovi
            Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
            transform.LookAt(lookPos);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                animator.SetTrigger("Attack");
                lastAttackTime = Time.time;
                Invoke(nameof(DealDamage), attackHitDelay);
            }
        }
        else if (dist <= chaseRange)
        {
            agent.SetDestination(player.position);
            animator.SetBool("IsMoving", true);
        }
        else
        {
            agent.ResetPath();
            animator.SetBool("IsMoving", false);
        }

        // Retro 12-FPS update animácie
        if (useSteppedAnimation)
        {
            animTimer += Time.deltaTime;
            float step = 1f / animationFPS;
            if (animTimer >= step)
            {
                animator.Update(animTimer);
                animTimer = 0f;
            }
        }
    }
}
