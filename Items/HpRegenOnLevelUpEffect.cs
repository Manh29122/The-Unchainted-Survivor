using UnityEngine;

[CreateAssetMenu(fileName = "HpRegenOnLevelUpEffect", menuName = "Game/Items/Effects/HP Regen On Level Up")]
public class HpRegenOnLevelUpEffect : ItemEffectBase
{
    [Header("Level Up Bonus")]
    [SerializeField] private float hpRegenPerLevel = 1f;
    [SerializeField] private bool multiplyByStack = true;

    public override string GetDescription(UnchaintedItemData itemData, int stackCount)
    {
        float amount = GetEffectiveHpRegen(stackCount);
        return $"Gain {amount:0.##} HP Regeneration when you level up";
    }

    public override ItemEffectHandle CreateHandle(EffectContext context, UnchaintedItemData itemData, int stackCount)
    {
        return new HpRegenOnLevelUpEffectHandle(this, context, itemData, stackCount);
    }

    private float GetEffectiveHpRegen(int stackCount)
    {
        int effectiveStacks = Mathf.Max(1, stackCount);
        return Mathf.Max(0f, multiplyByStack ? hpRegenPerLevel * effectiveStacks : hpRegenPerLevel);
    }

    private sealed class HpRegenOnLevelUpEffectHandle : ItemEffectHandle
    {
        private readonly HpRegenOnLevelUpEffect effect;
        private PlayerStats playerStats;
        private float grantedHpRegen;

        public HpRegenOnLevelUpEffectHandle(HpRegenOnLevelUpEffect effect, EffectContext context, UnchaintedItemData itemData, int initialStackCount)
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
            if (playerStats != null)
            {
                playerStats.OnLevelUp -= HandleLevelUp;

                if (grantedHpRegen > 0f)
                {
                    playerStats.DecreaseHPRegen(grantedHpRegen);
                    grantedHpRegen = 0f;
                }
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            if (playerStats == null)
            {
                return;
            }

            float amount = effect.GetEffectiveHpRegen(StackCount);
            if (amount <= 0f)
            {
                return;
            }

            playerStats.IncreaseHPRegen(amount);
            grantedHpRegen += amount;
        }
    }
}