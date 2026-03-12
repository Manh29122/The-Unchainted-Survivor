using System.Collections.Generic;
using UnityEngine;

public class RobotCompanionController : MonoBehaviour
{
    private sealed class RobotRuntime
    {
        public GameObject instance;
        public float cooldownTimer;
    }

    private sealed class RobotGroup
    {
        public object handle;
        public UnchaintedItemData itemData;
        public RobotCompanionEffect effect;
        public readonly List<RobotRuntime> robots = new List<RobotRuntime>();
        public int stackCount;
        public float rotationOffset;
    }

    [Header("Enemy Search")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float screenMargin = 0.05f;

    private readonly List<RobotGroup> groups = new List<RobotGroup>();
    private readonly List<Transform> visibleEnemyBuffer = new List<Transform>();
    private PlayerStats playerStats;
    private bool isPaused;

    public static RobotCompanionController GetOrCreate(GameObject owner)
    {
        if (owner == null)
        {
            return null;
        }

        RobotCompanionController controller = owner.GetComponent<RobotCompanionController>();
        if (controller == null)
        {
            controller = owner.AddComponent<RobotCompanionController>();
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
            RobotGroup group = groups[index];
            if (group == null || group.effect == null)
            {
                continue;
            }

            UpdateGroup(group);
        }
    }

    public void RegisterGroup(UnchaintedItemData itemData, RobotCompanionEffect effect, int stackCount, out object runtimeHandle)
    {
        RobotGroup group = new RobotGroup
        {
            handle = new object(),
            itemData = itemData,
            effect = effect,
            stackCount = Mathf.Max(1, stackCount),
            rotationOffset = Random.Range(0f, Mathf.PI * 2f)
        };

        groups.Add(group);
        runtimeHandle = group.handle;
        SyncRobots(group);
    }

    public void UpdateGroupStack(object runtimeHandle, int stackCount)
    {
        RobotGroup group = FindGroup(runtimeHandle);
        if (group == null)
        {
            return;
        }

        group.stackCount = Mathf.Max(1, stackCount);
        SyncRobots(group);
    }

    public void UnregisterGroup(object runtimeHandle)
    {
        RobotGroup group = FindGroup(runtimeHandle);
        if (group == null)
        {
            return;
        }

        ReleaseRobots(group);
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
            RobotGroup group = groups[groupIndex];
            if (group == null)
            {
                continue;
            }

            for (int robotIndex = 0; robotIndex < group.robots.Count; robotIndex++)
            {
                RobotRuntime robot = group.robots[robotIndex];
                if (robot != null && robot.instance != null)
                {
                    robot.instance.SetActive(!paused);
                }
            }
        }
    }

    private void UpdateGroup(RobotGroup group)
    {
        SyncRobots(group);

        int robotCount = group.robots.Count;
        if (robotCount <= 0)
        {
            return;
        }

        float spacing = (Mathf.PI * 2f) / robotCount;
        group.rotationOffset += Time.deltaTime * 0.5f;

        for (int index = 0; index < robotCount; index++)
        {
            RobotRuntime robot = group.robots[index];
            if (robot == null || robot.instance == null)
            {
                continue;
            }

            float angle = group.rotationOffset + spacing * index;
            Vector3 desiredPosition = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * group.effect.FollowRadius;
            Transform robotTransform = robot.instance.transform;
            Vector3 toDesired = desiredPosition - robotTransform.position;
            float moveStep = group.effect.FollowMoveSpeed * Time.deltaTime;
            Vector3 movedPosition = toDesired.sqrMagnitude <= moveStep * moveStep
                ? desiredPosition
                : robotTransform.position + toDesired.normalized * moveStep;
            robotTransform.position = Vector3.Lerp(robotTransform.position, movedPosition, Mathf.Clamp01(group.effect.FollowSmoothness * Time.deltaTime));

            robot.cooldownTimer -= Time.deltaTime;
            if (robot.cooldownTimer > 0f)
            {
                continue;
            }

            Transform target = FindRandomVisibleEnemy(robotTransform.position, group.effect.AttackRange);
            if (target == null)
            {
                continue;
            }

            FireProjectile(group, robotTransform.position, target.position);
            robot.cooldownTimer = group.effect.GetAttackCooldown(group.stackCount);
        }
    }

