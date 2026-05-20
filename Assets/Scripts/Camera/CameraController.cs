using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Class which handles camera movement
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    // The camera being controlled by this script
    private Camera playerCamera = null;

    [Header("GameObject References")]
    [Tooltip("The target to follow with this camera")]
    public Transform target = null;

    [Header("Camera Bounds")]
    [Tooltip("2D Collider which defines the bounds of the camera's movement. " +
             "The camera will not move outside of this collider.")]
    public Collider2D cameraBounds = null;

    /// <summary>
    /// Enum to determine camera movement styles
    /// </summary>
    public enum CameraStyles { Locked, Overhead, Free };

    [Header("CameraMovement")]
    [Tooltip("The way this camera moves:\n" +
        "\tLocked: Camera cannot follow mouse, it stays locked onto the target.\n" +
        "\tOverhead: Camera follows the target directly with no mouse offset.\n" +
        "\tFree: Camera follows the mouse, clamped within maxDistanceFromTarget from the target.")]
    public CameraStyles cameraMovementStyle = CameraStyles.Locked;

    [Tooltip("The distance between the target position and the mouse to move the camera to in \"Free\" mode.")]
    [Range(0, 0.75f)] public float freeCameraMouseTracking = 0.5f;

    [Tooltip("The maximum distance away from the target that the camera can move")]
    public float maxDistanceFromTarget = 5.0f;

    [Tooltip("The z coordinate to use for the camera position")]
    public float cameraZCoordinate = -10.0f;

    [Header("Camera Smoothing")]
    [Tooltip("How long it takes the camera to reach its desired position in Free mode. " +
             "0 means no smoothing (instant snap). Higher values feel floatier.")]
    [Range(0f, 0.5f)] public float cameraSmoothTime = 0.12f;

    [Header("Input Actions & Controls")]
    [Tooltip("The input action(s) that map to where the camera looks")]
    public InputAction lookAction;

    // Internal velocity used by SmoothDamp to track camera momentum between frames
    private Vector3 _smoothVelocity = Vector3.zero;

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is enabled
    /// </summary>
    void OnEnable()
    {
        lookAction.Enable();
    }

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is disabled
    /// </summary>
    void OnDisable()
    {
        lookAction.Disable();
    }

    /// <summary>
    /// Description:
    /// When the script starts up, get the camera component to use
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    void Start()
    {
        playerCamera = GetComponent<Camera>();
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called after all Update calls in the same frame.
    /// Using LateUpdate ensures the target has already moved before the camera follows,
    /// preventing jitter or one-frame lag.
    /// Inputs: none
    /// Returns: 
    /// void (no return)
    /// </summary>
    void LateUpdate()
    {
        SetCameraPosition();
    }

    /// <summary>
    /// Description:
    /// Sets the camera's position according to the settings.
    /// In Free mode, applies SmoothDamp for gradual movement and double-clamps
    /// against the bounds collider to prevent overshoot from accumulated velocity.
    /// Input:
    /// none
    /// Return:
    /// void (no return)
    /// </summary>
    private void SetCameraPosition()
    {
        if (!target) return;

        Vector3 targetPosition = GetTargetPosition();
        Vector3 mousePosition = GetPlayerMousePosition();
        Vector3 desired = ComputeCameraPosition(targetPosition, mousePosition);

        // Clamp the desired destination first so SmoothDamp aims at a valid position
        Vector3 clampedDesired = ClampCameraPositionToBounds(desired);

        Vector3 next;
        if (cameraMovementStyle == CameraStyles.Free && cameraSmoothTime > 0f)
        {
            // Smoothly move toward the already-clamped destination
            next = Vector3.SmoothDamp(
                transform.position,
                clampedDesired,
                ref _smoothVelocity,
                cameraSmoothTime
            );

            // Re-clamp to cut any overshoot caused by SmoothDamp inertia
            next = ClampCameraPositionToBounds(next);

            // Zero out velocity on axes that were blocked to prevent boundary vibration.
            // Without this, accumulated velocity keeps pushing against the wall each frame.
            if (next.x != clampedDesired.x) _smoothVelocity.x = 0f;
            if (next.y != clampedDesired.y) _smoothVelocity.y = 0f;
        }
        else
        {
            next = clampedDesired;
        }

        transform.position = next;
    }

    /// <summary>
    /// Description:
    /// Clamps the camera center inside the configured bounds collider so that the
    /// camera viewport edges never exceed the collider boundary.
    /// For orthographic cameras the half-extents of the viewport are subtracted from
    /// each side, keeping the rendered area fully inside the bounds.
    /// For perspective cameras (or when playerCamera is unavailable) the camera
    /// center is simply constrained to the nearest point inside the collider.
    /// Inputs:
    /// Vector3 position
    /// Returns:
    /// Vector3
    /// </summary>
    /// <param name="position">Desired camera position.</param>
    /// <returns>Vector3: A position constrained to cameraBounds when assigned.</returns>
    private Vector3 ClampCameraPositionToBounds(Vector3 position)
    {
        if (!cameraBounds)
        {
            return position;
        }

        if (!playerCamera)
        {
            playerCamera = GetComponent<Camera>();
        }

        // For a perspective camera or unknown setup, fall back to center clamp
        if (!playerCamera || !playerCamera.orthographic)
        {
            Vector2 clampedPoint = cameraBounds.ClosestPoint(new Vector2(position.x, position.y));
            position.x = clampedPoint.x;
            position.y = clampedPoint.y;
            return position;
        }

        Bounds bounds = cameraBounds.bounds;
        float cameraHalfHeight = playerCamera.orthographicSize;
        float cameraHalfWidth = cameraHalfHeight * playerCamera.aspect;

        // Inset the allowed range by the viewport half-extents so the screen edges
        // stay inside the collider boundary rather than just the camera center
        float minX = bounds.min.x + cameraHalfWidth;
        float maxX = bounds.max.x - cameraHalfWidth;
        float minY = bounds.min.y + cameraHalfHeight;
        float maxY = bounds.max.y - cameraHalfHeight;

        // If the map is smaller than the camera viewport on an axis, center on that axis
        position.x = (minX > maxX) ? bounds.center.x : Mathf.Clamp(position.x, minX, maxX);
        position.y = (minY > maxY) ? bounds.center.y : Mathf.Clamp(position.y, minY, maxY);
        return position;
    }

    /// <summary>
    /// Description:
    /// Gets the follow target's position
    /// Inputs: 
    /// none
    /// Returns: 
    /// Vector3
    /// </summary>
    /// <returns>Vector3: The position of the target assigned to this camera controller.</returns>
    public Vector3 GetTargetPosition()
    {
        return target ? target.position : transform.position;
    }

    /// <summary>
    /// Description:
    /// Finds and returns the mouse position in world coordinates.
    /// Returns Vector3.zero when the camera is in Locked mode since
    /// mouse tracking is not used in that mode.
    /// Inputs: 
    /// none
    /// Returns: 
    /// Vector3
    /// </summary>
    /// <returns>Vector3: The position of the player's mouse in world coordinates</returns>
    public Vector3 GetPlayerMousePosition()
    {
        if (cameraMovementStyle == CameraStyles.Locked)
        {
            return Vector3.zero;
        }
        return playerCamera.ScreenToWorldPoint(lookAction.ReadValue<Vector2>());
    }

    /// <summary>
    /// Description:
    /// Takes the target's position and mouse position, and returns the desired position of the camera.
    /// Locked  - Camera follows the target directly with no mouse influence.
    /// Overhead - Camera follows the target directly with no mouse influence.
    /// Free    - Camera interpolates between the target and mouse, clamped to maxDistanceFromTarget.
    /// Inputs: 
    /// Vector3 targetPosition, Vector3 offsetPosition
    /// Returns: 
    /// Vector3
    /// </summary>
    /// <param name="targetPosition"> The position of the target the camera is following. </param>
    /// <param name="mousePosition"> The position of the mouse in world space used to determine distance from the target. </param>
    /// <returns>Vector3: The position the camera should be at</returns>
    public Vector3 ComputeCameraPosition(Vector3 targetPosition, Vector3 mousePosition)
    {
        Vector3 result = Vector3.zero;
        switch (cameraMovementStyle)
        {
            case CameraStyles.Locked:
                // Follow the target directly; mouse has no influence in this mode
                result = targetPosition;
                break;
            case CameraStyles.Overhead:
                result = targetPosition;
                break;
            case CameraStyles.Free:
                Vector3 desiredPosition = Vector3.Lerp(targetPosition, mousePosition, freeCameraMouseTracking);
                Vector3 difference = desiredPosition - targetPosition;
                difference = Vector3.ClampMagnitude(difference, maxDistanceFromTarget);
                result = targetPosition + difference;
                break;
        }
        result.z = cameraZCoordinate;
        return result;
    }
}