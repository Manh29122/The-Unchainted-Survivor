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

    [Header("Damage Cooldown")]
    [SerializeField] private bool enableDamageInvulnerability = true;
    [SerializeField] private float damageInvulnerabilityDuration = 3f;

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
    [Range(0f, 100f)] public float maxDodgeChance = 60f;
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
    private float nextDamageAllowedTime = float.NegativeInfinity;

    // ── Events ───────────────────────────────
    public event Action<int, int> OnExpChanged;     // (current, toNext)
    public event Action<int> OnLevelUp;        // (newLevel)
    public event Action<int> OnGoldChanged;    // (total)
    public event Action<int, int> OnHPChanged;      // (current, max)
    public event Action OnStatsChanged;
    public event Action<int> OnDamageDodged;
    public event Action<int> OnDamageTaken;

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
        dodge = Mathf.Clamp(dodge, 0f, maxDodgeChance);
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
        int validDamage = Mathf.Max(0, damage);
        if (validDamage <= 0)
        {
            return;
        }

        if (!CanTakeDamage())
        {
            return;
        }

        if (TryDodge(validDamage))
        {
            return;
        }

        int mitigatedDamage = GetDamageAfterArmor(validDamage);
        currentHP = Mathf.Max(0, currentHP - mitigatedDamage);
        nextDamageAllowedTime = Time.time + Mathf.Max(0f, damageInvulnerabilityDuration);
        OnDamageTaken?.Invoke(mitigatedDamage);
        OnHPChanged?.Invoke(currentHP, maxHP);
        NotifyStatsChanged();

        if (currentHP <= 0)
            OnDead();
    }

    public int GetDamageAfterArmor(int incomingDamage)
    {
        int validDamage = Mathf.Max(0, incomingDamage);
        if (validDamage <= 0)
        {
            return 0;
        }

        int mitigatedDamage = Mathf.RoundToInt(validDamage - armor);
        return Mathf.Max(1, mitigatedDamage);
    }

    public bool CanTakeDamage()
    {
        return !enableDamageInvulnerability || Time.time >= nextDamageAllowedTime;
    }

    public float GetRemainingDamageInvulnerabilityTime()
    {
        if (!enableDamageInvulnerability)
        {
            return 0f;
        }

        return Mathf.Max(0f, nextDamageAllowedTime - Time.time);
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

    public bool TryDodge(int incomingDamage = 0)
    {
        float clampedDodge = Mathf.Clamp(dodge, 0f, maxDodgeChance);
        if (clampedDodge <= 0f)
        {
            return false;
        }

        float randomRoll = UnityEngine.Random.Range(0f, 100f);
        bool dodged = randomRoll < clampedDodge;

        if (dodged)
        {
            Debug.Log($"[Player] 🌀 Dodged hit! Chance: {clampedDodge:0.##}% | Incoming damage: {incomingDamage}");
            OnDamageDodged?.Invoke(incomingDamage);
        }

        return dodged;
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

    public float ModifyStat(PlayerStatType statType, float amount, StatModifierValueType valueType = StatModifierValueType.Flat)
    {
        float resolvedAmount = ResolveStatChangeAmount(statType, amount, valueType);

        switch (statType)
        {
            case PlayerStatType.MaxHP:
                SetMaxHP(maxHP + Mathf.RoundToInt(resolvedAmount), false);
                return resolvedAmount;
            case PlayerStatType.HPRegen:
                hpRegen += resolvedAmount;
                break;
            case PlayerStatType.Armor:
                armor += resolvedAmount;
                break;
            case PlayerStatType.Dodge:
                dodge = Mathf.Clamp(dodge + resolvedAmount, 0f, maxDodgeChance);
                break;
            case PlayerStatType.MoveSpeed:
                moveSpeedBonus += resolvedAmount;
                break;
            case PlayerStatType.DamagePercent:
                damagePercent += resolvedAmount;
                break;
            case PlayerStatType.MeleeDamage:
                meleeDamage += resolvedAmount;
                break;
            case PlayerStatType.RangedDamage:
                rangedDamage += resolvedAmount;
                break;
            case PlayerStatType.ElementalDamage:
                elementalDamage += resolvedAmount;
                break;
            case PlayerStatType.AttackSpeed:
                attackSpeed = Mathf.Max(0.1f, attackSpeed + resolvedAmount);
                break;
            case PlayerStatType.CritChance:
                critChance = Mathf.Clamp(critChance + resolvedAmount, 0f, 100f);
                break;
            case PlayerStatType.CritDamage:
                critDamage = Mathf.Max(1f, critDamage + resolvedAmount);
                break;
            case PlayerStatType.Engineering:
                engineering += resolvedAmount;
                break;
            case PlayerStatType.ExplosionDamage:
                explosionDamage += resolvedAmount;
                break;
            case PlayerStatType.ExplosionSize:
                explosionSize = Mathf.Max(0.1f, explosionSize + resolvedAmount);
                break;
            case PlayerStatType.LifeSteal:
                lifeSteal = Mathf.Max(0f, lifeSteal + resolvedAmount);
                break;
            case PlayerStatType.Knockback:
                knockback += resolvedAmount;
                break;
            case PlayerStatType.Range:
                attackRange += resolvedAmount;
                break;
            case PlayerStatType.ProjectileSpeed:
                projectileSpeed = Mathf.Max(0.1f, projectileSpeed + resolvedAmount);
                break;
            case PlayerStatType.Luck:
                luck += resolvedAmount;
                break;
            case PlayerStatType.Harvesting:
                harvesting += resolvedAmount;
                break;
            case PlayerStatType.Magnet:
                magnetMultiplier = Mathf.Max(0f, magnetMultiplier + resolvedAmount);
                break;
            case PlayerStatType.PickupRadius:
                basePickupRadius = Mathf.Max(0f, basePickupRadius + resolvedAmount);
                break;
            case PlayerStatType.GoldMultiplier:
                goldMultiplier = Mathf.Max(0f, goldMultiplier + resolvedAmount);
                break;
            case PlayerStatType.ExpMultiplier:
                expMultiplier = Mathf.Max(0f, expMultiplier + resolvedAmount);
                break;
        }

        NotifyStatsChanged();
        return resolvedAmount;
    }

    public float IncreaseStat(PlayerStatType statType, float amount, StatModifierValueType valueType = StatModifierValueType.Flat)
    {
        return ModifyStat(statType, Mathf.Abs(amount), valueType);
    }

    public float DecreaseStat(PlayerStatType statType, float amount, StatModifierValueType valueType = StatModifierValueType.Flat)
    {
        return ModifyStat(statType, -Mathf.Abs(amount), valueType);
    }

    public void SetStatValue(PlayerStatType statType, float value)
    {
        float currentValue = GetStatValue(statType);
        ModifyStat(statType, value - currentValue);
    }

    private float ResolveStatChangeAmount(PlayerStatType statType, float amount, StatModifierValueType valueType)
    {
        if (valueType != StatModifierValueType.Percent)
        {
            return amount;
        }

        float baseValue = GetStatValue(statType);
        float percentAmount = baseValue * (amount / 100f);
        return RoundPercentDelta(percentAmount);
    }

    private static float RoundPercentDelta(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value)) ? value : Mathf.Round(value);
    }

    public float IncreaseArmor(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.Armor, amount, valueType);
    public float DecreaseArmor(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.Armor, amount, valueType);

    public float IncreaseDodge(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.Dodge, amount, valueType);
    public float DecreaseDodge(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.Dodge, amount, valueType);

    public float IncreaseMoveSpeed(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.MoveSpeed, amount, valueType);
    public float DecreaseMoveSpeed(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.MoveSpeed, amount, valueType);

    public float IncreaseDamagePercent(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.DamagePercent, amount, valueType);
    public float DecreaseDamagePercent(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.DamagePercent, amount, valueType);

    public float IncreaseMeleeDamage(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.MeleeDamage, amount, valueType);
    public float DecreaseMeleeDamage(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.MeleeDamage, amount, valueType);

    public float IncreaseRangedDamage(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.RangedDamage, amount, valueType);
    public float DecreaseRangedDamage(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.RangedDamage, amount, valueType);

    public float IncreaseElementalDamage(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.ElementalDamage, amount, valueType);
    public float DecreaseElementalDamage(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.ElementalDamage, amount, valueType);

    public float IncreaseAttackSpeed(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.AttackSpeed, amount, valueType);
    public float DecreaseAttackSpeed(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.AttackSpeed, amount, valueType);

    public float IncreaseCritChance(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.CritChance, amount, valueType);
    public float DecreaseCritChance(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.CritChance, amount, valueType);

    public float IncreaseCritDamage(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.CritDamage, amount, valueType);
    public float DecreaseCritDamage(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.CritDamage, amount, valueType);

    public float IncreaseLuck(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.Luck, amount, valueType);
    public float DecreaseLuck(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.Luck, amount, valueType);

    public float IncreaseHarvesting(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.Harvesting, amount, valueType);
    public float DecreaseHarvesting(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.Harvesting, amount, valueType);

    public float IncreaseRange(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.Range, amount, valueType);
    public float DecreaseRange(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.Range, amount, valueType);

    public float IncreaseProjectileSpeed(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.ProjectileSpeed, amount, valueType);
    public float DecreaseProjectileSpeed(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.ProjectileSpeed, amount, valueType);

    public float IncreaseLifeSteal(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.LifeSteal, amount, valueType);
    public float DecreaseLifeSteal(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.LifeSteal, amount, valueType);

    public float IncreaseKnockback(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.Knockback, amount, valueType);
    public float DecreaseKnockback(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.Knockback, amount, valueType);

    public float IncreaseEngineering(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.Engineering, amount, valueType);
    public float DecreaseEngineering(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.Engineering, amount, valueType);

    public float IncreaseExplosionDamage(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.ExplosionDamage, amount, valueType);
    public float DecreaseExplosionDamage(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.ExplosionDamage, amount, valueType);

    public float IncreaseExplosionSize(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.ExplosionSize, amount, valueType);
    public float DecreaseExplosionSize(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.ExplosionSize, amount, valueType);

    public float IncreaseHPRegen(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.HPRegen, amount, valueType);
    public float DecreaseHPRegen(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.HPRegen, amount, valueType);

    public float IncreaseMagnet(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.Magnet, amount, valueType);
    public float DecreaseMagnet(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.Magnet, amount, valueType);

    public float IncreasePickupRadius(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.PickupRadius, amount, valueType);
    public float DecreasePickupRadius(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.PickupRadius, amount, valueType);

    public float IncreaseGoldMultiplier(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.GoldMultiplier, amount, valueType);
    public float DecreaseGoldMultiplier(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.GoldMultiplier, amount, valueType);

    public float IncreaseExpMultiplier(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => IncreaseStat(PlayerStatType.ExpMultiplier, amount, valueType);
    public float DecreaseExpMultiplier(float amount, StatModifierValueType valueType = StatModifierValueType.Flat) => DecreaseStat(PlayerStatType.ExpMultiplier, amount, valueType);
}