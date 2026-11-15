using UnityEngine;

public sealed class Damageable : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0f;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        if (IsDead)
        {
            Debug.Log($"{name} was defeated.", this);
            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }
    }

    public void RestoreFullHealth()
    {
        CurrentHealth = maxHealth;
    }
}
