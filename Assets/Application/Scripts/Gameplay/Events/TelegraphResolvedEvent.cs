using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by <see cref="Enemy"/> right after a telegraphed skill fired: who was standing on the marked cells and
    /// was affected, and whether the skill had been aimed at the player (the enemy AI always aims at the player).
    /// Rule talents react to it (a counterstrike when the strike misses the player).
    /// </summary>
    public class TelegraphResolvedEvent
    {
        public TelegraphResolvedEvent(Enemy caster, SkillTelegraph telegraph, IReadOnlyList<Character> hit)
        {
            Caster = caster;
            Telegraph = telegraph;
            Hit = hit;
        }

        public Enemy Caster { get; }
        public SkillTelegraph Telegraph { get; }

        /// <summary>The characters the skill affected when it fired (on the marked cells, through its target filter).</summary>
        public IReadOnlyList<Character> Hit { get; }

        /// <summary>Whether <paramref name="character"/> was among the characters hit.</summary>
        public bool WasHit(Character character)
        {
            if (character == null || Hit == null) return false;

            foreach (var hit in Hit)
            {
                if (hit == character) return true;
            }

            return false;
        }
    }
}
