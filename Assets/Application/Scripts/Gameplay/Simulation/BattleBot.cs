using System;
using System.Collections.Generic;
using GridBattle.Core.Randomness;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Controllers;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.States;
using GridBattle.Gameplay.States.Effects;
using GridBattle.Gameplay.Stats;
using GridBattle.Gameplay.Terrain;
using UnityEngine;

namespace GridBattle.Gameplay.Simulation
{
    /// <summary>
    /// The player's brain in the balance simulation: plays one turn through the same entry points as the real input
    /// (a tap on a cell, <see cref="PlayerCharacterController.TryUseSkill"/> and
    /// <see cref="PlayerCharacterController.TryUseConsumable"/>), so every rule applies as it does for a person.
    /// Deterministic: enemies are always looked at in the same order and every tie is broken with the bot's own
    /// <see cref="Rng"/> (never the run's streams).
    /// <para>
    /// Policy of a turn, in this order: (1) consumables, which do not take the action: healing below a HP fraction,
    /// bombs on clusters or kills, support items below a HP fraction; (2) an attack that kills (most kills, then the
    /// most wanted roles, then the most damage, then the basic attack to save the skill); (3) a defensive skill that
    /// heals when the HP is low; (4) a rush toward an enemy of a rush role (a summoner) that nothing reaches this
    /// turn; (5) a skill when it hits several enemies or out-damages the basic attack, else the basic attack on the
    /// most wanted enemy (role order, then the lowest HP); (6) a defensive skill that buffs the caster when enemies
    /// are close; (7) a step toward the best target, avoiding hazard cells and liking bonus ones.
    /// </para>
    /// <para>
    /// Skills that push or pull (<see cref="DisplaceSkillEffect"/>) are valued with the same prediction the game
    /// uses (<see cref="DisplacementResolver.Predict"/>): the collision and terrain damage count as damage (and can
    /// kill), and a bonus in HP-equivalents is added for sending an enemy that could hit the player out of reach,
    /// pulling a ranged enemy into the player's reach and sending enemies onto hazards (the opposite is charged).
    /// A displacing skill whose total value is not positive is not an option.
    /// </para>
    /// <para>
    /// Skills are valued as the player would use them: through <see cref="EffectiveSkill"/> (talent modifiers on
    /// damage, area, range, cooldown, push distance and attached effects) and with
    /// <see cref="CombatResolver.PredictDamage"/> (conditional bonuses of passives: adjacent enemies, poisoned
    /// targets, moving first...). The damage over time of the states a skill applies to its targets is counted in
    /// its expected damage (weighted by <see cref="BattleBotOptions.StateValueWeight"/>), never in its kills.
    /// </para>
    /// </summary>
    public sealed class BattleBot : IDisposable
    {
        private const float Epsilon = 0.0001f;
        private const float LethalCost = 1000f;

        private readonly BattleBotOptions _options;
        private readonly Rng _rng;
        private readonly List<Enemy> _enemies = new();
        private int _actionsRaised;
        private int _turnsInBattle;
        private int _rushStreak;
        private bool _subscribed;

        /// <summary>Optional listener of the bot's decisions (one line per decision), for debugging the bot.</summary>
        public Action<string> Trace { get; set; }

        /// <param name="options">The bot's settings.</param>
        /// <param name="rng">The bot's own random stream (never one of the run's).</param>
        public BattleBot(BattleBotOptions options, Rng rng)
        {
            _options = options ?? new BattleBotOptions();
            _rng = rng ?? new Rng(1UL);
            EventBus.Subscribe<PlayerActionEvent>(OnPlayerAction);
            EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
            _subscribed = true;
        }

        public void Dispose()
        {
            if (!_subscribed) return;

            _subscribed = false;
            EventBus.Unsubscribe<PlayerActionEvent>(OnPlayerAction);
            EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
        }

        private void OnPlayerAction(PlayerActionEvent e) => _actionsRaised++;

        private void OnBattleStarted(BattleStartedEvent e)
        {
            _turnsInBattle = 0;
            _rushStreak = 0;
        }

