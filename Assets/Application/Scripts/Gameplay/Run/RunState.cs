using System;
using System.Collections.Generic;
using GridBattle.Core.Randomness;

namespace GridBattle.Gameplay.Run
{
    public enum ERunPhase
    {
        /// <summary>On the map, choosing the next node.</summary>
        Map,

        /// <summary>Inside a battle node.</summary>
        Battle,

        /// <summary>Resolving a talent node (paying the cost / choosing).</summary>
        TalentNode,

        /// <summary>Victory or defeat; the run is over.</summary>
        Finished,

        /// <summary>
        /// A consumable node is waiting for the player to choose among the offered items or to pick the slot
        /// to discard (appended last so the numeric values of the other phases stay stable in saves).
        /// </summary>
        ConsumableOffer
    }

    public enum EMapNodeType
    {
        Battle,
        Heal,
        Talent,
        Consumable,
        Boss
    }

    public enum EBattleDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public enum ETalentOfferSource
    {
        LevelUp,
        TalentNode
    }

    /// <summary>
    /// Everything about a run in progress, saved automatically between the
    /// player's actions (GDD 8.1). Plain data (Newtonsoft JSON): content is
    /// referenced by <see cref="Data.GameDefinition.Id"/>, and grid positions by
    /// X/Y. Reloading must resume exactly where the player stopped, with the same
    /// random positions and the same pending talent offer.
    /// </summary>
    [Serializable]
    public sealed class RunState
    {
        public int Version = 1;
        public RunRandom Random;

        /// <summary>PlayerCharacterConfig id of the chosen class.</summary>
        public string ClassId;

        public ERunPhase Phase = ERunPhase.Map;
        public PlayerRunState Player = new();
        public MapState Map = new();

        /// <summary>Talent offers waiting for a choice, oldest first (one per level gained).</summary>
        public List<TalentOfferState> PendingOffers = new();

        /// <summary>
        /// Consumable node offer waiting for a decision (phase <see cref="ERunPhase.ConsumableOffer"/>):
        /// ConsumableDefinition ids, in the order offered.
        /// </summary>
        public List<string> PendingConsumableOptions = new();

        /// <summary>Battle in progress (null outside battles). Captured at the start of each player turn.</summary>
        public BattleSnapshot Battle;

        public RunStatistics Statistics = new();

        /// <summary>Set when the run ends.</summary>
        public bool Victory;
    }

    [Serializable]
    public sealed class PlayerRunState
    {
        /// <summary>Current HP; persists between nodes (GDD Mechanic 2).</summary>
        public int Hp;

        public int Level = 1;
        public int Xp;

        /// <summary>Talents taken, in pick order (rank = how many times).</summary>
        public List<TalentRankState> Talents = new();

        /// <summary>Unlocked skills (SkillDefinition ids), in skill bar order.</summary>
        public List<string> SkillIds = new();

        /// <summary>Consumable slots (ConsumableDefinition ids; null or empty = free slot).</summary>
        public List<string> ConsumableIds = new();

        /// <summary>Talents banned from this run's pool.</summary>
        public List<string> BannedTalentIds = new();

        public int RerollsUsed;
        public int BansUsed;
        public int SkipsUsed;

        /// <summary>Non-talent states carried between battles (depends on RunSettings).</summary>
        public List<StateSnapshot> States = new();

        /// <summary>
        /// Skill cooldowns carried between battles (key = SkillDefinition id). Only filled when
        /// RunSettings.ResetSkillCooldownsBetweenBattles is off.
        /// </summary>
        public List<CounterState> SkillCooldowns = new();
    }

    [Serializable]
    public sealed class TalentRankState
    {
        public string TalentId;
        public int Rank = 1;
    }

    [Serializable]
    public sealed class MapState
    {
        public List<MapNodeState> Nodes = new();

        /// <summary>Node the player is at (-1 = before the first floor).</summary>
        public int CurrentNodeId = -1;

