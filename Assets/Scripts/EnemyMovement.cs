using UnityEngine;

/// <summary>
/// Moves an enemy along the battlefield waypoints.
/// Call Initialize() immediately after instantiation to give it the path.
/// When it reaches the final waypoint it deals damage to the base and destroys itself.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [Tooltip("World-space units per second. Keep small — the battlefield is AR-scale (~0.5-1 m).")]
    [SerializeField] float moveSpeed = 0.15f;

    [Tooltip("Distance (m) at which we consider a waypoint 'reached'.")]
    [SerializeField] float waypointThreshold = 0.02f;

    // ── State ─────────────────────────────────────────────────────────────────
    Transform[] waypoints;
    int         currentWaypointIndex;

    // ── Public API ────────────────────────────────────────────────────────────
    /// <summary>Assign the path. Must be called right after Instantiate.</summary>
    public void Initialize(Transform[] path)
    {
        waypoints             = path;
        currentWaypointIndex  = 0;

        // Snap to spawn position (waypoint 0)
        if (waypoints != null && waypoints.Length > 0)
            transform.position = waypoints[0].position;
    }

    /// <summary>
    /// Returns a 0-1 value representing how far along the path this enemy is.
    /// Towers use this to prioritise the enemy closest to the base.
    /// </summary>
    public float PathProgress =>
        (waypoints == null || waypoints.Length <= 1) ? 0f
        : (float)currentWaypointIndex / (waypoints.Length - 1);

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform target    = waypoints[currentWaypointIndex];
        Vector3   direction = (target.position - transform.position).normalized;

        transform.position += direction * moveSpeed * Time.deltaTime;

        // Face movement direction (y-axis only so the model stays upright)
        if (direction != Vector3.zero)
        {
            Quaternion look = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 10f);
        }

        // Check arrival
        if (Vector3.Distance(transform.position, target.position) <= waypointThreshold)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= waypoints.Length)
            {
                ReachBase();
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    void ReachBase()
    {
        GameManager.Instance.LoseLife();
        WaveManager.Instance.OnEnemyRemoved();
        Destroy(gameObject);
    }
}
