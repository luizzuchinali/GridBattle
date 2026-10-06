using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Entities;

namespace GridBattle.Gameplay.Meta
{
    /// <summary>How the in-game glossary shows an entry (GDD 2.7).</summary>
    public enum EGlossaryEntryState
    {
        /// <summary>Not listed at all.</summary>
        Hidden,

        /// <summary>Listed as "?" (not discovered yet).</summary>
        Unknown,

        /// <summary>Fully shown.</summary>
        Discovered
    }

    /// <summary>
    /// Data for the in-game glossary (GDD 2.7), built from the player profile and
    /// <see cref="MetaSettings"/>: unlocked classes, which talents of a class are revealed or
    /// shown as "?", and which enemies are listed. The glossary screen only decides how to draw it.
    /// </summary>
    public static class GlossaryService
    {
        /// <summary>Classes the player has unlocked, in unlock order (fewest battles first).</summary>
        public static List<PlayerCharacterConfig> GetUnlockedClasses()
        {
            var result = new List<PlayerCharacterConfig>();
            var database = GameDatabase.Instance;
            if (database == null) return result;

            foreach (var config in database.GetAll<PlayerCharacterConfig>())
            {
                if (ProfileService.IsClassUnlocked(config))
                    result.Add(config);
            }

            result.Sort((a, b) => a.BattlesToUnlock != b.BattlesToUnlock
                ? a.BattlesToUnlock.CompareTo(b.BattlesToUnlock)
                : string.CompareOrdinal(a.name, b.name));
            return result;
        }

        /// <summary>Whether a talent of a class is revealed (<see cref="EGlossaryEntryState.Discovered"/>) or shown as "?".</summary>
        public static EGlossaryEntryState GetTalentState(PlayerCharacterConfig cls, string talentId)
        {
            return ProfileService.IsTalentDiscovered(cls, talentId)
                ? EGlossaryEntryState.Discovered
                : EGlossaryEntryState.Unknown;
        }

        /// <summary>
        /// Faced enemies are discovered; the others are "?" or hidden depending on
        /// <see cref="MetaSettings.ShowUnknownEnemies"/>.
        /// </summary>
        public static EGlossaryEntryState GetEnemyState(EnemyConfig enemy)
        {
            if (enemy == null) return EGlossaryEntryState.Hidden;
            if (ProfileService.HasFacedEnemy(enemy)) return EGlossaryEntryState.Discovered;

            return MetaSettings.Current.ShowUnknownEnemies ? EGlossaryEntryState.Unknown : EGlossaryEntryState.Hidden;
        }

        /// <summary>Enemies the glossary lists (discovered and, if allowed, unknown ones), in database order.</summary>
        public static List<EnemyConfig> GetListedEnemies()
        {
            var result = new List<EnemyConfig>();
            var database = GameDatabase.Instance;
            if (database == null) return result;

            foreach (var enemy in database.GetAll<EnemyConfig>())
            {
                if (GetEnemyState(enemy) != EGlossaryEntryState.Hidden)
                    result.Add(enemy);
            }

            return result;
        }
    }
}
