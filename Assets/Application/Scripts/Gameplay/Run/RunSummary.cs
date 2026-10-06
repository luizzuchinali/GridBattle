using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using JetBrains.Annotations;

namespace GridBattle.Gameplay.Run
{
    /// <summary>Why a run ended.</summary>
    public enum ERunEndReason
    {
        /// <summary>The final boss was defeated.</summary>
        Victory,

        /// <summary>The player died in a battle.</summary>
        Defeat,

        /// <summary>The player gave up (pause menu).</summary>
        GivenUp
    }

    /// <summary>
    /// What the end-of-run screen needs (GDD 2.1: build summary, level and depth reached) and what was reported
    /// to the profile. Built by the run manager when a run ends and carried by <c>RunEndedEvent</c>.
    /// </summary>
    public sealed class RunSummary
    {
        public ERunEndReason Reason;

        /// <summary>Class played (null only if its config could not be resolved).</summary>
        [CanBeNull]
        public PlayerCharacterConfig PlayerClass;

        /// <summary>Id of the class (PlayerCharacterConfig).</summary>
        public string ClassId;

        /// <summary>Player level at the end.</summary>
        public int Level;

        /// <summary>Deepest map position reached (1 = first node; the floor count means the boss was reached).</summary>
        public int Depth;

        /// <summary>Number of floors of the map (the depth of the final boss).</summary>
        public int FloorCount;

        public int BattlesWon;
        public int EnemiesKilled;
        public int DamageDealt;
        public int DamageTaken;
        public int TurnsPlayed;

        /// <summary>Talents taken, in pick order (the build).</summary>
        public List<TalentRankState> Talents = new();

        /// <summary>Sum of the ranks of the talents taken.</summary>
        public int TalentCount;

        /// <summary>Skills the player had unlocked (SkillDefinition ids).</summary>
        public List<string> SkillIds = new();

        /// <summary>The run's statistics at the end (copy).</summary>
        public RunStatistics Statistics;

        public bool Victory => Reason == ERunEndReason.Victory;
    }
}
