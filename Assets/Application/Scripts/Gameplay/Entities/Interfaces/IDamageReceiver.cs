using System;

namespace GridBattle.Gameplay.Entities.Interfaces
{
    public interface IDamageReceiver
    {
        void ReceiveDamage(int damage);

        Action<int> OnLifeChanged { get; set; }
    }
}