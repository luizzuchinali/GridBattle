namespace GridBattle.Managers.Audio
{
    /// <summary>
    /// What is going on, as far as the music is concerned (GDD 5.2). Each context
    /// has a track in the <see cref="AudioLibrary"/>; a context without clips can
    /// fall back to another one (the map shares the calm menu track by default).
    /// </summary>
    public enum EMusicContext
    {
        /// <summary>Silence: fades the current music out.</summary>
        None = 0,

        /// <summary>Start screen and main menu (calm track).</summary>
        Menu = 1,

        /// <summary>Run map (shares the calm track unless it gets its own clips).</summary>
        Map = 2,

        /// <summary>Regular battles (can have several tracks to avoid repetition).</summary>
        Battle = 3,

        /// <summary>Final boss battle.</summary>
        Boss = 4
    }
}
