namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Emitido sempre que o XP do PlayerCharacter muda (ganho de XP ou level up).
    /// Consumido pela UI (barra de XP).
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