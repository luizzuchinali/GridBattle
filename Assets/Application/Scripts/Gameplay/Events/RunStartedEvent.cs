using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Run;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by the run manager when a run begins (<see cref="Resumed"/> false) or is loaded from the save
    /// (true). The map (<see cref="MapOpenedEvent"/>) or the saved battle (<see cref="GridInitializedEvent"/>)
    /// follows right after.
    /// </summary>
    public class RunStartedEvent
    {
        public RunState Run { get; }
        public PlayerCharacterConfig PlayerClass { get; }

        /// <summary>The run was loaded from the save instead of being started now.</summary>
        public bool Resumed { get; }

        public RunStartedEvent(RunState run, PlayerCharacterConfig playerClass, bool resumed)
        {
            Run = run;
            PlayerClass = playerClass;
            Resumed = resumed;
        }
    }
}
