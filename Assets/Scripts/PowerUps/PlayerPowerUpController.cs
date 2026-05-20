using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla os efeitos temporários de power-ups no player.
/// </summary>
public class PlayerPowerUpController : MonoBehaviour
{
    [Header("Power-up State")]
    [Tooltip("Current fire-rate multiplier. Values above 1 indicate Rapid Fire is active.")]
    public float fireRateMultiplier = 1.0f;

    [Tooltip("Whether the player currently fires triple shots.")]
    public bool tripleShotEnabled = false;

    [Tooltip("Whether the player currently has a shield available to absorb damage.")]
    public bool shieldEnabled = false;

    [Tooltip("Child visual on the player that is enabled while Shield is active. Assign the player's shield object here, not the pickup prefab.")]
    public GameObject shieldPrefab = null;

    [Tooltip("Current movement speed multiplier. Values above 1 indicate Speed Boost is active.")]
    public float speedMultiplier = 1.0f;

    // Expiration times for temporary power-ups.
    private float rapidFireEndTime = 0.0f;
    private float tripleShotEndTime = 0.0f;
    private float shieldEndTime = 0.0f;
    private float speedBoostEndTime = 0.0f;

    private bool rapidFireInfinite;
    private bool tripleShotInfinite;
    private bool shieldInfinite;
    private bool speedBoostInfinite;

    // Active coroutine handlers (kept so repeated pickups extend instead of conflicting).
    private Coroutine rapidFireCoroutine;
    private Coroutine tripleShotCoroutine;
    private Coroutine shieldCoroutine;
    private Coroutine speedBoostCoroutine;

    private int rapidFireStacks;
    private int tripleShotStacks;
    private int shieldStacks;
    private int speedBoostStacks;

    private void Awake()
    {
        ResetTemporaryPowerUps();
    }

    /// <summary>
    /// Description:
    /// Indicates whether any temporary power-up effect is currently active.
    /// Inputs:
    /// none
    /// Returns:
    /// bool: true if there is any active temporary power-up effect, otherwise false
    /// </summary>
    public bool HasActivePowerUp => fireRateMultiplier > 1.0f || tripleShotEnabled || shieldEnabled || speedMultiplier > 1.0f;

    /// <summary>
    /// Description:
    /// Gets a readable name for the current active power-up.
    /// Inputs:
    /// none
    /// Returns:
    /// string: The name of the active power-up, or "None" if no effect is active
    /// </summary>
    public string ActivePowerUpDisplayName
    {
        get
        {
            if (shieldEnabled)
            {
                return "Shield";
            }

            if (tripleShotEnabled)
            {
                return "Extra Shots";
            }

            if (fireRateMultiplier > 1.0f)
            {
                return "Rapid Fire";
            }

            if (speedMultiplier > 1.0f)
            {
                return "Speed Boost";
            }

            return "None";
        }
    }

    /// <summary>
    /// Description:
    /// Fills the provided list with every temporary power-up effect currently active.
    /// Inputs:
    /// List&lt;PowerUpType&gt; activePowerUps
    /// Returns:
    /// void (no return)
    /// </summary>
    /// <param name="activePowerUps">List that receives the active power-up types</param>
    public void GetActivePowerUps(List<PowerUpType> activePowerUps)
    {
        if (activePowerUps == null)
        {
            return;
        }

        activePowerUps.Clear();

        if (fireRateMultiplier > 1.0f)
        {
            activePowerUps.Add(PowerUpType.RapidFire);
        }

        if (tripleShotEnabled)
        {
            activePowerUps.Add(PowerUpType.TripleShot);
        }

        if (shieldEnabled)
        {
            activePowerUps.Add(PowerUpType.Shield);
        }

        if (speedMultiplier > 1.0f)
        {
            activePowerUps.Add(PowerUpType.SpeedBoost);
        }
    }

