using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles attack detection, dash/burst attack coroutine, applying damage and knockback to IHitable objects.
/// </summary>
public class EnemyMeleeCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Effective melee range (used only for deciding to attempt attack).")]
    public float attackRange = 0.8f;

    [Tooltip("Cooldown between attacks in seconds.")]
    public float attackCooldown = 1.0f;

    [Tooltip("Damage applied when hitting an IHitable target.")]
    public float damage = 10f;

    [Tooltip("Layer mask used to query colliders that can be hit (players, other hitable things).")]
    public LayerMask hitableLayer;

    [Tooltip("Offset from the enemy root where the attack hitbox sits (x respects facing).")]
    public Vector2 attackDirectionOffset = Vector2.zero;

    [Tooltip("Size of the attack box used in OverlapBox.")]
    public Vector2 attackBoxSize = new Vector2(0.6f, 0.6f);

    [Header("Animation / Timing")]
    [Tooltip("Delay before checking for hits after playing the attack animation.")]
    public float attackCheckDelay = 0f;

    [Tooltip("Time to sleep after the attack completes (enemy won't act).")]
    public float sleepTime = 2f;

    [Header("Attack Movement (Dash/Burst)")]
    [Tooltip("Impulse applied in facing direction during the attack dash.")]
    public Vector2 attackSpeedBurst = Vector2.zero;

    [Tooltip("How long the burst lasts (seconds).")]
    public float burstDuration = 0.5f;

    [Header("Knockback")]
    [Tooltip("Knockback force multiplier applied when this enemy is knocked.")]
    public float knockbackForce = 1.5f;

    [Header("Events")]
    [Tooltip("Invoked when the enemy performs an attack.")]
    public UnityEvent onAttack;



    [Header("Optional Animator")]
    [Tooltip("Optional weapon animator: plays 'Enemy_Stab' or 'Enemy_Swipe' by name.")]
    public Animator weaponAnimator;

    // Runtime
    EnemyController ctx;
    Rigidbody2D rb;
    Coroutine attackRoutine;
    float lastAttackTime = -999f;

    /// <summary>
    /// Initialize from controller. Called by EnemyController at Awake.
    /// </summary>
    public void Initialize(EnemyController controller)
    {
        ctx = controller;
        rb = ctx.rb;
        // weaponAnimator may be assigned from controller as well
    }

    void FixedUpdate()
    {
        if (ctx == null || rb == null || ctx.player == null) return;

        // Should not act while currently performing an attack routine or sleeping
        if (attackRoutine != null) return;

        // decide to attack if in range
        float dist = Vector2.Distance(rb.position, ctx.player.position);
        if (dist <= attackRange)
        {
            TryAttack();
        }
    }

    /// <summary>
    /// Attempt to start an attack if cooldown allows and there is at least one hitable within the attack box.
    /// </summary>
    public void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        // compute offset corrected for facing sign
        Transform visual = ctx.visualObject != null ? ctx.visualObject : ctx.transform;
        float sign = Mathf.Sign(visual.localScale.x != 0f ? visual.localScale.x : 1f);
        Vector2 offset = attackDirectionOffset;
        offset.x = Mathf.Abs(offset.x) * sign;
        Vector2 center = (Vector2)ctx.transform.position + offset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f, hitableLayer);

        bool anyHitableFound = false;
        foreach (var hit in hits)
        {
            if (hit == null) continue;

            Rigidbody2D hitRb = hit.attachedRigidbody;
            if (hitRb == null) continue;

            // if it's the player (or something else we want to hit)
            if (hitRb.CompareTag("Player"))
            {
                anyHitableFound = true;
                break;
            }
            // Optionally filter out other enemies / props here.
        }

        if (anyHitableFound)
        {
            // stop movement immediately and begin attack coroutine
            if (attackRoutine == null)
            {
                // clear velocity and start attack
                rb.linearVelocity = Vector2.zero;
                if (ctx.movement != null) ctx.movement.SetCanAct(false);
                attackRoutine = StartCoroutine(AttackFlash());
            }
        }
    }

    IEnumerator AttackFlash()
    {
        // movement paused for the duration of the attack
        Transform visual = ctx.visualObject != null ? ctx.visualObject : ctx.transform;
        // Play animation
        string animName = "Enemy_Stab";
        // If you want a Swipe vs Stab distinction, you can change this based on a setting in controller
        // kept as default/unchanged from original code
        if (weaponAnimator != null) weaponAnimator.Play(animName);

        // wait before checking hits (e.g. animation timing)
        yield return new WaitForSeconds(attackCheckDelay);

        // initial check before dash
        bool hitRegistered = TryHitPlayer();

        // call event
        onAttack?.Invoke();

        // DASH BURST: apply an impulse in the facing direction
        Vector2 dir = new Vector2(Mathf.Sign(visual.localScale.x != 0f ? visual.localScale.x : 1f), 0f);
        if (attackSpeedBurst != Vector2.zero)
        {
            // Use AddForce with impulse to keep behavior like original
            rb.AddForce(dir * attackSpeedBurst.magnitude, ForceMode2D.Impulse);
        }

        // while burst duration runs, keep checking hits
        float t = 0f;
        while (t < burstDuration)
        {
            t += Time.deltaTime;
            if (!hitRegistered)
                hitRegistered = TryHitPlayer();
            yield return null;
        }

        // damp velocity back to zero smoothly
        float time = 0f;
        float maxTime = 0.25f;
        Vector2 startSpeed = rb.linearVelocity;
        while (time < maxTime)
        {
            time += Time.deltaTime;
            float perc = Mathf.Clamp01(time / maxTime);
            rb.linearVelocity = Vector2.Lerp(startSpeed, Vector2.zero, perc);
            yield return null;
        }
        rb.linearVelocity = Vector2.zero;

        // sleep
        yield return new WaitForSeconds(sleepTime);

        // restore movement ability
        if (ctx.movement != null) ctx.movement.SetCanAct(true);

        attackRoutine = null;
        lastAttackTime = Time.time;
    }

    /// <summary>
    /// Checks the attack box and applies damage + knockback to IHitable/IKnockbackable targets.
    /// Returns true if any player/hitable was hit.
    /// </summary>
    public bool TryHitPlayer()
    {
        Transform visual = ctx.visualObject != null ? ctx.visualObject : ctx.transform;
        Vector2 center = (Vector2)ctx.transform.position + new Vector2(Mathf.Abs(attackDirectionOffset.x) * Mathf.Sign(visual.localScale.x != 0f ? visual.localScale.x : 1f), attackDirectionOffset.y);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f, hitableLayer);

        bool playerHitted = false;

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            Rigidbody2D hitRb = hit.attachedRigidbody;
            if (hitRb == null) continue;

            // ignore other enemies (if they have tag Enemy)
            if (hitRb.CompareTag("Enemy")) continue;

            if (hitRb.CompareTag("Player"))
            {
                // send ApplyHit to IHitable
                if (hitRb.TryGetComponent<IHitable>(out IHitable playerHitable))
                {
                    playerHitable.ApplyHit(damage);
                }

                // send knockback if supported
                if (hitRb.TryGetComponent<IKnockbackable>(out IKnockbackable kb))
                {
                    Vector2 knockDir = new Vector2(Mathf.Sign(visual.localScale.x != 0f ? visual.localScale.x : 1f), 0f);
                    kb.KnockbackTarget(damage, knockDir); // reuse damage as intensity, as original did
                }

                playerHitted = true;
            }
        }

        return playerHitted;
    }

    /// <summary>
    /// Called externally to apply knockback to this enemy.
    /// Stops any current attack and applies an impulse.
    /// </summary>
    /// <param name="force">base force magnitude</param>
    /// <param name="dir">direction (should be normalized)</param>
    public void KnockbackTarget(float force, Vector2 dir)
    {
        // stop current dash/attack
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        // restore movement control (so movement doesn't remain disabled)
        if (ctx.movement != null) ctx.movement.SetCanAct(true);

        // clear velocity and apply impulse
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(dir.normalized * force * knockbackForce, ForceMode2D.Impulse);
    }
}
