using UnityEngine;

// Lives on the SwordHolder (next to the Animator) because animation events are delivered
// to components on the Animator's own GameObject. Forwards them to PlayerCombat.
public class SwordAnimationEvents : MonoBehaviour
{
    PlayerCombat combat;

    void Awake()
    {
        combat = GetComponentInParent<PlayerCombat>();
    }

    // Animation event: fired on the impact frame of each swing clip.
    public void OnSwordHit(string kind)
    {
        if (combat != null) combat.OnSwordHit(kind);
    }
}
