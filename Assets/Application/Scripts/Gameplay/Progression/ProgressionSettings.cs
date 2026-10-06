using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Gameplay.Progression
{
    /// <summary>
    /// The XP curve and the level cap, shared by every class (xp_e_niveis.md). The curve is global on purpose:
    /// it is tuned against the XP of the battles by depth (see <c>BattleGenerationSettings</c>), not per class.
    /// Open questions of the design document are fields here with neutral defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "ProgressionSettings", menuName = "GridBattle/Settings/Progression Settings", order = 7)]
    public sealed class ProgressionSettings : ScriptableObject, IGameSettings
    {
        [Header("XP curve (xp_e_niveis.md, current curve)")]
        [SerializeField]
        [Min(1)]
        [Tooltip("XP required to go from level 1 to level 2.")]
        private int baseXpToLevelUp = 50;

        [SerializeField]
        [Min(0)]
        [Tooltip("Increase of the XP threshold for each level the character reaches: XpToNextLevel(level) = Base + (level - 1) x Growth.")]
        private int xpToLevelUpGrowthPerLevel = 25;

        [Header("Level cap")]
        [SerializeField]
        [Min(1)]
        [Tooltip("Open question (xp_e_niveis, maximum level): the run's maximum level (the GDD's example is 30). XP gained at the cap gives no more levels, and therefore no more talents.")]
        private int maxLevel = 30;

        [Header("Leftover XP")]
        [SerializeField]
        [Tooltip("Open question (xp_e_niveis, curve): XP above the threshold is kept for the next level (as implemented so far). Off = it is discarded when the level is gained.")]
        private bool carryOverLeftoverXp = true;

        [Header("Level up")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Fraction of the max HP restored for each level gained (1 = full heal, 0 = none). User decision (2026-10-06): full heal.")]
        private float levelUpHealFraction = 1f;

        /// <summary>XP needed to go from level 1 to 2.</summary>
        public int BaseXpToLevelUp => Mathf.Max(1, baseXpToLevelUp);

        /// <summary>Increase of the threshold for each level reached.</summary>
        public int XpToLevelUpGrowthPerLevel => Mathf.Max(0, xpToLevelUpGrowthPerLevel);

        /// <summary>Highest level of a run.</summary>
        public int MaxLevel => Mathf.Max(1, maxLevel);

        /// <summary>Whether the XP above a threshold carries over to the next level.</summary>
        public bool CarryOverLeftoverXp => carryOverLeftoverXp;

        /// <summary>Fraction of the max HP restored for each level gained.</summary>
        public float LevelUpHealFraction => Mathf.Clamp01(levelUpHealFraction);

        /// <summary>The settings asset registered in <see cref="GameSettings"/> (defaults if missing).</summary>
        public static ProgressionSettings Current => GameSettings.Get<ProgressionSettings>();

        /// <summary>
        /// XP needed to go from <paramref name="level"/> to the next level
        /// (<c>Base + (level - 1) x Growth</c>; the same value is returned at the cap, where it is not used).
        /// </summary>
        public int GetXpToNextLevel(int level) =>
            BaseXpToLevelUp + (Mathf.Max(1, level) - 1) * XpToLevelUpGrowthPerLevel;

        /// <summary>Whether <paramref name="level"/> is the level cap.</summary>
        public bool IsMaxLevel(int level) => level >= MaxLevel;

        /// <summary>Total XP needed to reach <paramref name="level"/> from level 1.</summary>
        public int GetTotalXpToReach(int level)
        {
            var total = 0;
            for (var current = 1; current < Mathf.Min(level, MaxLevel); current++)
                total += GetXpToNextLevel(current);
            return total;
        }
    }
}
