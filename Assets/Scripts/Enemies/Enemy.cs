using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A class which controls enemy behaviour
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The speed at which the enemy moves.")]
    public float moveSpeed = 5.0f;
    [Tooltip("The score value for defeating this enemy")]            
    public int scoreValue = 5;

    [Header("Inertia Movement")]
    [Tooltip("Rigidbody2D used for inertia-based movement modes.")]
    public Rigidbody2D movementRigidbody;
    [Tooltip("Force applied every physics step in inertia movement modes.")]
    public float inertiaThrust = 15.0f;
    [Tooltip("Maximum speed for inertia movement modes.")]
    public float maxInertiaSpeed = 6.0f;
    [Tooltip("Linear damping used by inertia movement modes.")]
    [Range(0f, 10f)] public float inertiaDrag = 0.5f;
    [Tooltip("Distance from target where FollowTargetInertia starts braking.")]
    public float inertiaStopDistance = 1.2f;
    [Tooltip("Braking force applied when inside stop distance in FollowTargetInertia.")]
    public float inertiaBrakeForce = 20f;
    [Tooltip("Velocity magnitude considered fully stopped in FollowTargetInertia.")]
    public float inertiaStopVelocity = 0.1f;

    [Header("Following Settings")]
    [Tooltip("The transform of the object that this enemy should follow.")]
    public Transform followTarget;
    [Tooltip("The distance at which the enemy begins following the follow target.")]
    public float followRange = 10.0f;

    [Header("Shooting")]
    [Tooltip("The enemy's gun components")]
    public List<ShootingController> guns;    

    public static event Action<Enemy> EnemyDefeated;

    [Header("Target Lookup")]
    [Tooltip("Tag used to find the player when no follow target is assigned.")]
    [SerializeField] private string playerTag = "Player";
    /// <summary>
    /// Enum to help with shooting modes
    /// </summary>
    public enum ShootMode { None, ShootAll };

    [Tooltip("The way the enemy shoots:\n" +
        "None: Enemy does not shoot.\n" +
        "ShootAll: Enemy fires all guns whenever it can.")]
    public ShootMode shootMode = ShootMode.ShootAll;

    /// <summary>
    /// Enum to help wih different movement modes
    /// </summary>
    public enum MovementModes { NoMovement, FollowTarget, Scroll, FollowTargetInertia, ScrollInertia };

    [Tooltip("The way this enemy will move\n" +
        "NoMovement: This enemy will not move.\n" +
        "FollowTarget: This enemy will follow the assigned target.\n" +
        "Scroll: This enemy will move in one horizontal direction only.\n" +
        "FollowTargetInertia: Follows target with force + drag (inertia).\n" +
        "ScrollInertia: Scrolls with force + drag (inertia).")]
    public MovementModes movementMode = MovementModes.FollowTarget;

    // The direction that this enemy will try to scroll if it is set as a scrolling enemy.
    [SerializeField] private Vector3 scrollDirection = Vector3.right;


    private EnemyMovementModule _movementModule;
    private EnemyDefeatModule _defeatModule;

    private void Awake()
    {
        _movementModule = new EnemyMovementModule(transform, movementRigidbody);
        _defeatModule = new EnemyDefeatModule();
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called after update every frame
    /// Inputs: 
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void LateUpdate()
    {
        HandleBehaviour();       
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called once before the first call to Update
    /// Input:
    /// none
    /// Return:
    /// void (no return)
    /// </summary>
    private void Start()
    {
        ResolveFollowTarget();

        if (movementMode == MovementModes.Scroll || movementMode == MovementModes.ScrollInertia)
        {
            _movementModule.InitializeScroll(scrollDirection);
        }

        if (EnemyMovementModule.UsesInertia(movementMode))
        {
            ResolveInertiaRigidbody();
        }
    }

    /// <summary>
    /// Description:
    /// Finds the follow target for this enemy if one is not already assigned
    /// First it checks if the game manager has a player assigned, then it checks for a gameobject with the player tag,
    /// and if neither of those work then this enemy will not have a follow target and will not move in follow
    /// movement mode
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)    
    /// </summary>
    private void ResolveFollowTarget()
    {
        if (followTarget)
        {
            return;
        }

        if (GameManager.instance && GameManager.instance.player)
        {
            followTarget = GameManager.instance.player.transform;
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);

        if (player)
        {
            followTarget = player.transform;
        }
    }
    /// <summary>
    /// Description:
    /// Handles moving and shooting in accordance with the enemy's set behaviour
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void HandleBehaviour()
    {
        if (!EnemyMovementModule.UsesInertia(movementMode))
        {
            _movementModule.ApplyKinematicMovement(movementMode, moveSpeed, followTarget, followRange, scrollDirection);
        }

        EnemyShootingModule.TryShoot(shootMode, guns);
    }

    private void FixedUpdate()
    {
        if (!EnemyMovementModule.UsesInertia(movementMode))
        {
            return;
        }

        ResolveInertiaRigidbody();
        _movementModule.ApplyInertiaMovement(
            movementMode,
            inertiaThrust,
            maxInertiaSpeed,
            inertiaDrag,
            inertiaStopDistance,
            inertiaBrakeForce,
            inertiaStopVelocity,
            followTarget,
            followRange,
            scrollDirection
        );
    }

    private void ResolveInertiaRigidbody()
    {
        if (!movementRigidbody)
        {
            movementRigidbody = GetComponent<Rigidbody2D>();
        }

        if (!movementRigidbody)
        {
            Debug.LogWarning($"{nameof(Enemy)} on {name} is using inertia mode without Rigidbody2D.");
            return;
        }

        _movementModule.SetRigidbody(movementRigidbody);
    }

    /// <summary>
    /// Description:
    /// This is meant to be called before destroying the gameobject associated with this script
    /// It can not be replaced with OnDestroy() because of Unity's inability to distiguish between unloading a scene
    /// and destroying the gameobject from the Destroy function
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    public void DoBeforeDestroy()
    {
        if (_defeatModule == null)
        {
            _defeatModule = new EnemyDefeatModule();
        }

        _defeatModule.RegisterDefeat(this, scoreValue, enemy => EnemyDefeated?.Invoke(enemy));
    }
}
