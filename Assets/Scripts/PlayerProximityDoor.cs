using UnityEngine;

/// <summary>
/// Swings a hinged door for the GameObject using PlayerMovement only.
/// Attach this component to the moving door mesh, with its pivot at the hinge.
/// </summary>
public sealed class PlayerProximityDoor : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField, Min(0.1f)] private float openDistance = 12f;
    [SerializeField, Min(0.1f)] private float closeDistance = 15f;
    [SerializeField, Min(0f)] private float closeDelay = 0.6f;

    [Header("Swing")]
    [SerializeField, Range(1f, 170f)] private float openAngle = 95f;
    [SerializeField, Min(1f)] private float rotationSpeed = 160f;

    private PlayerMovement player;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Vector3 doorwayCenter;
    private Vector3 hingeToDoorCenter;
    private Collider doorCollider;
    private float nextPlayerSearchTime;
    private float outsideRangeSince = -1f;
    private bool isOpen;
    private bool initialized;

    private void Awake()
    {
        closedRotation = transform.rotation;
        openRotation = closedRotation;

        Renderer doorRenderer = GetComponent<Renderer>();
        doorwayCenter = doorRenderer != null
            ? doorRenderer.bounds.center
            : transform.position;
        hingeToDoorCenter = Vector3.ProjectOnPlane(
            doorwayCenter - transform.position,
            Vector3.up);

        if (hingeToDoorCenter.sqrMagnitude < 0.001f)
        {
            hingeToDoorCenter = transform.right;
        }

        // The imported door mesh has no collider. This collider follows the
        // swing and blocks the player's CharacterController while closed.
        doorCollider = GetComponent<Collider>();
        if (doorCollider == null)
        {
            doorCollider = gameObject.AddComponent<BoxCollider>();
        }

        FindPlayer();
        initialized = true;
    }

    private void Update()
    {
        if (player == null)
        {
            if (Time.time >= nextPlayerSearchTime)
            {
                FindPlayer();
            }

            isOpen = false;
            RotateDoor(closedRotation);
            SetDoorBlocking(true);
            return;
        }

        Vector3 toPlayer = player.transform.position - doorwayCenter;
        toPlayer.y = 0f;
        float distanceSquared = toPlayer.sqrMagnitude;

        if (!isOpen && distanceSquared <= openDistance * openDistance)
        {
            OpenAwayFrom(player.transform.position);
        }
        else if (isOpen)
        {
            if (distanceSquared < closeDistance * closeDistance)
            {
                outsideRangeSince = -1f;
            }
            else if (outsideRangeSince < 0f)
            {
                outsideRangeSince = Time.time;
            }
            else if (Time.time - outsideRangeSince >= closeDelay)
            {
                isOpen = false;
                outsideRangeSince = -1f;
            }
        }

        RotateDoor(isOpen ? openRotation : closedRotation);
        SetDoorBlocking(!isOpen);
    }

    private void FindPlayer()
    {
        player = FindFirstObjectByType<PlayerMovement>();
        nextPlayerSearchTime = Time.time + 0.5f;
    }

    private void OpenAwayFrom(Vector3 playerPosition)
    {
        Vector3 positiveCenter = transform.position
            + Quaternion.AngleAxis(openAngle, Vector3.up) * hingeToDoorCenter;
        Vector3 negativeCenter = transform.position
            + Quaternion.AngleAxis(-openAngle, Vector3.up) * hingeToDoorCenter;

        float positiveDistance = (positiveCenter - playerPosition).sqrMagnitude;
        float negativeDistance = (negativeCenter - playerPosition).sqrMagnitude;
        float direction = positiveDistance >= negativeDistance ? 1f : -1f;

        openRotation = Quaternion.AngleAxis(
            direction * openAngle,
            Vector3.up) * closedRotation;
        isOpen = true;
        outsideRangeSince = -1f;
    }

    private void RotateDoor(Quaternion targetRotation)
    {
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    private void SetDoorBlocking(bool shouldBlock)
    {
        if (doorCollider != null && doorCollider.enabled != shouldBlock)
        {
            doorCollider.enabled = shouldBlock;
        }
    }

    private void OnDisable()
    {
        if (initialized)
        {
            transform.rotation = closedRotation;
            SetDoorBlocking(true);
        }
    }

    private void OnValidate()
    {
        closeDistance = Mathf.Max(closeDistance, openDistance);
    }
}
