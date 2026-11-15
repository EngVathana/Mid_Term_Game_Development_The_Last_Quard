using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public Camera playerCamera;

    [Header("Shooting")]
    public float shootingRange = 100f;
    public int damage = 25;

    [Header("Bullet Tracer")]
    public LineRenderer tracerPrefab;
    public Transform bulletSpawn;
    public float tracerSpeed = 250f;
    public float tracerLength = 2f;

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
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out hit, shootingRange))
        {
            targetPoint = hit.point;

            Debug.Log("Hit: " + hit.collider.name);

            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log("Enemy Hit!");
            }
        }
        else
        {
            targetPoint = ray.GetPoint(shootingRange);
        }

        StartCoroutine(
            ShowTracer(bulletSpawn.position, targetPoint)
        );
    }

    private IEnumerator ShowTracer(Vector3 start, Vector3 end)
    {
        LineRenderer tracer = Instantiate(tracerPrefab);
        
        Debug.Log("TRACER CREATED!");

        tracer.positionCount = 2;
        tracer.useWorldSpace = true;

        Vector3 direction = (end - start).normalized;

        float distance = Vector3.Distance(start, end);
        float duration = distance / tracerSpeed;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            Vector3 head = Vector3.Lerp(start, end, progress);
            Vector3 tail = head - direction * tracerLength;

            if (Vector3.Dot(tail - start, direction) < 0)
            {
                tail = start;
            }

            tracer.SetPosition(0, tail);
            tracer.SetPosition(1, head);

            yield return null;
        }

        Destroy(tracer.gameObject);
    }
}
