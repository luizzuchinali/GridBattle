using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    /// <summary>
    /// A grid character (player or enemy). Everything that sets one character
    /// apart from another (attributes, skills and visuals) comes from its
    /// <see cref="CharacterConfig"/>: all characters share the same prefab template
    /// and receive their config on spawn (see <see cref="CharacterFactory"/>).
    /// </summary>
    public class Character : GridEntity, IDamageReceiver, IWalker, IAttacker
    {
        [SerializeField]
        [Tooltip("Set by CharacterFactory on spawn. Only needs to be assigned manually for characters placed directly in the scene.")]
        private CharacterConfig config;

        private int _currentHp = 100;

        public CharacterConfig Config => config;
        public int Current => _currentHp;
        public int MaxHp => config.MaxHp;
        public int WalkDistance => config.WalkDistance;
        public int AttackDistance => config.AttackDistance;
        public int BasicAttackDamage => config.BasicAttackDamage;
        public IReadOnlyList<SkillDefinition> Skills => config.Skills;
        public bool IsDead => _currentHp <= 0;

        public (int CurrentHp, int MaxHp) GetHpInfo() => (_currentHp, MaxHp);

        public event Action<DamageReceiveData> OnHpChanged;

        private void Awake()
        {
            if (config != null)
                _currentHp = config.MaxHp;
        }

        /// <summary>
        /// Applies the config to a freshly instantiated character: attributes, full HP
        /// and visuals (<see cref="CharacterView"/>).
        /// </summary>
        public virtual void Initialize(CharacterConfig characterConfig)
        {
            if (characterConfig == null)
                throw new ArgumentNullException(nameof(characterConfig));

            config = characterConfig;
            _currentHp = config.MaxHp;
            gameObject.name = config.name;

            if (TryGetComponent(out CharacterView view))
                view.Apply(config);
        }

        public void ReceiveDamage(int damage)
        {
            if (IsDead) return;

            _currentHp -= damage;
            OnHpChanged?.Invoke(new DamageReceiveData
            {
                Damage = damage,
                CurrentHp = Current,
                MaxHp = MaxHp,
            });

            if (IsDead)
                Die();
            else if (GetComponentInParent<GridController>() is { } grid)
                grid.PlayHitAnimation(this);
        }

        /// <summary>
        /// Single entry point for skill execution: validates through the SkillDefinition
        /// itself and executes the effect. Controllers and AI actions only forward the call.
        /// </summary>
        public bool TryUseSkill(GridController grid, SkillDefinition skill, Vector2Int targetPos)
        {
            if (IsDead || skill == null) return false;
            if (!skill.CanUse(this, grid, targetPos)) return false;

            return skill.Execute(this, grid, targetPos);
        }

        public bool CanWalk(Vector2Int targetPos)
        {
            return GridRules.IsInWalkRange(CurrentGridPos, targetPos, WalkDistance);
        }

        public void Attack(IDamageReceiver target)
        {
            target.ReceiveDamage(BasicAttackDamage);
        }

        public bool IsInAttackRange(Vector2Int targetPosition)
        {
            return GridRules.IsInAttackRange(CurrentGridPos, targetPosition, AttackDistance);
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
    }
}
