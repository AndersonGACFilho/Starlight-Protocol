using UnityEngine;

/// <summary>
/// Handles enemy movement and rotation logic.
/// </summary>
public sealed class EnemyMovementModule
{
    private const float AxisEpsilon = 0.0001f;

    private readonly Transform _owner;
    private Rigidbody2D _rb;
    private Vector3 _originalPosition;
    private Vector3 _turnPosition;
    private Vector3 _currentScrollDirection;
    private bool _scrollInitialized;

    public EnemyMovementModule(Transform owner, Rigidbody2D rb = null)
    {
        _owner = owner;
        _rb = rb;
    }

    public void SetRigidbody(Rigidbody2D rb)
    {
        _rb = rb;
    }

    public static bool UsesInertia(Enemy.MovementModes movementMode)
    {
        return movementMode == Enemy.MovementModes.FollowTargetInertia
               || movementMode == Enemy.MovementModes.ScrollInertia;
    }

    public void InitializeScroll(Vector3 initialScrollDirection)
    {
        _originalPosition = _owner.position;
        _currentScrollDirection = initialScrollDirection;
        _turnPosition = _originalPosition + _currentScrollDirection;
        _scrollInitialized = true;
    }

    public void ApplyKinematicMovement(
        Enemy.MovementModes movementMode,
        float moveSpeed,
        Transform followTarget,
        float followRange,
        Vector3 initialScrollDirection
    )
    {
        Vector3 movement = GetDesiredMovementDirection(movementMode, followTarget, followRange, initialScrollDirection)
                           * (moveSpeed * Time.deltaTime);
        Quaternion rotation = GetDesiredRotation(movementMode, followTarget);

        _owner.position += movement;
        _owner.rotation = rotation;
    }

    public void ApplyInertiaMovement(
        Enemy.MovementModes movementMode,
        float thrust,
        float maxSpeed,
        float drag,
        float stopDistance,
        float brakeForce,
        float stopVelocity,
        Transform followTarget,
        float followRange,
        Vector3 initialScrollDirection
    )
    {
        if (!_rb)
        {
            return;
        }

        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _rb.linearDamping = drag;

        Vector3 desiredDirection = GetDesiredMovementDirection(
            movementMode,
            followTarget,
            followRange,
            initialScrollDirection
        );

        if (movementMode == Enemy.MovementModes.FollowTargetInertia && followTarget)
        {
            float distanceToTarget = (followTarget.position - _owner.position).magnitude;
            if (distanceToTarget <= stopDistance)
            {
                float speed = _rb.linearVelocity.magnitude;
                if (speed <= stopVelocity)
                {
                    _rb.linearVelocity = Vector2.zero;
                }
                else if (speed > 0.0001f)
                {
                    _rb.AddForce(-_rb.linearVelocity.normalized * brakeForce);
                }

                _owner.rotation = GetDesiredRotation(movementMode, followTarget);
                return;
            }
        }

        if (desiredDirection.sqrMagnitude > 0.0001f)
        {
            _rb.AddForce((Vector2)desiredDirection.normalized * thrust);
        }

        float maxSpeedSqr = maxSpeed * maxSpeed;
        if (_rb.linearVelocity.sqrMagnitude > maxSpeedSqr)
        {
            _rb.linearVelocity = Vector2.ClampMagnitude(_rb.linearVelocity, maxSpeed);
        }

        _owner.rotation = GetDesiredRotation(movementMode, followTarget);
    }

    private Vector3 GetDesiredMovementDirection(
        Enemy.MovementModes movementMode,
        Transform followTarget,
        float followRange,
        Vector3 initialScrollDirection
    )
    {
        switch (movementMode)
        {
            case Enemy.MovementModes.FollowTarget:
            case Enemy.MovementModes.FollowTargetInertia:
                return GetFollowDirection(followTarget, followRange);
            case Enemy.MovementModes.Scroll:
            case Enemy.MovementModes.ScrollInertia:
                return GetScrollDirectionVector(initialScrollDirection);
            default:
                return Vector3.zero;
        }
    }

    private Quaternion GetDesiredRotation(Enemy.MovementModes movementMode, Transform followTarget)
    {
        switch (movementMode)
        {
            case Enemy.MovementModes.FollowTarget:
            case Enemy.MovementModes.FollowTargetInertia:
                return GetFollowRotation(followTarget);
            case Enemy.MovementModes.Scroll:
            case Enemy.MovementModes.ScrollInertia:
                return Quaternion.identity;
            default:
                return _owner.rotation;
        }
    }

    private Vector3 GetFollowDirection(Transform followTarget, float followRange)
    {
        if (!followTarget)
        {
            return Vector3.zero;
        }

        Vector3 toTarget = followTarget.position - _owner.position;
        if (toTarget.magnitude >= followRange)
        {
            return Vector3.zero;
        }

        return toTarget.normalized;
    }

    private Quaternion GetFollowRotation(Transform followTarget)
    {
        if (!followTarget)
        {
            return _owner.rotation;
        }

        float angle = Vector3.SignedAngle(
            Vector3.down,
            (followTarget.position - _owner.position).normalized,
            Vector3.forward
        );
        return Quaternion.Euler(0f, 0f, angle);
    }

    private Vector3 GetScrollDirectionVector(Vector3 initialScrollDirection)
    {
        EnsureScrollInitialized(initialScrollDirection);
        _currentScrollDirection = GetScrollDirection();
        return _currentScrollDirection.normalized;
    }

    private void EnsureScrollInitialized(Vector3 initialScrollDirection)
    {
        if (_scrollInitialized)
        {
            return;
        }

        InitializeScroll(initialScrollDirection);
    }

    private Vector3 GetScrollDirection()
    {
        bool overX = false;
        bool overY = false;
        bool overZ = false;

        Vector3 toTurnPosition = _turnPosition - _owner.position;

        if (IsAxisDone(toTurnPosition.x, _currentScrollDirection.x))
        {
            overX = true;
            _owner.position = new Vector3(_turnPosition.x, _owner.position.y, _owner.position.z);
        }

        if (IsAxisDone(toTurnPosition.y, _currentScrollDirection.y))
        {
            overY = true;
            _owner.position = new Vector3(_owner.position.x, _turnPosition.y, _owner.position.z);
        }

        if (IsAxisDone(toTurnPosition.z, _currentScrollDirection.z))
        {
            overZ = true;
            _owner.position = new Vector3(_owner.position.x, _owner.position.y, _turnPosition.z);
        }

        if (overX && overY && overZ)
        {
            _turnPosition = _originalPosition - _currentScrollDirection;
            return -_currentScrollDirection;
        }

        return _currentScrollDirection;
    }

    private static bool IsAxisDone(float deltaToTarget, float direction)
    {
        if (deltaToTarget <= AxisEpsilon && deltaToTarget >= -AxisEpsilon)
        {
            return true;
        }

        if (Mathf.Approximately(direction, 0f))
        {
            return false;
        }

        return !Mathf.Approximately(Mathf.Sign(deltaToTarget), Mathf.Sign(direction));
    }
}
