using UnityEngine;

namespace GridBattle.Gameplay.Simulation
{
    /// <summary>
    /// Global switch of the balance simulation (see <see cref="SimulationRunner"/>). While it is active the game
    /// plays at full speed with its real rules and nothing that only exists to be seen or heard: grid animations,
    /// floating texts and cell pulses are skipped, XP is credited at once (no orbs), the enemy turn has no stagger,
    /// audio is muted, tutorial tips are not requested, the UI flow does not navigate or open modals, the run is not
    /// written to disk and the design metrics are not recorded.
    /// <para>
    /// Every hook in the game code is a one-line <c>if (SimMode.IsActive)</c> guard; with the mode off the
    /// game behaves exactly as without it. Game rules and random draws are never touched by the mode.
    /// </para>
    /// </summary>
    public static class SimMode
    {
        /// <summary>Whether a simulation is running.</summary>
        public static bool IsActive { get; private set; }

        /// <summary>
        /// Seed of the next <c>RunManager.StartNewRun</c> (0 = the run settings' seed or a random one). Set by the
        /// runner before every run, so the run settings asset is never modified.
        /// </summary>
        public static ulong SeedOverride { get; set; }

        /// <summary>Turns the mode on. Called by the runner; always pair it with <see cref="End"/>.</summary>
        public static void Begin()
        {
            IsActive = true;
            SeedOverride = 0UL;
        }

        /// <summary>Turns the mode off and clears the seed override.</summary>
        public static void End()
        {
            IsActive = false;
            SeedOverride = 0UL;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsActive = false;
            SeedOverride = 0UL;
        }
    }
}
