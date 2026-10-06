using System.Collections.Generic;
using GridBattle.Gameplay.AI;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Run;
using UnityEngine;

namespace GridBattle.Gameplay.Controllers
{
    /// <summary>
    /// Runs the enemy's turn by delegating to the config's <see cref="EnemyBehavior"/>.
    /// The behavior (action priority and types) is data, not code. Owns the
    /// enemy's <see cref="EnemyMemory"/> (action cooldowns), which goes down at the
    /// end of each of this enemy's own turns.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyController : CharacterControllerBase<Enemy>
    {
        private readonly EnemyMemory _memory = new();
        private PlayerCharacter _target;

        /// <summary>Cooldowns and counters of this enemy.</summary>
        public EnemyMemory Memory => _memory;

        protected override void Awake()
        {
            base.Awake();
            EventBus.Subscribe<EntityTurnEndedEvent>(OnEntityTurnEnded);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<EntityTurnEndedEvent>(OnEntityTurnEnded);
        }

        public void Act()
        {
            if (Owner.IsDead) return;

            if (_target == null)
                _target = FindAnyObjectByType<PlayerCharacter>();
            if (_target == null) return;

            var config = Owner.EnemyConfig;
            if (config == null || config.Behavior == null) return;

            config.Behavior.TakeTurn(new EnemyTurnContext(Owner, Grid, _target, _memory));
        }

        /// <summary>Snapshot of the AI memory for saves.</summary>
        public List<CounterState> CaptureMemory() => _memory.Capture();

        /// <summary>Restores the AI memory from a saved snapshot.</summary>
        public void RestoreMemory(List<CounterState> state) => _memory.Restore(state);

        private void OnEntityTurnEnded(EntityTurnEndedEvent e)
        {
            if (e.Character == Owner)
                _memory.Tick();
        }
    }
}
