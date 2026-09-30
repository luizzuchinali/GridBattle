using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    public class Character : GridEntity, IDamageReceiver, IWalker, IAttacker
    {
        [SerializeField]
        private CharacterConfig config;

        private int current = 100;

        public CharacterConfig Config => config;
        public int Current => current;
        public int MaxHp => config != null ? config.MaxHp : 100;
        public int WalkDistance => config != null ? config.WalkDistance : 1;
        public int AttackDistance => config != null ? config.AttackDistance : 1;
        public int BasicAttackDamage => config != null ? config.BasicAttackDamage : 10;
        public IReadOnlyList<Skills.SkillDefinition> Skills => config != null ? config.Skills : Array.Empty<Skills.SkillDefinition>();
        public bool IsDead => current <= 0;

        public (int CurrentHp, int MaxHp) GetHpInfo() => (current, MaxHp);


        public Action<DamageReceiveData> OnHpChanged { get; set; }

        private void Awake()
        {
            if (config != null)
                current = config.MaxHp;
        }

        public void ReceiveDamage(int damage)
        {
            if (IsDead) return;

            current -= damage;
            OnHpChanged?.Invoke(new DamageReceiveData
            {
                Damage = damage,
                CurrentHp = Current,
                MaxHp = MaxHp,
            });

            if (IsDead)
                Die();
        }

        /// <summary>
        /// Ponto único de execução de skills: valida via a própria SkillDefinition
        /// e executa o efeito. Controllers apenas encaminham a chamada.
        /// </summary>
        public bool TryUseSkill(GridController grid, Skills.SkillDefinition skill, Vector2Int targetPos)
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

        protected virtual void Die()
        {
            var cell = GetComponentInParent<Cell>();
            if (cell != null && cell.GetContent() == this)
                cell.RemoveContent();

            EventBus.Raise(new CharacterDiedEvent(this));
            Destroy(gameObject);
        }
    }
}