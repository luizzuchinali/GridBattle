using System;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    public class Character : GridEntity, IDamageReceiver, IWalker, IAttacker
    {
        [SerializeField]
        private int current = 100;

        [SerializeField]
        private int maxHp = 100;

        [SerializeField]
        protected int walkDistance = 1;

        [SerializeField]
        protected int attackDistance = 1;

        public int Current => current;
        public int MaxHp => maxHp;
        public int WalkDistance => walkDistance;
        public int AttackDistance => attackDistance;
        public bool IsDead => current <= 0;

        public Action<DamageReceiveData> OnHpChanged { get; set; }

        public void ReceiveDamage(int damage)
        {
            if (IsDead) return;

            current -= damage;
            OnHpChanged?.Invoke(new DamageReceiveData
            {
                Damage = damage,
                CurrentHp = Current,
                MaxHp = maxHp,
            });

            if (IsDead)
                Die();
        }

        protected virtual void Die()
        {
            var cell = GetComponentInParent<Cell>();
            if (cell != null && cell.GetContent() == this)
                cell.RemoveContent();

            EventBus.Raise(new CharacterDiedEvent(this));
            Destroy(gameObject);
        }

        public bool CanWalk(Vector2Int targetPos)
        {
            return GridRules.IsInWalkRange(CurrentGridPos, targetPos, WalkDistance);
        }

        public void Attack(IDamageReceiver target)
        {
            target.ReceiveDamage(10);
        }

        public bool IsInAttackRange(Vector2Int targetPosition)
        {
            return GridRules.IsInAttackRange(CurrentGridPos, targetPosition, AttackDistance);
        }
    }
}