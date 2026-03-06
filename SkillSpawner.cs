using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Spawn skill tự động theo thời gian.
/// Hỗ trợ nhiều loại skill, mỗi loại có pool riêng.
/// 
/// Setup:
///   1. Tạo các SkillConfig trong Inspector
///   2. Gắn prefab (ProjectileSkill / AoESkill / BuffAura) vào mỗi config
///   3. Gắn script này vào Player hoặc GameManager
/// </summary>
public class SkillSpawner : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  SKILL CONFIG
    // ─────────────────────────────────────────
    [System.Serializable]
    public class SkillConfig
    {
        [Header("Identity")]
        public string skillName = "New Skill";
        public SkillType type;
        public GameObject prefab;

        [Header("Timing")]
        [Tooltip("Thời gian giữa 2 lần kích hoạt (giây)")]
        public float cooldown = 2f;
        [HideInInspector] public float cooldownTimer;   // Runtime

        [Header("Pool")]
        [Tooltip("Số object pre-spawn trong pool")]
        public int poolSize = 10;
        [HideInInspector] public ObjectPool pool;       // Runtime

        [Header("Spawn Position")]
        public SpawnMode spawnMode = SpawnMode.AtPlayer;
        [Tooltip("Offset so với player (dùng cho SpawnMode.AtPlayer)")]
        public Vector2 spawnOffset = Vector2.zero;
        [Tooltip("Bán kính spawn ngẫu nhiên quanh player")]
        public float randomRadius = 3f;

        [Header("Projectile Only")]
        [Tooltip("Số đạn bắn mỗi lần (spray)")]
        public int projectileCount = 1;
        [Tooltip("Góc quét khi spray nhiều đạn")]
        public float spreadAngle = 30f;

        public bool IsReady => cooldownTimer <= 0f;
    }

    public enum SkillType { Projectile, AoE, Buff }
    public enum SpawnMode { AtPlayer, RandomAroundPlayer, TowardNearestEnemy }

    // ─────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────
    [Header("Skills")]
    public List<SkillConfig> skills = new List<SkillConfig>();

    [Header("References")]
    public Transform player;
    [Tooltip("Container chứa pooled objects (giữ Hierarchy gọn)")]
    public Transform poolContainer;

    [Header("Enemy Detection")]
    public float enemyDetectRadius = 15f;
    public LayerMask enemyLayer;

    // ─────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────
    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (poolContainer == null)
        {
            poolContainer = new GameObject("SkillPool").transform;
            poolContainer.SetParent(transform);
        }

        // Khởi tạo pool cho từng skill
        foreach (var skill in skills)
        {
            if (skill.prefab == null)
            {
                Debug.LogWarning($"[SkillSpawner] '{skill.skillName}' thiếu prefab!");
                continue;
            }

            // Tạo sub-container cho từng skill
            var container = new GameObject($"Pool_{skill.skillName}").transform;
            container.SetParent(poolContainer);

            skill.pool = new ObjectPool(skill.prefab, container, skill.poolSize);
            skill.cooldownTimer = 0f;

            Debug.Log($"[SkillSpawner] Pool '{skill.skillName}' khởi tạo ({skill.poolSize} objects)");
        }
    }

    void Update()
    {
        foreach (var skill in skills)
        {
            if (skill.pool == null) continue;

            // Đếm cooldown
            if (skill.cooldownTimer > 0f)
            {
                skill.cooldownTimer -= Time.deltaTime;
                continue;
            }

            // Đủ cooldown → kích hoạt skill
            TriggerSkill(skill);
            skill.cooldownTimer = skill.cooldown;
        }
    }

    // ─────────────────────────────────────────
    //  TRIGGER SKILL
    // ─────────────────────────────────────────
    void TriggerSkill(SkillConfig config)
    {
        switch (config.type)
        {
            case SkillType.Projectile: SpawnProjectile(config); break;
            case SkillType.AoE: SpawnAoE(config); break;
            case SkillType.Buff: SpawnBuff(config); break;
        }
    }

    // ─────────────────────────────────────────
    //  SPAWN TỪNG LOẠI SKILL
    // ─────────────────────────────────────────

    void SpawnProjectile(SkillConfig config)
    {
        Vector3 origin = GetSpawnPosition(config);
        Transform nearestEnemy = FindNearestEnemy();

        // Hướng bắn: về phía enemy, nếu không có thì bắn thẳng lên
        Vector2 baseDir = nearestEnemy != null
            ? ((Vector2)(nearestEnemy.position - origin)).normalized
            : Vector2.up;

        // Spawn nhiều đạn theo spread angle
        float startAngle = -(config.spreadAngle / 2f);
        float step = config.projectileCount > 1
            ? config.spreadAngle / (config.projectileCount - 1)
            : 0f;

        for (int i = 0; i < config.projectileCount; i++)
        {
            float angle = startAngle + step * i;
            Vector2 dir = RotateVector(baseDir, angle);

            GameObject obj = config.pool.Get(origin);
            SetupPooledObject(obj, config.pool);

            var projectile = obj.GetComponent<ProjectileSkill>();
            projectile?.SetDirection(dir);
        }

        Debug.Log($"[SkillSpawner] '{config.skillName}' → {config.projectileCount} projectile(s)");
    }

    void SpawnAoE(SkillConfig config)
    {
        Vector3 pos = GetSpawnPosition(config);

        GameObject obj = config.pool.Get(pos);
        SetupPooledObject(obj, config.pool);

        Debug.Log($"[SkillSpawner] '{config.skillName}' AoE tại {pos}");
    }

    void SpawnBuff(SkillConfig config)
    {
        // Buff spawn tại player
        Vector3 pos = player != null ? player.position : Vector3.zero;

        GameObject obj = config.pool.Get(pos);
        SetupPooledObject(obj, config.pool);

        Debug.Log($"[SkillSpawner] '{config.skillName}' Buff activated");
    }

    // ─────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────

    void SetupPooledObject(GameObject obj, ObjectPool pool)
    {
        // Gán pool để object tự trả về sau lifetime
        var pooled = obj.GetComponent<PooledObject>();
        if (pooled != null)
            pooled.OwnerPool = pool;
    }

    Vector3 GetSpawnPosition(SkillConfig config)
    {
        Vector3 playerPos = player != null ? player.position : Vector3.zero;

        switch (config.spawnMode)
        {
            case SpawnMode.AtPlayer:
                return playerPos + (Vector3)config.spawnOffset;

            case SpawnMode.RandomAroundPlayer:
                Vector2 rand = Random.insideUnitCircle.normalized * config.randomRadius;
                return playerPos + new Vector3(rand.x, rand.y, 0f);

            case SpawnMode.TowardNearestEnemy:
                Transform enemy = FindNearestEnemy();
                return enemy != null ? enemy.position : playerPos;

            default:
                return playerPos;
        }
    }

    Transform FindNearestEnemy()
    {
        if (player == null) return null;

        Collider2D[] hits = Physics2D.OverlapCircleAll(player.position, enemyDetectRadius, enemyLayer);

        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            float dist = Vector2.Distance(player.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = hit.transform;
            }
        }

        return nearest;
    }

    Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad), cos = Mathf.Cos(rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }

    // ─────────────────────────────────────────
    //  RUNTIME CONTROL
    // ─────────────────────────────────────────

    /// <summary>Kích hoạt/tắt 1 skill theo tên</summary>
    public void SetSkillActive(string skillName, bool active)
    {
        var skill = skills.Find(s => s.skillName == skillName);
        if (skill == null) return;

        if (!active)
            skill.pool?.ReturnAll();    // Thu hồi tất cả object đang bay

        // Tạm thời đặt cooldown rất lớn để skip
        skill.cooldownTimer = active ? 0f : float.MaxValue;
    }

    /// <summary>Reset cooldown của tất cả skill</summary>
    public void ResetAllCooldowns()
    {
        foreach (var skill in skills)
            skill.cooldownTimer = 0f;
    }

    // ─────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        // Vùng detect enemy
        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Gizmos.DrawWireSphere(player.position, enemyDetectRadius);

        // Random spawn radius của từng skill
        foreach (var skill in skills)
        {
            if (skill.spawnMode != SpawnMode.RandomAroundPlayer) continue;
            Gizmos.color = new Color(0, 1, 1, 0.2f);
            Gizmos.DrawWireSphere(player.position, skill.randomRadius);
        }
    }
}