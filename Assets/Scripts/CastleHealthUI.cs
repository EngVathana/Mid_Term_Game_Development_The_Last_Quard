using TMPro;
using UnityEngine;

/// <summary>Displays shared wall health and the castle's independent health.</summary>
public sealed class CastleHealthUI : MonoBehaviour
{
    [SerializeField] private CastleDefense wall;
    [SerializeField] private Damageable castle;
    [SerializeField] private RectTransform wallFill;
    [SerializeField] private RectTransform castleFill;
    [SerializeField] private TMP_Text wallText;
    [SerializeField] private TMP_Text castleText;
    [SerializeField] private UnityEngine.UI.Image wallBorder;
    [SerializeField] private UnityEngine.UI.Image castleBorder;

    private float wallMaximum;
    private float castleMaximum;
    private float wallDisplay = 1f;
    private float castleDisplay = 1f;
    private static readonly Color BorderColor = new Color32(90, 119, 103, 255);

    private void Start()
    {
        wallMaximum = wall != null ? wall.MaxHealth : 1f;
        castleMaximum = castle != null ? castle.MaxHealth : 1f;
        Refresh(true);
    }

    private void Update() => Refresh(false);

    private void Refresh(bool instant)
    {
        // Destroyed Unity objects compare equal to null. Retain the maximum for the label.
        Draw(wall != null ? wall.CurrentHealth : 0f, wallMaximum,
            ref wallDisplay, wallFill, wallText, wallBorder, "WALL", instant);
        Draw(castle != null ? castle.CurrentHealth : 0f, castleMaximum,
            ref castleDisplay, castleFill, castleText, castleBorder, "CASTLE", instant);
    }

    private static void Draw(float current, float maximum, ref float displayed,
        RectTransform fill, TMP_Text label, UnityEngine.UI.Image border, string title, bool instant)
    {
        float ratio = Mathf.Clamp01(current / Mathf.Max(1f, maximum));
        displayed = instant || ratio <= 0f ? ratio :
            Mathf.MoveTowards(displayed, ratio, Time.unscaledDeltaTime * 1.5f);
        if (fill != null)
            fill.anchorMax = new Vector2(displayed, 1f);
        if (label != null)
            label.text = current <= 0f ? title + " DESTROYED" :
                $"{title}   {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
        if (border != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
            border.color = ratio > 0f && ratio < 0.25f
                ? Color.Lerp(BorderColor, new Color32(168, 255, 196, 255), pulse)
                : BorderColor;
        }
    }
}
