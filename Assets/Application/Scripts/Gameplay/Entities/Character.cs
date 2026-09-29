using System;
using GridBattle.Gameplay.Entities.Interfaces;
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
        public int WalkDistance => walkDistance;
        public int AttackDistance => attackDistance;

        public Action<DamageReceiveData> OnHpChanged { get; set; }

        public void ReceiveDamage(int damage)
        {
            current -= damage;
            OnHpChanged?.Invoke(new DamageReceiveData
            {
                Damage = damage,
                CurrentHp = Current,
                MaxHp = maxHp,
            });
        }

        public bool CanWalk(Vector2Int targetPos)
        {
            return Vector2Int.Distance(targetPos, CurrentGridPos) <= WalkDistance;
        }

        public void Attack(IDamageReceiver target)
        {
            target.ReceiveDamage(10);
        }

        public bool IsInAttackRange(Vector2Int targetPosition)
        {
            return Vector2Int.Distance(targetPosition, CurrentGridPos) <= AttackDistance;
        }
    }
}