    /// <summary>
    /// Description:
    /// Gets the remaining active time for a temporary power-up.
    /// Inputs:
    /// PowerUpType powerUpType
    /// Returns:
    /// float: seconds remaining, or 0 if the power-up is inactive or instant
    /// </summary>
    /// <param name="powerUpType">Power-up type to query</param>
    public float GetRemainingDuration(PowerUpType powerUpType)
    {
        float endTime = 0.0f;

        switch (powerUpType)
        {
            case PowerUpType.RapidFire:
                endTime = rapidFireEndTime;
                if (rapidFireInfinite)
                {
                    return float.PositiveInfinity;
                }
                break;

            case PowerUpType.TripleShot:
                endTime = tripleShotEndTime;
                if (tripleShotInfinite)
                {
                    return float.PositiveInfinity;
                }
                break;

            case PowerUpType.Shield:
                endTime = shieldEndTime;
                if (shieldInfinite)
                {
                    return float.PositiveInfinity;
                }
                break;

            case PowerUpType.SpeedBoost:
                endTime = speedBoostEndTime;
                if (speedBoostInfinite)
                {
                    return float.PositiveInfinity;
                }
                break;

            default:
                return 0.0f;
        }

        return Mathf.Max(0.0f, endTime - Time.time);
    }

    public int GetStackCount(PowerUpType powerUpType)
    {
        switch (powerUpType)
        {
            case PowerUpType.RapidFire:
                return rapidFireStacks;

            case PowerUpType.TripleShot:
                return tripleShotStacks;

            case PowerUpType.Shield:
                return shieldStacks;

            case PowerUpType.SpeedBoost:
                return speedBoostStacks;

            default:
                return 0;
        }
    }

    /// <summary>
    /// Description:
    /// Gets how many projectiles the player should fire after Extra Shots stacks are applied.
    /// Each active stack adds two extra projectiles to the base shot.
    /// Inputs:
    /// none
    /// Returns:
    /// int: projectile count for the next player shot
    /// </summary>
    public int GetExtraShotProjectileCount()
    {
        if (!tripleShotEnabled)
        {
            return 1;
        }

        return 1 + (tripleShotStacks * 2);
    }

    /// <summary>
    /// Description:
    /// Applies a power-up effect to the player.
    /// Health is applied instantly while the other power-ups run as timed coroutines.
    /// Inputs:
    /// PowerUpType powerUpType
    /// float duration
    /// int amount
    /// Returns:
    /// void (no return)
    /// </summary>
    /// <param name="powerUpType">Which power-up type should be applied</param>
    /// <param name="duration">How long the temporary effect should last</param>
    /// <param name="amount">The amount used by non-temporary effects such as healing</param>
    public void ApplyPowerUp(PowerUpType powerUpType, float duration, int amount)
    {
        float clampedDuration = Mathf.Max(0.0f, duration);

        switch (powerUpType)
        {
            case PowerUpType.Health:
                ApplyHealth(amount);
                break;

            case PowerUpType.RapidFire:
                rapidFireStacks++;
                rapidFireInfinite |= clampedDuration <= 0.0f;
                ApplyTimedPowerUp(
                    ref rapidFireEndTime,
                    clampedDuration,
                    ref rapidFireCoroutine,
                    ApplyRapidFire
                );
                break;

            case PowerUpType.TripleShot:
                tripleShotStacks++;
                tripleShotInfinite |= clampedDuration <= 0.0f;
                ApplyTimedPowerUp(
                    ref tripleShotEndTime,
                    clampedDuration,
                    ref tripleShotCoroutine,
                    ApplyTripleShot
                );
                break;

            case PowerUpType.Shield:
                shieldStacks++;
                shieldInfinite |= clampedDuration <= 0.0f;
                ApplyTimedPowerUp(
                    ref shieldEndTime,
                    clampedDuration,
                    ref shieldCoroutine,
                    ApplyShield
                );
                break;

            case PowerUpType.SpeedBoost:
                speedBoostStacks++;
                speedBoostInfinite |= clampedDuration <= 0.0f;
                ApplyTimedPowerUp(
                    ref speedBoostEndTime,
                    clampedDuration,
                    ref speedBoostCoroutine,
                    ApplySpeedBoost
                );
                break;
        }
    }

    /// <summary>
    /// Description:
    /// Updates a timed power-up expiration and starts the routine only once.
    /// Additional pickups extend the expiration time instead of creating conflicting routines.
    /// Inputs:
    /// ref float endTime
    /// float duration
    /// ref Coroutine routine
    /// System.Func&lt;IEnumerator&gt; routineFactory
    /// Returns:
    /// void (no return)
    /// </summary>
    /// <param name="endTime">Reference to the power-up expiration time</param>
    /// <param name="duration">How much time should be added/extended</param>
    /// <param name="routine">Reference to the coroutine handler for this power-up</param>
    /// <param name="routineFactory">Factory used to build the coroutine routine</param>
    private void ApplyTimedPowerUp(
        ref float endTime,
        float duration,
        ref Coroutine routine,
        System.Func<IEnumerator> routineFactory
    )
    {
        if (duration > 0.0f)
        {
            endTime = Mathf.Max(endTime, Time.time) + duration;
        }

        if (routine == null)
        {
            routine = StartCoroutine(routineFactory());
        }
    }

