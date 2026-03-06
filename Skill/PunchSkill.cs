using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PunchSkill : MonoBehaviour
{
    [Header("Punch Settings")]
    [SerializeField] private float punchDamage = 100f;
    [SerializeField] private float punchRange = 3f;
    [SerializeField] private float punchAngle = 45f;

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
        PerformPunch();
        StartCoroutine(CompletePunch());

        Debug.Log("Punch! Damage: " + punchDamage);
    }

    private void PerformPunch()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, punchRange);
        bool hitEnemy = false;
        int enemiesHit = 0;

        // calculate forward vector from joystick or default facing
        Vector2 forwardVec2 = transform.right;
        if (joystick != null && joystick.IsTouching && joystick.Input != Vector2.zero)
        {
            forwardVec2 = joystick.Input.normalized;
        }

        foreach (Collider2D collider in hitColliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                Vector2 dirToEnemy = (collider.transform.position - transform.position).normalized;
                float angleToEnemy = Vector2.Angle(forwardVec2, dirToEnemy);

                if (angleToEnemy <= punchAngle / 2f)
                {
                    EnemyHealth enemyHealth = collider.GetComponent<EnemyHealth>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(punchDamage);

                        // show damage number
                        if (floatingTextPrefab != null)
                        {
                            GameObject popup = Instantiate(floatingTextPrefab, collider.transform.position, Quaternion.identity);
                            FloatingText ft = popup.GetComponent<FloatingText>();
                            if (ft != null)
                                ft.SetText(punchDamage.ToString(), Color.red);
                        }

                        // apply knockback if enemy has rigidbody
                        Rigidbody2D enemyRb = collider.GetComponent<Rigidbody2D>();
                        if (enemyRb != null)
                        {
                            Vector2 knockDir = (collider.transform.position - transform.position).normalized;
                            enemyRb.linearVelocity = Vector2.zero; // reset existing motion
                            // start short knockback coroutine
                            StartCoroutine(ApplyKnockback(enemyRb, knockDir));
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
    /// Applies a short-duration velocity to the enemy then stops it.
    /// </summary>
    private IEnumerator ApplyKnockback(Rigidbody2D rb, Vector2 direction)
    {
        float elapsed = 0f;
        // push for knockbackDuration seconds
        while (elapsed < knockbackDuration)
        {
            rb.linearVelocity = direction * knockbackForce;
            elapsed += Time.deltaTime;
            yield return null;
        }
        // stop movement
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