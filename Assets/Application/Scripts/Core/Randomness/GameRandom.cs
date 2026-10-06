using UnityEngine;

namespace GridBattle.Core.Randomness
{
    /// <summary>
    /// Access to the random streams of the current run. Outside a run (e.g. the
    /// grid started from the editor) a throwaway clock-seeded state is used.
    /// </summary>
    public static class GameRandom
    {
        private static RunRandom _active;
        private static RunRandom _fallback;

        /// <summary>The current run's random state. Set by the run manager.</summary>
        public static RunRandom Active
        {
            get
            {
                if (_active != null) return _active;
                return _fallback ??= new RunRandom(RunRandom.CreateSeed());
            }
            set => _active = value;
        }

        public static Rng Stream(ERandomStream stream) => Active.Get(stream);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _active = null;
            _fallback = null;
        }
    }
}
