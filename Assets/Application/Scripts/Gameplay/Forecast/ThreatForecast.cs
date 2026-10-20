using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Turns;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Forecast
{
    /// <summary>One prepared enemy strike as it would land: whose, when its caster acts, on which cells.</summary>
    public sealed class TelegraphForecast
    {
        public TelegraphForecast(SkillTelegraph telegraph, Enemy caster, int order)
        {
            Telegraph = telegraph;
            Caster = caster;
            Order = order;
        }

        public SkillTelegraph Telegraph { get; }
        public Enemy Caster { get; }

        /// <summary>Place of the caster in the acting order (1 = acts first), see <see cref="ActingOrder"/>.</summary>
        public int Order { get; }

        /// <summary>The marked cells, inside the grid (moved with the caster when a forecast action displaces it).</summary>
        public List<Vector2Int> Cells { get; } = new();

        /// <summary>The aimed cell (moved like the cells), where the order number is drawn.</summary>
        public Vector2Int TargetPos { get; set; }

        /// <summary>Whether it fires in the next enemy phase (its caster's next turn).</summary>
        public bool FiresNext => Telegraph.TurnsLeft <= 1;
    }

    /// <summary>
    /// What the enemies' prepared strikes (telegraphed skills) threaten in the next enemy phase: each strike in the
    /// acting order with its cells, the cells where a character would be hit and the damage each character would
    /// take (strikes that fire in a later phase too; no critical hit, before shields). With an
    /// <see cref="ActionForecast"/> it answers "and after this action?": characters stand where the action leaves
    /// them, strikes that follow their caster move with it, and the strikes of enemies the action kills or stuns are
    /// gone.
    /// Pure: reads the board, changes nothing.
    /// </summary>
    public sealed class ThreatForecast
    {
        public List<TelegraphForecast> Telegraphs { get; } = new();

        /// <summary>Every marked cell.</summary>
        public HashSet<Vector2Int> MarkedCells { get; } = new();

        /// <summary>Marked cells where a character the strike affects would stand.</summary>
        public HashSet<Vector2Int> HitCells { get; } = new();

        /// <summary>Damage each character would take from the strikes.</summary>
        public Dictionary<Character, int> Damage { get; } = new();

        /// <summary>Characters the strikes would kill (damage at least their HP plus shields).</summary>
        public HashSet<Character> Killed { get; } = new();

        /// <summary>The place in the acting order of every living enemy (1 = acts first).</summary>
        public Dictionary<Enemy, int> Order { get; } = new();

        public static ThreatForecast Compute(GridController grid, [CanBeNull] ActionForecast action = null)
        {
            var forecast = new ThreatForecast();
            if (grid == null) return forecast;

            var enemies = ActingOrder.GetEnemies();
            for (var i = 0; i < enemies.Count; i++)
                forecast.Order[enemies[i]] = i + 1;

            // Who stands where once the action is done (the enemies it kills are gone).
            var characters = new List<(Character Character, Vector2Int Position)>();
            var player = PlayerCharacter.Active;
            if (player != null && !player.IsDead)
                characters.Add((player, action != null ? action.GetPosition(player) : player.CurrentGridPos));
            foreach (var enemy in enemies)
            {
                if (action != null && action.Killed.Contains(enemy)) continue;
                characters.Add((enemy, action != null ? action.GetPosition(enemy) : enemy.CurrentGridPos));
            }

            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                var telegraph = enemy.Telegraph;
                if (telegraph == null || (action != null && action.Killed.Contains(enemy))) continue;
                if (action != null && telegraph.Skill.InterruptibleByStates && action.Interrupted.Contains(enemy)) continue;

                var delta = Vector2Int.zero;
                if (action != null && action.Finals.TryGetValue(enemy, out var final))
                {
                    // A forced move cancels the strikes that say so; the others follow their caster when they say so.
                    if (telegraph.Skill.CancelledByDisplacement) continue;
                    if (telegraph.Skill.TelegraphFollowsCaster)
                        delta = final - enemy.CurrentGridPos;
                }

                var strike = new TelegraphForecast(telegraph, enemy, i + 1) { TargetPos = telegraph.TargetPos + delta };
                foreach (var cell in telegraph.Cells)
                {
                    var moved = cell + delta;
                    if (grid.IsValidPosition(moved))
                        strike.Cells.Add(moved);
                }

                forecast.Telegraphs.Add(strike);
                forecast.MarkedCells.UnionWith(strike.Cells);

                foreach (var (character, position) in characters)
                {
                    if (!strike.Cells.Contains(position) ||
                        !SkillTargeting.IsAffected(enemy, character, telegraph.Skill.Affects)) continue;

                    forecast.HitCells.Add(position);
                    var damage = telegraph.PredictDamage(character);
                    if (damage <= 0) continue;

                    forecast.Damage.TryGetValue(character, out var total);
                    total += damage;
                    forecast.Damage[character] = total;

                    // What the action takes first counts too.
                    var taken = 0;
                    if (action != null)
                        action.Damage.TryGetValue(character, out taken);
                    if (total + taken >= character.Current + character.States.TotalShield)
                        forecast.Killed.Add(character);
                }
            }

            return forecast;
        }
    }
}
