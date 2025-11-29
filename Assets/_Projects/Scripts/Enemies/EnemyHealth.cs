using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Holds health values. Keeps health logic separate and informs combat component about changes.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [Tooltip("Maximum health.")]
    public float MaxHealth = 10f;

    float currentHealth;
    EnemyController ctx;

    [Tooltip("Invoked when this enemy receives a hit. Passes normalized health (0..1).")]
    public UnityEvent<float> onHitReceived;

    public void Initialize(EnemyController controller)
    {
        ctx = controller;
        currentHealth = MaxHealth;
    }

    /// <summary>
    /// Apply damage to this enemy. Calls combat events and handles death.
    /// </summary>
    public void ApplyHit(float damage)
    {
        currentHealth -= damage;
        float normalized = Mathf.Clamp01(currentHealth / MaxHealth);
        onHitReceived?.Invoke(normalized);
        // notify combat of hit (will forward event)
        print(currentHealth);
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        // Death logic: you can expand this (spawn loot, play animation, disable, pool, etc.)
        // For now we destroy the GameObject to be consistent with "Death logic" placeholder.
        Destroy(gameObject);
    }
}
