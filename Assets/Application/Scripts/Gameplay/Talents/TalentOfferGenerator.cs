using System.Collections.Generic;
using System.Linq;
using GridBattle.Core.Randomness;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Run;
using JetBrains.Annotations;

namespace GridBattle.Gameplay.Talents
{
    /// <summary>
    /// Draws the talents of an offer (Mechanic 3, talentos_e_oferta.md). Deterministic: the draw only depends on
    /// the run's seed, the offer key (the level for a level-up offer, a distinct key per talent node) and the
    /// reroll index, through <see cref="RunRandom.Derive"/>, and on the state of the run's talents. A battle
    /// save that replays the turn therefore re-derives the same offer.
    /// <para>
    /// Filters: the class can take the talent, the level and prerequisites are met, the rank is below the maximum,
    /// it was not banned, it is not excluded by the caller, and skill talents respect the skill talent limit and
    /// the free skill slots (and never unlock a skill the player already has). The draw is without replacement,
    /// weighted by <c>baseWeight x (1 + bonus x synergy)</c>.
    /// </para>
    /// </summary>
    public static class TalentOfferGenerator
    {
        /// <summary>Offer keys of talent nodes start here (level-up offers use the level itself).</summary>
        public const long NodeOfferKeyBase = 1_000_000;

        /// <summary>Offer key of the level-up offer for <paramref name="level"/>.</summary>
        public static long GetLevelKey(int level) => level;

        /// <summary>Offer key of the offer of the talent node <paramref name="nodeId"/>.</summary>
        public static long GetNodeKey(int nodeId) => NodeOfferKeyBase + nodeId;

        /// <summary>A talent that can be offered, with its draw weight.</summary>
        public readonly struct Candidate
        {
            public TalentDefinition Talent { get; }
            public float Weight { get; }

            public Candidate(TalentDefinition talent, float weight)
            {
                Talent = talent;
                Weight = weight;
            }
        }

        /// <summary>
        /// The talents that can be offered now, with their weights, in pool order (class pool first). Useful for
        /// the draw and to inspect an offer.
        /// </summary>
        public static List<Candidate> GetCandidates(PlayerRunState player, [CanBeNull] PlayerCharacterConfig playerClass,
            int level, TalentOfferSettings settings, [CanBeNull] IReadOnlyCollection<string> exclude = null)
        {
            var result = new List<Candidate>();
            var tagCounts = TalentRules.GetTakenTagCounts(player);
            var freeSlots = TalentRules.GetFreeSkillSlots(player, playerClass);
            var skillTalents = TalentRules.CountSkillTalents(player);
            var ownedSkills = TalentRules.GetCurrentSkills(player, playerClass);

            foreach (var talent in TalentRules.GetPool(playerClass, settings))
            {
                if (!IsEligible(talent, player, level, settings, freeSlots, skillTalents, ownedSkills, exclude))
                    continue;

                var weight = TalentRules.GetWeight(talent, tagCounts, settings, playerClass);
                if (weight > 0f)
                    result.Add(new Candidate(talent, weight));
            }

            return result;
        }

        /// <summary>
        /// Draws up to <paramref name="count"/> different talents for the offer identified by
        /// <paramref name="offerKey"/> and <paramref name="rerollIndex"/> (fewer when the pool runs out).
        /// </summary>
        public static List<string> Generate(PlayerRunState player, [CanBeNull] PlayerCharacterConfig playerClass,
            int level, int count, TalentOfferSettings settings, RunRandom random, long offerKey, int rerollIndex,
            [CanBeNull] IReadOnlyCollection<string> exclude = null)
        {
            var rng = random.Derive(ERandomStream.Talents, offerKey, rerollIndex);
            var candidates = GetCandidates(player, playerClass, level, settings, exclude);
            return Draw(candidates, count, rng);
        }

        /// <summary>
        /// Like <see cref="Generate"/>, but <paramref name="avoid"/> is only a preference (a reroll that should
        /// not show the options already on screen): talents outside it are drawn first, and the avoided ones fill
        /// what is left when the pool has too few others.
        /// </summary>
        public static List<string> GenerateAvoiding(PlayerRunState player, [CanBeNull] PlayerCharacterConfig playerClass,
            int level, int count, TalentOfferSettings settings, RunRandom random, long offerKey, int rerollIndex,
            IReadOnlyCollection<string> avoid)
        {
            var rng = random.Derive(ERandomStream.Talents, offerKey, rerollIndex);
            var preferred = Draw(GetCandidates(player, playerClass, level, settings, avoid), count, rng);
            if (preferred.Count >= count) return preferred;

            // Not enough other talents: complete the offer with the ones that were avoided.
            var rest = GetCandidates(player, playerClass, level, settings, preferred);
            preferred.AddRange(Draw(rest, count - preferred.Count, rng));
            return preferred;
        }

        /// <summary>
        /// Draws the talent that replaces a banned one. <paramref name="serial"/> tells apart the draws of the
        /// same offer (the run's number of bans so far), so each replacement is reproducible on its own.
        /// </summary>
        [CanBeNull]
        public static string DrawReplacement(PlayerRunState player, [CanBeNull] PlayerCharacterConfig playerClass,
            int level, TalentOfferSettings settings, RunRandom random, long offerKey, int rerollIndex, int serial,
            IReadOnlyCollection<string> exclude)
        {
            var rng = random.Derive(ERandomStream.Talents, offerKey, rerollIndex, serial);
            var candidates = GetCandidates(player, playerClass, level, settings, exclude);
            var drawn = Draw(candidates, 1, rng);
            return drawn.Count > 0 ? drawn[0] : null;
        }

        /// <summary>Weighted draw without replacement from <paramref name="candidates"/>.</summary>
        public static List<string> Draw(List<Candidate> candidates, int count, Rng rng)
        {
            var result = new List<string>();
            var weights = new List<float>(candidates.Count);
            foreach (var candidate in candidates)
                weights.Add(candidate.Weight);

            while (result.Count < count)
            {
                var index = rng.WeightedIndex(weights);
                if (index < 0) break;

                result.Add(candidates[index].Talent.Id);
                weights[index] = 0f;
            }

            return result;
        }

        /// <summary>Whether the talent can be offered in the run now (see the class remarks for the filters).</summary>
        public static bool IsEligible(TalentDefinition talent, PlayerRunState player, int level,
            TalentOfferSettings settings, int freeSkillSlots, int skillTalentsTaken,
            IReadOnlyList<SkillDefinition> ownedSkills, [CanBeNull] IReadOnlyCollection<string> exclude)
        {
            if (talent == null || string.IsNullOrEmpty(talent.Id)) return false;
            if (talent.BaseWeight <= 0f) return false;
            if (level < talent.RequiredLevel) return false;
            if (TalentRules.IsBanned(player, talent.Id)) return false;
            if (exclude != null && exclude.Contains(talent.Id)) return false;
            if (TalentRules.GetRank(player, talent) >= talent.MaxRank) return false;
            if (!TalentRules.ArePrerequisitesMet(player, talent)) return false;

            if (talent.IsSkillTalent)
            {
                if (freeSkillSlots <= 0) return false;
                if (settings.MaxSkillTalents > 0 && skillTalentsTaken >= settings.MaxSkillTalents) return false;

                foreach (var owned in ownedSkills)
                {
                    if (owned == talent.UnlockedSkill)
                        return false;
                }
            }

            return true;
        }
    }
}
