using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A class which controlls player aiming and shooting
/// </summary>
public class ShootingController : MonoBehaviour
{
    [Header("GameObject/Component References")]
    [Tooltip("The projectile to be fired.")]
    public GameObject projectilePrefab = null;
    [Tooltip("The transform in the heirarchy which holds projectiles if any")]
    public Transform projectileHolder = null;

    [Header("Input Settings, Actions, & Controls")]
    [Tooltip("Whether this shooting controller is controled by the player")]
    public bool isPlayerControlled = false;
    public InputAction fireAction;

    [Header("Firing Settings")]
    [Tooltip("The minimum time between projectiles being fired.")]
    public float fireRate = 0.05f;

    [Tooltip("The maximum diference between the direction the" +
        " shooting controller is facing and the direction projectiles are launched.")]
    public float projectileSpread = 1.0f;

    [Tooltip("Angle in degrees between Extra Shots projectiles until the pattern reaches a full circle.")]
    [SerializeField] private float extraShotAngleInterval = 15.0f;

    // The last time this component was fired
    private float lastFired = Mathf.NegativeInfinity;

    [Header("Effects")]
    [Tooltip("The effect to create when this fires")]
    public GameObject fireEffect;

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is enabled
    /// </summary>
    void OnEnable()
    {
        fireAction.Enable();
    }

    /// <summary>
    /// Standard Unity function called whenever the attached gameobject is disabled
    /// </summary>
    void OnDisable()
    {
        fireAction.Disable();
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called at a fixed time interval (good for reading input and physics)
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void FixedUpdate()
    {
        ProcessInput();
    }

    /// <summary>
    /// Description:
    /// Standard unity function that runs when the script starts
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void Start()
    {
        if (fireAction.bindings.Count == 0 && isPlayerControlled)
        {
            Debug.LogWarning("The Fire Input Action does not have a binding set but is set to be player " +
                             "controlled! Make sure that it has a binding or the shooting controller will not shoot!");
        }
    }


    /// <summary>
    /// Description:
    /// Reads input from the input manager
    /// Inputs:
    /// None
    /// Returns:
    /// void (no return)
    /// </summary>
    void ProcessInput()
    {
        if (isPlayerControlled)
        {
            if (fireAction.bindings.Count == 0)
            {
                Debug.LogError("The Fire Input Action does not have a binding set! It must have a binding " +
                               "set in order to fire!");
            }
            if (fireAction.ReadValue<float>() >= 1)
            {
                Fire();
            }
        }   
    }

    /// <summary>
    /// Description:
    /// Fires a projectile if possible
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    public void Fire()
    {
        // If the cooldown is over fire a projectile
        if ((Time.timeSinceLevelLoad - lastFired) > GetEffectiveFireDelay())
        {
            SpawnProjectiles();

            if (fireEffect)
            {
                Instantiate(fireEffect, transform.position, transform.rotation, null);
            }

            // Restart the cooldown
            lastFired = Time.timeSinceLevelLoad;
        }
    }

    /// <summary>
    /// Description:
    /// Spawns a projectile and sets it up
    /// Inputs: 
    /// none
    /// Returns: 
    /// void (no return)
    /// </summary>
    public void SpawnProjectile()
    {
        SpawnProjectile(0.0f);
    }

    private void SpawnProjectiles()
    {
        int projectileCount = GetProjectileCount();
        if (projectileCount <= 1)
        {
            SpawnProjectile();
            return;
        }

        float angleStep = GetExtraShotAngleStep(projectileCount);
        float startAngle = -angleStep * (projectileCount - 1) * 0.5f;

        for (int i = 0; i < projectileCount; i++)
        {
            SpawnProjectile(startAngle + (angleStep * i));
        }
    }

    private void SpawnProjectile(float angleOffset)
    {
        // Check that the prefab is valid
        if (projectilePrefab)
        {
            // Create the projectile
            GameObject projectileGameObject = Instantiate(projectilePrefab, transform.position, transform.rotation, null);

            // Account for spread
            Vector3 rotationEulerAngles = projectileGameObject.transform.rotation.eulerAngles;
            rotationEulerAngles.z += angleOffset + Random.Range(-projectileSpread, projectileSpread);
            projectileGameObject.transform.rotation = Quaternion.Euler(rotationEulerAngles);

            // Keep the heirarchy organized
            if (projectileHolder&& GameObject.Find("ProjectileHolder"))
            {
                projectileHolder = GameObject.Find("ProjectileHolder").transform;
            }
            if (projectileHolder)
            {
                projectileGameObject.transform.SetParent(projectileHolder);
            }
        }
    }

    private float GetEffectiveFireDelay()
    {
        PlayerPowerUpController powerUps = GetComponentInParent<PlayerPowerUpController>();
        float fireRateMultiplier = powerUps ? powerUps.fireRateMultiplier : 1.0f;

        if (fireRateMultiplier <= 0.0f)
        {
            fireRateMultiplier = 1.0f;
        }

        return fireRate / fireRateMultiplier;
    }

    private int GetProjectileCount()
    {
        PlayerPowerUpController powerUps = GetComponentInParent<PlayerPowerUpController>();
        return powerUps ? powerUps.GetExtraShotProjectileCount() : 1;
    }

    private float GetExtraShotAngleStep(int projectileCount)
    {
        float interval = Mathf.Max(0.0f, extraShotAngleInterval);
        if (projectileCount <= 1 || interval <= 0.0f)
        {
            return 0.0f;
        }

        float totalArc = interval * (projectileCount - 1);
        if (totalArc < 360.0f)
        {
            return interval;
        }

        return 360.0f / projectileCount;
    }
}
