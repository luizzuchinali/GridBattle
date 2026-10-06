using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Managers.Audio;
using UnityEngine;

namespace GridBattle.Gameplay.Terrain
{
    /// <summary>
    /// Applies terrain effects to the characters standing on hazard and bonus
    /// cells. Owned by the <see cref="GridController"/>, which enables it while
    /// playing. Player and enemies go through the same path: each trigger looks at
    /// the cell under the character and applies the cell's damage and states
    /// (no source) when the cell's trigger matches. Logic is immediate; the cell
    /// pulse and the sound are only feedback.
    /// </summary>
    public sealed class TerrainEffects
    {
        private readonly GridController _grid;
        private bool _enabled;

        public TerrainEffects(GridController grid)
        {
            _grid = grid;
        }

        public void Enable()
        {
            if (_enabled) return;

            _enabled = true;
            EventBus.Subscribe<EntityTurnStartedEvent>(OnTurnStarted);
            EventBus.Subscribe<EntityTurnEndedEvent>(OnTurnEnded);
            EventBus.Subscribe<EntityEnteredCellEvent>(OnEntered);
        }

        public void Disable()
        {
            if (!_enabled) return;

            _enabled = false;
            EventBus.Unsubscribe<EntityTurnStartedEvent>(OnTurnStarted);
            EventBus.Unsubscribe<EntityTurnEndedEvent>(OnTurnEnded);
            EventBus.Unsubscribe<EntityEnteredCellEvent>(OnEntered);
        }

        private void OnTurnStarted(EntityTurnStartedEvent e) => Trigger(e.Character, ETerrainTrigger.OnTurnStart);

        private void OnTurnEnded(EntityTurnEndedEvent e) => Trigger(e.Character, ETerrainTrigger.OnTurnEnd);

        private void OnEntered(EntityEnteredCellEvent e)
        {
            if (e.Entity is Character character)
                Trigger(character, ETerrainTrigger.OnEnter);
        }

        /// <summary>
        /// Forced entry: the character was pushed or pulled and stopped on this cell, so the terrain under it applies
        /// its effect at once, whatever its trigger (damage and states, like a normal trigger). Terrain that triggers
        /// on entering is skipped: the move that put the character there already applied it. The cell's normal
        /// turn-start / turn-end trigger is unchanged and still applies later. Returns whether an effect was applied.
        /// </summary>
        public bool ApplyForcedEntry(Character character)
        {
            if (!_enabled) return false;

            return Trigger(character, null, true);
        }

        /// <summary>
        /// Applies the terrain under <paramref name="character"/> if its trigger is
        /// <paramref name="trigger"/> (any trigger when null, except entering, for forced entries) and it affects
        /// the character's side. Returns whether an effect was applied.
        /// </summary>
        private bool Trigger(Character character, ETerrainTrigger? trigger, bool forced = false)
        {
            if (_grid == null || character == null || character.IsDead) return false;

            var position = character.CurrentGridPos;
            if (!_grid.IsValidPosition(position) || _grid.GetContent(position) != character) return false;

            var terrain = _grid.GetTerrain(position);
            if (terrain == null) return false;
            if (trigger.HasValue ? terrain.Trigger != trigger.Value : terrain.Trigger == ETerrainTrigger.OnEnter)
                return false;
            if (!terrain.HasEffect || !terrain.AffectsCharacter(character)) return false;

            var damage = 0;
            if (terrain.Damage > 0)
                damage = CombatResolver.DealDamage(null, character, terrain.Damage, EDamageKind.Terrain).Damage;

            if (!character.IsDead)
            {
                foreach (var grant in terrain.States)
                {
                    if (grant.IsValid)
                        character.States.Apply(grant);
                }
            }

            EventBus.Raise(new TerrainTriggeredEvent(character, position, terrain, terrain.Trigger, damage, forced));

            var cell = _grid.GetCell(position);
            if (cell != null)
                cell.PlayTerrainPulse();

            AudioManager.Play(terrain.Kind == ETerrainKind.Bonus ? ESfx.BonusCell : ESfx.HazardCell);
            return true;
        }
    }
}
