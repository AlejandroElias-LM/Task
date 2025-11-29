using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Reflection;

public class EnemyBehaviour : MonoBehaviour, IHitable, IKnockbackable
{
    [Header("References")]
    public Transform player; // assign in inspector or set from spawn manager
    Rigidbody2D _rb;

    [Header("Visual")]
    [Tooltip("The transform that will be flipped/checked for facing. If null, the root transform will be used.")]
    public Transform visualObject;

    [Header("Health")]
    public float MaxHealth = 10;
    float currentHealth;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float minSpeed = 0.5f; // so it never becomes too slow
    public float stoppingDistance = 0.6f; // how close to the player before stopping/attacking
    public float detectionRadius = 8f; // how far the enemy can "see" the player
    public LayerMask obstacleMask; // obstacles to consider for avoidance (tilemap/solid)
    public float obstacleAvoidDistance = 0.6f; // how far ahead to check for obstacles
    public float obstacleAvoidStrength = 1.2f; // how strongly to steer away from obstacles

    [Header("Attack")]
    public float attackRange = 0.8f; // effective melee range
    public float attackCooldown = 1.0f;
    public int damage = 1;
    public LayerMask hitableLayer; // changed from playerLayer -> we now query for any IHitable
    public Vector2 attackDirectionOffset = Vector2.zero; // if the hitbox is in front
    public Vector2 attackBoxSize = new Vector2(0.6f, 0.6f);

    public enum AttackType { Stab, Swipe }
    [Header("Attack Animation")]

    [SerializeField] private Animator weaponAnimator;


    //-------------------------------------Attack Type------------------------------------//
    public AttackType animationType;
    [Tooltip("How much time the enemy wait to check if it can damage the player")]
    public float attackCheckDelay = 0f;
    [Tooltip("How much time the enemy will sleep after some attack")]
    public float sleepTime = 2f;

    //-------------------------------------Speed Burst------------------------------------//
    [Tooltip("Sets a speed once the attack are performed")]
    public Vector2 attackSpeedBurst;
    public float burstDuration = .5f;


    Coroutine attackAnimation;

    [Header("Events")]
    public UnityEvent onAttack;
    public UnityEvent<float> onHitReceived; //shoot with the normalized health

    [Header("Optional")]
    public bool faceSpriteToMovement = true;
    public float knockbackForce = 1.5f;

    float lastAttackTime = 0;
    Vector2 velocity;
    bool canAct = true;

