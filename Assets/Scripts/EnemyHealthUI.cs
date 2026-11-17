using UnityEngine;

public class EnemyHealthUI : MonoBehaviour
{
    public RectTransform healthFill;
    public float visibleDistance = 650f;
    public float heightAboveHead = 3f;

    private Enemy enemy;
    private Camera playerCamera;
    private Canvas healthCanvas;
    private float height;

    void Start()
    {
        enemy = GetComponentInParent<Enemy>();
        playerCamera = Camera.main;
        healthCanvas = GetComponent<Canvas>();
        healthCanvas.enabled = false;

        if (enemy == null)
        {
            return;
        }

        // Find the top of the model, including enemies of different sizes.
        height = heightAboveHead;
        foreach (Renderer model in enemy.GetComponentsInChildren<Renderer>())
        {
            height = Mathf.Max(height, model.bounds.max.y - enemy.transform.position.y + heightAboveHead);
        }

        // Give larger enemies a larger bar. The golem is much taller than other enemies.
        float barWidth = Mathf.Max(12f, (height - heightAboveHead) * 0.3f);
        float barScale = barWidth / GetComponent<RectTransform>().rect.width;
        Vector3 scale = enemy.transform.lossyScale;
        transform.localScale = new Vector3(barScale / scale.x, barScale / scale.y, barScale / scale.z);
    }

    void LateUpdate()
    {
        if (enemy == null || playerCamera == null)
        {
            healthCanvas.enabled = false;
            return;
        }

        float distance = Vector3.Distance(playerCamera.transform.position, enemy.transform.position);
        healthCanvas.enabled = enemy.health > 0 && enemy.health < enemy.MaxHealth
            && distance <= visibleDistance;

        if (!healthCanvas.enabled)
        {
            return;
        }

        healthFill.anchorMax = new Vector2(Mathf.Clamp01((float)enemy.health / enemy.MaxHealth), 1f);
        transform.position = enemy.transform.position + Vector3.up * height;
        transform.rotation = playerCamera.transform.rotation;
    }
}
