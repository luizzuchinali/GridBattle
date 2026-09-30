namespace GridBattle.Gameplay.Entities
{
    public class PlayerCharacter : Character
    {
        public PlayerCharacterConfig PlayerConfig => Config as PlayerCharacterConfig;

        /// <summary>
        /// Informação exclusiva de PlayerCharacters: a classe escolhida no menu.
        /// Enemies não possuem classe.
        /// </summary>
        public ECharacter Class => PlayerConfig != null ? PlayerConfig.CharacterClass : default;
    }
}