        /// <summary>
        /// Plays the player's turn: items first (they are free), then the one action. Returns false when the bot
        /// found no legal action at all (the player cannot move, attack or use a skill).
        /// </summary>
        public bool TakeTurn(GridController grid, PlayerCharacter player)
        {
            if (grid == null || player == null || player.IsDead) return false;
            if (!player.TryGetComponent(out PlayerCharacterController controller)) return false;

            CollectEnemies(player);
            if (_enemies.Count == 0) return true;
            _turnsInBattle++;

            if (Trace != null)
            {
                var list = new System.Text.StringBuilder();
                foreach (var enemy in _enemies)
                    list.Append($" {enemy.name}@{enemy.CurrentGridPos.x},{enemy.CurrentGridPos.y}:{enemy.Current}");
                Trace($"hp {player.Current}/{player.MaxHp} at {player.CurrentGridPos.x},{player.CurrentGridPos.y} " +
                      $"enemies{list}");
            }

            if (_options.UseConsumables)
            {
                TryUseConsumable(grid, player, controller);
                CollectEnemies(player);
                if (_enemies.Count == 0 || player.IsDead) return true;
            }

            var options = new List<ActionOption>();
            BuildBasicOptions(grid, player, options);
            if (_options.UseSkills)
                BuildSkillOptions(grid, player, options);

            var chosen = PickKill(options);
            if (chosen == null)
            {
                if (_options.UseSkills && TryUtilitySkill(grid, player, controller, true))
                    return true;

                if (_rushStreak < _options.RushMaxStreak && TryRush(grid, player, options))
                {
                    _rushStreak++;
                    return true;
                }

                _rushStreak = 0;
                chosen = PickAttack(options);
            }

            if (chosen != null && Execute(grid, controller, chosen))
            {
                Trace?.Invoke(chosen.Skill != null
                    ? $"  skill {chosen.Skill.name} at {chosen.Cell.x},{chosen.Cell.y} hits {chosen.Targets} (kills {chosen.Kills}, dmg {chosen.Effective:0})"
                    : $"  attack {chosen.Cell.x},{chosen.Cell.y} (kills {chosen.Kills}, dmg {chosen.Effective:0})");
                return true;
            }

            if (_options.UseSkills && TryUtilitySkill(grid, player, controller, false))
                return true;

            return TryMove(grid, player);
        }

        // ------------------------------------------------------------------------------------ enemies

        private void CollectEnemies(PlayerCharacter player)
        {
            _enemies.Clear();
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
            {
                if (enemy != null && !enemy.IsDead)
                    _enemies.Add(enemy);
            }

            // Find order is not stable: a fixed order keeps the bot deterministic.
            _enemies.Sort((a, b) =>
            {
                var byY = a.CurrentGridPos.y.CompareTo(b.CurrentGridPos.y);
                if (byY != 0) return byY;
                var byX = a.CurrentGridPos.x.CompareTo(b.CurrentGridPos.x);
                return byX != 0 ? byX : string.CompareOrdinal(a.name, b.name);
            });
        }

        /// <summary>How much the bot wants this enemy dead: its role's tier in the focus order (4 = first), else 0.</summary>
        private float Tier(Enemy enemy)
        {
            var role = enemy.Role;
            if (role == null) return 0f;

            var order = _options.FocusRoleOrder;
            for (var i = 0; i < order.Count; i++)
            {
                if (order[i] == role.name)
                    return order.Count - i;
            }

            return 0f;
        }

        // ------------------------------------------------------------------------------------ attack options

        private sealed class ActionOption
        {
            /// <summary>The skill, or null for the basic attack.</summary>
            public SkillDefinition Skill;

            public Vector2Int Cell;
            public int Kills;
            public float KillTier;
            public float Effective;
            public float Expected;
            public int Targets;
            public float FocusTier;
            public int LowestHp;

            /// <summary>Enemies the action reaches.</summary>
            public readonly List<Enemy> Hit = new();

            /// <summary>No damage: the skill only applies states to enemies.</summary>
            public bool ControlOnly;

            /// <summary>Damage the skill itself is predicted to do to each enemy it hits (parallel to <see cref="Hit"/>).</summary>
            public readonly List<int> SkillDamage = new();

            /// <summary>Positional value of a push or pull, in HP-equivalents (0 for every other action).</summary>
            public float Bonus;
        }

        private void BuildBasicOptions(GridController grid, PlayerCharacter player, List<ActionOption> into)
        {
            if (!player.CanBasicAttack) return;

            var damage = player.Stats.GetInt(EAttribute.BasicDamage);
            foreach (var enemy in _enemies)
            {
                if (!GridRules.IsAttackTarget(grid, player, enemy.CurrentGridPos)) continue;

                var hp = enemy.Current + enemy.States.TotalShield;
                var predicted = Predict(player, enemy, damage, EDamageKind.BasicAttack);
                var kills = predicted >= hp ? 1 : 0;
                into.Add(new ActionOption
                {
                    Cell = enemy.CurrentGridPos,
                    Kills = kills,
                    KillTier = kills > 0 ? Tier(enemy) : 0f,
                    Effective = Math.Min(predicted, hp),
                    Expected = Math.Min(predicted * CritFactor(player, EDamageKind.BasicAttack), hp),
                    Targets = 1,
                    FocusTier = Tier(enemy),
                    LowestHp = enemy.Current,
                    Hit = { enemy },
                });
            }
        }

