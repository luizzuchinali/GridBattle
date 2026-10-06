using GridBattle.Core;
using GridBattle.Gameplay.AI;
using GridBattle.Gameplay.Entities.Roles;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Localization;

namespace GridBattle.Gameplay.Entities
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "GridBattle/Characters/Enemy Config", order = 0)]
    public class EnemyConfig : CharacterConfig
    {
        [Header("Template")]
        [SerializeField]
        [Tooltip("Shared enemy prefab template. Only use a dedicated prefab if the enemy needs extra components.")]
        private Enemy prefab;

        [Header("Role")]
        [SerializeField]
        [Tooltip("Role of the enemy (GDD Mechanic 4): shown over the enemy and in the node preview, and used by the battle generator.")]
        private EnemyRoleDefinition role;

        [SerializeField]
        [Tooltip("Boss of the final node (GDD Mechanic 2).")]
        private bool isBoss;

        [Header("Behavior")]
        [SerializeField]
        [Tooltip("Turn AI: actions in priority order. Without a behavior the enemy does not act.")]
        private EnemyBehavior behavior;

        [SerializeField]
        [Tooltip("Short localized text explaining how the enemy acts, shown in its details window and in the glossary.")]
        private LocalizedString behaviorDescription = new();

        [Header("Reward")]
        [SerializeField]
        [Min(0)]
        private int xpReward = 10;

        public Enemy Prefab => prefab;
        public EnemyBehavior Behavior => behavior;

        /// <summary>Role of the enemy, or null if none is set.</summary>
        [CanBeNull]
        public EnemyRoleDefinition Role => role;

        public bool IsBoss => isBoss;

        public LocalizedString BehaviorDescription => behaviorDescription;

        /// <summary>
        /// XP granted to the PlayerCharacter when this enemy dies.
        /// </summary>
        public int XpReward => xpReward;

        /// <summary>Localized description of how the enemy acts, or an empty string if none is set.</summary>
        public string GetBehaviorDescription() => Loc.Get(behaviorDescription, string.Empty);
    }
}
