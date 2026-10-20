using System.Collections.Generic;
using GridBattle.Gameplay.AI;
using GridBattle.Gameplay.Combat;
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
    /// end of each of this enemy's own turns. A telegraphed skill being prepared
    /// (<see cref="Enemy.Telegraph"/>) is resolved before the behavior runs: the enemy fires, then walks and prepares
    /// the next one.
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
                _target = PlayerCharacter.Active;
            if (_target == null) return;

            // A telegraphed skill being prepared comes first. Firing it uses nothing by default (decision of
            // 2026-10-19: fire, walk, then prepare the next strike; CombatSettings.FiringUsesAction restores the old
            // rule); still waiting uses the action, and the walk unless the skill lets the enemy move.
            var walkUsed = false;
            var actionUsed = false;
            var telegraph = Owner.Telegraph;
            if (telegraph != null && Owner.AdvanceTelegraph(Grid))
            {
                var waiting = Owner.Telegraph != null;
                actionUsed = waiting || CombatResolver.Settings.FiringUsesAction;
                walkUsed = waiting && !telegraph.Skill.CanMoveWhileWindingUp;
            }

            if (Owner.IsDead || (walkUsed && actionUsed)) return;

            var config = Owner.EnemyConfig;
            if (config == null || config.Behavior == null) return;

            config.Behavior.TakeTurn(new EnemyTurnContext(Owner, Grid, _target, _memory), walkUsed, actionUsed);
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
