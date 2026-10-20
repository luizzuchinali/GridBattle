using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Stats;
using GridBattle.UI.Hud;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI.Screens
{
    /// <summary>
    /// Builds the rows of the information lists (end-of-run summary, consumable offer, glossary, character
    /// details): section titles, rows with an optional icon and value, and paragraphs, plus the common blocks
    /// built from them (base attributes, skills). Styles are the <c>info-*</c> classes of Components.uss.
    /// </summary>
    public static class SummaryRows
    {
        /// <summary>Attributes a character sheet always lists (base values).</summary>
        private static readonly EAttribute[] BasicAttributes =
        {
            EAttribute.MaxHp, EAttribute.WalkRange, EAttribute.AttackRange, EAttribute.BasicDamage, EAttribute.Defense,
        };

        /// <summary>Attributes a character sheet lists only when their base value is not zero.</summary>
        private static readonly EAttribute[] OptionalAttributes =
        {
            EAttribute.CritChance, EAttribute.DefensePenetration, EAttribute.SkillDamageBonus, EAttribute.SkillRange,
            EAttribute.CooldownReduction,
        };

        public static Label AddSection(VisualElement parent, string title)
        {
            var label = new Label(title) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("info-section-title");
            parent.Add(label);
            return label;
        }

        public static Label AddText(VisualElement parent, string text, string modifier = null)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("info-text");
            if (!string.IsNullOrEmpty(modifier))
                label.AddToClassList(modifier);
            parent.Add(label);
            return label;
        }

        public static VisualElement AddRow(VisualElement parent, string label, string value, Sprite icon = null,
            string modifier = null)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("info-row");
            if (!string.IsNullOrEmpty(modifier))
                row.AddToClassList(modifier);

            if (icon != null)
            {
                var image = new Image { sprite = icon, pickingMode = PickingMode.Ignore };
                image.AddToClassList("info-icon");
                row.Add(image);
            }

            var name = new Label(label) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("info-row__label");
            row.Add(name);

            if (!string.IsNullOrEmpty(value))
            {
                var text = new Label(value) { pickingMode = PickingMode.Ignore };
                text.AddToClassList("info-row__value");
                row.Add(text);
            }

            parent.Add(row);
            return row;
        }

        /// <summary>
        /// The base attributes of a character config (before the depth scaling of a battle and any state): the
        /// basic ones always, the others only when they are not zero (crit multiplier only with a crit chance).
        /// </summary>
        public static void AddBaseAttributes(VisualElement parent, CharacterConfig config)
        {
            foreach (var attribute in BasicAttributes)
            {
                // Without a basic attack (only skills), its range and damage mean nothing.
                if (!config.HasBasicAttack && IsBasicAttackAttribute(attribute)) continue;

                AddAttributeRow(parent, config, attribute);
            }

            foreach (var attribute in OptionalAttributes)
            {
                if (Mathf.Abs(config.GetBaseAttribute(attribute)) <= 0.0001f) continue;

                AddAttributeRow(parent, config, attribute);
                if (attribute == EAttribute.CritChance)
                    AddAttributeRow(parent, config, EAttribute.CritMultiplier);
            }
        }

        private static bool IsBasicAttackAttribute(EAttribute attribute) =>
            attribute == EAttribute.AttackRange || attribute == EAttribute.BasicDamage;

        /// <summary>One row per skill (icon and name) followed by its description; "none" when there is none.</summary>
        public static void AddSkills(VisualElement parent, System.Collections.Generic.IEnumerable<SkillDefinition> skills)
        {
            var hasSkill = false;
            foreach (var skill in skills)
            {
                if (skill == null) continue;

                hasSkill = true;
                AddRow(parent, skill.GetDisplayName(), string.Empty, skill.Icon);
                var description = skill.GetDescription();
                if (!string.IsNullOrEmpty(description))
                    AddText(parent, description);
            }

            if (!hasSkill)
                AddText(parent, HudText.Get("details.none"));
        }

        private static void AddAttributeRow(VisualElement parent, CharacterConfig config, EAttribute attribute)
        {
            AddRow(parent, HudText.Get(EntityDetailsModel.GetAttributeKey(attribute)),
                EntityDetailsModel.FormatAttribute(attribute, config.GetBaseAttribute(attribute)));
        }
    }
}
