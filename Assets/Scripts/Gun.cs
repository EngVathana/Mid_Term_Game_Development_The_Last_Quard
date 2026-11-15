using UnityEngine;

public class Gun : MonoBehaviour
{
    public Camera playerCamera;

    [SerializeField] public float shootingRange = 10000f;
    [SerializeField] public int damage = 50;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            FireWeapon();
        }
    }

    private void FireWeapon()
    {
        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, shootingRange))
        {
            Debug.Log("Hit: " + hit.collider.name);

            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);

                Debug.Log("Enemy Hit!");
            }
        }
    }
}
