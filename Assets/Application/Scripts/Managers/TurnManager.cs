using System;
using System.Collections.Generic;
using GridBattle.Gameplay;
using GridBattle.Gameplay.Controllers;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Simulation;
using GridBattle.Gameplay.Turns;
using UnityEngine;

namespace GridBattle.Managers
{
    public enum EEnemyTurnPacing
    {
        /// <summary>Each enemy acts after the previous one finished moving.</summary>
        Sequential,

        /// <summary>
        /// Each enemy acts a short interval after the previous one, without waiting
        /// for its animation; the turn ends when every animation finished.
        /// </summary>
        Staggered,

        /// <summary>All enemies act at once; the turn ends when every move finished.</summary>
        Simultaneous
    }

    /// <summary>
    /// Turn flow (GDD: global turn). Each global turn the player plays first, then
    /// every enemy. Each entity has its own turn inside it: at its start the
    /// entity's states run their turn-start effects (damage/healing over time)
    /// and at its end the state durations go down (EntityTurnStarted/EndedEvent).
    /// Every action consumed by the player (PlayerActionEvent) ends the player's
    /// turn; after the player's animations the enemies act (closest to the player
    /// first, ties by y then x) and then the turn returns to the player. Each step
    /// also waits for <see cref="TurnBlockers"/> (XP orbs that level up, talent
    /// choice, pause). Tapping during the enemies' turn speeds the animations up
    /// until the turn returns to the player. Raises TurnChangedEvent on every
    /// change.
    /// </summary>
    [RequireComponent(typeof(GridController), typeof(BattleController))]
    public class TurnManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Whether enemies take their turns one after another or all at once.")]
        private EEnemyTurnPacing enemyPacing = EEnemyTurnPacing.Staggered;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds between enemies in Staggered pacing (at normal speed).")]
        private float enemyStagger = 0.08f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("Animation speed while the player taps during the enemies' turn.")]
        private float speedUpMultiplier = 2f;

        private GridController _grid;
        private BattleController _battle;
        private int _turnVersion;
        private bool _battleOver;

        public ETurnOwner CurrentTurn { get; private set; } = ETurnOwner.Player;

        /// <summary>Global turn number of the current battle (1 = first turn).</summary>
        public int GlobalTurn { get; private set; }

        private void Awake()
        {
            _grid = GetComponent<GridController>();
            _battle = GetComponent<BattleController>();
            EventBus.Subscribe<PlayerActionEvent>(OnPlayerAction);
            EventBus.Subscribe<GridInitializedEvent>(OnGridInitialized);
            EventBus.Subscribe<CellTapEvent>(OnCellTap);
            EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Subscribe<BattleDecidedEvent>(OnBattleDecided);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerActionEvent>(OnPlayerAction);
            EventBus.Unsubscribe<GridInitializedEvent>(OnGridInitialized);
            EventBus.Unsubscribe<CellTapEvent>(OnCellTap);
            EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Unsubscribe<BattleDecidedEvent>(OnBattleDecided);
        }

        private void Start()
        {
            // The grid built by the editor (debug) before this component subscribed.
            if (GlobalTurn == 0 && FindAnyObjectByType<PlayerCharacter>() != null)
                BeginBattle(1);
        }

        private void OnCellTap(CellTapEvent e)
        {
            if (CurrentTurn == ETurnOwner.Enemies)
                _grid.AnimationSpeed = speedUpMultiplier;
        }

        private void OnPlayerAction(PlayerActionEvent e)
        {
            if (CurrentTurn != ETurnOwner.Player || IsBattleOver) return;

            _ = RunEnemyTurn(++_turnVersion);
        }

        /// <summary>
        /// A new battle starts on the player's turn; a pending enemy turn of the
        /// previous battle is abandoned.
        /// </summary>
        private void OnGridInitialized(GridInitializedEvent e)
        {
            BeginBattle(e.FirstGlobalTurn);
        }

        /// <summary>
        /// The outcome is decided: the turn flow stops and nobody may act while the
        /// last animations, XP orbs and talent choice resolve.
        /// </summary>
        private void OnBattleDecided(BattleDecidedEvent e)
        {
            _turnVersion++;
            if (CurrentTurn != ETurnOwner.None)
                SetTurn(ETurnOwner.None);
        }

        private void OnBattleEnded(BattleEndedEvent e)
        {
            _battleOver = true;
            _turnVersion++;
        }

        private void BeginBattle(int firstGlobalTurn)
        {
            _battleOver = false;
            TurnBlockers.Clear();
            if (_battle != null)
                _battle.BeginBattle();
            GlobalTurn = Mathf.Max(1, firstGlobalTurn) - 1;
            _ = BeginPlayerTurn(++_turnVersion);
        }

        private async Awaitable BeginPlayerTurn(int version)
        {
            GlobalTurn++;
            EventBus.Raise(new GlobalTurnStartedEvent(GlobalTurn));

            var player = FindAnyObjectByType<PlayerCharacter>();
            if (player != null)
                BeginEntityTurn(player);

            try
            {
                await WaitForAnimationsAndBlockers();
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (!IsCurrent(version)) return;
            if (player == null || player.IsDead) return;

            SetTurn(ETurnOwner.Player);
        }

        private async Awaitable RunEnemyTurn(int version)
        {
            SetTurn(ETurnOwner.Enemies);

            var player = FindAnyObjectByType<PlayerCharacter>();
            if (player != null && !player.IsDead)
                EndEntityTurn(player);

            try
            {
                // Let the player's own move (and any level up it caused) finish first.
                await WaitForAnimationsAndBlockers();
                if (!IsCurrent(version)) return;

                foreach (var enemy in GetEnemiesInActingOrder())
                {
                    if (enemy == null) continue;
                    var character = enemy.GetComponent<Enemy>();
                    if (character == null || character.IsDead) continue;

                    BeginEntityTurn(character);
                    if (!character.IsDead)
                        enemy.Act();
                    if (!character.IsDead)
                        EndEntityTurn(character);

                    if (enemyPacing == EEnemyTurnPacing.Sequential)
                    {
                        await _grid.WaitForMovementsAsync(destroyCancellationToken);
                    }
                    else if (enemyPacing == EEnemyTurnPacing.Staggered && enemyStagger > 0f && !SimMode.IsActive)
                    {
                        await Awaitable.WaitForSecondsAsync(enemyStagger / _grid.AnimationSpeed,
                            destroyCancellationToken);
                    }

                    await TurnBlockers.WaitAsync(destroyCancellationToken);
                    if (!IsCurrent(version)) return;
                    if (player == null || player.IsDead) return;
                }

                await WaitForAnimationsAndBlockers();
                if (!IsCurrent(version)) return;

                await BeginPlayerTurn(version);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private bool IsBattleOver => _battleOver || (_battle != null && _battle.IsOver);

        private bool IsCurrent(int version) => version == _turnVersion && !IsBattleOver;

        private async Awaitable WaitForAnimationsAndBlockers()
        {
            do
            {
                await _grid.WaitForMovementsAsync(destroyCancellationToken);
                await TurnBlockers.WaitAsync(destroyCancellationToken);
            } while (_grid.IsAnimatingMovement || TurnBlockers.IsBlocked);
        }

        private void BeginEntityTurn(Character character)
        {
            character.BeginTurn();
            if (!character.IsDead)
                EventBus.Raise(new EntityTurnStartedEvent(character, GlobalTurn));
        }

        private void EndEntityTurn(Character character)
        {
            EventBus.Raise(new EntityTurnEndedEvent(character, GlobalTurn));
            if (!character.IsDead)
                character.EndTurn();
        }

        private static List<EnemyController> GetEnemiesInActingOrder()
        {
            var enemies = new List<EnemyController>(FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude));
            var player = FindAnyObjectByType<PlayerCharacter>();
            if (player == null) return enemies;

            var playerPos = player.CurrentGridPos;
            enemies.Sort((a, b) =>
            {
                var posA = a.GetComponent<GridEntity>().CurrentGridPos;
                var posB = b.GetComponent<GridEntity>().CurrentGridPos;
                var byDistance = (posA - playerPos).sqrMagnitude.CompareTo((posB - playerPos).sqrMagnitude);
                if (byDistance != 0) return byDistance;
                var byY = posA.y.CompareTo(posB.y);
                return byY != 0 ? byY : posA.x.CompareTo(posB.x);
            });
            return enemies;
        }

        private void SetTurn(ETurnOwner owner)
        {
            if (owner == ETurnOwner.Player)
                _grid.AnimationSpeed = 1f;
            CurrentTurn = owner;
            EventBus.Raise(new TurnChangedEvent(owner));
        }
    }
}
