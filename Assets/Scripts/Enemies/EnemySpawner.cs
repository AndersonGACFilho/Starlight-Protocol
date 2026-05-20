#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// This class is responsible for spawning enemies based on defined spawn points and enemy definitions.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy References")] [Tooltip("Enemy definitions available for spawning.")]
    public List<EnemySpawnDefinition> enemySpawnDefinitions = new List<EnemySpawnDefinition>();

    [Tooltip("The target followed by spawned enemies. If empty, the spawner will search by Player tag.")]
    public Transform target = null;

    [Tooltip("The object used as parent holder for enemy projectiles.")]
    public Transform projectileHolder = null;

    [Header("Target Lookup")] [Tooltip("Tag used to find the player when target is not assigned.")] [SerializeField]
    private string playerTag = "Player";

    [Header("Spawn Points")] [Tooltip("Possible spawn points used by this spawner.")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Tooltip("Minimum distance required between the player and the selected spawn point.")] [Min(0)]
    public float minDistanceFromTarget = 6.0f;

    [Tooltip("Maximum attempts to find a safe spawn point.")] [Min(1)]
    public int maxSpawnPointAttempts = 20;

    [Tooltip("If true, allows spawning at any valid spawn point if no safe point is found.")]
    public bool fallbackToAnySpawnPoint = false;

    [Header("Fallback Spawn Area")] [Tooltip("Fallback X range used only when no spawn points are assigned.")] [Min(0)]
    public float spawnRangeX = 10.0f;

    [Tooltip("Fallback Y range used only when no spawn points are assigned.")] [Min(0)]
    public float spawnRangeY = 10.0f;

    [Header("Debug")] [Tooltip("Draw debug gizmos in the Scene view.")] [SerializeField]
    private bool drawDebugGizmos = true;

    [Tooltip("Radius used to draw each spawn point in the Scene view.")] [Min(0.1f)] [SerializeField]
    private float spawnPointDebugRadius = 0.5f;

    [Header("Spawn Mode")]
    [Tooltip("If true, WaveManager keeps spawning enemies without ending the wave.")]
    public bool spawnInfinite = false;

    [Tooltip("Legacy field kept for GameManager compatibility. WaveManager controls enemy count now.")]
    [HideInInspector]
    public int maxSpawn = 0;

    /// <summary>
    /// Description:
    /// Attempts to spawn an enemy based on the current wave and available spawn points.
    /// Inputs:
    /// currentWave: The current wave number, used to determine which enemies can be spawned.
    /// Returns:
    /// true if an enemy was successfully spawned; false otherwise.
    /// </summary>
    public bool TrySpawnEnemy(int currentWave)
    {
        EnemySpawnDefinition spawnDefinition = GetRandomEnemyDefinition(currentWave);

        if (spawnDefinition == null)
        {
            Debug.LogWarning($"{nameof(EnemySpawner)}: No valid enemy definition found for wave {currentWave}.");
            return false;
        }

        if (!TryGetSpawnLocation(out Vector3 spawnLocation))
        {
            Debug.LogWarning($"{nameof(EnemySpawner)}: No valid spawn location found.");
            return false;
        }

        SpawnEnemy(spawnDefinition, spawnLocation);
        return true;
    }

    /// <summary>
    /// Description:
    /// Spawns an enemy based on the provided spawn definition and location.
    /// Inputs:
    /// spawnDefinition: The definition of the enemy to spawn, including prefab and settings.
    /// spawnLocation: The world position where the enemy should be spawned.
    /// Returns:
    /// void (no return)
    /// </summary>
    private void SpawnEnemy(EnemySpawnDefinition spawnDefinition, Vector3 spawnLocation)
    {
        Enemy enemyPrefab = spawnDefinition.enemyPrefab;

        if (!enemyPrefab)
        {
            Debug.LogWarning($"{nameof(EnemySpawner)}: Enemy prefab is null.");
            return;
        }

        Enemy enemy = Instantiate(
            enemyPrefab,
            spawnLocation,
            enemyPrefab.transform.rotation,
            spawnDefinition.enemyParent
        );

        Transform resolvedTarget = ResolveTarget();

        if (resolvedTarget)
        {
            enemy.followTarget = resolvedTarget;
        }

        ShootingController[] shootingControllers =
            enemy.GetComponentsInChildren<ShootingController>();

        foreach (ShootingController gun in shootingControllers)
        {
            gun.projectileHolder = projectileHolder;
        }
    }

    /// <summary>
    /// Description:
    /// Selects a random enemy definition from the list of available definitions based on the current wave and their
    /// spawn weights.
    /// Inputs:
    /// currentWave: The current wave number, used to filter which enemy definitions are eligible for spawning.
    /// Returns:
    /// A randomly selected EnemySpawnDefinition that is valid for the current wave, or null if no valid definitions
    /// are found.
    /// </summary>
    private EnemySpawnDefinition GetRandomEnemyDefinition(int currentWave)
    {
        List<EnemySpawnDefinition> availableDefinitions = GetAvailableEnemyDefinitions(currentWave);

        if (availableDefinitions.Count == 0)
        {
            return null;
        }

        int totalWeight = 0;

        foreach (EnemySpawnDefinition definition in availableDefinitions)
        {
            totalWeight += Mathf.Max(1, definition.spawnWeight);
        }

        int randomWeight = Random.Range(0, totalWeight);
        int accumulatedWeight = 0;

        foreach (EnemySpawnDefinition definition in availableDefinitions)
        {
            accumulatedWeight += Mathf.Max(1, definition.spawnWeight);

            if (randomWeight < accumulatedWeight)
            {
                return definition;
            }
        }

        return availableDefinitions[0];
    }

    /// <summary>
    /// Description:
    /// Filters the list of enemy spawn definitions to those that are valid for the current wave and have valid prefabs.
    /// Inputs:
    /// currentWave: The current wave number, used to determine which enemy definitions are eligible for spawning.
    /// Returns:
    /// A list of EnemySpawnDefinition objects that are valid for the current wave and have valid enemy prefabs.
    /// The list may be empty if no definitions are valid.
    /// </summary>
    private List<EnemySpawnDefinition> GetAvailableEnemyDefinitions(int currentWave)
    {
        List<EnemySpawnDefinition> availableDefinitions = new List<EnemySpawnDefinition>();

        if (enemySpawnDefinitions == null)
        {
            return availableDefinitions;
        }

        foreach (EnemySpawnDefinition definition in enemySpawnDefinitions)
        {
            if (definition == null)
            {
                continue;
            }

            if (!definition.enemyPrefab)
            {
                continue;
            }

            if (currentWave < definition.minimumWave)
            {
                continue;
            }

            availableDefinitions.Add(definition);
        }

        return availableDefinitions;
    }

    /// <summary>
    /// Description:
    /// Attempts to find a valid spawn location for an enemy based on the assigned spawn points and their distance
    /// from the target.
    /// The method will try up to maxSpawnPointAttempts times to randomly select a spawn point and check if it is safe
    /// (i.e., not too close to the target). If a safe spawn point is found, its position is returned.
    /// If no safe spawn point is found after the maximum attempts, the method can optionally fallback to any valid
    /// spawn point regardless of distance if fallbackToAnySpawn
    /// </summary>
    private bool TryGetSpawnLocation(out Vector3 spawnLocation)
    {
        spawnLocation = Vector3.zero;

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            spawnLocation = GetFallbackRandomLocation();
            return true;
        }

        for (int attempt = 0; attempt < maxSpawnPointAttempts; attempt++)
        {
            Transform candidate = spawnPoints[Random.Range(0, spawnPoints.Count)];

            if (!candidate)
            {
                continue;
            }

            if (IsSpawnPointSafe(candidate.position))
            {
                spawnLocation = candidate.position;
                return true;
            }
        }

        if (fallbackToAnySpawnPoint)
        {
            return TryGetAnyValidSpawnPoint(out spawnLocation);
        }

        return false;
    }

    /// <summary>
    /// Description:
    /// Attempts to find any valid spawn point from the list of assigned spawn points, regardless of its distance
    /// from the target.
    /// This is used as a fallback when the spawner fails to find a safe spawn point after the maximum number of
    /// attempts. It iterates through the list of spawn points and returns the position of the first valid spawn point
    /// it finds.
    /// If no valid spawn points are found, it returns false.
    /// Inputs:
    /// none
    /// Returns:
    /// true if a valid spawn point is found and its position is returned in the spawnLocation output parameter;
    /// false if no valid spawn points are found.
    /// </summary>
    private bool TryGetAnyValidSpawnPoint(out Vector3 spawnLocation)
    {
        spawnLocation = Vector3.zero;

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (!spawnPoint)
            {
                continue;
            }

            spawnLocation = spawnPoint.position;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Description:
    /// Generates a random spawn location within a defined rectangular area around the spawner's position.
    /// This is used as a fallback when no spawn points are assigned.
    /// The area is defined by spawnRangeX and spawnRangeY, which determine how far from the spawner's position
    /// the random location can be. The generated location will have a random offset in both X and Y directions,
    /// while maintaining the same Z coordinate as the spawner.
    /// Inputs:
    /// none
    /// Returns:
    /// A Vector3 representing the randomly generated spawn location within the defined area around the spawner's
    /// position.
    /// </summary>
    private Vector3 GetFallbackRandomLocation()
    {
        float x = Random.Range(-spawnRangeX, spawnRangeX);
        float y = Random.Range(-spawnRangeY, spawnRangeY);

        return new Vector3(
            transform.position.x + x,
            transform.position.y + y,
            transform.position.z
        );
    }

    /// <summary>
    /// Description:
    /// Determines if a given spawn position is safe for spawning an enemy based on its distance from the target.
    /// A spawn point is considered safe if it is at least minDistanceFromTarget units away from the target's position.
    /// If no target is assigned or found, all spawn points are considered safe.
    /// Inputs:
    /// spawnPosition: The world position of the spawn point being evaluated.
    /// Returns:
    /// true if the spawn point is safe for spawning an enemy; false if it is too close to the target.
    /// </summary>
    private bool IsSpawnPointSafe(Vector3 spawnPosition)
    {
        Transform resolvedTarget = ResolveTarget();

        if (!resolvedTarget)
        {
            return true;
        }

        float distanceToTarget = Vector3.Distance(spawnPosition, resolvedTarget.position);
        return distanceToTarget >= minDistanceFromTarget;
    }

    /// <summary>
    /// Description:
    /// Resolves the target transform that spawned enemies should follow.
    /// If a target is already assigned, it returns that target.
    /// If no target is assigned, it attempts to find a GameObject with the specified player tag and assigns its
    /// transform as the target.
    /// </summary>
    private Transform ResolveTarget()
    {
        if (target)
        {
            return target;
        }

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);

        if (player)
        {
            target = player.transform;
        }

        return target;
    }

    /// <summary>
    /// Description:
    /// Draws debug gizmos in the Scene view to visualize spawn points, fallback spawn area, and minimum distance from
    /// the target.
    /// This helps with level design and debugging by showing where enemies can potentially spawn and the areas they
    /// will avoid based on the target's position.
    /// Gizmos are only drawn when the spawner is selected in the editor and the drawDebugGizmos flag is enabled.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos)
        {
            return;
        }

        DrawFallbackSpawnAreaGizmo();
        DrawSpawnPointsGizmos();
        DrawMinimumDistanceFromTargetGizmo();
    }

    /// <summary>
    /// Description:
    /// Draws a gizmo representing the fallback spawn area when no spawn points are assigned. This is visualized as a
    /// wire cube centered on the spawner's position with dimensions based on the
    /// </summary>
    private void DrawFallbackSpawnAreaGizmo()
    {
        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(spawnRangeX * 2.0f, spawnRangeY * 2.0f, 0.0f)
        );
    }

    /// <summary>
    /// Description:
    /// Draws gizmos representing each spawn point in the Scene view. This helps visualize where enemies can potentially
    /// spawn based on the assigned spawn points. Each spawn point is drawn as a wire disc
    /// </summary>
    private void DrawSpawnPointsGizmos()
    {
        if (spawnPoints == null)
        {
            return;
        }

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (!spawnPoint)
            {
                continue;
            }

#if UNITY_EDITOR
            Handles.DrawWireDisc(spawnPoint.position, Vector3.forward, spawnPointDebugRadius);
#else
                Gizmos.DrawWireSphere(spawnPoint.position, spawnPointDebugRadius);
#endif
        }
    }

    /// <summary>
    /// Description:
    /// Draws a gizmo representing the minimum safe distance from the target around the target's position.
    /// This helps visualize the area where enemies will not spawn if the target is within that radius
    /// </summary>
    private void DrawMinimumDistanceFromTargetGizmo()
    {
        Transform resolvedTarget = ResolveTarget();

        if (!resolvedTarget)
        {
            return;
        }

#if UNITY_EDITOR
        Handles.DrawWireDisc(resolvedTarget.position, Vector3.forward, minDistanceFromTarget);
#else
            Gizmos.DrawWireSphere(resolvedTarget.position, minDistanceFromTarget);
#endif
    }
}