        private void BuildSkillOptions(GridController grid, PlayerCharacter player, List<ActionOption> into)
        {
            var skills = player.Skills;
            for (var i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                if (skill == null || !SkillTargeting.CanSelect(player, skill)) continue;

                var effective = EffectiveSkill.Resolve(player, skill);
                var damaging = skill.Damage > 0;
                var control = !damaging && (AppliesStatesToTargets(effective) || effective.FindDisplacement() != null);
                if (!damaging && !control) continue;

                var baseDamage = damaging ? effective.Damage : 0;
                ActionOption best = null;
                var ties = 1;
                foreach (var cell in SkillTargeting.GetTargetableCells(grid, player, skill))
                {
                    var option = EvaluateSkillCell(grid, player, effective, cell, baseDamage, control);
                    if (option == null) continue;

                    var comparison = best == null ? 1 : CompareSkillOptions(option, best);
                    if (comparison > 0)
                    {
                        best = option;
                        ties = 1;
                    }
                    else if (comparison == 0 && _rng.Range(0, ++ties) == 0)
                    {
                        best = option;
                    }
                }

                if (best != null)
                    into.Add(best);
            }
        }

        private ActionOption EvaluateSkillCell(GridController grid, PlayerCharacter player, in EffectiveSkill effective,
            Vector2Int cell, int baseDamage, bool control)
        {
            var skill = effective.Skill;
            var affected = SkillTargeting.GetAffectedCharacters(grid, player, effective, cell);
            var option = new ActionOption { Skill = skill, Cell = cell, ControlOnly = control, LowestHp = int.MaxValue };
            foreach (var character in affected)
            {
                // Never hurt itself.
                if (character is PlayerCharacter) return null;
                if (character is not Enemy enemy) continue;

                var hp = enemy.Current + enemy.States.TotalShield;
                var predicted = baseDamage > 0 ? Predict(player, enemy, baseDamage, EDamageKind.Skill) : 0;
                option.Targets++;
                option.Hit.Add(enemy);
                option.SkillDamage.Add(predicted);
                option.Effective += Math.Min(predicted, hp);
                var expected = Math.Min(predicted * CritFactor(player, EDamageKind.Skill), hp);
                option.Expected += expected + GetStateValue(effective, enemy, hp - expected);
                option.FocusTier = Math.Max(option.FocusTier, Tier(enemy));
                option.LowestHp = Math.Min(option.LowestHp, enemy.Current);
                if (baseDamage > 0 && predicted >= hp)
                {
                    option.Kills++;
                    option.KillTier += Tier(enemy);
                }
            }

            if (option.Targets == 0) return null;

            var displace = effective.FindDisplacement();
            if (displace != null && !ValueDisplacement(grid, player, effective, displace, cell, affected, option))
                return null;

            return option;
        }

        /// <summary>
        /// What the damage-over-time states the skill applies to the enemy are expected to do before the enemy
        /// would be dead anyway, in HP (<paramref name="hpLeft"/> is what the enemy has left after the skill's damage),
        /// weighted by <see cref="BattleBotOptions.StateValueWeight"/>. Zero for a skill that applies no such state.
        /// </summary>
        private float GetStateValue(in EffectiveSkill effective, Enemy enemy, float hpLeft)
        {
            if (_options.StateValueWeight <= 0f || hpLeft <= 0f) return 0f;

            var total = 0f;
            foreach (var effect in effective.Effects)
            {
                if (effect is not ApplyStatesSkillEffect { Recipient: ESkillEffectRecipient.AffectedTargets } applies)
                    continue;

                foreach (var grant in applies.States)
                {
                    if (!grant.IsValid) continue;

                    var perTurn = 0f;
                    foreach (var stateEffect in grant.State.Effects)
                    {
                        if (stateEffect is PeriodicDamageEffect periodic)
                            perTurn += periodic.GetDamagePerTurn(enemy, grant.Stacks);
                    }

                    if (perTurn <= 0f) continue;

                    var turns = grant.IsPermanent ? 3 : Math.Max(1, grant.Duration + effective.StateDurationBonus);
                    total += perTurn * turns;
                }
            }

            return Math.Min(total, hpLeft) * _options.StateValueWeight;
        }

