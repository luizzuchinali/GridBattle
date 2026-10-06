namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the run manager when the map becomes the current screen: a run starts or resumes on the map, or a
    /// node was resolved. The board has been (or is being) cleared; the UI shows the map from
    /// <c>RunManager.CurrentRun.Map</c> and the nodes from <c>RunManager.GetAvailableNodes()</c>.
    /// </summary>
    public class MapOpenedEvent
    {
        /// <summary>The map was reopened from the save (not reached by playing).</summary>
        public bool Resumed { get; }

        public MapOpenedEvent(bool resumed = false)
        {
            Resumed = resumed;
        }
    }
}
