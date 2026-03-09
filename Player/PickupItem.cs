using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEngine.RuleTile.TilingRuleOutput;

/// <summary>
/// Gắn vào Item Prefab (dùng với ObjectPool).
/// Xử lý: hút về player → thu khi chạm → áp dụng hiệu ứng.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PickupItem : PooledObject
{
    // ─────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────
    [Header("Item Config")]
    public ItemData data;

    // ─────────────────────────────────────────
    //  RUNTIME
    // ─────────────────────────────────────────
    private UnityEngine.Transform playerTransform;
    private PlayerStats playerStats;      // Nhận exp, gold, hp...
    private SpriteRenderer sr;
    private Vector3 originalScale;

    private bool isBeingMagnetized = false;
    private bool isCollected = false;
    private float despawnTimer;
    private Coroutine blinkCoroutine;
    private Coroutine popInCoroutine;

    [Header("Runtime Visual")]
    [SerializeField] private float maxMagnetSpeed = 6f;
    [SerializeField] private float collectEffectDuration = 0.12f;

    // ─────────────────────────────────────────
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    // ─────────────────────────────────────────
    //  POOL LIFECYCLE
    // ─────────────────────────────────────────
    protected override void OnActivated()
    {
        isBeingMagnetized = false;
        isCollected = false;
        despawnTimer = data != null ? data.despawnTime : 15f;
        sr.color = Color.white;
        sr.enabled = true;
        transform.localScale = originalScale;

        // Cache player reference
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerStats = player.GetComponent<PlayerStats>();
            }
        }

        // Hiệu ứng pop-in nhỏ
        if (popInCoroutine != null)
        {
            StopCoroutine(popInCoroutine);
        }

        popInCoroutine = StartCoroutine(PopIn());
    }

    protected override void OnDeactivated()
    {
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);
        if (popInCoroutine != null)
            StopCoroutine(popInCoroutine);
        sr.color = Color.white;
        sr.enabled = true;
        transform.localScale = originalScale;
        isBeingMagnetized = false;
        isCollected = false;
    }

    // ─────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────
    void Update()
    {
        if (isCollected || playerTransform == null || data == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        float effectivePickupRadius = data.pickupRadius;

        if (playerStats != null)
        {
            effectivePickupRadius = Mathf.Max(data.pickupRadius, playerStats.GetPickupRadius());
        }

        // 1. Kiểm tra thu thập (pickup radius)
        if (dist <= effectivePickupRadius)
        {
            Collect();
            return;
        }

        // 2. Kiểm tra vào vùng magnet
        if (dist <= data.magnetRadius)
        {
            isBeingMagnetized = true;
        }

        // 3. Hút về player nếu đang bị magnetize
        if (isBeingMagnetized)
        {
            float magnetFactor = data.magnetRadius > 0f
                ? Mathf.Clamp01((data.magnetRadius - dist) / data.magnetRadius)
                : 0f;
            float speed = Mathf.Lerp(data.magnetSpeed, maxMagnetSpeed, magnetFactor);
            transform.position = Vector2.MoveTowards(
                transform.position,
                playerTransform.position,
                speed * Time.deltaTime
            );
        }

        // 4. Despawn timer → nhấp nháy rồi biến mất
        despawnTimer -= Time.deltaTime;
        if (despawnTimer <= 3f && blinkCoroutine == null)
            blinkCoroutine = StartCoroutine(BlinkBeforeDespawn());

        if (despawnTimer <= 0f)
            ReturnToPool();
    }

    // ─────────────────────────────────────────
    //  COLLECT
    // ─────────────────────────────────────────
    void Collect()
    {
        if (isCollected)
        {
            return;
        }

        isCollected = true;
        isBeingMagnetized = false;

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        if (popInCoroutine != null)
        {
            StopCoroutine(popInCoroutine);
            popInCoroutine = null;
        }

        if (playerStats == null || data == null)
        {
            DespawnItem();
            return;
        }

        // Áp dụng hiệu ứng theo loại
        switch (data.type)
        {
            case ItemType.ExpGem:
                playerStats.AddExp(data.value);
                break;

            case ItemType.Gold:
                playerStats.AddGold(data.value);
                break;

            case ItemType.HP:
                playerStats.Heal(data.value);
                break;

            case ItemType.PowerUp:
                playerStats.ApplyPowerUp(data);
                break;
        }

        // Hiệu ứng thu thập
        StartCoroutine(CollectEffect());
    }

    // ─────────────────────────────────────────
    //  EFFECTS
    // ─────────────────────────────────────────

    IEnumerator PopIn()
    {
        transform.localScale = Vector3.zero;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 8f;
            // Overshoot nhẹ cho cảm giác bouncy
            transform.localScale = originalScale * Mathf.LerpUnclamped(0f, 1f,
                EaseOutBack(t));
            yield return null;
        }
        transform.localScale = originalScale;
        popInCoroutine = null;
    }

    IEnumerator CollectEffect()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = playerTransform != null ? playerTransform.position : transform.position;
        Vector3 boostedScale = originalScale * 1.25f;

        sr.color = data.glowColor;
        float t = 0f;
        while (t < collectEffectDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / Mathf.Max(0.01f, collectEffectDuration));
            transform.position = Vector3.Lerp(startPos, targetPos, normalized);
            transform.localScale = Vector3.Lerp(boostedScale, Vector3.zero, normalized);
            Color color = data.glowColor;
            color.a = 1f - normalized;
            sr.color = color;
            yield return null;
        }

        DespawnItem();
    }

    IEnumerator BlinkBeforeDespawn()
    {
        while (despawnTimer > 0f)
        {
            sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.2f);
        }
        sr.enabled = true;
        blinkCoroutine = null;
    }

    // ─────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────

    // Easing function: overshoot nhẹ
    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    void DespawnItem()
    {
        if (OwnerPool != null)
        {
            ReturnToPool();
            return;
        }

        Destroy(gameObject);
    }

    // ─────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (data == null) return;

        // Magnet radius - vàng
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, data.magnetRadius);

        // Pickup radius - xanh
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, data.pickupRadius);
    }
}