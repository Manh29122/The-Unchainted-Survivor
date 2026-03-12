using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "MeteorShowerEffect", menuName = "Game/Items/Effects/Meteor Shower")]
public class MeteorShowerEffect : ItemEffectBase
{
    [Header("Meteor")]
    [SerializeField] private GameObject meteorPrefab;
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private float intervalSeconds = 5f;
    [SerializeField] private int meteorCount = 3;
    [SerializeField] private float baseDamage = 25f;
    [SerializeField] private float impactRadius = 1.5f;
    [SerializeField] private float fallSpeed = 12f;
    [SerializeField] private float fallAngleDegrees = -60f;
    [SerializeField] private float spawnDistance = 8f;
    [SerializeField] private float impactLifetime = 1.2f;
    [SerializeField] private bool addRangedDamageBonus = true;

    [Header("Screen Region")]
    [SerializeField, Range(0f, 1f)] private float minViewportX = 0.1f;
    [SerializeField, Range(0f, 1f)] private float maxViewportX = 0.9f;
    [SerializeField, Range(0f, 1f)] private float minViewportY = 0.1f;
    [SerializeField, Range(0f, 1f)] private float maxViewportY = 0.9f;

    [Header("Stack Scaling")]
    [SerializeField] private int additionalMeteorCountPerStack = 1;
    [SerializeField] private bool multiplyMeteorCountByStack;
    [SerializeField] private bool multiplyDamageByStack = true;
    [SerializeField] private bool multiplyImpactRadiusByStack;

    public override string GetDescription(UnchaintedItemData itemData, int stackCount)
    {
        return $"Every {Mathf.Max(0.05f, intervalSeconds):0.##} seconds, call {GetMeteorCount(stackCount)} meteors that impact random screen positions for {GetDamage(null, stackCount):0.##} damage";
    }

    public override ItemEffectHandle CreateHandle(EffectContext context, UnchaintedItemData itemData, int stackCount)
    {
        return new MeteorShowerEffectHandle(this, context, itemData, stackCount);
    }

    public int GetMeteorCount(int stackCount)
    {
        int effectiveStacks = Mathf.Max(1, stackCount);
        int baseMeteorCount = Mathf.Max(1, meteorCount);
        int count = multiplyMeteorCountByStack
            ? baseMeteorCount * effectiveStacks
            : baseMeteorCount + (Mathf.Max(0, effectiveStacks - 1) * Mathf.Max(0, additionalMeteorCountPerStack));
        return Mathf.Max(1, count);
    }

    public float GetDamage(PlayerStats playerStats, int stackCount)
    {
        int effectiveStacks = Mathf.Max(1, stackCount);
        float damage = multiplyDamageByStack ? baseDamage * effectiveStacks : baseDamage;

        if (playerStats != null)
        {
            if (addRangedDamageBonus)
            {
                damage += playerStats.rangedDamage;
            }
        }

        return Mathf.Max(0f, damage);
    }

    public float GetImpactRadius(int stackCount)
    {
        int effectiveStacks = Mathf.Max(1, stackCount);
        float radius = multiplyImpactRadiusByStack ? impactRadius * effectiveStacks : impactRadius;
        return Mathf.Max(0.05f, radius);
    }

    private void OnValidate()
    {
        intervalSeconds = Mathf.Max(0.05f, intervalSeconds);
        meteorCount = Mathf.Max(1, meteorCount);
        additionalMeteorCountPerStack = Mathf.Max(0, additionalMeteorCountPerStack);
        baseDamage = Mathf.Max(0f, baseDamage);
        impactRadius = Mathf.Max(0.05f, impactRadius);
        fallSpeed = Mathf.Max(0.05f, fallSpeed);
        spawnDistance = Mathf.Max(0.25f, spawnDistance);
        impactLifetime = Mathf.Max(0.05f, impactLifetime);

        minViewportX = Mathf.Clamp01(minViewportX);
        maxViewportX = Mathf.Clamp01(maxViewportX);
        minViewportY = Mathf.Clamp01(minViewportY);
        maxViewportY = Mathf.Clamp01(maxViewportY);

        if (maxViewportX < minViewportX)
        {
            maxViewportX = minViewportX;
        }

        if (maxViewportY < minViewportY)
        {
            maxViewportY = minViewportY;
        }
    }

