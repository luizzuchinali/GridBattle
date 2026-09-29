using UnityEngine;

namespace GridBattle.Gameplay.Entities.Interfaces
{
    public interface IAttacker
    {
        public void Attack(IDamageReceiver target);
        public bool IsInAttackRange(Vector2Int targetPosition);
    }
}