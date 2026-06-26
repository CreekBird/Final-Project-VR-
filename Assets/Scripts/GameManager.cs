using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton that owns all game-level state: gold, lives, wave number, and game state machine.
/// Other scripts communicate through this class rather than directly to each other.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── State ────────────────────────────────────────────────────────────────
    public enum GameState
    {
        Placement,  // Scanning for a plane and placing the battlefield
        Build,      // Player can place towers; wave not yet started
        Wave,       // Enemies are spawning / alive
        GameOver,
        Victory
    }

    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Starting Values")]
    [SerializeField] int startingLives = 10;
    [SerializeField] int startingGold  = 150;

    // ── Public read-only state ───────────────────────────────────────────────
    public GameState State        { get; private set; } = GameState.Placement;
    public int       Lives        { get; private set; }
    public int       Gold         { get; private set; }
    public int       CurrentWave  { get; private set; }

    // ── Events ───────────────────────────────────────────────────────────────
    /// <summary>Fired whenever State changes.</summary>
    public event Action OnStateChanged;

    /// <summary>Fired whenever Gold, Lives, or CurrentWave changes.</summary>
    public event Action OnResourcesChanged;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        Lives = startingLives;
        Gold  = startingGold;
        OnResourcesChanged?.Invoke();
    }

    // ── State machine ─────────────────────────────────────────────────────────
    public void SetState(GameState newState)
    {
        State = newState;
        OnStateChanged?.Invoke();
    }

    // ── Economy ───────────────────────────────────────────────────────────────
    /// <summary>Returns true and deducts gold if the player can afford it.</summary>
    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        OnResourcesChanged?.Invoke();
        return true;
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        OnResourcesChanged?.Invoke();
    }

    // ── Lives ─────────────────────────────────────────────────────────────────
    public void LoseLife()
    {
        Lives = Mathf.Max(0, Lives - 1);
        OnResourcesChanged?.Invoke();
        if (Lives <= 0)
            SetState(GameState.GameOver);
    }

    // ── Wave tracking ─────────────────────────────────────────────────────────
    public void SetWave(int wave)
    {
        CurrentWave = wave;
        OnResourcesChanged?.Invoke();
    }

    // ── Scene helpers ─────────────────────────────────────────────────────────
    public void LoadMainMenu() => SceneManager.LoadScene("MainMenu");
    public void RestartGame()  => SceneManager.LoadScene("Game");
}
