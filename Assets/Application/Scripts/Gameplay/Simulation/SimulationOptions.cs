using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay.Simulation
{
    /// <summary>Difficulty the map bot prefers when it picks a battle node.</summary>
    public enum EMapPolicy
    {
        /// <summary>Always the easiest battle available.</summary>
        Easy,

        /// <summary>Normal battles, the closest difficulty when there is none.</summary>
        Normal,

        /// <summary>Always the hardest battle available.</summary>
        Hard,

        /// <summary>Any battle, drawn with the bot's own random stream.</summary>
        Mixed
    }

    /// <summary>How the talent bot answers a talent offer.</summary>
    public enum ETalentPolicy
    {
        /// <summary>The first option on screen.</summary>
        FirstOption,

        /// <summary>The option sharing the most synergy tags with the build (first one on ties).</summary>
        HighestSynergy,

        /// <summary>A random option (the bot's own random stream).</summary>
        Random
    }

    /// <summary>Settings of the map bot (<see cref="MapPolicy"/>).</summary>
    [Serializable]
    public sealed class MapPolicyOptions
    {
        [SerializeField]
        [Tooltip("Difficulty preferred when choosing between battle nodes.")]
        private EMapPolicy policy = EMapPolicy.Mixed;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("A heal node is taken whenever one is available and the HP fraction is below this value.")]
        private float healBelowHpFraction = 0.5f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("A talent node costs HP: it is only taken when the HP fraction is at least this value.")]
        private float talentNodeMinHpFraction = 0.6f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Relative chance of a battle node among the nodes available.")]
        private float battleWeight = 3f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Relative chance of a heal node when the HP is not low enough to force it.")]
        private float healWeight = 0.5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Relative chance of a talent node (when the HP allows it).")]
        private float talentWeight = 1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Relative chance of a consumable node.")]
        private float consumableWeight = 1f;

        public EMapPolicy Policy { get => policy; set => policy = value; }
        public float HealBelowHpFraction { get => healBelowHpFraction; set => healBelowHpFraction = value; }
        public float TalentNodeMinHpFraction { get => talentNodeMinHpFraction; set => talentNodeMinHpFraction = value; }
        public float BattleWeight { get => battleWeight; set => battleWeight = value; }
        public float HealWeight { get => healWeight; set => healWeight = value; }
        public float TalentWeight { get => talentWeight; set => talentWeight = value; }
        public float ConsumableWeight { get => consumableWeight; set => consumableWeight = value; }
    }

    /// <summary>Settings of the talent bot (<see cref="TalentPolicy"/>).</summary>
    [Serializable]
    public sealed class TalentPolicyOptions
    {
        [SerializeField]
        [Tooltip("How the bot picks one of the options of an offer.")]
        private ETalentPolicy policy = ETalentPolicy.FirstOption;

        [SerializeField]
        [Tooltip("Reroll an offer in which no option shares a tag with the build (while rerolls are left).")]
        private bool rerollWithoutSynergy;

        [SerializeField]
        [Min(0)]
        [Tooltip("Most rerolls the bot spends on one offer.")]
        private int maxRerollsPerOffer = 1;

        [SerializeField]
        [Tooltip("Ban an option the build does not combine with when no option does, before choosing (while bans are left).")]
        private bool banWithoutSynergy;

        [SerializeField]
        [Tooltip("Skip an offer in which no option shares a tag with the build (while skips are left).")]
        private bool skipWithoutSynergy;

        [SerializeField]
        [Tooltip("Names of talent assets the bot takes whenever they are offered, before the policy picks (first one on screen wins). " +
                 "Empty = the policy decides alone. Used to measure a build, e.g. the push and pull skill talents.")]
        private List<string> preferredTalents = new();

        public ETalentPolicy Policy { get => policy; set => policy = value; }
        public bool RerollWithoutSynergy { get => rerollWithoutSynergy; set => rerollWithoutSynergy = value; }
        public int MaxRerollsPerOffer { get => maxRerollsPerOffer; set => maxRerollsPerOffer = value; }
        public bool BanWithoutSynergy { get => banWithoutSynergy; set => banWithoutSynergy = value; }
        public bool SkipWithoutSynergy { get => skipWithoutSynergy; set => skipWithoutSynergy = value; }
        public IReadOnlyList<string> PreferredTalents => preferredTalents;

        public void SetPreferredTalents(IEnumerable<string> names)
        {
            preferredTalents = new List<string>(names ?? new List<string>());
        }
    }

    /// <summary>Settings of the battle bot (<see cref="BattleBot"/>).</summary>
    [Serializable]
    public sealed class BattleBotOptions
    {
        [SerializeField]
        [Tooltip("The bot uses skills (off = basic attacks only).")]
        private bool useSkills = true;

        [SerializeField]
        [Tooltip("The bot uses consumables.")]
        private bool useConsumables = true;

        [SerializeField]
        [Min(1)]
        [Tooltip("A skill is used when it hits at least this many enemies (it is also used when it kills or out-damages the basic attack).")]
        private int skillMinTargets = 2;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Healing consumables are used when the HP fraction is below this value.")]
        private float potionHpFraction = 0.5f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Consumables that only give states to the user (regeneration, shield...) are used below this HP fraction.")]
        private float supportItemHpFraction = 0.8f;

        [SerializeField]
        [Min(1)]
        [Tooltip("Area consumables (bombs) are used when they reach at least this many enemies (or kill one).")]
        private int areaItemMinTargets = 2;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Defensive skills that heal the caster are used (instead of attacking) below this HP fraction.")]
        private float healSkillHpFraction = 0.4f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Cost, in half route steps, the bot gives to stepping on a hazard cell that hurts the player " +
                 "(3 = it walks through a hazard when that saves two steps). It triples as the HP goes to zero, and a " +
                 "hazard that would kill the player is never chosen while another cell exists.")]
        private float hazardPenalty = 3f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Reward, in half route steps, for stepping on a bonus cell that helps the player.")]
        private float bonusReward = 1.5f;

        [SerializeField]
        [Min(1)]
        [Tooltip("After this many player turns in one battle the bot stops weighing hazard and bonus cells (only a cell that would kill " +
                 "it still counts), walks straight to its target and chases the enemies of Focus Role Order even when a ranged skill " +
                 "reaches them. Breaks stand-offs where a hazard blocks the only way to an enemy that keeps its distance, or where " +
                 "healers out-heal weak ranged hits.")]
        private int stallTurns = 40;

        [SerializeField]
        [Min(0f)]
        [Tooltip("How much one role tier (see Focus Role Order) is worth, in route steps, when choosing whom to walk toward.")]
        private float roleTierDistanceBonus = 1f;

        [SerializeField]
        [Tooltip("Names of the enemy role assets the bot kills first, most wanted first (then the lowest HP).")]
        private List<string> focusRoleOrder = new() { "Support", "Summoner", "Controller", "Ranged" };

        [SerializeField]
        [Tooltip("Names of the enemy role assets the bot walks to even when it could attack another enemy now " +
                 "(a summoner that keeps refilling the board, for example). It only does so when no skill or attack " +
                 "reaches that enemy this turn and a step brings it closer.")]
        private List<string> rushRoleNames = new() { "Summoner" };

        [SerializeField]
        [Min(1)]
        [Tooltip("Most turns in a row the bot rushes a Rush Role enemy; then it spends one turn on the normal choice (attacking " +
                 "what is in reach) before rushing again. Keeps a summoner that hides behind its minions from pulling the bot " +
                 "back and forth without ever attacking.")]
        private int rushMaxStreak = 3;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Pushing and pulling: value, in HP, of sending an enemy that could hit the player next turn out of its reach " +
                 "(and the cost of pulling one that could not into reach).")]
        private float displaceOutOfReachValue = 4f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Pushing and pulling: value, in HP, of pulling a ranged enemy (attack range 2 or more) from out of the " +
                 "player's attack range to within it.")]
        private float displacePullInValue = 3f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Pushing and pulling: value, in HP, of each harmful state of the hazard an enemy is sent onto or of each " +
                 "beneficial state of the bonus cell it is pulled off (the cost of the opposite). Terrain damage counts " +
                 "as damage. High on purpose: an enemy that keeps a shield from a bonus cell cannot be killed by damage alone.")]
        private float displaceTerrainStateValue = 12f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("How much of the damage over time that a skill's states are expected to do (poison, burn) counts in " +
                 "the skill's expected damage when the bot compares it with a basic attack (1 = fully, 0 = ignored). " +
                 "Never counted as a kill.")]
        private float stateValueWeight = 1f;

        public bool UseSkills { get => useSkills; set => useSkills = value; }
        public float StateValueWeight { get => stateValueWeight; set => stateValueWeight = value; }
        public bool UseConsumables { get => useConsumables; set => useConsumables = value; }
        public int SkillMinTargets { get => skillMinTargets; set => skillMinTargets = value; }
        public float PotionHpFraction { get => potionHpFraction; set => potionHpFraction = value; }
        public float SupportItemHpFraction { get => supportItemHpFraction; set => supportItemHpFraction = value; }
        public int AreaItemMinTargets { get => areaItemMinTargets; set => areaItemMinTargets = value; }
        public float HealSkillHpFraction { get => healSkillHpFraction; set => healSkillHpFraction = value; }
        public float HazardPenalty { get => hazardPenalty; set => hazardPenalty = value; }
        public float BonusReward { get => bonusReward; set => bonusReward = value; }
        public int StallTurns { get => stallTurns; set => stallTurns = value; }
        public int RushMaxStreak { get => rushMaxStreak; set => rushMaxStreak = value; }
        public float RoleTierDistanceBonus { get => roleTierDistanceBonus; set => roleTierDistanceBonus = value; }
        public float DisplaceOutOfReachValue { get => displaceOutOfReachValue; set => displaceOutOfReachValue = value; }
        public float DisplacePullInValue { get => displacePullInValue; set => displacePullInValue = value; }
        public float DisplaceTerrainStateValue { get => displaceTerrainStateValue; set => displaceTerrainStateValue = value; }
        public IReadOnlyList<string> FocusRoleOrder => focusRoleOrder;
        public IReadOnlyList<string> RushRoleNames => rushRoleNames;

        public void SetFocusRoleOrder(IEnumerable<string> roles)
        {
            focusRoleOrder = new List<string>(roles ?? new List<string>());
        }

        public void SetRushRoleNames(IEnumerable<string> roles)
        {
            rushRoleNames = new List<string>(roles ?? new List<string>());
        }
    }

    /// <summary>
    /// Everything one simulation batch needs: what to play, how the bots decide, the safety limits and the output.
    /// Serialized in <see cref="BalanceSimulationSettings"/> (Inspector) and plain enough for scripts to clone and
    /// tweak (<see cref="Clone"/>) before running several batches.
    /// </summary>
    [Serializable]
    public sealed class SimulationOptions
    {
        [Header("Batch")]
        [SerializeField]
        [Tooltip("Name of the batch: written in the reports and in the output folder name.")]
        private string label = "baseline";

        [SerializeField]
        [Tooltip("Classes to simulate. Empty = every playable class.")]
        private List<ECharacter> classes = new();

        [SerializeField]
        [Min(1)]
        [Tooltip("Complete runs played per class.")]
        private int runsPerClass = 30;

        [SerializeField]
        [Min(1)]
        [Tooltip("Seed of the first run. Run number i of every class uses StartSeed + i, so the classes play the same maps.")]
        private long startSeed = 1000;

        [SerializeField]
        [Tooltip("Seed of the bots' own random stream (tie-breaks, random choices). Never mixed with the run's streams.")]
        private long botSeed = 12345;

        [Header("Bots")]
        [SerializeField]
        private MapPolicyOptions map = new();

        [SerializeField]
        private TalentPolicyOptions talents = new();

        [SerializeField]
        private BattleBotOptions battleBot = new();

        [Header("Diagnostics")]
        [SerializeField]
        [Tooltip("Heals the player to full on the map right before every battle node (through RunManager.ChangePlayerHp; " +
                 "the rules inside the battles are untouched). Measures the battles themselves, without the attrition " +
                 "between them, and lets the bots reach the late depths.")]
        private bool restoreHpBeforeBattle;

        [Header("Limits")]
        [SerializeField]
        [Min(10)]
        [Tooltip("A battle that passes this global turn is aborted (the run ends as aborted).")]
        private int maxTurnsPerBattle = 150;

        [SerializeField]
        [Min(30)]
        [Tooltip("Frames without any progress (turns, nodes, offers) before the battle or node is aborted as stuck.")]
        private int stuckFrames = 240;

        [SerializeField]
        [Min(1f)]
        [Tooltip("Real seconds a single run may take before it is aborted.")]
        private float maxSecondsPerRun = 120f;

        [SerializeField]
        [Min(1)]
        [Tooltip("Failed bot turns in a row (no legal action found) before the battle is aborted.")]
        private int maxFailedActions = 3;

        [Header("Speed")]
        [SerializeField]
        [Min(1)]
        [Tooltip("Real milliseconds of bot work per frame (the loop gives the frame back after this).")]
        private int actionBudgetMs = 25;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Time.timeScale during the simulation (the game rules do not depend on time).")]
        private float timeScale = 1f;

        [SerializeField]
        [Tooltip("Removes the frame rate cap and VSync during the simulation (restored afterwards).")]
        private bool uncapFrameRate = true;

        [Header("Output")]
        [SerializeField]
        [Tooltip("Folder under the persistent data path where each batch writes its summary.md, runs.csv and battles.csv.")]
        private string outputFolder = "Simulation";

        [SerializeField]
        [Tooltip("Writes runs.csv and battles.csv next to summary.md.")]
        private bool writeCsv = true;

        [SerializeField]
        [Tooltip("Prints the summary to the console when the batch ends.")]
        private bool logSummary = true;

        public string Label { get => label; set => label = value; }
        public IReadOnlyList<ECharacter> Classes => classes;
        public int RunsPerClass { get => runsPerClass; set => runsPerClass = value; }
        public long StartSeed { get => startSeed; set => startSeed = value; }
        public long BotSeed { get => botSeed; set => botSeed = value; }
        public MapPolicyOptions Map => map;
        public TalentPolicyOptions Talents => talents;
        public BattleBotOptions BattleBot => battleBot;
        public bool RestoreHpBeforeBattle { get => restoreHpBeforeBattle; set => restoreHpBeforeBattle = value; }
        public int MaxTurnsPerBattle { get => maxTurnsPerBattle; set => maxTurnsPerBattle = value; }
        public int StuckFrames { get => stuckFrames; set => stuckFrames = value; }
        public float MaxSecondsPerRun { get => maxSecondsPerRun; set => maxSecondsPerRun = value; }
        public int MaxFailedActions { get => maxFailedActions; set => maxFailedActions = value; }
        public int ActionBudgetMs { get => actionBudgetMs; set => actionBudgetMs = value; }
        public float TimeScale { get => timeScale; set => timeScale = value; }
        public bool UncapFrameRate { get => uncapFrameRate; set => uncapFrameRate = value; }
        public string OutputFolder { get => outputFolder; set => outputFolder = value; }
        public bool WriteCsv { get => writeCsv; set => writeCsv = value; }
        public bool LogSummary { get => logSummary; set => logSummary = value; }

        public void SetClasses(IEnumerable<ECharacter> value)
        {
            classes = new List<ECharacter>(value ?? new List<ECharacter>());
        }

        /// <summary>Deep copy (through Unity's serializer), so a script can tweak a batch without touching the asset.</summary>
        public SimulationOptions Clone() => JsonUtility.FromJson<SimulationOptions>(JsonUtility.ToJson(this));

        /// <summary>One line with the policies, for report headers.</summary>
        public string DescribePolicies() =>
            $"map={map.Policy}, talents={talents.Policy}, skills={(battleBot.UseSkills ? "on" : "off")}, " +
            $"items={(battleBot.UseConsumables ? "on" : "off")}" +
            (restoreHpBeforeBattle ? ", HP restored before every battle" : string.Empty);
    }
}