        /// <summary>
        /// Adds what a push or pull is worth to <paramref name="option"/>: collision and terrain damage (recomputing the
        /// damage, kills and expected damage per enemy, the displaced and the ones it hits) and the positional bonus.
        /// Returns false when the use is not worth taking (it does not help the player).
        /// </summary>
        private bool ValueDisplacement(GridController grid, PlayerCharacter player, in EffectiveSkill effective,
            DisplaceSkillEffect displace, Vector2Int cell, List<Character> affected, ActionOption option)
        {
            var forecasts = DisplacementResolver.Predict(grid, player, affected, displace.Mode,
                effective.GetDisplacementDistance(displace), cell);
            var extra = new Dictionary<Enemy, int>();
            var order = new List<Enemy>(option.Hit);
            var bonus = 0f;
            var playerPos = player.CurrentGridPos;
            foreach (var forecast in forecasts)
            {
                var result = forecast.Result;
                if (result.Target is not Enemy enemy) continue;

                AddExtra(extra, order, enemy, forecast.CollisionDamage + forecast.TerrainDamage);
                if (forecast.HitCharacter is Enemy hit)
                    AddExtra(extra, order, hit, forecast.HitCharacterDamage);

                if (result.CellsMoved > 0)
                {
                    // Sent out of the reach of its attack (it needs a turn to come back), or brought into it.
                    var before = GridRules.IsInAttackRange(result.Start, playerPos, enemy.AttackDistance);
                    var after = GridRules.IsInAttackRange(result.Final, playerPos, enemy.AttackDistance);
                    if (before && !after) bonus += _options.DisplaceOutOfReachValue;
                    else if (!before && after) bonus -= _options.DisplaceOutOfReachValue;

                    // A ranged enemy pulled from out of the player's reach into it can be hit.
                    if (enemy.AttackDistance >= 2 &&
                        !GridRules.IsInAttackRange(playerPos, result.Start, player.AttackDistance) &&
                        GridRules.IsInAttackRange(playerPos, result.Final, player.AttackDistance))
                        bonus += _options.DisplacePullInValue;
                }

                if (forecast.ForcedTerrain != null)
                {
                    var value = CountStates(forecast.ForcedTerrain) * _options.DisplaceTerrainStateValue;
                    bonus += forecast.ForcedTerrain.Kind == ETerrainKind.Bonus ? -value : value;
                }

                // Leaving a bonus cell (an enemy sitting on a shield or regeneration cell) is worth as much as
                // being sent onto a hazard; leaving a hazard is a cost.
                var startTerrain = result.CellsMoved > 0 ? grid.GetTerrain(result.Start) : null;
                if (startTerrain != null && startTerrain.HasEffect && startTerrain.AffectsCharacter(enemy))
                {
                    var value = CountStates(startTerrain) * _options.DisplaceTerrainStateValue;
                    bonus += startTerrain.Kind == ETerrainKind.Bonus ? value : -value;
                }
            }

            // Per enemy: skill damage + collision/terrain damage decide the kills, the damage and the expected damage.
            option.Effective = 0f;
            option.Expected = 0f;
            option.Kills = 0;
            option.KillTier = 0f;
            var crit = CritFactor(player, EDamageKind.Skill);
            foreach (var enemy in order)
            {
                var hp = enemy.Current + enemy.States.TotalShield;
                var index = option.Hit.IndexOf(enemy);
                var skillDamage = index >= 0 ? option.SkillDamage[index] : 0;
                extra.TryGetValue(enemy, out var extraDamage);
                var total = skillDamage + extraDamage;
                option.Effective += Math.Min(total, hp);
                option.Expected += Math.Min(skillDamage * crit + extraDamage, hp);
                if (total > 0 && total >= hp)
                {
                    option.Kills++;
                    option.KillTier += Tier(enemy);
                }
            }

            option.Bonus = bonus;
            option.Expected += bonus;
            return option.Effective + bonus > 0f;
        }

        private static int CountStates(TerrainDefinition terrain)
        {
            var states = 0;
            foreach (var grant in terrain.States)
            {
                if (grant.IsValid) states++;
            }

            return states;
        }

        private static void AddExtra(Dictionary<Enemy, int> extra, List<Enemy> order, Enemy enemy, int damage)
        {
            if (damage <= 0) return;

            extra.TryGetValue(enemy, out var current);
            extra[enemy] = current + damage;
            if (!order.Contains(enemy))
                order.Add(enemy);
        }

        private static int CompareSkillOptions(ActionOption a, ActionOption b)
        {
            var kills = a.Kills.CompareTo(b.Kills);
            if (kills != 0) return kills;
            var effective = (a.Effective + a.Bonus).CompareTo(b.Effective + b.Bonus);
            if (effective != 0) return effective;
            return a.Targets.CompareTo(b.Targets);
        }

        /// <summary>The best option that kills (most kills, wanted roles, damage; the basic attack on a tie), or null.</summary>
        private ActionOption PickKill(List<ActionOption> options)
        {
            ActionOption best = null;
            var ties = 1;
            foreach (var option in options)
            {
                if (option.Kills <= 0) continue;

                var comparison = best == null ? 1 : CompareKills(option, best);
                if (comparison > 0)
                {
                    best = option;
                    ties = 1;
                }
                else if (comparison == 0 && _rng.Range(0, ++ties) == 0)
                {
                    best = option;
                }
            }

            return best;
        }

