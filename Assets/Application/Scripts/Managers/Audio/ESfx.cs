namespace GridBattle.Managers.Audio
{
    /// <summary>
    /// Every sound effect the game can request (GDD 5.2). Values are explicit and
    /// grouped by category so new effects can be appended without changing the
    /// numbers stored in assets.
    /// </summary>
    public enum ESfx
    {
        // Combat
        Walk = 0,
        BasicAttack = 1,

        /// <summary>One cast of a skill (the skill module triggers it; a per-skill clip is a future refinement).</summary>
        SkillCast = 2,

        PlayerHurt = 3,
        EnemyHurt = 4,
        CriticalHit = 5,
        EnemyDeath = 6,
        PlayerDeath = 7,

        // Progression
        XpOrb = 20,
        LevelUp = 21,
        TalentOfferOpen = 22,
        TalentChosen = 23,
        Reroll = 24,
        Ban = 25,
        Skip = 26,

        // States and terrain
        StateApplied = 40,
        StateExpired = 41,
        HazardCell = 42,
        BonusCell = 43,

        // Map
        NodeSelect = 60,
        NodeConfirm = 61,
        Heal = 62,
        ConsumableGained = 63,
        ConsumableUse = 64,

        // Interface
        ButtonTap = 80,
        WindowOpen = 81,
        WindowClose = 82,
        ScreenTransition = 83,

        // Result
        RunVictory = 100,
        RunDefeat = 101,
        ClassUnlocked = 102
    }
}
