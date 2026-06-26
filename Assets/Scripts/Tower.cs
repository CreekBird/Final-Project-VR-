using UnityEngine;

/// <summary>
/// Generic tower behaviour: find the nearest enemy, rotate toward it, fire projectiles.
///
/// HOW TO SET UP THE PREFAB:
///   - Root: collider (for touch-placement raycast), this script.
///   - Child "Head": the part that rotates to aim — assign to rotatingHead.
///   - Child "FirePoint": empty Transform at the barrel tip — assign to firePoint.
///   - Assign a Projectile prefab to projectilePrefab.
///
/// Targeting strategy: prioritise enemy with greatest PathProgress (closest to the base).
/// </summary>
public class Tower : MonoBehaviour
{
    [Header("Stats")]
    [Tooltip("Detection radius in world units (AR scale: ~0.3-0.8 m).")]
    [SerializeField] float range    = 0.4f;

    [Tooltip("Shots per second.")]
    [SerializeField] float fireRate = 1f;

    [Tooltip("Damage dealt per projectile.")]
    [SerializeField] float damage   = 30f;

    [Header("References")]
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform  firePoint;
    [Tooltip("Child Transform that rotates to face the current target.")]
    [SerializeField] Transform  rotatingHead;

    // ── State ─────────────────────────────────────────────────────────────────
    float        fireCooldown;
    EnemyHealth  currentTarget;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Update()
    {
        // Towers only act during the Wave phase
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State != GameManager.GameState.Wave) return;

        fireCooldown -= Time.deltaTime;

        // Refresh target if null or out of range
        if (currentTarget == null || Vector3.Distance(transform.position, currentTarget.transform.position) > range)
            currentTarget = FindBestTarget();

        if (currentTarget == null) return;

        AimAtTarget();

        if (fireCooldown <= 0f)
        {
            Shoot();
            fireCooldown = 1f / fireRate;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns the in-range enemy that is furthest along the path.</summary>
    EnemyHealth FindBestTarget()
    {
        Collider[]   nearby      = Physics.OverlapSphere(transform.position, range);
        EnemyHealth  best        = null;
        float        bestProgress = -1f;

        foreach (var col in nearby)
        {
            if (!col.TryGetComponent<EnemyHealth>(out var eh)) continue;

            float progress = col.TryGetComponent<EnemyMovement>(out var em)
                ? em.PathProgress : 0f;

            if (progress > bestProgress)
            {
                bestProgress = progress;
                best         = eh;
            }
        }
        return best;
    }

    void AimAtTarget()
    {
        if (rotatingHead == null || currentTarget == null) return;
        Vector3 dir = currentTarget.transform.position - rotatingHead.position;
        if (dir != Vector3.zero)
            rotatingHead.rotation = Quaternion.LookRotation(dir);
    }

    void Shoot()
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject proj = Instantiate(
            projectilePrefab,
            firePoint.position,
            firePoint.rotation,
            transform.parent   // parent to battlefield so it inherits AR anchor movement
        );

        if (proj.TryGetComponent<Projectile>(out var p))
            p.Initialize(currentTarget.transform, damage);
    }

    // ── Editor helper ─────────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, range);
    }
#endif
}
