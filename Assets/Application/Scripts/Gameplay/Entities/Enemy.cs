using GridBattle.Gameplay.Entities.Roles;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    /// <summary>
    /// Enemy: attributes, behavior (AI) and reward come from the
    /// <see cref="Entities.EnemyConfig"/>. Creating a new enemy means creating an
    /// EnemyConfig asset, not a prefab.
    /// </summary>
    public class Enemy : Character
    {
        private int? _xpRewardOverride;

        public EnemyConfig EnemyConfig => Config as EnemyConfig;

        /// <summary>Role of this enemy (from its config), or null.</summary>
        [CanBeNull]
        public EnemyRoleDefinition Role => EnemyConfig != null ? EnemyConfig.Role : null;

        /// <summary>
        /// XP granted when this enemy dies: the config value unless the battle
        /// generator set another one (e.g. scaled by depth, or 0 for summoned
        /// enemies).
        /// </summary>
        public int XpReward => _xpRewardOverride ?? (EnemyConfig != null ? EnemyConfig.XpReward : 0);

        /// <summary>Whether another enemy created this one during the battle (not part of the generated encounter).</summary>
        public bool IsSummoned { get; private set; }

        /// <summary>
        /// The enemy that summoned this one, if it is known. Null for a summoned enemy
        /// restored from a save (only <see cref="IsSummoned"/> is saved) or whose
        /// summoner died and was destroyed.
        /// </summary>
        [CanBeNull]
        public Enemy Summoner { get; private set; }

        public void SetXpReward(int xpReward) => _xpRewardOverride = Mathf.Max(0, xpReward);

        /// <summary>
        /// Marks this enemy as summoned during the battle. Pass the summoner when it
        /// is known (null when restoring a save).
        /// </summary>
        public void MarkSummoned([CanBeNull] Enemy summoner = null)
        {
            IsSummoned = true;
            Summoner = summoner;
        }

        /// <summary>
        /// Applies the config like any character, then refreshes the role and state
        /// icons shown over the enemy.
        /// </summary>
        public override void Initialize(CharacterConfig characterConfig, CharacterScaling scaling)
        {
            base.Initialize(characterConfig, scaling);

            if (TryGetComponent(out EnemyIconsView icons))
                icons.Bind(this);
        }
    }
}
