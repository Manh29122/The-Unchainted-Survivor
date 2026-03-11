using UnityEngine;
using System.Collections.Generic;

public enum OrbitWeaponType
{
    Melee,
    Ranged
}

[CreateAssetMenu(fileName = "WeaponOrbitEffect", menuName = "Game/Items/Effects/Weapon Orbit")]
public class WeaponOrbitEffect : ItemEffectBase
{
    [Header("Weapon Visual")]
    [SerializeField] private GameObject weaponVisualPrefab;

    [Header("Attack")]
    [SerializeField] private OrbitWeaponType weaponType = OrbitWeaponType.Ranged;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float cooldownSeconds = 1f;
    [SerializeField] private float initialDelaySeconds;
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float currentDamagePercent;

    [Header("Melee Swing")]
    [SerializeField] private float meleeSwingAngle = 40f;
    [SerializeField] private float meleeSwingDuration = 0.12f;
    [SerializeField] private float meleeReturnDuration = 0.12f;
    [SerializeField] private float meleeHitRange = 1.25f;

    public GameObject WeaponVisualPrefab => weaponVisualPrefab;
    public OrbitWeaponType WeaponType => weaponType;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float CooldownSeconds => cooldownSeconds;
    public float InitialDelaySeconds => initialDelaySeconds;
    public float AttackRange => attackRange;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileLifetime => projectileLifetime;
    public float MeleeSwingAngle => meleeSwingAngle;
    public float MeleeSwingDuration => meleeSwingDuration;
    public float MeleeReturnDuration => meleeReturnDuration;
    public float MeleeHitRange => meleeHitRange;

    public override string GetDescription(UnchaintedItemData itemData, int stackCount)
    {
        string weaponTypeText = weaponType == OrbitWeaponType.Melee ? "melee" : "ranged";
        return $"Equip this {weaponTypeText} weapon around the player. Max 6 weapons";
    }

    public float GetDamage(PlayerStats playerStats)
    {
        float damage = Mathf.Max(0f, baseDamage);
        if (playerStats != null)
        {
            damage += weaponType == OrbitWeaponType.Melee ? playerStats.meleeDamage : playerStats.rangedDamage;
        }

        if (playerStats != null && currentDamagePercent > 0f)
        {
            damage += playerStats.currentHP * (currentDamagePercent / 100f);
        }

        return Mathf.Max(1f, damage);
    }

    public override ItemEffectHandle CreateHandle(EffectContext context, UnchaintedItemData itemData, int stackCount)
    {
        return new WeaponOrbitEffectHandle(this, context, itemData, stackCount);
    }

    private sealed class WeaponOrbitEffectHandle : ItemEffectHandle
    {
        private readonly WeaponOrbitEffect effect;
        private WeaponOrbitController controller;
        private readonly List<object> equippedHandles = new List<object>();

        public WeaponOrbitEffectHandle(WeaponOrbitEffect effect, EffectContext context, UnchaintedItemData itemData, int initialStackCount)
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

            controller = WeaponOrbitController.GetOrCreate(Context.OwnerObject);
            if (controller == null)
            {
                return;
            }

            SyncEquippedWeapons(StackCount);
        }

        protected override void OnStackChanged(int previousStackCount, int newStackCount)
        {
            SyncEquippedWeapons(newStackCount);
        }

        protected override void OnRemoved()
        {
            if (controller == null)
            {
                equippedHandles.Clear();
                return;
            }

            for (int index = equippedHandles.Count - 1; index >= 0; index--)
            {
                controller.UnequipWeapon(equippedHandles[index]);
            }

            equippedHandles.Clear();
        }

        private void SyncEquippedWeapons(int desiredCount)
        {
            if (controller == null)
            {
                return;
            }

            int clampedDesiredCount = Mathf.Max(0, desiredCount);

            while (equippedHandles.Count < clampedDesiredCount)
            {
                if (!controller.TryEquipWeapon(ItemData, effect, out object equippedHandle))
                {
                    Debug.LogWarning("[WeaponOrbitEffect] Không thể gắn thêm vũ khí vào 1 trong 6 slot hiện có.");
                    break;
                }

                equippedHandles.Add(equippedHandle);
            }

            while (equippedHandles.Count > clampedDesiredCount)
            {
                int lastIndex = equippedHandles.Count - 1;
                controller.UnequipWeapon(equippedHandles[lastIndex]);
                equippedHandles.RemoveAt(lastIndex);
            }
        }
    }
}