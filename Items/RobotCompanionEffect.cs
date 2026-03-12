using UnityEngine;

[CreateAssetMenu(fileName = "RobotCompanionEffect", menuName = "Game/Items/Effects/Robot Companion")]
public class RobotCompanionEffect : ItemEffectBase
{
    [Header("Robot")]
    [SerializeField] private GameObject robotPrefab;
    [SerializeField] private int robotCount = 1;
    [SerializeField] private float followRadius = 1.8f;
    [SerializeField] private float followMoveSpeed = 8f;
    [SerializeField] private float followSmoothness = 10f;

    [Header("Attack")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRange = 20f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float knockbackForce;
    [SerializeField] private float knockbackDuration = 0.15f;
    [SerializeField] private bool addRangedDamageBonus = true;

    [Header("Stack Scaling")]
    [SerializeField] private int additionalRobotsPerStack = 1;
    [SerializeField] private bool multiplyRobotCountByStack;
    [SerializeField] private bool multiplyDamageByStack = true;
    [SerializeField] private bool reduceCooldownByStack;
    [SerializeField] private float cooldownReductionPerStack = 0.05f;

    public GameObject RobotPrefab => robotPrefab;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float FollowRadius => Mathf.Max(0.1f, followRadius);
    public float FollowMoveSpeed => Mathf.Max(0.1f, followMoveSpeed);
    public float FollowSmoothness => Mathf.Max(0.1f, followSmoothness);
    public float AttackRange => Mathf.Max(0.5f, attackRange);
    public float ProjectileSpeed => Mathf.Max(0.1f, projectileSpeed);
    public float ProjectileLifetime => Mathf.Max(0.05f, projectileLifetime);
    public float KnockbackDuration => Mathf.Max(0f, knockbackDuration);

    public override string GetDescription(UnchaintedItemData itemData, int stackCount)
    {
        return $"Summon {GetRobotCount(stackCount)} robot companion(s) that follow the player and fire at random enemies on screen";
    }

    public override ItemEffectHandle CreateHandle(EffectContext context, UnchaintedItemData itemData, int stackCount)
    {
        return new RobotCompanionEffectHandle(this, context, itemData, stackCount);
    }

    public int GetRobotCount(int stackCount)
    {
        int effectiveStacks = Mathf.Max(1, stackCount);
        int baseRobotCount = Mathf.Max(1, robotCount);
        int count = multiplyRobotCountByStack
            ? baseRobotCount * effectiveStacks
            : baseRobotCount + (Mathf.Max(0, effectiveStacks - 1) * Mathf.Max(0, additionalRobotsPerStack));
        return Mathf.Max(1, count);
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

    public float GetAttackCooldown(int stackCount)
    {
        int effectiveStacks = Mathf.Max(1, stackCount);
        float cooldown = attackCooldown;
        if (reduceCooldownByStack)
        {
            cooldown -= Mathf.Max(0, effectiveStacks - 1) * Mathf.Max(0f, cooldownReductionPerStack);
        }

        return Mathf.Max(0.05f, cooldown);
    }

    public float GetKnockbackForce(PlayerStats playerStats)
    {
        float playerKnockback = playerStats != null ? Mathf.Max(0f, playerStats.knockback) : 0f;
        return Mathf.Max(0f, knockbackForce + playerKnockback);
    }

    private void OnValidate()
    {
        robotCount = Mathf.Max(1, robotCount);
        additionalRobotsPerStack = Mathf.Max(0, additionalRobotsPerStack);
        followRadius = Mathf.Max(0.1f, followRadius);
        followMoveSpeed = Mathf.Max(0.1f, followMoveSpeed);
        followSmoothness = Mathf.Max(0.1f, followSmoothness);
        attackCooldown = Mathf.Max(0.05f, attackCooldown);
        attackRange = Mathf.Max(0.5f, attackRange);
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        projectileLifetime = Mathf.Max(0.05f, projectileLifetime);
        baseDamage = Mathf.Max(0f, baseDamage);
        knockbackForce = Mathf.Max(0f, knockbackForce);
        knockbackDuration = Mathf.Max(0f, knockbackDuration);
        cooldownReductionPerStack = Mathf.Max(0f, cooldownReductionPerStack);
    }

    private sealed class RobotCompanionEffectHandle : ItemEffectHandle
    {
        private readonly RobotCompanionEffect effect;
        private RobotCompanionController controller;
        private object runtimeHandle;

        public RobotCompanionEffectHandle(RobotCompanionEffect effect, EffectContext context, UnchaintedItemData itemData, int initialStackCount)
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

            controller = RobotCompanionController.GetOrCreate(Context.OwnerObject);
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