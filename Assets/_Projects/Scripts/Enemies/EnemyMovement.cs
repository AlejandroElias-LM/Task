using UnityEngine;

/// <summary>
/// Handles movement toward the player, stopping distance, detection radius and facing flip.
/// Uses Rigidbody2D.velocity for movement so external forces (knockback / dash) are preserved.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Movement speed in units/sec.")]
    public float moveSpeed = 3f;

    [Tooltip("Minimum speed so the enemy never becomes too slow.")]
    public float minSpeed = 0.5f;

    [Tooltip("How close the enemy will stop from the player (won't try to move inside this).")]
    public float stoppingDistance = 0.6f;

    [Tooltip("How far the enemy can detect the player and start moving/aggressive behavior.")]
    public float detectionRadius = 8f;

    [Header("Facing")]
    [Tooltip("If true, flips the visual transform X scale based on movement/player position.")]
    public bool faceSpriteToMovement = true;

    [Header("Optional Obstacle Avoidance (simple)")]
    [Tooltip("LayerMask for obstacles used by a basic forward check.")]
    public LayerMask obstacleMask;
    [Tooltip("Distance ahead to check for obstacles.")]
    public float obstacleAvoidDistance = 0.6f;
    [Tooltip("How strongly to steer away from obstacles.")]
    public float obstacleAvoidStrength = 1.2f;

    // Runtime
    Rigidbody2D rb;
    EnemyController ctx;
    Transform Visual => ctx.visualObject != null ? ctx.visualObject : transform;

    // Movement smoothing
    float smooth = 10f;
    Vector2 cachedVelocity;
    bool canAct = true;

    /// <summary>
    /// Initialize references from controller. Called by EnemyController at Awake.
    /// </summary>
    public void Initialize(EnemyController controller)
    {
        ctx = controller;
        rb = ctx.rb;
    }

    void FixedUpdate()
    {
        if (ctx == null || rb == null || ctx.player == null) return;
        if (!canAct) return;

        Vector2 pos = rb.position;
        Vector2 playerPos = ctx.player.position;
        Vector2 toPlayer = playerPos - pos;
        float dist = toPlayer.magnitude;

        // start with current velocity so we don't instantly zero external impulses
        Vector2 velocity = rb.linearVelocity;

        Vector2 desired = Vector2.zero;
        if (dist > stoppingDistance)
        {
            // move toward player when within detection radius
            if (dist <= detectionRadius)
            {
                desired = toPlayer.normalized * Mathf.Max(moveSpeed, minSpeed);
            }
            else
            {
                desired = Vector2.zero;
            }
        }
        else
        {
            desired = Vector2.zero;
        }

        // Simple obstacle avoidance: cast a short box/ray ahead and steer away if we hit something.
        if (obstacleMask != 0)
        {
            Vector2 forward = desired.normalized;
            if (forward.sqrMagnitude > 0.001f)
            {
                RaycastHit2D hit = Physics2D.Raycast(rb.position, forward, obstacleAvoidDistance, obstacleMask);
                if (hit.collider != null)
                {
                    // steer with a perpendicular vector
                    Vector2 perp = Vector2.Perpendicular(forward).normalized;
                    // choose side depending on which is clearer
                    RaycastHit2D left = Physics2D.Raycast(rb.position, perp, obstacleAvoidDistance, obstacleMask);
                    if (left.collider != null)
                        perp = -perp;
                    desired += perp * obstacleAvoidStrength;
                }
            }
        }

        // aggressive chase increases speed slightly when close
        if (dist <= ctx.combat?.attackRange + 0.1f && ctx.combat != null)
        {
            desired *= 1.0f; // keep same (combat handles attack/dash). reserved here if you want to tweak
        }
        else if (dist <= detectionRadius)
        {
            desired *= 1.5f;
        }

        // smooth interpolation so external forces persist but movement still seeks target
        velocity = Vector2.Lerp(velocity, desired, Time.fixedDeltaTime * smooth);
        rb.linearVelocity = velocity; // apply to Rigidbody2D

        // facing (affect visualObject only)
        if (faceSpriteToMovement && velocity.sqrMagnitude > 0.01f)
        {
            Vector3 visScale = Visual.localScale;
            float targetSign = Mathf.Sign(toPlayer.x != 0 ? toPlayer.x : visScale.x);
            visScale.x = targetSign * Mathf.Abs(visScale.x);
            Visual.localScale = visScale;
        }
    }

    /// <summary>
    /// Temporarily enable/disable movement (used by combat when attacking).
    /// </summary>
    public void SetCanAct(bool value)
    {
        canAct = value;
    }

    public float knockbackForce = 1.5f;
    public void KnockbackTarget(float force, Vector2 dir)
    {
        print("Knockback");
        rb.linearVelocity = Vector2.zero;
        canAct = true;
        rb.AddForce(dir * force * knockbackForce, ForceMode2D.Impulse);
    }
}
