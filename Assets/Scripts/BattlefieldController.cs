using UnityEngine;

/// <summary>
/// Lives on the root of the Battlefield prefab.
/// Holds references to all the key children that other systems need:
///   - Waypoints (ordered path enemies walk)
///   - SpawnPoint (where enemies appear)
///   - GridCells  (valid tower placement slots)
///
/// HOW TO SET UP IN THE EDITOR:
///   1. Create a new empty GameObject: "Battlefield".
///   2. Add child empty GameObjects named "Waypoint_00", "Waypoint_01" … placed along your
///      desired enemy path. Assign them in order to the Waypoints array.
///   3. Add a child empty GameObject "SpawnPoint" at the start of the path.
///   4. Add child plane GameObjects for each tower slot; attach GridCell to each.
///      Assign them to the GridCells array.
///   5. Save as a Prefab and assign to BattlefieldPlacer.battlefieldPrefab.
/// </summary>
public class BattlefieldController : MonoBehaviour
{
    [Tooltip("Ordered list: enemies walk from index 0 → last entry (which is the player base).")]
    [SerializeField] Transform[] waypoints;

    [Tooltip("Enemies are instantiated here at the start of each wave.")]
    [SerializeField] Transform spawnPoint;

    [Tooltip("All valid grid cells where towers can be built.")]
    [SerializeField] GridCell[] gridCells;

    // ── Public accessors ──────────────────────────────────────────────────────
    public Transform[] Waypoints  => waypoints;
    public Transform   SpawnPoint => spawnPoint;
    public GridCell[]  GridCells  => gridCells;

#if UNITY_EDITOR
    // Draw the path in the Scene view so it is easy to position waypoints
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }

        Gizmos.color = Color.green;
        if (spawnPoint != null)
            Gizmos.DrawSphere(spawnPoint.position, 0.02f);
    }
#endif
}
