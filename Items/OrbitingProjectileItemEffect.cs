using UnityEngine;

[CreateAssetMenu(fileName = "OrbitingProjectileItemEffect", menuName = "Game/Items/Effects/Orbiting Projectile")]
public class OrbitingProjectileItemEffect : ItemEffectBase
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int projectileCount = 3;
    [SerializeField] private float orbitVelocity = 2f;
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float startAngleDegrees;
    [SerializeField] private bool destroyProjectileOnHit;
    [SerializeField] private bool rotateProjectileVisual = true;
    [SerializeField] private bool addRangedDamageBonus = true;

    [Header("Activation")]
    [SerializeField] private float cooldownSeconds;
    [SerializeField] private float activeDurationSeconds;
    [SerializeField] private bool activateImmediately = true;

    [Header("Stack Scaling")]
    [SerializeField] private int additionalProjectileCountPerStack = 1;
    [SerializeField] private bool multiplyProjectileCountByStack;
    [SerializeField] private bool multiplyDamageByStack = true;
    [SerializeField] private bool multiplyOrbitRadiusByStack;
    [SerializeField] private bool multiplyOrbitVelocityByStack;

    public GameObject ProjectilePrefab => projectilePrefab;
    public float StartAngleDegrees => startAngleDegrees;
    public bool DestroyProjectileOnHit => destroyProjectileOnHit;
    public bool RotateProjectileVisual => rotateProjectileVisual;
    public float CooldownSeconds => Mathf.Max(0f, cooldownSeconds);
    public float ActiveDurationSeconds => Mathf.Max(0f, activeDurationSeconds);
    public bool ActivateImmediately => activateImmediately;
    public bool UsesTimedActivation => CooldownSeconds > 0f && ActiveDurationSeconds > 0f;

    public override string GetDescription(UnchaintedItemData itemData, int stackCount)
    {
        int count = GetProjectileCount(stackCount);
        float damage = GetDamage(null, stackCount);

        if (UsesTimedActivation)
        {
            return $"Every {CooldownSeconds:0.##} seconds, summon {count} orbiting projectiles for {ActiveDurationSeconds:0.##} seconds, dealing {damage:0.##} damage";
        }

        return $"Summon {count} orbiting projectiles that deal {damage:0.##} damage";
    }

    public override ItemEffectHandle CreateHandle(EffectContext context, UnchaintedItemData itemData, int stackCount)
    {
        return new OrbitingProjectileItemEffectHandle(this, context, itemData, stackCount);
    }

    public int GetProjectileCount(int stackCount)
    {
        int effectiveStacks = Mathf.Max(1, stackCount);
        int baseProjectileCount = projectileCount > 0
            ? projectileCount
            : Mathf.Max(1, additionalProjectileCountPerStack);
        int count = multiplyProjectileCountByStack
            ? baseProjectileCount * effectiveStacks
            : baseProjectileCount + (Mathf.Max(0, effectiveStacks - 1) * Mathf.Max(0, additionalProjectileCountPerStack));
        return Mathf.Max(1, count);
    }

    private void OnValidate()
    {
        projectileCount = Mathf.Max(1, projectileCount);
        additionalProjectileCountPerStack = Mathf.Max(0, additionalProjectileCountPerStack);
        orbitRadius = Mathf.Max(0.05f, orbitRadius);
        orbitVelocity = Mathf.Max(0f, orbitVelocity);
        baseDamage = Mathf.Max(0f, baseDamage);
        cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
        activeDurationSeconds = Mathf.Max(0f, activeDurationSeconds);
    }

    public float GetOrbitVelocity(int stackCount)
    {
        int effectiveStacks = Mathf.Max(1, stackCount);
        float value = multiplyOrbitVelocityByStack ? orbitVelocity * effectiveStacks : orbitVelocity;
        return Mathf.Max(0f, value);
    }

    public float GetOrbitRadius(int stackCount)
    {
        int effectiveStacks = Mathf.Max(1, stackCount);
        float value = multiplyOrbitRadiusByStack ? orbitRadius * effectiveStacks : orbitRadius;
        return Mathf.Max(0.05f, value);
    }

    public float GetDamage(PlayerStats playerStats, int stackCount)
    {
        int effectiveStacks = Mathf.Max(1, stackCount);
        float damage = multiplyDamageByStack ? baseDamage * effectiveStacks : baseDamage;

        if (playerStats != null && addRangedDamageBonus)
        {
            damage += playerStats.rangedDamage;
        }

        return Mathf.Max(0f, damage);
    }

    private sealed class OrbitingProjectileItemEffectHandle : ItemEffectHandle
    {
        private readonly OrbitingProjectileItemEffect effect;
        private OrbitingProjectileItemController controller;
        private object runtimeHandle;

        public OrbitingProjectileItemEffectHandle(OrbitingProjectileItemEffect effect, EffectContext context, UnchaintedItemData itemData, int initialStackCount)
            : base(effect, context, itemData, initialStackCount)
        {
            this.effect = effect;
        }

        protected override void OnApplied()
        {
            if (Context == null || Context.OwnerObject == null)
            {
                return;
            }

            controller = OrbitingProjectileItemController.GetOrCreate(Context.OwnerObject);
            if (controller == null)
            {
                return;
            }

            controller.RegisterGroup(ItemData, effect, StackCount, out runtimeHandle);
        }

        protected override void OnStackChanged(int previousStackCount, int newStackCount)
        {
            controller?.UpdateGroupStack(runtimeHandle, newStackCount);
        }

        protected override void OnRemoved()
        {
            controller?.UnregisterGroup(runtimeHandle);
            runtimeHandle = null;
        }

        protected override void OnPaused()
        {
            controller?.SetPaused(true);
        }

        protected override void OnResumed()
        {
            controller?.SetPaused(false);
        }
    }
}