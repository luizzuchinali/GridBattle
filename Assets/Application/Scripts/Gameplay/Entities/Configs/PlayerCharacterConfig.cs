using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    [CreateAssetMenu(fileName = "PlayerCharacterConfig", menuName = "GridBattle/Characters/Player Character Config", order = 1)]
    public class PlayerCharacterConfig : CharacterConfig
    {
        [Header("Template")]
        [SerializeField]
        [Tooltip("Shared playable character prefab template.")]
        private PlayerCharacter prefab;

        [Header("Class")]
        [SerializeField]
        [Tooltip("Class chosen in the menu that resolves to this character.")]
        private ECharacter characterClass;

        [Header("Progression")]
        [SerializeField]
        [Min(1)]
        private int baseXpToLevelUp = 50;

        [SerializeField]
        [Min(0)]
        private int xpToLevelUpGrowthPerLevel = 25;

        public PlayerCharacter Prefab => prefab;
        public ECharacter CharacterClass => characterClass;

        /// <summary>
        /// XP required to go from level 1 to 2. Each following level adds
        /// XpToLevelUpGrowthPerLevel.
        /// </summary>
        public int BaseXpToLevelUp => baseXpToLevelUp;

        /// <summary>
        /// Increase in the XP threshold for each level the character reaches.
        /// </summary>
        public int XpToLevelUpGrowthPerLevel => xpToLevelUpGrowthPerLevel;

        /// <summary>
        /// XP curve: how much XP is needed to go from <paramref name="level"/> to the
        /// next level.
        /// </summary>
        public int GetXpToNextLevel(int level) => baseXpToLevelUp + (level - 1) * xpToLevelUpGrowthPerLevel;
    }
}
