using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileDamage : MonoBehaviour
{
    [SerializeField] private float damage = 50f;
    [SerializeField] private bool destroyOnHit = false;

    private OrbitingProjectileSkill ownerSkill;
    private PlayerStats ownerStats;

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    public void SetOwner(OrbitingProjectileSkill skill)
    {
        ownerSkill = skill;
        ownerStats = ownerSkill != null ? ownerSkill.GetComponentInParent<PlayerStats>() : null;
    }

    public void SetOwner(PlayerStats playerStats)
    {
        ownerSkill = null;
        ownerStats = playerStats;
    }

    public void SetDestroyOnHit(bool shouldDestroyOnHit)
    {
        destroyOnHit = shouldDestroyOnHit;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyUnit enemyUnit = collision.GetComponentInParent<EnemyUnit>();
            EnemyHealth enemyHealth = enemyUnit == null ? collision.GetComponentInParent<EnemyHealth>() : null;

            if (enemyUnit != null || enemyHealth != null)
            {
                if (enemyUnit != null)
                {
                    enemyUnit.TakeDamage(damage);
                }
                else
                {
                    enemyHealth.TakeDamage(damage);
                }

                if (ownerStats == null)
                {
                    ownerStats = FindFirstObjectByType<PlayerStats>();
                }

                ownerStats?.ApplyLifeStealFromDamage(damage);

                Debug.Log($"Projectile hit enemy for {damage} damage!");

                if (destroyOnHit)
                {
                    if (!PoolManager.Return(gameObject))
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}