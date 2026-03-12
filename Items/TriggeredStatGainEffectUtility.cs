using UnityEngine;

public static class TriggeredStatGainEffectUtility
{
    public static string GetStatDisplayName(PlayerStatType statType)
    {
        switch (statType)
        {
            case PlayerStatType.MaxHP: return "Max HP";
            case PlayerStatType.HPRegen: return "HP Regeneration";
            case PlayerStatType.Armor: return "Armor";
            case PlayerStatType.Dodge: return "Dodge";
            case PlayerStatType.MoveSpeed: return "Move Speed";
            case PlayerStatType.DamagePercent: return "Damage";
            case PlayerStatType.MeleeDamage: return "Melee Damage";
            case PlayerStatType.RangedDamage: return "Ranged Damage";
            case PlayerStatType.ElementalDamage: return "Elemental Damage";
            case PlayerStatType.AttackSpeed: return "Attack Speed";
            case PlayerStatType.CritChance: return "Crit Chance";
            case PlayerStatType.CritDamage: return "Crit Damage";
            case PlayerStatType.Engineering: return "Engineering";
            case PlayerStatType.ExplosionDamage: return "Explosion Damage";
            case PlayerStatType.ExplosionSize: return "Explosion Size";
            case PlayerStatType.LifeSteal: return "Life Steal";
            case PlayerStatType.Knockback: return "Knockback";
            case PlayerStatType.Range: return "Range";
            case PlayerStatType.ProjectileSpeed: return "Projectile Speed";
            case PlayerStatType.Luck: return "Luck";
            case PlayerStatType.Harvesting: return "Harvesting";
            case PlayerStatType.Magnet: return "Magnet";
            case PlayerStatType.PickupRadius: return "Pickup Radius";
            case PlayerStatType.GoldMultiplier: return "Gold Multiplier";
            case PlayerStatType.ExpMultiplier: return "Exp Multiplier";
            default: return statType.ToString();
        }
    }

    public static string FormatAmount(float amount, StatModifierValueType valueType)
    {
        string sign = amount >= 0f ? "+" : string.Empty;
        string suffix = valueType == StatModifierValueType.Percent ? "%" : string.Empty;
        return $"{sign}{amount:0.##}{suffix}";
    }
}