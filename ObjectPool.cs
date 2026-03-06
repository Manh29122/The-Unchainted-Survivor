using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic Object Pool - dùng chung cho Skill, Projectile, VFX...
/// 
/// Cách dùng:
///   var pool = new ObjectPool(prefab, parent, 20);
///   GameObject obj = pool.Get(position, rotation);
///   pool.Return(obj);
/// </summary>
public class ObjectPool
{
    private readonly GameObject prefab;
    private readonly Transform parent;
    private readonly Queue<GameObject> available = new Queue<GameObject>();
    private readonly HashSet<GameObject> inUse = new HashSet<GameObject>();

    public int TotalCreated { get; private set; }
    public int ActiveCount => inUse.Count;
    public int IdleCount => available.Count;

    // ─────────────────────────────────────────
    public ObjectPool(GameObject prefab, Transform parent = null, int preload = 10)
    {
        this.prefab = prefab;
        this.parent = parent;

        // Tạo sẵn một lượng object để tránh GC spike lúc đầu game
        for (int i = 0; i < preload; i++)
            available.Enqueue(CreateNew());
    }

    // ─────────────────────────────────────────
    /// <summary>Lấy object từ pool, đặt tại vị trí và rotation chỉ định</summary>
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject obj = available.Count > 0
            ? available.Dequeue()
            : CreateNew();             // Pool cạn → tạo thêm tự động

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        inUse.Add(obj);
        return obj;
    }

    public GameObject Get(Vector3 position) => Get(position, Quaternion.identity);

    // ─────────────────────────────────────────
    /// <summary>Trả object về pool (không Destroy)</summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;
        if (!inUse.Contains(obj))
        {
            Debug.LogWarning($"[Pool] Object '{obj.name}' không thuộc pool này!");
            return;
        }

        obj.SetActive(false);
        inUse.Remove(obj);
        available.Enqueue(obj);
    }

    // ─────────────────────────────────────────
    /// <summary>Trả toàn bộ object đang active về pool</summary>
    public void ReturnAll()
    {
        var copy = new List<GameObject>(inUse);
        foreach (var obj in copy)
            Return(obj);
    }

    // ─────────────────────────────────────────
    private GameObject CreateNew()
    {
        var obj = Object.Instantiate(prefab, parent);
        obj.name = $"{prefab.name}_pooled_{TotalCreated++}";
        obj.SetActive(false);
        return obj;
    }
}