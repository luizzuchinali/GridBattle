using GridBattle.Gameplay.Entities;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by ProfileService the moment a class is unlocked (GDD 3.2), which can happen in
    /// the middle of a run. The announcement for the player belongs on the end-of-run screen,
    /// through ProfileService.ConsumeNewlyUnlockedClasses().
    /// </summary>
    public class ClassUnlockedEvent
    {
        public PlayerCharacterConfig Config { get; }

        public ClassUnlockedEvent(PlayerCharacterConfig config)
        {
            Config = config;
        }
    }
}
