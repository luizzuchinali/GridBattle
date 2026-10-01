using GridBattle.Gameplay.Events;

namespace GridBattle.Gameplay.Entities
{
    public class PlayerCharacter : Character
    {
        public int Level { get; private set; } = 1;
        public int CurrentXp { get; private set; }

        // The screen changes as soon as the player dies.
        protected override bool PlaysDeathEffect => false;

        public PlayerCharacterConfig PlayerConfig => Config as PlayerCharacterConfig;

        /// <summary>
        /// PlayerCharacter-only information: the class chosen in the menu.
        /// Enemies have no class.
        /// </summary>
        public ECharacter Class => PlayerConfig != null ? PlayerConfig.CharacterClass : default;

        /// <summary>
        /// XP threshold for the next level, following the curve configured in
        /// PlayerCharacterConfig.
        /// </summary>
        public int XpToNextLevel => PlayerConfig != null ? PlayerConfig.GetXpToNextLevel(Level) : int.MaxValue;

        public override void Initialize(CharacterConfig characterConfig)
        {
            base.Initialize(characterConfig);

            // New character = new progression. Notifies the UI so it doesn't keep the
            // XP bar state from a previous run.
            EventBus.Raise(new PlayerXpChangedEvent(Level, CurrentXp, XpToNextLevel));
        }

        /// <summary>
        /// XP gain. Called by XpRewardSystem as each XP packet is delivered
        /// (not directly when the enemy dies). Raises PlayerXpChangedEvent for the UI.
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
