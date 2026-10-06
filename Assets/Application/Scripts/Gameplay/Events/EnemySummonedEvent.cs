using GridBattle.Gameplay.Entities;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised when an enemy creates another one during the battle (summoner role).
    /// The summoned enemy is already on the grid and marked
    /// <see cref="Enemy.IsSummoned"/>.
    /// </summary>
    public class EnemySummonedEvent
    {
        public Enemy Summoner { get; }
        public Enemy Minion { get; }

        public EnemySummonedEvent(Enemy summoner, Enemy minion)
        {
            Summoner = summoner;
            Minion = minion;
        }
    }
}
