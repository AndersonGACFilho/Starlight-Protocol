# 2D Shooter

Unity 2D space shooter with inertia-based movement, progressive waves, weighted enemy spawning, stackable power-ups, and a dynamic HUD.

## Project Version

- Unity: `6000.4.2f1`
- Rendering: Universal Render Pipeline 2D
- Input: Unity Input System
- Main scenes:
  - `Assets/_Scenes/MainMenu.unity`
  - `Assets/_Scenes/Level1.unity`

## Running the Project

1. Open the project folder in Unity Hub.
2. Use Unity `6000.4.2f1` or a compatible Unity 6 version.
3. Open `Assets/_Scenes/MainMenu.unity` or `Assets/_Scenes/Level1.unity`.
4. Press Play.

## Controls

The player uses `InputAction` fields configured on the prefab:

- Movement: 2D vector, usually `WASD`.
- Aim: mouse position.
- Fire: left mouse button.
- Pause: handled by `UIManager`, based on the Input Action configured in the UI prefab.

## Main Structure

- `Assets/Scripts/Player`: player controllers and animation helpers.
- `Assets/Scripts/ShootingProjectiles`: shooting, projectiles, spread, and extra shot patterns.
- `Assets/Scripts/Enemies`: enemy behavior, movement, shooting, and spawning.
- `Assets/Scripts/Health&Damage`: health, damage, invincibility, shield, and death flow.
- `Assets/Scripts/PowerUps`: pickups, temporary effects, and power-up spawning.
- `Assets/Scripts/Waves`: wave progression.
- `Assets/Scripts/UI`: HUD, menus, score, wave display, indicators, and power-up UI.
- `Assets/Scripts/Utility`: general utilities such as score reset, screenshots, floating motion, and timed destruction.
- `Assets/Prefabs`: prefabs for the player, enemies, power-ups, UI, cameras, effects, and managers.

## Player

Main prefab:

`Assets/Prefabs/Player/Player.prefab`

Important components:

- `SpaceshipController`: rotates toward the mouse, applies forward thrust, clamps max speed, applies drag, and drives the thruster light.
- `ShootingController`: fires projectiles and applies fire rate, spread, and Extra Shots projectile count.
- `Health`: controls health, invincibility, death, lives, and max-health increases from Health pickups.
- `PlayerPowerUpController`: stores the active power-up state.

The player moves like a spaceship: it rotates toward the mouse and applies force only forward. `thrusterLight` flickers while accelerating, and movement animation is driven through the `MoveSpeed` and `IsMoving` animator parameters.

## Shooting

Shooting is handled by:

`Assets/Scripts/ShootingProjectiles/ShootingController.cs`

Main settings:

- `fireRate`: base delay between shots.
- `projectileSpread`: random angle variation applied to each projectile.
- `extraShotAngleInterval`: base angular interval used by Extra Shots, currently `15` degrees on the player prefab.
- `fireEffect`: effect instantiated when firing.

Extra Shots rule:

- No power-up: 1 projectile.
- 1 stack: 3 projectiles.
- 2 stacks: 5 projectiles.
- Each new stack adds 2 more projectiles.

Projectiles are distributed symmetrically using the same angular interval. When the pattern would close a full 360-degree circle, the interval starts shrinking so every projectile fits around the circle without duplicated directions.

## Health, Damage, and Shield

`Health.cs` controls:

- `currentHealth`
- `maximumHealth`
- `defaultHealth`
- temporary invincibility after damage
- optional lives
- respawn or Game Over flow

Important rules:

- If the player has an active shield, `Health.TakeDamage` calls `PlayerPowerUpController.TryConsumeShield()` and the incoming damage is absorbed.
- Health pickup is not a normal temporary power-up. It heals immediately.
- If health is already full, a Health pickup increases `maximumHealth` and updates `defaultHealth`.

The shield visual is a child object on the player and is referenced by `PlayerPowerUpController.shieldPrefab`. This field must point to the player's shield visual object, not to the pickup prefab.

## Power-ups

Types in `PowerUpType.cs`:

- `Health`
- `RapidFire`
- `TripleShot` / Extra Shots
- `Shield`
- `SpeedBoost`

Prefabs:

- `Assets/Prefabs/PowerUps/Types/HealthPowerUp.prefab`
- `Assets/Prefabs/PowerUps/Types/ExtraShotsPowerUp.prefab`
- `Assets/Prefabs/PowerUps/Types/ShieldPowerUp.prefab`
- `Assets/Prefabs/PowerUps/Types/SpeedPowerUp.prefab`

`PowerUpPickup.cs` applies the effect when colliding with the player and plays `pickupSound` through `AudioSource.PlayClipAtPoint`.

Rules:

- `duration > 0`: temporary power-up.
- `duration == 0`: infinite power-up.
- Health ignores duration.
- Temporary power-ups stack while their timer is active.
- When the timer expires, that power-up state and stack count are reset.

Active power-ups:

- `RapidFire`: increases fire rate through a multiplier.
- `Extra Shots`: adds 2 projectiles per stack.
- `Shield`: enables the player shield visual and absorbs one damage event.
- `SpeedBoost`: increases player movement speed.
- `Health`: heals or increases max health when already full.

