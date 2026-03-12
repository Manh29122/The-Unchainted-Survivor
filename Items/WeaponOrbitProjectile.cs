using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeaponOrbitProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private bool destroyOnHit = true;

    private Vector2 direction = Vector2.right;
    private float damage;
    private float lifeTimer;
    private PlayerStats ownerStats;
    private float knockbackForce;
    private float knockbackDuration;

    public void Initialize(Vector2 travelDirection, float projectileSpeed, float projectileDamage, float projectileLifetime)
    {
        direction = travelDirection.sqrMagnitude > 0f ? travelDirection.normalized : Vector2.right;
        speed = Mathf.Max(0f, projectileSpeed);
        damage = Mathf.Max(0f, projectileDamage);
        lifetime = Mathf.Max(0.05f, projectileLifetime);
        lifeTimer = lifetime;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void SetOwner(PlayerStats playerStats)
    {
        ownerStats = playerStats;
    }

    public void SetKnockback(float force, float duration)
    {
        knockbackForce = Mathf.Max(0f, force);
        knockbackDuration = Mathf.Max(0f, duration);
    }

    private void OnEnable()
    {
        lifeTimer = Mathf.Max(0.05f, lifetime);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            if (!PoolManager.Return(gameObject))
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(enemyTag))
        {
            return;
        }

        EnemyUnit enemyUnit = collision.GetComponentInParent<EnemyUnit>();
        EnemyHealth enemyHealth = enemyUnit == null ? collision.GetComponentInParent<EnemyHealth>() : null;
        EnemyHealthBridge enemyHealthBridge = enemyUnit == null && enemyHealth == null
            ? collision.GetComponentInParent<EnemyHealthBridge>()
            : null;
        Vector2 knockbackDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;

        if (enemyUnit != null)
        {
            enemyUnit.TakeDamageWithKnockback(Mathf.RoundToInt(damage), knockbackDirection, knockbackForce, knockbackDuration);
        }
        else if (enemyHealth != null)
        {
            enemyHealth.TakeDamageWithKnockback(damage, knockbackDirection, knockbackForce, knockbackDuration);
        }
        else if (enemyHealthBridge != null)
        {
            enemyHealthBridge.TakeDamageWithKnockback(Mathf.RoundToInt(damage), knockbackDirection, knockbackForce, knockbackDuration);
        }
        else
        {
            return;
        }

        ownerStats?.ApplyLifeStealFromDamage(damage);

        if (destroyOnHit)
        {
            if (!PoolManager.Return(gameObject))
            {
                Destroy(gameObject);
            }
        }
    }
}