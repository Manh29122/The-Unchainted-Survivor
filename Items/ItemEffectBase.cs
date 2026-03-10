using UnityEngine;

public abstract class ItemEffectBase : ScriptableObject
{
    public virtual string GetDescription(UnchaintedItemData itemData, int stackCount)
    {
        return string.Empty;
    }

    public virtual ItemEffectHandle CreateHandle(EffectContext context, UnchaintedItemData itemData, int stackCount)
    {
        return new DelegatingItemEffectHandle(this, context, itemData, stackCount);
    }

    public virtual void OnEffectApplied(EffectContext context, UnchaintedItemData itemData, int stackCount)
    {
        OnItemAdded(context != null ? context.PlayerStats : null, context != null ? context.Inventory : null, itemData, stackCount);
    }

    public virtual void OnEffectStackChanged(EffectContext context, UnchaintedItemData itemData, int previousStackCount, int newStackCount)
    {
        if (newStackCount > previousStackCount)
        {
            for (int stack = previousStackCount + 1; stack <= newStackCount; stack++)
            {
                OnItemAdded(context != null ? context.PlayerStats : null, context != null ? context.Inventory : null, itemData, stack);
            }

            return;
        }

        for (int stack = previousStackCount; stack > newStackCount; stack--)
        {
            OnItemRemoved(context != null ? context.PlayerStats : null, context != null ? context.Inventory : null, itemData, stack);
        }
    }

    public virtual void OnEffectRemoved(EffectContext context, UnchaintedItemData itemData, int stackCount)
    {
    }

    public virtual void OnItemAdded(PlayerStats playerStats, PlayerItemInventory inventory, UnchaintedItemData itemData, int stackCount)
    {
    }

    public virtual void OnItemRemoved(PlayerStats playerStats, PlayerItemInventory inventory, UnchaintedItemData itemData, int stackCount)
    {
    }
}
