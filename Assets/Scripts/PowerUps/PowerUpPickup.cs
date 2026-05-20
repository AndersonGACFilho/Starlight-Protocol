using UnityEngine;

/// <summary>
/// Representa um power-up coletável.
/// Ao colidir com o player, aplica o efeito e destrói o objeto.
/// </summary>
public class PowerUpPickup : MonoBehaviour
{
    [Header("Power-up")]
    [Tooltip("Type of effect applied when the player collects this pickup. Health is instant; other types are temporary or infinite.")]
    [SerializeField] private PowerUpType powerUpType;

    [Tooltip("Effect duration in seconds. A value of 0 makes non-health power-ups infinite. Ignored by Health.")]
    [SerializeField] private float duration = 5.0f;

    [Tooltip("Amount used by instant effects. Health uses this as healing, or max-health increase when already full.")]
    [SerializeField] private int amount = 1;

    [Header("Audio")]
    [Tooltip("Sound played when this pickup is collected.")]
    [SerializeField] private AudioClip pickupSound;

    [Tooltip("Volume used when playing the pickup sound.")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float pickupSoundVolume = 1.0f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (powerUpType == PowerUpType.Health)
        {
            Health health = other.GetComponentInParent<Health>();
            if (!health)
            {
                return;
            }

            health.ReceiveHealing(amount);
            CompletePickup();
            return;
        }

        PlayerPowerUpController powerUpController =
            other.GetComponentInParent<PlayerPowerUpController>();

        if (!powerUpController)
        {
            return;
        }

        powerUpController.ApplyPowerUp(powerUpType, duration, amount);
        CompletePickup();
    }

    private void CompletePickup()
    {
        PlayPickupSound();
        Destroy(gameObject);
    }

    private void PlayPickupSound()
    {
        if (!pickupSound)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupSoundVolume);
    }
}
