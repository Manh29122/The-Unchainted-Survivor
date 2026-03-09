using UnityEngine;

public abstract class ItemEffectBase : ScriptableObject
{
    public virtual void OnItemAdded(PlayerStats playerStats, PlayerItemInventory inventory, UnchaintedItemData itemData, int stackCount)
    {
    }

    public virtual void OnItemRemoved(PlayerStats playerStats, PlayerItemInventory inventory, UnchaintedItemData itemData, int stackCount)
    {
    }
}
