using GridBattle.Gameplay.Entities;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by <see cref="GridController.SwapCharacters"/> after two characters traded cells (a skill such as Shadow
    /// Step, a talent that turns the walk into a swap). Both moves already happened.
    /// </summary>
    public class CharactersSwappedEvent
    {
        public CharactersSwappedEvent(Character first, Character second, Vector2Int firstFrom, Vector2Int secondFrom,
            [CanBeNull] Character source)
        {
            First = first;
            Second = second;
            FirstFrom = firstFrom;
            SecondFrom = secondFrom;
            Source = source;
        }

        /// <summary>The character that started the swap (usually the one using the skill).</summary>
        public Character First { get; }

        /// <summary>The other character.</summary>
        public Character Second { get; }

        /// <summary>Where <see cref="First"/> was (now <see cref="Second"/>'s cell).</summary>
        public Vector2Int FirstFrom { get; }

        /// <summary>Where <see cref="Second"/> was (now <see cref="First"/>'s cell).</summary>
        public Vector2Int SecondFrom { get; }

        /// <summary>Who caused the swap (the skill's caster), or null.</summary>
        [CanBeNull]
        public Character Source { get; }
    }
}
