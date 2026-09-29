using UnityEngine;

namespace GridBattle.Gameplay.Entities.Interfaces
{
    public interface IWalker
    {
        public bool CanWalk(Vector2Int targetPos);
    }
}