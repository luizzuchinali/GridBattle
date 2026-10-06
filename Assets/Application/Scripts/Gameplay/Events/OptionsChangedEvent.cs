namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by ProfileService when a player option changes (already saved). Carries the values
    /// of every option after the change, so listeners (audio, tips) can simply apply them all.
    /// </summary>
    public class OptionsChangedEvent
    {
        /// <summary>Locale code chosen by the player, or null for automatic.</summary>
        public string Locale { get; }

        /// <summary>Music volume, 0..1.</summary>
        public float MusicVolume { get; }

        /// <summary>Sound effects volume, 0..1.</summary>
        public float SfxVolume { get; }

        public bool TipsEnabled { get; }

        public OptionsChangedEvent(string locale, float musicVolume, float sfxVolume, bool tipsEnabled)
        {
            Locale = locale;
            MusicVolume = musicVolume;
            SfxVolume = sfxVolume;
            TipsEnabled = tipsEnabled;
        }
    }
}
