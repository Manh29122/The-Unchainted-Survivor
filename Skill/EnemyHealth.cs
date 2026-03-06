using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Basic enemy health component for taking damage from player skills
/// Attach this to enemy GameObjects
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health of the enemy")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Current health of the enemy")]
    [SerializeField] private float currentHealth;

    [Header("Death Settings")]
    [Tooltip("Should the enemy be destroyed when health reaches 0?")]
    [SerializeField] private bool destroyOnDeath = true;

    [Tooltip("Death effect prefab to spawn")]
    [SerializeField] private GameObject deathEffect;

    [Header("Floating Text")]
    [Tooltip("Prefab to show damage numbers (optional)")]
    [SerializeField] private GameObject floatingTextPrefab;

    [Header("Events")]
    [Tooltip("Called when enemy takes damage")]
    public UnityEvent<float> onTakeDamage;

    [Tooltip("Called when enemy dies")]
    public UnityEvent onDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Deals damage to the enemy
    /// </summary>
    /// <param name="damage">Amount of damage to deal</param>
    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0) return; // Already dead

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth); // Prevent negative health

        // show damage number
        if (floatingTextPrefab != null)
        {
            GameObject popup = Instantiate(floatingTextPrefab, transform.position, Quaternion.identity);
            FloatingText ft = popup.GetComponent<FloatingText>();
            if (ft != null)
                ft.SetText(damage.ToString("F0"), Color.red);
        }

        onTakeDamage?.Invoke(damage);

        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Heals the enemy
    /// </summary>
    /// <param name="healAmount">Amount to heal</param>
    public void Heal(float healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Cap at max health

        Debug.Log($"{gameObject.name} healed for {healAmount}. Health: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Instantly kills the enemy
    /// </summary>
    public void Kill()
    {
        currentHealth = 0;
        Die();
    }

    /// <summary>
    /// Handles enemy death
    /// </summary>
    private void Die()
    {
        onDeath?.Invoke();

        Debug.Log($"{gameObject.name} died!");

        // Spawn death effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // Destroy or disable the enemy
        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Gets the current health value
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Gets the maximum health value
    /// </summary>
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    /// <summary>
    /// Gets the health as a percentage (0-1)
    /// </summary>
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    /// <summary>
    /// Checks if the enemy is alive
    /// </summary>
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
}