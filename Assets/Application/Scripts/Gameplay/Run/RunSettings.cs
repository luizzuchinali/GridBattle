using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Gameplay.Run
{
    /// <summary>How the talent node charges the player for a talent without a fight (mapa_e_nos, open question).</summary>
    public enum ETalentNodeCost
    {
        /// <summary>The talent node is free.</summary>
        None,

        /// <summary>The player loses a fraction of the maximum HP.</summary>
        HpFraction,

        /// <summary>The player loses a flat amount of HP.</summary>
        HpFlat
    }

    /// <summary>What happens to the player's states when a battle ends (estados_e_atributos, open question).</summary>
    public enum ETemporaryStatePolicy
    {
        /// <summary>States with a duration are removed after the battle; permanent ones stay.</summary>
        ClearTemporary,

        /// <summary>Every state stays, with its remaining turns, into the next battle.</summary>
        KeepTemporary
    }

    /// <summary>
    /// Rules of a run (GDD Mechanic 2 and 8.1): seed, what the non-battle nodes do, what carries over
    /// between battles and saving. Open questions of the design documents are fields with neutral defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "RunSettings", menuName = "GridBattle/Settings/Run Settings", order = 1)]
    public sealed class RunSettings : ScriptableObject, IGameSettings
    {
        [Header("Seed")]
        [SerializeField]
        [Tooltip("Debug only (the seed is never shown to the player): a value other than 0 makes every run use that seed, so the same map, battles and draws repeat. 0 = a new random seed per run.")]
        private long debugSeed;

        [Header("Heal node")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Open question (balanceamento_e_geracao, life and healing): fraction of the maximum HP a heal node recovers.")]
        private float healFraction = 0.3f;

        [SerializeField]
        [Min(0)]
        [Tooltip("Flat HP added to the fraction above (0 = only the fraction).")]
        private int healFlat;

        [Header("Talent node")]
        [SerializeField]
        [Tooltip("Open question (mapa_e_nos, talent node cost): what the player pays to get a talent without fighting.")]
        private ETalentNodeCost talentNodeCost = ETalentNodeCost.HpFraction;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Fraction of the maximum HP lost (cost type HpFraction). The cost never kills: at least 1 HP is left.")]
        private float talentNodeCostHpFraction = 0.15f;

        [SerializeField]
        [Min(0)]
        [Tooltip("HP lost (cost type HpFlat). The cost never kills: at least 1 HP is left.")]
        private int talentNodeCostHpFlat = 10;

        [Header("Between battles")]
        [SerializeField]
        [Tooltip("Open question (estados_e_atributos, temporary states between battles): ClearTemporary removes states with a duration after each battle and keeps permanent ones; KeepTemporary keeps everything.")]
        private ETemporaryStatePolicy temporaryStates = ETemporaryStatePolicy.ClearTemporary;

        [SerializeField]
        [Tooltip("Skill cooldowns start over on every battle. Off = the cooldowns left at the end of a battle carry into the next one.")]
        private bool resetSkillCooldownsBetweenBattles = true;

        [Header("Saving (GDD 8.1)")]
        [SerializeField]
        [Tooltip("Saves the run automatically (after starting it, after every node, at the start of every player turn and when the app pauses or quits). Off = the run is never written to disk (debug).")]
        private bool autosave = true;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Until a map screen exists: whenever the map opens, the manager enters the first available node by itself, so a run plays straight through the nodes. Turn it off when a UI drives the map.")]
        private bool autoEnterFirstNode;

        /// <summary>Debug seed (0 = random).</summary>
        public ulong DebugSeed => debugSeed <= 0 ? 0UL : (ulong)debugSeed;

        public float HealFraction => healFraction;
        public int HealFlat => healFlat;
        public ETalentNodeCost TalentNodeCost => talentNodeCost;
        public float TalentNodeCostHpFraction => talentNodeCostHpFraction;
        public int TalentNodeCostHpFlat => talentNodeCostHpFlat;
        public ETemporaryStatePolicy TemporaryStates => temporaryStates;
        public bool ResetSkillCooldownsBetweenBattles => resetSkillCooldownsBetweenBattles;
        public bool Autosave => autosave;
        public bool AutoEnterFirstNode => autoEnterFirstNode;

        /// <summary>The settings asset registered in <see cref="GameSettings"/> (defaults if missing).</summary>
        public static RunSettings Current => GameSettings.Get<RunSettings>();

        /// <summary>HP a heal node recovers for a player with <paramref name="maxHp"/>.</summary>
        public int GetHealAmount(int maxHp) => Mathf.Max(0, Mathf.RoundToInt(maxHp * healFraction) + healFlat);

        /// <summary>
        /// HP the talent node costs a player with <paramref name="hp"/> of <paramref name="maxHp"/>. Never
        /// more than <c>hp - 1</c>: the cost cannot kill.
        /// </summary>
        public int GetTalentNodeCost(int hp, int maxHp)
        {
            var cost = talentNodeCost switch
            {
                ETalentNodeCost.HpFraction => Mathf.RoundToInt(maxHp * talentNodeCostHpFraction),
                ETalentNodeCost.HpFlat => talentNodeCostHpFlat,
                _ => 0,
            };

            return Mathf.Clamp(cost, 0, Mathf.Max(0, hp - 1));
        }
    }
}
