using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopRerollUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShopRerollSystem shopSystem;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TMP_Text rerollCostText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private List<ShopItemSlotUI> itemSlots = new List<ShopItemSlotUI>();

    [Header("Display")]
    [SerializeField] private string rerollPrefix = "Reroll: ";
    [SerializeField] private string notEnoughGoldMessage = "Not enough gold";
    [SerializeField] private string purchaseFailedMessage = "Cannot buy item";
    [SerializeField] private string purchasedPrefix = "Bought: ";

    private void Awake()
    {
        if (shopSystem == null)
        {
            shopSystem = FindFirstObjectByType<ShopRerollSystem>();
        }

        if (rerollButton != null)
        {
            rerollButton.gameObject.SetActive(false);
        }

        if (rerollCostText != null)
        {
            rerollCostText.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (shopSystem == null)
        {
            return;
        }

        shopSystem.OnOffersRolled += RefreshOffers;
        shopSystem.OnOffersChanged += RefreshOffers;
        shopSystem.OnRerollCostChanged += RefreshRerollCost;
        shopSystem.OnRerollFailed += HandleRerollFailed;
        shopSystem.OnPurchaseFailed += HandlePurchaseFailed;
        shopSystem.OnItemPurchased += HandleItemPurchased;

        RefreshRerollCost(shopSystem.CurrentRerollCost);
        RefreshOffers(new List<UnchaintedItemData>(shopSystem.CurrentOffers));
    }

    private void Start()
    {
        if (shopSystem != null && shopSystem.CurrentOffers.Count == 0)
        {
            shopSystem.RollInitialOffers();
        }
    }

    private void OnDisable()
    {
        if (shopSystem == null)
        {
            return;
        }

        shopSystem.OnOffersRolled -= RefreshOffers;
        shopSystem.OnOffersChanged -= RefreshOffers;
        shopSystem.OnRerollCostChanged -= RefreshRerollCost;
        shopSystem.OnRerollFailed -= HandleRerollFailed;
        shopSystem.OnPurchaseFailed -= HandlePurchaseFailed;
        shopSystem.OnItemPurchased -= HandleItemPurchased;
    }

    private void RefreshOffers(List<UnchaintedItemData> offers)
    {
        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (itemSlots[i] == null)
            {
                continue;
            }

            if (offers != null && i < offers.Count)
            {
                itemSlots[i].Bind(shopSystem, i, offers[i]);
            }
            else
            {
                itemSlots[i].Clear();
            }
        }
    }

    private void RefreshRerollCost(int cost)
    {
        if (rerollCostText != null)
        {
            rerollCostText.text = $"{rerollPrefix}{cost}";
        }
    }

    private void HandleRerollFailed(int cost)
    {
        SetFeedback($"{notEnoughGoldMessage} ({cost})");
    }

    private void HandlePurchaseFailed(UnchaintedItemData itemData)
    {
        SetFeedback(itemData != null ? $"{purchaseFailedMessage}: {itemData.itemName}" : purchaseFailedMessage);
    }

    private void HandleItemPurchased(UnchaintedItemData itemData)
    {
        SetFeedback(itemData != null ? $"{purchasedPrefix}{itemData.itemName}" : string.Empty);
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }
}