    private void SyncRobots(RobotGroup group)
    {
        for (int index = group.robots.Count - 1; index >= 0; index--)
        {
            if (group.robots[index] == null || group.robots[index].instance == null)
            {
                group.robots.RemoveAt(index);
            }
        }

        int desiredCount = group.effect.GetRobotCount(group.stackCount);
        while (group.robots.Count < desiredCount)
        {
            GameObject robotObject = SpawnRobot(group.effect.RobotPrefab);
            if (robotObject == null)
            {
                break;
            }

            group.robots.Add(new RobotRuntime
            {
                instance = robotObject,
                cooldownTimer = Random.Range(0f, Mathf.Max(0.05f, group.effect.GetAttackCooldown(group.stackCount)))
            });
        }

        while (group.robots.Count > desiredCount)
        {
            int lastIndex = group.robots.Count - 1;
            RobotRuntime robot = group.robots[lastIndex];
            if (robot != null && robot.instance != null)
            {
                ReturnObject(robot.instance);
            }

            group.robots.RemoveAt(lastIndex);
        }
    }

    private void ReleaseRobots(RobotGroup group)
    {
        for (int index = group.robots.Count - 1; index >= 0; index--)
        {
            RobotRuntime robot = group.robots[index];
            if (robot != null && robot.instance != null)
            {
                ReturnObject(robot.instance);
            }
        }

        group.robots.Clear();
    }

    private GameObject SpawnRobot(GameObject robotPrefab)
    {
        if (robotPrefab == null)
        {
            return null;
        }

        return PoolManager.Spawn(robotPrefab, transform.position, Quaternion.identity, 4);
    }

    private Transform FindRandomVisibleEnemy(Vector3 origin, float attackRange)
    {
        visibleEnemyBuffer.Clear();
        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            return null;
        }

        float searchRadius = Mathf.Max(0.5f, attackRange);
        if (enemyLayer.value != 0)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, searchRadius, enemyLayer);
            for (int index = 0; index < hits.Length; index++)
            {
                Collider2D hit = hits[index];
                if (hit == null)
                {
                    continue;
                }

                EnemyUnit enemyUnit = hit.GetComponentInParent<EnemyUnit>();
                Transform enemyTransform = enemyUnit != null ? enemyUnit.transform : hit.transform.root;
                if (!IsVisibleInCamera(enemyTransform.position, activeCamera))
                {
                    continue;
                }

                if (!visibleEnemyBuffer.Contains(enemyTransform))
                {
                    visibleEnemyBuffer.Add(enemyTransform);
                }
            }
        }
        else
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
            for (int index = 0; index < enemies.Length; index++)
            {
                GameObject enemy = enemies[index];
                if (enemy == null || !enemy.activeInHierarchy)
                {
                    continue;
                }

                if (Vector2.Distance(origin, enemy.transform.position) > searchRadius)
                {
                    continue;
                }

                if (!IsVisibleInCamera(enemy.transform.position, activeCamera))
                {
                    continue;
                }

                visibleEnemyBuffer.Add(enemy.transform);
            }
        }

        if (visibleEnemyBuffer.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, visibleEnemyBuffer.Count);
        return visibleEnemyBuffer[randomIndex];
    }

    private bool IsVisibleInCamera(Vector3 worldPosition, Camera activeCamera)
    {
        Vector3 viewportPoint = activeCamera.WorldToViewportPoint(worldPosition);
        if (viewportPoint.z < 0f)
        {
            return false;
        }

        float margin = Mathf.Max(0f, screenMargin);
        return viewportPoint.x >= -margin && viewportPoint.x <= 1f + margin && viewportPoint.y >= -margin && viewportPoint.y <= 1f + margin;
    }

    private void FireProjectile(RobotGroup group, Vector3 origin, Vector3 targetPosition)
    {
        if (group.effect == null || group.effect.ProjectilePrefab == null)
        {
            return;
        }

        Vector2 direction = ((Vector2)(targetPosition - origin)).normalized;
        if (direction == Vector2.zero)
        {
            direction = Vector2.right;
        }

        GameObject projectileObject = PoolManager.Spawn(group.effect.ProjectilePrefab, origin, Quaternion.identity, 8);
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
            projectile = projectileObject.AddComponent<WeaponOrbitProjectile>();
        }

        projectile.Initialize(direction, group.effect.ProjectileSpeed, group.effect.GetDamage(playerStats, group.stackCount), group.effect.ProjectileLifetime);
        projectile.SetKnockback(group.effect.GetKnockbackForce(playerStats), group.effect.KnockbackDuration);
        projectile.SetOwner(playerStats);
    }

    private void ReturnObject(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!PoolManager.Return(instance))
        {
            Destroy(instance);
        }
    }

    private RobotGroup FindGroup(object runtimeHandle)
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