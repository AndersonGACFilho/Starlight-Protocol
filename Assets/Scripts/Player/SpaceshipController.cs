using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Spaceship controller:
/// rotates toward mouse and applies forward thrust with inertia.
/// </summary>
public class SpaceshipController : PhysicsPlayerController
{
    [Header("Spaceship")]
    public float maxSpeed = 8f;
    public float spaceshipDrag = 0.5f;
    public float rotationSpeed = 180f;

    [Header("Thruster")]
    public Light2D thrusterLight;
    public float lightBaseIntensity = 200f;
    public float lightIntensityMultiplier = 50f;
    [Tooltip("Oscillation speed (Hz) while accelerating.")]
    public float flameFlickerFrequency = 18f;
    [Tooltip("Oscillation amount applied over target intensity while accelerating.")]
    [Range(0f, 1f)] public float flameFlickerAmount = 0.2f;

    protected override void SetupRigidbody()
    {
        base.SetupRigidbody();

        if (!Rb)
        {
            return;
        }

        Rb.freezeRotation = true;
        Rb.linearDamping = spaceshipDrag;
    }

    protected override void HandleMovement(Vector2 moveInput, float speed)
    {
        if (!Rb)
        {
            return;
        }

        float thrust = Mathf.Max(0f, moveInput.y);
        if (thrust > 0f)
        {
            Rb.AddForce((Vector2)transform.up * thrust * speed);
        }

        if (Rb.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            Rb.linearVelocity = Vector2.ClampMagnitude(Rb.linearVelocity, maxSpeed);
        }
    }

    protected override void HandleRotation(Vector2 moveInput, Vector2 lookPosition)
    {
        if (Time.timeScale <= 0f)
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
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    protected override void Update()
    {
        base.Update();
        UpdateThrusterLight();
    }

    protected override void UpdateAnimations(Vector2 moveInput, float speed)
    {
        if (!animator)
        {
            return;
        }

        float throttle = Mathf.Max(0f, moveInput.y);
        bool isThrusting = throttle > 0.01f;

        animator.SetFloat(AnimMoveSpeed, isThrusting ? animationSpeed : 0f);
        animator.SetBool(AnimIsMoving, isThrusting);
    }

    private void UpdateThrusterLight()
    {
        if (!thrusterLight)
        {
            return;
        }

        float throttle = Mathf.Max(0f, ReadMoveInput().y);
        float targetIntensity = throttle * GetCurrentMoveSpeed() * lightIntensityMultiplier + lightBaseIntensity;

        if (throttle > 0.001f && flameFlickerAmount > 0f)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f * flameFlickerFrequency);
            float centeredWave = (wave * 2f) - 1f;
            targetIntensity *= 1f + (centeredWave * flameFlickerAmount);
        }

        thrusterLight.intensity = Mathf.Lerp(thrusterLight.intensity, targetIntensity, Time.deltaTime * 5f);
    }
}
