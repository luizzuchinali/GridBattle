using System;

namespace GridBattle.Gameplay.Entities.Interfaces
{
    public interface IDamageReceiver
    {
        void ReceiveDamage(int damage);

        (int CurrentHp, int MaxHp) GetHpInfo();
        event Action<DamageReceiveData> OnHpChanged;
    }

    public struct DamageReceiveData
    {
        public int Damage;
        public int CurrentHp;
        public int MaxHp;
    }
}
