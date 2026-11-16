
using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public Camera playerCamera;

    [Header("Shooting")]
    public float shootingRange = 100f;
    public int damage = 25;
    [Min(0.1f)] public float fireRate = 10f;

    private float nextFireTime = 0f;

    [Header("Ammo")]
    [Min(1)] public int magazineSize = 40;
    [SerializeField, Min(0)] private int currentAmmo = 40;
    [Min(0f)] public float reloadDuration = 2f;

    public int CurrentAmmo => currentAmmo;
    public bool IsReloading { get; private set; }
    private Coroutine reloadCoroutine;

    [Header("Bullet Tracer")]
    public LineRenderer tracerPrefab;
    public Transform bulletSpawn;
    public float tracerSpeed = 250f;
    public float tracerLength = 2f;

    [Header("Muzzle Flash")]
    public ParticleSystem muzzleFlash;

    void Update()
    {
        // Hold left mouse button to shoot continuously.
        if (Input.GetMouseButton(0) &&
            Time.time >= nextFireTime &&
            !IsReloading &&
            currentAmmo > 0)
        {
            nextFireTime = Time.time + (1f / fireRate);
            FireWeapon();
        }

        // Reload automatically when empty, or manually with R.
        if (!IsReloading && currentAmmo < magazineSize &&
            (currentAmmo <= 0 || Input.GetKeyDown(KeyCode.R)))
        {
            reloadCoroutine = StartCoroutine(Reload());
        }
    }

    private void FireWeapon()
    {
        if (IsReloading || currentAmmo <= 0 || playerCamera == null)
        {
            return;
        }

        currentAmmo--;

        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play();
        }

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

        if (bulletSpawn != null && tracerPrefab != null)
        {
            StartCoroutine(ShowTracer(bulletSpawn.position, targetPoint));
        }
    }

    private IEnumerator Reload()
    {
        IsReloading = true;

        yield return new WaitForSeconds(reloadDuration);

        currentAmmo = magazineSize;

        IsReloading = false;
        reloadCoroutine = null;
    }

    public void ResetAmmo()
    {
        CancelReload();
        currentAmmo = magazineSize;
    }

    private void OnDisable()
    {
        CancelReload();
    }

    private void CancelReload()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        IsReloading = false;
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
