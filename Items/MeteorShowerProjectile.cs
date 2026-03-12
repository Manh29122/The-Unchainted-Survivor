using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MeteorShowerProjectile : MonoBehaviour
{
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private bool applyLifeSteal;

    private Vector3 impactPosition;
    private Vector2 travelDirection = Vector2.down;
    private float speed = 10f;
    private float damage = 10f;
    private float impactRadius = 1f;
    private GameObject impactPrefab;
    private float impactLifetime = 1f;
    private PlayerStats ownerStats;
    private bool hasImpacted;

    public void Initialize(Vector3 targetImpactPosition, Vector2 direction, float travelSpeed, float impactDamage, float radius, GameObject spawnedImpactPrefab, float spawnedImpactLifetime, PlayerStats playerStats)
    {
        impactPosition = targetImpactPosition;
        travelDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.down;
        speed = Mathf.Max(0.05f, travelSpeed);
        damage = Mathf.Max(0f, impactDamage);
        impactRadius = Mathf.Max(0.05f, radius);
        impactPrefab = spawnedImpactPrefab;
        impactLifetime = Mathf.Max(0.05f, spawnedImpactLifetime);
        ownerStats = playerStats;
        hasImpacted = false;

        float angle = Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnEnable()
    {
        hasImpacted = false;
    }

    private void Update()
    {
        if (hasImpacted)
        {
            return;
        }

        Vector3 toImpact = impactPosition - transform.position;
        float step = speed * Time.deltaTime;

        if (toImpact.sqrMagnitude <= step * step)
        {
            transform.position = impactPosition;
            Impact();
            return;
        }

        transform.position += (Vector3)(travelDirection * step);
    }

    private void Impact()
    {
        if (hasImpacted)
        {
            return;
        }

        hasImpacted = true;
        float totalDamageApplied = 0f;
        HashSet<EnemyUnit> damagedEnemyUnits = new HashSet<EnemyUnit>();
        HashSet<EnemyHealth> damagedEnemyHealths = new HashSet<EnemyHealth>();
        HashSet<EnemyHealthBridge> damagedEnemyBridges = new HashSet<EnemyHealthBridge>();

        Collider2D[] hits = enemyLayer.value != 0
            ? Physics2D.OverlapCircleAll(transform.position, impactRadius, enemyLayer)
            : Physics2D.OverlapCircleAll(transform.position, impactRadius);

        for (int index = 0; index < hits.Length; index++)
        {
            Collider2D hit = hits[index];
            if (hit == null || !hit.CompareTag(enemyTag))
            {
                continue;
            }

            EnemyUnit enemyUnit = hit.GetComponentInParent<EnemyUnit>();
            EnemyHealth enemyHealth = enemyUnit == null ? hit.GetComponentInParent<EnemyHealth>() : null;
            EnemyHealthBridge enemyHealthBridge = enemyUnit == null && enemyHealth == null
                ? hit.GetComponentInParent<EnemyHealthBridge>()
                : null;

            if (enemyUnit != null && damagedEnemyUnits.Add(enemyUnit))
            {
                enemyUnit.TakeDamage(Mathf.RoundToInt(damage));
                totalDamageApplied += damage;
            }
            else if (enemyHealth != null && damagedEnemyHealths.Add(enemyHealth))
            {
                enemyHealth.TakeDamage(damage);
                totalDamageApplied += damage;
            }
            else if (enemyHealthBridge != null && damagedEnemyBridges.Add(enemyHealthBridge))
            {
                enemyHealthBridge.TakeDamage(Mathf.RoundToInt(damage));
                totalDamageApplied += damage;
            }
        }

        if (applyLifeSteal)
        {
            ownerStats?.ApplyLifeStealFromDamage(totalDamageApplied);
        }

        if (impactPrefab != null)
        {
            GameObject impactObject = PoolManager.Spawn(impactPrefab, transform.position, Quaternion.identity, 8);
            if (impactObject != null)
            {
                impactObject.transform.position = transform.position;

                ImpactScaleFadeAutoReturn impactAutoReturn = impactObject.GetComponent<ImpactScaleFadeAutoReturn>();
                if (impactAutoReturn != null)
                {
                    impactAutoReturn.SetLifetime(impactLifetime);
                }
                else
                {
                    TimedReturnToPool timedReturn = impactObject.GetComponent<TimedReturnToPool>();
                    if (timedReturn == null)
                    {
                        timedReturn = impactObject.AddComponent<TimedReturnToPool>();
                    }

                    timedReturn.SetLifetime(impactLifetime);
                }
            }
        }

        if (!PoolManager.Return(gameObject))
        {
            Destroy(gameObject);
        }
    }
}