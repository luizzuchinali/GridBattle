using System;

namespace GridBattle.Gameplay.Entities.Interfaces
{
    public interface IDamageReceiver
    {
        void ReceiveDamage(int damage);

        (int CurrentHp, int MaxHp) GetHpInfo();
        event Action<DamageReceiveData> OnHpChanged;
    }

    /// <summary>
    /// HP change of a damage receiver. A pure refresh (e.g. max HP changed) has
    /// no damage and no healing.
    /// </summary>
    public struct DamageReceiveData
    {
        /// <summary>Damage of the hit (including the part absorbed by shields).</summary>
        public int Damage;

        /// <summary>Part of the damage absorbed by shields.</summary>
        public int Absorbed;

        public int Healed;
        public bool IsCrit;
        public int CurrentHp;
        public int MaxHp;
    }
}
