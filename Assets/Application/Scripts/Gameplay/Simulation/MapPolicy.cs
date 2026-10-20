using System;
using System.Collections.Generic;
using GridBattle.Core.Randomness;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Run;
using GridBattle.Managers;
using JetBrains.Annotations;

namespace GridBattle.Gameplay.Simulation
{
    /// <summary>
    /// The map decisions of the balance simulation: which node to enter next, what to do with a consumable offer, at a
    /// camp and with a skill upgrade.
    /// <list type="bullet">
    /// <item>The final boss is always entered.</item>
    /// <item>A heal node is taken whenever one is available and the HP fraction is below the threshold.</item>
    /// <item>Otherwise every node gets a weight by type (battle, heal, talent, consumable). A talent node is skipped
    /// while the HP is below its threshold (it costs HP).</item>
    /// <item>Battle nodes follow the policy: Normal and Elite pick that kind of battle when one is available (the
    /// other kind otherwise), Mixed any of them.</item>
    /// </list>
    /// Random picks use the bot's own <see cref="Rng"/>.
    /// </summary>
    public sealed class MapPolicy
    {
        private readonly MapPolicyOptions _options;
        private readonly Rng _rng;

        public MapPolicy(MapPolicyOptions options, Rng rng)
        {
            _options = options ?? new MapPolicyOptions();
            _rng = rng ?? new Rng(1UL);
        }

        /// <summary>The node to enter among <paramref name="available"/>, or null when there is none.</summary>
        [CanBeNull]
        public MapNodeState ChooseNode(IReadOnlyList<MapNodeState> available, int hp, int maxHp)
        {
            if (available == null || available.Count == 0) return null;
            if (available.Count == 1) return available[0];

            foreach (var node in available)
            {
                if (node.Type == EMapNodeType.Boss) return node;
            }

            var hpFraction = hp / (float)Math.Max(1, maxHp);
            if (hpFraction < _options.HealBelowHpFraction)
            {
                foreach (var node in available)
                {
                    if (node.Type == EMapNodeType.Heal) return node;
                }
            }

            var preferred = GetPreferredDifficulties(available);
            var weights = new List<float>(available.Count);
            foreach (var node in available)
            {
                float weight;
                switch (node.Type)
                {
                    case EMapNodeType.Battle:
                        weight = preferred.Contains(node.Difficulty) ? _options.BattleWeight : 0f;
                        break;
                    case EMapNodeType.Heal:
                        weight = _options.HealWeight;
                        break;
                    case EMapNodeType.Talent:
                        weight = hpFraction >= _options.TalentNodeMinHpFraction ? _options.TalentWeight : 0f;
                        break;
                    case EMapNodeType.Consumable:
                        weight = _options.ConsumableWeight;
                        break;
                    default:
                        weight = 0f;
                        break;
                }

                weights.Add(weight);
            }

            var index = _rng.WeightedIndex(weights);
            if (index >= 0) return available[index];

            // Nothing wanted: the first battle, else anything.
            foreach (var node in available)
            {
                if (node.Type == EMapNodeType.Battle) return node;
            }

            return available[0];
        }

        private HashSet<EBattleDifficulty> GetPreferredDifficulties(IReadOnlyList<MapNodeState> available)
        {
            var preferred = new HashSet<EBattleDifficulty>();
            var offered = new HashSet<EBattleDifficulty>();
            foreach (var node in available)
            {
                if (node.Type == EMapNodeType.Battle)
                    offered.Add(node.Difficulty);
            }

            if (offered.Count == 0) return preferred;

            switch (_options.Policy)
            {
                case EMapPolicy.Mixed:
                    preferred.UnionWith(offered);
                    break;
                case EMapPolicy.Elite:
                    preferred.Add(offered.Contains(EBattleDifficulty.Elite) ? EBattleDifficulty.Elite : EBattleDifficulty.Normal);
                    break;
                default:
                    // Normal (and the value of an old Easy policy).
                    preferred.Add(offered.Contains(EBattleDifficulty.Normal) ? EBattleDifficulty.Normal : EBattleDifficulty.Elite);
                    break;
            }

            return preferred;
        }

