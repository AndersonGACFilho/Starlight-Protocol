using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This class inherits from UIelement and displays the player's active power-up.
/// </summary>
public class PowerUpDisplay : UIelement
{
    [Serializable]
    private struct PowerUpIcon
    {
        [Tooltip("Power-up type represented by this UI icon.")]
        public PowerUpType type;

        [Tooltip("Sprite shown for this power-up type in the HUD.")]
        public Sprite sprite;
    }

    [Header("References")]
    [Tooltip("Player power-up state source. If empty, the display searches for one in the scene.")]
    [SerializeField] private PlayerPowerUpController playerPowerUpController;

    [Tooltip("Optional legacy text label. Hidden automatically when image slots are used.")]
    [SerializeField] private TextMeshProUGUI displayText;

    [Tooltip("Optional parent for generated icon slots. If empty, the text label parent is used.")]
    [SerializeField] private RectTransform iconContainer;

    [Tooltip("Optional prebuilt image slots. If empty, this component creates slots at runtime.")]
    [SerializeField] private Image[] powerUpImages;

    [Header("Formatting")]
    [Tooltip("Legacy text format used only if the text label remains visible.")]
    [SerializeField] private string activePowerUpFormat = "Power-up: {0}";

    [Tooltip("Legacy text shown when no temporary power-up is active.")]
    [SerializeField] private string noPowerUpText = "Power-up: None";

    [Tooltip("Disable the legacy text label when icons are available.")]
    [SerializeField] private bool hideTextWhenUsingImages = true;

    [Header("Icons")]
    [Tooltip("Mapping between temporary power-up types and their HUD sprites.")]
    [SerializeField] private PowerUpIcon[] powerUpIcons;

    [Tooltip("Fixed order of pickup slots shown in the HUD. Health is instant and Shield has an in-world player visual, so both are excluded.")]
    [SerializeField] private PowerUpType[] displayOrder =
    {
        PowerUpType.RapidFire,
        PowerUpType.TripleShot,
        PowerUpType.SpeedBoost
    };

    [Tooltip("Maximum number of power-up icon slots generated at runtime.")]
    [SerializeField] private int maxVisibleIcons = 3;

    [Tooltip("Size of each generated power-up icon slot.")]
    [SerializeField] private Vector2 iconSize = new Vector2(32.0f, 32.0f);

    [Tooltip("Horizontal spacing between generated icon slots.")]
    [SerializeField] private float iconSpacing = 8.0f;

    [Tooltip("Alpha used for inactive power-up icons.")]
    [SerializeField] private float inactiveAlpha = 0.25f;

    [Header("Expiration Warning")]
    [Tooltip("Seconds remaining before an active finite-duration power-up starts blinking. Infinite power-ups do not blink.")]
    [SerializeField] private float blinkWhenRemainingSeconds = 2.0f;

    [Tooltip("Blink frequency in cycles per second for expiring power-up icons.")]
    [SerializeField] private float blinkFrequency = 6.0f;

