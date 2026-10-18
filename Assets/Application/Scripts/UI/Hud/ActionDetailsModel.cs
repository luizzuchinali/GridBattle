using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.Stats;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.UI.Hud
{
    /// <summary>One line of a skill or item sheet: what it is and its value.</summary>
    public readonly struct DetailsFact
    {
        public DetailsFact(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }
        public string Value { get; }
    }

    /// <summary>
    /// What the details window shows for a skill or an item of the player's bars (long press or right click on the
    /// slot): name, icon, description and a sheet with the owner's real numbers (talent modifiers, Skill Range,
    /// Skill Damage Bonus, Damage Dealt and Cooldown Reduction included; the target's defense is not), plus the
    /// cooldown left of a skill and a note for items. Pure: reads the game, changes nothing.
    /// </summary>
    public sealed class ActionDetailsModel
    {
        private ActionDetailsModel(string title, Sprite icon, string description)
        {
            Title = title;
            Icon = icon;
            Description = description;
        }

        public string Title { get; }
        public Sprite Icon { get; }
        public string Description { get; }
        public List<DetailsFact> Facts { get; } = new();

        /// <summary>"Ready" or the cooldown left (skills only; null for items).</summary>
        [CanBeNull]
        public string Status { get; private set; }

        public bool Ready { get; private set; }

        /// <summary>A closing remark (items: using one does not take the action).</summary>
        [CanBeNull]
        public string Note { get; private set; }

        /// <summary>The sheet of a skill as <paramref name="owner"/> would use it now.</summary>
        public static ActionDetailsModel ForSkill(SkillDefinition skill, [CanBeNull] Character owner)
        {
            var effective = EffectiveSkill.Resolve(owner, skill);
            var model = new ActionDetailsModel(skill.GetDisplayName(), skill.Icon, skill.GetDescription());

            if (effective.Damage > 0)
                model.Add(HudText.Get("details.action.damage"), FormatDamage(effective.Damage, owner));

            model.Add(HudText.Get("details.action.range"), FormatSkillRange(effective,
                owner != null ? SkillTargeting.GetMaxRange(owner, effective) : effective.Range));
            if (effective.TargetCell != ESkillTargetCell.Any)
                model.Add(HudText.Get("details.action.target_cell"), effective.TargetCell == ESkillTargetCell.Empty
                    ? HudText.Get("details.target_cell.empty")
                    : HudText.Get("details.target_cell.occupied"));
            model.Add(HudText.Get("details.action.area"), FormatArea(effective.AreaShape, effective.AreaSize));
            model.Add(HudText.Get("details.action.targets"), GetTargetsText(skill.Affects));

            var cooldown = owner != null ? owner.Cooldowns.GetCooldownAfterUse(skill) : Mathf.Max(0, effective.Cooldown);
            model.Add(HudText.Get("details.action.cooldown"), cooldown > 0
                ? HudText.Format(cooldown == 1 ? "details.state.turn" : "details.state.turns", cooldown)
                : HudText.Get("details.action.no_cooldown"));

            if (!effective.IsSelfCentered && CombatResolver.Settings.SkillsNeedLineOfSight)
            {
                var sight = effective.IgnoresLineOfSight
                    ? HudText.Get("details.action.sight_ignored")
                    : HudText.Get("details.action.sight_needed");
                model.Add(HudText.Get("details.action.sight"), sight);
            }

            if (skill.Telegraphed)
                model.Add(HudText.Get("details.action.telegraph"), HudText.Format(
                    skill.WindupTurns == 1 ? "details.telegraph.turn" : "details.telegraph.turns", skill.WindupTurns));

            var displace = effective.FindDisplacement();
            if (displace != null)
            {
                var distance = effective.GetDisplacementDistance(displace);
                var movement = displace.Mode == EDisplacementMode.TowardCaster
                    ? HudText.Format("details.action.pulls", distance)
                    : HudText.Format("details.action.pushes", distance);
                model.Add(HudText.Get("details.action.displace"), movement);
            }

            if (owner != null)
            {
                var remaining = owner.Cooldowns.GetRemaining(skill);
                model.Ready = remaining <= 0;
                model.Status = model.Ready
                    ? HudText.Get("details.skill.ready")
                    : HudText.Format("details.skill.cooldown", remaining);
            }

            return model;
        }

        /// <summary>The sheet of an item (consumable).</summary>
        public static ActionDetailsModel ForConsumable(ConsumableDefinition item, [CanBeNull] Character owner)
        {
            var model = new ActionDetailsModel(item.GetDisplayName(), item.Icon, item.GetDescription());
            if (!item.NeedsTarget)
            {
                model.Add(HudText.Get("details.action.use"), HudText.Get("details.action.use_self"));
            }
            else
            {
                model.Add(HudText.Get("details.action.use"), HudText.Get("details.action.use_cell"));
                model.Add(HudText.Get("details.action.range"), FormatRange(item.IsSelfCentered, item.UnlimitedRange, item.Range));
                model.Add(HudText.Get("details.action.area"), FormatArea(item.AreaShape, item.AreaSize));
                model.Add(HudText.Get("details.action.targets"), GetTargetsText(item.Affects));
            }

            model.Note = HudText.Format("details.action.item_note", ConsumableSettings.Current.MaxUsesPerTurn);
            return model;
        }

        private void Add(string label, string value)
        {
            Facts.Add(new DetailsFact(label, value));
        }

        /// <summary>The skill's damage before the target's defense: base x (1 + skill bonus) x (1 + damage dealt).</summary>
        private static string FormatDamage(int damage, [CanBeNull] Character owner)
        {
            if (owner == null) return damage.ToString();

            var value = damage * (1f + owner.Stats.Get(EAttribute.SkillDamageBonus)) *
                        Mathf.Max(0f, 1f + owner.Stats.Get(EAttribute.DamageDealt));
            return CombatResolver.Settings.RoundValue(value).ToString();
        }

        /// <summary>
        /// The selection area of a skill: "3", "1–3" with a minimum range, "any cell", plus the shape when it is not
        /// a circle ("1–3, in line").
        /// </summary>
        private static string FormatSkillRange(in EffectiveSkill skill, int maxRange)
        {
            if (skill.IsSelfCentered) return HudText.Get("details.action.range_self");

            string distance;
            if (skill.UnlimitedRange)
                distance = HudText.Get("details.action.range_any");
            else if (skill.MinRange > 0 && skill.MinRange < maxRange)
                distance = HudText.Format("details.action.range_min_max", skill.MinRange, maxRange);
            else
                distance = maxRange.ToString();

            var shape = skill.SelectionShape switch
            {
                ESkillSelectionShape.Line => HudText.Get("details.selection.line"),
                ESkillSelectionShape.Diagonal => HudText.Get("details.selection.diagonal"),
                ESkillSelectionShape.Star => HudText.Get("details.selection.star"),
                ESkillSelectionShape.Square => HudText.Get("details.selection.square"),
                _ => null,
            };
            return shape == null ? distance : HudText.Format("details.action.range_shape", distance, shape);
        }

        private static string FormatRange(bool selfCentered, bool unlimited, int range)
        {
            if (selfCentered) return HudText.Get("details.action.range_self");
            if (unlimited) return HudText.Get("details.action.range_any");

            return range.ToString();
        }

        private static string FormatArea(ESkillAreaShape shape, int size)
        {
            if (size <= 1) return HudText.Get("details.action.area_single");

            return HudText.Format("details.action.area_value", GetShapeText(shape), size);
        }

        private static string GetShapeText(ESkillAreaShape shape) => shape switch
        {
            ESkillAreaShape.Cross => HudText.Get("details.shape.cross"),
            ESkillAreaShape.Linear => HudText.Get("details.shape.linear"),
            ESkillAreaShape.Cone => HudText.Get("details.shape.cone"),
            ESkillAreaShape.Arc => HudText.Get("details.shape.arc"),
            ESkillAreaShape.Perpendicular => HudText.Get("details.shape.perpendicular"),
            _ => HudText.Get("details.shape.circle"),
        };

        private static string GetTargetsText(ESkillTargetFilter filter) => filter switch
        {
            ESkillTargetFilter.Allies => HudText.Get("details.targets.allies"),
            ESkillTargetFilter.AlliesAndSelf => HudText.Get("details.targets.allies_and_self"),
            ESkillTargetFilter.Self => HudText.Get("details.targets.self"),
            ESkillTargetFilter.Everyone => HudText.Get("details.targets.everyone"),
            ESkillTargetFilter.EveryoneButCaster => HudText.Get("details.targets.everyone_but_self"),
            _ => HudText.Get("details.targets.enemies"),
        };
    }
}
