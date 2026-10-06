namespace GridBattle.Gameplay.Meta
{
    /// <summary>
    /// Situations that can show a contextual tutorial tip the first time they happen
    /// (interface 4.4). Modules report them with <see cref="TutorialService.Notify"/>.
    /// </summary>
    public enum ETutorialTrigger
    {
        /// <summary>A battle starts (raised automatically on GridInitializedEvent): moving, attacking, XP.</summary>
        FirstBattle,

        /// <summary>The map is shown for the first time.</summary>
        FirstMap,

        /// <summary>The first talent offer (level up): choosing while paused, reroll, ban and skip.</summary>
        FirstLevelUp,

        /// <summary>The first heal node is reached.</summary>
        FirstHealNode,

        /// <summary>The first talent node is reached.</summary>
        FirstTalentNode,

        /// <summary>The first consumable node is reached.</summary>
        FirstConsumableNode,

        /// <summary>The player long-presses (or right-clicks) an entity for the first time.</summary>
        FirstLongPress,

        /// <summary>The first skill is unlocked (the skill bar shows a skill).</summary>
        FirstSkill,

        /// <summary>The first battle is won (raised automatically on BattleEndedEvent): HP persists between battles.</summary>
        FirstBattleWon
    }
}
