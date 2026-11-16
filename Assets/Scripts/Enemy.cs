using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int health = 100;
    public int MaxHealth { get; private set; }

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
        health -= damage;

        Debug.Log(gameObject.name + " Health: " + health);

        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
