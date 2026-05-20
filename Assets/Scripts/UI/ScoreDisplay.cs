using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// This class inherits for the UIelement class and handles updating the score display
/// </summary>
public class ScoreDisplay : UIelement
{
    [Header("References")]
    [Tooltip("The text UI to use for display")]
    public TextMeshProUGUI displayText = null;

    [Header("Pulse Settings")]
    [Tooltip("Target multiplier applied to the original font size when the score changes.")]
    [SerializeField] private float expansionRatio = 1.5f;

    [Tooltip("Time used to expand the text.")]
    [SerializeField] private float expansionDuration = 0.08f;

    [Tooltip("Time the text stays expanded before shrinking.")]
    [SerializeField] private float holdDuration = 0.03f;

    [Tooltip("Time used to shrink the text back to its original size.")]
    [SerializeField] private float shrinkDuration = 0.25f;

    [Tooltip("Use unscaled time so UI animation is not affected by Time.timeScale.")]
    [SerializeField] private bool useUnscaledTime = true;

    private float originalFontSize;
    private string lastDisplayedScore;
    private Coroutine pulseCoroutine;

    public void Awake()
    {
        if (displayText == null)
        {
            Debug.LogError($"{nameof(ScoreDisplay)}: No display text assigned.");
            enabled = false;
            return;
        }

        originalFontSize = displayText.fontSize;
        lastDisplayedScore = displayText.text;
    }

    /// <summary>
    /// Description:
    /// Updates the score display
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    public void DisplayScore()
    {
        if (displayText == null)
        {
            return;
        }

        string currentScore = GameManager.score.ToString();

        if (currentScore == lastDisplayedScore)
        {
            return;
        }

        lastDisplayedScore = currentScore;
        displayText.text = currentScore;

        PlayPulse();
    }

    /// <summary>
    /// Description:
    /// Overides the virtual UpdateUI function and uses the DisplayScore to update the score display
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    public override void UpdateUI()
    {
        // This calls the base update UI function from the UIelement class
        base.UpdateUI();
        // The remaining code is only called for this sub-class of UIelement and not others
        DisplayScore();
    }

    /// <summary>
    /// Description:
    /// Plays the pulse animation when the score changes
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void PlayPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }

        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    /// <summary>
    /// Description:
    /// Coroutine that handles the pulse animation by first expanding the font size, then optionally holding it,
    /// and finally shrinking it back to the original size
    /// Inputs:
    /// none
    /// Returns:
    /// IEnumerator for the pulse animation routine 
    /// </summary>
    /// <returns></returns>
    private IEnumerator PulseRoutine()
    {
        float expandedFontSize = originalFontSize * expansionRatio;

        yield return AnimateFontSize(
            displayText.fontSize,
            expandedFontSize,
            expansionDuration
        );

        if (holdDuration > 0f)
        {
            yield return Wait(holdDuration);
        }

        yield return AnimateFontSize(
            displayText.fontSize,
            originalFontSize,
            shrinkDuration
        );

        displayText.fontSize = originalFontSize;
        pulseCoroutine = null;
    }

    /// <summary>
    /// Description:
    /// Coroutine that animates the font size from a starting value to a target value over a specified
    /// duration using linear interpolation
    /// Inputs:
    /// float from: The starting font size
    /// float to: The target font size
    /// float duration: The time over which to animate the font size
    /// </summary>
    /// <param name="from">The starting font size</param>
    /// <param name="to">The target font size</param>
    /// <param name="duration">The time over which to animate the font size</param>
    /// <returns>IEnumerator for the font size animation</returns>
    private IEnumerator AnimateFontSize(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            displayText.fontSize = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();

            float progress = Mathf.Clamp01(elapsed / duration);
            displayText.fontSize = Mathf.Lerp(from, to, progress);

            yield return null;
        }

        displayText.fontSize = to;
    }

    /// <summary>
    /// Description:
    /// Coroutine that waits for a specified duration using either scaled or unscaled time based on the
    /// useUnscaledTime setting
    /// Inputs:
    /// float duration: The time to wait before resuming the coroutine
    /// </summary>
    /// <param name="duration">The time to wait before resuming the coroutine</param>
    /// <returns>IEnumerator for the wait routine</returns>
    private IEnumerator Wait(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            yield return null;
        }
    }

    /// <summary>
    /// Description:
    /// Helper function to get the appropriate delta time based on the useUnscaledTime setting
    /// Inputs:
    /// none
    /// </summary>
    /// <returns>float: The delta time to use for animations</returns>
    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
