using UnityEngine;

/// <summary>
/// Marks a valid tower-placement slot on the battlefield grid.
///
/// HOW TO SET UP IN THE EDITOR:
///   - Add a flat quad/plane child to each grid slot and assign its Renderer here.
///   - Add a Collider (Box or Mesh) to this GameObject so raycasts from TowerPlacer can hit it.
///   - Assign this GameObject to a "Grid" layer (create one in Layer settings) so raycasts
///     can be filtered to only hit grid cells, avoiding enemy/tower colliders.
/// </summary>
public class GridCell : MonoBehaviour
{
    [SerializeField] Renderer cellRenderer;
    [SerializeField] Color availableColor = new Color(0.2f, 0.9f, 0.2f, 0.35f);
    [SerializeField] Color occupiedColor  = new Color(0.9f, 0.2f, 0.2f, 0.35f);

    public bool IsOccupied { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Start()
    {
        UpdateVisual();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += RefreshVisibility;
            RefreshVisibility();
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= RefreshVisibility;
    }

    // ── Public API ────────────────────────────────────────────────────────────
    /// <summary>Mark this cell as occupied; hides the visual indicator.</summary>
    public void Occupy()
    {
        IsOccupied = true;
        UpdateVisual();
        RefreshVisibility();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Show the cell indicator only during the Build phase (and only if free).</summary>
    void RefreshVisibility()
    {
        if (cellRenderer == null) return;
        bool isBuildPhase = GameManager.Instance != null &&
                            GameManager.Instance.State == GameManager.GameState.Build;
        cellRenderer.enabled = isBuildPhase && !IsOccupied;
    }

    void UpdateVisual()
    {
        if (cellRenderer == null) return;
        // Use a material property block so we do not create extra material instances
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", IsOccupied ? occupiedColor : availableColor);
        // Fallback for non-URP shaders
        block.SetColor("_Color",     IsOccupied ? occupiedColor : availableColor);
        cellRenderer.SetPropertyBlock(block);
    }
}
