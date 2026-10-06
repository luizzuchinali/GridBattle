using System;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Simulation;
using GridBattle.Gameplay.Turns;
using UnityEngine;

namespace GridBattle.Gameplay
{
    /// <summary>
    /// Decides when a battle ends (GDD Mechanic 2): victory when every enemy is
    /// eliminated (no turn limit), defeat when the player dies. A victory is
    /// announced only after the pending animations and turn blockers (XP orbs,
    /// talent choice) so the last kill is fully resolved first.
    /// </summary>
    [RequireComponent(typeof(GridController))]
    public class BattleController : MonoBehaviour
    {
        private GridController _grid;
        private int _battleVersion;

        /// <summary>The outcome is decided (the turn flow must not continue).</summary>
        public bool IsOver { get; private set; }

        private void Awake()
        {
            _grid = GetComponent<GridController>();
            EventBus.Subscribe<CharacterDiedEvent>(OnCharacterDied);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CharacterDiedEvent>(OnCharacterDied);
        }

        /// <summary>A new battle starts (called by TurnManager when the grid is initialized).</summary>
        public void BeginBattle()
        {
            IsOver = false;
            _battleVersion++;
        }

        private void OnCharacterDied(CharacterDiedEvent e)
        {
            if (IsOver) return;

            if (e.Character is PlayerCharacter)
            {
                IsOver = true;
                EventBus.Raise(new BattleDecidedEvent(false));
                EventBus.Raise(new BattleEndedEvent(false));
            }
            else if (!AnyEnemyAlive())
            {
                IsOver = true;
                EventBus.Raise(new BattleDecidedEvent(true));
                _ = AnnounceVictory(_battleVersion);
            }
        }

        private async Awaitable AnnounceVictory(int version)
        {
            try
            {
                // Simulation: nothing animates, so the announcement would come in the middle of the last kill's
                // event, before the handlers that run after this one credit its XP. A frame is what the animations
                // give the real game.
                if (SimMode.IsActive)
                    await Awaitable.NextFrameAsync(destroyCancellationToken);

                do
                {
                    await _grid.WaitForMovementsAsync(destroyCancellationToken);
                    await TurnBlockers.WaitAsync(destroyCancellationToken);
                } while (_grid.IsAnimatingMovement || TurnBlockers.IsBlocked);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (version != _battleVersion) return;
            EventBus.Raise(new BattleEndedEvent(true));
        }

        /// <summary>Whether any enemy on the board is still alive.</summary>
        public static bool AnyEnemyAlive()
        {
            foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
            {
                if (!enemy.IsDead)
                    return true;
            }

            return false;
        }
    }
}