        /// <summary>Nodes the player went through, in order (the path walked, for drawing).</summary>
        public List<int> VisitedNodeIds = new();

        /// <summary>Number of floors (the depth of the final boss).</summary>
        public int FloorCount;

        /// <summary>Number of columns of the map the nodes' <see cref="MapNodeState.Column"/> fit in (for drawing).</summary>
        public int Lanes;
    }

    [Serializable]
    public sealed class MapNodeState
    {
        public int Id;

        /// <summary>Floor index from 0. Depth = Floor + 1 (GDD: position from 1 up to the final boss).</summary>
        public int Floor;

        /// <summary>Horizontal lane (for drawing and connections).</summary>
        public int Column;

        public EMapNodeType Type;
        public EBattleDifficulty Difficulty;

        /// <summary>Ids of the nodes of the next floor reachable from this one.</summary>
        public List<int> Next = new();

        /// <summary>Generated battle (battle and boss nodes only).</summary>
        public BattleSpec Battle;

        public int Depth => Floor + 1;
    }

    /// <summary>A generated battle: enemies, terrain and the XP it grants (shown in the node preview).</summary>
    [Serializable]
    public sealed class BattleSpec
    {
        public int Depth;
        public int Width = 6;
        public int Height = 6;
        public int PlayerX = 2;
        public int PlayerY = 2;
        public List<EnemySpawnSpec> Enemies = new();
        public List<TerrainCellSpec> Terrain = new();
        public int TotalXp;
        public bool IsBoss;
    }

    [Serializable]
    public sealed class EnemySpawnSpec
    {
        public string EnemyId;
        public int X;
        public int Y;
        public float HpMultiplier = 1f;
        public float DamageMultiplier = 1f;
        public int XpReward;
    }

    [Serializable]
    public sealed class TerrainCellSpec
    {
        public int X;
        public int Y;
        public string TerrainId;
    }

    /// <summary>Exact battle state at the start of a player turn.</summary>
    [Serializable]
    public sealed class BattleSnapshot
    {
        public int NodeId;
        public int GlobalTurn = 1;
        public BattleSpec Spec;
        public EntitySnapshot Player;
        public List<EntitySnapshot> Enemies = new();
        public bool ConsumableUsedThisTurn;
    }

    [Serializable]
    public sealed class EntitySnapshot
    {
        public string ConfigId;
        public int X;
        public int Y;
        public int Hp;
        public float HpMultiplier = 1f;
        public float DamageMultiplier = 1f;
        public int XpReward;
        public bool IsSummoned;
        public List<StateSnapshot> States = new();

        /// <summary>Skill cooldowns (key = SkillDefinition id).</summary>
        public List<CounterState> SkillCooldowns = new();

        /// <summary>The entity walked or teleported during its last turn (read by conditional damage effects).</summary>
        public bool MovedLastTurn;

        /// <summary>AI memory: action cooldowns and counters (key chosen by each action).</summary>
        public List<CounterState> AiMemory = new();
    }

    [Serializable]
    public sealed class StateSnapshot
    {
        public string StateId;
        public int Remaining;
        public int Stacks = 1;
        public string SourceId;
        public int Shield;
    }

    [Serializable]
    public sealed class CounterState
    {
        public string Key;
        public int Value;
    }

    [Serializable]
    public sealed class TalentOfferState
    {
        public ETalentOfferSource Source;

        /// <summary>Level reached that created the offer (for level-up offers).</summary>
        public int Level;

        /// <summary>Offered talents (TalentDefinition ids).</summary>
        public List<string> OptionIds = new();

        /// <summary>How many times this offer was rerolled (part of its random key).</summary>
        public int RerollIndex;
    }

    /// <summary>Data for the end-of-run summary and the design metrics (GDD 9).</summary>
    [Serializable]
    public sealed class RunStatistics
    {
        public int BattlesWon;
        public int EnemiesKilled;
        public int DamageDealt;
        public int DamageTaken;
        public int TurnsPlayed;
        public int MaxDepthReached;
    }
}
