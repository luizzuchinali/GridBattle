using System;
using System.Collections.Generic;
using System.Threading;
using GridBattle.Gameplay.Controllers;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Movement;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.Terrain;
using GridBattle.Data;
using GridBattle.Managers;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace GridBattle.Gameplay
{
    /// <summary>
    /// Owns the board: creates the cells, places the player and the encounter's
    /// enemies (via CharacterFactory) and centralizes occupancy queries and
    /// moves.
    /// </summary>
    [ExecuteAlways]
    public class GridController : MonoBehaviour
    {
        private static readonly Vector2Int PlayerSpawnPosition = new(2, 2);

        [Header("Grid Settings")]
        [SerializeField]
        private Vector2Int gridSize = new Vector2Int(6, 6);

        [SerializeField]
        private Cell cellPrefab;

        [Header("Spawn")]
        [SerializeField]
        [Tooltip("Enemies placed when the grid is initialized.")]
        private EncounterConfig encounter;

        [SerializeField]
        [CanBeNull]
        [Tooltip("Character used when the grid is initialized from the editor, without going through the menu.")]
        private PlayerCharacterConfig debugPlayerConfig;

        [Header("Movement")]
        [SerializeField]
        [Tooltip("Hop animation used when entities change cells. Empty = instant.")]
        private GridMovementSettings movementSettings;

        [Header("Terrain")]
        [SerializeField]
        [Tooltip("Terrain placed when the grid is initialized from the editor (Awake in the editor, Reset grid), so designers can preview it. Runs started from the menu never use it.")]
        private List<TerrainPlacement> debugTerrain = new();

        private readonly GridMovementAnimator _movementAnimator = new();
        private readonly GridEffectsAnimator _effectsAnimator = new();
        private Cell[,] _cells;
        private TerrainEffects _terrainEffects;
        private int _blockedCellCount;
        private BattleSpec _battleSpec;

        public static float Ppu => GameConfigManager.Ppu;

        [CanBeNull]
        public PlayerCharacterConfig DebugPlayerConfig => debugPlayerConfig;

        /// <summary>Width and height of the grid, in cells.</summary>
        public Vector2Int Size => gridSize;

        /// <summary>Whether any cell is currently blocked by terrain.</summary>
        public bool HasBlockedCells => _blockedCellCount > 0;

        /// <summary>
        /// The generated battle being played (set by <see cref="InitializeBattle"/> and
        /// <see cref="RestoreBattle"/>), or null outside a run's battle (editor debug start, map).
        /// </summary>
        [CanBeNull]
        public BattleSpec CurrentBattleSpec => _battleSpec;

        /// <summary>Whether any entity is still playing a movement or combat animation.</summary>
        public bool IsAnimatingMovement => _movementAnimator.IsAnimating || _effectsAnimator.IsAnimating;

        /// <summary>Completes when every movement, attack, hit and death animation has finished.</summary>
        public async Awaitable WaitForMovementsAsync(CancellationToken cancellationToken)
        {
            while (IsAnimatingMovement)
                await Awaitable.NextFrameAsync(cancellationToken);
        }

        /// <summary>Divides the duration of every grid animation (1 = normal speed).</summary>
        public float AnimationSpeed
        {
            get => _movementAnimator.SpeedMultiplier;
            set
            {
                _movementAnimator.SpeedMultiplier = value;
                _effectsAnimator.SpeedMultiplier = value;
            }
        }

        /// <summary>
        /// Attack lunge toward <paramref name="targetPos"/>. Call before applying
        /// the damage so the target's hit reaction lands on the impact.
        /// </summary>
        public void PlayAttackAnimation(GridEntity attacker, Vector2Int targetPos)
        {
            if (attacker == null || !IsValidPosition(targetPos)) return;

            var targetCell = _cells[targetPos.x, targetPos.y];
            var direction = targetCell.transform.position - attacker.transform.position;
            _effectsAnimator.PlayAttack(attacker, targetCell.GetContent(), direction, movementSettings);
        }

        public void PlayHitAnimation(GridEntity entity) => _effectsAnimator.PlayHit(entity, movementSettings);

        /// <summary>
        /// Seconds the slide of a pushed or pulled character takes over <paramref name="cells"/> cells at normal speed
        /// (plus the small collision delay). 0 when there are no movement settings.
        /// </summary>
        public float GetSlideDuration(int cells)
        {
            if (movementSettings == null) return 0f;

            return (GridMovementAnimator.GetSlideDuration(movementSettings, cells) +
                    movementSettings.CollisionBumpDelay) / Mathf.Max(0.01f, AnimationSpeed);
        }

        /// <summary>
        /// Delays the next hit or death reaction of <paramref name="entity"/> by <paramref name="delay"/> seconds, with
        /// the recoil pushed along <paramref name="direction"/> (grid direction): the collision bump of a displaced
        /// character. Call it before the damage of the collision. Visual only.
        /// </summary>
        public void QueueImpactAnimation(GridEntity entity, Vector2Int direction, float delay)
        {
            // Grid y grows downward, like the world y does not: flip it to get the world direction.
            _effectsAnimator.QueueImpact(entity, new Vector3(direction.x, -direction.y, 0f), delay);
        }

        public void PlayDeathAnimation(GridEntity entity) => _effectsAnimator.PlayDeath(entity, movementSettings);

        private void Awake()
        {
            Assert.IsNotNull(cellPrefab, "Cell prefab is not set!");
#if UNITY_EDITOR
            InitializeGrid(debugPlayerConfig, true);
#endif
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;

            _terrainEffects ??= new TerrainEffects(this);
            _terrainEffects.Enable();
        }

        private void OnDisable()
        {
            _terrainEffects?.Disable();
        }

        /// <summary>
        /// Rebuilds the board (without terrain), places the player and the
        /// encounter's enemies. <paramref name="useDebugTerrain"/> also applies the
        /// designer's <c>debugTerrain</c> list (editor previews only).
        /// </summary>
        public void InitializeGrid([CanBeNull] PlayerCharacterConfig playerConfig, bool useDebugTerrain = false)
        {
            _battleSpec = null;
            BuildCells();
            if (useDebugTerrain)
                ApplyDebugTerrain();
            SpawnPlayer(playerConfig);
            SpawnEncounter();
            EventBus.Raise(new GridInitializedEvent());
        }

        // ------------------------------------------------------------------------------------------------
        // Run battles (GDD Mechanic 2, 8.1): built from a generated BattleSpec, captured and restored exactly.
        // ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Builds the board of a run's battle node: cells of the spec's size, its terrain, the player at the spec's
        /// start position carrying the run's state (<see cref="PlayerRunStateApplier"/>: level, XP, skills,
        /// consumables, carried states, talent hooks, persistent HP) and the spec's enemies with their depth scaling
        /// and XP reward. Raises <see cref="GridInitializedEvent"/>, which starts the battle's turn flow.
        /// </summary>
        public void InitializeBattle(BattleSpec spec, PlayerCharacterConfig playerConfig, PlayerRunState player)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (playerConfig == null) throw new ArgumentNullException(nameof(playerConfig));
            if (player == null) throw new ArgumentNullException(nameof(player));

            gridSize = new Vector2Int(Mathf.Max(1, spec.Width), Mathf.Max(1, spec.Height));
            BuildCells();
            ApplyTerrain(spec.Terrain);
            _battleSpec = spec;

            var playerPosition = new Vector2Int(spec.PlayerX, spec.PlayerY);
            var character = SpawnBattlePlayer(playerConfig, playerPosition);
            if (character != null)
            {
                PlayerRunStateApplier.Apply(character, player, false);
                PlayerRunStateApplier.FinishNewBattle(character, player);
            }

            var database = GameDatabase.Instance;
            foreach (var enemy in spec.Enemies)
            {
                var config = database != null ? database.Get<EnemyConfig>(enemy.EnemyId) : null;
                if (config == null)
                {
                    Debug.LogWarning($"Enemy id '{enemy.EnemyId}' not found in the GameDatabase; skipped.");
                    continue;
                }

                var spawned = SpawnEnemy(config, new Vector2Int(enemy.X, enemy.Y),
                    new CharacterScaling(enemy.HpMultiplier, enemy.DamageMultiplier), enemy.XpReward);
                if (spawned == null)
                    Debug.LogWarning($"Enemy {config.name} could not be placed at ({enemy.X}, {enemy.Y}); skipped.");
            }

            EventBus.Raise(new GridInitializedEvent());
        }

        /// <summary>
        /// Destroys every cell and the characters on them (the map is shown over an empty world). Safe to call when
        /// the board is already empty.
        /// </summary>
        public void ClearBoard()
        {
            foreach (var cell in FindObjectsByType<Cell>())
            {
                if (cell != null)
                    DestroyImmediate(cell.gameObject);
            }

            _cells = null;
            _blockedCellCount = 0;
            _battleSpec = null;
        }

        /// <summary>
        /// Exact state of the battle in progress: terrain, global turn, the player and every living enemy (config,
        /// position, HP, scaling, XP reward, summoned flag, states, skill cooldowns and AI memory) and whether a
        /// consumable was used this turn. <c>NodeId</c> is left at -1 for the run manager to fill. The run saves it
        /// at the start of each player turn (before that turn's start effects, so
        /// <see cref="RestoreBattle"/> can replay the turn start without applying them twice).
        /// </summary>
        public BattleSnapshot CaptureBattle()
        {
            var snapshot = new BattleSnapshot
            {
                NodeId = -1,
                GlobalTurn = TryGetComponent(out TurnManager turns) ? Mathf.Max(1, turns.GlobalTurn) : 1,
                Spec = CreateSnapshotSpec(),
            };

            var player = FindAnyObjectByType<PlayerCharacter>();
            if (player != null)
            {
                snapshot.Player = CaptureEntity(player);
                snapshot.ConsumableUsedThisTurn = player.ConsumableUsedThisTurn;
            }

            var enemies = new List<Enemy>();
            foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
            {
                if (!enemy.IsDead)
                    enemies.Add(enemy);
            }

            // Stable order, so the same battle always serializes the same way.
            enemies.Sort((a, b) =>
            {
                var byY = a.CurrentGridPos.y.CompareTo(b.CurrentGridPos.y);
                return byY != 0 ? byY : a.CurrentGridPos.x.CompareTo(b.CurrentGridPos.x);
            });
            foreach (var enemy in enemies)
                snapshot.Enemies.Add(CaptureEntity(enemy));

            return snapshot;
        }

        /// <summary>
        /// Rebuilds exactly the battle of <paramref name="snapshot"/>: the board and terrain, the player and the
        /// enemies at their positions with their HP, scaling, XP reward, states (restored as they were: remaining
        /// turns, stacks, shield), skill cooldowns, AI memory and summoned flag. The player also gets the run's
        /// progress, skills and consumables (<paramref name="player"/>). Raises
        /// <see cref="GridInitializedEvent"/> with the snapshot's global turn, so the turn flow resumes at the
        /// start of that player turn.
        /// </summary>
        public void RestoreBattle(BattleSnapshot snapshot, PlayerCharacterConfig playerConfig, PlayerRunState player)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.Spec == null) throw new ArgumentException("The snapshot has no battle spec.", nameof(snapshot));
            if (snapshot.Player == null) throw new ArgumentException("The snapshot has no player.", nameof(snapshot));
            if (playerConfig == null) throw new ArgumentNullException(nameof(playerConfig));
            if (player == null) throw new ArgumentNullException(nameof(player));

            var spec = snapshot.Spec;
            gridSize = new Vector2Int(Mathf.Max(1, spec.Width), Mathf.Max(1, spec.Height));
            BuildCells();
            ApplyTerrain(spec.Terrain);
            _battleSpec = spec;

            var character = SpawnBattlePlayer(playerConfig, new Vector2Int(snapshot.Player.X, snapshot.Player.Y));
            if (character != null)
            {
                PlayerRunStateApplier.Apply(character, player, true);
                PlayerRunStateApplier.RestoreSnapshotState(character, snapshot.Player);
                PlayerRunStateApplier.FinishRestoredBattle(character, player, snapshot.Player);
            }

            var database = GameDatabase.Instance;
            foreach (var entity in snapshot.Enemies)
            {
                var config = database != null ? database.Get<EnemyConfig>(entity.ConfigId) : null;
                if (config == null)
                {
                    Debug.LogWarning($"Enemy id '{entity.ConfigId}' not found in the GameDatabase; skipped.");
                    continue;
                }

                var enemy = SpawnEnemy(config, new Vector2Int(entity.X, entity.Y),
                    new CharacterScaling(entity.HpMultiplier, entity.DamageMultiplier), entity.XpReward);
                if (enemy == null)
                {
                    Debug.LogWarning($"Enemy {config.name} could not be restored at ({entity.X}, {entity.Y}); skipped.");
                    continue;
                }

                enemy.States.Clear();
                StateSnapshots.Restore(enemy, entity.States);
                enemy.Cooldowns.Restore(entity.SkillCooldowns);
                enemy.RestoreMoved(entity.MovedLastTurn);
                if (enemy.TryGetComponent(out EnemyController controller))
                    controller.RestoreMemory(entity.AiMemory);
                if (entity.IsSummoned)
                    enemy.MarkSummoned();
                enemy.SetHp(entity.Hp);
            }

            EventBus.Raise(new GridInitializedEvent(snapshot.GlobalTurn));

            // Starting the turn resets the counter, so it is restored after the event.
            if (character != null)
                character.RestoreConsumableUses(snapshot.ConsumableUsedThisTurn ? 1 : 0);
        }

        [CanBeNull]
        private PlayerCharacter SpawnBattlePlayer(PlayerCharacterConfig config, Vector2Int position)
        {
            if (!IsValidPosition(position))
            {
                Debug.LogError($"Player spawn {position} is outside the {gridSize.x}x{gridSize.y} grid.");
                return null;
            }

            var player = CharacterFactory.Spawn(config);
            if (player == null) return null;

            player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _cells[position.x, position.y].SetContent(player);
            return player;
        }

        /// <summary>The generated spec without the enemies (those are captured live) and with the board's current terrain.</summary>
        private BattleSpec CreateSnapshotSpec()
        {
            var spec = new BattleSpec
            {
                Width = gridSize.x,
                Height = gridSize.y,
                PlayerX = PlayerSpawnPosition.x,
                PlayerY = PlayerSpawnPosition.y,
            };
            if (_battleSpec != null)
            {
                spec.Depth = _battleSpec.Depth;
                spec.PlayerX = _battleSpec.PlayerX;
                spec.PlayerY = _battleSpec.PlayerY;
                spec.TotalXp = _battleSpec.TotalXp;
                spec.IsBoss = _battleSpec.IsBoss;
            }

            if (_cells == null) return spec;

            foreach (var cell in _cells)
            {
                if (cell == null || cell.Terrain == null || string.IsNullOrEmpty(cell.Terrain.Id)) continue;

                spec.Terrain.Add(new TerrainCellSpec
                {
                    X = cell.GridPosition.x,
                    Y = cell.GridPosition.y,
                    TerrainId = cell.Terrain.Id,
                });
            }

            return spec;
        }

        private static EntitySnapshot CaptureEntity(Character character)
        {
            var snapshot = new EntitySnapshot
            {
                ConfigId = character.Config != null ? character.Config.Id : null,
                X = character.CurrentGridPos.x,
                Y = character.CurrentGridPos.y,
                Hp = character.Current,
                HpMultiplier = character.Scaling.HpMultiplier,
                DamageMultiplier = character.Scaling.DamageMultiplier,
                States = StateSnapshots.Capture(character),
                SkillCooldowns = character.Cooldowns.Capture(),
                MovedLastTurn = character.MovedThisTurn,
            };

            if (character is Enemy enemy)
            {
                snapshot.XpReward = enemy.XpReward;
                snapshot.IsSummoned = enemy.IsSummoned;
                if (enemy.TryGetComponent(out EnemyController controller))
                    snapshot.AiMemory = controller.CaptureMemory();
            }

            return snapshot;
        }

        private void BuildCells()
        {
            var cells = FindObjectsByType<Cell>();
            foreach (var cell in cells)
            {
                if (cell != null)
                    DestroyImmediate(cell.gameObject);
            }

            _cells = new Cell[gridSize.x, gridSize.y];
            _blockedCellCount = 0;

            var pitchX = (Cell.Size.x + 2) / Ppu;
            var pitchY = (Cell.Size.y + 2) / Ppu;

            var originX = -(gridSize.x - 1) * pitchX / 2f;
            var originY = (gridSize.y - 1) * pitchY / 2f;

            var spriteBounds = cellPrefab.GetComponent<SpriteRenderer>().sprite.bounds;
            var pivotOffset = new Vector3(spriteBounds.center.x, spriteBounds.center.y, 0);

            for (var x = 0; x < gridSize.x; x++)
            {
                for (var y = 0; y < gridSize.y; y++)
                {
                    var pos = new Vector3(
                        originX + x * pitchX,
                        originY - y * pitchY,
                        0
                    ) - pivotOffset;

                    var instance = Instantiate(cellPrefab, pos, Quaternion.identity, new InstantiateParameters
                    {
                        parent = transform,
                        worldSpace = false
                    });
                    instance.gameObject.name = $"Cell_{x}_{y}";
                    instance.GridPosition = new Vector2Int(x, y);
                    _cells[x, y] = instance;
                }
            }
        }

        private void ApplyDebugTerrain()
        {
            foreach (var placement in debugTerrain)
            {
                if (placement.Terrain == null) continue;

                if (placement.Terrain.BlocksMovement && placement.Position == PlayerSpawnPosition)
                {
                    Debug.LogWarning($"Debug terrain {placement.Terrain.name} ignored: it would block the player spawn {PlayerSpawnPosition}.");
                    continue;
                }

                SetTerrain(placement.Position, placement.Terrain);
            }
        }

        private void SpawnPlayer([CanBeNull] PlayerCharacterConfig playerConfig)
        {
            if (playerConfig == null) return;

            var player = CharacterFactory.Spawn(playerConfig);
            if (player == null) return;

            player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _cells[PlayerSpawnPosition.x, PlayerSpawnPosition.y].SetContent(player);
        }

        private void SpawnEncounter()
        {
            if (encounter == null) return;

            foreach (var enemyConfig in encounter.Enemies)
            {
                if (enemyConfig == null) continue;

                var cell = FindFirstFreeCell();
                if (cell == null) return;

                var enemy = CharacterFactory.Spawn(enemyConfig);
                if (enemy == null) continue;

                enemy.transform.SetParent(cell.transform, false);
                cell.SetContent(enemy);
            }
        }

        /// <summary>
        /// Spawns an enemy on <paramref name="position"/> at runtime (summoning, the
        /// battle generator). The cell must be valid and free; returns null
        /// otherwise. A negative <paramref name="xpReward"/> keeps the config's XP.
        /// </summary>
        [CanBeNull]
        public Enemy SpawnEnemy(EnemyConfig config, Vector2Int position, CharacterScaling scaling,
            int xpReward = -1)
        {
            if (config == null || _cells == null) return null;
            if (!IsValidPosition(position) || !IsWalkable(position) || !IsFreePosition(position)) return null;

            var enemy = CharacterFactory.Spawn(config, scaling);
            if (enemy == null) return null;

            if (xpReward >= 0)
                enemy.SetXpReward(xpReward);

            var cell = _cells[position.x, position.y];
            enemy.transform.SetParent(cell.transform, false);
            cell.SetContent(enemy);
            return enemy;
        }

        [CanBeNull]
        private Cell FindFirstFreeCell()
        {
            foreach (var cell in _cells)
            {
                if (!cell.HasContent && !cell.IsBlocked)
                    return cell;
            }

            return null;
        }

        public void MoveEntity(GridEntity entity, Vector2Int targetPos)
        {
            var moved = entity.CurrentGridPos != targetPos;
            Move(entity.CurrentGridPos, targetPos);
            entity.CurrentGridPos = targetPos;
            if (moved && entity is Character character)
                character.RegisterMovement();
        }

        /// <summary>
        /// Moves an entity that was pushed or pulled: the same logical move as <see cref="MoveEntity"/> (occupancy,
        /// <see cref="EntityEnteredCellEvent"/>), but it slides to the cell instead of hopping. Used by
        /// <see cref="Combat.DisplacementResolver"/>.
        /// </summary>
        public void DisplaceEntity(GridEntity entity, Vector2Int targetPos)
        {
            Move(entity.CurrentGridPos, targetPos, true);
            entity.CurrentGridPos = targetPos;
        }

        /// <summary>
        /// Forced entry: makes the terrain under a character that was pushed or pulled onto it apply its effect at
        /// once (see <see cref="TerrainEffects.ApplyForcedEntry"/>). Returns whether an effect was applied.
        /// </summary>
        public bool ApplyForcedTerrain(Character character)
        {
            return _terrainEffects != null && _terrainEffects.ApplyForcedEntry(character);
        }

        public bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < gridSize.x &&
                   position.y >= 0 && position.y < gridSize.y;
        }

        /// <summary>
        /// Whether nothing occupies the cell and terrain does not block it, i.e. an
        /// entity can be placed there.
        /// </summary>
        public bool IsFreePosition(Vector2Int position)
        {
            var cell = _cells[position.x, position.y];
            return !cell.HasContent && !cell.IsBlocked;
        }

        /// <summary>Whether the position is inside the grid and not blocked by terrain.</summary>
        public bool IsWalkable(Vector2Int position)
        {
            return IsValidPosition(position) && _cells != null && !_cells[position.x, position.y].IsBlocked;
        }

        /// <summary>The terrain on the cell, or null (plain floor, or outside the grid).</summary>
        [CanBeNull]
        public TerrainDefinition GetTerrain(Vector2Int position)
        {
            return IsValidPosition(position) && _cells != null ? _cells[position.x, position.y].Terrain : null;
        }

        /// <summary>The cell at the position, or null outside the grid.</summary>
        [CanBeNull]
        public Cell GetCell(Vector2Int position)
        {
            return IsValidPosition(position) && _cells != null ? _cells[position.x, position.y] : null;
        }

        /// <summary>Whether skill areas skip the cell (its terrain blocks skill areas).</summary>
        public bool BlocksSkillArea(Vector2Int position)
        {
            var terrain = GetTerrain(position);
            return terrain != null && terrain.BlocksSkillArea;
        }

        /// <summary>
        /// Replaces the board's terrain with <paramref name="specs"/> (ids resolved
        /// through the GameDatabase). Unknown ids, positions outside the grid and
        /// blocking terrain over an occupied cell are skipped with a warning.
        /// Returns how many cells got terrain.
        /// </summary>
        public int ApplyTerrain(IEnumerable<TerrainCellSpec> specs)
        {
            ClearTerrain();
            if (specs == null) return 0;

            var applied = 0;
            foreach (var spec in specs)
            {
                if (spec == null) continue;

                var definition = GameDatabase.Instance != null
                    ? GameDatabase.Instance.Get<TerrainDefinition>(spec.TerrainId)
                    : null;
                if (definition == null)
                {
                    Debug.LogWarning($"Terrain id '{spec.TerrainId}' not found in the GameDatabase; skipped.");
                    continue;
                }

                if (SetTerrain(new Vector2Int(spec.X, spec.Y), definition))
                    applied++;
            }

            return applied;
        }

        /// <summary>
        /// Puts <paramref name="terrain"/> (null = plain floor) on one cell. Refuses
        /// positions outside the grid and blocking terrain over an occupied cell.
        /// </summary>
        public bool SetTerrain(Vector2Int position, [CanBeNull] TerrainDefinition terrain)
        {
            if (_cells == null || !IsValidPosition(position))
            {
                Debug.LogWarning($"Terrain position {position} is outside the grid; skipped.");
                return false;
            }

            var cell = _cells[position.x, position.y];
            if (terrain != null && terrain.BlocksMovement && cell.HasContent)
            {
                Debug.LogWarning($"Terrain {terrain.name} at {position} would block an occupied cell; skipped.");
                return false;
            }

            cell.Terrain = terrain;
            RecountBlockedCells();
            return true;
        }

        /// <summary>Removes the terrain of every cell.</summary>
        public void ClearTerrain()
        {
            if (_cells == null) return;

            foreach (var cell in _cells)
            {
                if (cell != null && cell.Terrain != null)
                    cell.Terrain = null;
            }

            _blockedCellCount = 0;
        }

        private void RecountBlockedCells()
        {
            _blockedCellCount = 0;
            foreach (var cell in _cells)
            {
                if (cell != null && cell.IsBlocked)
                    _blockedCellCount++;
            }
        }

        public GridEntity GetContent(Vector2Int position)
        {
            return _cells[position.x, position.y].GetContent();
        }

        public void Move(Vector2Int currentPos, Vector2Int targetPos) => Move(currentPos, targetPos, false);

        private void Move(Vector2Int currentPos, Vector2Int targetPos, bool slide)
        {
            if (currentPos == targetPos)
                return;

            if (!IsFreePosition(targetPos))
                throw new InvalidOperationException("Target position is not free");

            var content = _cells[currentPos.x, currentPos.y].GetContent();
            var fromWorld = content.transform.position;
            _cells[currentPos.x, currentPos.y].RemoveContent();
            var targetCell = _cells[targetPos.x, targetPos.y];
            targetCell.SetContent(content);
            if (slide)
            {
                _movementAnimator.AnimateSlide(content, fromWorld, currentPos, targetPos, movementSettings);
            }
            else
            {
                _movementAnimator.Animate(content, fromWorld, currentPos, targetPos, movementSettings,
                    () =>
                    {
                        if (targetCell != null && movementSettings != null)
                            targetCell.PlayArrivalPulse(movementSettings.ArrivalPulsePixels,
                                movementSettings.ArrivalPulseDuration);
                    });
            }
            EventBus.Raise(new EntityEnteredCellEvent(content, currentPos, targetPos));
        }

        public void HighlightCells(Dictionary<Vector2Int, ECellHighlightType> highlightInfos)
        {
            foreach (var cell in _cells)
            {
                if (highlightInfos.TryGetValue(cell.GridPosition, out var highlightType))
                {
                    cell.Highlighted = true;
                    cell.HighlightType = highlightType;
                }
                else
                {
                    cell.Highlighted = false;
                }
            }
        }
    }
}
