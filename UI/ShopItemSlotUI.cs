using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemSlotUI : MonoBehaviour
{
    [System.Serializable]
    private class TierColorEntry
    {
        public ItemTier tier;
        public Color color = Color.white;
    }

    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button buyButton;

    [Header("Tier Colors")]
    [SerializeField] private bool applyTierColorToName = true;
    [SerializeField] private bool applyTierColorToTierLabel = true;
    [SerializeField] private List<TierColorEntry> tierColors = new List<TierColorEntry>
    {
        new TierColorEntry { tier = ItemTier.Tier1, color = new Color(0.55f, 0.9f, 0.2f) },
        new TierColorEntry { tier = ItemTier.Tier2, color = new Color(0.1f, 0.75f, 0.2f) },
        new TierColorEntry { tier = ItemTier.Tier3, color = new Color(1f, 0.85f, 0.2f) },
        new TierColorEntry { tier = ItemTier.Tier4, color = new Color(1f, 0.55f, 0.15f) },
        new TierColorEntry { tier = ItemTier.Legendary, color = new Color(0.9f, 0.2f, 0.2f) }
    };

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
        Color tierColor = hasItem ? GetTierColor(itemData.tier) : Color.white;
        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.enabled = hasItem;
            iconImage.sprite = hasItem ? itemData.icon : null;
        }

        if (nameText != null)
        {
            nameText.text = hasItem ? itemData.itemName : "Empty";
            if (hasItem && applyTierColorToName)
            {
                nameText.color = tierColor;
            }
        }

        if (priceText != null)
        {
            priceText.text = hasItem ? itemData.price.ToString() : string.Empty;
        }

        if (tierText != null)
        {
            tierText.text = hasItem ? itemData.tier.ToString() : string.Empty;
            if (hasItem && applyTierColorToTierLabel)
            {
                tierText.color = tierColor;
            }
        }

        if (descriptionText != null)
        {
            descriptionText.text = hasItem ? BuildItemDescription(itemData) : string.Empty;
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

    private string BuildItemDescription(UnchaintedItemData itemData)
    {
        if (itemData == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();

        if (itemData.statModifiers != null)
        {
            for (int i = 0; i < itemData.statModifiers.Count; i++)
            {
                ItemStatModifier modifier = itemData.statModifiers[i];
                if (modifier == null)
                {
                    continue;
                }

                string color = modifier.value >= 0f ? "#4CFF6B" : "#FF5C5C";
                string sign = modifier.value >= 0f ? "+" : string.Empty;
                string statName = GetDisplayName(modifier.statType);
                string valueText = Mathf.Approximately(modifier.value % 1f, 0f)
                    ? Mathf.RoundToInt(modifier.value).ToString()
                    : modifier.value.ToString("0.##");

                builder.AppendLine($"<color={color}>{sign}{valueText} {statName}</color>");
            }
        }

        if (itemData.specialEffects != null)
        {
            for (int i = 0; i < itemData.specialEffects.Count; i++)
            {
                ItemEffectBase effect = itemData.specialEffects[i];
                if (effect == null)
                {
                    continue;
                }

                string effectDescription = effect.GetDescription(itemData, 1);
                if (!string.IsNullOrWhiteSpace(effectDescription))
                {
                    builder.AppendLine(effectDescription.Trim());
                }
            }
        }

        return builder.ToString().Trim();
    }

    private string GetDisplayName(PlayerStatType statType)
    {
        switch (statType)
        {
            case PlayerStatType.MaxHP: return "Max HP";
            case PlayerStatType.HPRegen: return "HP Regen";
            case PlayerStatType.Armor: return "Armor";
            case PlayerStatType.Dodge: return "Dodge";
            case PlayerStatType.MoveSpeed: return "Move Speed";
            case PlayerStatType.DamagePercent: return "% Damage";
            case PlayerStatType.MeleeDamage: return "Melee Damage";
            case PlayerStatType.RangedDamage: return "Ranged Damage";
            case PlayerStatType.ElementalDamage: return "Elemental Damage";
            case PlayerStatType.AttackSpeed: return "Attack Speed";
            case PlayerStatType.CritChance: return "Crit Chance";
            case PlayerStatType.CritDamage: return "Crit Damage";
            case PlayerStatType.Engineering: return "Engineering";
            case PlayerStatType.ExplosionDamage: return "Explosion Damage";
            case PlayerStatType.ExplosionSize: return "Explosion Size";
            case PlayerStatType.LifeSteal: return "Life Steal";
            case PlayerStatType.Knockback: return "Knockback";
            case PlayerStatType.Range: return "Range";
            case PlayerStatType.ProjectileSpeed: return "Projectile Speed";
            case PlayerStatType.Luck: return "Luck";
            case PlayerStatType.Harvesting: return "Harvesting";
            case PlayerStatType.Magnet: return "Magnet";
            case PlayerStatType.PickupRadius: return "Pickup Radius";
            case PlayerStatType.GoldMultiplier: return "Gold Multiplier";
            case PlayerStatType.ExpMultiplier: return "Exp Multiplier";
            default: return statType.ToString();
        }
    }

    private Color GetTierColor(ItemTier tier)
    {
        for (int i = 0; i < tierColors.Count; i++)
        {
            if (tierColors[i].tier == tier)
            {
                return tierColors[i].color;
            }
        }

        return Color.white;
    }
}
