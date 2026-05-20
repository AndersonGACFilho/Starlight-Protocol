using UnityEngine;

/// <summary>
/// Asteroids-style controller:
/// vertical input applies forward force, horizontal input rotates the ship.
/// </summary>
public class AsteroidsController : PhysicsPlayerController
{
    [Header("Asteroids")]
    public float rotationSpeed = 60f;

    protected override bool RequiresLookAction => false;

    protected override void SetupRigidbody()
    {
        base.SetupRigidbody();

        if (!Rb)
        {
            return;
        }

        Rb.freezeRotation = true;
    }

    protected override void HandleMovement(Vector2 moveInput, float speed)
    {
        if (!Rb || Mathf.Abs(moveInput.y) <= 0.001f)
        {
            return;
        }

        Vector2 force = (Vector2)transform.up * moveInput.y * speed;
        Rb.AddForce(force);
    }

    protected override void HandleRotation(Vector2 moveInput, Vector2 lookPosition)
    {
        float delta = -rotationSpeed * moveInput.x * Time.deltaTime;

        Vector3 euler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(euler.x, euler.y, euler.z + delta);
    }
}
