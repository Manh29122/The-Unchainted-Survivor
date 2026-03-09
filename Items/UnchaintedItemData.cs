using System.Collections.Generic;
using UnityEngine;

public enum ItemTier
{
    Tier1,
    Tier2,
    Tier3,
    Tier4,
    Legendary
}

[CreateAssetMenu(fileName = "NewUnchaintedItem", menuName = "Game/Unchainted Item Data")]
public class UnchaintedItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemId;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemTier tier = ItemTier.Tier1;

    [Header("Economy")]
    public int price = 25;
    public int maxStacks = 1;

    [Header("Stats")]
    public List<ItemStatModifier> statModifiers = new List<ItemStatModifier>();

    [Header("Special Effects")]
    public List<ItemEffectBase> specialEffects = new List<ItemEffectBase>();
}