        private static int CompareKills(ActionOption a, ActionOption b)
        {
            var kills = a.Kills.CompareTo(b.Kills);
            if (kills != 0) return kills;
            var tier = a.KillTier.CompareTo(b.KillTier);
            if (tier != 0) return tier;
            var effective = a.Effective.CompareTo(b.Effective);
            if (Math.Abs(a.Effective - b.Effective) > Epsilon) return effective;

            // Same result: keep the skill for later.
            return (a.Skill == null ? 1 : 0).CompareTo(b.Skill == null ? 1 : 0);
        }

        /// <summary>Without a kill: a skill that hits several enemies or beats the basic attack, else the basic attack.</summary>
        private ActionOption PickAttack(List<ActionOption> options)
        {
            ActionOption basic = null;
            ActionOption skill = null;
            ActionOption control = null;
            var basicTies = 1;
            var skillTies = 1;
            var controlTies = 1;
            foreach (var option in options)
            {
                if (option.Skill == null)
                {
                    var comparison = basic == null ? 1 : CompareBasic(option, basic);
                    if (comparison > 0)
                    {
                        basic = option;
                        basicTies = 1;
                    }
                    else if (comparison == 0 && _rng.Range(0, ++basicTies) == 0)
                    {
                        basic = option;
                    }
                }
                else if (option.ControlOnly)
                {
                    var comparison = control == null ? 1 : option.Targets.CompareTo(control.Targets);
                    if (comparison > 0)
                    {
                        control = option;
                        controlTies = 1;
                    }
                    else if (comparison == 0 && _rng.Range(0, ++controlTies) == 0)
                    {
                        control = option;
                    }
                }
                else
                {
                    var comparison = skill == null ? 1 : CompareExpected(option, skill);
                    if (comparison > 0)
                    {
                        skill = option;
                        skillTies = 1;
                    }
                    else if (comparison == 0 && _rng.Range(0, ++skillTies) == 0)
                    {
                        skill = option;
                    }
                }
            }

            if (skill != null && (basic == null || skill.Targets >= _options.SkillMinTargets ||
                                  skill.Expected > basic.Expected + Epsilon))
                return skill;

            if (control != null && control.Targets >= _options.SkillMinTargets)
                return control;

            return basic ?? skill;
        }

        private static int CompareBasic(ActionOption a, ActionOption b)
        {
            var tier = a.FocusTier.CompareTo(b.FocusTier);
            if (tier != 0) return tier;
            var hp = b.LowestHp.CompareTo(a.LowestHp);
            if (hp != 0) return hp;
            return a.Effective.CompareTo(b.Effective);
        }

        private static int CompareExpected(ActionOption a, ActionOption b)
        {
            if (Math.Abs(a.Expected - b.Expected) > Epsilon) return a.Expected.CompareTo(b.Expected);
            return a.Targets.CompareTo(b.Targets);
        }

        private bool Execute(GridController grid, PlayerCharacterController controller, ActionOption option)
        {
            if (option.Skill != null)
                return controller.TryUseSkill(option.Skill, option.Cell);

            var cell = grid.GetCell(option.Cell);
            if (cell == null) return false;

            var before = _actionsRaised;
            EventBus.Raise(new CellTapEvent { Cell = cell });
            return _actionsRaised > before;
        }

        // ------------------------------------------------------------------------------------ damage prediction

        /// <summary>The damage a hit would do now, without a critical and without touching any random stream.</summary>
        private static int Predict(Character attacker, Character target, int baseDamage, EDamageKind kind)
        {
            return CombatResolver.PredictDamage(attacker, target, baseDamage, kind);
        }

        /// <summary>Expected multiplier of the critical chance, for comparing skills with the basic attack.</summary>
        private static float CritFactor(Character attacker, EDamageKind kind)
        {
            var stats = attacker.Stats;
            var chance = Mathf.Min(stats.Get(EAttribute.CritChance), CombatResolver.Settings.CritChanceCap);
            if (chance <= 0f) return 1f;

            return 1f + chance * (Mathf.Max(1f, stats.Get(EAttribute.CritMultiplier)) - 1f);
        }

        // ------------------------------------------------------------------------------------ consumables

