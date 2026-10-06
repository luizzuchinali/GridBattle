using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Rules;
using GridBattle.Gameplay.States;
using GridBattle.Gameplay.Stats;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    /// <summary>
    /// A grid character (player or enemy). Everything that sets one character
    /// apart from another (attributes, skills, initial states and visuals) comes
    /// from its <see cref="CharacterConfig"/>: all characters share the same
    /// prefab template and receive their config on spawn (see
    /// <see cref="CharacterFactory"/>). Attribute values in play come from
    /// <see cref="Stats"/> (config × spawn scaling + active
    /// <see cref="States"/>).
    /// </summary>
    public class Character : GridEntity, IDamageReceiver, IWalker, IAttacker
    {
        [SerializeField]
        [Tooltip("Set by CharacterFactory on spawn. Only needs to be assigned manually for characters placed directly in the scene.")]
        private CharacterConfig config;

        private int _currentHp = 100;
        private CharacterStats _stats;
        private StateContainer _states;
        private CharacterScaling _scaling = CharacterScaling.None;
        private readonly List<SkillDefinition> _skills = new();
        private SkillCooldowns _cooldowns;

        public CharacterConfig Config => config;
        public CharacterScaling Scaling => _scaling;

        /// <summary>Effective attributes (config × scaling + states).</summary>
        public CharacterStats Stats
        {
            get
            {
                if (_stats == null)
                {
                    _stats = new CharacterStats(States.CollectModifiers);
                    ApplyBaseAttributes();
                }

                return _stats;
            }
        }

        /// <summary>Active states (buffs, debuffs, talent passives...).</summary>
        public StateContainer States
        {
            get
            {
                if (_states == null)
                {
                    _states = new StateContainer(this);
                    _states.Changed += OnStatesChanged;
                }

                return _states;
            }
        }

        public int Current => _currentHp;
        public int MaxHp => Mathf.Max(1, Stats.GetInt(EAttribute.MaxHp));

        /// <summary>Walk range in play (0 while a state prevents movement).</summary>
        public int WalkDistance =>
            States.IsRestricted(EBehaviorRestriction.PreventMovement) ? 0 : Stats.GetInt(EAttribute.WalkRange);

        public int AttackDistance => Stats.GetInt(EAttribute.AttackRange);
        public int BasicAttackDamage => Stats.GetInt(EAttribute.BasicDamage);
        public bool CanBasicAttack => !States.IsRestricted(EBehaviorRestriction.PreventBasicAttack);

        /// <summary>
        /// Skills the character can use now (runtime list: the config's starting
        /// skills, replaced by <see cref="SetSkills"/> when the run unlocks more).
        /// </summary>
        public virtual IReadOnlyList<SkillDefinition> Skills => _skills;

        /// <summary>Cooldowns of this character's skills.</summary>
        public SkillCooldowns Cooldowns => _cooldowns ??= new SkillCooldowns(this);

        public bool IsDead => _currentHp <= 0;

        /// <summary>Whether pushes and pulls can move this character (its config says; true without a config).</summary>
        public bool CanBeDisplaced => config == null || config.CanBeDisplaced;

        public (int CurrentHp, int MaxHp) GetHpInfo() => (_currentHp, MaxHp);

        public event Action<DamageReceiveData> OnHpChanged;

        protected virtual void Awake()
        {
            if (config != null)
            {
                ApplyBaseAttributes();
                _currentHp = MaxHp;
                CopyConfigSkills();
            }
        }

        /// <summary>
        /// Applies the config to a freshly instantiated character: attributes (with
        /// the spawn <paramref name="scaling"/>), initial states, full HP and visuals
        /// (<see cref="CharacterView"/>).
        /// </summary>
        public virtual void Initialize(CharacterConfig characterConfig, CharacterScaling scaling)
        {
            if (characterConfig == null)
                throw new ArgumentNullException(nameof(characterConfig));

            config = characterConfig;
            _scaling = scaling;
            ApplyBaseAttributes();
            States.Clear();
            foreach (var grant in config.InitialStates)
            {
                if (grant.IsValid)
                    States.Apply(grant);
            }

            _currentHp = MaxHp;
            gameObject.name = config.name;
            CopyConfigSkills();
            Cooldowns.Reset();
            MovedThisTurn = false;
            MovedLastTurn = false;

            if (TryGetComponent(out CharacterView view))
                view.Apply(config);
        }

        public void Initialize(CharacterConfig characterConfig) => Initialize(characterConfig, CharacterScaling.None);

        /// <summary>Exact damage (no critical, defense or modifiers), e.g. from scripted effects.</summary>
        public void ReceiveDamage(int damage)
        {
            CombatResolver.DealDamage(null, this, damage, EDamageKind.Pure);
        }

        /// <summary>
        /// Applies a resolved hit (called by CombatResolver): shields absorb first,
        /// the rest leaves HP. Fills the HP-related fields of <paramref name="hit"/>.
        /// </summary>
        public void ApplyHit(ref HitResult hit)
        {
            if (IsDead || hit.Damage <= 0) return;

            var toHp = States.Absorb(hit.Damage);
            hit.Absorbed = hit.Damage - toHp;
            hit.HpDamage = toHp;
            _currentHp -= toHp;

            OnHpChanged?.Invoke(new DamageReceiveData
            {
                Damage = hit.Damage,
                Absorbed = hit.Absorbed,
                IsCrit = hit.IsCrit,
                CurrentHp = Current,
                MaxHp = MaxHp,
            });

            if (IsDead)
            {
                hit.Killed = true;
                Die();
            }
            else if (GetComponentInParent<GridController>() is { } grid)
            {
                grid.PlayHitAnimation(this);
            }
        }

        /// <summary>Recovers HP up to the maximum. Returns the HP actually recovered.</summary>
        public int RestoreHp(int amount)
        {
            if (IsDead || amount <= 0) return 0;

            var healed = Mathf.Min(amount, MaxHp - _currentHp);
            if (healed <= 0) return 0;

            _currentHp += healed;
            OnHpChanged?.Invoke(new DamageReceiveData { Healed = healed, CurrentHp = Current, MaxHp = MaxHp });
            return healed;
        }

        /// <summary>Sets HP directly (restoring saved data). Does not kill nor show feedback.</summary>
        public void SetHp(int hp)
        {
            _currentHp = Mathf.Clamp(hp, 1, MaxHp);
            OnHpChanged?.Invoke(new DamageReceiveData { CurrentHp = Current, MaxHp = MaxHp });
        }

        /// <summary>
        /// Replaces the character's usable skills (the run / talent system calls this
        /// with the skills unlocked so far). Cooldowns of skills that stay are kept.
        /// Raises <see cref="SkillListChangedEvent"/>.
        /// </summary>
        public void SetSkills(IEnumerable<SkillDefinition> skills)
        {
            _skills.Clear();
            if (skills != null)
            {
                foreach (var skill in skills)
                {
                    if (skill != null && !_skills.Contains(skill))
                        _skills.Add(skill);
                }
            }

            EventBus.Raise(new SkillListChangedEvent(this));
        }

        /// <summary>
        /// Single entry point for skill execution: validates through the SkillDefinition
        /// itself, executes the effect and starts the cooldown. Controllers and AI actions
        /// only forward the call. An invalid use does nothing and returns false (the turn
        /// is not consumed).
        /// </summary>
        public virtual bool TryUseSkill(GridController grid, SkillDefinition skill, Vector2Int targetPos)
        {
            if (IsDead || skill == null) return false;
            if (States.IsRestricted(EBehaviorRestriction.PreventSkills)) return false;
            if (!skill.CanUse(this, grid, targetPos)) return false;
            if (!skill.Execute(this, grid, targetPos)) return false;

            Cooldowns.Trigger(skill);
            return true;
        }

        public bool CanWalk(Vector2Int targetPos)
        {
            return GridRules.IsInWalkRange(CurrentGridPos, targetPos, WalkDistance);
        }

        public void Attack(IDamageReceiver target)
        {
            if (target is Character character)
                CombatResolver.BasicAttack(this, character);
            else
                target.ReceiveDamage(BasicAttackDamage);
        }

        public bool IsInAttackRange(Vector2Int targetPosition)
        {
            return GridRules.IsInAttackRange(CurrentGridPos, targetPosition, AttackDistance);
        }

        /// <summary>
        /// Start of this character's own turn (called by TurnManager): the movement flag of the turn that just ended
        /// becomes <see cref="MovedLastTurn"/>, then the states' turn-start effects run.
        /// </summary>
        public void BeginTurn()
        {
            MovedLastTurn = MovedThisTurn;
            MovedThisTurn = false;
            States.OnTurnStarted();
        }

        /// <summary>The character walked or teleported during its current turn (reset when its next turn starts).</summary>
        public bool MovedThisTurn { get; private set; }

        /// <summary>
        /// The character walked or teleported during its previous turn. Conditional damage effects read it
        /// ("hit and run", "standing still"): a turn is one action, so the turn in which a character moves is
        /// never the one in which it attacks. Being pushed or pulled does not count.
        /// </summary>
        public bool MovedLastTurn { get; private set; }

        /// <summary>Marks that the character moved on its own this turn (called by <see cref="GridController.MoveEntity"/>).</summary>
        public void RegisterMovement() => MovedThisTurn = true;

        /// <summary>
        /// Restores the movement flag of the last turn from a battle snapshot (taken before the next turn starts,
        /// so the flag it holds is the one that becomes <see cref="MovedLastTurn"/>).
        /// </summary>
        public void RestoreMoved(bool movedLastTurn)
        {
            MovedThisTurn = movedLastTurn;
            MovedLastTurn = false;
        }

        /// <summary>The grid this character stands on (null if it is not on one).</summary>
        public GridController Grid => GetComponentInParent<GridController>();

        /// <summary>
        /// End of this character's own turn (called by TurnManager, right after
        /// <see cref="EntityTurnEndedEvent"/>): skill cooldowns tick, then the state
        /// durations go down.
        /// </summary>
        public void EndTurn()
        {
            Cooldowns.Tick();
            States.OnTurnEnded();
        }

        /// <summary>
        /// Whether dying plays a short visual effect before the object is destroyed.
        /// </summary>
        protected virtual bool PlaysDeathEffect => true;

        protected virtual void Die()
        {
            var grid = GetComponentInParent<GridController>();
            var cell = GetComponentInParent<Cell>();
            if (cell != null && cell.GetContent() == this)
                cell.RemoveContent();

            EventBus.Raise(new CharacterDiedEvent(this));

            if (PlaysDeathEffect && grid != null)
                grid.PlayDeathAnimation(this);
            else
                Destroy(gameObject);
        }

        /// <summary>Starts the runtime skill list from the config's skills (without raising events).</summary>
        private void CopyConfigSkills()
        {
            _skills.Clear();
            if (config == null) return;

            foreach (var skill in config.Skills)
            {
                if (skill != null && !_skills.Contains(skill))
                    _skills.Add(skill);
            }
        }

        private void ApplyBaseAttributes()
        {
            if (config == null) return;

            var stats = _stats;
            if (stats == null) return;

            foreach (EAttribute attribute in Enum.GetValues(typeof(EAttribute)))
                stats.SetBase(attribute, config.GetBaseAttribute(attribute));

            stats.SetBase(EAttribute.MaxHp, config.MaxHp * _scaling.HpMultiplier);
            stats.SetBase(EAttribute.BasicDamage, config.BasicAttackDamage * _scaling.DamageMultiplier);
        }

        /// <summary>Max HP may change with states: keep HP within it and refresh the bar.</summary>
        private void OnStatesChanged()
        {
            if (IsDead) return;

            var maxHp = MaxHp;
            if (_currentHp > maxHp)
                _currentHp = maxHp;
            OnHpChanged?.Invoke(new DamageReceiveData { CurrentHp = Current, MaxHp = maxHp });
        }
    }
}
