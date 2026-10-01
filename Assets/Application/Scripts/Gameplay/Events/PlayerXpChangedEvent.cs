namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised whenever the PlayerCharacter's XP changes (XP gain or level up).
    /// Consumed by the UI (XP bar).
    /// </summary>
    public class PlayerXpChangedEvent
    {
        public int Level { get; }
        public int CurrentXp { get; }
        public int XpToNextLevel { get; }

        public PlayerXpChangedEvent(int level, int currentXp, int xpToNextLevel)
        {
            Level = level;
            CurrentXp = currentXp;
            XpToNextLevel = xpToNextLevel;
        }
    }
}