using System.Collections.Generic;
using GridBattle.Gameplay.Run;

namespace GridBattle.Gameplay.Simulation
{
    /// <summary>How a simulated run ended.</summary>
    public enum ERunResult
    {
        /// <summary>The final boss was defeated.</summary>
        Victory,

        /// <summary>The player died.</summary>
        Defeat,

        /// <summary>The simulator stopped the run (stuck battle, turn limit, time limit, bot without a legal action).</summary>
        Aborted
    }

    /// <summary>A battle node of a generated map (the data needed to compare XP between difficulties).</summary>
    public readonly struct MapNodeInfo
    {
        public MapNodeInfo(int depth, EMapNodeType type, EBattleDifficulty difficulty, int totalXp, int enemyCount)
        {
            Depth = depth;
            Type = type;
            Difficulty = difficulty;
            TotalXp = totalXp;
            EnemyCount = enemyCount;
        }

        public int Depth { get; }
        public EMapNodeType Type { get; }
        public EBattleDifficulty Difficulty { get; }

        /// <summary>XP the battle grants (the generated spec's total, what the node preview shows).</summary>
        public int TotalXp { get; }

        public int EnemyCount { get; }
    }

    /// <summary>What happened in one battle of a simulated run.</summary>
    public sealed class BattleRecord
    {
        public string ClassName;
        public int RunIndex;
        public long Seed;
        public int NodeId;
        public int Depth;
        public EMapNodeType NodeType;
        public EBattleDifficulty Difficulty;

        /// <summary>Config names of the enemies the battle started with.</summary>
        public List<string> EnemyNames = new();

        /// <summary>Role asset names of the enemies the battle started with (same order as <see cref="EnemyNames"/>).</summary>
        public List<string> RoleNames = new();

        /// <summary>Terrain cells of the battle by kind: obstacles, hazards and bonus cells.</summary>
        public int ObstacleCells;

        public int HazardCells;
        public int BonusCells;

        /// <summary>Enemies summoned during the battle (not part of the generated encounter).</summary>
        public int Summoned;

        public int PlannedXp;

        /// <summary>Total XP the player owned when the battle began / the XP the run credited during it.</summary>
        public int XpTotalStart;

        public int XpCredited;

        /// <summary>XP rewards of the enemies that died (summoned ones included).</summary>
        public int GainedXp;

        public bool Victory;
        public bool Aborted;
        public string AbortReason = string.Empty;

        /// <summary>Actions the player took.</summary>
        public int Turns;

        /// <summary>Last global turn number.</summary>
        public int GlobalTurns;

        public int HpStart;
        public int HpEnd;
        public int MaxHp;

        /// <summary>HP removed from the player (shields excluded), whatever the source.</summary>
        public int DamageTaken;

        public int HealingReceived;
        public int DamageDealt;
        public int LevelStart;
        public int LevelEnd;
        public int SkillsUsed;
        public int ConsumablesUsed;
        public int TerrainTriggers;
        public int TerrainDamage;

        /// <summary>Pushes and pulls (G4): enemies displaced (by any skill) / times the player was displaced.</summary>
        public int EnemiesDisplaced;

        public int PlayerDisplaced;

        /// <summary>Displacements that ended in a collision (damage dealt), whoever caused them.</summary>
        public int Collisions;

        /// <summary>Collision damage dealt to enemies (the displaced one and the one it hit) / taken by the player.</summary>
        public int CollisionDamageToEnemies;

        public int CollisionDamageToPlayer;

        /// <summary>Enemies killed by collision damage.</summary>
        public int CollisionKills;

        /// <summary>Terrain cells that applied their effect at once to a pushed or pulled enemy / the player.</summary>
        public int ForcedTerrainEnemies;

        public int ForcedTerrainPlayer;

        public double Milliseconds;

        /// <summary>HP lost from the start to the end of the battle (healing counts; negative = net gain).</summary>
        public int HpLostNet => HpStart - HpEnd;

