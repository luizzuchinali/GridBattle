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
        ConsumableOffer,

        /// <summary>A camp (heal node) is waiting for the player to rest (heal) or train (upgrade a skill).</summary>
        Camp,

        /// <summary>The player is choosing a skill upgrade (training at a camp, or the reward of an elite battle).</summary>
        SkillUpgrade
    }

    /// <summary>Where a pending skill upgrade comes from.</summary>
    public enum ESkillUpgradeSource
    {
        /// <summary>Training at a camp (declining goes back to the camp's choice).</summary>
        Camp,

        /// <summary>The reward of an elite battle (declining gives nothing).</summary>
        Elite
    }

    public enum EMapNodeType
    {
        Battle,
        Heal,
        Talent,
        Consumable,
        Boss
    }

    /// <summary>
    /// Kind of a battle node (decision of 2026-10-20: no more easy, normal and hard battles). The numbers are kept
    /// from the old difficulties: a saved Hard (2) loads as Elite, and an old Easy (0) is turned into Normal when the
    /// run loads (<c>MapRules.NormalizeDifficulties</c>).
    /// </summary>
    public enum EBattleDifficulty
    {
        /// <summary>A regular battle.</summary>
        Normal = 1,

        /// <summary>
        /// A battle with at least one elite (a unique enemy) and a bigger budget: more XP, and its reward is a
        /// consumable offer and a skill upgrade.
        /// </summary>
        Elite = 2
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

        /// <summary>Where the pending skill upgrade comes from (phase <see cref="ERunPhase.SkillUpgrade"/>).</summary>
        public ESkillUpgradeSource PendingUpgradeSource;

        /// <summary>
        /// The pending consumable offer (phase <see cref="ERunPhase.ConsumableOffer"/>) is part of the reward of an
        /// elite battle, not a consumable node: answering it moves on to the rest of the reward.
        /// </summary>
        public bool EliteRewardOffer;

        /// <summary>
        /// Elite reward in progress: consumable offers still to open after the current one (the skill upgrade comes
        /// after the last of them).
        /// </summary>
        public int EliteOffersLeft;

        /// <summary>
        /// HP change of the node being resolved, kept while a choice inside it is pending (the elite reward), so
        /// the node's statistics stay right after a reload.
        /// </summary>
        public int PendingNodeHpDelta;

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

        /// <summary>
        /// Maximum HP added to the class's base for the rest of the run, besides the levels (negative: the talent
        /// node's lasting cost).
        /// </summary>
        public int MaxHpBonus;

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

        /// <summary>Normal or elite (battle nodes; Normal for the other types).</summary>
        public EBattleDifficulty Difficulty = EBattleDifficulty.Normal;

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

        /// <summary>
        /// The battle holds a unique enemy (an elite battle): winning it grants the elite reward (consumable offers
        /// and a skill upgrade, see <c>RunSettings</c>).
        /// </summary>
        public bool IsElite;
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

        /// <summary>Rounds left of a temporary terrain (made by a skill or an item); 0 = permanent.</summary>
        public int TurnsLeft;
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

        /// <summary>Place of an enemy in the acting order of the battle (0: unknown, an older save).</summary>
        public int ActOrder;

        public List<StateSnapshot> States = new();

        /// <summary>Skill cooldowns (key = SkillDefinition id).</summary>
        public List<CounterState> SkillCooldowns = new();

        /// <summary>The entity walked or teleported during its last turn (read by conditional damage effects).</summary>
        public bool MovedLastTurn;

        /// <summary>AI memory: action cooldowns and counters (key chosen by each action).</summary>
        public List<CounterState> AiMemory = new();

        /// <summary>The telegraphed skill the enemy is preparing, or null.</summary>
        public TelegraphState Telegraph;
    }

    /// <summary>A telegraphed skill being prepared (see <c>SkillTelegraph</c>).</summary>
    [Serializable]
    public sealed class TelegraphState
    {
        public string SkillId;
        public int TargetX;
        public int TargetY;
        public int TurnsLeft = 1;
        public List<GridCellState> Cells = new();

        /// <summary>The skill was aimed at the player when it was prepared.</summary>
        public bool AimedAtPlayer;
    }

    [Serializable]
    public sealed class GridCellState
    {
        public int X;
        public int Y;
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
