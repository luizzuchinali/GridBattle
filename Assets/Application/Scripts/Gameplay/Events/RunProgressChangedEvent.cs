namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the run manager whenever the run's progress shown in the HUD
    /// changes: map depth (GDD: position from 1 up to the final boss) and the
    /// player's persistent HP between nodes. Depth 0 = not in a run.
    /// </summary>
    public class RunProgressChangedEvent
    {
        public int Depth { get; }
        public int FloorCount { get; }
        public int Hp { get; }
        public int MaxHp { get; }

        public RunProgressChangedEvent(int depth, int floorCount, int hp, int maxHp)
        {
            Depth = depth;
            FloorCount = floorCount;
            Hp = hp;
            MaxHp = maxHp;
        }
    }
}