## Power-up Spawner

`PowerUpSpawner.cs` receives:

- a list of power-up pickup prefabs
- a list of spawn points
- an optional parent transform for organizing spawned pickups

`WaveManager` calls:

`powerUpSpawner.SpawnRandomPowerUps(powerUpsPerCompletedWave)`

The current documented code path spawns 2 random power-ups after each completed finite wave.

## Waves

`WaveManager.cs` controls the wave loop:

- starts the first wave in `Start`
- spawns enemies with a scaling delay
- increases enemy count per wave
- reduces spawn delay down to a minimum
- limits simultaneous alive enemies
- waits `timeBetweenWaves` before the next wave
- spawns power-ups after completing a finite wave

Main settings:

- `baseEnemyCount`
- `enemyIncreasePerWave`
- `baseSpawnDelay`
- `spawnDelayReductionPerWave`
- `minSpawnDelay`
- `timeBetweenWaves`
- `powerUpsPerCompletedWave`
- `maxAliveEnemies`

If `EnemySpawner.spawnInfinite` is enabled, `WaveManager` keeps spawning enemies and does not end the wave by enemy count.

## Enemies

`Enemy.cs` contains the base behavior:

- movement
- score value on defeat
- follow target
- shooting modes
- movement modes
- inertia-based movement support

`EnemySpawner.cs` selects enemies through `EnemySpawnDefinition`:

- enemy prefab
- optional parent
- minimum wave
- spawn weight

The spawner also:

- searches for safe spawn points away from the player
- uses a fallback area when no spawn points are assigned
- draws optional debug gizmos
- supports infinite spawn mode
- injects the target and projectile holder into spawned enemies

## UI and HUD

Base HUD prefab:

`Assets/Prefabs/UI/InGame/BaseUI.prefab`

Main systems:

- `ScoreDisplay`: shows score and pulses when it changes.
- `HighScoreDisplay`: shows saved high score.
- `WaveDisplay`: shows current wave, remaining enemies, and state.
- `WaveCountdownBar`: shows progress between waves.
- `PowerUpDisplay`: shows HUD icons for selected temporary power-ups.
- `NearestEnemyIndicator`: creates a red marker pointing toward the nearest enemy when no enemy is visible on screen.
- `HealthBarsDisplay`: generates red vertical health/life bars, but does not need to be attached to the HUD if that UI is not used.

### PowerUpDisplay

The power-up display shows 3 slots:

- Extra Shots
- Shield
- Speed

Health is excluded because it is instant. Shield is excluded because it has direct visual feedback on the player.

Features:

- runtime icon generation when `powerUpImages` is empty
- legacy text hidden when icons are available
- numeric stack indicator next to the icon
- blinking warning when a finite-duration power-up is close to expiring
- infinite power-ups do not blink

## Camera

`CameraController.cs` supports:

- `Locked`
- `Overhead`
- `Free`

It also supports `Collider2D` bounds, smoothing, and Input Action based look control.

## GameManager

`GameManager.cs` keeps:

- global singleton
- current score
- saved high score
- victory condition
- Game Over flow
- communication with `UIManager`
- victory and defeat effects

## UIManager

`UIManager.cs` manages UI pages:

- startup page
- active page
- pause page
- page switching
- `UIelement` updates
- default selected buttons per page

## Effects and Utilities

Utility scripts:

- `DirectionalMover`: moves objects in a configured direction.
- `Floating`: applies sine-wave floating motion, used by pickups.
- `TimedObjectDestroyer`: destroys effects after a configured lifetime.
- `ScreenshotUtility`: captures screenshots.
- `ScoreReseter`: resets score through GameManager.
- `CursorChanger`: swaps the cursor texture.

## Quick Configuration Points

Player:

- `Assets/Prefabs/Player/Player.prefab`
- movement speed: `SpaceshipController.moveSpeed`
- max speed: `SpaceshipController.maxSpeed`
- drag: `SpaceshipController.spaceshipDrag`
- Extra Shots interval: `ShootingController.extraShotAngleInterval`
- projectile prefab: `ShootingController.projectilePrefab`
- shield visual: `PlayerPowerUpController.shieldPrefab`

Waves:

- `Assets/Prefabs/Wave/WaveManager.prefab`
- initial enemy count, per-wave increase, delay, and power-ups per wave

Enemies:

- `Assets/Prefabs/Enemies/Spawners`
- `EnemySpawnDefinition` controls minimum wave and spawn weight

Power-ups:

- `Assets/Prefabs/PowerUps/Types`
- `duration == 0` makes a power-up infinite
- `pickupSound` defines collection audio

HUD:

- `Assets/Prefabs/UI/InGame/BaseUI.prefab`
- `PowerUpDisplay` controls slots, icons, stacks, and blinking
- `NearestEnemyIndicator` controls the red offscreen enemy marker

## Development Notes

- Many fields use `Header` and `Tooltip` attributes for Inspector configuration.
- Health and Shield are handled specially: Health is instant, Shield has its own player visual.
- Do not assign pickup prefabs to player visual fields. The player shield should be a child object on the player.
- If a power-up should expire, set `duration` above zero on the prefab.
- If a power-up should be permanent/infinite, set `duration` to zero.
