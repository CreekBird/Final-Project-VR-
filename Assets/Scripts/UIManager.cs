using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives all in-game UI:
///   - Scanning panel (shown while waiting for plane detection)
///   - HUD (lives, gold, wave counter, "Send Wave" button, tower shop)
///   - Game Over / Victory overlays
///
/// Attach to a single persistent GameObject in the Game scene and wire up
/// all references in the Inspector.
/// </summary>
public class UIManager : MonoBehaviour
{
    // ── Inspector groups ──────────────────────────────────────────────────────

    [Header("Panels")]
    [Tooltip("Shown while the player is scanning for a surface.")]
    [SerializeField] GameObject scanningPanel;

    [Tooltip("Main HUD shown once the battlefield is placed.")]
    [SerializeField] GameObject hudPanel;

    [Tooltip("Shown when the player runs out of lives.")]
    [SerializeField] GameObject gameOverPanel;

    [Tooltip("Shown when all waves are cleared.")]
    [SerializeField] GameObject victoryPanel;

    [Header("HUD Elements")]
    [SerializeField] TextMeshProUGUI livesText;
    [SerializeField] TextMeshProUGUI goldText;
    [SerializeField] TextMeshProUGUI waveText;

    [Tooltip("Button that sends the next wave of enemies.")]
    [SerializeField] Button sendWaveButton;

    [Tooltip("Optional label on the Send Wave button showing which wave comes next.")]
    [SerializeField] TextMeshProUGUI sendWaveLabel;

    [Header("Tower Shop")]
    [Tooltip("Parent panel that holds all TowerShopButtons. Hidden during Wave phase.")]
    [SerializeField] GameObject shopPanel;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Start()
    {
        // Subscribe to game events
        GameManager.Instance.OnStateChanged     += OnStateChanged;
        GameManager.Instance.OnResourcesChanged += UpdateHUD;

        // Wire Send Wave button
        if (sendWaveButton != null)
            sendWaveButton.onClick.AddListener(() => WaveManager.Instance.StartNextWave());

        // Initial panel state — everything hidden except scanning panel
        SetAllPanelsOff();
        if (scanningPanel != null) scanningPanel.SetActive(true);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged     -= OnStateChanged;
            GameManager.Instance.OnResourcesChanged -= UpdateHUD;
        }
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    void OnStateChanged()
    {
        var state = GameManager.Instance.State;

        SetAllPanelsOff();

        switch (state)
        {
            case GameManager.GameState.Placement:
                if (scanningPanel != null) scanningPanel.SetActive(true);
                break;

            case GameManager.GameState.Build:
                if (hudPanel   != null) hudPanel.SetActive(true);
                if (shopPanel  != null) shopPanel.SetActive(true);
                if (sendWaveButton != null) sendWaveButton.gameObject.SetActive(true);
                break;

            case GameManager.GameState.Wave:
                if (hudPanel  != null) hudPanel.SetActive(true);
                if (shopPanel != null) shopPanel.SetActive(false);  // no building during wave
                if (sendWaveButton != null) sendWaveButton.gameObject.SetActive(false);
                break;

            case GameManager.GameState.GameOver:
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
                break;

            case GameManager.GameState.Victory:
                if (victoryPanel != null) victoryPanel.SetActive(true);
                break;
        }

        UpdateHUD();
    }

    void UpdateHUD()
    {
        if (GameManager.Instance == null) return;

        if (livesText != null)
            livesText.text = $"♥  {GameManager.Instance.Lives}";

        if (goldText != null)
            goldText.text  = $"$  {GameManager.Instance.Gold}";

        if (waveText != null)
        {
            int current = GameManager.Instance.CurrentWave;
            int total   = WaveManager.Instance != null ? WaveManager.Instance.TotalWaves : 0;
            waveText.text = current == 0
                ? "Wave –"
                : $"Wave {current} / {total}";
        }

        if (sendWaveLabel != null && WaveManager.Instance != null)
        {
            int next = GameManager.Instance.CurrentWave + 1;
            sendWaveLabel.text = $"Send Wave {next}";
        }
    }

    // ── Button callbacks (wire in Inspector) ──────────────────────────────────

    /// <summary>Restart button on the Game Over panel.</summary>
    public void OnRestartButton() => GameManager.Instance.RestartGame();

    /// <summary>Main Menu button on the Game Over / Victory panel.</summary>
    public void OnMainMenuButton() => GameManager.Instance.LoadMainMenu();

    // ── Helpers ───────────────────────────────────────────────────────────────

    void SetAllPanelsOff()
    {
        if (scanningPanel != null) scanningPanel.SetActive(false);
        if (hudPanel      != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel  != null) victoryPanel.SetActive(false);
        if (shopPanel     != null) shopPanel.SetActive(false);
        if (sendWaveButton != null) sendWaveButton.gameObject.SetActive(false);
    }
}
