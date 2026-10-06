namespace GridBattle.Gameplay.Events
{
    /// <summary>What part of the player profile changed.</summary>
    public enum EProfileChange
    {
        /// <summary>The profile was (re)loaded or reset.</summary>
        Loaded,

        BattlesWon,
        RunEnded,
        TalentDiscovered,
        EnemyFaced,
        ClassesAnnounced,
        TipSeen,
        Options
    }

    /// <summary>
    /// Raised by ProfileService after every change of the player profile (already saved).
    /// The glossary, class selection and options screens refresh from it.
    /// </summary>
    public class ProfileChangedEvent
    {
        public EProfileChange Change { get; }

        public ProfileChangedEvent(EProfileChange change)
        {
            Change = change;
        }
    }
}
