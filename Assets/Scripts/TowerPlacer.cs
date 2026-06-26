using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles the touch-to-place-tower interaction during the Build phase.
///
/// Flow:
///   1. Player taps a tower button in the shop UI → TowerShopButton calls SelectTower(index).
///   2. Player taps a free GridCell on the battlefield → tower is placed and gold deducted.
///   3. Selection is cleared after placement (player must tap a button again for next tower).
///
/// Attach to any persistent GameObject in the Game scene.
/// </summary>
public class TowerPlacer : MonoBehaviour
{
    public static TowerPlacer Instance { get; private set; }

    // ── Tower catalogue ───────────────────────────────────────────────────────
    [System.Serializable]
    public class TowerOption
    {
        public string     displayName;
        public GameObject prefab;
        public int        cost;
    }

    [Tooltip("All buildable tower types. Index matches TowerShopButton.towerIndex.")]
    [SerializeField] List<TowerOption> towerOptions;

    [Tooltip("Layer mask for raycasting against GridCell colliders only. Create a 'Grid' layer and assign.")]
    [SerializeField] LayerMask gridLayerMask = ~0;   // default: all layers

    // ── State ─────────────────────────────────────────────────────────────────
    BattlefieldController battlefield;
    int selectedTowerIndex = -1;   // -1 = nothing selected

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (selectedTowerIndex < 0) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State != GameManager.GameState.Build) return;
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        // Do not consume taps on UI elements
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        TryPlaceTower(touch.position);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Called by BattlefieldPlacer once the battlefield exists.</summary>
    public void Initialize(BattlefieldController controller)
    {
        battlefield = controller;
    }

    /// <summary>Called by TowerShopButton when the player taps a shop entry.</summary>
    public void SelectTower(int index)
    {
        if (index < 0 || index >= towerOptions.Count) return;
        selectedTowerIndex = index;
    }

    /// <summary>Cancel current tower selection without placing anything.</summary>
    public void ClearSelection() => selectedTowerIndex = -1;

    /// <summary>Returns the TowerOption at the given index, or null if out of range.</summary>
    public TowerOption GetOption(int index) =>
        (index >= 0 && index < towerOptions.Count) ? towerOptions[index] : null;

    // ── Helpers ───────────────────────────────────────────────────────────────

    void TryPlaceTower(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 20f, gridLayerMask))
            return;

        if (!hit.collider.TryGetComponent<GridCell>(out var cell))
            return;

        if (cell.IsOccupied)
        {
            Debug.Log("[TowerPlacer] Cell is already occupied.");
            return;
        }

        TowerOption option = towerOptions[selectedTowerIndex];

        if (!GameManager.Instance.SpendGold(option.cost))
        {
            Debug.Log("[TowerPlacer] Not enough gold.");
            return;
        }

        // Instantiate tower snapped to the cell's position and orientation
        GameObject tower = Instantiate(
            option.prefab,
            cell.transform.position,
            cell.transform.rotation,
            battlefield.transform   // parent to battlefield anchor
        );

        cell.Occupy();
        selectedTowerIndex = -1;   // auto-deselect so the next tap doesn't accidentally place
    }
}
