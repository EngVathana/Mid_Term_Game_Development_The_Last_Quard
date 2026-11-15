using UnityEngine;

public class Gun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletVelocity = 30;
    public float bulletPrefabLifeTime = 3f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            FireWeapon();
        }
    }

    private void FireWeapon()
    {
        if (bulletPrefab == null || bulletSpawn == null)
        {
            Debug.LogWarning("Assign the bullet prefab and bullet spawn in the Inspector.", this);
            return;
        }

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, bulletSpawn.rotation);

        if (bullet.TryGetComponent<Rigidbody>(out Rigidbody body))
        {
            body.linearVelocity = bulletSpawn.forward * bulletVelocity;
        }
        else
        {
            Debug.LogWarning("The bullet prefab needs a Rigidbody.", bullet);
        }

        Destroy(bullet, bulletPrefabLifeTime);
    }
}