        private void TryUseConsumable(GridController grid, PlayerCharacter player, PlayerCharacterController controller)
        {
            var inventory = player.Inventory;
            var hpFraction = player.Current / (float)Math.Max(1, player.MaxHp);

            // 1. Healing when the HP is low.
            if (hpFraction < _options.PotionHpFraction)
            {
                for (var slot = 0; slot < inventory.Slots; slot++)
                {
                    var item = inventory.Get(slot);
                    if (item == null || !HasHeal(item) || !ConsumableRules.CanUse(grid, player, item, player.CurrentGridPos, true))
                        continue;

                    if (controller.TryUseConsumable(slot, player.CurrentGridPos))
                    {
                        Trace?.Invoke($"  item {item.name} (heal)");
                        return;
                    }
                }
            }

            // 2. Area items on clusters or kills.
            for (var slot = 0; slot < inventory.Slots; slot++)
            {
                var item = inventory.Get(slot);
                if (item == null || !item.NeedsTarget || !ConsumableRules.CanSelect(grid, player, item, true)) continue;

                var damage = GetDamage(item);
                if (damage <= 0 && !AppliesStatesToTargets(item)) continue;

                if (!TryFindItemCell(grid, player, item, damage, out var cell)) continue;
                if (controller.TryUseConsumable(slot, cell))
                {
                    Trace?.Invoke($"  item {item.name} at {cell.x},{cell.y}");
                    return;
                }
            }

            // 3. Items that only help the user (regeneration, shield...).
            if (hpFraction < _options.SupportItemHpFraction)
            {
                for (var slot = 0; slot < inventory.Slots; slot++)
                {
                    var item = inventory.Get(slot);
                    if (item == null || item.NeedsTarget || HasHeal(item) || !AppliesStatesToUser(item)) continue;
                    if (!ConsumableRules.CanUse(grid, player, item, player.CurrentGridPos, true)) continue;

                    if (controller.TryUseConsumable(slot, player.CurrentGridPos))
                    {
                        Trace?.Invoke($"  item {item.name} (support)");
                        return;
                    }
                }
            }
        }

        private bool TryFindItemCell(GridController grid, PlayerCharacter player, ConsumableDefinition item, int damage,
            out Vector2Int bestCell)
        {
            bestCell = default;
            var bestScore = float.MinValue;
            var found = false;
            var ties = 1;
            foreach (var cell in ConsumableRules.GetTargetableCells(grid, player, item))
            {
                var area = ConsumableRules.GetAreaCells(grid, player, item, cell);
                var affected = ConsumableRules.GetAffectedCharacters(grid, player, item, area);
                var enemies = 0;
                var kills = 0;
                var hurtsSelf = false;
                foreach (var character in affected)
                {
                    if (character is PlayerCharacter)
                    {
                        hurtsSelf = true;
                        break;
                    }

                    if (character is not Enemy enemy) continue;

                    enemies++;
                    if (damage > 0 && Predict(player, enemy, damage, EDamageKind.Consumable) >=
                        enemy.Current + enemy.States.TotalShield)
                        kills++;
                }

                if (hurtsSelf || (enemies < _options.AreaItemMinTargets && kills == 0)) continue;

                var score = kills * 100f + enemies;
                if (!found || score > bestScore + Epsilon)
                {
                    bestScore = score;
                    bestCell = cell;
                    found = true;
                    ties = 1;
                }
                else if (Math.Abs(score - bestScore) <= Epsilon && _rng.Range(0, ++ties) == 0)
                {
                    bestCell = cell;
                }
            }

            return found;
        }

        private static bool HasHeal(ConsumableDefinition item)
        {
            foreach (var effect in item.Effects)
            {
                if (effect is HealConsumableEffect) return true;
            }

            return false;
        }

        private static int GetDamage(ConsumableDefinition item)
        {
            var total = 0;
            foreach (var effect in item.Effects)
            {
                if (effect is DamageConsumableEffect damage)
                    total += damage.Damage;
            }

            return total;
        }

        private static bool AppliesStatesToUser(ConsumableDefinition item)
        {
            foreach (var effect in item.Effects)
            {
                if (effect is ApplyStatesConsumableEffect { Recipient: EConsumableRecipient.User }) return true;
            }

            return false;
        }

        private static bool AppliesStatesToTargets(ConsumableDefinition item)
        {
            foreach (var effect in item.Effects)
            {
                if (effect is ApplyStatesConsumableEffect { Recipient: EConsumableRecipient.AffectedTargets }) return true;
            }

            return false;
        }

        // ------------------------------------------------------------------------------------ utility skills

        private static bool AppliesStatesToTargets(in EffectiveSkill skill)
        {
            foreach (var effect in skill.Effects)
            {
                if (effect is ApplyStatesSkillEffect { Recipient: ESkillEffectRecipient.AffectedTargets }) return true;
            }

            return false;
        }

