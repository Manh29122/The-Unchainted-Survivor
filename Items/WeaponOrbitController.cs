using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponOrbitController : MonoBehaviour
{
    private sealed class WeaponSlot
    {
        public UnchaintedItemData itemData;
        public WeaponOrbitEffect effect;
        public GameObject visualInstance;
        public float cooldownTimer;
        public int anchorIndex = -1;
        public int equipOrder;
        public Quaternion restLocalRotation = Quaternion.identity;
        public Vector3 baseLocalScale = Vector3.one;
        public Coroutine attackCoroutine;
    }

    [Header("Weapon Slots")]
    [SerializeField] private int maxWeaponSlots = 6;
    [SerializeField] private Transform weaponAnchorRoot;
    [SerializeField] private List<Transform> weaponAnchors = new List<Transform>();

    [Header("Enemy Search")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float fallbackSearchRadius = 20f;

    private readonly List<WeaponSlot> weaponSlots = new List<WeaponSlot>();
    private PlayerStats playerStats;
    private int nextEquipOrder;
    private float lastFlipCompensationX = 0f;

    public int MaxWeaponSlots => Mathf.Max(1, maxWeaponSlots);
    public int EquippedWeaponCount => weaponSlots.Count;

    public static WeaponOrbitController GetOrCreate(GameObject owner)
    {
        if (owner == null)
        {
            return null;
        }

        WeaponOrbitController controller = owner.GetComponent<WeaponOrbitController>();
        if (controller == null)
        {
            controller = owner.AddComponent<WeaponOrbitController>();
        }

        return controller;
    }

    public bool CanEquipMoreWeapons()
    {
        return FindEmptyAnchorIndex() >= 0;
    }

    public bool TryEquipWeapon(UnchaintedItemData itemData, WeaponOrbitEffect effect, out object equippedHandle)
    {
        equippedHandle = null;

        int anchorIndex = FindEmptyAnchorIndex();
        if (effect == null || anchorIndex < 0)
        {
            return false;
        }

        WeaponSlot slot = new WeaponSlot
        {
            itemData = itemData,
            effect = effect,
            cooldownTimer = Mathf.Max(0f, effect.InitialDelaySeconds),
            anchorIndex = anchorIndex,
            equipOrder = nextEquipOrder++
        };

        if (effect.WeaponVisualPrefab != null)
        {
            Transform anchor = GetAnchor(anchorIndex);
            slot.visualInstance = Instantiate(effect.WeaponVisualPrefab, anchor != null ? anchor.position : transform.position, Quaternion.identity, anchor);
            slot.visualInstance.name = $"Weapon_{effect.name}_{anchorIndex}";
            slot.visualInstance.transform.localPosition = Vector3.zero;
            slot.visualInstance.transform.localRotation = Quaternion.identity;
            slot.restLocalRotation = slot.visualInstance.transform.localRotation;
            slot.baseLocalScale = slot.visualInstance.transform.localScale;
        }

        weaponSlots.Add(slot);
        equippedHandle = slot;
        return true;
    }

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        EnsureWeaponAnchors();
    }

    public bool TryGetReplacementCandidate(UnchaintedItemData incomingItemData, out UnchaintedItemData replacementItemData)
    {
        replacementItemData = null;
        if (CanEquipMoreWeapons())
        {
            return false;
        }

        WeaponSlot candidate = null;

        for (int index = 0; index < weaponSlots.Count; index++)
        {
            WeaponSlot slot = weaponSlots[index];
            if (slot == null || slot.itemData == null)
            {
                continue;
            }

            if (incomingItemData != null && slot.itemData == incomingItemData)
            {
                continue;
            }

            if (candidate == null || slot.equipOrder < candidate.equipOrder)
            {
                candidate = slot;
            }
        }

        if (candidate == null)
        {
            return false;
        }

        replacementItemData = candidate.itemData;
        return replacementItemData != null;
    }

    public void UnequipWeapon(object equippedHandle)
    {
        WeaponSlot slot = equippedHandle as WeaponSlot;
        if (slot == null)
        {
            return;
        }

        if (!weaponSlots.Remove(slot))
        {
            return;
        }

        if (slot.visualInstance != null)
        {
            if (slot.attackCoroutine != null)
            {
                StopCoroutine(slot.attackCoroutine);
                slot.attackCoroutine = null;
            }

            Destroy(slot.visualInstance);
        }
    }

    private void LateUpdate()
    {
        SyncAnchorRootFlipCompensation();

        if (weaponSlots.Count == 0)
        {
            return;
        }

        for (int index = 0; index < weaponSlots.Count; index++)
        {
            WeaponSlot slot = weaponSlots[index];
            if (slot == null || slot.effect == null)
            {
                continue;
            }

            SyncWeaponVisualFlip(slot);

            if (slot.attackCoroutine != null)
            {
                continue;
            }

            if (slot.cooldownTimer > 0f)
            {
                slot.cooldownTimer -= Time.deltaTime;
                continue;
            }

            Vector3 weaponOrigin = GetWeaponOrigin(slot);
            Transform target = FindNearestEnemy(weaponOrigin, slot.effect.AttackRange);
            if (target == null)
            {
                RestoreRestRotation(slot);
                continue;
            }

            AimWeaponAtTarget(slot, target);
            FireWeapon(slot, target);
            slot.cooldownTimer = Mathf.Max(0.05f, slot.effect.CooldownSeconds);
        }
    }

    private void FireWeapon(WeaponSlot slot, Transform target)
    {
        if (slot == null || slot.effect == null || target == null)
        {
            return;
        }

        if (slot.effect.WeaponType == OrbitWeaponType.Melee)
        {
            slot.attackCoroutine = StartCoroutine(AnimateMeleeAttack(slot, target));
            return;
        }

        GameObject projectilePrefab = slot.effect.ProjectilePrefab;
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[WeaponOrbitController] Weapon effect '{slot.effect.name}' thiếu projectilePrefab.");
            return;
        }

        Vector3 origin = GetWeaponOrigin(slot);
        Vector2 direction = ((Vector2)(target.position - origin)).normalized;
        if (direction == Vector2.zero)
        {
            direction = Vector2.right;
        }

        GameObject projectileObject = Instantiate(projectilePrefab, origin, Quaternion.identity);
        if (projectileObject == null)
        {
            return;
        }

        WeaponOrbitProjectile projectile = projectileObject.GetComponent<WeaponOrbitProjectile>();
        if (projectile == null)
        {
            projectile = projectileObject.GetComponentInChildren<WeaponOrbitProjectile>(true);
        }

        if (projectile == null)
        {
            Debug.LogWarning("[WeaponOrbitController] Projectile prefab phải gắn WeaponOrbitProjectile.");
            Destroy(projectileObject);
            return;
        }

        projectile.Initialize(direction, slot.effect.ProjectileSpeed, slot.effect.GetDamage(playerStats), slot.effect.ProjectileLifetime);
    }

    private IEnumerator AnimateMeleeAttack(WeaponSlot slot, Transform target)
    {
        if (slot == null)
        {
            yield break;
        }

        Transform visualTransform = slot.visualInstance != null ? slot.visualInstance.transform : null;
        if (visualTransform == null || target == null)
        {
            DealMeleeDamage(slot, target);
            slot.attackCoroutine = null;
            yield break;
        }

        Quaternion restRotation = slot.restLocalRotation;
        float targetAngle = GetAngleToTarget(visualTransform.position, target.position);
        float halfSwing = Mathf.Abs(slot.effect.MeleeSwingAngle) * 0.5f;
        Quaternion swingStart = Quaternion.Euler(0f, 0f, targetAngle - halfSwing);
        Quaternion swingEnd = Quaternion.Euler(0f, 0f, targetAngle + halfSwing);

        float swingDuration = Mathf.Max(0.01f, slot.effect.MeleeSwingDuration);
        float elapsed = 0f;
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / swingDuration);
            visualTransform.rotation = Quaternion.Slerp(swingStart, swingEnd, progress);
            yield return null;
        }

        DealMeleeDamage(slot, target);

        float returnDuration = Mathf.Max(0.01f, slot.effect.MeleeReturnDuration);
        elapsed = 0f;
        Quaternion returnStart = visualTransform.localRotation;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / returnDuration);
            visualTransform.localRotation = Quaternion.Slerp(returnStart, restRotation, progress);
            yield return null;
        }

        visualTransform.localRotation = restRotation;
        slot.attackCoroutine = null;
    }

    private void DealMeleeDamage(WeaponSlot slot, Transform target)
    {
        if (target == null)
        {
            return;
        }

        Vector3 origin = GetWeaponOrigin(slot);
        float meleeHitRange = Mathf.Max(0.05f, slot.effect.MeleeHitRange);
        if (Vector2.Distance(origin, target.position) > meleeHitRange)
        {
            return;
        }

        float damage = slot.effect.GetDamage(playerStats);
        EnemyUnit enemyUnit = target.GetComponentInParent<EnemyUnit>();
        EnemyHealth enemyHealth = enemyUnit == null ? target.GetComponentInParent<EnemyHealth>() : null;
        EnemyHealthBridge enemyHealthBridge = enemyUnit == null && enemyHealth == null
            ? target.GetComponentInParent<EnemyHealthBridge>()
            : null;

        if (enemyUnit != null)
        {
            enemyUnit.TakeDamage(Mathf.RoundToInt(damage));
        }
        else if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }
        else if (enemyHealthBridge != null)
        {
            enemyHealthBridge.TakeDamage(Mathf.RoundToInt(damage));
        }
    }

    private void AimWeaponAtTarget(WeaponSlot slot, Transform target)
    {
        if (slot == null || slot.visualInstance == null || target == null || slot.attackCoroutine != null)
        {
            return;
        }

        float targetAngle = GetAngleToTarget(slot.visualInstance.transform.position, target.position);
        slot.visualInstance.transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
    }

    private void RestoreRestRotation(WeaponSlot slot)
    {
        if (slot == null || slot.visualInstance == null || slot.attackCoroutine != null)
        {
            return;
        }

        slot.visualInstance.transform.localRotation = slot.restLocalRotation;
    }

    private void SyncWeaponVisualFlip(WeaponSlot slot)
    {
        if (slot == null || slot.visualInstance == null)
        {
            return;
        }

        Vector3 localScale = slot.baseLocalScale;
        float baseScaleX = Mathf.Abs(localScale.x);
        float facingScaleX = transform.localScale.x < 0f ? baseScaleX : -baseScaleX;
        localScale.x = Mathf.Approximately(baseScaleX, 0f) ? 0f : facingScaleX;
        slot.visualInstance.transform.localScale = localScale;
    }

    private static float GetAngleToTarget(Vector3 origin, Vector3 target)
    {
        Vector2 direction = (target - origin).normalized;
        if (direction == Vector2.zero)
        {
            return 0f;
        }

        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private void EnsureWeaponAnchors()
    {
        if (weaponAnchorRoot == null)
        {
            Transform existingRoot = transform.Find("WeaponAnchorRoot");
            if (existingRoot != null)
            {
                weaponAnchorRoot = existingRoot;
            }
            else
            {
                GameObject anchorRootObject = new GameObject("WeaponAnchorRoot");
                weaponAnchorRoot = anchorRootObject.transform;
                weaponAnchorRoot.SetParent(transform);
                weaponAnchorRoot.localPosition = Vector3.zero;
                weaponAnchorRoot.localRotation = Quaternion.identity;
            }
        }

        weaponAnchors.RemoveAll(anchor => anchor == null);

        Vector3[] defaultPositions = new Vector3[]
        {
            new Vector3(-0.9f, 0.85f, 0f),
            new Vector3(0f, 1.1f, 0f),
            new Vector3(0.9f, 0.85f, 0f),
            new Vector3(-0.9f, -0.2f, 0f),
            new Vector3(0f, -0.45f, 0f),
            new Vector3(0.9f, -0.2f, 0f)
        };

        int requiredCount = Mathf.Max(1, maxWeaponSlots);
        for (int index = weaponAnchors.Count; index < requiredCount; index++)
        {
            GameObject anchorObject = new GameObject($"WeaponAnchor_{index + 1}");
            Transform anchorTransform = anchorObject.transform;
            anchorTransform.SetParent(weaponAnchorRoot);
            anchorTransform.localPosition = index < defaultPositions.Length ? defaultPositions[index] : Vector3.zero;
            anchorTransform.localRotation = Quaternion.identity;
            weaponAnchors.Add(anchorTransform);
        }
    }

    private void SyncAnchorRootFlipCompensation()
    {
        if (weaponAnchorRoot == null)
        {
            return;
        }

        float compensationX = transform.localScale.x < 0f ? -1f : 1f;
        if (Mathf.Approximately(lastFlipCompensationX, compensationX))
        {
            return;
        }

        Vector3 localScale = weaponAnchorRoot.localScale;
        localScale.x = compensationX;
        weaponAnchorRoot.localScale = localScale;
        lastFlipCompensationX = compensationX;
    }

    private int FindEmptyAnchorIndex()
    {
        EnsureWeaponAnchors();

        int anchorCount = Mathf.Min(MaxWeaponSlots, weaponAnchors.Count);
        for (int anchorIndex = 0; anchorIndex < anchorCount; anchorIndex++)
        {
            bool occupied = false;
            for (int slotIndex = 0; slotIndex < weaponSlots.Count; slotIndex++)
            {
                WeaponSlot slot = weaponSlots[slotIndex];
                if (slot != null && slot.anchorIndex == anchorIndex)
                {
                    occupied = true;
                    break;
                }
            }

            if (!occupied)
            {
                return anchorIndex;
            }
        }

        return -1;
    }

    private Transform GetAnchor(int anchorIndex)
    {
        EnsureWeaponAnchors();
        if (anchorIndex < 0 || anchorIndex >= weaponAnchors.Count)
        {
            return transform;
        }

        return weaponAnchors[anchorIndex] != null ? weaponAnchors[anchorIndex] : transform;
    }

    private Vector3 GetWeaponOrigin(WeaponSlot slot)
    {
        if (slot != null && slot.visualInstance != null)
        {
            return slot.visualInstance.transform.position;
        }

        if (slot != null)
        {
            Transform anchor = GetAnchor(slot.anchorIndex);
            if (anchor != null)
            {
                return anchor.position;
            }
        }

        return transform.position;
    }

    private Transform FindNearestEnemy(Vector3 origin, float attackRange)
    {
        float searchRadius = attackRange > 0f ? attackRange : fallbackSearchRadius;

        if (enemyLayer.value != 0)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, searchRadius, enemyLayer);
            return GetNearestFromColliders(origin, hits, searchRadius);
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        Transform nearest = null;
        float minDistance = searchRadius;

        for (int index = 0; index < enemies.Length; index++)
        {
            GameObject enemy = enemies[index];
            if (enemy == null || !enemy.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector2.Distance(origin, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    private Transform GetNearestFromColliders(Vector3 origin, Collider2D[] hits, float maxDistance)
    {
        Transform nearest = null;
        float minDistance = maxDistance;

        for (int index = 0; index < hits.Length; index++)
        {
            Collider2D hit = hits[index];
            if (hit == null)
            {
                continue;
            }

            float distance = Vector2.Distance(origin, hit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = hit.transform;
            }
        }

        return nearest;
    }
}