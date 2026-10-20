using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.Terrain;
using UnityEngine;

namespace GridBattle.Gameplay.States.Effects
{
    /// <summary>
    /// Rule talent "Domino": when a character the holder pushes or pulls slams into another character, that one is
    /// pushed too, in the same direction (and takes its own collision if it hits something). Chains do not continue
    /// past <see cref="Distance"/> cells or into a third character. Read by <c>DisplacementResolver</c>.
    /// </summary>
    [Serializable]
    public sealed class DominoEffect : StateEffect
    {
        [SerializeField]
        [Min(1)]
        [Tooltip("Cells the character that was hit is pushed.")]
        private int distance = 1;

        public int Distance => Mathf.Max(1, distance);
    }

    /// <summary>
    /// Rule talent "Trail": a character the holder pushes or pulls leaves temporary terrain (Fire, a bog) on every
    /// cell it left on the way (its start cell and the cells it crossed, not the one it stops on). Only plain floor
    /// gets it. Read by <c>DisplacementResolver</c>.
    /// </summary>
    [Serializable]
    public sealed class DisplacementTrailEffect : StateEffect
    {
        [SerializeField]
        [Tooltip("Terrain left on the cells (it must not block movement).")]
        private TerrainDefinition terrain;

        [SerializeField]
        [Min(1)]
        [Tooltip("Rounds the terrain lasts.")]
        private int turns = 2;

        public TerrainDefinition Terrain => terrain;
        public int Turns => Mathf.Max(1, turns);

        /// <summary>Leaves the terrain on the cells a displacement left behind.</summary>
        public void Leave(GridController grid, DisplacementResult result)
        {
            if (terrain == null || terrain.BlocksMovement || grid == null || result == null || result.CellsMoved <= 0)
                return;

            TryPlace(grid, result.Start);
            for (var i = 0; i < result.Path.Count - 1; i++)
                TryPlace(grid, result.Path[i]);
        }

        private void TryPlace(GridController grid, Vector2Int cell)
        {
            if (grid.IsValidPosition(cell) && grid.GetTerrain(cell) == null)
                grid.SetTerrain(cell, terrain, Turns);
        }
    }

    /// <summary>
    /// Rule talent "Slippery ice": a character with one of <see cref="States"/> (Frozen, Chilled) that the holder
    /// pushes slides at least <see cref="SlideDistance"/> cells, until something stops it (collision rules as usual).
    /// Pulls are not changed. Read by <see cref="Displacement.GetDistance"/> (prediction and application alike).
    /// </summary>
    [Serializable]
    public sealed class SlipperyPushEffect : StateEffect
    {
        [SerializeField]
        [Tooltip("States that make the target slide (any of them).")]
        private List<StateDefinition> states = new();

        [SerializeField]
        [Min(1)]
        [Tooltip("Cells a slippery target is pushed (at least).")]
        private int slideDistance = 6;

        public IReadOnlyList<StateDefinition> States => states;
        public int SlideDistance => Mathf.Max(1, slideDistance);

        /// <summary>Whether <paramref name="target"/> slides (it has one of the states).</summary>
        public bool Slides(Character target)
        {
            if (target == null) return false;

            foreach (var state in states)
            {
                if (state != null && target.States.Has(state))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Rule talent "Counterstrike": when an announced enemy strike that was aimed at the holder fires and misses it
    /// (the holder stepped out, or was moved out, of the marked cells), the enemy that struck gains
    /// <see cref="States"/> (Exposed). Read by <see cref="PlayerCharacter"/> on <c>TelegraphResolvedEvent</c>.
    /// </summary>
    [Serializable]
    public sealed class CounterstrikeEffect : StateEffect
    {
        [SerializeField]
        [Tooltip("States applied to the enemy whose strike missed (the grant's duration and stacks).")]
        private List<StateGrant> states = new();

        public IReadOnlyList<StateGrant> States => states;

        /// <summary>Applies the states to the enemy that missed.</summary>
        public void Punish(Character attacker)
        {
            if (attacker == null || attacker.IsDead) return;

            foreach (var grant in states)
            {
                if (grant.IsValid)
                    attacker.States.Apply(grant);
            }
        }
    }

    /// <summary>
    /// Rule talent "Swap step": the holder's walk can also trade places with an adjacent character (8 neighbours)
    /// that can be displaced, instead of walking. Read by <c>PlayerCharacterController</c>.
    /// </summary>
    [Serializable]
    public sealed class SwapStepEffect : StateEffect
    {
    }
}
