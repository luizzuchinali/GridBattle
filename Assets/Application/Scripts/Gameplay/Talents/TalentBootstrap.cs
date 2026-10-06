using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Run;
using UnityEngine;

namespace GridBattle.Gameplay.Talents
{
    /// <summary>
    /// Starts the talent module without any scene object (domain reload is off in Play Mode, so statics are
    /// reset on SubsystemRegistration): registers the <see cref="TalentRunModifier"/> in the run's player hooks
    /// and wires the events that open the offers (level ups, the talent node) and end them (run over, a new
    /// battle).
    /// </summary>
    public static class TalentBootstrap
    {
        private static TalentRunModifier _modifier;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Unhook();
            TalentService.ResetStatics();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Hook();
        }

        private static void Hook()
        {
            Unhook();
            _modifier = new TalentRunModifier();
            RunPlayerHooks.Register(_modifier);

            EventBus.Subscribe<PlayerLeveledUpEvent>(TalentService.OnPlayerLeveledUp);
            EventBus.Subscribe<TalentNodeEnteredEvent>(TalentService.OnTalentNodeEntered);
            EventBus.Subscribe<RunStartedEvent>(TalentService.OnRunStarted);
            EventBus.Subscribe<RunEndedEvent>(TalentService.OnRunEnded);
            EventBus.Subscribe<GridInitializedEvent>(TalentService.OnGridInitialized);
            EventBus.Subscribe<MapOpenedEvent>(OnMapOpened);
            EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
        }

        private static void Unhook()
        {
            if (_modifier != null)
            {
                RunPlayerHooks.Unregister(_modifier);
                _modifier = null;
            }

            EventBus.Unsubscribe<PlayerLeveledUpEvent>(TalentService.OnPlayerLeveledUp);
            EventBus.Unsubscribe<TalentNodeEnteredEvent>(TalentService.OnTalentNodeEntered);
            EventBus.Unsubscribe<RunStartedEvent>(TalentService.OnRunStarted);
            EventBus.Unsubscribe<RunEndedEvent>(TalentService.OnRunEnded);
            EventBus.Unsubscribe<GridInitializedEvent>(TalentService.OnGridInitialized);
            EventBus.Unsubscribe<MapOpenedEvent>(OnMapOpened);
            EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
        }

        private static void OnMapOpened(MapOpenedEvent e)
        {
            if (e.Resumed)
                TalentService.OnResumePoint();
        }

        private static void OnBattleStarted(BattleStartedEvent e)
        {
            if (e.Restored)
                TalentService.OnResumePoint();
        }
    }
}
