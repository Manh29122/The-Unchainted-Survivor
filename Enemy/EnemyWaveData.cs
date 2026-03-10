using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct EnemyRuntimeData
{
    [Min(1)] public int maxHealth;
    [Min(0f)] public float moveSpeed;
    [Min(0)] public int contactDamage;
}

[Serializable]
public struct EnemySpawnEntry
{
    public GameObject enemyPrefab;
    [Min(0)] public int spawnCount;
    public bool overrideEnemyData;
    public EnemyRuntimeData overrideData;
}

[Serializable]
public struct EnemyWaveDefinition
{
    public string waveName;
    [Min(0f)] public float waveDuration;
    [Min(0f)] public float delayBetweenSpawns;
    [Min(0f)] public float minSpawnRadius;
    [Min(0f)] public float maxSpawnRadius;
    public bool waitForAllEnemiesDefeated;
    public List<EnemySpawnEntry> enemies;
}