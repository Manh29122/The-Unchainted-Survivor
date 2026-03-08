using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn vào Player. Quản lý Exp, Gold, HP và xử lý PowerUp.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Exp / Level")]
    public int level = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100;
    [Tooltip("Hệ số tăng exp mỗi level: expToNextLevel *= expScaling")]
    public float expScaling = 1.3f;

    [Header("Gold")]
    public int gold = 0;

    [Header("HP")]
    public int maxHP = 100;
    public int currentHP = 100;

    [Header("Magnet Upgrade")]
    [Tooltip("Nhân bán kính hút của tất cả item (upgrade magnet)")]
    public float magnetMultiplier = 1f;

    [Header("Pickup Settings")]
    [Tooltip("Bán kính nhặt đồ cơ bản của player")]
    public float basePickupRadius = 0.4f;
    [Tooltip("Nhân bán kính nhặt đồ hiện tại")]
    public float pickupRadiusMultiplier = 1f;

    [Header("Temporary Multipliers")]
    [Tooltip("Nhân vàng hiện tại")]
    public float goldMultiplier = 1f;
    [Tooltip("Nhân kinh nghiệm hiện tại")]
    public float expMultiplier = 1f;

    private readonly Dictionary<TemporaryBonusType, Coroutine> activeTemporaryBonuses = new Dictionary<TemporaryBonusType, Coroutine>();

    // ── Events ───────────────────────────────
    public event Action<int, int> OnExpChanged;     // (current, toNext)
    public event Action<int> OnLevelUp;        // (newLevel)
    public event Action<int> OnGoldChanged;    // (total)
    public event Action<int, int> OnHPChanged;      // (current, max)

    // ─────────────────────────────────────────
    //  EXP & LEVEL
    // ─────────────────────────────────────────
    public void AddExp(int amount)
    {
        int finalAmount = Mathf.Max(0, Mathf.RoundToInt(amount * expMultiplier));
        currentExp += finalAmount;
        OnExpChanged?.Invoke(currentExp, expToNextLevel);

        // Level up loop (phòng trường hợp nhận nhiều exp 1 lần)
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            LevelUp();
        }
    }
    void Update()
    {

    }
    void LevelUp()
    {
        level++;
        expToNextLevel = Mathf.RoundToInt(expToNextLevel * expScaling);

        Debug.Log($"[Player] ⭐ Level Up! → Lv.{level} | Next: {expToNextLevel} exp");
        OnLevelUp?.Invoke(level);

        // TODO: Mở skill/upgrade menu ở đây
    }

    // ─────────────────────────────────────────
    //  GOLD
    // ─────────────────────────────────────────
    public void AddGold(int amount)
    {
        int finalAmount = Mathf.Max(0, Mathf.RoundToInt(amount * goldMultiplier));
        gold += finalAmount;
        Debug.Log($"[Player] 💰 +{finalAmount} Gold (Total: {gold})");
        OnGoldChanged?.Invoke(gold);
    }

    // ─────────────────────────────────────────
    //  HP
    // ─────────────────────────────────────────
    public void Heal(int amount)
    {
        int prev = currentHP;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        if (currentHP != prev)
        {
            Debug.Log($"[Player] ❤️ Heal +{currentHP - prev} | HP: {currentHP}/{maxHP}");
            
            OnHPChanged?.Invoke(currentHP, maxHP);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
        OnHPChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0)
            OnDead();
    }

    void OnDead() => Debug.Log("[Player] 💀 Dead!");

    // ─────────────────────────────────────────
    //  POWER UP
    // ─────────────────────────────────────────
    public void ApplyPowerUp(ItemData item)
    {
        Debug.Log($"[Player] ✨ PowerUp: {item.itemName} (+{item.value})");

        if (item == null || item.temporaryBonusType == TemporaryBonusType.None)
        {
            return;
        }

        ApplyTemporaryBonus(item.temporaryBonusType, item.temporaryBonusPercent, item.temporaryBonusDuration);
    }

    public float GetPickupRadius()
    {
        return Mathf.Max(0f, basePickupRadius * pickupRadiusMultiplier);
    }

    public void SetPickupRadius(float radius)
    {
        basePickupRadius = Mathf.Max(0f, radius);
    }

    public void ApplyTemporaryBonus(TemporaryBonusType bonusType, float percent, float duration)
    {
        if (bonusType == TemporaryBonusType.None)
        {
            return;
        }

        if (activeTemporaryBonuses.TryGetValue(bonusType, out Coroutine runningBonus) && runningBonus != null)
        {
            StopCoroutine(runningBonus);
        }

        Coroutine bonusCoroutine = StartCoroutine(TemporaryBonusRoutine(bonusType, percent, duration));
        activeTemporaryBonuses[bonusType] = bonusCoroutine;
    }

    private IEnumerator TemporaryBonusRoutine(TemporaryBonusType bonusType, float percent, float duration)
    {
        float multiplier = Mathf.Max(0f, 1f + (percent / 100f));

        switch (bonusType)
        {
            case TemporaryBonusType.GoldMultiplier:
                goldMultiplier = multiplier;
                break;
            case TemporaryBonusType.ExpMultiplier:
                expMultiplier = multiplier;
                break;
            case TemporaryBonusType.PickupRadiusMultiplier:
                pickupRadiusMultiplier = multiplier;
                break;
        }

        Debug.Log($"[Player] Temporary bonus {bonusType} active for {duration}s with multiplier x{multiplier:0.##}");

        yield return new WaitForSeconds(Mathf.Max(0f, duration));

        switch (bonusType)
        {
            case TemporaryBonusType.GoldMultiplier:
                goldMultiplier = 1f;
                break;
            case TemporaryBonusType.ExpMultiplier:
                expMultiplier = 1f;
                break;
            case TemporaryBonusType.PickupRadiusMultiplier:
                pickupRadiusMultiplier = 1f;
                break;
        }

        activeTemporaryBonuses.Remove(bonusType);
        Debug.Log($"[Player] Temporary bonus {bonusType} ended.");
    }
}