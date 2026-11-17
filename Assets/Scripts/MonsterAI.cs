using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class MonsterAI : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField, Min(1f)] private float playerDetectionRange = 35f;
    [SerializeField, Min(0.1f)] private float attackRange = 1.5f;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 5f;
    [SerializeField, Min(1f)] private float turnSpeed = 360f;

    [Header("Attack")]
    [SerializeField, Min(0.1f)] private float attackDamage = 10f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1.25f;

    [Header("Animation")]
    [SerializeField] private RuntimeAnimatorController animatorController;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private Rigidbody body;
    private Animator animator;
    private PlayerMovement player;
    private CastleTarget castle;
    private Transform currentTarget;
    private float nextTargetSearchTime;
    private float nextAttackTime;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>(true);

        // These monster prefabs are model-prefab variants and did not contain an
        // Animator component. Put it on the model root so the FBX curve paths bind.
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        if (animator != null && animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
        }

        body.constraints |= RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationZ;
        // AI movement is controlled by MovePosition. A dynamic Rigidbody let
        // overlapping character colliders launch or bury the monster.
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        FindSceneTargets();
    }

    private void FixedUpdate()
    {
        if (Time.time >= nextTargetSearchTime || player == null || castle == null)
        {
            FindSceneTargets();
        }

        ChooseTarget();
        if (currentTarget == null)
        {
            SetWalking(false);
            return;
        }

        Vector3 targetPoint = GetClosestTargetPoint(currentTarget);
        Vector3 direction = targetPoint - body.position;
        direction.y = 0f;
        float distance = GetSurfaceDistance(targetPoint);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(
                body.rotation,
                lookRotation,
                turnSpeed * Time.fixedDeltaTime));
        }

        if (distance > attackRange)
        {
            SetWalking(true);
            Vector3 step = direction.normalized * moveSpeed * Time.fixedDeltaTime;
            body.MovePosition(body.position + step);
            return;
        }

        SetWalking(false);
        TryAttack(currentTarget);
    }

    private void FindSceneTargets()
    {
        player = FindFirstObjectByType<PlayerMovement>();
        castle = FindFirstObjectByType<CastleTarget>();
        nextTargetSearchTime = Time.time + 0.5f;
    }

    private void ChooseTarget()
    {
        Transform desiredTarget = castle != null ? castle.transform : null;

        Damageable playerHealth = player != null
            ? player.GetComponent<Damageable>()
            : null;

        if (player != null && (playerHealth == null || !playerHealth.IsDead))
        {
            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude <= playerDetectionRange * playerDetectionRange)
            {
                desiredTarget = player.transform;
            }
        }

        currentTarget = FindBlockingBarrier(desiredTarget) ?? desiredTarget;
    }

    private Transform FindBlockingBarrier(Transform desiredTarget)
    {
        if (desiredTarget == null)
        {
            return null;
        }

        Vector3 origin = body.worldCenterOfMass;
        Vector3 destination = GetClosestTargetPoint(desiredTarget);
        destination.y = origin.y;
        Vector3 direction = destination - origin;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
        {
            return null;
        }

        RaycastHit[] hits = Physics.BoxCastAll(
            origin,
            new Vector3(0.4f, 1.25f, 0.4f),
            direction.normalized,
            Quaternion.identity,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        foreach (RaycastHit hit in hits)
        {
            CastleBarrier barrier = hit.collider.GetComponentInParent<CastleBarrier>();
            if (barrier != null && !barrier.IsDestroyed)
            {
                return barrier.transform;
            }
        }

        return null;
    }

    private Vector3 GetClosestTargetPoint(Transform target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Vector3 closest = colliders[0].bounds.ClosestPoint(body.position);
            float closestDistance = (closest - body.position).sqrMagnitude;

            for (int i = 1; i < colliders.Length; i++)
            {
                Vector3 point = colliders[i].bounds.ClosestPoint(body.position);
                float distance = (point - body.position).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closest = point;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.ClosestPoint(body.position);
        }

        return target.position;
    }

    private float GetSurfaceDistance(Vector3 targetPoint)
    {
        Collider[] ownColliders = GetComponentsInChildren<Collider>();
        float closestDistance = float.PositiveInfinity;

        foreach (Collider ownCollider in ownColliders)
        {
            if (!ownCollider.enabled)
            {
                continue;
            }

            Vector3 ownPoint = ownCollider.bounds.ClosestPoint(targetPoint);
            Vector3 separation = targetPoint - ownPoint;
            separation.y = 0f;
            closestDistance = Mathf.Min(closestDistance, separation.magnitude);
        }

        if (!float.IsPositiveInfinity(closestDistance))
        {
            return closestDistance;
        }

        Vector3 fallback = targetPoint - body.position;
        fallback.y = 0f;
        return fallback.magnitude;
    }

    private void TryAttack(Transform target)
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        if (animator != null)
        {
            animator.SetTrigger(AttackHash);
        }

        CastleBarrier barrier = target.GetComponent<CastleBarrier>();
        if (barrier == null)
        {
            barrier = target.GetComponentInParent<CastleBarrier>();
        }

        if (barrier != null)
        {
            barrier.TakeDamage(attackDamage);
            return;
        }

        Damageable damageable = target.GetComponent<Damageable>();
        if (damageable == null)
        {
            damageable = target.GetComponentInParent<Damageable>();
        }

        damageable?.TakeDamage(attackDamage);
    }

    private void SetWalking(bool isWalking)
    {
        if (animator != null)
        {
            animator.SetBool(IsMovingHash, isWalking);
        }
    }
}
