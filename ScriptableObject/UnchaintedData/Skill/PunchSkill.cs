using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PunchSkill : MonoBehaviour
{
    [Header("Punch Settings")]
    [SerializeField] private float punchDamage = 100f;
    [SerializeField] private float punchRange = 3f;
    [SerializeField] private float punchAngle = 45f;

    [Header("Multi-Direction Punch")]
    [Tooltip("Number of punch directions (1 = single, 3 = spread, 5 = wide)")]
    [SerializeField] private int punchDirections = 1;
    [Tooltip("Angle spread between multiple punches (degrees)")]
    [SerializeField] private float spreadAngle = 30f;

    [Header("Health Restore")]
    [SerializeField] private int healthRestore = 10;

    [Header("Knockback")]
    [Tooltip("Force applied to enemies when hit")]
    [SerializeField] private float knockbackForce = 20f;
    [Tooltip("Duration of knockback in seconds")]
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Cooldown")]
    [SerializeField] private float cooldownTime = 3f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem punchEffect;
    [SerializeField] private AudioClip punchSound;
    [SerializeField] private AudioClip hitSound;

    [Tooltip("Prefab used to show damage numbers")]
    [SerializeField] private GameObject floatingTextPrefab;

    [Header("Input")]
    [SerializeField] private KeyCode punchKey = KeyCode.Q;
    [Tooltip("Optional joystick to aim punch direction")]
    [SerializeField] private Joystick joystick;

    private bool isPunching = false;
    private bool canPunch = true;
    private float lastPunchTime;

    private PlayerStats playerStats;
    private Animator animator;
    private AudioSource audioSource;

    public GameObject _firePunch;
    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (playerStats == null)
        {
            Debug.LogError("PunchSkill requires PlayerStats!");
            enabled = false;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (joystick == null)
        {
            joystick = FindFirstObjectByType<Joystick>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(punchKey) && canPunch && !isPunching)
        {
            StartPunch();
        }

        if (!canPunch && Time.time - lastPunchTime >= cooldownTime)
        {
            canPunch = true;
        }
    }

    public void StartPunch()
    {
        if (!canPunch || isPunching) return;

        isPunching = true;
        canPunch = false;
        lastPunchTime = Time.time;

        PlayPunchEffects();
        
        // Get punch directions and spawn fire punches
        Vector2 forwardVec2 = transform.right;
        if (joystick != null && joystick.IsTouching && joystick.Input != Vector2.zero)
        {
            forwardVec2 = joystick.Input.normalized;
        }
        List<Vector2> punchDirs = GeneratePunchDirections(forwardVec2);
        SpawnFirePunches(punchDirs);
        
        PerformPunch();
        StartCoroutine(CompletePunch());

        Debug.Log("Punch! Damage: " + punchDamage);
    }

    private void PerformPunch()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, punchRange);
        bool hitEnemy = false;
        int enemiesHit = 0;

        Vector2 forwardVec2 = transform.right;
        if (joystick != null && joystick.IsTouching && joystick.Input != Vector2.zero)
        {
            forwardVec2 = joystick.Input.normalized;
        }

        List<Vector2> punchDirs = GeneratePunchDirections(forwardVec2);

        Debug.Log(punchDirs.Count + " punch directions generated.");
        foreach (Collider2D collider in hitColliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                Vector2 dirToEnemy = (collider.transform.position - transform.position).normalized;
                bool isInCone = false;

                foreach (Vector2 punchDir in punchDirs)
                {
                    float angleToEnemy = Vector2.Angle(punchDir, dirToEnemy);
                    if (angleToEnemy <= punchAngle / 2f)
                    {
                        isInCone = true;
                        break;
                    }
                }

                if (isInCone)
                {
                    EnemyUnit enemyUnit = collider.GetComponentInParent<EnemyUnit>();
                    EnemyHealth enemyHealth = enemyUnit == null ? collider.GetComponentInParent<EnemyHealth>() : null;

                    if (enemyUnit != null || enemyHealth != null)
                    {
                        if (enemyUnit != null)
                        {
                            enemyUnit.TakeDamage(punchDamage);
                        }
                        else
                        {
                            enemyHealth.TakeDamage(punchDamage);
                        }

                        playerStats?.ApplyLifeStealFromDamage(punchDamage);

                        if (floatingTextPrefab != null)
                        {
                            GameObject popup = Instantiate(floatingTextPrefab, collider.transform.position, Quaternion.identity);
                            FloatingText ft = popup.GetComponent<FloatingText>();
                            if (ft != null)
                                ft.SetText(punchDamage.ToString(), Color.red);
                        }

                        Vector2 knockDir = (collider.transform.position - transform.position).normalized;
                        if (enemyUnit != null)
                        {
                            enemyUnit.ApplyKnockback(knockDir, knockbackForce, knockbackDuration);
                        }
                        else
                        {
                            Rigidbody2D enemyRb = collider.GetComponentInParent<Rigidbody2D>();
                            if (enemyRb != null)
                            {
                                enemyRb.linearVelocity = Vector2.zero;
                                StartCoroutine(ApplyKnockback(enemyRb, knockDir));
                            }
                        }

                        hitEnemy = true;
                        enemiesHit++;
                        Debug.Log("Hit enemy for " + punchDamage + "!");
                    }
                }
            }
        }

        if (hitEnemy && playerStats != null)
        {
            playerStats.Heal(healthRestore);
            Debug.Log("Healed " + healthRestore + " HP!");

            if (hitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
        }
    }

    private List<Vector2> GeneratePunchDirections(Vector2 baseDirection)
    {
        List<Vector2> directions = new List<Vector2>();

        if (punchDirections <= 1)
        {
            directions.Add(baseDirection);
        }
        else if (punchDirections == 2)
        {
            directions.Add(Rotate(baseDirection, spreadAngle / 2f));
            directions.Add(Rotate(baseDirection, -spreadAngle / 2f));
        }
        else
        {
            float angleStep = spreadAngle / (punchDirections - 1);
            for (int i = 0; i < punchDirections; i++)
            {
                float angle = (spreadAngle / 2f) - (i * angleStep);
                directions.Add(Rotate(baseDirection, angle));
            }
        }

        return directions;
    }

    private Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private void PlayPunchEffects()
    {
        if (punchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(punchSound);
        }

        if (punchEffect != null)
        {
            punchEffect.Play();
        }
    }

    /// <summary>
    /// Spawns fire punch visuals for each punch direction
    /// </summary>
    private void SpawnFirePunches(List<Vector2> punchDirections)
    {
        if (_firePunch == null) return;

        Debug.Log($"[SpawnFirePunches] Spawning {punchDirections.Count} fire punches");
        
        foreach (Vector2 dir in punchDirections)
        {
            // Position fire punch along the direction at mid-range with minimum distance to avoid overlap with player
            float spawnDistance = Mathf.Max(punchRange * 0.6f, 1.5f);
            Vector3 spawnPos = transform.position + (Vector3)dir * spawnDistance;
            GameObject firePunch = Instantiate(_firePunch, spawnPos, Quaternion.identity, null);
            
            // Rotate to face punch direction
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            firePunch.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            Debug.Log($"[SpawnFirePunches] Spawned at {spawnPos} with angle {angle}");
            
            // Destroy after a short time
            Destroy(firePunch, 0.5f);
        }
    }

    private IEnumerator CompletePunch()
    {
        yield return new WaitForSeconds(0.5f);
        EndPunch();
    }

    private void EndPunch()
    {
        isPunching = false;

        if (punchEffect != null)
        {
            punchEffect.Stop();
        }

        Debug.Log("Punch ended!");
    }

    public bool CanPunch()
    {
        return canPunch && !isPunching;
    }

    public float GetCooldownRemaining()
    {
        return canPunch ? 0f : cooldownTime - (Time.time - lastPunchTime);
    }

    /// <summary>
    /// Set number of punch directions
    /// </summary>
    public void SetPunchDirections(int directions)
    {
        punchDirections = Mathf.Max(1, directions);
        Debug.Log($"[PunchSkill] Punch directions set to: {punchDirections}");
    }

    /// <summary>
    /// Set punch range
    /// </summary>
    public void SetPunchRange(float range)
    {
        punchRange = Mathf.Max(0.1f, range);
        Debug.Log($"[PunchSkill] Punch range set to: {punchRange}");
    }

    /// <summary>
    /// Adjust the punch range by a percentage of its current value.
    /// For example, passing 200 will double the range, while 75 will reduce it to 75%.
    /// Percent is clamped between 10 and 500 (10% to 5x).
    /// </summary>
    /// <param name="percent">Percentage of the original range to keep (10-500).</param>
    public void SetPunchRangeByPercentage(float percent)
    {
        percent = Mathf.Clamp(percent, 10f, 500f);
        punchRange *= percent / 100f;
        Debug.Log($"[PunchSkill] Punch range adjusted by {percent}% of current value. New range: {punchRange}");
    }

    /// <summary>
    /// Set punch damage
    /// </summary>
    public void SetPunchDamage(float damage)
    {
        punchDamage = Mathf.Max(0, damage);
        Debug.Log($"[PunchSkill] Punch damage set to: {punchDamage}");
    }

    /// <summary>
    /// Adjust the punch damage by a percentage of its current value.
    /// For example, passing 150 will increase damage by 50%, while 50 will halve it.
    /// Percent is clamped between 0 and 1000 (0 removes damage, 1000 is 10x).
    /// </summary>
    /// <param name="percent">Percentage of the original damage to keep (0-1000).</param>
    public void SetPunchDamageByPercentage(float percent)
    {
        percent = Mathf.Clamp(percent, 0f, 1000f);
        punchDamage *= percent / 100f;
        Debug.Log($"[PunchSkill] Punch damage adjusted by {percent}% of current value. New damage: {punchDamage}");
    }

    /// <summary>
    /// Decrease cooldown time
    /// </summary>
    public void DecreaseCooldown(float amount)
    {
        cooldownTime = Mathf.Max(0.1f, cooldownTime - amount);
        Debug.Log($"[PunchSkill] Cooldown decreased by {amount}. New cooldown: {cooldownTime}");
    }

    /// <summary>
    /// Set cooldown time directly
    /// </summary>
    public void SetCooldown(float time)
    {
        cooldownTime = Mathf.Max(0.1f, time);
        Debug.Log($"[PunchSkill] Cooldown set to: {cooldownTime}");
    }

    /// <summary>
    /// Adjust the cooldown by a percentage of its current value.
    /// For example, passing 50 will cut the cooldown in half, while 200 will double it.
    /// Percent is clamped between 0 and 100 (0 freezes the skill, 100 leaves it unchanged).
    /// </summary>
    /// <param name="percent">Percentage of the original cooldown to keep (0-100).</param>
    public void SetCooldownByPercentage(float percent)
    {
        percent = Mathf.Clamp(percent, 0f, 100f);
        cooldownTime *= percent / 100f;
        Debug.Log($"[PunchSkill] Cooldown adjusted by {percent}% of current value. New cooldown: {cooldownTime}");
    }

    /// <summary>
    /// Set fire punch visual size
    /// </summary>
    public void SetFirePunchSize(float size)
    {
        if (_firePunch != null)
        {
            _firePunch.transform.localScale = Vector3.one * Mathf.Max(0.1f, size);
            Debug.Log($"[PunchSkill] Fire punch size set to: {size}");
        }
        else
        {
            Debug.LogWarning("[PunchSkill] Fire punch prefab not assigned!");
        }
    }

    /// <summary>
    /// Adjust the fire punch size by a percentage of its current value.
    /// For example, passing 150 will increase size by 50%, while 50 will halve it.
    /// Percent is clamped between 10 and 500 (10% to 5x).
    /// </summary>
    /// <param name="percent">Percentage of the original size to keep (10-500).</param>
    public void SetFirePunchSizeByPercentage(float percent)
    {
        if (_firePunch != null)
        {
            percent = Mathf.Clamp(percent, 10f, 500f);
            _firePunch.transform.localScale *= percent / 100f;
            Debug.Log($"[PunchSkill] Fire punch size adjusted by {percent}% of current value. New size: {_firePunch.transform.localScale.x}");
        }
        else
        {
            Debug.LogWarning("[PunchSkill] Fire punch prefab not assigned!");
        }
    }

    private IEnumerator ApplyKnockback(Rigidbody2D rb, Vector2 direction)
    {
        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            rb.linearVelocity = direction * knockbackForce;
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;
        Vector3 forward = transform.right;

        Gizmos.color = Color.red;
        float halfAngle = punchAngle / 2f * Mathf.Deg2Rad;

        Vector3 leftDir = Quaternion.Euler(0, 0, -punchAngle / 2f) * forward;
        Vector3 rightDir = Quaternion.Euler(0, 0, punchAngle / 2f) * forward;

        Gizmos.DrawLine(pos, pos + leftDir * punchRange);
        Gizmos.DrawLine(pos, pos + rightDir * punchRange);

        int segments = 20;
        Vector3 prevPoint = pos + leftDir * punchRange;
        for (int i = 1; i <= segments; i++)
        {
            float angle = -halfAngle + (halfAngle * 2f * i / segments);
            Vector3 dir = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg) * forward;
            Vector3 point = pos + dir * punchRange;
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
}
