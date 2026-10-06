using System.Collections.Generic;
using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Gameplay.Talents
{
    /// <summary>How the synergy of a candidate talent with the build is counted.</summary>
    public enum ESynergyCounting
    {
        /// <summary>Number of the candidate's tags that appear in at least one taken talent.</summary>
        DistinctTags,

        /// <summary>For each tag of the candidate, the number of taken talents that carry it (summed).</summary>
        TagOccurrences
    }

    /// <summary>
    /// Rules of the talent offer (Mechanic 3 / 3.5, talentos_e_oferta.md): how many options are offered, how
    /// many rerolls, bans and skips a run has, how the synergy weights the draw, the limit of skill talents and
    /// the shared talent pool. Open questions of the design documents are fields here with neutral defaults.
    /// The run-setting modifiers granted by states (<c>ERunModifier.TalentOptions/Rerolls/Bans/Skips</c>) add to
    /// the base values below.
    /// </summary>
    [CreateAssetMenu(fileName = "TalentOfferSettings", menuName = "GridBattle/Settings/Talent Offer Settings", order = 6)]
    public sealed class TalentOfferSettings : ScriptableObject, IGameSettings
    {
        [Header("Options per offer")]
        [SerializeField]
        [Min(1)]
        [Tooltip("Talents offered at each level (GDD: 3). States can add more (Talent Options run modifier).")]
        private int optionsPerOffer = 3;

        [SerializeField]
        [Min(1)]
        [Tooltip("Upper limit of the options per offer after the run modifiers (the choice screen has room for this many).")]
        private int maxOptionsPerOffer = 5;

        [Header("Player tools per run (open question: base amounts)")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Rerolls of the offer available in a run (shared by every offer). States can add more.")]
        private int baseRerolls = 2;

        [SerializeField]
        [Min(0)]
        [Tooltip("Bans (removing a talent from the run's pool) available in a run. States can add more.")]
        private int baseBans = 1;

        [SerializeField]
        [Min(0)]
        [Tooltip("Skips of an offer available in a run. Skipping loses that level's talent. States can add more.")]
        private int baseSkips = 1;

        [SerializeField]
        [Tooltip("A reroll never shows the options that are on screen when the pool has enough other talents (it falls back to any when it does not).")]
        private bool rerollExcludesCurrentOptions = true;

        [Header("Draw (open question: draw details)")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Weight = Base Weight x (1 + this x shared tags), where shared tags are the candidate's tags found among the talents already taken (0 = no synergy weighting).")]
        private float synergyBonusPerSharedTag = 0.5f;

        [SerializeField]
        private ESynergyCounting synergyCounting = ESynergyCounting.DistinctTags;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Balance G3 (class identity): draw weight multiplier of the talents that come only from the shared pool " +
                 "(listed below and not in the class's own Talent Pool), so the class's own talents show up more often. " +
                 "0.5 = a generic talent is half as likely as a class talent of the same weight. 1 = no difference. " +
                 "Applied before the synergy bonus.")]
        private float sharedPoolWeightMultiplier = 0.5f;

        [Header("Skill talents (talentos_e_oferta: limit of skill talents)")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Most talents that unlock a skill in a run (GDD: 6, the number of skill bar buttons). Once it is reached, skill talents are no longer offered. 0 = only the free skill slots limit them.")]
        private int maxSkillTalents = 6;

        [Header("When offers open")]
        [SerializeField]
        [Tooltip("Open question: a level gained by the XP of the final boss's last kill still opens its talent choice. Off (default) = the run is about to end, so those offers are skipped (the level still counts).")]
        private bool offerAfterFinalBossKill;

        [SerializeField]
        [Min(0)]
        [Tooltip("The talent node (mapa_e_nos) works like an extra level: its offer uses the talents available at the " +
                 "player's level plus this bonus. With 0 a level-1 player would be offered nothing (every talent needs " +
                 "level 2+), paying the node's cost for nothing.")]
        private int talentNodeLevelBonus = 1;

        [Header("Acquiring a talent")]
        [SerializeField]
        [Tooltip("A talent that raises the maximum HP also restores that amount of current HP (like most games do). Off = the maximum goes up and the current HP stays (the GDD only says a level up does not heal).")]
        private bool maxHpGainHealsSameAmount = true;

        [Header("Pool")]
        [SerializeField]
        [Tooltip("Talents every class can be offered (their Allowed Classes still apply). Each class adds its own Talent Pool on top.")]
        private List<TalentDefinition> sharedPool = new();

        public int OptionsPerOffer => Mathf.Max(1, optionsPerOffer);
        public int MaxOptionsPerOffer => Mathf.Max(OptionsPerOffer, maxOptionsPerOffer);
        public int BaseRerolls => Mathf.Max(0, baseRerolls);
        public int BaseBans => Mathf.Max(0, baseBans);
        public int BaseSkips => Mathf.Max(0, baseSkips);
        public bool RerollExcludesCurrentOptions => rerollExcludesCurrentOptions;
        public float SynergyBonusPerSharedTag => synergyBonusPerSharedTag;
        public ESynergyCounting SynergyCounting => synergyCounting;
        public float SharedPoolWeightMultiplier => Mathf.Max(0f, sharedPoolWeightMultiplier);
        public int MaxSkillTalents => Mathf.Max(0, maxSkillTalents);
        public bool OfferAfterFinalBossKill => offerAfterFinalBossKill;
        public int TalentNodeLevelBonus => Mathf.Max(0, talentNodeLevelBonus);
        public bool MaxHpGainHealsSameAmount => maxHpGainHealsSameAmount;
        public IReadOnlyList<TalentDefinition> SharedPool => sharedPool;

        /// <summary>The settings asset registered in <see cref="GameSettings"/> (defaults if missing).</summary>
        public static TalentOfferSettings Current => GameSettings.Get<TalentOfferSettings>();

        /// <summary>Options of an offer given the run modifier of the states (base + modifier, within 1 and the maximum).</summary>
        public int GetOptionCount(int modifier) => Mathf.Clamp(OptionsPerOffer + modifier, 1, MaxOptionsPerOffer);
    }
}
