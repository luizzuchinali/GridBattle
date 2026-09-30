using GridBattle.Gameplay.Events;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    public class PlayerCharacter : Character
    {
        public int Level { get; private set; } = 1;
        public int CurrentXp { get; private set; }

        public PlayerCharacterConfig PlayerConfig => Config as PlayerCharacterConfig;

        /// <summary>
        /// Informação exclusiva de PlayerCharacters: a classe escolhida no menu.
        /// Enemies não possuem classe.
        /// </summary>
        public ECharacter Class => PlayerConfig != null ? PlayerConfig.CharacterClass : default;

        /// <summary>
        /// Limiar de XP para o próximo nível: base + crescimento por nível,
        /// ambos configurados no PlayerCharacterConfig.
        /// </summary>
        public int XpToNextLevel
        {
            get
            {
                if (PlayerConfig == null) return int.MaxValue;

                return PlayerConfig.BaseXpToLevelUp +
                       (Level - 1) * PlayerConfig.XpToLevelUpGrowthPerLevel;
            }
        }

        /// <summary>
        /// Ganho de XP. Chamado pelo XpVfxController quando cada célula de XP
        /// chega à barra (não diretamente na morte do inimigo). Emite
        /// PlayerXpChangedEvent para a UI.
        /// </summary>
        public void GainXp(int amount)
        {
            if (IsDead || amount <= 0) return;

            CurrentXp += amount;

            while (CurrentXp >= XpToNextLevel)
            {
                CurrentXp -= XpToNextLevel;
                Level++;
            }

            EventBus.Raise(new PlayerXpChangedEvent(Level, CurrentXp, XpToNextLevel));
        }
    }
}