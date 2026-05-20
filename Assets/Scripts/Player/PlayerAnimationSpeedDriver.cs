using UnityEngine;

/// <summary>
/// Drives Animator "MoveSpeed" from runtime movement speed.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationSpeedDriver : MonoBehaviour
{
    [Header("Animator")]
    [Tooltip("Animator float parameter updated by this script.")]
    public string moveSpeedParameter = "MoveSpeed";

    [Header("Tuning")]
    [Tooltip("Final multiplier applied to computed speed.")]
    public float speedMultiplier = 1f;
    [Tooltip("When enabled, speed is normalized by controller moveSpeed.")]
    public bool normalizeByControllerSpeed = true;
    [Tooltip("Higher values respond faster; lower values smooth more.")]
    [Range(0f, 30f)] public float damping = 12f;

    private Animator _animator;
    private Rigidbody2D _rb;
    private BasePlayerController _controller;
    private int _moveSpeedHash;
    private Vector3 _lastPosition;
    private float _smoothedValue;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _controller = GetComponent<BasePlayerController>();
        _moveSpeedHash = Animator.StringToHash(moveSpeedParameter);
        _lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        float rawSpeed = GetCurrentWorldSpeed();

        if (normalizeByControllerSpeed && _controller && _controller.moveSpeed > 0.001f)
        {
            rawSpeed /= _controller.moveSpeed;
        }

        float target = rawSpeed * speedMultiplier;
        float t = 1f - Mathf.Exp(-damping * Time.deltaTime);
        _smoothedValue = Mathf.Lerp(_smoothedValue, target, t);

        _animator.SetFloat(_moveSpeedHash, _smoothedValue);
    }

    private float GetCurrentWorldSpeed()
    {
        if (_rb)
        {
            return _rb.linearVelocity.magnitude;
        }

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 currentPosition = transform.position;
        float speed = (currentPosition - _lastPosition).magnitude / dt;
        _lastPosition = currentPosition;
        return speed;
    }

    private void OnValidate()
    {
        _moveSpeedHash = Animator.StringToHash(moveSpeedParameter);
    }
}
