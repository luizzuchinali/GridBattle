namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by GridController after the board is (re)built and the characters
    /// are spawned, i.e. when a battle starts (or is restored from a save).
    /// </summary>
    public class GridInitializedEvent
    {
        /// <summary>Global turn the battle starts at (above 1 when restoring a saved battle).</summary>
        public int FirstGlobalTurn { get; }

        public GridInitializedEvent(int firstGlobalTurn = 1)
        {
            FirstGlobalTurn = firstGlobalTurn;
        }
    }
}
