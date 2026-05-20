using TMPro;
using UnityEngine;

/// <summary>
/// This class inherits from UIelement and displays wave information on the HUD.
/// </summary>
public class WaveDisplay : UIelement
{
    [Header("References")]
    [Tooltip("Wave manager that provides current wave state. If empty, one is found in the scene.")]
    [SerializeField] private WaveManager waveManager;

    [Tooltip("Text label used to show the current wave number.")]
    [SerializeField] private TextMeshProUGUI waveText;

    [Tooltip("Text label used to show enemies remaining in the current finite wave.")]
    [SerializeField] private TextMeshProUGUI enemiesLeftText;

    [Tooltip("Text label used to show active/waiting wave status.")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Formatting")]
    [Tooltip("Format string for the current wave. {0} receives the wave number.")]
    [SerializeField] private string waveFormat = "Wave: {0}";

    [Tooltip("Format string for enemies remaining. {0} receives the remaining count.")]
    [SerializeField] private string enemiesLeftFormat = "Enemies Left: {0}";

    [Tooltip("Status text shown while a wave is active.")]
    [SerializeField] private string inWaveStatus = "Fight!";

    [Tooltip("Status format shown between waves. {0} receives seconds remaining.")]
    [SerializeField] private string waitingStatusFormat = "Next wave in {0}s";

    /// <summary>
    /// Description:
    /// Standard Unity function called once when the script instance is being loaded.
    /// Ensures there is a valid WaveManager reference.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void Awake()
    {
        if (!waveManager)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called every frame.
    /// Keeps the HUD updated in real time during gameplay.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void Update()
    {
        DisplayWaveInfo();
    }

    /// <summary>
    /// Description:
    /// Overrides the virtual UpdateUI function and updates all wave-related text fields.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    public override void UpdateUI()
    {
        base.UpdateUI();
        DisplayWaveInfo();
    }

    /// <summary>
    /// Description:
    /// Updates the wave number, enemies left, and status text.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void DisplayWaveInfo()
    {
        if (!waveManager)
        {
            return;
        }

        if (waveText)
        {
            waveText.text = string.Format(waveFormat, waveManager.CurrentWave);
        }

        if (enemiesLeftText)
        {
            enemiesLeftText.text = string.Format(enemiesLeftFormat, waveManager.EnemiesRemaining);
        }

        if (statusText)
        {
            if (waveManager.WaitingNextWave)
            {
                int secondsLeft = Mathf.CeilToInt(waveManager.NextWaveCountdownRemaining);
                statusText.text = string.Format(waitingStatusFormat, secondsLeft);
            }
            else
            {
                statusText.text = inWaveStatus;
            }
        }
    }
}
