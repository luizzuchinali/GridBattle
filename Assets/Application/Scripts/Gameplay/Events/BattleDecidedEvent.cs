namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by BattleController the moment a battle's outcome is decided (last
    /// enemy or the player died), before the pending animations, XP orbs and
    /// talent choice finish. Nobody can act from here on; BattleEndedEvent follows
    /// once everything is resolved.
    /// </summary>
    public class BattleDecidedEvent
    {
        public bool Victory { get; }

        public BattleDecidedEvent(bool victory)
        {
            Victory = victory;
        }
    }
}
