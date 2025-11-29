using UnityEngine;

/// <summary>
/// PlayerAnimatorController: Handles the Animator Logic so the PlayerController doesn't turn into a spaghetti.
///
/// </summary>
public class PlayerAnimatorController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Animator that this class will work with")]
    [SerializeField] private Animator _anim;
    [Tooltip("The Input Manager that will provide the player Input")]
    [SerializeField] private PlayerInputManager _input;

    [Header("Visuals Settings")]
    [SerializeField] private Transform visualObject;
    [Tooltip("Should this transform be invertex on X to turn the character?")]
    public bool invertXScale = true;
    private Vector3 originalScale, negativeXscale;


    //String -> Hash
    private static readonly int f_Ydir = Animator.StringToHash("Y_Dir");
    private static readonly int b_isMoving = Animator.StringToHash("IsMoving");

    private void Awake()
    {
        if (_anim == null)
        {
            Destroy(this);
            Debug.LogError("[PlayerAnimatorController] Awake: Animator not assigned. Destroying Controller.");
        }
        if (_input == null)
            _input = GetComponent<PlayerInputManager>();

        if (visualObject) {
            originalScale = visualObject.localScale;
            negativeXscale = originalScale;
            negativeXscale.x *= -1;
        }
    }
    // Update is called once per frame
    void Update()
    {
        _anim.SetFloat(f_Ydir, Mathf.Clamp(Mathf.Sign(_input.Vertical) + 1, 0, 1));
        _anim.SetBool(b_isMoving, _input.Move.magnitude != 0);



        if (!visualObject) return;
        if(_input.Horizontal < 0 && invertXScale)
        {
            visualObject.localScale = negativeXscale;
        }
        else if (_input.Move.x > 0)
        {
            visualObject.localScale = originalScale;
        }
    }
}
