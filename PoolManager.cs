using System.Collections.Generic;
using UnityEngine;

public static class PoolManager
{
    private static readonly Dictionary<GameObject, ObjectPool> poolsByPrefab = new Dictionary<GameObject, ObjectPool>();
    private static Transform poolRoot;

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, int preload = 0)
    {
        if (prefab == null)
        {
            return null;
        }

        ObjectPool pool = GetOrCreatePool(prefab, preload);
        return pool.Get(position, rotation);
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, int preload = 0)
    {
        return Spawn(prefab, position, Quaternion.identity, preload);
    }

    public static bool Return(GameObject instance)
    {
        if (instance == null)
        {
            return false;
        }

        PooledObject pooledObject = instance.GetComponent<PooledObject>();
        if (pooledObject == null)
        {
            return false;
        }

        return pooledObject.ReturnToPool();
    }

    public static ObjectPool GetOrCreatePool(GameObject prefab, int preload = 0)
    {
        if (prefab == null)
        {
            return null;
        }

        EnsurePoolRoot();

        if (poolsByPrefab.TryGetValue(prefab, out ObjectPool existingPool))
        {
            return existingPool;
        }

        GameObject container = new GameObject($"{prefab.name}_Pool");
        container.transform.SetParent(poolRoot, false);

        ObjectPool newPool = new ObjectPool(prefab, container.transform, Mathf.Max(0, preload));
        poolsByPrefab.Add(prefab, newPool);
        return newPool;
    }

    private static void EnsurePoolRoot()
    {
        if (poolRoot != null)
        {
            return;
        }

        GameObject rootObject = GameObject.Find("[PoolManager]");
        if (rootObject == null)
        {
            rootObject = new GameObject("[PoolManager]");
            Object.DontDestroyOnLoad(rootObject);
        }

        poolRoot = rootObject.transform;
    }
}