    [Tooltip("Minimum alpha used during the blink warning.")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float blinkDimAlpha = 0.25f;

    private readonly List<PowerUpType> _activePowerUps = new List<PowerUpType>();
    private readonly List<Image> _runtimePowerUpImages = new List<Image>();
    private readonly List<TextMeshProUGUI> _runtimeStackLabels = new List<TextMeshProUGUI>();

    /// <summary>
    /// Description:
    /// Standard Unity function called once when the script is loaded.
    /// Ensures there is a valid PlayerPowerUpController reference.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void Awake()
    {
        if (!playerPowerUpController)
        {
            playerPowerUpController = FindObjectOfType<PlayerPowerUpController>();
        }

        EnsureImageSlots();

        if (hideTextWhenUsingImages && displayText && GetImageSlotCount() > 0)
        {
            displayText.enabled = false;
        }
    }

    /// <summary>
    /// Description:
    /// Standard Unity function called every frame.
    /// Keeps the power-up display text updated in gameplay.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void Update()
    {
        DisplayPowerUp();
    }

    /// <summary>
    /// Description:
    /// Overrides the virtual UpdateUI function and updates the power-up label.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    public override void UpdateUI()
    {
        base.UpdateUI();
        DisplayPowerUp();
    }

    /// <summary>
    /// Description:
    /// Displays either the currently active power-up or a default "none" text.
    /// Inputs:
    /// none
    /// Returns:
    /// void (no return)
    /// </summary>
    private void DisplayPowerUp()
    {
        if (!playerPowerUpController || !playerPowerUpController.HasActivePowerUp)
        {
            _activePowerUps.Clear();
            UpdateText();
            UpdateImages();
            return;
        }

        playerPowerUpController.GetActivePowerUps(_activePowerUps);
        UpdateText();
        UpdateImages();
    }

    private void UpdateText()
    {
        if (!displayText)
        {
            return;
        }

        displayText.text = playerPowerUpController && playerPowerUpController.HasActivePowerUp
            ? string.Format(activePowerUpFormat, playerPowerUpController.ActivePowerUpDisplayName)
            : noPowerUpText;
    }

    private void UpdateImages()
    {
        int imageSlotCount = GetImageSlotCount();
        if (imageSlotCount == 0)
        {
            return;
        }

        for (int i = 0; i < imageSlotCount; i++)
        {
            Image image = GetImageSlot(i);
            if (!image)
            {
                continue;
            }

            PowerUpType type = GetSlotPowerUpType(i);
            bool isActive = IsPowerUpActive(type);
            Sprite sprite = GetIcon(type);
            int stackCount = playerPowerUpController ? playerPowerUpController.GetStackCount(type) : 0;

            image.sprite = sprite;
            image.color = GetIconColor(type, isActive);
            image.enabled = sprite != null;
            image.gameObject.SetActive(sprite != null);
            UpdateStackLabel(i, isActive, stackCount);
        }
    }

    private Color GetIconColor(PowerUpType type, bool isActive)
    {
        Color color = Color.white;
        color.a = isActive ? 1.0f : inactiveAlpha;

        if (!isActive || !playerPowerUpController || blinkWhenRemainingSeconds <= 0.0f)
        {
            return color;
        }

        float remainingDuration = playerPowerUpController.GetRemainingDuration(type);
        if (remainingDuration <= 0.0f || remainingDuration > blinkWhenRemainingSeconds)
        {
            return color;
        }

        bool isDimFrame = Mathf.FloorToInt(Time.unscaledTime * blinkFrequency) % 2 == 0;
        color.a = isDimFrame ? blinkDimAlpha : 1.0f;
        return color;
    }

    private Sprite GetIcon(PowerUpType type)
    {
        if (powerUpIcons == null)
        {
            return null;
        }

        for (int i = 0; i < powerUpIcons.Length; i++)
        {
            if (powerUpIcons[i].type == type)
            {
                return powerUpIcons[i].sprite;
            }
        }

        return null;
    }

    private bool IsPowerUpActive(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.RapidFire:
                return playerPowerUpController && playerPowerUpController.fireRateMultiplier > 1.0f;

            case PowerUpType.TripleShot:
                return playerPowerUpController && playerPowerUpController.tripleShotEnabled;

            case PowerUpType.Shield:
                return playerPowerUpController && playerPowerUpController.shieldEnabled;

            case PowerUpType.SpeedBoost:
                return playerPowerUpController && playerPowerUpController.speedMultiplier > 1.0f;

            default:
                return false;
        }
    }

    private PowerUpType GetSlotPowerUpType(int index)
    {
        if (displayOrder != null && index < displayOrder.Length)
        {
            return displayOrder[index];
        }

        return (PowerUpType)Mathf.Clamp(index, 0, (int)PowerUpType.SpeedBoost);
    }

