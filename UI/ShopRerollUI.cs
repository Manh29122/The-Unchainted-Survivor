using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopRerollUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShopRerollSystem shopSystem;
    [SerializeField] private EnemyWaveSpawner waveSpawner;
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button closeShopButton;
    [SerializeField] private TMP_Text rerollCostText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private List<ShopItemSlotUI> itemSlots = new List<ShopItemSlotUI>();

    [Header("Floating Warning")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private RectTransform floatingTextParent;
    [SerializeField] private Vector3 rerollWarningOffset = new Vector3(0f, 50f, 0f);
    [SerializeField] private Color rerollWarningColor = Color.red;

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

        if (waveSpawner == null)
        {
            waveSpawner = FindFirstObjectByType<EnemyWaveSpawner>();
        }

        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(HandleRerollButton);
        }

        if (closeShopButton != null)
        {
            closeShopButton.onClick.AddListener(HandleCloseShopButton);
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
        shopSystem.OnFreeRerollsChanged += RefreshRerollCost;
        shopSystem.OnRerollFailed += HandleRerollFailed;
        shopSystem.OnPurchaseFailed += HandlePurchaseFailed;
        shopSystem.OnItemPurchased += HandleItemPurchased;

        RefreshRerollCost(shopSystem.CurrentRerollCost);
        RefreshOffers(new List<UnchaintedItemData>(shopSystem.CurrentOffers));
    }

    private void Start()
    {
        if (gameObject.activeSelf && shopSystem != null && shopSystem.CurrentOffers.Count == 0)
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
        shopSystem.OnFreeRerollsChanged -= RefreshRerollCost;
        shopSystem.OnRerollFailed -= HandleRerollFailed;
        shopSystem.OnPurchaseFailed -= HandlePurchaseFailed;
        shopSystem.OnItemPurchased -= HandleItemPurchased;
    }

    public void OpenShop()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (shopSystem == null)
        {
            shopSystem = FindFirstObjectByType<ShopRerollSystem>();
        }

        if (waveSpawner == null)
        {
            waveSpawner = FindFirstObjectByType<EnemyWaveSpawner>();
        }

        SetFeedback(string.Empty);

        if (shopSystem != null)
        {
            shopSystem.RollInitialOffers();
            RefreshRerollCost(shopSystem.CurrentRerollCost);
            RefreshOffers(new List<UnchaintedItemData>(shopSystem.CurrentOffers));
        }
    }

    public void HideShop()
    {
        SetFeedback(string.Empty);

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void HandleRerollButton()
    {
        if (shopSystem == null)
        {
            return;
        }

        bool success = shopSystem.TryReroll();
        if (success)
        {
            SetFeedback(string.Empty);
        }
    }

    private void HandleCloseShopButton()
    {
        HideShop();

        if (waveSpawner != null)
        {
            waveSpawner.StartNextWave();
        }
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
            rerollCostText.text = cost.ToString();
        }
    }

    private void HandleRerollFailed(int cost)
    {
        SetFeedback($"{notEnoughGoldMessage} ({cost})");
        ShowRerollWarningFloatingText(notEnoughGoldMessage);
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

    private void ShowRerollWarningFloatingText(string message)
    {
        if (floatingTextPrefab == null || rerollButton == null)
        {
            return;
        }

        Transform parentTransform = floatingTextParent != null ? floatingTextParent : rerollButton.transform.parent;
        GameObject popup = Instantiate(floatingTextPrefab, parentTransform);

        RectTransform popupRect = popup.transform as RectTransform;
        RectTransform parentRect = parentTransform as RectTransform;

        if (popupRect != null && parentRect != null)
        {
            popupRect.anchoredPosition = (Vector2)rerollWarningOffset;
            popupRect.localScale = Vector3.one;
            Vector3 localPosition = popupRect.localPosition;
            localPosition.z = -10f;
            popupRect.localPosition = localPosition;
        }
        else
        {
            popup.transform.localPosition = rerollWarningOffset;
            popup.transform.localScale = Vector3.one;
            Vector3 localPosition = popup.transform.localPosition;
            localPosition.z = -10f;
            popup.transform.localPosition = localPosition;
        }

        FloatingText floatingText = popup.GetComponent<FloatingText>();
        if (floatingText != null)
        {
            floatingText.SetText(message, rerollWarningColor);
        }
    }
}
