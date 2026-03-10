using UnityEngine;

[CreateAssetMenu(fileName = "HealOnDodgeEffect", menuName = "Game/Items/Effects/Heal On Dodge")]
public class HealOnDodgeEffect : ItemEffectBase
{
    [Header("Heal On Dodge")]
    [SerializeField, Range(0f, 100f)] private float healChancePercent = 50f;
    [SerializeField] private int healAmount = 5;
    [SerializeField] private bool multiplyChanceByStack;
    [SerializeField] private bool multiplyHealByStack;

    public override string GetDescription(UnchaintedItemData itemData, int stackCount)
    {
        float chance = GetEffectiveHealChance(stackCount);
        int amount = GetEffectiveHealAmount(stackCount);
        return $"{chance:0.##}% chance to heal {amount} HP when dodging an attack";
    }

    public override ItemEffectHandle CreateHandle(EffectContext context, UnchaintedItemData itemData, int stackCount)
    {
        return new HealOnDodgeEffectHandle(this, context, itemData, stackCount);
    }

    private float GetEffectiveHealChance(int stackCount)
    {
        int effectiveStacks = Mathf.Max(1, stackCount);
        float chance = multiplyChanceByStack ? healChancePercent * effectiveStacks : healChancePercent;
        return Mathf.Clamp(chance, 0f, 100f);
    }

    private int GetEffectiveHealAmount(int stackCount)
    {
        int effectiveStacks = Mathf.Max(1, stackCount);
        int amount = multiplyHealByStack ? healAmount * effectiveStacks : healAmount;
        return Mathf.Max(0, amount);
    }

    private sealed class HealOnDodgeEffectHandle : ItemEffectHandle
    {
        private readonly HealOnDodgeEffect effect;
        private PlayerStats playerStats;

        public HealOnDodgeEffectHandle(HealOnDodgeEffect effect, EffectContext context, UnchaintedItemData itemData, int initialStackCount)
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

            playerStats.OnDamageDodged += HandleDamageDodged;
        }

        protected override void OnStackChanged(int previousStackCount, int newStackCount)
        {
        }

        protected override void OnRemoved()
        {
            if (playerStats != null)
            {
                playerStats.OnDamageDodged -= HandleDamageDodged;
            }
        }

        private void HandleDamageDodged(int incomingDamage)
        {
            if (playerStats == null)
            {
                return;
            }

            float roll = Random.Range(0f, 100f);
            float chance = effect.GetEffectiveHealChance(StackCount);
            if (roll > chance)
            {
                return;
            }

            int heal = effect.GetEffectiveHealAmount(StackCount);
            if (heal <= 0)
            {
                return;
            }

            playerStats.Heal(heal);
        }
    }
}