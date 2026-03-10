using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerItemInventory : MonoBehaviour
{
    private class ActiveEffectEntry
    {
        public UnchaintedItemData itemData;
        public ItemEffectBase effectDefinition;
        public int effectIndex;
        public ItemEffectHandle handle;
    }

    [Serializable]
    public class OwnedItemEntry
    {
        public UnchaintedItemData itemData;
        public int stackCount;
    }

    [Header("Inventory")]
    [SerializeField] private List<OwnedItemEntry> ownedItems = new List<OwnedItemEntry>();
    [SerializeField] private bool autoFindPlayerStats = true;
    [SerializeField] private PlayerStats playerStats;

    private readonly List<ActiveEffectEntry> activeEffects = new List<ActiveEffectEntry>();

    public event Action<UnchaintedItemData, int> OnItemAdded;
    public event Action<UnchaintedItemData, int> OnItemRemoved;
    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (playerStats == null && autoFindPlayerStats)
        {
            playerStats = GetComponent<PlayerStats>();
        }
    }

    private void OnDestroy()
    {
        ClearActiveEffects();
    }

    public bool AddItem(UnchaintedItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0)
        {
            return false;
        }

        OwnedItemEntry entry = GetEntry(itemData);
        if (entry == null)
        {
            entry = new OwnedItemEntry
            {
                itemData = itemData,
                stackCount = 0
            };
            ownedItems.Add(entry);
        }

        int maxStacks = Mathf.Max(1, itemData.maxStacks);
        int addableAmount = Mathf.Min(amount, maxStacks - entry.stackCount);
        if (addableAmount <= 0)
        {
            return false;
        }

        for (int i = 0; i < addableAmount; i++)
        {
            entry.stackCount++;
            ApplyItem(itemData, entry.stackCount);
            OnItemAdded?.Invoke(itemData, entry.stackCount);
        }

        NotifyInventoryChanged();
        return true;
    }

    public bool RemoveItem(UnchaintedItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0)
        {
            return false;
        }

        OwnedItemEntry entry = GetEntry(itemData);
        if (entry == null || entry.stackCount <= 0)
        {
            return false;
        }

        int removableAmount = Mathf.Min(amount, entry.stackCount);
        for (int i = 0; i < removableAmount; i++)
        {
            RemoveItemEffects(itemData, entry.stackCount);
            entry.stackCount--;
            OnItemRemoved?.Invoke(itemData, entry.stackCount);
        }

        if (entry.stackCount <= 0)
        {
            ownedItems.Remove(entry);
        }

        NotifyInventoryChanged();
        return true;
    }

    public int GetStackCount(UnchaintedItemData itemData)
    {
        OwnedItemEntry entry = GetEntry(itemData);
        return entry != null ? entry.stackCount : 0;
    }

    public bool HasItem(UnchaintedItemData itemData)
    {
        return GetStackCount(itemData) > 0;
    }

    public List<OwnedItemEntry> GetOwnedItems()
    {
        return ownedItems;
    }

    public void ClearInventory()
    {
        for (int entryIndex = ownedItems.Count - 1; entryIndex >= 0; entryIndex--)
        {
            OwnedItemEntry entry = ownedItems[entryIndex];
            while (entry.stackCount > 0)
            {
                RemoveItemEffects(entry.itemData, entry.stackCount);
                entry.stackCount--;
            }
        }

        ownedItems.Clear();
        NotifyInventoryChanged();
    }

    private OwnedItemEntry GetEntry(UnchaintedItemData itemData)
    {
        return ownedItems.Find(entry => entry.itemData == itemData);
    }

    private void ApplyItem(UnchaintedItemData itemData, int stackCount)
    {
        if (playerStats != null && itemData.statModifiers != null)
        {
            foreach (ItemStatModifier modifier in itemData.statModifiers)
            {
                playerStats.ModifyStat(modifier.statType, modifier.value);
            }
        }

        SyncItemEffects(itemData, stackCount);
    }

    private void RemoveItemEffects(UnchaintedItemData itemData, int stackCount)
    {
        if (playerStats != null && itemData.statModifiers != null)
        {
            foreach (ItemStatModifier modifier in itemData.statModifiers)
            {
                playerStats.ModifyStat(modifier.statType, -modifier.value);
            }
        }

        SyncItemEffects(itemData, Mathf.Max(0, stackCount - 1));
    }

    private void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    private void SyncItemEffects(UnchaintedItemData itemData, int stackCount)
    {
        if (itemData == null)
        {
            return;
        }

        if (itemData.specialEffects == null || itemData.specialEffects.Count == 0)
        {
            if (stackCount <= 0)
            {
                RemoveAllHandlesForItem(itemData);
            }

            return;
        }

        EffectContext context = CreateEffectContext();

        for (int effectIndex = 0; effectIndex < itemData.specialEffects.Count; effectIndex++)
        {
            ItemEffectBase effectDefinition = itemData.specialEffects[effectIndex];
            if (effectDefinition == null)
            {
                continue;
            }

            ActiveEffectEntry activeEntry = FindActiveEffectEntry(itemData, effectDefinition, effectIndex);

            if (stackCount <= 0)
            {
                RemoveActiveEffectEntry(activeEntry);
                continue;
            }

            if (activeEntry == null)
            {
                ItemEffectHandle handle = effectDefinition.CreateHandle(context, itemData, stackCount);
                if (handle == null)
                {
                    continue;
                }

                activeEntry = new ActiveEffectEntry
                {
                    itemData = itemData,
                    effectDefinition = effectDefinition,
                    effectIndex = effectIndex,
                    handle = handle
                };

                activeEffects.Add(activeEntry);
                handle.Apply();
                continue;
            }

            activeEntry.handle.UpdateStackCount(stackCount);
        }
    }

    private EffectContext CreateEffectContext()
    {
        return new EffectContext(playerStats, this, gameObject, transform, this);
    }

    private ActiveEffectEntry FindActiveEffectEntry(UnchaintedItemData itemData, ItemEffectBase effectDefinition, int effectIndex)
    {
        return activeEffects.Find(entry => entry.itemData == itemData && entry.effectDefinition == effectDefinition && entry.effectIndex == effectIndex);
    }

    private void RemoveAllHandlesForItem(UnchaintedItemData itemData)
    {
        for (int index = activeEffects.Count - 1; index >= 0; index--)
        {
            if (activeEffects[index].itemData == itemData)
            {
                RemoveActiveEffectEntry(activeEffects[index]);
            }
        }
    }

    private void ClearActiveEffects()
    {
        for (int index = activeEffects.Count - 1; index >= 0; index--)
        {
            RemoveActiveEffectEntry(activeEffects[index]);
        }
    }

    private void RemoveActiveEffectEntry(ActiveEffectEntry activeEntry)
    {
        if (activeEntry == null)
        {
            return;
        }

        if (activeEntry.handle != null)
        {
            activeEntry.handle.UpdateStackCount(0);
            activeEntry.handle.Remove();
        }

        activeEffects.Remove(activeEntry);
    }
}
