using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button buyButton;

    private ShopRerollSystem shopSystem;
    private int offerIndex = -1;
    private UnchaintedItemData currentItem;

    private void Awake()
    {
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(BuyCurrentOffer);
        }
    }

    public void Bind(ShopRerollSystem rerollSystem, int index, UnchaintedItemData itemData)
    {
        shopSystem = rerollSystem;
        offerIndex = index;
        currentItem = itemData;

        bool hasItem = itemData != null;
        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.enabled = hasItem;
            iconImage.sprite = hasItem ? itemData.icon : null;
        }

        if (nameText != null)
        {
            nameText.text = hasItem ? itemData.itemName : "Empty";
        }

        if (priceText != null)
        {
            priceText.text = hasItem ? itemData.price.ToString() : string.Empty;
        }

        if (tierText != null)
        {
            tierText.text = hasItem ? itemData.tier.ToString() : string.Empty;
        }

        if (descriptionText != null)
        {
            descriptionText.text = hasItem ? itemData.description : string.Empty;
        }

        if (buyButton != null)
        {
            buyButton.interactable = hasItem;
        }
    }

    public void Clear()
    {
        Bind(shopSystem, -1, null);
    }

    public void BuyCurrentOffer()
    {
        if (shopSystem == null || offerIndex < 0 || currentItem == null)
        {
            return;
        }

        shopSystem.TryBuyOfferAt(offerIndex);
    }
}
