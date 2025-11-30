using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// PlayerController2D: moves a Rigidbody2D using acceleration toward a target velocity
/// determined by inputs provided by PlayerInputManager.
///
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IHitable, IKnockbackable
{
    [Header("Components")]
    [Tooltip("Reference to the PlayerInputManager to read input from.")]
    public PlayerInputManager inputManager;


    [Header("Movement")]
    public float maxSpeed = 5f;
    public float acceleration = 20f;

    [Header("Player Params")]
    public float MaxHealth = 100f;
    float currentMaxHealth = 100f;
    public float currentHealth;

    [Header("Events")]
    public UnityEvent<float> onHitReceived;
    public UnityEvent<string> lifeValueUpdate;

    private Rigidbody2D _rb;

    public UnityEvent<EnemyController> applyHitEffect;

    private Dictionary<ModifierType, (Action<float>,float)> onHitEffects;
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        onHitEffects = new();

        if (inputManager == null)
        {
            inputManager = GetComponent<PlayerInputManager>();
        }


        if (inputManager == null)
        {
            Debug.LogWarning("PlayerController2D has no PlayerInputManager assigned.");
        }
        currentMaxHealth = MaxHealth;
        currentHealth = MaxHealth;

        applyHitEffect.AddListener((EnemyController _) =>
        {
            foreach (var hfx in onHitEffects)
            {
                var v = hfx.Value;
                v.Item1.Invoke(v.Item2);
            }
        });
    }


    private void FixedUpdate()
    {
        if (inputManager == null || _rb == null) return;


        Vector2 move = inputManager.Move;
        Vector2 targetVelocity = move * maxSpeed;

        // Smoothly change the rigidbody velocity towards the target using acceleration
        Vector2 newVelocity = Vector2.MoveTowards(_rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);


        _rb.linearVelocity = newVelocity;
    }

    public void ApplyHit(float damage, GameObject hitter)
    {
        currentHealth -= damage;
        onHitReceived?.Invoke(Mathf.Clamp01(currentHealth / currentMaxHealth));
        lifeValueUpdate?.Invoke(currentHealth + "/" + currentMaxHealth);
        if (currentHealth <= 0)
        {
            //Death logic
        }
    }

    [Range(0,1)] public float knockbackForce = 1;
    public void KnockbackTarget(float force, Vector2 dir)
    {
        print("Knockback");
        _rb.AddForce(dir * force * knockbackForce, ForceMode2D.Impulse);
    }


    public void LoadHealthBuff()
    {
        if (PlayerBuffManager.instance == null) return;
        var instance = PlayerBuffManager.instance;

        var healthNorm = currentHealth / currentMaxHealth;
        var healthStatBuff = instance.GetBuffBucket(ModifierType.MaxHealth);
        if(healthStatBuff != null)
        {
            var value = healthStatBuff.GetValue();
            currentMaxHealth = MaxHealth + value;
            currentHealth = currentMaxHealth * healthNorm;
            lifeValueUpdate?.Invoke(Mathf.Floor(currentHealth) + "/" + currentMaxHealth);
        }

        var healthOnHit = instance.GetBuffBucket(ModifierType.HealthPerHit);
        if(healthOnHit != null)
        {
            onHitEffects[ModifierType.HealthPerHit] = ((float value )=>
            {
                this.currentHealth = Mathf.Clamp(currentHealth + value, 0, currentMaxHealth);
                print("healing " + value);

                lifeValueUpdate?.Invoke(Mathf.Floor(currentHealth) + "/" + currentMaxHealth);
                onHitReceived?.Invoke(Mathf.Clamp01(currentHealth / currentMaxHealth));
            }          
            , healthOnHit.GetValue());
        }
        else
        {
            if (onHitEffects.ContainsKey(ModifierType.HealthPerHit))
            {
                onHitEffects.Remove(ModifierType.HealthPerHit);
            }

        }
    }
}