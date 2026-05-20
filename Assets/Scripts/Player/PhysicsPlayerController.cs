using UnityEngine;

/// <summary>
/// Base class for physics-driven player controllers.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class PhysicsPlayerController : BasePlayerController
{
    [Header("Physics")]
    public Rigidbody2D myRigidbody;

    protected Rigidbody2D Rb { get; private set; }

    protected override bool MoveInFixedUpdate => true;

    protected override void Start()
    {
        base.Start();

        Rb = myRigidbody ? myRigidbody : GetComponent<Rigidbody2D>();
        myRigidbody = Rb;

        SetupRigidbody();
    }

    protected virtual void SetupRigidbody()
    {
        if (!Rb)
        {
            return;
        }

        Rb.gravityScale = 0f;
    }

    protected override void UpdateAnimations(Vector2 moveInput, float speed)
    {
        if (!animator)
        {
            return;
        }

        float normalizedSpeed = 0f;
        if (Rb)
        {
            normalizedSpeed = Mathf.Clamp01(Rb.linearVelocity.magnitude / Mathf.Max(speed, 0.001f));
        }

        animator.SetFloat(AnimMoveSpeed, normalizedSpeed * animationSpeed);
        animator.SetBool(AnimIsMoving, normalizedSpeed > 0.01f);
    }
}
