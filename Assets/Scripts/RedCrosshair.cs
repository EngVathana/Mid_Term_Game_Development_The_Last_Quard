using UnityEngine;
using UnityEngine.UI;

/// <summary>Replaces the black crosshair texture with a crisp red UI crosshair.</summary>
public sealed class RedCrosshair : MonoBehaviour
{
    private static readonly Color CrosshairRed = new Color(1f, 0.06f, 0.04f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject crosshair = GameObject.Find("Crosshair");
        if (crosshair == null || crosshair.GetComponent<RedCrosshair>() != null)
        {
            return;
        }

        crosshair.AddComponent<RedCrosshair>().Build();
    }

    private void Build()
    {
        Canvas crosshairCanvas = GetComponentInParent<Canvas>();
        Camera mainCamera = Camera.main;
        if (crosshairCanvas != null && mainCamera != null)
        {
            crosshairCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            crosshairCanvas.worldCamera = mainCamera;
            crosshairCanvas.planeDistance = 1f;
            crosshairCanvas.sortingOrder = 100;
        }

        Image original = GetComponent<Image>();
        if (original != null)
        {
            original.enabled = false;
            original.sprite = null;
            original.color = Color.clear;
            original.raycastTarget = false;
            Destroy(original);
        }

        CreateLine("Top", new Vector2(0f, 7f), new Vector2(2f, 8f));
        CreateLine("Bottom", new Vector2(0f, -7f), new Vector2(2f, 8f));
        CreateLine("Left", new Vector2(-7f, 0f), new Vector2(8f, 2f));
        CreateLine("Right", new Vector2(7f, 0f), new Vector2(8f, 2f));
    }

    private void CreateLine(string lineName, Vector2 position, Vector2 size)
    {
        GameObject line = new GameObject(lineName, typeof(RectTransform), typeof(Image));
        line.transform.SetParent(transform, false);

        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = line.GetComponent<Image>();
        image.color = CrosshairRed;
        image.raycastTarget = false;
    }
}
