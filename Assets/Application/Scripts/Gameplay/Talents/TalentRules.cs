using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.States;
using GridBattle.Gameplay.Stats;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Talents
{
    /// <summary>
    /// Pure rules over the talents of a run (the data in <see cref="PlayerRunState"/>): ranks, tags, skill
    /// slots, what the talents' states add to the run settings and to the maximum HP. Nothing here touches
    /// the scene, so the same answers hold in battle, on the map and in tests.
    /// </summary>
    public static class TalentRules
    {
        /// <summary>The talent with the saved id, or null.</summary>
        [CanBeNull]
        public static TalentDefinition Resolve(string id)
        {
            var database = GameDatabase.Instance;
            return database != null ? database.Get<TalentDefinition>(id) : null;
        }

        // ------------------------------------------------------------------ ranks and pool

        /// <summary>Times the talent was taken in the run (0 = not taken).</summary>
        public static int GetRank(PlayerRunState player, string talentId)
        {
            if (player == null || string.IsNullOrEmpty(talentId)) return 0;

            foreach (var entry in player.Talents)
            {
                if (entry.TalentId == talentId)
                    return Mathf.Max(1, entry.Rank);
            }

            return 0;
        }

        public static int GetRank(PlayerRunState player, [CanBeNull] TalentDefinition talent) =>
            talent != null ? GetRank(player, talent.Id) : 0;

        /// <summary>Adds one rank of the talent to the run (at most <see cref="TalentDefinition.MaxRank"/>). Returns the new rank.</summary>
        public static int AddRank(PlayerRunState player, TalentDefinition talent)
        {
            foreach (var entry in player.Talents)
            {
                if (entry.TalentId != talent.Id) continue;

                entry.Rank = Mathf.Min(talent.MaxRank, Mathf.Max(1, entry.Rank) + 1);
                return entry.Rank;
            }

            player.Talents.Add(new TalentRankState { TalentId = talent.Id, Rank = 1 });
            return 1;
        }

        public static bool IsBanned(PlayerRunState player, string talentId) =>
            player != null && player.BannedTalentIds.Contains(talentId);

        /// <summary>
        /// Every talent a class can ever be offered: its own pool and the shared pool of the settings, without
        /// duplicates and without the talents the class cannot take (<see cref="TalentDefinition.AllowedClasses"/>).
        /// In a stable order (class pool first), so draws are reproducible.
        /// </summary>
        public static List<TalentDefinition> GetPool([CanBeNull] PlayerCharacterConfig playerClass,
            [CanBeNull] TalentOfferSettings settings = null)
        {
            settings = settings != null ? settings : TalentOfferSettings.Current;
            var result = new List<TalentDefinition>();
            if (playerClass != null)
                AddAllowed(result, playerClass.TalentPool, playerClass);
            AddAllowed(result, settings.SharedPool, playerClass);
            return result;
        }

        private static void AddAllowed(List<TalentDefinition> into, IReadOnlyList<TalentDefinition> source,
            PlayerCharacterConfig playerClass)
        {
            foreach (var talent in source)
            {
                if (talent != null && !into.Contains(talent) && talent.IsAllowedFor(playerClass))
                    into.Add(talent);
            }
        }

        /// <summary>Whether every prerequisite of the talent was taken (rank 1 or more).</summary>
        public static bool ArePrerequisitesMet(PlayerRunState player, TalentDefinition talent)
        {
            foreach (var prerequisite in talent.Prerequisites)
            {
                if (prerequisite != null && GetRank(player, prerequisite) < 1)
                    return false;
            }

            return true;
        }

        // ------------------------------------------------------------------ synergy

        /// <summary>
        /// How many of the taken talents carry each tag (a talent counts once per tag whatever its rank).
        /// </summary>
        public static Dictionary<SynergyTagDefinition, int> GetTakenTagCounts(PlayerRunState player)
        {
            var counts = new Dictionary<SynergyTagDefinition, int>();
            foreach (var entry in player.Talents)
            {
                var talent = Resolve(entry.TalentId);
                if (talent == null) continue;

                foreach (var tag in talent.SynergyTags)
                {
                    if (tag == null) continue;
                    counts.TryGetValue(tag, out var count);
                    counts[tag] = count + 1;
                }
            }

            return counts;
        }

        /// <summary>The talent's tags that appear in the taken talents (what the choice screen highlights).</summary>
        public static List<SynergyTagDefinition> GetSharedTags(TalentDefinition talent,
            Dictionary<SynergyTagDefinition, int> takenTagCounts)
        {
            var shared = new List<SynergyTagDefinition>();
            foreach (var tag in talent.SynergyTags)
            {
                if (tag != null && !shared.Contains(tag) && takenTagCounts.ContainsKey(tag))
                    shared.Add(tag);
            }

            return shared;
        }

        /// <summary>The synergy count the draw weights with (see <see cref="ESynergyCounting"/>).</summary>
        public static int CountSynergy(TalentDefinition talent, Dictionary<SynergyTagDefinition, int> takenTagCounts,
            ESynergyCounting counting)
        {
            var total = 0;
            var seen = new List<SynergyTagDefinition>();
            foreach (var tag in talent.SynergyTags)
            {
                if (tag == null || seen.Contains(tag)) continue;
                seen.Add(tag);

                if (!takenTagCounts.TryGetValue(tag, out var count)) continue;
                total += counting == ESynergyCounting.TagOccurrences ? count : 1;
            }

            return total;
        }

        /// <summary>Draw weight: <c>baseWeight x (1 + bonus x synergy)</c> (no class: the shared multiplier is not applied).</summary>
        public static float GetWeight(TalentDefinition talent, Dictionary<SynergyTagDefinition, int> takenTagCounts,
            TalentOfferSettings settings)
        {
            var synergy = CountSynergy(talent, takenTagCounts, settings.SynergyCounting);
            return talent.BaseWeight * (1f + settings.SynergyBonusPerSharedTag * synergy);
        }

        /// <summary>
        /// Draw weight for a class: <c>baseWeight x sharedMultiplier x (1 + bonus x synergy)</c>, where the shared
        /// multiplier (<see cref="TalentOfferSettings.SharedPoolWeightMultiplier"/>) only applies to the talents
        /// that come from the shared pool alone (<see cref="IsSharedOnly"/>), so a class's own talents are offered
        /// more often than the generic ones.
        /// </summary>
        public static float GetWeight(TalentDefinition talent, Dictionary<SynergyTagDefinition, int> takenTagCounts,
            TalentOfferSettings settings, [CanBeNull] PlayerCharacterConfig playerClass)
        {
            var weight = GetWeight(talent, takenTagCounts, settings);
            return IsSharedOnly(talent, playerClass, settings) ? weight * settings.SharedPoolWeightMultiplier : weight;
        }

        /// <summary>
        /// Whether the talent reaches the class only through the shared pool (it is in the shared pool of the
        /// settings and not in the class's own Talent Pool): the "generic" talents.
        /// </summary>
        public static bool IsSharedOnly(TalentDefinition talent, [CanBeNull] PlayerCharacterConfig playerClass,
            [CanBeNull] TalentOfferSettings settings = null)
        {
            settings = settings != null ? settings : TalentOfferSettings.Current;
            if (talent == null || !ContainsTalent(settings.SharedPool, talent)) return false;

            return playerClass == null || !ContainsTalent(playerClass.TalentPool, talent);
        }

        private static bool ContainsTalent(IReadOnlyList<TalentDefinition> list, TalentDefinition talent)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == talent) return true;
            }

            return false;
        }

        // ------------------------------------------------------------------ skills

        /// <summary>
        /// The skills the run's player has: the saved list, or the class's starting skills when none is saved
        /// (the same rule as <see cref="PlayerRunStateApplier"/>).
        /// </summary>
        public static List<SkillDefinition> GetCurrentSkills(PlayerRunState player, [CanBeNull] PlayerCharacterConfig playerClass)
        {
            var result = new List<SkillDefinition>();
            var database = GameDatabase.Instance;
            if (database != null)
            {
                foreach (var id in player.SkillIds)
                {
                    var skill = database.Get<SkillDefinition>(id);
                    if (skill != null && !result.Contains(skill))
                        result.Add(skill);
                }
            }

            if (result.Count == 0 && playerClass != null)
            {
                foreach (var skill in playerClass.Skills)
                {
                    if (skill != null && !result.Contains(skill))
                        result.Add(skill);
                }
            }

            return result;
        }

        /// <summary>
        /// Skill bar slots the run's skills take: the class's starting skills only count when
        /// <see cref="SkillSettings.StartingSkillsUseSlots"/> is on (the same rule as
        /// <see cref="PlayerCharacter.UsedSkillSlots"/>).
        /// </summary>
        public static int GetUsedSkillSlots(PlayerRunState player, [CanBeNull] PlayerCharacterConfig playerClass)
        {
            var settings = SkillSettings.Current;
            var used = 0;
            foreach (var skill in GetCurrentSkills(player, playerClass))
            {
                if (settings.StartingSkillsUseSlots || !IsStartingSkill(playerClass, skill))
                    used++;
            }

            return used;
        }

        /// <summary>Skill bar slots still free.</summary>
        public static int GetFreeSkillSlots(PlayerRunState player, [CanBeNull] PlayerCharacterConfig playerClass) =>
            Mathf.Max(0, SkillSettings.Current.MaxSkillSlots - GetUsedSkillSlots(player, playerClass));

        private static bool IsStartingSkill(PlayerCharacterConfig playerClass, SkillDefinition skill)
        {
            if (playerClass == null) return false;

            foreach (var starting in playerClass.Skills)
            {
                if (starting == skill) return true;
            }

            return false;
        }

        /// <summary>Talents taken that unlock a skill (what the skill talent limit counts).</summary>
        public static int CountSkillTalents(PlayerRunState player)
        {
            var count = 0;
            foreach (var entry in player.Talents)
            {
                var talent = Resolve(entry.TalentId);
                if (talent != null && talent.IsSkillTalent)
                    count++;
            }

            return count;
        }

        // ------------------------------------------------------------------ what the states add up to

        /// <summary>Stacks of a granted state at a rank (stacks per rank x rank, within the state's maximum).</summary>
        public static int GetStacks(in StateGrant grant, int rank) =>
            grant.State != null ? Mathf.Clamp(grant.Stacks * Mathf.Max(1, rank), 1, grant.State.MaxStacks) : 0;

        /// <summary>
        /// Sum of a run modifier (talent options, rerolls, bans, skips) over the talents' states, as if they
        /// were active: used outside battles, where there is no character holding the states.
        /// </summary>
        public static int GetRunModifier(PlayerRunState player, ERunModifier modifier)
        {
            var total = 0;
            foreach (var entry in player.Talents)
            {
                var talent = Resolve(entry.TalentId);
                if (talent == null) continue;

                foreach (var grant in talent.States)
                {
                    if (!grant.IsValid) continue;

                    var instance = new StateInstance(grant.State, StateInstance.Permanent,
                        GetStacks(grant, Mathf.Max(1, entry.Rank)), talent.Id);
                    foreach (var effect in grant.State.Effects)
                    {
                        if (effect != null)
                            total += effect.GetRunModifier(modifier, instance);
                    }
                }
            }

            return total;
        }

        /// <summary>The attribute modifiers the talents' states add up to (the same ones the states give in battle).</summary>
        public static List<AttributeModifier> CollectModifiers(PlayerRunState player)
        {
            var modifiers = new List<AttributeModifier>();
            foreach (var entry in player.Talents)
            {
                var talent = Resolve(entry.TalentId);
                if (talent == null) continue;

                foreach (var grant in talent.States)
                {
                    if (!grant.IsValid) continue;

                    var instance = new StateInstance(grant.State, StateInstance.Permanent,
                        GetStacks(grant, Mathf.Max(1, entry.Rank)), talent.Id);
                    foreach (var effect in grant.State.Effects)
                    {
                        if (effect != null)
                            effect.CollectModifiers(instance, modifiers);
                    }
                }
            }

            return modifiers;
        }

        /// <summary>
        /// Maximum HP with the talents: <c>(base + flat) x (1 + percent)</c> over the talents' Max HP modifiers,
        /// the same formula <see cref="CharacterStats"/> uses in battle.
        /// </summary>
        public static int GetMaxHp(PlayerRunState player, int baseMaxHp)
        {
            var flat = 0f;
            var percent = 0f;
            foreach (var modifier in CollectModifiers(player))
            {
                if (modifier.Attribute != EAttribute.MaxHp) continue;

                if (modifier.Type == EModifierType.Flat)
                    flat += modifier.Value;
                else
                    percent += modifier.Value;
            }

            return Mathf.Max(1, Mathf.RoundToInt((baseMaxHp + flat) * (1f + percent)));
        }
    }
}
