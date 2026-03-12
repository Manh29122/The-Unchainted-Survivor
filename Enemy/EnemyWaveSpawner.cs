using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyWaveSpawner : MonoBehaviour
{
    private struct EnemySpawnRequest
    {
        public GameObject enemyPrefab;
        public bool overrideEnemyData;
        public EnemyRuntimeData runtimeData;
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private ShopRerollSystem shopRerollSystem;
    [SerializeField] private ShopRerollUI shopRerollUI;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerItemInventory playerItemInventory;
    [SerializeField] private string playerVisualRootName = "UnitRoot";

    [Header("Wave Settings")]
    [SerializeField] private List<EnemyWaveDefinition> waves = new List<EnemyWaveDefinition>();
    [SerializeField] private bool autoStartFirstWave = true;
    [SerializeField] private bool autoAdvanceWave = true;
    [SerializeField] private int startWaveIndex;
    [SerializeField] private bool openShopBetweenWaves = true;
    [SerializeField] private int enemyPoolPreload = 16;

    private readonly List<EnemyUnit> aliveEnemies = new List<EnemyUnit>();
    private readonly Dictionary<int, List<EnemyUnit>> waveAliveEnemies = new Dictionary<int, List<EnemyUnit>>();
    private Coroutine waveRoutine;
    private int currentWaveIndex = -1;
    private float currentWaveDuration;
    private float currentWaveEndTime;
    private bool isWaveActive;
    private GameObject playerVisualRoot;

    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    public event Action<int, float, float> OnWaveTimerUpdated;

    public int CurrentWaveIndex => currentWaveIndex;
    public bool IsWaveActive => isWaveActive;
    public float CurrentWaveDuration => currentWaveDuration;
    public float CurrentWaveRemainingTime
    {
        get
        {
            if (!isWaveActive)
            {
                return 0f;
            }

            if (float.IsInfinity(currentWaveEndTime))
            {
                return 0f;
            }

            return Mathf.Max(0f, currentWaveEndTime - Time.time);
        }
    }

    private void Start()
    {
        ResolvePlayer();
        ResolveReferences();

        if (shopRerollUI != null)
        {
            shopRerollUI.HideShop();
        }

        if (autoStartFirstWave)
        {
            StartWave(startWaveIndex);
        }
    }

    private void Update()
    {
        if (!isWaveActive)
        {
            return;
        }

        float remainingTime = CurrentWaveRemainingTime;
        OnWaveTimerUpdated?.Invoke(currentWaveIndex, remainingTime, currentWaveDuration);
    }

    public void StartWave(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= waves.Count)
        {
            Debug.LogWarning($"[EnemyWaveSpawner] Invalid wave index: {waveIndex}");
            return;
        }

        ResolveReferences();

        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
        }

        currentWaveIndex = waveIndex;
        waveAliveEnemies[waveIndex] = new List<EnemyUnit>();
        currentWaveDuration = Mathf.Max(0f, waves[waveIndex].waveDuration);
        currentWaveEndTime = currentWaveDuration > 0f ? Time.time + currentWaveDuration : float.PositiveInfinity;
        isWaveActive = true;
        SetPlayerVisualVisible(true);
        ResumePlayerEffects();

        if (shopRerollUI != null)
        {
            shopRerollUI.HideShop();
        }

        OnWaveTimerUpdated?.Invoke(currentWaveIndex, CurrentWaveRemainingTime, currentWaveDuration);
        waveRoutine = StartCoroutine(RunWave(waves[waveIndex], waveIndex));
    }

    public void StartNextWave()
    {
        int nextWave = currentWaveIndex + 1;
        if (nextWave >= waves.Count)
        {
            return;
        }

        StartWave(nextWave);
    }

    private IEnumerator RunWave(EnemyWaveDefinition waveDefinition, int waveIndex)
    {
        ResolveReferences();
        List<EnemySpawnRequest> requests = BuildSpawnRequests(waveDefinition);
        bool hasWaveDuration = waveDefinition.waveDuration > 0f;

        OnWaveStarted?.Invoke(waveIndex);

        for (int i = 0; i < requests.Count; i++)
        {
            if (Time.time >= currentWaveEndTime)
            {
                break;
            }

            SpawnEnemy(requests[i], waveDefinition);

            float delay = Mathf.Max(0f, waveDefinition.delayBetweenSpawns);
            if (delay > 0f && i < requests.Count - 1)
            {
                yield return new WaitForSeconds(delay);
            }
        }

        while (true)
        {
            bool allWaveEnemiesDefeated = GetAliveEnemyCountForWave(waveIndex) <= 0;
            bool timeExpired = hasWaveDuration && Time.time >= currentWaveEndTime;

            if (timeExpired)
            {
                DestroyAllAliveEnemies();
                break;
            }

            if (waveDefinition.waitForAllEnemiesDefeated && allWaveEnemiesDefeated)
            {
                break;
            }

            if (!hasWaveDuration && !waveDefinition.waitForAllEnemiesDefeated)
            {
                break;
            }

            yield return null;
        }

        isWaveActive = false;
        currentWaveEndTime = 0f;
        SetPlayerVisualVisible(false);
        PausePlayerEffects();
        OnWaveTimerUpdated?.Invoke(waveIndex, 0f, currentWaveDuration);
        OnWaveCompleted?.Invoke(waveIndex);
        waveRoutine = null;
        GrantHarvestingGoldReward();

        bool hasNextWave = waveIndex + 1 < waves.Count;
        if (hasNextWave && openShopBetweenWaves)
        {
            PrepareShopForNextWave();
            if (OpenShop())
            {
                yield break;
            }
        }

        if (autoAdvanceWave && hasNextWave)
        {
            StartWave(waveIndex + 1);
        }
    }

    private void SpawnEnemy(EnemySpawnRequest request, EnemyWaveDefinition waveDefinition)
    {
        if (request.enemyPrefab == null)
        {
            return;
        }

        ResolvePlayer();

        Vector3 spawnPosition = GetSpawnPosition(waveDefinition);
        GameObject enemyObject = PoolManager.Spawn(request.enemyPrefab, spawnPosition, Quaternion.identity, enemyPoolPreload);
        if (enemyObject == null)
        {
            return;
        }

        EnemyUnit enemyUnit = enemyObject.GetComponent<EnemyUnit>();
        if (enemyUnit == null)
        {
            enemyUnit = enemyObject.AddComponent<EnemyUnit>();
        }

        if (request.overrideEnemyData)
        {
            enemyUnit.Initialize(request.runtimeData, player);
        }
        else
        {
            enemyUnit.SetTargetPlayer(player);
        }

        enemyUnit.OnDied += HandleEnemyDied;
        aliveEnemies.Add(enemyUnit);

        if (!waveAliveEnemies.TryGetValue(currentWaveIndex, out List<EnemyUnit> waveEnemies))
        {
            waveEnemies = new List<EnemyUnit>();
            waveAliveEnemies[currentWaveIndex] = waveEnemies;
        }

        waveEnemies.Add(enemyUnit);
    }

    private void HandleEnemyDied(EnemyUnit enemyUnit)
    {
        if (enemyUnit == null)
        {
            return;
        }

        enemyUnit.OnDied -= HandleEnemyDied;
        aliveEnemies.Remove(enemyUnit);

        foreach (KeyValuePair<int, List<EnemyUnit>> pair in waveAliveEnemies)
        {
            pair.Value.Remove(enemyUnit);
        }
    }

    private List<EnemySpawnRequest> BuildSpawnRequests(EnemyWaveDefinition waveDefinition)
    {
        List<EnemySpawnRequest> requests = new List<EnemySpawnRequest>();
        if (waveDefinition.enemies == null)
        {
            return requests;
        }

        for (int entryIndex = 0; entryIndex < waveDefinition.enemies.Count; entryIndex++)
        {
            EnemySpawnEntry entry = waveDefinition.enemies[entryIndex];
            int spawnCount = Mathf.Max(0, entry.spawnCount);

            for (int spawnIndex = 0; spawnIndex < spawnCount; spawnIndex++)
            {
                requests.Add(new EnemySpawnRequest
                {
                    enemyPrefab = entry.enemyPrefab,
                    overrideEnemyData = entry.overrideEnemyData,
                    runtimeData = entry.overrideData
                });
            }
        }

        ShuffleRequests(requests);
        return requests;
    }

    private void ShuffleRequests(List<EnemySpawnRequest> requests)
    {
        for (int index = 0; index < requests.Count; index++)
        {
            int swapIndex = UnityEngine.Random.Range(index, requests.Count);
            EnemySpawnRequest temp = requests[index];
            requests[index] = requests[swapIndex];
            requests[swapIndex] = temp;
        }
    }

    private Vector3 GetSpawnPosition(EnemyWaveDefinition waveDefinition)
    {
        Vector3 center = player != null ? player.position : transform.position;
        float minRadius = Mathf.Max(0f, waveDefinition.minSpawnRadius);
        float maxRadius = Mathf.Max(minRadius, waveDefinition.maxSpawnRadius);
        Vector2 offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(minRadius, maxRadius);
        return center + new Vector3(offset.x, offset.y, 0f);
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void ResolveReferences()
    {
        ResolvePlayer();

        if (shopRerollSystem == null)
        {
            shopRerollSystem = FindFirstObjectByType<ShopRerollSystem>();
        }

        if (shopRerollUI == null)
        {
            shopRerollUI = FindFirstObjectByType<ShopRerollUI>(FindObjectsInactive.Include);
        }

        if (playerStats == null)
        {
            playerStats = player != null ? player.GetComponent<PlayerStats>() : null;
        }

        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        if (playerItemInventory == null)
        {
            playerItemInventory = player != null ? player.GetComponent<PlayerItemInventory>() : null;
        }

        if (playerItemInventory == null)
        {
            playerItemInventory = FindFirstObjectByType<PlayerItemInventory>();
        }

        ResolvePlayerVisualRoot();
    }

    private int GetAliveEnemyCountForWave(int waveIndex)
    {
        if (!waveAliveEnemies.TryGetValue(waveIndex, out List<EnemyUnit> waveEnemies) || waveEnemies == null)
        {
            return 0;
        }

        for (int index = waveEnemies.Count - 1; index >= 0; index--)
        {
            EnemyUnit enemyUnit = waveEnemies[index];
            if (enemyUnit == null || !enemyUnit.IsAlive)
            {
                waveEnemies.RemoveAt(index);
            }
        }

        return waveEnemies.Count;
    }

    private void DestroyAllAliveEnemies()
    {
        for (int index = aliveEnemies.Count - 1; index >= 0; index--)
        {
            EnemyUnit enemyUnit = aliveEnemies[index];
            if (enemyUnit == null)
            {
                aliveEnemies.RemoveAt(index);
                continue;
            }

            enemyUnit.OnDied -= HandleEnemyDied;
            if (!PoolManager.Return(enemyUnit.gameObject))
            {
                Destroy(enemyUnit.gameObject);
            }
        }

        aliveEnemies.Clear();

        foreach (KeyValuePair<int, List<EnemyUnit>> pair in waveAliveEnemies)
        {
            pair.Value.Clear();
        }
    }

    private void PrepareShopForNextWave()
    {
        if (shopRerollSystem == null)
        {
            return;
        }

        shopRerollSystem.ResetRerollCost();
        shopRerollSystem.GrantFreeRerolls(1);
    }

    private bool OpenShop()
    {
        if (shopRerollUI == null)
        {
            return false;
        }

        shopRerollUI.OpenShop();
        return true;
    }

    private void GrantHarvestingGoldReward()
    {
        if (playerStats == null)
        {
            return;
        }

        int harvestingGoldReward = playerStats.GetHarvestingGoldReward();
        if (harvestingGoldReward <= 0)
        {
            return;
        }

        playerStats.AddGoldIgnoringMultiplier(harvestingGoldReward);
    }

    private void ResolvePlayerVisualRoot()
    {
        if (playerVisualRoot != null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(playerVisualRootName))
        {
            playerVisualRoot = GameObject.Find(playerVisualRootName);
        }

        if (playerVisualRoot != null)
        {
            return;
        }

        Transform[] sceneTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        playerVisualRoot = sceneTransforms
            .FirstOrDefault(t => t != null && t.name == playerVisualRootName && t.gameObject.scene.IsValid())
            ?.gameObject;
    }

    private void SetPlayerVisualVisible(bool visible)
    {
        ResolvePlayerVisualRoot();

        if (playerVisualRoot == null)
        {
            return;
        }

        if (playerVisualRoot.activeSelf != visible)
        {
            playerVisualRoot.SetActive(visible);
        }
    }

    private void PausePlayerEffects()
    {
        playerItemInventory?.PauseAllEffects();
    }

    private void ResumePlayerEffects()
    {
        playerItemInventory?.ResumeAllEffects();
    }
}