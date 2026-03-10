using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileDamage : MonoBehaviour
{
    [SerializeField] private float damage = 50f;
    [SerializeField] private bool destroyOnHit = false;

    private OrbitingProjectileSkill ownerSkill;

    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    public void SetOwner(OrbitingProjectileSkill skill)
    {
        ownerSkill = skill;
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

                Debug.Log($"Projectile hit enemy for {damage} damage!");

                if (destroyOnHit)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}