        /// <summary>
        /// Answers the pending consumable offer (a consumable node, or the reward of an elite battle): takes the best
        /// option (healing, then damage, then the rest; one not carried yet is slightly preferred) and, with a full
        /// inventory, replaces the worst carried item when the new one is better or declines. Returns false when there
        /// is nothing to answer.
        /// </summary>
        public bool AnswerConsumableOffer(RunManager runs)
        {
            if (runs == null || runs.CurrentRun == null || runs.Phase != ERunPhase.ConsumableOffer) return false;

            var options = runs.PendingConsumableOptions;
            if (options.Count == 0) return runs.DeclineConsumableOffer();

            var inventory = new ConsumableInventory(runs.CurrentRun.Player.ConsumableIds);
            var bestIndex = 0;
            var bestScore = float.MinValue;
            for (var i = 0; i < options.Count; i++)
            {
                var score = Score(options[i]) + (inventory.IndexOf(options[i]) < 0 ? 0.1f : 0f);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            var result = runs.ChooseConsumableOffer(bestIndex);
            if (result != EConsumableOfferResult.NeedsSlotChoice) return result != EConsumableOfferResult.Invalid;

            var worstSlot = -1;
            var worstScore = float.MaxValue;
            for (var slot = 0; slot < inventory.Slots; slot++)
            {
                var item = inventory.Get(slot);
                if (item == null) continue;

                var score = Score(item);
                if (score < worstScore)
                {
                    worstScore = score;
                    worstSlot = slot;
                }
            }

            if (worstSlot >= 0 && bestScore - 0.1f > worstScore)
                return runs.ReplaceConsumable(worstSlot);

            return runs.DeclineConsumableOffer();
        }

        /// <summary>
        /// Answers a camp: rests when the HP fraction is below <see cref="MapPolicyOptions.CampRestBelowHpFraction"/>
        /// (or when resting heals and there is nothing to upgrade), trains otherwise. Returns false outside a camp.
        /// </summary>
        public bool AnswerCamp(RunManager runs)
        {
            if (runs == null || runs.CurrentRun == null || runs.Phase != ERunPhase.Camp) return false;

            var hpFraction = runs.PlayerHp / (float)Math.Max(1, runs.PlayerMaxHp);
            if (hpFraction >= _options.CampRestBelowHpFraction && runs.CampTrain()) return true;

            return runs.CampRest();
        }

        /// <summary>
        /// Answers a skill upgrade: one of the upgradable skills and one of its variants, both at random (the bot's
        /// own <see cref="Rng"/>). Returns false outside the SkillUpgrade phase.
        /// </summary>
        public bool AnswerSkillUpgrade(RunManager runs)
        {
            if (runs == null || runs.CurrentRun == null || runs.Phase != ERunPhase.SkillUpgrade) return false;

            var skills = runs.GetUpgradableSkills();
            if (skills.Count == 0) return runs.DeclineSkillUpgrade();

            var skill = _rng.Pick(skills);
            var variants = new List<int>();
            for (var i = 0; i < skill.Upgrades.Count; i++)
            {
                if (skill.Upgrades[i] != null)
                    variants.Add(i);
            }

            if (variants.Count == 0) return runs.DeclineSkillUpgrade();

            return runs.ChooseSkillUpgrade(skill, _rng.Pick(variants)) || runs.DeclineSkillUpgrade();
        }

        private static float Score(ConsumableDefinition item)
        {
            var score = 1f;
            foreach (var effect in item.Effects)
            {
                if (effect is HealConsumableEffect) return 3f;
                if (effect is DamageConsumableEffect) score = Math.Max(score, 2f);
            }

            return score;
        }
    }
}
