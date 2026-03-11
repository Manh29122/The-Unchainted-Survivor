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
            Destroy(gameObject);
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

        if (enemyUnit != null)
        {
            enemyUnit.TakeDamage(Mathf.RoundToInt(damage));
        }
        else if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }
        else if (enemyHealthBridge != null)
        {
            enemyHealthBridge.TakeDamage(Mathf.RoundToInt(damage));
        }
        else
        {
            return;
        }

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}