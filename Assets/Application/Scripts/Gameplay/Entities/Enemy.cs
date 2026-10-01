namespace GridBattle.Gameplay.Entities
{
    /// <summary>
    /// Enemy: attributes, behavior (AI) and reward come from the
    /// <see cref="Entities.EnemyConfig"/>. Creating a new enemy means creating an
    /// EnemyConfig asset, not a prefab.
    /// </summary>
    public class Enemy : Character
    {
        public EnemyConfig EnemyConfig => Config as EnemyConfig;
    }
}