        /// <summary>
        /// A skill without damage that helps the caster: <paramref name="healing"/> picks the ones that heal (used
        /// when the HP is low), otherwise the ones that give the caster states (used when an enemy is close and
        /// the states are not on it yet). Returns true when one was used (it takes the action).
        /// </summary>
        private bool TryUtilitySkill(GridController grid, PlayerCharacter player, PlayerCharacterController controller,
            bool healing)
        {
            var hpFraction = player.Current / (float)Math.Max(1, player.MaxHp);
            if (healing && hpFraction >= _options.HealSkillHpFraction) return false;

            var skills = player.Skills;
            for (var i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                if (skill == null || skill.Damage > 0 || !SkillTargeting.CanSelect(player, skill)) continue;
                if (!HelpsCaster(skill, player, healing)) continue;

                if (!healing && !AnyEnemyWithin(player, 4)) continue;

                var cell = ChooseUtilityCell(grid, player, skill);
                if (cell == null) continue;
                if (controller.TryUseSkill(skill, cell.Value))
                {
                    Trace?.Invoke($"  utility skill {skill.name}");
                    return true;
                }
            }

            return false;
        }

        private static bool HelpsCaster(SkillDefinition skill, PlayerCharacter player, bool healing)
        {
            foreach (var effect in skill.Effects)
            {
                if (healing)
                {
                    if (effect is HealSkillEffect heal && heal.Recipient == ESkillEffectRecipient.Caster) return true;
                    continue;
                }

                if (effect is ApplyStatesSkillEffect { Recipient: ESkillEffectRecipient.Caster } states)
                {
                    foreach (var grant in states.States)
                    {
                        if (grant.IsValid && !player.States.Has(grant.State)) return true;
                    }
                }
            }

            return false;
        }

        private static Vector2Int? ChooseUtilityCell(GridController grid, PlayerCharacter player, SkillDefinition skill)
        {
            Vector2Int? first = null;
            foreach (var cell in SkillTargeting.GetTargetableCells(grid, player, skill))
            {
                if (cell == player.CurrentGridPos) return cell;
                first ??= cell;
            }

            return first;
        }

        private bool AnyEnemyWithin(PlayerCharacter player, int manhattanDistance)
        {
            foreach (var enemy in _enemies)
            {
                var delta = enemy.CurrentGridPos - player.CurrentGridPos;
                if (Math.Abs(delta.x) + Math.Abs(delta.y) <= manhattanDistance) return true;
            }

            return false;
        }

        // ------------------------------------------------------------------------------------ movement

        private bool TryMove(GridController grid, PlayerCharacter player)
        {
            return WalkToward(grid, player, ChooseMoveTarget(player), false);
        }

        /// <summary>
        /// Walks toward an enemy of a rush role (see <see cref="BattleBotOptions.RushRoleNames"/>) that no attack or
        /// skill reaches this turn, instead of fighting the enemies around. Returns false when there is none, it can
        /// be hit now or no step brings the bot closer.
        /// </summary>
        private bool TryRush(GridController grid, PlayerCharacter player, List<ActionOption> options)
        {
            // In a stand-off (see BattleBotOptions.StallTurns) the bot chases the enemies it would kill first even when a
            // ranged skill reaches them: a pair of healers can out-heal a few dagger throws forever.
            var stalled = _turnsInBattle > _options.StallTurns;
            var roles = stalled ? _options.FocusRoleOrder : _options.RushRoleNames;
            if (roles.Count == 0) return false;

            Enemy target = null;
            var bestDistance = int.MaxValue;
            foreach (var enemy in _enemies)
            {
                var role = enemy.Role;
                if (role == null || !ContainsName(roles, role.name)) continue;

                var delta = enemy.CurrentGridPos - player.CurrentGridPos;
                var distance = Math.Abs(delta.x) + Math.Abs(delta.y);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    target = enemy;
                }
            }

            if (target == null) return false;

            foreach (var option in options)
            {
                if (option.Hit.Contains(target) && (!stalled || option.Skill == null)) return false;
            }

            return WalkToward(grid, player, target, true);
        }

        private static bool ContainsName(IReadOnlyList<string> names, string name)
        {
            for (var i = 0; i < names.Count; i++)
            {
                if (names[i] == name) return true;
            }

            return false;
        }

