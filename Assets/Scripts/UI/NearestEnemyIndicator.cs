using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a red screen-edge marker toward the closest enemy when no enemy is visible.
/// </summary>
public class NearestEnemyIndicator : UIelement
{
    [Header("References")]
    [Tooltip("Camera used to determine whether enemies are visible. If empty, Camera.main is used.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Player transform used to choose the closest offscreen enemy. If empty, the Player tag is used.")]
    [SerializeField] private Transform player;

    [Tooltip("Optional prebuilt indicator RectTransform. If empty, a red Image marker is created at runtime.")]
    [SerializeField] private RectTransform indicator;

    [Header("Indicator")]
    [Tooltip("Width and height of the generated red indicator marker.")]
    [SerializeField] private float size = 12.0f;

    [Tooltip("Minimum distance from the screen edge when clamping the indicator marker.")]
    [SerializeField] private float edgePadding = 28.0f;

    [Tooltip("Color used by the generated indicator marker.")]
    [SerializeField] private Color color = new Color(1.0f, 0.0f, 0.0f, 1.0f);

    private Image _indicatorImage;

    private void Awake()
    {
        ResolveReferences();
        EnsureIndicator();
    }

    private void LateUpdate()
    {
        UpdateUI();
    }

    public override void UpdateUI()
    {
        base.UpdateUI();

        ResolveReferences();

        if (!targetCamera || !player || !indicator)
        {
            SetVisible(false);
            return;
        }

        Enemy closestOffscreenEnemy = GetClosestOffscreenEnemy();
        if (!closestOffscreenEnemy)
        {
            SetVisible(false);
            return;
        }

        Vector3 viewportPosition = targetCamera.WorldToViewportPoint(closestOffscreenEnemy.transform.position);
        if (viewportPosition.z < 0.0f)
        {
            viewportPosition.x = 1.0f - viewportPosition.x;
            viewportPosition.y = 1.0f - viewportPosition.y;
        }

        RectTransform parent = indicator.parent as RectTransform;
        if (!parent)
        {
            SetVisible(false);
            return;
        }

        float x = Mathf.Clamp(viewportPosition.x * parent.rect.width, edgePadding, parent.rect.width - edgePadding);
        float y = Mathf.Clamp(viewportPosition.y * parent.rect.height, edgePadding, parent.rect.height - edgePadding);

        indicator.anchoredPosition = new Vector2(x, y);
        SetVisible(true);
    }

    private void ResolveReferences()
    {
        if (!targetCamera)
        {
            targetCamera = Camera.main;
        }

        if (!player)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject)
            {
                player = playerObject.transform;
            }
        }
    }

    private void EnsureIndicator()
    {
        if (indicator)
        {
            _indicatorImage = indicator.GetComponent<Image>();
            return;
        }

        RectTransform parent = GetCanvasRectTransform();
        if (!parent)
        {
            return;
        }

        GameObject marker = new GameObject("NearestEnemyIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        indicator = marker.GetComponent<RectTransform>();
        indicator.SetParent(parent, false);
        indicator.anchorMin = new Vector2(0.0f, 0.0f);
        indicator.anchorMax = new Vector2(0.0f, 0.0f);
        indicator.pivot = new Vector2(0.5f, 0.5f);
        indicator.sizeDelta = new Vector2(size, size);

        _indicatorImage = marker.GetComponent<Image>();
        _indicatorImage.color = color;
        _indicatorImage.raycastTarget = false;

        marker.SetActive(false);
    }

    private RectTransform GetCanvasRectTransform()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas)
        {
            return canvas.transform as RectTransform;
        }

        return transform as RectTransform;
    }

    private Enemy GetClosestOffscreenEnemy()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy closestEnemy = null;
        float closestDistanceSqr = float.PositiveInfinity;
        bool anyEnemyVisible = false;

        foreach (Enemy enemy in enemies)
        {
            if (!enemy)
            {
                continue;
            }

            Vector3 viewportPosition = targetCamera.WorldToViewportPoint(enemy.transform.position);
            bool isVisible =
                viewportPosition.z > 0.0f &&
                viewportPosition.x >= 0.0f &&
                viewportPosition.x <= 1.0f &&
                viewportPosition.y >= 0.0f &&
                viewportPosition.y <= 1.0f;

            if (isVisible)
            {
                anyEnemyVisible = true;
                break;
            }

            float distanceSqr = (enemy.transform.position - player.position).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closestEnemy = enemy;
            }
        }

        return anyEnemyVisible ? null : closestEnemy;
    }

    private void SetVisible(bool visible)
    {
        if (indicator)
        {
            indicator.gameObject.SetActive(visible);
        }
    }
}
