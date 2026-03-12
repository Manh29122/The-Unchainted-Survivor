using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "FireballBurstCurrentHpEffect", menuName = "Game/Items/Effects/Fireball Burst Current HP")]
public class FireballBurstCurrentHpEffect : ItemEffectBase
{
	[Header("Fireball Burst")]
	[SerializeField] private GameObject projectilePrefab;
	[SerializeField] private float intervalSeconds = 5f;
	[SerializeField] private int projectileCount = 8;
	[SerializeField] private float damagePercentOfCurrentHp = 50f;
	[SerializeField] private float projectileSpeed = 8f;
	[SerializeField] private float projectileLifetime = 3f;
	[SerializeField] private float spawnOffset = 0.35f;
	[SerializeField] private bool multiplyDamagePercentByStack;

	public override string GetDescription(UnchaintedItemData itemData, int stackCount)
	{
		int count = Mathf.Max(1, projectileCount);
		float damagePercent = GetEffectiveDamagePercent(stackCount);
		return $"Every {intervalSeconds:0.##} seconds, unleash {count} fireballs in 8 directions, dealing damage equal to {damagePercent:0.##}% of current HP";
	}

	public override ItemEffectHandle CreateHandle(EffectContext context, UnchaintedItemData itemData, int stackCount)
	{
		return new FireballBurstCurrentHpEffectHandle(this, context, itemData, stackCount);
	}

	private float GetEffectiveDamagePercent(int stackCount)
	{
		int effectiveStacks = Mathf.Max(1, stackCount);
		float value = multiplyDamagePercentByStack ? damagePercentOfCurrentHp * effectiveStacks : damagePercentOfCurrentHp;
		return Mathf.Max(0f, value);
	}

	private void FireBurst(EffectContext context, int stackCount)
	{
		if (context == null || context.OwnerTransform == null || context.PlayerStats == null)
		{
			return;
		}

		if (projectilePrefab == null)
		{
			Debug.LogWarning("[FireballBurstCurrentHpEffect] Chưa gán projectilePrefab.");
			return;
		}

		float damage = Mathf.Max(1f, context.PlayerStats.currentHP * (GetEffectiveDamagePercent(stackCount) / 100f));
		int count = Mathf.Max(1, projectileCount);
		float angleStep = 360f / count;

		for (int index = 0; index < count; index++)
		{
			float angle = angleStep * index;
			Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
			SpawnProjectile(context.OwnerTransform.position, direction, damage, context.PlayerStats);
		}
	}

	private void SpawnProjectile(Vector3 origin, Vector2 direction, float damage, PlayerStats playerStats)
	{
		Vector3 spawnPosition = origin + (Vector3)(direction.normalized * spawnOffset);
		GameObject projectileObject = PoolManager.Spawn(projectilePrefab, spawnPosition, Quaternion.identity, 8);
		if (projectileObject == null)
		{
			Debug.LogWarning("[FireballBurstCurrentHpEffect] Instantiate projectilePrefab thất bại.");
			return;
		}

		FireballBurstProjectile projectile = projectileObject.GetComponent<FireballBurstProjectile>();
		if (projectile == null)
		{
			projectile = projectileObject.GetComponentInChildren<FireballBurstProjectile>(true);
		}

		if (projectile == null)
		{
			Debug.LogWarning("[FireballBurstCurrentHpEffect] Fire Ball prefab phải gắn FireballBurstProjectile.");
			if (!PoolManager.Return(projectileObject))
			{
				Destroy(projectileObject);
			}
			return;
		}

		projectile.Initialize(direction, projectileSpeed, damage, projectileLifetime);
		projectile.SetOwner(playerStats);
	}

	private sealed class FireballBurstCurrentHpEffectHandle : ItemEffectHandle
	{
		private readonly FireballBurstCurrentHpEffect effect;
		private MonoBehaviour coroutineHost;
		private Coroutine firingCoroutine;

		public FireballBurstCurrentHpEffectHandle(FireballBurstCurrentHpEffect effect, EffectContext context, UnchaintedItemData itemData, int initialStackCount)
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

			firingCoroutine = coroutineHost.StartCoroutine(FiringLoop());
		}

		protected override void OnStackChanged(int previousStackCount, int newStackCount)
		{
		}

		protected override void OnRemoved()
		{
			StopFiringLoop();
		}

		protected override void OnPaused()
		{
			StopFiringLoop();
		}

		protected override void OnResumed()
		{
			if (coroutineHost == null || firingCoroutine != null)
			{
				return;
			}

			firingCoroutine = coroutineHost.StartCoroutine(FiringLoop());
		}

		private IEnumerator FiringLoop()
		{
			WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.05f, effect.intervalSeconds));

			while (true)
			{
				yield return wait;

				if (Context == null || Context.PlayerStats == null || Context.OwnerTransform == null)
				{
					continue;
				}

				effect.FireBurst(Context, StackCount);
			}
		}

		private void StopFiringLoop()
		{
			if (coroutineHost != null && firingCoroutine != null)
			{
				coroutineHost.StopCoroutine(firingCoroutine);
				firingCoroutine = null;
			}
		}
	}
}
