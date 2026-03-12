using UnityEngine;

[CreateAssetMenu(fileName = "StatGainOnWaveCompletedEffect", menuName = "Game/Items/Effects/Stat Gain On Wave Completed")]
public class StatGainOnWaveCompletedEffect : ItemEffectBase
{
    [Header("Wave Complete Bonus")]
    [SerializeField] private PlayerStatType statType = PlayerStatType.Harvesting;
    [SerializeField] private float amountPerWave = 1f;
    [SerializeField] private StatModifierValueType valueType = StatModifierValueType.Flat;
    [SerializeField] private bool multiplyByStack = true;

    public override string GetDescription(UnchaintedItemData itemData, int stackCount)
    {
        float amount = GetEffectiveAmount(stackCount);
        string statName = TriggeredStatGainEffectUtility.GetStatDisplayName(statType);
        string formattedAmount = TriggeredStatGainEffectUtility.FormatAmount(amount, valueType);
        return $"Gain {formattedAmount} {statName} when a wave ends";
    }

    public override ItemEffectHandle CreateHandle(EffectContext context, UnchaintedItemData itemData, int stackCount)
    {
        return new StatGainOnWaveCompletedEffectHandle(this, context, itemData, stackCount);
    }

    private float GetEffectiveAmount(int stackCount)
    {
        int effectiveStacks = Mathf.Max(1, stackCount);
        float value = multiplyByStack ? amountPerWave * effectiveStacks : amountPerWave;
        return Mathf.Max(0f, value);
    }

    private sealed class StatGainOnWaveCompletedEffectHandle : ItemEffectHandle
    {
        private readonly StatGainOnWaveCompletedEffect effect;
        private PlayerStats playerStats;
        private EnemyWaveSpawner waveSpawner;
        private float totalGrantedAmount;

        public StatGainOnWaveCompletedEffectHandle(StatGainOnWaveCompletedEffect effect, EffectContext context, UnchaintedItemData itemData, int initialStackCount)
            : base(effect, context, itemData, initialStackCount)
        {
            this.effect = effect;
        }

        protected override void OnApplied()
        {
            playerStats = Context != null ? Context.PlayerStats : null;
            waveSpawner = Object.FindFirstObjectByType<EnemyWaveSpawner>();

            if (playerStats == null || waveSpawner == null)
            {
                return;
            }

            waveSpawner.OnWaveCompleted += HandleWaveCompleted;
        }

        protected override void OnStackChanged(int previousStackCount, int newStackCount)
        {
        }

        protected override void OnRemoved()
        {
            if (waveSpawner != null)
            {
                waveSpawner.OnWaveCompleted -= HandleWaveCompleted;
            }

            if (playerStats != null && totalGrantedAmount > 0f)
            {
                playerStats.DecreaseStat(effect.statType, totalGrantedAmount);
                totalGrantedAmount = 0f;
            }
        }

        private void HandleWaveCompleted(int waveIndex)
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