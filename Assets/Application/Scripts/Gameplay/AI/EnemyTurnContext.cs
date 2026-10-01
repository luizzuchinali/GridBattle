using GridBattle.Gameplay.Entities;

namespace GridBattle.Gameplay.AI
{
    /// <summary>
    /// Data for an enemy's turn, passed to each <see cref="EnemyAction"/>.
    /// </summary>
    public readonly struct EnemyTurnContext
    {
        public Enemy Self { get; }
        public GridController Grid { get; }
        public PlayerCharacter Target { get; }

        public EnemyTurnContext(Enemy self, GridController grid, PlayerCharacter target)
        {
            Self = self;
            Grid = grid;
            Target = target;
        }
    }
}
