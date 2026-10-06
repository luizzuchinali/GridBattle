using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay.AI
{
    /// <summary>
    /// Data for an enemy's turn, passed to each <see cref="EnemyAction"/>.
    /// </summary>
    public readonly struct EnemyTurnContext
    {
        public Enemy Self { get; }
        public GridController Grid { get; }
        public PlayerCharacter Target { get; }

        /// <summary>Cooldowns and counters of <see cref="Self"/> (never null).</summary>
        public EnemyMemory Memory { get; }

        public EnemyTurnContext(Enemy self, GridController grid, PlayerCharacter target, EnemyMemory memory = null)
        {
            Self = self;
            Grid = grid;
            Target = target;
            Memory = memory ?? new EnemyMemory();
        }

        /// <summary>
        /// Living allies of <see cref="Self"/> (the other enemies), in a
        /// deterministic order (row, then column). Enemies are always allied with
        /// each other.
        /// </summary>
        public List<Enemy> GetAllies(bool includeSelf = false)
        {
            var allies = new List<Enemy>();
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
            {
                if (enemy == null || enemy.IsDead) continue;
                if (!includeSelf && enemy == Self) continue;
                allies.Add(enemy);
            }

            allies.Sort((a, b) =>
            {
                var byY = a.CurrentGridPos.y.CompareTo(b.CurrentGridPos.y);
                return byY != 0 ? byY : a.CurrentGridPos.x.CompareTo(b.CurrentGridPos.x);
            });
            return allies;
        }
    }
}
