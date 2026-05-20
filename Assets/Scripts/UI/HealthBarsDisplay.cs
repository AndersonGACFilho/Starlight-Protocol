using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays player health as vertical red bars.
/// </summary>
public class HealthBarsDisplay : UIelement
{
    [Header("References")]
    [Tooltip("Health component to display. If empty, the display searches for the Player tag.")]
    [SerializeField] private Health playerHealth;

    [Tooltip("Optional parent for generated health bars. If empty, bars are generated under this object.")]
    [SerializeField] private RectTransform barContainer;

    [Header("Layout")]
    [Tooltip("Maximum number of vertical bars that can be shown.")]
    [SerializeField] private int maxBars = 5;

    [Tooltip("Width and height of each generated vertical health bar.")]
    [SerializeField] private Vector2 barSize = new Vector2(6.0f, 28.0f);

    [Tooltip("Horizontal spacing between generated health bars.")]
    [SerializeField] private float barSpacing = 5.0f;

    [Tooltip("Anchored position of the first generated health bar.")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(34.0f, -34.0f);

    [Header("Colors")]
    [Tooltip("Color used for current/filled health bars.")]
    [SerializeField] private Color activeColor = new Color(1.0f, 0.05f, 0.08f, 1.0f);

    [Tooltip("Color used for missing/empty health bars.")]
    [SerializeField] private Color inactiveColor = new Color(1.0f, 0.05f, 0.08f, 0.2f);

    private readonly List<Image> _bars = new List<Image>();

    private void Awake()
    {
        if (!playerHealth)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                playerHealth = player.GetComponent<Health>();
            }
        }

        EnsureBars();
    }

    private void Update()
    {
        UpdateUI();
    }

    public override void UpdateUI()
    {
        base.UpdateUI();
        UpdateBars();
    }

    private void EnsureBars()
    {
        RectTransform parent = barContainer ? barContainer : transform as RectTransform;
        if (!parent)
        {
            return;
        }

        for (int i = 0; i < maxBars; i++)
        {
            GameObject bar = new GameObject($"HealthBar_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform barTransform = bar.GetComponent<RectTransform>();
            barTransform.SetParent(parent, false);
            barTransform.anchorMin = new Vector2(0.0f, 1.0f);
            barTransform.anchorMax = new Vector2(0.0f, 1.0f);
            barTransform.pivot = new Vector2(0.5f, 0.5f);
            barTransform.sizeDelta = barSize;
            barTransform.anchoredPosition = anchoredPosition + new Vector2(i * (barSize.x + barSpacing), 0.0f);

            Image image = bar.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = inactiveColor;

            _bars.Add(image);
        }
    }

    private void UpdateBars()
    {
        if (!playerHealth)
        {
            return;
        }

        int maximumValue = playerHealth.useLives ? playerHealth.maximumLives : playerHealth.maximumHealth;
        int currentValue = playerHealth.useLives ? playerHealth.currentLives : playerHealth.currentHealth;
        int visibleBars = Mathf.Clamp(maximumValue, 0, maxBars);
        int activeBars = Mathf.Clamp(currentValue, 0, visibleBars);

        for (int i = 0; i < _bars.Count; i++)
        {
            Image bar = _bars[i];
            if (!bar)
            {
                continue;
            }

            bool isVisible = i < visibleBars;
            bar.gameObject.SetActive(isVisible);
            bar.color = i < activeBars ? activeColor : inactiveColor;
        }
    }
}
