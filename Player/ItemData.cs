using UnityEngine;

/// <summary>
/// ScriptableObject định nghĩa 1 loại vật phẩm.
/// Tạo bằng: chuột phải → Create → Game/Item Data
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName;
    public ItemType type;
    public Sprite icon;

    [Header("Value")]
    [Tooltip("Exp nhận được / Gold / HP hồi / ...")]
    public int value = 10;

    [Header("Magnet")]
    [Tooltip("Bán kính bắt đầu bị hút về player")]
    public float magnetRadius = 3f;
    [Tooltip("Tốc độ hút về player")]
    public float magnetSpeed = 8f;

    [Header("Pickup")]
    [Tooltip("Bán kính chạm để thu thập")]
    public float pickupRadius = 0.4f;

    [Header("Temporary Bonus")]
    [Tooltip("Loại buff tạm thời khi nhặt item")]
    public TemporaryBonusType temporaryBonusType = TemporaryBonusType.None;
    [Tooltip("Giá trị buff theo phần trăm. Ví dụ 50 = +50%")]
    public float temporaryBonusPercent = 50f;
    [Tooltip("Thời gian tồn tại buff tạm thời")]
    public float temporaryBonusDuration = 5f;

    [Header("Visuals")]
    public Color glowColor = Color.yellow;
    [Tooltip("Thời gian nhấp nháy trước khi biến mất")]
    public float despawnTime = 15f;
}

public enum ItemType
{
    ExpGem,
    Gold,
    HP,
    PowerUp
}

public enum TemporaryBonusType
{
    None,
    GoldMultiplier,
    ExpMultiplier,
    PickupRadiusMultiplier
}