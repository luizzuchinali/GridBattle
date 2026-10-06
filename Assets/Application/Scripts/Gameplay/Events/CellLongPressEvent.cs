namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by a <see cref="Cell"/> on a long tap (touch held for
    /// <c>GameplayInputSettings.LongPressSeconds</c>) or a right click: the player asked for the
    /// details of what is on the cell. The tap that ends a long press is suppressed (no
    /// <see cref="CellTapEvent"/>). Opening the details never consumes the turn.
    /// </summary>
    public class CellLongPressEvent
    {
        public Cell Cell { get; }

        public CellLongPressEvent(Cell cell)
        {
            Cell = cell;
        }
    }
}
