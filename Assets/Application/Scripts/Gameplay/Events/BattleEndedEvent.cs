namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by BattleController when every enemy is eliminated (victory) or the player dies (defeat), after the pending animations and turn blockers (XP orbs, talent choice).
    /// </summary>
    public class BattleEndedEvent
    {
        public bool Victory { get; }

        public BattleEndedEvent(bool victory)
        {
            Victory = victory;
        }
    }
}
