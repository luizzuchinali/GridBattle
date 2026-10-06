using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.States;
using UnityEngine;

namespace GridBattle.Gameplay.Talents
{
    /// <summary>
    /// A talent (trait, GDD 3.5): what the player picks when a level is reached (or at a talent node). It grants
    /// permanent states (attributes, behaviors, run-setting modifiers) and may unlock a skill. It is offered
    /// only to the classes that can take it, once its level and prerequisites are met, and can be taken again
    /// up to <see cref="MaxRank"/> times (each rank adds one stack of every granted state).
    /// </summary>
    [CreateAssetMenu(fileName = "Talent", menuName = "GridBattle/Talents/Talent", order = 0)]
    public sealed class TalentDefinition : DisplayableDefinition
    {
        [Header("Content status")]
        [SerializeField]
        [Tooltip("The values, texts and icon are provisional (balancing and art pending). Informational only.")]
        private bool isPlaceholder;

        [Header("Availability")]
        [SerializeField]
        [Tooltip("Classes that can take this talent. Empty = shared by every class. A talent must also be listed in the class's Talent Pool or in the shared pool of the Talent Offer Settings to be offered.")]
        private List<PlayerCharacterConfig> allowedClasses = new();

        [SerializeField]
        [Min(1)]
        [Tooltip("Lowest player level at which the talent can be offered (a level-up offer counts the level just reached).")]
        private int requiredLevel = 2;

        [SerializeField]
        [Tooltip("Talents that must have been taken (at least rank 1) before this one can be offered.")]
        private List<TalentDefinition> prerequisites = new();

        [SerializeField]
        [Min(1)]
        [Tooltip("Open question (talentos_e_oferta): how many times the same talent can be taken. 1 = a single level. Each rank adds one stack of every granted state.")]
        private int maxRank = 1;

        [Header("Offer weight")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Base weight in the offer draw (0 = never offered). Multiplied by the synergy bonus of the Talent Offer Settings.")]
        private float baseWeight = 1f;

        [SerializeField]
        [Tooltip("Synergy tags: talents that share tags with the ones already taken are drawn more often, and are highlighted as 'combines with the build'.")]
        private List<SynergyTagDefinition> synergyTags = new();

        [Header("Effects")]
        [SerializeField]
        [Tooltip("Permanent states the talent grants (applied with this talent as the source). Duration and Permanent are ignored; Stacks is the number of stacks per rank. The state's Max Stacks must allow Max Rank x Stacks.")]
        private List<StateGrant> states = new();

        [SerializeField]
        [Tooltip("Skill the talent unlocks (makes it a 'skill talent': the run allows 6, one per skill bar slot).")]
        private SkillDefinition unlockedSkill;

        /// <summary>The talent's values, texts and icon are provisional.</summary>
        public bool IsPlaceholder => isPlaceholder;

        public IReadOnlyList<PlayerCharacterConfig> AllowedClasses => allowedClasses;
        public int RequiredLevel => Mathf.Max(1, requiredLevel);
        public IReadOnlyList<TalentDefinition> Prerequisites => prerequisites;
        public int MaxRank => Mathf.Max(1, maxRank);
        public float BaseWeight => baseWeight;
        public IReadOnlyList<SynergyTagDefinition> SynergyTags => synergyTags;
        public IReadOnlyList<StateGrant> States => states;
        public SkillDefinition UnlockedSkill => unlockedSkill;

        /// <summary>The talent unlocks a skill (counts toward the run's skill talent limit).</summary>
        public bool IsSkillTalent => unlockedSkill != null;

        /// <summary>Whether <paramref name="playerClass"/> can take the talent (no allowed class listed = every class).</summary>
        public bool IsAllowedFor(PlayerCharacterConfig playerClass)
        {
            if (allowedClasses.Count == 0) return true;
            if (playerClass == null) return false;

            foreach (var allowed in allowedClasses)
            {
                if (allowed == playerClass)
                    return true;
            }

            return false;
        }

        /// <summary>Whether the talent carries the tag.</summary>
        public bool HasTag(SynergyTagDefinition tag)
        {
            return tag != null && synergyTags.Contains(tag);
        }
    }
}
