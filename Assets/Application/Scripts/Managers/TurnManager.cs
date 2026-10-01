using System;
using GridBattle.Gameplay;
using GridBattle.Gameplay.Controllers;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
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
    /// Turn flow: every action consumed by the player (PlayerActionEvent) hands
    /// the turn to the enemies. After the player's move animation, the enemies act
    /// (waiting for their move animations) and then the turn returns to the
    /// player. Raises TurnChangedEvent on every change. Enemies act by distance to
    /// the player (closest first), ties broken by y then x. Tapping during the
    /// enemies' turn speeds the animations up until the turn returns to the player.
    /// </summary>
    [RequireComponent(typeof(GridController))]
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
        private int _turnVersion;

        public ETurnOwner CurrentTurn { get; private set; } = ETurnOwner.Player;

        private void Awake()
        {
            _grid = GetComponent<GridController>();
            EventBus.Subscribe<PlayerActionEvent>(OnPlayerAction);
            EventBus.Subscribe<GridInitializedEvent>(OnGridInitialized);
            EventBus.Subscribe<CellTapEvent>(OnCellTap);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerActionEvent>(OnPlayerAction);
            EventBus.Unsubscribe<GridInitializedEvent>(OnGridInitialized);
            EventBus.Unsubscribe<CellTapEvent>(OnCellTap);
        }

        private void OnCellTap(CellTapEvent e)
        {
            if (CurrentTurn == ETurnOwner.Enemies)
                _grid.AnimationSpeed = speedUpMultiplier;
        }

        private void OnPlayerAction(PlayerActionEvent e)
        {
            if (CurrentTurn != ETurnOwner.Player) return;

            _ = RunEnemyTurn(++_turnVersion);
        }

        /// <summary>
        /// A new run starts on the player's turn; a pending enemy turn of the
        /// previous run is abandoned.
        /// </summary>
        private void OnGridInitialized(GridInitializedEvent e)
        {
            _turnVersion++;
            SetTurn(ETurnOwner.Player);
        }

        private async Awaitable RunEnemyTurn(int version)
        {
            SetTurn(ETurnOwner.Enemies);
            var cancellation = destroyCancellationToken;

            try
            {
                // Let the player's own move finish first.
                await _grid.WaitForMovementsAsync(cancellation);
                if (version != _turnVersion) return;

                foreach (var enemy in GetEnemiesInActingOrder())
                {
                    if (enemy == null) continue;

                    enemy.Act();

                    if (enemyPacing == EEnemyTurnPacing.Sequential)
                    {
                        await _grid.WaitForMovementsAsync(cancellation);
                    }
                    else if (enemyPacing == EEnemyTurnPacing.Staggered && enemyStagger > 0f)
                    {
                        await Awaitable.WaitForSecondsAsync(enemyStagger / _grid.AnimationSpeed, cancellation);
                    }

                    if (version != _turnVersion) return;
                }

                await _grid.WaitForMovementsAsync(cancellation);
                if (version != _turnVersion) return;

                SetTurn(ETurnOwner.Player);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static EnemyController[] GetEnemiesInActingOrder()
        {
            var enemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude);
            var player = FindAnyObjectByType<PlayerCharacter>();
            if (player == null) return enemies;

            var playerPos = player.CurrentGridPos;
            Array.Sort(enemies, (a, b) =>
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
