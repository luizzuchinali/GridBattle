using GridBattle.Gameplay.Entities;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by <see cref="PlayerCharacter.GainXp"/> once per level gained, after
    /// <see cref="PlayerXpChangedEvent"/> (so the XP bar already shows the new level). A kill that crosses
    /// several thresholds raises it several times, in order. The talent module answers each one with a talent
    /// offer (xp_e_niveis: one choice per level, in sequence).
    /// </summary>
    public class PlayerLeveledUpEvent
    {
        public PlayerCharacter Player { get; }

        /// <summary>The level just reached.</summary>
        public int Level { get; }

        public PlayerLeveledUpEvent(PlayerCharacter player, int level)
        {
            Player = player;
            Level = level;
        }
    }
}
