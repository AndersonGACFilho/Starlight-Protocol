using System;

/// <summary>
/// Handles one-time defeat registration for an enemy.
/// </summary>
public sealed class EnemyDefeatModule
{
    private bool _defeatRegistered;

    public void RegisterDefeat(Enemy enemy, int scoreValue, Action<Enemy> onEnemyDefeated)
    {
        if (_defeatRegistered)
        {
            return;
        }

        _defeatRegistered = true;

        if (GameManager.instance != null && !GameManager.instance.gameIsOver)
        {
            GameManager.AddScore(scoreValue);
            GameManager.instance.IncrementEnemiesDefeated();
        }

        onEnemyDefeated?.Invoke(enemy);
    }
}
