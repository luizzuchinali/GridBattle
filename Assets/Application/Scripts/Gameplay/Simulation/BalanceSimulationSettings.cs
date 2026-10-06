using UnityEngine;

namespace GridBattle.Gameplay.Simulation
{
    /// <summary>
    /// Inspector-editable configuration of the balance simulator (menu GridBattle/Simulation/Run Balance
    /// Simulation): which classes to play, how many runs, the seeds, the bots' policies and the safety limits. It is a
    /// tool setting, not a game system: it is not an <c>IGameSettings</c> and nothing in the game reads it.
    /// </summary>
    [CreateAssetMenu(fileName = "BalanceSimulationSettings", menuName = "GridBattle/Settings/Balance Simulation Settings",
        order = 90)]
    public sealed class BalanceSimulationSettings : ScriptableObject
    {
        [SerializeField]
        private SimulationOptions options = new();

        /// <summary>The batch the asset describes. Clone it (<see cref="SimulationOptions.Clone"/>) before tweaking it from a script.</summary>
        public SimulationOptions Options => options;
    }
}
