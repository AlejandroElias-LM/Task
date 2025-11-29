using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;


/// <summary>
/// PlayerInputManager: receives InputActionProperty references (set in inspector)
/// and exposes a small API so other classes can read the current input values.
///
/// Usage:
/// - Assign MoveAction (Vector2) and InteractAction (Button) in the inspector.
/// - Other scripts can read the input via the public properties or subscribe to
/// the InteractPerformed event.
/// </summary>
[DisallowMultipleComponent]
public class PlayerInputManager : MonoBehaviour
{
    [Header("Input Actions (assign in inspector)")]
    [Tooltip("A Vector2 action for locomotion (x=horizontal, y=vertical)")]
    public InputActionProperty MoveAction;


    [Tooltip("A Button action for interact/submit")]
    public InputActionProperty InteractAction;
    [Tooltip("A Button action for Attack")]
    public InputActionProperty AttackAction;


    // Public read API
    public Vector2 Move => _currentMove;
    public float Horizontal => _currentMove.x;
    public float Vertical => _currentMove.y;


    /// <summary>
    /// True while the interact button is pressed (Read in Update/FixedUpdate)
    /// </summary>
    public bool InteractPressed => _interactPressed;


    [Header("Events")]
    public UnityEvent InteractPerformed;
    public UnityEvent AttackPerformed;


    // internal state
    private Vector2 _currentMove;
    private bool _interactPressed;


    private void OnEnable()
    {
        if (MoveAction != null && MoveAction.action != null)
        {
            MoveAction.action.Enable();
        }


        if (InteractAction != null)
        {
            InteractAction.action.Enable();
            InteractAction.action.performed += OnInteractPerformed;
        }

        if (AttackAction != null)
        {
            AttackAction.action.Enable();
            AttackAction.action.performed += OnAttackPerformed;
        }
    }


    private void OnDisable()
    {
        if (MoveAction != null && MoveAction.action != null)
            MoveAction.action.Disable();


        if (InteractAction != null && InteractAction.action != null)
        {
            InteractAction.action.performed -= OnInteractPerformed;
            InteractAction.action.Disable();
        }

        if (AttackAction != null)
        {
            AttackAction.action.performed -= OnAttackPerformed;
            AttackAction.action.Disable();
        }
    }


    private void Update()
    {
        // ReadMove every frame so other classes can access fresh values in Update/FixedUpdate
        _currentMove = MoveAction != null ? MoveAction.action.ReadValue<Vector2>() : Vector2.zero;


        // For pressed state, check the button's current value if available
        if (InteractAction != null)
        {
            var v = InteractAction.action.ReadValue<float>();
            _interactPressed = v > 0.5f;
        }
        else
        {
            _interactPressed = false;
        }
    }


    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        InteractPerformed?.Invoke();
    }
    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        AttackPerformed?.Invoke();
    }
}