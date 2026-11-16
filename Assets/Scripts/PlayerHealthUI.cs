using TMPro;
using UnityEngine;

public class PlayerHealthUI : MonoBehaviour
{
    public Damageable playerHealth;
    public RectTransform healthFill;
    public TMP_Text healthText;
    public UnityEngine.UI.Image border;

    void Update()
    {
        if (playerHealth == null || healthFill == null || healthText == null)
        {
            return;
        }

        float health = playerHealth.CurrentHealth;
        float maxHealth = playerHealth.MaxHealth;
        float percent = Mathf.Clamp01(health / maxHealth);

        // Shrink the green bar from right to left.
        float displayed = Mathf.MoveTowards(healthFill.anchorMax.x, percent,
            Time.unscaledDeltaTime * 1.5f);
        if (health <= 0f)
        {
            displayed = 0f;
            healthText.text = "PLAYER DOWN";
        }
        else
        {
            healthText.text = "PLAYER   " + Mathf.CeilToInt(health) + " / " + Mathf.CeilToInt(maxHealth);
        }
        healthFill.anchorMax = new Vector2(displayed, 1f);

        if (border != null)
        {
            Color normal = new Color32(90, 119, 103, 255);
            Color warning = new Color32(168, 255, 196, 255);
            float pulse = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) / 2f;
            border.color = percent > 0f && percent < 0.25f
                ? Color.Lerp(normal, warning, pulse) : normal;
        }
    }
}
