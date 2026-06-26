using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages an enemy's health pool.
/// Attach alongside EnemyMovement on every enemy prefab.
///
/// Optional: assign a world-space health bar Slider in the Inspector
/// to give visual feedback (set the Slider's Canvas to World Space).
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] float maxHealth  = 100f;
    [SerializeField] int   goldReward = 25;

    [Header("Optional UI")]
    [Tooltip("World-space Slider that shows the health bar above the enemy.")]
    [SerializeField] Slider healthBarSlider;

    public float CurrentHealth { get; private set; }
    public float HealthFraction => CurrentHealth / maxHealth;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Awake()
    {
        CurrentHealth = maxHealth;
        UpdateHealthBar();
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        UpdateHealthBar();

        if (CurrentHealth <= 0f)
            Die();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    void Die()
    {
        GameManager.Instance.AddGold(goldReward);
        WaveManager.Instance.OnEnemyRemoved();
        Destroy(gameObject);
    }

    void UpdateHealthBar()
    {
        if (healthBarSlider != null)
            healthBarSlider.value = HealthFraction;
    }
}
