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

        if (name.ToLowerInvariant().Contains("fence_stone_straight_gate"))
        {
            AddStoneFrameColliders();
        }
    }

    private void AddStoneFrameColliders()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null)
            {
                continue;
            }

            string objectName = meshFilter.name.ToLowerInvariant();
            if (objectName.Contains("door")
                || meshFilter.GetComponentInParent<PlayerProximityDoor>() != null)
            {
                continue;
            }

            MeshCollider stoneCollider = meshFilter.GetComponent<MeshCollider>();
            if (stoneCollider == null)
            {
                stoneCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            }

            stoneCollider.sharedMesh = meshFilter.sharedMesh;
            stoneCollider.convex = false;
        }

        AddGateSideBlockers();
    }

    private void AddGateSideBlockers()
    {
        PlayerProximityDoor door = GetComponentInChildren<PlayerProximityDoor>(true);
        Renderer doorRenderer = door != null ? door.GetComponent<Renderer>() : null;
        if (doorRenderer == null)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Bounds stoneBounds = default;
        bool foundStone = false;

        foreach (Renderer stoneRenderer in renderers)
        {
            if (stoneRenderer.GetComponentInParent<PlayerProximityDoor>() != null)
            {
                continue;
            }

            if (!foundStone)
            {
                stoneBounds = stoneRenderer.bounds;
                foundStone = true;
            }
            else
            {
                stoneBounds.Encapsulate(stoneRenderer.bounds);
            }
        }

        if (!foundStone)
        {
            return;
        }

        Bounds doorBounds = doorRenderer.bounds;
        bool widthIsX = stoneBounds.size.x >= stoneBounds.size.z;
        const float seamOverlap = 1f;

        if (widthIsX)
        {
            CreateSideBlocker(
                "GateSideBlocker_Left",
                new Vector3(stoneBounds.min.x, stoneBounds.min.y, stoneBounds.min.z),
                new Vector3(doorBounds.min.x + seamOverlap, stoneBounds.max.y, stoneBounds.max.z));
            CreateSideBlocker(
                "GateSideBlocker_Right",
                new Vector3(doorBounds.max.x - seamOverlap, stoneBounds.min.y, stoneBounds.min.z),
                new Vector3(stoneBounds.max.x, stoneBounds.max.y, stoneBounds.max.z));
        }
        else
        {
            CreateSideBlocker(
                "GateSideBlocker_Left",
                new Vector3(stoneBounds.min.x, stoneBounds.min.y, stoneBounds.min.z),
                new Vector3(stoneBounds.max.x, stoneBounds.max.y, doorBounds.min.z + seamOverlap));
            CreateSideBlocker(
                "GateSideBlocker_Right",
                new Vector3(stoneBounds.min.x, stoneBounds.min.y, doorBounds.max.z - seamOverlap),
                new Vector3(stoneBounds.max.x, stoneBounds.max.y, stoneBounds.max.z));
        }
    }

    private void CreateSideBlocker(string blockerName, Vector3 minimum, Vector3 maximum)
    {
        Vector3 size = maximum - minimum;
        if (size.x <= 0.1f || size.y <= 0.1f || size.z <= 0.1f)
        {
            return;
        }

        GameObject blockerObject = new GameObject(blockerName);
        blockerObject.transform.position = (minimum + maximum) * 0.5f;

        BoxCollider blockerCollider = blockerObject.AddComponent<BoxCollider>();
        blockerCollider.size = size;
        blockerObject.AddComponent<CastleBarrier>();

        // Preserve the world-sized box while making it part of the gate so it
        // disappears with the shared wall defense.
        blockerObject.transform.SetParent(transform, true);
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
