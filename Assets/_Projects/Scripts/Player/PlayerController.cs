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
    public float currentHealth;

    [Header("Events")]
    public UnityEvent<float> onHitReceived;

    private Rigidbody2D _rb;



    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();


        if (inputManager == null)
        {
            inputManager = GetComponent<PlayerInputManager>();
        }


        if (inputManager == null)
        {
            Debug.LogWarning("PlayerController2D has no PlayerInputManager assigned.");
        }
        currentHealth = MaxHealth;
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

    public void ApplyHit(float damage)
    {
        currentHealth -= damage;
        onHitReceived?.Invoke(Mathf.Clamp01(currentHealth / MaxHealth));

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
}