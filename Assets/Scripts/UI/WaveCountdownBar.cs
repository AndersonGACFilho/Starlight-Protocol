using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class updates a progress bar while waiting for the next wave to start.
/// It supports either Image.fillAmount or Slider value updates.
/// </summary>
public class WaveCountdownBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Wave manager that provides countdown progress. If empty, one is found in the scene.")]
    [SerializeField] private WaveManager waveManager;

    [Tooltip("Optional filled Image used as a countdown progress bar.")]
    [SerializeField] private Image fillImage;

    [Tooltip("Optional Slider used as a countdown progress bar.")]
    [SerializeField] private Slider slider;

    [Tooltip("Optional text label used to show countdown status.")]
    [SerializeField] private TextMeshProUGUI countdownLabel;

    [Tooltip("Optional CanvasGroup used to hide/show the whole countdown UI.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Display")]
    [Tooltip("When true, the bar is hidden while a wave is active.")]
    [SerializeField] private bool hideWhenWaveIsActive = true;

    [Tooltip("Text shown while wave is active, if a label is assigned.")]
    [SerializeField] private string inWaveLabel = "Wave in progress";

    [Tooltip("Label format used between waves. {0} receives the seconds left.")]
    [SerializeField] private string waitingLabelFormat = "Next wave in {0}s";

    /// <summary>
    /// Description:
    /// Standard Unity function called once when the script is loaded.
    /// Ensures there is a WaveManager reference.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void Awake()
    {
        if (!waveManager)
        {
            waveManager = FindObjectOfType<WaveManager>();
        }
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called every frame.
    /// Updates the bar fill, optional label text, and optional canvas visibility
    /// according to the current wave countdown state.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void Update()
    {
        if (!waveManager)
        {
            return;
        }

        bool waiting = waveManager.WaitingNextWave;
        float progress = waiting ? waveManager.NextWaveCountdownProgress : 0.0f;

        if (fillImage)
        {
            fillImage.fillAmount = progress;
        }

        if (slider)
        {
            slider.SetValueWithoutNotify(progress);
        }

        if (countdownLabel)
        {
            if (waiting)
            {
                int secondsLeft = Mathf.CeilToInt(waveManager.NextWaveCountdownRemaining);
                countdownLabel.text = string.Format(waitingLabelFormat, secondsLeft);
            }
            else
            {
                countdownLabel.text = inWaveLabel;
            }
        }

        if (canvasGroup && hideWhenWaveIsActive)
        {
            float alpha = waiting ? 1.0f : 0.0f;
            canvasGroup.alpha = alpha;
            canvasGroup.interactable = waiting;
            canvasGroup.blocksRaycasts = waiting;
        }
    }
}