        /// <summary>HP taken from the player by enemy type (config name).</summary>
        public Dictionary<string, int> DamageByEnemy = new();

        /// <summary>HP taken from the player by kind of damage (basic attack, skill, periodic, terrain...).</summary>
        public Dictionary<string, int> DamageByKind = new();

        public Dictionary<string, int> Kills = new();
        public Dictionary<string, int> SkillUses = new();
        public Dictionary<string, int> ItemUses = new();
        public Dictionary<string, int> TerrainBy = new();

        /// <summary>Number of different roles among the enemies the battle started with.</summary>
        public int DistinctRoles
        {
            get
            {
                var seen = new HashSet<string>(RoleNames);
                return seen.Count;
            }
        }

        /// <summary>Whether the battle has any terrain cell (obstacle, hazard or bonus).</summary>
        public bool HasTerrain => ObstacleCells + HazardCells + BonusCells > 0;

        public string EnemySummary
        {
            get
            {
                var counts = new SortedDictionary<string, int>();
                foreach (var name in EnemyNames)
                {
                    counts.TryGetValue(name, out var n);
                    counts[name] = n + 1;
                }

                var parts = new List<string>();
                foreach (var pair in counts)
                    parts.Add(pair.Value > 1 ? $"{pair.Key}x{pair.Value}" : pair.Key);
                return string.Join("+", parts);
            }
        }
    }

    /// <summary>What happened in one simulated run.</summary>
    public sealed class RunRecord
    {
        public string ClassName;
        public int RunIndex;
        public long Seed;
        public ERunResult Result;
        public string AbortReason = string.Empty;

        /// <summary>Deepest depth reached (the depth of the death for a defeat).</summary>
        public int EndDepth;

        public int Floors;
        public int FinalLevel;
        public int BattlesWon;
        public int BattlesPlayed;
        public int Turns;
        public int DamageDealt;
        public int DamageTaken;

        /// <summary>Talents taken, ranks included.</summary>
        public int TalentsPicked;

        public int DistinctTalents;

        /// <summary>Talent picks (ranks included) from the class's own pool.</summary>
        public int ClassTalentPicks;

        /// <summary>Talent picks (ranks included) of the generic talents that only the shared pool offers.</summary>
        public int SharedTalentPicks;

        public int SkillsUsed;
        public int ConsumablesUsed;
        public int TerrainTriggers;
        public int EnemiesDisplaced;
        public int PlayerDisplaced;
        public int Collisions;
        public int CollisionDamageToEnemies;
        public int CollisionDamageToPlayer;
        public int CollisionKills;
        public int ForcedTerrainEnemies;
        public int ForcedTerrainPlayer;
        public int HealNodes;
        public int TalentNodes;
        public int ConsumableNodes;
        public int Rerolls;
        public int Bans;
        public int Skips;

        /// <summary>Enemy config that dealt the last hit of a lost run ("terrain", "periodic" for the rest).</summary>
        public string KilledBy = string.Empty;

        public double Milliseconds;
        public int Frames;

        /// <summary>Talents shown on offers, by asset name.</summary>
        public Dictionary<string, int> TalentsOffered = new();

        /// <summary>Talents taken, by asset name.</summary>
        public Dictionary<string, int> TalentsTaken = new();

        public List<string> FinalSkills = new();

        /// <summary>Battle nodes of the generated map (every one, visited or not).</summary>
        public List<MapNodeInfo> MapBattles = new();
    }

    /// <summary>Everything one batch produced (see <see cref="SimulationRunner"/>).</summary>
    public sealed class SimulationBatchResult
    {
        public SimulationOptions Options;
        public List<RunRecord> Runs = new();
        public List<BattleRecord> Battles = new();
        public double TotalMilliseconds;
        public int TotalFrames;
        public int Exceptions;

        /// <summary>Errors and exceptions the console received while the batch ran (should be 0).</summary>
        public int ConsoleErrors;
        public string OutputFolder = string.Empty;
        public string SummaryText = string.Empty;
        public string Status = "finished";
    }
}
