using UnityEngine;

[CreateAssetMenu(fileName = "StatGainOnLevelUpEffect", menuName = "Game/Items/Effects/Stat Gain On Level Up")]
public class StatGainOnLevelUpEffect : ItemEffectBase
{
    [Header("Level Up Bonus")]
    [SerializeField] private PlayerStatType statType = PlayerStatType.HPRegen;
    [SerializeField] private float amountPerLevel = 1f;
    [SerializeField] private StatModifierValueType valueType = StatModifierValueType.Flat;
    [SerializeField] private bool multiplyByStack = true;

    public override string GetDescription(UnchaintedItemData itemData, int stackCount)
    {
        float amount = GetEffectiveAmount(stackCount);
        string statName = TriggeredStatGainEffectUtility.GetStatDisplayName(statType);
        string formattedAmount = TriggeredStatGainEffectUtility.FormatAmount(amount, valueType);
        return $"Gain {formattedAmount} {statName} when you level up";
    }

    public override ItemEffectHandle CreateHandle(EffectContext context, UnchaintedItemData itemData, int stackCount)
    {
        return new StatGainOnLevelUpEffectHandle(this, context, itemData, stackCount);
    }

    private float GetEffectiveAmount(int stackCount)
    {
        int effectiveStacks = Mathf.Max(1, stackCount);
        float value = multiplyByStack ? amountPerLevel * effectiveStacks : amountPerLevel;
        return Mathf.Max(0f, value);
    }

    private sealed class StatGainOnLevelUpEffectHandle : ItemEffectHandle
    {
        private readonly StatGainOnLevelUpEffect effect;
        private PlayerStats playerStats;
        private float totalGrantedAmount;

        public StatGainOnLevelUpEffectHandle(StatGainOnLevelUpEffect effect, EffectContext context, UnchaintedItemData itemData, int initialStackCount)
            : base(effect, context, itemData, initialStackCount)
        {
            this.effect = effect;
        }

        protected override void OnApplied()
        {
            playerStats = Context != null ? Context.PlayerStats : null;
            if (playerStats == null)
            {
                return;
            }

            playerStats.OnLevelUp += HandleLevelUp;
        }

        protected override void OnStackChanged(int previousStackCount, int newStackCount)
        {
        }

        protected override void OnRemoved()
        {
            if (playerStats == null)
            {
                return;
            }

            playerStats.OnLevelUp -= HandleLevelUp;

            if (totalGrantedAmount > 0f)
            {
                playerStats.DecreaseStat(effect.statType, totalGrantedAmount);
                totalGrantedAmount = 0f;
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            if (playerStats == null)
            {
                return;
            }

            float amount = effect.GetEffectiveAmount(StackCount);
            if (amount <= 0f)
            {
                return;
            }

            float appliedAmount = playerStats.IncreaseStat(effect.statType, amount, effect.valueType);
            totalGrantedAmount += Mathf.Max(0f, appliedAmount);
        }
    }
}