        private bool WalkToward(GridController grid, PlayerCharacter player, Enemy target, bool requireProgress)
        {
            if (target == null) return false;

            var route = ComputeRoute(grid, target.CurrentGridPos);
            var attackRange = Math.Max(1, player.AttackDistance);
            var current = player.CurrentGridPos;
            var currentLength = route[current.x, current.y];
            var currentApproach = currentLength < 0 ? 50f : Math.Max(0, currentLength - attackRange);
            var size = grid.Size;
            var bestCost = float.MaxValue;
            Vector2Int? best = null;
            var ties = 1;
            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    var position = new Vector2Int(x, y);
                    if (!GridRules.CanWalkTo(grid, player, position)) continue;

                    var length = route[x, y];
                    var approach = length < 0
                        ? 50f + Vector2Int.Distance(position, target.CurrentGridPos)
                        : Math.Max(0, length - attackRange);
                    // A rush goes through hazards: the goal is worth the damage.
                    var cost = approach * 2f + (requireProgress ? LethalTerrainCost(grid, player, position) : TerrainCost(grid, player, position)) +
                               Vector2Int.Distance(position, target.CurrentGridPos) * 0.01f;

                    if (best == null || cost < bestCost - Epsilon)
                    {
                        bestCost = cost;
                        best = position;
                        ties = 1;
                    }
                    else if (Math.Abs(cost - bestCost) <= Epsilon && _rng.Range(0, ++ties) == 0)
                    {
                        best = position;
                    }
                }
            }

            if (best == null) return false;
            if (requireProgress)
            {
                var length = route[best.Value.x, best.Value.y];
                var approach = length < 0 ? 50f : Math.Max(0, length - attackRange);
                if (approach >= currentApproach) return false;
            }

            var cell = grid.GetCell(best.Value);
            if (cell == null) return false;

            var before = _actionsRaised;
            EventBus.Raise(new CellTapEvent { Cell = cell });
            Trace?.Invoke($"  {(requireProgress ? "rush" : "walk")} to {best.Value.x},{best.Value.y} toward {target.name}");
            return _actionsRaised > before;
        }

        /// <summary>The enemy to walk toward: its role tier (worth some route steps) minus its distance.</summary>
        private Enemy ChooseMoveTarget(PlayerCharacter player)
        {
            Enemy best = null;
            var bestScore = float.MinValue;
            var ties = 1;
            foreach (var enemy in _enemies)
            {
                var delta = enemy.CurrentGridPos - player.CurrentGridPos;
                var distance = Math.Abs(delta.x) + Math.Abs(delta.y);
                var score = Tier(enemy) * _options.RoleTierDistanceBonus - distance;
                if (best == null || score > bestScore + Epsilon)
                {
                    best = enemy;
                    bestScore = score;
                    ties = 1;
                }
                else if (Math.Abs(score - bestScore) <= Epsilon && _rng.Range(0, ++ties) == 0)
                {
                    best = enemy;
                }
            }

            return best;
        }

        /// <summary>
        /// Cost of standing on a cell: hazards that hurt the player cost steps (more the lower the HP, and a cell
        /// that would kill it is never worth it), bonus cells give some back.
        /// </summary>
        private float TerrainCost(GridController grid, PlayerCharacter player, Vector2Int position)
        {
            // A stand-off (a hazard on the only way to an enemy that keeps its distance): only a lethal cell counts.
            if (_turnsInBattle > _options.StallTurns) return LethalTerrainCost(grid, player, position);

            var terrain = grid.GetTerrain(position);
            if (terrain == null || !terrain.HasEffect || !terrain.AffectsPlayer) return 0f;

            switch (terrain.Kind)
            {
                case ETerrainKind.Hazard:
                    if (terrain.Damage >= player.Current) return LethalCost;
                    var missing = 1f - player.Current / (float)Math.Max(1, player.MaxHp);
                    return _options.HazardPenalty * (1f + 2f * missing);
                case ETerrainKind.Bonus:
                    return -_options.BonusReward;
                default:
                    return 0f;
            }
        }

        /// <summary>Only a cell that would kill the player still costs something during a rush.</summary>
        private float LethalTerrainCost(GridController grid, PlayerCharacter player, Vector2Int position)
        {
            var terrain = grid.GetTerrain(position);
            var lethal = terrain != null && terrain.Kind == ETerrainKind.Hazard && terrain.AffectsPlayer &&
                         terrain.Damage >= player.Current;
            return lethal ? LethalCost : 0f;
        }

        private static readonly Vector2Int[] Steps = { new(0, -1), new(-1, 0), new(1, 0), new(0, 1) };

        /// <summary>Route length (4-adjacent steps, characters ignored) from every cell to the target; -1 = no route.</summary>
        private static int[,] ComputeRoute(GridController grid, Vector2Int target)
        {
            var size = grid.Size;
            var lengths = new int[size.x, size.y];
            for (var x = 0; x < size.x; x++)
            {
                for (var y = 0; y < size.y; y++)
                    lengths[x, y] = -1;
            }

            if (!grid.IsValidPosition(target)) return lengths;

            var queue = new Queue<Vector2Int>();
            lengths[target.x, target.y] = 0;
            queue.Enqueue(target);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var step in Steps)
                {
                    var next = current + step;
                    if (!grid.IsWalkable(next) || lengths[next.x, next.y] >= 0) continue;

                    lengths[next.x, next.y] = lengths[current.x, current.y] + 1;
                    queue.Enqueue(next);
                }
            }

            return lengths;
        }
    }
}
