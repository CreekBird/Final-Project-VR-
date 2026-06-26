using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ── Wave configuration ─────────────────────────────────────────────────────────
[System.Serializable]
public class WaveConfig
{
    [Tooltip("Enemy prefab to spawn (must have EnemyMovement + EnemyHealth).")]
    public GameObject enemyPrefab;

    [Tooltip("How many enemies spawn in this wave.")]
    public int count = 5;

    [Tooltip("Seconds between each enemy spawn.")]
    public float spawnInterval = 1.2f;

    [Tooltip("Gold bonus awarded when the whole wave is cleared.")]
    public int completionBonus = 50;
}

/// <summary>
/// Spawns wave after wave of enemies, tracks living enemy count, and notifies
/// GameManager when all waves are beaten.
///
/// Attach to a persistent GameObject in the Game scene.
/// Call Initialize() once the battlefield is placed, then StartNextWave()
/// (usually wired to the "Send Wave" UI button via UIManager).
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Definitions")]
    [SerializeField] List<WaveConfig> waves;

    [Header("Timing")]
    [Tooltip("Seconds of grace period between waves (Build phase).")]
    [SerializeField] float timeBetweenWaves = 5f;

    // ── State ─────────────────────────────────────────────────────────────────
    BattlefieldController battlefield;
    int  currentWaveIndex;
    int  enemiesAlive;
    bool waveActive;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Called by BattlefieldPlacer once the battlefield is in the world.</summary>
    public void Initialize(BattlefieldController controller)
    {
        battlefield = controller;
        currentWaveIndex = 0;
    }

    /// <summary>
    /// Called by the "Send Wave" button.
    /// No-ops silently if a wave is already active or the battlefield isn't placed.
    /// </summary>
    public void StartNextWave()
    {
        if (waveActive)           return;
        if (battlefield == null)  return;
        if (GameManager.Instance.State != GameManager.GameState.Build) return;

        if (currentWaveIndex >= waves.Count)
        {
            GameManager.Instance.SetState(GameManager.GameState.Victory);
            return;
        }

        GameManager.Instance.SetWave(currentWaveIndex + 1);
        GameManager.Instance.SetState(GameManager.GameState.Wave);
        StartCoroutine(SpawnWaveRoutine(waves[currentWaveIndex]));
        currentWaveIndex++;
    }

    /// <summary>
    /// Called by EnemyMovement (reached base) and EnemyHealth (killed).
    /// Decrements the alive counter; triggers wave-clear when it hits zero.
    /// </summary>
    public void OnEnemyRemoved()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        if (enemiesAlive <= 0 && waveActive)
            StartCoroutine(WaveCompleteRoutine());
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    IEnumerator SpawnWaveRoutine(WaveConfig wave)
    {
        waveActive   = true;
        enemiesAlive = wave.count;

        for (int i = 0; i < wave.count; i++)
        {
            SpawnEnemy(wave.enemyPrefab);
            yield return new WaitForSeconds(wave.spawnInterval);
        }
        // Wave keeps running until all enemies are removed (via OnEnemyRemoved)
    }

    IEnumerator WaveCompleteRoutine()
    {
        waveActive = false;

        // Award completion bonus for the wave we just finished
        int lastWaveIndex = currentWaveIndex - 1;
        if (lastWaveIndex >= 0 && lastWaveIndex < waves.Count)
            GameManager.Instance.AddGold(waves[lastWaveIndex].completionBonus);

        // Check if that was the last wave
        if (currentWaveIndex >= waves.Count)
        {
            yield return new WaitForSeconds(1f);
            GameManager.Instance.SetState(GameManager.GameState.Victory);
        }
        else
        {
            // Brief pause then return to Build phase for the next wave
            yield return new WaitForSeconds(timeBetweenWaves);
            if (GameManager.Instance.State == GameManager.GameState.Wave)
                GameManager.Instance.SetState(GameManager.GameState.Build);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void SpawnEnemy(GameObject prefab)
    {
        if (battlefield == null || prefab == null) return;

        GameObject enemy = Instantiate(
            prefab,
            battlefield.SpawnPoint.position,
            Quaternion.identity,
            battlefield.transform   // parent to battlefield so it moves with the AR anchor
        );

        if (enemy.TryGetComponent<EnemyMovement>(out var movement))
            movement.Initialize(battlefield.Waypoints);
        else
            Debug.LogWarning("[WaveManager] Enemy prefab is missing an EnemyMovement component.");
    }

    // ── Read-only info ────────────────────────────────────────────────────────
    public int  TotalWaves   => waves.Count;
    public bool IsWaveActive => waveActive;
}
