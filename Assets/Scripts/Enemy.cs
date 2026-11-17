using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health = 100;
    public int MaxHealth { get; private set; }
    public event System.Action<Enemy> Died;

    private bool isDead;

    void Awake()
    {
        MaxHealth = Mathf.Max(1, health);
    }

    void Start()
    {
        GameObject healthBar = Resources.Load<GameObject>("EnemyHealthBar");
        if (healthBar != null)
        {
            Instantiate(healthBar, transform);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
        {
            return;
        }

        health = Mathf.Max(0, health - damage);

        if (health <= 0)
        {
            isDead = true;
            Died?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
