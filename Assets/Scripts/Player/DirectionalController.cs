using UnityEngine;

/// <summary>
/// Transform-based controller for horizontal, vertical, or free movement.
/// </summary>
public class DirectionalController : BasePlayerController
{
    [Header("Axis Locks")]
    [Tooltip("Lock X movement (vertical-only style).")]
    public bool lockX = false;

    [Tooltip("Lock Y movement (horizontal-only style).")]
    public bool lockY = false;

    [Header("Aim")]
    [Tooltip("When enabled, rotates toward the mouse cursor.")]
    public bool aimTowardsMouse = true;

    protected override bool RequiresLookAction => aimTowardsMouse;

    protected override void HandleMovement(Vector2 moveInput, float speed)
    {
        Vector3 movement = new Vector3(
            lockX ? 0f : moveInput.x,
            lockY ? 0f : moveInput.y,
            0f
        );

        transform.position += movement * speed * Time.deltaTime;
    }

    protected override void HandleRotation(Vector2 moveInput, Vector2 lookPosition)
    {
        if (!aimTowardsMouse || Time.timeScale <= 0f)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (!mainCamera)
        {
            return;
        }

        Vector2 worldMouse = mainCamera.ScreenToWorldPoint(lookPosition);
        Vector2 direction = worldMouse - (Vector2)transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.up = direction;
        }
    }
}
