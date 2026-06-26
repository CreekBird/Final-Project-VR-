using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to each tower button in the shop UI.
/// Set towerIndex to match the index in TowerPlacer.towerOptions.
///
/// The button automatically reads the cost from TowerPlacer and displays it,
/// and dims itself when the player can't afford the tower.
/// </summary>
[RequireComponent(typeof(Button))]
public class TowerShopButton : MonoBehaviour
{
    [Tooltip("Index into TowerPlacer.towerOptions.")]
    [SerializeField] int towerIndex;

    [Tooltip("Text element that shows the tower cost.")]
    [SerializeField] TextMeshProUGUI costLabel;

    [Tooltip("Text element that shows the tower name (optional).")]
    [SerializeField] TextMeshProUGUI nameLabel;

    Button button;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);

        // Populate display text from TowerPlacer catalogue
        var option = TowerPlacer.Instance?.GetOption(towerIndex);
        if (option != null)
        {
            if (costLabel != null) costLabel.text = $"{option.cost}g";
            if (nameLabel != null) nameLabel.text = option.displayName;
        }

        // Keep the button affordance in sync with the player's gold
        if (GameManager.Instance != null)
            GameManager.Instance.OnResourcesChanged += RefreshAffordability;

        RefreshAffordability();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnResourcesChanged -= RefreshAffordability;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void OnClick()
    {
        TowerPlacer.Instance?.SelectTower(towerIndex);
    }

    /// <summary>Grey out the button if the player can't afford this tower.</summary>
    void RefreshAffordability()
    {
        if (GameManager.Instance == null || TowerPlacer.Instance == null) return;
        var option = TowerPlacer.Instance.GetOption(towerIndex);
        if (option == null) return;

        bool canAfford = GameManager.Instance.Gold >= option.cost;
        button.interactable = canAfford;
    }
}
