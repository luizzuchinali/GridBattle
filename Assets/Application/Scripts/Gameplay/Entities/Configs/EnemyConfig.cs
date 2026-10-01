using GridBattle.Gameplay.AI;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "GridBattle/Characters/Enemy Config", order = 0)]
    public class EnemyConfig : CharacterConfig
    {
        [Header("Template")]
        [SerializeField]
        [Tooltip("Shared enemy prefab template. Only use a dedicated prefab if the enemy needs extra components.")]
        private Enemy prefab;

        [Header("Behavior")]
        [SerializeField]
        [Tooltip("Turn AI: actions in priority order. Without a behavior the enemy does not act.")]
        private EnemyBehavior behavior;

        [Header("Reward")]
        [SerializeField]
        [Min(0)]
        private int xpReward = 10;

        public Enemy Prefab => prefab;
        public EnemyBehavior Behavior => behavior;

        /// <summary>
        /// XP granted to the PlayerCharacter when this enemy dies.
        /// </summary>
        public int XpReward => xpReward;
    }
}