    private void SpawnMeteor(EffectContext context, int stackCount)
    {
        if (context == null || context.OwnerTransform == null || meteorPrefab == null)
        {
            return;
        }

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            Debug.LogWarning("[MeteorShowerEffect] Không tìm thấy Camera.main để lấy vùng màn hình.");
            return;
        }

        float targetX = Random.Range(minViewportX, maxViewportX);
        float targetY = Random.Range(minViewportY, maxViewportY);
        Vector3 impactPosition = ViewportToWorldPoint(targetCamera, new Vector2(targetX, targetY), context.OwnerTransform.position.z);

        Vector2 travelDirection = Quaternion.Euler(0f, 0f, fallAngleDegrees) * Vector2.right;
        if (travelDirection == Vector2.zero)
        {
            travelDirection = new Vector2(1f, -1f).normalized;
        }

        Vector3 spawnPosition = impactPosition - (Vector3)(travelDirection.normalized * spawnDistance);
        GameObject meteorObject = PoolManager.Spawn(meteorPrefab, spawnPosition, Quaternion.identity, Mathf.Max(4, GetMeteorCount(stackCount)));
        if (meteorObject == null)
        {
            return;
        }

        MeteorShowerProjectile projectile = meteorObject.GetComponent<MeteorShowerProjectile>();
        if (projectile == null)
        {
            projectile = meteorObject.GetComponentInChildren<MeteorShowerProjectile>(true);
        }

        if (projectile == null)
        {
            projectile = meteorObject.AddComponent<MeteorShowerProjectile>();
        }

        projectile.Initialize(
            impactPosition,
            travelDirection,
            fallSpeed,
            GetDamage(context.PlayerStats, stackCount),
            GetImpactRadius(stackCount),
            impactPrefab,
            impactLifetime,
            context.PlayerStats);
    }

    private static Vector3 ViewportToWorldPoint(Camera targetCamera, Vector2 viewportPoint, float worldPlaneZ)
    {
        float cameraDistance = Mathf.Abs(targetCamera.transform.position.z - worldPlaneZ);
        Vector3 worldPoint = targetCamera.ViewportToWorldPoint(new Vector3(viewportPoint.x, viewportPoint.y, cameraDistance));
        worldPoint.z = worldPlaneZ;
        return worldPoint;
    }

    private sealed class MeteorShowerEffectHandle : ItemEffectHandle
    {
        private readonly MeteorShowerEffect effect;
        private MonoBehaviour coroutineHost;
        private Coroutine spawnCoroutine;

        public MeteorShowerEffectHandle(MeteorShowerEffect effect, EffectContext context, UnchaintedItemData itemData, int initialStackCount)
            : base(effect, context, itemData, initialStackCount)
        {
            this.effect = effect;
        }

        protected override void OnApplied()
        {
            coroutineHost = Context != null ? Context.CoroutineHost : null;
            if (coroutineHost == null)
            {
                return;
            }

            spawnCoroutine = coroutineHost.StartCoroutine(SpawnLoop());
        }

        protected override void OnStackChanged(int previousStackCount, int newStackCount)
        {
        }

        protected override void OnRemoved()
        {
            StopLoop();
        }

        protected override void OnPaused()
        {
            StopLoop();
        }

        protected override void OnResumed()
        {
            if (coroutineHost == null || spawnCoroutine != null)
            {
                return;
            }

            spawnCoroutine = coroutineHost.StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            WaitForSeconds wait = new WaitForSeconds(effect.intervalSeconds);

            while (true)
            {
                yield return wait;

                if (Context == null || Context.OwnerTransform == null)
                {
                    continue;
                }

                int count = effect.GetMeteorCount(StackCount);
                for (int index = 0; index < count; index++)
                {
                    effect.SpawnMeteor(Context, StackCount);
                }
            }
        }

        private void StopLoop()
        {
            if (coroutineHost != null && spawnCoroutine != null)
            {
                coroutineHost.StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }
    }
}