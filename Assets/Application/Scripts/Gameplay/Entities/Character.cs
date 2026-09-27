using System;
using GridBattle.Gameplay.Entities.Interfaces;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    public class Character : GridEntity, IDamageReceiver
    {
        [SerializeField]
        private int current = 100;

        [SerializeField]
        private int maxHp = 100;

        public int Current => current;

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
    }
}