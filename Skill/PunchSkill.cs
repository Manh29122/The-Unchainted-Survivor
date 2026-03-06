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

    [Header("Cooldown")]
    [SerializeField] private float cooldownTime = 3f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem punchEffect;
    [SerializeField] private AudioClip punchSound;
    [SerializeField] private AudioClip hitSound;

    [Header("Input")]
    [SerializeField] private KeyCode punchKey = KeyCode.Q;

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

        foreach (Collider2D collider in hitColliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                Vector2 dirToEnemy = (collider.transform.position - transform.position).normalized;
                float angleToEnemy = Vector2.Angle(transform.right, dirToEnemy);

                if (angleToEnemy <= punchAngle / 2f)
                {
                    EnemyHealth enemyHealth = collider.GetComponent<EnemyHealth>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(punchDamage);
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