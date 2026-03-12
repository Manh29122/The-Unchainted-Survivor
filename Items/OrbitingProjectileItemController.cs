using System.Collections.Generic;
using UnityEngine;

public class OrbitingProjectileItemController : MonoBehaviour
{
    private sealed class ProjectileRuntime
    {
        public GameObject instance;
        public ProjectileDamage damageComponent;
    }

    private sealed class OrbitGroup
    {
        public object handle;
        public UnchaintedItemData itemData;
        public OrbitingProjectileItemEffect effect;
        public readonly List<ProjectileRuntime> projectiles = new List<ProjectileRuntime>();
        public int stackCount;
        public float rotationRadians;
        public bool isActive;
        public float activeTimer;
        public float cooldownTimer;
    }

    private readonly List<OrbitGroup> groups = new List<OrbitGroup>();
    private PlayerStats playerStats;
    private bool isPaused;

    public static OrbitingProjectileItemController GetOrCreate(GameObject owner)
    {
        if (owner == null)
        {
            return null;
        }

        OrbitingProjectileItemController controller = owner.GetComponent<OrbitingProjectileItemController>();
        if (controller == null)
        {
            controller = owner.AddComponent<OrbitingProjectileItemController>();
        }

        return controller;
    }

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (isPaused)
        {
            return;
        }

        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }

        for (int index = groups.Count - 1; index >= 0; index--)
        {
            OrbitGroup group = groups[index];
            if (group == null || group.effect == null)
            {
                continue;
            }

            UpdateGroup(group);
        }
    }

    public void RegisterGroup(UnchaintedItemData itemData, OrbitingProjectileItemEffect effect, int stackCount, out object runtimeHandle)
    {
        OrbitGroup group = new OrbitGroup
        {
            handle = new object(),
            itemData = itemData,
            effect = effect,
            stackCount = Mathf.Max(0, stackCount),
            rotationRadians = effect.StartAngleDegrees * Mathf.Deg2Rad,
            isActive = false,
            activeTimer = 0f,
            cooldownTimer = 0f
        };

        groups.Add(group);
        runtimeHandle = group.handle;

        if (!effect.UsesTimedActivation)
        {
            ActivateGroup(group);
            return;
        }

        group.cooldownTimer = effect.ActivateImmediately ? 0f : effect.CooldownSeconds;
        if (effect.ActivateImmediately)
        {
            ActivateGroup(group);
        }
    }

    public void UpdateGroupStack(object runtimeHandle, int stackCount)
    {
        OrbitGroup group = FindGroup(runtimeHandle);
        if (group == null)
        {
            return;
        }

        group.stackCount = Mathf.Max(0, stackCount);

        if (group.isActive)
        {
            SyncProjectiles(group);
        }
    }

    public void UnregisterGroup(object runtimeHandle)
    {
        OrbitGroup group = FindGroup(runtimeHandle);
        if (group == null)
        {
            return;
        }

        DeactivateGroup(group);
        groups.Remove(group);
    }

    public void SetPaused(bool paused)
    {
        if (isPaused == paused)
        {
            return;
        }

        isPaused = paused;

        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            OrbitGroup group = groups[groupIndex];
            if (group == null)
            {
                continue;
            }

            for (int projectileIndex = 0; projectileIndex < group.projectiles.Count; projectileIndex++)
            {
                ProjectileRuntime projectile = group.projectiles[projectileIndex];
                if (projectile != null && projectile.instance != null)
                {
                    projectile.instance.SetActive(!paused);
                }
            }
        }
    }

    private void UpdateGroup(OrbitGroup group)
    {
        if (!group.effect.UsesTimedActivation)
        {
            if (!group.isActive)
            {
                ActivateGroup(group);
            }

            UpdateActiveGroup(group);
            return;
        }

        if (group.cooldownTimer > 0f)
        {
            group.cooldownTimer -= Time.deltaTime;
        }

        if (group.isActive)
        {
            group.activeTimer -= Time.deltaTime;
            UpdateActiveGroup(group);

            if (group.activeTimer <= 0f)
            {
                DeactivateGroup(group);
            }

            return;
        }

        if (group.cooldownTimer <= 0f)
        {
            ActivateGroup(group);
        }
    }

    private void ActivateGroup(OrbitGroup group)
    {
        group.isActive = true;
        group.activeTimer = group.effect.UsesTimedActivation ? group.effect.ActiveDurationSeconds : 0f;
        group.cooldownTimer = group.effect.CooldownSeconds;
        SyncProjectiles(group);
    }

    private void DeactivateGroup(OrbitGroup group)
    {
        group.isActive = false;
        group.activeTimer = 0f;

        for (int index = group.projectiles.Count - 1; index >= 0; index--)
        {
            ProjectileRuntime projectile = group.projectiles[index];
            if (projectile != null && projectile.instance != null)
            {
                if (!PoolManager.Return(projectile.instance))
                {
                    Destroy(projectile.instance);
                }
            }
        }

        group.projectiles.Clear();
    }

    private void UpdateActiveGroup(OrbitGroup group)
    {
        SyncProjectiles(group);

        int projectileCount = group.projectiles.Count;
        if (projectileCount <= 0)
        {
            return;
        }

        float velocity = group.effect.GetOrbitVelocity(group.stackCount);
        float radius = group.effect.GetOrbitRadius(group.stackCount);
        float damage = group.effect.GetDamage(playerStats, group.stackCount);

        group.rotationRadians += velocity * Time.deltaTime;
        float spacing = (Mathf.PI * 2f) / projectileCount;

        for (int index = 0; index < projectileCount; index++)
        {
            ProjectileRuntime projectile = group.projectiles[index];
            if (projectile == null || projectile.instance == null)
            {
                continue;
            }

            float angle = group.rotationRadians + (spacing * index);
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            projectile.instance.transform.position = transform.position + offset;

            if (group.effect.RotateProjectileVisual)
            {
                projectile.instance.transform.Rotate(0f, 0f, velocity * Mathf.Rad2Deg * Time.deltaTime, Space.Self);
            }

            if (projectile.damageComponent != null)
            {
                projectile.damageComponent.SetDamage(damage);
                projectile.damageComponent.SetOwner(playerStats);
                projectile.damageComponent.SetDestroyOnHit(group.effect.DestroyProjectileOnHit);
            }
        }
    }

    private void SyncProjectiles(OrbitGroup group)
    {
        for (int index = group.projectiles.Count - 1; index >= 0; index--)
        {
            if (group.projectiles[index] == null || group.projectiles[index].instance == null)
            {
                group.projectiles.RemoveAt(index);
            }
        }

        int desiredCount = group.effect.GetProjectileCount(group.stackCount);
        while (group.projectiles.Count < desiredCount)
        {
            ProjectileRuntime projectile = CreateProjectile(group);
            if (projectile == null)
            {
                break;
            }

            group.projectiles.Add(projectile);
        }

        while (group.projectiles.Count > desiredCount)
        {
            int lastIndex = group.projectiles.Count - 1;
            ProjectileRuntime projectile = group.projectiles[lastIndex];
            if (projectile != null && projectile.instance != null)
            {
                if (!PoolManager.Return(projectile.instance))
                {
                    Destroy(projectile.instance);
                }
            }

            group.projectiles.RemoveAt(lastIndex);
        }
    }

    private ProjectileRuntime CreateProjectile(OrbitGroup group)
    {
        if (group.effect.ProjectilePrefab == null)
        {
            Debug.LogWarning($"[OrbitingProjectileItemController] Effect '{group.effect.name}' thiếu projectilePrefab.");
            return null;
        }

        GameObject projectileObject = PoolManager.Spawn(group.effect.ProjectilePrefab, transform.position, Quaternion.identity, 8);
        if (projectileObject == null)
        {
            return null;
        }

        ProjectileDamage damageComponent = projectileObject.GetComponent<ProjectileDamage>();
        if (damageComponent == null)
        {
            damageComponent = projectileObject.GetComponentInChildren<ProjectileDamage>(true);
        }

        if (damageComponent == null)
        {
            damageComponent = projectileObject.AddComponent<ProjectileDamage>();
        }

        damageComponent.SetOwner(playerStats);
        damageComponent.SetDestroyOnHit(group.effect.DestroyProjectileOnHit);

        return new ProjectileRuntime
        {
            instance = projectileObject,
            damageComponent = damageComponent
        };
    }

    private OrbitGroup FindGroup(object runtimeHandle)
    {
        if (runtimeHandle == null)
        {
            return null;
        }

        for (int index = 0; index < groups.Count; index++)
        {
            if (ReferenceEquals(groups[index].handle, runtimeHandle))
            {
                return groups[index];
            }
        }

        return null;
    }
}