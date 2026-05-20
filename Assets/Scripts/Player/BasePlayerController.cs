using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Base class for all player controllers.
/// Shares input, speed/power-up logic, and animation updates.
/// </summary>
public abstract class BasePlayerController : MonoBehaviour
{
    [Header("Input Actions")]
    public InputAction moveAction;
    public InputAction lookAction;

    [Header("Movement")]
    public float moveSpeed = 10f;

    [Header("Animation")]
    public Animator animator;
    public float animationSpeed = 1f;

    protected static readonly int AnimMoveSpeed = Animator.StringToHash("MoveSpeed");
    protected static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");

    private PlayerPowerUpController _powerUpController;

    protected virtual bool MoveInFixedUpdate => false;
    protected virtual bool RequiresLookAction => true;

    protected virtual void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
    }

    protected virtual void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
    }

    protected virtual void Start()
    {
        ValidateBindings();
        _powerUpController = GetComponent<PlayerPowerUpController>();

        if (!animator)
        {
            animator = GetComponent<Animator>();
        }
    }

    protected virtual void Update()
    {
        Vector2 moveInput = ReadMoveInput();
        Vector2 lookPosition = GetLookPosition();
        float speed = GetCurrentMoveSpeed();

        if (!MoveInFixedUpdate)
        {
            HandleMovement(moveInput, speed);
        }

        HandleRotation(moveInput, lookPosition);
        UpdateAnimations(moveInput, speed);
    }

    protected virtual void FixedUpdate()
    {
        if (!MoveInFixedUpdate)
        {
            return;
        }

        HandleMovement(ReadMoveInput(), GetCurrentMoveSpeed());
    }

    protected abstract void HandleMovement(Vector2 moveInput, float speed);
    protected abstract void HandleRotation(Vector2 moveInput, Vector2 lookPosition);

    protected virtual Vector2 GetLookPosition()
    {
        if (RequiresLookAction && lookAction != null)
        {
            return lookAction.ReadValue<Vector2>();
        }

        return transform.up;
    }

    protected Vector2 ReadMoveInput()
    {
        return moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    }

    protected float GetCurrentMoveSpeed()
    {
        return _powerUpController ? moveSpeed * _powerUpController.speedMultiplier : moveSpeed;
    }

    protected virtual void UpdateAnimations(Vector2 moveInput, float speed)
    {
        if (!animator)
        {
            return;
        }

        float magnitude = moveInput.magnitude;
        animator.SetFloat(AnimMoveSpeed, magnitude * animationSpeed);
        animator.SetBool(AnimIsMoving, magnitude > 0.01f);
    }

    private void ValidateBindings()
    {
        if (moveAction == null || moveAction.bindings.Count == 0)
        {
            Debug.LogWarning($"[{GetType().Name}] moveAction has no binding configured.");
        }

        if (RequiresLookAction && (lookAction == null || lookAction.bindings.Count == 0))
        {
            Debug.LogWarning($"[{GetType().Name}] lookAction has no binding configured.");
        }
    }
}
