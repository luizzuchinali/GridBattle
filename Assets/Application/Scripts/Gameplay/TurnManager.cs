using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using UnityEngine;

namespace GridBattle.Gameplay
{
    public class TurnManager : MonoBehaviour
    {
        private void Awake()
        {
            EventBus.Subscribe<PlayerActionEvent>(OnPlayerAction);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerActionEvent>(OnPlayerAction);
        }

        private void OnPlayerAction(PlayerActionEvent _)
        {
            var enemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude);
            foreach (var enemy in enemies)
            {
                enemy.Act();
            }
        }
    }
}