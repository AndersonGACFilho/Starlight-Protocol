using System;
using System.Collections.Generic;

/// <summary>
/// Handles enemy shooting behavior.
/// </summary>
public static class EnemyShootingModule
{
    public static void TryShoot(Enemy.ShootMode shootMode, List<ShootingController> guns)
    {
        switch (shootMode)
        {
            case Enemy.ShootMode.None:
                return;

            case Enemy.ShootMode.ShootAll:
                if (guns == null)
                {
                    return;
                }

                foreach (ShootingController gun in guns)
                {
                    if (gun)
                    {
                        gun.Fire();
                    }
                }
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(shootMode), shootMode, null);
        }
    }
}
