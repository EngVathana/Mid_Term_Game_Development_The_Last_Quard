using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class WorldHealthBar : MonoBehaviour
{
    private Transform anchor;
    private PlayerMovement player;
    private Func<float> currentHealth;
    private Func<float> maximumHealth;
    private string label;
    private float verticalOffset;
    private GameObject barVisual;
    private Image fillImage;
    private RectTransform fillRect;
    private TMP_Text healthText;
    private Vector3 anchorPosition;

    public void Configure(
        Transform targetAnchor,
        PlayerMovement targetPlayer,
        Func<float> current,
        Func<float> maximum,
        string barLabel,
        float offset,
        Vector2 pixelSize,
        float worldScale,
        Color fillColor)
    {
        anchor = targetAnchor;
        player = targetPlayer;
        currentHealth = current;
        maximumHealth = maximum;
        label = barLabel;
        verticalOffset = offset;
        CreateVisuals(pixelSize, worldScale, fillColor);
        CacheAnchorPosition();
    }

    private void Update()
    {
        if (anchor == null || player == null || currentHealth == null || maximumHealth == null)
        {
            Destroy(gameObject);
            return;
        }

        barVisual.SetActive(true);

        float maximum = Mathf.Max(1f, maximumHealth());
        float current = Mathf.Clamp(currentHealth(), 0f, maximum);
        float healthRatio = current / maximum;
        fillImage.fillAmount = healthRatio;
        fillRect.anchorMax = new Vector2(
            Mathf.Lerp(0.02f, 0.98f, healthRatio),
            0.88f);
        healthText.text = $"{label}  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";

        transform.position = anchorPosition + Vector3.up * verticalOffset;
        Camera viewCamera = Camera.main;
        if (viewCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(
                transform.position - viewCamera.transform.position,
                Vector3.up);
        }
    }

    private void CacheAnchorPosition()
    {
        Renderer[] renderers = anchor.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            anchorPosition = anchor.position;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
        {
            bounds.Encapsulate(renderers[index].bounds);
        }

        anchorPosition = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }

    private void CreateVisuals(Vector2 pixelSize, float worldScale, Color fillColor)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;
        gameObject.AddComponent<CanvasScaler>();

        RectTransform canvasRect = GetComponent<RectTransform>();
        canvasRect.sizeDelta = pixelSize;
        transform.localScale = Vector3.one * worldScale;

        barVisual = new GameObject("Health Bar Visual", typeof(RectTransform));
        barVisual.transform.SetParent(transform, false);
        RectTransform visualRect = barVisual.GetComponent<RectTransform>();
        visualRect.anchorMin = Vector2.zero;
        visualRect.anchorMax = Vector2.one;
        visualRect.offsetMin = Vector2.zero;
        visualRect.offsetMax = Vector2.zero;

        Image background = barVisual.AddComponent<Image>();
        background.color = new Color(0.03f, 0.03f, 0.03f, 0.88f);

        GameObject fillObject = new GameObject("Health Fill", typeof(RectTransform));
        fillObject.transform.SetParent(barVisual.transform, false);
        fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0.02f, 0.12f);
        fillRect.anchorMax = new Vector2(0.98f, 0.88f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillImage = fillObject.AddComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;

        GameObject textObject = new GameObject("Health Text", typeof(RectTransform));
        textObject.transform.SetParent(barVisual.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        healthText = textObject.AddComponent<TextMeshProUGUI>();
        healthText.font = TMP_Settings.defaultFontAsset;
        healthText.fontSize = pixelSize.y * 0.48f;
        healthText.fontStyle = FontStyles.Bold;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.color = Color.white;

        barVisual.SetActive(false);
    }
}

public static class WorldHealthBarInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Install();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Install();
    }

    private static void Install()
    {
        PlayerMovement player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        if (player == null)
        {
            return;
        }

        CastleDefense wallHealth = UnityEngine.Object.FindFirstObjectByType<CastleDefense>();
        Transform gate = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(item => item.name.StartsWith(
                "fence_stone_straight_gate",
                StringComparison.OrdinalIgnoreCase));

        if (wallHealth != null && gate != null)
        {
            CreateBar(
                "Floating Wall Health",
                gate,
                player,
                () => wallHealth != null ? wallHealth.CurrentHealth : 0f,
                () => wallHealth != null ? wallHealth.MaxHealth : 1f,
                "WALL",
                8f,
                new Vector2(320f, 56f),
                0.11f,
                new Color(0.08f, 0.82f, 0.18f));
        }

        CastleTarget castle = UnityEngine.Object.FindFirstObjectByType<CastleTarget>();
        Damageable castleHealth = castle != null ? castle.GetComponent<Damageable>() : null;
        if (castle != null && castleHealth != null)
        {
            CreateBar(
                "Floating Castle Health",
                castle.transform,
                player,
                () => castleHealth != null ? castleHealth.CurrentHealth : 0f,
                () => castleHealth != null ? castleHealth.MaxHealth : 1f,
                "CASTLE",
                22f,
                new Vector2(520f, 78f),
                0.16f,
                new Color(0.08f, 0.82f, 0.18f));
        }
    }

    private static void CreateBar(
        string objectName,
        Transform anchor,
        PlayerMovement player,
        Func<float> current,
        Func<float> maximum,
        string label,
        float verticalOffset,
        Vector2 pixelSize,
        float worldScale,
        Color fillColor)
    {
        if (GameObject.Find(objectName) != null)
        {
            return;
        }

        GameObject barObject = new GameObject(objectName, typeof(RectTransform));
        WorldHealthBar bar = barObject.AddComponent<WorldHealthBar>();
        bar.Configure(
            anchor,
            player,
            current,
            maximum,
            label,

            verticalOffset,
            pixelSize,
            worldScale,
            fillColor);
    }
}