    private void EnsureImageSlots()
    {
        if (powerUpImages != null && powerUpImages.Length > 0)
        {
            return;
        }

        RectTransform displayTransform = displayText ? displayText.rectTransform : null;
        RectTransform parent = iconContainer;
        if (!parent && displayText)
        {
            parent = displayText.transform.parent as RectTransform;
        }

        if (!parent)
        {
            return;
        }

        int slotCount = Mathf.Max(0, maxVisibleIcons);
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slot = new GameObject($"PowerUpIcon_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform slotTransform = slot.GetComponent<RectTransform>();
            slotTransform.SetParent(parent, false);
            slotTransform.anchorMin = displayTransform ? displayTransform.anchorMin : new Vector2(0.0f, 0.0f);
            slotTransform.anchorMax = displayTransform ? displayTransform.anchorMax : new Vector2(0.0f, 0.0f);
            slotTransform.pivot = new Vector2(0.5f, 0.5f);
            slotTransform.sizeDelta = iconSize;
            slotTransform.anchoredPosition = GetSlotPosition(displayTransform, i);

            Image image = slot.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            slot.SetActive(false);

            _runtimePowerUpImages.Add(image);

            GameObject label = new GameObject($"PowerUpStack_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform labelTransform = label.GetComponent<RectTransform>();
            labelTransform.SetParent(parent, false);
            labelTransform.anchorMin = slotTransform.anchorMin;
            labelTransform.anchorMax = slotTransform.anchorMax;
            labelTransform.pivot = new Vector2(0.0f, 0.5f);
            labelTransform.sizeDelta = new Vector2(28.0f, iconSize.y);
            labelTransform.anchoredPosition = slotTransform.anchoredPosition + new Vector2(iconSize.x * 0.5f + 3.0f, 0.0f);

            TextMeshProUGUI stackLabel = label.GetComponent<TextMeshProUGUI>();
            stackLabel.fontSize = 18.0f;
            stackLabel.fontStyle = FontStyles.Bold;
            stackLabel.alignment = TextAlignmentOptions.MidlineLeft;
            stackLabel.color = Color.white;
            stackLabel.raycastTarget = false;
            stackLabel.text = string.Empty;
            label.SetActive(false);

            _runtimeStackLabels.Add(stackLabel);
        }
    }

    private Vector2 GetSlotPosition(RectTransform displayTransform, int index)
    {
        if (!displayTransform)
        {
            return new Vector2(
                iconSize.x * 0.5f + index * GetSlotStride(),
                iconSize.y * 0.5f
            );
        }

        float leftEdge = -displayTransform.sizeDelta.x * 0.5f;
        return displayTransform.anchoredPosition + new Vector2(
            leftEdge + iconSize.x * 0.5f + index * GetSlotStride(),
            0.0f
        );
    }

    private float GetSlotStride()
    {
        return iconSize.x + iconSpacing + 16.0f;
    }

    private int GetImageSlotCount()
    {
        if (powerUpImages != null && powerUpImages.Length > 0)
        {
            return powerUpImages.Length;
        }

        return _runtimePowerUpImages.Count;
    }

    private Image GetImageSlot(int index)
    {
        if (powerUpImages != null && powerUpImages.Length > 0)
        {
            return powerUpImages[index];
        }

        return _runtimePowerUpImages[index];
    }

    private void UpdateStackLabel(int index, bool isActive, int stackCount)
    {
        TextMeshProUGUI stackLabel = GetStackLabel(index);
        if (!stackLabel)
        {
            return;
        }

        bool shouldShow = isActive && stackCount > 1;
        stackLabel.text = shouldShow ? stackCount.ToString() : string.Empty;
        stackLabel.gameObject.SetActive(shouldShow);
    }

    private TextMeshProUGUI GetStackLabel(int index)
    {
        if (index < 0 || index >= _runtimeStackLabels.Count)
        {
            return null;
        }

        return _runtimeStackLabels[index];
    }
}
