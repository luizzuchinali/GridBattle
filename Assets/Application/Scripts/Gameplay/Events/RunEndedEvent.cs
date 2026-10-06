using GridBattle.Gameplay.Run;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the run manager when a run ends: victory (final boss defeated), defeat (the player died) or the
    /// player gave up. The save was deleted and the profile updated; the UI shows the end screen with the
    /// <see cref="Summary"/> (and the classes unlocked: <c>ProfileService.ConsumeNewlyUnlockedClasses()</c>).
    /// </summary>
    public class RunEndedEvent
    {
        public RunSummary Summary { get; }

        public bool Victory => Summary.Victory;

        public RunEndedEvent(RunSummary summary)
        {
            Summary = summary;
        }
    }
}