    /// <summary>
    /// Description:
    /// Applies healing to the player by using the Health component.
    /// Inputs:
    /// int amount
    /// Returns:
    /// void (no return)
    /// </summary>
    /// <param name="amount">The amount of healing to apply</param>
    private void ApplyHealth(int amount)
    {
        Health health = GetComponent<Health>();

        if (!health)
        {
            Debug.LogWarning($"{nameof(PlayerPowerUpController)}: No Health component found for healing.");
            return;
        }

        health.ReceiveHealing(amount);
    }

    /// <summary>
    /// Description:
    /// Temporarily increases fire rate.
    /// Inputs:
    /// none
    /// Returns:
    /// IEnumerator
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator ApplyRapidFire()
    {
        fireRateMultiplier = 2.0f;

        while (rapidFireInfinite || Time.time < rapidFireEndTime)
        {
            yield return null;
        }

        fireRateMultiplier = 1.0f;
        rapidFireStacks = 0;
        rapidFireInfinite = false;
        rapidFireCoroutine = null;
    }

    /// <summary>
    /// Description:
    /// Temporarily enables triple shot.
    /// Inputs:
    /// none
    /// Returns:
    /// IEnumerator
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator ApplyTripleShot()
    {
        tripleShotEnabled = true;

        while (tripleShotInfinite || Time.time < tripleShotEndTime)
        {
            yield return null;
        }

        tripleShotEnabled = false;
        tripleShotStacks = 0;
        tripleShotInfinite = false;
        tripleShotCoroutine = null;
    }

    /// <summary>
    /// Description:
    /// Temporarily enables shield protection.
    /// Inputs:
    /// none
    /// Returns:
    /// IEnumerator
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator ApplyShield()
    {
        shieldEnabled = true;
        SetShieldVisualActive(true);

        while (shieldEnabled && (shieldInfinite || Time.time < shieldEndTime))
        {
            yield return null;
        }

        SetShieldVisualActive(false);
        shieldEnabled = false;
        shieldStacks = 0;
        shieldInfinite = false;
        shieldCoroutine = null;
    }

    /// <summary>
    /// Description:
    /// Temporarily increases player speed.
    /// Inputs:
    /// none
    /// Returns:
    /// IEnumerator
    /// </summary>
    /// <returns>IEnumerator for coroutine execution</returns>
    private IEnumerator ApplySpeedBoost()
    {
        speedMultiplier = 1.5f;

        while (speedBoostInfinite || Time.time < speedBoostEndTime)
        {
            yield return null;
        }

        speedMultiplier = 1.0f;
        speedBoostStacks = 0;
        speedBoostInfinite = false;
        speedBoostCoroutine = null;
    }

    /// <summary>
    /// Description:
    /// Consumes the active shield if one is available.
    /// Inputs:
    /// none
    /// Returns:
    /// bool: true when a shield was consumed, otherwise false
    /// </summary>
    public bool TryConsumeShield()
    {
        if (!shieldEnabled)
        {
            return false;
        }

        shieldEnabled = false;
        shieldEndTime = 0.0f;
        shieldStacks = 0;
        shieldInfinite = false;
        SetShieldVisualActive(false);
        return true;
    }

    private void ResetTemporaryPowerUps()
    {
        fireRateMultiplier = 1.0f;
        tripleShotEnabled = false;
        shieldEnabled = false;
        speedMultiplier = 1.0f;

        rapidFireEndTime = 0.0f;
        tripleShotEndTime = 0.0f;
        shieldEndTime = 0.0f;
        speedBoostEndTime = 0.0f;

        rapidFireStacks = 0;
        tripleShotStacks = 0;
        shieldStacks = 0;
        speedBoostStacks = 0;

        rapidFireInfinite = false;
        tripleShotInfinite = false;
        shieldInfinite = false;
        speedBoostInfinite = false;

        SetShieldVisualActive(false);
    }

    private void SetShieldVisualActive(bool active)
    {
        if (shieldPrefab)
        {
            shieldPrefab.SetActive(active);
        }
    }
}
