using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Spawn item khi enemy ch?t.
/// G?n v�o GameManager ho?c g?i ItemSpawner.Instance.Drop() t? EnemyDeathSystem.
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance { get; private set; }

    // ?????????????????????????????????????????
    //  DROP TABLE
    // ?????????????????????????????????????????
    [System.Serializable]
    public class DropEntry
    {
        public ItemData itemData;
        public GameObject prefab;           // Prefab c� PickupItem + SpriteRenderer
        [Range(0f, 1f)]
        public float dropChance = 0.5f;     // 0 = kh�ng bao gi?, 1 = lu�n lu�n
        public int poolSize = 30;
        [HideInInspector] public ObjectPool pool;
    }

    [Header("Drop Table")]
    public List<DropEntry> dropTable = new List<DropEntry>();

    [Header("Spawn Settings")]
    [Tooltip("V?t ph?m v?ng ra xung quanh v? tr� drop")]
    public float scatterRadius = 0.5f;

    private Transform poolContainer;

    // ?????????????????????????????????????????
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        poolContainer = new GameObject("ItemPool").transform;
        poolContainer.SetParent(transform);

        // Kh?i t?o pool cho t?ng lo?i item
        foreach (var entry in dropTable)
        {
            if (entry.prefab == null) continue;

            var container = new GameObject($"Pool_{entry.itemData?.itemName ?? "Item"}").transform;
            container.SetParent(poolContainer);
            entry.pool = new ObjectPool(entry.prefab, container, entry.poolSize);
        }
    }

    // ?????????????????????????????????????????
    //  PUBLIC API
    // ?????????????????????????????????????????

    /// <summary>
    /// Drop ng?u nhi�n t? drop table t?i v? tr� ch? ??nh.
    /// G?i khi enemy ch?t: ItemSpawner.Instance.Drop(enemy.position);
    /// </summary>
    public void Drop(Vector3 position)
    {
        foreach (var entry in dropTable)
        {
            if (entry.pool == null) continue;
            if (Random.value > entry.dropChance) continue;

            SpawnItem(entry, position);
        }
    }

    /// <summary>Drop 1 lo?i item c? th? (kh�ng theo x�c su?t)</summary>
    public void DropSpecific(string itemName, Vector3 position)
    {
        var entry = dropTable.Find(e => e.itemData?.itemName == itemName);
        if (entry != null)
            SpawnItem(entry, position);
    }

    public void DropSpecific(ItemData itemData, Vector3 position, int count = 1)
    {
        if (itemData == null)
        {
            return;
        }

        DropEntry entry = dropTable.Find(e => e.itemData == itemData);
        if (entry == null)
        {
            Debug.LogWarning($"[ItemSpawner] No DropEntry found for item: {itemData.itemName}");
            return;
        }

        int validCount = Mathf.Max(0, count);
        for (int index = 0; index < validCount; index++)
        {
            SpawnItem(entry, position);
        }
    }

    public void DropPrefab(GameObject prefab, Vector3 position, int count = 1)
    {
        if (prefab == null)
        {
            return;
        }

        int validCount = Mathf.Max(0, count);
        for (int index = 0; index < validCount; index++)
        {
            Vector2 scatter = Random.insideUnitCircle * scatterRadius;
            Vector3 spawnPos = position + new Vector3(scatter.x, scatter.y, 0f);
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }

    /// <summary>Drop t?t c? item ??m b?o (boss loot)</summary>
    public void DropGuaranteed(Vector3 position, List<string> itemNames)
    {
        foreach (var name in itemNames)
            DropSpecific(name, position);
    }

    // ?????????????????????????????????????????
    //  SPAWN
    // ?????????????????????????????????????????
    void SpawnItem(DropEntry entry, Vector3 basePosition)
    {
        // V?ng ra quanh v? tr� drop
        Vector2 scatter = Random.insideUnitCircle * scatterRadius;
        Vector3 spawnPos = basePosition + new Vector3(scatter.x, scatter.y, 0f);

        GameObject obj = entry.pool.Get(spawnPos);

        // G�n data v� pool v�o PickupItem
        var pickup = obj.GetComponent<PickupItem>();
        if (pickup != null)
        {
            pickup.data = entry.itemData;
            pickup.OwnerPool = entry.pool;
        }
    }
}