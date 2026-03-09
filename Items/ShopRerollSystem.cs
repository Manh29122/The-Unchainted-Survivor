using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopRerollSystem : MonoBehaviour
{
    [Serializable]
    public class TierWeight
    {
        public ItemTier tier;
        public float baseWeight = 1f;
        public float luckWeightBonus = 0f;
    }

    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerItemInventory playerInventory;

    [Header("Shop Pool")]
    [SerializeField] private List<UnchaintedItemData> itemPool = new List<UnchaintedItemData>();
    [SerializeField] private int offerCount = 3;

    [Header("Reroll Cost")]
    [SerializeField] private int baseRerollCost = 10;
    [SerializeField] private int currentRerollCost = 10;
    [SerializeField] private float rerollCostMultiplier = 2f;

    [Header("Tier Weights")]
    [SerializeField] private List<TierWeight> tierWeights = new List<TierWeight>
    {
        new TierWeight { tier = ItemTier.Tier1, baseWeight = 60f, luckWeightBonus = -0.6f },
        new TierWeight { tier = ItemTier.Tier2, baseWeight = 25f, luckWeightBonus = 0.25f },
        new TierWeight { tier = ItemTier.Tier3, baseWeight = 10f, luckWeightBonus = 0.2f },
        new TierWeight { tier = ItemTier.Tier4, baseWeight = 4f, luckWeightBonus = 0.1f },
        new TierWeight { tier = ItemTier.Legendary, baseWeight = 1f, luckWeightBonus = 0.05f }
    };

    private readonly List<UnchaintedItemData> currentOffers = new List<UnchaintedItemData>();

    public event Action<List<UnchaintedItemData>> OnOffersRolled;
    public event Action<List<UnchaintedItemData>> OnOffersChanged;
    public event Action<int> OnRerollCostChanged;
    public event Action<int> OnRerollFailed;
    public event Action<UnchaintedItemData> OnItemPurchased;
    public event Action<UnchaintedItemData> OnPurchaseFailed;

    public IReadOnlyList<UnchaintedItemData> CurrentOffers => currentOffers;
    public int CurrentRerollCost => currentRerollCost;

    private void Awake()
    {
        ResolveReferences();

        ResetRerollCost();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    public void RollInitialOffers()
    {
        GenerateOffers();
    }

    public bool TryBuyOfferAt(int offerIndex)
    {
        ResolveReferences();

        if (offerIndex < 0 || offerIndex >= currentOffers.Count)
        {
            return false;
        }

        UnchaintedItemData item = currentOffers[offerIndex];
        if (item == null || playerStats == null || playerInventory == null)
        {
            OnPurchaseFailed?.Invoke(item);
            return false;
        }

        if (!playerStats.SpendGold(item.price))
        {
            OnPurchaseFailed?.Invoke(item);
            return false;
        }

        if (!playerInventory.AddItem(item))
        {
            playerStats.AddGold(item.price);
            OnPurchaseFailed?.Invoke(item);
            return false;
        }

        currentOffers[offerIndex] = RollReplacementItem(offerIndex, item);
        NotifyOffersChanged();
        OnItemPurchased?.Invoke(item);
        return true;
    }

    public bool TryReroll()
    {
        ResolveReferences();

        if (playerStats == null)
        {
            Debug.LogWarning("[ShopRerollSystem] PlayerStats not found.");
            return false;
        }

        if (!playerStats.SpendGold(currentRerollCost))
        {
            OnRerollFailed?.Invoke(currentRerollCost);
            return false;
        }

        GenerateOffers();
        currentRerollCost = Mathf.Max(1, Mathf.RoundToInt(currentRerollCost * Mathf.Max(1f, rerollCostMultiplier)));
        OnRerollCostChanged?.Invoke(currentRerollCost);
        return true;
    }

    public void ResetRerollCost()
    {
        currentRerollCost = Mathf.Max(1, baseRerollCost);
        OnRerollCostChanged?.Invoke(currentRerollCost);
    }

    public void SetItemPool(List<UnchaintedItemData> items)
    {
        itemPool = items ?? new List<UnchaintedItemData>();
    }

    public void ResolveReferences()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }

        if (playerInventory == null)
        {
            playerInventory = GetComponent<PlayerItemInventory>();
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerStats == null && playerObject != null)
        {
            playerStats = playerObject.GetComponent<PlayerStats>();
        }

        if (playerInventory == null && playerObject != null)
        {
            playerInventory = playerObject.GetComponent<PlayerItemInventory>();
        }

        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerItemInventory>();
        }
    }

    private void GenerateOffers()
    {
        currentOffers.Clear();

        List<UnchaintedItemData> availableItems = GetAvailableItems();
        if (availableItems.Count == 0)
        {
            Debug.LogWarning("[ShopRerollSystem] No valid items available for reroll.");
            NotifyOffersRolled();
            return;
        }

        int count = Mathf.Min(Mathf.Max(1, offerCount), availableItems.Count);
        List<UnchaintedItemData> localPool = new List<UnchaintedItemData>(availableItems);

        for (int i = 0; i < count; i++)
        {
            UnchaintedItemData item = RollSingleItem(localPool);
            if (item == null)
            {
                break;
            }

            currentOffers.Add(item);
            localPool.Remove(item);
        }

        NotifyOffersRolled();
    }

    private void NotifyOffersRolled()
    {
        List<UnchaintedItemData> snapshot = new List<UnchaintedItemData>(currentOffers);
        OnOffersRolled?.Invoke(snapshot);
        OnOffersChanged?.Invoke(snapshot);
    }

    private void NotifyOffersChanged()
    {
        OnOffersChanged?.Invoke(new List<UnchaintedItemData>(currentOffers));
    }

    private List<UnchaintedItemData> GetAvailableItems()
    {
        return GetAvailableItems(null);
    }

    private List<UnchaintedItemData> GetAvailableItems(HashSet<string> excludedItemIds, HashSet<UnchaintedItemData> excludedItems)
    {
        List<UnchaintedItemData> availableItems = new List<UnchaintedItemData>();

        foreach (UnchaintedItemData item in itemPool)
        {
            if (item == null)
            {
                continue;
            }

            if (IsExcludedItem(item, excludedItemIds, excludedItems))
            {
                continue;
            }

            if (playerInventory != null && playerInventory.GetStackCount(item) >= Mathf.Max(1, item.maxStacks))
            {
                continue;
            }

            availableItems.Add(item);
        }

        return availableItems;
    }

    private UnchaintedItemData RollReplacementItem(int replacedOfferIndex, UnchaintedItemData previousItem)
    {
        HashSet<string> excludedItemIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<UnchaintedItemData> excludedItems = new HashSet<UnchaintedItemData>();

        AddExcludedItem(previousItem, excludedItemIds, excludedItems);

        for (int i = 0; i < currentOffers.Count; i++)
        {
            if (i == replacedOfferIndex)
            {
                continue;
            }

            UnchaintedItemData shownItem = currentOffers[i];
            AddExcludedItem(shownItem, excludedItemIds, excludedItems);
        }

        List<UnchaintedItemData> availableItems = GetAvailableItems(excludedItemIds, excludedItems);
        if (availableItems.Count == 0)
        {
            return null;
        }

        return RollSingleItem(availableItems);
    }

    private UnchaintedItemData RollSingleItem(List<UnchaintedItemData> pool)
    {
        if (pool == null || pool.Count == 0)
        {
            return null;
        }

        ItemTier rolledTier = RollTier();
        List<UnchaintedItemData> tierItems = pool.FindAll(item => item.tier == rolledTier);

        if (tierItems.Count == 0)
        {
            tierItems = pool;
        }

        int randomIndex = UnityEngine.Random.Range(0, tierItems.Count);
        return tierItems[randomIndex];
    }

    private ItemTier RollTier()
    {
        float luck = playerStats != null ? playerStats.luck : 0f;
        float totalWeight = 0f;

        for (int i = 0; i < tierWeights.Count; i++)
        {
            totalWeight += GetTierWeightValue(tierWeights[i], luck);
        }

        if (totalWeight <= 0f)
        {
            return ItemTier.Tier1;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < tierWeights.Count; i++)
        {
            cumulative += GetTierWeightValue(tierWeights[i], luck);
            if (roll <= cumulative)
            {
                return tierWeights[i].tier;
            }
        }

        return tierWeights[tierWeights.Count - 1].tier;
    }

    private float GetTierWeightValue(TierWeight tierWeight, float luck)
    {
        return Mathf.Max(0f, tierWeight.baseWeight + (tierWeight.luckWeightBonus * luck));
    }

    private void AddExcludedItem(UnchaintedItemData item, HashSet<string> excludedItemIds, HashSet<UnchaintedItemData> excludedItems)
    {
        if (item == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(item.itemId))
        {
            excludedItemIds?.Add(item.itemId.Trim());
            return;
        }

        excludedItems?.Add(item);
    }

    private bool IsExcludedItem(UnchaintedItemData item, HashSet<string> excludedItemIds, HashSet<UnchaintedItemData> excludedItems)
    {
        if (item == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(item.itemId))
        {
            return excludedItemIds != null && excludedItemIds.Contains(item.itemId.Trim());
        }

        return excludedItems != null && excludedItems.Contains(item);
    }
}
