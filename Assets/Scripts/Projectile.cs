using UnityEngine;

/// <summary>
/// A simple homing projectile.
/// Call Initialize() immediately after Instantiate to assign a target.
/// Self-destructs if the target dies before impact or if it travels too far.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Tooltip("World-space speed (m/s). Should be several times faster than enemies.")]
    [SerializeField] float speed = 2f;

    [Tooltip("Max lifetime in seconds — safety valve so stray projectiles don't linger.")]
    [SerializeField] float maxLifetime = 5f;

    // ── State ─────────────────────────────────────────────────────────────────
    Transform target;
    float     damage;
    float     lifetime;

    // ── Public API ────────────────────────────────────────────────────────────
    public void Initialize(Transform target, float damage)
    {
        this.target = target;
        this.damage = damage;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    void Update()
    {
        lifetime += Time.deltaTime;
        if (lifetime >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Target was destroyed (enemy died to another projectile)
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Move toward target
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position  += direction * speed * Time.deltaTime;
        transform.rotation   = Quaternion.LookRotation(direction);

        // Hit check
        float hitDistance = speed * Time.deltaTime + 0.01f; // generous threshold
        if (Vector3.Distance(transform.position, target.position) <= hitDistance)
            Hit();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    void Hit()
    {
        if (target != null && target.TryGetComponent<EnemyHealth>(out var eh))
            eh.TakeDamage(damage);

        Destroy(gameObject);
    }
}
