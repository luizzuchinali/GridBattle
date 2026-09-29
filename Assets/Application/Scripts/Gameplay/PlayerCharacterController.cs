using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Events;
using UnityEngine;

namespace GridBattle.Gameplay
{
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterController : MonoBehaviour
    {
        private PlayerCharacter _playerCharacter;
        private GridController _gridController;
        private Vector2Int _currentPlayerGridPosition;

        private void Awake()
        {
            EventBus.Subscribe<CellTapEvent>(OnCellTap);
            EventBus.Subscribe<PlayerCharacterSpawnEvent>(OnPlayerCharacterSpawn);

            _playerCharacter = GetComponent<PlayerCharacter>();
            _gridController = FindAnyObjectByType<GridController>();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CellTapEvent>(OnCellTap);
            EventBus.Unsubscribe<PlayerCharacterSpawnEvent>(OnPlayerCharacterSpawn);
        }

        private void OnPlayerCharacterSpawn(PlayerCharacterSpawnEvent @event)
        {
            _currentPlayerGridPosition = @event.GridPosition;
        }

        private void OnCellTap(CellTapEvent @event)
        {
            if (!@event.Cell.HasContent)
            {
                _gridController.Move(_currentPlayerGridPosition, @event.Cell.GridPosition);
                _currentPlayerGridPosition = @event.Cell.GridPosition;
            }
            else
            {
                var target = @event.Cell.GetContent();
                switch (target)
                {
                    case PlayerCharacter: return;
                    case IDamageReceiver receiver:
                        receiver.ReceiveDamage(10);
                        break;
                }
            }
        }
    }
}