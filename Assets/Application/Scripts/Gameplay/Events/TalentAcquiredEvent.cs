using GridBattle.Gameplay.Talents;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the talent module when a talent takes effect (its states were applied to the player, its skill
    /// unlocked). The acquisition visual effect and the build displays listen to it. Raised before
    /// <see cref="TalentOfferResolvedEvent"/>.
    /// </summary>
    public class TalentAcquiredEvent
    {
        public TalentDefinition Talent { get; }

        /// <summary>Rank after taking it.</summary>
        public int Rank { get; }

        /// <summary>The talent unlocked a skill.</summary>
        public bool UnlockedSkill { get; }

        public TalentAcquiredEvent(TalentDefinition talent, int rank, bool unlockedSkill)
        {
            Talent = talent;
            Rank = rank;
            UnlockedSkill = unlockedSkill;
        }
    }
}
