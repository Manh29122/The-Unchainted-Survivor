using UnityEngine;

public sealed class EffectContext
{
    public EffectContext(PlayerStats playerStats, PlayerItemInventory inventory, GameObject ownerObject, Transform ownerTransform, MonoBehaviour coroutineHost)
    {
        PlayerStats = playerStats;
        Inventory = inventory;
        OwnerObject = ownerObject;
        OwnerTransform = ownerTransform;
        CoroutineHost = coroutineHost;
    }

    public PlayerStats PlayerStats { get; }
    public PlayerItemInventory Inventory { get; }
    public GameObject OwnerObject { get; }
    public Transform OwnerTransform { get; }
    public MonoBehaviour CoroutineHost { get; }

    public T GetComponent<T>() where T : Component
    {
        return OwnerObject != null ? OwnerObject.GetComponent<T>() : null;
    }
}
