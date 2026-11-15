using UnityEngine;

/// <summary>Connects one wall or gate to the shared castle defense health.</summary>
public sealed class CastleBarrier : MonoBehaviour
{
    private CastleDefense defense;
    private Damageable localHealth;

    public bool IsDestroyed => defense != null
        ? defense.IsDestroyed
        : localHealth == null || localHealth.IsDead;

    private void Awake()
    {
        defense = FindFirstObjectByType<CastleDefense>();
        localHealth = GetComponent<Damageable>();
    }

    public void TakeDamage(float amount)
    {
        if (defense == null)
        {
            defense = FindFirstObjectByType<CastleDefense>();
        }

        if (defense != null)
        {
            defense.TakeDamage(amount);
        }
        else
        {
            localHealth?.TakeDamage(amount);
        }
    }
}
