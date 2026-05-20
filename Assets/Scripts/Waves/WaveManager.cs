using System.Collections;
using UnityEngine;

/// <summary>
/// Controla a progressão das waves em um único level de sobrevivência.
/// Aumenta quantidade de inimigos, reduz intervalo de spawn e acompanha inimigos vivos.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Enemy spawner used by this wave manager. If empty, one is found in the scene.")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Tooltip("Power-up spawner called when a finite wave is completed. If empty, one is found in the scene.")]
    [SerializeField] private PowerUpSpawner powerUpSpawner;

    [Header("Wave Scaling")]
    [Tooltip("Enemy count used for wave 1.")]
    [SerializeField] private int baseEnemyCount = 5;

    [Tooltip("Additional enemies added for each new wave.")]
    [SerializeField] private int enemyIncreasePerWave = 2;

    [Tooltip("Initial delay between enemy spawn attempts.")]
    [SerializeField] private float baseSpawnDelay = 2.5f;

    [Tooltip("Amount removed from spawn delay for each completed wave.")]
    [SerializeField] private float spawnDelayReductionPerWave = 0.15f;

    [Tooltip("Lowest spawn delay allowed after wave scaling.")]
    [SerializeField] private float minSpawnDelay = 0.6f;

    [Header("Wave Flow")]
    [Tooltip("Seconds to wait after completing a finite wave before starting the next one.")]
    [SerializeField] private float timeBetweenWaves = 5.0f;

    [Tooltip("Number of random power-ups spawned when a finite wave is completed.")]
    [SerializeField] private int powerUpsPerCompletedWave = 2;

    [Tooltip("Maximum number of enemies alive at the same time.")]
    [SerializeField] private int maxAliveEnemies = 10;

    private int _currentWave = 0;
    private int _enemiesToSpawn = 0;
    private int _enemiesSpawned = 0;
    private int _enemiesDefeated = 0;
    private int _aliveEnemies = 0;

    private float _spawnTimer = 0.0f;
    private float _nextWaveCountdownRemaining = 0.0f;
    private bool _waveActive = false;
    private bool _waitingNextWave = false;

    public int CurrentWave => _currentWave;
    public int EnemiesToSpawn => _enemiesToSpawn;
    public int EnemiesSpawned => _enemiesSpawned;
    public int EnemiesDefeated => _enemiesDefeated;
    public int EnemiesRemaining => Mathf.Max(0, _enemiesToSpawn - _enemiesDefeated);
    public bool WaveActive => _waveActive;

    /// <summary>
    /// Description:
    /// Indicates whether the manager is currently in the between-waves waiting state.
    /// Inputs:
    /// none
    /// Returns:
    /// bool: true while waiting to start the next wave, otherwise false
    /// </summary>
    public bool WaitingNextWave => _waitingNextWave;

    /// <summary>
    /// Description:
    /// Gets the countdown time remaining until the next wave starts.
    /// Inputs:
    /// none
    /// Returns:
    /// float: Remaining seconds while waiting between waves, otherwise 0
    /// </summary>
    public float NextWaveCountdownRemaining => _waitingNextWave ? _nextWaveCountdownRemaining : 0.0f;

    /// <summary>
    /// Description:
    /// Returns normalized countdown progress for the next wave start.
    /// Inputs:
    /// none
    /// Returns:
    /// float: 0 to 1 progression for the between-wave timer
    /// </summary>
    public float NextWaveCountdownProgress
    {
        get
        {
            if (!_waitingNextWave)
            {
                return 0.0f;
            }

            if (timeBetweenWaves <= 0.0f)
            {
                return 1.0f;
            }

            return Mathf.Clamp01(1.0f - (_nextWaveCountdownRemaining / timeBetweenWaves));
        }
    }

    private float CurrentSpawnDelay
    {
        get
        {
            int completedWaves = Mathf.Max(0, _currentWave - 1);

            return Mathf.Max(
                minSpawnDelay,
                baseSpawnDelay - (completedWaves * spawnDelayReductionPerWave)
            );
        }
    }

    private bool SpawnInfinite => enemySpawner && enemySpawner.spawnInfinite;

    private void Awake()
    {
        if (!enemySpawner)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (!powerUpSpawner)
        {
            powerUpSpawner = FindFirstObjectByType<PowerUpSpawner>();
        }
    }

    private void OnEnable()
    {
        Enemy.EnemyDefeated += HandleEnemyDefeated;
    }

    private void OnDisable()
    {
        Enemy.EnemyDefeated -= HandleEnemyDefeated;
    }

    private void Start()
    {
        StartNextWave();
    }

    private void Update()
    {
        HandleEnemySpawning();
    }

    private void HandleEnemySpawning()
    {
        if (!_waveActive)
        {
            return;
        }

        if (!SpawnInfinite && _enemiesSpawned >= _enemiesToSpawn)
        {
            return;
        }

        if (_aliveEnemies >= maxAliveEnemies)
        {
            return;
        }

        _spawnTimer += Time.deltaTime;

        if (_spawnTimer < CurrentSpawnDelay)
        {
            return;
        }

        if (!enemySpawner)
        {
            Debug.LogError($"{nameof(WaveManager)}: EnemySpawner reference is missing.");
            _waveActive = false;
            return;
        }

        bool spawned = enemySpawner.TrySpawnEnemy(_currentWave);

        if (!spawned)
        {
            return;
        }

        _spawnTimer = 0.0f;
        _enemiesSpawned++;
        _aliveEnemies++;
    }

    private void StartNextWave()
    {
        _currentWave++;

        _enemiesToSpawn = baseEnemyCount + ((_currentWave - 1) * enemyIncreasePerWave);
        _enemiesSpawned = 0;
        _enemiesDefeated = 0;
        _aliveEnemies = 0;
        _spawnTimer = CurrentSpawnDelay;
        _nextWaveCountdownRemaining = 0.0f;
        _waveActive = true;
        _waitingNextWave = false;

        Debug.Log($"Wave {_currentWave} started. Enemies: {_enemiesToSpawn}. Spawn delay: {CurrentSpawnDelay}");
    }

    private void HandleEnemyDefeated(Enemy enemy)
    {
        if (!_waveActive)
        {
            return;
        }

        _enemiesDefeated++;
        _aliveEnemies = Mathf.Max(0, _aliveEnemies - 1);

        if (!SpawnInfinite && _enemiesDefeated >= _enemiesToSpawn)
        {
            EndCurrentWave();
        }
    }

    private void EndCurrentWave()
    {
        if (_waitingNextWave)
        {
            return;
        }

        _waveActive = false;
        _waitingNextWave = true;
        _nextWaveCountdownRemaining = Mathf.Max(0.0f, timeBetweenWaves);

        Debug.Log($"Wave {_currentWave} completed.");

        if (powerUpSpawner)
        {
            powerUpSpawner.SpawnRandomPowerUps(powerUpsPerCompletedWave);
        }

        StartCoroutine(StartNextWaveAfterDelay());
    }

    private IEnumerator StartNextWaveAfterDelay()
    {
        // Runs a frame-by-frame countdown so UI can read remaining time and progress.
        if (_nextWaveCountdownRemaining <= 0.0f)
        {
            StartNextWave();
            yield break;
        }

        while (_nextWaveCountdownRemaining > 0.0f)
        {
            _nextWaveCountdownRemaining -= Time.deltaTime;
            yield return null;
        }

        _nextWaveCountdownRemaining = 0.0f;
        StartNextWave();
    }
}
