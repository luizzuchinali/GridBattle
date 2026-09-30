using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    public class PlayerCharacterConfig : CharacterConfig
    {
        [SerializeField]
        private ECharacter characterClass;

        [SerializeField]
        private int baseXpToLevelUp = 50;

        [SerializeField]
        private int xpToLevelUpGrowthPerLevel = 25;

        public ECharacter CharacterClass => characterClass;

        /// <summary>
        /// XP necessário para subir do nível 1 ao 2. Níveis seguintes somam
        /// XpToLevelUpGrowthPerLevel por nível.
        /// </summary>
        public int BaseXpToLevelUp => baseXpToLevelUp;

        /// <summary>
        /// Acréscimo no limiar de XP a cada nível que o personagem atinge.
        /// </summary>
        public int XpToLevelUpGrowthPerLevel => xpToLevelUpGrowthPerLevel;
    }
}