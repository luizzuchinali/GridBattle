using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    public class EnemyConfig : CharacterConfig
    {
        [SerializeField]
        private int xpReward = 10;

        /// <summary>
        /// XP concedido ao PlayerCharacter quando este inimigo morre.
        /// </summary>
        public int XpReward => xpReward;
    }
}