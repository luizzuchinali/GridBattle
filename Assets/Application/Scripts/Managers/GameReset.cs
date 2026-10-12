using GridBattle.Core;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Meta;
using GridBattle.Gameplay.Monetization;
using GridBattle.Gameplay.Simulation;
using UnityEngine;

namespace GridBattle.Managers
{
    /// <summary>
    /// Development tool: puts the game back as if it had just been installed. It ends the run in progress (the
    /// same path as giving up, so the board, the talent pause and the turn flow are cleaned up), deletes the run
    /// save, erases the profile (unlocked classes, glossary, records, tips seen, options, the remove ads purchase
    /// and the forced ad counters), deletes the metrics, applies the default options and raises
    /// <see cref="GameResetEvent"/> (the flow goes back to the main menu). Only in development builds
    /// (<see cref="Debug.isDebugBuild"/>: the Editor and a Development Build).
    /// <para>
    /// The store keeps its own record: a remove ads purchase made with a store account comes back the next time
    /// the store is asked (startup, Restore purchases). Refund the test order in the store to buy it again.
    /// </para>
    /// </summary>
    public static class GameReset
    {
        /// <summary>The reset exists in this build (development builds only, never during the balance simulation).</summary>
        public static bool IsAvailable => Debug.isDebugBuild && !SimMode.IsActive;

        /// <summary>A reset is running: the end of the run it causes shows no end of run screen.</summary>
        public static bool IsResetting { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsResetting = false;
        }

        /// <summary>Erases everything the player has. Returns false when the reset is not available in this build.</summary>
        public static bool ResetEverything()
        {
            if (!IsAvailable) return false;

            IsResetting = true;
            try
            {
                var runs = RunManager.Instance;
                if (runs != null)
                {
                    if (runs.IsRunActive)
                        runs.GiveUp();
                    runs.AbandonSavedRun();
                }
                else
                {
                    SaveSystem.Delete(RunManager.SaveFileName);
                }

                ProfileService.ResetProfile();
                MetricsRecorder.DeleteFiles();
                TutorialService.ResetSession();
                MonetizationService.OnProfileReset();

                // The default options take effect now (volumes through the options event, the automatic language).
                var options = ProfileService.State.Options;
                EventBus.Raise(new OptionsChangedEvent(options.Locale, options.MusicVolume, options.SfxVolume,
                    ProfileService.TipsEnabled));
                ProfileService.ApplyLocale();

                Debug.Log("Game reset: the run, the profile (with the remove ads purchase) and the metrics were erased.");
            }
            finally
            {
                IsResetting = false;
            }

            EventBus.Raise(new GameResetEvent());
            return true;
        }
    }
}
