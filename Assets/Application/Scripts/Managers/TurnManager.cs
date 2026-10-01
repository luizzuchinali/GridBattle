using System;
using GridBattle.Gameplay;
using GridBattle.Gameplay.Controllers;
using GridBattle.Gameplay.Events;
using UnityEngine;

namespace GridBattle.Managers
{
    public enum EEnemyTurnPacing
    {
        /// <summary>Each enemy acts after the previous one finished moving.</summary>
        Sequential,

        /// <summary>All enemies act at once; the turn ends when every move finished.</summary>
        Simultaneous
    }

    /// <summary>
    /// Turn flow: every action consumed by the player (PlayerActionEvent) hands
    /// the turn to the enemies. After the player's move animation, the enemies act
    /// (waiting for their move animations) and then the turn returns to the
    /// player. Raises TurnChangedEvent on every change. Enemy order comes from an
    /// unsorted FindObjectsByType, so it can change between runs.
    /// </summary>
    [RequireComponent(typeof(GridController))]
    public class TurnManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Whether enemies take their turns one after another or all at once.")]
        private EEnemyTurnPacing enemyPacing = EEnemyTurnPacing.Sequential;

        private GridController _grid;
        private int _turnVersion;

        public ETurnOwner CurrentTurn { get; private set; } = ETurnOwner.Player;

        private void Awake()
        {
            _grid = GetComponent<GridController>();
            EventBus.Subscribe<PlayerActionEvent>(OnPlayerAction);
            EventBus.Subscribe<GridInitializedEvent>(OnGridInitialized);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerActionEvent>(OnPlayerAction);
            EventBus.Unsubscribe<GridInitializedEvent>(OnGridInitialized);
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

                var enemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude);
                foreach (var enemy in enemies)
                {
                    if (enemy == null) continue;

                    enemy.Act();

                    if (enemyPacing == EEnemyTurnPacing.Sequential)
                    {
                        await _grid.WaitForMovementsAsync(cancellation);
                        if (version != _turnVersion) return;
                    }
                }

                await _grid.WaitForMovementsAsync(cancellation);
                if (version != _turnVersion) return;

                SetTurn(ETurnOwner.Player);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void SetTurn(ETurnOwner owner)
        {
            CurrentTurn = owner;
            EventBus.Raise(new TurnChangedEvent(owner));
        }
    }
}