    // Helper to get the effective visual transform (fallback to root)
    Transform Visual => visualObject != null ? visualObject : transform;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (player == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO) player = playerGO.transform;
        }

        currentHealth = MaxHealth;
    }
    void FixedUpdate()
    {
        // do nothing while attacking/sleeping
        if (player == null || !canAct) return;

        Vector2 pos = _rb.position;
        Vector2 playerPos = player.position;
        Vector2 toPlayer = playerPos - pos;
        float dist = toPlayer.magnitude;

        // desired velocity toward player
        velocity = _rb.linearVelocity;
        Vector2 desired = toPlayer.normalized * moveSpeed;
        if(dist <= stoppingDistance)
        {
            desired = Vector2.zero;
        }
        if (dist <= attackRange)
        { 
            TryAttack();
        }
        else if (dist <= detectionRadius)
        {
            desired *= 1.5f;
        }

        float smooth = 10f;
        velocity = Vector2.Lerp(velocity, desired, Time.fixedDeltaTime * smooth);
        _rb.linearVelocity = velocity;

        // facing (affect visualObject only)
        if (faceSpriteToMovement && velocity.sqrMagnitude > 0.01f)
        {
            Vector3 visScale = Visual.localScale;
            float targetSign = Mathf.Sign(toPlayer.x);
            visScale.x = targetSign * Mathf.Abs(visScale.x);
            Visual.localScale = visScale;
        }
    }

    void TryAttack()
    {
        Debug.Log("Trying");
        if (Time.time - lastAttackTime < attackCooldown) return;

        // adjust attack offset to face direction using visual object's x scale sign
        var offSet = attackDirectionOffset;
        offSet.x = Mathf.Abs(offSet.x) * Mathf.Sign(Visual.localScale.x);
        Vector2 center = (Vector2)transform.position + offSet;

        // get all colliders inside the attack box
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f, hitableLayer);

        Debug.Log($"[EnemyBehaviour] Try Attack: Found {hits.Length} hit(s).");
        bool anyHitableFound = false;

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            // skip other enemies (or things that have EnemyBehaviour in parent)
            if (hit.attachedRigidbody.CompareTag("Player"));
            {
                anyHitableFound = true ;
            }
        }

        if (anyHitableFound)
        {
            if (attackAnimation == null)
            {
                // Immediately kill momentum: clear cache AND Rigidbody's linearVelocity
                velocity = Vector2.zero;
                _rb.linearVelocity = Vector2.zero;

                attackAnimation = StartCoroutine(AttackFlash());
            }
        }
    }


    IEnumerator AttackFlash()
    {
        // make sure movement stops immediately
        velocity = Vector2.zero;
        _rb.linearVelocity = Vector2.zero;

        canAct = false;

        var animation = (int)animationType == 1 ? "Enemy_Swipe" : "Enemy_Stab";
        if (weaponAnimator != null)
            weaponAnimator.Play(animation);

        yield return new WaitForSeconds(attackCheckDelay);

        bool playerHit = TryHitPlayer();

        onAttack?.Invoke();

        // --- DASH BURST ---
        // dash in facing direction using visual object's sign
        Vector2 dir = new Vector2(Mathf.Sign(Visual.localScale.x), 0f);
        _rb.AddForce(dir * attackSpeedBurst.magnitude);

        var time = 0f;
        while (time < burstDuration)
        {
            time += Time.deltaTime;
            if(!playerHit)
                playerHit = TryHitPlayer();
            yield return null;
        }


        // stop after dash
        time = 0f;
        var maxTime = .25f;
        var startSpeed = _rb.linearVelocity;
        while (time < maxTime)
        {
            time += Time.deltaTime;
            var perc = Mathf.Clamp01(time / maxTime);
            _rb.linearVelocity = Vector2.Lerp(startSpeed, Vector2.zero, perc);
            yield return null;
        }
        _rb.linearVelocity = Vector2.zero;

       

        yield return new WaitForSeconds(sleepTime);

        canAct = true;
        attackAnimation = null;
        lastAttackTime = Time.time;
    }


    public bool TryHitPlayer()
    {
        bool playerHitted = false;
        // check damage AFTER dash (same logic as TryAttack)
        Vector2 center = (Vector2)transform.position + new Vector2(Mathf.Abs(attackDirectionOffset.x) * Mathf.Sign(Visual.localScale.x), attackDirectionOffset.y);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f, hitableLayer);

        Debug.Log($"[EnemyBehaviour] AttackFlash: Post-dash found {hits.Length} hit(s).");
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.attachedRigidbody.CompareTag("Enemy")) continue;

            if (hit.attachedRigidbody.CompareTag("Player"))
            {
                hit.attachedRigidbody.TryGetComponent<IHitable>(out IHitable playerHitable);
                if(playerHitable != null)
                {
                    playerHitable.ApplyHit(10f);
                    if (playerHitable is IKnockbackable)
                        (playerHitable as IKnockbackable).KnockbackTarget(10f, new Vector2(Mathf.Sign(visualObject.localScale.x), 0));
                }
                playerHitted = true;
            }
        }

        return playerHitted;
    }


    void OnDrawGizmosSelected()
    {
        // detection radius around the enemy's root position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // attack range visualized around the attack origin (taking visual facing into account)
        Gizmos.color = Color.red;
        Vector2 attackCenter = (Vector2)transform.position;
        // compute offset adjusted by visual facing sign (safe even in editor if visualObject is null)
        float visSign = Mathf.Sign(Visual.localScale.x != 0f ? Visual.localScale.x : 1f);
        Vector2 signedOffset = attackDirectionOffset;
        signedOffset.x = Mathf.Abs(signedOffset.x) * visSign;
        attackCenter += signedOffset;
        Gizmos.DrawWireSphere(attackCenter, attackRange);

        // attack hitbox (cube) at the same adjusted center
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(attackCenter, attackBoxSize);
    }


    public void ApplyHit(float damage)
    {
        currentHealth -= damage;
        onHitReceived?.Invoke(Mathf.Clamp01(currentHealth / MaxHealth));

        if (currentHealth <= 0)
        {
            //Death logic
        }
    }

    public void KnockbackTarget(float force, Vector2 dir)
    {
        print("Knockback");
        _rb.linearVelocity = Vector2.zero;
        if (attackAnimation != null) StopCoroutine(attackAnimation);
        attackAnimation = null;
        canAct = true;
        _rb.AddForce(dir * force * knockbackForce, ForceMode2D.Impulse);
    }

}
