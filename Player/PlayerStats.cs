using System;
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
        currentExp += amount;
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
        gold += amount;
        Debug.Log($"[Player] 💰 +{amount} Gold (Total: {gold})");
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
        // Mở rộng tuỳ ý: tăng speed, damage, magnet...
        // Ví dụ: tăng bán kính hút
        // magnetMultiplier += item.value * 0.1f;
    }
}