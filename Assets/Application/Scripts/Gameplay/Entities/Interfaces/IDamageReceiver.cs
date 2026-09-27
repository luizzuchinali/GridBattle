using System;

namespace GridBattle.Gameplay.Entities.Interfaces
{
    public interface IDamageReceiver
    {
        void ReceiveDamage(int damage);

        Action<DamageReceiveData> OnHpChanged { get; set; }
    }

    public struct DamageReceiveData
    {
        public int Damage;
        public int CurrentHp;
        public int MaxHp;
    }
}