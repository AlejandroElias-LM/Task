using UnityEngine;

/// <summary>
/// Root component that wires Movement, Combat and Health together.
/// Configure references here (player, visual object, animator, layers).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour, IHitable, IKnockbackable
{
    [Header("References")]
    [Tooltip("Player transform. If left empty, the script will try to find a GameObject tagged 'Player' at Awake.")]
    public Transform player;

    [Tooltip("Transform used for flipping/visuals. If null, the root transform will be used.")]
    public Transform visualObject;

    [Header("Animator")]
    [Tooltip("Optional animator used to play weapon animations (Enemy_Stab, Enemy_Swipe).")]
    public Animator weaponAnimator;

    // Subcomponents (auto-wired if present)
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public EnemyMovement movement;
    [HideInInspector] public EnemyMeleeCombat combat;
    [HideInInspector] public EnemyHealth health;

    public void ApplyHit(float damage)
    {
        health.ApplyHit(damage);
    }

    public void KnockbackTarget(float force, Vector2 dir)
    {
        movement.KnockbackTarget(force, dir);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        // Try to find required pieces on same GameObject
        movement = GetComponent<EnemyMovement>();
        combat = GetComponent<EnemyMeleeCombat>();
        health = GetComponent<EnemyHealth>();

        // If components exist, give them references they need
        if (movement != null)
        {
            movement.Initialize(this);
        }

        if (combat != null)
        {
            combat.Initialize(this);
            // pass animator reference
            combat.weaponAnimator = weaponAnimator;
        }

        if (health != null)
        {
            health.Initialize(this);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Keep small editor-time safety: if visualObject set to null we still can use transform at runtime.
        if (visualObject == null) visualObject = transform;
    }
#endif
}
