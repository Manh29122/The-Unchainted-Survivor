using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerItemInventory : MonoBehaviour
{
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
        if (playerStats == null)
        {
            return;
        }

        foreach (ItemStatModifier modifier in itemData.statModifiers)
        {
            playerStats.ModifyStat(modifier.statType, modifier.value);
        }

        foreach (ItemEffectBase effect in itemData.specialEffects)
        {
            if (effect != null)
            {
                effect.OnItemAdded(playerStats, this, itemData, stackCount);
            }
        }
    }

    private void RemoveItemEffects(UnchaintedItemData itemData, int stackCount)
    {
        if (playerStats == null)
        {
            return;
        }

        foreach (ItemStatModifier modifier in itemData.statModifiers)
        {
            playerStats.ModifyStat(modifier.statType, -modifier.value);
        }

        foreach (ItemEffectBase effect in itemData.specialEffects)
        {
            if (effect != null)
            {
                effect.OnItemRemoved(playerStats, this, itemData, stackCount);
            }
        }
    }

    private void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}
