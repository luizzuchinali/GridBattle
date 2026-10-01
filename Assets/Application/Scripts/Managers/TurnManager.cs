using GridBattle.Gameplay.Controllers;
using GridBattle.Gameplay.Events;
using UnityEngine;

namespace GridBattle.Managers
{
    /// <summary>
    /// Turn flow: every action consumed by the player (PlayerActionEvent)
    /// triggers one full round of enemy turns. Enemy order comes from an
    /// unsorted FindObjectsByType, so it can change between runs.
    /// </summary>
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