using UnityEngine;

/// <summary>
/// PlayerController2D: moves a Rigidbody2D using acceleration toward a target velocity
/// determined by inputs provided by PlayerInputManager.
///
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("Reference to the PlayerInputManager to read input from.")]
    public PlayerInputManager inputManager;


    [Header("Movement")]
    public float maxSpeed = 5f;
    public float acceleration = 20f;


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
}