using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyUnit : MonoBehaviour
{
    [Header("Base Data")]
    [SerializeField] private EnemyRuntimeData baseData = new EnemyRuntimeData
    {
        maxHealth = 50,
        moveSpeed = 2f,
        contactDamage = 10
    };

    [Header("Runtime")]
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private float stopDistance = 0.1f;

    [Header("Death Drops")]
    [SerializeField] private GameObject expDropPrefab;
    [SerializeField] private int expDropCount = 1;
    [SerializeField] private GameObject goldDropPrefab;
    [SerializeField] private int goldDropCount = 1;
    [SerializeField] private GameObject healthDropPrefab;
    [SerializeField, Range(0f, 1f)] private float healthDropChance = 0f;
    [SerializeField] private int healthDropCount = 1;
    [SerializeField] private bool alsoUseRandomDropTable;

    private EnemyRuntimeData runtimeData;
    private int currentHealth;
    private bool isDead;
    private Transform targetPlayer;
    private PlayerStats targetPlayerStats;
    private Vector2 knockbackDirection;
    private float knockbackForce;
    private float knockbackDuration;
    private float knockbackTimer;
    private bool hasSpawnedDeathDrops;

    public event Action<EnemyUnit, int> OnDamaged;
    public event Action<EnemyUnit> OnDied;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => Mathf.Max(1, runtimeData.maxHealth);
    public bool IsAlive => !isDead && currentHealth > 0;
    public EnemyRuntimeData RuntimeData => runtimeData;

    private void Awake()
    {
        runtimeData = GetValidatedData(baseData);
        ResetRuntimeState();
        ResolvePlayer();
    }

    private void OnEnable()
    {
        if (runtimeData.maxHealth <= 0)
        {
            runtimeData = GetValidatedData(baseData);
        }

        ResetRuntimeState();

        if (targetPlayer == null && autoFindPlayer)
        {
            ResolvePlayer();
        }
    }

    private void OnDestroy()
    {
        if (isDead || currentHealth <= 0)
        {
            SpawnDeathDrops();
        }
    }

    private void Update()
    {
        if (!IsAlive)
        {
            return;
        }

        if (UpdateKnockback())
        {
            return;
        }

        if (targetPlayer == null)
        {
            ResolvePlayer();
            return;
        }

        Vector2 currentPosition = transform.position;
        Vector2 targetPosition = targetPlayer.position;
        Vector2 toPlayer = targetPosition - currentPosition;
        float distance = toPlayer.magnitude;

        if (distance <= stopDistance)
        {
            return;
        }

        Vector2 velocity = toPlayer.normalized * Mathf.Max(0f, runtimeData.moveSpeed);
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other.transform);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.transform);
    }

    public void Initialize(EnemyRuntimeData data, Transform playerTransform = null)
    {
        runtimeData = GetValidatedData(data);
        targetPlayer = playerTransform;
        targetPlayerStats = playerTransform != null ? playerTransform.GetComponent<PlayerStats>() : null;
        ResetRuntimeState();
    }

    public void SetTargetPlayer(Transform playerTransform)
    {
        targetPlayer = playerTransform;
        targetPlayerStats = playerTransform != null ? playerTransform.GetComponent<PlayerStats>() : null;
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(Mathf.RoundToInt(damage));
    }

    public void TakeDamage(int damage)
    {
        int validDamage = Mathf.Max(0, damage);
        if (!IsAlive || validDamage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - validDamage);
        OnDamaged?.Invoke(this, validDamage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (!IsAlive)
        {
            return;
        }

        Vector2 normalizedDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.zero;
        if (normalizedDirection == Vector2.zero)
        {
            return;
        }

        knockbackDirection = normalizedDirection;
        knockbackForce = Mathf.Max(0f, force);
        knockbackDuration = Mathf.Max(0f, duration);
        knockbackTimer = knockbackDuration;
    }

    public void Kill()
    {
        if (!IsAlive)
        {
            return;
        }

        currentHealth = 0;
        Die();
    }

    private void TryDamagePlayer(Transform otherTransform)
    {
        if (!IsAlive || otherTransform == null || runtimeData.contactDamage <= 0)
        {
            return;
        }

        PlayerStats playerStats = null;

        if (targetPlayer != null && otherTransform == targetPlayer)
        {
            playerStats = targetPlayerStats;
        }
        else if (otherTransform.CompareTag(playerTag))
        {
            playerStats = otherTransform.GetComponent<PlayerStats>();
        }

        if (playerStats == null)
        {
            return;
        }

        playerStats.TakeDamage(runtimeData.contactDamage);
    }

    private void ResolvePlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject == null)
        {
            return;
        }

        targetPlayer = playerObject.transform;
        targetPlayerStats = playerObject.GetComponent<PlayerStats>();
    }

    private void ResetRuntimeState()
    {
        runtimeData = GetValidatedData(runtimeData.maxHealth > 0 ? runtimeData : baseData);
        currentHealth = MaxHealth;
        isDead = false;
        hasSpawnedDeathDrops = false;
        knockbackDirection = Vector2.zero;
        knockbackForce = 0f;
        knockbackDuration = 0f;
        knockbackTimer = 0f;
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        SpawnDeathDrops();

        OnDied?.Invoke(this);

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void SpawnDeathDrops()
    {
        Debug.Log($"[EnemyUnit] Attempting to spawn death drops for {gameObject.name}.");
        if (hasSpawnedDeathDrops)
        {
            return;
        }

        hasSpawnedDeathDrops = true;

        if (ItemSpawner.Instance != null)
        {
            if (expDropPrefab != null && expDropCount > 0)
            {
                ItemSpawner.Instance.DropPrefab(expDropPrefab, transform.position, expDropCount);
            }

            if (goldDropPrefab != null && goldDropCount > 0)
            {
                ItemSpawner.Instance.DropPrefab(goldDropPrefab, transform.position, goldDropCount);
            }

            if (healthDropPrefab != null && healthDropCount > 0 && UnityEngine.Random.value <= healthDropChance)
            {
                ItemSpawner.Instance.DropPrefab(healthDropPrefab, transform.position, healthDropCount);
            }

            if (alsoUseRandomDropTable)
            {
                ItemSpawner.Instance.Drop(transform.position);
            }

            Debug.Log($"[EnemyUnit] Spawned death drops for {gameObject.name}.");
        }
        else
        {
            Debug.LogWarning($"[EnemyUnit] {gameObject.name} died but ItemSpawner.Instance is null, so no drops were spawned.");
        }

        if (expDropPrefab == null && goldDropPrefab == null && healthDropPrefab == null && !alsoUseRandomDropTable)
        {
            Debug.LogWarning($"[EnemyUnit] {gameObject.name} died but no death drop prefabs are assigned.");
        }
    }

    private EnemyRuntimeData GetValidatedData(EnemyRuntimeData data)
    {
        data.maxHealth = Mathf.Max(1, data.maxHealth);
        data.moveSpeed = Mathf.Max(0f, data.moveSpeed);
        data.contactDamage = Mathf.Max(0, data.contactDamage);
        return data;
    }

    private bool UpdateKnockback()
    {
        if (knockbackTimer <= 0f || knockbackForce <= 0f || knockbackDirection == Vector2.zero)
        {
            return false;
        }

        float normalizedTime = knockbackDuration > 0f ? knockbackTimer / knockbackDuration : 0f;
        float currentForce = knockbackForce * Mathf.Clamp01(normalizedTime);
        transform.position += (Vector3)(knockbackDirection * currentForce * Time.deltaTime);

        knockbackTimer -= Time.deltaTime;
        if (knockbackTimer <= 0f)
        {
            knockbackDirection = Vector2.zero;
            knockbackForce = 0f;
            knockbackDuration = 0f;
            knockbackTimer = 0f;
        }

        return true;
    }
}