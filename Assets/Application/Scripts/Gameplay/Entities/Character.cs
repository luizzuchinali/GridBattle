using System;
using GridBattle.Gameplay.Entities.Interfaces;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    public class Character : GridEntity, IDamageReceiver
    {
        [SerializeField]
        private int life = 100;

        public int Life => life;

        public Action<int> OnLifeChanged { get; set; }

        public void ReceiveDamage(int damage)
        {
            life -= damage;
            OnLifeChanged?.Invoke(life);
        }
    }
}