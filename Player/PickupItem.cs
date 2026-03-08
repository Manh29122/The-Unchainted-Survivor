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

    private bool isBeingMagnetized = false;
    private float despawnTimer;
    private Coroutine blinkCoroutine;

    // ─────────────────────────────────────────
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // ─────────────────────────────────────────
    //  POOL LIFECYCLE
    // ─────────────────────────────────────────
    protected override void OnActivated()
    {
        isBeingMagnetized = false;
        despawnTimer = data != null ? data.despawnTime : 15f;
        sr.color = Color.white;

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
        StartCoroutine(PopIn());
    }

    protected override void OnDeactivated()
    {
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);
        sr.color = Color.white;
        isBeingMagnetized = false;
    }

    // ─────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────
    void Update()
    {
        if (playerTransform == null || data == null) return;

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
            float speed = data.magnetSpeed * (1f + (data.magnetRadius - dist) / data.magnetRadius);
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
        if (playerStats == null || data == null)
        {
            ReturnToPool();
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
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f,
                EaseOutBack(t));
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    IEnumerator CollectEffect()
    {
        // Flash màu rồi scale về 0
        sr.color = data.glowColor;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 12f;
            transform.localScale = Vector3.one * (1f - t);
            yield return null;
        }
        ReturnToPool();
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