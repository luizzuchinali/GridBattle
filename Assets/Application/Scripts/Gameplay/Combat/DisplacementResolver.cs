using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.Terrain;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Combat
{
    /// <summary>What one displacement is predicted to do to a character, without applying it (AI, simulation bot).</summary>
    public sealed class DisplacementForecast
    {
        /// <summary>The geometric outcome (cells, stop, what blocked it).</summary>
        public DisplacementResult Result;

        /// <summary>Whether the stop is a collision (damage will be dealt).</summary>
        public bool Collided;

        /// <summary>Collision damage the displaced character takes (after defense and modifiers, before shields).</summary>
        public int CollisionDamage;

        /// <summary>The character hit by the displaced one and damaged (null: nobody, or it is the caster).</summary>
        [CanBeNull]
        public Character HitCharacter;

        /// <summary>Collision damage <see cref="HitCharacter"/> takes (before shields).</summary>
        public int HitCharacterDamage;

        /// <summary>Terrain that will apply its effect at once where the character stops, or null.</summary>
        [CanBeNull]
        public TerrainDefinition ForcedTerrain;

        /// <summary>Damage of <see cref="ForcedTerrain"/> to the displaced character (after modifiers, before shields).</summary>
        public int TerrainDamage;
    }

    /// <summary>
    /// Applies pushes and pulls (the rules are in <see cref="Displacement"/>): moves the character at once through
    /// the grid, deals collision damage through <see cref="CombatResolver"/> (so shields, deaths, XP, events and
    /// sounds work as for any hit; attributed to the caster), makes the terrain where the character stops react
    /// immediately and raises <see cref="CharacterDisplacedEvent"/>. Logic is immediate; the slide and the bump
    /// are only visual (skipped by the balance simulation). No random draws.
    /// </summary>
    public static class DisplacementResolver
    {
        /// <summary>
        /// Displaces every target of one skill use, in the order of <see cref="Displacement.OrderTargets"/>, each
        /// one seeing the cells the previous ones left. Dead and immovable targets are skipped. Returns the results
        /// of the characters that were displaced (moved or stopped at once).
        /// </summary>
        public static List<DisplacementResult> Apply(GridController grid, [CanBeNull] Character caster,
            IEnumerable<Character> targets, EDisplacementMode mode, int distance, Vector2Int areaCenter)
        {
            var applied = new List<DisplacementResult>();
            if (grid == null || caster == null || distance <= 0) return applied;

            foreach (var target in Displacement.OrderTargets(targets, mode, caster.CurrentGridPos, areaCenter))
            {
                if (target == null || target.IsDead) continue;

                var casterPos = caster.CurrentGridPos;
                var direction = Displacement.GetDirection(mode, casterPos, target.CurrentGridPos, areaCenter);
                var result = Displacement.Compute(grid, target, direction, distance,
                    mode == EDisplacementMode.TowardCaster ? casterPos : null);
                if (result.Immovable) continue;

                ApplyResult(grid, caster, result);
                applied.Add(result);
            }

            return applied;
        }

        private static void ApplyResult(GridController grid, Character caster, DisplacementResult result)
        {
            var settings = CombatResolver.Settings;
            var target = result.Target;

            if (result.CellsMoved > 0)
                grid.DisplaceEntity(target, result.Final);

            var collisionDamage = 0;
            var hitDamage = 0;
            var collided = result.IsCollision(settings);
            var hitCharacter = result.Block == EDisplacementBlock.Character && result.BlockedBy != caster
                ? result.BlockedBy
                : null;
            if (collided)
            {
                // The bump waits for the slide to end; queue it before the damage that triggers the reaction.
                var delay = grid.GetSlideDuration(result.CellsMoved);
                grid.QueueImpactAnimation(target, result.Direction, delay);
                if (hitCharacter != null)
                    grid.QueueImpactAnimation(hitCharacter, result.Direction, delay);

                var amount = settings.GetCollisionDamage(result.UnspentCells);
                if (amount > 0)
                    collisionDamage = CombatResolver.DealDamage(caster, target, amount, EDamageKind.Collision).Damage;

                if (hitCharacter != null && settings.CollisionHitCharacterDamage > 0)
                {
                    hitDamage = CombatResolver.DealDamage(caster, hitCharacter, settings.CollisionHitCharacterDamage,
                        EDamageKind.Collision).Damage;
                }
            }

            TerrainDefinition forced = null;
            if (result.CellsMoved > 0 && !target.IsDead && grid.ApplyForcedTerrain(target))
                forced = result.FinalTerrain;

            EventBus.Raise(new CharacterDisplacedEvent(target, caster, result.Start, result.Final,
                result.CellsMoved, result.Block, collided, result.BlockedBy, collisionDamage, hitDamage, forced));
        }

        /// <summary>
        /// Predicts a whole use (every target, in application order) without changing anything: where each
        /// character ends, the collision damage, the character hit and the terrain that reacts. Characters that
        /// cannot be displaced are left out. Damage numbers use the real formula without critical hits.
        /// </summary>
        public static List<DisplacementForecast> Predict(GridController grid, Character caster,
            IEnumerable<Character> targets, EDisplacementMode mode, int distance, Vector2Int areaCenter)
        {
            var forecasts = new List<DisplacementForecast>();
            if (grid == null || caster == null || distance <= 0) return forecasts;

            var settings = CombatResolver.Settings;
            foreach (var result in Displacement.Plan(grid, caster, targets, mode, distance, areaCenter))
            {
                if (result.Immovable) continue;

                var forecast = new DisplacementForecast { Result = result, Collided = result.IsCollision(settings) };
                if (forecast.Collided)
                {
                    forecast.CollisionDamage = PredictCollision(caster, result.Target,
                        settings.GetCollisionDamage(result.UnspentCells));
                    if (result.Block == EDisplacementBlock.Character && result.BlockedBy != caster)
                    {
                        forecast.HitCharacter = result.BlockedBy;
                        forecast.HitCharacterDamage = PredictCollision(caster, result.BlockedBy,
                            settings.CollisionHitCharacterDamage);
                    }
                }

                var terrain = result.FinalTerrain;
                if (result.CellsMoved > 0 && terrain != null && terrain.HasEffect &&
                    terrain.AffectsCharacter(result.Target))
                {
                    forecast.ForcedTerrain = terrain;
                    if (terrain.Damage > 0)
                        forecast.TerrainDamage = CombatResolver.PredictDamage(null, result.Target, terrain.Damage,
                            EDamageKind.Terrain);
                }

                forecasts.Add(forecast);
            }

            return forecasts;
        }

        private static int PredictCollision(Character caster, Character target, int baseDamage)
        {
            return CombatResolver.PredictDamage(caster, target, baseDamage, EDamageKind.Collision);
        }
    }
}
