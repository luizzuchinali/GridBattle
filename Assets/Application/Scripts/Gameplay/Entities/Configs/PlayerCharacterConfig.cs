using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    public class PlayerCharacterConfig : CharacterConfig
    {
        [SerializeField]
        private ECharacter characterClass;

        public ECharacter CharacterClass => characterClass;
    }
}