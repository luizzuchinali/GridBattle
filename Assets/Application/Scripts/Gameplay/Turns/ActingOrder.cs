using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay.Turns
{
    /// <summary>
    /// The order the enemies act in during the enemy phase (Into the Breach style): fixed for the whole battle, so the
    /// player can plan around it. It is the enemy's place in the battle (<see cref="Enemy.ActOrder"/>: the generated
    /// list first, then summoned enemies in the order they arrived), ties broken by row then column. The turn flow,
    /// the numbers over the enemies and on their telegraphed areas, the details window and the simulation bot all
    /// read it from here.
    /// </summary>
    public static class ActingOrder
    {
        /// <summary>The living enemies on the board, in acting order.</summary>
        public static List<Enemy> GetEnemies()
        {
            var enemies = new List<Enemy>();
            foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
            {
                if (enemy != null && !enemy.IsDead)
                    enemies.Add(enemy);
            }

            enemies.Sort(Compare);
            return enemies;
        }

        /// <summary>The place of <paramref name="enemy"/> in the order among the living enemies (1 = first), or 0.</summary>
        public static int GetRank(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead) return 0;

            var enemies = GetEnemies();
            var index = enemies.IndexOf(enemy);
            return index >= 0 ? index + 1 : 0;
        }

        public static int Compare(Enemy a, Enemy b)
        {
            var byOrder = a.ActOrder.CompareTo(b.ActOrder);
            if (byOrder != 0) return byOrder;

            var posA = a.CurrentGridPos;
            var posB = b.CurrentGridPos;
            var byY = posA.y.CompareTo(posB.y);
            return byY != 0 ? byY : posA.x.CompareTo(posB.x);
        }
    }
}
