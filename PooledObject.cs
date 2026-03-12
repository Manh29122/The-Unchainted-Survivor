using System;
using UnityEngine;

/// <summary>
/// Base pooled object that supports both explicit pool assignment and optional auto-return by lifetime.
/// </summary>
public class PooledObject : MonoBehaviour
{
    [Header("Auto Return")]
    [Tooltip("Tự trả về pool sau X giây. 0 = không tự trả")]
    public float lifetime = 0f;

    private GameObject sourcePrefab;
    private float lifeTimer;

    public ObjectPool OwnerPool { get; set; }
    public GameObject SourcePrefab => sourcePrefab;
    public bool IsAssigned => OwnerPool != null;

    public event Action OnSpawned;

    public void AssignPool(ObjectPool objectPool, GameObject prefab)
    {
        OwnerPool = objectPool;
        sourcePrefab = prefab;
    }

    private void OnEnable()
    {
        lifeTimer = lifetime;
        OnSpawned?.Invoke();
        OnActivated();
    }

    private void Update()
    {
        if (lifetime <= 0f)
        {
            return;
        }

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            ReturnToPool();
        }
    }

    public bool ReturnToPool()
    {
        if (OwnerPool == null)
        {
            return false;
        }

        OnDeactivated();
        OwnerPool.Return(gameObject);
        return true;
    }

    protected virtual void OnActivated() { }
    protected virtual void OnDeactivated() { }
}