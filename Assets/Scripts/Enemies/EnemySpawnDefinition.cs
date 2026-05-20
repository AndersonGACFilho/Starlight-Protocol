using System;
using UnityEngine;

/// <summary>
/// Define um inimigo que pode ser spawnado, incluindo o prefab,
/// o parent na Hierarchy, a wave mínima e o peso de spawn.
/// </summary>
[Serializable]
public class EnemySpawnDefinition
{
    [Tooltip("Enemy prefab to spawn. The prefab must have an Enemy component.")]
    public Enemy enemyPrefab;

    [Tooltip("Optional parent used to organize spawned enemies in the hierarchy.")]
    public Transform enemyParent;

    [Tooltip("Minimum wave required for this enemy to appear.")]
    [Min(1)]
    public int minimumWave = 1;

    [Tooltip("Relative chance of this enemy being selected. Higher means more frequent.")]
    [Min(1)]
    public int spawnWeight = 1;
}