using System;
using UnityEngine;

/// <summary>
/// Gắn vào prefab bất kỳ để nó tự trả về pool sau lifetime.
/// PooledObject là base class — kế thừa để tạo Projectile, AoESkill, Buff...
/// </summary>
public class PooledObject : MonoBehaviour
{
    [Header("Auto Return")]
    [Tooltip("Tự trả về pool sau X giây. 0 = không tự trả")]
    public float lifetime = 3f;

    // Pool đang quản lý object này (gán bởi SkillSpawner)
    public ObjectPool OwnerPool { get; set; }

    // Event khi object được kích hoạt lại từ pool
    public event Action OnSpawned;

    private float lifeTimer;

    // ─────────────────────────────────────────
    void OnEnable()
    {
        lifeTimer = lifetime;
        OnSpawned?.Invoke();
        OnActivated();
    }

    void Update()
    {
        if (lifetime <= 0f) return;

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            ReturnToPool();
    }

    // ─────────────────────────────────────────
    /// <summary>Trả về pool thủ công (gọi khi skill kết thúc sớm)</summary>
    public void ReturnToPool()
    {
        OnDeactivated();
        OwnerPool?.Return(gameObject);
    }

    // ─────────────────────────────────────────
    // Override ở subclass nếu cần logic riêng
    protected virtual void OnActivated() { }
    protected virtual void OnDeactivated() { }
}