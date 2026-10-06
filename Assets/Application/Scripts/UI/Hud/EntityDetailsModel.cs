using System.Collections.Generic;
using System.Globalization;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.States;
using GridBattle.Gameplay.Stats;
using GridBattle.Gameplay.Terrain;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.UI.Hud
{
    /// <summary>
    /// What the entity details window shows (GDD 4.1): a character (an enemy or the player) and/or the
    /// terrain of the cell. Opening the window passes it to the controller as the navigation argument.
    /// </summary>
    public sealed class EntityDetailsRequest
    {
        public EntityDetailsRequest([CanBeNull] Character character, [CanBeNull] TerrainDefinition terrain)
        {
            Character = character;
            Terrain = terrain;
        }

        /// <summary>The character on the cell (null for an empty cell).</summary>
        [CanBeNull]
        public Character Character { get; }

        /// <summary>The terrain of the cell (null for plain floor).</summary>
        [CanBeNull]
        public TerrainDefinition Terrain { get; }
    }

    /// <summary>One line of the attribute list.</summary>
    public readonly struct DetailsAttribute
    {
        public DetailsAttribute(EAttribute attribute, string label, string value)
        {
            Attribute = attribute;
            Label = label;
            Value = value;
        }

        public EAttribute Attribute { get; }
        public string Label { get; }
        public string Value { get; }
    }

    /// <summary>One line of the skill list.</summary>
    public readonly struct DetailsSkill
    {
        public DetailsSkill(Sprite icon, string name, string status, bool ready)
        {
            Icon = icon;
            Name = name;
            Status = status;
            Ready = ready;
        }

        public Sprite Icon { get; }
        public string Name { get; }

        /// <summary>"Ready" or the remaining cooldown.</summary>
        public string Status { get; }

        public bool Ready { get; }
    }

    /// <summary>One line of the active state list.</summary>
    public readonly struct DetailsState
    {
        public DetailsState(Sprite icon, string name, string duration, EStateKind kind)
        {
            Icon = icon;
            Name = name;
            Duration = duration;
            Kind = kind;
        }

        public Sprite Icon { get; }

        /// <summary>Name, with the stacks when there is more than one.</summary>
        public string Name { get; }

        /// <summary>Remaining turns or "permanent".</summary>
        public string Duration { get; }

        public EStateKind Kind { get; }
    }

    /// <summary>
    /// Pure data for the entity details window, built from the game state (no UI types). Texts are
    /// localized when it is built, so a language change builds it again.
    /// </summary>
    public sealed class EntityDetailsModel
    {
        public string Title { get; private set; }
        public Sprite Portrait { get; private set; }

        public bool IsCharacter { get; private set; }
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }

        public bool HasRole { get; private set; }
        public string RoleName { get; private set; }
        public Sprite RoleIcon { get; private set; }
        public string Behavior { get; private set; }

        public List<DetailsAttribute> Attributes { get; } = new();
        public List<DetailsSkill> Skills { get; } = new();
        public List<DetailsState> States { get; } = new();

        public bool HasTerrain { get; private set; }

        /// <summary>A look at the terrain of an empty cell: the window is titled after the terrain.</summary>
        public bool TerrainOnly => HasTerrain && !IsCharacter;
        public string TerrainName { get; private set; }
        public string TerrainDescription { get; private set; }
        public Sprite TerrainIcon { get; private set; }

        /// <summary>Builds the model for a character and/or terrain (at least one of them).</summary>
        public static EntityDetailsModel Build(EntityDetailsRequest request, bool showAllAttributes)
        {
            var model = new EntityDetailsModel();
            if (request.Character != null)
                model.AddCharacter(request.Character, showAllAttributes);

            if (request.Terrain != null)
                model.AddTerrain(request.Terrain);

            // A plain look at the terrain has no entity to name: the window is titled after the terrain.
            if (model.TerrainOnly)
                model.Title = model.TerrainName;

            return model;
        }

        private void AddCharacter(Character character, bool showAllAttributes)
        {
            IsCharacter = true;
            var config = character.Config;
            Title = config != null ? config.GetDisplayName() : character.name;
            Portrait = config != null ? config.Sprite : null;
            Hp = character.Current;
            MaxHp = character.MaxHp;

            if (character is Enemy { Role: not null } enemy)
            {
                HasRole = true;
                RoleName = enemy.Role.GetDisplayName();
                RoleIcon = enemy.Role.Icon;
                Behavior = enemy.EnemyConfig != null ? enemy.EnemyConfig.GetBehaviorDescription() : string.Empty;
            }

            AddAttributes(character, showAllAttributes);
            AddSkills(character);
            AddStates(character);
        }

        private void AddAttributes(Character character, bool all)
        {
            var stats = character.Stats;

            // The basics always show; the others only when they have an effect (or the setting lists them all).
            AddAttribute(EAttribute.WalkRange, character.WalkDistance);
            AddAttribute(EAttribute.AttackRange, stats.Get(EAttribute.AttackRange));
            AddAttribute(EAttribute.BasicDamage, stats.Get(EAttribute.BasicDamage));
            AddAttribute(EAttribute.Defense, stats.Get(EAttribute.Defense));
            AddOptional(stats, EAttribute.CritChance, all);
            // The multiplier is never zero, so it follows the chance instead of the non-zero rule.
            if (all || stats.Get(EAttribute.CritChance) > 0f)
                AddAttribute(EAttribute.CritMultiplier, stats.Get(EAttribute.CritMultiplier));
            AddOptional(stats, EAttribute.DefensePenetration, all);
            AddOptional(stats, EAttribute.SkillDamageBonus, all);
            AddOptional(stats, EAttribute.SkillRange, all);
            AddOptional(stats, EAttribute.CooldownReduction, all);
            AddOptional(stats, EAttribute.DamageDealt, all);
            AddOptional(stats, EAttribute.DamageTaken, all);
        }

        private void AddOptional(CharacterStats stats, EAttribute attribute, bool show)
        {
            var value = stats.Get(attribute);
            if (show || Mathf.Abs(value) > 0.0001f)
                AddAttribute(attribute, value);
        }

        private void AddAttribute(EAttribute attribute, float value)
        {
            Attributes.Add(new DetailsAttribute(attribute, HudText.Get(GetAttributeKey(attribute)),
                FormatAttribute(attribute, value)));
        }

        private void AddSkills(Character character)
        {
            foreach (var skill in character.Skills)
            {
                if (skill == null) continue;

                var remaining = character.Cooldowns.GetRemaining(skill);
                var status = remaining > 0
                    ? HudText.Format("details.skill.cooldown", remaining)
                    : HudText.Get("details.skill.ready");
                Skills.Add(new DetailsSkill(skill.Icon, skill.GetDisplayName(), status, remaining <= 0));
            }
        }

        private void AddStates(Character character)
        {
            foreach (var state in character.States.All)
            {
                if (state == null || state.Definition == null || !state.Definition.ShowInDetails) continue;

                var name = state.Definition.GetDisplayName();
                if (state.Stacks > 1)
                    name += " x" + state.Stacks;

                var duration = state.IsPermanent
                    ? HudText.Get("details.state.permanent")
                    : HudText.Format(state.Remaining == 1 ? "details.state.turn" : "details.state.turns", state.Remaining);
                States.Add(new DetailsState(state.Definition.Icon, name, duration, state.Definition.Kind));
            }
        }

        private void AddTerrain(TerrainDefinition terrain)
        {
            HasTerrain = true;
            TerrainName = terrain.GetDisplayName();
            TerrainDescription = terrain.GetDescription();
            TerrainIcon = terrain.Icon;
        }

        /// <summary>UI table key of the attribute's label (<c>attribute.max_hp</c>, ...).</summary>
        public static string GetAttributeKey(EAttribute attribute) => attribute switch
        {
            EAttribute.MaxHp => "attribute.max_hp",
            EAttribute.WalkRange => "attribute.walk_range",
            EAttribute.AttackRange => "attribute.attack_range",
            EAttribute.BasicDamage => "attribute.basic_damage",
            EAttribute.CritChance => "attribute.crit_chance",
            EAttribute.CritMultiplier => "attribute.crit_multiplier",
            EAttribute.DefensePenetration => "attribute.defense_penetration",
            EAttribute.Defense => "attribute.defense",
            EAttribute.SkillDamageBonus => "attribute.skill_damage_bonus",
            EAttribute.SkillRange => "attribute.skill_range",
            EAttribute.CooldownReduction => "attribute.cooldown_reduction",
            EAttribute.DamageDealt => "attribute.damage_dealt",
            EAttribute.DamageTaken => "attribute.damage_taken",
            _ => "attribute." + attribute.ToString().ToLowerInvariant(),
        };

        /// <summary>Formats an attribute value the way the player reads it (whole numbers, percentages, multiplier).</summary>
        public static string FormatAttribute(EAttribute attribute, float value)
        {
            var culture = CultureInfo.InvariantCulture;
            switch (attribute)
            {
                case EAttribute.CritChance:
                case EAttribute.DefensePenetration:
                case EAttribute.SkillDamageBonus:
                    return Mathf.RoundToInt(value * 100f).ToString(culture) + "%";
                case EAttribute.DamageDealt:
                case EAttribute.DamageTaken:
                    var percent = Mathf.RoundToInt(value * 100f);
                    return (percent > 0 ? "+" : string.Empty) + percent.ToString(culture) + "%";
                case EAttribute.CritMultiplier:
                    return "x" + value.ToString("0.##", culture);
                case EAttribute.Defense:
                    var defense = Mathf.RoundToInt(value).ToString(culture);
                    return CombatResolver.Settings.DefenseMode == EDefenseMode.Percent ? defense + "%" : defense;
                default:
                    return Mathf.RoundToInt(value).ToString(culture);
            }
        }
    }
}
