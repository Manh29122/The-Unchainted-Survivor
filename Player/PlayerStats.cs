using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerStatType
{
    MaxHP,
    HPRegen,
    Armor,
    Dodge,
    MoveSpeed,
    DamagePercent,
    MeleeDamage,
    RangedDamage,
    ElementalDamage,
    AttackSpeed,
    CritChance,
    CritDamage,
    Engineering,
    ExplosionDamage,
    ExplosionSize,
    LifeSteal,
    Knockback,
    Range,
    ProjectileSpeed,
    Luck,
    Harvesting,
    Magnet,
    PickupRadius,
    GoldMultiplier,
    ExpMultiplier
}

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
    [SerializeField] private PlayerLevelCurveCalculator levelCalculator;
    [SerializeField] private int totalExp = 0;

    [Header("Gold")]
    public int gold = 0;

    [Header("HP")]
    public int maxHP = 100;
    public int currentHP = 100;

    [Header("Combat Stats")]
    public float damagePercent = 0f;
    public float meleeDamage = 0f;
    public float rangedDamage = 0f;
    public float elementalDamage = 0f;
    public float attackSpeed = 1f;
    public float critChance = 0f;
    public float critDamage = 1.5f;
    public float engineering = 0f;
    public float explosionDamage = 0f;
    public float explosionSize = 1f;
    public float lifeSteal = 0f;
    public float knockback = 0f;
    public float attackRange = 0f;
    public float projectileSpeed = 1f;

    [Header("Defense / Utility Stats")]
    public float hpRegen = 0f;
    public float armor = 0f;
    public float dodge = 0f;
    public float moveSpeedBonus = 0f;
    public float luck = 0f;
    public float harvesting = 0f;

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
    public event Action OnStatsChanged;

    private void Awake()
    {
        if (levelCalculator == null)
        {
            levelCalculator = GetComponent<PlayerLevelCurveCalculator>();
        }

        if (levelCalculator != null)
        {
            RebuildTotalExpFromCurrentState();
            SyncLevelStateFromTotalExp(false);
        }

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        OnHPChanged?.Invoke(currentHP, maxHP);
        NotifyStatsChanged();
    }

    // ─────────────────────────────────────────
    //  EXP & LEVEL
    // ─────────────────────────────────────────
    public void AddExp(int amount)
    {
        int finalAmount = Mathf.Max(0, Mathf.RoundToInt(amount * expMultiplier));

        if (levelCalculator == null)
        {
            currentExp += finalAmount;
            OnExpChanged?.Invoke(currentExp, expToNextLevel);

            while (currentExp >= expToNextLevel)
            {
                currentExp -= expToNextLevel;
                LevelUp();
            }

            return;
        }

        int previousLevel = level;
        levelCalculator.AddExp(ref totalExp, finalAmount, out currentExp, out expToNextLevel);
        SyncLevelStateFromTotalExp(true);

        if (level > previousLevel)
        {
            for (int reachedLevel = previousLevel + 1; reachedLevel <= level; reachedLevel++)
            {
                Debug.Log($"[Player] ⭐ Level Up! → Lv.{reachedLevel} | Next: {expToNextLevel} exp");
                OnLevelUp?.Invoke(reachedLevel);
            }
        }

        OnExpChanged?.Invoke(currentExp, expToNextLevel);
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

    void NotifyStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }

    private void RebuildTotalExpFromCurrentState()
    {
        if (levelCalculator == null)
        {
            return;
        }

        totalExp = levelCalculator.GetTotalExpToReachLevel(level) + Mathf.Max(0, currentExp);
    }

    private void SyncLevelStateFromTotalExp(bool preserveCurrentLevelExp)
    {
        if (levelCalculator == null)
        {
            return;
        }

        level = levelCalculator.GetLevelFromTotalExp(totalExp);
        expToNextLevel = levelCalculator.GetExpToNextLevel(totalExp);

        if (preserveCurrentLevelExp)
        {
            currentExp = levelCalculator.GetCurrentLevelExp(totalExp);
        }
        else
        {
            currentExp = Mathf.Clamp(currentExp, 0, expToNextLevel);
        }
    }

    public int GetTotalExp()
    {
        return totalExp;
    }

    public float GetExpProgress01()
    {
        if (levelCalculator == null)
        {
            if (expToNextLevel <= 0)
            {
                return 1f;
            }

            return Mathf.Clamp01((float)currentExp / expToNextLevel);
        }

        return levelCalculator.GetLevelProgress01(totalExp);
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
        NotifyStatsChanged();
    }

    public bool CanSpendGold(int amount)
    {
        return gold >= Mathf.Max(0, amount);
    }

    public bool SpendGold(int amount)
    {
        int validAmount = Mathf.Max(0, amount);

        if (validAmount <= 0)
        {
            return true;
        }

        if (gold < validAmount)
        {
            Debug.Log($"[Player] ❌ Not enough gold. Need {validAmount}, have {gold}");
            return false;
        }

        gold -= validAmount;
        Debug.Log($"[Player] 🪙 -{validAmount} Gold (Total: {gold})");
        OnGoldChanged?.Invoke(gold);
        NotifyStatsChanged();
        return true;
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
            NotifyStatsChanged();
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
        OnHPChanged?.Invoke(currentHP, maxHP);
        NotifyStatsChanged();

        if (currentHP <= 0)
            OnDead();
    }

    void OnDead() => Debug.Log("[Player] 💀 Dead!");

    public void IncreaseMaxHP(int amount, bool healByIncrease = true)
    {
        if (amount <= 0)
        {
            return;
        }

        maxHP += amount;

        if (healByIncrease)
        {
            currentHP = Mathf.Min(currentHP + amount, maxHP);
        }
        else
        {
            currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        }

        Debug.Log($"[Player] Max HP increased by {amount}. HP: {currentHP}/{maxHP}");
        OnHPChanged?.Invoke(currentHP, maxHP);
        NotifyStatsChanged();
    }

    public void SetMaxHP(int value, bool keepCurrentHpRatio = false)
    {
        int newMaxHp = Mathf.Max(1, value);
        float hpRatio = maxHP > 0 ? (float)currentHP / maxHP : 1f;

        maxHP = newMaxHp;
        currentHP = keepCurrentHpRatio
            ? Mathf.Clamp(Mathf.RoundToInt(maxHP * hpRatio), 0, maxHP)
            : Mathf.Clamp(currentHP, 0, maxHP);

        OnHPChanged?.Invoke(currentHP, maxHP);
        NotifyStatsChanged();
    }

    public float GetHPProgress01()
    {
        if (maxHP <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)currentHP / maxHP);
    }

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
        NotifyStatsChanged();
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

            NotifyStatsChanged();

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
        NotifyStatsChanged();
    }

    public float GetStatValue(PlayerStatType statType)
    {
        switch (statType)
        {
            case PlayerStatType.MaxHP: return maxHP;
            case PlayerStatType.HPRegen: return hpRegen;
            case PlayerStatType.Armor: return armor;
            case PlayerStatType.Dodge: return dodge;
            case PlayerStatType.MoveSpeed: return moveSpeedBonus;
            case PlayerStatType.DamagePercent: return damagePercent;
            case PlayerStatType.MeleeDamage: return meleeDamage;
            case PlayerStatType.RangedDamage: return rangedDamage;
            case PlayerStatType.ElementalDamage: return elementalDamage;
            case PlayerStatType.AttackSpeed: return attackSpeed;
            case PlayerStatType.CritChance: return critChance;
            case PlayerStatType.CritDamage: return critDamage;
            case PlayerStatType.Engineering: return engineering;
            case PlayerStatType.ExplosionDamage: return explosionDamage;
            case PlayerStatType.ExplosionSize: return explosionSize;
            case PlayerStatType.LifeSteal: return lifeSteal;
            case PlayerStatType.Knockback: return knockback;
            case PlayerStatType.Range: return attackRange;
            case PlayerStatType.ProjectileSpeed: return projectileSpeed;
            case PlayerStatType.Luck: return luck;
            case PlayerStatType.Harvesting: return harvesting;
            case PlayerStatType.Magnet: return magnetMultiplier;
            case PlayerStatType.PickupRadius: return GetPickupRadius();
            case PlayerStatType.GoldMultiplier: return goldMultiplier;
            case PlayerStatType.ExpMultiplier: return expMultiplier;
            default: return 0f;
        }
    }

    public void ModifyStat(PlayerStatType statType, float amount)
    {
        switch (statType)
        {
            case PlayerStatType.MaxHP:
                SetMaxHP(maxHP + Mathf.RoundToInt(amount), false);
                return;
            case PlayerStatType.HPRegen:
                hpRegen += amount;
                break;
            case PlayerStatType.Armor:
                armor += amount;
                break;
            case PlayerStatType.Dodge:
                dodge = Mathf.Clamp(dodge + amount, 0f, 100f);
                break;
            case PlayerStatType.MoveSpeed:
                moveSpeedBonus += amount;
                break;
            case PlayerStatType.DamagePercent:
                damagePercent += amount;
                break;
            case PlayerStatType.MeleeDamage:
                meleeDamage += amount;
                break;
            case PlayerStatType.RangedDamage:
                rangedDamage += amount;
                break;
            case PlayerStatType.ElementalDamage:
                elementalDamage += amount;
                break;
            case PlayerStatType.AttackSpeed:
                attackSpeed = Mathf.Max(0.1f, attackSpeed + amount);
                break;
            case PlayerStatType.CritChance:
                critChance = Mathf.Clamp(critChance + amount, 0f, 100f);
                break;
            case PlayerStatType.CritDamage:
                critDamage = Mathf.Max(1f, critDamage + amount);
                break;
            case PlayerStatType.Engineering:
                engineering += amount;
                break;
            case PlayerStatType.ExplosionDamage:
                explosionDamage += amount;
                break;
            case PlayerStatType.ExplosionSize:
                explosionSize = Mathf.Max(0.1f, explosionSize + amount);
                break;
            case PlayerStatType.LifeSteal:
                lifeSteal = Mathf.Max(0f, lifeSteal + amount);
                break;
            case PlayerStatType.Knockback:
                knockback += amount;
                break;
            case PlayerStatType.Range:
                attackRange += amount;
                break;
            case PlayerStatType.ProjectileSpeed:
                projectileSpeed = Mathf.Max(0.1f, projectileSpeed + amount);
                break;
            case PlayerStatType.Luck:
                luck += amount;
                break;
            case PlayerStatType.Harvesting:
                harvesting += amount;
                break;
            case PlayerStatType.Magnet:
                magnetMultiplier = Mathf.Max(0f, magnetMultiplier + amount);
                break;
            case PlayerStatType.PickupRadius:
                basePickupRadius = Mathf.Max(0f, basePickupRadius + amount);
                break;
            case PlayerStatType.GoldMultiplier:
                goldMultiplier = Mathf.Max(0f, goldMultiplier + amount);
                break;
            case PlayerStatType.ExpMultiplier:
                expMultiplier = Mathf.Max(0f, expMultiplier + amount);
                break;
        }

        NotifyStatsChanged();
    }
}