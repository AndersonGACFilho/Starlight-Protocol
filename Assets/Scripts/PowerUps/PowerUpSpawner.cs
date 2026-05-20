using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Spawna power-ups em pontos definidos do mapa.
/// Pode ser chamado ao final de cada wave.
/// </summary>
public class PowerUpSpawner : MonoBehaviour
{
    [Header("Power-up References")]
    [Tooltip("List of pickup prefabs that can be randomly spawned between waves.")]
    [SerializeField] private List<PowerUpPickup> powerUpPrefabs = new List<PowerUpPickup>();

    [Header("Spawn Points")]
    [Tooltip("Possible world positions where power-up pickups can appear.")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("Organization")]
    [Tooltip("Optional parent transform used to keep spawned power-ups organized in the hierarchy.")]
    [SerializeField] private Transform powerUpParent;

    public void SpawnRandomPowerUp()
    {
        SpawnRandomPowerUps(1);
    }

    public void SpawnRandomPowerUps(int count)
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Count == 0)
        {
            Debug.LogWarning($"{nameof(PowerUpSpawner)}: No power-up prefabs assigned.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning($"{nameof(PowerUpSpawner)}: No power-up spawn points assigned.");
            return;
        }

        int spawnCount = Mathf.Max(0, count);
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnSingleRandomPowerUp();
        }
    }

    private void SpawnSingleRandomPowerUp()
    {
        PowerUpPickup prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Count)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

        if (!prefab || !spawnPoint)
        {
            return;
        }

        Instantiate(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation,
            powerUpParent
        );
    }

    private void OnDrawGizmosSelected()
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

            Gizmos.DrawWireSphere(spawnPoint.position, 0.4f);
        }
    }
}
