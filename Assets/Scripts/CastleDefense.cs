using UnityEngine;

/// <summary>One shared health pool for every wall section and gate.</summary>
public sealed class CastleDefense : MonoBehaviour
{
    [Header("Shared Wall Health")]
    [Min(1f)] public float maxHealth = 250f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDestroyed => CurrentHealth <= 0f;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (IsDestroyed || amount <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        if (!IsDestroyed)
        {
            return;
        }

        CastleBarrier[] barriers = FindObjectsByType<CastleBarrier>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (CastleBarrier barrier in barriers)
        {
            Destroy(barrier.gameObject);
        }

        Debug.Log($"Castle defense destroyed. Removed {barriers.Length} wall and gate objects.", this);
    }
}
