using System.Collections;
using UnityEngine;

// ══════════════════════════════════════════════
//  PROJECTILE — viên đạn bay về phía kẻ địch
// ══════════════════════════════════════════════
/// <summary>
/// Gắn vào Projectile Prefab.
/// Tự bay về hướng được gán, gây damage khi chạm enemy.
/// </summary>
public class ProjectileSkill : PooledObject
{
    [Header("Projectile")]
    public float speed = 8f;
    public int damage = 20;
    public string enemyTag = "Enemy";

    private Vector2 direction;
    private Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    /// <summary>Gán hướng bay trước khi spawn</summary>
    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;

        // Xoay sprite theo hướng bay
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    protected override void OnActivated()
    {
        if (rb != null)
            rb.linearVelocity = direction * speed;
    }

    protected override void OnDeactivated()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(enemyTag)) return;

        // Gây damage
        var enemyHealth = other.GetComponentInParent<EnemyHealthBridge>();
        enemyHealth?.TakeDamage(damage);

        ReturnToPool();
    }
}

// ══════════════════════════════════════════════
//  AOE SKILL — vùng sát thương tại chỗ
// ══════════════════════════════════════════════
/// <summary>
/// Gắn vào AoE Prefab (CircleCollider2D trigger).
/// Gây damage cho tất cả enemy trong vùng, có tick damage.
/// </summary>
public class AoESkill : PooledObject
{
    [Header("AoE")]
    public int damagePerTick = 15;
    public float tickInterval = 0.5f;
    public string enemyTag = "Enemy";

    private float tickTimer;

    protected override void OnActivated()
    {
        tickTimer = 0f;
        // Hiệu ứng scale-in
        StartCoroutine(ScaleIn());
    }

    void Update()
    {
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer = 0f;
            DamageEnemiesInRange();
        }
    }

    void DamageEnemiesInRange()
    {
        // Dùng OverlapCircle để tìm tất cả enemy trong bán kính
        float radius = transform.localScale.x * 0.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag(enemyTag)) continue;
            hit.GetComponentInParent<EnemyHealthBridge>()?.TakeDamage(damagePerTick);
        }
    }

    IEnumerator ScaleIn()
    {
        Vector3 targetScale = transform.localScale;
        transform.localScale = Vector3.zero;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }
    }
}

// ══════════════════════════════════════════════
//  BUFF AURA — theo sát player, buff trong vùng
// ══════════════════════════════════════════════
/// <summary>
/// Gắn vào Aura Prefab.
/// Theo player, áp dụng buff cho player trong suốt lifetime.
/// </summary>
public class BuffAura : PooledObject
{
    [Header("Aura")]
    public float healPerSecond = 5f;
    public float speedBonus = 1.5f;         // Nhân tốc độ player
    public bool followPlayer = true;

    private Transform playerTransform;
    //private PlayerHealth playerHealth;

    protected override void OnActivated()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        playerTransform = player.transform;
        //playerHealth = player.GetComponent<PlayerHealth>();

        // Áp dụng speed buff
        // player.GetComponent<PlayerMovement>()?.ApplySpeedMultiplier(speedBonus);
    }

    protected override void OnDeactivated()
    {
        // Gỡ speed buff
        // playerTransform?.GetComponent<PlayerMovement>()?.RemoveSpeedMultiplier(speedBonus);
    }

    void Update()
    {
        // Theo player
        if (followPlayer && playerTransform != null)
            transform.position = playerTransform.position;

        // Hồi máu theo giây
        //if (playerHealth != null)
        //    playerHealth.Heal(Mathf.RoundToInt(healPerSecond * Time.deltaTime));
    }
}

// ══════════════════════════════════════════════
//  BRIDGE: ECS Enemy → MonoBehaviour damage
// ══════════════════════════════════════════════
/// <summary>
/// Gắn vào Enemy GameObject để nhận damage từ skill.
/// (Dùng tạm khi enemy là GameObject; nếu DOTS thì bridge sang ECS)
/// </summary>
public class EnemyHealthBridge : MonoBehaviour
{
    public int hp = 50;

    private EnemyUnit enemyUnit;

    private void Awake()
    {
        enemyUnit = GetComponentInParent<EnemyUnit>();
    }

    public void TakeDamage(int dmg)
    {
        if (enemyUnit != null)
        {
            enemyUnit.TakeDamage(dmg);
            return;
        }

        hp -= dmg;
        if (hp <= 0)
        {
            Debug.LogWarning($"[EnemyHealthBridge] {gameObject.name} has no EnemyUnit in parent hierarchy. Disabling hit object directly, so EnemyUnit death drops will not run.");
            gameObject.SetActive(false); // hoặc gọi ECS DestroyEntity
        }
    }
}